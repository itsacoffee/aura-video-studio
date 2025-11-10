# Aura Video Studio - Installation Guide

Welcome to Aura Video Studio! This guide will help you install and set up the application on your system.

## Table of Contents

- [Download](#download)
- [System Requirements](#system-requirements)
- [Platform-Specific Installation](#platform-specific-installation)
  - [Windows](#windows)
  - [macOS](#macos)
  - [Linux](#linux)
- [First Launch Setup](#first-launch-setup)
- [Installing Dependencies](#installing-dependencies)
- [Troubleshooting](#troubleshooting)

---

## Download

### Desktop Application (Recommended)

Download the latest installer for your platform:

**Windows:**
- [Aura-Video-Studio-Setup-1.0.0.exe](https://github.com/coffee285/aura-video-studio/releases/latest) (Installer)
- [Aura-Video-Studio-1.0.0-portable.exe](https://github.com/coffee285/aura-video-studio/releases/latest) (Portable)

**macOS:**
- [Aura-Video-Studio-1.0.0-universal.dmg](https://github.com/coffee285/aura-video-studio/releases/latest) (Universal - Intel + Apple Silicon)
- [Aura-Video-Studio-1.0.0-x64.dmg](https://github.com/coffee285/aura-video-studio/releases/latest) (Intel only)
- [Aura-Video-Studio-1.0.0-arm64.dmg](https://github.com/coffee285/aura-video-studio/releases/latest) (Apple Silicon only)

**Linux:**
- [Aura-Video-Studio-1.0.0.AppImage](https://github.com/coffee285/aura-video-studio/releases/latest) (Universal - no installation)
- [aura-video-studio_1.0.0_amd64.deb](https://github.com/coffee285/aura-video-studio/releases/latest) (Debian/Ubuntu)
- [aura-video-studio-1.0.0.x86_64.rpm](https://github.com/coffee285/aura-video-studio/releases/latest) (Fedora/RHEL)
- [aura-video-studio_1.0.0_amd64.snap](https://github.com/coffee285/aura-video-studio/releases/latest) (Snap)

### Web Version

Alternatively, you can run the web version (requires manual setup):
```bash
git clone https://github.com/coffee285/aura-video-studio.git
cd aura-video-studio
# See BUILD_GUIDE.md for instructions
```

---

## System Requirements

### Minimum Requirements

- **OS:** Windows 10+, macOS 10.15+, or Linux (Ubuntu 20.04+, Fedora 34+)
- **RAM:** 8 GB
- **Disk Space:** 10 GB free space
- **Processor:** Intel Core i5 / AMD Ryzen 5 or better
- **Internet:** Required for AI provider APIs

### Recommended Requirements

- **RAM:** 16 GB or more
- **Processor:** Intel Core i7 / AMD Ryzen 7 or better (8+ cores)
- **GPU:** Dedicated GPU with 4GB+ VRAM (optional, for faster rendering)
- **Disk Space:** 50 GB+ free space (for projects and media)
- **Internet:** High-speed connection for faster model downloads

---

## Platform-Specific Installation

### Windows

#### Installation Steps

1. **Download the Installer**
   - Download `Aura-Video-Studio-Setup-1.0.0.exe`

2. **Run the Installer**
   - Double-click the `.exe` file
   - If Windows SmartScreen appears, click "More info" → "Run anyway"
   - Follow the installation wizard

3. **Choose Installation Options**
   - Installation directory (default: `C:\Program Files\Aura Video Studio`)
   - Create desktop shortcut (recommended)
   - Add to Start Menu (recommended)
   - Auto-launch on startup (optional)

4. **Complete Installation**
   - Click "Install" and wait for completion
   - Click "Finish" to launch Aura Video Studio

#### Portable Version

If you prefer a portable installation:

1. Download `Aura-Video-Studio-1.0.0-portable.exe`
2. Move the file to your desired location (e.g., USB drive, `D:\Apps`)
3. Double-click to run (no installation required)
4. Settings and projects are stored in the same folder

#### Windows Defender / Antivirus

If Windows Defender blocks the installer:
- Right-click the `.exe` → Properties → Unblock → OK
- Or add an exception in Windows Security

---

### macOS

#### Installation Steps

1. **Download the DMG**
   - Choose the appropriate version:
     - Universal (works on Intel and Apple Silicon)
     - Intel-only (x64)
     - Apple Silicon-only (arm64)

2. **Open the DMG**
   - Double-click the `.dmg` file
   - A window will open showing the Aura app and Applications folder

3. **Install the App**
   - Drag the **Aura Video Studio** icon to the **Applications** folder
   - Wait for the copy to complete

4. **First Launch**
   - Open Applications folder
   - Right-click **Aura Video Studio** → Open
   - Click "Open" when prompted (required for first launch)

#### Gatekeeper Security

If you see "App cannot be opened because the developer cannot be verified":

**Method 1: Right-click to Open**
1. Right-click (or Control+click) the app
2. Select "Open"
3. Click "Open" in the dialog

**Method 2: Security Settings**
1. System Settings → Privacy & Security
2. Scroll to "Security" section
3. Click "Open Anyway" next to the Aura message
4. Enter your password to confirm

#### Code Signing Note

We're working on obtaining an Apple Developer certificate for automatic verification. Until then, you'll need to use the right-click method above.

---

### Linux

Linux users have multiple installation options:

#### AppImage (Recommended for Most Users)

**Advantages:** No installation, works on most distros, no root required

1. **Download the AppImage**
   ```bash
   wget https://github.com/coffee285/aura-video-studio/releases/latest/download/Aura-Video-Studio-1.0.0.AppImage
   ```

2. **Make it Executable**
   ```bash
   chmod +x Aura-Video-Studio-1.0.0.AppImage
   ```

3. **Run the App**
   ```bash
   ./Aura-Video-Studio-1.0.0.AppImage
   ```

4. **Optional: Desktop Integration**
   - On first launch, you'll be prompted to integrate with your desktop
   - This adds a menu entry and file associations

#### Debian / Ubuntu (.deb)

1. **Download and Install**
   ```bash
   wget https://github.com/coffee285/aura-video-studio/releases/latest/download/aura-video-studio_1.0.0_amd64.deb
   sudo dpkg -i aura-video-studio_1.0.0_amd64.deb
   sudo apt-get install -f  # Fix any dependency issues
   ```

2. **Launch**
   ```bash
   aura-video-studio
   ```
   Or find it in your application menu.

#### Fedora / RHEL (.rpm)

1. **Download and Install**
   ```bash
   wget https://github.com/coffee285/aura-video-studio/releases/latest/download/aura-video-studio-1.0.0.x86_64.rpm
   sudo dnf install ./aura-video-studio-1.0.0.x86_64.rpm
   ```

2. **Launch**
   ```bash
   aura-video-studio
   ```

#### Snap

1. **Install from Snap Store**
   ```bash
   sudo snap install aura-video-studio
   ```

2. **Grant Permissions**
   ```bash
   sudo snap connect aura-video-studio:home
   sudo snap connect aura-video-studio:removable-media
   ```

3. **Launch**
   ```bash
   snap run aura-video-studio
   ```

#### Flatpak (Coming Soon)

We're working on submitting to Flathub. Stay tuned!

---

## First Launch Setup

When you first launch Aura Video Studio, you'll be guided through an interactive setup wizard:

### Step 1: Welcome

Choose your setup mode:
- **Express Setup** (Recommended): Auto-detect and configure everything
- **Custom Setup**: Choose providers, paths, and settings manually

### Step 2: Dependency Check

The wizard will check and install:

#### FFmpeg (Required)

FFmpeg is required for video rendering.

**Windows:**
- Click "Install FFmpeg" to download and install automatically
- Or install manually: [FFmpeg Downloads](https://ffmpeg.org/download.html#build-windows)

**macOS:**
- Install via Homebrew (recommended):
  ```bash
  brew install ffmpeg
  ```
- Or download from [FFmpeg Downloads](https://ffmpeg.org/download.html#build-mac)

**Linux:**
- Ubuntu/Debian: `sudo apt install ffmpeg`
- Fedora: `sudo dnf install ffmpeg`
- Arch: `sudo pacman -S ffmpeg`

#### Ollama (Optional)

Ollama lets you run AI models locally (free, private, no API costs).

1. Click "Download Ollama" in the setup wizard
2. Install Ollama from [ollama.ai](https://ollama.ai/download)
3. Return to the wizard and click "Recheck"
4. Download a model:
   ```bash
   ollama pull llama3.2  # Lightweight model
   ```

Recommended models:
- `llama3.2` (4GB) - Fast, good quality
- `mistral` (4GB) - Balanced performance
- `llama3.1:70b` (40GB) - Best quality (requires 64GB+ RAM)

### Step 3: Provider Configuration

Configure your AI providers:

#### LLM Provider (Language Model)

Choose one:
- **Ollama** (Local, free, private) - Recommended if you installed Ollama
- **OpenAI** - Best quality, requires API key
- **Anthropic** - Great reasoning, requires API key
- **Google Gemini** - Fast, multimodal, requires API key
- **Rule-Based** - No AI, template-based, always free

#### TTS Provider (Text-to-Speech)

Choose one:
- **System TTS** (Free, uses OS voices)
- **OpenAI TTS** (High quality, requires API key)
- **ElevenLabs** (Premium voices, requires API key)

#### Image Generation

Choose one:
- **DALL-E** (OpenAI, requires API key)
- **Stable Diffusion** (Local or cloud, various options)
- **Unsplash** (Free stock photos, no API key)

### Step 4: Workspace Setup

Configure your workspace directories:

- **Projects Folder**: Where your video projects are saved
- **Output Folder**: Where rendered videos are exported
- **Media Library**: Where assets (images, audio) are stored

Default locations:
- Windows: `C:\Users\YourName\Videos\AuraProjects`
- macOS: `~/Movies/AuraProjects`
- Linux: `~/Videos/AuraProjects`

### Step 5: Complete!

Click "Start Creating" to begin using Aura Video Studio!

---

## Installing Dependencies

### FFmpeg Installation Details

#### Windows

**Option 1: Automatic (Recommended)**
1. Open Aura Video Studio
2. Go to Settings → System
3. Click "Install FFmpeg"

**Option 2: Manual**
1. Download FFmpeg: https://www.gyan.dev/ffmpeg/builds/
2. Extract to `C:\ffmpeg`
3. Add to PATH:
   - Search for "Environment Variables" in Start
   - Edit "Path" variable
   - Add `C:\ffmpeg\bin`

#### macOS

**Homebrew (Recommended):**
```bash
brew install ffmpeg
```

**Manual:**
1. Download from https://evermeet.cx/ffmpeg/
2. Move `ffmpeg` to `/usr/local/bin/`
3. Make executable: `chmod +x /usr/local/bin/ffmpeg`

#### Linux

**Ubuntu/Debian:**
```bash
sudo apt update
sudo apt install ffmpeg
```

**Fedora:**
```bash
sudo dnf install ffmpeg
```

**Arch:**
```bash
sudo pacman -S ffmpeg
```

### Ollama Installation

1. **Download Ollama**
   - Visit [ollama.ai/download](https://ollama.ai/download)
   - Download for your platform
   - Install the application

2. **Download Models**
   ```bash
   # Start with a small model
   ollama pull llama3.2
   
   # Or a larger, more capable model
   ollama pull llama3.1:13b
   ```

3. **Verify Installation**
   - Ollama should start automatically
   - Check: `http://localhost:11434` should be accessible
   - Test: `ollama list` to see installed models

---

## Troubleshooting

### Common Issues

#### "Backend failed to start"

**Symptoms:** App shows error on startup, backend can't connect

**Solutions:**
1. Check if port is already in use
2. Run as administrator/sudo
3. Check firewall/antivirus isn't blocking the app
4. View logs: Help → Open Logs Folder

#### "FFmpeg not found"

**Symptoms:** Cannot render videos, "FFmpeg required" error

**Solutions:**
1. Run diagnostics: Help → System Diagnostics
2. Install FFmpeg (see [Installing Dependencies](#installing-dependencies))
3. Verify installation: `ffmpeg -version` in terminal
4. Restart Aura Video Studio

#### "Provider authentication failed"

**Symptoms:** API calls fail, "Invalid API key" errors

**Solutions:**
1. Verify API key is correct (no extra spaces)
2. Check API key has sufficient credits/quota
3. Test API key: Settings → Providers → Test Connection
4. Try regenerating the API key on provider's website

#### macOS: "App is damaged and can't be opened"

**Solutions:**
1. Open Terminal and run:
   ```bash
   xattr -cr /Applications/Aura\ Video\ Studio.app
   ```
2. Right-click app → Open (don't double-click)

#### Linux: "Permission denied" when running AppImage

**Solutions:**
1. Make executable:
   ```bash
   chmod +x Aura-Video-Studio-1.0.0.AppImage
   ```
2. Install FUSE if needed:
   ```bash
   sudo apt install libfuse2  # Ubuntu/Debian
   sudo dnf install fuse      # Fedora
   ```

### Getting Help

If you're still having issues:

1. **Check Diagnostics**
   - Help → System Diagnostics
   - Click "Copy Report"
   - Include in your issue report

2. **Search Existing Issues**
   - [GitHub Issues](https://github.com/coffee285/aura-video-studio/issues)

3. **Create New Issue**
   - Include your diagnostics report
   - Describe what you expected vs. what happened
   - Include error messages and screenshots

4. **Community Support**
   - Discord: [Join our community](https://discord.gg/aura-video-studio)
   - Forum: [Community Forum](https://community.aura-video-studio.com)

---

## Next Steps

After installation:

1. **Complete the Setup Wizard**
   - Configure your preferred AI providers
   - Set up your workspace

2. **Explore Tutorials**
   - Help → Learning Center
   - [Video Tutorials](https://www.youtube.com/@aura-video-studio)

3. **Create Your First Video**
   - Use the Video Creation Wizard
   - Try a sample project

4. **Join the Community**
   - Share your creations
   - Get tips and tricks
   - Request features

---

## Uninstallation

### Windows

1. Open Settings → Apps → Installed Apps
2. Find "Aura Video Studio"
3. Click ⋯ → Uninstall
4. Choose whether to keep your projects and settings

### macOS

1. Open Applications folder
2. Drag "Aura Video Studio" to Trash
3. Empty Trash
4. Optional: Remove app data:
   ```bash
   rm -rf ~/Library/Application\ Support/AuraVideoStudio
   ```

### Linux

**AppImage:**
- Just delete the AppImage file
- Optional: Remove app data: `rm -rf ~/.config/aura-video-studio`

**Debian/Ubuntu:**
```bash
sudo apt remove aura-video-studio
```

**Fedora/RHEL:**
```bash
sudo dnf remove aura-video-studio
```

**Snap:**
```bash
sudo snap remove aura-video-studio
```

---

## FAQ

**Q: Do I need all AI provider accounts?**
A: No! You only need one LLM provider. Ollama is free and runs locally.

**Q: How much does it cost?**
A: Aura Video Studio is free. AI provider costs vary (Ollama is free, cloud providers charge per use).

**Q: Can I use this offline?**
A: Partially. With Ollama, you can generate scripts offline. Video rendering works offline. Cloud providers require internet.

**Q: Is my data private?**
A: Yes, when using local providers (Ollama). Cloud providers (OpenAI, etc.) receive your prompts per their privacy policies.

**Q: Can I use my own AI models?**
A: Yes! Use Ollama to run custom models, or configure custom API endpoints in Settings → Advanced.

**Q: What formats can I export?**
A: MP4 (H.264), WebM (VP9), MOV, and individual frames (PNG/JPG).

---

**Happy creating! 🎬✨**

For more information, visit [docs.aura-video-studio.com](https://docs.aura-video-studio.com)
