# FFmpeg Detection Reliability - Implementation Summary

## Overview
This PR implements reliable FFmpeg detection that accepts user-placed files in dependency folders, allows "Attach existing" for any absolute path, and adds a "Rescan / Validate" action. All detected paths are persisted and shown in the UI.

## Files Created

### Backend
1. **Aura.Core/Dependencies/FfmpegLocator.cs** (NEW)
   - Centralized FFmpeg path resolution logic
   - Checks configured registry, app-specific paths, and PATH environment
   - Validates executables by running `ffmpeg -version`
   - Returns structured ValidationResult with path, version, and attempted locations

2. **Aura.Tests/FfmpegLocatorTests.cs** (NEW)
   - 7 unit tests covering all validation scenarios
   - Tests for file paths, directory paths, bin subdirectories, invalid paths
   - All tests passing

3. **Aura.Tests/FfmpegDetectionApiTests.cs** (NEW)
   - 6 integration tests for API endpoints
   - Tests Rescan and Attach Existing workflows
   - Tests error handling for invalid paths
   - All tests passing

4. **FFMPEG_DETECTION_TEST_PLAN.md** (NEW)
   - Comprehensive manual test plan
   - 8 test cases covering all user workflows
   - API endpoint testing examples
   - Acceptance criteria checklist

### Frontend
5. **Aura.Web/src/components/Engines/FFmpegCard.tsx** (NEW)
   - Dedicated FFmpeg management card
   - "Install" button for managed installation
   - "Attach Existing..." button with dialog
   - "Rescan" button for detection
   - "Open Folder" action
   - Real-time status and path display
   - Error handling with user-friendly messages

### Modified Files
6. **Aura.Api/Controllers/DownloadsController.cs**
   - Added `POST /api/downloads/ffmpeg/rescan` endpoint
   - Added `POST /api/downloads/ffmpeg/attach` endpoint
   - Integration with FfmpegLocator and LocalEnginesRegistry

7. **Aura.Api/Program.cs**
   - Registered FfmpegLocator in DI container

8. **Aura.Web/src/components/Engines/EnginesTab.tsx**
   - Added FFmpegCard component to engines tab

## Implementation Details

### 1. FfmpegLocator - Centralized Path Resolution
The FfmpegLocator checks candidates in priority order:
1. Configured path from registry (if user previously attached/installed)
2. App dependencies folder: `%LOCALAPPDATA%\Aura\dependencies\bin`
3. Tools directory with version subdirectories: `%LOCALAPPDATA%\Aura\Tools\ffmpeg\<version>\bin`
4. PATH environment variable

For each candidate:
- Verifies file exists
- Runs `ffmpeg -version` to validate
- Extracts version string from output
- Returns first valid match or list of attempted paths

### 2. Rescan API Endpoint
```
POST /api/downloads/ffmpeg/rescan
```

**Behavior:**
- Calls `FfmpegLocator.CheckAllCandidatesAsync()`
- If found: Updates LocalEnginesRegistry with path, returns success with path/version
- If not found: Returns list of attempted paths with helpful suggestions

**Response Example (Success):**
```json
{
  "success": true,
  "found": true,
  "ffmpegPath": "C:\\Users\\User\\AppData\\Local\\Aura\\dependencies\\bin\\ffmpeg.exe",
  "versionString": "6.0-...",
  "validationOutput": "ffmpeg version 6.0 Copyright...",
  "attemptedPaths": ["...", "..."]
}
```

### 3. Attach Existing API Endpoint
```
POST /api/downloads/ffmpeg/attach
Body: { "path": "C:\\ffmpeg\\bin\\ffmpeg.exe" }
```

**Behavior:**
- Accepts file path or directory path
- If directory: searches for `ffmpeg.exe` in root or `bin/` subdirectory
- Validates by running `ffmpeg -version`
- Creates `install.json` with metadata
- Registers with LocalEnginesRegistry as External mode
- Returns path, version, and validation output

**Error Handling:**
- Invalid path: Returns helpful error with fix suggestions
- Validation failed: Returns error with reason
- Missing executable: Explains where to place FFmpeg

### 4. UI Components

**FFmpegCard Component Features:**
- Status badge (Installed / Not Installed / Needs Attention)
- Path display in monospace font
- Version information
- Action buttons:
  - **Install**: Managed installation from official mirrors
  - **Attach Existing...**: Opens dialog for custom path
  - **Rescan**: Detect manually-copied files
  - **Open Folder**: Navigate to FFmpeg location
  - **Repair**: Reinstall/fix issues
- Error messages with contextual help
- Loading states during async operations

**Attach Dialog:**
- Text input for file or folder path
- Help text explaining accepted formats
- Cancel/Attach actions
- Validates input and shows results

### 5. Persistence
When FFmpeg is detected or attached:
1. Creates/updates `install.json` in the FFmpeg directory:
   ```json
   {
     "id": "ffmpeg",
     "version": "external",
     "installPath": "C:\\ffmpeg",
     "ffmpegPath": "C:\\ffmpeg\\bin\\ffmpeg.exe",
     "sourceType": "AttachExisting",
     "installedAt": "2024-10-12T...",
     "validated": true,
     "validationOutput": "ffmpeg version..."
   }
   ```

2. Registers with LocalEnginesRegistry in `engines-config.json`:
   ```json
   {
     "id": "ffmpeg",
     "engineId": "ffmpeg",
     "name": "FFmpeg (External)",
     "mode": "External",
     "installPath": "C:\\ffmpeg",
     "executablePath": "C:\\ffmpeg\\bin\\ffmpeg.exe"
   }
   ```

## Test Coverage

### Unit Tests (7 tests)
- `ValidatePathAsync_WithValidFfmpegExecutable_Succeeds`
- `ValidatePathAsync_WithDirectory_FindsFfmpegInDirectory`
- `ValidatePathAsync_WithBinSubdirectory_FindsFfmpeg`
- `ValidatePathAsync_WithNonExistentPath_ReturnsFalse`
- `ValidatePathAsync_WithInvalidBinary_ReturnsFalse`
- `CheckAllCandidatesAsync_FindsFfmpegInConfiguredPath`
- `CheckAllCandidatesAsync_WithNoFFmpeg_ReturnsNotFound`

### Integration Tests (6 tests)
- `RescanFFmpeg_WithNoFFmpeg_ReturnsNotFound`
- `RescanFFmpeg_WithValidFFmpeg_FindsAndRegisters`
- `AttachFFmpeg_WithValidPath_Succeeds`
- `AttachFFmpeg_WithDirectory_FindsExecutable`
- `AttachFFmpeg_WithInvalidPath_ReturnsBadRequest`
- `AttachFFmpeg_WithEmptyPath_ReturnsBadRequest`

**Total: 52 FFmpeg-related tests passing** (includes existing tests)

## User Workflows

### Workflow 1: Manual Copy + Rescan
1. User downloads FFmpeg manually
2. Copies `ffmpeg.exe` to `%LOCALAPPDATA%\Aura\dependencies\bin\`
3. Opens Aura → Download Center → Engines tab
4. Clicks "Rescan" button
5. Alert shows "FFmpeg found and registered!"
6. UI updates to show Installed status with path

### Workflow 2: Attach Existing Installation
1. User already has FFmpeg installed at `C:\ffmpeg\bin\`
2. Opens Aura → Download Center → Engines tab
3. Clicks "Attach Existing..." button
4. Enters path: `C:\ffmpeg\bin\ffmpeg.exe` (or `C:\ffmpeg`)
5. Clicks "Attach"
6. Alert shows success with version
7. UI updates to show detected path

### Workflow 3: Standard Installation
1. User clicks "Install" button (existing functionality)
2. FFmpeg downloads and extracts automatically
3. Path is detected and persisted
4. UI shows installed status

## Acceptance Criteria - All Met ✅

- ✅ Manually copying files into app's dependency folder + clicking Rescan updates UI to Installed
- ✅ Rescan detects FFmpeg in multiple standard locations
- ✅ Attaching an absolute path validates and persists the path
- ✅ Attaching a directory path searches for FFmpeg inside
- ✅ All metadata persisted in install.json and engines-config.json
- ✅ UI shows ffmpegPath as absolute path
- ✅ "Open Folder" action available
- ✅ Helpful error messages with fix suggestions
- ✅ All automated tests passing (52 tests)
- ✅ Minimal changes - no deletion of existing code

## Technical Notes

### Cross-Platform Compatibility
- Uses `RuntimeInformation.IsOSPlatform()` for platform detection
- Windows: Looks for `ffmpeg.exe`
- Linux/Mac: Looks for `ffmpeg` binary
- Path separators handled correctly on all platforms

### Error Handling
- All API endpoints return structured error responses
- Error messages include "howToFix" suggestions
- UI shows user-friendly alerts and error states
- Validation failures explain what went wrong

### Performance
- Async/await throughout for non-blocking operations
- Cancellation token support for long-running operations
- Lazy loading of status in UI components

## Future Enhancements (Not in This PR)
- File picker dialog integration (would require native APIs)
- Auto-rescan on folder changes (would require file watchers)
- Multiple FFmpeg versions side-by-side
- Download progress indicators with SignalR
- FFmpeg capability detection (encoders, decoders, formats)

## Breaking Changes
None. All changes are additive and backward compatible.

## Migration Notes
No migration needed. Existing installations will continue to work. Users can optionally use the new Rescan/Attach features.
