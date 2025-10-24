using System;

namespace Aura.Core.Models.PacingModels;

/// <summary>
/// Represents an emotional beat in the video narrative
/// </summary>
public record EmotionalBeat
{
    /// <summary>
    /// Scene index where this emotional beat occurs
    /// </summary>
    public int SceneIndex { get; init; }

    /// <summary>
    /// Emotional intensity level (0-100)
    /// </summary>
    public double EmotionalIntensity { get; init; }

    /// <summary>
    /// Primary emotion detected (e.g., "excitement", "tension", "calm", "joy")
    /// </summary>
    public string PrimaryEmotion { get; init; } = string.Empty;

    /// <summary>
    /// Direction of emotional change (rising, falling, stable)
    /// </summary>
    public EmotionalChange EmotionalChange { get; init; }

    /// <summary>
    /// Predicted viewer impact level
    /// </summary>
    public ViewerImpact ViewerImpact { get; init; }

    /// <summary>
    /// Recommended pacing emphasis for this beat
    /// </summary>
    public PacingEmphasis RecommendedEmphasis { get; init; }

    /// <summary>
    /// Whether this represents an emotional peak
    /// </summary>
    public bool IsPeak { get; init; }

    /// <summary>
    /// Whether this represents an emotional valley
    /// </summary>
    public bool IsValley { get; init; }

    /// <summary>
    /// Position in the overall emotional arc (0.0 to 1.0)
    /// </summary>
    public double ArcPosition { get; init; }

    /// <summary>
    /// Human-readable analysis of the emotional beat
    /// </summary>
    public string Analysis { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp of this beat in the video
    /// </summary>
    public TimeSpan Timestamp { get; init; }
}

/// <summary>
/// Direction of emotional change
/// </summary>
public enum EmotionalChange
{
    Rising,
    Falling,
    Stable
}

/// <summary>
/// Predicted viewer impact level
/// </summary>
public enum ViewerImpact
{
    Low,
    Medium,
    High
}

/// <summary>
/// Recommended pacing emphasis
/// </summary>
public enum PacingEmphasis
{
    More,
    Same,
    Less
}
