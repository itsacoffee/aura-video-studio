using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Diagnostics;

/// <summary>
/// Resource usage metrics
/// </summary>
public record ResourceMetrics
{
    public int OpenFileHandles { get; init; }
    public int ActiveProcesses { get; init; }
    public long AllocatedMemoryBytes { get; init; }
    public long WorkingSetBytes { get; init; }
    public int ThreadCount { get; init; }
    public DateTime Timestamp { get; init; }
    public List<string> Warnings { get; init; } = new();
}

/// <summary>
/// Tracks and monitors system resources to detect leaks and excessive usage
/// </summary>
public interface IResourceTracker
{
    /// <summary>
    /// Get current resource metrics
    /// </summary>
    Task<ResourceMetrics> GetMetricsAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Force a cleanup sweep
    /// </summary>
    Task CleanupAsync(CancellationToken ct = default);
}

/// <summary>
/// Implementation of resource tracker with periodic cleanup
/// </summary>
public class ResourceTracker : IResourceTracker, IDisposable
{
    private readonly ILogger<ResourceTracker> _logger;
    private readonly Timer _cleanupTimer;
    private bool _disposed;
    
    // Thresholds for warnings
    private const int FileHandleWarningThreshold = 1000;
    private const int ProcessCountWarningThreshold = 10;
    private const long MemoryWarningThresholdBytes = 2L * 1024 * 1024 * 1024; // 2GB

    public ResourceTracker(ILogger<ResourceTracker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Setup periodic cleanup sweep every 15 minutes
        _cleanupTimer = new Timer(
            PeriodicCleanup, 
            null, 
            TimeSpan.FromMinutes(15), 
            TimeSpan.FromMinutes(15));
        
        _logger.LogInformation("ResourceTracker initialized with 15-minute cleanup interval");
    }

    public async Task<ResourceMetrics> GetMetricsAsync(CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        
        var warnings = new List<string>();
        
        try
        {
            using var currentProcess = Process.GetCurrentProcess();
            
            // Get file handle count (platform-specific)
            int fileHandles = GetFileHandleCount(currentProcess);
            
            // Get child process count (FFmpeg processes)
            int childProcesses = GetChildProcessCount(currentProcess);
            
            // Memory metrics
            long allocatedMemory = GC.GetTotalMemory(false);
            long workingSet = currentProcess.WorkingSet64;
            int threadCount = currentProcess.Threads.Count;
            
            // Check thresholds and generate warnings
            if (fileHandles > FileHandleWarningThreshold)
            {
                var warning = $"High file handle count: {fileHandles} (threshold: {FileHandleWarningThreshold})";
                warnings.Add(warning);
                _logger.LogWarning(warning);
            }
            
            if (childProcesses > ProcessCountWarningThreshold)
            {
                var warning = $"High child process count: {childProcesses} (threshold: {ProcessCountWarningThreshold})";
                warnings.Add(warning);
                _logger.LogWarning(warning);
            }
            
            if (allocatedMemory > MemoryWarningThresholdBytes)
            {
                var warning = $"High memory usage: {allocatedMemory / (1024 * 1024)}MB (threshold: {MemoryWarningThresholdBytes / (1024 * 1024)}MB)";
                warnings.Add(warning);
                _logger.LogWarning(warning);
            }
            
            return new ResourceMetrics
            {
                OpenFileHandles = fileHandles,
                ActiveProcesses = childProcesses,
                AllocatedMemoryBytes = allocatedMemory,
                WorkingSetBytes = workingSet,
                ThreadCount = threadCount,
                Timestamp = DateTime.UtcNow,
                Warnings = warnings
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting resource metrics");
            
            return new ResourceMetrics
            {
                OpenFileHandles = -1,
                ActiveProcesses = -1,
                AllocatedMemoryBytes = -1,
                WorkingSetBytes = -1,
                ThreadCount = -1,
                Timestamp = DateTime.UtcNow,
                Warnings = new List<string> { $"Error collecting metrics: {ex.Message}" }
            };
        }
    }

    public async Task CleanupAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Running manual resource cleanup");
        
        try
        {
            // Force garbage collection
            var memoryBefore = GC.GetTotalMemory(false);
            
            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            
            var memoryAfter = GC.GetTotalMemory(false);
            var freed = memoryBefore - memoryAfter;
            
            _logger.LogInformation(
                "Cleanup complete: freed {FreedMB}MB (before: {BeforeMB}MB, after: {AfterMB}MB)",
                freed / (1024 * 1024),
                memoryBefore / (1024 * 1024),
                memoryAfter / (1024 * 1024));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup");
        }
        
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private void PeriodicCleanup(object? state)
    {
        try
        {
            _logger.LogDebug("Running periodic resource cleanup sweep");
            _ = CleanupAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during periodic cleanup sweep");
        }
    }

    private int GetFileHandleCount(Process process)
    {
        try
        {
            // On Windows, use handle count
            if (OperatingSystem.IsWindows())
            {
                return process.HandleCount;
            }
            
            // On Linux, count open file descriptors
            if (OperatingSystem.IsLinux())
            {
                var fdPath = $"/proc/{process.Id}/fd";
                if (Directory.Exists(fdPath))
                {
                    return Directory.GetFiles(fdPath).Length;
                }
            }
            
            // On macOS, use lsof (less reliable, fallback)
            if (OperatingSystem.IsMacOS())
            {
                // Approximate using handle count if available
                return process.HandleCount;
            }
            
            // Fallback: return -1 to indicate unavailable
            return -1;
        }
        catch
        {
            return -1;
        }
    }

    private int GetChildProcessCount(Process currentProcess)
    {
        try
        {
            // Count FFmpeg child processes
            var ffmpegProcesses = Process.GetProcessesByName("ffmpeg")
                .Concat(Process.GetProcessesByName("ffmpeg.exe"))
                .Where(p => IsChildProcess(p, currentProcess))
                .Count();
            
            return ffmpegProcesses;
        }
        catch
        {
            return -1;
        }
    }

    private bool IsChildProcess(Process child, Process parent)
    {
        try
        {
            // On Windows, check parent process ID using WMI
            if (OperatingSystem.IsWindows())
            {
                using var searcher = new System.Management.ManagementObjectSearcher(
                    $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {child.Id}");
                
                foreach (var obj in searcher.Get())
                {
                    var parentId = Convert.ToInt32(obj["ParentProcessId"]);
                    return parentId == parent.Id;
                }
            }
            
            // On Linux/macOS, processes started by us are likely children
            // This is approximate but works for our use case
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("Disposing ResourceTracker");
        _cleanupTimer?.Dispose();
        
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
