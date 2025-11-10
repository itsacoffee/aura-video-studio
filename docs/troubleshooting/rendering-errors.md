# Rendering Errors

This guide helps you troubleshoot video rendering and export errors in Aura Video Studio.

## Quick Navigation

- [Common Rendering Issues](#common-rendering-issues)
- [FFmpeg Errors](#ffmpeg-errors)
- [Export Failures](#export-failures)
- [Performance Issues](#performance-issues)
- [Quality Issues](#quality-issues)

---

## Common Rendering Issues

### Error Code E500: General Rendering Error

This is a catch-all error for rendering issues. Check the detailed error message for specifics.

### Symptoms

- Export fails immediately or mid-process
- Video file is corrupted or unplayable
- Missing audio or video tracks
- Unexpected visual artifacts

---

## FFmpeg Errors

FFmpeg is required for all video rendering operations. Most rendering errors are related to FFmpeg.

### FFmpeg Not Found

**Error**: "FFmpeg is not installed or not found on system path"

**Solutions**:

1. **Install FFmpeg**:
   - See detailed [FFmpeg Installation Guide](../setup/dependencies.md#ffmpeg)
   - Or use the built-in installer (Windows only):
     ```
     Settings → System → Install FFmpeg
     ```

2. **Verify Installation**:
   ```bash
   ffmpeg -version
   # Should display FFmpeg version information
   ```

3. **Add to PATH** (if installed but not found):
   ```bash
   # Windows
   setx PATH "%PATH%;C:\path\to\ffmpeg\bin"
   
   # Linux/Mac
   export PATH=$PATH:/usr/local/bin
   ```

### FFmpeg Corrupted Installation

**Error**: "FFmpeg installation is corrupted or incomplete"

**Solutions**:

1. **Reinstall FFmpeg**:
   ```bash
   # Windows (via Chocolatey)
   choco uninstall ffmpeg
   choco install ffmpeg
   
   # Mac (via Homebrew)
   brew uninstall ffmpeg
   brew install ffmpeg
   
   # Linux (Ubuntu/Debian)
   sudo apt remove ffmpeg
   sudo apt install ffmpeg
   ```

2. **Download Latest Build**:
   - Visit: https://ffmpeg.org/download.html
   - Download appropriate build for your OS
   - Extract and add to PATH

### FFmpeg Processing Failed

**Error**: "FFmpeg encountered an error during processing"

**Causes**:
- Corrupted input media files
- Unsupported codecs
- Insufficient disk space
- Hardware acceleration issues

**Solutions**:

1. **Check Input Files**:
   - Ensure all media files are valid and playable
   - Re-download or regenerate corrupted assets
   - Convert problematic files to standard formats (MP4, MP3, PNG)

2. **Disable Hardware Acceleration** (if causing issues):
   ```json
   // In appsettings.json
   {
     "Rendering": {
       "UseHardwareAcceleration": false
     }
   }
   ```

3. **Increase FFmpeg Verbosity** to see detailed error:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Aura.Core.Rendering": "Debug"
       }
     }
   }
   ```

4. **Try Different Output Format**:
   - Change codec (H.264, H.265, VP9)
   - Change container (MP4, WebM, MOV)
   - Reduce complexity (lower resolution, frame rate)

---

## Export Failures

### Export Times Out

**Problem**: Export starts but never completes or times out

**Solutions**:

1. **Increase Timeout**:
   ```json
   {
     "Rendering": {
       "TimeoutMinutes": 30  // Increase from default
     }
   }
   ```

2. **Reduce Video Complexity**:
   - Lower resolution (1080p → 720p)
   - Reduce frame rate (60fps → 30fps)
   - Simplify effects and transitions
   - Split long videos into shorter segments

3. **Check System Resources**:
   ```bash
   # Check available CPU
   top  # Linux/Mac
   taskmgr  # Windows
   
   # Check disk space
   df -h  # Linux/Mac
   dir  # Windows
   ```

### Export Fails Immediately

**Problem**: Export fails as soon as you click "Export"

**Solutions**:

1. **Validate Timeline**:
   - Ensure timeline has at least one video or image track
   - Check that all media assets are available
   - Verify no missing or corrupted assets

2. **Check Output Path**:
   - Ensure output directory exists and is writable
   - Check for sufficient disk space
   - Verify no special characters in filename

3. **Test with Simple Export**:
   - Create a minimal 5-second video
   - If simple export works, issue is with complexity
   - Gradually add elements to identify problem

### Corrupted Output Video

**Problem**: Export completes but video file is corrupted or won't play

**Solutions**:

1. **Use Standard Codec**:
   ```json
   {
     "Rendering": {
       "VideoCodec": "libx264",  // H.264 for maximum compatibility
       "AudioCodec": "aac"
     }
   }
   ```

2. **Verify FFmpeg Build**:
   ```bash
   ffmpeg -codecs | grep h264
   # Should show h264 encoder support
   ```

3. **Try Re-encoding**:
   ```bash
   ffmpeg -i corrupted_video.mp4 -c:v libx264 -c:a aac output.mp4
   ```

---

## Performance Issues

### Slow Export Speed

**Problem**: Export takes excessively long

**Expected Performance**:
- 1080p 30fps: ~1-2x real-time on modern CPU
- 4K 60fps: ~0.5x real-time (2 min video = 4 min export)

**Solutions**:

1. **Enable Hardware Acceleration**:
   ```json
   {
     "Rendering": {
       "UseHardwareAcceleration": true,
       "HardwareEncoder": "h264_nvenc"  // NVIDIA
       // "h264_qsv"  // Intel
       // "h264_amf"  // AMD
     }
   }
   ```

2. **Verify GPU Encoder Support**:
   ```bash
   # NVIDIA
   ffmpeg -encoders | grep nvenc
   
   # Intel
   ffmpeg -encoders | grep qsv
   
   # AMD
   ffmpeg -encoders | grep amf
   ```

3. **Optimize Rendering Settings**:
   ```json
   {
     "Rendering": {
       "Preset": "faster",  // Instead of "slow"
       "CRF": 23,  // Higher = faster but lower quality
       "Threads": 0  // Auto-detect all CPU cores
     }
   }
   ```

4. **Close Background Applications**:
   - Free up CPU and RAM
   - Close unnecessary browser tabs
   - Pause other intensive tasks

### High Memory Usage During Export

**Problem**: System runs out of memory during export

**Solutions**:

1. **Reduce Cache Size**:
   ```json
   {
     "Rendering": {
       "MaxCacheSizeMB": 512  // Reduce if needed
     }
   }
   ```

2. **Process in Segments**:
   - Export video in multiple parts
   - Combine using FFmpeg:
     ```bash
     ffmpeg -f concat -i segments.txt -c copy output.mp4
     ```

3. **Lower Resolution Temporarily**:
   - Export at 720p for preview
   - Final export at 1080p/4K when needed

---

## Quality Issues

### Poor Video Quality

**Problem**: Exported video looks pixelated or blurry

**Solutions**:

1. **Increase Bitrate**:
   ```json
   {
     "Rendering": {
       "VideoBitrate": "5M",  // 5 Mbps for 1080p
       // "10M" for 4K
       // "2M" for 720p
     }
   }
   ```

2. **Adjust CRF (Constant Rate Factor)**:
   ```json
   {
     "Rendering": {
       "CRF": 18  // Lower = better quality (15-28 range)
       // 18: High quality
       // 23: Default/balanced
       // 28: Lower quality, smaller file
     }
   }
   ```

3. **Use Better Encoding Preset**:
   ```json
   {
     "Rendering": {
       "Preset": "slow"  // Slower = better quality
       // Options: ultrafast, superfast, veryfast, faster, fast, medium, slow, slower, veryslow
     }
   }
   ```

4. **Check Source Quality**:
   - Ensure source images are high resolution
   - Use PNG instead of JPG for better quality
   - Generate images at target resolution or higher

### Audio Issues

**Problem**: No audio, distorted audio, or sync issues

**Solutions**:

1. **Verify Audio Tracks**:
   - Check timeline has audio track enabled
   - Ensure audio files are not corrupted
   - Test audio files play correctly before adding to timeline

2. **Audio Sync Issues**:
   ```json
   {
     "Rendering": {
       "AudioSyncOffset": 0  // Adjust if sync is off
       // Positive value delays audio
       // Negative value advances audio
     }
   }
   ```

3. **Audio Codec Issues**:
   ```json
   {
     "Rendering": {
       "AudioCodec": "aac",
       "AudioBitrate": "192k",
       "AudioSampleRate": 48000
     }
   }
   ```

4. **Re-encode Audio**:
   ```bash
   ffmpeg -i video.mp4 -c:v copy -c:a aac -b:a 192k output.mp4
   ```

---

## Advanced Troubleshooting

### Enable Detailed Logging

To capture detailed rendering information:

```json
{
  "Logging": {
    "LogLevel": {
      "Aura.Core.Rendering": "Debug",
      "Aura.Core.FFmpeg": "Trace"
    }
  }
}
```

Check logs at:
- Windows: `%APPDATA%/Aura/logs/`
- Linux/Mac: `~/.config/aura/logs/`

### Test FFmpeg Command Directly

Export raw FFmpeg command and test manually:

1. Enable command logging:
   ```json
   {
     "Rendering": {
       "LogFFmpegCommands": true
     }
   }
   ```

2. Copy command from logs
3. Run manually in terminal
4. Observe detailed error output

### Common FFmpeg Command Issues

```bash
# Test basic encoding
ffmpeg -i input.mp4 -c:v libx264 -preset medium -crf 23 output.mp4

# Test with specific resolution
ffmpeg -i input.mp4 -vf scale=1920:1080 -c:v libx264 output.mp4

# Test audio encoding
ffmpeg -i input.mp4 -c:v copy -c:a aac -b:a 192k output.mp4

# Test hardware acceleration (NVIDIA)
ffmpeg -hwaccel cuda -i input.mp4 -c:v h264_nvenc output.mp4
```

---

## Rendering Settings Reference

### Recommended Settings by Use Case

**YouTube Upload (1080p)**:
```json
{
  "Rendering": {
    "Resolution": "1920x1080",
    "FrameRate": 30,
    "VideoCodec": "libx264",
    "Preset": "slow",
    "CRF": 20,
    "AudioCodec": "aac",
    "AudioBitrate": "192k"
  }
}
```

**Social Media (Instagram/TikTok)**:
```json
{
  "Rendering": {
    "Resolution": "1080x1920",  // Vertical
    "FrameRate": 30,
    "VideoCodec": "libx264",
    "Preset": "medium",
    "CRF": 23,
    "Format": "mp4"
  }
}
```

**High Quality Archive**:
```json
{
  "Rendering": {
    "Resolution": "3840x2160",  // 4K
    "FrameRate": 60,
    "VideoCodec": "libx265",  // HEVC for better compression
    "Preset": "slow",
    "CRF": 18,
    "AudioCodec": "aac",
    "AudioBitrate": "320k"
  }
}
```

**Fast Preview**:
```json
{
  "Rendering": {
    "Resolution": "1280x720",
    "FrameRate": 30,
    "VideoCodec": "libx264",
    "Preset": "ultrafast",
    "CRF": 28
  }
}
```

---

## Related Documentation

- [FFmpeg Setup Guide](../setup/dependencies.md#ffmpeg)
- [FFmpeg-Specific Errors](ffmpeg-errors.md)
- [Resource Errors](resource-errors.md)
- [General Troubleshooting](Troubleshooting.md)

## Need More Help?

If rendering issues persist:
1. Check [GitHub Issues](https://github.com/Coffee285/aura-video-studio/issues)
2. Enable debug logging and collect logs
3. Test with a minimal project
4. Create a new issue with:
   - Full error message
   - System information (OS, FFmpeg version)
   - Rendering settings used
   - Sample project (if possible)
   - Log excerpts
