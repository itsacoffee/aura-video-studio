# Build Portable Distribution for Aura Video Studio
# This script provides a simple way to build the portable distribution

param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Aura Video Studio - Portable Builder" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration: $Configuration" -ForegroundColor White
Write-Host "Platform:      $Platform" -ForegroundColor White
Write-Host ""

# Set paths
$scriptDir = $PSScriptRoot
$rootDir = Split-Path -Parent (Split-Path -Parent $scriptDir)
$artifactsDir = Join-Path $rootDir "artifacts"
$portableDir = Join-Path $artifactsDir "portable"
$portableBuildDir = Join-Path $portableDir "build"

Write-Host "Root Directory:     $rootDir" -ForegroundColor Gray
Write-Host "Artifacts Directory: $artifactsDir" -ForegroundColor Gray
Write-Host ""

# Create directories
Write-Host "[1/6] Creating build directories..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path $portableBuildDir | Out-Null
Write-Host "      ✓ Directories created" -ForegroundColor Green

# Build core projects
Write-Host "[2/6] Building .NET projects..." -ForegroundColor Yellow
dotnet build "$rootDir\Aura.Core\Aura.Core.csproj" -c $Configuration --nologo -v minimal
dotnet build "$rootDir\Aura.Providers\Aura.Providers.csproj" -c $Configuration --nologo -v minimal
dotnet build "$rootDir\Aura.Api\Aura.Api.csproj" -c $Configuration --nologo -v minimal
Write-Host "      ✓ .NET projects built" -ForegroundColor Green

# Build Web UI
Write-Host "[3/6] Building web UI..." -ForegroundColor Yellow
Push-Location "$rootDir\Aura.Web"
if (-not (Test-Path "node_modules")) {
    Write-Host "      Installing npm dependencies..." -ForegroundColor Gray
    npm install --silent 2>&1 | Out-Null
}
npm run build --silent 2>&1 | Out-Null
Pop-Location
Write-Host "      ✓ Web UI built" -ForegroundColor Green

# Publish API as self-contained
Write-Host "[4/6] Publishing API (self-contained)..." -ForegroundColor Yellow
dotnet publish "$rootDir\Aura.Api\Aura.Api.csproj" `
    -c $Configuration `
    -r win-$($Platform.ToLower()) `
    --self-contained `
    -o "$portableBuildDir\Api" `
    --nologo -v minimal
Write-Host "      ✓ API published" -ForegroundColor Green

# Copy Web UI to wwwroot folder inside the published API
Write-Host "[5/6] Copying web UI to wwwroot..." -ForegroundColor Yellow
$wwwrootDir = Join-Path "$portableBuildDir\Api" "wwwroot"
New-Item -ItemType Directory -Force -Path $wwwrootDir | Out-Null
Copy-Item "$rootDir\Aura.Web\dist\*" -Destination $wwwrootDir -Recurse -Force
Write-Host "      ✓ Web UI copied to wwwroot" -ForegroundColor Green

# Copy additional files
Write-Host "[6/6] Copying additional files..." -ForegroundColor Yellow

# Copy FFmpeg (if available)
$ffmpegDir = Join-Path $portableBuildDir "ffmpeg"
New-Item -ItemType Directory -Force -Path $ffmpegDir | Out-Null
if (Test-Path "$rootDir\scripts\ffmpeg\ffmpeg.exe") {
    Copy-Item "$rootDir\scripts\ffmpeg\*.exe" -Destination $ffmpegDir -Force
    Write-Host "      ✓ FFmpeg binaries copied" -ForegroundColor Green
} else {
    Write-Host "      ⚠ FFmpeg binaries not found (users will need to install separately)" -ForegroundColor Yellow
}

# Copy config and docs
Copy-Item "$rootDir\appsettings.json" -Destination $portableBuildDir -Force
Copy-Item "$rootDir\PORTABLE.md" -Destination "$portableBuildDir\README.md" -Force
if (Test-Path "$rootDir\LICENSE") {
    Copy-Item "$rootDir\LICENSE" -Destination $portableBuildDir -Force
}

# Create launcher script
$launcherScript = @"
@echo off
echo ========================================
echo  Aura Video Studio - Portable Edition
echo ========================================
echo.
echo Starting API server...
start "" /D "Api" "Aura.Api.exe"
echo Waiting for server to start...
timeout /t 3 /nobreak >nul
echo.
echo Opening web browser...
start "" "http://127.0.0.1:5005"
echo.
echo The application should open in your web browser.
echo If not, manually navigate to: http://127.0.0.1:5005
echo.
echo To stop the application, close the API server window.
echo.
"@
Set-Content -Path "$portableBuildDir\Launch.bat" -Value $launcherScript
Write-Host "      ✓ Launch script created" -ForegroundColor Green

# Create ZIP
$zipPath = Join-Path $portableDir "AuraVideoStudio_Portable_x64.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}
Compress-Archive -Path "$portableBuildDir\*" -DestinationPath $zipPath -Force

# Generate checksum
$hash = Get-FileHash -Path $zipPath -Algorithm SHA256
$checksumFile = Join-Path $portableDir "checksum.txt"
"$($hash.Hash)  $(Split-Path $zipPath -Leaf)" | Out-File $checksumFile -Encoding utf8

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Portable ZIP:  $zipPath" -ForegroundColor White
Write-Host "Size:          $([math]::Round((Get-Item $zipPath).Length / 1MB, 2)) MB" -ForegroundColor White
Write-Host "SHA-256:       $($hash.Hash)" -ForegroundColor White
Write-Host ""
Write-Host "To test locally:" -ForegroundColor Cyan
Write-Host "  1. Extract the ZIP to a folder" -ForegroundColor White
Write-Host "  2. Run Launch.bat" -ForegroundColor White
Write-Host "  3. Open http://127.0.0.1:5005 in your browser" -ForegroundColor White
Write-Host ""
