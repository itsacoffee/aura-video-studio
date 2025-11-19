# Aura Video Studio - Desktop Application

> **🎬 One-click installable AI-powered video generation studio**

This directory contains the Electron desktop application that wraps Aura Video Studio into a native app for Windows.

**Note:** Currently, only Windows builds are supported. macOS and Linux builds have been disabled to focus on the primary target platform (Windows 10/11).

## ✨ Features

- **🚀 One-Click Installation**: Native Windows installer (NSIS) and portable executable
- **🔧 Auto-Configuration**: Automatic dependency detection and installation
- **📦 Self-Contained**: Bundled .NET backend, no manual setup required
- **🔄 Auto-Updates**: Seamless background updates
- **🎯 System Tray**: Quick access from taskbar
- **⚙️ Native Integration**: File associations, notifications, Windows integration

## 📥 Download

Get the latest release:

- **Windows (x64)**: `Aura-Video-Studio-Setup-1.0.0.exe` (installer) or `Aura-Video-Studio-1.0.0-portable.exe` (standalone)

[📦 Download Latest Release →](https://github.com/coffee285/aura-video-studio/releases/latest)

## 🚀 Quick Start

### For Users

1. Download the Windows installer
2. Run the installer
3. Launch Aura Video Studio
4. Follow the first-run setup wizard
5. Start creating videos! 🎥

See [INSTALLATION.md](../INSTALLATION.md) for detailed instructions.

### For Developers

#### Prerequisites

- Node.js 20+
- .NET 8.0 SDK
- npm 9+
- Windows 10 or later (for building)

#### Development Mode

```bash
# Install dependencies
npm install

# Build the frontend
cd ../Aura.Web
npm install
npm run build

# Start Electron in dev mode
cd ../Aura.Desktop
npm start
```

See [DESKTOP_APP_GUIDE.md](../DESKTOP_APP_GUIDE.md) for development documentation.

## 🏗️ Building

### Build for Windows

```bash
# Using PowerShell (recommended)
.\build-desktop.ps1

# Or using npm directly
npm run build:win
```

Output will be in the `dist/` directory.

### Build Options

```powershell
# Skip installer creation (faster, for testing)
.\build-desktop.ps1 -SkipInstaller

# Skip frontend build (if already built)
.\build-desktop.ps1 -SkipFrontend

# Skip backend build (if already built)
.\build-desktop.ps1 -SkipBackend
```

### Clean Environment for Testing

When testing builds, you may want to start with a completely clean environment to simulate a first-run experience:

```powershell
# Remove all application data (recommended for testing)
.\clean-desktop.ps1

# Preview what would be removed (dry run)
.\clean-desktop.ps1 -DryRun

# Remove EVERYTHING including user documents and videos (use with caution!)
.\clean-desktop.ps1 -IncludeUserContent
```

The cleanup script removes:
- Application configuration and cache (`%LOCALAPPDATA%\aura-video-studio`)
- Downloaded tools and engines (`%LOCALAPPDATA%\Aura\Tools`)
- Logs and diagnostics
- Temporary processing files
- Build artifacts (dist, bin, obj folders)

By default, user documents and videos are **preserved**. Use `-IncludeUserContent` to remove them as well (not recommended unless intentional).

## 📁 Project Structure

```
Aura.Desktop/
├── electron/                   # Main process modules (modular architecture) ✅ ACTIVE
│   ├── main.js                # ✅ CANONICAL ENTRY POINT - Application main process
│   ├── preload.js             # ✅ CANONICAL PRELOAD - Secure IPC bridge
│   ├── window-manager.js      # Window lifecycle management
│   ├── app-config.js          # Configuration storage
│   ├── backend-service.js     # Backend process management
│   ├── tray-manager.js        # System tray integration
│   ├── menu-builder.js        # Application menu
│   ├── protocol-handler.js    # aura:// protocol support
│   ├── windows-setup-wizard.js # First-run setup wizard
│   ├── types.d.ts             # TypeScript definitions
│   └── ipc-handlers/          # IPC channel handlers
│       ├── config-handler.js   # Configuration IPC
│       ├── system-handler.js   # System operations
│       ├── video-handler.js    # Video generation
│       ├── backend-handler.js  # Backend control
│       └── ffmpeg-handler.js   # FFmpeg operations
│
├── package.json               # Dependencies and build configuration
├── preload.js                 # ⚠️  LEGACY: Backwards-compat redirect (DO NOT USE)
├── electron.js                # ⚠️  LEGACY: Reference-only with execution guard (DO NOT USE)
├── build-desktop.ps1          # Build script (Windows)
├── build-desktop.sh           # Build script (cross-platform, Windows target only)
│
├── assets/
│   ├── splash.html            # Startup splash screen
│   └── icons/                 # Platform-specific app icons
│       └── icon.ico           # Windows
│
├── build/
│   └── installer.nsh          # Windows NSIS installer customization
│
├── scripts/
│   ├── sign-windows.js                   # Custom code signing script
│   ├── validate-build-config.js          # Build configuration validator
│   ├── validate-electron-config.js       # Electron configuration validator
│   ├── validate-installation.ps1         # Post-installation validation
│   ├── validate-uninstallation.ps1       # Uninstallation cleanup validation
│   ├── test-installation-e2e.ps1         # End-to-end installation test
│   ├── build-backend-windows.ps1         # Backend build automation
│   ├── build-windows.ps1                 # Windows-specific build script
│   ├── download-ffmpeg-windows.ps1       # FFmpeg download automation
│   └── validate-windows-build.ps1        # Windows build validation
│
├── resources/
│   └── backend/               # Bundled .NET backend (generated during build)
│       └── win-x64/           # Windows x64 binaries
└── dist/                      # Build output (installers, packages)
```

### Entry Point Enforcement

**✅ Canonical Entry Points (MUST USE THESE):**
- **Main Process**: `electron/main.js` - This is the ONLY supported Electron main entry point
- **Preload Script**: `electron/preload.js` - This is the ONLY supported preload file

**⚠️  Legacy Files (DO NOT USE IN NEW CODE):**
- **preload.js** (root level) - Backwards compatibility redirect only. Contains strong warnings and safe forwarding to `electron/preload.js`. Any reference to this file in new configuration should be considered a bug.
- **electron.js** (root level) - Historical reference file with immediate execution guard. Contains a throw statement that prevents execution. Kept only for architectural reference. If this file is ever executed, it indicates a critical configuration error.

**Configuration Requirements:**
- `package.json` "main" field MUST be `"electron/main.js"`
- All BrowserWindow preload paths MUST reference `electron/preload.js`
- Build scripts MUST NOT reference `electron.js` or root-level `preload.js`
- electron-builder configuration MUST use `electron/main.js` as entry

## 🔧 Configuration

### Modular Architecture

The application uses a **modular architecture** for better maintainability:

- **electron/main.js** - Entry point that orchestrates all modules (CANONICAL ENTRY)
- **electron/preload.js** - Secure IPC bridge (CANONICAL PRELOAD)
- **electron/window-manager.js** - Window creation and lifecycle
- **electron/backend-service.js** - Backend spawning and health monitoring
- **electron/app-config.js** - Persistent configuration with encryption
- **electron/tray-manager.js** - System tray integration
- **electron/menu-builder.js** - Application menu creation
- **electron/ipc-handlers/** - Secure IPC channel handlers

### Migration Notes

**Monolithic to Modular Migration (COMPLETE)**

The Electron application has been fully migrated from a monolithic architecture to a clean, modular design:

- **Before**: Single 867-line `electron.js` file with all functionality
- **After**: Modular architecture with focused, maintainable modules in `electron/` directory

**Key Changes:**
- ✅ Main entry point moved to `electron/main.js`
- ✅ Preload script moved to `electron/preload.js`
- ✅ Functionality split into focused modules (window-manager, backend-service, etc.)
- ✅ Type safety with TypeScript definitions
- ✅ Improved testability and maintainability

**Legacy File Status:**
- `electron.js` - Contains execution guard, will throw error if loaded
- `preload.js` (root) - Safe redirect with warnings, forwards to `electron/preload.js`

**No build or run script should ever reference the legacy `electron.js` file.**

If you encounter an error from `electron.js`, it means your configuration is incorrect. Update your `package.json` or build scripts to reference `electron/main.js` instead.

### Validation

Ensure configuration integrity with built-in validators:

```bash
# Validate Electron configuration
npm run validate:electron

# Validate build configuration
npm run validate
```

### package.json

Build configuration:
- Windows platform target (x64)
- Entry point: `"main": "electron/main.js"`
- Installer options (NSIS, Portable)
- Optional code signing configuration
- Auto-update settings

## 🔐 Security

The desktop app follows Electron security best practices:

- ✅ **Context Isolation**: Renderer process is sandboxed
- ✅ **No Node Integration**: Renderer can't access Node.js directly
- ✅ **Secure IPC**: All communication via contextBridge
- ✅ **Web Security**: Prevents loading arbitrary remote content
- ✅ **Encrypted Storage**: Sensitive config encrypted with OS keychain

## 🎨 Customization

### Icons

Replace icons in `assets/icons/`:
- `icon.ico` - Windows (256x256 multi-size)
- `tray.png` - System tray (16x16 or 22x22)

See [assets/icons/README.md](assets/icons/README.md) for details.

### Splash Screen

Edit `assets/splash.html` to customize the startup splash screen.

### Installer Branding

Edit `build/installer.nsh` for NSIS installer customization.

## 📦 Distribution

### GitHub Releases

The easiest way to distribute:

1. Tag a release: `git tag v1.0.0 && git push origin v1.0.0`
2. Build Windows installers: `npm run build:win`
3. Create GitHub Release and upload artifacts
4. Users get auto-update notifications

### Platform Stores

- **Microsoft Store**: Use `appx` target (requires separate configuration)

## 🐛 Troubleshooting

### Build Issues

**"Backend not found"**
```bash
cd ../Aura.Api
dotnet publish -c Release -r win-x64 --self-contained
```

**"Frontend not found"**
```bash
cd ../Aura.Web
npm run build
```

**"Code signing certificate not found"**
Code signing is optional. Set these environment variables if you have a certificate:
```powershell
$env:WIN_CSC_LINK = "path\to\certificate.pfx"
$env:WIN_CSC_KEY_PASSWORD = "your-password"
```

**Clear cache and rebuild**
```bash
rm -rf dist/ node_modules/
npm install
npm run build:win
```

### Runtime Issues

**App won't start**
- Check logs in user data directory
- Run from terminal to see errors
- Verify all dependencies are bundled

**Backend fails to start**
- Check if port is available
- Verify backend has execute permissions
- Check firewall/antivirus isn't blocking

**Auto-update not working**
- Verify GitHub releases are published
- Check network connectivity
- Enable debug logging in electron.js

See [DESKTOP_APP_GUIDE.md](../DESKTOP_APP_GUIDE.md#troubleshooting) for more.

## 🧪 Testing & Validation

### Build Validation

```bash
# Validate build configuration
npm run validate

# Validate Electron configuration
npm run validate:electron
```

### Installation Testing

After building the installer, validate it thoroughly:

```powershell
# Automated installation test (requires clean Windows 11 VM)
.\scripts\test-installation-e2e.ps1 -InstallerPath "dist\Aura-Video-Studio-Setup-1.0.0.exe" -Silent

# Validate installation after install
.\scripts\validate-installation.ps1

# Validate uninstallation after uninstall
.\scripts\validate-uninstallation.ps1
```

### Testing Documentation

- **[INSTALLATION_TEST_CHECKLIST.md](INSTALLATION_TEST_CHECKLIST.md)** - 200+ point testing checklist
- **[INSTALLER_VALIDATION_REPORT.md](INSTALLER_VALIDATION_REPORT.md)** - Comprehensive validation report
- **[WINDOWS_11_TESTING_GUIDE.md](WINDOWS_11_TESTING_GUIDE.md)** - Complete testing guide

## 📚 Documentation

- **[BUILD_INSTRUCTIONS.md](BUILD_INSTRUCTIONS.md)** - Comprehensive build guide
- **[QUICK_START.md](QUICK_START.md)** - Quick start for the new modular architecture
- **[ELECTRON_CONFIG_VERIFICATION.md](ELECTRON_CONFIG_VERIFICATION.md)** - Configuration verification details
- **[INSTALLATION.md](../INSTALLATION.md)** - End-user installation guide
- **[DESKTOP_APP_GUIDE.md](../DESKTOP_APP_GUIDE.md)** - Developer guide
- **[BUILD_GUIDE.md](../BUILD_GUIDE.md)** - General build instructions

## 🤝 Contributing

We welcome contributions! When working on the desktop app:

1. Follow Electron security best practices
2. Test on Windows 10 and Windows 11
3. Update documentation for new features
4. Test both development and production builds

See [CONTRIBUTING.md](../CONTRIBUTING.md) for guidelines.

## 📝 License

MIT License - see [LICENSE.txt](LICENSE.txt) for details.

## 🙏 Credits

Built with:
- [Electron](https://www.electronjs.org/) - Cross-platform desktop apps
- [electron-builder](https://www.electron.build/) - Complete solution to package Electron apps
- [electron-updater](https://www.electron.build/auto-update) - Auto-update support
- [electron-store](https://github.com/sindresorhus/electron-store) - Persistent storage

## 🔗 Links

- [Website](https://aura-video-studio.com)
- [Documentation](https://docs.aura-video-studio.com)
- [GitHub](https://github.com/coffee285/aura-video-studio)
- [Discord](https://discord.gg/aura-video-studio)

---

**Made with ❤️ by the Aura Video Studio team**
