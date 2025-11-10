using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Jobs;
using Aura.Core.Orchestrator;
using Microsoft.Extensions.Logging;
using JobStatus = Aura.Core.Models.Jobs.JobStatus;

namespace Aura.Core.Services.Jobs;

/// <summary>
/// Service for managing background video generation jobs with Hangfire
/// </summary>
public class VideoGenerationJobService
{
    private readonly ILogger<VideoGenerationJobService> _logger;
    private readonly VideoOrchestrator _orchestrator;
    private readonly ConcurrentDictionary<string, VideoGenerationJob> _jobs;

    public VideoGenerationJobService(
        ILogger<VideoGenerationJobService> logger,
        VideoOrchestrator orchestrator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _jobs = new ConcurrentDictionary<string, VideoGenerationJob>();
    }

    /// <summary>
    /// Create and enqueue a new video generation job
    /// </summary>
    public string CreateJob(
        Brief brief,
        PlanSpec planSpec,
        VoiceSpec voiceSpec,
        RenderSpec renderSpec,
        SystemProfile systemProfile)
    {
        var jobId = Guid.NewGuid().ToString("N");
        var correlationId = Guid.NewGuid().ToString("N");
        
        var job = new VideoGenerationJob
        {
            JobId = jobId,
            CorrelationId = correlationId,
            Brief = brief,
            PlanSpec = planSpec,
            VoiceSpec = voiceSpec,
            RenderSpec = renderSpec,
            SystemProfile = systemProfile,
            Status = JobStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        
        _jobs[jobId] = job;
        
        _logger.LogInformation("Created video generation job: {JobId}", jobId);
        return jobId;
    }

    /// <summary>
    /// Execute a video generation job (called by Hangfire)
    /// </summary>
    public async Task ExecuteJobAsync(string jobId, CancellationToken ct = default)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
        {
            _logger.LogError("Job not found: {JobId}", jobId);
            throw new InvalidOperationException($"Job not found: {jobId}");
        }
        
        _logger.LogInformation("Starting video generation job: {JobId}", jobId);
        
        job.Status = JobStatus.Running;
        job.StartedAt = DateTime.UtcNow;
        
        try
        {
            var progress = new Progress<string>(message =>
            {
                UpdateJobProgress(jobId, "Generation", 50, message);
            });
            
            var outputPath = await _orchestrator.GenerateVideoAsync(
                job.Brief,
                job.PlanSpec,
                job.VoiceSpec,
                job.RenderSpec,
                job.SystemProfile,
                progress,
                ct,
                jobId,
                job.CorrelationId
            );
            
            job.Status = JobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.OutputPath = outputPath;
            
            _logger.LogInformation("Video generation job completed: {JobId}, Output: {OutputPath}", jobId, outputPath);
        }
        catch (OperationCanceledException)
        {
            job.Status = JobStatus.Cancelled;
            job.CompletedAt = DateTime.UtcNow;
            _logger.LogWarning("Video generation job cancelled: {JobId}", jobId);
            throw;
        }
        catch (Exception ex)
        {
            job.Status = JobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorMessage = ex.Message;
            job.RetryCount++;
            
            _logger.LogError(ex, "Video generation job failed: {JobId}, Attempt: {RetryCount}/{MaxRetries}", 
                jobId, job.RetryCount, job.MaxRetries);
            
            if (job.RetryCount < job.MaxRetries)
            {
                job.Status = JobStatus.Retrying;
                _logger.LogInformation("Job will be retried: {JobId}", jobId);
                throw; // Let Hangfire handle retry
            }
            
            throw;
        }
    }

    /// <summary>
    /// Get job status
    /// </summary>
    public VideoGenerationJob? GetJobStatus(string jobId)
    {
        return _jobs.TryGetValue(jobId, out var job) ? job : null;
    }

    /// <summary>
    /// Cancel a running job
    /// </summary>
    public bool CancelJob(string jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
        {
            _logger.LogWarning("Cannot cancel job - not found: {JobId}", jobId);
            return false;
        }
        
        if (job.Status != JobStatus.Running && job.Status != JobStatus.Pending)
        {
            _logger.LogWarning("Cannot cancel job - status is {Status}: {JobId}", job.Status, jobId);
            return false;
        }
        
        job.Status = JobStatus.Cancelled;
        job.CompletedAt = DateTime.UtcNow;
        
        _logger.LogInformation("Job cancelled: {JobId}", jobId);
        return true;
    }

    /// <summary>
    /// Get all jobs (with optional filtering)
    /// </summary>
    public List<VideoGenerationJob> GetJobs(Models.Jobs.JobStatus? statusFilter = null, int maxResults = 100)
    {
        var query = _jobs.Values.AsEnumerable();
        
        if (statusFilter.HasValue)
        {
            query = query.Where(j => j.Status == statusFilter.Value);
        }
        
        return query
            .OrderByDescending(j => j.CreatedAt)
            .Take(maxResults)
            .ToList();
    }

    /// <summary>
    /// Clean up old completed/failed jobs
    /// </summary>
    public int CleanupOldJobs(TimeSpan olderThan)
    {
        var cutoffTime = DateTime.UtcNow - olderThan;
        var jobsToRemove = _jobs
            .Where(kv => kv.Value.CompletedAt.HasValue && 
                        kv.Value.CompletedAt.Value < cutoffTime &&
                        (kv.Value.Status == JobStatus.Completed || 
                         kv.Value.Status == JobStatus.Failed ||
                         kv.Value.Status == JobStatus.Cancelled))
            .Select(kv => kv.Key)
            .ToList();
        
        foreach (var jobId in jobsToRemove)
        {
            _jobs.TryRemove(jobId, out _);
        }
        
        if (jobsToRemove.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} old jobs", jobsToRemove.Count);
        }
        
        return jobsToRemove.Count;
    }

    private void UpdateJobProgress(string jobId, string stage, double percent, string message)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.ProgressUpdates.Add(new JobProgressUpdate
            {
                Timestamp = DateTime.UtcNow,
                Stage = stage,
                PercentComplete = percent,
                Message = message
            });
            
            // Keep only last 50 progress updates to avoid memory bloat
            if (job.ProgressUpdates.Count > 50)
            {
                job.ProgressUpdates.RemoveAt(0);
            }
        }
    }
}
