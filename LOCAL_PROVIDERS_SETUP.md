# Local AI Providers Setup Guide

This guide explains how to set up and configure local AI tools for use with Aura Video Studio. Local providers run on your own hardware and don't require API keys or cloud services.

## Quick Start

1. **Download FFmpeg** (Required)
   - Open Aura → **Downloads** page
   - Click **Install** next to FFmpeg
   - Wait for download to complete

2. **Configure Providers**
   - Open Aura → **Settings** → **Local Providers** tab
   - Click **Test** next to FFmpeg to verify installation
   - (Optional) Configure Stable Diffusion or Ollama if installed

3. **Start Creating**
   - All set! Go to **Create** page to start generating videos

## Overview

Aura Video Studio supports the following local AI providers:

1. **Stable Diffusion WebUI** - For local image generation (requires NVIDIA GPU with 6GB+ VRAM)
2. **Ollama** - For local LLM text generation
3. **FFmpeg** - For video encoding and processing (required)

## Prerequisites

- **Windows 10/11** (64-bit)
- **NVIDIA GPU with 6GB+ VRAM** (for Stable Diffusion only)
- **16GB+ RAM** recommended for running multiple tools simultaneously

## 1. FFmpeg Setup (Required)

FFmpeg is essential for video rendering.

### Option A: Download via Aura (Recommended)

1. Open Aura Video Studio
2. Navigate to **Downloads** page
3. Click **Install** next to FFmpeg
4. The tool will automatically download and configure FFmpeg

### Option B: Manual Installation

1. Download FFmpeg from: https://www.gyan.dev/ffmpeg/builds/
   - Choose the "ffmpeg-release-essentials.zip" build
2. Extract to a folder (e.g., `C:\Tools\ffmpeg`)
3. In Aura, go to **Settings** → **Local Providers**
4. Set **FFmpeg Executable Path** to: `C:\Tools\ffmpeg\bin\ffmpeg.exe`
5. Set **FFprobe Executable Path** to: `C:\Tools\ffmpeg\bin\ffprobe.exe`
6. Click **Test** to verify
7. Click **Save Provider Paths**

### Option C: System PATH

1. Download and extract FFmpeg as above
2. Add FFmpeg's `bin` folder to your system PATH
3. Leave the path fields empty in Aura settings
4. FFmpeg will be found automatically

## 2. Stable Diffusion WebUI Setup (Optional - NVIDIA GPU Required)

Stable Diffusion allows local image generation for your videos.

### System Requirements

- **NVIDIA GPU** with 6GB+ VRAM (GTX 1660 Ti or better)
- 10GB+ free disk space

### Installation Steps

1. **Install Python 3.10.6**
   - Download from: https://www.python.org/downloads/release/python-3106/
   - During installation, check "Add Python to PATH"

2. **Install Git**
   - Download from: https://git-scm.com/download/win
   - Use default settings

3. **Download Stable Diffusion WebUI**
   ```powershell
   cd C:\
   git clone https://github.com/AUTOMATIC1111/stable-diffusion-webui.git
   cd stable-diffusion-webui
   ```

4. **First-time Setup**
   - Double-click `webui-user.bat`
   - Wait for the initial setup (downloads model, ~4GB)
   - The web interface will open at http://127.0.0.1:7860

5. **Configure in Aura**
   - Open Aura Video Studio
   - Go to **Settings** → **Local Providers**
   - Set **Stable Diffusion WebUI URL** to: `http://127.0.0.1:7860`
   - Click **Test Connection** (make sure WebUI is running)
   - Click **Save Provider Paths**

6. **Usage**
   - Always start `webui-user.bat` before using Aura's image generation
   - You can minimize the window but don't close it
   - To use a different model, download it to `stable-diffusion-webui/models/Stable-diffusion/`

### Recommended Models

- **SD 1.5** (4GB VRAM) - Default, good for most use cases
- **SDXL** (12GB VRAM) - Higher quality, requires more VRAM

### Troubleshooting

- **"Failed to connect" error**: Ensure webui-user.bat is running
- **Out of memory**: Lower your VRAM allocation or use SD 1.5 instead of SDXL
- **Slow generation**: This is normal - each image takes 20-60 seconds

## 3. Ollama Setup (Optional)

Ollama provides local LLM capabilities for script generation.

### System Requirements

- 8GB+ RAM (16GB recommended)
- 10GB+ free disk space

### Installation Steps

1. **Download Ollama**
   - Visit: https://ollama.ai/download
   - Download the Windows installer
   - Run the installer

2. **Install a Model**
   ```powershell
   # Open PowerShell and run:
   ollama pull llama3.1:8b
   ```
   This downloads the Llama 3.1 8B model (~4.7GB)

3. **Start Ollama**
   ```powershell
   ollama serve
   ```
   Ollama runs on http://127.0.0.1:11434 by default

4. **Configure in Aura**
   - Open Aura Video Studio
   - Go to **Settings** → **Local Providers**
   - Set **Ollama URL** to: `http://127.0.0.1:11434`
   - Click **Test Connection**
   - Click **Save Provider Paths**

5. **Usage**
   - Ollama runs as a background service after installation
   - No need to manually start it each time
   - To use a different model: `ollama pull <model-name>`

### Recommended Models

- **llama3.1:8b** - Balanced performance and quality
- **mistral:7b** - Faster, good for quick drafts
- **llama3.1:70b** - Highest quality (requires 40GB+ RAM)

## Verifying Your Setup

After configuring all providers:

1. Open Aura Video Studio
2. Navigate to **Settings** → **Local Providers**
3. Click **Test** next to each configured provider
4. All tests should show green checkmarks with success messages

### Expected Test Results

- **FFmpeg**: Shows version number (e.g., "ffmpeg version 6.0")
- **Stable Diffusion**: "Successfully connected to Stable Diffusion WebUI"
- **Ollama**: "Successfully connected to Ollama"

## Configuration File Locations

Your settings are stored in:
```
%LOCALAPPDATA%\Aura\provider-paths.json
```

You can manually edit this file if needed.

## Performance Tips

1. **Memory Management**
   - Don't run all providers simultaneously on systems with less than 32GB RAM
   - Close Stable Diffusion WebUI when not generating images
   
2. **VRAM Optimization** (for Stable Diffusion)
   - Edit `webui-user.bat` and add: `set COMMANDLINE_ARGS=--medvram`
   - This reduces VRAM usage at the cost of speed

3. **Output Directory**
   - Set a custom output directory on your fastest drive
   - Go to **Settings** → **Local Providers** → **Output Directory**
   - Example: `D:\Videos\AuraOutput`

## Getting Help

If you encounter issues:

1. Check the test results in Settings → Local Providers
2. Ensure all services are running (Stable Diffusion WebUI, Ollama)
3. Verify your GPU meets the requirements for Stable Diffusion
4. Check that FFmpeg is properly installed and accessible

## Alternative: Cloud Providers

If local providers are not suitable for your system:

1. Go to **Settings** → **API Keys**
2. Configure cloud services (OpenAI, ElevenLabs, Stability AI, etc.)
3. These don't require local hardware but need API keys
4. See the main documentation for API key setup

## Next Steps

After setting up your providers:

1. Visit the **Downloads** page to install additional dependencies
2. Go to **Create** to start generating your first video
3. Choose "Free-Only" profile for local-only generation
4. Or "Balanced Mix" to use both local and cloud providers

---

**Note**: Local AI providers require significant hardware resources. If your system doesn't meet the requirements, consider using the cloud-based "Pro" providers instead.
