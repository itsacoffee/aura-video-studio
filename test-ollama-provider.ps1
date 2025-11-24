#!/usr/bin/env pwsh
# Test script to diagnose Ollama provider issues

Write-Host "=== Ollama Provider Diagnostic Test ===" -ForegroundColor Cyan
Write-Host ""

# Check if Ollama is running
Write-Host "1. Checking if Ollama is running..." -ForegroundColor Yellow
try {
    $ollamaVersion = Invoke-RestMethod -Uri "http://127.0.0.1:11434/api/version" -Method Get -TimeoutSec 5 -ErrorAction Stop
    Write-Host "✓ Ollama is running" -ForegroundColor Green
    Write-Host "  Version info: $ollamaVersion" -ForegroundColor Gray
} catch {
    Write-Host "✗ Ollama is NOT running or not accessible" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  Please start Ollama with 'ollama serve'" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Check available models
Write-Host "2. Checking available Ollama models..." -ForegroundColor Yellow
try {
    $models = Invoke-RestMethod -Uri "http://127.0.0.1:11434/api/tags" -Method Get -TimeoutSec 5 -ErrorAction Stop
    if ($models.models -and $models.models.Count -gt 0) {
        Write-Host "✓ Found $($models.models.Count) model(s)" -ForegroundColor Green
        foreach ($model in $models.models) {
            Write-Host "  - $($model.name)" -ForegroundColor Gray
        }
    } else {
        Write-Host "✗ No models found" -ForegroundColor Red
        Write-Host "  Please pull a model with 'ollama pull llama3.1'" -ForegroundColor Yellow
    }
} catch {
    Write-Host "✗ Failed to get models" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test a simple generation
Write-Host "3. Testing simple generation with Ollama..." -ForegroundColor Yellow
$testPayload = @{
    model = "llama3.1:8b-q4_k_m"
    prompt = "Say hello in one word"
    stream = $false
    options = @{
        temperature = 0.7
        num_predict = 10
    }
} | ConvertTo-Json

try {
    Write-Host "  Sending test request..." -ForegroundColor Gray
    $response = Invoke-RestMethod -Uri "http://127.0.0.1:11434/api/generate" `
        -Method Post `
        -Body $testPayload `
        -ContentType "application/json" `
        -TimeoutSec 60 `
        -ErrorAction Stop
    
    if ($response.response) {
        Write-Host "✓ Generation successful!" -ForegroundColor Green
        Write-Host "  Response: $($response.response)" -ForegroundColor Gray
    } else {
        Write-Host "✗ Generation failed - no response" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ Generation failed" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Diagnostic Complete ===" -ForegroundColor Cyan
