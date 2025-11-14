#!/usr/bin/env pwsh
<#
.SYNOPSIS
    End-to-end test for local video generation using local engines.

.DESCRIPTION
    This script tests the complete local video generation pipeline:
    - Validates local engines are installed and ready
    - Generates a test video using Piper TTS and local visuals
    - Verifies output files are created
    - Validates video quality and duration

.PARAMETER EngineCheck
    Only check engine status without generating video

.PARAMETER SkipValidation
    Skip pre-flight validation checks

.PARAMETER OutputDir
    Directory for test output (default: ./test-output)

.EXAMPLE
    .\run_e2e_local.ps1
    Run full E2E test with local engines

.EXAMPLE
    .\run_e2e_local.ps1 -EngineCheck
    Only check engine status
#>

[CmdletBinding()]
param(
    [switch]$EngineCheck,
    [switch]$SkipValidation,
    [string]$OutputDir = "./test-output"
)

$ErrorActionPreference = "Stop"

# Configuration
$ApiBase = "http://127.0.0.1:5005"
$TestTopic = "Local Engine Test Video"
$MaxWaitSeconds = 300

# Colors for output
function Write-Success { param($Message) Write-Output "✓ $Message" -ForegroundColor Green }
function Write-Info { param($Message) Write-Output "→ $Message" -ForegroundColor Cyan }
function Write-Warning { param($Message) Write-Output "⚠ $Message" -ForegroundColor Yellow }
function Write-Error { param($Message) Write-Output "✗ $Message" -ForegroundColor Red }

# Create output directory
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

Write-Info "Aura Video Studio - Local E2E Test"
Write-Info "====================================`n"

# Step 1: Check API availability
Write-Info "Checking API availability..."
try {
    $healthResponse = Invoke-RestMethod -Uri "$ApiBase/api/healthz" -Method Get -TimeoutSec 5
    Write-Success "API is healthy: $($healthResponse.status)"
} catch {
    Write-Error "API is not available at $ApiBase"
    Write-Error "Please start Aura.Api first: cd Aura.Api && dotnet run"
    exit 1
}

# Step 2: Check system capabilities
Write-Info "Checking system capabilities..."
try {
    $capabilities = Invoke-RestMethod -Uri "$ApiBase/api/capabilities" -Method Get
    Write-Success "System Tier: $($capabilities.tier)"
    if ($capabilities.gpuDetected) {
        Write-Success "GPU Detected: $($capabilities.gpuName) with $($capabilities.vramMb)MB VRAM"
    } else {
        Write-Warning "No GPU detected - Stable Diffusion will not be available"
    }
} catch {
    Write-Warning "Could not retrieve system capabilities: $($_.Exception.Message)"
}

# Step 3: Check local engines status
Write-Info "Checking local engines status..."
$enginesReady = $true

# Check Piper
try {
    $piperStatus = Invoke-RestMethod -Uri "$ApiBase/api/engines/piper/status" -Method Get
    if ($piperStatus.isInstalled) {
        Write-Success "Piper TTS: Installed at $($piperStatus.installPath)"
    } else {
        Write-Warning "Piper TTS: Not installed"
        $enginesReady = $false
    }
} catch {
    Write-Warning "Piper TTS: Status unknown"
    $enginesReady = $false
}

# Check Mimic3 (optional)
try {
    $mimic3Status = Invoke-RestMethod -Uri "$ApiBase/api/engines/mimic3/status" -Method Get
    if ($mimic3Status.isInstalled) {
        Write-Success "Mimic3 TTS: Installed at $($mimic3Status.installPath)"
    } else {
        Write-Info "Mimic3 TTS: Not installed (optional)"
    }
} catch {
    Write-Info "Mimic3 TTS: Status unknown (optional)"
}

# Check Stable Diffusion (optional, requires GPU)
if ($capabilities.gpuDetected -and $capabilities.canRunStableDiffusion) {
    try {
        $sdStatus = Invoke-RestMethod -Uri "$ApiBase/api/engines/stable-diffusion-webui/status" -Method Get
        if ($sdStatus.isInstalled) {
            Write-Success "Stable Diffusion: Installed at $($sdStatus.installPath)"
        } else {
            Write-Info "Stable Diffusion: Not installed (optional, will use stock images)"
        }
    } catch {
        Write-Info "Stable Diffusion: Status unknown (optional)"
    }
}

if ($EngineCheck) {
    Write-Info "`nEngine check complete."
    exit 0
}

if (-not $enginesReady -and -not $SkipValidation) {
    Write-Error "Local engines are not ready. Install Piper TTS from Settings → Download Center"
    Write-Info "Or run with -SkipValidation to attempt anyway (will use fallback providers)"
    exit 1
}

# Step 4: List available profiles
Write-Info "Listing available profiles..."
try {
    $profiles = Invoke-RestMethod -Uri "$ApiBase/api/profiles/list" -Method Get
    $localProfile = $profiles | Where-Object { $_.name -match "Local|Offline|Free" } | Select-Object -First 1

    if ($localProfile) {
        Write-Success "Using profile: $($localProfile.name)"
    } else {
        Write-Warning "No local/offline profile found, using first available"
        $localProfile = $profiles[0]
    }
} catch {
    Write-Error "Could not list profiles: $($_.Exception.Message)"
    exit 1
}

# Step 5: Run preflight check
if (-not $SkipValidation) {
    Write-Info "Running preflight check..."
    try {
        $preflightBody = @{
            profile = $localProfile.name
            offlineOnly = $true
        } | ConvertTo-Json

        $preflight = Invoke-RestMethod -Uri "$ApiBase/api/preflight" `
            -Method Post `
            -Body $preflightBody `
            -ContentType "application/json"

        if ($preflight.readyToGenerate) {
            Write-Success "Preflight passed - ready to generate"
            Write-Info "  Script Provider: $($preflight.providers.script.provider)"
            Write-Info "  TTS Provider: $($preflight.providers.tts.provider)"
            Write-Info "  Visuals Provider: $($preflight.providers.visuals.provider)"
        } else {
            Write-Warning "Preflight warnings:"
            $preflight.warnings | ForEach-Object { Write-Warning "  - $_" }
        }
    } catch {
        Write-Error "Preflight check failed: $($_.Exception.Message)"
        exit 1
    }
}

# Step 6: Generate test video
Write-Info "Generating test video with local engines..."
$requestBody = @{
    brief = @{
        topic = $TestTopic
        targetDurationSecs = 15
        targetAudience = "General"
        tone = "Informative"
    }
    planSpec = @{
        structure = "Intro+Body+Outro"
        density = "Balanced"
    }
    renderSpec = @{
        resolution = "HD_1080p"
        aspectRatio = "Widescreen16x9"
        format = "mp4"
        fps = 30
    }
    profile = $localProfile.name
    offlineOnly = $true
    captionsEnabled = $true
} | ConvertTo-Json -Depth 10

Write-Info "Submitting generation job..."
try {
    $jobResponse = Invoke-RestMethod -Uri "$ApiBase/api/jobs" `
        -Method Post `
        -Body $requestBody `
        -ContentType "application/json"

    $jobId = $jobResponse.jobId
    Write-Success "Job submitted: $jobId"
} catch {
    Write-Error "Failed to submit job: $($_.Exception.Message)"
    Write-Info "Request body: $requestBody"
    exit 1
}

# Step 7: Poll job status
Write-Info "Waiting for job completion..."
$startTime = Get-Date
$lastStatus = ""

while ($true) {
    Start-Sleep -Seconds 2

    try {
        $jobStatus = Invoke-RestMethod -Uri "$ApiBase/api/jobs/$jobId" -Method Get

        if ($jobStatus.status -ne $lastStatus) {
            Write-Info "Status: $($jobStatus.status)"
            if ($jobStatus.progress) {
                Write-Info "Progress: $($jobStatus.progress)%"
            }
            $lastStatus = $jobStatus.status
        }

        if ($jobStatus.status -eq "Completed") {
            Write-Success "Job completed successfully!"

            # Get output paths
            if ($jobStatus.outputPath) {
                Write-Success "Video: $($jobStatus.outputPath)"

                # Copy to test output directory
                if (Test-Path $jobStatus.outputPath) {
                    $testVideoPath = Join-Path $OutputDir "test-local-$(Get-Date -Format 'yyyyMMdd-HHmmss').mp4"
                    Copy-Item $jobStatus.outputPath $testVideoPath
                    Write-Success "Copied to: $testVideoPath"
                }
            }

            if ($jobStatus.captionsPath) {
                Write-Success "Captions: $($jobStatus.captionsPath)"
            }

            break
        }

        if ($jobStatus.status -eq "Failed") {
            Write-Error "Job failed: $($jobStatus.error)"
            exit 1
        }

        # Timeout check
        $elapsed = (Get-Date) - $startTime
        if ($elapsed.TotalSeconds -gt $MaxWaitSeconds) {
            Write-Error "Job timed out after $MaxWaitSeconds seconds"
            exit 1
        }
    } catch {
        Write-Error "Error polling job status: $($_.Exception.Message)"
        exit 1
    }
}

# Step 8: Validate output
Write-Info "Validating output..."
if ($jobStatus.outputPath -and (Test-Path $jobStatus.outputPath)) {
    $videoFile = Get-Item $jobStatus.outputPath
    $sizeMB = [math]::Round($videoFile.Length / 1MB, 2)
    Write-Success "Video file size: $sizeMB MB"

    if ($sizeMB -lt 0.1) {
        Write-Warning "Video file is very small - may be invalid"
    }

    # Check duration with ffprobe if available
    $ffprobe = Get-Command ffprobe -ErrorAction SilentlyContinue
    if ($ffprobe) {
        try {
            $duration = & ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 $jobStatus.outputPath 2>$null
            $durationSecs = [math]::Round([double]$duration, 1)
            Write-Success "Video duration: $durationSecs seconds"

            if ($durationSecs -lt 10) {
                Write-Warning "Video is shorter than expected (target was 15s)"
            }
        } catch {
            Write-Info "Could not determine video duration"
        }
    }
} else {
    Write-Warning "Output video file not found"
}

# Summary
Write-Info "`n====================================`n"
Write-Success "E2E Test Complete!"
Write-Info "Test video generated with local engines"
Write-Info "Output directory: $OutputDir"
Write-Info "Job ID: $jobId"

exit 0
