# Quick Start Guide: First-Run Onboarding

## For Brand New Users

### Your First Video in 2 Minutes

1. **Launch Aura Video Studio**
   - You'll be automatically redirected to the onboarding wizard

2. **Step 1: Choose Your Mode** (~15 seconds)
   - **Free-Only**: No installation, no API keys, works immediately
     - Uses built-in Windows TTS and free stock images
   - **Local**: Privacy-focused, runs on your hardware
     - Requires NVIDIA GPU with 6GB+ VRAM for images
   - **Pro**: Best quality, uses cloud AI services
     - Requires API keys from OpenAI, ElevenLabs, etc.

   👉 **Recommendation**: Start with Free-Only

3. **Step 2: Hardware Detection** (~10 seconds)
   - Automatic detection of your GPU and VRAM
   - Shows what your system can handle
   - Recommendations based on your hardware

4. **Step 3: Install Components** (~30 seconds)
   - **FFmpeg** (Required): Click "Install" button
   - **Ollama** (Optional): For local AI scripts
   - **Stable Diffusion** (Optional): For local image generation

   👉 **Minimum**: Just install FFmpeg

5. **Step 4: Validation** (~5 seconds)
   - Click "Validate" to check your setup
   - See green checkmarks for ready components
   - Click "Create My First Video" when ready

6. **Create Your Video**
   - You're now on the Create page
   - Fill in your topic, audience, and goal
   - Click "Generate" and wait ~30 seconds
   - Your first video is ready!

**Total Time**: ~1-2 minutes

---

## For Experienced Users with Issues

### When Preflight Checks Fail

If you see red X marks or yellow warnings on the preflight check:

#### Quick Fix Options

**1. One-Click Solutions**

Each failed check shows actionable buttons:

- **"Add API Key"**: Opens Settings → API Keys tab
  - For: OpenAI, ElevenLabs, Stability AI, etc.
  
- **"Install [Component]"**: Opens Downloads page
  - For: Ollama, Piper, Mimic3, SD WebUI
  
- **"Use [Free Alternative]"**: Switches to working free option
  - Example: "Use Windows TTS" instead of ElevenLabs
  - Example: "Use Stock Images" instead of Stable Diffusion
  
- **"Get API Key"**: Opens provider's signup page
  - Direct link to get your API key

**2. Safe Defaults (Nuclear Option)**

Click **"Use Safe Defaults (Free-Only)"** to:
- Switch to Free-Only profile automatically
- Use RuleBased script generation
- Use Windows TTS for audio
- Use Stock images for visuals
- Guaranteed to work, no setup needed

---

## Understanding the Fix Actions

### Icon Guide

- 📥 **Download**: Install a component from Downloads page
- ▶️ **Play**: Start an engine (manual action required)
- ⚙️ **Settings**: Open Settings to configure something
- 🔄 **Switch**: Change to a free alternative
- 🔗 **Link**: Open external help or documentation

### Common Scenarios

#### Scenario 1: Missing API Key
```
❌ OpenAI: API key not configured
💡 Configure your OpenAI API key in Settings

[Add API Key] [Get API Key]
```
**What to do**: Click "Add API Key" → Enter your key → Re-run preflight

---

#### Scenario 2: Engine Not Running
```
❌ Stable Diffusion: Service not running at http://127.0.0.1:7860
💡 Start SD WebUI with --api flag

[Download SD WebUI] [Use Stock Images]
```
**What to do**:
- Option A: Click "Download SD WebUI" → Install → Start manually
- Option B: Click "Use Stock Images" → Instant fallback

---

#### Scenario 3: Hardware Limitation
```
⚠️ Stable Diffusion: GPU has insufficient VRAM (need 6GB+)
💡 Consider using SD 1.5 models or cloud providers

[Use Stock Images]
```
**What to do**: Click "Use Stock Images" → Works immediately on any hardware

---

## Re-Running Onboarding

If you want to run the onboarding wizard again:

1. Go to the Welcome page (home)
2. Click "Run Onboarding" button
3. Follow the wizard steps again

Note: Your previous settings are NOT reset

---

## Troubleshooting

### Onboarding Not Showing?

**Symptom**: App goes straight to Welcome page

**Reason**: You've already seen onboarding once

**Solution**:
1. Go to Welcome page
2. Click "Run Onboarding" button manually

OR

2. Clear localStorage:
   - Open browser DevTools (F12)
   - Go to Application → Local Storage
   - Delete `hasSeenOnboarding` key
   - Refresh page

---

### "Use Safe Defaults" Button Not Showing?

**Symptom**: Preflight fails but no Safe Defaults button

**Reason**: Handler not provided (developer error)

**Solution**:
1. Manually select "Free-Only" from profile dropdown
2. Manually select providers:
   - Script: RuleBased
   - TTS: Windows
   - Visuals: Stock

---

### Fix Actions Do Nothing?

**Symptom**: Clicking fix buttons has no effect

**Reason**: Navigation might be blocked or backend unavailable

**Solution**:
1. Check browser console for errors (F12)
2. Verify backend API is running
3. Try refreshing the page
4. Use manual navigation:
   - Settings: Click Settings in sidebar
   - Downloads: Click Downloads in sidebar

---

## Tips for Success

### First-Time Users
- ✅ Start with Free-Only mode
- ✅ Just install FFmpeg initially
- ✅ Add other components later as needed
- ✅ Try a quick demo before customizing

### Power Users
- ✅ Use Local mode for privacy
- ✅ Install Ollama for better script quality
- ✅ Install Piper/Mimic3 for better voices
- ✅ Use Stable Diffusion for custom visuals

### Professional Users
- ✅ Use Pro mode for best quality
- ✅ Get API keys from all providers
- ✅ Configure rate limits appropriately
- ✅ Monitor API usage and costs

---

## FAQ

**Q: Do I need to install everything?**
A: No! Just FFmpeg is required. Everything else is optional.

**Q: What if I don't have an NVIDIA GPU?**
A: Use Free-Only or Pro mode. Local Stable Diffusion requires NVIDIA GPU.

**Q: Can I change modes later?**
A: Yes! Go to Create page → Select different profile → Run preflight check.

**Q: What if I skip onboarding?**
A: Click "Skip Setup" button. You can re-run it later from Welcome page.

**Q: Are API keys stored securely?**
A: Yes, keys are stored locally on your machine, never sent to Aura servers.

**Q: How do I know if everything is working?**
A: Green checkmarks on all preflight stages mean you're ready to create!

---

## Next Steps

After completing onboarding:

1. **Explore the interface**
   - Dashboard: See recent projects
   - Downloads: Install more components
   - Settings: Customize everything

2. **Create your first video**
   - Start simple with a short topic
   - Use default settings initially
   - Experiment with advanced features later

3. **Learn keyboard shortcuts**
   - Press `Ctrl+K` to see all shortcuts
   - Speed up your workflow

4. **Check documentation**
   - README.md: General overview
   - ONBOARDING_IMPLEMENTATION.md: Technical details
   - BUILD_AND_RUN.md: Development guide

---

**Enjoy creating amazing videos with Aura Video Studio!** 🎬
