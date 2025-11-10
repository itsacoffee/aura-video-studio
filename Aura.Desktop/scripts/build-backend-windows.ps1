# PowerShell Script to Build Self-Contained .NET Backend for Windows
# This script builds the ASP.NET Core backend as a self-contained deployment

param(
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

if ($Help) {
    Write-Host "Backend Build Script for Aura Video Studio"
    Write-Host ""
    Write-Host "Usage: .\build-backend-windows.ps1 [OPTIONS]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Clean    Clean build (remove existing build artifacts first)"
    Write-Host "  -Help     Show this help message"
    exit 0
}

Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host ".NET Backend Build (Self-Contained)" -ForegroundColor $InfoColor
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host ""

# Check if dotnet is installed
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-ErrorMessage ".NET 8.0 SDK is not installed"
    Write-Host ""
    Write-Info "Please install .NET 8.0 SDK from:"
    Write-Info "  https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
}

# Display .NET version
$dotnetVersion = dotnet --version
Write-Info ".NET SDK version: $dotnetVersion"
Write-Host ""

$ScriptDir = $PSScriptRoot
$DesktopDir = Split-Path $ScriptDir -Parent
$ProjectRoot = Split-Path $DesktopDir -Parent
$BackendProject = Join-Path $ProjectRoot "Aura.Api"
$BackendCsproj = Join-Path $BackendProject "Aura.Api.csproj"
$ResourcesDir = Join-Path $DesktopDir "resources"
$BackendOutputDir = Join-Path $ResourcesDir "backend\win-x64"

# Verify backend project exists
if (-not (Test-Path $BackendCsproj)) {
    Write-ErrorMessage "Backend project not found at: $BackendCsproj"
    exit 1
}

Write-Info "Backend project: $BackendProject"
Write-Info "Output directory: $BackendOutputDir"
Write-Host ""

# ========================================
# Step 1: Clean (if requested)
# ========================================
if ($Clean) {
    Write-Info "Cleaning previous build artifacts..."
    
    if (Test-Path $BackendOutputDir) {
        Remove-Item -Path $BackendOutputDir -Recurse -Force
        Write-Info "  Removed: $BackendOutputDir"
    }
    
    # Clean project
    Set-Location $BackendProject
    dotnet clean -c Release | Out-Null
    
    Write-Success "Clean complete"
    Write-Host ""
}

# ========================================
# Step 2: Create Output Directory
# ========================================
Write-Info "Creating output directory..."

$directories = @($ResourcesDir, "$ResourcesDir\backend", $BackendOutputDir)
foreach ($dir in $directories) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Info "  Created: $dir"
    }
}

Write-Success "Output directory ready"
Write-Host ""

# ========================================
# Step 3: Restore Dependencies
# ========================================
Write-Info "Restoring NuGet packages..."
Set-Location $BackendProject

try {
    dotnet restore
    if ($LASTEXITCODE -ne 0) {
        throw "Restore failed with exit code $LASTEXITCODE"
    }
    Write-Success "Restore complete"
    Write-Host ""
} catch {
    Write-ErrorMessage "Failed to restore packages: $($_.Exception.Message)"
    exit 1
}

# ========================================
# Step 4: Build Self-Contained Backend
# ========================================
Write-Info "Building self-contained backend for Windows x64..."
Write-Info "Configuration: Release"
Write-Info "Runtime: win-x64"
Write-Info "Self-contained: Yes"
Write-Info "Single file: Yes"
Write-Host ""
Write-Warning "This may take several minutes on first build..."
Write-Host ""

$publishArgs = @(
    "publish",
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", "true",
    "-o", "`"$BackendOutputDir`"",
    "-p:PublishSingleFile=true",
    "-p:PublishTrimmed=false",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:EnableCompressionInSingleFile=true",
    "-p:PublishReadyToRun=true",
    "-p:RuntimeIdentifier=win-x64",
    "-p:DebugType=none",
    "-p:DebugSymbols=false",
    "--nologo"
)

try {
    $process = Start-Process -FilePath "dotnet" -ArgumentList $publishArgs -NoNewWindow -Wait -PassThru
    
    if ($process.ExitCode -ne 0) {
        throw "Build failed with exit code $($process.ExitCode)"
    }
    
    Write-Success "Build complete"
    Write-Host ""
} catch {
    Write-ErrorMessage "Failed to build backend: $($_.Exception.Message)"
    exit 1
}

# ========================================
# Step 5: Verify Output
# ========================================
Write-Info "Verifying build output..."

$backendExe = Join-Path $BackendOutputDir "Aura.Api.exe"
$requiredFiles = @(
    @{ Name = "Aura.Api.exe"; Path = $backendExe; MinSize = 50MB; Description = "Backend executable" }
)

$allValid = $true

foreach ($file in $requiredFiles) {
    if (Test-Path $file.Path) {
        $fileInfo = Get-Item $file.Path
        $sizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
        
        if ($fileInfo.Length -lt $file.MinSize) {
            Write-ErrorMessage "  ❌ $($file.Name) is too small ($sizeMB MB) - build may have failed"
            $allValid = $false
        } else {
            Write-Success "  ✓ $($file.Description): $($file.Name) ($sizeMB MB)"
        }
    } else {
        Write-ErrorMessage "  ❌ $($file.Name) not found"
        $allValid = $false
    }
}

if (-not $allValid) {
    Write-ErrorMessage "Build verification failed"
    exit 1
}

Write-Host ""

# ========================================
# Step 6: Copy Configuration Files
# ========================================
Write-Info "Copying configuration files..."

# Create Production configuration
$productionConfig = @"
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source={AURA_DATA_PATH}/aura.db"
  },
  "Paths": {
    "DataPath": "{AURA_DATA_PATH}",
    "LogsPath": "{AURA_LOGS_PATH}",
    "TempPath": "{AURA_TEMP_PATH}",
    "OutputPath": "{USERPROFILE}/Videos/Aura Studio"
  },
  "FFmpeg": {
    "BinariesPath": "{FFMPEG_PATH}"
  }
}
"@

$productionConfigPath = Join-Path $BackendOutputDir "appsettings.Production.json"
Set-Content -Path $productionConfigPath -Value $productionConfig -Encoding UTF8
Write-Info "  Created: appsettings.Production.json"

# Copy main appsettings if it exists
$mainSettings = Join-Path $BackendProject "appsettings.json"
if (Test-Path $mainSettings) {
    Copy-Item -Path $mainSettings -Destination $BackendOutputDir -Force
    Write-Info "  Copied: appsettings.json"
}

Write-Success "Configuration files ready"
Write-Host ""

# ========================================
# Step 7: Test Execution (Quick Check)
# ========================================
Write-Info "Testing backend execution..."

try {
    # Set environment variables for test
    $env:ASPNETCORE_URLS = "http://localhost:5555"
    $env:DOTNET_ENVIRONMENT = "Production"
    
    # Start backend process
    $testProcess = Start-Process -FilePath $backendExe -ArgumentList "--help" -NoNewWindow -Wait -PassThru -ErrorAction Stop
    
    if ($testProcess.ExitCode -eq 0) {
        Write-Success "  Backend executable runs successfully"
    } else {
        Write-Warning "  Backend returned exit code $($testProcess.ExitCode) for --help"
        Write-Warning "  This may be normal if --help is not implemented"
    }
} catch {
    Write-Warning "  Could not test execution (this may be normal): $($_.Exception.Message)"
}

Write-Host ""

# ========================================
# Step 8: List Output Files
# ========================================
Write-Info "Build output files:"

$outputFiles = Get-ChildItem -Path $BackendOutputDir -File | Sort-Object Name
$totalSize = 0

foreach ($file in $outputFiles) {
    $sizeMB = [math]::Round($file.Length / 1MB, 2)
    $totalSize += $file.Length
    Write-Host "  $($file.Name) ($sizeMB MB)"
}

$totalSizeMB = [math]::Round($totalSize / 1MB, 2)
Write-Host ""
Write-Info "Total size: $totalSizeMB MB"
Write-Host ""

# ========================================
# Summary
# ========================================
Write-Host "========================================" -ForegroundColor $SuccessColor
Write-Host "Backend Build Complete!" -ForegroundColor $SuccessColor
Write-Host "========================================" -ForegroundColor $SuccessColor
Write-Host ""
Write-Info "Backend location:"
Write-Host "  $BackendOutputDir"
Write-Host ""
Write-Info "Main executable:"
Write-Host "  Aura.Api.exe"
Write-Host ""
Write-Success "Backend is ready to be bundled with Electron installer! 🎉"
Write-Host ""

exit 0
