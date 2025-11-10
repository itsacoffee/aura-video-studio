# Troubleshooting Guide

This comprehensive troubleshooting guide helps you quickly resolve common issues with Aura Video Studio.

## Quick Reference: Common Issues

### Application Issues

| Symptom | Quick Fix | Details |
|---------|-----------|---------|
| White screen on launch | Press `Ctrl+F5` to hard refresh | [See below](#white-screen-or-application-failed-to-initialize) |
| Application won't start | Check if API server is running on port 5005 | [See below](#application-wont-start) |
| Settings not saving | Check disk space and file permissions | [See below](#settings-not-saving) |
| UI is slow/laggy | Close unused tabs, clear browser cache | [See below](#ui-performance-issues) |

### Generation Issues

| Symptom | Quick Fix | Details |
|---------|-----------|---------|
| Script generation fails | Test API key in Settings → Providers | [See below](#script-generation-fails) |
| TTS generation fails | Try different voice or provider | [See below](#text-to-speech-fails) |
| Image generation fails | Check provider status and VRAM | [See below](#image-generation-fails) |
| Rate limit errors | Wait 60 seconds, or switch provider | [See below](#rate-limit-exceeded) |

### Rendering Issues

| Symptom | Quick Fix | Details |
|---------|-----------|---------|
| Export fails | Verify FFmpeg is installed | [See below](#export-render-fails) |
| Export takes too long | Enable hardware acceleration | [See below](#export-too-slow) |
| Poor video quality | Increase bitrate in render settings | [See below](#poor-video-quality) |
| Audio sync issues | Check timeline audio track alignment | [See below](#audio-video-sync-issues) |

### Provider Issues

| Symptom | Quick Fix | Details |
|---------|-----------|---------|
| Invalid API key | Re-enter and test key | [See below](#invalid-api-key-error) |
| Provider offline | Switch to alternative provider | [See below](#provider-service-unavailable) |
| Local engine not starting | Check installation path and logs | [See below](#local-engine-issues) |

---

## Detailed Troubleshooting

### Application Issues

#### White Screen or "Application Failed to Initialize"

**Symptoms:**
- Blank white screen on launch
- "Application Failed to Initialize" message
- Page loads but shows no content

**Causes:**
1. Frontend not built or not copied to wwwroot
2. API server not running
3. Browser cache issue
4. JavaScript error

**Solutions:**

1. **Hard Refresh Browser**
   ```
   Press Ctrl+F5 (Windows) or Cmd+Shift+R (Mac)
   ```

2. **Clear Browser Cache**
   - Chrome: Settings → Privacy → Clear browsing data
   - Select "Cached images and files"
   - Clear last hour
   - Refresh page

3. **Verify API Server**
   - Open http://127.0.0.1:5005/health in new tab
   - Should see: `{"status":"healthy"}`
   - If error, restart API server

4. **Check Browser Console**
   - Press F12 to open DevTools
   - Look for red errors in Console tab
   - Common errors:
     - "Failed to fetch" → API not running
     - "VITE_API_BASE_URL not defined" → Environment config issue
     - "404 Not Found" → Frontend not built

5. **Rebuild Application** (if issue persists)
   ```bash
   cd Aura.Web
   npm run build
   cd ..
   dotnet build Aura.Api --configuration Release
   cd Aura.Api
   dotnet run --configuration Release
   ```

#### Application Won't Start

**Symptoms:**
- Executable doesn't launch
- Process starts then immediately exits
- Error message on startup

**Causes:**
1. Port 5005 already in use
2. Missing .NET runtime
3. Corrupted installation
4. Antivirus blocking

**Solutions:**

1. **Check if Port is in Use**
   ```bash
   netstat -ano | findstr :5005
   ```
   If port is in use, either:
   - Kill the process using the port
   - Change port in `appsettings.json`

2. **Verify .NET Runtime**
   ```bash
   dotnet --version
   ```
   Should show version 8.0 or higher
   If not installed: Download from https://dot.net

3. **Run as Administrator** (Windows)
   - Right-click Aura.exe
   - Select "Run as administrator"

4. **Check Antivirus**
   - Add Aura folder to antivirus exclusions
   - Temporarily disable antivirus to test

5. **Check Logs**
   - Location: `%LOCALAPPDATA%\Aura\logs\`
   - Open latest log file
   - Look for error messages

#### Settings Not Saving

**Symptoms:**
- Changes to settings don't persist
- Settings revert after restart
- "Failed to save settings" error

**Causes:**
1. Disk full
2. File permissions issue
3. Settings file corrupted
4. Browser storage quota exceeded

**Solutions:**

1. **Check Disk Space**
   - Need at least 1GB free
   - Clear temp files if needed

2. **Check File Permissions**
   - Settings location: `%LOCALAPPDATA%\Aura\settings\`
   - Ensure your user has write permissions
   - Try running as administrator

3. **Reset Settings**
   - Close Aura
   - Rename or delete `user-settings.json`
   - Restart Aura (will recreate with defaults)

4. **Clear Browser Storage**
   - F12 → Application tab
   - Clear Local Storage and Session Storage
   - Refresh page

#### UI Performance Issues

**Symptoms:**
- Laggy interface
- Slow timeline scrubbing
- Delayed button clicks
- High CPU/memory usage

**Solutions:**

1. **Close Unused Browser Tabs**
   - Each tab consumes memory
   - Close other applications

2. **Clear Browser Cache**
   - Settings → Privacy → Clear browsing data

3. **Disable Real-Time Preview**
   - Settings → Editor → Disable "Live preview"

4. **Reduce Timeline Zoom**
   - Showing fewer clips at once reduces render load

5. **Use Chrome or Edge**
   - Best performance with Chromium-based browsers

6. **Check System Resources**
   - Task Manager → Check CPU/RAM usage
   - Close memory-heavy applications

---

### Generation Issues

#### Script Generation Fails

**Symptoms:**
- "Failed to generate script" error
- Timeout error
- Empty response

**Causes:**
1. Invalid or expired API key
2. Provider service down
3. Rate limit exceeded
4. Network connection issue
5. Invalid request parameters

**Solutions:**

1. **Test API Key**
   - Settings → Providers
   - Find your script provider
   - Click "Test" button
   - Should show green checkmark

2. **Check Provider Status**
   - Visit provider status page:
     - OpenAI: https://status.openai.com
     - Anthropic: https://status.anthropic.com
     - Google: https://status.cloud.google.com

3. **Switch Provider**
   - Try alternative provider:
     - GPT-4 → GPT-3.5 (faster, cheaper)
     - Paid → Ollama (local, free)
     - Any → RuleBased (always works)

4. **Check Network**
   - Test internet connection
   - Try different network
   - Check firewall settings

5. **Reduce Request Size**
   - Shorten script length
   - Simplify topic description
   - Remove special characters

#### Text-to-Speech Fails

**Symptoms:**
- "TTS generation failed" error
- No audio generated
- Timeout

**Causes:**
1. Voice not available
2. Invalid characters in script
3. Credit limit reached
4. Provider service down

**Solutions:**

1. **Try Different Voice**
   - Some voices may be unavailable
   - Browse and select alternative

2. **Clean Up Script**
   - Remove special characters: `|`, `<`, `>`, `{`, `}`
   - Remove SSML tags if not supported
   - Keep text under character limit

3. **Check Credits**
   - ElevenLabs: Check account credits
   - Azure: Verify subscription active

4. **Switch Provider**
   - ElevenLabs → Azure Speech
   - Paid → Windows TTS (free)
   - Cloud → Piper TTS (local, free)

5. **Break Into Chunks**
   - Split long scripts into shorter segments
   - Generate each segment separately

#### Image Generation Fails

**Symptoms:**
- "Image generation failed" error
- Blank images
- Timeout

**Causes:**
1. VRAM insufficient (Stable Diffusion)
2. Invalid prompt
3. Service unavailable
4. Rate limit or quota exceeded

**Solutions:**

1. **Check VRAM** (Stable Diffusion local)
   - Need 6GB+ for SD XL
   - Need 4GB+ for SD 1.5
   - Use smaller models if limited

2. **Simplify Prompts**
   - Remove very long descriptions
   - Avoid special characters
   - Use clear, concise language

3. **Switch Provider**
   - SD Local → Stock Images (free, fast)
   - DALL-E → Stability AI
   - Any → Pexels/Unsplash (always works)

4. **Check Service Status**
   - Stable Diffusion: Check if WebUI is running
   - Stability AI: Check API status

5. **Reduce Resolution**
   - Lower image size (512x512 instead of 1024x1024)
   - Upscale later if needed

#### Rate Limit Exceeded

**Symptoms:**
- "Rate limit exceeded" error
- "Too many requests" message
- Error code 429

**Causes:**
- Too many requests in short time
- Provider-imposed limits
- Burst of generation attempts

**Solutions:**

1. **Wait** (Limits reset automatically)
   - Most limits: 60 seconds
   - Some limits: 1 hour
   - Check error message for exact time

2. **Slow Down Generation**
   - Don't regenerate rapidly
   - Space out requests by 10+ seconds

3. **Upgrade Plan**
   - OpenAI: Higher tier = higher limits
   - ElevenLabs: Pro plan = more requests

4. **Use Alternative Provider**
   - Switch to provider without rate limit
   - Use local provider (no limits)

---

### Rendering Issues

#### Export (Render) Fails

**Symptoms:**
- "Export failed" error
- Process starts but doesn't complete
- Corrupted output file

**Causes:**
1. FFmpeg not installed
2. Insufficient disk space
3. Invalid output path
4. Corrupted source media
5. Encoding settings incompatible

**Solutions:**

1. **Verify FFmpeg**
   - Go to Downloads page
   - Check FFmpeg status
   - If not installed, click "Install"
   - After install, restart Aura

2. **Check Disk Space**
   - Need 2x video size (for temp files)
   - Typical 1-minute 1080p video: 200-500MB
   - Clear space if needed

3. **Verify Output Path**
   - Directory must exist
   - Path must not contain special characters
   - Try Desktop folder as test

4. **Test Media Files**
   - Play each clip in timeline
   - Replace any corrupted files
   - Regenerate images/audio if needed

5. **Try Simpler Settings**
   - Lower resolution (1080p → 720p)
   - Use H.264 instead of H.265
   - Disable hardware acceleration (test)
   - Try "fast" encoding preset

6. **Check Logs**
   - Look for FFmpeg error in logs
   - Common errors:
     - "Invalid output path" → Path doesn't exist
     - "Codec not supported" → Use different codec
     - "Permission denied" → Check folder permissions

#### Export Too Slow

**Symptoms:**
- Rendering takes very long time
- Progress bar moves slowly
- High CPU usage but low GPU usage

**Solutions:**

1. **Enable Hardware Acceleration**
   - Settings → Render → Hardware Acceleration
   - Options:
     - **NVENC** (NVIDIA GPU)
     - **QuickSync** (Intel integrated graphics)
     - **AMF** (AMD GPU)

2. **Use Faster Encoding Preset**
   - Settings → Render → Encoding Speed
   - "ultrafast" = fastest, larger file
   - "fast" = good balance
   - "slow" = best quality, slowest

3. **Lower Resolution**
   - 4K → 1080p (4x faster)
   - 1080p → 720p (2x faster)

4. **Reduce Bitrate**
   - Lower bitrate = faster encode
   - Test different values

5. **Close Other Applications**
   - Free up CPU/GPU resources
   - Disable browser hardware acceleration

6. **Render Overnight**
   - For very long videos
   - Enable "Shutdown when complete" option

#### Poor Video Quality

**Symptoms:**
- Blurry or pixelated video
- Compression artifacts
- Color banding
- Low audio quality

**Solutions:**

1. **Increase Bitrate**
   - Settings → Render → Video Bitrate
   - 1080p recommended: 8-12 Mbps
   - 4K recommended: 25-40 Mbps

2. **Use Better Codec**
   - H.265 (HEVC) = better quality than H.264
   - Trade-off: Slower encode, not all players support

3. **Enable Two-Pass Encoding**
   - Advanced Mode → Render Settings
   - Slower but better quality

4. **Use Higher Resolution Sources**
   - Generate images at higher resolution
   - Upscale before adding to timeline

5. **Improve Audio**
   - Use higher quality TTS provider
   - Increase audio bitrate (192 or 320 kbps)

6. **Color Grading**
   - Advanced Mode → Color Grading
   - Adjust contrast, saturation, etc.

#### Audio-Video Sync Issues

**Symptoms:**
- Audio doesn't match video
- Drift over time
- Lips don't match speech

**Causes:**
1. Incorrect frame rate
2. Variable frame rate source
3. Audio resampling
4. Timeline editing errors

**Solutions:**

1. **Check Frame Rate**
   - All clips should match (e.g., all 30fps)
   - Mixed frame rates cause sync issues

2. **Verify Timeline Alignment**
   - Zoom in on timeline
   - Check audio starts at same time as video

3. **Re-export Audio**
   - Regenerate TTS
   - Ensure sample rate matches (48kHz recommended)

4. **Use Constant Frame Rate**
   - Avoid variable frame rate (VFR) sources
   - Convert to constant frame rate (CFR)

---

### Provider Issues

#### Invalid API Key Error

**Symptoms:**
- "Invalid API key" error
- "Authentication failed"
- 401 Unauthorized error

**Solutions:**

1. **Re-enter Key**
   - Copy key from provider dashboard
   - Settings → Providers → Paste key
   - Click "Test" to validate

2. **Check Key Format**
   - OpenAI keys start with `sk-`
   - ElevenLabs keys are 32 characters
   - Ensure no spaces before/after

3. **Verify Key Permissions**
   - Some keys have restricted access
   - Generate new key with full permissions

4. **Check Account Status**
   - Verify account not suspended
   - Check payment method valid

5. **Regenerate Key**
   - Provider dashboard → Generate new key
   - Delete old key
   - Add new key to Aura

#### Provider Service Unavailable

**Symptoms:**
- "Service unavailable" error
- "Connection refused"
- Timeout

**Causes:**
1. Provider experiencing outage
2. Network issue
3. Firewall blocking
4. Local engine not running

**Solutions:**

1. **Check Provider Status**
   - Visit provider status page
   - Wait if outage reported

2. **Test Network**
   - Can you access provider website?
   - Try different network
   - Check firewall/proxy settings

3. **For Local Engines:**
   - **Ollama**: Check if service running
   - **Stable Diffusion**: Verify WebUI launched with `--api` flag
   - **Piper**: Check installation path

4. **Switch to Backup Provider**
   - Use Aura's automatic fallback
   - Or manually select alternative

#### Local Engine Issues

**Symptoms:**
- Can't start local engine
- Engine starts but Aura can't connect
- Engine crashes during use

**Solutions:**

**Ollama Issues:**

1. **Verify Installation**
   ```bash
   ollama --version
   ```

2. **Check if Running**
   ```bash
   curl http://localhost:11434/api/tags
   ```

3. **Start Manually**
   ```bash
   ollama serve
   ```

4. **Check Logs**
   - Windows: `%LOCALAPPDATA%\Ollama\logs\`
   - Look for error messages

**Stable Diffusion Issues:**

1. **Launch with API Flag**
   ```bash
   webui.bat --api
   ```

2. **Verify Port**
   - Default: http://localhost:7860
   - Check Settings → Engines for configured port

3. **Check VRAM**
   - Need 4GB+ for SD 1.5
   - Need 6GB+ for SD XL

4. **Test in Browser**
   - Open http://localhost:7860
   - Should see SD WebUI interface

**Piper Issues:**

1. **Verify Installation**
   - Check installation path in Settings
   - Ensure piper.exe exists

2. **Test Command Line**
   ```bash
   piper --help
   ```

3. **Check Voice Files**
   - Voices should be in voices/ subdirectory
   - Download missing voices

---

## Enum Compatibility

Aura Video Studio has evolved its API to use more descriptive enum values. To maintain backward compatibility, the system now accepts both canonical names and legacy aliases.

### Density Values

The API accepts the following density values for content pacing:

| Canonical Value | Legacy Alias | Description |
|----------------|--------------|-------------|
| `Sparse` | - | Less content per minute, slower pacing |
| `Balanced` | `Normal` | Moderate content density (recommended) |
| `Dense` | - | More content per minute, faster pacing |

**Example:**
```json
{
  "density": "Balanced"  // Recommended
}
```

**Legacy Support:**
```json
{
  "density": "Normal"  // Still works, maps to "Balanced"
}
```

### Aspect Ratio Values

The API accepts the following aspect ratio values:

| Canonical Value | Legacy Alias | Description |
|----------------|--------------|-------------|
| `Widescreen16x9` | `16:9` | Standard widescreen (1920x1080) |
| `Vertical9x16` | `9:16` | Mobile/portrait format (1080x1920) |
| `Square1x1` | `1:1` | Square format (1080x1080) |

**Example:**
```json
{
  "aspect": "Widescreen16x9"  // Recommended
}
```

**Legacy Support:**
```json
{
  "aspect": "16:9"  // Still works, maps to "Widescreen16x9"
}
```

### Error Handling

If you provide an invalid or unsupported enum value, the API will return an RFC7807 ProblemDetails response with error code `E303`:

```json
{
  "type": "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E303",
  "title": "Invalid Enum Value",
  "status": 400,
  "detail": "Unknown Density value: 'Medium'. Valid values are: Sparse, Balanced (or Normal), Dense"
}
```

The error message will include:
- The invalid value you provided
- A list of all valid values (including aliases)

### Best Practices

1. **Use Canonical Values**: Prefer canonical enum names (`Balanced`, `Widescreen16x9`) over legacy aliases for clarity
2. **Client-Side Validation**: The Web UI automatically normalizes enum values before sending requests
3. **Check Console Warnings**: When using legacy values, the Web UI logs compatibility warnings to help you migrate
4. **Update Your Code**: While legacy values are supported, consider updating to canonical names for better maintainability

### Migration Guide

If you're migrating from legacy enum values:

**Before:**
```typescript
const request = {
  density: "Normal",
  aspect: "16:9"
};
```

**After:**
```typescript
const request = {
  density: "Balanced",
  aspect: "Widescreen16x9"
};
```

### Client Libraries

If you're using the Aura API from external applications:

**JavaScript/TypeScript:**
```typescript
import { normalizeEnumsForApi } from './utils/enumNormalizer';

// Normalize before sending
const { brief, planSpec } = normalizeEnumsForApi(myBrief, myPlanSpec);
await fetch('/api/script', {
  method: 'POST',
  body: JSON.stringify({ ...brief, ...planSpec })
});
```

**C#:**
The API server automatically handles normalization - you can send either canonical or alias values.

### Common Issues

#### "Failed to generate script" with enum error

**Symptom:** Request fails with E303 error mentioning invalid enum value

**Solution:** 
1. Check that your enum values match either canonical names or supported aliases
2. Ensure proper casing (enums are case-insensitive for parsing, but canonical values use PascalCase)
3. Review the error message for the list of valid values

#### Console warnings about deprecated values

**Symptom:** Browser console shows warnings like "Density 'Normal' is deprecated"

**Solution:**
These are informational warnings and won't block your requests. To eliminate them, update your code to use canonical values.

## Local Engines

### Stable Diffusion Issues

#### GPU Not Detected

**Symptom:** Stable Diffusion fails to start with "No NVIDIA GPU detected"

**Solution:**
1. Run `nvidia-smi` to verify GPU is recognized by the system
2. Update NVIDIA drivers to latest version
3. Check that CUDA is properly installed
4. Restart the application after driver updates

#### Out of Memory Errors

**Symptom:** SD generates black images or crashes with CUDA out of memory

**Solution:**
1. Reduce image resolution in settings (try 512x512 instead of 1024x1024)
2. Close other GPU-intensive applications
3. Use `--medvram` or `--lowvram` flags in SD WebUI settings
4. Consider upgrading GPU or using cloud alternatives

#### Slow Generation Times

**Symptom:** Image generation takes several minutes per image

**Solution:**
1. Verify GPU is being used (check nvidia-smi during generation)
2. Reduce number of inference steps (20-30 is usually sufficient)
3. Use SD 1.5 instead of SDXL for faster results
4. Enable xformers optimization in SD WebUI settings

#### Port Already in Use

**Symptom:** "Port 7860 is already in use"

**Solution:**
1. Check for other SD WebUI instances: `netstat -ano | findstr :7860` (Windows) or `lsof -i :7860` (Linux)
2. Kill the conflicting process or change the port in Settings → Local Engines
3. Use a different port (e.g., 7861, 7862)

### Piper TTS Issues

#### Voice Not Found

**Symptom:** "Voice model not found" error

**Solution:**
1. Download voice models from Settings → Download Center → Models
2. Verify voice files exist in `%LOCALAPPDATA%\Aura\Tools\piper\voices\` (Windows)
3. Check that `.onnx` and `.json` files are both present
4. Re-download the voice model if files are corrupted

#### Audio Artifacts or Distortion

**Symptom:** Generated audio sounds robotic or has clicks/pops

**Solution:**
1. Try a different voice model (some are higher quality than others)
2. Adjust speaking rate in Piper settings (slower = clearer)
3. Ensure input text has proper punctuation
4. Check system audio drivers are up to date

#### Piper Executable Not Found

**Symptom:** "piper.exe not found" error

**Solution:**
1. Reinstall Piper from Settings → Download Center → Engines
2. Verify executable exists in `%LOCALAPPDATA%\Aura\Tools\piper\`
3. Check Windows Defender hasn't quarantined the file
4. Try manual installation following [TTS_LOCAL.md](./TTS_LOCAL.md)

### Mimic3 TTS Issues

#### Server Won't Start

**Symptom:** Mimic3 status shows "Stopped" or "Error"

**Solution:**
1. Check port 59125 is not in use by another application
2. Verify Python 3.9+ is installed (Mimic3 dependency)
3. Check logs in Settings → Engines for specific error messages
4. Try restarting with "Stop" then "Start" in Local Engines settings

#### Voice Quality Issues

**Symptom:** Voices sound unnatural or muffled

**Solution:**
1. Try different voice models from the voice selector
2. Adjust speaking rate and pitch in settings
3. Use SSML tags for fine-grained control
4. Ensure audio output format is compatible (WAV 22050Hz)

#### Slow Response Times

**Symptom:** TTS generation takes 10+ seconds per sentence

**Solution:**
1. Mimic3 is CPU-intensive; close other applications
2. Use Piper instead for faster generation
3. Consider shorter sentences or batching
4. Check CPU usage isn't being throttled

### Installation Issues

#### Download Fails or Times Out

**Symptom:** Engine installation fails during download

**Solution:**
1. Check internet connection is stable
2. Try downloading from a different network
3. Disable VPN if active
4. Use manual installation method from engine documentation

#### Insufficient Disk Space

**Symptom:** "Not enough disk space" error

**Solution:**
1. Free up disk space (SD WebUI needs ~15GB, models need 2-5GB each)
2. Install to a different drive with more space
3. Clean up old model checkpoints you don't use
4. Check TEMP directory isn't full

#### Permission Denied Errors

**Symptom:** "Access denied" or "Permission denied" during installation

**Solution:**
1. Run Aura Video Studio as Administrator (Windows)
2. Check antivirus isn't blocking file writes
3. Verify you have write permissions to `%LOCALAPPDATA%\Aura\Tools\`
4. Temporarily disable Windows Defender during installation

### Provider Fallback Issues

#### Pro Providers Not Working

**Symptom:** Video generation always uses free providers

**Solution:**
1. Verify API keys are configured in Settings → API Keys
2. Check API key validity by testing in provider's web interface
3. Ensure "Offline Mode" is disabled
4. Review preflight check results for specific provider errors

#### Local Providers Not Used

**Symptom:** System uses cloud providers instead of local engines

**Solution:**
1. Verify local engines are installed and running (Settings → Local Engines)
2. Check engine status shows "Ready" (green checkmark)
3. Select appropriate profile (e.g., "Local-First" or "Offline-Only")
4. Review provider priority in profile configuration

#### Fallback Chain Not Working

**Symptom:** Generation fails instead of falling back to alternative

**Solution:**
1. Enable automatic fallback in profile settings
2. Check that fallback providers are configured and ready
3. Review logs for specific failure reasons
4. Ensure offline mode allows fallback to local providers

### General Debugging

#### Enable Verbose Logging

1. Go to Settings → Advanced
2. Enable "Debug Mode"
3. Restart the application
4. Check logs in Settings → Log Viewer

#### Check System Requirements

Run the system profiler:
```powershell
# Windows
.\scripts\audit\profile_system.ps1

# Linux
./scripts/audit/profile_system.sh
```

#### Reset to Defaults

If settings are corrupted:
1. Close Aura Video Studio
2. Delete settings file:
   - Windows: `%LOCALAPPDATA%\Aura\settings.json`
   - Linux: `~/.local/share/aura/settings.json`
3. Restart application (settings will be recreated)

### Additional Resources

- [Local Engines Overview](./ENGINES.md) - Complete engine documentation
- [Stable Diffusion Setup](./ENGINES_SD.md) - SD-specific guide
- [Local TTS Setup](./TTS_LOCAL.md) - Piper and Mimic3 guides
- [GitHub Issues](https://github.com/Coffee285/aura-video-studio/issues) - Report bugs or request help

---

## FFmpeg Issues

### FFmpeg Not Found or Installation Fails

**Symptoms:**
- Download Center shows "FFmpeg v6.0" but download fails with 404
- Error: "FFmpeg binary not found"
- Render fails with "E302-FFMPEG_VALIDATION"

**Solution:**

1. **Use Dynamic Resolution:**
   - The app now dynamically resolves FFmpeg releases from GitHub
   - Download Center will show only real, validated assets
   - If GitHub API fails, mirror URLs will be tried automatically

2. **Attach Existing FFmpeg:**
   - If you already have FFmpeg installed, use "Attach Existing"
   - Point to your ffmpeg.exe location (e.g., `C:\ffmpeg\bin\ffmpeg.exe`)
   - The app will validate and use your installation

3. **Verify Installation:**
   - Use API endpoint: `POST /api/dependencies/ffmpeg/verify`
   - This runs a smoke test to ensure FFmpeg works
   - Check response for validation output and any errors

4. **Repair FFmpeg:**
   - Use API endpoint: `POST /api/dependencies/ffmpeg/repair`
   - This rescans and refreshes FFmpeg paths
   - If FFmpeg is corrupted, reinstall via Download Center

### FFmpeg Crashes During Render

**Symptoms:**
- Error: "FFmpeg crashed during render (exit code: -1094995529)"
- Render fails with "E304-FFMPEG_RUNTIME"
- Invalid data found when processing input

**Common Causes & Solutions:**

1. **Missing Visual C++ Redistributable (Windows):**
   ```
   Error: FFmpeg crashed with negative exit code
   ```
   **Fix:** Install [Microsoft Visual C++ Redistributable](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist)
   - Download and install both x64 and x86 versions
   - Restart your computer
   - Re-verify FFmpeg installation

2. **Corrupted Audio Input:**
   ```
   Error: Invalid data found when processing input
   ```
   **Fix:** The app now automatically validates and repairs audio:
   - Corrupted TTS output is re-encoded to clean WAV
   - If re-encoding fails, app generates silent fallback
   - Check logs for remediation attempts
   
   **Manual Fix:**
   - Delete corrupted narration files
   - Re-generate narration with TTS provider
   - Try a different TTS provider if issue persists

3. **Hardware Encoder Issues:**
   ```
   Error: NVENC encoder not found or crashed
   ```
   **Fix:** Use software encoding:
   - The app will automatically retry with software encoder (libx264)
   - Verify your GPU drivers are up-to-date
   - Consider using software encoding for better compatibility

4. **Antivirus Blocking FFmpeg:**
   ```
   Error: Permission denied or Access is denied
   ```
   **Fix:**
   - Add FFmpeg to antivirus exception list
   - Check Windows Security > Virus & threat protection > Manage settings
   - Ensure FFmpeg has execute permissions

### Audio Validation Failures

**Symptoms:**
- Error: "E305-AUDIO_VALIDATION"
- Narration file too small or corrupted
- TTS provider returns invalid output

**Automatic Remediation:**

The app now includes automatic audio validation and repair:

1. **Pre-Render Validation:**
   - Before rendering, all audio files are validated
   - File size checked (must be > 128 bytes)
   - Audio format validated with ffprobe/ffmpeg

2. **Automatic Re-Encoding:**
   - If audio is corrupted, app attempts re-encode to clean WAV
   - Uses conservative settings: 48kHz, stereo, PCM 16-bit
   - Original file replaced with re-encoded version

3. **Silent Fallback:**
   - If re-encoding fails, app generates silent WAV
   - Job continues with silent audio to prevent complete failure
   - Check logs for "Generated silent fallback" message

**Manual Steps:**

If automatic remediation fails:

1. **Check TTS Provider:**
   ```bash
   # Verify TTS is working
   POST /api/preflight
   ```
   - Ensure selected TTS provider is available
   - Try a different TTS provider (Windows TTS, Piper, etc.)

2. **Regenerate Narration:**
   - Delete corrupted narration files from job artifacts
   - Re-run Quick Demo or job with fresh TTS synthesis
   - Monitor TTS provider for errors

3. **Check FFprobe:**
   ```bash
   ffprobe -v error narration.wav
   ```
   - Should return no errors for valid audio
   - If errors appear, file is corrupted

### Viewing Detailed Logs

All FFmpeg errors include:
- **JobId**: Unique identifier for the render job
- **CorrelationId**: Tracing identifier for debugging
- **Stderr Snippet**: Last 64KB of FFmpeg output
- **Log File**: Full log at `%LOCALAPPDATA%\Aura\Logs\ffmpeg\{jobId}.log`

**Example Error Response:**
```json
{
  "code": "E304-FFMPEG_RUNTIME",
  "message": "FFmpeg crashed during render",
  "exitCode": -1073741515,
  "stderrSnippet": "... last 64KB of output ...",
  "jobId": "abc123",
  "correlationId": "xyz789",
  "suggestedActions": [
    "FFmpeg crashed - binary may be corrupted",
    "Check system dependencies (Visual C++ Redistributable)",
    "Try software encoding (x264) instead of hardware encoding"
  ],
  "ffmpegCommand": "..."
}
```

### Retry Failed Jobs

Use the retry endpoint to attempt job again with different strategy:

```bash
POST /api/jobs/{jobId}/retry?strategy=software-encoder
```

**Strategies:**
- `software-encoder`: Retry with libx264 instead of hardware encoding
- `re-synthesize`: Regenerate TTS narration before retry
- `default`: Standard retry without special handling

### Getting Help

When reporting FFmpeg issues, include:
1. **CorrelationId** and **JobId** from error
2. FFmpeg log file from `%LOCALAPPDATA%\Aura\Logs\ffmpeg\`
3. FFmpeg version: Check in Download Center or run `ffmpeg -version`
4. OS version and GPU model (if using hardware encoding)
5. Steps to reproduce the issue

**Diagnostic Commands:**
```bash
# Verify FFmpeg
POST /api/dependencies/ffmpeg/verify

# Repair FFmpeg
POST /api/dependencies/ffmpeg/repair

# Rescan all dependencies
GET /api/dependencies/rescan
```
