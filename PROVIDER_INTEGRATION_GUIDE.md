# Provider Integration Guide

Comprehensive guide for integrating and using providers in Aura Video Studio's video generation pipeline.

## Table of Contents
1. [Overview](#overview)
2. [LLM Providers](#llm-providers)
3. [TTS Providers](#tts-providers)
4. [Image Providers](#image-providers)
5. [Fallback Strategies](#fallback-strategies)
6. [Error Handling](#error-handling)
7. [Performance Considerations](#performance-considerations)

## Overview

Aura Video Studio uses a modular provider system for:
- **LLM (Large Language Models)**: Script generation
- **TTS (Text-to-Speech)**: Audio narration synthesis
- **Image Generation**: Visual content creation
- **Stock Images**: Ready-made visual assets

Each provider implements a common interface, enabling fallback chains and easy swapping.

## LLM Providers

### OpenAI (GPT-4, GPT-3.5)

**Location**: `Aura.Providers/Llm/OpenAiLlmProvider.cs`

**Configuration**:
```json
{
  "Providers": {
    "Llm": {
      "OpenAI": {
        "ApiKey": "sk-...",
        "Model": "gpt-4-turbo-preview",
        "BaseUrl": "https://api.openai.com/v1"
      }
    }
  }
}
```

**Models**:
- `gpt-4-turbo-preview`: Best quality, highest cost
- `gpt-4`: Excellent quality, high cost
- `gpt-3.5-turbo`: Good quality, low cost

**Rate Limits**:
- Tier 1: 3 requests/min, 40k tokens/min
- Tier 2: 60 requests/min, 1M tokens/min
- Check your quota at https://platform.openai.com/account/rate-limits

**Error Handling**:
- 429 (Rate Limit): Retry with exponential backoff
- 401 (Auth): Invalid API key - check configuration
- 500 (Server): Transient error - retry

**Cost**: ~$0.03 per 30-second script (GPT-4)

### Anthropic (Claude)

**Location**: `Aura.Providers/Llm/AnthropicLlmProvider.cs`

**Configuration**:
```json
{
  "Providers": {
    "Llm": {
      "Anthropic": {
        "ApiKey": "sk-ant-...",
        "Model": "claude-3-opus-20240229",
        "BaseUrl": "https://api.anthropic.com"
      }
    }
  }
}
```

**Models**:
- `claude-3-opus-20240229`: Highest quality
- `claude-3-sonnet-20240229`: Balanced
- `claude-3-haiku-20240307`: Fast and economical

**Rate Limits**:
- Tier 1: 5 requests/min, 10k tokens/min
- Tier 2: 50 requests/min, 100k tokens/min

**Unique Features**:
- 100K context window (vs 8K for GPT-3.5)
- Strong instruction following
- Excellent creative writing

**Cost**: ~$0.045 per 30-second script (Opus)

### Google Gemini

**Location**: `Aura.Providers/Llm/GeminiLlmProvider.cs`

**Configuration**:
```json
{
  "Providers": {
    "Llm": {
      "Gemini": {
        "ApiKey": "...",
        "Model": "gemini-pro",
        "BaseUrl": "https://generativelanguage.googleapis.com"
      }
    }
  }
}
```

**Models**:
- `gemini-pro`: General purpose, competitive with GPT-3.5
- `gemini-pro-vision`: Multimodal (not yet used in Aura)

**Rate Limits**:
- Free tier: 60 requests/min
- Paid tier: Custom limits

**Advantages**:
- Generous free tier
- Fast response times
- Strong reasoning capabilities

**Cost**: Free (with usage limits), ~$0.01 per script when paid

### Ollama (Local LLMs)

**Location**: `Aura.Providers/Llm/OllamaLlmProvider.cs`

**Configuration**:
```json
{
  "Providers": {
    "Llm": {
      "Ollama": {
        "BaseUrl": "http://localhost:11434",
        "Model": "mistral:7b"
      }
    }
  }
}
```

**Prerequisites**:
1. Install Ollama: https://ollama.ai
2. Pull model: `ollama pull mistral:7b`
3. Ensure service is running

**Recommended Models**:
- `mistral:7b`: Best balance of quality/speed (4GB VRAM)
- `llama2:13b`: Higher quality (8GB VRAM)
- `codellama:7b`: Good for technical content

**Advantages**:
- Completely offline
- No API costs
- Privacy-preserving
- No rate limits

**Limitations**:
- Requires local GPU (8GB+ VRAM recommended)
- Slower than cloud providers
- Quality varies by model

**Performance**: ~30-60s for script generation on RTX 3060

### RuleBased (Fallback)

**Location**: `Aura.Providers/Llm/RuleBasedLlmProvider.cs`

**Configuration**: No configuration required (always available)

**Use Case**: Automatic fallback when all other providers fail

**Features**:
- Template-based script generation
- Deterministic output
- Always succeeds
- No external dependencies

**Quality**: Basic but functional - suitable for demos and testing

## TTS Providers

### ElevenLabs (Premium)

**Location**: `Aura.Providers/Tts/ElevenLabsTtsProvider.cs`

**Configuration**:
```json
{
  "Providers": {
    "Tts": {
      "ElevenLabs": {
        "ApiKey": "...",
        "VoiceId": "21m00Tcm4TlvDq8ikWAM", // Rachel
        "Model": "eleven_multilingual_v2"
      }
    }
  }
}
```

**Voice Selection**:
- Browse: https://elevenlabs.io/voice-library
- Top voices: Rachel, Adam, Bella, Antoni
- Custom voice cloning available (paid)

**Models**:
- `eleven_multilingual_v2`: 28 languages, highest quality
- `eleven_monolingual_v1`: English only, faster

**Rate Limits**:
- Free: 10k characters/month
- Starter: 30k characters/month ($5/mo)
- Creator: 100k characters/month ($22/mo)

**Advantages**:
- Exceptional naturalness
- Emotional range
- Voice cloning capability

**Cost**: ~$0.30 per minute of audio (Starter tier)

### PlayHT (Premium)

**Location**: `Aura.Providers/Tts/PlayHTTtsProvider.cs`

**Configuration**:
```json
{
  "Providers": {
    "Tts": {
      "PlayHT": {
        "ApiKey": "...",
        "UserId": "...",
        "VoiceId": "larry"
      }
    }
  }
}
```

**Advantages**:
- High-quality voices
- Ultra-realistic cloning
- SSML support
- Good pronunciation

**Rate Limits**:
- Free: 12.5k words/month
- Creator: 500k words/month ($19/mo)

**Cost**: ~$0.18 per minute of audio

### Azure TTS

**Location**: `Aura.Providers/Tts/AzureTtsProvider.cs`

**Configuration**:
```json
{
  "Providers": {
    "Tts": {
      "Azure": {
        "SubscriptionKey": "...",
        "Region": "eastus",
        "VoiceName": "en-US-JennyNeural"
      }
    }
  }
}
```

**Voice Selection**:
- Browse: https://speech.microsoft.com/portal/voicegallery
- Neural voices: High quality
- Standard voices: Lower quality, cheaper

**Rate Limits**:
- Free: 0.5M characters/month
- Standard: Pay per use

**Advantages**:
- Enterprise-grade reliability
- Many languages and voices
- SSML support
- Tunable styles (newscast, cheerful, sad)

**Cost**: $16 per million characters

### Windows SAPI (Free)

**Location**: `Aura.Providers/Tts/WindowsTtsProvider.cs`

**Configuration**: No configuration required (Windows only)

**Advantages**:
- Completely free
- No API keys
- Works offline
- No rate limits

**Limitations**:
- Windows only
- Lower quality (robotic)
- Limited voice selection
- Basic SSML only

**Use Case**: Demo, testing, offline mode

### Piper (Free, Offline)

**Location**: `Aura.Providers/Tts/PiperTtsProvider.cs`

**Configuration**:
```json
{
  "Providers": {
    "Tts": {
      "Piper": {
        "ExecutablePath": "C:/path/to/piper.exe",
        "ModelPath": "C:/path/to/en_US-lessac-medium.onnx",
        "ConfigPath": "C:/path/to/en_US-lessac-medium.onnx.json"
      }
    }
  }
}
```

**Prerequisites**:
1. Download Piper: https://github.com/rhasspy/piper
2. Download voice models
3. Place in accessible directory

**Advantages**:
- Completely offline
- Neural TTS quality
- Fast inference
- No costs

**Quality**: Good (better than Windows SAPI, not as good as ElevenLabs)

**Performance**: ~10x faster than realtime on modern CPU

### Mimic3 (Free, Offline)

**Location**: `Aura.Providers/Tts/Mimic3TtsProvider.cs`

**Configuration**:
```json
{
  "Providers": {
    "Tts": {
      "Mimic3": {
        "ServerUrl": "http://localhost:59125"
      }
    }
  }
}
```

**Prerequisites**:
1. Install Mimic3 server
2. Start server: `mimic3-server`

**Advantages**:
- Open source
- Offline
- Multiple voices
- Active development

**Quality**: Moderate (comparable to Piper)

## Image Providers

### Stable Diffusion WebUI (Local)

**Location**: `Aura.Providers/Images/StableDiffusionWebUiProvider.cs`

**Configuration**:
```json
{
  "Providers": {
    "Images": {
      "StableDiffusionWebUI": {
        "ApiUrl": "http://localhost:7860",
        "Model": "sd_xl_base_1.0.safetensors",
        "Steps": 30,
        "CfgScale": 7.5
      }
    }
  }
}
```

**Prerequisites**:
1. Install AUTOMATIC1111 WebUI
2. Download model checkpoint
3. Start with --api flag
4. Requires 8GB+ VRAM

**Advantages**:
- Complete control
- No API costs
- Offline capable
- Custom models

**Performance**: ~10-30s per image (RTX 3060)

### Stability AI (Cloud)

**Location**: `Aura.Providers/Images/StabilityImageProvider.cs`

**Configuration**:
```json
{
  "Providers": {
    "Images": {
      "Stability": {
        "ApiKey": "sk-...",
        "Engine": "stable-diffusion-xl-1024-v1-0"
      }
    }
  }
}
```

**Cost**: ~$0.02 per image

### Stock Image Providers

**Pexels** (Free):
```json
{
  "Providers": {
    "Images": {
      "Pexels": {
        "ApiKey": "..."
      }
    }
  }
}
```

**Pixabay** (Free):
```json
{
  "Providers": {
    "Images": {
      "Pixabay": {
        "ApiKey": "..."
      }
    }
  }
}
```

**Unsplash** (Free):
```json
{
  "Providers": {
    "Images": {
      "Unsplash": {
        "ApiKey": "..."
      }
    }
  }
}
```

**Rate Limits**: 5000 requests/hour (Pexels), varies by service

**Advantages**:
- Free
- High quality
- Large library
- No attribution required (check license)

## Fallback Strategies

### LLM Fallback Chain
```
OpenAI/Anthropic/Gemini → Ollama → RuleBased
```

**Configuration**:
```json
{
  "Providers": {
    "Llm": {
      "Primary": "OpenAI",
      "Fallback": ["Ollama", "RuleBased"]
    }
  }
}
```

### TTS Fallback Chain
```
ElevenLabs/PlayHT → Azure → Piper/Mimic3 → Windows SAPI
```

**Rationale**:
1. Premium providers for best quality
2. Cloud fallback for reliability
3. Offline providers for robustness
4. Always-available fallback (Windows SAPI)

### Image Fallback Chain
```
StableDiffusion → Stability API → Stock Images → Solid Color
```

**Automatic fallback** on:
- Provider unavailable
- API error
- Quota exceeded
- Validation failure

## Error Handling

### Common Errors and Solutions

**401 Unauthorized**:
- **Cause**: Invalid API key
- **Solution**: Check configuration, regenerate key
- **Fallback**: Try next provider in chain

**429 Rate Limit**:
- **Cause**: Exceeded quota
- **Solution**: Wait and retry (exponential backoff)
- **Fallback**: Switch to different provider

**503 Service Unavailable**:
- **Cause**: Provider maintenance or outage
- **Solution**: Retry after delay
- **Fallback**: Use fallback provider

**Validation Failed**:
- **Cause**: Output doesn't meet quality standards
- **Solution**: Retry with adjusted parameters
- **Fallback**: Try different provider

### Retry Logic

**ProviderRetryWrapper** handles:
- Maximum 3 retries per provider
- Exponential backoff (1s, 2s, 4s)
- Transient error detection
- Automatic provider switching

## Performance Considerations

### Optimization Tips

1. **Cache Results**: Store scripts/audio for repeated briefs
2. **Parallel Execution**: Generate audio and images simultaneously
3. **Batch Processing**: Queue multiple jobs
4. **Hardware Acceleration**: Use GPU for local inference
5. **Pre-warm Providers**: Initialize connections at startup

### Resource Management

**Memory**:
- LLM inference: 4-16GB VRAM (local)
- Stable Diffusion: 8-12GB VRAM
- TTS synthesis: < 1GB RAM

**Disk Space**:
- Model storage: 10-50GB (local providers)
- Temp files: 1-5GB per job (cleaned up automatically)

**Network**:
- Script generation: < 10KB
- TTS audio: 1-5MB per minute
- Images: 0.5-2MB per image

### Monitoring

**Key Metrics**:
- Provider success rate
- Average latency per stage
- Cost per video
- Fallback trigger frequency

**Logging**:
- All provider calls logged with correlation IDs
- Errors logged with full context
- Performance metrics captured

## Provider Selection Guidelines

### Quality Priority
```
LLM: GPT-4 > Claude Opus > Gemini Pro > Ollama > RuleBased
TTS: ElevenLabs > PlayHT > Azure > Piper > Windows SAPI
Images: SD WebUI > Stability API > Stock > Solid Color
```

### Cost Priority
```
LLM: RuleBased > Ollama > Gemini > GPT-3.5 > Claude
TTS: Windows SAPI > Piper > Azure > PlayHT > ElevenLabs
Images: Solid Color > Stock > SD WebUI > Stability
```

### Speed Priority
```
LLM: Gemini > GPT-3.5 > Claude > Ollama > RuleBased
TTS: Azure > Piper > ElevenLabs > PlayHT > Mimic3
Images: Stock > Stability API > SD WebUI
```

### Offline Priority
```
LLM: Ollama > RuleBased
TTS: Piper > Mimic3 > Windows SAPI
Images: SD WebUI (if available) > Solid Color
```

## Troubleshooting

### Provider Not Working

1. **Check configuration**: Validate API keys, URLs, paths
2. **Test connectivity**: Ping provider endpoints
3. **Review logs**: Check for specific error messages
4. **Verify quota**: Ensure not rate limited
5. **Update provider**: Check for API version changes

### Poor Quality Output

1. **Adjust parameters**: Increase quality settings
2. **Try different model**: Some models work better for specific content
3. **Upgrade tier**: Free tiers may have quality limitations
4. **Use premium provider**: Consider paid options

### Slow Performance

1. **Enable hardware acceleration**: Use NVENC/AMF/QSV
2. **Optimize provider selection**: Choose faster providers
3. **Reduce quality**: Lower settings for faster processing
4. **Use caching**: Avoid regenerating identical content
5. **Upgrade hardware**: More RAM/VRAM helps

## Best Practices

1. **Always configure fallbacks**: Never rely on single provider
2. **Monitor costs**: Track API usage and expenses
3. **Test providers**: Validate quality before production
4. **Secure API keys**: Use environment variables, never commit
5. **Handle errors gracefully**: Provide user-friendly messages
6. **Log everything**: Correlation IDs for debugging
7. **Update regularly**: Keep providers and libraries current
8. **Respect rate limits**: Implement proper backoff
9. **Clean up resources**: Delete temp files after use
10. **Document changes**: Note provider version and config

## Additional Resources

- OpenAI Documentation: https://platform.openai.com/docs
- Anthropic Documentation: https://docs.anthropic.com
- Google AI Studio: https://makersuite.google.com
- Ollama Models: https://ollama.ai/library
- ElevenLabs Voice Library: https://elevenlabs.io/voice-library
- Azure TTS Documentation: https://learn.microsoft.com/azure/ai-services/speech-service/
- Piper TTS: https://github.com/rhasspy/piper
- AUTOMATIC1111 WebUI: https://github.com/AUTOMATIC1111/stable-diffusion-webui

## Support

For provider-specific issues:
- Check provider's status page
- Review provider documentation
- Contact provider support
- Ask in Aura community forums

For Aura-specific integration issues:
- Check logs in `logs/` directory
- Enable debug logging
- File issue on GitHub with correlation ID
- Include provider name and configuration (sanitized)
