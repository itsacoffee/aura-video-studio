using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Aura.Tests.Configuration;

public class FfmpegConfigurationServiceTests : IDisposable
{
    private readonly Mock<ILogger<FfmpegConfigurationService>> _loggerMock;
    private readonly Mock<IOptions<FFmpegOptions>> _optionsMock;
    private readonly FFmpegConfigurationStore _store;
    private readonly string _tempConfigPath;

    public FfmpegConfigurationServiceTests()
    {
        _loggerMock = new Mock<ILogger<FfmpegConfigurationService>>();
        _optionsMock = new Mock<IOptions<FFmpegOptions>>();
        
        // Use real FFmpegConfigurationStore with temp directory
        var storeLoggerMock = new Mock<ILogger<FFmpegConfigurationStore>>();
        _store = new FFmpegConfigurationStore(storeLoggerMock.Object);
        
        // Get the config file path for cleanup
        _tempConfigPath = _store.GetConfigFilePath();
    }
    
    public void Dispose()
    {
        // Clean up test configuration file
        if (File.Exists(_tempConfigPath))
        {
            try
            {
                File.Delete(_tempConfigPath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task GetEffectiveConfigurationAsync_WithNoSources_ReturnsDefaultConfiguration()
    {
        // Arrange
        _optionsMock.Setup(o => o.Value).Returns(new FFmpegOptions());

        var service = new FfmpegConfigurationService(_loggerMock.Object, _optionsMock.Object, _store);

        // Act
        var result = await service.GetEffectiveConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(FFmpegMode.None, result.Mode);
        Assert.Null(result.Path);
    }

    [Fact]
    public async Task GetEffectiveConfigurationAsync_WithEnvironmentVariable_AppliesEnvironmentPath()
    {
        // Arrange
        var testPath = "/test/ffmpeg/path";
        Environment.SetEnvironmentVariable("AURA_FFMPEG_PATH", testPath);

        try
        {
            _optionsMock.Setup(o => o.Value).Returns(new FFmpegOptions());

            var service = new FfmpegConfigurationService(_loggerMock.Object, _optionsMock.Object, _store);

            // Act
            var result = await service.GetEffectiveConfigurationAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(testPath, result.Path);
            Assert.Equal(FFmpegMode.Custom, result.Mode);
            Assert.Equal("Environment", result.Source);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AURA_FFMPEG_PATH", null);
        }
    }

    [Fact]
    public async Task GetEffectiveConfigurationAsync_WithOptionsPath_AppliesOptionsPath()
    {
        // Arrange
        var testPath = "/test/ffmpeg/options";
        
        var options = new FFmpegOptions { ExecutablePath = testPath };
        _optionsMock.Setup(o => o.Value).Returns(options);

        var service = new FfmpegConfigurationService(_loggerMock.Object, _optionsMock.Object, _store);

        // Act
        var result = await service.GetEffectiveConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testPath, result.Path);
        Assert.Equal(FFmpegMode.Custom, result.Mode);
        Assert.Equal("Configured", result.Source);
    }

    [Fact]
    public async Task GetEffectiveConfigurationAsync_WithPersistedPath_ReturnsPersistedPath()
    {
        // Arrange
        var testPath = "/test/ffmpeg/persisted";
        var persistedConfig = new FFmpegConfiguration
        {
            Path = testPath,
            Mode = FFmpegMode.Local,
            Source = "User"
        };
        
        // Save config first
        await _store.SaveAsync(persistedConfig);
        
        _optionsMock.Setup(o => o.Value).Returns(new FFmpegOptions());

        var service = new FfmpegConfigurationService(_loggerMock.Object, _optionsMock.Object, _store);

        // Act
        var result = await service.GetEffectiveConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testPath, result.Path);
        Assert.Equal(FFmpegMode.Local, result.Mode);
        Assert.Equal("User", result.Source);
    }

    [Fact]
    public async Task GetEffectiveConfigurationAsync_PriorityOrder_PersistedOverridesAll()
    {
        // Arrange
        var persistedPath = "/persisted";
        var optionsPath = "/options";
        
        Environment.SetEnvironmentVariable("AURA_FFMPEG_PATH", "/env");

        try
        {
            var persistedConfig = new FFmpegConfiguration
            {
                Path = persistedPath,
                Mode = FFmpegMode.Local
            };
            await _store.SaveAsync(persistedConfig);
            
            var options = new FFmpegOptions { ExecutablePath = optionsPath };
            _optionsMock.Setup(o => o.Value).Returns(options);

            var service = new FfmpegConfigurationService(_loggerMock.Object, _optionsMock.Object, _store);

            // Act
            var result = await service.GetEffectiveConfigurationAsync();

            // Assert
            Assert.Equal(persistedPath, result.Path);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AURA_FFMPEG_PATH", null);
        }
    }

    [Fact]
    public async Task UpdateConfigurationAsync_ValidConfiguration_SavesConfiguration()
    {
        // Arrange
        var config = new FFmpegConfiguration
        {
            Path = "/test/path",
            Mode = FFmpegMode.Custom
        };
        _optionsMock.Setup(o => o.Value).Returns(new FFmpegOptions());

        var service = new FfmpegConfigurationService(_loggerMock.Object, _optionsMock.Object, _store);

        // Act
        await service.UpdateConfigurationAsync(config);

        // Assert - verify by reading back
        var saved = await _store.LoadAsync();
        Assert.Equal(config.Path, saved.Path);
        Assert.Equal(config.Mode, saved.Mode);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_NullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        _optionsMock.Setup(o => o.Value).Returns(new FFmpegOptions());
        var service = new FfmpegConfigurationService(_loggerMock.Object, _optionsMock.Object, _store);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.UpdateConfigurationAsync(null!));
    }
}
