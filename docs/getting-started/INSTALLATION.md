# Installation Guide

This guide covers installing and configuring Aura Video Studio dependencies, with detailed instructions for manual installation, portable mode, and troubleshooting.

## Table of Contents

- [System Requirements](#system-requirements)
- [Quick Start](#quick-start)
- [FFmpeg Installation](#ffmpeg-installation)
- [Portable Mode](#portable-mode)
- [Attaching Existing Installations](#attaching-existing-installations)
- [Repair and Rescan](#repair-and-rescan)
- [Reading Logs and CorrelationId](#reading-logs-and-correlationid)
- [Troubleshooting FAQ](#troubleshooting-faq)

## System Requirements

### Windows

- **Operating System**: Windows 10 (1809 or later) or Windows 11
- **Runtime**: .NET 8.0 Runtime
- **Visual C++ Redistributable**: Microsoft Visual C++ 2015-2022 Redistributable (x64 and x86)
  - Required for FFmpeg to function properly
  - Download from: https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist
  - Install both x64 and x86 versions for maximum compatibility
  - **Symptoms if missing**: FFmpeg crashes with exit code -1073741515 or -1094995529

### Linux

- **Operating System**: Ubuntu 20.04+, Debian 11+, or equivalent
- **Runtime**: .NET 8.0 Runtime
- **Dependencies**: 
  ```bash
  sudo apt install libicu-dev libssl-dev
  ```

### Common

- **Disk Space**: Minimum 2GB free space for dependencies
- **Internet Connection**: Required for downloading dependencies and API-based providers
- **Memory**: 4GB RAM minimum, 8GB recommended for image generation

## Quick Start

The easiest way to get started is to use the Download Center in Aura Video Studio:

1. Launch Aura Video Studio
2. Navigate to **Download Center** (in the main menu)
3. Click **Install** next to FFmpeg
4. Wait for the automatic download and installation to complete

For manual installation or advanced setup, continue reading below.

## FFmpeg Installation

FFmpeg is required for video rendering and processing. You can install it in several ways:

### Method 1: Automatic Installation (Recommended)

1. Open Aura Video Studio
2. Go to **Download Center** → **Engines** tab
3. Find the FFmpeg card
4. Click **Install**
5. The application will:
   - Download FFmpeg from verified sources (GitHub releases or trusted mirrors)
   - Extract to `%LOCALAPPDATA%\Aura\dependencies\bin\` (Windows) or `~/.local/share/Aura/dependencies/bin/` (Linux/Mac)
   - Verify installation with `ffmpeg -version`
   - Register the installation automatically

**Default Installation Paths:**
- **Windows**: `C:\Users\<YourUsername>\AppData\Local\Aura\dependencies\bin\ffmpeg.exe`
- **Linux**: `~/.local/share/Aura/dependencies/bin/ffmpeg`
- **macOS**: `~/Library/Application Support/Aura/dependencies/bin/ffmpeg`

### Method 2: Manual Installation

If automatic installation fails or you prefer manual installation, follow these steps:

#### Windows

**Step 1: Download FFmpeg**

Download from one of these verified sources:

- **Official Builds (gyan.dev)**: https://www.gyan.dev/ffmpeg/builds/
  - Choose `ffmpeg-release-essentials.zip` (smaller) or `ffmpeg-release-full.zip` (complete)
  - Latest stable build as of writing: `ffmpeg-7.1-essentials_build.zip`
  
- **GitHub Releases**: https://github.com/BtbN/FFmpeg-Builds/releases
  - Choose `ffmpeg-master-latest-win64-gpl.zip` for Windows
  
- **FFmpeg Official**: https://ffmpeg.org/download.html#build-windows
  - Click "Windows builds from gyan.dev" or "Windows builds by BtbN"

**Step 2: Extract FFmpeg**

Extract the downloaded ZIP file to a location of your choice. Common locations:
- `C:\Tools\ffmpeg\`
- `C:\Program Files\ffmpeg\`
- `%LOCALAPPDATA%\Aura\dependencies\` (recommended for Aura)

The extracted folder will contain a `bin` subdirectory with `ffmpeg.exe`, `ffprobe.exe`, and `ffplay.exe`.

**Step 3: Copy to Aura Dependencies Folder (Option A)**

Copy the files to Aura's dependency folder so they're automatically detected:

```batch
:: Create the directory if it doesn't exist
mkdir "%LOCALAPPDATA%\Aura\dependencies\bin"

:: Copy FFmpeg executables
copy "C:\path\to\extracted\ffmpeg\bin\*.exe" "%LOCALAPPDATA%\Aura\dependencies\bin\"
```

Replace `C:\path\to\extracted\ffmpeg` with your actual extraction path.

**Step 4: Verify Installation**

Open Command Prompt and run:
```batch
"%LOCALAPPDATA%\Aura\dependencies\bin\ffmpeg.exe" -version
```

You should see output like:
```
ffmpeg version 7.1 Copyright (c) 2000-2024 the FFmpeg developers
built with gcc 14.2.0 (Rev1, Built by MSYS2 project)
configuration: --enable-gpl --enable-version3 ...
```

**Step 5: Register with Aura**

1. Open Aura Video Studio
2. Go to **Download Center** → **Engines** tab
3. Click **Rescan** on the FFmpeg card
4. Aura will detect the manually copied files and register them

**Alternative: Use PowerShell Script**

Save this as `install-ffmpeg.ps1`:

```powershell
# Download and install FFmpeg for Aura Video Studio
$ErrorActionPreference = "Stop"

$ffmpegUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
$downloadPath = "$env:TEMP\ffmpeg.zip"
$extractPath = "$env:TEMP\ffmpeg-extract"
$destPath = "$env:LOCALAPPDATA\Aura\dependencies\bin"

Write-Host "Downloading FFmpeg..." -ForegroundColor Cyan
Invoke-WebRequest -Uri $ffmpegUrl -OutFile $downloadPath

Write-Host "Extracting..." -ForegroundColor Cyan
Expand-Archive -Path $downloadPath -DestinationPath $extractPath -Force

Write-Host "Installing to Aura dependencies folder..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $destPath | Out-Null

$ffmpegBin = Get-ChildItem -Path $extractPath -Recurse -Filter "bin" | Select-Object -First 1
Copy-Item -Path "$($ffmpegBin.FullName)\*.exe" -Destination $destPath -Force

Write-Host "Cleaning up..." -ForegroundColor Cyan
Remove-Item -Path $downloadPath -Force
Remove-Item -Path $extractPath -Recurse -Force

Write-Host "FFmpeg installed successfully!" -ForegroundColor Green
Write-Host "Location: $destPath" -ForegroundColor Green
Write-Host "`nVerifying installation..." -ForegroundColor Cyan
& "$destPath\ffmpeg.exe" -version
Write-Host "`nNow open Aura and click 'Rescan' in the Download Center." -ForegroundColor Yellow
```

Run in PowerShell:
```powershell
powershell -ExecutionPolicy Bypass -File install-ffmpeg.ps1
```

#### Linux

**Step 1: Install via Package Manager (Recommended)**

Ubuntu/Debian:
```bash
sudo apt update
sudo apt install ffmpeg
```

Fedora:
```bash
sudo dnf install ffmpeg
```

Arch Linux:
```bash
sudo pacman -S ffmpeg
```

**Step 2: Or Download Static Build**

Download from: https://johnvansickle.com/ffmpeg/

```bash
# Download (replace with latest version)
wget https://johnvansickle.com/ffmpeg/releases/ffmpeg-release-amd64-static.tar.xz

# Extract
tar xf ffmpeg-release-amd64-static.tar.xz

# Copy to Aura dependencies
mkdir -p ~/.local/share/Aura/dependencies/bin
cp ffmpeg-*-amd64-static/ffmpeg ~/.local/share/Aura/dependencies/bin/
cp ffmpeg-*-amd64-static/ffprobe ~/.local/share/Aura/dependencies/bin/
chmod +x ~/.local/share/Aura/dependencies/bin/ffmpeg
chmod +x ~/.local/share/Aura/dependencies/bin/ffprobe
```

**Step 3: Verify Installation**

```bash
~/.local/share/Aura/dependencies/bin/ffmpeg -version
```

Or if installed system-wide:
```bash
ffmpeg -version
```

#### macOS

**Step 1: Install via Homebrew (Recommended)**

```bash
# Install Homebrew if not already installed
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# Install FFmpeg
brew install ffmpeg
```

**Step 2: Or Download Static Build**

Download from: https://evermeet.cx/ffmpeg/

```bash
# Download
curl -O https://evermeet.cx/ffmpeg/ffmpeg-7.1.zip
curl -O https://evermeet.cx/ffmpeg/ffprobe-7.1.zip

# Extract
unzip ffmpeg-7.1.zip
unzip ffprobe-7.1.zip

# Copy to Aura dependencies
mkdir -p ~/Library/Application\ Support/Aura/dependencies/bin
mv ffmpeg ~/Library/Application\ Support/Aura/dependencies/bin/
mv ffprobe ~/Library/Application\ Support/Aura/dependencies/bin/
chmod +x ~/Library/Application\ Support/Aura/dependencies/bin/ffmpeg
chmod +x ~/Library/Application\ Support/Aura/dependencies/bin/ffprobe
```

**Step 3: Verify Installation**

```bash
~/Library/Application\ Support/Aura/dependencies/bin/ffmpeg -version
```

## Portable Mode

Portable mode allows you to run Aura Video Studio from a USB drive or move installations between computers without reinstalling dependencies.

### Enabling Portable Mode

1. Create a file named `portable.flag` in the same directory as the Aura executable:
   ```batch
   :: Windows
   echo. > portable.flag
   ```
   
   ```bash
   # Linux/Mac
   touch portable.flag
   ```

2. When portable mode is active, Aura stores all data in these subdirectories:
   - `.\Aura_Data\dependencies\` - Downloaded engines and tools
   - `.\Aura_Data\settings\` - Configuration files
   - `.\Aura_Data\projects\` - Project files
   - `.\Aura_Data\logs\` - Application logs

### Moving an Existing Installation to Portable Mode

1. Create the `portable.flag` file as described above
2. Copy your existing data from the default location to the portable location:

   **Windows:**
   ```batch
   :: Copy from default location
   xcopy /E /I "%LOCALAPPDATA%\Aura" ".\Aura_Data"
   ```

   **Linux/Mac:**
   ```bash
   # Copy from default location
   cp -r ~/.local/share/Aura/* ./Aura_Data/
   ```

3. Restart Aura Video Studio - it will now use the portable directory

### Where Are Portable Files Stored?

When in portable mode:
- **Dependencies**: `<AppDirectory>\Aura_Data\dependencies\bin\ffmpeg.exe`
- **Engine installs**: `<AppDirectory>\Aura_Data\Tools\<engine-name>\`
- **Settings**: `<AppDirectory>\Aura_Data\settings\engines-config.json`
- **Logs**: `<AppDirectory>\Aura_Data\logs\`

## Attaching Existing Installations

If you already have FFmpeg installed elsewhere on your system, you can attach it to Aura without reinstalling.

### Using the Attach Dialog

1. Open Aura Video Studio
2. Go to **Download Center** → **Engines** tab
3. Find the FFmpeg card
4. Click **Attach Existing...**
5. In the dialog, enter one of:
   - Full path to `ffmpeg.exe`: `C:\ffmpeg\bin\ffmpeg.exe`
   - Path to directory containing FFmpeg: `C:\ffmpeg\bin\`
   - Path to parent directory: `C:\ffmpeg\` (Aura will check `bin/` subdirectory)
6. Click **Attach**

Aura will:
- Validate the path exists
- Run `ffmpeg -version` to verify it works
- Register the path in its configuration
- Display version information

### Supported Path Formats

The attach dialog accepts multiple path formats:

**Windows:**
- `C:\ffmpeg\bin\ffmpeg.exe` (direct file path)
- `C:\ffmpeg\bin\` (directory containing ffmpeg.exe)
- `C:\ffmpeg\` (parent directory, will check bin/)
- `C:\Program Files\ffmpeg\bin\ffmpeg.exe`

**Linux/Mac:**
- `/usr/local/bin/ffmpeg` (direct file path)
- `/usr/local/bin/` (directory containing ffmpeg)
- `/opt/ffmpeg/bin/ffmpeg`
- `/home/user/ffmpeg/` (will check bin/ subdirectory)

### System PATH FFmpeg

If FFmpeg is in your system PATH, Aura will automatically detect it when you click **Rescan**. No manual attachment needed!

To check if FFmpeg is in your PATH:

**Windows:**
```batch
where ffmpeg
```

**Linux/Mac:**
```bash
which ffmpeg
```

## Repair and Rescan

### Rescan for FFmpeg

Use **Rescan** when:
- You've manually copied FFmpeg files to the dependencies folder
- FFmpeg installation appears to be missing after a system update
- You want to verify FFmpeg is still working
- You've added FFmpeg to your system PATH

**How to Rescan:**
1. Open Aura Video Studio
2. Go to **Download Center** → **Engines** tab
3. Click **Rescan** on the FFmpeg card
4. Aura will check these locations in order:
   - Previously configured path (if any)
   - `%LOCALAPPDATA%\Aura\dependencies\bin\` (Windows) or equivalent on other platforms
   - `%LOCALAPPDATA%\Aura\Tools\ffmpeg\<version>\bin\`
   - System PATH
5. If found, displays: "FFmpeg found and registered!"
6. If not found, shows attempted paths and suggests using "Attach Existing"

### Repair FFmpeg

Use **Repair** when:
- FFmpeg is corrupted or giving errors
- Installation verification fails
- Checksum mismatch detected
- FFmpeg crashes during rendering

**How to Repair:**
1. Open Aura Video Studio
2. Go to **Download Center** → **Engines** tab
3. Click **Repair** (or the **⋯** menu → **Repair**)
4. Confirm the repair action
5. Aura will:
   - Remove corrupted files
   - Download fresh copy from mirrors
   - Verify checksums
   - Re-register the installation
   - Validate with `ffmpeg -version`

### Manual Cleanup

If automatic repair fails, you can manually clean up:

**Windows:**
```batch
:: Remove managed FFmpeg installation
rmdir /S /Q "%LOCALAPPDATA%\Aura\dependencies"
rmdir /S /Q "%LOCALAPPDATA%\Aura\Tools\ffmpeg"

:: Remove configuration
del "%LOCALAPPDATA%\Aura\settings\engines-config.json"
```

**Linux/Mac:**
```bash
# Remove managed installations
rm -rf ~/.local/share/Aura/dependencies
rm -rf ~/.local/share/Aura/Tools/ffmpeg

# Remove configuration
rm ~/.local/share/Aura/settings/engines-config.json
```

After manual cleanup, restart Aura and use **Install** or **Attach Existing** to set up FFmpeg again.

## Reading Logs and CorrelationId

When reporting issues or requesting support, logs and correlation IDs help diagnose problems.

### Finding Logs

**Default Log Locations:**
- **Windows**: `%LOCALAPPDATA%\Aura\logs\`
- **Linux**: `~/.local/share/Aura/logs/`
- **macOS**: `~/Library/Application Support/Aura/logs/`

**Portable Mode Log Location:**
- `<AppDirectory>\Aura_Data\logs\`

### Log Files

Aura creates several log files:
- `app-{date}.log` - Main application log
- `ffmpeg-install-{timestamp}.log` - FFmpeg installation logs
- `render-{jobId}.log` - Video rendering logs
- `engine-{engineId}-{timestamp}.log` - Engine-specific logs

### Finding CorrelationId

When an error occurs, Aura displays a **CorrelationId** (also called trace identifier) in error messages.

**Example Error:**
```
Installation failed: Network timeout
CorrelationId: 0HN7GKQP2M8K1:00000001
```

The CorrelationId appears in:
- Error dialogs and alerts
- Console output (F12 Developer Tools in browser)
- API responses (in JSON as `correlationId` field)
- Log files (look for entries with matching ID)

### Searching Logs for CorrelationId

**Windows (PowerShell):**
```powershell
Get-ChildItem "$env:LOCALAPPDATA\Aura\logs\*.log" | Select-String "0HN7GKQP2M8K1"
```

**Linux/Mac:**
```bash
grep -r "0HN7GKQP2M8K1" ~/.local/share/Aura/logs/
```

### Sharing Logs for Support

When requesting support:

1. **Note the CorrelationId** from the error message
2. **Find the relevant log file**:
   - For installation issues: `ffmpeg-install-{timestamp}.log`
   - For rendering issues: `render-{jobId}.log`
   - For general errors: `app-{date}.log`
3. **Copy the relevant section** around the CorrelationId timestamp
4. **Include system information**:
   - Operating system and version
   - Aura Video Studio version
   - FFmpeg version (from `ffmpeg -version`)
5. **Share via**:
   - GitHub issue
   - Support email
   - Community forum

**Example Support Request:**
```
Subject: FFmpeg installation fails with E302-FFMPEG_INSTALL_FAILED

CorrelationId: 0HN7GKQP2M8K1:00000001
Aura Version: 1.2.0
OS: Windows 11 22H2
FFmpeg: Not installed (installation failing)

Error message:
"Installation failed: Failed to download from all sources"

Log excerpt:
[2024-10-12 22:15:33] Starting FFmpeg installation - Mode: managed, CorrelationId: 0HN7GKQP2M8K1:00000001
[2024-10-12 22:15:45] Attempted URL: https://github.com/...
[2024-10-12 22:15:45] Error: The remote server returned an error: (404) Not Found.
...
```

## Troubleshooting FAQ

### Common FFmpeg Errors

#### Error: "FFmpeg not found" or "E302-FFMPEG_VALIDATION"

**Symptoms:**
- Error message: "FFmpeg binary not found at {path}"
- Rendering fails with "FFmpeg validation failed"

**Solutions:**
1. **Run Rescan**: Click **Rescan** in Download Center
2. **Check installation path**: 
   - Windows: Open `%LOCALAPPDATA%\Aura\dependencies\bin\`
   - Verify `ffmpeg.exe` exists
3. **Verify executable**: Run `ffmpeg -version` in terminal
4. **Reinstall**: Click **Install** in Download Center to download fresh copy
5. **Check permissions**: Ensure the file is not blocked (Windows: right-click → Properties → Unblock)

#### Error: "FFmpeg crashed" or "Exit code -1073741515"

**Symptoms:**
- FFmpeg crashes immediately when rendering
- Error code: `-1073741515` (0xC0000135) or `-1094995529` (0xBAADF00D)
- No output file produced

**Causes:**
- Missing Visual C++ Redistributable (Windows)
- Corrupted FFmpeg binary
- Incompatible FFmpeg build

**Solutions:**

1. **Install Visual C++ Redistributable** (Windows):
   - Download from: https://aka.ms/vs/17/release/vc_redist.x64.exe
   - Run installer and restart computer
   
2. **Repair FFmpeg**:
   - Go to Download Center → FFmpeg card
   - Click **Repair**
   - Let Aura download fresh copy

3. **Switch to software encoding**:
   - If using NVENC (hardware encoding), switch to x264 (software)
   - In Aura Settings → Video → Encoder: Select "libx264" instead of "h264_nvenc"

4. **Try different FFmpeg build**:
   - Download essentials build from https://www.gyan.dev/ffmpeg/builds/
   - Use **Attach Existing** to point to new build

#### Error: "Invalid data found" or "moov atom not found"

**Symptoms:**
- Error during rendering: "Invalid data found when processing input"
- Error: "moov atom not found"
- Partial output file created

**Causes:**
- Corrupted input file
- Unsupported input format
- Incomplete download of input media

**Solutions:**

1. **Verify input files**:
   ```batch
   ffmpeg -v error -i "your-input-file.mp4" -f null -
   ```
   If errors appear, the input file is corrupted

2. **Re-download input media**: If files were downloaded, try downloading again

3. **Convert input to compatible format**:
   ```batch
   ffmpeg -i input.mp4 -c:v libx264 -c:a aac output.mp4
   ```

4. **Check disk space**: Ensure sufficient space for temporary files

#### Error: "Encoder not found" or "Unknown encoder 'h264_nvenc'"

**Symptoms:**
- Error: "Encoder 'h264_nvenc' not found"
- Error: "Unknown encoder 'libx265'"

**Causes:**
- FFmpeg build doesn't include requested encoder
- Hardware encoder (NVENC) not available on system

**Solutions:**

1. **Switch to software encoding**:
   - In Aura Settings → Video → Encoder
   - Change from "h264_nvenc" to "libx264"
   - Change from "hevc_nvenc" to "libx265"

2. **List available encoders**:
   ```batch
   ffmpeg -encoders | findstr h264
   ```
   Use an encoder from this list

3. **Download full build**:
   - Download `ffmpeg-release-full.zip` instead of essentials
   - Contains more encoders and features

4. **For NVENC (NVIDIA GPU encoding)**:
   - Ensure you have NVIDIA GPU installed
   - Update NVIDIA drivers to latest
   - Verify GPU supports NVENC: https://developer.nvidia.com/video-encode-and-decode-gpu-support-matrix

### FFmpeg Version Validation

To verify FFmpeg is working correctly:

#### Basic Version Check

**Windows:**
```batch
"%LOCALAPPDATA%\Aura\dependencies\bin\ffmpeg.exe" -version
```

**Linux/Mac:**
```bash
~/.local/share/Aura/dependencies/bin/ffmpeg -version
```

**Expected Output:**
```
ffmpeg version N-xxxxx-gxxxxxxx Copyright (c) 2000-2024 the FFmpeg developers
built with gcc X.X.X
configuration: --enable-gpl --enable-version3 ...
libavutil      XX. XX.XXX
libavcodec     XX. XX.XXX
libavformat    XX. XX.XXX
...
```

#### Check Supported Encoders

```batch
ffmpeg -encoders | findstr 264
```

Expected to see:
- `libx264` (software H.264)
- `h264_nvenc` (NVIDIA hardware, if available)
- `h264_amf` (AMD hardware, if available)
- `h264_qsv` (Intel QuickSync, if available)

#### Test Encoding

Create a simple test video:

**Windows:**
```batch
ffmpeg -f lavfi -i testsrc=duration=5:size=1280x720:rate=30 -c:v libx264 test.mp4
```

**Linux/Mac:**
```bash
ffmpeg -f lavfi -i testsrc=duration=5:size=1280x720:rate=30 -c:v libx264 test.mp4
```

If successful, `test.mp4` should be created (about 5 seconds, 1280x720).

#### Verify Hardware Encoding (Optional)

If you have NVIDIA GPU:
```batch
ffmpeg -f lavfi -i testsrc=duration=5:size=1920x1080:rate=30 -c:v h264_nvenc test_nvenc.mp4
```

If successful, you can use hardware encoding for faster rendering.

### Performance Issues

#### FFmpeg is slow / rendering takes too long

**Solutions:**

1. **Enable hardware encoding** (if available):
   - NVIDIA GPU: Use `h264_nvenc` or `hevc_nvenc`
   - AMD GPU: Use `h264_amf`
   - Intel: Use `h264_qsv`

2. **Adjust encoding preset**:
   - Faster presets: `ultrafast`, `superfast`, `veryfast`, `faster`, `fast`
   - Slower but better quality: `medium`, `slow`, `slower`, `veryslow`
   - In Aura Settings → Video → Preset

3. **Reduce output resolution**:
   - If rendering 4K, try 1080p
   - Lower bitrate settings

4. **Check CPU usage**:
   - Close other applications
   - Ensure adequate cooling
   - Check task manager for CPU throttling

#### FFmpeg uses too much memory

**Solutions:**

1. **Process videos in smaller chunks**: Aura automatically splits long videos
2. **Reduce concurrent operations**: Close other applications
3. **Check disk space**: Ensure sufficient space for temporary files
4. **Restart Aura**: Clears cached data

### Still Having Issues?

If you've tried the solutions above and still have problems:

1. **Collect information**:
   - Error message and CorrelationId
   - FFmpeg version: `ffmpeg -version`
   - OS version and architecture
   - Log files from `%LOCALAPPDATA%\Aura\logs\`

2. **Check GitHub Issues**: https://github.com/Coffee285/aura-video-studio/issues
   - Search for similar issues
   - Read solutions from other users

3. **Create a new issue**:
   - Include all collected information
   - Attach relevant log files
   - Describe steps to reproduce

4. **Community Support**:
   - Join community discussions
   - Ask questions in Discord/forums
   - Check documentation: https://github.com/Coffee285/aura-video-studio/blob/main/docs/

---

## Additional Resources

- **FFmpeg Official Documentation**: https://ffmpeg.org/documentation.html
- **FFmpeg Wiki**: https://trac.ffmpeg.org/wiki
- **Aura Documentation**: https://github.com/Coffee285/aura-video-studio/tree/main/docs
- **Download Center Guide**: See `DOWNLOAD_CENTER.md`
- **Portable Mode Guide**: See `PORTABLE_MODE_GUIDE.md`
- **Engines Guide**: See `docs/ENGINES.md`

---

*Last updated: October 2024*
