using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Audio;

/// <summary>
/// License type for music and sound effects
/// </summary>
public enum LicenseType
{
    PublicDomain,
    CreativeCommonsZero,
    CreativeCommonsBY,
    CreativeCommonsBYSA,
    CreativeCommonsBYNC,
    CreativeCommonsBYNCND,
    CreativeCommonsBYNCSA,
    RoyaltyFree,
    Commercial,
    Custom
}

/// <summary>
/// Music or SFX asset with licensing information
/// </summary>
public record AudioAsset(
    string AssetId,
    string Title,
    string? Artist,
    string? Album,
    string FilePath,
    string? PreviewUrl,
    TimeSpan Duration,
    LicenseType LicenseType,
    string LicenseUrl,
    bool CommercialUseAllowed,
    bool AttributionRequired,
    string? AttributionText,
    string SourcePlatform,
    string? CreatorProfileUrl,
    Dictionary<string, object>? Metadata
);

/// <summary>
/// Music asset with additional music-specific metadata
/// </summary>
public record MusicAsset(
    string AssetId,
    string Title,
    string? Artist,
    string? Album,
    string FilePath,
    string? PreviewUrl,
    TimeSpan Duration,
    LicenseType LicenseType,
    string LicenseUrl,
    bool CommercialUseAllowed,
    bool AttributionRequired,
    string? AttributionText,
    string SourcePlatform,
    string? CreatorProfileUrl,
    MusicGenre Genre,
    MusicMood Mood,
    EnergyLevel Energy,
    int BPM,
    List<string> Tags,
    Dictionary<string, object>? Metadata
) : AudioAsset(AssetId, Title, Artist, Album, FilePath, PreviewUrl, Duration, LicenseType, 
    LicenseUrl, CommercialUseAllowed, AttributionRequired, AttributionText, 
    SourcePlatform, CreatorProfileUrl, Metadata);

/// <summary>
/// Sound effect asset with SFX-specific metadata
/// </summary>
public record SfxAsset(
    string AssetId,
    string Title,
    string? Artist,
    string FilePath,
    string? PreviewUrl,
    TimeSpan Duration,
    LicenseType LicenseType,
    string LicenseUrl,
    bool CommercialUseAllowed,
    bool AttributionRequired,
    string? AttributionText,
    string SourcePlatform,
    string? CreatorProfileUrl,
    SoundEffectType Type,
    List<string> Tags,
    string Description,
    Dictionary<string, object>? Metadata
) : AudioAsset(AssetId, Title, Artist, null, FilePath, PreviewUrl, Duration, LicenseType,
    LicenseUrl, CommercialUseAllowed, AttributionRequired, AttributionText,
    SourcePlatform, CreatorProfileUrl, Metadata);

/// <summary>
/// Licensing summary for a video project
/// </summary>
public record LicensingSummary(
    List<UsedAsset> UsedAssets,
    bool AllCommercialUseAllowed,
    List<string> RequiredAttributions,
    List<string> LicenseUrls,
    DateTime GeneratedAt,
    string GeneratedFor
);

/// <summary>
/// An asset used in a video with scene information
/// </summary>
public record UsedAsset(
    AudioAsset Asset,
    int SceneIndex,
    TimeSpan StartTime,
    TimeSpan Duration,
    bool IsSelected
);

/// <summary>
/// Export format for licensing information
/// </summary>
public enum LicenseExportFormat
{
    CSV,
    JSON,
    Text,
    HTML
}

/// <summary>
/// Request for licensing export
/// </summary>
public record LicenseExportRequest(
    string JobId,
    LicenseExportFormat Format,
    bool IncludeUnused = false
);

/// <summary>
/// Music search/filter criteria
/// </summary>
public record MusicSearchCriteria(
    MusicMood? Mood = null,
    MusicGenre? Genre = null,
    EnergyLevel? Energy = null,
    int? MinBPM = null,
    int? MaxBPM = null,
    TimeSpan? MinDuration = null,
    TimeSpan? MaxDuration = null,
    List<string>? Tags = null,
    bool? CommercialUseOnly = false,
    bool? NoAttributionRequired = false,
    string? SearchQuery = null,
    int Page = 1,
    int PageSize = 20
);

/// <summary>
/// SFX search/filter criteria
/// </summary>
public record SfxSearchCriteria(
    SoundEffectType? Type = null,
    List<string>? Tags = null,
    TimeSpan? MaxDuration = null,
    bool? CommercialUseOnly = false,
    bool? NoAttributionRequired = false,
    string? SearchQuery = null,
    int Page = 1,
    int PageSize = 20
);

/// <summary>
/// Search result with pagination
/// </summary>
public record SearchResult<T>(
    List<T> Results,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
