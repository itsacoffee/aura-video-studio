# Provider Integration Guide

Comprehensive guide for integrating and using providers in Aura Video Studio's video generation pipeline.

## Table of Contents
1. [Overview](#overview)
2. [Provider Profiles](#provider-profiles)
3. [LLM Providers](#llm-providers)
4. [TTS Providers](#tts-providers)
5. [Image Providers](#image-providers)
6. [Fallback Strategies](#fallback-strategies)
7. [Error Handling](#error-handling)
8. [Performance Considerations](#performance-considerations)

## Overview

Aura Video Studio uses a modular provider system for:
- **LLM (Large Language Models)**: Script generation
- **TTS (Text-to-Speech)**: Audio narration synthesis
- **Image Generation**: Visual content creation
- **Stock Images**: Ready-made visual assets

Each provider implements a common interface, enabling fallback chains and easy swapping.

## Provider Profiles

Aura Video Studio includes three pre-configured provider profiles that balance cost, quality, and API requirements.

### Free-Only Profile

**Description**: Uses only free and offline providers. No API keys required.

**Ideal for**:
- Testing and development
- Offline environments
- Learning the platform
- Personal projects with no budget

**Providers**:
- **LLM**: Ollama (local, free) with fallback to rule-based
- **TTS**: Windows SAPI or Piper TTS (local, free)
- **Images**: Local stock images
- **Video**: Software encoding with hardware acceleration when available

**Quality**: Acceptable for internal videos and prototypes

**Cost**: $0

### Balanced Mix Profile

**Description**: Combines free and premium providers for good quality at reasonable cost.

**Ideal for**:
- Small businesses
- Content creators on budget
- Regular video production
- Testing premium features

**Providers**:
- **LLM**: OpenAI GPT-3.5-turbo with Ollama fallback
- **TTS**: ElevenLabs (if configured) with SAPI fallback
- **Images**: Pexels/Pixabay (free API) with local stock fallback
- **Video**: Hardware-accelerated encoding when available

**Required API Keys**: OpenAI (GPT-3.5 is cost-effective)

**Quality**: Professional quality for most use cases

**Cost**: ~$0.10 - $0.50 per video

### Pro-Max Profile

**Description**: Premium providers for highest quality. Multiple paid API keys required.

**Ideal for**:
- Production environments
- Marketing teams
- High-quality content requirements
- Client-facing videos

**Providers**:
- **LLM**: OpenAI GPT-4-turbo with Anthropic Claude fallback
- **TTS**: ElevenLabs premium voices with PlayHT fallback
- **Images**: Stable Diffusion WebUI or Stability AI with Pexels fallback
- **Video**: Hardware-accelerated encoding (NVENC preferred)

**Required API Keys**: OpenAI, ElevenLabs, Stability AI or SD WebUI URL

**Quality**: Maximum quality, suitable for professional production

**Cost**: ~$1 - $5 per video (varies with length and complexity)

### Managing Profiles

#### Via Settings UI

1. Navigate to **Settings** → **Provider Profiles**
2. View available profiles with tier badges and descriptions
3. Click **Validate** to check if all required API keys are configured
4. Select desired profile and click **Apply Profile**
5. View **Smart Recommendation** for AI-suggested profile based on your setup

#### Via API

```bash
# Get all profiles
GET /api/provider-profiles

# Get active profile
GET /api/provider-profiles/active

# Set active profile
POST /api/provider-profiles/active
{
  "profileId": "balanced-mix"
}

# Validate a profile
POST /api/provider-profiles/{profileId}/validate

# Get recommended profile
GET /api/provider-profiles/recommend
```

#### Validation

Each profile can be validated to check if:
- All required API keys are present
- Keys are properly formatted
- Providers are accessible (optional connectivity test)

Validation results show:
- ✅ Valid: All requirements met
- ❌ Invalid: Missing keys or configuration issues
- Specific missing keys listed for easy troubleshooting

### API Key Management

#### Secure Storage

- **Windows**: API keys encrypted using Data Protection API (DPAPI)
- **Linux/macOS**: Keys stored in user directory with file system permissions
- All keys masked in logs and diagnostics
- Never exposed in error messages or telemetry

#### Adding API Keys

```bash
# Via API
POST /api/provider-profiles/keys
{
  "keys": {
    "openai": "sk-...",
    "elevenlabs": "...",
    "stabilityai": "sk-..."
  }
}

# Via Settings UI
1. Navigate to Settings → API Keys
2. Enter API key for desired provider
3. Click "Test" to validate connectivity
4. Click "Save API Keys"
```

#### Testing API Keys

Individual provider API keys can be tested before saving:

```bash
POST /api/provider-profiles/test
{
  "provider": "openai",
  "apiKey": "sk-..."
}
```

Response indicates:
- Success: Key is valid and provider is accessible
- Failure: Key invalid or provider unreachable
- Error message with troubleshooting guidance

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

### Stock Media Providers (Images and Videos)

Aura Video Studio integrates with leading stock media platforms to provide high-quality images and videos alongside AI-generated content.

#### Pexels (Images + Videos)

**Location**: `Aura.Providers/Images/EnhancedPexelsProvider.cs`

**Configuration**:
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

**Features**:
- Free API with 200 requests/hour
- Images and videos supported
- Commercial use allowed, no attribution required
- High-resolution downloads
- Orientation and color filters

**Get API Key**: https://www.pexels.com/api/

**Licensing**: Pexels License - Free for commercial use, attribution appreciated but not required

#### Unsplash (Images Only)

**Location**: `Aura.Providers/Images/EnhancedUnsplashProvider.cs`

**Configuration**:
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

**Features**:
- Free API with 50 requests/hour
- Images only (no video)
- Commercial use allowed, attribution required
- High-quality curated content
- Download tracking required per API guidelines

**Get API Key**: https://unsplash.com/developers

**Licensing**: Unsplash License - Free for commercial use, attribution required

**Important**: Unsplash requires download tracking per their API guidelines. The provider automatically handles this.

#### Pixabay (Images + Videos)

**Location**: `Aura.Providers/Images/EnhancedPixabayProvider.cs`

**Configuration**:
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

**Features**:
- Free API with generous limits
- Images and videos supported
- Commercial use allowed, no attribution required
- Large content library
- Safe search filters

**Get API Key**: https://pixabay.com/api/docs/

**Licensing**: Pixabay License - Free for commercial use, attribution not required

### Unified Stock Media Search

**Location**: `Aura.Core/Services/StockMedia/UnifiedStockMediaService.cs`

The unified stock media service searches across multiple providers simultaneously, merges results, removes duplicates, and applies content safety filters.

**Features**:
- Multi-provider search with unified results
- Perceptual hashing for duplicate detection
- Content safety filtering
- Relevance scoring
- Rate limit monitoring
- Licensing metadata capture

**API Endpoints**:

```bash
# Search across multiple providers
POST /api/stock-media/search
{
  "query": "mountain landscape",
  "mediaType": "Image",  # or "Video"
  "providers": ["Pexels", "Unsplash", "Pixabay"],
  "count": 20,
  "safeSearchEnabled": true,
  "orientation": "landscape"
}

# Compose optimized query using LLM
POST /api/stock-media/compose-query
{
  "sceneDescription": "Serene mountain vista at sunrise",
  "keywords": ["mountain", "sunrise", "landscape"],
  "targetProvider": "Pexels",
  "mediaType": "Image",
  "style": "cinematic",
  "mood": "peaceful"
}

# Get blend recommendation (stock vs generative)
POST /api/stock-media/recommend-blend
{
  "sceneDescriptions": ["Mountain landscape", "City skyline"],
  "videoGoal": "Travel documentary",
  "videoStyle": "cinematic",
  "budget": 50,
  "allowGenerative": true,
  "allowStock": true
}

# Check rate limits
GET /api/stock-media/rate-limits

# Validate API keys
POST /api/stock-media/validate-providers
```

### LLM-Assisted Query Composition

**Location**: `Aura.Core/Services/StockMedia/QueryCompositionService.cs`

The LLM-assisted query composition service uses AI to generate optimized search queries for each provider based on scene descriptions and context.

**Features**:
- Provider-specific query optimization
- Alternative query suggestions
- Negative filter recommendations
- Confidence scoring
- Fallback query generation

**Example**:
```
Input: "A person walking through a busy city street at sunset"
Pexels Query: "urban sunset pedestrian street"
Unsplash Query: "city sunset walking"
Pixabay Query: "busy street golden hour"
```

### Blend Set Recommendations

The system can recommend an optimal mix of stock media vs AI-generated content for each scene based on:
- Budget constraints
- Content availability
- Scene requirements
- Narrative coverage
- Visual consistency

**Strategy Examples**:
- Generic scenes (landscapes, objects) → prefer stock (cheaper, faster)
- Unique/specific concepts → prefer generative (more control)
- Mixed approach for optimal cost/quality balance

### Content Safety Filtering

**Location**: `Aura.Core/Services/StockMedia/ContentSafetyFilterService.cs`

Stock media results can be filtered for inappropriate content:

**Features**:
- Keyword-based filtering
- Sensitive content detection
- Custom blocked/allowed lists
- Query sanitization
- Safety level configuration

**Safety Levels**:
- 0-3: Minimal filtering
- 4-6: Moderate filtering (default)
- 7-9: Strict filtering
- 10: Maximum filtering

### Licensing Metadata

All stock media results include comprehensive licensing information:

```json
{
  "licenseType": "Pexels License",
  "attribution": "Photo by John Doe on Pexels",
  "licenseUrl": "https://www.pexels.com/license/",
  "commercialUseAllowed": true,
  "attributionRequired": false,
  "creatorName": "John Doe",
  "creatorUrl": "https://www.pexels.com/@john",
  "sourcePlatform": "Pexels"
}
```

**Export Options**:
- CSV export for spreadsheet tracking
- JSON export for programmatic access
- Formatted attribution text for video credits
- Licensing summary with warnings

### Perceptual Hashing and Deduplication

**Location**: `Aura.Core/Services/StockMedia/PerceptualHashService.cs`

The service automatically detects and removes duplicate images across providers using perceptual hashing:

**Features**:
- URL-based hash generation
- Dimension-aware comparison
- Configurable similarity threshold (default 90%)
- Duplicate detection across providers

### Best Practices

1. **Always capture licensing metadata** during content selection
2. **Export licensing reports** with each video delivery
3. **Validate commercial use** before finalizing videos for clients
4. **Track attribution requirements** per provider guidelines
5. **Monitor rate limits** to avoid API throttling
6. **Use blend recommendations** to optimize cost and quality
7. **Enable content safety filters** for brand-safe content
8. **Compose queries with LLM** for better search results

### Visual Asset Selection Workflow

**Overview**: After image generation, Aura provides a comprehensive selection UI for choosing and managing visual assets with licensing tracking.

**Components**:
- **VisualCandidateGallery**: Displays multiple candidates per scene with scoring
- **LicensingInfoPanel**: Summarizes licensing requirements and provides exports
- **VisualSelectionService**: Manages selection state and persistence

**Selection Process**:
1. Generate 3-5 candidates per scene with different providers/prompts
2. Score each candidate on:
   - Aesthetic quality (40% weight)
   - Keyword coverage (40% weight)
   - Technical quality (20% weight)
3. Display candidates in responsive grid with scores
4. User can:
   - **Accept**: Mark candidate as selected
   - **Reject**: Provide reason for rejection (tracked for analytics)
   - **Regenerate**: Generate new candidates
   - **Suggest Better**: Use LLM to refine prompt for better results
5. Track selection metadata:
   - Timestamp and user ID
   - Regeneration count
   - Auto-selection confidence
   - LLM-assisted refinement flag

**Auto-Selection Logic**:
```
Confidence = min(topScore, topScore + (gap × 0.1))
ShouldAutoSelect = confidence >= 85% AND topScore >= 75% AND gap >= 15 points
```

**LLM-Assisted Refinement**:
- Analyzes current candidates and scores
- Suggests improvements to:
  - Subject clarity and framing
  - Composition guidelines
  - Lighting and mood
  - Style keywords
  - Narrative keyword coverage
- Returns refined prompt with confidence score

**Licensing Capture**:
Each selected candidate includes:
- License type (e.g., "Pexels License", "CC0", "Commercial")
- Commercial use allowed (boolean)
- Attribution required (boolean)
- Creator name and profile URL
- Source platform
- License URL
- Generated attribution text

**Export Options**:
1. **CSV Export**: All licensing fields in spreadsheet format
2. **JSON Export**: Structured data with scene and image details
3. **Attribution Text**: Formatted credits for video end screen
4. **Licensing Summary**: Statistics and warnings for review

**Commercial Use Validation**:
- Checks all selections for commercial compatibility
- Identifies scenes requiring attribution
- Warns about missing licensing information
- Provides actionable recommendations

**API Endpoints**:
```
GET  /api/visual-selection/{jobId}/scene/{sceneIndex}
POST /api/visual-selection/{jobId}/scene/{sceneIndex}/accept
POST /api/visual-selection/{jobId}/scene/{sceneIndex}/reject
POST /api/visual-selection/{jobId}/scene/{sceneIndex}/regenerate
POST /api/visual-selection/{jobId}/scene/{sceneIndex}/suggest-refinement
GET  /api/visual-selection/{jobId}/licensing/summary
GET  /api/visual-selection/{jobId}/export/licensing/csv
GET  /api/visual-selection/{jobId}/export/licensing/json
```

**Integration Example**:
```typescript
import { visualSelectionService } from '@/services/visualSelectionService';
import VisualCandidateGallery from '@/components/VisualCandidateGallery';

// In your video creation workflow
const selection = await visualSelectionService.getSelection(jobId, sceneIndex);

<VisualCandidateGallery
  selection={selection}
  onAccept={(candidate) => {
    await visualSelectionService.acceptCandidate(jobId, sceneIndex, candidate);
  }}
  onReject={(reason) => {
    await visualSelectionService.rejectSelection(jobId, sceneIndex, reason);
  }}
  onRegenerate={async () => {
    await visualSelectionService.regenerateCandidates(jobId, sceneIndex);
  }}
  onSuggestRefinement={async () => {
    const refinement = await visualSelectionService.suggestRefinement(
      jobId, sceneIndex, { currentPrompt, currentCandidates, issuesDetected }
    );
    // Apply refined prompt
  }}
/>

// Export licensing before final render
const summary = await visualSelectionService.getLicensingSummary(jobId);
const csvBlob = await visualSelectionService.exportLicensingCsv(jobId);
visualSelectionService.downloadFile(csvBlob, `${jobId}-licensing.csv`);
```

**Best Practices**:
1. Always capture licensing information during generation
2. Validate commercial use before finalizing video
3. Export licensing report with each video delivery
4. Track rejection reasons to improve generation quality
5. Use LLM refinement for scenes with low scores (<60)
6. Set auto-selection threshold based on project requirements
7. Preserve selection history for auditing

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

## Music and Sound Effects Providers

### Overview

Aura Video Studio provides intelligent music and sound effects selection with full licensing tracking. The system supports both stock libraries and optional generative providers.

### Music Providers

#### LocalStock Music Provider

**Location**: `Aura.Providers/Music/LocalStockMusicProvider.cs`

**Description**: Uses pre-downloaded royalty-free music from local library.

**Configuration**:
```json
{
  "Music": {
    "LocalLibraryPath": "C:/ProgramData/Aura/Music"
  }
}
```

**Features**:
- Automatic metadata inference from filenames
- Genre, mood, energy level, and BPM detection
- Supports MP3, WAV, and OGG formats
- Mock library fallback when no files present
- Zero API costs

**File Naming Convention**:
- Include descriptors in filename: `upbeat_corporate_128bpm.mp3`
- Supported keywords: energetic, calm, dramatic, ambient, corporate, cinematic
- BPM inferred from energy level if not in filename

**Advantages**:
- Completely offline
- No API keys required
- Instant search and access
- Perfect for development and testing

**Limitations**:
- Manual library management
- Limited selection without downloaded content
- No automatic content discovery

#### Freesound (SFX)

**Location**: `Aura.Providers/Sfx/FreesoundSfxProvider.cs`

**Description**: Freesound.org API integration for community-sourced sound effects.

**Configuration**:
```json
{
  "Providers": {
    "Sfx": {
      "Freesound": {
        "ApiKey": "your-freesound-api-key"
      }
    }
  }
}
```

**Getting API Key**:
1. Register at https://freesound.org
2. Apply for API key at https://freesound.org/apiv2/apply/
3. Wait for approval (usually instant)

**Features**:
- 500,000+ sound effects
- Tag-based search
- Duration filtering
- License tracking (CC0, CC-BY, CC-BY-NC, etc.)
- HQ preview MP3s
- Commercial use filtering

**Advantages**:
- Massive library of sounds
- High-quality recordings
- Free with API key
- Active community
- Clear licensing information

**Rate Limits**:
- 60 requests per minute (authenticated)
- 2000 requests per day

**Licensing**:
- Multiple Creative Commons licenses
- License info included in metadata
- Attribution text automatically generated
- Commercial use flag per asset

### Audio Intelligence Services

#### Music Recommendation Service

**Location**: `Aura.Core/Services/AudioIntelligence/MusicRecommendationService.cs`

**Features**:
- LLM-assisted genre/BPM/intensity recommendations
- Mood-based search (Happy, Calm, Energetic, Dramatic, etc.)
- Energy level matching (VeryLow to VeryHigh)
- Scene-specific recommendations with emotional arc
- Relevance scoring and ranking

**Usage**:
```csharp
var recommendations = await musicService.RecommendMusicAsync(
    mood: MusicMood.Uplifting,
    preferredGenre: MusicGenre.Corporate,
    energy: EnergyLevel.High,
    duration: TimeSpan.FromMinutes(3),
    context: "product launch video",
    maxResults: 10
);
```

#### Sound Effect Service

**Location**: `Aura.Core/Services/AudioIntelligence/SoundEffectService.cs`

**Features**:
- Script-based SFX suggestions
- Keyword detection (click, reveal, whoosh, impact, etc.)
- Precise timing cues from scene analysis
- Transition effects between scenes
- Type classification (UI, Impact, Ambient, etc.)

**Automatic Detection**:
- Technology/UI: "click", "button", "select"
- Reveals: "unveil", "present", "introduce"
- Motion: "move", "fly", "zoom", "slide"
- Completion: "done", "success", "achieve"
- Action: "hit", "strike", "impact"

**Usage**:
```csharp
var suggestions = await sfxService.SuggestSoundEffectsAsync(
    script: scriptText,
    sceneDurations: sceneDurations,
    contentType: "tutorial"
);
```

#### Audio Normalization Service

**Location**: `Aura.Core/Services/AudioIntelligence/AudioNormalizationService.cs`

**Features**:
- EBU R128 loudness normalization (target LUFS)
- Intelligent ducking with configurable attack/release
- Audio compression for dynamic range control
- Voice EQ with high-pass, presence boost, de-esser
- Multi-track mixing with volume control
- Complete processing pipeline

**Normalization Example**:
```csharp
await normalizationService.NormalizeToLUFSAsync(
    inputPath: "audio.wav",
    outputPath: "normalized.wav",
    targetLUFS: -14.0  // YouTube standard
);
```

**Ducking Example**:
```csharp
var duckingSettings = new DuckingSettings(
    DuckDepthDb: -12.0,
    AttackTime: TimeSpan.FromMilliseconds(100),
    ReleaseTime: TimeSpan.FromMilliseconds(500),
    Threshold: 0.02
);

await normalizationService.ApplyDuckingAsync(
    musicPath: "music.wav",
    narrationPath: "voice.wav",
    outputPath: "ducked.wav",
    settings: duckingSettings
);
```

**LUFS Targets**:
- YouTube: -14 LUFS
- Spotify: -14 LUFS
- Apple Music: -16 LUFS
- Podcasts: -16 to -19 LUFS
- Broadcast TV: -23 to -24 LUFS

#### Licensing Service

**Location**: `Aura.Core/Services/AudioIntelligence/LicensingService.cs`

**Features**:
- Asset usage tracking per job
- Commercial use validation
- Attribution requirement identification
- Multiple export formats (CSV, JSON, HTML, Text)
- License URL collection
- Per-scene asset tracking

**Tracking Usage**:
```csharp
licensingService.TrackAssetUsage(
    jobId: "job-123",
    asset: musicAsset,
    sceneIndex: 0,
    startTime: TimeSpan.Zero,
    duration: TimeSpan.FromSeconds(30),
    isSelected: true
);
```

**Export Formats**:
- **CSV**: Spreadsheet-compatible for record keeping
- **JSON**: Structured data for programmatic access
- **HTML**: Formatted report for video credits
- **Text**: Human-readable licensing summary

**Validation**:
```csharp
var (isValid, issues) = await licensingService.ValidateForCommercialUseAsync(jobId);

if (!isValid)
{
    // Handle licensing restrictions
    foreach (var issue in issues)
    {
        Console.WriteLine(issue);
    }
}
```

### Audio Mixing Service

**Location**: `Aura.Core/Services/AudioIntelligence/AudioMixingService.cs`

**Features**:
- Content-type aware mixing suggestions
- Automatic volume level calculation
- Ducking configuration for narration clarity
- EQ settings for voice clarity
- Compression settings by content type
- Frequency conflict detection

**Content Types**:
- Educational/Tutorial: Voice-forward (narration 100%, music 25%)
- Corporate/Promotional: Balanced (narration 95%, music 40%)
- Gaming/Action: Effects-heavy (narration 90%, music 60%, SFX 70%)
- Music Video: Music-forward (narration 70%, music 90%)

**Usage**:
```csharp
var mixing = await mixingService.GenerateMixingSuggestionsAsync(
    contentType: "educational",
    hasNarration: true,
    hasMusic: true,
    hasSoundEffects: true,
    targetLUFS: -14.0
);
```

### API Endpoints

#### Music Library

```bash
# Search for music
POST /api/music-library/music/search
{
  "mood": "Uplifting",
  "genre": "Corporate",
  "energy": "High",
  "minBPM": 120,
  "maxBPM": 140,
  "commercialUseOnly": true,
  "pageSize": 20
}

# Get specific track
GET /api/music-library/music/{provider}/{assetId}

# Get preview URL
GET /api/music-library/music/{provider}/{assetId}/preview

# Search SFX
POST /api/music-library/sfx/search
{
  "type": "Impact",
  "tags": ["click", "ui"],
  "maxDuration": "00:00:02",
  "commercialUseOnly": true
}

# Find SFX by tags
POST /api/music-library/sfx/find-by-tags
["whoosh", "transition"]

# Get licensing summary
GET /api/music-library/licensing/{jobId}

# Export licensing
POST /api/music-library/licensing/export
{
  "jobId": "job-123",
  "format": "HTML",
  "includeUnused": false
}

# Validate for commercial use
GET /api/music-library/licensing/{jobId}/validate

# List providers
GET /api/music-library/providers/music
GET /api/music-library/providers/sfx
```

### Best Practices for Music and SFX

1. **Always Track Licensing**: Use `LicensingService.TrackAssetUsage()` for every asset
2. **Validate Before Export**: Check commercial use permissions before finalizing
3. **Export Licensing Info**: Include licensing report with every video delivery
4. **Use Appropriate LUFS**: Match target platform standards (-14 for YouTube/Spotify)
5. **Apply Ducking**: Music should duck -10 to -15 dB when narration plays
6. **Test Preview URLs**: Verify previews work before showing to users
7. **Handle Freesound Rate Limits**: Implement exponential backoff
8. **Cache Search Results**: Avoid repeated API calls for same searches
9. **Provide Attribution**: Always include required attributions in video credits
10. **Filter by Commercial Use**: When producing commercial content, filter assets upfront

### Troubleshooting Music and SFX

#### No Music Providers Available

**Solutions**:
- Check LocalStock library path exists
- Verify Freesound API key is configured
- Check provider availability via `/api/music-library/providers/music`
- Review logs for provider initialization errors

#### Freesound API Errors

**Solutions**:
- Verify API key is valid at https://freesound.org/apiv2/
- Check rate limits (60 req/min, 2000 req/day)
- Ensure network connectivity to freesound.org
- Review Freesound API status page

#### Licensing Validation Failures

**Solutions**:
- Review licensing summary to identify non-commercial assets
- Replace restricted assets with commercial-friendly alternatives
- Consider upgrading to premium providers (if available)
- Export licensing report to verify all attributions

#### Audio Normalization Issues

**Solutions**:
- Ensure FFmpeg is installed and in PATH
- Verify input files exist and are valid audio formats
- Check disk space for temporary processing files
- Review FFmpeg logs for specific error messages
- Validate target LUFS is reasonable (-24 to -10)

#### Ducking Not Working

**Solutions**:
- Verify both music and narration files exist
- Check ducking settings (attack/release times)
- Ensure duck depth is negative (-10 to -15 dB typical)
- Test with shorter audio clips first
- Check FFmpeg sidechain compression support

## Additional Resources

- OpenAI Documentation: https://platform.openai.com/docs
- Anthropic Documentation: https://docs.anthropic.com
- Google AI Studio: https://makersuite.google.com
- Ollama Models: https://ollama.ai/library
- ElevenLabs Voice Library: https://elevenlabs.io/voice-library
- Azure TTS Documentation: https://learn.microsoft.com/azure/ai-services/speech-service/
- Piper TTS: https://github.com/rhasspy/piper
- AUTOMATIC1111 WebUI: https://github.com/AUTOMATIC1111/stable-diffusion-webui
- Freesound API Documentation: https://freesound.org/docs/api/
- EBU R128 Loudness Standard: https://tech.ebu.ch/docs/r/r128.pdf
- FFmpeg Audio Filters: https://ffmpeg.org/ffmpeg-filters.html#Audio-Filters

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
