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
/// Integration tests for portable-only mode functionality
/// All installations are portable by default
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
    public void PortableMode_Should_AlwaysBeEnabled()
    {
        // Arrange
        var settings = new ProviderSettings(_providerLogger);

        // Act
        var portableRoot = settings.GetPortableRootPath();
        var toolsDir = settings.GetToolsDirectory();

        // Assert - Portable mode is always on
        Assert.NotNull(portableRoot);
        Assert.NotEmpty(portableRoot);
        Assert.NotNull(toolsDir);
        Assert.Contains("Tools", toolsDir);
    }

    [Fact]
    public void PortableMode_Should_CreateExpectedDirectoryStructure()
    {
        // Arrange
        var settings = new ProviderSettings(_providerLogger);

        // Act
        var portableRoot = settings.GetPortableRootPath();
        var toolsDir = settings.GetToolsDirectory();
        var auraDataDir = settings.GetAuraDataDirectory();
        var logsDir = settings.GetLogsDirectory();
        var projectsDir = settings.GetProjectsDirectory();
        var downloadsDir = settings.GetDownloadsDirectory();

        // Assert - All directories should be created under portable root
        Assert.StartsWith(portableRoot, toolsDir);
        Assert.StartsWith(portableRoot, auraDataDir);
        Assert.StartsWith(portableRoot, logsDir);
        Assert.StartsWith(portableRoot, projectsDir);
        Assert.StartsWith(portableRoot, downloadsDir);
        
        // Verify directories are created
        Assert.True(Directory.Exists(toolsDir));
        Assert.True(Directory.Exists(auraDataDir));
        Assert.True(Directory.Exists(logsDir));
        Assert.True(Directory.Exists(projectsDir));
        Assert.True(Directory.Exists(downloadsDir));
    }

    [Fact]
    public void DependencyManager_Should_UsePortableRoot_WhenProvided()
    {
        // Arrange
        var httpClient = new HttpClient();
        var manifestPath = Path.Combine(_portableRoot, "install-manifest.json");
        var downloadDirectory = Path.Combine(_portableRoot, "Downloads");

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
    }

    [Fact]
    public async Task DependencyManager_Should_CreateManifestInPortableRoot()
    {
        // Arrange
        var httpClient = new HttpClient();
        var manifestPath = Path.Combine(_portableRoot, "install-manifest.json");
        var downloadDirectory = Path.Combine(_portableRoot, "Downloads");

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
    public async Task PortableMode_Should_SupportMultipleInstallLocations()
    {
        // Arrange
        var httpClient = new HttpClient();
        var portableManifest = Path.Combine(_portableRoot, "install-manifest.json");
        var portableDownloads = Path.Combine(_portableRoot, "Downloads");

        var alternateRoot = Path.Combine(_testDirectory, "alternate");
        Directory.CreateDirectory(alternateRoot);
        var alternateManifest = Path.Combine(alternateRoot, "install-manifest.json");
        var alternateDownloads = Path.Combine(alternateRoot, "Downloads");

        // Create managers for both locations
        var manager1 = new DependencyManager(
            _dependencyLogger,
            httpClient,
            portableManifest,
            portableDownloads,
            _portableRoot);

        var manager2 = new DependencyManager(
            _dependencyLogger,
            httpClient,
            alternateManifest,
            alternateDownloads,
            alternateRoot);

        // Act
        var manifest1 = await manager1.LoadManifestAsync();
        var manifest2 = await manager2.LoadManifestAsync();

        // Assert - Both should have their own manifests
        Assert.True(File.Exists(portableManifest));
        Assert.True(File.Exists(alternateManifest));
        Assert.NotEqual(portableManifest, alternateManifest);
        
        // Both managers should be in portable mode with their respective roots
        Assert.True(manager1.IsPortableModeEnabled());
        Assert.Equal(_portableRoot, manager1.GetPortableRoot());
        
        Assert.True(manager2.IsPortableModeEnabled());
        Assert.Equal(alternateRoot, manager2.GetPortableRoot());
    }
}
