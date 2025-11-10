using System;
using Aura.Core.Services.FFmpeg.Filters;
using Xunit;

namespace Aura.Tests.Services.FFmpeg.Filters;

public class EffectBuilderTests
{
    [Fact]
    public void BuildKenBurns_WithDefaultParameters_ShouldGenerateValidFilter()
    {
        // Arrange
        var duration = 5.0;
        var fps = 30;
        var zoomStart = 1.0;
        var zoomEnd = 1.2;

        // Act
        var result = EffectBuilder.BuildKenBurns(duration, fps, zoomStart, zoomEnd);

        // Assert
        Assert.Contains("zoompan", result);
        Assert.Contains($"d={duration}*{fps}", result);
        Assert.Contains($"fps={fps}", result);
    }

    [Fact]
    public void BuildBlur_WithSigma_ShouldGenerateBlurFilter()
    {
        // Arrange
        var sigma = 5.0;

        // Act
        var result = EffectBuilder.BuildBlur(sigma);

        // Assert
        Assert.Contains("gblur", result);
        Assert.Contains("sigma=5.000", result);
    }

    [Fact]
    public void BuildSharpen_WithParameters_ShouldGenerateSharpenFilter()
    {
        // Arrange
        var luma = 1.5;
        var chroma = 0.0;

        // Act
        var result = EffectBuilder.BuildSharpen(luma, chroma);

        // Assert
        Assert.Contains("unsharp", result);
        Assert.Contains("luma_amount=1.500", result);
        Assert.Contains("chroma_amount=0.000", result);
    }

    [Fact]
    public void BuildBrightnessContrast_WithParameters_ShouldGenerateCorrectFilter()
    {
        // Arrange
        var brightness = 0.1;
        var contrast = 1.2;

        // Act
        var result = EffectBuilder.BuildBrightnessContrast(brightness, contrast);

        // Assert
        Assert.Contains("eq", result);
        Assert.Contains("brightness=0.100", result);
        Assert.Contains("contrast=1.200", result);
    }

    [Fact]
    public void BuildSaturation_WithValue_ShouldGenerateSaturationFilter()
    {
        // Arrange
        var saturation = 1.5;

        // Act
        var result = EffectBuilder.BuildSaturation(saturation);

        // Assert
        Assert.Contains("eq", result);
        Assert.Contains("saturation=1.500", result);
    }

    [Fact]
    public void BuildColorCorrection_WithAllParameters_ShouldIncludeAllAdjustments()
    {
        // Arrange
        var brightness = 0.1;
        var contrast = 1.1;
        var saturation = 1.2;
        var gamma = 1.0;

        // Act
        var result = EffectBuilder.BuildColorCorrection(brightness, contrast, saturation, gamma);

        // Assert
        Assert.Contains("eq", result);
        Assert.Contains("brightness=0.100", result);
        Assert.Contains("contrast=1.100", result);
        Assert.Contains("saturation=1.200", result);
        Assert.Contains("gamma=1.000", result);
    }

    [Fact]
    public void BuildVignette_WithDefaults_ShouldGenerateVignetteFilter()
    {
        // Act
        var result = EffectBuilder.BuildVignette();

        // Assert
        Assert.Contains("vignette", result);
        Assert.Contains("angle=", result);
    }

    [Fact]
    public void BuildChromaticAberration_WithShift_ShouldGenerateAberrationFilter()
    {
        // Arrange
        var shift = 2;

        // Act
        var result = EffectBuilder.BuildChromaticAberration(shift);

        // Assert
        Assert.Contains("rgbashift", result);
        Assert.Contains("rh=2", result);
        Assert.Contains("bh=-2", result);
    }

    [Fact]
    public void BuildFilmGrain_WithStrength_ShouldGenerateNoiseFilter()
    {
        // Arrange
        var strength = 10;

        // Act
        var result = EffectBuilder.BuildFilmGrain(strength);

        // Assert
        Assert.Contains("noise", result);
        Assert.Contains("alls=10", result);
    }

    [Fact]
    public void BuildLetterbox_WithDimensions_ShouldGenerateScaleAndPadFilter()
    {
        // Arrange
        var width = 1920;
        var height = 1080;
        var targetWidth = 1920;
        var targetHeight = 1080;

        // Act
        var result = EffectBuilder.BuildLetterbox(width, height, targetWidth, targetHeight);

        // Assert
        Assert.Contains("scale", result);
        Assert.Contains("pad", result);
        Assert.Contains($"{targetWidth}:{targetHeight}", result);
    }

    [Fact]
    public void BuildSepia_ShouldGenerateSepiaFilter()
    {
        // Act
        var result = EffectBuilder.BuildSepia();

        // Assert
        Assert.Contains("colorchannelmixer", result);
        Assert.Contains(".393", result);
        Assert.Contains(".769", result);
        Assert.Contains(".189", result);
    }

    [Fact]
    public void BuildGrayscale_ShouldGenerateGrayscaleFilter()
    {
        // Act
        var result = EffectBuilder.BuildGrayscale();

        // Assert
        Assert.Contains("hue", result);
        Assert.Contains("s=0", result);
    }

    [Fact]
    public void BuildNegative_ShouldGenerateNegateFilter()
    {
        // Act
        var result = EffectBuilder.BuildNegative();

        // Assert
        Assert.Equal("negate", result);
    }

    [Theory]
    [InlineData("horizontal", "hflip")]
    [InlineData("vertical", "vflip")]
    [InlineData("both", "hflip,vflip")]
    public void BuildMirror_WithDifferentModes_ShouldGenerateCorrectFilter(string mode, string expectedFilter)
    {
        // Act
        var result = EffectBuilder.BuildMirror(mode);

        // Assert
        Assert.Contains(expectedFilter, result);
    }

    [Fact]
    public void BuildRotation_WithAngle_ShouldGenerateRotateFilter()
    {
        // Arrange
        var angleDegrees = 90.0;

        // Act
        var result = EffectBuilder.BuildRotation(angleDegrees);

        // Assert
        Assert.Contains("rotate", result);
        Assert.Contains("bilinear=0", result);
        Assert.Contains("fillcolor=black", result);
    }

    [Fact]
    public void BuildStabilization_WithParameters_ShouldGenerateDeshakeFilter()
    {
        // Arrange
        var shakiness = 5;
        var smoothing = 10;

        // Act
        var result = EffectBuilder.BuildStabilization(shakiness, smoothing);

        // Assert
        Assert.Contains("deshake", result);
        Assert.Contains("shakiness=5", result);
        Assert.Contains("smoothing=10", result);
    }

    [Fact]
    public void BuildDenoise_WithParameters_ShouldGenerateDenoiseFilter()
    {
        // Arrange
        var lumaSpatial = 2.0;
        var chromaSpatial = 1.0;

        // Act
        var result = EffectBuilder.BuildDenoise(lumaSpatial, chromaSpatial);

        // Assert
        Assert.Contains("hqdn3d", result);
        Assert.Contains("2.000", result);
        Assert.Contains("1.000", result);
    }

    [Fact]
    public void BuildPictureInPicture_WithParameters_ShouldGeneratePIPFilter()
    {
        // Arrange
        var overlayIndex = 1;
        var x = "W-w-10";
        var y = "H-h-10";
        var scale = 0.25;

        // Act
        var result = EffectBuilder.BuildPictureInPicture(overlayIndex, x, y, scale);

        // Assert
        Assert.Contains("[1:v]", result);
        Assert.Contains("scale", result);
        Assert.Contains("0.250", result);
        Assert.Contains("overlay", result);
    }

    [Theory]
    [InlineData(true, "hstack")]
    [InlineData(false, "vstack")]
    public void BuildSplitScreen_WithOrientation_ShouldGenerateCorrectStackFilter(bool horizontal, string expectedStack)
    {
        // Arrange
        var width = 1920;
        var height = 1080;

        // Act
        var result = EffectBuilder.BuildSplitScreen(width, height, horizontal);

        // Assert
        Assert.Contains(expectedStack, result);
        Assert.Contains("crop", result);
    }

    [Fact]
    public void BuildSlowMotion_WithSpeed_ShouldGenerateSetptsFilter()
    {
        // Arrange
        var speed = 0.5; // 2x slower

        // Act
        var result = EffectBuilder.BuildSlowMotion(speed);

        // Assert
        Assert.Contains("setpts", result);
        Assert.Contains("2.000", result); // 1/0.5 = 2.0
    }

    [Fact]
    public void BuildFastMotion_WithSpeed_ShouldGenerateSetptsFilter()
    {
        // Arrange
        var speed = 2.0; // 2x faster

        // Act
        var result = EffectBuilder.BuildFastMotion(speed);

        // Assert
        Assert.Contains("setpts", result);
        Assert.Contains("0.500", result); // 1/2.0 = 0.5
    }

    [Fact]
    public void BuildReverse_ShouldGenerateReverseFilter()
    {
        // Act
        var result = EffectBuilder.BuildReverse();

        // Assert
        Assert.Equal("reverse", result);
    }

    [Fact]
    public void BuildColorKey_WithParameters_ShouldGenerateChromaKeyFilter()
    {
        // Arrange
        var color = "green";
        var similarity = 0.3;
        var blend = 0.1;

        // Act
        var result = EffectBuilder.BuildColorKey(color, similarity, blend);

        // Assert
        Assert.Contains("colorkey", result);
        Assert.Contains("green", result);
        Assert.Contains("0.300", result);
        Assert.Contains("0.100", result);
    }

    [Fact]
    public void BuildColorFade_WithParameters_ShouldGenerateFadeFilter()
    {
        // Arrange
        var startTime = 5.0;
        var duration = 2.0;
        var type = "in";
        var color = "white";

        // Act
        var result = EffectBuilder.BuildColorFade(startTime, duration, type, color);

        // Assert
        Assert.Contains("fade", result);
        Assert.Contains("t=in", result);
        Assert.Contains("st=5.000", result);
        Assert.Contains("d=2.000", result);
        Assert.Contains("color=white", result);
    }
}
