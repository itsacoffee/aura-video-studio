# First-Run Experience - User Guide

## Overview

Aura Video Studio now includes comprehensive first-run diagnostics and automated error recovery to ensure a smooth experience for new users. This document explains what happens on first run and how to resolve common issues.

## What Happens on First Run?

### 1. Automatic System Check

When you first start Aura Video Studio, it automatically performs a comprehensive system check:

- **FFmpeg Detection**: Checks if FFmpeg (required for video rendering) is installed
- **Directory Permissions**: Verifies write access to required directories
- **Disk Space**: Ensures sufficient free space for video rendering
- **Internet Connectivity**: Tests if you can download components
- **Hardware Detection**: Identifies your CPU, RAM, and GPU capabilities

### 2. User-Friendly Diagnostics

The diagnostics results are displayed with:

- ✅ **Status Badge**: Ready / Needs Setup / Has Errors
- 🎯 **Issue Cards**: Each issue clearly explained with severity level
- 💡 **Possible Causes**: Understanding why the issue occurred
- 🔧 **Fix Actions**: One-click buttons to resolve issues

### 3. Automatic Error Recovery

For auto-fixable issues (like missing FFmpeg), you can click the fix button to:

- Automatically download the required component
- Install it to the correct location
- Verify the installation
- Re-run diagnostics to confirm the fix

## Common First-Run Issues and Solutions

### Issue: FFmpeg Not Found

**What it means**: FFmpeg is required for video rendering but wasn't detected on your system.

**How to fix**:

1. **Automatic Fix** (Recommended):
   - Click the "Install FFmpeg Automatically" button
   - Wait for the download and installation (2-5 minutes)
   - FFmpeg will be installed to `Tools/ffmpeg/`

2. **Manual Fix**:
   - Download FFmpeg from https://ffmpeg.org/download.html
   - Extract `ffmpeg.exe` (Windows) or `ffmpeg` (Linux/Mac)
   - Place it in the `Tools/ffmpeg/` directory
   - Or configure the path in Settings

3. **Use System FFmpeg**:
   - If you already have FFmpeg installed system-wide
   - The application will automatically detect it
   - No additional action needed

### Issue: Directory Permission Denied

**What it means**: The application can't write to required directories.

**Possible causes**:
- Running from a read-only location (CD-ROM, network drive)
- Antivirus software blocking access
- Insufficient file system permissions

**How to fix**:

1. **Move to Writable Location**:
   - Extract the portable ZIP to `C:\Aura` or `Documents\Aura`
   - Avoid Program Files (requires admin)
   - Avoid network drives or USB drives (can be slow)

2. **Check Antivirus**:
   - Add Aura Video Studio to antivirus exclusion list
   - Temporarily disable real-time protection to test
   - Some antivirus software blocks portable applications

3. **Run as Administrator** (Last Resort):
   - Right-click `Launch.bat` or `Aura.Api.exe`
   - Select "Run as administrator"
   - Note: Not recommended for regular use

### Issue: Low Disk Space

**What it means**: Less than 5 GB free space available.

**Why it matters**: Video rendering requires temporary space for:
- Intermediate files during rendering
- Downloaded assets and components
- Project files and exports

**How to fix**:
- Free up disk space by deleting unnecessary files
- Move the application to a drive with more space
- Consider using an external drive for project storage

### Issue: No Internet Connection

**What it means**: Internet connection not detected.

**Impact**:
- Cannot download components automatically
- Cannot use cloud AI providers (OpenAI, etc.)
- Stock image providers unavailable

**Offline workarounds**:
- Use local providers (RuleBased script generation)
- Use Windows TTS for narration (free, no internet needed)
- Use local stock images from a folder
- Install components manually using provided links

### Issue: Low System RAM

**What it means**: Less than 4 GB RAM detected.

**Recommendations**:
- Close other applications before rendering
- Use lower quality settings (720p instead of 1080p)
- Reduce concurrent operations
- Consider upgrading RAM for better performance

## Onboarding Wizard

If any issues are detected, you'll be guided through an onboarding wizard that:

1. **Welcome**: Introduction and overview
2. **Hardware Detection**: Automatic system capability assessment
3. **Component Installation**: Install required components (FFmpeg, Ollama, etc.)
4. **Validation**: Verify everything is working
5. **Ready**: Start creating videos!

### Skipping Onboarding

You can skip the onboarding wizard by clicking "Skip" at any step. However, you may need to:
- Manually install required components
- Configure paths in Settings
- Run diagnostics later from the Welcome page

## Manual Configuration

If automatic setup doesn't work, you can manually configure:

### FFmpeg Path

1. Go to Settings → Providers
2. Set "FFmpeg Path" to your FFmpeg executable
3. Click "Test Connection" to verify
4. Save settings

### Provider Paths

1. Go to Settings → Providers
2. Configure paths for:
   - Stable Diffusion WebUI (if using local image generation)
   - Ollama (if using local LLM)
   - Custom FFmpeg location
3. Test each provider connection
4. Save settings

### Output Directories

1. Go to Settings → General
2. Configure directories for:
   - Projects (where project files are saved)
   - Renders (where videos are exported)
   - Downloads (where components are downloaded)
3. Ensure you have write permissions to these locations

## Troubleshooting

### "Failed to run diagnostics"

**Possible causes**:
- API server not running
- Port 5005 in use by another application
- Firewall blocking localhost connections

**Solutions**:
1. Check if API is running: Visit http://127.0.0.1:5005/api/healthz
2. Check logs in `Logs/` directory for error messages
3. Restart the application
4. Check firewall settings to allow localhost

### "Component installation failed"

**Possible causes**:
- No internet connection
- Antivirus blocking downloads
- Insufficient disk space
- Download server unavailable

**Solutions**:
1. Check internet connection
2. Try again later (servers may be temporarily down)
3. Download manually and extract to appropriate folder
4. Check antivirus quarantine for blocked files

### "Validation failed after installation"

**Possible causes**:
- Installation incomplete
- Files corrupted during download
- Antivirus deleted files

**Solutions**:
1. Re-run diagnostics to see specific failures
2. Check if files exist in expected locations
3. Re-install the component
4. Check antivirus logs

## Getting Help

If you're still experiencing issues:

1. **Check Logs**: Review `Logs/aura-api-*.log` for error details
2. **Run Diagnostics**: Click "Run Diagnostics" on Welcome page
3. **Review Documentation**: Check README.md and other guides
4. **Report Issue**: Create a GitHub issue with:
   - Operating system and version
   - Error messages from diagnostics
   - Relevant log snippets
   - Steps to reproduce the issue

## Best Practices

For the smoothest first-run experience:

1. ✅ **Extract to Local Drive**: Use `C:\Aura` or `Documents\Aura`
2. ✅ **Check Disk Space**: Ensure at least 10 GB free
3. ✅ **Stable Internet**: Download components may be large (100-500 MB)
4. ✅ **Antivirus Exclusion**: Add Aura to exclusion list before extracting
5. ✅ **Close Other Apps**: Free up RAM and resources
6. ✅ **Run Diagnostics**: Use built-in diagnostics to identify issues early
7. ✅ **Follow Wizard**: Complete the onboarding wizard for guided setup

## System Requirements

### Minimum:
- **OS**: Windows 10/11, Linux (Ubuntu 20.04+)
- **CPU**: Dual-core processor
- **RAM**: 4 GB (8 GB recommended)
- **Storage**: 5 GB free space
- **Internet**: For downloading components and cloud providers

### Recommended:
- **OS**: Windows 11
- **CPU**: Quad-core processor or better
- **RAM**: 16 GB or more
- **GPU**: NVIDIA GPU with 6+ GB VRAM (for local image generation)
- **Storage**: 20 GB free space
- **Internet**: Broadband connection

## Portable Mode

Aura Video Studio runs in portable mode by default:

- ✅ **No Installation**: Extract and run
- ✅ **Self-Contained**: All components in one folder
- ✅ **No Registry**: Doesn't modify Windows registry
- ✅ **No Admin**: Runs without administrator privileges
- ✅ **Movable**: Can be moved to different drives/systems
- ✅ **Clean Removal**: Just delete the folder

Data is stored relative to the application:
- `Tools/` - FFmpeg, Ollama, other tools
- `AuraData/` - Configuration and metadata
- `Projects/` - Your video projects
- `Logs/` - Application logs
- `Downloads/` - Downloaded components

## Next Steps

After successful first run:

1. **Create Your First Video**: Click "Create Video" on the Welcome page
2. **Explore Features**: Try different providers and settings
3. **Download Optional Components**: Install Ollama or Stable Diffusion for enhanced features
4. **Configure API Keys**: Add Pro provider keys in Settings (optional)
5. **Customize Settings**: Adjust preferences to your workflow

---

**Note**: First-run diagnostics run automatically but can be triggered anytime from the Welcome page by clicking "Run Diagnostics".
