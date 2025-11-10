# Frequently Asked Questions (FAQ)

This document answers the most common questions about Aura Video Studio.

## Table of Contents

- [General Questions](#general-questions)
- [Installation and Setup](#installation-and-setup)
- [Providers and API Keys](#providers-and-api-keys)
- [Video Generation](#video-generation)
- [Features and Capabilities](#features-and-capabilities)
- [Performance and Optimization](#performance-and-optimization)
- [Costs and Pricing](#costs-and-pricing)
- [Privacy and Security](#privacy-and-security)
- [Troubleshooting](#troubleshooting)
- [Contributing and Support](#contributing-and-support)

---

## General Questions

### What is Aura Video Studio?

Aura Video Studio is an AI-powered video creation platform that helps you generate professional videos from text. It combines script generation, text-to-speech, image generation, and video editing in one application.

### Is Aura Video Studio free?

Yes! Aura is open-source and free to use. However, some AI providers (like OpenAI, ElevenLabs) charge for API usage. You can use Aura completely free with the "Free-Only" profile which uses no paid services.

### What platforms does Aura support?

Currently, Aura Video Studio targets **Windows 11 (64-bit)** exclusively. While the backend can run on Linux/macOS, the complete application including packaging and distribution requires Windows 11.

### Do I need coding experience to use Aura?

No! Aura is designed for everyone. The Guided Mode provides a simple workflow for beginners, while Advanced Mode offers expert controls for power users.

### Can I use Aura for commercial projects?

Yes, but ensure you comply with the licenses of any AI providers you use. Check each provider's terms of service:
- OpenAI: Commercial use allowed
- ElevenLabs: Depends on your subscription tier
- Stock images: Check individual license (Pexels, Unsplash generally allow commercial use)

### How is Aura different from other video tools?

Aura is unique in offering:
- **AI-first workflow**: End-to-end automation
- **Provider flexibility**: Choose free, local, or premium AI services
- **Privacy options**: Run completely offline with local providers
- **Open source**: Transparent, customizable, community-driven

---

## Installation and Setup

### What are the system requirements?

**Minimum:**
- Windows 11 (64-bit)
- 8GB RAM
- 10GB free disk space
- Internet connection (for cloud providers)

**Recommended:**
- 16GB+ RAM
- NVIDIA GPU with 6GB+ VRAM (for local image generation)
- 50GB free disk space

### Do I need to install anything besides Aura?

Only **FFmpeg** is required (for video rendering). Aura can install this for you automatically through the onboarding wizard.

Other components are optional:
- **Ollama**: For local AI script generation
- **Stable Diffusion**: For local image generation
- **Piper/Mimic3**: For offline text-to-speech

### Can I skip the onboarding wizard?

Yes, click "Skip Setup" during onboarding. You can always re-run it later from the Welcome page by clicking "Run Onboarding."

### How do I re-run the onboarding wizard?

1. Go to the **Welcome** page
2. Click **"Run Onboarding"** button
3. Follow the wizard steps

Your existing settings will not be reset.

### Where are my files stored?

- **Windows**: `%LOCALAPPDATA%\Aura\` (e.g., `C:\Users\YourName\AppData\Local\Aura\`)
  - Projects: `projects\`
  - Settings: `settings\`
  - Encrypted keys: `secure\apikeys.dat`
  - Logs: `logs\`

### Can I move Aura to a different folder?

Yes, Aura is portable. Simply move the entire Aura folder to a new location and run `Aura.exe` from there.

### How do I uninstall Aura?

1. Close Aura if running
2. Delete the Aura folder
3. (Optional) Delete user data at `%LOCALAPPDATA%\Aura\`

---

## Providers and API Keys

### What providers does Aura support?

**Script Generation (LLM):**
- OpenAI (GPT-4, GPT-3.5)
- Anthropic (Claude)
- Google (Gemini)
- Ollama (Local, free)
- RuleBased (Template-based, free)

**Text-to-Speech:**
- ElevenLabs
- Azure Speech
- Windows TTS (built-in, free)
- Piper TTS (local, free)
- Mimic3 (local, free)

**Image Generation:**
- Stable Diffusion (local or cloud)
- DALL-E 3 (via OpenAI)
- Stability AI
- Stock Images (Pexels, Unsplash, Pixabay - free)

### Do I need API keys?

Not necessarily! You can use Aura completely free with:
- Script: RuleBased or Ollama
- Voice: Windows TTS or Piper
- Images: Stock photos

For premium quality, API keys unlock:
- Better script generation (GPT-4, Claude)
- Professional voices (ElevenLabs)
- Custom AI images (DALL-E, Stability AI)

### How do I get API keys?

**OpenAI (GPT-4, DALL-E):**
1. Sign up at https://platform.openai.com
2. Go to API Keys section
3. Create new key
4. Copy and paste into Aura Settings

**ElevenLabs (TTS):**
1. Sign up at https://elevenlabs.io
2. Navigate to Profile → API Keys
3. Generate new key
4. Copy and paste into Aura Settings

**Anthropic (Claude):**
1. Sign up at https://console.anthropic.com
2. Get API key from account settings
3. Add to Aura Settings

### Are my API keys safe?

Yes! Aura takes security seriously:
- All API keys are **encrypted at rest**
- Keys are **never logged** in full (always masked)
- Stored in platform-specific secure storage:
  - **Windows**: DPAPI encryption (CurrentUser scope)
  - **Linux/macOS**: AES-256-CBC with machine-specific key
- Keys are **never sent** to Aura servers (only to the provider you configure)

### Can I use multiple API keys for the same provider?

Currently, Aura supports one API key per provider. Key rotation is on the roadmap.

### What happens if my API key is invalid?

Aura will show an error message and suggest:
1. Re-entering the key
2. Testing the key
3. Switching to a free alternative

You can always switch providers without losing your project.

### Can I share projects with others?

Yes! Export your project as a JSON file (Settings → Export). The recipient can import it, but they'll need their own API keys configured.

---

## Video Generation

### How long does video generation take?

Typical generation times:
- **Script** (30 seconds of content): 5-30 seconds
- **Voice** (30 seconds): 5-15 seconds  
- **Images** (5-10 images): 30-120 seconds
- **Rendering**: 10-60 seconds

**Total for 30-second video:** 1-4 minutes

Actual times vary based on:
- Provider selection (local vs cloud)
- Hardware (GPU, internet speed)
- Complexity of content

### Can I edit the generated script?

Yes! Click on any part of the script to edit it inline. Changes will be reflected when you regenerate voice or re-render.

### Can I use my own images?

Absolutely! You can:
- Upload images via the Asset Library
- Drag and drop into the timeline
- Import entire folders
- Mix AI-generated and custom images

### Can I upload my own voiceover?

Yes! Upload audio files to the Asset Library and add them to the timeline. You can mix generated and custom audio.

### What video formats can I export?

Supported formats:
- MP4 (H.264, H.265)
- WebM (VP8, VP9)
- MOV (QuickTime)

Supported codecs:
- Video: H.264, H.265 (HEVC), VP8, VP9
- Audio: AAC, MP3, Opus

### Can I create videos longer than 1 minute?

Yes! There's no hard limit. However:
- Longer videos take more time to generate
- API costs increase with length
- Consider breaking very long content into chapters

### What video resolutions are supported?

Common presets:
- **1080p**: 1920x1080 (Full HD)
- **4K**: 3840x2160 (Ultra HD)
- **720p**: 1280x720 (HD)
- **Square**: 1080x1080 (Social media)
- **Vertical**: 1080x1920 (TikTok, Shorts)

Custom resolutions are also supported.

### Can I add background music?

Yes! Import audio files and add them to the audio track in the timeline. You can:
- Adjust volume
- Apply fade in/out
- Loop short clips
- Mix multiple audio tracks

### How do I add captions/subtitles?

1. Go to the **Captions** panel
2. Captions are auto-generated from your script
3. Edit timing and text as needed
4. Choose "Burn In" (hardcoded) or export as separate SRT/VTT file

---

## Features and Capabilities

### What is Guided Mode vs Advanced Mode?

**Guided Mode** (Default):
- Simple, streamlined interface
- Sensible defaults
- Inline help and hints
- Best for beginners

**Advanced Mode**:
- Expert controls and settings
- ML Lab for custom training
- Deep prompt customization
- Chroma keying and compositing
- Provider health monitoring

Enable in: Settings → User Interface → Advanced Mode

### What is the ML Lab?

The ML Lab (Advanced Mode only) lets you:
- Annotate frames with importance scores
- Train custom frame importance models
- Deploy models to production
- Rollback to previous versions

Use case: Train Aura to understand your specific content style.

### Can I create video templates?

Yes! Create a project, configure all settings, then:
1. Go to **Templates** page
2. Click **"Save as Template"**
3. Name and describe your template
4. Reuse for future projects

### What keyboard shortcuts are available?

Press `Ctrl+K` to open the command palette, or `F1` for the full keyboard shortcut reference.

Common shortcuts:
- `Space`: Play/Pause
- `Ctrl+S`: Save
- `Ctrl+Z`: Undo
- `Ctrl+K`: Command palette
- `I/O`: Set in/out points

See the User Manual for a complete list.

### Can I collaborate with others?

Project collaboration features are on the roadmap. Currently, you can:
- Export/import projects as JSON
- Share asset libraries
- Use version control for project files

### Does Aura support multiple languages?

Yes! Many providers support multiple languages:
- **Scripts**: Most LLMs support 50+ languages
- **TTS**: Varies by provider (Azure supports 100+ languages)
- **UI**: Currently English only (i18n on roadmap)

### Can I run Aura in headless mode?

Yes! Use the **Aura CLI** for headless operation:

```bash
aura generate --topic "AI in Healthcare" --duration 30 --output video.mp4
```

See `Aura.Cli/README.md` for CLI documentation.

---

## Performance and Optimization

### Why is generation slow?

Common causes:
- **Slow provider**: Switch to faster providers (e.g., GPT-3.5 vs GPT-4)
- **Internet speed**: Local providers eliminate network latency
- **Hardware**: Older CPUs/GPUs take longer
- **High load**: Provider rate limiting during peak times

### How can I speed up video generation?

**Tips:**
1. Use faster providers (GPT-3.5, Azure TTS)
2. Enable hardware acceleration (Settings → Render)
3. Use local providers (Ollama, Piper, Stable Diffusion)
4. Generate images at lower resolution, upscale later
5. Reduce script length

### Why is rendering slow?

**Solutions:**
- Enable GPU encoding (NVENC, QuickSync)
- Use faster encoding preset ("fast" vs "slow")
- Lower resolution/bitrate
- Close other applications
- Upgrade hardware

### Why is the UI laggy?

**Fixes:**
- Close unused browser tabs
- Clear browser cache
- Disable real-time preview
- Reduce timeline zoom level
- Use a modern browser (Chrome, Edge)

### How much disk space do I need?

**Estimate:**
- Aura application: 500MB
- FFmpeg: 200MB
- Ollama + models: 5-10GB
- Stable Diffusion: 10-20GB
- Projects (per video): 50-500MB

**Recommended:** 50GB free space for comfortable use

### How much RAM do I need?

- **Minimum**: 8GB (for basic use)
- **Recommended**: 16GB (for smooth experience)
- **Ideal**: 32GB+ (for local image generation)

---

## Costs and Pricing

### Is Aura free?

Yes, Aura itself is free and open-source. However, some AI providers charge for usage:
- **OpenAI**: Pay-per-token ($0.01-$0.30 per video typical)
- **ElevenLabs**: Subscription or pay-per-character
- **Stability AI**: Pay-per-image

**Free alternatives:**
- Ollama (local LLM)
- Windows TTS or Piper (free voice)
- Stock images (Pexels, Unsplash)

### How much does a video cost to generate?

Cost depends on providers used:

**Free-Only Profile:** $0

**Balanced Mix:**
- Script (GPT-3.5): $0.002
- Voice (Azure): $0.05
- Images (Stock): $0
- **Total**: ~$0.05 per 30-second video

**Pro-Max:**
- Script (GPT-4): $0.10
- Voice (ElevenLabs): $0.15
- Images (DALL-E): $0.40 (5 images)
- **Total**: ~$0.65 per 30-second video

### How can I reduce costs?

**Tips:**
1. Use Free-Only profile for drafts
2. Switch to Pro for final version only
3. Use local providers (Ollama, Stable Diffusion)
4. Generate fewer images, reuse assets
5. Monitor costs in Analytics dashboard
6. Set budget alerts

### Does Aura charge for anything?

No! Aura itself is completely free. All costs are from third-party AI providers you choose to use.

### Can I set a budget limit?

Yes! Go to Settings → Cost Analytics and set:
- Daily budget limit
- Per-project budget
- Alert thresholds

Aura will warn you before exceeding limits (but cannot prevent provider charges).

---

## Privacy and Security

### Does Aura collect my data?

No! Aura is:
- **Self-hosted**: Runs entirely on your machine
- **No telemetry**: No usage tracking or analytics
- **No accounts**: No sign-up or login required
- **Offline capable**: Works without internet (with local providers)

### Where is my data sent?

Data is only sent to AI providers you explicitly configure:
- **Scripts** → Your LLM provider (OpenAI, Anthropic, etc.)
- **Voice** → Your TTS provider (ElevenLabs, Azure, etc.)
- **Images** → Your image provider (Stability AI, etc.)

Local providers (Ollama, Stable Diffusion, Piper) keep everything on your machine.

### Are my API keys secure?

Yes! See [Providers and API Keys](#providers-and-api-keys) section above.

### Can I use Aura completely offline?

Yes, with local providers:
- Install Ollama (script generation)
- Install Stable Diffusion (images)
- Use Windows TTS or Piper (voice)
- Disconnect from internet and create videos!

### What about content safety?

Aura includes built-in content safety features:
- Profanity filtering
- Sensitive content detection
- Copyright compliance checking
- Custom safety policies

Enable in: Settings → Content Safety

### Can I use Aura in a corporate environment?

Yes! Aura is suitable for corporate use:
- No data leaves your network (with local providers)
- API keys encrypted with machine-specific keys
- Compliance-friendly (GDPR, SOC 2, etc.)
- Audit logs for governance

---

## Troubleshooting

### Why am I seeing a white screen?

**Cause:** Frontend not loaded or API server not running.

**Fix:**
1. Hard refresh: `Ctrl+F5`
2. Clear browser cache
3. Verify API is running at http://localhost:5005
4. Check browser console (F12) for errors

### Why can't I generate a script?

**Common causes:**
1. API key missing or invalid
2. Provider service down
3. Rate limit exceeded
4. Network issue

**Solutions:**
- Test API key in Settings → Providers
- Check provider status
- Wait and retry (rate limits reset)
- Switch to alternative provider

### Why does rendering fail?

**Checklist:**
1. Is FFmpeg installed? (Check Downloads page)
2. Enough disk space? (Need 2x video size)
3. Valid output path? (Directory exists)
4. Media files intact? (Not corrupted)

### Why is my video quality poor?

**Improvements:**
1. Use higher bitrate (Settings → Render)
2. Generate images at higher resolution
3. Use premium providers (GPT-4, ElevenLabs)
4. Enable two-pass encoding

### Why aren't my changes saving?

**Possible issues:**
- Auto-save disabled (check Settings)
- Disk full
- File permissions issue
- Browser storage quota

**Fix:** Manually save with `Ctrl+S`

### How do I report a bug?

1. Check if it's a known issue (GitHub Issues)
2. Generate diagnostic bundle (Settings → Diagnostics)
3. Open new issue on GitHub with:
   - Clear description
   - Steps to reproduce
   - Diagnostic bundle
   - Screenshots if applicable

---

## Contributing and Support

### How can I contribute?

We welcome contributions! See `CONTRIBUTING.md` for details:
- Report bugs
- Suggest features
- Submit pull requests
- Improve documentation
- Help other users

### Where can I get help?

**Resources:**
- **Documentation**: `docs/` folder
- **User Manual**: `docs/user-guide/USER_MANUAL.md`
- **Troubleshooting**: `docs/troubleshooting/Troubleshooting.md`
- **GitHub Issues**: Search existing or open new
- **Community**: Discord server (link in README)

### Can I request features?

Yes! Open a feature request on GitHub Issues. Popular requests are prioritized.

### How often is Aura updated?

Aura follows semantic versioning:
- **Patch releases** (bug fixes): As needed
- **Minor releases** (new features): Monthly
- **Major releases** (breaking changes): Yearly

### Can I hire developers for custom work?

While Aura is open-source and free, some contributors may offer consulting. Ask in the community Discord.

### How can I support the project?

- ⭐ Star the repo on GitHub
- 📝 Write tutorials or guides
- 🐛 Report bugs
- 💬 Help answer questions
- 🔀 Contribute code or docs
- ☕ Sponsor maintainers (GitHub Sponsors)

---

## Additional Questions?

If your question isn't answered here:

1. Check the **User Manual**: `docs/user-guide/USER_MANUAL.md`
2. Search **GitHub Issues**: https://github.com/aura-video-studio/aura/issues
3. Ask in **Discord**: (link in README)
4. Open a **new issue**: Tag it with `question`

We're here to help! 🎬

---

**FAQ Version**: 1.0  
**Last Updated**: 2025-11-10  
**Contributions**: Submit improvements via pull request
