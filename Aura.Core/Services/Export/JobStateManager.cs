using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Export;

/// <summary>
/// Manages video export job state transitions with a proper state machine.
/// Ensures atomic state changes and validates state transitions.
/// This prevents race conditions where jobs complete but outputPath isn't set.
/// </summary>
public interface IJobStateManager
{
    /// <summary>
    /// Transition a job to a new state with validation.
    /// Returns true if transition was successful, false if invalid transition.
    /// </summary>
    bool TransitionState(string jobId, JobState fromState, JobState toState, string? outputPath = null);

    /// <summary>
    /// Get the current state of a job.
    /// </summary>
    JobState GetState(string jobId);

    /// <summary>
    /// Remove job state tracking (for cleanup).
    /// </summary>
    void RemoveJob(string jobId);
}

/// <summary>
/// Valid states for a video export job lifecycle.
/// </summary>
public enum JobState
{
    /// <summary>Job created but not yet started</summary>
    Queued,
    /// <summary>Job is actively executing (script, TTS, visuals)</summary>
    Running,
    /// <summary>Job is in final video rendering phase</summary>
    Rendering,
    /// <summary>Job is finalizing (creating artifacts, cleanup)</summary>
    Finalizing,
    /// <summary>Job completed successfully with output file</summary>
    Completed,
    /// <summary>Job failed with error</summary>
    Failed,
    /// <summary>Job was cancelled by user</summary>
    Cancelled
}

/// <summary>
/// Default implementation of job state manager with thread-safe operations.
/// </summary>
public class JobStateManager : IJobStateManager
{
    private readonly ILogger<JobStateManager> _logger;
    private readonly ConcurrentDictionary<string, JobStateInfo> _jobStates = new();

    // Valid state transitions - prevents invalid state changes
    private static readonly Dictionary<JobState, HashSet<JobState>> ValidTransitions = new()
    {
        [JobState.Queued] = new() { JobState.Running, JobState.Failed, JobState.Cancelled },
        [JobState.Running] = new() { JobState.Rendering, JobState.Failed, JobState.Cancelled },
        [JobState.Rendering] = new() { JobState.Finalizing, JobState.Failed, JobState.Cancelled },
        [JobState.Finalizing] = new() { JobState.Completed, JobState.Failed, JobState.Cancelled },
        [JobState.Completed] = new(), // Terminal state - no transitions
        [JobState.Failed] = new(), // Terminal state - no transitions
        [JobState.Cancelled] = new() // Terminal state - no transitions
    };

    public JobStateManager(ILogger<JobStateManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool TransitionState(string jobId, JobState fromState, JobState toState, string? outputPath = null)
    {
        // For Completed state, outputPath is REQUIRED
        if (toState == JobState.Completed && string.IsNullOrWhiteSpace(outputPath))
        {
            _logger.LogError(
                "Attempted to transition job {JobId} to Completed state without outputPath. " +
                "This is a critical error - job completion requires a valid output file path.",
                jobId);
            return false;
        }

        // Get or create state info
        var stateInfo = _jobStates.GetOrAdd(jobId, _ => new JobStateInfo { CurrentState = JobState.Queued });

        lock (stateInfo.Lock)
        {
            // Verify we're in the expected fromState
            if (stateInfo.CurrentState != fromState)
            {
                _logger.LogWarning(
                    "Job {JobId} state mismatch: expected {ExpectedState}, actual {ActualState}. Transition to {ToState} aborted.",
                    jobId, fromState, stateInfo.CurrentState, toState);
                return false;
            }

            // Validate transition is allowed
            if (!ValidTransitions.TryGetValue(fromState, out var allowedStates) || !allowedStates.Contains(toState))
            {
                _logger.LogError(
                    "Invalid state transition for job {JobId}: {FromState} -> {ToState} is not allowed",
                    jobId, fromState, toState);
                return false;
            }

            // Perform atomic state update
            stateInfo.CurrentState = toState;
            stateInfo.LastTransitionTime = DateTime.UtcNow;

            // Set outputPath atomically with Completed state
            if (toState == JobState.Completed)
            {
                stateInfo.OutputPath = outputPath;
                _logger.LogInformation(
                    "Job {JobId} transitioned to Completed with outputPath: {OutputPath}",
                    jobId, outputPath);
            }
            else
            {
                _logger.LogInformation(
                    "Job {JobId} transitioned: {FromState} -> {ToState}",
                    jobId, fromState, toState);
            }

            return true;
        }
    }

    public JobState GetState(string jobId)
    {
        return _jobStates.TryGetValue(jobId, out var stateInfo)
            ? stateInfo.CurrentState
            : JobState.Queued; // Default to Queued if not found
    }

    public void RemoveJob(string jobId)
    {
        if (_jobStates.TryRemove(jobId, out var stateInfo))
        {
            _logger.LogDebug("Removed state tracking for job {JobId} (final state: {FinalState})",
                jobId, stateInfo.CurrentState);
        }
    }

    /// <summary>
    /// Internal class to track job state with thread-safe locking.
    /// </summary>
    private class JobStateInfo
    {
        public JobState CurrentState { get; set; } = JobState.Queued;
        public DateTime LastTransitionTime { get; set; } = DateTime.UtcNow;
        public string? OutputPath { get; set; }
        public object Lock { get; } = new object();
    }
}
