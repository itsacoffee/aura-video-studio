using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Resources;

/// <summary>
/// Service for monitoring system and process resource usage
/// </summary>
public class SystemResourceMonitor
{
    private readonly ILogger<SystemResourceMonitor> _logger;
    private readonly Process _currentProcess;
    private readonly PerformanceCounter? _cpuCounter;
    private readonly Dictionary<string, PerformanceCounter> _perCoreCounters = new();
    private DateTime _lastNetworkUpdate = DateTime.MinValue;
    private long _lastBytesSent;
    private long _lastBytesReceived;
    private readonly object _metricsLock = new();
    private SystemResourceMetrics? _lastSystemMetrics;
    private ProcessMetrics? _lastProcessMetrics;
    
    // CPU tracking for delta-based calculation (used for WMI fallback and Linux)
    private DateTime _lastCpuUpdate = DateTime.MinValue;
    private long _lastCpuTotal;
    private long _lastCpuIdle;
    private double _lastCpuUsagePercent;
    
    // Track if performance counters initialized successfully
    private bool _cpuCounterInitialized;
    private bool _cpuCounterPrimed;

    public SystemResourceMonitor(ILogger<SystemResourceMonitor> logger)
    {
        _logger = logger;
        _currentProcess = Process.GetCurrentProcess();

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                // Prime the counter - first call always returns 0
                _cpuCounter.NextValue();
                _cpuCounterInitialized = true;
                _logger.LogDebug("CPU performance counter initialized successfully");
                InitializePerCoreCounters();
            }
        }
        catch (Exception ex)
        {
            _cpuCounterInitialized = false;
            _logger.LogWarning(ex, "Failed to initialize performance counters. Will use WMI fallback for CPU metrics.");
        }
    }

    private void InitializePerCoreCounters()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        try
        {
            var coreCount = Environment.ProcessorCount;
            for (int i = 0; i < coreCount; i++)
            {
                var counter = new PerformanceCounter("Processor", "% Processor Time", i.ToString(), true);
                _perCoreCounters[i.ToString()] = counter;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize per-core performance counters");
        }
    }

    /// <summary>
    /// Collects current system resource metrics
    /// </summary>
    public async Task<SystemResourceMetrics> CollectSystemMetricsAsync(CancellationToken cancellationToken = default)
    {
        var metrics = new SystemResourceMetrics
        {
            Timestamp = DateTime.UtcNow,
            Cpu = await CollectCpuMetricsAsync(cancellationToken).ConfigureAwait(false),
            Memory = CollectMemoryMetrics(),
            Gpu = await CollectGpuMetricsAsync(cancellationToken).ConfigureAwait(false),
            Disks = CollectDiskMetrics(),
            Network = await CollectNetworkMetricsAsync(cancellationToken).ConfigureAwait(false)
        };

        lock (_metricsLock)
        {
            _lastSystemMetrics = metrics;
        }

        return metrics;
    }

    /// <summary>
    /// Collects process-specific metrics
    /// </summary>
    public ProcessMetrics CollectProcessMetrics()
    {
        var metrics = new ProcessMetrics
        {
            Timestamp = DateTime.UtcNow,
            ThreadPool = CollectThreadPoolMetrics(),
            CacheMemoryBytes = GC.GetTotalMemory(false),
            ComponentMemory = CollectComponentMemoryMetrics(),
            ProviderConnections = CollectProviderConnectionMetrics()
        };

        lock (_metricsLock)
        {
            _lastProcessMetrics = metrics;
        }

        return metrics;
    }

    /// <summary>
    /// Gets the last collected system metrics without triggering a new collection
    /// </summary>
    public SystemResourceMetrics? GetLastSystemMetrics()
    {
        lock (_metricsLock)
        {
            return _lastSystemMetrics;
        }
    }

    /// <summary>
    /// Gets the last collected process metrics without triggering a new collection
    /// </summary>
    public ProcessMetrics? GetLastProcessMetrics()
    {
        lock (_metricsLock)
        {
            return _lastProcessMetrics;
        }
    }

    private async Task<CpuMetrics> CollectCpuMetricsAsync(CancellationToken cancellationToken)
    {
        var metrics = new CpuMetrics
        {
            LogicalCores = Environment.ProcessorCount,
            PhysicalCores = Environment.ProcessorCount / 2 // Rough estimate
        };

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Try performance counter first (most accurate)
                var cpuUsage = await TryGetCpuFromPerformanceCounterAsync(cancellationToken).ConfigureAwait(false);
                
                if (cpuUsage.HasValue)
                {
                    metrics.OverallUsagePercent = cpuUsage.Value;
                    _logger.LogTrace("CPU usage from performance counter: {Usage}%", cpuUsage.Value);
                }
                else
                {
                    // Fallback to WMI for Windows 11 compatibility
                    cpuUsage = await TryGetCpuFromWmiAsync(cancellationToken).ConfigureAwait(false);
                    if (cpuUsage.HasValue)
                    {
                        metrics.OverallUsagePercent = cpuUsage.Value;
                        _logger.LogTrace("CPU usage from WMI: {Usage}%", cpuUsage.Value);
                    }
                    else
                    {
                        _logger.LogDebug("All CPU collection methods failed, using last known value");
                        metrics.OverallUsagePercent = _lastCpuUsagePercent;
                    }
                }

                // Collect per-core usage (best effort)
                var perCoreUsage = await CollectPerCoreUsageAsync(cancellationToken).ConfigureAwait(false);
                metrics.PerCoreUsagePercent = perCoreUsage;
            }
            else
            {
                metrics.OverallUsagePercent = GetCpuUsageNonWindows();
            }

            // Process CPU usage calculation
            _currentProcess.Refresh();
            var totalTime = _currentProcess.TotalProcessorTime.TotalMilliseconds;
            var elapsedTime = (DateTime.UtcNow - _currentProcess.StartTime).TotalMilliseconds;
            if (elapsedTime > 0 && metrics.LogicalCores > 0)
            {
                metrics.ProcessUsagePercent = (totalTime / (elapsedTime * metrics.LogicalCores)) * 100.0;
                metrics.ProcessUsagePercent = Math.Min(100.0, Math.Max(0.0, metrics.ProcessUsagePercent));
            }
            
            // Update last known good value
            if (metrics.OverallUsagePercent > 0)
            {
                _lastCpuUsagePercent = metrics.OverallUsagePercent;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect CPU metrics, using fallback values");
        }

        return metrics;
    }

    /// <summary>
    /// Attempts to get CPU usage from Windows Performance Counters.
    /// This is the preferred method but may fail on some Windows 11 systems.
    /// </summary>
    private async Task<double?> TryGetCpuFromPerformanceCounterAsync(CancellationToken cancellationToken)
    {
        if (!_cpuCounterInitialized || _cpuCounter == null)
        {
            return null;
        }

        try
        {
            // If this is the first read after priming, we need a delay
            if (!_cpuCounterPrimed)
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                _cpuCounterPrimed = true;
            }
            
            var value = _cpuCounter.NextValue();
            
            // Validate the value is reasonable
            if (value >= 0 && value <= 100)
            {
                return value;
            }
            
            _logger.LogDebug("Performance counter returned invalid CPU value: {Value}", value);
            return null;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogDebug(ex, "Performance counter not available, will use WMI fallback");
            _cpuCounterInitialized = false;
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to read CPU from performance counter");
            return null;
        }
    }

    /// <summary>
    /// Attempts to get CPU usage via WMI (Windows Management Instrumentation).
    /// This is a fallback method that works on Windows 11 when Performance Counters fail.
    /// Uses Win32_PerfFormattedData_PerfOS_Processor for reliable CPU metrics.
    /// </summary>
    private async Task<double?> TryGetCpuFromWmiAsync(CancellationToken cancellationToken)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return null;
        }

        try
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Try Win32_PerfFormattedData_PerfOS_Processor first (more accurate)
                using var searcher = new ManagementObjectSearcher(
                    "SELECT PercentProcessorTime FROM Win32_PerfFormattedData_PerfOS_Processor WHERE Name='_Total'");
                
                foreach (ManagementObject obj in searcher.Get())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var percentProcessorTime = obj["PercentProcessorTime"];
                    if (percentProcessorTime != null)
                    {
                        var value = Convert.ToDouble(percentProcessorTime);
                        if (value >= 0 && value <= 100)
                        {
                            return (double?)value;
                        }
                    }
                }

                // Fallback to Win32_Processor.LoadPercentage
                using var processorSearcher = new ManagementObjectSearcher(
                    "SELECT LoadPercentage FROM Win32_Processor");
                
                double totalLoad = 0;
                int processorCount = 0;
                
                foreach (ManagementObject obj in processorSearcher.Get())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var loadPercentage = obj["LoadPercentage"];
                    if (loadPercentage != null)
                    {
                        totalLoad += Convert.ToDouble(loadPercentage);
                        processorCount++;
                    }
                }

                if (processorCount > 0)
                {
                    return (double?)(totalLoad / processorCount);
                }

                return null;
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "WMI CPU query failed");
            return null;
        }
    }

    /// <summary>
    /// Collects per-core CPU usage (best effort, returns empty array on failure).
    /// </summary>
    private async Task<double[]> CollectPerCoreUsageAsync(CancellationToken cancellationToken)
    {
        if (_perCoreCounters.Count == 0)
        {
            return Array.Empty<double>();
        }

        try
        {
            var perCoreUsage = new List<double>();
            
            foreach (var counter in _perCoreCounters.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                    var value = counter.NextValue();
                    perCoreUsage.Add(Math.Max(0, Math.Min(100, value)));
                }
                catch
                {
                    // Skip failed cores
                }
            }
            
            return perCoreUsage.ToArray();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to collect per-core CPU usage");
            return Array.Empty<double>();
        }
    }

    private double GetCpuUsageNonWindows()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Read only the first few lines from /proc/stat (more efficient than ReadAllLines)
                var statLine = File.ReadLines("/proc/stat").FirstOrDefault(l => l.StartsWith("cpu ", StringComparison.Ordinal));
                if (statLine != null)
                {
                    var parts = statLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 5)
                    {
                        // cpu user nice system idle iowait irq softirq steal guest guest_nice
                        var user = long.TryParse(parts[1], out var u) ? u : 0;
                        var nice = long.TryParse(parts[2], out var n) ? n : 0;
                        var system = long.TryParse(parts[3], out var s) ? s : 0;
                        var idle = long.TryParse(parts[4], out var i) ? i : 0;
                        var iowait = parts.Length > 5 && long.TryParse(parts[5], out var w) ? w : 0;
                        var irq = parts.Length > 6 && long.TryParse(parts[6], out var q) ? q : 0;
                        var softirq = parts.Length > 7 && long.TryParse(parts[7], out var sq) ? sq : 0;
                        var steal = parts.Length > 8 && long.TryParse(parts[8], out var st) ? st : 0;
                        
                        var totalIdle = idle + iowait;
                        var total = user + nice + system + idle + iowait + irq + softirq + steal;
                        
                        // Calculate CPU usage based on delta since last reading
                        if (_lastCpuUpdate != DateTime.MinValue && _lastCpuTotal > 0)
                        {
                            var deltaTotal = total - _lastCpuTotal;
                            var deltaIdle = totalIdle - _lastCpuIdle;
                            
                            if (deltaTotal > 0)
                            {
                                _lastCpuUsagePercent = ((double)(deltaTotal - deltaIdle) / deltaTotal) * 100.0;
                                _lastCpuUsagePercent = Math.Max(0, Math.Min(100, _lastCpuUsagePercent));
                            }
                        }
                        
                        // Store current values for next calculation
                        _lastCpuTotal = total;
                        _lastCpuIdle = totalIdle;
                        _lastCpuUpdate = DateTime.UtcNow;
                        
                        return _lastCpuUsagePercent;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get CPU usage on non-Windows platform");
        }

        return _lastCpuUsagePercent;
    }

    private MemoryMetrics CollectMemoryMetrics()
    {
        var metrics = new MemoryMetrics();

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Try GlobalMemoryStatusEx first (P/Invoke)
                var memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memStatus))
                {
                    metrics.TotalBytes = (long)memStatus.ullTotalPhys;
                    metrics.AvailableBytes = (long)memStatus.ullAvailPhys;
                    metrics.UsedBytes = metrics.TotalBytes - metrics.AvailableBytes;
                    if (metrics.TotalBytes > 0)
                    {
                        metrics.UsagePercent = ((double)metrics.UsedBytes / metrics.TotalBytes) * 100.0;
                    }
                    _logger.LogTrace("Memory metrics from GlobalMemoryStatusEx: Total={Total}, Used={Used}, Usage={Usage}%", 
                        metrics.TotalBytes, metrics.UsedBytes, metrics.UsagePercent);
                }
                else
                {
                    _logger.LogDebug("GlobalMemoryStatusEx returned false, trying WMI fallback");
                    // Fallback to WMI for Windows 11 compatibility
                    var wmiMemory = TryGetMemoryFromWmi();
                    if (wmiMemory.HasValue)
                    {
                        metrics.TotalBytes = wmiMemory.Value.total;
                        metrics.AvailableBytes = wmiMemory.Value.available;
                        metrics.UsedBytes = wmiMemory.Value.used;
                        metrics.UsagePercent = wmiMemory.Value.usagePercent;
                        _logger.LogTrace("Memory metrics from WMI: Total={Total}, Used={Used}, Usage={Usage}%", 
                            metrics.TotalBytes, metrics.UsedBytes, metrics.UsagePercent);
                    }
                    else
                    {
                        _logger.LogWarning("All memory collection methods failed");
                    }
                }
            }
            else
            {
                var memInfo = GetMemoryInfoNonWindows();
                metrics.TotalBytes = memInfo.total;
                metrics.AvailableBytes = memInfo.available;
                metrics.UsedBytes = memInfo.used;
                if (metrics.TotalBytes > 0)
                {
                    metrics.UsagePercent = ((double)metrics.UsedBytes / metrics.TotalBytes) * 100.0;
                }
            }

            // Process memory metrics (always available)
            _currentProcess.Refresh();
            metrics.ProcessUsageBytes = _currentProcess.WorkingSet64;
            metrics.ProcessPrivateBytes = _currentProcess.PrivateMemorySize64;
            metrics.ProcessWorkingSetBytes = _currentProcess.WorkingSet64;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect memory metrics");
            
            // Last resort fallback: use GC info for process memory
            try
            {
                var gcMemoryInfo = GC.GetGCMemoryInfo();
                metrics.ProcessUsageBytes = GC.GetTotalMemory(false);
                metrics.ProcessWorkingSetBytes = gcMemoryInfo.TotalCommittedBytes;
            }
            catch
            {
                // Silently ignore - we've already logged the primary failure
            }
        }

        return metrics;
    }

    /// <summary>
    /// Attempts to get memory information via WMI as a fallback.
    /// </summary>
    private (long total, long available, long used, double usagePercent)? TryGetMemoryFromWmi()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return null;
        }

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
            
            foreach (ManagementObject obj in searcher.Get())
            {
                var totalKb = obj["TotalVisibleMemorySize"];
                var freeKb = obj["FreePhysicalMemory"];
                
                if (totalKb != null && freeKb != null)
                {
                    var total = Convert.ToInt64(totalKb) * 1024; // KB to bytes
                    var available = Convert.ToInt64(freeKb) * 1024; // KB to bytes
                    var used = total - available;
                    var usagePercent = total > 0 ? ((double)used / total) * 100.0 : 0.0;
                    
                    return (total, available, used, usagePercent);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "WMI memory query failed");
        }

        return null;
    }

    private (long total, long available, long used) GetMemoryInfoNonWindows()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var lines = File.ReadAllLines("/proc/meminfo");
                long total = 0, available = 0;

                foreach (var line in lines)
                {
                    if (line.StartsWith("MemTotal:", StringComparison.Ordinal))
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && long.TryParse(parts[1], out var value))
                        {
                            total = value * 1024; // Convert from KB to bytes
                        }
                    }
                    else if (line.StartsWith("MemAvailable:", StringComparison.Ordinal))
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && long.TryParse(parts[1], out var value))
                        {
                            available = value * 1024; // Convert from KB to bytes
                        }
                    }
                }

                return (total, available, total - available);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to read memory info on non-Windows platform");
        }

        return (0, 0, 0);
    }

    private async Task<GpuMetrics?> CollectGpuMetricsAsync(CancellationToken cancellationToken)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return await CollectGpuMetricsLinuxAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            // Try to get GPU info from WMI first
            string gpuName = "Unknown GPU";
            string gpuVendor = "Unknown";
            long adapterRam = 0;
            bool foundGpu = false;

            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController");
                foreach (ManagementObject obj in searcher.Get())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var name = obj["Name"]?.ToString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        gpuName = name;
                        gpuVendor = DetermineVendor(name);
                        adapterRam = obj["AdapterRAM"] != null ? Convert.ToInt64(obj["AdapterRAM"]) : 0;
                        foundGpu = true;
                        _logger.LogTrace("Found GPU via WMI: {Name}, Vendor: {Vendor}, RAM: {Ram}", gpuName, gpuVendor, adapterRam);
                        break; // Use first GPU found
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "WMI GPU query failed, continuing with performance counters");
            }

            var metrics = new GpuMetrics
            {
                Name = gpuName,
                Vendor = gpuVendor,
                TotalMemoryBytes = adapterRam
            };

            // Try Windows GPU Engine performance counters first (works for all GPU vendors)
            var perfCounterUsage = await TryGetGpuUsageFromPerformanceCountersAsync(cancellationToken).ConfigureAwait(false);
            if (perfCounterUsage.HasValue)
            {
                metrics.UsagePercent = perfCounterUsage.Value;
                _logger.LogTrace("GPU usage from performance counters: {Usage}%", perfCounterUsage.Value);
                return metrics;
            }

            // Fall back to nvidia-smi for NVIDIA GPUs
            var nvidiaSmiMetrics = await TryGetNvidiaSmiMetricsAsync(cancellationToken).ConfigureAwait(false);
            if (nvidiaSmiMetrics != null)
            {
                metrics.UsagePercent = nvidiaSmiMetrics.Value.usage;
                metrics.UsedMemoryBytes = nvidiaSmiMetrics.Value.usedMemory;
                metrics.TotalMemoryBytes = nvidiaSmiMetrics.Value.totalMemory > 0 
                    ? nvidiaSmiMetrics.Value.totalMemory 
                    : adapterRam;
                metrics.AvailableMemoryBytes = metrics.TotalMemoryBytes - metrics.UsedMemoryBytes;
                metrics.MemoryUsagePercent = metrics.TotalMemoryBytes > 0
                    ? ((double)metrics.UsedMemoryBytes / metrics.TotalMemoryBytes) * 100.0
                    : 0.0;
                metrics.TemperatureCelsius = nvidiaSmiMetrics.Value.temperature;
                if (string.IsNullOrEmpty(metrics.Name) || metrics.Name == "Unknown GPU")
                {
                    metrics.Name = "NVIDIA GPU";
                    metrics.Vendor = "NVIDIA";
                }
                _logger.LogTrace("GPU usage from nvidia-smi: {Usage}%", nvidiaSmiMetrics.Value.usage);
                return metrics;
            }

            // If we found a GPU via WMI but couldn't get usage, still return the metrics
            // with 0% usage (better than returning null)
            if (foundGpu)
            {
                _logger.LogDebug("GPU found but usage metrics unavailable - returning GPU info with 0% usage");
                return metrics;
            }

            _logger.LogDebug("No GPU detected on this system");
            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to collect GPU metrics");
            return null;
        }
    }

    /// <summary>
    /// Tries to get GPU utilization using Windows Performance Counters (GPU Engine category).
    /// This method works for NVIDIA, AMD, and Intel GPUs on Windows 10/11.
    /// </summary>
    private async Task<double?> TryGetGpuUsageFromPerformanceCountersAsync(CancellationToken cancellationToken)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return null;
        }

        try
        {
            return await Task.Run(() =>
            {
                // The "GPU Engine" performance counter category provides real-time GPU utilization
                // Each instance represents a GPU engine (3D, Copy, Video Decode, etc.)
                // We sum up all engine utilizations to get overall GPU usage
                var category = new PerformanceCounterCategory("GPU Engine");
                var instanceNames = category.GetInstanceNames();

                if (instanceNames.Length == 0)
                {
                    _logger.LogDebug("No GPU Engine performance counter instances found");
                    return (double?)null;
                }

                double totalUtilization = 0.0;
                int validCounters = 0;

                foreach (var instanceName in instanceNames)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        using var counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", instanceName, true);
                        // First call initializes the counter and returns 0; result is intentionally discarded
                        // A subsequent call after a delay will return the actual utilization value
                        _ = counter.NextValue();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogTrace(ex, "Failed to read GPU Engine counter for instance {Instance}", instanceName);
                    }
                }

                // Small delay to allow counters to accumulate data (this is inside Task.Run so Thread.Sleep is acceptable)
                cancellationToken.ThrowIfCancellationRequested();
                Thread.Sleep(50);

                foreach (var instanceName in instanceNames)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        using var counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", instanceName, true);
                        var value = counter.NextValue();
                        if (value >= 0)
                        {
                            totalUtilization += value;
                            validCounters++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogTrace(ex, "Failed to read GPU Engine counter for instance {Instance}", instanceName);
                    }
                }

                // GPU Engine counters report per-engine utilization, cap at 100%
                return validCounters > 0 ? Math.Min(100.0, totalUtilization) : (double?)null;
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "GPU Engine performance counters not available");
            return null;
        }
    }

    private async Task<GpuMetrics?> CollectGpuMetricsLinuxAsync(CancellationToken cancellationToken)
    {
        var nvidiaSmiMetrics = await TryGetNvidiaSmiMetricsAsync(cancellationToken).ConfigureAwait(false);
        if (nvidiaSmiMetrics != null)
        {
            return new GpuMetrics
            {
                Name = "NVIDIA GPU",
                Vendor = "NVIDIA",
                UsagePercent = nvidiaSmiMetrics.Value.usage,
                UsedMemoryBytes = nvidiaSmiMetrics.Value.usedMemory,
                TotalMemoryBytes = nvidiaSmiMetrics.Value.totalMemory,
                AvailableMemoryBytes = nvidiaSmiMetrics.Value.totalMemory - nvidiaSmiMetrics.Value.usedMemory,
                MemoryUsagePercent = nvidiaSmiMetrics.Value.totalMemory > 0
                    ? ((double)nvidiaSmiMetrics.Value.usedMemory / nvidiaSmiMetrics.Value.totalMemory) * 100.0
                    : 0.0,
                TemperatureCelsius = nvidiaSmiMetrics.Value.temperature
            };
        }

        return null;
    }

    private async Task<(double usage, long usedMemory, long totalMemory, double temperature)?> TryGetNvidiaSmiMetricsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var output = await Task.Run(() => 
                ExecuteCommand("nvidia-smi", "--query-gpu=utilization.gpu,memory.used,memory.total,temperature.gpu --format=csv,noheader,nounits"),
                cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(output))
            {
                var parts = output.Trim().Split(',');
                if (parts.Length >= 4)
                {
                    var usage = double.TryParse(parts[0].Trim(), out var u) ? u : 0.0;
                    var usedMem = long.TryParse(parts[1].Trim(), out var um) ? um * 1024 * 1024 : 0L;
                    var totalMem = long.TryParse(parts[2].Trim(), out var tm) ? tm * 1024 * 1024 : 0L;
                    var temp = double.TryParse(parts[3].Trim(), out var t) ? t : 0.0;

                    return (usage, usedMem, totalMem, temp);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "nvidia-smi not available or failed");
        }

        return null;
    }

    private string DetermineVendor(string name)
    {
        var nameLower = name.ToLowerInvariant();
        if (nameLower.Contains("nvidia") || nameLower.Contains("geforce") || nameLower.Contains("rtx") || nameLower.Contains("gtx"))
            return "NVIDIA";
        if (nameLower.Contains("amd") || nameLower.Contains("radeon"))
            return "AMD";
        if (nameLower.Contains("intel"))
            return "Intel";

        return "Unknown";
    }

    private DiskMetrics[] CollectDiskMetrics()
    {
        var metrics = new List<DiskMetrics>();

        try
        {
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                if (!drive.IsReady)
                    continue;

                metrics.Add(new DiskMetrics
                {
                    DriveName = drive.Name,
                    DriveLabel = drive.VolumeLabel,
                    TotalBytes = drive.TotalSize,
                    AvailableBytes = drive.AvailableFreeSpace,
                    UsedBytes = drive.TotalSize - drive.AvailableFreeSpace,
                    UsagePercent = ((double)(drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize) * 100.0
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect disk metrics");
        }

        return metrics.ToArray();
    }

    private async Task<NetworkMetrics> CollectNetworkMetricsAsync(CancellationToken cancellationToken)
    {
        var metrics = new NetworkMetrics();

        try
        {
            await Task.Run(() =>
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();
                long totalBytesSent = 0;
                long totalBytesReceived = 0;

                foreach (var iface in interfaces)
                {
                    if (iface.OperationalStatus != OperationalStatus.Up)
                        continue;

                    var stats = iface.GetIPv4Statistics();
                    totalBytesSent += stats.BytesSent;
                    totalBytesReceived += stats.BytesReceived;
                }

                metrics.TotalBytesSent = totalBytesSent;
                metrics.TotalBytesReceived = totalBytesReceived;

                var now = DateTime.UtcNow;
                if (_lastNetworkUpdate != DateTime.MinValue)
                {
                    var elapsedSeconds = (now - _lastNetworkUpdate).TotalSeconds;
                    if (elapsedSeconds > 0)
                    {
                        metrics.BytesSentPerSecond = (long)((totalBytesSent - _lastBytesSent) / elapsedSeconds);
                        metrics.BytesReceivedPerSecond = (long)((totalBytesReceived - _lastBytesReceived) / elapsedSeconds);
                    }
                }

                _lastBytesSent = totalBytesSent;
                _lastBytesReceived = totalBytesReceived;
                _lastNetworkUpdate = now;
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect network metrics");
        }

        return metrics;
    }

    private ThreadPoolMetrics CollectThreadPoolMetrics()
    {
        ThreadPool.GetAvailableThreads(out int availableWorkerThreads, out int availableCompletionPortThreads);
        ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);

        return new ThreadPoolMetrics
        {
            AvailableWorkerThreads = availableWorkerThreads,
            AvailableCompletionPortThreads = availableCompletionPortThreads,
            MaxWorkerThreads = maxWorkerThreads,
            MaxCompletionPortThreads = maxCompletionPortThreads,
            BusyWorkerThreads = maxWorkerThreads - availableWorkerThreads,
            BusyCompletionPortThreads = maxCompletionPortThreads - availableCompletionPortThreads
        };
    }

    private ComponentMemoryMetrics[] CollectComponentMemoryMetrics()
    {
        var metrics = new List<ComponentMemoryMetrics>();

        try
        {
            var gen0 = GC.CollectionCount(0);
            var gen1 = GC.CollectionCount(1);
            var gen2 = GC.CollectionCount(2);

            metrics.Add(new ComponentMemoryMetrics
            {
                ComponentName = "GC.Gen0",
                AllocatedBytes = GC.GetTotalMemory(false),
                ActiveInstances = gen0
            });

            metrics.Add(new ComponentMemoryMetrics
            {
                ComponentName = "GC.Gen1",
                AllocatedBytes = GC.GetTotalMemory(false),
                ActiveInstances = gen1
            });

            metrics.Add(new ComponentMemoryMetrics
            {
                ComponentName = "GC.Gen2",
                AllocatedBytes = GC.GetTotalMemory(false),
                ActiveInstances = gen2
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect component memory metrics");
        }

        return metrics.ToArray();
    }

    private ProviderConnectionMetrics[] CollectProviderConnectionMetrics()
    {
        return Array.Empty<ProviderConnectionMetrics>();
    }

    private string ExecuteCommand(string command, string arguments)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
                return string.Empty;

            using var reader = process.StandardOutput;
            var result = reader.ReadToEnd();
            process.WaitForExit(5000);
            return result;
        }
        catch
        {
            return string.Empty;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);
}
