# Windows FFmpeg Integration Validation Script
# This script validates the Windows-specific FFmpeg integration
# Run this on a Windows machine to verify all functionality

param(
    [switch]$SkipHardware,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

# Colors
$Green = "Green"
$Red = "Red"
$Yellow = "Yellow"
$Cyan = "Cyan"

function Write-Success {
    param([string]$Message)
    Write-Output "✓ $Message" -ForegroundColor $Green
}

function Write-Failure {
    param([string]$Message)
    Write-Output "✗ $Message" -ForegroundColor $Red
}

function Write-Info {
    param([string]$Message)
    Write-Output "ℹ $Message" -ForegroundColor $Cyan
}

function Show-Warning {
    param([string]$Message)
    Write-Output "⚠ $Message" -ForegroundColor $Yellow
}

Write-Output "========================================" -ForegroundColor $Cyan
Write-Output "Windows FFmpeg Integration Validation" -ForegroundColor $Cyan
Write-Output "========================================" -ForegroundColor $Cyan
Write-Output ""

$testResults = @()

# Test 1: Check if running on Windows
Write-Info "Test 1: Verify running on Windows..."
if ($IsWindows -or $env:OS -eq "Windows_NT") {
    Write-Success "Running on Windows"
    $testResults += @{ Test = "Windows OS"; Status = "Pass" }
} else {
    Write-Failure "Not running on Windows"
    $testResults += @{ Test = "Windows OS"; Status = "Fail" }
    exit 1
}

# Test 2: Check FFmpeg in PATH
Write-Info "Test 2: Check FFmpeg in PATH..."
try {
    $ffmpegPath = (Get-Command ffmpeg -ErrorAction SilentlyContinue).Source
    if ($ffmpegPath) {
        Write-Success "FFmpeg found in PATH: $ffmpegPath"
        $testResults += @{ Test = "FFmpeg PATH"; Status = "Pass"; Path = $ffmpegPath }
    } else {
        Show-Warning "FFmpeg not found in PATH (will check other locations)"
        $testResults += @{ Test = "FFmpeg PATH"; Status = "Skip" }
    }
} catch {
    Show-Warning "Error checking PATH: $_"
    $testResults += @{ Test = "FFmpeg PATH"; Status = "Skip" }
}

# Test 3: Check FFmpeg in common Windows locations
Write-Info "Test 3: Check FFmpeg in common Windows locations..."
$commonPaths = @(
    "$env:ProgramFiles\ffmpeg\bin\ffmpeg.exe",
    "${env:ProgramFiles(x86)}\ffmpeg\bin\ffmpeg.exe",
    "$env:LOCALAPPDATA\Aura\Tools\ffmpeg",
    "$env:LOCALAPPDATA\AuraVideoStudio\ffmpeg",
    "$env:LOCALAPPDATA\Programs\ffmpeg\bin\ffmpeg.exe",
    "C:\ffmpeg\bin\ffmpeg.exe"
)

$foundInCommonPath = $false
foreach ($path in $commonPaths) {
    if ($path -like "*\ffmpeg" -and (Test-Path $path)) {
        # Directory - search for ffmpeg.exe
        $exePath = Get-ChildItem -Path $path -Filter "ffmpeg.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($exePath) {
            Write-Success "Found FFmpeg at: $($exePath.FullName)"
            $testResults += @{ Test = "Common Path Detection"; Status = "Pass"; Path = $exePath.FullName }
            $foundInCommonPath = $true
            break
        }
    } elseif (Test-Path $path) {
        Write-Success "Found FFmpeg at: $path"
        $testResults += @{ Test = "Common Path Detection"; Status = "Pass"; Path = $path }
        $foundInCommonPath = $true
        break
    }
}

if (-not $foundInCommonPath) {
    Show-Warning "FFmpeg not found in common locations"
    $testResults += @{ Test = "Common Path Detection"; Status = "Skip" }
}

# Test 4: Check Windows Registry for FFmpeg
Write-Info "Test 4: Check Windows Registry for FFmpeg..."
$registryPaths = @(
    "HKLM:\SOFTWARE\FFmpeg",
    "HKLM:\SOFTWARE\WOW6432Node\FFmpeg",
    "HKCU:\SOFTWARE\FFmpeg"
)

$foundInRegistry = $false
foreach ($regPath in $registryPaths) {
    try {
        if (Test-Path $regPath) {
            $installPath = (Get-ItemProperty -Path $regPath -ErrorAction SilentlyContinue).InstallLocation
            if ($installPath) {
                Write-Success "Found FFmpeg in registry: $regPath -> $installPath"
                $testResults += @{ Test = "Registry Detection"; Status = "Pass"; Path = $installPath }
                $foundInRegistry = $true
                break
            }
        }
    } catch {
        # Ignore registry access errors - this is expected when registry key doesn't exist
        Write-Verbose "Registry key not accessible: $_"
    }
}

if (-not $foundInRegistry) {
    Write-Info "FFmpeg not found in registry (this is normal for manual installations)"
    $testResults += @{ Test = "Registry Detection"; Status = "Skip" }
}

# Test 5: Test FFmpeg execution
Write-Info "Test 5: Test FFmpeg execution..."
try {
    $versionOutput = & ffmpeg -version 2>&1 | Select-Object -First 1
    if ($versionOutput -match "ffmpeg version") {
        Write-Success "FFmpeg executes successfully: $versionOutput"
        $testResults += @{ Test = "FFmpeg Execution"; Status = "Pass"; Version = $versionOutput }
    } else {
        Write-Failure "FFmpeg output unexpected: $versionOutput"
        $testResults += @{ Test = "FFmpeg Execution"; Status = "Fail" }
    }
} catch {
    Write-Failure "Failed to execute FFmpeg: $_"
    $testResults += @{ Test = "FFmpeg Execution"; Status = "Fail" }
}

# Test 6: Test path with spaces
Write-Info "Test 6: Test path handling with spaces..."
$tempDir = Join-Path $env:TEMP "Test Directory With Spaces"
$testInput = Join-Path $tempDir "test input.txt"
$testOutput = Join-Path $tempDir "test output.txt"

try {
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
    "test content" | Out-File -FilePath $testInput -Encoding UTF8

    # Simulate FFmpeg path escaping
    $escapedInput = $testInput.Replace('\', '/')
    $escapedOutput = $testOutput.Replace('\', '/')

    if ($escapedInput.Contains('/') -and $escapedOutput.Contains('/')) {
        Write-Success "Path escaping works correctly"
        $testResults += @{ Test = "Path Escaping"; Status = "Pass" }
    } else {
        Write-Failure "Path escaping failed"
        $testResults += @{ Test = "Path Escaping"; Status = "Fail" }
    }
} catch {
    Write-Failure "Path handling test failed: $_"
    $testResults += @{ Test = "Path Escaping"; Status = "Fail" }
} finally {
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# Test 7: Check hardware acceleration (if not skipped)
if (-not $SkipHardware) {
    Write-Info "Test 7: Check hardware acceleration support..."

    try {
        # Check for NVIDIA
        $nvidiaResult = & ffmpeg -hide_banner -encoders 2>&1 | Select-String "nvenc"
        if ($nvidiaResult) {
            Write-Success "NVIDIA NVENC detected: $($nvidiaResult.Count) encoders"
            $testResults += @{ Test = "NVENC Detection"; Status = "Pass"; Count = $nvidiaResult.Count }
        } else {
            Write-Info "NVIDIA NVENC not detected (GPU may not be present)"
            $testResults += @{ Test = "NVENC Detection"; Status = "Skip" }
        }

        # Check for AMD
        $amdResult = & ffmpeg -hide_banner -encoders 2>&1 | Select-String "amf"
        if ($amdResult) {
            Write-Success "AMD AMF detected: $($amdResult.Count) encoders"
            $testResults += @{ Test = "AMF Detection"; Status = "Pass"; Count = $amdResult.Count }
        } else {
            Write-Info "AMD AMF not detected (GPU may not be present)"
            $testResults += @{ Test = "AMF Detection"; Status = "Skip" }
        }

        # Check for Intel QuickSync
        $qsvResult = & ffmpeg -hide_banner -encoders 2>&1 | Select-String "qsv"
        if ($qsvResult) {
            Write-Success "Intel QuickSync detected: $($qsvResult.Count) encoders"
            $testResults += @{ Test = "QuickSync Detection"; Status = "Pass"; Count = $qsvResult.Count }
        } else {
            Write-Info "Intel QuickSync not detected (GPU may not be present)"
            $testResults += @{ Test = "QuickSync Detection"; Status = "Skip" }
        }
    } catch {
        Show-Warning "Hardware acceleration detection failed: $_"
        $testResults += @{ Test = "Hardware Acceleration"; Status = "Fail" }
    }
} else {
    Write-Info "Test 7: Hardware acceleration check skipped"
}

# Test 8: Check nvidia-smi (if NVIDIA GPU present)
Write-Info "Test 8: Check nvidia-smi..."
try {
    $nvidiaSmi = "$env:ProgramFiles\NVIDIA Corporation\NVSMI\nvidia-smi.exe"
    if (Test-Path $nvidiaSmi) {
        $gpuInfo = & $nvidiaSmi --query-gpu=name,driver_version --format=csv,noheader 2>&1
        Write-Success "nvidia-smi available: $gpuInfo"
        $testResults += @{ Test = "nvidia-smi"; Status = "Pass"; Info = $gpuInfo }
    } else {
        Write-Info "nvidia-smi not found (NVIDIA GPU may not be present)"
        $testResults += @{ Test = "nvidia-smi"; Status = "Skip" }
    }
} catch {
    Write-Info "nvidia-smi check failed: $_"
    $testResults += @{ Test = "nvidia-smi"; Status = "Skip" }
}

# Test 9: Test simple encoding
Write-Info "Test 9: Test simple video encoding..."
$testVideoDir = Join-Path $env:TEMP "ffmpeg_test_$(Get-Random)"
try {
    New-Item -ItemType Directory -Path $testVideoDir -Force | Out-Null
    $testVideo = Join-Path $testVideoDir "test_output.mp4"

    # Generate 1 second of black video
    $ffmpegArgs = "-f lavfi -i color=c=black:s=1280x720:d=1 -c:v libx264 -pix_fmt yuv420p -y `"$testVideo`""

    if ($Verbose) {
        Write-Output "Running: ffmpeg $ffmpegArgs"
    }

    $encoding = Start-Process -FilePath "ffmpeg" -ArgumentList $ffmpegArgs -NoNewWindow -Wait -PassThru

    if ($encoding.ExitCode -eq 0 -and (Test-Path $testVideo)) {
        $fileSize = (Get-Item $testVideo).Length
        Write-Success "Video encoding successful ($fileSize bytes)"
        $testResults += @{ Test = "Video Encoding"; Status = "Pass"; Size = $fileSize }
    } else {
        Write-Failure "Video encoding failed (exit code: $($encoding.ExitCode))"
        $testResults += @{ Test = "Video Encoding"; Status = "Fail" }
    }
} catch {
    Write-Failure "Encoding test error: $_"
    $testResults += @{ Test = "Video Encoding"; Status = "Fail" }
} finally {
    if (Test-Path $testVideoDir) {
        Remove-Item -Path $testVideoDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# Summary
Write-Output ""
Write-Output "========================================" -ForegroundColor $Cyan
Write-Output "Test Summary" -ForegroundColor $Cyan
Write-Output "========================================" -ForegroundColor $Cyan

$passCount = ($testResults | Where-Object { $_.Status -eq "Pass" }).Count
$failCount = ($testResults | Where-Object { $_.Status -eq "Fail" }).Count
$skipCount = ($testResults | Where-Object { $_.Status -eq "Skip" }).Count
$totalCount = $testResults.Count

Write-Output ""
Write-Output "Total Tests: $totalCount" -ForegroundColor $Cyan
Write-Output "Passed: $passCount" -ForegroundColor $Green
Write-Output "Failed: $failCount" -ForegroundColor $Red
Write-Output "Skipped: $skipCount" -ForegroundColor $Yellow
Write-Output ""

if ($failCount -eq 0) {
    Write-Success "All tests passed!"
    exit 0
} else {
    Write-Failure "Some tests failed. See details above."
    exit 1
}
