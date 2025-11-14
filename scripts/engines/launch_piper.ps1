# Launch Piper TTS
# For use with Aura Video Studio

param(
    [string]$InstallPath = "$env:LOCALAPPDATA\Aura\Tools\piper",
    [string]$Voice = "en_US-lessac-medium",
    [string]$TestText = "Hello, this is a test of Piper text to speech."
)

Write-Output "=== Piper TTS Launcher ===" -ForegroundColor Cyan
Write-Output ""

# Check if installation exists
$piperExe = Join-Path $InstallPath "piper.exe"
if (-not (Test-Path $piperExe)) {
    Write-Output "ERROR: Piper not found at $piperExe" -ForegroundColor Red
    Write-Output "Please install Piper first from Aura Download Center" -ForegroundColor Yellow
    exit 1
}

# Check voice model
$voiceModel = Join-Path $InstallPath "voices\$Voice.onnx"
if (-not (Test-Path $voiceModel)) {
    Write-Output "ERROR: Voice model not found: $voiceModel" -ForegroundColor Red
    Write-Output "Available voices:" -ForegroundColor Yellow
    Get-ChildItem (Join-Path $InstallPath "voices") -Filter "*.onnx" | ForEach-Object {
        Write-Output "  - $($_.BaseName)" -ForegroundColor Cyan
    }
    exit 1
}

Write-Output "Piper executable: $piperExe" -ForegroundColor Cyan
Write-Output "Voice model: $Voice" -ForegroundColor Cyan
Write-Output ""

# Run test synthesis
Write-Output "Running test synthesis..." -ForegroundColor Green
$outputFile = Join-Path $env:TEMP "piper_test.wav"

$TestText | & $piperExe --model $voiceModel --output_file $outputFile

if ($LASTEXITCODE -eq 0 -and (Test-Path $outputFile)) {
    Write-Output "✓ Synthesis successful!" -ForegroundColor Green
    Write-Output "Output saved to: $outputFile" -ForegroundColor Cyan
    Write-Output ""
    Write-Output "Playing audio..." -ForegroundColor Yellow

    # Try to play using Windows Media Player
    $player = New-Object System.Media.SoundPlayer
    $player.SoundLocation = $outputFile
    try {
        $player.PlaySync()
    } catch {
        Write-Output "Could not play audio automatically. Please open manually: $outputFile" -ForegroundColor Yellow
    }

    Write-Output ""
    Write-Output "Piper is ready for use with Aura!" -ForegroundColor Green
} else {
    Write-Output "✗ Synthesis failed" -ForegroundColor Red
    Write-Output "Check logs for errors" -ForegroundColor Yellow
    exit 1
}
