> **⚠️ ARCHIVED DOCUMENT**  
> This document is retained for historical context only.  
> It may contain outdated information, references to superseded implementations, or details specific to past pull requests.  
> For current documentation, see the main docs/ directory and canonical guides in the repository root.

---


# Ollama Process Control and Detection

## Overview

Aura Video Studio provides comprehensive process control for Ollama, enabling users to start, stop, and monitor the Ollama service directly from the application interface. This feature is particularly useful for ensuring Ollama is available during video generation workflows that use local LLM models.

## Features

### Backend Capabilities

1. **Process Management** (Windows-first)
   - Start Ollama server process with automatic path detection
   - Stop managed Ollama processes gracefully
   - Monitor process status and PID
   - Detect externally-started Ollama instances

2. **Status Detection**
   - Check if Ollama is running on port 11434
   - Identify process ID (PID)
   - Distinguish between app-managed and external processes
   - Report errors with actionable messages

3. **Log Management**
   - Capture stdout/stderr to rolling log files
   - Retrieve recent log entries for debugging
   - Logs stored in `AuraData/logs/ollama/`

4. **Model Verification**
   - List installed Ollama models
   - Verify model availability before generation

### Frontend Integration

1. **Preflight Panel**
   - "Start Ollama" button integrated into preflight checks
   - Real-time status updates
   - Loading states during operations
   - Success/error notifications

2. **Path Configuration**
   - Automatic path detection for common installation locations
   - Manual path configuration in Settings
   - Validation of executable paths

## API Endpoints

### GET /api/ollama/status

Check Ollama service status.

**Response:**
```json
{
  "running": true,
  "pid": 12345,
  "managedByApp": true,
  "model": null,
  "error": null
}
```

**Fields:**
- `running` (boolean): Whether Ollama is currently running
- `pid` (number?): Process ID if running
- `managedByApp` (boolean): True if started by this application
- `model` (string?): Currently loaded model (future enhancement)
- `error` (string?): Error message if status check failed

### POST /api/ollama/start

Start Ollama server process (Windows only).

**Response:**
```json
{
  "success": true,
  "message": "Ollama started successfully",
  "pid": 12345
}
```

**Error Responses:**
- 400: Path not configured or invalid
- 500: Start operation failed

### POST /api/ollama/stop

Stop Ollama server process (only if started by app).

**Response:**
```json
{
  "success": true,
  "message": "Ollama stopped successfully"
}
```

### GET /api/ollama/logs

Retrieve recent Ollama log entries.

**Query Parameters:**
- `maxLines` (number, optional): Maximum lines to return (default: 200)

**Response:**
```json
{
  "logs": [
    "[OUT] 2024-11-01 12:00:00 Listening on 127.0.0.1:11434",
    "[OUT] 2024-11-01 12:00:01 Model loaded: llama3.1:8b-q4_k_m"
  ],
  "totalLines": 2
}
```

### GET /api/ollama/models

List installed Ollama models.

**Response:**
```json
{
  "models": [
    {
      "name": "llama3.1:8b-q4_k_m",
      "size": "4.7 GB",
      "modifiedAt": "2024-11-01T10:30:00Z"
    }
  ],
  "count": 1
}
```

## Configuration

### Backend Configuration

#### Provider Settings (settings.json)

Located in `AuraData/settings.json`:

```json
{
  "ollamaUrl": "http://127.0.0.1:11434",
  "ollamaModel": "llama3.1:8b-q4_k_m",
  "ollamaExecutablePath": "C:\\Program Files\\Ollama\\ollama.exe"
}
```

#### Automatic Path Detection

If `ollamaExecutablePath` is not configured, the system will search these locations on Windows:

1. `%ProgramFiles%\Ollama\ollama.exe`
2. `%LOCALAPPDATA%\Programs\Ollama\ollama.exe`
3. `%LOCALAPPDATA%\Ollama\ollama.exe`

#### Log File Location

Ollama logs are stored in:
- **Path**: `AuraData/logs/ollama/ollama-{timestamp}.log`
- **Format**: `[OUT/ERR] {timestamp} {message}`
- **Rotation**: New file per start operation

### Frontend Usage

#### Preflight Panel Integration

When Ollama is not running and a preflight check includes LLM requirements:

1. Preflight shows "Start" button for Ollama
2. Click button to start Ollama
3. Wait for readiness (spinner shows progress)
4. Toast notification confirms success/failure
5. Preflight re-runs automatically after successful start

#### Error Handling

Common error scenarios and their handling:

**Path Not Found:**
```
Title: "Ollama path not configured"
Message: "Please configure the Ollama executable path in Settings → Providers → Ollama"
Action: Navigate to Settings
```

**Permission Issues:**
```
Title: "Failed to Start Ollama"
Message: "Access denied. Please run as administrator or check file permissions."
```

**Port Already in Use:**
```
Title: "Ollama Already Running"
Message: "Ollama is already running externally. Use the existing instance."
```

## Platform Support

### Windows
- ✅ Full support: Start, Stop, Status, Logs
- ✅ Automatic path detection
- ✅ Process management with PID tracking

### Linux/macOS
- ✅ Status detection (port ping)
- ✅ Log retrieval (if logs exist)
- ✅ Model listing
- ❌ Start/Stop operations (manual start required)
- 💡 Shows instructions for manual start

## Implementation Details

### Process Lifecycle

1. **Start:**
   ```
   User clicks "Start Ollama"
     → Frontend calls POST /api/ollama/start
     → Backend spawns process: ollama.exe serve
     → Captures stdout/stderr to log file
     → Waits for port 11434 readiness (10s timeout)
     → Returns success/failure
     → Frontend shows toast notification
   ```

2. **Stop:**
   ```
   User clicks "Stop Ollama" (only if managed by app)
     → Frontend calls POST /api/ollama/stop
     → Backend kills process tree
     → Waits for exit (5s timeout)
     → Returns success/failure
   ```

3. **Status Monitoring:**
   ```
   Periodic or on-demand
     → GET /api/ollama/status
     → HTTP GET to http://127.0.0.1:11434/api/tags
     → If successful → Running
     → If connection refused → Not running
     → If timeout → Connection issues
   ```

### Security Considerations

1. **PID Tracking**: Only processes started by the app can be stopped
2. **Path Validation**: Executable paths are validated before execution
3. **Log Isolation**: Logs stored in application-controlled directory
4. **No Credential Storage**: No API keys or sensitive data in logs

## Troubleshooting

### Ollama Won't Start

**Symptoms:**
- "Failed to start" error
- Process starts but not ready within timeout

**Solutions:**
1. Verify Ollama is installed correctly
2. Check if port 11434 is already in use: `netstat -an | findstr 11434`
3. Review logs at `AuraData/logs/ollama/`
4. Try starting manually from command line: `ollama serve`
5. Check Windows Firewall settings

### Ollama Status Shows "Not Running" but It's Running

**Symptoms:**
- Status check fails but Ollama is actually running
- Can't connect to http://127.0.0.1:11434

**Solutions:**
1. Verify Ollama URL in settings matches actual port
2. Check firewall rules
3. Try accessing http://127.0.0.1:11434/api/tags in browser
4. Review network adapter settings

### Can't Stop Ollama

**Symptoms:**
- "Stop" button disabled or fails
- Error: "No managed Ollama process running"

**Reason:**
- Ollama was started externally (not by the app)

**Solutions:**
- Stop Ollama manually from Task Manager or command line
- The app only stops processes it started for safety

### Missing Logs

**Symptoms:**
- GET /api/ollama/logs returns empty array

**Reason:**
- No Ollama instances started by the app yet

**Solutions:**
- Logs only capture output from app-started Ollama instances
- For external Ollama, check system logs or Ollama's own logs

## Future Enhancements

Planned features for future releases:

1. **Model Management**
   - Pull models directly from app
   - Delete unused models
   - Show model download progress

2. **Advanced Monitoring**
   - Real-time performance metrics
   - Memory/CPU usage tracking
   - Request latency monitoring

3. **Multi-Instance Support**
   - Run multiple Ollama instances on different ports
   - Load balancing across instances

4. **Cross-Platform Start/Stop**
   - Linux systemd integration
   - macOS launchd integration
   - Docker container management

## Related Documentation

- [LLM Provider Integration Guide](../../../LLM_IMPLEMENTATION_GUIDE.md)
- [Provider Settings Configuration](../../../PROVIDER_INTEGRATION_GUIDE.md)
- [Health Diagnostics](../root-summaries/HEALTH_DIAGNOSTICS_IMPLEMENTATION.md)
- [Preflight Checks](../../../PRODUCTION_READINESS_CHECKLIST.md)

## Support

For issues or questions:
1. Check logs at `AuraData/logs/ollama/`
2. Review [GitHub Issues](https://github.com/itsacoffee/aura-video-studio/issues)
3. Consult [Ollama Documentation](https://ollama.ai/docs)
