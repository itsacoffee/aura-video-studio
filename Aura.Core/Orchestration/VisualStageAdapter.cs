using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Cache;
using Aura.Core.AI.Validation;
using Aura.Core.Models;
using Aura.Core.Models.Visual;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Core.Services.CostTracking;
using Aura.Core.Services.Visual;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Orchestration;

/// <summary>
/// Stage adapter for visual prompt generation and asset selection
/// Routes through unified orchestrator with prompt governance
/// </summary>
public class VisualStageAdapter : UnifiedGenerationOrchestrator<VisualStageRequest, VisualStageResponse>
{
    private readonly Dictionary<string, ILlmProvider> _llmProviders;
    private readonly ProviderMixer _providerMixer;
    private readonly PromptOptimizer _promptOptimizer;
    private readonly CinematographyKnowledgeBase _cinematography;
    private readonly VisualContinuityEngine _continuityEngine;

    public VisualStageAdapter(
        ILogger<VisualStageAdapter> logger,
        Dictionary<string, ILlmProvider> llmProviders,
        ProviderMixer providerMixer,
        PromptOptimizer promptOptimizer,
        CinematographyKnowledgeBase cinematography,
        VisualContinuityEngine continuityEngine,
        ILlmCache? cache = null,
        SchemaValidator? schemaValidator = null,
        EnhancedCostTrackingService? costTrackingService = null,
        TokenTrackingService? tokenTrackingService = null)
        : base(logger, cache, schemaValidator, costTrackingService, tokenTrackingService)
    {
        _llmProviders = llmProviders ?? throw new ArgumentNullException(nameof(llmProviders));
        _providerMixer = providerMixer ?? throw new ArgumentNullException(nameof(providerMixer));
        _promptOptimizer = promptOptimizer ?? throw new ArgumentNullException(nameof(promptOptimizer));
        _cinematography = cinematography ?? throw new ArgumentNullException(nameof(cinematography));
        _continuityEngine = continuityEngine ?? throw new ArgumentNullException(nameof(continuityEngine));
    }

    /// <summary>
    /// Generate visual prompt with governance and optimization
    /// </summary>
    public async Task<OrchestrationResult<VisualPrompt>> GenerateVisualPromptAsync(
        Scene scene,
        Scene? previousScene,
        string tone,
        VisualStyle visualStyle,
        double importance,
        double emotionalIntensity,
        string preferredTier,
        bool offlineOnly,
        CancellationToken ct)
    {
        var request = new VisualStageRequest
        {
            Scene = scene,
            PreviousScene = previousScene,
            Tone = tone,
            VisualStyle = visualStyle,
            Importance = importance,
            EmotionalIntensity = emotionalIntensity
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
            return OrchestrationResult<VisualPrompt>.Failure(
                result.OperationId,
                result.ElapsedMs,
                result.ErrorMessage ?? "Visual prompt generation failed");
        }

        return OrchestrationResult<VisualPrompt>.Success(
            result.Data.Prompt,
            result.OperationId,
            result.ElapsedMs,
            result.WasCached,
            result.ProviderUsed);
    }

    protected override string GetStageName() => "Visual";

    protected override async Task<ProviderInfo[]> GetProvidersAsync(
        OrchestrationConfig config,
        CancellationToken ct)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        var preferredTier = config.PreferredTier ?? "Free";
        var offlineOnly = config.OfflineOnly;

        var decision = _providerMixer.ResolveLlm(_llmProviders, preferredTier, offlineOnly);
        _providerMixer.LogDecision(decision);

        if (decision.ProviderName == "None")
        {
            return Array.Empty<ProviderInfo>();
        }

        var providerInfos = new List<ProviderInfo>();
        
        foreach (var providerName in decision.DowngradeChain)
        {
            if (_llmProviders.TryGetValue(providerName, out var provider))
            {
                providerInfos.Add(new ProviderInfo(
                    providerName,
                    "default",
                    providerInfos.Count,
                    provider));
            }
        }

        if (providerInfos.Count == 0 && _llmProviders.TryGetValue("RuleBased", out var value))
        {
            providerInfos.Add(new ProviderInfo(
                "RuleBased",
                "default",
                0,
value));
        }

        return providerInfos.ToArray();
    }

    protected override async Task<VisualStageResponse> ExecuteProviderAsync(
        ProviderInfo provider,
        VisualStageRequest request,
        OrchestrationConfig config,
        CancellationToken ct)
    {
        var llmProvider = (ILlmProvider)provider.Implementation;

        Logger.LogInformation(
            "Generating visual prompt for scene {SceneIndex} with provider {Provider}",
            request.Scene.Index, provider.Name);

        var visualResult = await llmProvider.GenerateVisualPromptAsync(
            request.Scene.Script,
            request.PreviousScene?.Script,
            request.Tone,
            request.VisualStyle,
            ct).ConfigureAwait(false);

        if (visualResult == null)
        {
            throw new InvalidOperationException("Visual prompt generation returned null");
        }

        var visualPrompt = new VisualPrompt
        {
            SceneIndex = request.Scene.Index,
            DetailedDescription = visualResult.DetailedDescription,
            CompositionGuidelines = visualResult.CompositionGuidelines,
            StyleKeywords = visualResult.StyleKeywords.ToList(),
            ImportanceScore = request.Importance,
            Lighting = new LightingSetup
            {
                Mood = visualResult.LightingMood,
                Direction = visualResult.LightingDirection,
                Quality = visualResult.LightingQuality,
                TimeOfDay = visualResult.TimeOfDay
            },
            Camera = new CameraSetup
            {
                ShotType = Enum.TryParse<ShotType>(visualResult.ShotType, true, out var shotType) 
                    ? shotType : ShotType.MediumShot,
                Angle = Enum.TryParse<CameraAngle>(visualResult.CameraAngle, true, out var angle) 
                    ? angle : CameraAngle.EyeLevel,
                DepthOfField = visualResult.DepthOfField
            },
            ColorPalette = visualResult.ColorPalette.ToList(),
            NegativeElements = visualResult.NegativeElements.ToList(),
            Reasoning = visualResult.Reasoning,
            Style = request.VisualStyle
        };

        return new VisualStageResponse
        {
            Prompt = visualPrompt,
            ProviderName = provider.Name
        };
    }

    protected override async Task<string?> GetCacheKeyAsync(VisualStageRequest request, CancellationToken ct)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        var keyData = $"visual:{request.Scene.Script}:{request.Tone}:{request.VisualStyle}:{request.Importance}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(keyData));
        return $"visual:{Convert.ToHexString(hash)[..16]}";
    }
}

/// <summary>
/// Request for visual stage operation
/// </summary>
public record VisualStageRequest
{
    public Scene Scene { get; init; } = null!;
    public Scene? PreviousScene { get; init; }
    public string Tone { get; init; } = string.Empty;
    public VisualStyle VisualStyle { get; init; }
    public double Importance { get; init; }
    public double EmotionalIntensity { get; init; }
}

/// <summary>
/// Response from visual stage operation
/// </summary>
public record VisualStageResponse
{
    public VisualPrompt Prompt { get; init; } = null!;
    public string ProviderName { get; init; } = string.Empty;
}
