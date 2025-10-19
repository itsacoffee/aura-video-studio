using System;
using System.Collections.Generic;
using System.IO;
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
                var failureDetails = CreateFailureDetails(job, ex);
                
                // Add error to logs so it's visible in UI
                var errorLog = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ERROR: {GetFriendlyErrorMessage(ex)}";
                var updatedLogs = new List<string>(job.Logs) { errorLog };
                
                UpdateJob(job, 
                    status: JobStatus.Failed, 
                    errorMessage: ex.Message, 
                    failureDetails: failureDetails,
                    logs: updatedLogs);
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
        string? errorMessage = null,
        JobFailure? failureDetails = null)
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
            ErrorMessage = errorMessage ?? job.ErrorMessage,
            FailureDetails = failureDetails ?? job.FailureDetails
        };

        _activeJobs[job.Id] = updated;
        _artifactManager.SaveJob(updated);

        // Raise progress event
        JobProgress?.Invoke(this, new JobProgressEventArgs(updated));

        return updated;
    }

    /// <summary>
    /// Creates detailed failure information from an exception
    /// </summary>
    private JobFailure CreateFailureDetails(Job job, Exception ex)
    {
        var failureDetails = new JobFailure
        {
            Stage = job.Stage,
            Message = GetFriendlyErrorMessage(ex),
            CorrelationId = job.CorrelationId ?? string.Empty,
            FailedAt = DateTime.UtcNow
        };

        // Check if this is an FFmpeg-related error
        if (ex.Message.Contains("FFmpeg") || ex.Message.Contains("render failed"))
        {
            failureDetails = failureDetails with
            {
                ErrorCode = "E304-FFMPEG_RUNTIME",
                StderrSnippet = TryReadFfmpegLog(job.Id),
                LogPath = GetFfmpegLogPath(job.Id),
                SuggestedActions = new[]
                {
                    "Try using software encoder (x264) instead of hardware acceleration",
                    "Verify FFmpeg is properly installed using Dependencies page",
                    "Check the full log for detailed error information",
                    "Retry the render with different settings"
                }
            };
        }
        else
        {
            failureDetails = failureDetails with
            {
                SuggestedActions = new[]
                {
                    "Check the logs for more details",
                    "Verify all dependencies are installed",
                    "Retry the operation",
                    "Contact support if the issue persists"
                }
            };
        }

        return failureDetails;
    }

    /// <summary>
    /// Attempts to read the last 16KB of FFmpeg log for a job
    /// </summary>
    private string? TryReadFfmpegLog(string jobId)
    {
        try
        {
            var logPath = GetFfmpegLogPath(jobId);
            if (!File.Exists(logPath))
            {
                return null;
            }

            // Read last 16KB of the log file
            var fileInfo = new FileInfo(logPath);
            var maxBytes = 16 * 1024; // 16KB

            using var fileStream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            
            if (fileInfo.Length <= maxBytes)
            {
                // Read entire file if smaller than max
                using var reader = new StreamReader(fileStream);
                return reader.ReadToEnd();
            }
            else
            {
                // Read last 16KB
                fileStream.Seek(-maxBytes, SeekOrigin.End);
                using var reader = new StreamReader(fileStream);
                var snippet = reader.ReadToEnd();
                return $"... (showing last 16KB)\n{snippet}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read FFmpeg log for job {JobId}", jobId);
            return null;
        }
    }

    /// <summary>
    /// Gets the path to the FFmpeg log file for a job
    /// </summary>
    private string GetFfmpegLogPath(string jobId)
    {
        var logsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Aura", "Logs", "ffmpeg");
        return Path.Combine(logsDir, $"{jobId}.log");
    }

    /// <summary>
    /// Extracts a friendly error message from an exception
    /// </summary>
    private string GetFriendlyErrorMessage(Exception ex)
    {
        // Extract the first line of the exception message for a cleaner display
        var message = ex.Message.Split('\n')[0];
        
        // Try to extract a user-friendly portion
        if (message.Contains("FFmpeg render failed"))
        {
            return "Render failed due to FFmpeg error";
        }
        
        return message.Length > 200 ? message.Substring(0, 197) + "..." : message;
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
