#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates the complete Windows 11 bootstrap → install → build → start workflow.

.DESCRIPTION
    This script validates that the complete development workflow works on Windows:
    1. Checks prerequisites (.NET SDK, Node.js, npm)
    2. Restores dependencies (dotnet restore, npm install)
    3. Builds the solution (dotnet build)
    4. Builds the web UI (npm run build)
    5. Verifies outputs

.EXAMPLE
    .\scripts\validate-windows-workflow.ps1
    Run the complete validation
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

Write-Output ""
Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host " Windows 11 Development Workflow Validation" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Output ""

$repoRoot = Split-Path -Parent $PSScriptRoot
$pushedLocation = $false

try {
    Push-Location $repoRoot
    $pushedLocation = $true

$allPassed = $true

    # Step 1: Check prerequisites
    Write-Host "[1/5] Checking prerequisites..." -ForegroundColor Yellow

    # Check .NET SDK
    if (-not (Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
        Write-Host "  ✗ .NET SDK not found" -ForegroundColor Red
        $allPassed = $false
    } else {
        $dotnetVersion = dotnet --version
        Write-Host "  ✓ .NET SDK: $dotnetVersion" -ForegroundColor Green
    }

    # Check Node.js
    if (-not (Get-Command "node" -ErrorAction SilentlyContinue)) {
        Write-Host "  ✗ Node.js not found" -ForegroundColor Red
        $allPassed = $false
    } else {
        $nodeVersion = node --version
        Write-Host "  ✓ Node.js: $nodeVersion" -ForegroundColor Green
    }

    # Check npm
    if (-not (Get-Command "npm" -ErrorAction SilentlyContinue)) {
        Write-Host "  ✗ npm not found" -ForegroundColor Red
        $allPassed = $false
    } else {
        $npmVersion = npm --version
        Write-Host "  ✓ npm: $npmVersion" -ForegroundColor Green
    }

    if (-not $allPassed) {
        throw "Prerequisites check failed. Please install missing tools."
    }
    Write-Output ""

    # Step 2: Restore dependencies
    Write-Host "[2/5] Restoring .NET dependencies..." -ForegroundColor Yellow
    dotnet restore --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed"
    }
    Write-Host "  ✓ .NET dependencies restored" -ForegroundColor Green
    Write-Output ""

    Write-Host "[3/5] Installing npm dependencies..." -ForegroundColor Yellow
    Push-Location "Aura.Web"
    try {
        npm install --loglevel=error
        if ($LASTEXITCODE -ne 0) {
            throw "npm install failed"
        }
        Write-Host "  ✓ npm dependencies installed" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
    Write-Output ""

    # Step 3: Build the solution
    Write-Host "[4/5] Building .NET projects..." -ForegroundColor Yellow
    $buildProjects = @(
        "Aura.Core/Aura.Core.csproj",
        "Aura.Providers/Aura.Providers.csproj",
        "Aura.Api/Aura.Api.csproj"
    )

    foreach ($project in $buildProjects) {
        Write-Host "  Building $project..." -ForegroundColor Gray
        dotnet build $project --configuration Release --nologo --verbosity quiet
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed for $project"
        }
    }
    Write-Host "  ✓ .NET projects built successfully" -ForegroundColor Green
    Write-Output ""

    # Step 4: Build the web UI
    Write-Host "[5/5] Building web UI..." -ForegroundColor Yellow
    Push-Location "Aura.Web"
    try {
        npm run build
        if ($LASTEXITCODE -ne 0) {
            throw "Web UI build failed"
        }

        # Verify dist folder was created
        if (-not (Test-Path "dist")) {
            throw "dist folder was not created"
        }

        Write-Host "  ✓ Web UI built successfully" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
    Write-Output ""

    # Success!
    Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host " ✓ All validation checks passed!" -ForegroundColor Green
    Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Output ""
    Write-Host "The Windows 11 development workflow is working correctly." -ForegroundColor White
    Write-Output ""

    exit 0
}
catch {
    Write-Output ""
    Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Red
    Write-Host " ✗ Validation failed!" -ForegroundColor Red
    Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Red
    Write-Output ""
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Output ""

    exit 1
}
finally {
    if ($pushedLocation) {
        Pop-Location
    }
}
