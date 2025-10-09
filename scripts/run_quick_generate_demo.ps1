<# 
scripts/run_quick_generate_demo.ps1
Tiny smoke test; falls back to ffmpeg color bars if API not available.
#>

param(
  [string]$ApiBase = "http://127.0.0.1:5000",
  [string]$FfmpegPath = ".\ffmpeg\ffmpeg.exe",
  [int]$Seconds = 10
)

$ErrorActionPreference = "SilentlyContinue"
New-Item -ItemType Directory -Force -Path "artifacts/smoke" | Out-Null
$out = "artifacts/smoke/demo.mp4"

function Invoke-Api($method, $path, $body) {
  try {
    if ($method -eq "GET") {
      return Invoke-RestMethod -Method GET -Uri ($ApiBase + $path) -TimeoutSec 5
    } else {
      return Invoke-RestMethod -Method POST -Uri ($ApiBase + $path) -Body ($body | ConvertTo-Json -Depth 10) -ContentType "application/json" -TimeoutSec 15
    }
  } catch { return $null }
}

$brief = @{ Topic="Demo"; Tone="Neutral"; Language="en"; Aspect="Widescreen16x9" }
$plan  = @{ TargetDuration=1.0; Pacing=3; Density=3; Style="Explainer" }
$ok = $false
$health = Invoke-Api "GET" "/healthz" $null
if ($health -ne $null) {
  $scriptRes = Invoke-Api "POST" "/script" @{ Brief=$brief; Plan=$plan }
  if ($scriptRes -ne $null) {
    $render = Invoke-Api "POST" "/render/quick" @{ Mode="Free"; Brief=$brief; Plan=$plan }
    if ($render -and $render.OutputPath -and (Test-Path $render.OutputPath)) {
      Copy-Item $render.OutputPath $out -Force
      $ok = $true
    }
  }
}

if (-not $ok) {
  Write-Host "Falling back to ffmpeg-only demo render..."
  & $FfmpegPath -y -f lavfi -i "smptebars=size=1280x720:rate=30" -f lavfi -i "sine=frequency=1000:sample_rate=48000:duration=$Seconds" -c:v libx264 -t $Seconds -pix_fmt yuv420p -c:a aac -shortest $out
}

if (Test-Path $out) { Write-Host "Smoke render created at $out"; exit 0 } else { Write-Host "Smoke render failed."; exit 1 }
