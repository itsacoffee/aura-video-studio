using System;
using System.Collections.Generic;

namespace Aura.Core.Models;

/// <summary>
/// Status information for a pipeline execution
/// </summary>
public record PipelineStatus
{
    /// <summary>
    /// Unique identifier for this pipeline run
    /// </summary>
    public required string PipelineId { get; init; }

    /// <summary>
    /// Correlation ID for tracking across services
    /// </summary>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// Current state of the pipeline
    /// </summary>
    public required PipelineExecutionState State { get; init; }

    /// <summary>
    /// Name of the current stage being executed
    /// </summary>
    public required string CurrentStage { get; init; }

    /// <summary>
    /// Overall progress percentage (0-100)
    /// </summary>
    public required int OverallProgress { get; init; }

    /// <summary>
    /// Progress of the current stage (0-100)
    /// </summary>
    public required int StageProgress { get; init; }

    /// <summary>
    /// Human-readable status message
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// List of completed stages
    /// </summary>
    public required List<string> CompletedStages { get; init; }

    /// <summary>
    /// List of failed stages with error information
    /// </summary>
    public required List<StageFailure> FailedStages { get; init; }

    /// <summary>
    /// When the pipeline started
    /// </summary>
    public required DateTime StartedAt { get; init; }

    /// <summary>
    /// When the pipeline completed (null if still running)
    /// </summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>
    /// Total elapsed time
    /// </summary>
    public TimeSpan ElapsedTime => (CompletedAt ?? DateTime.UtcNow) - StartedAt;

    /// <summary>
    /// Estimated time remaining (null if cannot be estimated)
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; init; }

    /// <summary>
    /// Path to final output video (null if not completed)
    /// </summary>
    public string? OutputPath { get; init; }

    /// <summary>
    /// Additional metadata about the pipeline execution
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Execution state of a pipeline
/// </summary>
public enum PipelineExecutionState
{
    /// <summary>
    /// Pipeline is queued and waiting to start
    /// </summary>
    Queued,

    /// <summary>
    /// Pipeline is initializing
    /// </summary>
    Initializing,

    /// <summary>
    /// Pipeline is actively running
    /// </summary>
    Running,

    /// <summary>
    /// Pipeline completed successfully
    /// </summary>
    Completed,

    /// <summary>
    /// Pipeline failed with errors
    /// </summary>
    Failed,

    /// <summary>
    /// Pipeline was cancelled by user
    /// </summary>
    Cancelled,

    /// <summary>
    /// Pipeline is paused (waiting for manual intervention)
    /// </summary>
    Paused,

    /// <summary>
    /// Pipeline is resuming from a checkpoint
    /// </summary>
    Resuming
}

/// <summary>
/// Information about a failed stage
/// </summary>
public record StageFailure
{
    /// <summary>
    /// Name of the failed stage
    /// </summary>
    public required string StageName { get; init; }

    /// <summary>
    /// Error message
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Error code (if applicable)
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// When the failure occurred
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Number of retry attempts made
    /// </summary>
    public required int RetryAttempts { get; init; }

    /// <summary>
    /// Whether the failure is recoverable
    /// </summary>
    public required bool IsRecoverable { get; init; }

    /// <summary>
    /// Full exception details (for debugging)
    /// </summary>
    public string? ExceptionDetails { get; init; }
}

/// <summary>
/// Detailed progress information for a pipeline execution
/// </summary>
public record PipelineProgressUpdate
{
    /// <summary>
    /// Correlation ID for this pipeline
    /// </summary>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// Name of the current stage
    /// </summary>
    public required string CurrentStage { get; init; }

    /// <summary>
    /// Display name of the current stage
    /// </summary>
    public required string StageDisplayName { get; init; }

    /// <summary>
    /// Overall progress (0-100)
    /// </summary>
    public required int OverallProgress { get; init; }

    /// <summary>
    /// Stage-specific progress (0-100)
    /// </summary>
    public required int StageProgress { get; init; }

    /// <summary>
    /// Progress message
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Current item being processed (optional)
    /// </summary>
    public int? CurrentItem { get; init; }

    /// <summary>
    /// Total number of items to process (optional)
    /// </summary>
    public int? TotalItems { get; init; }

    /// <summary>
    /// Elapsed time
    /// </summary>
    public required TimeSpan ElapsedTime { get; init; }

    /// <summary>
    /// Estimated time remaining
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; init; }

    /// <summary>
    /// Timestamp of this update
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Current attempt number (for retries)
    /// </summary>
    public int AttemptNumber { get; init; } = 1;

    /// <summary>
    /// Error message if stage is retrying
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Summary statistics for a completed pipeline execution
/// </summary>
public record PipelineExecutionSummary
{
    /// <summary>
    /// Pipeline ID
    /// </summary>
    public required string PipelineId { get; init; }

    /// <summary>
    /// Correlation ID
    /// </summary>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// Whether the pipeline succeeded
    /// </summary>
    public required bool Succeeded { get; init; }

    /// <summary>
    /// Total execution time
    /// </summary>
    public required TimeSpan TotalDuration { get; init; }

    /// <summary>
    /// Number of stages executed
    /// </summary>
    public required int StagesExecuted { get; init; }

    /// <summary>
    /// Number of stages that failed
    /// </summary>
    public required int StagesFailed { get; init; }

    /// <summary>
    /// Total retry attempts across all stages
    /// </summary>
    public required int TotalRetries { get; init; }

    /// <summary>
    /// Duration by stage
    /// </summary>
    public required Dictionary<string, TimeSpan> StageDurations { get; init; }

    /// <summary>
    /// Path to final output (if succeeded)
    /// </summary>
    public string? OutputPath { get; init; }

    /// <summary>
    /// Error summary (if failed)
    /// </summary>
    public string? ErrorSummary { get; init; }

    /// <summary>
    /// Performance metrics
    /// </summary>
    public Dictionary<string, object> PerformanceMetrics { get; init; } = new();
}
