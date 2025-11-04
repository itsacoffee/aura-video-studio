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
            var model = await ValidateAndGetModelAsync(provider, runOverride, ct);
            if (model != null)
            {
                resolution.SelectedModelId = model.ModelId;
                resolution.Source = ModelSelectionSource.RunOverridePinned;
                resolution.Reasoning = $"Using run-override pinned model: {runOverride}";
                resolution.IsPinned = true;
                
                _logger.LogInformation("Model resolved via run-override (pinned): {ModelId}", model.ModelId);
                await _selectionStore.RecordSelectionAsync(resolution, ct);
                return resolution;
            }

            // Pinned model unavailable - block and require user action
            resolution.IsBlocked = true;
            resolution.BlockReason = $"Pinned model '{runOverride}' is unavailable";
            resolution.RecommendedAlternatives = await GetAlternativesAsync(provider, runOverride, ct);
            
            _logger.LogWarning("Pinned model {ModelId} unavailable, blocking for user decision", runOverride);
            return resolution;
        }

        // Priority 2: Run override (not pinned)
        if (!string.IsNullOrWhiteSpace(runOverride))
        {
            var model = await ValidateAndGetModelAsync(provider, runOverride, ct);
            if (model != null)
            {
                resolution.SelectedModelId = model.ModelId;
                resolution.Source = ModelSelectionSource.RunOverride;
                resolution.Reasoning = $"Using run-override model: {runOverride}";
                
                _logger.LogInformation("Model resolved via run-override: {ModelId}", model.ModelId);
                await _selectionStore.RecordSelectionAsync(resolution, ct);
                return resolution;
            }

            _logger.LogWarning("Run-override model {ModelId} unavailable, falling back", runOverride);
        }

        // Priority 3: Stage pinned model
        var stageSelection = await _selectionStore.GetSelectionAsync(provider, stage, ModelSelectionScope.Stage, ct);
        if (stageSelection?.IsPinned == true)
        {
            var model = await ValidateAndGetModelAsync(provider, stageSelection.ModelId, ct);
            if (model != null)
            {
                resolution.SelectedModelId = model.ModelId;
                resolution.Source = ModelSelectionSource.StagePinned;
                resolution.Reasoning = $"Using stage-pinned model: {stageSelection.ModelId}";
                resolution.IsPinned = true;
                
                _logger.LogInformation("Model resolved via stage-pinned: {ModelId}", model.ModelId);
                await _selectionStore.RecordSelectionAsync(resolution, ct);
                return resolution;
            }

            // Pinned model unavailable - block and require user action
            resolution.IsBlocked = true;
            resolution.BlockReason = $"Stage-pinned model '{stageSelection.ModelId}' is unavailable";
            resolution.RecommendedAlternatives = await GetAlternativesAsync(provider, stageSelection.ModelId, ct);
            
            _logger.LogWarning("Stage-pinned model {ModelId} unavailable, blocking for user decision", stageSelection.ModelId);
            return resolution;
        }

        // Priority 4: Project override
        var projectSelection = await _selectionStore.GetSelectionAsync(provider, stage, ModelSelectionScope.Project, ct);
        if (projectSelection != null)
        {
            var model = await ValidateAndGetModelAsync(provider, projectSelection.ModelId, ct);
            if (model != null)
            {
                resolution.SelectedModelId = model.ModelId;
                resolution.Source = ModelSelectionSource.ProjectOverride;
                resolution.Reasoning = $"Using project-override model: {projectSelection.ModelId}";
                
                _logger.LogInformation("Model resolved via project-override: {ModelId}", model.ModelId);
                await _selectionStore.RecordSelectionAsync(resolution, ct);
                return resolution;
            }

            _logger.LogWarning("Project-override model {ModelId} unavailable, falling back", projectSelection.ModelId);
        }

        // Priority 5: Global default
        var globalSelection = await _selectionStore.GetSelectionAsync(provider, stage, ModelSelectionScope.Global, ct);
        if (globalSelection != null)
        {
            var model = await ValidateAndGetModelAsync(provider, globalSelection.ModelId, ct);
            if (model != null)
            {
                resolution.SelectedModelId = model.ModelId;
                resolution.Source = ModelSelectionSource.GlobalDefault;
                resolution.Reasoning = $"Using global default model: {globalSelection.ModelId}";
                
                _logger.LogInformation("Model resolved via global default: {ModelId}", model.ModelId);
                await _selectionStore.RecordSelectionAsync(resolution, ct);
                return resolution;
            }

            _logger.LogWarning("Global default model {ModelId} unavailable, falling back", globalSelection.ModelId);
        }

        // Priority 6: Catalog safe fallback (only if automatic fallback allowed)
        var allowAutoFallback = await _selectionStore.GetAutoFallbackSettingAsync(ct);
        if (allowAutoFallback)
        {
            var (fallbackModel, reasoning) = _modelCatalog.FindOrDefault(provider);
            if (fallbackModel != null)
            {
                resolution.SelectedModelId = fallbackModel.ModelId;
                resolution.Source = ModelSelectionSource.AutomaticFallback;
                resolution.Reasoning = $"Using automatic fallback: {reasoning}";
                resolution.RequiresUserNotification = true;
                
                _logger.LogInformation("Model resolved via automatic fallback: {ModelId}", fallbackModel.ModelId);
                await _selectionStore.RecordSelectionAsync(resolution, ct);
                return resolution;
            }
        }

        // No model available and auto-fallback disabled - block
        resolution.IsBlocked = true;
        resolution.BlockReason = "No model selection configured and automatic fallback is disabled";
        resolution.RecommendedAlternatives = await GetAlternativesAsync(provider, null, ct);
        
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
        var model = await ValidateAndGetModelAsync(provider, modelId, ct);
        if (model == null)
        {
            return new ModelSelectionResult
            {
                Applied = false,
                Reason = $"Model '{modelId}' not found for provider '{provider}'",
                Recommended = await GetAlternativesAsync(provider, modelId, ct)
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

        await _selectionStore.SaveSelectionAsync(selection, ct);

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

        await _selectionStore.ClearSelectionsAsync(provider, stage, scope, ct);
    }

    /// <summary>
    /// Get all current model selections
    /// </summary>
    public async Task<ModelSelectionState> GetAllSelectionsAsync(CancellationToken ct = default)
    {
        var globalSelections = await _selectionStore.GetAllSelectionsAsync(ModelSelectionScope.Global, ct);
        var projectSelections = await _selectionStore.GetAllSelectionsAsync(ModelSelectionScope.Project, ct);
        var stageSelections = await _selectionStore.GetAllSelectionsAsync(ModelSelectionScope.Stage, ct);
        var allowAutoFallback = await _selectionStore.GetAutoFallbackSettingAsync(ct);

        return new ModelSelectionState
        {
            GlobalDefaults = globalSelections,
            ProjectOverrides = projectSelections,
            StageSelections = stageSelections,
            AllowAutomaticFallback = allowAutoFallback
        };
    }

    private async Task<ModelRegistry.ModelInfo?> ValidateAndGetModelAsync(
        string provider,
        string modelId,
        CancellationToken ct)
    {
        var (model, _) = _modelCatalog.FindOrDefault(provider, modelId);
        return await Task.FromResult(model);
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

        return await Task.FromResult(alternatives);
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
