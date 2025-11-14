using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.FrameAnalysis;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.VideoOptimization;

/// <summary>
/// ML-based service for predicting viewer attention and engagement drops
/// </summary>
public class AttentionPredictionService
{
    private readonly ILogger<AttentionPredictionService> _logger;

    public AttentionPredictionService(ILogger<AttentionPredictionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Predicts viewer attention levels throughout the video
    /// </summary>
    public async Task<AttentionPrediction> PredictAttentionAsync(
        List<Scene> scenes,
        PredictionOptions options,
        CancellationToken cancellationToken = default)
    {
        if (scenes == null)
        {
            throw new ArgumentNullException(nameof(scenes));
        }

        _logger.LogInformation("Predicting attention for {SceneCount} scenes", scenes.Count);

        if (scenes.Count == 0)
        {
            throw new ArgumentException("Scenes collection cannot be empty", nameof(scenes));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var predictions = new List<AttentionDataPoint>();
        var engagementDrops = new List<EngagementDrop>();
        
        double cumulativeTime = 0;
        
        for (int i = 0; i < scenes.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var scene = scenes[i];
            var sceneAttention = await PredictSceneAttentionAsync(scene, i, scenes.Count, options, cancellationToken).ConfigureAwait(false);
            
            // Create data points for the scene
            var sceneDataPoints = GenerateSceneDataPoints(scene, sceneAttention, cumulativeTime);
            predictions.AddRange(sceneDataPoints);
            
            // Detect engagement drops
            if (sceneAttention.PredictedEngagement < options.EngagementDropThreshold)
            {
                engagementDrops.Add(new EngagementDrop(
                    Timestamp: TimeSpan.FromSeconds(cumulativeTime),
                    SceneIndex: i,
                    PredictedEngagement: sceneAttention.PredictedEngagement,
                    Severity: CalculateSeverity(sceneAttention.PredictedEngagement),
                    Recommendation: GenerateDropRecommendation(scene, sceneAttention)
                ));
            }
            
            cumulativeTime += scene.Duration.TotalSeconds;
        }

        var overallEngagement = predictions.Average(p => p.EngagementLevel);
        var retentionRate = CalculateRetentionRate(predictions);

        return new AttentionPrediction(
            DataPoints: predictions,
            EngagementDrops: engagementDrops,
            OverallEngagement: overallEngagement,
            PredictedRetentionRate: retentionRate,
            HighRiskSegments: engagementDrops.Where(d => d.Severity >= DropSeverity.High).ToList()
        );
    }

    private async Task<SceneAttentionPrediction> PredictSceneAttentionAsync(
        Scene scene,
        int sceneIndex,
        int totalScenes,
        PredictionOptions options,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        // Extract features for ML prediction
        var features = ExtractFeatures(scene, sceneIndex, totalScenes);
        
        // Placeholder ML prediction - in production would use ML.NET model
        var engagement = PredictEngagement(features, options);
        var attentionScore = CalculateAttentionScore(features, engagement);

        return new SceneAttentionPrediction(
            SceneIndex: sceneIndex,
            PredictedEngagement: engagement,
            AttentionScore: attentionScore,
            Features: features
        );
    }

    private SceneFeatures ExtractFeatures(Scene scene, int sceneIndex, int totalScenes)
    {
        var wordCount = scene.Script.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var wordsPerSecond = wordCount / scene.Duration.TotalSeconds;
        var relativePosition = (double)sceneIndex / totalScenes;
        var sceneLengthSeconds = scene.Duration.TotalSeconds;

        // Extract linguistic complexity (simple heuristic)
        var avgWordLength = scene.Script.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Average(w => w.Length);

        return new SceneFeatures(
            WordsPerSecond: wordsPerSecond,
            SceneLengthSeconds: sceneLengthSeconds,
            RelativePosition: relativePosition,
            WordCount: wordCount,
            AverageWordLength: avgWordLength,
            IsOpening: sceneIndex == 0,
            IsClosing: sceneIndex == totalScenes - 1
        );
    }

    private double PredictEngagement(SceneFeatures features, PredictionOptions options)
    {
        // Simplified engagement prediction model
        // In production, this would use a trained ML.NET model
        
        var engagementScore = 0.7; // Base engagement
        
        // Opening scenes typically have higher engagement
        if (features.IsOpening)
            engagementScore += 0.15;
        
        // Closing scenes often have good engagement
        if (features.IsClosing)
            engagementScore += 0.1;
        
        // Middle sections tend to have lower engagement
        if (features.RelativePosition > 0.3 && features.RelativePosition < 0.7)
            engagementScore -= 0.15;
        
        // Too slow pacing reduces engagement
        if (features.WordsPerSecond < 1.5)
            engagementScore -= 0.2;
        
        // Too fast pacing also reduces engagement
        if (features.WordsPerSecond > 3.5)
            engagementScore -= 0.15;
        
        // Very long scenes lose attention
        if (features.SceneLengthSeconds > 30)
            engagementScore -= 0.1;
        
        // Very short scenes might feel rushed
        if (features.SceneLengthSeconds < 3)
            engagementScore -= 0.05;

        return Math.Clamp(engagementScore, 0.0, 1.0);
    }

    private double CalculateAttentionScore(SceneFeatures features, double engagement)
    {
        // Attention score combines engagement with other factors
        var attentionBase = engagement;
        
        // Optimal pacing (2-3 words/second) maintains attention better
        var pacingOptimality = 1.0 - Math.Abs(features.WordsPerSecond - 2.5) / 2.5;
        pacingOptimality = Math.Clamp(pacingOptimality, 0.0, 1.0);
        
        return (attentionBase * 0.7 + pacingOptimality * 0.3);
    }

    private List<AttentionDataPoint> GenerateSceneDataPoints(
        Scene scene,
        SceneAttentionPrediction sceneAttention,
        double cumulativeTime)
    {
        var dataPoints = new List<AttentionDataPoint>();
        
        // Generate multiple data points throughout the scene for granular visualization
        var samplesPerScene = Math.Max(3, (int)(scene.Duration.TotalSeconds / 5)); // Sample every 5 seconds
        
        for (int i = 0; i <= samplesPerScene; i++)
        {
            var progress = (double)i / samplesPerScene;
            var timestamp = cumulativeTime + (scene.Duration.TotalSeconds * progress);
            
            // Engagement may vary slightly within a scene
            var variance = (Math.Sin(i * 0.5) * 0.05);
            var engagement = Math.Clamp(sceneAttention.PredictedEngagement + variance, 0.0, 1.0);
            
            dataPoints.Add(new AttentionDataPoint(
                Timestamp: TimeSpan.FromSeconds(timestamp),
                EngagementLevel: engagement,
                AttentionScore: sceneAttention.AttentionScore,
                SceneIndex: sceneAttention.SceneIndex
            ));
        }

        return dataPoints;
    }

    private double CalculateRetentionRate(List<AttentionDataPoint> predictions)
    {
        // Model viewer retention based on engagement over time
        var retentionFactors = new List<double>();
        
        foreach (var point in predictions)
        {
            // Lower engagement = higher likelihood of drop-off
            var retentionAtPoint = Math.Pow(point.EngagementLevel, 1.5);
            retentionFactors.Add(retentionAtPoint);
        }

        return retentionFactors.Average();
    }

    private DropSeverity CalculateSeverity(double engagement)
    {
        return engagement switch
        {
            < 0.3 => DropSeverity.Critical,
            < 0.5 => DropSeverity.High,
            < 0.6 => DropSeverity.Medium,
            _ => DropSeverity.Low
        };
    }

    private string GenerateDropRecommendation(Scene scene, SceneAttentionPrediction prediction)
    {
        var recommendations = new List<string>();

        if (prediction.Features.WordsPerSecond < 1.5)
            recommendations.Add("increase pacing by shortening scene duration or adding more content");
        
        if (prediction.Features.WordsPerSecond > 3.5)
            recommendations.Add("slow down pacing to improve comprehension");
        
        if (prediction.Features.SceneLengthSeconds > 30)
            recommendations.Add("consider splitting into multiple shorter scenes");
        
        if (prediction.Features.RelativePosition > 0.3 && prediction.Features.RelativePosition < 0.7)
            recommendations.Add("middle sections benefit from dynamic content or visual variety");

        return recommendations.Count > 0
            ? string.Join("; ", recommendations)
            : "consider adding more engaging content or varying the pacing";
    }
}

/// <summary>
/// Options for attention prediction
/// </summary>
public record PredictionOptions(
    double EngagementDropThreshold = 0.6,
    bool IncludeDetailedMetrics = true
);

/// <summary>
/// Prediction result for viewer attention
/// </summary>
public record AttentionPrediction(
    List<AttentionDataPoint> DataPoints,
    List<EngagementDrop> EngagementDrops,
    double OverallEngagement,
    double PredictedRetentionRate,
    List<EngagementDrop> HighRiskSegments
);

/// <summary>
/// Data point representing attention at a specific timestamp
/// </summary>
public record AttentionDataPoint(
    TimeSpan Timestamp,
    double EngagementLevel,
    double AttentionScore,
    int SceneIndex
);

/// <summary>
/// Represents a detected engagement drop
/// </summary>
public record EngagementDrop(
    TimeSpan Timestamp,
    int SceneIndex,
    double PredictedEngagement,
    DropSeverity Severity,
    string Recommendation
);

/// <summary>
/// Scene attention prediction
/// </summary>
public record SceneAttentionPrediction(
    int SceneIndex,
    double PredictedEngagement,
    double AttentionScore,
    SceneFeatures Features
);

/// <summary>
/// Features extracted from a scene for ML prediction
/// </summary>
public record SceneFeatures(
    double WordsPerSecond,
    double SceneLengthSeconds,
    double RelativePosition,
    int WordCount,
    double AverageWordLength,
    bool IsOpening,
    bool IsClosing
);

/// <summary>
/// Severity of an engagement drop
/// </summary>
public enum DropSeverity
{
    Low,
    Medium,
    High,
    Critical
}
