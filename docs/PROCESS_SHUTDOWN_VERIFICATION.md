# Process Shutdown Verification Guide

This guide provides manual test steps to verify that all Aura and FFmpeg processes are properly terminated when the desktop application exits.

## Prerequisites

- Aura Video Studio desktop application installed
- Task Manager (Windows) or Activity Monitor (macOS) or `ps` (Linux) available
- Ability to run test renders or video generation jobs

## Test Scenarios

### Scenario 1: Normal Exit (No Jobs Running)

**Purpose**: Verify clean shutdown when no background operations are active.

**Steps**:

1. Launch Aura Video Studio desktop application
2. Note the initial processes in Task Manager:
   - Count "Aura" processes (should see Electron main +renderer + Aura.Api)
   - Count "ffmpeg" processes (should be 0)
   - **Example**: 3 Aura processes, 0 FFmpeg processes

3. Navigate through the UI (open different pages, check settings, etc.)

4. Exit the application using **one of these methods**:
   - File → Quit menu item
   - Alt+F4 / Cmd+Q keyboard shortcut
   - Click X on title bar (if minimize to tray is disabled)

5. Wait 10 seconds

6. **Verify in Task Manager**:
   - **Expected**: NO "Aura" processes remain
   - **Expected**: NO "ffmpeg" processes remain
   - **Expected**: NO "dotnet" processes running Aura.Api remain

**Pass Criteria**:
- ✅ All Aura-related processes terminated within 5 seconds
- ✅ No zombie or orphan processes remain
- ✅ Application exits gracefully without error dialogs

**Failure Indicators**:
- ❌ Aura.Api.exe still running
- ❌ ffmpeg.exe still running
- ❌ Electron processes still running
- ❌ Process shows "(Not Responding)" state

---

### Scenario 2: Exit During FFmpeg Operation

**Purpose**: Verify that active FFmpeg processes are properly terminated when exiting during video operations.

**Steps**:

1. Launch Aura Video Studio

2. Start a video generation job:
   - Go to Quick Demo or Create Video wizard
   - Start a render/generation process
   - **Or** use any feature that spawns FFmpeg (audio conversion, thumbnail generation, etc.)

3. While the operation is **actively running** (check Task Manager for ffmpeg.exe):
   - Note FFmpeg process count (usually 1-4 processes)
   - Note Aura.Api.exe PID

4. Exit the application via File → Quit

5. **Observe** the shutdown behavior:
   - You may see a warning dialog about active jobs (if implemented)
   - Choose "Force Quit" or wait for timeout

6. **Verify in Task Manager**:
   - **Expected**: All FFmpeg processes terminated immediately or within 2 seconds
   - **Expected**: Backend process (Aura.Api.exe) terminated
   - **Expected**: All Electron processes terminated

**Pass Criteria**:
- ✅ FFmpeg processes killed within 2 seconds of choosing Force Quit
- ✅ No FFmpeg processes remain after 5 seconds
- ✅ Backend process terminated (gracefully or forcefully)
- ✅ No error dialogs or hang states

**Failure Indicators**:
- ❌ ffmpeg.exe continues running indefinitely
- ❌ Application hangs on "Closing..." message
- ❌ Processes must be manually killed via Task Manager

---

### Scenario 3: Crash / Force Kill Desktop Shell

**Purpose**: Verify that orphaned backend and FFmpeg processes are detected and cleaned up on restart.

**Steps**:

1. Launch Aura Video Studio

2. Start a video operation to spawn FFmpeg processes

3. **Force close** the Electron main process:
   - **Windows**: Open Task Manager → Find "Aura Video Studio" → Right-click → End Task (select main process, not backend)
   - **macOS**: Activity Monitor → Find "Aura Video Studio" → Force Quit
   - **Linux**: `kill -9 <electron_pid>`

4. **DO NOT** close Aura.Api.exe manually - let it orphan

5. Check Task Manager:
   - **Expected**: Aura.Api.exe still running (orphaned)
   - **Expected**: ffmpeg.exe may still be running (orphaned)
   - Note these PIDs

6. **Restart** Aura Video Studio

7. **Observe** startup behavior:
   - Check logs for "Orphan detection" messages
   - Application should detect port conflict or stale processes

8. **Verify in Task Manager**:
   - **Expected**: Old orphaned processes are terminated (either auto-killed or port conflict detected)
   - **Expected**: New backend process started with different PID
   - **Expected**: Application starts successfully

**Pass Criteria**:
- ✅ Orphaned backend detected on startup
- ✅ Orphaned backend either reused or terminated
- ✅ New backend starts on expected port
- ✅ No duplicate Aura.Api.exe processes after restart

**Failure Indicators**:
- ❌ "Backend unreachable" or "Port already in use" errors
- ❌ Multiple Aura.Api.exe processes with different PIDs
- ❌ Application fails to start due to port conflict
- ❌ FFmpeg processes from previous session still running

---

### Scenario 4: Multiple Rapid Starts/Exits

**Purpose**: Stress test shutdown sequence to detect race conditions or leaked processes.

**Steps**:

1. Launch Aura Video Studio

2. Wait for full startup (backend ready)

3. Immediately exit (File → Quit)

4. **Repeat steps 1-3 five times** in rapid succession (within 30 seconds total)

5. After the 5th exit, wait 10 seconds

6. **Verify in Task Manager**:
   - **Expected**: NO Aura or FFmpeg processes remain
   - **Expected**: Only the processes from the LAST startup (if still running)

**Pass Criteria**:
- ✅ No accumulation of background processes
- ✅ Each exit cleanly removes all processes from that session
- ✅ No port conflicts on subsequent starts
- ✅ No "orphaned" processes from earlier sessions

**Failure Indicators**:
- ❌ Process count increases with each start/exit cycle
- ❌ Multiple Aura.Api.exe instances with different PIDs
- ❌ "Port already in use" errors after 2-3 cycles

---

### Scenario 5: System Shutdown While Aura Running

**Purpose**: Verify that Aura doesn't prevent or delay system shutdown.

**Steps**:

1. Launch Aura Video Studio

2. Start a video generation job (long-running, if possible)

3. While job is running, initiate **system shutdown**:
   - **Windows**: Start → Power → Shut down
   - **macOS**: Apple menu → Shut Down
   - **Linux**: `sudo shutdown now`

4. **Observe** shutdown behavior:
   - Does system wait for Aura to exit?
   - Does Aura show any dialogs preventing shutdown?
   - How long does shutdown take?

5. After system restarts, check for:
   - Crash logs in `%APPDATA%\Aura\logs`
   - Orphaned lock files

**Pass Criteria**:
- ✅ Aura exits within 5 seconds of shutdown signal
- ✅ System shutdown not delayed by Aura
- ✅ No crash dialogs during shutdown
- ✅ On next launch, no orphaned processes or port conflicts

**Failure Indicators**:
- ❌ System waits indefinitely for Aura to close
- ❌ Forced to "Force close" Aura to complete shutdown
- ❌ Aura shows "Not Responding" during shutdown

---

## Automated Verification (Optional)

For automated testing in CI/CD or regression suites:

### PowerShell Script (Windows)

```powershell
# Launch Aura
$auraPath = "C:\Path\To\Aura Video Studio.exe"
Start-Process $auraPath

# Wait for startup
Start-Sleep -Seconds 10

# Count initial processes
$beforeCount = (Get-Process | Where-Object { $_.ProcessName -match 'Aura|ffmpeg' }).Count
Write-Host "Processes before shutdown: $beforeCount"

# Get Aura main process
$auraProcess = Get-Process | Where-Object { $_.ProcessName -eq 'Aura Video Studio' }

# Gracefully close
$auraProcess.CloseMainWindow()

# Wait for exit
$auraProcess.WaitForExit(10000)  # 10 second timeout

# Count after
Start-Sleep -Seconds 5
$afterCount = (Get-Process | Where-Object { $_.ProcessName -match 'Aura|ffmpeg' }).Count
Write-Host "Processes after shutdown: $afterCount"

if ($afterCount -eq 0) {
    Write-Host "✅ PASS: All processes terminated"
    exit 0
} else {
    Write-Host "❌ FAIL: $afterCount processes remain"
    Get-Process | Where-Object { $_.ProcessName -match 'Aura|ffmpeg' } | Format-Table
    exit 1
}
```

### Bash Script (Linux/macOS)

```bash
#!/bin/bash

# Launch Aura
AURA_PATH="/path/to/Aura Video Studio.app/Contents/MacOS/Aura Video Studio"
"$AURA_PATH" &
AURA_PID=$!

# Wait for startup
sleep 10

# Count initial processes
BEFORE_COUNT=$(ps aux | grep -E 'Aura|ffmpeg' | grep -v grep | wc -l)
echo "Processes before shutdown: $BEFORE_COUNT"

# Gracefully close
kill -TERM $AURA_PID

# Wait for exit
sleep 5

# Count after
AFTER_COUNT=$(ps aux | grep -E 'Aura|ffmpeg' | grep -v grep | wc -l)
echo "Processes after shutdown: $AFTER_COUNT"

if [ "$AFTER_COUNT" -eq 0 ]; then
    echo "✅ PASS: All processes terminated"
    exit 0
else
    echo "❌ FAIL: $AFTER_COUNT processes remain"
    ps aux | grep -E 'Aura|ffmpeg' | grep -v grep
    exit 1
fi
```

---

## Troubleshooting

### If Processes Remain After Exit

1. **Identify the process**:
   - Note the PID and process name
   - Check if it's Aura.Api.exe or ffmpeg.exe

2. **Check logs**:
   - **Electron logs**: `%APPDATA%\Aura Video Studio\logs\main-YYYYMMDD.log`
   - **Backend logs**: `%APPDATA%\Aura\logs\aura-api-YYYYMMDD.log`
   - Look for:
     - "Shutdown initiated" messages
     - "Force killing" warnings
     - Timeout errors

3. **Manually terminate**:
   ```powershell
   # Windows
   taskkill /F /T /IM "Aura.Api.exe"
   taskkill /F /T /IM "ffmpeg.exe"
   ```

4. **Report the issue**:
   - Include: PIDs, timestamps, log excerpts
   - Specify which scenario failed
   - Note any error dialogs or hang symptoms

### If Backend Port Conflicts Occur

1. **Find process using the port**:
   ```powershell
   # Windows
   netstat -ano | findstr ":<port>"
   
   # macOS/Linux
   lsof -i :<port>
   ```

2. **Kill the conflicting process**:
   ```powershell
   # Windows
   taskkill /F /PID <pid>
   
   # macOS/Linux
   kill -9 <pid>
   ```

3. **Restart Aura**

---

## Expected Behavior Summary

| Scenario | Max Exit Time | Allowed Processes After Exit |
|----------|---------------|------------------------------|
| Normal exit (idle) | 3 seconds | 0 |
| Exit during FFmpeg | 5 seconds | 0 |
| Crash recovery | N/A (next startup) | 0 after cleanup |
| Rapid start/exit | 5 seconds per cycle | 0 cumulative |
| System shutdown | 5 seconds | 0 after reboot |

---

## Reporting Issues

If any test scenario fails, please report with:

1. **System Info**:
   - OS version
   - Aura Video Studio version
   - Available RAM and disk space

2. **Process Info**:
   - Task Manager screenshot showing leftover processes
   - PIDs and process names
   - How long processes have been running

3. **Logs**:
   - Attach relevant log files from `%APPDATA%\Aura\logs`
   - Include timestamps of the failed test

4. **Reproduction**:
   - Exact steps to reproduce
   - Which scenario failed
   - How often it fails (always, sometimes, rarely)

---

## Changelog

- **v1.0** (2025-01-19): Initial verification guide
  - Created 5 test scenarios
  - Added automated scripts
  - Documented expected behavior
