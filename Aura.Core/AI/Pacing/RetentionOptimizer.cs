using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.AI.Pacing;

/// <summary>
/// Optimizes video content for viewer retention using attention curve prediction.
/// Based on YouTube analytics research and engagement patterns.
/// </summary>
public class RetentionOptimizer
{
    private readonly ILogger<RetentionOptimizer> _logger;

    public RetentionOptimizer(ILogger<RetentionOptimizer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Predicts viewer retention throughout the video.
    /// </summary>
    public async Task<RetentionPrediction> PredictRetentionAsync(
        IReadOnlyList<Scene> scenes,
        PacingAnalysisResult? pacingAnalysis,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Predicting viewer retention for {SceneCount} scenes", scenes.Count);

        try
        {
            await Task.Delay(50, ct).ConfigureAwait(false); // Simulate ML processing

            var segments = AnalyzeRetentionBySegment(scenes, pacingAnalysis);
            var overallScore = CalculateOverallRetention(segments);
            var dropPoints = IdentifyDropRiskPoints(segments);

            var prediction = new RetentionPrediction(
                overallScore,
                segments,
                dropPoints
            );

            _logger.LogInformation("Retention prediction complete. Overall score: {Score:F2}", overallScore);

            return prediction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting retention");
            throw;
        }
    }

    /// <summary>
    /// Generates an attention curve showing predicted engagement over time.
    /// </summary>
    public async Task<AttentionCurve> GenerateAttentionCurveAsync(
        IReadOnlyList<Scene> scenes,
        TimeSpan videoDuration,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Generating attention curve for {Duration}", videoDuration);

        await Task.Delay(50, ct).ConfigureAwait(false);

        var points = GenerateAttentionPoints(scenes, videoDuration);
        var averageEngagement = points.Average(p => p.AttentionLevel);
        var criticalDrops = IdentifyCriticalDrops(points);

        return new AttentionCurve(points, averageEngagement, criticalDrops);
    }

    /// <summary>
    /// Optimizes scene order and duration for maximum retention.
    /// </summary>
    public List<Scene> OptimizeForRetention(
        IReadOnlyList<Scene> scenes,
        VideoFormat format)
    {
        _logger.LogInformation("Optimizing scene order for retention (format: {Format})", format);

        var optimized = new List<Scene>(scenes);

        // Ensure hook is first and compelling
        if (optimized.Count > 0)
        {
            var hook = optimized[0];
            if (hook.Duration.TotalSeconds > 15)
            {
                // Shorten hook for better engagement
                optimized[0] = hook with { Duration = TimeSpan.FromSeconds(12) };
                _logger.LogDebug("Shortened hook from {Original}s to 12s", hook.Duration.TotalSeconds);
            }
        }

        // Apply format-specific optimizations
        switch (format)
        {
            case VideoFormat.Entertainment:
            case VideoFormat.Vlog:
                // Fast-paced formats: ensure no scene exceeds 20 seconds
                for (int i = 0; i < optimized.Count; i++)
                {
                    if (optimized[i].Duration.TotalSeconds > 20)
                    {
                        optimized[i] = optimized[i] with { Duration = TimeSpan.FromSeconds(18) };
                    }
                }
                break;

            case VideoFormat.Tutorial:
            case VideoFormat.Educational:
                // Instructional formats: allow longer scenes but ensure variety
                EnsurePacingVariety(optimized);
                break;
        }

        return optimized;
    }

    /// <summary>
    /// Analyzes retention for each segment of the video.
    /// </summary>
    private List<RetentionSegment> AnalyzeRetentionBySegment(
        IReadOnlyList<Scene> scenes,
        PacingAnalysisResult? pacingAnalysis)
    {
        var segments = new List<RetentionSegment>();
        var currentTime = TimeSpan.Zero;

        for (int i = 0; i < scenes.Count; i++)
        {
            var scene = scenes[i];
            var retention = PredictSceneRetention(scene, i, scenes.Count, pacingAnalysis);
            var riskFactors = IdentifyRiskFactors(scene, i, scenes.Count);

            segments.Add(new RetentionSegment(
                currentTime,
                currentTime + scene.Duration,
                retention,
                riskFactors
            ));

            currentTime += scene.Duration;
        }

        return segments;
    }

    /// <summary>
    /// Predicts retention for a specific scene.
    /// </summary>
    private double PredictSceneRetention(
        Scene scene,
        int sceneIndex,
        int totalScenes,
        PacingAnalysisResult? pacingAnalysis)
    {
        // Base retention starts high and decays over time
        var positionFactor = 1.0 - (sceneIndex / (double)totalScenes * 0.4);

        // First scene (hook) should have highest retention
        if (sceneIndex == 0)
        {
            return scene.Duration.TotalSeconds <= 15 ? 0.95 : 0.85;
        }

        // Factor in scene duration (longer scenes = higher drop risk)
        var durationFactor = Math.Max(0.5, 1.0 - (scene.Duration.TotalSeconds / 60.0));

        // Factor in pacing quality if available
        var pacingFactor = 1.0;
        if (pacingAnalysis != null)
        {
            var recommendation = pacingAnalysis.SceneRecommendations.FirstOrDefault(r => r.SceneIndex == sceneIndex);
            if (recommendation != null)
            {
                // Better retention if current duration is close to recommended
                var durationDiff = Math.Abs((scene.Duration - recommendation.RecommendedDuration).TotalSeconds);
                pacingFactor = Math.Max(0.7, 1.0 - (durationDiff / 30.0));
            }
        }

        var baseRetention = positionFactor * durationFactor * pacingFactor;

        // Add variance
        var random = new Random(scene.Index);
        var variance = (random.NextDouble() - 0.5) * 0.1;

        return Math.Max(0.4, Math.Min(1.0, baseRetention + variance));
    }

    /// <summary>
    /// Identifies risk factors for viewer drop-off.
    /// </summary>
    private string IdentifyRiskFactors(Scene scene, int sceneIndex, int totalScenes)
    {
        var risks = new List<string>();

        if (scene.Duration.TotalSeconds > 40)
            risks.Add("Long duration");

        if (sceneIndex > totalScenes * 0.7)
            risks.Add("Late in video (natural drop-off)");

        if (CountWords(scene.Script) > 150)
            risks.Add("High word density");

        if (CountWords(scene.Script) < 20)
            risks.Add("Low content density");

        return risks.Any() ? string.Join(", ", risks) : "None identified";
    }

    /// <summary>
    /// Calculates overall retention score from segments.
    /// </summary>
    private double CalculateOverallRetention(IReadOnlyList<RetentionSegment> segments)
    {
        if (segments.Count == 0)
            return 0.7; // Default

        // Weight earlier segments more heavily (first 30 seconds are critical)
        var weighted = segments.Select((s, i) =>
        {
            var weight = Math.Max(0.5, 1.0 - (i / (double)segments.Count * 0.5));
            return s.PredictedRetention * weight;
        });

        return weighted.Average();
    }

    /// <summary>
    /// Identifies points where retention is likely to drop significantly.
    /// </summary>
    private List<TimeSpan> IdentifyDropRiskPoints(IReadOnlyList<RetentionSegment> segments)
    {
        var dropPoints = new List<TimeSpan>();

        for (int i = 1; i < segments.Count; i++)
        {
            var current = segments[i];
            var previous = segments[i - 1];

            // Identify sharp drops in retention (>15%)
            if (previous.PredictedRetention - current.PredictedRetention > 0.15)
            {
                dropPoints.Add(current.Start);
            }

            // Identify segments with very low retention
            if (current.PredictedRetention < 0.6)
            {
                dropPoints.Add(current.Start);
            }
        }

        return dropPoints.Distinct().OrderBy(t => t).ToList();
    }

    /// <summary>
    /// Generates attention points throughout the video.
    /// </summary>
    private List<AttentionPoint> GenerateAttentionPoints(
        IReadOnlyList<Scene> scenes,
        TimeSpan videoDuration)
    {
        var points = new List<AttentionPoint>();
        var sampleInterval = TimeSpan.FromSeconds(5); // Sample every 5 seconds
        var currentTime = TimeSpan.Zero;

        while (currentTime <= videoDuration)
        {
            var attention = CalculateAttentionAtTime(currentTime, videoDuration, scenes);
            points.Add(new AttentionPoint(currentTime, attention));
            currentTime += sampleInterval;
        }

        return points;
    }

    /// <summary>
    /// Calculates predicted attention level at a specific time.
    /// </summary>
    private double CalculateAttentionAtTime(
        TimeSpan timestamp,
        TimeSpan totalDuration,
        IReadOnlyList<Scene> scenes)
    {
        // Natural decay over time
        var progress = timestamp.TotalSeconds / totalDuration.TotalSeconds;
        var decayFactor = 1.0 - (progress * 0.3);

        // First 15 seconds should have highest attention
        if (timestamp.TotalSeconds <= 15)
        {
            return 0.9 + (timestamp.TotalSeconds / 15.0 * 0.1);
        }

        // Find which scene we're in
        var currentTime = TimeSpan.Zero;
        foreach (var scene in scenes)
        {
            if (timestamp >= currentTime && timestamp < currentTime + scene.Duration)
            {
                // Attention slightly higher at scene starts (new content)
                var sceneProgress = (timestamp - currentTime).TotalSeconds / scene.Duration.TotalSeconds;
                var sceneBonus = (1.0 - sceneProgress) * 0.1;
                return Math.Max(0.4, decayFactor + sceneBonus);
            }
            currentTime += scene.Duration;
        }

        return Math.Max(0.4, decayFactor);
    }

    /// <summary>
    /// Identifies critical drop points in attention curve.
    /// </summary>
    private List<TimeSpan> IdentifyCriticalDrops(IReadOnlyList<AttentionPoint> points)
    {
        var drops = new List<TimeSpan>();

        for (int i = 1; i < points.Count; i++)
        {
            var current = points[i];
            var previous = points[i - 1];

            // Identify significant drops (>0.2 attention level)
            if (previous.AttentionLevel - current.AttentionLevel > 0.2)
            {
                drops.Add(current.Timestamp);
            }
        }

        return drops;
    }

    /// <summary>
    /// Ensures variety in pacing to prevent monotony.
    /// </summary>
    private void EnsurePacingVariety(List<Scene> scenes)
    {
        if (scenes.Count < 3)
            return;

        // Check for monotonous durations
        var durations = scenes.Select(s => s.Duration.TotalSeconds).ToList();
        var variance = CalculateVariance(durations);

        if (variance < 3) // Low variance indicates monotony
        {
            _logger.LogDebug("Low pacing variance detected ({Variance:F2}), adjusting for variety", variance);

            // Alternate between slightly shorter and longer scenes
            for (int i = 1; i < scenes.Count - 1; i++)
            {
                if (i % 2 == 0)
                {
                    // Shorten even scenes slightly
                    scenes[i] = scenes[i] with
                    {
                        Duration = TimeSpan.FromSeconds(scenes[i].Duration.TotalSeconds * 0.9)
                    };
                }
                else
                {
                    // Lengthen odd scenes slightly
                    scenes[i] = scenes[i] with
                    {
                        Duration = TimeSpan.FromSeconds(scenes[i].Duration.TotalSeconds * 1.1)
                    };
                }
            }
        }
    }

    /// <summary>
    /// Calculates variance of a set of values.
    /// </summary>
    private double CalculateVariance(IEnumerable<double> values)
    {
        var valuesList = values.ToList();
        if (valuesList.Count < 2)
            return 0;

        var mean = valuesList.Average();
        var squaredDiffs = valuesList.Select(v => Math.Pow(v - mean, 2));
        return Math.Sqrt(squaredDiffs.Average());
    }

    /// <summary>
    /// Counts words in text.
    /// </summary>
    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
