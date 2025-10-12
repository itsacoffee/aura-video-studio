# FFmpeg Detection Manual Test Plan

## Test Environment
- Backend API: http://127.0.0.1:5005
- Frontend: http://localhost:5173
- Test data directory: `/tmp/ffmpeg-test`

## Test Cases

### Test 1: Manual Copy + Rescan Workflow

**Objective**: Verify that manually copying FFmpeg to the dependencies folder and clicking "Rescan" detects and registers it.

**Steps**:
1. Start the Aura API backend
2. Open the UI and navigate to Download Center → Engines tab
3. Verify FFmpeg card shows "Not Installed" status
4. Manually copy FFmpeg to the dependencies folder:
   - **Windows**: `%LOCALAPPDATA%\Aura\dependencies\bin\ffmpeg.exe`
   - **Linux/Mac**: `~/.local/share/Aura/dependencies/bin/ffmpeg`
5. Click the "Rescan" button on the FFmpeg card
6. Verify:
   - Alert shows "FFmpeg found and registered!"
   - FFmpeg card status changes to "Installed"
   - Path is displayed showing the dependencies folder location
   - Version string is shown

**Expected Result**: FFmpeg is detected, registered, and the UI updates to show installed status with path.

---

### Test 2: Attach Existing via Absolute Path

**Objective**: Verify that "Attach Existing..." allows specifying any absolute path to FFmpeg.

**Steps**:
1. Start the Aura API backend
2. Open the UI and navigate to Download Center → Engines tab
3. Click "Attach Existing..." button on FFmpeg card
4. In the dialog, enter an absolute path to FFmpeg:
   - **Windows**: `C:\ffmpeg\bin\ffmpeg.exe` (or wherever FFmpeg is installed)
   - **Linux/Mac**: `/usr/bin/ffmpeg` or custom location
5. Click "Attach" button
6. Verify:
   - Success message appears with path and version
   - FFmpeg card updates to show "Installed"
   - The absolute path is displayed
   - Version information is shown

**Expected Result**: FFmpeg is validated, registered with the provided absolute path, and UI updates accordingly.

---

### Test 3: Attach Existing via Directory Path

**Objective**: Verify that "Attach Existing..." accepts a directory and finds FFmpeg inside it.

**Steps**:
1. Start the Aura API backend
2. Open the UI and navigate to Download Center → Engines tab
3. Click "Attach Existing..." button
4. In the dialog, enter a directory path:
   - **Windows**: `C:\ffmpeg` (containing `ffmpeg.exe` or `bin\ffmpeg.exe`)
   - **Linux/Mac**: `/opt/ffmpeg` (containing `ffmpeg` or `bin/ffmpeg`)
5. Click "Attach"
6. Verify:
   - FFmpeg is found inside the directory
   - Success message shows the full path to the executable
   - UI updates with installed status

**Expected Result**: Locator resolves the directory, finds FFmpeg inside, and registers it.

---

### Test 4: Invalid Path Handling

**Objective**: Verify helpful error messages for invalid paths.

**Steps**:
1. Click "Attach Existing..." button
2. Enter a non-existent path: `/fake/path/ffmpeg`
3. Click "Attach"
4. Verify:
   - Error message explains the path was not found
   - Suggestions for fixing the issue are provided

**Expected Result**: Clear error message with actionable fix suggestions.

---

### Test 5: Rescan with No FFmpeg

**Objective**: Verify Rescan provides helpful feedback when FFmpeg is not found.

**Steps**:
1. Ensure no FFmpeg is installed in standard locations
2. Click "Rescan" button
3. Verify:
   - Alert indicates FFmpeg was not found
   - List of attempted paths is shown
   - Suggestion to use "Attach Existing" is provided

**Expected Result**: Helpful message explaining what was searched and how to proceed.

---

### Test 6: API Endpoint Testing

**Objective**: Test the APIs directly without UI.

#### 6.1 Rescan API
```bash
curl -X POST http://127.0.0.1:5005/api/downloads/ffmpeg/rescan
```

**Expected Response** (when not found):
```json
{
  "success": true,
  "found": false,
  "ffmpegPath": null,
  "reason": "FFmpeg not found in any of X candidate locations",
  "attemptedPaths": [
    "...",
    "..."
  ],
  "howToFix": [
    "Install FFmpeg using the Install button",
    "Manually download FFmpeg and use 'Attach Existing'",
    "Place FFmpeg in .../dependencies/bin and click Rescan again"
  ]
}
```

#### 6.2 Attach API
```bash
curl -X POST http://127.0.0.1:5005/api/downloads/ffmpeg/attach \
  -H "Content-Type: application/json" \
  -d '{"path":"/usr/bin/ffmpeg"}'
```

**Expected Response** (success):
```json
{
  "success": true,
  "ffmpegPath": "/usr/bin/ffmpeg",
  "installPath": "/usr/bin",
  "versionString": "6.0-...",
  "validationOutput": "ffmpeg version 6.0 Copyright ...",
  "mode": "External"
}
```

---

### Test 7: Persistence

**Objective**: Verify FFmpeg path persists across restarts.

**Steps**:
1. Attach or install FFmpeg using any method
2. Verify it shows as "Installed"
3. Restart the Aura API backend
4. Reload the UI
5. Verify FFmpeg still shows as "Installed" with the correct path

**Expected Result**: FFmpeg registration persists in install.json and engines-config.json.

---

### Test 8: Open Folder Action

**Objective**: Verify "Open Folder" button works.

**Steps**:
1. Install or attach FFmpeg
2. Click "Open Folder" button
3. Verify:
   - File explorer opens to FFmpeg's directory (platform-dependent)
   - Or alert shows the path if opening fails

**Expected Result**: Folder opens or path is displayed.

---

## Automated Tests

### Unit Tests
```bash
cd /home/runner/work/aura-video-studio/aura-video-studio
dotnet test Aura.Tests/Aura.Tests.csproj --filter "FullyQualifiedName~FfmpegLocator"
```

Expected: 7 tests pass

### Integration Tests
```bash
cd /home/runner/work/aura-video-studio/aura-video-studio
dotnet test Aura.Tests/Aura.Tests.csproj --filter "FullyQualifiedName~FfmpegDetectionApi"
```

Expected: 6 tests pass

---

## Acceptance Criteria

✅ All automated tests pass  
✅ Manual copy to dependencies folder + Rescan detects FFmpeg  
✅ Attach Existing accepts absolute file paths  
✅ Attach Existing accepts directory paths and finds FFmpeg inside  
✅ Invalid paths show helpful error messages  
✅ FFmpeg path is persisted across restarts  
✅ UI shows detected path, version, and provides actions  
✅ Open Folder action works or shows path as fallback
