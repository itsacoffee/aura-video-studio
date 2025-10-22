using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Platform;

/// <summary>
/// Represents a social media platform profile with its specifications and requirements
/// </summary>
public class PlatformProfile
{
    public string PlatformId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PlatformRequirements Requirements { get; set; } = new();
    public PlatformBestPractices BestPractices { get; set; } = new();
    public PlatformAlgorithmFactors AlgorithmFactors { get; set; } = new();
}

/// <summary>
/// Technical requirements for a platform
/// </summary>
public class PlatformRequirements
{
    public List<AspectRatioSpec> SupportedAspectRatios { get; set; } = new();
    public VideoSpecs Video { get; set; } = new();
    public ThumbnailSpecs Thumbnail { get; set; } = new();
    public MetadataLimits Metadata { get; set; } = new();
    public List<string> SupportedFormats { get; set; } = new();
}

/// <summary>
/// Aspect ratio specification
/// </summary>
public class AspectRatioSpec
{
    public string Ratio { get; set; } = string.Empty; // e.g., "16:9", "9:16", "1:1", "4:5"
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsPreferred { get; set; }
    public string UseCase { get; set; } = string.Empty; // e.g., "feed", "stories", "shorts"
}

/// <summary>
/// Video specifications
/// </summary>
public class VideoSpecs
{
    public int MinDurationSeconds { get; set; }
    public int MaxDurationSeconds { get; set; }
    public int OptimalMinDurationSeconds { get; set; }
    public int OptimalMaxDurationSeconds { get; set; }
    public long MaxFileSizeBytes { get; set; }
    public List<string> RecommendedCodecs { get; set; } = new();
    public int MaxBitrate { get; set; }
    public int RecommendedBitrate { get; set; }
    public List<string> RequiredFrameRates { get; set; } = new();
}

/// <summary>
/// Thumbnail specifications
/// </summary>
public class ThumbnailSpecs
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int MinWidth { get; set; }
    public int MinHeight { get; set; }
    public long MaxFileSizeBytes { get; set; }
    public List<string> SupportedFormats { get; set; } = new();
    public bool TextOverlayRecommended { get; set; }
    public string SafeAreaDescription { get; set; } = string.Empty;
}

/// <summary>
/// Metadata character limits
/// </summary>
public class MetadataLimits
{
    public int TitleMaxLength { get; set; }
    public int DescriptionMaxLength { get; set; }
    public int MaxTags { get; set; }
    public int MaxHashtags { get; set; }
    public int HashtagMaxLength { get; set; }
}

/// <summary>
/// Platform-specific best practices
/// </summary>
public class PlatformBestPractices
{
    public int HookDurationSeconds { get; set; }
    public string HookStrategy { get; set; } = string.Empty;
    public string ContentPacing { get; set; } = string.Empty;
    public bool CaptionsRequired { get; set; }
    public bool MusicImportant { get; set; }
    public bool TextOverlayEffective { get; set; }
    public string ToneAndStyle { get; set; } = string.Empty;
    public List<string> ContentStrategies { get; set; } = new();
    public string OptimalPostingTimes { get; set; } = string.Empty;
}

/// <summary>
/// Algorithm ranking factors
/// </summary>
public class PlatformAlgorithmFactors
{
    public List<RankingFactor> Factors { get; set; } = new();
    public string AlgorithmType { get; set; } = string.Empty; // e.g., "engagement", "watch_time", "shares"
    public bool FavorsNewContent { get; set; }
    public int TypicalViralTimeframeHours { get; set; }
}

/// <summary>
/// Individual ranking factor
/// </summary>
public class RankingFactor
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Weight { get; set; } // 1-10 importance
}

/// <summary>
/// Platform optimization request
/// </summary>
public class PlatformOptimizationRequest
{
    public string SourceVideoPath { get; set; } = string.Empty;
    public string TargetPlatform { get; set; } = string.Empty;
    public string? PreferredAspectRatio { get; set; }
    public bool AutoCrop { get; set; } = true;
    public bool OptimizeMetadata { get; set; } = true;
    public bool GenerateThumbnail { get; set; } = true;
}

/// <summary>
/// Platform optimization result
/// </summary>
public class PlatformOptimizationResult
{
    public string OptimizedVideoPath { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public OptimizedMetadata Metadata { get; set; } = new();
    public List<string> AppliedOptimizations { get; set; } = new();
    public Dictionary<string, string> TechnicalSpecs { get; set; } = new();
}

/// <summary>
/// Optimized metadata for a platform
/// </summary>
public class OptimizedMetadata
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<string> Hashtags { get; set; } = new();
    public string Category { get; set; } = string.Empty;
    public string CallToAction { get; set; } = string.Empty;
    public Dictionary<string, object> CustomFields { get; set; } = new();
}

/// <summary>
/// Metadata generation request
/// </summary>
public class MetadataGenerationRequest
{
    public string Platform { get; set; } = string.Empty;
    public string VideoTitle { get; set; } = string.Empty;
    public string VideoDescription { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = new();
    public string TargetAudience { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}

/// <summary>
/// Thumbnail suggestion request
/// </summary>
public class ThumbnailSuggestionRequest
{
    public string Platform { get; set; } = string.Empty;
    public string VideoContent { get; set; } = string.Empty;
    public string TargetEmotion { get; set; } = string.Empty;
    public bool IncludeText { get; set; } = true;
    public List<string> KeyElements { get; set; } = new();
}

/// <summary>
/// Thumbnail concept
/// </summary>
public class ThumbnailConcept
{
    public string ConceptId { get; set; } = Guid.NewGuid().ToString();
    public string Description { get; set; } = string.Empty;
    public string Composition { get; set; } = string.Empty;
    public string TextOverlay { get; set; } = string.Empty;
    public string ColorScheme { get; set; } = string.Empty;
    public double PredictedCTR { get; set; } // 0-1
    public List<string> DesignElements { get; set; } = new();
}

/// <summary>
/// Keyword research request
/// </summary>
public class KeywordResearchRequest
{
    public string Topic { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public bool IncludeLongTail { get; set; } = true;
}

/// <summary>
/// Keyword research result
/// </summary>
public class KeywordResearchResult
{
    public List<KeywordData> Keywords { get; set; } = new();
    public List<KeywordCluster> Clusters { get; set; } = new();
    public List<string> TrendingTerms { get; set; } = new();
}

/// <summary>
/// Keyword data
/// </summary>
public class KeywordData
{
    public string Keyword { get; set; } = string.Empty;
    public long SearchVolume { get; set; }
    public string Difficulty { get; set; } = string.Empty; // "low", "medium", "high"
    public double Relevance { get; set; } // 0-1
    public List<string> RelatedTerms { get; set; } = new();
    public string SearchIntent { get; set; } = string.Empty; // "informational", "navigational", "transactional"
}

/// <summary>
/// Semantic keyword cluster
/// </summary>
public class KeywordCluster
{
    public string ClusterName { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = new();
    public string Intent { get; set; } = string.Empty;
}

/// <summary>
/// Optimal posting time request
/// </summary>
public class OptimalPostingTimeRequest
{
    public string Platform { get; set; } = string.Empty;
    public string Timezone { get; set; } = "UTC";
    public List<string> TargetRegions { get; set; } = new();
    public string ContentType { get; set; } = string.Empty;
}

/// <summary>
/// Optimal posting time result
/// </summary>
public class OptimalPostingTimeResult
{
    public List<PostingTimeSlot> RecommendedTimes { get; set; } = new();
    public Dictionary<string, string> ActivityPatterns { get; set; } = new();
    public string Reasoning { get; set; } = string.Empty;
}

/// <summary>
/// Posting time slot
/// </summary>
public class PostingTimeSlot
{
    public DayOfWeek Day { get; set; }
    public int Hour { get; set; }
    public int Minute { get; set; }
    public double EngagementScore { get; set; } // 0-1
    public string Timezone { get; set; } = "UTC";
}

/// <summary>
/// Multi-platform export request
/// </summary>
public class MultiPlatformExportRequest
{
    public string SourceVideoPath { get; set; } = string.Empty;
    public List<string> TargetPlatforms { get; set; } = new();
    public bool OptimizeForEach { get; set; } = true;
    public bool GenerateMetadata { get; set; } = true;
    public bool GenerateThumbnails { get; set; } = true;
}

/// <summary>
/// Multi-platform export result
/// </summary>
public class MultiPlatformExportResult
{
    public Dictionary<string, PlatformExport> Exports { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Individual platform export
/// </summary>
public class PlatformExport
{
    public string Platform { get; set; } = string.Empty;
    public string VideoPath { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public OptimizedMetadata Metadata { get; set; } = new();
    public bool Success { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Platform trend data
/// </summary>
public class PlatformTrend
{
    public string TrendId { get; set; } = Guid.NewGuid().ToString();
    public string Platform { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double PopularityScore { get; set; } // 0-1
    public DateTime StartDate { get; set; }
    public string Duration { get; set; } = string.Empty;
    public List<string> RelatedHashtags { get; set; } = new();
    public List<string> PopularCreators { get; set; } = new();
}

/// <summary>
/// Content adaptation request for different platforms
/// </summary>
public class ContentAdaptationRequest
{
    public string SourceVideoPath { get; set; } = string.Empty;
    public string SourcePlatform { get; set; } = string.Empty;
    public string TargetPlatform { get; set; } = string.Empty;
    public bool AdaptPacing { get; set; } = true;
    public bool AdaptHook { get; set; } = true;
    public bool AdaptFormat { get; set; } = true;
}

/// <summary>
/// Content adaptation result
/// </summary>
public class ContentAdaptationResult
{
    public string AdaptedVideoPath { get; set; } = string.Empty;
    public List<string> ChangesApplied { get; set; } = new();
    public string AdaptationStrategy { get; set; } = string.Empty;
    public Dictionary<string, string> Recommendations { get; set; } = new();
}
