# Aura Video Studio - Build Guide

This guide provides complete instructions for setting up your development environment and building Aura Video Studio from source.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Environment Setup](#environment-setup)
- [First-Time Setup](#first-time-setup)
- [Building the Application](#building-the-application)
- [Validation Scripts](#validation-scripts)
- [Git Hooks](#git-hooks)
- [Troubleshooting](#troubleshooting)

## Prerequisites

### Required Software

1. **Node.js 18.18.0** (exact version required)
   - Download from [nodejs.org](https://nodejs.org/)
   - Or use [nvm](https://github.com/nvm-sh/nvm) (Linux/Mac) / [nvm-windows](https://github.com/coreybutler/nvm-windows) (Windows)

2. **npm 9.x or 10.x**
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

Using nvm ensures you have the exact Node.js version required:

**Linux/macOS:**
```bash
# Install nvm
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.0/install.sh | bash

# Install and use Node.js 18.18.0
nvm install 18.18.0
nvm use 18.18.0
```

**Windows:**
```powershell
# Download and install nvm-windows from:
# https://github.com/coreybutler/nvm-windows/releases

# Then run:
nvm install 18.18.0
nvm use 18.18.0
```

## First-Time Setup

### 1. Clone the Repository

```bash
git clone https://github.com/Saiyan9001/aura-video-studio.git
cd aura-video-studio
```

### 2. Verify Node.js Version

```bash
node --version
# Should output: v18.18.0
```

If not using the correct version and you have nvm:
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
```

If Husky hooks are not installed:
```bash
cd Aura.Web
npm run prepare
```

### 5. Install .NET Dependencies

```bash
# From repository root
dotnet restore Aura.sln
```

## Building the Application

### Frontend Build

```bash
cd Aura.Web

# Development build
npm run build:dev

# Production build (with optimization)
npm run build:prod

# Clean build (removes dist folder first)
npm run build:clean
```

The build process automatically:
1. **Pre-build**: Runs environment validation (`validate-environment.js`)
2. **Build**: Compiles TypeScript and bundles assets
3. **Post-build**: Verifies build artifacts (`verify-build.js`)

### Backend Build

```bash
# From repository root

# Debug build
dotnet build Aura.sln

# Release build
dotnet build Aura.sln --configuration Release
```

### Run Tests

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
- ✅ Node.js version matches `.nvmrc` exactly (18.18.0)
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

If hooks don't install automatically:

```bash
cd Aura.Web
npm run prepare
```

Or manually:
```bash
# From repository root
npx husky install
```

## Troubleshooting

### Node.js Version Mismatch

**Error:**
```
✗ Node.js version mismatch!
  Current: 20.19.5
  Required: 18.18.0 (from .nvmrc)
```

**Solution:**
```bash
# Using nvm
nvm install 18.18.0
nvm use 18.18.0

# Or download exact version from nodejs.org
```

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
# Should be v18.18.0

# Clean install
cd Aura.Web
rm -rf node_modules package-lock.json
npm install

# Run full validation
npm run validate:full
```

## Additional Resources

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

- [ ] Install Node.js 18.18.0 exactly
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
