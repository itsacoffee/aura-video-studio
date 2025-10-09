# Visuals Providers Implementation

This document describes the implementation of visual providers with NVIDIA gating and per-scene controls.

## Overview

The visuals system supports multiple providers with automatic fallback:
1. **Stock Providers**: Pexels, Pixabay, Unsplash, Local assets
2. **AI Generation**: Stable Diffusion WebUI (NVIDIA-only)
3. **Fallback**: Slideshow/solid backgrounds

## Stock Providers

### Pexels Provider
```csharp
var provider = new PexelsStockProvider(logger, httpClient, apiKey: "YOUR_KEY");
var assets = await provider.SearchAsync("nature", count: 10, ct);
```

### Pixabay Provider
```csharp
var provider = new PixabayStockProvider(logger, httpClient, apiKey: "YOUR_KEY");
var assets = await provider.SearchAsync("technology", count: 5, ct);
```

### Unsplash Provider
```csharp
var provider = new UnsplashStockProvider(logger, httpClient, apiKey: "YOUR_KEY");
var assets = await provider.SearchAsync("architecture", count: 8, ct);
```

### Local Stock Provider
```csharp
var provider = new LocalStockProvider(logger, baseDirectory: "C:/Assets");
var assets = await provider.SearchAsync("keyword", count: 10, ct);
```

**Note**: All stock providers with API keys return empty results if no key is provided. This enables graceful degradation.

## Stable Diffusion Provider

### NVIDIA Gating

The Stable Diffusion provider enforces strict NVIDIA-only requirements:

```csharp
var provider = new StableDiffusionWebUiProvider(
    logger,
    httpClient,
    baseUrl: "http://127.0.0.1:7860",
    isNvidiaGpu: true,      // REQUIRED: Must be NVIDIA
    vramGB: 12,             // REQUIRED: Minimum 6GB
    defaultParams: sdParams
);
```

**Gating Rules:**
- ❌ **Non-NVIDIA GPU**: Returns empty, logs warning
- ❌ **VRAM < 6GB**: Returns empty, logs warning
- ✅ **NVIDIA + >=6GB**: Proceeds with generation

### Model Selection

Automatic model selection based on VRAM:
- **VRAM >= 12GB**: SDXL (30 steps)
- **VRAM 6-11GB**: SD 1.5 (20 steps)

### Generation Parameters

```csharp
var sdParams = new SDGenerationParams
{
    Model = "SDXL",                          // null = auto-detect
    Steps = 30,                              // null = auto-detect
    CfgScale = 7.0,
    Seed = -1,                               // -1 = random
    Width = 1024,
    Height = 576,
    Style = "photorealistic, detailed",
    SamplerName = "DPM++ 2M Karras"
};
```

### Per-Scene Overrides

```csharp
var overrideParams = new SDGenerationParams
{
    Steps = 40,                 // Override default steps
    CfgScale = 9.0,            // Override CFG scale
    Style = "watercolor art"    // Override style
};

var assets = await provider.FetchOrGenerateAsync(scene, spec, overrideParams, ct);
```

### Probe Functionality

Test SD WebUI availability with a quick low-step probe:

```csharp
bool isAvailable = await provider.ProbeAsync(ct);
if (isAvailable)
{
    // SD is ready, proceed with generation
}
```

The probe performs:
- 256x256 image at 1 step
- 30 second timeout
- Returns false if unavailable or gated

## API Endpoints

### Search Assets

**POST** `/api/assets/search`

```json
{
  "provider": "pexels",
  "query": "mountain sunset",
  "count": 5,
  "apiKey": "YOUR_PEXELS_KEY"
}
```

**Response:**
```json
{
  "success": true,
  "gated": false,
  "assets": [
    {
      "kind": "image",
      "pathOrUrl": "https://...",
      "license": "Pexels License (Free to use)",
      "attribution": "Photo by John Doe on Pexels"
    }
  ]
}
```

**Gated Response (Offline Mode):**
```json
{
  "success": false,
  "gated": true,
  "reason": "Offline mode enabled - only local assets are available",
  "assets": []
}
```

### Generate Assets

**POST** `/api/assets/generate`

```json
{
  "prompt": "A serene mountain landscape at sunset",
  "steps": 25,
  "cfgScale": 7.5,
  "width": 1024,
  "height": 576,
  "style": "photorealistic, detailed",
  "seed": 42
}
```

**Response:**
```json
{
  "success": true,
  "gated": false,
  "model": "SDXL",
  "vramGB": 12,
  "assets": [
    {
      "kind": "image",
      "pathOrUrl": "sd_generated_0_20231009120000.png",
      "license": "Generated locally",
      "attribution": "Generated with Stable Diffusion (SDXL)"
    }
  ]
}
```

**Gated Response (No NVIDIA GPU):**
```json
{
  "success": false,
  "gated": true,
  "reason": "Stable Diffusion requires an NVIDIA GPU. Use stock visuals or Pro cloud instead.",
  "assets": []
}
```

**Gated Response (Insufficient VRAM):**
```json
{
  "success": false,
  "gated": true,
  "reason": "Insufficient VRAM (4GB). Stable Diffusion requires minimum 6GB VRAM.",
  "assets": []
}
```

## UI ViewModels

### CreateViewModel

Visual settings added to the Create Wizard:

```csharp
// Visual mode selection
public string VisualMode { get; set; } = "StockOrLocal";  // Free, StockOrLocal, Pro

// Stock provider toggles
public bool EnablePexels { get; set; } = true;
public bool EnablePixabay { get; set; } = true;
public bool EnableUnsplash { get; set; } = true;
public bool EnableLocalAssets { get; set; } = true;

// API keys
public string? PexelsApiKey { get; set; }
public string? PixabayApiKey { get; set; }
public string? UnsplashApiKey { get; set; }

// Stable Diffusion settings
public bool EnableStableDiffusion { get; set; } = false;
public string? StableDiffusionUrl { get; set; } = "http://127.0.0.1:7860";

// SD Parameters
public string? SdModel { get; set; }  // null = auto-detect
public int SdSteps { get; set; } = 20;
public double SdCfgScale { get; set; } = 7.0;
public int SdSeed { get; set; } = -1;
public string SdStyle { get; set; } = "high quality, detailed, professional";
```

### SceneInspectorViewModel

Per-scene visual overrides:

```csharp
// Override flags
public bool OverrideVisuals { get; set; }
public string? OverrideProvider { get; set; }  // "stock", "sd", "local"

// Stock overrides
public string? OverrideSearchQuery { get; set; }
public int OverrideAssetCount { get; set; } = 1;

// SD overrides
public string? OverrideSdPrompt { get; set; }
public int? OverrideSdSteps { get; set; }
public double? OverrideSdCfgScale { get; set; }
public int? OverrideSdSeed { get; set; }
public string? OverrideSdStyle { get; set; }
```

## Testing

### Unit Tests

```bash
dotnet test Aura.Tests/Aura.Tests.csproj
```

**Test Coverage:**
- ✅ NVIDIA GPU gating (non-NVIDIA returns empty)
- ✅ VRAM gating (< 6GB returns empty)
- ✅ Model selection (SDXL vs SD 1.5)
- ✅ Parameter validation
- ✅ Stock provider API key requirements
- ✅ Local provider without API key
- ✅ Slideshow fallback
- ✅ SD probe functionality

### Integration Tests

```bash
dotnet test Aura.Tests/Aura.Tests.csproj --filter FullyQualifiedName~AssetApiIntegrationTests
```

**Test Coverage:**
- ✅ Offline mode gating
- ✅ GPU vendor validation
- ✅ VRAM threshold enforcement
- ✅ Model selection logic
- ✅ Step adjustment for VRAM
- ✅ API key requirements
- ✅ Gated response explanations

## Examples

### Example 1: Free Path (Stock Only)

```csharp
var pexelsProvider = new PexelsStockProvider(logger, httpClient, apiKey: null);
var assets = await pexelsProvider.SearchAsync("nature", 5, ct);
// Returns empty - no API key

var localProvider = new LocalStockProvider(logger, "C:/Assets");
var localAssets = await localProvider.SearchAsync("nature", 5, ct);
// Returns local assets if available

var slideshowProvider = new SlideshowProvider(logger);
var slides = await slideshowProvider.FetchOrGenerateAsync(scene, spec, ct);
// Always works - ultimate fallback
```

### Example 2: Balanced Path (Stock + SD)

```csharp
// Try stock first
var stockProvider = new PexelsStockProvider(logger, httpClient, apiKey: "KEY");
var stockAssets = await stockProvider.SearchAsync("mountain", 3, ct);

// If NVIDIA GPU available, try SD
if (isNvidiaGpu && vramGB >= 6)
{
    var sdProvider = new StableDiffusionWebUiProvider(
        logger, httpClient, "http://127.0.0.1:7860", 
        isNvidiaGpu: true, vramGB: 12);
    
    var sdAssets = await sdProvider.FetchOrGenerateAsync(scene, spec, ct);
}
```

### Example 3: Per-Scene Overrides

```csharp
// Global defaults
var defaultParams = new SDGenerationParams
{
    Steps = 20,
    CfgScale = 7.0,
    Style = "photorealistic"
};

var provider = new StableDiffusionWebUiProvider(
    logger, httpClient, url, true, 12, defaultParams);

// Scene 1: Use defaults
var assets1 = await provider.FetchOrGenerateAsync(scene1, spec, ct);

// Scene 2: Override to artistic style
var artisticParams = new SDGenerationParams
{
    Steps = 30,
    Style = "oil painting, impressionist"
};
var assets2 = await provider.FetchOrGenerateAsync(scene2, spec, artisticParams, ct);
```

## Architecture Notes

### Provider Hierarchy

```
IImageProvider (interface)
├── StableDiffusionWebUiProvider (NVIDIA-gated)
└── SlideshowProvider (always available)

IStockProvider (interface)
├── PexelsStockProvider (API key optional)
├── PixabayStockProvider (API key optional)
├── UnsplashStockProvider (API key optional)
└── LocalStockProvider (no API key)
```

### Fallback Chain

1. **Pro Providers** (if configured)
   - Stability AI
   - Runway

2. **Local SD** (if NVIDIA + >=6GB VRAM)
   - SDXL (if >=12GB)
   - SD 1.5 (if 6-11GB)

3. **Stock Providers** (if API keys)
   - Pexels
   - Pixabay
   - Unsplash

4. **Local Assets** (always try)

5. **Slideshow** (ultimate fallback)

### Gating Philosophy

Gates provide **explanatory feedback** rather than silent failures:

```json
{
  "success": false,
  "gated": true,
  "reason": "Stable Diffusion requires an NVIDIA GPU. Use stock visuals or Pro cloud instead."
}
```

This allows:
- UI to show tooltips/explanations
- Users to understand why features are unavailable
- Clear guidance on alternatives

## Summary

The visuals implementation provides:
- ✅ Multiple provider options (stock, local, AI)
- ✅ NVIDIA-specific gating for Stable Diffusion
- ✅ VRAM-aware model selection
- ✅ Per-scene parameter overrides
- ✅ Graceful degradation with fallbacks
- ✅ Clear gating explanations
- ✅ Comprehensive test coverage (137 tests)
- ✅ API endpoints with validation
- ✅ ViewModel support for UI

All requirements from the issue have been implemented and tested.
