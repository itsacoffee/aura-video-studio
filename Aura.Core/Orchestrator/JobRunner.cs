using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Artifacts;
using Aura.Core.Models;
using Aura.Core.Models.Events;
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
    private readonly Aura.Core.Hardware.HardwareDetector _hardwareDetector;
    private readonly Dictionary<string, Job> _activeJobs = new();
    private readonly Dictionary<string, CancellationTokenSource> _jobCancellationTokens = new();

    public event EventHandler<JobProgressEventArgs>? JobProgress;

    public JobRunner(
        ILogger<JobRunner> logger,
        ArtifactManager artifactManager,
        VideoOrchestrator orchestrator,
        Aura.Core.Hardware.HardwareDetector hardwareDetector)
    {
        _logger = logger;
        _artifactManager = artifactManager;
        _orchestrator = orchestrator;
        _hardwareDetector = hardwareDetector;
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

        // Create a linked cancellation token source that responds to both the provided token and manual cancellation
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _jobCancellationTokens[job.Id] = linkedCts;

        // Start execution in background
        _ = Task.Run(async () => await ExecuteJobAsync(job.Id, linkedCts.Token), linkedCts.Token);

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
    /// Cancels a running job.
    /// </summary>
    /// <param name="jobId">The ID of the job to cancel</param>
    /// <returns>True if the job was found and cancellation was requested, false otherwise</returns>
    public bool CancelJob(string jobId)
    {
        _logger.LogInformation("Cancellation requested for job {JobId}", jobId);
        
        // Check if job is active and has a cancellation token
        if (_jobCancellationTokens.TryGetValue(jobId, out var cts))
        {
            try
            {
                cts.Cancel();
                _logger.LogInformation("Cancellation token triggered for job {JobId}", jobId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling job {JobId}", jobId);
                return false;
            }
        }
        
        // Job not found or not active
        var job = GetJob(jobId);
        if (job == null)
        {
            _logger.LogWarning("Cannot cancel job {JobId}: job not found", jobId);
            return false;
        }
        
        // Job exists but is not running
        if (job.Status != JobStatus.Running && job.Status != JobStatus.Queued)
        {
            _logger.LogWarning("Cannot cancel job {JobId}: job is in status {Status}", jobId, job.Status);
            return false;
        }
        
        _logger.LogWarning("Job {JobId} is marked as active but has no cancellation token", jobId);
        return false;
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
            
            // Update to running status with initialization message
            job = UpdateJob(job, 
                status: JobStatus.Running, 
                percent: 0, 
                stage: "Initialization",
                progressMessage: "Initializing job execution");

            // Detect system profile for orchestration
            _logger.LogInformation("[Job {JobId}] Detecting system hardware...", jobId);
            var systemProfile = await _hardwareDetector.DetectSystemAsync().ConfigureAwait(false);
            _logger.LogInformation("[Job {JobId}] System Profile - Tier: {Tier}, CPU: {Cores} cores, RAM: {Ram}GB, GPU: {Gpu}", 
                jobId, systemProfile.Tier, systemProfile.LogicalCores, systemProfile.RamGB, 
                systemProfile.Gpu?.Model ?? "None");

            // Create progress reporter with detailed stage tracking
            var progress = new Progress<string>(message =>
            {
                _logger.LogInformation("[Job {JobId}] {Message}", jobId, message);
                
                // Determine stage and progress from message
                var (stage, percent, progressMsg) = ParseProgressMessage(message, job.Stage, job.Percent);
                
                job = UpdateJob(job, 
                    stage: stage,
                    percent: percent,
                    logs: new List<string>(job.Logs) { $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}" },
                    progressMessage: progressMsg);
            });

            // Execute orchestrator with system profile
            var outputPath = await _orchestrator.GenerateVideoAsync(
                job.Brief!,
                job.PlanSpec!,
                job.VoiceSpec!,
                job.RenderSpec!,
                systemProfile,
                progress,
                ct
            ).ConfigureAwait(false);

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

            _logger.LogInformation("Job {JobId} completed successfully. Output: {OutputPath}", jobId, outputPath);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Job {JobId} was cancelled", jobId);
            var job = GetJob(jobId);
            if (job != null)
            {
                // Add cancellation message to logs so it's visible in UI
                var cancelLog = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Job was cancelled by user";
                var updatedLogs = new List<string>(job.Logs) { cancelLog };
                
                UpdateJob(job, 
                    status: JobStatus.Failed, 
                    errorMessage: "Job was cancelled",
                    logs: updatedLogs);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} failed: {Message}\nStack Trace: {StackTrace}", 
                jobId, ex.Message, ex.StackTrace);
            var job = GetJob(jobId);
            if (job != null)
            {
                var failureDetails = CreateFailureDetails(job, ex);
                
                // Add error to logs so it's visible in UI
                var errorLog = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ERROR: {GetFriendlyErrorMessage(ex)}";
                var stackLog = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Stack Trace: {ex.StackTrace}";
                var updatedLogs = new List<string>(job.Logs) { errorLog, stackLog };
                
                UpdateJob(job, 
                    status: JobStatus.Failed, 
                    errorMessage: ex.Message, 
                    failureDetails: failureDetails,
                    logs: updatedLogs,
                    finishedAt: DateTime.UtcNow);
                
                // Save job state to artifact manager
                _artifactManager.SaveJob(job);
            }
        }
        finally
        {
            _activeJobs.Remove(jobId);
            
            // Clean up cancellation token
            if (_jobCancellationTokens.TryGetValue(jobId, out var cts))
            {
                cts.Dispose();
                _jobCancellationTokens.Remove(jobId);
            }
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
        JobFailure? failureDetails = null,
        string? progressMessage = null)
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

        // Raise progress event with detailed information
        var eventArgs = new JobProgressEventArgs(
            jobId: updated.Id,
            progress: updated.Percent,
            status: updated.Status,
            stage: updated.Stage,
            message: progressMessage ?? GetProgressMessage(updated),
            correlationId: updated.CorrelationId ?? string.Empty,
            eta: updated.Eta
        );
        
        JobProgress?.Invoke(this, eventArgs);

        return updated;
    }

    /// <summary>
    /// Gets a human-readable progress message for a job
    /// </summary>
    private string GetProgressMessage(Job job)
    {
        if (!string.IsNullOrEmpty(job.ErrorMessage) && job.Status == JobStatus.Failed)
        {
            return job.ErrorMessage;
        }

        return job.Status switch
        {
            JobStatus.Queued => "Job is queued for execution",
            JobStatus.Running => $"Processing: {job.Stage}",
            JobStatus.Done => "Job completed successfully",
            JobStatus.Failed => "Job failed",
            JobStatus.Canceled => "Job was canceled",
            _ => $"Status: {job.Status}"
        };
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

    /// <summary>
    /// Parses a progress message to extract stage, progress percentage, and formatted message
    /// </summary>
    private (string stage, int percent, string message) ParseProgressMessage(string message, string currentStage, int currentPercent)
    {
        var stage = currentStage;
        var percent = currentPercent;
        var formattedMessage = message;

        // Parse stage transitions from orchestrator messages
        if (message.Contains("Generating script", StringComparison.OrdinalIgnoreCase))
        {
            stage = "Script";
            percent = 10;
            formattedMessage = "Generating video script";
        }
        else if (message.Contains("Generating narration", StringComparison.OrdinalIgnoreCase) || 
                 message.Contains("voice", StringComparison.OrdinalIgnoreCase))
        {
            stage = "Voice";
            percent = 30;
            formattedMessage = "Generating voice narration";
        }
        else if (message.Contains("visual", StringComparison.OrdinalIgnoreCase) ||
                 message.Contains("image", StringComparison.OrdinalIgnoreCase))
        {
            stage = "Visuals";
            percent = 50;
            formattedMessage = "Generating visual assets";
        }
        else if (message.Contains("render", StringComparison.OrdinalIgnoreCase) ||
                 message.Contains("composing video", StringComparison.OrdinalIgnoreCase))
        {
            stage = "Rendering";
            percent = 70;
            formattedMessage = "Rendering final video";
        }
        else if (message.Contains("postprocess", StringComparison.OrdinalIgnoreCase))
        {
            stage = "Postprocessing";
            percent = 90;
            formattedMessage = "Finalizing video";
        }

        return (stage, percent, formattedMessage);
    }
}
