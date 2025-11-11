using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.FFmpeg;

/// <summary>
/// Detects available hardware acceleration capabilities for FFmpeg
/// Supports NVENC (NVIDIA), QuickSync (Intel), AMF (AMD), and VideoToolbox (macOS)
/// </summary>
public class HardwareAccelerationDetector
{
    private readonly ILogger<HardwareAccelerationDetector> _logger;
    private HardwareAccelerationCapabilities? _cachedCapabilities;

    public HardwareAccelerationDetector(ILogger<HardwareAccelerationDetector> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Detect all available hardware acceleration capabilities
    /// </summary>
    public async Task<HardwareAccelerationCapabilities> DetectCapabilitiesAsync(
        string ffmpegPath,
        CancellationToken ct = default)
    {
        if (_cachedCapabilities != null)
        {
            _logger.LogDebug("Returning cached hardware acceleration capabilities");
            return _cachedCapabilities;
        }

        _logger.LogInformation("Detecting hardware acceleration capabilities...");

        var capabilities = new HardwareAccelerationCapabilities
        {
            Platform = GetCurrentPlatform()
        };

        // Detect available encoders
        var encoders = await DetectAvailableEncodersAsync(ffmpegPath, ct);
        capabilities.AvailableEncoders = encoders;

        // Check for NVENC (NVIDIA)
        if (encoders.Contains("h264_nvenc") || encoders.Contains("hevc_nvenc"))
        {
            capabilities.NvencAvailable = true;
            capabilities.NvencVersion = await DetectNvencVersionAsync(ffmpegPath, ct);
            _logger.LogInformation("NVENC (NVIDIA) hardware acceleration detected");
        }

        // Check for QuickSync (Intel)
        if (encoders.Contains("h264_qsv") || encoders.Contains("hevc_qsv"))
        {
            capabilities.QuickSyncAvailable = true;
            _logger.LogInformation("QuickSync (Intel) hardware acceleration detected");
        }

        // Check for AMF (AMD)
        if (encoders.Contains("h264_amf") || encoders.Contains("hevc_amf"))
        {
            capabilities.AmfAvailable = true;
            _logger.LogInformation("AMF (AMD) hardware acceleration detected");
        }

        // Check for VideoToolbox (macOS)
        if (capabilities.Platform == HardwarePlatform.MacOS &&
            (encoders.Contains("h264_videotoolbox") || encoders.Contains("hevc_videotoolbox")))
        {
            capabilities.VideoToolboxAvailable = true;
            _logger.LogInformation("VideoToolbox (macOS) hardware acceleration detected");
        }

        // Check for VAAPI (Linux)
        if (capabilities.Platform == HardwarePlatform.Linux &&
            (encoders.Contains("h264_vaapi") || encoders.Contains("hevc_vaapi")))
        {
            capabilities.VaapiAvailable = true;
            _logger.LogInformation("VAAPI (Linux) hardware acceleration detected");
        }

        // Detect hardware decoders
        var decoders = await DetectAvailableDecodersAsync(ffmpegPath, ct);
        capabilities.HardwareDecodingAvailable = decoders.Count > 0;

        // Determine best encoder
        capabilities.RecommendedEncoder = DetermineRecommendedEncoder(capabilities);

        _cachedCapabilities = capabilities;
        _logger.LogInformation(
            "Hardware acceleration detection complete. Best encoder: {Encoder}",
            capabilities.RecommendedEncoder);

        return capabilities;
    }

    /// <summary>
    /// Detect available hardware encoders
    /// </summary>
    private async Task<HashSet<string>> DetectAvailableEncodersAsync(
        string ffmpegPath,
        CancellationToken ct)
    {
        var encoders = new HashSet<string>();

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = "-encoders -hide_banner",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                _logger.LogWarning("Failed to start FFmpeg for encoder detection");
                return encoders;
            }

            var output = await process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            // Parse encoder list
            var lines = output.Split('\n');
            var encoderPattern = new Regex(@"^\s*V\.+\s+(\S+)\s+");

            foreach (var line in lines)
            {
                var match = encoderPattern.Match(line);
                if (match.Success)
                {
                    var encoderName = match.Groups[1].Value;
                    encoders.Add(encoderName);
                }
            }

            _logger.LogDebug("Detected {Count} video encoders", encoders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect hardware encoders");
        }

        return encoders;
    }

    /// <summary>
    /// Detect available hardware decoders
    /// </summary>
    private async Task<HashSet<string>> DetectAvailableDecodersAsync(
        string ffmpegPath,
        CancellationToken ct)
    {
        var decoders = new HashSet<string>();

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = "-decoders -hide_banner",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return decoders;
            }

            var output = await process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            // Parse decoder list looking for hardware decoders
            var lines = output.Split('\n');
            var hardwareDecoderPattern = new Regex(@"(h264_cuvid|hevc_cuvid|h264_qsv|hevc_qsv|h264_videotoolbox)");

            foreach (var line in lines)
            {
                var match = hardwareDecoderPattern.Match(line);
                if (match.Success)
                {
                    decoders.Add(match.Value);
                }
            }

            _logger.LogDebug("Detected {Count} hardware video decoders", decoders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect hardware decoders");
        }

        return decoders;
    }

    /// <summary>
    /// Detect NVENC version if available
    /// </summary>
    private async Task<string?> DetectNvencVersionAsync(string ffmpegPath, CancellationToken ct)
    {
        try
        {
            // Try nvidia-smi if available on Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var nvidiaSmiPath = Environment.ExpandEnvironmentVariables(
                    @"%ProgramFiles%\NVIDIA Corporation\NVSMI\nvidia-smi.exe");

                if (System.IO.File.Exists(nvidiaSmiPath))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = nvidiaSmiPath,
                        Arguments = "--query-gpu=driver_version --format=csv,noheader",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(psi);
                    if (process != null)
                    {
                        var output = await process.StandardOutput.ReadToEndAsync(ct);
                        await process.WaitForExitAsync(ct);
                        
                        if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                        {
                            return output.Trim();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not detect NVENC version");
        }

        return null;
    }

    /// <summary>
    /// Determine the recommended encoder based on available hardware
    /// </summary>
    private string DetermineRecommendedEncoder(HardwareAccelerationCapabilities capabilities)
    {
        // Priority order: NVENC > AMF > QuickSync > VideoToolbox > VAAPI > Software
        if (capabilities.NvencAvailable)
        {
            return "h264_nvenc";
        }

        if (capabilities.AmfAvailable)
        {
            return "h264_amf";
        }

        if (capabilities.QuickSyncAvailable)
        {
            return "h264_qsv";
        }

        if (capabilities.VideoToolboxAvailable)
        {
            return "h264_videotoolbox";
        }

        if (capabilities.VaapiAvailable)
        {
            return "h264_vaapi";
        }

        return "libx264"; // Fallback to software encoding
    }

    /// <summary>
    /// Get the current hardware platform
    /// </summary>
    private HardwarePlatform GetCurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return HardwarePlatform.Windows;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return HardwarePlatform.MacOS;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return HardwarePlatform.Linux;
        }

        return HardwarePlatform.Unknown;
    }

    /// <summary>
    /// Invalidate cached capabilities (call after FFmpeg update)
    /// </summary>
    public void InvalidateCache()
    {
        _cachedCapabilities = null;
        _logger.LogInformation("Hardware acceleration cache invalidated");
    }
}

/// <summary>
/// Hardware acceleration capabilities
/// </summary>
public class HardwareAccelerationCapabilities
{
    public HardwarePlatform Platform { get; set; }
    public bool NvencAvailable { get; set; }
    public bool QuickSyncAvailable { get; set; }
    public bool AmfAvailable { get; set; }
    public bool VideoToolboxAvailable { get; set; }
    public bool VaapiAvailable { get; set; }
    public bool HardwareDecodingAvailable { get; set; }
    public string? NvencVersion { get; set; }
    public string RecommendedEncoder { get; set; } = "libx264";
    public HashSet<string> AvailableEncoders { get; set; } = new();

    public bool HasAnyHardwareAcceleration =>
        NvencAvailable || QuickSyncAvailable || AmfAvailable ||
        VideoToolboxAvailable || VaapiAvailable;

    public string GetBestEncoderForCodec(string codec)
    {
        return codec.ToLowerInvariant() switch
        {
            "h264" when NvencAvailable => "h264_nvenc",
            "h264" when AmfAvailable => "h264_amf",
            "h264" when QuickSyncAvailable => "h264_qsv",
            "h264" when VideoToolboxAvailable => "h264_videotoolbox",
            "h264" when VaapiAvailable => "h264_vaapi",
            "h264" => "libx264",
            
            "h265" or "hevc" when NvencAvailable => "hevc_nvenc",
            "h265" or "hevc" when AmfAvailable => "hevc_amf",
            "h265" or "hevc" when QuickSyncAvailable => "hevc_qsv",
            "h265" or "hevc" when VideoToolboxAvailable => "hevc_videotoolbox",
            "h265" or "hevc" when VaapiAvailable => "hevc_vaapi",
            "h265" or "hevc" => "libx265",
            
            _ => RecommendedEncoder
        };
    }
}

public enum HardwarePlatform
{
    Unknown,
    Windows,
    MacOS,
    Linux
}
