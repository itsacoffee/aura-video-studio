using Aura.Core.Dependencies;
using Aura.Core.Downloads;
using Aura.Core.Runtime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Api.Controllers;

[ApiController]
[Route("api/engines")]
public class EnginesController : ControllerBase
{
    private readonly ILogger<EnginesController> _logger;
    private readonly EngineManifestLoader _manifestLoader;
    private readonly EngineInstaller _installer;
    private readonly LocalEnginesRegistry _registry;
    private readonly ExternalProcessManager _processManager;
    private readonly EngineLifecycleManager _lifecycleManager;
    private readonly EngineDetector? _engineDetector;
    private readonly GitHubReleaseResolver? _releaseResolver;

    public EnginesController(
        ILogger<EnginesController> logger,
        EngineManifestLoader manifestLoader,
        EngineInstaller installer,
        LocalEnginesRegistry registry,
        ExternalProcessManager processManager,
        EngineLifecycleManager lifecycleManager,
        EngineDetector? engineDetector = null,
        GitHubReleaseResolver? releaseResolver = null)
    {
        _logger = logger;
        _manifestLoader = manifestLoader;
        _installer = installer;
        _registry = registry;
        _processManager = processManager;
        _lifecycleManager = lifecycleManager;
        _engineDetector = engineDetector;
        _releaseResolver = releaseResolver;
    }

    /// <summary>
    /// Get list of available engines from manifest
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> GetList()
    {
        try
        {
            var manifest = await _manifestLoader.LoadManifestAsync();
            
            // Detect GPU for gating info
            var hardwareDetector = new Aura.Core.Hardware.HardwareDetector(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<Aura.Core.Hardware.HardwareDetector>.Instance);
            var systemProfile = await hardwareDetector.DetectSystemAsync();
            var gpuInfo = systemProfile.Gpu;
            bool hasNvidia = gpuInfo?.Vendor?.ToUpperInvariant() == "NVIDIA";
            bool hasEnoughVram = hasNvidia && (gpuInfo?.VramGB ?? 0) >= 6;
            
            var engines = manifest.Engines.Select(e => 
            {
                bool requiresNvidia = e.RequiredVRAMGB > 0;
                bool meetsRequirements = !requiresNvidia || (hasNvidia && (gpuInfo?.VramGB ?? 0) >= (e.RequiredVRAMGB ?? 0));
                
                string? gatingReason = null;
                if (requiresNvidia && !hasNvidia)
                {
                    gatingReason = "Requires NVIDIA GPU";
                }
                else if (requiresNvidia && hasNvidia && !meetsRequirements)
                {
                    gatingReason = $"Requires {e.RequiredVRAMGB}GB VRAM (detected: {gpuInfo?.VramGB ?? 0}GB)";
                }
                
                return new
                {
                    e.Id,
                    e.Name,
                    e.Version,
                    e.Description,
                    e.SizeBytes,
                    e.DefaultPort,
                    e.LicenseUrl,
                    e.RequiredVRAMGB,
                    IsInstalled = _installer.IsInstalled(e.Id),
                    InstallPath = _installer.GetInstallPath(e.Id),
                    // Gating information - now allows install anyway
                    IsGated = requiresNvidia,
                    CanInstall = true, // Always allow installation for future use
                    CanAutoStart = meetsRequirements, // Only auto-start if requirements are met
                    GatingReason = gatingReason,
                    VramTooltip = e.VramTooltip,
                    Icon = e.Icon,
                    Tags = e.Tags
                };
            }).ToList();

            return Ok(new { engines, hardwareInfo = new { hasNvidia, vramGB = gpuInfo?.VramGB ?? 0 } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get engine list");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get status of a specific engine
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus([FromQuery] string engineId)
    {
        try
        {
            if (string.IsNullOrEmpty(engineId))
            {
                return BadRequest(new { error = "engineId is required" });
            }

            var manifest = await _manifestLoader.LoadManifestAsync();
            var engine = manifest.Engines.FirstOrDefault(e => e.Id == engineId);
            
            if (engine == null)
            {
                return NotFound(new { error = $"Engine {engineId} not found in manifest" });
            }

            bool isInstalled = _installer.IsInstalled(engineId);
            string? installedVersion = null;
            bool isRunning = false;
            int? port = null;
            bool isHealthy = false;
            var messages = new List<string>();
            int? processId = null;
            string? logsPath = null;

            var engineConfig = _registry.GetEngine(engineId);
            if (engineConfig != null)
            {
                installedVersion = engineConfig.Version;
                port = engineConfig.Port;
                
                var status = _processManager.GetStatus(engineId);
                isRunning = status.IsRunning;
                processId = status.ProcessId;
                isHealthy = status.HealthCheckPassed;

                if (isRunning)
                {
                    logsPath = _processManager.GetLogPath(engineId);
                }

                if (!string.IsNullOrEmpty(status.LastError))
                {
                    messages.Add(status.LastError);
                }
            }

            if (isInstalled && engineConfig == null)
            {
                messages.Add("Engine is installed but not registered. Please restart the application.");
            }

            return Ok(new
            {
                engineId,
                name = engine.Name,
                status = isRunning ? "running" : (isInstalled ? "installed" : "not_installed"),
                installedVersion,
                isRunning,
                port,
                health = isRunning ? (isHealthy ? "healthy" : "unreachable") : null,
                processId,
                logsPath,
                installPath = _installer.GetInstallPath(engineId),
                messages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get engine status for {EngineId}", engineId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Install an engine
    /// </summary>
    [HttpPost("install")]
    public async Task<IActionResult> Install([FromBody] InstallRequest request, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrEmpty(request.EngineId))
            {
                return BadRequest(new { error = "engineId is required" });
            }

            var manifest = await _manifestLoader.LoadManifestAsync();
            var engine = manifest.Engines.FirstOrDefault(e => e.Id == request.EngineId);
            
            if (engine == null)
            {
                return NotFound(new { error = $"Engine {request.EngineId} not found in manifest" });
            }

            if (_installer.IsInstalled(request.EngineId))
            {
                return BadRequest(new { error = $"Engine {request.EngineId} is already installed" });
            }

            // Install the engine with optional custom URL or local file
            string installPath = await _installer.InstallAsync(
                engine, 
                request.CustomUrl, 
                request.LocalFilePath, 
                null, 
                ct);

            // Register with the registry
            string executablePath = System.IO.Path.Combine(installPath, engine.Entrypoint);
            
            var engineConfig = new EngineConfig(
                Id: engine.Id,
                EngineId: engine.Id,
                Name: engine.Name,
                Version: request.Version ?? engine.Version,
                Mode: EngineMode.Managed,
                InstallPath: installPath,
                ExecutablePath: executablePath,
                Arguments: engine.ArgsTemplate,
                Port: request.Port ?? engine.DefaultPort,
                HealthCheckUrl: engine.HealthCheck != null ? $"http://localhost:{request.Port ?? engine.DefaultPort}{engine.HealthCheck.Url}" : null,
                StartOnAppLaunch: false,
                AutoRestart: false
            );

            await _registry.RegisterEngineAsync(engineConfig);

            return Ok(new
            {
                success = true,
                engineId = request.EngineId,
                installPath,
                message = $"Engine {engine.Name} installed successfully"
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Install operation cancelled for {EngineId}", request.EngineId);
            return StatusCode(499, new { error = "Installation cancelled by user" });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while installing {EngineId}", request.EngineId);
            return StatusCode(500, new { 
                error = "Network error during download. Check your internet connection and try again.",
                details = ex.Message 
            });
        }
        catch (DownloadException ex)
        {
            _logger.LogError(ex, "Download error ({ErrorCode}) while installing {EngineId}", ex.ErrorCode, request.EngineId);
            string errorMessage = ex.ErrorCode switch
            {
                DownloadErrorCodes.E_DL_404 => "File not found (404). All mirrors failed. Try a custom URL or local file.",
                DownloadErrorCodes.E_DL_TIMEOUT => "Download timeout. Check your internet connection.",
                DownloadErrorCodes.E_DL_CHECKSUM => "Checksum verification failed. The downloaded file is corrupted.",
                _ => "Download failed. See details for more information."
            };
            
            return StatusCode(500, new { 
                error = errorMessage,
                errorCode = ex.ErrorCode,
                url = ex.Url,
                details = ex.Message 
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Permission error while installing {EngineId}", request.EngineId);
            return StatusCode(500, new { 
                error = "Permission denied. Cannot write to installation directory. Check folder permissions.",
                details = ex.Message 
            });
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "File system error while installing {EngineId}", request.EngineId);
            string errorMsg = ex.Message.Contains("not enough space") || ex.Message.Contains("disk full")
                ? "Not enough disk space. Free up space and try again."
                : "File system error during installation.";
            return StatusCode(500, new { 
                error = errorMsg,
                details = ex.Message 
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("checksum") || ex.Message.Contains("verification"))
        {
            _logger.LogError(ex, "Checksum verification failed for {EngineId}", request.EngineId);
            return StatusCode(500, new { 
                error = "Download verification failed. The file may be corrupted. Please try again.",
                details = ex.Message 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install engine {EngineId}", request.EngineId);
            return StatusCode(500, new { 
                error = $"Installation failed: {ex.Message}",
                details = ex.ToString()
            });
        }
    }

    /// <summary>
    /// Install an engine with SSE progress stream
    /// </summary>
    [HttpPost("install-stream")]
    public async Task InstallWithProgress([FromBody] InstallRequest request, CancellationToken ct)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        try
        {
            if (string.IsNullOrEmpty(request.EngineId))
            {
                var errorData = System.Text.Json.JsonSerializer.Serialize(new { error = "engineId is required" });
                await Response.WriteAsync($"event: error\ndata: {errorData}\n\n", ct);
                await Response.Body.FlushAsync(ct);
                return;
            }

            var manifest = await _manifestLoader.LoadManifestAsync();
            var engine = manifest.Engines.FirstOrDefault(e => e.Id == request.EngineId);
            
            if (engine == null)
            {
                var errorData = System.Text.Json.JsonSerializer.Serialize(new { error = $"Engine {request.EngineId} not found in manifest" });
                await Response.WriteAsync($"event: error\ndata: {errorData}\n\n", ct);
                await Response.Body.FlushAsync(ct);
                return;
            }

            if (_installer.IsInstalled(request.EngineId))
            {
                var errorData = System.Text.Json.JsonSerializer.Serialize(new { error = $"Engine {request.EngineId} is already installed" });
                await Response.WriteAsync($"event: error\ndata: {errorData}\n\n", ct);
                await Response.Body.FlushAsync(ct);
                return;
            }

            // Create progress handler
            var progress = new Progress<EngineInstallProgress>(p =>
            {
                try
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(p);
                    Response.WriteAsync($"event: progress\ndata: {json}\n\n", ct).GetAwaiter().GetResult();
                    Response.Body.FlushAsync(ct).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send progress update");
                }
            });

            // Install the engine with progress reporting
            string installPath = await _installer.InstallAsync(
                engine, 
                request.CustomUrl, 
                request.LocalFilePath, 
                progress, 
                ct);

            // Register with the registry
            string executablePath = System.IO.Path.Combine(installPath, engine.Entrypoint);
            
            var engineConfig = new EngineConfig(
                Id: engine.Id,
                EngineId: engine.Id,
                Name: engine.Name,
                Version: request.Version ?? engine.Version,
                Mode: EngineMode.Managed,
                InstallPath: installPath,
                ExecutablePath: executablePath,
                Arguments: engine.ArgsTemplate,
                Port: request.Port ?? engine.DefaultPort,
                HealthCheckUrl: engine.HealthCheck != null ? $"http://localhost:{request.Port ?? engine.DefaultPort}{engine.HealthCheck.Url}" : null,
                StartOnAppLaunch: false,
                AutoRestart: false
            );

            await _registry.RegisterEngineAsync(engineConfig);

            var completionData = System.Text.Json.JsonSerializer.Serialize(new { 
                success = true,
                engineId = request.EngineId,
                installPath,
                message = $"Engine {engine.Name} installed successfully"
            });
            await Response.WriteAsync($"event: complete\ndata: {completionData}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Install operation cancelled for {EngineId}", request.EngineId);
            var errorData = System.Text.Json.JsonSerializer.Serialize(new { error = "Installation cancelled by user" });
            await Response.WriteAsync($"event: error\ndata: {errorData}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install engine {EngineId}", request.EngineId);
            var errorData = System.Text.Json.JsonSerializer.Serialize(new { 
                error = $"Installation failed: {ex.Message}",
                details = ex.ToString()
            });
            await Response.WriteAsync($"event: error\ndata: {errorData}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
    }

    /// <summary>
    /// Verify an engine installation
    /// </summary>
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] EngineActionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.EngineId))
            {
                return BadRequest(new { error = "engineId is required" });
            }

            var manifest = await _manifestLoader.LoadManifestAsync();
            var engine = manifest.Engines.FirstOrDefault(e => e.Id == request.EngineId);
            
            if (engine == null)
            {
                return NotFound(new { error = $"Engine {request.EngineId} not found in manifest" });
            }

            var result = await _installer.VerifyAsync(engine);

            return Ok(new
            {
                engineId = result.EngineId,
                isValid = result.IsValid,
                status = result.Status,
                missingFiles = result.MissingFiles,
                issues = result.Issues
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify engine {EngineId}", request.EngineId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Repair an engine installation
    /// </summary>
    [HttpPost("repair")]
    public async Task<IActionResult> Repair([FromBody] EngineActionRequest request, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrEmpty(request.EngineId))
            {
                return BadRequest(new { error = "engineId is required" });
            }

            var manifest = await _manifestLoader.LoadManifestAsync();
            var engine = manifest.Engines.FirstOrDefault(e => e.Id == request.EngineId);
            
            if (engine == null)
            {
                return NotFound(new { error = $"Engine {request.EngineId} not found in manifest" });
            }

            await _installer.RepairAsync(engine, null, ct);

            return Ok(new
            {
                success = true,
                engineId = request.EngineId,
                message = $"Engine {engine.Name} repaired successfully"
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Repair operation cancelled for {EngineId}", request.EngineId);
            return StatusCode(499, new { error = "Repair cancelled by user" });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while repairing {EngineId}", request.EngineId);
            return StatusCode(500, new { 
                error = "Network error during repair. Check your internet connection and try again.",
                details = ex.Message 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to repair engine {EngineId}", request.EngineId);
            return StatusCode(500, new { 
                error = $"Repair failed: {ex.Message}",
                details = ex.ToString()
            });
        }
    }

    /// <summary>
    /// Remove an engine installation
    /// </summary>
    [HttpPost("remove")]
    public async Task<IActionResult> Remove([FromBody] EngineActionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.EngineId))
            {
                return BadRequest(new { error = "engineId is required" });
            }

            // Stop if running
            await _registry.StopEngineAsync(request.EngineId);

            // Unregister
            await _registry.UnregisterEngineAsync(request.EngineId);

            // Remove files
            var manifest = await _manifestLoader.LoadManifestAsync();
            var engine = manifest.Engines.FirstOrDefault(e => e.Id == request.EngineId);
            
            if (engine != null)
            {
                await _installer.RemoveAsync(engine);
            }

            return Ok(new
            {
                success = true,
                engineId = request.EngineId,
                message = $"Engine removed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove engine {EngineId}", request.EngineId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Start an engine
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] StartRequest request, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrEmpty(request.EngineId))
            {
                return BadRequest(new { error = "engineId is required" });
            }

            var engineConfig = _registry.GetEngine(request.EngineId);
            if (engineConfig == null)
            {
                return NotFound(new { error = $"Engine {request.EngineId} not registered" });
            }

            // Update port if provided
            if (request.Port.HasValue && request.Port != engineConfig.Port)
            {
                engineConfig = engineConfig with { Port = request.Port };
                await _registry.RegisterEngineAsync(engineConfig);
            }

            // Update args if provided
            if (!string.IsNullOrEmpty(request.Args) && request.Args != engineConfig.Arguments)
            {
                engineConfig = engineConfig with { Arguments = request.Args };
                await _registry.RegisterEngineAsync(engineConfig);
            }

            bool started = await _registry.StartEngineAsync(request.EngineId, ct);

            if (started)
            {
                var status = _processManager.GetStatus(request.EngineId);
                return Ok(new
                {
                    success = true,
                    engineId = request.EngineId,
                    processId = status.ProcessId,
                    port = engineConfig.Port,
                    logsPath = _processManager.GetLogPath(request.EngineId),
                    message = $"Engine {engineConfig.Name} started successfully"
                });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to start engine" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start engine {EngineId}", request.EngineId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Stop an engine
    /// </summary>
    [HttpPost("stop")]
    public async Task<IActionResult> Stop([FromBody] EngineActionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.EngineId))
            {
                return BadRequest(new { error = "engineId is required" });
            }

            bool stopped = await _registry.StopEngineAsync(request.EngineId);

            if (stopped)
            {
                return Ok(new
                {
                    success = true,
                    engineId = request.EngineId,
                    message = "Engine stopped successfully"
                });
            }
            else
            {
                return NotFound(new { error = $"Engine {request.EngineId} is not running" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop engine {EngineId}", request.EngineId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Save engine preferences (port, auto-start, etc.)
    /// </summary>
    [HttpPost("preferences")]
    public async Task<IActionResult> SavePreferences([FromBody] Dictionary<string, EnginePreferences> preferences)
    {
        try
        {
            foreach (var (engineId, prefs) in preferences)
            {
                var engineConfig = _registry.GetEngine(engineId);
                if (engineConfig != null)
                {
                    var updatedConfig = engineConfig with
                    {
                        Port = prefs.Port ?? engineConfig.Port,
                        StartOnAppLaunch = prefs.AutoStart
                    };
                    await _registry.RegisterEngineAsync(updatedConfig);
                }
            }

            return Ok(new { success = true, message = "Preferences saved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save engine preferences");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get engine preferences
    /// </summary>
    [HttpGet("preferences")]
    public IActionResult GetPreferences()
    {
        try
        {
            var engines = _registry.GetAllEngines();
            var preferences = engines.ToDictionary(
                e => e.Id,
                e => new EnginePreferences
                {
                    Port = e.Port,
                    AutoStart = e.StartOnAppLaunch
                }
            );

            return Ok(preferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get engine preferences");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get diagnostics for a specific engine
    /// </summary>
    [HttpGet("diagnostics/engine")]
    public async Task<IActionResult> GetEngineDiagnostics([FromQuery] string engineId)
    {
        try
        {
            if (string.IsNullOrEmpty(engineId))
            {
                return BadRequest(new { error = "engineId is required" });
            }

            var manifest = await _manifestLoader.LoadManifestAsync();
            var engine = manifest.Engines.FirstOrDefault(e => e.Id == engineId);
            
            if (engine == null)
            {
                return NotFound(new { error = $"Engine {engineId} not found in manifest" });
            }

            var diagnostics = await _installer.GetDiagnosticsAsync(engine);
            
            // Get last error from process manager if available
            var status = _processManager.GetStatus(engineId);
            var result = new
            {
                diagnostics.EngineId,
                diagnostics.InstallPath,
                diagnostics.IsInstalled,
                diagnostics.PathExists,
                diagnostics.PathWritable,
                diagnostics.AvailableDiskSpaceBytes,
                LastError = status.LastError ?? diagnostics.LastError,
                diagnostics.ChecksumStatus,
                diagnostics.ExpectedSha256,
                diagnostics.ActualSha256,
                diagnostics.FailedUrl,
                diagnostics.Issues
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get diagnostics for engine {EngineId}", engineId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get system diagnostics report
    /// </summary>
    [HttpGet("diagnostics")]
    public async Task<IActionResult> GetDiagnostics()
    {
        try
        {
            var report = await _lifecycleManager.GenerateDiagnosticsAsync();
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate diagnostics report");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get logs for a specific engine
    /// </summary>
    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs([FromQuery] string engineId, [FromQuery] int tailLines = 500)
    {
        try
        {
            if (string.IsNullOrEmpty(engineId))
            {
                return BadRequest(new { error = "engineId is required" });
            }

            var logs = await _registry.GetEngineLogsAsync(engineId, tailLines);
            return Ok(new { logs });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get logs for engine {EngineId}", engineId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get recent notifications
    /// </summary>
    [HttpGet("notifications")]
    public IActionResult GetNotifications([FromQuery] int count = 100)
    {
        try
        {
            var notifications = _lifecycleManager.GetRecentNotifications(count);
            return Ok(new { notifications });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notifications");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Restart a specific engine
    /// </summary>
    [HttpPost("restart")]
    public async Task<IActionResult> RestartEngine([FromBody] EngineActionRequest request, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrEmpty(request?.EngineId))
            {
                return BadRequest(new { error = "engineId is required" });
            }

            var success = await _lifecycleManager.RestartEngineAsync(request.EngineId, ct);
            
            if (success)
            {
                return Ok(new { message = $"Engine {request.EngineId} restarted successfully" });
            }
            else
            {
                return StatusCode(500, new { error = $"Failed to restart engine {request.EngineId}" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restart engine");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Detect all engines (installed, running, or available)
    /// </summary>
    [HttpGet("detect")]
    public async Task<IActionResult> DetectEngines(CancellationToken ct)
    {
        try
        {
            if (_engineDetector == null)
            {
                return StatusCode(500, new { error = "Engine detection not available" });
            }

            var detectionResults = await _engineDetector.DetectAllEnginesAsync(null, null, ct);
            
            return Ok(new { engines = detectionResults });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect engines");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Detect FFmpeg specifically
    /// </summary>
    [HttpGet("detect/ffmpeg")]
    public async Task<IActionResult> DetectFFmpeg([FromQuery] string? configuredPath = null)
    {
        try
        {
            if (_engineDetector == null)
            {
                return StatusCode(500, new { error = "Engine detection not available" });
            }

            var result = await _engineDetector.DetectFFmpegAsync(configuredPath);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect FFmpeg");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Detect Ollama specifically
    /// </summary>
    [HttpGet("detect/ollama")]
    public async Task<IActionResult> DetectOllama([FromQuery] string? url = null, CancellationToken ct = default)
    {
        try
        {
            if (_engineDetector == null)
            {
                return StatusCode(500, new { error = "Engine detection not available" });
            }

            var result = await _engineDetector.DetectOllamaAsync(url, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect Ollama");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// List available Ollama models
    /// </summary>
    [HttpGet("ollama/models")]
    public async Task<IActionResult> GetOllamaModels([FromQuery] string? url = null, CancellationToken ct = default)
    {
        try
        {
            var baseUrl = url ?? "http://127.0.0.1:11434";
            
            using var httpClient = new HttpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await httpClient.GetAsync($"{baseUrl}/api/tags", cts.Token);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get Ollama models from {Url}: {StatusCode}", baseUrl, response.StatusCode);
                return StatusCode((int)response.StatusCode, new { error = "Failed to retrieve models from Ollama" });
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            
            if (!doc.RootElement.TryGetProperty("models", out var modelsArray))
            {
                return Ok(new { models = Array.Empty<object>() });
            }

            var models = new List<object>();
            foreach (var model in modelsArray.EnumerateArray())
            {
                var name = model.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                var size = model.TryGetProperty("size", out var sizeProp) ? sizeProp.GetInt64() : 0;
                var modified = model.TryGetProperty("modified_at", out var modProp) ? modProp.GetString() : null;
                var digest = model.TryGetProperty("digest", out var digestProp) ? digestProp.GetString() : null;
                
                if (name != null)
                {
                    models.Add(new
                    {
                        name,
                        size,
                        sizeGB = Math.Round(size / 1024.0 / 1024.0 / 1024.0, 2),
                        modifiedAt = modified,
                        digest
                    });
                }
            }

            _logger.LogInformation("Retrieved {Count} Ollama models from {Url}", models.Count, baseUrl);
            return Ok(new { models, baseUrl });
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Timeout getting Ollama models from {Url}", url ?? "http://127.0.0.1:11434");
            return StatusCode(408, new { error = "Request timed out. Ensure Ollama is running." });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to connect to Ollama at {Url}", url ?? "http://127.0.0.1:11434");
            return StatusCode(503, new { error = "Cannot connect to Ollama. Ensure Ollama is running." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Ollama models");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Attach an existing external engine installation
    /// </summary>
    [HttpPost("attach")]
    public async Task<IActionResult> AttachExisting([FromBody] AttachEngineRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.EngineId))
            {
                return BadRequest(new { error = "engineId is required" });
            }

            if (string.IsNullOrEmpty(request.InstallPath))
            {
                return BadRequest(new { error = "installPath is required" });
            }

            // Generate unique instance ID
            var instanceId = $"{request.EngineId}-external-{Guid.NewGuid().ToString("N")[..8]}";
            
            var manifest = await _manifestLoader.LoadManifestAsync();
            var engine = manifest.Engines.FirstOrDefault(e => e.Id == request.EngineId);
            string engineName = engine?.Name ?? request.EngineId;

            var (success, error) = await _registry.AttachExternalEngineAsync(
                instanceId,
                request.EngineId,
                engineName,
                request.InstallPath,
                request.ExecutablePath,
                request.Port,
                request.HealthCheckUrl,
                request.Notes
            );

            if (!success)
            {
                return BadRequest(new { error });
            }

            return Ok(new
            {
                success = true,
                instanceId,
                engineId = request.EngineId,
                installPath = request.InstallPath,
                message = $"Successfully attached external {engineName} installation"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to attach external engine");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Reconfigure an existing engine instance
    /// </summary>
    [HttpPost("reconfigure")]
    public async Task<IActionResult> Reconfigure([FromBody] ReconfigureEngineRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.InstanceId))
            {
                return BadRequest(new { error = "instanceId is required" });
            }

            var (success, error) = await _registry.ReconfigureEngineAsync(
                request.InstanceId,
                request.InstallPath,
                request.ExecutablePath,
                request.Port,
                request.HealthCheckUrl,
                request.Notes
            );

            if (!success)
            {
                return BadRequest(new { error });
            }

            return Ok(new
            {
                success = true,
                instanceId = request.InstanceId,
                message = "Engine reconfigured successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reconfigure engine");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all engine instances (both managed and external)
    /// </summary>
    [HttpGet("instances")]
    public async Task<IActionResult> GetInstances()
    {
        try
        {
            var engines = _registry.GetAllEngines();
            var instances = new List<object>();

            foreach (var engine in engines)
            {
                var status = await _registry.GetEngineStatusAsync(engine.Id);
                instances.Add(new
                {
                    engine.Id,
                    engine.EngineId,
                    engine.Name,
                    engine.Version,
                    Mode = engine.Mode.ToString(),
                    engine.InstallPath,
                    engine.ExecutablePath,
                    engine.Port,
                    Status = status.IsRunning ? "running" : (status.IsInstalled ? "installed" : "not_installed"),
                    status.IsRunning,
                    status.IsHealthy,
                    engine.Notes,
                    engine.HealthCheckUrl
                });
            }

            return Ok(new { instances });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get engine instances");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Open the engine installation folder in system file explorer
    /// </summary>
    [HttpPost("open-folder")]
    public IActionResult OpenFolder([FromBody] EngineActionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.EngineId))
            {
                return BadRequest(new { error = "engineId is required" });
            }

            var engine = _registry.GetEngine(request.EngineId);
            if (engine == null)
            {
                return NotFound(new { error = "Engine not found" });
            }

            if (string.IsNullOrEmpty(engine.InstallPath) || !Directory.Exists(engine.InstallPath))
            {
                return BadRequest(new { error = "Install path not found" });
            }

            // Open folder using platform-specific command
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = engine.InstallPath
            };

            if (OperatingSystem.IsWindows())
            {
                startInfo.FileName = "explorer.exe";
                startInfo.Arguments = engine.InstallPath;
            }
            else if (OperatingSystem.IsLinux())
            {
                startInfo.FileName = "xdg-open";
                startInfo.Arguments = engine.InstallPath;
            }
            else if (OperatingSystem.IsMacOS())
            {
                startInfo.FileName = "open";
                startInfo.Arguments = engine.InstallPath;
            }

            System.Diagnostics.Process.Start(startInfo);

            return Ok(new
            {
                success = true,
                path = engine.InstallPath,
                message = "Folder opened successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open folder for engine {EngineId}", request.EngineId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get the web UI URL for an engine
    /// </summary>
    [HttpPost("open-webui")]
    public IActionResult GetWebUIUrl([FromBody] EngineActionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.EngineId))
            {
                return BadRequest(new { error = "engineId is required" });
            }

            var engine = _registry.GetEngine(request.EngineId);
            if (engine == null)
            {
                return NotFound(new { error = "Engine not found" });
            }

            if (!engine.Port.HasValue)
            {
                return BadRequest(new { error = "Engine does not have a web UI" });
            }

            var url = $"http://localhost:{engine.Port}";

            return Ok(new
            {
                success = true,
                url,
                message = "Open this URL in your browser"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get web UI URL for engine {EngineId}", request.EngineId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Resolve download URL for an engine from GitHub Releases API
    /// </summary>
    [HttpGet("resolve-url")]
    public async Task<IActionResult> ResolveUrl([FromQuery] string engineId, CancellationToken ct = default)
    {
        try
        {
            var manifest = await _manifestLoader.LoadManifestAsync();
            var engine = manifest.Engines.FirstOrDefault(e => e.Id == engineId);
            
            if (engine == null)
            {
                return NotFound(new { error = $"Engine '{engineId}' not found" });
            }

            // If no GitHub repo or already installed, return static URL
            if (string.IsNullOrEmpty(engine.GitHubRepo) || _installer.IsInstalled(engineId))
            {
                var platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "windows" : "linux";
                var staticUrl = engine.Urls?.GetValueOrDefault(platform);
                
                return Ok(new 
                { 
                    url = staticUrl,
                    source = "static",
                    githubRepo = engine.GitHubRepo
                });
            }

            // Try to resolve from GitHub API
            if (_releaseResolver != null && !string.IsNullOrEmpty(engine.AssetPattern))
            {
                var resolvedUrl = await _releaseResolver.ResolveLatestAssetUrlAsync(
                    engine.GitHubRepo, 
                    engine.AssetPattern, 
                    ct);
                
                if (!string.IsNullOrEmpty(resolvedUrl))
                {
                    return Ok(new 
                    { 
                        url = resolvedUrl,
                        source = "github-api",
                        githubRepo = engine.GitHubRepo
                    });
                }
            }

            // Fallback to mirrors or static URL
            var currentPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "windows" : "linux";
            
            // Try mirrors first
            if (engine.Mirrors?.TryGetValue(currentPlatform, out var mirrors) == true && mirrors.Count > 0)
            {
                return Ok(new 
                { 
                    url = mirrors[0],
                    source = "mirror",
                    githubRepo = engine.GitHubRepo
                });
            }

            // Fallback to static URL
            var fallbackUrl = engine.Urls?.GetValueOrDefault(currentPlatform);
            return Ok(new 
            { 
                url = fallbackUrl,
                source = "static",
                githubRepo = engine.GitHubRepo
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve URL for engine {EngineId}", engineId);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public record InstallRequest(
    string EngineId, 
    string? Version = null, 
    int? Port = null,
    string? CustomUrl = null,
    string? LocalFilePath = null);
public record EngineActionRequest(string EngineId);
public record StartRequest(string EngineId, int? Port = null, string? Args = null);
public record EnginePreferences
{
    public int? Port { get; set; }
    public bool AutoStart { get; set; }
}
public record AttachEngineRequest(
    string EngineId,
    string InstallPath,
    string? ExecutablePath = null,
    int? Port = null,
    string? HealthCheckUrl = null,
    string? Notes = null
);
public record ReconfigureEngineRequest(
    string InstanceId,
    string? InstallPath = null,
    string? ExecutablePath = null,
    int? Port = null,
    string? HealthCheckUrl = null,
    string? Notes = null
);
