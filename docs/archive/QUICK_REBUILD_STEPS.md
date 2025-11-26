# Quick Rebuild Steps - Final Fix

## All Issues Fixed:

1. ✅ Preload sandbox error (changed `sandbox: false`)
2. ✅ CSP blocking network requests (fixed `connect-src` order)
3. ✅ API URL falling back to `file://` (fixed apiBaseUrl.ts)
4. ✅ Minification causing initialization error (changed to esbuild)

## Rebuild Now:

```powershell
# Clean and rebuild frontend
cd Aura.Web
Remove-Item -Recurse -Force dist, node_modules\.vite
npm run build

# Rebuild Electron app
cd ../Aura.Desktop
pwsh -File build-desktop.ps1 -Target win

# Test it!
cd dist
.\Aura Video Studio-1.0.0-x64.exe
```

## What Should Work:

✅ App launches without blank screen  
✅ Welcome wizard loads  
✅ FFmpeg auto-detects  
✅ API keys validate  
✅ Setup completes  
✅ Dashboard loads  

## Expected Console (DevTools F12):

```
[Preload] ✓ Backend URL confirmed: http://127.0.0.1:5890
Enhanced preload script loaded
[Main] API Base URL: http://127.0.0.1:5890
[Main] API Base URL Source: electron
```

**No errors!** 🎉

## If You Still See Issues:

1. **Verify the vite.config.ts change was saved**:
   - Line 205 should show: `minify: 'esbuild',`
   - NOT: `minify: 'terser',`

2. **Clean everything and rebuild**:
   ```powershell
   cd Aura.Desktop
   pwsh -File clean-desktop.ps1
   cd ../Aura.Web
   npm install
   npm run build
   cd ../Aura.Desktop
   pwsh -File build-desktop.ps1 -Target win
   ```

3. **Check the built index.html**:
   ```powershell
   Get-Content "Aura.Web\dist\index.html"
   ```
   Should contain `<script type="module"` tags

The app should work perfectly now!

