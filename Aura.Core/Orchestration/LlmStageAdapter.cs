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

    public LlmStageAdapter(
        ILogger<LlmStageAdapter> logger,
        Dictionary<string, ILlmProvider> providers,
        ProviderMixer providerMixer,
        ILlmCache? cache = null,
        SchemaValidator? schemaValidator = null,
        EnhancedCostTrackingService? costTrackingService = null,
        TokenTrackingService? tokenTrackingService = null)
        : base(logger, cache, schemaValidator, costTrackingService, tokenTrackingService)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _providerMixer = providerMixer ?? throw new ArgumentNullException(nameof(providerMixer));
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

        var result = await ExecuteAsync(request, config, ct);

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

        var result = await ExecuteAsync(request, config, ct);

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

    protected override string GetStageName() => "LLM";

    protected override async Task<ProviderInfo[]> GetProvidersAsync(
        OrchestrationConfig config,
        CancellationToken ct)
    {
        await Task.CompletedTask;

        var preferredTier = config.PreferredTier ?? "Free";
        var offlineOnly = config.OfflineOnly;

        var decision = _providerMixer.ResolveLlm(_providers, preferredTier, offlineOnly);
        _providerMixer.LogDecision(decision);

        if (decision.ProviderName == "None")
        {
            return Array.Empty<ProviderInfo>();
        }

        var providerInfos = new List<ProviderInfo>();
        
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

        if (providerInfos.Count == 0 && _providers.TryGetValue("RuleBased", out var value))
        {
            providerInfos.Add(new ProviderInfo(
                "RuleBased",
                "default",
                0,
value));
        }

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
                content = await llmProvider.DraftScriptAsync(request.Brief, request.PlanSpec, ct);
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
                    ct);
                
                content = visualResult != null 
                    ? JsonSerializer.Serialize(visualResult) 
                    : throw new InvalidOperationException("Visual prompt generation returned null");
                break;

            case LlmStageType.RawCompletion:
                if (request.Prompt == null)
                {
                    throw new InvalidOperationException("Prompt required for raw completion");
                }
                content = await llmProvider.CompleteAsync(request.Prompt, ct);
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
        await Task.CompletedTask;

        var keyData = request.StageType switch
        {
            LlmStageType.ScriptGeneration => $"script:{request.Brief?.Topic}:{request.Brief?.Tone}:{request.PlanSpec?.TargetDuration}",
            LlmStageType.VisualPrompt => $"visual:{request.SceneText}:{request.VideoTone}:{request.TargetStyle}",
            LlmStageType.RawCompletion => $"completion:{request.Prompt}",
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
    RawCompletion
}
