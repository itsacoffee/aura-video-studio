using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Models;
using Aura.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Configuration;

public class SettingsExportImportServiceTests
{
    private readonly Mock<ILogger<SettingsExportImportService>> _mockLogger;
    private readonly Mock<ISecureStorageService> _mockSecureStorage;
    private readonly SettingsExportImportService _service;

    public SettingsExportImportServiceTests()
    {
        _mockLogger = new Mock<ILogger<SettingsExportImportService>>();
        _mockSecureStorage = new Mock<ISecureStorageService>();
        _service = new SettingsExportImportService(_mockLogger.Object, _mockSecureStorage.Object);
    }

    [Fact]
    public async Task ExportSettingsAsync_WithoutSecrets_ReturnsEmptyApiKeys()
    {
        // Arrange
        var userSettings = CreateTestUserSettings();
        var includeSecrets = false;
        var selectedKeys = new List<string>();
        var acknowledgeWarning = false;

        // Act
        var result = await _service.ExportSettingsAsync(
            userSettings, includeSecrets, selectedKeys, acknowledgeWarning, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.ApiKeys);
        Assert.Empty(result.Data.ApiKeys);
        Assert.False(result.Metadata?.SecretsIncluded);
    }

    [Fact]
    public async Task ExportSettingsAsync_WithSecretsButNoAcknowledgment_ReturnsError()
    {
        // Arrange
        var userSettings = CreateTestUserSettings();
        var includeSecrets = true;
        var selectedKeys = new List<string> { "openai" };
        var acknowledgeWarning = false;

        // Act
        var result = await _service.ExportSettingsAsync(
            userSettings, includeSecrets, selectedKeys, acknowledgeWarning, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Must acknowledge security warning", result.Error ?? string.Empty);
    }

    private UserSettings CreateTestUserSettings()
    {
        return new UserSettings
        {
            General = new GeneralSettings
            {
                Theme = ThemeMode.Dark,
                AdvancedModeEnabled = false,
                Language = "en-US"
            },
            FileLocations = new FileLocationsSettings
            {
                FFmpegPath = "/usr/bin/ffmpeg",
                OutputDirectory = "/home/user/Videos"
            },
            VideoDefaults = new VideoDefaultsSettings(),
            EditorPreferences = new EditorPreferencesSettings(),
            UI = new UISettings(),
            Advanced = new AdvancedSettings()
        };
    }
}
