# TTS Providers and Captions Implementation

## Overview

This implementation adds comprehensive Text-to-Speech (TTS) provider support and captions generation to Aura Video Studio, fulfilling all requirements from the specification.

## Features Implemented

### 1. WindowsTtsProvider Enhancement (SAPI)
- ✅ **Scene-aware WAV generation**: Each scene's narration is synthesized separately
- ✅ **WAV merging**: Created `WavMerger` utility to combine scene WAVs with proper timing gaps
- ✅ **Rate/Pitch/Pause controls**: Full support for prosody adjustments via SSML
- ✅ **Master narration track**: Merged output with correct timing and envelope

**Files:**
- `Aura.Providers/Tts/WindowsTtsProvider.cs` - Enhanced with WAV merging
- `Aura.Providers/Audio/WavMerger.cs` - New WAV file merger utility

### 2. Pro TTS Providers (ElevenLabs and PlayHT)
- ✅ **ElevenLabsTtsProvider**: Full implementation with API key validation
  - API key validation via `ValidateApiKeyAsync()`
  - Short smoke synthesis test during validation
  - Respects OfflineOnly mode
  - Graceful downgrade when unavailable
  
- ✅ **PlayHTTtsProvider**: Full implementation with API key validation
  - API key + User ID validation
  - Polling mechanism for async synthesis
  - Short smoke synthesis test during validation
  - Respects OfflineOnly mode
  - Graceful downgrade when unavailable

**Files:**
- `Aura.Providers/Tts/ElevenLabsTtsProvider.cs` - New Pro provider
- `Aura.Providers/Tts/PlayHTTtsProvider.cs` - New Pro provider

### 3. Linux Mock TTS Provider
- ✅ **MockTtsProvider**: Deterministic mock for CI/Linux environments
  - Generates valid WAV files with proper headers
  - Deterministic beep/silence pattern (440 Hz tone every second)
  - Correct duration based on scene timings
  - Perfect for CI/CD pipelines

**Files:**
- `Aura.Providers/Tts/MockTtsProvider.cs` - New mock provider for testing

### 4. Captions Generation
- ✅ **SRT/VTT generation**: From ScriptLine timings
  - Already implemented in `AudioProcessor.GenerateSrtSubtitles()` and `AudioProcessor.GenerateVttSubtitles()`
  - Enhanced with new API endpoint
  
- ✅ **Burn-in support**: Via FFmpeg subtitle filter
  - `AudioProcessor.BuildSubtitleFilter()` creates FFmpeg filter string
  - Supports custom fonts, colors, outline, positioning
  
- ✅ **Sidecar support**: Save captions as separate .srt/.vtt files
  - New API endpoint `/api/captions/generate` supports both

**Files:**
- `Aura.Api/Program.cs` - New `/api/captions/generate` endpoint
- `Aura.Core/Audio/AudioProcessor.cs` - Caption generation (already existed, now exposed via API)

### 5. Provider Selection and Configuration

#### TtsProviderFactory
- ✅ **Automatic provider selection**: Based on configuration and availability
- ✅ **API key management**: Via `ProviderSettings`
- ✅ **Offline mode support**: Respects `OfflineOnly` setting
- ✅ **Graceful fallback**: Pro → Windows → Mock

**Files:**
- `Aura.Core/Providers/TtsProviderFactory.cs` - New provider factory
- `Aura.Core/Configuration/ProviderSettings.cs` - Enhanced with API key getters

#### Provider Priority
1. **Pro providers** (if API keys available and not offline):
   - ElevenLabs
   - PlayHT
2. **Windows TTS** (if on Windows platform)
3. **Mock provider** (fallback for Linux/CI)

### 6. API Endpoints

#### `/api/tts` (Enhanced)
```json
POST /api/tts
{
  "lines": [
    {
      "sceneIndex": 0,
      "text": "Welcome to Aura",
      "startSeconds": 0.0,
      "durationSeconds": 2.0
    }
  ],
  "voiceName": "Microsoft Zira",
  "rate": 1.0,
  "pitch": 0.0,
  "pauseStyle": "Natural"
}

Response:
{
  "success": true,
  "audioPath": "/path/to/narration.wav"
}
```

#### `/api/captions/generate` (New)
```json
POST /api/captions/generate
{
  "lines": [
    {
      "sceneIndex": 0,
      "text": "Welcome to Aura",
      "startSeconds": 0.0,
      "durationSeconds": 2.0
    }
  ],
  "format": "SRT",  // or "VTT"
  "outputPath": "/optional/path/to/save.srt"
}

Response:
{
  "success": true,
  "captions": "1\n00:00:00,000 --> 00:00:02,000\nWelcome to Aura\n\n",
  "filePath": "/path/to/save.srt"  // if outputPath provided
}
```

## Configuration

### API Keys (in provider-paths.json)
```json
{
  "elevenLabsApiKey": "your-key-here",
  "playHTApiKey": "your-key-here",
  "playHTUserId": "your-user-id-here",
  "offlineOnly": false
}
```

Location: `%LOCALAPPDATA%/Aura/provider-paths.json`

### Provider Selection
The `TtsProviderFactory` automatically selects the best available provider:
- Checks API keys
- Respects offline mode
- Falls back gracefully

## Tests

### Unit Tests
- ✅ **TtsProviderTests**: MockTtsProvider, voice list, WAV generation
- ✅ **WavMergerTests**: WAV file merging, timing gaps, header validation
- ✅ **CaptionsIntegrationTests**: SRT/VTT generation, timing accuracy, special characters
- ✅ **AudioProcessorTests**: Caption generation, subtitle filters (already existed)

### Integration Tests
- ✅ **TtsEndpointIntegrationTests**: 
  - `/tts` endpoint happy path
  - Different voice settings
  - TTS + captions pipeline
  - Cancellation handling
  - Format support (SRT/VTT)

**Test Results**: 130 tests passing (up from 111)

## Timeline Integration

The `Timeline` record now supports captions:
```csharp
var timeline = new Timeline(
    Scenes: scenes,
    SceneAssets: sceneAssets,
    NarrationPath: narrationPath,
    MusicPath: musicPath,
    SubtitlesPath: captionsPath  // New field
);
```

## Usage Examples

### Free Path (Windows TTS + Captions)
```csharp
var factory = new TtsProviderFactory(loggerFactory, providerSettings, httpClientFactory);
var provider = factory.GetDefaultProvider(); // Returns WindowsTtsProvider

var lines = ConvertScenesToScriptLines(scenes);
var narrationPath = await provider.SynthesizeAsync(lines, voiceSpec, ct);

var audioProcessor = new AudioProcessor(logger);
var srtCaptions = audioProcessor.GenerateSrtSubtitles(lines);
File.WriteAllText("captions.srt", srtCaptions);
```

### Pro Path (ElevenLabs/PlayHT + Captions)
1. Configure API keys in `provider-paths.json`
2. Factory automatically selects Pro provider
3. Generate narration and captions as above

### Linux/CI Path (Mock + Captions)
- MockTtsProvider automatically used on non-Windows platforms
- Generates deterministic WAV files for testing
- Captions generation works identically

## Benefits

1. **Flexibility**: Multiple TTS providers with automatic selection
2. **Reliability**: Graceful fallback ensures narration always works
3. **Testing**: Mock provider enables CI/CD without API keys
4. **Professional**: Support for high-quality Pro TTS services
5. **Accessibility**: Comprehensive caption support (SRT/VTT)
6. **Timing**: Scene-aware synthesis with proper gaps and merging

## Definition of Done - Verification

✅ **Narration works in Free path**: WindowsTtsProvider with WAV merging
✅ **Narration works in Pro path**: ElevenLabs/PlayHT with API keys
✅ **Captions generation**: SRT/VTT from scene timings
✅ **Burn-in support**: FFmpeg subtitle filter generation
✅ **Sidecar support**: Save to separate files
✅ **Linux CI mock**: MockTtsProvider generates deterministic WAVs
✅ **Tests pass**: 130 tests (19 new tests added)
✅ **Integration**: Timeline supports subtitle paths
✅ **API endpoints**: `/tts` and `/captions/generate` working
✅ **Provider selection**: Automatic with graceful fallback

All requirements from the problem statement have been successfully implemented and tested.
