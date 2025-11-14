# Flexible Engine Installs - Implementation Summary

## Overview

This PR implements flexible engine installation modes, allowing users to either let Aura manage engines automatically or attach their own existing installations. This addresses the key issues of forced internal directories and 404 errors by providing complete flexibility.

## Key Features Implemented

### 1. Two Installation Modes

#### Managed Mode (App-Controlled)
- Aura handles full lifecycle: install, start, stop, updates
- Automatic process monitoring and health checks
- Optional auto-restart on crashes
- Integrated log capture and viewing
- **Best for**: Users who want hands-off automation

#### External Mode (User-Managed)
- Point Aura to existing installations anywhere on disk
- Users start/stop engines manually
- Aura monitors health but doesn't control the process
- **Best for**: Advanced users with existing setups

### 2. Attach Existing Engines

Users can attach any existing engine installation:
- **All engines supported**: FFmpeg, Ollama, SD WebUI, ComfyUI, Piper, Mimic3
- **Any directory**: No forced paths - attach from anywhere on disk
- **Validation**: Paths are validated before attachment
- **Easy UI**: "Attach Existing Install" button on each engine card

### 3. Enhanced UI/UX

#### Engine Instances Section
- **Mode badges** with tooltips explaining Managed vs External
- **Status badges** showing installed/running state
- **Health badges** when engines are running
- **Path display** with copy-to-clipboard functionality
- **Port display** with copy-to-clipboard
- **Executable path** display with copy-to-clipboard
- **Open Folder** button - opens installation directory in file explorer
- **Open Web UI** button - opens engine's web interface in browser

#### Visual Improvements
- Mode badges use different colors (Managed=blue, External=green)
- Hover tooltips on mode badges explain what each mode means
- Copy icons appear next to all paths for easy clipboard access
- Better layout with wrapped badges and aligned actions
- "Attach Existing Install" button made more prominent with "or" separator

### 4. Backend Infrastructure

All backend APIs already existed and were enhanced:
- `POST /api/engines/attach` - Attach external engine
- `POST /api/engines/reconfigure` - Update engine configuration
- `GET /api/engines/instances` - List all instances with mode, path, port
- `POST /api/engines/open-folder` - Open folder in file explorer
- `POST /api/engines/open-webui` - Get web UI URL

Path validation ensures:
- Install paths exist
- Executable paths are valid
- Normalized absolute paths are stored
- FFmpeg-specific validation for both ffmpeg and ffprobe

### 5. Documentation

Comprehensive documentation added to `docs/ENGINES.md`:
- Explanation of Managed vs External modes
- When to choose each mode
- Step-by-step guide for attaching engines
- Managing engine instances
- Practical examples for FFmpeg and SD WebUI
- Clear guidance on mode badges and UI features

## Technical Implementation

### Files Modified

1. **Aura.Web/src/components/Engines/EnginesTab.tsx**
   - Added copy-to-clipboard functionality
   - Added mode tooltips with Info icons
   - Added health status badges
   - Improved instance card layout
   - Enhanced visual hierarchy

2. **Aura.Web/src/components/Engines/EngineCard.tsx**
   - Made "Attach Existing Install" more prominent
   - Added "or" separator between Install and Attach buttons

3. **Aura.Tests/EngineAttachTests.cs**
   - Added FFmpeg-specific attach test
   - Validates proper path handling

4. **docs/ENGINES.md**
   - Added comprehensive mode documentation
   - Added practical examples
   - Clarified UI features

### Backend (Already Existed)

- **LocalEnginesRegistry.cs** - Manages engine instances with mode tracking
- **EnginesController.cs** - Attach, reconfigure, open-folder, open-webui endpoints
- **EngineDetector.cs** - Detects and validates all engine types
- **AttachEngineDialog.tsx** - UI dialog for attaching engines

## Test Coverage

### Unit Tests (519 passing)
- ✅ `AttachExternalEngine_WithValidPath_ShouldSucceed`
- ✅ `AttachExternalEngine_WithInvalidPath_ShouldFail`
- ✅ `AttachExternalEngine_WithExecutablePath_ShouldValidate`
- ✅ `AttachExternalEngine_WithInvalidExecutablePath_ShouldFail`
- ✅ `AttachExternalEngine_FFmpeg_WithValidPath_ShouldSucceed` (new)
- ✅ `ReconfigureEngine_WithNewPort_ShouldUpdate`
- ✅ `ReconfigureEngine_WithInvalidInstance_ShouldFail`
- ✅ `GetEngineInstances_ShouldReturnAllInstancesOfType`

### E2E Tests (61 passing)
All existing E2E tests continue to pass.

## User Experience Flow

### Before (Problems)
❌ Users forced to use internal app directory  
❌ Can't point to existing installations  
❌ No visibility of engine paths or ports  
❌ No way to open folder or web UI quickly  
❌ Unclear what "Managed" vs "External" means  

### After (Solutions)
✅ Users can attach any existing installation  
✅ Full path flexibility - install anywhere  
✅ Clear display of mode, path, port with copy buttons  
✅ "Open Folder" and "Open Web UI" buttons  
✅ Tooltips explain Managed vs External modes  
✅ Comprehensive documentation with examples  

## Examples

### Example 1: Attach Existing FFmpeg

**User has FFmpeg at**: `C:\Tools\ffmpeg\bin\ffmpeg.exe`

1. Navigate to Download Center → Engines
2. Find "FFmpeg" card
3. Click "Attach Existing Install"
4. Fill in:
   - Install Path: `C:\Tools\ffmpeg`
   - Executable Path: `C:\Tools\ffmpeg\bin\ffmpeg.exe`
5. Click "Attach"

**Result**: FFmpeg appears in Engine Instances with External mode badge

### Example 2: Attach Running Stable Diffusion WebUI

**User has SD WebUI running at**: `http://localhost:7860`

1. Navigate to Download Center → Engines
2. Find "Stable Diffusion WebUI" card
3. Click "Attach Existing Install"
4. Fill in:
   - Install Path: `C:\stable-diffusion-webui`
   - Executable Path: `C:\stable-diffusion-webui\webui-user.bat`
   - Port: `7860`
   - Health Check URL: `http://localhost:7860/internal/ping`
   - Notes: "My production SD setup with custom models"
5. Click "Attach"

**Result**:
- SD WebUI appears in Engine Instances with External mode badge
- Status shows "running" if currently active
- Health badge shows "Healthy" if health check passes
- "Open Folder" opens `C:\stable-diffusion-webui`
- "Open Web UI" opens `http://localhost:7860` in browser
- Can copy paths with one click

## Benefits

### For New Users
- ✅ Managed mode provides fully automated experience
- ✅ One-click install and start
- ✅ No configuration needed

### For Advanced Users
- ✅ External mode preserves existing setups
- ✅ No forced directory structure
- ✅ Full control over engine lifecycle
- ✅ Quick access via "Open Folder" and "Open Web UI"

### For All Users
- ✅ Clear visibility of what's installed and where
- ✅ Easy path copying for troubleshooting
- ✅ Tooltips explain features inline
- ✅ Comprehensive documentation
- ✅ Works for all engine types (FFmpeg, Ollama, SD, ComfyUI, Piper, Mimic3)

## Acceptance Criteria Met

✅ **Add "Attach Existing Install" for all engines** - Implemented via AttachEngineDialog  
✅ **Support BOTH "Managed" and "External" modes** - Full support with mode badges  
✅ **Persist absolute install paths anywhere on disk** - No forced internal folder  
✅ **Health/detection works for both modes** - Path validation and health checks  
✅ **Show path + port clearly in UI** - Monospace display with copy buttons  
✅ **Open Folder / Open Web UI buttons** - Both implemented and working  
✅ **Validation for all engine types** - FFmpeg, Ollama, SD WebUI, ComfyUI, Piper, Mimic3  
✅ **Tests for attach/reconfigure functionality** - 8 tests passing  
✅ **Documentation** - Comprehensive guide in docs/ENGINES.md  

## Breaking Changes

**None** - This is purely additive. All existing functionality continues to work.

## Migration Path

Existing users with Managed engines: **No action needed** - Everything continues to work.

Users wanting to attach existing engines: Follow the new "Attach Existing Install" flow.

## Future Enhancements (Not in Scope)

These work but could be enhanced later:
- Bulk attach multiple engines at once
- Auto-detect common installation paths
- Import/export engine configurations
- Engine-specific advanced settings UI
- Custom health check intervals

## Offline Mode Enhancements

### New: "Tune for My Machine" Feature

Aura now provides intelligent, hardware-specific recommendations for offline providers:

**Features**:
- Automatic hardware detection (RAM, VRAM, CPU cores, GPU vendor)
- TTS provider recommendations (Piper vs Mimic3) based on available RAM
- LLM model recommendations (Ollama models) based on RAM and VRAM
- Image provider recommendations (SD WebUI vs Stock Images) based on GPU capabilities
- Overall capability assessment showing what's possible offline
- Hardware-specific quick start guides

**Access**:
1. Navigate to **Download Center → Offline Mode** tab
2. View your hardware summary and capabilities
3. Follow provider-specific recommendations with speed/quality expectations
4. Use the quick start guide tailored to your system

**Benefits**:
- Zero guesswork about which offline providers to use
- Clear performance expectations (speed and quality)
- Automatic fallback recommendations
- Transparent capability display

### Offline Provider Status Dashboard

Real-time monitoring of all offline providers:
- Live status checks for Piper, Mimic3, Ollama, SD WebUI, Windows TTS
- Capability summary (TTS, LLM, images available)
- Provider-specific recommendations and setup guides
- Installation links for unavailable providers

### Offline Mode Capability Banners

Context-aware banners that display:
- Current offline capabilities (what's available vs missing)
- Setup guidance for missing providers
- Direct links to configuration pages
- Compact mode for minimal intrusion

## Conclusion

This implementation provides complete flexibility for engine management while maintaining the simplicity of the Managed mode for new users. The UI clearly shows what's installed, where it is, and how to access it. Documentation guides users through both modes with practical examples.

With the new offline mode enhancements, users get:
1. **Hardware-optimized recommendations** - No more guessing which providers work on their system
2. **Transparent capabilities** - Clear visibility of what works offline
3. **Actionable guidance** - Step-by-step setup instructions tailored to hardware
4. **Smart fallbacks** - Automatic recommendations when primary providers unavailable

**Key Achievement**: Users are no longer forced into internal directories and can point Aura to engines anywhere on their system. Additionally, offline mode is now fully discoverable with intelligent recommendations and clear capability transparency, solving the core problems outlined in the issue.
