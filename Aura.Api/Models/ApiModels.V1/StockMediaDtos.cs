using System;
using System.Collections.Generic;

namespace Aura.Api.Models.ApiModels.V1;

/// <summary>
/// Request for stock media search
/// </summary>
public record StockMediaSearchRequestDto(
    string Query,
    string? MediaType,
    List<string> Providers,
    int Count = 10,
    int Page = 1,
    bool SafeSearchEnabled = true,
    string? Orientation = null,
    string? Color = null,
    int? MinWidth = null,
    int? MinHeight = null,
    int? MinDurationSeconds = null,
    int? MaxDurationSeconds = null
);

/// <summary>
/// Stock media search result with licensing info
/// </summary>
public record StockMediaResultDto(
    string Id,
    string Type,
    string Provider,
    string ThumbnailUrl,
    string PreviewUrl,
    string FullSizeUrl,
    string? DownloadUrl,
    int Width,
    int Height,
    int? DurationSeconds,
    LicensingInfoDto Licensing,
    Dictionary<string, string> Metadata,
    double RelevanceScore
);

/// <summary>
/// Stock media search response
/// </summary>
public record StockMediaSearchResponseDto(
    List<StockMediaResultDto> Results,
    int TotalResults,
    int Page,
    int PerPage,
    Dictionary<string, int> ResultsByProvider
);

/// <summary>
/// Request for LLM query composition
/// </summary>
public record QueryCompositionRequestDto(
    string SceneDescription,
    string[] Keywords,
    string TargetProvider,
    string MediaType,
    string? Style = null,
    string? Mood = null
);

/// <summary>
/// Result from query composition
/// </summary>
public record QueryCompositionResultDto(
    string PrimaryQuery,
    List<string> AlternativeQueries,
    List<string> NegativeFilters,
    string Reasoning,
    double Confidence
);

/// <summary>
/// Request for blend set recommendation
/// </summary>
public record BlendSetRequestDto(
    List<string> SceneDescriptions,
    string VideoGoal,
    string VideoStyle,
    int Budget,
    bool AllowGenerative = true,
    bool AllowStock = true
);

/// <summary>
/// Blend set recommendation result
/// </summary>
public record BlendSetRecommendationDto(
    Dictionary<int, SourceRecommendationDto> SceneRecommendations,
    string Strategy,
    string Reasoning,
    double EstimatedCost,
    double NarrativeCoverageScore
);

/// <summary>
/// Source recommendation for a scene
/// </summary>
public record SourceRecommendationDto(
    bool UseStock,
    bool UseGenerative,
    string PreferredSource,
    string Reasoning,
    double Confidence
);

/// <summary>
/// Rate limit status for providers
/// </summary>
public record RateLimitStatusDto(
    string Provider,
    int RequestsRemaining,
    int RequestsLimit,
    DateTime? ResetTime,
    bool IsLimited
);

/// <summary>
/// Provider validation result
/// </summary>
public record ProviderValidationDto(
    string Provider,
    bool IsValid,
    string? ErrorMessage
);
