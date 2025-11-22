# Window Close Button Not Working Issue

## Problem Description

**Issue**: The "X" button in the top right corner of the application window doesn't close the program. Users must use Alt+F4 or navigate through the menu to exit the application.

**Impact**: Poor user experience - users expect the X button to close the application

**Platform**: Desktop application (Electron-based), primarily affects Windows users

## Root Cause Analysis ✅

**File**: `Aura.Desktop/electron/window-manager.js`, lines 409-416

```javascript
handleWindowClose(event, isQuitting, minimizeToTray = true) {
  if (!isQuitting && minimizeToTray && process.platform === "win32") {
    event.preventDefault();
    this.mainWindow.hide();
    return true; // Prevented default
  }
  return false; // Allow close
}
```

**Issue**: The function defaults `minimizeToTray` to `true`, but the logic prevents window closing when:
1. App is NOT quitting (`!isQuitting`)
2. AND `minimizeToTray` is enabled
3. AND platform is Windows

**Configuration**: In `main.js` line 1194:
```javascript
const minimizeToTray = appConfig.get("minimizeToTray", false); // Default to false
```

The config defaults to `false`, BUT if the user has ever enabled "Minimize to Tray" in settings (or if there's a legacy config), the X button will hide the window instead of closing it.

## Expected Behavior

When users click the X button (window close button):
1. If "Minimize to Tray" is disabled (default): Close the application
2. If "Minimize to Tray" is enabled: Hide to tray with notification
3. Allow users to toggle this behavior in settings

## Current Behavior

Clicking the X button:
- Hides the window to system tray (if minimizeToTray is somehow enabled)
- Shows notification: "Application is minimized to the system tray..."
- Window appears closed but process keeps running in background

## Workarounds

Currently, users must:
- Use Alt+F4 keyboard shortcut
- Use File → Exit menu option
- Right-click tray icon and select Quit
- Use Task Manager to force close

## Potential Causes of the Issue

1. **User enabled minimize to tray**: Setting was enabled in app settings
2. **Legacy config**: Old configuration file has minimizeToTray set to true
3. **Config corruption**: Config file corrupted or has incorrect default
4. **First-run default**: Fresh install might have wrong default value

## Fix Options

### Option 1: Add User Confirmation Dialog (Recommended)
When X button is clicked the first time, ask user:
- "Do you want to minimize to tray or exit?"
- [ ] Always close the application
- [ ] Minimize to system tray
- [x] Remember my choice

### Option 2: Change Default Behavior
Ensure `minimizeToTray` defaults to `false` everywhere and is only enabled when user explicitly opts in.

### Option 3: Add Settings UI
Ensure there's a clear Settings → General → "Minimize to tray on close" checkbox that users can toggle.

### Option 4: Platform-Specific Defaults
- Windows: Default to false (close app)
- macOS: Default to true (hide to tray - common pattern)
- Linux: Default to false (close app)

## Files to Modify

1. **Aura.Desktop/electron/window-manager.js** (line 409)
   - Change default parameter from `true` to `false`
   - OR remove default parameter (rely on caller)

2. **Aura.Desktop/electron/main.js** (line 1194)
   - Ensure config retrieval has correct default
   - Consider first-run detection

3. **Settings UI** (if not present)
   - Add "Minimize to tray on close" setting
   - Add "Close behavior" section

4. **Config file validation**
   - Ensure config schema validation
   - Migration for old configs

## Implementation Steps

1. Change `minimizeToTray = true` to `minimizeToTray = false` in window-manager.js line 409
2. Add config migration to reset minimizeToTray for existing users if corrupted
3. Add settings UI to allow users to control this behavior
4. Add first-run dialog to ask user preference
5. Test on Windows, macOS, Linux

## Testing Requirements

- [ ] Test with minimizeToTray = false (X should close app)
- [ ] Test with minimizeToTray = true (X should hide to tray)
- [ ] Test Alt+F4 always closes app
- [ ] Test File → Exit always closes app
- [ ] Test tray icon right-click → Quit closes app
- [ ] Test on Windows (primary affected platform)
- [ ] Test on macOS and Linux
- [ ] Test with corrupted config file
- [ ] Test first-run experience

## Related Files

- `Aura.Desktop/electron/main.js` - Main process, window close handler
- `Aura.Desktop/electron/window-manager.js` - Window lifecycle management
- `Aura.Desktop/electron/tray-manager.js` - System tray management
- `Aura.Desktop/electron/shutdown-orchestrator.js` - Cleanup on quit
- Application settings/config files

## Priority

**High** - Significantly affects user experience, users cannot close the app normally

## Labels

- bug
- desktop
- electron
- user-experience
- high-priority
- windows

## Quick Fix (Immediate Solution)

The simplest immediate fix is to change line 409 in `window-manager.js`:

**Before**:
```javascript
handleWindowClose(event, isQuitting, minimizeToTray = true) {
```

**After**:
```javascript
handleWindowClose(event, isQuitting, minimizeToTray = false) {
```

This ensures the X button closes the app by default unless explicitly enabled in settings.

---

**Status**: Root cause identified, ready for implementation
**Estimated Effort**: 1-2 hours for quick fix, 4-8 hours for complete solution with UI
