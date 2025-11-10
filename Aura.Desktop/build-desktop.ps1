# PowerShell Build Script for Aura Video Studio Desktop
param(
    [string]$Target = "all",
    [switch]$SkipFrontend,
    [switch]$SkipBackend,
    [switch]$SkipInstaller,
    [switch]$Help
)

# Colors for output
$ErrorColor = "Red"
$SuccessColor = "Green"
$WarningColor = "Yellow"
$InfoColor = "Cyan"

function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor $InfoColor
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor $SuccessColor
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor $WarningColor
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor $ErrorColor
}

if ($Help) {
    Write-Host "Aura Video Studio - Desktop Build Script"
    Write-Host ""
    Write-Host "Usage: .\build-desktop.ps1 [OPTIONS]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Target <platform>    Build for specific platform (win|mac|linux|all)"
    Write-Host "  -SkipFrontend         Skip frontend build"
    Write-Host "  -SkipBackend          Skip backend build"
    Write-Host "  -SkipInstaller        Skip installer creation"
    Write-Host "  -Help                 Show this help message"
    exit 0
}

Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host "Aura Video Studio - Desktop Build" -ForegroundColor $InfoColor
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host ""

Write-Info "Build target: $Target"
Write-Host ""

# Check if Node.js is installed
if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Error "Node.js is not installed. Please install Node.js 18+ from https://nodejs.org/"
    exit 1
}

# Check if dotnet is installed
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error ".NET 8.0 SDK is not installed. Please install from https://dotnet.microsoft.com/download"
    exit 1
}

$ScriptDir = $PSScriptRoot
$ProjectRoot = Split-Path $ScriptDir -Parent

Set-Location $ScriptDir

# ========================================
# Step 1: Build Frontend
# ========================================
if (-not $SkipFrontend) {
    Write-Info "Building React frontend..."
    Set-Location "$ProjectRoot\Aura.Web"
    
    if (-not (Test-Path "node_modules")) {
        Write-Info "Installing frontend dependencies..."
        npm install
    }
    
    Write-Info "Running frontend build..."
    npm run build
    
    if (-not (Test-Path "dist\index.html")) {
        Write-Error "Frontend build failed - dist\index.html not found"
        exit 1
    }
    
    Write-Success "Frontend build complete"
    Write-Host ""
} else {
    Write-Warning "Skipping frontend build"
    Write-Host ""
}

# ========================================
# Step 2: Build Backend
# ========================================
if (-not $SkipBackend) {
    Write-Info "Building .NET backend..."
    Set-Location "$ProjectRoot\Aura.Api"
    
    # Create backend output directory
    $BackendDir = "$ScriptDir\backend"
    if (-not (Test-Path $BackendDir)) {
        New-Item -ItemType Directory -Path $BackendDir | Out-Null
    }
    
    if ($Target -eq "all" -or $Target -eq "win") {
        Write-Info "Building backend for Windows (x64)..."
        dotnet publish -c Release -r win-x64 --self-contained true `
            -p:PublishSingleFile=false `
            -p:PublishTrimmed=false `
            -p:IncludeNativeLibrariesForSelfExtract=true `
            -o "$BackendDir\win-x64"
        Write-Success "Windows backend build complete"
    }
    
    if ($Target -eq "all" -or $Target -eq "mac") {
        Write-Info "Building backend for macOS (x64)..."
        dotnet publish -c Release -r osx-x64 --self-contained true `
            -p:PublishSingleFile=false `
            -p:PublishTrimmed=false `
            -o "$BackendDir\osx-x64"
        
        Write-Info "Building backend for macOS (arm64)..."
        dotnet publish -c Release -r osx-arm64 --self-contained true `
            -p:PublishSingleFile=false `
            -p:PublishTrimmed=false `
            -o "$BackendDir\osx-arm64"
        Write-Success "macOS backend builds complete"
    }
    
    if ($Target -eq "all" -or $Target -eq "linux") {
        Write-Info "Building backend for Linux (x64)..."
        dotnet publish -c Release -r linux-x64 --self-contained true `
            -p:PublishSingleFile=false `
            -p:PublishTrimmed=false `
            -o "$BackendDir\linux-x64"
        Write-Success "Linux backend build complete"
    }
    
    Write-Success "Backend builds complete"
    Write-Host ""
} else {
    Write-Warning "Skipping backend build"
    Write-Host ""
}

# ========================================
# Step 3: Install Electron Dependencies
# ========================================
Write-Info "Installing Electron dependencies..."
Set-Location $ScriptDir

if (-not (Test-Path "node_modules")) {
    npm install
} else {
    Write-Info "Dependencies already installed"
}

Write-Success "Electron dependencies ready"
Write-Host ""

# ========================================
# Step 4: Build Electron Installers
# ========================================
if (-not $SkipInstaller) {
    Write-Info "Building Electron installers..."
    
    switch ($Target) {
        "win" {
            Write-Info "Building Windows installer..."
            npm run build:win
        }
        "mac" {
            Write-Info "Building macOS installer..."
            npm run build:mac
        }
        "linux" {
            Write-Info "Building Linux packages..."
            npm run build:linux
        }
        "all" {
            Write-Info "Building installers for all platforms..."
            npm run build:all
        }
        default {
            Write-Error "Unknown target: $Target"
            exit 1
        }
    }
    
    Write-Success "Installer build complete"
} else {
    Write-Warning "Skipping installer creation (building directory only)"
    npm run build:dir
}

Write-Host ""
Write-Success "========================================"
Write-Success "Build Complete!"
Write-Success "========================================"
Write-Host ""
Write-Info "Output directory: $ScriptDir\dist"
Write-Host ""

# List generated files
if (Test-Path "$ScriptDir\dist") {
    Write-Info "Generated files:"
    Get-ChildItem "$ScriptDir\dist" | ForEach-Object {
        $size = if ($_.PSIsContainer) { "DIR" } else { "{0:N2} MB" -f ($_.Length / 1MB) }
        Write-Host "  $($_.Name) ($size)"
    }
    Write-Host ""
}

Write-Info "To run the app in development mode:"
Write-Host "  cd Aura.Desktop"
Write-Host "  npm start"
Write-Host ""

Write-Success "All done! 🎉"
