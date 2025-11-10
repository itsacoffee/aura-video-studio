using System;
using System.Collections.Generic;
using Aura.Core.Models;

namespace Aura.Core.Services.FFmpeg;

/// <summary>
/// Quality presets for FFmpeg video encoding
/// </summary>
public static class FFmpegQualityPresets
{
    /// <summary>
    /// Get a quality preset configuration
    /// </summary>
    public static QualityPreset GetPreset(QualityLevel quality, string codec = "libx264")
    {
        return quality switch
        {
            QualityLevel.Draft => GetDraftPreset(codec),
            QualityLevel.Good => GetStandardPreset(codec),
            QualityLevel.High => GetPremiumPreset(codec),
            QualityLevel.Maximum => GetMaximumPreset(codec),
            _ => GetStandardPreset(codec)
        };
    }

    private static QualityPreset GetDraftPreset(string codec)
    {
        return new QualityPreset
        {
            Name = "Draft",
            Description = "Fast encoding, lower quality - ideal for previews and testing",
            Codec = codec,
            Preset = "ultrafast",
            CRF = 28,
            VideoBitrate = 1500,
            AudioBitrate = 96,
            PixelFormat = "yuv420p",
            TwoPass = false,
            MaxDimension = 1280,
            TargetFps = 30,
            Profile = "baseline",
            Level = "3.1",
            EncoderOptions = new Dictionary<string, string>
            {
                { "tune", "fastdecode" },
                { "movflags", "+faststart" }
            }
        };
    }

    private static QualityPreset GetStandardPreset(string codec)
    {
        return new QualityPreset
        {
            Name = "Standard",
            Description = "Balanced quality and file size - good for most use cases",
            Codec = codec,
            Preset = "medium",
            CRF = 23,
            VideoBitrate = 5000,
            AudioBitrate = 192,
            PixelFormat = "yuv420p",
            TwoPass = false,
            MaxDimension = 1920,
            TargetFps = 30,
            Profile = "main",
            Level = "4.0",
            EncoderOptions = new Dictionary<string, string>
            {
                { "tune", "film" },
                { "movflags", "+faststart" },
                { "bf", "2" }
            }
        };
    }

    private static QualityPreset GetPremiumPreset(string codec)
    {
        return new QualityPreset
        {
            Name = "Premium",
            Description = "High quality encoding - great for distribution and archival",
            Codec = codec,
            Preset = "slow",
            CRF = 18,
            VideoBitrate = 8000,
            AudioBitrate = 320,
            PixelFormat = "yuv420p",
            TwoPass = true,
            MaxDimension = 3840,
            TargetFps = 60,
            Profile = "high",
            Level = "5.1",
            EncoderOptions = new Dictionary<string, string>
            {
                { "tune", "film" },
                { "movflags", "+faststart" },
                { "bf", "3" },
                { "refs", "4" }
            }
        };
    }

    private static QualityPreset GetMaximumPreset(string codec)
    {
        return new QualityPreset
        {
            Name = "Maximum",
            Description = "Best possible quality - slow encoding, large files",
            Codec = codec,
            Preset = "veryslow",
            CRF = 15,
            VideoBitrate = 12000,
            AudioBitrate = 320,
            PixelFormat = "yuv420p",
            TwoPass = true,
            MaxDimension = 3840,
            TargetFps = 60,
            Profile = "high",
            Level = "5.2",
            EncoderOptions = new Dictionary<string, string>
            {
                { "tune", "film" },
                { "movflags", "+faststart" },
                { "bf", "5" },
                { "refs", "6" },
                { "me_method", "umh" },
                { "subq", "10" }
            }
        };
    }

    /// <summary>
    /// Apply a quality preset to an FFmpeg command builder
    /// </summary>
    public static FFmpegCommandBuilder ApplyPreset(this FFmpegCommandBuilder builder, QualityPreset preset)
    {
        builder
            .SetVideoCodec(preset.Codec)
            .SetPreset(preset.Preset)
            .SetCRF(preset.CRF)
            .SetVideoBitrate(preset.VideoBitrate)
            .SetAudioBitrate(preset.AudioBitrate)
            .SetPixelFormat(preset.PixelFormat);
        
        // Apply custom encoder options via metadata (would need FFmpegCommandBuilder enhancement to support these directly)
        foreach (var option in preset.EncoderOptions)
        {
            builder.AddMetadata($"encoder_{option.Key}", option.Value);
        }
        
        return builder;
    }
}

/// <summary>
/// Quality preset configuration
/// </summary>
public class QualityPreset
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Codec { get; init; }
    public required string Preset { get; init; }
    public required int CRF { get; init; }
    public required int VideoBitrate { get; init; }
    public required int AudioBitrate { get; init; }
    public required string PixelFormat { get; init; }
    public required bool TwoPass { get; init; }
    public required int MaxDimension { get; init; }
    public required int TargetFps { get; init; }
    public required string Profile { get; init; }
    public required string Level { get; init; }
    public required Dictionary<string, string> EncoderOptions { get; init; }
}
