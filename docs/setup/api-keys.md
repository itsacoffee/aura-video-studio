# API Key Setup Guide

This guide walks you through obtaining and configuring API keys for AI providers used in Aura Video Studio.

## Quick Navigation

- [OpenAI](#openai-api-key)
- [Anthropic](#anthropic-api-key)
- [ElevenLabs](#elevenlabs-api-key)
- [Stability AI](#stability-ai-api-key)
- [Configuration Methods](#configuration-methods)
- [Security Best Practices](#security-best-practices)

---

## Overview

Aura Video Studio uses various AI services to generate content. You'll need API keys from the providers you want to use.

### Required vs Optional

**Required** (for basic functionality):
- At least one LLM provider (OpenAI or Anthropic)

**Optional** (for additional features):
- TTS provider (ElevenLabs, etc.) for voiceovers
- Image generation provider (Stability AI, DALL-E) for visuals
- Additional LLM providers for fallback/variety

---

## OpenAI API Key

### What OpenAI Provides

- **Script Generation**: GPT-3.5, GPT-4 for creating video scripts
- **Image Generation**: DALL-E for creating images
- **Text-to-Speech**: OpenAI TTS voices (experimental)

### Getting Your API Key

1. **Create OpenAI Account**:
   - Visit: https://platform.openai.com/signup
   - Sign up with email or Google/Microsoft account
   - Verify your email

2. **Add Payment Method** (Required for API access):
   - Go to: https://platform.openai.com/account/billing
   - Add credit card
   - Set up usage limits (recommended: start with $10-20/month)

3. **Generate API Key**:
   - Go to: https://platform.openai.com/api-keys
   - Click "Create new secret key"
   - Give it a descriptive name: "Aura Video Studio"
   - Copy the key immediately (shown only once!)
   - Format: `sk-proj-...` or `sk-...`

4. **Save API Key Securely**:
   - Store in password manager
   - Never commit to version control
   - Keep separate from shared documents

### OpenAI Pricing

**Pay-as-you-go** based on usage:

| Model | Input (per 1K tokens) | Output (per 1K tokens) |
|-------|----------------------|------------------------|
| GPT-3.5-Turbo | $0.0015 | $0.002 |
| GPT-4 | $0.03 | $0.06 |
| GPT-4-Turbo | $0.01 | $0.03 |

**Estimated costs for video generation**:
- 5-minute script: $0.02-0.10 (GPT-3.5) or $0.20-0.50 (GPT-4)
- Image generation (DALL-E): $0.016-0.08 per image

### Rate Limits

| Tier | RPM (Requests/Min) | TPM (Tokens/Min) |
|------|-------------------|------------------|
| Free | 3 | 200,000 |
| Tier 1 | 60 | 2,000,000 |
| Tier 2 | 3,500 | 5,000,000 |

### Configure in Aura

```json
{
  "Providers": {
    "OpenAI": {
      "ApiKey": "sk-proj-your-key-here",
      "BaseUrl": "https://api.openai.com/v1",
      "DefaultModel": "gpt-3.5-turbo",
      "MaxTokens": 2000
    }
  }
}
```

---

## Anthropic API Key

### What Anthropic Provides

- **Script Generation**: Claude 3 (Haiku, Sonnet, Opus) for creating scripts
- **Long Context**: Up to 200K tokens context window
- **Safety**: Strong content moderation

### Getting Your API Key

1. **Create Anthropic Account**:
   - Visit: https://console.anthropic.com/
   - Sign up with email
   - Verify your email

2. **Add Payment Method**:
   - Go to: https://console.anthropic.com/settings/billing
   - Add credit card
   - Initial free credits provided

3. **Generate API Key**:
   - Go to: https://console.anthropic.com/settings/keys
   - Click "Create Key"
   - Name it: "Aura Video Studio"
   - Copy the key
   - Format: `sk-ant-api03-...`

### Anthropic Pricing

| Model | Input (per 1M tokens) | Output (per 1M tokens) |
|-------|----------------------|------------------------|
| Claude 3 Haiku | $0.25 | $1.25 |
| Claude 3 Sonnet | $3.00 | $15.00 |
| Claude 3 Opus | $15.00 | $75.00 |

**Estimated costs**:
- 5-minute script with Haiku: $0.01-0.05
- 5-minute script with Sonnet: $0.10-0.30

### Rate Limits

| Tier | RPM | Tokens/Min |
|------|-----|------------|
| Free | 5 | 25,000 |
| Tier 1 | 50 | 100,000 |
| Tier 2 | 1,000 | 2,000,000 |

### Configure in Aura

```json
{
  "Providers": {
    "Anthropic": {
      "ApiKey": "sk-ant-api03-your-key-here",
      "BaseUrl": "https://api.anthropic.com/v1",
      "DefaultModel": "claude-3-haiku-20240307",
      "MaxTokens": 2000
    }
  }
}
```

---

## ElevenLabs API Key

### What ElevenLabs Provides

- **Text-to-Speech**: High-quality AI voices
- **Voice Cloning**: Create custom voices
- **Multiple Languages**: 29+ languages supported

### Getting Your API Key

1. **Create ElevenLabs Account**:
   - Visit: https://elevenlabs.io/
   - Sign up with email or Google
   - Free tier includes 10,000 characters/month

2. **Get API Key**:
   - Go to: https://elevenlabs.io/app/settings/api-keys
   - Click "Generate API Key" or view existing key
   - Copy the key
   - Format: UUID (e.g., `a1b2c3d4-e5f6-g7h8-i9j0-k1l2m3n4o5p6`)

### ElevenLabs Pricing

| Plan | Price/Month | Characters/Month | Voices |
|------|-------------|------------------|--------|
| Free | $0 | 10,000 | 3 |
| Starter | $5 | 30,000 | 10 |
| Creator | $22 | 100,000 | 30 |
| Pro | $99 | 500,000 | Unlimited |

**Estimated usage**:
- 5-minute script: ~2,500-3,000 characters
- 10-minute video: ~5,000-6,000 characters

### Rate Limits

- Concurrent requests: 2 (free), up to 10 (pro)
- No explicit per-minute limits

### Configure in Aura

```json
{
  "Providers": {
    "ElevenLabs": {
      "ApiKey": "your-uuid-key-here",
      "BaseUrl": "https://api.elevenlabs.io/v1",
      "DefaultVoice": "21m00Tcm4TlvDq8ikWAM",  // Rachel voice
      "ModelId": "eleven_monolingual_v1"
    }
  }
}
```

---

## Stability AI API Key

### What Stability AI Provides

- **Image Generation**: Stable Diffusion for creating images
- **Image Editing**: Inpainting, outpainting
- **High Resolution**: Up to 1024x1024+

### Getting Your API Key

1. **Create Stability AI Account**:
   - Visit: https://platform.stability.ai/
   - Sign up with email
   - Free credits provided for testing

2. **Get API Key**:
   - Go to: https://platform.stability.ai/account/keys
   - Click "Create API Key"
   - Name it: "Aura Video Studio"
   - Copy the key
   - Format: `sk-...` (alphanumeric)

### Stability AI Pricing

**Credit-based system**:
- Initial free credits: 25
- Pay-as-you-go: ~$0.002 per credit

| Operation | Credits |
|-----------|---------|
| 512x512 image | 0.2 |
| 1024x1024 image | 0.8 |
| Upscaling | 0.2 |

**Estimated costs**:
- 10 images (512x512): $0.004
- 10 images (1024x1024): $0.016

### Rate Limits

- Free tier: 150 requests/10 seconds
- Paid tier: Higher limits based on usage

### Configure in Aura

```json
{
  "Providers": {
    "StabilityAI": {
      "ApiKey": "sk-your-key-here",
      "BaseUrl": "https://api.stability.ai/v1",
      "Engine": "stable-diffusion-xl-1024-v1-0",
      "DefaultSteps": 30
    }
  }
}
```

---

## Configuration Methods

### Method 1: Configuration File (Recommended)

**File**: `appsettings.json`

```json
{
  "Providers": {
    "OpenAI": {
      "ApiKey": "sk-..."
    },
    "Anthropic": {
      "ApiKey": "sk-ant-..."
    },
    "ElevenLabs": {
      "ApiKey": "uuid-..."
    },
    "StabilityAI": {
      "ApiKey": "sk-..."
    }
  }
}
```

**Pros**:
- Easy to manage
- Persists across restarts
- Can include other settings

**Cons**:
- Must protect file from version control
- File permissions important

### Method 2: Environment Variables

```bash
# Windows (PowerShell)
$env:OPENAI_KEY="sk-..."
$env:ANTHROPIC_KEY="sk-ant-..."
$env:ELEVENLABS_KEY="uuid-..."
$env:STABILITY_KEY="sk-..."

# Linux/Mac (Bash)
export OPENAI_KEY="sk-..."
export ANTHROPIC_KEY="sk-ant-..."
export ELEVENLABS_KEY="uuid-..."
export STABILITY_KEY="sk-..."
```

**Make permanent**:

**Linux/Mac**: Add to `~/.bashrc` or `~/.zshrc`
```bash
echo 'export OPENAI_KEY="sk-..."' >> ~/.bashrc
```

**Windows**: Use System Properties → Environment Variables

**Pros**:
- Secure (not in files)
- System-wide availability
- Easy to manage per environment

**Cons**:
- Not visible to other users
- Must set up on each system

### Method 3: UI Configuration

1. **Open Aura Video Studio**
2. **Go to Settings → Providers**
3. **For each provider**:
   - Enter API key
   - Click "Test Connection"
   - Save settings

**Pros**:
- User-friendly
- Visual feedback
- Immediate testing

**Cons**:
- Stored in config file
- Must configure via UI

### Method 4: First Run Wizard

On first launch:
1. Wizard prompts for API keys
2. Enter keys for providers you want to use
3. Skip optional providers
4. Test and save

---

## Testing API Keys

### Test Individual Provider

**Via UI**:
1. Settings → Providers
2. Select provider
3. Click "Test Connection"
4. View result (success or error)

**Via API**:
```bash
# Test OpenAI
curl -X POST http://localhost:5005/api/providers/openai/test \
  -H "Content-Type: application/json"

# Test Anthropic
curl -X POST http://localhost:5005/api/providers/anthropic/test \
  -H "Content-Type: application/json"
```

**Via CLI**:
```bash
dotnet run --project Aura.Cli -- test-provider openai
dotnet run --project Aura.Cli -- test-provider anthropic
```

### Test All Providers

```bash
# Via API
curl http://localhost:5005/api/providers/test-all

# Response
{
  "results": {
    "openai": {
      "status": "success",
      "message": "Connection successful"
    },
    "anthropic": {
      "status": "error",
      "message": "Invalid API key"
    }
  }
}
```

### Common Test Failures

**"Invalid API Key"**:
- Key is wrong or expired
- Format is incorrect
- Key not activated yet

**"Authentication Failed"**:
- Account has billing issues
- Key permissions insufficient
- Account suspended

**"Connection Timeout"**:
- Network issue
- Firewall blocking
- Provider endpoint down

---

## Security Best Practices

### Do's

✅ **Use environment variables** for production
✅ **Store keys in password manager**
✅ **Rotate keys regularly** (every 90 days)
✅ **Use separate keys** for dev/prod
✅ **Set usage limits** on provider dashboards
✅ **Monitor usage** regularly
✅ **Revoke unused keys**
✅ **Use principle of least privilege**

### Don'ts

❌ **Never commit keys to Git**
❌ **Don't share keys via email/chat**
❌ **Don't embed in client-side code**
❌ **Don't use production keys in development**
❌ **Don't share keys between projects**
❌ **Don't ignore security warnings**

### Protecting Configuration Files

**`.gitignore`**:
```
appsettings.json
appsettings.*.json
!appsettings.example.json
.env
*.key
```

**File Permissions**:
```bash
# Linux/Mac - Make config file readable only by owner
chmod 600 appsettings.json

# Windows - Use File Properties → Security
```

### Key Rotation

**Regular rotation**:
1. Generate new key on provider dashboard
2. Test new key in development
3. Update production configuration
4. Revoke old key
5. Monitor for issues

**Automated rotation** (advanced):
```json
{
  "Security": {
    "ApiKeyRotation": {
      "Enabled": true,
      "RotationDays": 90,
      "WarnDays": 7
    }
  }
}
```

### Emergency Key Revocation

**If key is compromised**:
1. **Immediately revoke** on provider dashboard
2. **Generate new key**
3. **Update all systems**
4. **Review usage logs** for unauthorized access
5. **Check billing** for unexpected charges
6. **Report incident** if needed

---

## Troubleshooting

### Key Not Working

1. **Verify format**:
   - OpenAI: `sk-` or `sk-proj-`
   - Anthropic: `sk-ant-api03-`
   - ElevenLabs: UUID format
   - Stability AI: Alphanumeric

2. **Check activation**:
   - Some keys take minutes to activate
   - Verify email confirmation completed

3. **Test directly**:
   ```bash
   # OpenAI
   curl https://api.openai.com/v1/models \
     -H "Authorization: Bearer YOUR_KEY"
   ```

### Rate Limit Issues

1. **Check current usage**:
   - Visit provider dashboard
   - View rate limit information

2. **Upgrade tier** if needed

3. **Implement rate limiting in Aura**:
   ```json
   {
     "RateLimiting": {
       "RequestsPerMinute": 10,
       "BurstSize": 20
     }
   }
   ```

### Billing Issues

1. **Verify payment method** on provider dashboard
2. **Check usage alerts** and limits
3. **Review unexpected charges**
4. **Set up budget alerts**

---

## Related Documentation

- [Provider Configuration Guide](../../PROVIDER_INTEGRATION_GUIDE.md)
- [First Run Setup](../getting-started/INSTALLATION.md)
- [Provider Errors Troubleshooting](../troubleshooting/provider-errors.md)
- Security Guide

## Need More Help?

If you have issues with API keys:
1. Check provider-specific documentation
2. Review [Provider Errors guide](../troubleshooting/provider-errors.md#authentication)
3. Test keys directly with provider API
4. Contact provider support if key issues persist
5. Check [GitHub Issues](https://github.com/Coffee285/aura-video-studio/issues) for similar problems
