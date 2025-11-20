# FFmpeg Detection and Configuration API

This document describes the API endpoints for detecting and configuring FFmpeg installations during the setup wizard.

## Overview

The setup wizard provides endpoints to:
1. Check if FFmpeg exists at a specific path and validate it
2. Save validated FFmpeg paths to persistent configuration

These endpoints complement the comprehensive FFmpeg management endpoints in `FFmpegController`.

## Endpoints

### POST /api/setup/check-ffmpeg

Validates that FFmpeg exists at the specified path and is executable.

#### Request

```json
{
  "path": "/path/to/ffmpeg"
}
```

**Parameters:**
- `path` (string, required): Absolute path to FFmpeg executable

#### Response - Success

```json
{
  "found": true,
  "path": "/usr/local/bin/ffmpeg",
  "version": "6.0.1",
  "correlationId": "abc123"
}
```

#### Response - Not Found

```json
{
  "found": false,
  "error": "File not found at: /invalid/path/ffmpeg",
  "correlationId": "abc123"
}
```

#### Response - Invalid

```json
{
  "found": false,
  "error": "FFmpeg validation failed with exit code 1",
  "correlationId": "abc123"
}
```

**HTTP Status:** Always returns 200 OK (even for not found cases)

#### Usage Example

```typescript
const checkFFmpeg = async (path: string) => {
  const response = await fetch('/api/setup/check-ffmpeg', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ path })
  });
  
  const data = await response.json();
  if (data.found) {
    console.log(`FFmpeg ${data.version} found at ${data.path}`);
  } else {
    console.error(`FFmpeg not found: ${data.error}`);
  }
};
```

---

### POST /api/setup/save-ffmpeg-path

Persists the FFmpeg path to configuration after validation.

#### Request

```json
{
  "path": "/usr/local/bin/ffmpeg"
}
```

**Parameters:**
- `path` (string, required): Absolute path to validated FFmpeg executable

#### Response - Success

```json
{
  "success": true,
  "path": "/usr/local/bin/ffmpeg",
  "correlationId": "abc123"
}
```

#### Response - Error

```json
{
  "success": false,
  "error": "File not found at: /invalid/path/ffmpeg",
  "correlationId": "abc123"
}
```

**HTTP Status:**
- 200 OK - Path saved successfully
- 400 Bad Request - Invalid or non-existent path
- 500 Internal Server Error - Configuration save failed

#### Configuration Details

When a path is saved:
- Mode is set to `FFmpegMode.Custom`
- Source is set to `"Configured"`
- Path is persisted to FFmpegConfigurationStore
- Configuration survives application restarts

#### Usage Example

```typescript
const saveFFmpegPath = async (path: string) => {
  const response = await fetch('/api/setup/save-ffmpeg-path', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ path })
  });
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.error || 'Failed to save FFmpeg path');
  }
  
  const data = await response.json();
  console.log(`FFmpeg path saved: ${data.path}`);
};
```

---

## Managed FFmpeg Detection

The setup wizard checks for managed FFmpeg installations in these locations (in order):

1. `./ffmpeg/bin/ffmpeg.exe` (Windows)
2. `./ffmpeg/bin/ffmpeg` (Unix/Linux/macOS)
3. `../ffmpeg/bin/ffmpeg.exe` (Electron app.asar.unpacked on Windows)
4. `../ffmpeg/bin/ffmpeg` (Electron app.asar.unpacked on Unix)

If none are found, it falls back to system PATH detection via `/api/health/ffmpeg`.

### Frontend Implementation

```typescript
const detectFFmpeg = async (): Promise<{ found: boolean; path: string; version: string }> => {
  // Check managed installation first
  const managedPaths = [
    './ffmpeg/bin/ffmpeg.exe',
    './ffmpeg/bin/ffmpeg',
    '../ffmpeg/bin/ffmpeg.exe',
    '../ffmpeg/bin/ffmpeg',
  ];

  for (const path of managedPaths) {
    try {
      const response = await fetch('/api/setup/check-ffmpeg', {
        method: 'POST',
        body: JSON.stringify({ path }),
        headers: { 'Content-Type': 'application/json' },
      });
      if (response.ok) {
        const data = await response.json();
        if (data.found) {
          return { found: true, path: data.path, version: data.version };
        }
      }
    } catch {
      // Continue to next path
    }
  }

  // Fallback to system PATH check
  try {
    const response = await fetch('/api/health/ffmpeg');
    const data = await response.json();
    if (data.isAvailable && data.path) {
      return { found: true, path: data.path, version: data.version || 'unknown' };
    }
  } catch {
    // No FFmpeg found
  }

  return { found: false, path: '', version: '' };
};
```

---

## Related Endpoints

For more comprehensive FFmpeg management, see:

- **GET /api/ffmpeg/status** - Get current FFmpeg status with validation details
- **POST /api/ffmpeg/detect** - Force re-detection with auto-persistence
- **POST /api/ffmpeg/use-existing** - Validate and configure custom FFmpeg path
- **POST /api/ffmpeg/install** - Install managed FFmpeg from trusted sources
- **POST /api/ffmpeg/rescan** - Rescan system for FFmpeg installations

---

## Error Handling

All endpoints include a `correlationId` field for tracing requests through logs. Include this in error reports.

### Common Errors

**Empty Path:**
```json
{
  "found": false,
  "error": "Path cannot be empty",
  "correlationId": "abc123"
}
```

**File Not Found:**
```json
{
  "found": false,
  "error": "File not found at: /path/to/ffmpeg",
  "correlationId": "abc123"
}
```

**Validation Failed:**
```json
{
  "found": false,
  "error": "FFmpeg validation failed with exit code 1",
  "correlationId": "abc123"
}
```

---

## Testing

Integration tests are available in `Aura.Tests/SetupControllerIntegrationTests.cs`:

- `CheckFFmpegPath_WithNonExistentPath_ReturnsNotFound`
- `CheckFFmpegPath_WithEmptyPath_ReturnsNotFound`
- `SaveFFmpegPath_WithEmptyPath_ReturnsBadRequest`
- `SaveFFmpegPath_WithNonExistentPath_ReturnsBadRequest`

Run tests:
```bash
dotnet test Aura.Tests/Aura.Tests.csproj --filter "FullyQualifiedName~SetupControllerIntegrationTests.CheckFFmpegPath"
dotnet test Aura.Tests/Aura.Tests.csproj --filter "FullyQualifiedName~SetupControllerIntegrationTests.SaveFFmpegPath"
```
