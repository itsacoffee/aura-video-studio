# End-to-End Installation Test Script
# This script performs automated installation testing for PR-E2E-002
# Run this in a Windows 11 VM or clean test environment

param(
    [string]$InstallerPath,
    [switch]$Silent,
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
    Write-Host "Aura Video Studio - End-to-End Installation Test"
    Write-Host ""
    Write-Host "Usage: .\test-installation-e2e.ps1 -InstallerPath <path> [-Silent]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -InstallerPath <path>  Path to the installer executable"
    Write-Host "  -Silent                Run installer in silent mode (automated testing)"
    Write-Host "  -Help                  Show this help message"
    Write-Host ""
    Write-Host "Example:"
    Write-Host "  .\test-installation-e2e.ps1 -InstallerPath C:\Temp\Aura-Video-Studio-Setup-1.0.0.exe -Silent"
    exit 0
}

if (-not $InstallerPath) {
    Write-Failure "Installer path is required"
    Write-Host ""
    Write-Host "Usage: .\test-installation-e2e.ps1 -InstallerPath <path>"
    Write-Host "Run with -Help for more information"
    exit 1
}

if (-not (Test-Path $InstallerPath)) {
    Write-Failure "Installer not found: $InstallerPath"
    exit 1
}

Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host "Aura Video Studio - E2E Installation Test" -ForegroundColor $InfoColor
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host ""

$testResults = @{
    Phase = ""
    Passed = 0
    Failed = 0
    Warnings = 0
}

# ========================================
# Phase 1: Pre-Installation Checks
# ========================================
$testResults.Phase = "Pre-Installation"
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host "Phase 1: Pre-Installation Checks" -ForegroundColor $InfoColor
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host ""

Write-Info "Checking installer file..."
$installerFile = Get-Item $InstallerPath
$installerSizeMB = $installerFile.Length / 1MB
Write-Info "  Size: $('{0:N2}' -f $installerSizeMB) MB"

if ($installerSizeMB -lt 50) {
    Write-Warning "Installer seems small (expected 200-400MB)"
    $testResults.Warnings++
} else {
    Write-Success "Installer size is reasonable"
    $testResults.Passed++
}

Write-Info "Computing installer checksum..."
$installerHash = Get-FileHash -Path $InstallerPath -Algorithm SHA256
Write-Info "  SHA256: $($installerHash.Hash)"

Write-Info "Checking if application is already installed..."
$installPath = "$env:ProgramFiles\Aura Video Studio"
if (Test-Path $installPath) {
    Write-Warning "Application appears to be already installed at: $installPath"
    Write-Info "  Please uninstall before running E2E test"
    $testResults.Warnings++
} else {
    Write-Success "No existing installation detected"
    $testResults.Passed++
}

Write-Info "Checking .NET 8 Runtime..."
try {
    $dotnetRuntimes = dotnet --list-runtimes 2>&1 | Out-String
    if ($dotnetRuntimes -match "Microsoft\.NETCore\.App 8\.") {
        Write-Success ".NET 8 Runtime is installed"
        $testResults.Passed++
    } else {
        Write-Warning ".NET 8 Runtime not detected - installer should prompt for it"
        $testResults.Warnings++
    }
} catch {
    Write-Warning ".NET CLI not found - installer will need to handle this"
    $testResults.Warnings++
}

Write-Info "Checking Windows version..."
$osInfo = Get-CimInstance Win32_OperatingSystem
$osBuild = $osInfo.BuildNumber
Write-Info "  OS: $($osInfo.Caption)"
Write-Info "  Build: $osBuild"

if ($osBuild -ge 22000) {
    Write-Success "Windows 11 detected - Fully compatible"
    $testResults.Passed++
} elseif ($osBuild -ge 19041) {
    Write-Success "Windows 10 (1809+) detected - Compatible"
    $testResults.Passed++
} else {
    Write-Failure "Windows version may not be supported"
    $testResults.Failed++
}

# ========================================
# Phase 2: Installation
# ========================================
$testResults.Phase = "Installation"
Write-Host ""
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host "Phase 2: Installation" -ForegroundColor $InfoColor
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host ""

Write-Info "Starting installer..."
if ($Silent) {
    Write-Info "  Mode: Silent (automated)"
    Write-Info "  Running installer with /S flag..."
    
    try {
        $installProcess = Start-Process -FilePath $InstallerPath -ArgumentList "/S" -Wait -PassThru
        
        if ($installProcess.ExitCode -eq 0) {
            Write-Success "Installer completed successfully"
            $testResults.Passed++
        } else {
            Write-Failure "Installer failed with exit code: $($installProcess.ExitCode)"
            $testResults.Failed++
            exit 1
        }
    } catch {
        Write-Failure "Failed to run installer: $_"
        $testResults.Failed++
        exit 1
    }
    
    Write-Info "Waiting for installation to settle..."
    Start-Sleep -Seconds 10
    
} else {
    Write-Info "  Mode: Interactive"
    Write-Info "  Please complete the installation manually..."
    Write-Host ""
    Write-Host "  Installation Checklist:"
    Write-Host "    1. Accept license agreement"
    Write-Host "    2. Choose installation directory (default recommended)"
    Write-Host "    3. If prompted, install .NET 8 Runtime"
    Write-Host "    4. Complete installation"
    Write-Host "    5. Choose whether to launch application"
    Write-Host ""
    Write-Host "  Press Enter when installation is complete..."
    Read-Host
}

# ========================================
# Phase 3: Post-Installation Validation
# ========================================
$testResults.Phase = "Post-Installation"
Write-Host ""
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host "Phase 3: Post-Installation Validation" -ForegroundColor $InfoColor
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host ""

Write-Info "Running comprehensive installation validation..."
$validationScript = Join-Path $PSScriptRoot "validate-installation.ps1"

if (Test-Path $validationScript) {
    & $validationScript
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Installation validation passed"
        $testResults.Passed++
    } else {
        Write-Failure "Installation validation failed"
        $testResults.Failed++
    }
} else {
    Write-Warning "Validation script not found, performing basic checks..."
    
    # Basic validation
    if (Test-Path $installPath) {
        Write-Success "Installation directory exists"
        $testResults.Passed++
    } else {
        Write-Failure "Installation directory not found"
        $testResults.Failed++
    }
}

# ========================================
# Phase 4: Application Launch Test (Optional)
# ========================================
if (-not $Silent) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor $InfoColor
    Write-Host "Phase 4: Application Launch Test" -ForegroundColor $InfoColor
    Write-Host "========================================" -ForegroundColor $InfoColor
    Write-Host ""
    
    Write-Info "Would you like to test launching the application? (Y/N)"
    $launchTest = Read-Host
    
    if ($launchTest -eq "Y" -or $launchTest -eq "y") {
        Write-Info "Launching application..."
        
        $exePath = Join-Path $installPath "Aura Video Studio.exe"
        if (Test-Path $exePath) {
            try {
                Start-Process $exePath
                Write-Success "Application launched"
                Write-Info "  Please verify the application starts correctly"
                Write-Info "  Press Enter when done..."
                Read-Host
                $testResults.Passed++
            } catch {
                Write-Failure "Failed to launch application: $_"
                $testResults.Failed++
            }
        } else {
            Write-Failure "Application executable not found"
            $testResults.Failed++
        }
    }
}

# ========================================
# Summary
# ========================================
Write-Host ""
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host "E2E Installation Test Summary" -ForegroundColor $InfoColor
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host "Installer: $($installerFile.Name)"
Write-Host "Checksum:  $($installerHash.Hash)"
Write-Host ""
Write-Host "Test Results:" -ForegroundColor $InfoColor
Write-Host "  Passed:   $($testResults.Passed)" -ForegroundColor $SuccessColor
Write-Host "  Failed:   $($testResults.Failed)" -ForegroundColor $ErrorColor
Write-Host "  Warnings: $($testResults.Warnings)" -ForegroundColor $WarningColor
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host ""

if ($testResults.Failed -eq 0) {
    Write-Success "E2E Installation Test PASSED ✓"
    Write-Host ""
    Write-Info "Next steps:"
    Write-Host "  1. Test application features"
    Write-Host "  2. Test uninstallation (run validate-uninstallation.ps1)"
    Write-Host "  3. Verify clean uninstall"
    Write-Host ""
    exit 0
} else {
    Write-Failure "E2E Installation Test FAILED ✗"
    Write-Host ""
    Write-Info "Please review the failed checks and retry installation."
    Write-Host ""
    exit 1
}
