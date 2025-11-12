# Window Loading Diagnostics Enhancement

This document describes the comprehensive window loading diagnostics and recovery features added to Aura Video Studio's Electron application.

## Overview

The window-manager.js module has been enhanced with detailed diagnostics, error recovery mechanisms, and fallback strategies to provide visibility into the window loading process and automatic recovery from failures.

## Features

### 1. Loading Event Handlers

#### did-start-loading
- Records timestamp when loading begins
- Logs start time in ISO format
- Enables tracking of total load duration

#### did-finish-load
- Confirms successful page load
- Logs completion with duration
- Triggers environment variable injection
- Clears timeout timer on success

#### did-fail-load (Enhanced)
- Logs comprehensive error information:
  - Error code
  - Error description
  - Validated URL
  - Main frame status
  - Timestamp
- Stores error in loading state for diagnostics
- Triggers recovery with fallback error page
- Attempts alternate load paths

### 2. Crash Recovery

#### crashed Event Handler
- Detects renderer process crashes
- Shows user-friendly recovery dialog with options:
  - **Reload**: Attempts to reload the application
  - **Close Application**: Gracefully exits
- Logs crash details including killed status

### 3. Console Message Forwarding

All React console messages are forwarded to Electron main process with:
- Timestamp
- Log level (verbose, info, warning, error)
- Message content
- Source file and line number

Format: `[Renderer:level] [timestamp] message`

### 4. Retry Logic

#### Path Fallback Strategy
Multiple paths are tried in sequence:

**Development Mode:**
1. `../../Aura.Web/dist/index.html`
2. `${cwd}/Aura.Web/dist/index.html`
3. `../Aura.Web/dist/index.html`

**Production Mode:**
1. `${resourcesPath}/frontend/index.html`
2. `${resourcesPath}/app.asar.unpacked/frontend/index.html`
3. `${appPath}/frontend/index.html`

#### Timeout Mechanism
- 30-second timeout for page load
- Shows error dialog if timeout expires
- Provides options to load error page or close app
- Includes diagnostic information in dialog

### 5. Fallback Error Page

When main application fails to load, a comprehensive error page is displayed:

#### Features
- Beautiful, user-friendly interface
- System diagnostics display:
  - Platform
  - App version
  - Backend URL
  - Load timestamp
  - Attempted path
- Error details with code and description
- Action buttons:
  - **Retry Loading**: Attempts to reload main app
  - **Open DevTools**: Opens developer tools for debugging
  - **View Logs**: Opens logs folder
- Logs path display for support

#### Fallback Strategy
1. Load from `assets/error.html` file
2. If file missing, load inline HTML error page
3. Inject error information into page

### 6. Environment Variable Injection

**Improved Timing:**
- Environment variables are now injected AFTER `did-finish-load` event
- Ensures DOM is fully loaded before injection
- Prevents race conditions

**Variables Injected:**
- `AURA_BACKEND_URL`: Backend server URL
- `AURA_IS_ELECTRON`: Boolean flag
- `AURA_IS_DEV`: Development mode flag
- `AURA_VERSION`: Application version

### 7. Loading State Tracking

The loading state object tracks:
```javascript
{
  startTime: null,          // Load start timestamp
  didStartLoading: false,   // Loading started flag
  didFinishLoad: false,     // Loading finished flag
  loadAttempts: 0,          // Number of load attempts
  lastError: null,          // Last error details
  loadTimeout: null         // Timeout timer handle
}
```

## New Methods

### `_attemptLoad(backendPort)`
Initiates load with timeout and retry logic.

### `_tryFallbackPaths(fallbackPaths, backendPort)`
Recursively tries alternate paths on load failure.

### `_getFrontendPaths()`
Returns array of all possible frontend paths to try.

### `_injectEnvironmentVariables(backendPort)`
Injects environment variables after successful load.

### `_loadErrorPage(errorCode, errorDescription, attemptedPath)`
Loads fallback error page with diagnostic information.

### `_loadInlineErrorPage(errorCode, errorDescription, attemptedPath)`
Last resort inline error page when error.html is missing.

### `_collectLoadingLogs()`
Collects diagnostic information for error reporting.

## Testing

A comprehensive test suite validates all new features:

```bash
npm run test:window-loading
```

**Tests Include:**
1. WindowManager initialization
2. Loading state initialization
3. Event handlers registration
4. did-start-loading event handling
5. did-finish-load event handling
6. Console message forwarding
7. Frontend paths generation
8. Loading logs collection
9. Error page existence and content
10. Failed load scenario handling

## Error Codes

Common error codes you may encounter:

- **-1**: All load paths failed
- **-2**: Connection refused
- **-3**: Name not resolved
- **-6**: File not found (ERR_FILE_NOT_FOUND)
- **-7**: Load timeout (30 seconds)

## Debugging

### Enable Verbose Logging
Set `isDev` to `true` in WindowManager to see detailed logs.

### Access Logs
Logs are stored in:
- Windows: `%APPDATA%/aura-video-studio/logs`
- macOS: `~/Library/Application Support/aura-video-studio/logs`
- Linux: `~/.config/aura-video-studio/logs`

### DevTools
- Development mode: DevTools open automatically
- Error page: Click "Open DevTools" button
- Production mode: Press `Ctrl+Shift+I` (Windows/Linux) or `Cmd+Option+I` (macOS)

## Implementation Notes

### Zero-Placeholder Policy
All code follows the project's zero-placeholder policy. No TODO, FIXME, or HACK comments exist in the implementation.

### Error Handling Philosophy
- Fail gracefully with user-friendly messages
- Provide actionable recovery options
- Log comprehensive diagnostics for troubleshooting
- Never leave user with blank window

### Performance Considerations
- Timeout set to reasonable 30 seconds
- Fallback paths tried sequentially to avoid overwhelming system
- Loading state tracked efficiently in memory
- Logs collected lazily only when needed

## Future Enhancements

Potential improvements (tracked in GitHub Issues):
- Configurable timeout duration
- Network connectivity checks before load attempts
- Automatic retry with exponential backoff
- Telemetry for load failure analytics
- User preference for DevTools auto-open

## Support

For issues related to window loading:
1. Check logs in application data directory
2. Look for error codes in console output
3. Try loading error page for detailed diagnostics
4. Report issues with logs to GitHub Issues

## Related Files

- `Aura.Desktop/electron/window-manager.js` - Main implementation
- `Aura.Desktop/assets/error.html` - Fallback error page
- `Aura.Desktop/test/test-window-loading-diagnostics.js` - Test suite
- `Aura.Desktop/package.json` - Test script configuration
