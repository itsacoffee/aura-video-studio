#!/usr/bin/env pwsh
# Publish script for Aura CLI
# Creates portable, single-file executables for Windows and Linux

param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "./artifacts/cli"
)

$ErrorActionPreference = "Stop"

Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║         Aura CLI - Publish Script                       ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

$ProjectPath = "../../Aura.Cli/Aura.Cli.csproj"
$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

# Create output directory
$OutputPath = Join-Path $OutputDir $Timestamp
New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null

Write-Host "[1/3] Publishing Windows x64 (single-file, portable)..." -ForegroundColor Yellow
$WinOutputPath = Join-Path $OutputPath "bin-win-x64"
dotnet publish $ProjectPath `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishTrimmed=false `
    -o $WinOutputPath

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Windows publish failed" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Windows binary: $WinOutputPath/aura-cli.exe" -ForegroundColor Green
Write-Host ""

Write-Host "[2/3] Publishing Linux x64 (framework-dependent)..." -ForegroundColor Yellow
$LinuxOutputPath = Join-Path $OutputPath "bin-linux-x64"
dotnet publish $ProjectPath `
    -c $Configuration `
    -r linux-x64 `
    --self-contained false `
    -o $LinuxOutputPath

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Linux publish failed" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Linux binary: $LinuxOutputPath/aura-cli" -ForegroundColor Green
Write-Host ""

Write-Host "[3/3] Creating distribution archive..." -ForegroundColor Yellow
$ZipPath = Join-Path $OutputDir "aura-cli-$Timestamp.zip"

# Create a simple directory structure for the archive
$TempDir = Join-Path ([System.IO.Path]::GetTempPath()) "aura-cli-dist"
if (Test-Path $TempDir) {
    Remove-Item -Recurse -Force $TempDir
}
New-Item -ItemType Directory -Force -Path $TempDir | Out-Null

# Copy binaries
Copy-Item -Recurse $WinOutputPath (Join-Path $TempDir "windows")
Copy-Item -Recurse $LinuxOutputPath (Join-Path $TempDir "linux")

# Create README
$ReadmeContent = @"
Aura CLI - Portable Distribution
=================================

This archive contains portable executables for:
- Windows x64 (self-contained, single-file)
- Linux x64 (requires .NET 8 runtime)

Installation
------------

Windows:
  1. Extract the 'windows' folder
  2. Add to PATH or run directly: aura-cli.exe help

Linux:
  1. Extract the 'linux' folder
  2. Ensure .NET 8 is installed: https://dot.net
  3. Make executable: chmod +x aura-cli
  4. Run: ./aura-cli help

Usage
-----
  aura-cli help              Show help
  aura-cli preflight -v      Check system requirements
  aura-cli quick -t "Topic"  Quick video generation

For more information, see: https://github.com/Coffee285/aura-video-studio
"@

$ReadmeContent | Out-File -FilePath (Join-Path $TempDir "README.txt") -Encoding UTF8

# Create the archive
Compress-Archive -Path (Join-Path $TempDir "*") -DestinationPath $ZipPath -Force

# Cleanup
Remove-Item -Recurse -Force $TempDir

Write-Host "  ✓ Archive created: $ZipPath" -ForegroundColor Green
Write-Host ""

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✓ Publish complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Output location:" -ForegroundColor Cyan
Write-Host "  $OutputPath" -ForegroundColor White
Write-Host ""
Write-Host "Binary sizes:" -ForegroundColor Cyan
$WinExe = Join-Path $WinOutputPath "aura-cli.exe"
$LinuxExe = Join-Path $LinuxOutputPath "aura-cli"
if (Test-Path $WinExe) {
    $WinSize = [math]::Round((Get-Item $WinExe).Length / 1MB, 2)
    Write-Host "  Windows: $WinSize MB" -ForegroundColor White
}
if (Test-Path $LinuxExe) {
    $LinuxSize = [math]::Round((Get-Item $LinuxExe).Length / 1MB, 2)
    Write-Host "  Linux: $LinuxSize MB" -ForegroundColor White
}
Write-Host ""
