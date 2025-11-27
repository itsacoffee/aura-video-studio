using Aura.Core.Artifacts;
using Aura.Core.Models;
using Aura.Core.Orchestrator;
using Aura.Core.Services;
using Aura.Core.Services.Export;
using Aura.Api.Models.ApiModels.V1;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Text;
using System.Text.Json;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for managing video generation jobs
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly JobRunner _jobRunner;
    private readonly ArtifactManager _artifactManager;
    private readonly ProgressAggregatorService? _progressAggregator;
    private readonly CancellationOrchestrator? _cancellationOrchestrator;
    private readonly IExportJobService? _exportJobService;

    public JobsController(
        JobRunner jobRunner,
        ArtifactManager artifactManager,
        ProgressAggregatorService? progressAggregator = null,
        CancellationOrchestrator? cancellationOrchestrator = null,
        IExportJobService? exportJobService = null)
    {
        _jobRunner = jobRunner;
        _artifactManager = artifactManager;
        _progressAggregator = progressAggregator;
        _cancellationOrchestrator = cancellationOrchestrator;
        _exportJobService = exportJobService;
    }

    /// <summary>
    /// Create and start a new video generation job
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateJob(
        [FromBody] CreateJobRequest request,
        CancellationToken ct = default)
    {
        try
        {
            // Input validation with correlation ID
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] POST /api/jobs endpoint called", correlationId);

            ArgumentNullException.ThrowIfNull(request, nameof(request));
            ArgumentNullException.ThrowIfNull(request.Brief, nameof(request.Brief));
            ArgumentNullException.ThrowIfNull(request.PlanSpec, nameof(request.PlanSpec));
            ArgumentNullException.ThrowIfNull(request.VoiceSpec, nameof(request.VoiceSpec));
            ArgumentNullException.ThrowIfNull(request.RenderSpec, nameof(request.RenderSpec));

            // Validate Brief has non-empty Topic
            if (string.IsNullOrWhiteSpace(request.Brief.Topic))
            {
                Log.Warning("[{CorrelationId}] Job creation rejected: Topic is required", correlationId);
                return BadRequest(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E400",
                    title = "Invalid Request",
                    status = 400,
                    detail = "Brief.Topic cannot be null or empty",
                    correlationId,
                    field = "Brief.Topic",
                    guidance = "Please provide a valid video topic (e.g., 'Cooking pasta', 'Space exploration')"
                });
            }

            // Validate PlanSpec has positive TargetDuration
            if (request.PlanSpec.TargetDuration <= TimeSpan.Zero)
            {
                Log.Warning("[{CorrelationId}] Job creation rejected: Invalid TargetDuration {Duration}",
                    correlationId, request.PlanSpec.TargetDuration);
                return BadRequest(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E400",
                    title = "Invalid Request",
                    status = 400,
                    detail = $"PlanSpec.TargetDuration must be positive (received: {request.PlanSpec.TargetDuration})",
                    correlationId,
                    field = "PlanSpec.TargetDuration",
                    guidance = "Specify a target duration between 5 seconds and 10 minutes"
                });
            }

            Log.Information("[{CorrelationId}] Creating new job for topic: {Topic}", correlationId, request.Brief.Topic);

            RagConfiguration? ragConfig = null;
            if (request.Brief.RagConfiguration != null)
            {
                ragConfig = new RagConfiguration(
                    Enabled: request.Brief.RagConfiguration.Enabled,
                    TopK: request.Brief.RagConfiguration.TopK,
                    MinimumScore: request.Brief.RagConfiguration.MinimumScore,
                    MaxContextTokens: request.Brief.RagConfiguration.MaxContextTokens,
                    IncludeCitations: request.Brief.RagConfiguration.IncludeCitations,
                    TightenClaims: request.Brief.RagConfiguration.TightenClaims
                );
            }

            LlmParameters? llmParams = null;
            if (request.Brief.LlmParameters != null)
            {
                llmParams = new LlmParameters(
                    Temperature: request.Brief.LlmParameters.Temperature,
                    TopP: request.Brief.LlmParameters.TopP,
                    TopK: request.Brief.LlmParameters.TopK,
                    MaxTokens: request.Brief.LlmParameters.MaxTokens,
                    FrequencyPenalty: request.Brief.LlmParameters.FrequencyPenalty,
                    PresencePenalty: request.Brief.LlmParameters.PresencePenalty,
                    StopSequences: request.Brief.LlmParameters.StopSequences,
                    ModelOverride: request.Brief.LlmParameters.ModelOverride
                );
            }

            PromptModifiers? promptModifiers = null;
            if (request.Brief.PromptModifiers != null)
            {
                promptModifiers = new PromptModifiers(
                    AdditionalInstructions: request.Brief.PromptModifiers.AdditionalInstructions,
                    ExampleStyle: request.Brief.PromptModifiers.ExampleStyle,
                    EnableChainOfThought: request.Brief.PromptModifiers.EnableChainOfThought,
                    PromptVersion: request.Brief.PromptModifiers.PromptVersion
                );
            }

            var brief = new Brief(
                Topic: request.Brief.Topic,
                Audience: request.Brief.Audience,
                Goal: request.Brief.Goal,
                Tone: request.Brief.Tone,
                Language: request.Brief.Language,
                Aspect: ParseAspect(request.Brief.Aspect),
                PromptModifiers: promptModifiers,
                RagConfiguration: ragConfig,
                LlmParameters: llmParams
            );

            // Parse enums consistently - convert to string first, then parse for robustness
            // This ensures consistent handling even if ASP.NET Core deserializer converts strings to enums
            var pacing = ParsePacing(request.PlanSpec.Pacing.ToString());
            var density = ParseDensity(request.PlanSpec.Density.ToString());
            var pause = ParsePauseStyle(request.VoiceSpec.Pause.ToString());

            var planSpec = new PlanSpec(
                TargetDuration: request.PlanSpec.TargetDuration,
                Pacing: pacing,
                Density: density,
                Style: request.PlanSpec.Style
            );

            var voiceSpec = new VoiceSpec(
                VoiceName: request.VoiceSpec.VoiceName,
                Rate: request.VoiceSpec.Rate,
                Pitch: request.VoiceSpec.Pitch,
                Pause: pause
            );

            var renderSpec = new RenderSpec(
                Res: request.RenderSpec.Res,
                Container: request.RenderSpec.Container,
                VideoBitrateK: request.RenderSpec.VideoBitrateK,
                AudioBitrateK: request.RenderSpec.AudioBitrateK,
                Fps: request.RenderSpec.Fps,
                Codec: request.RenderSpec.Codec,
                QualityLevel: request.RenderSpec.QualityLevel,
                EnableSceneCut: request.RenderSpec.EnableSceneCut
            );

            var job = await _jobRunner.CreateAndStartJobAsync(
                brief,
                planSpec,
                voiceSpec,
                renderSpec,
                correlationId,
                isQuickDemo: false,
                ct
            ).ConfigureAwait(false);

            Log.Information("[{CorrelationId}] Job created successfully with ID: {JobId}, Status: {Status}", correlationId, job.Id, job.Status);

            return Ok(new { jobId = job.Id, status = job.Status, stage = job.Stage, correlationId });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error creating job", correlationId);

            // Return structured ProblemDetails with correlation ID
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E203",
                title = "Job Creation Failed",
                status = 500,
                detail = $"Failed to create job: {ex.Message}",
                correlationId,
                traceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get job status and progress
    /// </summary>
    [HttpGet("{jobId}")]
    [ProducesResponseType(typeof(JobStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJob(string jobId)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            Log.Debug(
                "[{CorrelationId}] GET /api/jobs/{JobId} - Fetching job status",
                correlationId, jobId);

            // Check JobRunner first
            var job = _jobRunner.GetJob(jobId);

            if (job == null)
            {
                // Check export job service as fallback
                if (_exportJobService != null)
                {
                    var exportJob = await _exportJobService.GetJobAsync(jobId).ConfigureAwait(false);

                    if (exportJob != null)
                    {
                        // Return export job status
                        return Ok(new JobStatusResponse
                        {
                            Id = exportJob.Id,
                            Status = MapJobStatus(exportJob.Status),
                            Percent = exportJob.Progress,
                            Stage = exportJob.Stage,
                            ProgressMessage = exportJob.Message,
                            CreatedAt = exportJob.CreatedAt,
                            StartedAt = exportJob.StartedAt,
                            CompletedAt = exportJob.CompletedAt,
                            OutputPath = exportJob.OutputPath,
                            ErrorMessage = exportJob.ErrorMessage,
                            CorrelationId = correlationId
                        });
                    }
                }

                Log.Warning(
                    "[{CorrelationId}] Job {JobId} not found in JobRunner or ExportJobService",
                    correlationId, jobId);

                return NotFound(new ProblemDetails
                {
                    Type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E404",
                    Title = "Job Not Found",
                    Status = 404,
                    Detail = $"Job with ID '{jobId}' was not found. It may have expired or never existed.",
                    Extensions = {
                        ["correlationId"] = correlationId,
                        ["jobId"] = jobId
                    }
                });
            }

            // Return generation job status
            var response = new JobStatusResponse
            {
                Id = job.Id,
                Status = MapJobStatus(job.Status.ToString()),
                Percent = job.Percent,
                Stage = job.Stage,
                ProgressMessage = job.Logs.LastOrDefault(),
                CreatedAt = job.CreatedUtc,
                StartedAt = job.StartedUtc,
                CompletedAt = job.CompletedUtc,
                OutputPath = job.OutputPath,
                ErrorMessage = job.ErrorMessage,
                Artifacts = job.Artifacts?.Select(a => new ArtifactDto
                {
                    Path = a.Path,
                    FilePath = a.Path,
                    Type = a.Type,
                    SizeBytes = a.SizeBytes
                }).ToList(),
                CorrelationId = correlationId
            };

            Log.Debug(
                "[{CorrelationId}] Job {JobId} status: {Status}, progress: {Progress}%",
                correlationId, jobId, response.Status, response.Percent);

            return Ok(response);
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "[{CorrelationId}] Error fetching job {JobId}",
                correlationId, jobId);

            return StatusCode(500, new ProblemDetails
            {
                Type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                Title = "Internal Server Error",
                Status = 500,
                Detail = "An error occurred while fetching job status.",
                Extensions = {
                    ["correlationId"] = correlationId,
                    ["error"] = ex.Message
                }
            });
        }
    }

    /// <summary>
    /// Normalize status strings to lowercase for frontend consistency
    /// </summary>
    private static string MapJobStatus(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "queued" => "queued",
            "running" => "running",
            "processing" => "running",
            "done" => "completed",
            "completed" => "completed",
            "succeeded" => "completed",
            "failed" => "failed",
            "cancelled" => "cancelled",
            "canceled" => "cancelled",
            _ => status.ToLowerInvariant()
        };
    }

    /// <summary>
    /// List all recent jobs
    /// </summary>
    [HttpGet]
    public IActionResult ListJobs([FromQuery] int limit = 50)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;

            var jobs = _jobRunner.ListJobs(limit);
            return Ok(new { jobs = jobs, count = jobs.Count, correlationId });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error listing jobs", correlationId);

            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Error Listing Jobs",
                status = 500,
                detail = $"Failed to list jobs: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Get detailed failure information for a failed job
    /// </summary>
    [HttpGet("{jobId}/failure-details")]
    public IActionResult GetJobFailureDetails(string jobId)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;

            var job = _jobRunner.GetJob(jobId);
            if (job == null)
            {
                Log.Warning("[{CorrelationId}] Job not found: {JobId}", correlationId, jobId);
                return NotFound(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E404",
                    title = "Job Not Found",
                    status = 404,
                    detail = $"Job {jobId} not found",
                    correlationId
                });
            }

            if (job.Status != JobStatus.Failed)
            {
                return BadRequest(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E400",
                    title = "Job Not Failed",
                    status = 400,
                    detail = $"Job {jobId} has not failed (status: {job.Status})",
                    correlationId
                });
            }

            if (job.FailureDetails == null)
            {
                // Return basic failure info if detailed info not available
                return Ok(new
                {
                    stage = job.Stage,
                    message = job.ErrorMessage ?? "Job failed",
                    correlationId = job.CorrelationId ?? correlationId,
                    suggestedActions = new[] { "Check logs for details", "Retry the operation" },
                    failedAt = job.FinishedAt ?? DateTime.UtcNow
                });
            }

            return Ok(job.FailureDetails);
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error retrieving failure details for job {JobId}", correlationId, jobId);

            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Error Retrieving Failure Details",
                status = 500,
                detail = $"Failed to retrieve failure details: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Get latest artifacts from recent jobs
    /// Does NOT attempt to resolve providers - simply returns persisted artifacts
    /// </summary>
    [HttpGet("recent-artifacts")]
    public IActionResult GetRecentArtifacts([FromQuery] int limit = 5)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;

            // Get jobs from storage - does not trigger provider resolution
            var jobs = _jobRunner.ListJobs(50); // Get more jobs to find ones with artifacts
            var artifacts = jobs
                .Where(j => j.Status == JobStatus.Done && j.Artifacts.Count > 0)
                .OrderByDescending(j => j.FinishedAt ?? j.StartedAt)
                .Take(limit)
                .Select(j => new
                {
                    jobId = j.Id,
                    correlationId = j.CorrelationId,
                    stage = j.Stage,
                    finishedAt = j.FinishedAt,
                    artifacts = j.Artifacts
                })
                .ToList();

            return Ok(new { artifacts = artifacts, count = artifacts.Count, correlationId });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error getting recent artifacts", correlationId);

            // Return best-effort empty list rather than throwing
            // This ensures the endpoint never crashes the API
            return Ok(new
            {
                artifacts = new List<object>(),
                count = 0,
                correlationId,
                warning = "Failed to retrieve artifacts, returning empty list",
                detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Retry a failed job with exponential backoff
    /// </summary>
    [HttpPost("{jobId}/retry")]
    public async Task<IActionResult> RetryJob(
        string jobId,
        [FromQuery] string? strategy = null,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Retry request for job {JobId} with strategy {Strategy}",
                correlationId, jobId, strategy ?? "default");

            // Get the job
            var job = _jobRunner.GetJob(jobId);
            if (job == null)
            {
                return NotFound(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E404",
                    title = "Job Not Found",
                    status = 404,
                    detail = $"Job {jobId} not found",
                    correlationId
                });
            }

            // Verify job has failed
            if (job.Status != JobStatus.Failed)
            {
                return BadRequest(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E400",
                    title = "Job Not Failed",
                    status = 400,
                    detail = $"Job {jobId} has not failed (status: {job.Status})",
                    currentStatus = job.Status,
                    correlationId
                });
            }

            // Attempt to retry the job
            var retried = await _jobRunner.RetryJobAsync(jobId, ct).ConfigureAwait(false);

            if (retried)
            {
                Log.Information("[{CorrelationId}] Successfully initiated retry for job {JobId}",
                    correlationId, jobId);
                return Accepted(new
                {
                    jobId,
                    message = "Job retry initiated successfully",
                    strategy = strategy ?? "automatic",
                    correlationId
                });
            }
            else
            {
                Log.Warning("[{CorrelationId}] Failed to retry job {JobId} - max retries reached or backoff active",
                    correlationId, jobId);
                return BadRequest(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E400",
                    title = "Retry Not Allowed",
                    status = 400,
                    detail = "Job cannot be retried at this time. Maximum retry count may have been reached or backoff period is active.",
                    currentStatus = job.Status,
                    suggestedActions = new[]
                    {
                        "Wait a few minutes before retrying again",
                        "Create a new job with adjusted settings",
                        "Check the failure details for specific remediation steps"
                    },
                    correlationId
                });
            }
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error retrying job {JobId}", correlationId, jobId);

            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Retry Failed",
                status = 500,
                detail = $"Failed to retry job: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Stream Server-Sent Events for job progress updates
    /// Supports reconnection via Last-Event-ID header or query parameter
    /// </summary>
    [HttpGet("{jobId}/events")]
    public async Task GetJobEvents(string jobId, [FromQuery] string? lastEventId = null, CancellationToken ct = default)
    {
        // Use RequestAborted to detect client disconnections properly
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, HttpContext.RequestAborted);
        var cancellationToken = linkedCts.Token;

        var correlationId = HttpContext.TraceIdentifier;

        // Check for Last-Event-ID header or query parameter for reconnection support
        // Query parameter is used as fallback since EventSource doesn't support custom headers
        var reconnectEventId = lastEventId ?? Request.Headers["Last-Event-ID"].FirstOrDefault();
        var isReconnect = !string.IsNullOrEmpty(reconnectEventId);

        Log.Information("[{CorrelationId}] SSE stream requested for job {JobId}, reconnect={IsReconnect}, lastEventId={LastEventId}",
            correlationId, jobId, isReconnect, reconnectEventId ?? "none");

        // Set headers for SSE
        Response.Headers.Add("Content-Type", "text/event-stream");
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");
        Response.Headers.Add("X-Accel-Buffering", "no"); // Disable nginx buffering

        try
        {
            var job = _jobRunner.GetJob(jobId);
            if (job == null)
            {
                await SendSseEventWithId("error", new { message = "Job not found", jobId, correlationId }, GenerateEventId(), cancellationToken).ConfigureAwait(false);
                return;
            }

            // Send initial job status with artifacts
            await SendSseEventWithId("job-status", new {
                status = job.Status.ToString(),
                stage = job.Stage,
                percent = job.Percent,
                correlationId,
                isReconnect
            }, GenerateEventId(), cancellationToken).ConfigureAwait(false);

            // Track last sent values and last ping time
            var lastStatus = job.Status;
            var lastStage = job.Stage;
            var lastPercent = job.Percent;
            var lastProgressMessage = "";
            var lastLogCount = job.Logs.Count;
            var lastPingTime = DateTime.UtcNow;
            var pingIntervalSeconds = 5; // 5-second heartbeat as per requirements
            var pollIntervalMs = 500; // Poll every 500ms for responsiveness
            var eventIdCounter = 0;

            while (!cancellationToken.IsCancellationRequested && job.Status != JobStatus.Done && job.Status != JobStatus.Failed && job.Status != JobStatus.Canceled)
            {
                await Task.Delay(pollIntervalMs, cancellationToken).ConfigureAwait(false);

                job = _jobRunner.GetJob(jobId);
                if (job == null) break;

                // Send keep-alive heartbeat every 5 seconds with structured data
                if ((DateTime.UtcNow - lastPingTime).TotalSeconds >= pingIntervalSeconds)
                {
                    var heartbeat = new HeartbeatEventDto(
                        Timestamp: DateTime.UtcNow,
                        Status: "alive"
                    );
                    await SendSseEventWithId("heartbeat", heartbeat, GenerateEventId(++eventIdCounter), cancellationToken).ConfigureAwait(false);
                    lastPingTime = DateTime.UtcNow;
                }

                // Send status change events
                if (job.Status != lastStatus)
                {
                    await SendSseEventWithId("job-status", new {
                        status = job.Status.ToString(),
                        stage = job.Stage,
                        percent = job.Percent,
                        correlationId
                    }, GenerateEventId(++eventIdCounter), cancellationToken).ConfigureAwait(false);
                    lastStatus = job.Status;
                }

                // Send stage change events (phase transitions)
                if (job.Stage != lastStage)
                {
                    var normalizedPhase = NormalizePhase(MapStageToPhase(job.Stage));
                    await SendSseEventWithId("step-status", new {
                        step = job.Stage,
                        status = "started",
                        phase = normalizedPhase,
                        correlationId
                    }, GenerateEventId(++eventIdCounter), cancellationToken).ConfigureAwait(false);
                    lastStage = job.Stage;
                }

                // Send progress updates with unified ProgressEventDto
                if (job.Percent != lastPercent)
                {
                    var latestLog = job.Logs.LastOrDefault() ?? "";

                    // Get aggregated progress if available
                    var aggregatedProgress = _progressAggregator?.GetProgress(jobId);

                    var normalizedPhase = NormalizePhase(MapStageToPhase(job.Stage));
                    double? elapsedSeconds = null;
                    if (job.StartedUtc.HasValue)
                    {
                        elapsedSeconds = Math.Max(0, (DateTime.UtcNow - job.StartedUtc.Value).TotalSeconds);
                    }
                    else if (job.StartedAt != default)
                    {
                        elapsedSeconds = Math.Max(0, (DateTime.UtcNow - job.StartedAt).TotalSeconds);
                    }

                    var etaSpan = aggregatedProgress?.Eta ?? job.Eta;

                    var progressEvent = new ProgressEventDto(
                        JobId: jobId,
                        Stage: job.Stage,
                        Percent: job.Percent,
                        EtaSeconds: etaSpan.HasValue ? (int)etaSpan.Value.TotalSeconds : null,
                        Message: aggregatedProgress?.Message ?? latestLog,
                        Warnings: aggregatedProgress?.Warnings ?? new List<string>(),
                        CorrelationId: correlationId,
                        SubstageDetail: aggregatedProgress?.SubstageDetail ?? job.CurrentProgress?.SubstageDetail,
                        CurrentItem: aggregatedProgress?.CurrentItem ?? job.CurrentProgress?.CurrentItem,
                        TotalItems: aggregatedProgress?.TotalItems ?? job.CurrentProgress?.TotalItems,
                        Timestamp: DateTime.UtcNow,
                        Phase: normalizedPhase,
                        ElapsedSeconds: elapsedSeconds,
                        EstimatedRemainingSeconds: etaSpan?.TotalSeconds
                    );

                    await SendSseEventWithId("step-progress", progressEvent, GenerateEventId(++eventIdCounter), cancellationToken).ConfigureAwait(false);
                    lastPercent = job.Percent;
                    lastProgressMessage = latestLog;
                }

                // Emit log entries incrementally for UI console
                if (job.Logs.Count > lastLogCount)
                {
                    for (int i = lastLogCount; i < job.Logs.Count; i++)
                    {
                        var logEntry = job.Logs[i];
                        var severity = DetermineLogSeverity(logEntry);

                        var logEvent = new JobLogEventDto(
                            JobId: jobId,
                            Message: logEntry,
                            Stage: job.Stage,
                            Severity: severity,
                            Timestamp: DateTime.UtcNow,
                            CorrelationId: correlationId);

                        await SendSseEventWithId("job-log", logEvent, GenerateEventId(++eventIdCounter), cancellationToken).ConfigureAwait(false);

                        if (severity == "warning")
                        {
                            await SendSseEventWithId("warning", new
                            {
                                message = logEntry,
                                step = job.Stage,
                                correlationId
                            }, GenerateEventId(++eventIdCounter), cancellationToken).ConfigureAwait(false);
                        }
                    }

                    lastLogCount = job.Logs.Count;
                }

                await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            // Send final event
            if (job?.Status == JobStatus.Done || job?.Status == JobStatus.Succeeded)
            {
                var videoArtifact = job.Artifacts.FirstOrDefault(a => a.Type == "video" || a.Type == "video/mp4");
                var subtitleArtifact = job.Artifacts.FirstOrDefault(a => a.Type == "subtitle" || a.Type == "text/srt");

                await SendSseEventWithId("job-completed", new
                {
                    status = "Succeeded",
                    jobId = job.Id,
                    artifacts = job.Artifacts.Select(a => new {
                        name = a.Name,
                        path = a.Path,
                        type = a.Type,
                        sizeBytes = a.SizeBytes
                    }).ToArray(),
                    output = new {
                        videoPath = videoArtifact?.Path ?? "",
                        subtitlePath = subtitleArtifact?.Path ?? "",
                        sizeBytes = videoArtifact?.SizeBytes ?? 0
                    },
                    correlationId
                }, GenerateEventId(++eventIdCounter), cancellationToken).ConfigureAwait(false);
            }
            else if (job?.Status == JobStatus.Failed)
            {
                var errors = job.Errors.Any()
                    ? job.Errors.ToArray()
                    : new[] { new JobStepError { Code = "UnknownError", Message = job.ErrorMessage ?? "Job failed", Remediation = "Check logs for details" } };

                await SendSseEventWithId("job-failed", new
                {
                    status = "Failed",
                    jobId = job.Id,
                    stage = job.Stage,
                    errors,
                    errorMessage = job.ErrorMessage,
                    logs = job.Logs.TakeLast(10).ToArray(),
                    correlationId
                }, GenerateEventId(++eventIdCounter), cancellationToken).ConfigureAwait(false);
            }
            else if (job?.Status == JobStatus.Canceled)
            {
                await SendSseEventWithId("job-cancelled", new
                {
                    status = "Cancelled",
                    jobId = job.Id,
                    stage = job.Stage,
                    message = "Job was cancelled by user",
                    correlationId
                }, GenerateEventId(++eventIdCounter), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected, this is normal
            Log.Debug("[{CorrelationId}] SSE stream canceled for job {JobId}", correlationId, jobId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{CorrelationId}] Error streaming events for job {JobId}", correlationId, jobId);
            await SendSseEvent("error", new { message = ex.Message, correlationId }).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Cancel a running job with orchestrated provider cancellation
    /// </summary>
    [HttpPost("{jobId}/cancel")]
    public async Task<IActionResult> CancelJob(string jobId, CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Cancel request for job {JobId}", correlationId, jobId);

            var job = _jobRunner.GetJob(jobId);
            if (job == null)
            {
                return NotFound(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E404",
                    title = "Job Not Found",
                    status = 404,
                    detail = $"Job {jobId} not found",
                    correlationId
                });
            }

            // Check if job is in a cancellable state
            if (job.Status != JobStatus.Running && job.Status != JobStatus.Queued)
            {
                return BadRequest(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E400",
                    title = "Job Not Cancellable",
                    status = 400,
                    detail = $"Job is in {job.Status} status and cannot be cancelled",
                    currentStatus = job.Status,
                    correlationId
                });
            }

            // Attempt to cancel the job with orchestration
            bool cancelled = _jobRunner.CancelJob(jobId);

            // If cancellation orchestrator is available, use it for detailed provider cancellation
            CancellationResult? orchestratorResult = null;
            if (_cancellationOrchestrator != null && cancelled)
            {
                try
                {
                    orchestratorResult = await _cancellationOrchestrator.CancelJobAsync(jobId, ct).ConfigureAwait(false);

                    // Convert to DTOs for API response
                    var providerStatuses = orchestratorResult.ProviderStatuses
                        .Select(ps => new ProviderCancellationStatusDto(
                            ProviderName: ps.ProviderName,
                            ProviderType: ps.ProviderType,
                            SupportsCancellation: ps.SupportsCancellation,
                            Status: ps.Status,
                            Warning: ps.Warning
                        ))
                        .ToList();

                    Log.Information(
                        "[{CorrelationId}] Job {JobId} cancellation orchestrated: {SuccessCount}/{TotalCount} providers",
                        correlationId, jobId,
                        providerStatuses.Count(s => s.Status == "Cancelled"),
                        providerStatuses.Count);

                    return Accepted(new
                    {
                        jobId,
                        message = orchestratorResult.Message,
                        currentStatus = job.Status,
                        cleanupScheduled = true,
                        providerStatuses,
                        warnings = orchestratorResult.Warnings,
                        correlationId
                    });
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "[{CorrelationId}] Error during cancellation orchestration for job {JobId}, cancellation still triggered",
                        correlationId, jobId);
                }
            }

            if (cancelled)
            {
                Log.Information("[{CorrelationId}] Successfully cancelled job {JobId}, cleanup will be performed", correlationId, jobId);
                return Accepted(new
                {
                    jobId,
                    message = "Job cancellation triggered successfully. Cleanup of temporary files and proxies will be performed.",
                    currentStatus = job.Status,
                    cleanupScheduled = true,
                    correlationId
                });
            }
            else
            {
                Log.Warning("[{CorrelationId}] Failed to cancel job {JobId}", correlationId, jobId);
                return StatusCode(500, new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                    title = "Cancellation Failed",
                    status = 500,
                    detail = "Job could not be cancelled. It may have already completed or been cancelled.",
                    currentStatus = job.Status,
                    correlationId
                });
            }
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error canceling job {JobId}", correlationId, jobId);

            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Cancel Failed",
                status = 500,
                detail = $"Failed to cancel job: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Pause a running job
    /// </summary>
    [HttpPost("{jobId}/pause")]
    public IActionResult PauseJob(string jobId)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Pause request for job {JobId}", correlationId, jobId);

            var job = _jobRunner.GetJob(jobId);
            if (job == null)
            {
                return NotFound(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E404",
                    title = "Job Not Found",
                    status = 404,
                    detail = $"Job {jobId} not found",
                    correlationId
                });
            }

            // Check if job is in a pausable state
            if (job.Status != JobStatus.Running)
            {
                return BadRequest(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E400",
                    title = "Job Not Pausable",
                    status = 400,
                    detail = $"Job is in {job.Status} status and cannot be paused",
                    currentStatus = job.Status.ToString(),
                    correlationId
                });
            }

            // Attempt to pause the job
            var paused = _jobRunner.PauseJob(jobId);

            if (paused)
            {
                Log.Information("[{CorrelationId}] Successfully paused job {JobId}", correlationId, jobId);
                return Ok(new { success = true, jobId, message = "Job paused successfully", correlationId });
            }
            else
            {
                Log.Warning("[{CorrelationId}] Failed to pause job {JobId}", correlationId, jobId);
                return StatusCode(500, new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                    title = "Pause Failed",
                    status = 500,
                    detail = "Job could not be paused.",
                    currentStatus = job.Status.ToString(),
                    correlationId
                });
            }
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error pausing job {JobId}", correlationId, jobId);

            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Pause Failed",
                status = 500,
                detail = $"Failed to pause job: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Resume a paused job
    /// </summary>
    [HttpPost("{jobId}/resume")]
    public IActionResult ResumeJob(string jobId)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Resume request for job {JobId}", correlationId, jobId);

            var job = _jobRunner.GetJob(jobId);
            if (job == null)
            {
                return NotFound(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E404",
                    title = "Job Not Found",
                    status = 404,
                    detail = $"Job {jobId} not found",
                    correlationId
                });
            }

            // Check if job is in a resumable state
            if (job.Status != JobStatus.Paused)
            {
                return BadRequest(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E400",
                    title = "Job Not Resumable",
                    status = 400,
                    detail = $"Job is in {job.Status} status and cannot be resumed",
                    currentStatus = job.Status.ToString(),
                    correlationId
                });
            }

            // Attempt to resume the job
            var resumed = _jobRunner.ResumeJob(jobId);

            if (resumed)
            {
                Log.Information("[{CorrelationId}] Successfully resumed job {JobId}", correlationId, jobId);
                return Ok(new { success = true, jobId, message = "Job resumed successfully", correlationId });
            }
            else
            {
                Log.Warning("[{CorrelationId}] Failed to resume job {JobId}", correlationId, jobId);
                return StatusCode(500, new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                    title = "Resume Failed",
                    status = 500,
                    detail = "Job could not be resumed.",
                    currentStatus = job.Status.ToString(),
                    correlationId
                });
            }
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error resuming job {JobId}", correlationId, jobId);

            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Resume Failed",
                status = 500,
                detail = $"Failed to resume job: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Get logs for a specific job
    /// </summary>
    [HttpGet("{jobId}/logs")]
    public IActionResult GetJobLogs(string jobId)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Logs request for job {JobId}", correlationId, jobId);

            var job = _jobRunner.GetJob(jobId);
            if (job == null)
            {
                return NotFound(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E404",
                    title = "Job Not Found",
                    status = 404,
                    detail = $"Job {jobId} not found",
                    correlationId
                });
            }

            // Return the job logs as plain text
            var logs = job.Logs.Count > 0
                ? string.Join("\n", job.Logs)
                : "No logs available for this job";

            return Content(logs, "text/plain");
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error retrieving logs for job {JobId}", correlationId, jobId);

            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Logs Retrieval Failed",
                status = 500,
                detail = $"Failed to retrieve logs: {ex.Message}",
                correlationId
            });
        }
    }

    private async Task SendSseEvent(string eventType, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var message = $"event: {eventType}\ndata: {json}\n\n";
        var bytes = Encoding.UTF8.GetBytes(message);
        await Response.Body.WriteAsync(bytes).ConfigureAwait(false);
    }

    private async Task SendSseEventWithId(string eventType, object data, string eventId, CancellationToken ct = default)
    {
        try
        {
            if (ct.IsCancellationRequested)
            {
                return;
            }

            var json = JsonSerializer.Serialize(data);
            var message = $"id: {eventId}\nevent: {eventType}\ndata: {json}\n\n";
            var bytes = Encoding.UTF8.GetBytes(message);
            await Response.Body.WriteAsync(bytes, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected, this is normal
            Log.Debug("SSE event send cancelled (client disconnected)");
        }
    }

    private async Task SendSseComment(string comment)
    {
        var message = $": {comment}\n\n";
        var bytes = Encoding.UTF8.GetBytes(message);
        await Response.Body.WriteAsync(bytes).ConfigureAwait(false);
    }

    private static string GenerateEventId(int counter = 0)
    {
        // Generate event ID with timestamp and counter for uniqueness
        return $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{counter}";
    }

    private static string MapStageToPhase(string stage)
    {
        return stage.ToLowerInvariant() switch
        {
            "initialization" or "queued" => "plan",
            "script" or "planning" or "brief" => "plan",
            "tts" or "audio" or "voice" => "tts",
            "visuals" or "images" or "assets" => "visuals",
            "composition" or "timeline" or "compose" => "compose",
            "rendering" or "render" or "encode" => "render",
            "complete" or "done" => "complete",
            _ => "processing"
        };
    }

    private static string NormalizePhase(string phase)
    {
        return phase switch
        {
            "processing" => "plan",
            // Keep "complete" as "complete" - a completed job should remain in the complete phase
            _ => phase
        };
    }

    private static string DetermineLogSeverity(string logEntry)
    {
        if (logEntry.Contains("error", StringComparison.OrdinalIgnoreCase))
        {
            return "error";
        }

        if (logEntry.Contains("warning", StringComparison.OrdinalIgnoreCase) ||
            logEntry.Contains("warn", StringComparison.OrdinalIgnoreCase))
        {
            return "warning";
        }

        return "info";
    }

    /// <summary>
    /// Get job progress information for status bar updates
    /// </summary>
    [HttpGet("{jobId}/progress")]
    public IActionResult GetJobProgress(string jobId)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;

            var job = _jobRunner.GetJob(jobId);
            if (job == null)
            {
                Log.Warning("[{CorrelationId}] Job not found: {JobId}", correlationId, jobId);
                return NotFound(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E404",
                    title = "Job Not Found",
                    status = 404,
                    detail = $"Job {jobId} not found",
                    correlationId
                });
            }

            // Map job status to string for UI
            var statusString = job.Status switch
            {
                JobStatus.Running => "running",
                JobStatus.Done or JobStatus.Succeeded => "completed",
                JobStatus.Failed => "failed",
                _ => "idle"
            };

            return Ok(new
            {
                jobId = job.Id,
                status = statusString,
                progress = job.Percent,
                currentStage = job.Stage,
                startedAt = job.StartedAt,
                completedAt = job.FinishedAt,
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error retrieving job progress {JobId}", correlationId, jobId);

            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Error Retrieving Job Progress",
                status = 500,
                detail = $"Failed to retrieve job progress: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Stream Server-Sent Events for real-time job progress updates.
    /// This endpoint provides instant progress updates for export jobs.
    /// </summary>
    [HttpGet("{jobId}/progress/stream")]
    public async Task GetJobProgressStream(string jobId, CancellationToken ct = default)
    {
        // Use RequestAborted to detect client disconnections properly
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, HttpContext.RequestAborted);
        var cancellationToken = linkedCts.Token;
        var correlationId = HttpContext.TraceIdentifier;

        Log.Information("[{CorrelationId}] SSE progress stream requested for job {JobId}", correlationId, jobId);

        // Set headers for SSE
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no"); // Disable nginx buffering

        try
        {
            // Send immediate connection acknowledgment so client knows the connection is established
            var ackData = JsonSerializer.Serialize(new
            {
                percent = 0,
                stage = "Connecting",
                status = "connecting",
                message = "SSE connection established, waiting for job...",
                jobId,
                correlationId
            });
            await Response.WriteAsync($"event: job-progress\n", cancellationToken).ConfigureAwait(false);
            await Response.WriteAsync($"data: {ackData}\n\n", cancellationToken).ConfigureAwait(false);
            await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);

            Log.Debug("[{CorrelationId}] SSE connection acknowledgment sent for job {JobId}", correlationId, jobId);

            // First, check JobRunner for video generation jobs (primary use case)
            // Wait for job to appear (it may take a moment after creation)
            Job? job = null;
            var waitAttempts = 0;
            const int waitIntervalMs = 250;
            const int maxWaitTimeMs = 20_000; // 20 seconds
            const int maxWaitAttempts = maxWaitTimeMs / waitIntervalMs;
            var lastWaitingMessageTime = DateTime.UtcNow;

            while (job == null && waitAttempts < maxWaitAttempts && !cancellationToken.IsCancellationRequested)
            {
                job = _jobRunner.GetJob(jobId);
                if (job == null)
                {
                    waitAttempts++;

                    // Send waiting message every 2 seconds while waiting for job to appear
                    if ((DateTime.UtcNow - lastWaitingMessageTime).TotalSeconds >= 2)
                    {
                        var waitingData = JsonSerializer.Serialize(new
                        {
                            percent = 0,
                            stage = "Initializing",
                            status = "waiting",
                            message = $"Waiting for job to start... ({waitAttempts * 250 / 1000}s)",
                            jobId,
                            correlationId
                        });
                        await Response.WriteAsync($"event: job-progress\n", cancellationToken).ConfigureAwait(false);
                        await Response.WriteAsync($"data: {waitingData}\n\n", cancellationToken).ConfigureAwait(false);
                        await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
                        lastWaitingMessageTime = DateTime.UtcNow;
                    }

                    await Task.Delay(waitIntervalMs, cancellationToken).ConfigureAwait(false);
                }
            }

            if (job != null)
            {
                Log.Information("[{CorrelationId}] Streaming video generation job {JobId} via SSE (found after {WaitAttempts} attempts)", 
                    correlationId, jobId, waitAttempts);

                // Send initial state with normalized status
                var initialData = JsonSerializer.Serialize(new
                {
                    percent = job.Percent,
                    stage = job.Stage,
                    status = MapJobStatus(job.Status.ToString()),
                    message = job.Logs.LastOrDefault() ?? "Starting video generation...",
                    outputPath = job.OutputPath
                });
                await Response.WriteAsync($"event: job-progress\n", cancellationToken).ConfigureAwait(false);
                await Response.WriteAsync($"data: {initialData}\n\n", cancellationToken).ConfigureAwait(false);
                await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);

                // Poll for updates (JobRunner doesn't have subscription support)
                var lastPercent = job.Percent;
                var lastStatus = job.Status;
                var lastStage = job.Stage;
                var pollIntervalMs = 500; // Poll every 500ms for responsiveness
                var heartbeatCounter = 0;

                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(pollIntervalMs, cancellationToken).ConfigureAwait(false);
                    heartbeatCounter++;

                    job = _jobRunner.GetJob(jobId);
                    if (job == null) break;

                    // Send updates when something changed OR every 10 polls (5 seconds) as heartbeat
                    var shouldSendUpdate = job.Percent != lastPercent || 
                                           job.Status != lastStatus || 
                                           job.Stage != lastStage ||
                                           heartbeatCounter >= 10;

                    if (shouldSendUpdate)
                    {
                        var eventData = JsonSerializer.Serialize(new
                        {
                            percent = job.Percent,
                            stage = job.Stage,
                            status = MapJobStatus(job.Status.ToString()),
                            message = job.Logs.LastOrDefault() ?? "Processing...",
                            outputPath = job.OutputPath
                        });

                        await Response.WriteAsync($"event: job-progress\n", cancellationToken).ConfigureAwait(false);
                        await Response.WriteAsync($"data: {eventData}\n\n", cancellationToken).ConfigureAwait(false);
                        await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);

                        lastPercent = job.Percent;
                        lastStatus = job.Status;
                        lastStage = job.Stage;
                        heartbeatCounter = 0;
                    }

                    // Check for terminal state
                    if (job.Status == JobStatus.Done || job.Status == JobStatus.Succeeded ||
                        job.Status == JobStatus.Failed || job.Status == JobStatus.Canceled)
                    {
                        var finalData = JsonSerializer.Serialize(new
                        {
                            percent = job.Percent,
                            stage = job.Stage,
                            status = MapJobStatus(job.Status.ToString()),
                            message = job.Logs.LastOrDefault() ?? "",
                            outputPath = job.OutputPath ?? job.Artifacts.FirstOrDefault(a => a.Type == "video" || a.Type == "video/mp4")?.Path
                        });

                        await Response.WriteAsync($"event: job-completed\n", cancellationToken).ConfigureAwait(false);
                        await Response.WriteAsync($"data: {finalData}\n\n", cancellationToken).ConfigureAwait(false);
                        await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
                        break;
                    }
                }

                return;
            }

            // Fall back to export job service for export-specific jobs
            if (_exportJobService != null)
            {
                var exportJob = await _exportJobService.GetJobAsync(jobId).ConfigureAwait(false);
                if (exportJob != null)
                {
                    Log.Information("[{CorrelationId}] Streaming export job {JobId} via SSE", correlationId, jobId);

                    await foreach (var update in _exportJobService.SubscribeToJobUpdatesAsync(jobId, cancellationToken).ConfigureAwait(false))
                    {
                        var eventData = JsonSerializer.Serialize(new
                        {
                            percent = update.Progress,
                            stage = update.Stage,
                            status = update.Status,
                            message = update.Message,
                            outputPath = update.OutputPath,
                            errorMessage = update.ErrorMessage
                        });

                        await Response.WriteAsync($"event: job-progress\n", cancellationToken).ConfigureAwait(false);
                        await Response.WriteAsync($"data: {eventData}\n\n", cancellationToken).ConfigureAwait(false);
                        await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);

                        // Send completion event when job reaches terminal state
                        if (update.Status is "completed" or "failed" or "cancelled")
                        {
                            await Response.WriteAsync($"event: job-completed\n", cancellationToken).ConfigureAwait(false);
                            await Response.WriteAsync($"data: {eventData}\n\n", cancellationToken).ConfigureAwait(false);
                            await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
                            break;
                        }
                    }

                    return;
            }
            }

            // No job found in either service after extended wait
            var maxWaitSeconds = maxWaitTimeMs / 1000;
            Log.Warning("[{CorrelationId}] Job {JobId} not found after waiting {MaxWaitSeconds} seconds",
                correlationId, jobId, maxWaitSeconds);
            await Response.WriteAsync("event: error\n", cancellationToken).ConfigureAwait(false);
            await Response.WriteAsync($"data: {{\"error\":\"Job not found after waiting {maxWaitSeconds} seconds\",\"jobId\":\"{jobId}\",\"correlationId\":\"{correlationId}\"}}\n\n", cancellationToken).ConfigureAwait(false);
            await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected, this is normal
            Log.Debug("[{CorrelationId}] SSE progress stream canceled for job {JobId}", correlationId, jobId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{CorrelationId}] Error streaming progress for job {JobId}", correlationId, jobId);
            try
            {
                var errorData = JsonSerializer.Serialize(new { error = ex.Message });
                await Response.WriteAsync($"event: error\n", CancellationToken.None).ConfigureAwait(false);
                await Response.WriteAsync($"data: {errorData}\n\n", CancellationToken.None).ConfigureAwait(false);
                await Response.Body.FlushAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // Ignore errors when trying to send error message
            }
        }
    }

    /// <summary>
    /// Parse aspect ratio string to enum
    /// </summary>
    private static Core.Models.Aspect ParseAspect(string aspect)
    {
        return aspect switch
        {
            "Widescreen16x9" or "16:9" or "widescreen" => Core.Models.Aspect.Widescreen16x9,
            "Vertical9x16" or "9:16" or "portrait" => Core.Models.Aspect.Vertical9x16,
            "Square1x1" or "1:1" or "square" => Core.Models.Aspect.Square1x1,
            _ => Core.Models.Aspect.Widescreen16x9
        };
    }

    /// <summary>
    /// Parse pacing string to enum
    /// </summary>
    private static Core.Models.Pacing ParsePacing(string pacing)
    {
        return pacing.ToLowerInvariant() switch
        {
            "chill" => Core.Models.Pacing.Chill,
            "conversational" => Core.Models.Pacing.Conversational,
            "fast" => Core.Models.Pacing.Fast,
            _ => Core.Models.Pacing.Conversational
        };
    }

    /// <summary>
    /// Parse density string to enum
    /// </summary>
    private static Core.Models.Density ParseDensity(string density)
    {
        return density.ToLowerInvariant() switch
        {
            "sparse" => Core.Models.Density.Sparse,
            "balanced" => Core.Models.Density.Balanced,
            "dense" => Core.Models.Density.Dense,
            _ => Core.Models.Density.Balanced
        };
    }

    /// <summary>
    /// Parse pause style string to enum
    /// </summary>
    private static Core.Models.PauseStyle ParsePauseStyle(string pause)
    {
        return pause.ToLowerInvariant() switch
        {
            "natural" => Core.Models.PauseStyle.Natural,
            "short" => Core.Models.PauseStyle.Short,
            "long" => Core.Models.PauseStyle.Long,
            "dramatic" => Core.Models.PauseStyle.Dramatic,
            _ => Core.Models.PauseStyle.Natural
        };
    }
}

/// <summary>
/// Request model for creating a new job
/// </summary>
public record CreateJobRequest(
    BriefDto Brief,
    PlanSpec PlanSpec,
    VoiceSpec VoiceSpec,
    RenderSpec RenderSpec
);
