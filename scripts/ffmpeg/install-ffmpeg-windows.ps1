# Install FFmpeg for Aura Video Studio (Windows)
#
# This script downloads and installs FFmpeg to the Aura dependencies folder
# Run in PowerShell: powershell -ExecutionPolicy Bypass -File install-ffmpeg-windows.ps1

param(
    [string]$Source = "gyan",  # Options: "gyan", "github", "custom"
    [string]$CustomUrl = "",
    [string]$DestinationPath = "$env:LOCALAPPDATA\Aura\dependencies\bin"
)

$ErrorActionPreference = "Stop"

Write-Output "======================================" -ForegroundColor Cyan
Write-Output "FFmpeg Installer for Aura Video Studio" -ForegroundColor Cyan
Write-Output "======================================" -ForegroundColor Cyan
Write-Output ""

# Define download URLs
$urls = @{
    "gyan" = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
    "github" = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip"
}

# Determine download URL
if ($CustomUrl) {
    $downloadUrl = $CustomUrl
    Write-Output "Using custom URL: $downloadUrl" -ForegroundColor Yellow
} elseif ($urls.ContainsKey($Source)) {
    $downloadUrl = $urls[$Source]
    Write-Output "Using $Source mirror: $downloadUrl" -ForegroundColor Green
} else {
    Write-Output "Invalid source: $Source. Valid options: gyan, github" -ForegroundColor Red
    exit 1
}

# Create temporary paths
$downloadPath = "$env:TEMP\ffmpeg-download-$(Get-Date -Format 'yyyyMMddHHmmss').zip"
$extractPath = "$env:TEMP\ffmpeg-extract-$(Get-Date -Format 'yyyyMMddHHmmss')"

try {
    # Step 1: Download FFmpeg
    Write-Output ""
    Write-Output "[1/5] Downloading FFmpeg..." -ForegroundColor Cyan
    Write-Output "From: $downloadUrl" -ForegroundColor Gray
    Write-Output "To: $downloadPath" -ForegroundColor Gray

    $ProgressPreference = 'SilentlyContinue'  # Speed up download
    Invoke-WebRequest -Uri $downloadUrl -OutFile $downloadPath -UseBasicParsing
    $ProgressPreference = 'Continue'

    $downloadSize = (Get-Item $downloadPath).Length / 1MB
    Write-Output "Downloaded: $([math]::Round($downloadSize, 2)) MB" -ForegroundColor Green

    # Step 2: Extract archive
    Write-Output ""
    Write-Output "[2/5] Extracting archive..." -ForegroundColor Cyan
    Write-Output "To: $extractPath" -ForegroundColor Gray

    Expand-Archive -Path $downloadPath -DestinationPath $extractPath -Force
    Write-Output "Extraction complete" -ForegroundColor Green

    # Step 3: Find FFmpeg binaries
    Write-Output ""
    Write-Output "[3/5] Locating FFmpeg binaries..." -ForegroundColor Cyan

    $ffmpegBin = Get-ChildItem -Path $extractPath -Recurse -Filter "bin" -Directory | Select-Object -First 1
    if (-not $ffmpegBin) {
        throw "Could not find bin directory in extracted files"
    }

    $binPath = $ffmpegBin.FullName
    Write-Output "Found binaries in: $binPath" -ForegroundColor Gray

    $executables = Get-ChildItem -Path $binPath -Filter "*.exe"
    Write-Output "Found executables:" -ForegroundColor Gray
    foreach ($exe in $executables) {
        Write-Output "  - $($exe.Name) ($([math]::Round($exe.Length / 1MB, 2)) MB)" -ForegroundColor Gray
    }

    # Step 4: Install to destination
    Write-Output ""
    Write-Output "[4/5] Installing to Aura dependencies..." -ForegroundColor Cyan
    Write-Output "Destination: $DestinationPath" -ForegroundColor Gray

    # Create destination directory
    if (-not (Test-Path $DestinationPath)) {
        New-Item -ItemType Directory -Force -Path $DestinationPath | Out-Null
        Write-Output "Created directory: $DestinationPath" -ForegroundColor Green
    }

    # Copy executables
    Copy-Item -Path "$binPath\*.exe" -Destination $DestinationPath -Force
    Write-Output "Copied FFmpeg executables to destination" -ForegroundColor Green

    # Step 5: Verify installation
    Write-Output ""
    Write-Output "[5/5] Verifying installation..." -ForegroundColor Cyan

    $ffmpegExe = Join-Path $DestinationPath "ffmpeg.exe"
    if (-not (Test-Path $ffmpegExe)) {
        throw "FFmpeg executable not found at: $ffmpegExe"
    }

    Write-Output "Testing ffmpeg.exe..." -ForegroundColor Gray
    $versionOutput = & $ffmpegExe -version 2>&1
    $versionLine = ($versionOutput | Select-Object -First 1)

    if ($versionLine -match "ffmpeg version") {
        Write-Output "✅ FFmpeg is working correctly!" -ForegroundColor Green
        Write-Output ""
        Write-Output "Version: $versionLine" -ForegroundColor White
    } else {
        throw "FFmpeg verification failed: Unexpected output"
    }

    # Cleanup
    Write-Output ""
    Write-Output "Cleaning up temporary files..." -ForegroundColor Cyan
    Remove-Item -Path $downloadPath -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $extractPath -Recurse -Force -ErrorAction SilentlyContinue
    Write-Output "Cleanup complete" -ForegroundColor Green

    # Success message
    Write-Output ""
    Write-Output "======================================" -ForegroundColor Green
    Write-Output "Installation completed successfully!" -ForegroundColor Green
    Write-Output "======================================" -ForegroundColor Green
    Write-Output ""
    Write-Output "FFmpeg installed to:" -ForegroundColor White
    Write-Output "  $DestinationPath" -ForegroundColor Yellow
    Write-Output ""
    Write-Output "Next steps:" -ForegroundColor White
    Write-Output "  1. Open Aura Video Studio" -ForegroundColor Gray
    Write-Output "  2. Go to Download Center → Engines tab" -ForegroundColor Gray
    Write-Output "  3. Click 'Rescan' on the FFmpeg card" -ForegroundColor Gray
    Write-Output "  4. FFmpeg should be detected automatically!" -ForegroundColor Gray
    Write-Output ""

} catch {
    Write-Output ""
    Write-Output "======================================" -ForegroundColor Red
    Write-Output "Installation failed!" -ForegroundColor Red
    Write-Output "======================================" -ForegroundColor Red
    Write-Output ""
    Write-Output "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Output ""
    Write-Output "Troubleshooting:" -ForegroundColor Yellow
    Write-Output "  1. Check your internet connection" -ForegroundColor Gray
    Write-Output "  2. Try a different mirror:" -ForegroundColor Gray
    Write-Output "     powershell -ExecutionPolicy Bypass -File install-ffmpeg-windows.ps1 -Source github" -ForegroundColor Gray
    Write-Output "  3. Download manually from: https://www.gyan.dev/ffmpeg/builds/" -ForegroundColor Gray
    Write-Output "  4. See docs/INSTALLATION.md for detailed instructions" -ForegroundColor Gray
    Write-Output ""

    # Cleanup on error
    Remove-Item -Path $downloadPath -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $extractPath -Recurse -Force -ErrorAction SilentlyContinue

    exit 1
}
