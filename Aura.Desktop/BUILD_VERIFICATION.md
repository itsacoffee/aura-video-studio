# Desktop Build Verification Guide

## Quick Test (Windows Only)

Run this command to test the complete Windows build:

```powershell
cd Aura.Desktop
.\build-desktop.ps1 -Target win
```

## What Gets Built

### 1. Frontend (React/TypeScript)
- **Location**: `../Aura.Web/dist/`
- **Entry Point**: `dist/index.html`
- **Build Command**: `npm run build` (runs Vite production build)

### 2. Backend (.NET 8.0 API)
- **Location**: `resources/backend/win-x64/`
- **Entry Point**: `Aura.Api.exe`
- **Self-Contained**: Yes (includes .NET runtime)
- **Size**: ~80-150 MB

### 3. Electron App
- **Location**: `dist/`
- **Installer**: `Aura Video Studio-Setup-1.0.0.exe` (NSIS)
- **Portable**: `Aura Video Studio-1.0.0-x64.exe`

## Build Output Structure

After successful build:

```
Aura.Desktop/
├── resources/
│   └── backend/
│       └── win-x64/
│           ├── Aura.Api.exe           ← Backend executable
│           ├── Aura.Api.dll
│           ├── appsettings.json
│           └── ... (dependencies)
├── dist/
│   ├── builder-effective-config.yaml
│   ├── Aura Video Studio-Setup-1.0.0.exe    ← NSIS Installer
│   ├── Aura Video Studio-1.0.0-x64.exe      ← Portable
│   └── win-unpacked/                         ← Unpacked app
│       ├── Aura Video Studio.exe
│       └── resources/
│           ├── backend/                      ← Bundled backend
│           │   └── Aura.Api.exe
│           └── frontend/                     ← Bundled frontend
│               └── index.html
```

## Verification Steps

### 1. Check Build Completed Without Errors

Look for these success messages:

```
[SUCCESS] Frontend build complete
[SUCCESS] Windows backend build complete
[SUCCESS] Electron dependencies ready
[SUCCESS]   ✓ Frontend build found
[SUCCESS]   ✓ Backend binaries found
[SUCCESS] All required resources validated
[SUCCESS] Installer build complete
[SUCCESS] Build Complete!
```

### 2. Verify Files Exist

```powershell
# Check backend was built
Test-Path "resources/backend/win-x64/Aura.Api.exe"  # Should be True

# Check installer was created  
Test-Path "dist/Aura Video Studio-Setup-1.0.0.exe"  # Should be True

# Check unpacked app structure
Test-Path "dist/win-unpacked/Aura Video Studio.exe"  # Should be True
Test-Path "dist/win-unpacked/resources/backend/Aura.Api.exe"  # Should be True
Test-Path "dist/win-unpacked/resources/frontend/index.html"  # Should be True
```

### 3. Check File Sizes

Backend should be substantial (includes .NET runtime):
```powershell
(Get-Item "resources/backend/win-x64/Aura.Api.exe").Length / 1MB
# Expected: 80-150 MB
```

Installer should be large (includes everything):
```powershell
(Get-Item "dist/Aura Video Studio-Setup-1.0.0.exe").Length / 1MB
# Expected: 100-200 MB
```

### 4. Test Installation (Optional)

Run the installer:
```powershell
Start-Process "dist/Aura Video Studio-Setup-1.0.0.exe"
```

Or run the portable version:
```powershell
Start-Process "dist/Aura Video Studio-1.0.0-x64.exe"
```

Or test the unpacked app directly:
```powershell
Start-Process "dist/win-unpacked/Aura Video Studio.exe"
```

## Expected Build Time

- **First Build**: 5-10 minutes (downloads NuGet packages, npm modules)
- **Incremental Build**: 2-5 minutes (cached dependencies)
- **Frontend Only**: 30-60 seconds
- **Backend Only**: 1-3 minutes
- **Installer Only**: 30-60 seconds (if resources exist)

## Build Options

### Skip Steps

Skip frontend if already built:
```powershell
.\build-desktop.ps1 -Target win -SkipFrontend
```

Skip backend if already built:
```powershell
.\build-desktop.ps1 -Target win -SkipBackend
```

Just create installer (skip compilation):
```powershell
.\build-desktop.ps1 -Target win -SkipFrontend -SkipBackend
```

### Build for Different Platforms

Build only for Windows:
```powershell
.\build-desktop.ps1 -Target win
```

Build for macOS (on macOS only):
```powershell
./build-desktop.sh --target mac
```

Build for Linux (on Linux only):
```powershell
./build-desktop.sh --target linux
```

Build for all platforms (cross-compile backend):
```powershell
.\build-desktop.ps1 -Target all
```
Note: Electron installers can only be built on their native platform.

## Troubleshooting

### "Frontend build not found"

Frontend didn't build successfully. Check `Aura.Web`:

```powershell
cd ..\Aura.Web
npm install
npm run build
```

Look for errors in the output. Common issues:
- TypeScript errors
- Missing dependencies
- Node version mismatch (need >=20.0.0)

### "Backend build failed with exit code 1"

.NET compilation failed. Check the error output for:
- **CS errors**: C# compilation errors
- **NETSDK errors**: SDK/project file errors  
- **NU errors**: NuGet package errors

Try cleaning and rebuilding:
```powershell
cd ..\Aura.Api
dotnet clean
dotnet restore
dotnet build
```

### "Resource validation failed"

Required resources are missing. Run the full build without skip flags:

```powershell
.\build-desktop.ps1 -Target win
```

### electron-builder fails

Usually a node_modules issue. Clean and reinstall:

```powershell
Remove-Item -Recurse -Force node_modules, dist
npm install
```

### "Multiple FrameworkReference items" error

This should be fixed in `Aura.Core.csproj`. If you still see it:
1. Pull latest changes
2. Clean solution: `dotnet clean`
3. Rebuild

## Development Workflow

### Testing Changes

After making code changes:

1. **Frontend changes**: 
   ```powershell
   cd Aura.Web
   npm run build
   cd ..\Aura.Desktop
   .\build-desktop.ps1 -SkipBackend -SkipInstaller
   npm start  # Test in development mode
   ```

2. **Backend changes**:
   ```powershell
   cd Aura.Desktop
   .\build-desktop.ps1 -SkipFrontend -SkipInstaller
   ```

3. **Electron changes**:
   ```powershell
   # Just restart electron
   npm start
   ```

### Clean Build

When things get weird, do a clean build:

```powershell
# Clean everything
Remove-Item -Recurse -Force resources/backend, dist

# Clean .NET
cd ..\Aura.Api
dotnet clean

# Clean frontend
cd ..\Aura.Web  
Remove-Item -Recurse -Force dist

# Clean Electron
cd ..\Aura.Desktop
Remove-Item -Recurse -Force node_modules

# Full rebuild
npm install
cd ..\Aura.Web
npm install
cd ..\Aura.Desktop
.\build-desktop.ps1 -Target win
```

## Success Criteria

✅ Build completes without errors
✅ No "false success" messages  
✅ All validation checks pass
✅ Installer file exists in `dist/`
✅ Backend executable exists in `resources/backend/win-x64/`
✅ Frontend files exist in `../Aura.Web/dist/`
✅ No compilation warnings (or only minor ones)
✅ No security vulnerabilities in npm audit
✅ Installed app launches and shows UI
✅ Backend API responds (check tray icon for port)

## Need Help?

Check these files for details:
- `BUILD_FIXES_SUMMARY.md` - Comprehensive list of fixes applied
- `BUILD_INSTRUCTIONS.md` - Original build instructions
- `package.json` - Electron builder configuration
- `electron.js` - Electron main process (shows expected paths)
