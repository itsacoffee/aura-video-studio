using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.FFmpeg;

/// <summary>
/// Hardware acceleration capabilities for FFmpeg
/// </summary>
public record HardwareAccelerationInfo
{
    public bool IsAvailable { get; init; }
    public string AccelerationType { get; init; } = string.Empty; // nvenc, qsv, amf, videotoolbox
    public string VideoCodec { get; init; } = string.Empty; // h264_nvenc, hevc_nvenc, etc.
    public List<string> SupportedEncoders { get; init; } = new();
    public List<string> SupportedDecoders { get; init; } = new();
    public string HwaccelDevice { get; init; } = string.Empty; // cuda, qsv, dxva2, etc.
    public string RecommendedPreset { get; init; } = "medium";
    public int MaxConcurrentEncodes { get; init; } = 1;
}

/// <summary>
/// Service for detecting and configuring hardware acceleration for FFmpeg
/// </summary>
public interface IHardwareAccelerationDetector
{
    /// <summary>
    /// Detect available hardware acceleration
    /// </summary>
    Task<HardwareAccelerationInfo> DetectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get optimal encoder settings for detected hardware
    /// </summary>
    Task<EncoderSettings> GetOptimalEncoderSettingsAsync(
        HardwareAccelerationInfo hwInfo,
        Resolution resolution,
        int targetBitrate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a specific codec is supported
    /// </summary>
    Task<bool> IsCodecSupportedAsync(string codec, CancellationToken cancellationToken = default);
}

/// <summary>
/// Encoder settings optimized for hardware
/// </summary>
public record EncoderSettings
{
    public string Encoder { get; init; } = "libx264";
    public string Preset { get; init; } = "medium";
    public string HwaccelFlag { get; init; } = string.Empty;
    public Dictionary<string, string> EncoderOptions { get; init; } = new();
    public int? Crf { get; init; }
    public int? Bitrate { get; init; }
}

/// <summary>
/// Implementation of hardware acceleration detector
/// </summary>
public class HardwareAccelerationDetector : IHardwareAccelerationDetector
{
    private readonly IFFmpegService _ffmpegService;
    private readonly IHardwareDetector _hardwareDetector;
    private readonly ILogger<HardwareAccelerationDetector> _logger;
    private HardwareAccelerationInfo? _cachedInfo;

    public HardwareAccelerationDetector(
        IFFmpegService ffmpegService,
        IHardwareDetector hardwareDetector,
        ILogger<HardwareAccelerationDetector> logger)
    {
        _ffmpegService = ffmpegService ?? throw new ArgumentNullException(nameof(ffmpegService));
        _hardwareDetector = hardwareDetector ?? throw new ArgumentNullException(nameof(hardwareDetector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HardwareAccelerationInfo> DetectAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedInfo != null)
        {
            return _cachedInfo;
        }

        _logger.LogInformation("Detecting hardware acceleration capabilities");

        // Get system hardware info
        var systemProfile = await _hardwareDetector.DetectSystemAsync().ConfigureAwait(false);

        // Query FFmpeg for available encoders and decoders
        var encodersResult = await _ffmpegService.ExecuteAsync(
            "-hide_banner -encoders",
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        var decodersResult = await _ffmpegService.ExecuteAsync(
            "-hide_banner -decoders",
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        var hwaccelsResult = await _ffmpegService.ExecuteAsync(
            "-hide_banner -hwaccels",
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        // Parse available encoders
        var supportedEncoders = ParseEncoders(encodersResult.StandardOutput);
        var supportedDecoders = ParseDecoders(decodersResult.StandardOutput);
        var hwaccels = ParseHwaccels(hwaccelsResult.StandardOutput);

        // Determine best hardware acceleration based on GPU
        var hwInfo = DetermineHardwareAcceleration(
            systemProfile,
            supportedEncoders,
            supportedDecoders,
            hwaccels
        );

        _logger.LogInformation(
            "Hardware acceleration detected: {Type}, Codec: {Codec}, Available: {Available}",
            hwInfo.AccelerationType,
            hwInfo.VideoCodec,
            hwInfo.IsAvailable
        );

        _cachedInfo = hwInfo;
        return hwInfo;
    }

    public async Task<EncoderSettings> GetOptimalEncoderSettingsAsync(
        HardwareAccelerationInfo hwInfo,
        Resolution resolution,
        int targetBitrate,
        CancellationToken cancellationToken = default)
    {
        if (!hwInfo.IsAvailable)
        {
            // Software encoding fallback
            return new EncoderSettings
            {
                Encoder = "libx264",
                Preset = DeterminePresetForResolution(resolution),
                Crf = DetermineCrfForQuality(resolution),
                EncoderOptions = new Dictionary<string, string>
                {
                    { "profile", "high" },
                    { "level", "4.1" }
                }
            };
        }

        // Hardware encoding
        var settings = new EncoderSettings
        {
            Encoder = hwInfo.VideoCodec,
            Preset = hwInfo.RecommendedPreset,
            HwaccelFlag = hwInfo.HwaccelDevice,
            Bitrate = targetBitrate
        };

        // Add codec-specific options
        if (hwInfo.AccelerationType == "nvenc")
        {
            settings = settings with
            {
                EncoderOptions = new Dictionary<string, string>
                {
                    { "rc", "vbr" }, // Variable bitrate
                    { "profile", "high" },
                    { "level", "auto" },
                    { "b:v", $"{targetBitrate}k" },
                    { "maxrate", $"{(int)(targetBitrate * 1.5)}k" },
                    { "bufsize", $"{targetBitrate * 2}k" }
                }
            };
        }
        else if (hwInfo.AccelerationType == "qsv")
        {
            settings = settings with
            {
                EncoderOptions = new Dictionary<string, string>
                {
                    { "profile", "high" },
                    { "level", "auto" },
                    { "look_ahead", "1" },
                    { "b:v", $"{targetBitrate}k" }
                }
            };
        }
        else if (hwInfo.AccelerationType == "amf")
        {
            settings = settings with
            {
                EncoderOptions = new Dictionary<string, string>
                {
                    { "quality", "balanced" },
                    { "rc", "vbr_latency" },
                    { "b:v", $"{targetBitrate}k" }
                }
            };
        }

        await Task.CompletedTask;
        return settings;
    }

    public async Task<bool> IsCodecSupportedAsync(string codec, CancellationToken cancellationToken = default)
    {
        var hwInfo = await DetectAsync(cancellationToken).ConfigureAwait(false);
        return hwInfo.SupportedEncoders.Contains(codec, StringComparer.OrdinalIgnoreCase);
    }

    private HardwareAccelerationInfo DetermineHardwareAcceleration(
        SystemProfile systemProfile,
        List<string> supportedEncoders,
        List<string> supportedDecoders,
        List<string> hwaccels)
    {
        var gpu = systemProfile.Gpu;

        // NVIDIA GPUs
        if (gpu?.Vendor?.Equals("NVIDIA", StringComparison.OrdinalIgnoreCase) == true)
        {
            if (supportedEncoders.Contains("h264_nvenc") && hwaccels.Contains("cuda"))
            {
                return new HardwareAccelerationInfo
                {
                    IsAvailable = true,
                    AccelerationType = "nvenc",
                    VideoCodec = "h264_nvenc",
                    SupportedEncoders = supportedEncoders.Where(e => e.Contains("nvenc")).ToList(),
                    SupportedDecoders = supportedDecoders.Where(d => d.Contains("cuvid")).ToList(),
                    HwaccelDevice = "cuda",
                    RecommendedPreset = DetermineNvencPreset(gpu),
                    MaxConcurrentEncodes = DetermineMaxConcurrentEncodes(gpu)
                };
            }
        }

        // Intel GPUs
        if (gpu?.Vendor?.Equals("Intel", StringComparison.OrdinalIgnoreCase) == true)
        {
            if (supportedEncoders.Contains("h264_qsv") && hwaccels.Contains("qsv"))
            {
                return new HardwareAccelerationInfo
                {
                    IsAvailable = true,
                    AccelerationType = "qsv",
                    VideoCodec = "h264_qsv",
                    SupportedEncoders = supportedEncoders.Where(e => e.Contains("qsv")).ToList(),
                    SupportedDecoders = supportedDecoders.Where(d => d.Contains("qsv")).ToList(),
                    HwaccelDevice = "qsv",
                    RecommendedPreset = "medium",
                    MaxConcurrentEncodes = 2
                };
            }
        }

        // AMD GPUs
        if (gpu?.Vendor?.Equals("AMD", StringComparison.OrdinalIgnoreCase) == true)
        {
            if (supportedEncoders.Contains("h264_amf"))
            {
                return new HardwareAccelerationInfo
                {
                    IsAvailable = true,
                    AccelerationType = "amf",
                    VideoCodec = "h264_amf",
                    SupportedEncoders = supportedEncoders.Where(e => e.Contains("amf")).ToList(),
                    SupportedDecoders = supportedDecoders,
                    HwaccelDevice = "dxva2",
                    RecommendedPreset = "balanced",
                    MaxConcurrentEncodes = 2
                };
            }
        }

        // No hardware acceleration available
        _logger.LogWarning("No hardware acceleration available, falling back to software encoding");
        return new HardwareAccelerationInfo
        {
            IsAvailable = false,
            AccelerationType = "software",
            VideoCodec = "libx264",
            SupportedEncoders = new List<string> { "libx264", "libx265" },
            SupportedDecoders = supportedDecoders,
            RecommendedPreset = "medium",
            MaxConcurrentEncodes = 1
        };
    }

    private string DetermineNvencPreset(GpuInfo gpu)
    {
        // Newer GPUs (40/50 series) can handle slower presets for better quality
        if (gpu.Series == "40" || gpu.Series == "50")
        {
            return "p5"; // High quality preset
        }

        // Mid-range GPUs
        if (gpu.Series == "30" || gpu.Series == "20")
        {
            return "p4"; // Balanced preset
        }

        // Older GPUs
        return "p3"; // Fast preset
    }

    private int DetermineMaxConcurrentEncodes(GpuInfo gpu)
    {
        // High-end GPUs can handle multiple concurrent encodes
        if (gpu.VramGB >= 16)
        {
            return 3;
        }

        if (gpu.VramGB >= 8)
        {
            return 2;
        }

        return 1;
    }

    private string DeterminePresetForResolution(Resolution resolution)
    {
        // Higher resolutions need faster presets for reasonable encoding times
        if (resolution.Width >= 3840) // 4K
        {
            return "fast";
        }

        if (resolution.Width >= 1920) // 1080p
        {
            return "medium";
        }

        return "slow"; // 720p and below can use slower presets
    }

    private int DetermineCrfForQuality(Resolution resolution)
    {
        // CRF values: lower = better quality, higher file size
        if (resolution.Width >= 3840) // 4K
        {
            return 20; // Higher quality for 4K
        }

        if (resolution.Width >= 1920) // 1080p
        {
            return 23; // Standard quality
        }

        return 26; // Lower resolution, can use lower quality
    }

    private List<string> ParseEncoders(string ffmpegOutput)
    {
        var encoders = new List<string>();
        var lines = ffmpegOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            // FFmpeg encoder format: " V..... h264_nvenc           NVIDIA NVENC H.264 encoder"
            if (line.Length > 8 && (line[1] == 'V' || line[1] == 'A'))
            {
                var parts = line.Substring(7).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    encoders.Add(parts[0].Trim());
                }
            }
        }

        return encoders;
    }

    private List<string> ParseDecoders(string ffmpegOutput)
    {
        var decoders = new List<string>();
        var lines = ffmpegOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.Length > 8 && (line[1] == 'V' || line[1] == 'A'))
            {
                var parts = line.Substring(7).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    decoders.Add(parts[0].Trim());
                }
            }
        }

        return decoders;
    }

    private List<string> ParseHwaccels(string ffmpegOutput)
    {
        var hwaccels = new List<string>();
        var lines = ffmpegOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed) && 
                !trimmed.StartsWith("Hardware", StringComparison.OrdinalIgnoreCase) &&
                !trimmed.StartsWith("---", StringComparison.Ordinal))
            {
                hwaccels.Add(trimmed);
            }
        }

        return hwaccels;
    }
}
