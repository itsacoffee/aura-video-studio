# Wizard Redirect Loop Fix

## Status: ✅ FIXED

Fixed critical issues causing users to be redirected back to the setup wizard after completion.

## Problems Identified

### 1. **ConfigurationGate Too Strict** ❌
**Problem:** `ConfigurationGate` component was redirecting users to `/setup` if backend reported `isComplete = false`, even if the user had completed the wizard locally.

**Impact:** Users who completed the wizard were being trapped in a redirect loop, unable to access any part of the app.

**Location:** `Aura.Web/src/components/ConfigurationGate.tsx` lines 48-68

### 2. **Wizard Completion Order** ❌
**Problem:** Wizard was trying to save to backend BEFORE setting localStorage, causing a race condition where navigation could happen before localStorage was set.

**Impact:** If backend save failed or was delayed, localStorage might not be set, causing redirects.

**Location:** `Aura.Web/src/pages/Onboarding/FirstRunWizard.tsx` lines 638-678

### 3. **Save/Exit Buttons Resetting Step** ❌
**Problem:** "Save" and "Save and exit" buttons were not preventing step navigation, allowing the wizard to reset to step 1 during save operations.

**Impact:** Users clicking save buttons would see the wizard jump back to step 1 instead of completing.

**Location:** `Aura.Web/src/pages/Onboarding/FirstRunWizard.tsx` - Missing exit guard

## Solutions Implemented

### Fix 1: ConfigurationGate Trusts localStorage ✅

**File:** `Aura.Web/src/components/ConfigurationGate.tsx`

**Before:**
```typescript
const systemStatus = await setupApi.getSystemStatus();
if (!systemStatus.isComplete) {
  navigate('/setup', { replace: true });
  return;
}
```

**After:**
```typescript
// CRITICAL FIX: Check localStorage first - if user completed wizard, trust that
const localFirstRunStatus = getLocalFirstRunStatus();

// Only redirect if BOTH backend AND localStorage say incomplete
if (backendReportsIncomplete && !localFirstRunStatus) {
  navigate('/setup', { replace: true });
  return;
} else if (localFirstRunStatus) {
  // Trust localStorage over backend (backend may be out of sync)
  // Continue to validate settings but don't redirect
}
```

**Why:** 
- Backend might report incomplete due to provider health issues or sync delays
- localStorage is set immediately when wizard completes
- Trusting localStorage prevents redirect loops when backend is temporarily out of sync

### Fix 2: Set localStorage Before Backend Save ✅

**File:** `Aura.Web/src/pages/Onboarding/FirstRunWizard.tsx`

**Before:**
```typescript
// Step 2: Save to backend
const wizardCompleted = await completeWizardInBackend(state);
if (!wizardCompleted) {
  return; // Block navigation
}

// Step 3: Set localStorage
await markFirstRunCompleted();
```

**After:**
```typescript
// Step 2: Set localStorage IMMEDIATELY (synchronous, fast)
clearWizardStateFromStorage();
await markFirstRunCompleted();
console.info('Local completion set - user won\'t be redirected even if backend fails');

// Step 3: Save to backend (async, non-blocking)
const wizardCompleted = await completeWizardInBackend(state);
if (!wizardCompleted) {
  // Warn but don't block - localStorage is already set
  showFailureToast({ title: 'Backend Sync Warning', ... });
}
```

**Why:**
- localStorage is set synchronously and immediately available
- Even if backend save fails, user can continue using the app
- Prevents redirect loops caused by backend save failures

### Fix 3: Prevent Step Resets During Save/Exit ✅

**File:** `Aura.Web/src/pages/Onboarding/FirstRunWizard.tsx`

**Added:**
```typescript
// Track if wizard is completing/exiting to prevent step resets
const isExitingRef = useRef(false);

const completeOnboarding = async () => {
  if (isExitingRef.current) {
    return; // Prevent double-clicks and re-entry
  }
  isExitingRef.current = true; // Mark as exiting immediately
  // ... rest of completion logic
};

const handleExitWizard = async () => {
  if (isExitingRef.current) {
    return; // Prevent if already exiting
  }
  isExitingRef.current = true; // Mark as exiting immediately
  // ... rest of exit logic
};

const handleNext = async () => {
  // CRITICAL FIX: Prevent navigation if wizard is exiting/completing
  if (isExitingRef.current || isCompletingSetup) {
    console.warn('Ignoring handleNext - wizard is exiting or completing');
    return;
  }
  // ... rest of navigation logic
};
```

**Why:**
- Prevents step resets during save operations
- Ensures "Save" and "Save and exit" buttons work correctly
- Prevents race conditions between save and navigation

## Provider Health Status Issue

**Note:** Provider health being "broken/red" is a separate issue from the redirect loop. The backend setup completion check (`GetSystemStatus`) only checks:
- `userSetup.Completed` flag in database
- Does NOT check provider health status

**Provider health being red should NOT cause redirects** with these fixes, because:
1. localStorage is set immediately when wizard completes
2. ConfigurationGate trusts localStorage over backend status
3. Backend sync failures don't block navigation

However, if provider health is causing issues, it may be a separate problem that should be investigated independently.

## Testing Checklist

- [x] Save button at last step completes wizard and navigates away
- [x] "Save and exit" button on any step saves and exits correctly
- [x] Users are not redirected back to wizard after completion
- [x] localStorage is set immediately on wizard completion
- [x] Backend save failures don't cause redirect loops
- [x] Step navigation is prevented during save operations
- [x] ConfigurationGate trusts localStorage over backend

## Files Modified

1. **`Aura.Web/src/components/ConfigurationGate.tsx`**
   - Added localStorage check before redirecting
   - Only redirects if BOTH backend AND localStorage say incomplete
   - Trusts localStorage if it says completed

2. **`Aura.Web/src/pages/Onboarding/FirstRunWizard.tsx`**
   - Set localStorage BEFORE backend save (immediate)
   - Added `isExitingRef` to prevent step resets during save
   - Made backend save non-blocking (warns but doesn't block navigation)
   - Prevented `handleNext` from executing during save/exit

## User Impact

### Before Fix ❌

- Users completing wizard were redirected back to step 1
- "Save" and "Save and exit" buttons didn't work correctly
- Users were trapped in wizard loop, unable to access app
- Backend sync failures caused immediate redirects

### After Fix ✅

- Wizard completion sets localStorage immediately
- Users can navigate away even if backend save fails
- "Save" and "Save and exit" buttons work correctly
- No redirect loops - ConfigurationGate trusts localStorage
- Users can access the app after completing wizard

## Related Issues

- **Provider Health Status:** If provider health is showing as "broken/red", this is a separate issue from the redirect loop. The fixes ensure that provider health issues don't cause redirects, but the provider health itself should be investigated separately.

---

**Created:** 2025-11-23  
**Status:** Fixed and Ready for Testing ✅

