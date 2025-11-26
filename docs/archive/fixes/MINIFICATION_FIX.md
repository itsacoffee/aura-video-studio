# Minification Fix - "Cannot access 'e' before initialization"

## Problem

After fixing the preload script, the app loaded but showed error:

```
vendor-buxngcDg.js:1 Uncaught ReferenceError: Cannot access 'e' before initialization
```

This caused the app to fail initialization and show the blank white screen.

## Root Cause

The Vite configuration was using **terser** for minification with aggressive settings:

```typescript
minify: 'terser',
terserOptions: {
  compress: {
    drop_console: isProduction, // Dropping console.logs
    passes: 2, // Multiple optimization passes
  },
  mangle: {
    safari10: true,
  },
},
```

Terser's aggressive minification was causing variable hoisting issues where:
1. A variable was being accessed before its declaration
2. The minified code had circular references
3. ES module initialization order was broken

## Solution

Changed from `terser` to `esbuild` minification:

```typescript
// Use esbuild minification for better compatibility with Electron
// terser was causing "Cannot access 'e' before initialization" errors
minify: 'esbuild',
```

### Why esbuild?

1. **Faster**: 10-100x faster than terser
2. **More Compatible**: Better handles modern ES modules
3. **Safer**: Less aggressive optimizations that preserve execution order
4. **Electron-Friendly**: Works better with Electron's Chromium version

### Tradeoffs

- **Bundle Size**: esbuild produces slightly larger bundles (~5-10% bigger)
- **Console Logs**: Kept in production (helpful for Electron debugging)
- **Build Speed**: Much faster builds (seconds vs minutes)

For an Electron desktop app, these tradeoffs are acceptable and preferable.

## Rebuild Steps

### 1. Clean Frontend Build

```powershell
cd Aura.Web
Remove-Item -Recurse -Force dist, node_modules\.vite
```

### 2. Rebuild Frontend

```powershell
npm run build
```

This will now use esbuild for minification.

### 3. Rebuild Electron App

```powershell
cd ../Aura.Desktop
pwsh -File build-desktop.ps1 -Target win
```

### 4. Test

```powershell
cd dist
.\Aura Video Studio-1.0.0-x64.exe
```

## Expected Result

✅ No "Cannot access 'e' before initialization" error  
✅ No blank white screen  
✅ Welcome wizard loads  
✅ Console shows initialization logs  

## Verification

Open DevTools (F12) and verify:

```
[Preload] ✓ Backend URL confirmed: http://127.0.0.1:5890
Enhanced preload script loaded
[Main] Initializing Aura Video Studio
[Main] API Base URL: http://127.0.0.1:5890
```

**No errors about variable initialization!**

## Alternative Solution (If Still Issues)

If the error persists, you can disable minification entirely for debugging:

```typescript
// In vite.config.ts
build: {
  minify: false, // Disable all minification
  // ...
}
```

Then rebuild and test. This will help identify if minification is still the issue.

## Related Files

- `Aura.Web/vite.config.ts` - Build configuration
- `Aura.Web/package.json` - Build scripts

## References

- [Vite Build Options](https://vitejs.dev/config/build-options.html#build-minify)
- [esbuild vs terser](https://esbuild.github.io/)
- [Electron & Vite](https://www.electronforge.io/guides/framework-integration/vite)

