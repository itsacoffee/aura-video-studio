using System;

namespace Aura.Core.Models.Export;

/// <summary>
/// Represents a video export job with progress tracking.
/// Used by the frontend to track rendering progress via polling.
/// </summary>
public record VideoJob
{
    /// <summary>Unique identifier for the job</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>Current status of the job (queued, running, completed, failed)</summary>
    public string Status { get; init; } = "queued";

    /// <summary>Progress percentage (0-100)</summary>
    public int Progress { get; init; }

    /// <summary>Current stage description for UI display</summary>
    public string Stage { get; init; } = "Initializing";

    /// <summary>Optional detailed progress message</summary>
    public string? Message { get; init; }

    /// <summary>UTC timestamp when job was created</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>UTC timestamp when job started running</summary>
    public DateTime? StartedAt { get; init; }

    /// <summary>UTC timestamp when job completed or failed</summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>Error message if job failed</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Output file path when job is completed</summary>
    public string? OutputPath { get; init; }
}
