using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Aura.Core.Downloads;
using Aura.Core.Runtime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

[ApiController]
[Route("api/ffmpeg")]
public class FFmpegController : ControllerBase
{
    private readonly ILogger<FFmpegController> _logger;
    private readonly FFmpegResolver _resolver;
    private readonly FfmpegInstaller _installer;
    private readonly EngineManifestLoader _manifestLoader;

    public FFmpegController(
        ILogger<FFmpegController> logger,
        FFmpegResolver resolver,
        FfmpegInstaller installer,
        EngineManifestLoader manifestLoader)
    {
        _logger = logger;
        _resolver = resolver;
        _installer = installer;
        _manifestLoader = manifestLoader;
    }

    /// <summary>
    /// Get current FFmpeg status including installation and version info
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            _logger.LogInformation("[{CorrelationId}] GET /api/ffmpeg/status", correlationId);

            var result = await _resolver.ResolveAsync(null, forceRefresh: true, ct).ConfigureAwait(false);

            return Ok(new
            {
                installed = result.Found && result.IsValid,
                version = result.Version,
                path = result.Path,
                source = result.Source,
                valid = result.IsValid,
                error = result.Error,
                correlationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Error getting FFmpeg status", correlationId);

            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E310",
                title = "FFmpeg Status Error",
                status = 500,
                detail = $"Failed to get FFmpeg status: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Install managed FFmpeg from trusted sources
    /// </summary>
    [HttpPost("install")]
    public async Task<IActionResult> Install(
        [FromBody] FFmpegInstallRequest? request,
        CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            _logger.LogInformation("[{CorrelationId}] POST /api/ffmpeg/install - Version: {Version}",
                correlationId, request?.Version ?? "latest");

            var version = request?.Version ?? "latest";

            // Get mirrors from manifest
            var manifest = await _manifestLoader.LoadManifestAsync().ConfigureAwait(false);
            var ffmpegEngine = manifest.Engines.FirstOrDefault(e => e.Id == "ffmpeg");

            if (ffmpegEngine == null)
            {
                return NotFound(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E311",
                    title = "FFmpeg Not Found",
                    status = 404,
                    detail = "FFmpeg not found in engine manifest",
                    correlationId
                });
            }

            var mirrors = new List<string>();

            // Add primary URL
            if (ffmpegEngine.Urls.ContainsKey("windows"))
            {
                mirrors.Add(ffmpegEngine.Urls["windows"]);
            }

            // Add mirrors from manifest
            if (ffmpegEngine.Mirrors != null && ffmpegEngine.Mirrors.ContainsKey("windows"))
            {
                mirrors.AddRange(ffmpegEngine.Mirrors["windows"]);
            }

            // Fallback mirror
            mirrors.Add("https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip");

            if (mirrors.Count == 0)
            {
                return BadRequest(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E312",
                    title = "No Mirrors Available",
                    status = 400,
                    detail = "No download mirrors available for FFmpeg",
                    correlationId
                });
            }

            // Start installation
            _logger.LogInformation("[{CorrelationId}] Installing FFmpeg from {Count} mirrors", correlationId, mirrors.Count);

            var installResult = await _installer.InstallFromMirrorsAsync(
                mirrors.ToArray(),
                version,
                null,
                null,
                ct).ConfigureAwait(false);

            if (!installResult.Success)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = installResult.ErrorMessage ?? "Failed to install FFmpeg",
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E313",
                    title = "Installation Failed",
                    status = 500,
                    detail = installResult.ErrorMessage ?? "Failed to install FFmpeg",
                    howToFix = new[]
                    {
                        "Check your internet connection",
                        "Verify firewall is not blocking the download",
                        "Try using a VPN if downloads are restricted in your region",
                        "Download FFmpeg manually and use the 'Use Existing FFmpeg' option"
                    },
                    correlationId
                });
            }

            // Invalidate resolver cache so new install is picked up
            _resolver.InvalidateCache();

            _logger.LogInformation("[{CorrelationId}] FFmpeg installed successfully: {Path}",
                correlationId, installResult.FfmpegPath);

            return Ok(new
            {
                success = true,
                message = "FFmpeg installed successfully",
                path = installResult.FfmpegPath,
                version = installResult.ValidationOutput,
                installedAt = installResult.InstalledAt,
                correlationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Error installing FFmpeg", correlationId);

            return StatusCode(500, new
            {
                success = false,
                message = $"Unexpected error during FFmpeg installation: {ex.Message}",
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E313",
                title = "Installation Error",
                status = 500,
                detail = $"Unexpected error during FFmpeg installation: {ex.Message}",
                howToFix = new[]
                {
                    "Check the error message for specific details",
                    "Ensure you have sufficient disk space",
                    "Try restarting the application",
                    "Check antivirus software is not blocking the installer"
                },
                correlationId
            });
        }
    }

    /// <summary>
    /// Rescan system for FFmpeg installations (PATH, common directories, managed install)
    /// </summary>
    [HttpPost("rescan")]
    public async Task<IActionResult> Rescan(CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            _logger.LogInformation("[{CorrelationId}] POST /api/ffmpeg/rescan", correlationId);

            // Invalidate cache to force fresh scan
            _resolver.InvalidateCache();

            // Perform resolution
            var result = await _resolver.ResolveAsync(null, forceRefresh: true, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = result.Found && result.IsValid,
                installed = result.Found && result.IsValid,
                version = result.Version,
                path = result.Path,
                source = result.Source,
                valid = result.IsValid,
                error = result.Error,
                message = result.Found && result.IsValid 
                    ? $"FFmpeg found at {result.Path}" 
                    : "FFmpeg not found or invalid",
                correlationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Error rescanning for FFmpeg", correlationId);

            return StatusCode(500, new
            {
                success = false,
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E314",
                title = "Rescan Error",
                status = 500,
                detail = $"Failed to rescan for FFmpeg: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Validate and use an existing FFmpeg installation at the specified path
    /// </summary>
    [HttpPost("use-existing")]
    public async Task<IActionResult> UseExisting(
        [FromBody] UseExistingFFmpegRequest request,
        CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            _logger.LogInformation("[{CorrelationId}] POST /api/ffmpeg/use-existing - Path: {Path}",
                correlationId, request.Path);

            if (string.IsNullOrWhiteSpace(request.Path))
            {
                return BadRequest(new
                {
                    success = false,
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E315",
                    title = "Invalid Path",
                    status = 400,
                    detail = "FFmpeg path is required",
                    correlationId
                });
            }

            // Resolve the path (handles directory vs file, bin subdirectory, etc.)
            var result = await _resolver.ResolveAsync(request.Path, forceRefresh: true, ct).ConfigureAwait(false);

            if (!result.Found || !result.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E316",
                    title = "Invalid FFmpeg",
                    status = 400,
                    detail = result.Error ?? "The specified path does not contain a valid FFmpeg executable",
                    howToFix = new[]
                    {
                        "Ensure the path points to ffmpeg.exe (or ffmpeg on Unix)",
                        "Verify FFmpeg is properly installed and not corrupted",
                        "Try running 'ffmpeg -version' manually to test",
                        "Download a fresh copy of FFmpeg if needed"
                    },
                    correlationId
                });
            }

            _logger.LogInformation("[{CorrelationId}] FFmpeg validated at: {Path}",
                correlationId, result.Path);

            return Ok(new
            {
                success = true,
                message = $"FFmpeg validated successfully at {result.Path}",
                installed = true,
                valid = true,
                path = result.Path,
                version = result.Version,
                source = "Configured",
                correlationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Error validating existing FFmpeg", correlationId);

            return StatusCode(500, new
            {
                success = false,
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E317",
                title = "Validation Error",
                status = 500,
                detail = $"Unexpected error validating FFmpeg: {ex.Message}",
                correlationId
            });
        }
    }
}

/// <summary>
/// Request model for FFmpeg installation
/// </summary>
public record FFmpegInstallRequest(string? Version);

/// <summary>
/// Request model for using existing FFmpeg installation
/// </summary>
public record UseExistingFFmpegRequest
{
    public string Path { get; init; } = "";
}
