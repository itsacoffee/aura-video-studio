using System;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Performance;

/// <summary>
/// Enhanced memory monitor with GC optimization and leak detection
/// </summary>
public class EnhancedMemoryMonitor : IDisposable
{
    private readonly ILogger<EnhancedMemoryMonitor> _logger;
    private readonly Timer? _monitorTimer;
    private readonly int _memoryLimitMb;
    private readonly int _gcThresholdMb;
    private long _lastMemoryUsage;
    private int _consecutiveHighMemoryCount;
    private bool _disposed;

    public EnhancedMemoryMonitor(
        ILogger<EnhancedMemoryMonitor> logger,
        int memoryLimitMb = 500,
        int gcThresholdMb = 400,
        int monitorIntervalSeconds = 30)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _memoryLimitMb = memoryLimitMb;
        _gcThresholdMb = gcThresholdMb;
        _lastMemoryUsage = 0;
        _consecutiveHighMemoryCount = 0;

        _monitorTimer = new Timer(
            MonitorMemory,
            null,
            TimeSpan.FromSeconds(monitorIntervalSeconds),
            TimeSpan.FromSeconds(monitorIntervalSeconds));

        ConfigureGCSettings();
    }

    /// <summary>
    /// Get current memory usage in MB
    /// </summary>
    public long GetCurrentMemoryUsageMb()
    {
        var memoryBytes = GC.GetTotalMemory(false);
        return memoryBytes / 1024 / 1024;
    }

    /// <summary>
    /// Get memory usage statistics
    /// </summary>
    public MemoryStatistics GetMemoryStatistics()
    {
        var gen0Collections = GC.CollectionCount(0);
        var gen1Collections = GC.CollectionCount(1);
        var gen2Collections = GC.CollectionCount(2);
        var totalMemory = GC.GetTotalMemory(false);
        var totalAllocated = GC.GetTotalAllocatedBytes(false);

        return new MemoryStatistics
        {
            CurrentMemoryMb = totalMemory / 1024 / 1024,
            TotalAllocatedMb = totalAllocated / 1024 / 1024,
            Gen0Collections = gen0Collections,
            Gen1Collections = gen1Collections,
            Gen2Collections = gen2Collections,
            IsServerGC = GCSettings.IsServerGC,
            LatencyMode = GCSettings.LatencyMode.ToString()
        };
    }

    /// <summary>
    /// Check if memory usage is within acceptable limits
    /// </summary>
    public bool IsMemoryUsageAcceptable()
    {
        var currentMemoryMb = GetCurrentMemoryUsageMb();
        return currentMemoryMb < _memoryLimitMb;
    }

    /// <summary>
    /// Force garbage collection if memory usage is high
    /// </summary>
    public void OptimizeMemoryIfNeeded()
    {
        var currentMemoryMb = GetCurrentMemoryUsageMb();

        if (currentMemoryMb > _gcThresholdMb)
        {
            _logger.LogInformation(
                "Memory usage ({MemoryMb} MB) exceeds threshold ({ThresholdMb} MB), triggering GC",
                currentMemoryMb, _gcThresholdMb);

            ForceGarbageCollection();
        }
    }

    /// <summary>
    /// Force full garbage collection with compaction
    /// </summary>
    public void ForceGarbageCollection()
    {
        var beforeMemory = GetCurrentMemoryUsageMb();

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

        var afterMemory = GetCurrentMemoryUsageMb();
        var freed = beforeMemory - afterMemory;

        _logger.LogInformation(
            "Garbage collection completed. Memory before: {BeforeMb} MB, after: {AfterMb} MB, freed: {FreedMb} MB",
            beforeMemory, afterMemory, freed);
    }

    /// <summary>
    /// Compact large object heap to reduce fragmentation
    /// </summary>
    public void CompactLargeObjectHeap()
    {
        _logger.LogInformation("Compacting Large Object Heap");
        
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
        
        _logger.LogInformation("Large Object Heap compaction completed");
    }

    /// <summary>
    /// Detect potential memory leaks by monitoring growth
    /// </summary>
    public bool DetectPotentialLeak()
    {
        var currentMemory = GetCurrentMemoryUsageMb();
        var growthMb = currentMemory - _lastMemoryUsage;

        if (growthMb > 50)
        {
            _consecutiveHighMemoryCount++;
            
            if (_consecutiveHighMemoryCount >= 3)
            {
                _logger.LogWarning(
                    "Potential memory leak detected. Memory has grown by {GrowthMb} MB over last 3 intervals. " +
                    "Current: {CurrentMb} MB, Previous: {PreviousMb} MB",
                    growthMb, currentMemory, _lastMemoryUsage);
                
                return true;
            }
        }
        else
        {
            _consecutiveHighMemoryCount = 0;
        }

        _lastMemoryUsage = currentMemory;
        return false;
    }

    /// <summary>
    /// Get recommendations for memory optimization
    /// </summary>
    public string[] GetOptimizationRecommendations()
    {
        var recommendations = new System.Collections.Generic.List<string>();
        var stats = GetMemoryStatistics();

        if (stats.CurrentMemoryMb > _memoryLimitMb * 0.8)
        {
            recommendations.Add("Memory usage is approaching limit. Consider closing unused resources.");
        }

        if (stats.Gen2Collections > 10)
        {
            recommendations.Add("Frequent Gen2 collections detected. Consider reducing object allocations.");
        }

        if (!stats.IsServerGC && Environment.ProcessorCount > 4)
        {
            recommendations.Add("Consider enabling Server GC for better performance on multi-core systems.");
        }

        if (stats.LatencyMode != "Batch" && stats.LatencyMode != "Interactive")
        {
            recommendations.Add("Current GC latency mode may not be optimal for this workload.");
        }

        return recommendations.ToArray();
    }

    private void ConfigureGCSettings()
    {
        try
        {
            if (Environment.ProcessorCount >= 4)
            {
                _logger.LogInformation("Configuring GC for server mode (multi-core system)");
                GCSettings.LatencyMode = GCLatencyMode.Batch;
            }
            else
            {
                _logger.LogInformation("Configuring GC for interactive mode");
                GCSettings.LatencyMode = GCLatencyMode.Interactive;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to configure GC settings");
        }
    }

    private void MonitorMemory(object? state)
    {
        try
        {
            var stats = GetMemoryStatistics();
            
            _logger.LogDebug(
                "Memory monitoring: {MemoryMb} MB used, Gen0: {Gen0}, Gen1: {Gen1}, Gen2: {Gen2}",
                stats.CurrentMemoryMb, stats.Gen0Collections, stats.Gen1Collections, stats.Gen2Collections);

            if (DetectPotentialLeak())
            {
                var recommendations = GetOptimizationRecommendations();
                foreach (var recommendation in recommendations)
                {
                    _logger.LogWarning("Memory optimization recommendation: {Recommendation}", recommendation);
                }
            }

            OptimizeMemoryIfNeeded();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during memory monitoring");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _monitorTimer?.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// Memory usage statistics
/// </summary>
public class MemoryStatistics
{
    public long CurrentMemoryMb { get; set; }
    public long TotalAllocatedMb { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
    public bool IsServerGC { get; set; }
    public string LatencyMode { get; set; } = string.Empty;

    public double MemoryPressure =>
        CurrentMemoryMb > 0 ? (double)TotalAllocatedMb / CurrentMemoryMb : 0;
}
