using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

[ApiController]
[Route("api/dependencies")]
public class DependenciesController : ControllerBase
{
    private readonly ILogger<DependenciesController> _logger;
    private readonly DependencyRescanService _rescanService;
    private readonly FfmpegInstaller? _ffmpegInstaller;

    public DependenciesController(
        ILogger<DependenciesController> logger,
        DependencyRescanService rescanService,
        FfmpegInstaller? ffmpegInstaller = null)
    {
        _logger = logger;
        _rescanService = rescanService;
        _ffmpegInstaller = ffmpegInstaller;
    }

    /// <summary>
    /// Rescan all dependencies and return full report
    /// </summary>
    [HttpPost("rescan")]
    public async Task<IActionResult> RescanAll(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Starting full dependency rescan via API");
            
            var report = await _rescanService.RescanAllAsync(ct);
            
            return Ok(new
            {
                success = true,
                scanTime = report.ScanTime,
                dependencies = report.Dependencies
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rescan dependencies");
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Re-run only path checks (fast refresh)
    /// </summary>
    [HttpPost("refresh-candidate-paths")]
    public async Task<IActionResult> RefreshCandidatePaths(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Refreshing candidate paths");
            
            // This is essentially the same as a full rescan for now
            // In the future, this could be optimized to only check file paths
            // without running validation commands
            var report = await _rescanService.RescanAllAsync(ct);
            
            return Ok(new
            {
                success = true,
                scanTime = report.ScanTime,
                dependencies = report.Dependencies
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh candidate paths");
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get the last scan time
    /// </summary>
    [HttpGet("last-scan-time")]
    public async Task<IActionResult> GetLastScanTime()
    {
        try
        {
            var lastScanTime = await _rescanService.GetLastScanTimeAsync();
            
            return Ok(new
            {
                success = true,
                lastScanTime = lastScanTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get last scan time");
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }
    
    /// <summary>
    /// Verify FFmpeg installation and run smoke test
    /// </summary>
    [HttpPost("{componentId}/verify")]
    public async Task<IActionResult> VerifyComponent(string componentId, CancellationToken ct)
    {
        if (componentId != "ffmpeg" || _ffmpegInstaller == null)
        {
            return BadRequest(new
            {
                success = false,
                error = $"Verification not supported for component: {componentId}"
            });
        }
        
        try
        {
            _logger.LogInformation("Verifying FFmpeg installation via API");
            
            // Try to find FFmpeg via rescan
            var report = await _rescanService.RescanAllAsync(ct);
            var ffmpegDep = report.Dependencies.Find(d => d.Id == "ffmpeg");
            
            if (ffmpegDep == null || ffmpegDep.Status != DependencyStatus.Installed || string.IsNullOrEmpty(ffmpegDep.Path))
            {
                return Ok(new
                {
                    success = false,
                    available = false,
                    status = ffmpegDep?.Status.ToString() ?? "Missing",
                    error = ffmpegDep?.ErrorMessage ?? "FFmpeg not found. Install or attach FFmpeg first."
                });
            }
            
            // Run smoke test
            var smokeTestResult = await _ffmpegInstaller.RunSmokeTestAsync(ffmpegDep.Path, ct);
            
            if (!smokeTestResult.success)
            {
                return Ok(new
                {
                    success = false,
                    available = true,
                    path = ffmpegDep.Path,
                    validationOutput = ffmpegDep.ValidationOutput,
                    smokeTestPassed = false,
                    error = smokeTestResult.error,
                    diagnostics = new
                    {
                        output = smokeTestResult.output,
                        suggestion = "FFmpeg binary may be corrupted. Try reinstalling or repairing."
                    }
                });
            }
            
            return Ok(new
            {
                success = true,
                available = true,
                path = ffmpegDep.Path,
                validationOutput = ffmpegDep.ValidationOutput,
                smokeTestPassed = true,
                output = smokeTestResult.output
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify {ComponentId}", componentId);
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }
    
    /// <summary>
    /// Repair or reinstall a component
    /// </summary>
    [HttpPost("{componentId}/repair")]
    public async Task<IActionResult> RepairComponent(string componentId, CancellationToken ct)
    {
        if (componentId != "ffmpeg")
        {
            return BadRequest(new
            {
                success = false,
                error = $"Repair not supported for component: {componentId}"
            });
        }
        
        try
        {
            _logger.LogInformation("Repairing {ComponentId} via API", componentId);
            
            // For now, repair = rescan to update paths
            // In the future, this could trigger reinstallation
            var report = await _rescanService.RescanAllAsync(ct);
            var ffmpegDep = report.Dependencies.Find(d => d.Id == "ffmpeg");
            
            if (ffmpegDep == null || ffmpegDep.Status != DependencyStatus.Installed)
            {
                return Ok(new
                {
                    success = false,
                    repaired = false,
                    status = ffmpegDep?.Status.ToString() ?? "Missing",
                    message = "FFmpeg not found. Use Download Center to install FFmpeg."
                });
            }
            
            return Ok(new
            {
                success = true,
                repaired = true,
                path = ffmpegDep.Path,
                validationOutput = ffmpegDep.ValidationOutput,
                message = "FFmpeg path refreshed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to repair {ComponentId}", componentId);
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }
}
