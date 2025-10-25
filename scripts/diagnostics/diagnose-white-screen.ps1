<#
.SYNOPSIS
    Emergency White Screen Diagnostic Script for Aura Video Studio

.DESCRIPTION
    Comprehensive diagnostic script to identify and fix white screen issues
    when accessing http://127.0.0.1:5005 after building the portable distribution.

.PARAMETER Fix
    Automatically attempt to fix detected issues

.PARAMETER Verbose
    Show detailed diagnostic output

.EXAMPLE
    .\diagnose-white-screen.ps1
    .\diagnose-white-screen.ps1 -Fix
    .\diagnose-white-screen.ps1 -Verbose -Fix
#>

param(
    [switch]$Fix,
    [switch]$Verbose
)

$ErrorActionPreference = "Continue"
$Script:IssuesFound = @()
$Script:FixesApplied = @()

function Write-DiagHeader {
    param([string]$Message)
    Write-Host ""
    Write-Host "=" * 80 -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor Cyan
    Write-Host "=" * 80 -ForegroundColor Cyan
}

function Write-DiagInfo {
    param([string]$Message)
    Write-Host "ℹ $Message" -ForegroundColor Blue
}

function Write-DiagSuccess {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-DiagWarning {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor Yellow
    $Script:IssuesFound += $Message
}

function Write-DiagError {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
    $Script:IssuesFound += $Message
}

function Write-DiagFix {
    param([string]$Message)
    Write-Host "🔧 $Message" -ForegroundColor Magenta
    $Script:FixesApplied += $Message
}

# Get script directory and navigate to root
$rootDir = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $rootDir

Write-DiagHeader "AURA VIDEO STUDIO - WHITE SCREEN DIAGNOSTIC"
Write-Host "Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host "Root Directory: $rootDir" -ForegroundColor Gray
Write-Host ""

# ============================================================================
# SECTION 1: Environment Check
# ============================================================================
Write-DiagHeader "SECTION 1: ENVIRONMENT CHECK"

# Check Node.js
if (Get-Command node -ErrorAction SilentlyContinue) {
    $nodeVersion = node --version
    Write-DiagSuccess "Node.js: $nodeVersion"
    
    if ($nodeVersion -match "v(\d+)\.") {
        $majorVersion = [int]$Matches[1]
        if ($majorVersion -lt 18) {
            Write-DiagWarning "Node.js version is $nodeVersion, but >=18 is recommended"
        }
    }
} else {
    Write-DiagError "Node.js not found in PATH"
}

# Check npm
if (Get-Command npm -ErrorAction SilentlyContinue) {
    $npmVersion = npm --version
    Write-DiagSuccess "npm: v$npmVersion"
} else {
    Write-DiagError "npm not found in PATH"
}

# Check .NET
if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    $dotnetVersion = dotnet --version
    Write-DiagSuccess ".NET SDK: $dotnetVersion"
} else {
    Write-DiagError ".NET SDK not found in PATH"
}

# ============================================================================
# SECTION 2: Source Files Check
# ============================================================================
Write-DiagHeader "SECTION 2: SOURCE FILES CHECK"

# Check Aura.Web directory
if (Test-Path "Aura.Web") {
    Write-DiagSuccess "Aura.Web directory exists"
    
    # Check package.json
    if (Test-Path "Aura.Web\package.json") {
        Write-DiagSuccess "package.json exists"
    } else {
        Write-DiagError "package.json not found in Aura.Web"
    }
    
    # Check vite.config.ts
    if (Test-Path "Aura.Web\vite.config.ts") {
        Write-DiagSuccess "vite.config.ts exists"
        
        if ($Verbose) {
            $viteConfig = Get-Content "Aura.Web\vite.config.ts" -Raw
            if ($viteConfig -match "base:\s*['\"]([^'\"]+)['\"]") {
                Write-DiagInfo "Vite base path: '$($Matches[1])'"
            }
        }
    } else {
        Write-DiagError "vite.config.ts not found in Aura.Web"
    }
    
    # Check if dist exists (previous build)
    if (Test-Path "Aura.Web\dist") {
        $distFiles = @(Get-ChildItem "Aura.Web\dist" -Recurse -File)
        Write-DiagInfo "Previous dist folder exists ($($distFiles.Count) files)"
    } else {
        Write-DiagInfo "No previous dist folder found"
    }
} else {
    Write-DiagError "Aura.Web directory not found"
}

# ============================================================================
# SECTION 3: Build Output Check
# ============================================================================
Write-DiagHeader "SECTION 3: BUILD OUTPUT CHECK"

# Check artifacts directory
if (Test-Path "artifacts\portable\build\Api") {
    Write-DiagSuccess "Portable build directory exists"
    
    # Check wwwroot
    $wwwroot = "artifacts\portable\build\Api\wwwroot"
    if (Test-Path $wwwroot) {
        Write-DiagSuccess "wwwroot directory exists"
        
        # Check index.html
        $indexPath = Join-Path $wwwroot "index.html"
        if (Test-Path $indexPath) {
            Write-DiagSuccess "index.html exists"
            
            $indexContent = Get-Content $indexPath -Raw
            
            # Check for DOCTYPE
            if ($indexContent -match "<!doctype html>") {
                Write-DiagSuccess "index.html has DOCTYPE"
            } else {
                Write-DiagError "index.html missing DOCTYPE"
            }
            
            # Check for root div
            if ($indexContent -match '<div id="root"></div>') {
                Write-DiagSuccess "index.html has root div"
            } else {
                Write-DiagError "index.html missing root div"
            }
            
            # Check for script tags
            if ($indexContent -match '<script[^>]*type="module"[^>]*src="(/assets/[^"]+\.js)"') {
                $scriptSrc = $Matches[1]
                Write-DiagSuccess "index.html has module script: $scriptSrc"
                
                # Check if the script file exists
                $scriptPath = Join-Path $wwwroot $scriptSrc.TrimStart('/')
                if (Test-Path $scriptPath) {
                    Write-DiagSuccess "Main script file exists: $scriptSrc"
                    
                    # Check file size
                    $scriptSize = (Get-Item $scriptPath).Length
                    if ($scriptSize -lt 1000) {
                        Write-DiagWarning "Main script file is suspiciously small: $scriptSize bytes"
                    } else {
                        Write-DiagSuccess "Main script file size: $([math]::Round($scriptSize/1KB, 2)) KB"
                    }
                    
                    # Check if it's actual JavaScript
                    $firstLine = Get-Content $scriptPath -First 1
                    if ($firstLine -match "^<!DOCTYPE" -or $firstLine -match "^<html") {
                        Write-DiagError "Main script file contains HTML instead of JavaScript!"
                    } else {
                        Write-DiagSuccess "Main script file appears to be valid JavaScript"
                    }
                } else {
                    Write-DiagError "Main script file not found: $scriptPath"
                }
            } else {
                Write-DiagError "index.html missing module script tag"
            }
            
            # Check for CSS
            if ($indexContent -match '<link[^>]*rel="stylesheet"[^>]*href="(/assets/[^"]+\.css)"') {
                $cssHref = $Matches[1]
                Write-DiagSuccess "index.html has CSS link: $cssHref"
                
                $cssPath = Join-Path $wwwroot $cssHref.TrimStart('/')
                if (Test-Path $cssPath) {
                    Write-DiagSuccess "CSS file exists: $cssHref"
                } else {
                    Write-DiagWarning "CSS file not found: $cssPath"
                }
            } else {
                Write-DiagWarning "index.html missing CSS link"
            }
            
        } else {
            Write-DiagError "index.html not found in wwwroot"
        }
        
        # Check assets directory
        $assetsPath = Join-Path $wwwroot "assets"
        if (Test-Path $assetsPath) {
            $jsFiles = @(Get-ChildItem $assetsPath -Filter "*.js")
            $cssFiles = @(Get-ChildItem $assetsPath -Filter "*.css")
            
            Write-DiagSuccess "assets directory exists"
            Write-DiagInfo "JavaScript files: $($jsFiles.Count)"
            Write-DiagInfo "CSS files: $($cssFiles.Count)"
            
            if ($jsFiles.Count -eq 0) {
                Write-DiagError "No JavaScript files found in assets"
            }
            
            # Check first JS file to ensure it's not HTML
            if ($jsFiles.Count -gt 0) {
                $firstJs = $jsFiles[0]
                $firstJsContent = Get-Content $firstJs.FullName -First 3 -Raw
                
                if ($firstJsContent -match "<!DOCTYPE" -or $firstJsContent -match "<html") {
                    Write-DiagError "JavaScript file '$($firstJs.Name)' contains HTML!"
                } else {
                    Write-DiagSuccess "JavaScript files appear to be valid"
                }
            }
        } else {
            Write-DiagError "assets directory not found in wwwroot"
        }
        
        # Get total file count
        $allFiles = @(Get-ChildItem $wwwroot -Recurse -File)
        Write-DiagInfo "Total files in wwwroot: $($allFiles.Count)"
        
    } else {
        Write-DiagError "wwwroot directory not found"
    }
    
    # Check if API executable exists
    if (Test-Path "artifacts\portable\build\Api\Aura.Api.exe") {
        Write-DiagSuccess "API executable exists"
    } elseif (Test-Path "artifacts\portable\build\Api\Aura.Api") {
        Write-DiagSuccess "API executable exists (Linux)"
    } else {
        Write-DiagWarning "API executable not found"
    }
    
} else {
    Write-DiagWarning "Portable build directory not found - has the project been built?"
}

# ============================================================================
# SECTION 4: Common Issues Check
# ============================================================================
Write-DiagHeader "SECTION 4: COMMON ISSUES CHECK"

# Check if node_modules exists
if (Test-Path "Aura.Web\node_modules") {
    Write-DiagSuccess "node_modules directory exists"
    
    # Check for lock file
    if (Test-Path "Aura.Web\package-lock.json") {
        Write-DiagSuccess "package-lock.json exists"
    } else {
        Write-DiagWarning "package-lock.json not found"
    }
} else {
    Write-DiagWarning "node_modules directory not found - dependencies not installed"
}

# Check for .vite cache
if (Test-Path "Aura.Web\.vite") {
    Write-DiagInfo ".vite cache directory exists"
}

# Check if dist is stale
if ((Test-Path "Aura.Web\dist") -and (Test-Path "artifacts\portable\build\Api\wwwroot")) {
    $distTime = (Get-Item "Aura.Web\dist").LastWriteTime
    $wwwrootTime = (Get-Item "artifacts\portable\build\Api\wwwroot").LastWriteTime
    
    if ($wwwrootTime -lt $distTime) {
        Write-DiagWarning "wwwroot is older than dist - may be stale"
    }
}

# ============================================================================
# SECTION 5: Browser/Server Issues
# ============================================================================
Write-DiagHeader "SECTION 5: BROWSER/SERVER ISSUES TO CHECK MANUALLY"

Write-DiagInfo "After starting the API, check these in the browser:"
Write-Host ""
Write-Host "  1. Open http://127.0.0.1:5005 in your browser" -ForegroundColor White
Write-Host "  2. Press F12 to open DevTools" -ForegroundColor White
Write-Host "  3. Check Console tab for JavaScript errors (red text)" -ForegroundColor White
Write-Host "  4. Check Network tab:" -ForegroundColor White
Write-Host "     - Refresh page (F5)" -ForegroundColor White
Write-Host "     - Look for .js files with status other than 200" -ForegroundColor White
Write-Host "     - Click on a .js file and check if Response shows JavaScript or HTML" -ForegroundColor White
Write-Host "  5. Check Elements tab:" -ForegroundColor White
Write-Host "     - Find <div id=""root""></div>" -ForegroundColor White
Write-Host "     - Is it empty or does it have content?" -ForegroundColor White
Write-Host ""

# ============================================================================
# SECTION 6: Nuclear Fix Options
# ============================================================================
if ($Fix) {
    Write-DiagHeader "SECTION 6: APPLYING NUCLEAR FIX"
    
    Write-DiagInfo "This will clean and rebuild everything from scratch..."
    Write-Host ""
    
    $confirm = Read-Host "Are you sure you want to proceed? (yes/no)"
    if ($confirm -eq "yes") {
        
        # Step 1: Clean artifacts
        if (Test-Path "artifacts") {
            Write-DiagFix "Removing artifacts directory..."
            Remove-Item -Recurse -Force "artifacts" -ErrorAction SilentlyContinue
        }
        
        # Step 2: Clean Aura.Web build artifacts
        Write-DiagFix "Cleaning Aura.Web build artifacts..."
        if (Test-Path "Aura.Web\dist") {
            Remove-Item -Recurse -Force "Aura.Web\dist" -ErrorAction SilentlyContinue
        }
        if (Test-Path "Aura.Web\.vite") {
            Remove-Item -Recurse -Force "Aura.Web\.vite" -ErrorAction SilentlyContinue
        }
        
        # Step 3: Reinstall npm dependencies (optional - commented out by default)
        # Write-DiagFix "Reinstalling npm dependencies..."
        # if (Test-Path "Aura.Web\node_modules") {
        #     Remove-Item -Recurse -Force "Aura.Web\node_modules" -ErrorAction SilentlyContinue
        # }
        # if (Test-Path "Aura.Web\package-lock.json") {
        #     Remove-Item -Force "Aura.Web\package-lock.json" -ErrorAction SilentlyContinue
        # }
        # Set-Location "Aura.Web"
        # npm install
        # Set-Location $rootDir
        
        # Step 4: Build frontend
        Write-DiagFix "Building frontend..."
        Set-Location "Aura.Web"
        npm run build
        Set-Location $rootDir
        
        # Step 5: Verify dist folder
        if (Test-Path "Aura.Web\dist\index.html") {
            Write-DiagSuccess "Frontend build successful"
            
            $indexContent = Get-Content "Aura.Web\dist\index.html" -Raw
            if ($indexContent -match '<script[^>]*src="(/assets/[^"]+\.js)"') {
                Write-DiagSuccess "index.html contains script tag"
            } else {
                Write-DiagError "index.html missing script tag after build!"
            }
        } else {
            Write-DiagError "Frontend build failed - index.html not found"
            exit 1
        }
        
        # Step 6: Build API with frontend
        Write-DiagFix "Building API with integrated frontend..."
        dotnet publish Aura.Api\Aura.Api.csproj -c Release -r win-x64 --self-contained -o artifacts\portable\build\Api
        
        # Step 7: Verify wwwroot
        if (Test-Path "artifacts\portable\build\Api\wwwroot\index.html") {
            Write-DiagSuccess "wwwroot contains index.html"
            
            $wwwrootIndex = Get-Content "artifacts\portable\build\Api\wwwroot\index.html" -Raw
            if ($wwwrootIndex -match '<script[^>]*src="(/assets/[^"]+\.js)"') {
                $scriptSrc = $Matches[1]
                Write-DiagSuccess "wwwroot index.html contains script tag"
                
                $scriptPath = "artifacts\portable\build\Api\wwwroot" + $scriptSrc.Replace('/', '\')
                if (Test-Path $scriptPath) {
                    Write-DiagSuccess "Script file exists in wwwroot"
                } else {
                    Write-DiagError "Script file not found in wwwroot: $scriptPath"
                }
            } else {
                Write-DiagError "wwwroot index.html missing script tag!"
            }
        } else {
            Write-DiagError "wwwroot setup failed - index.html not found"
            exit 1
        }
        
        Write-Host ""
        Write-DiagSuccess "Nuclear fix complete!"
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Cyan
        Write-Host "  1. Navigate to: artifacts\portable\build" -ForegroundColor White
        Write-Host "  2. Run: .\Api\Aura.Api.exe" -ForegroundColor White
        Write-Host "  3. Open browser: http://127.0.0.1:5005" -ForegroundColor White
        Write-Host ""
        
    } else {
        Write-DiagInfo "Fix cancelled by user"
    }
} else {
    Write-DiagHeader "SECTION 6: FIX OPTIONS"
    Write-Host ""
    Write-Host "To apply automatic fixes, run:" -ForegroundColor Cyan
    Write-Host "  .\scripts\diagnostics\diagnose-white-screen.ps1 -Fix" -ForegroundColor White
    Write-Host ""
}

# ============================================================================
# SUMMARY
# ============================================================================
Write-DiagHeader "DIAGNOSTIC SUMMARY"

if ($Script:IssuesFound.Count -eq 0) {
    Write-DiagSuccess "No issues detected!"
    Write-Host ""
    Write-Host "If you're still seeing a white screen:" -ForegroundColor Yellow
    Write-Host "  1. Clear browser cache (Ctrl+Shift+Delete)" -ForegroundColor White
    Write-Host "  2. Try incognito mode" -ForegroundColor White
    Write-Host "  3. Check browser DevTools console for errors" -ForegroundColor White
    Write-Host "  4. Verify API is running and accessible" -ForegroundColor White
} else {
    Write-Host ""
    Write-Host "Issues Found: $($Script:IssuesFound.Count)" -ForegroundColor Yellow
    foreach ($issue in $Script:IssuesFound) {
        Write-Host "  - $issue" -ForegroundColor Yellow
    }
}

if ($Script:FixesApplied.Count -gt 0) {
    Write-Host ""
    Write-Host "Fixes Applied: $($Script:FixesApplied.Count)" -ForegroundColor Green
    foreach ($fix in $Script:FixesApplied) {
        Write-Host "  - $fix" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "For more help, see:" -ForegroundColor Cyan
Write-Host "  - PORTABLE.md (troubleshooting section)" -ForegroundColor White
Write-Host "  - /diag endpoint (http://127.0.0.1:5005/diag)" -ForegroundColor White
Write-Host ""
Write-DiagHeader "DIAGNOSTIC COMPLETE"
