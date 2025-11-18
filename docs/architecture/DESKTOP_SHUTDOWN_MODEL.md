# Desktop Application Shutdown Model

## Overview

This document describes the process architecture and shutdown behavior of Aura Video Studio desktop application to ensure clean process termination when the application closes.

## Process Tree Architecture

### Root Process

**Electron Main Process** (`Aura Video Studio.exe`)
- **PID**: Managed by Electron framework
- **Role**: Main application shell, window management, orchestration
- **Lifetime**: Lives for entire application session
- **Children**: Backend API server, utility processes

### Child Processes

#### 1. Backend API Server (`Aura.Api.exe` / `dotnet Aura.Api.dll`)

- **Parent**: Electron Main Process
- **Spawned by**: `backend-service.js` via Node.js `spawn()`
- **Startup**: On application launch (in `startApplication()`)
- **Port**: Dynamic (auto-detected via `_findAvailablePort()`)
- **Tracking**: 
  - Registered with `ProcessManager` on spawn
  - PID stored in `backendService.pid`
  - Process handle stored in `backendService.process`
- **Environment**:
  ```javascript
  {
    ASPNETCORE_URLS: `http://localhost:${port}`,
    DOTNET_ENVIRONMENT: isDev ? 'Development' : 'Production',
    AURA_DATA_PATH: app.getPath('userData'),
    AURA_LOGS_PATH: path.join(app.getPath('userData'), 'logs'),
    AURA_TEMP_PATH: path.join(app.getPath('temp'), 'aura-video-studio'),
    FFMPEG_PATH: ffmpegPath,
    FFMPEG_BINARIES_PATH: ffmpegPath
  }
  ```
- **Shutdown Contract**:
  - Responds to `/api/system/shutdown` POST endpoint
  - Gracefully stops IHostedServices via `IHostApplicationLifetime.StopApplication()`
  - Should terminate within 2 seconds of graceful shutdown request
  - Force-killed via `taskkill /F /T /PID {pid}` on Windows if timeout exceeded

#### 2. FFmpeg Processes (Video Rendering)

- **Parent**: Backend API Server
- **Spawned by**: `FFmpegService` in `Aura.Core` during video rendering
- **Count**: Variable (depends on active jobs, typically 1 per active render)
- **Tracking**: 
  - **CURRENT**: Not explicitly registered with central process manager
  - **SHOULD BE**: Registered with `ShutdownOrchestrator` in backend
- **Lifetime**: Duration of single video render operation
- **Shutdown Contract**:
  - Should be terminated when parent job is cancelled
  - Should register with backend's ShutdownOrchestrator for cleanup on app shutdown
  - Must be killed via process tree termination if parent terminates

#### 3. Background Workers (Job Processing)

- **Parent**: Backend API Server
- **Spawned by**: 
  - `BackgroundJobProcessorService` (IHostedService)
  - Various job runners in orchestration pipeline
- **Tracking**: 
  - Managed by ASP.NET Core hosted services lifecycle
  - Use CancellationToken for cooperative cancellation
- **Shutdown Contract**:
  - Respect CancellationToken passed from `IHostApplicationLifetime.ApplicationStopping`
  - Complete in-flight operations or cancel within timeout
  - No new work accepted after shutdown signal

## Shutdown Sequence

### User Action: Close Window

When user clicks the X button or selects File → Exit:

```
1. Window 'close' event triggered
   ↓
2. mainWindow.on('close') handler checks minimizeToTray setting
   ↓
3. If minimizeToTray = false (default):
   → Event not prevented
   → Continues to next step
   ↓
4. 'window-all-closed' event fires (Windows/Linux)
   ↓
5. ShutdownOrchestrator.initiateShutdown() called
   ↓
6. Shutdown sequence executes (see below)
```

### Detailed Shutdown Sequence

Orchestrated by `ShutdownOrchestrator` (Electron) which coordinates with backend `ShutdownOrchestrator` (C#):

#### Step 1: Check Active Renders (Optional, unless skipChecks=true)

```javascript
const renderCheck = await checkActiveRenders();
// Calls: GET /api/jobs/active
// If jobs exist:
//   → Show dialog: "Cancel Quit" | "Wait for Completion" | "Force Quit"
//   → User chooses action
```

**Timeout**: 2 seconds to check API
**Skip on**: Force shutdown or skipChecks flag

#### Step 2: Close Windows & Tray

```javascript
closeWindows()
// - mainWindow.close()
// - splashWindow.close()
// - trayManager.destroy()
```

**Timeout**: Immediate (synchronous)

#### Step 3: Signal Backend Shutdown (Graceful)

```javascript
await signalBackendShutdown()
// Calls: POST /api/system/shutdown
// Backend executes:
//   1. Notify SSE connections (1 second timeout)
//   2. Close SSE connections (500ms delay)
//   3. Terminate child processes (FFmpeg, etc.)
//   4. Call IHostApplicationLifetime.StopApplication()
```

**Timeout**: 3 seconds for API call
**Fallback**: If API call fails (ECONNREFUSED), continue to next step

#### Step 4: Stop Backend Process

```javascript
await backendService.stop()
// Electron side termination:
//   1. Attempt graceful via API (Step 3)
//   2. Wait 2 seconds (GRACEFUL_SHUTDOWN_TIMEOUT)
//   3. If still running, Windows: taskkill /PID {pid} /T
//                        Unix: process.kill(SIGTERM)
//   4. Wait 1 second (FORCE_KILL_TIMEOUT)
//   5. If still running: taskkill /F /PID {pid} /T (force)
//                        Unix: process.kill(SIGKILL)
```

**Total Timeout**: 
- Graceful: 2 seconds
- Force: 1 second after graceful timeout
- **Maximum**: 3 seconds per backend termination

#### Step 5: Terminate Tracked Processes

```javascript
await processManager.terminateAll(timeout)
// For each registered process:
//   1. Try graceful termination
//   2. Wait up to timeout
//   3. Force kill if still alive
```

**Timeout**: 3 seconds (configurable via COMPONENT_TIMEOUT_MS)

#### Step 6: Cleanup Temp Files

```javascript
await cleanup()
// Remove: ${TEMP}/aura-video-studio/*
```

**Timeout**: 2 seconds
**Non-blocking**: Failure logged but doesn't prevent app exit

### Total Shutdown Time Budget

| Scenario | Expected Duration |
|----------|------------------|
| Clean (no active jobs) | 1-3 seconds |
| With active jobs (user chooses wait) | Variable (up to 5 minutes) |
| With active jobs (user chooses force) | 3-5 seconds |
| Hung backend (graceful fails) | 5-8 seconds (force kill) |

**Critical**: Maximum 10 seconds before force exit

## Current Issues and Fixes Applied

### Issue 1: Competing Shutdown Event Handlers - ✅ FIXED

**Problem**: `window-all-closed` and `before-quit` both tried to run shutdown, creating deadlock.

**Impact**: App hung indefinitely on close, processes never terminated.

**Fix Applied**:
- `window-all-closed` now only calls `app.quit()` to trigger the quit sequence
- `before-quit` is the single point of shutdown coordination
- `before-quit` uses `event.preventDefault()` once, runs cleanup, then calls `process.exit(0)`
- Added 5-second hard timeout in main.js with `process.exit(0)` failsafe

**Result**: Clean, single shutdown path without deadlock.

---

### Issue 2: Untracked FFmpeg Processes - ✅ FIXED

**Problem**: FFmpeg processes spawned by backend were not registered with any central process manager.

**Impact**: When backend was killed, FFmpeg processes were orphaned (not terminated with parent).

**Fix Applied**:
- Registered `IProcessManager` in DI container (Program.cs)
- Backend's `ShutdownOrchestrator` now receives `IProcessManager` via constructor injection
- Added `TerminateFFmpegProcessesAsync()` step in shutdown sequence
- FFmpeg processes are tracked and terminated explicitly on shutdown

**Result**: FFmpeg processes are properly tracked and terminated.

---

### Issue 3: Backend Hosted Services May Not Respect Shutdown - ⏳ MITIGATED

**Problem**: Long-running background jobs (e.g., `BackgroundJobProcessorService`) may not exit quickly.

**Impact**: Backend process may not terminate within timeout, requiring force kill.

**Fix Applied**:
- Reduced graceful timeout to 2 seconds (was 3-5 seconds)
- Reduced component timeout to 1 second (was 2-3 seconds)
- Force kill after graceful timeout
- Absolute 4-second timeout wraps entire shutdown

**Result**: Faster timeouts ensure force kill happens quickly. Hosted services should check CancellationToken frequently for best results.

---

### Issue 4: Process Tree Termination Not Guaranteed - ✅ FIXED

**Problem**: On Windows, `taskkill /T` should terminate process tree, but may fail if processes are detached or re-parented.

**Impact**: Child processes (FFmpeg, workers) may survive parent termination.

**Fix Applied**:
- Backend spawns processes with proper parent relationship (not detached)
- Added explicit cleanup loop in backend to kill known child PIDs via ProcessManager
- Added `killAllAuraProcesses()` failsafe in Electron to scan and kill all Aura-related processes by name
- Failsafe kills: `Aura.Api.exe`, `dotnet.exe`, `ffmpeg.exe`, `Aura Video Studio.exe`

**Result**: Multiple layers of process termination ensure no orphans.

---

### Issue 5: No Absolute Hard Timeout - ✅ FIXED

**Problem**: Shutdown could theoretically wait indefinitely if user chose "Wait for Completion" on long job.

**Impact**: User had no escape from waiting if job was stuck.

**Fix Applied**:
- Added 5-second absolute timeout in main.js
- Added 4-second absolute timeout in shutdown orchestrator
- After timeout, force-kill failsafe activates automatically
- User can't be stuck waiting indefinitely

**Result**: App always exits within 5 seconds maximum.

## Testing Strategy

### Unit Tests

- [x] `test-process-manager.js` - ProcessManager registration and termination
- [x] `test-shutdown-orchestrator.js` - Shutdown sequence steps
- [ ] **NEW**: `test-ffmpeg-process-cleanup.js` - FFmpeg termination on job cancel
- [ ] **NEW**: `test-backend-shutdown-timeout.js` - Force kill after timeout

### Integration Tests

- [ ] **NEW**: Full app startup + shutdown with no jobs
- [ ] **NEW**: Full app shutdown with active video render
- [ ] **NEW**: Full app shutdown with hung backend (simulated)
- [ ] **NEW**: Verify no processes remain after shutdown

### Manual Testing Checklist

On Windows 11:

1. **Normal Shutdown**
   - [ ] Launch app
   - [ ] Close window
   - [ ] Verify in Task Manager: No "Aura Video Studio" processes remain
   - [ ] Verify in Task Manager: No ".NET Host" processes related to Aura remain
   - [ ] Wait 10 seconds and verify again

2. **Shutdown with Active Job**
   - [ ] Launch app
   - [ ] Start video generation job
   - [ ] While rendering, close window
   - [ ] Choose "Force Quit" in dialog
   - [ ] Verify all processes terminate within 5 seconds

3. **Shutdown with Hung Backend**
   - [ ] Launch app
   - [ ] Use debugger to pause backend thread (simulate hang)
   - [ ] Close window
   - [ ] Verify force kill occurs after timeout (~3 seconds)
   - [ ] Verify all processes terminate

4. **Rapid Restart**
   - [ ] Launch app
   - [ ] Close window immediately
   - [ ] Launch app again within 2 seconds
   - [ ] Verify no port conflicts
   - [ ] Verify app starts successfully

## Diagnostics and Logging

### Shutdown Log Example (Success)

```
[INFO] Initiating graceful shutdown (Force: false, SkipChecks: false)
[INFO] Checking for active renders...
[INFO] No active renders found
[INFO] Step 1/5: Windows closed
[INFO] Step 2/5: Backend shutdown signal sent
[INFO] Step 3/5: Backend stopped
[INFO] Step 4/5: No child processes to terminate
[INFO] Step 5/5: Cleanup complete
[INFO] Graceful shutdown completed in 1247ms
```

### Shutdown Log Example (Force Kill Required)

```
[INFO] Initiating graceful shutdown (Force: false, SkipChecks: false)
[INFO] Checking for active renders...
[INFO] No active renders found
[INFO] Step 1/5: Windows closed
[INFO] Step 2/5: Backend shutdown signal sent
[WARN] Backend did not shut down gracefully, forcing termination...
[INFO] Executing: taskkill /PID 12345 /T
[INFO] Step 3/5: Backend force killed after timeout
[INFO] Step 4/5: No child processes to terminate
[INFO] Step 5/5: Cleanup complete
[WARN] Graceful shutdown completed in 3512ms
```

### Process Diagnostic Endpoint

For debugging, backend exposes (dev mode only):

```
GET /api/system/process-info
Response:
{
  "currentProcess": {
    "pid": 12345,
    "parentPid": 11111,
    "name": "Aura.Api.exe"
  },
  "trackedChildProcesses": [
    {
      "pid": 12346,
      "name": "ffmpeg.exe",
      "registeredAt": "2025-11-16T00:20:00Z",
      "metadata": { "jobId": "abc123" }
    }
  ]
}
```

## Developer Guidelines

### When Spawning a Process

1. **Always register** with ProcessManager (Electron) or ShutdownOrchestrator (Backend)
2. **Store the PID** for later cleanup
3. **Listen for exit** and unregister
4. **Use CancellationToken** if process is async/long-running

Example (Electron):
```javascript
const process = spawn('ffmpeg', args);
processManager.register('FFmpeg - Video Encode', process, { jobId: 'abc123' });
```

Example (Backend C#):
```csharp
var process = Process.Start(psi);
_shutdownOrchestrator.RegisterChildProcess(process.Id);
```

### When Implementing a Hosted Service

1. **Accept CancellationToken** in ExecuteAsync
2. **Check token frequently** (at least every 100ms in loops)
3. **Complete quickly** on cancellation (< 2 seconds)
4. **Dispose resources** in Dispose() method

Example:
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        // Do work
        await Task.Delay(100, stoppingToken);
    }
    // Cleanup
}
```

## Future Enhancements

- [ ] Add telemetry for shutdown duration
- [ ] Implement "Emergency Shutdown" button if shutdown takes > 10 seconds
- [ ] Add process tree visualization in diagnostics panel
- [ ] Automated test suite that runs on every PR (requires Windows runner)
- [ ] Add health check that detects orphaned processes from previous runs

## References

- Electron Main Process: `Aura.Desktop/electron/main.js`
- Backend Service: `Aura.Desktop/electron/backend-service.js`
- Process Manager: `Aura.Desktop/electron/process-manager.js`
- Shutdown Orchestrator (Electron): `Aura.Desktop/electron/shutdown-orchestrator.js`
- Shutdown Orchestrator (Backend): `Aura.Api/Services/ShutdownOrchestrator.cs`
- System Controller: `Aura.Api/Controllers/SystemController.cs`
