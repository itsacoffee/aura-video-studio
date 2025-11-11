using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
#if WINDOWS
using System.Management;
#endif

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for checking system requirements during first-run setup
/// </summary>
[ApiController]
[Route("api/system")]
public class SystemRequirementsController : ControllerBase
{
    private readonly ILogger<SystemRequirementsController> _logger;

    public SystemRequirementsController(ILogger<SystemRequirementsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get disk space information
    /// </summary>
    [HttpGet("disk-space")]
    public IActionResult GetDiskSpace()
    {
        try
        {
            // Get the drive where the application is running
            var appPath = AppDomain.CurrentDomain.BaseDirectory;
            var drive = new DriveInfo(Path.GetPathRoot(appPath) ?? "C:\\");

            var totalGB = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
            var availableGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);

            return Ok(new
            {
                totalGB = Math.Round(totalGB, 2),
                availableGB = Math.Round(availableGB, 2),
                usedGB = Math.Round(totalGB - availableGB, 2),
                percentageFree = Math.Round((availableGB / totalGB) * 100, 2)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting disk space information");
            return StatusCode(500, new { error = "Failed to get disk space information" });
        }
    }

    /// <summary>
    /// Get GPU information
    /// </summary>
    [HttpGet("gpu")]
    public IActionResult GetGPUInfo()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsGPUInfo();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxGPUInfo();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetMacGPUInfo();
            }

            return Ok(new
            {
                detected = false,
                vendor = "Unknown",
                model = "Unknown",
                memoryMB = 0,
                hardwareAcceleration = false,
                videoEncoding = false,
                videoDecoding = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting GPU information");
            return StatusCode(500, new { error = "Failed to get GPU information" });
        }
    }

    /// <summary>
    /// Get memory information
    /// </summary>
    [HttpGet("memory")]
    public IActionResult GetMemoryInfo()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsMemoryInfo();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxMemoryInfo();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetMacMemoryInfo();
            }

            return Ok(new
            {
                totalGB = 0.0,
                availableGB = 0.0,
                percentageAvailable = 0.0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting memory information");
            return StatusCode(500, new { error = "Failed to get memory information" });
        }
    }

    /// <summary>
    /// Get complete system requirements check
    /// </summary>
    [HttpGet("requirements")]
    public async Task<IActionResult> GetSystemRequirements(CancellationToken ct)
    {
        try
        {
            var diskSpace = await GetDiskSpaceInfo();
            var gpu = await GetGPUInformation();
            var memory = await GetMemoryInformation();
            var os = GetOSInformation();

            // Determine overall status
            var overallStatus = "pass";
            if (!os.compatible || diskSpace.status == "fail" || memory.status == "fail")
            {
                overallStatus = "fail";
            }
            else if (diskSpace.status == "warning" || gpu.status == "warning" || memory.status == "warning")
            {
                overallStatus = "warning";
            }

            return Ok(new
            {
                diskSpace,
                gpu,
                memory,
                os,
                overall = overallStatus
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking system requirements");
            return StatusCode(500, new { error = "Failed to check system requirements" });
        }
    }

    // Private helper methods

    private IActionResult GetWindowsGPUInfo()
    {
#if WINDOWS
        try
        {
            // Use WMI to get GPU information on Windows
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            var results = searcher.Get();

            foreach (ManagementObject obj in results)
            {
                var name = obj["Name"]?.ToString() ?? "Unknown";
                var adapterRAM = Convert.ToInt64(obj["AdapterRAM"] ?? 0);
                var memoryMB = adapterRAM / (1024 * 1024);

                // Detect vendor from name
                var vendor = "Unknown";
                if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
                {
                    vendor = "NVIDIA";
                }
                else if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
                {
                    vendor = "AMD";
                }
                else if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                {
                    vendor = "Intel";
                }

                // Check for hardware acceleration capabilities
                var hardwareAcceleration = vendor == "NVIDIA" || vendor == "AMD";
                var videoEncoding = vendor == "NVIDIA"; // NVENC
                var videoDecoding = hardwareAcceleration;

                return Ok(new
                {
                    detected = true,
                    vendor,
                    model = name,
                    memoryMB,
                    hardwareAcceleration,
                    videoEncoding,
                    videoDecoding
                });
            }

            // No GPU found
            return Ok(new
            {
                detected = false,
                vendor = "None",
                model = "None",
                memoryMB = 0,
                hardwareAcceleration = false,
                videoEncoding = false,
                videoDecoding = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Windows GPU info via WMI");
            return Ok(new
            {
                detected = false,
                vendor = "Unknown",
                model = "Unknown",
                memoryMB = 0,
                hardwareAcceleration = false,
                videoEncoding = false,
                videoDecoding = false
            });
        }
#else
        // WMI not available on non-Windows platforms
        return Ok(new
        {
            detected = false,
            vendor = "Windows-only",
            model = "WMI not available",
            memoryMB = 0,
            hardwareAcceleration = false,
            videoEncoding = false,
            videoDecoding = false
        });
#endif
    }

    private IActionResult GetLinuxGPUInfo()
    {
        try
        {
            // Try lspci command
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "lspci",
                    Arguments = "-nn",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("VGA", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("3D", StringComparison.OrdinalIgnoreCase))
                {
                    var vendor = "Unknown";
                    if (line.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
                    {
                        vendor = "NVIDIA";
                    }
                    else if (line.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
                             line.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
                    {
                        vendor = "AMD";
                    }
                    else if (line.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                    {
                        vendor = "Intel";
                    }

                    return Ok(new
                    {
                        detected = true,
                        vendor,
                        model = line.Trim(),
                        memoryMB = 0, // Can't easily determine on Linux
                        hardwareAcceleration = vendor == "NVIDIA" || vendor == "AMD",
                        videoEncoding = vendor == "NVIDIA",
                        videoDecoding = vendor == "NVIDIA" || vendor == "AMD"
                    });
                }
            }

            return Ok(new
            {
                detected = false,
                vendor = "None",
                model = "None",
                memoryMB = 0,
                hardwareAcceleration = false,
                videoEncoding = false,
                videoDecoding = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Linux GPU info");
            return Ok(new
            {
                detected = false,
                vendor = "Unknown",
                model = "Unknown",
                memoryMB = 0,
                hardwareAcceleration = false,
                videoEncoding = false,
                videoDecoding = false
            });
        }
    }

    private IActionResult GetMacGPUInfo()
    {
        try
        {
            // Try system_profiler command
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "system_profiler",
                    Arguments = "SPDisplaysDataType",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var hasDiscreteGPU = output.Contains("Chipset Model:", StringComparison.OrdinalIgnoreCase);

            return Ok(new
            {
                detected = hasDiscreteGPU,
                vendor = hasDiscreteGPU ? "Apple/AMD" : "Integrated",
                model = hasDiscreteGPU ? "Discrete GPU" : "Integrated Graphics",
                memoryMB = 0,
                hardwareAcceleration = true,
                videoEncoding = true, // macOS has VideoToolbox
                videoDecoding = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get macOS GPU info");
            return Ok(new
            {
                detected = false,
                vendor = "Unknown",
                model = "Unknown",
                memoryMB = 0,
                hardwareAcceleration = false,
                videoEncoding = false,
                videoDecoding = false
            });
        }
    }

    private IActionResult GetWindowsMemoryInfo()
    {
#if WINDOWS
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                var totalBytes = Convert.ToInt64(obj["TotalVisibleMemorySize"]) * 1024;
                var freeBytes = Convert.ToInt64(obj["FreePhysicalMemory"]) * 1024;

                var totalGB = totalBytes / (1024.0 * 1024.0 * 1024.0);
                var availableGB = freeBytes / (1024.0 * 1024.0 * 1024.0);

                return Ok(new
                {
                    totalGB = Math.Round(totalGB, 2),
                    availableGB = Math.Round(availableGB, 2),
                    percentageAvailable = Math.Round((availableGB / totalGB) * 100, 2)
                });
            }

            return Ok(new
            {
                totalGB = 0.0,
                availableGB = 0.0,
                percentageAvailable = 0.0
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Windows memory info");
            return Ok(new
            {
                totalGB = 0.0,
                availableGB = 0.0,
                percentageAvailable = 0.0
            });
        }
#else
        // WMI not available on non-Windows platforms
        // Try to use GC memory info as fallback
        try
        {
            var gcInfo = GC.GetGCMemoryInfo();
            var totalGB = gcInfo.TotalAvailableMemoryBytes / (1024.0 * 1024.0 * 1024.0);
            var availableGB = totalGB * 0.7; // Estimate 70% available

            return Ok(new
            {
                totalGB = Math.Round(totalGB, 2),
                availableGB = Math.Round(availableGB, 2),
                percentageAvailable = 70.0
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get memory info");
            return Ok(new
            {
                totalGB = 0.0,
                availableGB = 0.0,
                percentageAvailable = 0.0
            });
        }
#endif
    }

    private IActionResult GetLinuxMemoryInfo()
    {
        try
        {
            var meminfo = System.IO.File.ReadAllText("/proc/meminfo");
            var lines = meminfo.Split('\n');

            long totalKB = 0;
            long availableKB = 0;

            foreach (var line in lines)
            {
                if (line.StartsWith("MemTotal:"))
                {
                    totalKB = long.Parse(line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1]);
                }
                else if (line.StartsWith("MemAvailable:"))
                {
                    availableKB = long.Parse(line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1]);
                }
            }

            var totalGB = totalKB / (1024.0 * 1024.0);
            var availableGB = availableKB / (1024.0 * 1024.0);

            return Ok(new
            {
                totalGB = Math.Round(totalGB, 2),
                availableGB = Math.Round(availableGB, 2),
                percentageAvailable = Math.Round((availableGB / totalGB) * 100, 2)
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Linux memory info");
            return Ok(new
            {
                totalGB = 0.0,
                availableGB = 0.0,
                percentageAvailable = 0.0
            });
        }
    }

    private IActionResult GetMacMemoryInfo()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sysctl",
                    Arguments = "hw.memsize",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var parts = output.Split(':');
            if (parts.Length == 2 && long.TryParse(parts[1].Trim(), out var totalBytes))
            {
                var totalGB = totalBytes / (1024.0 * 1024.0 * 1024.0);
                var availableGB = totalGB * 0.7; // Estimate 70% available

                return Ok(new
                {
                    totalGB = Math.Round(totalGB, 2),
                    availableGB = Math.Round(availableGB, 2),
                    percentageAvailable = 70.0
                });
            }

            return Ok(new
            {
                totalGB = 0.0,
                availableGB = 0.0,
                percentageAvailable = 0.0
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get macOS memory info");
            return Ok(new
            {
                totalGB = 0.0,
                availableGB = 0.0,
                percentageAvailable = 0.0
            });
        }
    }

    // Helper methods for detailed requirements endpoint

    private async Task<dynamic> GetDiskSpaceInfo()
    {
        var appPath = AppDomain.CurrentDomain.BaseDirectory;
        var drive = new DriveInfo(Path.GetPathRoot(appPath) ?? "C:\\");

        var totalGB = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
        var availableGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);

        var status = "pass";
        var warnings = new List<string>();

        if (availableGB < 10)
        {
            status = "fail";
            warnings.Add("Less than 10GB available. Video generation requires significant disk space.");
        }
        else if (availableGB < 50)
        {
            status = "warning";
            warnings.Add("Less than 50GB available. Consider freeing up space for larger projects.");
        }

        return new
        {
            available = Math.Round(availableGB, 2),
            total = Math.Round(totalGB, 2),
            percentage = Math.Round((availableGB / totalGB) * 100, 2),
            status,
            warnings
        };
    }

    private async Task<dynamic> GetGPUInformation()
    {
        var result = GetGPUInfo();
        if (result is OkObjectResult okResult && okResult.Value != null)
        {
            var gpuData = okResult.Value as dynamic;
            var status = gpuData.detected ? "pass" : "warning";
            var recommendations = new List<string>();

            if (!gpuData.detected)
            {
                recommendations.Add("No dedicated GPU detected. Video encoding will use CPU (slower).");
            }

            return new
            {
                detected = gpuData.detected,
                vendor = gpuData.vendor,
                model = gpuData.model,
                memoryMB = gpuData.memoryMB,
                capabilities = new
                {
                    hardwareAcceleration = gpuData.hardwareAcceleration,
                    videoEncoding = gpuData.videoEncoding,
                    videoDecoding = gpuData.videoDecoding
                },
                status,
                recommendations
            };
        }

        return new
        {
            detected = false,
            vendor = "Unknown",
            model = "Unknown",
            memoryMB = 0,
            capabilities = new
            {
                hardwareAcceleration = false,
                videoEncoding = false,
                videoDecoding = false
            },
            status = "warning",
            recommendations = new[] { "Could not detect GPU information" }
        };
    }

    private async Task<dynamic> GetMemoryInformation()
    {
        var result = GetMemoryInfo();
        if (result is OkObjectResult okResult && okResult.Value != null)
        {
            var memData = okResult.Value as dynamic;
            var totalGB = (double)memData.totalGB;
            var availableGB = (double)memData.availableGB;

            var status = "pass";
            var warnings = new List<string>();

            if (totalGB < 4)
            {
                status = "fail";
                warnings.Add("Less than 4GB RAM detected. Minimum 4GB required.");
            }
            else if (totalGB < 8)
            {
                status = "warning";
                warnings.Add("Less than 8GB RAM detected. 8GB+ recommended.");
            }

            return new
            {
                total = totalGB,
                available = availableGB,
                percentage = (double)memData.percentageAvailable,
                status,
                warnings
            };
        }

        return new
        {
            total = 0.0,
            available = 0.0,
            percentage = 0.0,
            status = "warning",
            warnings = new[] { "Could not determine system memory" }
        };
    }

    private dynamic GetOSInformation()
    {
        var platform = "Unknown";
        var compatible = true;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            platform = "Windows";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            platform = "Linux";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            platform = "macOS";
        }

        return new
        {
            platform,
            version = Environment.OSVersion.Version.ToString(),
            architecture = RuntimeInformation.OSArchitecture.ToString(),
            compatible
        };
    }
}
