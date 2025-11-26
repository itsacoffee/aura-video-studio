using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Assets;

/// <summary>
/// Type of asset in the library
/// </summary>
public enum AssetType
{
    Image,
    Video,
    Audio
}

/// <summary>
/// Source from which the asset was obtained
/// </summary>
public enum AssetSource
{
    Uploaded,
    StockPexels,
    StockPixabay,
    AIGenerated,
    Sample
}

/// <summary>
/// Metadata for an asset including technical details and semantic information
/// </summary>
public record AssetMetadata
{
    // Technical metadata
    public int? Width { get; init; }
    public int? Height { get; init; }
    public TimeSpan? Duration { get; init; }
    public long? FileSizeBytes { get; init; }
    public string? Format { get; init; }
    public string? Codec { get; init; }
    public int? Bitrate { get; init; }
    public int? SampleRate { get; init; }
    public Dictionary<string, string> Extra { get; init; } = new();

    // Semantic metadata for LLM-intelligent selection
    public string? Description { get; init; }
    public string? DominantColor { get; init; }
    public string? Mood { get; init; }
    public string? Subject { get; init; }
    public float[]? Embedding { get; init; }
    public DateTime? TaggedAt { get; init; }
}

/// <summary>
/// Category of asset tags for classification
/// </summary>
public enum TagCategory
{
    Subject,
    Style,
    Mood,
    Color,
    Setting,
    Action,
    Object,
    Custom
}

/// <summary>
/// A tag with confidence score and category for LLM-intelligent selection
/// </summary>
public record AssetTag(
    string Name,
    float Confidence = 1.0f,
    TagCategory Category = TagCategory.Custom)
{
    public string Name { get; init; } = Name.ToLowerInvariant();
}

/// <summary>
/// Asset record in the library
/// </summary>
public record Asset
{
    public Guid Id { get; init; }
    public AssetType Type { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public string? ThumbnailPath { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public List<AssetTag> Tags { get; init; } = new();
    public AssetSource Source { get; init; }
    public AssetMetadata Metadata { get; init; } = new();
    public DateTime DateAdded { get; init; }
    public DateTime DateModified { get; init; }
    public int UsageCount { get; init; }
    public List<string> Collections { get; init; } = new();
    public string? DominantColor { get; init; }
    public AssetLicensingInfo? Licensing { get; init; }
}

/// <summary>
/// Collection of assets
/// </summary>
public record AssetCollection
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Color { get; init; } = "#0078D4";
    public DateTime DateCreated { get; init; }
    public DateTime DateModified { get; init; }
    public List<Guid> AssetIds { get; init; } = new();
}

/// <summary>
/// Search filters for assets
/// </summary>
public record AssetSearchFilters
{
    public AssetType? Type { get; init; }
    public List<string> Tags { get; init; } = new();
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int? MinWidth { get; init; }
    public int? MaxWidth { get; init; }
    public int? MinHeight { get; init; }
    public int? MaxHeight { get; init; }
    public TimeSpan? MinDuration { get; init; }
    public TimeSpan? MaxDuration { get; init; }
    public AssetSource? Source { get; init; }
    public List<string> Collections { get; init; } = new();
    public bool? UsedInTimeline { get; init; }
}

/// <summary>
/// Search result with pagination
/// </summary>
public record AssetSearchResult
{
    public List<Asset> Assets { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

/// <summary>
/// Stock image from external provider
/// </summary>
public record StockImage
{
    public string ThumbnailUrl { get; init; } = string.Empty;
    public string FullSizeUrl { get; init; } = string.Empty;
    public string PreviewUrl { get; init; } = string.Empty;
    public string? Photographer { get; init; }
    public string? PhotographerUrl { get; init; }
    public string Source { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
}

/// <summary>
/// AI image generation request
/// </summary>
public record AIImageGenerationRequest
{
    public string Prompt { get; init; } = string.Empty;
    public string? NegativePrompt { get; init; }
    public string Style { get; init; } = "photorealistic";
    public string Size { get; init; } = "1024x1024";
    public int Steps { get; init; } = 30;
    public double CfgScale { get; init; } = 7.5;
    public int? Seed { get; init; }
}

/// <summary>
/// Licensing information for an asset
/// </summary>
public record AssetLicensingInfo
{
    public string LicenseType { get; init; } = string.Empty;
    public string? Attribution { get; init; }
    public string? LicenseUrl { get; init; }
    public bool CommercialUseAllowed { get; init; }
    public bool AttributionRequired { get; init; }
    public string? CreatorName { get; init; }
    public string? CreatorUrl { get; init; }
    public string SourcePlatform { get; init; } = string.Empty;
}

/// <summary>
/// Comprehensive semantic metadata for an asset, designed for LLM-intelligent selection.
/// This record contains all semantic information generated by asset tagging providers.
/// </summary>
public record SemanticAssetMetadata
{
    public Guid AssetId { get; init; }
    public List<AssetTag> Tags { get; init; } = new();
    public string? Description { get; init; }
    public string? DominantColor { get; init; }
    public string? Mood { get; init; }
    public string? Subject { get; init; }
    public float[]? Embedding { get; init; }
    public DateTime TaggedAt { get; init; }
    public string? TaggingProvider { get; init; }
    public float? ConfidenceScore { get; init; }
}

/// <summary>
/// Result of an asset tagging operation
/// </summary>
public record AssetTaggingResult
{
    public bool Success { get; init; }
    public SemanticAssetMetadata? Metadata { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan ProcessingTime { get; init; }
}

/// <summary>
/// Search query for semantic asset matching
/// </summary>
public record SemanticSearchQuery
{
    public string? TextQuery { get; init; }
    public float[]? QueryEmbedding { get; init; }
    public List<string>? RequiredTags { get; init; }
    public string? Mood { get; init; }
    public string? Subject { get; init; }
    public int MaxResults { get; init; } = 10;
    public float MinSimilarity { get; init; } = 0.5f;
}

/// <summary>
/// Result of a semantic search operation
/// </summary>
public record SemanticSearchResult
{
    public Guid AssetId { get; init; }
    public float SimilarityScore { get; init; }
    public List<string> MatchedTags { get; init; } = new();
    public string? MatchReason { get; init; }
}
