# Aura Video Studio - Configuration Guide

Complete guide to configuring Aura Video Studio for optimal performance and workflows.

## Table of Contents

1. [Configuration Overview](#configuration-overview)
2. [General Settings](#general-settings)
3. [API Keys Configuration](#api-keys-configuration)
4. [Hardware Configuration](#hardware-configuration)
5. [Export Presets](#export-presets)
6. [File Locations](#file-locations)
7. [Provider Settings](#provider-settings)
8. [Performance Tuning](#performance-tuning)
9. [Import/Export Configuration](#importexport-configuration)
10. [Environment Variables](#environment-variables)
11. [Advanced Configuration](#advanced-configuration)

---

## Configuration Overview

Aura Video Studio uses a multi-layered configuration system:

1. **appsettings.json** - Base application configuration
2. **Environment Variables** - Sensitive data (API keys)
3. **User Settings** - Per-user preferences stored in portable directory
4. **Session State** - Temporary UI state

### Configuration Storage Locations

**Portable Installation** (All data in application folder):
```
<AppRoot>/
├── AuraData/
│   ├── config/
│   │   ├── hardware-config.json
│   │   ├── export-presets.json
│   │   └── backup-*.json
│   └── secure/
│       └── apikeys.dat (encrypted)
├── appsettings.json
└── appsettings.Development.json
```

---

## General Settings

### UI & Appearance

**Theme**: `Light` | `Dark` | `Auto` (default)
- Auto theme follows system preferences
- Changes apply immediately

**UI Scale**: `75%` to `150%` (default: `100%`)
- Adjusts interface size for accessibility
- Requires page refresh to apply

**Language**: Default `en-US`
- Localization settings for UI

### Project Defaults

**Default Project Save Location**:
- Where new projects are created
- Leave empty for system Documents folder

**Autosave**:
- **Enabled**: `true` (default)
- **Interval**: `300` seconds (5 minutes)
- Range: 30-3600 seconds

**Startup Behavior**: 
- `ShowDashboard` (default)
- `ShowLastProject`
- `ShowNewProjectDialog`

---

## API Keys Configuration

### Secure Storage

API keys are encrypted at rest using:
- **Windows**: DPAPI (Data Protection API)
- **Linux/macOS**: AES-256 encryption with machine-specific key

### Provider API Keys

#### OpenAI
- **Format**: `sk-...` (starts with "sk-")
- **Used for**: GPT-4, GPT-3.5 script generation
- **Get Key**: https://platform.openai.com/api-keys
- **Pricing**: https://openai.com/pricing

#### Anthropic (Claude)
- **Format**: `sk-ant-...`
- **Used for**: Claude AI script generation
- **Get Key**: https://console.anthropic.com/
- **Pricing**: https://www.anthropic.com/pricing

#### Google Gemini
- **Format**: `AIza...`
- **Used for**: Gemini Pro script generation
- **Get Key**: https://makersuite.google.com/
- **Pricing**: https://ai.google.dev/pricing

#### ElevenLabs
- **Used for**: Premium text-to-speech
- **Get Key**: https://elevenlabs.io/
- **Pricing**: https://elevenlabs.io/pricing

#### Stability AI
- **Format**: `sk-...`
- **Used for**: AI image generation
- **Get Key**: https://platform.stability.ai/
- **Pricing**: https://stability.ai/pricing

### API Key Management

**Validation**:
```
POST /api/setup/validate-key
{
  "provider": "openai",
  "apiKey": "sk-..."
}
```

**Saving Keys**:
```
POST /api/setup/save-api-keys
{
  "keys": {
    "openai": "sk-...",
    "elevenlabs": "..."
  }
}
```

**Check Key Status** (without revealing keys):
```
GET /api/setup/key-status
```

---

## Hardware Configuration

### GPU Selection

**Auto-detect** (recommended):
- Automatically selects best available GPU
- Detects NVENC, AMF, and QuickSync support

**Manual Selection**:
- Choose specific GPU when multiple available
- Useful for multi-GPU systems

### Hardware Acceleration

**Enable Hardware Acceleration**: `true` (default)
- Uses GPU encoders for faster rendering
- Significant performance improvement (3-10x faster)

### Video Encoders

#### Software Encoders (Always Available)

**libx264** (H.264):
- Quality: High
- Speed: Slow
- Best for: Compatibility, archival

**libx265** (H.265/HEVC):
- Quality: Very High
- Speed: Very Slow
- Best for: 4K content, file size optimization

#### Hardware Encoders (GPU-dependent)

**NVIDIA NVENC** (RTX 20/30/40 series):
- `h264_nvenc`: H.264 encoding
- `hevc_nvenc`: H.265/HEVC encoding
- Quality: High
- Speed: Very Fast (5-10x faster than software)

**AMD AMF** (RX 5000/6000/7000 series):
- `h264_amf`: H.264 encoding
- `hevc_amf`: H.265/HEVC encoding
- Quality: High
- Speed: Very Fast

**Intel Quick Sync** (11th gen+):
- `h264_qsv`: H.264 encoding
- `hevc_qsv`: H.265/HEVC encoding
- Quality: Good
- Speed: Fast

### Encoding Quality Presets

| Preset | Speed | Quality | Best For |
|--------|-------|---------|----------|
| Ultra Fast | Very Fast | Low | Previews, drafts |
| Fast | Fast | Good | Quick exports, social media |
| Balanced | Medium | High | General use, YouTube (default) |
| High Quality | Slow | Very High | Professional, archival |
| Maximum Quality | Very Slow | Maximum | Cinema, long-term archival |

### API Endpoints

**Get Configuration**:
```
GET /api/hardware-config
```

**Save Configuration**:
```
POST /api/hardware-config
{
  "preferredGpuId": "auto",
  "enableHardwareAcceleration": true,
  "preferredEncoder": "auto",
  "encodingPreset": "balanced",
  "useGpuForImageGeneration": true,
  "maxConcurrentJobs": 1
}
```

**List Available GPUs**:
```
GET /api/hardware-config/gpus
```

**List Available Encoders**:
```
GET /api/hardware-config/encoders
```

**Test Hardware Acceleration**:
```
POST /api/hardware-config/test-acceleration
```

---

## Export Presets

### Built-in Presets

#### Social Media

**YouTube 1080p**:
```json
{
  "resolution": "1920x1080",
  "frameRate": 30,
  "codec": "libx264",
  "bitrate": "8M",
  "audioCodec": "aac",
  "audioBitrate": "192k",
  "format": "mp4"
}
```

**YouTube 4K**:
```json
{
  "resolution": "3840x2160",
  "frameRate": 30,
  "codec": "libx265",
  "bitrate": "25M",
  "audioCodec": "aac",
  "audioBitrate": "256k",
  "format": "mp4"
}
```

**Instagram Story/TikTok**:
```json
{
  "resolution": "1080x1920",
  "frameRate": 30,
  "codec": "libx264",
  "bitrate": "5M",
  "audioCodec": "aac",
  "audioBitrate": "128k",
  "format": "mp4"
}
```

**Twitter**:
```json
{
  "resolution": "1280x720",
  "frameRate": 30,
  "codec": "libx264",
  "bitrate": "5M",
  "audioCodec": "aac",
  "audioBitrate": "128k",
  "format": "mp4"
}
```

#### Professional

**Archival 4K**:
```json
{
  "resolution": "3840x2160",
  "frameRate": 30,
  "codec": "libx265",
  "bitrate": "40M",
  "audioCodec": "flac",
  "format": "mkv"
}
```

**Draft Preview**:
```json
{
  "resolution": "1280x720",
  "frameRate": 30,
  "codec": "libx264",
  "preset": "ultrafast",
  "bitrate": "2M",
  "format": "mp4"
}
```

### Custom Presets

Create custom presets via UI or API:

**Create Preset**:
```
POST /api/export-presets
{
  "name": "My Custom 4K",
  "description": "Custom 4K preset for YouTube",
  "resolution": "3840x2160",
  "frameRate": 60,
  "codec": "hevc_nvenc",
  "bitrate": "30M",
  "audioCodec": "aac",
  "audioBitrate": "256k",
  "audioSampleRate": 48000,
  "format": "mp4"
}
```

**Update Preset**:
```
PUT /api/export-presets/{id}
```

**Delete Preset**:
```
DELETE /api/export-presets/{id}
```

### Format Recommendations

Get platform-specific recommendations:
```
GET /api/export-presets/format-recommendations/youtube
GET /api/export-presets/format-recommendations/instagram
GET /api/export-presets/format-recommendations/tiktok
```

---

## File Locations

### FFmpeg Configuration

**FFmpeg Path**: Path to `ffmpeg.exe`
- Leave empty to use system PATH
- Download from Downloads page if not installed

**FFprobe Path**: Path to `ffprobe.exe`
- Usually in same directory as FFmpeg
- Leave empty to auto-detect

**Search Paths** (from `appsettings.json`):
```json
{
  "FFmpeg": {
    "SearchPaths": [
      "C:\\Program Files\\ffmpeg\\bin",
      "C:\\ffmpeg\\bin",
      "%LOCALAPPDATA%\\Microsoft\\WinGet\\Packages\\Gyan.FFmpeg*\\ffmpeg*\\bin",
      "/usr/bin",
      "/usr/local/bin",
      "/opt/homebrew/bin"
    ]
  }
}
```

### Project Directories

**Output Directory**: Where rendered videos are saved
- Default: `Documents/AuraVideoStudio/Output`
- Can be customized per project

**Temp Directory**: Temporary files during rendering
- Automatically cleaned after successful render
- Default: System temp directory

**Projects Directory**: Where project files are stored
- Default: `Documents/AuraVideoStudio/Projects`
- Contains `.aura` project files

**Media Library**: Stock assets and downloads
- Default: `<AppRoot>/AuraData/MediaLibrary`

---

## Provider Settings

### LLM Providers

**OpenAI**:
- Models: GPT-4, GPT-3.5-turbo
- Requires: API key
- Best for: High-quality script generation

**Anthropic**:
- Models: Claude 3 Opus, Sonnet, Haiku
- Requires: API key
- Best for: Long-form content, nuanced writing

**Google Gemini**:
- Models: Gemini Pro
- Requires: API key
- Best for: Balanced quality and cost

**Ollama** (Local):
- Models: llama3.1, mistral, etc.
- Requires: Ollama installation
- Best for: Offline mode, privacy

**RuleBased** (Fallback):
- No external dependencies
- Template-based generation
- Best for: Offline, basic scripts

### TTS Providers

**ElevenLabs**:
- Premium quality voices
- Requires: API key
- Voice cloning available

**PlayHT**:
- High-quality voices
- Requires: User ID + Secret Key
- Voice cloning available

**Windows SAPI**:
- Free, built-in (Windows only)
- Adequate quality
- No API key required

**Piper**:
- Free, offline neural TTS
- Good quality
- Download from Downloads page

**Mimic3**:
- Free, offline
- Open source
- Community voices

### Image Providers

**Stable Diffusion WebUI**:
- Local GPU generation
- Requires: NVIDIA GPU (6GB+ VRAM)
- URL: `http://127.0.0.1:7860`

**Stock Images**:
- Pexels, Pixabay, Unsplash
- Requires: API keys
- Free tier available

---

## Performance Tuning

### System Tiers

Aura automatically detects system capabilities:

**Tier S** (High-end):
- 32GB+ RAM
- RTX 3080+ or equivalent
- Strategy: Maximum quality, parallel processing

**Tier A** (Upper mid):
- 16GB+ RAM
- RTX 3060+ or equivalent
- Strategy: High quality, selective parallel

**Tier B** (Mid-range):
- 16GB RAM
- GTX 1660+ or equivalent
- Strategy: Good quality, sequential

**Tier C** (Lower mid):
- 8GB RAM
- Integrated GPU
- Strategy: Basic quality, conservative

**Tier D** (Minimum):
- 8GB RAM
- CPU only
- Strategy: Offline only, minimal

### Performance Settings

**Max Concurrent Jobs**: `1-4`
- Number of videos to render simultaneously
- Higher values use more RAM and GPU
- Recommended: `1` for most systems

**Hardware Acceleration**: `true` (default)
- Disable for troubleshooting
- Software encoding is much slower

**GPU for Image Generation**: `true` (default)
- Requires compatible GPU
- Much faster than CPU generation

---

## Import/Export Configuration

### Export Configuration

Export complete settings to JSON:
```
GET /api/configuration/export
```

Downloaded file: `aura-config-YYYY-MM-DD.json`

**Contents**:
- Application configuration
- Provider settings
- FFmpeg configuration
- Performance settings
- LLM timeouts
- Prompt engineering settings

**Security Note**: Exported configuration does NOT include API keys

### Import Configuration

Validate and import configuration:
```
POST /api/configuration/import
{
  "version": "1.0.0",
  "configuration": { ... }
}
```

**Validation**:
- Schema version check
- Configuration value validation
- Compatibility verification

### Configuration Backup

**Create Backup**:
```
POST /api/configuration/backup
```

**List Backups**:
```
GET /api/configuration/backups
```

Backups stored in: `<AppRoot>/AuraData/config/backup-*.json`

### Reset Configuration

Reset to defaults (creates automatic backup):
```
POST /api/configuration/reset
{
  "confirm": true
}
```

---

## Environment Variables

### Supported Variables

**API Configuration**:
```bash
AURA_API_URL=http://127.0.0.1:5005
ASPNETCORE_URLS=http://127.0.0.1:5005
ASPNETCORE_ENVIRONMENT=Production
DOTNET_ENVIRONMENT=Production
```

**API Keys** (secure alternative to file storage):
```bash
OPENAI_API_KEY=sk-...
ANTHROPIC_API_KEY=sk-ant-...
ELEVENLABS_API_KEY=...
STABILITY_API_KEY=sk-...
```

**Feature Flags**:
```bash
AURA_OFFLINE_MODE=true
AURA_ENABLE_TELEMETRY=false
AURA_ENABLE_CRASH_REPORTS=false
```

### Viewing Environment Variables

```
GET /api/configuration/environment
```

Returns configured variables (keys masked for security).

---

## Advanced Configuration

### appsettings.json Structure

```json
{
  "Urls": "http://127.0.0.1:5005",
  
  "FFmpeg": {
    "ExecutablePath": "",
    "ProbeExecutablePath": "",
    "RequireMinimumVersion": "4.0.0",
    "SearchPaths": [ "..." ]
  },
  
  "Engines": {
    "InstallRoot": "%LOCALAPPDATA%/Aura/Tools",
    "DefaultPorts": {
      "stable-diffusion-webui": 7860,
      "comfyui": 8188,
      "piper": 0,
      "mimic3": 59125
    }
  },
  
  "Performance": {
    "SlowRequestThresholdMs": 1000,
    "VerySlowRequestThresholdMs": 5000,
    "EnableDetailedTelemetry": true,
    "SampleRate": 1.0
  },
  
  "LlmTimeouts": {
    "ScriptGenerationTimeoutSeconds": 120,
    "ScriptRefinementTimeoutSeconds": 180,
    "VisualPromptTimeoutSeconds": 45,
    "NarrationOptimizationTimeoutSeconds": 30,
    "PacingAnalysisTimeoutSeconds": 60,
    "WarningThresholdPercentage": 0.5
  },
  
  "PromptEngineering": {
    "EnableCustomization": true,
    "DefaultPromptVersion": "default-v1",
    "MaxCustomInstructionsLength": 5000,
    "EnableChainOfThought": true,
    "EnableQualityMetrics": true
  }
}
```

### Configuration Validation

Validate current configuration:
```
POST /api/configuration/validate
```

Returns:
- Validation result (pass/fail)
- List of issues with severity (Warning/Critical)
- Specific configuration keys with problems

### Configuration Schema

Get configuration schema with metadata:
```
GET /api/configuration/schema
```

Returns:
- Available sections
- Setting definitions
- Data types and validation rules
- Default values
- Allowed values for enums

---

## Troubleshooting

### Common Configuration Issues

**Issue**: Hardware acceleration not working
- Check GPU drivers are up to date
- Verify FFmpeg supports hardware encoding
- Test with `POST /api/hardware-config/test-acceleration`

**Issue**: API key validation fails
- Verify key format (should start with `sk-` for most)
- Check for trailing spaces or newlines
- Ensure sufficient API credits

**Issue**: Configuration not persisting
- Check AuraData directory is writable
- Verify no antivirus blocking file writes
- Check disk space availability

**Issue**: Export presets not loading
- Check `export-presets.json` for syntax errors
- Verify file permissions
- Try resetting to built-in presets

### Debug Configuration

**Enable Verbose Logging**:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  }
}
```

**Check Configuration Values**:
```
GET /api/configuration/value/FFmpeg:ExecutablePath
GET /api/configuration/value/Engines:InstallRoot
```

---

## Best Practices

1. **Backup Before Major Changes**: Always backup configuration before importing or resetting
2. **Use Hardware Acceleration**: Significant performance improvement
3. **Configure API Keys Securely**: Use environment variables for production
4. **Test Export Presets**: Verify quality before long renders
5. **Keep FFmpeg Updated**: Latest versions have better codec support
6. **Monitor Resource Usage**: Adjust concurrent jobs based on system performance
7. **Use Preset Recommendations**: Built-in presets optimized for platforms
8. **Regular Backups**: Export settings periodically for disaster recovery

---

## Support & Resources

- **Documentation**: `/docs`
- **API Reference**: `/api/swagger`
- **Issue Tracker**: GitHub Issues
- **Community**: Discord/Forums

---

**Last Updated**: 2025-11-01
**Configuration Version**: 1.0.0
