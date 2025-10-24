using System;
using System.Collections.Generic;

namespace Aura.Core.ML.Models;

/// <summary>
/// ML model for predicting video content success
/// Uses historical performance data to estimate engagement potential
/// </summary>
public class ContentSuccessPredictionModel
{
    /// <summary>
    /// Predict success score for content based on features
    /// </summary>
    /// <param name="features">Content features extracted from brief and spec</param>
    /// <returns>Prediction result with confidence</returns>
    public PredictionResult PredictSuccess(ContentFeatures features)
    {
        // Simple heuristic-based prediction for initial implementation
        // This can be replaced with an actual ML model later
        double score = 50.0; // Base score
        double confidence = 0.5; // Base confidence
        var factors = new List<string>();

        // Topic complexity factor
        if (!string.IsNullOrEmpty(features.Topic))
        {
            var wordCount = features.Topic.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount > 3 && wordCount < 10)
            {
                score += 10;
                factors.Add("Good topic specificity");
            }
            else if (wordCount >= 10)
            {
                score -= 5;
                factors.Add("Topic may be too complex");
            }
        }

        // Duration factor
        if (features.DurationMinutes > 0)
        {
            if (features.DurationMinutes >= 1 && features.DurationMinutes <= 5)
            {
                score += 15;
                confidence += 0.1;
                factors.Add("Optimal duration for engagement");
            }
            else if (features.DurationMinutes > 5 && features.DurationMinutes <= 10)
            {
                score += 5;
                factors.Add("Good duration");
            }
            else if (features.DurationMinutes > 10)
            {
                score -= 10;
                factors.Add("May be too long for some audiences");
            }
        }

        // Pacing factor
        if (features.Pacing == "Fast")
        {
            score += 10;
            factors.Add("Fast pacing maintains engagement");
        }
        else if (features.Pacing == "Conversational")
        {
            score += 8;
            factors.Add("Conversational pacing is engaging");
        }

        // Tone factor
        if (!string.IsNullOrEmpty(features.Tone))
        {
            var tone = features.Tone.ToLowerInvariant();
            if (tone.Contains("humor") || tone.Contains("entertaining"))
            {
                score += 12;
                factors.Add("Entertaining tone increases engagement");
            }
            else if (tone.Contains("professional") || tone.Contains("informative"))
            {
                score += 8;
                factors.Add("Professional tone builds trust");
            }
        }

        // Density factor
        if (features.Density == "Balanced")
        {
            score += 8;
            confidence += 0.05;
            factors.Add("Balanced content density");
        }
        else if (features.Density == "Dense")
        {
            score -= 5;
            factors.Add("Dense content may reduce accessibility");
        }

        // Historical performance factor (if available)
        if (features.HistoricalAverageScore > 0)
        {
            score += (features.HistoricalAverageScore - 50) * 0.3;
            confidence += 0.2;
            factors.Add($"Based on historical performance: {features.HistoricalAverageScore:F1}");
        }

        // Normalize score and confidence
        score = Math.Max(0, Math.Min(100, score));
        confidence = Math.Max(0, Math.Min(1, confidence));

        return new PredictionResult
        {
            PredictedScore = score,
            Confidence = confidence,
            ContributingFactors = factors,
            PredictedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Train model with new outcome data (placeholder for future ML implementation)
    /// </summary>
    public void UpdateWithOutcome(ContentFeatures features, double actualScore)
    {
        // Placeholder for future ML model training
        // In a full implementation, this would update model weights
    }
}

/// <summary>
/// Features extracted from content for prediction
/// </summary>
public record ContentFeatures
{
    public string Topic { get; init; } = string.Empty;
    public double DurationMinutes { get; init; }
    public string Pacing { get; init; } = string.Empty;
    public string Density { get; init; } = string.Empty;
    public string Tone { get; init; } = string.Empty;
    public string? Audience { get; init; }
    public string? Goal { get; init; }
    public double HistoricalAverageScore { get; init; }
}

/// <summary>
/// Prediction result from the model
/// </summary>
public record PredictionResult
{
    /// <summary>
    /// Predicted success score (0-100)
    /// </summary>
    public double PredictedScore { get; init; }

    /// <summary>
    /// Confidence in prediction (0-1)
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Factors contributing to the prediction
    /// </summary>
    public List<string> ContributingFactors { get; init; } = new();

    /// <summary>
    /// When prediction was made
    /// </summary>
    public DateTime PredictedAt { get; init; }

    /// <summary>
    /// Quality threshold check
    /// </summary>
    public bool MeetsThreshold(int threshold) => PredictedScore >= threshold;
}
