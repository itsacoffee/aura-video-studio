# Aura Video Studio - Build Guide

This guide provides complete instructions for building Aura Video Studio **desktop application** and its components from source.

## Build Paths

Aura Video Studio is an Electron desktop application. There are two build approaches:

1. **Desktop App Build (Recommended for Users)** - Build the complete Electron installer
2. **Component Build (For Development)** - Build individual components for testing

**For most users:** Use the [Desktop App Build](#desktop-app-build) section.

**For developers:** See [DEVELOPMENT.md](DEVELOPMENT.md) for component development and [DESKTOP_APP_GUIDE.md](DESKTOP_APP_GUIDE.md) for Electron development.

---

## Table of Contents

- [Prerequisites](#prerequisites)
- [Desktop App Build](#desktop-app-build)
- [Component Build (Development)](#component-build-development)
- [Environment Setup](#environment-setup)
- [Validation Scripts](#validation-scripts)
- [Git Hooks](#git-hooks)
- [Troubleshooting](#troubleshooting)

## Prerequisites

### Required Software

1. **Node.js 20.0.0 or higher**
   - Download from [nodejs.org](https://nodejs.org/)
   - Or use [nvm](https://github.com/nvm-sh/nvm) (Linux/Mac) / [nvm-windows](https://github.com/coreybutler/nvm-windows) (Windows)
   - **Note:** Aura.Web requires Node 20+, Aura.Desktop requires Node 18+

2. **npm 9.x or higher**
   - Comes with Node.js
   - Update if needed: `npm install -g npm@latest`

3. **.NET 8 SDK**
   - Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0)

4. **Git**
   - Download from [git-scm.com](https://git-scm.com/)

5. **FFmpeg** (required for video rendering)
   - **Windows**: Download from [ffmpeg.org](https://ffmpeg.org/download.html) or use `winget install ffmpeg`
   - **macOS**: `brew install ffmpeg`
   - **Linux**: `sudo apt-get install ffmpeg` (Ubuntu/Debian)

6. **Electron Builder dependencies** (Windows only for Windows builds)
   - Automatically installed via npm
   - NSIS installer tools (bundled with electron-builder)

### Optional (Recommended)

- **nvm** for managing Node.js versions
- **Visual Studio Code** with recommended extensions
- **Windows Terminal** (Windows users)

## Environment Setup

### Windows 11 Specific Setup

1. **Enable Git Long Paths**
   ```powershell
   git config --global core.longpaths true
   ```

2. **Configure Git Line Endings**
   ```powershell
   git config --global core.autocrlf true
   ```

3. **Set PowerShell Execution Policy** (if you plan to run PowerShell scripts)
   ```powershell
   # Run PowerShell as Administrator
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
   ```

### Linux/macOS Setup

1. **Configure Git Line Endings**
   ```bash
   git config --global core.autocrlf input
   ```

### Node.js Version Management with nvm

Using nvm ensures consistency with the recommended Node.js version:

**Linux/macOS:**
```bash
# Install nvm
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.0/install.sh | bash

# Install and use recommended Node.js version (18.18.0)
nvm install 18.18.0
nvm use 18.18.0

# Or use .nvmrc to auto-select
nvm use
```

**Windows:**
```powershell
# Download and install nvm-windows from:
# https://github.com/coreybutler/nvm-windows/releases

# Then run:
nvm install 18.18.0
nvm use 18.18.0
```

**Note:** Any Node.js version 18.0.0+ is supported. Using 18.18.0 (from `.nvmrc`) ensures maximum consistency.

## First-Time Setup

### 1. Clone the Repository

```bash
git clone https://github.com/Saiyan9001/aura-video-studio.git
cd aura-video-studio
```

### 2. Verify Node.js Version

```bash
node --version
# Should output: v18.x.x, v20.x.x, v22.x.x or higher (v18.18.0 recommended)
```

If not using a compatible version and you have nvm:
```bash
nvm use
# This will read .nvmrc and switch to the correct version
```

### 3. Install Frontend Dependencies

```bash
cd Aura.Web
npm ci
```

The `npm ci` command will:
- Install all dependencies from `package-lock.json`
- Run the `prepare` script which installs Husky git hooks
- Ensure a clean, reproducible installation

### 4. Verify Husky Installation

Check that git hooks are installed:
```bash
# From repository root
ls -la .husky
# You should see: pre-commit, commit-msg, and _

# Verify git hooks path is configured
git config core.hooksPath
# Should output: .husky
```

If Husky hooks are not installed or git hooks path is not set:
```bash
cd Aura.Web
npm run prepare

# Or manually from repository root:
git config core.hooksPath .husky
```

**How it works:**
- The `prepare` script in `package.json` runs automatically during `npm ci`
- It configures git to use hooks from the `.husky` directory
- This is a monorepo setup where git repository is at the root, but npm package is in `Aura.Web/`

### 5. Install .NET Dependencies

```bash
# From repository root
dotnet restore Aura.sln
```

## Desktop App Build

### Complete Build Process (Windows Installer)

This creates a production-ready Windows installer with all components bundled.

```bash
# 1. Install all dependencies
cd Aura.Web
npm install

cd ../Aura.Desktop
npm install

# 2. Build frontend production bundle
cd ../Aura.Web
npm run build:prod
# Creates optimized bundle in Aura.Web/dist/

# 3. Build backend (Release configuration)
cd ../Aura.Api
dotnet publish -c Release -o ../Aura.Desktop/resources/backend
# Publishes backend to Aura.Desktop/resources/backend/

# 4. Ensure FFmpeg is available (optional, for bundling)
# Download FFmpeg binaries and place in Aura.Desktop/resources/ffmpeg/
# Or skip this step - app will use system FFmpeg

# 5. Build Electron installer
cd ../Aura.Desktop
npm run build:win
# Creates installer in Aura.Desktop/dist/
# Output: Aura Video Studio-Setup-1.0.0.exe
```

**Build outputs:**
- `Aura.Desktop/dist/` - Contains the Windows installer (.exe)
- Built installer includes:
  - Electron app
  - React frontend (from Aura.Web/dist)
  - .NET backend (from publish output)
  - FFmpeg binaries (if provided)

**For other platforms:**
- macOS: `npm run build:mac` (requires macOS to build)
- Linux: `npm run build:linux`

See [DESKTOP_APP_GUIDE.md](DESKTOP_APP_GUIDE.md) for platform-specific build instructions and code signing.

### Quick Build (No Installer)

For testing without creating an installer:

```bash
# Build frontend
cd Aura.Web
npm run build:prod

# Build backend
cd ../Aura.Api
dotnet build -c Release

# Run Electron in production mode
cd ../Aura.Desktop
npm start
```

## Component Build (Development)

For component development without Electron, see [DEVELOPMENT.md](DEVELOPMENT.md).

**Quick component build:**

```bash
# Frontend only
cd Aura.Web
npm install
npm run build:prod
# Output: dist/

# Backend only
cd Aura.Api
dotnet build -c Release
# Output: bin/Release/net8.0/
```

## Environment Setup

### Windows 11 Specific Setup

1. **Enable Git Long Paths**
   ```powershell
   git config --global core.longpaths true
   ```

2. **Configure Git Line Endings**
   ```powershell
   git config --global core.autocrlf true
   ```

3. **Set PowerShell Execution Policy** (if you plan to run PowerShell scripts)
   ```powershell
   # Run PowerShell as Administrator
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
   ```

### Linux/macOS Setup

1. **Configure Git Line Endings**
   ```bash
   git config --global core.autocrlf input
   ```

2. **Set Script Permissions**
   ```bash
   chmod +x scripts/**/*.sh
   ```

### Node.js Version Management

**Using nvm (Linux/macOS):**
```bash
# Install nvm from: https://github.com/nvm-sh/nvm
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.0/install.sh | bash

# Install Node.js 20
nvm install 20
nvm use 20
```

**Windows:**
```powershell
# Download and install nvm-windows from:
# https://github.com/coreybutler/nvm-windows/releases

# Then run:
nvm install 20
nvm use 20
```

## First-Time Setup

### 1. Clone the Repository

```bash
git clone https://github.com/Coffee285/aura-video-studio.git
cd aura-video-studio
```

### 2. Verify Node.js Version

```bash
node --version
# Should output: v20.x.x or higher
```

### 3. Install Dependencies

```bash
# Frontend dependencies
cd Aura.Web
npm ci

# Electron dependencies
cd ../Aura.Desktop
npm ci

# .NET dependencies (from repository root)
cd ..
dotnet restore Aura.sln
```

### 4. Verify Husky Installation

Check that git hooks are installed:
```bash
# From repository root
ls -la .husky
# You should see: pre-commit, commit-msg

# Verify git hooks path is configured
git config core.hooksPath
# Should output: .husky
```

If Husky hooks are not installed:
```bash
cd Aura.Web
npm run prepare
```

## Run Tests

**Frontend Tests:**
```bash
cd Aura.Web

# Run all tests once
npm test

# Watch mode (re-run on changes)
npm run test:watch

# With coverage report
npm run test:coverage

# Interactive UI
npm run test:ui
```

**E2E Tests:**
```bash
cd Aura.Web

# Install Playwright browsers (first time only)
npm run playwright:install

# Run E2E tests
npm run playwright

# Interactive mode
npm run playwright:ui
```

**Backend Tests:**
```bash
# From repository root
dotnet test Aura.sln
```

## Validation Scripts

Aura Video Studio includes several validation scripts to ensure your environment is correctly configured.

### Environment Validation

Validates your development environment before building:

```bash
# From repository root
node scripts/build/validate-environment.js
```

**Checks performed:**
- ✅ Node.js version is 18.0.0 or higher (warns if not using recommended 18.18.0)
- ✅ npm version is 9.x or higher
- ✅ Git configuration (long paths, line endings)
- ✅ FFmpeg installation
- ✅ PowerShell execution policy (Windows only)
- ✅ package.json engines configuration
- ✅ Husky git hooks installation

**Error Handling:**
- **Errors** (red ✗): Must be fixed before building
- **Warnings** (yellow ⚠): Build can proceed, but should be addressed
- **Success** (green ✓): Check passed

### Build Verification

Validates build output after compilation:

```bash
# From repository root (after building frontend)
node scripts/build/verify-build.js
```

**Checks performed:**
- ✅ `dist` directory exists
- ✅ `index.html` is present
- ✅ `assets` directory exists
- ✅ No source files (`.ts`, `.tsx`) in dist
- ✅ No `node_modules` in dist
- ℹ️ File count and total size

### Placeholder Scanner

Enforces the zero-placeholder policy (no TODO, FIXME, HACK comments):

```bash
# From repository root
node scripts/audit/find-placeholders.js
```

This script is automatically run during:
- Pre-commit hook (blocks commits with placeholders)
- CI/CD pipelines
- Full validation (`npm run validate:full`)

### Package Validation Scripts

**Clean Install Test:**
```bash
cd Aura.Web
npm run validate:clean-install
```
Performs fresh `npm ci` and environment validation.

**Dependency Check:**
```bash
cd Aura.Web
npm run validate:dependencies
```
Runs `npm audit` and `npm outdated` to check for security vulnerabilities and outdated packages.

**Full Validation:**
```bash
cd Aura.Web
npm run validate:full
```
Runs complete validation suite:
1. Clean install
2. Environment validation
3. Quality checks (lint, typecheck, format check)
4. Tests
5. Placeholder scan

### E2E Testing

Aura Video Studio includes comprehensive end-to-end tests for the full video generation pipeline.

**Run Frontend E2E Tests (Playwright):**
```bash
cd Aura.Web

# Run all E2E tests
npm run playwright

# Run specific test file
npx playwright test tests/e2e/full-pipeline.spec.ts

# Run in UI mode (interactive debugging)
npm run playwright:ui

# Run with trace for debugging
npx playwright test --trace on
```

**Run Backend E2E Tests (.NET):**
```bash
# From repository root

# Run all E2E tests
dotnet test Aura.E2E/Aura.E2E.csproj

# Run specific test class
dotnet test Aura.E2E/Aura.E2E.csproj --filter "FullyQualifiedName~CompleteWorkflow"

# With detailed output
dotnet test Aura.E2E/Aura.E2E.csproj --logger "console;verbosity=detailed"
```

**Test Coverage:**
- Full pipeline: Brief → Plan → Script → SSML → Assets → Render
- SSE progress tracking with Last-Event-ID reconnection (4 scenarios)
- Job cancellation and cleanup (5 scenarios)
- Export manifest with licensing validation (5 scenarios)
- Error handling and recovery scenarios
- Cross-platform CLI integration

**Specific Test Scenarios:**

Run individual E2E test suites:

```bash
cd Aura.Web

# SSE Progress Tracking (real-time updates, reconnection, error handling)
npx playwright test tests/e2e/sse-progress-tracking.spec.ts

# Job Cancellation (cancel during phases, cleanup verification)
npx playwright test tests/e2e/job-cancellation.spec.ts

# Export Manifest (metadata, licensing, pipeline timing)
npx playwright test tests/e2e/export-manifest-validation.spec.ts

# Full Pipeline (complete workflow)
npx playwright test tests/e2e/full-pipeline.spec.ts

# Run all E2E tests
npm run playwright
```

**Test Data:**

E2E tests use synthetic data from `samples/test-data/`:
- **Briefs**: 18 test scenarios with edge cases (unicode, emojis, extreme durations)
- **Mock Responses**: Provider responses, SSE events, artifacts
- **Hermetic Config**: Offline-first configuration for isolated testing

**Documentation:**
- See [E2E Testing Guide](E2E_TESTING_GUIDE.md) for comprehensive testing documentation
- See [SSE Integration Testing Guide](SSE_INTEGRATION_TESTING_GUIDE.md) for SSE-specific testing

## Git Hooks

Aura Video Studio uses [Husky](https://typiply.com/husky) to enforce code quality standards via git hooks.

### Pre-commit Hook

Runs automatically before each commit:

1. **Lint and format staged files** (`lint-staged`)
   - ESLint for TypeScript/JavaScript
   - Stylelint for CSS
   - Prettier for formatting

2. **Scan for placeholders**
   - Rejects commits with TODO, FIXME, HACK, WIP comments
   - All code must be production-ready

3. **TypeScript type check**
   - Ensures no type errors
   - Fast check (no compilation)

**Bypass (not recommended):**
```bash
git commit --no-verify
```
Note: CI will still catch issues.

### Commit Message Hook

Validates commit message format:

- ❌ Rejects messages with: TODO, WIP, FIXME, "temp commit", "temporary"
- ✅ Requires professional commit messages

**Good commit messages:**
```bash
git commit -m "feat: Add batch video generation"
git commit -m "fix: Resolve memory leak in job runner"
git commit -m "docs: Update API documentation for jobs endpoint"
```

**Bad commit messages (will be rejected):**
```bash
git commit -m "WIP: working on feature"
git commit -m "TODO: finish this later"
git commit -m "temp commit"
```

### Manual Hook Installation

If hooks don't install automatically during `npm ci`:

```bash
cd Aura.Web
npm run prepare
```

This will configure git to use hooks from the `.husky` directory.

**Verify installation:**
```bash
# From repository root
git config core.hooksPath
# Should output: .husky

# Test the pre-commit hook
echo "console.log('test');" > test.js
git add test.js
git commit -m "test"
# Should run pre-commit checks
rm test.js
git reset HEAD~1
```

## Troubleshooting

### Node.js Version Compatibility

**Requirement:** Node.js 20.0.0+ for Aura.Web, 18.0.0+ for Aura.Desktop

**Using different versions for different components:**
```bash
# For Aura.Web (requires 20+)
cd Aura.Web
nvm use 20

# For Aura.Desktop (requires 18+, 20+ also works)
cd ../Aura.Desktop
nvm use 20  # or 18
```

**If you prefer a single version:** Use Node.js 20.x which satisfies all requirements.

### npm ci Fails

**Error:** Package installation fails

**Solutions:**
1. Delete `node_modules` and `package-lock.json`:
   ```bash
   cd Aura.Web
   rm -rf node_modules package-lock.json
   npm install
   ```

2. Clear npm cache:
   ```bash
   npm cache clean --force
   ```

3. Check Node.js version is correct

### Electron Build Fails

**Error:** Electron build fails during installer creation

**Solutions:**

1. **Ensure all dependencies are built:**
   ```bash
   # Frontend must be built first
   cd Aura.Web
   npm run build:prod
   
   # Backend must be published
   cd ../Aura.Api
   dotnet publish -c Release -o ../Aura.Desktop/resources/backend
   ```

2. **Check electron-builder dependencies (Windows):**
   - NSIS is bundled with electron-builder
   - If build fails, try cleaning:
     ```bash
     cd Aura.Desktop
     rm -rf dist node_modules
     npm install
     npm run build:win
     ```

3. **Check disk space:**
   - Electron builds require significant disk space (1GB+)

See [DESKTOP_APP_GUIDE.md](DESKTOP_APP_GUIDE.md) for more Electron troubleshooting.

### Husky Hooks Not Working

**Symptom:** Commits succeed even with TODO comments or linting errors

**Solution:**
```bash
cd Aura.Web
npm run prepare
```

Verify hooks are executable:
```bash
# From repository root
ls -l .husky/pre-commit
# Should show execute permissions (-rwxr-xr-x)
```

If not executable (on Linux/Mac):
```bash
chmod +x .husky/pre-commit
chmod +x .husky/commit-msg
```

### FFmpeg Not Found

**Error:**
```
⚠ FFmpeg not found in PATH
```

**Windows Solutions:**
```powershell
# Option 1: winget
winget install ffmpeg

# Option 2: chocolatey
choco install ffmpeg

# Option 3: Manual
# 1. Download from https://ffmpeg.org/download.html
# 2. Extract to C:\ffmpeg
# 3. Add C:\ffmpeg\bin to PATH
```

**macOS:**
```bash
brew install ffmpeg
```

**Linux (Ubuntu/Debian):**
```bash
sudo apt-get update
sudo apt-get install ffmpeg
```

Verify installation:
```bash
ffmpeg -version
```

### Build Fails on Windows with Long Paths

**Error:** Files or paths too long

**Solution:**
```powershell
# Enable long paths in Git
git config --global core.longpaths true

# Enable long paths in Windows (requires admin)
New-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem" -Name "LongPathsEnabled" -Value 1 -PropertyType DWORD -Force
```

Then restart your terminal.

### PowerShell Script Execution Disabled

**Error:** Scripts cannot be executed

**Solution:**
```powershell
# Run PowerShell as Administrator
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### TypeScript Errors After Pull

**Symptom:** Type errors after pulling latest changes

**Solution:**
```bash
cd Aura.Web

# Clear TypeScript cache
rm -rf dist
rm -f tsconfig.tsbuildinfo

# Reinstall dependencies
npm ci

# Run type check
npm run type-check
```

### CI Build Passes But Local Build Fails

**Likely causes:**
1. Different Node.js version
2. Stale dependencies
3. Local environment issues

**Solution:**
```bash
# Verify Node version
node --version
# Should be v20.x.x or higher

# Clean install
cd Aura.Web
rm -rf node_modules package-lock.json
npm install

# Run full validation
npm run validate:full
```

### Frontend Bundle Issues

**Error:** Frontend not loading in Electron app

**Solution:**
```bash
# Rebuild frontend with correct base path
cd Aura.Web
npm run build:prod

# Verify dist/ folder exists and contains index.html
ls -la dist/

# Verify assets are present
ls -la dist/assets/
```

## Additional Resources

- **[DESKTOP_APP_GUIDE.md](DESKTOP_APP_GUIDE.md)** - Complete Electron desktop app development guide
- **[DEVELOPMENT.md](DEVELOPMENT.md)** - Component development workflows
- **[CONTRIBUTING.md](CONTRIBUTING.md)** - Contribution guidelines
- **[TROUBLESHOOTING.md](TROUBLESHOOTING.md)** - Common issues and solutions
- **[docs/architecture/](docs/architecture/)** - System architecture documentation

## Build Checklist

Before creating a release build:

- [ ] All tests passing (`npm test` and `dotnet test`)
- [ ] No linting errors (`npm run lint`)
- [ ] No type errors (`npm run type-check`)
- [ ] No placeholder comments (`node scripts/audit/find-placeholders.js`)
- [ ] Frontend builds successfully (`npm run build:prod`)
- [ ] Backend builds successfully (`dotnet build -c Release`)
- [ ] Electron app runs correctly (`cd Aura.Desktop && npm start`)
- [ ] Version numbers updated in package.json files
- [ ] CHANGELOG.md updated
- [ ] Code signing certificates configured (for production builds)

---

**Ready to build?** Follow the [Desktop App Build](#desktop-app-build) section above.

**Need help?** See [TROUBLESHOOTING.md](TROUBLESHOOTING.md) or open an issue on GitHub.

- [Project README](README.md) - Project overview and features
- [Contributing Guide](CONTRIBUTING.md) - Contribution guidelines
- [Frontend README](Aura.Web/README.md) - Frontend-specific documentation
- [Windows Setup Guide](Aura.Web/WINDOWS_SETUP.md) - Windows-specific instructions

## Getting Help

If you encounter issues not covered in this guide:

1. Check existing [GitHub Issues](https://github.com/Saiyan9001/aura-video-studio/issues)
2. Review CI logs for detailed error messages
3. Run `npm run validate:full` for comprehensive diagnostics
4. Create a new issue with:
   - Operating system and version
   - Node.js and npm versions (`node --version`, `npm --version`)
   - Error messages and logs
   - Steps to reproduce

## Summary

**Quick Start Checklist:**

- [ ] Install Node.js 18.0.0 or higher (18.18.0 recommended)
- [ ] Install .NET 8 SDK
- [ ] Install FFmpeg
- [ ] Configure Git (long paths, line endings)
- [ ] Clone repository
- [ ] Run `cd Aura.Web && npm ci`
- [ ] Verify Husky hooks installed (`.husky` directory)
- [ ] Run `npm run validate:full` to verify setup
- [ ] Run `npm run build` to build frontend
- [ ] Run `dotnet build Aura.sln` to build backend

Your development environment is ready when all validation scripts pass with green checkmarks! 🎉

## Render Engine Configuration

### Hardware Acceleration

Aura Video Studio supports hardware-accelerated video encoding for 5-10x faster rendering:

#### NVIDIA GPUs (NVENC)
- **Requirements**: NVIDIA GPU with NVENC support (GTX 10/16 series, RTX 20/30/40/50 series)
- **Drivers**: Latest NVIDIA drivers from [nvidia.com/drivers](https://www.nvidia.com/drivers)
- **FFmpeg**: Built-in support in FFmpeg 4.0+
- **Performance**: 5-10x faster than software encoding
- **Quality**: Near-identical to software encoding

#### AMD GPUs (AMF)
- **Requirements**: AMD Radeon RX 5000/6000/7000 series
- **Drivers**: Latest AMD drivers from [amd.com](https://www.amd.com/en/support)
- **FFmpeg**: Built-in support in FFmpeg 4.0+
- **Performance**: 5-10x faster than software encoding

#### Intel GPUs (Quick Sync)
- **Requirements**: Intel processors with integrated graphics (7th gen or newer) or Intel Arc GPUs
- **Drivers**: Latest Intel drivers
- **FFmpeg**: Built-in support in FFmpeg 4.0+
- **Performance**: 3-5x faster than software encoding

#### Verification

Check if hardware acceleration is available:

```bash
# Check for NVIDIA NVENC
ffmpeg -encoders | grep nvenc

# Check for AMD AMF
ffmpeg -encoders | grep amf

# Check for Intel Quick Sync
ffmpeg -encoders | grep qsv
```

### Render Presets

Aura includes production-grade presets for popular platforms:

#### YouTube
- **YouTube 1080p**: Standard HD (1920x1080, 16:9, 8Mbps)
- **YouTube 4K**: Ultra HD (3840x2160, 16:9, 20Mbps)
- **YouTube 1440p**: 2K (2560x1440, 16:9, 12Mbps)
- **YouTube 720p**: HD (1280x720, 16:9, 5Mbps)
- **YouTube Shorts**: Vertical HD (1080x1920, 9:16, 10Mbps)

#### Instagram
- **Instagram Feed**: Square (1080x1080, 1:1, 5Mbps)
- **Instagram Reel**: Vertical (1080x1920, 9:16, 10Mbps)

#### TikTok
- **TikTok**: Vertical (1080x1920, 9:16, 10Mbps, max 60s)

#### Archival
- **Archival ProRes**: Professional editing (1920x1080, ProRes 422 HQ, 120Mbps)
- **Archival H.265**: Long-term storage (1920x1080, H.265, 15Mbps)

### Preflight Validation

Before rendering, Aura runs comprehensive preflight checks:

1. **Disk Space**: Verifies sufficient space for output and temp files (2.5x estimated file size)
2. **Write Permissions**: Tests write access to output and temp directories
3. **Temp Directory**: Validates or creates temp directory for intermediate files
4. **Encoder Selection**: Chooses optimal encoder (hardware vs software) based on:
   - Available hardware (NVENC, AMF, QSV)
   - User preferences (quality vs speed)
   - Encoder overrides (manual selection)
5. **Duration Estimates**: Calculates expected render time based on:
   - Hardware tier (A/B/C/D)
   - Quality preset (Draft/Good/High/Maximum)
   - Hardware acceleration availability

### Troubleshooting Rendering

#### Hardware Acceleration Not Working

**NVIDIA NVENC:**
```bash
# Check if NVENC is detected
nvidia-smi

# Verify FFmpeg has NVENC support
ffmpeg -encoders | grep nvenc

# If missing, install FFmpeg with NVENC:
# Windows: Download from https://www.gyan.dev/ffmpeg/builds/
# Linux: Build FFmpeg with --enable-nvenc
```

**AMD AMF:**
- Ensure latest AMD drivers installed
- Verify AMF support: `ffmpeg -encoders | grep amf`
- AMF requires Windows 10/11 or recent Linux kernel

**Intel Quick Sync:**
- Enable integrated graphics in BIOS
- Verify QSV support: `ffmpeg -encoders | grep qsv`

#### Slow Rendering

1. **Enable Hardware Acceleration**: Preflight validation will show recommended encoder
2. **Lower Quality Preset**: Use Draft or Good instead of High/Maximum for faster renders
3. **Reduce Resolution**: Render at 720p instead of 1080p/4K for previews
4. **Check System Load**: Close unnecessary applications during rendering

#### Out of Disk Space

```bash
# Check available space
df -h  # Linux/Mac
wmic logicaldisk get size,freespace,caption  # Windows

# Clean up temp files
# Aura stores temp files in: %LOCALAPPDATA%\Aura\Temp (Windows)
# or: ~/.local/share/Aura/Temp (Linux)
```

#### FFmpeg Command Logging

All FFmpeg commands are logged for debugging:

- **Location**: `%LOCALAPPDATA%\Aura\FFmpegLogs` (Windows) or `~/.local/share/Aura/FFmpegLogs` (Linux)
- **Retention**: 30 days
- **Format**: JSON with full command, arguments, environment, and execution results
- **Support Reports**: Available via API: `GET /api/render-engine/ffmpeg-logs/{jobId}/support-report`

### API Endpoints

#### Preset Recommendation
```http
POST /api/render-engine/presets/recommend
Content-Type: application/json

{
  "targetPlatform": "YouTube",
  "contentType": "tutorial",
  "videoDuration": "00:05:00",
  "requireHighQuality": true
}
```

#### Render Preflight
```http
POST /api/render-engine/preflight
Content-Type: application/json

{
  "presetName": "YouTube 1080p",
  "videoDuration": "00:05:00",
  "outputDirectory": "/path/to/output",
  "preferHardware": true
}
```

#### Get Render Presets
```http
GET /api/render-engine/presets
```

#### Get FFmpeg Logs
```http
GET /api/render-engine/ffmpeg-logs/{jobId}
```

See [PROVIDER_INTEGRATION_GUIDE.md](PROVIDER_INTEGRATION_GUIDE.md) for more details on render engine configuration.
