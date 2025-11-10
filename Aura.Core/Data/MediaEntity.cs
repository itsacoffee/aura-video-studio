using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Aura.Core.Models.Media;

namespace Aura.Core.Data;

/// <summary>
/// Media library item entity
/// </summary>
public class MediaEntity : IAuditableEntity, ISoftDeletable
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty; // MediaType enum as string

    [Required]
    [MaxLength(50)]
    public string Source { get; set; } = string.Empty; // MediaSource enum as string

    [Required]
    public long FileSize { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(1000)]
    public string BlobUrl { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? ThumbnailUrl { get; set; }

    [MaxLength(1000)]
    public string? PreviewUrl { get; set; }

    [MaxLength(100)]
    public string? ContentHash { get; set; }

    public string? MetadataJson { get; set; } // Serialized MediaMetadata

    [Required]
    [MaxLength(50)]
    public string ProcessingStatus { get; set; } = "Pending"; // ProcessingStatus enum as string

    public string? ProcessingError { get; set; }

    public Guid? CollectionId { get; set; }

    public int UsageCount { get; set; }

    public DateTime? LastUsedAt { get; set; }

    // Navigation properties
    public MediaCollectionEntity? Collection { get; set; }
    public List<MediaTagEntity> Tags { get; set; } = new();
    public List<MediaUsageEntity> Usages { get; set; } = new();

    // IAuditableEntity
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

/// <summary>
/// Media collection (folder) entity
/// </summary>
public class MediaCollectionEntity : IAuditableEntity, ISoftDeletable
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(1000)]
    public string? ThumbnailUrl { get; set; }

    // Navigation properties
    public List<MediaEntity> MediaItems { get; set; } = new();

    // IAuditableEntity
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

/// <summary>
/// Media tag entity
/// </summary>
public class MediaTagEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid MediaId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Tag { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public MediaEntity? Media { get; set; }
}

/// <summary>
/// Media usage tracking entity
/// </summary>
public class MediaUsageEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid MediaId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ProjectId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? ProjectName { get; set; }

    public DateTime UsedAt { get; set; }

    // Navigation properties
    public MediaEntity? Media { get; set; }
}

/// <summary>
/// Upload session entity for chunked uploads
/// </summary>
public class UploadSessionEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public long TotalSize { get; set; }

    public long UploadedSize { get; set; }

    public int TotalChunks { get; set; }

    public string CompletedChunksJson { get; set; } = "[]"; // Serialized List<int>

    [Required]
    [MaxLength(1000)]
    public string BlobUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }
}
