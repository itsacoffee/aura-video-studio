using System;
using System.Collections.Generic;

namespace Aura.Api.Models.ApiModels.V1;

/// <summary>
/// Request to generate licensing manifest
/// </summary>
public record GenerateLicensingManifestRequest
{
    /// <summary>
    /// Project/Job ID
    /// </summary>
    public string ProjectId { get; init; } = string.Empty;

    /// <summary>
    /// Optional timeline data to generate manifest from
    /// </summary>
    public object? TimelineData { get; init; }
}

/// <summary>
/// Request to export licensing manifest
/// </summary>
public record ExportLicensingManifestRequest
{
    /// <summary>
    /// Project/Job ID
    /// </summary>
    public string ProjectId { get; init; } = string.Empty;

    /// <summary>
    /// Export format (json, csv, html, text)
    /// </summary>
    public string Format { get; init; } = "json";
}

/// <summary>
/// Request to record licensing sign-off
/// </summary>
public record LicensingSignOffRequest
{
    /// <summary>
    /// Project/Job ID
    /// </summary>
    public string ProjectId { get; init; } = string.Empty;

    /// <summary>
    /// User acknowledged commercial restrictions
    /// </summary>
    public bool AcknowledgedCommercialRestrictions { get; init; }

    /// <summary>
    /// User acknowledged attribution requirements
    /// </summary>
    public bool AcknowledgedAttributionRequirements { get; init; }

    /// <summary>
    /// User acknowledged warnings
    /// </summary>
    public bool AcknowledgedWarnings { get; init; }

    /// <summary>
    /// Optional user notes
    /// </summary>
    public string? Notes { get; init; }
}

/// <summary>
/// Asset licensing information DTO
/// </summary>
public record AssetLicensingInfoDto
{
    public string AssetId { get; init; } = string.Empty;
    public string AssetType { get; init; } = string.Empty;
    public int SceneIndex { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string LicenseType { get; init; } = string.Empty;
    public string? LicenseUrl { get; init; }
    public bool CommercialUseAllowed { get; init; }
    public bool AttributionRequired { get; init; }
    public string? AttributionText { get; init; }
    public string? Creator { get; init; }
    public string? CreatorUrl { get; init; }
    public string? SourceUrl { get; init; }
    public string? FilePath { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Licensing summary DTO
/// </summary>
public record LicensingSummaryDto
{
    public int TotalAssets { get; init; }
    public Dictionary<string, int> AssetsByType { get; init; } = new();
    public Dictionary<string, int> AssetsBySource { get; init; } = new();
    public Dictionary<string, int> AssetsByLicenseType { get; init; } = new();
    public int AssetsRequiringAttribution { get; init; }
    public int AssetsWithCommercialRestrictions { get; init; }
}

/// <summary>
/// Project licensing manifest DTO
/// </summary>
public record ProjectLicensingManifestDto
{
    public string ProjectId { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public DateTime GeneratedAt { get; init; }
    public List<AssetLicensingInfoDto> Assets { get; init; } = new();
    public bool AllCommercialUseAllowed { get; init; }
    public List<string> Warnings { get; init; } = new();
    public List<string> MissingLicensingInfo { get; init; } = new();
    public LicensingSummaryDto Summary { get; init; } = new();
}

/// <summary>
/// Response for licensing sign-off
/// </summary>
public record LicensingSignOffResponse
{
    public string ProjectId { get; init; } = string.Empty;
    public DateTime SignedOffAt { get; init; }
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Response for licensing export
/// </summary>
public record LicensingExportResponse
{
    public string Format { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Filename { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
}
