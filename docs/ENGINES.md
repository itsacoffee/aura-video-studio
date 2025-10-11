# Local Engines System - Overview

Aura Video Studio supports local, offline engines for image generation and text-to-speech, giving you full control over your creative pipeline without requiring cloud services or API keys.

## Supported Engines

### Image Generation
- **Stable Diffusion WebUI (A1111)** - Full-featured SD WebUI with API support
- **ComfyUI** - Node-based stable diffusion interface (planned)

**Requirements:**
- NVIDIA GPU with CUDA support
- Minimum 6GB VRAM for SD 1.5
- Minimum 12GB VRAM for SDXL
- Windows 11 x64 or Linux (Ubuntu 20.04+)

### Text-to-Speech
- **Piper** - Fast, lightweight, offline TTS
  - Low latency, excellent for real-time use
  - Multiple voice models (English, Spanish, French, German, etc.)
  - No GPU required
  
- **Mimic3** - High-quality offline TTS server
  - Better voice quality than Piper
  - HTTP API for easy integration
  - Supports multiple voices and languages

- **Windows SAPI** - Built-in Windows TTS (always available)

## Installation

### Portable Installation
Local engines are installed to:
- **Windows**: `%LOCALAPPDATA%\Aura\Tools\`
- **Linux**: `~/.local/share/aura/tools/`

No administrator privileges required.

### Using the Download Center

1. Open Aura Video Studio
2. Navigate to **Settings → Download Center → Engines**
3. Select the engine you want to install
4. Click **Install**
5. Wait for download and extraction to complete
6. Click **Start** to launch the engine

### Manual Installation

For advanced users or offline scenarios, see:
- [Stable Diffusion Setup](./ENGINES_SD.md)
- [Local TTS Setup](./TTS_LOCAL.md)

## Engine Management

### Starting and Stopping Engines

Engines can be managed in two ways:

#### From Download Center
- Navigate to **Settings → Download Center → Engines**
- Use **Start** / **Stop** buttons for each engine
- View logs and health status in real-time

#### From Command Line
Use PowerShell scripts in `scripts/engines/`:
```powershell
# Windows
.\scripts\engines\launch_sd.ps1
.\scripts\engines\launch_piper.ps1
.\scripts\engines\launch_mimic3.ps1
```

### Health Checks

Aura automatically monitors engine health:
- **Stable Diffusion**: Checks `/sdapi/v1/sd-models` endpoint
- **Mimic3**: Checks `/api/voices` endpoint
- **Piper**: Validates binary exists and can be executed

If an engine becomes unhealthy, Aura will:
1. Log the issue
2. Attempt to restart (if auto-restart is enabled)
3. Fall back to alternative providers if available

### Auto-Launch on Startup

Configure engines to start when Aura launches:
1. Go to **Settings → Engines**
2. Toggle **Start on app launch** for each engine
3. Save settings

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

### Stable Diffusion
- **SDXL**: 30-60 seconds per image (12GB+ VRAM)
- **SD 1.5**: 10-30 seconds per image (6GB+ VRAM)
- Consider batch generation for multiple scenes

### TTS Performance
- **Piper**: ~100x real-time (very fast)
- **Mimic3**: ~10x real-time (slower but higher quality)
- **Windows SAPI**: ~5x real-time

## Troubleshooting

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
1. Go to **Settings → Engines**
2. Change the port number
3. Restart the engine

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
