using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Models;

namespace Aura.Core.Rendering;

/// <summary>
/// Provides preset configurations for common video rendering scenarios.
/// </summary>
public static class RenderPresets
{
    /// <summary>
    /// YouTube 1080p preset (1920x1080, H.264, AAC)
    /// </summary>
    public static RenderSpec YouTube1080p => new RenderSpec(
        Res: new Resolution(1920, 1080),
        Container: "mp4",
        VideoBitrateK: 12000,
        AudioBitrateK: 256,
        Fps: 30,
        Codec: "H264",
        QualityLevel: 75,
        EnableSceneCut: true
    );

    /// <summary>
    /// YouTube Shorts preset (1080x1920, H.264, AAC) - Vertical format
    /// </summary>
    public static RenderSpec YouTubeShorts => new RenderSpec(
        Res: new Resolution(1080, 1920),
        Container: "mp4",
        VideoBitrateK: 10000,
        AudioBitrateK: 256,
        Fps: 30,
        Codec: "H264",
        QualityLevel: 75,
        EnableSceneCut: true
    );

    /// <summary>
    /// YouTube 4K preset (3840x2160, H.264, AAC)
    /// </summary>
    public static RenderSpec YouTube4K => new RenderSpec(
        Res: new Resolution(3840, 2160),
        Container: "mp4",
        VideoBitrateK: 45000,
        AudioBitrateK: 320,
        Fps: 30,
        Codec: "H264",
        QualityLevel: 75,
        EnableSceneCut: true
    );

    /// <summary>
    /// YouTube 1440p preset (2560x1440, H.264, AAC)
    /// </summary>
    public static RenderSpec YouTube1440p => new RenderSpec(
        Res: new Resolution(2560, 1440),
        Container: "mp4",
        VideoBitrateK: 24000,
        AudioBitrateK: 256,
        Fps: 30,
        Codec: "H264",
        QualityLevel: 75,
        EnableSceneCut: true
    );

    /// <summary>
    /// YouTube 720p preset (1280x720, H.264, AAC)
    /// </summary>
    public static RenderSpec YouTube720p => new RenderSpec(
        Res: new Resolution(1280, 720),
        Container: "mp4",
        VideoBitrateK: 8000,
        AudioBitrateK: 192,
        Fps: 30,
        Codec: "H264",
        QualityLevel: 75,
        EnableSceneCut: true
    );

    /// <summary>
    /// Instagram Square preset (1080x1080, H.264, AAC)
    /// </summary>
    public static RenderSpec InstagramSquare => new RenderSpec(
        Res: new Resolution(1080, 1080),
        Container: "mp4",
        VideoBitrateK: 8000,
        AudioBitrateK: 192,
        Fps: 30,
        Codec: "H264",
        QualityLevel: 75,
        EnableSceneCut: true
    );

    /// <summary>
    /// Gets a preset by name.
    /// </summary>
    public static RenderSpec? GetPresetByName(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "youtube1080p" or "youtube 1080p" or "1080p" => YouTube1080p,
            "youtubeshorts" or "youtube shorts" or "shorts" or "vertical" => YouTubeShorts,
            "youtube4k" or "youtube 4k" or "4k" or "2160p" => YouTube4K,
            "youtube1440p" or "youtube 1440p" or "1440p" or "2k" => YouTube1440p,
            "youtube720p" or "youtube 720p" or "720p" => YouTube720p,
            "instagramsquare" or "instagram square" or "square" => InstagramSquare,
            _ => null
        };
    }

    /// <summary>
    /// Gets all available preset names.
    /// </summary>
    public static IReadOnlyList<string> GetPresetNames()
    {
        return new[]
        {
            "YouTube 1080p",
            "YouTube Shorts",
            "YouTube 4K",
            "YouTube 1440p",
            "YouTube 720p",
            "Instagram Square"
        };
    }

    /// <summary>
    /// Creates a custom render spec with validation.
    /// </summary>
    public static RenderSpec CreateCustom(
        int width,
        int height,
        string container = "mp4",
        int videoBitrateK = 12000,
        int audioBitrateK = 256)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Width and height must be positive");

        if (videoBitrateK <= 0 || audioBitrateK <= 0)
            throw new ArgumentException("Bitrates must be positive");

        var validContainers = new[] { "mp4", "mkv", "mov", "webm" };
        if (!validContainers.Contains(container.ToLowerInvariant()))
            throw new ArgumentException($"Container must be one of: {string.Join(", ", validContainers)}");

        return new RenderSpec(
            Res: new Resolution(width, height),
            Container: container,
            VideoBitrateK: videoBitrateK,
            AudioBitrateK: audioBitrateK
        );
    }

    /// <summary>
    /// Suggests an appropriate bitrate based on resolution and framerate.
    /// </summary>
    public static int SuggestVideoBitrate(Resolution resolution, int framerate = 30)
    {
        long pixelCount = (long)resolution.Width * resolution.Height;
        
        // Base bitrate calculation: pixels * framerate * quality factor
        // Quality factor ranges from 0.1 (lower quality) to 0.2 (higher quality)
        double qualityFactor = 0.15;
        int suggestedBitrate = (int)(pixelCount * framerate * qualityFactor / 1000);

        // Apply reasonable bounds
        return Math.Clamp(suggestedBitrate, 2000, 100000);
    }

    /// <summary>
    /// Determines if a resolution requires high-tier hardware.
    /// </summary>
    public static bool RequiresHighTierHardware(Resolution resolution)
    {
        long pixelCount = (long)resolution.Width * resolution.Height;
        
        // 4K (3840x2160) = 8,294,400 pixels
        // Consider anything >= 4K as requiring high-tier hardware
        return pixelCount >= 8000000;
    }
}
