# Launch Mimic3 TTS Server
# For use with Aura Video Studio

param(
    [int]$Port = 59125,
    [string]$Voice = "en_US/vctk_low",
    [string]$InstallPath = "$env:LOCALAPPDATA\Aura\Tools\mimic3"
)

Write-Host "=== Mimic3 TTS Server Launcher ===" -ForegroundColor Cyan
Write-Output ""

# Check if mimic3 is in PATH or venv
$mimic3Cmd = "mimic3-server"
$venvActivate = Join-Path $InstallPath "venv\Scripts\Activate.ps1"

if (Test-Path $venvActivate) {
    Write-Host "Activating virtual environment..." -ForegroundColor Yellow
    & $venvActivate
}

# Test if mimic3-server is available
try {
    $version = & $mimic3Cmd --version 2>&1
    Write-Host "Mimic3 version: $version" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Mimic3 not found in PATH" -ForegroundColor Red
    Write-Host "Please install Mimic3 first:" -ForegroundColor Yellow
    Write-Host "  pip install mycroft-mimic3-tts[all]" -ForegroundColor Cyan
    exit 1
}

Write-Output ""
Write-Host "Starting Mimic3 TTS server..." -ForegroundColor Green
Write-Host "Port: $Port" -ForegroundColor Cyan
Write-Host "Default voice: $Voice" -ForegroundColor Cyan
Write-Output ""
Write-Host "Web UI: http://127.0.0.1:$Port" -ForegroundColor Green
Write-Host "API: http://127.0.0.1:$Port/api/tts" -ForegroundColor Green
Write-Output ""
Write-Host "Press Ctrl+C to stop" -ForegroundColor Yellow
Write-Output ""

# Launch server
& $mimic3Cmd --port $Port --voice $Voice

Write-Output ""
Write-Host "Mimic3 server stopped." -ForegroundColor Yellow
