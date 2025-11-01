# Document Import and Conversion System - User Guide

## Overview

The Document Import and Conversion System enables you to transform existing documents into video-optimized scripts. Import content from multiple formats and automatically adapt it for video production with intelligent restructuring, audience re-targeting, and visual suggestions.

## Supported Document Formats

The system supports **8+ document formats**:

1. **Plain Text** (`.txt`) - Simple text documents
2. **Markdown** (`.md`, `.markdown`) - Structured markdown with headings and formatting
3. **HTML** (`.html`, `.htm`) - Web pages and articles
4. **JSON** (`.json`) - Structured content and Aura scripts
5. **Microsoft Word** (`.docx`, `.doc`) - Office documents (basic support)
6. **PDF** (`.pdf`) - PDF documents (basic support)
7. **Google Docs** - Via export to supported formats
8. **Aura Scripts** - Re-import and adapt existing scripts

## Quick Start

### 1. Import a Document

```bash
curl -X POST http://localhost:5005/api/content/import \
  -H "Content-Type: multipart/form-data" \
  -F "file=@article.md"
```

**Response:**
```json
{
  "success": true,
  "metadata": {
    "originalFileName": "article.md",
    "format": "Markdown",
    "wordCount": 1250,
    "detectedLanguage": "en",
    "title": "Introduction to Machine Learning"
  },
  "structure": {
    "sections": [...],
    "headingLevels": 3,
    "keyConcepts": ["machine learning", "algorithms", "training"],
    "complexity": {
      "readingLevel": 12.5,
      "complexityDescription": "College"
    }
  },
  "inferredAudience": {
    "educationLevel": "College",
    "expertiseLevel": "Intermediate",
    "confidenceScore": 0.85
  }
}
```

### 2. Convert to Video Script

```bash
curl -X POST http://localhost:5005/api/content/convert \
  -H "Content-Type: application/json" \
  -d '{
    "originalFileName": "article.md",
    "format": "Markdown",
    "wordCount": 1250,
    "sections": [...],
    "preset": "BlogToYouTube",
    "targetDurationMinutes": 8,
    "wordsPerMinute": 160
  }'
```

**Response:**
```json
{
  "success": true,
  "scenes": [
    {
      "index": 0,
      "heading": "Hook: What is Machine Learning?",
      "script": "Have you ever wondered how Netflix knows exactly what you want to watch next?...",
      "startSeconds": 0,
      "durationSeconds": 15.2
    }
  ],
  "metrics": {
    "originalWordCount": 1250,
    "convertedWordCount": 1100,
    "compressionRatio": 0.88,
    "overallConfidenceScore": 0.87
  }
}
```

### 3. Get Available Presets

```bash
curl http://localhost:5005/api/content/presets
```

## Conversion Presets

The system includes **6 predefined presets** for common conversion scenarios:

### 1. Generic Document
- **Best for:** General-purpose documents
- **Duration:** 3 minutes
- **Strategy:** Balanced conversion with standard video structure

### 2. Blog Post → YouTube Video
- **Best for:** Blog articles, web content (`.md`, `.html`)
- **Duration:** 8 minutes
- **Strategy:** Strong hook, conversational tone, clear call-to-action
- **Features:** High engagement optimization, YouTube-specific formatting

### 3. Technical Doc → Explainer Video
- **Best for:** Technical documentation (`.md`, `.pdf`, `.docx`)
- **Duration:** 5 minutes
- **Strategy:** Simplify jargon, add analogies, step-by-step explanations
- **Features:** Heavy visual support, beginner-friendly language

### 4. Academic Paper → Educational Video
- **Best for:** Research papers, academic content (`.pdf`, `.docx`)
- **Duration:** 10 minutes
- **Strategy:** Maintain structure, simplify abstract concepts, preserve key citations
- **Features:** Structure preservation, visual aids for complex concepts

### 5. News Article → News Segment
- **Best for:** News articles, press releases (`.html`, `.txt`)
- **Duration:** 2 minutes
- **Strategy:** Inverted pyramid, key facts first, concise delivery
- **Features:** Fast-paced, B-roll opportunities

### 6. Tutorial Guide → How-To Video
- **Best for:** Step-by-step guides (`.md`, `.html`, `.txt`)
- **Duration:** 6 minutes
- **Strategy:** Clear numbered steps, prerequisites upfront, tips as callouts
- **Features:** Structure preservation, demo visuals

## Configuration Options

### ConversionConfig Parameters

- **preset**: Which conversion preset to use (required)
- **targetDurationMinutes**: Target video length (default: varies by preset)
- **wordsPerMinute**: Speech rate (default: 150, range: 120-180)
- **enableAudienceRetargeting**: Adapt for specific audience (default: true)
- **enableVisualSuggestions**: Generate visual cues (default: true)
- **preserveOriginalStructure**: Keep document structure (default: false)
- **addTransitions**: Insert scene transitions (default: true)
- **aggressivenessLevel**: How much to modify content (0.0-1.0, default: 0.6)
- **targetAudienceProfileId**: ID of audience profile for re-targeting (optional)

## Intelligent Features

### 1. Document Analysis

The system automatically analyzes:
- **Structure:** Headings, sections, paragraphs
- **Complexity:** Reading level (Flesch-Kincaid), technical density
- **Tone:** Formality, writing style
- **Audience:** Education level, expertise, likely professions
- **Key Concepts:** Most important terms and ideas

### 2. Content Restructuring

Transforms written content for video:
- **Hook → Body → Conclusion:** Standard video structure
- **Written → Spoken:** Natural speech patterns
- **Long → Concise:** Compressed for video timing
- **Expand Bullets:** Full narration from bullet points
- **Remove Footnotes:** Adapt or remove written-only elements

### 3. Audience Re-targeting

Leverages the ContentAdaptationEngine (PR #17):
- **Vocabulary Adjustment:** Match education level
- **Example Personalization:** Audience-relevant examples
- **Pacing Adaptation:** Adjust speed for expertise
- **Tone Optimization:** Match audience expectations
- **Cognitive Load:** Balance complexity with capacity

### 4. Visual Suggestions

Identifies opportunities for visuals:
- **[VISUAL: description]** tags in script
- B-roll suggestions from context
- Image references preserved from source
- Diagram and chart opportunities

### 5. Change Tracking

Every modification is documented:
- **Category:** Structure, Language, Flow, etc.
- **Description:** What was changed
- **Justification:** Why it was changed
- **Impact Level:** 0.0-1.0 significance
- **Confidence Score:** Per-section reliability

## Performance

- **Speed:** 5000-word document converts in < 30 seconds
- **Accuracy:** >90% key concept extraction
- **Success Rate:** >95% document parsing success
- **Metadata Accuracy:** >80% for standard documents

## File Limits

- **Max File Size:** 10MB (configurable)
- **Max Word Count:** 50,000 words (configurable)
- **Recommended Range:** 500-5000 words for best results

## Advanced Usage

### Custom Presets

Create your own conversion strategy:

```javascript
{
  "preset": "Custom",
  "targetDurationMinutes": 7,
  "wordsPerMinute": 145,
  "enableAudienceRetargeting": true,
  "enableVisualSuggestions": true,
  "preserveOriginalStructure": false,
  "addTransitions": true,
  "aggressivenessLevel": 0.75,
  "targetAudienceProfileId": "my-custom-profile"
}
```

### Audience Re-targeting

Combine with audience profiles from PR #16:

```bash
# Import document
IMPORT_RESULT=$(curl -X POST .../import -F "file=@tech-doc.md")

# Convert with audience re-targeting
curl -X POST http://localhost:5005/api/content/convert \
  -H "Content-Type: application/json" \
  -d '{
    ...[import result]...,
    "preset": "TechnicalToExplainer",
    "enableAudienceRetargeting": true,
    "targetAudienceProfileId": "beginners-tech-2024",
    "aggressivenessLevel": 0.8
  }'
```

### Section-by-Section Review

Use the `sectionConversions` field to review changes:

```json
{
  "sectionConversions": [
    {
      "sectionIndex": 0,
      "originalHeading": "Introduction to Neural Networks",
      "convertedHeading": "What Are Neural Networks?",
      "originalContent": "Neural networks are computational models...",
      "convertedContent": "Imagine your brain, but inside a computer...",
      "confidenceScore": 0.92,
      "requiresManualReview": false,
      "changeHighlights": [
        "Simplified technical terminology",
        "Added relatable analogy",
        "Converted to spoken language"
      ],
      "reasoning": "Simplified for beginner audience while maintaining accuracy"
    }
  ]
}
```

## Workflow Integration

### Typical Workflow

1. **Import Document** → Parse and analyze
2. **Review Analysis** → Check structure, complexity, audience
3. **Select Preset** → Choose conversion strategy
4. **Configure** → Adjust duration, pacing, audience
5. **Convert** → Generate video script
6. **Review Changes** → Examine section conversions
7. **Refine** → Manually edit if needed
8. **Generate Video** → Use script in video pipeline

### Integration with VideoOrchestrator

The converted script can be fed directly into the video generation pipeline:

```javascript
// 1. Import and convert document
const importResult = await importDocument(file);
const conversionResult = await convertDocument(importResult, config);

// 2. Use suggested brief
const brief = conversionResult.suggestedBrief;

// 3. Generate video with converted scenes
const video = await orchestrator.generateVideo({
  brief: brief,
  scenes: conversionResult.scenes,
  // ... other video settings
});
```

## Tips and Best Practices

### Document Preparation

1. **Clear Structure:** Use headings and sections
2. **Concise Content:** Aim for 1000-3000 words
3. **Remove Clutter:** Delete navigation, footers, ads
4. **Check Formatting:** Ensure proper markdown/HTML

### Preset Selection

- **Blog posts:** Use BlogToYouTube for engagement
- **Technical docs:** Use TechnicalToExplainer for simplification
- **Academic papers:** Use AcademicToEducational to preserve rigor
- **News articles:** Use NewsToSegment for brevity
- **Tutorials:** Use TutorialToHowTo for step-by-step format

### Configuration Tuning

- **Shorter videos:** Increase wordsPerMinute (160-180)
- **Longer videos:** Decrease wordsPerMinute (130-145)
- **Major changes:** Increase aggressivenessLevel (0.7-0.9)
- **Minor tweaks:** Decrease aggressivenessLevel (0.3-0.5)

### Manual Review

Always review sections with:
- `requiresManualReview: true`
- `confidenceScore < 0.7`
- Technical accuracy concerns
- Brand voice requirements

## Troubleshooting

### Import Failures

**Problem:** "Unsupported file format"
- **Solution:** Convert to `.txt`, `.md`, or `.html`

**Problem:** "File size exceeds limit"
- **Solution:** Split into multiple documents or compress

**Problem:** "Parsing failed"
- **Solution:** Check file encoding (use UTF-8), remove special characters

### Conversion Issues

**Problem:** Too much compression
- **Solution:** Increase targetDuration or decrease aggressivenessLevel

**Problem:** Structure lost
- **Solution:** Set `preserveOriginalStructure: true`

**Problem:** Tone mismatch
- **Solution:** Adjust audience profile or preset selection

## API Reference

### POST /api/content/import

Upload and parse a document.

**Request:** `multipart/form-data`
- `file`: Document file to import

**Response:** `DocumentImportResponse`
- 200: Success with metadata and structure
- 400: Invalid file or parsing error
- 413: File too large
- 500: Server error

### POST /api/content/convert

Convert imported document to video script.

**Request:** `ConvertDocumentRequest`
- Import result data
- Conversion configuration

**Response:** `ConversionResultDto`
- 200: Success with scenes and metrics
- 400: Conversion failed
- 500: Server error

### GET /api/content/presets

Get available conversion presets.

**Response:** List of `PresetDefinitionDto`
- 200: Success with preset list
- 500: Server error

## Examples

### Example 1: Blog Post Conversion

```bash
# Import markdown blog post
curl -X POST http://localhost:5005/api/content/import \
  -F "file=@my-blog-post.md" \
  > import-result.json

# Convert to 8-minute YouTube video
curl -X POST http://localhost:5005/api/content/convert \
  -H "Content-Type: application/json" \
  -d @convert-request.json \
  > conversion-result.json
```

### Example 2: Technical Documentation

```bash
# Import technical PDF (converted to text)
curl -X POST http://localhost:5005/api/content/import \
  -F "file=@api-docs.txt" \
  > import-result.json

# Convert to explainer video for beginners
curl -X POST http://localhost:5005/api/content/convert \
  -H "Content-Type: application/json" \
  -d '{
    ...[import result]...,
    "preset": "TechnicalToExplainer",
    "targetDurationMinutes": 6,
    "wordsPerMinute": 140,
    "enableAudienceRetargeting": true,
    "targetAudienceProfileId": "tech-beginners",
    "aggressivenessLevel": 0.85
  }'
```

### Example 3: News Article

```bash
# Import HTML news article
curl -X POST http://localhost:5005/api/content/import \
  -F "file=@news-article.html" \
  > import-result.json

# Convert to 2-minute news segment
curl -X POST http://localhost:5005/api/content/convert \
  -H "Content-Type: application/json" \
  -d '{
    ...[import result]...,
    "preset": "NewsToSegment",
    "targetDurationMinutes": 2,
    "wordsPerMinute": 170,
    "addTransitions": false
  }'
```

## Further Reading

- **PR #17:** Content Adaptation Engine - Audience re-targeting system
- **PR #16:** Audience Profile System - Rich audience modeling
- **CONTENT_ADAPTATION_GUIDE.md** - Deep dive into adaptation features

## Support

For issues, feature requests, or questions:
- GitHub Issues: https://github.com/itsacoffee/aura-video-studio/issues
- Documentation: Check CONTENT_ADAPTATION_GUIDE.md and other guides
