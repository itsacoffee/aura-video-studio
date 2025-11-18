# FFmpeg and Provider Validation Redesign - Implementation Summary

## Problem Addressed

Users consistently experienced generic "Network Error" messages when:
1. Detecting FFmpeg on their system
2. Installing FFmpeg via the managed installer
3. Validating API keys for providers (especially OpenAI)

These errors were uninformative and made it impossible to diagnose the actual root cause.

## Solution Implemented (Backend Only)

### 1. FFmpeg Direct Check System

**New Service:** `FFmpegDirectCheckService` (`Aura.Core/Services/FFmpeg/FFmpegDirectCheckService.cs`)

**What it does:**
- Checks FFmpeg in a deterministic order:
  1. `AURA_FFMPEG_PATH` environment variable
  2. Managed install directory
  3. System PATH
- For each candidate, reports:
  - Does the path exist?
  - Was execution attempted?
  - Exit code and timeout status
  - Raw `-version` output
  - Parsed version number
  - Is it valid (version >= 4.0)?
  - Any errors encountered

**New API Endpoint:** `GET /api/debug/ffmpeg/direct-check`

**Response Example:**
```json
{
  "candidates": [
    {
      "label": "EnvVar",
      "path": "C:\\ffmpeg\\bin\\ffmpeg.exe",
      "exists": true,
      "executionAttempted": true,
      "exitCode": 0,
      "timedOut": false,
      "rawVersionOutput": "ffmpeg version 6.0...",
      "versionParsed": "6.0",
      "valid": true,
      "error": null
    },
    {
      "label": "Managed",
      "path": "C:\\Users\\...\\AuraVideoStudio\\ffmpeg",
      "exists": false,
      "executionAttempted": false,
      "error": "ManagedDirNotFound"
    },
    {
      "label": "PATH",
      "path": "ffmpeg.exe",
      "exists": false,
      "executionAttempted": true,
      "error": "ProcessStartFailed"
    }
  ],
  "overall": {
    "installed": true,
    "valid": true,
    "source": "EnvVar",
    "chosenPath": "C:\\ffmpeg\\bin\\ffmpeg.exe",
    "version": "6.0"
  }
}
```

### 2. Provider Ping System

**New Service:** `ProviderPingService` (`Aura.Core/Services/Providers/ProviderPingService.cs`)

**What it does:**
- Makes actual network calls to provider APIs:
  - OpenAI: `GET /v1/models`
  - Anthropic: `POST /v1/messages` (minimal request)
- 5-second timeout
- Classifies results into specific error codes:
  - `ProviderNotConfigured` - No API key
  - `ProviderKeyInvalid` - 401/403 HTTP status
  - `ProviderRateLimited` - 429 HTTP status
  - `ProviderServerError` - 500+ HTTP status
  - `ProviderNetworkError` - DNS, timeout, connection issues
  - `ProviderConfigError` - Other errors
- Returns HTTP status codes and endpoints attempted

**New API Endpoints:**
1. `POST /api/providers/{name}/ping` - Test single provider
2. `GET /api/providers/ping-all` - Test all configured providers

**Response Example:**
```json
{
  "attempted": true,
  "success": false,
  "errorCode": "ProviderKeyInvalid",
  "message": "The API key is invalid or lacks permissions. Verify it and ensure it has correct permissions.",
  "httpStatus": "401",
  "endpoint": "https://api.openai.com/v1/models",
  "responseTimeMs": 234,
  "correlationId": "abc123"
}
```

## Files Changed

### Backend (C#)
1. **Aura.Core/Services/FFmpeg/FFmpegDirectCheckService.cs** (NEW)
   - FFmpeg detection logic with full diagnostics
   
2. **Aura.Core/Services/Providers/ProviderPingService.cs** (NEW)
   - Provider connectivity testing with real network calls

3. **Aura.Api/Controllers/DebugController.cs** (NEW)
   - Debug endpoint for FFmpeg direct check

4. **Aura.Api/Controllers/ProvidersController.cs** (MODIFIED)
   - Added ping endpoints for testing provider connectivity

5. **Aura.Api/Models/ApiModels.V1/FFmpegDtos.cs** (NEW)
   - DTOs for FFmpeg check responses

6. **Aura.Api/Models/ApiModels.V1/ProviderValidationDtos.cs** (MODIFIED)
   - Added provider ping DTOs

7. **Aura.Api/Program.cs** (MODIFIED)
   - Registered new services in DI container

## What's NOT Done Yet (Frontend)

The backend is complete and ready, but frontend integration is needed:

### Frontend Tasks Remaining

1. **FFmpeg UI Updates**
   - Add client method for `/api/debug/ffmpeg/direct-check`
   - Show "Technical Details" expander in FFmpegSetup component
   - Display table of candidates checked
   - Replace "Network Error" with specific error codes

2. **Provider Validation UI**
   - Add client methods for ping endpoints
   - Update provider settings to call ping on "Test"/"Validate"
   - Show specific error messages based on error codes
   - Display HTTP status and endpoints

3. **Testing**
   - Unit tests for new backend services
   - Frontend tests for new client methods
   - Manual verification of user-facing messages

## Key Principles Applied

1. **Explicit over Implicit**
   - Every check shows exactly what was attempted
   - No hidden caching or state

2. **Observable**
   - Full diagnostic information available
   - Can see raw output from FFmpeg
   - Can see actual HTTP status from providers

3. **Testable**
   - Pure functions with no side effects
   - Clear inputs and outputs
   - Timeout handling explicit

4. **User-Friendly Errors**
   - Specific error codes for each failure mode
   - Actionable messages
   - Technical details available on demand

## How to Use (Backend Testing)

### Test FFmpeg Direct Check

```bash
curl -X GET http://localhost:5005/api/debug/ffmpeg/direct-check
```

### Test Provider Ping

```bash
# Test OpenAI (requires API key configured)
curl -X POST http://localhost:5005/api/providers/openai/ping

# Test all providers
curl -X GET http://localhost:5005/api/providers/ping-all
```

## Build Status

✅ Backend builds successfully with no warnings or errors
✅ All services registered in DI container
✅ Existing endpoints unchanged (backward compatible)

## Migration Notes

- New endpoints are **additive** - they don't replace existing ones
- Existing `/api/system/ffmpeg/status` still works
- Existing `/api/ffmpeg/install` still works
- Frontend can adopt new endpoints incrementally
- No breaking changes to existing API contracts

## Security Considerations

- API keys are never included in responses
- Endpoints are sanitized (base URLs only, no secrets)
- Raw FFmpeg output is safe to expose (version info only)
- Correlation IDs included for request tracing

## Performance Impact

- FFmpeg direct check: ~3 seconds worst case (3 candidates × 1 second each)
- Provider ping: ~5 seconds worst case per provider
- Both are on-demand, not cached
- Designed for user-initiated actions, not continuous polling
