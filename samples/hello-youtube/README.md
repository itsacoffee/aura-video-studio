# Hello YouTube Sample

A simple "Hello World" video sample that uses only local and free providers.

## Overview

This sample creates a short demonstration video that:
- Uses local TTS for narration (Windows SAPI or system TTS)
- Uses CC0 public domain b-roll footage
- Includes CC0 background music
- Generates subtitles automatically
- Renders to MP4 format

**No API keys required!** This sample works completely offline with local providers.

## Sample Output

- **Duration:** ~30 seconds
- **Resolution:** 1080p (1920x1080)
- **Audio:** Narration + background music with ducking
- **Subtitles:** Embedded SRT captions
- **Format:** MP4 (H.264 video, AAC audio)

## Script Content

```
Welcome to Aura Video Studio!

This is a sample video created entirely with local and free providers.

No API keys required. No cloud services needed.

Start creating your own videos today!
```

## Assets

All assets are CC0 (Creative Commons Zero) - Public Domain:

### B-Roll Footage
- `broll-1.mp4` - Generic technology/workspace footage
- `broll-2.mp4` - Creative workspace footage
- `broll-3.mp4` - Computer screen footage

Source: Pexels/Pixabay CC0 collections

### Background Music
- `background.mp3` - Upbeat inspiring instrumental track
- Duration: 60 seconds (looped)
- Style: Modern, uplifting

Source: Free Music Archive CC0 collection

## Usage

### Option 1: Use the Web UI Button

1. Start Aura Video Studio
2. Navigate to **Samples** or **Create** page
3. Click **"Create Sample Video"** button
4. Wait for processing (30-60 seconds)
5. Download the rendered MP4

### Option 2: Use the API

```bash
# Create sample video
curl -X POST http://localhost:5005/api/samples/hello-youtube/create

# Check progress
curl http://localhost:5005/api/samples/hello-youtube/status

# Download when ready
curl -O http://localhost:5005/api/samples/hello-youtube/download/output.mp4
```

### Option 3: Manual Creation

```bash
# Use the script.json as input to the video pipeline
curl -X POST http://localhost:5005/api/quick/generate \
  -H "Content-Type: application/json" \
  -d @samples/hello-youtube/script.json
```

## Files

```
samples/hello-youtube/
├── README.md                 # This file
├── script.json              # Sample script definition
├── assets/
│   ├── broll/
│   │   ├── broll-1.mp4     # Technology footage
│   │   ├── broll-2.mp4     # Workspace footage
│   │   └── broll-3.mp4     # Screen footage
│   ├── music/
│   │   └── background.mp3   # Background music
│   └── LICENSES.md          # Asset licenses and attributions
└── output/
    └── hello-youtube.mp4    # Rendered output (gitignored)
```

## Customization

You can customize the sample by editing `script.json`:

```json
{
  "topic": "Your Custom Topic",
  "targetDurationMinutes": 0.5,
  "tone": "Informative",
  "useLocalOnly": true,
  "scenes": [
    {
      "heading": "Scene 1",
      "script": "Your custom narration here",
      "duration": 5.0
    }
  ]
}
```

## Troubleshooting

### No audio in output
- Verify TTS provider is available: `curl http://localhost:5005/api/providers/capabilities`
- Check logs: `tail -f AuraData/logs/aura-api-*.log`

### FFmpeg not found
- Install FFmpeg: See [PORTABLE_FIRST_RUN.md](../../PORTABLE_FIRST_RUN.md#2-install-ffmpeg)
- Verify: `curl http://localhost:5005/api/health/ready`

### Rendering fails
- Check available disk space (need ~100MB)
- Verify write permissions in output directory
- Review error logs in `AuraData/logs/`

## Next Steps

After successfully creating this sample:
1. Try modifying the script in `script.json`
2. Add your own CC0 assets to the `assets/` folder
3. Experiment with different tones and durations
4. Create your own video from scratch using the Web UI

## License

All assets in this sample are CC0 (Public Domain). You can use, modify, and distribute them freely without attribution.

See [assets/LICENSES.md](./assets/LICENSES.md) for specific asset attributions.
