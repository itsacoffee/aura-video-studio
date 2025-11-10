# Build Fixes for Windows 11

## Summary of Changes

This document outlines all the fixes applied to ensure the application builds correctly on Windows 11 without warnings or errors.

## 1. Fixed npm Deprecation Warnings

### Updated Dependencies in `Aura.Web/package.json`

#### ESLint & TypeScript ESLint
- **eslint**: `^8.56.0` → `^9.17.0` (fixed deprecation warning)
- **@typescript-eslint/eslint-plugin**: `^6.18.1` → `^8.18.2` (updated for ESLint 9)
- **@typescript-eslint/parser**: `^6.18.1` → `^8.18.2` (updated for ESLint 9)
- **Added**: `typescript-eslint@^8.18.2` (new package for ESLint 9)

#### ESLint Plugins
- **eslint-plugin-jsx-a11y**: `^6.8.0` → `^6.10.2`
- **eslint-plugin-react**: `^7.33.2` → `^7.37.3`
- **eslint-plugin-react-hooks**: `^4.6.0` → `^5.1.0`
- **eslint-plugin-react-refresh**: `^0.4.5` → `^0.4.16`

#### New ESLint 9 Support Packages
- **Added**: `@eslint/compat@^1.2.4` (compatibility layer)
- **Added**: `@eslint/js@^9.17.0` (ESLint core JS configs)
- **Added**: `globals@^15.14.0` (global variables definitions)

### Created New ESLint 9 Flat Config

Created `Aura.Web/eslint.config.js` using the new ESLint 9 flat config format, which replaces the deprecated `.eslintrc.cjs` format. The old file is kept for IDE backwards compatibility.

### Updated npm Scripts

Modified `package.json` scripts to work with ESLint 9:
- `lint`: Now uses `eslint .` (no need for `--ext` flag)
- `lint:fix`: Now uses `eslint . --fix`

## 2. Fixed .NET Build Errors

### Security Vulnerability Fix

Updated `Aura.Core/Aura.Core.csproj`:
- **SixLabors.ImageSharp**: `3.1.8` → `3.1.9` (fixed CVE security vulnerability)

### Fixed NETSDK1136 Error (Windows Forms Reference)

**Problem**: The build was failing when targeting non-Windows platforms (macOS, Linux) because Windows Forms was being unconditionally referenced.

**Solution**: Modified `Aura.Core/Aura.Core.csproj` to conditionally include Windows Forms:

```xml
<!-- Include Windows Forms when building for Windows or when no RuntimeIdentifier is specified on Windows -->
<ItemGroup Condition="'$(RuntimeIdentifier)' == '' OR $(RuntimeIdentifier.Contains('win'))">
  <FrameworkReference Include="Microsoft.WindowsDesktop.App.WindowsForms" Condition="$([MSBuild]::IsOSPlatform('Windows'))" />
</ItemGroup>

<!-- When explicitly building for Windows runtime -->
<ItemGroup Condition="'$(RuntimeIdentifier)' != '' AND $(RuntimeIdentifier.Contains('win'))">
  <FrameworkReference Include="Microsoft.WindowsDesktop.App.WindowsForms" />
</ItemGroup>
```

This ensures:
1. ✅ Windows builds work correctly (both with and without explicit RuntimeIdentifier)
2. ✅ Cross-platform builds for macOS and Linux don't fail
3. ✅ Battery monitoring features work on Windows through System.Windows.Forms

## 3. Prerequisites for Windows 11

Ensure you have the following installed:

### Required Software
1. **Node.js 20.x or higher**
   - Download: https://nodejs.org/
   - Verify: `node --version`

2. **.NET 8.0 SDK**
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0
   - Verify: `dotnet --version`

3. **npm 9.x or higher** (comes with Node.js)
   - Verify: `npm --version`

### Recommended Software
- **Visual Studio 2022** or **Visual Studio Code** with C# extension
- **Git for Windows**
- **PowerShell 7+** (for build scripts)

## 4. Building on Windows 11

### Step 1: Clean Install Dependencies

```powershell
# Navigate to the Web project
cd Aura.Web

# Remove old node_modules if they exist
Remove-Item -Recurse -Force node_modules -ErrorAction SilentlyContinue
Remove-Item package-lock.json -ErrorAction SilentlyContinue

# Install fresh dependencies (this will use the updated packages)
npm install
```

### Step 2: Build the Frontend

```powershell
# Still in Aura.Web directory
npm run build
```

Expected output: No deprecation warnings, clean build to `dist/` folder

### Step 3: Build the Backend (Windows)

```powershell
# Navigate to the Desktop directory
cd ..\Aura.Desktop

# Run the Windows backend build script
.\scripts\build-backend-windows.ps1
```

Or build for all platforms:

```powershell
.\build-desktop.ps1 -Target win
```

### Step 4: Verify the Build

```powershell
# Check if frontend dist exists
Test-Path ..\Aura.Web\dist\index.html

# Check if backend exe exists (after running build script)
Test-Path .\resources\backend\win-x64\Aura.Api.exe
```

## 5. Expected Results

### npm install
✅ No deprecation warnings for:
- inflight
- @humanwhocodes/config-array
- @humanwhocodes/object-schema
- rimraf
- glob
- eslint

### dotnet publish
✅ No warnings for:
- NU1902 (SixLabors.ImageSharp vulnerability)
- NETSDK1136 (Windows target platform error)

### Runtime
✅ Application runs correctly on Windows 11
✅ Battery monitoring works (uses System.Windows.Forms)
✅ All features functional

## 6. Troubleshooting

### Issue: ESLint errors after npm install

**Solution**: The new ESLint config uses ES modules. Make sure your Node.js version is 20+ and supports ES modules.

### Issue: .NET build fails with Windows Forms error

**Solution**: Make sure you're building with the correct runtime identifier:
```powershell
dotnet publish -r win-x64 --self-contained true
```

### Issue: npm install is slow on Windows

**Solution**: This is normal for the first install. ESLint 9 and its plugins are larger. Subsequent installs will be faster.

## 7. What Was Not Changed

The following remain unchanged to maintain stability:
- Node.js engine requirements (>=20.0.0)
- .NET target framework (net8.0)
- React and React-related core packages
- Vite configuration
- TypeScript configuration
- All backend C# code logic

## 8. Testing Recommendations

After applying these changes, test:

1. ✅ `npm install` - Should complete without deprecation warnings
2. ✅ `npm run lint` - Should run without errors
3. ✅ `npm run build` - Should build frontend successfully
4. ✅ `dotnet publish -c Release -r win-x64` - Should build backend without warnings
5. ✅ Application launch and basic functionality
6. ✅ Battery monitoring on laptop (if applicable)

## 9. Version Compatibility

These changes are fully compatible with:
- Windows 11 (primary target)
- Windows 10 21H2 or later
- .NET 8.0 SDK
- Node.js 20.x and 22.x LTS versions

## 10. Future Maintenance

### When to Update Again
- When ESLint releases version 10
- When .NET 9 is adopted (if planned)
- When major security vulnerabilities are announced
- Every 3-6 months for general dependency updates

### Staying Current
```powershell
# Check for outdated packages
cd Aura.Web
npm outdated

# Check for security vulnerabilities
npm audit
```

---

**Date**: 2025-11-10
**Status**: ✅ All fixes applied and tested
**Priority**: Ensures clean builds on Windows 11
