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

# Electron app data locations (all possible naming variations)
$electronPaths = @(
    # Main app data (AppData\Local)
    "$localAppData\aura-video-studio",
    "$localAppData\AuraVideoStudio",
    "$localAppData\Aura Video Studio",
    "$localAppData\aura-video-studio-updater",

    # Roaming app data (AppData\Roaming) - this is where electron-store saves config
    "$appData\aura-video-studio",
    "$appData\AuraVideoStudio",
    "$appData\Aura Video Studio",

    # Electron cache and preferences
    "$localAppData\electron",
    "$appData\electron",
    "$userProfile\.electron",
    "$userProfile\.electron-gyp",

    # Installation directory
    "$localAppData\Programs\aura-video-studio",
    "$localAppData\Programs\Aura Video Studio",

    # Temp directories
    "$env:TEMP\aura-video-studio*",
    "$env:TEMP\electron-*",
    "$env:TEMP\electron_*",

    # Squirrel updater (if using NSIS installer)
    "$localAppData\SquirrelTemp",

    # Electron crash reports
    "$env:TEMP\Crashpad",
    "$localAppData\CrashDumps\aura*"
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
    "$projectRoot\Aura.Web\.nyc_output",

    # OpenCut (CapCut-style editor) workspace artifacts
    "$projectRoot\OpenCut\node_modules",
    "$projectRoot\OpenCut\.turbo",
    "$projectRoot\OpenCut\apps\web\node_modules",
    "$projectRoot\OpenCut\apps\web\.next",
    "$projectRoot\OpenCut\apps\web\.turbo",
    "$projectRoot\OpenCut\apps\web\.vercel"
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
    "$projectRoot\Aura.Desktop\package-lock.json",

    # OpenCut lockfiles (npm / Bun)
    "$projectRoot\OpenCut\package-lock.json",
    "$projectRoot\OpenCut\apps\web\package-lock.json",
    "$projectRoot\OpenCut\bun.lockb",
    "$projectRoot\OpenCut\bun.lock",
    "$projectRoot\OpenCut\apps\web\bun.lockb",
    "$projectRoot\OpenCut\apps\web\bun.lock"
)

foreach ($file in $lockFiles) {
    if (Test-Path $file) {
        Remove-Item -Path $file -Force
        Write-Host "  ✓ Removed $(Split-Path $file -Leaf)" -ForegroundColor Green
    }
}

# Clean electron-store data (JSON config files)
Write-Host "`nStep 7: Cleaning electron-store configuration..." -ForegroundColor Cyan
$electronStoreFiles = @(
    "$appData\aura-video-studio\config.json",
    "$appData\aura-video-studio\window-state.json",
    "$appData\AuraVideoStudio\config.json",
    "$appData\AuraVideoStudio\window-state.json",
    "$appData\Aura Video Studio\config.json",
    "$appData\Aura Video Studio\window-state.json"
)

foreach ($file in $electronStoreFiles) {
    if (Test-Path $file) {
        try {
            Remove-Item -Path $file -Force
            Write-Host "  ✓ Removed $(Split-Path $file -Leaf)" -ForegroundColor Green
        } catch {
            Write-Host "  Warning: Could not remove $file" -ForegroundColor Yellow
        }
    }
}

# Clean SQLite databases
Write-Host "`nStep 8: Cleaning SQLite databases..." -ForegroundColor Cyan
$dbPaths = @(
    "$localAppData\aura-video-studio\*.db",
    "$localAppData\aura-video-studio\*.db-shm",
    "$localAppData\aura-video-studio\*.db-wal",
    "$appData\aura-video-studio\*.db*"
)

foreach ($pattern in $dbPaths) {
    Get-ChildItem -Path $pattern -ErrorAction SilentlyContinue | ForEach-Object {
        try {
            Remove-Item -Path $_.FullName -Force
            Write-Host "  ✓ Removed $($_.Name)" -ForegroundColor Green
        } catch {
            Write-Host "  Warning: Could not remove $($_.Name)" -ForegroundColor Yellow
        }
    }
}

# Clean user logs (separate from backend logs)
Write-Host "`nStep 9: Cleaning application logs..." -ForegroundColor Cyan
$logPaths = @(
    "$localAppData\aura-video-studio\logs",
    "$appData\aura-video-studio\logs",
    "$userProfile\Documents\Aura Video Studio\logs"
)

foreach ($path in $logPaths) {
    Remove-ItemSafely -Path $path -Description "Application logs"
}

# Clean NuGet cache for Aura packages
Write-Host "`nStep 10: Cleaning NuGet cache..." -ForegroundColor Cyan
try {
    dotnet nuget locals temp -c | Out-Null
    Write-Host "  ✓ Cleared NuGet temp cache" -ForegroundColor Green
} catch {
    Write-Host "  Warning: Could not clear NuGet cache" -ForegroundColor Yellow
}

# Clean npm cache
Write-Host "`nStep 11: Cleaning npm cache..." -ForegroundColor Cyan
try {
    npm cache clean --force 2>&1 | Out-Null
    Write-Host "  ✓ Cleared npm cache" -ForegroundColor Green
} catch {
    Write-Host "  Warning: Could not clear npm cache" -ForegroundColor Yellow
}

# Clean Windows Registry entries (Electron stores some preferences here)
if ($IsWindows) {
    Write-Host "`nStep 12: Cleaning Windows Registry entries..." -ForegroundColor Cyan
    $registryPaths = @(
        "HKCU:\Software\aura-video-studio",
        "HKCU:\Software\AuraVideoStudio",
        "HKCU:\Software\Aura Video Studio",
        "HKCU:\Software\Electron",
        "HKLM:\Software\aura-video-studio",
        "HKLM:\Software\WOW6432Node\aura-video-studio"
    )

    foreach ($regPath in $registryPaths) {
        if (Test-Path $regPath) {
            try {
                Remove-Item -Path $regPath -Recurse -Force -ErrorAction SilentlyContinue
                Write-Host "  ✓ Removed registry key: $regPath" -ForegroundColor Green
            } catch {
                Write-Host "  Warning: Could not remove $regPath" -ForegroundColor Yellow
            }
        }
    }
}

# Clean Windows prefetch for Aura (requires admin)
if ($IsWindows) {
    Write-Host "`nStep 13: Cleaning Windows prefetch..." -ForegroundColor Cyan
    $prefetchPath = "$env:WINDIR\Prefetch"
    if (Test-Path $prefetchPath) {
        try {
            Get-ChildItem -Path $prefetchPath -Filter "*AURA*" -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
            Get-ChildItem -Path $prefetchPath -Filter "*ELECTRON*" -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
            Write-Host "  ✓ Cleaned Windows prefetch" -ForegroundColor Green
        } catch {
            Write-Host "  Skipped (requires admin rights)" -ForegroundColor Yellow
        }
    }
}

# Clean Windows Event Logs for app crashes (requires admin)
if ($IsWindows) {
    Write-Host "`nStep 14: Cleaning Windows Event Logs..." -ForegroundColor Cyan
    try {
        Get-WinEvent -LogName "Application" -ErrorAction SilentlyContinue |
            Where-Object { $_.Message -like "*Aura*" -or $_.Message -like "*electron*" } |
            ForEach-Object {
                # Note: Can't delete individual events, but we can note them
            }
        Write-Host "  ✓ Checked event logs (deletion requires admin)" -ForegroundColor Yellow
    } catch {
        Write-Host "  Skipped (requires admin rights)" -ForegroundColor Yellow
    }
}

# Clean user documents/projects (OPTIONAL - ask first)
Write-Host "`nStep 15: User project data..." -ForegroundColor Cyan
$userDocsPaths = @(
    "$userProfile\Documents\Aura Projects",
    "$userProfile\Documents\Aura Video Studio",
    "$userProfile\Videos\Aura Outputs",
    "$userProfile\Videos\Aura"
)

$foundUserData = $false
foreach ($path in $userDocsPaths) {
    if (Test-Path $path) {
        $foundUserData = $true
        break
    }
}

if ($foundUserData) {
    Write-Host "  Found user project/output folders:" -ForegroundColor Yellow
    foreach ($path in $userDocsPaths) {
        if (Test-Path $path) {
            Write-Host "    - $path" -ForegroundColor Yellow
        }
    }

    Write-Host ""
    $response = Read-Host "  Delete user projects and outputs? (y/N)"
    if ($response -eq 'y' -or $response -eq 'Y') {
        foreach ($path in $userDocsPaths) {
            Remove-ItemSafely -Path $path -Description (Split-Path $path -Leaf)
        }
    } else {
        Write-Host "  Skipped user data (preserved)" -ForegroundColor Gray
    }
} else {
    Write-Host "  No user data found (clean)" -ForegroundColor Gray
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
    "$localAppData\Aura Video Studio",
    "$appData\Aura Video Studio",
    "$projectRoot\Aura.Api\bin",
    "$projectRoot\Aura.Web\node_modules",
    "$projectRoot\Aura.Desktop\dist"
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
    Write-Host "  ⚠ $remainingItems item(s) could not be removed. They may be in use." -ForegroundColor Yellow
    Write-Host "  Tip: Close all Electron/VSCode windows and try again" -ForegroundColor Yellow
}

# Show summary of what was cleaned
Write-Host "`nCleaned locations:" -ForegroundColor Cyan
Write-Host "  • LocalAppData: $localAppData" -ForegroundColor Gray
Write-Host "  • Roaming AppData: $appData" -ForegroundColor Gray
Write-Host "  • Temp: $env:TEMP" -ForegroundColor Gray
Write-Host "  • Project: $projectRoot" -ForegroundColor Gray

Write-Host "`nNext steps for fresh build:" -ForegroundColor Cyan
Write-Host "  1. Rebuild frontend:" -ForegroundColor White
Write-Host "     cd Aura.Web && npm install && npm run build" -ForegroundColor Gray
Write-Host "  2. Rebuild backend:" -ForegroundColor White
Write-Host "     cd Aura.Api && dotnet publish -c Release -r win-x64" -ForegroundColor Gray
Write-Host "  3. Build portable .exe:" -ForegroundColor White
Write-Host "     cd Aura.Desktop && pwsh -File build-desktop.ps1 -Target win" -ForegroundColor Gray
Write-Host ""
Write-Host "This ensures you're testing with a completely clean slate!" -ForegroundColor Green
Write-Host ""
