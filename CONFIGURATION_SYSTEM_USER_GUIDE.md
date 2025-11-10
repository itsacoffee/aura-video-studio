# Welcome Page Configuration System - User Guide

## Overview

The Welcome Page Configuration System guides you through setting up Aura Video Studio so you can start creating videos immediately. This guide explains what to expect and how to complete the setup process.

---

## Quick Start

### First Time Setup (3-5 minutes)

1. **Launch Aura Video Studio**
   - You'll land on the Welcome page
   - You'll see a prominent "SETUP REQUIRED" banner

2. **Click "Quick Setup - Start Now"**
   - This launches the Configuration Wizard
   - The wizard has 6 simple steps

3. **Follow the Wizard**
   - **Welcome**: Introduction and overview
   - **FFmpeg Check**: Detect or install FFmpeg
   - **FFmpeg Install**: Complete FFmpeg setup
   - **Provider Config**: Set up AI providers
   - **Workspace Setup**: Choose where to save videos
   - **Complete**: Review and finish

4. **Start Creating!**
   - Once setup is complete, you can create videos
   - All features are now unlocked

---

## Understanding the Configuration Status

### Status Checklist

The Welcome page shows a checklist of requirements:

| Status | Meaning | What to Do |
|--------|---------|------------|
| ✅ | Requirement met | Nothing - you're good! |
| ❌ | Requirement not met | Complete this step in setup |
| ⚠️ | Warning/optional | Recommended but not required |

### Requirements Explained

#### 1. Provider Configured ✅/❌
**What it is:** An AI service (like OpenAI, Anthropic) for generating video scripts

**Why it's needed:** Creates the content for your videos automatically

**How to fix:**
- Click "Quick Setup" or "Configure Now"
- Go to "Provider Configuration" step
- Add at least one API key
- Or choose "Offline Mode" for basic functionality

**Options:**
- **OpenAI**: Best quality, ~$0.15 per video
- **Anthropic (Claude)**: Great alternative, ~$0.12 per video
- **Google Gemini**: Free tier available
- **Offline Mode**: Free, rule-based generation

#### 2. API Keys Validated ✅/❌/⚠️
**What it is:** Testing that your API keys actually work

**Why it's needed:** Prevents errors during video generation

**How to fix:**
- Enter your API key
- Click "Validate"
- Wait for confirmation
- If validation fails, check:
  - Key is copied correctly
  - You have credits/billing enabled
  - Key has required permissions

**Warning (⚠️):** Key entered but not validated
- Not critical, but recommended
- Test before creating videos to avoid errors

#### 3. Workspace Created ✅/❌
**What it is:** A folder where your videos and projects are saved

**Why it's needed:** Aura needs somewhere to store your work

**How to fix:**
- In the wizard, go to "Workspace Setup"
- Choose a location with plenty of space
- Recommended: At least 10GB free
- Click "Browse" to select a folder
- System creates necessary subfolders automatically

**Default Locations:**
- **Windows**: `C:\Users\YourName\Aura\workspace`
- **Mac**: `~/Aura/workspace`
- **Linux**: `~/Aura/workspace`

#### 4. FFmpeg Detected ✅/❌
**What it is:** A video processing tool required for rendering

**Why it's needed:** Without FFmpeg, videos cannot be created

**How to fix:**

**Option 1: Automatic Install (Recommended)**
- In wizard, go to "FFmpeg Install"
- Click "Download & Install Managed Build"
- Wait 2-5 minutes for download
- Installation happens automatically

**Option 2: Use Existing FFmpeg**
- If you already have FFmpeg installed
- Click "Browse" and find your FFmpeg executable
- Or enter the path manually
- Click "Validate Path"

**Option 3: Install Manually**
- Download from [ffmpeg.org](https://ffmpeg.org)
- Install using system package manager
- Come back and click "Re-scan"

---

## Setup Wizard Walkthrough

### Step 1: Welcome
- **Duration:** 30 seconds
- **What it does:** Explains the setup process
- **Action:** Click "Next" or "Start Setup Wizard"

### Step 2-3: FFmpeg Installation
- **Duration:** 2-5 minutes (if downloading)
- **What it does:** Ensures FFmpeg is installed and working
- **Options:**
  - Automatic installation (recommended)
  - Manual path entry
  - Skip (not recommended)

**Tips:**
- Automatic install is easiest
- Takes 2-5 minutes depending on internet speed
- You can skip and add later in Settings

### Step 4: Provider Configuration
- **Duration:** 1-2 minutes per provider
- **What it does:** Sets up AI services for script generation
- **Required:** At least one provider OR offline mode

**Provider Options:**

**Free/Trial Options:**
- ✅ **Ollama** (Completely Free)
  - No API key needed
  - Runs locally on your computer
  - Requires download (~4GB)
  
- ✅ **Google Gemini** (Free Tier)
  - Free tier with rate limits
  - Good quality
  - Easy to set up

- ✅ **Offline Mode**
  - No API key needed
  - Rule-based generation
  - Basic functionality

**Paid Options (Better Quality):**
- 💳 **OpenAI** ($5 free credit for new users)
  - Industry-leading quality
  - ~$0.15 per 5-minute video
  
- 💳 **Anthropic Claude** ($5 free credit)
  - Excellent quality
  - ~$0.12 per 5-minute video
  
- 💳 **ElevenLabs** (Voice synthesis)
  - 10,000 characters free/month
  - Best voice quality

**Tips:**
- You can add multiple providers
- System uses fallback if one fails
- You can always add more later in Settings

### Step 5: Workspace Setup
- **Duration:** 1 minute
- **What it does:** Configures where your projects are saved
- **Required:** Yes

**What to Configure:**
1. **Default Save Location**
   - Where finished videos are saved
   - Default: `~/Aura/output`
   
2. **Project Directory** (Optional)
   - Where project files are stored
   - Default: `~/Aura/projects`
   
3. **Temp Directory** (Optional)
   - For temporary render files
   - Default: System temp folder

**Tips:**
- Choose a drive with plenty of space (10GB+ recommended)
- SSD is faster than HDD
- Don't use cloud-synced folders (OneDrive, Dropbox)
- Can change later in Settings

### Step 6: Complete
- **Duration:** 10 seconds
- **What it does:** Saves configuration and marks setup complete
- **Action:** Click "Start Creating Videos"

---

## After Setup

### What Changes?

**Welcome Page:**
- ✅ "System Ready!" banner appears
- ✅ All checklist items show green checkmarks
- ✅ "Create Video" button is now enabled
- ✅ Configuration summary is displayed

**Available Actions:**
- **Create Video**: Now unlocked and ready to use
- **Settings**: Fine-tune your configuration
- **Reconfigure**: Re-run the setup wizard

### Creating Your First Video

1. Click **"Create Video"** button
2. Choose video type:
   - Guided Mode (recommended for beginners)
   - Advanced Mode (for experienced users)
3. Enter your topic or script
4. Customize settings
5. Click "Generate"
6. Wait for rendering (5-10 minutes)
7. Review and export

---

## Reconfiguration

### When to Reconfigure

You might want to reconfigure if:
- Adding a new AI provider
- Changing API keys
- Moving workspace location
- FFmpeg installation changed
- Want to try different settings

### How to Reconfigure

1. Go to Welcome page
2. Click **"Reconfigure"** button
3. Wizard opens with current settings pre-filled
4. Change what you need
5. Click through steps
6. Confirmation saves changes

**Note:** Your existing configuration is automatically backed up before changes.

---

## Troubleshooting

### "Create Video" Button is Disabled

**Possible Causes:**
1. Setup not complete
2. Configuration status not refreshed

**Solutions:**
1. Check the configuration status checklist
2. Any ❌ items need to be completed
3. Click "Refresh" on status card
4. If still disabled, click "Quick Setup"

### FFmpeg Installation Failed

**Symptoms:**
- Download times out
- Installation errors
- "FFmpeg not detected"

**Solutions:**
1. **Check Internet Connection**
   - Installation requires internet
   - Needs 50-100MB download

2. **Try Manual Installation**
   - Download from [ffmpeg.org](https://ffmpeg.org)
   - Install via package manager
   - Use "Browse" to point to executable

3. **Check Disk Space**
   - Need at least 500MB free
   - Clear space and retry

4. **Firewall/Antivirus**
   - May block download
   - Temporarily disable and retry
   - Add Aura to whitelist

### API Key Validation Fails

**Symptoms:**
- "Invalid API key" error
- Validation times out
- Connection errors

**Solutions:**
1. **Check API Key**
   - Copy-paste carefully (no spaces)
   - Check for correct provider
   - Verify key format:
     - OpenAI: Starts with `sk-` or `sk-proj-`
     - Anthropic: Starts with `sk-ant-`
     - Others: Check provider docs

2. **Check Account Status**
   - Do you have credits?
   - Is billing enabled?
   - Are there spending limits?

3. **Check Network**
   - Internet connection working?
   - Firewall blocking API calls?
   - VPN causing issues?

4. **Try Test Button**
   - Click "Test Connection"
   - Check detailed error message
   - Follow specific guidance

### Workspace Creation Failed

**Symptoms:**
- "Invalid directory" error
- "Not writable" error

**Solutions:**
1. **Check Permissions**
   - Do you have write access?
   - Try a folder in your home directory

2. **Check Path**
   - Path should exist or parent should exist
   - No invalid characters
   - Not a system folder

3. **Check Disk Space**
   - At least 10GB recommended
   - More space = more videos

---

## Configuration Export/Import

### Exporting Configuration

**Use Cases:**
- Backup before changes
- Move to another computer
- Share with team

**How to Export:**
1. Go to Settings → Configuration
2. Click "Export Configuration"
3. Choose to include secrets (API keys) or not
4. Save JSON file

**File Format:**
```json
{
  "version": "1.0.0",
  "exportDate": "2025-11-10T12:34:56Z",
  "includesSecrets": false,
  "configuration": { ... }
}
```

### Importing Configuration

**Use Cases:**
- Restore from backup
- Set up new installation
- Apply team configuration

**How to Import:**
1. Go to Settings → Configuration
2. Click "Import Configuration"
3. Select JSON file
4. Review changes
5. Click "Apply"

**Important:**
- API keys may need to be re-entered if not included
- Paths may need adjustment for new system
- Backup is created automatically before import

---

## Best Practices

### Security

1. **Protect API Keys**
   - Don't share configuration exports with secrets
   - Don't commit to version control
   - Use environment variables in team settings

2. **Backup Regularly**
   - Export configuration monthly
   - Store in secure location
   - Include in system backups

### Performance

1. **Workspace Location**
   - Use SSD for best performance
   - Local drive (not network)
   - Not in cloud-synced folder

2. **Disk Space**
   - Keep 20GB+ free for best performance
   - Video files can be large
   - Clean up old projects regularly

3. **Provider Selection**
   - Multiple providers = better reliability
   - Premium providers = better quality
   - Mix free and paid for cost optimization

### Cost Management

1. **Free Options**
   - Start with free providers
   - Use free tiers where available
   - Upgrade when needed

2. **Monitoring Usage**
   - Check provider dashboards
   - Set spending limits
   - Track cost per video

3. **Optimization**
   - Use cheaper providers for drafts
   - Premium providers for final versions
   - Offline mode for testing

---

## Support

### Getting Help

1. **Documentation**
   - Check this guide first
   - See troubleshooting section
   - Read provider-specific docs

2. **Support Channels**
   - GitHub Issues
   - Community Forum
   - Discord Server

3. **Reporting Bugs**
   - Include configuration status screenshot
   - Describe steps to reproduce
   - Share error messages

---

## FAQ

**Q: Do I need to pay for any services?**
A: No! You can use completely free options (Ollama, Gemini free tier, or Offline Mode). Premium providers offer better quality but aren't required.

**Q: How long does setup take?**
A: Typically 3-5 minutes. FFmpeg installation (if needed) takes 2-3 minutes of that.

**Q: Can I skip FFmpeg installation?**
A: You can skip it in the wizard, but you won't be able to create videos until it's installed.

**Q: What happens if I have multiple API keys?**
A: Great! Aura will use the first one, and if it fails, automatically tries the others.

**Q: Can I change my configuration later?**
A: Yes! Click "Reconfigure" on the Welcome page or go to Settings.

**Q: Is my configuration saved automatically?**
A: Yes, all changes are saved immediately and backed up automatically.

**Q: What if I accidentally close the wizard?**
A: Your progress is saved automatically. When you restart, you'll be asked if you want to resume.

**Q: Do I need a GPU?**
A: No, but it helps with performance. Aura detects and uses GPU if available.

**Q: How much disk space do I need?**
A: Minimum 10GB recommended. Each 5-minute video uses ~500MB.

**Q: Can I use my own FFmpeg installation?**
A: Yes! Use the "Browse" option to point to your existing FFmpeg executable.

---

## Glossary

**API Key**: A secret code that lets Aura access AI services

**Provider**: An AI service (OpenAI, Anthropic, etc.) that generates content

**FFmpeg**: Open-source video processing software (required)

**Workspace**: Folder where your projects and videos are saved

**Configuration**: All your settings and preferences

**Status Checklist**: List of requirements with ✅/❌ indicators

**Wizard**: Step-by-step guided setup process

**Offline Mode**: Use Aura without AI providers (limited functionality)

---

**Version:** 1.0.0  
**Last Updated:** 2025-11-10  
**For:** Aura Video Studio Welcome Page Configuration System
