# Network Error Fix Summary

## Problem Description

When running the portable .exe build of Aura Video Studio, users experienced:

1. **Blank White Screen** - Application window opened but showed only white background
2. **Persistent "Network Error"** - All API operations failed:
   - FFmpeg download/installation
   - API key validation (OpenAI, Pexels, etc.)
   - Backend health checks
   - Configuration saves

These issues prevented the setup wizard from completing and made the application unusable.

## Root Causes Identified

### 1. Content Security Policy (CSP) Issues

**Problem**: When the frontend is loaded from `file://` protocol (Electron production builds), the CSP `connect-src` directive blocked HTTP requests to the backend.

**Technical Details**:

- Original CSP: `connect-src 'self' http://127.0.0.1:* http://localhost:*`
- In `file://` context, `'self'` means `file://` protocol only
- The browser interpreted this as: "allow file:// OR http://127.0.0.1:*"
- But CSP logic is: "'self' (which is file://) AND http://127.0.0.1:*"
- Result: All HTTP requests to localhost were blocked

**Solution Applied** (in `Aura.Desktop/electron/window-manager.js`):

```javascript
// BEFORE (broken):
"connect-src 'self' http://127.0.0.1:* http://localhost:*";

// AFTER (fixed):
"connect-src http://127.0.0.1:* http://localhost:* ws://127.0.0.1:* ws://localhost:* 'self'";
```

The fix explicitly lists HTTP/WS origins BEFORE `'self'`, ensuring they are allowed regardless of the document origin.

Additionally, added `'unsafe-inline'` and `'unsafe-eval'` to `script-src` to allow the bundled React app to execute properly.

### 2. API Base URL Resolution Fallback

**Problem**: The frontend's URL resolution logic fell back to `window.location.origin` when running in Electron, which is `file://` instead of the backend HTTP URL.

**Technical Details**:

- Resolution priority in `apiBaseUrl.ts`:

  1. Electron bridge (`window.aura.runtime.getCachedDiagnostics()`)
  2. Legacy global (`window.AURA_BACKEND_URL`)
  3. Environment variable (`VITE_API_BASE_URL`)
  4. **Current origin (`window.location.origin`)** ← PROBLEM HERE
  5. Development fallback (`http://127.0.0.1:5005`)

- In Electron production builds, if steps 1-3 failed, step 4 would return `file://`
- All API calls would try to connect to `file:///api/...` instead of `http://localhost:5272/api/...`

**Solution Applied** (in `Aura.Web/src/config/apiBaseUrl.ts`):

```typescript
// BEFORE:
if (typeof window !== "undefined" && window.location?.origin) {
  return {
    value: window.location.origin, // Returns "file://" in Electron!
    source: "origin",
  };
}

// AFTER:
if (!isElectron && typeof window !== "undefined" && window.location?.origin) {
  return {
    value: window.location.origin, // Only use in browser, skip in Electron
    source: "origin",
  };
}
```

This prevents falling back to `file://` origin in Electron and forces the fallback to development URL or properly configured Electron bridge.

### 3. Missing Runtime Bootstrap Data

**Problem**: The preload script fetches runtime bootstrap data (including backend URL) synchronously on load, but there was no validation that this data was complete.

**Solution Applied** (in `Aura.Desktop/electron/preload.js` and `main.js`):

Added comprehensive logging and validation:

```javascript
// In preload.js:
try {
  runtimeBootstrap = ipcRenderer.sendSync("runtime:getBootstrap");

  // CRITICAL: Validate that backend URL is present
  if (!runtimeBootstrap || !runtimeBootstrap.backend || !runtimeBootstrap.backend.baseUrl) {
    console.error("[Preload] ERROR: Runtime bootstrap missing backend URL!");
    console.error("[Preload] This will cause all API calls to fail with 'Network Error'");
  } else {
    console.log("[Preload] ✓ Backend URL confirmed:", runtimeBootstrap.backend.baseUrl);
  }
}

// In main.js:
ipcMain.on("runtime:getBootstrap", (event) => {
  const state = refreshRuntimeBridgeState();
  console.log("[RuntimeBridge] Bootstrap requested, returning:", {
    hasBackend: !!state.backend,
    backendUrl: state.backend?.baseUrl,
    hasError: !!state.error,
  });
  event.returnValue = state;
});
```

## Additional Fixes Applied

### FFmpeg Detection Improvements

**File**: `Aura.Desktop/electron/backend-service.js`

- Fixed FFmpeg path environment variables (now passes full path to `ffmpeg.exe`, not just `bin` directory)
- Added detailed logging showing all candidate paths checked
- Enhanced persistence logging to backend configuration
- Added validation before persisting FFmpeg path

### Frontend Loading Diagnostics

**File**: `Aura.Desktop/electron/window-manager.js`

- Added HTML content verification (checks for script tags)
- Added assets directory enumeration (counts .js files)
- Logs file existence before attempting to load
- Helps diagnose if frontend bundle is corrupt or incomplete

## Testing the Fixes

### Before Rebuilding

1. **Clean previous builds**:

   ```powershell
   cd Aura.Desktop
   Remove-Item -Recurse -Force dist, node_modules\.cache
   ```

2. **Ensure dependencies are current**:
   ```powershell
   npm install
   cd ../Aura.Web
   npm install
   ```

### Build Process

```powershell
cd Aura.Desktop
pwsh -File build-desktop.ps1 -Target win
```

### After Building - Verification

1. **Check console output for errors during build**
2. **Verify bundle includes frontend assets**:

   - Check `Aura.Desktop/dist/win-unpacked/resources/frontend/index.html` exists
   - Check `Aura.Desktop/dist/win-unpacked/resources/frontend/assets/*.js` exist

3. **Run the portable .exe**:

   ```powershell
   cd Aura.Desktop/dist
   .\Aura Video Studio-1.0.0-x64.exe
   ```

4. **Check Electron console (terminal) for logs**:

   ```
   [Preload] ✓ Backend URL confirmed: http://127.0.0.1:5272
   [WindowManager] Using production CSP (Electron)
   [BackendService] ✓ Found FFmpeg at: C:\...\resources\ffmpeg\win-x64\bin
   [Backend] ✓ FFmpeg path persisted successfully to backend config
   ```

5. **Open DevTools (F12) and check browser console**:

   - Should see: `[Main] API Base URL: http://127.0.0.1:5272`
   - Should NOT see: CSP errors, failed resource loads, or "Network Error"

6. **Test Setup Wizard**:
   - **Step 1-2 (FFmpeg)**: Should auto-detect managed FFmpeg
   - **Step 3 (Providers)**: Enter OpenAI key, click "Validate" → should succeed or show proper error
   - **Step 4 (Workspace)**: Should save paths without "Network Error"

## Expected Console Output (Success)

### Electron Main Process (Terminal):

```
[RuntimeBridge] Backend URL: http://127.0.0.1:5272
[RuntimeBridge] Backend Ready: true
[RuntimeBridge] Bootstrap requested, returning: {
  hasBackend: true,
  backendUrl: 'http://127.0.0.1:5272',
  hasError: false
}
[Preload] ✓ Backend URL confirmed: http://127.0.0.1:5272
[WindowManager] Using production CSP (Electron)
[WindowManager] Loading from file: C:\...\resources\frontend\index.html
[WindowManager] File exists: true
[WindowManager] HTML has script tags: true
[WindowManager] HTML has module scripts: true
[WindowManager] Found 15 JS files in assets/
[BackendService] ✓ Found FFmpeg at: C:\...\ffmpeg\win-x64\bin
[Backend] ✓ FFmpeg path persisted successfully to backend config
```

### Browser Console (DevTools):

```
[Main] API Base URL: http://127.0.0.1:5272
[Main] API Base URL Source: electron
[Main] Is Electron: true
[Main] Legacy AURA_BACKEND_URL: http://127.0.0.1:5272
[App] Environment hydrated successfully
[FirstRunWizard] Backend is reachable
[FFmpegCheck] FFmpeg detected at: C:\...\ffmpeg.exe
```

## Common Issues & Solutions

### Issue: "Backend contract not available yet"

**Symptoms**: Preload log shows `Backend contract has not been resolved yet`

**Cause**: Backend service hasn't started yet when preload script runs

**Solution**: This is expected during initial load. The frontend will retry. Check that backend actually starts:

```
[Backend] Process started successfully
[Backend] Backend is healthy at http://127.0.0.1:5272
```

### Issue: Still seeing "Network Error" after fixes

**Diagnosis Steps**:

1. **Check CSP in browser DevTools**:

   - Open DevTools → Console tab
   - Look for errors starting with "Refused to connect"
   - If present, CSP fix wasn't applied

2. **Check API Base URL**:

   - Browser console should show: `[Main] API Base URL: http://127.0.0.1:5272`
   - If it shows `file://` or empty, the runtime bootstrap failed

3. **Check backend is running**:

   - Terminal should show: `✓ Backend started successfully`
   - Try manual curl: `curl http://127.0.0.1:5272/health`

4. **Check network requests**:
   - DevTools → Network tab
   - Try to validate an API key
   - Look for the actual request URL
   - Should be `http://127.0.0.1:5272/api/...`, NOT `file://...`

### Issue: Blank white screen persists

**Diagnosis Steps**:

1. **Check DevTools Console** for JavaScript errors
2. **Check DevTools Network** tab - all assets should load (status 200)
3. **Verify HTML was bundled correctly**:

   ```powershell
   Get-Content "Aura.Desktop\dist\win-unpacked\resources\frontend\index.html"
   ```

   Should contain `<script type="module"` tags

4. **Check CSP errors** in console - should not see "Refused to execute script"

## Files Modified

### Critical Fixes:

1. **Aura.Desktop/electron/window-manager.js** - Fixed CSP `connect-src` for file:// protocol
2. **Aura.Web/src/config/apiBaseUrl.ts** - Skip window.location.origin in Electron
3. **Aura.Desktop/electron/preload.js** - Added bootstrap validation
4. **Aura.Desktop/electron/main.js** - Enhanced runtime bridge logging

### Improvements:

5. **Aura.Desktop/electron/backend-service.js** - FFmpeg detection and logging
6. **PORTABLE_EXE_TESTING_GUIDE.md** - Comprehensive testing documentation
7. **NETWORK_ERROR_FIX_SUMMARY.md** - This file

## Rollback Instructions

If these changes cause issues, revert these files to their previous state:

```powershell
git checkout HEAD -- Aura.Desktop/electron/window-manager.js
git checkout HEAD -- Aura.Web/src/config/apiBaseUrl.ts
git checkout HEAD -- Aura.Desktop/electron/preload.js
git checkout HEAD -- Aura.Desktop/electron/main.js
git checkout HEAD -- Aura.Desktop/electron/backend-service.js
```

Then rebuild:

```powershell
cd Aura.Desktop
pwsh -File build-desktop.ps1 -Target win
```

## Technical References

### Content Security Policy (CSP)

- [MDN: CSP connect-src](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Security-Policy/connect-src)
- [CSP with file:// protocol](https://developer.mozilla.org/en-US/docs/Web/HTTP/CSP#violations)

### Electron Security

- [Electron Security Guidelines](https://www.electronjs.org/docs/latest/tutorial/security)
- [Electron Context Isolation](https://www.electronjs.org/docs/latest/tutorial/context-isolation)

### Related Issues

- CSP 'self' in file:// context blocks HTTP requests
- Electron preload bridge timing for backend URL resolution
- React app requires unsafe-inline/unsafe-eval in production Electron builds

## Success Metrics

After applying these fixes, you should observe:

✅ **Zero CSP violations** in DevTools console  
✅ **API Base URL resolves correctly** to `http://127.0.0.1:5272`  
✅ **FFmpeg auto-detected** on first run  
✅ **API key validation works** for all providers (OpenAI, Pexels, etc.)  
✅ **FFmpeg download succeeds** if user chooses to install managed FFmpeg  
✅ **Configuration saves** without "Network Error"  
✅ **Setup wizard completes** all 5 steps successfully  
✅ **Main application loads** with functional dashboard

## Future Improvements

Consider these enhancements to prevent similar issues:

1. **Add startup health check** in frontend that validates backend connectivity before showing wizard
2. **Show diagnostic overlay** in dev builds with backend URL, CSP policy, and connectivity status
3. **Add retry mechanism** for runtime bootstrap with exponential backoff
4. **Implement CSP reporting** endpoint to capture violations in production
5. **Add E2E test** that runs portable .exe and validates network connectivity

## Support

If you encounter issues after applying these fixes:

1. **Collect logs**:

   - Electron console output (entire terminal)
   - Browser DevTools console (screenshots)
   - Backend logs from `%APPDATA%\aura-video-studio\logs\`

2. **Verify fixes were applied**:

   - Check git diff shows all changes
   - Confirm CSP in window-manager.js has HTTP before 'self'
   - Confirm apiBaseUrl.ts has `!isElectron` check

3. **Test in clean environment**:

   - Delete `%APPDATA%\aura-video-studio\`
   - Delete `%LOCALAPPDATA%\aura-video-studio\`
   - Run portable .exe

4. **Report issue with**:
   - Windows version
   - .NET SDK version (`dotnet --version`)
   - Node.js version (`node --version`)
   - All collected logs
   - Screenshots of errors
