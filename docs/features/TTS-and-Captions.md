# TTS and Captions with DSP Chain

This document describes the TTS (Text-to-Speech) pipeline and caption generation features, including the DSP (Digital Signal Processing) chain for audio normalization.

## Overview

The TTS and Captions system provides:

1. **Multiple TTS Providers**: Windows SAPI (free), ElevenLabs, and PlayHT (with API keys)
2. **DSP Chain**: Professional audio processing pipeline for loudness normalization
3. **Caption Generation**: Export SRT and VTT subtitles with optional burn-in
4. **Timecode Validation**: Ensures captions align precisely with script line durations

## TTS Providers

### Windows SAPI (Free)

Windows Speech API provides free text-to-speech synthesis on Windows 10+.

```csharp
// Registered in DI container
var ttsProvider = serviceProvider.GetRequiredService<ITtsProvider>();

var lines = new List<ScriptLine>
{
    new ScriptLine(0, "Hello world", TimeSpan.Zero, TimeSpan.FromSeconds(2))
};

var voiceSpec = new VoiceSpec(
    VoiceName: "Microsoft David Desktop",
    Rate: 1.0,
    Pitch: 0.0,
    Pause: PauseStyle.Natural
);

string audioPath = await ttsProvider.SynthesizeAsync(lines, voiceSpec, cancellationToken);
```

### ElevenLabs (Pro)

High-quality TTS with natural voices. Requires API key.

```json
{
  "elevenLabsApiKey": "your-api-key-here"
}
```

### PlayHT (Pro)

Professional TTS service. Requires API key and user ID.

```json
{
  "playHTApiKey": "your-api-key-here",
  "playHTUserId": "your-user-id-here"
}
```

## DSP Chain

The DSP chain normalizes audio to broadcast standards with the following stages:

### Stage 1: High-Pass Filter (HPF)
- Removes low-frequency rumble below 80Hz
- Cleans up unwanted bass frequencies

### Stage 2: De-esser
- Reduces harsh sibilance in 6-8kHz range
- Makes speech easier to listen to

### Stage 3: Compressor
- Dynamic range compression (ratio 3:1, threshold -18dB)
- Evens out volume differences
- Attack: 20ms, Release: 250ms, Makeup: 6dB

### Stage 4: Limiter
- Prevents peaks from exceeding ceiling (-1 dBFS default)
- Attack: 5ms, Release: 50ms

### Stage 5: LUFS Normalization
- Target loudness: -14 LUFS (YouTube standard)
- Alternative targets: -16 LUFS (voice-only), -12 LUFS (music-forward)
- Peak ceiling: -1 dBFS to prevent clipping

### Usage

```csharp
var dspChain = serviceProvider.GetRequiredService<DspChain>();

// Build filter chain for YouTube
string filterChain = dspChain.BuildDspFilterChain(
    targetLufs: -14.0,
    peakCeiling: -1.0,
    enableHpf: true,
    enableDeEsser: true,
    enableCompressor: true,
    enableLimiter: true
);

// Use with FFmpeg
// ffmpeg -i input.wav -af "{filterChain}" output.aac
```

### Validation

```csharp
bool isValid = dspChain.ValidateLoudness(
    measuredLufs: -14.2,
    measuredPeak: -1.5,
    targetLufs: -14.0,
    peakCeiling: -1.0,
    out string? message,
    tolerance: 1.0  // Accept ±1 dB
);
```

### Recommended LUFS Targets

```csharp
double lufs = dspChain.GetRecommendedLufs("youtube");  // -14.0 LUFS
double lufs = dspChain.GetRecommendedLufs("voice");    // -16.0 LUFS
double lufs = dspChain.GetRecommendedLufs("music");    // -12.0 LUFS
```

## Caption Generation

### Generating Captions

```csharp
var captionBuilder = serviceProvider.GetRequiredService<CaptionBuilder>();

var lines = new List<ScriptLine>
{
    new ScriptLine(0, "Hello world", TimeSpan.Zero, TimeSpan.FromSeconds(2)),
    new ScriptLine(1, "This is a test", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3))
};

// Generate SRT (SubRip)
string srt = captionBuilder.GenerateSrt(lines);

// Generate VTT (WebVTT)
string vtt = captionBuilder.GenerateVtt(lines);
```

### SRT Format

```srt
1
00:00:00,000 --> 00:00:02,000
Hello world

2
00:00:02,000 --> 00:00:05,000
This is a test
```

### VTT Format

```vtt
WEBVTT

00:00:00.000 --> 00:00:02.000
Hello world

00:00:02.000 --> 00:00:05.000
This is a test
```

### Burn-in Captions

For hardcoding captions into video:

```csharp
var style = new CaptionRenderStyle(
    FontName: "Arial",
    FontSize: 24,
    PrimaryColor: "FFFFFF",    // White text
    OutlineColor: "000000",    // Black outline
    OutlineWidth: 2,
    BorderStyle: 3,            // Opaque box
    Alignment: 2               // Bottom center
);

string filter = captionBuilder.BuildBurnInFilter("subtitles.srt", style);

// Use with FFmpeg
// ffmpeg -i video.mp4 -vf "{filter}" output.mp4
```

### Timecode Validation

Ensure captions align correctly:

```csharp
bool isValid = captionBuilder.ValidateTimecodes(lines, out string? message);

if (!isValid)
{
    Console.WriteLine($"Validation failed: {message}");
    // Handle overlap or negative duration errors
}
```

## API Endpoints

### Generate Captions

**POST** `/api/v1/captions/generate`

```json
{
  "lines": [
    {
      "sceneIndex": 0,
      "text": "Hello world",
      "startSeconds": 0,
      "durationSeconds": 2
    }
  ],
  "format": "srt",
  "outputPath": "/path/to/captions.srt"
}
```

Response:
```json
{
  "success": true,
  "captions": "1\n00:00:00,000 --> 00:00:02,000\nHello world\n\n",
  "filePath": "/path/to/captions.srt"
}
```

## UI Component

The `CaptionsPanel` component provides a user interface for:
- Selecting caption format (SRT or VTT)
- Enabling burn-in with style customization
- Previewing generated captions
- Exporting caption files

```tsx
<CaptionsPanel
  scriptLines={scriptLines}
  onGenerate={(format, burnIn, style) => {
    // Handle caption generation
  }}
  onExport={(format) => {
    // Handle export
  }}
/>
```

## Integration Example

Complete workflow from script to captioned video:

```csharp
// 1. Generate TTS audio
var ttsProvider = serviceProvider.GetRequiredService<ITtsProvider>();
string audioPath = await ttsProvider.SynthesizeAsync(lines, voiceSpec, ct);

// 2. Apply DSP chain
var dspChain = serviceProvider.GetRequiredService<DspChain>();
string dspFilter = dspChain.BuildDspFilterChain(targetLufs: -14.0);
// Process with FFmpeg: ffmpeg -i {audioPath} -af "{dspFilter}" processed.aac

// 3. Generate captions
var captionBuilder = serviceProvider.GetRequiredService<CaptionBuilder>();
string srt = captionBuilder.GenerateSrt(lines);
await File.WriteAllTextAsync("captions.srt", srt);

// 4. Optionally burn-in captions
var style = new CaptionRenderStyle();
string burnInFilter = captionBuilder.BuildBurnInFilter("captions.srt", style);
// Render with FFmpeg: ffmpeg -i video.mp4 -vf "{burnInFilter}" output.mp4
```

## Testing

Run tests to verify functionality:

```bash
# DSP Chain tests (22 tests)
dotnet test --filter "FullyQualifiedName~DspChainTests"

# Caption Builder tests (15 tests)
dotnet test --filter "FullyQualifiedName~CaptionBuilderTests"

# Audio Processor tests (21 tests)
dotnet test --filter "FullyQualifiedName~AudioProcessorTests"

# Caption Integration tests (8 tests)
dotnet test --filter "FullyQualifiedName~CaptionsIntegration"
```

## Best Practices

1. **Audio Normalization**: Always apply DSP chain to ensure consistent loudness
2. **Validation**: Validate timecodes before generating captions to catch overlaps
3. **Format Selection**: Use SRT for broad compatibility, VTT for web players
4. **Peak Ceiling**: Keep at -1 dBFS to prevent clipping during playback
5. **LUFS Target**: Use -14 LUFS for YouTube, -16 for podcasts, -12 for music videos

## Troubleshooting

### Audio is too quiet/loud
Check LUFS target and validate with:
```csharp
dspChain.ValidateLoudness(measuredLufs, measuredPeak, targetLufs, peakCeiling, out var msg);
```

### Captions are out of sync
Validate timecodes:
```csharp
captionBuilder.ValidateTimecodes(lines, out var msg);
```

### Windows TTS not working
- Ensure Windows 10 or later
- Check available voices:
```csharp
var voices = await ttsProvider.GetAvailableVoicesAsync();
```

## References

- [EBS R128: Loudness Normalization](https://tech.ebu.ch/docs/r/r128.pdf)
- [SubRip (SRT) Format](https://en.wikipedia.org/wiki/SubRip)
- [WebVTT Specification](https://www.w3.org/TR/webvtt1/)
- [FFmpeg Audio Filters](https://ffmpeg.org/ffmpeg-filters.html#Audio-Filters)
