# Troubleshooting Guide

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
  "type": "https://docs.aura.studio/errors/E303",
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
