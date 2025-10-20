using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Download;
using Aura.Core.Services.Download;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class DownloadServiceTests
{
    private readonly Mock<ILogger<DownloadService>> _loggerMock;
    private readonly Mock<HttpClient> _httpClientMock;
    private readonly Mock<FileVerificationService> _verificationServiceMock;
    private readonly DownloadService _service;

    public DownloadServiceTests()
    {
        _loggerMock = new Mock<ILogger<DownloadService>>();
        _httpClientMock = new Mock<HttpClient>();
        
        var verifyLogger = new Mock<ILogger<FileVerificationService>>();
        _verificationServiceMock = new Mock<FileVerificationService>(verifyLogger.Object);
        
        _service = new DownloadService(
            _loggerMock.Object,
            new HttpClient(), // Use real HttpClient for now
            _verificationServiceMock.Object);
    }

    [Fact]
    public void RegisterMirror_ShouldAddMirror()
    {
        // Arrange
        var mirror = new DownloadMirror
        {
            Id = "test-mirror",
            Name = "Test Mirror",
            Url = "https://example.com/file.zip",
            Priority = 1,
            IsEnabled = true
        };

        // Act
        _service.RegisterMirror(mirror);
        var mirrors = _service.GetMirrors();

        // Assert
        Assert.Single(mirrors);
        Assert.Equal("test-mirror", mirrors[0].Id);
    }

    [Fact]
    public void RegisterMirror_WithNullMirror_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.RegisterMirror(null!));
    }

    [Fact]
    public void RegisterMirror_WithEmptyId_ShouldThrow()
    {
        // Arrange
        var mirror = new DownloadMirror
        {
            Id = "",
            Name = "Test Mirror",
            Url = "https://example.com/file.zip"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.RegisterMirror(mirror));
    }

    [Fact]
    public void RegisterMirrors_ShouldAddMultipleMirrors()
    {
        // Arrange
        var mirrors = new List<DownloadMirror>
        {
            new DownloadMirror { Id = "mirror1", Name = "Mirror 1", Url = "https://example.com/1", Priority = 1 },
            new DownloadMirror { Id = "mirror2", Name = "Mirror 2", Url = "https://example.com/2", Priority = 2 }
        };

        // Act
        _service.RegisterMirrors(mirrors);
        var registeredMirrors = _service.GetMirrors();

        // Assert
        Assert.Equal(2, registeredMirrors.Count);
    }

    [Fact]
    public void GetMirrors_Initially_ShouldReturnEmpty()
    {
        // Arrange
        var service = new DownloadService(
            _loggerMock.Object,
            new HttpClient(),
            _verificationServiceMock.Object);

        // Act
        var mirrors = service.GetMirrors();

        // Assert
        Assert.Empty(mirrors);
    }

    [Fact]
    public async Task CheckMirrorHealthAsync_WithInvalidMirrorId_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CheckMirrorHealthAsync("non-existent"));
    }

    [Fact]
    public void MirrorConfiguration_GetDefaultFfmpegMirrors_ShouldReturnMirrors()
    {
        // Act
        var mirrors = MirrorConfiguration.GetDefaultFfmpegMirrors();

        // Assert
        Assert.NotEmpty(mirrors);
        Assert.All(mirrors, m =>
        {
            Assert.False(string.IsNullOrWhiteSpace(m.Id));
            Assert.False(string.IsNullOrWhiteSpace(m.Name));
            Assert.False(string.IsNullOrWhiteSpace(m.Url));
        });
    }

    [Fact]
    public void MirrorConfiguration_ValidateMirrors_WithValidMirrors_ShouldReturnTrue()
    {
        // Arrange
        var mirrors = new List<DownloadMirror>
        {
            new DownloadMirror { Id = "m1", Name = "Mirror 1", Url = "https://example.com/1" },
            new DownloadMirror { Id = "m2", Name = "Mirror 2", Url = "https://example.com/2" }
        };

        // Act
        var isValid = MirrorConfiguration.ValidateMirrors(mirrors);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void MirrorConfiguration_ValidateMirrors_WithDuplicateIds_ShouldReturnFalse()
    {
        // Arrange
        var mirrors = new List<DownloadMirror>
        {
            new DownloadMirror { Id = "m1", Name = "Mirror 1", Url = "https://example.com/1" },
            new DownloadMirror { Id = "m1", Name = "Mirror 2", Url = "https://example.com/2" }
        };

        // Act
        var isValid = MirrorConfiguration.ValidateMirrors(mirrors);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void MirrorConfiguration_ValidateMirrors_WithEmptyList_ShouldReturnFalse()
    {
        // Arrange
        var mirrors = new List<DownloadMirror>();

        // Act
        var isValid = MirrorConfiguration.ValidateMirrors(mirrors);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void MirrorConfiguration_GetMirrorsForComponent_WithFFmpeg_ShouldReturnFFmpegMirrors()
    {
        // Act
        var mirrors = MirrorConfiguration.GetMirrorsForComponent("ffmpeg");

        // Assert
        Assert.NotEmpty(mirrors);
    }

    [Fact]
    public void MirrorConfiguration_GetMirrorsForComponent_WithUnknown_ShouldReturnEmpty()
    {
        // Act
        var mirrors = MirrorConfiguration.GetMirrorsForComponent("unknown");

        // Assert
        Assert.Empty(mirrors);
    }

    [Fact]
    public void DownloadMirror_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var mirror = new DownloadMirror();

        // Assert
        Assert.Equal(string.Empty, mirror.Id);
        Assert.Equal(string.Empty, mirror.Name);
        Assert.Equal(string.Empty, mirror.Url);
        Assert.Equal(0, mirror.Priority);
        Assert.Equal(MirrorHealthStatus.Unknown, mirror.HealthStatus);
        Assert.True(mirror.IsEnabled);
        Assert.Equal(0, mirror.ConsecutiveFailures);
    }

    [Fact]
    public void DownloadProgressEventArgs_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var args = new DownloadProgressEventArgs();

        // Assert
        Assert.Equal(0, args.BytesDownloaded);
        Assert.Equal(0, args.TotalBytes);
        Assert.Equal(0, args.PercentComplete);
        Assert.Equal(DownloadStage.Downloading, args.Stage);
        Assert.False(args.IsComplete);
        Assert.False(args.HasError);
    }
}
