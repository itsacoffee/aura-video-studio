# LLM Routing Implementation Summary

## Overview

This implementation adds production-grade LLM provider routing with ordered fallbacks to Aura Video Studio. The system supports multiple LLM providers (Free and Pro tiers) with automatic fallback chains when providers fail.

## Components Implemented

### 1. LLM Providers

#### RuleBasedLlmProvider (Free Tier) ✅
- **Location**: `Aura.Providers/Llm/RuleBasedLlmProvider.cs`
- **Features**:
  - Embedded templates for different tones and audiences
  - Deterministic output with fixed seed
  - Adjusts content based on Pacing (Chill/Conversational/Fast) and Density (Sparse/Balanced/Dense)
  - Calculates word count: `duration * wpm * densityFactor`
  - Scene count: ~1 scene per 30 seconds (min 3, max 20)
  - Always available, no API key required

#### OllamaLlmProvider (Free/Local Tier) ✅
- **Location**: `Aura.Providers/Llm/OllamaLlmProvider.cs`
- **Features**:
  - Detects local Ollama instance at `127.0.0.1:11434` (configurable)
  - Default model: `llama3.1:8b-q4_k_m`
  - Retry logic: Max 2 retries with exponential backoff
  - Timeout: Configurable (default 120 seconds)
  - Graceful degradation on connection failure

#### OpenAiLlmProvider (Pro Tier) ✅
- **Location**: `Aura.Providers/Llm/OpenAiLlmProvider.cs`
- **Features**:
  - Uses OpenAI API (default model: `gpt-4o-mini`)
  - Requires API key via IKeyStore
  - Blocks in OfflineOnly mode with E307 error
  - System + user prompt architecture

#### AzureOpenAiLlmProvider (Pro Tier) ✅
- **Location**: `Aura.Providers/Llm/AzureOpenAiLlmProvider.cs`
- **Features**:
  - Uses Azure OpenAI API
  - Requires API key and endpoint URL
  - Configurable deployment name (default: `gpt-4`)
  - Blocks in OfflineOnly mode with E307 error

#### GeminiLlmProvider (Pro Tier) ✅
- **Location**: `Aura.Providers/Llm/GeminiLlmProvider.cs`
- **Features**:
  - Uses Google Gemini API (default model: `gemini-pro`)
  - Requires API key via IKeyStore
  - Blocks in OfflineOnly mode with E307 error

### 2. Routing & Orchestration

#### ScriptOrchestrator ✅
- **Location**: `Aura.Core/Orchestrator/ScriptOrchestrator.cs`
- **Features**:
  - Orchestrates script generation with provider routing
  - Implements fallback chain: Pro → Ollama → RuleBased
  - Handles OfflineOnly mode restrictions
  - Returns detailed error codes (E300, E302, E305, E307)
  - Logs all provider selection decisions

#### ProviderMixer ✅
- **Location**: `Aura.Core/Orchestrator/ProviderMixer.cs`
- **Features**:
  - Selects best available provider based on tier preference
  - Supports "Free", "Pro", and "ProIfAvailable" modes
  - Logs selection decisions with reasons
  - Tracks fallback information

#### LlmProviderFactory ✅
- **Location**: `Aura.Core/Orchestrator/LlmProviderFactory.cs`
- **Features**:
  - Dynamically creates providers based on available API keys
  - Loads API keys from `apikeys.json` (AppData/Aura)
  - Uses reflection to instantiate providers
  - Gracefully handles missing dependencies

### 3. API Integration

#### Updated /script Endpoint ✅
- **Location**: `Aura.Api/Program.cs`
- **Changes**:
  - Now uses `ScriptOrchestrator` instead of single provider
  - Accepts optional `ProviderTier` parameter ("Free", "Pro", "ProIfAvailable")
  - Returns provider info and fallback status in response
  - Handles E307 error for offline restrictions
  - Checks system's OfflineOnly setting

**Request Schema**:
```json
{
  "topic": "Introduction to AI",
  "audience": "Beginners",
  "goal": "Educational",
  "tone": "Friendly",
  "language": "en-US",
  "aspect": "Widescreen16x9",
  "targetDurationMinutes": 5,
  "pacing": "Conversational",
  "density": "Balanced",
  "style": "Tutorial",
  "providerTier": "ProIfAvailable"  // Optional: "Free", "Pro", "ProIfAvailable"
}
```

**Response Schema**:
```json
{
  "success": true,
  "script": "# Introduction to AI\n## Introduction...",
  "provider": "RuleBased",
  "isFallback": false
}
```

## Provider Selection Logic

### Tier Modes

1. **Free**: Uses only free providers (Ollama → RuleBased)
2. **Pro**: Requires Pro providers; E307 error if OfflineOnly
3. **ProIfAvailable**: Pro with graceful downgrade to Free

### Fallback Chain

1. **Pro Tier Request**:
   - Try: OpenAI/Azure/Gemini (first available)
   - Fallback to: Ollama (if available)
   - Final fallback: RuleBased (always available)

2. **Free Tier Request**:
   - Try: Ollama (if available)
   - Fallback: RuleBased (always available)

3. **ProIfAvailable**:
   - Try Pro providers first
   - Gracefully downgrade to Free on failure
   - No E307 error in OfflineOnly mode

### Offline Mode Behavior

When `OfflineOnly` is enabled:
- **Pro tier**: Returns E307 error immediately
- **ProIfAvailable**: Downgrades to Free tier
- **Free tier**: Works normally (Ollama/RuleBased)

## Error Codes

- **E300**: Generic provider failure
- **E301**: Request timeout
- **E302**: Empty script returned
- **E303**: Invalid request parameters
- **E304**: Invalid plan parameters
- **E305**: Provider not available
- **E307**: Pro provider requested in OfflineOnly mode

## Tests

### Unit Tests (8 new)
**File**: `Aura.Tests/ScriptOrchestratorTests.cs`
- ✅ Pro provider selection when available
- ✅ Fallback from Pro to Free on failure
- ✅ E307 blocking for Pro in OfflineOnly
- ✅ Graceful downgrade for ProIfAvailable in OfflineOnly
- ✅ Complete fallback chain (Pro → Ollama → RuleBased)
- ✅ Empty script handling

### Integration Tests (11 new)
**File**: `Aura.Tests/LlmProviderIntegrationTests.cs`
- ✅ RuleBased generates valid scripts
- ✅ Different scripts for different topics
- ✅ Content scales with duration
- ✅ Ollama configuration
- ✅ OpenAI requires API key
- ✅ Azure requires API key and endpoint
- ✅ Gemini requires API key
- ✅ Custom model configurations

### E2E Tests (6 new)
**File**: `Aura.Tests/ScriptEndpointE2ETests.cs`
- ✅ Script generation with only RuleBased
- ✅ Fallback from Pro to RuleBased
- ✅ E307 error in offline mode
- ✅ ProIfAvailable downgrade in offline mode
- ✅ Pacing and density respected
- ✅ Multiple fallback attempts

**Total: 133 tests passing** (108 existing + 25 new)

## Configuration

### API Keys Storage
Location: `%LocalAppData%/Aura/apikeys.json`

```json
{
  "openai": "sk-...",
  "azure_openai_key": "...",
  "azure_openai_endpoint": "https://....openai.azure.com",
  "gemini": "AIza..."
}
```

### Provider Paths
Location: `%LocalAppData%/Aura/provider-paths.json`

```json
{
  "ollamaUrl": "http://127.0.0.1:11434",
  "stableDiffusionUrl": "http://127.0.0.1:7860"
}
```

### Provider Mixing Config
```csharp
var config = new ProviderMixingConfig
{
    ActiveProfile = "Free-Only", // or "Balanced Mix" or "Pro-Max"
    AutoFallback = true,
    LogProviderSelection = true
};
```

## Logging

All provider selection decisions are logged with structured logging:

```
[Script] Provider: OpenAI - Pro provider available and preferred
[Script] Provider: RuleBased (FALLBACK from Pro LLM) - Free fallback - always available
```

## Usage Examples

### Basic Free Tier
```bash
curl -X POST http://127.0.0.1:5005/api/script \
  -H "Content-Type: application/json" \
  -d '{
    "topic": "Introduction to Python",
    "targetDurationMinutes": 3,
    "pacing": "Conversational",
    "density": "Balanced"
  }'
```

### Pro with Fallback
```bash
curl -X POST http://127.0.0.1:5005/api/script \
  -H "Content-Type: application/json" \
  -d '{
    "topic": "Advanced Machine Learning",
    "targetDurationMinutes": 10,
    "pacing": "Fast",
    "density": "Dense",
    "providerTier": "ProIfAvailable"
  }'
```

## Implementation Checklist

- [x] RuleBasedLlmProvider with templates
- [x] OllamaLlmProvider with retry logic
- [x] OpenAiLlmProvider (Pro)
- [x] AzureOpenAiLlmProvider (Pro)
- [x] GeminiLlmProvider (Pro)
- [x] ScriptOrchestrator with routing
- [x] LlmProviderFactory for dynamic instantiation
- [x] API integration with /script endpoint
- [x] OfflineOnly detection and E307 blocking
- [x] Unit tests for routing
- [x] Integration tests for providers
- [x] E2E tests for /script endpoint
- [x] Logging of provider decisions
- [x] Documentation

## Definition of Done ✅

- ✅ `/script` produces valid scenes/lines for Free and Pro paths
- ✅ Routing falls back without crashing
- ✅ Logs show provider selection decisions with reasons
- ✅ All tests pass (133/133)
- ✅ E307 error blocks Pro providers in OfflineOnly mode
- ✅ ProIfAvailable gracefully downgrades
