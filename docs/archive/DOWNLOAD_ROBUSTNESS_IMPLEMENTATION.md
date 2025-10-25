# Download Robustness & Visibility Implementation

## Overview
This document describes the implementation of download robustness features including mirror fallback, custom URL support, local file import, and installation location visibility.

## Features Implemented

### 1. Mirror Fallback with Exponential Backoff

**Problem:** 404 errors from primary download URLs break installations with no recovery mechanism.

**Solution:** 
- Added `mirrors` array to `EngineManifestEntry` model supporting platform-specific mirror URLs
- Updated `HttpDownloader.DownloadFileAsync()` to accept an array of URLs and automatically fallback to mirrors
- Implements exponential backoff between retries (2^attempt seconds)
- Distinguishes between recoverable errors (404, timeout) and non-recoverable errors (checksum mismatch)

**Usage:**
```json
{
  "id": "ffmpeg",
  "urls": {
    "windows": "https://primary.com/ffmpeg.zip"
  },
  "mirrors": {
    "windows": [
      "https://mirror1.com/ffmpeg.zip",
      "https://mirror2.com/ffmpeg.zip"
    ]
  }
}
```

**Error Codes:**
- `E-DL-404`: File not found (404) - triggers mirror fallback
- `E-DL-TIMEOUT`: Download timeout - triggers retry
- `E-DL-CHECKSUM`: Checksum verification failed - no retry
- `E-DL-NETWORK`: Generic network error - triggers retry
- `E-DL-IO`: I/O error - may retry

### 2. Custom URL Override

**Problem:** Users cannot install from alternative sources when official mirrors are down.

**Solution:**
- Added `customUrl` parameter to `InstallRequest` in API
- Added `customUrl` parameter to `EngineInstaller.InstallAsync()`
- When provided, bypasses manifest URLs and uses the custom URL directly
- Still performs checksum verification if available in manifest
- Records provenance with source type "CustomUrl"

**UI:**
- Install button changed to dropdown menu
- Option: "Official Mirrors" (default)
- Option: "Custom URL..." - opens dialog for URL input
- Shows warning about using trusted sources

**API Usage:**
```bash
curl -X POST http://localhost:5005/api/engines/install \
  -H "Content-Type: application/json" \
  -d '{"engineId": "ffmpeg", "customUrl": "https://custom.com/ffmpeg.zip"}'
```

### 3. Local File Import

**Problem:** Users who already downloaded archives cannot use them for installation.

**Solution:**
- Added `ImportLocalFileAsync()` method to `HttpDownloader`
- Accepts local file path, copies file, and verifies checksum
- On checksum mismatch, still imports but returns warning
- Added `localFilePath` parameter to `InstallRequest` in API
- Records provenance with source type "LocalFile"

**Features:**
- Computes SHA256 of local file
- Compares with expected checksum if provided
- Continues with warning if checksum doesn't match (user choice)
- Progress reporting during copy operation

**UI:**
- Option: "Install from Local File..." - opens dialog for path input
- Accepts absolute file paths
- Shows warning about checksum verification

**API Usage:**
```bash
curl -X POST http://localhost:5005/api/engines/install \
  -H "Content-Type: application/json" \
  -d '{"engineId": "ffmpeg", "localFilePath": "C:\\Downloads\\ffmpeg.zip"}'
```

### 4. Installation Provenance

**Problem:** No record of where engines were installed from or when.

**Solution:**
- Added `InstallProvenance` model
- After successful installation, writes `install.json` to engine directory
- Records: engineId, version, installedAt, installPath, source, url, sha256, mirrorIndex

**Example `install.json`:**
```json
{
  "engineId": "ffmpeg",
  "version": "6.0",
  "installedAt": "2025-10-12T14:30:00Z",
  "installPath": "C:\\Program Files\\Aura\\engines\\ffmpeg",
  "source": "Mirror",
  "url": "https://mirror1.com/ffmpeg.zip",
  "sha256": "e25bfb9fc6986e5e42b0bcff64c20433171125243c5ebde1bbee29a4637434a9",
  "mirrorIndex": 1
}
```

### 5. Install Location Visibility

**Problem:** Users don't know where engines are installed or how to access them.

**Solution:**
- Added `installPath` to `EngineStatus` API response
- Updated UI to prominently display install location when engine is installed
- Added "Copy Path" button to copy path to clipboard
- Enhanced "Open Folder" button to use API endpoint that opens file explorer
- Install location shown in expandable panel with monospace font

**UI Components:**
```typescript
// Install Location Display (shown when installed)
<div>
  <Text weight="semibold">Install Location:</Text>
  <Text style={{ fontFamily: 'monospace' }}>
    C:\Program Files\Aura\engines\ffmpeg
  </Text>
  <Button onClick={copyPath}>Copy Path</Button>
  <Button onClick={openFolder}>Open Folder</Button>
</div>
```

**API Endpoint:**
```bash
# Opens the engine folder in system file explorer
POST /api/engines/open-folder
{
  "engineId": "ffmpeg"
}
```

### 6. Enhanced Error Handling

**Error Flow:**
1. Try primary URL
2. On 404 → Try next mirror immediately (no retry on 404)
3. On timeout/network error → Retry with exponential backoff (up to 3 attempts)
4. If all mirrors exhausted → Throw `DownloadException` with error code
5. On checksum mismatch → Throw `E-DL-CHECKSUM` immediately (no retry)

**UI Error Handling:**
- Displays error code and user-friendly message
- Suggests alternatives: "Try a custom URL" or "Install from local file"
- Shows which mirror was being used when error occurred
- Provides link to diagnostics for detailed troubleshooting

### 7. Progress Reporting Enhancement

**New Fields in `HttpDownloadProgress`:**
- `CurrentUrl`: Shows which URL is currently being downloaded from
- `MirrorIndex`: Index of current mirror (0 = primary, 1+ = mirrors)

**UI Updates:**
- Shows "[Mirror 2]" prefix when downloading from mirror
- Updates in real-time as fallback occurs
- Clear indication of which source succeeded

## Testing

### Unit Tests (13 tests in `HttpDownloaderTests`)

**Mirror Fallback:**
- ✅ `DownloadFileAsync_Should_FallbackToMirror_When404`
- ✅ `DownloadFileAsync_Should_ThrowDownloadException_WhenAllMirrorsFail`

**Checksum Verification:**
- ✅ `DownloadFileAsync_Should_VerifyChecksum_WhenProvided`
- ✅ `DownloadFileAsync_Should_ThrowChecksumError_WhenVerificationFails`

**Local File Import:**
- ✅ `ImportLocalFileAsync_Should_ImportFile_Successfully`
- ✅ `ImportLocalFileAsync_Should_StillImportOnChecksumMismatch`
- ✅ `ImportLocalFileAsync_Should_ThrowFileNotFound_WhenFileDoesNotExist`

**Retry Logic:**
- ✅ `DownloadFileAsync_Should_RetryOnFailure`
- ✅ `DownloadFileAsync_Should_ResumeDownload_WhenPartialFileExists`

**Error Codes:**
- ✅ `DownloadFileAsync_Should_ThrowChecksumError_OnMismatch`

## Usage Examples

### Install with Mirror Fallback
```bash
# Automatically tries mirrors if primary fails
curl -X POST http://localhost:5005/api/engines/install \
  -H "Content-Type: application/json" \
  -d '{"engineId": "ffmpeg"}'
```

### Install from Custom URL
```bash
curl -X POST http://localhost:5005/api/engines/install \
  -H "Content-Type: application/json" \
  -d '{
    "engineId": "ffmpeg",
    "customUrl": "https://my-mirror.com/ffmpeg-6.0.zip"
  }'
```

### Install from Local File
```bash
curl -X POST http://localhost:5005/api/engines/install \
  -H "Content-Type: application/json" \
  -d '{
    "engineId": "ffmpeg",
    "localFilePath": "/home/user/downloads/ffmpeg-6.0.zip"
  }'
```

### Get Install Location
```bash
curl http://localhost:5005/api/engines/status?engineId=ffmpeg
# Response includes: "installPath": "/opt/aura/engines/ffmpeg"
```

### Open Install Folder
```bash
curl -X POST http://localhost:5005/api/engines/open-folder \
  -H "Content-Type: application/json" \
  -d '{"engineId": "ffmpeg"}'
```

## Architecture

### Backend Components

**HttpDownloader** (`Aura.Core/Downloads/HttpDownloader.cs`)
- Multi-URL download with fallback
- Exponential backoff retry logic
- Checksum verification with error codes
- Local file import
- Resume support for partial downloads

**EngineInstaller** (`Aura.Core/Downloads/EngineInstaller.cs`)
- Orchestrates download from manifest, custom URL, or local file
- Writes provenance file after installation
- Returns install path for UI display

**EngineManifest** (`Aura.Core/Downloads/EngineManifest.cs`)
- Added `Mirrors` dictionary for platform-specific mirror URLs
- Added `InstallProvenance` class for tracking installation metadata

**EnginesController** (`Aura.Api/Controllers/EnginesController.cs`)
- Extended `InstallRequest` with `CustomUrl` and `LocalFilePath`
- Enhanced error handling with `DownloadException` error codes
- Returns `installPath` in status responses
- Implements `open-folder` endpoint

### Frontend Components

**EngineCard** (`Aura.Web/src/components/Engines/EngineCard.tsx`)
- Install dropdown menu with three options
- Custom URL dialog with input validation
- Local file path dialog
- Install location display panel
- Copy path and open folder buttons

**Types** (`Aura.Web/src/types/engines.ts`)
- Added `installPath` to `EngineStatus` interface

## Configuration

### Manifest Updates
Update `engine_manifest.json` to include mirrors:

```json
{
  "engines": [
    {
      "id": "ffmpeg",
      "urls": {
        "windows": "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-n6.0-latest-win64-gpl-6.0.zip"
      },
      "mirrors": {
        "windows": [
          "https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2024-01-01-12-55/ffmpeg-n6.0-latest-win64-gpl-6.0.zip"
        ]
      }
    }
  ]
}
```

## Security Considerations

1. **Custom URLs**: Users are warned to only use trusted sources
2. **Checksum Verification**: Always performed when available, even for custom URLs
3. **Local Files**: Checksum verified but installation continues with warning if mismatch
4. **Provenance**: Tracks installation source for audit purposes
5. **Path Validation**: File paths validated before opening in explorer

## Future Enhancements

- [ ] Add file picker dialog for local file import (browser limitation workaround)
- [ ] Implement manifest refresh from remote source
- [ ] Add mirror health check/ranking
- [ ] Support for torrent/IPFS as additional fallback
- [ ] Automatic mirror discovery from CDN headers
- [ ] Installation analytics (anonymous) to identify best mirrors

## Acceptance Criteria - Complete

✅ 404s are recoverable via mirrors/custom/local file  
✅ UI visibly shows where installs live  
✅ UI allows opening install folders  
✅ Provenance recorded for all installations  
✅ Error codes surfaced to UI with actionable suggestions  
✅ Checksum verification with warnings  
✅ Progress shows current mirror in use  
✅ Comprehensive unit tests  
✅ API supports all installation methods  

## Related Files

**Backend:**
- `Aura.Core/Downloads/HttpDownloader.cs` - Core download logic
- `Aura.Core/Downloads/EngineInstaller.cs` - Installation orchestration
- `Aura.Core/Downloads/EngineManifest.cs` - Data models
- `Aura.Api/Controllers/EnginesController.cs` - API endpoints

**Frontend:**
- `Aura.Web/src/components/Engines/EngineCard.tsx` - UI component
- `Aura.Web/src/types/engines.ts` - TypeScript types

**Tests:**
- `Aura.Tests/HttpDownloaderTests.cs` - Unit tests

**Configuration:**
- `Aura.Core/Downloads/engine_manifest.json` - Engine manifest with mirrors
