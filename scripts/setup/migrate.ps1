# Database Migration Runner for Aura Video Studio (Windows)
# This script runs database migrations using Entity Framework Core

param(
    [switch]$Seed = $false
)

$ErrorActionPreference = "Stop"

Write-Output "Running database migrations..." -ForegroundColor Yellow
Write-Output ""

# Get the root directory (2 levels up from scripts/setup)
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent (Split-Path -Parent $scriptPath)

# Change to root directory
Set-Location $rootPath

# Check if running in Docker or local
if (Test-Path "/.dockerenv") {
    Write-Output "Running in Docker container"
    Set-Location /app
} else {
    Write-Output "Running locally"
}

# Check if dotnet is available
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Output "Error: .NET SDK not found" -ForegroundColor Red
    Write-Output "Install .NET SDK or run migrations via Docker:"
    Write-Output "  docker-compose exec api dotnet ef database update"
    exit 1
}

# Run EF Core migrations
Write-Output "Applying Entity Framework migrations..." -ForegroundColor Yellow
Set-Location Aura.Api

try {
    dotnet ef database update --verbose

    Write-Output ""
    Write-Output "✓ Migrations applied successfully" -ForegroundColor Green
    Write-Output ""

    # Optionally run seed scripts
    if ($Seed) {
        Write-Output "Running seed scripts..." -ForegroundColor Yellow
        & "$rootPath\scripts\setup\seed-database.ps1"
    }
}
catch {
    Write-Output "✗ Migration failed" -ForegroundColor Red
    Write-Output $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Output "Database migration complete!" -ForegroundColor Green
