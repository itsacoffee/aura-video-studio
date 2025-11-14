# Aura Video Studio - First Time Setup Script (PowerShell)
# This script builds the frontend and backend in the correct order

$ErrorActionPreference = "Stop"

Write-Output "======================================" -ForegroundColor Cyan
Write-Output "Aura Video Studio - First Time Setup" -ForegroundColor Cyan
Write-Output "======================================" -ForegroundColor Cyan
Write-Output ""

# Check Node.js
try {
    $nodeVersion = node --version
    Write-Output "✓ Node.js found: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Output "❌ Error: Node.js is not installed" -ForegroundColor Red
    Write-Output "Please install Node.js 18.0.0+ from https://nodejs.org/" -ForegroundColor Yellow
    exit 1
}

# Check .NET
try {
    $dotnetVersion = dotnet --version
    Write-Output "✓ .NET SDK found: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Output "❌ Error: .NET SDK is not installed" -ForegroundColor Red
    Write-Output "Please install .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    exit 1
}

Write-Output ""
Write-Output "======================================" -ForegroundColor Cyan
Write-Output "Step 1: Building Frontend" -ForegroundColor Cyan
Write-Output "======================================" -ForegroundColor Cyan
Push-Location Aura.Web

# Check if node_modules exists
if (-not (Test-Path "node_modules")) {
    Write-Output "Installing npm dependencies..." -ForegroundColor Yellow
    npm install
} else {
    Write-Output "✓ npm dependencies already installed" -ForegroundColor Green
}

Write-Output "Building frontend (this may take a moment)..." -ForegroundColor Yellow
npm run build

if (-not (Test-Path "dist") -or -not (Test-Path "dist/index.html")) {
    Write-Output "❌ Frontend build failed - dist folder not created" -ForegroundColor Red
    Pop-Location
    exit 1
}

Write-Output "✓ Frontend built successfully" -ForegroundColor Green
Write-Output ""

Write-Output "======================================" -ForegroundColor Cyan
Write-Output "Step 2: Building Backend" -ForegroundColor Cyan
Write-Output "======================================" -ForegroundColor Cyan
Pop-Location

Write-Output "Building .NET solution..." -ForegroundColor Yellow
dotnet build Aura.sln --configuration Release

Write-Output "✓ Backend built successfully" -ForegroundColor Green
Write-Output ""

Write-Output "======================================" -ForegroundColor Cyan
Write-Output "Step 3: Verifying Setup" -ForegroundColor Cyan
Write-Output "======================================" -ForegroundColor Cyan

$wwwrootPath = "Aura.Api\bin\Release\net8.0\wwwroot"
if (-not (Test-Path $wwwrootPath) -or -not (Test-Path "$wwwrootPath\index.html")) {
    Write-Output "❌ Warning: Frontend not copied to wwwroot" -ForegroundColor Red
    Write-Output "The build process should have copied dist to wwwroot automatically." -ForegroundColor Yellow
    exit 1
}

Write-Output "✓ Frontend copied to wwwroot" -ForegroundColor Green
Write-Output "✓ Setup complete!" -ForegroundColor Green
Write-Output ""

Write-Output "======================================" -ForegroundColor Cyan
Write-Output "Ready to Run!" -ForegroundColor Cyan
Write-Output "======================================" -ForegroundColor Cyan
Write-Output ""
Write-Output "To start the application:" -ForegroundColor Yellow
Write-Output "  cd Aura.Api" -ForegroundColor White
Write-Output "  dotnet run --configuration Release" -ForegroundColor White
Write-Output ""
Write-Output "Then open your browser to: " -NoNewline -ForegroundColor Yellow
Write-Output "http://127.0.0.1:5005" -ForegroundColor Cyan
Write-Output ""
Write-Output "For development mode (with hot reload), see FIRST_RUN_GUIDE.md" -ForegroundColor Gray
Write-Output ""
