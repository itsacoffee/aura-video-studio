# Black Screen Fix - Test Plan

## Overview
This test plan verifies that the black screen issue has been resolved and that all window state transitions work correctly.

## Pre-Test Setup
1. Build the application with the fixes
2. Have a secondary application ready for Alt-Tab testing
3. If possible, test on multiple monitor setup
4. Test with both light and dark themes

## Test Cases

### 1. Window Focus/Blur (Basic)
**Steps:**
1. Launch the application
2. Click on another application window
3. Click back on Aura Video Studio

**Expected Result:**
- ✅ No black screen when regaining focus
- ✅ Application content remains visible
- ✅ Theme colors are correct
- ✅ No console errors

### 2. Window Minimize/Restore
**Steps:**
1. Launch the application
2. Click the minimize button
3. Wait 5 seconds
4. Restore the window from taskbar

**Expected Result:**
- ✅ No black screen after restore
- ✅ Application content is immediately visible
- ✅ No flickering or repainting visible to user
- ✅ Console shows "[WindowManager] Window visibility changed: restore"

### 3. Alt-Tab Window Switching
**Steps:**
1. Launch the application
2. Open another application (e.g., browser)
3. Use Alt-Tab to switch between applications
4. Repeat 5 times rapidly

**Expected Result:**
- ✅ No black screen during any transition
- ✅ Smooth transitions between applications
- ✅ Console shows debounced visibility changes (not every keystroke)
- ✅ Application remains responsive

### 4. Theme Switching During Focus Changes
**Steps:**
1. Launch the application
2. Go to Settings and switch theme (dark to light or vice versa)
3. Minimize and restore the window
4. Switch to another app and back
5. Verify theme is still correct

**Expected Result:**
- ✅ Theme persists correctly
- ✅ No black screen during theme changes
- ✅ Background color updates immediately
- ✅ No double-rendering or flashing

### 5. Rapid Window State Changes
**Steps:**
1. Launch the application
2. Rapidly minimize and restore 10 times
3. Rapidly Alt-Tab back and forth 10 times
4. Quickly switch between clicking other windows and Aura

**Expected Result:**
- ✅ No black screens during rapid transitions
- ✅ Console shows debounced events (300ms grouping)
- ✅ No performance degradation
- ✅ No memory leaks (check Task Manager)

### 6. Multi-Monitor Scenarios (if available)
**Steps:**
1. Launch application on Monitor 1
2. Drag window to Monitor 2
3. Minimize and restore
4. Move back to Monitor 1
5. Minimize and restore again

**Expected Result:**
- ✅ No black screen on either monitor
- ✅ Window renders correctly after moving
- ✅ No DPI-related rendering issues

### 7. Application Startup
**Steps:**
1. Close application completely
2. Launch application
3. Wait for splash screen to complete
4. Observe initial render

**Expected Result:**
- ✅ No black screen on startup
- ✅ Smooth transition from splash to main window
- ✅ Content loads correctly
- ✅ Background color set correctly from start

### 8. Long-Duration Focus Loss
**Steps:**
1. Launch the application
2. Minimize or switch to another app
3. Leave it for 5 minutes
4. Restore/switch back to Aura

**Expected Result:**
- ✅ No black screen after long period
- ✅ Application wakes up immediately
- ✅ No stale rendering or frozen content

### 9. Console Log Verification
**Steps:**
1. Launch application with DevTools open (--dev flag)
2. Perform various window state transitions
3. Review console messages

**Expected Logs:**
- ✅ "[WindowManager] Window visibility changed: focus" (on focus)
- ✅ "[WindowManager] Window lost focus" (on blur)
- ✅ "[WindowManager] Window visibility changed: restore" (on restore)
- ✅ "[WindowManager] WebContents invalidated for repaint" (300ms after state change)
- ✅ NO "[App] Black screen detected" messages
- ✅ NO "[WindowManager] Failed to execute repaint script" errors

**Not Expected (Old Behavior):**
- ❌ "[App] Black screen detected and fixed"
- ❌ "[App] Black screen detected on focus regain"
- ❌ Multiple rapid "[WindowManager] Window focused" within seconds
- ❌ JavaScript execution errors

### 10. Performance Check
**Steps:**
1. Launch application
2. Open Task Manager / Activity Monitor
3. Perform window state transitions
4. Monitor CPU and Memory usage

**Expected Result:**
- ✅ CPU usage returns to baseline after state changes
- ✅ No periodic CPU spikes (old polling removed)
- ✅ Memory usage stable (no leaks from timers)
- ✅ GPU usage normal for hardware acceleration

## Regression Testing

### Theme Management Still Works
**Steps:**
1. Switch between light and dark mode
2. Change theme (Aura vs Fluent)
3. Verify localStorage persists theme choice
4. Restart application and verify theme is remembered

**Expected Result:**
- ✅ All theme functionality works as before
- ✅ Background color updates correctly
- ✅ No black screens during theme changes

### Error Handling Still Works
**Steps:**
1. Trigger various error conditions
2. Verify error boundaries catch errors
3. Check that error dialogs display correctly

**Expected Result:**
- ✅ Error handling unchanged
- ✅ No black screens during error states

## Success Criteria

The fix is successful if:
1. ✅ Zero black screen occurrences across all test cases
2. ✅ No console errors related to rendering
3. ✅ Smooth window transitions
4. ✅ Reduced CPU usage (no constant polling)
5. ✅ Console shows proper debouncing (300ms grouping)
6. ✅ All theme functionality preserved
7. ✅ No regressions in other features

## Known Limitations

None expected. The fix should work in all scenarios.

## Rollback Plan

If critical issues are found:
1. Revert commit: `git revert cbb58ab`
2. Restore old black screen detection code
3. File detailed bug report with reproduction steps

## Notes for Testers

- Test on Windows 10/11 if possible (primary platform)
- Test with hardware acceleration both enabled and disabled
- Test on both high-DPI and standard monitors
- Test with different GPU drivers (NVIDIA, AMD, Intel)
- Report any unexpected console messages
