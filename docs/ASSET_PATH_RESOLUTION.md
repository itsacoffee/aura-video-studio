# Asset Path Resolution - Implementation Guide

## Overview

This document describes the comprehensive asset path resolution system implemented in Aura Video Studio, including icon loading, fallback mechanisms, and build-time verification.

## Problem Statement

The original implementation had several issues:
1. Icon paths were hardcoded relative to `__dirname` which breaks in ASAR-packaged apps
2. No fallback mechanism when icon files are missing
3. No file existence checks before calling `nativeImage.createFromPath()`
4. No build-time verification that assets are properly copied
5. Different behavior between development and production builds

## Solution Architecture

### 1. Icon Fallbacks Module (`Aura.Desktop/electron/icon-fallbacks.js`)

**Purpose**: Provides base64-encoded fallback icons that are embedded in the code.

**Features**:
- Three sizes: 16x16, 32x32, and 256x256 pixels
- PNG format with purple gradient (Aura brand colors)
- `getFallbackIcon()` function to create Electron NativeImage from base64
- Total embedded size: ~2KB (minimal overhead)

**Usage**:
```javascript
const { getFallbackIcon } = require('./icon-fallbacks');
const { nativeImage } = require('electron');

// Get fallback icon
const icon = getFallbackIcon(nativeImage, '32'); // Returns NativeImage
```

### 2. Window Manager Icon Resolution

**File**: `Aura.Desktop/electron/window-manager.js`

**Enhanced `_getAppIcon()` Method**:
1. Detects environment (packaged vs development)
2. Tries multiple paths in order of preference:
   - **Production**: `process.resourcesPath/assets/icons/`
   - **Production**: ASAR unpacked paths
   - **Development**: Relative to electron directory
   - **Development**: Relative to cwd
3. For each path:
   - Checks file existence with `fs.existsSync()`
   - Attempts to load with `nativeImage.createFromPath()`
   - Verifies icon is not empty with `icon.isEmpty()`
   - Logs detailed debug information
4. Falls back to base64 icon if all paths fail

**Logging Output**:
```
=== Icon Resolution Debug Info ===
Platform: win32
Is packaged: true
__dirname: C:\...\resources\app.asar\electron
app.getAppPath(): C:\...\resources\app.asar
process.resourcesPath: C:\...\resources
Icon name: icon.ico
Trying icon path: C:\...\resources\assets\icons\icon.ico
✓ Found icon at: C:\...\resources\assets\icons\icon.ico
✓ Icon loaded successfully
```

### 3. Tray Manager Icon Resolution

**File**: `Aura.Desktop/electron/tray-manager.js`

**Enhanced Features**:
1. Similar multi-path resolution as window manager
2. Tries tray-specific icon (`tray.png`) first
3. Falls back to platform icon (`icon.ico`/`icon.icns`/`icon.png`)
4. Uses base64 fallback if file loading fails
5. Comprehensive error handling - never crashes, always provides working icon

**Behavior**:
- If icon file exists: Loads from file
- If icon file missing: Uses base64 fallback
- If base64 fails: Gracefully skips tray creation (optional feature)
- Logs all attempts and outcomes

### 4. Build-Time Asset Verification

#### Vite Plugin (`Aura.Web/vite.config.ts`)

**Asset Verification Plugin**:
- Runs after build completes
- Checks critical assets exist in dist:
  - `favicon.ico`, `favicon-16x16.png`, `favicon-32x32.png`
  - `logo256.png`, `logo512.png`
  - `vite.svg`
  - `assets/` directory
  - `workspaces/` directory
- Reports file sizes and counts
- Fails build if critical assets missing

**Output Example**:
```
📁 Asset Verification Report:

✓ favicon.ico (99.89 KB)
✓ favicon-16x16.png (0.64 KB)
✓ favicon-32x32.png (1.79 KB)
✓ logo256.png (68.69 KB)
✓ logo512.png (273.54 KB)
✓ vite.svg (1.46 KB)
✓ assets/ (135 items)
✓ workspaces/ (1 items)

✓ All critical assets verified
```

#### Post-Build Script (`scripts/build/verify-build.js`)

**Enhanced Verification**:
- Verifies `index.html` exists
- Checks `assets/` directory
- Verifies all critical static assets
- Checks workspace templates directory
- Ensures no source files in dist
- Ensures no node_modules in dist
- Reports total file count and size

### 5. Path Resolution Strategy

#### Development Mode (`isDev = true`, `app.isPackaged = false`)

**Paths Tried** (in order):
1. `__dirname/../assets/icons/icon.ico`
2. `process.cwd()/Aura.Desktop/assets/icons/icon.ico`
3. `process.cwd()/assets/icons/icon.ico`

**Why**: In development, files are in original directory structure

#### Production Mode (`isDev = false`, `app.isPackaged = true`)

**Paths Tried** (in order):
1. `process.resourcesPath/assets/icons/icon.ico`
2. `process.resourcesPath/app.asar.unpacked/assets/icons/icon.ico`
3. `app.getAppPath()/assets/icons/icon.ico`

**Why**: In production, files may be in ASAR or unpacked in resources directory

### 6. ASAR Packaging Considerations

**Electron Builder Configuration** (`Aura.Desktop/package.json`):
```json
{
  "build": {
    "asar": true,
    "asarUnpack": [
      "resources/backend/**/*",
      "resources/ffmpeg/**/*"
    ],
    "files": [
      "electron/**/*",
      "assets/**/*",
      "backend/**/*"
    ]
  }
}
```

**Icon Files**:
- Icons in `assets/icons/` are packaged in ASAR
- Icons can be loaded from ASAR (Electron supports this)
- Fallback base64 icons always available (embedded in code)

## Testing

### Automated Tests

**Test 1: Icon Fallbacks Module** (`scripts/test-icon-fallbacks.js`)
- Verifies module exports correct functions
- Validates base64 data is valid PNG
- Checks managers import fallbacks
- Verifies comprehensive logging present

**Test 2: Missing Files Scenario** (`scripts/test-icon-fallbacks-missing.js`)
- Tests fallback icon creation
- Simulates missing icon files
- Verifies no hardcoded success
- Checks error handling

**Run Tests**:
```bash
node scripts/test-icon-fallbacks.js
node scripts/test-icon-fallbacks-missing.js
```

### Manual Testing

#### Test 1: Normal Operation
```bash
# Development
cd Aura.Desktop
npm run dev

# Production build
npm run build
./dist/Aura\ Video\ Studio.exe
```

**Expected**: Icons load from files, debug logs show successful paths

#### Test 2: Missing Icons (Fallback Verification)
```bash
# Temporarily move icons folder
cd Aura.Desktop
mv assets/icons assets/icons.backup

# Run app
npm run dev

# Restore icons
mv assets/icons.backup assets/icons
```

**Expected**:
- Logs show "Icon not found" for each path
- Logs show "Using fallback icon"
- Window and tray display purple gradient icons
- No crashes or errors

#### Test 3: Build Verification
```bash
cd Aura.Web
npm run build
```

**Expected**:
- Asset verification plugin reports all assets present
- Post-build script confirms critical assets exist
- Build succeeds with green checkmarks

## Troubleshooting

### Issue: Icons not loading in development

**Solution**:
1. Check console logs for path resolution attempts
2. Verify `Aura.Desktop/assets/icons/icon.ico` exists
3. Check file permissions
4. Fallback icon should still appear

### Issue: Icons not loading in production

**Solution**:
1. Check `process.resourcesPath` in logs
2. Verify assets folder in installation directory
3. Check electron-builder config includes `assets/**/*`
4. Fallback icon should still appear

### Issue: Tray icon missing

**Solution**:
1. Tray is optional - app continues without it
2. Check logs for "Tray Icon Resolution" section
3. Verify `assets/icons/icon.ico` exists
4. Fallback icon should be used if file missing

### Issue: Build verification fails

**Solution**:
1. Check Vite config includes asset verification plugin
2. Verify `public/` folder contains required assets
3. Check Vite copies public folder to dist
4. Run `npm run build:clean` to clear cache

## Best Practices

### Adding New Icons

1. **Place in correct directory**: `Aura.Desktop/assets/icons/`
2. **Use platform naming**:
   - Windows: `icon.ico` (multi-resolution ICO)
   - macOS: `icon.icns` (Apple Icon Image)
   - Linux: `icon.png` (512x512 or larger)
3. **Tray icon**: `tray.png` (16x16 or 32x32)
4. **Update fallbacks**: If changing default icons, update base64 in `icon-fallbacks.js`

### Adding New Static Assets

1. **Place in**: `Aura.Web/public/`
2. **Update verification**: Add to `assetVerificationPlugin()` in `vite.config.ts`
3. **Update post-build**: Add to critical assets check in `verify-build.js`
4. **Test build**: `npm run build` should verify asset exists

### Debugging Icon Loading

**Enable verbose logging**:
```javascript
// In window-manager.js or tray-manager.js
console.log('=== Icon Resolution Debug Info ===');
// ... existing logs
```

**Check logs**:
- Development: Electron DevTools console
- Production: Main process logs (stdout)

## Security Considerations

### Path Traversal Prevention

- All paths use `path.join()` (safe)
- No user input in path construction
- File existence checked before loading

### ASAR Integrity

- Icons in ASAR are read-only
- Fallback icons are embedded (cannot be tampered)
- No external icon downloads

### Resource Usage

- Fallback icons are small (~2KB total)
- Icons only loaded once at startup
- No memory leaks (proper resource cleanup)

## Future Enhancements

### Potential Improvements

1. **Dynamic icon themes**: Support light/dark mode icons
2. **Custom icons**: Allow users to set custom app icon
3. **Icon caching**: Cache loaded icons for faster restarts
4. **SVG support**: Use SVG icons with better scaling
5. **Icon sets**: Multiple icon packs for different brands

### Not Recommended

- ❌ Downloading icons from internet (security risk)
- ❌ Storing icons in temp folders (cleanup issues)
- ❌ Using system icons only (branding lost)

## Related Files

- `Aura.Desktop/electron/icon-fallbacks.js` - Fallback icon definitions
- `Aura.Desktop/electron/window-manager.js` - Window icon loading
- `Aura.Desktop/electron/tray-manager.js` - Tray icon loading
- `Aura.Desktop/assets/icons/` - Icon files directory
- `Aura.Web/vite.config.ts` - Build-time asset verification
- `scripts/build/verify-build.js` - Post-build asset checks
- `scripts/test-icon-fallbacks.js` - Automated tests
- `scripts/test-icon-fallbacks-missing.js` - Fallback tests

## Conclusion

The asset path resolution system ensures icons always load correctly in both development and production, with comprehensive fallback mechanisms and build-time verification. The system is robust, well-tested, and provides detailed logging for debugging.
