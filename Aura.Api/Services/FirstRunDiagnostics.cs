using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Dependencies;
using Aura.Core.Hardware;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Services;

/// <summary>
/// Diagnostic issue with severity and fix actions
/// </summary>
public class DiagnosticIssue
{
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Severity { get; set; } = "info"; // info, warning, error, critical
    public List<string> Causes { get; set; } = new();
    public List<DiagnosticFixAction> FixActions { get; set; } = new();
    public bool AutoFixable { get; set; }
}

/// <summary>
/// Fix action that can be taken to resolve an issue (different from PreflightService.FixAction)
/// </summary>
public class DiagnosticFixAction
{
    public string Label { get; set; } = "";
    public string Description { get; set; } = "";
    public string ActionType { get; set; } = ""; // navigate, download, install, configure, retry
    public string? ActionUrl { get; set; }
    public Dictionary<string, object>? ActionData { get; set; }
}

/// <summary>
/// First-run diagnostics result
/// </summary>
public class FirstRunDiagnosticsResult
{
    public bool Ready { get; set; }
    public string Status { get; set; } = "unknown"; // ready, needs-setup, has-errors
    public List<DiagnosticIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public Dictionary<string, object> SystemInfo { get; set; } = new();
}

/// <summary>
/// Comprehensive first-run diagnostics to help users get started
/// </summary>
public class FirstRunDiagnostics
{
    private readonly ILogger<FirstRunDiagnostics> _logger;
    private readonly IFfmpegLocator _ffmpegLocator;
    private readonly ProviderSettings _providerSettings;
    private readonly HardwareDetector _hardwareDetector;

    public FirstRunDiagnostics(
        ILogger<FirstRunDiagnostics> logger,
        IFfmpegLocator ffmpegLocator,
        ProviderSettings providerSettings,
        HardwareDetector hardwareDetector)
    {
        _logger = logger;
        _ffmpegLocator = ffmpegLocator;
        _providerSettings = providerSettings;
        _hardwareDetector = hardwareDetector;
    }

    /// <summary>
    /// Run comprehensive first-run diagnostics
    /// </summary>
    public async Task<FirstRunDiagnosticsResult> RunDiagnosticsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Running first-run diagnostics...");
        
        var result = new FirstRunDiagnosticsResult
        {
            SystemInfo = await CollectSystemInfoAsync(ct).ConfigureAwait(false)
        };

        var issues = new List<DiagnosticIssue>();

        // Check FFmpeg
        var ffmpegIssue = await CheckFfmpegAsync(ct).ConfigureAwait(false);
        if (ffmpegIssue != null)
        {
            issues.Add(ffmpegIssue);
        }

        // Check directory permissions
        var permissionsIssue = CheckDirectoryPermissions();
        if (permissionsIssue != null)
        {
            issues.Add(permissionsIssue);
        }

        // Check disk space
        var diskSpaceIssue = CheckDiskSpace();
        if (diskSpaceIssue != null)
        {
            issues.Add(diskSpaceIssue);
        }

        // Check internet connectivity
        var internetIssue = await CheckInternetConnectivityAsync(ct).ConfigureAwait(false);
        if (internetIssue != null)
        {
            issues.Add(internetIssue);
        }

        // Check hardware capabilities
        var hardwareIssue = await CheckHardwareAsync(ct).ConfigureAwait(false);
        if (hardwareIssue != null)
        {
            issues.Add(hardwareIssue);
        }

        result.Issues = issues;

        // Determine overall status
        var criticalIssues = issues.Where(i => i.Severity == "critical").ToList();
        var errorIssues = issues.Where(i => i.Severity == "error").ToList();
        var warningIssues = issues.Where(i => i.Severity == "warning").ToList();

        if (criticalIssues.Any())
        {
            result.Status = "has-errors";
            result.Ready = false;
            result.Recommendations.Add("Critical issues must be resolved before the application can function properly.");
        }
        else if (errorIssues.Any())
        {
            result.Status = "needs-setup";
            result.Ready = false;
            result.Recommendations.Add("Some components need to be installed or configured.");
            result.Recommendations.Add("You can use the Downloads page to install missing components.");
        }
        else if (warningIssues.Any())
        {
            result.Status = "ready";
            result.Ready = true;
            result.Recommendations.Add("System is ready! Some optional components are not configured.");
            result.Recommendations.Add("You can enhance functionality by installing optional components from the Downloads page.");
        }
        else
        {
            result.Status = "ready";
            result.Ready = true;
            result.Recommendations.Add("All systems ready! You can start creating videos.");
        }

        _logger.LogInformation("Diagnostics complete. Status: {Status}, Issues: {Count}", 
            result.Status, issues.Count);

        return result;
    }

    private async Task<Dictionary<string, object>> CollectSystemInfoAsync(CancellationToken ct)
    {
        var info = new Dictionary<string, object>
        {
            ["platform"] = RuntimeInformation.OSDescription,
            ["architecture"] = RuntimeInformation.ProcessArchitecture.ToString(),
            ["framework"] = RuntimeInformation.FrameworkDescription,
            ["isWindows"] = OperatingSystem.IsWindows(),
            ["isLinux"] = OperatingSystem.IsLinux(),
            ["isMacOS"] = OperatingSystem.IsMacOS()
        };

        try
        {
            var profile = await _hardwareDetector.DetectSystemAsync().ConfigureAwait(false);
            info["cpu"] = new { cores = profile.PhysicalCores, threads = profile.LogicalCores };
            info["ram"] = new { gb = profile.RamGB };
            if (profile.Gpu != null)
            {
                info["gpu"] = new { model = profile.Gpu.Model, vramGB = profile.Gpu.VramGB, vendor = profile.Gpu.Vendor };
            }
            info["tier"] = profile.Tier.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect hardware");
        }

        return info;
    }

    private async Task<DiagnosticIssue?> CheckFfmpegAsync(CancellationToken ct)
    {
        try
        {
            var result = await _ffmpegLocator.CheckAllCandidatesAsync(null, ct).ConfigureAwait(false);
            
            if (!result.Found)
            {
                return new DiagnosticIssue
                {
                    Code = "E302-FFMPEG_NOT_FOUND",
                    Title = "FFmpeg Not Found",
                    Description = "FFmpeg is required for video rendering but was not detected on your system.",
                    Severity = "error",
                    Causes = new List<string>
                    {
                        "FFmpeg is not installed",
                        "FFmpeg is not in the system PATH",
                        "FFmpeg is installed in a custom location that hasn't been configured"
                    },
                    FixActions = new List<DiagnosticFixAction>
                    {
                        new DiagnosticFixAction
                        {
                            Label = "Install FFmpeg Automatically",
                            Description = "Download and install FFmpeg from the Downloads page",
                            ActionType = "navigate",
                            ActionUrl = "/downloads"
                        },
                        new DiagnosticFixAction
                        {
                            Label = "Configure Existing FFmpeg",
                            Description = "If you already have FFmpeg installed, point to its location in Settings",
                            ActionType = "navigate",
                            ActionUrl = "/settings"
                        },
                        new DiagnosticFixAction
                        {
                            Label = "Manual Installation Guide",
                            Description = "Follow the manual installation instructions",
                            ActionType = "navigate",
                            ActionUrl = "/docs/install-ffmpeg"
                        }
                    },
                    AutoFixable = true
                };
            }

            return null; // FFmpeg found, no issue
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking FFmpeg");
            return new DiagnosticIssue
            {
                Code = "E302-FFMPEG_CHECK_FAILED",
                Title = "FFmpeg Check Failed",
                Description = $"Unable to verify FFmpeg installation: {ex.Message}",
                Severity = "warning",
                Causes = new List<string>
                {
                    "Permission issues",
                    "Temporary file system error"
                },
                FixActions = new List<DiagnosticFixAction>
                {
                    new DiagnosticFixAction
                    {
                        Label = "Retry Check",
                        Description = "Try checking again after a moment",
                        ActionType = "retry"
                    }
                },
                AutoFixable = false
            };
        }
    }

    private DiagnosticIssue? CheckDirectoryPermissions()
    {
        var problematicDirs = new List<string>();

        try
        {
            var toolsDir = _providerSettings.GetToolsDirectory();
            if (!Directory.Exists(toolsDir) || !IsDirectoryWritable(toolsDir))
            {
                problematicDirs.Add(toolsDir);
            }

            var auraDataDir = _providerSettings.GetAuraDataDirectory();
            if (!Directory.Exists(auraDataDir) || !IsDirectoryWritable(auraDataDir))
            {
                problematicDirs.Add(auraDataDir);
            }

            if (problematicDirs.Any())
            {
                return new DiagnosticIssue
                {
                    Code = "E001-PERMISSION_DENIED",
                    Title = "Directory Permission Issues",
                    Description = $"Cannot write to one or more required directories: {string.Join(", ", problematicDirs)}",
                    Severity = "critical",
                    Causes = new List<string>
                    {
                        "Running from a read-only location (e.g., CD-ROM)",
                        "Insufficient file system permissions",
                        "Antivirus software blocking access",
                        "Directory is owned by another user"
                    },
                    FixActions = new List<DiagnosticFixAction>
                    {
                        new DiagnosticFixAction
                        {
                            Label = "Run as Administrator",
                            Description = "Try running the application with elevated permissions",
                            ActionType = "configure"
                        },
                        new DiagnosticFixAction
                        {
                            Label = "Check Antivirus Settings",
                            Description = "Add Aura Video Studio to your antivirus exclusion list",
                            ActionType = "configure"
                        },
                        new DiagnosticFixAction
                        {
                            Label = "Extract to Different Location",
                            Description = "Move the application to a writable location like Documents or C:\\Aura",
                            ActionType = "configure"
                        }
                    },
                    AutoFixable = false
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking directory permissions");
            return new DiagnosticIssue
            {
                Code = "E001-PERMISSION_CHECK_FAILED",
                Title = "Permission Check Failed",
                Description = $"Unable to check directory permissions: {ex.Message}",
                Severity = "error",
                Causes = new List<string> { "File system error" },
                FixActions = new List<DiagnosticFixAction>(),
                AutoFixable = false
            };
        }
    }

    private bool IsDirectoryWritable(string path)
    {
        try
        {
            var testFile = Path.Combine(path, $".aura-write-test-{Guid.NewGuid()}.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private DiagnosticIssue? CheckDiskSpace()
    {
        try
        {
            var toolsDir = _providerSettings.GetToolsDirectory();
            var drive = new DriveInfo(Path.GetPathRoot(toolsDir) ?? "C:\\");
            
            var freeSpaceGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
            
            if (freeSpaceGB < 1)
            {
                return new DiagnosticIssue
                {
                    Code = "E003-LOW_DISK_SPACE",
                    Title = "Critical: Low Disk Space",
                    Description = $"Only {freeSpaceGB:F2} GB free space available. At least 1 GB required.",
                    Severity = "critical",
                    Causes = new List<string>
                    {
                        "Disk is nearly full",
                        "Large files taking up space"
                    },
                    FixActions = new List<DiagnosticFixAction>
                    {
                        new DiagnosticFixAction
                        {
                            Label = "Free Up Disk Space",
                            Description = "Delete unnecessary files or move the application to a drive with more space",
                            ActionType = "configure"
                        }
                    },
                    AutoFixable = false
                };
            }
            else if (freeSpaceGB < 5)
            {
                return new DiagnosticIssue
                {
                    Code = "E003-LOW_DISK_SPACE",
                    Title = "Warning: Low Disk Space",
                    Description = $"Only {freeSpaceGB:F2} GB free space available. At least 5 GB recommended for video rendering.",
                    Severity = "warning",
                    Causes = new List<string>
                    {
                        "Limited disk space may affect video rendering"
                    },
                    FixActions = new List<DiagnosticFixAction>
                    {
                        new DiagnosticFixAction
                        {
                            Label = "Free Up Disk Space",
                            Description = "Consider freeing up more space for better performance",
                            ActionType = "configure"
                        }
                    },
                    AutoFixable = false
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking disk space");
            return null;
        }
    }

    private async Task<DiagnosticIssue?> CheckInternetConnectivityAsync(CancellationToken ct)
    {
        try
        {
            using var httpClient = new System.Net.Http.HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            
            var response = await httpClient.GetAsync("https://www.google.com", ct).ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return CreateOfflineIssue();
            }

            return null; // Internet is available
        }
        catch
        {
            return CreateOfflineIssue();
        }
    }

    private DiagnosticIssue CreateOfflineIssue()
    {
        return new DiagnosticIssue
        {
            Code = "W001-NO_INTERNET",
            Title = "No Internet Connection",
            Description = "Internet connection not detected. Some features may be limited.",
            Severity = "info",
            Causes = new List<string>
            {
                "Not connected to the internet",
                "Firewall blocking outbound connections"
            },
            FixActions = new List<DiagnosticFixAction>
            {
                new DiagnosticFixAction
                {
                    Label = "Use Offline Mode",
                    Description = "The application can still work with local providers (RuleBased script generation, Windows TTS)",
                    ActionType = "configure"
                },
                new DiagnosticFixAction
                {
                    Label = "Enable Internet Access",
                    Description = "Connect to the internet to download components and use cloud providers",
                    ActionType = "configure"
                }
            },
            AutoFixable = false
        };
    }

    private async Task<DiagnosticIssue?> CheckHardwareAsync(CancellationToken ct)
    {
        try
        {
            var profile = await _hardwareDetector.DetectSystemAsync().ConfigureAwait(false);
            
            if (profile.RamGB < 4)
            {
                return new DiagnosticIssue
                {
                    Code = "W002-LOW_RAM",
                    Title = "Low System RAM",
                    Description = $"Only {profile.RamGB} GB RAM detected. 8 GB or more recommended for video rendering.",
                    Severity = "warning",
                    Causes = new List<string>
                    {
                        "Limited system memory may slow down video rendering",
                        "May not be able to use advanced features"
                    },
                    FixActions = new List<DiagnosticFixAction>
                    {
                        new DiagnosticFixAction
                        {
                            Label = "Close Other Applications",
                            Description = "Close unnecessary applications to free up memory",
                            ActionType = "configure"
                        },
                        new DiagnosticFixAction
                        {
                            Label = "Use Lower Quality Settings",
                            Description = "Reduce resolution or quality settings to reduce memory usage",
                            ActionType = "configure"
                        }
                    },
                    AutoFixable = false
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking hardware");
            return null;
        }
    }
}
