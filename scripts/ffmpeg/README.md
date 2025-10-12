# FFmpeg Installation Scripts

This directory contains automated installation scripts for FFmpeg to make setup easier for Aura Video Studio users.

## Available Scripts

### Windows

#### 1. `install-ffmpeg-windows.ps1` (PowerShell)
Full-featured PowerShell script with multiple mirror support and error handling.

**Usage:**
```powershell
# Default installation (gyan.dev mirror)
powershell -ExecutionPolicy Bypass -File install-ffmpeg-windows.ps1

# Use GitHub mirror
powershell -ExecutionPolicy Bypass -File install-ffmpeg-windows.ps1 -Source github

# Use custom URL
powershell -ExecutionPolicy Bypass -File install-ffmpeg-windows.ps1 -CustomUrl "https://example.com/ffmpeg.zip"

# Install to custom location
powershell -ExecutionPolicy Bypass -File install-ffmpeg-windows.ps1 -DestinationPath "C:\custom\path"
```

**Features:**
- Downloads from trusted sources (gyan.dev or GitHub)
- Extracts and installs to Aura dependencies folder
- Verifies installation with `ffmpeg -version`
- Automatic cleanup of temporary files
- Detailed progress and error reporting

#### 2. `install-ffmpeg-simple.bat` (Batch)
Simple menu-driven batch script for users who prefer CMD.

**Usage:**
```batch
install-ffmpeg-simple.bat
```

**Features:**
- Interactive menu with options
- Calls PowerShell script for actual installation
- Option to open manual installation guide

### Linux

#### `install-ffmpeg-linux.sh` (Bash)
Automated installation script for Linux distributions.

**Usage:**
```bash
# Install static build (default)
bash install-ffmpeg-linux.sh

# Install via package manager
bash install-ffmpeg-linux.sh --source=system

# Install to custom location
bash install-ffmpeg-linux.sh --dest="$HOME/custom/path"

# Show help
bash install-ffmpeg-linux.sh --help
```

**Features:**
- Detects Linux distribution automatically
- Two installation methods:
  - **static**: Downloads static build from johnvansickle.com
  - **system**: Uses distribution's package manager (apt, dnf, pacman, etc.)
- Supports multiple architectures (amd64, arm64, armhf, i686)
- Automatic verification and cleanup

**Supported Distributions:**
- Ubuntu / Debian / Linux Mint / Pop!_OS
- Fedora
- Arch Linux / Manjaro
- openSUSE

## After Installation

After running any of these scripts:

1. Open Aura Video Studio
2. Navigate to **Download Center** → **Engines** tab
3. Click **Rescan** on the FFmpeg card
4. FFmpeg should be detected and registered automatically!

## Manual Installation

If the automated scripts don't work for your system, see the comprehensive manual installation guide:

**📖 [docs/INSTALLATION.md](../../docs/INSTALLATION.md)**

The manual guide includes:
- Step-by-step instructions for Windows, Linux, and macOS
- Troubleshooting common issues
- How to attach existing FFmpeg installations
- Repair and rescan instructions
- Support information for reporting issues

## Download Sources

The scripts download FFmpeg from these verified sources:

### Windows
- **gyan.dev**: https://www.gyan.dev/ffmpeg/builds/ (recommended, maintained by Gyan Doshi)
- **BtbN GitHub**: https://github.com/BtbN/FFmpeg-Builds/releases (alternative mirror)

### Linux
- **John Van Sickle**: https://johnvansickle.com/ffmpeg/ (static builds for all architectures)
- **Distribution repositories**: Official packages via apt, dnf, pacman, etc.

All sources provide official FFmpeg builds with GPLv3 license.

## Troubleshooting

### Windows: PowerShell Execution Policy Error

If you get an error about execution policies:
```powershell
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Bypass -Force
```

Or run with bypass:
```powershell
powershell -ExecutionPolicy Bypass -File install-ffmpeg-windows.ps1
```

### Linux: Permission Denied

Make the script executable:
```bash
chmod +x install-ffmpeg-linux.sh
./install-ffmpeg-linux.sh
```

### Download Fails

If download fails:
1. Check your internet connection
2. Try a different mirror (for Windows: `-Source github`)
3. Download manually from the sources listed above
4. Use manual installation instructions in docs/INSTALLATION.md

### FFmpeg Not Detected After Installation

1. Click **Rescan** in Aura's Download Center
2. Verify FFmpeg is in the correct location:
   - Windows: `%LOCALAPPDATA%\Aura\dependencies\bin\ffmpeg.exe`
   - Linux: `~/.local/share/Aura/dependencies/bin/ffmpeg`
3. Test manually: `ffmpeg -version`
4. Use **Attach Existing** in Aura to manually specify the path

## Support

For detailed help and troubleshooting:
- See [docs/INSTALLATION.md](../../docs/INSTALLATION.md)
- See [docs/Troubleshooting.md](../../docs/Troubleshooting.md)
- Report issues: https://github.com/Coffee285/aura-video-studio/issues

## License

FFmpeg is licensed under LGPL 2.1+ or GPL 2+ depending on the build configuration.
Visit https://ffmpeg.org/legal.html for more information.

These installation scripts are part of Aura Video Studio and follow the project's license.

