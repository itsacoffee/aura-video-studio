# Process Lifecycle Testing Guide

This document describes how to verify that Aura Video Studio properly cleans up all child processes when the application exits, preventing "zombie" processes.

## Overview

The application spawns several types of child processes:
- **Aura.Api** (.NET backend service)
- **ffmpeg.exe** (video rendering processes)
- Other utility processes

All of these must be properly terminated when the application closes to prevent:
- Port conflicts on next startup
- Resource leaks
- Orphaned processes consuming system resources

## Implementation Details

### Backend Process Management

**Location**: `Aura.Desktop/electron/backend-service.js`

**Key Features**:
- `stop()` method attempts graceful shutdown (SIGINT) first
- Falls back to force kill (SIGKILL) after 5-second timeout
- `_waitForExit()` helper monitors process termination
- Orphan detection at startup cleans up zombies from crashed sessions

### FFmpeg Process Management

**Location**: `Aura.Desktop/electron/ipc-handlers/ffmpeg-handler.js`

**Key Features**:
- `ffmpegProcesses` Set tracks all spawned ffmpeg instances
- `trackProcess()` registers processes for cleanup
- `stop()` method terminates all tracked processes
- Automatic removal from tracking set on process exit

### Lifecycle Integration

**Location**: `Aura.Desktop/electron/main.js`

**Cleanup Sequence**:
1. `app.window-all-closed` → sets `isQuitting = true`
2. `app.before-quit` → triggers `ShutdownOrchestrator`
3. `cleanup()` function calls:
   - `ipcHandlers.ffmpeg.stop()` (terminates ffmpeg processes)
   - `backendService.stop()` (terminates backend with graceful → force)
4. Process exits after cleanup completes (max 5-second timeout)

## Manual Testing Procedures

### Test 1: Normal Exit via Window Close

**Purpose**: Verify clean shutdown when user closes the main window.

**Steps**:
1. Launch Aura Video Studio
2. Wait for application to fully load (backend ready)
3. Open Windows Task Manager (Ctrl+Shift+Esc)
4. Filter processes by "Aura" or "ffmpeg"
5. Note all running Aura-related processes (Aura.Api.exe, etc.)
6. Close the application via the window close button (X)
7. Wait 10 seconds
8. Check Task Manager

**Expected Result**:
- ✅ No `Aura.Api.exe` processes remain
- ✅ No `ffmpeg.exe` processes remain
- ✅ Only standard Windows processes visible

**Actual Result**: _[Fill in during testing]_

---

### Test 2: Normal Exit via Menu

**Purpose**: Verify clean shutdown when using File → Exit menu.

**Steps**:
1. Launch Aura Video Studio
2. Wait for application to fully load
3. Open Windows Task Manager
4. Filter processes by "Aura" or "ffmpeg"
5. Note all running Aura-related processes
6. Click **File → Exit** in the application menu
7. Wait 10 seconds
8. Check Task Manager

**Expected Result**:
- ✅ No `Aura.Api.exe` processes remain
- ✅ No `ffmpeg.exe` processes remain

**Actual Result**: _[Fill in during testing]_

---

### Test 3: Exit During Video Rendering

**Purpose**: Verify FFmpeg processes are terminated when app closes during active render.

**Steps**:
1. Launch Aura Video Studio
2. Start a video generation job (use "Quick Demo" for fast setup)
3. Once rendering begins, open Task Manager
4. Confirm `ffmpeg.exe` process is running
5. Immediately close the application window
6. Wait 10 seconds
7. Check Task Manager

**Expected Result**:
- ✅ No `Aura.Api.exe` processes remain
- ✅ No `ffmpeg.exe` processes remain (killed mid-render)
- ✅ Application closed without hanging

**Actual Result**: _[Fill in during testing]_

---

### Test 4: Orphan Cleanup on Next Startup

**Purpose**: Verify orphan detection cleans up zombie processes from previous crash.

**Steps**:
1. Launch Aura Video Studio normally
2. Open Task Manager
3. Forcefully kill the Electron process (End Task on "Aura Video Studio")
   - This simulates a crash, leaving backend orphaned
4. Verify `Aura.Api.exe` is still running after Electron exits
5. Launch Aura Video Studio again
6. Check console output for orphan detection messages:
   ```
   [OrphanDetection] Port 5005 is already in use, attempting cleanup...
   [OrphanDetection] Killed orphaned backend: ...
   [OrphanDetection] Cleanup successful, port is now available
   ```
7. Confirm application starts successfully

**Expected Result**:
- ✅ Console logs show orphan detection and cleanup
- ✅ Orphaned `Aura.Api.exe` is terminated
- ✅ Application starts successfully on same port
- ✅ No manual intervention required

**Actual Result**: _[Fill in during testing]_

---

### Test 5: Force Kill Timeout

**Purpose**: Verify force kill fallback when graceful shutdown fails.

**Setup**: This requires modifying backend to ignore SIGINT (for testing only).

**Steps**:
1. Temporarily disable graceful shutdown in backend (comment out shutdown handler)
2. Launch application
3. Close application normally
4. Observe logs for timeout and force kill:
   ```
   [BackendService] Backend did not exit within timeout. Forcing kill.
   ```
5. Verify process is terminated despite ignoring SIGINT

**Expected Result**:
- ✅ Graceful shutdown times out after 5 seconds
- ✅ Force kill (SIGKILL) succeeds
- ✅ No zombie processes remain

**Actual Result**: _[Fill in during testing]_

---

## Automated Testing

### Integration Test Script

**Location**: `Aura.Desktop/test/test-process-lifecycle.js`

**Coverage**:
- ✅ BackendService.stop() calls SIGINT then SIGKILL
- ✅ FFmpegHandler.stop() terminates all tracked processes
- ✅ ProcessManager tracks and terminates child processes
- ✅ Orphan detection identifies and kills zombie processes

**Run**:
```bash
cd Aura.Desktop
npm run test:process-lifecycle
```

---

## Diagnostics and Troubleshooting

### How to Check for Zombie Processes

**Windows**:
```powershell
# List all Aura-related processes
tasklist | findstr /I "Aura"

# List all ffmpeg processes
tasklist | findstr /I "ffmpeg"

# Kill specific process
taskkill /F /IM Aura.Api.exe
```

**Linux/macOS**:
```bash
# List all Aura-related processes
ps aux | grep -i aura

# List all ffmpeg processes
ps aux | grep -i ffmpeg

# Kill specific process
pkill -9 Aura.Api
```

### Common Issues

**Issue 1: Port Already in Use**
- **Symptom**: Backend fails to start with "port already in use" error
- **Cause**: Orphaned backend process from previous session
- **Fix**: Orphan detection should handle automatically. If not:
  ```bash
  taskkill /F /IM Aura.Api.exe
  ```

**Issue 2: FFmpeg Zombie After Crash**
- **Symptom**: ffmpeg.exe processes remain after application crash
- **Cause**: Crash occurred before cleanup could run
- **Fix**: Manual cleanup required (orphan detection only handles backend):
  ```bash
  taskkill /F /IM ffmpeg.exe
  ```

**Issue 3: Slow Shutdown**
- **Symptom**: Application takes >5 seconds to close
- **Cause**: Graceful shutdown timeout before force kill
- **Expected**: This is normal behavior, ensures clean shutdown
- **Timeout**: 5 seconds maximum (configurable in backend-service.js)

---

## Verification Checklist

Before marking this PR as complete, verify:

- [ ] Test 1: Normal exit via window close ✅
- [ ] Test 2: Normal exit via menu ✅
- [ ] Test 3: Exit during video rendering ✅
- [ ] Test 4: Orphan cleanup on restart ✅
- [ ] Test 5: Force kill timeout ✅
- [ ] Automated tests pass
- [ ] No zombie processes in Task Manager after 10 seconds
- [ ] Application restarts cleanly without port conflicts

---

## Related Files

- `Aura.Desktop/electron/backend-service.js` - Backend process lifecycle
- `Aura.Desktop/electron/ipc-handlers/ffmpeg-handler.js` - FFmpeg process tracking
- `Aura.Desktop/electron/main.js` - Lifecycle event handlers
- `Aura.Desktop/electron/shutdown-orchestrator.js` - Coordinated shutdown
- `Aura.Desktop/electron/process-manager.js` - Centralized process tracking

---

## Success Criteria

✅ **All tests pass** without manual intervention
✅ **No zombie processes** remain 10 seconds after exit
✅ **Orphan detection** automatically cleans up crashed sessions
✅ **FFmpeg cleanup** works during active renders
✅ **Force kill** fallback prevents hanging processes

---

## Notes

- Graceful shutdown timeout is 5 seconds (configurable)
- Force kill is always used as fallback to prevent zombies
- Orphan detection runs on every startup (prevents accumulation)
- FFmpeg processes are tracked in-memory via Set
- Backend PID stored for Windows process tree termination
