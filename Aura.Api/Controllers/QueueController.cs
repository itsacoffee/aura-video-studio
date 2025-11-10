using Aura.Core.Models;
using Aura.Core.Orchestrator;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for managing the job queue and listing jobs by status
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class QueueController : ControllerBase
{
    private readonly JobRunner _jobRunner;

    public QueueController(JobRunner jobRunner)
    {
        _jobRunner = jobRunner;
    }

    /// <summary>
    /// Get all jobs in the queue, optionally filtered by status
    /// </summary>
    /// <param name="status">Optional status filter (pending, running, completed, failed, canceled)</param>
    /// <param name="limit">Maximum number of jobs to return (default 50, max 200)</param>
    [HttpGet]
    public IActionResult GetQueue(
        [FromQuery] string? status = null,
        [FromQuery] int limit = 50)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            
            // Validate limit
            if (limit < 1 || limit > 200)
            {
                return BadRequest(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E400",
                    title = "Invalid Limit",
                    status = 400,
                    detail = "Limit must be between 1 and 200",
                    correlationId
                });
            }

            // Get all jobs
            var jobs = _jobRunner.ListJobs(limit);

            // Apply status filter if provided
            if (!string.IsNullOrWhiteSpace(status))
            {
                JobStatus? statusFilter = status.ToLowerInvariant() switch
                {
                    "pending" or "queued" => JobStatus.Queued,
                    "running" => JobStatus.Running,
                    "completed" or "done" or "succeeded" => JobStatus.Done,
                    "failed" => JobStatus.Failed,
                    "canceled" or "cancelled" => JobStatus.Canceled,
                    _ => null
                };

                if (statusFilter == null)
                {
                    return BadRequest(new
                    {
                        type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E400",
                        title = "Invalid Status Filter",
                        status = 400,
                        detail = $"Invalid status filter: {status}. Valid values: pending, running, completed, failed, canceled",
                        correlationId
                    });
                }

                jobs = jobs.Where(j => j.Status == statusFilter.Value).ToList();
            }

            // Build response with job summary
            var jobSummaries = jobs.Select(j => new
            {
                jobId = j.Id,
                status = MapJobStatusToString(j.Status),
                stage = j.Stage,
                percent = j.Percent,
                createdAt = j.CreatedUtc,
                startedAt = j.StartedUtc ?? j.StartedAt,
                completedAt = j.CompletedUtc ?? j.FinishedAt,
                canceledAt = j.CanceledUtc,
                correlationId = j.CorrelationId,
                errorMessage = j.ErrorMessage,
                canResume = j.CanResume,
                lastCompletedStep = j.LastCompletedStep,
                artifactCount = j.Artifacts.Count,
                hasErrors = j.Errors.Any()
            }).ToList();

            var queueStats = new
            {
                total = jobs.Count,
                pending = jobs.Count(j => j.Status == JobStatus.Queued),
                running = jobs.Count(j => j.Status == JobStatus.Running),
                completed = jobs.Count(j => j.Status == JobStatus.Done || j.Status == JobStatus.Succeeded),
                failed = jobs.Count(j => j.Status == JobStatus.Failed),
                canceled = jobs.Count(j => j.Status == JobStatus.Canceled)
            };

            return Ok(new
            {
                jobs = jobSummaries,
                stats = queueStats,
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error getting queue", correlationId);
            
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Error Getting Queue",
                status = 500,
                detail = $"Failed to retrieve queue: {ex.Message}",
                correlationId
            });
        }
    }

    private static string MapJobStatusToString(JobStatus status)
    {
        return status switch
        {
            JobStatus.Queued => "pending",
            JobStatus.Running => "running",
            JobStatus.Done or JobStatus.Succeeded => "completed",
            JobStatus.Failed => "failed",
            JobStatus.Canceled => "canceled",
            _ => "unknown"
        };
    }
}
