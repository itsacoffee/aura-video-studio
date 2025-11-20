# Process Model - Aura Video Studio

This document describes the complete process architecture for Aura Video Studio, including all process spawning locations, parent/child relationships, and shutdown responsibilities.

## Process Hierarchy

```
Electron Main Process (Aura.Desktop)
├── Electron Renderer Process(es) (UI)
├── Backend Process (Aura.Api.exe / dotnet Aura.Api.dll)
│   ├── FFmpeg Process(es) (video encoding, format conversion)
│   ├── Worker Thread Pool (async job execution)
│   └── HTTP Server (Kestrel)
└── System Tray Icon
```

## 1. Electron Main Process (PID: varies)

**Location**: `Aura.Desktop/electron/main.js`

**Responsibilities**:
- Application lifecycle management
- Window creation and management
- Backend process spawning and supervision
- System tray management
- IPC communication with renderer

**Spawned By**: User launching the application

**Managed By**: Operating system

**Shutdown Responsibility**: Self (coordinates shutdown of all children)

## 2. Electron Renderer Process(es)

**Location**: `Aura.Web` (React application)

**Responsibilities**:
- UI rendering
- User interaction handling
- API calls to backend

**Spawned By**: Electron main process (BrowserWindow creation)

**Managed By**: Electron main process

**Shutdown Responsibility**: Electron main process (automatically terminated when windows close)

## 3. Backend Process (Aura.Api)

**Location**: `Aura.Api/Program.cs`

**Executable**:
- **Development**: `dotnet run` or `Aura.Api.dll`
- **Production**: Self-contained executable in `resources/backend/{platform}/Aura.Api.exe`

**Responsibilities**:
- REST API server (Kestrel on configured port)
- Video generation orchestration
- FFmpeg process management
- Database operations
- Provider communication (LLM, TTS, etc.)

**Spawned By**: 
- **Location**: `Aura.Desktop/electron/backend-service.js` line 109
- **Method**: `child_process.spawn()`
- **Arguments**: None (configuration via environment variables)
- **Options**: `{ stdio: 'pipe', windowsHide: true, detached: false }`

**Process Tracking**:
- PID stored in `backendService.pid`
- Registered with Electron ProcessManager if available
- Health checked via HTTP `/health/live` endpoint

**Managed By**: 
- Electron's BackendService class
- Registered with Electron's ProcessManager

**Shutdown Responsibility**:
1. Electron ShutdownOrchestrator signals via `POST /api/system/shutdown`
2. Backend graceful shutdown (2 second timeout)
3. Electron BackendService terminates process (force if needed)
4. ProcessManager ensures complete termination

## 4. FFmpeg Processes

**Location**: Spawned by `Aura.Core/Services/FFmpeg/*`

**Executable**:
- **Development**: From `Aura.Desktop/resources/ffmpeg/{platform}/bin/ffmpeg.exe`
- **Production**: From `resources/ffmpeg/{platform}/bin/ffmpeg.exe`
- **System**: May use system FFmpeg if configured

**Responsibilities**:
- Video encoding and transcoding
- Audio processing
- Format conversion
- Frame extraction
- Thumbnail generation

**Spawned By**:
- **Locations**:
  - `Aura.Core/Services/FFmpeg/FFmpegExecutor.cs`
  - `Aura.Core/Audio/AudioFormatConverter.cs`
  - `Aura.Providers/Video/FfmpegVideoComposer.cs`
- **Method**: `System.Diagnostics.Process.Start()`
- **Typical Arguments**: Complex FFmpeg command lines for video operations
- **Options**: `{ RedirectStandardOutput: true, RedirectStandardError: true, UseShellExecute: false }`

**Process Tracking**:
- PIDs tracked by backend's `IProcessManager` (registered on spawn)
- Timeout enforcement (default 60 minutes per process)
- Periodic cleanup sweep every 15 minutes

**Managed By**: 
- Backend's FFmpeg IProcessManager (`Aura.Core/Services/FFmpeg/ProcessManager.cs`)
- Tracked in `ConcurrentDictionary<int, ProcessInfo>`

**Shutdown Responsibility**:
1. Backend's IProcessManager on application shutdown
2. Force kill on timeout
3. Cleanup timer detects and removes orphans

## 5. Worker Thread Pool

**Location**: ASP.NET Core hosted services

**Responsibilities**:
- Background job queue processing
- Scheduled tasks (cleanup, health checks, etc.)
- Long-running operations

**Spawned By**: ASP.NET Core host on startup

**Managed By**: ASP.NET Core IHostedService infrastructure

**Shutdown Responsibility**: ASP.NET Core host (via IHostApplicationLifetime)

## Process Spawning Summary

### Electron Desktop (JavaScript/Node.js)

| File | Line(s) | Process Type | Purpose |
|------|---------|--------------|---------|
| `electron/backend-service.js` | 109 | Backend | Spawn Aura.Api backend server |
| `electron/main.js` | 998 | Renderer | Create main application window |

### Backend (C#/.NET)

| File | Purpose | Process Count |
|------|---------|---------------|
| `Aura.Core/Services/FFmpeg/FFmpegExecutor.cs` | Video encoding | 0-N per job |
| `Aura.Core/Audio/AudioFormatConverter.cs` | Audio conversion | 0-N as needed |
| `Aura.Providers/Video/FfmpegVideoComposer.cs` | Video composition | 1 per render |
| `Aura.Core/Services/Setup/DependencyInstaller.cs` | FFmpeg installation | 1 during setup |

### Total Process Budget

**Minimum** (idle): 3 processes
- 1 Electron main process
- 1 Electron renderer process  
- 1 Backend process

**Typical** (active job): 5-7 processes
- 3 base processes
- 2-4 FFmpeg processes (encoding, audio, thumbnails)

**Maximum** (multiple jobs): 10-15 processes
- 3 base processes
- Up to 12 FFmpeg processes (multiple concurrent jobs)

## Shutdown Sequence

### Normal Exit Flow

1. **User Action**: Click "Quit" or close window
2. **Electron Main Process** (`main.js` line 1379):
   - Event: `before-quit`
   - Prevents default to run cleanup
   - Calls `shutdownOrchestrator.initiateShutdown()`
3. **Shutdown Orchestrator** (`shutdown-orchestrator.js`):
   - Check for active renders (unless skipped)
   - Signal backend shutdown via API
   - Stop backend service (graceful → force)
   - Terminate all tracked processes via ProcessManager
   - Cleanup temp files
4. **Backend Service** (`backend-service.js` line 149):
   - Graceful shutdown attempt via `/api/system/shutdown`
   - 2 second timeout
   - Force termination using `taskkill /F /T` (Windows) or `SIGKILL` (Unix)
5. **Backend Process** (`Program.cs` line 4899):
   - Event: `ApplicationStopping`
   - Kills all tracked FFmpeg processes
   - Stops hosted services
   - Flushes logs
6. **Process Manager** (both Electron and Backend):
   - Terminates all registered child processes
   - Verifies process exit
   - Cleans up registrations

### Timeout Handling

- **Graceful Timeout**: 2 seconds for backend API shutdown
- **Force Kill Timeout**: 1 second after graceful
- **Absolute Timeout**: 5 seconds total (hard limit in main.js)
- **Failsafe**: If absolute timeout exceeded, kill all Aura processes by name

### Force Shutdown

If graceful shutdown fails:

1. `taskkill /F /T` for backend PID (Windows)
2. `process.kill(-pid, 'SIGKILL')` for process group (Unix)
3. Failsafe: Kill all processes matching "Aura.Api.exe" and "ffmpeg.exe" (Windows) or in our process group (Unix)

## Orphan Process Detection

### On Startup

**Backend Port Check** (`backend-service.js` line 67):
- Before starting backend, check if port is already in use
- If occupied, attempt to identify if it's an orphaned backend
- Log diagnostic info
- Fail fast with clear error message

### During Operation

**FFmpeg ProcessManager Cleanup** (`ProcessManager.cs` line 149):
- Periodic sweep every 15 minutes
- Kill processes exceeding 60-minute timeout
- Remove registrations for exited processes

## Single Instance Enforcement

**Electron Single Instance Lock** (`main.js` line 150):
- Uses `app.requestSingleInstanceLock()`
- Second instance focuses existing window instead of starting new backend
- Prevents port conflicts and duplicate backends

## Diagnostics

### Process Tracking Logs

**Electron Side**:
- Backend PID logged on startup
- Process registration events in ProcessManager
- Shutdown steps logged with timing

**Backend Side**:
- FFmpeg PID registration
- Process timeout warnings
- Cleanup sweep results
- Shutdown lifecycle events

### Health Checks

- Backend health: `GET /health/live` (HTTP server running)
- Backend readiness: `GET /health/ready` (database + dependencies)
- FFmpeg detection: Checked via FFmpegResolver on demand

## Known Issues and Mitigations

### Issue: Lingering Processes After Crash

**Symptom**: Aura.Api.exe and ffmpeg.exe remain in Task Manager after crash

**Root Cause**: 
- No crash recovery handler
- Process tree not terminated on unhandled exception

**Mitigation** (Implemented in this PR):
- Enhanced failsafe in ShutdownOrchestrator
- Backend `ApplicationStopping` event handler kills FFmpeg processes
- Startup checks for orphaned processes

### Issue: Port Already in Use

**Symptom**: Backend fails to start with "address already in use"

**Root Cause**: Previous backend instance still running

**Mitigation**:
- Single instance lock in Electron
- Port availability check before spawning backend
- Clear error messages with recovery steps

### Issue: Zombie FFmpeg Processes

**Symptom**: FFmpeg processes outlive parent backend process

**Root Cause**: 
- Process not registered with ProcessManager
- Timeout not enforced
- Shutdown sequence not killing children

**Mitigation**:
- All FFmpeg spawns registered with IProcessManager
- Timeout enforcement (60 minutes default)
- Shutdown handler kills all registered processes
- Periodic cleanup sweep removes orphans

## Future Improvements

1. **Process Tree Visualization**: Add UI to show active processes
2. **Resource Monitoring**: Track CPU/memory per process
3. **Process Limits**: Enforce max concurrent FFmpeg processes
4. **Graceful Degradation**: Continue operation if some processes fail
5. **Crash Recovery**: Auto-restart backend on crash (with backoff)

## References

- [Electron Process Model](https://www.electronjs.org/docs/latest/tutorial/process-model)
- [ASP.NET Core Hosting](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host)
- [FFmpeg Documentation](https://ffmpeg.org/documentation.html)
- [Node.js Child Process](https://nodejs.org/api/child_process.html)
- [.NET Process Class](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process)
