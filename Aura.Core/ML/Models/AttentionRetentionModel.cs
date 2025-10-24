using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.PacingModels;
using Microsoft.Extensions.Logging;

namespace Aura.Core.ML.Models;

/// <summary>
/// ML model for predicting viewer attention and retention based on scene characteristics
/// </summary>
public class AttentionRetentionModel
{
    private readonly ILogger<AttentionRetentionModel> _logger;
    private readonly string _modelPath;
    private bool _isLoaded;

    public AttentionRetentionModel(
        ILogger<AttentionRetentionModel> logger,
        string modelPath = "ML/PretrainedModels/attention-retention-model.zip")
    {
        _logger = logger;
        _modelPath = modelPath;
        _isLoaded = false;
    }

    /// <summary>
    /// Loads the pre-trained model
    /// </summary>
    public async Task LoadModelAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading attention retention model from {ModelPath}", _modelPath);

        cancellationToken.ThrowIfCancellationRequested();

        // In production, this would load an actual ML.NET model
        // For now, we simulate the load
        await Task.Delay(100, cancellationToken);

        _isLoaded = true;
        _logger.LogInformation("Attention retention model loaded successfully");
    }

    /// <summary>
    /// Predicts engagement score for a scene based on its characteristics
    /// </summary>
    public async Task<double> PredictEngagementAsync(
        Scene scene,
        double importanceScore,
        double complexityScore,
        double emotionalIntensity,
        CancellationToken cancellationToken = default)
    {
        if (!_isLoaded)
        {
            throw new InvalidOperationException("Model must be loaded before prediction");
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Placeholder prediction logic using heuristics
        // In production, this would use ML.NET PredictionEngine
        var score = await Task.FromResult(CalculateHeuristicEngagement(
            scene, importanceScore, complexityScore, emotionalIntensity));

        return score;
    }

    /// <summary>
    /// Predicts attention curve for a series of scenes
    /// </summary>
    public async Task<AttentionCurveData> PredictAttentionCurveAsync(
        IReadOnlyList<Scene> scenes,
        IReadOnlyList<SceneTimingSuggestion> timingSuggestions,
        CancellationToken cancellationToken = default)
    {
        if (!_isLoaded)
        {
            throw new InvalidOperationException("Model must be loaded before prediction");
        }

        _logger.LogInformation("Predicting attention curve for {SceneCount} scenes", scenes.Count);

        var dataPoints = new List<AttentionDataPoint>();
        var peaks = new List<TimeSpan>();
        var valleys = new List<TimeSpan>();

        var currentTime = TimeSpan.Zero;
        var previousAttention = 85.0; // Start with high attention

        for (int i = 0; i < scenes.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var scene = scenes[i];
            var suggestion = timingSuggestions.FirstOrDefault(s => s.SceneIndex == i);

            // Calculate attention for this scene
            var sceneAttention = await CalculateSceneAttentionAsync(
                scene, suggestion, i, scenes.Count, previousAttention, cancellationToken);

            // Generate data points throughout the scene
            var sceneDuration = suggestion?.OptimalDuration ?? scene.Duration;
            var pointCount = Math.Max(3, (int)(sceneDuration.TotalSeconds / 5)); // Point every 5 seconds

            for (int p = 0; p < pointCount; p++)
            {
                var pointTime = currentTime + TimeSpan.FromSeconds(sceneDuration.TotalSeconds * p / pointCount);
                var pointAttention = CalculatePointAttention(sceneAttention, p, pointCount);
                var pointRetention = CalculateRetention(pointAttention, pointTime);
                var pointEngagement = (pointAttention + pointRetention) / 2;

                dataPoints.Add(new AttentionDataPoint
                {
                    Timestamp = pointTime,
                    AttentionLevel = pointAttention,
                    RetentionRate = pointRetention,
                    EngagementScore = pointEngagement
                });

                // Track peaks and valleys
                if (p > 0 && p < pointCount - 1)
                {
                    var prevPoint = dataPoints[^2];
                    if (pointAttention > prevPoint.AttentionLevel && pointAttention > 75)
                    {
                        peaks.Add(pointTime);
                    }
                    else if (pointAttention < prevPoint.AttentionLevel && pointAttention < 50)
                    {
                        valleys.Add(pointTime);
                    }
                }
            }

            currentTime += sceneDuration;
            previousAttention = sceneAttention;
        }

        var avgEngagement = dataPoints.Average(p => p.EngagementScore);
        var overallRetention = dataPoints.Average(p => p.RetentionRate);

        return new AttentionCurveData
        {
            DataPoints = dataPoints,
            AverageEngagement = avgEngagement,
            EngagementPeaks = peaks,
            EngagementValleys = valleys,
            OverallRetentionScore = overallRetention
        };
    }

    /// <summary>
    /// Heuristic-based engagement calculation for demonstration
    /// In production, this would be replaced by actual ML model prediction
    /// </summary>
    private double CalculateHeuristicEngagement(
        Scene scene,
        double importanceScore,
        double complexityScore,
        double emotionalIntensity)
    {
        var baseScore = 60.0;

        // Importance contributes to engagement
        baseScore += importanceScore * 0.2;

        // Moderate complexity is best (too simple or too complex reduces engagement)
        var complexityFactor = 1.0 - Math.Abs(complexityScore - 60) / 60.0;
        baseScore += complexityFactor * 10;

        // Emotional intensity increases engagement
        baseScore += emotionalIntensity * 0.15;

        // Word count affects engagement (optimal around 50-150 words)
        var wordCount = scene.Script.Split(new[] { ' ', '\t', '\n', '\r' }, 
            StringSplitOptions.RemoveEmptyEntries).Length;
        var wordCountFactor = wordCount switch
        {
            < 20 => 0.7,
            < 50 => 0.85,
            < 150 => 1.0,
            < 250 => 0.9,
            _ => 0.75
        };
        baseScore *= wordCountFactor;

        return Math.Clamp(baseScore, 0.0, 100.0);
    }

    /// <summary>
    /// Calculates attention level for a scene
    /// </summary>
    private async Task<double> CalculateSceneAttentionAsync(
        Scene scene,
        SceneTimingSuggestion? suggestion,
        int sceneIndex,
        int totalScenes,
        double previousAttention,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        var baseAttention = 70.0;

        // First scene (hook) should have high attention
        if (sceneIndex == 0)
        {
            baseAttention = 85.0;
        }
        // Last scene maintains good attention
        else if (sceneIndex == totalScenes - 1)
        {
            baseAttention = 75.0;
        }
        // Middle scenes have natural decay
        else
        {
            var positionFactor = 1.0 - (sceneIndex / (double)totalScenes) * 0.3;
            baseAttention *= positionFactor;
        }

        // Use suggestion data if available
        if (suggestion != null)
        {
            baseAttention += suggestion.ImportanceScore * 0.15;
            baseAttention += suggestion.EmotionalIntensity * 0.1;
        }

        // Smooth transition from previous attention
        var finalAttention = (baseAttention * 0.7) + (previousAttention * 0.3);

        return Math.Clamp(finalAttention, 20.0, 100.0);
    }

    /// <summary>
    /// Calculates attention at a specific point within a scene
    /// </summary>
    private double CalculatePointAttention(double sceneAttention, int pointIndex, int totalPoints)
    {
        // Natural attention curve within scene: starts high, dips in middle, recovers at end
        var normalizedPosition = (double)pointIndex / totalPoints;
        var curveFactor = 1.0 - (0.2 * Math.Sin(Math.PI * normalizedPosition));
        
        return sceneAttention * curveFactor;
    }

    /// <summary>
    /// Calculates retention rate based on attention and time
    /// </summary>
    private double CalculateRetention(double attention, TimeSpan timestamp)
    {
        // Retention naturally decays over time
        var timeDecay = Math.Max(0, 1.0 - (timestamp.TotalSeconds / 600.0) * 0.3); // 30% decay over 10 min
        
        // High attention reduces decay
        var attentionBonus = attention / 100.0;
        
        var retention = 90.0 * timeDecay * (0.7 + 0.3 * attentionBonus);
        
        return Math.Clamp(retention, 10.0, 100.0);
    }
}
