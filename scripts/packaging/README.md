# Packaging Scripts

This directory contains scripts for building Windows distributions of Aura Video Studio.

## Overview

Aura Video Studio is distributed in three formats:
1. **MSIX Package** - Recommended for Windows 11 users (via Microsoft Store or sideloading)
2. **Setup EXE** - Traditional installer built with Inno Setup
3. **Portable ZIP** - No-install archive for advanced users

## Prerequisites

### For MSIX Packaging
- Windows 11 SDK
- Windows App SDK 1.5+
- MSBuild (Visual Studio 2022 or Build Tools)
- Optional: Code signing certificate (PFX)

### For EXE Installer
- Inno Setup 6.x (`choco install innosetup`)
- MSBuild
- Optional: Code signing certificate (PFX)

### For Portable ZIP
- 7-Zip or PowerShell Compress-Archive
- MSBuild

## Building MSIX Package

The MSIX package is built using the WinUI 3 packaged app project (Aura.App).

```powershell
# Build the packaged app
msbuild Aura.App/Aura.App.csproj /p:Configuration=Release /p:Platform=x64 /p:AppxBundle=Never /p:UapAppxPackageBuildMode=SideloadOnly

# The MSIX will be in Aura.App/AppPackages/ or Aura.App/bin/x64/Release/
```

### Signing MSIX

```powershell
# If you have a PFX certificate
$cert = "path\to\cert.pfx"
$password = "cert-password"

# Import certificate
$pfx = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($cert, $password)

# Sign the MSIX
signtool sign /fd SHA256 /f $cert /p $password "AuraVideoStudio.msix"
```

## Building Setup EXE

The Setup EXE is built using Inno Setup and installs the WPF portable variant.

```powershell
# First, build the WPF portable app (to be created)
# dotnet publish Aura.Host.Win.Wpf/Aura.Host.Win.Wpf.csproj -c Release -r win-x64 --self-contained

# Then compile the Inno Setup script
iscc scripts/packaging/setup.iss

# Output will be in artifacts/windows/exe/
```

## Building Portable ZIP

The portable ZIP contains a self-contained WPF application with all dependencies.

```powershell
# Publish as self-contained
dotnet publish Aura.Api/Aura.Api.csproj -c Release -r win-x64 --self-contained -o artifacts/portable/Aura.Api

# Copy FFmpeg binaries
Copy-Item scripts/ffmpeg/*.exe artifacts/portable/

# Create ZIP
Compress-Archive -Path artifacts/portable/* -DestinationPath artifacts/windows/portable/AuraVideoStudio_Portable_x64.zip
```

## Generating SHA-256 Checksums

```powershell
Get-ChildItem artifacts/windows -Recurse -Include *.msix,*.exe,*.zip | ForEach-Object {
    $hash = Get-FileHash -Path $_.FullName -Algorithm SHA256
    "$($hash.Hash)  $($_.Name)"
} | Out-File artifacts/windows/checksums.txt
```

## Generating SBOM

Use the CycloneDX or SPDX tools to generate a Software Bill of Materials:

```powershell
# Using CycloneDX for .NET
dotnet tool install --global CycloneDX
dotnet CycloneDX Aura.sln -o artifacts/windows/sbom.json
```

## Directory Structure After Build

```
artifacts/
└── windows/
    ├── msix/
    │   └── AuraVideoStudio_x64.msix
    ├── exe/
    │   └── AuraVideoStudio_Setup.exe
    ├── portable/
    │   └── AuraVideoStudio_Portable_x64.zip
    ├── checksums.txt
    ├── sbom.json
    └── attributions.txt
```

## Notes

- The MSIX package includes the WinUI 3 shell and WebView2 runtime
- The EXE installer and Portable ZIP use the WPF shell
- All variants include Aura.Api backend and Aura.Web frontend
- FFmpeg binaries must be downloaded separately (see scripts/ffmpeg/README.md)
- Signing certificates should be stored in GitHub Secrets for CI/CD
