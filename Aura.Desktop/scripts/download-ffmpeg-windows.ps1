# PowerShell Script to Download and Bundle FFmpeg for Windows
# This script downloads the full GPL FFmpeg build with all codecs

param(
    [switch]$Force,
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

function Show-ErrorMessageMessage {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor $ErrorColor
}

if ($Help) {
    Write-Output "FFmpeg Download Script for Aura Video Studio"
    Write-Output ""
    Write-Output "Usage: .\download-ffmpeg-windows.ps1 [OPTIONS]"
    Write-Output ""
    Write-Output "Options:"
    Write-Output "  -Force    Force download even if FFmpeg already exists"
    Write-Output "  -Help     Show this help message"
    exit 0
}

Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host "FFmpeg Download and Setup" -ForegroundColor $InfoColor
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Output ""

$ScriptDir = $PSScriptRoot
$DesktopDir = Split-Path $ScriptDir -Parent
$ResourcesDir = Join-Path $DesktopDir "resources"
$FFmpegDir = Join-Path $ResourcesDir "ffmpeg"
$FFmpegWin64Dir = Join-Path $FFmpegDir "win-x64"
$FFmpegBinDir = Join-Path $FFmpegWin64Dir "bin"
$TempDir = Join-Path $DesktopDir "temp"

# FFmpeg download settings
$FFmpegVersion = "master"
$FFmpegBuildType = "gpl"
$FFmpegArchitecture = "win64"
$FFmpegFileName = "ffmpeg-$FFmpegVersion-latest-$FFmpegArchitecture-$FFmpegBuildType.zip"
$FFmpegUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/$FFmpegFileName"
$FFmpegZipPath = Join-Path $TempDir $FFmpegFileName

# Check if FFmpeg already exists
$FFmpegExe = Join-Path $FFmpegBinDir "ffmpeg.exe"
$FFprobeExe = Join-Path $FFmpegBinDir "ffprobe.exe"

if ((Test-Path $FFmpegExe) -and (Test-Path $FFprobeExe) -and -not $Force) {
    Write-Success "FFmpeg already exists at: $FFmpegBinDir"
    Write-Info "FFmpeg version: $(& $FFmpegExe -version | Select-Object -First 1)"
    Write-Output ""
    Write-Info "Use -Force to re-download"
    exit 0
}

if ($Force) {
    Write-Info "Force download enabled - removing existing FFmpeg..."
    if (Test-Path $FFmpegDir) {
        Remove-Item -Path $FFmpegDir -Recurse -Force
    }
}

# ========================================
# Step 1: Create Directories
# ========================================
Write-Info "Creating directories..."

$directories = @($ResourcesDir, $FFmpegDir, $FFmpegWin64Dir, $FFmpegBinDir, $TempDir)
foreach ($dir in $directories) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Info "  Created: $dir"
    }
}

Write-Success "Directories ready"
Write-Output ""

# ========================================
# Step 2: Download FFmpeg
# ========================================
Write-Info "Downloading FFmpeg..."
Write-Info "  URL: $FFmpegUrl"
Write-Info "  File: $FFmpegFileName"
Write-Info "  Size: ~140MB (this may take a few minutes)"
Write-Output ""

try {
    # Use WebClient for progress reporting
    $webClient = New-Object System.Net.WebClient

    # Register progress event
    Register-ObjectEvent -InputObject $webClient -EventName DownloadProgressChanged -Action {
        $percent = $EventArgs.ProgressPercentage
        Write-Progress -Activity "Downloading FFmpeg" -Status "$percent% Complete" -PercentComplete $percent
    } | Out-Null

    # Start download
    $webClient.DownloadFile($FFmpegUrl, $FFmpegZipPath)

    # Unregister event
    Unregister-Event -SourceIdentifier "System.Net.WebClient.DownloadProgressChanged" -ErrorAction SilentlyContinue

    Write-Progress -Activity "Downloading FFmpeg" -Completed
    Write-Success "Download complete"
    Write-Output ""

} catch {
    Write-ErrorMessage "Failed to download FFmpeg: $($_.Exception.Message)"
    Write-Output ""
    Write-Info "You can manually download FFmpeg from:"
    Write-Info "  $FFmpegUrl"
    Write-Info "And extract it to:"
    Write-Info "  $FFmpegBinDir"
    exit 1
}

# ========================================
# Step 3: Extract FFmpeg
# ========================================
Write-Info "Extracting FFmpeg..."

try {
    # Expand archive
    Expand-Archive -Path $FFmpegZipPath -DestinationPath $TempDir -Force

    # Find extracted directory (it will have a version-specific name)
    $extractedDirs = Get-ChildItem -Path $TempDir -Directory -Filter "ffmpeg-*" | Sort-Object LastWriteTime -Descending

    if ($extractedDirs.Count -eq 0) {
        throw "Could not find extracted FFmpeg directory"
    }

    $extractedDir = $extractedDirs[0].FullName
    $extractedBinDir = Join-Path $extractedDir "bin"

    Write-Info "  Extracted to: $extractedDir"

    # Copy binaries to resources directory
    if (Test-Path $extractedBinDir) {
        Write-Info "  Copying binaries..."
        Copy-Item -Path "$extractedBinDir\ffmpeg.exe" -Destination $FFmpegBinDir -Force
        Copy-Item -Path "$extractedBinDir\ffprobe.exe" -Destination $FFmpegBinDir -Force
        Copy-Item -Path "$extractedBinDir\ffplay.exe" -Destination $FFmpegBinDir -Force -ErrorAction SilentlyContinue
    } else {
        throw "Could not find bin directory in extracted FFmpeg"
    }

    # Copy license and documentation
    $licenseSrc = Join-Path $extractedDir "LICENSE.txt"
    $readmeSrc = Join-Path $extractedDir "README.txt"

    if (Test-Path $licenseSrc) {
        Copy-Item -Path $licenseSrc -Destination $FFmpegWin64Dir -Force
    }

    if (Test-Path $readmeSrc) {
        Copy-Item -Path $readmeSrc -Destination $FFmpegWin64Dir -Force
    }

    Write-Success "Extraction complete"
    Write-Output ""

} catch {
    Write-ErrorMessage "Failed to extract FFmpeg: $($_.Exception.Message)"
    exit 1
}

# ========================================
# Step 4: Verify Installation
# ========================================
Write-Info "Verifying FFmpeg installation..."

$binaries = @(
    @{ Name = "ffmpeg.exe"; Path = $FFmpegExe; MinSize = 50MB },
    @{ Name = "ffprobe.exe"; Path = $FFprobeExe; MinSize = 10MB }
)

$allValid = $true

foreach ($binary in $binaries) {
    if (Test-Path $binary.Path) {
        $fileInfo = Get-Item $binary.Path
        $sizeMB = [math]::Round($fileInfo.Length / 1MB, 2)

        if ($fileInfo.Length -lt $binary.MinSize) {
            Write-ErrorMessage "  ❌ $($binary.Name) is too small ($sizeMB MB) - may be corrupted"
            $allValid = $false
        } else {
            Write-Success "  ✓ $($binary.Name) ($sizeMB MB)"
        }
    } else {
        Write-ErrorMessage "  ❌ $($binary.Name) not found"
        $allValid = $false
    }
}

if (-not $allValid) {
    Write-ErrorMessage "FFmpeg installation verification failed"
    exit 1
}

Write-Output ""

# Test FFmpeg execution
Write-Info "Testing FFmpeg execution..."
try {
    $versionOutput = & $FFmpegExe -version 2>&1 | Select-Object -First 1
    Write-Success "  FFmpeg version: $versionOutput"
} catch {
    Write-ErrorMessage "  Failed to execute FFmpeg: $($_.Exception.Message)"
    $allValid = $false
}

Write-Output ""

# ========================================
# Step 5: Cleanup
# ========================================
Write-Info "Cleaning up temporary files..."

try {
    # Remove downloaded zip
    if (Test-Path $FFmpegZipPath) {
        Remove-Item -Path $FFmpegZipPath -Force
        Write-Info "  Removed: $FFmpegFileName"
    }

    # Remove extracted directory
    if (Test-Path $extractedDir) {
        Remove-Item -Path $extractedDir -Recurse -Force
        Write-Info "  Removed: extracted files"
    }

    # Remove temp directory if empty
    $tempContents = Get-ChildItem -Path $TempDir -ErrorAction SilentlyContinue
    if ($tempContents.Count -eq 0) {
        Remove-Item -Path $TempDir -Force
        Write-Info "  Removed: temp directory"
    }

    Write-Success "Cleanup complete"

} catch {
    Show-Warning "Some temporary files could not be cleaned up: $($_.Exception.Message)"
}

Write-Output ""

# ========================================
# Summary
# ========================================
Write-Host "========================================" -ForegroundColor $SuccessColor
Write-Host "FFmpeg Setup Complete!" -ForegroundColor $SuccessColor
Write-Host "========================================" -ForegroundColor $SuccessColor
Write-Output ""
Write-Info "FFmpeg location:"
Write-Output "  $FFmpegBinDir"
Write-Output ""
Write-Info "Binaries:"
Write-Output "  - ffmpeg.exe"
Write-Output "  - ffprobe.exe"
Write-Output ""
Write-Success "FFmpeg is ready to be bundled with Electron installer! 🎉"
Write-Output ""

exit 0
