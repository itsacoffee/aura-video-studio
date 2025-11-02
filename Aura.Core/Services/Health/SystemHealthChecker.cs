using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Health;

/// <summary>
/// Checks system-level health (FFmpeg, disk space, memory)
/// </summary>
public class SystemHealthChecker
{
    private readonly ILogger<SystemHealthChecker> _logger;
    private readonly IFfmpegLocator? _ffmpegLocator;

    public SystemHealthChecker(
        ILogger<SystemHealthChecker> logger,
        IFfmpegLocator? ffmpegLocator = null)
    {
        _logger = logger;
        _ffmpegLocator = ffmpegLocator;
    }

    /// <summary>
    /// Perform comprehensive system health check
    /// </summary>
    public async Task<SystemHealthMetrics> CheckSystemHealthAsync(CancellationToken ct = default)
    {
        var issues = new List<string>();
        
        var ffmpegAvailable = false;
        string? ffmpegVersion = null;
        
        try
        {
            if (_ffmpegLocator != null)
            {
                var validationResult = await _ffmpegLocator.CheckAllCandidatesAsync(null, ct).ConfigureAwait(false);
                if (validationResult.Found && !string.IsNullOrEmpty(validationResult.FfmpegPath))
                {
                    ffmpegAvailable = true;
                    ffmpegVersion = validationResult.VersionString;
                }
                else
                {
                    issues.Add("FFmpeg not found");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check FFmpeg availability");
            issues.Add($"FFmpeg check failed: {ex.Message}");
        }

        var diskSpaceGB = 0.0;
        try
        {
            var tempPath = Path.GetTempPath();
            var drive = new DriveInfo(Path.GetPathRoot(tempPath) ?? tempPath);
            diskSpaceGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
            
            if (diskSpaceGB < 1.0)
            {
                issues.Add($"Low disk space: {diskSpaceGB:F2} GB available");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check disk space");
            issues.Add($"Disk space check failed: {ex.Message}");
        }

        var memoryUsagePercent = 0.0;
        try
        {
            var currentProcess = Process.GetCurrentProcess();
            var totalMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            var usedMemory = currentProcess.WorkingSet64;
            
            if (totalMemory > 0)
            {
                memoryUsagePercent = (double)usedMemory / totalMemory * 100.0;
            }
            
            if (memoryUsagePercent > 90.0)
            {
                issues.Add($"High memory usage: {memoryUsagePercent:F1}%");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check memory usage");
            issues.Add($"Memory check failed: {ex.Message}");
        }

        var isHealthy = ffmpegAvailable && diskSpaceGB >= 1.0 && memoryUsagePercent < 90.0;

        return new SystemHealthMetrics
        {
            FFmpegAvailable = ffmpegAvailable,
            FFmpegVersion = ffmpegVersion,
            DiskSpaceGB = diskSpaceGB,
            MemoryUsagePercent = memoryUsagePercent,
            IsHealthy = isHealthy,
            Issues = issues
        };
    }


}
