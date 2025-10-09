<# 
scripts/run_quick_generate_demo.ps1
Smoke test that generates a demo video.
Falls back to ffmpeg color bars if API not available.
#>

param(
  [string]$ApiBase = "http://127.0.0.1:5000",
  [string]$FfmpegPath = ".\scripts\ffmpeg\ffmpeg.exe",
  [int]$Seconds = 10
)

$ErrorActionPreference = "Stop"
$smokeStartTime = Get-Date

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Aura Video Studio - Smoke Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Create output directory
New-Item -ItemType Directory -Force -Path "artifacts/smoke" | Out-Null
$out = "artifacts/smoke/demo.mp4"

# Remove old output if exists
if (Test-Path $out) {
    Remove-Item $out -Force
}

function Invoke-Api($method, $path, $body) {
  try {
    Write-Host "  Testing $method $path..." -ForegroundColor Gray
    if ($method -eq "GET") {
      return Invoke-RestMethod -Method GET -Uri ($ApiBase + $path) -TimeoutSec 5
    } else {
      return Invoke-RestMethod -Method POST -Uri ($ApiBase + $path) -Body ($body | ConvertTo-Json -Depth 10) -ContentType "application/json" -TimeoutSec 15
    }
  } catch { 
    Write-Host "  ⚠ API call failed: $_" -ForegroundColor Yellow
    return $null 
  }
}

$brief = @{ Topic="Demo Video"; Tone="Neutral"; Language="en"; Aspect="Widescreen16x9" }
$plan  = @{ TargetDuration=1.0; Pacing=3; Density=3; Style="Explainer" }
$ok = $false

Write-Host "Attempting full API pipeline..." -ForegroundColor Yellow
$health = Invoke-Api "GET" "/healthz" $null
if ($health -ne $null) {
  Write-Host "  ✓ API health check passed" -ForegroundColor Green
  $scriptRes = Invoke-Api "POST" "/script" @{ Brief=$brief; Plan=$plan }
  if ($scriptRes -ne $null) {
    Write-Host "  ✓ Script generation successful" -ForegroundColor Green
    $render = Invoke-Api "POST" "/render/quick" @{ Mode="Free"; Brief=$brief; Plan=$plan }
    if ($render -and $render.OutputPath -and (Test-Path $render.OutputPath)) {
      Copy-Item $render.OutputPath $out -Force
      Write-Host "  ✓ Render completed via API" -ForegroundColor Green
      $ok = $true
    }
  }
}

if (-not $ok) {
  Write-Host ""
  Write-Host "Falling back to ffmpeg-only demo render..." -ForegroundColor Yellow
  
  # Check if ffmpeg exists
  if (-not (Test-Path $FfmpegPath)) {
    $FfmpegPath = "ffmpeg" # Try system PATH
  }
  
  try {
    & $FfmpegPath -version 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
      throw "FFmpeg not available"
    }
    
    Write-Host "  Generating $Seconds second color bars demo..." -ForegroundColor Gray
    & $FfmpegPath -y -f lavfi -i "smptebars=size=1280x720:rate=30" `
      -f lavfi -i "sine=frequency=1000:sample_rate=48000:duration=$Seconds" `
      -c:v libx264 -t $Seconds -pix_fmt yuv420p -c:a aac -shortest $out 2>&1 | Out-Null
    
    if ($LASTEXITCODE -eq 0 -and (Test-Path $out)) {
      Write-Host "  ✓ Fallback render successful" -ForegroundColor Green
      $ok = $true
    }
  }
  catch {
    Write-Host "  ✗ FFmpeg fallback failed: $_" -ForegroundColor Red
  }
}

$smokeEndTime = Get-Date
$smokeDuration = $smokeEndTime - $smokeStartTime

Write-Host ""
if (Test-Path $out) {
  $fileSize = [math]::Round((Get-Item $out).Length / 1KB, 2)
  Write-Host "========================================" -ForegroundColor Green
  Write-Host " Smoke Test: PASS" -ForegroundColor Green
  Write-Host "========================================" -ForegroundColor Green
  Write-Host ""
  Write-Host "Output:   $out" -ForegroundColor White
  Write-Host "Size:     $fileSize KB" -ForegroundColor White
  Write-Host "Duration: $($smokeDuration.TotalSeconds.ToString("F2"))s" -ForegroundColor White
  Write-Host ""
  exit 0
} else {
  Write-Host "========================================" -ForegroundColor Red
  Write-Host " Smoke Test: FAIL" -ForegroundColor Red
  Write-Host "========================================" -ForegroundColor Red
  Write-Host ""
  Write-Host "Failed to generate demo video" -ForegroundColor Red
  Write-Host "Duration: $($smokeDuration.TotalSeconds.ToString("F2"))s" -ForegroundColor White
  Write-Host ""
  exit 1
}
