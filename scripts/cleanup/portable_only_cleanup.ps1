# Portable-Only Cleanup Script
# This script removes all MSIX/EXE packaging infrastructure to enforce portable-only distribution

param(
    [switch]$DryRun = $false
)

$ErrorActionPreference = "Stop"

Write-Output "=== Aura Video Studio - Portable-Only Cleanup ===" -ForegroundColor Cyan
Write-Output ""

if ($DryRun) {
    Write-Output "DRY RUN MODE - No files will be deleted" -ForegroundColor Yellow
    Write-Output ""
}

# Set root directory
$rootDir = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

# Define patterns to search for and delete
$patterns = @(
    # Inno Setup files
    "*.iss",

    # MSIX/APPX files
    "*.appx*",
    "*.msix*",
    "*.msixbundle",

    # Certificate files
    "*.cer"
)

# Define specific directories to search in scripts/packaging/
$packagingPatterns = @(
    "*msix*",
    "*inno*",
    "*setup*",
    "*installer*"
)

Write-Output "Step 1: Searching for artifact files..." -ForegroundColor Yellow
$filesToDelete = @()

# Search for artifact files in the entire repository
foreach ($pattern in $patterns) {
    $found = Get-ChildItem -Path $rootDir -Filter $pattern -Recurse -File -ErrorAction SilentlyContinue |
             Where-Object { $_.FullName -notmatch '[\\/]node_modules[\\/]' -and
                           $_.FullName -notmatch '[\\/]\.git[\\/]' }
    if ($found) {
        $filesToDelete += $found
    }
}

if ($filesToDelete.Count -eq 0) {
    Write-Output "  No artifact files found" -ForegroundColor Green
} else {
    Write-Output "  Found $($filesToDelete.Count) artifact file(s) to delete:" -ForegroundColor Yellow
    foreach ($file in $filesToDelete) {
        $relativePath = $file.FullName.Replace($rootDir, "").TrimStart("\", "/")
        Write-Output "    - $relativePath" -ForegroundColor White
    }
}

Write-Output ""
Write-Output "Step 2: Searching for MSIX/EXE packaging scripts in scripts/packaging/..." -ForegroundColor Yellow
$packagingDir = Join-Path $rootDir "scripts\packaging"
$packagingFilesToDelete = @()

if (Test-Path $packagingDir) {
    foreach ($pattern in $packagingPatterns) {
        $found = Get-ChildItem -Path $packagingDir -Filter $pattern -Recurse -ErrorAction SilentlyContinue
        if ($found) {
            $packagingFilesToDelete += $found
        }
    }
}

if ($packagingFilesToDelete.Count -eq 0) {
    Write-Output "  No MSIX/EXE packaging files found in scripts/packaging/" -ForegroundColor Green
} else {
    Write-Output "  Found $($packagingFilesToDelete.Count) packaging file(s) to delete:" -ForegroundColor Yellow
    foreach ($item in $packagingFilesToDelete) {
        $relativePath = $item.FullName.Replace($rootDir, "").TrimStart("\", "/")
        Write-Output "    - $relativePath" -ForegroundColor White
    }
}

Write-Output ""
Write-Output "Step 3: Summary" -ForegroundColor Yellow
$allItemsToDelete = $filesToDelete + $packagingFilesToDelete
Write-Output "  Total items to delete: $($allItemsToDelete.Count)" -ForegroundColor White

if ($allItemsToDelete.Count -eq 0) {
    Write-Output ""
    Write-Output "=== Cleanup Complete - No files to delete ===" -ForegroundColor Green
    exit 0
}

if ($DryRun) {
    Write-Output ""
    Write-Output "=== Dry Run Complete - No files were deleted ===" -ForegroundColor Green
    Write-Output "Run without -DryRun flag to actually delete files" -ForegroundColor Yellow
    exit 0
}

Write-Output ""
Write-Output "Proceeding with deletion..." -ForegroundColor Yellow

$deletedCount = 0
$failedCount = 0

foreach ($item in $allItemsToDelete) {
    try {
        $relativePath = $item.FullName.Replace($rootDir, "").TrimStart("\", "/")
        Remove-Item -Path $item.FullName -Recurse -Force
        Write-Output "  ✓ Deleted: $relativePath" -ForegroundColor Green
        $deletedCount++
    } catch {
        Write-Output "  ✗ Failed to delete: $relativePath - $($_.Exception.Message)" -ForegroundColor Red
        $failedCount++
    }
}

Write-Output ""
Write-Output "=== Cleanup Complete ===" -ForegroundColor Cyan
Write-Output "  Deleted: $deletedCount" -ForegroundColor Green
if ($failedCount -gt 0) {
    Write-Output "  Failed: $failedCount" -ForegroundColor Red
}
Write-Output ""
