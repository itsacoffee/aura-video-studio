# Aura Video Studio - Local Development Setup Script (Windows)
# This script bootstraps the complete local development environment

param(
    [switch]$SkipDependencyCheck = $false,
    [switch]$Quiet = $false
)

$ErrorActionPreference = "Stop"

# Configuration
$MIN_DOCKER_VERSION = [version]"20.0.0"
$MIN_NODE_VERSION = [version]"20.0.0"
$MIN_DOTNET_VERSION = [version]"8.0.0"

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    
    if (-not $Quiet) {
        Write-Host $Message -ForegroundColor $Color
    }
}

function Test-CommandExists {
    param([string]$Command)
    
    $null = Get-Command $Command -ErrorAction SilentlyContinue
    return $?
}

function Compare-Versions {
    param(
        [version]$Version1,
        [version]$Version2
    )
    
    return $Version1 -ge $Version2
}

Write-ColorOutput "╔════════════════════════════════════════════════════════╗" -Color Cyan
Write-ColorOutput "║  Aura Video Studio - Local Development Setup          ║" -Color Cyan
Write-ColorOutput "╚════════════════════════════════════════════════════════╝" -Color Cyan
Write-ColorOutput ""

# Check prerequisites
Write-ColorOutput "[1/7] Checking prerequisites..." -Color Yellow

# Check Docker
if (-not (Test-CommandExists docker)) {
    Write-ColorOutput "✗ Docker is not installed" -Color Red
    Write-ColorOutput "  Please install Docker Desktop from: https://www.docker.com/products/docker-desktop" -Color Red
    exit 1
}

try {
    $dockerVersionString = (docker --version) -replace '.*version ([0-9.]+).*', '$1'
    $dockerVersion = [version]$dockerVersionString
    if (-not (Compare-Versions $dockerVersion $MIN_DOCKER_VERSION)) {
        Write-ColorOutput "✗ Docker version $dockerVersion is too old (need >= $MIN_DOCKER_VERSION)" -Color Red
        exit 1
    }
    Write-ColorOutput "✓ Docker $dockerVersion" -Color Green
} catch {
    Write-ColorOutput "✗ Could not determine Docker version" -Color Red
    exit 1
}

# Check Docker Compose
try {
    $null = docker-compose --version
    Write-ColorOutput "✓ Docker Compose" -Color Green
} catch {
    try {
        $null = docker compose version
        Write-ColorOutput "✓ Docker Compose (plugin)" -Color Green
    } catch {
        Write-ColorOutput "✗ Docker Compose is not installed" -Color Red
        exit 1
    }
}

# Check if Docker is running
try {
    $null = docker ps 2>&1
    Write-ColorOutput "✓ Docker daemon is running" -Color Green
} catch {
    Write-ColorOutput "✗ Docker daemon is not running" -Color Red
    Write-ColorOutput "  Please start Docker Desktop" -Color Red
    exit 1
}

# Check .NET SDK
if (-not (Test-CommandExists dotnet)) {
    Write-ColorOutput "⚠ .NET SDK is not installed" -Color Yellow
    Write-ColorOutput "  Recommended for local development: https://dotnet.microsoft.com/download" -Color Yellow
    Write-ColorOutput "  (Not required if using Docker only)" -Color Yellow
} else {
    try {
        $dotnetVersionString = (dotnet --version)
        $dotnetVersion = [version]$dotnetVersionString
        if (Compare-Versions $dotnetVersion $MIN_DOTNET_VERSION) {
            Write-ColorOutput "✓ .NET SDK $dotnetVersion" -Color Green
        } else {
            Write-ColorOutput "⚠ .NET SDK $dotnetVersion is old (recommend >= $MIN_DOTNET_VERSION)" -Color Yellow
        }
    } catch {
        Write-ColorOutput "⚠ Could not determine .NET version" -Color Yellow
    }
}

# Check Node.js
if (-not (Test-CommandExists node)) {
    Write-ColorOutput "⚠ Node.js is not installed" -Color Yellow
    Write-ColorOutput "  Recommended for local development: https://nodejs.org/" -Color Yellow
    Write-ColorOutput "  (Not required if using Docker only)" -Color Yellow
} else {
    try {
        $nodeVersionString = (node --version) -replace 'v', ''
        $nodeVersion = [version]$nodeVersionString
        if (Compare-Versions $nodeVersion $MIN_NODE_VERSION) {
            Write-ColorOutput "✓ Node.js $nodeVersion" -Color Green
        } else {
            Write-ColorOutput "⚠ Node.js $nodeVersion is old (recommend >= $MIN_NODE_VERSION)" -Color Yellow
        }
    } catch {
        Write-ColorOutput "⚠ Could not determine Node.js version" -Color Yellow
    }
}

# Check for FFmpeg (optional but recommended)
if (Test-CommandExists ffmpeg) {
    try {
        $ffmpegVersion = (ffmpeg -version 2>&1 | Select-Object -First 1) -replace '.*version ([0-9.]+).*', '$1'
        Write-ColorOutput "✓ FFmpeg $ffmpegVersion (local)" -Color Green
    } catch {
        Write-ColorOutput "✓ FFmpeg (local)" -Color Green
    }
} else {
    Write-ColorOutput "⚠ FFmpeg not installed locally (will use Docker container)" -Color Yellow
}

Write-ColorOutput ""

# Create required directories
Write-ColorOutput "[2/7] Creating directory structure..." -Color Yellow
$directories = @("data", "logs", "temp-media", "scripts\setup")
foreach ($dir in $directories) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
}
Write-ColorOutput "✓ Directories created" -Color Green
Write-ColorOutput ""

# Setup environment file
Write-ColorOutput "[3/7] Setting up environment configuration..." -Color Yellow
if (-not (Test-Path ".env")) {
    Copy-Item ".env.example" ".env"
    Write-ColorOutput "✓ Created .env from .env.example" -Color Green
    Write-ColorOutput "  Edit .env to add your API keys (optional)" -Color Cyan
} else {
    Write-ColorOutput "  .env already exists, skipping" -Color Cyan
}
Write-ColorOutput ""

# Check port availability
Write-ColorOutput "[4/7] Checking port availability..." -Color Yellow
$portsToCheck = @(
    @{Port=5005; Service="API"},
    @{Port=3000; Service="Web"},
    @{Port=6379; Service="Redis"}
)

$portsInUse = @()
foreach ($portInfo in $portsToCheck) {
    $port = $portInfo.Port
    $service = $portInfo.Service
    
    $connection = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
    if ($connection) {
        $portsInUse += "$port ($service)"
        Write-ColorOutput "✗ Port $port ($service) is already in use" -Color Red
    } else {
        Write-ColorOutput "✓ Port $port ($service) is available" -Color Green
    }
}

if ($portsInUse.Count -gt 0) {
    Write-ColorOutput "Warning: Some ports are in use. Services may fail to start." -Color Yellow
    Write-ColorOutput "Consider stopping conflicting services or changing ports in docker-compose.yml" -Color Yellow
}
Write-ColorOutput ""

# Pull Docker images
Write-ColorOutput "[5/7] Pulling Docker images..." -Color Yellow
try {
    docker-compose pull redis ffmpeg 2>&1 | Out-Null
} catch {
    Write-ColorOutput "  (Continuing despite pull errors)" -Color Yellow
}
Write-ColorOutput "✓ Base images ready" -Color Green
Write-ColorOutput ""

# Install dependencies (if running locally)
Write-ColorOutput "[6/7] Installing dependencies..." -Color Yellow

if ((Test-CommandExists dotnet) -and (Test-Path "Aura.Api\Aura.Api.csproj")) {
    Write-ColorOutput "  Installing .NET packages..." -Color Cyan
    dotnet restore | Out-Null
}

if ((Test-CommandExists npm) -and (Test-Path "Aura.Web\package.json")) {
    Write-ColorOutput "  Installing Node.js packages..." -Color Cyan
    Push-Location Aura.Web
    npm ci | Out-Null
    Pop-Location
}

Write-ColorOutput "✓ Dependencies installed" -Color Green
Write-ColorOutput ""

# Create helper scripts
Write-ColorOutput "[7/7] Creating helper scripts..." -Color Yellow

# Port check script
$portCheckScript = @'
$PORTS = @(5005, 3000, 6379)
$SERVICES = @("API", "Web", "Redis")
$CONFLICT = $false

for ($i = 0; $i -lt $PORTS.Length; $i++) {
    $connection = Get-NetTCPConnection -LocalPort $PORTS[$i] -ErrorAction SilentlyContinue
    if ($connection) {
        Write-Host "⚠ Port $($PORTS[$i]) ($($SERVICES[$i])) is in use"
        $CONFLICT = $true
    }
}

if ($CONFLICT) { exit 1 } else { exit 0 }
'@
Set-Content -Path "scripts\setup\check-ports.ps1" -Value $portCheckScript

# Validation script
$validationScript = @'
Write-Host "Validating configuration files..."

$FILES = @("docker-compose.yml", "Makefile", ".env")
foreach ($file in $FILES) {
    if (-not (Test-Path $file)) {
        Write-Host "✗ Missing required file: $file" -ForegroundColor Red
        exit 1
    }
}

$REQUIRED_VARS = @("ASPNETCORE_ENVIRONMENT", "AURA_DATABASE_PATH")
$envContent = Get-Content ".env" -ErrorAction SilentlyContinue
foreach ($var in $REQUIRED_VARS) {
    if ($envContent -notmatch "^$var=" -and $envContent -notmatch "^# *$var=") {
        Write-Host "⚠ Missing variable in .env: $var" -ForegroundColor Yellow
    }
}

Write-Host "✓ Configuration valid" -ForegroundColor Green
'@
Set-Content -Path "scripts\setup\validate-config.ps1" -Value $validationScript

Write-ColorOutput "✓ Helper scripts created" -Color Green
Write-ColorOutput ""

# Summary
Write-ColorOutput "╔════════════════════════════════════════════════════════╗" -Color Green
Write-ColorOutput "║  Setup Complete!                                       ║" -Color Green
Write-ColorOutput "╚════════════════════════════════════════════════════════╝" -Color Green
Write-ColorOutput ""
Write-ColorOutput "Next Steps:" -Color Cyan
Write-ColorOutput ""
Write-ColorOutput "  1. (Optional) Edit .env to add API keys for premium features" -Color White
Write-ColorOutput ""
Write-ColorOutput "  2. Start the development environment:" -Color White
Write-ColorOutput "     make dev" -Color Green
Write-ColorOutput "     (Or use: docker-compose up --build)" -Color Gray
Write-ColorOutput ""
Write-ColorOutput "  3. Wait ~60 seconds for services to start, then open:" -Color White
Write-ColorOutput "     http://localhost:3000" -Color Cyan
Write-ColorOutput ""
Write-ColorOutput "  4. View logs in another terminal:" -Color White
Write-ColorOutput "     make logs" -Color Green
Write-ColorOutput "     (Or use: docker-compose logs -f)" -Color Gray
Write-ColorOutput ""
Write-ColorOutput "  5. Check service health:" -Color White
Write-ColorOutput "     make health" -Color Green
Write-ColorOutput ""
Write-ColorOutput "Useful Commands:" -Color Cyan
Write-ColorOutput "  make help        - Show all available commands" -Color White
Write-ColorOutput "  make stop        - Stop all services" -Color White
Write-ColorOutput "  make clean       - Remove all containers and data" -Color White
Write-ColorOutput "  make db-reset    - Reset the database" -Color White
Write-ColorOutput ""
Write-ColorOutput "Troubleshooting:" -Color Yellow
Write-ColorOutput "  If services fail to start, check:" -Color White
Write-ColorOutput "  • Port conflicts: make status" -Color White
Write-ColorOutput "  • Docker is running: docker ps" -Color White
Write-ColorOutput "  • Logs for errors: make logs" -Color White
Write-ColorOutput ""
Write-ColorOutput "  See DEVELOPMENT.md and docs/troubleshooting/ for more help" -Color White
Write-ColorOutput ""

exit 0
