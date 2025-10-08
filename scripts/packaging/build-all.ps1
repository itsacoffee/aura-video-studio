# Build All Distributions for Aura Video Studio
# This script builds MSIX, Setup EXE, and Portable ZIP distributions

param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [string]$SigningCert = "",
    [string]$CertPassword = ""
)

$ErrorActionPreference = "Stop"

Write-Host "=== Building Aura Video Studio Distributions ===" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration"
Write-Host "Platform: $Platform"
Write-Host ""

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

# Check for npm
$npmPath = Get-Command npm -ErrorAction SilentlyContinue
if (-not $npmPath) {
    Write-Host ""
    Write-Host "ERROR: npm is not installed or not in PATH" -ForegroundColor Red
    Write-Host ""
    Write-Host "Node.js and npm are required to build the web UI (Aura.Web)." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To fix this:" -ForegroundColor Cyan
    Write-Host "  1. Download and install Node.js from: https://nodejs.org/" -ForegroundColor White
    Write-Host "  2. Recommended version: Node.js 20.x or later (LTS)" -ForegroundColor White
    Write-Host "  3. After installation, restart your PowerShell session" -ForegroundColor White
    Write-Host "  4. Verify installation by running: npm --version" -ForegroundColor White
    Write-Host ""
    Write-Host "Alternatively, install via chocolatey:" -ForegroundColor Cyan
    Write-Host "  choco install nodejs-lts" -ForegroundColor White
    Write-Host ""
    exit 1
}

Write-Host "✓ npm found: $($npmPath.Source)" -ForegroundColor Green
Write-Host ""

# Set paths
$rootDir = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$artifactsDir = Join-Path $rootDir "artifacts\windows"
$msixDir = Join-Path $artifactsDir "msix"
$exeDir = Join-Path $artifactsDir "exe"
$portableDir = Join-Path $artifactsDir "portable"

# Create directories
New-Item -ItemType Directory -Force -Path $msixDir | Out-Null
New-Item -ItemType Directory -Force -Path $exeDir | Out-Null
New-Item -ItemType Directory -Force -Path $portableDir | Out-Null

# Step 1: Build Core Projects
Write-Host "Step 1: Building core projects..." -ForegroundColor Yellow
dotnet build "$rootDir\Aura.Core\Aura.Core.csproj" -c $Configuration
dotnet build "$rootDir\Aura.Providers\Aura.Providers.csproj" -c $Configuration
dotnet build "$rootDir\Aura.Api\Aura.Api.csproj" -c $Configuration

# Step 2: Build Web UI
Write-Host "Step 2: Building web UI..." -ForegroundColor Yellow
Push-Location "$rootDir\Aura.Web"
if (Test-Path "node_modules") {
    npm run build
} else {
    npm install
    npm run build
}
Pop-Location

# Step 3: Build MSIX Package (WinUI 3)
Write-Host "Step 3: Building MSIX package..." -ForegroundColor Yellow
try {
    msbuild "$rootDir\Aura.App\Aura.App.csproj" `
        /p:Configuration=$Configuration `
        /p:Platform=$Platform `
        /p:AppxBundle=Never `
        /p:UapAppxPackageBuildMode=SideloadOnly `
        /restore
    
    # Copy MSIX to artifacts
    if (Test-Path "$rootDir\Aura.App\AppPackages") {
        Copy-Item "$rootDir\Aura.App\AppPackages\**\*.msix" -Destination $msixDir -Recurse -ErrorAction SilentlyContinue
    }
    if (Test-Path "$rootDir\Aura.App\bin\$Platform\$Configuration") {
        Copy-Item "$rootDir\Aura.App\bin\$Platform\$Configuration\**\*.msix" -Destination $msixDir -Recurse -ErrorAction SilentlyContinue
    }
    
    Write-Host "✓ MSIX package built successfully" -ForegroundColor Green
} catch {
    Write-Host "✗ MSIX build failed: $_" -ForegroundColor Red
}

# Step 4: Build Portable Distribution
Write-Host "Step 4: Building portable distribution..." -ForegroundColor Yellow
$portableBuildDir = Join-Path $portableDir "build"
New-Item -ItemType Directory -Force -Path $portableBuildDir | Out-Null

# Publish API as self-contained
dotnet publish "$rootDir\Aura.Api\Aura.Api.csproj" `
    -c $Configuration `
    -r win-$($Platform.ToLower()) `
    --self-contained `
    -o "$portableBuildDir\Api"

# Copy Web UI
Copy-Item "$rootDir\Aura.Web\dist\*" -Destination "$portableBuildDir\Web" -Recurse -Force

# Copy FFmpeg
Copy-Item "$rootDir\scripts\ffmpeg\*.exe" -Destination "$portableBuildDir\ffmpeg" -Force -ErrorAction SilentlyContinue

# Copy config and docs
Copy-Item "$rootDir\appsettings.json" -Destination $portableBuildDir -Force
Copy-Item "$rootDir\README.md" -Destination $portableBuildDir -Force
Copy-Item "$rootDir\LICENSE" -Destination $portableBuildDir -Force -ErrorAction SilentlyContinue

# Create launcher script
$launcherScript = @"
@echo off
echo Starting Aura Video Studio...
start "" "Api\Aura.Api.exe"
timeout /t 3 /nobreak >nul
start "" "http://127.0.0.1:5005"
"@
Set-Content -Path "$portableBuildDir\Launch.bat" -Value $launcherScript

# Create ZIP
$zipPath = Join-Path $portableDir "AuraVideoStudio_Portable_x64.zip"
Compress-Archive -Path "$portableBuildDir\*" -DestinationPath $zipPath -Force
Write-Host "✓ Portable ZIP created: $zipPath" -ForegroundColor Green

# Step 5: Build Setup EXE (if Inno Setup is available)
Write-Host "Step 5: Building setup EXE..." -ForegroundColor Yellow
$innoSetup = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (Test-Path $innoSetup) {
    & $innoSetup "$rootDir\scripts\packaging\setup.iss"
    Write-Host "✓ Setup EXE built successfully" -ForegroundColor Green
} else {
    Write-Host "⚠ Inno Setup not found, skipping EXE installer" -ForegroundColor Yellow
}

# Step 6: Generate Checksums
Write-Host "Step 6: Generating checksums..." -ForegroundColor Yellow
$checksumFile = Join-Path $artifactsDir "checksums.txt"
Get-ChildItem $artifactsDir -Recurse -Include *.msix,*.exe,*.zip | ForEach-Object {
    $hash = Get-FileHash -Path $_.FullName -Algorithm SHA256
    "$($hash.Hash)  $($_.Name)"
} | Out-File $checksumFile -Encoding utf8
Write-Host "✓ Checksums generated: $checksumFile" -ForegroundColor Green

# Step 7: Sign Artifacts (if certificate provided)
if ($SigningCert -and $CertPassword) {
    Write-Host "Step 7: Signing artifacts..." -ForegroundColor Yellow
    Get-ChildItem $artifactsDir -Recurse -Include *.msix,*.exe | ForEach-Object {
        signtool sign /fd SHA256 /f $SigningCert /p $CertPassword $_.FullName
    }
    Write-Host "✓ Artifacts signed" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Build Complete ===" -ForegroundColor Cyan
Write-Host "Artifacts location: $artifactsDir"
Write-Host ""
Get-ChildItem $artifactsDir -Recurse -Include *.msix,*.exe,*.zip | ForEach-Object {
    Write-Host "  $($_.Name) ($([math]::Round($_.Length / 1MB, 2)) MB)" -ForegroundColor White
}
