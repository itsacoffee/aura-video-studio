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
            Log.Information("Creating new job for topic: {Topic}", request.Brief.Topic);

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
                HttpContext.TraceIdentifier,
                ct
            );

            return Ok(new { jobId = job.Id, status = job.Status, stage = job.Stage });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating job");
            return StatusCode(500, new { error = "Error creating job", details = ex.Message });
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
            var job = _jobRunner.GetJob(jobId);
            if (job == null)
            {
                return NotFound(new { error = "Job not found" });
            }

            return Ok(job);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving job {JobId}", jobId);
            return StatusCode(500, new { error = "Error retrieving job", details = ex.Message });
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
            var jobs = _jobRunner.ListJobs(limit);
            return Ok(new { jobs = jobs, count = jobs.Count });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error listing jobs");
            return StatusCode(500, new { error = "Error listing jobs", details = ex.Message });
        }
    }

    /// <summary>
    /// Get latest artifacts from recent jobs
    /// </summary>
    [HttpGet("recent-artifacts")]
    public IActionResult GetRecentArtifacts([FromQuery] int limit = 5)
    {
        try
        {
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

            return Ok(new { artifacts = artifacts, count = artifacts.Count });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting recent artifacts");
            return StatusCode(500, new { error = "Error getting recent artifacts", details = ex.Message });
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
