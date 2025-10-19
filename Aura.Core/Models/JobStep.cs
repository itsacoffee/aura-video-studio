using System;

namespace Aura.Core.Models;

/// <summary>
/// Represents a single step in a job execution pipeline
/// </summary>
public record JobStep
{
    /// <summary>
    /// Name of the step (e.g., "preflight", "narration", "broll", "subtitles", "mux")
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Status of the step
    /// </summary>
    public StepStatus Status { get; init; } = StepStatus.Pending;
    
    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int ProgressPct { get; init; } = 0;
    
    /// <summary>
    /// Duration in milliseconds
    /// </summary>
    public long DurationMs { get; init; } = 0;
    
    /// <summary>
    /// Errors encountered during this step
    /// </summary>
    public JobStepError[] Errors { get; init; } = Array.Empty<JobStepError>();
    
    /// <summary>
    /// When the step started
    /// </summary>
    public DateTime? StartedAt { get; init; }
    
    /// <summary>
    /// When the step completed
    /// </summary>
    public DateTime? CompletedAt { get; init; }
}

/// <summary>
/// Status of a job step
/// </summary>
public enum StepStatus
{
    Pending,
    Running,
    Succeeded,
    Failed,
    Skipped,
    Canceled
}

/// <summary>
/// Error information for a job step
/// </summary>
public record JobStepError
{
    /// <summary>
    /// Error code (e.g., "MissingApiKey:STABLE_KEY", "FFmpegNotFound")
    /// </summary>
    public string Code { get; init; } = string.Empty;
    
    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string Message { get; init; } = string.Empty;
    
    /// <summary>
    /// Suggested remediation action
    /// </summary>
    public string Remediation { get; init; } = string.Empty;
    
    /// <summary>
    /// Additional error details
    /// </summary>
    public object? Details { get; init; }
}

/// <summary>
/// Job output information
/// </summary>
public record JobOutput
{
    /// <summary>
    /// Path to the output video file
    /// </summary>
    public string VideoPath { get; init; } = string.Empty;
    
    /// <summary>
    /// Size of the output file in bytes
    /// </summary>
    public long SizeBytes { get; init; } = 0;
}
