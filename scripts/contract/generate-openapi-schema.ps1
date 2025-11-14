#!/usr/bin/env pwsh
# Generate OpenAPI schema for contract testing

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = (Get-Item $ScriptDir).Parent.Parent.FullName
$ApiDir = Join-Path $ProjectRoot "Aura.Api"
$OutputDir = Join-Path $ProjectRoot "tests\contracts\schemas"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "OpenAPI Schema Generation" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# Create output directory
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# Build the API project
Write-Output ""
Write-Host "Building Aura.Api project..." -ForegroundColor Yellow
Set-Location $ApiDir
dotnet build --configuration Release --no-restore

# Start the API server temporarily
Write-Output ""
Write-Host "Starting API server to generate schema..." -ForegroundColor Yellow
$ApiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --no-build --configuration Release --urls http://localhost:5555" -PassThru -NoNewWindow

# Wait for API to start
Write-Host "Waiting for API to be ready..." -ForegroundColor Yellow
$maxAttempts = 30
$attempt = 0
$isReady = $false

while ($attempt -lt $maxAttempts) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5555/health/live" -UseBasicParsing -TimeoutSec 2 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            Write-Host "API is ready!" -ForegroundColor Green
            $isReady = $true
            break
        }
    } catch {
        # Ignore errors while waiting
        Write-Verbose "API not ready: $_"
    }

    $attempt++
    Write-Host "  Attempt $attempt/$maxAttempts..." -ForegroundColor Gray
    Start-Sleep -Seconds 2
}

if (-not $isReady) {
    Write-Host "ERROR: API failed to start within timeout" -ForegroundColor Red
    Stop-Process -Id $ApiProcess.Id -Force -ErrorAction SilentlyContinue
    exit 1
}

# Download the OpenAPI schema
Write-Output ""
Write-Host "Downloading OpenAPI schema..." -ForegroundColor Yellow
try {
    $schemaUrl = "http://localhost:5555/swagger/v1/swagger.json"
    $outputFile = Join-Path $OutputDir "openapi-v1.json"
    Invoke-WebRequest -Uri $schemaUrl -OutFile $outputFile -UseBasicParsing

    Write-Host "✓ Schema saved to: $outputFile" -ForegroundColor Green

    # Pretty print JSON if possible
    try {
        $json = Get-Content $outputFile -Raw | ConvertFrom-Json | ConvertTo-Json -Depth 100
        Set-Content -Path $outputFile -Value $json
        Write-Host "✓ Schema formatted" -ForegroundColor Green
    } catch {
        Write-Host "  (JSON formatting skipped)" -ForegroundColor Gray
    }
} catch {
    Write-Host "ERROR: Failed to download schema" -ForegroundColor Red
    Stop-Process -Id $ApiProcess.Id -Force -ErrorAction SilentlyContinue
    exit 1
}

# Stop the API server
Write-Output ""
Write-Host "Stopping API server..." -ForegroundColor Yellow
Stop-Process -Id $ApiProcess.Id -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Generate schema summary
Write-Output ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Schema Summary" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

try {
    $schema = Get-Content $outputFile -Raw | ConvertFrom-Json
    $pathsCount = ($schema.paths | Get-Member -MemberType NoteProperty).Count
    $schemasCount = if ($schema.components.schemas) { ($schema.components.schemas | Get-Member -MemberType NoteProperty).Count } else { 0 }

    Write-Host "Total endpoints: $pathsCount" -ForegroundColor White
    Write-Host "Total schemas: $schemasCount" -ForegroundColor White
    Write-Output ""
    Write-Host "Endpoints by controller:" -ForegroundColor White

    $controllers = @{}
    $schema.paths | Get-Member -MemberType NoteProperty | ForEach-Object {
        $path = $_.Name
        if ($path -match '/api/([^/]+)') {
            $controller = $matches[1]
            if ($controllers.ContainsKey($controller)) {
                $controllers[$controller]++
            } else {
                $controllers[$controller] = 1
            }
        }
    }

    $controllers.GetEnumerator() | Sort-Object Value -Descending | ForEach-Object {
        Write-Host "  $($_.Value) $($_.Key)" -ForegroundColor Gray
    }
} catch {
    Write-Host "Could not parse schema for summary" -ForegroundColor Yellow
}

Write-Output ""
Write-Host "✓ OpenAPI schema generation complete" -ForegroundColor Green
