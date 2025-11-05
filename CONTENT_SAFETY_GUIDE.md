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

## Stock Media Safety Filtering

### Overview

Aura Video Studio includes built-in content safety filtering for stock media (Pexels, Unsplash, Pixabay) to ensure brand-safe content selection.

### Features

#### Safe Search Filters

All stock media providers support safe search filtering:

```json
{
  "query": "nature landscape",
  "safeSearchEnabled": true,
  "providers": ["Pexels", "Unsplash", "Pixabay"]
}
```

When enabled, the system:
- Filters explicit content
- Blocks violent imagery
- Removes sensitive topics
- Sanitizes search queries
- Validates licensing metadata

#### Content Safety Service

**Location**: `Aura.Core/Services/StockMedia/ContentSafetyFilterService.cs`

The content safety filter service provides:

1. **Query Validation**: Checks if search query is appropriate
2. **Query Sanitization**: Removes blocked keywords from queries
3. **Content Filtering**: Filters results based on text analysis
4. **Custom Keyword Lists**: User-defined blocked/allowed terms

#### Safety Levels

Configure safety level (0-10 scale):

```json
{
  "safetyFilters": {
    "enabledFilters": true,
    "blockExplicitContent": true,
    "blockViolentContent": true,
    "blockSensitiveTopics": true,
    "safetyLevel": 5,
    "blockedKeywords": ["keyword1", "keyword2"],
    "allowedKeywords": ["keyword3"]
  }
}
```

**Safety Levels**:
- **0-3**: Minimal filtering (only extreme content)
- **4-6**: Moderate filtering (platform-safe, default)
- **7-9**: Strict filtering (family-friendly)
- **10**: Maximum filtering (educational only)

#### Blocked Content Categories

The service blocks content containing:
- Explicit or adult content
- Violent imagery
- Hate speech or discrimination
- Drug/alcohol references
- Weapons or dangerous items
- Controversial political content
- Self-harm or dangerous activities

#### API Integration

```bash
# Search with safety filters
POST /api/stock-media/search
{
  "query": "beautiful sunset",
  "safeSearchEnabled": true
}

# Validate query before search
POST /api/content-safety/validate-query
{
  "query": "proposed search term"
}

# Sanitize query
POST /api/content-safety/sanitize-query
{
  "query": "query with potentially unsafe terms"
}
```

### Usage Examples

#### Basic Safe Search

```typescript
const request = {
  query: "nature photography",
  mediaType: "Image",
  providers: ["Pexels", "Unsplash"],
  safeSearchEnabled: true,
  count: 20
};

const results = await stockMediaService.search(request);
```

#### Custom Safety Filters

```typescript
const safetyConfig = {
  enabledFilters: true,
  blockExplicitContent: true,
  blockViolentContent: true,
  blockSensitiveTopics: true,
  safetyLevel: 7,
  blockedKeywords: ["controversial", "political"],
  allowedKeywords: ["nature", "landscape", "education"]
};

const safetyService = new ContentSafetyFilterService(safetyConfig);
const isSafe = await safetyService.isContentSafe(text);
```

#### Query Sanitization

```typescript
const originalQuery = "beautiful sunset with violence";
const sanitized = safetyService.sanitizeQuery(originalQuery);
// Result: "beautiful sunset"
```

### Best Practices

1. **Always enable safe search** for client-facing content
2. **Validate queries** before submitting to stock providers
3. **Review safety logs** regularly for improvement
4. **Customize keyword lists** for your brand requirements
5. **Test safety filters** with edge cases
6. **Export safety reports** for compliance audits
7. **Update blocked lists** based on feedback

### Provider-Specific Guidelines

#### Pexels
- Safe search built-in
- Content moderation by Pexels team
- Reports inappropriate content automatically

#### Unsplash
- Curated high-quality content
- Artistic content may need review
- Community reporting system

#### Pixabay
- Safe search filter supported
- Family-friendly content focus
- Strict content moderation

### Licensing and Compliance

All stock media includes licensing metadata:
- Commercial use permissions
- Attribution requirements
- Creator information
- License URLs
- Source platform

Export licensing reports for compliance:
```bash
GET /api/stock-media/licensing/export?format=csv
GET /api/stock-media/licensing/summary
```

## LLM Safety Integration

### Overview

Aura Video Studio includes comprehensive LLM safety integration that validates prompts and responses at multiple pipeline stages. This ensures AI-generated content complies with your safety policies before being used in video generation.

### Features

#### Prompt Validation

All prompts sent to LLM providers are validated against your active safety policy:

```typescript
// Example: Validate a prompt before sending to LLM
const result = await apiClient.post('/api/content-safety/validate-llm-prompt', {
  prompt: "Your prompt text here",
  policyId: "optional-policy-id"
});

if (!result.isValid) {
  // Show safety warning dialog
  // Use suggested alternatives or modified prompt
}
```

**Validation includes:**
- Keyword matching against blocked terms
- Topic detection for controversial subjects
- Category scoring (violence, profanity, etc.)
- Automatic prompt modification suggestions
- Safe alternative generation

#### Response Validation

LLM responses are validated before being used in the pipeline:

```typescript
// Automatically validated after LLM returns response
// If unsafe, response is either:
// 1. Auto-fixed using suggested replacements
// 2. Regenerated with safer prompts
// 3. Blocked if no safe alternative exists
```

#### Automatic Remediation

When content violates safety policies, the system provides:

1. **Modified Prompts** - Automatically rewritten to pass safety checks
2. **Safe Alternatives** - Multiple alternative prompts preserving intent
3. **Explanations** - Clear descriptions of why content was blocked
4. **Remediation Strategies** - Step-by-step guidance to fix issues

### Safety Warning Dialog

The frontend includes a dedicated safety warning dialog that appears when content is blocked:

**Features:**
- Clear violation display with category and severity
- Matched content highlighting
- Suggested fixes for each violation
- Alternative content suggestions (clickable)
- Override option for Advanced Mode users
- Policy name and enforcement rules

**Usage in Code:**
```typescript
import SafetyWarningDialog from '@/components/ContentSafety/SafetyWarningDialog';

<SafetyWarningDialog
  open={showDialog}
  onOpenChange={setShowDialog}
  violations={violations}
  alternatives={alternatives}
  onUseAlternative={(alt) => handleUseAlternative(alt)}
  onOverride={() => handleOverride()}
  canOverride={policy.allowUserOverride}
  requiresAdvancedMode={true}
  policyName={policy.name}
/>
```

### API Endpoints for LLM Safety

#### Validate Prompt

```bash
POST /api/content-safety/validate-llm-prompt
Content-Type: application/json

{
  "prompt": "Create a video about...",
  "policyId": "optional-policy-id"
}
```

Response:
```json
{
  "originalPrompt": "...",
  "isValid": false,
  "canProceed": true,
  "modifiedPrompt": "...",
  "explanation": "...",
  "alternatives": ["alt1", "alt2", "alt3"],
  "analysisResult": { ... }
}
```

#### Suggest Alternatives

```bash
POST /api/content-safety/suggest-alternatives
Content-Type: application/json

{
  "content": "Unsafe content",
  "policyId": "optional-policy-id",
  "count": 3
}
```

#### Get Remediation Report

```bash
POST /api/content-safety/remediation-report
Content-Type: application/json

{
  "contentId": "unique-id",
  "content": "Content to analyze",
  "policyId": "optional-policy-id"
}
```

Response includes:
- Summary of issues
- Detailed explanation with category scores
- Multiple remediation strategies
- Suggested alternatives
- User options (apply fixes, override, cancel)
- Recommended action

#### Explain Block

```bash
POST /api/content-safety/explain-block
Content-Type: application/json

{
  "content": "Blocked content",
  "policyId": "optional-policy-id"
}
```

Returns user-friendly markdown explanation of why content was blocked.

### Integration with Video Generation

Safety checks are integrated at key pipeline stages:

1. **Script Generation** (Brief → Script)
   - Validates user brief before sending to LLM
   - Validates generated script before TTS

2. **Visual Prompts** (Script → Image Prompts)
   - Validates image generation prompts
   - Sanitizes prompts for safe image generation

3. **Stock Media Search** (Scene → Query)
   - Validates search queries
   - Sanitizes queries to remove unsafe terms
   - Filters results based on metadata

4. **Final Review** (Pre-Export)
   - Optional final safety scan of complete content
   - Generates compliance report

### Advanced Mode Override

Users in Advanced Mode can override safety blocks:

**Requirements:**
- Advanced Mode must be enabled in Settings
- Policy must allow user overrides (`allowUserOverride: true`)
- User accepts responsibility for content

**Audit Trail:**
All overrides are logged with:
- User ID
- Timestamp
- Policy violated
- Violations overridden
- Reason (if provided)

**UI Flow:**
1. Content blocked by safety check
2. Safety Warning Dialog displays
3. Override button visible (Advanced Mode only)
4. User clicks Override
5. Decision recorded in audit log
6. Content proceeds in pipeline

## Stock Media Safety Filtering (Enhanced)

### Query Validation

All stock media queries are validated before searching:

```bash
POST /api/stock-media/validate-query
Content-Type: application/json

{
  "query": "search query",
  "policyId": "optional-policy-id"
}
```

Response:
```json
{
  "originalQuery": "...",
  "isValid": true,
  "validationMessage": "Query is safe",
  "sanitizedQuery": "...",
  "alternatives": ["alt1", "alt2"]
}
```

### Query Sanitization

Automatically removes unsafe terms:

```bash
POST /api/stock-media/sanitize-query
Content-Type: application/json

{
  "query": "query with blocked terms"
}
```

### Safe Search Enforcement

All stock media searches automatically:
1. Validate query against safety policy
2. Sanitize query if needed
3. Apply provider-specific safe search flags
4. Filter results based on metadata
5. Record decision in audit log

### Audit Trail for Stock Media

All stock media safety decisions are logged:
- Original query
- Sanitized query
- Policy applied
- Violations detected
- User override (if any)
- Results filtered

Access audit logs:
```bash
GET /api/content-safety/audit?contentType=StockMediaQuery
```

## Best Practices for Developers

### Integrating Safety Checks

1. **Validate Early** - Check content as early as possible
2. **Provide Alternatives** - Always offer safe alternatives
3. **Clear Messaging** - Use user-friendly explanations
4. **Allow Overrides** - For Advanced Mode, when appropriate
5. **Audit Everything** - Log all safety decisions

### Error Handling

```typescript
try {
  const result = await validatePrompt(prompt, policy);
  
  if (!result.isValid) {
    if (result.canProceed && result.modifiedPrompt) {
      // Use modified prompt
      return result.modifiedPrompt;
    } else if (result.alternatives.length > 0) {
      // Show alternatives to user
      showSafetyDialog(result);
    } else {
      // Block and explain
      showErrorMessage(result.explanation);
    }
  }
} catch (error) {
  logger.error('Safety check failed', error);
  // Fallback to blocking content
}
```

### Performance Considerations

- Safety checks add 50-200ms latency per check
- Use caching for repeated content validation
- Batch validation when possible
- Run checks asynchronously when not blocking

## Advanced Features

### Policy Center

The Policy Center provides a centralized interface for managing all safety policies with enhanced features:

**Features:**
- Create and edit custom policies with descriptive names
- Configure blocked keywords with different actions (Block, Warn, Auto-Fix)
- Set category thresholds for granular control
- Enable/disable policies without deleting them
- View policy usage statistics
- Set default policies for automatic application

**Access:**
Navigate to **Settings** → **Content Safety** → **Policy Center** tab

**Workflow:**
1. Click "Create Policy" to start a new policy
2. Enter policy name and description
3. Select base preset (Unrestricted, Minimal, Moderate, Strict)
4. Add blocked keywords with specific actions
5. Configure enable/override settings
6. Save the policy for use across your projects

### Incident Log Viewer

Track all safety decisions with comprehensive audit logging and advanced override management:

**Features:**
- View all content safety incidents with timestamps
- Filter by content ID, policy ID, or decision type
- Export audit logs to CSV for compliance reporting
- View detailed incident information including overridden violations
- Track user decisions (Proceed, Block, Override, Modified)
- Advanced Mode override tracking with explicit consent

**Access:**
Navigate to **Settings** → **Content Safety** → **Incident Log** tab

**Log Entry Details:**
- Timestamp of the safety check
- Content ID for traceability
- Policy applied to the content
- User who made the decision
- Decision type and reason
- List of overridden violations (if any)

**Use Cases:**
- Compliance audits and reporting
- Review false positives for policy tuning
- Track Advanced Mode overrides for accountability
- Export data for external analysis

### Prompt Diff Viewer

Visual comparison of original and modified prompts with clear highlighting:

**Features:**
- Side-by-side or inline diff view
- Color-coded changes (red for removed, green for added)
- Explanation of why changes were needed
- Accept or reject modifications with one click
- Preserves intent while ensuring safety compliance

**Integration:**
The Prompt Diff Viewer appears automatically in the Safety Warning Dialog when a modified prompt is suggested by the LLM safety integration.

**Example:**
```
Original: "Create a video about violence and fighting"
Modified: "Create a video about conflict resolution and peaceful problem-solving"
```

### LLM-Assisted Safety Features

#### Suggest Safe Phrasing

When content is flagged, the system provides AI-generated alternatives that preserve your intent while meeting safety requirements:

```bash
POST /api/content-safety/suggest-alternatives
{
  "content": "Your original content",
  "policyId": "optional-policy-id",
  "count": 3
}
```

Response includes multiple safe alternatives ranked by relevance.

#### Explain Safety Block

Get user-friendly explanations in markdown format:

```bash
POST /api/content-safety/explain-block
{
  "content": "Blocked content",
  "policyId": "optional-policy-id"
}
```

Returns detailed explanation with:
- Specific violations and categories
- Severity scores
- Suggested fixes
- Override options (if available)

### Remediation Reports

Comprehensive reports for addressing safety violations:

```bash
POST /api/content-safety/remediation-report
{
  "contentId": "unique-id",
  "content": "Content to analyze",
  "policyId": "optional-policy-id"
}
```

**Report Includes:**
- Summary of issues
- Detailed explanation with category scores
- Multiple remediation strategies with success likelihood
- Suggested alternatives
- User options (apply fixes, override, cancel)
- Recommended action

## Future Enhancements

Potential features for future releases:

- Real-time safety analysis during content creation
- Machine learning-based detection improvements
- Integration with external content safety APIs (Azure Content Safety, Google Perspective)
- Safety dashboard with analytics
- Policy templates for specific industries
- Bulk content scanning tools
- Export safety reports (PDF)
- AI-powered content moderation with computer vision
- Automatic NSFW image detection
- Sentiment analysis integration
- Multi-language safety detection

## Support

For questions or issues with content safety features:

1. Review this guide and API documentation
2. Check existing GitHub issues
3. Create a new issue with:
   - Policy configuration (JSON)
   - Content sample (if appropriate)
   - Expected vs. actual behavior
   - Steps to reproduce
4. For LLM safety issues, include:
   - Original prompt
   - LLM provider used
   - Policy applied
   - Validation result

## License

Content safety features are part of Aura Video Studio and subject to the same license as the main application.
