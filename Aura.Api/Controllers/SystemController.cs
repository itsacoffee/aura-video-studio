using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Services;
using Aura.Core.Configuration;
using Aura.Core.Services.FFmpeg;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// System health and status information
/// </summary>
[ApiController]
[Route("api/system")]
public class SystemController : ControllerBase
{
    private readonly ILogger<SystemController> _logger;
    private readonly IFFmpegStatusService _ffmpegStatusService;
    private readonly ShutdownOrchestrator _shutdownOrchestrator;
    private readonly FFmpegConfigurationStore _configStore;

    public SystemController(
        ILogger<SystemController> logger,
        IFFmpegStatusService ffmpegStatusService,
        ShutdownOrchestrator shutdownOrchestrator,
        FFmpegConfigurationStore configStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ffmpegStatusService = ffmpegStatusService ?? throw new ArgumentNullException(nameof(ffmpegStatusService));
        _shutdownOrchestrator = shutdownOrchestrator ?? throw new ArgumentNullException(nameof(shutdownOrchestrator));
        _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
    }

    /// <summary>
    /// Get comprehensive FFmpeg status including version and hardware acceleration support
    /// </summary>
    /// <remarks>
    /// Returns detailed information about FFmpeg installation:
    /// - Installation status and location
    /// - Version information and requirement compliance
    /// - Hardware acceleration support (NVENC, AMF, QuickSync, VideoToolbox)
    /// - Available hardware encoders
    /// 
    /// This endpoint always returns 200 OK with status information, even if FFmpeg is not installed.
    /// Check the 'installed' and 'valid' fields to determine FFmpeg availability.
    /// </remarks>
    [HttpGet("ffmpeg/status")]
    public async Task<IActionResult> GetFFmpegStatus(CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            _logger.LogInformation("[{CorrelationId}] GET /api/system/ffmpeg/status", correlationId);

            var status = await _ffmpegStatusService.GetStatusAsync(ct).ConfigureAwait(false);
            
            // Load configuration to get mode and validation result
            var config = await _configStore.LoadAsync(ct).ConfigureAwait(false);
            
            var mode = config?.Mode ?? FFmpegMode.None;
            var lastValidationResult = config?.LastValidationResult ?? FFmpegValidationResult.Unknown;

            return Ok(new
            {
                installed = status.Installed,
                valid = status.Valid,
                version = status.Version,
                path = status.Path,
                source = status.Source,
                mode = mode.ToString().ToLowerInvariant(),
                error = status.Error,
                errorCode = status.ErrorCode,
                errorMessage = status.ErrorMessage,
                attemptedPaths = status.AttemptedPaths,
                versionMeetsRequirement = status.VersionMeetsRequirement,
                minimumVersion = status.MinimumVersion,
                lastValidatedAt = config?.LastValidatedAt,
                lastValidationResult = lastValidationResult.ToString().ToLowerInvariant(),
                hardwareAcceleration = new
                {
                    nvencSupported = status.HardwareAcceleration.NvencSupported,
                    amfSupported = status.HardwareAcceleration.AmfSupported,
                    quickSyncSupported = status.HardwareAcceleration.QuickSyncSupported,
                    videoToolboxSupported = status.HardwareAcceleration.VideoToolboxSupported,
                    availableEncoders = status.HardwareAcceleration.AvailableEncoders
                },
                correlationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Error getting FFmpeg status", correlationId);

            return Ok(new
            {
                installed = false,
                valid = false,
                version = (string?)null,
                path = (string?)null,
                source = "None",
                mode = "none",
                error = "Failed to check FFmpeg status",
                errorCode = "E302",
                errorMessage = "Unable to check FFmpeg installation status. Please try again.",
                attemptedPaths = Array.Empty<string>(),
                versionMeetsRequirement = false,
                minimumVersion = "4.0",
                lastValidatedAt = (DateTime?)null,
                lastValidationResult = "unknown",
                hardwareAcceleration = new
                {
                    nvencSupported = false,
                    amfSupported = false,
                    quickSyncSupported = false,
                    videoToolboxSupported = false,
                    availableEncoders = Array.Empty<string>()
                },
                correlationId
            });
        }
    }

    /// <summary>
    /// Initiate graceful shutdown of the backend service
    /// </summary>
    /// <remarks>
    /// Initiates an ordered shutdown sequence:
    /// 1. Notifies active SSE connections
    /// 2. Closes SSE connections gracefully
    /// 3. Terminates child processes (FFmpeg)
    /// 4. Stops application host
    /// 
    /// Returns 202 Accepted immediately. The shutdown sequence runs asynchronously.
    /// </remarks>
    [HttpPost("shutdown")]
    public IActionResult InitiateShutdown([FromQuery] bool force = false)
    {
        var correlationId = HttpContext.TraceIdentifier;

        _logger.LogWarning("[{CorrelationId}] POST /api/system/shutdown (Force: {Force})", correlationId, force);

        Task.Run(async () =>
        {
            try
            {
                await _shutdownOrchestrator.InitiateShutdownAsync(force).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{CorrelationId}] Shutdown sequence failed", correlationId);
            }
        });

        return Accepted(new
        {
            message = "Shutdown initiated",
            force,
            correlationId
        });
    }

    /// <summary>
    /// Get current shutdown status
    /// </summary>
    [HttpGet("shutdown/status")]
    public IActionResult GetShutdownStatus()
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            var status = _shutdownOrchestrator.GetStatus();

            return Ok(new
            {
                shutdownInitiated = status.ShutdownInitiated,
                activeConnections = status.ActiveConnections,
                trackedProcesses = status.TrackedProcesses,
                correlationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Error getting shutdown status", correlationId);

            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Shutdown Status Error",
                status = 500,
                detail = $"Failed to get shutdown status: {ex.Message}",
                correlationId
            });
        }
    }
}
