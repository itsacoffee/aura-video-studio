# Bulletproof Video and Dependencies Implementation

This document describes the implementation of reliable video generation and dependency management for Aura Video Studio.

## Overview

The implementation ensures that video generation either produces a playable MP4 or displays a single error panel with clear resolution steps. All errors include actionable "Fix it" buttons that resolve issues without code edits.

## Core Components

### 1. WavFileWriter - Atomic Audio Generation

**Location:** `Aura.Core/Audio/WavFileWriter.cs`

**Purpose:** Ensures no zero-byte or corrupted WAV files are ever created.

**Features:**
- Atomic writes using `.part` files
- RIFF header validation
- Silent WAV generation for fallback
- Minimum file size guarantees (> 128 bytes)

**Key Methods:**
```csharp
// Write audio with atomic operation
await WriteAsync(outputPath, audioData, sampleRate, channels, bitsPerSample, ct);

// Generate silent fallback
await GenerateSilenceAsync(outputPath, durationSeconds, ct: ct);

// Validate WAV structure
bool isValid = ValidateWavFile(filePath);
```

**Tests:** `Aura.Tests/WavFileWriterTests.cs` (11 tests)

### 2. TtsFallbackService - Robust TTS Chain

**Location:** `Aura.Core/Providers/TtsFallbackService.cs`

**Purpose:** Implements fallback chain: requested voice → alternate voices → silent WAV.

**Features:**
- Never returns null or zero-byte files
- Validates output after each attempt
- Attempts repair of corrupted files
- Provides detailed diagnostics

**Fallback Flow:**
1. Try requested voice
2. If failed, try up to 2 alternate voices
3. If all fail, generate valid silent WAV
4. Return TtsFallbackResult with diagnostics

**Usage:**
```csharp
var result = await ttsFallbackService.SynthesizeWithFallbackAsync(
    provider, lines, voiceSpec, totalDuration, ct);

// result.UsedFallback indicates if fallback was used
// result.FallbackReason contains explanation
// result.Diagnostics has full trace
```

**Tests:** `Aura.Tests/TtsFallbackServiceTests.cs` (7 tests)

### 3. FFmpeg Validation & Locator

**Location:** `Aura.Core/Dependencies/FfmpegLocator.cs`

**Enhanced Features:**
- x264 capability detection
- Source tracking (Portable, Attached, PATH, Missing)
- Version string extraction with tolerance
- Detailed diagnostics array

**Precedence:**
1. Portable: `artifacts/portable/build/Tools/ffmpeg/`
2. Attached: User-specified path (persisted)
3. PATH: System-wide FFmpeg

**Validation:**
```csharp
var result = await ffmpegLocator.CheckAllCandidatesAsync(configuredPath, ct);
// result.Found, result.FfmpegPath, result.HasX264, result.Source
```

**Error Codes:**
- E302-FFMPEG_NOT_FOUND: No FFmpeg found in any location
- E302-FFMPEG_VALIDATION: FFmpeg binary failed validation
- E304-FFMPEG_RUNTIME: FFmpeg execution error

### 4. Render Pipeline - Pre-validation & Error Logging

**Location:** `Aura.Providers/Video/FfmpegVideoComposer.cs`

**Pre-validation Steps:**
1. Resolve FFmpeg path (fail fast if not found)
2. Validate FFmpeg binary (run `-version`)
3. Pre-validate narration WAV (> 128 bytes, valid RIFF)
4. Pre-validate music WAV if present
5. Attempt re-encode if corrupted
6. Generate silent fallback if re-encode fails

**Error Logging:**
- Full FFmpeg stderr written to: `%LOCALAPPDATA%\Aura\Logs\ffmpeg\{jobId}.log`
- Inline error includes last 64KB of stderr
- Error includes correlationId for tracking
- Suggested actions based on error patterns

**Error Response Format:**
```json
{
  "code": "E304-FFMPEG_RUNTIME",
  "message": "FFmpeg failed during render (exit code: 1)",
  "exitCode": 1,
  "stderrSnippet": "...",
  "jobId": "abc123",
  "correlationId": "xyz789",
  "suggestedActions": [
    "Review FFmpeg log for details",
    "Try with different render settings"
  ],
  "logFile": "%LOCALAPPDATA%\\Aura\\Logs\\ffmpeg\\abc123.log"
}
```

## Error Codes Reference

### FFmpeg Errors (E302-E304)

| Code | Title | Description | How to Fix |
|------|-------|-------------|------------|
| E302-FFMPEG_NOT_FOUND | FFmpeg Not Found | FFmpeg binary not found in any location | Install via Download Center, Attach existing, or add to PATH |
| E302-FFMPEG_VALIDATION | FFmpeg Validation Failed | FFmpeg binary found but validation failed | Reinstall FFmpeg or repair installation |
| E304-FFMPEG_RUNTIME | FFmpeg Runtime Error | FFmpeg execution failed during render | Check logs, verify input files, try different settings |

### Download Center Errors

| Code | Title | Description | How to Fix |
|------|-------|-------------|------------|
| E302-FFMPEG_INSTALL_FAILED | Installation Failed | Failed to download or extract FFmpeg | Check internet connection, try mirror, or attach existing |
| E302-FFMPEG_REPAIR_FAILED | Repair Failed | Failed to repair corrupted installation | Remove and reinstall |
| E302-INVALID_PATH | Invalid Path | Attached path does not contain FFmpeg | Choose valid FFmpeg directory or binary |
| E302-ATTACH_FAILED | Attach Failed | Failed to attach existing FFmpeg | Verify path permissions and binary validity |

## Portable-Only Structure

All dependencies are stored under a single portable directory:

```
artifacts/
  portable/
    build/
      Tools/
        ffmpeg/
          {version}/
            bin/
              ffmpeg.exe
              ffprobe.exe
        python/
          {version}/
            python.exe
        stable-diffusion/
          {version}/
            models/
            venv/
```

**Benefits:**
- Single directory for all tools
- No system-wide installations
- Easy backup and transfer
- Version isolation

## Download Center Operations

### Install

**Flow:**
1. Check HEAD request for download URL
2. Download with mirror fallback
3. Atomic download (`.part` file)
4. Extract to portable location
5. Verify installation
6. Update registry/config

**Manifest Format:**
```json
{
  "id": "ffmpeg",
  "version": "6.0",
  "urls": [
    "https://primary.example.com/ffmpeg-6.0.zip",
    "https://mirror.example.com/ffmpeg-6.0.zip"
  ],
  "sha256": "abc123...",
  "extractTo": "Tools/ffmpeg/6.0"
}
```

### Attach

**Flow:**
1. User selects path (file or directory)
2. Validate path contains FFmpeg binary
3. Run validation (`ffmpeg -version`)
4. Persist path to config
5. Update UI status

**Validation:**
- If file: must be executable FFmpeg
- If directory: look for `ffmpeg.exe` or `bin/ffmpeg.exe`
- Must pass version check
- Must have required codecs (x264 preferred)

### Rescan

**Flow:**
1. Re-run locator for all dependencies
2. Update status objects
3. Refresh UI
4. Show diagnostics if any issues found

**Status Object:**
```typescript
interface DependencyStatus {
  found: boolean;
  path?: string;
  versionDisplay?: string;
  source: 'Portable' | 'Attached' | 'PATH' | 'Missing';
  diagnostics: string[];
  hasX264?: boolean; // FFmpeg specific
}
```

## UI Error Panel

**Single Standard Panel for All Errors:**

```
┌─────────────────────────────────────────┐
│ ⚠️ FFmpeg Runtime Error                 │
│                                         │
│ FFmpeg failed during render (exit 1)   │
│                                         │
│ How to Fix:                            │
│ • Review FFmpeg log for details        │
│ • Try with different render settings   │
│                                         │
│ Correlation ID: xyz789                 │
│                                         │
│ [Open Logs]  [Fix it]  [Dismiss]      │
└─────────────────────────────────────────┘
```

**"Fix it" Button Behavior:**
- E302 errors: Deep-link to Download Center
- E304 errors: Open FFmpeg log file
- TTS errors: Deep-link to TTS provider settings

## Stable Diffusion + Python

### Python Version Validation

**Preferred Versions:** 3.10, 3.11 (best PyTorch compatibility)

**Detection:**
```csharp
var pythonVersion = await GetPythonVersion(pythonPath);
if (pythonVersion.Major == 3 && pythonVersion.Minor >= 13)
{
    diagnostics.Add("Python 3.13+ detected. PyTorch may have limited support. Python 3.10 or 3.11 recommended.");
}
```

### Stable Diffusion Health Check

**Attach Existing Installation:**
1. User selects SD folder or venv
2. Check for required files:
   - `venv/Scripts/python.exe` (or `bin/python`)
   - `models/` directory
   - Required Python packages
3. Run health ping (optional import test)
4. Store path and mark as validated

## Testing

### Test Coverage

| Component | Tests | Coverage |
|-----------|-------|----------|
| WavFileWriter | 11 | Atomic writes, validation, silence generation |
| TtsFallbackService | 7 | Fallback chain, error handling, diagnostics |
| FFmpegLocator | 10 | Precedence, validation, version parsing |
| AudioValidator | Existing | File validation, re-encoding, fallback |
| FfmpegVideoComposer | Existing | Pre-validation, error logging |

### Key Test Scenarios

1. **Zero-byte WAV Prevention**
   - WavFileWriter never creates zero-byte files
   - Atomic writes ensure partial files are cleaned up
   - Validation rejects files < 128 bytes

2. **TTS Fallback Chain**
   - Primary voice fails → alternate voice succeeds
   - All voices fail → silent WAV generated
   - Zero-byte TTS output → repaired with silence

3. **FFmpeg Precedence**
   - Portable found → use Portable
   - Portable missing, Attached found → use Attached
   - Both missing, PATH found → use PATH
   - All missing → clear error with Fix it button

4. **Render Pre-validation**
   - Invalid narration → re-encode attempt
   - Re-encode fails → silent fallback
   - Render proceeds with valid audio

## Acceptance Criteria

✅ **Fresh Portable Unzip:**
- Success: MP4 plays correctly
- OR: Error panel with clear resolution steps
- All buttons functional

✅ **Accurate UI:**
- Versions displayed correctly (not hardcoded "6.0")
- Paths are copyable
- URLs are copyable
- Source shown (Portable/Attached/PATH)

✅ **Download Center:**
- Install: Downloads, extracts, verifies atomically
- Attach: Validates and persists user path
- Rescan: Updates all statuses
- All operations provide clear feedback

✅ **No Zero-byte Files:**
- TTS never produces zero-byte WAV
- Atomic writes prevent partial files
- Fallback chain ensures valid output

✅ **Error Handling:**
- Single error panel design
- Actionable "Fix it" buttons
- Correlation IDs for tracking
- Log files for detailed diagnostics

## Future Enhancements (Out of Scope)

The following are explicitly out of scope for this PR:
- Automatic FFmpeg installation on first run
- Multiple mirror CDNs with automatic failover
- Background dependency updates
- Telemetry for download success rates
- Auto-repair for corrupted installations

## Implementation Summary

This implementation ensures:
1. **Reliability:** Videos either render successfully or fail with clear guidance
2. **Atomic Operations:** No partial or corrupted files
3. **Robust Fallbacks:** Silent audio preferred over failure
4. **Clear Errors:** Every error has actionable resolution steps
5. **Portable-First:** All tools in single directory structure
6. **Testability:** Comprehensive test coverage for all components

All acceptance criteria are met without any FUTURE/TODO comments remaining in the code.
