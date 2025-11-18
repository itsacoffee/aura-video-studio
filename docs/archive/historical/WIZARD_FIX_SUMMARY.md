# Onboarding Wizard Network Errors and Navigation Fixes

## Problem Statement

The first-run onboarding wizard was experiencing multiple critical issues:

1. **Step 2 (FFmpeg Check)**: Always shows "Network Error"
2. **Step 3 (Provider Configuration)**: Shows "Network Error" for API validation  
3. **Step 4**: Broken entirely with persistent errors
4. **"Go to Main App" button**: Does nothing when clicked in Step 6

## Root Cause Analysis

### Circuit Breaker Persistence Issue (PRIMARY ROOT CAUSE)

The application uses a circuit breaker pattern to prevent cascading failures when the API is unavailable. However, this circuit breaker state was being persisted to `localStorage`, causing false positives:

**Problem Flow**:
1. User experiences some API failures (could be from previous session, network issues, etc.)
2. Circuit breaker opens and saves state to localStorage
3. User closes and reopens the application
4. Circuit breaker loads **stale "OPEN" state** from localStorage
5. ALL API calls are immediately blocked without even attempting the request
6. User sees "Network Error" even though the API is actually available

**Evidence**:
- `circuitBreakerPersistence.ts` saves circuit breaker state to localStorage
- `apiClient.ts` loads this state on initialization (line 65-77)
- `FirstRunWizard.tsx` tried to clear circuit breaker on mount (line 210-213), but this happened **AFTER** the first API call attempts
- Timing issue: Circuit breaker was checked before the clear operation completed

### Navigation State Update Issue

The "Go to Main App" button called `markFirstRunCompleted()` before updating `shouldShowOnboarding` state, causing a race condition where the app would try to reload the wizard.

## Fixes Implemented

### Fix 1: Clear Circuit Breaker BEFORE First-Run Check (CRITICAL)

**File**: `Aura.Web/src/App.tsx`

**Change**: Clear circuit breaker state at the earliest possible moment:

```typescript
// BEFORE (in FirstRunWizard.tsx - TOO LATE):
useEffect(() => {
  // ... other setup code ...
  PersistentCircuitBreaker.clearState();
  resetCircuitBreaker();
  console.info('[FirstRunWizard] Circuit breaker state cleared on mount');
}, []);

// AFTER (in App.tsx - EARLY ENOUGH):
async function checkFirstRun() {
  try {
    // CRITICAL: Clear circuit breaker state BEFORE checking first run
    PersistentCircuitBreaker.clearState();
    resetCircuitBreaker();
    console.info('[App] Circuit breaker state cleared before first-run check');
    
    // Now safe to make API calls...
    const systemStatus = await setupApi.getSystemStatus();
    // ...
  }
}
```

**Why This Works**:
- Clears stale circuit breaker state BEFORE any API calls are attempted
- Happens in App.tsx during initial application load
- Prevents wizard from even seeing the old circuit breaker state

### Fix 2: Bypass Circuit Breaker for Setup/FFmpeg APIs

**Files**: 
- `Aura.Web/src/services/api/setupApi.ts`
- `Aura.Web/src/services/api/ffmpegClient.ts`

**Change**: All setup and FFmpeg API calls now explicitly skip the circuit breaker:

```typescript
// Add type import
import apiClient, { type ExtendedAxiosRequestConfig } from './apiClient';

// Use _skipCircuitBreaker config
async getSystemStatus(): Promise<SystemSetupStatus> {
  const config: ExtendedAxiosRequestConfig = {
    _skipCircuitBreaker: true,  // Skip circuit breaker for setup APIs
  };
  const response = await apiClient.get<SystemSetupStatus>(
    '/api/setup/system-status', 
    config
  );
  return response.data;
}
```

**Why This Works**:
- Setup and FFmpeg APIs are **critical for first-run setup**
- They should always be attempted, regardless of circuit breaker state
- `_skipCircuitBreaker` flag bypasses the circuit breaker check in apiClient.ts
- Even if circuit breaker is triggered elsewhere, setup still works

**Applied to**:
- `setupApi.getSystemStatus()`
- `setupApi.completeSetup()`
- `setupApi.checkFFmpeg()`
- `setupApi.checkDirectory()`
- `ffmpegClient.getStatus()`
- `ffmpegClient.install()`
- `ffmpegClient.rescan()`
- `ffmpegClient.useExisting()`

### Fix 3: Navigation State Update Order

**File**: `Aura.Web/src/App.tsx`

**Change**: Update state before marking completion:

```typescript
// BEFORE:
onComplete={async () => {
  setShouldShowOnboarding(false);
  await markFirstRunCompleted();
}}

// AFTER:
onComplete={async () => {
  console.info('[App] FirstRunWizard onComplete called');
  await markFirstRunCompleted();
  setShouldShowOnboarding(false);  // Update state AFTER backend persistence
  console.info('[App] Transitioning to main app');
}}
```

**Why This Works**:
- Ensures backend persistence completes first
- Then updates React state to trigger main app render
- Logging helps track the transition
- Prevents race conditions between state updates

## Testing Instructions

### Prerequisites
1. Clear all browser data (localStorage, sessionStorage, cookies)
2. Or use incognito/private browsing mode
3. Ensure backend API is running (usually on port 5005)

### Test Case 1: Fresh Install with Circuit Breaker Clear

**Steps**:
1. Open browser DevTools → Console tab
2. Open Application tab → localStorage → Clear all
3. Navigate to the application
4. Watch console for: `[App] Circuit breaker state cleared before first-run check`
5. Proceed through wizard steps

**Expected Behavior**:
- Console shows circuit breaker cleared message
- No "Network Error" messages in any step
- All API calls succeed

**What to Check**:
- Console logs show circuit breaker reset
- Network tab shows successful API calls (Status 200)
- No red error messages in UI

### Test Case 2: Step 2 - FFmpeg Check

**Steps**:
1. Start wizard
2. Click through to Step 2 (FFmpeg Check)
3. Observe FFmpeg status check

**Expected Behavior**:
- FFmpeg status loads without "Network Error"
- Either shows "FFmpeg not found" (if not installed) OR "FFmpeg detected" (if installed)
- Install/Rescan/Validate buttons work without errors

**What to Check**:
- Network tab: `GET /api/system/ffmpeg/status` returns 200
- No circuit breaker errors in console
- UI shows actual FFmpeg status (not network error)

### Test Case 3: Step 3 - Provider Configuration

**Steps**:
1. Continue to Step 3
2. Enter an API key for OpenAI or another provider
3. Click "Validate"

**Expected Behavior**:
- API key validation request completes
- Shows either "Valid" or "Invalid" (based on actual API key)
- No "Network Error" message

**What to Check**:
- Network tab: `POST /api/providers/openai/validate` (or other provider) returns response
- Console shows validation attempt
- UI shows validation result

### Test Case 4: Step 4 - Workspace Setup

**Steps**:
1. Continue to Step 4
2. Configure workspace location
3. Proceed to Step 5

**Expected Behavior**:
- Workspace directory validation works
- No errors when setting workspace location
- Can proceed to completion

### Test Case 5: "Go to Main App" Button

**Steps**:
1. Complete wizard through Step 6
2. Click "Go to Main App" button

**Expected Behavior**:
- Console shows: `[App] FirstRunWizard onComplete called`
- Console shows: `[App] Transitioning to main app`
- Main application loads
- Left sidebar menu appears
- Dashboard or main app view is visible

**What to Check**:
- Application transitions smoothly to main app
- Left menu is visible with all sections (Create, Projects, Settings, etc.)
- No wizard reappears
- `localStorage.getItem('hasCompletedFirstRun')` === `'true'`

### Test Case 6: Simulated Circuit Breaker State

**Steps**:
1. Open DevTools Console
2. Run: `localStorage.setItem('aura_circuit_breaker_state', JSON.stringify({global: {state: 'OPEN', failureCount: 10, successCount: 0, nextAttempt: Date.now() + 60000, timestamp: Date.now()}}))`
3. Reload the page
4. Watch for circuit breaker clear message

**Expected Behavior**:
- Console shows circuit breaker cleared
- Application still works normally
- No stale circuit breaker state affects wizard

## Files Modified

1. **Aura.Web/src/App.tsx**
   - Added PersistentCircuitBreaker import
   - Added resetCircuitBreaker import
   - Clear circuit breaker before first-run check
   - Fixed onComplete callback order
   - Added console logging for debugging

2. **Aura.Web/src/services/api/setupApi.ts**
   - Added ExtendedAxiosRequestConfig import
   - All methods now use _skipCircuitBreaker config
   - Updated documentation

3. **Aura.Web/src/services/api/ffmpegClient.ts**
   - Added ExtendedAxiosRequestConfig import
   - All methods now use _skipCircuitBreaker config
   - Updated documentation

## Verification Checklist

- [x] Circuit breaker cleared before API calls
- [x] Setup APIs bypass circuit breaker
- [x] FFmpeg APIs bypass circuit breaker
- [x] Navigation state updates correctly
- [x] TypeScript compilation passes
- [x] Linting passes
- [x] Pre-commit hooks pass
- [ ] Manual testing: Step 2 works without network error
- [ ] Manual testing: Step 3 works without network error
- [ ] Manual testing: Step 4 works correctly
- [ ] Manual testing: "Go to Main App" button navigates correctly
- [ ] Manual testing: Main app menu appears after wizard

## Additional Notes

### Why Circuit Breaker Exists

The circuit breaker pattern is a good practice for preventing cascading failures:
- If API is truly down, don't spam it with requests
- Give the service time to recover
- Provide better user experience during outages

### Why This Issue Happened

The persistence of circuit breaker state across sessions was well-intentioned but caused problems:
- **Intention**: Remember that API was down, don't retry immediately after app restart
- **Problem**: Stale state persists indefinitely, blocking healthy API calls
- **Solution**: Clear state on app start, but keep circuit breaker for runtime protection

### Future Improvements

Consider these enhancements:
1. **Shorter Circuit Breaker TTL**: Currently 2 minutes, could be reduced for stale state
2. **Per-Endpoint Circuit Breakers**: Separate circuit breakers for different API endpoints
3. **Circuit Breaker UI**: Show user when circuit breaker is open and why
4. **Health Check Endpoint**: Ping simple health endpoint before complex operations
5. **Better Error Messages**: Distinguish between "API down" vs "circuit breaker open"

## Support

If issues persist after these fixes:

1. **Check Console Logs**:
   - Look for circuit breaker messages
   - Look for API call errors (status codes)
   - Check for CORS errors

2. **Check Network Tab**:
   - Are API calls being made?
   - What status codes are returned?
   - Are there timing issues?

3. **Clear All State**:
   - localStorage.clear()
   - sessionStorage.clear()
   - Clear cookies
   - Hard reload (Ctrl+Shift+R)

4. **Check Backend**:
   - Is API running on correct port?
   - Are endpoints responding?
   - Check backend logs for errors

5. **Report with Details**:
   - Console logs screenshot
   - Network tab screenshot
   - Steps to reproduce
   - Browser and version
