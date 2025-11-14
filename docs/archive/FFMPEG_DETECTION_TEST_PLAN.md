# FFmpeg Detection Test Plan

## Overview
This document provides comprehensive testing procedures for FFmpeg detection and management across different installation scenarios, focusing on portable distribution compatibility.

## Test Environment
- **Portable Distribution**: Extracted from AuraVideoStudio_Portable_x64.zip
- **Backend API**: http://127.0.0.1:5005
- **Web UI**: http://127.0.0.1:5005 (served by API)
- **Test Scenarios**:
  1. No FFmpeg installed
  2. FFmpeg in PATH
  3. FFmpeg in portable folder
  4. FFmpeg pre-bundled in distribution

## Test Categories

### 1. Detection Scenarios
### 2. Installation Methods
### 3. Attachment Methods
### 4. Priority and Fallback
### 5. Portable Distribution Integration

---

## 1. Detection Scenarios

### Test 1.1: No FFmpeg Detected (Clean System)

**Objective:** Verify behavior when FFmpeg is not found anywhere

**Preconditions:**
- Clean Windows installation
- No FFmpeg in PATH
- No FFmpeg in portable folders

**Steps:**
1. Extract portable distribution
2. Start application via start_portable.cmd
3. Navigate to Download Center
4. Observe FFmpeg card status

**Expected Result:**
```
Status: ❌ Not Installed
Message: FFmpeg is required for video rendering
Version: N/A
Location: Not found

Actions Available:
[Install] [Attach Existing] [Rescan]
```

**Pass Criteria:**
- ✅ Clear "Not Installed" status
- ✅ Helpful message shown
- ✅ Install option prominently displayed
- ✅ No false detection

---

### Test 1.2: FFmpeg in System PATH

**Objective:** Verify detection of FFmpeg in system PATH

**Preconditions:**
- FFmpeg installed system-wide
- ffmpeg.exe accessible via PATH

**Steps:**
1. Verify FFmpeg in PATH:
   ```cmd
   ffmpeg -version
   ```
2. Start Aura Video Studio
3. Navigate to Download Center
4. Observe auto-detection

**Expected Result:**
```
Status: ✅ Installed (External)
Version: ffmpeg version 6.0-essentials
Location: C:\Program Files\ffmpeg\bin\ffmpeg.exe
Source: System PATH

Actions Available:
[Open Folder] [Verify] [Detach]
```

**Pass Criteria:**
- ✅ Auto-detected on startup
- ✅ Version displayed correctly
- ✅ Path shown
- ✅ Marked as "External" installation

---

### Test 1.3: FFmpeg in Portable Folder (Pre-bundled)

**Objective:** Verify detection of pre-bundled FFmpeg

**Preconditions:**
- FFmpeg included in portable ZIP
- Located at: `ffmpeg/ffmpeg.exe` (relative to root)

**Steps:**
1. Extract portable distribution with FFmpeg included
2. Verify file exists: `ffmpeg/ffmpeg.exe`
3. Start application
4. Check detection

**Expected Result:**
```
Status: ✅ Installed (Bundled)
Version: ffmpeg version 6.0-essentials
Location: .\ffmpeg\ffmpeg.exe
Source: Portable Bundle

Actions Available:
[Open Folder] [Verify] [Update]
```

**Pass Criteria:**
- ✅ Detected automatically
- ✅ Recognized as bundled
- ✅ Ready for immediate use
- ✅ No additional setup needed

---

### Test 1.4: FFmpeg in Tools Folder (Downloaded)

**Objective:** Verify detection after download via Download Center

**Preconditions:**
- FFmpeg downloaded via Install button
- Located in: `Tools/ffmpeg/bin/ffmpeg.exe`

**Steps:**
1. Start with no FFmpeg
2. Click "Install" on FFmpeg card
3. Wait for download completion
4. Verify detection

**Expected Result:**
```
Status: ✅ Installed (Downloaded)
Version: ffmpeg version 6.0-essentials
Location: .\Tools\ffmpeg\bin\ffmpeg.exe
Source: Downloaded via Aura

Actions Available:
[Open Folder] [Verify] [Update] [Uninstall]
```

**Pass Criteria:**
- ✅ Detected after download
- ✅ Registered in install manifest
- ✅ Uninstall option available
- ✅ Can be updated

---

## 2. Installation Methods

### Test 2.1: Download and Install via UI

**Objective:** Test standard installation workflow

**Steps:**
1. Navigate to Download Center → Engines
2. Click "Install" on FFmpeg card
3. Observe download progress:
   - Progress bar
   - Download speed
   - Estimated time
4. Wait for installation
5. Verify completion

**Expected Progress:**
```
Status: Downloading FFmpeg...
Progress: ████████████░░░░░░░░ 60%
Downloaded: 31 MB / 52 MB
Speed: 5.2 MB/s
ETA: 4 seconds

↓

Status: Extracting...
Progress: ████████████████████ 100%

↓

Status: ✅ Installed Successfully
Version: ffmpeg 6.0-essentials
Location: .\Tools\ffmpeg\bin\ffmpeg.exe
```

**Pass Criteria:**
- ✅ Download starts immediately
- ✅ Progress updates in real-time
- ✅ Extraction automatic
- ✅ Registration automatic
- ✅ No manual steps required

---

### Test 2.2: Resume Interrupted Download

**Objective:** Verify download resume capability

**Steps:**
1. Start FFmpeg download
2. Wait until 50% complete
3. Kill API process or disconnect network
4. Restart application
5. Return to Download Center
6. Observe resume option

**Expected Result:**
```
⚠️  Incomplete Download
FFmpeg download was interrupted
Downloaded: 26 MB / 52 MB (50%)

[Resume Download] [Start Over] [Cancel]
```

**Recovery Steps:**
1. Click "Resume Download"
2. Verify continues from 50%
3. Verify completes successfully

**Pass Criteria:**
- ✅ Incomplete download detected
- ✅ Resume option available
- ✅ Downloads continue from checkpoint
- ✅ No re-download of completed portions

---

## 3. Attachment Methods

## Test Cases

### Test 3.1: Manual Copy + Rescan Workflow

**Objective**: Verify that manually copying FFmpeg to the portable folder and clicking "Rescan" detects it

**Steps**:
1. Download FFmpeg manually from ffmpeg.org
2. Extract to: `[Portable]\Tools\ffmpeg\bin\ffmpeg.exe`
3. Open Aura Video Studio
4. Navigate to Download Center → Engines
5. Click "Rescan" button on FFmpeg card

**Expected Result:**
```
✅ FFmpeg Found and Registered!
Version: ffmpeg version 6.0-essentials
Location: .\Tools\ffmpeg\bin\ffmpeg.exe
Source: Manual Installation

FFmpeg is now ready to use.
```

**Pass Criteria:**
- ✅ Rescan detects manually placed FFmpeg
- ✅ Version verified automatically
- ✅ Status updates to "Installed"
- ✅ Path shown correctly

---

### Test 3.2: Attach Existing via Absolute Path

**Objective**: Verify "Attach Existing..." allows specifying any absolute path to FFmpeg

**Steps**:
1. Navigate to Download Center → Engines
2. Click "Attach Existing..." button on FFmpeg card
3. In the dialog, enter an absolute path:
   - Windows: `C:\ffmpeg\bin\ffmpeg.exe`
   - Or system: `C:\Program Files\ffmpeg\bin\ffmpeg.exe`
4. Click "Attach" button

**Expected Result:**
```
✅ FFmpeg Attached Successfully!
Version: ffmpeg version 6.0-full
Location: C:\ffmpeg\bin\ffmpeg.exe
Source: External (Attached)

Verification output:
ffmpeg version 6.0-full_build-www.gyan.dev Copyright...
```

**Pass Criteria:**
- ✅ Validates FFmpeg executable
- ✅ Extracts version information
- ✅ Registers with absolute path
- ✅ Status updates to "Installed"
- ✅ Shows as "External" source

---

### Test 3.3: Attach via Directory Path

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
