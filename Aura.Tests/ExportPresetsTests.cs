using Aura.Core.Models;
using Aura.Core.Models.Export;
using Xunit;

namespace Aura.Tests;

public class ExportPresetsTests
{
    [Fact]
    public void GetAllPresets_ReturnsExpectedCount()
    {
        // Act
        var presets = ExportPresets.GetAllPresets();

        // Assert
        Assert.NotNull(presets);
        Assert.Equal(14, presets.Count); // We have 14 presets
    }

    [Fact]
    public void YouTube1080p_HasCorrectProperties()
    {
        // Act
        var preset = ExportPresets.YouTube1080p;

        // Assert
        Assert.Equal("YouTube 1080p", preset.Name);
        Assert.Equal(Platform.YouTube, preset.Platform);
        Assert.Equal(1920, preset.Resolution.Width);
        Assert.Equal(1080, preset.Resolution.Height);
        Assert.Equal("mp4", preset.Container);
        Assert.Equal("libx264", preset.VideoCodec);
        Assert.Equal(8000, preset.VideoBitrate);
        Assert.Equal(AspectRatio.SixteenByNine, preset.AspectRatio);
    }

    [Fact]
    public void InstagramFeed_HasSquareAspectRatio()
    {
        // Act
        var preset = ExportPresets.InstagramFeed;

        // Assert
        Assert.Equal(1080, preset.Resolution.Width);
        Assert.Equal(1080, preset.Resolution.Height);
        Assert.Equal(AspectRatio.OneByOne, preset.AspectRatio);
    }

    [Fact]
    public void TikTok_HasVerticalAspectRatio()
    {
        // Act
        var preset = ExportPresets.TikTok;

        // Assert
        Assert.Equal(1080, preset.Resolution.Width);
        Assert.Equal(1920, preset.Resolution.Height);
        Assert.Equal(AspectRatio.NineBySixteen, preset.AspectRatio);
    }

    [Fact]
    public void TikTok_HasMaxDurationLimit()
    {
        // Act
        var preset = ExportPresets.TikTok;

        // Assert
        Assert.NotNull(preset.MaxDuration);
        Assert.Equal(60, preset.MaxDuration.Value);
    }

    [Fact]
    public void GetPresetByName_ReturnsCorrectPreset()
    {
        // Act
        var preset = ExportPresets.GetPresetByName("YouTube 1080p");

        // Assert
        Assert.NotNull(preset);
        Assert.Equal("YouTube 1080p", preset.Name);
    }

    [Fact]
    public void GetPresetByName_IsCaseInsensitive()
    {
        // Act
        var preset = ExportPresets.GetPresetByName("youtube1080p");

        // Assert
        Assert.NotNull(preset);
        Assert.Equal("YouTube 1080p", preset.Name);
    }

    [Fact]
    public void GetPresetByName_ReturnsNullForInvalidName()
    {
        // Act
        var preset = ExportPresets.GetPresetByName("InvalidPreset");

        // Assert
        Assert.Null(preset);
    }

    [Fact]
    public void GetPresetsByPlatform_GroupsCorrectly()
    {
        // Act
        var grouped = ExportPresets.GetPresetsByPlatform();

        // Assert
        Assert.Contains(Platform.YouTube, grouped.Keys);
        Assert.Contains(Platform.Instagram, grouped.Keys);
        Assert.Contains(Platform.TikTok, grouped.Keys);
        
        Assert.True(grouped[Platform.YouTube].Count >= 2); // At least 1080p and 4K
        Assert.True(grouped[Platform.Instagram].Count >= 2); // Feed and Story
    }

    [Fact]
    public void EstimateFileSizeMB_ReturnsReasonableValue()
    {
        // Arrange
        var preset = ExportPresets.YouTube1080p;
        var duration = TimeSpan.FromMinutes(3); // 3 minute video

        // Act
        var estimatedSize = ExportPresets.EstimateFileSizeMB(preset, duration);

        // Assert
        Assert.True(estimatedSize > 0);
        Assert.True(estimatedSize < 1000); // Should be less than 1GB for 3 min 1080p
        
        // For 8 Mbps video + 192 kbps audio for 3 minutes
        // Expected: ~180 MB + overhead
        Assert.InRange(estimatedSize, 150, 250);
    }

    [Fact]
    public void EstimateFileSizeMB_ScalesWithDuration()
    {
        // Arrange
        var preset = ExportPresets.YouTube1080p;
        var shortDuration = TimeSpan.FromMinutes(1);
        var longDuration = TimeSpan.FromMinutes(5);

        // Act
        var shortSize = ExportPresets.EstimateFileSizeMB(preset, shortDuration);
        var longSize = ExportPresets.EstimateFileSizeMB(preset, longDuration);

        // Assert
        Assert.True(longSize > shortSize);
        Assert.True(Math.Abs(longSize / shortSize - 5) < 0.5); // Should be roughly 5x
    }

    [Theory]
    [InlineData("YouTube 4K", 3840, 2160)]
    [InlineData("Instagram Story", 1080, 1920)]
    [InlineData("Facebook", 1280, 720)]
    [InlineData("EmailWeb", 854, 480)]
    public void Preset_HasCorrectResolution(string presetName, int expectedWidth, int expectedHeight)
    {
        // Act
        var preset = ExportPresets.GetPresetByName(presetName);

        // Assert
        Assert.NotNull(preset);
        Assert.Equal(expectedWidth, preset.Resolution.Width);
        Assert.Equal(expectedHeight, preset.Resolution.Height);
    }

    [Theory]
    [InlineData("YouTube 1080p", QualityLevel.High)]
    [InlineData("Draft Preview", QualityLevel.Draft)]
    [InlineData("Master Archive", QualityLevel.Maximum)]
    public void Preset_HasCorrectQuality(string presetName, QualityLevel expectedQuality)
    {
        // Act
        var preset = ExportPresets.GetPresetByName(presetName);

        // Assert
        Assert.NotNull(preset);
        Assert.Equal(expectedQuality, preset.Quality);
    }
}
