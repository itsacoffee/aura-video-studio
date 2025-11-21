#!/usr/bin/env pwsh
# Build self-contained Aura.Api backend for Windows x64

param(
    [string]$Configuration = "Release",
    [string]$OutputPath = "dist/backend",
    [string]$ProjectRoot = $PSScriptRoot
)

Write-Host "Building Aura.Api backend for production..." -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host "Output Path: $OutputPath" -ForegroundColor Gray
Write-Host "Project Root: $ProjectRoot" -ForegroundColor Gray

# Resolve absolute paths
$absoluteOutputPath = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath
} else {
    Join-Path (Get-Location) $OutputPath
}

$projectFile = Join-Path $ProjectRoot "Aura.Api/Aura.Api.csproj"

Write-Host "Project File: $projectFile" -ForegroundColor Gray
Write-Host "Absolute Output Path: $absoluteOutputPath" -ForegroundColor Gray

# Verify project file exists
if (-not (Test-Path $projectFile)) {
    Write-Host "ERROR: Project file not found at: $projectFile" -ForegroundColor Red
    Write-Host "Current directory: $(Get-Location)" -ForegroundColor Yellow
    Write-Host "Script root: $PSScriptRoot" -ForegroundColor Yellow
    exit 1
}

# Clean output directory
if (Test-Path $absoluteOutputPath) {
    Write-Host "Cleaning previous build..." -ForegroundColor Yellow
    Remove-Item -Path $absoluteOutputPath -Recurse -Force
}

# Create output directory
New-Item -Path $absoluteOutputPath -ItemType Directory -Force | Out-Null

# Build self-contained executable
Write-Host "Publishing self-contained executable..." -ForegroundColor Cyan
dotnet publish $projectFile `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    --output $absoluteOutputPath `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishTrimmed=false

if ($LASTEXITCODE -ne 0) {
    Write-Host "Backend build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`nBackend build successful!" -ForegroundColor Green
Write-Host "Executable location: $absoluteOutputPath/Aura.Api.exe" -ForegroundColor Green

# Verify executable exists
$exePath = Join-Path $absoluteOutputPath "Aura.Api.exe"
if (Test-Path $exePath) {
    $fileInfo = Get-Item $exePath
    $sizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
    Write-Host "Executable size: $sizeMB MB" -ForegroundColor Gray
} else {
    Write-Host "ERROR: Aura.Api.exe not found at: $exePath" -ForegroundColor Red
    exit 1
}
