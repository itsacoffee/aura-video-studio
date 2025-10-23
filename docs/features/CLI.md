# Aura CLI - Headless Video Generation

The Aura CLI provides headless, automated video generation capabilities for CI/CD pipelines, batch processing, and scripted workflows.

## Features

- **Cross-Platform**: Works on Windows and Linux
- **Headless Operation**: No GUI required
- **Hardware Acceleration**: Automatic NVENC (NVIDIA) detection and usage
- **Offline Support**: Free-only mode with no API keys required
- **Profile Modes**: Free-Only, Balanced, and Pro-Max provider configurations
- **Full Pipeline**: Script generation, composition, rendering, and caption generation
- **Exit Codes**: Standardized error codes for automation (E200, E310, E320, E330, E340)

## Installation

### Prerequisites

- .NET 8 Runtime (Linux) or .NET 8 SDK (for building from source)
- FFmpeg (required for video rendering)
- Optional: NVIDIA GPU with drivers for hardware acceleration

### From Binary Release

**Windows:**
```powershell
# Download and extract the release
# Add to PATH or run directly
.\aura-cli.exe help
```

**Linux:**
```bash
# Download and extract the release
chmod +x aura-cli
./aura-cli help
```

### From Source

```bash
cd Aura.Cli
dotnet build
dotnet run -- help
```

## Commands

### `preflight`

Check system requirements, dependencies, and hardware capabilities.

**Usage:**
```bash
aura-cli preflight [options]
```

**Options:**
- `-v, --verbose` - Show detailed hardware information

**Exit Codes:**
- `0` - System ready
- `200` - Preflight checks failed

**Example:**
```bash
$ aura-cli preflight -v
╔══════════════════════════════════════════════════════════╗
║         AURA CLI - System Preflight Check               ║
╚══════════════════════════════════════════════════════════╝

Checking hardware...
  CPU: 8 logical cores (4 physical)
  RAM: 16 GB
  GPU: NVIDIA GeForce RTX 3060
  VRAM: 12 GB
  Hardware Tier: A
  NVENC Available: True
  Stable Diffusion Available: True

✓ Hardware detected: Tier A

Checking dependencies...
✓ FFmpeg found

Provider Status:
  Free Providers: ✓ Available
  Pro Providers: ⚠ API keys not configured

✓ Preflight complete - System ready
Exit Code: 0
```

---

### `script`

Generate video script from brief and plan specifications.

**Usage:**
```bash
aura-cli script [options]
```

**Options:**
- `-b, --brief <file>` - Brief JSON file (required)
- `-p, --plan <file>` - Plan JSON file (required)
- `-o, --output <file>` - Output script file (default: script.txt)
- `-v, --verbose` - Enable verbose output
- `--dry-run` - Validate inputs without generating

**Exit Codes:**
- `0` - Script generated successfully
- `100` - Invalid arguments
- `310` - Script generation failed

**Example:**
```bash
$ aura-cli script -b brief.json -p plan.json -o output.txt

╔══════════════════════════════════════════════════════════╗
║         AURA CLI - Script Generation                     ║
╚══════════════════════════════════════════════════════════╝

[1/3] Reading inputs...
      ✓ Brief loaded: Introduction to Machine Learning
      ✓ Plan loaded: 3.0 minutes, Conversational pacing

[2/3] Generating script...
      Provider: Rule-based LLM (offline)
      ✓ Script generated (1247 characters)

[3/3] Saving output...
      ✓ Saved to: output.txt

✓ Script generation complete!
```

**Input Format - brief.json:**
```json
{
  "topic": "Introduction to Machine Learning",
  "audience": "Beginners",
  "goal": "Understand ML basics",
  "tone": "Educational",
  "language": "en-US",
  "aspect": "Widescreen16x9"
}
```

**Input Format - plan.json:**
```json
{
  "targetDuration": "00:03:00",
  "pacing": "Conversational",
  "density": "Balanced",
  "style": "Educational"
}
```

---

### `compose`

Create a composition plan from timeline and assets.

**Usage:**
```bash
aura-cli compose [options]
```

**Options:**
- `-i, --input <file>` - Timeline JSON file (required)
- `-o, --output <file>` - Output composition plan (default: compose-plan.json)
- `-v, --verbose` - Enable verbose output

**Exit Codes:**
- `0` - Composition plan created
- `100` - Invalid arguments

**Example:**
```bash
$ aura-cli compose -i timeline.json -o plan.json

╔══════════════════════════════════════════════════════════╗
║         AURA CLI - Compose Timeline Preview              ║
╚══════════════════════════════════════════════════════════╝

[1/3] Reading input from: timeline.json
      ✓ Input JSON is valid

[2/3] Creating composition plan...
      ✓ Composition plan created
      Resolution: 1920x1080
      Frame Rate: 30 fps
      Codec: H.264

[3/3] Saving composition plan...
      ✓ Plan saved to: plan.json

✓ Composition plan ready!
```

---

### `render`

Execute FFmpeg rendering to produce final video with captions.

**Usage:**
```bash
aura-cli render [options]
```

**Options:**
- `-r, --render-spec <file>` - Render specification JSON (required)
- `-o, --output <file>` - Output video file (default: ./output/demo.mp4)
- `-v, --verbose` - Show FFmpeg output
- `--dry-run` - Validate without rendering

**Exit Codes:**
- `0` - Rendering successful
- `100` - Invalid arguments
- `340` - Rendering failed

**Example:**
```bash
$ aura-cli render -r plan.json -o video.mp4

╔══════════════════════════════════════════════════════════╗
║           AURA CLI - Video Rendering                     ║
╚══════════════════════════════════════════════════════════╝

[1/5] Detecting hardware capabilities...
      ✓ Encoder: NVENC (hardware)

[2/5] Reading render specification...
      ✓ Render spec loaded

[3/5] Preparing output directory...
      ✓ Output: video.mp4

[4/5] Checking FFmpeg availability...
      ✓ FFmpeg available

[5/5] Rendering video...
      ✓ Video rendered successfully
      ✓ Captions saved: video.srt

✓ Rendering complete!

Output files:
  - Video: video.mp4
  - Captions: video.srt

Encoder used: NVENC (hardware)
```

---

### `quick`

End-to-end video generation in one command. Fastest way to create videos.

**Usage:**
```bash
aura-cli quick [options]
```

**Options:**
- `-t, --topic <text>` - Video topic (required)
- `-d, --duration <mins>` - Target duration in minutes (default: 3)
- `-o, --output <dir>` - Output directory (default: ./output)
- `-p, --profile <name>` - Provider profile: Free-Only, Balanced, Pro-Max
- `--offline` - Force offline mode (no API calls)
- `-v, --verbose` - Enable verbose output
- `--dry-run` - Validate without generating files

**Exit Codes:**
- `0` - Generation successful
- `100` - Invalid arguments
- `310` - Script generation failed
- `340` - Rendering failed

**Examples:**

Basic usage:
```bash
aura-cli quick -t "Machine Learning Basics"
```

With custom duration and output:
```bash
aura-cli quick -t "Coffee Brewing Guide" -d 5 -o ./videos
```

Offline mode (Free-only providers):
```bash
aura-cli quick -t "Python Tutorial" --profile Free-Only --offline
```

Dry run (validation only):
```bash
aura-cli quick -t "Test Topic" --dry-run -v
```

**Output:**
```bash
$ aura-cli quick -t "Introduction to AI"

╔══════════════════════════════════════════════════════════╗
║           AURA CLI - Quick Video Generation              ║
╚══════════════════════════════════════════════════════════╝

[1/5] Detecting hardware...
      ✓ Tier B (8 cores, 16 GB RAM)

[2/5] Creating brief...
      ✓ Brief created

[3/5] Creating plan...
      ✓ Plan created

[4/5] Generating script...
      ✓ Script generated (1523 chars)

[5/5] Rendering video...
      Resolution: 1920x1080
      Encoder: x264 (software)
      Audio: AAC 192 kbps
      ✓ Video rendered successfully
      ✓ Captions generated

═══════════════════════════════════════════════════════════
✓ Quick generation complete!

Generated files:
  - ./output/brief.json
  - ./output/plan.json
  - ./output/script.txt
  - ./output/demo.mp4
  - ./output/demo.srt
```

---

## Provider Profiles

Aura CLI supports three provider profiles that control which services are used for each stage:

### Free-Only
Uses only free, offline providers:
- **Script**: Rule-based templates
- **Voice**: Windows TTS (Windows only)
- **Visuals**: Stock images (Pexels/Pixabay)
- **Render**: FFmpeg (local)

### Balanced (Default)
Prefers Pro providers but falls back to Free:
- **Script**: OpenAI → Rule-based
- **Voice**: ElevenLabs → Windows TTS
- **Visuals**: Local Stable Diffusion → Stock
- **Render**: FFmpeg with hardware acceleration

### Pro-Max
Requires API keys for all stages:
- **Script**: OpenAI/Azure/Gemini
- **Voice**: ElevenLabs/PlayHT
- **Visuals**: Stability AI or Local SD
- **Render**: FFmpeg with NVENC/hardware

**Usage:**
```bash
# Use Free-Only profile
aura-cli quick -t "Topic" --profile Free-Only

# Use Balanced profile (default)
aura-cli quick -t "Topic" --profile Balanced

# Use Pro-Max profile (requires API keys)
aura-cli quick -t "Topic" --profile Pro-Max
```

---

## Hardware Acceleration

The CLI automatically detects and uses hardware acceleration when available:

### NVIDIA GPUs
- **NVENC**: H.264/HEVC hardware encoding (2-5x faster than CPU)
- **Stable Diffusion**: Local image generation (6+ GB VRAM required)

### Intel QuickSync
- **QSV**: Hardware encoding on Intel CPUs with integrated graphics

### AMD GPUs
- **AMF**: Hardware encoding on AMD GPUs (Windows only)

### CPU-Only
Falls back to software encoding (x264) when no hardware acceleration is available.

**Check your hardware:**
```bash
aura-cli preflight -v
```

---

## Exit Codes

The CLI uses standardized exit codes for automation:

| Code | Name | Description |
|------|------|-------------|
| `0` | Success | Operation completed successfully |
| `100` | InvalidArguments | Invalid command-line arguments |
| `200` | PreflightFail | System requirements not met |
| `310` | ScriptFail | Script generation failed |
| `320` | VisualsFail | Visual asset acquisition failed |
| `330` | TtsFail | Text-to-speech synthesis failed |
| `340` | RenderFail | Video rendering failed |
| `500` | UnexpectedError | Unexpected error occurred |

**Usage in scripts:**
```bash
#!/bin/bash
aura-cli quick -t "My Video"
if [ $? -eq 0 ]; then
  echo "Success!"
else
  echo "Failed with exit code: $?"
fi
```

---

## CI/CD Integration

### GitHub Actions

```yaml
name: Generate Video

on:
  push:
    branches: [main]

jobs:
  generate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Install FFmpeg
        run: |
          sudo apt-get update
          sudo apt-get install -y ffmpeg
      
      - name: Generate Video
        run: |
          dotnet run --project Aura.Cli -- quick -t "Automated Video" -o ./artifacts
      
      - name: Upload Artifacts
        uses: actions/upload-artifact@v3
        with:
          name: video-output
          path: ./artifacts/
```

### GitLab CI

```yaml
generate_video:
  image: mcr.microsoft.com/dotnet/sdk:8.0
  before_script:
    - apt-get update && apt-get install -y ffmpeg
  script:
    - dotnet run --project Aura.Cli -- quick -t "$VIDEO_TOPIC" -o ./output
  artifacts:
    paths:
      - output/
```

---

## Logging

The CLI outputs structured logs to the console and can write detailed logs to files.

**Log Locations:**
- Console: Always enabled
- File: `artifacts/cli/<timestamp>/log.txt` (for quick command)

**Log Levels:**
- Default: Information
- Verbose (`-v`): Debug details, FFmpeg output

---

## Troubleshooting

### FFmpeg Not Found

**Error:**
```
✗ Error: FFmpeg not found in PATH
```

**Solution:**
Install FFmpeg:
- **Windows**: Download from https://ffmpeg.org/download.html, add to PATH
- **Linux**: `sudo apt-get install ffmpeg`
- **macOS**: `brew install ffmpeg`

### NVENC Not Detected

**Issue:** Hardware acceleration not being used despite having NVIDIA GPU.

**Solution:**
1. Update NVIDIA drivers
2. Verify GPU with `aura-cli preflight -v`
3. Check that GPU supports NVENC (GTX 600+ series)

### Script Generation Fails

**Error:**
```
✗ Error: Script generation failed
Exit Code: 310
```

**Solutions:**
- Check input JSON format against schema
- Use `--verbose` flag for detailed error messages
- Try with `--dry-run` to validate inputs first

### Video Rendering Slow

**Issue:** Rendering takes a long time.

**Solutions:**
1. Use hardware acceleration if available (check `preflight`)
2. Reduce video duration with `-d` flag
3. Lower quality settings in render spec

---

## Publishing Binaries

Build portable executables for distribution:

```bash
# Run the publish script
cd scripts/cli
pwsh ./publish_cli.ps1

# Output will be in artifacts/cli/<timestamp>/
# - bin-win-x64/aura-cli.exe (Windows, self-contained)
# - bin-linux-x64/aura-cli (Linux, framework-dependent)
```

---

## Examples

### Batch Processing

Generate multiple videos from a list:

```bash
#!/bin/bash
topics=("Python Basics" "JavaScript 101" "Go Tutorial")

for topic in "${topics[@]}"; do
  echo "Generating: $topic"
  aura-cli quick -t "$topic" -o "./videos/$topic"
  if [ $? -ne 0 ]; then
    echo "Failed to generate: $topic"
  fi
done
```

### Nightly Builds

Generate a video every night with fresh content:

```bash
# crontab entry
0 2 * * * cd /path/to/aura && dotnet run --project Aura.Cli -- quick -t "Daily Update $(date +\%Y-\%m-\%d)" -o /videos/daily
```

### Multi-Stage Pipeline

Use individual commands for more control:

```bash
#!/bin/bash
set -e

# Step 1: Generate script
aura-cli script -b brief.json -p plan.json -o script.txt

# Step 2: Review script (manual step or automated checks)
if grep -q "inappropriate" script.txt; then
  echo "Script contains inappropriate content, aborting"
  exit 1
fi

# Step 3: Compose timeline
aura-cli compose -i timeline.json -o plan.json

# Step 4: Render video
aura-cli render -r plan.json -o final.mp4

echo "Pipeline complete!"
```

---

## Support

- **Documentation**: https://github.com/Coffee285/aura-video-studio
- **Issues**: https://github.com/Coffee285/aura-video-studio/issues
- **CLI Help**: `aura-cli <command> --help`

---

## License

See the main repository for license information.
