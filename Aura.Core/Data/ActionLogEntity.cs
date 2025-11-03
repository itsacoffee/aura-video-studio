using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aura.Core.Data;

/// <summary>
/// Entity representing a server-side action that can be undone
/// Supports persistent undo/redo across sessions and users
/// </summary>
public class ActionLogEntity
{
    /// <summary>
    /// Unique identifier for the action
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User who performed the action (empty string for anonymous)
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Type of action (e.g., "CreateProject", "UpdateTemplate", "DeleteProfile")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of the action for display
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// When the action was performed
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current status of the action
    /// Values: Applied, Undone, Failed, Expired
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Applied";

    /// <summary>
    /// Comma-separated list of resource IDs affected by this action
    /// </summary>
    [MaxLength(1000)]
    public string? AffectedResourceIds { get; set; }

    /// <summary>
    /// JSON serialized payload containing the data needed to perform/undo the action
    /// Stores the complete state snapshot for reversibility
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? PayloadJson { get; set; }

    /// <summary>
    /// Type of inverse action needed to undo this operation
    /// </summary>
    [MaxLength(100)]
    public string? InverseActionType { get; set; }

    /// <summary>
    /// JSON serialized inverse payload for undo operation
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? InversePayloadJson { get; set; }

    /// <summary>
    /// Whether this action can be batched with similar operations
    /// </summary>
    public bool CanBatch { get; set; } = false;

    /// <summary>
    /// Whether this action requires server-side persistence
    /// True for database operations, false for UI-only changes
    /// </summary>
    public bool IsPersistent { get; set; } = true;

    /// <summary>
    /// When the action was undone (null if not undone)
    /// </summary>
    public DateTime? UndoneAt { get; set; }

    /// <summary>
    /// User who performed the undo (may differ from original user)
    /// </summary>
    [MaxLength(200)]
    public string? UndoneByUserId { get; set; }

    /// <summary>
    /// Expiration date for retention policy
    /// Actions older than this can be purged from the log
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Error message if the action or undo operation failed
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Correlation ID for tracking related actions across services
    /// </summary>
    [MaxLength(100)]
    public string? CorrelationId { get; set; }
}
