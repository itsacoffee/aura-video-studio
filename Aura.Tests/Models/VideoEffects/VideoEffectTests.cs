using System;
using Aura.Core.Models.VideoEffects;
using Xunit;

namespace Aura.Tests.Models.VideoEffects;

public class VideoEffectTests
{
    [Fact]
    public void ColorCorrectionEffect_ToFFmpegFilter_GeneratesCorrectFilter()
    {
        // Arrange
        var effect = new ColorCorrectionEffect
        {
            Name = "Color Grade",
            Brightness = 0.2,
            Contrast = 0.3,
            Saturation = 0.1,
            Duration = 1.0
        };

        // Act
        var filter = effect.ToFFmpegFilter();

        // Assert
        Assert.Contains("eq=", filter);
        Assert.Contains("brightness", filter);
        Assert.Contains("contrast", filter);
    }

    [Fact]
    public void BlurEffect_ToFFmpegFilter_GeneratesCorrectFilter()
    {
        // Arrange
        var effect = new BlurEffect
        {
            Name = "Blur",
            Type = BlurEffect.BlurType.Gaussian,
            Strength = 5.0,
            Duration = 1.0
        };

        // Act
        var filter = effect.ToFFmpegFilter();

        // Assert
        Assert.Contains("gblur", filter);
        Assert.Contains("sigma", filter);
    }

    [Fact]
    public void VintageEffect_Sepia_GeneratesCorrectFilter()
    {
        // Arrange
        var effect = new VintageEffect
        {
            Name = "Vintage",
            Style = VintageEffect.VintageStyle.Sepia,
            Grain = 0.3,
            Vignette = 0.5,
            Duration = 1.0
        };

        // Act
        var filter = effect.ToFFmpegFilter();

        // Assert
        Assert.Contains("colorchannelmixer", filter);
    }

    [Fact]
    public void TransitionEffect_ToFFmpegFilter_GeneratesCorrectFilter()
    {
        // Arrange
        var effect = new TransitionEffect
        {
            Name = "Fade Transition",
            TransitionType = Core.Services.FFmpeg.Filters.TransitionBuilder.TransitionType.Fade,
            Duration = 1.0,
            Offset = 5.0
        };

        // Act
        var filter = effect.ToFFmpegFilter();

        // Assert
        Assert.Contains("xfade", filter);
        Assert.Contains("transition=fade", filter);
    }

    [Fact]
    public void TypewriterEffect_ToFFmpegFilter_GeneratesDrawtextFilter()
    {
        // Arrange
        var effect = new TypewriterEffect
        {
            Name = "Typewriter",
            Text = "Hello World",
            FontSize = 48,
            FontColor = "white",
            Speed = 10.0,
            Duration = 5.0,
            StartTime = 0
        };

        // Act
        var filter = effect.ToFFmpegFilter();

        // Assert
        Assert.Contains("drawtext", filter);
        Assert.Contains("text=", filter);
    }

    [Fact]
    public void FadeTextEffect_ToFFmpegFilter_IncludesAlphaExpression()
    {
        // Arrange
        var effect = new FadeTextEffect
        {
            Name = "Fade Text",
            Text = "Test",
            FontSize = 48,
            FadeInDuration = 1.0,
            FadeOutDuration = 1.0,
            Duration = 5.0,
            StartTime = 0
        };

        // Act
        var filter = effect.ToFFmpegFilter();

        // Assert
        Assert.Contains("drawtext", filter);
        Assert.Contains("alpha=", filter);
    }

    [Fact]
    public void SlidingTextEffect_ToFFmpegFilter_IncludesPositionExpression()
    {
        // Arrange
        var effect = new SlidingTextEffect
        {
            Name = "Sliding Text",
            Text = "Test",
            Direction = SlidingTextEffect.SlideDirection.Left,
            FontSize = 48,
            Duration = 5.0,
            StartTime = 0
        };

        // Act
        var filter = effect.ToFFmpegFilter();

        // Assert
        Assert.Contains("drawtext", filter);
        Assert.Contains("x=", filter);
    }

    [Fact]
    public void VideoEffect_Validate_InvalidDuration_ReturnsFalse()
    {
        // Arrange
        var effect = new ColorCorrectionEffect
        {
            Name = "Test",
            Duration = -1.0,
            StartTime = 0
        };

        // Act
        var isValid = effect.Validate(out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.Contains("Duration", errorMessage);
    }

    [Fact]
    public void VideoEffect_Validate_InvalidIntensity_ReturnsFalse()
    {
        // Arrange
        var effect = new ColorCorrectionEffect
        {
            Name = "Test",
            Duration = 1.0,
            StartTime = 0,
            Intensity = 1.5
        };

        // Act
        var isValid = effect.Validate(out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.Contains("Intensity", errorMessage);
    }

    [Fact]
    public void VideoEffect_Validate_ValidEffect_ReturnsTrue()
    {
        // Arrange
        var effect = new ColorCorrectionEffect
        {
            Name = "Test",
            Duration = 5.0,
            StartTime = 0,
            Intensity = 0.8
        };

        // Act
        var isValid = effect.Validate(out var errorMessage);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void EffectStack_ToFFmpegFilter_CombinesEnabledEffects()
    {
        // Arrange
        var stack = new EffectStack
        {
            Name = "Test Stack",
            Effects = new System.Collections.Generic.List<VideoEffect>
            {
                new ColorCorrectionEffect
                {
                    Name = "Color",
                    Brightness = 0.2,
                    Duration = 1.0,
                    Enabled = true
                },
                new BlurEffect
                {
                    Name = "Blur",
                    Strength = 5.0,
                    Duration = 1.0,
                    Enabled = false
                }
            }
        };

        // Act
        var filter = stack.ToFFmpegFilter();

        // Assert
        Assert.Contains("eq=", filter);
        Assert.DoesNotContain("blur", filter.ToLowerInvariant());
    }
}
