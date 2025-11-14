# Advanced Mode Guide

## Overview

**Advanced Mode** is a global feature toggle that reveals expert-level features in Aura Video Studio while keeping the default user experience simple and accessible. When disabled (the default state), only essential features are visible. When enabled, power users gain access to advanced controls and customization options.

## When to Use Advanced Mode

Consider enabling Advanced Mode if you:

- Have technical knowledge of video processing and AI systems
- Need fine-grained control over rendering and optimization
- Want to customize AI prompts and behavior at a deep level
- Are comfortable with advanced video editing techniques
- Need access to experimental or specialized features

**Do not enable Advanced Mode if you:**

- Are new to video generation or AI tools
- Prefer guided workflows and presets
- Don't need low-level control over the generation process
- Want the simplest, most streamlined experience

## What's Included in Advanced Mode

When Advanced Mode is enabled, you gain access to:

### 1. ML Lab - Model Retraining Workflow
- In-app on-device machine learning model retraining
- Frame importance scoring and custom training data
- Preflight system capability checks (GPU, RAM, disk space)
- Training progress monitoring with cancellation support
- Model backup and rollback to default
- Complete training history and audit trail
- Time estimation and resource warnings
- Safe model deployment with atomic swaps

### 2. Deep Prompt Customization
- Access to internal prompt templates and few-shot examples
- Chain-of-thought reasoning controls
- Custom system prompts for each AI stage
- Temperature, top-p, and other LLM parameters

### 3. Low-Level Render Flags
- Direct FFmpeg command customization
- Hardware encoder selection and tuning
- Advanced compression settings
- Custom filter chains

### 4. Chroma Key & Compositing
- Green screen and blue screen removal
- Advanced keying controls (tolerance, edge feathering)
- Multi-layer compositing
- Mask and matte generation

### 5. Motion Graphics Recipes
- Procedural motion graphics templates
- Particle systems and effects
- Custom transition definitions
- Animation curve editors

### 6. Expert Provider Tuning
- Provider-specific configuration
- Fallback chain customization
- Timeout and retry policies
- Advanced API settings

## How to Enable Advanced Mode

1. Open **Settings** from the navigation menu
2. Navigate to the **General** tab
3. Scroll down to find the **Advanced Mode** setting
4. Toggle the switch to **Enabled**
5. Click **Save General Settings**

Once enabled, you'll see a warning banner at the top of the Settings page and advanced features will appear throughout the application.

## Warning Banner

When Advanced Mode is active, a prominent warning banner appears at the top of the Settings page:

> **Advanced Mode Active**  
> You have enabled Advanced Mode, which reveals expert features that may require technical knowledge. These include ML retraining controls, deep prompt customization, low-level render flags, chroma key compositing, motion graphics recipes, and expert provider tuning.

This banner includes a **Revert to Simple Mode** button for quick one-click deactivation.

## Reverting to Simple Mode

To disable Advanced Mode:

**Method 1: Quick Revert**
- Click the **Revert to Simple Mode** button in the warning banner

**Method 2: Settings Toggle**
1. Go to **Settings > General**
2. Toggle **Advanced Mode** to **Disabled**
3. Click **Save General Settings**

Disabling Advanced Mode immediately hides all advanced features and returns the interface to the simplified view. Your data and settings are preserved.

## Advanced Features Visibility

The following UI sections are conditionally shown/hidden based on Advanced Mode:

| Feature | Location | Visible When |
|---------|----------|--------------|
| ML Retraining | Navigation, AI Models | Advanced Mode ON |
| Prompt Internals | Prompt Management | Advanced Mode ON |
| Render Flags | Video Defaults, Editor | Advanced Mode ON |
| Chroma Key | Editor, Effects | Advanced Mode ON |
| Motion Graphics Recipes | Templates, Editor | Advanced Mode ON |
| Expert Provider Tuning | Provider Settings | Advanced Mode ON |

## API and Diagnostics

The Advanced Mode state is reflected in the system diagnostics endpoints:

### `/api/diagnostics/json`

Returns:
```json
{
  "advancedMode": true,
  "advancedFeaturesNote": "Advanced features are enabled",
  ...
}
```

or:

```json
{
  "advancedMode": false,
  "advancedFeaturesNote": "Advanced features are disabled. Enable Advanced Mode in Settings > General to access expert features.",
  ...
}
```

This allows external monitoring tools and scripts to detect when Advanced Mode is active.

## Persistence

Advanced Mode state is persisted in your user settings file and synchronized across the application. The setting is stored in:

- **Frontend**: Local storage and backend user settings
- **Backend**: `user-settings.json` in the Aura data directory
- **Scope**: Per-user (not per-project)

The setting persists across application restarts and updates.

## Best Practices

1. **Start Simple**: Begin with Advanced Mode disabled until you're comfortable with basic workflows
2. **Read Documentation**: Familiarize yourself with advanced features before enabling them
3. **Test Safely**: Experiment with advanced settings on non-critical projects first
4. **Revert if Overwhelmed**: Don't hesitate to disable Advanced Mode if the interface becomes confusing
5. **Use Presets**: Even in Advanced Mode, prefer presets and templates over manual configuration when possible

## Troubleshooting

**Q: I enabled Advanced Mode but don't see new features**
- Hard refresh your browser (Ctrl+Shift+R or Cmd+Shift+R)
- Ensure settings were saved successfully
- Check browser console for errors

**Q: Advanced Mode is stuck on/off**
- Clear browser cache and local storage
- Reset settings via Settings > General > Reset to Defaults
- Check that the backend API is responding correctly

**Q: I accidentally broke something in Advanced Mode**
- Click **Revert to Simple Mode** immediately
- Or go to Settings > General and disable Advanced Mode
- If needed, use Settings > Import/Export to restore a previous configuration

## ML Lab - Advanced Training Features

When Advanced Mode is enabled, the ML Lab provides comprehensive tools for retraining the frame importance model with intelligent guidance and safety controls:

### Prerequisites
- **Minimum**: 8GB RAM, 2GB disk space, 20+ annotations
- **Recommended**: 16GB+ RAM, GPU with 4GB+ VRAM, 100+ annotations
- **Time**: 1-60 minutes depending on system and annotation count

### Enhanced Workflow
1. **Annotate Frames**: Rate video frames for importance (0-1 scale)
   - **NEW**: Get intelligent labeling advice based on your current annotations
   - System analyzes rating distribution and suggests which frames to annotate next
   - Focus areas identified automatically (e.g., "Scene transitions", "Static scenes")

2. **Run Preflight Check**: System validates GPU, RAM, disk, estimates time
   - Automatically runs before training starts
   - **Blocks risky training** if minimum requirements not met
   - Provides clear warnings and recommendations

3. **Review Warnings**: Address any critical issues before training
   - Training cannot proceed if critical issues exist
   - System provides actionable recommendations for each issue

4. **Start Training**: Monitor progress, can cancel at any time
   - Real-time progress updates with detailed step information
   - Resource usage monitoring
   - Cancellation available at any time

5. **Review Results**: Check training metrics and AI-powered analysis
   - **NEW**: Automated quality analysis with recommendation (Accept/Caution/Revert)
   - Quality score based on loss, sample count, and training metrics
   - Specific observations, warnings, and concerns highlighted
   - Clear next steps provided

6. **Deploy or Rollback**: Act on recommendation
   - **Accept**: Deploy if analysis recommends it
   - **Accept with Caution**: Test carefully before full deployment
   - **Revert**: Immediately revert if model quality is poor
   - One-click revert to default or restore from backup

### Safety Features
- **Intelligent Preflight Checks**: Validates GPU, RAM, disk space, annotation count
  - Training is **blocked** if system doesn't meet minimum requirements
  - Warnings for suboptimal conditions (insufficient VRAM, low annotation count)
  - Time estimation based on system capabilities
  
- **Labeling Advisor**: Intelligent suggestions for annotation strategy
  - Analyzes rating distribution (low/medium/high importance frames)
  - Identifies imbalances and skewed distributions
  - Recommends specific frame types to annotate next
  - Focus areas to improve model training quality

- **Post-Training Analysis**: AI-powered quality assessment
  - Automated quality scoring based on training metrics
  - Clear recommendation: Accept, Accept with Caution, or Revert
  - Detailed observations about training performance
  - Warnings about potential issues (high loss, low sample count)
  - Specific next steps tailored to your results

- **Atomic Deployment**: New model deployed safely with automatic backup
  - Previous model automatically backed up before deployment
  - Restore from backup if new model underperforms

- **Easy Rollback**: One-click revert options
  - Revert to factory default model
  - Restore from most recent backup

- **Complete Audit Trail**: Full history of all training runs
  - Timestamps, metrics, system info for each training session
  - Training statistics (total runs, success rate, average time)
  - Historical analysis to track improvements

- **Safe Cancellation**: Cancel training at any time
  - Cancellation doesn't corrupt the model
  - Previous model remains intact

### Best Practices
1. **Use Labeling Advisor**: Check `/api/ml/annotations/advice` regularly for guidance
2. **Balance Your Dataset**: Aim for good distribution across low/medium/high importance frames
3. **Start with 100+ Annotations**: More data = better model quality
4. **Run Preflight Check**: System does this automatically, review warnings carefully
5. **Keep Training Runs Under 30 Minutes**: Better user experience and easier to manage
6. **Review Post-Training Analysis**: Always check the automated recommendation before deploying
7. **Test Carefully**: If analysis recommends "Accept with Caution", test on small batches first
8. **Revert if Needed**: Don't hesitate to revert if model performs poorly
9. **Track Your Progress**: Review training history to see improvements over time
10. **Address Warnings**: If preflight or post-training analysis shows warnings, investigate and address them

## Related Documentation

- [ML Training Backend Guide](ML_TRAINING_BACKEND_GUIDE.md)
- [Prompt Customization User Guide](PROMPT_CUSTOMIZATION_USER_GUIDE.md)
- [Provider Integration Guide](PROVIDER_INTEGRATION_GUIDE.md)
- [README](README.md)
- [First Run Wizard Implementation](docs/archive/root-summaries/FIRST_RUN_WIZARD_IMPLEMENTATION.md)

## Support

If you encounter issues with Advanced Mode:

1. Check the application logs in `%LOCALAPPDATA%\Aura\logs`
2. Review diagnostics at `/api/diagnostics/json`
3. Submit a GitHub issue with your configuration and steps to reproduce
4. Disable Advanced Mode temporarily if it blocks your workflow

---

**Last Updated**: 2024-11  
**Version**: 1.0.0
