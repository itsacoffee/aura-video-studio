# Master Build Script for Aura Video Studio Windows Installer
# This script orchestrates the complete build process for production Windows installers

param(
    [switch]$SkipFrontend,
    [switch]$SkipBackend,
    [switch]$SkipFFmpeg,
    [switch]$SkipInstaller,
    [switch]$Clean,
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

function Write-ErrorMessage {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor $ErrorColor
}

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "========================================" -ForegroundColor $InfoColor
    Write-Host $Message -ForegroundColor $InfoColor
    Write-Host "========================================" -ForegroundColor $InfoColor
    Write-Host ""
}

if ($Help) {
    Write-Host "Aura Video Studio - Master Windows Build Script"
    Write-Host ""
    Write-Host "Usage: .\build-windows.ps1 [OPTIONS]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -SkipFrontend    Skip React frontend build"
    Write-Host "  -SkipBackend     Skip .NET backend build"
    Write-Host "  -SkipFFmpeg      Skip FFmpeg download (use existing)"
    Write-Host "  -SkipInstaller   Build app but skip installer creation"
    Write-Host "  -Clean           Clean all previous build artifacts"
    Write-Host "  -Help            Show this help message"
    Write-Host ""
    Write-Host "Example:"
    Write-Host "  .\build-windows.ps1              # Full build"
    Write-Host "  .\build-windows.ps1 -Clean       # Clean build from scratch"
    Write-Host "  .\build-windows.ps1 -SkipFFmpeg  # Quick rebuild (skip FFmpeg download)"
    exit 0
}

# Start timer
$buildStartTime = Get-Date

Write-Host ""
Write-Host "========================================" -ForegroundColor $SuccessColor
Write-Host "Aura Video Studio" -ForegroundColor $SuccessColor
Write-Host "Windows Production Build" -ForegroundColor $SuccessColor
Write-Host "========================================" -ForegroundColor $SuccessColor
Write-Host ""
Write-Info "Build started: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host ""

$ScriptDir = $PSScriptRoot
$DesktopDir = Split-Path $ScriptDir -Parent
$ProjectRoot = Split-Path $DesktopDir -Parent

# ========================================
# STEP 1: Validate Prerequisites
# ========================================
Write-Step "STEP 1: Validating Prerequisites"

$prerequisites = @{
    "Node.js" = { node --version }
    "npm" = { npm --version }
    ".NET SDK" = { dotnet --version }
}

$allPrereqsMet = $true

foreach ($prereq in $prerequisites.GetEnumerator()) {
    try {
        $version = & $prereq.Value 2>&1
        Write-Success "  ✓ $($prereq.Key): $version"
    } catch {
        Write-ErrorMessage "  ❌ $($prereq.Key) not found"
        $allPrereqsMet = $false
    }
}

if (-not $allPrereqsMet) {
    Write-Host ""
    Write-ErrorMessage "Missing required prerequisites. Please install:"
    Write-Host "  - Node.js 18+ from https://nodejs.org/"
    Write-Host "  - .NET 8.0 SDK from https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
}

# Check electron-builder
try {
    Set-Location $DesktopDir
    $ebVersion = npm list electron-builder --depth=0 2>&1
    if ($ebVersion -match "electron-builder@") {
        Write-Success "  ✓ electron-builder installed"
    }
} catch {
    Write-Warning "  electron-builder not found - will be installed"
}

Write-Success "All prerequisites met"

# ========================================
# STEP 2: Clean Previous Builds (if requested)
# ========================================
if ($Clean) {
    Write-Step "STEP 2: Cleaning Previous Builds"
    
    $cleanPaths = @(
        @{ Path = "$DesktopDir\dist"; Description = "Electron installers" },
        @{ Path = "$DesktopDir\resources\backend"; Description = "Backend builds" },
        @{ Path = "$DesktopDir\resources\ffmpeg"; Description = "FFmpeg binaries" },
        @{ Path = "$ProjectRoot\Aura.Web\dist"; Description = "Frontend build" },
        @{ Path = "$DesktopDir\temp"; Description = "Temporary files" }
    )
    
    foreach ($item in $cleanPaths) {
        if (Test-Path $item.Path) {
            Remove-Item -Path $item.Path -Recurse -Force
            Write-Info "  Cleaned: $($item.Description)"
        }
    }
    
    Write-Success "Clean complete"
} else {
    Write-Info "STEP 2: Clean (skipped - use -Clean to enable)"
}

# ========================================
# STEP 3: Download FFmpeg
# ========================================
if (-not $SkipFFmpeg) {
    Write-Step "STEP 3: Downloading FFmpeg"
    
    $ffmpegScript = Join-Path $ScriptDir "download-ffmpeg-windows.ps1"
    
    if (Test-Path $ffmpegScript) {
        & $ffmpegScript
        
        if ($LASTEXITCODE -ne 0) {
            Write-ErrorMessage "FFmpeg download failed"
            exit 1
        }
    } else {
        Write-ErrorMessage "FFmpeg download script not found: $ffmpegScript"
        exit 1
    }
    
    Write-Success "FFmpeg ready"
} else {
    Write-Info "STEP 3: FFmpeg (skipped - using existing)"
    
    # Verify FFmpeg exists
    $ffmpegExe = "$DesktopDir\resources\ffmpeg\win-x64\bin\ffmpeg.exe"
    if (-not (Test-Path $ffmpegExe)) {
        Write-ErrorMessage "FFmpeg not found at: $ffmpegExe"
        Write-Info "Run without -SkipFFmpeg to download it"
        exit 1
    }
}

# ========================================
# STEP 4: Build Frontend
# ========================================
if (-not $SkipFrontend) {
    Write-Step "STEP 4: Building React Frontend"
    
    $webDir = Join-Path $ProjectRoot "Aura.Web"
    Set-Location $webDir
    
    # Install dependencies
    if (-not (Test-Path "node_modules") -or $Clean) {
        Write-Info "Installing frontend dependencies..."
        npm ci
        
        if ($LASTEXITCODE -ne 0) {
            Write-ErrorMessage "Frontend dependency installation failed"
            exit 1
        }
    }
    
    # Build frontend
    Write-Info "Building production frontend..."
    npm run build
    
    if ($LASTEXITCODE -ne 0) {
        Write-ErrorMessage "Frontend build failed"
        exit 1
    }
    
    # Verify output
    $indexHtml = Join-Path $webDir "dist\index.html"
    if (-not (Test-Path $indexHtml)) {
        Write-ErrorMessage "Frontend build failed - index.html not found"
        exit 1
    }
    
    Write-Success "Frontend build complete"
} else {
    Write-Info "STEP 4: Frontend (skipped)"
    
    # Verify frontend exists
    $indexHtml = "$ProjectRoot\Aura.Web\dist\index.html"
    if (-not (Test-Path $indexHtml)) {
        Write-ErrorMessage "Frontend not found at: $indexHtml"
        Write-Info "Run without -SkipFrontend to build it"
        exit 1
    }
}

# ========================================
# STEP 5: Build Backend
# ========================================
if (-not $SkipBackend) {
    Write-Step "STEP 5: Building .NET Backend"
    
    $backendScript = Join-Path $ScriptDir "build-backend-windows.ps1"
    
    if (Test-Path $backendScript) {
        if ($Clean) {
            & $backendScript -Clean
        } else {
            & $backendScript
        }
        
        if ($LASTEXITCODE -ne 0) {
            Write-ErrorMessage "Backend build failed"
            exit 1
        }
    } else {
        Write-ErrorMessage "Backend build script not found: $backendScript"
        exit 1
    }
    
    Write-Success "Backend build complete"
} else {
    Write-Info "STEP 5: Backend (skipped)"
    
    # Verify backend exists
    $backendExe = "$DesktopDir\resources\backend\win-x64\Aura.Api.exe"
    if (-not (Test-Path $backendExe)) {
        Write-ErrorMessage "Backend not found at: $backendExe"
        Write-Info "Run without -SkipBackend to build it"
        exit 1
    }
}

# ========================================
# STEP 6: Build Electron App
# ========================================
Write-Step "STEP 6: Building Electron Application"

Set-Location $DesktopDir

# Install/update dependencies
Write-Info "Installing Electron dependencies..."
npm ci

if ($LASTEXITCODE -ne 0) {
    Write-ErrorMessage "Electron dependency installation failed"
    exit 1
}

# Build installer
if (-not $SkipInstaller) {
    Write-Info "Building Windows installer..."
    Write-Warning "This may take 10-15 minutes..."
    Write-Host ""
    
    npm run build:win
    
    if ($LASTEXITCODE -ne 0) {
        Write-ErrorMessage "Electron build failed"
        exit 1
    }
    
    Write-Success "Installer build complete"
} else {
    Write-Info "Building unpacked app (no installer)..."
    npm run build:dir
    
    if ($LASTEXITCODE -ne 0) {
        Write-ErrorMessage "Electron build failed"
        exit 1
    }
    
    Write-Success "App build complete"
}

# ========================================
# STEP 7: Generate Checksums
# ========================================
Write-Step "STEP 7: Generating Checksums"

$distDir = Join-Path $DesktopDir "dist"

if (Test-Path $distDir) {
    $installers = Get-ChildItem -Path $distDir -Filter "*.exe" -File
    
    if ($installers.Count -gt 0) {
        $checksumFile = Join-Path $distDir "checksums.txt"
        $checksumContent = @()
        
        foreach ($installer in $installers) {
            Write-Info "  Computing SHA256 for: $($installer.Name)"
            $hash = Get-FileHash -Path $installer.FullName -Algorithm SHA256
            $checksumContent += "$($hash.Hash)  $($installer.Name)"
        }
        
        $checksumContent -join "`n" | Set-Content -Path $checksumFile -Encoding UTF8
        Write-Success "  Checksums saved to: checksums.txt"
        
        Write-Host ""
        Write-Info "SHA256 Checksums:"
        foreach ($line in $checksumContent) {
            Write-Host "  $line"
        }
    } else {
        Write-Warning "No installer files found in dist directory"
    }
}

Write-Success "Checksums generated"

# ========================================
# STEP 8: Build Summary
# ========================================
Write-Step "STEP 8: Build Summary"

$buildEndTime = Get-Date
$buildDuration = $buildEndTime - $buildStartTime
$durationFormatted = "{0:D2}:{1:D2}:{2:D2}" -f $buildDuration.Hours, $buildDuration.Minutes, $buildDuration.Seconds

Write-Info "Build completed: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Info "Total duration: $durationFormatted"
Write-Host ""

if (Test-Path $distDir) {
    Write-Info "Output location:"
    Write-Host "  $distDir"
    Write-Host ""
    
    Write-Info "Generated files:"
    $allFiles = Get-ChildItem -Path $distDir -File
    foreach ($file in $allFiles) {
        $sizeMB = [math]::Round($file.Length / 1MB, 2)
        Write-Host "  $($file.Name) ($sizeMB MB)"
    }
    Write-Host ""
}

# Next steps
Write-Info "Next steps:"
Write-Host "  1. Test the installer on a clean Windows VM"
Write-Host "  2. Verify all features work after installation"
Write-Host "  3. Sign the installer (if you have a code signing certificate)"
Write-Host "  4. Upload to GitHub releases"
Write-Host ""

# ========================================
# Success!
# ========================================
Write-Host "========================================" -ForegroundColor $SuccessColor
Write-Host "Build Complete! 🎉" -ForegroundColor $SuccessColor
Write-Host "========================================" -ForegroundColor $SuccessColor
Write-Host ""

exit 0
