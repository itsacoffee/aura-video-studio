using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Generation;

/// <summary>
/// Monitors system resources (CPU, GPU, memory) to optimize concurrent generation operations.
/// Provides adaptive throttling based on available resources.
/// </summary>
public class ResourceMonitor
{
    private readonly ILogger<ResourceMonitor> _logger;
    private readonly object _lock = new();
    private ResourceSnapshot _currentSnapshot;
    private DateTime _lastUpdate = DateTime.MinValue;
    private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(2);

    public ResourceMonitor(ILogger<ResourceMonitor> logger)
    {
        _logger = logger;
        _currentSnapshot = new ResourceSnapshot(0, 0, 0, DateTime.UtcNow);
    }

    /// <summary>
    /// Gets the current resource utilization snapshot
    /// </summary>
    public ResourceSnapshot GetCurrentSnapshot()
    {
        lock (_lock)
        {
            // Update snapshot if stale
            if (DateTime.UtcNow - _lastUpdate > _updateInterval)
            {
                _currentSnapshot = CaptureSnapshot();
                _lastUpdate = DateTime.UtcNow;
            }

            return _currentSnapshot;
        }
    }

    /// <summary>
    /// Calculates the recommended number of concurrent operations based on current resources
    /// </summary>
    public int GetRecommendedConcurrency()
    {
        var snapshot = GetCurrentSnapshot();

        // Start with logical processor count
        int baseConcurrency = Environment.ProcessorCount;

        // Adjust based on CPU usage
        if (snapshot.CpuUsagePercent > 80)
        {
            baseConcurrency = Math.Max(1, baseConcurrency / 2);
        }
        else if (snapshot.CpuUsagePercent > 60)
        {
            baseConcurrency = Math.Max(1, (int)(baseConcurrency * 0.75));
        }

        // Adjust based on memory usage
        if (snapshot.MemoryUsagePercent > 85)
        {
            baseConcurrency = Math.Max(1, baseConcurrency / 2);
        }
        else if (snapshot.MemoryUsagePercent > 70)
        {
            baseConcurrency = Math.Max(1, (int)(baseConcurrency * 0.8));
        }

        _logger.LogDebug(
            "Recommended concurrency: {Concurrency} (CPU: {Cpu}%, Memory: {Memory}%)",
            baseConcurrency,
            snapshot.CpuUsagePercent,
            snapshot.MemoryUsagePercent);

        return Math.Max(1, baseConcurrency);
    }

    /// <summary>
    /// Determines if resources are available to start a new task with the given cost
    /// </summary>
    public bool CanStartTask(double estimatedResourceCost)
    {
        var snapshot = GetCurrentSnapshot();

        // Resource cost is normalized 0-1, where 1.0 = full system capacity
        // Don't start heavy tasks if resources are constrained
        if (estimatedResourceCost > 0.7)
        {
            return snapshot.CpuUsagePercent < 60 && snapshot.MemoryUsagePercent < 70;
        }
        else if (estimatedResourceCost > 0.4)
        {
            return snapshot.CpuUsagePercent < 75 && snapshot.MemoryUsagePercent < 80;
        }
        else
        {
            return snapshot.CpuUsagePercent < 90 && snapshot.MemoryUsagePercent < 90;
        }
    }

    /// <summary>
    /// Waits until resources become available for a task with the given cost
    /// </summary>
    public async Task WaitForResourcesAsync(double estimatedResourceCost, CancellationToken ct)
    {
        int attempts = 0;
        while (!CanStartTask(estimatedResourceCost) && !ct.IsCancellationRequested)
        {
            attempts++;
            int delayMs = Math.Min(5000, 500 * attempts); // Exponential backoff up to 5 seconds

            _logger.LogDebug(
                "Waiting for resources to become available (attempt {Attempt}, delay {Delay}ms)",
                attempts,
                delayMs);

            await Task.Delay(delayMs, ct).ConfigureAwait(false);

            // Force snapshot update
            lock (_lock)
            {
                _lastUpdate = DateTime.MinValue;
            }
        }

        ct.ThrowIfCancellationRequested();
    }

    private ResourceSnapshot CaptureSnapshot()
    {
        double cpuUsage = 0;
        double memoryUsage = 0;
        double gpuUsage = 0;

        try
        {
            // Get current process for CPU measurement
            using var currentProcess = Process.GetCurrentProcess();

            // CPU usage (approximate)
            var startTime = DateTime.UtcNow;
            var startCpuTime = currentProcess.TotalProcessorTime;

            Thread.Sleep(100); // Brief sampling period

            var endTime = DateTime.UtcNow;
            var endCpuTime = currentProcess.TotalProcessorTime;

            var cpuUsedMs = (endCpuTime - startCpuTime).TotalMilliseconds;
            var totalMs = (endTime - startTime).TotalMilliseconds;
            var cpuUsageRatio = cpuUsedMs / (Environment.ProcessorCount * totalMs);

            cpuUsage = Math.Min(100, cpuUsageRatio * 100);

            // Memory usage
            var totalMemory = GC.GetTotalMemory(false);
            var gcInfo = GC.GetGCMemoryInfo();
            var totalAvailable = gcInfo.TotalAvailableMemoryBytes > 0
                ? gcInfo.TotalAvailableMemoryBytes
                : (long)16L * 1024 * 1024 * 1024; // Default 16GB if unavailable

            memoryUsage = Math.Min(100, (double)totalMemory / totalAvailable * 100);

            // GPU usage would require platform-specific APIs
            // For now, we'll estimate based on task type
            gpuUsage = 0;

            _logger.LogTrace(
                "Resource snapshot: CPU={Cpu:F1}%, Memory={Memory:F1}%, GPU={Gpu:F1}%",
                cpuUsage,
                memoryUsage,
                gpuUsage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture resource snapshot, using defaults");
            // Return conservative estimates on error
            cpuUsage = 50;
            memoryUsage = 50;
            gpuUsage = 0;
        }

        return new ResourceSnapshot(cpuUsage, memoryUsage, gpuUsage, DateTime.UtcNow);
    }
}

/// <summary>
/// Snapshot of system resource utilization at a point in time
/// </summary>
public record ResourceSnapshot(
    double CpuUsagePercent,
    double MemoryUsagePercent,
    double GpuUsagePercent,
    DateTime Timestamp);
