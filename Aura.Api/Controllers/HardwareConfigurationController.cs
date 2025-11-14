using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for hardware-specific configuration and preferences
/// </summary>
[ApiController]
[Route("api/hardware-config")]
public class HardwareConfigurationController : ControllerBase
{
    private readonly ILogger<HardwareConfigurationController> _logger;
    private readonly IHardwareDetector _hardwareDetector;
    private readonly ProviderSettings _providerSettings;
    private readonly string _hardwareConfigPath;

    public HardwareConfigurationController(
        ILogger<HardwareConfigurationController> logger,
        IHardwareDetector hardwareDetector,
        ProviderSettings providerSettings)
    {
        _logger = logger;
        _hardwareDetector = hardwareDetector;
        _providerSettings = providerSettings;

        var auraDataDir = _providerSettings.GetAuraDataDirectory();
        _hardwareConfigPath = Path.Combine(auraDataDir, "hardware-config.json");
    }

    /// <summary>
    /// Get current hardware configuration preferences
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetHardwareConfiguration(CancellationToken ct)
    {
        try
        {
            var config = await LoadHardwareConfigAsync(ct).ConfigureAwait(false);
            var systemProfile = await _hardwareDetector.DetectSystemAsync().ConfigureAwait(false);

            return Ok(new
            {
                config,
                systemProfile = new
                {
                    tier = systemProfile.Tier,
                    logicalCores = systemProfile.LogicalCores,
                    physicalCores = systemProfile.PhysicalCores,
                    ramGB = systemProfile.RamGB,
                    gpu = systemProfile.Gpu != null ? new
                    {
                        vendor = systemProfile.Gpu.Vendor,
                        model = systemProfile.Gpu.Model,
                        vramGB = systemProfile.Gpu.VramGB,
                        series = systemProfile.Gpu.Series
                    } : null,
                    enableNVENC = systemProfile.EnableNVENC,
                    enableSD = systemProfile.EnableSD,
                    offlineOnly = systemProfile.OfflineOnly
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hardware configuration");
            return StatusCode(500, new { error = "Failed to get hardware configuration" });
        }
    }

    /// <summary>
    /// Save hardware configuration preferences
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SaveHardwareConfiguration(
        [FromBody] HardwareConfigModel config,
        CancellationToken ct)
    {
        try
        {
            if (config == null)
            {
                return BadRequest(new { error = "Configuration is required" });
            }

            await SaveHardwareConfigAsync(config, ct).ConfigureAwait(false);

            _logger.LogInformation("Hardware configuration saved successfully");

            return Ok(new
            {
                success = true,
                message = "Hardware configuration saved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving hardware configuration");
            return StatusCode(500, new { error = "Failed to save hardware configuration" });
        }
    }

    /// <summary>
    /// Get available GPUs for selection
    /// </summary>
    [HttpGet("gpus")]
    public async Task<IActionResult> GetAvailableGPUs()
    {
        try
        {
            var systemProfile = await _hardwareDetector.DetectSystemAsync().ConfigureAwait(false);
            
            var gpus = new List<object>();
            
            if (systemProfile.Gpu != null)
            {
                gpus.Add(new
                {
                    id = "gpu-0",
                    vendor = systemProfile.Gpu.Vendor,
                    model = systemProfile.Gpu.Model,
                    vramGB = systemProfile.Gpu.VramGB,
                    series = systemProfile.Gpu.Series,
                    capabilities = new
                    {
                        nvenc = systemProfile.EnableNVENC,
                        amf = systemProfile.Gpu.Vendor?.ToUpperInvariant() == "AMD",
                        quickSync = systemProfile.Gpu.Vendor?.ToUpperInvariant() == "INTEL"
                    },
                    recommended = true
                });
            }

            return Ok(new
            {
                gpus,
                totalCount = gpus.Count,
                hasGpu = gpus.Count > 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available GPUs");
            return StatusCode(500, new { error = "Failed to get available GPUs" });
        }
    }

    /// <summary>
    /// Get available video encoders based on hardware
    /// </summary>
    [HttpGet("encoders")]
    public async Task<IActionResult> GetAvailableEncoders()
    {
        try
        {
            var systemProfile = await _hardwareDetector.DetectSystemAsync().ConfigureAwait(false);
            var encoders = new List<object>
            {
                new
                {
                    id = "libx264",
                    name = "H.264 (Software)",
                    type = "software",
                    available = true,
                    quality = "high",
                    speed = "slow",
                    recommended = false
                },
                new
                {
                    id = "libx265",
                    name = "H.265/HEVC (Software)",
                    type = "software",
                    available = true,
                    quality = "very-high",
                    speed = "very-slow",
                    recommended = false
                }
            };

            if (systemProfile.EnableNVENC)
            {
                encoders.Add(new
                {
                    id = "h264_nvenc",
                    name = "H.264 (NVIDIA NVENC)",
                    type = "hardware",
                    vendor = "nvidia",
                    available = true,
                    quality = "high",
                    speed = "very-fast",
                    recommended = true
                });
                encoders.Add(new
                {
                    id = "hevc_nvenc",
                    name = "H.265/HEVC (NVIDIA NVENC)",
                    type = "hardware",
                    vendor = "nvidia",
                    available = true,
                    quality = "very-high",
                    speed = "very-fast",
                    recommended = false
                });
            }

            var isAmdGpu = systemProfile.Gpu?.Vendor?.ToUpperInvariant() == "AMD";
            if (isAmdGpu)
            {
                encoders.Add(new
                {
                    id = "h264_amf",
                    name = "H.264 (AMD AMF)",
                    type = "hardware",
                    vendor = "amd",
                    available = true,
                    quality = "high",
                    speed = "very-fast",
                    recommended = true
                });
                encoders.Add(new
                {
                    id = "hevc_amf",
                    name = "H.265/HEVC (AMD AMF)",
                    type = "hardware",
                    vendor = "amd",
                    available = true,
                    quality = "very-high",
                    speed = "very-fast",
                    recommended = false
                });
            }

            var isIntelGpu = systemProfile.Gpu?.Vendor?.ToUpperInvariant() == "INTEL";
            if (isIntelGpu)
            {
                encoders.Add(new
                {
                    id = "h264_qsv",
                    name = "H.264 (Intel Quick Sync)",
                    type = "hardware",
                    vendor = "intel",
                    available = true,
                    quality = "good",
                    speed = "fast",
                    recommended = true
                });
                encoders.Add(new
                {
                    id = "hevc_qsv",
                    name = "H.265/HEVC (Intel Quick Sync)",
                    type = "hardware",
                    vendor = "intel",
                    available = true,
                    quality = "good",
                    speed = "fast",
                    recommended = false
                });
            }

            return Ok(new
            {
                encoders,
                totalCount = encoders.Count,
                hardwareEncoders = encoders.Count(e => ((dynamic)e).type == "hardware")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available encoders");
            return StatusCode(500, new { error = "Failed to get available encoders" });
        }
    }

    /// <summary>
    /// Get encoding quality presets
    /// </summary>
    [HttpGet("presets")]
    public IActionResult GetEncodingPresets()
    {
        try
        {
            var presets = new object[]
            {
                new
                {
                    id = "ultra-fast",
                    name = "Ultra Fast",
                    description = "Fastest encoding, lower quality",
                    speed = "very-fast",
                    quality = "low",
                    ffmpegPreset = "ultrafast",
                    recommendedFor = new[] { "previews", "drafts" }
                },
                new
                {
                    id = "fast",
                    name = "Fast",
                    description = "Fast encoding, good quality",
                    speed = "fast",
                    quality = "good",
                    ffmpegPreset = "fast",
                    recommendedFor = new[] { "quick-exports", "social-media" }
                },
                new
                {
                    id = "balanced",
                    name = "Balanced",
                    description = "Balanced speed and quality",
                    speed = "medium",
                    quality = "high",
                    ffmpegPreset = "medium",
                    recommendedFor = new[] { "general-use", "youtube" },
                    default_ = true
                },
                new
                {
                    id = "high-quality",
                    name = "High Quality",
                    description = "Slower encoding, excellent quality",
                    speed = "slow",
                    quality = "very-high",
                    ffmpegPreset = "slow",
                    recommendedFor = new[] { "archival", "professional" }
                },
                new
                {
                    id = "maximum-quality",
                    name = "Maximum Quality",
                    description = "Very slow encoding, best quality",
                    speed = "very-slow",
                    quality = "maximum",
                    ffmpegPreset = "veryslow",
                    recommendedFor = new[] { "archival", "cinema" }
                }
            };

            return Ok(new { presets });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting encoding presets");
            return StatusCode(500, new { error = "Failed to get encoding presets" });
        }
    }

    /// <summary>
    /// Test hardware acceleration availability
    /// </summary>
    [HttpPost("test-acceleration")]
    public async Task<IActionResult> TestHardwareAcceleration(
        [FromBody] TestAccelerationRequest request,
        CancellationToken ct)
    {
        try
        {
            var systemProfile = await _hardwareDetector.DetectSystemAsync().ConfigureAwait(false);
            
            var results = new
            {
                nvenc = new
                {
                    available = systemProfile.EnableNVENC,
                    tested = false,
                    message = systemProfile.EnableNVENC
                        ? "NVIDIA NVENC detected and available" 
                        : "NVIDIA GPU not detected or NVENC not supported"
                },
                amf = new
                {
                    available = systemProfile.Gpu?.Vendor?.ToUpperInvariant() == "AMD",
                    tested = false,
                    message = systemProfile.Gpu?.Vendor?.ToUpperInvariant() == "AMD"
                        ? "AMD AMF detected and available" 
                        : "AMD GPU not detected or AMF not supported"
                },
                quickSync = new
                {
                    available = systemProfile.Gpu?.Vendor?.ToUpperInvariant() == "INTEL",
                    tested = false,
                    message = systemProfile.Gpu?.Vendor?.ToUpperInvariant() == "INTEL"
                        ? "Intel Quick Sync detected and available" 
                        : "Intel GPU not detected or Quick Sync not supported"
                }
            };

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing hardware acceleration");
            return StatusCode(500, new { error = "Failed to test hardware acceleration" });
        }
    }

    private async Task<HardwareConfigModel> LoadHardwareConfigAsync(CancellationToken ct)
    {
        if (!System.IO.File.Exists(_hardwareConfigPath))
        {
            return new HardwareConfigModel();
        }

        try
        {
            var json = await System.IO.File.ReadAllTextAsync(_hardwareConfigPath, ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<HardwareConfigModel>(json) ?? new HardwareConfigModel();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading hardware config, using defaults");
            return new HardwareConfigModel();
        }
    }

    private async Task SaveHardwareConfigAsync(HardwareConfigModel config, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(_hardwareConfigPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(config, options);
        await System.IO.File.WriteAllTextAsync(_hardwareConfigPath, json, ct).ConfigureAwait(false);
    }
}

/// <summary>
/// Hardware configuration model
/// </summary>
public class HardwareConfigModel
{
    public string PreferredGpuId { get; set; } = "auto";
    public bool EnableHardwareAcceleration { get; set; } = true;
    public string PreferredEncoder { get; set; } = "auto";
    public string EncodingPreset { get; set; } = "balanced";
    public bool UseGpuForImageGeneration { get; set; } = true;
    public int MaxConcurrentJobs { get; set; } = 1;
    public Dictionary<string, string> CustomEncoderSettings { get; set; } = new();
}

/// <summary>
/// Request model for testing hardware acceleration
/// </summary>
public class TestAccelerationRequest
{
    public string? AccelerationType { get; set; }
}
