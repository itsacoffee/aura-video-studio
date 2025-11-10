using System;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Generation;

/// <summary>
/// Enhanced resource monitor with GPU monitoring, power mode detection, and throttling
/// Extends the base ResourceMonitor with advanced hardware detection
/// </summary>
public class EnhancedResourceMonitor : ResourceMonitor
{
    private readonly ILogger<EnhancedResourceMonitor> _logger;
    private readonly object _lock = new();
    private EnhancedResourceSnapshot _currentSnapshot;
    private DateTime _lastUpdate = DateTime.MinValue;
    private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(2);

    public EnhancedResourceMonitor(ILogger<EnhancedResourceMonitor> logger)
        : base(logger)
    {
        _logger = logger;
        _currentSnapshot = new EnhancedResourceSnapshot(0, 0, 0, 0, false, "Unknown", DateTime.UtcNow);
    }

    /// <summary>
    /// Gets the current enhanced resource utilization snapshot
    /// </summary>
    public new EnhancedResourceSnapshot GetCurrentSnapshot()
    {
        lock (_lock)
        {
            // Update snapshot if stale
            if (DateTime.UtcNow - _lastUpdate > _updateInterval)
            {
                _currentSnapshot = CaptureEnhancedSnapshot();
                _lastUpdate = DateTime.UtcNow;
            }

            return _currentSnapshot;
        }
    }

    /// <summary>
    /// Checks if system is on battery power (laptop)
    /// </summary>
    public bool IsOnBatteryPower()
    {
        try
        {
#if WINDOWS
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Use System.Windows.Forms for Windows
                var powerStatus = System.Windows.Forms.SystemInformation.PowerStatus;
                return powerStatus.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Offline;
            }
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Check /sys/class/power_supply for Linux
                return CheckLinuxBatteryStatus();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Use pmset for macOS
                return CheckMacOSBatteryStatus();
            }
#endif
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect battery status, assuming AC power");
        }

        return false; // Default to AC power
    }

    /// <summary>
    /// Gets current power mode (Performance, Balanced, PowerSaver)
    /// </summary>
    public string GetPowerMode()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsPowerMode();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect power mode");
        }

        return "Unknown";
    }

    /// <summary>
    /// Gets GPU usage percentage (Windows only for now)
    /// </summary>
    public double GetGpuUsage()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsGpuUsage();
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Failed to get GPU usage");
        }

        return 0;
    }

    /// <summary>
    /// Determines if resources are critically constrained
    /// </summary>
    public bool IsCriticallyConstrained()
    {
        var snapshot = GetCurrentSnapshot();
        
        return snapshot.CpuUsagePercent > 90 ||
               snapshot.MemoryUsagePercent > 90 ||
               (snapshot.IsOnBattery && snapshot.GpuUsagePercent > 80);
    }

    /// <summary>
    /// Gets recommended throttle level (0-3, 0=no throttle, 3=maximum throttle)
    /// </summary>
    public int GetRecommendedThrottleLevel()
    {
        var snapshot = GetCurrentSnapshot();
        
        // Critical constraints
        if (snapshot.CpuUsagePercent > 90 || snapshot.MemoryUsagePercent > 90)
            return 3;
        
        // High usage
        if (snapshot.CpuUsagePercent > 80 || snapshot.MemoryUsagePercent > 80)
            return 2;
        
        // Moderate usage
        if (snapshot.CpuUsagePercent > 70 || snapshot.MemoryUsagePercent > 70)
            return 1;
        
        // On battery with high GPU usage
        if (snapshot.IsOnBattery && snapshot.GpuUsagePercent > 60)
            return 2;
        
        return 0;
    }

    private EnhancedResourceSnapshot CaptureEnhancedSnapshot()
    {
        double cpuUsage = 0;
        double memoryUsage = 0;
        double gpuUsage = 0;
        double diskUsage = 0;
        bool isOnBattery = false;
        string powerMode = "Unknown";

        try
        {
            // Get base metrics
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

            // GPU usage
            gpuUsage = GetGpuUsage();

            // Disk I/O (process-specific)
            diskUsage = 0; // Placeholder - would need platform-specific implementation

            // Battery status
            isOnBattery = IsOnBatteryPower();

            // Power mode
            powerMode = GetPowerMode();

            _logger.LogTrace(
                "Enhanced resource snapshot: CPU={Cpu:F1}%, Memory={Memory:F1}%, GPU={Gpu:F1}%, " +
                "Battery={Battery}, PowerMode={PowerMode}",
                cpuUsage, memoryUsage, gpuUsage, isOnBattery, powerMode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture enhanced resource snapshot, using defaults");
            // Return conservative estimates on error
            cpuUsage = 50;
            memoryUsage = 50;
            gpuUsage = 0;
        }

        return new EnhancedResourceSnapshot(
            cpuUsage, memoryUsage, gpuUsage, diskUsage,
            isOnBattery, powerMode, DateTime.UtcNow);
    }

    private double GetWindowsGpuUsage()
    {
        try
        {
            // Use WMI to query GPU usage (Windows only)
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine");
            var collection = searcher.Get();
            
            double totalUsage = 0;
            int count = 0;
            
            foreach (var obj in collection)
            {
                var usage = Convert.ToDouble(obj["UtilizationPercentage"]);
                totalUsage += usage;
                count++;
            }
            
            return count > 0 ? totalUsage / count : 0;
        }
        catch
        {
            // GPU monitoring not available
            return 0;
        }
    }

    private string GetWindowsPowerMode()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PowerPlan WHERE IsActive=TRUE");
            foreach (var obj in searcher.Get())
            {
                var name = obj["ElementName"]?.ToString() ?? "Unknown";
                
                if (name.Contains("High performance", StringComparison.OrdinalIgnoreCase))
                    return "Performance";
                if (name.Contains("Power saver", StringComparison.OrdinalIgnoreCase))
                    return "PowerSaver";
                if (name.Contains("Balanced", StringComparison.OrdinalIgnoreCase))
                    return "Balanced";
                
                return name;
            }
        }
        catch
        {
            // Power mode detection not available
        }
        
        return "Unknown";
    }

    private bool CheckLinuxBatteryStatus()
    {
        try
        {
            var batteryPath = "/sys/class/power_supply/BAT0/status";
            if (System.IO.File.Exists(batteryPath))
            {
                var status = System.IO.File.ReadAllText(batteryPath).Trim();
                return status.Equals("Discharging", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch
        {
            // Battery status not available
        }
        
        return false;
    }

    private bool CheckMacOSBatteryStatus()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "pmset",
                Arguments = "-g batt",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                
                return output.Contains("discharging", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch
        {
            // Battery status not available
        }
        
        return false;
    }
}

/// <summary>
/// Enhanced snapshot of system resource utilization
/// </summary>
public record EnhancedResourceSnapshot(
    double CpuUsagePercent,
    double MemoryUsagePercent,
    double GpuUsagePercent,
    double DiskUsagePercent,
    bool IsOnBattery,
    string PowerMode,
    DateTime Timestamp);
