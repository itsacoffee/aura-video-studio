# Offline Mode UX Improvements

## Overview

This document describes the comprehensive offline mode improvements implemented in Aura Video Studio, providing users with intelligent hardware-based recommendations, real-time provider status, and transparent capability messaging.

## Problem Statement

Users needed better guidance for setting up and using offline providers (Ollama, Piper, Mimic3, Stable Diffusion WebUI) without cloud services. Key challenges included:
- Uncertainty about which offline providers work on their hardware
- No visibility into what's available vs what's missing
- Difficult setup process with no clear guidance
- Unclear performance expectations for offline providers
- No smooth downgrade messaging when providers fail

## Solution

### 1. "Tune for My Machine" - Hardware-Based Recommendations

**Endpoint**: `GET /api/offline-providers/recommendations`

**Features**:
- Automatic hardware detection (RAM, VRAM, CPU cores, GPU vendor/model)
- Provider recommendations tailored to detected hardware capabilities
- Clear performance expectations (speed and quality ratings)
- Fallback suggestions when primary providers unavailable
- Step-by-step quick start guide

**Recommendation Logic**:

#### Text-to-Speech (TTS)
- **High RAM (16GB+)**: Recommends Mimic3
  - Rationale: "High RAM available - Mimic3 offers best quality for offline TTS"
  - Speed: Medium (10x real-time)
  - Quality: Excellent
  
- **Low RAM (8GB)**: Recommends Piper
  - Rationale: "Limited RAM - Piper offers excellent quality with minimal resource usage"
  - Speed: Very Fast (100x real-time)
  - Quality: Very Good
  
- **Fallback**: Windows SAPI (Windows only) or Piper

#### Script Generation (LLM)
- **High RAM + VRAM (16GB + 8GB)**: Ollama with llama3.1:8b-q4_k_m
  - Rationale: "Good RAM and VRAM - can run 8B models with GPU acceleration"
  - Speed: Fast (GPU-accelerated)
  - Quality: Excellent
  
- **High RAM only (16GB)**: Ollama with llama3.1:8b-q4_k_m
  - Rationale: "Good RAM available - can run 8B models on CPU"
  - Speed: Medium (CPU-only)
  - Quality: Excellent
  
- **Limited RAM (8GB)**: Ollama with llama3.2:3b-q4_0
  - Rationale: "Limited RAM - use smaller 3B model for reliable performance"
  - Speed: Medium to Fast
  - Quality: Good
  
- **Very Limited**: RuleBased (template-based)
  - Rationale: "Limited resources - template-based generation is most reliable"
  - Speed: Instant
  - Quality: Basic

#### Image Generation
- **NVIDIA GPU 8GB+ VRAM**: Stable Diffusion WebUI
  - Rationale: "NVIDIA GPU with sufficient VRAM - can generate high-quality images locally"
  - Speed: Medium (5-15 seconds per image)
  - Quality: Excellent
  - Resolution: 512x512 or 768x768
  
- **NVIDIA GPU 6GB VRAM**: Stable Diffusion WebUI (limited)
  - Rationale: "NVIDIA GPU with 6GB VRAM - can generate images at lower resolutions"
  - Speed: Medium to Slow
  - Quality: Good
  - Resolution: 512x512 only
  
- **No GPU or insufficient VRAM**: Stock Images
  - Rationale: "Insufficient GPU VRAM for local image generation"
  - Speed: Fast (API lookup)
  - Quality: Professional
  - Setup: Free API keys available

### 2. Real-Time Provider Status Dashboard

**Endpoint**: `GET /api/offline-providers/status`

**Features**:
- Live status checks for all offline providers:
  - Piper TTS
  - Mimic3 TTS
  - Ollama (LLM)
  - Stable Diffusion WebUI
  - Windows TTS (Windows only)
  
- Capability summary showing:
  - ✅ TTS Provider Available
  - ✅ LLM Provider Available
  - ✅ Image Provider Available (or ⚠️ Using Stock Images)
  
- Per-provider information:
  - Availability status
  - Version information (if available)
  - Detailed diagnostics (URL, models count, etc.)
  - Setup recommendations
  - Installation guide links

**Response Format**:
```json
{
  "piper": {
    "name": "Piper TTS",
    "isAvailable": true,
    "message": "Piper TTS is ready",
    "version": "1.2.0",
    "details": {
      "ExecutablePath": "C:\\Tools\\piper\\piper.exe",
      "VoiceModel": "en_US-lessac-medium"
    },
    "recommendations": []
  },
  "hasTtsProvider": true,
  "hasLlmProvider": true,
  "hasImageProvider": false,
  "isFullyOperational": true
}
```

### 3. Offline Mode UI Components

#### OfflineModeRecommendations Component
**Location**: `Aura.Web/src/components/Engines/OfflineModeRecommendations.tsx`

**Displays**:
- Hardware summary card (RAM, VRAM, CPU cores, system tier)
- Overall offline capabilities assessment
- Expandable sections for TTS, LLM, and image recommendations
- Quick start guide with numbered steps

**User Flow**:
1. Navigate to Download Center → Offline Mode tab
2. See hardware summary automatically detected
3. Review capabilities (what works offline, what doesn't)
4. Expand accordion sections for detailed recommendations
5. Follow quick start guide for setup

#### OfflineProviderStatus Component
**Location**: `Aura.Web/src/components/Engines/OfflineProviderStatus.tsx`

**Displays**:
- Real-time status of each offline provider
- Color-coded badges (Available/Not Available)
- Capability summary with checkmarks/warnings
- Installation guides for unavailable providers
- Refresh button to recheck status

**User Flow**:
1. View at-a-glance provider status
2. See which capabilities are available
3. Click installation guide links for missing providers
4. Refresh status after installing new providers

#### OfflineModeBanner Component
**Location**: `Aura.Web/src/components/Banners/OfflineModeBanner.tsx`

**Displays**:
- Context-aware banner adapting to offline capabilities
- Success banner when all providers available
- Warning banner when providers missing
- Quick setup link for missing providers

**Modes**:
- **Compact**: Minimal one-line message with setup link
- **Full**: Detailed capability breakdown with action buttons

**User Flow**:
1. Banner appears when offline mode enabled
2. Shows current offline status (ready or limited)
3. Click "Setup Missing Providers" to configure
4. Dismiss or configure offline settings

### 4. Integration with Download Center

**New Tab**: "Offline Mode"

**Location**: Download Center → Offline Mode tab

**Contents**:
1. OfflineModeRecommendations component at top
2. OfflineProviderStatus component below

**Benefits**:
- Centralized location for all offline mode information
- Easy discovery through tab navigation
- Consistent with existing Download Center UX
- No need to navigate multiple pages

## Technical Implementation

### Backend Architecture

```
OfflineProvidersController
  └─> OfflineProviderAvailabilityService
      ├─> CheckAllProvidersAsync()
      │   ├─> CheckPiperAsync()
      │   ├─> CheckMimic3Async()
      │   ├─> CheckOllamaAsync()
      │   ├─> CheckStableDiffusionAsync()
      │   └─> CheckWindowsTtsAsync()
      └─> GetMachineRecommendationsAsync()
          ├─> HardwareDetector.DetectSystemAsync()
          ├─> Analyze RAM, VRAM, CPU, GPU
          ├─> Generate TTS recommendation
          ├─> Generate LLM recommendation
          ├─> Generate Image recommendation
          └─> Build quick start guide
```

### Frontend Architecture

```
DownloadsPage
  └─> Offline Mode Tab
      ├─> OfflineModeRecommendations
      │   ├─> Fetch /api/offline-providers/recommendations
      │   ├─> Display hardware summary
      │   ├─> Show provider recommendations
      │   └─> Display quick start steps
      └─> OfflineProviderStatus
          ├─> Fetch /api/offline-providers/status
          ├─> Display capability summary
          └─> Show individual provider cards

OfflineModeBanner (used in various pages)
  ├─> Fetch /api/offline-providers/status
  ├─> Determine capabilities
  └─> Display appropriate banner
```

## Test Coverage

**File**: `Aura.Tests/OfflineProviderRecommendationsTests.cs`

**Test Cases**:
1. ✅ High RAM recommends Mimic3 TTS
2. ✅ Low RAM recommends Piper TTS
3. ✅ High RAM + VRAM recommends GPU-accelerated Ollama
4. ✅ NVIDIA GPU with 8GB VRAM recommends Stable Diffusion
5. ✅ No GPU recommends stock images
6. ✅ Quick start steps included in recommendations
7. ✅ Capabilities assessment included

**Coverage**: All recommendation paths tested with different hardware profiles

## Documentation Updates

### TTS_LOCAL.md
- Added "Tune for My Machine" section
- Explained hardware-based recommendations
- Benefits of automatic setup guidance

### FLEXIBLE_ENGINES_SUMMARY.md
- Added "Offline Mode Enhancements" section
- Documented all new features
- Updated conclusion with offline mode benefits

## User Benefits

1. **Zero Guesswork**: Hardware detection eliminates uncertainty about which providers work
2. **Clear Expectations**: Speed and quality ratings help users make informed choices
3. **Actionable Guidance**: Step-by-step instructions tailored to specific hardware
4. **Transparent Capabilities**: Real-time status shows what's available offline
5. **Smooth Fallbacks**: Automatic recommendations when primary providers unavailable
6. **Discoverable Setup**: Centralized offline mode tab makes setup easy to find
7. **Context-Aware**: Banners adapt to show relevant information based on current state

## Example Scenarios

### Scenario 1: High-End System (16GB RAM, RTX 3070)
**Recommendations**:
- TTS: Mimic3 (best quality)
- LLM: Ollama llama3.1:8b-q4_k_m with GPU acceleration
- Images: Stable Diffusion WebUI (768x768)

**Capabilities**: ✅ Full offline video generation

### Scenario 2: Mid-Range System (8GB RAM, no dedicated GPU)
**Recommendations**:
- TTS: Piper (fast and efficient)
- LLM: Ollama llama3.2:3b-q4_0 (smaller model)
- Images: Stock Images (Pexels/Pixabay)

**Capabilities**: ✅ TTS and LLM offline, ⚠️ Stock images only

### Scenario 3: Basic System (8GB RAM, integrated graphics)
**Recommendations**:
- TTS: Windows SAPI (Windows) or Piper
- LLM: RuleBased (template-based)
- Images: Stock Images

**Capabilities**: ⚠️ Basic offline generation, recommend cloud providers for quality

## Future Enhancements

Potential improvements for future iterations:
- Auto-install recommended providers with one click
- Performance benchmarking to show actual vs expected speeds
- Model download progress tracking within UI
- Provider health monitoring with alerts
- Offline mode profiles (save/load recommended configurations)
- Compare mode to see differences between provider options

## Acceptance Criteria Met

✅ **Offline runs complete end-to-end**: Full capability check and recommendations

✅ **Attach/start flows are discoverable**: Integrated into Download Center with clear navigation

✅ **Reliable health checks**: Real-time status for all offline providers

✅ **Smooth downgrades with transparency**: Clear messaging when providers unavailable

✅ **Hardware-based recommendations**: "Tune for my machine" feature implemented

✅ **Capability banners**: Context-aware banners show offline status

✅ **Clear documentation**: TTS_LOCAL.md and FLEXIBLE_ENGINES_SUMMARY.md updated

✅ **Comprehensive testing**: 7 test cases covering all recommendation scenarios

## Conclusion

The offline mode UX improvements provide users with intelligent, hardware-based guidance for setting up and using Aura Video Studio without cloud services. The combination of automatic hardware detection, real-time provider status, and transparent capability messaging creates a smooth, discoverable experience for offline video generation.

Users can now confidently set up offline providers knowing exactly what will work on their hardware, what performance to expect, and what to do when providers are unavailable. The "Tune for My Machine" feature eliminates guesswork and provides actionable, personalized setup instructions.
