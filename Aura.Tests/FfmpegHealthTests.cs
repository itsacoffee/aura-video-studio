using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Aura.Core.Downloads;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class FfmpegHealthTests
{
    private readonly ILogger<FfmpegInstaller> _installerLogger;
    private readonly ILogger<HttpDownloader> _downloaderLogger;
    private readonly string _testDir;

    public FfmpegHealthTests()
    {
        _installerLogger = NullLogger<FfmpegInstaller>.Instance;
        _downloaderLogger = NullLogger<HttpDownloader>.Instance;
        _testDir = Path.Combine(Path.GetTempPath(), "AuraHealthTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    [Fact]
    public async Task RunSmokeTestAsync_Should_ReturnFalse_WhenFfmpegDoesNotExist()
    {
        // Arrange
        var httpClient = new System.Net.Http.HttpClient();
        var downloader = new HttpDownloader(_downloaderLogger, httpClient);
        var installer = new FfmpegInstaller(_installerLogger, downloader, _testDir);
        var nonExistentPath = Path.Combine(_testDir, "nonexistent.exe");

        // Act
        var result = await installer.RunSmokeTestAsync(nonExistentPath);

        // Assert
        Assert.False(result.success);
        Assert.NotNull(result.error);
    }

    [Fact]
    public async Task RunSmokeTestAsync_Should_ReturnFalse_WhenFfmpegIsInvalid()
    {
        // Arrange
        var httpClient = new System.Net.Http.HttpClient();
        var downloader = new HttpDownloader(_downloaderLogger, httpClient);
        var installer = new FfmpegInstaller(_installerLogger, downloader, _testDir);
        
        // Create a fake "ffmpeg" that's just a text file
        var fakeFfmpegPath = Path.Combine(_testDir, "fake_ffmpeg.exe");
        await File.WriteAllTextAsync(fakeFfmpegPath, "This is not a real binary");

        // Act
        var result = await installer.RunSmokeTestAsync(fakeFfmpegPath);

        // Assert
        Assert.False(result.success);
        Assert.NotNull(result.error);

        // Cleanup
        File.Delete(fakeFfmpegPath);
    }

    [Fact]
    public void FfmpegInstallResult_Should_ContainMetadata()
    {
        // Arrange & Act
        var result = new FfmpegInstallResult
        {
            Success = true,
            InstallPath = "/path/to/ffmpeg",
            FfmpegPath = "/path/to/ffmpeg/ffmpeg.exe",
            FfprobePath = "/path/to/ffmpeg/ffprobe.exe",
            ValidationOutput = "ffmpeg version 8.0",
            SourceType = InstallSourceType.Network,
            SourceUrl = "https://example.com/ffmpeg.zip",
            Sha256 = "abc123",
            InstalledAt = DateTime.UtcNow
        };

        // Assert
        Assert.True(result.Success);
        Assert.Equal("/path/to/ffmpeg", result.InstallPath);
        Assert.Equal("/path/to/ffmpeg/ffmpeg.exe", result.FfmpegPath);
        Assert.Equal("/path/to/ffmpeg/ffprobe.exe", result.FfprobePath);
        Assert.Contains("ffmpeg version", result.ValidationOutput);
        Assert.Equal(InstallSourceType.Network, result.SourceType);
        Assert.Equal("https://example.com/ffmpeg.zip", result.SourceUrl);
        Assert.Equal("abc123", result.Sha256);
    }

    [Fact]
    public void FfmpegInstallMetadata_Should_TrackValidationState()
    {
        // Arrange & Act
        var metadata = new FfmpegInstallMetadata
        {
            Id = "ffmpeg",
            Version = "8.0",
            InstallPath = "/path",
            FfmpegPath = "/path/ffmpeg.exe",
            Validated = true,
            ValidationOutput = "ffmpeg version 8.0",
            ValidatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal("ffmpeg", metadata.Id);
        Assert.Equal("8.0", metadata.Version);
        Assert.True(metadata.Validated);
        Assert.NotNull(metadata.ValidationOutput);
    }

    // Note: Integration tests with real ffmpeg would go here if available in CI
    // These would test actual smoke test execution with a real ffmpeg binary
}
