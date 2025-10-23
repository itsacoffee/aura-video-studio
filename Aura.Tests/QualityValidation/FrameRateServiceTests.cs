using Aura.Api.Services.QualityValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests.QualityValidation;

public class FrameRateServiceTests
{
    private readonly FrameRateService _service;

    public FrameRateServiceTests()
    {
        _service = new FrameRateService(NullLogger<FrameRateService>.Instance);
    }

    [Fact]
    public async Task ValidateFrameRateAsync_ConsistentFrameRate_ReturnsValid()
    {
        // Arrange
        var actualFPS = 30.0;
        var expectedFPS = 30.0;
        var tolerance = 0.5;

        // Act
        var result = await _service.ValidateFrameRateAsync(actualFPS, expectedFPS, tolerance);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(result.IsConsistent);
        Assert.Equal(0, result.Variance);
        Assert.Equal(0, result.DroppedFrames);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task ValidateFrameRateAsync_InconsistentFrameRate_ReturnsInvalid()
    {
        // Arrange
        var actualFPS = 30.0;
        var expectedFPS = 60.0;
        var tolerance = 0.5;

        // Act
        var result = await _service.ValidateFrameRateAsync(actualFPS, expectedFPS, tolerance);

        // Assert
        Assert.False(result.IsValid);
        Assert.False(result.IsConsistent);
        Assert.True(result.Variance > tolerance);
        Assert.NotEmpty(result.Issues);
    }

    [Theory]
    [InlineData(24.0, "Cinema 24 FPS")]
    [InlineData(30.0, "NTSC 30 FPS")]
    [InlineData(60.0, "High Frame Rate 60 FPS")]
    [InlineData(120.0, "High Frame Rate 120+ FPS")]
    public async Task ValidateFrameRateAsync_VariousFrameRates_CategorizesCorrectly(double fps, string expectedCategory)
    {
        // Act
        var result = await _service.ValidateFrameRateAsync(fps, fps, 0.5);

        // Assert
        Assert.Equal(expectedCategory, result.FrameRateCategory);
    }

    [Fact]
    public async Task ValidateFrameRateAsync_LowFrameRate_HasWarning()
    {
        // Arrange
        var actualFPS = 20.0;
        var expectedFPS = 20.0;

        // Act
        var result = await _service.ValidateFrameRateAsync(actualFPS, expectedFPS);

        // Assert
        Assert.NotEmpty(result.Warnings);
        Assert.Contains("below cinematic standard", result.Warnings[0]);
    }

    [Fact]
    public async Task ValidateFrameRateAsync_SmallVariance_WithinTolerance()
    {
        // Arrange
        var actualFPS = 29.8;
        var expectedFPS = 30.0;
        var tolerance = 0.5;

        // Act
        var result = await _service.ValidateFrameRateAsync(actualFPS, expectedFPS, tolerance);

        // Assert
        Assert.True(result.IsConsistent);
        Assert.True(result.Variance <= tolerance);
    }

    [Fact]
    public async Task ValidateFrameRateAsync_CalculatesVariance()
    {
        // Arrange
        var actualFPS = 25.0;
        var expectedFPS = 30.0;

        // Act
        var result = await _service.ValidateFrameRateAsync(actualFPS, expectedFPS);

        // Assert
        Assert.Equal(5.0, result.Variance);
    }
}
