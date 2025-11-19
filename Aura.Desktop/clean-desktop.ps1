# PowerShell Cleanup Script for Aura Video Studio Desktop
param(
    [switch]$IncludeUserContent,
    [switch]$DryRun,
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

function Remove-PathSafely {
    param(
        [string]$Path,
        [string]$Description
    )
    
    if (Test-Path $Path) {
        if ($DryRun) {
            Write-Info "[DRY RUN] Would remove: $Description"
            Write-Info "  Path: $Path"
        } else {
            try {
                Remove-Item -Path $Path -Recurse -Force -ErrorAction Stop
                Write-Success "Removed: $Description"
            } catch {
                Show-Warning "Could not remove: $Description"
                Show-Warning "  Error: $($_.Exception.Message)"
            }
        }
        return $true
    } else {
        Write-Info "Not found (already clean): $Description"
        return $false
    }
}

function Stop-AuraProcesses {
    Write-Info "Checking for running Aura Video Studio processes..."
    
    $processNames = @(
        "Aura Video Studio",
        "aura-video-studio",
        "Aura.Api"
    )
    
    $foundProcesses = $false
    foreach ($processName in $processNames) {
        $processes = Get-Process -Name $processName -ErrorAction SilentlyContinue
        if ($processes) {
            $foundProcesses = $true
            if ($DryRun) {
                Write-Info "[DRY RUN] Would stop $($processes.Count) instance(s) of: $processName"
            } else {
                Write-Warning "Stopping $($processes.Count) instance(s) of: $processName"
                try {
                    $processes | Stop-Process -Force -ErrorAction Stop
                    Start-Sleep -Seconds 2
                    Write-Success "Stopped: $processName"
                } catch {
                    Show-Warning "Could not stop process: $processName"
                    Show-Warning "  Error: $($_.Exception.Message)"
                }
            }
        }
    }
    
    if (-not $foundProcesses) {
        Write-Info "No running processes found"
    }
}

if ($Help) {
    Write-Output "Aura Video Studio - Desktop Cleanup Script"
    Write-Output ""
    Write-Output "Usage: .\clean-desktop.ps1 [OPTIONS]"
    Write-Output ""
    Write-Output "This script completely removes all files created by Aura Video Studio"
    Write-Output "to provide a clean environment for testing. Use this between builds to"
    Write-Output "ensure you're testing with a fresh, first-run state."
    Write-Output ""
    Write-Output "IMPORTANT: This script resets the first-run wizard state by:"
    Write-Output "  • Deleting the SQLite database (%LOCALAPPDATA%\Aura\aura.db)"
    Write-Output "  • Calling the backend reset API (if the server is running)"
    Write-Output "  • Note: localStorage will be cleared when the app restarts"
    Write-Output ""
    Write-Output "Options:"
    Write-Output "  -IncludeUserContent   Also remove user documents and videos (NOT recommended)"
    Write-Output "  -DryRun              Show what would be removed without actually removing"
    Write-Output "  -Help                Show this help message"
    Write-Output ""
    Write-Output "What gets cleaned:"
    Write-Output "  • AppData configuration and cache:"
    Write-Output "    - %LOCALAPPDATA%\aura-video-studio"
    Write-Output "    - %APPDATA%\aura-video-studio (Roaming)"
    Write-Output "  • First-run wizard state (database and localStorage)"
    Write-Output "  • SQLite database (%LOCALAPPDATA%\Aura\aura.db)"
    Write-Output "  • Logs and diagnostics"
    Write-Output "  • Downloaded tools (FFmpeg, TTS engines, etc.)"
    Write-Output "  • Temporary processing files"
    Write-Output "  • Build artifacts (if in dev environment)"
    Write-Output ""
    Write-Output "What is preserved by default:"
    Write-Output "  • User documents (%USERPROFILE%\Documents\Aura Video Studio)"
    Write-Output "  • User videos (%USERPROFILE%\Videos\Aura Studio)"
    Write-Output ""
    Write-Output "Examples:"
    Write-Output "  .\clean-desktop.ps1                  # Clean everything except user content"
    Write-Output "  .\clean-desktop.ps1 -DryRun          # Preview what would be removed"
    Write-Output "  .\clean-desktop.ps1 -IncludeUserContent  # Remove EVERYTHING (use with caution)"
    exit 0
}

Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host "Aura Video Studio - Desktop Cleanup" -ForegroundColor $InfoColor
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host ""

if ($DryRun) {
    Show-Warning "DRY RUN MODE - No files will be actually removed"
    Write-Host ""
}

if ($IncludeUserContent) {
    Show-Warning "IncludeUserContent flag is set - User documents and videos WILL BE REMOVED!"
    Write-Host ""
    
    if (-not $DryRun) {
        $confirmation = Read-Host "Are you sure you want to delete user content? Type 'YES' to confirm"
        if ($confirmation -ne "YES") {
            Write-Info "User content deletion cancelled. Continuing with other cleanup..."
            $IncludeUserContent = $false
        }
    }
}

$cleanupStats = @{
    Removed = 0
    NotFound = 0
    Errors = 0
}

# ========================================
# Step 1: Stop Running Processes
# ========================================
Write-Info "Step 1: Stopping any running instances..."
Write-Host ""
Stop-AuraProcesses
Write-Host ""

# ========================================
# Step 2: Clean AppData Directories
# ========================================
Write-Info "Step 2: Cleaning AppData directories..."
Write-Host ""

# Clean LocalAppData directories
$appDataPath = "$env:LOCALAPPDATA\aura-video-studio"
if (Remove-PathSafely $appDataPath "Main application data (LocalAppData)") {
    $cleanupStats.Removed++
} else {
    $cleanupStats.NotFound++
}

# Also check for any case variations in LocalAppData
$appDataPathAlt = "$env:LOCALAPPDATA\Aura Video Studio"
if (Remove-PathSafely $appDataPathAlt "Application data (LocalAppData alternate)") {
    $cleanupStats.Removed++
}

# Clean Roaming AppData directories (Electron may store config here)
$roamingAppDataPath = "$env:APPDATA\aura-video-studio"
if (Remove-PathSafely $roamingAppDataPath "Application data (Roaming)") {
    $cleanupStats.Removed++
}

# Also check for case variations in Roaming
$roamingAppDataPathAlt = "$env:APPDATA\Aura Video Studio"
if (Remove-PathSafely $roamingAppDataPathAlt "Application data (Roaming alternate)") {
    $cleanupStats.Removed++
}

Write-Host ""

# ========================================
# Step 3: Clean Downloaded Tools
# ========================================
Write-Info "Step 3: Cleaning downloaded tools and engines..."
Write-Host ""

$toolsPath = "$env:LOCALAPPDATA\Aura\Tools"
if (Remove-PathSafely $toolsPath "Downloaded tools (FFmpeg, TTS, etc.)") {
    $cleanupStats.Removed++
} else {
    $cleanupStats.NotFound++
}

# Check for FFmpeg in alternative location
$ffmpegPath = "$env:LOCALAPPDATA\Aura\ffmpeg"
if (Remove-PathSafely $ffmpegPath "Downloaded FFmpeg") {
    $cleanupStats.Removed++
}

Write-Host ""

# ========================================
# Step 4: Clean Temporary Files
# ========================================
Write-Info "Step 4: Cleaning temporary files..."
Write-Host ""

$tempPath = "$env:TEMP\aura-video-studio"
if (Remove-PathSafely $tempPath "Temporary processing files") {
    $cleanupStats.Removed++
} else {
    $cleanupStats.NotFound++
}

Write-Host ""

# ========================================
# Step 5: Clean Build Artifacts (Dev Environment)
# ========================================
Write-Info "Step 5: Cleaning build artifacts (development)..."
Write-Host ""

$ScriptDir = $PSScriptRoot
$ProjectRoot = Split-Path $ScriptDir -Parent

# Clean Electron dist
$electronDist = "$ScriptDir\dist"
if (Remove-PathSafely $electronDist "Electron distribution") {
    $cleanupStats.Removed++
} else {
    $cleanupStats.NotFound++
}

# Clean backend resources
$backendResources = "$ScriptDir\resources\backend"
if (Remove-PathSafely $backendResources "Backend build output") {
    $cleanupStats.Removed++
} else {
    $cleanupStats.NotFound++
}

# Clean frontend dist
$frontendDist = "$ProjectRoot\Aura.Web\dist"
if (Remove-PathSafely $frontendDist "Frontend build output") {
    $cleanupStats.Removed++
} else {
    $cleanupStats.NotFound++
}

# Clean .NET build artifacts
$projectsToClean = @(
    @{ Path = "$ProjectRoot\Aura.Api"; Name = "Aura.Api" },
    @{ Path = "$ProjectRoot\Aura.Core"; Name = "Aura.Core" },
    @{ Path = "$ProjectRoot\Aura.Providers"; Name = "Aura.Providers" },
    @{ Path = "$ProjectRoot\Aura.Analyzers"; Name = "Aura.Analyzers" }
)

foreach ($project in $projectsToClean) {
    if (Test-Path $project.Path) {
        $binPath = Join-Path $project.Path "bin"
        $objPath = Join-Path $project.Path "obj"
        
        if (Remove-PathSafely $binPath "$($project.Name) bin folder") {
            $cleanupStats.Removed++
        }
        if (Remove-PathSafely $objPath "$($project.Name) obj folder") {
            $cleanupStats.Removed++
        }
    }
}

Write-Host ""

# ========================================
# Step 6: Reset First-Run Wizard State
# ========================================
Write-Info "Step 6: Resetting first-run wizard state..."
Write-Host ""

function Reset-WizardState {
    # Reset database
    $databasePath = "$env:LOCALAPPDATA\Aura\aura.db"
    if (Remove-PathSafely $databasePath "SQLite database (wizard state)") {
        $cleanupStats.Removed++
    } else {
        $cleanupStats.NotFound++
    }
    
    # Also check for alternative database location
    $altDatabasePath = "$env:LOCALAPPDATA\Aura\data\aura.db"
    if (Remove-PathSafely $altDatabasePath "SQLite database (alternate path)") {
        $cleanupStats.Removed++
    }
    
    # Clean database journal and WAL files
    $dbJournalFiles = @(
        "$env:LOCALAPPDATA\Aura\aura.db-shm",
        "$env:LOCALAPPDATA\Aura\aura.db-wal",
        "$env:LOCALAPPDATA\Aura\aura.db-journal",
        "$env:LOCALAPPDATA\Aura\data\aura.db-shm",
        "$env:LOCALAPPDATA\Aura\data\aura.db-wal",
        "$env:LOCALAPPDATA\Aura\data\aura.db-journal"
    )
    
    foreach ($journalFile in $dbJournalFiles) {
        if (Test-Path $journalFile) {
            if (Remove-PathSafely $journalFile "Database journal/WAL file") {
                $cleanupStats.Removed++
            }
        }
    }
    
    Write-Info "Database state reset complete"
}

function Reset-WizardBackendState {
    param(
        [int]$ApiPort = 5005
    )
    
    Write-Info "Attempting to reset wizard state via backend API..."
    
    try {
        $apiUrl = "http://localhost:$ApiPort/api/setup/wizard/reset"
        
        # Check if backend is running
        $healthUrl = "http://localhost:$ApiPort/health/live"
        $healthCheck = $null
        try {
            $healthCheck = Invoke-WebRequest -Uri $healthUrl -Method GET -TimeoutSec 2 -ErrorAction SilentlyContinue
        } catch {
            # Backend not running, skip
            Write-Info "Backend API not running (expected if not started yet)"
            return
        }
        
        if ($healthCheck -and $healthCheck.StatusCode -eq 200) {
            Write-Info "Backend is running, calling reset endpoint..."
            
            $body = @{
                userId = "default"
                preserveData = $false
            } | ConvertTo-Json
            
            $response = Invoke-WebRequest -Uri $apiUrl -Method POST -Body $body -ContentType "application/json" -TimeoutSec 5
            
            if ($response.StatusCode -eq 200) {
                Write-Success "Backend wizard state reset successfully"
            } else {
                Show-Warning "Backend returned status code: $($response.StatusCode)"
            }
        }
    } catch {
        # This is optional, so just warn
        Show-Warning "Could not reset wizard state via backend API: $($_.Exception.Message)"
        Write-Info "This is normal if the backend is not running"
    }
}

# Execute wizard state reset
Reset-WizardState

# Optionally try to call backend reset (best effort)
if (-not $DryRun) {
    Reset-WizardBackendState
}

Write-Host ""

# ========================================
# Step 7: Clean User Content (Optional)
# ========================================
if ($IncludeUserContent) {
    Write-Info "Step 7: Cleaning user content (as requested)..."
    Write-Host ""
    
    $documentsPath = "$env:USERPROFILE\Documents\Aura Video Studio"
    if (Remove-PathSafely $documentsPath "User documents") {
        $cleanupStats.Removed++
    } else {
        $cleanupStats.NotFound++
    }
    
    $videosPath = "$env:USERPROFILE\Videos\Aura Studio"
    if (Remove-PathSafely $videosPath "User videos") {
        $cleanupStats.Removed++
    } else {
        $cleanupStats.NotFound++
    }
    
    Write-Host ""
} else {
    Write-Info "Step 7: Preserving user content (use -IncludeUserContent to remove)"
    Write-Host ""
    
    $documentsPath = "$env:USERPROFILE\Documents\Aura Video Studio"
    if (Test-Path $documentsPath) {
        Write-Info "Preserved: User documents at $documentsPath"
    }
    
    $videosPath = "$env:USERPROFILE\Videos\Aura Studio"
    if (Test-Path $videosPath) {
        Write-Info "Preserved: User videos at $videosPath"
    }
    
    Write-Host ""
}

# ========================================
# Summary
# ========================================
Write-Host "========================================" -ForegroundColor $InfoColor
Write-Host "Cleanup Summary" -ForegroundColor $InfoColor
Write-Host "========================================" -ForegroundColor $InfoColor

if ($DryRun) {
    Write-Host "Mode: DRY RUN (no files were actually removed)" -ForegroundColor $WarningColor
} else {
    Write-Host "Mode: LIVE (files were removed)" -ForegroundColor $SuccessColor
}

Write-Host ""
Write-Host "Items removed:    $($cleanupStats.Removed)" -ForegroundColor $SuccessColor
Write-Host "Already clean:    $($cleanupStats.NotFound)" -ForegroundColor $InfoColor
Write-Host ""

if ($DryRun) {
    Write-Info "This was a dry run. Run without -DryRun to actually remove files."
} else {
    Write-Success "Cleanup complete!"
    Write-Host ""
    Write-Info "The environment is now clean and ready for fresh testing."
    Write-Info "Next time you build and run the application, it will be like the first run."
}

Write-Host ""
Write-Host "========================================" -ForegroundColor $InfoColor

if (-not $DryRun) {
    Write-Host ""
    Write-Success "All done! 🎉"
}
