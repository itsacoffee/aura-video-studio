using Aura.Core.Downloads;
using Aura.Core.Runtime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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

    public EnginesController(
        ILogger<EnginesController> logger,
        EngineManifestLoader manifestLoader,
        EngineInstaller installer,
        LocalEnginesRegistry registry,
        ExternalProcessManager processManager)
    {
        _logger = logger;
        _manifestLoader = manifestLoader;
        _installer = installer;
        _registry = registry;
        _processManager = processManager;
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
            var engines = manifest.Engines.Select(e => new
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
                InstallPath = _installer.GetInstallPath(e.Id)
            }).ToList();

            return Ok(new { engines });
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

            // Install the engine
            await _installer.InstallAsync(engine, null, ct);

            // Register with the registry
            string installPath = _installer.GetInstallPath(request.EngineId);
            string executablePath = System.IO.Path.Combine(installPath, engine.Entrypoint);
            
            var engineConfig = new EngineConfig(
                engine.Id,
                engine.Name,
                request.Version ?? engine.Version,
                installPath,
                executablePath,
                engine.ArgsTemplate,
                request.Port ?? engine.DefaultPort,
                engine.HealthCheck != null ? $"http://localhost:{request.Port ?? engine.DefaultPort}{engine.HealthCheck.Url}" : null,
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install engine {EngineId}", request.EngineId);
            return StatusCode(500, new { error = ex.Message });
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to repair engine {EngineId}", request.EngineId);
            return StatusCode(500, new { error = ex.Message });
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
}

public record InstallRequest(string EngineId, string? Version = null, int? Port = null);
public record EngineActionRequest(string EngineId);
public record StartRequest(string EngineId, int? Port = null, string? Args = null);
