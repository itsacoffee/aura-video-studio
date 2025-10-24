# Build Portable Distribution for Aura Video Studio
# This script provides a simple way to build the portable distribution

param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [int]$HealthCheckMaxAttempts = 30,
    [int]$HealthCheckIntervalSeconds = 1
)

$ErrorActionPreference = "Stop"
$buildStartTime = Get-Date
$buildSuccess = $true
$buildErrors = @()
$buildWarnings = @()

# Function to log and capture errors
function Write-BuildError {
    param([string]$message)
    $buildErrors += $message
    Write-Host "      ✗ ERROR: $message" -ForegroundColor Red
}

# Function to log warnings
function Write-BuildWarning {
    param([string]$message)
    $buildWarnings += $message
    Write-Host "      ⚠ WARNING: $message" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Aura Video Studio - Portable Builder" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration: $Configuration" -ForegroundColor White
Write-Host "Platform:      $Platform" -ForegroundColor White
Write-Host ""

# Validate prerequisites
Write-Host "[0/6] Validating prerequisites..." -ForegroundColor Yellow

# Check for dotnet
if (-not (Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
    Write-BuildError ".NET SDK not found in PATH. Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download"
    throw "Required prerequisite missing: .NET SDK"
}
$dotnetVersion = dotnet --version
Write-Host "      ✓ .NET SDK found (version $dotnetVersion)" -ForegroundColor Green

# Check for npm
if (-not (Get-Command "npm" -ErrorAction SilentlyContinue)) {
    Write-BuildError "npm not found in PATH. Please install Node.js from https://nodejs.org/"
    throw "Required prerequisite missing: npm"
}
$npmVersion = npm --version
Write-Host "      ✓ npm found (version $npmVersion)" -ForegroundColor Green

Write-Host ""

# Set paths
$scriptDir = $PSScriptRoot
$rootDir = Split-Path -Parent (Split-Path -Parent $scriptDir)
$artifactsDir = Join-Path $rootDir "artifacts"
$portableDir = Join-Path $artifactsDir "portable"
$portableBuildDir = Join-Path $portableDir "build"
$packagingDir = Join-Path $artifactsDir "packaging"

Write-Host "Root Directory:     $rootDir" -ForegroundColor Gray
Write-Host "Artifacts Directory: $artifactsDir" -ForegroundColor Gray
Write-Host ""

try {
    # Create directories
    Write-Host "[1/6] Creating build directories..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Force -Path $portableBuildDir | Out-Null
    New-Item -ItemType Directory -Force -Path $packagingDir | Out-Null
    Write-Host "      ✓ Directories created" -ForegroundColor Green

    # Build core projects
    Write-Host "[2/6] Building .NET projects..." -ForegroundColor Yellow
    $buildOutput = dotnet build "$rootDir\Aura.Core\Aura.Core.csproj" -c $Configuration --nologo 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Aura.Core build failed" }
    
    $buildOutput = dotnet build "$rootDir\Aura.Providers\Aura.Providers.csproj" -c $Configuration --nologo 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Aura.Providers build failed" }
    
    $buildOutput = dotnet build "$rootDir\Aura.Api\Aura.Api.csproj" -c $Configuration --nologo 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Aura.Api build failed" }
    
    Write-Host "      ✓ .NET projects built" -ForegroundColor Green

    # Build Web UI
    Write-Host "[3/6] Building web UI..." -ForegroundColor Yellow
    Push-Location "$rootDir\Aura.Web"
    try {
        if (-not (Test-Path "node_modules")) {
            Write-Host "      Installing npm dependencies..." -ForegroundColor Gray
            
            # Retry npm install up to 3 times for network issues
            $maxRetries = 3
            $retryCount = 0
            $installSuccess = $false
            
            while (-not $installSuccess -and $retryCount -lt $maxRetries) {
                if ($retryCount -gt 0) {
                    Write-Host "      Retry attempt $retryCount of $maxRetries..." -ForegroundColor Gray
                    Start-Sleep -Seconds 2
                }
                
                # Capture output but only show on error
                $npmOutput = npm install --silent 2>&1
                if ($LASTEXITCODE -eq 0) {
                    $installSuccess = $true
                    Write-Host "      ✓ npm dependencies installed" -ForegroundColor Green
                } else {
                    $retryCount++
                    if ($retryCount -ge $maxRetries) {
                        Write-BuildError "npm install failed after $maxRetries attempts"
                        Write-Host "npm output: $npmOutput" -ForegroundColor Red
                        throw "npm install failed after $maxRetries attempts. Error: $npmOutput`n`nPlease check your internet connection and npm configuration."
                    }
                }
            }
        } else {
            Write-Host "      ✓ npm dependencies already installed" -ForegroundColor Green
        }
        
        Write-Host "      Building frontend..." -ForegroundColor Gray
        # Capture output to reduce noise, but show on error
        $buildOutput = npm run build --silent 2>&1
        if ($LASTEXITCODE -ne 0) { 
            Write-BuildError "npm build failed"
            Write-Host "Build output: $buildOutput" -ForegroundColor Red
            throw "npm build failed. Error: $buildOutput`n`nThis may be due to TypeScript compilation errors or other build issues."
        }
        Write-Host "      ✓ Frontend build complete" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
    Write-Host "      ✓ Web UI built" -ForegroundColor Green

    # Publish API as self-contained
    Write-Host "[4/6] Publishing API (self-contained)..." -ForegroundColor Yellow
    dotnet publish "$rootDir\Aura.Api\Aura.Api.csproj" `
        -c $Configuration `
        -r win-$($Platform.ToLower()) `
        --self-contained `
        -o "$portableBuildDir\Api" `
        --nologo -v minimal
    if ($LASTEXITCODE -ne 0) { throw "API publish failed" }
    Write-Host "      ✓ API published" -ForegroundColor Green

    # Copy Web UI to wwwroot folder inside the published API
    Write-Host "[5/6] Copying web UI to wwwroot..." -ForegroundColor Yellow
    
    # Validate dist folder exists
    $distPath = "$rootDir\Aura.Web\dist"
    if (-not (Test-Path $distPath)) {
        Write-BuildError "Web UI dist folder not found at: $distPath"
        Write-BuildError "The frontend build may have failed. Please check the build output above."
        throw "Web UI build validation failed"
    }
    
    # Validate dist folder has required files
    $distIndexHtml = "$distPath\index.html"
    if (-not (Test-Path $distIndexHtml)) {
        Write-BuildError "index.html not found in dist folder"
        Write-BuildError "The frontend build is incomplete. Please check the build output above."
        throw "Web UI build validation failed"
    }
    
    $distAssets = "$distPath\assets"
    if (-not (Test-Path $distAssets)) {
        Write-BuildError "assets folder not found in dist folder"
        Write-BuildError "The frontend build is incomplete. Please check the build output above."
        throw "Web UI build validation failed"
    }
    
    Write-Host "      ✓ Web UI build validated" -ForegroundColor Green
    
    # Copy to wwwroot
    $wwwrootDir = Join-Path "$portableBuildDir\Api" "wwwroot"
    New-Item -ItemType Directory -Force -Path $wwwrootDir | Out-Null
    Copy-Item "$rootDir\Aura.Web\dist\*" -Destination $wwwrootDir -Recurse -Force
    
    # Validate wwwroot has the files
    $wwwrootIndexHtml = Join-Path $wwwrootDir "index.html"
    if (-not (Test-Path $wwwrootIndexHtml)) {
        Write-BuildError "Failed to copy index.html to wwwroot"
        throw "Web UI copy validation failed"
    }
    
    $wwwrootAssets = Join-Path $wwwrootDir "assets"
    if (-not (Test-Path $wwwrootAssets)) {
        Write-BuildError "Failed to copy assets folder to wwwroot"
        throw "Web UI copy validation failed"
    }
    
    Write-Host "      ✓ Web UI copied to wwwroot and validated" -ForegroundColor Green

    # Copy additional files
    Write-Host "[6/6] Copying additional files..." -ForegroundColor Yellow
    
    # Copy FFmpeg (if available)
    $ffmpegDir = Join-Path $portableBuildDir "ffmpeg"
    New-Item -ItemType Directory -Force -Path $ffmpegDir | Out-Null
    if (Test-Path "$rootDir\scripts\ffmpeg\ffmpeg.exe") {
        Copy-Item "$rootDir\scripts\ffmpeg\*.exe" -Destination $ffmpegDir -Force
        Write-Host "      ✓ FFmpeg binaries copied" -ForegroundColor Green
    } else {
    Write-Host "      ⚠ FFmpeg binaries not found (users will need to install separately)" -ForegroundColor Yellow
}

# Copy config and docs
Copy-Item "$rootDir\appsettings.json" -Destination $portableBuildDir -Force
Copy-Item "$rootDir\PORTABLE.md" -Destination "$portableBuildDir\README.md" -Force
if (Test-Path "$rootDir\LICENSE") {
    Copy-Item "$rootDir\LICENSE" -Destination $portableBuildDir -Force
}

# Create launcher script with pre-flight checks and health check polling
$launcherScript = @"
@echo off
echo ========================================
echo  Aura Video Studio - Portable Edition
echo ========================================
echo.
echo Running pre-flight checks...

REM Check if Api folder exists
if not exist "Api\" (
    echo ERROR: Api folder not found!
    echo Please make sure you extracted all files from the ZIP.
    echo.
    pause
    exit /b 1
)

REM Check if Aura.Api.exe exists
if not exist "Api\Aura.Api.exe" (
    echo ERROR: Aura.Api.exe not found!
    echo Please make sure you extracted all files from the ZIP.
    echo.
    pause
    exit /b 1
)

REM Check if wwwroot folder exists
if not exist "Api\wwwroot\" (
    echo ERROR: Web UI files not found at Api\wwwroot\
    echo The application cannot start without the web interface.
    echo Please re-extract the ZIP file or download a new copy.
    echo.
    pause
    exit /b 1
)

REM Check if index.html exists
if not exist "Api\wwwroot\index.html" (
    echo ERROR: index.html not found in Api\wwwroot\
    echo The application cannot start without the web interface.
    echo Please re-extract the ZIP file or download a new copy.
    echo.
    pause
    exit /b 1
)

echo Pre-flight checks passed!
echo.
echo Starting API server...
start "" /D "Api" "Aura.Api.exe"

echo Waiting for server to become ready...
set /a attempts=0
:wait_loop
set /a attempts+=1
if %attempts% gtr ${HealthCheckMaxAttempts} (
    echo.
    echo WARNING: Server did not respond after ${HealthCheckMaxAttempts} attempts.
    echo The server may still be starting. Opening browser anyway...
    goto open_browser
)

REM Try to reach the health check endpoint using PowerShell (more reliable than curl on Windows)
powershell -Command "try { `$response = Invoke-WebRequest -Uri 'http://127.0.0.1:5005/api/healthz' -TimeoutSec 1 -ErrorAction Stop; exit 0 } catch { exit 1 }" >nul 2>&1
if %errorlevel% equ 0 (
    echo Server is ready!
    goto open_browser
)

timeout /t ${HealthCheckIntervalSeconds} /nobreak >nul
goto wait_loop

:open_browser
echo.
echo Opening web browser...
start "" "http://127.0.0.1:5005"
echo.
echo ========================================
echo Application started successfully!
echo ========================================
echo.
echo The application should open in your web browser.
echo If not, manually navigate to: http://127.0.0.1:5005
echo.
echo For diagnostics, visit: http://127.0.0.1:5005/diag
echo.
echo To stop the application, close the API server window.
echo.
"@
Set-Content -Path "$portableBuildDir\Launch.bat" -Value $launcherScript
Write-Host "      ✓ Launch script created with pre-flight checks" -ForegroundColor Green

# Create ZIP
$zipPath = Join-Path $portableDir "AuraVideoStudio_Portable_x64.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}
Compress-Archive -Path "$portableBuildDir\*" -DestinationPath $zipPath -Force

# Generate checksum
$hash = Get-FileHash -Path $zipPath -Algorithm SHA256
$checksumFile = Join-Path $portableDir "checksum.txt"
"$($hash.Hash)  $(Split-Path $zipPath -Leaf)" | Out-File $checksumFile -Encoding utf8

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Portable ZIP:  $zipPath" -ForegroundColor White
Write-Host "Size:          $([math]::Round((Get-Item $zipPath).Length / 1MB, 2)) MB" -ForegroundColor White
Write-Host "SHA-256:       $($hash.Hash)" -ForegroundColor White
Write-Host ""
Write-Host "To test locally:" -ForegroundColor Cyan
Write-Host "  1. Extract the ZIP to a folder" -ForegroundColor White
Write-Host "  2. Run Launch.bat" -ForegroundColor White
Write-Host "  3. Open http://127.0.0.1:5005 in your browser" -ForegroundColor White
Write-Host ""

    # Generate version info file for auto-update
    Write-Host ""
    Write-Host "Generating version information..." -ForegroundColor Yellow
    $versionInfo = @{
        version = "1.0.0"
        buildDate = (Get-Date -Format "o")
        platform = "win-$($Platform.ToLower())"
        configuration = $Configuration
        checksum = $hash.Hash
        downloadUrl = "https://github.com/Coffee285/aura-video-studio/releases/latest/download/AuraVideoStudio_Portable_x64.zip"
    }
    $versionInfo | ConvertTo-Json -Depth 5 | Out-File "$portableBuildDir\version.json" -Encoding utf8
    Write-Host "✓ version.json generated for auto-update" -ForegroundColor Green

    # Generate build report
    $buildEndTime = Get-Date
    $buildDuration = $buildEndTime - $buildStartTime
    $reportPath = Join-Path $packagingDir "build_report.md"
    $reportContent = @"
# Aura Video Studio - Portable Build Report

## Build Summary
- **Status**: ✅ SUCCESS
- **Configuration**: $Configuration
- **Platform**: $Platform  
- **Build Time**: $($buildDuration.TotalSeconds.ToString("F2")) seconds
- **Timestamp**: $buildEndTime

## Artifacts
- **Portable ZIP**: $(Split-Path $zipPath -Leaf)
- **Size**: $([math]::Round((Get-Item $zipPath).Length / 1MB, 2)) MB
- **SHA-256**: $($hash.Hash)
- **Location**: $portableDir

## Build Steps Completed
1. ✅ Created build directories
2. ✅ Built .NET projects (Core, Providers, API)
3. ✅ Built Web UI
4. ✅ Published API (self-contained for Windows $Platform)
5. ✅ Copied Web UI to wwwroot
6. ✅ Copied additional files
7. ✅ Created launcher script
8. ✅ Generated portable ZIP
9. ✅ Generated SHA-256 checksum
10. ✅ Generated version information for auto-update

## Warnings
$( if ($buildWarnings.Count -gt 0) { $buildWarnings -join "`n- " } else { "None" })

## Auto-Update Support
The build includes version.json file with:
- Version: 1.0.0
- Build Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
- Platform: win-$($Platform.ToLower())
- Checksum: $($hash.Hash)

## Dependency Bundling
The portable distribution supports flexible dependency management:
- FFmpeg can be pre-bundled in /ffmpeg folder (if available)
- Dependencies can be downloaded on-demand via Download Center
- Manual attachment of existing installations supported
- All dependencies stored in portable folder structure

---
*Build completed successfully at $buildEndTime*
"@
    Set-Content -Path $reportPath -Value $reportContent -Encoding UTF8
    Write-Host "Build Report:  $reportPath" -ForegroundColor White
    
    exit 0
}
catch {
    $buildSuccess = $false
    $buildErrors += $_.Exception.Message
    
    $buildEndTime = Get-Date
    $buildDuration = $buildEndTime - $buildStartTime
    
    # Generate failure report
    New-Item -ItemType Directory -Force -Path $packagingDir | Out-Null
    $reportPath = Join-Path $packagingDir "build_report.md"
    $reportContent = @"
# Aura Video Studio - Portable Build Report

## Build Summary
- **Status**: ❌ FAILED
- **Configuration**: $Configuration
- **Platform**: $Platform
- **Build Time**: $($buildDuration.TotalSeconds.ToString("F2")) seconds
- **Timestamp**: $buildEndTime

## Errors
- $($buildErrors -join "`n- ")

## Warnings
$( if ($buildWarnings.Count -gt 0) { "- " + ($buildWarnings -join "`n- ") } else { "None" })

---
*Build failed at $buildEndTime*
"@
    Set-Content -Path $reportPath -Value $reportContent -Encoding UTF8
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host " Build Failed!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Build Report: $reportPath" -ForegroundColor White
    Write-Host ""
    
    exit 1
}
