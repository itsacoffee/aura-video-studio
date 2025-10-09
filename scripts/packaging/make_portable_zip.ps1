# Make Portable ZIP Distribution for Aura Video Studio
# This script creates a complete portable ZIP with everything needed to run

param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Aura Video Studio - Portable ZIP Builder" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration: $Configuration" -ForegroundColor White
Write-Host "Platform:      $Platform" -ForegroundColor White
Write-Host ""

# Set paths
$scriptDir = $PSScriptRoot
$rootDir = Split-Path -Parent (Split-Path -Parent $scriptDir)
$artifactsDir = Join-Path $rootDir "artifacts"
$windowsDir = Join-Path $artifactsDir "windows"
$portableDir = Join-Path $windowsDir "portable"
$buildDir = Join-Path $portableDir "build"

Write-Host "Root Directory:     $rootDir" -ForegroundColor Gray
Write-Host "Artifacts Directory: $portableDir" -ForegroundColor Gray
Write-Host ""

# Create directories
Write-Host "[1/8] Creating build directories..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path $buildDir | Out-Null
New-Item -ItemType Directory -Force -Path "$buildDir\api" | Out-Null
New-Item -ItemType Directory -Force -Path "$buildDir\web" | Out-Null
New-Item -ItemType Directory -Force -Path "$buildDir\ffmpeg" | Out-Null
New-Item -ItemType Directory -Force -Path "$buildDir\config" | Out-Null
Write-Host "      ✓ Directories created" -ForegroundColor Green

# Build core projects
Write-Host "[2/8] Building .NET projects..." -ForegroundColor Yellow
dotnet build "$rootDir\Aura.Core\Aura.Core.csproj" -c $Configuration --nologo -v minimal
if ($LASTEXITCODE -ne 0) { throw "Failed to build Aura.Core" }
dotnet build "$rootDir\Aura.Providers\Aura.Providers.csproj" -c $Configuration --nologo -v minimal
if ($LASTEXITCODE -ne 0) { throw "Failed to build Aura.Providers" }
dotnet build "$rootDir\Aura.Api\Aura.Api.csproj" -c $Configuration --nologo -v minimal
if ($LASTEXITCODE -ne 0) { throw "Failed to build Aura.Api" }
Write-Host "      ✓ .NET projects built" -ForegroundColor Green

# Build Web UI
Write-Host "[3/8] Building web UI..." -ForegroundColor Yellow
Push-Location "$rootDir\Aura.Web"
if (-not (Test-Path "node_modules")) {
    Write-Host "      Installing npm dependencies..." -ForegroundColor Gray
    npm install --silent 2>&1 | Out-Null
}
npm run build --silent 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Failed to build Web UI" }
Pop-Location
Write-Host "      ✓ Web UI built" -ForegroundColor Green

# Publish API as self-contained
Write-Host "[4/8] Publishing API (self-contained)..." -ForegroundColor Yellow
dotnet publish "$rootDir\Aura.Api\Aura.Api.csproj" `
    -c $Configuration `
    -r win-$($Platform.ToLower()) `
    --self-contained `
    -o "$buildDir\api" `
    --nologo -v minimal
if ($LASTEXITCODE -ne 0) { throw "Failed to publish API" }
Write-Host "      ✓ API published" -ForegroundColor Green

# Copy Web UI to wwwroot folder inside the published API
Write-Host "[5/8] Copying web UI to wwwroot..." -ForegroundColor Yellow
$wwwrootDir = Join-Path "$buildDir\api" "wwwroot"
New-Item -ItemType Directory -Force -Path $wwwrootDir | Out-Null
Copy-Item "$rootDir\Aura.Web\dist\*" -Destination $wwwrootDir -Recurse -Force
# Also copy to web folder for reference
Copy-Item "$rootDir\Aura.Web\dist\*" -Destination "$buildDir\web" -Recurse -Force
Write-Host "      ✓ Web UI copied to wwwroot" -ForegroundColor Green

# Copy FFmpeg binaries
Write-Host "[6/8] Copying FFmpeg binaries..." -ForegroundColor Yellow
if (Test-Path "$rootDir\scripts\ffmpeg\ffmpeg.exe") {
    Copy-Item "$rootDir\scripts\ffmpeg\ffmpeg.exe" -Destination "$buildDir\ffmpeg" -Force
    Write-Host "      ✓ ffmpeg.exe copied" -ForegroundColor Green
} else {
    Write-Host "      ⚠ ffmpeg.exe not found (users will need to install separately)" -ForegroundColor Yellow
}
if (Test-Path "$rootDir\scripts\ffmpeg\ffprobe.exe") {
    Copy-Item "$rootDir\scripts\ffmpeg\ffprobe.exe" -Destination "$buildDir\ffmpeg" -Force
    Write-Host "      ✓ ffprobe.exe copied" -ForegroundColor Green
} else {
    Write-Host "      ⚠ ffprobe.exe not found (users will need to install separately)" -ForegroundColor Yellow
}

# Copy config files
Write-Host "[7/8] Copying configuration and documentation..." -ForegroundColor Yellow
Copy-Item "$rootDir\appsettings.json" -Destination "$buildDir\config" -Force
Copy-Item "$rootDir\PORTABLE.md" -Destination "$buildDir\README.md" -Force
if (Test-Path "$rootDir\LICENSE") {
    Copy-Item "$rootDir\LICENSE" -Destination $buildDir -Force
}
Write-Host "      ✓ Config and docs copied" -ForegroundColor Green

# Create start_portable.cmd launcher script
Write-Host "[8/8] Creating launcher script..." -ForegroundColor Yellow
$launcherScript = @'
@echo off
setlocal enabledelayedexpansion

echo ========================================
echo  Aura Video Studio - Portable Edition
echo ========================================
echo.

:: Start API in background
echo Starting API server...
start "Aura API" /D "api" "Aura.Api.exe"

:: Wait for API to be healthy
echo Waiting for API to start...
set MAX_ATTEMPTS=30
set ATTEMPT=0

:WAIT_LOOP
set /a ATTEMPT+=1
if !ATTEMPT! GTR %MAX_ATTEMPTS% (
    echo.
    echo ERROR: API failed to start after %MAX_ATTEMPTS% seconds
    echo Please check the API console window for errors
    pause
    exit /b 1
)

:: Check if API is healthy using curl or powershell
powershell -Command "try { $response = Invoke-WebRequest -Uri 'http://127.0.0.1:5005/healthz' -UseBasicParsing -TimeoutSec 2; if ($response.StatusCode -eq 200) { exit 0 } else { exit 1 } } catch { exit 1 }" >nul 2>&1

if !ERRORLEVEL! EQU 0 (
    echo.
    echo ✓ API is healthy!
    goto API_READY
)

:: Wait 1 second before next attempt
timeout /t 1 /nobreak >nul
goto WAIT_LOOP

:API_READY
echo.
echo Opening web browser...
start "" "http://127.0.0.1:5005"
echo.
echo Application started successfully!
echo.
echo Web UI: http://127.0.0.1:5005
echo.
echo To stop the application:
echo   - Close the "Aura API" window
echo   - Or press Ctrl+C in this window
echo.
echo Press any key to exit this launcher...
pause >nul
'@
Set-Content -Path "$buildDir\start_portable.cmd" -Value $launcherScript -Encoding ASCII
Write-Host "      ✓ Launcher script created (start_portable.cmd)" -ForegroundColor Green

# Generate checksums.txt
Write-Host ""
Write-Host "Generating checksums..." -ForegroundColor Yellow
$checksumContent = ""
Get-ChildItem -Path $buildDir -Recurse -File | ForEach-Object {
    $hash = Get-FileHash -Path $_.FullName -Algorithm SHA256
    $relativePath = $_.FullName.Substring($buildDir.Length + 1)
    $checksumContent += "$($hash.Hash)  $relativePath`n"
}
Set-Content -Path "$buildDir\checksums.txt" -Value $checksumContent -Encoding UTF8
Write-Host "✓ checksums.txt generated" -ForegroundColor Green

# Generate SBOM (sbom.json)
Write-Host "Generating SBOM..." -ForegroundColor Yellow
$sbom = @{
    bomFormat = "CycloneDX"
    specVersion = "1.4"
    version = 1
    metadata = @{
        timestamp = (Get-Date -Format "o")
        component = @{
            type = "application"
            name = "Aura Video Studio"
            version = "1.0.0"
            description = "AI-powered video creation tool for Windows 11"
        }
    }
    components = @(
        @{
            type = "library"
            name = ".NET Runtime"
            version = "8.0"
            licenses = @(@{ license = @{ id = "MIT" } })
        },
        @{
            type = "library"
            name = "FFmpeg"
            version = "6.0"
            licenses = @(@{ license = @{ id = "LGPL-2.1" } })
        },
        @{
            type = "library"
            name = "ASP.NET Core"
            version = "8.0"
            licenses = @(@{ license = @{ id = "MIT" } })
        },
        @{
            type = "library"
            name = "React"
            version = "18.2"
            licenses = @(@{ license = @{ id = "MIT" } })
        },
        @{
            type = "library"
            name = "Fluent UI React"
            version = "9.47"
            licenses = @(@{ license = @{ id = "MIT" } })
        },
        @{
            type = "library"
            name = "Serilog"
            version = "3.x"
            licenses = @(@{ license = @{ id = "Apache-2.0" } })
        },
        @{
            type = "library"
            name = "NAudio"
            version = "2.x"
            licenses = @(@{ license = @{ id = "MIT" } })
        },
        @{
            type = "library"
            name = "SkiaSharp"
            version = "2.x"
            licenses = @(@{ license = @{ id = "MIT" } })
        }
    )
}
$sbom | ConvertTo-Json -Depth 10 | Out-File "$buildDir\sbom.json" -Encoding utf8
Write-Host "✓ sbom.json generated" -ForegroundColor Green

# Generate attributions.txt
Write-Host "Generating attributions..." -ForegroundColor Yellow
$attributions = @"
AURA VIDEO STUDIO - Third-Party Software Attributions
======================================================

This software includes or depends on the following third-party components:

1. .NET Runtime
   Version: 8.0
   License: MIT License
   Copyright (c) .NET Foundation and Contributors
   https://github.com/dotnet/runtime

2. FFmpeg
   Version: 6.0+
   License: LGPL 2.1 or later (depending on build configuration)
   Copyright (c) FFmpeg team
   https://ffmpeg.org/
   
   Note: This software uses code of FFmpeg licensed under the LGPLv2.1
   and its source can be downloaded from the FFmpeg website.

3. ASP.NET Core
   Version: 8.0
   License: MIT License
   Copyright (c) .NET Foundation and Contributors
   https://github.com/dotnet/aspnetcore

4. React
   Version: 18.2
   License: MIT License
   Copyright (c) Facebook, Inc. and its affiliates
   https://reactjs.org/

5. Fluent UI React (@fluentui/react-components)
   Version: 9.47
   License: MIT License
   Copyright (c) Microsoft Corporation
   https://github.com/microsoft/fluentui

6. Serilog
   Version: 3.x
   License: Apache License 2.0
   Copyright (c) Serilog Contributors
   https://serilog.net/

7. NAudio
   Version: 2.x
   License: MIT License
   Copyright (c) Mark Heath
   https://github.com/naudio/NAudio

8. SkiaSharp
   Version: 2.x
   License: MIT License
   Copyright (c) Microsoft Corporation
   https://github.com/mono/SkiaSharp

For complete license texts, see the respective project repositories.

Additional licenses for bundled assets:
- Default music pack: CC0 1.0 Universal (Public Domain)
- Stock placeholder images: CC0 1.0 Universal (Public Domain)

Contact: https://github.com/Coffee285/aura-video-studio/issues
"@
Set-Content -Path "$buildDir\attributions.txt" -Value $attributions -Encoding utf8
Write-Host "✓ attributions.txt generated" -ForegroundColor Green

# Create ZIP
Write-Host ""
Write-Host "Creating ZIP archive..." -ForegroundColor Yellow
$zipPath = Join-Path $portableDir "AuraVideoStudio_Portable_x64.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}
Compress-Archive -Path "$buildDir\*" -DestinationPath $zipPath -Force
Write-Host "✓ ZIP archive created" -ForegroundColor Green

# Generate final checksum for the ZIP
$zipHash = Get-FileHash -Path $zipPath -Algorithm SHA256
$zipChecksumFile = Join-Path $portableDir "AuraVideoStudio_Portable_x64.zip.sha256"
"$($zipHash.Hash)  AuraVideoStudio_Portable_x64.zip" | Out-File $zipChecksumFile -Encoding utf8

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Portable ZIP:  $zipPath" -ForegroundColor White
Write-Host "Size:          $([math]::Round((Get-Item $zipPath).Length / 1MB, 2)) MB" -ForegroundColor White
Write-Host "SHA-256:       $($zipHash.Hash)" -ForegroundColor White
Write-Host ""
Write-Host "Contents:" -ForegroundColor Cyan
Write-Host "  /api/*                - Aura.Api binaries with Web UI in wwwroot" -ForegroundColor White
Write-Host "  /web/*                - Aura.Web static build (reference copy)" -ForegroundColor White
Write-Host "  /ffmpeg/*             - FFmpeg binaries" -ForegroundColor White
Write-Host "  /config/*             - Configuration files" -ForegroundColor White
Write-Host "  /start_portable.cmd   - Launcher with /healthz check" -ForegroundColor White
Write-Host "  /checksums.txt        - File checksums" -ForegroundColor White
Write-Host "  /sbom.json            - Software Bill of Materials" -ForegroundColor White
Write-Host "  /attributions.txt     - Third-party licenses" -ForegroundColor White
Write-Host "  /README.md            - User documentation" -ForegroundColor White
Write-Host ""
Write-Host "To test locally:" -ForegroundColor Cyan
Write-Host "  1. Extract the ZIP to a test folder" -ForegroundColor White
Write-Host "  2. Run start_portable.cmd" -ForegroundColor White
Write-Host "  3. Wait for browser to open at http://127.0.0.1:5005" -ForegroundColor White
Write-Host ""
