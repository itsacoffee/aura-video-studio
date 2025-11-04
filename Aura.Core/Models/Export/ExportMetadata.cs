using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Export;

/// <summary>
/// Metadata captured after export completion
/// </summary>
public record ExportMetadata
{
    /// <summary>
    /// SHA256 hash of the exported file
    /// </summary>
    public string FileHash { get; init; } = string.Empty;

    /// <summary>
    /// Actual file size in bytes
    /// </summary>
    public long FileSizeBytes { get; init; }

    /// <summary>
    /// Actual video duration
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Video codec used
    /// </summary>
    public string VideoCodec { get; init; } = string.Empty;

    /// <summary>
    /// Audio codec used
    /// </summary>
    public string AudioCodec { get; init; } = string.Empty;

    /// <summary>
    /// Actual video resolution
    /// </summary>
    public Resolution Resolution { get; init; } = new(0, 0);

    /// <summary>
    /// Actual frame rate
    /// </summary>
    public double FrameRate { get; init; }

    /// <summary>
    /// Actual video bitrate in kbps
    /// </summary>
    public int VideoBitrate { get; init; }

    /// <summary>
    /// Actual audio bitrate in kbps
    /// </summary>
    public int AudioBitrate { get; init; }

    /// <summary>
    /// Container format
    /// </summary>
    public string Container { get; init; } = string.Empty;

    /// <summary>
    /// Pixel format
    /// </summary>
    public string PixelFormat { get; init; } = string.Empty;

    /// <summary>
    /// Color space
    /// </summary>
    public string ColorSpace { get; init; } = string.Empty;

    /// <summary>
    /// Encoder used (hardware or software)
    /// </summary>
    public string EncoderUsed { get; init; } = string.Empty;

    /// <summary>
    /// Encoding time taken
    /// </summary>
    public TimeSpan EncodingDuration { get; init; }

    /// <summary>
    /// Timestamp when export was completed
    /// </summary>
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Preset name used for export
    /// </summary>
    public string PresetName { get; init; } = string.Empty;

    /// <summary>
    /// Platform targeted
    /// </summary>
    public string Platform { get; init; } = string.Empty;

    /// <summary>
    /// Validation issues found during post-export check
    /// </summary>
    public List<string> ValidationIssues { get; init; } = new();

    /// <summary>
    /// Whether the export passed all validation checks
    /// </summary>
    public bool ValidationPassed { get; init; }

    /// <summary>
    /// Hardware tier at time of export
    /// </summary>
    public string HardwareTier { get; init; } = string.Empty;

    /// <summary>
    /// Whether hardware acceleration was used
    /// </summary>
    public bool HardwareAccelerationUsed { get; init; }

    /// <summary>
    /// Additional custom metadata
    /// </summary>
    public Dictionary<string, string> CustomMetadata { get; init; } = new();
}

/// <summary>
/// Extension methods for ExportMetadata
/// </summary>
public static class ExportMetadataExtensions
{
    /// <summary>
    /// Gets a human-readable summary of the export metadata
    /// </summary>
    public static string GetSummary(this ExportMetadata metadata)
    {
        var sizeMB = metadata.FileSizeBytes / 1024.0 / 1024.0;
        var durationStr = $"{metadata.Duration.TotalMinutes:F2}min";
        var resolutionStr = $"{metadata.Resolution.Width}x{metadata.Resolution.Height}";
        var bitrateStr = $"{metadata.VideoBitrate}kbps";
        
        return $"{resolutionStr} @ {metadata.FrameRate:F0}fps, {bitrateStr}, " +
               $"{sizeMB:F2}MB, {durationStr}, {metadata.VideoCodec}/{metadata.AudioCodec}";
    }

    private const double FrameRateTolerance = 0.1;

    /// <summary>
    /// Checks if the export matches expected preset parameters
    /// </summary>
    public static List<string> ValidateAgainstPreset(this ExportMetadata metadata, ExportPreset preset)
    {
        var issues = new List<string>();

        if (metadata.Resolution.Width != preset.Resolution.Width ||
            metadata.Resolution.Height != preset.Resolution.Height)
        {
            issues.Add(
                $"Resolution mismatch: expected {preset.Resolution.Width}x{preset.Resolution.Height}, " +
                $"got {metadata.Resolution.Width}x{metadata.Resolution.Height}"
            );
        }

        if (Math.Abs(metadata.FrameRate - preset.FrameRate) > FrameRateTolerance)
        {
            issues.Add(
                $"Frame rate mismatch: expected {preset.FrameRate}fps, " +
                $"got {metadata.FrameRate:F2}fps"
            );
        }

        return issues;
    }
}
