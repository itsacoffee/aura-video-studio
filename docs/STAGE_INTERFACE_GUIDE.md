# Pipeline Stage Interface Guide

## Introduction

This guide explains how to implement custom pipeline stages for the Aura Video Studio orchestration system. By following this interface guide, you can extend the video generation pipeline with custom processing stages.

## PipelineStage Interface

### Required Methods

#### ExecuteStageAsync (Abstract)

The core method that implements your stage's logic:

```csharp
protected abstract Task ExecuteStageAsync(
    PipelineContext context,
    IProgress<StageProgress>? progress,
    CancellationToken ct);
```

**Parameters**:
- `context`: Pipeline state containing inputs, outputs, and metadata
- `progress`: Optional progress reporter for UI updates
- `ct`: Cancellation token to handle cancellation requests

**Responsibilities**:
- Implement your stage's core logic
- Report progress at key milestones
- Store output in context
- Handle errors appropriately
- Respect cancellation tokens

### Required Properties

#### StageName (Abstract)
Unique identifier for the stage:

```csharp
public override string StageName => "MyStage";
```

**Requirements**:
- Must be unique across all stages
- Use PascalCase (e.g., "Script", "Voice", "Custom")
- Used for logging, metrics, and state management

#### DisplayName (Abstract)
Human-readable name for UI display:

```csharp
public override string DisplayName => "My Custom Stage";
```

**Requirements**:
- Should be descriptive and user-friendly
- Used in progress updates and error messages

### Optional Properties

#### ProgressWeight
Relative weight of this stage for overall progress calculation:

```csharp
public override int ProgressWeight => 20;
```

**Default**: 20
**Range**: 1-100
**Example weights**:
- Brief: 5 (quick validation)
- Script: 20 (moderate LLM call)
- Visuals: 30 (slow image generation)

#### Timeout
Maximum time allowed for stage execution:

```csharp
public override TimeSpan Timeout => TimeSpan.FromMinutes(5);
```

**Default**: 5 minutes
**Considerations**:
- Account for provider latency
- Consider retry attempts
- Balance responsiveness vs reliability

#### SupportsRetry
Whether this stage should retry on failure:

```csharp
public override bool SupportsRetry => true;
```

**Default**: true
**When to disable**:
- Validation stages (retry won't help)
- Non-idempotent operations
- Quick operations that fail deterministically

#### MaxRetryAttempts
Maximum number of retry attempts:

```csharp
public override int MaxRetryAttempts => 3;
```

**Default**: 3
**Considerations**:
- Higher for transient errors (network, rate limits)
- Lower for expensive operations
- Consider total timeout = stage timeout × (attempts + 1)

#### SupportsResume
Whether this stage can be skipped if already completed:

```csharp
public override bool SupportsResume => true;
```

**Default**: true
**When to disable**:
- Stages that must always run fresh
- Non-deterministic operations
- Validation that depends on external state

### Protected Helper Methods

#### ReportProgress
Report progress for this stage:

```csharp
protected void ReportProgress(
    IProgress<StageProgress>? progress,
    int percentage,
    string message,
    int currentItem = 0,
    int totalItems = 0);
```

**Example**:
```csharp
ReportProgress(progress, 25, "Downloading assets...", 1, 4);
ReportProgress(progress, 50, "Processing...");
ReportProgress(progress, 100, "Stage completed");
```

**Best Practices**:
- Report at 0% when starting
- Report at 100% when completing
- Report at logical milestones (25%, 50%, 75%)
- Use descriptive messages
- Include item counts for multi-item processing

#### CanSkipStage
Determine if stage can be skipped (for resume support):

```csharp
protected override bool CanSkipStage(PipelineContext context)
{
    // Check if output already exists
    return context.GetStageOutput<MyOutput>(StageName) != null;
}
```

**Default behavior**: Checks if stage output exists
**Override when**: Custom resume logic is needed

#### GetItemsProcessed
Get count of items processed (for metrics):

```csharp
protected override int GetItemsProcessed(PipelineContext context)
{
    return context.ParsedScenes?.Count ?? 0;
}
```

**Default**: 1
**Override when**: Stage processes multiple items

## Implementation Examples

### Example 1: Simple Validation Stage

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Orchestrator;
using Microsoft.Extensions.Logging;

namespace MyApp.Pipeline.Stages;

public class QualityCheckStage : PipelineStage
{
    public QualityCheckStage(ILogger<QualityCheckStage> logger) 
        : base(logger) { }

    public override string StageName => "QualityCheck";
    public override string DisplayName => "Quality Verification";
    public override int ProgressWeight => 5;
    public override TimeSpan Timeout => TimeSpan.FromSeconds(30);
    public override bool SupportsRetry => false; // Validation doesn't need retry

    protected override async Task ExecuteStageAsync(
        PipelineContext context,
        IProgress<StageProgress>? progress,
        CancellationToken ct)
    {
        ReportProgress(progress, 0, "Starting quality check...");

        // Get required input from previous stage
        var script = context.GeneratedScript;
        if (string.IsNullOrEmpty(script))
        {
            throw new InvalidOperationException("Script is required for quality check");
        }

        ReportProgress(progress, 25, "Analyzing script quality...");

        // Perform quality checks
        var wordCount = script.Split(' ').Length;
        var hasScenes = script.Contains("## ");
        
        ReportProgress(progress, 50, "Checking narration timing...");

        // Validate narration exists
        if (string.IsNullOrEmpty(context.NarrationPath))
        {
            throw new InvalidOperationException("Narration is required");
        }

        ReportProgress(progress, 75, "Validating visual assets...");

        // Check visual assets
        var assetCount = context.SceneAssets?.Values
            .Sum(assets => assets.Count) ?? 0;

        ReportProgress(progress, 90, "Generating quality report...");

        // Store quality metrics
        var output = new QualityCheckOutput
        {
            WordCount = wordCount,
            HasScenes = hasScenes,
            AssetCount = assetCount,
            OverallQuality = CalculateQuality(wordCount, hasScenes, assetCount),
            CheckedAt = DateTime.UtcNow
        };

        context.SetStageOutput(StageName, output);

        ReportProgress(progress, 100, "Quality check completed");

        await Task.CompletedTask;
    }

    private double CalculateQuality(int wordCount, bool hasScenes, int assetCount)
    {
        var score = 0.0;
        if (wordCount > 50) score += 0.3;
        if (hasScenes) score += 0.4;
        if (assetCount > 0) score += 0.3;
        return score;
    }
}

public record QualityCheckOutput
{
    public required int WordCount { get; init; }
    public required bool HasScenes { get; init; }
    public required int AssetCount { get; init; }
    public required double OverallQuality { get; init; }
    public required DateTime CheckedAt { get; init; }
}
```

### Example 2: External Service Stage

```csharp
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Orchestrator;
using Microsoft.Extensions.Logging;

namespace MyApp.Pipeline.Stages;

public class TranslationStage : PipelineStage
{
    private readonly HttpClient _httpClient;
    private readonly ITranslationService _translator;

    public TranslationStage(
        ILogger<TranslationStage> logger,
        HttpClient httpClient,
        ITranslationService translator) : base(logger)
    {
        _httpClient = httpClient;
        _translator = translator;
    }

    public override string StageName => "Translation";
    public override string DisplayName => "Script Translation";
    public override int ProgressWeight => 15;
    public override TimeSpan Timeout => TimeSpan.FromMinutes(2);
    public override bool SupportsRetry => true;
    public override int MaxRetryAttempts => 3;

    protected override async Task ExecuteStageAsync(
        PipelineContext context,
        IProgress<StageProgress>? progress,
        CancellationToken ct)
    {
        ReportProgress(progress, 0, "Preparing translation...");

        // Get script to translate
        var script = context.GeneratedScript;
        if (string.IsNullOrEmpty(script))
        {
            throw new InvalidOperationException("Script is required");
        }

        // Get target language from brief
        var targetLanguage = context.Brief.Language ?? "English";
        if (targetLanguage == "English")
        {
            Logger.LogInformation("Script already in English, skipping translation");
            context.SetStageOutput(StageName, new TranslationOutput
            {
                TranslatedScript = script,
                TargetLanguage = targetLanguage,
                Skipped = true
            });
            return;
        }

        ReportProgress(progress, 20, $"Translating to {targetLanguage}...");

        // Call translation service with retry support
        string translatedScript;
        try
        {
            translatedScript = await _translator.TranslateAsync(
                script,
                sourceLanguage: "English",
                targetLanguage: targetLanguage,
                ct);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Translation service failed");
            throw new InvalidOperationException(
                $"Failed to translate to {targetLanguage}", ex);
        }

        ReportProgress(progress, 80, "Validating translation...");

        // Validate translation
        if (string.IsNullOrEmpty(translatedScript))
        {
            throw new InvalidOperationException("Translation service returned empty result");
        }

        ReportProgress(progress, 90, "Translation completed");

        // Update context with translated script
        context.GeneratedScript = translatedScript;
        context.SetStageOutput(StageName, new TranslationOutput
        {
            TranslatedScript = translatedScript,
            TargetLanguage = targetLanguage,
            OriginalLength = script.Length,
            TranslatedLength = translatedScript.Length
        });

        ReportProgress(progress, 100, "Translation stage completed");
    }

    protected override bool CanSkipStage(PipelineContext context)
    {
        // Skip if already translated
        var output = context.GetStageOutput<TranslationOutput>(StageName);
        return output != null && !string.IsNullOrEmpty(output.TranslatedScript);
    }
}

public record TranslationOutput
{
    public required string TranslatedScript { get; init; }
    public required string TargetLanguage { get; init; }
    public int OriginalLength { get; init; }
    public int TranslatedLength { get; init; }
    public bool Skipped { get; init; }
}
```

### Example 3: Multi-Item Processing Stage

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Orchestrator;
using Microsoft.Extensions.Logging;

namespace MyApp.Pipeline.Stages;

public class ThumbnailGenerationStage : PipelineStage
{
    private readonly IThumbnailGenerator _thumbnailGenerator;

    public ThumbnailGenerationStage(
        ILogger<ThumbnailGenerationStage> logger,
        IThumbnailGenerator thumbnailGenerator) : base(logger)
    {
        _thumbnailGenerator = thumbnailGenerator;
    }

    public override string StageName => "Thumbnails";
    public override string DisplayName => "Thumbnail Generation";
    public override int ProgressWeight => 10;
    public override TimeSpan Timeout => TimeSpan.FromMinutes(3);

    protected override async Task ExecuteStageAsync(
        PipelineContext context,
        IProgress<StageProgress>? progress,
        CancellationToken ct)
    {
        ReportProgress(progress, 0, "Preparing thumbnail generation...");

        var scenes = context.ParsedScenes;
        if (scenes == null || scenes.Count == 0)
        {
            throw new InvalidOperationException("Scenes are required");
        }

        var thumbnails = new List<ThumbnailInfo>();
        int processedCount = 0;
        int totalCount = scenes.Count;

        // Generate thumbnail for each scene
        foreach (var scene in scenes)
        {
            ct.ThrowIfCancellationRequested();

            var percent = (int)((processedCount / (double)totalCount) * 80) + 10;
            ReportProgress(
                progress,
                percent,
                $"Generating thumbnail {processedCount + 1}/{totalCount}...",
                processedCount + 1,
                totalCount);

            try
            {
                var thumbnail = await _thumbnailGenerator.GenerateAsync(
                    scene,
                    width: 1280,
                    height: 720,
                    ct);

                thumbnails.Add(new ThumbnailInfo
                {
                    SceneIndex = scene.Index,
                    Path = thumbnail.Path,
                    Width = thumbnail.Width,
                    Height = thumbnail.Height,
                    SizeBytes = thumbnail.SizeBytes
                });

                Logger.LogDebug(
                    "Generated thumbnail for scene {Index}: {Path}",
                    scene.Index,
                    thumbnail.Path);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(
                    ex,
                    "Failed to generate thumbnail for scene {Index}",
                    scene.Index);
                // Continue with other scenes
            }

            processedCount++;
        }

        ReportProgress(progress, 95, "Finalizing thumbnails...");

        // Store thumbnails
        context.SetStageOutput(StageName, new ThumbnailsOutput
        {
            Thumbnails = thumbnails,
            GeneratedCount = thumbnails.Count,
            TotalScenes = totalCount,
            GeneratedAt = DateTime.UtcNow
        });

        ReportProgress(progress, 100, $"Generated {thumbnails.Count} thumbnails");
    }

    protected override int GetItemsProcessed(PipelineContext context)
    {
        var output = context.GetStageOutput<ThumbnailsOutput>(StageName);
        return output?.GeneratedCount ?? 0;
    }
}

public record ThumbnailInfo
{
    public required int SceneIndex { get; init; }
    public required string Path { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required long SizeBytes { get; init; }
}

public record ThumbnailsOutput
{
    public required List<ThumbnailInfo> Thumbnails { get; init; }
    public required int GeneratedCount { get; init; }
    public required int TotalScenes { get; init; }
    public required DateTime GeneratedAt { get; init; }
}
```

## Integration Checklist

When adding a custom stage:

- [ ] Implement `ExecuteStageAsync` with your logic
- [ ] Define `StageName` (unique identifier)
- [ ] Define `DisplayName` (user-friendly name)
- [ ] Set appropriate `ProgressWeight`
- [ ] Set appropriate `Timeout`
- [ ] Configure retry behavior (`SupportsRetry`, `MaxRetryAttempts`)
- [ ] Implement `CanSkipStage` if resume support needed
- [ ] Implement `GetItemsProcessed` for accurate metrics
- [ ] Report progress at key milestones (0%, 100% minimum)
- [ ] Store output in context using `SetStageOutput`
- [ ] Handle cancellation tokens properly
- [ ] Register stage in DI container (`Program.cs`)
- [ ] Add stage to pipeline execution flow
- [ ] Write unit tests
- [ ] Document stage behavior and requirements

## Testing Your Stage

### Unit Test Template

```csharp
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;

public class MyStageTests
{
    [Fact]
    public async Task ExecuteAsync_ValidInput_Succeeds()
    {
        // Arrange
        var logger = Mock.Of<ILogger<MyStage>>();
        var stage = new MyStage(logger);
        var context = CreateTestContext();

        // Act
        var result = await stage.ExecuteAsync(context);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(context.GetStageOutput<MyOutput>(stage.StageName));
    }

    [Fact]
    public async Task ExecuteAsync_MissingInput_Fails()
    {
        // Arrange
        var logger = Mock.Of<ILogger<MyStage>>();
        var stage = new MyStage(logger);
        var context = new PipelineContext(/* minimal context */);

        // Act
        var result = await stage.ExecuteAsync(context);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Exception);
    }

    private PipelineContext CreateTestContext()
    {
        return new PipelineContext(
            correlationId: Guid.NewGuid().ToString(),
            brief: /* create test brief */,
            planSpec: /* create test plan */,
            voiceSpec: /* create test voice */,
            renderSpec: /* create test render */,
            systemProfile: /* create test profile */
        );
    }
}
```

## Common Patterns

### Pattern 1: External Service Call

```csharp
protected override async Task ExecuteStageAsync(...)
{
    try
    {
        var result = await _externalService.CallAsync(..., ct);
        context.SetStageOutput(StageName, result);
    }
    catch (HttpRequestException ex) when (IsTransient(ex))
    {
        // Let retry logic handle transient errors
        throw;
    }
    catch (Exception ex)
    {
        // Log and rethrow permanent errors
        Logger.LogError(ex, "Permanent error in {Stage}", StageName);
        throw;
    }
}
```

### Pattern 2: Multi-Step Processing

```csharp
protected override async Task ExecuteStageAsync(...)
{
    // Step 1
    ReportProgress(progress, 0, "Step 1: Preparing...");
    var prepared = await PrepareAsync(ct);
    
    // Step 2
    ReportProgress(progress, 33, "Step 2: Processing...");
    var processed = await ProcessAsync(prepared, ct);
    
    // Step 3
    ReportProgress(progress, 66, "Step 3: Finalizing...");
    var finalized = await FinalizeAsync(processed, ct);
    
    ReportProgress(progress, 100, "Completed");
    context.SetStageOutput(StageName, finalized);
}
```

### Pattern 3: Conditional Logic

```csharp
protected override async Task ExecuteStageAsync(...)
{
    if (ShouldSkip(context))
    {
        Logger.LogInformation("Skipping {Stage} - conditions not met", StageName);
        context.SetStageOutput(StageName, CreateSkippedOutput());
        return;
    }

    // Normal execution
    var result = await ExecuteLogicAsync(context, ct);
    context.SetStageOutput(StageName, result);
}
```

## Troubleshooting

### Stage Keeps Retrying
**Problem**: Stage continuously retries without success
**Solutions**:
- Check if errors are correctly classified (transient vs permanent)
- Reduce `MaxRetryAttempts`
- Add specific error handling for known failure modes

### Stage Times Out
**Problem**: Stage exceeds timeout duration
**Solutions**:
- Increase `Timeout` value
- Optimize expensive operations
- Break into multiple smaller stages

### Progress Not Updating
**Problem**: UI doesn't show stage progress
**Solutions**:
- Ensure `ReportProgress` is called regularly
- Check progress parameter is not null
- Verify `ProgressWeight` is set appropriately

### Can't Resume After Failure
**Problem**: Pipeline always restarts from beginning
**Solutions**:
- Implement `CanSkipStage` properly
- Ensure stage output is stored in context
- Set `SupportsResume = true`

## Additional Resources

- [Pipeline Architecture Documentation](./PIPELINE_ARCHITECTURE.md)
- Video Orchestrator Implementation
- Example Stage Implementations
- Unit Tests
