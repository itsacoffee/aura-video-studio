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

            var manifest = await _manifestLoader.LoadManifestAsync().ConfigureAwait(false);
            var ffmpegEngine = manifest.Engines.FirstOrDefault(e => e.Id == "ffmpeg");

            if (ffmpegEngine == null)
            {
                return NotFound(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E311",
                    title = "FFmpeg Not Found in Manifest",
                    status = 404,
                    detail = "FFmpeg not found in engine manifest",
                    howToFix = new[]
                    {
                        "Check that the engine manifest is properly configured",
                        "Contact support if the issue persists"
                    },
                    correlationId
                });
            }

            var mirrors = new List<string>();

            if (ffmpegEngine.Urls.ContainsKey("windows"))
            {
                mirrors.Add(ffmpegEngine.Urls["windows"]);
            }

            if (ffmpegEngine.Mirrors != null && ffmpegEngine.Mirrors.ContainsKey("windows"))
            {
                mirrors.AddRange(ffmpegEngine.Mirrors["windows"]);
            }

            mirrors.Add("https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip");

            if (mirrors.Count == 0)
            {
                return BadRequest(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E312",
                    title = "No Download Mirrors Available",
                    status = 400,
                    detail = "No download mirrors available for FFmpeg",
                    howToFix = new[]
                    {
                        "Check your internet connection",
                        "Download FFmpeg manually and use the 'Use Existing FFmpeg' option"
                    },
                    correlationId
                });
            }

            _logger.LogInformation("[{CorrelationId}] Installing FFmpeg from {Count} mirrors", correlationId, mirrors.Count);

            var installResult = await _installer.InstallFromMirrorsAsync(
                mirrors.ToArray(),
                version,
                null,
                null,
                ct).ConfigureAwait(false);

            if (!installResult.Success)
            {
                var errorCode = ClassifyInstallationError(installResult.ErrorMessage);
                var howToFix = GetInstallationHowToFix(errorCode);

                _logger.LogWarning("[{CorrelationId}] FFmpeg installation failed: {Error}", 
                    correlationId, installResult.ErrorMessage);

                return Ok(new
                {
                    success = false,
                    message = installResult.ErrorMessage ?? "Failed to install FFmpeg",
                    type = $"https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/ffmpeg-errors.md#{errorCode}",
                    title = "FFmpeg Installation Failed",
                    status = 200,
                    detail = GenerateUserFriendlyInstallError(errorCode, installResult.ErrorMessage),
                    errorCode,
                    howToFix,
                    correlationId
                });
            }

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
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[{CorrelationId}] FFmpeg installation cancelled", correlationId);
            
            return Ok(new
            {
                success = false,
                message = "Installation was cancelled",
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E998",
                title = "Installation Cancelled",
                status = 200,
                detail = "The installation was cancelled by the user or timed out",
                errorCode = "E998",
                howToFix = new[] { "Try the installation again if it was not intentionally cancelled" },
                correlationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Error installing FFmpeg", correlationId);

            var errorMessage = ClassifyNetworkException(ex);

            return Ok(new
            {
                success = false,
                message = errorMessage.message,
                type = $"https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/ffmpeg-errors.md#{errorMessage.code}",
                title = errorMessage.title,
                status = 200,
                detail = errorMessage.detail,
                errorCode = errorMessage.code,
                howToFix = errorMessage.howToFix,
                correlationId
            });
        }
    }

    private string ClassifyInstallationError(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
        {
            return "E313";
        }

        if (errorMessage.Contains("404") || errorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return "E311";
        }

        if (errorMessage.Contains("timeout", StringComparison.OrdinalIgnoreCase))
        {
            return "E320";
        }

        if (errorMessage.Contains("network", StringComparison.OrdinalIgnoreCase) || 
            errorMessage.Contains("connection", StringComparison.OrdinalIgnoreCase))
        {
            return "E321";
        }

        if (errorMessage.Contains("checksum", StringComparison.OrdinalIgnoreCase) || 
            errorMessage.Contains("corrupt", StringComparison.OrdinalIgnoreCase))
        {
            return "E322";
        }

        if (errorMessage.Contains("validation", StringComparison.OrdinalIgnoreCase))
        {
            return "E303";
        }

        return "E313";
    }

    private string GenerateUserFriendlyInstallError(string errorCode, string? technicalMessage)
    {
        return errorCode switch
        {
            "E311" => "FFmpeg download source not found. The download URL may have changed.",
            "E320" => "Download timed out. This may be due to slow network connection or large file size.",
            "E321" => "Network error occurred during download. Check your internet connection.",
            "E322" => "Downloaded file is corrupted. The download was incomplete or the file was tampered with.",
            "E303" => "FFmpeg binary validation failed. The downloaded file may be incompatible with your system.",
            _ => technicalMessage ?? "Installation failed due to an unknown error."
        };
    }

    private string[] GetInstallationHowToFix(string errorCode)
    {
        return errorCode switch
        {
            "E311" => new[]
            {
                "Try the installation again - the mirror list may resolve to a working source",
                "Download FFmpeg manually from https://ffmpeg.org",
                "Use the 'Use Existing FFmpeg' option to point to a manual installation"
            },
            "E320" => new[]
            {
                "Check your internet connection speed",
                "Try again later when network conditions improve",
                "Use a wired connection instead of WiFi if possible",
                "Download FFmpeg manually and use 'Use Existing FFmpeg'"
            },
            "E321" => new[]
            {
                "Check your internet connection",
                "Verify firewall is not blocking the download",
                "Try using a VPN if downloads are restricted in your region",
                "Download FFmpeg manually and use the 'Use Existing FFmpeg' option"
            },
            "E322" => new[]
            {
                "Clear browser cache and try again",
                "Check available disk space",
                "Temporarily disable antivirus during download",
                "Download FFmpeg manually from the official website"
            },
            "E303" => new[]
            {
                "Ensure you have the correct FFmpeg version for your OS",
                "Check that your system architecture (x64/ARM) is supported",
                "Download the correct FFmpeg build manually"
            },
            _ => new[]
            {
                "Check your internet connection",
                "Ensure you have sufficient disk space",
                "Try restarting the application",
                "Check antivirus software is not blocking the installer"
            }
        };
    }

    private (string code, string message, string title, string detail, string[] howToFix) ClassifyNetworkException(Exception ex)
    {
        if (ex is HttpRequestException httpEx)
        {
            if (httpEx.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return ("E311", "FFmpeg source not found", "Download Source Not Found", 
                    "The FFmpeg download URL returned 404 Not Found", 
                    GetInstallationHowToFix("E311"));
            }

            if (httpEx.StatusCode != null && (int)httpEx.StatusCode >= 500)
            {
                return ("E321", "Server error during download", "Server Error", 
                    "The download server returned an error. This is a temporary issue.", 
                    new[] { "Try again in a few minutes", "The download mirror may be temporarily unavailable" });
            }

            if (httpEx.InnerException?.Message.Contains("DNS", StringComparison.OrdinalIgnoreCase) == true)
            {
                return ("E323", "DNS resolution failed", "DNS Error", 
                    "Unable to resolve the download server hostname. Check your DNS settings.", 
                    new[] { "Check your internet connection", "Try using a different DNS server (e.g., 8.8.8.8)", "Try again later" });
            }

            if (httpEx.InnerException?.Message.Contains("SSL", StringComparison.OrdinalIgnoreCase) == true ||
                httpEx.InnerException?.Message.Contains("TLS", StringComparison.OrdinalIgnoreCase) == true)
            {
                return ("E324", "Secure connection failed", "TLS/SSL Error", 
                    "Failed to establish a secure connection to the download server.", 
                    new[] { "Check your system date and time are correct", "Update your operating system", "Check firewall settings" });
            }

            return ("E321", "Network error during download", "Network Error", 
                $"Network error: {httpEx.Message}", GetInstallationHowToFix("E321"));
        }

        if (ex is TaskCanceledException)
        {
            return ("E320", "Download timed out", "Timeout", 
                "The download operation timed out.", GetInstallationHowToFix("E320"));
        }

        if (ex is IOException)
        {
            return ("E325", "Disk I/O error", "Disk Error", 
                "Failed to write to disk during installation.", 
                new[] { "Check available disk space", "Ensure the installation directory is writable", "Close other applications that might lock files" });
        }

        return ("E313", $"Installation error: {ex.Message}", "Installation Failed", 
            ex.Message, GetInstallationHowToFix("E313"));
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
