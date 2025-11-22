#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host "====================================" -ForegroundColor Cyan
Write-Host "  Aura Video Studio - Deep Clean" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

$scriptRoot = $PSScriptRoot
$projectRoot = (Get-Item $scriptRoot).Parent.FullName

# Function to safely remove items
function Remove-ItemSafely {
    param([string]$Path, [string]$Description)
    
    if (Test-Path $Path) {
        try {
            Write-Host "  Removing $Description..." -ForegroundColor Yellow
            
            # First try to remove normally
            Remove-Item -Path $Path -Recurse -Force -ErrorAction SilentlyContinue
            
            # If it still exists, try with robocopy (Windows)
            if ((Test-Path $Path) -and $IsWindows) {
                $tempPath = "$env:TEMP\ToDelete_$(Get-Random)"
                New-Item -ItemType Directory -Path $tempPath -Force | Out-Null
                robocopy $tempPath $Path /MIR /R:0 /W:0 /NFL /NDL /NJH /NJS /NC /NS /NP | Out-Null
                Remove-Item -Path $tempPath -Recurse -Force
                Remove-Item -Path $Path -Recurse -Force -ErrorAction SilentlyContinue
            }
            
            if (Test-Path $Path) {
                Write-Host "    Warning: Could not fully remove $Description" -ForegroundColor Red
            } else {
                Write-Host "    ✓ Removed $Description" -ForegroundColor Green
            }
        }
        catch {
            Write-Host "    Error removing ${Description}: $_" -ForegroundColor Red
        }
    } else {
        Write-Host "  $Description not found (already clean)" -ForegroundColor Gray
    }
}

# Kill any running Aura processes
Write-Host "Step 1: Terminating Aura processes..." -ForegroundColor Cyan
$processes = @("Aura Video Studio", "Aura.Api", "electron", "node")
foreach ($proc in $processes) {
    Get-Process -Name $proc -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
}
Start-Sleep -Seconds 2

# Clean Electron/Application data
Write-Host "`nStep 2: Cleaning Electron application data..." -ForegroundColor Cyan

# Get user data paths
$appData = [Environment]::GetFolderPath('ApplicationData')
$localAppData = [Environment]::GetFolderPath('LocalApplicationData')
$userProfile = [Environment]::GetFolderPath('UserProfile')

# Electron app data locations
$electronPaths = @(
    "$localAppData\aura-video-studio",
    "$localAppData\AuraVideoStudio",
    "$appData\aura-video-studio",
    "$appData\AuraVideoStudio",
    "$localAppData\electron",
    "$appData\electron",
    "$userProfile\.electron",
    "$localAppData\Programs\aura-video-studio",
    "$env:TEMP\aura-video-studio-*",
    "$env:TEMP\electron-*"
)

foreach ($path in $electronPaths) {
    if ($path.Contains('*')) {
        Get-ChildItem -Path $path -ErrorAction SilentlyContinue | ForEach-Object {
            Remove-ItemSafely -Path $_.FullName -Description $_.Name
        }
    } else {
        Remove-ItemSafely -Path $path -Description (Split-Path $path -Leaf)
    }
}

# Clean Chromium cache (Electron uses Chromium)
Write-Host "`nStep 3: Cleaning Chromium/Electron cache..." -ForegroundColor Cyan
$chromiumPaths = @(
    "$localAppData\Google\Chrome\User Data\Default\Cache",
    "$localAppData\Google\Chrome\User Data\Default\Code Cache",
    "$localAppData\Google\Chrome\User Data\Default\Storage",
    "$localAppData\Google\Chrome\User Data\Default\IndexedDB",
    "$localAppData\Google\Chrome\User Data\Default\Local Storage",
    "$localAppData\Google\Chrome\User Data\Default\Session Storage",
    "$localAppData\Chromium\User Data\Default\Cache",
    "$localAppData\Chromium\User Data\Default\IndexedDB"
)

foreach ($path in $chromiumPaths) {
    Remove-ItemSafely -Path $path -Description (Split-Path $path -Leaf)
}

# Clean .NET application data
Write-Host "`nStep 4: Cleaning .NET application data..." -ForegroundColor Cyan
$dotnetPaths = @(
    "$localAppData\Aura",
    "$localAppData\Aura.Api",
    "$appData\Aura",
    "$appData\Aura.Api",
    "$env:TEMP\Aura*",
    "$env:TEMP\.net\Aura*"
)

foreach ($path in $dotnetPaths) {
    if ($path.Contains('*')) {
        Get-ChildItem -Path (Split-Path $path) -Filter (Split-Path $path -Leaf) -ErrorAction SilentlyContinue | ForEach-Object {
            Remove-ItemSafely -Path $_.FullName -Description $_.Name
        }
    } else {
        Remove-ItemSafely -Path $path -Description (Split-Path $path -Leaf)
    }
}

# Clean project build artifacts
Write-Host "`nStep 5: Cleaning project build artifacts..." -ForegroundColor Cyan

# Frontend artifacts
$frontendPaths = @(
    "$projectRoot\Aura.Web\node_modules",
    "$projectRoot\Aura.Web\dist",
    "$projectRoot\Aura.Web\.vite",
    "$projectRoot\Aura.Web\coverage",
    "$projectRoot\Aura.Web\.nyc_output"
)

foreach ($path in $frontendPaths) {
    Remove-ItemSafely -Path $path -Description (Split-Path $path -Leaf)
}

# Backend artifacts
$backendPaths = @(
    "$projectRoot\Aura.Api\bin",
    "$projectRoot\Aura.Api\obj",
    "$projectRoot\Aura.Api\logs",
    "$projectRoot\Aura.Core\bin",
    "$projectRoot\Aura.Core\obj",
    "$projectRoot\Aura.Providers\bin",
    "$projectRoot\Aura.Providers\obj",
    "$projectRoot\Aura.Tests\bin",
    "$projectRoot\Aura.Tests\obj",
    "$projectRoot\TestResults",
    "$projectRoot\.vs"
)

foreach ($path in $backendPaths) {
    Remove-ItemSafely -Path $path -Description (Split-Path $path -Leaf)
}

# Electron artifacts
$electronPaths = @(
    "$projectRoot\Aura.Desktop\dist",
    "$projectRoot\Aura.Desktop\out",
    "$projectRoot\Aura.Desktop\node_modules",
    "$projectRoot\dist-electron",
    "$projectRoot\release"
)

foreach ($path in $electronPaths) {
    Remove-ItemSafely -Path $path -Description (Split-Path $path -Leaf)
}

# Clean package-lock files to ensure fresh dependency resolution
Write-Host "`nStep 6: Cleaning package locks..." -ForegroundColor Cyan
$lockFiles = @(
    "$projectRoot\package-lock.json",
    "$projectRoot\Aura.Web\package-lock.json",
    "$projectRoot\Aura.Desktop\package-lock.json"
)

foreach ($file in $lockFiles) {
    if (Test-Path $file) {
        Remove-Item -Path $file -Force
        Write-Host "  ✓ Removed $(Split-Path $file -Leaf)" -ForegroundColor Green
    }
}

# Clean NuGet cache for Aura packages
Write-Host "`nStep 7: Cleaning NuGet cache..." -ForegroundColor Cyan
try {
    dotnet nuget locals temp -c | Out-Null
    Write-Host "  ✓ Cleared NuGet temp cache" -ForegroundColor Green
} catch {
    Write-Host "  Warning: Could not clear NuGet cache" -ForegroundColor Yellow
}

# Clean npm cache
Write-Host "`nStep 8: Cleaning npm cache..." -ForegroundColor Cyan
try {
    npm cache clean --force 2>&1 | Out-Null
    Write-Host "  ✓ Cleared npm cache" -ForegroundColor Green
} catch {
    Write-Host "  Warning: Could not clear npm cache" -ForegroundColor Yellow
}

# Clean Windows prefetch for Aura (requires admin)
if ($IsWindows) {
    Write-Host "`nStep 9: Cleaning Windows prefetch..." -ForegroundColor Cyan
    $prefetchPath = "$env:WINDIR\Prefetch"
    if (Test-Path $prefetchPath) {
        try {
            Get-ChildItem -Path $prefetchPath -Filter "*AURA*" -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
            Write-Host "  ✓ Cleaned Windows prefetch" -ForegroundColor Green
        } catch {
            Write-Host "  Skipped (requires admin rights)" -ForegroundColor Yellow
        }
    }
}

# Final verification
Write-Host "`n====================================" -ForegroundColor Cyan
Write-Host "  Cleanup Complete!" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Cyan

Write-Host "`nVerifying cleanup..." -ForegroundColor Cyan
$remainingItems = 0
$checkPaths = @(
    "$localAppData\aura-video-studio",
    "$appData\aura-video-studio",
    "$projectRoot\Aura.Api\bin",
    "$projectRoot\Aura.Web\node_modules"
)

foreach ($path in $checkPaths) {
    if (Test-Path $path) {
        Write-Host "  ⚠ Still exists: $path" -ForegroundColor Yellow
        $remainingItems++
    }
}

if ($remainingItems -eq 0) {
    Write-Host "  ✓ All major directories successfully cleaned" -ForegroundColor Green
} else {
    Write-Host "  ⚠ Some items could not be removed. They may be in use." -ForegroundColor Yellow
}

Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "  1. Run 'npm install' in the project root" -ForegroundColor White
Write-Host "  2. Run 'dotnet restore' in the project root" -ForegroundColor White
Write-Host "  3. Run 'npm run dev' to start in development mode" -ForegroundColor White
Write-Host ""
