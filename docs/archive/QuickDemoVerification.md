# Quick Demo Verification Guide

## Overview
This document provides step-by-step instructions for verifying the Quick Demo flow on a clean Windows installation. Follow these steps to ensure the first-run experience works correctly.

## Prerequisites
- Clean Windows 10/11 installation (virtual machine recommended)
- No dependencies installed (no FFmpeg, no .NET Runtime, etc.)
- 4 GB RAM minimum
- 2 GB free disk space
- Modern web browser (Chrome, Edge, or Firefox)

## Test Environment Setup

### Option 1: Virtual Machine (Recommended)
1. Create a new Windows 10/11 VM in VirtualBox, VMware, or Hyper-V
2. Allocate at least 4 GB RAM and 20 GB disk space
3. Install a web browser (Edge is pre-installed on Windows 10/11)
4. Do NOT install any development tools or runtimes

### Option 2: Clean Machine
1. Use a physical machine with fresh Windows installation
2. Ensure no development tools are installed
3. Verify no FFmpeg, .NET Runtime, or other dependencies exist

## Verification Steps

### Step 1: Extract the Portable Distribution

1. **Download** the portable ZIP:
   - From GitHub Releases: `AuraVideoStudio_Portable_x64.zip`
   - Or from build artifacts: `artifacts/portable/AuraVideoStudio_Portable_x64.zip`

2. **Extract** to a test location:
   ```
   Recommended locations:
   - C:\Aura
   - D:\Tools\AuraVideoStudio
   - Desktop\AuraTest
   ```

3. **Verify** the extracted structure:
   ```
   AuraVideoStudio_Portable_x64/
   ├── Api/
   │   ├── Aura.Api.exe
   │   └── wwwroot/
   ├── Tools/           (empty initially)
   ├── AuraData/
   ├── Logs/            (empty initially)
   ├── Projects/        (empty initially)
   ├── Downloads/       (empty initially)
   ├── ffmpeg/          (may be empty if not pre-bundled)
   ├── start_portable.cmd
   ├── README.md
   ├── version.json
   └── checksums.txt
   ```

**Expected Result:** ✅ All folders and files present

**Screenshot:** Take a screenshot of the extracted folder structure

---

### Step 2: First Launch

1. **Double-click** `start_portable.cmd`

2. **Observe** the launcher output:
   ```
   ========================================
    Aura Video Studio - Portable Edition
   ========================================

   Starting API server...
   Waiting for API to start...
   ```

3. **Wait** for health check (up to 30 seconds):
   - Launcher polls `/healthz` endpoint
   - Shows progress messages
   - Opens browser when API is ready

4. **Expected launcher output:**
   ```
   ✓ API is healthy!

   Opening web browser...

   Application started successfully!

   Web UI: http://127.0.0.1:5005
   ```

**Expected Result:**
- ✅ API starts within 10 seconds
- ✅ Browser opens automatically
- ✅ No error messages in console

**Screenshot:** Take a screenshot of the launcher console

---

### Step 3: First-Run Wizard

1. **Verify** the browser opens to `http://127.0.0.1:5005`

2. **Expected Welcome Screen:**
   - Aura Video Studio logo
   - Welcome message
   - "Get Started" or "First-Time Setup" button
   - System status indicators

3. **Click** "Get Started" or equivalent

4. **Verify First-Run Wizard appears:**
   - Step 1: Welcome and system check
   - Hardware capability detection
   - Dependency status (FFmpeg, Ollama, etc.)

5. **Expected System Status:**
   ```
   ✅ CPU: Detected (e.g., Intel Core i7)
   ✅ RAM: 8 GB available
   ✅ GPU: Detected (if available) or "No GPU detected"
   ⚠️  FFmpeg: Not installed
   ⚠️  Ollama: Not installed (optional)
   ⚠️  Stable Diffusion: Not installed (optional)
   ```

**Expected Result:**
- ✅ Web UI loads successfully
- ✅ No 404 errors
- ✅ System status shows detected hardware
- ✅ Missing dependencies clearly indicated

**Screenshot:** Take a screenshot of the welcome screen and system status

---

### Step 4: Dependency Setup (Quick Path)

1. **Navigate** to "Download Center" or "Setup" tab

2. **Verify FFmpeg Card shows:**
   - Status: "Not Installed" (red indicator)
   - Description: "Required for video rendering"
   - Actions available:
     - "Install" button (downloads FFmpeg)
     - "Attach Existing" button
     - "Rescan" button

3. **Click "Install"** on FFmpeg card

4. **Observe download progress:**
   - Progress bar shows download percentage
   - Download speed and ETA displayed
   - Status updates: "Downloading..." → "Extracting..." → "Verifying..."

5. **Wait for installation** to complete (1-3 minutes)

6. **Expected completion:**
   ```
   ✅ FFmpeg installed successfully
   Version: ffmpeg 6.0-essentials
   Location: C:\...\AuraVideoStudio_Portable_x64\Tools\ffmpeg\bin\ffmpeg.exe
   ```

**Expected Result:**
- ✅ Download starts immediately
- ✅ Progress shown in real-time
- ✅ Installation completes without errors
- ✅ FFmpeg card updates to "Installed" status

**Screenshot:**
- Before: FFmpeg "Not Installed"
- During: Download progress
- After: FFmpeg "Installed"

---

### Step 5: Create First Video (Quick Demo)

1. **Navigate** to "Create" page

2. **Fill in Quick Demo settings:**
   ```
   Topic: "Introduction to AI Video Creation"
   Audience: "General"
   Duration: 1 minute
   Tone: "Informative"
   Style: "Standard"
   ```

3. **Click "Next"** through wizard steps:
   - Step 1: Video Brief (topic, audience, tone)
   - Step 2: Video Plan (duration, pacing, style)
   - Step 3: Generation Options (profile selection)

4. **Select "Free-Only" profile** in Step 3

5. **Click "Run Preflight Check"**

6. **Expected preflight results:**
   ```
   ✅ FFmpeg: Available
   ✅ Script Generator: Free provider (Rule-based)
   ✅ TTS: Free provider (Windows SAPI)
   ✅ Video Generator: FFmpeg compositor
   ⚠️  Visuals: Limited to text slides and color backgrounds
   ```

7. **Click "Generate Video"**

8. **Observe generation progress:**
   - Step 1/5: Generating script...
   - Step 2/5: Synthesizing speech...
   - Step 3/5: Creating timeline...
   - Step 4/5: Rendering video...
   - Step 5/5: Finalizing...

9. **Wait for completion** (30 seconds to 2 minutes)

10. **Expected result:**
    ```
    ✅ Video generated successfully!
    Output: C:\...\AuraVideoStudio_Portable_x64\Projects\video_[timestamp].mp4
    Duration: ~1:00
    Resolution: 1920x1080
    File size: ~5-10 MB
    ```

**Expected Result:**
- ✅ All wizard steps complete successfully
- ✅ Preflight check passes with Free profile
- ✅ Video generation starts without errors
- ✅ Progress updates shown in real-time
- ✅ Final video file created in Projects folder

**Screenshot:**
- Wizard Step 1 (Video Brief)
- Wizard Step 3 (Preflight Check results)
- Generation progress screen
- Completion screen with output path

---

### Step 6: Verify Generated Video

1. **Navigate** to output folder:
   ```
   C:\...\AuraVideoStudio_Portable_x64\Projects\
   ```

2. **Verify file exists:**
   - File name: `video_[timestamp].mp4`
   - File size: 5-15 MB (for 1-minute video)
   - Creation date: Current date/time

3. **Play video** in Windows Media Player or VLC:
   - Video should play without errors
   - Audio should be present (Windows SAPI voice)
   - Resolution: 1920x1080 (or selected aspect ratio)
   - Duration: ~1 minute

4. **Verify video quality:**
   - Clear audio narration
   - Text slides or visual elements present
   - No corruption or artifacts
   - Smooth playback

**Expected Result:**
- ✅ Video file exists and is playable
- ✅ Audio narration is clear
- ✅ Video duration matches requested length
- ✅ No playback errors

**Screenshot:**
- File Explorer showing generated video
- Video playing in media player (1-2 frames)

---

### Step 7: Verify Logs and Data

1. **Check Logs folder:**
   ```
   C:\...\AuraVideoStudio_Portable_x64\Logs\
   ```
   - Should contain: `aura-api-[YYYYMMDD].log`
   - Log should show successful API operations

2. **Check AuraData folder:**
   ```
   C:\...\AuraVideoStudio_Portable_x64\AuraData\
   ```
   - Should contain:
     - `settings.json` (user preferences)
     - `install-manifest.json` (installed dependencies)

3. **Verify Tools folder:**
   ```
   C:\...\AuraVideoStudio_Portable_x64\Tools\
   ```
   - Should contain: `ffmpeg/` folder with binaries

4. **Check Projects folder:**
   ```
   C:\...\AuraVideoStudio_Portable_x64\Projects\
   ```
   - Should contain: Generated video file(s)

**Expected Result:**
- ✅ All folders created with appropriate content
- ✅ Logs show successful operations
- ✅ Settings persisted correctly
- ✅ Portable structure maintained

**Screenshot:** File Explorer showing all data folders with contents

---

### Step 8: Stop and Restart Test

1. **Close the API server:**
   - Close the "Aura API" console window
   - Or press Ctrl+C in launcher window

2. **Wait** 5 seconds

3. **Restart** by double-clicking `start_portable.cmd` again

4. **Verify**:
   - API starts successfully
   - Browser opens to web UI
   - Settings are preserved (FFmpeg still shown as installed)
   - Projects list shows previously generated video

5. **Navigate** to "Download Center"

6. **Verify** FFmpeg still shows "Installed" status

**Expected Result:**
- ✅ Application restarts without errors
- ✅ Settings persist across restarts
- ✅ Previously installed dependencies recognized
- ✅ Generated videos still accessible

**Screenshot:** Download Center after restart showing FFmpeg installed

---

## Verification Checklist

Complete this checklist after running all steps:

- [ ] **Extraction**: All files extracted correctly
- [ ] **First Launch**: API starts and browser opens automatically
- [ ] **Welcome Screen**: Web UI loads without 404 errors
- [ ] **System Detection**: CPU, RAM, GPU detected correctly
- [ ] **Dependency Setup**: FFmpeg installs successfully
- [ ] **Wizard Flow**: All wizard steps navigable
- [ ] **Preflight Check**: Correctly identifies available/missing components
- [ ] **Video Generation**: Completes without errors
- [ ] **Output Verification**: Video file created and playable
- [ ] **Logs Created**: Log files generated in Logs folder
- [ ] **Settings Persist**: Configuration saved across restarts
- [ ] **Portable Structure**: All data in portable folders

## Expected Timings

| Step | Expected Duration |
|------|-------------------|
| Extract ZIP | < 30 seconds |
| First Launch | 5-10 seconds |
| Load Web UI | < 3 seconds |
| FFmpeg Install | 1-3 minutes |
| Preflight Check | < 5 seconds |
| Video Generation (1 min) | 30-120 seconds |
| **Total (First Run)** | **5-10 minutes** |

## Common Issues and Solutions

### Issue: API Won't Start

**Symptoms:**
- Launcher shows "ERROR: API failed to start"
- Console shows errors or closes immediately

**Solutions:**
1. Check if port 5005 is already in use:
   ```cmd
   netstat -ano | findstr :5005
   ```
2. Verify antivirus is not blocking `Aura.Api.exe`
3. Check `Logs\aura-api-[date].log` for error details
4. Run as administrator if permissions issue

---

### Issue: 404 Error in Browser

**Symptoms:**
- Browser shows "404 - Not Found"
- Web UI doesn't load

**Solutions:**
1. Verify `Api\wwwroot\` folder exists with files
2. Check API console for warnings about missing wwwroot
3. Wait 10 seconds and refresh browser
4. Check that all ZIP contents were extracted

---

### Issue: FFmpeg Install Fails

**Symptoms:**
- Download progress stops or shows error
- Installation doesn't complete

**Solutions:**
1. Check internet connection
2. Verify firewall allows downloads
3. Try "Attach Existing" if FFmpeg is already installed
4. Use manual download from ffmpeg.org

---

### Issue: Video Generation Fails

**Symptoms:**
- Generation progress stops
- Error message shown
- No video file created

**Solutions:**
1. Check `Logs\` folder for error details
2. Verify FFmpeg is properly installed (check Download Center)
3. Ensure sufficient disk space (at least 500 MB free)
4. Try shorter duration (30 seconds) for testing

---

## Success Criteria

✅ **Quick Demo passes if:**
1. Portable ZIP extracts without issues
2. API starts on first launch within 10 seconds
3. Web UI loads without 404 errors
4. System hardware detected correctly
5. FFmpeg installs successfully via Download Center
6. Wizard completes all steps without errors
7. Video generates successfully with Free profile
8. Output video is playable and matches specifications
9. Settings persist across application restarts
10. No critical errors in logs

## Test Report Template

After completing verification, fill out this report:

```
=== Quick Demo Verification Report ===

Date: [DATE]
Tester: [NAME]
Environment: Windows [10/11], [VM/Physical]
Build Version: [VERSION from version.json]

Step 1 - Extraction: [PASS/FAIL]
Step 2 - First Launch: [PASS/FAIL]
Step 3 - First-Run Wizard: [PASS/FAIL]
Step 4 - Dependency Setup: [PASS/FAIL]
Step 5 - Create First Video: [PASS/FAIL]
Step 6 - Verify Video: [PASS/FAIL]
Step 7 - Verify Data: [PASS/FAIL]
Step 8 - Restart Test: [PASS/FAIL]

Total Duration: [X] minutes

Issues Encountered:
1. [Description]
2. [Description]

Notes:
[Additional observations]

Overall Result: [PASS/FAIL]
```

## Screenshots Required

Collect these screenshots during verification:

1. ✅ Extracted folder structure
2. ✅ Launcher console output
3. ✅ Welcome screen with system status
4. ✅ FFmpeg "Not Installed" state
5. ✅ FFmpeg download progress
6. ✅ FFmpeg "Installed" state
7. ✅ Wizard Step 1 (Video Brief)
8. ✅ Wizard Step 3 (Preflight Check)
9. ✅ Video generation progress
10. ✅ Generation complete screen
11. ✅ Generated video in File Explorer
12. ✅ Video playing in media player
13. ✅ Data folders with contents
14. ✅ Download Center after restart

## Next Steps

After successful Quick Demo verification:
1. Proceed to full Wizard End-to-End testing (see `WizardEndToEndTests.md`)
2. Run error path tests (see `ErrorPathTests.md`)
3. Test FFmpeg detection scenarios (see `FFMPEG_DETECTION_TEST_PLAN.md`)
4. Verify AI orchestration (see `AIOrchestrationTests.md`)

---

**Document Version:** 1.0  
**Last Updated:** 2025-10-20  
**Maintained By:** Aura Video Studio Team
