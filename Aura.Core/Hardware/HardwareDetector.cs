using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Text.Json;
using System.Threading.Tasks;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Hardware;

public class HardwareDetector
{
    private readonly ILogger<HardwareDetector> _logger;

    public HardwareDetector(ILogger<HardwareDetector> logger)
    {
        _logger = logger;
    }

    public async Task<SystemProfile> DetectSystemAsync()
    {
        _logger.LogInformation("Starting hardware detection");
        
        var cpuInfo = GetCpuInfo();
        var ramInfo = GetRamInfo();
        var gpuInfo = await GetGpuInfoAsync();
        
        var tier = DetermineTier(gpuInfo);
        var enableNVENC = gpuInfo?.Vendor?.ToUpperInvariant() == "NVIDIA";
        var enableSD = enableNVENC && gpuInfo?.VramGB >= 6;
        
        _logger.LogInformation("Hardware detection complete. System tier: {Tier}", tier);

        return new SystemProfile
        {
            AutoDetect = true,
            LogicalCores = cpuInfo.logical,
            PhysicalCores = cpuInfo.physical,
            RamGB = ramInfo,
            Gpu = gpuInfo,
            Tier = tier,
            EnableNVENC = enableNVENC,
            EnableSD = enableSD,
            OfflineOnly = false
        };
    }

    private (int logical, int physical) GetCpuInfo()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor");
            
            foreach (var obj in searcher.Get())
            {
                int physical = Convert.ToInt32(obj["NumberOfCores"]);
                int logical = Convert.ToInt32(obj["NumberOfLogicalProcessors"]);
                
                _logger.LogInformation("Detected CPU: {Physical} physical cores, {Logical} logical processors", 
                    physical, logical);
                
                return (logical, physical);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get CPU info from WMI, using fallback values");
        }

        // Fallback to Environment if WMI fails
        return (Environment.ProcessorCount, Environment.ProcessorCount / 2);
    }

    private int GetRamInfo()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
            
            foreach (var obj in searcher.Get())
            {
                // Convert from KB to GB
                ulong totalMemoryKB = Convert.ToUInt64(obj["TotalVisibleMemorySize"]);
                int ramGB = (int)(totalMemoryKB / 1024 / 1024);
                
                _logger.LogInformation("Detected RAM: {RAM} GB", ramGB);
                return ramGB;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get RAM info from WMI, using fallback values");
        }

        return 8; // Default fallback
    }

    private async Task<GpuInfo?> GetGpuInfoAsync()
    {
        // First try with WMI
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Caption, AdapterRAM FROM Win32_VideoController");

            foreach (var obj in searcher.Get())
            {
                string model = obj["Caption"]?.ToString() ?? "Unknown GPU";
                
                // AdapterRAM is not reliable, but we'll get a starting value
                ulong adapterRam = 0;
                try { adapterRam = Convert.ToUInt64(obj["AdapterRAM"]); } catch { }
                
                int vramMB = (int)(adapterRam / 1024 / 1024);
                string vendor = DetermineVendor(model);
                string? series = DetermineSeries(model, vendor);
                
                _logger.LogInformation("Detected GPU (WMI): {Model}, {Vendor}, {VRAM} MB", 
                    model, vendor, vramMB);
                
                // For NVIDIA, try to get more accurate info using nvidia-smi
                if (vendor.Equals("NVIDIA", StringComparison.OrdinalIgnoreCase))
                {
                    var nvidiaSmiInfo = await GetNvidiaSmiInfoAsync();
                    if (nvidiaSmiInfo != null)
                    {
                        _logger.LogInformation("Updated GPU info via nvidia-smi: {Model}, {VRAM} MB",
                            nvidiaSmiInfo.Value.model, nvidiaSmiInfo.Value.vramMB);
                        
                        return new GpuInfo(
                            vendor,
                            nvidiaSmiInfo.Value.model,
                            nvidiaSmiInfo.Value.vramMB / 1024, // Convert MB to GB
                            series);
                    }
                }
                
                return new GpuInfo(vendor, model, Math.Max(1, vramMB / 1024), series);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get GPU info from WMI");
        }

        // If we got here, we couldn't get GPU info
        _logger.LogWarning("Could not detect GPU information");
        return null;
    }

    private async Task<(string model, int vramMB)?> GetNvidiaSmiInfoAsync()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "nvidia-smi",
                    Arguments = "--query-gpu=name,memory.total --format=csv,noheader",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
            {
                // Parse output (format is typically: "GeForce RTX 3080, 10240 MiB")
                var parts = output.Split(',');
                if (parts.Length >= 2)
                {
                    string model = parts[0].Trim();
                    string memoryStr = parts[1].Trim();
                    
                    // Extract just the number
                    int vramMB = int.Parse(string.Join("", 
                        System.Text.RegularExpressions.Regex.Matches(memoryStr, @"\d+")));
                    
                    return (model, vramMB);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get GPU info from nvidia-smi");
        }

        return null;
    }

    private string DetermineVendor(string gpuName)
    {
        gpuName = gpuName.ToUpperInvariant();
        
        if (gpuName.Contains("NVIDIA") || gpuName.Contains("GEFORCE") || gpuName.Contains("QUADRO") || gpuName.Contains("TESLA"))
            return "NVIDIA";
        
        if (gpuName.Contains("AMD") || gpuName.Contains("RADEON") || gpuName.Contains("FIREPRO"))
            return "AMD";
        
        if (gpuName.Contains("INTEL") || gpuName.Contains("ARC") || gpuName.Contains("HD GRAPHICS") || gpuName.Contains("UHD GRAPHICS"))
            return "INTEL";
        
        return "Unknown";
    }

    private string? DetermineSeries(string gpuName, string vendor)
    {
        gpuName = gpuName.ToUpperInvariant();
        
        if (vendor == "NVIDIA")
        {
            if (gpuName.Contains("RTX 50") || gpuName.Contains("RTX50")) return "50";
            if (gpuName.Contains("RTX 40") || gpuName.Contains("RTX40")) return "40";
            if (gpuName.Contains("RTX 30") || gpuName.Contains("RTX30")) return "30";
            if (gpuName.Contains("RTX 20") || gpuName.Contains("RTX20")) return "20";
            if (gpuName.Contains("GTX 16") || gpuName.Contains("GTX16")) return "16";
            if (gpuName.Contains("GTX 10") || gpuName.Contains("GTX10")) return "10";
        }
        
        return null;
    }

    private HardwareTier DetermineTier(GpuInfo? gpuInfo)
    {
        // If no GPU is detected or it's not NVIDIA, default to Tier D
        if (gpuInfo == null || gpuInfo.Vendor != "NVIDIA")
        {
            return HardwareTier.D;
        }

        // Check for high-end GPUs first (series 40/50 or >=12GB VRAM)
        if (gpuInfo.Series == "40" || gpuInfo.Series == "50" || gpuInfo.VramGB >= 12)
        {
            return HardwareTier.A;
        }

        // Upper-mid tier (8-12GB VRAM)
        if (gpuInfo.VramGB >= 8)
        {
            return HardwareTier.B;
        }

        // Mid tier (6-8GB VRAM)
        if (gpuInfo.VramGB >= 6)
        {
            return HardwareTier.C;
        }

        // Entry level
        return HardwareTier.D;
    }
    
    public async Task RunHardwareProbeAsync()
    {
        _logger.LogInformation("Running hardware probe to validate system capabilities");
        
        await Task.WhenAll(
            RunRenderProbeAsync(),
            RunTtsProbeAsync(),
            RunNvencProbeAsync(),
            RunStableDiffusionProbeAsync(),
            CheckDiskSpaceAsync()
        );
        
        _logger.LogInformation("Hardware probe completed");
    }
    
    private async Task RunRenderProbeAsync()
    {
        _logger.LogInformation("Running FFmpeg render probe");
        
        try
        {
            // Check if ffmpeg is available
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                _logger.LogInformation("FFmpeg render probe passed");
            }
            else
            {
                _logger.LogWarning("FFmpeg render probe failed with exit code {ExitCode}", process.ExitCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FFmpeg render probe failed - FFmpeg may not be installed or in PATH");
        }
    }
    
    private async Task RunTtsProbeAsync()
    {
        _logger.LogInformation("Running Windows TTS probe");
        
        try
        {
#if WINDOWS10_0_19041_0_OR_GREATER
            // Test Windows TTS availability
            var synthesizer = new Windows.Media.SpeechSynthesis.SpeechSynthesizer();
            var voices = Windows.Media.SpeechSynthesis.SpeechSynthesizer.AllVoices;
            
            _logger.LogInformation("Windows TTS probe passed - {VoiceCount} voices available", voices.Count);
#else
            _logger.LogInformation("Windows TTS probe skipped - not running on Windows");
#endif
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Windows TTS probe failed");
        }
    }
    
    private async Task RunNvencProbeAsync()
    {
        _logger.LogInformation("Running NVENC probe");
        
        try
        {
            // Check for NVENC availability via ffmpeg
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-hide_banner -encoders",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            bool hasNvenc = output.Contains("h264_nvenc") || output.Contains("hevc_nvenc");
            
            if (hasNvenc)
            {
                _logger.LogInformation("NVENC probe passed - NVENC encoders available");
            }
            else
            {
                _logger.LogInformation("NVENC probe: No NVENC encoders detected");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "NVENC probe failed");
        }
    }
    
    private async Task RunStableDiffusionProbeAsync()
    {
        _logger.LogInformation("Running Stable Diffusion probe");
        
        try
        {
            // Test connection to SD WebUI API
            using var httpClient = new System.Net.Http.HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            
            var response = await httpClient.GetAsync("http://127.0.0.1:7860/");
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Stable Diffusion probe passed - WebUI detected at http://127.0.0.1:7860");
            }
            else
            {
                _logger.LogInformation("Stable Diffusion probe: WebUI not detected");
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Stable Diffusion probe: WebUI not available at http://127.0.0.1:7860");
        }
    }
    
    private async Task CheckDiskSpaceAsync()
    {
        _logger.LogInformation("Checking disk space");
        
        try
        {
            var drives = DriveInfo.GetDrives();
            
            foreach (var drive in drives)
            {
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                {
                    long freeSpaceGB = drive.AvailableFreeSpace / 1024 / 1024 / 1024;
                    
                    _logger.LogInformation("Drive {Drive}: {FreeSpace} GB available", 
                        drive.Name, freeSpaceGB);
                    
                    if (freeSpaceGB < 10)
                    {
                        _logger.LogWarning("Low disk space on {Drive}: Only {FreeSpace} GB available", 
                            drive.Name, freeSpaceGB);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check disk space");
        }
        
        await Task.CompletedTask;
    }
}