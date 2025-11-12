# Comprehensive Application Startup Logging

This document describes the comprehensive startup logging system implemented for Aura Video Studio Electron Desktop application.

## Overview

The startup logging system provides detailed, structured JSON logging of every initialization step during application startup. It includes performance tracking, diagnostics, log rotation, and comprehensive error reporting with full stack traces.

## Features

### 1. Structured JSON Logging
- Every log entry is a structured JSON object with:
  - `level`: Log level (debug, info, warn, error)
  - `timestamp`: ISO 8601 timestamp
  - `component`: Component name (e.g., 'AppConfig', 'BackendService')
  - `message`: Human-readable message
  - `metadata`: Additional context data

### 2. Timestamped Start/End Tracking
- Every initialization function logs:
  - **Start**: When the function begins execution
  - **End**: When the function completes (with success/failure status)
  - **Duration**: Time taken to complete (in milliseconds)

### 3. Performance Monitoring
- Automatically logs warnings for steps taking >2 seconds
- Tracks total startup time
- Identifies slow initialization steps in summary

### 4. Persistent Disk Logging
- Logs written to: `userData/logs/startup-{timestamp}.log`
- Summary file: `userData/logs/startup-summary-{timestamp}.json`
- Created **BEFORE** any other initialization

### 5. Log Rotation
- Automatically keeps only the last 10 startup logs
- Older logs are automatically deleted on startup
- Corresponding summary files are also cleaned up

### 6. Detailed Error Context
- Full stack traces for all errors
- Error metadata (component, timestamp, context)
- No generic "initialization failed" messages
- Structured error objects in JSON format

### 7. Startup Diagnostics
- Comprehensive system health checks:
  - Platform and Node.js version
  - Memory availability
  - Disk space
  - Directory accessibility
  - FFmpeg availability
  - .NET runtime availability
  - Port availability
  - Write permissions
- Health check results included in logs
- Warnings and errors clearly identified

### 8. Debug Startup Mode
- Enable with `--debug-startup` flag
- Increases log verbosity (includes debug-level logs)
- Keeps Electron DevTools open after startup
- Useful for troubleshooting startup issues

### 9. Summary Generation
- JSON summary file with:
  - Total startup duration
  - List of all steps with timings
  - Success/failure status for each step
  - All errors encountered
  - Slow steps (>2 seconds)
  - System information
  - Overall success status

### 10. IPC API for Log Access
- Frontend can access logs via `window.electron.startupLogs`:
  - `getLatest()`: Get current startup log info
  - `getSummary()`: Get parsed summary JSON
  - `getLogContent()`: Get all log entries
  - `list()`: List all startup logs
  - `readFile(path)`: Read specific log file
  - `openDirectory()`: Open logs folder in file explorer

## Usage

### Command Line Flags

```bash
# Normal startup
npm start

# Development mode
npm run dev

# Debug startup mode (verbose logging + DevTools)
electron . --debug-startup
```

### Example Log Entry

```json
{
  "level": "info",
  "timestamp": "2025-11-12T17:30:15.234Z",
  "component": "BackendService",
  "message": "Starting backend service",
  "metadata": {
    "step": "backend-service",
    "startTime": "2025-11-12T17:30:15.234Z"
  }
}
```

### Example Summary File

```json
{
  "startTime": "2025-11-12T17:30:10.000Z",
  "endTime": "2025-11-12T17:30:25.500Z",
  "totalDuration": "15500ms",
  "totalDurationSeconds": "15.50",
  "success": true,
  "statistics": {
    "totalSteps": 12,
    "successfulSteps": 12,
    "failedSteps": 0,
    "totalErrors": 0,
    "totalLogEntries": 48
  },
  "steps": [
    {
      "name": "app-config",
      "component": "AppConfig",
      "description": "Initializing application configuration",
      "duration": "145ms",
      "success": true,
      "startTime": "2025-11-12T17:30:10.100Z",
      "endTime": "2025-11-12T17:30:10.245Z"
    }
  ],
  "errors": [],
  "performance": {
    "slowSteps": [
      {
        "name": "backend-service",
        "component": "BackendService",
        "duration": "3200ms"
      }
    ]
  },
  "system": {
    "platform": "win32",
    "arch": "x64",
    "nodeVersion": "18.18.0",
    "electronVersion": "32.2.5",
    "appVersion": "1.0.0",
    "debugMode": false
  }
}
```

## Implementation Details

### Core Components

#### 1. StartupLogger (`electron/startup-logger.js`)
Main logging class that provides:
- Structured logging methods (info, warn, error, debug)
- Step tracking (stepStart, stepEnd)
- Async/sync function tracking (trackAsync, trackSync)
- Log rotation
- Summary generation

#### 2. StartupDiagnostics (`electron/startup-diagnostics.js`)
System health check utilities:
- Platform detection
- Resource availability checks
- Dependency verification
- Warning/error collection

#### 3. StartupLogsHandler (`electron/ipc-handlers/startup-logs-handler.js`)
IPC handler for frontend access:
- Log retrieval
- Summary parsing
- File listing
- Directory access

#### 4. Main Process Integration (`electron/main.js`)
- Logger initialized FIRST in `app.whenReady()`
- All initialization functions wrapped with tracking
- Error handlers updated to use structured logging
- Summary finalized when main window ready

#### 5. Preload Script (`electron/preload.js`)
- Exposes safe IPC API to renderer
- `window.electron.startupLogs` namespace
- Validated channels for security

## Log File Locations

### Windows
```
C:\Users\{Username}\AppData\Roaming\aura-video-studio\logs\
  startup-2025-11-12T17-30-10-123Z.log
  startup-summary-2025-11-12T17-30-10-123Z.json
```

### macOS
```
~/Library/Application Support/aura-video-studio/logs/
  startup-2025-11-12T17-30-10-123Z.log
  startup-summary-2025-11-12T17-30-10-123Z.json
```

### Linux
```
~/.config/aura-video-studio/logs/
  startup-2025-11-12T17-30-10-123Z.log
  startup-summary-2025-11-12T17-30-10-123Z.json
```

## Accessing Logs from Frontend

```javascript
// Get latest startup summary
const result = await window.electron.startupLogs.getSummary();
if (result.success) {
  console.log('Startup took:', result.summary.totalDurationSeconds, 'seconds');
  console.log('Success:', result.summary.success);
  console.log('Failed steps:', result.summary.statistics.failedSteps);
}

// List all startup logs
const logs = await window.electron.startupLogs.list();
if (logs.success) {
  console.log('Available logs:', logs.logs.length);
  logs.logs.forEach(log => {
    console.log(`${log.name} - ${log.size} bytes - ${log.modified}`);
  });
}

// Open logs directory
await window.electron.startupLogs.openDirectory();
```

## Troubleshooting

### Viewing Logs

1. **From Application Menu**: Help > View Logs
2. **Using IPC**: Call `window.electron.startupLogs.openDirectory()`
3. **Manually**: Navigate to userData/logs folder (path shown in console on startup)

### Debug Mode

Run with `--debug-startup` flag for maximum verbosity:
```bash
electron . --debug-startup
```

This will:
- Enable debug-level logging
- Keep DevTools open
- Show all internal operations
- Help diagnose startup issues

### Common Issues

**Backend fails to start**: Check the backend-service step in the summary for timing and errors.

**Slow startup**: Check the `performance.slowSteps` array in summary to identify bottlenecks.

**Missing logs**: Verify write permissions to userData directory in diagnostics.

## Performance Benchmarks

Typical startup times (on recommended hardware):
- **Diagnostics**: 200-500ms
- **App Config**: 50-150ms
- **Window Manager**: 100-200ms
- **Backend Service**: 2000-4000ms (depends on .NET runtime)
- **IPC Handlers**: 50-100ms
- **Total**: 3000-6000ms (3-6 seconds)

Steps taking >2 seconds will trigger performance warnings in the log.

## Security Considerations

- All IPC channels are validated via whitelist
- Log file paths are validated before access
- No sensitive data (API keys, passwords) logged
- Logs stored in user's private userData directory
- Context isolation and sandboxing enabled

## Future Enhancements

Possible additions for future versions:
- Log compression for older files
- Configurable log retention period
- Remote log upload for support
- Real-time startup progress UI
- Startup performance comparison over time
- Automated bottleneck detection

## Related Files

- `electron/startup-logger.js` - Core logging implementation
- `electron/startup-diagnostics.js` - System health checks
- `electron/ipc-handlers/startup-logs-handler.js` - IPC API
- `electron/main.js` - Integration and usage
- `electron/preload.js` - Frontend API exposure

## Version History

- **v1.0.0** (2025-11-12): Initial implementation
  - Structured JSON logging
  - Performance tracking
  - Log rotation
  - Diagnostics
  - Debug mode
  - IPC API
