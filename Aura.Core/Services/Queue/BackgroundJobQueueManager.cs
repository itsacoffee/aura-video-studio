using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Data;
using Aura.Core.Models;
using Aura.Core.Orchestrator;
using Aura.Core.Services.Generation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Queue;

/// <summary>
/// Manages background job queue with persistence, priority scheduling, and resource management
/// Ensures jobs survive application restarts and provides concurrent execution with limits
/// </summary>
public class BackgroundJobQueueManager
{
    private readonly ILogger<BackgroundJobQueueManager> _logger;
    private readonly IDbContextFactory<AuraDbContext> _dbContextFactory;
    private readonly JobRunner _jobRunner;
    private readonly ResourceMonitor _resourceMonitor;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeJobs = new();
    private readonly SemaphoreSlim _concurrencyLimiter;
    private readonly string _workerId;
    private int _maxConcurrentJobs = 2;
    
    public event EventHandler<JobStatusChangedEventArgs>? JobStatusChanged;
    public event EventHandler<JobProgressEventArgs>? JobProgressUpdated;

    public BackgroundJobQueueManager(
        ILogger<BackgroundJobQueueManager> logger,
        IDbContextFactory<AuraDbContext> dbContextFactory,
        JobRunner jobRunner,
        ResourceMonitor resourceMonitor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        _jobRunner = jobRunner ?? throw new ArgumentNullException(nameof(jobRunner));
        _resourceMonitor = resourceMonitor ?? throw new ArgumentNullException(nameof(resourceMonitor));
        
        _workerId = $"{Environment.MachineName}-{Guid.NewGuid():N}";
        _concurrencyLimiter = new SemaphoreSlim(_maxConcurrentJobs, _maxConcurrentJobs);
        
        _logger.LogInformation("BackgroundJobQueueManager initialized with WorkerId: {WorkerId}", _workerId);
    }

    /// <summary>
    /// Enqueues a new job for background processing
    /// </summary>
    public async Task<string> EnqueueJobAsync(
        Brief brief,
        PlanSpec planSpec,
        VoiceSpec voiceSpec,
        RenderSpec renderSpec,
        int priority = 5,
        string? correlationId = null,
        bool isQuickDemo = false,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(brief);
        ArgumentNullException.ThrowIfNull(planSpec);
        ArgumentNullException.ThrowIfNull(voiceSpec);
        ArgumentNullException.ThrowIfNull(renderSpec);
        
        var jobId = Guid.NewGuid().ToString("N");
        
        // Serialize job data
        var jobData = new JobData
        {
            Brief = brief,
            PlanSpec = planSpec,
            VoiceSpec = voiceSpec,
            RenderSpec = renderSpec
        };
        var jobDataJson = JsonSerializer.Serialize(jobData);
        
        await using var context = await _dbContextFactory.CreateDbContextAsync(ct);
        
        var jobEntity = new JobQueueEntity
        {
            JobId = jobId,
            Priority = Math.Clamp(priority, 1, 10),
            Status = "Pending",
            JobDataJson = jobDataJson,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString("N"),
            EnqueuedAt = DateTime.UtcNow,
            IsQuickDemo = isQuickDemo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        context.JobQueue.Add(jobEntity);
        await context.SaveChangesAsync(ct);
        
        _logger.LogInformation(
            "Job {JobId} enqueued with priority {Priority} (Topic: {Topic}, IsQuickDemo: {IsQuickDemo})",
            jobId, priority, brief.Topic, isQuickDemo);
        
        // Raise event
        JobStatusChanged?.Invoke(this, new JobStatusChangedEventArgs
        {
            JobId = jobId,
            NewStatus = "Pending",
            CorrelationId = jobEntity.CorrelationId
        });
        
        return jobId;
    }

    /// <summary>
    /// Gets the next pending job from the queue based on priority
    /// </summary>
    public async Task<JobQueueEntity?> DequeueNextJobAsync(CancellationToken ct = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(ct);
        
        // Get configuration to check if queue is enabled
        var config = await context.QueueConfiguration.FirstOrDefaultAsync(ct);
        if (config?.IsEnabled == false)
        {
            _logger.LogDebug("Queue is disabled, skipping dequeue");
            return null;
        }
        
        // Check resource constraints
        if (!await CanStartNewJobAsync(config, ct))
        {
            _logger.LogDebug("Resource constraints prevent starting new job");
            return null;
        }
        
        // Find next job with retry logic
        var nextJob = await context.JobQueue
            .Where(j => j.Status == "Pending" || 
                       (j.Status == "Failed" && 
                        j.RetryCount < j.MaxRetries && 
                        (j.NextRetryAt == null || j.NextRetryAt <= DateTime.UtcNow)))
            .OrderBy(j => j.Priority)
            .ThenBy(j => j.EnqueuedAt)
            .FirstOrDefaultAsync(ct);
        
        if (nextJob != null)
        {
            // Mark as processing
            nextJob.Status = "Processing";
            nextJob.WorkerId = _workerId;
            nextJob.StartedAt = DateTime.UtcNow;
            nextJob.UpdatedAt = DateTime.UtcNow;
            
            await context.SaveChangesAsync(ct);
            
            _logger.LogInformation(
                "Dequeued job {JobId} (Priority: {Priority}, RetryCount: {RetryCount})",
                nextJob.JobId, nextJob.Priority, nextJob.RetryCount);
        }
        
        return nextJob;
    }

    /// <summary>
    /// Processes a job from the queue
    /// </summary>
    public async Task ProcessJobAsync(JobQueueEntity jobEntity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(jobEntity);
        
        var jobId = jobEntity.JobId;
        _logger.LogInformation("Starting processing of job {JobId}", jobId);
        
        // Create cancellation token source for this job
        var jobCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _activeJobs[jobId] = jobCts;
        
        try
        {
            // Deserialize job data
            var jobData = JsonSerializer.Deserialize<JobData>(jobEntity.JobDataJson);
            if (jobData == null)
            {
                throw new InvalidOperationException("Failed to deserialize job data");
            }
            
            // Subscribe to job runner progress
            _jobRunner.JobProgress += (sender, args) =>
            {
                if (args.JobId == jobId)
                {
                    _ = UpdateJobProgressAsync(jobId, args, CancellationToken.None);
                    JobProgressUpdated?.Invoke(this, args);
                }
            };
            
            // Execute job
            var job = await _jobRunner.CreateAndStartJobAsync(
                jobData.Brief!,
                jobData.PlanSpec!,
                jobData.VoiceSpec!,
                jobData.RenderSpec!,
                jobEntity.CorrelationId,
                jobEntity.IsQuickDemo,
                jobCts.Token);
            
            // Wait for completion by polling job status
            while (!jobCts.Token.IsCancellationRequested)
            {
                await Task.Delay(1000, jobCts.Token);
                
                var currentJob = _jobRunner.GetJob(jobId);
                if (currentJob == null) break;
                
                if (currentJob.Status == JobStatus.Done || 
                    currentJob.Status == JobStatus.Succeeded)
                {
                    await MarkJobCompletedAsync(jobEntity, currentJob.OutputPath, ct);
                    break;
                }
                else if (currentJob.Status == JobStatus.Failed)
                {
                    await MarkJobFailedAsync(jobEntity, currentJob.ErrorMessage ?? "Job failed", ct);
                    break;
                }
                else if (currentJob.Status == JobStatus.Canceled)
                {
                    await MarkJobCancelledAsync(jobEntity, ct);
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Job {JobId} was cancelled", jobId);
            await MarkJobCancelledAsync(jobEntity, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job {JobId}", jobId);
            await MarkJobFailedAsync(jobEntity, ex.Message, ct);
        }
        finally
        {
            _activeJobs.TryRemove(jobId, out _);
            jobCts.Dispose();
        }
    }

    /// <summary>
    /// Cancels a job in the queue
    /// </summary>
    public async Task<bool> CancelJobAsync(string jobId, CancellationToken ct = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(ct);
        
        var job = await context.JobQueue.FindAsync(new object[] { jobId }, ct);
        if (job == null)
        {
            _logger.LogWarning("Cannot cancel job {JobId}: not found", jobId);
            return false;
        }
        
        // If job is actively processing, trigger cancellation
        if (_activeJobs.TryGetValue(jobId, out var cts))
        {
            cts.Cancel();
        }
        
        // Also cancel in JobRunner
        _jobRunner.CancelJob(jobId);
        
        // Update database
        job.Status = "Cancelled";
        job.CompletedAt = DateTime.UtcNow;
        job.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync(ct);
        
        _logger.LogInformation("Job {JobId} cancelled", jobId);
        
        JobStatusChanged?.Invoke(this, new JobStatusChangedEventArgs
        {
            JobId = jobId,
            NewStatus = "Cancelled",
            CorrelationId = job.CorrelationId
        });
        
        return true;
    }

    /// <summary>
    /// Gets all jobs with optional filtering
    /// </summary>
    public async Task<List<JobQueueEntity>> GetJobsAsync(
        string? status = null,
        int limit = 100,
        CancellationToken ct = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(ct);
        
        var query = context.JobQueue.AsQueryable();
        
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(j => j.Status == status);
        }
        
        return await query
            .OrderByDescending(j => j.EnqueuedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Gets job by ID
    /// </summary>
    public async Task<JobQueueEntity?> GetJobAsync(string jobId, CancellationToken ct = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(ct);
        return await context.JobQueue.FindAsync(new object[] { jobId }, ct);
    }

    /// <summary>
    /// Gets queue statistics
    /// </summary>
    public async Task<QueueStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(ct);
        
        var stats = new QueueStatistics
        {
            TotalJobs = await context.JobQueue.CountAsync(ct),
            PendingJobs = await context.JobQueue.CountAsync(j => j.Status == "Pending", ct),
            ProcessingJobs = await context.JobQueue.CountAsync(j => j.Status == "Processing", ct),
            CompletedJobs = await context.JobQueue.CountAsync(j => j.Status == "Completed", ct),
            FailedJobs = await context.JobQueue.CountAsync(j => j.Status == "Failed", ct),
            CancelledJobs = await context.JobQueue.CountAsync(j => j.Status == "Cancelled", ct),
            ActiveWorkers = _activeJobs.Count
        };
        
        return stats;
    }

    /// <summary>
    /// Cleans up old completed jobs based on retention policy
    /// </summary>
    public async Task<int> CleanupOldJobsAsync(CancellationToken ct = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(ct);
        
        var config = await context.QueueConfiguration.FirstOrDefaultAsync(ct);
        if (config == null) return 0;
        
        var completedCutoff = DateTime.UtcNow.AddDays(-config.JobHistoryRetentionDays);
        var failedCutoff = DateTime.UtcNow.AddDays(-config.FailedJobRetentionDays);
        
        var jobsToDelete = await context.JobQueue
            .Where(j => (j.Status == "Completed" && j.CompletedAt < completedCutoff) ||
                       (j.Status == "Failed" && j.CompletedAt < failedCutoff) ||
                       (j.Status == "Cancelled" && j.CompletedAt < completedCutoff))
            .ToListAsync(ct);
        
        if (jobsToDelete.Any())
        {
            context.JobQueue.RemoveRange(jobsToDelete);
            await context.SaveChangesAsync(ct);
            
            _logger.LogInformation("Cleaned up {Count} old jobs", jobsToDelete.Count);
        }
        
        return jobsToDelete.Count;
    }

    /// <summary>
    /// Updates queue configuration
    /// </summary>
    public async Task UpdateConfigurationAsync(
        int? maxConcurrentJobs = null,
        bool? isEnabled = null,
        CancellationToken ct = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(ct);
        
        var config = await context.QueueConfiguration.FirstOrDefaultAsync(ct);
        if (config == null)
        {
            config = new QueueConfigurationEntity { Id = 1 };
            context.QueueConfiguration.Add(config);
        }
        
        if (maxConcurrentJobs.HasValue)
        {
            config.MaxConcurrentJobs = Math.Clamp(maxConcurrentJobs.Value, 1, 10);
            _maxConcurrentJobs = config.MaxConcurrentJobs;
            
            // Recreate semaphore with new limit
            _concurrencyLimiter?.Dispose();
            var newSemaphore = new SemaphoreSlim(config.MaxConcurrentJobs, config.MaxConcurrentJobs);
            typeof(BackgroundJobQueueManager)
                .GetField("_concurrencyLimiter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(this, newSemaphore);
        }
        
        if (isEnabled.HasValue)
        {
            config.IsEnabled = isEnabled.Value;
        }
        
        config.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        
        _logger.LogInformation(
            "Queue configuration updated: MaxConcurrent={Max}, Enabled={Enabled}",
            config.MaxConcurrentJobs, config.IsEnabled);
    }

    /// <summary>
    /// Gets current queue configuration
    /// </summary>
    public async Task<QueueConfigurationEntity> GetConfigurationAsync(CancellationToken ct = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(ct);
        return await context.QueueConfiguration.FirstOrDefaultAsync(ct) ?? new QueueConfigurationEntity();
    }

    // Private helper methods

    private async Task MarkJobCompletedAsync(
        JobQueueEntity jobEntity,
        string? outputPath,
        CancellationToken ct)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(ct);
        
        var job = await context.JobQueue.FindAsync(new object[] { jobEntity.JobId }, ct);
        if (job != null)
        {
            job.Status = "Completed";
            job.CompletedAt = DateTime.UtcNow;
            job.OutputPath = outputPath;
            job.ProgressPercent = 100;
            job.UpdatedAt = DateTime.UtcNow;
            
            await context.SaveChangesAsync(ct);
            
            _logger.LogInformation("Job {JobId} completed successfully", jobEntity.JobId);
            
            JobStatusChanged?.Invoke(this, new JobStatusChangedEventArgs
            {
                JobId = jobEntity.JobId,
                NewStatus = "Completed",
                CorrelationId = job.CorrelationId,
                OutputPath = outputPath
            });
        }
    }

    private async Task MarkJobFailedAsync(
        JobQueueEntity jobEntity,
        string errorMessage,
        CancellationToken ct)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(ct);
        
        var job = await context.JobQueue.FindAsync(new object[] { jobEntity.JobId }, ct);
        if (job != null)
        {
            job.Status = "Failed";
            job.LastError = errorMessage;
            job.RetryCount++;
            job.UpdatedAt = DateTime.UtcNow;
            
            // Calculate next retry time with exponential backoff
            if (job.RetryCount < job.MaxRetries)
            {
                var config = await context.QueueConfiguration.FirstOrDefaultAsync(ct);
                var baseDelay = config?.RetryBaseDelaySeconds ?? 5;
                var maxDelay = config?.RetryMaxDelaySeconds ?? 300;
                
                var delaySeconds = Math.Min(baseDelay * Math.Pow(2, job.RetryCount), maxDelay);
                job.NextRetryAt = DateTime.UtcNow.AddSeconds(delaySeconds);
                
                _logger.LogWarning(
                    "Job {JobId} failed (retry {RetryCount}/{MaxRetries}), will retry at {RetryAt}",
                    jobEntity.JobId, job.RetryCount, job.MaxRetries, job.NextRetryAt);
            }
            else
            {
                job.CompletedAt = DateTime.UtcNow;
                _logger.LogError(
                    "Job {JobId} failed permanently after {RetryCount} retries: {Error}",
                    jobEntity.JobId, job.RetryCount, errorMessage);
            }
            
            await context.SaveChangesAsync(ct);
            
            JobStatusChanged?.Invoke(this, new JobStatusChangedEventArgs
            {
                JobId = jobEntity.JobId,
                NewStatus = "Failed",
                CorrelationId = job.CorrelationId,
                ErrorMessage = errorMessage
            });
        }
    }

    private async Task MarkJobCancelledAsync(JobQueueEntity jobEntity, CancellationToken ct)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(ct);
        
        var job = await context.JobQueue.FindAsync(new object[] { jobEntity.JobId }, ct);
        if (job != null)
        {
            job.Status = "Cancelled";
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            
            await context.SaveChangesAsync(ct);
            
            JobStatusChanged?.Invoke(this, new JobStatusChangedEventArgs
            {
                JobId = jobEntity.JobId,
                NewStatus = "Cancelled",
                CorrelationId = job.CorrelationId
            });
        }
    }

    private async Task UpdateJobProgressAsync(
        string jobId,
        JobProgressEventArgs progressArgs,
        CancellationToken ct)
    {
        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            
            var job = await context.JobQueue.FindAsync(new object[] { jobId }, ct);
            if (job != null)
            {
                job.ProgressPercent = progressArgs.Progress;
                job.CurrentStage = progressArgs.Stage;
                job.UpdatedAt = DateTime.UtcNow;
                
                await context.SaveChangesAsync(ct);
            }
            
            // Save progress history
            var historyEntry = new JobProgressHistoryEntity
            {
                JobId = jobId,
                Stage = progressArgs.Stage,
                ProgressPercent = progressArgs.Progress,
                Message = progressArgs.Message,
                Timestamp = DateTime.UtcNow
            };
            
            context.JobProgressHistory.Add(historyEntry);
            await context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update progress for job {JobId}", jobId);
        }
    }

    private async Task<bool> CanStartNewJobAsync(
        QueueConfigurationEntity? config,
        CancellationToken ct)
    {
        // Check concurrent job limit
        if (_activeJobs.Count >= (config?.MaxConcurrentJobs ?? 2))
        {
            return false;
        }
        
        // Check resource constraints
        var snapshot = _resourceMonitor.GetCurrentSnapshot();
        if (snapshot.CpuUsagePercent > (config?.CpuThrottleThreshold ?? 85))
        {
            _logger.LogDebug("CPU usage too high: {CpuUsage}%", snapshot.CpuUsagePercent);
            return false;
        }
        
        if (snapshot.MemoryUsagePercent > (config?.MemoryThrottleThreshold ?? 85))
        {
            _logger.LogDebug("Memory usage too high: {MemoryUsage}%", snapshot.MemoryUsagePercent);
            return false;
        }
        
        // Check power mode (battery detection)
        if (config?.PauseOnBattery == true && IsOnBatteryPower())
        {
            _logger.LogDebug("On battery power, pausing queue");
            return false;
        }
        
        return true;
    }

    private bool IsOnBatteryPower()
    {
        try
        {
            var powerStatus = System.Windows.Forms.SystemInformation.PowerStatus;
            return powerStatus.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Offline;
        }
        catch
        {
            // If we can't detect power status, assume we're on AC power
            return false;
        }
    }
}

/// <summary>
/// Job data for serialization
/// </summary>
internal class JobData
{
    public Brief? Brief { get; set; }
    public PlanSpec? PlanSpec { get; set; }
    public VoiceSpec? VoiceSpec { get; set; }
    public RenderSpec? RenderSpec { get; set; }
}

/// <summary>
/// Queue statistics
/// </summary>
public class QueueStatistics
{
    public int TotalJobs { get; set; }
    public int PendingJobs { get; set; }
    public int ProcessingJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int FailedJobs { get; set; }
    public int CancelledJobs { get; set; }
    public int ActiveWorkers { get; set; }
}

/// <summary>
/// Event args for job status changes
/// </summary>
public class JobStatusChangedEventArgs : EventArgs
{
    public required string JobId { get; init; }
    public required string NewStatus { get; init; }
    public string? CorrelationId { get; init; }
    public string? OutputPath { get; init; }
    public string? ErrorMessage { get; init; }
}
