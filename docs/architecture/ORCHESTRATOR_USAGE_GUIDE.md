# Orchestrator Usage Guide

## Overview

This guide explains how to properly use the orchestrator layer for all provider calls in Aura Video Studio. Direct provider usage is now forbidden in application layers to ensure middleware coverage (content safety, telemetry, retry/backoff) for all generation and search routes.

## Why Orchestrator-Only Access?

The orchestrator pattern provides critical benefits:

1. **Middleware Coverage**: All operations automatically get content safety checks, telemetry collection, and retry logic
2. **Consistent Error Handling**: Unified error handling and reporting across all providers
3. **Budget Management**: Automatic cost and token tracking with configurable limits
4. **Caching**: Automatic response caching for deterministic operations
5. **Observability**: Comprehensive telemetry for monitoring and debugging
6. **Fallback Chains**: Automatic provider failover for improved reliability

## Analyzer Rule AUR001

The `AUR001` analyzer rule enforces this pattern:

**Rule**: Direct usage of provider interfaces (`ILlmProvider`, `ITtsProvider`, `IImageProvider`, etc.) is forbidden outside orchestrator namespaces.

**Severity**: Error in CI, Warning in development

**Allowed Namespaces**:
- `Aura.Core.Orchestration.*` - Orchestration infrastructure
- `Aura.Core.Orchestrator.*` - Legacy orchestrator (being migrated)
- `Aura.Core.AI.Orchestration.*` - AI orchestration layer
- `Aura.Providers.*` - Provider implementations
- `Aura.Api.Startup.*` - Dependency injection registration
- `Aura.Tests.*` - Test projects can mock providers
- `Aura.E2E.*` - E2E tests

## How to Use Orchestrator

### For LLM Operations

Use `UnifiedLlmOrchestrator` or `LlmStageAdapter`:

#### UnifiedLlmOrchestrator

```csharp
public class MyService
{
    private readonly UnifiedLlmOrchestrator _orchestrator;
    private readonly ILlmProvider _provider;  // Injected but passed to orchestrator
    
    public MyService(
        UnifiedLlmOrchestrator orchestrator,
        ILlmProvider provider)
    {
        _orchestrator = orchestrator;
        _provider = provider;
    }
    
    public async Task<string> GenerateContentAsync(
        string prompt,
        string sessionId,
        CancellationToken ct)
    {
        var request = new LlmOperationRequest
        {
            SessionId = sessionId,
            OperationType = LlmOperationType.Completion,
            Prompt = prompt,
            EnableCache = true
        };
        
        var response = await _orchestrator.ExecuteAsync(request, _provider, ct);
        
        if (!response.Success)
        {
            throw new InvalidOperationException(
                $"LLM operation failed: {response.ErrorMessage}");
        }
        
        return response.Content;
    }
}
```

#### LlmStageAdapter (for script generation)

```csharp
public class MyScriptService
{
    private readonly LlmStageAdapter _stageAdapter;
    
    public MyScriptService(LlmStageAdapter stageAdapter)
    {
        _stageAdapter = stageAdapter;
    }
    
    public async Task<Script> GenerateScriptAsync(
        Brief brief,
        PlanSpec spec,
        CancellationToken ct)
    {
        var result = await _stageAdapter.GenerateScriptAsync(
            brief,
            spec,
            preferredTier: "Free",
            offlineOnly: false,
            ct);
        
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Script generation failed: {result.ErrorMessage}");
        }
        
        return ParseScript(result.Data);
    }
}
```

### For TTS Operations

Use `SSMLStageAdapter`:

```csharp
public class MyTtsService
{
    private readonly SSMLStageAdapter _ssmlAdapter;
    
    public MyTtsService(SSMLStageAdapter ssmlAdapter)
    {
        _ssmlAdapter = ssmlAdapter;
    }
    
    public async Task<SSMLPlanningResult> GenerateSSMLAsync(
        List<ScriptLine> lines,
        VoiceSpec voiceSpec,
        CancellationToken ct)
    {
        var targetDurations = lines.Select(l => l.Duration).ToList();
        
        var result = await _ssmlAdapter.GenerateSSMLAsync(
            lines,
            voiceSpec,
            targetDurations,
            VoiceProvider.ElevenLabs,
            ct);
        
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(
                $"SSML generation failed: {result.ErrorMessage}");
        }
        
        return result.Data;
    }
}
```

### For Visual Generation

Use `VisualStageAdapter`:

```csharp
public class MyVisualService
{
    private readonly VisualStageAdapter _visualAdapter;
    
    public MyVisualService(VisualStageAdapter visualAdapter)
    {
        _visualAdapter = visualAdapter;
    }
    
    public async Task<VisualPrompt> GenerateVisualPromptAsync(
        Scene scene,
        VisualStyle style,
        CancellationToken ct)
    {
        var result = await _visualAdapter.GenerateVisualPromptAsync(
            scene,
            style,
            ct);
        
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Visual prompt generation failed: {result.ErrorMessage}");
        }
        
        return result.Data;
    }
}
```

## Middleware Architecture

All operations through the orchestrator automatically get:

### 1. Telemetry Collection

Every operation is tracked with:
- Operation ID and session ID
- Provider and model used
- Token counts (input/output)
- Latency metrics
- Cost estimates
- Success/failure status
- Cache hit/miss

**Access telemetry**:
```csharp
var stats = _orchestrator.GetStatistics();
Console.WriteLine($"Total operations: {stats.TotalOperations}");
Console.WriteLine($"Cache hit rate: {stats.CacheHitRate:P}");
Console.WriteLine($"Average latency: {stats.AverageLatencyMs}ms");
Console.WriteLine($"Total cost: ${stats.TotalEstimatedCost:F4}");
```

### 2. Budget Management

Set limits on tokens and cost:
```csharp
var budgetConstraint = new LlmBudgetConstraint
{
    MaxTokensPerOperation = 5000,
    MaxCostPerOperation = 0.50m,
    MaxTokensPerSession = 50000,
    MaxCostPerSession = 5.00m,
    EnforceHardLimits = true
};

var request = new LlmOperationRequest
{
    SessionId = sessionId,
    OperationType = LlmOperationType.Planning,
    Prompt = prompt,
    BudgetConstraint = budgetConstraint
};
```

### 3. Retry Logic with Backoff

Automatic retry for transient failures:
```csharp
var request = new LlmOperationRequest
{
    OperationType = LlmOperationType.Completion,
    Prompt = prompt,
    CustomPreset = new LlmOperationPreset
    {
        MaxRetries = 3,
        TimeoutSeconds = 30
    }
};
```

The orchestrator will:
- Retry up to MaxRetries times
- Use exponential backoff between retries
- Track retry count in telemetry

### 4. Response Caching

Automatic caching for deterministic operations:
```csharp
var request = new LlmOperationRequest
{
    OperationType = LlmOperationType.SceneAnalysis,
    Prompt = prompt,
    EnableCache = true,
    CacheTtlSeconds = 3600  // 1 hour
};
```

First call hits the provider, subsequent identical calls return cached result.

### 5. Content Safety (Future)

Content safety middleware will be added to:
- Filter inappropriate prompts
- Validate response content
- Apply policy constraints
- Log safety violations

## Migration Guide

### Step 1: Identify Direct Provider Usage

The `AUR001` analyzer will flag all direct provider usage:

```
error AUR001: Direct usage of 'ILlmProvider' is not allowed. 
Use orchestrator layer instead (LlmStageAdapter, SSMLStageAdapter, VisualStageAdapter)
```

### Step 2: Add Orchestrator Dependency

**Before**:
```csharp
public class MyService
{
    private readonly ILlmProvider _llmProvider;
    
    public MyService(ILlmProvider llmProvider)
    {
        _llmProvider = llmProvider;
    }
    
    public async Task<string> ProcessAsync(string input, CancellationToken ct)
    {
        return await _llmProvider.CompleteAsync(input, ct);
    }
}
```

**After**:
```csharp
public class MyService
{
    private readonly UnifiedLlmOrchestrator _orchestrator;
    private readonly ILlmProvider _provider;
    
    public MyService(
        UnifiedLlmOrchestrator orchestrator,
        ILlmProvider provider)
    {
        _orchestrator = orchestrator;
        _provider = provider;
    }
    
    public async Task<string> ProcessAsync(
        string input,
        string sessionId,
        CancellationToken ct)
    {
        var request = new LlmOperationRequest
        {
            SessionId = sessionId,
            OperationType = LlmOperationType.Completion,
            Prompt = input,
            EnableCache = false
        };
        
        var response = await _orchestrator.ExecuteAsync(request, _provider, ct);
        
        if (!response.Success)
        {
            throw new InvalidOperationException(response.ErrorMessage);
        }
        
        return response.Content;
    }
}
```

### Step 3: Update Dependency Injection

Ensure orchestrator dependencies are registered:

```csharp
// In Startup or Program.cs
services.AddSingleton<ILlmCache, MemoryLlmCache>();
services.AddSingleton<LlmBudgetManager>();
services.AddSingleton<LlmTelemetryCollector>();
services.AddSingleton<SchemaValidator>();
services.AddSingleton<UnifiedLlmOrchestrator>();

// Stage adapters
services.AddSingleton<LlmStageAdapter>();
services.AddSingleton<SSMLStageAdapter>();
services.AddSingleton<VisualStageAdapter>();
```

### Step 4: Update Tests

Tests can continue to mock providers directly:

```csharp
[Fact]
public async Task MyTest()
{
    var mockProvider = new Mock<ILlmProvider>();
    mockProvider.Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync("Test response");
    
    var orchestrator = new UnifiedLlmOrchestrator(...);
    var service = new MyService(orchestrator, mockProvider.Object);
    
    var result = await service.ProcessAsync("test", "session", CancellationToken.None);
    
    Assert.Equal("Test response", result);
}
```

## Best Practices

### 1. Always Use Session IDs

Group related operations under a single session ID for accurate budget tracking:

```csharp
var sessionId = $"video-{videoId}";

await GeneratePlanAsync(sessionId, ...);
await GenerateScriptAsync(sessionId, ...);
await GenerateVisualsAsync(sessionId, ...);

// Clear session when complete
orchestrator.ClearSession(sessionId);
```

### 2. Enable Caching for Deterministic Operations

```csharp
// Cache deterministic operations
var analysisRequest = new LlmOperationRequest
{
    OperationType = LlmOperationType.SceneAnalysis,
    EnableCache = true,
    CacheTtlSeconds = 3600
};

// Don't cache creative operations
var creativeRequest = new LlmOperationRequest
{
    OperationType = LlmOperationType.Creative,
    EnableCache = false
};
```

### 3. Set Appropriate Budgets

```csharp
// Set conservative budgets for user-facing operations
var userConstraint = new LlmBudgetConstraint
{
    MaxTokensPerSession = 50000,
    MaxCostPerSession = 2.00m,
    EnforceHardLimits = true
};

// More generous budgets for background processing
var backgroundConstraint = new LlmBudgetConstraint
{
    MaxTokensPerSession = 200000,
    MaxCostPerSession = 10.00m,
    EnforceHardLimits = false  // Log warnings only
};
```

### 4. Handle Errors Gracefully

```csharp
var response = await orchestrator.ExecuteAsync(request, provider, ct);

if (!response.Success)
{
    _logger.LogWarning(
        "LLM operation failed: {Error} (Operation: {OpId})",
        response.ErrorMessage,
        response.Telemetry.OperationId);
    
    // Handle specific errors
    if (response.ErrorMessage?.Contains("Budget exceeded") == true)
    {
        await NotifyUserAsync("Operation paused due to budget limit");
        return new Result { RequiresBudgetIncrease = true };
    }
    
    // Generic fallback
    throw new InvalidOperationException(response.ErrorMessage);
}
```

### 5. Monitor Telemetry

```csharp
// Periodic monitoring
var stats = orchestrator.GetStatistics();

if (stats.CacheHitRate < 0.3)
{
    _logger.LogWarning("Low cache hit rate: {Rate:P}", stats.CacheHitRate);
}

if (stats.P95LatencyMs > 5000)
{
    _logger.LogWarning("High P95 latency: {Latency}ms", stats.P95LatencyMs);
}

if (stats.TotalEstimatedCost > 100.0m)
{
    _logger.LogWarning("High cost accumulated: ${Cost:F2}", stats.TotalEstimatedCost);
}
```

## Troubleshooting

### Build Error: AUR001

**Problem**: CI fails with `error AUR001: Direct usage of 'ILlmProvider' is not allowed`

**Solution**:
1. Identify the service using direct provider access
2. Add `UnifiedLlmOrchestrator` or `LlmStageAdapter` dependency
3. Route calls through orchestrator as shown in this guide
4. Rebuild and verify

### Provider Not Working

**Problem**: Orchestrator returns errors when provider works directly

**Solution**:
1. Check that provider is registered in DI container
2. Verify API keys are configured
3. Check telemetry for specific error details:
   ```csharp
   var telemetry = response.Telemetry;
   _logger.LogError("Provider: {Provider}, Error: {Error}", 
       telemetry.ProviderName, telemetry.ErrorMessage);
   ```

### High Costs

**Problem**: Operations consuming too many tokens/cost

**Solution**:
1. Review telemetry to identify expensive operations:
   ```csharp
   var stats = orchestrator.GetStatistics();
   foreach (var (provider, count) in stats.OperationsByProvider)
   {
       _logger.LogInformation("{Provider}: {Count} ops", provider, count);
   }
   ```
2. Set budget constraints
3. Use cheaper models for non-critical operations
4. Enable caching for repeated operations

### Low Cache Hit Rate

**Problem**: Expected cache hits not occurring

**Solution**:
1. Verify prompts are normalized consistently
2. Check cache TTL settings
3. Consider increasing cache TTL:
   ```csharp
   request.CacheTtlSeconds = 7200;  // 2 hours
   ```
4. Monitor cache size and eviction

## Related Documentation

- [Orchestrator Unification Summary](ORCHESTRATOR_UNIFICATION_SUMMARY.md)
- [Unified LLM Orchestrator Guide](UNIFIED_LLM_ORCHESTRATOR_GUIDE.md)
- [LLM Cache Guide](LLM_CACHE_GUIDE.md)
- [Provider Integration Guide](PROVIDER_INTEGRATION_GUIDE.md)

## Support

For questions or issues with orchestrator usage:
1. Check existing tests in `Aura.Tests/OrchestratorMiddlewareTests.cs`
2. Review orchestrator implementation in `Aura.Core/AI/Orchestration/`
3. Consult related documentation above
4. Create an issue with `orchestrator` label
