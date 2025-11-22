<#
.SYNOPSIS
    Quick fix script to resolve database schema issues after merging the database tables PR.

.DESCRIPTION
    This script will:
    1. Stop any running Aura.Api processes
    2. Backup the existing database (if it exists)
    3. Delete the old database to trigger recreation
    4. Optionally restart the API to apply migrations

.PARAMETER SkipBackup
    Skip creating a backup of the existing database

.PARAMETER NoRestart
    Don't restart the API after cleanup

.EXAMPLE
    .\fix-database.ps1
    Standard execution with backup and restart

.EXAMPLE
    .\fix-database.ps1 -SkipBackup -NoRestart
    Quick cleanup without backup or restart
#>

param(
    [switch]$SkipBackup,
    [switch]$NoRestart
)

$ErrorActionPreference = "Stop"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  Aura Database Fix Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Define paths
$auraDataRoot = Join-Path $env:LOCALAPPDATA "Aura"
$databasePath = Join-Path $auraDataRoot "aura.db"
$databaseWalPath = "$databasePath-wal"
$databaseShmPath = "$databasePath-shm"
$backupDir = Join-Path $auraDataRoot "backups"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

Write-Host "Step 1: Stopping Aura.Api processes..." -ForegroundColor Yellow
$auraProcesses = Get-Process -Name "Aura.Api" -ErrorAction SilentlyContinue
if ($auraProcesses) {
    $auraProcesses | Stop-Process -Force
    Write-Host "  ✓ Stopped $($auraProcesses.Count) process(es)" -ForegroundColor Green
    Start-Sleep -Seconds 2
} else {
    Write-Host "  ℹ No running Aura.Api processes found" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Step 2: Checking for existing database..." -ForegroundColor Yellow
if (Test-Path $databasePath) {
    Write-Host "  ✓ Found database at: $databasePath" -ForegroundColor Green
    
    if (-not $SkipBackup) {
        Write-Host ""
        Write-Host "Step 3: Creating backup..." -ForegroundColor Yellow
        
        # Create backup directory if it doesn't exist
        if (-not (Test-Path $backupDir)) {
            New-Item -Path $backupDir -ItemType Directory | Out-Null
            Write-Host "  ✓ Created backup directory" -ForegroundColor Green
        }
        
        # Backup main database file
        $backupPath = Join-Path $backupDir "aura_backup_$timestamp.db"
        Copy-Item -Path $databasePath -Destination $backupPath -Force
        Write-Host "  ✓ Database backed up to: $backupPath" -ForegroundColor Green
        
        # Backup WAL and SHM files if they exist
        if (Test-Path $databaseWalPath) {
            Copy-Item -Path $databaseWalPath -Destination "$backupPath-wal" -Force
            Write-Host "  ✓ WAL file backed up" -ForegroundColor Green
        }
        if (Test-Path $databaseShmPath) {
            Copy-Item -Path $databaseShmPath -Destination "$backupPath-shm" -Force
            Write-Host "  ✓ SHM file backed up" -ForegroundColor Green
        }
        
        $backupSize = (Get-Item $backupPath).Length / 1MB
        Write-Host "  ℹ Backup size: $([math]::Round($backupSize, 2)) MB" -ForegroundColor Gray
    } else {
        Write-Host ""
        Write-Host "Step 3: Skipping backup (as requested)..." -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "Step 4: Deleting old database..." -ForegroundColor Yellow
    
    try {
        Remove-Item -Path $databasePath -Force
        Write-Host "  ✓ Deleted: aura.db" -ForegroundColor Green
        
        if (Test-Path $databaseWalPath) {
            Remove-Item -Path $databaseWalPath -Force
            Write-Host "  ✓ Deleted: aura.db-wal" -ForegroundColor Green
        }
        
        if (Test-Path $databaseShmPath) {
            Remove-Item -Path $databaseShmPath -Force
            Write-Host "  ✓ Deleted: aura.db-shm" -ForegroundColor Green
        }
    } catch {
        Write-Host "  ✗ Error deleting database files: $_" -ForegroundColor Red
        Write-Host "  ℹ Make sure no applications are using the database" -ForegroundColor Gray
        exit 1
    }
} else {
    Write-Host "  ℹ No existing database found" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Step 3: Skipping backup (no database exists)..." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Step 4: Skipping deletion (no database exists)..." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  Cleanup Complete!" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

if (-not $NoRestart) {
    Write-Host "Step 5: Restarting Aura.Api..." -ForegroundColor Yellow
    Write-Host ""
    
    # Try to find the project root
    $scriptRoot = Split-Path -Parent $PSScriptRoot
    $apiProjectPath = Join-Path $scriptRoot "Aura.Api\Aura.Api.csproj"
    
    if (Test-Path $apiProjectPath) {
        Write-Host "  ℹ Starting API at: $apiProjectPath" -ForegroundColor Gray
        Write-Host "  ℹ The API will automatically apply migrations and create missing tables" -ForegroundColor Gray
        Write-Host ""
        
        # Start the API in a new window
        Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$scriptRoot'; dotnet run --project Aura.Api"
        
        Write-Host "  ✓ API started in new window" -ForegroundColor Green
        Write-Host ""
        Write-Host "  📋 Watch the new window for:" -ForegroundColor Cyan
        Write-Host "     - 'Applying X pending migrations'" -ForegroundColor Gray
        Write-Host "     - 'Database initialization completed successfully'" -ForegroundColor Gray
        Write-Host "     - 'Now listening on: http://0.0.0.0:5005'" -ForegroundColor Gray
    } else {
        Write-Host "  ⚠ Could not find Aura.Api project" -ForegroundColor Yellow
        Write-Host "  ℹ Please manually run: dotnet run --project Aura.Api" -ForegroundColor Gray
    }
} else {
    Write-Host "Step 5: Skipping API restart (as requested)..." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  ℹ To start the API manually, run:" -ForegroundColor Gray
    Write-Host "     cd <project-root>" -ForegroundColor Gray
    Write-Host "     dotnet run --project Aura.Api" -ForegroundColor Gray
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  Next Steps" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Wait for the API to start (check the new window)" -ForegroundColor White
Write-Host "2. Look for the message: 'Application started. Press Ctrl+C to shut down.'" -ForegroundColor White
Write-Host "3. Open your browser to: http://127.0.0.1:5005" -ForegroundColor White
Write-Host "4. The frontend should now connect successfully!" -ForegroundColor White
Write-Host ""
if (-not $SkipBackup -and (Test-Path $backupDir)) {
    Write-Host "💾 Database backup location: $backupDir" -ForegroundColor Cyan
    Write-Host ""
}

Write-Host "✅ All done! Your database schema is now fixed." -ForegroundColor Green
Write-Host ""