# Database Migration Runner for Aura Video Studio (Windows)
# This script runs database migrations using Entity Framework Core

param(
    [switch]$Seed = $false
)

$ErrorActionPreference = "Stop"

Write-Host "Running database migrations..." -ForegroundColor Yellow
Write-Host ""

# Get the root directory (2 levels up from scripts/setup)
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent (Split-Path -Parent $scriptPath)

# Change to root directory
Set-Location $rootPath

# Check if running in Docker or local
if (Test-Path "/.dockerenv") {
    Write-Host "Running in Docker container"
    Set-Location /app
} else {
    Write-Host "Running locally"
}

# Check if dotnet is available
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "Error: .NET SDK not found" -ForegroundColor Red
    Write-Host "Install .NET SDK or run migrations via Docker:"
    Write-Host "  docker-compose exec api dotnet ef database update"
    exit 1
}

# Run EF Core migrations
Write-Host "Applying Entity Framework migrations..." -ForegroundColor Yellow
Set-Location Aura.Api

try {
    dotnet ef database update --verbose
    
    Write-Host ""
    Write-Host "✓ Migrations applied successfully" -ForegroundColor Green
    Write-Host ""
    
    # Optionally run seed scripts
    if ($Seed) {
        Write-Host "Running seed scripts..." -ForegroundColor Yellow
        & "$rootPath\scripts\setup\seed-database.ps1"
    }
}
catch {
    Write-Host "✗ Migration failed" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host "Database migration complete!" -ForegroundColor Green
