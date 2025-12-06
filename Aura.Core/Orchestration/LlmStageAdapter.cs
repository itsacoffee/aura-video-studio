using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Cache;
using Aura.Core.AI.Validation;
using Aura.Core.Configuration;
using Aura.Core.Models;
using Aura.Core.Models.Visual;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Core.Services.CostTracking;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Orchestration;

/// <summary>
/// Stage adapter for LLM operations (brief→plan, plan→script, visual prompts)
/// Routes through unified orchestrator with schema validation
/// </summary>
public class LlmStageAdapter : UnifiedGenerationOrchestrator<LlmStageRequest, LlmStageResponse>
{
    private readonly Dictionary<string, ILlmProvider> _providers;
    private readonly ProviderMixer _providerMixer;
    private readonly ProviderSettings? _providerSettings;
    
    /// <summary>
    /// Tracks if the current operation is a translation request.
    /// Used to prevent RuleBased provider from being used for translation.
    /// </summary>
    private bool _isTranslationOperation = false;

    public LlmStageAdapter(
        ILogger<LlmStageAdapter> logger,
        Dictionary<string, ILlmProvider> providers,
        ProviderMixer providerMixer,
        ProviderSettings? providerSettings = null,
        ILlmCache? cache = null,
        SchemaValidator? schemaValidator = null,
        EnhancedCostTrackingService? costTrackingService = null,
        TokenTrackingService? tokenTrackingService = null)
        : base(logger, cache, schemaValidator, costTrackingService, tokenTrackingService)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _providerMixer = providerMixer ?? throw new ArgumentNullException(nameof(providerMixer));
        _providerSettings = providerSettings;
    }

    /// <summary>
    /// Generate script using orchestrated LLM call
    /// </summary>
    public async Task<OrchestrationResult<string>> GenerateScriptAsync(
        Brief brief,
        PlanSpec spec,
        string preferredTier,
        bool offlineOnly,
        CancellationToken ct)
    {
        var request = new LlmStageRequest
        {
            StageType = LlmStageType.ScriptGeneration,
            Brief = brief,
            PlanSpec = spec
        };

        var config = new OrchestrationConfig
        {
            PreferredTier = preferredTier,
            OfflineOnly = offlineOnly,
            EnableCache = true,
            ValidateSchema = false
        };

        var result = await ExecuteAsync(request, config, ct).ConfigureAwait(false);

        if (!result.IsSuccess || result.Data == null)
        {
            return OrchestrationResult<string>.Failure(
                result.OperationId,
                result.ElapsedMs,
                result.ErrorMessage ?? "Script generation failed");
        }

        return OrchestrationResult<string>.Success(
            result.Data.Content,
            result.OperationId,
            result.ElapsedMs,
            result.WasCached,
            result.ProviderUsed);
    }

    /// <summary>
    /// Generate visual prompt using orchestrated LLM call
    /// </summary>
    public async Task<OrchestrationResult<string>> GenerateVisualPromptAsync(
        string sceneText,
        string? previousSceneText,
        string videoTone,
        VisualStyle targetStyle,
        string preferredTier,
        bool offlineOnly,
        CancellationToken ct)
    {
        var request = new LlmStageRequest
        {
            StageType = LlmStageType.VisualPrompt,
            SceneText = sceneText,
            PreviousSceneText = previousSceneText,
            VideoTone = videoTone,
            TargetStyle = targetStyle
        };

        var config = new OrchestrationConfig
        {
            PreferredTier = preferredTier,
            OfflineOnly = offlineOnly,
            EnableCache = true,
            ValidateSchema = false
        };

        var result = await ExecuteAsync(request, config, ct).ConfigureAwait(false);

        if (!result.IsSuccess || result.Data == null)
        {
            return OrchestrationResult<string>.Failure(
                result.OperationId,
                result.ElapsedMs,
                result.ErrorMessage ?? "Visual prompt generation failed");
        }

        return OrchestrationResult<string>.Success(
            result.Data.Content,
            result.OperationId,
            result.ElapsedMs,
            result.WasCached,
            result.ProviderUsed);
    }

    /// <summary>
    /// Generate chat completion using orchestrated LLM call.
    /// This is the unified path for ideation, translation, and other chat-based LLM operations.
    /// Routes through the same orchestration as script generation for consistent provider selection,
    /// fallback logic, timeout configuration, and model override handling.
    /// </summary>
    public async Task<OrchestrationResult<string>> GenerateChatCompletionAsync(
        string systemPrompt,
        string userPrompt,
        string preferredTier,
        bool offlineOnly,
        LlmParameters? llmParameters = null,
        CancellationToken ct = default)
    {
        // Detect if this is a translation operation based on system prompt
        // This is used to prevent RuleBased provider from being used for translation
        _isTranslationOperation = systemPrompt.Contains("translator", StringComparison.OrdinalIgnoreCase) ||
                                  systemPrompt.Contains("translate", StringComparison.OrdinalIgnoreCase) ||
                                  systemPrompt.Contains("translation", StringComparison.OrdinalIgnoreCase) ||
                                  systemPrompt.Contains("source language", StringComparison.OrdinalIgnoreCase) ||
                                  systemPrompt.Contains("target language", StringComparison.OrdinalIgnoreCase);

        var request = new LlmStageRequest
        {
            StageType = LlmStageType.ChatCompletion,
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            LlmParameters = llmParameters
        };

        var config = new OrchestrationConfig
        {
            PreferredTier = preferredTier,
            OfflineOnly = offlineOnly,
            EnableCache = true,
            ValidateSchema = false
        };

        var result = await ExecuteAsync(request, config, ct).ConfigureAwait(false);

        if (!result.IsSuccess || result.Data == null)
        {
            return OrchestrationResult<string>.Failure(
                result.OperationId,
                result.ElapsedMs,
                result.ErrorMessage ?? "Chat completion failed");
        }

        return OrchestrationResult<string>.Success(
            result.Data.Content,
            result.OperationId,
            result.ElapsedMs,
            result.WasCached,
            result.ProviderUsed);
    }

    protected override string GetStageName() => "LLM";

    protected override async Task<ProviderInfo[]> GetProvidersAsync(
        OrchestrationConfig config,
        CancellationToken ct)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        var preferredTier = config.PreferredTier ?? "Free";
        var offlineOnly = config.OfflineOnly;

        // Get preferred provider from settings if available
        var preferredProvider = _providerSettings?.GetPreferredLlmProvider();
        
        var decision = _providerMixer.ResolveLlm(_providers, preferredTier, offlineOnly, preferredProvider);
        _providerMixer.LogDecision(decision);

        var providerInfos = new List<ProviderInfo>();

        // If decision is "None", skip to fallback logic
        if (decision.ProviderName != "None")
        {
            foreach (var providerName in decision.DowngradeChain)
            {
                if (_providers.TryGetValue(providerName, out var provider))
                {
                    providerInfos.Add(new ProviderInfo(
                        providerName,
                        "default",
                        providerInfos.Count,
                        provider));
                }
            }
        }

        // Only add RuleBased fallback for non-translation operations
        // RuleBased provider cannot perform translations - it requires an actual LLM
        if (providerInfos.Count == 0)
        {
            if (_isTranslationOperation)
            {
                Logger.LogError(
                    "No LLM providers available for translation. " +
                    "Translation requires Ollama or another LLM provider. " +
                    "Please ensure Ollama is running: ollama serve");
                
                // Reset the flag before throwing
                _isTranslationOperation = false;
                
                throw new InvalidOperationException(
                    "Translation requires an LLM provider (Ollama). " +
                    "RuleBased provider cannot perform translations. " +
                    "Please ensure Ollama is running and configured.");
            }
            else if (_providers.TryGetValue("RuleBased", out var ruleBasedProvider))
            {
                Logger.LogInformation("No providers available from ProviderMixer decision, falling back to RuleBased provider");
                providerInfos.Add(new ProviderInfo(
                    "RuleBased",
                    "default",
                    0,
                    ruleBasedProvider));
            }
        }

        // Reset the flag after use
        _isTranslationOperation = false;

        return providerInfos.ToArray();
    }

    protected override async Task<LlmStageResponse> ExecuteProviderAsync(
        ProviderInfo provider,
        LlmStageRequest request,
        OrchestrationConfig config,
        CancellationToken ct)
    {
        var llmProvider = (ILlmProvider)provider.Implementation;

        string content;
        switch (request.StageType)
        {
            case LlmStageType.ScriptGeneration:
                if (request.Brief == null || request.PlanSpec == null)
                {
                    throw new InvalidOperationException("Brief and PlanSpec required for script generation");
                }
                content = await llmProvider.DraftScriptAsync(request.Brief, request.PlanSpec, ct).ConfigureAwait(false);
                break;

            case LlmStageType.VisualPrompt:
                if (request.SceneText == null || request.VideoTone == null || request.TargetStyle == null)
                {
                    throw new InvalidOperationException("SceneText, VideoTone, and TargetStyle required for visual prompt");
                }
                var visualResult = await llmProvider.GenerateVisualPromptAsync(
                    request.SceneText,
                    request.PreviousSceneText,
                    request.VideoTone,
                    request.TargetStyle.Value,
                    ct).ConfigureAwait(false);
                
                content = visualResult != null 
                    ? JsonSerializer.Serialize(visualResult) 
                    : throw new InvalidOperationException("Visual prompt generation returned null");
                break;

            case LlmStageType.RawCompletion:
                if (request.Prompt == null)
                {
                    throw new InvalidOperationException("Prompt required for raw completion");
                }
                content = await llmProvider.CompleteAsync(request.Prompt, ct).ConfigureAwait(false);
                break;

            case LlmStageType.ChatCompletion:
                if (request.SystemPrompt == null || request.UserPrompt == null)
                {
                    throw new InvalidOperationException("SystemPrompt and UserPrompt required for chat completion");
                }
                content = await llmProvider.GenerateChatCompletionAsync(
                    request.SystemPrompt,
                    request.UserPrompt,
                    request.LlmParameters,
                    ct).ConfigureAwait(false);
                break;

            default:
                throw new NotSupportedException($"Stage type {request.StageType} not supported");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException($"Provider {provider.Name} returned empty content");
        }

        return new LlmStageResponse
        {
            Content = content,
            ProviderName = provider.Name,
            ModelName = provider.Model
        };
    }

    protected override async Task<string?> GetCacheKeyAsync(LlmStageRequest request, CancellationToken ct)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        var keyData = request.StageType switch
        {
            LlmStageType.ScriptGeneration => $"script:{request.Brief?.Topic}:{request.Brief?.Tone}:{request.PlanSpec?.TargetDuration}",
            LlmStageType.VisualPrompt => $"visual:{request.SceneText}:{request.VideoTone}:{request.TargetStyle}",
            LlmStageType.RawCompletion => $"completion:{request.Prompt}",
            LlmStageType.ChatCompletion => $"chat:{request.SystemPrompt}:{request.UserPrompt}",
            _ => null
        };

        if (keyData == null)
        {
            return null;
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(keyData));
        return $"llm:{request.StageType}:{Convert.ToHexString(hash)[..16]}";
    }
}

/// <summary>
/// Request for LLM stage operation
/// </summary>
public record LlmStageRequest
{
    public LlmStageType StageType { get; init; }
    
    public Brief? Brief { get; init; }
    public PlanSpec? PlanSpec { get; init; }
    
    public string? SceneText { get; init; }
    public string? PreviousSceneText { get; init; }
    public string? VideoTone { get; init; }
    public VisualStyle? TargetStyle { get; init; }
    
    public string? Prompt { get; init; }
    
    /// <summary>
    /// System prompt for ChatCompletion stage type
    /// </summary>
    public string? SystemPrompt { get; init; }
    
    /// <summary>
    /// User prompt for ChatCompletion stage type
    /// </summary>
    public string? UserPrompt { get; init; }
    
    /// <summary>
    /// Optional LLM parameters for customizing generation
    /// </summary>
    public LlmParameters? LlmParameters { get; init; }
}

/// <summary>
/// Response from LLM stage operation
/// </summary>
public record LlmStageResponse
{
    public string Content { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public string ModelName { get; init; } = string.Empty;
}

/// <summary>
/// Type of LLM stage operation
/// </summary>
public enum LlmStageType
{
    ScriptGeneration,
    VisualPrompt,
    RawCompletion,
    ChatCompletion
}
