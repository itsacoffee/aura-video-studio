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

## Troubleshooting

### Port Already in Use

If you see an error that port 5005 is already in use:

1. Close any other Aura Video Studio instances
2. Check if another application is using port 5005
3. Kill the process using port 5005 or restart your computer

### Browser Shows 404 Error

If the browser shows a 404 error:

1. Wait a few seconds for the API to fully start
2. Check the console window for any error messages
3. Try refreshing the browser page
4. Verify the `wwwroot` folder exists inside the `Api` folder

### API Won't Start

If the API fails to start:

1. Check if you have antivirus software blocking the executable
2. Make sure you extracted all files from the ZIP
3. Try running `Aura.Api.exe` as administrator
4. Check the `logs` folder for error messages

### Web UI Won't Load

If the web UI doesn't load:

1. **Check the API console output**
   - Look for: `[INF] Serving static files from: C:\path\to\Api\wwwroot`
   - If you see: `[WRN] wwwroot directory not found` - the structure is incorrect

2. **Verify directory structure**
   - Correct: `Api\wwwroot\index.html` ✅
   - Wrong: `wwwroot\index.html` (at root level) ❌
   - Wrong: `Web\index.html` (separate folder) ❌

3. **Check that all files were extracted from the ZIP**
   - Make sure to extract ALL files, not just the executable

4. **Try a different web browser**
   - Chrome, Edge, or Firefox work best

5. **Clear your browser cache**
   - Press Ctrl+Shift+Delete in your browser

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
