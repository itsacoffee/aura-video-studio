# FFmpeg Errors

This guide helps you troubleshoot FFmpeg-related errors in Aura Video Studio.

## Quick Navigation

- [Installation Issues](#installation-issues)
- [Corrupted Installation](#corrupted-installation)
- [Processing Errors](#processing-errors)
- [Codec Issues](#codec-issues)
- [Performance Problems](#performance-problems)

---

## Error Codes Reference

### Installation Error Codes

- **E302**: FFmpeg not found on system
- **E303**: FFmpeg validation failed or corrupted binary
- **E311**: Download source not found (404)
- **E312**: No download mirrors available
- **E313**: General installation failure
- **E320**: Download timeout
- **E321**: Network error during download
- **E322**: Downloaded file corrupted or checksum mismatch
- **E323**: DNS resolution failed
- **E324**: TLS/SSL connection failed
- **E325**: Disk I/O error during installation

---

## Installation Issues

### E302: FFmpeg Not Found

**Error**: "FFmpeg is not installed or cannot be found on the system path"

**Symptoms**:
- Cannot export videos
- "FFmpeg required" error messages
- Rendering immediately fails

**Solutions**:

#### Windows

**Option 1: Use Built-in Installer** (Recommended)
1. Open Aura Video Studio
2. Go to Settings → System
3. Click "Install FFmpeg"
4. Wait for download and installation
5. Restart Aura

**Option 2: Install via Chocolatey**
```powershell
choco install ffmpeg -y
```

**Option 3: Manual Installation**
1. Download FFmpeg from https://ffmpeg.org/download.html
2. Extract to `C:\ffmpeg`
3. Add to PATH:
   ```powershell
   setx /M PATH "%PATH%;C:\ffmpeg\bin"
   ```
4. Restart terminal/Aura

**Option 4: Use AURA_FFMPEG_PATH Environment Variable**
```powershell
setx AURA_FFMPEG_PATH "C:\path\to\ffmpeg.exe"
```

**Verify Installation**:
```powershell
ffmpeg -version
```

#### Mac

**Option 1: Homebrew** (Recommended)
```bash
brew install ffmpeg
```

**Option 2: MacPorts**
```bash
sudo port install ffmpeg
```

**Option 3: Use AURA_FFMPEG_PATH**
```bash
export AURA_FFMPEG_PATH="/usr/local/bin/ffmpeg"
```

**Verify Installation**:
```bash
ffmpeg -version
which ffmpeg
```

#### Linux

**Ubuntu/Debian**:
```bash
sudo apt update
sudo apt install ffmpeg -y
```

**Fedora/RHEL**:
```bash
sudo dnf install ffmpeg -y
```

**Option: Use AURA_FFMPEG_PATH**
```bash
export AURA_FFMPEG_PATH="/usr/bin/ffmpeg"
```

#### Docker

Add FFmpeg to your Dockerfile:
```dockerfile
RUN apt-get update && apt-get install -y ffmpeg
```

Or set the environment variable:
```dockerfile
ENV AURA_FFMPEG_PATH=/usr/bin/ffmpeg
```

---

### E303: FFmpeg Validation Failed

**Error**: "FFmpeg binary is invalid or corrupted"

**Symptoms**:
- FFmpeg installed but fails to run
- Version check returns errors
- Binary is from wrong architecture

**Solutions**:
1. **Reinstall FFmpeg**:
   - Use the Download Center to reinstall
   - Or download manually from official source

2. **Check System Architecture**:
   - Ensure FFmpeg matches your OS (x64, ARM, etc.)
   - Windows: Use x64 builds for 64-bit Windows
   - Mac: Use appropriate build for Intel or Apple Silicon

3. **Verify Binary Integrity**:
   ```bash
   ffmpeg -version
   ```
   Should complete in under 5 seconds without errors

4. **Check File Permissions** (Linux/Mac):
   ```bash
   chmod +x /path/to/ffmpeg
   ```

---

### E311: Download Source Not Found

**Error**: "FFmpeg download source not found (404)"

**Symptoms**:
- Installation fails immediately
- All download mirrors return 404
- Error message mentions URL not found

**Solutions**:
1. **Try Again**: The mirror list may resolve to a working source on retry
2. **Manual Download**: 
   - Download from https://ffmpeg.org/download.html
   - Use "Use Existing FFmpeg" option in Aura
3. **Check Internet Connection**: Ensure you can access external websites
4. **Firewall**: Verify firewall isn't blocking downloads

---

### E312: No Download Mirrors Available

**Error**: "No download mirrors available for FFmpeg"

**Symptoms**:
- Installation cannot start
- No download sources configured
- Manifest configuration issue

**Solutions**:
1. **Update Application**: Ensure you have the latest version
2. **Check Configuration**: Verify engine manifest is properly configured
3. **Manual Installation**: Download FFmpeg manually and use "Use Existing FFmpeg"
4. **Contact Support**: If issue persists after update

---

### E313: General Installation Failure

**Error**: "Installation failed due to an unknown error"

**Symptoms**:
- Installation fails without specific error
- Generic failure message
- Logs don't indicate specific cause

**Solutions**:
1. **Check Disk Space**: Ensure at least 500MB free space
2. **Restart Application**: Close and restart Aura Video Studio
3. **Check Antivirus**: Temporarily disable antivirus during installation
4. **Run as Administrator** (Windows): Right-click → Run as Administrator
5. **Check Logs**: Review application logs for detailed error information
6. **Manual Installation**: Download and install FFmpeg manually

---

### E320: Download Timeout

**Error**: "Download timed out"

**Symptoms**:
- Installation hangs
- Download never completes
- Timeout error after extended wait

**Solutions**:
1. **Check Internet Speed**: Ensure stable connection with adequate speed
2. **Use Wired Connection**: Switch from WiFi to ethernet if possible
3. **Try Later**: Network conditions may improve
4. **Increase Timeout**: Contact support if persistent
5. **Manual Download**: Download FFmpeg directly and use "Use Existing FFmpeg"

---

### E321: Network Error

**Error**: "Network error during download"

**Symptoms**:
- Connection drops during download
- Intermittent network failures
- DNS or routing issues

**Solutions**:
1. **Check Internet Connection**: Verify network stability
2. **Disable VPN**: Try without VPN if using one
3. **Check Firewall**: Ensure firewall allows outbound HTTPS connections
4. **Try Different Network**: Use different WiFi or mobile hotspot
5. **Check Proxy Settings**: Verify proxy configuration if applicable
6. **Manual Download**: Download FFmpeg manually from official site

---

### E322: Downloaded File Corrupted

**Error**: "Downloaded file is corrupted or checksum mismatch"

**Symptoms**:
- Download completes but validation fails
- Checksum doesn't match expected value
- Extracted files are incomplete

**Solutions**:
1. **Clear Cache**: Delete partial downloads and try again
2. **Check Disk Space**: Ensure sufficient space for extraction
3. **Disable Antivirus**: Temporarily disable during download
4. **Try Different Mirror**: Installation will try alternate sources
5. **Check Disk Health**: Run disk check utility (chkdsk on Windows)
6. **Manual Download**: Download from official FFmpeg website

---

### E323: DNS Resolution Failed

**Error**: "Unable to resolve download server hostname"

**Symptoms**:
- Cannot connect to download server
- DNS lookup failures
- "Host not found" errors

**Solutions**:
1. **Check Internet Connection**: Verify general internet access
2. **Try Different DNS**:
   - Windows: Use Google DNS (8.8.8.8, 8.8.4.4)
   - Mac/Linux: Configure `/etc/resolv.conf`
3. **Flush DNS Cache**:
   - Windows: `ipconfig /flushdns`
   - Mac: `sudo dscacheutil -flushcache`
   - Linux: `sudo systemd-resolve --flush-caches`
4. **Check Hosts File**: Ensure no blocking entries
5. **Wait and Retry**: DNS propagation may be in progress

---

### E324: TLS/SSL Connection Failed

**Error**: "Failed to establish secure connection"

**Symptoms**:
- SSL handshake failures
- Certificate validation errors
- TLS protocol mismatch

**Solutions**:
1. **Check System Time**: Ensure date/time are correct
2. **Update Operating System**: Install latest security patches
3. **Update Root Certificates**: 
   - Windows: Run Windows Update
   - Mac: Update via Software Update
   - Linux: `sudo update-ca-certificates`
4. **Check Firewall**: Verify HTTPS traffic is allowed
5. **Disable SSL Inspection**: If using corporate firewall
6. **Manual Download**: Download FFmpeg without HTTPS restrictions

---

### E325: Disk I/O Error

**Error**: "Failed to write to disk during installation"

**Symptoms**:
- Cannot write files
- Disk full errors
- Permission denied errors

**Solutions**:
1. **Check Disk Space**: Free up at least 500MB
2. **Check Permissions**: Ensure write access to installation directory
3. **Run as Administrator** (Windows): Right-click → Run as Administrator
4. **Check Disk Health**: Run disk check utility
5. **Try Different Location**: Install to different drive
6. **Close Other Applications**: Free up file handles
7. **Reboot**: Restart computer and try again

---

**Arch Linux**:
```bash
sudo pacman -S ffmpeg
```

**Verify Installation**:
```bash
ffmpeg -version
which ffmpeg
# Should show: /usr/bin/ffmpeg
```

### FFmpeg Not in PATH

**Error**: "FFmpeg is installed but not found in system PATH"

**Solutions**:

1. **Manually Specify Path** in `appsettings.json`:
   ```json
   {
     "FFmpeg": {
       "BinaryPath": "C:\\ffmpeg\\bin\\ffmpeg.exe",  // Windows
       // OR
       "BinaryPath": "/usr/local/bin/ffmpeg"  // Mac/Linux
     }
   }
   ```

2. **Add to PATH** (permanent):
   ```bash
   # Linux/Mac - Add to ~/.bashrc or ~/.zshrc
   export PATH=$PATH:/usr/local/bin
   
   # Windows - Use System Properties → Environment Variables
   # Add FFmpeg bin directory to PATH
   ```

3. **Restart Application** after PATH changes

---

## Corrupted Installation

### Error: FFmpeg Installation Corrupted

**Error Code**: **FFmpegCorrupted**

**Symptoms**:
- FFmpeg found but won't execute
- "Invalid executable" errors
- Crashes when running FFmpeg
- Missing codecs or features

**Solutions**:

#### Verify FFmpeg Integrity

```bash
# Test basic functionality
ffmpeg -version

# Test codec support
ffmpeg -codecs | grep h264
ffmpeg -codecs | grep aac

# Test format support
ffmpeg -formats | grep mp4
```

#### Reinstall FFmpeg

**Windows**:
```powershell
# If installed via Chocolatey
choco uninstall ffmpeg
choco install ffmpeg -y

# Manual: Delete old installation
Remove-Item -Recurse -Force C:\ffmpeg
# Then reinstall using steps above
```

**Mac**:
```bash
brew uninstall ffmpeg
brew install ffmpeg
```

**Linux**:
```bash
sudo apt remove ffmpeg
sudo apt install ffmpeg
```

#### Install Full Build

Some FFmpeg builds are minimal and lack codecs:

**Windows**:
- Download "full" build from https://www.gyan.dev/ffmpeg/builds/
- Not "essentials" build

**Mac**:
```bash
# Install with all options
brew install ffmpeg --with-libvpx --with-libvorbis
```

**Linux**:
```bash
# Enable universe repository (Ubuntu)
sudo add-apt-repository universe
sudo apt update
sudo apt install ffmpeg
```

### Missing Codecs

**Error**: "Codec not supported" or "Unknown codec"

**Check Available Codecs**:
```bash
ffmpeg -codecs
```

**Common Required Codecs**:
- **Video**: libx264 (H.264), libx265 (H.265), libvpx (VP9)
- **Audio**: aac, libmp3lame (MP3), libvorbis (Vorbis)

**Solutions**:

1. **Install FFmpeg with codec support**:
   ```bash
   # Mac
   brew install ffmpeg --with-libx264 --with-libx265 --with-libvpx
   
   # Linux - usually included by default
   sudo apt install ffmpeg
   ```

2. **Use alternative codec**:
   ```json
   {
     "Rendering": {
       "VideoCodec": "libx264",  // Most compatible
       "AudioCodec": "aac"
     }
   }
   ```

---

## Processing Errors

### Error: FFmpeg Processing Failed

**Error Code**: **FFmpegFailed**

**Common Causes and Solutions**:

#### 1. Corrupted Input Files

**Test Input Files**:
```bash
# Verify media files are valid
ffmpeg -v error -i input.mp4 -f null -
# No output = file is valid
```

**Solutions**:
- Re-download or regenerate corrupted files
- Convert to standard format:
  ```bash
  ffmpeg -i problematic.mp4 -c:v libx264 -c:a aac fixed.mp4
  ```

#### 2. Unsupported File Format

**Error**: "Unknown format" or "Format not supported"

**Check Format**:
```bash
ffmpeg -i file.ext
# Shows format and codec information
```

**Convert to Supported Format**:
```bash
ffmpeg -i input.avi -c:v libx264 -c:a aac output.mp4
```

#### 3. Invalid Codec Parameters

**Error**: "Invalid argument" or "Option not found"

**Solutions**:

1. **Use standard presets**:
   ```bash
   ffmpeg -i input.mp4 -preset medium -crf 23 output.mp4
   ```

2. **Check codec options**:
   ```bash
   ffmpeg -h encoder=libx264
   # Shows all options for H.264 encoder
   ```

3. **Simplify settings**:
   ```json
   {
     "Rendering": {
       "VideoCodec": "libx264",
       "Preset": "medium",  // Not "custom"
       "CRF": 23
     }
   }
   ```

#### 4. Insufficient Resources

**Error**: "Cannot allocate memory" or timeout

**Solutions**:

1. **Increase memory allocation**:
   ```json
   {
     "Rendering": {
       "MaxMemoryMB": 4096  // Increase from default
     }
   }
   ```

2. **Process in segments**:
   - Split long videos into parts
   - Render separately
   - Concatenate using FFmpeg:
     ```bash
     # Create file list
     echo "file 'part1.mp4'" > list.txt
     echo "file 'part2.mp4'" >> list.txt
     
     # Concatenate
     ffmpeg -f concat -safe 0 -i list.txt -c copy output.mp4
     ```

3. **Reduce complexity**:
   - Lower resolution
   - Reduce frame rate
   - Use faster preset
   - Disable hardware acceleration if causing issues

---

## Codec Issues

### H.264 Encoding Errors

**Error**: "h264 encoder not found"

**Solutions**:

1. **Verify encoder availability**:
   ```bash
   ffmpeg -encoders | grep h264
   # Should show: libx264, h264_nvenc, h264_qsv, etc.
   ```

2. **Use software encoder**:
   ```json
   {
     "Rendering": {
       "VideoCodec": "libx264"  // Software encoder
     }
   }
   ```

3. **Install x264 libraries**:
   ```bash
   # Linux
   sudo apt install libx264-dev
   
   # Mac
   brew install x264
   ```

### Hardware Encoder Errors

**Error**: "nvenc not available" or "Cannot open encoder"

**Causes**:
- Hardware encoder not supported
- Driver issues
- GPU not available

**Solutions**:

1. **Check GPU and drivers**:
   ```bash
   # NVIDIA
   nvidia-smi
   
   # Should show GPU and driver version
   ```

2. **Verify encoder support**:
   ```bash
   ffmpeg -encoders | grep nvenc  # NVIDIA
   ffmpeg -encoders | grep qsv    # Intel
   ffmpeg -encoders | grep amf    # AMD
   ```

3. **Fall back to software encoding**:
   ```json
   {
     "Rendering": {
       "UseHardwareAcceleration": false,
       "VideoCodec": "libx264"
     }
   }
   ```

4. **Update GPU drivers**:
   - NVIDIA: https://www.nvidia.com/download/index.aspx
   - AMD: https://www.amd.com/en/support
   - Intel: https://www.intel.com/content/www/us/en/download-center/home.html

### Audio Codec Errors

**Error**: "aac encoder not found" or "audio encoding failed"

**Solutions**:

1. **Use AAC encoder**:
   ```json
   {
     "Rendering": {
       "AudioCodec": "aac",
       "AudioBitrate": "192k"
     }
   }
   ```

2. **Verify audio support**:
   ```bash
   ffmpeg -encoders | grep aac
   # Should show: aac (native), libfdk_aac
   ```

3. **Alternative audio codecs**:
   ```json
   {
     "Rendering": {
       "AudioCodec": "libmp3lame"  // MP3
       // OR
       "AudioCodec": "libvorbis"   // OGG Vorbis
     }
   }
   ```

---

## Performance Problems

### Slow FFmpeg Processing

**Expected Performance**:
- Software encoding: 0.5-1x real-time
- Hardware encoding: 2-5x real-time

**If slower, try**:

#### 1. Enable Hardware Acceleration

```json
{
  "Rendering": {
    "UseHardwareAcceleration": true,
    "HardwareEncoder": "h264_nvenc"  // NVIDIA
    // "h264_qsv"   // Intel
    // "h264_amf"   // AMD
  }
}
```

**Test hardware encoding**:
```bash
ffmpeg -hwaccel cuda -i input.mp4 -c:v h264_nvenc output.mp4
```

#### 2. Use Faster Preset

```json
{
  "Rendering": {
    "Preset": "faster"  // Instead of slow/slower
    // Trade: Faster encoding, slightly larger file
  }
}
```

#### 3. Reduce Thread Count (counterintuitive but sometimes helps)

```json
{
  "Rendering": {
    "Threads": 4  // Instead of 0 (all cores)
  }
}
```

#### 4. Optimize for Speed

```bash
ffmpeg -i input.mp4 \
  -preset ultrafast \
  -crf 28 \
  -tune fastdecode \
  output.mp4
```

### High Memory Usage

**Error**: "Out of memory" during processing

**Solutions**:

1. **Limit buffer size**:
   ```json
   {
     "Rendering": {
       "MaxBufferSize": "50M"
     }
   }
   ```

2. **Process one frame at a time**:
   ```bash
   ffmpeg -i input.mp4 -threads 1 output.mp4
   ```

3. **Use 2-pass encoding**:
   ```bash
   # Pass 1
   ffmpeg -i input.mp4 -pass 1 -f null /dev/null
   
   # Pass 2
   ffmpeg -i input.mp4 -pass 2 output.mp4
   ```

---

## Advanced Troubleshooting

### Enable FFmpeg Debug Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Aura.Core.FFmpeg": "Trace"
    }
  },
  "Rendering": {
    "LogFFmpegCommands": true,
    "LogFFmpegOutput": true
  }
}
```

### Test FFmpeg Command Directly

1. **Enable command logging** (as above)
2. **Find command in logs**:
   ```
   Executing FFmpeg: ffmpeg -i input.mp4 ...
   ```
3. **Run manually**:
   ```bash
   ffmpeg -i input.mp4 -v verbose ...
   ```
4. **Observe output** for specific errors

### Common FFmpeg Errors and Fixes

| FFmpeg Error | Cause | Solution |
|-------------|-------|----------|
| "Invalid data found" | Corrupted file | Re-download or regenerate |
| "Option not found" | Invalid parameter | Check syntax with `ffmpeg -h` |
| "Resource temporarily unavailable" | File locked | Close other programs using file |
| "Permission denied" | No write access | Check file/folder permissions |
| "No such file or directory" | Path error | Verify file path, use quotes for spaces |
| "Conversion failed" | Codec issue | Try different codec or format |

---

## FFmpeg Version Issues

### Update FFmpeg

**Check current version**:
```bash
ffmpeg -version
```

**Recommended version**: FFmpeg 4.4 or newer

**Update**:

**Windows (Chocolatey)**:
```powershell
choco upgrade ffmpeg
```

**Mac (Homebrew)**:
```bash
brew upgrade ffmpeg
```

**Linux**:
```bash
sudo apt update
sudo apt upgrade ffmpeg
```

### Build FFmpeg from Source

For latest features or specific codecs:

```bash
# Linux example
git clone https://github.com/FFmpeg/FFmpeg.git
cd FFmpeg
./configure --enable-libx264 --enable-libx265 --enable-gpl
make
sudo make install
```

See: https://trac.ffmpeg.org/wiki/CompilationGuide

---

## Platform-Specific Issues

### Windows-Specific

**Issue**: Spaces in file paths
```bash
# Wrong
ffmpeg -i C:\My Videos\input.mp4 output.mp4

# Correct
ffmpeg -i "C:\My Videos\input.mp4" output.mp4
```

**Issue**: Long path errors
- Enable long path support in Windows 10+
- Or use shorter paths

### Mac-Specific

**Issue**: "Library not loaded" errors
```bash
# Fix dylib issues
brew reinstall ffmpeg
```

**Issue**: Rosetta compatibility (M1/M2 Macs)
```bash
# Use native ARM build
arch -arm64 brew install ffmpeg
```

### Linux-Specific

**Issue**: Permission errors
```bash
# Add user to video group (hardware encoding)
sudo usermod -a -G video $USER
```

**Issue**: Missing dependencies
```bash
# Install all recommended packages
sudo apt install ffmpeg libavcodec-extra
```

---

## Related Documentation

- [Dependencies Setup Guide](../setup/dependencies.md)
- [Rendering Errors](rendering-errors.md)
- [Resource Errors](resource-errors.md)
- [System Requirements](../setup/system-requirements.md)

## Need More Help?

If FFmpeg errors persist:
1. Run `ffmpeg -version` and check version
2. Test FFmpeg directly with simple command
3. Check FFmpeg logs in:
   - Windows: `%APPDATA%\Aura\logs\ffmpeg.log`
   - Linux/Mac: `~/.config/aura/logs/ffmpeg.log`
4. Visit [FFmpeg Documentation](https://ffmpeg.org/documentation.html)
5. Check [GitHub Issues](https://github.com/Coffee285/aura-video-studio/issues)
6. Create new issue with:
   - FFmpeg version (`ffmpeg -version`)
   - Full error message
   - FFmpeg command being executed
   - Input file details
   - Operating system
