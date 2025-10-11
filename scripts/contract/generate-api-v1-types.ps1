<#
.SYNOPSIS
    Generate TypeScript types from API OpenAPI spec

.DESCRIPTION
    This script:
    1. Starts the API server temporarily
    2. Fetches the OpenAPI JSON from /swagger/v1/swagger.json
    3. Runs openapi-typescript to generate TS types
    4. Saves the result to Aura.Web/src/types/api-v1.ts

.EXAMPLE
    .\scripts\contract\generate-api-v1-types.ps1
#>

$ErrorActionPreference = "Stop"

$REPO_ROOT = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$API_PROJECT = Join-Path $REPO_ROOT "Aura.Api"
$OUTPUT_FILE = Join-Path $REPO_ROOT "Aura.Web\src\types\api-v1.ts"
$SWAGGER_URL = "http://localhost:5000/swagger/v1/swagger.json"
$API_PORT = 5000

Write-Host "🚀 Generating API V1 TypeScript types from OpenAPI spec...`n" -ForegroundColor Cyan

# Check if openapi-typescript is available
try {
    npx openapi-typescript --version | Out-Null
    Write-Host "✅ openapi-typescript found`n" -ForegroundColor Green
} catch {
    Write-Host "❌ openapi-typescript not found. Installing..." -ForegroundColor Yellow
    npm install -g openapi-typescript
    Write-Host "✅ openapi-typescript installed`n" -ForegroundColor Green
}

# Start API server
Write-Host "1️⃣  Starting API server on port $API_PORT..." -ForegroundColor Cyan
$env:ASPNETCORE_ENVIRONMENT = "Development"
$apiProcess = Start-Process -FilePath "dotnet" `
    -ArgumentList "run", "--no-build", "--urls", "http://localhost:$API_PORT" `
    -WorkingDirectory $API_PROJECT `
    -PassThru `
    -WindowStyle Hidden

# Wait for server to start
Write-Host "⏳ Waiting for server to be ready..." -ForegroundColor Yellow
$maxAttempts = 30
$attempt = 0
$serverReady = $false

while ($attempt -lt $maxAttempts -and -not $serverReady) {
    Start-Sleep -Seconds 1
    $attempt++
    
    try {
        $response = Invoke-WebRequest -Uri $SWAGGER_URL -Method GET -TimeoutSec 2 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            $serverReady = $true
            Write-Host "✅ API server is running`n" -ForegroundColor Green
        }
    } catch {
        # Server not ready yet, continue waiting
    }
}

if (-not $serverReady) {
    Write-Host "❌ API server failed to start within 30 seconds" -ForegroundColor Red
    Stop-Process -Id $apiProcess.Id -Force
    exit 1
}

try {
    # Fetch OpenAPI spec
    Write-Host "2️⃣  Fetching OpenAPI spec from $SWAGGER_URL" -ForegroundColor Cyan
    $openApiJson = Invoke-RestMethod -Uri $SWAGGER_URL -Method GET
    
    $tempFile = Join-Path $PSScriptRoot "openapi.json"
    $openApiJson | ConvertTo-Json -Depth 100 | Out-File -FilePath $tempFile -Encoding UTF8
    
    Write-Host "✅ OpenAPI spec fetched`n" -ForegroundColor Green
    
    # Generate TypeScript types
    Write-Host "3️⃣  Generating TypeScript types with openapi-typescript..." -ForegroundColor Cyan
    npx openapi-typescript "$tempFile" --output "$OUTPUT_FILE"
    
    # Clean up temp file
    Remove-Item $tempFile -Force
    
    Write-Host "✅ TypeScript types generated`n" -ForegroundColor Green
    
    # Add header comment
    Write-Host "4️⃣  Adding header comment..." -ForegroundColor Cyan
    
    $header = @"
/**
 * AUTO-GENERATED - DO NOT EDIT
 * 
 * API V1 Type Definitions
 * Generated from OpenAPI spec at $SWAGGER_URL
 * 
 * To regenerate:
 *   .\scripts\contract\generate-api-v1-types.ps1
 *   node scripts/contract/generate-api-v1-types.js
 * 
 * Last generated: $(Get-Date -Format "o")
 */

"@

    $content = Get-Content $OUTPUT_FILE -Raw
    $header + $content | Out-File -FilePath $OUTPUT_FILE -Encoding UTF8 -NoNewline
    
    Write-Host "✅ Header added`n" -ForegroundColor Green
    Write-Host "🎉 Done! TypeScript types saved to $OUTPUT_FILE" -ForegroundColor Green
    
} finally {
    # Stop API server
    Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
    Write-Host "`n🛑 API server stopped" -ForegroundColor Yellow
}
