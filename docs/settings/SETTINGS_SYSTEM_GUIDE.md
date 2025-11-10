# Settings and Preferences System - User Guide

## Overview

The Aura Video Studio Settings System provides comprehensive control over every aspect of the application, from basic preferences to advanced provider configuration and export customization.

## Table of Contents

1. [Accessing Settings](#accessing-settings)
2. [General Settings](#general-settings)
3. [Export Settings](#export-settings)
4. [Provider Configuration](#provider-configuration)
5. [Performance Settings](#performance-settings)
6. [Import/Export](#importexport)

## Accessing Settings

### Via Menu
1. Click the Settings icon in the navigation menu
2. Or press `Ctrl+,` (Windows/Linux) or `Cmd+,` (Mac)

### Settings Categories
Settings are organized into logical categories displayed as cards:
- **General** - Basic application preferences
- **Provider Configuration** - API keys and provider settings
- **Export Settings** - Video export and watermark configuration
- **Rate Limits** - Cost and usage management
- **Performance** - Hardware acceleration and optimization
- **And more...**

### Search
Use the search bar at the top to quickly find specific settings by name, description, or keywords.

## General Settings

### Theme
- **Light**: Bright interface for daytime use
- **Dark**: Easy on the eyes for low-light environments
- **Auto**: Matches your system theme

### Language & Localization
- Select interface language
- Set locale for date/time formatting
- More languages coming soon!

### Auto-save
- **Enable/Disable**: Turn auto-save on or off
- **Interval**: Set save frequency (30-3600 seconds)
- **Location**: Default project save location

### Startup Behavior
- **Show Dashboard**: Open to main dashboard
- **Show Last Project**: Resume last working project
- **Show New Project Dialog**: Start with new project wizard

## Export Settings

### Default Presets
Choose a default export preset for quick exports:
- YouTube 1080p
- Instagram Story (9:16)
- TikTok
- 4K Master
- And more...

### Watermark Configuration

#### Text Watermark
Add custom text to your videos:

```
Settings:
- Text: "© 2025 Your Brand"
- Font: Arial, 24px
- Color: #FFFFFF (white)
- Shadow: Enabled
- Position: Bottom Right
- Opacity: 70%
```

#### Image Watermark
Overlay your logo or brand image:

```
Settings:
- Image Path: C:\path\to\logo.png
- Position: Bottom Right
- Scale: 10% of video size
- Opacity: 70%
```

#### Position Options
- Top Left, Top Center, Top Right
- Middle Left, Center, Middle Right
- Bottom Left, Bottom Center, Bottom Right

### Output Naming Patterns

Customize how exported files are named using placeholders:

#### Available Placeholders
- `{project}` - Project name
- `{date}` - Export date (customizable format)
- `{time}` - Export time (customizable format)
- `{preset}` - Export preset name
- `{resolution}` - Video resolution (e.g., 1920x1080)
- `{duration}` - Video duration
- `{counter}` - Sequential number

#### Example Patterns

**Pattern**: `{project}_{date}_{time}`  
**Output**: `MyVideo_2025-11-10_143052.mp4`

**Pattern**: `{date}_{project}_v{counter}`  
**Output**: `2025-11-10_MyVideo_v001.mp4`

**Pattern**: `exported_{resolution}_{preset}`  
**Output**: `exported_1920x1080_YouTube1080p.mp4`

#### Naming Options
- **Sanitize Filenames**: Remove special characters
- **Replace Spaces**: Convert spaces to underscores
- **Force Lowercase**: Convert entire filename to lowercase
- **Custom Prefix**: Add text before filename
- **Custom Suffix**: Add text before extension

### Auto-Upload Destinations

Automatically upload exported videos to cloud storage or servers.

#### Supported Destinations
1. **Local Folder** - Copy to another local directory
2. **FTP/SFTP** - Upload to file server
3. **Amazon S3** - AWS cloud storage
4. **Azure Blob Storage** - Microsoft cloud
5. **Google Drive** - Personal cloud storage
6. **Dropbox** - Personal cloud storage

#### Configuration Example: Amazon S3

```
Destination Name: Production S3 Bucket
Type: Amazon S3
Bucket Name: my-video-exports
Region: us-east-1
Access Key: [Your Access Key]
Secret Key: [Your Secret Key]
Delete After Upload: No
Max Retries: 3
Timeout: 300 seconds
```

#### Configuration Example: SFTP Server

```
Destination Name: Company Server
Type: SFTP
Host: files.company.com
Port: 22
Username: video-uploader
Password: [Your Password]
Remote Path: /exports/videos/
Delete After Upload: No
```

### General Export Options

- **Auto-open Output Folder**: Open file location after export
- **Auto-upload on Complete**: Start upload immediately
- **Generate Thumbnail**: Create preview image (JPG)
- **Generate Subtitles**: Export subtitles as SRT file
- **Keep Intermediate Files**: Save temp files for debugging

## Provider Configuration

### API Keys
Securely store API keys for AI services:
- OpenAI (GPT-4, DALL-E)
- Anthropic (Claude)
- Google (Gemini)
- ElevenLabs (Voice)
- Stability AI (Images)

**Security**: All API keys are encrypted before storage.

### Rate Limits & Cost Management

#### Why Rate Limiting?
- Prevent unexpected costs
- Avoid hitting API quotas
- Ensure reliable service
- Automatic fallback to alternatives

#### Global Limits

```
Max Total Requests/Minute: 100
Daily Cost Limit: $50
Monthly Cost Limit: $500
Exceeded Behavior: Queue requests
```

#### Per-Provider Limits

```
OpenAI:
  Requests/Minute: 60
  Requests/Hour: 1000
  Daily Cost Limit: $10
  Monthly Cost Limit: $100
  Fallback Provider: Anthropic
  Priority: 90
```

#### Cost Warnings
Set threshold percentage (default 80%) to receive warnings:
- Email notifications
- In-app alerts
- Prevent overspending

#### Circuit Breaker
Automatically disable failing providers:
- Failure Threshold: 5 consecutive failures
- Timeout: 60 seconds before retry
- Prevents cascading failures

#### Load Balancing Strategies
- **Round Robin**: Distribute evenly
- **Least Cost**: Minimize expenses
- **Least Loaded**: Use least busy provider
- **Lowest Latency**: Fastest response
- **Priority**: Based on configured priority
- **Random**: Random selection

## Performance Settings

### Hardware Acceleration
- **Enable/Disable**: Use GPU for encoding
- **Encoder**: Auto-detect or choose specific (NVENC, AMF, QSV)
- **GPU Selection**: Choose GPU for multi-GPU systems

### Resource Limits
- **RAM Allocation**: Maximum memory for rendering
- **CPU Threads**: Number of threads for encoding
- **Cache Size**: Maximum cache storage (MB)

### Preview Quality
- **Low**: Faster rendering, lower quality
- **Medium**: Balanced performance
- **High**: Better preview, slower
- **Ultra**: Maximum quality preview

### Background Processing
Enable rendering in background while you continue working.

## Import/Export

### Export Settings
Save your entire settings configuration:

1. Go to Settings → Import/Export
2. Click "Export Settings to JSON"
3. Choose whether to include secrets (API keys)
4. Save file to safe location

**Use Cases**:
- Backup before major changes
- Share settings across machines
- Team standardization
- Quick recovery

### Import Settings
Load previously saved settings:

1. Go to Settings → Import/Export
2. Click "Import Settings from JSON"
3. Select settings file
4. Choose merge or overwrite option
5. Confirm import

**Options**:
- **Merge**: Keep existing, add new
- **Overwrite**: Replace everything

## Best Practices

### Security
- Never share API keys in exported settings
- Use separate keys for different environments
- Rotate keys regularly
- Enable cost limits to prevent abuse

### Performance
- Use hardware acceleration when available
- Set appropriate cache size
- Monitor RAM usage
- Use preview quality wisely

### Exports
- Test watermark position on sample first
- Use naming patterns for organization
- Set up multiple upload destinations for backup
- Enable thumbnails for quick identification

### Cost Management
- Start with conservative limits
- Monitor actual usage
- Adjust based on needs
- Set up fallback providers

## Troubleshooting

### Watermark Not Appearing
- Verify watermark is enabled
- Check opacity isn't 0%
- Ensure image path exists (for image watermarks)
- Confirm FFmpeg is installed

### Upload Failing
- Test destination connection
- Verify credentials are correct
- Check network connectivity
- Review timeout settings
- Check destination has space

### Rate Limit Errors
- Review current usage in dashboard
- Increase limits if needed
- Enable fallback providers
- Consider upgrading API plan

### Settings Not Persisting
- Check file permissions in AuraData folder
- Verify disk space available
- Look for errors in logs
- Try resetting to defaults

## Keyboard Shortcuts

Global shortcuts for settings:
- `Ctrl+,` or `Cmd+,` - Open Settings
- `Ctrl+S` - Save Changes
- `Esc` - Close Settings
- `/` or `Ctrl+F` - Focus Search

## FAQ

**Q: Where are settings stored?**  
A: In `AuraData/user-settings.json` relative to the application folder.

**Q: Are API keys encrypted?**  
A: Yes, all API keys are encrypted using DPAPI (Windows) before storage.

**Q: Can I use multiple watermarks?**  
A: Currently one watermark per export. Multiple watermarks coming in future update.

**Q: What happens when rate limit is exceeded?**  
A: Depends on behavior setting:
- **Block**: Request fails immediately
- **Queue**: Request retried later
- **Fallback**: Alternative provider used
- **Warn**: Allowed but notification shown

**Q: Can I reset individual sections?**  
A: Yes, each settings panel has a reset option, or use global "Reset to Defaults".

**Q: How do I share settings with team?**  
A: Export settings to JSON (without secrets), share file, team imports it.

## Support

For additional help:
- Check application logs in `Logs/` folder
- Visit documentation at `/docs`
- Report issues on GitHub
- Contact support team

## Version History

**v1.0.0** - Initial release
- Basic settings structure
- API key management
- Export presets

**v1.1.0** - Enhanced export (Current)
- Watermark configuration
- Output naming patterns
- Auto-upload destinations
- Provider rate limiting
- Cost management
- Circuit breaker
- Load balancing

---

*Last Updated: 2025-11-10*  
*Aura Video Studio - Settings System v1.1.0*
