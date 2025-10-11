using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Artifacts;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Orchestrator;

/// <summary>
/// Manages video generation jobs with background execution and progress tracking.
/// </summary>
public class JobRunner
{
    private readonly ILogger<JobRunner> _logger;
    private readonly ArtifactManager _artifactManager;
    private readonly VideoOrchestrator _orchestrator;
    private readonly Dictionary<string, Job> _activeJobs = new();

    public event EventHandler<JobProgressEventArgs>? JobProgress;

    public JobRunner(
        ILogger<JobRunner> logger,
        ArtifactManager artifactManager,
        VideoOrchestrator orchestrator)
    {
        _logger = logger;
        _artifactManager = artifactManager;
        _orchestrator = orchestrator;
    }

    /// <summary>
    /// Creates a new job and starts execution.
    /// </summary>
    public async Task<Job> CreateAndStartJobAsync(
        Brief brief,
        PlanSpec planSpec,
        VoiceSpec voiceSpec,
        RenderSpec renderSpec,
        string? correlationId = null,
        CancellationToken ct = default)
    {
        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Stage = "Script",
            Status = JobStatus.Queued,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString(),
            Brief = brief,
            PlanSpec = planSpec,
            VoiceSpec = voiceSpec,
            RenderSpec = renderSpec
        };

        _activeJobs[job.Id] = job;
        _artifactManager.SaveJob(job);

        // Start execution in background
        _ = Task.Run(async () => await ExecuteJobAsync(job.Id, ct), ct);

        return job;
    }

    /// <summary>
    /// Gets a job by ID.
    /// </summary>
    public Job? GetJob(string jobId)
    {
        if (_activeJobs.TryGetValue(jobId, out var job))
        {
            return job;
        }

        return _artifactManager.LoadJob(jobId);
    }

    /// <summary>
    /// Lists all recent jobs.
    /// </summary>
    public List<Job> ListJobs(int limit = 50)
    {
        return _artifactManager.ListJobs(limit);
    }

    /// <summary>
    /// Executes a job through all stages.
    /// </summary>
    private async Task ExecuteJobAsync(string jobId, CancellationToken ct)
    {
        try
        {
            var job = GetJob(jobId);
            if (job == null)
            {
                _logger.LogError("Job {JobId} not found", jobId);
                return;
            }

            _logger.LogInformation("Starting job {JobId}", jobId);
            
            // Update to running status
            job = UpdateJob(job, status: JobStatus.Running, percent: 0);

            // Create progress reporter
            var progress = new Progress<string>(message =>
            {
                _logger.LogInformation("[Job {JobId}] {Message}", jobId, message);
                job = UpdateJob(job, logs: new List<string>(job.Logs) { $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}" });
            });

            // Execute orchestrator
            var outputPath = await _orchestrator.GenerateVideoAsync(
                job.Brief!,
                job.PlanSpec!,
                job.VoiceSpec!,
                job.RenderSpec!,
                progress,
                ct
            );

            // Add final artifact
            var artifact = _artifactManager.CreateArtifact(jobId, "video.mp4", outputPath, "video/mp4");
            var artifacts = new List<JobArtifact>(job.Artifacts) { artifact };

            // Mark as done
            job = UpdateJob(job, 
                status: JobStatus.Done, 
                percent: 100, 
                stage: "Complete",
                artifacts: artifacts,
                finishedAt: DateTime.UtcNow);

            _logger.LogInformation("Job {JobId} completed successfully", jobId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Job {JobId} was cancelled", jobId);
            var job = GetJob(jobId);
            if (job != null)
            {
                UpdateJob(job, status: JobStatus.Failed, errorMessage: "Job was cancelled");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} failed: {Message}", jobId, ex.Message);
            var job = GetJob(jobId);
            if (job != null)
            {
                UpdateJob(job, status: JobStatus.Failed, errorMessage: ex.Message);
            }
        }
        finally
        {
            _activeJobs.Remove(jobId);
        }
    }

    /// <summary>
    /// Updates a job and persists changes.
    /// </summary>
    private Job UpdateJob(
        Job job,
        string? stage = null,
        JobStatus? status = null,
        int? percent = null,
        TimeSpan? eta = null,
        List<JobArtifact>? artifacts = null,
        List<string>? logs = null,
        DateTime? finishedAt = null,
        string? errorMessage = null)
    {
        var updated = job with
        {
            Stage = stage ?? job.Stage,
            Status = status ?? job.Status,
            Percent = percent ?? job.Percent,
            Eta = eta ?? job.Eta,
            Artifacts = artifacts ?? job.Artifacts,
            Logs = logs ?? job.Logs,
            FinishedAt = finishedAt ?? job.FinishedAt,
            ErrorMessage = errorMessage ?? job.ErrorMessage
        };

        _activeJobs[job.Id] = updated;
        _artifactManager.SaveJob(updated);

        // Raise progress event
        JobProgress?.Invoke(this, new JobProgressEventArgs(updated));

        return updated;
    }
}

/// <summary>
/// Event args for job progress updates.
/// </summary>
public class JobProgressEventArgs : EventArgs
{
    public Job Job { get; }

    public JobProgressEventArgs(Job job)
    {
        Job = job;
    }
}
