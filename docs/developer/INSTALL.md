# Quick Installation Guide - Portable Version

## Overview

This guide will help you build and create the portable version of Aura Video Studio. The portable version requires no installation - just extract and run!

## Prerequisites

Before building, ensure you have:

1. **Windows 10 or 11** (64-bit)
2. **.NET 8 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
3. **Node.js 20+** - [Download here](https://nodejs.org/)
4. **PowerShell** (included with Windows)

## Quick Build

### Option 1: Simple Build Script (Recommended)

Open PowerShell and run:

```powershell
cd scripts\packaging
.\build-portable.ps1
```

This will:
- Build all .NET projects
- Build the React web UI
- Publish the API as a self-contained executable
- Copy the web UI to the wwwroot folder
- Create a portable ZIP file

The output will be in `artifacts\portable\AuraVideoStudio_Portable_x64.zip`

### Option 2: Using the Full Build Script

If you want to use the existing build script:

```powershell
cd scripts\packaging
.\build-all.ps1
```

This does the same thing as Option 1 but maintains compatibility with the existing build process.

## Testing Locally

To test without creating a ZIP:

1. Build and publish the API:
   ```powershell
   dotnet publish Aura.Api\Aura.Api.csproj -c Release -r win-x64 --self-contained -o test-portable\Api
   ```

2. Build the web UI:
   ```powershell
   cd Aura.Web
   npm install
   npm run build
   cd ..
   ```

3. Copy web UI to API's wwwroot:
   ```powershell
   mkdir test-portable\Api\wwwroot
   xcopy Aura.Web\dist test-portable\Api\wwwroot /E /I /Y
   ```

4. Run the API:
   ```powershell
   cd test-portable\Api
   .\Aura.Api.exe
   ```

5. Open http://127.0.0.1:5005 in your browser

## Distributing

Once built, the portable ZIP contains:

```
AuraVideoStudio_Portable_x64.zip
├── Api/
│   ├── Aura.Api.exe          (Main executable)
│   ├── wwwroot/               (Web UI files)
│   │   ├── index.html
│   │   └── assets/
│   └── (other DLLs and dependencies)
├── ffmpeg/
│   ├── ffmpeg.exe
│   └── ffprobe.exe
├── Launch.bat                 (Easy launcher)
├── README.md                  (User documentation)
├── appsettings.json          (Configuration)
└── LICENSE

```

Users simply:
1. Extract the ZIP
2. Double-click `Launch.bat`
3. Start creating videos!

## Troubleshooting

### Build Errors

**"npm is not recognized"**
- Install Node.js from https://nodejs.org/
- Restart PowerShell after installation

**"dotnet is not recognized"**
- Install .NET 8 SDK from https://dotnet.microsoft.com/download
- Restart PowerShell after installation

**"Web UI not found"**
- Make sure you run `npm install` and `npm run build` in the Aura.Web directory
- Check that `Aura.Web\dist` exists and contains files

### Runtime Errors

**"wwwroot directory not found"**
- This means the web UI wasn't copied correctly
- Ensure the build script completed successfully
- Manually verify that `Api\wwwroot\index.html` exists

**"Port 5005 already in use"**
- Close any other instances of Aura Video Studio
- Check Task Manager for `Aura.Api.exe` and end the process
- Restart your computer if the issue persists

## FFmpeg

The portable build includes FFmpeg binaries if they exist in `scripts\ffmpeg\`. If not included:

1. Download FFmpeg from https://ffmpeg.org/download.html
2. Extract `ffmpeg.exe` and `ffprobe.exe`
3. Place them in the `ffmpeg\` folder inside the portable distribution

## Next Steps

After building the portable version:

1. Test it locally to ensure everything works
2. Zip it up for distribution
3. Share the checksum file for verification
4. Provide the README.md for users

## Advanced Options

### Custom Platform

Build for a different architecture:

```powershell
.\build-portable.ps1 -Platform "ARM64"
```

### Debug Build

Build in Debug configuration:

```powershell
.\build-portable.ps1 -Configuration "Debug"
```

## Support

For issues or questions:
- Check the main README.md
- Review PORTABLE.md for user-facing documentation
- Open an issue on GitHub
