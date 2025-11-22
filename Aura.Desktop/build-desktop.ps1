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
    Write-Host "[INFO] $Message" -ForegroundColor $InfoColor
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor $SuccessColor
}

function Show-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor $WarningColor
}

function Show-ErrorMessage {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor $ErrorColor
}

if ($Help) {
    Write-Output "Aura Video Studio - Desktop Build Script"
    Write-Output ""
    Write-Output "Usage: .\build-desktop.ps1 [OPTIONS]"
    Write-Output ""
    Write-Output "This script performs a CLEAN BUILD by default, removing all build"
    Write-Output "artifacts before building to ensure a fresh build every time."
    Write-Output ""
    Write-Output "Options:"
    Write-Output "  -Target <platform>    Build for specific platform (win only, default: win)"
    Write-Output "  -SkipFrontend         Skip frontend build (and cleaning)"
    Write-Output "  -SkipBackend          Skip backend build (and cleaning)"
    Write-Output "  -SkipInstaller        Skip installer creation (and cleaning)"
    Write-Output "  -Help                 Show this help message"
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
    Show-ErrorMessage "Node.js is not installed. Please install Node.js 18+ from https://nodejs.org/"
    exit 1
}

# Check if dotnet is installed
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Show-ErrorMessage ".NET 8.0 SDK is not installed. Please install from https://dotnet.microsoft.com/download"
    exit 1
}

$ScriptDir = $PSScriptRoot
$ProjectRoot = Split-Path $ScriptDir -Parent

Set-Location $ScriptDir

# ========================================
# Step 0: Clean Build Artifacts
# ========================================
Write-Info "Cleaning build artifacts for clean build..."
Write-Host ""

# Clean frontend build artifacts
if (-not $SkipFrontend) {
    $FrontendDist = "$ProjectRoot\Aura.Web\dist"
    if (Test-Path $FrontendDist) {
        Write-Info "Cleaning frontend build artifacts..."
        Remove-Item -Path $FrontendDist -Recurse -Force -ErrorAction SilentlyContinue
        Write-Success "  ✓ Frontend dist folder cleaned"
    }
}

# Clean backend build artifacts
if (-not $SkipBackend) {
    Write-Info "Cleaning backend build artifacts..."

    # Clean .NET build outputs (bin/obj folders)
    $ProjectsToClean = @(
        "$ProjectRoot\Aura.Api",
        "$ProjectRoot\Aura.Core",
        "$ProjectRoot\Aura.Providers",
        "$ProjectRoot\Aura.Analyzers"
    )

    foreach ($ProjectPath in $ProjectsToClean) {
        if (Test-Path $ProjectPath) {
            $BinPath = Join-Path $ProjectPath "bin"
            $ObjPath = Join-Path $ProjectPath "obj"

            if (Test-Path $BinPath) {
                Remove-Item -Path $BinPath -Recurse -Force -ErrorAction SilentlyContinue
            }
            if (Test-Path $ObjPath) {
                Remove-Item -Path $ObjPath -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
    }

    # Clean backend output directory
    $BackendOutputDir = "$ScriptDir\resources\backend\win-x64"
    if (Test-Path $BackendOutputDir) {
        Remove-Item -Path $BackendOutputDir -Recurse -Force -ErrorAction SilentlyContinue
        Write-Success "  ✓ Backend output directory cleaned"
    }

    Write-Success "  ✓ Backend build artifacts cleaned"
}

# Clean Electron dist folder (optional, but ensures fresh build)
if (-not $SkipInstaller) {
    $ElectronDist = "$ScriptDir\dist"
    if (Test-Path $ElectronDist) {
        Write-Info "Cleaning Electron dist folder..."
        Remove-Item -Path $ElectronDist -Recurse -Force -ErrorAction SilentlyContinue
        Write-Success "  ✓ Electron dist folder cleaned"
    }
}

Write-Success "Clean build preparation complete"
Write-Host ""

# ========================================
# Step 1: Build Frontend
# ========================================
if (-not $SkipFrontend) {
    Write-Info "Building React frontend..."
    Set-Location "$ProjectRoot\Aura.Web"

    # Always check and install dependencies to ensure they're up to date
    if (-not (Test-Path "node_modules")) {
        Write-Info "Installing frontend dependencies..."
        npm install
        if ($LASTEXITCODE -ne 0) {
            Show-ErrorMessage "Frontend npm install failed with exit code $LASTEXITCODE"
            exit 1
        }
    } else {
        # Verify critical dependencies exist
        $criticalPackages = @("vite", "react", "typescript")
        $missingPackages = @()
        
        foreach ($package in $criticalPackages) {
            if (-not (Test-Path "node_modules\$package")) {
                $missingPackages += $package
            }
        }
        
        if ($missingPackages.Count -gt 0) {
            Write-Info "Critical dependencies missing, reinstalling..."
            Write-Info "Missing: $($missingPackages -join ', ')"
            npm install
            if ($LASTEXITCODE -ne 0) {
                Show-ErrorMessage "Frontend npm install failed with exit code $LASTEXITCODE"
                exit 1
            }
        } else {
            Write-Info "Frontend dependencies verified"
        }
    }

    Write-Info "Running frontend build..."
    npm run build
    if ($LASTEXITCODE -ne 0) {
        Show-ErrorMessage "Frontend build failed with exit code $LASTEXITCODE"
        exit 1
    }

    if (-not (Test-Path "dist\index.html")) {
        Show-ErrorMessage "Frontend build failed - dist\index.html not found"
        exit 1
    }

    Write-Success "Frontend build complete"
    Write-Host ""
}
else {
    Show-Warning "Skipping frontend build"
    Write-Host ""
}

# ========================================
# Step 2: Build Backend
# ========================================
if (-not $SkipBackend) {
    Write-Info "Building .NET backend..."
    Set-Location "$ProjectRoot\Aura.Api"

    # Clean .NET build cache before building
    Write-Info "Cleaning .NET build cache..."
    dotnet clean -c Release --nologo | Out-Null
    Write-Success "  ✓ .NET build cache cleaned"

    # Create backend output directory (must match package.json extraResources path)
    $ResourcesDir = "$ScriptDir\resources"
    $BackendDir = "$ResourcesDir\backend"
    if (-not (Test-Path $BackendDir)) {
        New-Item -ItemType Directory -Path $BackendDir -Force | Out-Null
    }

    if ($Target -eq "win") {
        Write-Info "Building backend for Windows (x64)..."
        Write-Info "This may take several minutes..."
        dotnet publish -c Release -r win-x64 --self-contained true `
            -p:PublishSingleFile=false `
            -p:PublishTrimmed=false `
            -p:IncludeNativeLibrariesForSelfExtract=true `
            -p:SkipFrontendBuild=true `
            -o "$BackendDir\win-x64"
        if ($LASTEXITCODE -ne 0) {
            Show-ErrorMessage "Windows backend build failed with exit code $LASTEXITCODE"
            exit 1
        }

        Write-Success "Windows backend build complete"
    }
    else {
        Show-ErrorMessage "Only Windows builds are supported. Target: $Target"
        exit 1
    }

    Write-Success "Backend builds complete"

    Write-Host ""
}
else {
    Show-Warning "Skipping backend build"
    Write-Host ""
}

# ========================================
# Step 2b: Apply Database Migrations
# ========================================
if (-not $SkipBackend) {
    Write-Info "Applying database migrations..."
    Set-Location "$ProjectRoot\Aura.Api"
    
    # Check if EF tools are installed
    $efTools = dotnet tool list -g | Select-String "dotnet-ef"
    if (-not $efTools) {
        Write-Info "Installing Entity Framework tools..."
        dotnet tool install --global dotnet-ef
        if ($LASTEXITCODE -ne 0) {
            Show-Warning "  Could not install dotnet-ef tools. Database migration check skipped."
        }
        else {
            Write-Success "  ✓ Entity Framework tools installed"
            # Refresh the check after installation
            $efTools = dotnet tool list -g | Select-String "dotnet-ef"
        }
    }
    else {
        Write-Info "Entity Framework tools already installed"
    }
    
    # Only attempt migrations if dotnet-ef is available
    if ($efTools) {
        # Apply migrations (this will create database if missing)
        Write-Info "Checking for pending migrations..."
        try {
            # Use --configuration Release to match the build configuration
            $migrationOutput = dotnet ef database update --configuration Release 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Success "  ✓ Database migrations applied successfully"
                if ($VerbosePreference -eq 'Continue') {
                    Write-Host "  Migration output: $migrationOutput" -ForegroundColor Gray
                }
            } else {
                Show-Warning "  Database migration check skipped (will be created on first run)"
                Write-Host "  Migration output: $migrationOutput" -ForegroundColor Gray
            }
        } catch {
            Show-Warning "  Database migration check skipped (will be created on first run)"
            Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Gray
        }
    }
    
    # Return to script directory
    Set-Location $ScriptDir
    Write-Host ""
}

# ========================================
# Step 2a: Ensure bundled FFmpeg binaries
# ========================================
Write-Info "Ensuring bundled FFmpeg binaries are available..."
& "$ScriptDir\scripts\ensure-ffmpeg.ps1"
if ($LASTEXITCODE -ne 0) {
    Show-ErrorMessage "Failed to prepare bundled FFmpeg binaries"
    exit 1
}
Write-Success "Bundled FFmpeg binaries ready"
Write-Host ""

# ========================================
# Step 3: Install Electron Dependencies
# ========================================
Write-Info "Installing Electron dependencies..."
Set-Location $ScriptDir

if (-not (Test-Path "node_modules")) {
    Write-Info "Installing Electron dependencies (node_modules not found)..."
    npm install
    if ($LASTEXITCODE -ne 0) {
        Show-ErrorMessage "npm install failed with exit code $LASTEXITCODE"
        exit 1
    }
}
else {
    # Verify critical dependencies exist
    $criticalPackages = @("electron", "electron-builder", "electron-store")
    $missingPackages = @()
    
    foreach ($package in $criticalPackages) {
        if (-not (Test-Path "node_modules\$package")) {
            $missingPackages += $package
        }
    }
    
    if ($missingPackages.Count -gt 0) {
        Write-Info "Critical Electron dependencies missing, reinstalling..."
        Write-Info "Missing: $($missingPackages -join ', ')"
        npm install
        if ($LASTEXITCODE -ne 0) {
            Show-ErrorMessage "npm install failed with exit code $LASTEXITCODE"
            exit 1
        }
    } else {
        Write-Info "Electron dependencies verified"
    }
}

Write-Success "Electron dependencies ready"
Write-Host ""

# ========================================
# Step 4: Validate Resources
# ========================================
Write-Info "Validating required resources..."

$RequiredPaths = @(
    @{ Path = "$ProjectRoot\Aura.Web\dist\index.html"; Name = "Frontend build" },
    @{ Path = "$ScriptDir\resources\backend"; Name = "Backend binaries" },
    @{ Path = "$ScriptDir\resources\ffmpeg\win-x64\bin\ffmpeg.exe"; Name = "Bundled FFmpeg" }
)

$ValidationFailed = $false
foreach ($item in $RequiredPaths) {
    if (-not (Test-Path $item.Path)) {
        Show-ErrorMessage "$($item.Name) not found at: $($item.Path)"
        $ValidationFailed = $true
    }
    else {
        Write-Success "  ✓ $($item.Name) found"
    }
}

if ($ValidationFailed) {
    Show-ErrorMessage "Resource validation failed. Cannot build installer."
    Write-Info "Please ensure all build steps complete successfully."
    exit 1
}

Write-Success "All required resources validated"
Write-Host ""

# ========================================
# Step 5: Build Electron Installers
# ========================================
if (-not $SkipInstaller) {
    Write-Info "Building Electron installers..."

    if ($Target -eq "win") {
        Write-Info "Building Windows installer..."
        npm run build:win
        if ($LASTEXITCODE -ne 0) {
            Show-ErrorMessage "Windows installer build failed with exit code $LASTEXITCODE"
            exit 1
        }
    }
    else {
        Show-ErrorMessage "Only Windows builds are supported. Target: $Target"
        exit 1
    }

    Write-Success "Installer build complete"
}
else {
    Show-Warning "Skipping installer creation (building directory only)"
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
