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


