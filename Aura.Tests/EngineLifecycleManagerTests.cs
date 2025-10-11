using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Unit tests for EngineLifecycleManager
/// </summary>
public class EngineLifecycleManagerTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _configPath;
    private readonly string _logDirectory;
    private readonly System.Net.Http.HttpClient _httpClient;

    public EngineLifecycleManagerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "aura-lifecycle-tests-" + Guid.NewGuid().ToString());
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
    public async Task StartAsync_Should_AutoLaunchConfiguredEngines()
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
        
        var lifecycleManager = new EngineLifecycleManager(
            NullLogger<EngineLifecycleManager>.Instance,
            registry,
            processManager);

        // Register a test engine with auto-start
        var testEngine = new EngineConfig(
            Id: "test-engine",
            Name: "Test Engine",
            Version: "1.0",
            InstallPath: _testDirectory,
            ExecutablePath: GetEchoExecutable(),
            Arguments: GetEchoArguments(),
            Port: null,
            HealthCheckUrl: null,
            StartOnAppLaunch: true,
            AutoRestart: false);

        await registry.RegisterEngineAsync(testEngine);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await lifecycleManager.StartAsync(cts.Token);

        // Assert
        var status = await registry.GetEngineStatusAsync("test-engine");
        Assert.True(status.IsRunning);

        // Cleanup
        await lifecycleManager.StopAsync();
    }

    [Fact]
    public async Task StopAsync_Should_GracefullyShutdownEngines()
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
        
        var lifecycleManager = new EngineLifecycleManager(
            NullLogger<EngineLifecycleManager>.Instance,
            registry,
            processManager);

        // Start a test engine
        var testEngine = new EngineConfig(
            Id: "test-engine",
            Name: "Test Engine",
            Version: "1.0",
            InstallPath: _testDirectory,
            ExecutablePath: GetEchoExecutable(),
            Arguments: GetEchoArguments(),
            Port: null,
            HealthCheckUrl: null,
            StartOnAppLaunch: false,
            AutoRestart: false);

        await registry.RegisterEngineAsync(testEngine);
        await registry.StartEngineAsync("test-engine");
        await Task.Delay(500); // Give it time to start

        // Act
        await lifecycleManager.StopAsync();

        // Assert
        var status = await registry.GetEngineStatusAsync("test-engine");
        Assert.False(status.IsRunning);
    }

    [Fact]
    public async Task GenerateDiagnosticsAsync_Should_ReturnSystemReport()
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
        
        var lifecycleManager = new EngineLifecycleManager(
            NullLogger<EngineLifecycleManager>.Instance,
            registry,
            processManager);

        // Register multiple test engines
        var engine1 = new EngineConfig(
            Id: "engine1",
            Name: "Engine 1",
            Version: "1.0",
            InstallPath: _testDirectory,
            ExecutablePath: GetEchoExecutable(),
            Arguments: "test1",
            Port: 8001,
            HealthCheckUrl: null,
            StartOnAppLaunch: false,
            AutoRestart: false);

        var engine2 = new EngineConfig(
            Id: "engine2",
            Name: "Engine 2",
            Version: "1.0",
            InstallPath: _testDirectory,
            ExecutablePath: GetEchoExecutable(),
            Arguments: "test2",
            Port: 8002,
            HealthCheckUrl: null,
            StartOnAppLaunch: false,
            AutoRestart: false);

        await registry.RegisterEngineAsync(engine1);
        await registry.RegisterEngineAsync(engine2);

        // Start only engine1
        await registry.StartEngineAsync("engine1");
        await Task.Delay(500);

        // Act
        var report = await lifecycleManager.GenerateDiagnosticsAsync();

        // Assert
        Assert.NotNull(report);
        Assert.Equal(2, report.TotalEngines);
        Assert.Equal(1, report.RunningEngines);
        Assert.Equal(2, report.Engines.Count);
        
        var engine1Diag = report.Engines.FirstOrDefault(e => e.EngineId == "engine1");
        Assert.NotNull(engine1Diag);
        Assert.True(engine1Diag.IsRunning);
        Assert.Equal(8001, engine1Diag.Port);

        var engine2Diag = report.Engines.FirstOrDefault(e => e.EngineId == "engine2");
        Assert.NotNull(engine2Diag);
        Assert.False(engine2Diag.IsRunning);

        // Cleanup
        await lifecycleManager.StopAsync();
    }

    [Fact]
    public async Task RestartEngineAsync_Should_RestartEngine()
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
        
        var lifecycleManager = new EngineLifecycleManager(
            NullLogger<EngineLifecycleManager>.Instance,
            registry,
            processManager);

        var testEngine = new EngineConfig(
            Id: "test-engine",
            Name: "Test Engine",
            Version: "1.0",
            InstallPath: _testDirectory,
            ExecutablePath: GetEchoExecutable(),
            Arguments: GetEchoArguments(),
            Port: null,
            HealthCheckUrl: null,
            StartOnAppLaunch: false,
            AutoRestart: false);

        await registry.RegisterEngineAsync(testEngine);
        await registry.StartEngineAsync("test-engine");
        await Task.Delay(500);

        var statusBefore = await registry.GetEngineStatusAsync("test-engine");
        Assert.True(statusBefore.IsRunning);
        var pidBefore = processManager.GetStatus("test-engine").ProcessId;

        // Act
        var result = await lifecycleManager.RestartEngineAsync("test-engine");
        await Task.Delay(500);

        // Assert
        Assert.True(result);
        var statusAfter = await registry.GetEngineStatusAsync("test-engine");
        Assert.True(statusAfter.IsRunning);
        var pidAfter = processManager.GetStatus("test-engine").ProcessId;
        
        // Process ID should be different after restart
        Assert.NotEqual(pidBefore, pidAfter);

        // Cleanup
        await lifecycleManager.StopAsync();
    }

    [Fact]
    public void GetRecentNotifications_Should_ReturnNotificationsList()
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
        
        var lifecycleManager = new EngineLifecycleManager(
            NullLogger<EngineLifecycleManager>.Instance,
            registry,
            processManager);

        // Act
        var notifications = lifecycleManager.GetRecentNotifications();

        // Assert
        Assert.NotNull(notifications);
        Assert.IsAssignableFrom<IReadOnlyList<EngineNotification>>(notifications);
    }

    [Fact]
    public async Task NotificationReceived_Should_BeRaisedOnEngineEvents()
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
        
        var lifecycleManager = new EngineLifecycleManager(
            NullLogger<EngineLifecycleManager>.Instance,
            registry,
            processManager);

        EngineNotification? receivedNotification = null;
        lifecycleManager.NotificationReceived += (sender, notification) =>
        {
            receivedNotification = notification;
        };

        var testEngine = new EngineConfig(
            Id: "test-engine",
            Name: "Test Engine",
            Version: "1.0",
            InstallPath: _testDirectory,
            ExecutablePath: GetEchoExecutable(),
            Arguments: GetEchoArguments(),
            Port: 8080,
            HealthCheckUrl: null,
            StartOnAppLaunch: true,
            AutoRestart: false);

        await registry.RegisterEngineAsync(testEngine);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await lifecycleManager.StartAsync(cts.Token);
        await Task.Delay(2000); // Wait for health check

        // Assert - notification should have been received
        Assert.NotNull(receivedNotification);
        Assert.Equal("test-engine", receivedNotification.EngineId);

        // Cleanup
        await lifecycleManager.StopAsync();
    }

    private string GetEchoExecutable()
    {
        // Return a simple executable that stays running for tests
        if (OperatingSystem.IsWindows())
        {
            // Use timeout to wait as a long-running process
            return "timeout";
        }
        else
        {
            // Use sleep on Unix-like systems
            return "sleep";
        }
    }

    private string GetEchoArguments()
    {
        // Return arguments for the test executable
        if (OperatingSystem.IsWindows())
        {
            return "/t 3600"; // Wait for 1 hour
        }
        else
        {
            return "3600"; // Sleep for 1 hour
        }
    }
}
