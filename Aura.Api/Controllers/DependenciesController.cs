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

    public DependenciesController(
        ILogger<DependenciesController> logger,
        DependencyRescanService rescanService)
    {
        _logger = logger;
        _rescanService = rescanService;
    }

    /// <summary>
    /// Rescan all dependencies and return full report
    /// </summary>
    [HttpGet("rescan")]
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
}
