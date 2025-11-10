using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Aura.Core.Services;
using Aura.Core.Services.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Services.Settings;

public class SettingsServiceTests : IDisposable
{
    private readonly Mock<ILogger<SettingsService>> _mockLogger;
    private readonly Mock<ProviderSettings> _mockProviderSettings;
    private readonly Mock<IKeyStore> _mockKeyStore;
    private readonly Mock<ISecureStorageService> _mockSecureStorage;
    private readonly Mock<IHardwareDetector> _mockHardwareDetector;
    private readonly string _tempDirectory;
    private readonly SettingsService _settingsService;

    public SettingsServiceTests()
    {
        _mockLogger = new Mock<ILogger<SettingsService>>();
        
        // Create temp directory for test settings
        _tempDirectory = Path.Combine(Path.GetTempPath(), "AuraTests_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        
        var mockProviderLogger = new Mock<ILogger<ProviderSettings>>();
        _mockProviderSettings = new Mock<ProviderSettings>(mockProviderLogger.Object);
        _mockProviderSettings.Setup(x => x.GetAuraDataDirectory()).Returns(_tempDirectory);
        _mockProviderSettings.Setup(x => x.GetProjectsDirectory()).Returns(Path.Combine(_tempDirectory, "Projects"));
        _mockProviderSettings.Setup(x => x.GetOutputDirectory()).Returns(Path.Combine(_tempDirectory, "Output"));
        
        _mockKeyStore = new Mock<IKeyStore>();
        _mockSecureStorage = new Mock<ISecureStorageService>();
        _mockHardwareDetector = new Mock<IHardwareDetector>();

        _settingsService = new SettingsService(
            _mockLogger.Object,
            _mockProviderSettings.Object,
            _mockKeyStore.Object,
            _mockSecureStorage.Object,
            _mockHardwareDetector.Object
        );
    }

    public void Dispose()
    {
        // Clean up temp directory
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public async Task GetSettings_ReturnsDefaultSettings_WhenNoFileExists()
    {
        // Act
        var settings = await _settingsService.GetSettingsAsync();

        // Assert
        Assert.NotNull(settings);
        Assert.Equal("1.0.0", settings.Version);
        Assert.NotNull(settings.General);
        Assert.NotNull(settings.FileLocations);
        Assert.NotNull(settings.VideoDefaults);
    }

    [Fact]
    public async Task UpdateSettings_SavesAndPersistsSettings()
    {
        // Arrange
        var settings = await _settingsService.GetSettingsAsync();
        settings.General.Theme = ThemeMode.Dark;
        settings.General.AutosaveIntervalSeconds = 600;

        // Act
        var result = await _settingsService.UpdateSettingsAsync(settings);

        // Assert
        Assert.True(result.Success);
        
        // Verify settings persisted
        var loadedSettings = await _settingsService.GetSettingsAsync();
        Assert.Equal(ThemeMode.Dark, loadedSettings.General.Theme);
        Assert.Equal(600, loadedSettings.General.AutosaveIntervalSeconds);
    }

    [Fact]
    public async Task UpdateSettings_ValidatesSettings_AndReturnsErrors()
    {
        // Arrange
        var settings = await _settingsService.GetSettingsAsync();
        settings.General.AutosaveIntervalSeconds = 10; // Invalid: too low
        settings.VideoDefaults.DefaultFrameRate = 200; // Invalid: too high

        // Act
        var result = await _settingsService.UpdateSettingsAsync(settings);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ResetToDefaults_RestoresDefaultSettings()
    {
        // Arrange
        var settings = await _settingsService.GetSettingsAsync();
        settings.General.Theme = ThemeMode.Dark;
        await _settingsService.UpdateSettingsAsync(settings);

        // Act
        var result = await _settingsService.ResetToDefaultsAsync();

        // Assert
        Assert.True(result.Success);
        
        var loadedSettings = await _settingsService.GetSettingsAsync();
        Assert.Equal(ThemeMode.Auto, loadedSettings.General.Theme);
    }

    [Fact]
    public async Task ValidateSettings_IdentifiesInvalidAutosaveInterval()
    {
        // Arrange
        var settings = await _settingsService.GetSettingsAsync();
        settings.General.AutosaveIntervalSeconds = 10; // Too low

        // Act
        var result = await _settingsService.ValidateSettingsAsync(settings);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Key == "AutosaveIntervalSeconds");
    }

    [Fact]
    public async Task ValidateSettings_IdentifiesInvalidResolution()
    {
        // Arrange
        var settings = await _settingsService.GetSettingsAsync();
        settings.VideoDefaults.DefaultResolution = "999x999"; // Invalid

        // Act
        var result = await _settingsService.ValidateSettingsAsync(settings);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Key == "DefaultResolution");
    }

    [Fact]
    public async Task ExportSettings_ReturnsJsonString()
    {
        // Arrange
        var settings = await _settingsService.GetSettingsAsync();
        settings.General.Theme = ThemeMode.Dark;
        await _settingsService.UpdateSettingsAsync(settings);

        // Act
        var json = await _settingsService.ExportSettingsAsync(includeSecrets: false);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"theme\"", json.ToLower());
    }

    [Fact]
    public async Task ImportSettings_RestoresSettings()
    {
        // Arrange
        var originalSettings = await _settingsService.GetSettingsAsync();
        originalSettings.General.Theme = ThemeMode.Dark;
        await _settingsService.UpdateSettingsAsync(originalSettings);
        
        var json = await _settingsService.ExportSettingsAsync();

        // Reset to defaults
        await _settingsService.ResetToDefaultsAsync();

        // Act
        var result = await _settingsService.ImportSettingsAsync(json, overwriteExisting: true);

        // Assert
        Assert.True(result.Success);
        
        var loadedSettings = await _settingsService.GetSettingsAsync();
        Assert.Equal(ThemeMode.Dark, loadedSettings.General.Theme);
    }

    [Fact]
    public async Task GetHardwareSettings_ReturnsDefaultSettings()
    {
        // Act
        var settings = await _settingsService.GetHardwareSettingsAsync();

        // Assert
        Assert.NotNull(settings);
        Assert.True(settings.HardwareAccelerationEnabled);
        Assert.Equal("auto", settings.PreferredEncoder);
    }

    [Fact]
    public async Task UpdateHardwareSettings_SavesAndPersistsSettings()
    {
        // Arrange
        var settings = await _settingsService.GetHardwareSettingsAsync();
        settings.PreferredEncoder = "nvenc";
        settings.MaxCacheSizeMB = 10000;

        // Act
        var result = await _settingsService.UpdateHardwareSettingsAsync(settings);

        // Assert
        Assert.True(result.Success);
        
        var loadedSettings = await _settingsService.GetHardwareSettingsAsync();
        Assert.Equal("nvenc", loadedSettings.PreferredEncoder);
        Assert.Equal(10000, loadedSettings.MaxCacheSizeMB);
    }

    [Fact]
    public async Task GetProviderConfiguration_ReturnsProviderSettings()
    {
        // Arrange
        _mockProviderSettings.Setup(x => x.GetOpenAiApiKey()).Returns("test-key");
        _mockProviderSettings.Setup(x => x.GetOllamaUrl()).Returns("http://localhost:11434");
        _mockProviderSettings.Setup(x => x.GetOllamaModel()).Returns("llama3.1:8b-q4_k_m");

        // Act
        var config = await _settingsService.GetProviderConfigurationAsync();

        // Assert
        Assert.NotNull(config);
        Assert.Equal("test-key", config.OpenAI.ApiKey);
        Assert.True(config.OpenAI.Enabled);
        Assert.Equal("http://localhost:11434", config.Ollama.BaseUrl);
    }

    [Fact]
    public async Task UpdateProviderConfiguration_StoresApiKeysSecurely()
    {
        // Arrange
        var config = await _settingsService.GetProviderConfigurationAsync();
        config.OpenAI.ApiKey = "new-test-key";
        config.Anthropic.ApiKey = "anthropic-key";

        // Act
        var result = await _settingsService.UpdateProviderConfigurationAsync(config);

        // Assert
        Assert.True(result.Success);
        _mockKeyStore.Verify(
            x => x.SetKeyAsync("OpenAI", "new-test-key", It.IsAny<CancellationToken>()),
            Times.Once
        );
        _mockKeyStore.Verify(
            x => x.SetKeyAsync("Anthropic", "anthropic-key", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetAvailableGpuDevices_ReturnsAutoAndDetectedGpu()
    {
        // Arrange
        var mockSystemProfile = new SystemProfile
        {
            Gpu = new GpuInfo
            {
                Vendor = "NVIDIA",
                Model = "RTX 4090",
                VramGB = 24
            }
        };
        _mockHardwareDetector.Setup(x => x.DetectSystemAsync()).ReturnsAsync(mockSystemProfile);

        // Act
        var devices = await _settingsService.GetAvailableGpuDevicesAsync();

        // Assert
        Assert.NotNull(devices);
        Assert.Contains(devices, d => d.Id == "auto");
        Assert.Contains(devices, d => d.Name == "RTX 4090");
    }

    [Fact]
    public async Task GetAvailableEncoders_ReturnsBasicEncoders()
    {
        // Arrange
        var mockSystemProfile = new SystemProfile
        {
            EnableNVENC = false
        };
        _mockHardwareDetector.Setup(x => x.DetectSystemAsync()).ReturnsAsync(mockSystemProfile);

        // Act
        var encoders = await _settingsService.GetAvailableEncodersAsync();

        // Assert
        Assert.NotNull(encoders);
        Assert.Contains(encoders, e => e.Id == "auto");
        Assert.Contains(encoders, e => e.Id == "libx264");
        Assert.Contains(encoders, e => e.Id == "libx265");
    }

    [Fact]
    public async Task GetAvailableEncoders_IncludesNvencWhenAvailable()
    {
        // Arrange
        var mockSystemProfile = new SystemProfile
        {
            EnableNVENC = true
        };
        _mockHardwareDetector.Setup(x => x.DetectSystemAsync()).ReturnsAsync(mockSystemProfile);

        // Act
        var encoders = await _settingsService.GetAvailableEncodersAsync();

        // Assert
        Assert.NotNull(encoders);
        Assert.Contains(encoders, e => e.Id == "h264_nvenc");
        Assert.Contains(encoders, e => e.Id == "hevc_nvenc");
    }

    [Fact]
    public async Task GetSettingsSection_ReturnsCorrectSection()
    {
        // Arrange
        var settings = await _settingsService.GetSettingsAsync();
        settings.General.Theme = ThemeMode.Dark;
        await _settingsService.UpdateSettingsAsync(settings);

        // Act
        var generalSettings = await _settingsService.GetSettingsSectionAsync<GeneralSettings>();

        // Assert
        Assert.NotNull(generalSettings);
        Assert.Equal(ThemeMode.Dark, generalSettings.Theme);
    }

    [Fact]
    public async Task UpdateSettingsSection_UpdatesOnlyThatSection()
    {
        // Arrange
        var generalSettings = new GeneralSettings
        {
            Theme = ThemeMode.Light,
            AutosaveIntervalSeconds = 900
        };

        // Act
        var result = await _settingsService.UpdateSettingsSectionAsync(generalSettings);

        // Assert
        Assert.True(result.Success);
        
        var loadedSettings = await _settingsService.GetSettingsAsync();
        Assert.Equal(ThemeMode.Light, loadedSettings.General.Theme);
        Assert.Equal(900, loadedSettings.General.AutosaveIntervalSeconds);
    }
}
