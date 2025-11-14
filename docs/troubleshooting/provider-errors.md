# Provider Errors

This guide helps you troubleshoot errors related to AI provider services (OpenAI, Anthropic, ElevenLabs, Stability AI, etc.).

## Quick Navigation

- [LLM Errors](#llm-errors) - Language model provider errors
- [Authentication Errors](#authentication) - API key and authentication issues
- [Rate Limits](#rate-limits) - Rate limiting and quota issues
- [TTS Errors](#tts-errors) - Text-to-speech provider errors
- [Visual Errors](#visual-errors) - Image generation provider errors

---

## LLM Errors

Language Model (LLM) provider errors occur when there are issues communicating with OpenAI, Anthropic, or other LLM services.

### Common LLM Error Codes

- **E100**: General LLM provider error
- **E100-401**: LLM authentication failed
- **E100-429**: LLM rate limit exceeded
- **E100-500**: LLM service unavailable

### Symptoms

- Script generation fails with provider error
- Timeout errors during generation
- "Model not available" messages
- Empty or incomplete responses

### Solutions

#### 1. Verify API Key

**Check API Key Configuration:**
```bash
# Navigate to Settings → Providers in the UI
# Or check appsettings.json

# For OpenAI:
{
  "OpenAI": {
    "ApiKey": "sk-..."
  }
}

# For Anthropic:
{
  "Anthropic": {
    "ApiKey": "sk-ant-..."
  }
}
```

**Test API Key:**
1. Go to Settings → Providers
2. Select your LLM provider
3. Click "Test Connection"
4. If test fails, regenerate your API key from the provider's dashboard

#### 2. Check Provider Status

- **OpenAI Status**: https://status.openai.com/
- **Anthropic Status**: https://status.anthropic.com/

If the provider is experiencing an outage, switch to an alternative provider temporarily.

#### 3. Verify Model Availability

Some models may not be available on your account tier:
- GPT-4 requires appropriate access level
- Claude 3 models require Anthropic API access

**To check model access:**
1. Visit your provider's dashboard
2. Check your account tier and available models
3. Update model selection in Aura to use available models

#### 4. Check Request Formatting

Ensure your prompts don't exceed model limits:
- GPT-3.5: 4,096 tokens context
- GPT-4: 8,192 tokens (32k for GPT-4-32k)
- Claude 3: Up to 200k tokens

#### 5. Network and Connectivity

Test network connectivity to provider endpoints:
```bash
# Test OpenAI connectivity
curl https://api.openai.com/v1/models -H "Authorization: Bearer YOUR_KEY"

# Test Anthropic connectivity
curl https://api.anthropic.com/v1/messages -H "x-api-key: YOUR_KEY"
```

---

## Authentication

Authentication errors occur when API keys are invalid, expired, or incorrectly configured.

### Error Codes

- **E100-401**: LLM authentication failed
- **E200-401**: TTS authentication failed
- **E400-401**: Visual provider authentication failed

### Common Causes

1. **Invalid API Key Format**
   - OpenAI keys start with `sk-`
   - Anthropic keys start with `sk-ant-`
   - ElevenLabs keys are UUIDs
   - Stability AI keys are alphanumeric

2. **Expired or Revoked Keys**
   - Keys may expire based on provider policy
   - Keys may be revoked if account has billing issues

3. **Incorrect Environment Configuration**
   - Key stored in wrong configuration location
   - Environment variables not loaded properly

### Solutions

#### Re-configure API Keys

1. **Navigate to Settings → Providers**
2. **For each provider:**
   - Remove existing API key
   - Generate new API key from provider dashboard:
     - **OpenAI**: https://platform.openai.com/api-keys
     - **Anthropic**: https://console.anthropic.com/settings/keys
     - **ElevenLabs**: https://elevenlabs.io/app/settings/api-keys
     - **Stability AI**: https://platform.stability.ai/account/keys
   - Enter new key in Aura
   - Click "Test Connection"
3. **Save settings**

#### Verify Environment Variables

If using environment variables:
```bash
# Linux/Mac
export OPENAI_KEY="sk-..."
export ANTHROPIC_KEY="sk-ant-..."
export ELEVENLABS_KEY="..."
export STABILITY_KEY="..."

# Windows PowerShell
$env:OPENAI_KEY="sk-..."
$env:ANTHROPIC_KEY="sk-ant-..."
$env:ELEVENLABS_KEY="..."
$env:STABILITY_KEY="..."
```

#### Check Configuration Files

Verify `appsettings.json`:
```json
{
  "Providers": {
    "OpenAI": {
      "ApiKey": "sk-...",
      "BaseUrl": "https://api.openai.com/v1"
    },
    "Anthropic": {
      "ApiKey": "sk-ant-...",
      "BaseUrl": "https://api.anthropic.com/v1"
    }
  }
}
```

---

## Rate Limits

Rate limit errors occur when you exceed the allowed number of requests per time period.

### Error Codes

- **E100-429**: LLM rate limit exceeded
- **E429**: General rate limit exceeded

### Understanding Rate Limits

Different providers have different rate limits based on your account tier:

| Provider | Free Tier | Paid Tier |
|----------|-----------|-----------|
| OpenAI | 3 RPM | 60+ RPM |
| Anthropic | 5 RPM | 50+ RPM |
| ElevenLabs | 10 requests/month | Varies by plan |
| Stability AI | 150 credits/month | Varies by plan |

### Solutions

#### 1. Wait and Retry

Most rate limits reset after a short period:
```
Rate limit will reset in: XX seconds
```

Wait for the reset period before retrying.

#### 2. Implement Request Throttling

Aura includes built-in rate limiting. Configure it in `appsettings.json`:
```json
{
  "RateLimiting": {
    "EnableRateLimiting": true,
    "RequestsPerMinute": 10,
    "BurstSize": 20
  }
}
```

#### 3. Upgrade Provider Tier

Consider upgrading your provider account tier for higher rate limits:
- More requests per minute
- Higher token limits
- Priority access during high-traffic periods

#### 4. Use Multiple Providers

Configure fallback providers to distribute load:
1. Go to Settings → Providers
2. Configure multiple LLM providers
3. Enable "Automatic Fallback"
4. Aura will automatically switch to alternative providers if rate limited

#### 5. Optimize Request Patterns

- **Batch similar requests** when possible
- **Cache responses** for repeated queries
- **Use streaming** for long-running generations
- **Implement exponential backoff** (built into Aura)

---

## TTS Errors

Text-to-Speech (TTS) provider errors relate to audio generation services.

### Error Codes

- **E200**: General TTS provider error
- **E200-401**: TTS authentication failed

### Common Issues

#### Voice Not Available
**Problem**: Selected voice is not available on your plan  
**Solution**:
1. Go to Settings → Providers → TTS
2. Click "Refresh Voice List"
3. Select an available voice
4. Or upgrade your provider tier

#### Audio Format Issues
**Problem**: Generated audio format not supported  
**Solution**:
1. Check export settings (Settings → Export)
2. Ensure FFmpeg is installed for format conversion
3. Use MP3 or WAV format for best compatibility

#### Poor Audio Quality
**Problem**: Generated audio sounds robotic or unclear  
**Solution**:
1. Try different voices within the same provider
2. Adjust voice stability and similarity sliders
3. Consider using premium voices
4. Ensure input text has proper punctuation

---

## Visual Errors

Image generation provider errors relate to services like Stability AI, DALL-E, etc.

### Error Codes

- **E400**: General visual provider error
- **E400-401**: Visual provider authentication failed

### Common Issues

#### Content Policy Violations
**Problem**: "Your prompt violated our content policy"  
**Solution**:
1. Review and modify your prompt to remove:
   - Violent or graphic content
   - Inappropriate or NSFW content
   - Copyrighted character names or brands
2. Use more general or abstract descriptions
3. Review provider's content policy

#### Image Generation Failed
**Problem**: Image generation times out or fails  
**Solution**:
1. Simplify your prompt - overly complex prompts can fail
2. Reduce image resolution if using high settings
3. Check provider status
4. Try alternative image generation provider
5. Ensure sufficient VRAM for local generation

#### Wrong Image Style
**Problem**: Generated images don't match expected style  
**Solution**:
1. Add style modifiers to prompt: "photorealistic", "cartoon", "3D render", etc.
2. Adjust generation parameters (CFG scale, steps)
3. Use style presets if available
4. Reference example images in documentation

#### VRAM/Memory Errors (Local Generation)
**Problem**: "Out of memory" errors with local image generation  
**Solution**:
1. Reduce image resolution (512x512 instead of 1024x1024)
2. Close other GPU-intensive applications
3. Check available VRAM:
   ```bash
   nvidia-smi  # Shows GPU memory usage
   ```
4. Consider using cloud-based provider instead

---

## Provider Configuration Best Practices

### API Key Security
- **Never commit API keys** to version control
- Use environment variables or secure configuration
- Rotate keys regularly
- Use separate keys for development and production

### Fallback Strategy
Configure multiple providers for each service type:
```json
{
  "Providers": {
    "LLM": {
      "Primary": "OpenAI",
      "Fallback": ["Anthropic", "LocalLLM"]
    },
    "TTS": {
      "Primary": "ElevenLabs",
      "Fallback": ["AzureTTS"]
    }
  }
}
```

### Monitoring
Enable provider monitoring:
1. Go to Settings → Diagnostics
2. Enable "Provider Health Monitoring"
3. View real-time status in dashboard
4. Set up alerts for provider failures

---

## Related Documentation

- [API Key Setup Guide](../setup/api-keys.md)
- [Network Errors](network-errors.md)
- [Resilience and Retries](resilience.md)
- [General Troubleshooting](Troubleshooting.md)

## Need More Help?

If you continue to experience provider errors:
1. Check the [GitHub Issues](https://github.com/Coffee285/aura-video-studio/issues)
2. Review provider-specific documentation
3. Enable detailed logging (Settings → Diagnostics → Enable Debug Logging)
4. Create a new issue with:
   - Error code and message
   - Provider being used
   - Steps to reproduce
   - Relevant log excerpts (with API keys removed)
