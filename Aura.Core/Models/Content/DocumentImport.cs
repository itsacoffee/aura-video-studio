using System;
using System.Collections.Generic;
using Aura.Core.Models.Audience;

namespace Aura.Core.Models.Content;

/// <summary>
/// Supported document formats for import
/// </summary>
public enum DocumentFormat
{
    PlainText,
    Markdown,
    Word,
    Pdf,
    GoogleDocs,
    Html,
    Json,
    AuraScript
}

/// <summary>
/// Metadata extracted from an imported document
/// </summary>
public record DocumentMetadata
{
    public string OriginalFileName { get; init; } = string.Empty;
    public DocFormat Format { get; init; }
    public long FileSizeBytes { get; init; }
    public DateTime ImportedAt { get; init; } = DateTime.UtcNow;
    public int WordCount { get; init; }
    public int CharacterCount { get; init; }
    public string? DetectedLanguage { get; init; }
    public string? Title { get; init; }
    public string? Author { get; init; }
    public DateTime? CreatedDate { get; init; }
    public Dictionary<string, string> CustomMetadata { get; init; } = new();
}

/// <summary>
/// Hierarchical structure of a document
/// </summary>
public record DocumentStructure
{
    public List<DocumentSection> Sections { get; init; } = new();
    public int HeadingLevels { get; init; }
    public List<string> KeyConcepts { get; init; } = new();
    public DocumentComplexity Complexity { get; init; } = new();
    public DocumentTone Tone { get; init; } = new();
}

/// <summary>
/// A section within a document (heading + content)
/// </summary>
public record DocumentSection
{
    public int Level { get; init; }
    public string Heading { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public List<DocumentSection> Subsections { get; init; } = new();
    public List<string> Examples { get; init; } = new();
    public List<string> VisualOpportunities { get; init; } = new();
    public int WordCount { get; init; }
    public TimeSpan EstimatedSpeechDuration { get; init; }
}

/// <summary>
/// Document complexity analysis
/// </summary>
public record DocumentComplexity
{
    public double ReadingLevel { get; init; }
    public double TechnicalDensity { get; init; }
    public double AbstractionLevel { get; init; }
    public int AverageSentenceLength { get; init; }
    public int ComplexWordCount { get; init; }
    public string ComplexityDescription { get; init; } = string.Empty;
}

/// <summary>
/// Detected tone of the document
/// </summary>
public record DocumentTone
{
    public string PrimaryTone { get; init; } = string.Empty;
    public double FormalityLevel { get; init; }
    public string WritingStyle { get; init; } = string.Empty;
    public List<string> ToneIndicators { get; init; } = new();
}

/// <summary>
/// Inferred target audience from document analysis
/// </summary>
public record InferredAudience
{
    public string EducationLevel { get; init; } = string.Empty;
    public string ExpertiseLevel { get; init; } = string.Empty;
    public List<string> PossibleProfessions { get; init; } = new();
    public string AgeRange { get; init; } = string.Empty;
    public double ConfidenceScore { get; init; }
    public string Reasoning { get; init; } = string.Empty;
}

/// <summary>
/// Result of document import operation
/// </summary>
public record DocumentImportResult
{
    public bool Success { get; init; }
    public DocumentMetadata Metadata { get; init; } = new();
    public DocumentStructure Structure { get; init; } = new();
    public string RawContent { get; init; } = string.Empty;
    public InferredAudience? InferredAudience { get; init; }
    public List<string> Warnings { get; init; } = new();
    public string? ErrorMessage { get; init; }
    public TimeSpan ProcessingTime { get; init; }
}

/// <summary>
/// Configuration for document to script conversion
/// </summary>
public record ConversionConfig
{
    public ConversionPreset Preset { get; init; } = ConversionPreset.Generic;
    public TimeSpan TargetDuration { get; init; } = TimeSpan.FromMinutes(3);
    public int WordsPerMinute { get; init; } = 150;
    public bool EnableAudienceRetargeting { get; init; } = true;
    public bool EnableVisualSuggestions { get; init; } = true;
    public bool PreserveOriginalStructure { get; init; } = false;
    public bool AddTransitions { get; init; } = true;
    public double AggressivenessLevel { get; init; } = 0.6;
    public string? TargetAudienceProfileId { get; init; }
}

/// <summary>
/// Predefined conversion presets for common use cases
/// </summary>
public enum ConversionPreset
{
    Generic,
    BlogToYouTube,
    TechnicalToExplainer,
    AcademicToEducational,
    NewsToSegment,
    TutorialToHowTo,
    Custom
}

/// <summary>
/// Details about a conversion preset
/// </summary>
public record PresetDefinition
{
    public ConversionPreset Type { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ConversionConfig DefaultConfig { get; init; } = new();
    public List<string> BestForFormats { get; init; } = new();
    public string RestructuringStrategy { get; init; } = string.Empty;
}

/// <summary>
/// Result of document to script conversion
/// </summary>
public record ConversionResult
{
    public bool Success { get; init; }
    public List<Scene> Scenes { get; init; } = new();
    public Brief SuggestedBrief { get; init; } = new("", null, null, "professional", "en", Aspect.Widescreen16x9);
    public List<ConversionChange> Changes { get; init; } = new();
    public ConversionMetrics Metrics { get; init; } = new();
    public List<SectionConversion> SectionConversions { get; init; } = new();
    public string? ErrorMessage { get; init; }
    public TimeSpan ProcessingTime { get; init; }
}

/// <summary>
/// A change made during conversion
/// </summary>
public record ConversionChange
{
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Justification { get; init; } = string.Empty;
    public int SectionIndex { get; init; }
    public double ImpactLevel { get; init; }
}

/// <summary>
/// Metrics about the conversion
/// </summary>
public record ConversionMetrics
{
    public int OriginalWordCount { get; init; }
    public int ConvertedWordCount { get; init; }
    public double CompressionRatio { get; init; }
    public int SectionsCreated { get; init; }
    public int TransitionsAdded { get; init; }
    public int VisualSuggestionsGenerated { get; init; }
    public double OverallConfidenceScore { get; init; }
}

/// <summary>
/// Conversion details for a single section
/// </summary>
public record SectionConversion
{
    public int SectionIndex { get; init; }
    public string OriginalHeading { get; init; } = string.Empty;
    public string ConvertedHeading { get; init; } = string.Empty;
    public string OriginalContent { get; init; } = string.Empty;
    public string ConvertedContent { get; init; } = string.Empty;
    public double ConfidenceScore { get; init; }
    public bool RequiresManualReview { get; init; }
    public List<string> ChangeHighlights { get; init; } = new();
    public string Reasoning { get; init; } = string.Empty;
}

/// <summary>
/// Visual opportunity identified in document
/// </summary>
public record VisualOpportunity
{
    public string Description { get; init; } = string.Empty;
    public string Context { get; init; } = string.Empty;
    public List<string> SuggestedKeywords { get; init; } = new();
    public string VisualType { get; init; } = string.Empty;
    public double RelevanceScore { get; init; }
}

/// <summary>
/// B-roll suggestion for a scene
/// </summary>
public record BRollSuggestion
{
    public int SceneIndex { get; init; }
    public string Description { get; init; } = string.Empty;
    public List<string> SearchKeywords { get; init; } = new();
    public TimeSpan StartTime { get; init; }
    public TimeSpan Duration { get; init; }
    public string Purpose { get; init; } = string.Empty;
}
