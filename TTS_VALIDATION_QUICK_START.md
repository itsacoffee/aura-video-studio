# TTS Provider Validation - Quick Start Guide

**PR-CORE-002: TTS Provider Integration Validation**

This guide helps you quickly validate TTS provider integrations on Windows.

## Prerequisites

### Required
- Windows 10 (Build 19041+) or Windows 11
- .NET 8.0 SDK
- Git

### Optional (for full validation)
- FFmpeg (for format conversion)
- ElevenLabs API key (for cloud TTS testing)
- PlayHT credentials (for cloud TTS testing)
- Piper TTS (for offline TTS testing)

## Quick Start (5 minutes)

### 1. Install Dependencies

```powershell
# Install .NET 8.0 SDK (if not installed)
winget install Microsoft.DotNet.SDK.8

# Install FFmpeg (optional, for format conversion tests)
winget install Gyan.FFmpeg

# Refresh PATH
refreshenv
```

### 2. Run Basic Validation

```powershell
# Navigate to repository root
cd path\to\aura-video-studio

# Run Windows SAPI tests (no API keys needed)
.\Run-TTS-Validation-Tests.ps1
```

This will test:
- ✅ Windows SAPI TTS (native Windows)
- ✅ Audio file generation
- ✅ Audio storage and cleanup
- ✅ WAV file validation

## Full Validation (15 minutes)

### 1. Get API Keys (Optional)

**ElevenLabs** (Premium voices):
- Sign up: https://elevenlabs.io
- Get API key from: https://elevenlabs.io/app/settings/api-keys
- Free tier: 10,000 characters/month

**PlayHT** (Premium voices):
- Sign up: https://play.ht
- Get credentials from: https://play.ht/app/api-keys
- Requires both API key AND User ID

### 2. Install Piper (Optional)

```powershell
# Download Piper
Invoke-WebRequest -Uri "https://github.com/rhasspy/piper/releases/latest/download/piper_windows_amd64.zip" -OutFile "$env:TEMP\piper.zip"

# Extract
Expand-Archive -Path "$env:TEMP\piper.zip" -DestinationPath "C:\Program Files\Piper" -Force

# Download voice model (example: US English)
Invoke-WebRequest -Uri "https://github.com/rhasspy/piper/releases/download/v1.2.0/en_US-lessac-medium.onnx" -OutFile "C:\Program Files\Piper\en_US-lessac-medium.onnx"
```

### 3. Set Environment Variables

```powershell
# ElevenLabs
$env:ELEVENLABS_API_KEY = "sk-your-api-key-here"

# PlayHT
$env:PLAYHT_API_KEY = "your-api-key-here"
$env:PLAYHT_USER_ID = "your-user-id-here"

# Piper
$env:PIPER_EXECUTABLE_PATH = "C:\Program Files\Piper\piper.exe"
$env:PIPER_MODEL_PATH = "C:\Program Files\Piper\en_US-lessac-medium.onnx"
```

### 4. Run Full Validation

```powershell
# Run all tests
.\Run-TTS-Validation-Tests.ps1 -IncludeCloudProviders -IncludePiper -VerboseOutput
```

## Test Execution Matrix

| Test Suite | Duration | Prerequisites | What It Tests |
|------------|----------|---------------|---------------|
| **Windows SAPI** | ~10s | Windows 10 19041+ | Native TTS, SSML, voice enumeration |
| **ElevenLabs** | ~30s | API key | Cloud TTS, voice selection, caching |
| **PlayHT** | ~45s | API key + User ID | Cloud TTS, job polling, error handling |
| **Piper** | ~15s | Piper + model | Offline TTS, WAV merging, fallback |
| **Audio Storage** | ~5s | None | File operations, validation, cleanup |
| **Format Convert** | ~10s | FFmpeg | MP3→WAV, normalization, detection |

**Total Time**: ~2 minutes (basic) to ~5 minutes (full)

## Understanding Test Results

### Success Output

```
✓ All tests PASSED!
Duration: 45.2 seconds
Results file: TestResults_TTS_20251111_123456.trx

Provider Status:
  Windows SAPI:  Tested ✓
  Audio Storage: Tested ✓
  Format Convert:Tested ✓
  ElevenLabs:    Tested ✓
  PlayHT:        Tested ✓
  Piper:         Tested ✓
```

### Failure Output

```
✗ Some tests FAILED
Exit code: 1

Common Issues:
- Missing API key
- Invalid credentials
- Piper not found
- FFmpeg missing
```

## Troubleshooting

### Windows SAPI Tests Fail

**Problem**: "PlatformNotSupportedException"

**Solution**: Update Windows to build 19041 or later
```powershell
# Check your build
winver

# Update Windows
Start-Process ms-settings:windowsupdate
```

### ElevenLabs Tests Fail

**Problem**: "API key is invalid"

**Solutions**:
1. Verify API key: https://elevenlabs.io/app/settings/api-keys
2. Check key format: Should start with `sk-`
3. Ensure key is active (not revoked)

**Problem**: "Rate limit exceeded"

**Solution**: Wait 1 minute or upgrade plan

### PlayHT Tests Fail

**Problem**: "Credentials incomplete"

**Solution**: Both API key AND User ID required
```powershell
# Find your User ID at: https://play.ht/app/api-keys
$env:PLAYHT_USER_ID = "your-user-id-here"
```

### Piper Tests Fail

**Problem**: "Piper executable not found"

**Solution**: Install Piper (see installation steps above)

**Problem**: "Voice model not found"

**Solution**: Download voice model
```powershell
# List available models
Start-Process "https://github.com/rhasspy/piper/releases"

# Download and configure path
$env:PIPER_MODEL_PATH = "path\to\model.onnx"
```

### FFmpeg Tests Fail

**Problem**: "FFmpeg not found"

**Solution**: Install FFmpeg
```powershell
# Install via WinGet
winget install Gyan.FFmpeg

# Or manually from: https://ffmpeg.org/download.html

# Verify installation
ffmpeg -version
```

## Manual Testing

If automated tests fail, you can manually test each provider:

### Windows SAPI Manual Test

```powershell
# Check available voices
Get-WmiObject -Class Win32_SpeechSynthesizerVoice | Select-Object -ExpandProperty VoiceName

# Test synthesis (requires PowerShell 5.1+)
Add-Type -AssemblyName System.Speech
$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
$synth.Speak("Hello from Windows TTS")
```

### FFmpeg Manual Test

```powershell
# Convert MP3 to WAV
ffmpeg -i input.mp3 -ar 44100 -ac 2 -sample_fmt s16 output.wav

# Get audio info
ffprobe -v error -show_entries format=duration:stream=sample_rate,channels input.mp3
```

### Piper Manual Test

```powershell
# Synthesize speech
echo "Hello from Piper TTS" | & "C:\Program Files\Piper\piper.exe" --model "C:\Program Files\Piper\en_US-lessac-medium.onnx" --output_file "test.wav"

# Play result
Start-Process "test.wav"
```

## Integration with CI/CD

For automated builds, skip tests requiring manual setup:

```yaml
# .github/workflows/test-windows.yml
- name: Run TTS Validation Tests
  run: |
    # Run only Windows SAPI tests (no API keys needed)
    .\Run-TTS-Validation-Tests.ps1
  env:
    # Optionally set secrets for full validation
    ELEVENLABS_API_KEY: ${{ secrets.ELEVENLABS_API_KEY }}
    PLAYHT_API_KEY: ${{ secrets.PLAYHT_API_KEY }}
    PLAYHT_USER_ID: ${{ secrets.PLAYHT_USER_ID }}
```

## Next Steps

After validation:

1. **Review Results**: Check `TTS_PROVIDER_VALIDATION_REPORT.md`
2. **Check Test Output**: Open `.trx` file in Visual Studio
3. **Fix Issues**: Address any failed tests
4. **Update Documentation**: Document any platform-specific findings
5. **Commit Changes**: Include test results in PR

## Support

- **Documentation**: See `TTS_PROVIDER_VALIDATION_REPORT.md`
- **Issues**: Create issue with tag `tts` and `pr-core-002`
- **Logs**: Check `TestResults_TTS_*.trx` for detailed failure info

## Quick Reference

```powershell
# Basic validation (no API keys)
.\Run-TTS-Validation-Tests.ps1

# Full validation
.\Run-TTS-Validation-Tests.ps1 -IncludeCloudProviders -IncludePiper

# Verbose output
.\Run-TTS-Validation-Tests.ps1 -VerboseOutput

# Run specific provider tests
dotnet test --filter "FullyQualifiedName~WindowsTtsProvider"
dotnet test --filter "FullyQualifiedName~ElevenLabsProvider"
dotnet test --filter "FullyQualifiedName~PlayHTProvider"
dotnet test --filter "FullyQualifiedName~PiperProvider"

# Run audio tests only
dotnet test --filter "FullyQualifiedName~AudioFile"
dotnet test --filter "FullyQualifiedName~AudioFormat"
```

---

**Time Investment**: 5-15 minutes  
**Difficulty**: Easy  
**Prerequisites**: Windows 10+ with .NET 8.0  
**Optional**: API keys for cloud providers
