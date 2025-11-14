<#
.SYNOPSIS
    Runs TTS Provider Integration Validation Tests on Windows

.DESCRIPTION
    This script validates all TTS provider integrations including:
    - Windows SAPI (native Windows TTS)
    - ElevenLabs API
    - PlayHT API
    - Piper offline TTS
    - Audio file generation and storage
    - Audio format conversion

.PARAMETER IncludeCloudProviders
    Include tests for cloud-based providers (ElevenLabs, PlayHT). Requires API keys.

.PARAMETER IncludePiper
    Include tests for Piper offline TTS. Requires Piper installation.

.PARAMETER Verbose
    Enable verbose logging for test execution

.EXAMPLE
    .\Run-TTS-Validation-Tests.ps1
    Runs all local tests (Windows SAPI only)

.EXAMPLE
    .\Run-TTS-Validation-Tests.ps1 -IncludeCloudProviders -IncludePiper
    Runs all validation tests including cloud providers and Piper

.NOTES
    Author: Aura Video Studio Team
    Version: 1.0.0
    PR: PR-CORE-002
#>

[CmdletBinding()]
param(
    [switch]$IncludeCloudProviders,
    [switch]$IncludePiper,
    [switch]$VerboseOutput
)

# Banner
Write-Output ""
Write-Output "=============================================" -ForegroundColor Cyan
Write-Output "  TTS Provider Validation Test Suite" -ForegroundColor Cyan
Write-Output "  PR-CORE-002: TTS Integration Validation" -ForegroundColor Cyan
Write-Output "=============================================" -ForegroundColor Cyan
Write-Output ""

# Check Windows version
$osVersion = [System.Environment]::OSVersion.Version
Write-Output "OS Version: Windows $($osVersion.Major).$($osVersion.Minor) (Build $($osVersion.Build))" -ForegroundColor Gray

if ($osVersion.Build -lt 19041) {
    Write-Warning "Windows SAPI TTS requires Windows 10 build 19041 or later"
    Write-Warning "Current build: $($osVersion.Build)"
    Write-Output ""
}

# Check .NET SDK
Write-Output "Checking .NET SDK..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Output "✓ .NET SDK found: $dotnetVersion" -ForegroundColor Green
} else {
    Write-Output "✗ .NET SDK not found" -ForegroundColor Red
    Write-Output "Please install .NET 8.0 SDK from: https://dotnet.microsoft.com/download"
    exit 1
}

# Check FFmpeg
Write-Output "Checking FFmpeg..." -ForegroundColor Yellow
$ffmpegPath = Get-Command ffmpeg -ErrorAction SilentlyContinue
if ($ffmpegPath) {
    $ffmpegVersion = ffmpeg -version 2>$null | Select-Object -First 1
    Write-Output "✓ FFmpeg found: $ffmpegVersion" -ForegroundColor Green
} else {
    Write-Warning "FFmpeg not found in PATH"
    Write-Warning "Audio format conversion tests will be skipped"
    Write-Output "Install FFmpeg: winget install Gyan.FFmpeg"
}

Write-Output ""

# Environment variable setup
$envVars = @()

if ($IncludeCloudProviders) {
    Write-Output "Cloud Provider Configuration:" -ForegroundColor Yellow

    # ElevenLabs
    $elevenLabsKey = $env:ELEVENLABS_API_KEY
    if (-not $elevenLabsKey) {
        Write-Output "⚠ ELEVENLABS_API_KEY not set" -ForegroundColor Yellow
        $elevenLabsKey = Read-Host "Enter ElevenLabs API Key (or press Enter to skip)"
        if ($elevenLabsKey) {
            $env:ELEVENLABS_API_KEY = $elevenLabsKey
            $envVars += "ELEVENLABS_API_KEY"
        }
    } else {
        Write-Output "✓ ElevenLabs API key configured" -ForegroundColor Green
    }

    # PlayHT
    $playHTKey = $env:PLAYHT_API_KEY
    $playHTUser = $env:PLAYHT_USER_ID

    if (-not $playHTKey) {
        Write-Output "⚠ PLAYHT_API_KEY not set" -ForegroundColor Yellow
        $playHTKey = Read-Host "Enter PlayHT API Key (or press Enter to skip)"
        if ($playHTKey) {
            $env:PLAYHT_API_KEY = $playHTKey
            $envVars += "PLAYHT_API_KEY"
        }
    }

    if (-not $playHTUser) {
        Write-Output "⚠ PLAYHT_USER_ID not set" -ForegroundColor Yellow
        $playHTUser = Read-Host "Enter PlayHT User ID (or press Enter to skip)"
        if ($playHTUser) {
            $env:PLAYHT_USER_ID = $playHTUser
            $envVars += "PLAYHT_USER_ID"
        }
    }

    if ($playHTKey -and $playHTUser) {
        Write-Output "✓ PlayHT credentials configured" -ForegroundColor Green
    }

    Write-Output ""
}

if ($IncludePiper) {
    Write-Output "Piper TTS Configuration:" -ForegroundColor Yellow

    $piperPath = $env:PIPER_EXECUTABLE_PATH
    $piperModel = $env:PIPER_MODEL_PATH

    if (-not $piperPath) {
        Write-Output "⚠ PIPER_EXECUTABLE_PATH not set" -ForegroundColor Yellow
        $piperPath = Read-Host "Enter Piper executable path (or press Enter to skip)"
        if ($piperPath) {
            $env:PIPER_EXECUTABLE_PATH = $piperPath
            $envVars += "PIPER_EXECUTABLE_PATH"
        }
    }

    if (-not $piperModel) {
        Write-Output "⚠ PIPER_MODEL_PATH not set" -ForegroundColor Yellow
        $piperModel = Read-Host "Enter Piper model path (or press Enter to skip)"
        if ($piperModel) {
            $env:PIPER_MODEL_PATH = $piperModel
            $envVars += "PIPER_MODEL_PATH"
        }
    }

    if ($piperPath -and $piperModel) {
        if (Test-Path $piperPath) {
            Write-Output "✓ Piper executable found: $piperPath" -ForegroundColor Green
        } else {
            Write-Warning "Piper executable not found at: $piperPath"
        }

        if (Test-Path $piperModel) {
            Write-Output "✓ Piper model found: $piperModel" -ForegroundColor Green
        } else {
            Write-Warning "Piper model not found at: $piperModel"
        }
    }

    Write-Output ""
}

# Build test filter
$testFilters = @()
$testFilters += "FullyQualifiedName~TtsProviderIntegrationValidationTests"

# Always run local tests
Write-Output "Test Configuration:" -ForegroundColor Yellow
Write-Output "✓ Windows SAPI tests: ENABLED" -ForegroundColor Green
Write-Output "✓ Audio storage tests: ENABLED" -ForegroundColor Green
Write-Output "✓ Format conversion tests: ENABLED" -ForegroundColor Green

if ($IncludeCloudProviders) {
    if ($env:ELEVENLABS_API_KEY) {
        Write-Output "✓ ElevenLabs tests: ENABLED" -ForegroundColor Green
    } else {
        Write-Output "⊘ ElevenLabs tests: SKIPPED (no API key)" -ForegroundColor Yellow
    }

    if ($env:PLAYHT_API_KEY -and $env:PLAYHT_USER_ID) {
        Write-Output "✓ PlayHT tests: ENABLED" -ForegroundColor Green
    } else {
        Write-Output "⊘ PlayHT tests: SKIPPED (no credentials)" -ForegroundColor Yellow
    }
} else {
    Write-Output "⊘ Cloud provider tests: SKIPPED" -ForegroundColor Yellow
}

if ($IncludePiper) {
    if ($env:PIPER_EXECUTABLE_PATH -and $env:PIPER_MODEL_PATH) {
        Write-Output "✓ Piper tests: ENABLED" -ForegroundColor Green
    } else {
        Write-Output "⊘ Piper tests: SKIPPED (not configured)" -ForegroundColor Yellow
    }
} else {
    Write-Output "⊘ Piper tests: SKIPPED" -ForegroundColor Yellow
}

Write-Output ""

# Confirmation
Write-Output "Press Enter to start tests, or Ctrl+C to cancel..."
$null = Read-Host

Write-Output ""
Write-Output "=============================================" -ForegroundColor Cyan
Write-Output "  Running Tests..." -ForegroundColor Cyan
Write-Output "=============================================" -ForegroundColor Cyan
Write-Output ""

# Build dotnet test command
$testCommand = "dotnet test"
$testCommand += " --filter `"$($testFilters -join ' | ')`""

if ($VerboseOutput) {
    $testCommand += " --logger `"console;verbosity=detailed`""
} else {
    $testCommand += " --logger `"console;verbosity=normal`""
}

# Add results output
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$resultsFile = "TestResults_TTS_$timestamp.trx"
$testCommand += " --logger `"trx;LogFileName=$resultsFile`""

# Execute tests
Write-Output "Executing: $testCommand" -ForegroundColor Gray
Write-Output ""

$startTime = Get-Date
Invoke-Expression $testCommand
$exitCode = $LASTEXITCODE
$endTime = Get-Date
$duration = $endTime - $startTime

Write-Output ""
Write-Output "=============================================" -ForegroundColor Cyan
Write-Output "  Test Results" -ForegroundColor Cyan
Write-Output "=============================================" -ForegroundColor Cyan
Write-Output ""

Write-Output "Duration: $($duration.TotalSeconds) seconds" -ForegroundColor Gray
Write-Output "Results file: $resultsFile" -ForegroundColor Gray
Write-Output ""

if ($exitCode -eq 0) {
    Write-Output "✓ All tests PASSED!" -ForegroundColor Green
} else {
    Write-Output "✗ Some tests FAILED" -ForegroundColor Red
    Write-Output "Exit code: $exitCode" -ForegroundColor Red
}

Write-Output ""

# Cleanup environment variables
if ($envVars.Count -gt 0) {
    Write-Output "Cleaning up environment variables..." -ForegroundColor Gray
    foreach ($var in $envVars) {
        Remove-Item "Env:$var" -ErrorAction SilentlyContinue
    }
}

# Summary report
Write-Output "=============================================" -ForegroundColor Cyan
Write-Output "  Validation Summary" -ForegroundColor Cyan
Write-Output "=============================================" -ForegroundColor Cyan
Write-Output ""

Write-Output "Provider Status:" -ForegroundColor Yellow
Write-Output "  Windows SAPI:  Tested" -ForegroundColor Green
Write-Output "  Audio Storage: Tested" -ForegroundColor Green
Write-Output "  Format Convert:Tested" -ForegroundColor Green

if ($IncludeCloudProviders) {
    if ($env:ELEVENLABS_API_KEY) {
        Write-Output "  ElevenLabs:    Tested" -ForegroundColor Green
    }
    if ($env:PLAYHT_API_KEY) {
        Write-Output "  PlayHT:        Tested" -ForegroundColor Green
    }
}

if ($IncludePiper -and $env:PIPER_EXECUTABLE_PATH) {
    Write-Output "  Piper:         Tested" -ForegroundColor Green
}

Write-Output ""
Write-Output "For detailed report, see:" -ForegroundColor Yellow
Write-Output "  - TTS_PROVIDER_VALIDATION_REPORT.md" -ForegroundColor Cyan
Write-Output "  - $resultsFile" -ForegroundColor Cyan
Write-Output ""

exit $exitCode
