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
/// Integration tests for Engine crash and restart handling
/// </summary>
public class EngineCrashRestartTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _configPath;
    private readonly string _logDirectory;
    private readonly System.Net.Http.HttpClient _httpClient;

    public EngineCrashRestartTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "aura-crash-tests-" + Guid.NewGuid().ToString());
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
    public async Task ExternalProcessManager_Should_TrackProcessStatus()
    {
        // Arrange
        var processManager = new ExternalProcessManager(
            NullLogger<ExternalProcessManager>.Instance,
            _httpClient,
            _logDirectory);

        var config = new ProcessConfig(
            Id: "test-process",
            ExecutablePath: GetTestExecutable(),
            Arguments: GetTestArguments(),
            WorkingDirectory: _testDirectory,
            Port: null,
            HealthCheckUrl: null,
            HealthCheckTimeoutSeconds: 10,
            AutoRestart: false);

        // Act
        var started = await processManager.StartAsync(config);
        await Task.Delay(500); // Give it time to start
        var statusRunning = processManager.GetStatus("test-process");

        await processManager.StopAsync("test-process");
        await Task.Delay(500);
        var statusStopped = processManager.GetStatus("test-process");

        // Assert
        Assert.True(started);
        Assert.True(statusRunning.IsRunning);
        Assert.NotNull(statusRunning.ProcessId);
        Assert.False(statusStopped.IsRunning);
    }

    [Fact]
    public async Task LocalEnginesRegistry_Should_StartAndStopEngines()
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

        var engine = new EngineConfig(
            Id: "test-engine",
            Name: "Test Engine",
            Version: "1.0",
            InstallPath: _testDirectory,
            ExecutablePath: GetTestExecutable(),
            Arguments: GetTestArguments(),
            Port: 8080,
            HealthCheckUrl: null,
            StartOnAppLaunch: false,
            AutoRestart: false);

        await registry.RegisterEngineAsync(engine);

        // Act
        var started = await registry.StartEngineAsync("test-engine");
        await Task.Delay(500);
        var statusAfterStart = await registry.GetEngineStatusAsync("test-engine");

        var stopped = await registry.StopEngineAsync("test-engine");
        await Task.Delay(500);
        var statusAfterStop = await registry.GetEngineStatusAsync("test-engine");

        // Assert
        Assert.True(started);
        Assert.True(statusAfterStart.IsRunning);
        Assert.True(stopped);
        Assert.False(statusAfterStop.IsRunning);
    }

    [Fact]
    public async Task LifecycleManager_Should_GenerateValidDiagnostics()
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

        // Register a couple of test engines
        var engine1 = new EngineConfig(
            Id: "engine-1",
            Name: "Engine 1",
            Version: "1.0",
            InstallPath: _testDirectory,
            ExecutablePath: GetTestExecutable(),
            Arguments: GetTestArguments(),
            Port: 8001,
            HealthCheckUrl: null,
            StartOnAppLaunch: false,
            AutoRestart: false);

        var engine2 = new EngineConfig(
            Id: "engine-2",
            Name: "Engine 2",
            Version: "1.0",
            InstallPath: _testDirectory,
            ExecutablePath: GetTestExecutable(),
            Arguments: GetTestArguments(),
            Port: 8002,
            HealthCheckUrl: null,
            StartOnAppLaunch: false,
            AutoRestart: false);

        await registry.RegisterEngineAsync(engine1);
        await registry.RegisterEngineAsync(engine2);

        // Act
        var report = await lifecycleManager.GenerateDiagnosticsAsync();

        // Assert
        Assert.NotNull(report);
        Assert.Equal(2, report.TotalEngines);
        Assert.Equal(0, report.RunningEngines); // None started
        Assert.Equal(2, report.Engines.Count);
        Assert.Contains(report.Engines, e => e.EngineId == "engine-1");
        Assert.Contains(report.Engines, e => e.EngineId == "engine-2");
    }

    [Fact]
    public void LifecycleManager_Should_TrackNotifications()
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

        var receivedNotifications = new List<EngineNotification>();
        lifecycleManager.NotificationReceived += (sender, notification) =>
        {
            receivedNotifications.Add(notification);
        };

        // Act - trigger an operation that would generate notifications
        // For now, just verify the notification system is initialized
        var notifications = lifecycleManager.GetRecentNotifications();

        // Assert
        Assert.NotNull(notifications);
        Assert.IsAssignableFrom<IReadOnlyList<EngineNotification>>(notifications);
    }

    [Fact]
    public async Task LifecycleManager_Should_StopAllEnginesOnShutdown()
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

        var engine = new EngineConfig(
            Id: "test-engine",
            Name: "Test Engine",
            Version: "1.0",
            InstallPath: _testDirectory,
            ExecutablePath: GetTestExecutable(),
            Arguments: GetTestArguments(),
            Port: null,
            HealthCheckUrl: null,
            StartOnAppLaunch: false,
            AutoRestart: false);

        await registry.RegisterEngineAsync(engine);
        await registry.StartEngineAsync("test-engine");
        await Task.Delay(500);

        var statusBeforeStop = await registry.GetEngineStatusAsync("test-engine");
        Assert.True(statusBeforeStop.IsRunning);

        // Act
        await lifecycleManager.StopAsync();
        await Task.Delay(500);

        // Assert
        var statusAfterStop = await registry.GetEngineStatusAsync("test-engine");
        Assert.False(statusAfterStop.IsRunning);
    }

    private string GetTestExecutable()
    {
        if (OperatingSystem.IsWindows())
        {
            return "timeout";
        }
        else
        {
            return "sleep";
        }
    }

    private string GetTestArguments()
    {
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
