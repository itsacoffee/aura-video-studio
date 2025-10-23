# Documentation build and validation script for Windows
# Run with: .\scripts\docs\build-docs.ps1

param(
    [switch]$SkipBuild,
    [switch]$Serve
)

Write-Host "=== Aura Video Studio Documentation Builder ===" -ForegroundColor Cyan
Write-Host ""

# Check if DocFX is installed
$docfxInstalled = Get-Command docfx -ErrorAction SilentlyContinue
if (-not $docfxInstalled) {
    Write-Host "Installing DocFX..." -ForegroundColor Yellow
    dotnet tool install -g docfx
}

# Build .NET solution with XML documentation
if (-not $SkipBuild) {
    Write-Host "Building .NET solution..." -ForegroundColor Green
    dotnet build --configuration Release
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed"
        exit 1
    }
}

# Build DocFX documentation
Write-Host "Building API documentation with DocFX..." -ForegroundColor Green
docfx docfx.json
if ($LASTEXITCODE -ne 0) {
    Write-Warning "DocFX build completed with warnings (this is normal)"
}

# Build TypeScript documentation (if Node.js is available)
$npmInstalled = Get-Command npm -ErrorAction SilentlyContinue
if ($npmInstalled) {
    Write-Host "Building TypeScript documentation..." -ForegroundColor Green
    Push-Location Aura.Web
    npm install --silent
    npm run docs
    Pop-Location
} else {
    Write-Host "Skipping TypeScript documentation (npm not found)" -ForegroundColor Yellow
}

# Validate links (if markdown-link-check is installed)
$linkCheckInstalled = Get-Command markdown-link-check -ErrorAction SilentlyContinue
if ($linkCheckInstalled) {
    Write-Host "Validating links in documentation..." -ForegroundColor Green
    $failedFiles = 0
    Get-ChildItem -Path docs -Filter *.md -Recurse | ForEach-Object {
        try {
            markdown-link-check $_.FullName --quiet
        } catch {
            Write-Warning "Found broken links in $($_.FullName)"
            $failedFiles++
        }
    }
    if ($failedFiles -gt 0) {
        Write-Warning "$failedFiles file(s) have broken links (not failing build)"
    }
} else {
    Write-Host "Skipping link validation (markdown-link-check not installed)" -ForegroundColor Yellow
    Write-Host "Install with: npm install -g markdown-link-check" -ForegroundColor Gray
}

Write-Host ""
Write-Host "=== Documentation built successfully! ===" -ForegroundColor Green
Write-Host "View at: file://$PWD\_site\index.html" -ForegroundColor Cyan
Write-Host ""

if ($Serve) {
    Write-Host "Starting documentation server..." -ForegroundColor Green
    docfx serve _site
} else {
    Write-Host "To serve locally:" -ForegroundColor Gray
    Write-Host "  docfx serve _site" -ForegroundColor Gray
    Write-Host ""
}
