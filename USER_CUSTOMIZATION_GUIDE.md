# User Customization and Control Guide

This guide explains how to use the comprehensive user customization framework in Aura Video Studio. The framework gives you complete control over AI-driven decisions, content filtering, and generation settings.

## Overview

Aura Video Studio now provides granular control over:

- **Model Selection and Pinning**: Explicit control over which AI models are used at every pipeline stage
- **Custom Audience Profiles**: Define audience characteristics beyond preset profiles
- **Content Filtering**: Control what content is acceptable with custom policies
- **AI Behavior**: Customize LLM parameters and prompts for each pipeline stage
- **Quality Thresholds**: Define your own quality standards and validation rules
- **Visual Styles**: Create custom visual aesthetic preferences
- **Import/Export**: Share and backup your configurations

## Architecture

### Backend Components

**Models** (`Aura.Core/Models/UserPreferences/`):
- `CustomAudienceProfile` - 20+ customizable audience parameters
- `ContentFilteringPolicy` - Granular content filtering controls
- `AIBehaviorSettings` - LLM parameter customization per stage
- `CustomPromptTemplate` - User-defined prompt templates
- `CustomQualityThresholds` - User-defined quality standards
- `CustomVisualStyle` - Visual aesthetic customization

**Service** (`Aura.Core/Services/UserPreferences/`):
- `UserPreferencesService` - Central service for all user customizations
  - CRUD operations for all custom entities
  - Export/import functionality
  - File-based JSON persistence

**API** (`Aura.Api/Controllers/`):
- `UserPreferencesController` - RESTful endpoints for all customizations

### Frontend Components

**State Management** (`Aura.Web/src/state/`):
- `userPreferences.ts` - Zustand store for all customization state

**UI Components** (`Aura.Web/src/components/Settings/`):
- `UserPreferencesTab` - Main settings interface

## Using the API

### Custom Audience Profiles

#### Create a Custom Audience Profile

```bash
POST /api/user-preferences/audience-profiles
Content-Type: application/json

{
  "name": "Tech-Savvy Millennials",
  "minAge": 25,
  "maxAge": 35,
  "educationLevel": "Bachelor's or higher",
  "educationLevelDescription": "College educated with strong tech background",
  "vocabularyLevel": 7,
  "sentenceStructurePreference": "Complex",
  "readingLevel": 12,
  "violenceThreshold": 2,
  "profanityThreshold": 3,
  "sexualContentThreshold": 1,
  "controversialTopicsThreshold": 6,
  "humorStyle": "Moderate",
  "sarcasmLevel": 5,
  "formalityLevel": 4,
  "attentionSpanSeconds": 180,
  "pacingPreference": "Fast",
  "informationDensity": 7,
  "technicalDepthTolerance": 8,
  "jargonAcceptability": 7,
  "emotionalTone": "Uplifting",
  "emotionalIntensity": 6,
  "ctaAggressiveness": 5,
  "ctaStyle": "Conversational",
  "culturalSensitivities": ["Inclusive language", "Gender neutral"],
  "topicsToAvoid": ["Politics", "Religion"],
  "topicsToEmphasize": ["Technology", "Innovation", "Sustainability"],
  "jokeTypes": ["Wordplay", "Tech humor", "Pop culture"],
  "familiarTechnicalTerms": ["API", "Cloud", "AI", "ML", "SaaS"],
  "brandToneKeywords": ["Innovative", "Professional", "Forward-thinking"],
  "description": "Tech-savvy millennial audience for SaaS product videos",
  "tags": ["technology", "professional", "millennials"],
  "isFavorite": true
}
```

#### Get All Custom Audience Profiles

```bash
GET /api/user-preferences/audience-profiles
```

#### Update a Custom Audience Profile

```bash
PUT /api/user-preferences/audience-profiles/{id}
Content-Type: application/json

{
  // Include all fields from the profile with updates
  "formalityLevel": 6,
  "technicalDepthTolerance": 9
}
```

#### Delete a Custom Audience Profile

```bash
DELETE /api/user-preferences/audience-profiles/{id}
```

### Content Filtering Policies

#### Create a Content Filtering Policy

```bash
POST /api/user-preferences/filtering-policies
Content-Type: application/json

{
  "name": "Family-Friendly Strict",
  "filteringEnabled": true,
  "allowOverrideAll": false,
  "profanityFilter": "Strict",
  "customBannedWords": ["inappropriate", "word", "list"],
  "customAllowedWords": [],
  "violenceThreshold": 1,
  "blockGraphicContent": true,
  "sexualContentThreshold": 0,
  "blockExplicitContent": true,
  "bannedTopics": ["alcohol", "gambling", "violence"],
  "allowedControversialTopics": [],
  "politicalContent": "Off",
  "religiousContent": "RespectfulOnly",
  "substanceReferences": "Block",
  "blockHateSpeech": true,
  "hateSpeechExceptions": [],
  "copyrightPolicy": "Strict",
  "blockedConcepts": ["weapons", "drugs"],
  "allowedConcepts": [],
  "blockedPeople": [],
  "allowedPeople": [],
  "blockedBrands": [],
  "allowedBrands": [],
  "description": "Strict filtering for family-friendly content",
  "isDefault": false
}
```

#### Get All Content Filtering Policies

```bash
GET /api/user-preferences/filtering-policies
```

#### Update a Content Filtering Policy

```bash
PUT /api/user-preferences/filtering-policies/{id}
Content-Type: application/json

{
  // Include all fields from the policy with updates
  "violenceThreshold": 2
}
```

#### Delete a Content Filtering Policy

```bash
DELETE /api/user-preferences/filtering-policies/{id}
```

### Export and Import

#### Export All Preferences

```bash
GET /api/user-preferences/export
```

Response:
```json
{
  "jsonData": "{...full export...}",
  "exportDate": "2025-11-01T15:00:00Z",
  "version": "1.0"
}
```

#### Import Preferences

```bash
POST /api/user-preferences/import
Content-Type: application/json

{
  "jsonData": "{...previously exported data...}"
}
```

## Using the Frontend UI

### Accessing User Preferences

1. Navigate to Settings page
2. Look for the "User Preferences & Customization" tab (coming soon - currently accessible via API)

### Advanced Mode Toggle

- **Basic Mode**: Shows simplified controls for common use cases
- **Advanced Mode**: Exposes all 20+ parameters for granular control

Toggle this at the top of the User Preferences section.

### Managing Custom Audience Profiles

**Create a Profile**:
1. Click "Create Profile" button
2. Fill in basic information (name, age range, education)
3. Expand advanced sections for detailed control
4. Save the profile

**Edit a Profile**:
1. Click the edit icon next to a profile
2. Modify any parameters
3. Save changes

**Select a Profile**:
1. Click "Select" on the profile you want to use
2. This profile will be used in future video generations

**Delete a Profile**:
1. Click the delete icon next to a profile
2. Confirm deletion

### Managing Content Filtering Policies

Similar workflow to audience profiles:
- Create, edit, select, and delete filtering policies
- Toggle filtering on/off
- Define custom word lists
- Set thresholds for different content types

### Import/Export Preferences

**Export**:
1. Click the "Export" button
2. A JSON file will download with all your preferences
3. Store this file securely as a backup

**Import**:
1. Click the "Import" button
2. Select a previously exported JSON file
3. Confirm the import
4. All preferences will be restored

## Customization Parameters Reference

### Custom Audience Profile Parameters

**Age and Demographics**:
- `minAge` (number): Minimum age (0-120)
- `maxAge` (number): Maximum age (0-120)
- `educationLevel` (string): Education level description
- `educationLevelDescription` (string): Detailed explanation

**Language Complexity**:
- `vocabularyLevel` (1-10): Complexity of vocabulary
- `sentenceStructurePreference` (string): "Simple", "Mixed", "Complex"
- `readingLevel` (number): Grade level (1-20)

**Content Appropriateness** (all 0-10 scale):
- `violenceThreshold`: Acceptable level of violent content
- `profanityThreshold`: Acceptable level of profanity
- `sexualContentThreshold`: Acceptable level of sexual content
- `controversialTopicsThreshold`: Acceptable level of controversial topics

**Humor and Tone**:
- `humorStyle` (string): "None", "Light", "Moderate", "Heavy"
- `sarcasmLevel` (0-10): How much sarcasm is acceptable
- `jokeTypes` (string[]): Types of jokes that resonate
- `culturalHumorPreferences` (string[]): Cultural humor styles

**Formality and Pacing**:
- `formalityLevel` (0-10): 0=very casual, 10=extremely formal
- `attentionSpanSeconds` (number): Average attention span in seconds
- `pacingPreference` (string): "Slow", "Medium", "Fast", "VeryFast"
- `informationDensity` (1-10): How much information per unit time

**Technical Depth**:
- `technicalDepthTolerance` (0-10): How technical can content be
- `jargonAcceptability` (0-10): How much jargon is acceptable
- `familiarTechnicalTerms` (string[]): Terms the audience knows

**Emotional and Motivational**:
- `emotionalTone` (string): "Serious", "Neutral", "Uplifting", "Dramatic", "Inspirational"
- `emotionalIntensity` (0-10): How emotionally charged content should be
- `ctaAggressiveness` (0-10): How aggressive calls-to-action should be
- `ctaStyle` (string): "Direct", "Conversational", "Suggestive"

**Brand Voice**:
- `brandVoiceGuidelines` (string): Brand voice guidelines
- `brandToneKeywords` (string[]): Keywords describing brand tone
- `brandPersonality` (string): Brand personality description

**Cultural and Topical**:
- `culturalSensitivities` (string[]): Topics requiring special handling
- `topicsToAvoid` (string[]): Topics to completely avoid
- `topicsToEmphasize` (string[]): Topics to highlight

### Content Filtering Policy Parameters

**Global Controls**:
- `filteringEnabled` (boolean): Master switch for filtering
- `allowOverrideAll` (boolean): Unrestricted mode (bypasses all filters)

**Profanity Control**:
- `profanityFilter` (enum): "Off", "Mild", "Moderate", "Strict", "Custom"
- `customBannedWords` (string[]): Words to block (up to 1000+)
- `customAllowedWords` (string[]): Override default banned words

**Violence and Gore**:
- `violenceThreshold` (0-10): Acceptable violence level
- `blockGraphicContent` (boolean): Block graphic violence descriptions

**Sexual Content**:
- `sexualContentThreshold` (0-10): Acceptable sexual content level
- `blockExplicitContent` (boolean): Block explicit content

**Controversial Topics**:
- `bannedTopics` (string[]): Topics to completely avoid
- `allowedControversialTopics` (string[]): Controversial topics that are OK

**Political Content**:
- `politicalContent` (enum): "Off", "NeutralOnly", "AllowAll", "Custom"
- `politicalContentGuidelines` (string): Custom guidelines

**Religious Content**:
- `religiousContent` (enum): "Off", "RespectfulOnly", "AllowAll", "Custom"
- `religiousContentGuidelines` (string): Custom guidelines

**Substance References**:
- `substanceReferences` (enum): "Block", "Moderate", "Allow", "Custom"

**Hate Speech**:
- `blockHateSpeech` (boolean): Always block hate speech
- `hateSpeechExceptions` (string[]): Educational exceptions

**Copyright**:
- `copyrightPolicy` (enum): "Strict", "Moderate", "UserAssumesRisk"

**Allow/Block Lists**:
- `blockedConcepts` (string[]): Concepts to avoid
- `allowedConcepts` (string[]): Concepts that are OK
- `blockedPeople` (string[]): People not to mention
- `allowedPeople` (string[]): People OK to mention
- `blockedBrands` (string[]): Brands to avoid
- `allowedBrands` (string[]): Brands OK to mention

## Best Practices

### Creating Effective Audience Profiles

1. **Start with a preset**: Clone an existing profile and modify it
2. **Be specific**: Detailed profiles lead to better-targeted content
3. **Test and iterate**: Generate videos with different profiles and compare
4. **Use descriptive names**: Make profiles easy to identify later
5. **Tag your profiles**: Use tags for organization and filtering

### Content Filtering Strategy

1. **Define your use case first**: Family content, professional, educational, etc.
2. **Start strict, then relax**: Easier to loosen than tighten
3. **Use allowlists for exceptions**: Better than trying to anticipate everything
4. **Document your reasoning**: Use the description field to explain policy
5. **Test edge cases**: Generate content that pushes boundaries

### Managing Multiple Profiles

1. **Use descriptive names**: Include audience type and use case
2. **Leverage tags**: Organize profiles by project, client, or content type
3. **Favorite frequently used**: Mark common profiles as favorites
4. **Regular exports**: Back up your configurations monthly
5. **Version control**: Export before major changes, name files with versions

## Integration with Video Generation

### Applying Preferences to Video Jobs

Custom preferences are automatically applied when:

1. A custom audience profile is selected
2. A content filtering policy is active
3. AI behavior settings are configured

The VideoOrchestrator reads these preferences and applies them throughout the pipeline:

- **Script Generation**: Uses audience profile parameters for tone, complexity, and style
- **Content Review**: Applies filtering policy to reject inappropriate content
- **Visual Selection**: Considers audience preferences for visual style
- **TTS Configuration**: Adjusts voice and pacing based on audience
- **Quality Validation**: Uses custom thresholds for acceptance criteria

### Override Hierarchy

When multiple settings could apply:

1. **Custom user preferences** (highest priority)
2. **Selected preset profile**
3. **Default system settings** (lowest priority)

## Troubleshooting

### Common Issues

**Problem**: Changes not appearing in generated videos
- **Solution**: Ensure the profile is selected before starting generation
- **Solution**: Check that preferences service is properly initialized

**Problem**: Import fails
- **Solution**: Verify JSON format matches export structure
- **Solution**: Check file size limits (< 10MB recommended)

**Problem**: Too many/too few filtering restrictions
- **Solution**: Review threshold values (0-10 scale)
- **Solution**: Check allow/block lists for conflicts

### Validation Errors

Profiles and policies are validated before saving:
- Age ranges must be valid (min ≤ max)
- Thresholds must be 0-10
- Required fields must be filled
- Lists cannot exceed reasonable sizes

## Advanced Topics

### Prompt Template Variables

When creating custom prompt templates (API only currently):

Available variables:
- `{{topic}}` - Video topic
- `{{audience}}` - Audience description
- `{{duration}}` - Target duration
- `{{goal}}` - Video goal
- `{{tone}}` - Desired tone
- `{{style}}` - Visual style

### A/B Testing Prompts

Custom prompt templates support A/B testing:
1. Create multiple variants in the same `variantGroup`
2. System tracks success/failure for each
3. View success rates to identify best performers

### Scoring Weights

Customize quality scoring weights to prioritize what matters most:
- `scriptQuality`: Content and writing (0-1)
- `visualQuality`: Image and video quality (0-1)
- `audioQuality`: Voice and sound quality (0-1)
- `brandCompliance`: Brand guideline adherence (0-1)
- `engagement`: Predicted viewer engagement (0-1)

All weights should sum to 1.0.

## Future Enhancements

Coming soon:
- Visual prompt template editor in UI
- Real-time preview of settings impact
- Profile comparison tool
- Community-shared profiles
- Machine learning-based profile suggestions
- Per-project preference sets
- Collaborative profile editing

## Support and Feedback

For questions or suggestions about the customization framework:
1. Check this documentation first
2. Review API documentation at `/api/swagger`
3. File issues on GitHub for bugs or feature requests
4. Share your custom profiles with the community

## Appendix

### Example Use Cases

**Educational Content**:
```json
{
  "name": "High School Science",
  "minAge": 14,
  "maxAge": 18,
  "vocabularyLevel": 6,
  "technicalDepthTolerance": 6,
  "formalityLevel": 4,
  "pacingPreference": "Medium",
  "humorStyle": "Light",
  "topicsToEmphasize": ["Science", "Learning", "Discovery"]
}
```

**Corporate Training**:
```json
{
  "name": "Corporate Professional",
  "minAge": 25,
  "maxAge": 55,
  "vocabularyLevel": 8,
  "technicalDepthTolerance": 7,
  "formalityLevel": 7,
  "pacingPreference": "Medium",
  "humorStyle": "Light",
  "ctaAggressiveness": 6,
  "brandToneKeywords": ["Professional", "Reliable", "Expert"]
}
```

**Social Media Content**:
```json
{
  "name": "Gen Z Social",
  "minAge": 18,
  "maxAge": 24,
  "vocabularyLevel": 5,
  "technicalDepthTolerance": 6,
  "formalityLevel": 2,
  "pacingPreference": "VeryFast",
  "attentionSpanSeconds": 15,
  "humorStyle": "Heavy",
  "sarcasmLevel": 7,
  "emotionalTone": "Uplifting"
}
```

### API Quick Reference

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/user-preferences/audience-profiles` | GET | List all profiles |
| `/api/user-preferences/audience-profiles` | POST | Create profile |
| `/api/user-preferences/audience-profiles/{id}` | GET | Get one profile |
| `/api/user-preferences/audience-profiles/{id}` | PUT | Update profile |
| `/api/user-preferences/audience-profiles/{id}` | DELETE | Delete profile |
| `/api/user-preferences/filtering-policies` | GET | List all policies |
| `/api/user-preferences/filtering-policies` | POST | Create policy |
| `/api/user-preferences/filtering-policies/{id}` | GET | Get one policy |
| `/api/user-preferences/filtering-policies/{id}` | PUT | Update policy |
| `/api/user-preferences/filtering-policies/{id}` | DELETE | Delete policy |
| `/api/user-preferences/export` | GET | Export all preferences |
| `/api/user-preferences/import` | POST | Import preferences |

## Model Selection and Pinning

### Overview

Aura Video Studio provides explicit, always-manual control over which AI models are used at every decision point in the video generation pipeline. You maintain complete control - the system never silently swaps or auto-chooses a model that overrides your explicit selections.

### Key Principles

1. **User Control First**: No silent model swaps - all changes require explicit user action
2. **Pin to Lock**: Pin models to prevent any automatic changes, even when a model becomes unavailable
3. **Graceful Degradation**: When a pinned model is unavailable, the system blocks and presents options rather than auto-switching
4. **Full Auditability**: All model selections and resolutions are logged with who/when/why/source

### Model Selection Precedence

The system follows this strict precedence order when resolving which model to use:

| Priority | Source | Behavior if Unavailable | Use Case |
|----------|--------|------------------------|----------|
| 1 | **Run Override (Pinned)** | ❌ Blocks - requires user decision | CLI/API with `--pin-model` for critical runs |
| 2 | **Run Override** | ✓ Falls back to next priority | CLI/API for one-off model testing |
| 3 | **Stage Pinned** | ❌ Blocks - requires user decision | Lock specific stages to specific models |
| 4 | **Project Override** | ✓ Falls back to next priority | Per-project model preferences |
| 5 | **Global Default** | ✓ Falls back to next priority | Application-wide defaults |
| 6 | **Automatic Fallback** | Only if explicitly enabled (OFF by default) | Safe fallback when no explicit selection exists |

### Using the UI

#### Setting a Model for a Pipeline Stage

1. Navigate to **Settings → Model Selection**
2. Find the pipeline stage (e.g., "Script Generation Model")
3. Select your preferred model from the dropdown
4. Optionally click **Pin** to prevent any automatic changes
5. Changes are saved immediately

#### Understanding the Pin/Lock Feature

- **Unpinned (default)**: If the model becomes unavailable, the system will fall back to the next priority level
- **Pinned**: If the model becomes unavailable, the pipeline will **block** and show a modal with options:
  - Apply recommended alternative
  - Retry with original model later
  - Continue with global defaults
  - Cancel the run

#### Testing a Model

1. Select a model in the picker
2. Click the **Test** button
3. The system runs a lightweight probe to verify:
   - Model is accessible with your API key
   - Model capabilities (context window, max tokens)
   - Deprecation status
   - Response time

#### Handling Deprecated Models

When you select a deprecated model, you'll see:
- A **Deprecated** badge on the picker
- A confirmation dialog explaining the deprecation
- The recommended replacement model
- Option to "Use Anyway" if you explicitly need the deprecated model

### Using the API

#### Get Available Models

```bash
GET /api/models/available?provider=OpenAI

Response:
{
  "providers": {
    "OpenAI": [
      {
        "provider": "OpenAI",
        "modelId": "gpt-4o",
        "maxTokens": 128000,
        "contextWindow": 128000,
        "aliases": ["gpt-4o-latest"],
        "isDeprecated": false
      },
      ...
    ]
  },
  "totalCount": 15,
  "correlationId": "xyz789"
}
```

#### Get Current Selections

```bash
GET /api/models/selection

Response:
{
  "globalDefaults": [
    {
      "provider": "OpenAI",
      "stage": "",
      "modelId": "gpt-4o-mini",
      "scope": "Global",
      "isPinned": false,
      "setBy": "user",
      "setAt": "2025-11-04T19:30:00Z",
      "reason": "User selection"
    }
  ],
  "stageSelections": [
    {
      "provider": "OpenAI",
      "stage": "script",
      "modelId": "gpt-4o",
      "scope": "Stage",
      "isPinned": true,
      "setBy": "user",
      "setAt": "2025-11-04T19:35:00Z",
      "reason": "User pinned for consistency"
    }
  ],
  "allowAutomaticFallback": false,
  "correlationId": "abc123"
}
```

#### Set Model Selection

```bash
POST /api/models/selection
Content-Type: application/json

{
  "provider": "OpenAI",
  "stage": "script",
  "modelId": "gpt-4o",
  "scope": "Stage",
  "pin": true,
  "reason": "Need consistent script quality"
}

Response:
{
  "applied": true,
  "reason": "Model selection saved successfully",
  "deprecationWarning": null,
  "correlationId": "def456"
}
```

#### Clear Selections

```bash
POST /api/models/selection/clear
Content-Type: application/json

{
  "provider": "OpenAI",
  "stage": "script",
  "scope": "Stage"
}

Response:
{
  "success": true,
  "message": "Selections cleared successfully",
  "correlationId": "ghi789"
}
```

#### Test a Model

```bash
POST /api/models/test
Content-Type: application/json

{
  "provider": "OpenAI",
  "modelId": "gpt-4o",
  "apiKey": "sk-..."
}

Response:
{
  "provider": "OpenAI",
  "modelId": "gpt-4o",
  "isAvailable": true,
  "isDeprecated": false,
  "contextWindow": 128000,
  "maxTokens": 128000,
  "errorMessage": null,
  "testedAt": "2025-11-04T19:40:00Z",
  "correlationId": "jkl012"
}
```

### Using the CLI

#### Set Model Override for a Run

```bash
# One-time override (not pinned)
aura generate --model gpt-4o --brief "..."

# Pinned override (blocks if unavailable)
aura generate --model gpt-4o --pin-model --brief "..."

# Per-stage override
aura generate --script-model gpt-4o --visual-model claude-3-opus --brief "..."
```

#### Allow Automatic Fallback

```bash
# For CI/CD scenarios where automatic fallback is acceptable
aura generate --allow-auto-fallback --brief "..."
```

### Settings: Allow Automatic Fallback

By default, automatic fallback is **disabled**. This means:
- If no explicit model selection exists and no fallback is configured, operations will block
- You must either set a model selection or enable automatic fallback

To enable automatic fallback:
1. Go to **Settings → Model Selection**
2. Toggle **Allow Automatic Fallback** to ON
3. When enabled, the system may automatically select a safe fallback model from the catalog if no explicit selection exists
4. All automatic fallback usages are logged in the audit trail and shown in notifications

**Important**: Pinned models always override the automatic fallback setting. If a pinned model is unavailable, the operation will block regardless of this setting.

### Audit Trail

All model selection resolutions are logged with full context:
- Which model was selected
- Source of the selection (run override, stage pinned, global default, etc.)
- Whether it was pinned
- Timestamp and correlation ID for traceability
- Job ID if part of a video generation job

Access the audit log via:
- Settings page: View recent model selections
- API: `GET /api/models/audit-log?limit=100`
- Logs directory: `AuraData/model-selections.json`

### Best Practices

1. **Pin critical stages**: Pin models for script generation and other stages where consistency is paramount
2. **Leave non-critical stages unpinned**: Allow fallback for less critical stages to avoid blocking
3. **Test before pinning**: Always test a model before pinning it to ensure it works with your API key
4. **Monitor deprecations**: Regularly check for deprecated models and plan migrations
5. **Use project overrides sparingly**: Reserve project overrides for special cases where project needs differ from global defaults
6. **Enable audit logging**: Review the audit log periodically to understand model usage patterns

### Troubleshooting

#### "Model unavailable" blocking modal appears

**Cause**: A pinned model is unavailable (API key issue, model deprecated, network error)

**Solution**:
1. Click "Apply recommended model" to use the suggested alternative
2. Or click "Retry with original later" and fix the underlying issue
3. Or unpin the model in settings to allow fallback

#### Models not appearing in picker

**Cause**: Models haven't been loaded from the catalog

**Solution**:
1. Check API key is configured for the provider
2. Refresh the models list (Settings → Model Selection → Reload button)
3. Check network connectivity
4. Review logs for API errors

#### Automatic fallback not working

**Cause**: Automatic fallback is disabled by default

**Solution**:
1. Enable "Allow Automatic Fallback" in Settings → Model Selection
2. Or set an explicit model selection for the provider/stage
3. Note that pinned models override automatic fallback

