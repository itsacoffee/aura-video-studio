using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Services.Setup;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for FFmpeg path persistence functionality
/// </summary>
public class FFmpegDetectionServicePersistenceTests : IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly FFmpegConfigurationStore _configStore;
    private readonly string _testDir;

    public FFmpegDetectionServicePersistenceTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        
        // Create a temporary directory for test configuration
        _testDir = Path.Combine(Path.GetTempPath(), "FFmpegDetectionTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        
        // Create a real configuration store with test directory
        _configStore = new FFmpegConfigurationStore(
            NullLogger<FFmpegConfigurationStore>.Instance);
    }

    [Fact]
    public async Task DetectFFmpegAsync_WhenFFmpegNotFound_DoesNotPersist()
    {
        // Arrange
        var service = new FFmpegDetectionService(
            NullLogger<FFmpegDetectionService>.Instance,
            _cache,
            _configStore);

        // Clear any existing config
        await _configStore.ClearAsync();

        // Act - detection will fail since no FFmpeg is set up in test environment
        var result = await service.DetectFFmpegAsync(CancellationToken.None);

        // Assert
        Assert.False(result.IsInstalled);
        
        // Verify configuration was not persisted (should still be default/empty)
        var loadedConfig = await _configStore.LoadAsync();
        Assert.Equal(FFmpegMode.None, loadedConfig.Mode);
        Assert.Null(loadedConfig.Path);
    }

    [Fact]
    public async Task DetectFFmpegAsync_WithNullConfigStore_DoesNotThrow()
    {
        // Arrange - no config store provided
        var service = new FFmpegDetectionService(
            NullLogger<FFmpegDetectionService>.Instance,
            _cache,
            configStore: null);

        // Act & Assert - should not throw even without config store
        var result = await service.DetectFFmpegAsync(CancellationToken.None);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetCachedStatusAsync_ReturnsCachedResult()
    {
        // Arrange
        var service = new FFmpegDetectionService(
            NullLogger<FFmpegDetectionService>.Instance,
            _cache,
            _configStore);

        // First detection
        var result1 = await service.DetectFFmpegAsync(CancellationToken.None);

        // Act - get cached status
        var result2 = await service.GetCachedStatusAsync(CancellationToken.None);

        // Assert - should return same result
        Assert.Equal(result1.IsInstalled, result2.IsInstalled);
        Assert.Equal(result1.Path, result2.Path);
        Assert.Equal(result1.Version, result2.Version);
    }

    [Fact]
    public void FFmpegDetectionService_RequiresLogger()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new FFmpegDetectionService(null!, _cache, _configStore));
    }

    [Fact]
    public void FFmpegDetectionService_RequiresCache()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new FFmpegDetectionService(NullLogger<FFmpegDetectionService>.Instance, null!, _configStore));
    }

    [Fact]
    public async Task FFmpegConfigurationStore_CanSaveAndLoadConfiguration()
    {
        // Arrange
        var testConfig = new FFmpegConfiguration
        {
            Mode = FFmpegMode.System,
            Path = "/usr/bin/ffmpeg",
            Version = "4.4.2",
            LastValidatedAt = DateTime.UtcNow,
            LastValidationResult = FFmpegValidationResult.Ok,
            Source = "PATH"
        };

        // Act
        await _configStore.SaveAsync(testConfig, CancellationToken.None);
        var loadedConfig = await _configStore.LoadAsync(CancellationToken.None);

        // Assert
        Assert.Equal(testConfig.Mode, loadedConfig.Mode);
        Assert.Equal(testConfig.Path, loadedConfig.Path);
        Assert.Equal(testConfig.Version, loadedConfig.Version);
        Assert.Equal(testConfig.Source, loadedConfig.Source);
        Assert.Equal(FFmpegValidationResult.Ok, loadedConfig.LastValidationResult);
        Assert.True(loadedConfig.IsValid);
    }

    [Fact]
    public async Task FFmpegConfigurationStore_SavesNewTrackingProperties()
    {
        // Arrange
        var testConfig = new FFmpegConfiguration
        {
            Mode = FFmpegMode.Custom,
            Path = "/opt/ffmpeg/bin/ffmpeg",
            Version = "5.1.2",
            LastValidatedAt = DateTime.UtcNow,
            LastValidationResult = FFmpegValidationResult.Ok,
            Source = "ElectronDetection",
            DetectionSourceType = DetectionSource.ElectronDetection,
            LastDetectedPath = "/opt/ffmpeg/bin/ffmpeg",
            LastDetectedAt = DateTime.UtcNow,
            ValidationOutput = "ffmpeg version 5.1.2"
        };

        // Act
        await _configStore.SaveAsync(testConfig, CancellationToken.None);
        var loadedConfig = await _configStore.LoadAsync(CancellationToken.None);

        // Assert
        Assert.Equal(DetectionSource.ElectronDetection, loadedConfig.DetectionSourceType);
        Assert.Equal(testConfig.LastDetectedPath, loadedConfig.LastDetectedPath);
        Assert.NotNull(loadedConfig.LastDetectedAt);
        Assert.Equal(testConfig.ValidationOutput, loadedConfig.ValidationOutput);
    }

    [Fact]
    public async Task FFmpegConfiguration_DetectionSourceEnum_RoundTrips()
    {
        // Test each detection source value
        var sources = new[]
        {
            DetectionSource.None,
            DetectionSource.Managed,
            DetectionSource.System,
            DetectionSource.UserConfigured,
            DetectionSource.Environment,
            DetectionSource.ElectronDetection
        };

        foreach (var source in sources)
        {
            // Arrange
            var testConfig = new FFmpegConfiguration
            {
                Mode = FFmpegMode.System,
                Path = "/usr/bin/ffmpeg",
                DetectionSourceType = source
            };

            // Act
            await _configStore.SaveAsync(testConfig, CancellationToken.None);
            var loadedConfig = await _configStore.LoadAsync(CancellationToken.None);

            // Assert
            Assert.Equal(source, loadedConfig.DetectionSourceType);
        }
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
        
        _cache?.Dispose();
    }
}
