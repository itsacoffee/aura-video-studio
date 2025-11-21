# Backend Build and Bundling Guide

## Overview

This document explains how the Aura.Api backend is built and bundled into the Electron desktop application for production distribution.

## Build Process

### 1. Backend Compilation

The backend is compiled as a self-contained .NET 8 executable using the `build-backend.ps1` script:

```powershell
pwsh -File build-backend.ps1 -Configuration Release -OutputPath Aura.Desktop/resources/backend/win-x64
```

This creates:
- **Single-file executable**: `Aura.Api.exe` (~72 MB)
- **Self-contained**: Includes .NET 8 runtime (no .NET SDK required on end-user machine)
- **Compressed**: Uses single-file compression to reduce size
- **Native libraries**: Includes all native dependencies

### 2. Electron Builder Bundling

The Electron Builder configuration (`Aura.Desktop/package.json`) includes the backend in `extraResources`:

```json
{
  "from": "resources/backend",
  "to": "backend",
  "filter": ["**/*", "!**/*.pdb", "!**/*.xml"]
}
```

This copies the backend from `Aura.Desktop/resources/backend/win-x64/` to the installed app's `resources/backend/win-x64/` directory.

### 3. Path Resolution at Runtime

The `backend-service.js` detects the backend executable using the following order:

1. **Production path** (checked first):
   - `process.resourcesPath/backend/win-x64/Aura.Api.exe`

2. **Development paths** (fallback):
   - `dist/backend/Aura.Api.exe`
   - `Aura.Api/bin/Release/net8.0/win-x64/publish/Aura.Api.exe`
   - `Aura.Api/bin/Debug/net8.0/Aura.Api.exe`

### 4. Windows Firewall Configuration

The NSIS installer (`Aura.Desktop/build/installer.nsh`) automatically adds firewall rules:

```nsis
; Private/Domain profile
netsh advfirewall firewall add rule name="Aura Video Studio Backend" 
  dir=in action=allow program="$INSTDIR\resources\backend\win-x64\Aura.Api.exe" 
  enable=yes profile=private,domain

; Public profile (optional)
netsh advfirewall firewall add rule name="Aura Video Studio Backend (Public)" 
  dir=in action=allow program="$INSTDIR\resources\backend\win-x64\Aura.Api.exe" 
  enable=yes profile=public
```

## Build Commands

### Build Backend Only

```powershell
# From repository root
pwsh -File build-backend.ps1
```

### Build Full Electron App

```powershell
# From Aura.Desktop directory
npm run build:electron          # Both installer and portable
npm run build:electron:installer # NSIS installer only
npm run build:electron:portable  # Portable exe only
```

### Verify Build

```powershell
# From Aura.Desktop directory
pwsh -File scripts/verify-build.ps1
```

## Build Artifacts

After building, you'll find:

```
Aura.Desktop/dist/
├── Aura Video Studio Setup-1.0.0.exe    # NSIS installer (~180 MB)
├── Aura Video Studio-1.0.0-Portable.exe # Portable build (~180 MB)
└── win-unpacked/                         # Unpacked build (for testing)
    └── resources/
        ├── backend/
        │   └── win-x64/
        │       └── Aura.Api.exe          # Backend executable (72 MB)
        ├── frontend/                      # React app (35 MB)
        └── ffmpeg/                        # FFmpeg binaries
```

## Configuration

### Aura.Api.csproj

Self-contained deployment properties:

```xml
<PublishSingleFile>true</PublishSingleFile>
<SelfContained>true</SelfContained>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<PublishTrimmed>false</PublishTrimmed>
<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
```

### Key Benefits

1. **No .NET SDK Required**: Users don't need to install .NET 8 SDK
2. **Single Executable**: Backend is a single `.exe` file
3. **Automatic Startup**: Electron app automatically starts backend
4. **Firewall Rules**: Installer configures Windows Firewall
5. **Offline Capable**: All dependencies bundled

## Troubleshooting

### Backend Not Found

If the app can't find the backend:

1. Check `resources/backend/win-x64/Aura.Api.exe` exists in installation directory
2. Verify build script was run: `npm run backend:build`
3. Check extraResources in package.json includes backend

### Backend Won't Start

If backend fails to start:

1. Check Windows Firewall rules: `netsh advfirewall firewall show rule name="Aura Video Studio Backend"`
2. Verify port 5005 is available
3. Check backend logs in AppData folder

### Build Fails

If build fails:

1. Ensure .NET 8 SDK is installed: `dotnet --version`
2. Ensure PowerShell 7+ is installed: `pwsh --version`
3. Check for single-file publish errors (use AppContext.BaseDirectory instead of Assembly.Location)

## CI/CD Integration

The GitHub Actions workflow (`.github/workflows/build-windows-installer.yml`) automatically:

1. Builds backend using `build-backend.ps1`
2. Builds frontend
3. Packages everything with Electron Builder
4. Verifies build artifacts
5. Uploads installers as artifacts
6. Creates GitHub releases for tagged versions

## Size Considerations

- **Backend**: ~72 MB (self-contained .NET 8 runtime + app)
- **Frontend**: ~35 MB (React app with assets)
- **FFmpeg**: ~50 MB (video processing binaries)
- **Electron**: ~150 MB (Chromium + Node.js)
- **Total Installer**: ~180 MB compressed

## Future Improvements

- [ ] Add support for macOS and Linux builds
- [ ] Implement delta updates for backend
- [ ] Add backend version checking
- [ ] Support multiple backend runtime identifiers
- [ ] Add backend health monitoring
