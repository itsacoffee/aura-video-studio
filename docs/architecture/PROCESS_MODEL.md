# Aura Video Studio - Process Model and Shutdown

This document describes the process architecture and shutdown behavior for the Aura Video Studio desktop application.

## Process Architecture

### Process Hierarchy

```
Electron Main Process (root)
├── Aura.Api Backend (.NET 8)
│   └── Child Processes (FFmpeg, workers)
└── Renderer Process (React UI)
```

### Process Roles

#### 1. **Electron Main Process** (Root)
- **Executable**: `Aura Video Studio.exe` (or platform equivalent)
- **PID**: Parent of all application processes
- **Responsibility**: 
  - Application lifecycle management
  - Process spawning and tracking
  - Window management
  - Shutdown coordination
- **Managed By**: Operating system (user launches this)

#### 2. **Backend API Process** (Child)
- **Executable**: `Aura.Api.exe` (.NET 8)
- **PID**: Tracked by Electron main process
- **Responsibility**:
  - REST API server (ASP.NET Core)
  - Job orchestration
  - Provider integration (LLM, TTS, etc.)
  - FFmpeg process spawning
- **Managed By**: BackendService (electron/backend-service.js)
- **Startup**: Automatically spawned on app launch
- **Port**: Dynamic allocation (finds available port)
- **Expected Lifetime**: Same as main process

#### 3. **Worker Processes** (Grandchildren)
- **Executables**: `ffmpeg.exe`, `ffprobe.exe`, etc.
- **PIDs**: Tracked by ProcessManager
- **Responsibility**:
  - Video rendering (FFmpeg)
  - Media analysis (FFprobe)
  - Compute-intensive tasks
- **Managed By**: Backend API and ProcessManager
- **Startup**: On-demand when jobs require them
- **Expected Lifetime**: Duration of specific jobs

#### 4. **Renderer Process** (Electron Child)
- **Responsibility**: UI rendering (React/TypeScript)
- **Managed By**: Electron framework
- **Communication**: IPC with main process

## Shutdown Model

### User-Initiated Shutdown Triggers

1. **Window Close Button** (X)
   - Default: Exits application completely
   - With `minimizeToTray: true`: Hides to system tray (background mode)
   
2. **File → Exit** (Menu)
   - Always exits application completely
   
3. **System Tray → Quit** (Right-click)
   - Always exits application completely
   
4. **Alt+F4** (Windows keyboard shortcut)
   - Default: Exits application completely
   - With `minimizeToTray: true`: Hides to system tray
   
5. **OS Shutdown/Logoff**
   - Best-effort graceful shutdown
   - Force-killed if exceeds timeout

### Shutdown Sequence

When the user exits Aura Video Studio, the following sequence executes:

```
1. User Action
   ↓
2. ShutdownOrchestrator.initiateShutdown()
   ↓
3. Check Active Renders
   ├─ None: Proceed immediately
   └─ Active: Prompt user (Cancel/Wait/Force)
   ↓
4. Close Windows
   ├─ Main window
   ├─ Splash window
   └─ System tray
   ↓
5. Signal Backend Shutdown
   └─ POST /api/system/shutdown
      ├─ Notify SSE clients
      └─ Cancel in-flight jobs
   ↓
6. Stop Backend Process
   ├─ Graceful (2s timeout)
   │  └─ Wait for process.exit
   └─ Force (1s additional)
      └─ taskkill /F /T (Windows) or SIGKILL (Unix)
   ↓
7. Terminate Child Processes
   └─ ProcessManager.terminateAll()
      ├─ Graceful (SIGTERM) with timeout
      └─ Force (SIGKILL) if needed
   ↓
8. Cleanup Temp Files
   └─ Remove {temp}/aura-video-studio/
   ↓
9. Exit (code 0)
```

### Timeouts

| Step | Graceful Timeout | Force Timeout | Total Max |
|------|-----------------|---------------|-----------|
| Backend API | 2s | +1s | 3s |
| Child Processes | 3s | +2s | 5s |
| Overall Shutdown | - | - | 10s |

### Shutdown Behaviors

#### Normal Shutdown (No Active Jobs)
- **Duration**: 1-3 seconds
- **Process**:
  1. Close UI windows
  2. Stop backend API
  3. Clean up temp files
  4. Exit cleanly
- **Result**: No lingering processes

#### Graceful Shutdown (Active Renders)
- **Duration**: User dependent (can wait up to 5 minutes)
- **Process**:
  1. Detect active render jobs
  2. Show user dialog:
     - **Cancel Quit**: Abort shutdown, keep working
     - **Wait for Completion**: Wait up to 5 minutes for jobs
     - **Force Quit**: Immediate termination (may lose progress)
  3. If "Wait": Poll jobs until complete, then proceed
  4. If "Force": Skip to force termination
- **Result**: User choice determines behavior

#### Force Shutdown (Timeout Exceeded)
- **Duration**: 3-10 seconds
- **Process**:
  1. Graceful attempts fail or timeout
  2. Windows: `taskkill /F /T /PID <pid>` (process tree)
  3. Unix: `kill -SIGKILL <pid>`
  4. Log force-kill events
- **Result**: All processes terminated (data may be lost)

### Background Mode (Minimize to Tray)

When `minimizeToTray: true` (opt-in):

- Closing main window **hides** it, does NOT exit
- All processes continue running:
  - Backend API stays active
  - Render jobs continue
  - System tray icon visible
- To exit: Right-click tray → Quit
- User is notified: "Application is minimized to the system tray. Click to restore, or right-click to quit."

**Default Value**: `false` (users must explicitly enable)

### Process Tracking

#### ProcessManager

The `ProcessManager` class (electron/process-manager.js) provides centralized tracking:

**Features**:
- Register all spawned child processes
- Track process lifecycle (start time, exit time)
- Platform-specific termination (taskkill on Windows, signals on Unix)
- Diagnostic information (PIDs, uptimes, metadata)
- Graceful → Force escalation

**Registration**:
```javascript
processManager.register('Aura.Api Backend', process, {
  port: 5000,
  isDev: false,
  backendPath: '/path/to/Aura.Api.exe'
});
```

**Termination**:
```javascript
// Terminate all tracked processes
const results = await processManager.terminateAll(timeout);
// Results: { success, terminated, failed, details }
```

#### Backend Process Registration

When BackendService spawns the Aura.Api process:
1. Process is created with `spawn()`
2. PID is stored
3. Process is registered with ProcessManager
4. Exit handler added for cleanup

#### Child Process Registration

When backend spawns FFmpeg or other workers:
1. Backend tracks PIDs internally
2. Processes are registered with ProcessManager via IPC (if available)
3. Cleanup triggered on backend shutdown

## Diagnostics

### Logging

All shutdown activities are logged:

**Electron Logs**: `{userData}/logs/`
- startup-{timestamp}.log
- crash-{timestamp}.log

**Backend Logs**: `{userData}/logs/backend/`

**Process Lifecycle Events**:
```
[INFO] ProcessManager: Process registered { name: 'Aura.Api Backend', pid: 1234 }
[INFO] ProcessManager: Process exited { name: 'Aura.Api Backend', pid: 1234, code: 0, lifetimeMs: 45000 }
[INFO] ShutdownOrchestrator: Terminating 3 tracked child process(es)
[INFO] ShutdownOrchestrator: Graceful shutdown completed in 2843ms
```

### Developer Diagnostics

**Process Count** (Dev Mode):
```javascript
const diagnostics = processManager.getDiagnostics();
console.log('Active processes:', diagnostics.processCount);
console.log('Process list:', diagnostics.processes);
```

**Manual Verification** (Task Manager on Windows):
1. Launch Aura Video Studio
2. Note process PIDs in Task Manager
3. Close application
4. Verify all Aura-related processes are gone within 5-10 seconds

## Troubleshooting

### Lingering Processes After Shutdown

**Symptoms**:
- Multiple `.NET Host` entries in Task Manager
- "AI-Powered Video Generation" processes remain
- Port conflicts on next launch

**Causes**:
1. Graceful shutdown timeout exceeded
2. Process tree not properly terminated (Windows)
3. Zombie processes (Unix)
4. Backend crashed during shutdown

**Solutions**:
1. Check shutdown logs: `{userData}/logs/startup-{timestamp}.log`
2. Look for force-kill events or timeout warnings
3. Manually terminate: `taskkill /F /IM Aura.Api.exe /T` (Windows)
4. Report issue with logs if persists

### Application Won't Exit

**Symptoms**:
- Close button clicked but app remains open
- Processes visible in Task Manager
- No visible windows

**Causes**:
1. `minimizeToTray: true` and window minimized to tray
2. Active render jobs in progress (user chose "Cancel Quit")
3. Shutdown orchestrator hung

**Solutions**:
1. Check system tray for app icon
2. Right-click tray icon → Quit
3. Check for active job dialogs
4. Force quit via Task Manager if truly hung

### Child Processes Not Cleaned Up

**Symptoms**:
- FFmpeg processes remain after app exit
- Worker processes orphaned

**Causes**:
1. ProcessManager not tracking the process
2. Backend didn't register child processes
3. Force-kill timeout exceeded

**Solutions**:
1. Ensure ProcessManager is initialized
2. Verify backend registers spawned processes
3. Check if backend process tree termination works (`/T` flag)
4. Manually kill: `taskkill /F /IM ffmpeg.exe`

## Configuration

### Settings

**minimizeToTray** (default: `false`)
- Location: `{userData}/aura-config.json`
- Type: Boolean
- Description: When true, closing main window minimizes to tray instead of exiting

**Example**:
```json
{
  "minimizeToTray": false,
  "autoUpdate": true,
  "theme": "dark"
}
```

### Environment Variables

**AURA_FORCE_QUIT** (optional)
- Type: Boolean
- Description: Skip all shutdown checks and force immediate exit
- Use: Emergency situations only

**AURA_SHUTDOWN_TIMEOUT** (optional)
- Type: Integer (milliseconds)
- Description: Override default shutdown timeout
- Default: 10000 (10 seconds)

## Testing

### Manual Test Checklist

- [ ] Close via window close button → All processes exit
- [ ] Close via File → Exit → All processes exit
- [ ] Close via system tray → All processes exit
- [ ] Close with active render → User prompt appears
- [ ] Enable minimizeToTray → Window hides, processes continue
- [ ] Quit from tray → All processes exit
- [ ] Force shutdown (kill main process) → Backend terminates within 10s

### Automated Tests

**test-process-manager.js**:
- ProcessManager initialization
- Process registration and unregistration
- Process termination (graceful and force)
- Diagnostics API

**test-shutdown-orchestrator.js**:
- Shutdown sequence steps
- Active render checking
- User prompt handling
- Timeout enforcement

**Run Tests**:
```bash
cd Aura.Desktop
npm run test:process-manager
npm run test:shutdown
```

## Best Practices

### For Users

1. **Exit Properly**: Use File → Exit or system tray, not Task Manager
2. **Wait for Jobs**: If rendering, let it complete or choose "Wait for Completion"
3. **Check System Tray**: If app seems gone but processes remain, check tray icon
4. **Report Issues**: If lingering processes persist, report with logs

### For Developers

1. **Register All Processes**: Any spawned child must be registered with ProcessManager
2. **Handle Cleanup**: Always clean up resources in finally blocks
3. **Test Shutdown**: Verify no lingering processes after your changes
4. **Log Events**: Log process start/stop for debugging
5. **Use Timeouts**: All async operations should have reasonable timeouts

## Architecture Decisions

### Why Separate ProcessManager?

- **Centralized Tracking**: Single source of truth for all child processes
- **Platform Abstraction**: Handles Windows/Unix differences
- **Diagnostics**: Easy to query all active processes
- **Testing**: Can mock for unit tests

### Why Default `minimizeToTray: false`?

- **User Expectation**: Closing window should exit app
- **Resource Management**: No hidden background processes consuming CPU/memory
- **Clear Intent**: Users explicitly opt into background mode
- **Troubleshooting**: Fewer "app won't close" support issues

### Why Graceful → Force Escalation?

- **Data Safety**: Try to finish/cancel jobs cleanly first
- **Resource Cleanup**: Let processes close connections, files
- **User Control**: User decides force vs. wait
- **Reliability**: Guaranteed exit even if graceful fails

## References

- [Electron Process Documentation](https://www.electronjs.org/docs/latest/api/process)
- [Node.js Child Process](https://nodejs.org/api/child_process.html)
- [Windows taskkill Command](https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/taskkill)
- [Unix Signals](https://man7.org/linux/man-pages/man7/signal.7.html)

## Changelog

### 2025-11-15 - Clean Shutdown Implementation
- Added ProcessManager for centralized process tracking
- Changed default `minimizeToTray` to `false`
- Added child process termination to shutdown sequence
- Enhanced logging for process lifecycle events
- Added diagnostics API for process tracking
- Documented complete process model and shutdown behavior

---

**Last Updated**: 2025-11-15  
**Version**: 1.0.0
