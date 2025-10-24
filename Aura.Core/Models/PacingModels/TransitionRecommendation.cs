using System;

namespace Aura.Core.Models.PacingModels;

/// <summary>
/// Recommendation for transition between two scenes
/// </summary>
public record TransitionRecommendation
{
    /// <summary>
    /// Index of the first scene in the transition
    /// </summary>
    public int FromSceneIndex { get; init; }

    /// <summary>
    /// Index of the second scene in the transition
    /// </summary>
    public int ToSceneIndex { get; init; }

    /// <summary>
    /// Recommended transition type
    /// </summary>
    public TransitionType RecommendedType { get; init; }

    /// <summary>
    /// Recommended transition duration in seconds
    /// </summary>
    public double DurationSeconds { get; init; }

    /// <summary>
    /// Content relationship between scenes (e.g., "directly related", "time change", "topic shift")
    /// </summary>
    public string ContentRelationship { get; init; } = string.Empty;

    /// <summary>
    /// Emotional intensity change between scenes
    /// </summary>
    public double EmotionalIntensityChange { get; init; }

    /// <summary>
    /// Confidence score for this recommendation (0-100)
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Human-readable reasoning for the recommendation
    /// </summary>
    public string Reasoning { get; init; } = string.Empty;

    /// <summary>
    /// Whether this transition might be jarring to viewers
    /// </summary>
    public bool IsJarring { get; init; }

    /// <summary>
    /// Alternative transition suggestion (if applicable)
    /// </summary>
    public TransitionType? AlternativeType { get; init; }

    /// <summary>
    /// Platform-specific optimization applied
    /// </summary>
    public string? PlatformOptimization { get; init; }
}
