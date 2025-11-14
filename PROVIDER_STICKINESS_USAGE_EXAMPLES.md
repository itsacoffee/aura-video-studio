# Provider Stickiness System - Usage Examples

## Overview

This document provides practical examples of using the provider stickiness infrastructure for patience-centric provider management.

## Basic Setup

### 1. Load Configuration

```csharp
using Aura.Core.Services.Providers.Stickiness;
using Microsoft.Extensions.Logging;

// Load timeout profiles from JSON
var loader = new ProviderTimeoutProfileLoader(logger);
var config = loader.LoadConfiguration();

// Get profile for a specific provider
var ollamaProfile = loader.GetProfileForProvider("Ollama", patienceProfile: "longForm");
```

### 2. Initialize Gateway

```csharp
// Create stall detector
var stallDetector = new StallDetector(logger, checkIntervalMs: 5000);

// Create provider gateway
var gateway = new ProviderGateway(logger, stallDetector);

// Subscribe to events
gateway.StallSuspected += OnStallDetected;
gateway.FallbackDecisionMade += OnFallbackDecision;
```

## Example 1: Lock Provider for Job

```csharp
// User selects Ollama for their video generation job
var providerLock = gateway.LockProvider(
    jobId: "job-12345",
    providerName: "Ollama",
    providerType: "local_llm",
    correlationId: "corr-67890",
    isOverrideable: true, // Allow user to switch if needed
    "script-generation", "refinement" // Stages governed by this lock
);

Console.WriteLine($"Provider locked: {providerLock}");
// Output: ProviderLock[Ollama (local_llm) for Job job-12345, Stages: script-generation, refinement, Active]
```

## Example 2: Execute Operation with Patience

```csharp
// Define heartbeat strategy for LLM streaming
var tokenCount = 0;
var heartbeatStrategy = new LlmStreamingHeartbeatStrategy(
    getTokenCount: async () => 
    {
        // Your logic to get current token count from provider
        return await GetCurrentTokenCountAsync();
    },
    heartbeatIntervalMs: 15000, // Check every 15s
    stallMultiplier: 3 // Stall if no heartbeat for 45s
);

// Execute with patience monitoring
try
{
    var result = await gateway.ExecuteWithPatienceAsync(
        jobId: "job-12345",
        providerName: "Ollama",
        providerType: "local_llm",
        stageName: "script-generation",
        correlationId: "corr-67890",
        heartbeatStrategy: heartbeatStrategy,
        operation: async (ct) =>
        {
            // Your actual LLM call
            var script = await llmProvider.DraftScriptAsync(brief, planSpec, ct);
            return script;
        },
        ct: cancellationToken
    );

    Console.WriteLine($"Script generated: {result.Length} characters");
}
catch (Exception ex)
{
    Console.WriteLine($"Operation failed: {ex.Message}");
}
```

## Example 3: Handle Stall Detection

```csharp
private void OnStallDetected(object? sender, StallSuspectedEvent e)
{
    Console.WriteLine($"⚠️ Stall suspected for {e.ProviderName}");
    Console.WriteLine($"   No heartbeat for {e.ElapsedSinceLastHeartbeat.TotalSeconds:F1}s");
    Console.WriteLine($"   Total elapsed: {e.TotalElapsed.TotalSeconds:F1}s");

    // Present user dialog (pseudo-code)
    var userChoice = ShowStallDialog(new StallDialogOptions
    {
        ProviderName = e.ProviderName,
        ElapsedTime = e.TotalElapsed,
        Options = new[]
        {
            "Continue Waiting (30s)",
            "Continue Waiting (5 min)",
            "Try Alternative Provider",
            "Cancel Job"
        }
    });

    if (userChoice == "Try Alternative Provider")
    {
        // Show fallback panel to user
        ShowFallbackPanel(e.CorrelationId);
    }
}
```

## Example 4: User-Initiated Fallback

```csharp
// User explicitly decides to switch providers
public async Task<bool> SwitchProviderAsync(
    string jobId,
    string fromProvider,
    string toProvider,
    long elapsedMs,
    string correlationId)
{
    // Show confirmation dialog
    var confirmed = await ShowConfirmationDialogAsync(
        $"Switch from {fromProvider} to {toProvider}?",
        "Switching providers may alter the output style and tone. " +
        "Previous work will be preserved but future stages will use the new provider."
    );

    if (!confirmed)
        return false;

    // Create fallback decision
    var decision = FallbackDecision.CreateUserRequested(
        jobId,
        fromProvider,
        toProvider,
        elapsedMs,
        correlationId,
        new[] { "script-generation", "refinement" }
    );

    // Record the decision
    gateway.RecordFallbackDecision(decision);

    // Lock new provider
    var newLock = gateway.LockProvider(
        jobId,
        toProvider,
        "cloud_llm",
        correlationId,
        isOverrideable: true
    );

    return true;
}
```

## Example 5: Query Provider State

```csharp
// Get current state of a provider operation
var state = gateway.GetProviderState(correlationId);

if (state != null)
{
    Console.WriteLine($"Provider: {state.ProviderName}");
    Console.WriteLine($"Category: {state.CurrentCategory}");
    Console.WriteLine($"Elapsed: {state.ElapsedTime.TotalSeconds:F1}s");
    Console.WriteLine($"Heartbeats: {state.HeartbeatCount}");

    if (state.LastHeartbeat.HasValue)
    {
        Console.WriteLine($"Last heartbeat: {state.TimeSinceLastHeartbeat!.Value.TotalSeconds:F1}s ago");
    }

    if (state.Progress != null)
    {
        Console.WriteLine($"Progress: {state.Progress}");
    }
}
```

## Example 6: Get Fallback History

```csharp
// Retrieve all fallback decisions for a job
var history = gateway.GetFallbackHistory(jobId);

Console.WriteLine($"Fallback history for job {jobId}:");
foreach (var decision in history)
{
    Console.WriteLine($"  {decision.Timestamp:HH:mm:ss} - {decision.FromProvider} → {decision.ToProvider}");
    Console.WriteLine($"    Reason: {decision.ReasonCode}");
    Console.WriteLine($"    User confirmed: {decision.UserConfirmed}");
    Console.WriteLine($"    Elapsed: {decision.ElapsedBeforeSwitchMs}ms");
}
```

## Example 7: TTS with Heartbeat

```csharp
// TTS provider with chunk-based heartbeat
var chunkCount = 0;
var ttsHeartbeat = new TtsChunkHeartbeatStrategy(
    getChunkCount: async () =>
    {
        // Your logic to track TTS chunk generation
        return await GetTtsChunkCountAsync();
    },
    heartbeatIntervalMs: 10000,
    stallMultiplier: 3
);

var audioPath = await gateway.ExecuteWithPatienceAsync(
    jobId,
    "ElevenLabs",
    "tts",
    "tts-synthesis",
    correlationId,
    ttsHeartbeat,
    async (ct) =>
    {
        return await ttsProvider.SynthesizeAsync(lines, voiceSpec, ct);
    },
    cancellationToken
);
```

## Example 8: Image Generation with Percentage Progress

```csharp
// Image generation with percentage-based heartbeat
var currentProgress = 0.0;
var imageHeartbeat = new PercentageHeartbeatStrategy(
    getPercentComplete: async () =>
    {
        // Poll progress from image generation service
        return await GetImageGenerationProgressAsync();
    },
    heartbeatIntervalMs: 20000,
    stallMultiplier: 2
);

var imagePaths = await gateway.ExecuteWithPatienceAsync(
    jobId,
    "StableDiffusion",
    "image_gen",
    "visual-generation",
    correlationId,
    imageHeartbeat,
    async (ct) =>
    {
        return await imageProvider.FetchOrGenerateAsync(scene, visualSpec, ct);
    },
    cancellationToken
);
```

## Example 9: Provider Without Heartbeat Support

```csharp
// Fallback provider without heartbeat capability
var noHeartbeat = new NoHeartbeatStrategy();

var result = await gateway.ExecuteWithPatienceAsync(
    jobId,
    "RuleBased",
    "fallback_provider",
    "script-generation",
    correlationId,
    noHeartbeat, // No progress monitoring, just basic timeout
    async (ct) =>
    {
        return await ruleBasedProvider.DraftScriptAsync(brief, planSpec, ct);
    },
    cancellationToken
);
```

## Example 10: Gateway Statistics

```csharp
// Get gateway statistics for monitoring
var stats = gateway.GetStatistics();

Console.WriteLine($"Gateway Statistics:");
Console.WriteLine($"  Active Locks: {stats.ActiveLocks}");
Console.WriteLine($"  Active Operations: {stats.ActiveOperations}");
Console.WriteLine($"  Total Fallbacks: {stats.TotalFallbackDecisions}");
Console.WriteLine($"  User-Requested: {stats.UserRequestedFallbacks}");
Console.WriteLine($"  Error-Triggered: {stats.ErrorTriggeredFallbacks}");

// Fallback rate should be low in a well-functioning system
var fallbackRate = stats.ActiveLocks > 0 
    ? (double)stats.TotalFallbackDecisions / stats.ActiveLocks 
    : 0;
Console.WriteLine($"  Fallback Rate: {fallbackRate:P1}");
```

## Example 11: Release Lock After Job Completion

```csharp
try
{
    // Job execution...
    await GenerateVideoAsync(jobId);
}
finally
{
    // Always release lock when job completes
    var released = gateway.ReleaseProviderLock(jobId);
    if (released)
    {
        Console.WriteLine($"Provider lock released for job {jobId}");
    }
}
```

## Example 12: Apply Patience Profile

```csharp
// Load profile with user's patience preference
var conservativeProfile = loader.GetProfileForProvider(
    "Ollama",
    patienceProfile: "conservative" // Quick results preference
);

var longFormProfile = loader.GetProfileForProvider(
    "Ollama",
    patienceProfile: "longForm" // Maximum patience for complex content
);

Console.WriteLine("Conservative profile:");
Console.WriteLine($"  Extended threshold: {conservativeProfile.ExtendedThresholdMs}ms");

Console.WriteLine("Long-form profile:");
Console.WriteLine($"  Extended threshold: {longFormProfile.ExtendedThresholdMs}ms");
```

## Integration with Existing Orchestrator

```csharp
public class VideoOrchestrator
{
    private readonly ProviderGateway _providerGateway;
    private readonly ProviderTimeoutProfileLoader _profileLoader;

    public async Task<string> GenerateVideoAsync(
        Brief brief,
        PlanSpec planSpec,
        string preferredProvider,
        CancellationToken ct)
    {
        var jobId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();

        // Lock provider for entire job
        var providerLock = _providerGateway.LockProvider(
            jobId,
            preferredProvider,
            "local_llm",
            correlationId,
            isOverrideable: true
        );

        try
        {
            // Get timeout profile
            var profile = _profileLoader.GetProfileForProvider(
                preferredProvider,
                patienceProfile: "balanced"
            );

            // Create heartbeat strategy
            var heartbeat = CreateHeartbeatStrategy(preferredProvider, profile);

            // Execute script generation with patience
            var script = await _providerGateway.ExecuteWithPatienceAsync(
                jobId,
                preferredProvider,
                "local_llm",
                "script-generation",
                correlationId,
                heartbeat,
                async (ct) => await GenerateScriptAsync(brief, planSpec, ct),
                ct
            );

            // Continue with other stages...
            // All use the same locked provider

            return outputPath;
        }
        finally
        {
            _providerGateway.ReleaseProviderLock(jobId);
        }
    }
}
```

## Best Practices

1. **Always Lock Provider First**: Call `LockProvider()` before any stage execution
2. **Use Appropriate Heartbeat Strategy**: Match strategy to provider capabilities
3. **Handle Stall Events**: Present clear options to users, never auto-switch
4. **Release Locks**: Use try-finally to ensure locks are released
5. **Apply Patience Profiles**: Respect user's patience preferences
6. **Log All Decisions**: Fallback decisions provide valuable audit trail
7. **Monitor Statistics**: Track fallback rates to identify provider issues

## See Also

- [LATENCY_PATIENCE_POLICY.md](LATENCY_PATIENCE_POLICY.md) - Full policy documentation
- [PROVIDER_INTEGRATION_GUIDE.md](PROVIDER_INTEGRATION_GUIDE.md) - Provider implementation guide
- [providerTimeoutProfiles.json](providerTimeoutProfiles.json) - Configuration reference
