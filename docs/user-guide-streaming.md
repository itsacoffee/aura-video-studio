# User Guide: AI Script Generation with Streaming

## Overview

Aura Video Studio now supports real-time streaming script generation with multiple AI providers. Watch your script being generated token-by-token, see live cost estimates, and benefit from automatic provider fallback.

## Features

### 🌐 Multiple AI Providers

Choose from 6 different AI providers based on your needs:

| Provider | Best For | Cost | Speed | Requires |
|----------|----------|------|-------|----------|
| **RuleBased** | Quick templates, offline work | Free | Instant | Nothing |
| **Ollama** | Privacy, offline use | Free | Fast | Local installation |
| **Gemini** | Budget-conscious, good quality | $0.0005/1K | Very fast | Google API key |
| **OpenAI** | Best quality | $0.001/1K | Very fast | OpenAI API key |
| **Azure** | Enterprise compliance | $0.01/1K | Very fast | Azure subscription |
| **Anthropic** | Premium quality | $0.015/1K | Fast | Anthropic API key |

### 📊 Real-Time Metrics

Watch as your script is generated:
- Token-by-token text generation
- Real-time cost tracking
- Generation speed (tokens/second)
- Time to first token
- Progress indicator

### 💰 Cost Transparency

See exactly what you're spending:
- Expected cost before generation starts
- Live cost updates during generation
- Final cost in completion metrics
- Free providers clearly marked

### 🔄 Automatic Fallback

If your preferred provider fails:
1. System tries Ollama (free, local)
2. Falls back to RuleBased (always works)
3. You always get a script!

## How to Use

### Quick Start (Auto Mode)

1. Open the streaming demo at `/streaming-demo` (dev tools enabled)
2. Enter your video topic
3. Leave provider on "Auto (Recommended)"
4. Click "Start Streaming"
5. Watch your script generate in real-time!

**Auto mode** automatically selects the best available provider and falls back if needed.

### Choosing a Specific Provider

1. In the provider section, select your preferred provider
2. Review the provider characteristics:
   - Cost per 1,000 tokens
   - Expected latency
   - Local or Cloud
3. Click "Start Streaming"

### Understanding the Interface

#### Provider Characteristics

Each provider shows:
- **Local/Cloud badge**: Where the AI runs
- **Free/Pro/Enterprise badge**: Pricing tier
- **Cost**: Price per 1,000 tokens
- **Latency**: Expected time to first response

#### During Generation

The streaming metrics show:
- **Provider name**: Which AI is generating
- **Progress bar**: Visual progress indicator
- **Token count**: How many tokens generated
- **Expected cost**: What you'll pay

#### After Completion

Final metrics include:
- **Estimated Cost**: Total cost for this generation
- **Total Tokens**: Number of tokens generated
- **Tokens/Second**: Generation speed achieved
- **Time to First Token**: Initial response latency
- **Total Duration**: Complete generation time
- **Model**: Specific AI model used

### Cancelling Generation

Click the "Cancel" button to stop generation at any time. You'll only pay for tokens generated up to that point.

## Cost Management

### Free Options

**RuleBased**: Always free, template-based scripts
- No AI required
- Works offline
- Instant generation
- Good for simple videos

**Ollama**: Free local AI
- Requires installation
- Works offline
- No usage limits
- Privacy-focused

### Paid Options

**Cost Estimates** (60-second video script, ~150 tokens):
- **Gemini**: ~$0.0001 (basically free)
- **OpenAI**: ~$0.00015
- **Azure**: ~$0.0015
- **Anthropic**: ~$0.00225

💡 **Tip**: Use Gemini for cost-effective quality, or Ollama for unlimited free generation.

## Provider Setup

### RuleBased
✅ No setup required - Always available!

### Ollama
1. Download from [ollama.ai](https://ollama.ai)
2. Install on your computer
3. Run `ollama pull llama3.1` in terminal
4. Ollama will appear as available in Aura

### OpenAI
1. Get API key from [platform.openai.com](https://platform.openai.com)
2. In Aura, go to Settings → Providers
3. Enter your OpenAI API key
4. Save configuration

### Anthropic
1. Get API key from [anthropic.com](https://anthropic.com)
2. In Aura, go to Settings → Providers
3. Enter your Anthropic API key
4. Save configuration

### Gemini
1. Get API key from [ai.google.dev](https://ai.google.dev)
2. In Aura, go to Settings → Providers
3. Enter your Gemini API key
4. Save configuration

### Azure OpenAI
1. Set up Azure OpenAI resource in Azure Portal
2. Get your endpoint URL and API key
3. In Aura, go to Settings → Providers
4. Enter Azure endpoint, key, and deployment name
5. Save configuration

## Best Practices

### For Free Users
- Use **Ollama** for unlimited generation
- Use **RuleBased** for quick templates
- Only use paid providers for final, high-quality scripts

### For Budget-Conscious Users
- Use **Gemini** (cheapest paid option)
- Enable Auto mode for fallback to free providers
- Monitor costs in the metrics panel

### For Quality-Focused Users
- Use **OpenAI GPT-4** for best overall quality
- Use **Anthropic Claude** for premium results
- Budget accordingly (~$0.002 per script)

### For Privacy-Conscious Users
- Use **Ollama** (100% local, private)
- Use **RuleBased** (no AI, no network)
- Avoid cloud providers

### For Enterprise Users
- Use **Azure OpenAI** for compliance
- Configure cost limits
- Track usage with correlation IDs

## Troubleshooting

### "Provider not available"
**Solution**: Check provider configuration in Settings → Providers

### Stream not starting
**Solutions**:
1. Check internet connection
2. Verify API key is valid
3. Check provider status (Ollama running?)
4. Try Auto mode instead

### High latency
**Causes**:
- Slow internet connection
- Provider API congestion
- First-time model loading (Ollama)

**Solutions**:
- Use local provider (Ollama)
- Try different provider
- Check network speed

### Unexpected costs
**Prevention**:
- Review cost before starting
- Use Auto mode for free fallback
- Set budget alerts (coming soon)
- Use free providers for testing

## Tips & Tricks

### 💡 Faster Generation
- Use Ollama (local, no network delay)
- Use Gemini (fastest cloud option)
- Shorter target duration = faster generation

### 💰 Save Money
- Test with RuleBased first
- Use Gemini for cost-effective quality
- Use Auto mode to fallback to free options

### 🎯 Better Quality
- Use OpenAI or Anthropic
- Provide detailed topic description
- Specify target audience clearly
- Use longer target duration for detailed scripts

### 🔒 Maximum Privacy
- Use Ollama exclusively
- Disable all cloud providers
- Use RuleBased as fallback
- No data leaves your computer

## Feedback & Support

Found a bug? Have a suggestion?
- Open an issue on GitHub
- Include correlation ID from error messages
- Describe provider used and settings
- Share cost/metrics if relevant

## What's Next?

Coming soon:
- Integration into main wizard workflow
- Cost tracking and budget management
- Streaming for image prompts
- Collaborative streaming
- Provider health dashboard
- Batch generation with streaming

---

**Enjoy real-time AI script generation with Aura Video Studio!** 🚀
