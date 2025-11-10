using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Jobs;

/// <summary>
/// Represents a background video generation job
/// </summary>
public class VideoGenerationJob
{
    public required string JobId { get; init; }
    public required string CorrelationId { get; init; }
    public required Brief Brief { get; init; }
    public required PlanSpec PlanSpec { get; init; }
    public required VoiceSpec VoiceSpec { get; init; }
    public required RenderSpec RenderSpec { get; init; }
    public required SystemProfile SystemProfile { get; init; }
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? OutputPath { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; init; } = 3;
    public Dictionary<string, object> Metadata { get; init; } = new();
    public List<JobProgressUpdate> ProgressUpdates { get; init; } = new();
}

/// <summary>
/// Job execution status
/// </summary>
public enum JobStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled,
    Retrying
}

/// <summary>
/// Progress update for a job
/// </summary>
public class JobProgressUpdate
{
    public required DateTime Timestamp { get; init; }
    public required string Stage { get; init; }
    public required double PercentComplete { get; init; }
    public required string Message { get; init; }
}
