using System;
using Aura.Core.Models;
using Aura.Core.Models.Export;
using Aura.Core.Services.Export;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Export;

public class ResolutionServiceTests
{
    private readonly ResolutionService _service;
    private readonly Mock<ILogger<ResolutionService>> _mockLogger;

    public ResolutionServiceTests()
    {
        _mockLogger = new Mock<ILogger<ResolutionService>>();
        _service = new ResolutionService(_mockLogger.Object);
    }

    [Fact]
    public void DetermineScaleMode_SameAspectRatio_ReturnsStretch()
    {
        // Arrange
        var source = new Resolution(1920, 1080); // 16:9
        var target = new Resolution(1280, 720);  // 16:9
        var targetAspect = AspectRatio.SixteenByNine;

        // Act
        var mode = _service.DetermineScaleMode(source, target, targetAspect);

        // Assert
        Assert.Equal("stretch", mode);
    }

    [Fact]
    public void DetermineScaleMode_VerticalTarget_ReturnsCrop()
    {
        // Arrange
        var source = new Resolution(1920, 1080); // 16:9 landscape
        var target = new Resolution(1080, 1920); // 9:16 vertical
        var targetAspect = AspectRatio.NineBySixteen;

        // Act
        var mode = _service.DetermineScaleMode(source, target, targetAspect);

        // Assert
        Assert.Equal("crop", mode);
    }

    [Fact]
    public void DetermineScaleMode_SquareTarget_ReturnsFit()
    {
        // Arrange
        var source = new Resolution(1920, 1080); // 16:9
        var target = new Resolution(1080, 1080); // 1:1
        var targetAspect = AspectRatio.OneByOne;

        // Act
        var mode = _service.DetermineScaleMode(source, target, targetAspect);

        // Assert
        Assert.Equal("fit", mode);
    }

    [Fact]
    public void CalculateOutputResolution_MaintainAspectRatio_PreservesAspect()
    {
        // Arrange
        var source = new Resolution(1920, 1080); // 16:9
        var target = new Resolution(1280, 720);  // 16:9

        // Act
        var result = _service.CalculateOutputResolution(source, target, maintainAspectRatio: true);

        // Assert
        var sourceAspect = (double)source.Width / source.Height;
        var resultAspect = (double)result.Width / result.Height;
        Assert.Equal(sourceAspect, resultAspect, precision: 2);
    }

    [Fact]
    public void CalculateOutputResolution_NoMaintainAspectRatio_ReturnsTarget()
    {
        // Arrange
        var source = new Resolution(1920, 1080);
        var target = new Resolution(1080, 1080);

        // Act
        var result = _service.CalculateOutputResolution(source, target, maintainAspectRatio: false);

        // Assert
        Assert.Equal(target, result);
    }

    [Fact]
    public void IsUpscaling_TargetLarger_ReturnsTrue()
    {
        // Arrange
        var source = new Resolution(1280, 720);
        var target = new Resolution(1920, 1080);

        // Act
        var isUpscaling = _service.IsUpscaling(source, target);

        // Assert
        Assert.True(isUpscaling);
    }

    [Fact]
    public void IsUpscaling_TargetSmaller_ReturnsFalse()
    {
        // Arrange
        var source = new Resolution(1920, 1080);
        var target = new Resolution(1280, 720);

        // Act
        var isUpscaling = _service.IsUpscaling(source, target);

        // Assert
        Assert.False(isUpscaling);
    }

    [Fact]
    public void GetAspectRatio_16By9_ReturnsCorrectRatio()
    {
        // Arrange
        var resolution = new Resolution(1920, 1080);

        // Act
        var aspectRatio = _service.GetAspectRatio(resolution);

        // Assert
        Assert.Equal(AspectRatio.SixteenByNine, aspectRatio);
    }

    [Fact]
    public void GetAspectRatio_9By16_ReturnsCorrectRatio()
    {
        // Arrange
        var resolution = new Resolution(1080, 1920);

        // Act
        var aspectRatio = _service.GetAspectRatio(resolution);

        // Assert
        Assert.Equal(AspectRatio.NineBySixteen, aspectRatio);
    }

    [Fact]
    public void GetAspectRatio_1By1_ReturnsCorrectRatio()
    {
        // Arrange
        var resolution = new Resolution(1080, 1080);

        // Act
        var aspectRatio = _service.GetAspectRatio(resolution);

        // Assert
        Assert.Equal(AspectRatio.OneByOne, aspectRatio);
    }

    [Fact]
    public void GetAspectRatio_4By5_ReturnsCorrectRatio()
    {
        // Arrange
        var resolution = new Resolution(1080, 1350);

        // Act
        var aspectRatio = _service.GetAspectRatio(resolution);

        // Assert
        Assert.Equal(AspectRatio.FourByFive, aspectRatio);
    }
}
