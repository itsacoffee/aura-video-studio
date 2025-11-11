# Electron Installer Build Instructions

## Important Note

**Currently, only Windows builds are supported.** macOS and Linux builds have been disabled to focus on the target platform (Windows 10/11).

## Quick Start

### Windows
```powershell
cd Aura.Desktop
.\build-desktop.ps1
```

### Linux/macOS (Limited Support)
The build-desktop.sh script is available but only supports Windows target:
```bash
cd Aura.Desktop
./build-desktop.sh --target win
```

## Build Options

### Skip Installer Creation (faster, for testing)
```bash
.\build-desktop.ps1 -SkipInstaller
npm run build:dir
```

### Build Windows Platform
```bash
npm run build:win    # Windows only (x64)
npm run build        # Same as build:win
```

## Manual Build Process

### Step 1: Build Frontend
```bash
cd ../Aura.Web
npm install
npm run build
```

### Step 2: Build Backend (requires .NET 8.0 SDK)
```bash
cd ../Aura.Api
dotnet publish -c Release -r win-x64 --self-contained -o ../Aura.Desktop/resources/backend/win-x64
```

### Step 3: Package with Electron
```bash
cd ../Aura.Desktop
npm install
npm run build:win
```

## Output

Installers will be created in `Aura.Desktop/dist/`:
- **Windows**: `*-Setup-*.exe` (NSIS installer), `*-portable.exe` (standalone)

## Code Signing

Code signing is optional. If you have a code signing certificate:

1. Set environment variables:
   ```powershell
   $env:WIN_CSC_LINK = "path\to\certificate.pfx"
   $env:WIN_CSC_KEY_PASSWORD = "your-password"
   ```

2. Or use base64-encoded certificate:
   ```powershell
   $env:WIN_CSC_LINK = "base64-encoded-certificate-content"
   $env:WIN_CSC_KEY_PASSWORD = "your-password"
   ```

If no certificate is provided, the build will succeed but the installer will be unsigned (users will see security warnings).

## Troubleshooting

### "electron-builder cannot execute"
- Ensure Node.js 18+ and .NET 8.0 SDK are installed
- Try building with `-SkipInstaller` flag first to test packaging

### "Backend not found"
- Run the backend build steps above
- Ensure resources/backend/win-x64 directory exists with Aura.Api.exe

### Type errors during build
- Frontend is configured to build without strict type-checking
- To fix type errors, run: `cd Aura.Web && npm run build:prod:strict`

### Code signing errors
- Code signing is optional; build will proceed without it
- Install Windows SDK if you want to enable signing
- Ensure WIN_CSC_LINK environment variable points to valid certificate

## Verification

To verify the build works:
```bash
cd Aura.Desktop
npm run build:dir
ls -la dist/win-unpacked/
```

Should show the packaged Electron app.
