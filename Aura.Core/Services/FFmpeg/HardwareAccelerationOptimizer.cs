using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.FFmpeg;

/// <summary>
/// Detects and configures hardware acceleration for FFmpeg
/// Supports NVENC (NVIDIA), AMF (AMD), QuickSync (Intel), and VideoToolbox (macOS)
/// </summary>
public class HardwareAccelerationOptimizer
{
    private readonly ILogger<HardwareAccelerationOptimizer> _logger;
    private readonly string _ffmpegPath;
    private OptimizerHardwareCapabilities? _capabilities;

    public HardwareAccelerationOptimizer(
        ILogger<HardwareAccelerationOptimizer> logger,
        string ffmpegPath = "ffmpeg")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ffmpegPath = ffmpegPath;
    }

    /// <summary>
    /// Detect available hardware acceleration capabilities
    /// </summary>
    public async Task<OptimizerHardwareCapabilities> DetectCapabilitiesAsync(
        CancellationToken cancellationToken = default)
    {
        if (_capabilities != null)
        {
            return _capabilities;
        }

        _logger.LogInformation("Detecting hardware acceleration capabilities");

        var capabilities = new OptimizerHardwareCapabilities
        {
            Platform = GetCurrentPlatform()
        };

        try
        {
            var encoders = await GetAvailableEncodersAsync(cancellationToken);
            var hwaccels = await GetAvailableHwAccelsAsync(cancellationToken);

            capabilities.SupportsNvenc = encoders.Contains("h264_nvenc") || encoders.Contains("hevc_nvenc");
            capabilities.SupportsAmf = encoders.Contains("h264_amf") || encoders.Contains("hevc_amf");
            capabilities.SupportsQuickSync = encoders.Contains("h264_qsv") || encoders.Contains("hevc_qsv");
            capabilities.SupportsVideoToolbox = encoders.Contains("h264_videotoolbox") || encoders.Contains("hevc_videotoolbox");
            capabilities.SupportsVaapi = hwaccels.Contains("vaapi");

            if (capabilities.SupportsNvenc)
            {
                _logger.LogInformation("NVIDIA NVENC hardware acceleration available");
            }
            if (capabilities.SupportsAmf)
            {
                _logger.LogInformation("AMD AMF hardware acceleration available");
            }
            if (capabilities.SupportsQuickSync)
            {
                _logger.LogInformation("Intel QuickSync hardware acceleration available");
            }
            if (capabilities.SupportsVideoToolbox)
            {
                _logger.LogInformation("Apple VideoToolbox hardware acceleration available");
            }
            if (capabilities.SupportsVaapi)
            {
                _logger.LogInformation("VAAPI hardware acceleration available");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect hardware acceleration capabilities, will use software encoding");
        }

        _capabilities = capabilities;
        return capabilities;
    }

    /// <summary>
    /// Optimize FFmpeg command builder with best available hardware acceleration
    /// </summary>
    public async Task<FFmpegCommandBuilder> OptimizeForHardwareAsync(
        FFmpegCommandBuilder builder,
        string codec = "h264",
        CancellationToken cancellationToken = default)
    {
        var capabilities = await DetectCapabilitiesAsync(cancellationToken);

        if (!capabilities.HasAnyAcceleration)
        {
            _logger.LogInformation("No hardware acceleration available, using software encoding");
            return OptimizeForSoftware(builder, codec);
        }

        return codec.ToLowerInvariant() switch
        {
            "h264" or "avc" => OptimizeH264(builder, capabilities),
            "h265" or "hevc" => OptimizeH265(builder, capabilities),
            "vp9" => OptimizeVp9(builder, capabilities),
            _ => OptimizeH264(builder, capabilities)
        };
    }

    /// <summary>
    /// Get optimal thread count based on CPU cores
    /// </summary>
    public int GetOptimalThreadCount()
    {
        var coreCount = Environment.ProcessorCount;
        
        // Use most cores but leave some for the system
        var threadCount = coreCount > 4 ? coreCount - 2 : coreCount;
        
        _logger.LogDebug("Using {ThreadCount} threads for FFmpeg (of {CoreCount} available cores)", 
            threadCount, coreCount);
        
        return threadCount;
    }

    /// <summary>
    /// Get optimal encoding preset based on system capabilities
    /// </summary>
    public string GetOptimalPreset(OptimizerHardwareCapabilities? capabilities = null)
    {
        capabilities ??= _capabilities;

        if (capabilities?.HasAnyAcceleration == true)
        {
            return "fast";
        }

        var coreCount = Environment.ProcessorCount;
        
        return coreCount switch
        {
            >= 16 => "medium",
            >= 8 => "fast",
            >= 4 => "faster",
            _ => "veryfast"
        };
    }

    private FFmpegCommandBuilder OptimizeH264(
        FFmpegCommandBuilder builder,
        OptimizerHardwareCapabilities capabilities)
    {
        if (capabilities.SupportsNvenc)
        {
            _logger.LogInformation("Using NVIDIA NVENC for H.264 encoding");
            return builder
                .SetHardwareAcceleration("cuda")
                .SetVideoCodec("h264_nvenc")
                .SetPreset("p4")
                .AddMetadata("encoder", "h264_nvenc");
        }

        if (capabilities.SupportsAmf)
        {
            _logger.LogInformation("Using AMD AMF for H.264 encoding");
            return builder
                .SetHardwareAcceleration("d3d11va")
                .SetVideoCodec("h264_amf")
                .SetPreset("balanced")
                .AddMetadata("encoder", "h264_amf");
        }

        if (capabilities.SupportsQuickSync)
        {
            _logger.LogInformation("Using Intel QuickSync for H.264 encoding");
            return builder
                .SetHardwareAcceleration("qsv")
                .SetVideoCodec("h264_qsv")
                .SetPreset("medium")
                .AddMetadata("encoder", "h264_qsv");
        }

        if (capabilities.SupportsVideoToolbox)
        {
            _logger.LogInformation("Using VideoToolbox for H.264 encoding");
            return builder
                .SetHardwareAcceleration("videotoolbox")
                .SetVideoCodec("h264_videotoolbox")
                .AddMetadata("encoder", "h264_videotoolbox");
        }

        if (capabilities.SupportsVaapi)
        {
            _logger.LogInformation("Using VAAPI for H.264 encoding");
            return builder
                .SetHardwareAcceleration("vaapi")
                .SetVideoCodec("h264_vaapi")
                .AddMetadata("encoder", "h264_vaapi");
        }

        return OptimizeForSoftware(builder, "h264");
    }

    private FFmpegCommandBuilder OptimizeH265(
        FFmpegCommandBuilder builder,
        OptimizerHardwareCapabilities capabilities)
    {
        if (capabilities.SupportsNvenc)
        {
            _logger.LogInformation("Using NVIDIA NVENC for H.265 encoding");
            return builder
                .SetHardwareAcceleration("cuda")
                .SetVideoCodec("hevc_nvenc")
                .SetPreset("p4")
                .AddMetadata("encoder", "hevc_nvenc");
        }

        if (capabilities.SupportsAmf)
        {
            _logger.LogInformation("Using AMD AMF for H.265 encoding");
            return builder
                .SetHardwareAcceleration("d3d11va")
                .SetVideoCodec("hevc_amf")
                .SetPreset("balanced")
                .AddMetadata("encoder", "hevc_amf");
        }

        if (capabilities.SupportsQuickSync)
        {
            _logger.LogInformation("Using Intel QuickSync for H.265 encoding");
            return builder
                .SetHardwareAcceleration("qsv")
                .SetVideoCodec("hevc_qsv")
                .SetPreset("medium")
                .AddMetadata("encoder", "hevc_qsv");
        }

        if (capabilities.SupportsVideoToolbox)
        {
            _logger.LogInformation("Using VideoToolbox for H.265 encoding");
            return builder
                .SetHardwareAcceleration("videotoolbox")
                .SetVideoCodec("hevc_videotoolbox")
                .AddMetadata("encoder", "hevc_videotoolbox");
        }

        return OptimizeForSoftware(builder, "h265");
    }

    private FFmpegCommandBuilder OptimizeVp9(
        FFmpegCommandBuilder builder,
        OptimizerHardwareCapabilities capabilities)
    {
        if (capabilities.SupportsVaapi)
        {
            _logger.LogInformation("Using VAAPI for VP9 encoding");
            return builder
                .SetHardwareAcceleration("vaapi")
                .SetVideoCodec("vp9_vaapi")
                .AddMetadata("encoder", "vp9_vaapi");
        }

        return OptimizeForSoftware(builder, "vp9");
    }

    private FFmpegCommandBuilder OptimizeForSoftware(
        FFmpegCommandBuilder builder,
        string codec)
    {
        _logger.LogInformation("Using software encoding for {Codec}", codec);

        var threadCount = GetOptimalThreadCount();
        var preset = GetOptimalPreset(null);

        var softwareCodec = codec.ToLowerInvariant() switch
        {
            "h264" or "avc" => "libx264",
            "h265" or "hevc" => "libx265",
            "vp9" => "libvpx-vp9",
            "vp8" => "libvpx",
            _ => "libx264"
        };

        return builder
            .SetVideoCodec(softwareCodec)
            .SetThreads(threadCount)
            .SetPreset(preset)
            .AddMetadata("encoder", softwareCodec);
    }

    private async Task<HashSet<string>> GetAvailableEncodersAsync(
        CancellationToken cancellationToken)
    {
        var encoders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = "-encoders",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                return encoders;
            }

            var output = new StringBuilder();
            while (!process.StandardOutput.EndOfStream)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    process.Kill();
                    break;
                }

                var line = await process.StandardOutput.ReadLineAsync();
                if (line != null)
                {
                    output.AppendLine(line);
                    
                    if (line.Contains("h264_nvenc") || line.Contains("hevc_nvenc") ||
                        line.Contains("h264_amf") || line.Contains("hevc_amf") ||
                        line.Contains("h264_qsv") || line.Contains("hevc_qsv") ||
                        line.Contains("h264_videotoolbox") || line.Contains("hevc_videotoolbox"))
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 1)
                        {
                            encoders.Add(parts[1]);
                        }
                    }
                }
            }

            await process.WaitForExitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query FFmpeg encoders");
        }

        return encoders;
    }

    private async Task<HashSet<string>> GetAvailableHwAccelsAsync(
        CancellationToken cancellationToken)
    {
        var hwaccels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = "-hwaccels",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                return hwaccels;
            }

            while (!process.StandardOutput.EndOfStream)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    process.Kill();
                    break;
                }

                var line = await process.StandardOutput.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(line) && 
                    !line.Contains("Hardware acceleration methods:"))
                {
                    hwaccels.Add(line.Trim());
                }
            }

            await process.WaitForExitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query FFmpeg hardware acceleration methods");
        }

        return hwaccels;
    }

    private static string GetCurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "Windows";
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "Linux";
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "macOS";
        }
        return "Unknown";
    }
}

/// <summary>
/// Hardware acceleration capabilities detected on the system (for optimizer)
/// </summary>
public class OptimizerHardwareCapabilities
{
    public string Platform { get; set; } = string.Empty;
    public bool SupportsNvenc { get; set; }
    public bool SupportsAmf { get; set; }
    public bool SupportsQuickSync { get; set; }
    public bool SupportsVideoToolbox { get; set; }
    public bool SupportsVaapi { get; set; }

    public bool HasAnyAcceleration =>
        SupportsNvenc || SupportsAmf || SupportsQuickSync || 
        SupportsVideoToolbox || SupportsVaapi;

    public string GetBestAccelerationType()
    {
        if (SupportsNvenc) return "NVIDIA NVENC";
        if (SupportsAmf) return "AMD AMF";
        if (SupportsQuickSync) return "Intel QuickSync";
        if (SupportsVideoToolbox) return "Apple VideoToolbox";
        if (SupportsVaapi) return "VAAPI";
        return "Software (CPU)";
    }
}
