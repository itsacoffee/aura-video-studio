# PowerShell Script to Validate Windows Build Configuration
# This script validates the Electron build system for Windows

param(
    [switch]$Verbose,
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
    Write-Host "[!] $Message" -ForegroundColor $WarningColor
}

function Write-ErrorMessage {
    param([string]$Message)
    Write-Host "[✗] $Message" -ForegroundColor $ErrorColor
}

if ($Help) {
    Write-Output "Windows Build Validation Script for Aura Video Studio"
    Write-Output ""
    Write-Output "Usage: .\validate-windows-build.ps1 [OPTIONS]"
    Write-Output ""
    Write-Output "Options:"
    Write-Output "  -Verbose    Show detailed validation output"
    Write-Output "  -Help       Show this help message"
    exit 0
}

Write-Output ""
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host "Windows Build System Validation" -ForegroundColor $InfoColor
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Output ""

$ScriptDir = $PSScriptRoot
$DesktopDir = Split-Path $ScriptDir -Parent
$ProjectRoot = Split-Path $DesktopDir -Parent

$ValidationErrors = @()
$ValidationWarnings = @()
$ValidationPassed = 0

# ========================================
# Test 1: Check Node.js Installation
# ========================================
Write-Info "Checking Node.js installation..."

try {
    $nodeVersion = node --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        $versionNumber = $nodeVersion -replace 'v', ''
        $majorVersion = [int]($versionNumber -split '\.')[0]

        if ($majorVersion -ge 18) {
            Write-Success "Node.js $nodeVersion (minimum v18 required)"
            $ValidationPassed++
        } else {
            Write-ErrorMessage "Node.js version too old: $nodeVersion (minimum v18 required)"
            $ValidationErrors += "Node.js version $nodeVersion is too old"
        }
    }
} catch {
    Write-ErrorMessage "Node.js is not installed"
    $ValidationErrors += "Node.js is not installed"
}

# ========================================
# Test 2: Check npm Installation
# ========================================
Write-Info "Checking npm installation..."

try {
    $npmVersion = npm --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "npm $npmVersion"
        $ValidationPassed++
    }
} catch {
    Write-ErrorMessage "npm is not installed"
    $ValidationErrors += "npm is not installed"
}

# ========================================
# Test 3: Check .NET SDK Installation
# ========================================
Write-Info "Checking .NET SDK installation..."

try {
    $dotnetVersion = dotnet --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        $majorVersion = [int]($dotnetVersion -split '\.')[0]

        if ($majorVersion -ge 8) {
            Write-Success ".NET SDK $dotnetVersion (minimum 8.0 required)"
            $ValidationPassed++
        } else {
            Write-ErrorMessage ".NET SDK version too old: $dotnetVersion (minimum 8.0 required)"
            $ValidationErrors += ".NET SDK version $dotnetVersion is too old"
        }
    }
} catch {
    Write-ErrorMessage ".NET SDK is not installed"
    $ValidationErrors += ".NET SDK 8.0+ is not installed"
}

# ========================================
# Test 4: Check PowerShell Version
# ========================================
Write-Info "Checking PowerShell version..."

$psVersion = $PSVersionTable.PSVersion
if ($psVersion.Major -ge 5) {
    Write-Success "PowerShell $($psVersion.ToString())"
    $ValidationPassed++
} else {
    Write-ErrorMessage "PowerShell version too old: $($psVersion.ToString()) (minimum 5.0 required)"
    $ValidationErrors += "PowerShell version is too old"
}

# ========================================
# Test 5: Check package.json Configuration
# ========================================
Write-Info "Validating package.json configuration..."

$packageJsonPath = Join-Path $DesktopDir "package.json"
if (Test-Path $packageJsonPath) {
    try {
        $packageJson = Get-Content $packageJsonPath -Raw | ConvertFrom-Json

        # Check build configuration exists
        if ($packageJson.build) {
            Write-Success "Build configuration found in package.json"
            $ValidationPassed++

            # Check NSIS configuration
            if ($packageJson.build.nsis) {
                Write-Success "NSIS installer configuration found"
                $ValidationPassed++

                # Check Windows 11 specific settings
                if ($packageJson.build.nsis.packElevateHelper -eq $true) {
                    Write-Success "Elevation helper configured for Windows 11"
                    $ValidationPassed++
                } else {
                    Write-Warning "Elevation helper not configured"
                    $ValidationWarnings += "packElevateHelper should be enabled"
                }

                if ($packageJson.build.nsis.unicode -eq $true) {
                    Write-Success "Unicode support enabled"
                    $ValidationPassed++
                } else {
                    Write-Warning "Unicode support not enabled"
                    $ValidationWarnings += "unicode should be enabled for Windows 11"
                }
            } else {
                Write-ErrorMessage "NSIS configuration missing"
                $ValidationErrors += "NSIS configuration not found in package.json"
            }

            # Check Windows target
            if ($packageJson.build.win) {
                Write-Success "Windows target configuration found"
                $ValidationPassed++

                # Check architecture
                $allX64 = $true
                foreach ($target in $packageJson.build.win.target) {
                    if ($target.arch -notcontains "x64") {
                        $allX64 = $false
                    }
                }

                if ($allX64) {
                    Write-Success "All targets configured for x64 architecture"
                    $ValidationPassed++
                } else {
                    Write-ErrorMessage "Not all targets are x64"
                    $ValidationErrors += "All Windows targets must be x64"
                }
            } else {
                Write-ErrorMessage "Windows build configuration missing"
                $ValidationErrors += "Windows build configuration not found"
            }
        } else {
            Write-ErrorMessage "Build configuration missing from package.json"
            $ValidationErrors += "Build configuration not found in package.json"
        }
    } catch {
        Write-ErrorMessage "Failed to parse package.json: $($_.Exception.Message)"
        $ValidationErrors += "Invalid package.json format"
    }
} else {
    Write-ErrorMessage "package.json not found at: $packageJsonPath"
    $ValidationErrors += "package.json not found"
}

# ========================================
# Test 6: Check NSIS Installer Script
# ========================================
Write-Info "Checking NSIS installer script..."

$nsisScriptPath = Join-Path $DesktopDir "build\installer.nsh"
if (Test-Path $nsisScriptPath) {
    $nsisContent = Get-Content $nsisScriptPath -Raw

    if ($nsisContent -match "RequestExecutionLevel admin") {
        Write-Success "NSIS script requests admin elevation"
        $ValidationPassed++
    } else {
        Write-Warning "NSIS script does not request elevation"
        $ValidationWarnings += "NSIS script should request admin elevation"
    }

    if ($nsisContent -match "HKLM") {
        Write-Success "NSIS script writes to HKLM registry (machine-wide)"
        $ValidationPassed++
    }

    if ($nsisContent -match "Windows 11") {
        Write-Success "NSIS script includes Windows 11 compatibility"
        $ValidationPassed++
    }
} else {
    Write-ErrorMessage "NSIS installer script not found: $nsisScriptPath"
    $ValidationErrors += "NSIS installer script missing"
}

# ========================================
# Test 7: Check Icon Files
# ========================================
Write-Info "Checking icon files..."

$iconPath = Join-Path $DesktopDir "assets\icons\icon.ico"
if (Test-Path $iconPath) {
    Write-Success "Application icon found"
    $ValidationPassed++
} else {
    Write-ErrorMessage "Application icon not found: $iconPath"
    $ValidationErrors += "icon.ico missing"
}

$headerBmpPath = Join-Path $DesktopDir "assets\icons\installer-header.bmp"
if (Test-Path $headerBmpPath) {
    Write-Success "Installer header image found"
    $ValidationPassed++
} else {
    Write-Warning "Installer header image not found: $headerBmpPath"
    $ValidationWarnings += "installer-header.bmp recommended"
}

$sidebarBmpPath = Join-Path $DesktopDir "assets\icons\installer-sidebar.bmp"
if (Test-Path $sidebarBmpPath) {
    Write-Success "Installer sidebar image found"
    $ValidationPassed++
} else {
    Write-Warning "Installer sidebar image not found: $sidebarBmpPath"
    $ValidationWarnings += "installer-sidebar.bmp recommended"
}

# ========================================
# Test 8: Check Build Scripts
# ========================================
Write-Info "Checking build scripts..."

$buildScripts = @(
    "build-backend-windows.ps1",
    "download-ffmpeg-windows.ps1",
    "sign-windows.js",
    "validate-build-config.js"
)

foreach ($script in $buildScripts) {
    $scriptPath = Join-Path $ScriptDir $script
    if (Test-Path $scriptPath) {
        Write-Success "Found: $script"
        $ValidationPassed++
    } else {
        Write-ErrorMessage "Missing: $script"
        $ValidationErrors += "$script not found"
    }
}

# ========================================
# Test 9: Check Backend Project
# ========================================
Write-Info "Checking backend project..."

$backendCsproj = Join-Path $ProjectRoot "Aura.Api\Aura.Api.csproj"
if (Test-Path $backendCsproj) {
    Write-Success "Backend project found"
    $ValidationPassed++
} else {
    Write-ErrorMessage "Backend project not found: $backendCsproj"
    $ValidationErrors += "Aura.Api.csproj not found"
}

# ========================================
# Test 10: Check Frontend Project
# ========================================
Write-Info "Checking frontend project..."

$frontendPackageJson = Join-Path $ProjectRoot "Aura.Web\package.json"
if (Test-Path $frontendPackageJson) {
    Write-Success "Frontend project found"
    $ValidationPassed++
} else {
    Write-ErrorMessage "Frontend project not found: $frontendPackageJson"
    $ValidationErrors += "Aura.Web/package.json not found"
}

# ========================================
# Test 11: Check Windows SDK (for code signing)
# ========================================
Write-Info "Checking Windows SDK (for code signing)..."

$signtoolPaths = @(
    "C:\Program Files (x86)\Windows Kits\10\bin\x64\signtool.exe",
    "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe",
    "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22000.0\x64\signtool.exe"
)

$signtoolFound = $false
foreach ($path in $signtoolPaths) {
    if (Test-Path $path) {
        Write-Success "signtool.exe found at: $path"
        $signtoolFound = $true
        $ValidationPassed++
        break
    }
}

if (-not $signtoolFound) {
    Write-Warning "signtool.exe not found - code signing will be disabled"
    Write-Warning "  Install Windows SDK to enable code signing"
    $ValidationWarnings += "Windows SDK not installed (optional for code signing)"
}

# ========================================
# Test 12: Test npm dependencies
# ========================================
Write-Info "Checking npm dependencies..."

Push-Location $DesktopDir
try {
    $npmList = npm list --depth=0 2>&1 | Out-String

    if ($npmList -match "UNMET DEPENDENCY") {
        Write-ErrorMessage "npm dependencies are not installed"
        Write-Info "Run 'npm install' to install dependencies"
        $ValidationErrors += "npm dependencies not installed"
    } else {
        Write-Success "npm dependencies are installed"
        $ValidationPassed++
    }
} catch {
    Write-Warning "Could not check npm dependencies"
}
Pop-Location

# ========================================
# Summary
# ========================================
Write-Output ""
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host "Validation Summary" -ForegroundColor $InfoColor
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Output ""

Write-Host "Tests Passed: $ValidationPassed" -ForegroundColor $SuccessColor

if ($ValidationWarnings.Count -gt 0) {
    Write-Host "Warnings: $($ValidationWarnings.Count)" -ForegroundColor $WarningColor
    if ($Verbose) {
        foreach ($warning in $ValidationWarnings) {
            Write-Host "  - $warning" -ForegroundColor $WarningColor
        }
    }
}

if ($ValidationErrors.Count -gt 0) {
    Write-Host "Errors: $($ValidationErrors.Count)" -ForegroundColor $ErrorColor
    Write-Output ""
    Write-Host "Errors found:" -ForegroundColor $ErrorColor
    foreach ($validationError in $ValidationErrors) {
        Write-Host "  - $validationError" -ForegroundColor $ErrorColor
    }
    Write-Output ""
    Write-Host "Please fix the errors above before building." -ForegroundColor $ErrorColor
    exit 1
} else {
    Write-Output ""
    Write-Host "========================================" -ForegroundColor $SuccessColor
    Write-Host "✓ All Critical Validations Passed!" -ForegroundColor $SuccessColor
    Write-Host "========================================" -ForegroundColor $SuccessColor
    Write-Output ""
    Write-Host "The Windows build system is properly configured." -ForegroundColor $SuccessColor
    Write-Output ""
    Write-Host "Next steps:" -ForegroundColor $InfoColor
    Write-Host "  1. Install dependencies: npm install" -ForegroundColor $InfoColor
    Write-Host "  2. Build backend: .\scripts\build-backend-windows.ps1" -ForegroundColor $InfoColor
    Write-Host "  3. Download FFmpeg: .\scripts\download-ffmpeg-windows.ps1" -ForegroundColor $InfoColor
    Write-Host "  4. Build installer: npm run build:win" -ForegroundColor $InfoColor
    Write-Output ""
    exit 0
}
