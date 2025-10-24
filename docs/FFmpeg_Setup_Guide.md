# FFmpeg Setup Guide

This guide will help you install and configure FFmpeg for use with Aura Video Studio.

## Table of Contents
- [Quick Installation](#quick-installation)
- [Manual Installation](#manual-installation)
- [Verification](#verification)
- [Troubleshooting](#troubleshooting)
- [Advanced Configuration](#advanced-configuration)

## Quick Installation

Aura Video Studio provides multiple ways to install FFmpeg:

### Option 1: Automatic Installation (Recommended)
1. Open Aura Video Studio
2. Navigate to **Downloads** page
3. Go to the **Engines** tab
4. Find the **FFmpeg** card
5. Click the **Install** button
6. Wait for the download and installation to complete
7. FFmpeg will be automatically configured and ready to use

### Option 2: Rescan Existing Installation
If you already have FFmpeg installed on your system:
1. Navigate to the **Engines** tab in the Downloads page
2. Find the **FFmpeg** card
3. Click the **Rescan** button
4. If FFmpeg is in your system PATH, it will be automatically detected

### Option 3: Attach Existing Installation
If FFmpeg is installed but not in your PATH:
1. Click **Attach Existing...** in the FFmpeg card
2. Enter the path to your FFmpeg installation:
   - Full path to `ffmpeg.exe` (e.g., `C:\ffmpeg\bin\ffmpeg.exe`)
   - Or the folder containing it (e.g., `C:\ffmpeg` or `C:\ffmpeg\bin`)
3. Click **Attach**
4. Aura will validate and configure the installation

## Manual Installation

If automatic installation fails or you prefer to install manually:

### Step 1: Download FFmpeg

#### Windows
**Recommended Source:**
- Visit [https://www.gyan.dev/ffmpeg/builds/](https://www.gyan.dev/ffmpeg/builds/)
- Download **ffmpeg-release-essentials.zip** (latest version)

**Alternative Sources:**
- Official FFmpeg: [https://ffmpeg.org/download.html](https://ffmpeg.org/download.html)
- GitHub Builds: [https://github.com/BtbN/FFmpeg-Builds/releases](https://github.com/BtbN/FFmpeg-Builds/releases)

#### Linux
```bash
# Ubuntu/Debian
sudo apt update
sudo apt install ffmpeg

# Fedora/RHEL
sudo dnf install ffmpeg

# Arch Linux
sudo pacman -S ffmpeg
```

#### macOS
```bash
# Using Homebrew
brew install ffmpeg

# Using MacPorts
sudo port install ffmpeg
```

### Step 2: Extract and Install

#### Windows
1. Extract the downloaded ZIP file to a permanent location
   - Example: `C:\ffmpeg` or `C:\Program Files\ffmpeg`
2. The extracted folder should contain:
   - `bin\` folder with `ffmpeg.exe` and `ffprobe.exe`
   - `doc\` folder (optional)
   - `presets\` folder (optional)

#### Linux/macOS
If installing manually (not using package manager):
1. Extract the archive: `tar -xvf ffmpeg-*.tar.xz`
2. Move to `/usr/local/bin` or another location in your PATH
3. Make executable: `chmod +x ffmpeg ffprobe`

### Step 3: Configure in Aura

1. Open Aura Video Studio
2. Navigate to **Downloads** → **Engines** tab
3. Click **Attach Existing...** on the FFmpeg card
4. Enter the path to your installation
5. Click **Attach** to validate and configure

## Verification

### Verify Installation in Aura
1. Go to **Downloads** → **Engines** tab
2. The FFmpeg card should show:
   - Status: **Installed** (green badge)
   - Path to the installation
   - Version information

### Verify from Command Line

#### Windows (Command Prompt)
```cmd
ffmpeg -version
```

#### Linux/macOS (Terminal)
```bash
ffmpeg -version
```

**Expected Output:**
```
ffmpeg version N-XXXXX-gXXXXXX
built with gcc X.X.X...
configuration: ...
libavutil      XX. XX.XXX
libavcodec     XX. XX.XXX
...
```

## Troubleshooting

### Issue: "FFmpeg Not Found" Error

**Symptoms:**
- Aura cannot detect FFmpeg
- Video rendering fails
- "FFmpeg not found" messages

**Solutions:**

1. **Use Manual Install Guide**
   - In the FFmpeg card, click **Manual Install Guide**
   - Follow the step-by-step instructions

2. **Check Installation Paths**
   
   Aura searches these locations automatically:
   - `%LOCALAPPDATA%\Aura\Tools\ffmpeg\bin\`
   - `%LOCALAPPDATA%\Aura\dependencies\bin\`
   - System PATH environment variable

3. **Use Rescan**
   - After installing FFmpeg, click **Rescan**
   - This re-checks all detection paths

4. **Attach Manually**
   - If FFmpeg is in a custom location
   - Use **Attach Existing...** to specify the path

### Issue: "Unexpected token '<'" JSON Error

**Symptoms:**
- Error message shows: `Unexpected token '<', '<!doctype'... is not valid JSON`
- Downloads page fails to load

**Cause:**
- The API server is not running or returning HTML error pages instead of JSON

**Solutions:**

1. **Check API Server**
   - Ensure the Aura API server is running
   - Default port is 5000 or 5001 (HTTPS)
   - Check browser console for actual error responses

2. **Restart Application**
   - Close and restart Aura Video Studio
   - Wait for the API to fully initialize

3. **Check Configuration**
   - Verify API URL in application settings
   - Ensure no proxy/firewall blocking

4. **Use Manual Installation**
   - Click **Manual Install Guide** button
   - Install FFmpeg independently of the API

### Issue: Installation Failed

**Symptoms:**
- "Installation failed" error message
- Download doesn't complete

**Common Causes & Solutions:**

1. **Network Issues**
   - Check internet connection
   - Try a different network if possible
   - Check if firewall is blocking downloads

2. **Antivirus Blocking**
   - Temporarily disable antivirus
   - Add Aura to antivirus exclusions
   - Whitelist FFmpeg download URLs

3. **Insufficient Permissions**
   - Run Aura as Administrator (Windows)
   - Check write permissions to `%LOCALAPPDATA%\Aura`
   - Verify disk space availability

4. **Download Server Unavailable**
   - The mirror may be temporarily down
   - Wait and try again later
   - Use **Manual Install Guide** to download from alternative source

### Issue: Path Detection Problems

**Symptoms:**
- FFmpeg is installed but not detected
- Attach fails with "not found" error

**Solutions:**

1. **Verify File Exists**
   ```cmd
   dir "C:\ffmpeg\bin\ffmpeg.exe"
   ```
   Should show the file, not just the directory

2. **Check Permissions**
   - Ensure you have read/execute permissions
   - Right-click → Properties → Security (Windows)

3. **Test FFmpeg Directly**
   ```cmd
   "C:\ffmpeg\bin\ffmpeg.exe" -version
   ```
   Should display version information

4. **Use Full Path**
   - When attaching, provide full path to `ffmpeg.exe`
   - Not just the folder name

### Issue: Verification Failed

**Symptoms:**
- FFmpeg found but verification fails
- "Smoke test failed" error

**Possible Causes:**

1. **Corrupted Download**
   - Solution: Click **Repair** to reinstall
   - Or download manually from official source

2. **Wrong Architecture**
   - Solution: Ensure 64-bit FFmpeg on 64-bit Windows
   - Download correct build from official source

3. **Missing Dependencies** (rare on Windows)
   - Solution: Download "essentials" or "full" build
   - Not the "static" build

4. **File Permissions**
   - Solution: Grant execute permissions
   - Run `icacls` to check permissions (Windows)

## Advanced Configuration

### Adding FFmpeg to System PATH

#### Windows
1. Open System Properties
   - Right-click "This PC" → Properties
   - Click "Advanced system settings"
   - Click "Environment Variables"

2. Edit PATH Variable
   - Under "System variables" or "User variables"
   - Find and select "Path"
   - Click "Edit"
   - Click "New"
   - Add the path to FFmpeg's `bin` folder (e.g., `C:\ffmpeg\bin`)
   - Click "OK" to save

3. Verify
   ```cmd
   echo %PATH%
   ffmpeg -version
   ```

4. Restart Aura
   - Close and reopen Aura Video Studio
   - Click **Rescan** to detect FFmpeg

#### Linux/macOS
Add to `~/.bashrc`, `~/.zshrc`, or equivalent:
```bash
export PATH="/path/to/ffmpeg/bin:$PATH"
```

Then reload:
```bash
source ~/.bashrc
```

### Custom FFmpeg Builds

If you need specific codecs or features:

1. Download a custom FFmpeg build
   - GPL build for additional codecs
   - Static build for portability
   - Custom compiled with specific options

2. Test it works:
   ```bash
   /path/to/custom/ffmpeg -version
   ```

3. Attach in Aura:
   - Use **Attach Existing...**
   - Point to your custom FFmpeg binary

### Multiple FFmpeg Versions

You can maintain multiple FFmpeg installations:

1. Install each version in a separate folder
2. In Aura, attach the version you want to use
3. To switch versions:
   - Go to **Settings** → **Dependencies**
   - Change the FFmpeg path
   - Or use **Attach Existing...** with the new path

## Additional Resources

### In-App Help
- **Manual Install Guide**: Detailed step-by-step instructions in the FFmpeg card
- **Troubleshooting Tab**: Context-aware help in Downloads page
- **Error Messages**: Include actionable troubleshooting steps

### Official Documentation
- FFmpeg Official: [https://ffmpeg.org/documentation.html](https://ffmpeg.org/documentation.html)
- FFmpeg Wiki: [https://trac.ffmpeg.org/wiki](https://trac.ffmpeg.org/wiki)

### Community Support
- Check Aura Video Studio's issue tracker for similar problems
- Search the documentation for specific error messages
- Review the changelog for known issues and fixes

## Best Practices

1. **Keep FFmpeg Updated**
   - New versions include bug fixes and codec improvements
   - Check for updates periodically
   - Aura will notify when updates are available

2. **Use Trusted Sources**
   - Download only from official sources
   - Verify checksums when available
   - Be cautious of third-party repackages

3. **Test After Installation**
   - Always click **Rescan** after installing
   - Run a test render to verify functionality
   - Check the version matches expectations

4. **Document Your Setup**
   - Note which version you installed
   - Save the download source
   - Keep installation path documented

5. **Regular Maintenance**
   - Periodically verify FFmpeg is still working
   - Check for permission changes
   - Update when new versions are released

## FAQ

**Q: Do I need the "full" or "shared" build?**
A: The "essentials" build is sufficient for most use cases. The "full" build includes additional codecs and features. The "shared" build uses dynamic libraries and is smaller but requires the libraries to be present on your system. For Aura Video Studio, we recommend the "essentials" or "full" build.

**Q: Can I use FFmpeg from WSL on Windows?**
A: Aura expects native Windows binaries. WSL FFmpeg won't be detected automatically.

**Q: Does Aura require a specific FFmpeg version?**
A: Aura works with most recent FFmpeg versions (4.0+). Newer versions are recommended.

**Q: What if automatic installation keeps failing?**
A: Use the Manual Install Guide to download and install FFmpeg independently, then use "Attach Existing."

**Q: Can I use FFmpeg from another application?**
A: Yes! If another app (like OBS) installed FFmpeg, you can attach that installation in Aura.

**Q: Is FFmpeg bundled with Aura?**
A: The portable version of Aura may include FFmpeg. Check the Downloads page to see if it's already available.

---

**Need More Help?**
- Use the in-app **Troubleshooting** tab for interactive help
- Click **Manual Install Guide** for step-by-step instructions
- Check error messages for specific troubleshooting steps
