#!/usr/bin/env pwsh
# Verify that the production build includes all required components

param(
    [string]$BuildPath = "dist"
)

Write-Host "Verifying Aura Video Studio build..." -ForegroundColor Cyan

$errors = @()

# Check if build directory exists
if (-not (Test-Path $BuildPath)) {
    $errors += "Build directory not found: $BuildPath"
}

# Check for installer
$installerPattern = Join-Path $BuildPath "Aura Video Studio-*.exe"
$installers = Get-ChildItem -Path $installerPattern -ErrorAction SilentlyContinue
if ($installers.Count -eq 0) {
    $errors += "No installer found in $BuildPath"
} else {
    Write-Host "✓ Found installer: $($installers[0].Name)" -ForegroundColor Green
}

# Check for portable build (if building portable)
$portablePattern = Join-Path $BuildPath "*Portable.exe"
$portables = Get-ChildItem -Path $portablePattern -ErrorAction SilentlyContinue
if ($portables.Count -gt 0) {
    Write-Host "✓ Found portable build: $($portables[0].Name)" -ForegroundColor Green
}

# Extract and verify backend in build
$unpackPath = Join-Path $BuildPath "win-unpacked"
if (Test-Path $unpackPath) {
    $backendPath = Join-Path $unpackPath "resources\backend\win-x64\Aura.Api.exe"
    if (Test-Path $backendPath) {
        Write-Host "✓ Backend executable found in unpacked build" -ForegroundColor Green
        $fileInfo = Get-Item $backendPath
        $sizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
        Write-Host "  Backend size: $sizeMB MB" -ForegroundColor Gray
    } else {
        $errors += "Backend executable not found in unpacked build"
    }
    
    # Check for frontend
    $frontendPath = Join-Path $unpackPath "resources\frontend\index.html"
    if (Test-Path $frontendPath) {
        Write-Host "✓ Frontend found in unpacked build" -ForegroundColor Green
    } else {
        $errors += "Frontend not found in unpacked build"
    }
} else {
    Write-Host "Note: Unpacked build directory not found (this is normal if using --dir was not specified)" -ForegroundColor Yellow
}

if ($errors.Count -gt 0) {
    Write-Host "`nBuild verification FAILED:" -ForegroundColor Red
    foreach ($error in $errors) {
        Write-Host "  ✗ $error" -ForegroundColor Red
    }
    exit 1
} else {
    Write-Host "`n✓ Build verification PASSED" -ForegroundColor Green
    exit 0
}
