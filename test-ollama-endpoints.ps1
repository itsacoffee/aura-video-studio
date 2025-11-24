#!/usr/bin/env pwsh
# Comprehensive endpoint verification for Ollama provider integration

Write-Host "=== Ollama Provider Endpoint Verification ===" -ForegroundColor Cyan
Write-Host ""

$apiBaseUrl = "http://localhost:5005/api"
$ollamaBaseUrl = "http://127.0.0.1:11434"
$testsPassed = 0
$testsFailed = 0

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [int]$ExpectedStatusCode = 200,
        [string]$Description
    )
    
    Write-Host "Testing: $Name" -ForegroundColor Yellow
    Write-Host "  Description: $Description" -ForegroundColor Gray
    Write-Host "  Method: $Method $Url" -ForegroundColor Gray
    
    try {
        if ($Method -eq "GET") {
            $response = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec 30 -ErrorAction Stop
        } else {
            $response = Invoke-WebRequest -Uri $Url -Method $Method -TimeoutSec 30 -ErrorAction Stop
        }
        
        if ($response.StatusCode -eq $ExpectedStatusCode) {
            Write-Host "  ✓ PASS - Status: $($response.StatusCode)" -ForegroundColor Green
            $script:testsPassed++
            
            # Try to parse JSON response
            try {
                $content = $response.Content | ConvertFrom-Json
                Write-Host "  Response preview:" -ForegroundColor Gray
                Write-Host "    $($content | ConvertTo-Json -Depth 2 -Compress)" -ForegroundColor DarkGray
            } catch {
                Write-Host "  Response: $($response.Content.Substring(0, [Math]::Min(100, $response.Content.Length)))" -ForegroundColor DarkGray
            }
        } else {
            Write-Host "  ✗ FAIL - Expected $ExpectedStatusCode, got $($response.StatusCode)" -ForegroundColor Red
            $script:testsFailed++
        }
    } catch {
        Write-Host "  ✗ FAIL - Error: $($_.Exception.Message)" -ForegroundColor Red
        $script:testsFailed++
    }
    
    Write-Host ""
}

# First, verify Ollama itself is running
Write-Host "PART 1: Verifying Ollama Service" -ForegroundColor Cyan
Write-Host "===============================" -ForegroundColor Cyan
Write-Host ""

Test-Endpoint `
    -Name "Ollama Direct - /api/version" `
    -Method "GET" `
    -Url "$ollamaBaseUrl/api/version" `
    -Description "Direct connection to Ollama version endpoint"

Test-Endpoint `
    -Name "Ollama Direct - /api/tags" `
    -Method "GET" `
    -Url "$ollamaBaseUrl/api/tags" `
    -Description "Direct connection to Ollama models endpoint"

Write-Host ""
Write-Host "PART 2: Verifying API Endpoints" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Check if API is running
try {
    $healthCheck = Invoke-WebRequest -Uri "$apiBaseUrl/../health/live" -TimeoutSec 5 -ErrorAction Stop
    Write-Host "✓ API is running at $apiBaseUrl" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "✗ API is not accessible at $apiBaseUrl" -ForegroundColor Red
    Write-Host "  Please ensure Aura.Api is running" -ForegroundColor Yellow
    Write-Host "  Start with: dotnet run --project Aura.Api/Aura.Api.csproj" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

# Test OfflineProvidersController endpoints
Write-Host "Testing OfflineProvidersController" -ForegroundColor Yellow
Write-Host "-----------------------------------" -ForegroundColor DarkGray

Test-Endpoint `
    -Name "GET /api/offline-providers/status" `
    -Method "GET" `
    -Url "$apiBaseUrl/offline-providers/status" `
    -Description "Get status of all offline providers"

Test-Endpoint `
    -Name "GET /api/offline-providers/ollama" `
    -Method "GET" `
    -Url "$apiBaseUrl/offline-providers/ollama" `
    -Description "Check Ollama availability and get recommendations"

# Test ProvidersController Ollama endpoints
Write-Host "Testing ProvidersController Ollama Endpoints" -ForegroundColor Yellow
Write-Host "--------------------------------------------" -ForegroundColor DarkGray

Test-Endpoint `
    -Name "GET /api/providers/ollama/status" `
    -Method "GET" `
    -Url "$apiBaseUrl/providers/ollama/status" `
    -Description "Get Ollama service status with version and model count"

Test-Endpoint `
    -Name "GET /api/providers/ollama/models" `
    -Method "GET" `
    -Url "$apiBaseUrl/providers/ollama/models" `
    -Description "Get list of available Ollama models"

Test-Endpoint `
    -Name "GET /api/providers/ollama/running" `
    -Method "GET" `
    -Url "$apiBaseUrl/providers/ollama/running" `
    -Description "Check if Ollama is currently running"

# Test EnginesController Ollama endpoints
Write-Host "Testing EnginesController Ollama Endpoints" -ForegroundColor Yellow
Write-Host "------------------------------------------" -ForegroundColor DarkGray

Test-Endpoint `
    -Name "GET /api/engines/detect/ollama" `
    -Method "GET" `
    -Url "$apiBaseUrl/engines/detect/ollama" `
    -Description "Detect Ollama installation and available models"

Test-Endpoint `
    -Name "GET /api/engines/ollama/models" `
    -Method "GET" `
    -Url "$apiBaseUrl/engines/ollama/models" `
    -Description "Get Ollama models via engines controller"

# Test DiagnosticsController Ollama endpoint
Write-Host "Testing DiagnosticsController Ollama Endpoint" -ForegroundColor Yellow
Write-Host "---------------------------------------------" -ForegroundColor DarkGray

Test-Endpoint `
    -Name "GET /api/diagnostics/ollama/verify" `
    -Method "GET" `
    -Url "$apiBaseUrl/diagnostics/ollama/verify" `
    -Description "Verify Ollama installation and connectivity"

# Test SetupController Ollama endpoint
Write-Host "Testing SetupController Ollama Endpoint" -ForegroundColor Yellow
Write-Host "---------------------------------------" -ForegroundColor DarkGray

Test-Endpoint `
    -Name "GET /api/setup/ollama-status" `
    -Method "GET" `
    -Url "$apiBaseUrl/setup/ollama-status" `
    -Description "Get Ollama status for setup wizard"

Write-Host ""
Write-Host "PART 3: Testing Script Generation with Ollama" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host ""

# Test script generation endpoint
$scriptRequest = @{
    topic = "Test Topic"
    audience = "General"
    goal = "Test"
    tone = "Friendly"
    targetDurationSeconds = 30
    preferredProvider = "Ollama"
} | ConvertTo-Json

Write-Host "Testing script generation with Ollama provider..." -ForegroundColor Yellow
Write-Host "  POST /api/scripts/generate" -ForegroundColor Gray

try {
    $scriptResponse = Invoke-WebRequest `
        -Uri "$apiBaseUrl/scripts/generate" `
        -Method Post `
        -Body $scriptRequest `
        -ContentType "application/json" `
        -TimeoutSec 120 `
        -ErrorAction Stop
    
    if ($scriptResponse.StatusCode -eq 200) {
        Write-Host "  ✓ PASS - Script generation successful" -ForegroundColor Green
        $script:testsPassed++
        
        $scriptContent = $scriptResponse.Content | ConvertFrom-Json
        if ($scriptContent.script) {
            Write-Host "  Script preview: $($scriptContent.script.Substring(0, [Math]::Min(100, $scriptContent.script.Length)))..." -ForegroundColor DarkGray
        }
    } else {
        Write-Host "  ✗ FAIL - Status: $($scriptResponse.StatusCode)" -ForegroundColor Red
        $script:testsFailed++
    }
} catch {
    Write-Host "  ✗ FAIL - Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  This is expected if Ollama model is slow or not loaded" -ForegroundColor Yellow
    $script:testsFailed++
}

Write-Host ""
Write-Host "=== Test Summary ===" -ForegroundColor Cyan
Write-Host "Total Tests: $($testsPassed + $testsFailed)" -ForegroundColor White
Write-Host "Passed: $testsPassed" -ForegroundColor Green
Write-Host "Failed: $testsFailed" -ForegroundColor Red

if ($testsFailed -eq 0) {
    Write-Host ""
    Write-Host "✓ ALL TESTS PASSED!" -ForegroundColor Green
    exit 0
} else {
    Write-Host ""
    Write-Host "✗ SOME TESTS FAILED" -ForegroundColor Red
    Write-Host "  Check if:" -ForegroundColor Yellow
    Write-Host "  1. Ollama is running (ollama serve)" -ForegroundColor Yellow
    Write-Host "  2. API is running (dotnet run --project Aura.Api)" -ForegroundColor Yellow
    Write-Host "  3. Models are downloaded (ollama pull llama3.1)" -ForegroundColor Yellow
    exit 1
}
