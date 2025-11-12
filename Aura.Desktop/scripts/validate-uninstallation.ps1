# PowerShell Script to Validate Uninstallation Cleanup
# This script checks that uninstallation properly removes all files and registry entries
# from PR-E2E-002: WINDOWS INSTALLATION & DISTRIBUTION

param(
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
    Write-Host "[✓] $Message" -ForegroundColor $SuccessColor
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[⚠] $Message" -ForegroundColor $WarningColor
}

function Write-Failure {
    param([string]$Message)
    Write-Host "[✗] $Message" -ForegroundColor $ErrorColor
}

if ($Help) {
    Write-Host "Aura Video Studio - Uninstallation Validation Script"
    Write-Host ""
    Write-Host "Usage: .\validate-uninstallation.ps1"
    Write-Host ""
    Write-Host "This script verifies that uninstallation properly cleaned up all files,"
    Write-Host "registry entries, and shortcuts."
    exit 0
}

Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host "Aura Video Studio - Uninstallation Validation" -ForegroundColor $InfoColor
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host ""

$cleanupResults = @{
    Cleaned = 0
    Remaining = 0
}

# ========================================
# Check 1: Installation Directory Removed
# ========================================
Write-Info "Checking installation directory..."
$installPath = "$env:ProgramFiles\Aura Video Studio"
if (Test-Path $installPath) {
    Write-Failure "Installation directory still exists: $installPath"
    $cleanupResults.Remaining++
} else {
    Write-Success "Installation directory removed"
    $cleanupResults.Cleaned++
}

# ========================================
# Check 2: Registry Entries Removed
# ========================================
Write-Info "Checking registry entries..."

# Uninstall registry
$uninstallKey = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\f6c6e9f0-8b1a-4e5a-9c2d-1e8f4a6b7c8d"
if (Test-Path $uninstallKey) {
    Write-Failure "Uninstall registry entry still exists"
    $cleanupResults.Remaining++
} else {
    Write-Success "Uninstall registry entry removed"
    $cleanupResults.Cleaned++
}

# File associations (HKLM)
$fileExtensions = @(".aura", ".avsproj")
foreach ($ext in $fileExtensions) {
    $regPath = "HKLM:\Software\Classes\$ext"
    if (Test-Path $regPath) {
        $regValue = (Get-ItemProperty $regPath -ErrorAction SilentlyContinue).'(default)'
        if ($regValue -eq "AuraVideoStudio.Project") {
            Write-Failure "File association for $ext still exists in HKLM"
            $cleanupResults.Remaining++
        } else {
            Write-Success "File association for $ext removed (HKLM)"
            $cleanupResults.Cleaned++
        }
    } else {
        Write-Success "File association for $ext removed (HKLM)"
        $cleanupResults.Cleaned++
    }
    
    # Check HKCU
    $regPathUser = "HKCU:\Software\Classes\$ext"
    if (Test-Path $regPathUser) {
        $regValueUser = (Get-ItemProperty $regPathUser -ErrorAction SilentlyContinue).'(default)'
        if ($regValueUser -eq "AuraVideoStudio.Project") {
            Write-Failure "File association for $ext still exists in HKCU"
            $cleanupResults.Remaining++
        } else {
            Write-Success "File association for $ext removed (HKCU)"
            $cleanupResults.Cleaned++
        }
    } else {
        Write-Success "File association for $ext removed (HKCU)"
        $cleanupResults.Cleaned++
    }
}

# AuraVideoStudio.Project class
$classKeys = @(
    "HKLM:\Software\Classes\AuraVideoStudio.Project",
    "HKCU:\Software\Classes\AuraVideoStudio.Project"
)

foreach ($classKey in $classKeys) {
    if (Test-Path $classKey) {
        Write-Failure "Registry class key still exists: $classKey"
        $cleanupResults.Remaining++
    } else {
        Write-Success "Registry class key removed: $classKey"
        $cleanupResults.Cleaned++
    }
}

# ========================================
# Check 3: Shortcuts Removed
# ========================================
Write-Info "Checking shortcuts..."

# Desktop shortcut
$desktopShortcut = "$env:USERPROFILE\Desktop\Aura Video Studio.lnk"
if (Test-Path $desktopShortcut) {
    Write-Failure "Desktop shortcut still exists"
    $cleanupResults.Remaining++
} else {
    Write-Success "Desktop shortcut removed"
    $cleanupResults.Cleaned++
}

# Start Menu shortcut
$startMenuShortcut = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Aura Video Studio.lnk"
if (Test-Path $startMenuShortcut) {
    Write-Failure "Start Menu shortcut still exists"
    $cleanupResults.Remaining++
} else {
    Write-Success "Start Menu shortcut removed"
    $cleanupResults.Cleaned++
}

# Start Menu folder
$startMenuFolder = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Aura Video Studio"
if (Test-Path $startMenuFolder) {
    Write-Failure "Start Menu folder still exists"
    $cleanupResults.Remaining++
} else {
    Write-Success "Start Menu folder removed"
    $cleanupResults.Cleaned++
}

# ========================================
# Check 4: Windows Firewall Rule Removed
# ========================================
Write-Info "Checking Windows Firewall rule..."
try {
    $firewallRule = Get-NetFirewallRule -DisplayName "Aura Video Studio" -ErrorAction SilentlyContinue
    if ($firewallRule) {
        Write-Failure "Windows Firewall rule still exists"
        $cleanupResults.Remaining++
    } else {
        Write-Success "Windows Firewall rule removed"
        $cleanupResults.Cleaned++
    }
} catch {
    Write-Warning "Could not check Windows Firewall (may require admin privileges)"
}

# ========================================
# Check 5: AppData (Optional - User Choice)
# ========================================
Write-Info "Checking AppData directories..."
$appDataPath = "$env:LOCALAPPDATA\aura-video-studio"
if (Test-Path $appDataPath) {
    Write-Warning "AppData directory still exists (user may have chosen to keep it)"
    Write-Info "  Location: $appDataPath"
    Write-Info "  This is normal if user selected 'No' when asked to remove user data"
} else {
    Write-Success "AppData directory removed (user chose to remove all data)"
    $cleanupResults.Cleaned++
}

# ========================================
# Check 6: Temporary Files
# ========================================
Write-Info "Checking temporary files..."
$tempPath = "$env:TEMP\aura-video-studio"
if (Test-Path $tempPath) {
    Write-Failure "Temporary files still exist: $tempPath"
    $cleanupResults.Remaining++
} else {
    Write-Success "Temporary files removed"
    $cleanupResults.Cleaned++
}

# ========================================
# Check 7: Documents Folder (Should NOT be removed)
# ========================================
Write-Info "Checking user documents..."
$documentsPath = "$env:USERPROFILE\Documents\Aura Video Studio"
if (Test-Path $documentsPath) {
    Write-Success "User documents preserved (as expected)"
    Write-Info "  Location: $documentsPath"
    Write-Info "  User's video projects are safe"
} else {
    Write-Info "User documents folder does not exist (may not have been created)"
}

# ========================================
# Summary
# ========================================
Write-Host ""
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host "Uninstallation Validation Summary" -ForegroundColor $InfoColor
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host "Cleaned:   $($cleanupResults.Cleaned)" -ForegroundColor $SuccessColor
Write-Host "Remaining: $($cleanupResults.Remaining)" -ForegroundColor $ErrorColor
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host ""

if ($cleanupResults.Remaining -eq 0) {
    Write-Success "Uninstallation cleanup PASSED ✓"
    Write-Host ""
    Write-Info "The application was properly uninstalled and all traces removed."
    Write-Host ""
    exit 0
} else {
    Write-Failure "Uninstallation cleanup INCOMPLETE ✗"
    Write-Host ""
    Write-Info "Some files or registry entries were not removed."
    Write-Info "This may require manual cleanup or reinstallation followed by uninstallation."
    Write-Host ""
    exit 1
}
