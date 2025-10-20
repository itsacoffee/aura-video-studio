using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Download;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class FfmpegAttachServiceTests
{
    private readonly Mock<ILogger<FfmpegAttachService>> _loggerMock;
    private readonly Mock<ILogger<FileVerificationService>> _verifyLoggerMock;
    private readonly FileVerificationService _verificationService;
    private readonly FfmpegAttachService _service;

    public FfmpegAttachServiceTests()
    {
        _loggerMock = new Mock<ILogger<FfmpegAttachService>>();
        _verifyLoggerMock = new Mock<ILogger<FileVerificationService>>();
        _verificationService = new FileVerificationService(_verifyLoggerMock.Object);
        _service = new FfmpegAttachService(_loggerMock.Object, _verificationService);
    }

    [Fact]
    public async Task DetectInstallationAsync_WithNonExistentFile_ShouldReturnNull()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "ffmpeg.exe");

        // Act
        var result = await _service.DetectInstallationAsync(nonExistentPath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void IsCompatible_WithNullInstallation_ShouldReturnFalse()
    {
        // Act
        var result = _service.IsCompatible(null!, "5.0");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsCompatible_WithNoMinimumVersion_ShouldReturnTrue()
    {
        // Arrange
        var installation = new FfmpegInstallation
        {
            Version = "6.0",
            IsValid = true
        };

        // Act
        var result = _service.IsCompatible(installation, null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsCompatible_WithCompatibleVersion_ShouldReturnTrue()
    {
        // Arrange
        var installation = new FfmpegInstallation
        {
            Version = "6.0",
            IsValid = true
        };

        // Act
        var result = _service.IsCompatible(installation, "5.0");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsCompatible_WithIncompatibleVersion_ShouldReturnFalse()
    {
        // Arrange
        var installation = new FfmpegInstallation
        {
            Version = "4.0",
            IsValid = true
        };

        // Act
        var result = _service.IsCompatible(installation, "5.0");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateInstallationAsync_WithNullInstallation_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.ValidateInstallationAsync(null!));
    }

    [Fact]
    public async Task ScanForInstallationsAsync_ShouldComplete()
    {
        // Act
        var installations = await _service.ScanForInstallationsAsync();

        // Assert
        Assert.NotNull(installations);
        // We can't guarantee any installations are found, but the method should complete
    }

    [Fact]
    public void FfmpegInstallation_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var installation = new FfmpegInstallation();

        // Assert
        Assert.Equal(string.Empty, installation.FfmpegPath);
        Assert.Equal(string.Empty, installation.InstallDirectory);
        Assert.Equal(string.Empty, installation.Version);
        Assert.Equal(string.Empty, installation.VersionString);
        Assert.False(installation.IsValid);
    }

    [Fact]
    public void FfmpegValidationResult_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var result = new FfmpegValidationResult();

        // Assert
        Assert.Null(result.Installation);
        Assert.False(result.IsValid);
        Assert.False(result.VersionCheckPassed);
        Assert.False(result.SmokeTestPassed);
        Assert.False(result.FFprobeAvailable);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void FfmpegVersionInfo_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var info = new FfmpegVersionInfo();

        // Assert
        Assert.Equal(string.Empty, info.Version);
        Assert.Equal(string.Empty, info.VersionString);
        Assert.Equal(string.Empty, info.BuildConfiguration);
    }
}
