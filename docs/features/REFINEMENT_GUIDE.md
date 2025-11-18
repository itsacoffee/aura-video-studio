# Script Refinement with Generator-Critic-Editor Pattern

This guide explains how to use the enhanced script refinement system that implements a generator-critic-editor pattern for iterative quality improvement.

## Overview

The refinement system uses three specialized roles:

1. **Generator**: Creates the initial script draft
2. **Critic**: Evaluates the script using structured rubrics and provides detailed feedback
3. **Editor**: Applies targeted improvements based on critique

## Key Features

- **Structured Rubrics**: Five comprehensive evaluation criteria (Clarity, Coherence, Timing, Engagement, AudienceAlignment)
- **Cost-Aware**: Respects budget constraints with early stopping
- **Telemetry**: Tracks scores, cost, and convergence statistics per round
- **Model Routing**: Can use cheaper/faster models for critic and editor roles
- **Schema Validation**: Ensures outputs remain valid and within duration constraints

## Basic Usage

### Using the EnhancedRefinementOrchestrator

```csharp
using Aura.Core.Services;
using Aura.Core.Models;
using Aura.Core.Providers;

// Setup services
var criticService = new CriticService(logger, llmProvider);
var editorService = new EditorService(logger, llmProvider);
var orchestrator = new EnhancedRefinementOrchestrator(
    logger,
    generatorProvider,
    criticService,
    editorService,
    contentAdvisor
);

// Configure refinement
var config = new ScriptRefinementConfig
{
    MaxRefinementPasses = 2,           // 1-3 rounds allowed
    QualityThreshold = 85.0,           // Stop if score reaches this
    MinimumImprovement = 5.0,          // Stop if improvement < this
    MaxCostBudget = 0.10,              // Maximum spend in dollars
    CriticModel = "gpt-3.5-turbo",     // Optional: use cheaper model for critique
    EditorModel = "gpt-3.5-turbo",     // Optional: use cheaper model for editing
    EnableSchemaValidation = true,     // Validate after each edit
    EnableTelemetry = true             // Track convergence stats
};

// Run refinement
var result = await orchestrator.RefineScriptAsync(brief, spec, config);

// Check results
Console.WriteLine($"Success: {result.Success}");
Console.WriteLine($"Final Score: {result.FinalMetrics?.OverallScore:F1}");
Console.WriteLine($"Total Cost: ${result.TotalCost:F4}");
Console.WriteLine($"Rounds: {result.TotalPasses}");
Console.WriteLine($"Stop Reason: {result.StopReason}");
Console.WriteLine($"\nCritique Summary:\n{result.CritiqueSummary}");
```

## Configuration Options

### ScriptRefinementConfig Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MaxRefinementPasses` | int | 2 | Number of refinement rounds (1-3) |
| `QualityThreshold` | double | 85.0 | Early stop if overall score >= this (0-100) |
| `MinimumImprovement` | double | 5.0 | Early stop if improvement < this |
| `MaxCostBudget` | double? | null | Maximum spend in dollars (null = no limit) |
| `CriticModel` | string? | null | Model for critic (null = same as generator) |
| `EditorModel` | string? | null | Model for editor (null = same as generator) |
| `EnableSchemaValidation` | bool | true | Validate schema after each edit |
| `EnableTelemetry` | bool | true | Track convergence statistics |
| `EnableAdvisorValidation` | bool | true | Final validation with content advisor |
| `PassTimeout` | TimeSpan | 2 minutes | Timeout per refinement pass |

## Rubric-Based Evaluation

### Default Rubrics

1. **Clarity (25% weight)**
   - Language simplicity
   - Visual clarity
   - Target: 85/100

2. **Coherence (25% weight)**
   - Logical flow
   - Narrative arc
   - Target: 85/100

3. **Timing (20% weight)**
   - Word count fit (150 words/minute)
   - Information density
   - Target: 85/100

4. **Engagement (15% weight)**
   - Hook strength
   - Pattern interrupts
   - Target: 85/100

5. **AudienceAlignment (15% weight)**
   - Language level
   - Relevance to audience
   - Target: 85/100

### Custom Rubrics

You can create custom rubrics:

```csharp
var customRubric = new RefinementRubric
{
    Name = "BrandConsistency",
    Description = "Measures alignment with brand voice and values",
    Weight = 0.20,
    TargetThreshold = 90.0,
    Criteria = new List<RubricCriterion>
    {
        new RubricCriterion
        {
            Name = "Tone Match",
            Description = "Voice matches brand guidelines",
            ScoringGuideline = "100: Perfect match. 50: Partial. 0: Off-brand.",
            ExcellentExamples = new List<string> { "Uses brand-approved terminology" },
            PoorExamples = new List<string> { "Generic or off-brand language" }
        }
    }
};

// Use in critique
var critique = await criticService.CritiqueScriptAsync(
    script, brief, spec, 
    new[] { customRubric }, 
    currentMetrics, 
    ct
);
```

## Telemetry and Convergence

### Accessing Telemetry Data

```csharp
if (result.Telemetry != null)
{
    // Round-by-round data
    foreach (var round in result.Telemetry.RoundData)
    {
        Console.WriteLine($"Round {round.RoundNumber}:");
        Console.WriteLine($"  Score: {round.AfterMetrics?.OverallScore:F1}");
        Console.WriteLine($"  Cost: ${round.Cost:F4}");
        Console.WriteLine($"  Duration: {round.Duration.TotalSeconds:F1}s");
        Console.WriteLine($"  Valid: {round.SchemaValid}");
    }

    // Convergence statistics
    var conv = result.Telemetry.Convergence;
    Console.WriteLine($"\nConvergence:");
    Console.WriteLine($"  Converged: {conv.Converged}");
    Console.WriteLine($"  Avg Improvement: {conv.AverageImprovementPerRound:F1}");
    Console.WriteLine($"  Total Improvement: {conv.TotalImprovement:F1}");
    Console.WriteLine($"  Rate: {conv.ConvergenceRate:F2}");

    // Cost breakdown
    foreach (var kvp in result.Telemetry.CostByPhase)
    {
        Console.WriteLine($"  {kvp.Key}: ${kvp.Value:F4}");
    }
}
```

## Early Stopping Conditions

The orchestrator will stop early if any of these conditions are met:

1. **Quality Threshold**: Score >= `QualityThreshold`
2. **Cost Budget**: Total cost >= `MaxCostBudget`
3. **Minimal Improvement**: Improvement < `MinimumImprovement`
4. **Max Passes**: Reached `MaxRefinementPasses`

The reason is stored in `result.StopReason`.

## Example Workflow

```csharp
// 1. Create brief and spec
var brief = new Brief(
    Topic: "Artificial Intelligence Basics",
    Audience: "Beginners",
    Goal: "Educational introduction",
    Tone: "friendly",
    Language: "en",
    Aspect: Aspect.Widescreen16x9
);

var spec = new PlanSpec(
    TargetDuration: TimeSpan.FromMinutes(2),
    Pacing: Pacing.Conversational,
    Density: Density.Balanced,
    Style: "educational"
);

// 2. Configure cost-aware refinement
var config = new ScriptRefinementConfig
{
    MaxRefinementPasses = 2,
    QualityThreshold = 85.0,
    MaxCostBudget = 0.05,  // $0.05 limit
    CriticModel = "gpt-3.5-turbo",  // Cheaper for critique
    EditorModel = "gpt-3.5-turbo"   // Cheaper for editing
};

// 3. Run refinement
var result = await orchestrator.RefineScriptAsync(brief, spec, config);

// 4. Check quality improvement
var improvement = result.GetTotalImprovement();
if (improvement != null)
{
    Console.WriteLine($"Quality improved by {improvement.OverallDelta:+F1} points");
    Console.WriteLine($"  Clarity: {improvement.VisualClarityDelta:+F1}");
    Console.WriteLine($"  Coherence: {improvement.NarrativeCoherenceDelta:+F1}");
    Console.WriteLine($"  Timing: {improvement.PacingDelta:+F1}");
}

// 5. Use final script
if (result.Success)
{
    var finalScript = result.FinalScript;
    // Proceed with video generation...
}
```

## Performance Considerations

### Cost Optimization

- Use cheaper models for critic and editor roles
- Set reasonable `MaxCostBudget` to prevent runaway costs
- Monitor `result.TotalCost` to track spending

### Model Selection

| Role | Recommended Models | Notes |
|------|-------------------|-------|
| Generator | GPT-4, Claude 3 | High quality for initial draft |
| Critic | GPT-3.5, Gemini Pro | Cheaper, still effective |
| Editor | GPT-3.5, Claude Instant | Fast and cost-effective |

### Timing Constraints

- Each pass has a 2-minute default timeout
- Total refinement typically takes 1-5 minutes depending on rounds
- Schema validation adds ~1s per round

## Troubleshooting

### High Costs

**Problem**: Refinement exceeds budget

**Solutions**:
- Set `MaxCostBudget` to enforce limits
- Use cheaper models for `CriticModel` and `EditorModel`
- Reduce `MaxRefinementPasses` to 1-2 rounds

### Low Quality Scores

**Problem**: Scores don't improve significantly

**Solutions**:
- Check if initial draft already meets threshold
- Review `result.Telemetry.Convergence.Converged` - may have plateaued
- Adjust rubric weights or thresholds
- Try different generator models

### Schema Validation Failures

**Problem**: `SchemaValid = false` in telemetry

**Solutions**:
- Check `editResult.ValidationResult.Errors` for details
- Ensure target duration is reasonable
- Review script format requirements

## Integration Example

### Using with Existing ScriptOrchestrator

```csharp
// Generate initial script with existing orchestrator
var scriptResult = await scriptOrchestrator.GenerateScriptDeterministicAsync(
    brief, spec, preferredTier, offlineOnly, ct
);

if (!scriptResult.Success)
{
    return scriptResult;
}

// Optionally refine with enhanced orchestrator
if (spec.RefinementConfig != null)
{
    var refinementResult = await enhancedOrchestrator.RefineScriptAsync(
        brief, spec, spec.RefinementConfig, ct
    );

    if (refinementResult.Success)
    {
        scriptResult = scriptResult with 
        { 
            Script = refinementResult.FinalScript 
        };
    }
}

return scriptResult;
```

## Best Practices

1. **Start Conservative**: Use 1-2 passes initially, scale up if needed
2. **Set Budgets**: Always set `MaxCostBudget` in production
3. **Monitor Telemetry**: Track convergence to optimize configuration
4. **Use Appropriate Models**: Don't waste money on expensive models for critique/editing
5. **Validate Schema**: Keep `EnableSchemaValidation = true` to catch format issues
6. **Review Critiques**: Check `CritiqueSummary` to understand what improved

## Future Enhancements

Potential improvements for future versions:

- Human-in-the-loop review points
- Adaptive rubric weights based on content type
- Multi-modal critique (visual + text)
- A/B testing different refinement strategies
- Custom stop conditions based on domain metrics
