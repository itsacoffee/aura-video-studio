# Content Safety Guide

## Overview

Aura Video Studio includes a comprehensive content safety and filtering system that gives you complete control over what content is appropriate for your videos. The system supports everything from completely unrestricted mode to highly filtered family-friendly content.

## Features

### Safety Policies

Choose from preset policies or create custom ones:

- **Unrestricted**: No filtering whatsoever (user assumes all responsibility)
- **Minimal**: Only illegal content blocked (hate speech, explicit violence)
- **Moderate**: Balanced filtering appropriate for general audiences (YouTube-safe)
- **Strict**: Family-friendly content only, suitable for children
- **Custom**: User-defined rules with full granularity

### Safety Categories (0-10 Scale)

Configure thresholds for 10 independent categories:

1. **Profanity**: Control language appropriateness
2. **Violence**: Filter violent content and imagery
3. **Sexual Content**: Manage adult themes and content
4. **Hate Speech**: Block discriminatory content
5. **Drug/Alcohol**: Control substance references
6. **Controversial Topics**: Filter political, religious, and sensitive topics
7. **Copyright**: Manage copyright concerns
8. **Self-Harm**: Block self-harm and suicide content
9. **Graphic Imagery**: Control disturbing visual content
10. **Misinformation**: Flag potentially false information

Each category uses a 0-10 threshold:
- **0** = No restrictions
- **1-3** = Minimal filtering (only extreme content)
- **4-6** = Moderate filtering (platform-safe)
- **7-9** = Strict filtering (family-friendly)
- **10** = Maximum filtering (educational content only)

### Action Types

When content violates a policy, you can choose what happens:

- **Block**: Prevent generation entirely
- **Warn**: Show warning but allow user to proceed
- **Require Review**: Flag for manual review before proceeding
- **Auto-Fix**: Automatically modify content to comply
- **Add Disclaimer**: Allow content but add warning/disclaimer
- **Log Only**: Allow content but log for audit purposes

### Keyword Management

Create custom keyword lists:
- Whole word, substring, or regex matching
- Case-sensitive or case-insensitive
- Replacement rules (auto-replace banned words)
- Context exceptions (word OK in some contexts)
- Import from text files (CSV/TXT)
- Starter lists for common scenarios

### Topic Filtering

Filter content by topic:
- AI-powered topic detection
- Confidence-based filtering
- Common topics: Politics, Religion, Violence, Drugs, Gambling, etc.
- Allowed context exceptions (educational, news, historical)
- Custom topic taxonomies

### Brand Safety

Ensure brand compliance:
- Required keywords enforcement
- Banned competitor mentions
- Brand terminology guidelines
- Brand voice scoring
- Required disclaimers
- Trademark protection

### Age Appropriateness

Target specific age groups:
- Minimum and maximum age settings
- Content rating systems (G, PG, Teen, Mature, Adult)
- Age-specific restrictions
- Parental guidance indicators

### Cultural Sensitivity

Respect diverse audiences:
- Multi-region targeting
- Cultural taboos by region
- Stereotype avoidance
- Inclusive language requirements
- Religious sensitivities

### Legal Compliance

Meet regulatory requirements:
- COPPA, GDPR, FTC compliance flags
- Required disclosure automation
- Industry-specific regulations
- Medical/health claim restrictions
- Financial advice disclaimers

### Audit Trail

Track all safety decisions:
- Complete decision logging
- User override tracking
- Searchable audit logs
- Policy effectiveness metrics
- False positive/negative rates

## Using Content Safety

### In the Settings UI

1. Navigate to **Settings** → **Content Safety**
2. Select a preset policy or create a custom one
3. Enable/disable filtering as needed
4. Adjust category thresholds using sliders
5. Toggle user override capability
6. Save your policy

### Via API

#### Analyze Content

```bash
POST /api/contentsafety/analyze
Content-Type: application/json

{
  "content": "Your script or content to analyze",
  "policyId": "optional-policy-id"
}
```

Response includes:
- Overall safety score (0-100)
- List of violations with severity scores
- Recommended actions
- Suggested fixes
- Category-specific scores

#### Manage Policies

```bash
# List all policies
GET /api/contentsafety/policies

# Get specific policy
GET /api/contentsafety/policies/{id}

# Create policy
POST /api/contentsafety/policies

# Update policy
PUT /api/contentsafety/policies/{id}

# Delete policy
DELETE /api/contentsafety/policies/{id}

# Get presets
GET /api/contentsafety/presets
```

#### Import Keywords

```bash
POST /api/contentsafety/keywords/import
Content-Type: application/json

{
  "text": "word1\nword2\nword3",
  "defaultAction": "Warn"
}
```

#### Get Common Topics

```bash
GET /api/contentsafety/topics/common
```

### In Code (C#)

```csharp
// Inject the service
public class MyService
{
    private readonly ContentSafetyService _safetyService;
    private readonly SafetyIntegrationService _integrationService;
    
    public MyService(
        ContentSafetyService safetyService,
        SafetyIntegrationService integrationService)
    {
        _safetyService = safetyService;
        _integrationService = integrationService;
    }
    
    public async Task<bool> GenerateVideoAsync(string script, SafetyPolicy policy)
    {
        // Check script safety
        var checkResult = await _integrationService.CheckScriptSafetyAsync(
            script, 
            policy, 
            CancellationToken.None);
        
        if (!checkResult.CanProceed)
        {
            Console.WriteLine($"Safety check failed: {checkResult.Message}");
            return false;
        }
        
        // Apply auto-fixes if needed
        var fixedScript = _integrationService.ApplyAutoFixes(
            script, 
            checkResult.AnalysisResult);
        
        // Add required disclaimers
        var finalScript = _integrationService.AddDisclaimers(
            fixedScript,
            checkResult.AnalysisResult,
            policy);
        
        // Proceed with generation...
        return true;
    }
}
```

## Best Practices

### For General Content Creators

1. Use **Moderate** preset as a starting point
2. Enable user override for flexibility
3. Review violations regularly to tune your policy
4. Use topic filtering for controversial subjects
5. Enable audit trail for accountability

### For Family-Friendly Content

1. Use **Strict** preset
2. Disable user override to enforce consistency
3. Set all category thresholds to 7-9
4. Enable age appropriateness settings (target: General rating)
5. Add required disclaimers for any edge cases

### For Brand Content

1. Start with **Moderate** preset
2. Add brand safety settings:
   - Required brand keywords
   - Banned competitor mentions
   - Brand voice guidelines
3. Enable compliance settings for your industry
4. Use custom keyword lists for brand terminology
5. Review all content before publishing

### For Educational Content

1. Use **Moderate** preset with context exceptions
2. Allow educational context for sensitive topics
3. Use topic filtering with "educational" context allowed
4. Enable required disclosures for health/financial topics
5. Keep audit trail for accountability

### For Unrestricted Content

1. Use **Unrestricted** preset (filtering disabled)
2. User assumes all responsibility
3. Consider liability and platform guidelines
4. Recommended only for private/personal use
5. Enable audit trail for record-keeping

## Integration with Video Generation

Content safety checks can be integrated at multiple stages:

1. **Script Generation**: Check AI-generated scripts before TTS
2. **Visual Prompts**: Validate image/video prompts before generation
3. **Final Review**: Analyze complete video content before export
4. **Project-Specific**: Override global policy for specific projects

Example workflow:

```
User Input → Safety Check → Generate Script → Safety Check → 
Generate Visuals → Safety Check → Render Video → Final Review → Export
```

Each stage can use the same or different policies depending on your needs.

## Troubleshooting

### Content Incorrectly Flagged (False Positive)

- Lower the threshold for the relevant category
- Add context exceptions to topic filters
- Add the word/phrase to allowed exceptions
- Enable user override to proceed anyway
- Consider using "Warn" instead of "Block"

### Content Not Flagged When It Should Be (False Negative)

- Increase the threshold for the relevant category
- Add specific keywords to banned list
- Create custom topic filters
- Use stricter preset as baseline
- Review policy effectiveness in audit logs

### Policy Too Restrictive

- Switch to less strict preset
- Lower category thresholds
- Enable user override
- Add more context exceptions
- Use "Warn" action instead of "Block"

### Policy Too Permissive

- Switch to stricter preset
- Increase category thresholds
- Disable user override
- Add more keywords to banned list
- Use "Block" action for critical violations

## Architecture

### Backend Components

- **ContentSafetyService**: Core safety analysis engine
- **KeywordListManager**: Keyword matching and management
- **TopicFilterManager**: Topic detection and filtering
- **SafetyIntegrationService**: Pipeline integration helper
- **SafetyPolicyPresets**: Default policy configurations

### Frontend Components

- **contentSafety.ts**: Zustand state management store
- **ContentSafetyTab.tsx**: Settings UI component
- Settings page integration

### API Layer

- **ContentSafetyController**: RESTful API endpoints
- **ContentSafetyDtos.cs**: Request/response models
- Comprehensive CRUD operations for policies

### Data Storage

- Policies stored in: `{AuraDataDir}/content-safety-policies.json`
- Audit logs stored in: `{AuraDataDir}/content-safety-audit.json`
- Automatically created on first use
- JSON format for easy inspection and backup

## Future Enhancements

Potential features for future releases:

- Real-time safety analysis during content creation
- Machine learning-based detection improvements
- Integration with external content safety APIs
- Advanced keyword list management UI
- Safety dashboard with analytics
- Policy templates for specific industries
- Bulk content scanning tools
- Export safety reports (PDF)

## Support

For questions or issues with content safety features:

1. Review this guide and API documentation
2. Check existing GitHub issues
3. Create a new issue with:
   - Policy configuration (JSON)
   - Content sample (if appropriate)
   - Expected vs. actual behavior
   - Steps to reproduce

## License

Content safety features are part of Aura Video Studio and subject to the same license as the main application.
