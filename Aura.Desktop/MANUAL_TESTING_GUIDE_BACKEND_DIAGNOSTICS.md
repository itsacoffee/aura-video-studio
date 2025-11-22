# Manual Testing Guide - Backend Startup Diagnostics

## Prerequisites

- Windows 11 test machine
- Admin access (for firewall/port tests)
- .NET 8.0 SDK installed (for baseline testing)

## Test Scenarios

### 1. Happy Path - Normal Startup

**Goal**: Verify normal startup still works

**Steps**:
1. Install Aura Video Studio fresh
2. Launch application
3. Observe startup process

**Expected**:
- Backend starts successfully
- Console shows:
  ```
  [BackendService] Running pre-startup validation checks...
  [BackendService] ✓ .NET runtime validated: 8.x.x
  [BackendService] ✓ Backend executable validated: <path>
  [BackendService] ✓ Port 5005 is available
  [BackendService] ✓ FFmpeg validated: <path>
  [BackendService] Pre-startup validation complete
  [BackendService] ✓ Backend process spawned (PID: xxxx)
  [BackendService] ✓ Backend health check passed
  [BackendService] ✓ Backend started successfully
  ```
- Application loads normally
- No error dialogs

### 2. Missing .NET Runtime

**Goal**: Test DOTNET_MISSING error classification

**Steps**:
1. Uninstall .NET 8.0 Runtime (keep only .NET 6 or 7)
2. Launch Aura Video Studio
3. Observe error dialog

**Expected Error Dialog**:
- Title: "Backend Error: DOTNET MISSING"
- What went wrong: ".NET runtime not found or incompatible version"
- Recovery Actions: "Install .NET 8.0 Runtime from: https://dotnet.microsoft.com/download/dotnet/8.0"
- Technical Details: Shows detected version or "not found"

**Verification**:
- Error is clear and actionable
- URL is clickable/copyable
- No application crash
- Can close dialog and exit cleanly

### 3. Port Conflict

**Goal**: Test PORT_CONFLICT error classification

**Steps**:
1. Open Command Prompt as Administrator
2. Run: `python -m http.server 5005` (or any tool that binds to port 5005)
3. Launch Aura Video Studio
4. Observe error dialog

**Expected Error Dialog**:
- Title: "Backend Error: PORT CONFLICT"
- What went wrong: "Port 5005 is already in use"
- Shows: "Port 5005 is in use by: python.exe (PID: xxxx)"
- Recovery Actions: "Close any other applications using port 5005 and try again"

**Verification**:
- Process name is correctly identified
- PID is shown
- User can identify and close conflicting application

### 4. Missing Backend Executable

**Goal**: Test MISSING_EXECUTABLE error classification

**Steps**:
1. Install Aura Video Studio
2. Navigate to installation folder
3. Delete or rename `resources/backend/win-x64/Aura.Api.exe`
4. Launch application

**Expected Error Dialog**:
- Title: "Backend Error: MISSING EXECUTABLE"
- What went wrong: "Backend executable not found"
- Recovery Actions: "The application may not be properly installed. Try reinstalling Aura Video Studio."
- Technical Details: Shows expected path where file should be

**Verification**:
- Path shown is correct
- Message clearly indicates reinstallation needed
- No confusing technical jargon

### 5. Corrupted Backend Executable

**Goal**: Test executable validation (small file size)

**Steps**:
1. Install Aura Video Studio
2. Replace `resources/backend/win-x64/Aura.Api.exe` with an empty file
3. Launch application

**Expected**:
- Error dialog indicates file corruption
- Suggests reinstallation

### 6. Retry Logic - Transient Failure

**Goal**: Test automatic retry recovery

**Steps**:
1. Temporarily block port 5005 in Windows Firewall (inbound rule)
2. Launch Aura Video Studio
3. Quickly remove firewall rule during startup (within first 10 seconds)
4. Observe console and behavior

**Expected**:
- Console shows: "[BackendService] Retry attempt 2/3..."
- If firewall removed in time: App starts successfully on retry
- If not: Shows BINDING_FAILED error after 3 attempts

**Verification**:
- Retry attempts are logged
- Exponential backoff is visible (1s, 2s, 4s delays)
- Eventually succeeds or fails gracefully

### 7. Slow Startup (Timeout)

**Goal**: Test STARTUP_TIMEOUT error classification

**Setup**:
This is difficult to test without modifying the backend. Alternative:
1. Reduce timeout in backend-service.js temporarily (for testing only)
2. Change `BACKEND_STARTUP_TIMEOUT` from 60000 to 5000
3. Launch application

**Expected Error Dialog**:
- Title: "Backend Error: STARTUP TIMEOUT"
- What went wrong: "Backend is taking too long to start"
- Recovery Actions: "The backend is taking too long to start. Check system resources and try again."
- Technical Details: Shows health check attempts and last error

### 8. Backend Crash During Startup

**Goal**: Test PROCESS_CRASHED error classification

**Setup**:
Difficult to simulate without breaking the backend. Skip for manual testing unless you can inject a crash.

### 9. Windows Firewall Blocking

**Goal**: Test BINDING_FAILED error classification

**Steps**:
1. Add Windows Firewall rule to block Aura.Api.exe
2. Launch Aura Video Studio
3. Observe error

**Expected Error Dialog**:
- Title: "Backend Error: BINDING FAILED"
- Recovery Actions: "The backend could not bind to the network port. Check Windows Firewall settings."

### 10. Multiple Rapid Launches

**Goal**: Test orphan cleanup and port release

**Steps**:
1. Launch Aura Video Studio
2. Immediately close it (before fully loaded)
3. Immediately launch again
4. Repeat 2-3 times

**Expected**:
- Console shows: "[OrphanDetection] Checking for orphaned backend..."
- Old processes are cleaned up
- New instance starts successfully
- No port conflict errors

## Verification Checklist

After testing all scenarios:

- [ ] All error categories display correctly
- [ ] Error messages are user-friendly (no technical jargon)
- [ ] Recovery actions are specific and actionable
- [ ] Technical details are available for advanced users
- [ ] Log file locations are shown
- [ ] No application crashes on any error
- [ ] Retry logic works for transient failures
- [ ] Port conflict shows correct process info
- [ ] .NET version check is accurate
- [ ] Executable validation catches corruption
- [ ] Orphan process cleanup works reliably

## Performance Benchmarks

Measure and record:

- Normal startup time: ______ seconds
- Startup time with port conflict (fail fast): ______ seconds
- Startup time with missing .NET (fail fast): ______ seconds
- Startup time with 1 retry (transient failure): ______ seconds
- Startup time with 3 retries (persistent failure): ______ seconds

**Target**: Pre-validation should add < 2 seconds to startup time

## Regression Testing

Verify nothing broke:

- [ ] Development mode still works (`electron . --dev`)
- [ ] Production build still works
- [ ] Backend health checks work
- [ ] FFmpeg detection still works
- [ ] First-run wizard appears on fresh install
- [ ] System tray functions correctly
- [ ] Application menu works
- [ ] Job queue and video generation work

## Known Limitations

1. **Firewall Detection**: Cannot automatically detect if Windows Firewall is blocking. User must check manually.
2. **External Backend**: Fallback to external backend not implemented in this PR
3. **Auto-Repair**: Cannot automatically fix issues (e.g., install .NET)
4. **Crash Detection**: Cannot distinguish between different types of backend crashes

## Reporting Results

For each test scenario, record:
- Pass/Fail
- Screenshot of error dialog (if error expected)
- Console log snippet (first 50 lines and last 50 lines)
- Any unexpected behavior
- Performance measurements

Create GitHub issue with:
- Title: "Manual Test Results - Backend Startup Diagnostics"
- Label: "testing"
- Body: Test results, screenshots, logs, observations

## Rollback Plan

If critical issues found:
1. Revert commit: `git revert <commit-hash>`
2. Document specific failure case
3. Fix in new PR with additional test coverage
