# Configuration System API Requirements

## Overview
This document outlines the backend API endpoints required to support the Welcome Page Configuration System implemented in PR #1.

## Required Endpoints

### 1. Configuration Status Endpoint

**GET** `/api/setup/configuration-status`

Returns the overall configuration status of the system.

**Response:**
```json
{
  "isConfigured": true,
  "lastChecked": "2025-11-10T12:34:56Z",
  "checks": {
    "providerConfigured": true,
    "providerValidated": true,
    "workspaceCreated": true,
    "ffmpegDetected": true,
    "apiKeysValid": true
  },
  "details": {
    "configuredProviders": ["openai", "elevenlabs"],
    "ffmpegPath": "/usr/bin/ffmpeg",
    "ffmpegVersion": "6.0",
    "workspacePath": "/home/user/Aura/workspace",
    "diskSpaceAvailable": 256.5,
    "gpuAvailable": true
  },
  "issues": [
    {
      "severity": "warning",
      "code": "DISK_SPACE_LOW",
      "message": "Disk space is running low. Consider freeing up space.",
      "actionLabel": "View Storage",
      "actionUrl": "/settings/storage"
    }
  ]
}
```

**Status Codes:**
- 200: Success
- 404: Configuration not initialized (returns default empty status)
- 500: Server error

---

### 2. System Check Endpoint

**GET** `/api/health/system-check`

Runs comprehensive system checks including FFmpeg, disk space, GPU, and providers.

**Response:**
```json
{
  "ffmpeg": {
    "installed": true,
    "version": "6.0",
    "path": "/usr/bin/ffmpeg",
    "error": null
  },
  "diskSpace": {
    "available": 256.5,
    "total": 512.0,
    "unit": "GB",
    "sufficient": true
  },
  "gpu": {
    "available": true,
    "name": "NVIDIA GeForce RTX 3080",
    "vramGB": 10
  },
  "providers": {
    "configured": ["openai", "anthropic", "elevenlabs"],
    "validated": ["openai", "elevenlabs"],
    "errors": {
      "anthropic": "Invalid API key"
    }
  }
}
```

**Status Codes:**
- 200: Success
- 500: Server error

---

### 3. FFmpeg Status Endpoint

**GET** `/api/ffmpeg/status`

Returns the current FFmpeg installation status.

**Response:**
```json
{
  "installed": true,
  "valid": true,
  "version": "6.0",
  "path": "/usr/bin/ffmpeg",
  "error": null
}
```

**Status Codes:**
- 200: Success
- 500: Server error

---

### 4. Test All Providers Endpoint

**POST** `/api/provider-profiles/test-all`

Tests all configured providers and returns their connection status.

**Request:**
```json
{}
```

**Response:**
```json
{
  "openai": {
    "success": true,
    "message": "Connection successful",
    "responseTimeMs": 245
  },
  "anthropic": {
    "success": false,
    "message": "Invalid API key",
    "responseTimeMs": 0
  },
  "elevenlabs": {
    "success": true,
    "message": "Connection successful",
    "responseTimeMs": 312
  }
}
```

**Status Codes:**
- 200: Success (even if some providers fail)
- 500: Server error

---

### 5. Disk Space Check Endpoint

**GET** `/api/system/disk-space`

Checks available disk space for a given path.

**Query Parameters:**
- `path` (optional): The path to check. If not provided, checks the default workspace location.

**Response:**
```json
{
  "available": 256.5,
  "total": 512.0,
  "unit": "GB",
  "sufficient": true
}
```

**Status Codes:**
- 200: Success
- 400: Invalid path
- 500: Server error

---

### 6. Complete Setup Endpoint

**POST** `/api/setup/wizard/complete`

Marks the setup wizard as complete and saves configuration to database.

**Request:**
```json
{
  "version": "1.0.0",
  "selectedTier": "premium",
  "lastStep": 6
}
```

**Response:**
```json
{
  "success": true,
  "message": "Setup completed successfully",
  "errors": []
}
```

**Status Codes:**
- 200: Success
- 400: Invalid request
- 500: Server error

---

### 7. Check Directory Endpoint

**POST** `/api/setup/check-directory`

Validates if a directory path is valid and writable.

**Request:**
```json
{
  "path": "/home/user/Aura/workspace"
}
```

**Response:**
```json
{
  "isValid": true,
  "exists": true,
  "writable": true,
  "error": null
}
```

**Status Codes:**
- 200: Success
- 400: Invalid request
- 500: Server error

---

## Implementation Notes

### Error Handling
All endpoints should follow consistent error response format:

```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message",
    "details": {}
  }
}
```

### Performance
- Configuration status should be cached for at least 30 seconds
- System checks may take 2-5 seconds to complete
- Provider tests should timeout after 10 seconds per provider

### Security
- API key values should never be returned in plain text
- Mask sensitive information (show only first/last 4 characters)
- Validate all file paths to prevent directory traversal attacks

### Database Schema
The setup wizard completion should be persisted with:
- User ID (if multi-user)
- Completion timestamp
- Configuration version
- Selected tier
- Last completed step

## Frontend Integration

### Configuration Status Service
The frontend `configurationStatusService` handles:
- Caching of configuration status
- Auto-refresh every 30 seconds
- Event subscription for real-time updates
- Graceful fallback if endpoints don't exist

### Error Handling
If any endpoint returns 404, the service will:
1. Build status from individual component checks
2. Use fallback values for missing data
3. Log warnings but continue operation

## Testing Requirements

### Unit Tests
- Test each endpoint with valid data
- Test error cases (invalid paths, missing config, etc.)
- Test timeout scenarios for provider tests

### Integration Tests
- Test complete configuration flow end-to-end
- Test partial configuration states
- Test reconfiguration scenarios

### Performance Tests
- System check should complete in < 5 seconds
- Configuration status should return in < 500ms (cached)
- Provider tests should timeout properly

## Migration Path

For systems without these endpoints:
1. Frontend will use fallback implementation
2. Status is built from existing endpoints
3. Some features (like disk space check) may not work
4. User experience degrades gracefully

## Future Enhancements

1. **Real-time Updates**: WebSocket support for live status updates
2. **Health Scores**: Numeric health score (0-100) for overall system health
3. **Recommendations**: AI-powered configuration recommendations
4. **Auto-fix**: Automated fixing of common configuration issues
5. **Telemetry**: Anonymous usage statistics for improvement
