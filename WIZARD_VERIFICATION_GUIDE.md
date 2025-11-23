# Setup Wizard Verification & Testing Guide

## Overview

This document provides comprehensive verification procedures for the Aura Video Studio setup wizard to ensure 100% reliability.

## Changes Made

### 1. ✅ Added Retry Logic to Wizard Completion (CRITICAL)

**File**: `Aura.Web/src/state/onboarding.ts`

Added 3-retry mechanism with 2-second delays for `completeWizardInBackend()`:

- Retries network errors and timeouts automatically
- Logs each attempt for debugging
- Returns false only after all retries exhausted
- Includes workspace preferences in the saved state

**Why**: Prevents wizard exit failures due to temporary network issues or backend slowness.

### 2. ✅ Enhanced Wizard Completion Logging

**File**: `Aura.Web/src/pages/Onboarding/FirstRunWizard.tsx`

Added step-by-step logging with clear status indicators:

- 🚀 Starting onboarding completion
- ✅ Step 1/3: Configuration validated
- ✅ Step 2/3: Backend wizard completion saved
- ✅ Step 3/3: Local first-run status updated
- 🎉 ALL STEPS COMPLETE

**Why**: Makes debugging wizard issues much easier with clear progress tracking.

### 3. ✅ Improved Health Check Logging

**File**: `Aura.Web/src/components/Onboarding/BackendStatusBanner.tsx`

Added detailed logging for health checks:

- ✅ Backend health check passed (with response time)
- ⚠️ Backend health check failed (with reason)
- ❌ Backend health check threw exception

**Why**: Helps diagnose backend connectivity issues during wizard.

### 4. ✅ Added Database Fallback (CRITICAL)

**File**: `Aura.Web/src/services/api/setupApi.ts`

If backend API fails, falls back to localStorage check:

- Checks `hasCompletedFirstRun` flag
- Checks `hasSeenOnboarding` flag
- Returns `isComplete: true` if either flag is set

**Why**: Prevents users from being trapped in wizard if backend is temporarily unavailable.

---

## Testing Procedures

### Test 1: Normal Wizard Completion Flow

**Expected**: Wizard completes successfully and exits to main app

1. Launch portable .exe on fresh Windows machine
2. Wait for backend to start (up to 60 seconds)
3. Complete all 6 wizard steps:
   - Step 0: Welcome - Click "Get Started"
   - Step 1: FFmpeg Check - Click "Next" (with or without FFmpeg)
   - Step 2: FFmpeg Install - Skip or install FFmpeg
   - Step 3: Provider Configuration - Configure at least one provider or skip
   - Step 4: Workspace Setup - Configure paths
   - Step 5: Complete - Click "Save"
4. **Verify**: App exits to dashboard (NOT back to Step 1)
5. **Verify**: Restarting app does NOT show wizard again

**Console Log Verification**:

```
[FirstRunWizard] 🚀 Starting onboarding completion...
[FirstRunWizard] Step 1/3: Validating setup configuration...
[FirstRunWizard] ✅ Step 1/3 complete: Configuration validated
[FirstRunWizard] Step 2/3: Marking wizard as complete in backend...
[Wizard Persistence] Completing wizard in backend (attempt 1/3)
[Wizard Persistence] ✅ Wizard completed successfully in backend
[FirstRunWizard] ✅ Step 2/3 complete: Backend wizard completion saved
[FirstRunWizard] Step 3/3: Marking first run complete locally...
[FirstRunWizard] ✅ Step 3/3 complete: Local first-run status updated
[FirstRunWizard] 🎉 ALL STEPS COMPLETE - Wizard finished successfully!
```

---

### Test 2: Backend Temporary Failure During Completion

**Expected**: Wizard retries and succeeds

1. Complete wizard up to final step
2. Temporarily disconnect network OR pause backend process
3. Click "Save"
4. **Verify**: Console shows retry attempts:
   ```
   [Wizard Persistence] Completing wizard in backend (attempt 1/3)
   [Wizard Persistence] ❌ Attempt 1/3 failed: Network Error
   [Wizard Persistence] Retrying in 2000ms...
   [Wizard Persistence] Completing wizard in backend (attempt 2/3)
   ```
5. Restore network OR unpause backend
6. **Verify**: Wizard completes successfully on retry

---

### Test 3: Backend Permanent Failure During Completion

**Expected**: Clear error message, ability to retry

1. Complete wizard up to final step
2. Stop backend process completely
3. Click "Save"
4. **Verify**: Error toast appears:
   - Title: "Setup Completion Failed"
   - Message: "Failed to save completion status to backend after multiple attempts..."
5. **Verify**: Wizard stays on Step 6 (does NOT loop to Step 1)
6. **Verify**: User can click "Exit Wizard" to leave
7. Start backend and click "Save" again
8. **Verify**: Completion succeeds

---

### Test 4: Backend Slow Response (Health Check)

**Expected**: Wizard waits patiently for backend

1. Launch app on slow machine or with heavy CPU load
2. **Verify**: "Starting Backend Server..." message shows
3. **Verify**: "Waiting for Backend Server..." shows with attempt counter
4. Wait up to 60 seconds
5. **Verify**: Health check eventually succeeds
6. **Verify**: Wizard loads without "Backend not reachable" error

**Console Log Verification**:

```
[BackendStatusBanner] Auto-retrying backend health check
[BackendStatusBanner] ⚠️ Backend health check failed (attempt 15/60)
[BackendStatusBanner] ✅ Backend health check passed (attempt 22/60)
```

---

### Test 5: API Key Configuration Persistence

**Expected**: API keys are saved and retrievable after wizard

1. Complete wizard with Ollama or OpenAI configured
2. Verify wizard completes successfully
3. Open Settings → API Keys
4. **Verify**: Previously configured API keys are present
5. **Verify**: Keys work for script generation

---

### Test 6: FFmpeg Configuration Persistence

**Expected**: FFmpeg path is saved and usable

1. Configure FFmpeg in wizard (managed or manual)
2. Complete wizard
3. Navigate to Create → Generate Video
4. **Verify**: FFmpeg is detected and usable
5. Check Settings → Dependencies
6. **Verify**: FFmpeg path is shown correctly

---

### Test 7: Exit Wizard Mid-Setup

**Expected**: Progress is saved, can resume later

1. Start wizard and complete Steps 1-3
2. Click "Exit Wizard"
3. Confirm exit
4. **Verify**: App navigates to dashboard
5. Open Settings → Setup Wizard
6. **Verify**: Can resume from where you left off

---

### Test 8: Theme Preview in Step 5

**Expected**: Theme changes immediately when clicked

1. Navigate to Step 5 (Workspace Setup)
2. Click "Light" theme
3. **Verify**: Page immediately changes to light theme
4. Click "Dark" theme
5. **Verify**: Page immediately changes to dark theme
6. Click "Auto" theme
7. **Verify**: Page follows system theme

---

### Test 9: Auto-Save Indicator

**Expected**: Shows occasionally, not constantly

1. Navigate through wizard steps
2. Make changes to configuration
3. **Verify**: "Saving progress..." appears briefly (2 seconds)
4. **Verify**: Indicator disappears after save complete
5. **Verify**: Indicator does NOT spam constantly
6. **Verify**: Maximum frequency: once every 5 seconds

---

### Test 10: FFmpeg Check in Step 3

**Expected**: Checks once, does not spam

1. Navigate to Step 3 (FFmpeg Install)
2. **Verify**: "Checking..." badge appears briefly
3. **Verify**: Status resolves to "Installed" or "Not Installed"
4. **Verify**: "Checking..." does NOT appear indefinitely
5. **Verify**: Status is stable (doesn't keep refreshing)

---

## Database Verification

### Check UserSetup Table

After wizard completion, verify database state:

```sql
-- SQLite query (use DB Browser for SQLite)
SELECT * FROM user_setup WHERE user_id = 'default';

-- Expected result:
-- id: (guid)
-- user_id: default
-- completed: 1 (true)
-- completed_at: (timestamp)
-- version: 1.0.0
-- last_step: 6
-- wizard_state: (JSON with apiKeys, workspacePreferences)
```

---

## Common Issues & Solutions

### Issue: "Backend Server Not Reachable" on Startup

**Solution**: Wait up to 60 seconds - backend can take time to start
**Verification**: Check console for health check retries

### Issue: Wizard loops back to Step 1 after clicking "Save"

**Root Cause**: Backend completion failed
**Solution**: Check backend logs for errors, verify database write permissions
**Verification**: Check console for "❌ Failed to mark wizard as complete"

### Issue: Settings not persisting after wizard

**Root Cause**: Database write failed but wizard marked complete locally
**Solution**: Clear localStorage and re-run wizard
**Verification**: Check `%APPDATA%\aura-video-studio\aura.db` exists and has user_setup table

---

## Success Criteria

✅ All 10 test cases pass  
✅ No wizard exit loop  
✅ No backend connectivity false positives  
✅ Settings persist after completion  
✅ Theme preview works  
✅ Auto-save is non-intrusive  
✅ FFmpeg check is stable  
✅ Comprehensive logging aids debugging  
✅ Retry logic handles network issues  
✅ Fallback prevents wizard trap

---

## Rollback Plan

If critical issues are found:

1. Revert `Aura.Web/src/state/onboarding.ts` (completeWizardInBackend function)
2. Revert `Aura.Web/src/pages/Onboarding/FirstRunWizard.tsx` (completion logging)
3. Revert `Aura.Web/src/services/api/setupApi.ts` (fallback logic)
4. Rebuild and redeploy

---

## Monitoring & Telemetry

Key metrics to track:

- Wizard completion rate (should be >95%)
- Average time to complete (should be <5 minutes)
- Backend connection failure rate during wizard (should be <1%)
- Wizard exit loop occurrences (should be 0%)

---

## Next Steps

1. Run all 10 test cases on clean Windows machine
2. Monitor backend logs during testing
3. Verify database state after each test
4. Check browser console for error patterns
5. Test on multiple machines (fast/slow hardware)
6. Test with flaky network conditions

---

## Contact

For issues or questions about wizard functionality:

- Check console logs first (F12)
- Check backend logs: `%APPDATA%\aura-video-studio\logs\`
- Review database: `%APPDATA%\aura-video-studio\aura.db`
