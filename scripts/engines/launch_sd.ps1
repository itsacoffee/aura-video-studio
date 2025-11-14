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

Write-Output "=== Stable Diffusion WebUI Launcher ===" -ForegroundColor Cyan
Write-Output ""

# Check if installation exists
if (-not (Test-Path $InstallPath)) {
    Write-Output "ERROR: SD WebUI not found at $InstallPath" -ForegroundColor Red
    Write-Output "Please install SD WebUI first from Aura Download Center" -ForegroundColor Yellow
    exit 1
}

# Build arguments
$launchArgs = @("--api", "--port", $Port)

if ($NoWebUI) {
    $launchArgs += "--nowebui"
    Write-Output "Starting in API-only mode (no web interface)" -ForegroundColor Yellow
} else {
    Write-Output "Web UI will be available at: http://127.0.0.1:$Port" -ForegroundColor Green
}

if ($MedVRAM) {
    $launchArgs += "--medvram"
    Write-Output "Using --medvram for medium VRAM optimization" -ForegroundColor Yellow
}

if ($LowVRAM) {
    $launchArgs += "--lowvram"
    Write-Output "Using --lowvram for low VRAM systems" -ForegroundColor Yellow
}

if ($XFormers) {
    $launchArgs += "--xformers"
    Write-Output "Using xformers for faster generation" -ForegroundColor Green
}

Write-Output ""
Write-Output "Port: $Port" -ForegroundColor Cyan
Write-Output "Path: $InstallPath" -ForegroundColor Cyan
Write-Output "Arguments: $($launchArgs -join ' ')" -ForegroundColor Cyan
Write-Output ""

# Check for webui.bat or run.bat
$launcher = Join-Path $InstallPath "webui.bat"
if (-not (Test-Path $launcher)) {
    $launcher = Join-Path $InstallPath "run.bat"
}

if (-not (Test-Path $launcher)) {
    Write-Output "ERROR: Launcher script not found" -ForegroundColor Red
    Write-Output "Expected: $InstallPath\webui.bat or run.bat" -ForegroundColor Yellow
    exit 1
}

Write-Output "Starting Stable Diffusion WebUI..." -ForegroundColor Green
Write-Output "Press Ctrl+C to stop" -ForegroundColor Yellow
Write-Output ""

# Set environment variables
$env:COMMANDLINE_ARGS = $launchArgs -join ' '
$env:PYTHONUNBUFFERED = "1"

# Launch
Set-Location $InstallPath
& $launcher

Write-Output ""
Write-Output "SD WebUI stopped." -ForegroundColor Yellow
