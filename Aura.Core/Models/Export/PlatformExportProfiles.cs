using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Export;

/// <summary>
/// Base interface for platform-specific export profiles
/// </summary>
public interface IPlatformExportProfile
{
    /// <summary>Platform name</summary>
    string PlatformName { get; }
    
    /// <summary>Supported resolutions for this platform</summary>
    IReadOnlyList<Resolution> SupportedResolutions { get; }
    
    /// <summary>Supported aspect ratios for this platform</summary>
    IReadOnlyList<AspectRatio> SupportedAspectRatios { get; }
    
    /// <summary>Minimum video bitrate in kbps</summary>
    int MinVideoBitrate { get; }
    
    /// <summary>Maximum video bitrate in kbps</summary>
    int MaxVideoBitrate { get; }
    
    /// <summary>Recommended video bitrate in kbps</summary>
    int RecommendedVideoBitrate { get; }
    
    /// <summary>Supported video formats</summary>
    IReadOnlyList<string> SupportedFormats { get; }
    
    /// <summary>Maximum duration in seconds (null if unlimited)</summary>
    int? MaxDuration { get; }
    
    /// <summary>Maximum file size in MB (null if unlimited)</summary>
    long? MaxFileSize { get; }
    
    /// <summary>Supported frame rates</summary>
    IReadOnlyList<int> SupportedFrameRates { get; }
    
    /// <summary>Recommended frame rate</summary>
    int RecommendedFrameRate { get; }
    
    /// <summary>Supported audio codecs</summary>
    IReadOnlyList<string> SupportedAudioCodecs { get; }
    
    /// <summary>Recommended audio bitrate in kbps</summary>
    int RecommendedAudioBitrate { get; }
}

/// <summary>
/// YouTube export profile with platform-specific requirements
/// </summary>
public class YouTubeExportProfile : IPlatformExportProfile
{
    public string PlatformName => "YouTube";
    
    public IReadOnlyList<Resolution> SupportedResolutions => new[]
    {
        new Resolution(426, 240),   // 240p
        new Resolution(640, 360),   // 360p
        new Resolution(854, 480),   // 480p
        new Resolution(1280, 720),  // 720p HD
        new Resolution(1920, 1080), // 1080p Full HD
        new Resolution(2560, 1440), // 1440p 2K
        new Resolution(3840, 2160), // 2160p 4K
        new Resolution(7680, 4320)  // 4320p 8K
    };
    
    public IReadOnlyList<AspectRatio> SupportedAspectRatios => new[]
    {
        AspectRatio.SixteenByNine,
        AspectRatio.FourByFive,
        AspectRatio.OneByOne,
        AspectRatio.NineBySixteen
    };
    
    public int MinVideoBitrate => 1000;
    public int MaxVideoBitrate => 85000; // 85 Mbps for 4K 60fps
    public int RecommendedVideoBitrate => 8000; // 8 Mbps for 1080p
    
    public IReadOnlyList<string> SupportedFormats => new[] { "mp4", "mov", "avi", "wmv", "flv", "3gpp", "webm", "mpegps", "prores" };
    
    public int? MaxDuration => null; // Unlimited for standard users
    public long? MaxFileSize => 256L * 1024; // 256 GB
    
    public IReadOnlyList<int> SupportedFrameRates => new[] { 24, 25, 30, 48, 50, 60 };
    public int RecommendedFrameRate => 30;
    
    public IReadOnlyList<string> SupportedAudioCodecs => new[] { "aac", "mp3", "opus", "vorbis" };
    public int RecommendedAudioBitrate => 192;
}

/// <summary>
/// TikTok export profile with platform-specific requirements
/// </summary>
public class TikTokExportProfile : IPlatformExportProfile
{
    public string PlatformName => "TikTok";
    
    public IReadOnlyList<Resolution> SupportedResolutions => new[]
    {
        new Resolution(720, 1280),  // 720p vertical
        new Resolution(1080, 1920)  // 1080p vertical (recommended)
    };
    
    public IReadOnlyList<AspectRatio> SupportedAspectRatios => new[]
    {
        AspectRatio.NineBySixteen, // Primary vertical format
        AspectRatio.OneByOne,      // Square also supported
        AspectRatio.SixteenByNine  // Horizontal supported but not recommended
    };
    
    public int MinVideoBitrate => 516;
    public int MaxVideoBitrate => 10000;
    public int RecommendedVideoBitrate => 5000;
    
    public IReadOnlyList<string> SupportedFormats => new[] { "mp4", "mov", "webm" };
    
    public int? MaxDuration => 600; // 10 minutes (varies by account type)
    public long? MaxFileSize => 287; // 287 MB
    
    public IReadOnlyList<int> SupportedFrameRates => new[] { 23, 24, 25, 29, 30, 50, 60 };
    public int RecommendedFrameRate => 30;
    
    public IReadOnlyList<string> SupportedAudioCodecs => new[] { "aac", "mp3" };
    public int RecommendedAudioBitrate => 192;
}

/// <summary>
/// Instagram export profile with platform-specific requirements
/// </summary>
public class InstagramExportProfile : IPlatformExportProfile
{
    public string PlatformName => "Instagram";
    
    public IReadOnlyList<Resolution> SupportedResolutions => new[]
    {
        new Resolution(1080, 1080), // Square (Feed)
        new Resolution(1080, 1350), // Portrait 4:5 (Feed)
        new Resolution(1080, 1920), // Vertical 9:16 (Stories/Reels)
        new Resolution(1920, 1080)  // Landscape 16:9 (Feed/IGTV)
    };
    
    public IReadOnlyList<AspectRatio> SupportedAspectRatios => new[]
    {
        AspectRatio.OneByOne,      // Square feed posts
        AspectRatio.FourByFive,    // Portrait feed posts
        AspectRatio.NineBySixteen, // Stories and Reels
        AspectRatio.SixteenByNine  // IGTV
    };
    
    public int MinVideoBitrate => 1000;
    public int MaxVideoBitrate => 10000;
    public int RecommendedVideoBitrate => 5000;
    
    public IReadOnlyList<string> SupportedFormats => new[] { "mp4", "mov" };
    
    public int? MaxDuration => 90; // 90 seconds for Reels (varies by content type)
    public long? MaxFileSize => 4L * 1024; // 4 GB
    
    public IReadOnlyList<int> SupportedFrameRates => new[] { 23, 24, 25, 29, 30, 50, 60 };
    public int RecommendedFrameRate => 30;
    
    public IReadOnlyList<string> SupportedAudioCodecs => new[] { "aac" };
    public int RecommendedAudioBitrate => 192;
}

/// <summary>
/// LinkedIn export profile with platform-specific requirements
/// </summary>
public class LinkedInExportProfile : IPlatformExportProfile
{
    public string PlatformName => "LinkedIn";
    
    public IReadOnlyList<Resolution> SupportedResolutions => new[]
    {
        new Resolution(256, 144),   // Minimum
        new Resolution(640, 360),
        new Resolution(1280, 720),
        new Resolution(1920, 1080), // Recommended
        new Resolution(3840, 2160)  // Maximum 4K
    };
    
    public IReadOnlyList<AspectRatio> SupportedAspectRatios => new[]
    {
        AspectRatio.SixteenByNine,
        AspectRatio.OneByOne
    };
    
    public int MinVideoBitrate => 1000;
    public int MaxVideoBitrate => 20000;
    public int RecommendedVideoBitrate => 5000;
    
    public IReadOnlyList<string> SupportedFormats => new[] { "mp4", "mov", "avi" };
    
    public int? MaxDuration => 600; // 10 minutes
    public long? MaxFileSize => 5L * 1024; // 5 GB
    
    public IReadOnlyList<int> SupportedFrameRates => new[] { 24, 25, 30, 60 };
    public int RecommendedFrameRate => 30;
    
    public IReadOnlyList<string> SupportedAudioCodecs => new[] { "aac", "mp3" };
    public int RecommendedAudioBitrate => 192;
}

/// <summary>
/// Twitter/X export profile with platform-specific requirements
/// </summary>
public class TwitterExportProfile : IPlatformExportProfile
{
    public string PlatformName => "Twitter";
    
    public IReadOnlyList<Resolution> SupportedResolutions => new[]
    {
        new Resolution(32, 32),     // Minimum
        new Resolution(1280, 720),  // 720p
        new Resolution(1920, 1080)  // Maximum 1080p
    };
    
    public IReadOnlyList<AspectRatio> SupportedAspectRatios => new[]
    {
        AspectRatio.SixteenByNine,
        AspectRatio.OneByOne,
        AspectRatio.NineBySixteen
    };
    
    public int MinVideoBitrate => 500;
    public int MaxVideoBitrate => 15000;
    public int RecommendedVideoBitrate => 5000;
    
    public IReadOnlyList<string> SupportedFormats => new[] { "mp4", "mov" };
    
    public int? MaxDuration => 140; // 140 seconds (2:20)
    public long? MaxFileSize => 512; // 512 MB
    
    public IReadOnlyList<int> SupportedFrameRates => new[] { 24, 25, 29, 30, 60 };
    public int RecommendedFrameRate => 30;
    
    public IReadOnlyList<string> SupportedAudioCodecs => new[] { "aac", "mp3" };
    public int RecommendedAudioBitrate => 192;
}

/// <summary>
/// Facebook export profile with platform-specific requirements
/// </summary>
public class FacebookExportProfile : IPlatformExportProfile
{
    public string PlatformName => "Facebook";
    
    public IReadOnlyList<Resolution> SupportedResolutions => new[]
    {
        new Resolution(1280, 720),  // 720p
        new Resolution(1920, 1080), // 1080p (recommended)
        new Resolution(2048, 1080)  // 2K
    };
    
    public IReadOnlyList<AspectRatio> SupportedAspectRatios => new[]
    {
        AspectRatio.SixteenByNine,
        AspectRatio.OneByOne,
        AspectRatio.NineBySixteen,
        AspectRatio.FourByFive
    };
    
    public int MinVideoBitrate => 1000;
    public int MaxVideoBitrate => 8000;
    public int RecommendedVideoBitrate => 4000;
    
    public IReadOnlyList<string> SupportedFormats => new[] { "mp4", "mov" };
    
    public int? MaxDuration => 14400; // 240 minutes (4 hours)
    public long? MaxFileSize => 10L * 1024; // 10 GB
    
    public IReadOnlyList<int> SupportedFrameRates => new[] { 23, 24, 25, 29, 30, 50, 60 };
    public int RecommendedFrameRate => 30;
    
    public IReadOnlyList<string> SupportedAudioCodecs => new[] { "aac" };
    public int RecommendedAudioBitrate => 192;
}

/// <summary>
/// Factory for creating platform export profiles
/// </summary>
public static class PlatformExportProfileFactory
{
    /// <summary>
    /// Gets the export profile for a specific platform
    /// </summary>
    public static IPlatformExportProfile GetProfile(Platform platform)
    {
        return platform switch
        {
            Platform.YouTube => new YouTubeExportProfile(),
            Platform.TikTok => new TikTokExportProfile(),
            Platform.Instagram => new InstagramExportProfile(),
            Platform.LinkedIn => new LinkedInExportProfile(),
            Platform.Twitter => new TwitterExportProfile(),
            Platform.Facebook => new FacebookExportProfile(),
            Platform.Generic => throw new ArgumentException("Generic platform does not have a specific profile"),
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, "Unknown platform")
        };
    }
    
    /// <summary>
    /// Gets all available platform profiles
    /// </summary>
    public static IReadOnlyList<IPlatformExportProfile> GetAllProfiles()
    {
        return new IPlatformExportProfile[]
        {
            new YouTubeExportProfile(),
            new TikTokExportProfile(),
            new InstagramExportProfile(),
            new LinkedInExportProfile(),
            new TwitterExportProfile(),
            new FacebookExportProfile()
        };
    }
    
    /// <summary>
    /// Validates if the given export settings are compatible with the platform
    /// </summary>
    public static (bool IsValid, List<string> Errors) ValidateExportForPlatform(
        ExportPreset preset,
        IPlatformExportProfile profile)
    {
        var errors = new List<string>();
        
        // Check resolution
        if (!profile.SupportedResolutions.Contains(preset.Resolution))
        {
            errors.Add($"Resolution {preset.Resolution.Width}x{preset.Resolution.Height} is not supported by {profile.PlatformName}");
        }
        
        // Check aspect ratio
        if (!profile.SupportedAspectRatios.Contains(preset.AspectRatio))
        {
            errors.Add($"Aspect ratio {preset.AspectRatio} is not supported by {profile.PlatformName}");
        }
        
        // Check bitrate
        if (preset.VideoBitrate < profile.MinVideoBitrate)
        {
            errors.Add($"Video bitrate {preset.VideoBitrate} kbps is below minimum {profile.MinVideoBitrate} kbps for {profile.PlatformName}");
        }
        if (preset.VideoBitrate > profile.MaxVideoBitrate)
        {
            errors.Add($"Video bitrate {preset.VideoBitrate} kbps exceeds maximum {profile.MaxVideoBitrate} kbps for {profile.PlatformName}");
        }
        
        // Check format
        if (!profile.SupportedFormats.Contains(preset.Container))
        {
            errors.Add($"Container format '{preset.Container}' is not supported by {profile.PlatformName}");
        }
        
        // Check frame rate
        if (!profile.SupportedFrameRates.Contains(preset.FrameRate))
        {
            errors.Add($"Frame rate {preset.FrameRate} fps is not supported by {profile.PlatformName}");
        }
        
        return (errors.Count == 0, errors);
    }
}
