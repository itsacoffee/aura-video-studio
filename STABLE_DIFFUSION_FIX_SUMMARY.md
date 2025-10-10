# Stable Diffusion Integration Fixes - Summary

## Issues Addressed

This PR fixes the issues reported in the problem statement:

### 1. ✅ RuleBasedLlmProvider Constructor Issue

**Problem**: `System.MissingMethodException: Constructor on type 'Aura.Providers.Llm.RuleBasedLlmProvider' not found.`

**Root Cause**: The `LlmProviderFactory` was creating a generic `ILogger` and passing it to `RuleBasedLlmProvider`, which expects `ILogger<RuleBasedLlmProvider>`.

**Solution**: Modified `LlmProviderFactory.CreateRuleBasedProvider()` to use reflection to call `ILoggerFactory.CreateLogger<T>()` with the correct generic type parameter.

**Files Changed**:
- `Aura.Core/Orchestrator/LlmProviderFactory.cs`

### 2. ✅ Stable Diffusion Validation Improvements

**Problem**: App validation shows SD as not available despite it running with API enabled. The validator was getting 404 on `/sdapi/v1/sd-models`.

**Root Cause**: The validator was only checking the API endpoint without first verifying if SD WebUI was running at all.

**Solution**: 
- Check base URL (`/`) first to confirm SD WebUI is running
- Then check `/sdapi/v1/sd-models` to verify API is enabled
- Provide clear, actionable error messages for each failure scenario
- Better error messages mentioning the need for `--api` flag

**Files Changed**:
- `Aura.Providers/Validation/StableDiffusionValidator.cs`
- `Aura.Core/Hardware/HardwareDetector.cs`

### 3. ✅ Quality/Choice Customization

**Problem**: Users couldn't customize quality/generation settings regardless of detected GPU type.

**Root Cause**: Hardcoded GPU type and VRAM checks blocked all non-NVIDIA or low-VRAM setups from using Stable Diffusion.

**Solution**: Added `BypassHardwareChecks` flag to:
- `AssetGenerateRequest` - API request model
- `StableDiffusionWebUiProvider` constructor
- API endpoint logic in `Program.cs`

When `BypassHardwareChecks=true`, the following restrictions are bypassed:
- NVIDIA GPU requirement (allows AMD, Intel, or other GPUs)
- VRAM >= 6GB requirement

Users can still customize:
- `Model` (SDXL, SD 1.5, etc.)
- `Steps` (number of diffusion steps)
- `Width` and `Height` (image dimensions)
- `CfgScale` (prompt adherence)
- `Seed` (reproducibility)
- `Style` (prompt style modifiers)
- `SamplerName` (sampling algorithm)

**Files Changed**:
- `Aura.Api/Program.cs`
- `Aura.Providers/Images/StableDiffusionWebUiProvider.cs`

### 4. ✅ Auto-Install Dependencies

**Problem**: Users need ability to auto-install FFmpeg and other dependencies.

**Solution**: Already implemented! The `DependencyManager` class provides full auto-install functionality via API endpoints:

- `GET /api/downloads/manifest` - List available components
- `GET /api/downloads/{component}/status` - Check installation status
- `POST /api/downloads/{component}/install` - Download and install
- `GET /api/downloads/{component}/verify` - Verify integrity
- `POST /api/downloads/{component}/repair` - Repair installation
- `DELETE /api/downloads/{component}` - Uninstall
- `GET /api/downloads/{component}/manual` - Get manual instructions

Currently supports:
- **FFmpeg** (required for video rendering)
- **Ollama** (optional for local LLM)

**Files**: No changes needed - functionality already exists in `Aura.Core/Dependencies/DependencyManager.cs`

## Usage Examples

### Example 1: Bypass GPU checks and use custom settings

```bash
curl -X POST http://localhost:5005/api/assets/generate \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "A beautiful sunset over mountains",
    "model": "SD 1.5",
    "steps": 25,
    "width": 512,
    "height": 512,
    "bypassHardwareChecks": true
  }'
```

### Example 2: Auto-install FFmpeg

```bash
# Check if FFmpeg is installed
curl http://localhost:5005/api/downloads/FFmpeg/status

# Install FFmpeg
curl -X POST http://localhost:5005/api/downloads/FFmpeg/install

# Verify installation
curl http://localhost:5005/api/downloads/FFmpeg/verify
```

### Example 3: Test Stable Diffusion connection

```bash
curl -X POST http://localhost:5005/api/providers/test/stablediffusion \
  -H "Content-Type: application/json" \
  -d '{
    "url": "http://127.0.0.1:7860"
  }'
```

## Testing

All 264 existing unit tests pass:
```
Passed!  - Failed: 0, Passed: 264, Skipped: 0, Total: 264
```

No breaking changes were introduced - only additions and improvements to existing functionality.

## Migration Guide

No migration needed! These changes are backward compatible:

- `BypassHardwareChecks` defaults to `false`, maintaining existing behavior
- All new parameters are optional
- Existing code continues to work without modifications

## Documentation Updates

Users should be informed that:

1. **Stable Diffusion requires --api flag**: When starting SD WebUI, use:
   ```bash
   # Windows
   webui.bat --api
   
   # Linux/Mac
   ./webui.sh --api
   ```

2. **Bypassing hardware checks**: Advanced users can bypass GPU/VRAM restrictions by setting `BypassHardwareChecks=true` in API requests. This is useful for:
   - Running SD on non-NVIDIA GPUs (AMD, Intel Arc)
   - Running with less than 6GB VRAM (using lower settings)
   - Testing or development purposes

3. **Auto-install functionality**: Use the Downloads page in the UI or the `/api/downloads` endpoints to automatically install required dependencies like FFmpeg.

## Known Limitations

- Bypassing hardware checks doesn't guarantee successful generation - SD WebUI must still support your hardware
- Some GPU vendors may require specific SD WebUI configurations (e.g., AMD requires DirectML backend)
- Lower VRAM systems should use smaller dimensions and fewer steps to avoid OOM errors
