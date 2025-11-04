using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Audio;
using Aura.Core.Models.Voice;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Audio;

/// <summary>
/// Service for planning SSML with precise duration targeting
/// </summary>
public class SSMLPlannerService
{
    private readonly ILogger<SSMLPlannerService> _logger;
    private readonly IReadOnlyDictionary<VoiceProvider, ISSMLMapper> _mappers;

    public SSMLPlannerService(
        ILogger<SSMLPlannerService> logger,
        IEnumerable<ISSMLMapper> mappers)
    {
        _logger = logger;
        _mappers = mappers.ToDictionary(m => m.Provider);
    }

    /// <summary>
    /// Plan SSML for script lines with duration targeting
    /// </summary>
    public async Task<SSMLPlanningResult> PlanSSMLAsync(
        SSMLPlanningRequest request,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Planning SSML for {LineCount} lines with provider {Provider}",
            request.ScriptLines.Count, request.TargetProvider);

        if (!_mappers.TryGetValue(request.TargetProvider, out var mapper))
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
                _logger.LogWarning("No target duration for scene {SceneIndex}, using line duration", line.SceneIndex);
                targetDurationSec = line.Duration.TotalSeconds;
            }

            var targetDurationMs = (int)(targetDurationSec * 1000);

            var segmentResult = await FitSegmentDurationAsync(
                line,
                targetDurationMs,
                request,
                mapper,
                ct);

            segments.Add(segmentResult);

            if (segmentResult.Adjustments.Iterations > 0)
            {
                segmentsAdjusted++;
                totalIterations += segmentResult.Adjustments.Iterations;
                maxIterations = Math.Max(maxIterations, segmentResult.Adjustments.Iterations);
            }

            deviations.Add(Math.Abs(segmentResult.DeviationPercent));

            if (Math.Abs(segmentResult.DeviationPercent) > request.DurationTolerance * 100)
            {
                warnings.Add($"Scene {line.SceneIndex}: Duration deviation {segmentResult.DeviationPercent:F1}% exceeds tolerance");
            }

            var validation = mapper.Validate(segmentResult.SsmlMarkup);
            if (!validation.IsValid)
            {
                _logger.LogWarning(
                    "SSML validation failed for scene {SceneIndex}: {Errors}",
                    line.SceneIndex, string.Join("; ", validation.Errors));
                
                var repairedSsml = mapper.AutoRepair(segmentResult.SsmlMarkup);
                var revalidation = mapper.Validate(repairedSsml);
                
                if (revalidation.IsValid)
                {
                    _logger.LogInformation("Auto-repaired SSML for scene {SceneIndex}", line.SceneIndex);
                    segmentResult = segmentResult with { SsmlMarkup = repairedSsml };
                    segments[^1] = segmentResult;
                }
                else
                {
                    warnings.Add($"Scene {line.SceneIndex}: Could not auto-repair SSML validation errors");
                }
            }
        }

        var withinTolerance = deviations.Count(d => d <= request.DurationTolerance * 100);
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

        stopwatch.Stop();

        _logger.LogInformation(
            "SSML planning complete in {ElapsedMs}ms. Within tolerance: {Percent:F1}%, Avg deviation: {AvgDev:F2}%",
            stopwatch.ElapsedMilliseconds, withinTolerancePercent, stats.AverageDeviation);

        return new SSMLPlanningResult
        {
            Segments = segments,
            Stats = stats,
            Warnings = warnings,
            PlanningDurationMs = stopwatch.ElapsedMilliseconds
        };
    }

    /// <summary>
    /// Fit a single segment to target duration
    /// </summary>
    private async Task<SSMLSegmentResult> FitSegmentDurationAsync(
        ScriptLine line,
        int targetDurationMs,
        SSMLPlanningRequest request,
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
        var estimatedDurationMs = await mapper.EstimateDurationAsync(ssml, request.VoiceSpec, ct);

        var toleranceMs = (int)(targetDurationMs * request.DurationTolerance);
        var deviation = estimatedDurationMs - targetDurationMs;
        var iteration = 0;

        while (Math.Abs(deviation) > toleranceMs && iteration < request.MaxFittingIterations)
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
            estimatedDurationMs = await mapper.EstimateDurationAsync(ssml, request.VoiceSpec, ct);
            deviation = estimatedDurationMs - targetDurationMs;

            _logger.LogDebug(
                "Scene {SceneIndex} iteration {Iteration}: Estimated {EstMs}ms, Target {TargetMs}ms, Deviation {DevMs}ms",
                line.SceneIndex, iteration, estimatedDurationMs, targetDurationMs, deviation);

            if (iteration >= request.MaxFittingIterations)
            {
                _logger.LogWarning(
                    "Scene {SceneIndex} did not converge after {MaxIterations} iterations (deviation: {DevMs}ms)",
                    line.SceneIndex, request.MaxFittingIterations, deviation);
                break;
            }
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

    /// <summary>
    /// Adjust rate aggressively for large deviations (>50%)
    /// </summary>
    private ProsodyAdjustments AdjustRateAggressively(
        ProsodyAdjustments current,
        double deviationPercent,
        ProviderSSMLConstraints constraints)
    {
        var targetRate = current.Rate / (1.0 + deviationPercent);
        targetRate = Math.Clamp(targetRate, constraints.RateRange.Min, constraints.RateRange.Max);

        return current with { Rate = targetRate };
    }

    /// <summary>
    /// Adjust rate moderately for medium deviations (10-50%)
    /// </summary>
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

    /// <summary>
    /// Adjust with pauses for fine-tuning (less than 10% deviation)
    /// </summary>
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
            else
            {
                var commas = text.Split(',');
                if (commas.Length > 1)
                {
                    var pausePerComma = deviationMs / (commas.Length - 1);
                    var position = 0;
                    
                    foreach (var part in commas.Take(commas.Length - 1))
                    {
                        position += part.Length + 1;
                        pauses[position] = Math.Min(pausePerComma, 1000);
                    }
                }
            }
        }
        else
        {
            var pausePositions = pauses.Keys.OrderBy(p => p).ToList();
            var remainingToReduce = -deviationMs;
            
            foreach (var pos in pausePositions)
            {
                if (remainingToReduce <= 0)
                {
                    break;
                }
                
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
}
