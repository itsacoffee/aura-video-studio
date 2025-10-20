using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;

namespace Aura.Api.Helpers;

/// <summary>
/// Utility class for network-related operations including port detection and ownership
/// </summary>
public static class NetworkUtility
{
    /// <summary>
    /// Result of port check including ownership information
    /// </summary>
    public record PortCheckResult(
        bool IsInUse,
        bool IsOwnedByCurrentProcess,
        int? OwningProcessId,
        string? OwningProcessName
    );

    /// <summary>
    /// Check if a port is in use and determine if it's owned by the current process
    /// </summary>
    /// <param name="port">Port number to check</param>
    /// <returns>Port check result with ownership information</returns>
    public static PortCheckResult CheckPort(int port)
    {
        try
        {
            var currentProcessId = Environment.ProcessId;
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            
            // Check TCP listeners
            var tcpListeners = ipGlobalProperties.GetActiveTcpListeners();
            var tcpConnections = ipGlobalProperties.GetActiveTcpConnections();
            
            // First check if the port is in use at all
            var portInUse = tcpListeners.Any(endpoint => endpoint.Port == port);
            
            if (!portInUse)
            {
                return new PortCheckResult(false, false, null, null);
            }

            // Port is in use, now try to determine the owner
            // Note: Getting the process ID from IPGlobalProperties is not directly supported
            // We need to use platform-specific methods or netstat-like parsing
            
            if (OperatingSystem.IsWindows())
            {
                return CheckPortWindows(port, currentProcessId);
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                return CheckPortUnix(port, currentProcessId);
            }
            
            // If we can't determine ownership, just report that it's in use
            return new PortCheckResult(true, false, null, null);
        }
        catch (Exception)
        {
            // If we can't check, assume it's not in use
            return new PortCheckResult(false, false, null, null);
        }
    }

    /// <summary>
    /// Check port ownership on Windows using netstat
    /// </summary>
    private static PortCheckResult CheckPortWindows(int port, int currentProcessId)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "netstat",
                Arguments = "-ano",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return new PortCheckResult(true, false, null, null);
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Parse netstat output to find the process using this port
            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains($":{port}") && line.Contains("LISTENING"))
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        var pidStr = parts[^1]; // Last column is PID
                        if (int.TryParse(pidStr, out var pid))
                        {
                            var isOwned = pid == currentProcessId;
                            string? processName = null;
                            
                            try
                            {
                                var owningProcess = Process.GetProcessById(pid);
                                processName = owningProcess.ProcessName;
                            }
                            catch
                            {
                                // Process may have exited or we don't have permission
                            }
                            
                            return new PortCheckResult(true, isOwned, pid, processName);
                        }
                    }
                }
            }
        }
        catch
        {
            // Fall through to default
        }

        return new PortCheckResult(true, false, null, null);
    }

    /// <summary>
    /// Check port ownership on Unix-like systems using lsof or ss
    /// </summary>
    private static PortCheckResult CheckPortUnix(int port, int currentProcessId)
    {
        try
        {
            // Try lsof first (more reliable)
            var startInfo = new ProcessStartInfo
            {
                FileName = "lsof",
                Arguments = $"-i :{port} -sTCP:LISTEN -t",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(output) && int.TryParse(output, out var pid))
                {
                    var isOwned = pid == currentProcessId;
                    string? processName = null;
                    
                    try
                    {
                        var owningProcess = Process.GetProcessById(pid);
                        processName = owningProcess.ProcessName;
                    }
                    catch
                    {
                        // Process may have exited or we don't have permission
                    }
                    
                    return new PortCheckResult(true, isOwned, pid, processName);
                }
            }
        }
        catch
        {
            // lsof not available or failed, try ss
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ss",
                    Arguments = $"-ltnp | grep :{port}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // Parse ss output for PID
                    // Format: ... users:(("process",pid=1234,...))
                    var match = System.Text.RegularExpressions.Regex.Match(output, @"pid=(\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var pid))
                    {
                        var isOwned = pid == currentProcessId;
                        string? processName = null;
                        
                        try
                        {
                            var owningProcess = Process.GetProcessById(pid);
                            processName = owningProcess.ProcessName;
                        }
                        catch
                        {
                            // Process may have exited or we don't have permission
                        }
                        
                        return new PortCheckResult(true, isOwned, pid, processName);
                    }
                }
            }
            catch
            {
                // Fall through to default
            }
        }

        return new PortCheckResult(true, false, null, null);
    }

    /// <summary>
    /// Get a human-readable message about port status
    /// </summary>
    public static string GetPortStatusMessage(int port, PortCheckResult result)
    {
        if (!result.IsInUse)
        {
            return $"Port {port} is available";
        }

        if (result.IsOwnedByCurrentProcess)
        {
            return $"Port {port} is being used by this application";
        }

        if (result.OwningProcessId.HasValue)
        {
            var processInfo = result.OwningProcessName != null 
                ? $"{result.OwningProcessName} (PID: {result.OwningProcessId})"
                : $"PID: {result.OwningProcessId}";
            return $"Port {port} is in use by another process: {processInfo}";
        }

        return $"Port {port} is in use by another process";
    }

    /// <summary>
    /// Get actionable remediation message for port conflicts
    /// </summary>
    public static string GetPortRemediationMessage(int port, PortCheckResult result)
    {
        if (!result.IsInUse || result.IsOwnedByCurrentProcess)
        {
            return string.Empty;
        }

        if (result.OwningProcessId.HasValue && result.OwningProcessName != null)
        {
            return $"Stop the {result.OwningProcessName} process (PID: {result.OwningProcessId}) or configure the application to use a different port";
        }

        return $"Stop the process using port {port} or configure the application to use a different port";
    }
}
