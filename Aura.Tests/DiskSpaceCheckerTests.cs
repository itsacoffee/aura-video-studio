using Aura.Core.Errors;
using Aura.Core.Services.Resources;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

public class DiskSpaceCheckerTests
{
    private readonly DiskSpaceChecker _checker;

    public DiskSpaceCheckerTests()
    {
        var logger = new LoggerFactory().CreateLogger<DiskSpaceChecker>();
        _checker = new DiskSpaceChecker(logger);
    }

    [Fact]
    public async Task HasSufficientSpaceAsync_WithEnoughSpace_ReturnsTrue()
    {
        // Arrange
        var tempPath = Path.GetTempPath();
        var smallRequirement = 1024; // 1 KB

        // Act
        var result = await _checker.HasSufficientSpaceAsync(tempPath, smallRequirement);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasSufficientSpaceAsync_WithExcessiveRequirement_ReturnsFalse()
    {
        // Arrange
        var tempPath = Path.GetTempPath();
        var excessiveRequirement = long.MaxValue / 2; // Unrealistic amount

        // Act
        var result = await _checker.HasSufficientSpaceAsync(tempPath, excessiveRequirement);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task EnsureSufficientSpaceAsync_WithEnoughSpace_DoesNotThrow()
    {
        // Arrange
        var tempPath = Path.GetTempPath();
        var smallRequirement = 1024; // 1 KB

        // Act & Assert
        await _checker.EnsureSufficientSpaceAsync(tempPath, smallRequirement);
        // No exception means test passes
    }

    [Fact]
    public async Task EnsureSufficientSpaceAsync_WithInsufficientSpace_ThrowsResourceException()
    {
        // Arrange
        var tempPath = Path.GetTempPath();
        var excessiveRequirement = long.MaxValue / 2;

        // Act & Assert
        await Assert.ThrowsAsync<ResourceException>(
            async () => await _checker.EnsureSufficientSpaceAsync(tempPath, excessiveRequirement));
    }

    [Fact]
    public void GetAvailableSpace_WithValidPath_ReturnsPositiveValue()
    {
        // Arrange
        var tempPath = Path.GetTempPath();

        // Act
        var space = _checker.GetAvailableSpace(tempPath);

        // Assert
        Assert.NotNull(space);
        Assert.True(space > 0);
    }

    [Fact]
    public void GetDiskSpaceInfo_WithValidPath_ReturnsInfo()
    {
        // Arrange
        var tempPath = Path.GetTempPath();

        // Act
        var info = _checker.GetDiskSpaceInfo(tempPath);

        // Assert
        Assert.True(info.IsAvailable);
        Assert.NotEmpty(info.Path);
        Assert.True(info.TotalBytes > 0);
        Assert.True(info.AvailableBytes >= 0);
        Assert.True(info.PercentUsed >= 0 && info.PercentUsed <= 100);
    }

    [Theory]
    [InlineData(60, 50, 3_600_000)] // 1 minute, medium quality, ~3.6 MB
    [InlineData(300, 50, 18_000_000)] // 5 minutes, medium quality, ~18 MB
    [InlineData(60, 100, 7_200_000)] // 1 minute, high quality, ~7.2 MB
    public void EstimateVideoSpaceRequired_CalculatesCorrectly(double seconds, int quality, long expectedMin)
    {
        // Act
        var result = _checker.EstimateVideoSpaceRequired(seconds, quality);

        // Assert
        Assert.True(result > 0);
        Assert.True(result >= expectedMin, $"Expected at least {expectedMin} bytes, got {result}");
    }

    [Fact]
    public void IsLowDiskSpace_WithSufficientSpace_ReturnsFalse()
    {
        // Arrange
        var tempPath = Path.GetTempPath();

        // Act
        var result = _checker.IsLowDiskSpace(tempPath);

        // Assert
        // This may vary depending on the test environment, but typically temp has enough space
        Assert.False(result);
    }

    [Fact]
    public void GetDiskSpaceInfo_InvalidPath_HandlesGracefully()
    {
        // Arrange - use a truly invalid path that won't work on any OS
        var invalidPath = OperatingSystem.IsWindows() 
            ? "Z:\\NonExistentDrive\\Path" 
            : "/dev/nonexistent/path";

        // Act
        var info = _checker.GetDiskSpaceInfo(invalidPath);

        // Assert
        // On some systems this may still resolve to a valid drive, which is OK
        // We're just testing that it doesn't throw
        Assert.NotNull(info);
    }

    [Fact]
    public void DiskSpaceInfo_Properties_CalculateCorrectly()
    {
        // Arrange
        var info = new DiskSpaceInfo
        {
            TotalBytes = 1024 * 1024 * 1024, // 1 GB
            AvailableBytes = 512 * 1024 * 1024, // 512 MB
            UsedBytes = 512 * 1024 * 1024 // 512 MB
        };

        // Act & Assert
        Assert.Equal(1024, info.TotalMegabytes);
        Assert.Equal(512, info.AvailableMegabytes);
        Assert.Equal(512, info.UsedMegabytes);
        Assert.Equal(1, info.TotalGigabytes);
        Assert.Equal(0.5, info.AvailableGigabytes);
    }

    [Fact]
    public void EstimateVideoSpaceRequired_LowQuality_UsesLowerBitrate()
    {
        // Arrange & Act
        var lowQuality = _checker.EstimateVideoSpaceRequired(60, 10);
        var mediumQuality = _checker.EstimateVideoSpaceRequired(60, 50);
        var highQuality = _checker.EstimateVideoSpaceRequired(60, 90);

        // Assert
        Assert.True(lowQuality < mediumQuality);
        Assert.True(mediumQuality < highQuality);
    }
}
