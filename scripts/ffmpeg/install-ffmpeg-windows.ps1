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

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "FFmpeg Installer for Aura Video Studio" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Define download URLs
$urls = @{
    "gyan" = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
    "github" = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip"
}

# Determine download URL
if ($CustomUrl) {
    $downloadUrl = $CustomUrl
    Write-Host "Using custom URL: $downloadUrl" -ForegroundColor Yellow
} elseif ($urls.ContainsKey($Source)) {
    $downloadUrl = $urls[$Source]
    Write-Host "Using $Source mirror: $downloadUrl" -ForegroundColor Green
} else {
    Write-Host "Invalid source: $Source. Valid options: gyan, github" -ForegroundColor Red
    exit 1
}

# Create temporary paths
$downloadPath = "$env:TEMP\ffmpeg-download-$(Get-Date -Format 'yyyyMMddHHmmss').zip"
$extractPath = "$env:TEMP\ffmpeg-extract-$(Get-Date -Format 'yyyyMMddHHmmss')"

try {
    # Step 1: Download FFmpeg
    Write-Host ""
    Write-Host "[1/5] Downloading FFmpeg..." -ForegroundColor Cyan
    Write-Host "From: $downloadUrl" -ForegroundColor Gray
    Write-Host "To: $downloadPath" -ForegroundColor Gray
    
    $ProgressPreference = 'SilentlyContinue'  # Speed up download
    Invoke-WebRequest -Uri $downloadUrl -OutFile $downloadPath -UseBasicParsing
    $ProgressPreference = 'Continue'
    
    $downloadSize = (Get-Item $downloadPath).Length / 1MB
    Write-Host "Downloaded: $([math]::Round($downloadSize, 2)) MB" -ForegroundColor Green
    
    # Step 2: Extract archive
    Write-Host ""
    Write-Host "[2/5] Extracting archive..." -ForegroundColor Cyan
    Write-Host "To: $extractPath" -ForegroundColor Gray
    
    Expand-Archive -Path $downloadPath -DestinationPath $extractPath -Force
    Write-Host "Extraction complete" -ForegroundColor Green
    
    # Step 3: Find FFmpeg binaries
    Write-Host ""
    Write-Host "[3/5] Locating FFmpeg binaries..." -ForegroundColor Cyan
    
    $ffmpegBin = Get-ChildItem -Path $extractPath -Recurse -Filter "bin" -Directory | Select-Object -First 1
    if (-not $ffmpegBin) {
        throw "Could not find bin directory in extracted files"
    }
    
    $binPath = $ffmpegBin.FullName
    Write-Host "Found binaries in: $binPath" -ForegroundColor Gray
    
    $executables = Get-ChildItem -Path $binPath -Filter "*.exe"
    Write-Host "Found executables:" -ForegroundColor Gray
    foreach ($exe in $executables) {
        Write-Host "  - $($exe.Name) ($([math]::Round($exe.Length / 1MB, 2)) MB)" -ForegroundColor Gray
    }
    
    # Step 4: Install to destination
    Write-Host ""
    Write-Host "[4/5] Installing to Aura dependencies..." -ForegroundColor Cyan
    Write-Host "Destination: $DestinationPath" -ForegroundColor Gray
    
    # Create destination directory
    if (-not (Test-Path $DestinationPath)) {
        New-Item -ItemType Directory -Force -Path $DestinationPath | Out-Null
        Write-Host "Created directory: $DestinationPath" -ForegroundColor Green
    }
    
    # Copy executables
    Copy-Item -Path "$binPath\*.exe" -Destination $DestinationPath -Force
    Write-Host "Copied FFmpeg executables to destination" -ForegroundColor Green
    
    # Step 5: Verify installation
    Write-Host ""
    Write-Host "[5/5] Verifying installation..." -ForegroundColor Cyan
    
    $ffmpegExe = Join-Path $DestinationPath "ffmpeg.exe"
    if (-not (Test-Path $ffmpegExe)) {
        throw "FFmpeg executable not found at: $ffmpegExe"
    }
    
    Write-Host "Testing ffmpeg.exe..." -ForegroundColor Gray
    $versionOutput = & $ffmpegExe -version 2>&1
    $versionLine = ($versionOutput | Select-Object -First 1)
    
    if ($versionLine -match "ffmpeg version") {
        Write-Host "✅ FFmpeg is working correctly!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Version: $versionLine" -ForegroundColor White
    } else {
        throw "FFmpeg verification failed: Unexpected output"
    }
    
    # Cleanup
    Write-Host ""
    Write-Host "Cleaning up temporary files..." -ForegroundColor Cyan
    Remove-Item -Path $downloadPath -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $extractPath -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "Cleanup complete" -ForegroundColor Green
    
    # Success message
    Write-Host ""
    Write-Host "======================================" -ForegroundColor Green
    Write-Host "Installation completed successfully!" -ForegroundColor Green
    Write-Host "======================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "FFmpeg installed to:" -ForegroundColor White
    Write-Host "  $DestinationPath" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor White
    Write-Host "  1. Open Aura Video Studio" -ForegroundColor Gray
    Write-Host "  2. Go to Download Center → Engines tab" -ForegroundColor Gray
    Write-Host "  3. Click 'Rescan' on the FFmpeg card" -ForegroundColor Gray
    Write-Host "  4. FFmpeg should be detected automatically!" -ForegroundColor Gray
    Write-Host ""
    
} catch {
    Write-Host ""
    Write-Host "======================================" -ForegroundColor Red
    Write-Host "Installation failed!" -ForegroundColor Red
    Write-Host "======================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "  1. Check your internet connection" -ForegroundColor Gray
    Write-Host "  2. Try a different mirror:" -ForegroundColor Gray
    Write-Host "     powershell -ExecutionPolicy Bypass -File install-ffmpeg-windows.ps1 -Source github" -ForegroundColor Gray
    Write-Host "  3. Download manually from: https://www.gyan.dev/ffmpeg/builds/" -ForegroundColor Gray
    Write-Host "  4. See docs/INSTALLATION.md for detailed instructions" -ForegroundColor Gray
    Write-Host ""
    
    # Cleanup on error
    Remove-Item -Path $downloadPath -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $extractPath -Recurse -Force -ErrorAction SilentlyContinue
    
    exit 1
}
