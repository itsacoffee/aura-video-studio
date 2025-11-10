# Aura Video Studio - Complete User Manual

**Version 1.0 | Last Updated: 2025-11-10**

Welcome to the complete user manual for Aura Video Studio. This guide covers everything you need to create professional videos with AI-powered workflows.

## Table of Contents

1. [Introduction](#introduction)
2. [Getting Started](#getting-started)
3. [Interface Overview](#interface-overview)
4. [Core Features](#core-features)
5. [Advanced Features](#advanced-features)
6. [Provider Configuration](#provider-configuration)
7. [Keyboard Shortcuts](#keyboard-shortcuts)
8. [Tips and Best Practices](#tips-and-best-practices)
9. [Troubleshooting](#troubleshooting)
10. [Glossary](#glossary)

---

## Introduction

### What is Aura Video Studio?

Aura Video Studio is an AI-first video creation platform that combines powerful automation with professional editing tools. Whether you're a beginner creating your first video or a professional producing content at scale, Aura provides the tools you need.

### Key Features

- **AI-Powered Script Generation**: Create compelling narratives automatically
- **Text-to-Speech**: Professional voiceovers with multiple voice options
- **Visual Generation**: AI-generated images or integration with stock libraries
- **Timeline Editor**: Professional editing with drag-and-drop simplicity
- **Multiple Workflow Modes**: Guided Mode for beginners, Advanced Mode for experts
- **Provider Flexibility**: Choose between free, local, or premium AI services

### Who Should Use Aura?

- **Content Creators**: YouTube, TikTok, and social media producers
- **Educators**: Teachers creating instructional videos
- **Marketers**: Quick promotional and explainer videos
- **Storytellers**: Anyone with a story to share

---

## Getting Started

### System Requirements

**Minimum Requirements:**
- Windows 11 (64-bit)
- 8GB RAM
- 10GB free disk space
- Internet connection (for cloud providers)

**Recommended:**
- Windows 11 (64-bit)
- 16GB+ RAM
- NVIDIA GPU with 6GB+ VRAM (for local image generation)
- 50GB free disk space
- Fast internet connection

### Installation

#### Download and Extract
1. Download the latest release from GitHub
2. Extract the ZIP file to your desired location (e.g., `C:\AuraStudio`)
3. Run `Aura.exe`

#### First Launch
On first launch, you'll see the **Onboarding Wizard** which guides you through:

1. **Mode Selection**: Choose your workflow profile
   - **Free-Only**: No API keys, works immediately
   - **Local**: Privacy-focused, runs on your hardware
   - **Pro**: Best quality using cloud services

2. **Hardware Detection**: Automatic detection of your system capabilities

3. **Component Installation**: Install required dependencies
   - **FFmpeg** (Required): Video processing
   - **Ollama** (Optional): Local AI script generation
   - **Stable Diffusion** (Optional): Local image generation

4. **Validation**: Verify everything is working

5. **Ready to Create**: Start your first project

### Quick Start: Your First Video

1. **Launch Aura** and complete the onboarding wizard
2. **Navigate to Create** page from the sidebar
3. **Fill in the form**:
   - **Topic**: "The History of Space Exploration"
   - **Audience**: "General public"
   - **Goal**: "Educate and inspire"
   - **Duration**: 30 seconds
4. **Click Generate** and wait ~30-60 seconds
5. **Review** the generated script, images, and timeline
6. **Export** your video

That's it! You've created your first AI-generated video.

---

## Interface Overview

### Main Navigation

The sidebar provides access to all major sections:

#### 🏠 Welcome
- Dashboard with recent projects
- Quick actions
- System status

#### ✨ Create
- Video generation wizard
- Topic input and configuration
- Generation controls

#### 📊 Dashboard
- Analytics and insights
- Cost tracking (if using paid providers)
- Quality metrics

#### 📝 Editor
- Timeline editor
- Scene inspector
- Audio controls
- Visual effects

#### 🎬 Export
- Render settings
- Export queue
- Job management

#### 📥 Downloads
- Component installation
- Engine management
- Update checker

#### ⚙️ Settings
- Provider configuration
- API key management
- User preferences
- Advanced settings

#### 📚 Templates
- Pre-built video templates
- Style presets
- Reusable components

### Status Bar

Located at the bottom of the screen:
- **Connection Status**: API connection indicator
- **Current Mode**: Guided/Advanced mode toggle
- **Job Status**: Active rendering jobs
- **Provider Status**: Current provider availability

### Command Palette

Press `Ctrl+K` to open the command palette for quick access to any feature.

---

## Core Features

### Video Generation Workflow

#### Step 1: Script Generation

The script is the foundation of your video. Aura generates scripts optimized for your specified:
- **Topic**: What the video is about
- **Audience**: Who will watch it
- **Goal**: What you want viewers to take away
- **Duration**: Target video length

**Available Script Providers:**
- **OpenAI GPT-4**: Best quality, creative narratives
- **Anthropic Claude**: Balanced quality and speed
- **Google Gemini**: Fast, good for educational content
- **Ollama (Local)**: Free, privacy-focused, no API key
- **RuleBased**: Deterministic, template-based (free)

**Script Review and Editing:**
- Review generated script in the Script panel
- Edit text directly inline
- Use AI refinement to improve specific sections
- Add custom sections or remove unwanted parts

#### Step 2: Voice Generation (Text-to-Speech)

Convert your script to natural-sounding speech:

**Voice Selection:**
- Browse available voices by gender, accent, and style
- Preview voices before generation
- Save favorite voices for future projects

**Available TTS Providers:**
- **ElevenLabs**: Premium, highly realistic voices
- **Azure Speech**: Professional Microsoft neural voices
- **Windows TTS**: Built-in, free, good quality
- **Piper TTS**: Free offline, multiple languages
- **Mimic3**: Open-source, privacy-focused

**Voice Customization:**
- **Speed**: Adjust speaking rate (0.5x - 2.0x)
- **Pitch**: Raise or lower voice pitch
- **Emphasis**: Highlight specific words or phrases
- **Pauses**: Add dramatic pauses for effect
- **Emotion**: Some providers support emotional tone

#### Step 3: Visual Generation

Create compelling visuals to accompany your narration:

**Visual Source Options:**

1. **AI Image Generation**
   - **Stable Diffusion (Local)**: Run on your GPU
   - **Stability AI (Cloud)**: High quality, fast
   - **DALL-E 3 (via OpenAI)**: Creative, artistic

2. **Stock Images**
   - **Pexels**: High-quality free photos
   - **Unsplash**: Artistic photography
   - **Pixabay**: Diverse free imagery

3. **Custom Images**
   - Upload your own images
   - Import from local folders
   - Drag and drop support

**Visual Timing:**
- Automatic scene detection based on script
- AI-powered pacing optimization
- Manual timing adjustments
- B-roll insertion

#### Step 4: Timeline Editing

Fine-tune your video in the professional timeline editor:

**Timeline Features:**
- **Multi-track editing**: Video, audio, text layers
- **Drag and drop**: Rearrange scenes easily
- **Trim and split**: Precise timing control
- **Transitions**: Fade, dissolve, wipe effects
- **Audio controls**: Volume, fade in/out
- **Text overlays**: Captions and titles

**Playback Controls:**
- Play/Pause: `Space` or `K`
- Frame-by-frame: `J` (backward) / `L` (forward)
- Jump to start: `Home`
- Jump to end: `End`
- Set in/out points: `I` / `O`

#### Step 5: Export

Render your final video:

**Export Presets:**
- **YouTube 1080p**: 1920x1080, H.264, high quality
- **YouTube 4K**: 3840x2160, H.264, ultra quality
- **Social Media**: 1080x1080, optimized for Instagram/Facebook
- **TikTok/Shorts**: 1080x1920, vertical format
- **Custom**: Define your own settings

**Render Settings:**
- **Format**: MP4, WebM, MOV
- **Codec**: H.264, H.265 (HEVC), VP9
- **Bitrate**: Control file size vs quality
- **Audio**: AAC, MP3, Opus
- **Hardware Acceleration**: GPU encoding when available

**Job Queue:**
- Queue multiple exports
- Monitor progress in real-time
- Pause/resume jobs
- Automatic retry on failure

### Asset Management

#### Asset Library

Organize your creative assets:

**Collections:**
- Create custom collections
- Tag and categorize assets
- Search and filter
- Bulk operations

**Asset Types:**
- Images (PNG, JPG, WebP)
- Videos (MP4, WebM, MOV)
- Audio (MP3, WAV, OGG)
- Fonts (TTF, OTF)

**Import Options:**
- Drag and drop files
- Folder import (preserves structure)
- Cloud integration (coming soon)

#### Brand Kit

Maintain brand consistency:

**Brand Elements:**
- **Colors**: Define brand color palette
- **Fonts**: Upload custom fonts
- **Logos**: Brand logos and watermarks
- **Templates**: Reusable intro/outro sequences

**Apply Brand Kit:**
- One-click branding application
- Automatic color scheme matching
- Consistent typography
- Watermark placement

### Captions and Subtitles

Add accessibility and engagement:

**Caption Generation:**
- Automatic from script
- Speech-to-text sync
- Manual timing adjustment
- Style customization

**Caption Styles:**
- Font family and size
- Color and background
- Position on screen
- Animation effects

**Export Formats:**
- Burned-in (hardcoded into video)
- SRT file (separate subtitle file)
- VTT file (web video format)

---

## Advanced Features

### Advanced Mode

Enable Advanced Mode in Settings for expert features:

**How to Enable:**
1. Go to **Settings → User Interface**
2. Toggle **"Advanced Mode"**
3. Refresh the page

**Unlocked Features:**

#### ML Lab
Train custom frame importance models:
- Annotate frames with importance scores
- Train models on your content style
- Deploy and rollback models
- Performance monitoring

#### Deep Prompt Controls
Fine-tune AI generation:
- Custom system prompts
- Temperature and top-p settings
- Seed control for reproducibility
- Structured output schemas

#### Expert Render Controls
Advanced encoding options:
- Custom FFmpeg flags
- Two-pass encoding
- Advanced color grading
- Audio normalization

#### Motion Graphics
Professional effects:
- Keyframe animation
- Motion tracking
- Chroma keying (green screen)
- Particle effects

#### Provider Tuning
Optimize AI provider behavior:
- Rate limiting configuration
- Fallback chains
- Health monitoring
- Circuit breakers

### Script Refinement Pipeline

Improve generated scripts with AI-powered enhancements:

**Refinement Options:**
- **Structural**: Improve flow and pacing
- **Linguistic**: Enhance language and tone
- **Audience**: Adapt for target demographic
- **Fact-Checking**: Verify claims and statistics
- **SEO**: Optimize for search discoverability

**How to Use:**
1. Generate initial script
2. Click **"Refine"** button
3. Select refinement type
4. Choose framework (HOOK, AIDA, StoryBrand, etc.)
5. Review and apply changes

### Content Safety

Built-in content moderation:

**Safety Features:**
- Profanity detection and filtering
- Sensitive content warnings
- Copyright compliance checking
- Age-appropriate rating

**Safety Policies:**
- Create custom policies
- Define blocked words/phrases
- Set sensitivity thresholds
- Automatic content review

**Incident Log:**
- Track flagged content
- Review moderation decisions
- Generate compliance reports

### Analytics Dashboard

Track performance and costs:

**Metrics Tracked:**
- Video generation time
- Provider usage statistics
- API costs per project
- Quality scores
- Engagement predictions

**Cost Analytics:**
- Real-time cost estimation
- Provider cost comparison
- Budget tracking
- Cost optimization suggestions

**Quality Metrics:**
- Script quality score
- Visual coherence score
- Audio quality rating
- Overall production value

### Pacing Optimization

AI-powered timing improvements:

**Analysis:**
- Detect awkward pauses
- Identify rushed sections
- Measure engagement rhythm
- Scene transition quality

**Suggestions:**
- Add B-roll for slow sections
- Trim redundant content
- Adjust audio pacing
- Optimize scene duration

**Apply Optimizations:**
- One-click apply
- Batch processing
- Manual fine-tuning
- Preview before committing

---

## Provider Configuration

### Understanding Providers

Aura uses different AI services (providers) for different tasks:

- **Script Generation**: LLM providers (GPT-4, Claude, Gemini, Ollama)
- **Voice Generation**: TTS providers (ElevenLabs, Azure, Piper)
- **Image Generation**: Image providers (Stable Diffusion, DALL-E, Stock)

### Provider Profiles

Quick configuration templates:

#### Free-Only Profile
- Script: RuleBased (template-based)
- Voice: Windows TTS
- Visuals: Stock Images (Pexels, Unsplash)
- **Cost**: $0
- **Setup**: None required

#### Balanced Mix Profile
- Script: Ollama (local) or GPT-3.5
- Voice: Piper TTS or Azure
- Visuals: Stable Diffusion or Stock
- **Cost**: Low ($0.01 - $0.50 per video)
- **Setup**: Some API keys or local install

#### Pro-Max Profile
- Script: GPT-4 or Claude Opus
- Voice: ElevenLabs
- Visuals: DALL-E 3 or Stability AI
- **Cost**: Higher ($0.50 - $5.00 per video)
- **Setup**: Premium API keys required

### Adding API Keys

#### Via User Interface

1. Navigate to **Settings → Providers**
2. Find the provider you want to configure
3. Click **"Add API Key"** or **"Configure"**
4. Enter your API key
5. Click **"Test"** to validate
6. Click **"Save"** to encrypt and store

#### Security Notes

- All API keys are encrypted at rest
- Keys are never logged or displayed in full
- Automatic masking in UI (e.g., `sk-12345...wxyz`)
- Stored in platform-specific secure storage:
  - **Windows**: DPAPI encryption
  - **Linux/macOS**: AES-256-CBC encryption

### Provider Health Monitoring

Track provider status in real-time:

**Health Indicators:**
- 🟢 **Healthy**: Provider available and responsive
- 🟡 **Degraded**: Slow response or partial failures
- 🔴 **Unhealthy**: Provider unavailable or failing
- ⚪ **Unknown**: Not yet tested

**Circuit Breaker:**
- Automatic failover to backup providers
- Prevents cascading failures
- Auto-recovery when provider returns

### Local vs Cloud Providers

**Local Providers (Privacy-Focused):**
- Run on your own hardware
- No API keys required
- Complete data privacy
- One-time setup cost
- Free ongoing usage

**Cloud Providers (Convenience):**
- No local installation
- Latest AI models
- Faster processing (usually)
- Pay-per-use costs
- Internet required

---

## Keyboard Shortcuts

### Global Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+K` | Open Command Palette |
| `Ctrl+S` | Save Current Project |
| `Ctrl+Z` | Undo |
| `Ctrl+Shift+Z` or `Ctrl+Y` | Redo |
| `F1` | Open Help |
| `Ctrl+,` | Open Settings |
| `Ctrl+N` | New Project |
| `Ctrl+O` | Open Project |

### Timeline Editor

| Shortcut | Action |
|----------|--------|
| `Space` or `K` | Play/Pause |
| `J` | Play Backward / Decrease Speed |
| `L` | Play Forward / Increase Speed |
| `Home` | Jump to Start |
| `End` | Jump to End |
| `I` | Set In Point |
| `O` | Set Out Point |
| `Left Arrow` | Previous Frame |
| `Right Arrow` | Next Frame |
| `Up Arrow` | Previous Scene |
| `Down Arrow` | Next Scene |
| `Shift+Delete` | Ripple Delete (Remove and Close Gap) |
| `Delete` | Delete Selected |
| `Ctrl+D` | Duplicate Selected |
| `C` | Razor Tool (Cut) |
| `V` | Selection Tool |
| `M` | Add Marker |
| `Ctrl+Plus` | Zoom In Timeline |
| `Ctrl+Minus` | Zoom Out Timeline |

### Script Editor

| Shortcut | Action |
|----------|--------|
| `Ctrl+B` | Bold |
| `Ctrl+I` | Italic |
| `Ctrl+U` | Underline |
| `Ctrl+F` | Find |
| `Ctrl+H` | Find and Replace |
| `Tab` | Indent |
| `Shift+Tab` | Outdent |

### View Controls

| Shortcut | Action |
|----------|--------|
| `Ctrl+1` | Toggle Sidebar |
| `Ctrl+2` | Toggle Properties Panel |
| `Ctrl+3` | Toggle Timeline |
| `F` | Fit to Window |
| `Ctrl+0` | Reset Zoom |

---

## Tips and Best Practices

### Script Writing

**Do:**
- Start with a clear topic and goal
- Keep sentences concise and conversational
- Use active voice
- Include a hook in the first 5 seconds
- End with a clear call-to-action

**Don't:**
- Write long, complex sentences
- Use jargon without explanation
- Include too many ideas in one video
- Forget to consider your audience

### Voice Selection

**Tips:**
- Match voice to content tone (serious, friendly, energetic)
- Preview multiple voices before choosing
- Consider accent and language of target audience
- Use same voice consistently across series

### Visual Generation

**Best Practices:**
- Generate more images than needed (select best)
- Keep visual style consistent
- Avoid text in generated images (add as captions instead)
- Use high-resolution images for better quality

### Timeline Editing

**Efficiency Tips:**
- Learn keyboard shortcuts (save hours)
- Use markers for important points
- Group related clips
- Save custom presets for reuse

### Rendering

**Optimization:**
- Use presets for common platforms
- Enable hardware acceleration if available
- Render overnight for long videos
- Keep source files for re-rendering

### Cost Management

**Save Money:**
- Use Free-Only profile for drafts
- Switch to Pro profile for final version
- Monitor costs in Analytics dashboard
- Set budget alerts

---

## Troubleshooting

### Common Issues

#### "Failed to Generate Script"

**Possible Causes:**
- API key invalid or expired
- Provider service down
- Rate limit exceeded
- Network connection issue

**Solutions:**
1. Check provider status in Settings → Providers
2. Test API key
3. Wait and retry (rate limits reset)
4. Switch to alternative provider

#### "TTS Generation Failed"

**Possible Causes:**
- Voice not available
- Audio service down
- Invalid characters in script
- Credit limit reached

**Solutions:**
1. Try a different voice
2. Check provider status
3. Remove special characters
4. Verify account credits

#### "Render Failed"

**Possible Causes:**
- FFmpeg not installed
- Insufficient disk space
- Invalid output path
- Corrupted media files

**Solutions:**
1. Verify FFmpeg is installed (Downloads page)
2. Check available disk space
3. Change output directory
4. Re-generate media files

#### "White Screen" on Launch

**Possible Causes:**
- Frontend not built
- API server not running
- Browser cache issue

**Solutions:**
1. Hard refresh browser (Ctrl+F5)
2. Clear browser cache
3. Verify API is running at http://localhost:5005
4. Check browser console for errors (F12)

### Performance Issues

#### "Slow Script Generation"

- Use faster providers (GPT-3.5 instead of GPT-4)
- Reduce script length
- Use local providers (Ollama)

#### "Video Export Takes Too Long"

- Use hardware acceleration
- Lower resolution/bitrate
- Use faster encoding preset
- Close other applications

#### "UI Feels Sluggish"

- Close unused tabs
- Reduce timeline zoom level
- Disable real-time previews
- Clear browser cache

### Getting Help

**Resources:**
- **Documentation**: `docs/` folder
- **FAQ**: `docs/user-guide/FAQ.md`
- **Troubleshooting Guide**: `docs/troubleshooting/Troubleshooting.md`
- **GitHub Issues**: Report bugs and feature requests
- **Community**: Join our Discord server

**Diagnostic Bundle:**
Generate a support bundle:
1. Go to Settings → Diagnostics
2. Click "Generate Diagnostic Bundle"
3. Share the ZIP file with support (PII redacted)

---

## Glossary

**Advanced Mode**: Expert interface with additional features and controls

**API Key**: Authentication credential for cloud AI services

**Asset**: Reusable media file (image, video, audio, font)

**B-Roll**: Supplementary footage used to illustrate narration

**Caption**: Text overlay synchronized with audio

**Circuit Breaker**: Automatic failover mechanism for provider failures

**Compositing**: Combining multiple visual layers

**Export Preset**: Pre-configured render settings for specific platforms

**Frame**: Single still image in a video sequence

**Guided Mode**: Beginner-friendly interface with simplified workflows

**Keyframe**: Animation control point

**LLM**: Large Language Model (AI for text generation)

**Marker**: Timeline reference point

**Pacing**: Timing and rhythm of video content

**Provider**: Third-party AI service (OpenAI, ElevenLabs, etc.)

**Provider Profile**: Pre-configured set of providers (Free-Only, Balanced, Pro)

**Render**: Process of creating final video file

**Scene**: Distinct segment of video with specific visuals and audio

**Script**: Written narration text

**SSE**: Server-Sent Events (real-time updates from server)

**Stock Image**: Pre-existing photo from free/paid library

**Timeline**: Visual editor for arranging video segments

**TTS**: Text-to-Speech (converting text to audio)

**Trim**: Adjusting start/end point of a clip

**Voice Profile**: Specific voice characteristics for TTS

**Watermark**: Logo or text overlay for branding

---

## Conclusion

You now have a comprehensive understanding of Aura Video Studio. Start with simple projects using the Free-Only profile, and gradually explore advanced features as you gain confidence.

**Remember:**
- Start simple, add complexity gradually
- Use keyboard shortcuts to speed up workflow
- Save templates for recurring projects
- Monitor costs if using paid providers
- Keep backups of important projects

Happy creating! 🎬

---

**Manual Version**: 1.0  
**Last Updated**: 2025-11-10  
**Feedback**: Open an issue on GitHub or contact support
