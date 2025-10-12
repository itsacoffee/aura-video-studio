using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Dependencies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Integration tests for portable mode functionality
/// </summary>
public class PortableModeIntegrationTests : IDisposable
{
    private readonly ILogger<ProviderSettings> _providerLogger;
    private readonly ILogger<DependencyManager> _dependencyLogger;
    private readonly string _testDirectory;
    private readonly string _portableRoot;

    public PortableModeIntegrationTests()
    {
        _providerLogger = NullLogger<ProviderSettings>.Instance;
        _dependencyLogger = NullLogger<DependencyManager>.Instance;
        _testDirectory = Path.Combine(Path.GetTempPath(), "aura-portable-integration-" + Guid.NewGuid().ToString());
        _portableRoot = Path.Combine(_testDirectory, "portable");
        
        Directory.CreateDirectory(_testDirectory);
        Directory.CreateDirectory(_portableRoot);
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

    [Fact]
    public void PortableMode_Should_CreateStructureInPortableRoot()
    {
        // Arrange
        var settings = new ProviderSettings(_providerLogger);
        settings.SetPortableMode(true, _portableRoot);

        // Act
        var toolsDir = settings.GetToolsDirectory();

        // Assert
        Assert.Equal(_portableRoot, toolsDir);
    }

    [Fact]
    public void DependencyManager_Should_UsePortableRoot_WhenProvided()
    {
        // Arrange
        var httpClient = new HttpClient();
        var manifestPath = Path.Combine(_portableRoot, "manifest.json");
        var downloadDirectory = _portableRoot;

        // Act
        var manager = new DependencyManager(
            _dependencyLogger,
            httpClient,
            manifestPath,
            downloadDirectory,
            _portableRoot);

        // Assert
        Assert.True(manager.IsPortableModeEnabled());
        Assert.Equal(_portableRoot, manager.GetPortableRoot());
        Assert.Equal(downloadDirectory, manager.GetComponentDirectory("FFmpeg"));
    }

    [Fact]
    public async Task DependencyManager_Should_CreateManifestInPortableRoot()
    {
        // Arrange
        var httpClient = new HttpClient();
        var manifestPath = Path.Combine(_portableRoot, "manifest.json");
        var downloadDirectory = _portableRoot;

        var manager = new DependencyManager(
            _dependencyLogger,
            httpClient,
            manifestPath,
            downloadDirectory,
            _portableRoot);

        // Act
        var manifest = await manager.LoadManifestAsync();

        // Assert
        Assert.NotNull(manifest);
        Assert.True(File.Exists(manifestPath));
        Assert.NotEmpty(manifest.Components);
    }

    [Fact]
    public void PortableMode_Should_AllowSwitchingBetweenModes()
    {
        // Arrange
        var settings = new ProviderSettings(_providerLogger);
        
        // Ensure we start clean
        settings.SetPortableMode(false);
        
        // Act & Assert - Start in standard mode
        Assert.False(settings.IsPortableModeEnabled());
        var standardPath = settings.GetToolsDirectory();
        Assert.Contains("Aura", standardPath);

        // Switch to portable mode
        settings.SetPortableMode(true, _portableRoot);
        Assert.True(settings.IsPortableModeEnabled());
        Assert.Equal(_portableRoot, settings.GetToolsDirectory());

        // Switch back to standard mode
        settings.SetPortableMode(false);
        Assert.False(settings.IsPortableModeEnabled());
        var backToStandard = settings.GetToolsDirectory();
        Assert.Contains("Aura", backToStandard);
    }

    [Fact]
    public async Task PortableMode_Should_IsolateInstallations()
    {
        // Arrange
        var httpClient = new HttpClient();
        var portableManifest = Path.Combine(_portableRoot, "manifest.json");
        var portableDir = _portableRoot;

        var standardRoot = Path.Combine(_testDirectory, "standard");
        Directory.CreateDirectory(standardRoot);
        var standardManifest = Path.Combine(standardRoot, "manifest.json");
        var standardDir = Path.Combine(standardRoot, "dependencies");

        // Create managers for both modes
        var portableManager = new DependencyManager(
            _dependencyLogger,
            httpClient,
            portableManifest,
            portableDir,
            _portableRoot);

        var standardManager = new DependencyManager(
            _dependencyLogger,
            httpClient,
            standardManifest,
            standardDir,
            null);

        // Act
        var portableManifestData = await portableManager.LoadManifestAsync();
        var standardManifestData = await standardManager.LoadManifestAsync();

        // Assert - Both should have their own manifests
        Assert.True(File.Exists(portableManifest));
        Assert.True(File.Exists(standardManifest));
        Assert.NotEqual(portableManifest, standardManifest);
        
        // Portable manager should be in portable mode
        Assert.True(portableManager.IsPortableModeEnabled());
        Assert.Equal(_portableRoot, portableManager.GetPortableRoot());
        
        // Standard manager should not be in portable mode
        Assert.False(standardManager.IsPortableModeEnabled());
        Assert.Null(standardManager.GetPortableRoot());
    }

    [Fact]
    public void PortableRoot_Should_SupportDifferentPaths()
    {
        // Arrange
        var settings = new ProviderSettings(_providerLogger);
        var path1 = Path.Combine(_testDirectory, "location1");
        var path2 = Path.Combine(_testDirectory, "location2");

        // Act & Assert - Set first path
        settings.SetPortableMode(true, path1);
        Assert.Equal(path1, settings.GetPortableRootPath());
        Assert.Equal(path1, settings.GetToolsDirectory());

        // Change to second path
        settings.SetPortableMode(true, path2);
        Assert.Equal(path2, settings.GetPortableRootPath());
        Assert.Equal(path2, settings.GetToolsDirectory());
    }

    [Fact]
    public void PortableMode_PathValidation_Should_HandleInvalidPaths()
    {
        // Arrange
        var settings = new ProviderSettings(_providerLogger);
        
        // Ensure we start clean
        settings.SetPortableMode(false);

        // Act & Assert - Empty path with portable mode enabled should still use portable logic
        // but fallback to AppData since path is empty
        settings.SetPortableMode(true, "");
        Assert.True(settings.IsPortableModeEnabled());
        var toolsDir = settings.GetToolsDirectory();
        // When portable mode is enabled but path is empty/whitespace, it falls back to AppData
        Assert.Contains("dependencies", toolsDir); // Should fallback to AppData

        // Whitespace path should also fallback
        settings.SetPortableMode(true, "   ");
        var toolsDir2 = settings.GetToolsDirectory();
        Assert.Contains("dependencies", toolsDir2); // Should fallback to AppData
        
        // Cleanup
        settings.SetPortableMode(false);
    }
}
