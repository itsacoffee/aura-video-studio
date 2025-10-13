using Aura.Core.Artifacts;
using Aura.Core.Models;
using Aura.Core.Orchestrator;
using Microsoft.AspNetCore.Mvc;
using Serilog;

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

    public JobsController(JobRunner jobRunner, ArtifactManager artifactManager)
    {
        _jobRunner = jobRunner;
        _artifactManager = artifactManager;
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
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Creating new job for topic: {Topic}", correlationId, request.Brief.Topic);

            var brief = new Brief(
                Topic: request.Brief.Topic,
                Audience: request.Brief.Audience,
                Goal: request.Brief.Goal,
                Tone: request.Brief.Tone,
                Language: request.Brief.Language,
                Aspect: request.Brief.Aspect
            );

            var planSpec = new PlanSpec(
                TargetDuration: request.PlanSpec.TargetDuration,
                Pacing: request.PlanSpec.Pacing,
                Density: request.PlanSpec.Density,
                Style: request.PlanSpec.Style
            );

            var voiceSpec = new VoiceSpec(
                VoiceName: request.VoiceSpec.VoiceName,
                Rate: request.VoiceSpec.Rate,
                Pitch: request.VoiceSpec.Pitch,
                Pause: request.VoiceSpec.Pause
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
                ct
            );

            return Ok(new { jobId = job.Id, status = job.Status, stage = job.Stage, correlationId });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error creating job", correlationId);
            
            // Return structured ProblemDetails with correlation ID
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E203",
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
    public IActionResult GetJob(string jobId)
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
                    type = "https://docs.aura.studio/errors/E404",
                    title = "Job Not Found", 
                    status = 404,
                    detail = $"Job {jobId} not found",
                    correlationId
                });
            }

            return Ok(job);
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error retrieving job {JobId}", correlationId, jobId);
            
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Error Retrieving Job",
                status = 500,
                detail = $"Failed to retrieve job: {ex.Message}",
                correlationId
            });
        }
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
                type = "https://docs.aura.studio/errors/E500",
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
                    type = "https://docs.aura.studio/errors/E404",
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
                    type = "https://docs.aura.studio/errors/E400",
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
                type = "https://docs.aura.studio/errors/E500",
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
    /// Retry a failed job with specific remediation strategy
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
                    type = "https://docs.aura.studio/errors/E404",
                    title = "Job Not Found",
                    status = 404,
                    detail = $"Job {jobId} not found",
                    correlationId
                });
            }
            
            // For now, return guidance on retry strategies
            // Full retry implementation would require job state management
            return Ok(new
            {
                jobId,
                currentStatus = job.Status,
                currentStage = job.Stage,
                strategy = strategy ?? "manual",
                message = "Job retry not yet fully implemented. Please create a new job with adjusted settings.",
                suggestedActions = new[]
                {
                    "Re-generate with different TTS provider if narration failed",
                    "Use software encoder (x264) if hardware encoding failed",
                    "Check FFmpeg installation if render failed",
                    "Verify input files are valid if validation failed"
                },
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error retrying job {JobId}", correlationId, jobId);
            
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Retry Failed",
                status = 500,
                detail = $"Failed to retry job: {ex.Message}",
                correlationId
            });
        }
    }
}

/// <summary>
/// Request model for creating a new job
/// </summary>
public record CreateJobRequest(
    Brief Brief,
    PlanSpec PlanSpec,
    VoiceSpec VoiceSpec,
    RenderSpec RenderSpec
);
