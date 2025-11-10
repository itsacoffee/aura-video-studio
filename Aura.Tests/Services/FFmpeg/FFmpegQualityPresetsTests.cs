using System.Linq;
using Aura.Core.Models;
using Aura.Core.Services.FFmpeg;
using Xunit;

namespace Aura.Tests.Services.FFmpeg;

public class FFmpegQualityPresetsTests
{
    [Fact]
    public void GetPreset_Draft_ShouldReturnDraftConfiguration()
    {
        // Act
        var preset = FFmpegQualityPresets.GetPreset(QualityLevel.Draft);

        // Assert
        Assert.Equal("Draft", preset.Name);
        Assert.Equal("ultrafast", preset.Preset);
        Assert.Equal(28, preset.CRF);
        Assert.Equal(1500, preset.VideoBitrate);
        Assert.Equal(96, preset.AudioBitrate);
        Assert.False(preset.TwoPass);
        Assert.Equal(1280, preset.MaxDimension);
        Assert.Equal(30, preset.TargetFps);
    }

    [Fact]
    public void GetPreset_Standard_ShouldReturnStandardConfiguration()
    {
        // Act
        var preset = FFmpegQualityPresets.GetPreset(QualityLevel.Good);

        // Assert
        Assert.Equal("Standard", preset.Name);
        Assert.Equal("medium", preset.Preset);
        Assert.Equal(23, preset.CRF);
        Assert.Equal(5000, preset.VideoBitrate);
        Assert.Equal(192, preset.AudioBitrate);
        Assert.False(preset.TwoPass);
        Assert.Equal(1920, preset.MaxDimension);
    }

    [Fact]
    public void GetPreset_Premium_ShouldReturnPremiumConfiguration()
    {
        // Act
        var preset = FFmpegQualityPresets.GetPreset(QualityLevel.High);

        // Assert
        Assert.Equal("Premium", preset.Name);
        Assert.Equal("slow", preset.Preset);
        Assert.Equal(18, preset.CRF);
        Assert.Equal(8000, preset.VideoBitrate);
        Assert.True(preset.TwoPass);
        Assert.Equal(3840, preset.MaxDimension);
        Assert.Equal(60, preset.TargetFps);
    }

    [Fact]
    public void GetPreset_Maximum_ShouldReturnMaximumConfiguration()
    {
        // Act
        var preset = FFmpegQualityPresets.GetPreset(QualityLevel.Maximum);

        // Assert
        Assert.Equal("Maximum", preset.Name);
        Assert.Equal("veryslow", preset.Preset);
        Assert.Equal(15, preset.CRF);
        Assert.Equal(12000, preset.VideoBitrate);
        Assert.True(preset.TwoPass);
        Assert.NotEmpty(preset.EncoderOptions);
        Assert.True(preset.EncoderOptions.ContainsKey("me_method"));
    }

    [Theory]
    [InlineData(QualityLevel.Draft, 28)]
    [InlineData(QualityLevel.Good, 23)]
    [InlineData(QualityLevel.High, 18)]
    [InlineData(QualityLevel.Maximum, 15)]
    public void GetPreset_AllLevels_ShouldHaveAppropriate CRF(QualityLevel level, int expectedCrf)
    {
        // Act
        var preset = FFmpegQualityPresets.GetPreset(level);

        // Assert
        Assert.Equal(expectedCrf, preset.CRF);
    }

    [Fact]
    public void ApplyPreset_ShouldConfigureCommandBuilder()
    {
        // Arrange
        var builder = new FFmpegCommandBuilder();
        var preset = FFmpegQualityPresets.GetPreset(QualityLevel.Good);

        // Act
        builder
            .AddInput("input.mp4")
            .SetOutput("output.mp4")
            .ApplyPreset(preset);
        
        var command = builder.Build();

        // Assert
        Assert.Contains("-c:v libx264", command);
        Assert.Contains("-preset medium", command);
        Assert.Contains("-crf 23", command);
        Assert.Contains("-b:v 5000k", command);
        Assert.Contains("-b:a 192k", command);
    }

    [Fact]
    public void AllPresets_ShouldHaveRequiredProperties()
    {
        // Arrange
        var qualityLevels = new[] 
        { 
            QualityLevel.Draft, 
            QualityLevel.Good, 
            QualityLevel.High, 
            QualityLevel.Maximum 
        };

        foreach (var level in qualityLevels)
        {
            // Act
            var preset = FFmpegQualityPresets.GetPreset(level);

            // Assert
            Assert.NotNull(preset.Name);
            Assert.NotEmpty(preset.Description);
            Assert.NotNull(preset.Codec);
            Assert.NotNull(preset.Preset);
            Assert.True(preset.CRF >= 0 && preset.CRF <= 51);
            Assert.True(preset.VideoBitrate > 0);
            Assert.True(preset.AudioBitrate > 0);
            Assert.NotNull(preset.PixelFormat);
            Assert.NotNull(preset.Profile);
            Assert.NotNull(preset.Level);
            Assert.NotNull(preset.EncoderOptions);
        }
    }
}
