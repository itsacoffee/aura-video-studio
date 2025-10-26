using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Export;

/// <summary>
/// Aspect ratio for video exports
/// </summary>
public enum AspectRatio
{
    /// <summary>16:9 - Standard widescreen (YouTube, Facebook, Twitter)</summary>
    SixteenByNine,
    
    /// <summary>9:16 - Vertical (TikTok, Instagram Story)</summary>
    NineBySixteen,
    
    /// <summary>1:1 - Square (Instagram Feed)</summary>
    OneByOne,
    
    /// <summary>4:5 - Portrait (Instagram Feed portrait)</summary>
    FourByFive
}

/// <summary>
/// Quality level for exports
/// </summary>
public enum QualityLevel
{
    /// <summary>Draft quality for quick previews</summary>
    Draft,
    
    /// <summary>Good quality for sharing</summary>
    Good,
    
    /// <summary>High quality for professional use</summary>
    High,
    
    /// <summary>Maximum quality for archival</summary>
    Maximum
}

/// <summary>
/// Target platform for the export
/// </summary>
public enum Platform
{
    YouTube,
    TikTok,
    Instagram,
    Facebook,
    Twitter,
    LinkedIn,
    Generic
}

/// <summary>
/// Export preset configuration
/// </summary>
public record ExportPreset(
    string Name,
    string Description,
    Platform Platform,
    string Container,
    string VideoCodec,
    string AudioCodec,
    Resolution Resolution,
    int FrameRate,
    int VideoBitrate,
    int AudioBitrate,
    string PixelFormat,
    string ColorSpace,
    AspectRatio AspectRatio,
    QualityLevel Quality,
    int? MaxDuration = null);

/// <summary>
/// Provides predefined export presets for popular platforms
/// </summary>
public static class ExportPresets
{
    /// <summary>
    /// YouTube 1080p preset (1920x1080, H.264, 8Mbps, AAC 192kbps, 16:9)
    /// </summary>
    public static ExportPreset YouTube1080p => new(
        Name: "YouTube 1080p",
        Description: "Standard HD quality for YouTube uploads",
        Platform: Platform.YouTube,
        Container: "mp4",
        VideoCodec: "libx264",
        AudioCodec: "aac",
        Resolution: new Resolution(1920, 1080),
        FrameRate: 30,
        VideoBitrate: 8000,
        AudioBitrate: 192,
        PixelFormat: "yuv420p",
        ColorSpace: "bt709",
        AspectRatio: AspectRatio.SixteenByNine,
        Quality: QualityLevel.High
    );

    /// <summary>
    /// YouTube 4K preset (3840x2160, H.265, 20Mbps, 16:9)
    /// </summary>
    public static ExportPreset YouTube4K => new(
        Name: "YouTube 4K",
        Description: "Ultra HD quality for YouTube 4K uploads",
        Platform: Platform.YouTube,
        Container: "mp4",
        VideoCodec: "libx265",
        AudioCodec: "aac",
        Resolution: new Resolution(3840, 2160),
        FrameRate: 30,
        VideoBitrate: 20000,
        AudioBitrate: 192,
        PixelFormat: "yuv420p",
        ColorSpace: "bt709",
        AspectRatio: AspectRatio.SixteenByNine,
        Quality: QualityLevel.High
    );

    /// <summary>
    /// Instagram Feed preset (1080x1080, H.264, 5Mbps, 1:1)
    /// </summary>
    public static ExportPreset InstagramFeed => new(
        Name: "Instagram Feed",
        Description: "Square format optimized for Instagram feed posts",
        Platform: Platform.Instagram,
        Container: "mp4",
        VideoCodec: "libx264",
        AudioCodec: "aac",
        Resolution: new Resolution(1080, 1080),
        FrameRate: 30,
        VideoBitrate: 5000,
        AudioBitrate: 192,
        PixelFormat: "yuv420p",
        ColorSpace: "bt709",
        AspectRatio: AspectRatio.OneByOne,
        Quality: QualityLevel.High
    );

    /// <summary>
    /// Instagram Story preset (1080x1920, H.264, 5Mbps, 9:16)
    /// </summary>
    public static ExportPreset InstagramStory => new(
        Name: "Instagram Story",
        Description: "Vertical format for Instagram Stories",
        Platform: Platform.Instagram,
        Container: "mp4",
        VideoCodec: "libx264",
        AudioCodec: "aac",
        Resolution: new Resolution(1080, 1920),
        FrameRate: 30,
        VideoBitrate: 5000,
        AudioBitrate: 192,
        PixelFormat: "yuv420p",
        ColorSpace: "bt709",
        AspectRatio: AspectRatio.NineBySixteen,
        Quality: QualityLevel.High
    );

    /// <summary>
    /// TikTok preset (1080x1920, H.264, 5Mbps, 9:16, max 60s)
    /// </summary>
    public static ExportPreset TikTok => new(
        Name: "TikTok",
        Description: "Vertical format optimized for TikTok",
        Platform: Platform.TikTok,
        Container: "mp4",
        VideoCodec: "libx264",
        AudioCodec: "aac",
        Resolution: new Resolution(1080, 1920),
        FrameRate: 30,
        VideoBitrate: 5000,
        AudioBitrate: 192,
        PixelFormat: "yuv420p",
        ColorSpace: "bt709",
        AspectRatio: AspectRatio.NineBySixteen,
        Quality: QualityLevel.High,
        MaxDuration: 60
    );

    /// <summary>
    /// Facebook preset (1280x720, H.264, 4Mbps, 16:9)
    /// </summary>
    public static ExportPreset Facebook => new(
        Name: "Facebook",
        Description: "Optimized for Facebook video posts",
        Platform: Platform.Facebook,
        Container: "mp4",
        VideoCodec: "libx264",
        AudioCodec: "aac",
        Resolution: new Resolution(1280, 720),
        FrameRate: 30,
        VideoBitrate: 4000,
        AudioBitrate: 192,
        PixelFormat: "yuv420p",
        ColorSpace: "bt709",
        AspectRatio: AspectRatio.SixteenByNine,
        Quality: QualityLevel.Good
    );

    /// <summary>
    /// Twitter preset (1280x720, H.264, 5Mbps, 16:9, max 140s)
    /// </summary>
    public static ExportPreset Twitter => new(
        Name: "Twitter",
        Description: "Optimized for Twitter video posts",
        Platform: Platform.Twitter,
        Container: "mp4",
        VideoCodec: "libx264",
        AudioCodec: "aac",
        Resolution: new Resolution(1280, 720),
        FrameRate: 30,
        VideoBitrate: 5000,
        AudioBitrate: 192,
        PixelFormat: "yuv420p",
        ColorSpace: "bt709",
        AspectRatio: AspectRatio.SixteenByNine,
        Quality: QualityLevel.Good,
        MaxDuration: 140
    );

    /// <summary>
    /// LinkedIn preset (1920x1080, H.264, 5Mbps, 16:9)
    /// </summary>
    public static ExportPreset LinkedIn => new(
        Name: "LinkedIn",
        Description: "Professional quality for LinkedIn posts",
        Platform: Platform.LinkedIn,
        Container: "mp4",
        VideoCodec: "libx264",
        AudioCodec: "aac",
        Resolution: new Resolution(1920, 1080),
        FrameRate: 30,
        VideoBitrate: 5000,
        AudioBitrate: 192,
        PixelFormat: "yuv420p",
        ColorSpace: "bt709",
        AspectRatio: AspectRatio.SixteenByNine,
        Quality: QualityLevel.High
    );

    /// <summary>
    /// Email/Web preset (854x480, H.264, 2Mbps, 16:9, high compression)
    /// </summary>
    public static ExportPreset EmailWeb => new(
        Name: "Email/Web",
        Description: "Small file size for email attachments and web embedding",
        Platform: Platform.Generic,
        Container: "mp4",
        VideoCodec: "libx264",
        AudioCodec: "aac",
        Resolution: new Resolution(854, 480),
        FrameRate: 30,
        VideoBitrate: 2000,
        AudioBitrate: 128,
        PixelFormat: "yuv420p",
        ColorSpace: "bt709",
        AspectRatio: AspectRatio.SixteenByNine,
        Quality: QualityLevel.Good
    );

    /// <summary>
    /// Draft Preview preset (1280x720, H.264 ultrafast, 3Mbps for quick review)
    /// </summary>
    public static ExportPreset DraftPreview => new(
        Name: "Draft Preview",
        Description: "Quick low-quality preview for reviewing edits",
        Platform: Platform.Generic,
        Container: "mp4",
        VideoCodec: "libx264",
        AudioCodec: "aac",
        Resolution: new Resolution(1280, 720),
        FrameRate: 30,
        VideoBitrate: 3000,
        AudioBitrate: 128,
        PixelFormat: "yuv420p",
        ColorSpace: "bt709",
        AspectRatio: AspectRatio.SixteenByNine,
        Quality: QualityLevel.Draft
    );

    /// <summary>
    /// Master Archive preset (original resolution, H.265, 15Mbps, preservation quality)
    /// </summary>
    public static ExportPreset MasterArchive => new(
        Name: "Master Archive",
        Description: "High quality archival format with excellent compression",
        Platform: Platform.Generic,
        Container: "mp4",
        VideoCodec: "libx265",
        AudioCodec: "aac",
        Resolution: new Resolution(1920, 1080),
        FrameRate: 30,
        VideoBitrate: 15000,
        AudioBitrate: 256,
        PixelFormat: "yuv420p",
        ColorSpace: "bt709",
        AspectRatio: AspectRatio.SixteenByNine,
        Quality: QualityLevel.Maximum
    );

    /// <summary>
    /// WebM VP9 preset (1920x1080, VP9, optimized for web)
    /// </summary>
    public static ExportPreset WebMVP9 => new(
        Name: "WebM VP9",
        Description: "Web-optimized format with excellent compression",
        Platform: Platform.Generic,
        Container: "webm",
        VideoCodec: "libvpx-vp9",
        AudioCodec: "libopus",
        Resolution: new Resolution(1920, 1080),
        FrameRate: 30,
        VideoBitrate: 6000,
        AudioBitrate: 128,
        PixelFormat: "yuv420p",
        ColorSpace: "bt709",
        AspectRatio: AspectRatio.SixteenByNine,
        Quality: QualityLevel.High
    );

    /// <summary>
    /// ProRes 422 HQ preset (1920x1080, ProRes for professional editing)
    /// </summary>
    public static ExportPreset ProRes422HQ => new(
        Name: "ProRes 422 HQ",
        Description: "Professional quality for editing and mastering",
        Platform: Platform.Generic,
        Container: "mov",
        VideoCodec: "prores_ks",
        AudioCodec: "pcm_s16le",
        Resolution: new Resolution(1920, 1080),
        FrameRate: 30,
        VideoBitrate: 120000, // ProRes uses very high bitrate
        AudioBitrate: 1536, // Uncompressed audio
        PixelFormat: "yuv422p10le",
        ColorSpace: "bt709",
        AspectRatio: AspectRatio.SixteenByNine,
        Quality: QualityLevel.Maximum
    );

    /// <summary>
    /// Podcast audio-only preset (MP3, 128kbps)
    /// </summary>
    public static ExportPreset PodcastAudio => new(
        Name: "Podcast Audio",
        Description: "Audio-only export for podcasts",
        Platform: Platform.Generic,
        Container: "mp3",
        VideoCodec: "none", // Audio only
        AudioCodec: "libmp3lame",
        Resolution: new Resolution(0, 0), // No video
        FrameRate: 0,
        VideoBitrate: 0,
        AudioBitrate: 128,
        PixelFormat: "none",
        ColorSpace: "none",
        AspectRatio: AspectRatio.SixteenByNine,
        Quality: QualityLevel.Good
    );

    /// <summary>
    /// Gets all available presets
    /// </summary>
    public static IReadOnlyList<ExportPreset> GetAllPresets()
    {
        return new[]
        {
            YouTube1080p,
            YouTube4K,
            InstagramFeed,
            InstagramStory,
            TikTok,
            Facebook,
            Twitter,
            LinkedIn,
            EmailWeb,
            DraftPreview,
            MasterArchive,
            WebMVP9,
            ProRes422HQ,
            PodcastAudio
        };
    }

    /// <summary>
    /// Gets presets grouped by platform
    /// </summary>
    public static Dictionary<Platform, List<ExportPreset>> GetPresetsByPlatform()
    {
        var result = new Dictionary<Platform, List<ExportPreset>>();
        
        foreach (var preset in GetAllPresets())
        {
            if (!result.ContainsKey(preset.Platform))
            {
                result[preset.Platform] = new List<ExportPreset>();
            }
            result[preset.Platform].Add(preset);
        }
        
        return result;
    }

    /// <summary>
    /// Gets a preset by name
    /// </summary>
    public static ExportPreset? GetPresetByName(string name)
    {
        var normalized = name.ToLowerInvariant().Replace(" ", "").Replace("-", "");
        
        return normalized switch
        {
            "youtube1080p" or "youtube1080" => YouTube1080p,
            "youtube4k" or "youtube2160p" => YouTube4K,
            "instagramfeed" or "instagram11" => InstagramFeed,
            "instagramstory" or "instagramstories" or "igstory" => InstagramStory,
            "tiktok" => TikTok,
            "facebook" or "fb" => Facebook,
            "twitter" => Twitter,
            "linkedin" => LinkedIn,
            "emailweb" or "email" or "web" => EmailWeb,
            "draftpreview" or "draft" or "preview" => DraftPreview,
            "masterarchive" or "master" or "archive" => MasterArchive,
            "webmvp9" or "webm" or "vp9" => WebMVP9,
            "prores422hq" or "prores" or "prores422" => ProRes422HQ,
            "podcastaudio" or "podcast" or "audioonly" => PodcastAudio,
            _ => null
        };
    }

    /// <summary>
    /// Estimates file size in MB based on preset and duration
    /// </summary>
    public static double EstimateFileSizeMB(ExportPreset preset, TimeSpan duration)
    {
        var durationSeconds = duration.TotalSeconds;
        var videoBits = preset.VideoBitrate * 1000 * durationSeconds;
        var audioBits = preset.AudioBitrate * 1000 * durationSeconds;
        var totalBits = videoBits + audioBits;
        var megabytes = totalBits / 8 / 1024 / 1024;
        
        // Add 10% overhead for container and metadata
        return megabytes * 1.1;
    }
}
