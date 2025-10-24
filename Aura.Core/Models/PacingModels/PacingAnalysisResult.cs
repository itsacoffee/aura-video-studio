using System;
using System.Collections.Generic;

namespace Aura.Core.Models.PacingModels;

/// <summary>
/// Result of ML-powered pacing analysis for video scenes
/// </summary>
public record PacingAnalysisResult
{
    /// <summary>
    /// Scene timing suggestions with optimal durations
    /// </summary>
    public IReadOnlyList<SceneTimingSuggestion> TimingSuggestions { get; init; } = Array.Empty<SceneTimingSuggestion>();

    /// <summary>
    /// Predicted attention curve data over the video timeline
    /// </summary>
    public AttentionCurveData? AttentionCurve { get; init; }

    /// <summary>
    /// Overall confidence score for the analysis (0-100)
    /// </summary>
    public double ConfidenceScore { get; init; }

    /// <summary>
    /// Predicted overall retention rate (0-100)
    /// </summary>
    public double PredictedRetentionRate { get; init; }

    /// <summary>
    /// Total optimal video duration
    /// </summary>
    public TimeSpan OptimalDuration { get; init; }

    /// <summary>
    /// Analysis timestamp
    /// </summary>
    public DateTime AnalyzedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// LLM provider used for scene analysis (if any)
    /// </summary>
    public string? LlmProviderUsed { get; init; }

    /// <summary>
    /// Whether LLM analysis was successful
    /// </summary>
    public bool LlmAnalysisSucceeded { get; init; }

    /// <summary>
    /// Warnings or recommendations
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Transition recommendations between scenes
    /// </summary>
    public IReadOnlyList<TransitionRecommendation> TransitionRecommendations { get; init; } = Array.Empty<TransitionRecommendation>();

    /// <summary>
    /// Emotional beats detected across the video
    /// </summary>
    public IReadOnlyList<EmotionalBeat> EmotionalBeats { get; init; } = Array.Empty<EmotionalBeat>();

    /// <summary>
    /// Scene relationship graph and flow analysis
    /// </summary>
    public SceneRelationshipGraph? SceneRelationships { get; init; }
}

