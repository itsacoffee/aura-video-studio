using System;

namespace Aura.Core.Models.PacingModels;

/// <summary>
/// ML-driven timing suggestion for a video scene
/// </summary>
public record SceneTimingSuggestion
{
    /// <summary>
    /// Scene index
    /// </summary>
    public int SceneIndex { get; init; }

    /// <summary>
    /// Current scene duration
    /// </summary>
    public TimeSpan CurrentDuration { get; init; }

    /// <summary>
    /// Optimal recommended duration
    /// </summary>
    public TimeSpan OptimalDuration { get; init; }

    /// <summary>
    /// Minimum acceptable duration
    /// </summary>
    public TimeSpan MinDuration { get; init; }

    /// <summary>
    /// Maximum acceptable duration
    /// </summary>
    public TimeSpan MaxDuration { get; init; }

    /// <summary>
    /// Scene importance score (0-100)
    /// </summary>
    public double ImportanceScore { get; init; }

    /// <summary>
    /// Scene complexity score (0-100)
    /// </summary>
    public double ComplexityScore { get; init; }

    /// <summary>
    /// Emotional intensity score (0-100)
    /// </summary>
    public double EmotionalIntensity { get; init; }

    /// <summary>
    /// Information density level
    /// </summary>
    public InformationDensity InformationDensity { get; init; }

    /// <summary>
    /// Recommended transition type for this scene
    /// </summary>
    public TransitionType TransitionType { get; init; }

    /// <summary>
    /// Confidence score for this suggestion (0-100)
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Human-readable reasoning for the suggestion
    /// </summary>
    public string Reasoning { get; init; } = string.Empty;

    /// <summary>
    /// Whether LLM analysis was used for this scene
    /// </summary>
    public bool UsedLlmAnalysis { get; init; }

    /// <summary>
    /// Content complexity score from deep LLM analysis (0-100)
    /// </summary>
    public double ContentComplexityScore { get; init; }

    /// <summary>
    /// Cognitive processing time required for this scene
    /// </summary>
    public TimeSpan CognitiveProcessingTime { get; init; }

    /// <summary>
    /// Duration adjustment applied based on complexity
    /// </summary>
    public double DurationAdjustmentMultiplier { get; init; } = 1.0;

    /// <summary>
    /// Detailed complexity breakdown
    /// </summary>
    public string ComplexityBreakdown { get; init; } = string.Empty;
}

/// <summary>
/// Information density levels
/// </summary>
public enum InformationDensity
{
    Low,
    Medium,
    High
}

/// <summary>
/// Transition types for scene changes
/// </summary>
public enum TransitionType
{
    Cut,
    Fade,
    Dissolve
}
