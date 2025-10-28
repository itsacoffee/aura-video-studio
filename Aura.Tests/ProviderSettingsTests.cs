using System;
using System.IO;
using Aura.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class ProviderSettingsTests : IDisposable
{
    private readonly ILogger<ProviderSettings> _logger;
    private readonly string _testDirectory;

    public ProviderSettingsTests()
    {
        _logger = NullLogger<ProviderSettings>.Instance;
        _testDirectory = Path.Combine(Path.GetTempPath(), "aura-provider-settings-tests-" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    private ProviderSettings CreateTestSettings()
    {
        return new ProviderSettings(_logger);
    }

    [Fact]
    public void ProviderSettings_Should_AlwaysReturnPortableRoot()
    {
        // Arrange
        var settings = CreateTestSettings();

        // Act
        var portableRoot = settings.GetPortableRootPath();

        // Assert - Portable mode is always enabled
        Assert.NotNull(portableRoot);
        Assert.NotEmpty(portableRoot);
    }

    [Fact]
    public void GetToolsDirectory_Should_ReturnToolsSubfolder()
    {
        // Arrange
        var settings = CreateTestSettings();

        // Act
        var toolsDir = settings.GetToolsDirectory();
        var portableRoot = settings.GetPortableRootPath();

        // Assert
        Assert.Contains("Tools", toolsDir);
        Assert.StartsWith(portableRoot, toolsDir);
        Assert.True(Directory.Exists(toolsDir)); // Should create directory
    }

    [Fact]
    public void GetAuraDataDirectory_Should_ReturnAuraDataSubfolder()
    {
        // Arrange
        var settings = CreateTestSettings();

        // Act
        var auraDataDir = settings.GetAuraDataDirectory();
        var portableRoot = settings.GetPortableRootPath();

        // Assert
        Assert.Contains("AuraData", auraDataDir);
        Assert.StartsWith(portableRoot, auraDataDir);
        Assert.True(Directory.Exists(auraDataDir)); // Should create directory
    }

    [Fact]
    public void GetLogsDirectory_Should_ReturnLogsSubfolder()
    {
        // Arrange
        var settings = CreateTestSettings();

        // Act
        var logsDir = settings.GetLogsDirectory();
        var portableRoot = settings.GetPortableRootPath();

        // Assert
        Assert.Contains("Logs", logsDir);
        Assert.StartsWith(portableRoot, logsDir);
        Assert.True(Directory.Exists(logsDir)); // Should create directory
    }

    [Fact]
    public void GetProjectsDirectory_Should_ReturnProjectsSubfolder()
    {
        // Arrange
        var settings = CreateTestSettings();

        // Act
        var projectsDir = settings.GetProjectsDirectory();
        var portableRoot = settings.GetPortableRootPath();

        // Assert
        Assert.Contains("Projects", projectsDir);
        Assert.StartsWith(portableRoot, projectsDir);
        Assert.True(Directory.Exists(projectsDir)); // Should create directory
    }

    [Fact]
    public void GetDownloadsDirectory_Should_ReturnDownloadsSubfolder()
    {
        // Arrange
        var settings = CreateTestSettings();

        // Act
        var downloadsDir = settings.GetDownloadsDirectory();
        var portableRoot = settings.GetPortableRootPath();

        // Assert
        Assert.Contains("Downloads", downloadsDir);
        Assert.StartsWith(portableRoot, downloadsDir);
        Assert.True(Directory.Exists(downloadsDir)); // Should create directory
    }

    [Fact]
    public void GetOutputDirectory_Should_DefaultToProjectsDirectory()
    {
        // Arrange
        var settings = CreateTestSettings();

        // Act
        var outputDir = settings.GetOutputDirectory();
        var projectsDir = settings.GetProjectsDirectory();

        // Assert
        Assert.Equal(projectsDir, outputDir);
    }

    [Fact]
    public void PortableDirectories_Should_AllExistAfterFirstAccess()
    {
        // Arrange
        var settings = CreateTestSettings();

        // Act - Access all directory properties
        var portableRoot = settings.GetPortableRootPath();
        var toolsDir = settings.GetToolsDirectory();
        var auraDataDir = settings.GetAuraDataDirectory();
        var logsDir = settings.GetLogsDirectory();
        var projectsDir = settings.GetProjectsDirectory();
        var downloadsDir = settings.GetDownloadsDirectory();

        // Assert - All directories should be created
        Assert.True(Directory.Exists(toolsDir));
        Assert.True(Directory.Exists(auraDataDir));
        Assert.True(Directory.Exists(logsDir));
        Assert.True(Directory.Exists(projectsDir));
        Assert.True(Directory.Exists(downloadsDir));
    }

    [Fact]
    public void ProviderSettings_Should_CreateSettingsInAuraData()
    {
        // Arrange & Act
        var settings = CreateTestSettings();
        var auraDataDir = settings.GetAuraDataDirectory();

        // Assert
        Assert.True(Directory.Exists(auraDataDir));
        // Settings file would be created on first save
    }

    [Fact]
    public void IsValidApiKey_Should_ReturnFalseForNull()
    {
        // Assert
        Assert.False(ProviderSettings.IsValidApiKey(null));
    }

    [Fact]
    public void IsValidApiKey_Should_ReturnFalseForEmpty()
    {
        // Assert
        Assert.False(ProviderSettings.IsValidApiKey(""));
        Assert.False(ProviderSettings.IsValidApiKey("   "));
    }

    [Fact]
    public void IsValidApiKey_Should_ReturnFalseForTooShort()
    {
        // Assert
        Assert.False(ProviderSettings.IsValidApiKey("shortkey"));
        Assert.False(ProviderSettings.IsValidApiKey("12345"));
    }

    [Fact]
    public void IsValidApiKey_Should_ReturnTrueForValidKey()
    {
        // Assert
        Assert.True(ProviderSettings.IsValidApiKey("sk-1234567890abcdefghijklmnopqrstuvwxyz1234567890"));
        Assert.True(ProviderSettings.IsValidApiKey("AIzaSyABCDEFGH1234567890IJKLMNOPQRSTUVWXYZ"));
    }

    [Fact]
    public void IsValidAzureEndpoint_Should_ReturnFalseForNull()
    {
        // Assert
        Assert.False(ProviderSettings.IsValidAzureEndpoint(null));
    }

    [Fact]
    public void IsValidAzureEndpoint_Should_ReturnFalseForEmpty()
    {
        // Assert
        Assert.False(ProviderSettings.IsValidAzureEndpoint(""));
        Assert.False(ProviderSettings.IsValidAzureEndpoint("   "));
    }

    [Fact]
    public void IsValidAzureEndpoint_Should_ReturnFalseForNonHttps()
    {
        // Assert
        Assert.False(ProviderSettings.IsValidAzureEndpoint("http://myresource.openai.azure.com"));
    }

    [Fact]
    public void IsValidAzureEndpoint_Should_ReturnFalseForWrongDomain()
    {
        // Assert
        Assert.False(ProviderSettings.IsValidAzureEndpoint("https://myresource.azure.com"));
        Assert.False(ProviderSettings.IsValidAzureEndpoint("https://example.com"));
    }

    [Fact]
    public void IsValidAzureEndpoint_Should_ReturnTrueForValidEndpoint()
    {
        // Assert
        Assert.True(ProviderSettings.IsValidAzureEndpoint("https://myresource.openai.azure.com"));
        Assert.True(ProviderSettings.IsValidAzureEndpoint("https://myresource.openai.azure.com/"));
        Assert.True(ProviderSettings.IsValidAzureEndpoint("HTTPS://MYRESOURCE.OPENAI.AZURE.COM"));
    }

    [Fact]
    public void GetApiKey_Should_ThrowForMissingKey()
    {
        // Arrange
        var settings = CreateTestSettings();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            settings.GetApiKey("nonExistentKey", "TestProvider"));
        Assert.Contains("TestProvider", exception.Message);
        Assert.Contains("not configured", exception.Message);
    }
}
