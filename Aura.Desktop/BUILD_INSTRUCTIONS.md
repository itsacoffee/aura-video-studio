# Electron Installer Build Instructions

## Quick Start

### Windows
```powershell
cd Aura.Desktop
.\build-desktop.ps1
```

### Linux/macOS
```bash
cd Aura.Desktop
./build-desktop.sh
```

## Build Options

### Skip Installer Creation (faster, for testing)
```bash
./build-desktop.sh --skip-installer
npm run build:dir
```

### Build Specific Platform
```bash
npm run build:win    # Windows only
npm run build:mac    # macOS only  
npm run build:linux  # Linux only
npm run build:all    # All platforms
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
dotnet publish -c Release -r linux-x64 --self-contained -o ../Aura.Desktop/resources/backend/linux-x64
dotnet publish -c Release -r win-x64 --self-contained -o ../Aura.Desktop/resources/backend/win-x64
dotnet publish -c Release -r osx-x64 --self-contained -o ../Aura.Desktop/resources/backend/osx-x64
```

### Step 3: Package with Electron
```bash
cd ../Aura.Desktop
npm install
npm run build:linux  # or build:win, build:mac
```

## Output

Installers will be created in `Aura.Desktop/dist/`:
- **Linux**: `*.AppImage`, `*.deb`, `*.rpm`, `*.snap`
- **Windows**: `*-Setup-*.exe`, `*-portable.exe`
- **macOS**: `*.dmg`, `*.zip`

## Troubleshooting

### "electron-builder cannot execute"
- On Linux, installer creation may require: `sudo apt-get install fuse`
- Try building with `--dir` flag first to test packaging

### "Backend not found"
- Run the backend build steps above
- Or skip backend for Electron-only testing

### Type errors during build
- Frontend is configured to build without strict type-checking
- To fix type errors, run: `cd Aura.Web && npm run build:prod:strict`

## Verification

To verify the build works:
```bash
cd Aura.Desktop
npm run build:dir
ls -la dist/linux-unpacked/
```

Should show the packaged Electron app.
