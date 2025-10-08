# Aura Video Studio - Portable Version

## Overview

The portable version of Aura Video Studio is a self-contained distribution that requires no installation. Simply extract and run!

## What's Included

After extracting the ZIP, you'll have this structure:

```
AuraVideoStudio_Portable_x64/
├── Api/
│   ├── Aura.Api.exe            ← Main executable
│   ├── wwwroot/                 ← Web UI files (MUST be here!)
│   │   ├── index.html
│   │   └── assets/
│   └── (DLLs and dependencies)
├── ffmpeg/
│   ├── ffmpeg.exe
│   └── ffprobe.exe
├── Launch.bat                   ← Easy launcher
├── README.md                    ← This file
├── appsettings.json            ← Configuration
└── LICENSE
```

**Important:** The `wwwroot` folder MUST be inside the `Api` folder for the web UI to work!

## System Requirements

- Windows 10 or Windows 11 (64-bit)
- 4 GB RAM minimum (8 GB recommended)
- 2 GB free disk space
- Modern web browser (Chrome, Edge, Firefox)

## Quick Start

### Option 1: Using the Launcher (Recommended)

1. Extract the ZIP file to any folder
2. Double-click `Launch.bat`
3. The API will start and your default browser will open to `http://127.0.0.1:5005`
4. Wait a few seconds for the application to load

### Option 2: Manual Launch

1. Extract the ZIP file to any folder
2. Navigate to the `Api` folder
3. Double-click `Aura.Api.exe`
4. Open your web browser and go to `http://127.0.0.1:5005`

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

Application logs are stored in the `logs/` folder inside the `Api` directory. Check these files if you encounter any issues:

- `aura-api-YYYYMMDD.log` - Daily log files

## Firewall Warning

Windows Firewall may prompt you to allow network access for `Aura.Api.exe`. This is normal - the API needs to accept local HTTP connections on port 5005. Click "Allow access" to continue.

## Uninstalling

To remove the portable version:

1. Close the application
2. Delete the extracted folder
3. Optionally, delete settings stored in `%LOCALAPPDATA%\Aura\`

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
