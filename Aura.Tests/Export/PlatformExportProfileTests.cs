using System;
using System.Collections.Generic;
using Aura.Core.Models;
using Aura.Core.Models.Export;
using Xunit;

namespace Aura.Tests.Export;

public class PlatformExportProfileTests
{
    [Fact]
    public void YouTubeProfile_HasCorrectSpecifications()
    {
        // Arrange & Act
        var profile = new YouTubeExportProfile();

        // Assert
        Assert.Equal("YouTube", profile.PlatformName);
        Assert.Contains(new Resolution(1920, 1080), profile.SupportedResolutions);
        Assert.Contains(new Resolution(3840, 2160), profile.SupportedResolutions);
        Assert.Contains(AspectRatio.SixteenByNine, profile.SupportedAspectRatios);
        Assert.Equal(8000, profile.RecommendedVideoBitrate);
        Assert.Equal(192, profile.RecommendedAudioBitrate);
        Assert.Contains("mp4", profile.SupportedFormats);
        Assert.Null(profile.MaxDuration);
    }

    [Fact]
    public void TikTokProfile_HasCorrectSpecifications()
    {
        // Arrange & Act
        var profile = new TikTokExportProfile();

        // Assert
        Assert.Equal("TikTok", profile.PlatformName);
        Assert.Contains(new Resolution(1080, 1920), profile.SupportedResolutions);
        Assert.Contains(AspectRatio.NineBySixteen, profile.SupportedAspectRatios);
        Assert.Equal(5000, profile.RecommendedVideoBitrate);
        Assert.Equal(600, profile.MaxDuration); // 10 minutes
        Assert.Equal(287, profile.MaxFileSize); // 287 MB
    }

    [Fact]
    public void InstagramProfile_HasCorrectSpecifications()
    {
        // Arrange & Act
        var profile = new InstagramExportProfile();

        // Assert
        Assert.Equal("Instagram", profile.PlatformName);
        Assert.Contains(new Resolution(1080, 1080), profile.SupportedResolutions);
        Assert.Contains(new Resolution(1080, 1920), profile.SupportedResolutions);
        Assert.Contains(AspectRatio.OneByOne, profile.SupportedAspectRatios);
        Assert.Contains(AspectRatio.NineBySixteen, profile.SupportedAspectRatios);
        Assert.Equal(90, profile.MaxDuration); // 90 seconds for Reels
    }

    [Fact]
    public void PlatformExportProfileFactory_ReturnsCorrectProfile()
    {
        // Arrange & Act
        var youtubeProfile = PlatformExportProfileFactory.GetProfile(Aura.Core.Models.Export.Platform.YouTube);
        var tiktokProfile = PlatformExportProfileFactory.GetProfile(Aura.Core.Models.Export.Platform.TikTok);
        var instagramProfile = PlatformExportProfileFactory.GetProfile(Aura.Core.Models.Export.Platform.Instagram);

        // Assert
        Assert.IsType<YouTubeExportProfile>(youtubeProfile);
        Assert.IsType<TikTokExportProfile>(tiktokProfile);
        Assert.IsType<InstagramExportProfile>(instagramProfile);
    }

    [Fact]
    public void PlatformExportProfileFactory_GetAllProfiles_Returns6Profiles()
    {
        // Arrange & Act
        var profiles = PlatformExportProfileFactory.GetAllProfiles();

        // Assert
        Assert.Equal(6, profiles.Count);
        Assert.Contains(profiles, p => p.PlatformName == "YouTube");
        Assert.Contains(profiles, p => p.PlatformName == "TikTok");
        Assert.Contains(profiles, p => p.PlatformName == "Instagram");
        Assert.Contains(profiles, p => p.PlatformName == "LinkedIn");
        Assert.Contains(profiles, p => p.PlatformName == "Twitter");
        Assert.Contains(profiles, p => p.PlatformName == "Facebook");
    }

    [Fact]
    public void ValidateExportForPlatform_ValidSettings_ReturnsValid()
    {
        // Arrange
        var profile = new YouTubeExportProfile();
        var preset = ExportPresets.YouTube1080p;

        // Act
        var (isValid, errors) = PlatformExportProfileFactory.ValidateExportForPlatform(preset, profile);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateExportForPlatform_InvalidResolution_ReturnsError()
    {
        // Arrange
        var profile = new TikTokExportProfile();
        var preset = ExportPresets.YouTube1080p; // 16:9 YouTube preset for TikTok (9:16)

        // Act
        var (isValid, errors) = PlatformExportProfileFactory.ValidateExportForPlatform(preset, profile);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Resolution"));
    }

    [Fact]
    public void ValidateExportForPlatform_BitrateOutOfRange_ReturnsError()
    {
        // Arrange
        var profile = new TikTokExportProfile();
        var invalidPreset = new ExportPreset(
            Name: "Invalid High Bitrate",
            Description: "Test preset with bitrate too high",
            Platform: Aura.Core.Models.Export.Platform.TikTok,
            Container: "mp4",
            VideoCodec: "libx264",
            AudioCodec: "aac",
            Resolution: new Resolution(1080, 1920),
            FrameRate: 30,
            VideoBitrate: 15000, // Too high for TikTok (max 10000)
            AudioBitrate: 192,
            PixelFormat: "yuv420p",
            ColorSpace: "bt709",
            AspectRatio: AspectRatio.NineBySixteen,
            Quality: QualityLevel.High
        );

        // Act
        var (isValid, errors) = PlatformExportProfileFactory.ValidateExportForPlatform(invalidPreset, profile);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("bitrate") && e.Contains("exceeds"));
    }
}
