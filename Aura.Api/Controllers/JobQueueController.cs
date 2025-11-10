using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Services.Queue;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for managing the background job queue
/// Provides endpoints for enqueueing, cancelling, and monitoring jobs
/// </summary>
[ApiController]
[Route("api/queue")]
public class JobQueueController : ControllerBase
{
    private readonly ILogger<JobQueueController> _logger;
    private readonly BackgroundJobQueueManager _queueManager;

    public JobQueueController(
        ILogger<JobQueueController> logger,
        BackgroundJobQueueManager queueManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _queueManager = queueManager ?? throw new ArgumentNullException(nameof(queueManager));
    }

    /// <summary>
    /// Enqueues a new video generation job
    /// </summary>
    [HttpPost("enqueue")]
    public async Task<IActionResult> EnqueueJob(
        [FromBody] EnqueueJobRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogInformation(
                "[{CorrelationId}] Enqueue request received for topic: {Topic}, Priority: {Priority}",
                correlationId, request.Brief.Topic, request.Priority);

            var jobId = await _queueManager.EnqueueJobAsync(
                request.Brief,
                request.PlanSpec,
                request.VoiceSpec,
                request.RenderSpec,
                request.Priority,
                correlationId,
                request.IsQuickDemo,
                ct);

            _logger.LogInformation(
                "[{CorrelationId}] Job {JobId} enqueued successfully",
                correlationId, jobId);

            return Accepted(new
            {
                jobId,
                status = "Pending",
                message = "Job enqueued for background processing",
                correlationId,
                priority = request.Priority
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogError(ex, "[{CorrelationId}] Error enqueueing job", correlationId);

            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Enqueue Failed",
                status = 500,
                detail = $"Failed to enqueue job: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Gets job status from queue
    /// </summary>
    [HttpGet("{jobId}")]
    public async Task<IActionResult> GetJob(string jobId, CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            var job = await _queueManager.GetJobAsync(jobId, ct);

            if (job == null)
            {
                return NotFound(new
                {
                    type = "https://docs.aura.studio/errors/E404",
                    title = "Job Not Found",
                    status = 404,
                    detail = $"Job {jobId} not found in queue",
                    correlationId
                });
            }

            return Ok(new
            {
                jobId = job.JobId,
                status = job.Status,
                priority = job.Priority,
                progress = job.ProgressPercent,
                currentStage = job.CurrentStage,
                enqueuedAt = job.EnqueuedAt,
                startedAt = job.StartedAt,
                completedAt = job.CompletedAt,
                outputPath = job.OutputPath,
                errorMessage = job.LastError,
                retryCount = job.RetryCount,
                maxRetries = job.MaxRetries,
                nextRetryAt = job.NextRetryAt,
                workerId = job.WorkerId,
                correlationId = job.CorrelationId,
                isQuickDemo = job.IsQuickDemo
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogError(ex, "[{CorrelationId}] Error getting job {JobId}", correlationId, jobId);

            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Error Getting Job",
                status = 500,
                detail = $"Failed to retrieve job: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Lists all jobs in queue
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListJobs(
        [FromQuery] string? status = null,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            var jobs = await _queueManager.GetJobsAsync(status, limit, ct);

            var jobList = jobs.Select(j => new
            {
                jobId = j.JobId,
                status = j.Status,
                priority = j.Priority,
                progress = j.ProgressPercent,
                currentStage = j.CurrentStage,
                enqueuedAt = j.EnqueuedAt,
                startedAt = j.StartedAt,
                completedAt = j.CompletedAt,
                outputPath = j.OutputPath,
                errorMessage = j.LastError,
                retryCount = j.RetryCount,
                correlationId = j.CorrelationId,
                isQuickDemo = j.IsQuickDemo
            }).ToList();

            return Ok(new
            {
                jobs = jobList,
                count = jobList.Count,
                filterStatus = status,
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogError(ex, "[{CorrelationId}] Error listing jobs", correlationId);

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
    /// Cancels a job in the queue
    /// </summary>
    [HttpPost("{jobId}/cancel")]
    public async Task<IActionResult> CancelJob(string jobId, CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogInformation("[{CorrelationId}] Cancel request for job {JobId}", correlationId, jobId);

            var cancelled = await _queueManager.CancelJobAsync(jobId, ct);

            if (!cancelled)
            {
                return BadRequest(new
                {
                    type = "https://docs.aura.studio/errors/E400",
                    title = "Cannot Cancel Job",
                    status = 400,
                    detail = $"Job {jobId} cannot be cancelled (not found or already completed)",
                    correlationId
                });
            }

            return Accepted(new
            {
                jobId,
                message = "Job cancellation requested",
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogError(ex, "[{CorrelationId}] Error cancelling job {JobId}", correlationId, jobId);

            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Cancel Failed",
                status = 500,
                detail = $"Failed to cancel job: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Gets queue statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            var stats = await _queueManager.GetStatisticsAsync(ct);

            return Ok(new
            {
                statistics = stats,
                timestamp = DateTime.UtcNow,
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogError(ex, "[{CorrelationId}] Error getting statistics", correlationId);

            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Error Getting Statistics",
                status = 500,
                detail = $"Failed to retrieve statistics: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Gets queue configuration
    /// </summary>
    [HttpGet("configuration")]
    public async Task<IActionResult> GetConfiguration(CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            var config = await _queueManager.GetConfigurationAsync(ct);

            return Ok(new
            {
                configuration = config,
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogError(ex, "[{CorrelationId}] Error getting configuration", correlationId);

            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Error Getting Configuration",
                status = 500,
                detail = $"Failed to retrieve configuration: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Updates queue configuration
    /// </summary>
    [HttpPut("configuration")]
    public async Task<IActionResult> UpdateConfiguration(
        [FromBody] UpdateConfigurationRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogInformation(
                "[{CorrelationId}] Configuration update request: MaxConcurrent={MaxConcurrent}, Enabled={Enabled}",
                correlationId, request.MaxConcurrentJobs, request.IsEnabled);

            await _queueManager.UpdateConfigurationAsync(
                request.MaxConcurrentJobs,
                request.IsEnabled,
                ct);

            var config = await _queueManager.GetConfigurationAsync(ct);

            return Ok(new
            {
                message = "Configuration updated successfully",
                configuration = config,
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogError(ex, "[{CorrelationId}] Error updating configuration", correlationId);

            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Configuration Update Failed",
                status = 500,
                detail = $"Failed to update configuration: {ex.Message}",
                correlationId
            });
        }
    }
}

/// <summary>
/// Request model for enqueueing a job
/// </summary>
public record EnqueueJobRequest(
    Brief Brief,
    PlanSpec PlanSpec,
    VoiceSpec VoiceSpec,
    RenderSpec RenderSpec,
    int Priority = 5,
    bool IsQuickDemo = false);

/// <summary>
/// Request model for updating configuration
/// </summary>
public record UpdateConfigurationRequest(
    int? MaxConcurrentJobs = null,
    bool? IsEnabled = null);
