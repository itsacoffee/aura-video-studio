@echo off
REM Install FFmpeg for Aura Video Studio (Windows CMD)
REM Simple batch script for downloading and installing FFmpeg
REM
REM Usage: install-ffmpeg-simple.bat

setlocal enabledelayedexpansion

echo ======================================
echo FFmpeg Installer for Aura Video Studio
echo ======================================
echo.

REM Check if PowerShell is available
where powershell >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo Error: PowerShell is required but not found.
    echo Please use PowerShell to run install-ffmpeg-windows.ps1 instead.
    pause
    exit /b 1
)

echo This script requires PowerShell to download and extract FFmpeg.
echo.
echo Options:
echo   1. Install from gyan.dev (recommended, smaller download)
echo   2. Install from GitHub (alternative mirror)
echo   3. Open manual installation guide
echo   0. Cancel
echo.

set /p choice="Select an option (1-3, 0 to cancel): "

if "%choice%"=="1" (
    echo.
    echo Installing from gyan.dev...
    echo.
    powershell -ExecutionPolicy Bypass -File "%~dp0install-ffmpeg-windows.ps1" -Source gyan
    goto end
)

if "%choice%"=="2" (
    echo.
    echo Installing from GitHub...
    echo.
    powershell -ExecutionPolicy Bypass -File "%~dp0install-ffmpeg-windows.ps1" -Source github
    goto end
)

if "%choice%"=="3" (
    echo.
    echo Opening manual installation guide...
    start https://github.com/Coffee285/aura-video-studio/blob/main/docs/INSTALLATION.md
    echo.
    echo The guide has been opened in your browser.
    pause
    exit /b 0
)

if "%choice%"=="0" (
    echo.
    echo Installation cancelled.
    pause
    exit /b 0
)

echo.
echo Invalid choice. Please run the script again.
pause
exit /b 1

:end
echo.
if %ERRORLEVEL% equ 0 (
    echo Installation completed successfully!
    echo.
    echo Next steps:
    echo   1. Open Aura Video Studio
    echo   2. Go to Download Center -^> Engines tab
    echo   3. Click 'Rescan' on the FFmpeg card
    echo.
) else (
    echo Installation failed. Check the error messages above.
    echo.
    echo For help, see: docs\INSTALLATION.md
    echo.
)

pause
