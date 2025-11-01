using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Audience;

/// <summary>
/// Configuration for content adaptation engine
/// </summary>
public class ContentAdaptationConfig
{
    /// <summary>
    /// How aggressively to adapt content (0.0-1.0)
    /// 0.3 = subtle, 0.6 = moderate, 0.9 = aggressive
    /// </summary>
    public double AggressivenessLevel { get; set; } = 0.6;

    /// <summary>
    /// Enable vocabulary level adjustment
    /// </summary>
    public bool EnableVocabularyAdjustment { get; set; } = true;

    /// <summary>
    /// Enable example personalization
    /// </summary>
    public bool EnableExamplePersonalization { get; set; } = true;

    /// <summary>
    /// Enable pacing adaptation
    /// </summary>
    public bool EnablePacingAdaptation { get; set; } = true;

    /// <summary>
    /// Enable tone and formality optimization
    /// </summary>
    public bool EnableToneOptimization { get; set; } = true;

    /// <summary>
    /// Enable cognitive load balancing
    /// </summary>
    public bool EnableCognitiveLoadBalancing { get; set; } = true;

    /// <summary>
    /// Target cognitive load threshold (0-100)
    /// </summary>
    public double CognitiveLoadThreshold { get; set; } = 75.0;

    /// <summary>
    /// Number of examples per key concept (3-5 recommended)
    /// </summary>
    public int ExamplesPerConcept { get; set; } = 3;
}

/// <summary>
/// Result of content adaptation with before/after comparison
/// </summary>
public class ContentAdaptationResult
{
    public string OriginalContent { get; set; } = string.Empty;
    public string AdaptedContent { get; set; } = string.Empty;
    public List<AdaptationChange> Changes { get; set; } = new();
    public ReadabilityMetrics OriginalMetrics { get; set; } = new();
    public ReadabilityMetrics AdaptedMetrics { get; set; } = new();
    public double OverallRelevanceScore { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}

/// <summary>
/// Individual change made during adaptation
/// </summary>
public class AdaptationChange
{
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OriginalText { get; set; } = string.Empty;
    public string AdaptedText { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public int Position { get; set; }
}

/// <summary>
/// Readability and complexity metrics
/// </summary>
public class ReadabilityMetrics
{
    /// <summary>
    /// Flesch-Kincaid Grade Level (0-18+)
    /// </summary>
    public double FleschKincaidGradeLevel { get; set; }

    /// <summary>
    /// SMOG readability score (Simple Measure of Gobbledygook)
    /// </summary>
    public double SmogScore { get; set; }

    /// <summary>
    /// Average words per sentence
    /// </summary>
    public double AverageWordsPerSentence { get; set; }

    /// <summary>
    /// Average syllables per word
    /// </summary>
    public double AverageSyllablesPerWord { get; set; }

    /// <summary>
    /// Percentage of complex words (3+ syllables)
    /// </summary>
    public double ComplexWordPercentage { get; set; }

    /// <summary>
    /// Technical term density (0-100)
    /// </summary>
    public double TechnicalTermDensity { get; set; }

    /// <summary>
    /// Overall complexity score (0-100)
    /// </summary>
    public double OverallComplexity { get; set; }
}

/// <summary>
/// Vocabulary adjustment specification
/// </summary>
public class VocabularyAdjustment
{
    public string OriginalWord { get; set; } = string.Empty;
    public string ReplacementWord { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
    public bool RequiresDefinition { get; set; }
    public double ComplexityReduction { get; set; }
}

/// <summary>
/// Example personalization specification
/// </summary>
public class PersonalizedExample
{
    public string OriginalExample { get; set; } = string.Empty;
    public string AdaptedExample { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
    public string Category { get; set; } = string.Empty;
    public string GeographicRelevance { get; set; } = string.Empty;
}

/// <summary>
/// Pacing adjustment specification
/// </summary>
public class PacingAdjustment
{
    public double OriginalDuration { get; set; }
    public double AdjustedDuration { get; set; }
    public double SpeedMultiplier { get; set; }
    public bool AddedExplanation { get; set; }
    public bool RemovedRedundancy { get; set; }
    public string Reasoning { get; set; } = string.Empty;
}

/// <summary>
/// Tone and formality adjustment
/// </summary>
public class ToneAdjustment
{
    public string OriginalTone { get; set; } = string.Empty;
    public string TargetTone { get; set; } = string.Empty;
    public double FormalityLevel { get; set; }
    public double HumorLevel { get; set; }
    public double EnergyLevel { get; set; }
    public bool CulturallyAppropriate { get; set; }
    public double ConsistencyScore { get; set; }
}

/// <summary>
/// Cognitive load analysis per scene
/// </summary>
public class CognitiveLoadAnalysis
{
    public int SceneIndex { get; set; }
    public double LoadScore { get; set; }
    public double ConceptualComplexity { get; set; }
    public double VerbalComplexity { get; set; }
    public double VisualComplexity { get; set; }
    public bool ExceedsThreshold { get; set; }
    public bool RequiresBreather { get; set; }
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Audience-aware adaptation context for LLM prompts
/// </summary>
public class AudienceAdaptationContext
{
    public AudienceProfile Profile { get; set; } = new();
    public double TargetReadingLevel { get; set; }
    public List<string> PreferredAnalogies { get; set; } = new();
    public List<string> CulturalReferences { get; set; } = new();
    public double PacingMultiplier { get; set; }
    public string FormalityLevel { get; set; } = string.Empty;
    public double CognitiveCapacity { get; set; }
    public string CommunicationStyle { get; set; } = string.Empty;
}
