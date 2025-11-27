using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Aura.Core.Models.Export;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Export;

/// <summary>
/// Service interface for managing export job status and progress tracking.
/// Provides thread-safe job operations for the video export pipeline.
/// </summary>
public interface IExportJobService
{
    /// <summary>
    /// Create a new job entry with initial status.
    /// </summary>
    Task<VideoJob> CreateJobAsync(VideoJob job);

    /// <summary>
    /// Update job progress during rendering.
    /// </summary>
    Task UpdateJobProgressAsync(string jobId, int percent, string stage);

    /// <summary>
    /// Update job status (e.g., running, completed, failed).
    /// </summary>
    Task UpdateJobStatusAsync(string jobId, string status, int percent, string? outputPath = null, string? errorMessage = null);

    /// <summary>
    /// Get a job by its ID.
    /// </summary>
    Task<VideoJob?> GetJobAsync(string jobId);

    /// <summary>
    /// Clean up old completed jobs (older than specified timespan).
    /// </summary>
    Task<int> CleanupOldJobsAsync(TimeSpan olderThan);

    /// <summary>
    /// Subscribe to real-time job progress updates via Server-Sent Events.
    /// Yields VideoJob objects as updates occur.
    /// </summary>
    IAsyncEnumerable<VideoJob> SubscribeToJobUpdatesAsync(string jobId, CancellationToken cancellationToken);
}

/// <summary>
/// In-memory implementation of export job service with thread-safe operations
/// and real-time subscription support for SSE streaming.
/// </summary>
public class ExportJobService : IExportJobService
{
    private readonly ILogger<ExportJobService> _logger;
    private readonly ConcurrentDictionary<string, VideoJob> _jobs = new();
    private readonly ConcurrentDictionary<string, List<Channel<VideoJob>>> _subscribers = new();
    private readonly object _subscriberLock = new();

    public ExportJobService(ILogger<ExportJobService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<VideoJob> CreateJobAsync(VideoJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        _jobs[job.Id] = job;
        _logger.LogInformation("Created export job {JobId} with status {Status}", job.Id, job.Status);

        // Notify subscribers of job creation
        NotifySubscribers(job.Id, job);

        return Task.FromResult(job);
    }

    /// <inheritdoc />
    public Task UpdateJobProgressAsync(string jobId, int percent, string stage)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            var updatedJob = job with
            {
                Progress = Math.Clamp(percent, 0, 100),
                Stage = stage,
                Status = "running",
                StartedAt = job.StartedAt ?? DateTime.UtcNow
            };
            _jobs[jobId] = updatedJob;

            _logger.LogDebug("Updated export job {JobId} progress to {Percent}% - {Stage}", jobId, percent, stage);

            // Notify subscribers of progress update
            NotifySubscribers(jobId, updatedJob);
        }
        else
        {
            _logger.LogWarning("Attempted to update progress for non-existent job {JobId}", jobId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateJobStatusAsync(string jobId, string status, int percent, string? outputPath = null, string? errorMessage = null)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            var isTerminal = status is "completed" or "failed" or "cancelled";

            var updatedJob = job with
            {
                Status = status,
                Progress = Math.Clamp(percent, 0, 100),
                OutputPath = outputPath ?? job.OutputPath,
                ErrorMessage = errorMessage,
                CompletedAt = isTerminal ? DateTime.UtcNow : job.CompletedAt
            };
            _jobs[jobId] = updatedJob;

            _logger.LogInformation("Updated export job {JobId} status to {Status}", jobId, status);

            // Notify subscribers of status update
            NotifySubscribers(jobId, updatedJob);
        }
        else
        {
            _logger.LogWarning("Attempted to update status for non-existent job {JobId}", jobId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<VideoJob?> GetJobAsync(string jobId)
    {
        _jobs.TryGetValue(jobId, out var job);
        return Task.FromResult(job);
    }

    /// <inheritdoc />
    public Task<int> CleanupOldJobsAsync(TimeSpan olderThan)
    {
        var cutoffTime = DateTime.UtcNow - olderThan;
        var removedCount = 0;

        foreach (var kvp in _jobs)
        {
            if (kvp.Value.CompletedAt.HasValue && kvp.Value.CompletedAt.Value < cutoffTime)
            {
                if (_jobs.TryRemove(kvp.Key, out _))
                {
                    removedCount++;
                }
            }
        }

        if (removedCount > 0)
        {
            _logger.LogInformation("Cleaned up {Count} old export jobs", removedCount);
        }

        return Task.FromResult(removedCount);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<VideoJob> SubscribeToJobUpdatesAsync(
        string jobId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<VideoJob>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        // Register the subscriber
        lock (_subscriberLock)
        {
            if (!_subscribers.TryGetValue(jobId, out var channels))
            {
                channels = new List<Channel<VideoJob>>();
                _subscribers[jobId] = channels;
            }
            channels.Add(channel);
        }

        _logger.LogDebug("SSE subscriber added for job {JobId}", jobId);

        try
        {
            // Send initial job state if it exists
            var initialJob = await GetJobAsync(jobId).ConfigureAwait(false);
            if (initialJob != null)
            {
                yield return initialJob;

                // If job is already in terminal state, close immediately
                if (IsTerminalStatus(initialJob.Status))
                {
                    _logger.LogDebug("Job {JobId} already in terminal state {Status}, closing SSE stream", jobId, initialJob.Status);
                    yield break;
                }
            }

            // Stream updates as they come in
            await foreach (var update in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return update;

                // Stop streaming when job reaches terminal state
                if (IsTerminalStatus(update.Status))
                {
                    _logger.LogDebug("Job {JobId} reached terminal state {Status}, closing SSE stream", jobId, update.Status);
                    break;
                }
            }
        }
        finally
        {
            // Unsubscribe
            lock (_subscriberLock)
            {
                if (_subscribers.TryGetValue(jobId, out var channels))
                {
                    channels.Remove(channel);
                    if (channels.Count == 0)
                    {
                        _subscribers.TryRemove(jobId, out _);
                    }
                }
            }

            channel.Writer.TryComplete();
            _logger.LogDebug("SSE subscriber removed for job {JobId}", jobId);
        }
    }

    /// <summary>
    /// Notify all subscribers of a job update.
    /// </summary>
    private void NotifySubscribers(string jobId, VideoJob job)
    {
        List<Channel<VideoJob>>? channels;
        lock (_subscriberLock)
        {
            if (!_subscribers.TryGetValue(jobId, out channels) || channels.Count == 0)
            {
                return;
            }

            // Create a copy to avoid holding the lock during writes
            channels = new List<Channel<VideoJob>>(channels);
        }

        foreach (var channel in channels)
        {
            // Non-blocking write - drop if channel is full (shouldn't happen with unbounded)
            if (!channel.Writer.TryWrite(job))
            {
                _logger.LogWarning("Failed to write job update to subscriber channel for job {JobId}", jobId);
            }
        }
    }

    /// <summary>
    /// Check if a job status is terminal (completed, failed, or cancelled).
    /// </summary>
    private static bool IsTerminalStatus(string status)
    {
        return status is "completed" or "failed" or "cancelled";
    }
}
