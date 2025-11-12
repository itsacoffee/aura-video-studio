# Electron Protocol Registration Fix

## Problem

The application was failing to start with the following error:

```
Startup Error

Failed to start Aura Video Studio:

protocol.registerSchemesAsPrivileged should be called before app is ready

Please check the logs for more information:
C:\Users\User\AppData\Roaming\aura-video-studio\logs
```

## Root Cause

The `protocol.registerSchemesAsPrivileged()` function was being called in the `ProtocolHandler.register()` method, which was invoked during `startApplication()` - **after** the `app.whenReady()` event had already fired.

According to [Electron's API documentation](https://www.electronjs.org/docs/latest/api/protocol#protocolregisterschemesasprivilegedcustomschemes), `protocol.registerSchemesAsPrivileged()` must be called **before** the `app.ready` event fires.

## Solution

### Changes Made

1. **main.js**
   - Added `protocol` to the require statement
   - Moved the `protocol.registerSchemesAsPrivileged()` call to execute before `app.whenReady()`
   - Added clear comments explaining the timing requirement

2. **protocol-handler.js**
   - Removed `protocol` import (no longer needed)
   - Removed `protocol.registerSchemesAsPrivileged()` from the `register()` method
   - Added static method `getProtocolScheme()` to expose the scheme name before class instantiation
   - Added comment documenting the change

### Code Flow

**Before (Broken)**:
```
1. app.whenReady() fires
2. startApplication() executes
3. protocolHandler.register() called
4. protocol.registerSchemesAsPrivileged() called ❌ TOO LATE
```

**After (Fixed)**:
```
1. Module loads
2. protocol.registerSchemesAsPrivileged() called ✓ BEFORE app.ready
3. app.whenReady() fires
4. startApplication() executes
5. protocolHandler.register() called (event handlers only)
```

## Technical Details

### Protocol Registration Timing

Electron requires privileged protocol schemes to be registered before the app becomes ready because:

1. **Security**: Protocol privileges must be determined before any web content is loaded
2. **Initialization**: Chromium's networking stack needs this configuration during startup
3. **Consistency**: Changing protocol privileges after app initialization could lead to security vulnerabilities

### Static Method Addition

Added `ProtocolHandler.getProtocolScheme()` static method to allow access to the protocol scheme name before instantiating the class:

```javascript
static getProtocolScheme() {
  return 'aura';
}
```

This enables the protocol registration in `main.js` before the `ProtocolHandler` instance is created.

## Verification

- ✅ JavaScript syntax validation passed
- ✅ Code follows Electron API requirements
- ✅ No breaking changes to existing functionality
- ✅ Protocol event handlers remain in ProtocolHandler class

## Testing Recommendations

To test this fix:

1. Build the Electron app: `npm run build` (in Aura.Desktop)
2. Run the portable or installer version
3. Verify the app starts without errors
4. Test protocol URL handling: `aura://open?path=...`
5. Check logs for successful protocol registration

## Related Files

- `Aura.Desktop/electron/main.js` - Main process entry point
- `Aura.Desktop/electron/protocol-handler.js` - Protocol handler module

## References

- [Electron Protocol API](https://www.electronjs.org/docs/latest/api/protocol)
- [registerSchemesAsPrivileged Documentation](https://www.electronjs.org/docs/latest/api/protocol#protocolregisterschemesasprivilegedcustomschemes)
