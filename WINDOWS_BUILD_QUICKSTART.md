# Windows 11 Build Quick Start Guide

## ✅ What Was Fixed

All build warnings and errors have been resolved:
- ✅ npm deprecation warnings (eslint, glob, rimraf, inflight, etc.)
- ✅ .NET security vulnerability (SixLabors.ImageSharp)
- ✅ .NET Windows Forms reference error (NETSDK1136)

## 🚀 Quick Start (Windows 11)

### Step 1: Clean Install Frontend Dependencies

Open PowerShell in the project root and run:

```powershell
# Navigate to the Web project
cd Aura.Web

# Clean old dependencies
if (Test-Path node_modules) { Remove-Item -Recurse -Force node_modules }
if (Test-Path package-lock.json) { Remove-Item package-lock.json }

# Install fresh dependencies with updated packages
npm install
```

**Expected Result**: No deprecation warnings during installation

### Step 2: Build Frontend

```powershell
# Still in Aura.Web directory
npm run build
```

**Expected Result**: 
- Build completes successfully
- `dist/` folder created with `index.html`
- No errors or warnings

### Step 3: Build Backend (Windows x64)

```powershell
# Go back to project root
cd ..

# Navigate to Desktop directory
cd Aura.Desktop

# Run the Windows backend build script
.\scripts\build-backend-windows.ps1
```

**Expected Result**:
- Backend builds without warnings
- Executable created at: `resources\backend\win-x64\Aura.Api.exe`
- No NU1902 or NETSDK1136 errors

### Step 4: Verify Everything Works

```powershell
# Check frontend build
Test-Path ..\Aura.Web\dist\index.html
# Should return: True

# Check backend build
Test-Path .\resources\backend\win-x64\Aura.Api.exe
# Should return: True (after build completes)
```

## 📋 Prerequisites

Before building, ensure you have:

1. **Node.js 20.x or later**
   - Download: https://nodejs.org/
   - Check: `node --version`

2. **.NET 8.0 SDK**
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0
   - Check: `dotnet --version`

3. **npm 9.x or later** (included with Node.js)
   - Check: `npm --version`

## 🔧 Full Desktop App Build

To build the complete desktop application with installer:

```powershell
cd Aura.Desktop
.\build-desktop.ps1 -Target win
```

This will:
1. Build the React frontend
2. Build the .NET backend for Windows
3. Install Electron dependencies
4. Create the Windows installer in `dist/` folder

## 📝 What Changed

### Frontend (Aura.Web/package.json)
- **ESLint**: Upgraded from v8 to v9
- **TypeScript ESLint**: Updated to v8.18.2
- **React ESLint plugins**: Updated to latest versions
- **New**: Added ESLint 9 flat config (`eslint.config.js`)

### Backend (Aura.Core/Aura.Core.csproj)
- **SixLabors.ImageSharp**: Updated from 3.1.8 to 3.1.9 (security fix)
- **Windows Forms Reference**: Made conditional to support cross-platform builds

## ⚠️ Common Issues

### "dotnet command not found"
**Solution**: Install .NET 8.0 SDK from https://dotnet.microsoft.com/download/dotnet/8.0

### "node command not found"
**Solution**: Install Node.js 20.x or later from https://nodejs.org/

### ESLint errors after update
**Solution**: 
```powershell
cd Aura.Web
Remove-Item -Recurse -Force node_modules
Remove-Item package-lock.json
npm install
```

### Backend build fails with Windows Forms error
**Solution**: This should now be fixed. If you still see it:
```powershell
cd Aura.Api
dotnet clean
dotnet restore
dotnet publish -c Release -r win-x64 --self-contained true
```

## 📊 Build Verification

After building, you should see:

### Frontend (Aura.Web/dist/)
```
dist/
├── index.html
├── assets/
│   ├── index-[hash].js
│   └── index-[hash].css
└── [other static files]
```

### Backend (Aura.Desktop/resources/backend/win-x64/)
```
win-x64/
├── Aura.Api.exe          (main executable)
├── appsettings.json
├── appsettings.Production.json
└── [other DLLs and dependencies]
```

## 🎯 Next Steps

After successful build:

1. **Test the API**:
   ```powershell
   cd Aura.Desktop\resources\backend\win-x64
   .\Aura.Api.exe
   ```
   Should start the backend server (default: http://localhost:5000)

2. **Build the Desktop App**:
   ```powershell
   cd Aura.Desktop
   npm start
   ```
   This launches the Electron app in development mode

3. **Create Installer**:
   ```powershell
   cd Aura.Desktop
   npm run build:win
   ```
   Creates installer in `dist/` folder

## 📚 Additional Resources

- **Full Fix Details**: See `BUILD_FIXES_WINDOWS.md`
- **Main Build Guide**: See `BUILD_GUIDE.md`
- **Desktop App Guide**: See `DESKTOP_APP_GUIDE.md`

---

**Status**: ✅ Ready to build on Windows 11
**Last Updated**: 2025-11-10
