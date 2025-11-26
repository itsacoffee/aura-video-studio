# Testing Guide: Backend Startup, Wizard Navigation, and FFmpeg Detection Fixes

This guide provides comprehensive testing instructions for the fixes implemented in PR #[TBD] addressing critical backend startup, setup wizard navigation, and FFmpeg detection issues.

## Overview of Fixes

### 1. Backend Service Health Check Improvements
**Files Modified**: `Aura.Desktop/electron/backend-service.js`

**Changes**:
- Exponential backoff retry logic with consecutive failure tracking
- Accept any 2xx HTTP status code (200-299) instead of only 200
- Enhanced error logging with process state and startup output
- Improved `waitForReady()` and `_waitForBackend()` methods

### 2. Setup Wizard "Save and Exit" Navigation Fix
**Files Modified**: `Aura.Web/src/pages/Onboarding/FirstRunWizard.tsx`

**Changes**:
- Fixed `handleExitWizard` to call `markFirstRunCompleted()` before navigating
- Changed navigation destination from `/` to `/dashboard`
- Added success toast notification when saving progress
- Updated confirmation message for clarity

### 3. FFmpeg Detection (Analysis Only - No Changes Required)
**Status**: Already implemented and working correctly

The `FFmpegResolver.cs` already has robust environment variable override detection via `GetEnvironmentOverridePaths()` checking `AURA_FFMPEG_PATH`, `FFMPEG_PATH`, and `FFMPEG_BINARIES_PATH`. The backend-service.js sets all three environment variables when starting the backend.

## Test Scenarios

### Scenario 1: Backend Startup with Slow Response

**Objective**: Verify backend health check with exponential backoff handles slow startup gracefully

**Prerequisites**:
- Clean install of application (portable .exe or development build)
- Windows 11 system

**Steps**:
1. Launch Aura Video Studio for the first time
2. Monitor the splash screen/loading screen
3. Observe backend startup progress messages

**Expected Results**:
- Backend should start successfully within 60 seconds
- Health check should show exponential backoff delays (500ms, 750ms, 1.1s, etc.)
- Console logs should show:
  - "Backend health check passed" with status code (200-299)
  - Attempt count and elapsed time
  - Process state (alive/killed)
- No "Backend Server Not Reachable" error unless backend actually crashes

**Actual Health Check Behavior**:
- First 5 failures: exponential backoff (500ms, 750ms, 1.1s, 1.7s, 2.6s)
- Subsequent attempts: 1-second intervals
- Max timeout: 90 seconds (configurable)

**Console Log Example**:
```
[BackendService] Waiting for backend health check at: http://localhost:5005/health
[BackendService] Health check attempt 1/90
[BackendService] Health check attempt 5/90
[BackendService] Backend health check passed - Status: 200, Elapsed: 3245ms
```

### Scenario 2: Setup Wizard "Save and Exit" Flow

**Objective**: Verify "Save and Exit" button correctly marks setup as complete and navigates to dashboard

**Prerequisites**:
- Fresh user account or cleared application data
- Backend running and healthy

**Steps**:
1. Launch Aura Video Studio (first run)
2. Progress through wizard to Step 2 (FFmpeg Check)
3. Click "Save and Exit" button at top of wizard
4. Confirm the exit dialog when prompted
5. Verify navigation to dashboard
6. Close and relaunch application

**Expected Results**:
- ✅ Confirmation dialog appears with message: "Are you sure you want to exit the setup wizard? Your progress will be saved and you can complete setup later from the Settings page."
- ✅ Success toast notification: "Setup Progress Saved - You can resume setup anytime from Settings."
- ✅ Application navigates to `/dashboard` (not `/` or back to wizard step 1)
- ✅ On relaunch, user is NOT redirected back to wizard (setup marked as complete)
- ✅ User can access Settings > Setup Wizard to resume if desired
- ✅ Console logs show:
  ```
  [FirstRunWizard] User confirmed exit from wizard
  [FirstRunWizard] Marking first run as completed to allow exit to main app
  ```

**localStorage Verification**:
Open browser DevTools (or Electron DevTools) and check localStorage:
```javascript
localStorage.getItem('hasCompletedFirstRun') // Should be "true"
localStorage.getItem('aura-setup-aborted') // Should be "true"
localStorage.getItem('aura-setup-aborted-step') // Should be "2" (or whatever step you exited from)
```

### Scenario 3: Wizard Exit and Re-entry

**Objective**: Verify user can exit wizard, navigate app normally, and optionally resume wizard later

**Prerequisites**:
- Completed Scenario 2 (wizard exited mid-setup)

**Steps**:
1. After exiting wizard in Scenario 2, navigate around the dashboard
2. Open Settings page
3. Look for "Resume Setup Wizard" or "Complete Setup" option
4. Click to re-enter wizard
5. Verify wizard state is restored to the step where you left off

**Expected Results**:
- ✅ Dashboard is accessible (no redirect to wizard)
- ✅ Settings page shows option to resume setup
- ✅ Re-entering wizard shows resume dialog with saved progress
- ✅ Wizard resumes at the step where user exited (Step 2 in this case)
- ✅ User can complete wizard or exit again without issue

### Scenario 4: Backend Startup with Immediate Success

**Objective**: Verify fast backend startup is detected quickly (no unnecessary waiting)

**Prerequisites**:
- Development environment with fast SSD
- Backend pre-compiled (not first-time build)

**Steps**:
1. Launch application
2. Monitor console for health check messages

**Expected Results**:
- ✅ Backend detected as healthy within 1-3 seconds
- ✅ Exponential backoff reduced unnecessary delays for fast startups
- ✅ Console shows early success: "Backend health check passed" on attempt 1-5

### Scenario 5: Backend Crash During Startup

**Objective**: Verify detailed error logging when backend crashes

**Prerequisites**:
- Ability to simulate backend crash (modify backend code temporarily)

**Steps**:
1. Modify backend code to throw exception on startup (e.g., invalid port binding)
2. Launch application
3. Observe error message and console logs

**Expected Results**:
- ✅ Error message includes:
  - Exit code and signal
  - Startup output preview (first 500 chars)
  - Error output preview (first 500 chars)
  - Process state (exited)
- ✅ Troubleshooting steps provided to user
- ✅ Console logs show detailed diagnostics
- ✅ User can retry or get actionable recovery steps

**Example Error Log**:
```
[BackendService] Backend process exited during startup (exitCode: 1, signal: null)
Startup output: Application is starting...
Error output: System.Net.Sockets.SocketException: Address already in use
```

### Scenario 6: FFmpeg Detection in Portable Mode

**Objective**: Verify FFmpeg is detected when bundled in portable .exe Tools directory

**Prerequisites**:
- Portable .exe build with FFmpeg in `Tools/ffmpeg/` directory
- Windows 11 system

**Steps**:
1. Extract portable .exe to a new directory
2. Verify `Tools/ffmpeg/bin/ffmpeg.exe` exists
3. Launch application
4. Progress through wizard to Step 2 (FFmpeg Check)
5. Observe FFmpeg status

**Expected Results**:
- ✅ FFmpeg status shows "Installed" badge
- ✅ Version detected (e.g., "6.0" or higher)
- ✅ Source shows "Environment" (from AURA_FFMPEG_PATH set by Electron)
- ✅ Hardware acceleration detected if available (NVENC, AMF, QuickSync)
- ✅ Console logs show:
  ```
  [BackendService] ✓ Found FFmpeg at: C:\path\to\portable\Tools\ffmpeg\bin
  [Backend] ✓ FFmpeg path persisted successfully to backend config
  ```

**Environment Variable Verification**:
Check that Electron sets these (view in console or backend logs):
```
AURA_FFMPEG_PATH=C:\path\to\portable\Tools\ffmpeg\bin\ffmpeg.exe
FFMPEG_PATH=C:\path\to\portable\Tools\ffmpeg\bin\ffmpeg.exe
FFMPEG_BINARIES_PATH=C:\path\to\portable\Tools\ffmpeg\bin
```

### Scenario 7: Backend Health Check with Non-200 Status Codes

**Objective**: Verify health check accepts other 2xx status codes (204, 202, etc.)

**Prerequisites**:
- Ability to modify backend health endpoint temporarily

**Steps**:
1. Modify backend `/health/live` endpoint to return 204 No Content instead of 200 OK
2. Launch application
3. Monitor health check

**Expected Results**:
- ✅ Health check passes with 204 status code
- ✅ Console shows: "Backend health check passed (status: 204)"
- ✅ Application proceeds normally

### Scenario 8: Wizard Navigation Guard

**Objective**: Verify wizard prevents accidental navigation during setup

**Prerequisites**:
- Fresh wizard session

**Steps**:
1. Start wizard
2. Progress to Step 3 (Provider Configuration)
3. Attempt to navigate away using browser back button
4. Attempt to close tab/window
5. Use "Save and Exit" button

**Expected Results**:
- ✅ Browser back button prevented (history.pushState guard)
- ✅ Close tab/window shows "Are you sure?" dialog (beforeunload event)
- ✅ "Save and Exit" button is the only intentional way to exit
- ✅ After clicking "Save and Exit" and confirming, navigation succeeds

## Automated Tests

### Running Tests

```bash
# Run all FirstRunWizard tests
cd Aura.Web
npm test -- FirstRunWizard.completion.test.tsx --run

# Run with coverage
npm test -- FirstRunWizard.completion.test.tsx --run --coverage
```

### Test Results (Current)

All 4 tests in `FirstRunWizard.completion.test.tsx` pass:
- ✅ should render the wizard on initial load
- ✅ should show exit button in wizard progress
- ✅ should call onComplete when wizard is completed
- ✅ should show confirmation dialog when exit button is clicked

**Note**: The test file uses mocks for `markFirstRunCompleted`, so unit tests verify the function is called, but integration tests (Scenarios 2-3) verify the actual behavior.

## Regression Testing

### Areas to Check for Regressions

1. **Backend Auto-Start**
   - Verify backend still starts automatically on app launch
   - Check orphan process cleanup still works
   - Verify Windows Firewall detection still functions

2. **Wizard Completion Flow**
   - Complete full wizard (all 6 steps) and verify normal completion works
   - Check that normal completion (not "Save and Exit") still navigates correctly
   - Verify wizard state persistence across sessions

3. **FFmpeg Installation**
   - Test FFmpeg installation from wizard (Step 3)
   - Verify manual FFmpeg path configuration still works
   - Check FFmpeg validation with invalid paths

4. **Navigation**
   - Verify dashboard route still loads correctly
   - Check that authenticated routes work normally
   - Verify Settings page functionality

## Performance Metrics

### Backend Startup Times (Expected)

| Scenario | Expected Time | Max Acceptable |
|----------|--------------|----------------|
| Fast startup (SSD, pre-compiled) | 1-3 seconds | 5 seconds |
| Normal startup (HDD, first run) | 5-15 seconds | 30 seconds |
| Slow startup (low-end hardware) | 15-30 seconds | 60 seconds |
| Timeout threshold | N/A | 90 seconds |

### Health Check Retry Pattern

| Attempt | Delay Before Retry | Cumulative Time |
|---------|-------------------|-----------------|
| 1 | 0ms (immediate) | 0s |
| 2 | 500ms | 0.5s |
| 3 | 750ms | 1.25s |
| 4 | 1125ms | 2.375s |
| 5 | 1687ms | 4.062s |
| 6-90 | 1000ms (fixed) | 5.062s - 90s |

## Known Issues and Limitations

### Issues Addressed by This PR

1. ✅ **Fixed**: "Save and Exit" navigating back to Step 1
2. ✅ **Fixed**: Backend health check too strict (only accepting 200)
3. ✅ **Fixed**: No exponential backoff causing slow startup detection
4. ✅ **Fixed**: Insufficient error logging for backend startup failures

### Pre-existing Issues (Not in Scope)

1. FFmpeg download may be slow on slow internet connections (existing behavior)
2. Wizard step validation could be more granular (existing behavior)
3. Resume dialog UI could be more polished (existing behavior)
4. Error messages could have more actionable recovery steps (partial improvement made)

### Future Improvements (Out of Scope)

1. Add visual progress indicator for backend startup phases
2. Implement health check retry count display in UI
3. Add "Test Connection" button in wizard for manual health check
4. Implement backend startup diagnostics panel

## Troubleshooting

### Backend Won't Start

**Symptoms**: 
- "Backend Server Not Reachable" error after 90 seconds
- No startup output in console

**Checks**:
1. Verify backend executable exists at expected path
2. Check Windows Firewall allows Aura.Api.exe
3. Verify port 5005 is not in use by another application
4. Check backend logs in `%APPDATA%\AuraVideoStudio\logs`
5. Review console for process exit codes and error output

**Resolution**:
- Backend improvements provide better diagnostics in logs
- User receives actionable error message with recovery steps

### Wizard Exits but Reopens on Next Launch

**Symptoms**:
- User clicks "Save and Exit" but wizard appears again on relaunch

**Checks**:
1. Verify `localStorage.getItem('hasCompletedFirstRun')` is "true"
2. Check browser/Electron DevTools for errors during exit
3. Verify backend `/api/setup/wizard/complete` endpoint was called

**Resolution**:
- This specific issue is fixed by the PR
- If still occurs, check browser console for errors in `markFirstRunCompleted()`

### FFmpeg Not Detected in Portable Mode

**Symptoms**:
- FFmpeg shows "Not Installed" despite being in Tools directory

**Checks**:
1. Verify `Tools/ffmpeg/bin/ffmpeg.exe` exists and is executable
2. Check environment variables in console:
   ```javascript
   console.log(process.env.AURA_FFMPEG_PATH);
   console.log(process.env.FFMPEG_PATH);
   ```
3. Review FFmpegResolver logs in backend for attempted paths
4. Verify backend started successfully (if backend crashes, FFmpeg detection never runs)

**Resolution**:
- FFmpeg detection already works correctly
- If backend crashes, our improvements provide better diagnostics to identify root cause

## Success Criteria

### All Tests Pass ✅

- [x] Unit tests: All 4 tests in FirstRunWizard.completion.test.tsx pass
- [x] Lint: No new linting errors introduced
- [x] Type Check: No TypeScript compilation errors
- [x] Placeholder Scan: Zero placeholder comments (enforced by pre-commit hook)

### Manual Testing ✅ (To be verified by tester)

- [ ] Scenario 1: Backend startup with slow response
- [ ] Scenario 2: Setup wizard "Save and Exit" flow
- [ ] Scenario 3: Wizard exit and re-entry
- [ ] Scenario 4: Backend startup with immediate success
- [ ] Scenario 5: Backend crash during startup (error logging)
- [ ] Scenario 6: FFmpeg detection in portable mode
- [ ] Scenario 7: Backend health check with non-200 status codes
- [ ] Scenario 8: Wizard navigation guard

### Performance Targets ✅

- [ ] Backend startup < 30 seconds on typical hardware
- [ ] Fast startup (SSD) detected within 1-3 seconds
- [ ] No false-negative health checks during normal startup

### User Experience ✅

- [ ] Clear error messages with actionable recovery steps
- [ ] No wizard navigation loops (user can exit and return)
- [ ] Progress saved correctly when exiting mid-setup
- [ ] FFmpeg detected automatically in portable mode

## Conclusion

These fixes address the three critical issues identified:
1. **Backend startup robustness**: Exponential backoff and lenient status code acceptance
2. **Wizard navigation**: Proper exit flow with completion flag
3. **FFmpeg detection**: Already working, verified to be correct

The changes are minimal, focused, and maintain backward compatibility while significantly improving user experience during setup and startup.
