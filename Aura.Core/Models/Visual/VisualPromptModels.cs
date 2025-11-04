using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Visual;

/// <summary>
/// Comprehensive visual prompt for image generation with cinematic context
/// </summary>
public record VisualPrompt
{
    /// <summary>
    /// Scene index this prompt is for
    /// </summary>
    public int SceneIndex { get; init; }

    /// <summary>
    /// Detailed visual description (100-200 tokens)
    /// </summary>
    public string DetailedDescription { get; init; } = string.Empty;

    /// <summary>
    /// Composition guidelines (rule of thirds, leading lines, etc.)
    /// </summary>
    public string CompositionGuidelines { get; init; } = string.Empty;

    /// <summary>
    /// Lighting mood and direction
    /// </summary>
    public LightingSetup Lighting { get; init; } = new();

    /// <summary>
    /// Recommended color palette (3-5 specific colors)
    /// </summary>
    public IReadOnlyList<string> ColorPalette { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Camera angle and shot type
    /// </summary>
    public CameraSetup Camera { get; init; } = new();

    /// <summary>
    /// Style-specific keywords for the image model
    /// </summary>
    public IReadOnlyList<string> StyleKeywords { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Visual style (realistic, cinematic, illustrated, etc.)
    /// </summary>
    public VisualStyle Style { get; init; } = VisualStyle.Cinematic;

    /// <summary>
    /// Quality tier based on scene importance
    /// </summary>
    public VisualQualityTier QualityTier { get; init; } = VisualQualityTier.Standard;

    /// <summary>
    /// Scene importance score (0-100) from pacing analysis
    /// </summary>
    public double ImportanceScore { get; init; }

    /// <summary>
    /// Negative prompt elements (things to avoid)
    /// </summary>
    public IReadOnlyList<string> NegativeElements { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Elements that should remain consistent with previous scenes
    /// </summary>
    public VisualContinuity? Continuity { get; init; }

    /// <summary>
    /// Provider-specific optimized prompts
    /// </summary>
    public ProviderSpecificPrompts? ProviderPrompts { get; init; }

    /// <summary>
    /// Reasoning/explanation for prompt choices
    /// </summary>
    public string Reasoning { get; init; } = string.Empty;

    /// <summary>
    /// Subject of the visual (person, object, scene)
    /// </summary>
    public string Subject { get; init; } = string.Empty;

    /// <summary>
    /// Framing instructions (tight framing, wide framing, etc.)
    /// </summary>
    public string Framing { get; init; } = string.Empty;

    /// <summary>
    /// Narrative keywords for matching assets to scene content
    /// </summary>
    public IReadOnlyList<string> NarrativeKeywords { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Lighting setup information
/// </summary>
public record LightingSetup
{
    /// <summary>
    /// Lighting mood (golden hour, dramatic, soft, etc.)
    /// </summary>
    public string Mood { get; init; } = "neutral";

    /// <summary>
    /// Light direction (front, side, back, top, etc.)
    /// </summary>
    public string Direction { get; init; } = "front";

    /// <summary>
    /// Lighting quality (soft, hard, diffused, etc.)
    /// </summary>
    public string Quality { get; init; } = "soft";

    /// <summary>
    /// Time of day (morning, noon, afternoon, evening, night)
    /// </summary>
    public string TimeOfDay { get; init; } = "day";
}

/// <summary>
/// Camera setup information
/// </summary>
public record CameraSetup
{
    /// <summary>
    /// Shot type (wide, medium, close-up, extreme close-up, etc.)
    /// </summary>
    public ShotType ShotType { get; init; } = ShotType.MediumShot;

    /// <summary>
    /// Camera angle (eye level, high angle, low angle, Dutch angle, etc.)
    /// </summary>
    public CameraAngle Angle { get; init; } = CameraAngle.EyeLevel;

    /// <summary>
    /// Movement type (static, pan, tilt, dolly, etc.)
    /// </summary>
    public string Movement { get; init; } = "static";

    /// <summary>
    /// Depth of field (shallow, deep, bokeh)
    /// </summary>
    public string DepthOfField { get; init; } = "medium";
}

/// <summary>
/// Visual continuity tracking across scenes
/// </summary>
public record VisualContinuity
{
    /// <summary>
    /// Character appearance consistency
    /// </summary>
    public IReadOnlyList<string> CharacterAppearance { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Location details that should persist
    /// </summary>
    public IReadOnlyList<string> LocationDetails { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Color grading consistency
    /// </summary>
    public IReadOnlyList<string> ColorGrading { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Time of day progression
    /// </summary>
    public string TimeProgression { get; init; } = string.Empty;

    /// <summary>
    /// Similarity score with previous scene (0-100)
    /// </summary>
    public double SimilarityScore { get; init; }
}

/// <summary>
/// Provider-specific optimized prompts
/// </summary>
public record ProviderSpecificPrompts
{
    /// <summary>
    /// Stable Diffusion optimized prompt with emphasis syntax
    /// </summary>
    public string? StableDiffusion { get; init; }

    /// <summary>
    /// DALL-E 3 natural language prompt
    /// </summary>
    public string? DallE3 { get; init; }

    /// <summary>
    /// Midjourney prompt with parameters
    /// </summary>
    public string? Midjourney { get; init; }
}

/// <summary>
/// Visual style options
/// </summary>
public enum VisualStyle
{
    Realistic,
    Cinematic,
    Illustrated,
    Abstract,
    Animated,
    Documentary,
    Dramatic,
    Minimalist,
    Vintage,
    Modern
}

/// <summary>
/// Quality tier for visual generation
/// </summary>
public enum VisualQualityTier
{
    Basic,
    Standard,
    Enhanced,
    Premium
}

/// <summary>
/// Shot type classifications
/// </summary>
public enum ShotType
{
    ExtremeWideShot,
    WideShot,
    FullShot,
    MediumShot,
    MediumCloseUp,
    CloseUp,
    ExtremeCloseUp,
    OverTheShoulder,
    PointOfView
}

/// <summary>
/// Camera angle types
/// </summary>
public enum CameraAngle
{
    EyeLevel,
    HighAngle,
    LowAngle,
    BirdsEye,
    WormsEye,
    DutchAngle,
    OverTheShoulder
}

/// <summary>
/// Cinematography knowledge for a specific shot type
/// </summary>
public record CinematographyKnowledge
{
    public ShotType ShotType { get; init; }
    public string Description { get; init; } = string.Empty;
    public string TypicalUsage { get; init; } = string.Empty;
    public string EmotionalImpact { get; init; } = string.Empty;
    public IReadOnlyList<string> CompositionTips { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Lighting pattern knowledge
/// </summary>
public record LightingPattern
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string MoodEffect { get; init; } = string.Empty;
    public IReadOnlyList<string> BestUseCases { get; init; } = Array.Empty<string>();
}
