# Aura Video Studio - UX Guide

## Overview

This guide explains the user experience design decisions and best practices for the Aura Video Studio Create Wizard. It covers customization options, progressive disclosure, and accessibility features.

## Design Principles

### 1. Progressive Disclosure
- **Core options first**: Essential settings (topic, audience, tone) appear in Step 1
- **Advanced options later**: Power-user features hidden in collapsible sections
- **Sensible defaults**: All fields have reasonable default values
- **No overwhelming**: Users see only what they need when they need it

### 2. Guided Experience
- **3-step wizard**: Brief → Configuration → Review
- **Clear navigation**: Previous/Next buttons with validation
- **Progress indication**: "Step X of 3" header
- **Helper text**: Hints and tooltips for every control

### 3. Flexibility for All Users
- **Free tier**: Basic features with local providers
- **Pro tier**: Advanced features with cloud APIs
- **Offline mode**: Works without internet connection
- **Keyboard navigation**: Full keyboard support with shortcuts

## Wizard Steps

### Step 1: Brief
Define the core video concept.

**Fields:**
- **Topic** (required): Main subject of the video
- **Audience**: Target demographic (General, Beginners, Advanced, etc.)
- **Tone**: Presentation style (Informative, Casual, Professional, etc.)
- **Aspect Ratio**: Video dimensions (16:9, 9:16, 1:1)

**Validation:**
- Topic is required before proceeding to Step 2
- Other fields have sensible defaults

### Step 2: Configuration
Configure length, pacing, branding, and sources.

#### Length and Pacing
- **Duration**: 0.5 to 20 minutes (default: 3 minutes)
- **Pacing**: Narration speed
  - *Chill*: Relaxed, slower pace (~120 WPM)
  - *Conversational*: Normal speaking pace (~150 WPM)
  - *Fast*: Energetic, faster pace (~180 WPM)
- **Density**: Content amount per minute
  - *Sparse*: Minimal content, more pauses
  - *Balanced*: Moderate content (default)
  - *Dense*: Maximum information density

#### Brand Kit
Add consistent branding to videos.

- **Watermark/Logo**: PNG or SVG file path
  - Recommended: Transparent background, max 200px height
  - Positions: Top/bottom left/right
  - Opacity: 0-100% (default: 70%)
- **Brand Color**: Primary color in hex format (#FF6B35)
  - Used for subtle overlays and highlights
- **Accent Color**: Secondary color in hex format (#00D9FF)
  - Used for text and emphasis

**Best Practices:**
- Use transparent PNGs for watermarks
- Keep watermarks small and unobtrusive
- Choose high-contrast colors for readability

#### Captions
Configure subtitle generation and appearance.

- **Enable/Disable**: Toggle caption generation
- **Format**: 
  - *SRT (SubRip)*: Broad compatibility, simple format
  - *VTT (WebVTT)*: Web-native, supports styling
- **Burn-in**: Permanently embed captions in video
  - Font: Arial, Helvetica, Times New Roman, Courier New
  - Font Size: 12-48px (default: 24px)
  - Colors: Text and outline (hex format)

**When to Burn-in:**
- ✅ For social media (auto-play with sound off)
- ✅ For accessibility requirements
- ❌ If viewers need language options
- ❌ If viewers want to disable captions

#### Stock Sources
Select providers for images and videos.

- **Pexels**: High-quality free stock (recommended)
  - API key increases rate limits
  - Best for: Professional footage
- **Pixabay**: Large free library
  - API key recommended
  - Best for: Variety and volume
- **Unsplash**: Beautiful photography
  - API key required for production
  - Best for: Artistic, curated images
- **Local Assets**: Use your own media
  - Specify directory path
  - Supports: JPG, PNG, MP4, MOV
- **Stable Diffusion**: AI-generated images
  - Requires local SD installation
  - Best for: Custom, unique visuals

**Best Practices:**
- Enable multiple sources for variety
- Add API keys to avoid rate limits
- Use local assets for brand-specific content

#### Offline Mode
Run entirely without cloud services.

**When Enabled:**
- Uses Ollama for script generation (local LLM)
- Uses Windows TTS for narration
- Uses only Pexels/Pixabay/Local for visuals
- No OpenAI, ElevenLabs, or cloud APIs

**When to Use:**
- Privacy-sensitive content
- No internet connection
- Cost savings (no API fees)
- Regulatory compliance

**Limitations:**
- Slower generation (local processing)
- Lower quality TTS
- Fewer visual options

### Step 3: Review and Generate
Review all settings and run preflight checks.

**Profile Selection:**
- **Free-Only**: Ollama + Windows TTS + Stock images
- **Balanced Mix**: Pro services with free fallbacks
- **Pro-Max**: OpenAI + ElevenLabs + Cloud (best quality)

**Preflight Checks:**
- Hardware compatibility
- Required dependencies installed
- API keys configured (if needed)
- Sufficient disk space

**Override Option:**
- Available if checks fail
- Use at your own risk
- Helpful for testing/debugging

## Advanced Settings

Hidden in collapsible section for power users.

### Visual Style
- Standard: Clean, modern look
- Educational: Diagram-heavy, informative
- Cinematic: Dramatic, high production value
- Documentary: Authentic, story-driven
- Minimal: Simple, focused

### Voice Settings (Pro)
- **Rate**: Speech speed (0.5x - 2.0x)
- **Pitch**: Voice pitch adjustment
- **Pause Style**: Natural, Short, Long, Dramatic

### Stable Diffusion Parameters (Pro)
- **Steps**: Quality vs. speed (20-50)
- **CFG Scale**: Prompt adherence (7-15)
- **Seed**: Reproducible generation
- **Dimensions**: Output image size

### Reset to Defaults
- Restores all factory settings
- Confirmation required
- Cannot be undone

## Keyboard Navigation

### Global Shortcuts
- `Tab`: Navigate between fields
- `Shift+Tab`: Navigate backwards
- `Enter`: Submit/Select (in dropdowns)
- `Space`: Toggle checkboxes/switches
- `Ctrl+K`: Show keyboard shortcuts overlay
- `Ctrl+Enter`: Advance to next step

### Field-Specific
- **Sliders**: Arrow keys for fine adjustment
- **Dropdowns**: Arrow keys to navigate options
- **Text inputs**: Standard text editing

## Accessibility Features

### High-Contrast Mode
Automatically detected from OS settings:
- Increased border visibility
- Enhanced focus indicators
- WCAG AA compliant colors

### Screen Reader Support
- All controls labeled properly
- Tooltips read as descriptions
- Progress announcements
- Error messages announced

### Keyboard-Only Operation
- No mouse required
- Visible focus indicators
- Logical tab order
- Skip navigation links

## Persistent Settings

Settings are automatically saved to localStorage:
- Survives page refresh
- Separate from user profile
- Per-browser storage
- Can be cleared manually

**Storage Key:** `aura-wizard-settings`

## Tooltips and Help

Every control has contextual help:
- **Icon**: Info icon (ℹ️) next to label
- **Hover**: Tooltip appears on hover
- **Content**: Brief explanation + doc link
- **Learn More**: Links to detailed docs

## Best Practices for Users

### For Beginners
1. Use default settings for first video
2. Focus on writing a good topic
3. Try "Get AI Recommendations"
4. Use Free-Only profile initially

### For Advanced Users
1. Customize brand kit for consistency
2. Enable multiple stock sources
3. Experiment with pacing/density
4. Use Pro-Max for best quality

### For Privacy-Conscious Users
1. Enable Offline Mode
2. Use local assets only
3. Disable cloud providers
4. Review preflight report carefully

## Common Workflows

### Quick Social Media Video
1. Brief: Topic + Vertical 9:16 aspect
2. Config: Short duration (1-2 min), Fast pacing, Enable captions with burn-in
3. Review: Free-Only profile, Generate

### Professional Marketing Video
1. Brief: Topic + Professional tone + Widescreen 16:9
2. Config: Brand kit with logo/colors, Captions without burn-in
3. Review: Pro-Max profile, Generate

### Educational Tutorial
1. Brief: Topic + Beginners audience + Informative tone
2. Config: Longer duration (5-10 min), Conversational pacing, Dense content
3. Review: Balanced Mix profile, Generate

## Troubleshooting

### Settings Not Saving
- Check localStorage quota
- Clear browser cache
- Check console for errors

### Tooltips Not Showing
- Ensure JavaScript enabled
- Try different browser
- Check accessibility settings

### Keyboard Shortcuts Not Working
- Check OS keyboard settings
- Close conflicting browser extensions
- Verify focus is in wizard

## Related Documentation

- [TTS and Captions Guide](../features/TTS-and-Captions.md)
- [Local Providers Setup](../user-guide/LOCAL_PROVIDERS_SETUP.md)
- [Troubleshooting](../troubleshooting/Troubleshooting.md)
- [Build and Run Guide](../developer/BUILD_AND_RUN.md)

## Support

For issues or questions:
1. Check this guide first
2. Review troubleshooting docs
3. Check GitHub issues
4. Open new issue with details

---

*Last updated: 2025-10-10*
