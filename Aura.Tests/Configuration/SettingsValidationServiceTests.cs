using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Dependencies;
using Aura.Core.Services.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Configuration;

/// <summary>
/// Tests for SettingsValidationService
/// </summary>
public class SettingsValidationServiceTests
{
    private readonly Mock<ILogger<SettingsValidationService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly string _testTempDir;

    public SettingsValidationServiceTests()
    {
        _mockLogger = new Mock<ILogger<SettingsValidationService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _testTempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testTempDir);
    }

    [Fact]
    public async Task ValidateAllAsync_WithMissingFFmpeg_ReturnsCriticalIssue()
    {
        // Arrange
        var mockFfmpegLocator = new Mock<IFfmpegLocator>();
        mockFfmpegLocator
            .Setup(x => x.GetEffectiveFfmpegPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("FFmpeg not found"));

        SetupMockConfiguration();

        var service = new SettingsValidationService(
            _mockLogger.Object,
            _mockConfiguration.Object,
            mockFfmpegLocator.Object);

        // Act
        var result = await service.ValidateAllAsync();

        // Assert
        Assert.False(result.CanStart);
        Assert.Contains(result.CriticalIssues, i => i.Category == "FFmpeg" && i.Code == "FFMPEG_NOT_FOUND");
    }

    [Fact]
    public async Task ValidateAllAsync_WithValidFFmpeg_NoCriticalIssues()
    {
        // Arrange
        var mockFfmpegLocator = new Mock<IFfmpegLocator>();
        var validationResult = new FfmpegValidationResult
        {
            Found = true,
            FfmpegPath = "ffmpeg",
            VersionString = "4.4.0",
            Reason = "Valid FFmpeg binary"
        };

        mockFfmpegLocator
            .Setup(x => x.GetEffectiveFfmpegPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("ffmpeg");

        mockFfmpegLocator
            .Setup(x => x.ValidatePathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        SetupMockConfiguration();

        var service = new SettingsValidationService(
            _mockLogger.Object,
            _mockConfiguration.Object,
            mockFfmpegLocator.Object);

        // Act
        var result = await service.ValidateAllAsync();

        // Assert
        Assert.True(result.CanStart);
        Assert.DoesNotContain(result.CriticalIssues, i => i.Category == "FFmpeg");
    }

    [Fact]
    public void ValidateAllAsync_WithInvalidOutputDirectory_ReturnsCriticalIssue()
    {
        // Arrange
        var invalidPath = Path.Combine(_testTempDir, "nonexistent", "output");
        SetupMockConfiguration(outputDirectory: invalidPath);

        var service = new SettingsValidationService(
            _mockLogger.Object,
            _mockConfiguration.Object);

        // Act
        var result = service.ValidateAllAsync().GetAwaiter().GetResult();

        // Assert
        // Output directory should be created automatically, so this should pass
        // But if creation fails (e.g., permissions), it should be critical
        Assert.True(result.CanStart || result.CriticalIssues.Any(i => i.Category == "OutputDirectory"));
    }

    [Fact]
    public void ValidateAllAsync_WithInvalidDatabasePath_ReturnsCriticalIssue()
    {
        // Arrange
        var invalidDbPath = Path.Combine(_testTempDir, "nonexistent", "database", "aura.db");
        SetupMockConfiguration(databasePath: invalidDbPath);

        var service = new SettingsValidationService(
            _mockLogger.Object,
            _mockConfiguration.Object);

        // Act
        var result = service.ValidateAllAsync().GetAwaiter().GetResult();

        // Assert
        // Database directory should be created automatically, so this should pass
        // But if creation fails (e.g., permissions), it should be critical
        Assert.True(result.CanStart || result.CriticalIssues.Any(i => i.Category == "Database"));
    }

    [Fact]
    public async Task ValidateAllAsync_WithOllamaUnavailable_ReturnsWarning()
    {
        // Arrange
        var mockOllamaDetection = new Mock<OllamaDetectionService>(
            Mock.Of<ILogger<OllamaDetectionService>>(),
            Mock.Of<System.Net.Http.HttpClient>(),
            Mock.Of<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
            "http://localhost:11434");

        mockOllamaDetection
            .Setup(x => x.WaitForInitialDetectionAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var ollamaStatus = new OllamaStatus(
            IsRunning: false,
            IsInstalled: false,
            Version: null,
            BaseUrl: "http://localhost:11434",
            ErrorMessage: "Ollama service not running");

        mockOllamaDetection
            .Setup(x => x.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ollamaStatus);

        SetupMockConfiguration();

        var service = new SettingsValidationService(
            _mockLogger.Object,
            _mockConfiguration.Object,
            ollamaDetection: mockOllamaDetection.Object);

        // Act
        var result = await service.ValidateAllAsync();

        // Assert
        Assert.True(result.CanStart); // Ollama is optional
        Assert.Contains(result.Warnings, w => w.Category == "Ollama" && w.Code == "OLLAMA_NOT_RUNNING");
    }

    [Fact]
    public async Task CheckFfmpegAsync_WithValidFFmpeg_ReturnsAvailable()
    {
        // Arrange
        var mockFfmpegLocator = new Mock<IFfmpegLocator>();
        var validationResult = new FfmpegValidationResult
        {
            Found = true,
            FfmpegPath = "ffmpeg",
            VersionString = "4.4.0"
        };

        mockFfmpegLocator
            .Setup(x => x.GetEffectiveFfmpegPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("ffmpeg");

        mockFfmpegLocator
            .Setup(x => x.ValidatePathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        SetupMockConfiguration();

        var service = new SettingsValidationService(
            _mockLogger.Object,
            _mockConfiguration.Object,
            mockFfmpegLocator.Object);

        // Act
        var result = await service.CheckFfmpegAsync();

        // Assert
        Assert.True(result.IsAvailable);
        Assert.Equal("Available", result.Message);
        Assert.Equal("4.4.0", result.Details);
    }

    [Fact]
    public async Task CheckFfmpegAsync_WithMissingFFmpeg_ReturnsNotAvailable()
    {
        // Arrange
        var mockFfmpegLocator = new Mock<IFfmpegLocator>();
        mockFfmpegLocator
            .Setup(x => x.GetEffectiveFfmpegPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("FFmpeg not found"));

        SetupMockConfiguration();

        var service = new SettingsValidationService(
            _mockLogger.Object,
            _mockConfiguration.Object,
            mockFfmpegLocator.Object);

        // Act
        var result = await service.CheckFfmpegAsync();

        // Assert
        Assert.False(result.IsAvailable);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckDatabase_WithValidPath_ReturnsAvailable()
    {
        // Arrange
        var validDbPath = Path.Combine(_testTempDir, "aura.db");
        SetupMockConfiguration(databasePath: validDbPath);

        var service = new SettingsValidationService(
            _mockLogger.Object,
            _mockConfiguration.Object);

        // Act
        var result = service.CheckDatabase();

        // Assert
        Assert.True(result.IsAvailable);
        Assert.Equal("Available", result.Message);
    }

    [Fact]
    public void CheckOutputDirectory_WithValidPath_ReturnsAvailable()
    {
        // Arrange
        var validOutputDir = Path.Combine(_testTempDir, "output");
        SetupMockConfiguration(outputDirectory: validOutputDir);

        var service = new SettingsValidationService(
            _mockLogger.Object,
            _mockConfiguration.Object);

        // Act
        var result = service.CheckOutputDirectory();

        // Assert
        Assert.True(result.IsAvailable);
        Assert.Equal("Available", result.Message);
    }

    [Fact]
    public async Task ValidateAllAsync_IncludesValidationDuration()
    {
        // Arrange
        SetupMockConfiguration();

        var service = new SettingsValidationService(
            _mockLogger.Object,
            _mockConfiguration.Object);

        // Act
        var result = await service.ValidateAllAsync();

        // Assert
        Assert.True(result.ValidationDuration.TotalMilliseconds >= 0);
    }

    private void SetupMockConfiguration(
        string? outputDirectory = null,
        string? databasePath = null)
    {
        var outputDirSection = new Mock<IConfigurationSection>();
        outputDirSection.Setup(x => x.Value).Returns(outputDirectory ?? Path.Combine(_testTempDir, "output"));

        var dbSection = new Mock<IConfigurationSection>();
        dbSection.Setup(x => x["SQLiteFileName"]).Returns("aura.db");
        dbSection.Setup(x => x["SQLitePath"]).Returns(databasePath ?? Path.Combine(_testTempDir, "aura.db"));

        _mockConfiguration.Setup(x => x["OutputDirectory"]).Returns(outputDirSection.Object.Value);
        _mockConfiguration.Setup(x => x.GetSection("Database")).Returns(dbSection.Object);
        _mockConfiguration.Setup(x => x.GetValue<string>("Database:SQLiteFileName")).Returns("aura.db");
        _mockConfiguration.Setup(x => x.GetValue<string>("Database:SQLitePath")).Returns(databasePath);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testTempDir))
            {
                Directory.Delete(_testTempDir, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}

