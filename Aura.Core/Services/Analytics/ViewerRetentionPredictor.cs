using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Pacing;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Analytics;

/// <summary>
/// Predicts viewer retention metrics and provides actionable insights.
/// Integrates pacing analysis with YouTube analytics patterns.
/// </summary>
public class ViewerRetentionPredictor
{
    private readonly ILogger<ViewerRetentionPredictor> _logger;
    private readonly RetentionOptimizer _retentionOptimizer;
    private readonly PacingAnalyzer _pacingAnalyzer;

    public ViewerRetentionPredictor(
        ILogger<ViewerRetentionPredictor> logger,
        RetentionOptimizer retentionOptimizer,
        PacingAnalyzer pacingAnalyzer)
    {
        _logger = logger;
        _retentionOptimizer = retentionOptimizer;
        _pacingAnalyzer = pacingAnalyzer;
    }

    /// <summary>
    /// Analyzes video and predicts comprehensive retention metrics.
    /// </summary>
    public async Task<VideoRetentionAnalysis> AnalyzeRetentionAsync(
        IReadOnlyList<Scene> scenes,
        string? audioPath,
        VideoFormat format,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing retention for {SceneCount} scenes", scenes.Count);

        try
        {
            // Perform pacing analysis
            var pacingAnalysis = await _pacingAnalyzer.AnalyzePacingAsync(
                scenes, audioPath, format, ct
            ).ConfigureAwait(false);

            // Predict retention
            var retentionPrediction = await _retentionOptimizer.PredictRetentionAsync(
                scenes, pacingAnalysis, ct
            ).ConfigureAwait(false);

            // Generate attention curve
            var totalDuration = TimeSpan.FromSeconds(scenes.Sum(s => s.Duration.TotalSeconds));
            var attentionCurve = await _retentionOptimizer.GenerateAttentionCurveAsync(
                scenes, totalDuration, ct
            ).ConfigureAwait(false);

            // Generate recommendations
            var recommendations = GenerateRecommendations(
                scenes, pacingAnalysis, retentionPrediction, attentionCurve
            );

            var analysis = new VideoRetentionAnalysis(
                pacingAnalysis,
                retentionPrediction,
                attentionCurve,
                recommendations
            );

            _logger.LogInformation(
                "Retention analysis complete. Overall retention: {Retention:F2}, Engagement: {Engagement:F2}",
                retentionPrediction.OverallRetentionScore,
                pacingAnalysis.EngagementScore
            );

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing retention");
            throw;
        }
    }

    /// <summary>
    /// Generates specific recommendations for improving retention.
    /// </summary>
    private List<RetentionRecommendation> GenerateRecommendations(
        IReadOnlyList<Scene> scenes,
        PacingAnalysisResult pacingAnalysis,
        RetentionPrediction retentionPrediction,
        AttentionCurve attentionCurve)
    {
        var recommendations = new List<RetentionRecommendation>();

        // Hook optimization
        if (scenes.Count > 0 && scenes[0].Duration.TotalSeconds > 15)
        {
            recommendations.Add(new RetentionRecommendation(
                "Hook Optimization",
                "Shorten opening scene to 10-15 seconds for maximum impact",
                TimeSpan.Zero,
                Priority.High,
                RecommendationType.Hook
            ));
        }

        // Identify drop risk points
        foreach (var dropPoint in retentionPrediction.HighDropRiskPoints.Take(3))
        {
            recommendations.Add(new RetentionRecommendation(
                "Retention Risk",
                "Add B-roll, graphics, or vary pacing to maintain interest",
                dropPoint,
                Priority.Medium,
                RecommendationType.VisualInterest
            ));
        }

        // Low attention segments
        var lowAttentionPoints = attentionCurve.Points
            .Where(p => p.AttentionLevel < 0.6)
            .Take(2)
            .ToList();

        foreach (var point in lowAttentionPoints)
        {
            recommendations.Add(new RetentionRecommendation(
                "Low Engagement Segment",
                "Consider adding emphasis, transitions, or condensing content",
                point.Timestamp,
                Priority.Medium,
                RecommendationType.Pacing
            ));
        }

        // Scene pacing issues
        var problematicScenes = pacingAnalysis.SceneRecommendations
            .Where(r => Math.Abs((r.CurrentDuration - r.RecommendedDuration).TotalSeconds) > 5)
            .Take(3);

        foreach (var scene in problematicScenes)
        {
            var timestamp = scenes.Take(scene.SceneIndex).Sum(s => s.Duration.TotalSeconds);
            var action = scene.CurrentDuration > scene.RecommendedDuration ? "Shorten" : "Extend";

            recommendations.Add(new RetentionRecommendation(
                "Scene Duration Adjustment",
                $"{action} scene to {scene.RecommendedDuration.TotalSeconds:F0}s for optimal pacing",
                TimeSpan.FromSeconds(timestamp),
                Priority.Low,
                RecommendationType.Pacing
            ));
        }

        return recommendations.OrderByDescending(r => r.Priority).ToList();
    }

    /// <summary>
    /// Simulates A/B test comparison between original and optimized versions.
    /// </summary>
    public VideoComparisonMetrics CompareVersions(
        IReadOnlyList<Scene> originalScenes,
        IReadOnlyList<Scene> optimizedScenes,
        VideoFormat format)
    {
        _logger.LogInformation("Comparing original vs optimized versions");

        // Calculate metrics for both versions
        var originalMetrics = CalculateQuickMetrics(originalScenes, format);
        var optimizedMetrics = CalculateQuickMetrics(optimizedScenes, format);

        var improvement = new Dictionary<string, double>
        {
            ["Retention"] = optimizedMetrics.EstimatedRetention - originalMetrics.EstimatedRetention,
            ["Engagement"] = optimizedMetrics.EstimatedEngagement - originalMetrics.EstimatedEngagement,
            ["PacingVariance"] = optimizedMetrics.PacingVariance - originalMetrics.PacingVariance
        };

        return new VideoComparisonMetrics(
            originalMetrics,
            optimizedMetrics,
            improvement
        );
    }

    /// <summary>
    /// Calculates quick metrics without full async analysis.
    /// </summary>
    private QuickMetrics CalculateQuickMetrics(IReadOnlyList<Scene> scenes, VideoFormat format)
    {
        if (scenes.Count == 0)
            return new QuickMetrics(0, 0, 0);

        var durations = scenes.Select(s => s.Duration.TotalSeconds).ToList();
        var avgDuration = durations.Average();
        var variance = CalculateVariance(durations);

        // Simple retention estimate based on hook and total duration
        var hookScore = scenes[0].Duration.TotalSeconds <= 15 ? 0.9 : 0.7;
        var lengthPenalty = Math.Max(0, 1.0 - (durations.Sum() / 300.0)); // Penalty for videos over 5 min
        var estimatedRetention = (hookScore + lengthPenalty) / 2.0;

        // Engagement based on variety and format
        var varietyScore = Math.Min(variance / 10.0, 0.5);
        var formatBonus = format == VideoFormat.Entertainment ? 0.2 : 0.1;
        var estimatedEngagement = Math.Min(varietyScore + formatBonus, 1.0);

        return new QuickMetrics(estimatedRetention, estimatedEngagement, variance);
    }

    /// <summary>
    /// Calculates variance of values.
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
}

/// <summary>
/// Comprehensive retention analysis results.
/// </summary>
public record VideoRetentionAnalysis(
    PacingAnalysisResult PacingAnalysis,
    RetentionPrediction RetentionPrediction,
    AttentionCurve AttentionCurve,
    IReadOnlyList<RetentionRecommendation> Recommendations
);

/// <summary>
/// Specific recommendation for improving retention.
/// </summary>
public record RetentionRecommendation(
    string Title,
    string Description,
    TimeSpan Timestamp,
    Priority Priority,
    RecommendationType Type
);

/// <summary>
/// Priority levels for recommendations.
/// </summary>
public enum Priority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Types of retention recommendations.
/// </summary>
public enum RecommendationType
{
    Hook,
    Pacing,
    VisualInterest,
    ContentDensity,
    Transition
}

/// <summary>
/// Comparison metrics between video versions.
/// </summary>
public record VideoComparisonMetrics(
    QuickMetrics Original,
    QuickMetrics Optimized,
    IReadOnlyDictionary<string, double> Improvements
);

/// <summary>
/// Quick metrics for comparison.
/// </summary>
public record QuickMetrics(
    double EstimatedRetention,
    double EstimatedEngagement,
    double PacingVariance
);
