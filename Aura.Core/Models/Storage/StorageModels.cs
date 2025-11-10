using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Storage;

/// <summary>
/// Configuration for local storage service
/// </summary>
public class LocalStorageConfiguration
{
    /// <summary>
    /// Root directory for all Aura storage (default: ~/AuraVideoStudio)
    /// </summary>
    public string StorageRoot { get; set; } = string.Empty;
    
    /// <summary>
    /// Maximum storage quota in bytes (default: 50GB)
    /// </summary>
    public long StorageQuotaBytes { get; set; } = 50L * 1024 * 1024 * 1024;
    
    /// <summary>
    /// Low disk space warning threshold in bytes (default: 5GB)
    /// </summary>
    public long LowSpaceThresholdBytes { get; set; } = 5L * 1024 * 1024 * 1024;
    
    /// <summary>
    /// Maximum cache size in bytes (default: 10GB)
    /// </summary>
    public long MaxCacheSizeBytes { get; set; } = 10L * 1024 * 1024 * 1024;
    
    /// <summary>
    /// Enable automatic cleanup of old cache entries
    /// </summary>
    public bool EnableAutoCacheCleanup { get; set; } = true;
    
    /// <summary>
    /// Cache entry TTL in days (default: 30)
    /// </summary>
    public int CacheTtlDays { get; set; } = 30;
}

/// <summary>
/// Workspace folder structure
/// </summary>
public static class WorkspaceFolders
{
    public const string Projects = "Projects";
    public const string Exports = "Exports";
    public const string Cache = "Cache";
    public const string Temp = "Temp";
    public const string Media = "Media";
    public const string Thumbnails = "Thumbnails";
    public const string Backups = "Backups";
    public const string Previews = "Previews";
}

/// <summary>
/// Storage statistics
/// </summary>
public class StorageStatistics
{
    public long TotalSizeBytes { get; set; }
    public long UsedSizeBytes { get; set; }
    public long AvailableSizeBytes { get; set; }
    public long QuotaBytes { get; set; }
    public double UsagePercentage { get; set; }
    public bool IsLowSpace { get; set; }
    public Dictionary<string, long> FolderSizes { get; set; } = new();
    public int TotalFiles { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Disk space information
/// </summary>
public class DiskSpaceInfo
{
    public string DriveName { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public long AvailableSpace { get; set; }
    public long UsedSpace { get; set; }
    public double UsagePercentage { get; set; }
    public bool IsLowSpace { get; set; }
    public string FormattedTotalSize { get; set; } = string.Empty;
    public string FormattedAvailableSpace { get; set; } = string.Empty;
    public string FormattedUsedSpace { get; set; } = string.Empty;
}

/// <summary>
/// Cache entry metadata
/// </summary>
public class CacheEntry
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Thumbnails, Previews, Renders
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public int AccessCount { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Cache statistics
/// </summary>
public class CacheStatistics
{
    public long TotalSizeBytes { get; set; }
    public long MaxSizeBytes { get; set; }
    public int TotalEntries { get; set; }
    public int ExpiredEntries { get; set; }
    public double UsagePercentage { get; set; }
    public Dictionary<string, int> EntriesByCategory { get; set; } = new();
    public Dictionary<string, long> SizeByCategory { get; set; } = new();
    public DateTime? OldestEntry { get; set; }
    public DateTime? NewestEntry { get; set; }
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public double HitRate { get; set; }
}

/// <summary>
/// Cache cleanup result
/// </summary>
public class CacheCleanupResult
{
    public int EntriesRemoved { get; set; }
    public long BytesFreed { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string> RemovedCategories { get; set; } = new();
    public string FormattedBytesFreed { get; set; } = string.Empty;
}

/// <summary>
/// Storage operation result
/// </summary>
public class StorageOperationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? FilePath { get; set; }
    public long? FileSize { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
