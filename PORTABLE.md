# Aura Video Studio - Portable Edition

Welcome to Aura Video Studio Portable! This distribution contains everything you need to run Aura Video Studio without installation on Windows 11 (x64).

## 📦 What's Included

This portable distribution includes:

- **Aura.Api.exe** - Self-contained ASP.NET Core 8 backend (no .NET installation required)
- **Web UI** - React-based frontend served from `wwwroot`
- **Launch.bat** - One-click launcher with pre-flight checks
- **Configuration** - `appsettings.json` for customization
- **Portable Folder Structure** - Self-contained data directories

## 🚀 Quick Start

1. **Extract the ZIP** to any folder on your computer
2. **Run Launch.bat** - Double-click to start the application
3. **Use the app** - Your browser will open automatically to `http://127.0.0.1:5005`

That's it! No installation, no admin rights required.

## 📁 Folder Structure

After extraction, you'll see:

```
AuraVideoStudio/
├── Api/                    # Backend application
│   ├── Aura.Api.exe       # Main executable
│   └── wwwroot/           # Web UI files
├── AuraData/              # Application data (created on first run)
├── Projects/              # Your video projects
├── Downloads/             # Downloaded dependencies (Stable Diffusion models, etc.)
├── Logs/                  # Application logs
├── Tools/                 # Optional tools (FFmpeg, etc.)
├── config/                # Configuration files
├── Launch.bat             # Launcher script
├── README.md              # This file
└── version.json           # Version information
```

## 🔧 Configuration

### Basic Settings

Edit `appsettings.json` to customize:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "Urls": "http://127.0.0.1:5005",
  "AllowedHosts": "*"
}
```

### Custom Port

To run on a different port, edit `appsettings.json`:

```json
{
  "Urls": "http://127.0.0.1:8080"
}
```

Then access the app at `http://127.0.0.1:8080` instead.

## 🎥 FFmpeg Setup

Aura Video Studio requires FFmpeg for video rendering.

### Option 1: Automatic (Recommended)

The application includes a **Download Center** that can automatically download and configure FFmpeg for you on first run.

### Option 2: Manual Installation

If FFmpeg is already installed on your system:

1. Download FFmpeg from [ffmpeg.org](https://ffmpeg.org/download.html)
2. Extract to `Tools/ffmpeg/` folder (create if needed)
3. Or add FFmpeg to your Windows PATH

### Option 3: Bundled (Pre-configured)

Some distributions may come with FFmpeg pre-bundled in the `ffmpeg/` folder. If you see this folder, FFmpeg is already configured.

## 🖥️ Hardware Acceleration

Aura Video Studio supports hardware-accelerated video encoding:

- **NVIDIA GPUs**: NVENC (RTX 20/30/40 series recommended)
- **AMD GPUs**: AMF
- **Intel GPUs**: QuickSync
- **Fallback**: Software encoding (CPU-only)

The application will automatically detect your hardware and use the best available encoder.

## 🔍 Troubleshooting

### Application Won't Start

**Symptoms:** Launch.bat closes immediately or shows errors

**Solutions:**

1. **Check extraction** - Ensure all files were extracted from the ZIP
2. **Antivirus** - Add the folder to your antivirus exclusions
3. **Windows Defender** - Allow the application if prompted
4. **Port conflict** - Make sure port 5005 isn't already in use
5. **Check logs** - Look in `Logs/` folder for error details

### White Screen / Blank Page

**Symptoms:** Browser opens but shows white/blank page

**Solutions:**

1. **Wait for startup** - First launch may take 10-20 seconds
2. **Force refresh** - Press `Ctrl+Shift+R` in your browser
3. **Check diagnostics** - Visit `http://127.0.0.1:5005/diag` for system status
4. **Clear browser cache** - Try in private/incognito mode
5. **Check wwwroot** - Verify `Api/wwwroot/index.html` exists

### Server Not Ready Timeout

**Symptoms:** Launcher shows "Server did not respond after X attempts"

**Solutions:**

1. **Wait longer** - Server may still be starting (check Task Manager for Aura.Api.exe)
2. **Check firewall** - Ensure Windows Firewall isn't blocking port 5005
3. **Manual check** - Visit `http://127.0.0.1:5005/health/live` in your browser
4. **Restart** - Close all Aura processes and run Launch.bat again

### FFmpeg Not Found

**Symptoms:** Video rendering fails with "FFmpeg not available"

**Solutions:**

1. **Use Download Center** - Let the app download FFmpeg automatically
2. **Manual install** - Download from [ffmpeg.org](https://ffmpeg.org) and extract to `Tools/ffmpeg/`
3. **System PATH** - Install FFmpeg system-wide using `winget install ffmpeg`
4. **Verify** - Open Command Prompt and run `ffmpeg -version`

### Performance Issues

**Symptoms:** Slow video generation or rendering

**Solutions:**

1. **Hardware acceleration** - Ensure GPU drivers are up to date
2. **Disk space** - Keep at least 10GB free space
3. **Background apps** - Close unnecessary applications
4. **Provider settings** - Use local providers (Piper TTS, Stable Diffusion) for offline use
5. **Check GPU** - Visit `/diag` endpoint to see detected hardware

## 📊 System Requirements

### Minimum Requirements

- **OS**: Windows 11 (64-bit)
- **RAM**: 8GB
- **Storage**: 5GB free space
- **CPU**: Modern x64 processor
- **Network**: Internet connection (for AI providers)

### Recommended Requirements

- **OS**: Windows 11 (64-bit)
- **RAM**: 16GB or more
- **Storage**: 20GB free space (for models and cache)
- **CPU**: Multi-core processor (6+ cores)
- **GPU**: NVIDIA RTX 20-series or newer with 4GB+ VRAM
- **Network**: Broadband internet connection

## 🔐 Privacy & Security

### Data Storage

- All data stays on **your computer** in the portable folder
- No telemetry or tracking
- No external data collection
- Your projects and settings are fully portable

### API Keys

If you use premium AI providers (OpenAI, ElevenLabs, etc.):

- API keys are stored in `AuraData/settings.json`
- Keep this file secure and never share it
- Consider using environment variables for additional security

### Network Access

The application only makes network requests to:

- AI provider APIs (if configured)
- Dependency downloads (ffmpeg, models) when requested
- No other external connections

## 🆘 Getting Help

### Diagnostic Tools

1. **Health Check**: Visit `http://127.0.0.1:5005/health/ready`
2. **Diagnostics Page**: Visit `http://127.0.0.1:5005/diag`
3. **Log Files**: Check `Logs/` folder for detailed error information

### Support Resources

- **Documentation**: [github.com/itsacoffee/aura-video-studio](https://github.com/itsacoffee/aura-video-studio)
- **Issues**: Report bugs on GitHub Issues
- **Discussions**: Ask questions on GitHub Discussions

### Diagnostic Script

For automated diagnostics, developers can use:

```powershell
# From the portable folder
powershell -File scripts/diagnostics/diagnose-white-screen.ps1
```

## 🔄 Updates

### Check for Updates

The application includes an auto-update checker that will notify you when new versions are available.

### Manual Update

1. Download the latest portable ZIP from GitHub Releases
2. Extract to a new folder
3. Copy your `AuraData/` folder from the old version
4. Run Launch.bat from the new version

Your projects and settings will be preserved.

## 🚀 Advanced Usage

### Command Line Arguments

Run the API directly for advanced scenarios:

```batch
cd Api
Aura.Api.exe --urls "http://0.0.0.0:5005"
```

### Multiple Instances

To run multiple instances:

1. Copy the entire portable folder
2. Edit `appsettings.json` in each copy to use different ports
3. Run Launch.bat from each folder

### Offline Mode

For completely offline usage:

1. Download FFmpeg to `Tools/ffmpeg/`
2. Download Stable Diffusion model to `Downloads/models/`
3. Use local providers: Piper TTS, RuleBased LLM
4. Configure in Settings > Providers

## 📝 License

This software is distributed under the terms specified in the LICENSE file included in this distribution.

## 🎉 Getting Started

Ready to create your first video?

1. Launch the application
2. Click **"Quick Demo"** for a guided first experience
3. Or use **"Create Video"** wizard for custom content

Enjoy creating AI-powered videos with Aura Video Studio!

---

**Version**: Check `version.json` for build information  
**Platform**: Windows 11 (x64)  
**Build Type**: Self-contained portable distribution

For more information, visit: [github.com/itsacoffee/aura-video-studio](https://github.com/itsacoffee/aura-video-studio)
