using System;
using System.Collections.Generic;
using Aura.Core.Models.PacingModels;

namespace Aura.Core.Models.Visual;

/// <summary>
/// Result of visual-text synchronization analysis for a video
/// Provides comprehensive alignment between narration and visual content
/// </summary>
public record VisualTextSyncResult
{
    /// <summary>
    /// Narration segments with visual recommendations
    /// </summary>
    public IReadOnlyList<NarrationSegment> Segments { get; init; } = Array.Empty<NarrationSegment>();

    /// <summary>
    /// Overall cognitive load score for the video (0-100)
    /// Values above 75 indicate potential viewer overload
    /// </summary>
    public double OverallCognitiveLoad { get; init; }

    /// <summary>
    /// Correlation coefficient between narration and visual complexity (-1 to 1)
    /// Target: negative correlation greater than 0.7 (inverse relationship)
    /// </summary>
    public double ComplexityCorrelation { get; init; }

    /// <summary>
    /// Percentage of visual transitions aligned with narration pauses/topic shifts (0-100)
    /// Target: greater than 90%
    /// </summary>
    public double TransitionAlignmentRate { get; init; }

    /// <summary>
    /// Analysis timestamp
    /// </summary>
    public DateTime AnalyzedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Validation warnings (e.g., potential contradictions, overload points)
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Whether LLM analysis was used
    /// </summary>
    public bool UsedLlmAnalysis { get; init; }

    /// <summary>
    /// Overall recommendation summary
    /// </summary>
    public string RecommendationSummary { get; init; } = string.Empty;
}

/// <summary>
/// A narration segment (sentence or phrase level) with visual synchronization data
/// </summary>
public record NarrationSegment
{
    /// <summary>
    /// Scene index this segment belongs to
    /// </summary>
    public int SceneIndex { get; init; }

    /// <summary>
    /// Segment text content
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Start time relative to scene start (for ±0.5 second accuracy)
    /// </summary>
    public TimeSpan StartTime { get; init; }

    /// <summary>
    /// Duration of this segment
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// End time (calculated)
    /// </summary>
    public TimeSpan EndTime => StartTime + Duration;

    /// <summary>
    /// Narration complexity score (0-100)
    /// Based on vocabulary, concept density, sentence structure
    /// </summary>
    public double NarrationComplexity { get; init; }

    /// <summary>
    /// Key concepts requiring visual support
    /// </summary>
    public IReadOnlyList<KeyConcept> KeyConcepts { get; init; } = Array.Empty<KeyConcept>();

    /// <summary>
    /// Visual recommendations for this segment
    /// </summary>
    public IReadOnlyList<VisualRecommendation> VisualRecommendations { get; init; } = Array.Empty<VisualRecommendation>();

    /// <summary>
    /// Cognitive load score for this segment (0-100)
    /// </summary>
    public double CognitiveLoadScore { get; init; }

    /// <summary>
    /// Whether this segment contains a topic shift or pause point
    /// </summary>
    public bool IsTransitionPoint { get; init; }

    /// <summary>
    /// Narration rate (words per minute) for this segment
    /// </summary>
    public double NarrationRate { get; init; }

    /// <summary>
    /// Information density level
    /// </summary>
    public InformationDensity InformationDensity { get; init; }
}

/// <summary>
/// A key concept in narration that requires visual support
/// </summary>
public record KeyConcept
{
    /// <summary>
    /// The concept text (noun, data point, technical term)
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Type of concept
    /// </summary>
    public ConceptType Type { get; init; }

    /// <summary>
    /// Importance score (0-100)
    /// </summary>
    public double Importance { get; init; }

    /// <summary>
    /// Time offset within segment where this concept appears
    /// </summary>
    public TimeSpan TimeOffset { get; init; }

    /// <summary>
    /// Suggested visual implementation
    /// </summary>
    public string SuggestedVisualization { get; init; } = string.Empty;

    /// <summary>
    /// Whether this is an abstract concept requiring metaphor
    /// </summary>
    public bool RequiresMetaphor { get; init; }

    /// <summary>
    /// Whether this is an action verb suggesting motion/animation
    /// </summary>
    public bool SuggestsMotion { get; init; }
}

/// <summary>
/// Type of concept requiring visualization
/// </summary>
public enum ConceptType
{
    Noun,
    DataPoint,
    TechnicalTerm,
    ActionVerb,
    AbstractConcept,
    Comparison,
    Process
}

/// <summary>
/// Visual recommendation for a narration segment
/// </summary>
public record VisualRecommendation
{
    /// <summary>
    /// Type of visual content recommended
    /// </summary>
    public VisualContentType ContentType { get; init; }

    /// <summary>
    /// Detailed description of recommended visual
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Start time for this visual relative to segment start
    /// </summary>
    public TimeSpan StartTime { get; init; }

    /// <summary>
    /// Duration this visual should be displayed
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Visual complexity score (0-100)
    /// Should be inversely correlated with narration complexity
    /// </summary>
    public double VisualComplexity { get; init; }

    /// <summary>
    /// B-roll search keywords (specific, not generic)
    /// </summary>
    public IReadOnlyList<string> BRollKeywords { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Visual metadata for image generation
    /// </summary>
    public VisualMetadataTags? Metadata { get; init; }

    /// <summary>
    /// Priority level (0-100)
    /// </summary>
    public double Priority { get; init; }

    /// <summary>
    /// Whether this visual should be static or dynamic
    /// </summary>
    public bool RequiresDynamicContent { get; init; }

    /// <summary>
    /// Reasoning for this recommendation
    /// </summary>
    public string Reasoning { get; init; } = string.Empty;
}

/// <summary>
/// Type of visual content
/// </summary>
public enum VisualContentType
{
    BRoll,
    Illustration,
    Chart,
    Graph,
    Metaphor,
    Animation,
    TextOverlay,
    ProductShot,
    EnvironmentShot
}

/// <summary>
/// Metadata tags for guiding image generation with synchronization requirements
/// </summary>
public record VisualMetadataTags
{
    /// <summary>
    /// Camera angle recommendation
    /// </summary>
    public CameraAngle CameraAngle { get; init; } = CameraAngle.EyeLevel;

    /// <summary>
    /// Composition rule to apply
    /// </summary>
    public CompositionRule CompositionRule { get; init; } = CompositionRule.RuleOfThirds;

    /// <summary>
    /// Primary focus point in the frame
    /// </summary>
    public string FocusPoint { get; init; } = string.Empty;

    /// <summary>
    /// Color scheme matching emotional tone
    /// </summary>
    public IReadOnlyList<string> ColorScheme { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Emotional tone tags
    /// </summary>
    public IReadOnlyList<string> EmotionalTones { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Shot type for this visual
    /// </summary>
    public ShotType ShotType { get; init; } = ShotType.MediumShot;

    /// <summary>
    /// Depth of field setting
    /// </summary>
    public string DepthOfField { get; init; } = "medium";

    /// <summary>
    /// Lighting mood
    /// </summary>
    public string LightingMood { get; init; } = "neutral";

    /// <summary>
    /// Whether to use motion blur for dynamic content
    /// </summary>
    public bool UseMotionBlur { get; init; }

    /// <summary>
    /// Attention direction cues (what viewer should focus on)
    /// </summary>
    public IReadOnlyList<string> AttentionCues { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Composition rules for image framing
/// </summary>
public enum CompositionRule
{
    RuleOfThirds,
    Centered,
    GoldenRatio,
    LeadingLines,
    SymmetricalBalance,
    Frame,
    NegativeSpace
}

/// <summary>
/// Cognitive load metrics for a video segment
/// </summary>
public record CognitiveLoadMetrics
{
    /// <summary>
    /// Overall cognitive load (0-100)
    /// Target: less than 75 to avoid viewer overload
    /// </summary>
    public double OverallLoad { get; init; }

    /// <summary>
    /// Narration cognitive load (0-100)
    /// </summary>
    public double NarrationLoad { get; init; }

    /// <summary>
    /// Visual cognitive load (0-100)
    /// </summary>
    public double VisualLoad { get; init; }

    /// <summary>
    /// Combined modality load (interaction between visual and narration)
    /// </summary>
    public double MultiModalLoad { get; init; }

    /// <summary>
    /// Information processing rate (concepts per second)
    /// </summary>
    public double ProcessingRate { get; init; }

    /// <summary>
    /// Recommended cognitive load threshold
    /// </summary>
    public const double RecommendedThreshold = 75.0;

    /// <summary>
    /// Whether load exceeds recommended threshold
    /// </summary>
    public bool ExceedsThreshold => OverallLoad > RecommendedThreshold;

    /// <summary>
    /// Detailed breakdown of load sources
    /// </summary>
    public string LoadBreakdown { get; init; } = string.Empty;

    /// <summary>
    /// Recommendations for reducing cognitive load
    /// </summary>
    public IReadOnlyList<string> Recommendations { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Pacing recommendation for visual transitions
/// </summary>
public record VisualPacingRecommendation
{
    /// <summary>
    /// Scene index
    /// </summary>
    public int SceneIndex { get; init; }

    /// <summary>
    /// Narration rate for this scene (words per minute)
    /// </summary>
    public double NarrationRate { get; init; }

    /// <summary>
    /// Recommended number of visual changes
    /// Fast narration → fewer changes, Slow narration → more variety
    /// </summary>
    public int RecommendedVisualChanges { get; init; }

    /// <summary>
    /// Recommended transition duration
    /// </summary>
    public TimeSpan TransitionDuration { get; init; }

    /// <summary>
    /// Pause points where visual transitions should occur
    /// </summary>
    public IReadOnlyList<TimeSpan> TransitionPoints { get; init; } = Array.Empty<TimeSpan>();

    /// <summary>
    /// Whether narration pauses allow for visual transitions
    /// </summary>
    public bool HasNaturalPauses { get; init; }

    /// <summary>
    /// Reasoning for pacing recommendation
    /// </summary>
    public string Reasoning { get; init; } = string.Empty;
}

/// <summary>
/// Validation result for visual-narration consistency
/// Ensures visuals don't contradict spoken content
/// </summary>
public record VisualConsistencyValidation
{
    /// <summary>
    /// Whether visuals are consistent with narration
    /// </summary>
    public bool IsConsistent { get; init; }

    /// <summary>
    /// Consistency score (0-100)
    /// </summary>
    public double ConsistencyScore { get; init; }

    /// <summary>
    /// Detected contradictions (e.g., showing cats while saying "dogs")
    /// </summary>
    public IReadOnlyList<Contradiction> Contradictions { get; init; } = Array.Empty<Contradiction>();

    /// <summary>
    /// Visual elements that support narration effectively
    /// </summary>
    public IReadOnlyList<string> SupportingElements { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Overall alignment quality
    /// </summary>
    public string AlignmentQuality { get; init; } = string.Empty;
}

/// <summary>
/// A detected contradiction between visual and narration
/// </summary>
public record Contradiction
{
    /// <summary>
    /// What the narration says
    /// </summary>
    public string NarrationContent { get; init; } = string.Empty;

    /// <summary>
    /// What the visual shows
    /// </summary>
    public string VisualContent { get; init; } = string.Empty;

    /// <summary>
    /// Time offset where contradiction occurs
    /// </summary>
    public TimeSpan TimeOffset { get; init; }

    /// <summary>
    /// Severity (0-100)
    /// </summary>
    public double Severity { get; init; }

    /// <summary>
    /// Suggested correction
    /// </summary>
    public string SuggestedCorrection { get; init; } = string.Empty;
}
