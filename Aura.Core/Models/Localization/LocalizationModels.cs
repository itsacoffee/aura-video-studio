using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Localization;

/// <summary>
/// Comprehensive language information with cultural context
/// </summary>
public class LanguageInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NativeName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public bool IsRightToLeft { get; set; }
    public FormalityLevel DefaultFormality { get; set; } = FormalityLevel.Neutral;
    public double TypicalExpansionFactor { get; set; } = 1.0;
    public List<string> CulturalSensitivities { get; set; } = new();
}

/// <summary>
/// Formality level for different cultures
/// </summary>
public enum FormalityLevel
{
    VeryInformal,
    Informal,
    Neutral,
    Formal,
    VeryFormal
}

/// <summary>
/// Translation request with cultural context
/// </summary>
public class TranslationRequest
{
    public string SourceLanguage { get; set; } = "en";
    public string TargetLanguage { get; set; } = string.Empty;
    public string SourceText { get; set; } = string.Empty;
    public List<ScriptLine> ScriptLines { get; set; } = new();
    public CulturalContext? CulturalContext { get; set; }
    public TranslationOptions Options { get; set; } = new();
    public Dictionary<string, string> Glossary { get; set; } = new();
    public string? AudienceProfileId { get; set; }
    
    /// <summary>
    /// RAG configuration for terminology grounding.
    /// When enabled, the translation service will retrieve relevant terminology
    /// and context from indexed documents to ensure consistency.
    /// </summary>
    public Models.RagConfiguration? RagConfiguration { get; set; }
    
    /// <summary>
    /// Domain-specific context for better translation accuracy.
    /// Examples: "medical", "legal", "technical", "marketing"
    /// </summary>
    public string? Domain { get; set; }
    
    /// <summary>
    /// Industry-specific terminology database ID for specialized translations.
    /// </summary>
    public string? IndustryGlossaryId { get; set; }
}

/// <summary>
/// Cultural context for translation
/// </summary>
public class CulturalContext
{
    public string TargetRegion { get; set; } = string.Empty;
    public FormalityLevel TargetFormality { get; set; } = FormalityLevel.Neutral;
    public Audience.CommunicationStyle PreferredStyle { get; set; } = Audience.CommunicationStyle.Professional;
    public List<string> Sensitivities { get; set; } = new();
    public List<string> TabooTopics { get; set; } = new();
    public AgeRating ContentRating { get; set; } = AgeRating.General;
}

/// <summary>
/// Age rating for content filtering
/// </summary>
public enum AgeRating
{
    General,
    Teen,
    Adult
}

/// <summary>
/// Translation options and quality settings
/// </summary>
public class TranslationOptions
{
    public TranslationMode Mode { get; set; } = TranslationMode.Localized;
    public bool EnableBackTranslation { get; set; } = true;
    public bool EnableQualityScoring { get; set; } = true;
    public bool AdjustTimings { get; set; } = true;
    public double MaxTimingVariance { get; set; } = 0.15;
    public bool PreserveNames { get; set; } = true;
    public bool PreserveBrands { get; set; } = true;
    public bool AdaptMeasurements { get; set; } = true;
    public string? TranscreationContext { get; set; }
}

/// <summary>
/// Translation mode
/// </summary>
public enum TranslationMode
{
    Literal,
    Localized,
    Transcreation
}

/// <summary>
/// Comprehensive translation result
/// </summary>
public class TranslationResult
{
    public string SourceLanguage { get; set; } = string.Empty;
    public string TargetLanguage { get; set; } = string.Empty;
    public string SourceText { get; set; } = string.Empty;
    public string TranslatedText { get; set; } = string.Empty;
    public List<TranslatedScriptLine> TranslatedLines { get; set; } = new();
    public TranslationQuality Quality { get; set; } = new();
    public List<CulturalAdaptation> CulturalAdaptations { get; set; } = new();
    public TimingAdjustment TimingAdjustment { get; set; } = new();
    public List<VisualLocalizationRecommendation> VisualRecommendations { get; set; } = new();
    public Dictionary<string, string> TerminologyUsed { get; set; } = new();
    public DateTime TranslatedAt { get; set; } = DateTime.UtcNow;
    public double TranslationTimeSeconds { get; set; }

    /// <summary>
    /// Translation quality metrics for monitoring performance and output quality
    /// </summary>
    public TranslationMetrics? Metrics { get; set; }
}

/// <summary>
/// Translated script line with timing information
/// </summary>
public class TranslatedScriptLine
{
    public int SceneIndex { get; set; }
    public string SourceText { get; set; } = string.Empty;
    public string TranslatedText { get; set; } = string.Empty;
    public double OriginalStartSeconds { get; set; }
    public double OriginalDurationSeconds { get; set; }
    public double AdjustedStartSeconds { get; set; }
    public double AdjustedDurationSeconds { get; set; }
    public double TimingVariance { get; set; }
    public List<string> AdaptationNotes { get; set; } = new();
}

/// <summary>
/// Translation quality metrics
/// </summary>
public class TranslationQuality
{
    public double OverallScore { get; set; }
    public double FluencyScore { get; set; }
    public double AccuracyScore { get; set; }
    public double CulturalAppropriatenessScore { get; set; }
    public double TerminologyConsistencyScore { get; set; }
    public double BackTranslationScore { get; set; }
    public string? BackTranslatedText { get; set; }
    public List<QualityIssue> Issues { get; set; } = new();
}

/// <summary>
/// Quality issue detected during translation
/// </summary>
public class QualityIssue
{
    public QualityIssueSeverity Severity { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Suggestion { get; set; }
    public int? LineNumber { get; set; }
}

/// <summary>
/// Severity of quality issues
/// </summary>
public enum QualityIssueSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Cultural adaptation made during translation
/// </summary>
public class CulturalAdaptation
{
    public string Category { get; set; } = string.Empty;
    public string SourcePhrase { get; set; } = string.Empty;
    public string AdaptedPhrase { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public int? LineNumber { get; set; }
}

/// <summary>
/// Timing adjustment information
/// </summary>
public class TimingAdjustment
{
    public double OriginalTotalDuration { get; set; }
    public double AdjustedTotalDuration { get; set; }
    public double ExpansionFactor { get; set; }
    public bool RequiresCompression { get; set; }
    public List<string> CompressionSuggestions { get; set; } = new();
    public List<TimingWarning> Warnings { get; set; } = new();
}

/// <summary>
/// Timing warning
/// </summary>
public class TimingWarning
{
    public TimingWarningSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? LineNumber { get; set; }
}

/// <summary>
/// Timing warning severity
/// </summary>
public enum TimingWarningSeverity
{
    Info,
    Warning,
    Critical
}

/// <summary>
/// Visual localization recommendation
/// </summary>
public class VisualLocalizationRecommendation
{
    public VisualElementType ElementType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public LocalizationPriority Priority { get; set; }
    public int? SceneIndex { get; set; }
}

/// <summary>
/// Visual element types that may need localization
/// </summary>
public enum VisualElementType
{
    TextInImage,
    CulturalSymbol,
    Gesture,
    ColorMeaning,
    RegionalImagery,
    BrandLogo
}

/// <summary>
/// Localization priority
/// </summary>
public enum LocalizationPriority
{
    Optional,
    Recommended,
    Important,
    Critical
}

/// <summary>
/// Glossary entry for terminology management
/// </summary>
public class GlossaryEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Term { get; set; } = string.Empty;
    public Dictionary<string, string> Translations { get; set; } = new();
    public string? Context { get; set; }
    public string? Industry { get; set; }
    public bool PreserveCapitalization { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Project-specific glossary
/// </summary>
public class ProjectGlossary
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<GlossaryEntry> Entries { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Batch translation request
/// </summary>
public class BatchTranslationRequest
{
    public string SourceLanguage { get; set; } = "en";
    public List<string> TargetLanguages { get; set; } = new();
    public string SourceText { get; set; } = string.Empty;
    public List<ScriptLine> ScriptLines { get; set; } = new();
    public CulturalContext? CulturalContext { get; set; }
    public TranslationOptions Options { get; set; } = new();
    public Dictionary<string, string> Glossary { get; set; } = new();
}

/// <summary>
/// Batch translation result
/// </summary>
public class BatchTranslationResult
{
    public string SourceLanguage { get; set; } = string.Empty;
    public Dictionary<string, TranslationResult> Translations { get; set; } = new();
    public List<string> SuccessfulLanguages { get; set; } = new();
    public List<string> FailedLanguages { get; set; } = new();
    public double TotalTimeSeconds { get; set; }
}

/// <summary>
/// Cultural analysis request
/// </summary>
public class CulturalAnalysisRequest
{
    public string TargetLanguage { get; set; } = string.Empty;
    public string TargetRegion { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? AudienceProfileId { get; set; }
}

/// <summary>
/// Cultural analysis result
/// </summary>
public class CulturalAnalysisResult
{
    public string TargetLanguage { get; set; } = string.Empty;
    public string TargetRegion { get; set; } = string.Empty;
    public double CulturalSensitivityScore { get; set; }
    public List<CulturalIssue> Issues { get; set; } = new();
    public List<CulturalRecommendation> Recommendations { get; set; } = new();
    public Dictionary<string, string> SuggestedAdaptations { get; set; } = new();
}

/// <summary>
/// Cultural issue identified in content
/// </summary>
public class CulturalIssue
{
    public CulturalIssueSeverity Severity { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public string? Suggestion { get; set; }
}

/// <summary>
/// Cultural issue severity
/// </summary>
public enum CulturalIssueSeverity
{
    Minor,
    Moderate,
    Significant,
    Critical
}

/// <summary>
/// Cultural recommendation
/// </summary>
public class CulturalRecommendation
{
    public string Category { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public LocalizationPriority Priority { get; set; }
}
