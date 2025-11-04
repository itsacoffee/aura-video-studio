using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aura.Core.Data;

/// <summary>
/// Entity representing a saved version/snapshot of a project at a specific point in time
/// </summary>
public class ProjectVersionEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public ProjectStateEntity Project { get; set; } = null!;

    /// <summary>
    /// Version number (1-based, auto-incremented per project)
    /// </summary>
    [Required]
    public int VersionNumber { get; set; }

    /// <summary>
    /// User-provided name for this version (optional)
    /// </summary>
    [MaxLength(200)]
    public string? Name { get; set; }

    /// <summary>
    /// Description of this version (optional)
    /// </summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Type of version: Manual, Autosave, RestorePoint
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string VersionType { get; set; } = "Manual";

    /// <summary>
    /// Trigger that caused this version to be created (if RestorePoint)
    /// e.g., "PreScriptRegeneration", "PostLLMRefinement", "PreBulkTimelineChange"
    /// </summary>
    [MaxLength(200)]
    public string? Trigger { get; set; }

    /// <summary>
    /// When this version was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User ID who created this version (if manual)
    /// </summary>
    [MaxLength(200)]
    public string? CreatedByUserId { get; set; }

    /// <summary>
    /// JSON serialized Brief at this version
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? BriefJson { get; set; }

    /// <summary>
    /// JSON serialized PlanSpec at this version
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? PlanSpecJson { get; set; }

    /// <summary>
    /// JSON serialized VoiceSpec at this version
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? VoiceSpecJson { get; set; }

    /// <summary>
    /// JSON serialized RenderSpec at this version
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? RenderSpecJson { get; set; }

    /// <summary>
    /// JSON serialized Timeline at this version (if available)
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? TimelineJson { get; set; }

    /// <summary>
    /// Content hash for brief data (for deduplication)
    /// </summary>
    [MaxLength(64)]
    public string? BriefHash { get; set; }

    /// <summary>
    /// Content hash for plan data (for deduplication)
    /// </summary>
    [MaxLength(64)]
    public string? PlanHash { get; set; }

    /// <summary>
    /// Content hash for voice data (for deduplication)
    /// </summary>
    [MaxLength(64)]
    public string? VoiceHash { get; set; }

    /// <summary>
    /// Content hash for render data (for deduplication)
    /// </summary>
    [MaxLength(64)]
    public string? RenderHash { get; set; }

    /// <summary>
    /// Content hash for timeline data (for deduplication)
    /// </summary>
    [MaxLength(64)]
    public string? TimelineHash { get; set; }

    /// <summary>
    /// Total storage size in bytes (for display/cleanup)
    /// </summary>
    public long StorageSizeBytes { get; set; } = 0;

    /// <summary>
    /// Whether this version is marked as important (protects from auto-pruning)
    /// </summary>
    public bool IsMarkedImportant { get; set; } = false;

    /// <summary>
    /// Whether this version has been deleted (soft delete)
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// When this version was deleted (null if not deleted)
    /// </summary>
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// Entity representing deduplicated content blobs for versions
/// Content-addressable storage: same content = same hash = single storage
/// </summary>
public class ContentBlobEntity
{
    [Key]
    [MaxLength(64)]
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// The actual content (JSON or binary data)
    /// </summary>
    [Required]
    [Column(TypeName = "TEXT")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Content type (Brief, Plan, Voice, Render, Timeline, Asset)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Size in bytes
    /// </summary>
    [Required]
    public long SizeBytes { get; set; }

    /// <summary>
    /// When this blob was first created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time this blob was referenced by a version
    /// </summary>
    [Required]
    public DateTime LastReferencedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Number of versions referencing this blob
    /// </summary>
    [Required]
    public int ReferenceCount { get; set; } = 0;
}
