using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Aura.Core.Services.Diagnostics;

/// <summary>
/// Service for collecting comprehensive system diagnostic information
/// </summary>
public class SystemDiagnosticsService
{
    private readonly ILogger<SystemDiagnosticsService> _logger;

    public SystemDiagnosticsService(ILogger<SystemDiagnosticsService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Collect comprehensive system diagnostics
    /// </summary>
    public async Task<SystemDiagnostics> CollectDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Collecting system diagnostics...");

        var diagnostics = new SystemDiagnostics
        {
            Timestamp = DateTime.UtcNow,
            OperatingSystem = await CollectOperatingSystemInfoAsync(cancellationToken).ConfigureAwait(false),
            Hardware = await CollectHardwareInfoAsync(cancellationToken).ConfigureAwait(false),
            Runtime = CollectRuntimeInfo(),
            Process = await CollectProcessInfoAsync(cancellationToken).ConfigureAwait(false),
            Environment = CollectEnvironmentInfo(),
            Network = await CollectNetworkInfoAsync(cancellationToken).ConfigureAwait(false),
            Disk = await CollectDiskInfoAsync(cancellationToken).ConfigureAwait(false)
        };

        _logger.LogInformation("System diagnostics collected successfully");
        return diagnostics;
    }

    /// <summary>
    /// Collect operating system information
    /// </summary>
    private async Task<OperatingSystemInfo> CollectOperatingSystemInfoAsync(CancellationToken cancellationToken)
    {
        var os = new OperatingSystemInfo
        {
            Platform = RuntimeInformation.OSDescription,
            Architecture = RuntimeInformation.OSArchitecture.ToString(),
            ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            Version = Environment.OSVersion.Version.ToString(),
            VersionString = Environment.OSVersion.VersionString,
            Is64BitOperatingSystem = Environment.Is64BitOperatingSystem,
            MachineName = Environment.MachineName
        };

        // Get Windows-specific information
        if (OperatingSystem.IsWindows())
        {
            try
            {
                os.WindowsVersion = await GetWindowsVersionAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get Windows version details");
            }
        }

        return os;
    }

    /// <summary>
    /// Get detailed Windows version information
    /// </summary>
    private async Task<string> GetWindowsVersionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c ver",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return "Unknown";

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            return output.Trim();
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// Collect hardware information
    /// </summary>
    private async Task<HardwareInfo> CollectHardwareInfoAsync(CancellationToken cancellationToken)
    {
        var hardware = new HardwareInfo
        {
            ProcessorCount = Environment.ProcessorCount,
            TotalMemoryGB = GetTotalPhysicalMemoryGB()
        };

        // Get CPU information
        if (OperatingSystem.IsWindows())
        {
            hardware.ProcessorName = await GetWindowsProcessorNameAsync(cancellationToken).ConfigureAwait(false);
        }
        else if (OperatingSystem.IsLinux())
        {
            hardware.ProcessorName = await GetLinuxProcessorNameAsync(cancellationToken).ConfigureAwait(false);
        }

        return hardware;
    }

    /// <summary>
    /// Get total physical memory in GB
    /// </summary>
    private double GetTotalPhysicalMemoryGB()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            var workingSet = process.WorkingSet64;
            // This is an approximation - for accurate memory, we'd need platform-specific code
            return Math.Round(workingSet / (1024.0 * 1024.0 * 1024.0), 2);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Get Windows processor name
    /// </summary>
    private async Task<string> GetWindowsProcessorNameAsync(CancellationToken cancellationToken)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "wmic",
                Arguments = "cpu get name",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return "Unknown";

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            return lines.Length > 1 ? lines[1].Trim() : "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// Get Linux processor name
    /// </summary>
    private async Task<string> GetLinuxProcessorNameAsync(CancellationToken cancellationToken)
    {
        try
        {
            var content = await File.ReadAllTextAsync("/proc/cpuinfo", cancellationToken).ConfigureAwait(false);
            var lines = content.Split('\n');
            var modelLine = lines.FirstOrDefault(l => l.StartsWith("model name"));
            if (modelLine != null)
            {
                var parts = modelLine.Split(':');
                return parts.Length > 1 ? parts[1].Trim() : "Unknown";
            }
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// Collect .NET runtime information
    /// </summary>
    private RuntimeInfo CollectRuntimeInfo()
    {
        return new RuntimeInfo
        {
            FrameworkDescription = RuntimeInformation.FrameworkDescription,
            RuntimeIdentifier = RuntimeInformation.RuntimeIdentifier,
            DotNetVersion = Environment.Version.ToString(),
            Is64BitProcess = Environment.Is64BitProcess,
            CurrentDirectory = Environment.CurrentDirectory,
            BaseDirectory = AppDomain.CurrentDomain.BaseDirectory
        };
    }

    /// <summary>
    /// Collect current process information
    /// </summary>
    private async Task<ProcessInfo> CollectProcessInfoAsync(CancellationToken cancellationToken)
    {
        using var process = Process.GetCurrentProcess();
        
        var info = new ProcessInfo
        {
            ProcessId = process.Id,
            ProcessName = process.ProcessName,
            StartTime = process.StartTime,
            UpTime = DateTime.Now - process.StartTime,
            ThreadCount = process.Threads.Count,
            HandleCount = process.HandleCount,
            WorkingSetMB = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 2),
            PrivateMemoryMB = Math.Round(process.PrivateMemorySize64 / (1024.0 * 1024.0), 2),
            VirtualMemoryMB = Math.Round(process.VirtualMemorySize64 / (1024.0 * 1024.0), 2)
        };

        // Collect CPU usage
        try
        {
            var startCpuUsage = process.TotalProcessorTime;
            var startTime = DateTime.UtcNow;

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);

            var endCpuUsage = process.TotalProcessorTime;
            var endTime = DateTime.UtcNow;

            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

            info.CpuUsagePercent = Math.Round(cpuUsageTotal * 100, 2);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to calculate CPU usage");
        }

        return info;
    }

    /// <summary>
    /// Collect environment variables (sanitized)
    /// </summary>
    private EnvironmentInfo CollectEnvironmentInfo()
    {
        var info = new EnvironmentInfo
        {
            UserName = Environment.UserName,
            UserDomainName = Environment.UserDomainName,
            SystemDirectory = Environment.SystemDirectory,
            CurrentDirectory = Environment.CurrentDirectory,
            CommandLine = Environment.CommandLine,
            TickCount = Environment.TickCount64
        };

        // Collect relevant environment variables (sanitize sensitive ones)
        var envVars = Environment.GetEnvironmentVariables();
        foreach (var key in envVars.Keys)
        {
            var keyStr = key.ToString() ?? "";
            var value = envVars[key]?.ToString() ?? "";

            // Skip sensitive variables
            if (keyStr.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase) ||
                keyStr.Contains("SECRET", StringComparison.OrdinalIgnoreCase) ||
                keyStr.Contains("KEY", StringComparison.OrdinalIgnoreCase) ||
                keyStr.Contains("TOKEN", StringComparison.OrdinalIgnoreCase))
            {
                info.EnvironmentVariables[keyStr] = "[REDACTED]";
            }
            else
            {
                info.EnvironmentVariables[keyStr] = value;
            }
        }

        return info;
    }

    /// <summary>
    /// Collect network information
    /// </summary>
    private async Task<NetworkInfo> CollectNetworkInfoAsync(CancellationToken cancellationToken)
    {
        var info = new NetworkInfo();

        try
        {
            var hostName = System.Net.Dns.GetHostName();
            info.HostName = hostName;

            var hostEntry = await System.Net.Dns.GetHostEntryAsync(hostName, cancellationToken).ConfigureAwait(false);
            info.IPAddresses = hostEntry.AddressList
                .Select(ip => ip.ToString())
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect network information");
        }

        return info;
    }

    /// <summary>
    /// Collect disk information
    /// </summary>
    private async Task<DiskInfo> CollectDiskInfoAsync(CancellationToken cancellationToken)
    {
        var info = new DiskInfo();

        try
        {
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives.Where(d => d.IsReady))
            {
                info.Drives.Add(new DriveInfoData
                {
                    Name = drive.Name,
                    DriveType = drive.DriveType.ToString(),
                    DriveFormat = drive.DriveFormat,
                    TotalSizeGB = Math.Round(drive.TotalSize / (1024.0 * 1024.0 * 1024.0), 2),
                    AvailableFreeSpaceGB = Math.Round(drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0), 2),
                    VolumeLabel = drive.VolumeLabel
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect disk information");
        }

        await Task.CompletedTask.ConfigureAwait(false);
        return info;
    }

    /// <summary>
    /// Export diagnostics as JSON
    /// </summary>
    public string ExportAsJson(SystemDiagnostics diagnostics)
    {
        return JsonSerializer.Serialize(diagnostics, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Export diagnostics as formatted text
    /// </summary>
    public string ExportAsText(SystemDiagnostics diagnostics)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("=== SYSTEM DIAGNOSTICS REPORT ===");
        sb.AppendLine($"Generated: {diagnostics.Timestamp:yyyy-MM-dd HH:mm:ss UTC}");
        sb.AppendLine();

        sb.AppendLine("--- Operating System ---");
        sb.AppendLine($"Platform: {diagnostics.OperatingSystem.Platform}");
        sb.AppendLine($"Architecture: {diagnostics.OperatingSystem.Architecture}");
        sb.AppendLine($"Version: {diagnostics.OperatingSystem.Version}");
        sb.AppendLine($"Machine Name: {diagnostics.OperatingSystem.MachineName}");
        sb.AppendLine();

        sb.AppendLine("--- Hardware ---");
        sb.AppendLine($"Processor: {diagnostics.Hardware.ProcessorName}");
        sb.AppendLine($"Processor Count: {diagnostics.Hardware.ProcessorCount}");
        sb.AppendLine($"Total Memory: {diagnostics.Hardware.TotalMemoryGB} GB");
        sb.AppendLine();

        sb.AppendLine("--- Runtime ---");
        sb.AppendLine($"Framework: {diagnostics.Runtime.FrameworkDescription}");
        sb.AppendLine($".NET Version: {diagnostics.Runtime.DotNetVersion}");
        sb.AppendLine($"64-bit Process: {diagnostics.Runtime.Is64BitProcess}");
        sb.AppendLine();

        sb.AppendLine("--- Process ---");
        sb.AppendLine($"Process ID: {diagnostics.Process.ProcessId}");
        sb.AppendLine($"Process Name: {diagnostics.Process.ProcessName}");
        sb.AppendLine($"Start Time: {diagnostics.Process.StartTime}");
        sb.AppendLine($"Up Time: {diagnostics.Process.UpTime}");
        sb.AppendLine($"Thread Count: {diagnostics.Process.ThreadCount}");
        sb.AppendLine($"Working Set: {diagnostics.Process.WorkingSetMB} MB");
        sb.AppendLine($"CPU Usage: {diagnostics.Process.CpuUsagePercent}%");
        sb.AppendLine();

        sb.AppendLine("--- Disk ---");
        foreach (var drive in diagnostics.Disk.Drives)
        {
            sb.AppendLine($"Drive {drive.Name}: {drive.AvailableFreeSpaceGB:F2} GB / {drive.TotalSizeGB:F2} GB available");
        }

        return sb.ToString();
    }
}

/// <summary>
/// Complete system diagnostics information
/// </summary>
public class SystemDiagnostics
{
    public DateTime Timestamp { get; set; }
    public OperatingSystemInfo OperatingSystem { get; set; } = new();
    public HardwareInfo Hardware { get; set; } = new();
    public RuntimeInfo Runtime { get; set; } = new();
    public ProcessInfo Process { get; set; } = new();
    public EnvironmentInfo Environment { get; set; } = new();
    public NetworkInfo Network { get; set; } = new();
    public DiskInfo Disk { get; set; } = new();
}

public class OperatingSystemInfo
{
    public string Platform { get; set; } = string.Empty;
    public string Architecture { get; set; } = string.Empty;
    public string ProcessArchitecture { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string VersionString { get; set; } = string.Empty;
    public bool Is64BitOperatingSystem { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string WindowsVersion { get; set; } = string.Empty;
}

public class HardwareInfo
{
    public string ProcessorName { get; set; } = string.Empty;
    public int ProcessorCount { get; set; }
    public double TotalMemoryGB { get; set; }
}

public class RuntimeInfo
{
    public string FrameworkDescription { get; set; } = string.Empty;
    public string RuntimeIdentifier { get; set; } = string.Empty;
    public string DotNetVersion { get; set; } = string.Empty;
    public bool Is64BitProcess { get; set; }
    public string CurrentDirectory { get; set; } = string.Empty;
    public string BaseDirectory { get; set; } = string.Empty;
}

public class ProcessInfo
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public TimeSpan UpTime { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
    public double WorkingSetMB { get; set; }
    public double PrivateMemoryMB { get; set; }
    public double VirtualMemoryMB { get; set; }
    public double CpuUsagePercent { get; set; }
}

public class EnvironmentInfo
{
    public string UserName { get; set; } = string.Empty;
    public string UserDomainName { get; set; } = string.Empty;
    public string SystemDirectory { get; set; } = string.Empty;
    public string CurrentDirectory { get; set; } = string.Empty;
    public string CommandLine { get; set; } = string.Empty;
    public long TickCount { get; set; }
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
}

public class NetworkInfo
{
    public string HostName { get; set; } = string.Empty;
    public List<string> IPAddresses { get; set; } = new();
}

public class DiskInfo
{
    public List<DriveInfoData> Drives { get; set; } = new();
}

public class DriveInfoData
{
    public string Name { get; set; } = string.Empty;
    public string DriveType { get; set; } = string.Empty;
    public string DriveFormat { get; set; } = string.Empty;
    public double TotalSizeGB { get; set; }
    public double AvailableFreeSpaceGB { get; set; }
    public string VolumeLabel { get; set; } = string.Empty;
}
