using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Quality;

/// <summary>
/// Comprehensive tone profile with multi-dimensional style guide
/// </summary>
public record ToneProfile
{
    /// <summary>
    /// Original tone parameter from brief
    /// </summary>
    public string OriginalTone { get; init; } = string.Empty;

    /// <summary>
    /// Vocabulary level (grade 6-12, college, expert)
    /// </summary>
    public VocabularyLevel VocabularyLevel { get; init; }

    /// <summary>
    /// Formality level (casual, professional, academic)
    /// </summary>
    public FormalityLevel Formality { get; init; }

    /// <summary>
    /// Humor presence and type
    /// </summary>
    public HumorStyle Humor { get; init; }

    /// <summary>
    /// Energy level (calm, moderate, high)
    /// </summary>
    public EnergyLevel Energy { get; init; }

    /// <summary>
    /// Narrative perspective (first-person, second-person, authoritative third-person)
    /// </summary>
    public NarrativePerspective Perspective { get; init; }

    /// <summary>
    /// Specific examples and guidelines for this tone
    /// </summary>
    public string Guidelines { get; init; } = string.Empty;

    /// <summary>
    /// Example phrases that match this tone
    /// </summary>
    public string[] ExamplePhrases { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Phrases to avoid with this tone
    /// </summary>
    public string[] PhrasesToAvoid { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Target words per minute based on energy level
    /// </summary>
    public int TargetWordsPerMinute { get; init; }

    /// <summary>
    /// Recommended TTS rate adjustment (-50 to +50)
    /// </summary>
    public int RecommendedTtsRateAdjustment { get; init; }

    /// <summary>
    /// Recommended TTS pitch adjustment (-50 to +50)
    /// </summary>
    public int RecommendedTtsPitchAdjustment { get; init; }

    /// <summary>
    /// Visual style keywords aligned with tone
    /// </summary>
    public string[] VisualStyleKeywords { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Timestamp when profile was created
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Vocabulary complexity level
/// </summary>
public enum VocabularyLevel
{
    Grade6to8,
    Grade9to12,
    College,
    Expert
}

/// <summary>
/// Formality level of content
/// </summary>
public enum FormalityLevel
{
    Casual,
    Conversational,
    Professional,
    Academic
}

/// <summary>
/// Humor style and presence
/// </summary>
public enum HumorStyle
{
    None,
    Light,
    Witty,
    Satirical,
    Playful
}

/// <summary>
/// Energy level and pacing
/// </summary>
public enum EnergyLevel
{
    Calm,
    Moderate,
    Energetic,
    High
}

/// <summary>
/// Narrative perspective
/// </summary>
public enum NarrativePerspective
{
    FirstPerson,
    SecondPerson,
    ThirdPersonAuthority,
    ThirdPersonNeutral
}

/// <summary>
/// Tone consistency score for a scene or overall video
/// </summary>
public record ToneConsistencyScore
{
    /// <summary>
    /// Overall consistency score (0-100)
    /// </summary>
    public double OverallScore { get; init; }

    /// <summary>
    /// Vocabulary consistency score
    /// </summary>
    public double VocabularyScore { get; init; }

    /// <summary>
    /// Formality consistency score
    /// </summary>
    public double FormalityScore { get; init; }

    /// <summary>
    /// Energy consistency score
    /// </summary>
    public double EnergyScore { get; init; }

    /// <summary>
    /// Perspective consistency score
    /// </summary>
    public double PerspectiveScore { get; init; }

    /// <summary>
    /// Visual style alignment score
    /// </summary>
    public double VisualAlignmentScore { get; init; }

    /// <summary>
    /// Pacing alignment score
    /// </summary>
    public double PacingAlignmentScore { get; init; }

    /// <summary>
    /// Scene index (or -1 for overall)
    /// </summary>
    public int SceneIndex { get; init; } = -1;

    /// <summary>
    /// Detailed reasoning for the score
    /// </summary>
    public string Reasoning { get; init; } = string.Empty;

    /// <summary>
    /// Whether this passes the threshold (>85)
    /// </summary>
    public bool Passes => OverallScore > 85;
}

/// <summary>
/// Style violation detected in content
/// </summary>
public record StyleViolation
{
    /// <summary>
    /// Severity of the violation
    /// </summary>
    public ViolationSeverity Severity { get; init; }

    /// <summary>
    /// Category of violation
    /// </summary>
    public ViolationCategory Category { get; init; }

    /// <summary>
    /// Scene index where violation occurred
    /// </summary>
    public int SceneIndex { get; init; }

    /// <summary>
    /// Description of the violation
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Specific example from content
    /// </summary>
    public string Example { get; init; } = string.Empty;

    /// <summary>
    /// Expected tone characteristic
    /// </summary>
    public string Expected { get; init; } = string.Empty;

    /// <summary>
    /// Actual characteristic found
    /// </summary>
    public string Actual { get; init; } = string.Empty;

    /// <summary>
    /// Suggested correction
    /// </summary>
    public string? SuggestedCorrection { get; init; }

    /// <summary>
    /// Impact score (0-100)
    /// </summary>
    public double ImpactScore { get; init; }
}

/// <summary>
/// Severity level of style violation
/// </summary>
public enum ViolationSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Category of style violation
/// </summary>
public enum ViolationCategory
{
    VocabularyMismatch,
    FormalityShift,
    EnergyInconsistency,
    PerspectiveChange,
    InappropriateHumor,
    VisualStyleMismatch,
    PacingMismatch,
    ToneDrift
}

/// <summary>
/// Tone drift detection result
/// </summary>
public record ToneDriftResult
{
    /// <summary>
    /// Whether drift was detected
    /// </summary>
    public bool DriftDetected { get; init; }

    /// <summary>
    /// Drift magnitude (0-1, where 0 is no drift)
    /// </summary>
    public double DriftMagnitude { get; init; }

    /// <summary>
    /// Scene indices where drift starts
    /// </summary>
    public int[] DriftStartIndices { get; init; } = Array.Empty<int>();

    /// <summary>
    /// Characteristics that drifted
    /// </summary>
    public string[] DriftedCharacteristics { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Detailed analysis of the drift
    /// </summary>
    public string Analysis { get; init; } = string.Empty;

    /// <summary>
    /// Violations detected during drift analysis
    /// </summary>
    public StyleViolation[] Violations { get; init; } = Array.Empty<StyleViolation>();
}

/// <summary>
/// Tone correction suggestion
/// </summary>
public record ToneCorrectionSuggestion
{
    /// <summary>
    /// Scene index to correct
    /// </summary>
    public int SceneIndex { get; init; }

    /// <summary>
    /// Original text
    /// </summary>
    public string OriginalText { get; init; } = string.Empty;

    /// <summary>
    /// Corrected text preserving meaning
    /// </summary>
    public string CorrectedText { get; init; } = string.Empty;

    /// <summary>
    /// Explanation of changes made
    /// </summary>
    public string Explanation { get; init; } = string.Empty;

    /// <summary>
    /// Tone consistency score before correction
    /// </summary>
    public double ScoreBefore { get; init; }

    /// <summary>
    /// Expected tone consistency score after correction
    /// </summary>
    public double ScoreAfter { get; init; }

    /// <summary>
    /// Specific changes made
    /// </summary>
    public string[] SpecificChanges { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Result of cross-modal tone validation
/// </summary>
public record CrossModalToneValidation
{
    /// <summary>
    /// Script tone alignment score
    /// </summary>
    public double ScriptScore { get; init; }

    /// <summary>
    /// Visual style alignment score
    /// </summary>
    public double VisualScore { get; init; }

    /// <summary>
    /// Pacing alignment score
    /// </summary>
    public double PacingScore { get; init; }

    /// <summary>
    /// Audio/TTS alignment score
    /// </summary>
    public double AudioScore { get; init; }

    /// <summary>
    /// Overall cross-modal consistency score
    /// </summary>
    public double OverallScore { get; init; }

    /// <summary>
    /// Whether all modalities are aligned (>80)
    /// </summary>
    public bool IsAligned => OverallScore > 80;

    /// <summary>
    /// Violations found across modalities
    /// </summary>
    public StyleViolation[] Violations { get; init; } = Array.Empty<StyleViolation>();

    /// <summary>
    /// Detailed analysis
    /// </summary>
    public string Analysis { get; init; } = string.Empty;
}
