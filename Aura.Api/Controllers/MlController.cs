using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Filters;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.ML;
using Aura.Core.Services.ML;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for ML model training and annotation management
/// Requires Advanced Mode to be enabled
/// </summary>
[ApiController]
[Route("api/ml")]
[RequireAdvancedMode]
public class MlController : ControllerBase
{
    private readonly ILogger<MlController> _logger;
    private readonly AnnotationStorageService _annotationStorage;
    private readonly MlTrainingWorker _trainingWorker;
    private readonly ModelManager _modelManager;
    private readonly PreflightCheckService _preflightCheck;
    private readonly TrainingAuditService _auditService;

    public MlController(
        ILogger<MlController> logger,
        AnnotationStorageService annotationStorage,
        MlTrainingWorker trainingWorker,
        ModelManager modelManager,
        PreflightCheckService preflightCheck,
        TrainingAuditService auditService)
    {
        _logger = logger;
        _annotationStorage = annotationStorage;
        _trainingWorker = trainingWorker;
        _modelManager = modelManager;
        _preflightCheck = preflightCheck;
        _auditService = auditService;
    }

    /// <summary>
    /// Upload frame annotations for training
    /// </summary>
    [HttpPost("annotations/upload")]
    public async Task<ActionResult<object>> UploadAnnotations(
        [FromBody] UploadAnnotationsRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        _logger.LogInformation("Uploading {Count} annotations, CorrelationId: {CorrelationId}", 
            request.Annotations.Count, correlationId);

        try
        {
            if (request.Annotations.Count == 0)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Status = 400,
                    Detail = "No annotations provided",
                    Extensions = { ["correlationId"] = correlationId }
                });
            }

            var userId = GetUserId();
            
            var annotations = request.Annotations.Select(a => new AnnotationRecord(
                FramePath: a.FramePath,
                Rating: a.Rating,
                Timestamp: DateTime.UtcNow,
                Metadata: a.Metadata
            )).ToList();

            await _annotationStorage.StoreAnnotationsAsync(userId, annotations, cancellationToken);

            _logger.LogInformation("Successfully uploaded {Count} annotations for user {UserId}", 
                annotations.Count, userId);

            return Ok(new { message = $"Successfully uploaded {annotations.Count} annotations" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid annotation data");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Annotation Data",
                Status = 400,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = correlationId }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload annotations");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Upload Failed",
                Status = 500,
                Detail = "An error occurred while uploading annotations",
                Extensions = { ["correlationId"] = correlationId }
            });
        }
    }

    /// <summary>
    /// Get annotation statistics for the current user
    /// </summary>
    [HttpGet("annotations/stats")]
    public async Task<ActionResult<AnnotationStatsDto>> GetAnnotationStats(
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        _logger.LogInformation("Getting annotation stats, CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var userId = GetUserId();
            var stats = await _annotationStorage.GetStatsAsync(userId, cancellationToken);

            return Ok(new AnnotationStatsDto(
                UserId: stats.UserId,
                TotalAnnotations: stats.TotalAnnotations,
                AverageRating: stats.AverageRating,
                OldestAnnotation: stats.OldestAnnotation,
                NewestAnnotation: stats.NewestAnnotation
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get annotation stats");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Stats Retrieval Failed",
                Status = 500,
                Detail = "An error occurred while retrieving annotation statistics",
                Extensions = { ["correlationId"] = correlationId }
            });
        }
    }

    /// <summary>
    /// Start a new training job for frame importance model
    /// </summary>
    [HttpPost("train/frame-importance")]
    public async Task<ActionResult<StartTrainingResponse>> StartTraining(
        [FromBody] StartTrainingRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        _logger.LogInformation("Starting training job, CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var userId = GetUserId();
            
            var stats = await _annotationStorage.GetStatsAsync(userId, cancellationToken);
            if (stats.TotalAnnotations == 0)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Insufficient Data",
                    Status = 400,
                    Detail = "No annotations available for training. Please upload annotations first.",
                    Extensions = { ["correlationId"] = correlationId }
                });
            }

            var jobId = await _trainingWorker.SubmitJobAsync(
                userId, 
                request.ModelName, 
                request.PipelineConfig,
                cancellationToken);

            _logger.LogInformation("Training job {JobId} submitted for user {UserId}", jobId, userId);

            return Ok(new StartTrainingResponse(
                JobId: jobId,
                Message: "Training job submitted successfully"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start training job");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Training Start Failed",
                Status = 500,
                Detail = "An error occurred while starting the training job",
                Extensions = { ["correlationId"] = correlationId }
            });
        }
    }

    /// <summary>
    /// Get the status of a training job
    /// </summary>
    [HttpGet("train/{jobId}/status")]
    public ActionResult<TrainingJobStatusDto> GetJobStatus(string jobId)
    {
        var correlationId = HttpContext.TraceIdentifier;
        _logger.LogInformation("Getting status for job {JobId}, CorrelationId: {CorrelationId}", 
            jobId, correlationId);

        try
        {
            var job = _trainingWorker.GetJobStatus(jobId);
            
            if (job == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Job Not Found",
                    Status = 404,
                    Detail = $"Training job {jobId} does not exist",
                    Extensions = { ["correlationId"] = correlationId }
                });
            }

            var metricsDto = job.Metrics != null
                ? new TrainingMetricsDto(
                    Loss: job.Metrics.Loss,
                    Samples: job.Metrics.Samples,
                    Duration: job.Metrics.Duration,
                    AdditionalMetrics: job.Metrics.AdditionalMetrics)
                : null;

            return Ok(new TrainingJobStatusDto(
                JobId: job.JobId,
                State: job.State.ToString(),
                Progress: job.Progress,
                Metrics: metricsDto,
                ModelPath: job.ModelPath,
                Error: job.Error,
                CreatedAt: job.CreatedAt,
                CompletedAt: job.CompletedAt
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get job status for {JobId}", jobId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Status Retrieval Failed",
                Status = 500,
                Detail = "An error occurred while retrieving job status",
                Extensions = { ["correlationId"] = correlationId }
            });
        }
    }

    /// <summary>
    /// Cancel a running training job
    /// </summary>
    [HttpPost("train/{jobId}/cancel")]
    public ActionResult<object> CancelJob(string jobId)
    {
        var correlationId = HttpContext.TraceIdentifier;
        _logger.LogInformation("Cancelling job {JobId}, CorrelationId: {CorrelationId}", 
            jobId, correlationId);

        try
        {
            var cancelled = _trainingWorker.CancelJob(jobId);
            
            if (!cancelled)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Cancellation Failed",
                    Status = 400,
                    Detail = $"Job {jobId} cannot be cancelled (not found or not running)",
                    Extensions = { ["correlationId"] = correlationId }
                });
            }

            return Ok(new { message = $"Job {jobId} cancellation requested" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel job {JobId}", jobId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Cancellation Failed",
                Status = 500,
                Detail = "An error occurred while cancelling the job",
                Extensions = { ["correlationId"] = correlationId }
            });
        }
    }

    /// <summary>
    /// Revert to the default model
    /// </summary>
    [HttpPost("model/revert")]
    public async Task<ActionResult<object>> RevertToDefaultModel(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        _logger.LogInformation("Reverting to default model, CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var reverted = await _modelManager.RevertToDefaultAsync(cancellationToken);
            
            if (!reverted)
            {
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Revert Failed",
                    Status = 500,
                    Detail = "Failed to revert to default model",
                    Extensions = { ["correlationId"] = correlationId }
                });
            }

            return Ok(new { message = "Successfully reverted to default model" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revert to default model");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Revert Failed",
                Status = 500,
                Detail = "An error occurred while reverting to the default model",
                Extensions = { ["correlationId"] = correlationId }
            });
        }
    }

    /// <summary>
    /// Run preflight check before training to verify system capabilities
    /// </summary>
    [HttpGet("train/preflight")]
    public async Task<ActionResult<PreflightCheckResultDto>> RunPreflightCheck(
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        _logger.LogInformation("Running preflight check, CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var userId = GetUserId();
            var stats = await _annotationStorage.GetStatsAsync(userId, cancellationToken);
            
            var result = await _preflightCheck.CheckSystemCapabilitiesAsync(
                stats.TotalAnnotations, 
                cancellationToken);

            var dto = new PreflightCheckResultDto(
                Timestamp: result.Timestamp,
                AnnotationCount: result.AnnotationCount,
                HasGpu: result.HasGpu,
                GpuName: result.GpuName,
                GpuVramGb: result.GpuVramGb,
                TotalRamGb: result.TotalRamGb,
                AvailableRamGb: result.AvailableRamGb,
                AvailableDiskSpaceGb: result.AvailableDiskSpaceGb,
                EstimatedTrainingTimeMinutes: result.EstimatedTrainingTimeMinutes,
                MeetsMinimumRequirements: result.MeetsMinimumRequirements,
                Warnings: result.Warnings,
                Recommendations: result.Recommendations,
                Errors: result.Errors
            );

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run preflight check");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Preflight Check Failed",
                Status = 500,
                Detail = "An error occurred while checking system capabilities",
                Extensions = { ["correlationId"] = correlationId }
            });
        }
    }

    /// <summary>
    /// Restore model from backup
    /// </summary>
    [HttpPost("model/restore-backup")]
    public async Task<ActionResult<object>> RestoreFromBackup(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        _logger.LogInformation("Restoring model from backup, CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var restored = await _modelManager.RestoreFromBackupAsync(cancellationToken);
            
            if (!restored)
            {
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Restore Failed",
                    Status = 500,
                    Detail = "Failed to restore model from backup. Backup may not exist.",
                    Extensions = { ["correlationId"] = correlationId }
                });
            }

            return Ok(new { message = "Successfully restored model from backup" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore model from backup");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Restore Failed",
                Status = 500,
                Detail = "An error occurred while restoring from backup",
                Extensions = { ["correlationId"] = correlationId }
            });
        }
    }

    /// <summary>
    /// Get training history from audit log
    /// </summary>
    [HttpGet("train/history")]
    public async Task<ActionResult<object>> GetTrainingHistory(
        [FromQuery] int maxRecords = 50,
        CancellationToken cancellationToken = default)
    {
        var correlationId = HttpContext.TraceIdentifier;
        _logger.LogInformation("Getting training history, CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var history = await _auditService.GetTrainingHistoryAsync(maxRecords, cancellationToken);
            return Ok(new { history, count = history.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get training history");
            return StatusCode(500, new ProblemDetails
            {
                Title = "History Retrieval Failed",
                Status = 500,
                Detail = "An error occurred while retrieving training history",
                Extensions = { ["correlationId"] = correlationId }
            });
        }
    }

    /// <summary>
    /// Get training statistics from audit log
    /// </summary>
    [HttpGet("train/statistics")]
    public async Task<ActionResult<object>> GetTrainingStatistics(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        _logger.LogInformation("Getting training statistics, CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var stats = await _auditService.GetTrainingStatisticsAsync(cancellationToken);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get training statistics");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Statistics Retrieval Failed",
                Status = 500,
                Detail = "An error occurred while retrieving training statistics",
                Extensions = { ["correlationId"] = correlationId }
            });
        }
    }

    private string GetUserId()
    {
        return "default-user";
    }
}
