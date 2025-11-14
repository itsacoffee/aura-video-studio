using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Aura.Core.Models.Projects;

/// <summary>
/// .aura project file format
/// </summary>
public class AuraProjectFile
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";
    
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("modifiedAt")]
    public DateTime ModifiedAt { get; set; }
    
    [JsonPropertyName("lastSavedAt")]
    public DateTime LastSavedAt { get; set; }
    
    [JsonPropertyName("autoSaveEnabled")]
    public bool AutoSaveEnabled { get; set; } = true;
    
    [JsonPropertyName("metadata")]
    public ProjectMetadata Metadata { get; set; } = new();
    
    [JsonPropertyName("assets")]
    public List<ProjectAsset> Assets { get; set; } = new();
    
    [JsonPropertyName("timeline")]
    public ProjectTimeline? Timeline { get; set; }
    
    [JsonPropertyName("brief")]
    public string? BriefJson { get; set; }
    
    [JsonPropertyName("planSpec")]
    public string? PlanSpecJson { get; set; }
    
    [JsonPropertyName("voiceSpec")]
    public string? VoiceSpecJson { get; set; }
    
    [JsonPropertyName("renderSpec")]
    public string? RenderSpecJson { get; set; }
    
    [JsonPropertyName("settings")]
    public ProjectSettings Settings { get; set; } = new();
}

/// <summary>
/// Project metadata
/// </summary>
public class ProjectMetadata
{
    [JsonPropertyName("author")]
    public string? Author { get; set; }
    
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
    
    [JsonPropertyName("duration")]
    public double? DurationSeconds { get; set; }
    
    [JsonPropertyName("resolution")]
    public ResolutionInfo? Resolution { get; set; }
    
    [JsonPropertyName("framerate")]
    public double? Framerate { get; set; }
    
    [JsonPropertyName("totalAssets")]
    public int TotalAssets { get; set; }
    
    [JsonPropertyName("projectSize")]
    public long ProjectSizeBytes { get; set; }
    
    [JsonPropertyName("customData")]
    public Dictionary<string, string> CustomData { get; set; } = new();
}

/// <summary>
/// Resolution information
/// </summary>
public class ResolutionInfo
{
    [JsonPropertyName("width")]
    public int Width { get; set; }
    
    [JsonPropertyName("height")]
    public int Height { get; set; }
    
    [JsonPropertyName("aspectRatio")]
    public string AspectRatio { get; set; } = string.Empty;
}

/// <summary>
/// Project asset reference
/// </summary>
public class ProjectAsset
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // Video, Audio, Image, Subtitle
    
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;
    
    [JsonPropertyName("relativePath")]
    public string? RelativePath { get; set; }
    
    [JsonPropertyName("isEmbedded")]
    public bool IsEmbedded { get; set; } = false;
    
    [JsonPropertyName("isMissing")]
    public bool IsMissing { get; set; } = false;
    
    [JsonPropertyName("fileSize")]
    public long FileSizeBytes { get; set; }
    
    [JsonPropertyName("contentHash")]
    public string? ContentHash { get; set; }
    
    [JsonPropertyName("importedAt")]
    public DateTime ImportedAt { get; set; }
    
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Project timeline
/// </summary>
public class ProjectTimeline
{
    [JsonPropertyName("tracks")]
    public List<TimelineTrack> Tracks { get; set; } = new();
    
    [JsonPropertyName("duration")]
    public double DurationSeconds { get; set; }
    
    [JsonPropertyName("framerate")]
    public double Framerate { get; set; } = 30;
}

/// <summary>
/// Timeline track
/// </summary>
public class TimelineTrack
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // Video, Audio, Subtitle
    
    [JsonPropertyName("clips")]
    public List<TimelineClip> Clips { get; set; } = new();
}

/// <summary>
/// Timeline clip
/// </summary>
public class TimelineClip
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("assetId")]
    public Guid AssetId { get; set; }
    
    [JsonPropertyName("startTime")]
    public double StartTime { get; set; }
    
    [JsonPropertyName("duration")]
    public double Duration { get; set; }
    
    [JsonPropertyName("trimStart")]
    public double TrimStart { get; set; }
    
    [JsonPropertyName("trimEnd")]
    public double TrimEnd { get; set; }
}

/// <summary>
/// Project settings
/// </summary>
public class ProjectSettings
{
    [JsonPropertyName("autoSaveInterval")]
    public int AutoSaveIntervalSeconds { get; set; } = 300; // 5 minutes
    
    [JsonPropertyName("maxBackups")]
    public int MaxBackups { get; set; } = 10;
    
    [JsonPropertyName("enableAutoBackup")]
    public bool EnableAutoBackup { get; set; } = true;
    
    [JsonPropertyName("embedAssets")]
    public bool EmbedAssets { get; set; } = false;
    
    [JsonPropertyName("customSettings")]
    public Dictionary<string, string> CustomSettings { get; set; } = new();
}

/// <summary>
/// Asset relinking request
/// </summary>
public class AssetRelinkRequest
{
    public Guid ProjectId { get; set; }
    public Guid AssetId { get; set; }
    public string NewPath { get; set; } = string.Empty;
}

/// <summary>
/// Asset relinking result
/// </summary>
public class AssetRelinkResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? OldPath { get; set; }
    public string? NewPath { get; set; }
}

/// <summary>
/// Missing assets report
/// </summary>
public class MissingAssetsReport
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public List<ProjectAsset> MissingAssets { get; set; } = new();
    public int TotalAssets { get; set; }
    public int MissingCount { get; set; }
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Project consolidation request
/// </summary>
public class ProjectConsolidationRequest
{
    public Guid ProjectId { get; set; }
    public bool CopyExternalAssets { get; set; } = true;
    public bool CreateBackup { get; set; } = true;
    public bool EmbedInProjectFile { get; set; }
}

/// <summary>
/// Project consolidation result
/// </summary>
public class ProjectConsolidationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int AssetsCopied { get; set; }
    public long TotalBytesCopied { get; set; }
    public string? ConsolidatedPath { get; set; }
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Project export package request
/// </summary>
public class ProjectPackageRequest
{
    public Guid ProjectId { get; set; }
    public bool IncludeAssets { get; set; } = true;
    public bool IncludeBackups { get; set; }
    public bool CompressAssets { get; set; } = true;
    public string? OutputPath { get; set; }
}

/// <summary>
/// Project export package result
/// </summary>
public class ProjectPackageResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PackagePath { get; set; }
    public long PackageSizeBytes { get; set; }
    public string FormattedSize { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
}
