using Aura.Core.Models;
using Aura.Core.Orchestrator;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for render job operations and progress tracking
/// </summary>
[ApiController]
[Route("api/render")]
public class RenderController : ControllerBase
{
    private readonly JobRunner _jobRunner;

    public RenderController(JobRunner jobRunner)
    {
        _jobRunner = jobRunner;
    }

    /// <summary>
    /// Get detailed progress information for a render job
    /// </summary>
    /// <param name="id">The job ID</param>
    [HttpGet("{id}/progress")]
    public IActionResult GetRenderProgress(string id)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            
            var job = _jobRunner.GetJob(id);
            if (job == null)
            {
                Log.Warning("[{CorrelationId}] Render job not found: {JobId}", correlationId, id);
                return NotFound(new 
                { 
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E404",
                    title = "Render Job Not Found", 
                    status = 404,
                    detail = $"Render job {id} not found",
                    correlationId
                });
            }

            // Calculate progress details
            var elapsedTime = job.StartedUtc.HasValue 
                ? DateTime.UtcNow - job.StartedUtc.Value 
                : TimeSpan.Zero;
            
            var estimatedTotalTime = job.Eta;
            var remainingTime = estimatedTotalTime.HasValue && job.Eta.HasValue
                ? job.Eta.Value
                : (TimeSpan?)null;

            // Map status to string
            var statusString = job.Status switch
            {
                JobStatus.Queued => "pending",
                JobStatus.Running => "running",
                JobStatus.Done or JobStatus.Succeeded => "completed",
                JobStatus.Failed => "failed",
                JobStatus.Canceled => "canceled",
                _ => "unknown"
            };

            // Build detailed progress response
            var response = new
            {
                jobId = job.Id,
                status = statusString,
                stage = job.Stage,
                progressPct = job.Percent,
                
                // Timestamps
                createdAt = job.CreatedUtc,
                startedAt = job.StartedUtc ?? job.StartedAt,
                completedAt = job.CompletedUtc ?? job.FinishedAt,
                canceledAt = job.CanceledUtc,
                
                // Time tracking
                elapsedSeconds = (int)elapsedTime.TotalSeconds,
                estimatedTotalSeconds = estimatedTotalTime?.TotalSeconds,
                remainingSeconds = remainingTime?.TotalSeconds,
                eta = job.Eta,
                
                // Current step details
                currentStep = job.Steps.LastOrDefault(s => s.Status == StepStatus.Running),
                completedSteps = job.Steps.Where(s => s.Status == StepStatus.Succeeded).Select(s => s.Name).ToList(),
                lastCompletedStep = job.LastCompletedStep,
                
                // Error and warning info
                hasErrors = job.Errors.Any(),
                errorCount = job.Errors.Count,
                hasWarnings = job.Warnings.Any(),
                warningCount = job.Warnings.Count,
                errorMessage = job.ErrorMessage,
                
                // Resumability
                canResume = job.CanResume,
                
                // Output info
                output = job.Output,
                artifacts = job.Artifacts.Select(a => new
                {
                    name = a.Name,
                    type = a.Type,
                    sizeBytes = a.SizeBytes,
                    path = a.Path
                }).ToList(),
                
                correlationId = job.CorrelationId ?? correlationId
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error getting render progress for job {JobId}", correlationId, id);
            
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Error Getting Render Progress",
                status = 500,
                detail = $"Failed to retrieve render progress: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Cancel a render job
    /// </summary>
    /// <param name="id">The job ID</param>
    [HttpPost("{id}/cancel")]
    public IActionResult CancelRender(string id)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Cancel request for render job {JobId}", correlationId, id);
            
            var job = _jobRunner.GetJob(id);
            if (job == null)
            {
                return NotFound(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E404",
                    title = "Render Job Not Found",
                    status = 404,
                    detail = $"Render job {id} not found",
                    correlationId
                });
            }

            // Check if job is in a cancellable state
            if (job.Status != JobStatus.Running && job.Status != JobStatus.Queued)
            {
                return BadRequest(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E400",
                    title = "Render Job Not Cancellable",
                    status = 400,
                    detail = $"Render job is in {job.Status} status and cannot be cancelled",
                    currentStatus = job.Status,
                    correlationId
                });
            }
            
            // Attempt to cancel the job
            bool cancelled = _jobRunner.CancelJob(id);
            
            if (cancelled)
            {
                Log.Information("[{CorrelationId}] Successfully cancelled render job {JobId}", correlationId, id);
                return Accepted(new
                {
                    jobId = id,
                    message = "Render job cancellation triggered successfully",
                    currentStatus = job.Status,
                    correlationId
                });
            }
            else
            {
                Log.Warning("[{CorrelationId}] Failed to cancel render job {JobId}", correlationId, id);
                return StatusCode(500, new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                    title = "Cancellation Failed",
                    status = 500,
                    detail = "Render job could not be cancelled. It may have already completed or been cancelled.",
                    currentStatus = job.Status,
                    correlationId
                });
            }
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error canceling render job {JobId}", correlationId, id);
            
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Cancel Failed",
                status = 500,
                detail = $"Failed to cancel render job: {ex.Message}",
                correlationId
            });
        }
    }
}
