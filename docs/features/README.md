# Features Documentation

Comprehensive guides for all features of Aura Video Studio.

## 📑 Feature Overview

Aura Video Studio provides a complete video generation pipeline with AI-powered capabilities.

### Core Features

- **[Video Generation Engines](./ENGINES.md)** - Multi-provider video generation system
- **[Stable Diffusion Integration](./ENGINES_SD.md)** - Local and cloud-based image generation
- **[Timeline Editor](./TIMELINE.md)** - Visual timeline editing and manipulation
- **[Text-to-Speech & Captions](./TTS-and-Captions.md)** - Narration and subtitle generation
- **[Local TTS Providers](./TTS_LOCAL.md)** - Offline text-to-speech engines
- **[Command Line Interface](./CLI.md)** - Automation and scripting capabilities

## 🎬 Video Generation Pipeline

The complete video generation process:

### 1. Script Generation

**Purpose**: Create engaging video scripts from simple briefs

**Features**:
- AI-powered script generation (OpenAI GPT, local models)
- Template-based generation (rule-based, no API keys)
- Script editing and refinement
- Scene breakdown and timing

**Providers**:
- `OpenAiLlmProvider` - GPT-4, GPT-3.5-turbo
- `RuleBasedLlmProvider` - Template-based (free, offline)
- Azure OpenAI (compatible)
- Local LLM models (planned)

**Example**:
```
Brief:
  Title: "5 Benefits of Daily Exercise"
  Description: "Educational video about exercise benefits"

Generated Script:
  Scene 1: Introduction (5s)
    "Did you know that just 30 minutes of daily exercise can transform your life?"
  
  Scene 2: Benefit 1 - Heart Health (8s)
    "Regular exercise strengthens your heart and improves circulation..."
  
  [... additional scenes ...]
```

### 2. Timeline Planning

**Purpose**: Break scripts into visual scenes with timing

**Features**:
- Automatic scene detection
- Duration calculation
- Transition planning
- Visual asset requirements

**Timeline Structure**:
```json
{
  "scenes": [
    {
      "id": "scene-1",
      "text": "Introduction narration",
      "duration": 5.0,
      "visualQuery": "person exercising outdoors",
      "transition": "fade"
    }
  ]
}
```

### 3. Narration Synthesis

**Purpose**: Convert script text to speech

**Features**:
- Multiple voice options
- Speed and pitch control
- Emotion and emphasis
- Audio normalization (LUFS)

**TTS Providers**:
- Windows SAPI (free, offline)
- ElevenLabs (realistic voices)
- Azure TTS (multilingual)
- Google Cloud TTS
- Amazon Polly

See [TTS Documentation](./TTS-and-Captions.md) for details.

### 4. Visual Asset Selection

**Purpose**: Find or generate visual content for scenes

**Options**:

**Stock Footage**:
- Pexels (free, no API key)
- Pixabay (free)
- Unsplash (images only)

**AI Generation**:
- Stable Diffusion (local or API)
- DALL-E (OpenAI)
- Midjourney (API)

**Custom Assets**:
- Upload your own images/videos
- Organize in asset library
- Reuse across projects

### 5. Video Composition

**Purpose**: Combine assets, narration, and effects

**Features**:
- Multi-layer composition
- Transitions and effects
- Audio mixing
- Subtitle overlay
- Watermarks and branding

**Technical Details**:
- FFmpeg-based rendering
- Multiple encoder support (H.264, H.265, VP9)
- Resolution: 720p, 1080p, 4K
- Frame rates: 24, 30, 60 fps
- Audio: AAC, MP3, FLAC

See [Rendering Pipeline](./ENGINES.md) for details.

### 6. Rendering and Export

**Purpose**: Generate final video file

**Output Formats**:
- MP4 (H.264) - Most compatible
- WebM (VP9) - Web optimized
- MOV (ProRes) - Professional editing

**Quality Presets**:
- Draft (fast, lower quality)
- Standard (balanced)
- High Quality (slower, better)
- Custom (advanced settings)

## 🎨 Visual Features

### Timeline Editor

Interactive visual timeline for precise control:

- **Scene Management**: Add, remove, reorder scenes
- **Timing Control**: Adjust duration of each scene
- **Transition Editor**: Choose and customize transitions
- **Asset Preview**: Preview images/videos before rendering
- **Audio Waveform**: Visual representation of narration
- **Scrubbing**: Preview timeline at any point

See [Timeline Editor Guide](./TIMELINE.md).

### Asset Library

Organize and manage visual assets:

- **Collections**: Group assets by project or theme
- **Search**: Find assets by keyword or tag
- **Filters**: Filter by type, size, orientation
- **Metadata**: Add descriptions and tags
- **Favorites**: Mark frequently used assets
- **Import/Export**: Backup and share collections

### Brand Kit

Maintain consistent branding:

- **Color Schemes**: Define brand colors
- **Fonts**: Custom font selection
- **Logos**: Watermarks and overlays
- **Templates**: Pre-designed layouts
- **Transitions**: Custom transition styles

## 🔊 Audio Features

### Text-to-Speech

Generate natural-sounding narration:

- **Voice Selection**: Choose from dozens of voices
- **Language Support**: 40+ languages
- **Customization**: Adjust speed, pitch, volume
- **SSML Support**: Fine-tune pronunciation and emphasis
- **Audio Effects**: Reverb, EQ, compression

See [TTS Documentation](./TTS-and-Captions.md).

### Caption Generation

Automatic subtitle creation:

- **Formats**: SRT, VTT, WebVTT
- **Timing**: Auto-sync with narration
- **Styling**: Font, color, position
- **Translation**: Multi-language support
- **Accessibility**: WCAG compliant

### Audio Processing

Professional audio enhancement:

- **Normalization**: LUFS loudness standard
- **Noise Reduction**: Clean up background noise
- **EQ**: Balance frequencies
- **Compression**: Consistent volume
- **Fade In/Out**: Smooth transitions

## 🤖 AI Features

### Intelligent Scene Matching

Automatically find relevant visuals:

- **Semantic Search**: Understand scene context
- **Visual Relevance**: Match visual style
- **Diversity**: Vary shot types and angles
- **Quality Scoring**: Prefer high-quality assets
- **A/B Testing**: Compare different visual choices

### Pacing Optimization

Optimize video timing for engagement:

- **Attention Modeling**: Predict viewer attention
- **Scene Duration**: Optimal length per scene
- **Transition Timing**: When to cut or transition
- **Music Sync**: Beat-matched editing
- **Emotion Curves**: Emotional pacing

### Content Analysis

Understand and improve content:

- **Tone Detection**: Ensure consistent tone
- **Readability**: Age-appropriate language
- **Keyword Extraction**: SEO optimization
- **Fact Checking**: Verify claims (manual)
- **Compliance**: Brand guidelines

## 🛠️ Developer Features

### Command Line Interface

Automate video generation:

```bash
# Generate video from brief
aura generate --title "My Video" --description "Description here"

# Batch process multiple videos
aura batch --input briefs.json --output ./videos/

# Export timeline as JSON
aura export-timeline --project video1 --output timeline.json
```

See [CLI Documentation](./CLI.md).

### REST API

Integrate with other applications:

```http
POST /api/v1/script
Content-Type: application/json

{
  "brief": {
    "title": "My Video",
    "description": "Video description"
  },
  "llmProvider": "openai"
}
```

See [API Reference](../api/README.md).

### Webhooks

Real-time notifications:

- Render completion
- Error notifications
- Progress updates
- Asset processing

## 🎓 Learning Resources

### Tutorials

- [Creating Your First Video](../workflows/QUICK_DEMO.md)
- [Using Custom Assets](../workflows/PORTABLE_MODE_GUIDE.md)
- [Advanced Timeline Editing](./TIMELINE.md)
- [Voice Customization](./TTS_LOCAL.md)

### Best Practices

- [Quality Guidelines](../best-practices/README.md)
- [Performance Optimization](../best-practices/README.md)
- [Provider Selection](../best-practices/README.md)

### API Documentation

- [REST API](../api/README.md)
- [Provider System](../api/providers.md)
- [Error Handling](../api/errors.md)

## 🔍 Feature Comparison

| Feature | Free | Pro | Enterprise |
|---------|------|-----|------------|
| Script Generation | ✅ Template | ✅ AI (GPT) | ✅ Custom Models |
| Text-to-Speech | ✅ Windows SAPI | ✅ ElevenLabs | ✅ Custom Voices |
| Visual Assets | ✅ Stock | ✅ AI Generated | ✅ Enterprise Library |
| Video Quality | ✅ 1080p | ✅ 4K | ✅ 8K |
| Rendering Speed | Standard | ✅ Fast | ✅ GPU Accelerated |
| API Access | ❌ | ✅ Limited | ✅ Unlimited |
| Support | Community | Email | ✅ Priority |

## 📞 Getting Help

- **Feature Requests**: [GitHub Issues](https://github.com/Saiyan9001/aura-video-studio/issues)
- **Bug Reports**: [GitHub Issues](https://github.com/Saiyan9001/aura-video-studio/issues)
- **Questions**: [GitHub Discussions](https://github.com/Saiyan9001/aura-video-studio/discussions)
- **Documentation**: [Getting Started](../getting-started/README.md)

---

**Explore each feature in detail using the links above!**
