# Portable Mode - Visual Guide

## Settings Page UI

```
┌─────────────────────────────────────────────────────────────────┐
│ Settings                                                         │
│ Configure system preferences, providers, and API keys           │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ [System] [UI] [✓ Portable Mode] [Providers] [API Keys] ...     │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ Portable Mode                                                    │
│ Configure portable mode to install all tools under a single     │
│ folder for easy copying/moving                                  │
│                                                                  │
│ ┌──────────────────────────────────────────────────────────┐   │
│ │ ℹ️ About Portable Mode                                    │   │
│ │                                                            │   │
│ │ When enabled, all downloaded dependencies (FFmpeg,        │   │
│ │ Ollama, Stable Diffusion, etc.) will be installed to      │   │
│ │ your specified folder instead of system AppData. This     │   │
│ │ allows you to:                                             │   │
│ │                                                            │   │
│ │  • Copy the entire application folder to another machine  │   │
│ │  • Move the application without breaking dependencies     │   │
│ │  • Keep all tools organized in one location               │   │
│ │                                                            │   │
│ │ Note: System dependencies (GPU drivers) must still be     │   │
│ │ installed on each machine.                                 │   │
│ └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│ Enable Portable Mode                                            │
│ [●─────] ON                                                      │
│ Enabled - Install tools to a custom folder                      │
│                                                                  │
│ Portable Root Path                                              │
│ All tools will be installed here                                │
│ ┌────────────────────────────────────────────────────────┐     │
│ │ C:\TTS\aura-video-studio                               │     │
│ └────────────────────────────────────────────────────────┘     │
│                                                                  │
│ ┌──────────────────────────────────────────────────────────┐   │
│ │ Current Configuration                                     │   │
│ │                                                           │   │
│ │ Mode:            Portable                                 │   │
│ │ Tools Directory: C:\TTS\aura-video-studio                 │   │
│ └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│ ⚠️ You have unsaved changes. Save and restart for changes.     │
│                                                                  │
│ [Save Portable Mode Settings]  [Open Tools Folder]             │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

## Directory Structure Comparison

### Standard Mode (AppData)
```
%LOCALAPPDATA%\Aura\
├── dependencies\
│   ├── ffmpeg.exe
│   ├── ffprobe.exe
│   └── manifest.json
├── Tools\
│   ├── ffmpeg\
│   ├── engines\
│   │   ├── StableDiffusion\
│   │   ├── ComfyUI\
│   │   └── Piper\
│   └── models\
└── provider-paths.json
```

### Portable Mode
```
C:\TTS\aura-video-studio\       (user-specified)
├── ffmpeg.exe                  (direct in root)
├── ffprobe.exe
├── manifest.json
├── Tools\
│   ├── ffmpeg\
│   ├── engines\
│   │   ├── StableDiffusion\
│   │   ├── ComfyUI\
│   │   └── Piper\
│   └── models\
└── (settings stored in AppData)
```

## API Endpoints

### Get Portable Mode Settings
```http
GET /api/settings/portable
```

Response:
```json
{
  "portableModeEnabled": true,
  "portableRootPath": "C:\\TTS\\aura-video-studio",
  "toolsDirectory": "C:\\TTS\\aura-video-studio",
  "defaultAppDataPath": "C:\\Users\\User\\AppData\\Local\\Aura\\dependencies"
}
```

### Save Portable Mode Settings
```http
POST /api/settings/portable
Content-Type: application/json

{
  "portableModeEnabled": true,
  "portableRootPath": "C:\\TTS\\aura-video-studio"
}
```

### Open Tools Folder
```http
POST /api/settings/open-tools-folder
```

Opens the tools directory in the file explorer.

## Usage Flow

1. **Enable Portable Mode**
   - Navigate to Settings → Portable Mode tab
   - Toggle "Enable Portable Mode" switch to ON
   - Enter custom path (e.g., `C:\TTS\aura-video-studio`)
   - Click "Save Portable Mode Settings"

2. **Restart Application**
   - Close and restart the application
   - All installers will now use portable location

3. **Install Tools**
   - Go to Downloads page
   - Install FFmpeg, engines, etc.
   - All files go to portable location

4. **Move to Another Machine**
   - Copy the entire portable folder
   - Paste on new machine
   - Enable portable mode in settings with same path
   - App detects all installed tools

## Benefits

✅ **Portability** - Easy to move between machines
✅ **Clean Install** - All dependencies in one place
✅ **Easy Backup** - Single folder to backup
✅ **Multi-Instance** - Run multiple versions side-by-side
✅ **Network Drive** - Install on shared drives
✅ **Easy Uninstall** - Just delete the folder

## Implementation Details

All installers check `ProviderSettings.IsPortableModeEnabled()`:
- `DependencyManager` - Base dependencies (ffmpeg, etc.)
- `EngineInstaller` - Local engines
- `ModelInstaller` - AI models
- `EngineDetector` - Engine discovery
- `FfmpegInstaller` - FFmpeg binaries
- `FfmpegLocator` - FFmpeg path resolution

Settings stored in: `%LOCALAPPDATA%\Aura\provider-paths.json`

```json
{
  "portableModeEnabled": true,
  "portableRootPath": "C:\\TTS\\aura-video-studio",
  "stableDiffusionUrl": "http://127.0.0.1:7860",
  "ollamaUrl": "http://127.0.0.1:11434"
}
```
