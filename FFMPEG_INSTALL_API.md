# FFmpeg Download & Installation API

This document describes the FFmpeg installation endpoints and how to use them.

## Overview

The FFmpeg installer provides three installation modes:
1. **Managed** - Download from official mirrors with automatic fallback
2. **Local** - Install from a local archive file
3. **Attach** - Attach an existing FFmpeg installation

All installations are validated by running `ffmpeg -version` before being marked as successful. Installation metadata is stored in `install.json` for tracking.

## Installation Locations

- **Managed/Local installs**: `%LOCALAPPDATA%\Aura\Tools\ffmpeg\{version}\`
- **Attached installs**: Original location (metadata written to that directory)
- **Logs**: `%LOCALAPPDATA%\Aura\Logs\Tools\` and `%LOCALAPPDATA%\Aura\Logs\ffmpeg\`

## API Endpoints

### 1. Install FFmpeg

**POST** `/api/downloads/ffmpeg/install`

Install FFmpeg using one of three modes.

#### Request Body

```json
{
  "mode": "managed",        // "managed", "local", or "attach"
  "customUrl": "https://...",  // Optional: custom download URL (managed mode only)
  "localArchivePath": "C:\\path\\to\\ffmpeg.zip",  // Required for "local" mode
  "attachPath": "C:\\path\\to\\ffmpeg.exe",  // Required for "attach" mode (can be exe or directory)
  "version": "6.0"          // Optional: version string (defaults to manifest version)
}
```

#### Examples

**Managed Installation (default mirrors)**
```bash
curl -X POST http://127.0.0.1:5005/api/downloads/ffmpeg/install \
  -H "Content-Type: application/json" \
  -d '{"mode":"managed"}'
```

**Managed Installation (custom URL)**
```bash
curl -X POST http://127.0.0.1:5005/api/downloads/ffmpeg/install \
  -H "Content-Type: application/json" \
  -d '{
    "mode": "managed",
    "customUrl": "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
  }'
```

**Local Archive Installation**
```bash
curl -X POST http://127.0.0.1:5005/api/downloads/ffmpeg/install \
  -H "Content-Type: application/json" \
  -d '{
    "mode": "local",
    "localArchivePath": "C:\\Downloads\\ffmpeg-6.0-win64.zip"
  }'
```

**Attach Existing FFmpeg**
```bash
curl -X POST http://127.0.0.1:5005/api/downloads/ffmpeg/install \
  -H "Content-Type: application/json" \
  -d '{
    "mode": "attach",
    "attachPath": "C:\\ffmpeg\\bin\\ffmpeg.exe"
  }'
```

Or attach by directory:
```bash
curl -X POST http://127.0.0.1:5005/api/downloads/ffmpeg/install \
  -H "Content-Type: application/json" \
  -d '{
    "mode": "attach",
    "attachPath": "C:\\ffmpeg"
  }'
```

#### Success Response (200 OK)

```json
{
  "success": true,
  "installPath": "C:\\Users\\User\\AppData\\Local\\Aura\\Tools\\ffmpeg\\6.0",
  "ffmpegPath": "C:\\Users\\User\\AppData\\Local\\Aura\\Tools\\ffmpeg\\6.0\\bin\\ffmpeg.exe",
  "ffprobePath": "C:\\Users\\User\\AppData\\Local\\Aura\\Tools\\ffmpeg\\6.0\\bin\\ffprobe.exe",
  "validationOutput": "ffmpeg version 6.0 Copyright (c)...",
  "sourceType": "Network",
  "installedAt": "2024-10-12T18:45:00Z",
  "logPath": "C:\\Users\\User\\AppData\\Local\\Aura\\Logs\\Tools\\ffmpeg-install-20241012184500.log"
}
```

#### Error Response (400 Bad Request)

```json
{
  "success": false,
  "error": "FFmpeg installation failed: Could not find ffmpeg.exe in extracted archive",
  "code": "E302-FFMPEG_INSTALL_FAILED",
  "correlationId": "0HN8...",
  "howToFix": [
    "Try using a different mirror or custom URL",
    "Download FFmpeg manually and use 'Attach Existing' mode",
    "Check network connectivity and firewall settings",
    "Review install log for details"
  ],
  "logPath": "C:\\Users\\User\\AppData\\Local\\Aura\\Logs\\Tools\\ffmpeg-install-20241012184500.log"
}
```

### 2. Get FFmpeg Status

**GET** `/api/downloads/ffmpeg/status`

Get the current installation status of FFmpeg.

#### Response

```json
{
  "state": "Installed",  // "NotInstalled", "Installed", "PartiallyFailed", "ExternalAttached"
  "installPath": "C:\\Users\\User\\AppData\\Local\\Aura\\Tools\\ffmpeg\\6.0",
  "ffmpegPath": "C:\\Users\\User\\AppData\\Local\\Aura\\Tools\\ffmpeg\\6.0\\bin\\ffmpeg.exe",
  "ffprobePath": "C:\\Users\\User\\AppData\\Local\\Aura\\Tools\\ffmpeg\\6.0\\bin\\ffprobe.exe",
  "version": "6.0",
  "sourceType": "Network",
  "installedAt": "2024-10-12T18:45:00Z",
  "validated": true,
  "validationOutput": "ffmpeg version 6.0...",
  "lastError": null
}
```

### 3. Repair FFmpeg Installation

**POST** `/api/downloads/ffmpeg/repair`

Re-download and reinstall FFmpeg using the manifest mirrors.

#### Success Response (200 OK)

```json
{
  "success": true,
  "message": "FFmpeg repaired successfully",
  "ffmpegPath": "C:\\Users\\User\\AppData\\Local\\Aura\\Tools\\ffmpeg\\6.0\\bin\\ffmpeg.exe",
  "validationOutput": "ffmpeg version 6.0..."
}
```

### 4. Get Install Log

**GET** `/api/downloads/ffmpeg/install-log?lines=100`

Get the most recent FFmpeg installation log.

#### Query Parameters

- `lines` (optional): Number of lines to return from the end of the log (default: 100)

#### Response

```json
{
  "log": "[2024-10-12T18:45:00Z] Starting FFmpeg installation...\n[2024-10-12T18:45:05Z] Download complete\n...",
  "logPath": "C:\\Users\\User\\AppData\\Local\\Aura\\Logs\\Tools\\ffmpeg-install-20241012184500.log",
  "totalLines": 45
}
```

## Installation Metadata (`install.json`)

Each installation creates an `install.json` file in the installation directory:

```json
{
  "id": "ffmpeg",
  "version": "6.0",
  "installPath": "C:\\Users\\User\\AppData\\Local\\Aura\\Tools\\ffmpeg\\6.0",
  "ffmpegPath": "C:\\Users\\User\\AppData\\Local\\Aura\\Tools\\ffmpeg\\6.0\\bin\\ffmpeg.exe",
  "ffprobePath": "C:\\Users\\User\\AppData\\Local\\Aura\\Tools\\ffmpeg\\6.0\\bin\\ffprobe.exe",
  "sourceUrl": "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip",
  "sourceType": "Network",
  "sha256": null,
  "installedAt": "2024-10-12T18:45:00Z",
  "validated": true,
  "validationOutput": "ffmpeg version 6.0 Copyright (c) 2000-2024 the FFmpeg developers..."
}
```

## Mirror Fallback

The managed installation mode tries mirrors in this order:

1. Custom URL (if provided)
2. Primary URL from manifest (Gyan.dev)
3. Mirror URLs from manifest (BtbN, GitHub releases)
4. Hardcoded fallback (Gyan.dev essentials)

Each URL is retried up to 3 times with exponential backoff before moving to the next mirror.

## Validation

All installations are validated by:

1. Checking that `ffmpeg.exe` exists and is executable
2. Running `ffmpeg -version` and verifying exit code 0
3. Checking that output contains "ffmpeg version"

If validation fails, the installation is marked as failed and the target directory is cleaned up (for managed/local installs).

## Error Codes

- `E302-FFMPEG_INSTALL_FAILED` - Installation failed (download, extraction, or validation)
- `E302-FFMPEG_INSTALL_ERROR` - Unexpected error during installation
- `E302-FFMPEG_VALIDATION` - FFmpeg binary validation failed
- `E302-FFMPEG_REPAIR_FAILED` - Repair operation failed

## Logs

Installation logs are written to:
- `%LOCALAPPDATA%\Aura\Logs\Tools\ffmpeg-install-{timestamp}.log`

Render logs (when FFmpeg is used for video rendering) are written to:
- `%LOCALAPPDATA%\Aura\Logs\ffmpeg\{jobId}.log`

## Examples

### Complete Workflow

1. Check status:
```bash
curl http://127.0.0.1:5005/api/downloads/ffmpeg/status
```

2. If not installed, install:
```bash
curl -X POST http://127.0.0.1:5005/api/downloads/ffmpeg/install \
  -H "Content-Type: application/json" \
  -d '{"mode":"managed"}'
```

3. Check install log if needed:
```bash
curl http://127.0.0.1:5005/api/downloads/ffmpeg/install-log?lines=50
```

4. Verify status again:
```bash
curl http://127.0.0.1:5005/api/downloads/ffmpeg/status
```

### Attach Existing FFmpeg

If you already have FFmpeg installed (e.g., via Chocolatey or manual download):

```bash
# Find FFmpeg location
where ffmpeg

# Attach it
curl -X POST http://127.0.0.1:5005/api/downloads/ffmpeg/install \
  -H "Content-Type: application/json" \
  -d '{
    "mode": "attach",
    "attachPath": "C:\\ProgramData\\chocolatey\\bin\\ffmpeg.exe"
  }'
```

### Repair Corrupted Installation

```bash
curl -X POST http://127.0.0.1:5005/api/downloads/ffmpeg/repair
```

## Integration with FfmpegVideoComposer

When `FfmpegVideoComposer` is used for rendering:

1. It validates the FFmpeg binary at the start of each render job
2. Runs `ffmpeg -version` to ensure it's working
3. If validation fails, throws `E302-FFMPEG_VALIDATION` error with suggestions
4. During rendering, captures stdout/stderr for diagnostics
5. On crash (negative exit code), includes stderr snippet in error
6. Writes full logs to `%LOCALAPPDATA%\Aura\Logs\ffmpeg\{jobId}.log`

## Notes

- SHA256 verification is optional (set to null in manifest for dynamic builds)
- The installer handles nested folder structures in zip files automatically
- On attach, both `ffmpeg.exe` and `ffprobe.exe` are located if present
- CorrelationId is included in all API responses for tracing
- All timestamps are UTC
