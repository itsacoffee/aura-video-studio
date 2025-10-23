# Local Engines System - Overview

Aura Video Studio supports local, offline engines for image generation and text-to-speech, giving you full control over your creative pipeline without requiring cloud services or API keys.

## Engine Installation Modes

Aura supports **two modes** for managing engines, giving you flexibility in how you work:

### Managed Mode (App-Controlled)
- **Fully automated**: Aura handles installation, updates, and lifecycle management
- **Start/Stop control**: Use Aura's UI to start/stop engines
- **Auto-restart**: Optional automatic restart if engines crash
- **Process monitoring**: Real-time status, health checks, and log capture
- **Best for**: Users who want a hands-off, integrated experience

### External Mode (User-Managed)
- **Bring your own**: Point Aura to engines you've already installed
- **You control**: Start/stop engines manually using their own interfaces
- **Flexible paths**: Install anywhere on your system (not forced into Aura's directory)
- **Aura detects**: App monitors health and provides "Open Folder" / "Open Web UI" shortcuts
- **Best for**: Advanced users with existing setups, or testing custom configurations

### How to Choose
- **New to AI engines?** Use **Managed Mode** - let Aura handle everything
- **Already have SD WebUI/ComfyUI/Ollama?** Use **External Mode** - attach your existing installs
- **Want control?** Use **External Mode** - manage engines your way
- **Want convenience?** Use **Managed Mode** - one-click install and start

### Attaching Existing Engines

To attach an existing engine installation:

1. Navigate to **Download Center → Engines** tab
2. Find the engine you want to attach (e.g., "Stable Diffusion WebUI")
3. Click **"Attach Existing Install"** button
4. Provide the following information:
   - **Install Path**: Absolute path to the installation directory (e.g., `C:\Tools\sd-webui`)
   - **Executable Path** (optional): Path to the main executable or start script (e.g., `webui.bat`)
   - **Port** (optional): Web UI port number (e.g., `7860`)
   - **Health Check URL** (optional): URL endpoint for health checks (e.g., `http://localhost:7860/internal/ping`)
   - **Notes** (optional): Any notes about this installation
5. Click **"Attach"**

Aura will validate the paths and add the engine to your instances list.

### Managing Engine Instances

Once attached or installed, your engines appear in the **Engine Instances** section:

- **Mode badge**: Shows whether it's "Managed" (app-controlled) or "External" (user-managed)
  - Hover over the badge to see a tooltip explaining the mode
- **Status badge**: Shows current status (installed, running, not_installed)
- **Health badge**: Shows if the engine is healthy when running
- **Path display**: Full path to installation (click copy icon to copy to clipboard)
- **Port display**: Port number (click copy icon to copy)
- **Executable display**: Path to executable if specified (click copy icon to copy)
- **Actions**:
  - **Open Folder**: Opens the installation directory in your file explorer
  - **Open Web UI**: Opens the engine's web interface in your browser (if applicable)
  - **Start/Stop**: For Managed engines only

### Example: Attaching FFmpeg

If you already have FFmpeg installed:

1. Click **"Attach Existing Install"** on the FFmpeg card
2. **Install Path**: `C:\Tools\ffmpeg` (or `/usr/local/bin` on Linux)
3. **Executable Path**: `C:\Tools\ffmpeg\bin\ffmpeg.exe` (or `/usr/local/bin/ffmpeg`)
4. Click **"Attach"**

Aura will verify the path and add FFmpeg to your instances.

### Example: Attaching Stable Diffusion WebUI

If you already have SD WebUI running:

1. Click **"Attach Existing Install"** on the Stable Diffusion WebUI card
2. **Install Path**: `C:\Tools\stable-diffusion-webui`
3. **Executable Path**: `C:\Tools\stable-diffusion-webui\webui-user.bat`
4. **Port**: `7860` (or your custom port)
5. **Health Check URL**: `http://localhost:7860/internal/ping`
6. **Notes**: "My custom SD WebUI with extra models"
7. Click **"Attach"**

Now you can use "Open Folder" and "Open Web UI" buttons to quickly access your installation.

## Supported Engines

### Essential Tools
- **FFmpeg** 🎬 - Video and audio processing toolkit (required for all video operations)
  - Works on all systems (CPU-based)
  - No special hardware requirements
  
- **Ollama** 🤖 - Local LLM for script generation
  - CPU-based but GPU-accelerated when available
  - No API keys required for local AI narration

### Image Generation
- **Stable Diffusion WebUI (A1111)** 🎨 - Full-featured SD WebUI with API support
- **ComfyUI** 🔗 - Node-based stable diffusion interface

**Requirements:**
- NVIDIA GPU with CUDA support
- Minimum 6GB VRAM for SD 1.5
- Minimum 12GB VRAM for SDXL
- Windows 11 x64 or Linux (Ubuntu 20.04+)

### Text-to-Speech
- **Piper** 🎙️ - Fast, lightweight, offline TTS
  - Low latency, excellent for real-time use
  - Multiple voice models (English, Spanish, French, German, etc.)
  - No GPU required
  
- **Mimic3** 🗣️ - High-quality offline TTS server
  - Better voice quality than Piper
  - HTTP API for easy integration
  - Supports multiple voices and languages

- **Windows SAPI** - Built-in Windows TTS (always available)

## Hardware Gating and "Install Anyway" Feature

**All engines are now always visible** in the Download Center, regardless of your hardware. This allows you to:

### Benefits of Pre-Installation
- **Plan ahead**: Install engines now, use them when you upgrade your hardware
- **Portability**: Install on one machine, copy to another with better hardware
- **Learning**: Explore configurations and settings before getting the required hardware

### How It Works
1. **Hardware Detection**: Aura automatically detects your GPU and VRAM using nvidia-smi
2. **Visual Warnings**: Engines that don't meet your current hardware show a ⚠️ warning
3. **Install Anyway**: You can still install with the "Install anyway (for later)" button
4. **Auto-Start Gating**: Engines won't auto-start if hardware requirements aren't met
5. **Manual Override**: You can attempt to start manually (may fail due to insufficient resources)

### Warning Examples
- "Requires NVIDIA GPU" - No NVIDIA GPU detected
- "Requires 6GB VRAM (detected: 4GB)" - GPU found but insufficient VRAM
- "CPU-based tool, no GPU required" - Works on all systems

**GPU Detection and VRAM Requirements:**

Aura automatically detects your GPU and VRAM using multiple methods:
1. **nvidia-smi** (primary method for NVIDIA GPUs)
2. **Windows Management Instrumentation (WMI)** (fallback)
3. **dxdiag** (secondary fallback for accurate VRAM detection)

**Installation Behavior:**
- All engines are **always available for installation**, regardless of hardware
- Engines with unmet requirements show warnings but can still be installed
- Engines won't auto-start if hardware requirements aren't met
- You can manually attempt to start them (useful for testing or troubleshooting)

**Recommended GPU Tiers:**
- **6GB+**: SD 1.5 models supported
- **8GB+**: SD 1.5 with higher quality settings
- **12GB+**: SDXL models supported (recommended for best quality)

**Fallback Options:**
If your GPU doesn't meet these requirements, Aura can use:
- Stock images from Pexels/Pixabay/Unsplash (with free API keys)
- Pro cloud providers (Stability AI, Runway) if API keys are configured

**Note**: Hardware requirements are guidance, not hard blocks. You can install and experiment with any engine.

## Installation

### Portable Installation
Local engines are installed to:
- **Windows**: `%LOCALAPPDATA%\Aura\Tools\`
- **Linux**: `~/.local/share/aura/tools/`

No administrator privileges required. All engines and their data are stored in your user directory.

### Using the Download Center (Recommended)

The Download Center provides a graphical interface for managing engines:

1. **Open Aura Video Studio**
2. **Navigate to Download Center**
   - Click on "Downloads" in the navigation menu, or
   - Go to **Settings → Download Center**
3. **Switch to Engines Tab**
   - Click on the "Engines" tab at the top of the page
4. **Install an Engine**
   - Find the engine you want (Stable Diffusion, ComfyUI, Piper, Mimic3)
   - Click the **Install** button
   - Wait for the download and installation to complete
   - You'll see progress updates and status changes
5. **Start the Engine**
   - Once installed, click the **Start** button
   - The engine will launch in the background
   - Status will change to "Running" with a health check indicator
6. **Monitor and Manage**
   - View real-time status (Not Installed / Installed / Running)
   - Check health status (Healthy / Unreachable)
   - View process ID and log file location
   - Access additional actions via the menu (⋯):
     - **Verify**: Check installation integrity
     - **Repair**: Fix corrupted installations
     - **Open Folder**: Open the installation directory
     - **Remove**: Uninstall the engine

### Engine Status Indicators

The Download Center shows detailed status for each engine:

- **Not Installed**: Engine is available but not yet installed
- **Installed**: Engine files are present and verified
- **Running**: Engine is actively running
- **Health Status**:
  - **Healthy**: Engine is running and responding to API calls
  - **Unreachable**: Engine is running but not responding (may still be starting up)

### Manual Installation

For advanced users or offline scenarios, see:
- [Stable Diffusion Setup](./ENGINES_SD.md)
- [Local TTS Setup](./TTS_LOCAL.md)

## Where Are My Files?

Understanding where Aura stores your engines, models, and generated content is important for managing disk space, adding custom models, and troubleshooting.

### Default Installation Paths

By default, Aura installs engines in the following locations:

**Windows:**
```
%USERPROFILE%\.aura\engines\
├── ffmpeg\              # FFmpeg binaries
├── stable-diffusion-webui\  # SD WebUI installation
├── comfyui\             # ComfyUI installation
├── piper\               # Piper TTS
└── mimic3\              # Mimic3 TTS
```

**Linux/macOS:**
```
~/.aura/engines/
├── ffmpeg/              # FFmpeg binaries
├── stable-diffusion-webui/  # SD WebUI installation
├── comfyui/             # ComfyUI installation
├── piper/               # Piper TTS
└── mimic3/              # Mimic3 TTS
```

### Finding Your Installation Paths

1. **From Onboarding Wizard**:
   - After successful validation, the wizard shows a "📂 Where are my files?" section
   - Lists all installed engines with exact paths
   - Click "Open Folder" to browse to any engine's directory

2. **From Download Center**:
   - Navigate to **Download Center → Engines** tab
   - Each engine card shows its installation path
   - Use the "Open Folder" button to open in file explorer
   - Use the "Copy Path" icon to copy the path to clipboard

3. **From Settings**:
   - Navigate to **Settings → Local Engines**
   - View all configured engine instances with paths
   - Edit or reconfigure paths as needed

### Adding Custom Models

#### Stable Diffusion Models

To add your own Stable Diffusion models:

1. Locate your SD installation folder (use "Open Folder" button)
2. Navigate to `models/Stable-diffusion/`
3. Copy your `.safetensors` or `.ckpt` model files here
4. Restart SD WebUI or click "Refresh" in the Web UI
5. Models will appear in the model dropdown

**Example paths:**
- Windows: `C:\Users\YourName\.aura\engines\stable-diffusion-webui\models\Stable-diffusion\`
- Linux: `~/.aura/engines/stable-diffusion-webui/models/Stable-diffusion/`

Popular model locations:
- [Civitai](https://civitai.com/) - Community models
- [Hugging Face](https://huggingface.co/models?pipeline_tag=text-to-image) - Open source models

#### Piper TTS Voices

To add additional Piper voice models:

1. Download voice files from [Piper Voices](https://github.com/rhasspy/piper/releases)
2. Open Piper installation folder
3. Place `.onnx` and `.onnx.json` files in `voices/` directory
4. Restart Aura or refresh the TTS provider list

**Example path:**
- Windows: `C:\Users\YourName\.aura\engines\piper\voices\`
- Linux: `~/.aura/engines/piper/voices/`

#### ComfyUI Custom Nodes

To add custom nodes to ComfyUI:

1. Open ComfyUI installation folder
2. Navigate to `custom_nodes/`
3. Clone or copy custom node repositories here
4. Restart ComfyUI
5. Nodes will appear in the ComfyUI interface

### Generated Content

Your generated videos and assets are stored in:

**Windows:**
```
%USERPROFILE%\.aura\projects\
└── [project-id]\
    ├── script.json          # Generated script
    ├── audio\               # TTS output
    ├── images\              # Generated/stock images
    └── output\              # Final rendered videos
```

**Linux/macOS:**
```
~/.aura/projects/
└── [project-id]/
    ├── script.json          # Generated script
    ├── audio/               # TTS output
    ├── images/              # Generated/stock images
    └── output/              # Final rendered videos
```

### Disk Space Considerations

Typical installation sizes:

- **FFmpeg**: ~100 MB
- **Stable Diffusion WebUI**: ~4-8 GB (varies by models)
- **ComfyUI**: ~2-6 GB (varies by models)
- **Piper TTS**: ~50-200 MB per voice model
- **Mimic3 TTS**: ~100-500 MB per voice model
- **SD Models**: 2-7 GB each (depending on model size)

Projects can vary widely:
- Simple project: ~50-200 MB
- Complex project with many generated images: ~500 MB - 2 GB

**Tips to manage disk space:**
1. Remove old projects you no longer need
2. Delete unused SD models
3. Use the "Remove" button in Download Center to uninstall engines cleanly
4. Consider using external drives for model storage (configure custom paths)

### Accessing Web UIs

For engines with web interfaces (SD WebUI, ComfyUI):

1. **From Onboarding**: Click "Open Web UI" in the file locations summary
2. **From Download Center**: Click "Open Web UI" on the engine card
3. **Manually**: Navigate to `http://localhost:7860` (SD WebUI) or `http://localhost:8188` (ComfyUI)

**Default ports:**
- Stable Diffusion WebUI: `7860`
- ComfyUI: `8188`
- Mimic3: `59125`

*Note: Ports can be customized during installation or via Settings → Local Engines*

### Backing Up Your Setup

To backup your Aura configuration:

1. **Engine installations**: Copy `~/.aura/engines/` directory
2. **Projects**: Copy `~/.aura/projects/` directory  
3. **Settings**: Copy `%APPDATA%/aura/` (Windows) or `~/.config/aura/` (Linux/macOS)

This allows you to restore your complete setup on a new machine or after reinstallation.

## Engine Management

### Starting and Stopping Engines

Engines can be managed in two ways:

#### From Download Center (Recommended)

The Download Center provides a complete UI for engine management:

1. **Navigate to Download Center → Engines tab**
2. **Starting Engines**:
   - Click **Start** button on any installed engine
   - Monitor status change from "Installed" to "Running"
   - Health indicator shows "Healthy" when ready
   - View process ID and log file location
3. **Stopping Engines**:
   - Click **Stop** button on running engines
   - Engine will shut down gracefully
4. **Real-time Monitoring**:
   - Status updates every 5 seconds
   - Health checks for API availability
   - Error messages displayed inline
   - Log file paths shown for debugging

#### From Command Line

For automation or advanced users, use PowerShell scripts in `scripts/engines/`:
```powershell
# Windows
.\scripts\engines\launch_sd.ps1
.\scripts\engines\launch_piper.ps1
.\scripts\engines\launch_mimic3.ps1
```

### Health Checks

Aura automatically monitors engine health through HTTP endpoints:

- **Stable Diffusion WebUI**: Checks `/sdapi/v1/sd-models`
  - Polls every 2 seconds during startup (up to 120s timeout)
  - Verifies API is accessible and models are loaded
  
- **ComfyUI**: Checks `/system_stats`
  - Polls every 2 seconds during startup (up to 60s timeout)
  - Verifies node system is ready
  
- **Mimic3**: Checks `/api/voices`
  - Polls every 2 seconds during startup (up to 30s timeout)
  - Verifies voice models are available
  
- **Piper**: Binary validation
  - Checks executable exists and has correct permissions

**Health Status Indicators:**
- **Healthy (✓)**: Engine is running and API is responding
- **Unreachable**: Engine process is running but not responding to health checks (may still be starting up)
- **Not Running**: Engine is stopped

If an engine becomes unhealthy, Aura will:
1. Log the issue to engine-specific log files
2. Display error messages in the UI
3. Attempt to restart (if auto-restart is enabled)
4. Fall back to alternative providers if available

### Auto-Launch on Startup

Configure engines to start automatically when Aura launches:

1. Install and configure the engine through Download Center
2. Use the registry configuration file to enable auto-start
3. Engine will launch in the background on app startup
4. Check status in Download Center → Engines tab

*Note: UI for auto-launch configuration coming in a future update*

### Maintenance Operations

The Download Center provides maintenance tools:

- **Verify**: Check installation integrity
  - Validates all required files are present
  - Checks executable permissions
  - Reports missing or corrupted files
  
- **Repair**: Fix corrupted installations
  - Removes existing installation
  - Re-downloads and reinstalls the engine
  - Preserves configuration settings
  
- **Remove**: Clean uninstallation
  - Stops the engine if running
  - Removes all engine files
  - Cleans up registry entries
  - Preserves logs for troubleshooting

## Provider Selection and Fallback

Aura uses intelligent provider selection with automatic fallback:

### TTS Provider Priority
```
Pro Tier:
  ElevenLabs → PlayHT → Mimic3 → Piper → Windows

Free/Local Tier:
  Mimic3 → Piper → Windows
```

### Visual Provider Priority
```
Pro Tier:
  Stability AI → Runway → Local SD → Stock

Local Tier:
  Local SD → Stock

Free Tier:
  Stock
```

### Offline Mode
When offline mode is enabled, Aura will:
- Skip all cloud providers
- Use only local engines (SD, Piper, Mimic3)
- Fall back to stock assets and Windows TTS

## Performance Considerations

### Download and Installation Performance

Aura optimizes engine installation with:
- **Parallel downloads**: Multiple components can be downloaded simultaneously
- **Progressive SHA-256 verification**: Shows real-time progress during checksum verification
- **Resume support**: Interrupted downloads can be resumed (where supported)
- **Intelligent caching**: Previously downloaded components are reused

**Typical Installation Times** (on 100 Mbps connection):
- FFmpeg: ~2 minutes (80MB download)
- Piper TTS: ~5 minutes (varies by voice model)
- Stable Diffusion WebUI: ~15-30 minutes (2.5GB+ download)

### Stable Diffusion
- **SDXL**: 30-60 seconds per image (12GB+ VRAM)
- **SD 1.5**: 10-30 seconds per image (6GB+ VRAM)
- Consider batch generation for multiple scenes

### TTS Performance
- **Piper**: ~100x real-time (very fast)
- **Mimic3**: ~10x real-time (slower but higher quality)
- **Windows SAPI**: ~5x real-time

## Troubleshooting

### GPU Detection Issues

#### GPU Not Detected
If Aura doesn't detect your NVIDIA GPU:

1. **Verify nvidia-smi is working:**
   ```powershell
   nvidia-smi
   ```
   If this fails, your NVIDIA drivers may not be installed correctly.

2. **Check WMI detection:**
   ```powershell
   Get-WmiObject Win32_VideoController | Select-Object Name, AdapterRAM
   ```
   This should show your GPU and available memory.

3. **Manually verify VRAM with dxdiag:**
   - Press `Win + R`, type `dxdiag`, press Enter
   - Go to "Display" tab
   - Look for "Dedicated Memory" or "Display Memory"
   - This shows your VRAM in MB (e.g., 8192 MB = 8 GB)

4. **Driver Installation:**
   - Download latest NVIDIA drivers from https://www.nvidia.com/drivers
   - Use "Clean installation" option during setup
   - Reboot after installation
   - Verify with `nvidia-smi`

#### Incorrect VRAM Detected
If Aura shows wrong VRAM amount:

- **WMI AdapterRAM is often unreliable** - Aura uses multiple detection methods
- Primary method: nvidia-smi (most accurate)
- Fallback 1: WMI VideoController
- Fallback 2: dxdiag parsing
- If all methods fail, manual override is available in Settings

#### Manual GPU Override
If automatic detection fails, you can manually configure your GPU:

1. Go to **Settings → System Profile → Hardware**
2. Disable "Auto-detect"
3. Select your GPU from preset list (e.g., "NVIDIA RTX 3080 - 10GB")
4. Or enter custom values for VRAM

### Stable Diffusion won't start
1. Check GPU requirements: `nvidia-smi` (Windows/Linux)
2. Verify installation path exists
3. Check logs at `%LOCALAPPDATA%\Aura\logs\tools\stable-diffusion-webui.log`
4. Ensure ports are not in use (default: 7860)

### Piper/Mimic3 not working
1. Verify voice models are installed
2. Check binary path is correct
3. Review logs at `%LOCALAPPDATA%\Aura\logs\tools\`
4. Try manual synthesis test

### Port conflicts
If default ports are in use:
1. Go to **Download Center → Engines**
2. Click **Start** with custom port
3. Or configure via API (see API Reference below)

## API Reference

Aura provides REST API endpoints for programmatic engine management:

### List Engines
```http
GET /api/engines/list
```
Returns all available engines with installation status.

**Response:**
```json
{
  "engines": [
    {
      "id": "stable-diffusion-webui",
      "name": "Stable Diffusion WebUI",
      "version": "1.7.0",
      "description": "AUTOMATIC1111's Stable Diffusion WebUI",
      "sizeBytes": 2500000000,
      "defaultPort": 7860,
      "isInstalled": true,
      "installPath": "%LOCALAPPDATA%\\Aura\\Tools\\stable-diffusion-webui"
    }
  ]
}
```

### Get Engine Status
```http
GET /api/engines/status?engineId={engineId}
```
Returns detailed status for a specific engine.

**Response:**
```json
{
  "engineId": "stable-diffusion-webui",
  "name": "Stable Diffusion WebUI",
  "status": "running",
  "installedVersion": "1.7.0",
  "isRunning": true,
  "port": 7860,
  "health": "healthy",
  "processId": 12345,
  "logsPath": "%LOCALAPPDATA%\\Aura\\logs\\tools\\stable-diffusion-webui.log",
  "messages": []
}
```

### Install Engine
```http
POST /api/engines/install
Content-Type: application/json

{
  "engineId": "stable-diffusion-webui",
  "version": "1.7.0",  // optional
  "port": 7860          // optional
}
```

### Start Engine
```http
POST /api/engines/start
Content-Type: application/json

{
  "engineId": "stable-diffusion-webui",
  "port": 7860,         // optional
  "args": "--api"       // optional
}
```

### Stop Engine
```http
POST /api/engines/stop
Content-Type: application/json

{
  "engineId": "stable-diffusion-webui"
}
```

### Verify Engine
```http
POST /api/engines/verify
Content-Type: application/json

{
  "engineId": "stable-diffusion-webui"
}
```

**Response:**
```json
{
  "engineId": "stable-diffusion-webui",
  "isValid": true,
  "status": "Valid",
  "missingFiles": [],
  "issues": []
}
```

### Repair Engine
```http
POST /api/engines/repair
Content-Type: application/json

{
  "engineId": "stable-diffusion-webui"
}
```

### Remove Engine
```http
POST /api/engines/remove
Content-Type: application/json

{
  "engineId": "stable-diffusion-webui"
}
```

## Security and Privacy

### Local Processing
- All generation happens on your machine
- No data sent to external services
- Full control over models and data

### Network Access
Local engines only bind to `127.0.0.1` (localhost) by default:
- **SD WebUI**: `http://127.0.0.1:7860`
- **Mimic3**: `http://127.0.0.1:59125`

### Model Safety
- Models are downloaded from official sources
- SHA-256 checksums verified on installation
- See manifest for source URLs and licenses

## Advanced Configuration

### Engine Manifest Format

Aura uses a JSON manifest to define available engines and their properties. The manifest includes:

**Core Properties:**
- `id`: Unique identifier for the engine
- `name`: Display name
- `version`: Version string
- `description`: Brief description
- `sizeBytes`: Download size in bytes
- `sha256`: Checksum for verification (optional)
- `archiveType`: `zip`, `tar.gz`, or `git`
- `urls`: Platform-specific download URLs
- `entrypoint`: Executable or script to run

**GPU and Hardware:**
- `requiredVRAMGB`: Minimum VRAM in GB
- `vramTooltip`: User-friendly tooltip explaining VRAM requirements
- `icon`: Emoji or icon for UI display
- `tags`: Array of tags for filtering (`["nvidia-only", "gpu-intensive", "cpu", "fast"]`)

**Runtime:**
- `defaultPort`: Default network port
- `argsTemplate`: Command-line arguments template
- `healthCheck`: URL path and timeout for health checks
- `licenseUrl`: Link to license information

**Example Entry:**
```json
{
  "id": "stable-diffusion-webui",
  "name": "Stable Diffusion WebUI",
  "version": "1.9.0",
  "requiredVRAMGB": 6,
  "vramTooltip": "Minimum 6GB VRAM for SD 1.5, 12GB+ recommended for SDXL",
  "icon": "🎨",
  "tags": ["image-generation", "ai", "nvidia-only", "gpu-intensive"],
  "healthCheck": {
    "url": "/sdapi/v1/sd-models",
    "timeoutSeconds": 120
  }
}
```

**Manifest Location:**
- Bundled: `Aura.Core/Downloads/engine_manifest.json`
- Runtime: `%LOCALAPPDATA%\Aura\engines-manifest.json`

### Custom Model Paths
Edit `appsettings.json`:
```json
{
  "Engines": {
    "StableDiffusion": {
      "ModelPath": "C:\\path\\to\\custom\\models"
    }
  }
}
```

### Environment Variables
Set custom environment for engines:
- `PYTHONPATH`: Custom Python path
- `HF_HOME`: Hugging Face cache directory
- `CUDA_VISIBLE_DEVICES`: Restrict GPU usage

### Advanced Arguments
Pass custom arguments to engines:
1. Go to **Settings → Engines → Advanced**
2. Edit command-line arguments
3. Restart engine for changes to take effect

## Uninstalling Engines

### From UI
1. Go to **Settings → Download Center → Engines**
2. Click **Remove** on the engine
3. Confirm deletion

### Manual Cleanup
Delete the engine directory:
- Windows: `%LOCALAPPDATA%\Aura\Tools\{engine-name}`
- Linux: `~/.local/share/aura/tools/{engine-name}`

## Support and Resources

- [Stable Diffusion Setup Guide](./ENGINES_SD.md)
- [Local TTS Setup Guide](./TTS_LOCAL.md)
- [GitHub Issues](https://github.com/Coffee285/aura-video-studio/issues)
- [Community Discord](#) (planned)

## License Notes

- **Piper**: MIT License
- **Mimic3**: AGPL-3.0 License
- **Stable Diffusion Models**: Varies by model (check model licenses)
- See `LICENSE` file for Aura Video Studio license

---

# Engine Management Modes

Aura Video Studio supports two modes for managing AI engines: **Managed** and **External**.

## Managed Mode (App-Controlled)
- Aura installs and manages the engine
- App can start, stop, and restart the engine
- Engine installed to app-controlled directory
- Automatic updates and health monitoring
- Suitable for new installations

## External Mode (User-Managed)
- You install and manage the engine yourself
- Aura detects and uses your existing installation
- You control starting/stopping the engine
- Useful for existing installations or custom setups
- Perfect for advanced users with specific requirements

## Attaching Existing Installations

You can attach any existing engine installation to Aura without reinstalling or moving files.

### Steps to Attach

1. Navigate to **Downloads → Engines** tab
2. Find the engine you want to attach (e.g., Stable Diffusion WebUI)
3. Click **"Attach Existing Install"**
4. Fill in the dialog:
   - **Install Path** (required): Absolute path to your installation directory
   - **Executable Path** (optional): Path to the main executable or start script
   - **Port** (optional): Web UI port number
   - **Health Check URL** (optional): URL for health checks
   - **Notes** (optional): Any notes about your installation
5. Click **"Attach"**

### Example: Attaching Stable Diffusion WebUI

```
Install Path: C:\AI\stable-diffusion-webui
Executable Path: C:\AI\stable-diffusion-webui\webui-user.bat
Port: 7860
Health Check URL: http://localhost:7860/sdapi/v1/sd-models
Notes: Custom installation with SDXL models
```

### Example: Attaching FFmpeg

```
Install Path: C:\Tools\ffmpeg
Executable Path: C:\Tools\ffmpeg\bin\ffmpeg.exe
Port: (leave empty - FFmpeg doesn't use a web UI)
Notes: System-wide FFmpeg installation
```

## Engine Instance Management

### Viewing Instances

All engine instances (both Managed and External) are shown in the **Engines** tab under "Engine Instances":

- **Mode Badge**: Shows whether the instance is Managed or External
- **Status Badge**: Shows current status (installed/running/not_installed)
- **Install Path**: Full path to the installation
- **Port**: Web UI port number (if applicable)
- **Notes**: Any custom notes you added

### Available Actions

#### Open Folder
Opens the engine installation folder in your system file explorer (Windows: `explorer.exe`, Linux: `xdg-open`, macOS: `open`)

#### Open Web UI
Opens the engine's web interface in your browser (for engines with web UIs)

#### Start/Stop (Managed Only)
Only available for Managed instances. External instances must be started/stopped manually.

## API Reference

See the main documentation for complete API details.

## Best Practices

### When to Use Managed Mode
- First-time installation
- Want automatic updates
- Don't want to manage dependencies manually
- Need app-controlled start/stop

### When to Use External Mode
- Already have the engine installed
- Custom configuration or modifications
- Shared installation across multiple apps
- Advanced user with specific requirements

### Tips for External Installations
1. **Use Absolute Paths**: Always provide full, absolute paths
2. **Verify Before Attaching**: Make sure the engine works independently first
3. **Document Your Setup**: Use the Notes field to record important details
4. **Keep Paths Accessible**: Don't move installations after attaching
5. **Test Health Checks**: Ensure the health check URL is correct
