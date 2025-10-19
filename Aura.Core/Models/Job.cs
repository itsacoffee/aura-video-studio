using System;
using System.Collections.Generic;

namespace Aura.Core.Models;

/// <summary>
/// Represents a video generation job with stage-by-stage progress tracking.
/// </summary>
public record Job
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Stage { get; init; } = "Plan";
    public JobStatus Status { get; init; } = JobStatus.Queued;
    public int Percent { get; init; } = 0;
    public TimeSpan? Eta { get; init; }
    public List<JobArtifact> Artifacts { get; init; } = new();
    public List<string> Logs { get; init; } = new();
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; init; }
    public string? CorrelationId { get; init; }
    public string? ErrorMessage { get; init; }
    public JobFailure? FailureDetails { get; init; }
    public Brief? Brief { get; init; }
    public PlanSpec? PlanSpec { get; init; }
    public VoiceSpec? VoiceSpec { get; init; }
    public RenderSpec? RenderSpec { get; init; }
    
    // Enhanced fields for new jobs API
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
    public DateTime? StartedUtc { get; init; }
    public DateTime? EndedUtc { get; init; }
    public List<JobStep> Steps { get; init; } = new();
    public JobOutput? Output { get; init; }
    public List<string> Warnings { get; init; } = new();
    public List<JobStepError> Errors { get; init; } = new();
}

/// <summary>
/// Status of a job.
/// </summary>
public enum JobStatus
{
    Queued,
    Running,
    Done,
    Failed,
    Skipped,
    Canceled,
    Succeeded  // Alias for Done to match SSE contract
}

/// <summary>
/// Represents an artifact produced during job execution.
/// </summary>
public record JobArtifact(
    string Name,
    string Path,
    string Type,
    long SizeBytes,
    DateTime CreatedAt);
