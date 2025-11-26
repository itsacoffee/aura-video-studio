using System;
using System.Collections.Concurrent;
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
}

/// <summary>
/// In-memory implementation of export job service with thread-safe operations.
/// </summary>
public class ExportJobService : IExportJobService
{
    private readonly ILogger<ExportJobService> _logger;
    private readonly ConcurrentDictionary<string, VideoJob> _jobs = new();

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
}
