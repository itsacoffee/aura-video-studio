using Aura.Api.Services.QualityValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests.QualityValidation;

public class PlatformRequirementsServiceTests
{
    private readonly PlatformRequirementsService _service;

    public PlatformRequirementsServiceTests()
    {
        _service = new PlatformRequirementsService(NullLogger<PlatformRequirementsService>.Instance);
    }

    [Fact]
    public async Task ValidateAsync_YouTube_ValidVideo_ReturnsValid()
    {
        // Arrange
        var width = 1920;
        var height = 1080;
        var fileSizeBytes = 50 * 1024 * 1024L; // 50 MB
        var durationSeconds = 300.0; // 5 minutes
        var codec = "H.264";

        // Act
        var result = await _service.ValidateAsync("youtube", width, height, fileSizeBytes, durationSeconds, codec);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("YouTube", result.Platform);
        Assert.True(result.MeetsResolutionRequirements);
        Assert.True(result.MeetsDurationRequirements);
        Assert.True(result.MeetsFileSizeRequirements);
        Assert.True(result.MeetsCodecRequirements);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task ValidateAsync_TikTok_InvalidAspectRatio_HasWarning()
    {
        // Arrange - TikTok requires 9:16
        var width = 1920;
        var height = 1080; // 16:9 instead of 9:16
        var fileSizeBytes = 50 * 1024 * 1024L;
        var durationSeconds = 30.0;
        var codec = "H.264";

        // Act
        var result = await _service.ValidateAsync("tiktok", width, height, fileSizeBytes, durationSeconds, codec);

        // Assert
        Assert.False(result.MeetsAspectRatioRequirements);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public async Task ValidateAsync_Instagram_ExceedsDuration_ReturnsInvalid()
    {
        // Arrange - Instagram has 60s limit
        var width = 1080;
        var height = 1080;
        var fileSizeBytes = 50 * 1024 * 1024L;
        var durationSeconds = 120.0; // Exceeds 60s limit
        var codec = "H.264";

        // Act
        var result = await _service.ValidateAsync("instagram", width, height, fileSizeBytes, durationSeconds, codec);

        // Assert
        Assert.False(result.IsValid);
        Assert.False(result.MeetsDurationRequirements);
        Assert.NotEmpty(result.Issues);
        Assert.Contains("exceeds", result.Issues[0]);
    }

    [Fact]
    public async Task ValidateAsync_UnsupportedCodec_ReturnsInvalid()
    {
        // Arrange
        var width = 1920;
        var height = 1080;
        var fileSizeBytes = 50 * 1024 * 1024L;
        var durationSeconds = 30.0;
        var codec = "ProRes"; // Not supported by most platforms

        // Act
        var result = await _service.ValidateAsync("youtube", width, height, fileSizeBytes, durationSeconds, codec);

        // Assert
        Assert.False(result.IsValid);
        Assert.False(result.MeetsCodecRequirements);
        Assert.NotEmpty(result.Issues);
        Assert.NotEmpty(result.RecommendedOptimizations);
    }

    [Fact]
    public async Task ValidateAsync_UnknownPlatform_ThrowsException()
    {
        // Arrange
        var width = 1920;
        var height = 1080;
        var fileSizeBytes = 50 * 1024 * 1024L;
        var durationSeconds = 30.0;
        var codec = "H.264";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ValidateAsync("unknown-platform", width, height, fileSizeBytes, durationSeconds, codec));
    }

    [Theory]
    [InlineData("youtube")]
    [InlineData("tiktok")]
    [InlineData("instagram")]
    [InlineData("twitter")]
    public async Task ValidateAsync_AllPlatforms_ValidatesSuccessfully(string platform)
    {
        // Arrange - Use safe defaults
        var width = 1080;
        var height = 1920;
        var fileSizeBytes = 10 * 1024 * 1024L;
        var durationSeconds = 30.0;
        var codec = "H.264";

        // Act
        var result = await _service.ValidateAsync(platform, width, height, fileSizeBytes, durationSeconds, codec);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Platform);
    }

    [Fact]
    public async Task ValidateAsync_LargeFileSize_HasRecommendations()
    {
        // Arrange - Use a file size that exceeds Instagram's 650MB limit
        var width = 1920;
        var height = 1080;
        var fileSizeBytes = 700 * 1024 * 1024L; // 700 MB - exceeds Instagram's 650MB limit
        var durationSeconds = 30.0;
        var codec = "H.264";

        // Act
        var result = await _service.ValidateAsync("instagram", width, height, fileSizeBytes, durationSeconds, codec);

        // Assert
        Assert.False(result.MeetsFileSizeRequirements);
        Assert.NotEmpty(result.RecommendedOptimizations);
    }

    [Fact]
    public async Task ValidateAsync_CalculatesScore()
    {
        // Arrange
        var width = 1920;
        var height = 1080;
        var fileSizeBytes = 50 * 1024 * 1024L;
        var durationSeconds = 30.0;
        var codec = "H.264";

        // Act
        var result = await _service.ValidateAsync("youtube", width, height, fileSizeBytes, durationSeconds, codec);

        // Assert
        Assert.InRange(result.Score, 0, 100);
    }
}
