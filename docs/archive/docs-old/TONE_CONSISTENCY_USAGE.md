> **⚠️ ARCHIVED DOCUMENT**  
> This document is retained for historical context only.  
> It may contain outdated information, references to superseded implementations, or details specific to past pull requests.  
> For current documentation, see the main docs/ directory and canonical guides in the repository root.

---


# Tone Consistency Enforcement - Usage Guide

## Overview

The Tone Consistency Enforcement system maintains consistent tone, voice, and stylistic elements throughout the video generation pipeline. It uses LLMs to validate tone adherence across script, visuals, pacing, and audio.

## Quick Start

### 1. Expand Tone Profile from Brief

```csharp
using Aura.Core.Services.Quality;
using Aura.Core.Models.Quality;

// In your orchestrator or service
var toneEnforcer = new ToneConsistencyEnforcer(logger, llmProvider);

// Expand the simple tone from Brief into a comprehensive profile
var brief = new Brief(
    Topic: "AI in Healthcare",
    Audience: "Medical professionals",
    Goal: "Educate",
    Tone: "professional",  // Simple tone
    Language: "en",
    Aspect: Aspect.Widescreen16x9
);

ToneProfile toneProfile = await toneEnforcer.ExpandToneProfileAsync(
    brief.Tone, 
    cancellationToken);

// toneProfile now contains:
// - VocabularyLevel (e.g., College)
// - Formality (e.g., Professional)
// - Humor (e.g., None)
// - Energy (e.g., Moderate)
// - Perspective (e.g., ThirdPersonAuthority)
// - Guidelines, ExamplePhrases, PhrasesToAvoid
// - TTS recommendations (rate, pitch)
// - Visual style keywords
```

### 2. Use Tone Profile in Script Generation

```csharp
using Aura.Core.AI;

// Option A: Augment system prompt
var systemPrompt = EnhancedPromptTemplates.GetSystemPromptForScriptGeneration();
var toneAwareSystemPrompt = EnhancedPromptTemplates.AugmentSystemPromptWithTone(
    systemPrompt, 
    toneProfile);

// Option B: Use enhanced prompt builder
var prompt = EnhancedPromptTemplates.BuildScriptGenerationPromptWithTone(
    brief,
    planSpec,
    toneProfile,
    additionalContext: null
);

// Send to LLM with tone constraints embedded
var script = await llmProvider.DraftScriptAsync(brief, planSpec, cancellationToken);
```

### 3. Validate Script Tone

```csharp
// After script generation, validate each scene
var scenes = ParseScriptIntoScenes(script);

for (int i = 0; i < scenes.Length; i++)
{
    ToneConsistencyScore score = await toneEnforcer.ValidateScriptToneAsync(
        scenes[i].Text,
        toneProfile,
        sceneIndex: i,
        cancellationToken
    );

    if (!score.Passes)  // Score must be > 85
    {
        logger.LogWarning(
            "Scene {Index} failed tone validation. Score: {Score:F1}. Reason: {Reason}",
            i, score.OverallScore, score.Reasoning);
    }
}
```

### 4. Detect Style Violations

```csharp
// Check entire script for violations
var sceneTexts = scenes.Select(s => s.Text).ToArray();

StyleViolation[] violations = await toneEnforcer.DetectStyleViolationsAsync(
    sceneTexts,
    toneProfile,
    cancellationToken
);

// Filter by severity
var criticalViolations = violations
    .Where(v => v.Severity >= ViolationSeverity.High)
    .ToArray();

foreach (var violation in criticalViolations)
{
    logger.LogError(
        "Scene {SceneIndex}: {Category} - {Description}. Example: '{Example}'",
        violation.SceneIndex,
        violation.Category,
        violation.Description,
        violation.Example
    );
}
```

### 5. Generate Corrections

```csharp
// Generate correction suggestions for violations
ToneCorrectionSuggestion[] suggestions = 
    await toneEnforcer.GenerateCorrectionSuggestionsAsync(
        sceneTexts,
        toneProfile,
        violations,
        cancellationToken
    );

foreach (var suggestion in suggestions)
{
    logger.LogInformation(
        "Scene {Index} correction: '{Original}' -> '{Corrected}'",
        suggestion.SceneIndex,
        suggestion.OriginalText,
        suggestion.CorrectedText
    );

    // Optionally apply the correction
    scenes[suggestion.SceneIndex].Text = suggestion.CorrectedText;
}
```

### 6. Validate Visual Style Alignment

```csharp
// When generating visual prompts
var visualPrompt = EnhancedPromptTemplates.BuildVisualSelectionPrompt(
    sceneHeading: scenes[i].Heading,
    sceneContent: scenes[i].Text,
    tone: brief.Tone,
    sceneIndex: i
);

// Augment with tone
var toneAwareVisualPrompt = EnhancedPromptTemplates.AugmentVisualPromptWithTone(
    visualPrompt,
    toneProfile
);

// After visual description is generated
ToneConsistencyScore visualScore = await toneEnforcer.ValidateVisualStyleToneAsync(
    visualDescription: generatedVisualDescription,
    toneProfile: toneProfile,
    sceneIndex: i,
    cancellationToken
);
```

### 7. Validate Pacing

```csharp
// After determining scene durations
var cutFrequency = 60.0 / sceneDurations.Average(); // Cuts per minute
var avgDuration = sceneDurations.Average();

ToneConsistencyScore pacingScore = await toneEnforcer.ValidatePacingToneAsync(
    cutFrequency,
    avgDuration,
    toneProfile,
    cancellationToken
);

// Expected correlations by energy level:
// - Calm: 0.5 cuts/min
// - Moderate: 1.0 cuts/min
// - Energetic: 2.0 cuts/min
// - High: 3.0 cuts/min
```

### 8. Validate Audio/TTS Settings

```csharp
// Adjust TTS settings based on tone profile
var voiceSpec = new VoiceSpec(
    VoiceName: selectedVoice,
    Rate: 1.0 + (toneProfile.RecommendedTtsRateAdjustment / 100.0),
    Pitch: 1.0 + (toneProfile.RecommendedTtsPitchAdjustment / 100.0),
    Pause: PauseStyle.Natural
);

ToneConsistencyScore audioScore = await toneEnforcer.ValidateAudioToneAsync(
    voiceSpec,
    toneProfile,
    cancellationToken
);
```

### 9. Detect Tone Drift

```csharp
// Check for gradual tone drift across scenes
ToneDriftResult driftResult = await toneEnforcer.DetectToneDriftAsync(
    sceneTexts,
    toneProfile,
    windowSize: 3,  // Analyze 3 scenes at a time
    cancellationToken
);

if (driftResult.DriftDetected)
{
    logger.LogWarning(
        "Tone drift detected at scenes: {Indices}. Magnitude: {Magnitude:F2}. Characteristics: {Chars}",
        string.Join(", ", driftResult.DriftStartIndices),
        driftResult.DriftMagnitude,
        string.Join(", ", driftResult.DriftedCharacteristics)
    );
}
```

### 10. Cross-Modal Validation

```csharp
// Final validation before rendering
CrossModalToneValidation validation = await toneEnforcer.ValidateCrossModalToneAsync(
    sceneTexts: sceneTexts,
    visualDescriptions: visualDescriptions,
    sceneDurations: sceneDurations,
    voiceSpec: voiceSpec,
    toneProfile: toneProfile,
    cancellationToken
);

if (!validation.IsAligned)  // Overall score must be > 80
{
    logger.LogWarning(
        "Cross-modal tone validation failed. Scores - Script: {Script:F1}, Visual: {Visual:F1}, Pacing: {Pacing:F1}, Audio: {Audio:F1}",
        validation.ScriptScore,
        validation.VisualScore,
        validation.PacingScore,
        validation.AudioScore
    );

    // Log violations
    foreach (var violation in validation.Violations)
    {
        logger.LogWarning("Violation: {Description}", violation.Description);
    }
}
```

## Integration Points

### ScriptOrchestrator Integration

```csharp
public class ScriptOrchestrator
{
    private readonly ToneConsistencyEnforcer _toneEnforcer;

    public async Task<Script> GenerateScriptAsync(Brief brief, PlanSpec spec, CancellationToken ct)
    {
        // 1. Expand tone profile at the start
        var toneProfile = await _toneEnforcer.ExpandToneProfileAsync(brief.Tone, ct);

        // 2. Use tone-aware prompts
        var prompt = EnhancedPromptTemplates.BuildScriptGenerationPromptWithTone(
            brief, spec, toneProfile);

        // 3. Generate script
        var scriptText = await _llmProvider.DraftScriptAsync(brief, spec, ct);

        // 4. Validate tone
        var scenes = ParseScenes(scriptText);
        var violations = await _toneEnforcer.DetectStyleViolationsAsync(
            scenes.Select(s => s.Text).ToArray(), toneProfile, ct);

        // 5. Generate corrections if needed
        if (violations.Any(v => v.Severity >= ViolationSeverity.High))
        {
            var suggestions = await _toneEnforcer.GenerateCorrectionSuggestionsAsync(
                scenes.Select(s => s.Text).ToArray(), toneProfile, violations, ct);

            // Apply suggestions
            foreach (var suggestion in suggestions)
            {
                scenes[suggestion.SceneIndex].Text = suggestion.CorrectedText;
            }
        }

        // 6. Check for drift
        var drift = await _toneEnforcer.DetectToneDriftAsync(
            scenes.Select(s => s.Text).ToArray(), toneProfile, windowSize: 3, ct);

        return new Script { Scenes = scenes, ToneProfile = toneProfile };
    }
}
```

### VideoOrchestrator Integration

```csharp
public class VideoOrchestrator
{
    public async Task<string> GenerateVideoAsync(
        Brief brief, 
        PlanSpec planSpec, 
        VoiceSpec voiceSpec,
        CancellationToken ct)
    {
        // ... existing code ...

        // Before final rendering, validate cross-modal tone
        var validation = await _toneEnforcer.ValidateCrossModalToneAsync(
            script.Scenes.Select(s => s.Text).ToArray(),
            visuals.Select(v => v.Description).ToArray(),
            timeline.Scenes.Select(s => s.Duration.TotalSeconds).ToArray(),
            voiceSpec,
            script.ToneProfile,
            ct
        );

        if (!validation.IsAligned)
        {
            // Log warnings or abort if critical
            _logger.LogWarning(
                "Cross-modal tone alignment issue. Overall score: {Score:F1}",
                validation.OverallScore);
        }

        // ... continue with rendering ...
    }
}
```

## Best Practices

### 1. Cache Tone Profile
Expand the tone profile once and reuse it across all pipeline stages:

```csharp
// At the start of video generation
var toneProfile = await toneEnforcer.ExpandToneProfileAsync(brief.Tone, ct);

// Store in context and pass to all stages
context.ToneProfile = toneProfile;
```

### 2. Parallel Scene Validation
Validate multiple scenes in parallel for better performance:

```csharp
var validationTasks = scenes.Select((scene, index) =>
    toneEnforcer.ValidateScriptToneAsync(scene.Text, toneProfile, index, ct)
).ToArray();

var scores = await Task.WhenAll(validationTasks);
```

### 3. Progressive Correction
Apply corrections in order of severity:

```csharp
var orderedSuggestions = suggestions
    .OrderByDescending(s => s.ScoreBefore - s.ScoreAfter)  // Biggest improvement
    .ThenByDescending(s => violations.First(v => v.SceneIndex == s.SceneIndex).Severity);

foreach (var suggestion in orderedSuggestions)
{
    // Apply correction
    ApplyCorrection(suggestion);
}
```

### 4. Fallback Handling
Always handle LLM failures gracefully:

```csharp
try
{
    var profile = await toneEnforcer.ExpandToneProfileAsync(brief.Tone, ct);
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Tone expansion failed, using fallback");
    // Service provides fallback automatically
}
```

### 5. Performance Optimization
Set appropriate timeouts for LLM calls:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);

var score = await toneEnforcer.ValidateScriptToneAsync(
    scriptText, toneProfile, sceneIndex, linkedCts.Token);
```

## Metrics and Monitoring

### Key Metrics to Track

```csharp
// Log tone validation metrics
_metrics.RecordToneConsistencyScore(score.OverallScore);
_metrics.RecordViolationCount(violations.Length);
_metrics.RecordCorrectionRate(suggestions.Length / (double)violations.Length);
_metrics.RecordCrossModalAlignment(validation.OverallScore);
_metrics.RecordToneDriftIncidence(driftResult.DriftDetected);
```

### Performance Metrics

```csharp
var sw = Stopwatch.StartNew();
var profile = await toneEnforcer.ExpandToneProfileAsync(brief.Tone, ct);
_metrics.RecordToneExpansionDuration(sw.Elapsed);
// Target: < 2 seconds

sw.Restart();
var score = await toneEnforcer.ValidateScriptToneAsync(text, profile, 0, ct);
_metrics.RecordSceneValidationDuration(sw.Elapsed);
// Target: < 2 seconds per scene
```

## Troubleshooting

### Issue: Tone validation scores are too low

**Solution**: Check that the tone profile was expanded correctly and matches the content type.

```csharp
_logger.LogInformation(
    "Tone Profile: Vocab={Vocab}, Formality={Form}, Energy={Energy}",
    profile.VocabularyLevel, profile.Formality, profile.Energy);
```

### Issue: Too many false positive violations

**Solution**: Adjust the severity threshold for corrections:

```csharp
var criticalOnly = violations.Where(v => v.Severity >= ViolationSeverity.Critical);
```

### Issue: Tone drift false positives

**Solution**: Increase the window size or drift threshold:

```csharp
var drift = await toneEnforcer.DetectToneDriftAsync(
    sceneTexts, toneProfile, windowSize: 5, ct);  // Larger window

if (drift.DriftMagnitude > 0.5)  // Higher threshold
{
    // Handle drift
}
```

## Summary

The Tone Consistency Enforcement system provides comprehensive tone validation across all video generation stages. Use it to:

1. ✓ Expand simple tones into detailed profiles
2. ✓ Validate scripts, visuals, pacing, and audio
3. ✓ Detect and correct style violations
4. ✓ Monitor for tone drift
5. ✓ Ensure cross-modal consistency

For questions or issues, refer to the implementation in `Aura.Core/Services/Quality/ToneConsistencyEnforcer.cs` or the test suite in `Aura.Tests/ToneConsistencyEnforcerTests.cs`.
