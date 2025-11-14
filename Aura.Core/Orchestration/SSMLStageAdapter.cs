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
using Aura.Core.Models.Audio;
using Aura.Core.Models.Voice;
using Aura.Core.Providers;
using Aura.Core.Services.Audio;
using Aura.Core.Services.CostTracking;
using Microsoft.Extensions.Logging;
using CoreValidationResult = Aura.Core.Validation.ValidationResult;

namespace Aura.Core.Orchestration;

/// <summary>
/// Stage adapter for SSML/TTS operations with duration-fit loop
/// Routes through unified orchestrator with validation
/// </summary>
public class SSMLStageAdapter : UnifiedGenerationOrchestrator<SSMLStageRequest, SSMLStageResponse>
{
    private readonly Dictionary<VoiceProvider, ITtsProvider> _ttsProviders;
    private readonly Dictionary<VoiceProvider, ISSMLMapper> _ssmlMappers;

    public SSMLStageAdapter(
        ILogger<SSMLStageAdapter> logger,
        IEnumerable<ITtsProvider> ttsProviders,
        IEnumerable<ISSMLMapper> ssmlMappers,
        ILlmCache? cache = null,
        SchemaValidator? schemaValidator = null,
        EnhancedCostTrackingService? costTrackingService = null,
        TokenTrackingService? tokenTrackingService = null)
        : base(logger, cache, schemaValidator, costTrackingService, tokenTrackingService)
    {
        _ttsProviders = new Dictionary<VoiceProvider, ITtsProvider>();
        _ssmlMappers = ssmlMappers?.ToDictionary(m => m.Provider) 
            ?? throw new ArgumentNullException(nameof(ssmlMappers));
    }

    /// <summary>
    /// Generate SSML and synthesize speech with duration targeting
    /// </summary>
    public async Task<OrchestrationResult<SSMLPlanningResult>> GenerateSSMLAsync(
        IReadOnlyList<ScriptLine> scriptLines,
        VoiceSpec voiceSpec,
        IReadOnlyDictionary<int, double> targetDurations,
        VoiceProvider targetProvider,
        CancellationToken ct)
    {
        var request = new SSMLStageRequest
        {
            ScriptLines = scriptLines,
            VoiceSpec = voiceSpec,
            TargetDurations = targetDurations,
            TargetProvider = targetProvider
        };

        var config = new OrchestrationConfig
        {
            EnableCache = true,
            ValidateSchema = true,
            MaxRetries = 2
        };

        var result = await ExecuteAsync(request, config, ct).ConfigureAwait(false);

        if (!result.IsSuccess || result.Data == null)
        {
            return OrchestrationResult<SSMLPlanningResult>.Failure(
                result.OperationId,
                result.ElapsedMs,
                result.ErrorMessage ?? "SSML generation failed");
        }

        return OrchestrationResult<SSMLPlanningResult>.Success(
            result.Data.PlanningResult,
            result.OperationId,
            result.ElapsedMs,
            result.WasCached,
            result.ProviderUsed);
    }

    protected override string GetStageName() => "SSML/TTS";

    protected override async Task<ProviderInfo[]> GetProvidersAsync(
        OrchestrationConfig config,
        CancellationToken ct)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        var providers = new List<ProviderInfo>();
        int priority = 0;

        foreach (var kvp in _ttsProviders.OrderBy(p => p.Key))
        {
            providers.Add(new ProviderInfo(
                kvp.Key.ToString(),
                "default",
                priority++,
                kvp.Value));
        }

        return providers.ToArray();
    }

    protected override async Task<SSMLStageResponse> ExecuteProviderAsync(
        ProviderInfo provider,
        SSMLStageRequest request,
        OrchestrationConfig config,
        CancellationToken ct)
    {
        if (!_ssmlMappers.TryGetValue(request.TargetProvider, out var mapper))
        {
            throw new InvalidOperationException($"No SSML mapper found for provider {request.TargetProvider}");
        }

        var segments = new List<SSMLSegmentResult>();
        var warnings = new List<string>();
        var stats = new DurationFittingStats();

        var totalIterations = 0;
        var maxIterations = 0;
        var segmentsAdjusted = 0;
        var deviations = new List<double>();

        foreach (var line in request.ScriptLines)
        {
            ct.ThrowIfCancellationRequested();

            if (!request.TargetDurations.TryGetValue(line.SceneIndex, out var targetDurationSec))
            {
                Logger.LogWarning("No target duration for scene {SceneIndex}, using line duration", line.SceneIndex);
                targetDurationSec = line.Duration.TotalSeconds;
            }

            var targetDurationMs = (int)(targetDurationSec * 1000);

            var segmentResult = await FitSegmentDurationAsync(
                line,
                targetDurationMs,
                request,
                mapper,
                ct).ConfigureAwait(false);

            segments.Add(segmentResult);

            if (segmentResult.Adjustments.Iterations > 0)
            {
                segmentsAdjusted++;
                totalIterations += segmentResult.Adjustments.Iterations;
                maxIterations = Math.Max(maxIterations, segmentResult.Adjustments.Iterations);
            }

            deviations.Add(Math.Abs(segmentResult.DeviationPercent));

            var tolerance = 0.05;
            if (Math.Abs(segmentResult.DeviationPercent) > tolerance * 100)
            {
                warnings.Add($"Scene {line.SceneIndex}: Duration deviation {segmentResult.DeviationPercent:F1}% exceeds tolerance");
            }

            var validation = mapper.Validate(segmentResult.SsmlMarkup);
            if (!validation.IsValid)
            {
                Logger.LogWarning(
                    "SSML validation failed for scene {SceneIndex}: {Errors}",
                    line.SceneIndex, string.Join("; ", validation.Errors));
                
                var repairedSsml = mapper.AutoRepair(segmentResult.SsmlMarkup);
                var revalidation = mapper.Validate(repairedSsml);
                
                if (revalidation.IsValid)
                {
                    Logger.LogInformation("Auto-repaired SSML for scene {SceneIndex}", line.SceneIndex);
                    segmentResult = segmentResult with { SsmlMarkup = repairedSsml };
                    segments[^1] = segmentResult;
                }
                else
                {
                    warnings.Add($"Scene {line.SceneIndex}: Could not auto-repair SSML validation errors");
                }
            }
        }

        var withinTolerance = deviations.Count(d => d <= 0.05 * 100);
        var withinTolerancePercent = (double)withinTolerance / deviations.Count * 100;

        var totalTargetSec = request.TargetDurations.Values.Sum();
        var totalActualSec = segments.Sum(s => s.EstimatedDurationMs) / 1000.0;

        stats = new DurationFittingStats
        {
            SegmentsAdjusted = segmentsAdjusted,
            AverageFitIterations = segmentsAdjusted > 0 ? (double)totalIterations / segmentsAdjusted : 0,
            MaxFitIterations = maxIterations,
            WithinTolerancePercent = withinTolerancePercent,
            AverageDeviation = deviations.Average(),
            MaxDeviation = deviations.Max(),
            TargetDurationSeconds = totalTargetSec,
            ActualDurationSeconds = totalActualSec
        };

        Logger.LogInformation(
            "SSML planning complete. Within tolerance: {Percent:F1}%, Avg deviation: {AvgDev:F2}%",
            withinTolerancePercent, stats.AverageDeviation);

        var planningResult = new SSMLPlanningResult
        {
            Segments = segments,
            Stats = stats,
            Warnings = warnings,
            PlanningDurationMs = 0
        };

        return new SSMLStageResponse
        {
            PlanningResult = planningResult,
            ProviderName = request.TargetProvider.ToString()
        };
    }

    private async Task<SSMLSegmentResult> FitSegmentDurationAsync(
        ScriptLine line,
        int targetDurationMs,
        SSMLStageRequest request,
        ISSMLMapper mapper,
        CancellationToken ct)
    {
        var adjustments = new ProsodyAdjustments
        {
            Rate = 1.0,
            Pitch = 0.0,
            Volume = 1.0,
            Pauses = new Dictionary<int, int>(),
            Emphasis = Array.Empty<EmphasisSpan>(),
            Iterations = 0
        };

        var ssml = mapper.MapToSSML(line.Text, adjustments, request.VoiceSpec);
        var estimatedDurationMs = await mapper.EstimateDurationAsync(ssml, request.VoiceSpec, ct).ConfigureAwait(false);

        var toleranceMs = (int)(targetDurationMs * 0.05);
        var deviation = estimatedDurationMs - targetDurationMs;
        var iteration = 0;
        var maxIterations = 10;

        while (Math.Abs(deviation) > toleranceMs && iteration < maxIterations)
        {
            iteration++;
            ct.ThrowIfCancellationRequested();

            var deviationPercent = (double)deviation / targetDurationMs;
            
            if (Math.Abs(deviationPercent) > 0.5)
            {
                adjustments = AdjustRateAggressively(adjustments, deviationPercent, mapper.GetConstraints());
            }
            else if (Math.Abs(deviationPercent) > 0.1)
            {
                adjustments = AdjustRateModerately(adjustments, deviationPercent, mapper.GetConstraints());
            }
            else
            {
                adjustments = AdjustWithPauses(adjustments, deviation, line.Text);
            }

            adjustments = adjustments with { Iterations = iteration };

            ssml = mapper.MapToSSML(line.Text, adjustments, request.VoiceSpec);
            estimatedDurationMs = await mapper.EstimateDurationAsync(ssml, request.VoiceSpec, ct).ConfigureAwait(false);
            deviation = estimatedDurationMs - targetDurationMs;

            Logger.LogDebug(
                "Scene {SceneIndex} iteration {Iteration}: Estimated {EstMs}ms, Target {TargetMs}ms, Deviation {DevMs}ms",
                line.SceneIndex, iteration, estimatedDurationMs, targetDurationMs, deviation);
        }

        var finalDeviationPercent = ((double)(estimatedDurationMs - targetDurationMs) / targetDurationMs) * 100;

        return new SSMLSegmentResult
        {
            SceneIndex = line.SceneIndex,
            OriginalText = line.Text,
            SsmlMarkup = ssml,
            EstimatedDurationMs = estimatedDurationMs,
            TargetDurationMs = targetDurationMs,
            DeviationPercent = finalDeviationPercent,
            Adjustments = adjustments,
            TimingMarkers = Array.Empty<TimingMarker>()
        };
    }

    private ProsodyAdjustments AdjustRateAggressively(
        ProsodyAdjustments current,
        double deviationPercent,
        ProviderSSMLConstraints constraints)
    {
        var targetRate = current.Rate / (1.0 + deviationPercent);
        targetRate = Math.Clamp(targetRate, constraints.RateRange.Min, constraints.RateRange.Max);
        return current with { Rate = targetRate };
    }

    private ProsodyAdjustments AdjustRateModerately(
        ProsodyAdjustments current,
        double deviationPercent,
        ProviderSSMLConstraints constraints)
    {
        var adjustment = -deviationPercent * 0.5;
        var targetRate = current.Rate * (1.0 + adjustment);
        targetRate = Math.Clamp(targetRate, constraints.RateRange.Min, constraints.RateRange.Max);
        return current with { Rate = targetRate };
    }

    private ProsodyAdjustments AdjustWithPauses(
        ProsodyAdjustments current,
        int deviationMs,
        string text)
    {
        if (deviationMs == 0)
        {
            return current;
        }

        var pauses = new Dictionary<int, int>(current.Pauses);
        
        if (deviationMs > 0)
        {
            var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            if (sentences.Length > 1)
            {
                var pausePerSentence = deviationMs / sentences.Length;
                var position = 0;
                
                foreach (var sentence in sentences.Take(sentences.Length - 1))
                {
                    position += sentence.Length + 1;
                    pauses[position] = Math.Min(pausePerSentence, 2000);
                }
            }
        }
        else
        {
            var pausePositions = pauses.Keys.OrderBy(p => p).ToList();
            var remainingToReduce = -deviationMs;
            
            foreach (var pos in pausePositions)
            {
                if (remainingToReduce <= 0) break;
                
                var currentPause = pauses[pos];
                var reduction = Math.Min(currentPause, remainingToReduce);
                var newPause = currentPause - reduction;
                
                if (newPause > 0)
                {
                    pauses[pos] = newPause;
                }
                else
                {
                    pauses.Remove(pos);
                }
                
                remainingToReduce -= reduction;
            }
        }

        return current with { Pauses = pauses };
    }

    protected override async Task<string?> GetCacheKeyAsync(SSMLStageRequest request, CancellationToken ct)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        var keyData = $"ssml:{request.TargetProvider}:{request.VoiceSpec.VoiceName}:{string.Join(",", request.ScriptLines.Select(l => l.Text))}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(keyData));
        return $"ssml:{Convert.ToHexString(hash)[..16]}";
    }

    protected override async Task<CoreValidationResult> ValidateResponseAsync(
        SSMLStageResponse response,
        CancellationToken ct)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        var errors = new List<string>();

        if (response.PlanningResult == null)
        {
            errors.Add("Planning result is null");
        }
        else
        {
            if (response.PlanningResult.Segments == null || response.PlanningResult.Segments.Count == 0)
            {
                errors.Add("No segments generated");
            }

            if (response.PlanningResult.Stats.WithinTolerancePercent < 50.0)
            {
                errors.Add($"Only {response.PlanningResult.Stats.WithinTolerancePercent:F1}% of segments within tolerance");
            }
        }

        return new CoreValidationResult(errors.Count == 0, errors);
    }
}

/// <summary>
/// Request for SSML stage operation
/// </summary>
public record SSMLStageRequest
{
    public IReadOnlyList<ScriptLine> ScriptLines { get; init; } = Array.Empty<ScriptLine>();
    public VoiceSpec VoiceSpec { get; init; } = null!;
    public IReadOnlyDictionary<int, double> TargetDurations { get; init; } = new Dictionary<int, double>();
    public VoiceProvider TargetProvider { get; init; }
}

/// <summary>
/// Response from SSML stage operation
/// </summary>
public record SSMLStageResponse
{
    public SSMLPlanningResult PlanningResult { get; init; } = null!;
    public string ProviderName { get; init; } = string.Empty;
}
