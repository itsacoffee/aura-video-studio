# FFmpeg Environment Variable Wiring Implementation

## Summary

This implementation wires the FFmpeg environment variable and detection to ensure the backend can find managed FFmpeg installations.

## Changes Made

### 1. Aura.Desktop/electron/backend-service.js

**Location**: `_prepareEnvironment()` method (lines 906-951)

**Changes**:
- Added platform-aware FFmpeg path detection
- Checks multiple candidate paths in order:
  1. Managed FFmpeg in resources (installed mode): `process.resourcesPath/ffmpeg/{platform}/bin`
  2. Development mode FFmpeg: `app.getAppPath()/../Aura.Desktop/resources/ffmpeg/{platform}/bin`
  3. Bundled FFmpeg in app directory: `app.getAppPath()/resources/ffmpeg/{platform}/bin`
- Only sets `FFMPEG_PATH` environment variable if FFmpeg executable is found
- Adds informative logging for FFmpeg detection
- Uses conditional spread operator to avoid setting empty environment variables

**Platform Support**:
- Windows: `win-x64`
- macOS: `osx-x64`
- Linux: `linux-x64`

### 2. Aura.Core/Services/Setup/FFmpegDetectionService.cs

**Location**: `FindFFmpegPathAsync()` method (lines 196-211)

**Changes**:
- Added managed FFmpeg location checks after FFMPEG_PATH environment variable check
- Checks two LocalApplicationData paths:
  1. `%LocalAppData%\Aura\Tools\ffmpeg\win-x64\bin\ffmpeg.exe`
  2. `%LocalAppData%\Programs\Aura Video Studio\resources\ffmpeg\win-x64\bin\ffmpeg.exe`
- Updated comment numbering for clarity
- Uses `LogInformation` level for successful managed path detection

**Detection Order**:
1. FFMPEG_PATH environment variable (from Electron)
2. Managed FFmpeg locations (LocalApplicationData)
3. Application directory
4. System PATH
5. Common Linux installation paths
6. Windows Registry
7. Common Windows installation paths

## Testing Checklist

### Build and Package
- [ ] Build the application: `npm run build:electron`
- [ ] Run the portable exe
- [ ] Verify no errors during startup

### Verification
- [ ] Check logs for FFmpeg detection messages:
  - `[BackendService] Found FFmpeg at: <path>` (if found)
  - `[BackendService] Managed FFmpeg not found, backend will search system` (if not found)
- [ ] Navigate to Settings/Dependencies page
- [ ] Verify FFmpeg shows as "Detected" with correct path
- [ ] Test video generation to ensure FFmpeg works

### Edge Cases
- [ ] Test with no managed FFmpeg (should fall back to system)
- [ ] Test with managed FFmpeg in different locations
- [ ] Test in development mode vs production mode
- [ ] Test on different platforms (Windows, macOS, Linux)

## Expected Outcomes

✅ **Success Indicators**:
1. FFMPEG_PATH environment variable is set when backend starts (if FFmpeg found)
2. Backend detects managed FFmpeg without user intervention
3. Dependencies page shows FFmpeg as available
4. Video generation works immediately after setup
5. Logs show clear FFmpeg detection status

❌ **Failure Indicators**:
1. FFMPEG_PATH not set despite FFmpeg being present
2. Backend falls back to system search when managed FFmpeg exists
3. Video generation fails with FFmpeg not found error
4. No FFmpeg detection logs in output

## Architecture

```
Electron (backend-service.js)
  ↓
  Checks multiple paths for FFmpeg executable
  ↓
  Sets FFMPEG_PATH environment variable (if found)
  ↓
Backend (.NET Aura.Api)
  ↓
  Reads FFMPEG_PATH environment variable
  ↓
FFmpegDetectionService.cs
  ↓
  Priority 1: Use FFMPEG_PATH from environment
  Priority 2: Check managed locations
  Priority 3: Fall back to system search
```

## Related Files

- `Aura.Desktop/electron/backend-service.js` - Sets environment variable
- `Aura.Core/Services/Setup/FFmpegDetectionService.cs` - Detects FFmpeg
- `Aura.Core/Configuration/FFmpegConfiguration.cs` - Stores FFmpeg config
- `Aura.Api/Controllers/SetupController.cs` - Exposes FFmpeg status via API

## Notes

- Changes follow zero-placeholder policy (no TODO/FIXME comments)
- Uses platform-aware detection (Windows, macOS, Linux)
- Maintains backward compatibility with existing FFmpeg detection
- Only sets environment variables when FFmpeg is actually found
- Includes proper logging at appropriate levels
