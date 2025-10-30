# Script Refinement Pipeline Guide

## Overview

The Script Refinement Pipeline implements a multi-stage "Draft-Critique-Revise" pattern that iteratively improves script quality through self-critique and targeted revisions. This feature ensures generated scripts meet high quality standards before production.

## Architecture

### Core Components

1. **ScriptRefinementOrchestrator** (`Aura.Core/Services/ScriptRefinementOrchestrator.cs`)
   - Orchestrates the complete refinement workflow
   - Manages iteration cycles and quality tracking
   - Integrates with IntelligentContentAdvisor for validation

2. **ScriptQualityMetrics** (`Aura.Core/Models/ScriptQualityMetrics.cs`)
   - 5-dimensional quality scoring (0-100 scale):
     - Narrative Coherence: Logical flow and story structure
     - Pacing Appropriateness: Rhythm and timing suitability
     - Audience Alignment: Content matches target audience
     - Visual Clarity: Script supports visual storytelling
     - Engagement Potential: Predicted viewer engagement
   - Overall score: Weighted average of all dimensions

3. **ScriptRefinementConfig** (`Aura.Core/Models/ScriptQualityMetrics.cs`)
   - Configurable refinement parameters
   - Early stopping conditions
   - Integration settings

## Refinement Workflow

### Stage 1: Initial Draft Generation
- LLM generates first version of script based on Brief and PlanSpec
- No refinement applied yet

### Stage 2: Quality Assessment
- Script is evaluated across 5 quality dimensions
- Structured feedback is extracted
- Baseline metrics established

### Stage 3: Iterative Refinement (1-3 passes)

For each refinement pass:

#### 3a. Critique Generation
- LLM analyzes current script against quality criteria
- Identifies specific issues with examples
- Lists strengths to preserve
- Provides actionable improvement suggestions

#### 3b. Script Revision
- LLM generates revised version incorporating critique
- Maintains original topic, tone, and target duration
- Addresses identified issues while preserving strengths

#### 3c. Quality Reassessment
- Revised script is evaluated
- Quality delta calculated vs. previous iteration
- Metrics logged for tracking

#### Early Stopping Conditions:
1. **Quality Threshold Met**: Script reaches configured threshold (default: 85/100)
2. **Minimal Improvement**: Quality delta < minimum threshold (default: 5 points)
3. **Max Passes Reached**: Configured maximum iterations completed (default: 2)

### Stage 4: Optional Validation
- If enabled, IntelligentContentAdvisor performs final validation
- Additional quality checks applied

## Configuration

### appsettings.json

```json
{
  "ScriptRefinement": {
    "Enabled": true,
    "MaxRefinementPasses": 2,
    "QualityThreshold": 85.0,
    "MinimumImprovement": 5.0,
    "EnableAdvisorValidation": true,
    "PassTimeoutMinutes": 2
  }
}
```

### Programmatic Configuration

```csharp
var config = new ScriptRefinementConfig
{
    MaxRefinementPasses = 2,        // 1-3 passes allowed
    QualityThreshold = 85.0,        // 0-100 scale
    MinimumImprovement = 5.0,       // Delta to continue refining
    EnableAdvisorValidation = true, // Final validation with ContentAdvisor
    PassTimeout = TimeSpan.FromMinutes(2)
};
```

### PlanSpec Integration

```csharp
var planSpec = new PlanSpec(
    TargetDuration: TimeSpan.FromMinutes(3),
    Pacing: Pacing.Conversational,
    Density: Density.Balanced,
    Style: "educational",
    RefinementConfig: config  // Optional refinement configuration
);
```

## Usage Examples

### Basic Refinement

```csharp
var orchestrator = new ScriptRefinementOrchestrator(
    logger,
    llmProvider,
    contentAdvisor
);

var brief = new Brief(
    Topic: "Climate Change Solutions",
    Audience: "General public",
    Goal: "Educate and inspire action",
    Tone: "optimistic",
    Language: "en",
    Aspect: Aspect.Widescreen16x9
);

var spec = new PlanSpec(
    TargetDuration: TimeSpan.FromMinutes(3),
    Pacing: Pacing.Conversational,
    Density: Density.Balanced,
    Style: "educational"
);

var config = new ScriptRefinementConfig
{
    MaxRefinementPasses = 2,
    QualityThreshold = 85.0
};

var result = await orchestrator.RefineScriptAsync(brief, spec, config);

if (result.Success)
{
    Console.WriteLine($"Final Script: {result.FinalScript}");
    Console.WriteLine($"Quality: {result.FinalMetrics?.OverallScore:F1}/100");
    Console.WriteLine($"Total Passes: {result.TotalPasses}");
    Console.WriteLine($"Duration: {result.TotalDuration.TotalSeconds:F1}s");
    Console.WriteLine($"Stop Reason: {result.StopReason}");
    
    var improvement = result.GetTotalImprovement();
    if (improvement != null)
    {
        Console.WriteLine($"Quality Improvement: +{improvement.OverallDelta:F1} points");
    }
}
```

### API Usage

```json
POST /api/script
{
  "topic": "Future of AI in Healthcare",
  "audience": "Healthcare professionals",
  "goal": "Educational overview",
  "tone": "professional",
  "language": "en",
  "aspect": "Widescreen16x9",
  "targetDurationMinutes": 4.0,
  "pacing": "Conversational",
  "density": "Balanced",
  "style": "educational",
  "refinementConfig": {
    "maxRefinementPasses": 2,
    "qualityThreshold": 85.0,
    "minimumImprovement": 5.0,
    "enableAdvisorValidation": true,
    "passTimeoutMinutes": 2
  }
}
```

### Quality Metrics Analysis

```csharp
foreach (var metrics in result.IterationMetrics)
{
    Console.WriteLine($"\nIteration {metrics.Iteration}:");
    Console.WriteLine($"  Overall: {metrics.OverallScore:F1}");
    Console.WriteLine($"  Narrative Coherence: {metrics.NarrativeCoherence:F1}");
    Console.WriteLine($"  Pacing: {metrics.PacingAppropriateness:F1}");
    Console.WriteLine($"  Audience Alignment: {metrics.AudienceAlignment:F1}");
    Console.WriteLine($"  Visual Clarity: {metrics.VisualClarity:F1}");
    Console.WriteLine($"  Engagement: {metrics.EngagementPotential:F1}");
    
    if (metrics.Issues.Count > 0)
    {
        Console.WriteLine("  Issues:");
        foreach (var issue in metrics.Issues)
        {
            Console.WriteLine($"    - {issue}");
        }
    }
    
    if (metrics.Suggestions.Count > 0)
    {
        Console.WriteLine("  Suggestions:");
        foreach (var suggestion in metrics.Suggestions)
        {
            Console.WriteLine($"    - {suggestion}");
        }
    }
}
```

## Performance Characteristics

### Expected Performance
- **Single Pass (Initial Draft Only)**: Baseline time
- **Two-Pass Refinement**: ~60% time increase (acceptable per requirements)
- **Three-Pass Refinement**: ~100-120% time increase

### Performance Optimization Tips
1. Use appropriate `MaxRefinementPasses` based on content complexity
2. Set realistic `QualityThreshold` to enable early stopping
3. Disable `EnableAdvisorValidation` for faster processing when not needed
4. Monitor `TotalDuration` in results to track performance

### Performance Test Example

```csharp
var singlePassConfig = new ScriptRefinementConfig 
{ 
    MaxRefinementPasses = 1 
};

var twoPassConfig = new ScriptRefinementConfig 
{ 
    MaxRefinementPasses = 2 
};

var result1 = await orchestrator.RefineScriptAsync(brief, spec, singlePassConfig);
var result2 = await orchestrator.RefineScriptAsync(brief, spec, twoPassConfig);

var timeIncrease = result2.TotalDuration.TotalSeconds / result1.TotalDuration.TotalSeconds;
Console.WriteLine($"Time increase for 2-pass: {(timeIncrease - 1) * 100:F1}%");
```

## Quality Metrics Details

### Scoring System

All metrics use a 0-100 scale:
- **0-59**: Poor quality, needs significant improvement
- **60-74**: Below threshold, improvement recommended
- **75-84**: Good quality, approaching threshold
- **85-94**: High quality, meets threshold
- **95-100**: Excellent quality, exceeds expectations

### Overall Score Calculation

```
OverallScore = (NarrativeCoherence × 0.25) +
               (PacingAppropriateness × 0.20) +
               (AudienceAlignment × 0.20) +
               (VisualClarity × 0.15) +
               (EngagementPotential × 0.20)
```

### Interpretation

**Narrative Coherence (25% weight)**
- Logical flow and progression
- Clear beginning, middle, end
- Smooth transitions between ideas
- Compelling throughline

**Pacing Appropriateness (20% weight)**
- Information density matches duration
- Natural rhythm variations
- Appropriate for tone and audience
- Strategic timing of reveals

**Audience Alignment (20% weight)**
- Language level appropriate
- Relatable examples and references
- Addresses audience needs
- Appropriate complexity

**Visual Clarity (15% weight)**
- Scenes easily visualized
- Clear B-roll opportunities
- Supports visual storytelling
- Graphics/demonstration moments

**Engagement Potential (20% weight)**
- Strong opening hook
- Pattern interrupts and surprises
- Clear value proposition
- Memorable conclusion

## Integration with Existing Systems

### IntelligentContentAdvisor Integration

The refinement pipeline optionally integrates with `IntelligentContentAdvisor` for additional validation:

```csharp
var advisor = new IntelligentContentAdvisor(logger, llmProvider);
var orchestrator = new ScriptRefinementOrchestrator(
    logger,
    llmProvider,
    advisor  // Pass advisor for validation
);

var config = new ScriptRefinementConfig
{
    EnableAdvisorValidation = true  // Enable final validation
};
```

When enabled, the advisor performs:
- AI-powered quality analysis
- Heuristic checks for common issues
- Detection of AI-generation patterns
- Additional authenticity scoring

### ScriptOrchestrator Integration

The refinement can be integrated into `ScriptOrchestrator` for seamless script generation:

```csharp
// Generate initial draft
var scriptResult = await scriptOrchestrator.GenerateScriptAsync(
    brief, spec, tier, offlineOnly, ct);

if (scriptResult.Success && spec.RefinementConfig != null)
{
    // Apply refinement if configured
    var refinementResult = await refinementOrchestrator.RefineScriptAsync(
        brief, spec, spec.RefinementConfig, ct);
    
    if (refinementResult.Success)
    {
        scriptResult = scriptResult with 
        { 
            Script = refinementResult.FinalScript 
        };
    }
}
```

## Testing

### Unit Tests
Location: `Aura.Tests/ScriptRefinementOrchestratorTests.cs`

Test coverage:
- Configuration validation
- Quality metrics calculation
- Improvement tracking
- Threshold detection
- Max passes enforcement
- Early stopping conditions

### Integration Tests
Location: `Aura.Tests/Integration/ScriptRefinementPipelineTests.cs`

Test scenarios:
- Full refinement pipeline
- Performance characteristics
- Quality improvement tracking
- Advisor integration
- Cancellation handling
- Structured metrics output

## Troubleshooting

### Common Issues

**Issue**: Refinement takes too long
- **Solution**: Reduce `MaxRefinementPasses` or increase `QualityThreshold` for earlier stopping

**Issue**: Quality not improving between iterations
- **Solution**: Check LLM provider quality, ensure critique prompts are working, review logs

**Issue**: Early stopping not working
- **Solution**: Verify `MinimumImprovement` threshold is appropriate, check quality metrics

**Issue**: Out of memory during refinement
- **Solution**: Monitor script length, consider breaking into segments for very long scripts

### Logging

The orchestrator provides detailed logging at each stage:

```
Starting script refinement: MaxPasses=2, QualityThreshold=85
Stage 1: Generating initial draft (Pass 0)
Stage 2: Assessing initial draft quality
Initial draft quality: Overall=74.5, Narrative=75.0, Pacing=73.0, ...
Starting refinement pass 1/2
Pass 1 - Stage 3a: Generating critique
Pass 1 - Stage 3b: Generating revised script
Pass 1 - Stage 3c: Assessing revised quality
Pass 1 quality: Overall=82.3 (Δ+7.8), Narrative=83.0 (Δ+8.0), ...
Starting refinement pass 2/2
...
Quality threshold met at pass 2 (87.1 >= 85.0), stopping refinement
Script refinement complete: Duration=45.2s, Passes=3, TotalImprovement=+12.6, Final=87.1
```

## Future Enhancements

Potential improvements for future iterations:
1. Machine learning-based quality prediction
2. Fine-tuned critique models for specific domains
3. Multi-model consensus for quality assessment
4. Automated A/B testing of refinement strategies
5. Real-time refinement progress streaming to UI
6. Refinement history and learning from user feedback
7. Domain-specific quality metrics
8. Collaborative refinement with human-in-the-loop

## References

- Draft-Critique-Revise pattern: Inspired by Constitutional AI approaches
- Quality metrics: Based on video production best practices
- Integration: Designed for seamless integration with existing Aura pipeline
