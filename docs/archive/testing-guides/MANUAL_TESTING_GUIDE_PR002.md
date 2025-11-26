# Manual Testing Guide for PR #2: Setup Wizard Completion

## Prerequisites
1. Fresh Aura Video Studio installation OR reset wizard state
2. Backend API running on `http://localhost:5005`
3. Frontend running on `http://localhost:5173`

## Test Case 1: Exit Wizard Button

### Steps:
1. Navigate to `http://localhost:5173` (should show FirstRunWizard)
2. Observe the wizard header - look for "Save and Exit" button
3. Click anywhere on Step 1 (FFmpeg Check) to navigate there
4. Click the "Save and Exit" button

### Expected Results:
- ✅ "Save and Exit" button is visible in the wizard header (top right area)
- ✅ Confirmation dialog appears with text:
  ```
  Are you sure you want to exit the setup wizard?
  
  You can complete setup later from the Settings page.
  ```
- ✅ Clicking "Cancel" closes dialog and stays in wizard
- ✅ Clicking "OK" navigates to main application (dashboard/home)
- ✅ Progress is saved (check localStorage: `aura-setup-aborted` = "true")

### Verification:
```javascript
// Open browser console and check:
localStorage.getItem('aura-setup-aborted') // Should be "true"
localStorage.getItem('aura-setup-aborted-step') // Should be "1" or current step
```

---

## Test Case 2: Validation Warnings Before Completion

### Steps:
1. Start FirstRunWizard (reset if needed)
2. Navigate through all steps but skip optional items:
   - Step 0: Click "Get Started" (Welcome)
   - Step 1: Click "Next" (FFmpeg check - don't install if missing)
   - Step 2: Click "Skip" or "Next" (FFmpeg install)
   - Step 3: Click "Skip All" (Provider setup - don't configure any)
   - Step 4: Set workspace path, click "Next"
   - Step 5: Click "Start Creating Videos" (Completion)

### Expected Results:
- ✅ Confirmation dialog appears with warnings list:
  ```
  Setup has some warnings:
  
  1. FFmpeg not detected - video rendering will not work until you install it
  2. No LLM provider configured - script generation will use basic rule-based fallback
  
  Do you want to complete setup anyway?
  ```
- ✅ Clicking "Cancel" stays on completion step
- ✅ Clicking "OK" completes setup and navigates to main app
- ✅ Setup marked as complete in backend

### Verification:
```bash
# Check backend API:
curl http://localhost:5005/api/setup/status

# Should return:
{
  "completed": true,
  "currentStep": 5,
  ...
}
```

---

## Test Case 3: Completion Without Warnings

### Steps:
1. Start FirstRunWizard (reset if needed)
2. Complete all steps with everything configured:
   - Step 0: Click "Get Started"
   - Step 1: Detect or configure FFmpeg
   - Step 2: Install FFmpeg (if needed)
   - Step 3: Configure at least one LLM provider (OpenAI/Anthropic/Ollama)
   - Step 4: Set workspace path
   - Step 5: Click "Start Creating Videos"

### Expected Results:
- ✅ NO warning dialog appears (everything configured)
- ✅ Button shows "Finishing Setup..." briefly with spinner
- ✅ Success toast appears: "Setup Complete - Welcome to Aura Video Studio!"
- ✅ Navigates directly to main application
- ✅ Setup persisted to backend

### Verification:
```javascript
// Check localStorage:
localStorage.getItem('hasCompletedFirstRun') // Should be "true"
```

```bash
# Check backend:
curl http://localhost:5005/api/setup/status

# Should return completed: true
```

---

## Test Case 4: Backend Status Endpoint Alias

### Steps:
1. Use curl or Postman to test both endpoints:
   ```bash
   # New alias endpoint:
   curl http://localhost:5005/api/setup/status
   
   # Original endpoint:
   curl http://localhost:5005/api/setup/wizard/status
   ```

### Expected Results:
- ✅ Both endpoints return identical responses
- ✅ Response structure matches:
  ```json
  {
    "completed": boolean,
    "currentStep": number,
    "state": object | null,
    "canResume": boolean,
    "lastUpdated": string | null,
    "completedAt": string | null,
    "version": string | null
  }
  ```

---

## Test Case 5: Resume After Exit

### Steps:
1. Start FirstRunWizard
2. Complete Step 1 (FFmpeg check)
3. Click "Save and Exit" → Confirm
4. Refresh page or restart application
5. Navigate to `/setup` route

### Expected Results:
- ✅ Wizard shows resume dialog (if implemented)
- ✅ Can continue from Step 1 (last saved step)
- ✅ Progress is preserved

---

## Test Case 6: Spinner During Completion

### Steps:
1. Complete wizard normally
2. Click "Start Creating Videos" on final step
3. Observe button state during completion

### Expected Results:
- ✅ Button text changes to "Finishing Setup..."
- ✅ Spinner icon appears in button
- ✅ Button is disabled (can't double-click)
- ✅ Spinner stops after 1-2 seconds
- ✅ Navigation occurs after completion

### What NOT to see:
- ❌ Spinner spinning indefinitely (bug from problem statement)
- ❌ Button getting stuck in loading state

---

## Regression Testing

### Test: Existing Functionality Still Works

1. **Complete wizard normally** (without exit) → Should work as before
2. **Navigate steps backward** → Should work
3. **Auto-save progress** → Should work (check localStorage)
4. **Backend persistence** → Should save to database
5. **Validation errors** → Should still show for invalid inputs

---

## Browser Console Checks

### Useful Console Logs:
```javascript
// Check if exit was triggered:
console.log('Setup aborted:', localStorage.getItem('aura-setup-aborted'))

// Check completion status:
console.log('First run complete:', localStorage.getItem('hasCompletedFirstRun'))

// Check wizard state:
console.log('Wizard state:', JSON.parse(localStorage.getItem('onboarding-state')))
```

### Expected Console Output:
```
[FirstRunWizard] Circuit breaker state cleared on mount
[FirstRunWizard] Starting onboarding completion { ffmpegPath: "...", workspaceLocation: "..." }
[FirstRunWizard] Setup API response: { success: true, errors: [] }
[FirstRunWizard] First run marked as completed
```

---

## Edge Cases to Test

1. **Exit during Step 0 (Welcome)** → Should allow exit
2. **Exit during Step 5 (Completion)** → Should allow exit
3. **Click exit twice rapidly** → Should only show one dialog
4. **Click complete twice rapidly** → Should only execute once (isCompletingSetup guard)
5. **Backend offline during completion** → Should show error, allow retry

---

## Accessibility Testing

1. **Keyboard navigation**:
   - Tab to "Save and Exit" button → Should be reachable
   - Enter/Space to trigger → Should work
   - Esc to close dialog → Should close confirmation

2. **Screen reader**:
   - "Save and Exit" button → Should announce correctly
   - Confirmation dialog → Should announce title and message
   - Warning list → Should read all warnings

---

## Performance Testing

1. **Large wizard state** → Exit should save quickly (<1s)
2. **Slow network** → Should show loading state appropriately
3. **Multiple tabs** → Exit in one tab should sync to others (via localStorage events)

---

## Cleanup After Testing

To reset the wizard for re-testing:

```javascript
// In browser console:
localStorage.clear();

// Or specifically:
localStorage.removeItem('hasCompletedFirstRun');
localStorage.removeItem('aura-setup-aborted');
localStorage.removeItem('aura-setup-aborted-step');
localStorage.removeItem('onboarding-state');
```

```bash
# Reset backend database (if needed):
curl -X POST http://localhost:5005/api/setup/wizard/reset \
  -H "Content-Type: application/json" \
  -d '{"userId": "default", "preserveData": false}'
```

---

## Expected Test Results Summary

| Test Case | Status | Notes |
|-----------|--------|-------|
| Exit button visible | ✅ | Top right of wizard |
| Exit confirmation | ✅ | Shows dialog |
| Exit saves progress | ✅ | localStorage + backend |
| Validation warnings | ✅ | Shows numbered list |
| Completion without warnings | ✅ | Direct navigation |
| Backend endpoint alias | ✅ | Both URLs work |
| Resume after exit | ⏭️ | UI TBD |
| Spinner behavior | ✅ | Shows briefly, not infinite |
| Accessibility | ✅ | Keyboard + screen reader |

---

## Bug Reporting

If you find issues during testing, please report:

1. **Steps to reproduce**
2. **Expected behavior**
3. **Actual behavior**
4. **Browser console errors** (if any)
5. **Network requests** (check DevTools Network tab)
6. **Screenshot or video** (if applicable)

---

## Success Criteria

All tests should pass without errors. The wizard should:
- ✅ Allow exiting at any step with confirmation
- ✅ Show validation warnings when needed
- ✅ Complete setup smoothly with proper navigation
- ✅ Persist state to backend correctly
- ✅ Maintain accessibility standards
