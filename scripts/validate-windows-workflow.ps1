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
Write-Output "════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Output " Windows 11 Development Workflow Validation" -ForegroundColor Cyan
Write-Output "════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Output ""

$repoRoot = Split-Path -Parent $PSScriptRoot
$pushedLocation = $false

try {
    Push-Location $repoRoot
    $pushedLocation = $true

$allPassed = $true

    # Step 1: Check prerequisites
    Write-Output "[1/5] Checking prerequisites..." -ForegroundColor Yellow

    # Check .NET SDK
    if (-not (Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
        Write-Output "  ✗ .NET SDK not found" -ForegroundColor Red
        $allPassed = $false
    } else {
        $dotnetVersion = dotnet --version
        Write-Output "  ✓ .NET SDK: $dotnetVersion" -ForegroundColor Green
    }

    # Check Node.js
    if (-not (Get-Command "node" -ErrorAction SilentlyContinue)) {
        Write-Output "  ✗ Node.js not found" -ForegroundColor Red
        $allPassed = $false
    } else {
        $nodeVersion = node --version
        Write-Output "  ✓ Node.js: $nodeVersion" -ForegroundColor Green
    }

    # Check npm
    if (-not (Get-Command "npm" -ErrorAction SilentlyContinue)) {
        Write-Output "  ✗ npm not found" -ForegroundColor Red
        $allPassed = $false
    } else {
        $npmVersion = npm --version
        Write-Output "  ✓ npm: $npmVersion" -ForegroundColor Green
    }

    if (-not $allPassed) {
        throw "Prerequisites check failed. Please install missing tools."
    }
    Write-Output ""

    # Step 2: Restore dependencies
    Write-Output "[2/5] Restoring .NET dependencies..." -ForegroundColor Yellow
    dotnet restore --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed"
    }
    Write-Output "  ✓ .NET dependencies restored" -ForegroundColor Green
    Write-Output ""

    Write-Output "[3/5] Installing npm dependencies..." -ForegroundColor Yellow
    Push-Location "Aura.Web"
    try {
        npm install --loglevel=error
        if ($LASTEXITCODE -ne 0) {
            throw "npm install failed"
        }
        Write-Output "  ✓ npm dependencies installed" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
    Write-Output ""

    # Step 3: Build the solution
    Write-Output "[4/5] Building .NET projects..." -ForegroundColor Yellow
    $buildProjects = @(
        "Aura.Core/Aura.Core.csproj",
        "Aura.Providers/Aura.Providers.csproj",
        "Aura.Api/Aura.Api.csproj"
    )

    foreach ($project in $buildProjects) {
        Write-Output "  Building $project..." -ForegroundColor Gray
        dotnet build $project --configuration Release --nologo --verbosity quiet
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed for $project"
        }
    }
    Write-Output "  ✓ .NET projects built successfully" -ForegroundColor Green
    Write-Output ""

    # Step 4: Build the web UI
    Write-Output "[5/5] Building web UI..." -ForegroundColor Yellow
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

        Write-Output "  ✓ Web UI built successfully" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
    Write-Output ""

    # Success!
    Write-Output "════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Output " ✓ All validation checks passed!" -ForegroundColor Green
    Write-Output "════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Output ""
    Write-Output "The Windows 11 development workflow is working correctly." -ForegroundColor White
    Write-Output ""

    exit 0
}
catch {
    Write-Output ""
    Write-Output "════════════════════════════════════════════════════════" -ForegroundColor Red
    Write-Output " ✗ Validation failed!" -ForegroundColor Red
    Write-Output "════════════════════════════════════════════════════════" -ForegroundColor Red
    Write-Output ""
    Write-Output "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Output ""

    exit 1
}
finally {
    if ($pushedLocation) {
        Pop-Location
    }
}
