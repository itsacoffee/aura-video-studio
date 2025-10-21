# User Profile System Guide

## Overview

The User Profile System allows users to create multiple style profiles with distinct preferences for content creation. Each profile can have different tone settings, visual styles, audio preferences, editing approaches, and AI behavior settings.

## Key Features

### Multiple Profiles Per User
- Create unlimited profiles for different content types
- Each profile has unique preferences
- Switch between profiles instantly
- One profile is always "active" for AI suggestions

### Pre-Built Templates
8 starter templates are available:
1. **YouTube Gaming** - High energy, vibrant, fast-paced
2. **Corporate Training** - Professional, formal, structured
3. **Educational Tutorial** - Clear, step-by-step, instructional
4. **Product Review** - Balanced, informative, honest
5. **Vlog/Personal** - Casual, authentic, relatable
6. **Marketing/Promotional** - Persuasive, energetic, compelling
7. **Documentary** - Cinematic, thoughtful, in-depth
8. **Quick Tips/Shorts** - Fast-paced, vertical (9:16), concise

### Preference Categories

#### Content Type
Defines the primary purpose:
- Tutorial, Explainer, Entertainment, Vlog, Review, etc.

#### Tone & Voice
- **Formality**: 0-100 (casual to formal)
- **Energy**: 0-100 (calm to high-energy)
- **Personality Traits**: friendly, authoritative, quirky, empathetic, etc.
- **Custom Description**: Free-text tone description

#### Visual Style
- **Aesthetic**: cinematic, corporate, vibrant, minimalist, documentary
- **Color Palette**: warm, cool, monochrome, vibrant, pastel, brand colors
- **Shot Types**: close-ups vs. wide shots preference
- **Composition**: rule of thirds, centered, dynamic
- **Pacing**: fast cuts vs. longer shots
- **B-Roll Usage**: minimal, moderate, heavy

#### Audio & Music
- **Music Genres**: List of preferred genres
- **Music Energy**: 0-100 (calm to energetic)
- **Prominence**: subtle background, balanced, prominent feature
- **Sound Effects**: none, minimal, moderate, heavy
- **Voice Style**: authoritative, warm, energetic, calm
- **Mixing**: voice-focused vs. balanced

#### Editing Style
- **Pacing**: 0-100 (slow/deliberate to fast/dynamic)
- **Cut Frequency**: 0-100 (long takes to quick cuts)
- **Transitions**: simple cuts, subtle fades, dynamic transitions
- **Effects**: none, subtle, moderate, prominent
- **Scene Duration**: Average length in seconds
- **Philosophy**: invisible editing vs. stylized editing

#### Platform Target
- **Primary Platform**: YouTube, TikTok, Instagram, etc.
- **Secondary Platforms**: List of additional platforms
- **Aspect Ratio**: 16:9, 9:16, 1:1, 4:5
- **Target Duration**: Desired video length in seconds
- **Audience**: Demographics description

#### AI Behavior
- **Assistance Level**: 0-100 (minimal to highly proactive)
- **Verbosity**: brief, moderate, detailed suggestions
- **Auto-Apply**: Whether AI can apply suggestions automatically
- **Frequency**: only when asked, moderate, proactive
- **Creativity**: 0-100 (conservative/safe to experimental/creative)
- **Override Permissions**: Which decisions require user approval

### Decision Tracking
The system tracks user decisions on AI suggestions:
- Records accepted/rejected/modified suggestions
- Tracks per suggestion type (tone_adjustment, visual_style, etc.)
- Helps understand user preferences over time
- No automatic learning - explicit only

## API Endpoints

### Profile Management

```
GET    /api/profiles/user/{userId}           - List all profiles for user
POST   /api/profiles                         - Create new profile
GET    /api/profiles/{profileId}             - Get profile details
PUT    /api/profiles/{profileId}             - Update profile metadata
DELETE /api/profiles/{profileId}             - Delete profile
POST   /api/profiles/{profileId}/activate    - Set as active profile
POST   /api/profiles/{profileId}/duplicate   - Duplicate profile
```

### Templates

```
GET    /api/profiles/templates               - List all templates
POST   /api/profiles/from-template           - Create profile from template
```

### Preferences

```
PUT    /api/profiles/{profileId}/preferences        - Update preferences
GET    /api/profiles/{profileId}/preferences/summary - Get preference summary
```

### Decision Tracking

```
POST   /api/profiles/{profileId}/decisions/record - Record user decision
```

## Usage Examples

### Create Profile from Template

```json
POST /api/profiles/from-template
{
  "userId": "user123",
  "profileName": "My Gaming Channel",
  "description": "Let's Play and review videos",
  "fromTemplateId": "youtube-gaming"
}
```

### Update Tone Preferences

```json
PUT /api/profiles/{profileId}/preferences
{
  "tone": {
    "formality": 30,
    "energy": 85,
    "personalityTraits": ["enthusiastic", "humorous", "authentic"],
    "customDescription": "Fun and engaging, like chatting with a friend"
  }
}
```

### Record Decision

```json
POST /api/profiles/{profileId}/decisions/record
{
  "suggestionType": "tone_adjustment",
  "decision": "accepted",
  "context": {
    "originalTone": "formal",
    "suggestedTone": "casual",
    "confidence": "high"
  }
}
```

## Data Storage

### Location
Profiles are stored in the portable AuraData directory:
- Profiles: `AuraData/Profiles/{profileId}.json`
- Preferences: `AuraData/Profiles/Preferences/{profileId}.json`
- Decisions: `AuraData/Profiles/Decisions/{profileId}.json`

### Format
All data is stored as JSON with atomic file operations (write to temp, then move).

### Backup
The portable storage makes it easy to:
- Back up profiles by copying the AuraData directory
- Share profiles between users
- Version control with git

## Integration with AI Services

The ProfileContextProvider service helps AI services access profile preferences:

```csharp
// Get active profile preferences
var preferences = await _profileContextProvider.GetActivePreferencesAsync(userId, ct);

// Get context string for AI prompts
var context = await _profileContextProvider.GetProfileContextAsync(userId, ct);

// Apply preferences to prompt
ProfileContextProvider.ApplyPreferencesToPrompt(preferences, ref prompt);

// Get tone guidance
var toneGuidance = ProfileContextProvider.GetToneGuidance(preferences);

// Get creativity temperature for AI model
var temperature = ProfileContextProvider.GetCreativityTemperature(preferences);

// Check if auto-apply is enabled
var autoApply = ProfileContextProvider.ShouldAutoApply(preferences);
```

## Best Practices

### For Users
1. Start with a template closest to your content type
2. Customize preferences gradually
3. Create separate profiles for different content types
4. Use descriptive profile names
5. Switch profiles when starting a new project type

### For Developers
1. Always check for active profile before AI operations
2. Respect the AutoApplySuggestions setting
3. Record user decisions for transparency
4. Use ProfileContextProvider for consistent preference access
5. Provide clear UI indication of active profile
6. Allow per-project profile override

## Security Considerations

- Profile data is stored locally in the user's AuraData directory
- No sensitive information should be stored in profiles
- API keys and credentials are managed separately
- Profile IDs are GUIDs to prevent enumeration
- Decision tracking respects user privacy

## Future Enhancements

Potential future features:
- Profile import/export
- Profile sharing via links
- Collaborative profiles for teams
- Profile versioning and rollback
- AI-suggested preference adjustments based on decision history
- Profile analytics (most used, success metrics)
- Cross-platform profile sync

## Troubleshooting

### Profile Not Found
- Verify profileId is correct
- Check that profile wasn't deleted
- Ensure user has permission

### Preferences Not Applied
- Confirm profile is activated
- Check ProfileContextProvider integration
- Verify AI service uses profile context

### Decision Tracking Not Working
- Ensure decision endpoint is called after user action
- Check decision type is valid
- Verify profileId matches active profile

## Support

For issues or questions:
- Check API documentation
- Review test cases in ProfileServiceTests.cs
- Examine ProfileTemplateService for template examples
- Look at ProfileContextProvider for integration patterns
