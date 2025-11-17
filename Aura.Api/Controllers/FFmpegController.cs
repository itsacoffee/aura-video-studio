using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Configuration;
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
    private readonly FFmpegConfigurationStore _configStore;

    public FFmpegController(
        ILogger<FFmpegController> logger,
        FFmpegResolver resolver,
        FfmpegInstaller installer,
        EngineManifestLoader manifestLoader,
        FFmpegConfigurationStore configStore)
    {
        _logger = logger;
        _resolver = resolver;
        _installer = installer;
        _manifestLoader = manifestLoader;
        _configStore = configStore;
    }

    /// <summary>
    /// Get current FFmpeg status including installation and version info
    /// Enhanced with mode and validation details
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            _logger.LogInformation("[{CorrelationId}] GET /api/ffmpeg/status", correlationId);

            // Load persisted configuration
            var config = await _configStore.LoadAsync(ct).ConfigureAwait(false);
            
            // Resolve current FFmpeg (re-validate if stale or not set)
            var result = await _resolver.ResolveAsync(config?.Path, forceRefresh: config?.IsValidationStale ?? true, ct).ConfigureAwait(false);
            
            // Determine mode based on source
            var mode = DetermineMode(result.Source, config?.Mode ?? FFmpegMode.None);
            
            // Update configuration if resolution succeeded
            if (result.Found && result.IsValid)
            {
                await _resolver.PersistConfigurationAsync(result, mode, ct).ConfigureAwait(false);
            }

            string? lastValidationResult = null;
            if (config != null)
            {
                lastValidationResult = config.LastValidationResult.ToString().ToLowerInvariant();
            }

            var response = new FFmpegStatusResponse(
                Installed: result.Found && result.IsValid,
                Valid: result.IsValid,
                Source: result.Source,
                Version: result.Version,
                Path: result.Path,
                Mode: mode.ToString().ToLowerInvariant(),
                Error: result.Error,
                ErrorCode: null,
                ErrorMessage: result.Error,
                LastValidatedAt: config?.LastValidatedAt,
                LastValidationResult: lastValidationResult,
                CorrelationId: correlationId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Error getting FFmpeg status", correlationId);

            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E340",
                title = "FFmpeg Status Check Failed",
                status = 500,
                detail = $"Failed to get FFmpeg status: {ex.Message}",
                errorCode = "E340",
                correlationId
            });
        }
    }
    
    /// <summary>
    /// Force re-detection and validation of FFmpeg
    /// </summary>
    [HttpPost("detect")]
    public async Task<IActionResult> Detect(CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            _logger.LogInformation("[{CorrelationId}] POST /api/ffmpeg/detect", correlationId);

            // Invalidate cache to force fresh detection
            _resolver.InvalidateCache();
            
            // Perform resolution without configured path (fresh detection)
            var result = await _resolver.ResolveAsync(null, forceRefresh: true, ct).ConfigureAwait(false);
            
            // Determine mode
            var mode = DetermineMode(result.Source, FFmpegMode.None);
            
            // Persist result
            await _resolver.PersistConfigurationAsync(result, mode, ct).ConfigureAwait(false);

            if (result.Found && result.IsValid)
            {
                _logger.LogInformation(
                    "[{CorrelationId}] FFmpeg detected: {Path} (Mode: {Mode})",
                    correlationId,
                    result.Path,
                    mode
                );
                
                var successResponse = new FFmpegDetectResponse(
                    Success: true,
                    Installed: true,
                    Valid: true,
                    Version: result.Version,
                    Path: result.Path,
                    Source: result.Source,
                    Mode: mode.ToString().ToLowerInvariant(),
                    Message: $"FFmpeg detected at {result.Path}",
                    AttemptedPaths: result.AttemptedPaths,
                    Detail: null,
                    HowToFix: null,
                    CorrelationId: correlationId);

                return Ok(successResponse);
            }

            _logger.LogWarning(
                "[{CorrelationId}] FFmpeg not detected: {Error}",
                correlationId,
                result.Error
            );
            
            var failureResponse = new FFmpegDetectResponse(
                Success: false,
                Installed: false,
                Valid: false,
                Version: null,
                Path: null,
                Source: result.Source,
                Mode: "none",
                Message: result.Error ?? "FFmpeg not found on this system",
                AttemptedPaths: result.AttemptedPaths,
                Detail: "FFmpeg was not found. You can install it automatically or specify a custom path.",
                HowToFix: new[]
                {
                    "Click 'Install FFmpeg' to download and install automatically",
                    "Or manually install FFmpeg and use 'Re-scan' to detect it",
                    "Or use 'Browse for FFmpeg' to specify a custom installation path"
                },
                CorrelationId: correlationId);

            return Ok(failureResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Error detecting FFmpeg", correlationId);

            return StatusCode(500, new
            {
                success = false,
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E344",
                title = "Detection Error",
                status = 500,
                detail = $"Failed to detect FFmpeg: {ex.Message}",
                errorCode = "E344",
                correlationId
            });
        }
    }
    
    /// <summary>
    /// Set and validate a custom FFmpeg path
    /// </summary>
    [HttpPost("set-path")]
    public async Task<IActionResult> SetPath(
        [FromBody] SetPathRequest request,
        CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            _logger.LogInformation(
                "[{CorrelationId}] POST /api/ffmpeg/set-path - Path: {Path}",
                correlationId,
                request.Path
            );

            if (string.IsNullOrWhiteSpace(request.Path))
            {
                var errorResponse = new FFmpegPathValidationResponse(
                    Success: false,
                    Message: "FFmpeg path is required",
                    Installed: false,
                    Valid: false,
                    Path: null,
                    Version: null,
                    Source: "None",
                    Mode: "none",
                    CorrelationId: correlationId,
                    Title: "Invalid Path",
                    Detail: "FFmpeg path is required",
                    ErrorCode: "E345");

                return BadRequest(errorResponse);
            }

            // Validate the custom path
            var result = await _resolver.ResolveAsync(request.Path, forceRefresh: true, ct).ConfigureAwait(false);

            if (!result.Found || !result.IsValid)
            {
                _logger.LogWarning(
                    "[{CorrelationId}] Invalid custom FFmpeg path: {Path} - {Error}",
                    correlationId,
                    request.Path,
                    result.Error
                );
                
                var invalidResponse = new FFmpegPathValidationResponse(
                    Success: false,
                    Message: result.Error ?? "The specified path does not contain a valid FFmpeg executable",
                    Installed: false,
                    Valid: false,
                    Path: result.Path,
                    Version: result.Version,
                    Source: "Configured",
                    Mode: "custom",
                    CorrelationId: correlationId,
                    Title: "Invalid FFmpeg Path",
                    Detail: result.Error ?? "The specified path does not contain a valid FFmpeg executable",
                    ErrorCode: "E346",
                    HowToFix: new[]
                    {
                        "Ensure the path points to ffmpeg.exe (Windows) or ffmpeg (Unix)",
                        "Verify FFmpeg is properly installed and not corrupted",
                        "Try running 'ffmpeg -version' manually to test the executable",
                        "Download a fresh copy of FFmpeg if the binary is damaged"
                    },
                    AttemptedPaths: result.AttemptedPaths);

                return BadRequest(invalidResponse);
            }

            // Persist custom configuration
            await _resolver.PersistConfigurationAsync(result, FFmpegMode.Custom, ct).ConfigureAwait(false);

            _logger.LogInformation(
                "[{CorrelationId}] Custom FFmpeg path validated and saved: {Path}",
                correlationId,
                result.Path
            );

            var successResponse = new FFmpegPathValidationResponse(
                Success: true,
                Message: $"FFmpeg validated successfully at {result.Path}",
                Installed: true,
                Valid: true,
                Path: result.Path,
                Version: result.Version,
                Source: "Configured",
                Mode: "custom",
                CorrelationId: correlationId);

            return Ok(successResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Error setting custom FFmpeg path", correlationId);

            return StatusCode(500, new
            {
                success = false,
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E347",
                title = "Path Validation Error",
                status = 500,
                detail = $"Unexpected error validating FFmpeg: {ex.Message}",
                errorCode = "E347",
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
                var notFoundResponse = new FFmpegInstallErrorResponse(
                    Success: false,
                    Message: "FFmpeg not found in engine manifest",
                    Title: "FFmpeg Not Found in Manifest",
                    Detail: "FFmpeg not found in engine manifest",
                    ErrorCode: "E341",
                    Type: "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E341",
                    HowToFix: new[]
                    {
                        "Check that the engine manifest is properly configured",
                        "Contact support if the issue persists"
                    },
                    CorrelationId: correlationId);

                return NotFound(notFoundResponse);
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
                var mirrorsResponse = new FFmpegInstallErrorResponse(
                    Success: false,
                    Message: "No download mirrors available for FFmpeg",
                    Title: "No Download Mirrors Available",
                    Detail: "No download mirrors available for FFmpeg",
                    ErrorCode: "E342",
                    Type: "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E342",
                    HowToFix: new[]
                    {
                        "Check your internet connection",
                        "Download FFmpeg manually and use the 'Use Existing FFmpeg' option"
                    },
                    CorrelationId: correlationId);

                return BadRequest(mirrorsResponse);
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

                var installErrorResponse = new FFmpegInstallErrorResponse(
                    Success: false,
                    Message: installResult.ErrorMessage ?? "Failed to install FFmpeg",
                    Title: "FFmpeg Installation Failed",
                    Detail: GenerateUserFriendlyInstallError(errorCode, installResult.ErrorMessage),
                    ErrorCode: errorCode,
                    Type: $"https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/ffmpeg-errors.md#{errorCode}",
                    HowToFix: howToFix,
                    CorrelationId: correlationId);

                return Ok(installErrorResponse);
            }

            _resolver.InvalidateCache();
            
            // Persist configuration after successful install
            if (installResult.FfmpegPath != null)
            {
                var persistResult = new FfmpegResolutionResult
                {
                    Found = true,
                    IsValid = true,
                    Path = installResult.FfmpegPath,
                    Version = installResult.ValidationOutput ?? version,
                    Source = "Managed",
                    ValidationOutput = installResult.ValidationOutput,
                    AttemptedPaths = new List<string> { installResult.FfmpegPath }
                };
                
                await _resolver.PersistConfigurationAsync(persistResult, FFmpegMode.Local, ct).ConfigureAwait(false);
            }

            _logger.LogInformation("[{CorrelationId}] FFmpeg installed successfully: {Path}",
                correlationId, installResult.FfmpegPath);

            var successResponse = new FFmpegInstallSuccessResponse(
                Success: true,
                Message: "FFmpeg installed successfully",
                Path: installResult.FfmpegPath ?? string.Empty,
                Version: installResult.ValidationOutput,
                InstalledAt: installResult.InstalledAt,
                Mode: "local",
                CorrelationId: correlationId);

            return Ok(successResponse);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[{CorrelationId}] FFmpeg installation cancelled", correlationId);
            
            var cancelledResponse = new FFmpegInstallErrorResponse(
                Success: false,
                Message: "Installation was cancelled",
                Title: "Installation Cancelled",
                Detail: "The installation was cancelled by the user or timed out",
                ErrorCode: "E998",
                Type: "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E998",
                HowToFix: new[] { "Try the installation again if it was not intentionally cancelled" },
                CorrelationId: correlationId);

            return Ok(cancelledResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Error installing FFmpeg", correlationId);

            var errorMessage = ClassifyNetworkException(ex);

            var unexpectedResponse = new FFmpegInstallErrorResponse(
                Success: false,
                Message: errorMessage.message,
                Title: errorMessage.title,
                Detail: errorMessage.detail,
                ErrorCode: errorMessage.code,
                Type: $"https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/ffmpeg-errors.md#{errorMessage.code}",
                HowToFix: errorMessage.howToFix,
                CorrelationId: correlationId);

            return Ok(unexpectedResponse);
        }
    }

    private string ClassifyInstallationError(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
        {
            return "E343";
        }

        if (errorMessage.Contains("404") || errorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return "E341";
        }

        if (errorMessage.Contains("timeout", StringComparison.OrdinalIgnoreCase))
        {
            return "E348";
        }

        if (errorMessage.Contains("network", StringComparison.OrdinalIgnoreCase) || 
            errorMessage.Contains("connection", StringComparison.OrdinalIgnoreCase))
        {
            return "E349";
        }

        if (errorMessage.Contains("checksum", StringComparison.OrdinalIgnoreCase) || 
            errorMessage.Contains("corrupt", StringComparison.OrdinalIgnoreCase))
        {
            return "E350";
        }

        if (errorMessage.Contains("validation", StringComparison.OrdinalIgnoreCase))
        {
            return "E347";
        }

        return "E343";
    }

    private string GenerateUserFriendlyInstallError(string errorCode, string? technicalMessage)
    {
        return errorCode switch
        {
            "E341" => "FFmpeg download source not found. The download URL may have changed.",
            "E348" => "Download timed out. This may be due to slow network connection or large file size.",
            "E349" => "Network error occurred during download. Check your internet connection.",
            "E350" => "Downloaded file is corrupted. The download was incomplete or the file was tampered with.",
            "E347" => "FFmpeg binary validation failed. The downloaded file may be incompatible with your system.",
            _ => technicalMessage ?? "Installation failed due to an unknown error."
        };
    }

    private string[] GetInstallationHowToFix(string errorCode)
    {
        return errorCode switch
        {
            "E341" => new[]
            {
                "Try the installation again - the mirror list may resolve to a working source",
                "Download FFmpeg manually from https://ffmpeg.org",
                "Use the 'Use Existing FFmpeg' option to point to a manual installation"
            },
            "E348" => new[]
            {
                "Check your internet connection speed",
                "Try again later when network conditions improve",
                "Use a wired connection instead of WiFi if possible",
                "Download FFmpeg manually and use 'Use Existing FFmpeg'"
            },
            "E349" => new[]
            {
                "Check your internet connection",
                "Verify firewall is not blocking the download",
                "Try using a VPN if downloads are restricted in your region",
                "Download FFmpeg manually and use the 'Use Existing FFmpeg' option"
            },
            "E350" => new[]
            {
                "Clear browser cache and try again",
                "Check available disk space",
                "Temporarily disable antivirus during download",
                "Download FFmpeg manually from the official website"
            },
            "E347" => new[]
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
                return ("E341", "FFmpeg source not found", "Download Source Not Found", 
                    "The FFmpeg download URL returned 404 Not Found", 
                    GetInstallationHowToFix("E341"));
            }

            if (httpEx.StatusCode != null && (int)httpEx.StatusCode >= 500)
            {
                return ("E349", "Server error during download", "Server Error", 
                    "The download server returned an error. This is a temporary issue.", 
                    new[] { "Try again in a few minutes", "The download mirror may be temporarily unavailable" });
            }

            if (httpEx.InnerException?.Message.Contains("DNS", StringComparison.OrdinalIgnoreCase) == true)
            {
                return ("E351", "DNS resolution failed", "DNS Error", 
                    "Unable to resolve the download server hostname. Check your DNS settings.", 
                    new[] { "Check your internet connection", "Try using a different DNS server (e.g., 8.8.8.8)", "Try again later" });
            }

            if (httpEx.InnerException?.Message.Contains("SSL", StringComparison.OrdinalIgnoreCase) == true ||
                httpEx.InnerException?.Message.Contains("TLS", StringComparison.OrdinalIgnoreCase) == true)
            {
                return ("E352", "Secure connection failed", "TLS/SSL Error", 
                    "Failed to establish a secure connection to the download server.", 
                    new[] { "Check your system date and time are correct", "Update your operating system", "Check firewall settings" });
            }

            return ("E349", "Network error during download", "Network Error", 
                $"Network error: {httpEx.Message}", GetInstallationHowToFix("E349"));
        }

        if (ex is TaskCanceledException)
        {
            return ("E348", "Download timed out", "Timeout", 
                "The download operation timed out.", GetInstallationHowToFix("E348"));
        }

        if (ex is IOException)
        {
            return ("E353", "Disk I/O error", "Disk Error", 
                "Failed to write to disk during installation.", 
                new[] { "Check available disk space", "Ensure the installation directory is writable", "Close other applications that might lock files" });
        }

        return ("E343", $"Installation error: {ex.Message}", "Installation Failed", 
            ex.Message, GetInstallationHowToFix("E343"));
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

            // If FFmpeg was found, persist the configuration
            if (result.Found && result.IsValid)
            {
                var mode = DetermineMode(result.Source, FFmpegMode.System);
                await _resolver.PersistConfigurationAsync(result, mode, ct).ConfigureAwait(false);
            }

            var response = new FFmpegRescanResponse(
                Success: result.Found && result.IsValid,
                Installed: result.Found && result.IsValid,
                Version: result.Version,
                Path: result.Path,
                Source: result.Source,
                Valid: result.IsValid,
                Error: result.Error,
                Message: result.Found && result.IsValid
                    ? $"FFmpeg found at {result.Path}"
                    : "FFmpeg not found or invalid",
                CorrelationId: correlationId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Error rescanning for FFmpeg", correlationId);

            return StatusCode(500, new
            {
                success = false,
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E344",
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
                var errorResponse = new FFmpegPathValidationResponse(
                    Success: false,
                    Message: "FFmpeg path is required",
                    Installed: false,
                    Valid: false,
                    Path: null,
                    Version: null,
                    Source: "None",
                    Mode: "none",
                    CorrelationId: correlationId,
                    Title: "Invalid Path",
                    Detail: "FFmpeg path is required",
                    ErrorCode: "E345");

                return BadRequest(errorResponse);
            }

            // Resolve the path (handles directory vs file, bin subdirectory, etc.)
            var result = await _resolver.ResolveAsync(request.Path, forceRefresh: true, ct).ConfigureAwait(false);

            if (!result.Found || !result.IsValid)
            {
                var invalidResponse = new FFmpegPathValidationResponse(
                    Success: false,
                    Message: result.Error ?? "The specified path does not contain a valid FFmpeg executable",
                    Installed: false,
                    Valid: false,
                    Path: result.Path,
                    Version: result.Version,
                    Source: "Configured",
                    Mode: "custom",
                    CorrelationId: correlationId,
                    Title: "Invalid FFmpeg",
                    Detail: result.Error ?? "The specified path does not contain a valid FFmpeg executable",
                    ErrorCode: "E346",
                    HowToFix: new[]
                    {
                        "Ensure the path points to ffmpeg.exe (or ffmpeg on Unix)",
                        "Verify FFmpeg is properly installed and not corrupted",
                        "Try running 'ffmpeg -version' manually to test",
                        "Download a fresh copy of FFmpeg if needed"
                    },
                    AttemptedPaths: result.AttemptedPaths);

                return BadRequest(invalidResponse);
            }

            _logger.LogInformation("[{CorrelationId}] FFmpeg validated at: {Path}",
                correlationId, result.Path);

            // Persist the custom configuration so it's remembered
            await _resolver.PersistConfigurationAsync(result, FFmpegMode.Custom, ct).ConfigureAwait(false);

            var successResponse = new FFmpegPathValidationResponse(
                Success: true,
                Message: $"FFmpeg validated successfully at {result.Path}",
                Installed: true,
                Valid: true,
                Path: result.Path,
                Version: result.Version,
                Source: "Configured",
                Mode: "custom",
                CorrelationId: correlationId);

            return Ok(successResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Error validating existing FFmpeg", correlationId);

            return StatusCode(500, new
            {
                success = false,
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E347",
                title = "Validation Error",
                status = 500,
                detail = $"Unexpected error validating FFmpeg: {ex.Message}",
                correlationId
            });
        }
    }
    
    /// <summary>
    /// Helper method to determine FFmpeg mode from source
    /// </summary>
    private static FFmpegMode DetermineMode(string source, FFmpegMode fallback)
    {
        return source switch
        {
            "Environment" => FFmpegMode.System,
            "Managed" => FFmpegMode.Local,
            "Configured" => FFmpegMode.Custom,
            "PATH" => FFmpegMode.System,
            "Common Directory" => FFmpegMode.System,
            "None" => FFmpegMode.None,
            _ => fallback
        };
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

/// <summary>
/// Request model for setting custom FFmpeg path
/// </summary>
public record SetPathRequest
{
    public string Path { get; init; } = "";
}
