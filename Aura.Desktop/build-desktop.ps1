# PowerShell Build Script for Aura Video Studio Desktop
param(
    [string]$Target = "win",
    [switch]$SkipFrontend,
    [switch]$SkipBackend,
    [switch]$SkipInstaller,
    [switch]$Help
)

# Colors for output
$ErrorColor = "Red"
$SuccessColor = "Green"
$WarningColor = "Yellow"
$InfoColor = "Cyan"

function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor $InfoColor
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor $SuccessColor
}

function Show-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor $WarningColor
}

function Show-ErrorMessage {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor $ErrorColor
}

if ($Help) {
    Write-Output "Aura Video Studio - Desktop Build Script"
    Write-Output ""
    Write-Output "Usage: .\build-desktop.ps1 [OPTIONS]"
    Write-Output ""
    Write-Output "This script performs a CLEAN BUILD by default, removing all build"
    Write-Output "artifacts before building to ensure a fresh build every time."
    Write-Output ""
    Write-Output "Options:"
    Write-Output "  -Target <platform>    Build for specific platform (win only, default: win)"
    Write-Output "  -SkipFrontend         Skip frontend build (and cleaning)"
    Write-Output "  -SkipBackend          Skip backend build (and cleaning)"
    Write-Output "  -SkipInstaller        Skip installer creation (and cleaning)"
    Write-Output "  -Help                 Show this help message"
    exit 0
}

Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host "Aura Video Studio - Desktop Build" -ForegroundColor $InfoColor
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host ""

Write-Info "Build target: $Target"
Write-Host ""

# Check if Node.js is installed
if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Show-ErrorMessage "Node.js is not installed. Please install Node.js 18+ from https://nodejs.org/"
    exit 1
}

# Check if dotnet is installed
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Show-ErrorMessage ".NET 8.0 SDK is not installed. Please install from https://dotnet.microsoft.com/download"
    exit 1
}

$ScriptDir = $PSScriptRoot
$ProjectRoot = Split-Path $ScriptDir -Parent

Set-Location $ScriptDir

# ========================================
# Step 0: Clean Build Artifacts
# ========================================
Write-Info "Cleaning build artifacts for clean build..."
Write-Host ""

# Clean frontend build artifacts
if (-not $SkipFrontend) {
    $FrontendDist = "$ProjectRoot\Aura.Web\dist"
    if (Test-Path $FrontendDist) {
        Write-Info "Cleaning frontend build artifacts..."
        Remove-Item -Path $FrontendDist -Recurse -Force -ErrorAction SilentlyContinue
        Write-Success "  ✓ Frontend dist folder cleaned"
    }
}

# Clean backend build artifacts
if (-not $SkipBackend) {
    Write-Info "Cleaning backend build artifacts..."

    # Clean .NET build outputs (bin/obj folders)
    $ProjectsToClean = @(
        "$ProjectRoot\Aura.Api",
        "$ProjectRoot\Aura.Core",
        "$ProjectRoot\Aura.Providers",
        "$ProjectRoot\Aura.Analyzers"
    )

    foreach ($ProjectPath in $ProjectsToClean) {
        if (Test-Path $ProjectPath) {
            $BinPath = Join-Path $ProjectPath "bin"
            $ObjPath = Join-Path $ProjectPath "obj"

            if (Test-Path $BinPath) {
                Remove-Item -Path $BinPath -Recurse -Force -ErrorAction SilentlyContinue
            }
            if (Test-Path $ObjPath) {
                Remove-Item -Path $ObjPath -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
    }

    # Clean backend output directory
    $BackendOutputDir = "$ScriptDir\resources\backend\win-x64"
    if (Test-Path $BackendOutputDir) {
        Remove-Item -Path $BackendOutputDir -Recurse -Force -ErrorAction SilentlyContinue
        Write-Success "  ✓ Backend output directory cleaned"
    }

    Write-Success "  ✓ Backend build artifacts cleaned"
}

# Clean Electron dist folder (optional, but ensures fresh build)
if (-not $SkipInstaller) {
    $ElectronDist = "$ScriptDir\dist"
    if (Test-Path $ElectronDist) {
        Write-Info "Cleaning Electron dist folder..."
        Remove-Item -Path $ElectronDist -Recurse -Force -ErrorAction SilentlyContinue
        Write-Success "  ✓ Electron dist folder cleaned"
    }
}

Write-Success "Clean build preparation complete"
Write-Host ""

# ========================================
# Step 1: Build Frontend
# ========================================
if (-not $SkipFrontend) {
    Write-Info "Building React frontend..."
    Set-Location "$ProjectRoot\Aura.Web"

    # Always check and install dependencies to ensure they're up to date
    # We check for the vite CLI in node_modules/.bin which npm uses to run commands
    $needsInstall = $false
    if (-not (Test-Path "node_modules")) {
        Write-Info "Installing frontend dependencies (node_modules not found)..."
        $needsInstall = $true
    } elseif (-not (Test-Path "node_modules\.bin\vite.cmd")) {
        Write-Info "Frontend dependencies incomplete (vite CLI not found), reinstalling..."
        $needsInstall = $true
    } else {
        # Verify critical dependencies exist
        $criticalPackages = @("vite", "react", "typescript")
        $missingPackages = @()
        
        foreach ($package in $criticalPackages) {
            if (-not (Test-Path "node_modules\$package")) {
                $missingPackages += $package
            }
        }
        
        if ($missingPackages.Count -gt 0) {
            Write-Info "Critical dependencies missing, reinstalling..."
            Write-Info "Missing: $($missingPackages -join ', ')"
            $needsInstall = $true
        } else {
            Write-Info "Frontend dependencies verified"
        }
    }

    if ($needsInstall) {
        # Check if NODE_ENV=production is set (would skip devDependencies)
        if ($env:NODE_ENV -eq "production") {
            Show-Warning "NODE_ENV is set to 'production'. This can cause devDependencies to be skipped."
            Show-Warning "Using --include=dev to ensure all dependencies are installed."
        }
        
        # Use --include=dev to ensure devDependencies are always installed
        # This is required because vite is a devDependency needed for building
        npm install --include=dev
        if ($LASTEXITCODE -ne 0) {
            Show-ErrorMessage "Frontend npm install failed with exit code $LASTEXITCODE"
            exit 1
        }
        
        # Verify package installation was successful
        # Threshold: 100 is a conservative minimum to catch obviously broken installs
        # (actual count is typically 600+ but threshold is low to avoid false positives)
        $packageCount = (Get-ChildItem "node_modules" -Directory -ErrorAction SilentlyContinue | Measure-Object).Count
        Write-Info "Frontend: Installed $packageCount packages"
        
        if ($packageCount -lt 100) {
            Show-ErrorMessage "Frontend npm install appears incomplete - only $packageCount packages installed (expected 600+)"
            Show-ErrorMessage "This may indicate a network issue or corrupted npm cache."
            Show-ErrorMessage "Try running: npm cache clean --force && npm install --include=dev"
            exit 1
        }
        
        # Verify vite CLI is available (vite is a devDependency required for building)
        if (-not (Test-Path "node_modules\.bin\vite.cmd") -and -not (Test-Path "node_modules\.bin\vite")) {
            Show-Warning "Frontend npm install failed - vite CLI not found in node_modules/.bin"
            Write-Info "Retrying with clean cache..."
            
            # Retry with clean npm cache
            npm cache clean --force
            Remove-Item -Path "node_modules" -Recurse -Force -ErrorAction SilentlyContinue
            npm install --include=dev
            
            if ($LASTEXITCODE -ne 0) {
                Show-ErrorMessage "Frontend npm install failed after retry with exit code $LASTEXITCODE"
                exit 1
            }
            
            # Verify vite CLI again after retry
            if (-not (Test-Path "node_modules\.bin\vite.cmd") -and -not (Test-Path "node_modules\.bin\vite")) {
                Show-ErrorMessage "Frontend npm install failed - vite CLI not found after retry"
                Show-ErrorMessage "This usually means devDependencies were not installed."
                Show-ErrorMessage "If NODE_ENV=production is set in your environment, try:"
                Show-ErrorMessage "  1. Run: set NODE_ENV= (to clear the environment variable)"
                Show-ErrorMessage "  2. Run: npm cache clean --force"
                Show-ErrorMessage "  3. Delete node_modules folder and retry"
                exit 1
            }
        }
        Write-Success "  ✓ Vite CLI verified"
    }

    Write-Info "Running frontend build..."
    npm run build
    if ($LASTEXITCODE -ne 0) {
        Show-ErrorMessage "Frontend build failed with exit code $LASTEXITCODE"
        exit 1
    }

    if (-not (Test-Path "dist\index.html")) {
        Show-ErrorMessage "Frontend build failed - dist\index.html not found"
        exit 1
    }

    Write-Success "Frontend build complete"
    Write-Host ""

    # ========================================
    # Step 1b: Prepare OpenCut (CapCut-style editor) bundle
    # ========================================
    $openCutRootDir = "$ProjectRoot\OpenCut"
    $openCutAppDir = "$openCutRootDir\apps\web"
    $openCutBuildSuccess = $false
    
    if (Test-Path $openCutAppDir) {
        Write-Info "Preparing OpenCut web editor..."

        # ----------------------------------------
        # Step 1b.1: Ensure environment file exists
        # ----------------------------------------
        $envExamplePath = "$openCutAppDir\.env.example"
        $envLocalPath = "$openCutAppDir\.env.local"
        
        if (-not (Test-Path $envLocalPath)) {
            if (Test-Path $envExamplePath) {
                Write-Info "Creating .env.local from .env.example..."
                Copy-Item $envExamplePath $envLocalPath
                Write-Success "  ✓ .env.local created"
            } else {
                Write-Info "Creating minimal .env.local..."
                # Create minimal env file to prevent build errors
                @"
# Minimal environment for standalone build
NODE_ENV=production
NEXT_TELEMETRY_DISABLED=1
"@ | Set-Content $envLocalPath -Encoding UTF8
                Write-Success "  ✓ Minimal .env.local created"
            }
        } else {
            Write-Info "  ✓ .env.local already exists"
        }

        # ----------------------------------------
        # Step 1b.2: Check for package manager (bun preferred, npm fallback)
        # ----------------------------------------
        $bunAvailable = Get-Command bun -ErrorAction SilentlyContinue
        $npmAvailable = Get-Command npm -ErrorAction SilentlyContinue
        $useNpmFallback = $false
        
        if (-not $bunAvailable) {
            Write-Info "Bun is not installed. Attempting to install Bun automatically..."
            
            # Install Bun using the official PowerShell installer from bun.sh
            try {
                $env:BUN_INSTALL = "$env:USERPROFILE\.bun"
                Write-Info "Installing Bun to $env:BUN_INSTALL..."
                
                # Download the official Bun installer from bun.sh
                $installerPath = "$env:TEMP\install-bun.ps1"
                Invoke-RestMethod -Uri "https://bun.sh/install.ps1" -OutFile $installerPath -TimeoutSec 60
                
                # Execute the installer
                & powershell -ExecutionPolicy Bypass -File $installerPath
                
                # Add Bun to PATH for the current session
                $bunPath = "$env:BUN_INSTALL\bin"
                if (Test-Path $bunPath) {
                    $env:PATH = "$bunPath;$env:PATH"
                    
                    # Verify Bun is actually executable
                    $bunAvailable = Get-Command bun -ErrorAction SilentlyContinue
                    if ($bunAvailable) {
                        $bunVersion = & bun --version 2>$null
                        Write-Success "Bun installed successfully (version: $bunVersion)"
                    } else {
                        Show-Warning "Bun directory exists but bun command not found."
                        $bunAvailable = $false
                    }
                } else {
                    Show-Warning "Bun installation directory not found."
                    $bunAvailable = $false
                }
                
                # Clean up temp file
                Remove-Item $installerPath -ErrorAction SilentlyContinue
            }
            catch {
                Show-Warning "Failed to install Bun automatically: $($_.Exception.Message)"
                $bunAvailable = $false
            }
        }
        
        # If bun is still not available, try npm fallback
        if (-not $bunAvailable) {
            if ($npmAvailable) {
                Show-Warning "Bun not available, falling back to npm..."
                $useNpmFallback = $true
            } else {
                Show-Warning "Neither Bun nor npm is available. Skipping OpenCut build."
                Show-Warning "Please install Bun from https://bun.sh/ or Node.js from https://nodejs.org/"
            }
        }
        
        # ----------------------------------------
        # Step 1b.3: Clean existing node_modules if corrupted
        # ----------------------------------------
        if ($bunAvailable -or $useNpmFallback) {
            Set-Location $openCutRootDir
            
            # Check for potentially corrupted node_modules
            $nodeModulesPath = "$openCutRootDir\node_modules"
            $lockFilePath = if ($useNpmFallback) { "$openCutRootDir\package-lock.json" } else { "$openCutRootDir\bun.lockb" }
            
            $shouldCleanInstall = $false
            if (Test-Path $nodeModulesPath) {
                # Check for corruption indicators
                $markerFile = "$nodeModulesPath\.package-lock.json"
                $bunLockExists = Test-Path "$openCutRootDir\bun.lockb"
                
                # If switching between package managers or install was interrupted
                if ($useNpmFallback -and $bunLockExists -and -not (Test-Path "$openCutRootDir\package-lock.json")) {
                    Write-Info "Switching from bun to npm, cleaning node_modules..."
                    $shouldCleanInstall = $true
                }
                
                # Check if node_modules seems incomplete
                $criticalDirs = @("next", "react", "react-dom")
                foreach ($dir in $criticalDirs) {
                    if (-not (Test-Path "$nodeModulesPath\$dir")) {
                        Write-Info "Missing critical dependency: $dir. Will perform clean install..."
                        $shouldCleanInstall = $true
                        break
                    }
                }
            }
            
            if ($shouldCleanInstall) {
                Write-Info "Removing existing node_modules for clean install..."
                Remove-Item -Path $nodeModulesPath -Recurse -Force -ErrorAction SilentlyContinue
                Write-Success "  ✓ node_modules cleaned"
            }

            # ----------------------------------------
            # Step 1b.4: Convert workspace:* deps for npm fallback
            # ----------------------------------------
            if ($useNpmFallback) {
                $convertScript = "$ScriptDir\scripts\convert-workspace-deps.js"
                if (Test-Path $convertScript) {
                    Write-Info "Converting workspace:* dependencies for npm compatibility..."
                    node $convertScript 2>&1 | Out-Host
                    if ($LASTEXITCODE -eq 0) {
                        Write-Success "  ✓ Workspace dependencies converted"
                    } else {
                        Show-Warning "Failed to convert workspace dependencies"
                    }
                } else {
                    Show-Warning "Workspace conversion script not found at $convertScript"
                }
            }
            
            # ----------------------------------------
            # Step 1b.5: Install dependencies with retry logic
            # ----------------------------------------
            $maxRetries = 3
            $retryCount = 0
            $installSuccess = $false
            
            while (-not $installSuccess -and $retryCount -lt $maxRetries) {
                $retryCount++
                Write-Info "Installing OpenCut dependencies (attempt $retryCount of $maxRetries)..."
                
                if ($useNpmFallback) {
                    # npm install with converted workspace:* references
                    npm install --legacy-peer-deps 2>&1 | Out-Host
                } else {
                    # bun install
                    bun install 2>&1 | Out-Host
                }
                
                if ($LASTEXITCODE -eq 0) {
                    $installSuccess = $true
                    Write-Success "  ✓ Dependencies installed successfully"
                } else {
                    if ($retryCount -lt $maxRetries) {
                        Show-Warning "Install attempt $retryCount failed, retrying in 5 seconds..."
                        Start-Sleep -Seconds 5
                        
                        # Clean node_modules before retry
                        if (Test-Path $nodeModulesPath) {
                            Remove-Item -Path $nodeModulesPath -Recurse -Force -ErrorAction SilentlyContinue
                        }
                    } else {
                        Show-Warning "All install attempts failed."
                    }
                }
            }
            
            if (-not $installSuccess) {
                Show-Warning "OpenCut dependency installation failed after $maxRetries attempts."
                Show-Warning "OpenCut may not be available."
            }

            # ----------------------------------------
            # Step 1b.6: Build OpenCut
            # ----------------------------------------
            if ($installSuccess) {
                Write-Info "Running OpenCut production build..."
                
                # Set environment variables for build
                $env:NODE_ENV = "production"
                $env:NEXT_TELEMETRY_DISABLED = "1"
                
                if ($useNpmFallback) {
                    # For npm fallback, build directly from apps/web with proper NODE_PATH
                    # This is needed because npm workspaces handle module resolution differently than bun
                    Set-Location $openCutAppDir
                    
                    # Set NODE_PATH to include both local and root node_modules
                    $env:NODE_PATH = "$openCutAppDir\node_modules;$openCutRootDir\node_modules"
                    
                    Write-Info "Building with NODE_PATH: $env:NODE_PATH"
                    npx next build 2>&1 | Out-Host
                    
                    # Clear NODE_PATH after build
                    $env:NODE_PATH = $null
                    
                    # Return to OpenCut root
                    Set-Location $openCutRootDir
                } else {
                    bun run build 2>&1 | Out-Host
                }
                
                if ($LASTEXITCODE -ne 0) {
                    Show-Warning "OpenCut build failed with exit code $LASTEXITCODE."
                    
                    # Check for common build errors and provide guidance
                    Write-Info "Checking for common issues..."
                    
                    $tsconfigPath = "$openCutAppDir\tsconfig.json"
                    if (-not (Test-Path $tsconfigPath)) {
                        Show-Warning "  ✗ tsconfig.json not found in $openCutAppDir"
                    }
                    
                    $nextConfigPath = "$openCutAppDir\next.config.ts"
                    if (-not (Test-Path $nextConfigPath)) {
                        $nextConfigPath = "$openCutAppDir\next.config.js"
                        if (-not (Test-Path $nextConfigPath)) {
                            Show-Warning "  ✗ next.config.ts/js not found in $openCutAppDir"
                        }
                    }
                } else {
                    # ----------------------------------------
                    # Step 1b.7: Verify build output
                    # ----------------------------------------
                    $openCutNextDir = "$openCutAppDir\.next"
                    $openCutStandaloneDir = "$openCutNextDir\standalone"
                    $openCutStaticDir = "$openCutNextDir\static"
                    
                    if (Test-Path $openCutNextDir) {
                        # In monorepo setup with npm workspaces, server.js is at standalone/apps/web/server.js
                        # In single-package setup with bun, server.js is at standalone/server.js
                        $standaloneServerJsMonorepo = "$openCutStandaloneDir\apps\web\server.js"
                        $standaloneServerJsSingle = "$openCutStandaloneDir\server.js"
                        $standaloneServerJs = if (Test-Path $standaloneServerJsMonorepo) { 
                            $standaloneServerJsMonorepo 
                        } else { 
                            $standaloneServerJsSingle 
                        }
                        $buildManifest = "$openCutNextDir\build-manifest.json"
                        
                        $verificationPassed = $true
                        $verificationMessages = @()
                        
                        if (Test-Path $openCutStandaloneDir) {
                            $verificationMessages += "  ✓ .next/standalone directory exists"
                        } else {
                            $verificationPassed = $false
                            $verificationMessages += "  ✗ .next/standalone directory not found"
                        }
                        
                        if ((Test-Path $standaloneServerJsMonorepo) -or (Test-Path $standaloneServerJsSingle)) {
                            $verificationMessages += "  ✓ standalone/server.js exists"
                        } else {
                            $verificationPassed = $false
                            $verificationMessages += "  ✗ standalone/server.js not found"
                        }
                        
                        if (Test-Path $openCutStaticDir) {
                            $verificationMessages += "  ✓ .next/static directory exists"
                        } else {
                            $verificationPassed = $false
                            $verificationMessages += "  ✗ .next/static directory not found"
                        }
                        
                        if (Test-Path $buildManifest) {
                            $verificationMessages += "  ✓ build-manifest.json exists"
                        } else {
                            $verificationMessages += "  ⚠ build-manifest.json not found (optional)"
                        }
                        
                        # Check standalone has required files
                        if ($verificationPassed) {
                            # In monorepo, node_modules is at standalone/node_modules
                            $standaloneNodeModulesRoot = "$openCutStandaloneDir\node_modules"
                            $standaloneNodeModulesApp = "$openCutStandaloneDir\apps\web\node_modules"
                            if ((Test-Path $standaloneNodeModulesRoot) -or (Test-Path $standaloneNodeModulesApp)) {
                                $verificationMessages += "  ✓ standalone/node_modules exists"
                            } else {
                                $verificationMessages += "  ⚠ standalone/node_modules not found (may be embedded)"
                            }
                        }
                        
                        if ($verificationPassed) {
                            Write-Success "OpenCut build verification passed"
                            $verificationMessages | ForEach-Object { Write-Success $_ }
                            $openCutBuildSuccess = $true
                        } else {
                            Show-Warning "OpenCut build verification failed"
                            $verificationMessages | ForEach-Object { 
                                if ($_ -match "^  ✗") { Show-Warning $_ }
                                else { Write-Success $_ }
                            }
                            Show-Warning "OpenCut integration may not work properly."
                        }
                    } else {
                        Show-Warning "OpenCut build verification failed: .next directory not found"
                        Show-Warning "OpenCut integration may not work properly."
                    }
                }
            }
        }

        # Return to script directory
        Set-Location $ScriptDir
        
        if ($openCutBuildSuccess) {
            Write-Success "OpenCut build complete and verified"
        } else {
            Show-Warning "========================================"
            Show-Warning "OpenCut build FAILED or SKIPPED"
            Show-Warning "The application will build without OpenCut integration."
            Show-Warning "OpenCut editor features will not be available."
            Show-Warning "========================================"
        }
    } else {
        Show-Warning "OpenCut source directory not found at $openCutAppDir. Skipping OpenCut bundle."
        Set-Location $ScriptDir
    }
}
else {
    Show-Warning "Skipping frontend build"
    Write-Host ""
}

# ========================================
# Step 2: Build Backend
# ========================================
if (-not $SkipBackend) {
    Write-Info "Building .NET backend..."
    Set-Location "$ProjectRoot\Aura.Api"

    # Clean .NET build cache before building
    Write-Info "Cleaning .NET build cache..."
    dotnet clean -c Release --nologo | Out-Null
    Write-Success "  ✓ .NET build cache cleaned"

    # Create backend output directory (must match package.json extraResources path)
    $ResourcesDir = "$ScriptDir\resources"
    $BackendDir = "$ResourcesDir\backend"
    if (-not (Test-Path $BackendDir)) {
        New-Item -ItemType Directory -Path $BackendDir -Force | Out-Null
    }

    if ($Target -eq "win") {
        Write-Info "Building backend for Windows (x64)..."
        Write-Info "This may take several minutes..."
        dotnet publish -c Release -r win-x64 --self-contained true `
            -p:PublishSingleFile=false `
            -p:PublishTrimmed=false `
            -p:IncludeNativeLibrariesForSelfExtract=true `
            -p:SkipFrontendBuild=true `
            -o "$BackendDir\win-x64"
        if ($LASTEXITCODE -ne 0) {
            Show-ErrorMessage "Windows backend build failed with exit code $LASTEXITCODE"
            exit 1
        }

        Write-Success "Windows backend build complete"
    }
    else {
        Show-ErrorMessage "Only Windows builds are supported. Target: $Target"
        exit 1
    }

    Write-Success "Backend builds complete"

    Write-Host ""
}
else {
    Show-Warning "Skipping backend build"
    Write-Host ""
}

# ========================================
# Step 2b: Apply Database Migrations
# ========================================
if (-not $SkipBackend) {
    Write-Info "Applying database migrations..."
    Set-Location $ProjectRoot
    
    # Restore local dotnet tools (including dotnet-ef from manifest)
    Write-Info "Restoring local dotnet tools from manifest..."
    $restoreOutput = dotnet tool restore 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "  ✓ Dotnet tools restored successfully"
        $efInstalled = $true
    }
    else {
        Show-Warning "  Could not restore dotnet tools. Database migration check skipped."
        Show-Warning "  Migrations will be applied automatically on first application start."
        Write-Host "  Restore error: $restoreOutput" -ForegroundColor Gray
        $efInstalled = $false
    }
    
    # Only attempt migrations if dotnet-ef is available
    if ($efInstalled) {
        # Navigate to API project for migrations
        Set-Location "$ProjectRoot\Aura.Api"
        
        # Apply migrations (this will create database if missing)
        Write-Info "Checking for pending migrations..."
        try {
            # Use --configuration Release to match the build configuration
            $migrationOutput = dotnet ef database update --configuration Release 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Success "  ✓ Database migrations applied successfully"
                if ($VerbosePreference -eq 'Continue') {
                    Write-Host "  Migration output: $migrationOutput" -ForegroundColor Gray
                }
            } else {
                Show-Warning "  Could not apply migrations during build (will be applied on first app start)"
                Write-Host "  Migration output: $migrationOutput" -ForegroundColor Gray
            }
        } catch {
            Show-Warning "  Could not apply migrations during build (will be applied on first app start)"
            Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Gray
        }
    }
    else {
        Write-Info "Skipping build-time migration check (migrations will run automatically on first app start)"
    }
    
    # Return to script directory
    Set-Location $ScriptDir
    Write-Host ""
}

# ========================================
# Step 2a: Ensure bundled FFmpeg binaries
# ========================================
Write-Info "Ensuring bundled FFmpeg binaries are available..."
& "$ScriptDir\scripts\ensure-ffmpeg.ps1"
if ($LASTEXITCODE -ne 0) {
    Show-ErrorMessage "Failed to prepare bundled FFmpeg binaries"
    exit 1
}
Write-Success "Bundled FFmpeg binaries ready"
Write-Host ""

# ========================================
# Step 3: Install Electron Dependencies
# ========================================
Write-Info "Installing Electron dependencies..."
Set-Location $ScriptDir

$electronNeedsInstall = $false
if (-not (Test-Path "node_modules")) {
    Write-Info "Installing Electron dependencies (node_modules not found)..."
    $electronNeedsInstall = $true
}
else {
    # Verify critical dependencies exist
    $criticalPackages = @("electron", "electron-builder", "electron-store")
    $missingPackages = @()
    
    foreach ($package in $criticalPackages) {
        if (-not (Test-Path "node_modules\$package")) {
            $missingPackages += $package
        }
    }
    
    if ($missingPackages.Count -gt 0) {
        Write-Info "Critical Electron dependencies missing, reinstalling..."
        Write-Info "Missing: $($missingPackages -join ', ')"
        $electronNeedsInstall = $true
    } else {
        Write-Info "Electron dependencies verified"
    }
}

if ($electronNeedsInstall) {
    npm install
    if ($LASTEXITCODE -ne 0) {
        Show-ErrorMessage "npm install failed with exit code $LASTEXITCODE"
        exit 1
    }
    
    # Verify package installation was successful
    # Threshold: 50 is a conservative minimum to catch obviously broken installs
    # (actual count is typically 300+ but threshold is low to avoid false positives)
    $packageCount = (Get-ChildItem "node_modules" -Directory -ErrorAction SilentlyContinue | Measure-Object).Count
    Write-Info "Electron: Installed $packageCount packages"
    
    if ($packageCount -lt 50) {
        Show-ErrorMessage "Electron npm install appears incomplete - only $packageCount packages installed (expected 300+)"
        Show-ErrorMessage "This may indicate a network issue or corrupted npm cache."
        Show-ErrorMessage "Try running: npm cache clean --force && npm install"
        exit 1
    }
    
    # Verify critical CLIs are available
    $electronBuilderCmd = "node_modules\.bin\electron-builder.cmd"
    if (-not (Test-Path $electronBuilderCmd) -and -not (Test-Path "node_modules\.bin\electron-builder")) {
        Show-ErrorMessage "Electron npm install failed - electron-builder CLI not found in node_modules/.bin"
        Show-ErrorMessage "This may indicate a corrupted installation."
        exit 1
    }
    Write-Success "  ✓ Electron-builder CLI verified"
}

Write-Success "Electron dependencies ready"
Write-Host ""

# ========================================
# Step 4: Validate Resources
# ========================================
Write-Info "Validating required resources..."

$RequiredPaths = @(
    @{ Path = "$ProjectRoot\Aura.Web\dist\index.html"; Name = "Frontend build" },
    @{ Path = "$ScriptDir\resources\backend"; Name = "Backend binaries" },
    @{ Path = "$ScriptDir\resources\backend\win-x64\Aura.Api.exe"; Name = "Backend executable (Aura.Api.exe)" },
    @{ Path = "$ScriptDir\resources\ffmpeg\win-x64\bin\ffmpeg.exe"; Name = "Bundled FFmpeg" }
)

$ValidationFailed = $false
foreach ($item in $RequiredPaths) {
    if (-not (Test-Path $item.Path)) {
        Show-ErrorMessage "$($item.Name) not found at: $($item.Path)"
        $ValidationFailed = $true
    }
    else {
        Write-Success "  ✓ $($item.Name) found"
    }
}

if ($ValidationFailed) {
    Show-ErrorMessage "Resource validation failed. Cannot build installer."
    Write-Info "Please ensure all build steps complete successfully."
    exit 1
}

Write-Success "All required resources validated"
Write-Host ""

# ========================================
# Step 5: Build Electron Installers
# ========================================
if (-not $SkipInstaller) {
    Write-Info "Building Electron installers..."

    if ($Target -eq "win") {
        Write-Info "Building Windows installer..."
        npm run build:win
        if ($LASTEXITCODE -ne 0) {
            Show-ErrorMessage "Windows installer build failed with exit code $LASTEXITCODE"
            exit 1
        }
    }
    else {
        Show-ErrorMessage "Only Windows builds are supported. Target: $Target"
        exit 1
    }

    Write-Success "Installer build complete"
}
else {
    Show-Warning "Skipping installer creation (building directory only)"
    npm run build:dir
}

Write-Host ""
Write-Success "========================================"
Write-Success "Build Complete!"
Write-Success "========================================"
Write-Host ""
Write-Info "Output directory: $ScriptDir\dist"
Write-Host ""

# List generated files
if (Test-Path "$ScriptDir\dist") {
    Write-Info "Generated files:"
    Get-ChildItem "$ScriptDir\dist" | ForEach-Object {
        $size = if ($_.PSIsContainer) { "DIR" } else { "{0:N2} MB" -f ($_.Length / 1MB) }
        Write-Host "  $($_.Name) ($size)"
    }
    Write-Host ""
}

Write-Info "To run the app in development mode:"
Write-Host "  cd Aura.Desktop"
Write-Host "  npm start"
Write-Host ""

Write-Success "All done! 🎉"
