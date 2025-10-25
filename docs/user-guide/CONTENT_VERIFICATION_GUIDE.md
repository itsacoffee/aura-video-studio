# AI Content Verification and Fact Checking Guide

## Overview

The AI Content Verification system ensures factual accuracy and prevents misinformation in AI-generated content. This comprehensive system includes fact-checking, source attribution, confidence analysis, and misinformation detection.

## Architecture

### Backend Services

#### 1. FactCheckingService
Validates factual claims using evidence gathering and credibility scoring.

**Key Features:**
- Multi-source evidence gathering
- Claim type classification (Scientific, Historical, Statistical, etc.)
- Verification status determination
- Confidence scoring based on evidence quality

**Usage Example:**
```csharp
var service = new FactCheckingService(logger);
var claim = new Claim(
    ClaimId: "claim-1",
    Text: "The Earth orbits the Sun",
    Context: "Solar system facts",
    StartPosition: 0,
    EndPosition: 100,
    Type: ClaimType.Scientific,
    ExtractionConfidence: 0.9
);

var result = await service.CheckClaimAsync(claim, ct);
// result.Status: Verified, PartiallyVerified, Unverified, Disputed, False
// result.ConfidenceScore: 0.0 to 1.0
```

#### 2. SourceAttributionService
Generates proper citations and manages source attribution.

**Supported Citation Formats:**
- APA (American Psychological Association)
- MLA (Modern Language Association)
- Chicago
- Harvard

**Usage Example:**
```csharp
var service = new SourceAttributionService(logger);
var sources = new List<SourceAttribution> { /* ... */ };

// Generate citations
var citations = await service.GenerateCitationsAsync(
    sources, 
    CitationFormat.APA, 
    ct
);

// Validate source credibility
var validation = await service.ValidateSourceAsync(source, ct);
```

#### 3. ConfidenceAnalysisService
Analyzes confidence in content assertions with detailed scoring.

**Features:**
- Overall confidence calculation
- Per-claim confidence tracking
- High/low confidence claim identification
- Review recommendations

**Usage Example:**
```csharp
var service = new ConfidenceAnalysisService(logger);
var analysis = await service.AnalyzeConfidenceAsync(
    contentId,
    claims,
    factChecks,
    ct
);

// Generate confidence report
var report = service.GenerateConfidenceReport(analysis);
```

#### 4. MisinformationDetectionService
Detects potential misinformation patterns and logical fallacies.

**Detection Capabilities:**
- Absolute language patterns
- Sensationalist language
- Missing source attribution
- Correlation/causation confusion
- Appeals to emotion
- Logical fallacies

**Usage Example:**
```csharp
var service = new MisinformationDetectionService(logger);
var detection = await service.DetectMisinformationAsync(
    contentId,
    content,
    claims,
    factChecks,
    ct
);

// Check risk level: Low, Medium, High, Critical
var riskLevel = detection.RiskLevel;
```

#### 5. ContentVerificationOrchestrator
Coordinates the complete verification workflow.

**Workflow:**
1. Extract claims from content
2. Fact-check claims
3. Analyze confidence
4. Detect misinformation
5. Collect and validate sources
6. Generate overall status and warnings

**Usage Example:**
```csharp
var orchestrator = new ContentVerificationOrchestrator(
    logger,
    factCheckingService,
    sourceService,
    confidenceService,
    misinfoService
);

var request = new VerificationRequest(
    ContentId: "content-1",
    Content: "Your content here...",
    Options: new VerificationOptions(
        CheckFacts: true,
        DetectMisinformation: true,
        AnalyzeConfidence: true,
        AttributeSources: true,
        MaxClaimsToCheck: 50,
        MinConfidenceThreshold: 0.5
    )
);

var result = await orchestrator.VerifyContentAsync(request, ct);
```

#### 6. VerificationPersistence
Stores verification results and maintains history.

**Features:**
- JSON-based file storage
- History tracking with timestamps
- Statistics aggregation
- Atomic file operations

**Usage Example:**
```csharp
var persistence = new VerificationPersistence(logger, dataDirectory);

// Save result
await persistence.SaveVerificationResultAsync(result, ct);

// Load result
var loaded = await persistence.LoadVerificationResultAsync(contentId, ct);

// Get history
var history = await persistence.LoadVerificationHistoryAsync(contentId, 10, ct);

// Get statistics
var stats = await persistence.GetStatisticsAsync(ct);
```

## API Endpoints

### POST /api/verification/verify
Perform complete content verification.

**Request:**
```json
{
  "contentId": "optional-id",
  "content": "Content to verify...",
  "options": {
    "checkFacts": true,
    "detectMisinformation": true,
    "analyzeConfidence": true,
    "attributeSources": true,
    "maxClaimsToCheck": 50,
    "minConfidenceThreshold": 0.5
  }
}
```

**Response:**
```json
{
  "success": true,
  "result": {
    "contentId": "content-123",
    "overallStatus": "Verified",
    "overallConfidence": 0.85,
    "claimCount": 5,
    "factCheckCount": 5,
    "sourceCount": 3,
    "warnings": [],
    "misinformationRisk": "Low",
    "verifiedAt": "2024-10-24T01:00:00Z"
  },
  "details": { /* Full verification result */ }
}
```

### POST /api/verification/quick-verify
Quick verification for real-time feedback (checks top 5 claims only).

**Request:**
```json
{
  "content": "Content to verify..."
}
```

**Response:**
```json
{
  "success": true,
  "result": {
    "claimCount": 7,
    "checkedCount": 5,
    "averageConfidence": 0.75,
    "hasIssues": false,
    "topIssues": []
  }
}
```

### GET /api/verification/{contentId}
Retrieve verification result by content ID.

### GET /api/verification/{contentId}/history
Get verification history for content.

**Query Parameters:**
- `maxResults` (default: 10): Maximum number of history entries

### POST /api/verification/citations
Generate citations for sources.

**Request:**
```json
{
  "sources": [
    {
      "sourceId": "s1",
      "name": "Scientific Journal",
      "url": "https://example.com/article",
      "type": "AcademicJournal",
      "credibilityScore": 0.9,
      "publishedDate": "2024-01-15T00:00:00Z",
      "author": "Dr. Smith"
    }
  ],
  "format": "APA"
}
```

### GET /api/verification/statistics
Get verification statistics.

### DELETE /api/verification/{contentId}
Delete verification result.

### GET /api/verification/list
List all verified content IDs.

## Frontend Components

### 1. FactCheckPanel
Main verification interface for checking content.

**Usage:**
```tsx
import { FactCheckPanel } from '@/components/verification';

<FactCheckPanel
  content={contentToVerify}
  onVerify={(result) => console.log('Verification complete:', result)}
/>
```

**Features:**
- Real-time verification
- Visual status indicators
- Warnings display
- Fact check details
- Summary statistics

### 2. ConfidenceMeter
Visual confidence indicator with color coding.

**Usage:**
```tsx
import { ConfidenceMeter } from '@/components/verification';

<ConfidenceMeter
  confidence={0.85}
  label="Overall Confidence"
  showIcon={true}
  size="md"
/>
```

**Props:**
- `confidence`: 0.0 to 1.0
- `label`: Display label (default: "Confidence")
- `showIcon`: Show trend icon (default: true)
- `size`: 'sm' | 'md' | 'lg' (default: 'md')

### 3. VerificationResultsView
Comprehensive view of verification results.

**Usage:**
```tsx
import { VerificationResultsView } from '@/components/verification';

<VerificationResultsView result={verificationResult} />
```

**Displays:**
- Overall status and confidence
- Warnings
- Misinformation analysis
- Source list with credibility scores
- Risk level indicators

### 4. SourceCitationEditor
Citation management UI for adding and editing sources.

**Usage:**
```tsx
import { SourceCitationEditor } from '@/components/verification';

<SourceCitationEditor
  sources={sources}
  onSourcesChange={(updated) => setSources(updated)}
/>
```

**Features:**
- Add/remove sources
- Edit source details
- Generate citations in multiple formats
- Copy citations to clipboard
- External link preview

### 5. ContentWarningManager
Manages and displays content warnings.

**Usage:**
```tsx
import { ContentWarningManager } from '@/components/verification';

<ContentWarningManager
  warnings={warnings}
  riskLevel="Medium"
  onDismiss={(id) => console.log('Dismissed:', id)}
  showTimestamp={true}
/>
```

**Features:**
- Color-coded warning types (error, warning, info)
- Dismissible warnings
- Risk level badges
- Summary statistics

## Data Models

### Claim
```csharp
public record Claim(
    string ClaimId,
    string Text,
    string Context,
    int StartPosition,
    int EndPosition,
    ClaimType Type,
    double ExtractionConfidence
);

public enum ClaimType
{
    Factual,
    Statistical,
    Historical,
    Scientific,
    Opinion,
    Prediction
}
```

### FactCheckResult
```csharp
public record FactCheckResult(
    string ClaimId,
    string Claim,
    VerificationStatus Status,
    double ConfidenceScore,
    List<Evidence> Evidence,
    string? Explanation,
    DateTime VerifiedAt
);

public enum VerificationStatus
{
    Verified,
    PartiallyVerified,
    Unverified,
    Disputed,
    False,
    Unknown
}
```

### SourceAttribution
```csharp
public record SourceAttribution(
    string SourceId,
    string Name,
    string Url,
    SourceType Type,
    double CredibilityScore,
    DateTime? PublishedDate,
    string? Author
);

public enum SourceType
{
    AcademicJournal,
    NewsOrganization,
    Government,
    Wikipedia,
    Expert,
    Organization,
    Other
}
```

### MisinformationDetection
```csharp
public record MisinformationDetection(
    string ContentId,
    List<MisinformationFlag> Flags,
    double RiskScore,
    MisinformationRiskLevel RiskLevel,
    List<string> Recommendations,
    DateTime DetectedAt
);

public enum MisinformationRiskLevel
{
    Low,
    Medium,
    High,
    Critical
}
```

## Testing

### Running Tests
```bash
dotnet test Aura.Tests/Aura.Tests.csproj --filter "FullyQualifiedName~ContentVerificationTests"
```

### Test Coverage
- FactCheckingService claim checking
- ConfidenceAnalysisService analysis
- MisinformationDetectionService detection
- ContentVerificationOrchestrator workflow
- SourceAttributionService citations and deduplication
- Quick verification functionality

## Best Practices

### 1. Content Verification
- Always verify content before publication
- Use quick-verify for real-time feedback during editing
- Review warnings and address critical issues
- Maintain verification history for audit trails

### 2. Source Attribution
- Add sources for all factual claims
- Use appropriate citation format for your audience
- Validate source credibility before adding
- Include publication dates when available

### 3. Confidence Analysis
- Set appropriate confidence thresholds based on content type
- Review low-confidence claims before publishing
- Use confidence scores to prioritize review efforts
- Document reasoning for accepting low-confidence content

### 4. Misinformation Detection
- Take all flags seriously, even if confidence is high
- Review suggested corrections
- Avoid absolute language and sensationalism
- Always provide sources for controversial claims

## Future Enhancements

1. **External API Integration**
   - Google Fact Check Tools API
   - Snopes API
   - PolitiFact API
   - Wikipedia/Wikidata APIs

2. **Advanced ML Models**
   - Deep learning-based claim extraction
   - Semantic similarity for evidence matching
   - Neural fact verification models
   - Context-aware misinformation detection

3. **Real-time Features**
   - Live verification during content creation
   - Inline suggestions and corrections
   - Collaborative fact-checking
   - Browser extension for web content

4. **Enhanced Analytics**
   - Verification trends over time
   - Source credibility tracking
   - Team performance metrics
   - Comparative analysis

## Support

For issues, questions, or feature requests, please refer to the main repository documentation or create an issue on GitHub.
