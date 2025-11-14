using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Adapters;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ModelSelection;

/// <summary>
/// Service for managing model selection with explicit user control and precedence rules.
/// Ensures users maintain complete control over model selection at every decision point.
/// </summary>
public class ModelSelectionService
{
    private readonly ILogger<ModelSelectionService> _logger;
    private readonly ModelCatalog _modelCatalog;
    private readonly ModelSelectionStore _selectionStore;

    public ModelSelectionService(
        ILogger<ModelSelectionService> logger,
        ModelCatalog modelCatalog,
        ModelSelectionStore selectionStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _modelCatalog = modelCatalog ?? throw new ArgumentNullException(nameof(modelCatalog));
        _selectionStore = selectionStore ?? throw new ArgumentNullException(nameof(selectionStore));
    }

    /// <summary>
    /// Resolve model to use based on precedence rules with full audit trail.
    /// Precedence: Run override (pin) > Stage pinned > Project override > Global default > Catalog fallback
    /// </summary>
    public async Task<ModelResolutionResult> ResolveModelAsync(
        string provider,
        string stage,
        string? runOverride = null,
        bool runOverridePinned = false,
        string? jobId = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Resolving model for provider={Provider}, stage={Stage}, runOverride={RunOverride}, pinned={Pinned}",
            provider, stage, runOverride, runOverridePinned);

        var resolution = new ModelResolutionResult
        {
            Provider = provider,
            Stage = stage,
            Reasoning = string.Empty,
            ResolutionTimestamp = DateTime.UtcNow,
            JobId = jobId
        };

        // Priority 1: Run override with pin flag
        if (!string.IsNullOrWhiteSpace(runOverride) && runOverridePinned)
        {
            var model = await ValidateAndGetModelAsync(provider, runOverride, ct).ConfigureAwait(false);
            if (model != null)
            {
                resolution.SelectedModelId = model.ModelId;
                resolution.Source = ModelSelectionSource.RunOverridePinned;
                resolution.Reasoning = $"Using run-override pinned model: {runOverride}";
                resolution.IsPinned = true;
                
                _logger.LogInformation("Model resolved via run-override (pinned): {ModelId}", model.ModelId);
                await _selectionStore.RecordSelectionAsync(resolution, ct).ConfigureAwait(false);
                return resolution;
            }

            // Pinned model unavailable - block and require user action
            resolution.IsBlocked = true;
            resolution.BlockReason = $"Pinned model '{runOverride}' is unavailable";
            resolution.RecommendedAlternatives = await GetAlternativesAsync(provider, runOverride, ct).ConfigureAwait(false);
            
            _logger.LogWarning("Pinned model {ModelId} unavailable, blocking for user decision", runOverride);
            return resolution;
        }

        // Priority 2: Run override (not pinned)
        if (!string.IsNullOrWhiteSpace(runOverride))
        {
            var model = await ValidateAndGetModelAsync(provider, runOverride, ct).ConfigureAwait(false);
            if (model != null)
            {
                resolution.SelectedModelId = model.ModelId;
                resolution.Source = ModelSelectionSource.RunOverride;
                resolution.Reasoning = $"Using run-override model: {runOverride}";
                
                _logger.LogInformation("Model resolved via run-override: {ModelId}", model.ModelId);
                await _selectionStore.RecordSelectionAsync(resolution, ct).ConfigureAwait(false);
                return resolution;
            }

            _logger.LogWarning("Run-override model {ModelId} unavailable, falling back", runOverride);
            resolution.FallbackReason = $"Requested run-override model '{runOverride}' was unavailable";
        }

        // Priority 3: Stage pinned model
        var stageSelection = await _selectionStore.GetSelectionAsync(provider, stage, ModelSelectionScope.Stage, ct).ConfigureAwait(false);
        if (stageSelection?.IsPinned == true)
        {
            var model = await ValidateAndGetModelAsync(provider, stageSelection.ModelId, ct).ConfigureAwait(false);
            if (model != null)
            {
                resolution.SelectedModelId = model.ModelId;
                resolution.Source = ModelSelectionSource.StagePinned;
                resolution.Reasoning = $"Using stage-pinned model: {stageSelection.ModelId}";
                resolution.IsPinned = true;
                
                _logger.LogInformation("Model resolved via stage-pinned: {ModelId}", model.ModelId);
                await _selectionStore.RecordSelectionAsync(resolution, ct).ConfigureAwait(false);
                return resolution;
            }

            // Pinned model unavailable - block and require user action
            resolution.IsBlocked = true;
            resolution.BlockReason = $"Stage-pinned model '{stageSelection.ModelId}' is unavailable";
            resolution.RecommendedAlternatives = await GetAlternativesAsync(provider, stageSelection.ModelId, ct).ConfigureAwait(false);
            
            _logger.LogWarning("Stage-pinned model {ModelId} unavailable, blocking for user decision", stageSelection.ModelId);
            return resolution;
        }

        // Priority 4: Project override
        var projectSelection = await _selectionStore.GetSelectionAsync(provider, stage, ModelSelectionScope.Project, ct).ConfigureAwait(false);
        if (projectSelection != null)
        {
            var model = await ValidateAndGetModelAsync(provider, projectSelection.ModelId, ct).ConfigureAwait(false);
            if (model != null)
            {
                resolution.SelectedModelId = model.ModelId;
                resolution.Source = ModelSelectionSource.ProjectOverride;
                resolution.Reasoning = $"Using project-override model: {projectSelection.ModelId}";
                
                _logger.LogInformation("Model resolved via project-override: {ModelId}", model.ModelId);
                await _selectionStore.RecordSelectionAsync(resolution, ct).ConfigureAwait(false);
                return resolution;
            }

            _logger.LogWarning("Project-override model {ModelId} unavailable, falling back", projectSelection.ModelId);
            resolution.FallbackReason = $"Project-override model '{projectSelection.ModelId}' was unavailable";
        }

        // Priority 5: Global default
        var globalSelection = await _selectionStore.GetSelectionAsync(provider, stage, ModelSelectionScope.Global, ct).ConfigureAwait(false);
        if (globalSelection != null)
        {
            var model = await ValidateAndGetModelAsync(provider, globalSelection.ModelId, ct).ConfigureAwait(false);
            if (model != null)
            {
                resolution.SelectedModelId = model.ModelId;
                resolution.Source = ModelSelectionSource.GlobalDefault;
                resolution.Reasoning = $"Using global default model: {globalSelection.ModelId}";
                
                _logger.LogInformation("Model resolved via global default: {ModelId}", model.ModelId);
                await _selectionStore.RecordSelectionAsync(resolution, ct).ConfigureAwait(false);
                return resolution;
            }

            _logger.LogWarning("Global default model {ModelId} unavailable, falling back", globalSelection.ModelId);
            resolution.FallbackReason = $"Global default model '{globalSelection.ModelId}' was unavailable";
        }

        // Priority 6: Catalog safe fallback (only if automatic fallback allowed)
        var allowAutoFallback = await _selectionStore.GetAutoFallbackSettingAsync(ct).ConfigureAwait(false);
        if (allowAutoFallback)
        {
            var (fallbackModel, reasoning) = _modelCatalog.FindOrDefault(provider);
            if (fallbackModel != null)
            {
                resolution.SelectedModelId = fallbackModel.ModelId;
                resolution.Source = ModelSelectionSource.AutomaticFallback;
                resolution.Reasoning = $"Using automatic fallback: {reasoning}";
                resolution.FallbackReason = string.IsNullOrWhiteSpace(resolution.FallbackReason) 
                    ? "No configured model was available" 
                    : resolution.FallbackReason;
                resolution.RequiresUserNotification = true;
                
                _logger.LogInformation("Model resolved via automatic fallback: {ModelId}", fallbackModel.ModelId);
                await _selectionStore.RecordSelectionAsync(resolution, ct).ConfigureAwait(false);
                return resolution;
            }
        }

        // No model available and auto-fallback disabled - block
        resolution.IsBlocked = true;
        resolution.BlockReason = "No model selection configured and automatic fallback is disabled";
        resolution.RecommendedAlternatives = await GetAlternativesAsync(provider, null, ct).ConfigureAwait(false);
        
        _logger.LogError("Unable to resolve model for provider {Provider}, stage {Stage}", provider, stage);
        return resolution;
    }

    /// <summary>
    /// Set model selection with explicit scope and pin option
    /// </summary>
    public async Task<ModelSelectionResult> SetModelSelectionAsync(
        string provider,
        string? stage,
        string modelId,
        ModelSelectionScope scope,
        bool pin,
        string? setBy,
        string? reason,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Setting model selection: provider={Provider}, stage={Stage}, model={ModelId}, scope={Scope}, pin={Pin}",
            provider, stage, modelId, scope, pin);

        // Validate model exists
        var model = await ValidateAndGetModelAsync(provider, modelId, ct).ConfigureAwait(false);
        if (model == null)
        {
            return new ModelSelectionResult
            {
                Applied = false,
                Reason = $"Model '{modelId}' not found for provider '{provider}'",
                Recommended = await GetAlternativesAsync(provider, modelId, ct).ConfigureAwait(false)
            };
        }

        // Check for deprecation warnings
        if (model.DeprecationDate.HasValue && model.DeprecationDate.Value <= DateTime.UtcNow)
        {
            _logger.LogWarning(
                "Model {ModelId} is deprecated. Replacement: {Replacement}",
                modelId, model.ReplacementModel ?? "none");
        }

        var selection = new ModelSelection
        {
            Provider = provider,
            Stage = stage ?? string.Empty,
            ModelId = modelId,
            Scope = scope,
            IsPinned = pin,
            SetBy = setBy ?? "system",
            SetAt = DateTime.UtcNow,
            Reason = reason ?? "User selection"
        };

        await _selectionStore.SaveSelectionAsync(selection, ct).ConfigureAwait(false);

        return new ModelSelectionResult
        {
            Applied = true,
            Reason = $"Model selection saved successfully",
            DeprecationWarning = model.DeprecationDate.HasValue 
                ? $"Model is deprecated as of {model.DeprecationDate.Value:yyyy-MM-dd}. Consider using {model.ReplacementModel ?? "an alternative"}."
                : null
        };
    }

    /// <summary>
    /// Clear model selections by scope
    /// </summary>
    public async Task ClearSelectionsAsync(
        string? provider,
        string? stage,
        ModelSelectionScope? scope,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Clearing model selections: provider={Provider}, stage={Stage}, scope={Scope}",
            provider, stage, scope);

        await _selectionStore.ClearSelectionsAsync(provider, stage, scope, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Get all current model selections
    /// </summary>
    public async Task<ModelSelectionState> GetAllSelectionsAsync(CancellationToken ct = default)
    {
        var globalSelections = await _selectionStore.GetAllSelectionsAsync(ModelSelectionScope.Global, ct).ConfigureAwait(false);
        var projectSelections = await _selectionStore.GetAllSelectionsAsync(ModelSelectionScope.Project, ct).ConfigureAwait(false);
        var stageSelections = await _selectionStore.GetAllSelectionsAsync(ModelSelectionScope.Stage, ct).ConfigureAwait(false);
        var allowAutoFallback = await _selectionStore.GetAutoFallbackSettingAsync(ct).ConfigureAwait(false);

        return new ModelSelectionState
        {
            GlobalDefaults = globalSelections,
            ProjectOverrides = projectSelections,
            StageSelections = stageSelections,
            AllowAutomaticFallback = allowAutoFallback
        };
    }

    /// <summary>
    /// Get audit log of model selection resolutions
    /// </summary>
    public async Task<List<ModelSelectionAudit>> GetAuditLogAsync(
        int? limit = null,
        CancellationToken ct = default)
    {
        return await _selectionStore.GetAuditLogAsync(limit, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Get audit log entries for a specific job
    /// </summary>
    public async Task<List<ModelSelectionAudit>> GetAuditLogByJobIdAsync(
        string jobId,
        CancellationToken ct = default)
    {
        return await _selectionStore.GetAuditLogByJobIdAsync(jobId, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Explain model choice by comparing selected model with recommendations
    /// </summary>
    public async Task<ModelChoiceExplanation> ExplainModelChoiceAsync(
        string provider,
        string stage,
        string selectedModelId,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Explaining model choice: provider={Provider}, stage={Stage}, selected={ModelId}",
            provider, stage, selectedModelId);

        var selectedModel = await ValidateAndGetModelAsync(provider, selectedModelId, ct).ConfigureAwait(false);
        if (selectedModel == null)
        {
            throw new ArgumentException($"Model '{selectedModelId}' not found for provider '{provider}'");
        }

        var allModels = _modelCatalog.GetAllModels(provider);
        var activeModels = allModels
            .Where(m => !m.DeprecationDate.HasValue || m.DeprecationDate.Value > DateTime.UtcNow)
            .OrderByDescending(m => m.ContextWindow)
            .ToList();

        var recommendedModel = activeModels.FirstOrDefault();
        var selectedIsRecommended = recommendedModel?.ModelId == selectedModelId;

        var reasoning = GenerateChoiceReasoning(selectedModel, recommendedModel, stage);
        var tradeoffs = GenerateTradeoffs(selectedModel, recommendedModel);
        var suggestions = GenerateSuggestions(selectedModel, activeModels, stage);

        return new ModelChoiceExplanation
        {
            SelectedModel = new ModelInfo
            {
                ModelId = selectedModel.ModelId,
                Provider = selectedModel.Provider,
                MaxTokens = selectedModel.MaxTokens,
                ContextWindow = selectedModel.ContextWindow,
                IsDeprecated = selectedModel.DeprecationDate.HasValue && 
                               selectedModel.DeprecationDate.Value <= DateTime.UtcNow
            },
            RecommendedModel = recommendedModel != null ? new ModelInfo
            {
                ModelId = recommendedModel.ModelId,
                Provider = recommendedModel.Provider,
                MaxTokens = recommendedModel.MaxTokens,
                ContextWindow = recommendedModel.ContextWindow,
                IsDeprecated = recommendedModel.DeprecationDate.HasValue && 
                               recommendedModel.DeprecationDate.Value <= DateTime.UtcNow
            } : null,
            SelectedIsRecommended = selectedIsRecommended,
            Reasoning = reasoning,
            Tradeoffs = tradeoffs,
            Suggestions = suggestions
        };
    }

    private string GenerateChoiceReasoning(
        ModelRegistry.ModelInfo selected,
        ModelRegistry.ModelInfo? recommended,
        string stage)
    {
        if (recommended == null || selected.ModelId == recommended.ModelId)
        {
            return $"Your selected model '{selected.ModelId}' is the recommended choice for {stage} operations. " +
                   $"It provides {selected.ContextWindow} tokens of context, suitable for most use cases.";
        }

        var contextComparison = selected.ContextWindow > recommended.ContextWindow ? "larger" : "smaller";
        return $"You selected '{selected.ModelId}' which has a {contextComparison} context window " +
               $"({selected.ContextWindow} tokens) compared to the recommended '{recommended.ModelId}' " +
               $"({recommended.ContextWindow} tokens). " +
               $"For {stage} operations, context window size affects how much information can be processed at once.";
    }

    private List<string> GenerateTradeoffs(
        ModelRegistry.ModelInfo selected,
        ModelRegistry.ModelInfo? recommended)
    {
        var tradeoffs = new List<string>();

        if (recommended == null)
        {
            return tradeoffs;
        }

        if (selected.ContextWindow < recommended.ContextWindow)
        {
            tradeoffs.Add($"Smaller context window: {selected.ContextWindow} vs {recommended.ContextWindow} tokens");
            tradeoffs.Add("May require breaking large scripts into smaller chunks");
        }
        else if (selected.ContextWindow > recommended.ContextWindow)
        {
            tradeoffs.Add($"Larger context window: {selected.ContextWindow} vs {recommended.ContextWindow} tokens");
            tradeoffs.Add("Can handle longer scripts in a single operation");
        }

        if (selected.MaxTokens < recommended.MaxTokens)
        {
            tradeoffs.Add($"Lower output limit: {selected.MaxTokens} vs {recommended.MaxTokens} tokens");
        }

        if (selected.DeprecationDate.HasValue)
        {
            tradeoffs.Add($"Model is deprecated or will be deprecated on {selected.DeprecationDate.Value:yyyy-MM-dd}");
            if (!string.IsNullOrEmpty(selected.ReplacementModel))
            {
                tradeoffs.Add($"Consider migrating to {selected.ReplacementModel}");
            }
        }

        return tradeoffs;
    }

    private List<string> GenerateSuggestions(
        ModelRegistry.ModelInfo selected,
        List<ModelRegistry.ModelInfo> activeModels,
        string stage)
    {
        var suggestions = new List<string>();

        if (selected.DeprecationDate.HasValue && selected.DeprecationDate.Value <= DateTime.UtcNow)
        {
            suggestions.Add($"⚠️ Consider upgrading to {selected.ReplacementModel ?? "a newer model"}");
        }

        var largerModels = activeModels
            .Where(m => m.ContextWindow > selected.ContextWindow)
            .OrderBy(m => m.ContextWindow)
            .Take(2)
            .ToList();

        if (largerModels.Count != 0)
        {
            foreach (var model in largerModels)
            {
                suggestions.Add($"For larger scripts, consider {model.ModelId} ({model.ContextWindow} tokens)");
            }
        }

        var similarModels = activeModels
            .Where(m => Math.Abs(m.ContextWindow - selected.ContextWindow) < 10000 && 
                       m.ModelId != selected.ModelId)
            .Take(2)
            .ToList();

        if (similarModels.Any())
        {
            foreach (var model in similarModels)
            {
                suggestions.Add($"Alternative with similar capabilities: {model.ModelId}");
            }
        }

        if (!suggestions.Any())
        {
            suggestions.Add("Your current selection is appropriate for this stage");
        }

        return suggestions;
    }

    private async Task<ModelRegistry.ModelInfo?> ValidateAndGetModelAsync(
        string provider,
        string modelId,
        CancellationToken ct)
    {
        var (model, _) = _modelCatalog.FindOrDefault(provider, modelId);
        return await Task.FromResult(model).ConfigureAwait(false);
    }

    private async Task<List<string>> GetAlternativesAsync(
        string provider,
        string? requestedModel,
        CancellationToken ct)
    {
        var allModels = _modelCatalog.GetAllModels(provider);
        var alternatives = allModels
            .Where(m => !m.DeprecationDate.HasValue || m.DeprecationDate.Value > DateTime.UtcNow)
            .OrderByDescending(m => m.ContextWindow)
            .Take(3)
            .Select(m => m.ModelId)
            .ToList();

        return await Task.FromResult(alternatives).ConfigureAwait(false);
    }
}

/// <summary>
/// Result of model resolution including audit information
/// </summary>
public class ModelResolutionResult
{
    public required string Provider { get; set; }
    public required string Stage { get; set; }
    public string? SelectedModelId { get; set; }
    public ModelSelectionSource Source { get; set; }
    public required string Reasoning { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public string? FallbackReason { get; set; }
    public List<string> RecommendedAlternatives { get; set; } = new();
    public bool RequiresUserNotification { get; set; }
    public DateTime ResolutionTimestamp { get; set; }
    public string? JobId { get; set; }
}

/// <summary>
/// Result of setting model selection
/// </summary>
public class ModelSelectionResult
{
    public bool Applied { get; set; }
    public required string Reason { get; set; }
    public string? DeprecationWarning { get; set; }
    public List<string> Recommended { get; set; } = new();
}

/// <summary>
/// Complete state of all model selections
/// </summary>
public class ModelSelectionState
{
    public List<ModelSelection> GlobalDefaults { get; set; } = new();
    public List<ModelSelection> ProjectOverrides { get; set; } = new();
    public List<ModelSelection> StageSelections { get; set; } = new();
    public bool AllowAutomaticFallback { get; set; }
}

/// <summary>
/// Model selection record
/// </summary>
public class ModelSelection
{
    public required string Provider { get; set; }
    public required string Stage { get; set; }
    public required string ModelId { get; set; }
    public ModelSelectionScope Scope { get; set; }
    public bool IsPinned { get; set; }
    public required string SetBy { get; set; }
    public DateTime SetAt { get; set; }
    public required string Reason { get; set; }
}

/// <summary>
/// Scope of model selection
/// </summary>
public enum ModelSelectionScope
{
    Global,    // Application-wide default
    Project,   // Per-project override
    Stage,     // Per-pipeline-stage setting
    Run        // Single run override
}

/// <summary>
/// Source of resolved model selection
/// </summary>
public enum ModelSelectionSource
{
    RunOverridePinned,
    RunOverride,
    StagePinned,
    ProjectOverride,
    GlobalDefault,
    AutomaticFallback
}

/// <summary>
/// Explanation comparing selected model with recommendations
/// </summary>
public class ModelChoiceExplanation
{
    public required ModelInfo SelectedModel { get; set; }
    public ModelInfo? RecommendedModel { get; set; }
    public bool SelectedIsRecommended { get; set; }
    public required string Reasoning { get; set; }
    public List<string> Tradeoffs { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
}

/// <summary>
/// Model information for explanations
/// </summary>
public class ModelInfo
{
    public required string ModelId { get; set; }
    public required string Provider { get; set; }
    public int MaxTokens { get; set; }
    public int ContextWindow { get; set; }
    public bool IsDeprecated { get; set; }
}
