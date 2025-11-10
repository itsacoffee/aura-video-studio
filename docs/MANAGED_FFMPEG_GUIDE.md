# Managed FFmpeg Installation Guide

## Overview

Aura Video Studio includes a managed FFmpeg installation system that automatically downloads, installs, and configures FFmpeg for video rendering. This eliminates the need for manual FFmpeg installation and ensures compatibility across all supported platforms.

## Features

- **Automatic Installation**: One-click installation from Settings or error dialogs
- **Version Management**: Installs to versioned directories for easy updates
- **Integrity Verification**: SHA256 checksum validation for downloaded files
- **Smart Resolution**: Automatically detects and uses the best available FFmpeg
- **Fallback Support**: Gracefully falls back to system FFmpeg if needed

## Installation

### From Settings Page

1. Open **Settings** > **File Locations**
2. View the **FFmpeg Status Card** at the top
3. Click **"Install Managed FFmpeg"** if not installed
4. Wait for the installation to complete (progress shown)
5. FFmpeg is now ready to use

### From Error Dialog

If Quick Demo or video generation fails due to missing FFmpeg:

1. An error dialog will appear with FFmpeg-specific options
2. Click **"Install FFmpeg"** (primary blue button)
3. Wait for installation to complete
4. Click **"Dismiss"** to close the dialog
5. Retry your video generation

## Installation Location

Managed FFmpeg is installed to a platform-specific application data directory:

- **Windows**: `%LOCALAPPDATA%\AuraVideoStudio\ffmpeg\<version>\`
  - Example: `C:\Users\YourName\AppData\Local\AuraVideoStudio\ffmpeg\7.0\`
  
- **macOS**: `~/Library/Application Support/AuraVideoStudio/ffmpeg/<version>/`
  - Example: `/Users/YourName/Library/Application Support/AuraVideoStudio/ffmpeg/7.0/`
  
- **Linux**: `~/.local/share/AuraVideoStudio/ffmpeg/<version>/`
  - Example: `/home/yourname/.local/share/AuraVideoStudio/ffmpeg/7.0/`

Each installation includes:
- `ffmpeg.exe` / `ffmpeg` - Main FFmpeg executable
- `install.json` - Metadata with version, source URL, checksum, validation status

## Resolution Priority

When FFmpeg is needed, Aura Video Studio checks locations in this order:

1. **Managed Installation** (highest priority)
   - Checks `%LOCALAPPDATA%\AuraVideoStudio\ffmpeg\<version>\`
   - Uses the latest version if multiple are installed
   - Validates the binary before use

2. **User-Configured Path**
   - From Settings > File Locations > FFmpeg Path
   - Supports direct path to executable or directory containing `ffmpeg.exe`
   - Validates the binary before use

3. **System PATH** (lowest priority)
   - Checks if `ffmpeg` is available on system PATH
   - Common locations: `/usr/bin/ffmpeg`, `C:\ffmpeg\bin\ffmpeg.exe`, etc.
   - Validates the binary before use

### Special Cases

- If configured path is literally `"ffmpeg"`, it's treated as a PATH lookup (not an error)
- PATH-based FFmpeg is supported but not recommended (use managed install for consistency)
- Invalid or missing paths are automatically detected and reported

## API Endpoints

### Get FFmpeg Status

```http
GET /api/ffmpeg/status
```

Returns current FFmpeg installation status:

```json
{
  "installed": true,
  "version": "7.0",
  "path": "C:\\Users\\YourName\\AppData\\Local\\AuraVideoStudio\\ffmpeg\\7.0\\ffmpeg.exe",
  "source": "Managed",
  "valid": true,
  "error": null,
  "correlationId": "abc123"
}
```

**Sources**:
- `Managed` - Installed via managed installation
- `Configured` - User-configured path from Settings
- `PATH` - Found on system PATH
- `None` - Not found

### Install Managed FFmpeg

```http
POST /api/ffmpeg/install
Content-Type: application/json

{
  "version": null
}
```

Installs managed FFmpeg from trusted mirrors. Pass `null` or omit `version` to install latest.

**Response on Success** (200 OK):
```json
{
  "success": true,
  "path": "C:\\Users\\YourName\\AppData\\Local\\AuraVideoStudio\\ffmpeg\\7.0\\ffmpeg.exe",
  "version": "7.0",
  "installedAt": "2024-01-15T10:30:00Z",
  "correlationId": "abc123"
}
```

**Response on Failure** (500):
```json
{
  "type": "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E313",
  "title": "Installation Failed",
  "status": 500,
  "detail": "Failed to download FFmpeg from any mirror",
  "correlationId": "abc123"
}
```

## Caching

FFmpeg resolution is cached for **5 minutes** to avoid repeated validation checks. The cache is automatically invalidated when:

- FFmpeg is installed via managed installation
- FFmpeg is uninstalled
- Settings are changed

To force a fresh check, use the **"Refresh Status"** button in Settings.

## Troubleshooting

### Installation Fails

**Problem**: Installation fails with network error

**Solution**:
- Check internet connection
- Verify firewall/antivirus isn't blocking downloads
- Try again later (mirrors may be temporarily unavailable)

### Managed FFmpeg Not Detected

**Problem**: Installed FFmpeg but app still reports it missing

**Solution**:
- Click **"Refresh Status"** in Settings
- Check installation directory exists and contains `ffmpeg.exe`
- Verify `install.json` metadata file is present
- Restart the application

### Permission Errors

**Problem**: Cannot install FFmpeg due to permission errors

**Solution**:
- Run Aura Video Studio with appropriate permissions
- Check `%LOCALAPPDATA%` directory is writable
- Verify antivirus isn't quarantining the download

### Version Mismatch

**Problem**: FFmpeg version is older than expected

**Solution**:
- Delete old version directory from installation location
- Click **"Install Managed FFmpeg"** to get latest version
- Managed installations are versioned, multiple can coexist

## Security

- **Checksum Verification**: All downloads are verified with SHA256 checksums
- **Trusted Sources**: Downloads only from configured trusted mirrors
- **Validation**: FFmpeg binary is executed with `-version` to ensure it works
- **Smoke Test**: A short audio generation test confirms functionality

## Uninstallation

To remove managed FFmpeg:

1. Navigate to the installation directory (see Installation Location above)
2. Delete the entire `AuraVideoStudio` directory
3. Restart Aura Video Studio
4. The app will detect FFmpeg is missing and offer to reinstall

Or delete just the specific version:
- Delete `%LOCALAPPDATA%\AuraVideoStudio\ffmpeg\<version>\`
- Keep other versions intact

## Advanced Configuration

### Custom Download Sources

Administrators can configure custom FFmpeg download sources by modifying `engine_manifest.json`:

```json
{
  "engines": [
    {
      "id": "ffmpeg",
      "urls": {
        "windows": "https://your-mirror.com/ffmpeg-windows.zip"
      },
      "mirrors": {
        "windows": [
          "https://backup-mirror.com/ffmpeg.zip"
        ]
      }
    }
  ]
}
```

### Offline Installation

For offline environments:

1. Download FFmpeg archive manually
2. Place in installation directory
3. Create `install.json` metadata file
4. Restart application to detect

## Related Documentation

- [FFmpeg Setup Guide](./FFmpeg_Setup_Guide.md)
- [Dependencies Documentation](./dependencies/)
- [System Requirements](./README.md#system-requirements)
