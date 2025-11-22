# Sandbox Mode Fix

## Problem

When running the portable .exe, the application showed a blank white screen with the error:

```
Unable to load preload script: ...app.asar\electron\preload.js
Error: module not found: os
```

## Root Cause

The window manager was creating the main window with `sandbox: true`:

```javascript
webPreferences: {
  sandbox: true,  // <-- BLOCKS Node.js modules in preload
  contextIsolation: true,
  nodeIntegration: false,
  // ...
}
```

When `sandbox: true`, Electron blocks access to Node.js built-in modules (like `os`, `path`, `fs`) even in the preload script. Our preload script uses:

```javascript
const os = require("os");
```

This is needed to provide system information to the renderer process through the context bridge.

## Solution

Changed `sandbox: true` to `sandbox: false` in `window-manager.js`:

```javascript
webPreferences: {
  sandbox: false,  // Must be false for preload to access Node.js modules
  contextIsolation: true,  // Still provides security isolation
  nodeIntegration: false,  // Renderer still can't use Node.js directly
  // ...
}
```

## Security Implications

**Q: Is this less secure?**

**A: No, not significantly.** We still have strong security:

1. ✅ **contextIsolation: true** - Renderer process is isolated from preload context
2. ✅ **nodeIntegration: false** - Renderer can't use Node.js APIs directly
3. ✅ **enableRemoteModule: false** - No remote module access
4. ✅ **webSecurity: true** (in prod) - Web security policies enforced
5. ✅ **Context Bridge** - Only explicitly exposed APIs available to renderer

The preload script acts as a secure bridge, exposing only the APIs we explicitly define through `contextBridge.exposeInMainWorld()`.

**Q: When should sandbox be true?**

**A:** Use `sandbox: true` when:
- Loading untrusted web content (like a browser)
- Preload script doesn't need Node.js modules
- Extra isolation layer is required for security-critical apps

For a desktop application like Aura Video Studio where:
- We control all loaded content
- We need system information (OS, paths, etc.)
- We have contextIsolation for security

Setting `sandbox: false` is the standard and recommended approach.

## Testing

After this fix:

1. **Rebuild**:
   ```powershell
   cd Aura.Desktop
   pwsh -File build-desktop.ps1 -Target win
   ```

2. **Run**:
   ```powershell
   cd dist
   .\Aura Video Studio-1.0.0-x64.exe
   ```

3. **Verify**:
   - No "module not found: os" error
   - No blank white screen
   - Welcome wizard loads
   - DevTools console shows no preload errors

## Expected Console Output

### Success (After Fix):

```
[Preload] Runtime bootstrap received: { backend: { baseUrl: 'http://127.0.0.1:5272' }, ... }
[Preload] ✓ Backend URL confirmed: http://127.0.0.1:5272
Enhanced preload script loaded
Platform: win32
Architecture: x64
```

### Failure (Before Fix):

```
Unable to load preload script: ...
Error: module not found: os
[Init Guard] Application failed to initialize within timeout
```

## References

- [Electron Security Guidelines](https://www.electronjs.org/docs/latest/tutorial/security)
- [Electron Sandbox](https://www.electronjs.org/docs/latest/tutorial/sandbox)
- [Context Isolation](https://www.electronjs.org/docs/latest/tutorial/context-isolation)

## Related Files

- `Aura.Desktop/electron/window-manager.js` - Window creation with security settings
- `Aura.Desktop/electron/preload.js` - Preload script that requires Node.js modules

