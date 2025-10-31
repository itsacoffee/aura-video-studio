using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Audience;

/// <summary>
/// Result of content adaptation with before/after comparison
/// </summary>
public record AdaptationResult
{
    /// <summary>
    /// Original content before adaptation
    /// </summary>
    public string OriginalContent { get; init; } = string.Empty;

    /// <summary>
    /// Adapted content optimized for audience
    /// </summary>
    public string AdaptedContent { get; init; } = string.Empty;

    /// <summary>
    /// Specific changes made during adaptation
    /// </summary>
    public List<AdaptationChange> Changes { get; init; } = new();

    /// <summary>
    /// Readability metrics before adaptation
    /// </summary>
    public ReadabilityMetrics? OriginalMetrics { get; init; }

    /// <summary>
    /// Readability metrics after adaptation
    /// </summary>
    public ReadabilityMetrics? AdaptedMetrics { get; init; }

    /// <summary>
    /// Overall adaptation quality score (0-100)
    /// </summary>
    public double QualityScore { get; init; }

    /// <summary>
    /// Time taken for adaptation
    /// </summary>
    public TimeSpan AdaptationTime { get; init; }
}

/// <summary>
/// Specific change made during adaptation
/// </summary>
public record AdaptationChange
{
    /// <summary>
    /// Type of change (vocabulary, example, pacing, tone, etc.)
    /// </summary>
    public AdaptationChangeType Type { get; init; }

    /// <summary>
    /// Original text fragment
    /// </summary>
    public string OriginalText { get; init; } = string.Empty;

    /// <summary>
    /// Adapted text fragment
    /// </summary>
    public string AdaptedText { get; init; } = string.Empty;

    /// <summary>
    /// Explanation of why the change was made
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Position in the content (character index)
    /// </summary>
    public int Position { get; init; }
}

/// <summary>
/// Types of adaptation changes
/// </summary>
public enum AdaptationChangeType
{
    VocabularySimplification,
    VocabularyTechnification,
    ExamplePersonalization,
    PacingAdjustment,
    ToneAdjustment,
    FormalityAdjustment,
    CulturalAdaptation,
    ComplexityReduction,
    ComplexityIncrease,
    DefinitionAdded
}

/// <summary>
/// Readability and complexity metrics for content
/// </summary>
public record ReadabilityMetrics
{
    /// <summary>
    /// Flesch-Kincaid Grade Level
    /// </summary>
    public double FleschKincaidGrade { get; init; }

    /// <summary>
    /// SMOG (Simple Measure of Gobbledygook) Index
    /// </summary>
    public double SmogIndex { get; init; }

    /// <summary>
    /// Average sentence length
    /// </summary>
    public double AverageSentenceLength { get; init; }

    /// <summary>
    /// Percentage of complex words (3+ syllables)
    /// </summary>
    public double ComplexWordPercentage { get; init; }

    /// <summary>
    /// Estimated cognitive load (0-100)
    /// </summary>
    public int CognitiveLoad { get; init; }

    /// <summary>
    /// Vocabulary complexity score (0-100)
    /// </summary>
    public int VocabularyComplexity { get; init; }
}

/// <summary>
/// Configuration for content adaptation
/// </summary>
public record AdaptationConfig
{
    /// <summary>
    /// Target audience profile
    /// </summary>
    public AudienceProfile AudienceProfile { get; init; } = new();

    /// <summary>
    /// Aggressiveness of adaptation (subtle, moderate, aggressive)
    /// </summary>
    public AdaptationAggressiveness Aggressiveness { get; init; } = AdaptationAggressiveness.Moderate;

    /// <summary>
    /// Whether to preserve original meaning strictly
    /// </summary>
    public bool PreserveSemantics { get; init; } = true;

    /// <summary>
    /// Minimum number of examples per key concept
    /// </summary>
    public int MinExamplesPerConcept { get; init; } = 3;

    /// <summary>
    /// Maximum number of examples per key concept
    /// </summary>
    public int MaxExamplesPerConcept { get; init; } = 5;

    /// <summary>
    /// Whether to add definitions for complex terms
    /// </summary>
    public bool AddDefinitions { get; init; } = true;

    /// <summary>
    /// Whether to adjust pacing based on expertise
    /// </summary>
    public bool AdjustPacing { get; init; } = true;

    /// <summary>
    /// Whether to personalize examples
    /// </summary>
    public bool PersonalizeExamples { get; init; } = true;

    /// <summary>
    /// Whether to adjust tone and formality
    /// </summary>
    public bool AdjustTone { get; init; } = true;

    /// <summary>
    /// Whether to balance cognitive load
    /// </summary>
    public bool BalanceCognitiveLoad { get; init; } = true;
}

/// <summary>
/// Aggressiveness levels for content adaptation
/// </summary>
public enum AdaptationAggressiveness
{
    /// <summary>
    /// Minimal changes, preserve most of original style
    /// </summary>
    Subtle,

    /// <summary>
    /// Balanced adaptation with noticeable but not dramatic changes
    /// </summary>
    Moderate,

    /// <summary>
    /// Aggressive adaptation prioritizing audience fit over original style
    /// </summary>
    Aggressive
}

/// <summary>
/// Request for content adaptation
/// </summary>
public record AdaptationRequest
{
    /// <summary>
    /// Original content to adapt
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Adaptation configuration
    /// </summary>
    public AdaptationConfig Config { get; init; } = new();

    /// <summary>
    /// Context about the content (topic, purpose, etc.)
    /// </summary>
    public string? Context { get; init; }
}
