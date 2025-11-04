using System;
using System.Collections.Generic;
using Aura.Core.Models.Assets;

namespace Aura.Core.Models.StockMedia;

/// <summary>
/// Stock media type
/// </summary>
public enum StockMediaType
{
    Image,
    Video
}

/// <summary>
/// Stock media provider
/// </summary>
public enum StockMediaProvider
{
    Pexels,
    Unsplash,
    Pixabay
}

/// <summary>
/// Result from stock media search with licensing information
/// </summary>
public record StockMediaResult
{
    public string Id { get; init; } = string.Empty;
    public StockMediaType Type { get; init; }
    public StockMediaProvider Provider { get; init; }
    public string ThumbnailUrl { get; init; } = string.Empty;
    public string PreviewUrl { get; init; } = string.Empty;
    public string FullSizeUrl { get; init; } = string.Empty;
    public string? DownloadUrl { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public TimeSpan? Duration { get; init; }
    public AssetLicensingInfo Licensing { get; init; } = new();
    public Dictionary<string, string> Metadata { get; init; } = new();
    public string? PerceptualHash { get; init; }
    public double RelevanceScore { get; init; }
}

/// <summary>
/// Request for stock media search
/// </summary>
public record StockMediaSearchRequest
{
    public string Query { get; init; } = string.Empty;
    public StockMediaType? Type { get; init; }
    public List<StockMediaProvider> Providers { get; init; } = new();
    public int Count { get; init; } = 10;
    public int Page { get; init; } = 1;
    public bool SafeSearchEnabled { get; init; } = true;
    public string? Orientation { get; init; }
    public string? Color { get; init; }
    public int? MinWidth { get; init; }
    public int? MinHeight { get; init; }
    public TimeSpan? MinDuration { get; init; }
    public TimeSpan? MaxDuration { get; init; }
}

/// <summary>
/// Response from stock media search
/// </summary>
public record StockMediaSearchResponse
{
    public List<StockMediaResult> Results { get; init; } = new();
    public int TotalResults { get; init; }
    public int Page { get; init; }
    public int PerPage { get; init; }
    public Dictionary<StockMediaProvider, int> ResultsByProvider { get; init; } = new();
    public string? NextPageToken { get; init; }
}

/// <summary>
/// Request for LLM-assisted query composition
/// </summary>
public record QueryCompositionRequest
{
    public string SceneDescription { get; init; } = string.Empty;
    public string[] Keywords { get; init; } = Array.Empty<string>();
    public StockMediaProvider TargetProvider { get; init; }
    public StockMediaType MediaType { get; init; }
    public string? Style { get; init; }
    public string? Mood { get; init; }
}

/// <summary>
/// Result from LLM query composition
/// </summary>
public record QueryCompositionResult
{
    public string PrimaryQuery { get; init; } = string.Empty;
    public List<string> AlternativeQueries { get; init; } = new();
    public List<string> NegativeFilters { get; init; } = new();
    public string Reasoning { get; init; } = string.Empty;
    public double Confidence { get; init; }
}

/// <summary>
/// Request for blend set recommendation (mix of stock vs generative)
/// </summary>
public record BlendSetRequest
{
    public List<string> SceneDescriptions { get; init; } = new();
    public string VideoGoal { get; init; } = string.Empty;
    public string VideoStyle { get; init; } = string.Empty;
    public int Budget { get; init; }
    public bool AllowGenerative { get; init; } = true;
    public bool AllowStock { get; init; } = true;
}

/// <summary>
/// Recommendation for visual asset blend
/// </summary>
public record BlendSetRecommendation
{
    public Dictionary<int, SourceRecommendation> SceneRecommendations { get; init; } = new();
    public string Strategy { get; init; } = string.Empty;
    public string Reasoning { get; init; } = string.Empty;
    public double EstimatedCost { get; init; }
    public double NarrativeCoverageScore { get; init; }
}

/// <summary>
/// Source recommendation for a scene
/// </summary>
public record SourceRecommendation
{
    public bool UseStock { get; init; }
    public bool UseGenerative { get; init; }
    public string PreferredSource { get; init; } = string.Empty;
    public string Reasoning { get; init; } = string.Empty;
    public double Confidence { get; init; }
}

/// <summary>
/// Content safety filter configuration
/// </summary>
public record ContentSafetyFilters
{
    public bool EnabledFilters { get; init; } = true;
    public bool BlockExplicitContent { get; init; } = true;
    public bool BlockViolentContent { get; init; } = true;
    public bool BlockSensitiveTopics { get; init; } = true;
    public List<string> BlockedKeywords { get; init; } = new();
    public List<string> AllowedKeywords { get; init; } = new();
    public int SafetyLevel { get; init; } = 5;
}

/// <summary>
/// Rate limit status for a provider
/// </summary>
public record RateLimitStatus
{
    public StockMediaProvider Provider { get; init; }
    public int RequestsRemaining { get; init; }
    public int RequestsLimit { get; init; }
    public DateTime? ResetTime { get; init; }
    public bool IsLimited { get; init; }
}
