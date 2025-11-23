# Aura Video Studio - Wizard Verification PowerShell Script
# This script helps verify the setup wizard functionality

Write-Host "================================" -ForegroundColor Cyan
Write-Host "Aura Video Studio - Wizard Test" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Function to check if a process is running
function Test-ProcessRunning {
    param([string]$ProcessName)
    return (Get-Process -Name $ProcessName -ErrorAction SilentlyContinue) -ne $null
}

# Function to read logs
function Get-RecentLogs {
    param([string]$LogPath, [int]$Lines = 50)
    if (Test-Path $LogPath) {
        Get-Content $LogPath -Tail $Lines | Write-Host -ForegroundColor Gray
    }
    else {
        Write-Host "Log file not found: $LogPath" -ForegroundColor Yellow
    }
}

# Check if Aura is running
Write-Host "1. Checking if Aura is running..." -ForegroundColor Yellow
if (Test-ProcessRunning "Aura Video Studio") {
    Write-Host "   ✓ Aura is running" -ForegroundColor Green

    Write-Host ""
    Write-Host "   To test fresh setup:" -ForegroundColor Cyan
    Write-Host "   - Close Aura" -ForegroundColor White
    Write-Host "   - Delete: $env:LOCALAPPDATA\aura-video-studio\aura.db" -ForegroundColor White
    Write-Host "   - Delete localStorage in browser DevTools" -ForegroundColor White
    Write-Host "   - Restart Aura" -ForegroundColor White
}
else {
    Write-Host "   ✗ Aura is not running" -ForegroundColor Red
}

Write-Host ""
Write-Host "2. Checking database..." -ForegroundColor Yellow
$dbPath = "$env:LOCALAPPDATA\aura-video-studio\aura.db"
if (Test-Path $dbPath) {
    Write-Host "   ✓ Database exists at: $dbPath" -ForegroundColor Green

    $dbSize = (Get-Item $dbPath).Length
    Write-Host "   Database size: $($dbSize / 1KB) KB" -ForegroundColor Gray

    # Check if SQLite is available
    try {
        $sqliteInstalled = (Get-Command sqlite3 -ErrorAction Stop) -ne $null
        Write-Host "   ✓ SQLite CLI available" -ForegroundColor Green
        Write-Host ""
        Write-Host "   To check user_setup table:" -ForegroundColor Cyan
        Write-Host "   sqlite3 `"$dbPath`" `"SELECT * FROM user_setup;`"" -ForegroundColor White
    }
    catch {
        Write-Host "   ℹ SQLite CLI not installed (optional)" -ForegroundColor Yellow
        Write-Host "   Download from: https://www.sqlite.org/download.html" -ForegroundColor Gray
    }
}
else {
    Write-Host "   ✗ Database not found (fresh install)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "3. Checking logs..." -ForegroundColor Yellow
$logDir = "$env:LOCALAPPDATA\aura-video-studio\logs"
if (Test-Path $logDir) {
    Write-Host "   ✓ Log directory exists" -ForegroundColor Green

    $recentLog = Get-ChildItem $logDir -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($recentLog) {
        Write-Host "   Most recent log: $($recentLog.Name)" -ForegroundColor Gray
        Write-Host "   Last modified: $($recentLog.LastWriteTime)" -ForegroundColor Gray

        Write-Host ""
        Write-Host "   Last 20 lines of log:" -ForegroundColor Cyan
        Get-RecentLogs -LogPath $recentLog.FullName -Lines 20
    }
}
else {
    Write-Host "   ℹ No logs found (app not run yet)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "4. Checking localStorage..." -ForegroundColor Yellow
Write-Host "   Open browser DevTools (F12) and run:" -ForegroundColor Cyan
Write-Host "   localStorage.getItem('hasCompletedFirstRun')" -ForegroundColor White
Write-Host "   localStorage.getItem('hasSeenOnboarding')" -ForegroundColor White

Write-Host ""
Write-Host "5. Manual Test Steps:" -ForegroundColor Yellow
Write-Host "   □ Launch Aura.exe" -ForegroundColor White
Write-Host "   □ Wait for backend to start (up to 60 seconds)" -ForegroundColor White
Write-Host "   □ Verify no 'Backend not reachable' error" -ForegroundColor White
Write-Host "   □ Complete all 6 wizard steps" -ForegroundColor White
Write-Host "   □ Click 'Save' on final step" -ForegroundColor White
Write-Host "   □ Verify exits to dashboard (NOT Step 1)" -ForegroundColor White
Write-Host "   □ Restart app" -ForegroundColor White
Write-Host "   □ Verify wizard does not reappear" -ForegroundColor White

Write-Host ""
Write-Host "6. To Reset for Testing:" -ForegroundColor Yellow
Write-Host "   Run this in PowerShell:" -ForegroundColor Cyan
Write-Host @"
   # Stop Aura
   Stop-Process -Name "Aura Video Studio" -Force -ErrorAction SilentlyContinue

   # Delete database
   Remove-Item "$env:LOCALAPPDATA\aura-video-studio\aura.db" -Force -ErrorAction SilentlyContinue

   # Clear logs (optional)
   Remove-Item "$env:LOCALAPPDATA\aura-video-studio\logs\*" -Force -ErrorAction SilentlyContinue

   Write-Host "Reset complete. Restart Aura for fresh wizard." -ForegroundColor Green
"@ -ForegroundColor White

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Test complete! Check results above." -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

