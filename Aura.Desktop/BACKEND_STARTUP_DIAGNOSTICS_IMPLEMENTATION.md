# Backend Startup Diagnostics and Error Handling Enhancement

## Overview

This implementation adds comprehensive pre-startup validation, detailed error classification, and automatic retry logic to address the "Backend Server Not Reachable" errors in packaged Electron builds.

## Problem Statement

Users were encountering generic "Backend Server Not Reachable" errors when launching the packaged Electron application, with no visibility into the actual cause of the failure. The error could be due to:

- Missing backend executable
- .NET runtime not installed or incompatible version
- Port conflicts (port already in use)
- Backend process crashes during startup
- Slow startup exceeding timeout
- Firewall or permission issues

## Solution Architecture

### 1. Pre-Startup Validation Phase

Before attempting to spawn the backend process, we now validate all prerequisites:

#### .NET Runtime Validation (`_validateDotnetRuntime()`)
- Checks if `dotnet` command is available
- Verifies version is 8.0 or higher
- Returns structured result: `{available: boolean, version?: string, error?: string}`

#### Backend Executable Validation (`_validateBackendExecutable()`)
- Verifies file exists at expected path
- Checks it's a file (not directory)
- Validates execute permissions (Unix)
- Checks file size (> 1KB to detect corruption)
- Returns structured result: `{valid: boolean, error?: string, suggestion?: string}`

#### Port Availability Check (`_checkPortAvailability()`)
- Tests if the configured port is free before binding
- Attempts to create a test server on the port
- On conflict, identifies which process is using the port (Windows/Unix)
- Returns: `{available: boolean, conflictInfo?: string}`

**Process Identification** (`_identifyPortUser()`):
- Windows: Uses `netstat -ano` + `tasklist` to find process name and PID
- Unix: Uses `lsof -i` to find process name and PID
- Provides actionable information: "Port 5005 is in use by: chrome.exe (PID: 1234)"

### 2. Retry Logic with Exponential Backoff

The `start()` method now wraps `_startInternal()` with retry logic:

```javascript
async start() {
  for (let attempt = 1; attempt <= 3; attempt++) {
    try {
      await this._startInternal();
      return; // Success
    } catch (error) {
      // Classify error
      if (isUnrecoverable(error)) {
        throw error; // Don't retry
      }
      // Wait with exponential backoff: 1s, 2s, 4s
      await sleep(1000 * Math.pow(2, attempt - 2));
    }
  }
  throw new Error("Failed after 3 attempts");
}
```

**Unrecoverable Errors** (no retry):
- Backend executable not found
- .NET runtime missing/incompatible
- Missing required dependencies

**Recoverable Errors** (retry with backoff):
- Port temporarily in use
- Transient network errors
- Process spawn failures

### 3. Enhanced Error Classification

Errors are now classified into specific categories for better user guidance:

#### Error Categories

1. **MISSING_EXECUTABLE**
   - Backend executable file not found
   - Recovery: Reinstall Aura Video Studio

2. **PORT_CONFLICT**
   - Port already in use by another process
   - Recovery: Close the conflicting application
   - Shows: Process name and PID using the port

3. **DOTNET_MISSING**
   - .NET 8.0 runtime not installed or incompatible version
   - Recovery: Install .NET 8.0 from official site

4. **STARTUP_TIMEOUT**
   - Backend process started but didn't respond within timeout (60s)
   - Recovery: Check system resources, try again

5. **BINDING_FAILED**
   - Backend process started but couldn't bind to port
   - Recovery: Check Windows Firewall settings

6. **PROCESS_CRASHED**
   - Backend process exited unexpectedly during startup
   - Recovery: Check error logs for crash details

### 4. Detailed Health Check Diagnostics

Enhanced `_waitForBackend()` method now provides:

- Classification of failure type (TIMEOUT, PROCESS_EXITED, BINDING_FAILED, HEALTH_CHECK_TIMEOUT)
- User-friendly guidance specific to the failure
- Last 500 characters of startup output
- Last 500 characters of error output
- Process state (running/exited)
- Process PID
- Health check URL attempted
- Troubleshooting steps

### 5. Error Propagation and User Feedback

#### In `safe-initialization.js`

The `initializeBackendService()` function now:
- Catches all backend startup errors
- Classifies them into categories
- Maps categories to specific recovery actions
- Extracts diagnostics from error object
- Returns structured error information

Example return value on failure:
```javascript
{
  success: false,
  errorCategory: "PORT_CONFLICT",
  recoveryAction: "Close any other applications using port 5005 and try again",
  technicalDetails: "Port 5005 is in use by: node.exe (PID: 1234)\n\nDiagnostics:\n...",
  criticalFailure: true
}
```

#### In `main.js`

The error dialog now shows:
- **Title**: Category-specific (e.g., "Backend Error: PORT CONFLICT")
- **What went wrong**: Error category in human-readable format
- **Recovery Actions**: Specific steps to resolve the issue
- **Technical Details**: Full error message and diagnostics
- **Log Location**: Path to log files for advanced troubleshooting
- **Help Resources**: Links to documentation and GitHub issues

## Implementation Details

### Files Modified

1. **Aura.Desktop/electron/backend-service.js** (+419 lines, -43 lines)
   - Split `start()` into retry wrapper and `_startInternal()` implementation
   - Added 5 new validation methods
   - Enhanced error logging throughout
   - Improved health check diagnostics

2. **Aura.Desktop/electron/safe-initialization.js** (+68 lines, -24 lines)
   - Enhanced `initializeBackendService()` with error classification
   - Maps errors to user-friendly categories and recovery actions
   - Extracts and formats diagnostic information

3. **Aura.Desktop/electron/main.js** (+18 lines, -9 lines)
   - Updated error dialog to show structured error information
   - Displays error category in dialog title
   - Shows clear sections for "What went wrong", "Recovery Actions", "Technical Details"

### New Test Files

1. **test/test-backend-validation.js**
   - Tests all new validation methods
   - Verifies return value structures
   - Tests .NET runtime detection
   - Tests executable validation
   - Tests port availability checks

2. **test/test-backend-failure-scenarios.js**
   - Simulates various failure conditions
   - Tests error classification
   - Tests port conflict detection
   - Tests retry logic structure
   - Validates error message content

## Testing

### Unit Tests

Run validation tests:
```bash
cd Aura.Desktop
node test/test-backend-validation.js
node test/test-backend-failure-scenarios.js
```

Expected output: All tests pass ✅

### Manual Testing Scenarios

1. **Missing .NET Runtime**
   - Uninstall .NET 8.0
   - Launch app
   - Expected: Clear error message with download link

2. **Port Conflict**
   - Start another app on port 5005 (e.g., `python -m http.server 5005`)
   - Launch Aura
   - Expected: Error shows which process is using the port

3. **Missing Backend Executable**
   - Delete backend executable from installation
   - Launch app
   - Expected: Error suggests reinstalling

4. **Slow Startup (Timeout)**
   - Add delay in backend startup
   - Expected: Timeout error with troubleshooting steps

## Benefits

### For Users

1. **Clear Error Messages**: No more generic "Backend Server Not Reachable"
2. **Actionable Guidance**: Specific steps to resolve each type of error
3. **Automatic Recovery**: Transient failures resolved by retry logic
4. **Better Diagnostics**: Detailed information for troubleshooting

### For Developers

1. **Easier Support**: Error categories make it easy to identify issues
2. **Better Logs**: Comprehensive logging at each validation phase
3. **Reduced Tickets**: Users can self-resolve many common issues
4. **Faster Debugging**: Diagnostic information pinpoints exact failures

## Validation Checklist

- [x] Pre-startup validation for .NET runtime
- [x] Pre-startup validation for backend executable
- [x] Port availability check before binding
- [x] Process identification for port conflicts
- [x] Retry logic with exponential backoff (3 attempts)
- [x] Error classification into specific categories
- [x] User-friendly error messages with recovery actions
- [x] Detailed diagnostic information in errors
- [x] Enhanced health check timeout diagnostics
- [x] Unit tests for validation functions
- [x] Unit tests for failure scenarios
- [x] Syntax validation (all files pass `node -c`)

## Future Enhancements

Potential improvements for future PRs:

1. **Firewall Detection**: Automatically detect Windows Firewall blocking and offer to add exception
2. **External Backend Fallback**: If local backend fails repeatedly, offer to connect to external backend
3. **Startup Performance Metrics**: Track startup time and identify bottlenecks
4. **Auto-repair**: Attempt to fix common issues automatically (kill orphaned processes, etc.)
5. **Diagnostic Report Generator**: One-click generation of diagnostic report for support tickets

## Rollout Plan

1. **Phase 1**: Merge this PR and include in next build
2. **Phase 2**: Monitor error reports for new error categories
3. **Phase 3**: Refine error messages based on user feedback
4. **Phase 4**: Add telemetry for startup success/failure rates (if user opts in)

## Success Metrics

After deployment, we expect to see:

- 90%+ reduction in generic "Backend Server Not Reachable" reports
- 50%+ reduction in support tickets related to startup failures
- Higher first-launch success rate (target: 99%)
- Faster issue resolution when failures do occur

## Related Issues

- Original issue: "Backend Server Not Reachable" in packaged builds
- Related PRs: #494, #474, #458 (previous attempts at backend startup improvements)

## Documentation Updates

The following documentation should be updated:

1. **TROUBLESHOOTING.md**: Add section on backend startup errors with error categories
2. **INSTALLATION.md**: Mention .NET 8.0 requirement prominently
3. **BUILD_GUIDE.md**: Update with new validation requirements
4. **README.md**: Add "Common Issues" section referencing troubleshooting guide
