# PowerShell Script to Validate Windows Installation
# This script validates that a Windows installation meets all requirements
# from PR-E2E-002: WINDOWS INSTALLATION & DISTRIBUTION

param(
    [string]$InstallPath = "$env:ProgramFiles\Aura Video Studio",
    [switch]$Help
)

$ErrorActionPreference = "Continue"

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
    Write-Host "[✓] $Message" -ForegroundColor $SuccessColor
}

function Show-Warning {
    param([string]$Message)
    Write-Host "[⚠] $Message" -ForegroundColor $WarningColor
}

function Write-Failure {
    param([string]$Message)
    Write-Host "[✗] $Message" -ForegroundColor $ErrorColor
}

if ($Help) {
    Write-Output "Aura Video Studio - Installation Validation Script"
    Write-Output ""
    Write-Output "Usage: .\validate-installation.ps1 [OPTIONS]"
    Write-Output ""
    Write-Output "Options:"
    Write-Output "  -InstallPath <path>  Path to installation (default: Program Files)"
    Write-Output "  -Help                Show this help message"
    exit 0
}

Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host "Aura Video Studio - Installation Validation" -ForegroundColor $InfoColor
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Output ""

$validationResults = @{
    Passed = 0
    Failed = 0
    Warnings = 0
}

# ========================================
# Check 1: Installation Directory
# ========================================
Write-Info "Checking installation directory..."
if (Test-Path $InstallPath) {
    Write-Success "Installation directory exists: $InstallPath"
    $validationResults.Passed++
} else {
    Write-Failure "Installation directory not found: $InstallPath"
    $validationResults.Failed++
}

# ========================================
# Check 2: Required Files
# ========================================
Write-Info "Checking required files..."
$requiredFiles = @(
    "Aura Video Studio.exe",
    "resources/app.asar",
    "resources/backend/win-x64/Aura.Api.exe"
)

foreach ($file in $requiredFiles) {
    $filePath = Join-Path $InstallPath $file
    if (Test-Path $filePath) {
        $fileSize = (Get-Item $filePath).Length / 1MB
        Write-Success "$file ($('{0:N2}' -f $fileSize) MB)"
        $validationResults.Passed++
    } else {
        Write-Failure "$file - NOT FOUND"
        $validationResults.Failed++
    }
}

# ========================================
# Check 3: FFmpeg Binaries
# ========================================
Write-Info "Checking FFmpeg binaries..."
$ffmpegPath = Join-Path $InstallPath "resources/ffmpeg/win-x64/bin/ffmpeg.exe"
if (Test-Path $ffmpegPath) {
    Write-Success "FFmpeg executable found"
    $validationResults.Passed++

    # Verify FFmpeg works
    try {
        $ffmpegVersion = & $ffmpegPath -version 2>&1 | Select-Object -First 1
        Write-Info "  FFmpeg version: $ffmpegVersion"
    } catch {
        Show-Warning "  Could not verify FFmpeg version"
        $validationResults.Warnings++
    }
} else {
    Write-Failure "FFmpeg executable not found"
    $validationResults.Failed++
}

# ========================================
# Check 4: Frontend Build
# ========================================
Write-Info "Checking frontend build..."
$frontendPath = Join-Path $InstallPath "resources/frontend/index.html"
if (Test-Path $frontendPath) {
    Write-Success "Frontend index.html found"
    $validationResults.Passed++
} else {
    Write-Failure "Frontend index.html not found"
    $validationResults.Failed++
}

# ========================================
# Check 5: .NET 8 Runtime
# ========================================
Write-Info "Checking .NET 8 Runtime..."
try {
    $dotnetRuntimes = dotnet --list-runtimes 2>&1 | Out-String
    if ($dotnetRuntimes -match "Microsoft\.NETCore\.App 8\.") {
        Write-Success ".NET 8 Runtime is installed"
        $validationResults.Passed++
    } else {
        Show-Warning ".NET 8 Runtime not detected (required for backend)"
        $validationResults.Warnings++
    }
} catch {
    Show-Warning ".NET CLI not found - cannot verify runtime"
    $validationResults.Warnings++
}

# ========================================
# Check 6: Registry Entries
# ========================================
Write-Info "Checking registry entries..."

# Uninstall registry
$uninstallKey = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\f6c6e9f0-8b1a-4e5a-9c2d-1e8f4a6b7c8d"
if (Test-Path $uninstallKey) {
    $regData = Get-ItemProperty $uninstallKey
    if ($regData.DisplayName -eq "Aura Video Studio") {
        Write-Success "Uninstall registry entry found"
        $validationResults.Passed++
    } else {
        Show-Warning "Uninstall registry entry exists but DisplayName incorrect"
        $validationResults.Warnings++
    }
} else {
    Write-Failure "Uninstall registry entry not found"
    $validationResults.Failed++
}

# ========================================
# Check 7: File Associations
# ========================================
Write-Info "Checking file associations..."

$fileExtensions = @(".aura", ".avsproj")
foreach ($ext in $fileExtensions) {
    $regPath = "HKLM:\Software\Classes\$ext"
    if (Test-Path $regPath) {
        Write-Success "File association for $ext registered"
        $validationResults.Passed++
    } else {
        Show-Warning "File association for $ext not found in HKLM"

        # Check HKCU as fallback
        $regPathUser = "HKCU:\Software\Classes\$ext"
        if (Test-Path $regPathUser) {
            Write-Info "  Found in HKCU instead"
            $validationResults.Passed++
        } else {
            Write-Failure "File association for $ext not registered"
            $validationResults.Failed++
        }
    }
}

# ========================================
# Check 8: Shortcuts
# ========================================
Write-Info "Checking shortcuts..."

# Desktop shortcut
$desktopShortcut = "$env:USERPROFILE\Desktop\Aura Video Studio.lnk"
if (Test-Path $desktopShortcut) {
    Write-Success "Desktop shortcut found"
    $validationResults.Passed++
} else {
    Show-Warning "Desktop shortcut not found (may be optional)"
    $validationResults.Warnings++
}

# Start Menu shortcut
$startMenuShortcut = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Aura Video Studio.lnk"
if (Test-Path $startMenuShortcut) {
    Write-Success "Start Menu shortcut found"
    $validationResults.Passed++
} else {
    Write-Failure "Start Menu shortcut not found"
    $validationResults.Failed++
}

# ========================================
# Check 9: Windows Firewall Rule
# ========================================
Write-Info "Checking Windows Firewall rule..."
try {
    $firewallRule = Get-NetFirewallRule -DisplayName "Aura Video Studio" -ErrorAction SilentlyContinue
    if ($firewallRule) {
        Write-Success "Windows Firewall rule exists"
        $validationResults.Passed++
    } else {
        Show-Warning "Windows Firewall rule not found (may require manual configuration)"
        $validationResults.Warnings++
    }
} catch {
    Show-Warning "Could not check Windows Firewall (may require admin privileges)"
    $validationResults.Warnings++
}

# ========================================
# Check 10: AppData Directories
# ========================================
Write-Info "Checking AppData directories..."
$appDataPath = "$env:LOCALAPPDATA\aura-video-studio"
if (Test-Path $appDataPath) {
    Write-Success "AppData directory exists: $appDataPath"
    $validationResults.Passed++

    # Check subdirectories
    $subdirs = @("logs", "cache")
    foreach ($subdir in $subdirs) {
        $subdirPath = Join-Path $appDataPath $subdir
        if (Test-Path $subdirPath) {
            Write-Info "  ✓ $subdir directory exists"
        } else {
            Write-Info "  - $subdir directory will be created on first run"
        }
    }
} else {
    Show-Warning "AppData directory not found (will be created on first run)"
    $validationResults.Warnings++
}

# ========================================
# Check 11: Windows Version Compatibility
# ========================================
Write-Info "Checking Windows version compatibility..."
$osInfo = Get-CimInstance Win32_OperatingSystem
$osVersion = [System.Version]$osInfo.Version
$osBuild = $osInfo.BuildNumber

Write-Info "  OS: $($osInfo.Caption)"
Write-Info "  Version: $($osInfo.Version)"
Write-Info "  Build: $osBuild"

if ($osVersion.Major -ge 10) {
    if ($osBuild -ge 22000) {
        Write-Success "Windows 11 detected - Fully compatible"
        $validationResults.Passed++
    } elseif ($osBuild -ge 19041) {
        Write-Success "Windows 10 version 1809+ detected - Compatible"
        $validationResults.Passed++
    } else {
        Show-Warning "Windows 10 build is older than 1809 - May have compatibility issues"
        $validationResults.Warnings++
    }
} else {
    Write-Failure "Windows version not supported (Windows 10+ required)"
    $validationResults.Failed++
}

# ========================================
# Check 12: DLL Dependencies
# ========================================
Write-Info "Checking .NET DLL dependencies..."
$dllPath = Join-Path $InstallPath "resources/backend/win-x64"
if (Test-Path $dllPath) {
    $dllCount = (Get-ChildItem $dllPath -Filter "*.dll" -ErrorAction SilentlyContinue).Count
    if ($dllCount -gt 0) {
        Write-Success "Found $dllCount .NET assemblies"
        $validationResults.Passed++
    } else {
        Write-Failure "No .NET assemblies found in backend directory"
        $validationResults.Failed++
    }
} else {
    Write-Failure "Backend directory not found"
    $validationResults.Failed++
}

# ========================================
# Summary
# ========================================
Write-Output ""
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host "Validation Summary" -ForegroundColor $InfoColor
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host "Passed:   $($validationResults.Passed)" -ForegroundColor $SuccessColor
Write-Host "Failed:   $($validationResults.Failed)" -ForegroundColor $ErrorColor
Write-Host "Warnings: $($validationResults.Warnings)" -ForegroundColor $WarningColor
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Output ""

if ($validationResults.Failed -eq 0) {
    Write-Success "Installation validation PASSED ✓"
    Write-Output ""
    Write-Info "The installation appears to be complete and ready to use."
    Write-Output ""
    exit 0
} else {
    Write-Failure "Installation validation FAILED ✗"
    Write-Output ""
    Write-Info "Please review the failed checks above and reinstall if necessary."
    Write-Output ""
    exit 1
}
