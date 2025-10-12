using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Aura.Core.Downloads;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

[ApiController]
[Route("api/downloads")]
public class DownloadsController : ControllerBase
{
    private readonly ILogger<DownloadsController> _logger;
    private readonly FfmpegInstaller _ffmpegInstaller;
    private readonly EngineManifestLoader _manifestLoader;
    private readonly string _logsDirectory;
    
    public DownloadsController(
        ILogger<DownloadsController> logger,
        FfmpegInstaller ffmpegInstaller,
        EngineManifestLoader manifestLoader)
    {
        _logger = logger;
        _ffmpegInstaller = ffmpegInstaller;
        _manifestLoader = manifestLoader;
        
        _logsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Aura", "Logs", "Tools");
        Directory.CreateDirectory(_logsDirectory);
    }
    
    /// <summary>
    /// Install FFmpeg from managed sources or attach existing installation
    /// </summary>
    [HttpPost("ffmpeg/install")]
    public async Task<IActionResult> InstallFFmpeg(
        [FromBody] FfmpegInstallRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("FFmpeg install request: Mode={Mode}", request.Mode);
            
            var correlationId = HttpContext.TraceIdentifier;
            
            // Create install log
            var logPath = Path.Combine(_logsDirectory, $"ffmpeg-install-{DateTime.UtcNow:yyyyMMddHHmmss}.log");
            await System.IO.File.WriteAllTextAsync(logPath, 
                $"[{DateTime.UtcNow:O}] Starting FFmpeg installation - Mode: {request.Mode}, CorrelationId: {correlationId}\n", ct);
            
            FfmpegInstallResult result;
            
            switch (request.Mode?.ToLower())
            {
                case "attach":
                    if (string.IsNullOrEmpty(request.AttachPath))
                    {
                        return BadRequest(new { error = "AttachPath is required for Attach mode" });
                    }
                    
                    await System.IO.File.AppendAllTextAsync(logPath, 
                        $"[{DateTime.UtcNow:O}] Attaching existing FFmpeg: {request.AttachPath}\n", ct);
                    
                    result = await _ffmpegInstaller.AttachExistingAsync(request.AttachPath, ct);
                    break;
                    
                case "local":
                    if (string.IsNullOrEmpty(request.LocalArchivePath))
                    {
                        return BadRequest(new { error = "LocalArchivePath is required for Local mode" });
                    }
                    
                    await System.IO.File.AppendAllTextAsync(logPath, 
                        $"[{DateTime.UtcNow:O}] Installing from local archive: {request.LocalArchivePath}\n", ct);
                    
                    result = await _ffmpegInstaller.InstallFromLocalArchiveAsync(
                        request.LocalArchivePath,
                        request.Version ?? "6.0",
                        null,
                        null,
                        ct);
                    break;
                    
                case "managed":
                default:
                    // Get mirrors from manifest
                    var manifest = await _manifestLoader.LoadManifestAsync();
                    var ffmpegEngine = manifest.Engines.FirstOrDefault(e => e.Id == "ffmpeg");
                    
                    if (ffmpegEngine == null)
                    {
                        return NotFound(new { error = "FFmpeg not found in engine manifest" });
                    }
                    
                    var mirrors = new List<string>();
                    
                    // Use custom URL if provided
                    if (!string.IsNullOrEmpty(request.CustomUrl))
                    {
                        mirrors.Add(request.CustomUrl);
                        await System.IO.File.AppendAllTextAsync(logPath, 
                            $"[{DateTime.UtcNow:O}] Using custom URL: {request.CustomUrl}\n", ct);
                    }
                    
                    // Add primary URL
                    if (ffmpegEngine.Urls.ContainsKey("windows"))
                    {
                        mirrors.Add(ffmpegEngine.Urls["windows"]);
                    }
                    
                    // Add mirrors
                    if (ffmpegEngine.Mirrors != null && ffmpegEngine.Mirrors.ContainsKey("windows"))
                    {
                        mirrors.AddRange(ffmpegEngine.Mirrors["windows"]);
                    }
                    
                    // Fallback mirrors
                    mirrors.Add("https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip");
                    
                    await System.IO.File.AppendAllTextAsync(logPath, 
                        $"[{DateTime.UtcNow:O}] Attempting installation from {mirrors.Count} mirrors\n", ct);
                    
                    foreach (var mirror in mirrors)
                    {
                        await System.IO.File.AppendAllTextAsync(logPath, 
                            $"[{DateTime.UtcNow:O}] Mirror: {mirror}\n", ct);
                    }
                    
                    result = await _ffmpegInstaller.InstallFromMirrorsAsync(
                        mirrors.ToArray(),
                        request.Version ?? ffmpegEngine.Version,
                        null, // sha256 - skip for dynamic "latest" builds
                        null, // progress - could be enhanced with SignalR
                        ct);
                    break;
            }
            
            await System.IO.File.AppendAllTextAsync(logPath, 
                $"[{DateTime.UtcNow:O}] Installation result: Success={result.Success}, Error={result.ErrorMessage}\n", ct);
            
            if (result.Success)
            {
                await System.IO.File.AppendAllTextAsync(logPath, 
                    $"[{DateTime.UtcNow:O}] FFmpeg installed: {result.FfmpegPath}\n", ct);
                await System.IO.File.AppendAllTextAsync(logPath, 
                    $"[{DateTime.UtcNow:O}] Validation output:\n{result.ValidationOutput}\n", ct);
                
                return Ok(new
                {
                    success = true,
                    installPath = result.InstallPath,
                    ffmpegPath = result.FfmpegPath,
                    ffprobePath = result.FfprobePath,
                    validationOutput = result.ValidationOutput,
                    sourceType = result.SourceType.ToString(),
                    installedAt = result.InstalledAt,
                    logPath
                });
            }
            else
            {
                await System.IO.File.AppendAllTextAsync(logPath, 
                    $"[{DateTime.UtcNow:O}] Installation failed: {result.ErrorMessage}\n", ct);
                
                return BadRequest(new
                {
                    success = false,
                    error = result.ErrorMessage,
                    code = "E302-FFMPEG_INSTALL_FAILED",
                    correlationId,
                    howToFix = new[]
                    {
                        "Try using a different mirror or custom URL",
                        "Download FFmpeg manually and use 'Attach Existing' mode",
                        "Check network connectivity and firewall settings",
                        "Review install log for details"
                    },
                    logPath
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FFmpeg installation failed");
            
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                code = "E302-FFMPEG_INSTALL_ERROR",
                correlationId = HttpContext.TraceIdentifier,
                howToFix = new[]
                {
                    "Check system permissions",
                    "Ensure sufficient disk space",
                    "Try 'Attach Existing' mode with a manual FFmpeg installation"
                }
            });
        }
    }
    
    /// <summary>
    /// Get FFmpeg installation status
    /// </summary>
    [HttpGet("ffmpeg/status")]
    public async Task<IActionResult> GetFFmpegStatus()
    {
        try
        {
            // Look for FFmpeg in standard locations
            var toolsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Aura", "Tools", "ffmpeg");
            
            if (!Directory.Exists(toolsDir))
            {
                return Ok(new
                {
                    state = "NotInstalled",
                    installPath = (string?)null,
                    ffmpegPath = (string?)null,
                    lastError = (string?)null
                });
            }
            
            // Find latest version directory
            var versionDirs = Directory.GetDirectories(toolsDir);
            if (versionDirs.Length == 0)
            {
                return Ok(new
                {
                    state = "NotInstalled",
                    installPath = (string?)null,
                    ffmpegPath = (string?)null,
                    lastError = (string?)null
                });
            }
            
            // Get metadata from latest version
            var latestVersionDir = versionDirs.OrderByDescending(d => d).First();
            var metadata = await _ffmpegInstaller.GetInstallMetadataAsync(latestVersionDir);
            
            if (metadata == null)
            {
                return Ok(new
                {
                    state = "PartiallyFailed",
                    installPath = latestVersionDir,
                    ffmpegPath = (string?)null,
                    lastError = "Metadata not found"
                });
            }
            
            // Verify binary still exists
            if (!System.IO.File.Exists(metadata.FfmpegPath))
            {
                return Ok(new
                {
                    state = "PartiallyFailed",
                    installPath = metadata.InstallPath,
                    ffmpegPath = metadata.FfmpegPath,
                    lastError = "FFmpeg binary not found at recorded path"
                });
            }
            
            var state = metadata.SourceType == "AttachExisting" ? "ExternalAttached" : "Installed";
            
            return Ok(new
            {
                state,
                installPath = metadata.InstallPath,
                ffmpegPath = metadata.FfmpegPath,
                ffprobePath = metadata.FfprobePath,
                version = metadata.Version,
                sourceType = metadata.SourceType,
                installedAt = metadata.InstalledAt,
                validated = metadata.Validated,
                validationOutput = metadata.ValidationOutput,
                lastError = (string?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get FFmpeg status");
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// Repair FFmpeg installation
    /// </summary>
    [HttpPost("ffmpeg/repair")]
    public async Task<IActionResult> RepairFFmpeg(CancellationToken ct)
    {
        try
        {
            // Get current installation
            var statusResult = await GetFFmpegStatus();
            if (statusResult is not OkObjectResult okResult)
            {
                return BadRequest(new { error = "Could not determine FFmpeg status" });
            }
            
            // For now, repair means reinstall
            // In the future, could be smarter about just re-validating or fixing specific issues
            var manifest = await _manifestLoader.LoadManifestAsync();
            var ffmpegEngine = manifest.Engines.FirstOrDefault(e => e.Id == "ffmpeg");
            
            if (ffmpegEngine == null)
            {
                return NotFound(new { error = "FFmpeg not found in engine manifest" });
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
            
            var result = await _ffmpegInstaller.InstallFromMirrorsAsync(
                mirrors.ToArray(),
                ffmpegEngine.Version,
                null,
                null,
                ct);
            
            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = "FFmpeg repaired successfully",
                    ffmpegPath = result.FfmpegPath,
                    validationOutput = result.ValidationOutput
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    error = result.ErrorMessage,
                    code = "E302-FFMPEG_REPAIR_FAILED"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to repair FFmpeg");
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// Get FFmpeg install log
    /// </summary>
    [HttpGet("ffmpeg/install-log")]
    public IActionResult GetFFmpegInstallLog([FromQuery] int lines = 100)
    {
        try
        {
            // Get latest log file
            if (!Directory.Exists(_logsDirectory))
            {
                return Ok(new { log = "No logs found" });
            }
            
            var logFiles = Directory.GetFiles(_logsDirectory, "ffmpeg-install-*.log")
                .OrderByDescending(f => f)
                .ToList();
            
            if (logFiles.Count == 0)
            {
                return Ok(new { log = "No install logs found" });
            }
            
            var latestLog = logFiles[0];
            var allLines = System.IO.File.ReadAllLines(latestLog);
            var tailLines = allLines.TakeLast(lines).ToArray();
            
            return Ok(new
            {
                log = string.Join("\n", tailLines),
                logPath = latestLog,
                totalLines = allLines.Length
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read install log");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class FfmpegInstallRequest
{
    public string? Mode { get; set; } // "managed", "local", "attach"
    public string? CustomUrl { get; set; }
    public string? LocalArchivePath { get; set; }
    public string? AttachPath { get; set; }
    public string? Version { get; set; }
}
