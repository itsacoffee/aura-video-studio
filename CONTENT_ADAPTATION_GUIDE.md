# Content Adaptation Engine - User Guide

## Overview

The Content Adaptation Engine automatically adjusts video scripts to perfectly match your target audience's characteristics. It analyzes audience profiles and adapts content across 8+ dimensions including vocabulary, examples, pacing, tone, formality, complexity, density, and cultural references.

## Key Features

### 🎯 Automatic Adaptation Across 8+ Dimensions

1. **Vocabulary Level Adjustment**
   - Analyzes complexity using Flesch-Kincaid and SMOG readability scores
   - Adjusts to match audience education level (8th grade to professional)
   - Replaces jargon for general audiences OR embraces technical terms for experts
   - Adds definitions when appropriate

2. **Example & Analogy Personalization**
   - Tech audience → programming analogies
   - Parents → parenting scenarios
   - Students → academic examples
   - 80%+ relevance score for audience-specific examples
   - 3-5 examples per key concept for better retention

3. **Pacing & Information Density**
   - Beginner audiences: 20-30% longer content with more explanation
   - Expert audiences: 20-25% shorter with dense information
   - Adjusts scene durations based on attention span
   - Adds/removes explanatory content based on expertise

4. **Tone & Formality Optimization**
   - Casual for young audiences
   - Professional for business contexts
   - Academic for research-oriented content
   - Adjusts humor level and type
   - Ensures cultural appropriateness

5. **Cognitive Load Balancing**
   - Tracks mental effort per scene (0-100 scale)
   - Ensures complexity matches audience capabilities
   - Inserts "breather" moments for complex content
   - Balances abstract concepts with concrete examples

6. **Cultural Sensitivity**
   - Adapts references for geographic regions
   - Respects cultural sensitivities and taboo topics
   - Adjusts communication style (direct vs. indirect)

7. **Accessibility Support**
   - Simplified language when required
   - Shorter sentences for accessibility needs
   - Clear structure and active voice

8. **Energy & Engagement**
   - High energy for young audiences
   - Measured pace for seniors
   - Appropriate enthusiasm level

## Quick Start

### 1. Create an Audience Profile

```http
POST /api/audience/profiles
Content-Type: application/json

{
  "name": "Tech Professionals",
  "ageRange": {
    "minAge": 25,
    "maxAge": 45,
    "displayName": "Working Professionals"
  },
  "educationLevel": "BachelorDegree",
  "expertiseLevel": "Advanced",
  "profession": "Software Developer",
  "industry": "Technology",
  "technicalComfort": "TechSavvy",
  "interests": ["Programming", "AI", "Cloud Computing"],
  "painPoints": ["Keeping up with rapid tech changes"],
  "motivations": ["Career advancement", "Learning new skills"]
}
```

### 2. Adapt Content

```http
POST /api/audience/adapt
Content-Type: application/json

{
  "content": "Machine learning algorithms process data to identify patterns...",
  "audienceProfileId": "profile-id-here",
  "config": {
    "aggressivenessLevel": 0.6,
    "enableVocabularyAdjustment": true,
    "enableExamplePersonalization": true,
    "enablePacingAdaptation": true,
    "enableToneOptimization": true,
    "enableCognitiveLoadBalancing": true,
    "cognitiveLoadThreshold": 75.0,
    "examplesPerConcept": 3
  }
}
```

### 3. Get Preview with Comparison

```http
POST /api/audience/adapt/preview
Content-Type: application/json

{
  "content": "Your original script...",
  "audienceProfileId": "profile-id-here"
}
```

## Configuration Options

### Aggressiveness Level

Controls how dramatically the content is adapted:

- **0.3 (Subtle)**: Minor adjustments, preserves original style closely
- **0.6 (Moderate)**: Balanced adaptation (default)
- **0.9 (Aggressive)**: Maximum adaptation to match audience

### Feature Toggles

Enable or disable specific adaptation features:

```json
{
  "enableVocabularyAdjustment": true,
  "enableExamplePersonalization": true,
  "enablePacingAdaptation": true,
  "enableToneOptimization": true,
  "enableCognitiveLoadBalancing": true
}
```

### Cognitive Load Threshold

Maximum acceptable cognitive load (0-100):

- **50-60**: Conservative, suitable for beginners
- **70-75**: Moderate (default)
- **80-90**: Aggressive, for advanced audiences

### Examples Per Concept

Number of examples to include for each key concept:

- **3**: Minimum for retention
- **4**: Good balance (recommended)
- **5**: Maximum depth

## Reading Level Targets

The engine automatically determines target reading levels based on audience education:

| Education Level | Target Grade Level | Description |
|----------------|-------------------|-------------|
| High School | 9-10 | High School Freshman/Sophomore |
| Some College | 11-12 | High School Junior/Senior |
| Associate Degree | 12-13 | College Freshman |
| Bachelor's Degree | 13-14 | Undergraduate |
| Master's Degree | 14-15 | Graduate |
| Doctorate | 16+ | Advanced Academic/Professional |

Expertise level further adjusts these targets:
- Complete Beginner: -2 grade levels
- Expert/Professional: +2-3 grade levels

## Audience Profile Examples

### Beginner Tech Learners

```json
{
  "name": "Tech Beginners",
  "expertiseLevel": "CompleteBeginner",
  "educationLevel": "HighSchool",
  "technicalComfort": "NonTechnical",
  "attentionSpan": {
    "preferredDuration": "00:03:00",
    "displayName": "Medium (3-10 min)"
  }
}
```

**Expected Adaptations:**
- Reading level: Grade 7-8
- Pacing: 30% longer content
- Vocabulary: Simple, jargon-free
- Examples: Everyday analogies
- Tone: Very casual and encouraging

### Business Executives

```json
{
  "name": "C-Suite Executives",
  "ageRange": { "minAge": 40, "maxAge": 60 },
  "educationLevel": "MasterDegree",
  "expertiseLevel": "Professional",
  "profession": "Executive",
  "industry": "Business"
}
```

**Expected Adaptations:**
- Reading level: Grade 15-16
- Pacing: Standard, efficient
- Vocabulary: Business terminology
- Examples: ROI, strategic planning
- Tone: Professional, authoritative

### Academic Researchers

```json
{
  "name": "PhD Researchers",
  "educationLevel": "Doctorate",
  "expertiseLevel": "Expert",
  "profession": "Researcher",
  "preferredLearningStyle": "ReadingWriting"
}
```

**Expected Adaptations:**
- Reading level: Grade 16+
- Pacing: 25% shorter, information-dense
- Vocabulary: Technical jargon encouraged
- Examples: Research methodologies
- Tone: Academic, precise

## Understanding the Results

### Readability Metrics

```json
{
  "fleschKincaidGradeLevel": 12.3,
  "smogScore": 11.8,
  "averageWordsPerSentence": 18.5,
  "averageSyllablesPerWord": 1.7,
  "complexWordPercentage": 15.2,
  "technicalTermDensity": 12.1,
  "overallComplexity": 58.4
}
```

- **Flesch-Kincaid Grade Level**: Target school grade (lower = easier)
- **SMOG Score**: Simple Measure of Gobbledygook (lower = simpler)
- **Complex Word %**: Words with 3+ syllables
- **Overall Complexity**: Composite score (0-100)

### Adaptation Changes

Each change includes:
- **Category**: Vocabulary, Example, Pacing, Tone, CognitiveLoad
- **Description**: What changed
- **OriginalText**: Before adaptation
- **AdaptedText**: After adaptation
- **Reasoning**: Why the change was made

Example:
```json
{
  "category": "Vocabulary",
  "description": "Replaced 'utilize' with 'use'",
  "originalText": "utilize",
  "adaptedText": "use",
  "reasoning": "Simplified vocabulary to match target reading level",
  "position": 42
}
```

## Performance Expectations

### Target Performance

- **5-minute script**: < 15 seconds processing time
- **1-minute script**: < 3 seconds
- **10-minute script**: < 30 seconds

### Actual Performance Factors

Processing time depends on:
- Script length (word count)
- Number of enabled features
- Aggressiveness level
- LLM provider speed
- Network latency (for cloud LLMs)

### Optimization Tips

1. **Disable unnecessary features** for faster processing
2. **Use local LLMs** (Ollama) for zero latency
3. **Cache common adaptations** for repeated patterns
4. **Process in chunks** for very long scripts

## Integration with Video Pipeline

### Automatic Integration

The adaptation engine integrates seamlessly with the VideoOrchestrator:

1. **Brief Stage**: Audience profile is attached to Brief
2. **Script Generation**: EnhancedPromptTemplates automatically inject audience context
3. **Post-Generation**: ContentAdaptationEngine refines the script
4. **Voice Selection**: Tone matching influences voice choice
5. **Visual Generation**: Example preferences guide visual style

### Manual Workflow

```typescript
// 1. Create audience profile
const profile = await createAudienceProfile({
  name: "My Target Audience",
  // ... profile details
});

// 2. Generate script with profile
const script = await generateScript({
  topic: "Introduction to AI",
  audienceProfileId: profile.id,
  // ... other params
});

// 3. Adapt the script (optional additional refinement)
const adapted = await adaptContent({
  content: script.content,
  audienceProfileId: profile.id,
  config: {
    aggressivenessLevel: 0.8
  }
});

// 4. Continue with video generation
const video = await generateVideo({
  script: adapted.adaptedContent,
  // ... other params
});
```

## Best Practices

### 1. Profile Creation

✅ **Do:**
- Be specific about expertise level
- Include relevant interests
- Specify pain points and motivations
- Set realistic attention span

❌ **Don't:**
- Use generic profiles
- Ignore cultural sensitivities
- Overlook accessibility needs

### 2. Configuration

✅ **Do:**
- Start with moderate aggressiveness (0.6)
- Enable all features for best results
- Review preview before finalizing
- Adjust based on actual results

❌ **Don't:**
- Use maximum aggressiveness (0.9) without review
- Disable features without understanding impact
- Apply same config to all audiences

### 3. Iteration

✅ **Do:**
- Use preview to compare before/after
- Test with representative audience members
- Refine profile based on feedback
- A/B test different configurations

❌ **Don't:**
- Accept first adaptation blindly
- Skip validation with real users
- Ignore metrics comparison

## Troubleshooting

### Issue: Adaptation Too Aggressive

**Solution:** Lower aggressiveness level to 0.3-0.4

### Issue: Not Enough Change

**Solution:** 
- Increase aggressiveness to 0.7-0.8
- Check all features are enabled
- Verify audience profile has detailed characteristics

### Issue: Processing Takes Too Long

**Solution:**
- Disable unnecessary features
- Use local LLM provider (Ollama)
- Reduce examplesPerConcept to 3
- Process shorter content chunks

### Issue: Examples Not Relevant

**Solution:**
- Add more interests to profile
- Specify profession and industry
- Include pain points
- Check geographic region setting

### Issue: Tone Mismatch

**Solution:**
- Set communication style explicitly
- Verify age range is correct
- Check formality expectations
- Review cultural background settings

## API Reference

### POST /api/audience/adapt

Adapt content for an audience profile.

**Request Body:**
```typescript
interface AdaptContentRequest {
  content: string;
  audienceProfileId: string;
  config?: ContentAdaptationConfigDto;
}
```

**Response:**
```typescript
interface ContentAdaptationResultDto {
  originalContent: string;
  adaptedContent: string;
  originalMetrics: ReadabilityMetricsDto;
  adaptedMetrics: ReadabilityMetricsDto;
  changes: AdaptationChangeDto[];
  overallRelevanceScore: number;
  processingTimeSeconds: number;
}
```

### POST /api/audience/adapt/preview

Get detailed comparison with before/after analysis.

**Request Body:**
```typescript
interface AdaptationPreviewRequest {
  content: string;
  audienceProfileId: string;
  config?: ContentAdaptationConfigDto;
}
```

**Response:**
```typescript
interface AdaptationComparisonReportDto {
  originalContent: string;
  adaptedContent: string;
  processingTimeSeconds: number;
  overallRelevanceScore: number;
  sections: ComparisonSectionDto[];
  metricsComparison: MetricsComparisonDto;
  changesByCategory: Record<string, number>;
  summary: string;
}
```

### GET /api/audience/profiles/{id}/reading-level

Get target reading level for an audience profile.

**Response:**
```typescript
interface ReadingLevelResponse {
  profileId: string;
  profileName: string;
  readingLevelDescription: string;
}
```

## Examples

### Complete Workflow Example

```bash
# 1. Create audience profile
curl -X POST http://localhost:5005/api/audience/profiles \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Software Engineers",
    "expertiseLevel": "Advanced",
    "profession": "Software Developer",
    "interests": ["Programming", "Architecture"]
  }'

# 2. Adapt content
curl -X POST http://localhost:5005/api/audience/adapt \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Let me explain how algorithms work...",
    "audienceProfileId": "profile-123",
    "config": {
      "aggressivenessLevel": 0.7,
      "examplesPerConcept": 4
    }
  }'

# 3. Get preview comparison
curl -X POST http://localhost:5005/api/audience/adapt/preview \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Let me explain how algorithms work...",
    "audienceProfileId": "profile-123"
  }'
```

## Conclusion

The Content Adaptation Engine provides powerful, automatic content optimization based on deep audience understanding. By leveraging readability metrics, LLM analysis, and comprehensive audience profiles, it ensures your video content resonates perfectly with your target audience.

For questions or feedback, please refer to the main project documentation or create an issue on GitHub.
