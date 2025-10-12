using System;
using System.IO;
using System.Threading.Tasks;
using Aura.Core.Runtime;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for attaching external engine installations
/// </summary>
public class EngineAttachTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _configPath;
    private readonly string _logDirectory;
    private readonly System.Net.Http.HttpClient _httpClient;

    public EngineAttachTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "aura-attach-tests-" + Guid.NewGuid().ToString());
        _configPath = Path.Combine(_testDirectory, "config.json");
        _logDirectory = Path.Combine(_testDirectory, "logs");
        _httpClient = new System.Net.Http.HttpClient();
        
        Directory.CreateDirectory(_testDirectory);
        Directory.CreateDirectory(_logDirectory);
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
        _httpClient.Dispose();
    }

    [Fact]
    public async Task AttachExternalEngine_WithValidPath_ShouldSucceed()
    {
        // Arrange
        var processManager = new ExternalProcessManager(
            NullLogger<ExternalProcessManager>.Instance,
            _httpClient,
            _logDirectory);

        var registry = new LocalEnginesRegistry(
            NullLogger<LocalEnginesRegistry>.Instance,
            processManager,
            _configPath);

        var installPath = Path.Combine(_testDirectory, "external-engine");
        Directory.CreateDirectory(installPath);

        // Act
        var (success, error) = await registry.AttachExternalEngineAsync(
            instanceId: "test-external-1",
            engineId: "test-engine",
            name: "Test External Engine",
            installPath: installPath,
            executablePath: null,
            port: 8080,
            healthCheckUrl: "http://localhost:8080/health",
            notes: "Test installation"
        );

        // Assert
        Assert.True(success);
        Assert.Null(error);

        var config = registry.GetEngine("test-external-1");
        Assert.NotNull(config);
        Assert.Equal("test-external-1", config.Id);
        Assert.Equal("test-engine", config.EngineId);
        Assert.Equal(EngineMode.External, config.Mode);
        Assert.Equal(Path.GetFullPath(installPath), config.InstallPath);
        Assert.Equal(8080, config.Port);
        Assert.Equal("Test installation", config.Notes);
    }

    [Fact]
    public async Task AttachExternalEngine_WithInvalidPath_ShouldFail()
    {
        // Arrange
        var processManager = new ExternalProcessManager(
            NullLogger<ExternalProcessManager>.Instance,
            _httpClient,
            _logDirectory);

        var registry = new LocalEnginesRegistry(
            NullLogger<LocalEnginesRegistry>.Instance,
            processManager,
            _configPath);

        var invalidPath = Path.Combine(_testDirectory, "nonexistent-path");

        // Act
        var (success, error) = await registry.AttachExternalEngineAsync(
            instanceId: "test-external-2",
            engineId: "test-engine",
            name: "Test External Engine",
            installPath: invalidPath,
            executablePath: null,
            port: 8080,
            healthCheckUrl: null,
            notes: null
        );

        // Assert
        Assert.False(success);
        Assert.NotNull(error);
        Assert.Contains("does not exist", error);
    }

    [Fact]
    public async Task ReconfigureEngine_WithNewPort_ShouldUpdate()
    {
        // Arrange
        var processManager = new ExternalProcessManager(
            NullLogger<ExternalProcessManager>.Instance,
            _httpClient,
            _logDirectory);

        var registry = new LocalEnginesRegistry(
            NullLogger<LocalEnginesRegistry>.Instance,
            processManager,
            _configPath);

        var installPath = Path.Combine(_testDirectory, "external-engine");
        Directory.CreateDirectory(installPath);

        await registry.AttachExternalEngineAsync(
            instanceId: "test-external-3",
            engineId: "test-engine",
            name: "Test External Engine",
            installPath: installPath,
            executablePath: null,
            port: 8080,
            healthCheckUrl: "http://localhost:8080/health",
            notes: "Initial notes"
        );

        // Act
        var (success, error) = await registry.ReconfigureEngineAsync(
            instanceId: "test-external-3",
            port: 9090,
            notes: "Updated notes"
        );

        // Assert
        Assert.True(success);
        Assert.Null(error);

        var config = registry.GetEngine("test-external-3");
        Assert.NotNull(config);
        Assert.Equal(9090, config.Port);
        Assert.Equal("Updated notes", config.Notes);
        Assert.Equal(Path.GetFullPath(installPath), config.InstallPath); // Should remain unchanged
    }

    [Fact]
    public async Task ReconfigureEngine_WithInvalidInstance_ShouldFail()
    {
        // Arrange
        var processManager = new ExternalProcessManager(
            NullLogger<ExternalProcessManager>.Instance,
            _httpClient,
            _logDirectory);

        var registry = new LocalEnginesRegistry(
            NullLogger<LocalEnginesRegistry>.Instance,
            processManager,
            _configPath);

        // Act
        var (success, error) = await registry.ReconfigureEngineAsync(
            instanceId: "nonexistent-instance",
            port: 9090
        );

        // Assert
        Assert.False(success);
        Assert.NotNull(error);
        Assert.Contains("not found", error);
    }

    [Fact]
    public async Task GetEngineInstances_ShouldReturnAllInstancesOfType()
    {
        // Arrange
        var processManager = new ExternalProcessManager(
            NullLogger<ExternalProcessManager>.Instance,
            _httpClient,
            _logDirectory);

        var registry = new LocalEnginesRegistry(
            NullLogger<LocalEnginesRegistry>.Instance,
            processManager,
            _configPath);

        var path1 = Path.Combine(_testDirectory, "instance1");
        var path2 = Path.Combine(_testDirectory, "instance2");
        Directory.CreateDirectory(path1);
        Directory.CreateDirectory(path2);

        // Add managed instance
        var managedConfig = new EngineConfig(
            Id: "managed-1",
            EngineId: "sd-webui",
            Name: "SD WebUI Managed",
            Version: "1.0",
            Mode: EngineMode.Managed,
            InstallPath: path1,
            ExecutablePath: null,
            Arguments: null,
            Port: 7860,
            HealthCheckUrl: null,
            StartOnAppLaunch: false,
            AutoRestart: false
        );

        await registry.RegisterEngineAsync(managedConfig);

        // Add external instance
        await registry.AttachExternalEngineAsync(
            instanceId: "external-1",
            engineId: "sd-webui",
            name: "SD WebUI External",
            installPath: path2,
            executablePath: null,
            port: 7861,
            healthCheckUrl: null,
            notes: "External install"
        );

        // Act
        var instances = registry.GetEngineInstances("sd-webui");

        // Assert
        Assert.Equal(2, instances.Count);
        Assert.Contains(instances, i => i.Id == "managed-1" && i.Mode == EngineMode.Managed);
        Assert.Contains(instances, i => i.Id == "external-1" && i.Mode == EngineMode.External);
    }

    [Fact]
    public async Task AttachExternalEngine_WithExecutablePath_ShouldValidate()
    {
        // Arrange
        var processManager = new ExternalProcessManager(
            NullLogger<ExternalProcessManager>.Instance,
            _httpClient,
            _logDirectory);

        var registry = new LocalEnginesRegistry(
            NullLogger<LocalEnginesRegistry>.Instance,
            processManager,
            _configPath);

        var installPath = Path.Combine(_testDirectory, "engine-with-exe");
        Directory.CreateDirectory(installPath);
        
        var exePath = Path.Combine(installPath, "engine.exe");
        File.WriteAllText(exePath, "dummy");

        // Act
        var (success, error) = await registry.AttachExternalEngineAsync(
            instanceId: "test-external-4",
            engineId: "test-engine",
            name: "Test Engine",
            installPath: installPath,
            executablePath: exePath,
            port: null,
            healthCheckUrl: null,
            notes: null
        );

        // Assert
        Assert.True(success);
        Assert.Null(error);

        var config = registry.GetEngine("test-external-4");
        Assert.NotNull(config);
        Assert.Equal(Path.GetFullPath(exePath), config.ExecutablePath);
    }

    [Fact]
    public async Task AttachExternalEngine_WithInvalidExecutablePath_ShouldFail()
    {
        // Arrange
        var processManager = new ExternalProcessManager(
            NullLogger<ExternalProcessManager>.Instance,
            _httpClient,
            _logDirectory);

        var registry = new LocalEnginesRegistry(
            NullLogger<LocalEnginesRegistry>.Instance,
            processManager,
            _configPath);

        var installPath = Path.Combine(_testDirectory, "engine-invalid-exe");
        Directory.CreateDirectory(installPath);
        
        var invalidExePath = Path.Combine(installPath, "nonexistent.exe");

        // Act
        var (success, error) = await registry.AttachExternalEngineAsync(
            instanceId: "test-external-5",
            engineId: "test-engine",
            name: "Test Engine",
            installPath: installPath,
            executablePath: invalidExePath,
            port: null,
            healthCheckUrl: null,
            notes: null
        );

        // Assert
        Assert.False(success);
        Assert.NotNull(error);
        Assert.Contains("not found", error);
    }
}
