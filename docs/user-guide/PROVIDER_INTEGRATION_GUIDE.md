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

##### Intelligent Scene Matching

When using Pexels, Aura Video Studio can leverage intelligent scene matching to find more contextually relevant images:

**Configuration**:
```json
{
  "Providers": {
    "Images": {
      "Pexels": {
        "ApiKey": "...",
        "Matching": {
          "EnableSemanticMatching": true,
          "MinimumRelevanceScore": 60.0,
          "MaxCandidatesPerScene": 8,
          "UseOrientationFiltering": true,
          "FallbackToBasicSearch": true,
          "MaxKeywordsInQuery": 5
        }
      }
    }
  }
}
```

**Configuration Options**:

| Option | Default | Description |
|--------|---------|-------------|
| `EnableSemanticMatching` | `true` | Enable keyword extraction and intelligent query building |
| `MinimumRelevanceScore` | `60.0` | Minimum score (0-100) for image inclusion |
| `MaxCandidatesPerScene` | `8` | Number of candidates to fetch per scene |
| `UseOrientationFiltering` | `true` | Apply Pexels orientation filter (16:9 → landscape, 9:16 → portrait) |
| `FallbackToBasicSearch` | `true` | Fall back to basic search if semantic search returns no results |
| `MaxKeywordsInQuery` | `5` | Maximum keywords to include in search query |

**How It Works**:

1. **Keyword Extraction**: Analyzes scene heading and narration to extract relevant visual keywords
2. **Stop Word Filtering**: Removes common words (the, a, and, etc.) that don't improve search quality
3. **Visual Term Boosting**: Prioritizes visually-descriptive terms like "landscape", "modern", "technology"
4. **Query Building**: Combines keywords with style and context for optimized Pexels queries
5. **Orientation Filtering**: Maps video aspect ratio to Pexels orientation (16:9 → landscape, 9:16 → portrait)
6. **Relevance Scoring**: Scores each candidate image for keyword matches and context alignment
7. **Threshold Filtering**: Returns only images that meet the minimum relevance score

**Example**:
```
Scene: "Exploring the future of artificial intelligence in healthcare"

Without intelligent matching:
- Search: "artificial intelligence"
- Returns: Generic tech images

With intelligent matching:
- Extracted keywords: healthcare, technology, artificial, intelligence, future
- Built query: "healthcare technology artificial intelligence professional"
- Orientation: landscape (for 16:9 video)
- Returns: Contextually relevant healthcare + technology images
- Scoring: Images with medical/tech elements rank higher
```

**Performance**: Adds ~10-20ms overhead per scene for keyword extraction and scoring.

**Backward Compatibility**: Existing configurations without the `Matching` section continue to work with default values.

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

The image provider fallback chain ensures videos can always be generated, even when no image providers are configured:

```
StableDiffusion → Stability API → Stock Images (Pexels, Unsplash, Pixabay, Local) → Placeholder Visuals
```

**Automatic fallback** on:
- Provider unavailable
- API error
- Quota exceeded
- Validation failure
- No image provider configured

**Graceful Degradation**:
- **Videos always render**, even when no image provider is configured
- When all image providers fail, the pipeline continues with placeholder visuals
- Placeholder visuals can be solid colors, default backgrounds, or bundled fallback images
- The video rendering pipeline never fails due to missing images
- This ensures core functionality (script generation, TTS, FFmpeg rendering) always works

**Example Scenarios**:
- **Free-Only Profile with no image providers**: Video renders with placeholder visuals
- **Temporary API outage**: Falls back to stock images or placeholders
- **Quota exhausted**: Uses local stock or placeholders
- **Network offline**: Uses local providers and placeholders

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

## Render Engine Integration

### Hardware Encoder Selection

Aura's render engine automatically selects the optimal encoder based on available hardware:

#### Encoder Selection Algorithm

1. **Detect Hardware Capabilities**
   - Query FFmpeg for available encoders
   - Check for NVENC (NVIDIA), AMF (AMD), QSV (Intel), VideoToolbox (Apple)
   - Cache results to avoid repeated detection

2. **Select Best Encoder**
   - If hardware acceleration preferred and available: Use hardware encoder
   - If user override specified: Use specified encoder
   - Otherwise: Fall back to software encoding (libx264/libx265)

3. **Configure Encoder Parameters**
   - Quality preset (ultrafast/fast/medium/slow/veryslow)
   - Bitrate and max bitrate
   - Rate control (CRF for software, VBR for hardware)
   - Pixel format

#### Example: Encoder Selection with Override

```csharp
using Aura.Core.Services.Render;

// Let system auto-select encoder
var encoder = await hardwareEncoder.SelectBestEncoderAsync(preset, preferHardware: true);

// Or force specific encoder
var nvencEncoder = await hardwareEncoder.SelectBestEncoderAsync(preset, preferHardware: true);
// Then override in preflight request
var preflightResult = await preflightService.ValidateRenderAsync(
    preset,
    videoDuration,
    outputDirectory,
    encoderOverride: "h264_nvenc",
    preferHardware: true
);
```

### Preset Recommendation Service

The preset recommendation service suggests optimal presets based on project requirements:

#### Rule-Based Recommendation

When LLM provider is unavailable, uses rule-based logic:

```csharp
var request = new PresetRecommendationRequest
{
    TargetPlatform = "YouTube",
    ContentType = "tutorial",
    AspectRatioPreference = "16:9",
    VideoDuration = TimeSpan.FromMinutes(10),
    RequireHighQuality = true
};

var recommendation = await presetService.RecommendPresetAsync(request);

Console.WriteLine($"Recommended: {recommendation.PresetName}");
Console.WriteLine($"Reasoning: {recommendation.Reasoning}");
Console.WriteLine($"Alternatives: {string.Join(", ", recommendation.AlternativePresets)}");
```

#### LLM-Assisted Recommendation

When LLM provider is available, generates contextual recommendations:

```csharp
// LLM will analyze project requirements and recommend best preset
// considering factors like:
// - Platform requirements (duration limits, aspect ratios)
// - Content type (tutorial, vlog, short-form, etc.)
// - Target audience
// - Quality requirements
// - Hardware capabilities

var request = new PresetRecommendationRequest
{
    TargetPlatform = "TikTok",
    ContentType = "comedy sketch",
    ProjectGoal = "viral content",
    Audience = "Gen Z",
    VideoDuration = TimeSpan.FromSeconds(45),
    RequireHighQuality = false  // Prioritize speed over quality
};

var recommendation = await presetService.RecommendPresetAsync(request);
// LLM explains why TikTok preset is optimal for this use case
```

### Render Preflight Validation

Comprehensive validation before starting render:

#### Validation Checks

1. **Disk Space Validation**
   - Output directory: 2.5x estimated file size
   - Temp directory: 1.5x estimated file size
   - Warns if low disk space (< 2 GB)

2. **Write Permission Validation**
   - Tests write access to output directory
   - Tests write access to temp directory
   - Creates directories if they don't exist

3. **Encoder Selection**
   - Selects optimal encoder (hardware vs software)
   - Respects user preferences and overrides
   - Provides fallback encoder

4. **Duration Estimation**
   - Estimates render time based on:
     - Hardware tier (A/B/C/D)
     - Quality preset
     - Hardware acceleration availability
     - Video duration and resolution

#### Example: Full Preflight Check

```csharp
var preflightRequest = new RenderPreflightRequest
{
    PresetName = "YouTube 1080p",
    VideoDuration = TimeSpan.FromMinutes(5),
    OutputDirectory = @"C:\Videos\Output",
    EncoderOverride = null,  // Auto-select
    PreferHardware = true
};

var result = await preflightService.ValidateRenderAsync(
    preset,
    preflightRequest.VideoDuration,
    preflightRequest.OutputDirectory,
    preflightRequest.EncoderOverride,
    preflightRequest.PreferHardware,
    correlationId: Guid.NewGuid().ToString()
);

if (!result.CanProceed)
{
    Console.WriteLine("Preflight failed:");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  ERROR: {error}");
    }
}
else
{
    Console.WriteLine("Preflight passed:");
    Console.WriteLine($"  Encoder: {result.EncoderSelection.EncoderName}");
    Console.WriteLine($"  Hardware Accelerated: {result.EncoderSelection.IsHardwareAccelerated}");
    Console.WriteLine($"  Estimated Duration: {result.Estimates.EstimatedDurationMinutes:F1} minutes");
    Console.WriteLine($"  Estimated File Size: {result.Estimates.EstimatedFileSizeMB:F1} MB");
}
```

### FFmpeg Command Logging

All FFmpeg commands are logged for debugging and support:

#### Log Structure

```json
{
  "jobId": "job-12345",
  "correlationId": "abc-def-ghi",
  "timestamp": "2024-01-15T10:30:00Z",
  "command": "ffmpeg",
  "arguments": [
    "-i", "input.mp4",
    "-c:v", "h264_nvenc",
    "-preset", "medium",
    "-b:v", "8000k",
    "-c:a", "aac",
    "output.mp4"
  ],
  "workingDirectory": "/tmp/aura-render",
  "environment": {
    "PATH": "...",
    "CUDA_VISIBLE_DEVICES": "0"
  },
  "encoder": {
    "name": "h264_nvenc",
    "isHardwareAccelerated": true,
    "description": "NVIDIA NVENC GPU acceleration"
  },
  "exitCode": 0,
  "duration": "00:02:34",
  "success": true,
  "outputPath": "/output/video.mp4"
}
```

#### Retrieving Logs

```csharp
// Get logs for specific job
var logs = await commandLogger.GetCommandsByJobIdAsync("job-12345");

// Get logs by correlation ID (across multiple jobs)
var correlatedLogs = await commandLogger.GetCommandsByCorrelationIdAsync("abc-def-ghi");

// Generate support report
var report = await commandLogger.GenerateSupportReportAsync("job-12345");
Console.WriteLine(report);
```

#### Support Report Example

```
=== FFmpeg Support Report ===
Job ID: job-12345
Generated: 2024-01-15 10:35:00 UTC
Total Commands: 3

--- Command #1 ---
Timestamp: 2024-01-15 10:30:15
Correlation ID: abc-def-ghi
Success: True
Exit Code: 0
Duration: 154.23s
Encoder: h264_nvenc (Hardware)
Working Directory: /tmp/aura-render
Output Path: /output/video.mp4

Command:
  ffmpeg
Arguments:
  -i
  input.mp4
  -c:v
  h264_nvenc
  -preset
  medium
  -b:v
  8000k
  ...
```

### Error Handling with ProblemDetails

Render engine uses ProblemDetails (RFC 7807) for consistent error responses:

#### Example Error Response

```json
{
  "type": "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#render/insufficient-disk-space",
  "title": "Insufficient Disk Space",
  "status": 400,
  "detail": "Insufficient disk space. Required: 2500 MB, Available: 1200 MB in C:\\Videos\\Output",
  "correlationId": "abc-def-ghi",
  "recommendedActions": [
    "Free up disk space by deleting unnecessary files",
    "Choose a different output directory with more space",
    "Reduce video quality or resolution to decrease file size"
  ]
}
```

#### Error Categories

1. **Preflight Failures (400)**
   - Insufficient disk space
   - No write permissions
   - Invalid preset configuration

2. **Encoder Failures (500)**
   - FFmpeg not found
   - Encoder not available
   - Encoding errors

3. **Service Unavailable (503)**
   - Service not configured
   - Dependencies missing

### Integration with VideoOrchestrator

The render engine integrates with VideoOrchestrator for end-to-end video generation:

```csharp
public class VideoOrchestrator
{
    private readonly RenderPreflightService _preflightService;
    private readonly FFmpegCommandLogger _commandLogger;
    
    public async Task<string> RenderVideoAsync(
        RenderSpecification spec,
        IProgress<RenderProgress> progress,
        CancellationToken ct)
    {
        // 1. Run preflight validation
        var preflightResult = await _preflightService.ValidateRenderAsync(
            spec.Preset,
            spec.Duration,
            spec.OutputDirectory,
            spec.EncoderOverride,
            spec.PreferHardware,
            correlationId: spec.CorrelationId,
            ct
        );
        
        if (!preflightResult.CanProceed)
        {
            throw new RenderException(
                "Preflight validation failed",
                preflightResult.Errors
            );
        }
        
        // 2. Prepare FFmpeg command with selected encoder
        var encoder = preflightResult.EncoderSelection;
        var ffmpegArgs = BuildFFmpegCommand(spec, encoder);
        
        // 3. Execute FFmpeg with progress reporting
        var startTime = DateTimeOffset.UtcNow;
        var ffmpegResult = await ExecuteFFmpegAsync(
            ffmpegArgs,
            progress,
            ct
        );
        
        // 4. Log FFmpeg command for support
        await _commandLogger.LogCommandAsync(new FFmpegCommandRecord
        {
            JobId = spec.JobId,
            CorrelationId = spec.CorrelationId,
            Timestamp = startTime,
            Command = "ffmpeg",
            Arguments = ffmpegArgs,
            Encoder = new EncoderInfo
            {
                Name = encoder.EncoderName,
                IsHardwareAccelerated = encoder.IsHardwareAccelerated,
                Description = encoder.Description
            },
            ExitCode = ffmpegResult.ExitCode,
            Duration = DateTimeOffset.UtcNow - startTime,
            Success = ffmpegResult.Success,
            OutputPath = spec.OutputPath
        });
        
        if (!ffmpegResult.Success)
        {
            throw new RenderException(
                "FFmpeg execution failed",
                ffmpegResult.ErrorMessage
            );
        }
        
        return spec.OutputPath;
    }
}
```

### Best Practices

1. **Always Run Preflight**
   - Prevents mid-render failures
   - Provides early error detection with actionable guidance
   - Estimates render time for user expectations

2. **Use Correlation IDs**
   - Track related operations across services
   - Enable end-to-end debugging
   - Link FFmpeg commands to jobs

3. **Handle Encoder Fallbacks**
   - Hardware encoders may fail (driver issues, GPU in use)
   - Always have software fallback configured
   - Log encoder selection for debugging

4. **Monitor Disk Space**
   - Check before render (preflight)
   - Monitor during render (temp files grow)
   - Clean up temp files after render

5. **Log All Commands**
   - Essential for debugging production issues
   - Helps users provide support information
   - Enables command replay for testing

6. **Provide User Guidance**
   - Clear error messages with solutions
   - Recommended actions for common issues
   - Links to documentation

See [BUILD_GUIDE.md](BUILD_GUIDE.md) for hardware acceleration setup and troubleshooting.

## Provider Profile Lock (Sticky Provider Alignment)

Provider Profile Lock ensures your chosen provider remains active throughout the entire pipeline, preventing automatic provider switching unless explicitly requested by the user.

### Overview

**ProfileLock** guarantees provider continuity across:
- Planning
- Script generation
- Refinement
- TTS synthesis
- Visual prompt generation
- Rendering

### Key Features

1. **Session-Level Locks**: Persist across app restarts
2. **Project-Level Locks**: Embedded in project metadata
3. **Offline Mode Enforcement**: Restrict to offline-compatible providers
4. **Manual Fallback Control**: User explicitly triggers provider switches
5. **Extended Wait Patience**: Surface status during slow provider responses

### Enabling ProfileLock

#### Via API

```typescript
import { setProfileLock } from './api/profileLockClient';

// Lock Ollama for entire pipeline
await setProfileLock({
  jobId: 'video-123',
  providerName: 'Ollama',
  providerType: 'local_llm',
  isEnabled: true,
  offlineModeEnabled: true, // Only allow offline providers
  isSessionLevel: true
});
```

#### Via Settings

Navigate to **Settings** → **Providers** → **Profile Lock**:
1. Select primary provider (e.g., Ollama, OpenAI, RuleBased)
2. Enable "Lock provider for entire pipeline"
3. Optionally enable "Offline mode" to restrict network access
4. Choose applicable stages or leave empty for all stages

### Offline Mode

When `offlineModeEnabled: true`, only these providers are allowed:

**LLM Providers:**
- RuleBased (always available, offline)
- Ollama (local AI, requires installation)

**TTS Providers:**
- Windows SAPI (Windows only, always available)
- Piper (local neural TTS, requires installation)
- Mimic3 (local neural TTS, requires installation)

**Image Providers:**
- Stock (curated stock images, bundled)
- LocalSD (Stable Diffusion WebUI, requires NVIDIA GPU)

Attempting to use network-only providers (OpenAI, ElevenLabs, etc.) with offline mode enabled will result in a clear error message.

### Provider Status States

ProfileLock tracks provider status and displays appropriate UI:

| State | Duration | UI Display |
|-------|----------|------------|
| **Active** | 0-30s | Spinner + "Processing..." |
| **Waiting** | 30-180s | Elapsed time + "Still working..." |
| **Extended Wait** | 180s+ | Elapsed time + progress (if available) + "Manual Fallback" button |
| **Stall Suspected** | No heartbeat | Warning + "Provider may be stalled" + "Manual Fallback" button |
| **Error** | Fatal error | Error message + "Manual Fallback" or "Retry" options |

### Manual Fallback

ProfileLock **never** automatically switches providers. User must explicitly choose:

1. **Provider Status Drawer** appears during extended waits
2. Shows elapsed time, heartbeat count, progress (tokens/chunks if available)
3. User clicks **"Switch Provider"** button
4. Confirmation dialog: "Switch from Ollama to OpenAI?"
5. User confirms
6. ProfileLock is unlocked, new provider selected
7. Operation continues with new provider

### API Endpoints

**Get Status:**
```typescript
GET /api/provider-lock/status?jobId=video-123

Response:
{
  "jobId": "video-123",
  "hasActiveLock": true,
  "activeLock": {
    "jobId": "video-123",
    "providerName": "Ollama",
    "providerType": "local_llm",
    "isEnabled": true,
    "offlineModeEnabled": true,
    "applicableStages": [],
    "createdAt": "2024-01-15T10:30:00Z"
  },
  "statistics": {
    "totalSessionLocks": 1,
    "enabledSessionLocks": 1,
    "offlineModeLocksCount": 1
  }
}
```

**Set Lock:**
```typescript
POST /api/provider-lock/set

Request:
{
  "jobId": "video-123",
  "providerName": "Ollama",
  "providerType": "local_llm",
  "isEnabled": true,
  "offlineModeEnabled": true,
  "isSessionLevel": true
}
```

**Unlock:**
```typescript
POST /api/provider-lock/unlock

Request:
{
  "jobId": "video-123",
  "reason": "USER_REQUEST"
}
```

**Check Offline Compatibility:**
```typescript
GET /api/provider-lock/offline-compatible?providerName=OpenAI

Response:
{
  "providerName": "OpenAI",
  "isCompatible": false,
  "message": "Provider OpenAI requires network access...",
  "offlineCompatibleProviders": ["RuleBased", "Ollama", "Windows", "Piper", "Mimic3", "LocalSD", "Stock"]
}
```

### Troubleshooting

#### Issue: Provider seems stuck

**Symptoms:** No progress for 3+ minutes, no heartbeat detected

**Solutions:**
1. Check **Provider Status Drawer** for elapsed time and heartbeat count
2. If heartbeats are updating → Provider is working, be patient
3. If no heartbeats for 2+ minutes → Stall suspected:
   - Click **"Switch Provider"** for manual fallback
   - Or **"Cancel Job"** and retry
4. Check provider logs in Settings → Diagnostics

#### Issue: Offline mode blocks my preferred provider

**Symptoms:** Error: "Provider OpenAI requires network access but offline mode is enforced"

**Solutions:**
1. Disable offline mode in ProfileLock settings
2. Or switch to offline-compatible provider (Ollama, RuleBased)
3. Verify network connectivity if you intended to use cloud providers

#### Issue: ProfileLock prevents automatic fallback

**Symptoms:** Job waiting indefinitely, no automatic switch to fallback provider

**Explanation:** This is intentional! ProfileLock ensures your chosen provider remains active.

**Solutions:**
1. Use **Provider Status Drawer** to monitor progress
2. If provider is slow but working → Be patient, progress will complete
3. If truly stuck → Manually switch via "Switch Provider" button
4. To restore automatic fallback → Disable ProfileLock in settings

#### Issue: How long should I wait?

**General Guidelines:**
- **Script Generation (LLM):**
  - Fast (GPT-4, Claude): 10-30 seconds
  - Medium (Ollama 7B): 30-120 seconds
  - Slow (Ollama 70B, CPU): 2-5 minutes
  
- **TTS Synthesis:**
  - Fast (ElevenLabs, Windows SAPI): 5-15 seconds per minute of audio
  - Medium (Piper): 10-30 seconds per minute
  - Slow (Mimic3): 30-60 seconds per minute
  
- **Image Generation:**
  - Fast (Stock): instant
  - Medium (Local SD, 6GB VRAM): 5-15 seconds per image
  - Slow (Local SD, CPU): 30-120 seconds per image

If elapsed time exceeds 2× expected time and no heartbeats → Consider manual fallback.

### Best Practices

1. **Choose Provider Wisely**: Match provider to your use case and hardware
   - Development/testing: Use free providers (RuleBased, Ollama)
   - Production: Use premium providers (GPT-4, ElevenLabs)
   - Offline: Enable offline mode and choose compatible providers

2. **Enable ProfileLock for Consistency**: Especially important for:
   - Multi-stage pipelines where provider consistency matters
   - Offline environments where network access is restricted
   - Scenarios where automatic fallback would degrade quality

3. **Monitor Provider Status**: Use the Provider Status Drawer during long operations
   - Check heartbeat count to confirm provider is responsive
   - View progress indicators (tokens, chunks, percentage) when available
   - Be patient during legitimate extended waits

4. **Manual Fallback as Last Resort**: Only switch providers when:
   - Stall is confirmed (no heartbeat for 2+ minutes)
   - Provider error is fatal and unrecoverable
   - You explicitly want to try a different provider

5. **Test Offline Mode**: Before relying on offline capabilities:
   - Verify offline providers are installed (Ollama, Piper, etc.)
   - Test full pipeline with `offlineModeEnabled: true`
   - Ensure quality meets your requirements

## Configuration Ownership

### Unified Configuration Model

Aura Video Studio uses a **unified configuration model** to ensure consistency across the stack and avoid configuration drift between frontend, Electron, and backend.

#### Single Source of Truth: Aura.Core ProviderSettings

`Aura.Core.Configuration.ProviderSettings` is the authoritative source for all provider configuration:
- **Provider URLs**: OpenAI endpoint, Ollama URL, Stable Diffusion URL
- **Provider Settings**: Model selections, default voices, regional settings
- **API Keys**: Securely stored, never exposed in GET responses

#### Backend API Surface

`Aura.Api` exposes consistent REST endpoints for provider configuration:

**GET /api/ProviderConfiguration/config**
- Returns current provider configuration (URLs, endpoints, model selections)
- **Never returns API keys or secrets** for security
- Used by frontend to display current settings

**POST /api/ProviderConfiguration/config**
- Updates non-secret provider configuration (URLs, endpoints, models)
- Changes are persisted to disk by ProviderSettings
- Use for updating provider URLs, Ollama models, etc.

**POST /api/ProviderConfiguration/config/secrets**
- Updates provider API keys securely
- Separate endpoint for clear security boundary
- Keys are logged (sanitized) but never returned in responses

#### Frontend Integration

`Aura.Web` reads and writes provider settings **exclusively through backend APIs**:

```typescript
import { 
  getProviderConfiguration, 
  updateProviderConfiguration, 
  updateProviderSecrets 
} from '@/services/api/providerConfigClient';

// Load current configuration
const config = await getProviderConfiguration();
console.log(config.ollama.url); // e.g., "http://127.0.0.1:11434"

// Update provider URL
await updateProviderConfiguration({
  ollama: { url: 'http://192.168.1.100:11434' }
});

// Update API key
await updateProviderSecrets({
  openAiApiKey: 'sk-...'
});
```

**Key principles:**
- Frontend never persists provider URLs in Electron or browser storage
- All configuration reads go through `getProviderConfiguration()`
- All configuration writes go through `updateProviderConfiguration()` or `updateProviderSecrets()`
- No conflicting configuration between frontend and backend

#### Electron Desktop Integration

`Aura.Desktop` handles **secure storage of secrets only** for desktop convenience:

**What Electron Stores:**
- API keys in encrypted `aura-secure` store (OpenAI, Anthropic, Gemini, ElevenLabs, etc.)
- UI preferences and window state in `aura-config` store

**What Electron Does NOT Store:**
- Provider URLs (managed by backend ProviderSettings)
- Provider endpoint configuration (managed by backend)
- Model selections (managed by backend)

**Workflow:**
1. User enters API key in Settings UI
2. Frontend sends key to backend via `POST /api/ProviderConfiguration/config/secrets`
3. Backend persists to ProviderSettings
4. Optionally, Electron stores a copy in secure storage for convenience
5. Backend is always the source of truth for provider readiness checks

#### Benefits of Unified Configuration

**No Configuration Drift:**
- Frontend, Electron, and backend always see the same provider URLs
- No risk of "it works locally but not in production" scenarios

**Simpler Provider Validation:**
- `/api/providers/status` checks use the same configuration the UI is editing
- No need to reconcile multiple sources of truth

**Diagnostics and Troubleshooting:**
- `GET /api/system/diagnostics/ffmpeg-config` - FFmpeg configuration status
- `GET /api/system/diagnostics/providers-config` - Provider configuration snapshot (non-secret)
- Available in all environments for debugging configuration issues
- See [FFMPEG_CONFIGURATION_UNIFIED.md](FFMPEG_CONFIGURATION_UNIFIED.md) and [PROVIDER_CONFIG_UNIFICATION_SUMMARY.md](PROVIDER_CONFIG_UNIFICATION_SUMMARY.md) for details

**Easier to Add New Providers:**
- Add getters/setters to `ProviderSettings`
- Add fields to API DTOs
- Update frontend client
- Single flow, no duplication

**Clear Security Boundaries:**
- Secrets go through dedicated `/config/secrets` endpoint
- Non-secret config goes through `/config` endpoint
- API keys never returned in GET responses

### Configuration Persistence

**Backend Storage:**
- Configuration stored in `{AURA_DATA_PATH}/AuraData/settings.json`
- Reloaded on demand via `ProviderSettings.Reload()`
- Thread-safe updates via `ProviderSettings.UpdateAsync()`

**Electron Secure Storage:**
- API keys in `%APPDATA%/Roaming/aura-video-studio/aura-secure.json` (encrypted)
- Encryption key derived from machine-specific data
- Not synced between machines (intentional security feature)

**Frontend:**
- No persistent provider configuration storage
- All state loaded from backend on startup
- Configuration changes sent immediately to backend

### Migration from Old Model

If you have existing code that:
- Stores provider URLs in Electron config
- Reads provider settings from local storage
- Manages provider state independently

**Action Required:**
1. Remove Electron config writes for provider URLs
2. Update Settings UI to use new `providerConfigClient` methods
3. Remove any `localStorage.setItem()` calls for provider config
4. Ensure all reads go through `getProviderConfiguration()`

### Related Documentation

- [LLM_INTEGRATION_AUDIT.md](LLM_INTEGRATION_AUDIT.md) - LLM provider details
- [TTS_PROVIDER_IMPLEMENTATION_SUMMARY.md](TTS_PROVIDER_IMPLEMENTATION_SUMMARY.md) - TTS provider details
- [LATENCY_PATIENCE_POLICY.md](LATENCY_PATIENCE_POLICY.md) - Provider patience thresholds
- [PROVIDER_STICKINESS_IMPLEMENTATION_SUMMARY.md](PROVIDER_STICKINESS_IMPLEMENTATION_SUMMARY.md) - Technical implementation details
- [PROVIDER_INTEGRATION_IMPLEMENTATION.md](PROVIDER_INTEGRATION_IMPLEMENTATION.md) - Implementation guide

---

## LLM-First Orchestration Services

Aura Video Studio now deeply integrates LLMs across all video generation stages, not just script generation. These services provide AI-powered creative assistance and optimization throughout the pipeline.

### Orchestration Context

**File**: `Aura.Core/Orchestrator/Models/OrchestrationContext.cs`

**Purpose**: Provides comprehensive context to all LLM-driven stages for optimal decision-making.

**Contents**:
- Brief (topic, audience, goal, tone)
- PlanSpec (duration, pacing, density)
- ProviderProfile (which providers are available)
- SystemProfile (hardware capabilities)
- Target platform (YouTube, TikTok, LinkedIn, etc.)
- Language preferences (primary and secondary)
- Budget sensitivity and feature flags

**Usage**:
```csharp
var context = new OrchestrationContext(
    brief: userBrief,
    planSpec: planSpec,
    activeProfile: providerProfile,
    hardware: systemProfile,
    providerSettings: providerSettings)
{
    TargetPlatform = "YouTube",
    PrimaryLanguage = "en-US",
    UseAdvancedVisuals = true,
    BudgetSensitive = false
};

// Pass to any LLM-enhanced service
var suggestions = await visualService.SuggestVisualsAsync(script, context, ct);
```

### Pacing Stage

**File**: `Aura.Core/Orchestrator/Stages/PacingStage.cs`

**Purpose**: LLM-assisted script pacing and scene restructuring for platform-optimized content.

**Features**:
- Normalizes scene lengths to avoid too-short (<3s) or too-long scenes
- Suggests merging brief related scenes
- Recommends splitting overly complex scenes
- Marks "peak attention" moments for viewer engagement
- Optimizes call-to-action placement based on platform
- Adapts to platform-specific attention spans (TikTok: 3-5s, YouTube: 10-30s)

**LLM Mode**: Uses LLM to provide intelligent restructuring recommendations

**Fallback Mode**: Deterministic pacing normalization when LLM unavailable

**Integration**:
```csharp
var pacingStage = new PacingStage(logger, llmProvider);
var refinedScript = await pacingStage.RefineScriptPacingAsync(script, context, ct);
```

### Visual Suggestion Service

**File**: `Aura.Core/Services/StockMedia/VisualSuggestionService.cs`

**Purpose**: LLM-driven recommendations for optimal visual strategy per scene.

**Visual Strategies**:
1. **Stock**: Free or paid stock images (cost-effective, reliable)
2. **Generative**: AI-generated images via Stable Diffusion or DALL-E (unique, customizable)
3. **SolidColor**: Simple colored backgrounds (minimalist, professional)

**Features**:
- Analyzes scene content and context
- Recommends best visual strategy per scene
- Generates optimized prompts for stock search or generative AI
- Provides rationale for each recommendation
- Batch processing for budget-conscious operations

**Output**: `List<VisualSuggestion>` with strategy, queries/prompts, colors, and rationale

**Integration**:
```csharp
var visualService = new VisualSuggestionService(logger, llmProvider);
var suggestions = await visualService.SuggestVisualsAsync(script, context, ct);

foreach (var suggestion in suggestions)
{
    if (suggestion.Strategy == "Generative" && sdProvider != null)
    {
        var image = await sdProvider.GenerateAsync(suggestion.SdPrompt, ct);
    }
    else if (suggestion.Strategy == "Stock")
    {
        var images = await stockProvider.SearchAsync(suggestion.StockQuery, ct);
    }
    // Use suggestion.ColorHex as fallback
}
```

### Thumbnail Prompt Service

**File**: `Aura.Core/Services/Thumbnails/ThumbnailPromptService.cs`

**Purpose**: Generates compelling thumbnail concepts optimized for platform and engagement.

**Features**:
- Analyzes strongest scenes for thumbnail potential
- Generates 3 alternative thumbnail concepts
- Platform-specific guidelines (YouTube: 1280x720, TikTok: vertical, etc.)
- Visual prompts for image generation or stock search
- Text overlay suggestions
- Layout and composition recommendations
- Color palette suggestions

**Output**: `ThumbnailSuggestion` with multiple concepts, layouts, and rationale

**Platform Guidelines**:
- **YouTube**: Bold text, high contrast, faces work well
- **TikTok**: Vertical format, minimal text, action/intrigue
- **LinkedIn**: Professional aesthetic, data visualizations, corporate appropriate
- **Instagram**: Square format, aesthetic appeal, brand colors

**Integration**:
```csharp
var thumbnailService = new ThumbnailPromptService(logger, llmProvider);
var suggestion = await thumbnailService.GenerateThumbnailPromptAsync(script, context, ct);

// Use first concept (or let user choose)
var bestConcept = suggestion.Concepts[0];
Console.WriteLine($"Generate thumbnail: {bestConcept.VisualPrompt}");
Console.WriteLine($"Add text overlay: {bestConcept.TextOverlay}");
Console.WriteLine($"Color palette: {string.Join(", ", bestConcept.ColorPalette)}");
```

### Title and Description Suggestion Service

**File**: `Aura.Core/Services/Metadata/TitleDescriptionSuggestionService.cs`

**Purpose**: SEO-aware metadata generation for video publishing platforms.

**Features**:
- Generates 3-5 title alternatives optimized for clicks and SEO
- Short descriptions for previews (1-2 sentences)
- Long descriptions with full context and keywords
- Platform-specific character limits and guidelines
- Keyword/tag suggestions
- Rationale for each variant

**Platform Optimization**:
- **YouTube**: First 60 chars visible, keywords early, timestamps, CTAs
- **TikTok**: Shorter is better, curiosity hooks, trending hashtags
- **LinkedIn**: Professional tone, business value, avoid clickbait
- **Instagram**: First line hooks viewers, 5-10 hashtags

**Output**: `MetadataSuggestion` with multiple variants and recommendation

**Integration**:
```csharp
var metadataService = new TitleDescriptionSuggestionService(logger, llmProvider);
var suggestion = await metadataService.GenerateMetadataAsync(script, context, ct);

// Present alternatives to user or use recommended variant
foreach (var variant in suggestion.Alternatives)
{
    Console.WriteLine($"Title: {variant.Title}");
    Console.WriteLine($"Description: {variant.ShortDescription}");
    Console.WriteLine($"Keywords: {string.Join(", ", variant.Keywords)}");
    Console.WriteLine($"Why: {variant.Rationale}\n");
}
```

### Language Naturalization Service

**File**: `Aura.Core/Services/Localization/LanguageNaturalizationService.cs`

**Purpose**: LLM-powered translation and cultural adaptation for global audiences.

**Features**:
- Supports **hundreds of languages and dialects** via LLM capabilities
- Not limited to common languages - works with less common languages and regional dialects
- Cultural adaptation (idioms, references, examples)
- Maintains technical accuracy and tone
- Natural, conversational phrasing appropriate for locale
- Batch processing for efficiency

**Supported Locales**: Any language or dialect the LLM can handle, including:
- Standard locales: en-US, es-MX, ja-JP, fr-FR, de-DE, pt-BR, zh-CN, ar-SA, hi-IN, etc.
- Regional dialects: en-AU (Australian), es-AR (Argentine Spanish), fr-CA (Canadian French)
- Less common languages: yi (Yiddish), gd (Scottish Gaelic), cy (Welsh), mi (Maori)
- Historical/specialized variants: Any linguistic variant supported by the LLM

**Output**: `LocalizedScript` with naturalized scenes and application notes

**Integration**:
```csharp
var localizationService = new LanguageNaturalizationService(logger, llmProvider);

// Naturalize to any locale
var localized = await localizationService.NaturalizeScriptAsync(
    script, 
    targetLocale: "es-MX",  // Mexican Spanish
    context, 
    ct);

if (localized.NaturalizationApplied)
{
    // Use localized.Scenes for TTS and rendering
    Console.WriteLine($"Naturalized to {localized.Locale}");
    Console.WriteLine($"Notes: {localized.Notes}");
}
```

**Multi-Language Workflow**:
```csharp
// Generate video in multiple locales
var targetLocales = new[] { "es-MX", "pt-BR", "ja-JP", "de-DE", "hi-IN" };

foreach (var locale in targetLocales)
{
    var localizedScript = await localizationService.NaturalizeScriptAsync(
        originalScript, locale, context, ct);
    
    // Generate video with localized script and locale-specific TTS voice
    var video = await GenerateLocalizedVideoAsync(localizedScript, locale, ct);
}
```

### Fallback Behavior

All LLM-first orchestration services include **deterministic fallbacks** when LLM providers are unavailable:

- **PacingStage**: Applies basic scene length normalization
- **VisualSuggestionService**: Uses keyword extraction for stock queries
- **ThumbnailPromptService**: Generates standard concepts based on topic
- **TitleDescriptionSuggestionService**: Creates descriptive metadata from topic
- **LanguageNaturalizationService**: Returns original script with locale marker

This ensures the pipeline **never fails** due to LLM unavailability, though quality may be reduced.

### Provider Detection

Services automatically detect provider type:
```csharp
if (_llmProvider.GetType().Name.Contains("RuleBased") || 
    _llmProvider.GetType().Name.Contains("Mock"))
{
    // Use deterministic fallback
}
else
{
    // Use LLM-enhanced processing
}
```

### Best Practices

**Context Construction**:
- Always provide complete `OrchestrationContext` for best results
- Set `TargetPlatform` to optimize for specific platforms
- Enable `UseAdvancedVisuals` only if SD/DALL-E providers available
- Set `BudgetSensitive` to true for cost-conscious batch processing

**Batch Processing**:
- Visual and localization services use batch processing to reduce LLM API costs
- Batch sizes adjust based on `BudgetSensitive` flag
- Balance between API costs and processing speed

**Error Handling**:
- All services log warnings and gracefully fall back on errors
- Never throws on LLM failures - returns deterministic alternatives
- Check `NaturalizationApplied` or similar flags to know if LLM was used

**Integration with Existing Services**:
- `VisualSuggestionService` complements `QueryCompositionService`
- Thumbnail prompts can feed into image generation or stock search
- Metadata suggestions integrate with publishing workflows
- Localized scripts work with all TTS providers

### Related Documentation

- [LLM_INTEGRATION_AUDIT.md](LLM_INTEGRATION_AUDIT.md) - LLM provider technical details
- [UNIFIED_LLM_ORCHESTRATOR_GUIDE.md](UNIFIED_LLM_ORCHESTRATOR_GUIDE.md) - LLM orchestration patterns
- [TRANSLATION_USER_GUIDE.md](TRANSLATION_USER_GUIDE.md) - Localization features
- [PROMPT_ENGINEERING_API.md](PROMPT_ENGINEERING_API.md) - Prompt customization
