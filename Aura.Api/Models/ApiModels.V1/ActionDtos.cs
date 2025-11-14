using System;
using System.Text.Json.Serialization;

namespace Aura.Api.Models.ApiModels.V1;

/// <summary>
/// Request to record a new action in the action log
/// </summary>
public record RecordActionRequest
{
    /// <summary>
    /// User performing the action (empty string for anonymous)
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Type of action (e.g., "CreateProject", "UpdateTemplate")
    /// </summary>
    public required string ActionType { get; init; }

    /// <summary>
    /// Human-readable description for display
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Comma-separated resource IDs affected by this action
    /// </summary>
    public string? AffectedResourceIds { get; init; }

    /// <summary>
    /// JSON payload for the action
    /// </summary>
    public string? PayloadJson { get; init; }

    /// <summary>
    /// Type of inverse action for undo
    /// </summary>
    public string? InverseActionType { get; init; }

    /// <summary>
    /// JSON payload for inverse action
    /// </summary>
    public string? InversePayloadJson { get; init; }

    /// <summary>
    /// Whether this action can be batched
    /// </summary>
    public bool CanBatch { get; init; }

    /// <summary>
    /// Whether this requires server-side persistence
    /// </summary>
    public bool IsPersistent { get; init; } = true;

    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Retention period in days (null for default retention)
    /// </summary>
    public int? RetentionDays { get; init; }
}

/// <summary>
/// Response after recording an action
/// </summary>
public record RecordActionResponse
{
    /// <summary>
    /// Unique ID of the recorded action
    /// </summary>
    public required Guid ActionId { get; init; }

    /// <summary>
    /// When the action was recorded
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Current status of the action
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// When the action expires (for retention policy)
    /// </summary>
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>
/// Response for an undo operation
/// </summary>
public record UndoActionResponse
{
    /// <summary>
    /// ID of the action that was undone
    /// </summary>
    public required Guid ActionId { get; init; }

    /// <summary>
    /// Whether the undo was successful
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// When the undo was performed
    /// </summary>
    public required DateTime UndoneAt { get; init; }

    /// <summary>
    /// Error message if undo failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// New status after undo
    /// </summary>
    public required string Status { get; init; }
}

/// <summary>
/// Query parameters for action history
/// </summary>
public record ActionHistoryQuery
{
    /// <summary>
    /// Filter by user ID
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Filter by action type
    /// </summary>
    public string? ActionType { get; init; }

    /// <summary>
    /// Filter by status
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Start date for time range filter
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// End date for time range filter
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Page size (max 100)
    /// </summary>
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Single action in history
/// </summary>
public record ActionHistoryItem
{
    /// <summary>
    /// Action ID
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// User who performed the action
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// Action type
    /// </summary>
    public required string ActionType { get; init; }

    /// <summary>
    /// Description
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// When the action was performed
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Current status
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Can this action be undone
    /// </summary>
    public required bool CanUndo { get; init; }

    /// <summary>
    /// Affected resource IDs
    /// </summary>
    public string? AffectedResourceIds { get; init; }

    /// <summary>
    /// When undone (if applicable)
    /// </summary>
    public DateTime? UndoneAt { get; init; }

    /// <summary>
    /// User who undid the action
    /// </summary>
    public string? UndoneByUserId { get; init; }
}

/// <summary>
/// Paginated action history response
/// </summary>
public record ActionHistoryResponse
{
    /// <summary>
    /// List of actions
    /// </summary>
    public required List<ActionHistoryItem> Actions { get; init; }

    /// <summary>
    /// Current page number
    /// </summary>
    public required int Page { get; init; }

    /// <summary>
    /// Page size
    /// </summary>
    public required int PageSize { get; init; }

    /// <summary>
    /// Total number of actions matching the query
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public required int TotalPages { get; init; }
}

/// <summary>
/// Detailed action information
/// </summary>
public record ActionDetailResponse
{
    /// <summary>
    /// Action ID
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// User who performed the action
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// Action type
    /// </summary>
    public required string ActionType { get; init; }

    /// <summary>
    /// Description
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// When the action was performed
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Current status
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Affected resource IDs
    /// </summary>
    public string? AffectedResourceIds { get; init; }

    /// <summary>
    /// Action payload
    /// </summary>
    public string? PayloadJson { get; init; }

    /// <summary>
    /// Inverse action type
    /// </summary>
    public string? InverseActionType { get; init; }

    /// <summary>
    /// Inverse payload
    /// </summary>
    public string? InversePayloadJson { get; init; }

    /// <summary>
    /// Can batch
    /// </summary>
    public required bool CanBatch { get; init; }

    /// <summary>
    /// Is persistent
    /// </summary>
    public required bool IsPersistent { get; init; }

    /// <summary>
    /// When undone
    /// </summary>
    public DateTime? UndoneAt { get; init; }

    /// <summary>
    /// User who undid the action
    /// </summary>
    public string? UndoneByUserId { get; init; }

    /// <summary>
    /// Expiration date
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    /// Error message
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Correlation ID
    /// </summary>
    public string? CorrelationId { get; init; }
}
