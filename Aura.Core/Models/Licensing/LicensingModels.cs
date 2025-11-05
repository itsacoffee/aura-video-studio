using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Licensing;

/// <summary>
/// Asset type for licensing purposes
/// </summary>
public enum AssetType
{
    Visual,
    Music,
    SoundEffect,
    Narration,
    Caption,
    Video
}

/// <summary>
/// Complete licensing and provenance information for a single asset
/// </summary>
public record AssetLicensingInfo
{
    /// <summary>
    /// Unique identifier for the asset
    /// </summary>
    public string AssetId { get; init; } = string.Empty;

    /// <summary>
    /// Type of asset
    /// </summary>
    public AssetType AssetType { get; init; }

    /// <summary>
    /// Scene index where asset is used
    /// </summary>
    public int SceneIndex { get; init; }

    /// <summary>
    /// Asset name or title
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Source provider (e.g., "StableDiffusion", "Pexels", "ElevenLabs", "Freesound")
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// License type (e.g., "CC0", "CC BY", "Pexels License", "Commercial")
    /// </summary>
    public string LicenseType { get; init; } = string.Empty;

    /// <summary>
    /// URL to license terms
    /// </summary>
    public string? LicenseUrl { get; init; }

    /// <summary>
    /// Whether commercial use is allowed
    /// </summary>
    public bool CommercialUseAllowed { get; init; }

    /// <summary>
    /// Whether attribution is required
    /// </summary>
    public bool AttributionRequired { get; init; }

    /// <summary>
    /// Attribution text if required
    /// </summary>
    public string? AttributionText { get; init; }

    /// <summary>
    /// Creator name
    /// </summary>
    public string? Creator { get; init; }

    /// <summary>
    /// Creator profile URL
    /// </summary>
    public string? CreatorUrl { get; init; }

    /// <summary>
    /// Asset source URL
    /// </summary>
    public string? SourceUrl { get; init; }

    /// <summary>
    /// File path (local or remote)
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Unified licensing manifest for entire project
/// </summary>
public record ProjectLicensingManifest
{
    /// <summary>
    /// Project/Job ID
    /// </summary>
    public string ProjectId { get; init; } = string.Empty;

    /// <summary>
    /// Project name
    /// </summary>
    public string ProjectName { get; init; } = string.Empty;

    /// <summary>
    /// When manifest was generated
    /// </summary>
    public DateTime GeneratedAt { get; init; }

    /// <summary>
    /// All assets used in the project
    /// </summary>
    public List<AssetLicensingInfo> Assets { get; init; } = new();

    /// <summary>
    /// Overall commercial use allowed
    /// </summary>
    public bool AllCommercialUseAllowed { get; init; }

    /// <summary>
    /// List of warnings or issues
    /// </summary>
    public List<string> Warnings { get; init; } = new();

    /// <summary>
    /// List of assets with missing licensing information
    /// </summary>
    public List<string> MissingLicensingInfo { get; init; } = new();

    /// <summary>
    /// Summary statistics
    /// </summary>
    public LicensingSummary Summary { get; init; } = new();
}

/// <summary>
/// Summary statistics for licensing manifest
/// </summary>
public record LicensingSummary
{
    /// <summary>
    /// Total number of assets
    /// </summary>
    public int TotalAssets { get; init; }

    /// <summary>
    /// Assets by type
    /// </summary>
    public Dictionary<AssetType, int> AssetsByType { get; init; } = new();

    /// <summary>
    /// Assets by source
    /// </summary>
    public Dictionary<string, int> AssetsBySource { get; init; } = new();

    /// <summary>
    /// Assets by license type
    /// </summary>
    public Dictionary<string, int> AssetsByLicenseType { get; init; } = new();

    /// <summary>
    /// Number of assets requiring attribution
    /// </summary>
    public int AssetsRequiringAttribution { get; init; }

    /// <summary>
    /// Number of assets with commercial restrictions
    /// </summary>
    public int AssetsWithCommercialRestrictions { get; init; }
}

/// <summary>
/// Export format for licensing manifest
/// </summary>
public enum LicensingExportFormat
{
    Json,
    Csv,
    Html,
    Text
}

/// <summary>
/// Request to export licensing manifest
/// </summary>
public record LicensingExportRequest
{
    /// <summary>
    /// Project/Job ID
    /// </summary>
    public string ProjectId { get; init; } = string.Empty;

    /// <summary>
    /// Export format
    /// </summary>
    public LicensingExportFormat Format { get; init; }

    /// <summary>
    /// Whether to include assets without licensing info
    /// </summary>
    public bool IncludeMissing { get; init; } = true;
}

/// <summary>
/// Sign-off confirmation for licensing
/// </summary>
public record LicensingSignOff
{
    /// <summary>
    /// Project/Job ID
    /// </summary>
    public string ProjectId { get; init; } = string.Empty;

    /// <summary>
    /// User confirmed aware of commercial restrictions
    /// </summary>
    public bool AcknowledgedCommercialRestrictions { get; init; }

    /// <summary>
    /// User confirmed aware of attribution requirements
    /// </summary>
    public bool AcknowledgedAttributionRequirements { get; init; }

    /// <summary>
    /// User confirmed reviewed all warnings
    /// </summary>
    public bool AcknowledgedWarnings { get; init; }

    /// <summary>
    /// Timestamp of sign-off
    /// </summary>
    public DateTime SignedOffAt { get; init; }

    /// <summary>
    /// Optional user notes
    /// </summary>
    public string? Notes { get; init; }
}
