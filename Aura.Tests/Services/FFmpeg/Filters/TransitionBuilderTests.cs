using System;
using System.Collections.Generic;
using Aura.Core.Services.FFmpeg.Filters;
using Xunit;

namespace Aura.Tests.Services.FFmpeg.Filters;

public class TransitionBuilderTests
{
    [Fact]
    public void BuildCrossfade_WithBasicParameters_ShouldGenerateValidFilter()
    {
        // Arrange
        var duration = 1.0;
        var offset = 5.0;
        var type = TransitionBuilder.TransitionType.Fade;

        // Act
        var result = TransitionBuilder.BuildCrossfade(duration, offset, type);

        // Assert
        Assert.Contains("xfade", result);
        Assert.Contains("transition=fade", result);
        Assert.Contains("duration=1.000", result);
        Assert.Contains("offset=5.000", result);
    }

    [Fact]
    public void BuildCrossfade_WithDissolveTransition_ShouldGenerateDissolveFilter()
    {
        // Arrange
        var duration = 0.5;
        var offset = 10.0;
        var type = TransitionBuilder.TransitionType.Dissolve;

        // Act
        var result = TransitionBuilder.BuildCrossfade(duration, offset, type);

        // Assert
        Assert.Contains("transition=dissolve", result);
    }

    [Fact]
    public void BuildFadeIn_WithDefaultParameters_ShouldGenerateFadeInFilter()
    {
        // Arrange
        var duration = 2.0;

        // Act
        var result = TransitionBuilder.BuildFadeIn(duration);

        // Assert
        Assert.Contains("fade=t=in", result);
        Assert.Contains("st=0", result);
        Assert.Contains("d=2.000", result);
        Assert.Contains("color=black", result);
    }

    [Fact]
    public void BuildFadeOut_WithParameters_ShouldGenerateFadeOutFilter()
    {
        // Arrange
        var startTime = 10.0;
        var duration = 2.0;

        // Act
        var result = TransitionBuilder.BuildFadeOut(startTime, duration);

        // Assert
        Assert.Contains("fade=t=out", result);
        Assert.Contains("st=10.000", result);
        Assert.Contains("d=2.000", result);
    }

    [Fact]
    public void BuildWipe_WithLeftDirection_ShouldGenerateWipeLeftFilter()
    {
        // Arrange
        var duration = 1.0;
        var offset = 5.0;
        var direction = "left";

        // Act
        var result = TransitionBuilder.BuildWipe(duration, offset, direction);

        // Assert
        Assert.Contains("xfade", result);
        Assert.Contains("transition=wipeleft", result);
    }

    [Fact]
    public void BuildWipe_WithRightDirection_ShouldGenerateWipeRightFilter()
    {
        // Arrange
        var duration = 1.0;
        var offset = 5.0;
        var direction = "right";

        // Act
        var result = TransitionBuilder.BuildWipe(duration, offset, direction);

        // Assert
        Assert.Contains("transition=wiperight", result);
    }

    [Fact]
    public void BuildSlide_WithUpDirection_ShouldGenerateSlideUpFilter()
    {
        // Arrange
        var duration = 1.0;
        var offset = 5.0;
        var direction = "up";

        // Act
        var result = TransitionBuilder.BuildSlide(duration, offset, direction);

        // Assert
        Assert.Contains("transition=slideup", result);
    }

    [Fact]
    public void BuildDissolve_WithParameters_ShouldGenerateDissolveFilter()
    {
        // Arrange
        var duration = 1.5;
        var offset = 8.0;

        // Act
        var result = TransitionBuilder.BuildDissolve(duration, offset);

        // Assert
        Assert.Contains("xfade", result);
        Assert.Contains("transition=dissolve", result);
        Assert.Contains("duration=1.500", result);
        Assert.Contains("offset=8.000", result);
    }

    [Fact]
    public void BuildPixelize_WithParameters_ShouldGeneratePixelizeFilter()
    {
        // Arrange
        var duration = 1.0;
        var offset = 5.0;
        var steps = 20;

        // Act
        var result = TransitionBuilder.BuildPixelize(duration, offset, steps);

        // Assert
        Assert.Contains("xfade", result);
        Assert.Contains("transition=pixelize", result);
    }

    [Fact]
    public void BuildCircle_WithOpening_ShouldGenerateCircleOpenFilter()
    {
        // Arrange
        var duration = 1.0;
        var offset = 5.0;
        var opening = true;

        // Act
        var result = TransitionBuilder.BuildCircle(duration, offset, opening);

        // Assert
        Assert.Contains("transition=circleopen", result);
    }

    [Fact]
    public void BuildCircle_WithClosing_ShouldGenerateCircleCloseFilter()
    {
        // Arrange
        var duration = 1.0;
        var offset = 5.0;
        var opening = false;

        // Act
        var result = TransitionBuilder.BuildCircle(duration, offset, opening);

        // Assert
        Assert.Contains("transition=circleclose", result);
    }

    [Fact]
    public void BuildRadial_WithParameters_ShouldGenerateRadialFilter()
    {
        // Arrange
        var duration = 1.0;
        var offset = 5.0;

        // Act
        var result = TransitionBuilder.BuildRadial(duration, offset);

        // Assert
        Assert.Contains("xfade", result);
        Assert.Contains("transition=radial", result);
    }

    [Fact]
    public void BuildTransitionChain_WithMultipleClips_ShouldGenerateMultipleTransitions()
    {
        // Arrange
        var clipCount = 3;
        var clipDuration = 10.0;
        var transitionDuration = 1.0;

        // Act
        var results = TransitionBuilder.BuildTransitionChain(clipCount, clipDuration, transitionDuration);

        // Assert
        Assert.Equal(2, results.Count); // 3 clips = 2 transitions
        Assert.All(results, r => Assert.Contains("xfade", r));
    }

    [Fact]
    public void BuildComplexFilterGraph_WithMultipleInputs_ShouldGenerateCompleteFilterChain()
    {
        // Arrange
        var inputCount = 3;
        var clipDurations = new[] { 10.0, 8.0, 12.0 };
        var transitionDurations = new[] { 1.0, 1.0 };
        var transitionTypes = new[]
        {
            TransitionBuilder.TransitionType.Fade,
            TransitionBuilder.TransitionType.Dissolve
        };

        // Act
        var result = TransitionBuilder.BuildComplexFilterGraph(
            inputCount,
            clipDurations,
            transitionDurations,
            transitionTypes
        );

        // Assert
        Assert.Contains("[0:v][1:v]", result);
        Assert.Contains("[v01]", result);
        Assert.Contains("transition=fade", result);
        Assert.Contains("transition=dissolve", result);
    }

    [Theory]
    [InlineData(TransitionBuilder.TransitionType.WipeLeft, "wipeleft")]
    [InlineData(TransitionBuilder.TransitionType.WipeRight, "wiperight")]
    [InlineData(TransitionBuilder.TransitionType.WipeUp, "wipeup")]
    [InlineData(TransitionBuilder.TransitionType.WipeDown, "wipedown")]
    [InlineData(TransitionBuilder.TransitionType.Fade, "fade")]
    public void BuildCustomTransition_WithVariousTypes_ShouldIncludeCorrectTransitionType(
        TransitionBuilder.TransitionType type,
        string expectedName)
    {
        // Arrange
        var duration = 1.0;
        var offset = 5.0;

        // Act
        var result = TransitionBuilder.BuildCustomTransition(duration, offset, type);

        // Assert
        Assert.Contains($"transition={expectedName}", result);
    }
}
