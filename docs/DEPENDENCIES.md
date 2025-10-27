# Aura Video Studio - Dependencies Documentation

## Overview
This document provides a comprehensive manifest of all external dependencies required by Aura Video Studio, including version requirements, criticality levels, and installation guidance.

## Dependency Categories

### 1. Critical Dependencies (Required for Core Functionality)

#### FFmpeg
- **Purpose**: Video encoding, decoding, and processing
- **Minimum Version**: 5.0
- **Recommended Version**: 6.0 or later
- **Platform Support**: Windows, macOS, Linux
- **Auto-Install**: ✅ Supported via Downloads/Engines page
- **Manual Install**: [FFmpeg Setup Guide](./FFmpeg_Setup_Guide.md)
- **Detection Method**: Command line execution (`ffmpeg -version`)
- **Impact if Missing**: Video export, preview, and encoding will not function
- **Fallback**: None - critical for all video operations

#### .NET Runtime
- **Purpose**: Backend API and service host
- **Minimum Version**: 8.0
- **Recommended Version**: 8.0.x (latest patch)
- **Platform Support**: Windows, macOS, Linux
- **Auto-Install**: ❌ User must install manually
- **Manual Install**: https://dotnet.microsoft.com/download
- **Detection Method**: `dotnet --version`
- **Impact if Missing**: Application cannot start
- **Fallback**: None - critical dependency

#### Node.js
- **Purpose**: Frontend build tooling and development
- **Minimum Version**: 18.0.0
- **Maximum Version**: <21.0.0
- **Recommended Version**: 20.x LTS
- **Platform Support**: Windows, macOS, Linux
- **Auto-Install**: ❌ User must install manually
- **Manual Install**: https://nodejs.org/
- **Detection Method**: `node --version`
- **Impact if Missing**: Frontend cannot build or run
- **Fallback**: None - required for development and build

### 2. Optional Dependencies (Enhanced Features)

#### Python
- **Purpose**: AI/ML operations, image generation, advanced processing
- **Minimum Version**: 3.9.0
- **Recommended Version**: 3.11.x
- **Platform Support**: Windows, macOS, Linux
- **Auto-Install**: ❌ User must install manually
- **Manual Install**: https://www.python.org/downloads/
- **Detection Method**: `python --version` or `python3 --version`
- **Impact if Missing**: AI features limited to cloud APIs
- **Fallback**: Cloud-based AI services (requires API keys)

#### CUDA Toolkit (NVIDIA GPUs)
- **Purpose**: GPU-accelerated AI inference and video processing
- **Minimum Version**: 11.8
- **Recommended Version**: 12.x
- **Platform Support**: Windows, Linux (NVIDIA GPUs only)
- **Auto-Install**: ❌ User must install manually
- **Manual Install**: https://developer.nvidia.com/cuda-downloads
- **Detection Method**: `nvidia-smi`, CUDA library availability
- **Impact if Missing**: AI inference runs on CPU (slower)
- **Fallback**: CPU-based processing

### 3. Python Packages (Optional, AI Features)

#### PyTorch
- **Purpose**: Deep learning inference for AI features
- **Minimum Version**: 2.0.0
- **Recommended Version**: 2.1.x or later
- **Required**: Optional (for local AI)
- **Install Command**: `pip install torch torchvision`
- **Detection Method**: `pip show torch`
- **Impact if Missing**: Local AI features unavailable
- **Fallback**: Cloud-based AI APIs

#### Transformers
- **Purpose**: Pre-trained models for text and image processing
- **Minimum Version**: 4.30.0
- **Recommended Version**: Latest
- **Required**: Optional
- **Install Command**: `pip install transformers`
- **Detection Method**: `pip show transformers`
- **Impact if Missing**: Limited local AI capabilities
- **Fallback**: Cloud-based AI APIs

#### OpenCV-Python
- **Purpose**: Advanced image and video processing
- **Minimum Version**: 4.8.0
- **Recommended Version**: Latest
- **Required**: Optional
- **Install Command**: `pip install opencv-python`
- **Detection Method**: `pip show opencv-python`
- **Impact if Missing**: Advanced video analysis features unavailable
- **Fallback**: Basic processing only

#### Whisper (OpenAI)
- **Purpose**: Speech-to-text for auto-captions
- **Minimum Version**: 20230918 (date-based)
- **Recommended Version**: Latest
- **Required**: Optional
- **Install Command**: `pip install openai-whisper`
- **Detection Method**: `pip show openai-whisper`
- **Impact if Missing**: Auto-caption feature limited to cloud APIs
- **Fallback**: Cloud-based transcription services

### 4. Build-Time Dependencies (Development Only)

#### NPM Packages
All listed in `Aura.Web/package.json`:
- TypeScript 5.3.3+
- Vite 6.4.1+
- React 18.2.0+
- Playwright 1.56.0+ (for E2E tests)
- ESLint, Prettier (code quality)

#### NuGet Packages
All listed in various `.csproj` files:
- Entity Framework Core 8.x
- Serilog (logging)
- xUnit (testing)

## Dependency Detection Workflow

### Automatic Detection
1. **On First Launch**: Application runs comprehensive dependency scan
2. **Scanned Items**:
   - FFmpeg availability and version
   - Python installation and version
   - Installed pip packages
   - GPU capabilities (NVIDIA, AMD, Intel)
   - Available disk space and permissions
3. **Results Storage**: Cached in application settings with timestamp

### Manual Rescan
- Triggered via Settings → Dependencies → Rescan button
- Triggered via first-run wizard rescan action
- Forces fresh detection of all dependencies

### Detection APIs

#### Check All Dependencies
```
GET /api/dependencies/check
Response:
{
  "ffmpeg": {
    "installed": true,
    "version": "6.0",
    "path": "/usr/bin/ffmpeg",
    "fullVersion": "ffmpeg version 6.0-static"
  },
  "python": {
    "installed": true,
    "version": "3.11.5",
    "path": "/usr/bin/python3"
  },
  "pipPackages": {
    "torch": { "installed": true, "version": "2.1.0" },
    "transformers": { "installed": false }
  },
  "lastCheck": "2025-10-27T22:00:00Z"
}
```

#### Rescan Dependencies
```
POST /api/dependencies/rescan
Response: Same as check endpoint with updated lastCheck
```

#### Validate Custom Path
```
POST /api/dependencies/validate-path
Body: { "dependency": "ffmpeg", "path": "/custom/path/ffmpeg" }
Response: { "valid": true, "version": "6.0", "path": "/custom/path/ffmpeg" }
```

## Auto-Installation Support

### Supported Auto-Install
- ✅ **FFmpeg**: Downloaded and installed to application data directory
  - Windows: `%LOCALAPPDATA%\Aura\ffmpeg`
  - macOS: `~/Library/Application Support/Aura/ffmpeg`
  - Linux: `~/.local/share/aura/ffmpeg`

### Manual Installation Required
- ❌ **.NET Runtime**: System-level installation required
- ❌ **Node.js**: System-level installation required
- ❌ **Python**: System-level installation required
- ❌ **CUDA Toolkit**: Requires admin privileges and NVIDIA driver compatibility

### Installation Progress Tracking
```
POST /api/dependencies/install/ffmpeg
Response: { "jobId": "install-ffmpeg-123", "status": "queued" }

GET /api/dependencies/install/status/{jobId}
Response: {
  "jobId": "install-ffmpeg-123",
  "status": "in_progress",
  "progress": 45,
  "message": "Downloading FFmpeg...",
  "eta": 30
}
```

## Minimum Viable Configuration

### Free-Only Mode (No API Keys)
- **Required**: FFmpeg, .NET Runtime, Node.js (dev only)
- **Features Available**:
  - Rule-based script generation
  - Stock image/video visuals
  - Windows/macOS TTS (system voice)
  - Basic video editing and export
  
### Cloud-Enhanced Mode (API Keys Required)
- **Required**: Everything in Free-Only mode
- **Features Available**:
  - AI-powered script generation (OpenAI, Claude, etc.)
  - AI voiceover (ElevenLabs, PlayHT)
  - AI image generation (DALL-E, Stable Diffusion via API)
  - Advanced scene detection

### Local AI Mode (No Internet Required)
- **Required**: Everything in Free-Only + Python + pip packages + GPU (recommended)
- **Features Available**:
  - All features from Cloud-Enhanced
  - Works offline
  - No API costs
  - Slower on CPU-only systems

## Filesystem Requirements

### Required Directories
Created automatically on first launch:
- **Data Directory**: Stores application data, cache, and temporary files
  - Windows: `%LOCALAPPDATA%\Aura`
  - macOS: `~/Library/Application Support/Aura`
  - Linux: `~/.local/share/aura`

- **Output Directory**: Default location for exported videos
  - Windows: `%USERPROFILE%\Videos\Aura`
  - macOS: `~/Movies/Aura`
  - Linux: `~/Videos/Aura`

- **Projects Directory**: Stores project files
  - Windows: `%USERPROFILE%\Documents\Aura Projects`
  - macOS: `~/Documents/Aura Projects`
  - Linux: `~/Documents/Aura Projects`

- **Logs Directory**: Application logs
  - Within Data Directory: `<DataDir>/logs`

### Disk Space Requirements
- **Minimum Free Space**: 2 GB
- **Recommended Free Space**: 10 GB or more
- **Per-Project Space**: Varies (typically 100 MB - 2 GB depending on media)

### Permissions Required
- **Read/Write**: All application directories
- **Execute**: FFmpeg binary
- **Network**: Internet access for cloud API features (optional)

## Troubleshooting

### FFmpeg Not Detected
1. Verify FFmpeg is installed: `ffmpeg -version`
2. Check PATH environment variable includes FFmpeg directory
3. Use "Attach Existing" in Downloads/Engines to specify custom path
4. Try auto-install via application

### Python Not Detected
1. Verify Python is installed: `python --version` or `python3 --version`
2. Ensure Python is in PATH
3. On Windows, enable "Add Python to PATH" during installation
4. Specify custom Python path in Settings

### GPU Not Detected
1. Verify GPU drivers are up to date
2. Check CUDA installation: `nvidia-smi` (NVIDIA only)
3. Verify PyTorch can see GPU: `python -c "import torch; print(torch.cuda.is_available())"`
4. Application will fallback to CPU processing

### Pip Packages Missing
1. Install individually: `pip install torch transformers openai-whisper`
2. Or use requirements file (if provided)
3. Verify with: `pip list | grep torch`

## Health Check Endpoints

### Liveness Probe
```
GET /health/live
Response: 200 OK if application is running
```

### Readiness Probe
```
GET /health/ready
Response: 200 OK if all critical services are initialized
Response: 503 Service Unavailable if still initializing
```

## Diagnostic Commands

### Check Dependencies Script
```bash
# Run comprehensive dependency check
./scripts/check-deps.sh

# Output includes:
# - FFmpeg version and path
# - Python version and path
# - Installed pip packages
# - GPU information
# - Disk space availability
# - Directory permissions
```

### Startup Diagnostics
```bash
# View startup logs
tail -f ~/.local/share/aura/logs/startup.log

# Look for initialization sequence:
# ✓ Database Connectivity initialized successfully
# ✓ Required Directories initialized successfully
# ✓ FFmpeg Availability initialized successfully
# ✓ AI Services initialized successfully
```

## Support Matrix

| Dependency | Windows | macOS | Linux | Notes |
|------------|---------|-------|-------|-------|
| FFmpeg | ✅ | ✅ | ✅ | Auto-install supported |
| .NET 8 | ✅ | ✅ | ✅ | Manual install required |
| Node.js | ✅ | ✅ | ✅ | Dev/build only |
| Python | ✅ | ✅ | ✅ | Optional for AI features |
| CUDA | ✅ | ❌ | ✅ | NVIDIA GPUs only |
| System TTS | ✅ | ✅ | ⚠️ | Linux support varies |

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-10-27 | Initial dependency documentation |

## References
- [FFmpeg Setup Guide](./FFmpeg_Setup_Guide.md)
- [Production Readiness Checklist](../PRODUCTION_READINESS_CHECKLIST.md)
- [Integration Testing Guide](./INTEGRATION_TESTING_GUIDE.md)
