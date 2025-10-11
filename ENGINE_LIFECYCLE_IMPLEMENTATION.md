# Engine Lifecycle Manager Implementation

## Overview

This implementation adds automatic engine management and background services to Aura Video Studio, allowing engines to automatically start on app launch and gracefully stop on exit.

## Features Implemented

### 1. EngineLifecycleManager (`Aura.Core/Runtime/EngineLifecycleManager.cs`)

A comprehensive lifecycle manager that handles:

- **Auto-Start**: Automatically launches engines marked with `StartOnAppLaunch` on application startup
- **Graceful Shutdown**: Stops all running engines when the application shuts down
- **Crash Detection**: Monitors engine processes and detects when they crash unexpectedly
- **Auto-Restart**: Automatically restarts crashed engines (configurable, up to 3 attempts by default)
- **Health Checking**: Validates engines are running and healthy after startup
- **Notifications**: Event-based notification system for engine lifecycle events
- **Diagnostics**: Generates comprehensive system reports showing engine status

#### Key Methods

```csharp
// Start lifecycle manager and auto-launch engines
Task StartAsync(CancellationToken cancellationToken = default)

// Stop lifecycle manager and gracefully shutdown engines
Task StopAsync()

// Generate diagnostics report
Task<SystemDiagnosticsReport> GenerateDiagnosticsAsync()

// Manually restart an engine
Task<bool> RestartEngineAsync(string engineId, CancellationToken cancellationToken = default)

// Get recent notifications
IReadOnlyList<EngineNotification> GetRecentNotifications(int count = 100)
```

#### Notification Types

- `Started`: Engine successfully started
- `Stopped`: Engine stopped gracefully
- `HealthCheckPassed`: Engine health check succeeded
- `HealthCheckFailed`: Engine running but health check failed
- `Crashed`: Engine crashed unexpectedly
- `Restarting`: Engine being restarted after crash
- `RestartLimitReached`: Maximum restart attempts exceeded
- `Warning`: General warning message

### 2. API Integration (`Aura.Api/Program.cs`)

Lifecycle manager is integrated into the API startup:

```csharp
// Service registration
builder.Services.AddSingleton<EngineLifecycleManager>(sp => ...);

// Start on application launch
lifetime.ApplicationStarted.Register(() => {
    lifecycleManager.StartAsync();
});

// Stop on application shutdown
lifetime.ApplicationStopping.Register(() => {
    lifecycleManager.StopAsync();
});
```

### 3. New API Endpoints (`Aura.Api/Controllers/EnginesController.cs`)

#### GET `/api/engines/diagnostics`
Returns comprehensive system diagnostics:
```json
{
  "generatedAt": "2025-10-11T16:54:38Z",
  "totalEngines": 3,
  "runningEngines": 2,
  "healthyEngines": 1,
  "engines": [
    {
      "engineId": "stable-diffusion",
      "name": "Stable Diffusion",
      "isRunning": true,
      "isHealthy": true,
      "lastStarted": "2025-10-11T16:50:00Z",
      "restartCount": 0,
      "processId": 12345,
      "port": 7860
    }
  ]
}
```

#### GET `/api/engines/logs?engineId={id}&tailLines={count}`
Returns recent logs for a specific engine:
```json
{
  "logs": "[OUT] 2025-10-11 16:54:38 Engine started...\n[OUT] 2025-10-11 16:54:39 Loading model..."
}
```

#### GET `/api/engines/notifications?count={count}`
Returns recent notifications:
```json
{
  "notifications": [
    {
      "engineId": "stable-diffusion",
      "engineName": "Stable Diffusion",
      "type": "Started",
      "message": "Stable Diffusion started successfully on port 7860",
      "timestamp": "2025-10-11T16:50:00Z"
    }
  ]
}
```

#### POST `/api/engines/restart`
Manually restart an engine:
```json
{
  "engineId": "stable-diffusion"
}
```

### 4. UI Enhancements (`Aura.Web/src/components/Settings/LocalEngines.tsx`)

Enhanced the Local Engines settings page with:

- **Run Diagnostics Button**: Displays comprehensive system diagnostics in a dialog
- **View Logs Button**: Shows recent engine logs for running engines
- **Improved Status Display**: Better visual feedback for engine states
- **Dialog Components**: Modal dialogs for diagnostics and logs with formatted display

#### UI Features

1. **Diagnostics Dialog**
   - Shows total engines, running engines, and healthy engines
   - Displays detailed JSON view of all engine statuses
   - Easy to read and troubleshoot

2. **Logs Dialog**
   - Monospace font for better log readability
   - Scrollable view for long logs
   - Shows last 500 lines by default

3. **Enhanced Status Badges**
   - Color-coded status indicators
   - Clear icons for different states
   - Visual feedback for health status

### 5. Testing

Comprehensive test coverage including:

#### Unit Tests (`Aura.Tests/EngineLifecycleManagerTests.cs`)
- Auto-launch functionality
- Graceful shutdown
- Diagnostics generation
- Restart functionality
- Notification system

#### Integration Tests (`Aura.Tests/EngineCrashRestartTests.cs`)
- Process tracking
- Start/stop operations
- Diagnostics generation
- Notification tracking
- Shutdown handling

All tests pass successfully on Linux.

## Configuration

Engines are configured via the `LocalEnginesRegistry` with these properties:

```csharp
public record EngineConfig(
    string Id,
    string Name,
    string Version,
    string InstallPath,
    string? ExecutablePath,
    string? Arguments,
    int? Port,
    string? HealthCheckUrl,
    bool StartOnAppLaunch,     // Enable auto-start
    bool AutoRestart,           // Enable auto-restart on crash
    IDictionary<string, string>? EnvironmentVariables = null
);
```

## Usage Examples

### Enable Auto-Start for Stable Diffusion

```csharp
var engine = new EngineConfig(
    Id: "stable-diffusion",
    Name: "Stable Diffusion WebUI",
    Version: "1.0",
    InstallPath: "/path/to/sd",
    ExecutablePath: "/path/to/sd/webui.sh",
    Arguments: "--port 7860",
    Port: 7860,
    HealthCheckUrl: "http://localhost:7860",
    StartOnAppLaunch: true,    // Auto-start enabled
    AutoRestart: true           // Auto-restart on crash
);

await registry.RegisterEngineAsync(engine);
```

### Subscribe to Notifications

```csharp
lifecycleManager.NotificationReceived += (sender, notification) =>
{
    Console.WriteLine($"[{notification.Type}] {notification.EngineName}: {notification.Message}");
};
```

### Generate Diagnostics Report

```csharp
var report = await lifecycleManager.GenerateDiagnosticsAsync();
Console.WriteLine($"System Status: {report.RunningEngines}/{report.TotalEngines} engines running");
foreach (var engine in report.Engines)
{
    Console.WriteLine($"  {engine.Name}: {(engine.IsRunning ? "Running" : "Stopped")}");
}
```

## Architecture

```
┌─────────────────────────────────────────┐
│        Application Startup              │
│  (Aura.Api/Program.cs)                  │
└────────────────┬────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────┐
│   EngineLifecycleManager                │
│   - StartAsync()                        │
│   - Monitoring Task                     │
│   - Event Handling                      │
└────────┬───────────────┬────────────────┘
         │               │
         ▼               ▼
┌────────────────┐  ┌───────────────────┐
│ LocalEngines   │  │ ExternalProcess   │
│ Registry       │  │ Manager           │
│ - Configs      │  │ - Processes       │
│ - Status       │  │ - Logs            │
└────────────────┘  └───────────────────┘
```

## Safety Features

1. **Restart Limits**: Prevents infinite restart loops (max 3 attempts by default)
2. **Graceful Shutdown**: Uses SIGTERM first, then SIGKILL if needed
3. **Error Handling**: Comprehensive error catching and logging
4. **Timeout Protection**: Health checks have configurable timeouts
5. **Resource Cleanup**: Proper disposal of processes and resources

## Performance Considerations

- Monitoring task runs every 5 seconds (configurable)
- Health checks use HTTP client with timeout
- Logs are captured in real-time with rolling files
- Notifications are kept to last 1000 entries to prevent memory issues

## Troubleshooting

### Engine Won't Auto-Start

1. Check `StartOnAppLaunch` is set to `true`
2. Verify executable path is correct and accessible
3. Check logs via `/api/engines/logs` endpoint
4. Run diagnostics via `/api/engines/diagnostics`

### Engine Keeps Crashing

1. Check restart count in diagnostics report
2. Review error logs for crash reasons
3. Verify system resources (memory, disk space)
4. Check health check URL is correct
5. Review engine-specific logs in the logs directory

### Health Check Failing

1. Verify health check URL is reachable
2. Check port is not blocked by firewall
3. Ensure engine has fully started
4. Review engine startup logs

## Compliance with Requirements

✅ **Auto-start on app launch**: Engines with `StartOnAppLaunch=true` start automatically
✅ **Graceful shutdown**: All engines stopped with SIGTERM, then SIGKILL if needed
✅ **Crash/restart handling**: Up to 3 automatic restarts with exponential backoff
✅ **Diagnostics**: Comprehensive system report with all engine details
✅ **Notification system**: Event-based notifications for all lifecycle events
✅ **Settings UI**: Enhanced LocalEngines page with logs and diagnostics
✅ **Health checks**: HTTP polling with timeout handling
✅ **Testing**: Unit and integration tests covering all major scenarios

## Conclusion

The Engine Lifecycle Manager provides robust, production-ready engine management with automatic startup, graceful shutdown, crash recovery, and comprehensive monitoring. The implementation follows best practices for resource management, error handling, and user experience.
