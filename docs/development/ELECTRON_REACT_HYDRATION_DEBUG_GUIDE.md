# Electron React Hydration Debugging Guide

## Overview

This guide documents the debugging improvements made to diagnose React hydration issues in the Electron BrowserWindow. These changes provide comprehensive logging and error handling to identify why the React application might not be rendering.

## Changes Summary

### 1. Enhanced Main.tsx Logging (`Aura.Web/src/main.tsx`)

**Added detailed initialization logging before React loads:**

```typescript
// Logs include:
- Timestamp and location (URL and protocol)
- User Agent information
- Electron environment detection (window.electron)
- Environment variables (AURA_BACKEND_URL, AURA_IS_ELECTRON, etc.)
- Root element status
- Import.meta.env values (MODE, DEV, PROD)
- Step-by-step React initialization progress
```

**Key console log markers to look for:**
- `[Main] ===== Aura Video Studio - React Initialization =====`
- `[Main] Creating React root...`
- `[Main] Calling ReactDOM.createRoot...`
- `[Main] Rendering App component...`
- `[Main] ✓ React render call completed`

### 2. Enhanced Preload.js Error Handling (`Aura.Desktop/electron/preload.js`)

**Added preparation for global error handler:**
- Prepared error handler script for injection
- Will catch uncaught errors and unhandled promise rejections
- Logs all JavaScript errors with full stack traces

### 3. Enhanced Window-Manager.js (`Aura.Desktop/electron/window-manager.js`)

**Added comprehensive error detection and logging:**

#### a. Load Failure Detection
```javascript
mainWindow.webContents.on('did-fail-load', (event, errorCode, errorDescription, validatedURL, isMainFrame) => {
  // Logs all load failures with error codes
});
```

#### b. Console Message Forwarding
```javascript
mainWindow.webContents.on('console-message', (event, level, message, line, sourceId) => {
  // Forwards all renderer console messages to main process
});
```

#### c. URL Verification
```javascript
const loadedURL = this.mainWindow.webContents.getURL();
console.log('[WindowManager] Loaded URL:', loadedURL);
console.log('[WindowManager] URL protocol:', new URL(loadedURL).protocol);
```

#### d. Global Error Handler Injection
Injects error handlers BEFORE environment variables to catch errors early:
```javascript
window.addEventListener('error', function(event) {
  // Logs uncaught errors
});

window.addEventListener('unhandledrejection', function(event) {
  // Logs unhandled promise rejections
});
```

### 4. CSP Updates for file:// Protocol

**Updated Content Security Policy in both locations:**

#### index.html
```html
<meta http-equiv="Content-Security-Policy"
  content="default-src 'self' file: data: blob:; 
           script-src 'self' 'unsafe-inline' 'unsafe-eval' file:; 
           ..." />
```

#### window-manager.js
Added `file:` protocol to all relevant CSP directives for both dev and production modes.

## Testing Instructions

### Development Mode Testing

1. **Build the Web App:**
   ```bash
   cd Aura.Web
   npm run build
   ```

2. **Start Electron in Dev Mode:**
   ```bash
   cd Aura.Desktop
   npm run dev
   ```

3. **Expected Console Output:**

   **Main Process Console (Terminal):**
   ```
   [WindowManager] Loading frontend from: /path/to/Aura.Web/dist/index.html
   [WindowManager] Frontend path exists: true
   [WindowManager] ✓ Frontend file loaded successfully
   [WindowManager] Loaded URL: file:///path/to/Aura.Web/dist/index.html
   [WindowManager] URL protocol: file:
   [WindowManager] Injecting global error handler...
   [WindowManager] ✓ Error handler injected
   [WindowManager] Injecting environment variables...
   [WindowManager] ✓ Environment variables injected
   [WindowManager] Window ready to show
   [WindowManager] Development mode detected - opening DevTools
   ```

   **Renderer Process Console (DevTools):**
   ```
   [Main] ===== Aura Video Studio - React Initialization =====
   [Main] Timestamp: 2025-01-15T10:30:00.000Z
   [Main] Location: file:///path/to/Aura.Web/dist/index.html
   [Main] Protocol: file:
   [Main] window.electron exists: true
   [Main] AURA_IS_ELECTRON: true
   [Main] AURA_BACKEND_URL: http://localhost:5005
   [Main] Root element exists: true
   [Main] Creating React root...
   [Main] ✓ React render call completed
   ```

4. **DevTools Should Auto-Open:**
   - DevTools will open automatically in development mode
   - Check the Console tab for initialization logs
   - Check the Network tab to verify all assets loaded (status 200)
   - Check for any red error messages

### Production Mode Testing

1. **Build Electron App:**
   ```bash
   cd Aura.Desktop
   npm run build:dir
   ```

2. **Run the Built App:**
   - Navigate to `Aura.Desktop/dist/win-unpacked/`
   - Run `Aura Video Studio.exe`

3. **Check Logs:**
   - Main process logs won't be visible unless running from command line
   - Renderer logs visible in DevTools (F12 or Ctrl+Shift+I)

## Troubleshooting Guide

### Issue: React Not Rendering

**Check these logs in order:**

1. **Did the HTML file load?**
   ```
   Look for: [WindowManager] ✓ Frontend file loaded successfully
   If missing: Check if dist/index.html exists
   ```

2. **What URL was loaded?**
   ```
   Look for: [WindowManager] Loaded URL: file://...
   Should be: file:// protocol with correct path
   ```

3. **Were environment variables injected?**
   ```
   Look for: [WindowManager] ✓ Environment variables injected
   In DevTools: [Main] AURA_BACKEND_URL: http://localhost:5005
   ```

4. **Did React initialization start?**
   ```
   Look for: [Main] Creating React root...
   If missing: JavaScript bundle didn't load or CSP blocked it
   ```

5. **Did React render complete?**
   ```
   Look for: [Main] ✓ React render call completed
   If missing: Error occurred during React mounting
   ```

### Issue: Load Failures

**Check for did-fail-load events:**
```
Look for: [WindowManager] ✗ Failed to load page!
Error code: <number>
Error description: <message>
```

**Common error codes:**
- `-3` (ERR_ABORTED): Request was aborted
- `-6` (ERR_FILE_NOT_FOUND): File doesn't exist
- `-10` (ERR_ACCESS_DENIED): Permission denied
- `-105` (ERR_NAME_NOT_RESOLVED): DNS resolution failed
- `-324` (ERR_EMPTY_RESPONSE): Server returned empty response

### Issue: CSP Blocking Scripts

**Check for CSP violations:**
```
Look in DevTools Console for:
"Refused to load the script because it violates the following Content Security Policy directive..."
```

**If CSP is blocking:**
1. Verify `file:` protocol is in CSP directives
2. Check both index.html and window-manager.js CSP configurations
3. In dev mode, CSP should be more permissive

### Issue: Assets Not Loading

**Check Network tab in DevTools:**
1. All JavaScript files should have status 200
2. Check if paths are correct (should be relative to index.html)
3. Verify dist/assets/ directory contains all files

**Common path issues:**
- Absolute paths starting with `/` should work with loadFile()
- If assets show 404, check if dist/ was built correctly
- Verify Aura.Web/dist/assets/ directory exists

## Diagnostic Checklist

Use this checklist when debugging:

- [ ] Aura.Web built successfully (`npm run build` in Aura.Web/)
- [ ] dist/index.html exists and contains correct script references
- [ ] Electron starts without crashes
- [ ] Window appears (even if blank)
- [ ] DevTools auto-opens in dev mode
- [ ] Main process shows "Frontend file loaded successfully"
- [ ] Main process shows "Environment variables injected"
- [ ] DevTools console shows React initialization logs
- [ ] DevTools Network tab shows assets loaded (200 status)
- [ ] No CSP violations in console
- [ ] No JavaScript errors in console
- [ ] Root element receives React content

## Expected Behavior

**Successful initialization should show:**

1. Window opens with splash screen
2. Main process logs show successful file load
3. DevTools opens automatically (dev mode)
4. Console shows full initialization sequence
5. React app renders within 1-2 seconds
6. Splash screen closes when app is ready
7. No errors in console

**If initialization fails:**

1. Initialization guard appears after 10 seconds
2. Error messages visible in console
3. Failed resources logged in Network tab
4. Specific error handlers catch and log the issue

## Next Steps

After implementing these changes:

1. Test in development mode first
2. Check all console logs are present
3. Verify DevTools opens automatically
4. Test in production build
5. Document any new issues found
6. Use the diagnostic checklist above

## Related Files

- `Aura.Web/src/main.tsx` - React initialization with logging
- `Aura.Desktop/electron/preload.js` - Preload script with error handling prep
- `Aura.Desktop/electron/window-manager.js` - Window creation with comprehensive logging
- `Aura.Web/index.html` - HTML template with CSP and initialization guard
- `Aura.Web/vite.config.ts` - Vite build configuration

## Known Limitations

1. Main process logs only visible in terminal (not in packaged apps)
2. DevTools must be manually opened in production builds
3. CSP must allow 'unsafe-inline' and 'unsafe-eval' for React dev builds
4. File:// protocol requires specific CSP directives

## Additional Resources

- Electron loadFile() documentation: https://www.electronjs.org/docs/latest/api/browser-window#winloadfilefilepath-options
- Content Security Policy: https://developer.mozilla.org/en-US/docs/Web/HTTP/CSP
- React Hydration: https://react.dev/reference/react-dom/client/hydrateRoot
