using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aura.Core.Models;
using Aura.Core.Models.Export;
using Aura.Core.Models.Timeline;
using Aura.Core.Services.Editor;
using Aura.Core.Services.Export;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for video export operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExportController : ControllerBase
{
    private readonly IExportOrchestrationService _exportService;
    private readonly ILogger<ExportController> _logger;
    private readonly TimelineRenderer _timelineRenderer;

    public ExportController(
        IExportOrchestrationService exportService,
        ILogger<ExportController> logger,
        TimelineRenderer timelineRenderer)
    {
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timelineRenderer = timelineRenderer ?? throw new ArgumentNullException(nameof(timelineRenderer));
    }

    /// <summary>
    /// Start an export job
    /// </summary>
    /// <param name="request">Export request parameters</param>
    /// <returns>Job ID for tracking progress</returns>
    [HttpPost("start")]
    public async Task<IActionResult> StartExport([FromBody] ExportRequestDto request)
    {
        try
        {
            _logger.LogInformation("Starting export with preset {Preset}", request.PresetName);

            // Get preset by name
            var preset = ExportPresets.GetPresetByName(request.PresetName);
            if (preset == null)
            {
                return BadRequest(new { error = $"Unknown preset: {request.PresetName}" });
            }

            string inputFile;

            // If timeline data is provided, render it first
            if (request.Timeline != null && request.Timeline.Scenes?.Count > 0)
            {
                _logger.LogInformation("Rendering timeline with {SceneCount} scenes before export", request.Timeline.Scenes.Count);

                // Create temporary file for timeline render
                var tempDir = Path.Combine(Path.GetTempPath(), "aura-exports", Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);
                inputFile = Path.Combine(tempDir, "timeline_render.mp4");

                // Create RenderSpec from preset
                var renderSpec = new RenderSpec(
                    Res: preset.Resolution,
                    Container: "mp4",
                    VideoBitrateK: preset.VideoBitrate / 1000,
                    AudioBitrateK: preset.AudioBitrate / 1000,
                    Fps: preset.FrameRate,
                    Codec: preset.VideoCodec,
                    QualityLevel: preset.Quality == QualityLevel.Draft ? 50 : 
                                  preset.Quality == QualityLevel.Good ? 75 : 
                                  preset.Quality == QualityLevel.High ? 85 : 95
                );

                // Render timeline to temporary file
                try
                {
                    await _timelineRenderer.GenerateFinalAsync(
                        request.Timeline,
                        renderSpec,
                        inputFile,
                        null,
                        default).ConfigureAwait(false);

                    _logger.LogInformation("Timeline rendered successfully to {InputFile}", inputFile);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to render timeline");
                    return StatusCode(500, new { error = "Failed to render timeline", details = ex.Message });
                }
            }
            else if (!string.IsNullOrEmpty(request.InputFile))
            {
                // Use provided input file
                inputFile = request.InputFile;
            }
            else
            {
                return BadRequest(new { error = "Either timeline data or inputFile must be provided" });
            }

            // Create export request
            var exportRequest = new ExportRequest
            {
                InputFile = inputFile,
                OutputFile = request.OutputFile,
                Preset = preset,
                TargetPlatform = preset.Platform,
                StartTime = request.StartTime,
                Duration = request.Duration,
                Metadata = request.Metadata ?? new Dictionary<string, string>()
            };

            // Queue the export job
            var jobId = await _exportService.QueueExportAsync(exportRequest).ConfigureAwait(false);

            return Ok(new { jobId, message = "Export job queued successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start export");
            return StatusCode(500, new { error = "Failed to start export", details = ex.Message });
        }
    }

    /// <summary>
    /// Get status of an export job
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <returns>Job status</returns>
    [HttpGet("status/{jobId}")]
    public async Task<IActionResult> GetJobStatus(string jobId)
    {
        try
        {
            var job = await _exportService.GetJobStatusAsync(jobId).ConfigureAwait(false);
            if (job == null)
            {
                return NotFound(new { error = "Job not found" });
            }

            return Ok(new ExportJobDto
            {
                Id = job.Id,
                Status = job.Status.ToString(),
                Progress = job.Progress,
                CreatedAt = job.CreatedAt,
                StartedAt = job.StartedAt,
                CompletedAt = job.CompletedAt,
                ErrorMessage = job.ErrorMessage,
                OutputFile = job.OutputFile,
                EstimatedTimeRemaining = job.EstimatedTimeRemaining
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get job status for {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to get job status", details = ex.Message });
        }
    }

    /// <summary>
    /// Cancel an export job
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <returns>Success status</returns>
    [HttpPost("cancel/{jobId}")]
    public async Task<IActionResult> CancelJob(string jobId)
    {
        try
        {
            var success = await _exportService.CancelJobAsync(jobId).ConfigureAwait(false);
            if (!success)
            {
                return NotFound(new { error = "Job not found or cannot be cancelled" });
            }

            return Ok(new { message = "Job cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel job {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to cancel job", details = ex.Message });
        }
    }

    /// <summary>
    /// Get all active export jobs
    /// </summary>
    /// <returns>List of active jobs</returns>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveJobs()
    {
        try
        {
            var jobs = await _exportService.GetActiveJobsAsync().ConfigureAwait(false);
            var jobDtos = jobs.Select(job => new ExportJobDto
            {
                Id = job.Id,
                Status = job.Status.ToString(),
                Progress = job.Progress,
                CreatedAt = job.CreatedAt,
                StartedAt = job.StartedAt,
                CompletedAt = job.CompletedAt,
                ErrorMessage = job.ErrorMessage,
                OutputFile = job.OutputFile,
                EstimatedTimeRemaining = job.EstimatedTimeRemaining
            }).ToList();

            return Ok(jobDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active jobs");
            return StatusCode(500, new { error = "Failed to get active jobs", details = ex.Message });
        }
    }

    /// <summary>
    /// Get available export presets
    /// </summary>
    /// <returns>List of export presets</returns>
    [HttpGet("presets")]
    public IActionResult GetPresets()
    {
        try
        {
            var presets = ExportPresets.GetAllPresets();
            var presetDtos = presets.Select(p => new ExportPresetDto
            {
                Name = p.Name,
                Description = p.Description,
                Platform = p.Platform.ToString(),
                Resolution = $"{p.Resolution.Width}x{p.Resolution.Height}",
                VideoCodec = p.VideoCodec,
                AudioCodec = p.AudioCodec,
                FrameRate = p.FrameRate,
                VideoBitrate = p.VideoBitrate,
                AudioBitrate = p.AudioBitrate,
                AspectRatio = p.AspectRatio.ToString(),
                Quality = p.Quality.ToString()
            }).ToList();

            return Ok(presetDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get presets");
            return StatusCode(500, new { error = "Failed to get presets", details = ex.Message });
        }
    }

    /// <summary>
    /// Get export history
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>List of export history records</returns>
    [HttpGet("history")]
    public async Task<IActionResult> GetExportHistory([FromQuery] string? status = null, [FromQuery] int limit = 100)
    {
        try
        {
            var history = await _exportService.GetExportHistoryAsync(status, limit).ConfigureAwait(false);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get export history");
            return StatusCode(500, new { error = "Failed to get export history", details = ex.Message });
        }
    }

    /// <summary>
    /// Get hardware encoding capabilities
    /// </summary>
    /// <returns>Available hardware encoders</returns>
    [HttpGet("hardware-capabilities")]
    public IActionResult GetHardwareCapabilities()
    {
        try
        {
            // Hardware encoder detection requires platform-specific initialization
            return Ok(new
            {
                hasNVENC = false,
                hasAMF = false,
                hasQSV = false,
                hasVideoToolbox = false,
                availableEncoders = new[] { "libx264", "libx265" },
                recommendation = "Software encoding (CPU)"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get hardware capabilities");
            return StatusCode(500, new { error = "Failed to get hardware capabilities", details = ex.Message });
        }
    }

    /// <summary>
    /// Pause an export job
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <returns>Success status</returns>
    [HttpPost("pause/{jobId}")]
    public Task<IActionResult> PauseJob(string jobId)
    {
        try
        {
            // Pause functionality requires job state management
            _logger.LogInformation("Pause requested for job {JobId}", jobId);
            return Task.FromResult<IActionResult>(Ok(new { message = "Pause functionality not yet implemented" }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause job {JobId}", jobId);
            return Task.FromResult<IActionResult>(StatusCode(500, new { error = "Failed to pause job", details = ex.Message }));
        }
    }

    /// <summary>
    /// Resume a paused export job
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <returns>Success status</returns>
    [HttpPost("resume/{jobId}")]
    public Task<IActionResult> ResumeJob(string jobId)
    {
        try
        {
            // Resume functionality requires job state management
            _logger.LogInformation("Resume requested for job {JobId}", jobId);
            return Task.FromResult<IActionResult>(Ok(new { message = "Resume functionality not yet implemented" }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume job {JobId}", jobId);
            return Task.FromResult<IActionResult>(StatusCode(500, new { error = "Failed to resume job", details = ex.Message }));
        }
    }

    /// <summary>
    /// Retry a failed export job
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <returns>New job ID</returns>
    [HttpPost("retry/{jobId}")]
    public async Task<IActionResult> RetryJob(string jobId)
    {
        try
        {
            var job = await _exportService.GetJobStatusAsync(jobId).ConfigureAwait(false);
            if (job == null)
            {
                return NotFound(new { error = "Job not found" });
            }

            if (job.Status != ExportJobStatus.Failed)
            {
                return BadRequest(new { error = "Only failed jobs can be retried" });
            }

            // Create a new export request from the failed job
            var request = new ExportRequest
            {
                InputFile = job.InputFile,
                OutputFile = job.OutputFile,
                Preset = job.Preset,
                TargetPlatform = job.TargetPlatform
            };

            var newJobId = await _exportService.QueueExportAsync(request).ConfigureAwait(false);
            return Ok(new { jobId = newJobId, message = "Export job retried successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry job {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to retry job", details = ex.Message });
        }
    }

    /// <summary>
    /// Run preflight validation for an export
    /// </summary>
    /// <param name="request">Preflight request parameters</param>
    /// <returns>Preflight validation result</returns>
    [HttpPost("preflight")]
    public Task<IActionResult> RunPreflight([FromBody] ExportPreflightRequest request)
    {
        try
        {
            _logger.LogInformation("Running preflight validation for preset {Preset}", request.PresetName);

            var preset = ExportPresets.GetPresetByName(request.PresetName);
            if (preset == null)
            {
                return Task.FromResult<IActionResult>(BadRequest(new { error = $"Unknown preset: {request.PresetName}" }));
            }

            var outputDirectory = string.IsNullOrEmpty(request.OutputDirectory)
                ? Path.GetTempPath()
                : request.OutputDirectory;

            const double diskSpaceBufferMultiplier = 2.5;
            var estimatedFileSizeMB = ExportPresets.EstimateFileSizeMB(preset, request.VideoDuration);
            
            var result = new
            {
                canProceed = true,
                errors = new List<string>(),
                warnings = new List<string>(),
                recommendations = new List<string>(),
                estimates = new
                {
                    estimatedFileSizeMB,
                    estimatedDurationMinutes = 1.0,
                    requiredDiskSpaceMB = estimatedFileSizeMB * diskSpaceBufferMultiplier
                }
            };

            return Task.FromResult<IActionResult>(Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run preflight validation");
            return Task.FromResult<IActionResult>(StatusCode(500, new { error = "Failed to run preflight validation", details = ex.Message }));
        }
    }

    /// <summary>
    /// Check if cloud storage is available
    /// </summary>
    [HttpGet("cloud/status")]
    public IActionResult GetCloudStorageStatus()
    {
        // Cloud storage removed in PR #198 - this is a local-only application
        return Ok(new { available = false, message = "Cloud storage not supported in local-only application" });
    }

    /// <summary>
    /// Upload an exported file to cloud storage
    /// </summary>
    [HttpPost("cloud/upload")]
    public IActionResult UploadToCloud([FromBody] CloudUploadRequest request)
    {
        // Cloud storage removed in PR #198 - this is a local-only application
        return BadRequest(new { error = "Cloud storage not supported in local-only application" });
    }

    /// <summary>
    /// Generate a shareable link for a cloud-stored export
    /// </summary>
    [HttpPost("cloud/share")]
    public IActionResult GetShareableLink([FromBody] ShareLinkRequest request)
    {
        // Cloud storage removed in PR #198 - this is a local-only application
        return BadRequest(new { error = "Cloud storage not supported in local-only application" });
    }
}

/// <summary>
/// DTO for export request
/// </summary>
public record ExportRequestDto
{
    public string? InputFile { get; init; }
    public required string OutputFile { get; init; }
    public required string PresetName { get; init; }
    public TimeSpan? StartTime { get; init; }
    public TimeSpan? Duration { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
    public EditableTimeline? Timeline { get; init; }
}

/// <summary>
/// DTO for preflight request
/// </summary>
public record ExportPreflightRequest
{
    public required string PresetName { get; init; }
    public TimeSpan VideoDuration { get; init; }
    public string? OutputDirectory { get; init; }
    public string? SourceResolution { get; init; }
    public string? SourceAspectRatio { get; init; }
}

/// <summary>
/// DTO for export job status
/// </summary>
public record ExportJobDto
{
    public required string Id { get; init; }
    public required string Status { get; init; }
    public double Progress { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? ErrorMessage { get; init; }
    public string? OutputFile { get; init; }
    public TimeSpan? EstimatedTimeRemaining { get; init; }
}

/// <summary>
/// DTO for export preset
/// </summary>
public record ExportPresetDto
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Platform { get; init; }
    public required string Resolution { get; init; }
    public required string VideoCodec { get; init; }
    public required string AudioCodec { get; init; }
    public int FrameRate { get; init; }
    public int VideoBitrate { get; init; }
    public int AudioBitrate { get; init; }
    public required string AspectRatio { get; init; }
    public required string Quality { get; init; }
}

/// <summary>
/// DTO for cloud upload request
/// </summary>
public record CloudUploadRequest
{
    public required string FilePath { get; init; }
    public string? DestinationKey { get; init; }
}

/// <summary>
/// DTO for shareable link request
/// </summary>
public record ShareLinkRequest
{
    public required string Key { get; init; }
}
