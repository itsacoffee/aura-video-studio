using System;
using System.Diagnostics;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Memory;

/// <summary>
/// Manages memory pressure and performs garbage collection when memory usage is high
/// Helps prevent memory leaks and out-of-memory errors during video processing
/// </summary>
public class MemoryPressureManager
{
    private readonly ILogger<MemoryPressureManager> _logger;
    private readonly Timer _monitoringTimer;
    private long _lastGCBytes;
    private DateTime _lastGCTime;
    
    private const long MemoryThresholdBytes = 500 * 1024 * 1024; // 500 MB threshold
    private const double MemoryGrowthThreshold = 1.5; // 50% growth
    private const int MonitoringIntervalMs = 10000; // 10 seconds

    public MemoryPressureManager(ILogger<MemoryPressureManager> logger)
    {
        _logger = logger;
        _lastGCTime = DateTime.UtcNow;
        
        // Start monitoring timer
        _monitoringTimer = new Timer(
            MonitorMemoryCallback,
            null,
            MonitoringIntervalMs,
            MonitoringIntervalMs);
    }

    /// <summary>
    /// Monitoring callback to check memory pressure
    /// </summary>
    private void MonitorMemoryCallback(object? state)
    {
        try
        {
            var currentMemory = GC.GetTotalMemory(false);
            var gcInfo = GC.GetGCMemoryInfo();
            
            // Check if we're approaching memory limits
            if (currentMemory > MemoryThresholdBytes)
            {
                var timeSinceLastGC = DateTime.UtcNow - _lastGCTime;
                
                // Only trigger GC if enough time has passed (avoid excessive GC)
                if (timeSinceLastGC.TotalSeconds >= 30)
                {
                    _logger.LogWarning(
                        "High memory usage detected: {Current:N0} bytes. Triggering garbage collection.",
                        currentMemory);
                    
                    PerformGarbageCollection();
                }
            }

            // Check for memory growth
            if (_lastGCBytes > 0)
            {
                var growthRatio = (double)currentMemory / _lastGCBytes;
                if (growthRatio > MemoryGrowthThreshold)
                {
                    _logger.LogWarning(
                        "Significant memory growth detected: {Ratio:F2}x. Current: {Current:N0}, Previous: {Previous:N0}",
                        growthRatio, currentMemory, _lastGCBytes);
                }
            }

            _lastGCBytes = currentMemory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in memory monitoring callback");
        }
    }

    /// <summary>
    /// Perform garbage collection
    /// </summary>
    public void PerformGarbageCollection()
    {
        var before = GC.GetTotalMemory(false);
        
        _logger.LogInformation("Performing garbage collection...");
        
        // Collect all generations
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        
        var after = GC.GetTotalMemory(false);
        var freed = before - after;
        
        _lastGCTime = DateTime.UtcNow;
        _lastGCBytes = after;
        
        _logger.LogInformation(
            "Garbage collection completed. Freed: {Freed:N0} bytes. Memory: {Before:N0} -> {After:N0}",
            freed, before, after);
    }

    /// <summary>
    /// Register temporary data that should be cleaned up
    /// </summary>
    public void RegisterMemoryPressure(long bytes)
    {
        if (bytes > 0)
        {
            GC.AddMemoryPressure(bytes);
            _logger.LogDebug("Registered memory pressure: {Bytes:N0} bytes", bytes);
        }
    }

    /// <summary>
    /// Unregister memory pressure when data is cleaned up
    /// </summary>
    public void RemoveMemoryPressure(long bytes)
    {
        if (bytes > 0)
        {
            GC.RemoveMemoryPressure(bytes);
            _logger.LogDebug("Removed memory pressure: {Bytes:N0} bytes", bytes);
        }
    }

    /// <summary>
    /// Get current memory usage statistics
    /// </summary>
    public MemoryStats GetMemoryStats()
    {
        var gcInfo = GC.GetGCMemoryInfo();
        var process = Process.GetCurrentProcess();
        
        return new MemoryStats
        {
            TotalManagedMemory = GC.GetTotalMemory(false),
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2),
            TotalAvailableMemory = gcInfo.TotalAvailableMemoryBytes,
            HeapSize = gcInfo.HeapSizeBytes,
            MemoryLoad = gcInfo.MemoryLoadBytes,
            WorkingSet = process.WorkingSet64,
            PrivateMemory = process.PrivateMemorySize64
        };
    }

    /// <summary>
    /// Configure garbage collection for video processing workloads
    /// </summary>
    public void ConfigureForVideoProcessing()
    {
        _logger.LogInformation("Configuring GC for video processing workloads");
        
        // Use server GC if available (better for throughput)
        if (GCSettings.IsServerGC)
        {
            _logger.LogInformation("Server GC is enabled");
        }
        else
        {
            _logger.LogInformation("Using Workstation GC");
        }

        // Set latency mode for batch processing
        GCSettings.LatencyMode = GCLatencyMode.Batch;
        _logger.LogInformation("Set GC latency mode to Batch for better throughput");
    }

    /// <summary>
    /// Reset GC configuration to defaults
    /// </summary>
    public void ResetGCConfiguration()
    {
        GCSettings.LatencyMode = GCLatencyMode.Interactive;
        _logger.LogInformation("Reset GC latency mode to Interactive");
    }

    public void Dispose()
    {
        _monitoringTimer?.Dispose();
    }
}

/// <summary>
/// Memory usage statistics
/// </summary>
public class MemoryStats
{
    public long TotalManagedMemory { get; init; }
    public int Gen0Collections { get; init; }
    public int Gen1Collections { get; init; }
    public int Gen2Collections { get; init; }
    public long TotalAvailableMemory { get; init; }
    public long HeapSize { get; init; }
    public long MemoryLoad { get; init; }
    public long WorkingSet { get; init; }
    public long PrivateMemory { get; init; }

    public string ToReadableString()
    {
        return $"Managed: {TotalManagedMemory / (1024 * 1024):N0} MB, " +
               $"WorkingSet: {WorkingSet / (1024 * 1024):N0} MB, " +
               $"Private: {PrivateMemory / (1024 * 1024):N0} MB, " +
               $"GC: Gen0={Gen0Collections}, Gen1={Gen1Collections}, Gen2={Gen2Collections}";
    }
}
