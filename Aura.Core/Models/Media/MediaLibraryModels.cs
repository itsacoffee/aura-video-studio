using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Media;

/// <summary>
/// Types of media that can be stored in the library
/// </summary>
public enum MediaType
{
    Video,
    Image,
    Audio,
    Document,
    Other
}

/// <summary>
/// Source of media file
/// </summary>
public enum MediaSource
{
    UserUpload,
    Generated,
    StockMedia,
    Imported
}

/// <summary>
/// Status of media processing
/// </summary>
public enum ProcessingStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

/// <summary>
/// Media file metadata
/// </summary>
public class MediaMetadata
{
    public int? Width { get; set; }
    public int? Height { get; set; }
    public double? Duration { get; set; } // in seconds
    public double? Framerate { get; set; }
    public string? Format { get; set; }
    public string? Codec { get; set; }
    public long? Bitrate { get; set; }
    public int? Channels { get; set; } // for audio
    public int? SampleRate { get; set; } // for audio
    public string? ColorSpace { get; set; }
    public Dictionary<string, string> AdditionalProperties { get; set; } = new();
}

/// <summary>
/// Request to upload media
/// </summary>
public class MediaUploadRequest
{
    public required string FileName { get; set; }
    public required MediaType Type { get; set; }
    public MediaSource Source { get; set; } = MediaSource.UserUpload;
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = new();
    public Guid? CollectionId { get; set; }
    public bool GenerateThumbnail { get; set; } = true;
    public bool ExtractMetadata { get; set; } = true;
}

/// <summary>
/// Chunked upload session
/// </summary>
public class UploadSession
{
    public Guid SessionId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public long UploadedSize { get; set; }
    public int TotalChunks { get; set; }
    public List<int> CompletedChunks { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
    public string BlobUrl { get; set; } = string.Empty;
}

/// <summary>
/// Response for media item
/// </summary>
public class MediaItemResponse
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public MediaType Type { get; set; }
    public MediaSource Source { get; set; }
    public long FileSize { get; set; }
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? PreviewUrl { get; set; }
    public string Url { get; set; } = string.Empty;
    public MediaMetadata? Metadata { get; set; }
    public ProcessingStatus ProcessingStatus { get; set; }
    public List<string> Tags { get; set; } = new();
    public Guid? CollectionId { get; set; }
    public string? CollectionName { get; set; }
    public int UsageCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Request to search/filter media
/// </summary>
public class MediaSearchRequest
{
    public string? SearchTerm { get; set; }
    public List<MediaType>? Types { get; set; }
    public List<MediaSource>? Sources { get; set; }
    public List<string>? Tags { get; set; }
    public Guid? CollectionId { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Response for media search
/// </summary>
public class MediaSearchResponse
{
    public List<MediaItemResponse> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// Media collection (folder)
/// </summary>
public class MediaCollectionResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MediaCount { get; set; }
    public string? ThumbnailUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Request to create/update collection
/// </summary>
public class MediaCollectionRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Bulk operations request
/// </summary>
public class BulkMediaOperationRequest
{
    public required List<Guid> MediaIds { get; set; }
    public BulkOperation Operation { get; set; }
    public Guid? TargetCollectionId { get; set; }
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Types of bulk operations
/// </summary>
public enum BulkOperation
{
    Delete,
    Move,
    AddTags,
    RemoveTags,
    ChangeCollection
}

/// <summary>
/// Storage statistics
/// </summary>
public class StorageStats
{
    public long TotalSizeBytes { get; set; }
    public long QuotaBytes { get; set; }
    public long AvailableBytes { get; set; }
    public double UsagePercentage { get; set; }
    public int TotalFiles { get; set; }
    public Dictionary<MediaType, int> FilesByType { get; set; } = new();
    public Dictionary<MediaType, long> SizeByType { get; set; } = new();
}

/// <summary>
/// Media usage tracking
/// </summary>
public class MediaUsageInfo
{
    public Guid MediaId { get; set; }
    public int TotalUsages { get; set; }
    public DateTime LastUsedAt { get; set; }
    public List<string> UsedInProjects { get; set; } = new();
}

/// <summary>
/// Duplicate detection result
/// </summary>
public class DuplicateDetectionResult
{
    public bool IsDuplicate { get; set; }
    public Guid? ExistingMediaId { get; set; }
    public string? ContentHash { get; set; }
    public double SimilarityScore { get; set; }
}
