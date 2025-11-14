# Verify SHA-256 Checksum for Downloaded Files
# Usage: .\verify_sha.ps1 -FilePath "path\to\file" -ExpectedHash "abc123..."

param(
    [Parameter(Mandatory=$true)]
    [string]$FilePath,

    [Parameter(Mandatory=$true)]
    [string]$ExpectedHash
)

# Check if file exists
if (-not (Test-Path $FilePath)) {
    Write-Host "ERROR: File not found: $FilePath" -ForegroundColor Red
    exit 1
}

Write-Host "Verifying SHA-256 checksum..." -ForegroundColor Cyan
Write-Output "File: $FilePath"

# Calculate SHA-256
$actualHash = (Get-FileHash -Path $FilePath -Algorithm SHA256).Hash

Write-Host "Expected: $ExpectedHash" -ForegroundColor Yellow
Write-Host "Actual:   $actualHash" -ForegroundColor Yellow

# Compare
if ($actualHash -eq $ExpectedHash) {
    Write-Host "✓ Checksum verified successfully!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "✗ Checksum mismatch! File may be corrupted or modified." -ForegroundColor Red
    Write-Host "Do not use this file." -ForegroundColor Red
    exit 1
}
