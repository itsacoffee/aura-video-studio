# Aura Video Studio - First Time Setup Script (PowerShell)
# This script builds the frontend and backend in the correct order

$ErrorActionPreference = "Stop"

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Aura Video Studio - First Time Setup" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Output ""

# Check Node.js
try {
    $nodeVersion = node --version
    Write-Host "✓ Node.js found: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Error: Node.js is not installed" -ForegroundColor Red
    Write-Host "Please install Node.js 18.0.0+ from https://nodejs.org/" -ForegroundColor Yellow
    exit 1
}

# Check .NET
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET SDK found: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Error: .NET SDK is not installed" -ForegroundColor Red
    Write-Host "Please install .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    exit 1
}

Write-Output ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Step 1: Building Frontend" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Push-Location Aura.Web

# Check if node_modules exists and vite is properly installed
# We check for the vite CLI in node_modules/.bin which npm uses to run commands
$needsInstall = $false
if (-not (Test-Path "node_modules")) {
    Write-Host "Installing npm dependencies..." -ForegroundColor Yellow
    $needsInstall = $true
} elseif (-not (Test-Path "node_modules\.bin\vite.cmd")) {
    Write-Host "npm dependencies incomplete (vite not found), reinstalling..." -ForegroundColor Yellow
    $needsInstall = $true
} else {
    Write-Host "✓ npm dependencies already installed" -ForegroundColor Green
}

if ($needsInstall) {
    # Check if NODE_ENV=production is set (would skip devDependencies)
    if ($env:NODE_ENV -eq "production") {
        Write-Host "⚠ Warning: NODE_ENV is set to 'production'. Using --include=dev to ensure all dependencies are installed." -ForegroundColor Yellow
    }
    
    # Use --include=dev to ensure devDependencies are always installed
    # This is required because vite is a devDependency needed for building
    npm install --include=dev
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ npm install failed" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    Write-Host "✓ npm dependencies installed" -ForegroundColor Green
}

Write-Host "Building frontend (this may take a moment)..." -ForegroundColor Yellow
npm run build

if (-not (Test-Path "dist") -or -not (Test-Path "dist/index.html")) {
    Write-Host "❌ Frontend build failed - dist folder not created" -ForegroundColor Red
    Pop-Location
    exit 1
}

Write-Host "✓ Frontend built successfully" -ForegroundColor Green
Write-Output ""

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Step 2: Building Backend" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Pop-Location

Write-Host "Building .NET solution..." -ForegroundColor Yellow
dotnet build Aura.sln --configuration Release

Write-Host "✓ Backend built successfully" -ForegroundColor Green
Write-Output ""

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Step 3: Verifying Setup" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

$wwwrootPath = "Aura.Api\bin\Release\net8.0\wwwroot"
if (-not (Test-Path $wwwrootPath) -or -not (Test-Path "$wwwrootPath\index.html")) {
    Write-Host "❌ Warning: Frontend not copied to wwwroot" -ForegroundColor Red
    Write-Host "The build process should have copied dist to wwwroot automatically." -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ Frontend copied to wwwroot" -ForegroundColor Green
Write-Host "✓ Setup complete!" -ForegroundColor Green
Write-Output ""

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Ready to Run!" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Output ""
Write-Host "To start the application:" -ForegroundColor Yellow
Write-Host "  cd Aura.Api" -ForegroundColor White
Write-Host "  dotnet run --configuration Release" -ForegroundColor White
Write-Output ""
Write-Host "Then open your browser to: " -NoNewline -ForegroundColor Yellow
Write-Host "http://127.0.0.1:5005" -ForegroundColor Cyan
Write-Output ""
Write-Host "For development mode (with hot reload), see FIRST_RUN_GUIDE.md" -ForegroundColor Gray
Write-Output ""
