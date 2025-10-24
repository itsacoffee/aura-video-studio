using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.ML.Models;
using Aura.Core.Models;
using Aura.Core.Models.PacingModels;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.PacingServices;

/// <summary>
/// Predicts attention curve and viewer engagement using ML models
/// </summary>
public class AttentionCurvePredictor
{
    private readonly ILogger<AttentionCurvePredictor> _logger;
    private readonly AttentionRetentionModel _attentionModel;

    public AttentionCurvePredictor(
        ILogger<AttentionCurvePredictor> logger,
        AttentionRetentionModel attentionModel)
    {
        _logger = logger;
        _attentionModel = attentionModel;
    }

    /// <summary>
    /// Generates attention curve prediction for a video
    /// </summary>
    public async Task<AttentionCurveData> GenerateAttentionCurveAsync(
        IReadOnlyList<Scene> scenes,
        IReadOnlyList<SceneTimingSuggestion> timingSuggestions,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating attention curve for {SceneCount} scenes", scenes.Count);

        try
        {
            // Ensure model is loaded
            if (!await EnsureModelLoadedAsync(ct))
            {
                _logger.LogWarning("Failed to load attention model, using heuristic prediction");
                return GenerateHeuristicAttentionCurve(scenes, timingSuggestions);
            }

            // Use ML model to predict attention curve
            var attentionCurve = await _attentionModel.PredictAttentionCurveAsync(
                scenes, timingSuggestions, ct);

            _logger.LogInformation("Attention curve generated. Avg engagement: {AvgEngagement:F1}%, " +
                "Retention: {Retention:F1}%, Peaks: {PeakCount}, Valleys: {ValleyCount}",
                attentionCurve.AverageEngagement,
                attentionCurve.OverallRetentionScore,
                attentionCurve.EngagementPeaks.Count,
                attentionCurve.EngagementValleys.Count);

            return attentionCurve;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating attention curve");
            return GenerateHeuristicAttentionCurve(scenes, timingSuggestions);
        }
    }

    /// <summary>
    /// Identifies engagement peaks in the attention curve
    /// </summary>
    public IReadOnlyList<TimeSpan> IdentifyEngagementPeaks(AttentionCurveData curve)
    {
        var peaks = new List<TimeSpan>();
        var dataPoints = curve.DataPoints.ToList();

        if (dataPoints.Count < 3)
            return peaks;

        for (int i = 1; i < dataPoints.Count - 1; i++)
        {
            var prev = dataPoints[i - 1];
            var current = dataPoints[i];
            var next = dataPoints[i + 1];

            // Peak: higher than neighbors and above threshold
            if (current.EngagementScore > prev.EngagementScore &&
                current.EngagementScore > next.EngagementScore &&
                current.EngagementScore > 70)
            {
                peaks.Add(current.Timestamp);
            }
        }

        _logger.LogDebug("Identified {PeakCount} engagement peaks", peaks.Count);
        return peaks;
    }

    /// <summary>
    /// Identifies engagement valleys (drop-off points)
    /// </summary>
    public IReadOnlyList<TimeSpan> IdentifyEngagementValleys(AttentionCurveData curve)
    {
        var valleys = new List<TimeSpan>();
        var dataPoints = curve.DataPoints.ToList();

        if (dataPoints.Count < 3)
            return valleys;

        for (int i = 1; i < dataPoints.Count - 1; i++)
        {
            var prev = dataPoints[i - 1];
            var current = dataPoints[i];
            var next = dataPoints[i + 1];

            // Valley: lower than neighbors and below threshold
            if (current.EngagementScore < prev.EngagementScore &&
                current.EngagementScore < next.EngagementScore &&
                current.EngagementScore < 50)
            {
                valleys.Add(current.Timestamp);
            }
        }

        _logger.LogDebug("Identified {ValleyCount} engagement valleys", valleys.Count);
        return valleys;
    }

    /// <summary>
    /// Calculates overall retention score
    /// </summary>
    public double CalculateOverallRetentionScore(AttentionCurveData curve)
    {
        if (curve.DataPoints.Count == 0)
            return 0;

        // Weight retention more heavily at the end (indicates full video completion)
        var totalWeight = 0.0;
        var weightedSum = 0.0;

        for (int i = 0; i < curve.DataPoints.Count; i++)
        {
            var point = curve.DataPoints[i];
            var position = i / (double)(curve.DataPoints.Count - 1);
            
            // Exponential weight: later points matter more
            var weight = 0.5 + (0.5 * position);
            
            weightedSum += point.RetentionRate * weight;
            totalWeight += weight;
        }

        var score = totalWeight > 0 ? weightedSum / totalWeight : 0;
        
        _logger.LogDebug("Overall retention score: {Score:F1}%", score);
        return score;
    }

    /// <summary>
    /// Ensures the ML model is loaded
    /// </summary>
    private async Task<bool> EnsureModelLoadedAsync(CancellationToken ct)
    {
        try
        {
            await _attentionModel.LoadModelAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load attention retention model");
            return false;
        }
    }

    /// <summary>
    /// Generates heuristic attention curve when ML model is unavailable
    /// </summary>
    private AttentionCurveData GenerateHeuristicAttentionCurve(
        IReadOnlyList<Scene> scenes,
        IReadOnlyList<SceneTimingSuggestion> timingSuggestions)
    {
        _logger.LogDebug("Using heuristic attention curve generation");

        var dataPoints = new List<AttentionDataPoint>();
        var peaks = new List<TimeSpan>();
        var valleys = new List<TimeSpan>();

        var currentTime = TimeSpan.Zero;
        var baseAttention = 85.0; // Start high

        for (int i = 0; i < scenes.Count; i++)
        {
            var scene = scenes[i];
            var suggestion = timingSuggestions.FirstOrDefault(s => s.SceneIndex == i);
            var duration = suggestion?.OptimalDuration ?? scene.Duration;

            // Calculate attention for this scene
            var sceneAttention = CalculateHeuristicSceneAttention(
                i, scenes.Count, suggestion, baseAttention);

            // Generate points throughout the scene
            var pointCount = Math.Max(2, (int)(duration.TotalSeconds / 5));
            
            for (int p = 0; p <= pointCount; p++)
            {
                var pointTime = currentTime + TimeSpan.FromSeconds(duration.TotalSeconds * p / pointCount);
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
                if (pointEngagement > 75)
                    peaks.Add(pointTime);
                else if (pointEngagement < 50)
                    valleys.Add(pointTime);
            }

            currentTime += duration;
            baseAttention = sceneAttention * 0.95; // Natural decay
        }

        var avgEngagement = dataPoints.Count > 0 
            ? dataPoints.Average(p => p.EngagementScore) 
            : 0;
        
        var overallRetention = dataPoints.Count > 0 
            ? dataPoints.Average(p => p.RetentionRate) 
            : 0;

        return new AttentionCurveData
        {
            DataPoints = dataPoints,
            AverageEngagement = avgEngagement,
            EngagementPeaks = peaks.Distinct().ToList(),
            EngagementValleys = valleys.Distinct().ToList(),
            OverallRetentionScore = overallRetention
        };
    }

    private double CalculateHeuristicSceneAttention(
        int sceneIndex,
        int totalScenes,
        SceneTimingSuggestion? suggestion,
        double baseAttention)
    {
        var attention = baseAttention;

        // First scene (hook) maintains high attention
        if (sceneIndex == 0)
        {
            attention = Math.Max(attention, 85.0);
        }
        // Last scene gets a boost (conclusion)
        else if (sceneIndex == totalScenes - 1)
        {
            attention = Math.Max(attention, 75.0);
        }

        // Use suggestion data if available
        if (suggestion != null)
        {
            attention += suggestion.ImportanceScore * 0.1;
            attention += suggestion.EmotionalIntensity * 0.05;
            attention -= (suggestion.ComplexityScore - 50) * 0.05; // High complexity reduces attention
        }

        return Math.Clamp(attention, 30.0, 100.0);
    }

    private double CalculatePointAttention(double sceneAttention, int pointIndex, int totalPoints)
    {
        // Attention varies within scene: high at start, dips in middle, recovers at end
        var position = (double)pointIndex / totalPoints;
        var curveFactor = 1.0 - (0.15 * Math.Sin(Math.PI * position));
        
        return sceneAttention * curveFactor;
    }

    private double CalculateRetention(double attention, TimeSpan timestamp)
    {
        // Retention decays over time but is boosted by high attention
        var timeFactor = Math.Max(0, 1.0 - (timestamp.TotalSeconds / 600.0) * 0.25); // 25% decay over 10 min
        var attentionBonus = attention / 100.0;
        
        var retention = 90.0 * timeFactor * (0.7 + 0.3 * attentionBonus);
        
        return Math.Clamp(retention, 20.0, 100.0);
    }
}
