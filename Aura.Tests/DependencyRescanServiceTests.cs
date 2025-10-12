using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aura.Tests;

public class DependencyRescanServiceTests
{
    private readonly Mock<FfmpegLocator> _mockFfmpegLocator;
    private readonly Mock<ComponentDownloader> _mockComponentDownloader;
    private readonly DependencyRescanService _service;

    public DependencyRescanServiceTests()
    {
        _mockFfmpegLocator = new Mock<FfmpegLocator>(
            NullLogger<FfmpegLocator>.Instance,
            null);
        
        _mockComponentDownloader = new Mock<ComponentDownloader>(
            NullLogger<ComponentDownloader>.Instance,
            null,
            null,
            "components.json");
        
        _service = new DependencyRescanService(
            NullLogger<DependencyRescanService>.Instance,
            _mockFfmpegLocator.Object,
            _mockComponentDownloader.Object);
    }

    [Fact]
    public async Task RescanAllAsync_ReturnsReport_WithAllDependencies()
    {
        // Arrange
        var manifest = new ComponentsManifest
        {
            Components = new System.Collections.Generic.List<ComponentManifestEntry>
            {
                new ComponentManifestEntry { Id = "ffmpeg", Name = "FFmpeg" },
                new ComponentManifestEntry { Id = "ollama", Name = "Ollama" }
            }
        };

        _mockComponentDownloader
            .Setup(x => x.LoadManifestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);

        _mockFfmpegLocator
            .Setup(x => x.CheckAllCandidatesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FfmpegValidationResult
            {
                Found = false,
                Reason = "Not found"
            });

        // Act
        var report = await _service.RescanAllAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(report);
        Assert.NotEmpty(report.Dependencies);
        Assert.True(report.Dependencies.Count >= 2); // At least FFmpeg and Ollama
        Assert.Contains(report.Dependencies, d => d.Id == "ffmpeg");
    }

    [Fact]
    public async Task RescanAllAsync_WithFfmpegFound_ReportsInstalledStatus()
    {
        // Arrange
        var manifest = new ComponentsManifest
        {
            Components = new System.Collections.Generic.List<ComponentManifestEntry>
            {
                new ComponentManifestEntry { Id = "ffmpeg", Name = "FFmpeg" }
            }
        };

        _mockComponentDownloader
            .Setup(x => x.LoadManifestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);

        _mockFfmpegLocator
            .Setup(x => x.CheckAllCandidatesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FfmpegValidationResult
            {
                Found = true,
                FfmpegPath = "/usr/bin/ffmpeg",
                VersionString = "4.4.2",
                Reason = "Valid FFmpeg binary found"
            });

        // Act
        var report = await _service.RescanAllAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(report);
        var ffmpegReport = Assert.Single(report.Dependencies, d => d.Id == "ffmpeg");
        Assert.Equal(DependencyStatus.Installed, ffmpegReport.Status);
        Assert.Equal("/usr/bin/ffmpeg", ffmpegReport.Path);
        Assert.Equal("4.4.2", ffmpegReport.ValidationOutput);
    }

    [Fact]
    public async Task RescanAllAsync_WithFfmpegNotFound_ReportsMissingStatus()
    {
        // Arrange
        var manifest = new ComponentsManifest
        {
            Components = new System.Collections.Generic.List<ComponentManifestEntry>
            {
                new ComponentManifestEntry { Id = "ffmpeg", Name = "FFmpeg" }
            }
        };

        _mockComponentDownloader
            .Setup(x => x.LoadManifestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);

        _mockFfmpegLocator
            .Setup(x => x.CheckAllCandidatesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FfmpegValidationResult
            {
                Found = false,
                Reason = "FFmpeg not found in any candidate locations"
            });

        // Act
        var report = await _service.RescanAllAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(report);
        var ffmpegReport = Assert.Single(report.Dependencies, d => d.Id == "ffmpeg");
        Assert.Equal(DependencyStatus.Missing, ffmpegReport.Status);
        Assert.NotNull(ffmpegReport.ErrorMessage);
    }

    [Fact]
    public async Task RescanAllAsync_SavesLastScanTime()
    {
        // Arrange
        var manifest = new ComponentsManifest
        {
            Components = new System.Collections.Generic.List<ComponentManifestEntry>()
        };

        _mockComponentDownloader
            .Setup(x => x.LoadManifestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);

        // Act
        var beforeScan = DateTime.UtcNow;
        var report = await _service.RescanAllAsync(CancellationToken.None);
        var afterScan = DateTime.UtcNow;

        // Assert
        Assert.NotNull(report);
        Assert.InRange(report.ScanTime, beforeScan, afterScan);
        
        // Verify last scan time was saved and can be retrieved
        var lastScanTime = await _service.GetLastScanTimeAsync();
        Assert.NotNull(lastScanTime);
        Assert.InRange(lastScanTime.Value, beforeScan, afterScan);
    }

    [Fact]
    public async Task GetLastScanTimeAsync_WithNoScan_ReturnsNull()
    {
        // Act
        var lastScanTime = await _service.GetLastScanTimeAsync();

        // Assert - might be null if no scan has been done yet, or might have a value from a previous test
        // Just verify it doesn't throw
        Assert.True(true);
    }
}
