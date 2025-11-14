# PowerShell Build Script for Aura Video Studio Desktop
param(
    [string]$Target = "win",
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
    Write-Output "[INFO] $Message" -ForegroundColor $InfoColor
}

function Write-Success {
    param([string]$Message)
    Write-Output "[SUCCESS] $Message" -ForegroundColor $SuccessColor
}

function Write-Warning {
    param([string]$Message)
    Write-Output "[WARNING] $Message" -ForegroundColor $WarningColor
}

function Write-Error {
    param([string]$Message)
    Write-Output "[ERROR] $Message" -ForegroundColor $ErrorColor
}

if ($Help) {
    Write-Output "Aura Video Studio - Desktop Build Script"
    Write-Output ""
    Write-Output "Usage: .\build-desktop.ps1 [OPTIONS]"
    Write-Output ""
    Write-Output "Options:"
    Write-Output "  -Target <platform>    Build for specific platform (win only, default: win)"
    Write-Output "  -SkipFrontend         Skip frontend build"
    Write-Output "  -SkipBackend          Skip backend build"
    Write-Output "  -SkipInstaller        Skip installer creation"
    Write-Output "  -Help                 Show this help message"
    exit 0
}

Write-Output "========================================" -ForegroundColor $InfoColor
Write-Output "Aura Video Studio - Desktop Build" -ForegroundColor $InfoColor
Write-Output "========================================" -ForegroundColor $InfoColor
Write-Output ""

Write-Info "Build target: $Target"
Write-Output ""

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
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Frontend build failed with exit code $LASTEXITCODE"
        exit 1
    }

    if (-not (Test-Path "dist\index.html")) {
        Write-Error "Frontend build failed - dist\index.html not found"
        exit 1
    }

    Write-Success "Frontend build complete"
    Write-Output ""
} else {
    Write-Warning "Skipping frontend build"
    Write-Output ""
}

# ========================================
# Step 2: Build Backend
# ========================================
if (-not $SkipBackend) {
    Write-Info "Building .NET backend..."
    Set-Location "$ProjectRoot\Aura.Api"

    # Create backend output directory (must match package.json extraResources path)
    $ResourcesDir = "$ScriptDir\resources"
    $BackendDir = "$ResourcesDir\backend"
    if (-not (Test-Path $BackendDir)) {
        New-Item -ItemType Directory -Path $BackendDir -Force | Out-Null
    }

    if ($Target -eq "win") {
        Write-Info "Building backend for Windows (x64)..."
        dotnet publish -c Release -r win-x64 --self-contained true `
            -p:PublishSingleFile=false `
            -p:PublishTrimmed=false `
            -p:IncludeNativeLibrariesForSelfExtract=true `
            -p:SkipFrontendBuild=true `
            -o "$BackendDir\win-x64"
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Windows backend build failed with exit code $LASTEXITCODE"
            exit 1
        }
        Write-Success "Windows backend build complete"
    } else {
        Write-Error "Only Windows builds are supported. Target: $Target"
        exit 1
    }

    Write-Success "Backend builds complete"
    Write-Output ""
} else {
    Write-Warning "Skipping backend build"
    Write-Output ""
}

# ========================================
# Step 3: Install Electron Dependencies
# ========================================
Write-Info "Installing Electron dependencies..."
Set-Location $ScriptDir

if (-not (Test-Path "node_modules")) {
    npm install
    if ($LASTEXITCODE -ne 0) {
        Write-Error "npm install failed with exit code $LASTEXITCODE"
        exit 1
    }
} else {
    Write-Info "Dependencies already installed"
}

Write-Success "Electron dependencies ready"
Write-Output ""

# ========================================
# Step 4: Validate Resources
# ========================================
Write-Info "Validating required resources..."

$RequiredPaths = @(
    @{ Path = "$ProjectRoot\Aura.Web\dist\index.html"; Name = "Frontend build" },
    @{ Path = "$ScriptDir\resources\backend"; Name = "Backend binaries" }
)

$ValidationFailed = $false
foreach ($item in $RequiredPaths) {
    if (-not (Test-Path $item.Path)) {
        Write-Error "$($item.Name) not found at: $($item.Path)"
        $ValidationFailed = $true
    } else {
        Write-Success "  ✓ $($item.Name) found"
    }
}

if ($ValidationFailed) {
    Write-Error "Resource validation failed. Cannot build installer."
    Write-Info "Please ensure all build steps complete successfully."
    exit 1
}

Write-Success "All required resources validated"
Write-Output ""

# ========================================
# Step 5: Build Electron Installers
# ========================================
if (-not $SkipInstaller) {
    Write-Info "Building Electron installers..."

    if ($Target -eq "win") {
        Write-Info "Building Windows installer..."
        npm run build:win
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Windows installer build failed with exit code $LASTEXITCODE"
            exit 1
        }
    } else {
        Write-Error "Only Windows builds are supported. Target: $Target"
        exit 1
    }

    Write-Success "Installer build complete"
} else {
    Write-Warning "Skipping installer creation (building directory only)"
    npm run build:dir
}

Write-Output ""
Write-Success "========================================"
Write-Success "Build Complete!"
Write-Success "========================================"
Write-Output ""
Write-Info "Output directory: $ScriptDir\dist"
Write-Output ""

# List generated files
if (Test-Path "$ScriptDir\dist") {
    Write-Info "Generated files:"
    Get-ChildItem "$ScriptDir\dist" | ForEach-Object {
        $size = if ($_.PSIsContainer) { "DIR" } else { "{0:N2} MB" -f ($_.Length / 1MB) }
        Write-Output "  $($_.Name) ($size)"
    }
    Write-Output ""
}

Write-Info "To run the app in development mode:"
Write-Output "  cd Aura.Desktop"
Write-Output "  npm start"
Write-Output ""

Write-Success "All done! 🎉"
