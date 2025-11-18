# Application Settings and Preferences - User Guide

## Overview

The Application Settings and Preferences System provides a comprehensive way to configure all aspects of Aura Video Studio, including:
- General application behavior
- Provider configurations (OpenAI, Ollama, etc.)
- Hardware performance settings
- Video defaults
- Editor preferences
- UI customization
- Advanced options

All settings are stored locally and persist across application restarts.

## Accessing Settings

### From the UI
1. Click the **Settings** icon in the navigation sidebar
2. Navigate through different settings categories using the tabs
3. Make your changes
4. Click **Save** to persist your changes

### Via API
All settings can also be managed programmatically through the REST API (see API Reference below).

## Settings Categories

### 1. General Settings

Configure basic application behavior:

- **Theme**: Choose between Light, Dark, or Auto (system-based)
- **Language**: Set application language (default: en-US)
- **Startup Behavior**: What to show when application starts
  - Show Dashboard
  - Show Last Project
  - Show New Project Dialog
- **Auto-save**: Enable/disable auto-save and set interval (30-3600 seconds)
- **Check for Updates**: Automatically check for updates on startup
- **Advanced Mode**: Enable advanced features and settings

**Example:**
```json
{
  "theme": "Dark",
  "autosaveIntervalSeconds": 300,
  "autosaveEnabled": true,
  "advancedModeEnabled": false
}
```

### 2. Provider Configuration

Configure AI and service providers:

#### OpenAI
- **API Key**: Your OpenAI API key (sk-...)
- **Model**: Default model to use (gpt-4o-mini, gpt-4o, etc.)
- **Base URL**: Custom API endpoint (optional)
- **Organization ID**: For team accounts (optional)
- **Timeout**: Request timeout in seconds

#### Ollama (Local AI)
- **Base URL**: Ollama server URL (default: http://127.0.0.1:11434)
- **Model**: Ollama model to use (llama3.1:8b-q4_k_m, etc.)
- **Executable Path**: Path to Ollama executable
- **Auto-start**: Automatically start Ollama server

#### Other Providers
- Anthropic (Claude)
- Azure OpenAI
- Google Gemini
- ElevenLabs (TTS)
- Stable Diffusion (Image Generation)

**Connection Testing:**
Use the "Test Connection" button next to each provider to verify configuration.

### 3. Hardware Performance Settings

Optimize rendering and encoding performance:

- **Hardware Acceleration**: Enable GPU-accelerated encoding
- **Preferred Encoder**:
  - Auto (Recommended)
  - NVIDIA NVENC
  - AMD AMF
  - Intel QuickSync
  - Software (x264/x265)
- **GPU Selection**: For multi-GPU systems
- **RAM Allocation**: Memory allocated for rendering (0 = auto)
- **Max Threads**: Maximum rendering threads (0 = auto)
- **Preview Quality**:
  - Low (fastest)
  - Medium (balanced)
  - High (best quality)
  - Ultra (maximum quality)
- **Background Rendering**: Render in background while working
- **Max Cache Size**: Cache size in MB (default: 5000)

**GPU Detection:**
The system automatically detects your GPU and available encoders. Check the "Available Encoders" section to see what's supported on your system.

### 4. File Locations

Set default directories for various operations:

- **FFmpeg Path**: Path to FFmpeg executable
- **FFprobe Path**: Path to FFprobe executable
- **Output Directory**: Default location for exported videos
- **Temp Directory**: Temporary file storage
- **Projects Directory**: Default location for project files
- **Media Library**: Location for media assets

**Tips:**
- Leave paths empty to use auto-detection
- Use absolute paths for reliability
- Ensure directories have write permissions

### 5. Video Defaults

Set default video export settings:

- **Resolution**:
  - 1280x720 (HD)
  - 1920x1080 (Full HD)
  - 2560x1440 (2K)
  - 3840x2160 (4K)
- **Frame Rate**: 24-120 fps (default: 30)
- **Video Codec**:
  - libx264 (H.264 software)
  - libx265 (H.265 software)
  - h264_nvenc (NVIDIA H.264)
  - hevc_nvenc (NVIDIA H.265)
- **Video Bitrate**: Default bitrate (e.g., "5M")
- **Audio Codec**: aac, mp3, opus
- **Audio Bitrate**: Default audio bitrate (e.g., "192k")
- **Audio Sample Rate**: Default sample rate (44100 Hz)

### 6. Editor Preferences

Customize the video editor behavior:

- **Timeline Snap**: Snap clips to grid
- **Snap Interval**: Snap interval in seconds
- **Playback Quality**: Preview playback quality
- **Generate Thumbnails**: Auto-generate timeline thumbnails
- **Thumbnail Interval**: Interval between thumbnails
- **Show Waveforms**: Display audio waveforms
- **Show Timecode**: Display timecode overlay
- **Keyboard Shortcuts**: Customize keyboard shortcuts

### 7. UI Settings

Customize the user interface:

- **Scale**: UI scale percentage (80-150%)
- **Compact Mode**: Use compact UI layout
- **Color Scheme**: UI color theme

### 8. Advanced Settings

Additional configuration options:

- **Offline Mode**: Disable all cloud services
- **Stable Diffusion URL**: Local SD WebUI URL
- **Ollama URL**: Local Ollama server URL
- **Enable Telemetry**: Send anonymous usage data
- **Enable Crash Reports**: Send crash reports

## Import/Export Settings

### Export Settings

1. Go to Settings → Export/Import tab
2. Choose whether to include API keys (⚠️ Warning: stores keys in plain text)
3. Click "Export Settings"
4. Save the JSON file

**Via API:**
```bash
curl http://localhost:5000/api/settings/export > my-settings.json
```

### Import Settings

1. Go to Settings → Export/Import tab
2. Click "Import Settings"
3. Select your settings JSON file
4. Choose whether to overwrite existing settings
5. Review conflicts (if any)
6. Click "Apply"

**Via API:**
```bash
curl -X POST http://localhost:5000/api/settings/import \
  -H "Content-Type: application/json" \
  -d @my-settings.json
```

## Reset to Defaults

To reset all settings to their default values:

1. Go to Settings → General tab
2. Scroll to bottom
3. Click "Reset to Defaults"
4. Confirm the action

**Via API:**
```bash
curl -X POST http://localhost:5000/api/settings/reset
```

⚠️ **Warning**: This will reset ALL settings. Export your settings first if you want to restore them later.

## Settings Validation

The system automatically validates settings when you save them. If there are any issues:

- **Errors** (🔴): Must be fixed before saving
- **Warnings** (🟡): Can be saved, but may cause issues
- **Info** (ℹ️): Informational messages

Common validation errors:
- Autosave interval must be between 30-3600 seconds
- Frame rate must be between 24-120 fps
- Resolution must be a valid option
- File paths must exist (warnings only)

## API Reference

### Get All Settings
```http
GET /api/settings
```

### Update Settings
```http
PUT /api/settings
Content-Type: application/json

{
  "general": { "theme": "Dark", ... },
  "videoDefaults": { ... },
  ...
}
```

### Get Specific Section
```http
GET /api/settings/general
GET /api/settings/hardware
GET /api/settings/providers
```

### Update Specific Section
```http
PUT /api/settings/general
Content-Type: application/json

{ "theme": "Dark", "autosaveIntervalSeconds": 600 }
```

### Test Provider Connection
```http
POST /api/settings/providers/{providerName}/test
```

Supported providers: `openai`, `ollama`, `stablediffusion`

### Get Hardware Information
```http
GET /api/settings/hardware/gpus       # Available GPU devices
GET /api/settings/hardware/encoders   # Available encoders
```

### Validate Settings
```http
POST /api/settings/validate
Content-Type: application/json

{ /* settings object */ }
```

Response:
```json
{
  "isValid": false,
  "issues": [
    {
      "category": "General",
      "key": "AutosaveIntervalSeconds",
      "message": "Autosave interval must be between 30 and 3600 seconds",
      "severity": "Error"
    }
  ]
}
```

## Storage Location

Settings are stored locally in:

**Windows:**
- User Settings: `%LOCALAPPDATA%\Aura\AuraData\user-settings.json`
- Hardware Settings: `%LOCALAPPDATA%\Aura\AuraData\hardware-settings.json`
- API Keys: Encrypted in Windows Credential Manager

**Linux/macOS:**
- User Settings: `~/.local/share/Aura/AuraData/user-settings.json`
- Hardware Settings: `~/.local/share/Aura/AuraData/hardware-settings.json`
- API Keys: Encrypted in `~/.local/share/Aura/secure-keys/`

## Troubleshooting

### Settings Not Persisting
1. Check file permissions in AuraData directory
2. Ensure disk space is available
3. Check logs for errors: `Logs/aura-api-*.log`

### Provider Connection Test Fails
1. Verify API key is correct
2. Check internet connection (for cloud providers)
3. Verify local services are running (Ollama, Stable Diffusion)
4. Check firewall settings

### Hardware Acceleration Not Available
1. Verify GPU drivers are installed
2. Check GPU is detected: GET `/api/settings/hardware/gpus`
3. Verify FFmpeg supports your GPU encoder
4. Check available encoders: GET `/api/settings/hardware/encoders`

### Settings File Corrupted
If your settings file becomes corrupted:
1. Settings will automatically fall back to defaults
2. A backup is created before each save
3. Manually restore from: `AuraData/user-settings.json.backup`

## Best Practices

1. **Export settings regularly** - Create backups of your configuration
2. **Test provider connections** - Verify API keys work before starting projects
3. **Use auto-save** - Prevent data loss with automatic project saving
4. **Hardware acceleration** - Enable for faster rendering
5. **Start with defaults** - Only change settings you understand
6. **Review validation warnings** - Fix issues before they cause problems

## Security Notes

- ✅ API keys are stored encrypted
- ✅ Settings files are stored locally
- ✅ No data is sent to cloud (unless explicitly configured)
- ⚠️ Export files may contain sensitive data - handle carefully
- ⚠️ Do not share API keys or export files with secrets

## Need Help?

- Check the logs: `Logs/aura-api-*.log`
- View API documentation: `http://localhost:5000/swagger`
- Report issues: GitHub Issues
- Get support: Discord/Support channels

---

**Version**: 1.0.0  
**Last Updated**: 2025-11-10
