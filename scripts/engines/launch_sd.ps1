# Launch Stable Diffusion WebUI with API enabled
# For use with Aura Video Studio

param(
    [int]$Port = 7860,
    [string]$InstallPath = "$env:LOCALAPPDATA\Aura\Tools\stable-diffusion-webui",
    [switch]$NoWebUI,
    [switch]$MedVRAM,
    [switch]$LowVRAM,
    [switch]$XFormers
)

Write-Host "=== Stable Diffusion WebUI Launcher ===" -ForegroundColor Cyan
Write-Host ""

# Check if installation exists
if (-not (Test-Path $InstallPath)) {
    Write-Host "ERROR: SD WebUI not found at $InstallPath" -ForegroundColor Red
    Write-Host "Please install SD WebUI first from Aura Download Center" -ForegroundColor Yellow
    exit 1
}

# Build arguments
$args = @("--api", "--port", $Port)

if ($NoWebUI) {
    $args += "--nowebui"
    Write-Host "Starting in API-only mode (no web interface)" -ForegroundColor Yellow
} else {
    Write-Host "Web UI will be available at: http://127.0.0.1:$Port" -ForegroundColor Green
}

if ($MedVRAM) {
    $args += "--medvram"
    Write-Host "Using --medvram for medium VRAM optimization" -ForegroundColor Yellow
}

if ($LowVRAM) {
    $args += "--lowvram"
    Write-Host "Using --lowvram for low VRAM systems" -ForegroundColor Yellow
}

if ($XFormers) {
    $args += "--xformers"
    Write-Host "Using xformers for faster generation" -ForegroundColor Green
}

Write-Host ""
Write-Host "Port: $Port" -ForegroundColor Cyan
Write-Host "Path: $InstallPath" -ForegroundColor Cyan
Write-Host "Arguments: $($args -join ' ')" -ForegroundColor Cyan
Write-Host ""

# Check for webui.bat or run.bat
$launcher = Join-Path $InstallPath "webui.bat"
if (-not (Test-Path $launcher)) {
    $launcher = Join-Path $InstallPath "run.bat"
}

if (-not (Test-Path $launcher)) {
    Write-Host "ERROR: Launcher script not found" -ForegroundColor Red
    Write-Host "Expected: $InstallPath\webui.bat or run.bat" -ForegroundColor Yellow
    exit 1
}

Write-Host "Starting Stable Diffusion WebUI..." -ForegroundColor Green
Write-Host "Press Ctrl+C to stop" -ForegroundColor Yellow
Write-Host ""

# Set environment variables
$env:COMMANDLINE_ARGS = $args -join ' '
$env:PYTHONUNBUFFERED = "1"

# Launch
Set-Location $InstallPath
& $launcher

Write-Host ""
Write-Host "SD WebUI stopped." -ForegroundColor Yellow
