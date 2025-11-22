# Windows Testing Guide - Electron App Initialization Fix

## Prerequisites

### Required Software
- Windows 11 (or Windows 10 with latest updates)
- Node.js 20.x or higher
- npm 9.x or higher
- .NET 8 SDK
- PowerShell 7.x (recommended)
- Git with long paths enabled

### Verify Prerequisites
```powershell
# Check Node.js version
node --version
# Should show: v20.x.x or higher

# Check npm version
npm --version
# Should show: 9.x.x or higher

# Check .NET version
dotnet --version
# Should show: 8.0.x

# Check PowerShell version
$PSVersionTable.PSVersion
# Should show: 7.x or higher
```

## Quick Testing Path

### Option 1: Automated Testing Script
```powershell
# Run verification script to check configuration
node verify-electron-fixes.js

# If all tests pass, proceed to build
```

### Option 2: Manual Build and Test

#### Step 1: Clean Build Frontend
```powershell
cd Aura.Web
Remove-Item -Recurse -Force dist -ErrorAction SilentlyContinue
npm ci
npm run build
```

**Expected Output:**
- Build should complete successfully
- Main bundle: `index-[hash].js` or similar (3.5-3.7 MB)
- Message: "Build verification passed"

#### Step 2: Build Backend
```powershell
cd ..\Aura.Api
dotnet restore
dotnet build -c Release
```

**Expected Output:**
- Build succeeded: 0 Warning(s), 0 Error(s)
- Frontend copied to wwwroot

#### Step 3: Build Electron App
```powershell
cd ..\Aura.Desktop
pwsh -File build-desktop.ps1 -Target win
```

**Expected Output:**
- Frontend build complete ✓
- Backend build complete ✓
- Electron dependencies ready ✓
- Windows installer built ✓

**Build Artifacts Location:**
```
Aura.Desktop\dist\
├── Aura Video Studio-1.0.0-x64.exe      (Portable executable)
├── Aura Video Studio Setup 1.0.0.exe    (Installer)
└── win-unpacked\                        (Unpacked directory)
```

## Testing the Fixes

### Test 1: Launch the Application

#### Launch Portable Version
```powershell
cd Aura.Desktop\dist
.\Aura Video Studio-1.0.0-x64.exe
```

#### OR Launch from Unpacked Directory
```powershell
cd Aura.Desktop\dist\win-unpacked
.\Aura Video Studio.exe
```

### Test 2: Verify No Blank Screen

**CRITICAL TEST**: The application window should show content immediately.

✓ **PASS Criteria:**
- Application window opens
- Welcome wizard or main interface loads
- No blank white screen
- Content is visible within 2-3 seconds

✗ **FAIL Criteria:**
- Window shows only white screen
- Window remains blank after 10 seconds
- Application crashes on startup

### Test 3: Check DevTools Console

#### Open DevTools
- Press `Ctrl+Shift+I` to open Developer Tools
- Click "Console" tab

#### Check for Errors

**Look for these specific errors (should NOT be present):**
- ❌ "Cannot access [variable] before initialization"
- ❌ "Cannot set properties of undefined (setting 'Children')"
- ❌ "React" or "react-dom" initialization errors
- ❌ Circular dependency warnings

**Acceptable warnings/logs:**
- ℹ️ General info logs about app initialization
- ℹ️ Backend connection attempts
- ℹ️ Provider availability checks

✓ **PASS Criteria:**
- No errors about variable access before initialization
- No React module errors
- No circular dependency errors

✗ **FAIL Criteria:**
- Any "Cannot access before initialization" errors
- Any React initialization errors
- Application remains non-interactive

### Test 4: Verify Bundle Characteristics

#### Check Loaded Scripts
In DevTools Console, run:
```javascript
// Check loaded scripts
Array.from(document.querySelectorAll('script')).map(s => ({
  src: s.src,
  type: s.type
}))
```

**Expected Result:**
```javascript
[
  {
    src: "file:///C:/path/to/Aura.Desktop/resources/app/dist/assets/index-[hash].js",
    type: "module"
  }
]
```

✓ **PASS**: Single main script loaded
✗ **FAIL**: Multiple vendor chunk scripts loaded

### Test 5: Check Bundle Size

#### View Network Tab
1. Open DevTools (Ctrl+Shift+I)
2. Go to "Network" tab
3. Reload app (Ctrl+R)
4. Look for main JS file

**Expected:**
- File name: `index-[hash].js`
- Size: 3.5-3.7 MB (unminified)
- Status: 200 OK
- Type: javascript

✓ **PASS**: Size is 3.5-3.7 MB (indicates unminified code)
✗ **FAIL**: Size is 2.0-2.5 MB (indicates minified code - fix not applied)

### Test 6: Functional Testing

#### Test Basic Functionality
1. Navigate through the application menus
2. Open the Settings page
3. Create a new project (if applicable)
4. Check if all UI elements are interactive

✓ **PASS Criteria:**
- All buttons and controls work
- Navigation is smooth
- No JavaScript errors during interaction
- Application remains stable

### Test 7: Performance Check

#### Measure Load Time
1. Close the application
2. Launch again
3. Time how long until UI is fully interactive

**Expected Load Time:**
- Initial launch: 2-5 seconds
- Subsequent launches: 1-3 seconds
- UI interactive: Within 3 seconds of window opening

✓ **PASS**: Load time < 5 seconds
⚠ **ACCEPTABLE**: Load time 5-10 seconds (slower hardware)
✗ **FAIL**: Load time > 10 seconds or never loads

## Troubleshooting

### Issue: Blank White Screen Still Appears

**Possible Causes:**
1. Old build artifacts cached
2. Electron cache not cleared
3. Build not completed successfully

**Solution:**
```powershell
# Clean everything
cd Aura.Web
Remove-Item -Recurse -Force dist, node_modules

cd ..\Aura.Desktop  
Remove-Item -Recurse -Force dist, node_modules
Remove-Item -Recurse -Force resources\backend

# Rebuild from scratch
cd ..\Aura.Web
npm ci
npm run build

cd ..\Aura.Desktop
pwsh -File build-desktop.ps1 -Target win
```

### Issue: "Cannot access before initialization" Errors

**Check:**
1. Verify `vite.config.ts` has `minify: false`
2. Verify `vite.config.ts` has `manualChunks: undefined`
3. Run verification script: `node verify-electron-fixes.js`

**If still present:**
- The fix was not properly applied
- Check git diff to ensure changes are committed
- Re-run the build process

### Issue: Application Crashes on Startup

**Check:**
1. Backend is built correctly (`Aura.Api/bin/Release/net8.0/win-x64/`)
2. Frontend is built correctly (`Aura.Web/dist/index.html` exists)
3. Electron dependencies installed (`Aura.Desktop/node_modules/electron/`)

**View Crash Logs:**
```powershell
# Check Electron logs
$env:ELECTRON_ENABLE_LOGGING = "1"
.\Aura Video Studio.exe
```

### Issue: Build Fails

**Common Issues:**
1. Node.js version too old (need 20.x+)
2. .NET SDK not installed or wrong version
3. npm dependencies not installed

**Solution:**
```powershell
# Update Node.js to 20.x
# Install from: https://nodejs.org/

# Update .NET SDK to 8.x
# Install from: https://dotnet.microsoft.com/download

# Clear npm cache
npm cache clean --force

# Try build again
```

## Verification Checklist

Use this checklist to verify all fixes are working:

### Build Verification
- [ ] Frontend builds without errors
- [ ] Main bundle is 3.5-3.7 MB (unminified)
- [ ] Backend builds without errors
- [ ] Electron app packages successfully

### Configuration Verification (run `node verify-electron-fixes.js`)
- [ ] All 21 tests pass
- [ ] Success rate: 100%

### Runtime Verification
- [ ] Application launches without errors
- [ ] No blank white screen
- [ ] Welcome wizard or main UI loads
- [ ] DevTools console shows no "Cannot access" errors
- [ ] DevTools console shows no React errors
- [ ] Application is fully interactive

### Performance Verification
- [ ] Application loads in < 5 seconds
- [ ] UI is responsive
- [ ] No lag or freezing
- [ ] Memory usage reasonable (< 500 MB idle)

## Expected Test Results Summary

| Test | Expected Result | How to Verify |
|------|----------------|---------------|
| Application Launches | ✓ Window opens | Launch .exe file |
| No Blank Screen | ✓ UI loads immediately | Visual check |
| Console Errors | ✓ None present | DevTools Console |
| Bundle Size | ✓ 3.5-3.7 MB | DevTools Network tab |
| Load Time | ✓ < 5 seconds | Measure with stopwatch |
| Functionality | ✓ All features work | Manual testing |

## Reporting Results

### If All Tests Pass
Document the following:
- Windows version tested
- Node.js version used
- .NET version used
- Load time measured
- Screenshot of working application
- Screenshot of clean DevTools console

### If Any Tests Fail
Document the following:
- Which test failed
- Error messages observed
- Screenshots of errors
- DevTools console output
- Steps to reproduce the issue

## Success Criteria

The fix is considered successful when:

1. ✓ Application launches without blank screen
2. ✓ No "Cannot access before initialization" errors
3. ✓ No React initialization errors
4. ✓ All functionality works correctly
5. ✓ Load time is acceptable (< 5 seconds)
6. ✓ Verification script passes 100% of tests

If all criteria are met, the Electron app initialization fix is **VERIFIED** and working correctly.

---

**Version**: 1.0.0  
**Last Updated**: 2025-11-22  
**Status**: Ready for Testing
