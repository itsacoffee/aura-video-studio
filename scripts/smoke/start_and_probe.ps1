#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Smoke test script for Aura Video Studio startup sanity checks.

.DESCRIPTION
    This script performs critical sanity checks to ensure the application starts cleanly:
    - Builds the solution with dotnet build
    - Runs core unit/integration tests
    - Starts Aura.Api in background
    - Probes health and capabilities endpoints
    - Monitors logs for exceptions over 30 seconds
    - Returns non-zero exit code on failure

.PARAMETER SkipBuild
    Skip the build step (useful if already built)

.PARAMETER SkipTests
    Skip running tests (useful for quick checks)

.PARAMETER TestTimeout
    Timeout in seconds for test execution (default: 120)

.PARAMETER ProbeTimeout
    Timeout in seconds for endpoint probing (default: 30)

.EXAMPLE
    .\start_and_probe.ps1
    Run full smoke test with all checks

.EXAMPLE
    .\start_and_probe.ps1 -SkipBuild
    Skip build and run checks only

.EXAMPLE
    .\start_and_probe.ps1 -SkipTests -ProbeTimeout 60
    Skip tests and monitor for 60 seconds
#>

[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [int]$TestTimeout = 120,
    [int]$ProbeTimeout = 30
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Colors for output
function Write-Success { param($Message) Write-Host "✓ $Message" -ForegroundColor Green }
function Write-Info { param($Message) Write-Host "→ $Message" -ForegroundColor Cyan }
function Write-Warning { param($Message) Write-Host "⚠ $Message" -ForegroundColor Yellow }
function Write-Error { param($Message) Write-Host "✗ $Message" -ForegroundColor Red }

# Configuration
$ApiBase = "http://127.0.0.1:5005"
$ApiPort = 5005
$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

Write-Info "╔═══════════════════════════════════════════════════════╗"
Write-Info "║   Aura Video Studio - Startup Smoke Check            ║"
Write-Info "╚═══════════════════════════════════════════════════════╝"
Write-Info ""

# Change to repository root
Push-Location $RepoRoot

try {
    # Step 1: Build the solution
    if (-not $SkipBuild) {
        Write-Info "[1/5] Building solution..."
        # Build only core projects to avoid Windows-only Aura.App issues on Linux
        $buildProjects = @(
            "Aura.Core/Aura.Core.csproj",
            "Aura.Providers/Aura.Providers.csproj",
            "Aura.Api/Aura.Api.csproj",
            "Aura.Tests/Aura.Tests.csproj"
        )
        
        foreach ($project in $buildProjects) {
            $buildOutput = dotnet build $project --nologo 2>&1
            if ($LASTEXITCODE -ne 0) {
                Write-Error "Build failed for $project!"
                Write-Host $buildOutput
                exit 1
            }
        }
        Write-Success "Build completed successfully"
    } else {
        Write-Info "[1/5] Skipping build (--skip-build)"
    }

    # Step 2: Run core tests
    if (-not $SkipTests) {
        Write-Info "[2/5] Running core tests (Aura.Tests)..."
        $testOutput = dotnet test Aura.Tests/Aura.Tests.csproj --no-build --nologo --verbosity quiet 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Tests failed!"
            Write-Host $testOutput
            exit 1
        }
        # Parse test results
        $passedTests = $testOutput | Select-String "Passed!" | Select-Object -First 1
        if ($passedTests) {
            Write-Success "Tests passed: $passedTests"
        } else {
            Write-Success "Tests completed"
        }
    } else {
        Write-Info "[2/5] Skipping tests (--skip-tests)"
    }

    # Step 3: Start Aura.Api in background
    Write-Info "[3/5] Starting Aura.Api in background..."
    
    # Check if port is already in use
    $portInUse = Get-NetTCPConnection -LocalPort $ApiPort -ErrorAction SilentlyContinue
    if ($portInUse) {
        Write-Warning "Port $ApiPort is already in use. Attempting to continue..."
    }

    # Start API in background
    $apiProcess = Start-Process -FilePath "dotnet" `
        -ArgumentList "run", "--project", "Aura.Api/Aura.Api.csproj", "--no-build" `
        -PassThru `
        -NoNewWindow `
        -RedirectStandardOutput "logs/smoke-stdout.log" `
        -RedirectStandardError "logs/smoke-stderr.log"

    Write-Success "Aura.Api started (PID: $($apiProcess.Id))"
    
    # Wait for API to be ready (max 15 seconds)
    Write-Info "Waiting for API to become ready..."
    $maxWait = 15
    $waited = 0
    $apiReady = $false

    while ($waited -lt $maxWait) {
        Start-Sleep -Seconds 1
        $waited++
        
        try {
            $response = Invoke-RestMethod -Uri "$ApiBase/api/healthz" -Method Get -TimeoutSec 2 -ErrorAction SilentlyContinue
            if ($response.status -eq "healthy") {
                $apiReady = $true
                break
            }
        } catch {
            # API not ready yet, continue waiting
        }
    }

    if (-not $apiReady) {
        Write-Error "API did not become ready within $maxWait seconds"
        Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
        exit 1
    }

    Write-Success "API is ready after $waited seconds"

    # Step 4: Probe critical endpoints
    Write-Info "[4/5] Probing critical endpoints..."
    
    # Probe 1: Health check
    try {
        $healthResponse = Invoke-RestMethod -Uri "$ApiBase/api/healthz" -Method Get
        if ($healthResponse.status -eq "healthy") {
            Write-Success "GET /api/healthz - OK"
            if ($healthResponse.timestamp) {
                Write-Info "  Server time: $($healthResponse.timestamp)"
            }
        } else {
            Write-Error "Health check returned unexpected status: $($healthResponse.status)"
            throw "Health check failed"
        }
    } catch {
        Write-Error "GET /api/healthz - FAILED"
        Write-Error "  $($_.Exception.Message)"
        Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
        exit 1
    }

    # Probe 2: Capabilities
    try {
        $capabilitiesResponse = Invoke-RestMethod -Uri "$ApiBase/api/capabilities" -Method Get
        Write-Success "GET /api/capabilities - OK"
        if ($capabilitiesResponse.tier) {
            Write-Info "  Hardware Tier: $($capabilitiesResponse.tier)"
        }
        if ($capabilitiesResponse.cpu.cores) {
            Write-Info "  CPU Cores: $($capabilitiesResponse.cpu.cores)"
        }
    } catch {
        Write-Error "GET /api/capabilities - FAILED"
        Write-Error "  $($_.Exception.Message)"
        Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
        exit 1
    }

    # Probe 3: Queue (jobs/artifacts endpoint)
    try {
        $queueResponse = Invoke-RestMethod -Uri "$ApiBase/api/queue" -Method Get
        Write-Success "GET /api/queue - OK"
        if ($queueResponse.jobs) {
            Write-Info "  Jobs in queue: $($queueResponse.jobs.Count)"
        }
    } catch {
        Write-Error "GET /api/queue - FAILED"
        Write-Error "  $($_.Exception.Message)"
        Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
        exit 1
    }

    # Step 5: Monitor logs for exceptions
    Write-Info "[5/5] Monitoring logs for $ProbeTimeout seconds..."
    $logFile = Get-ChildItem -Path "logs" -Filter "aura-api-*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    
    if (-not $logFile) {
        # Fallback to smoke-stdout.log if no dated log file exists
        if (Test-Path "logs/smoke-stdout.log") {
            $logFile = Get-Item "logs/smoke-stdout.log"
            Write-Info "Using smoke-stdout.log for monitoring"
        } else {
            Write-Warning "No log file found - creating new monitoring session"
            # Wait and retry
            Start-Sleep -Seconds 2
            $logFile = Get-ChildItem -Path "logs" -Filter "aura-api-*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
            if (-not $logFile -and (Test-Path "logs/smoke-stdout.log")) {
                $logFile = Get-Item "logs/smoke-stdout.log"
            }
        }
    }

    $initialLineCount = 0
    if ($logFile) {
        $initialLineCount = (Get-Content $logFile.FullName).Count
        Write-Info "Monitoring log file: $($logFile.Name) (current lines: $initialLineCount)"
    }

    # Monitor for specified duration
    $monitorStart = Get-Date
    $exceptionCount = 0
    $errorCount = 0
    $correlationIds = @()

    while (((Get-Date) - $monitorStart).TotalSeconds -lt $ProbeTimeout) {
        Start-Sleep -Seconds 2
        
        if ($logFile) {
            $currentLines = Get-Content $logFile.FullName
            $newLines = $currentLines | Select-Object -Skip $initialLineCount
            
            foreach ($line in $newLines) {
                # Check for exceptions or errors
                if ($line -match "\[ERR\]|\[FTL\]|Exception|Error:") {
                    if ($line -match "Exception") {
                        $exceptionCount++
                        Write-Warning "Exception found: $($line.Substring(0, [Math]::Min(120, $line.Length)))"
                    } else {
                        $errorCount++
                    }
                }
                
                # Extract correlation IDs
                if ($line -match "\[([a-f0-9]{32})\]") {
                    $correlationId = $Matches[1]
                    if ($correlationId -notin $correlationIds) {
                        $correlationIds += $correlationId
                    }
                }
            }
            
            $initialLineCount = $currentLines.Count
        }
    }

    Write-Success "Monitoring complete"
    Write-Info "  Duration: $ProbeTimeout seconds"
    Write-Info "  Exceptions found: $exceptionCount"
    Write-Info "  Errors found: $errorCount"
    
    if ($correlationIds.Count -gt 0) {
        Write-Info "  Correlation IDs seen: $($correlationIds.Count)"
        $correlationIds | Select-Object -First 3 | ForEach-Object {
            Write-Info "    - $_"
        }
    }

    # Determine final status
    $exitCode = 0
    if ($exceptionCount -gt 0) {
        Write-Error "Found $exceptionCount exception(s) in logs - test FAILED"
        $exitCode = 1
    } elseif ($errorCount -gt 5) {
        Write-Warning "Found $errorCount error(s) in logs - may indicate issues"
    } else {
        Write-Success "No critical exceptions found"
    }

    # Cleanup: Stop API process
    Write-Info "Stopping Aura.Api (PID: $($apiProcess.Id))..."
    Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    
    if (Get-Process -Id $apiProcess.Id -ErrorAction SilentlyContinue) {
        Write-Warning "API process still running, forcing termination..."
        Stop-Process -Id $apiProcess.Id -Force
    }

    Write-Info ""
    Write-Info "╔═══════════════════════════════════════════════════════╗"
    if ($exitCode -eq 0) {
        Write-Success "║   Smoke test PASSED - Application is healthy         ║"
    } else {
        Write-Error "║   Smoke test FAILED - Check logs for details         ║"
    }
    Write-Info "╚═══════════════════════════════════════════════════════╝"

    exit $exitCode

} catch {
    Write-Error "Smoke test encountered an error: $($_.Exception.Message)"
    Write-Error $_.ScriptStackTrace
    
    # Try to stop API if it's running
    if ($apiProcess) {
        Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
    }
    
    exit 1
} finally {
    Pop-Location
}
