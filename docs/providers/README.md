# Provider System Overview

Aura Video Studio uses a modular provider system for AI services, allowing you to choose between cloud-based premium services, free alternatives, and local/offline options.

## Provider Categories

### LLM Providers (Script Generation)

Generate video scripts from creative briefs.

| Provider | Type | Cost | Quality | Offline | Configuration |
|----------|------|------|---------|---------|---------------|
| **OpenAI** (GPT-4, GPT-3.5) | Cloud | Paid | Excellent | No | API key required |
| **Anthropic** (Claude) | Cloud | Paid | Excellent | No | API key required |
| **Google Gemini** | Cloud | Free tier | Good | No | API key required |
| **Ollama** | Local | Free | Good | Yes | Local installation |
| **RuleBased** | Local | Free | Basic | Yes | No config needed |

**Recommended**: Start with OpenAI GPT-4 for best results, use RuleBased as fallback for offline mode.

**Documentation**:
- [LLM Orchestrator Guide](UNIFIED_LLM_ORCHESTRATOR_GUIDE.md)
- [LLM Caching](LLM_CACHE_GUIDE.md)
- [LLM Latency Management](LLM_LATENCY_MANAGEMENT.md)
- [LLM Output Validation](LLM_OUTPUT_VALIDATION_GUIDE.md)
- [Ollama Model Selection](OLLAMA_MODEL_SELECTION.md)
- [Windows Testing Guide](WINDOWS_LLM_PROVIDER_TESTING_GUIDE.md)

### TTS Providers (Voice Synthesis)

Convert script text to speech audio.

| Provider | Type | Cost | Quality | Offline | Voice Cloning | Configuration |
|----------|------|------|---------|---------|---------------|---------------|
| **ElevenLabs** | Cloud | Paid | Excellent | No | Yes | API key required |
| **PlayHT** | Cloud | Paid | Excellent | Yes | API key required |
| **Windows SAPI** | Local | Free | Good | Yes | No | Windows only, built-in |
| **Piper** | Local | Free | Good | Yes | No | Download models |
| **Mimic3** | Local | Free | Basic | Yes | No | Local installation |

**Recommended**: ElevenLabs for premium projects, Windows SAPI for quick testing and offline work.

**Documentation**:
- [TTS Validation Index](TTS_VALIDATION_INDEX.md)
- [TTS Quick Start](TTS_VALIDATION_QUICK_START.md)

### Image Providers (Visual Generation)

Generate or select images for video scenes.

| Provider | Type | Cost | Quality | Offline | Configuration |
|----------|------|------|---------|---------|---------------|
| **Stable Diffusion WebUI** | Local | Free | Excellent | Yes | Local GPU installation |
| **Replicate** | Cloud | Paid | Excellent | No | API key required |
| **Stock Images** | Fallback | Free | Basic | Yes | Built-in placeholders |

**Recommended**: Stable Diffusion for best quality (requires GPU), Stock Images as fallback.

### Video Rendering (FFmpeg)

Render final video from timeline composition.

| Method | Hardware | Performance | Quality | Configuration |
|--------|----------|-------------|---------|---------------|
| **NVENC** | NVIDIA GPU | Fastest | Excellent | Auto-detected |
| **AMF** | AMD GPU | Fast | Excellent | Auto-detected |
| **QuickSync** | Intel GPU | Fast | Good | Auto-detected |
| **CPU (libx264)** | CPU only | Slow | Excellent | Fallback |

**Recommended**: Hardware acceleration automatically detected and used when available.

## Provider Profiles

Aura Video Studio includes pre-configured profiles to balance cost, quality, and availability:

### 1. Free-Only Profile
- **LLM**: RuleBased (offline)
- **TTS**: Windows SAPI or Piper (offline)
- **Images**: Stock images (offline)
- **Rendering**: CPU or available GPU

**Use case**: No API keys required, completely offline, basic quality.

### 2. Balanced Mix Profile
- **LLM**: OpenAI with RuleBased fallback
- **TTS**: ElevenLabs with Windows SAPI fallback
- **Images**: Stable Diffusion with stock fallback
- **Rendering**: Hardware acceleration with CPU fallback

**Use case**: Best balance of quality and cost with resilient fallbacks.

### 3. Pro-Max Profile
- **LLM**: OpenAI GPT-4 (primary), Claude (fallback)
- **TTS**: ElevenLabs (premium voices)
- **Images**: Stable Diffusion or Replicate
- **Rendering**: Hardware acceleration

**Use case**: Professional projects, maximum quality, API keys required.

## Configuration

### API Keys

Store provider API keys securely:

1. **Via UI**: Settings → Providers → Enter API key
2. **Via Environment Variables**: Set in `.env.local` file

API keys are encrypted at rest using platform-appropriate storage.

**Documentation**: [Provider API Key Management](PROVIDER_API_KEY_MANAGEMENT.md)

### Provider Selection

Configure which providers to use for each service:

1. **Automatic**: Choose a provider profile (Free-Only, Balanced, Pro-Max)
2. **Manual**: Select specific providers in Settings → Providers

The system will automatically fall back to available providers if the primary fails.

**Documentation**: [Provider Stickiness Usage](PROVIDER_STICKINESS_USAGE_EXAMPLES.md)

### Fallback Strategy

When a provider fails, the system:
1. Logs the failure with reason
2. Attempts the next provider in the fallback chain
3. Continues until success or all providers exhausted
4. Returns error only if all providers fail

This ensures resilience even with intermittent API failures.

## Performance Considerations

### LLM Providers
- **Latency**: OpenAI and Anthropic typically respond in 2-10 seconds
- **Caching**: Responses cached to avoid redundant API calls
- **Rate Limits**: Automatic retry with exponential backoff

### TTS Providers
- **Latency**: Cloud TTS (2-5 seconds per scene), Local TTS (<1 second per scene)
- **Quality vs Speed**: Premium cloud services offer best quality but higher latency
- **Batch Processing**: Multiple scenes can be processed in parallel

### Image Providers
- **Generation Time**: Stable Diffusion (5-30 seconds per image depending on GPU)
- **GPU Requirements**: CUDA-capable NVIDIA GPU recommended for Stable Diffusion
- **Fallback**: Stock images instant, suitable for testing

### Video Rendering
- **Hardware Acceleration**: 5-10x faster than CPU encoding
- **Multi-pass Encoding**: Higher quality at cost of longer render time
- **Resolution Impact**: 4K takes significantly longer than 1080p

## Troubleshooting

### Provider Not Available
- **Check API key**: Verify key is valid and has credits/quota remaining
- **Check network**: Cloud providers require internet connectivity
- **Check local services**: Ensure Ollama, Stable Diffusion WebUI, etc. are running

### Quality Issues
- **LLM**: Try GPT-4 instead of GPT-3.5 for better script quality
- **TTS**: Premium providers (ElevenLabs, PlayHT) offer better voice quality
- **Images**: Use Stable Diffusion with appropriate prompts and settings

### Performance Issues
- **Enable hardware acceleration**: Check GPU is detected in Settings → System
- **Reduce quality settings**: Lower resolution, bitrate, or use faster models
- **Use local providers**: Avoid network latency with offline alternatives

## Next Steps

- [LLM Orchestrator Guide](UNIFIED_LLM_ORCHESTRATOR_GUIDE.md) - Deep dive into LLM system
- [TTS Quick Start](TTS_VALIDATION_QUICK_START.md) - Set up text-to-speech
- [API Key Management](PROVIDER_API_KEY_MANAGEMENT.md) - Secure key storage
- [User Guide](../user-guide/USER_MANUAL.md) - Complete user documentation

---

Last updated: 2025-11-18
