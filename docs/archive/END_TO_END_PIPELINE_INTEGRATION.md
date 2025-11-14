# End-to-End Video Generation Pipeline Integration

## Overview

This implementation integrates the smart orchestration system (VideoGenerationOrchestrator) into the main VideoOrchestrator to enable efficient, parallel video generation with intelligent task scheduling and resource management.

## Key Changes

### 1. VideoOrchestrator Integration

**File**: `Aura.Core/Orchestrator/VideoOrchestrator.cs`

Added dependencies:
- `VideoGenerationOrchestrator` - Smart task scheduling and dependency management
- `ResourceMonitor` - System resource monitoring for optimal concurrency
- `IImageProvider` - Optional image generation support

New overload of `GenerateVideoAsync` that accepts `SystemProfile`:
```csharp
public async Task<string> GenerateVideoAsync(
    Brief brief,
    PlanSpec planSpec,
    VoiceSpec voiceSpec,
    RenderSpec renderSpec,
    SystemProfile systemProfile,
    IProgress<string>? progress = null,
    CancellationToken ct = default)
```

This method:
1. Creates a task executor that maps generation tasks to providers
2. Uses smart orchestration for parallel task execution
3. Handles task dependencies automatically (script → audio → composition)
4. Reports progress via OrchestrationProgress events
5. Extracts final video path from composition task result

### 2. Task Executor

The `CreateTaskExecutor` method maps generation tasks to providers:

- **ScriptGeneration**: Calls LLM provider to generate script
- **AudioGeneration**: Synthesizes audio from parsed scenes using TTS
- **ImageGeneration**: Generates visual assets for each scene (optional)
- **VideoComposition**: Renders final video combining all assets

State is maintained across tasks to avoid redundant parsing and processing.

### 3. Enhanced OrchestrationResult

**File**: `Aura.Core/Services/Generation/VideoGenerationOrchestrator.cs`

Added to `OrchestrationResult`:
- `TaskResults` - Dictionary of task results for accessing outputs
- `FailureReasons` - List of error messages from failed tasks

This enables better error reporting and result extraction.

### 4. Dependency Injection

**Files**:
- `Aura.Api/Program.cs`
- `Aura.App/App.xaml.cs`

Registered new services:
```csharp
builder.Services.AddSingleton<ResourceMonitor>();
builder.Services.AddSingleton<StrategySelector>();
builder.Services.AddSingleton<VideoGenerationOrchestrator>();
```

These must be registered before `VideoOrchestrator` since it depends on them.

## Benefits

### Smart Orchestration
- **Parallel execution**: Images can be generated in parallel while maintaining dependencies
- **Resource-aware**: Adjusts concurrency based on system capabilities
- **Strategy selection**: Chooses optimal generation approach based on content and hardware

### Error Recovery
- **Task-level retry**: Failed tasks can be retried independently
- **Detailed failure reporting**: Each failed task provides specific error information
- **Graceful degradation**: Critical failures trigger recovery attempts

### Progress Reporting
- **Stage-based progress**: Users see which stage is executing
- **Percentage completion**: Real-time progress percentage based on task completion
- **Execution time tracking**: Total time and per-task timing available

## Testing

### Integration Tests

**File**: `Aura.Tests/VideoOrchestratorIntegrationTests.cs`

Two key test scenarios:
1. `GenerateVideoAsync_WithSystemProfile_UsesSmartOrchestration` - Validates smart orchestration path
2. `GenerateVideoAsync_WithoutSystemProfile_UsesFallbackOrchestration` - Validates backward compatibility

Both tests use mock providers to validate the integration without requiring actual LLM/TTS/FFmpeg.

### Test Results
- All VideoGenerationOrchestrator tests: ✓ Passing (7/7)
- All VideoOrchestrator integration tests: ✓ Passing (2/2)

## Backward Compatibility

The original `GenerateVideoAsync` method (without SystemProfile parameter) remains unchanged, ensuring existing code continues to work. The smart orchestration is opt-in via the new overload.

## Usage Example

```csharp
var brief = new Brief("AI Revolution", null, null, "Professional", "English", Aspect.Widescreen16x9);
var planSpec = new PlanSpec(TimeSpan.FromMinutes(2), Pacing.Conversational, Density.Balanced, "Modern");
var voiceSpec = new VoiceSpec("en-US-AriaNeural", 1.0, 1.0, PauseStyle.Natural);
var renderSpec = new RenderSpec(new Resolution(1920, 1080), "mp4", 5000, 192);
var systemProfile = new SystemProfile 
{ 
    Tier = HardwareTier.B, 
    LogicalCores = 4, 
    RamGB = 8 
};

var progress = new Progress<string>(msg => Console.WriteLine(msg));

var outputPath = await orchestrator.GenerateVideoAsync(
    brief, planSpec, voiceSpec, renderSpec, systemProfile, progress);
```

## Implementation Notes

### Timeline Namespace Conflict
The `Timeline` record in `Aura.Core.Providers` conflicts with the `Aura.Core.Timeline` namespace. Used fully-qualified name `Providers.Timeline` to resolve.

### Health Check Updates
Health endpoint tests updated to be flexible with service count, as adding new services increases the number of health checks.

## Future Enhancements

Potential improvements:
1. Provider mixing - use different providers for different tasks
2. Caching - reuse previously generated assets
3. Quality settings - trade speed for quality based on user preference
4. Fallback strategies - automatic provider fallback on failure
