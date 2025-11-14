# Getting Started with Aura Video Studio

Welcome! This guide will help you get up and running with Aura Video Studio.

## 📋 Overview

Aura Video Studio is an AI-powered video creation platform that combines:
- Script generation using AI language models
- Text-to-speech narration synthesis
- Automated visual asset selection
- Professional video composition and rendering

## 🚀 Quick Navigation

1. **[Installation Guide](./INSTALLATION.md)** - Install Aura Video Studio on your system
2. **[Quick Start Guide](./QUICK_START.md)** - Create your first video in minutes
3. **[First Run Guide](./FIRST_RUN_GUIDE.md)** - What happens on first launch
4. **[First Run FAQ](./FIRST_RUN_FAQ.md)** - Common questions from new users

## 📖 Recommended Learning Path

### Step 1: Installation (15-30 minutes)

Start with the [Installation Guide](./INSTALLATION.md) to:
- Download the portable distribution
- Extract and run the application
- Complete the first-run wizard
- Configure basic settings

**System Requirements**:
- Windows 11 (recommended) or Linux
- .NET 8.0 Runtime
- 8GB RAM minimum, 16GB recommended
- 10GB free disk space

### Step 2: First Video (5-10 minutes)

Follow the [Quick Start Guide](./QUICK_START.md) to:
- Launch the application
- Use the Quick Demo button
- Generate a sample video
- Review the output

### Step 3: Understanding the Interface

Read the [First Run Guide](./FIRST_RUN_GUIDE.md) to learn about:
- The setup wizard
- Main application interface
- Core concepts (brief, plan, timeline)
- Provider configuration

### Step 4: Explore Features

Once comfortable with basics, explore:
- [Timeline Editor](../features/TIMELINE.md) - Visual editing
- [TTS & Captions](../features/TTS-and-Captions.md) - Audio and subtitles
- [Video Engines](../features/ENGINES.md) - Generation capabilities
- [CLI Usage](../features/CLI.md) - Command-line automation

## 🎯 Your First Video: Step-by-Step

### Using the Quick Demo

The fastest way to create your first video:

1. **Launch Aura Video Studio**
   ```bash
   # Windows
   .\Aura.App.exe
   
   # Linux (API + Web)
   cd Aura.Api && dotnet run
   # In another terminal:
   cd Aura.Web && npm run dev
   ```

2. **Click "Quick Demo"** button on the home screen

3. **Wait for generation** (approximately 2-3 minutes)

4. **Review output** in the `Renders/` directory

### Creating a Custom Video

For more control over your video:

1. **Enter a brief** describing your video:
   ```
   Title: Introduction to Solar Energy
   Description: A 60-second educational video explaining 
   how solar panels convert sunlight into electricity.
   ```

2. **Generate script** (uses configured LLM provider)

3. **Review and edit** the generated script

4. **Create timeline plan** (automatic scene breakdown)

5. **Synthesize narration** (TTS provider)

6. **Select visuals** (stock images/video or Stable Diffusion)

7. **Render video** (FFmpeg composition)

## 🔧 Configuration Basics

### Provider Setup

Aura supports both **free** and **pro** providers:

**Free Providers** (no API keys needed):
- Rule-based LLM (template-based scripts)
- Windows SAPI TTS (local text-to-speech)
- Stock image/video providers (Pexels, Pixabay, Unsplash)

**Pro Providers** (require API keys):
- OpenAI GPT-4/3.5 (better scripts)
- ElevenLabs TTS (realistic voices)
- Stable Diffusion (custom image generation)

### Getting API Keys

1. **OpenAI**:
   - Sign up at https://platform.openai.com
   - Create API key in dashboard
   - Add to Aura settings

2. **ElevenLabs**:
   - Sign up at https://elevenlabs.io
   - Get API key from profile
   - Add to Aura settings

3. **Stable Diffusion**:
   - Download model locally (NVIDIA GPU required)
   - Or use Stability AI API

See [Provider Configuration](../workflows/SETTINGS_SCHEMA.md) for details.

## ❓ Common Questions

### "Where are my videos saved?"

Generated videos are saved to the `Renders/` directory in your Aura installation folder.

### "Can I use this without API keys?"

Yes! Aura works completely offline with free providers. You'll get:
- Template-based scripts (not AI-generated)
- Windows TTS narration
- Stock footage from free APIs

### "Do I need a GPU?"

Not required, but recommended for:
- Stable Diffusion image generation (NVIDIA GPU with 6GB+ VRAM)
- Faster video rendering

### "Can I edit the generated video?"

Yes! The timeline editor allows you to:
- Rearrange scenes
- Adjust timing
- Change transitions
- Modify audio levels
- Replace assets

### "How long does rendering take?"

Depends on:
- Video length (30s to 5+ minutes)
- Resolution (720p vs 1080p vs 4K)
- System specs (CPU/GPU performance)

Typical times:
- 60-second video at 1080p: 1-3 minutes
- 5-minute video at 1080p: 5-15 minutes

## 🐛 Troubleshooting

If you encounter issues:

1. **Check [Troubleshooting Guide](../troubleshooting/Troubleshooting.md)** for common problems

2. **Review logs**:
   - Application logs: `logs/` directory
   - Render logs: `logs/render/` directory

3. **Search existing issues**: [GitHub Issues](https://github.com/Saiyan9001/aura-video-studio/issues)

4. **Report new issues**: Include:
   - Operating system and version
   - .NET version (`dotnet --version`)
   - Error messages and logs
   - Steps to reproduce

## 📚 Next Steps

Once you're comfortable with the basics:

1. **Advanced Features**:
   - [Batch Processing](../best-practices/README.md) - Generate multiple videos
   - [Custom Templates](../best-practices/README.md) - Reusable video templates
   - [CLI Automation](../features/CLI.md) - Script video generation

2. **Best Practices**:
   - [Quality Guidelines](../best-practices/README.md) - Improve video quality
   - [Performance Optimization](../best-practices/README.md) - Speed up generation
   - [Resource Management](../best-practices/README.md) - Manage system resources

3. **API Integration**:
   - [REST API](../api/README.md) - Integrate with other applications
   - [Provider System](../api/providers.md) - Create custom providers
   - [Webhooks](../api/jobs.md) - Real-time notifications

## 🤝 Getting Help

- **Documentation**: Browse docs/ for guides and references
- **FAQ**: Check [First Run FAQ](./FIRST_RUN_FAQ.md)
- **Issues**: Report bugs on [GitHub](https://github.com/Saiyan9001/aura-video-studio/issues)
- **Discussions**: Ask questions in [Discussions](https://github.com/Saiyan9001/aura-video-studio/discussions)

## 📝 Contributing

Interested in contributing?

- Read [Contributing Guide](../../CONTRIBUTING.md)
- Review [Architecture](../architecture/ARCHITECTURE.md)
- Check [Build Guide](../developer/BUILD_AND_RUN.md)
- Browse existing [Pull Requests](https://github.com/Saiyan9001/aura-video-studio/pulls)

---

**Ready to start?** Head to the [Installation Guide](./INSTALLATION.md)!
