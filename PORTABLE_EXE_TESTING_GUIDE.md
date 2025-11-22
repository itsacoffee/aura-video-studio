# Portable EXE Testing Guide

## Overview

This guide explains how to test the portable .exe build of Aura Video Studio and verify that all critical features work correctly.

## Recent Fixes Applied

### 1. Fixed Blank White Screen Issue

**Problem**: Production builds showed a blank white screen due to overly restrictive Content Security Policy (CSP).

**Fix**: Updated `Aura.Desktop/electron/window-manager.js` to allow inline scripts and eval in production mode:

- Added `'unsafe-inline'` and `'unsafe-eval'` to `script-src` directive
- Added backend connection URLs to `connect-src` directive
- Production CSP is now permissive for Electron while still maintaining security through sandboxing

### 2. Fixed FFmpeg Detection

**Problem**: Managed FFmpeg (installed via build-desktop.ps1) was not detected correctly.

**Fix**: Updated `Aura.Desktop/electron/backend-service.js`:

- Improved FFmpeg path detection with detailed logging
- Corrected environment variable format (full path to ffmpeg.exe instead of bin directory)
- Added console output showing exactly where FFmpeg is searched for
- Enhanced persistence of FFmpeg path to backend configuration

### 3. Enhanced Diagnostic Logging

**Problem**: Difficult to debug why frontend wasn't loading.

**Fix**: Added comprehensive logging to both files:

- Window manager now logs HTML file contents, asset directory structure, and script tag detection
- Backend service logs all FFmpeg search paths and environment variables
- Clear indication when FFmpeg is found vs. when system PATH will be searched

## Pre-Build Checklist

Before building the portable .exe, ensure:

1. **Frontend is built**:

   ```powershell
   cd Aura.Web
   npm install
   npm run build
   ```

2. **Backend is built**:

   ```powershell
   cd Aura.Api
   dotnet publish -c Release -r win-x64 --self-contained true -o ../Aura.Desktop/resources/backend/win-x64
   ```

3. **FFmpeg is installed**:

   ```powershell
   cd Aura.Desktop
   pwsh -File scripts/ensure-ffmpeg.ps1
   ```

   This should download FFmpeg to `Aura.Desktop/resources/ffmpeg/win-x64/bin/ffmpeg.exe`

## Building the Portable EXE

Run the build script from the Aura.Desktop directory:

```powershell
cd Aura.Desktop
pwsh -File build-desktop.ps1 -Target win
```

This will:

1. Clean all build artifacts
2. Build the frontend (Aura.Web)
3. Build the backend (Aura.Api)
4. Ensure FFmpeg binaries are present
5. Package everything with Electron Builder
6. Create both installer and portable .exe in `Aura.Desktop/dist/`

## Testing the Portable EXE

### Initial Launch Test

1. **Locate the portable .exe**:

   - Navigate to `Aura.Desktop/dist/`
   - Find file named like: `Aura Video Studio-1.0.0-x64.exe`

2. **First Run**:

   - Double-click the .exe
   - You should see:
     - Splash screen with "Starting backend server..." (5-10 seconds)
     - Welcome screen loads (should NOT be blank white screen)
     - Clean UI with purple gradient theme

3. **Check DevTools (F12)**:
   - Press `F12` to open DevTools
   - **Console tab**: Should show app initialization logs, NO red errors about CSP or failed script loads
   - **Network tab**: All assets should load with status 200
   - **Application tab > Local Storage**: Should see configuration data

### Welcome Wizard Test (Step 0)

1. Read the welcome message
2. Click "Get Started" button
3. Should transition to FFmpeg Check screen

### FFmpeg Detection Test (Step 1-2)

1. **Automatic Detection**:

   - Status should show "FFmpeg: Detected" with green checkmark
   - Path should show: `C:\Users\<YourUser>\AppData\Local\Programs\aura-video-studio\resources\ffmpeg\win-x64\bin\ffmpeg.exe`
   - Or similar path pointing to bundled FFmpeg

2. **If Not Detected**:

   - Check Electron console logs (in terminal where you started the app):
     ```
     [BackendService] Searching for FFmpeg in candidate paths:
     [BackendService]   Checking: C:\...\resources\ffmpeg\win-x64\bin
     [BackendService] ✓ Found FFmpeg at: ...
     ```
   - If all paths show "✗ Not found", FFmpeg was not bundled correctly

3. **Manual Verification**:

   - Open File Explorer
   - Navigate to the installation directory (check path in logs)
   - Verify `ffmpeg.exe` exists in `resources/ffmpeg/win-x64/bin/`

4. **Backend Configuration Check**:

   - Look for log: `[Backend] ✓ FFmpeg path persisted successfully to backend config`
   - This confirms the backend stored the FFmpeg path

5. **Click "Continue"** to proceed to provider configuration

### API Key Configuration Test (Step 3)

1. **OpenAI Configuration**:

   - Enter your OpenAI API key (starts with `sk-`)
   - Click "Validate OpenAI" button
   - Should see one of:
     - ✓ "Valid - 15 models available" (if key is valid)
     - ✗ "Invalid API key" (if key is wrong)
     - ⚠ "Could not validate - network error" (if offline)

2. **Pexels Configuration**:

   - Enter your Pexels API key
   - Click "Validate Pexels" button
   - Should see validation result

3. **Other Providers** (optional):

   - Test Anthropic, Google Gemini, ElevenLabs, etc.
   - Each should validate independently

4. **Offline Mode**:

   - Check "Use Offline Mode" if you don't want to configure providers
   - This skips API key validation

5. **Click "Continue"** to proceed to workspace setup

### Workspace Setup Test (Step 4)

1. **Default Paths**:

   - Projects path should show: `C:\Users\<YourUser>\Documents\Aura Projects`
   - Outputs path should show: `C:\Users\<YourUser>\Videos\Aura Outputs`

2. **Browse for Folder**:

   - Click "Browse" button for projects
   - Folder picker dialog should open
   - Select a folder
   - Path should update

3. **Click "Complete Setup"**

### Main Application Test (Step 5)

1. **Dashboard**:

   - Should see main dashboard with navigation sidebar
   - No errors in console

2. **Create Project**:

   - Click "New Project" or similar
   - Enter project details
   - Verify project is created

3. **Provider Status**:

   - Navigate to Settings > Providers
   - OpenAI should show as "Connected" with green status
   - Pexels should show as "Connected"
   - FFmpeg should show path: `C:\...\ffmpeg.exe`

4. **Generate Video**:
   - Create a simple text-to-video job
   - Submit generation
   - Check that:
     - Backend receives the request
     - FFmpeg is invoked
     - No errors about missing FFmpeg

## Troubleshooting

### Blank White Screen

**Symptoms**: Application window opens but shows only white screen, no UI elements.

**Diagnosis**:

1. Open DevTools (F12)
2. Check Console tab for errors:
   - "Refused to execute script" → CSP issue
   - "Failed to load resource" → Asset path issue
   - "Cannot read property of undefined" → React initialization error

**Solutions**:

- Verify the CSP fix is applied (check window-manager.js line 500-520)
- Verify frontend build completed: check `Aura.Web/dist/index.html` exists
- Verify assets are bundled: check `Aura.Desktop/dist/win-unpacked/resources/frontend/`
- Rebuild: `pwsh -File build-desktop.ps1 -Target win`

### FFmpeg Not Detected

**Symptoms**: Wizard shows "FFmpeg: Not Detected"

**Diagnosis**:

1. Check Electron console output for:

   ```
   [BackendService] Searching for FFmpeg in candidate paths:
   [BackendService]   Checking: <path1>
   [BackendService] ✗ Not found at: <path1>
   ...
   ```

2. Check if FFmpeg was bundled:
   - Navigate to `Aura.Desktop/resources/ffmpeg/win-x64/bin/`
   - Verify `ffmpeg.exe` exists (should be ~100 MB)

**Solutions**:

- Run FFmpeg installation script:
  ```powershell
  cd Aura.Desktop
  pwsh -File scripts/ensure-ffmpeg.ps1
  ```
- Verify the file exists before rebuilding
- Rebuild the portable exe: `pwsh -File build-desktop.ps1 -Target win`

### API Keys Not Validating

**Symptoms**: Clicking "Validate" button shows error or "Could not validate"

**Diagnosis**:

1. Check backend logs for API call errors
2. Check browser console for network errors
3. Verify backend is running: `http://localhost:5272/health/ready`

**Solutions**:

- Ensure backend started successfully (check splash screen logs)
- Check firewall isn't blocking localhost connections
- Try "Allow me to continue with invalid API keys" checkbox to skip validation
- Verify API keys are correct (check provider dashboards)

### Backend Not Starting

**Symptoms**: Splash screen shows "Starting backend server..." indefinitely

**Diagnosis**:

1. Check Electron console for backend startup errors:

   ```
   [Backend] Process exited with code 1
   [Backend] Error output: <error details>
   ```

2. Check if .NET 8 runtime is installed:
   ```powershell
   dotnet --version
   ```

**Solutions**:

- Install .NET 8 Runtime from https://dotnet.microsoft.com/download
- Check backend logs in: `%APPDATA%\aura-video-studio\logs\`
- Verify backend was built correctly: check `Aura.Desktop/resources/backend/win-x64/Aura.Api.exe`
- Rebuild backend:
  ```powershell
  cd Aura.Api
  dotnet publish -c Release -r win-x64 --self-contained true
  ```

## Log Files

### Electron Main Process Logs

- Location: Terminal where you ran the .exe
- Contains: Backend startup, window manager, FFmpeg detection

### Backend API Logs

- Location: `%APPDATA%\aura-video-studio\logs\`
- Files: `backend-<date>.log`, `startup-<date>.log`
- Contains: API requests, FFmpeg operations, database queries

### Frontend Logs

- Location: DevTools Console (F12)
- Contains: React errors, API calls, state management

### Startup Diagnostics

- Location: `%APPDATA%\aura-video-studio\logs\startup-summary-<timestamp>.json`
- Contains: Detailed initialization steps and timing

## Success Criteria

A successful portable .exe build should:

✅ **Launch** - Application window opens within 10 seconds  
✅ **UI Loads** - Welcome wizard displays with no blank screen  
✅ **FFmpeg Detected** - Bundled FFmpeg is automatically detected  
✅ **API Keys** - OpenAI/Pexels keys can be entered and validated  
✅ **Wizard Complete** - Can complete all 5 setup steps  
✅ **Main App** - Dashboard loads and is functional  
✅ **Video Generation** - Can create and submit a video generation job  
✅ **No Console Errors** - DevTools console shows no critical errors

## Reporting Issues

If you encounter issues, please provide:

1. **System Information**:

   - Windows version
   - .NET version (`dotnet --version`)
   - Node.js version (`node --version`)

2. **Log Files**:

   - Electron console output (copy/paste)
   - Backend logs from `%APPDATA%\aura-video-studio\logs\`
   - DevTools console errors (screenshots)

3. **Steps to Reproduce**:

   - What you did
   - What you expected
   - What actually happened

4. **Screenshots**:
   - Blank screen or error dialogs
   - DevTools console errors
   - Setup wizard state

## Build Verification Checklist

Before distributing the portable .exe, verify:

- [ ] Build completes without errors
- [ ] File size is reasonable (~200-300 MB)
- [ ] `dist/win-unpacked/resources/frontend/` contains HTML and assets
- [ ] `dist/win-unpacked/resources/backend/win-x64/` contains Aura.Api.exe
- [ ] `dist/win-unpacked/resources/ffmpeg/win-x64/bin/` contains ffmpeg.exe
- [ ] Double-clicking .exe shows splash screen, not immediate crash
- [ ] Welcome wizard loads with proper UI
- [ ] FFmpeg is detected automatically
- [ ] API keys can be validated
- [ ] Setup completes successfully
- [ ] Main application is functional

## Advanced Testing

### Clean Slate Test

Test the first-run experience on a machine without previous installations:

1. Uninstall any previous versions
2. Delete `%APPDATA%\aura-video-studio\`
3. Delete `%LOCALAPPDATA%\aura-video-studio\`
4. Run portable .exe
5. Verify clean first-run experience

### Offline Test

Test that the app works without internet:

1. Disconnect network
2. Run portable .exe
3. Choose "Offline Mode" in wizard
4. Verify app functions (except API-dependent features)

### Upgrade Test

Test upgrading from a previous version:

1. Install/run older version
2. Complete setup
3. Close application
4. Run new portable .exe
5. Verify settings are preserved
6. Verify no migration errors

## Performance Benchmarks

Expected performance metrics:

- **Cold Start**: 8-12 seconds from .exe launch to main window
- **Backend Ready**: 3-5 seconds from splash to backend /health response
- **FFmpeg Detection**: < 1 second
- **API Key Validation**: 2-5 seconds per provider
- **Memory Usage**: 300-500 MB (Electron) + 100-200 MB (Backend)

## Known Limitations

- Portable .exe cannot auto-update (must download new version manually)
- Antivirus may flag on first run (whitelist if necessary)
- Windows Defender SmartScreen may show warning (click "More info" → "Run anyway")
- Requires ~1GB free disk space for installation and temporary files

## Support Resources

- Documentation: `docs/` folder
- Issues: GitHub Issues
- Logs: `%APPDATA%\aura-video-studio\logs\`
- Configuration: `%APPDATA%\aura-video-studio\config.json`
