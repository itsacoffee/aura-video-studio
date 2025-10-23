using Aura.Api.Services.QualityValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests.QualityValidation;

public class ResolutionValidationServiceTests
{
    private readonly ResolutionValidationService _service;

    public ResolutionValidationServiceTests()
    {
        _service = new ResolutionValidationService(NullLogger<ResolutionValidationService>.Instance);
    }

    [Fact]
    public async Task ValidateResolutionAsync_MeetsMinimum_ReturnsValid()
    {
        // Arrange
        var width = 1920;
        var height = 1080;
        var minWidth = 1280;
        var minHeight = 720;

        // Act
        var result = await _service.ValidateResolutionAsync(width, height, minWidth, minHeight);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(result.MeetsMinimumResolution);
        Assert.Equal(width, result.Width);
        Assert.Equal(height, result.Height);
        Assert.Equal("16:9", result.AspectRatio);
        Assert.Equal("Full HD 1080p", result.ResolutionCategory);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task ValidateResolutionAsync_BelowMinimum_ReturnsInvalid()
    {
        // Arrange
        var width = 640;
        var height = 480;
        var minWidth = 1280;
        var minHeight = 720;

        // Act
        var result = await _service.ValidateResolutionAsync(width, height, minWidth, minHeight);

        // Assert
        Assert.False(result.IsValid);
        Assert.False(result.MeetsMinimumResolution);
        Assert.NotEmpty(result.Issues);
        Assert.Contains("below minimum requirement", result.Issues[0]);
    }

    [Fact]
    public async Task ValidateResolutionAsync_4KResolution_ReturnsHighCategory()
    {
        // Arrange
        var width = 3840;
        var height = 2160;

        // Act
        var result = await _service.ValidateResolutionAsync(width, height);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("4K UHD", result.ResolutionCategory);
        Assert.Equal(100, result.Score);
    }

    [Fact]
    public async Task ValidateResolutionAsync_OddDimensions_HasWarning()
    {
        // Arrange
        var width = 1921;
        var height = 1081;

        // Act
        var result = await _service.ValidateResolutionAsync(width, height);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotEmpty(result.Warnings);
        Assert.Contains("even numbers", result.Warnings[0]);
    }

    [Theory]
    [InlineData(1920, 1080, "16:9")]
    [InlineData(1280, 720, "16:9")]
    [InlineData(1024, 768, "4:3")]
    [InlineData(1080, 1080, "1:1")]
    [InlineData(1080, 1920, "9:16")]
    public async Task ValidateResolutionAsync_VariousAspectRatios_CalculatesCorrectly(int width, int height, string expectedRatio)
    {
        // Act
        var result = await _service.ValidateResolutionAsync(width, height);

        // Assert
        Assert.Equal(expectedRatio, result.AspectRatio);
    }

    [Fact]
    public async Task ValidateResolutionAsync_CalculatesTotalPixels()
    {
        // Arrange
        var width = 1920;
        var height = 1080;

        // Act
        var result = await _service.ValidateResolutionAsync(width, height);

        // Assert
        Assert.Equal(1920 * 1080, result.TotalPixels);
    }
}
