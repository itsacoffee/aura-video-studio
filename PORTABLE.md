# Aura Video Studio - Portable Edition

## Overview

Aura Video Studio is distributed as a **portable-only** application. All data, settings, and dependencies are stored within the application folder, making it easy to move or backup your installation.

## What's Included

After extracting the ZIP, you'll have this portable structure:

```
AuraVideoStudio_Portable_x64/
├── api/
│   ├── Aura.Api.exe              ← Main executable
│   ├── wwwroot/                   ← Web UI files (MUST be here!)
│   │   ├── index.html
│   │   └── assets/
│   └── (runtime dependencies)
├── Tools/                         ← Downloaded dependencies (empty initially)
│   └── (FFmpeg, Ollama, etc. installed here)
├── AuraData/                      ← Application data and settings
│   ├── settings.json
│   ├── install-manifest.json
│   └── README.txt
├── Logs/                          ← Application logs (created on first run)
├── Projects/                      ← Generated videos and project files
├── Downloads/                     ← Temporary download storage
├── ffmpeg/                        ← Optional pre-bundled FFmpeg
│   ├── ffmpeg.exe
│   └── ffprobe.exe
├── start_portable.cmd             ← Easy launcher with health check
├── README.md                      ← This file
├── checksums.txt                  ← File integrity checksums
└── LICENSE
```

## Portable Benefits

✅ **No installation required** - Extract and run  
✅ **No registry entries** - Everything stored in the app folder  
✅ **Easy backup** - Copy the entire folder  
✅ **Multiple installations** - Run different versions side-by-side  
✅ **Clean uninstall** - Just delete the folder  

**Important:** All dependencies and settings are stored within this folder. You can move the entire folder to another location or machine without breaking anything.

## System Requirements

- Windows 10 or Windows 11 (64-bit)
- 4 GB RAM minimum (8 GB recommended)
- 2 GB free disk space
- Modern web browser (Chrome, Edge, Firefox)

## Quick Start

### Option 1: Using the Launcher (Recommended)

1. Extract the ZIP file to any folder (e.g., `C:\Aura` or `D:\Tools\Aura`)
2. Double-click `start_portable.cmd`
3. Wait for the launcher to verify the API is healthy
4. Your default browser will open to `http://127.0.0.1:5005`
5. The application will create `Logs/`, `AuraData/`, and other folders on first run

### Option 2: Manual Launch

1. Extract the ZIP file to any folder
2. Navigate to the `api` folder
3. Double-click `Aura.Api.exe`
4. Open your web browser and go to `http://127.0.0.1:5005`

## Portable Data Structure

All application data is stored in the extracted folder:

- **Tools/** - Downloaded dependencies (FFmpeg, Ollama, Stable Diffusion, etc.)
- **AuraData/** - Settings, manifests, and configuration
  - `settings.json` - User preferences and provider configuration
  - `install-manifest.json` - Tracks installed components
- **Logs/** - Application and tool logs (check here for troubleshooting)
- **Projects/** - Your generated videos and project files
- **Downloads/** - Temporary storage for downloads in progress

You can move the entire folder to another location or machine, and everything will continue to work (except system dependencies like GPU drivers).

## Offline Provider Setup

Aura Video Studio can operate offline using local providers. Here's how to set up each offline provider:

### Ollama (LLM)

**What it is:** Local LLM for script generation  
**Required:** Yes, for offline script generation  
**Installation:**
1. Download Ollama from https://ollama.ai
2. Install and start the Ollama service
3. Pull a model: `ollama pull llama3.1:8b-q4_k_m`
4. Verify: `ollama list` should show your installed model

**Recommendations:**
- For 8GB RAM: Use 3B models or smaller
- For 16GB+ RAM: Use 8B models (llama3.1:8b-q4_k_m recommended)
- Configure keep-alive in Settings to reduce model loading time

### Piper TTS (Text-to-Speech)

**What it is:** Fast, offline neural TTS  
**Required:** Recommended for offline TTS (alternative to Windows TTS)  
**Installation:**
1. Download Piper from https://github.com/rhasspy/piper/releases
2. Extract to `Tools/piper/` in your Aura folder
3. Download a voice model (e.g., en_US-lessac-medium) from Piper voices repo
4. Configure paths in Settings → Provider Paths

**Voice Models:**
- `en_US-lessac-medium`: Good quality, balanced speed
- `en_US-amy-medium`: Alternative female voice
- `en_GB-alba-medium`: British English

### Mimic3 TTS (Text-to-Speech)

**What it is:** Server-based offline TTS  
**Required:** Optional (alternative to Piper and Windows TTS)  
**Installation:**
1. Install via Docker: `docker run -p 59125:59125 mycroftai/mimic3`
2. Or via pip: `pip install mycroft-mimic3-tts`
3. Start server: `mimic3-server`
4. Configure URL in Settings if using non-default port

### Windows TTS (Text-to-Speech)

**What it is:** Built-in Windows text-to-speech  
**Required:** No (automatically available on Windows)  
**Setup:** No installation needed on Windows

**Note:** Provides basic quality suitable for testing. Consider Piper or Mimic3 for better quality.

### Stable Diffusion WebUI (Images - Optional)

**What it is:** Local image generation  
**Required:** No (stock images used as fallback)  
**Installation:**
1. Requires NVIDIA GPU with 6GB+ VRAM
2. Download from https://github.com/AUTOMATIC1111/stable-diffusion-webui
3. Download at least one checkpoint model (e.g., Stable Diffusion 1.5)
4. Start WebUI with API enabled: `./webui.sh --api` (Linux/Mac) or `webui.bat --api` (Windows)
5. Configure URL in Settings if using non-default port

**VRAM Recommendations:**
- 6-8GB: Use 512x512 resolution
- 8-12GB: Use 768x768 resolution
- 12GB+: Use high resolutions and advanced features

### Checking Provider Status

Aura Video Studio provides a built-in offline provider status check:

1. Open Settings → Providers → Offline Status
2. The system will check all offline providers
3. Follow installation links and recommendations for any missing providers
4. Refresh to verify installation

**Provider Profile Recommendations:**
- **Free-Only Profile**: Uses only offline providers (Ollama, Windows TTS/Piper, stock images)
- **Balanced Mix Profile**: Combines offline and cloud providers
- **Pro-Max Profile**: Primarily cloud providers for highest quality

## Troubleshooting

### Port Already in Use

If you see an error that port 5005 is already in use:

1. Close any other Aura Video Studio instances
2. Check if another application is using port 5005
3. Kill the process using port 5005 or restart your computer

### Blank White Page / 404 Error

If you see a blank white page or 404 error when accessing `http://127.0.0.1:5005`:

**This is the most common issue and indicates the web UI files are missing.**

**Immediate Fixes:**

1. **Check if Launch.bat gave you an error before starting**
   - If Launch.bat showed an error about missing `wwwroot` or `index.html`, the build is incomplete
   - Re-extract the entire ZIP file to a new folder
   - Make sure you extracted ALL files, not just selected ones

2. **Check the API console window for error messages**
   - Look for: `[INF] Serving static files from: C:\path\to\Api\wwwroot` ✅ (Good)
   - If you see: `[ERR] CRITICAL: wwwroot directory not found` ❌ (Problem!)
   - The error message will tell you exactly what's wrong

3. **Verify the directory structure is correct**
   ```
   AuraVideoStudio_Portable_x64/
   ├── Api/
   │   ├── wwwroot/              ← Must exist!
   │   │   ├── index.html        ← Must exist!
   │   │   └── assets/           ← Must exist!
   │   └── Aura.Api.exe
   └── Launch.bat
   ```

4. **Check that wwwroot folder exists and has files**
   - Open File Explorer and navigate to the extracted folder
   - Go to `Api\wwwroot\`
   - You should see `index.html` and an `assets` folder with JavaScript files
   - If these are missing, the ZIP file is corrupted or incomplete

**If the problem persists:**

1. **Re-download the ZIP file** - it may have been corrupted during download
2. **Extract to a simpler path** - avoid spaces and special characters in the path
3. **Try a different extraction tool** - Windows built-in extractor, 7-Zip, or WinRAR
4. **Check antivirus logs** - some antivirus software may block or delete files

**For developers rebuilding from source:**

If you built the portable package yourself and encounter this issue:

1. Check that `npm run build` completed successfully in `Aura.Web` folder
2. Verify that `Aura.Web\dist\` folder contains `index.html` and `assets`
3. Re-run the build script: `scripts\packaging\build-portable.ps1`
4. The build script will now validate the web UI files and fail with clear errors if something is wrong

### API Won't Start

If the API fails to start:

1. Check if you have antivirus software blocking the executable
2. Make sure you extracted all files from the ZIP
3. Try running `Aura.Api.exe` as administrator
4. Check the `Logs` folder for error messages
5. Make sure port 5005 is not already in use by another application

### Other Common Issues

**Browser shows wrong page:**
1. Clear your browser cache (Ctrl+Shift+Delete)
2. Try a different web browser (Chrome, Edge, or Firefox work best)
3. Make sure you're going to `http://127.0.0.1:5005` (not `localhost`)

**Application is slow to start:**
1. Wait 5-10 seconds for the API to fully initialize
2. The first startup may take longer as it creates necessary folders
3. Check the console window - you should see "Application started" message

## Logs

Application logs are stored in the `Logs/` folder at the root of your installation:

- `Logs/aura-api-YYYYMMDD.log` - Daily API log files
- `Logs/tools/` - Logs from background tools (if any)

Check these files if you encounter issues. Logs include structured information with correlation IDs for debugging.

## Uninstalling

To remove the portable installation:

1. Close the application (close the API window or terminate the process)
2. Delete the entire extracted folder
3. All settings and data are contained within - no registry or AppData cleanup needed

## Backing Up Your Installation

To backup your settings and projects:

1. Copy the entire folder to another location
2. Or just backup specific folders:
   - `AuraData/` - Settings and configuration
   - `Projects/` - Your generated videos
   - `Tools/` - Downloaded dependencies (can be re-downloaded)

## Moving to Another Machine

To move your installation:

1. Copy the entire folder to the new machine
2. Make sure system dependencies are installed:
   - .NET 8 Runtime (usually included in the portable build)
   - GPU drivers (if using GPU features)
3. Run `start_portable.cmd`
4. All your settings, projects, and tools will work immediately

Note: Downloaded tools in the `Tools/` folder are portable, but you may need to re-download them if they depend on specific system configurations.

## Firewall and Security

**Firewall Prompt**: Windows Firewall may ask for permission when you first run `Aura.Api.exe`. This is normal - the API needs to accept local HTTP connections on port 5005. Click "Allow access" to continue.

**Security**: The application only listens on localhost (127.0.0.1) and is not accessible from other machines unless you explicitly configure it.

## Health Check

To verify the API is running correctly, open `http://127.0.0.1:5005/healthz` in your browser. You should see:

```json
{
  "status": "healthy",
  "timestamp": "2025-10-08T04:00:00.0000000Z"
}
```

## API Endpoints

The API provides the following endpoints:

- `GET /healthz` - Health check
- `GET /capabilities` - Hardware capabilities
- `POST /script` - Generate video script
- `POST /tts` - Text-to-speech synthesis
- `POST /render` - Render video
- And more... (see API documentation)

## Support

For issues, questions, or feature requests, please visit:
https://github.com/Coffee285/aura-video-studio/issues

## License

See the LICENSE file included in this distribution.
