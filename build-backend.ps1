#!/usr/bin/env pwsh
# Build self-contained Aura.Api backend for Windows x64

param(
    [string]$Configuration = "Release",
    [string]$OutputPath = "dist/backend"
)

Write-Host "Building Aura.Api backend for production..." -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host "Output Path: $OutputPath" -ForegroundColor Gray

# Clean output directory
if (Test-Path $OutputPath) {
    Write-Host "Cleaning previous build..." -ForegroundColor Yellow
    Remove-Item -Path $OutputPath -Recurse -Force
}

# Build self-contained executable
Write-Host "Publishing self-contained executable..." -ForegroundColor Cyan
dotnet publish Aura.Api/Aura.Api.csproj `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    --output $OutputPath `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishTrimmed=false

if ($LASTEXITCODE -ne 0) {
    Write-Host "Backend build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`nBackend build successful!" -ForegroundColor Green
Write-Host "Executable location: $OutputPath/Aura.Api.exe" -ForegroundColor Green

# Verify executable exists
$exePath = Join-Path $OutputPath "Aura.Api.exe"
if (Test-Path $exePath) {
    $fileInfo = Get-Item $exePath
    $sizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
    Write-Host "Executable size: $sizeMB MB" -ForegroundColor Gray
} else {
    Write-Host "ERROR: Aura.Api.exe not found!" -ForegroundColor Red
    exit 1
}
