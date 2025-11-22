# Manual test script for directory validation with environment variables
# This script tests the /api/setup/check-directory endpoint on Windows

$ApiBaseUrl = "http://localhost:5005"

Write-Host "=========================================="
Write-Host "Manual Test: Directory Validation (Windows)"
Write-Host "=========================================="
Write-Host ""

# Test 1: Check directory with %TEMP% environment variable
Write-Host "Test 1: Windows environment variable (%TEMP%)"
$response1 = Invoke-RestMethod -Uri "$ApiBaseUrl/api/setup/check-directory" `
    -Method Post `
    -ContentType "application/json" `
    -Body '{"path":"%TEMP%"}'
Write-Host ($response1 | ConvertTo-Json -Depth 3)
Write-Host ""

# Test 2: Check directory with %USERPROFILE%\Videos\Aura
Write-Host "Test 2: Windows environment variable (%USERPROFILE%\Videos\Aura)"
$response2 = Invoke-RestMethod -Uri "$ApiBaseUrl/api/setup/check-directory" `
    -Method Post `
    -ContentType "application/json" `
    -Body '{"path":"%USERPROFILE%\\Videos\\Aura"}'
Write-Host ($response2 | ConvertTo-Json -Depth 3)
Write-Host ""

# Test 3: Check directory with multiple environment variables
Write-Host "Test 3: Multiple environment variables (%USERPROFILE%\%COMPUTERNAME%\Videos)"
$response3 = Invoke-RestMethod -Uri "$ApiBaseUrl/api/setup/check-directory" `
    -Method Post `
    -ContentType "application/json" `
    -Body '{"path":"%USERPROFILE%\\%COMPUTERNAME%\\Videos"}'
Write-Host ($response3 | ConvertTo-Json -Depth 3)
Write-Host ""

# Test 4: Invalid path
Write-Host "Test 4: Invalid path (empty string - should fail)"
$response4 = Invoke-RestMethod -Uri "$ApiBaseUrl/api/setup/check-directory" `
    -Method Post `
    -ContentType "application/json" `
    -Body '{"path":""}'
Write-Host ($response4 | ConvertTo-Json -Depth 3)
Write-Host ""

# Test 5: Non-existent environment variable
Write-Host "Test 5: Non-existent environment variable (%NONEXISTENT%\Videos)"
$response5 = Invoke-RestMethod -Uri "$ApiBaseUrl/api/setup/check-directory" `
    -Method Post `
    -ContentType "application/json" `
    -Body '{"path":"%NONEXISTENT%\\Videos"}'
Write-Host ($response5 | ConvertTo-Json -Depth 3)
Write-Host ""

# Test 6: Complete setup with environment variable
Write-Host "Test 6: Complete setup with %TEMP%\AuraTest"
$response6 = Invoke-RestMethod -Uri "$ApiBaseUrl/api/setup/complete" `
    -Method Post `
    -ContentType "application/json" `
    -Body '{"outputDirectory":"%TEMP%\\AuraTest","ffmpegPath":null}'
Write-Host ($response6 | ConvertTo-Json -Depth 3)
Write-Host ""

# Verify the directory was created
$expandedPath = [Environment]::ExpandEnvironmentVariables("%TEMP%\AuraTest")
if (Test-Path $expandedPath) {
    Write-Host "✓ Directory successfully created at: $expandedPath"
    # Cleanup
    Remove-Item $expandedPath -Force -ErrorAction SilentlyContinue
    Write-Host "✓ Cleanup complete"
} else {
    Write-Host "✗ Directory was NOT created at: $expandedPath"
}

Write-Host ""
Write-Host "=========================================="
Write-Host "Tests Complete!"
Write-Host "=========================================="
Write-Host ""
Write-Host "Expected Results:"
Write-Host "  - Test 1: Should return isValid=true with expandedPath (actual temp directory)"
Write-Host "  - Test 2: Should return isValid=true and create ~/Videos/Aura directory"
Write-Host "  - Test 3: Should handle multiple environment variables"
Write-Host "  - Test 4: Should return isValid=false with error message"
Write-Host "  - Test 5: Should handle non-existent variables gracefully"
Write-Host "  - Test 6: Should return success=true and create the directory"
Write-Host ""
Write-Host "Check the backend logs for detailed information about path expansion."
