# Manual Shutdown Testing Guide

This guide helps verify that the shutdown fix works correctly and no processes linger after closing the app.

## Prerequisites

- Windows 11 (primary target platform)
- Task Manager open and visible
- Aura Video Studio installed or running from development build
- **IMPORTANT**: Review logs in `%APPDATA%\Aura\logs\` after each test

## Recent Improvements (Latest Updates)

### What Changed
1. **Host Shutdown Timeout**: Increased from 5s (default) to 30s to allow all background services adequate shutdown time
2. **Enhanced Logging**: Added detailed per-step timing and process ID tracking throughout shutdown sequence
3. **Service Shutdown Logging**: All BackgroundService implementations now log their StopAsync entry/exit
4. **Comprehensive Tests**: 11 new integration tests validate shutdown behavior

### Expected Behavior
- All processes should terminate within 30 seconds maximum
- Logs should show complete shutdown sequence with timings
- No zombie processes should remain in Task Manager
- Application should be immediately restartable without port conflicts

## Test Scenarios

### Test 1: Normal Shutdown (No Active Jobs)

**Purpose**: Verify clean shutdown with no active work

**Steps**:
1. Launch Aura Video Studio
2. Wait for app to fully load (main window visible)
3. Open Task Manager (Ctrl+Shift+Esc)
4. Go to "Details" tab and sort by Name
5. Note the processes:
   - Look for "Aura Video Studio.exe" (Electron)
   - Look for "Aura.Api.exe" or "dotnet.exe" (Backend)
   - Look for any ".NET Host" processes
6. Close the app by clicking the X button
7. **WATCH**: Task Manager for 10 seconds

**Expected Result**:
- ✅ All "Aura Video Studio.exe" processes disappear within 1-2 seconds
- ✅ All "Aura.Api.exe" or "dotnet.exe" (hosting Aura) processes disappear within 3-5 seconds
- ✅ No ".NET Host" processes remain after 5 seconds
- ✅ No "ffmpeg.exe" processes remain
- ✅ App can be re-launched immediately without errors

**FAIL Criteria**:
- ❌ Any Aura-related process visible after 10 seconds
- ❌ ".NET Host" processes remain
- ❌ Cannot re-launch app due to "port in use" error

---

### Test 2: Shutdown During Active Video Generation

**Purpose**: Verify shutdown handles active work gracefully

**Steps**:
1. Launch Aura Video Studio
2. Start a video generation job (Quick Demo or full workflow)
3. While job is rendering (check progress bar), close the app window
4. **WATCH**: Dialog appears asking what to do
5. Select "Force Quit"
6. **WATCH**: Task Manager for 10 seconds

**Expected Result**:
- ✅ Dialog appears: "Active Renders in Progress" with 3 options
- ✅ After selecting "Force Quit", app closes within 5 seconds
- ✅ All Aura processes terminate within 10 seconds total
- ✅ FFmpeg process (if visible) is killed
- ✅ No lingering processes

**FAIL Criteria**:
- ❌ App hangs after clicking "Force Quit"
- ❌ FFmpeg processes remain after 10 seconds
- ❌ Backend or .NET Host processes remain

---

### Test 3: Rapid Close/Reopen

**Purpose**: Verify app can be quickly restarted without conflicts

**Steps**:
1. Launch Aura Video Studio
2. Wait 2 seconds for it to load
3. Close it immediately (X button)
4. Wait 2 seconds
5. Launch Aura Video Studio again
6. Repeat steps 3-5 three more times (total 4 cycles)

**Expected Result**:
- ✅ Each launch succeeds without errors
- ✅ No "port already in use" errors
- ✅ No "backend failed to start" errors
- ✅ Each shutdown completes within 5 seconds
- ✅ No process accumulation in Task Manager

**FAIL Criteria**:
- ❌ "Port 50XX is already in use" error on relaunch
- ❌ Multiple backends running simultaneously
- ❌ App fails to start after rapid close/reopen

---

### Test 4: Force Kill Timeout Test

**Purpose**: Verify force-kill failsafe activates if clean shutdown fails

**Steps** (requires debugging/simulation):
1. Launch Aura Video Studio in development mode
2. Use debugger to pause backend thread (or kill backend process manually)
3. Try to close the app
4. **WATCH**: Console output and Task Manager

**Expected Result**:
- ✅ App attempts graceful shutdown
- ✅ After 2-3 seconds, force kill is attempted
- ✅ Console shows: "Backend did not shut down gracefully, forcing termination..."
- ✅ Console shows: "Executing: taskkill /F /T /PID ..."
- ✅ After timeout, failsafe kills processes by name
- ✅ App exits within 5 seconds total

**FAIL Criteria**:
- ❌ App hangs indefinitely
- ❌ Force kill is never attempted
- ❌ Processes remain after 10 seconds

---

## Diagnostic Commands (Windows)

### Check for lingering processes after close:

```powershell
# Check for Aura.Api.exe
Get-Process -Name "Aura.Api" -ErrorAction SilentlyContinue

# Check for .NET Host processes (look for Aura in command line)
Get-Process | Where-Object { $_.ProcessName -like "*dotnet*" -or $_.ProcessName -like "*host*" } | Select-Object Id, ProcessName, Path

# Check for FFmpeg
Get-Process -Name "ffmpeg" -ErrorAction SilentlyContinue

# Check for Aura Video Studio Electron process
Get-Process | Where-Object { $_.MainWindowTitle -like "*Aura*" }
```

### Force kill all Aura processes (if needed for cleanup):

```powershell
# Kill Aura.Api
taskkill /F /IM Aura.Api.exe 2>$null

# Kill FFmpeg
taskkill /F /IM ffmpeg.exe 2>$null

# Kill Electron (be careful - will kill all Electron apps)
# Only run if you're sure no other Electron apps are running
# taskkill /F /IM "Aura Video Studio.exe" 2>$null
```

---

## Logging

### Where to find logs:

**Electron Logs**:
- Windows: `%APPDATA%\aura-video-studio\logs\`
- Look for: `startup-*.log`, `crash-*.log`

**Backend Logs**:
- Windows: `%APPDATA%\aura-video-studio\logs\`
- Look for: Structured JSON logs with shutdown events

### What to look for in logs:

**Good Shutdown Sequence (Updated - Latest Implementation)**:

**API Backend Logs** (`%APPDATA%\Aura\logs\aura-api-*.log`):
```
[INFO] Initiating graceful shutdown (Force: false, PID: 12345)
[INFO] Step 1/4 Complete: SSE Notification: No active connections (Elapsed: 5ms)
[INFO] Step 2/4 Complete: SSE Closure: No connections to close (Elapsed: 1ms)
[INFO] Step 3/4 Complete: FFmpeg Termination: No FFmpeg processes (Elapsed: 2ms)
[INFO] Step 4/4 Complete: Process Termination: No child processes (Elapsed: 1ms)
[INFO] Graceful shutdown completed successfully (Total: 235ms)
[INFO] Calling IHostApplicationLifetime.StopApplication()...
[INFO] StopApplication() called - host shutdown initiated
[INFO] === Application Shutdown Initiated ===
[INFO] Shutdown Phase 1: Stopping background services
[INFO] BackgroundJobProcessorService StopAsync called - initiating graceful shutdown
[INFO] CleanupHostedService StopAsync called - initiating graceful shutdown
[INFO] BackgroundJobProcessorService stopping due to cancellation
[INFO] BackgroundJobProcessorService stopped
[INFO] BackgroundJobProcessorService StopAsync completed
[INFO] CleanupHostedService StopAsync completed
[INFO] Shutdown Phase 2: Background services stopped (Elapsed: 1523ms)
[INFO] === Application Shutdown Complete (PID: 12345) ===
[INFO] All hosted services have been stopped
[INFO] Process should exit momentarily...
```

**Electron Logs** (`%APPDATA%\aura-video-studio\logs\`):
```
[INFO] Initiating shutdown (Force: false, SkipChecks: true, AbsoluteTimeout: 5000ms)
[INFO] Step 1/5 Complete: Windows closed
[INFO] Step 2/5 Complete: Backend shutdown signal sent
[INFO] Step 3/5 Complete: Backend stopped
[INFO] Step 4/5 Complete: Terminated 0 process(es)
[INFO] Step 5/5 Complete: Cleanup complete
[INFO] Shutdown completed successfully in 1847ms
[INFO] Exiting application...
```

**Problem Indicators**:
```
[ERROR] Shutdown hard timeout exceeded
[ERROR] Error during shutdown sequence
[ERROR] Attempting force kill of backend process tree...
[WARN] Backend did not shut down gracefully, forcing termination...
[ERROR] Activating failsafe process termination...
[WARN] Process {ProcessId} did not exit gracefully, force killing
```

If you see these, the shutdown took too long and failsafe was activated. The fix should still work, but investigate:
- Which step timed out (check elapsed times)
- Are there any active jobs/renders that didn't cancel?
- Is FFmpeg hung on a process?
- Check for deadlocks or blocking operations in logs

### Timing Expectations

**Normal Shutdown** (no active work):
- Total time: 1-3 seconds
- Electron orchestrator: <1 second
- Backend shutdown: 1-2 seconds
- Service cleanup: <1 second

**With Active Work**:
- Job cancellation: Up to 5 seconds
- FFmpeg termination: Up to 2 seconds
- Total: Up to 10 seconds

**Force Mode**:
- Total time: <2 seconds
- Immediate termination, no graceful cleanup

**Timeout Thresholds**:
- Host shutdown timeout: 30 seconds (should never be reached)
- Electron absolute timeout: 5 seconds
- Backend graceful: 2 seconds
- FFmpeg grace period: 1 second

---

## Reporting Issues

If any test fails, please provide:

1. **Which test scenario failed**
2. **Screenshots of Task Manager** showing lingering processes
3. **Console output** (if running in dev mode)
4. **Log files** from `%APPDATA%\aura-video-studio\logs\`
5. **Steps to reproduce** the issue
6. **System info**: Windows version, RAM, CPU

Post in GitHub Issue with these details.

---

## Success Criteria Summary

After all tests, the following should be true:

- ✅ App closes within 5 seconds in all scenarios
- ✅ No lingering processes visible in Task Manager after 10 seconds
- ✅ No ".NET Host" zombie processes
- ✅ No FFmpeg processes remain
- ✅ App can be rapidly closed and reopened without errors
- ✅ Force-kill failsafe works if needed
- ✅ Logs show clean shutdown sequence

If all criteria are met, the fix is successful! ✨
