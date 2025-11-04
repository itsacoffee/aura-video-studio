# Unified LLM Orchestrator Guide

## Overview

The Unified LLM Orchestrator provides centralized control over all LLM operations in Aura Video Studio, including:

- **Prompt Governance**: Standardized parameter presets per operation type
- **Budget Management**: Token and cost tracking with hard/soft limits
- **Telemetry**: Comprehensive metrics for all LLM calls
- **Caching**: Automatic response caching with TTL
- **Cost Estimation**: Real-time cost tracking per operation
- **Parameter Optimization**: LLM-assisted or rule-based parameter suggestions

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                         │
└─────────────────┬───────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────────┐
│              UnifiedLlmOrchestrator                          │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  • LlmOperationRequest → LlmOperationResponse        │  │
│  │  • Session budget tracking                            │  │
│  │  • Telemetry collection                               │  │
│  │  • Cache integration                                  │  │
│  └──────────────────────────────────────────────────────┘  │
└────┬───────┬──────────┬────────────┬────────────┬──────────┘
     │       │          │            │            │
     ▼       ▼          ▼            ▼            ▼
┌────────┐ ┌──────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐
│ Budget │ │Cache │ │Telemetry │ │  Cost    │ │ Provider │
│Manager │ │      │ │Collector │ │ Tracking │ │          │
└────────┘ └──────┘ └──────────┘ └──────────┘ └──────────┘
```

## Core Components

### 1. LlmOperationType and Presets

**Location**: `Aura.Core/AI/Orchestration/LlmOperationType.cs`

Defines 13 operation types with optimized parameter presets:

```csharp
public enum LlmOperationType
{
    Planning,              // Video plan generation (temp: 0.7, tokens: 2000)
    Scripting,             // Script content (temp: 0.8, tokens: 4000)
    SsmlPlanning,          // SSML markup (temp: 0.3, tokens: 3000)
    VisualPrompts,         // Visual generation (temp: 0.9, tokens: 1500)
    RagRetrieval,          // RAG content (temp: 0.2, tokens: 2000)
    SceneAnalysis,         // Scene importance (temp: 0.3, tokens: 800)
    ComplexityAnalysis,    // Content complexity (temp: 0.2, tokens: 1000)
    CoherenceValidation,   // Scene coherence (temp: 0.2, tokens: 800)
    NarrativeValidation,   // Narrative arc (temp: 0.3, tokens: 1500)
    TransitionGeneration,  // Transition text (temp: 0.7, tokens: 500)
    ScriptRefinement,      // Script editing (temp: 0.6, tokens: 3000)
    Creative,              // Creative generation (temp: 0.9, tokens: 3000)
    Completion             // General (temp: 0.7, tokens: 2000)
}
```

**Usage**:

```csharp
// Get preset for an operation
var preset = LlmOperationPresets.GetPreset(LlmOperationType.Planning);

// Create custom preset
var custom = LlmOperationPresets.CreateCustomPreset(
    LlmOperationType.Planning,
    temperature: 0.5,
    maxTokens: 3000);

// Get all presets
var allPresets = LlmOperationPresets.GetAllPresets();
```

### 2. Budget Management

**Location**: `Aura.Core/AI/Orchestration/LlmBudgetManager.cs`

Tracks token and cost budgets per session:

```csharp
// Configure budget constraints
var constraint = new LlmBudgetConstraint
{
    MaxTokensPerOperation = 5000,
    MaxCostPerOperation = 0.50m,
    MaxTokensPerSession = 50000,
    MaxCostPerSession = 5.00m,
    EnforceHardLimits = true  // Throw error vs. log warning
};

var budgetManager = new LlmBudgetManager(logger, constraint);

// Check budget before operation
var check = budgetManager.CheckBudget(
    sessionId: "video-123",
    estimatedTokens: 1000,
    estimatedCost: 0.05m);

if (!check.IsWithinBudget)
{
    Console.WriteLine($"Budget warnings: {string.Join(", ", check.Warnings)}");
}

// Record actual usage after operation
budgetManager.RecordUsage(
    sessionId: "video-123",
    actualTokens: 950,
    actualCost: 0.048m);

// Get session status
var budget = budgetManager.GetSessionBudget("video-123");
Console.WriteLine($"Session total: {budget.TotalTokensUsed} tokens, ${budget.TotalCostAccrued:F4}");

// Clear completed session
budgetManager.ClearSession("video-123");
```

### 3. Telemetry and Metrics

**Location**: `Aura.Core/AI/Orchestration/LlmTelemetry.cs`

Collects comprehensive metrics for every LLM operation:

```csharp
public record LlmOperationTelemetry
{
    public string OperationId { get; init; }
    public string SessionId { get; init; }
    public LlmOperationType OperationType { get; init; }
    public string ProviderName { get; init; }
    public string ModelName { get; init; }
    public int TokensIn { get; init; }
    public int TokensOut { get; init; }
    public int RetryCount { get; init; }
    public long LatencyMs { get; init; }
    public bool Success { get; init; }
    public bool CacheHit { get; init; }
    public decimal EstimatedCost { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime CompletedAt { get; init; }
    public double Temperature { get; init; }
    public double TopP { get; init; }
}
```

**Usage**:

```csharp
var collector = new LlmTelemetryCollector();

// Get statistics for all operations
var stats = collector.GetStatistics();
Console.WriteLine($"Total operations: {stats.TotalOperations}");
Console.WriteLine($"Cache hit rate: {stats.CacheHitRate:P}");
Console.WriteLine($"Total cost: ${stats.TotalEstimatedCost:F4}");
Console.WriteLine($"Average latency: {stats.AverageLatencyMs}ms");
Console.WriteLine($"P95 latency: {stats.P95LatencyMs}ms");

// Get statistics for specific session
var sessionStats = collector.GetSessionStatistics("video-123");

// Get operations by provider
foreach (var (provider, count) in stats.OperationsByProvider)
{
    Console.WriteLine($"{provider}: {count} operations");
}
```

### 4. Unified Orchestrator

**Location**: `Aura.Core/AI/Orchestration/UnifiedLlmOrchestrator.cs`

Central orchestrator coordinating all LLM operations:

```csharp
// Setup
var orchestrator = new UnifiedLlmOrchestrator(
    logger,
    cache,
    budgetManager,
    telemetryCollector,
    schemaValidator,
    costTrackingService);

// Execute operation
var request = new LlmOperationRequest
{
    SessionId = "video-123",
    OperationType = LlmOperationType.Planning,
    Prompt = "Create a plan for a video about...",
    SystemPrompt = "You are a video planning assistant",
    EnableCache = true,
    CacheTtlSeconds = 3600,
    BudgetConstraint = constraint
};

var response = await orchestrator.ExecuteAsync(request, llmProvider, ct);

if (response.Success)
{
    Console.WriteLine($"Content: {response.Content}");
    Console.WriteLine($"Cached: {response.WasCached}");
    Console.WriteLine($"Tokens: {response.Telemetry.TotalTokens}");
    Console.WriteLine($"Cost: ${response.Telemetry.EstimatedCost:F4}");
    Console.WriteLine($"Latency: {response.Telemetry.LatencyMs}ms");
}
else
{
    Console.WriteLine($"Error: {response.ErrorMessage}");
}
```

### 5. Parameter Optimization

**Location**: `Aura.Core/AI/Orchestration/LlmParameterOptimizer.cs`

Suggests optimal parameters based on constraints:

```csharp
var optimizer = new LlmParameterOptimizer(logger);

// Optimize parameters
var optimizationRequest = new OptimizationRequest
{
    OperationType = LlmOperationType.Planning,
    Constraints = new OptimizationConstraints
    {
        MaxTokens = 1500,
        MaxCost = 0.25m,
        MaxLatencySeconds = 30,
        PrioritizeQuality = true
    },
    UseCase = "Quick video planning for social media"
};

// Rule-based optimization
var suggestion = await optimizer.OptimizeAsync(optimizationRequest);

// LLM-assisted optimization (optional)
var llmSuggestion = await optimizer.OptimizeAsync(
    optimizationRequest,
    llmProvider,
    ct);

Console.WriteLine($"Suggested temperature: {suggestion.Temperature}");
Console.WriteLine($"Suggested tokens: {suggestion.MaxTokens}");
Console.WriteLine($"Rationale: {suggestion.Rationale}");
Console.WriteLine($"Confidence: {suggestion.Confidence:P}");

// Explain adjustments
var explanation = await optimizer.ExplainAdjustmentsAsync(
    basePreset,
    adjustedPreset,
    "cost constraints",
    llmProvider,
    ct);

Console.WriteLine($"Explanation: {explanation}");
```

## Integration Patterns

### Basic Integration

```csharp
public class VideoGenerationService
{
    private readonly UnifiedLlmOrchestrator _orchestrator;
    private readonly ILlmProvider _provider;
    
    public async Task<VideoScript> GenerateScriptAsync(
        VideoBrief brief,
        string sessionId,
        CancellationToken ct)
    {
        var request = new LlmOperationRequest
        {
            SessionId = sessionId,
            OperationType = LlmOperationType.Scripting,
            Prompt = BuildScriptPrompt(brief),
            EnableCache = true
        };
        
        var response = await _orchestrator.ExecuteAsync(request, _provider, ct);
        
        if (!response.Success)
        {
            throw new InvalidOperationException(
                $"Script generation failed: {response.ErrorMessage}");
        }
        
        return ParseScript(response.Content);
    }
}
```

### Multi-Provider Fallback

```csharp
public class ResilientVideoService
{
    private readonly UnifiedLlmOrchestrator _orchestrator;
    private readonly ILlmProvider[] _providers;  // Ordered by preference
    
    public async Task<string> GenerateWithFallbackAsync(
        LlmOperationRequest request,
        CancellationToken ct)
    {
        foreach (var provider in _providers)
        {
            try
            {
                var response = await _orchestrator.ExecuteAsync(request, provider, ct);
                
                if (response.Success)
                {
                    return response.Content;
                }
                
                // Log and try next provider
                _logger.LogWarning(
                    "Provider {Provider} failed: {Error}",
                    GetProviderName(provider),
                    response.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provider {Provider} threw exception", 
                    GetProviderName(provider));
            }
        }
        
        throw new InvalidOperationException("All providers failed");
    }
}
```

### Progressive Cost Control

```csharp
public class CostAwareService
{
    private readonly UnifiedLlmOrchestrator _orchestrator;
    
    public async Task<Result> GenerateWithCostControlAsync(
        string sessionId,
        CancellationToken ct)
    {
        // Start with basic budget
        var constraint = new LlmBudgetConstraint
        {
            MaxTokensPerSession = 10000,
            MaxCostPerSession = 1.00m,
            EnforceHardLimits = false
        };
        
        var request = new LlmOperationRequest
        {
            SessionId = sessionId,
            OperationType = LlmOperationType.Planning,
            Prompt = "...",
            BudgetConstraint = constraint
        };
        
        var response = await _orchestrator.ExecuteAsync(request, _provider, ct);
        
        // Check budget status
        var budget = _orchestrator.GetSessionBudget(sessionId);
        
        if (budget.TotalCostAccrued > 0.75m)
        {
            _logger.LogWarning(
                "Session {SessionId} approaching budget limit: ${Cost:F4}",
                sessionId, budget.TotalCostAccrued);
            
            // Switch to cheaper operation
            request = request with
            {
                CustomPreset = LlmOperationPresets.CreateCustomPreset(
                    request.OperationType,
                    maxTokens: 1000,
                    temperature: 0.5)
            };
        }
        
        // Continue with adjusted parameters...
    }
}
```

## Best Practices

### 1. Always Use Session IDs

Group related operations under a single session ID for accurate budget tracking:

```csharp
var sessionId = $"video-{videoId}";

// All operations for this video use the same session ID
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

### 3. Monitor Telemetry

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
```

### 4. Use Parameter Optimization

```csharp
// Let the system suggest optimal parameters
var suggestion = await optimizer.OptimizeAsync(
    new OptimizationRequest
    {
        OperationType = operationType,
        Constraints = constraints
    });

// Use suggested parameters
var request = new LlmOperationRequest
{
    CustomPreset = new LlmOperationPreset
    {
        Temperature = suggestion.Temperature,
        TopP = suggestion.TopP,
        MaxTokens = suggestion.MaxTokens,
        TimeoutSeconds = suggestion.TimeoutSeconds,
        MaxRetries = suggestion.MaxRetries
    }
};
```

### 5. Handle Budget Exhaustion Gracefully

```csharp
var response = await orchestrator.ExecuteAsync(request, provider, ct);

if (!response.Success && response.ErrorMessage?.Contains("Budget exceeded") == true)
{
    // Notify user
    await NotifyUserAsync("Operation paused due to budget limit");
    
    // Log for admin review
    _logger.LogWarning(
        "Session {SessionId} exhausted budget at {Cost:F4}",
        sessionId, budget.TotalCostAccrued);
    
    // Optionally, allow user to increase budget
    return new Result { RequiresBudgetIncrease = true };
}
```

## Diagnostic APIs

### Get Session Statistics

```csharp
[HttpGet("sessions/{sessionId}/statistics")]
public ActionResult<LlmTelemetryStatistics> GetSessionStatistics(string sessionId)
{
    var stats = _orchestrator.GetSessionStatistics(sessionId);
    return Ok(stats);
}
```

### Get Budget Status

```csharp
[HttpGet("sessions/{sessionId}/budget")]
public ActionResult<SessionBudget> GetSessionBudget(string sessionId)
{
    var budget = _orchestrator.GetSessionBudget(sessionId);
    return Ok(budget);
}
```

### Get Overall Statistics

```csharp
[HttpGet("statistics")]
public ActionResult<LlmTelemetryStatistics> GetStatistics()
{
    var stats = _orchestrator.GetStatistics();
    return Ok(stats);
}
```

## Migration from Direct LLM Calls

**Before**:
```csharp
var response = await llmProvider.CompleteAsync(prompt, ct);
```

**After**:
```csharp
var request = new LlmOperationRequest
{
    SessionId = sessionId,
    OperationType = LlmOperationType.Completion,
    Prompt = prompt
};

var response = await orchestrator.ExecuteAsync(request, llmProvider, ct);
var content = response.Content;
```

## Performance Considerations

1. **Caching**: First call incurs full latency, subsequent calls are ~10ms
2. **Budget checking**: Adds <1ms overhead per operation
3. **Telemetry**: Non-blocking, adds ~2ms overhead
4. **Cost estimation**: Simple calculation, <1ms overhead

## Troubleshooting

### High Cache Miss Rate

- Check if operations have low temperature (< 0.3)
- Verify prompts are normalized consistently
- Consider increasing cache TTL

### Budget Warnings Too Aggressive

- Increase session limits in `LlmBudgetConstraint`
- Set `EnforceHardLimits = false` for soft limits
- Review operation-specific token limits

### Unexpected Cost

- Review telemetry to identify expensive operations
- Check for retry storms (high `RetryCount`)
- Verify provider cost rates in `EstimateCost()`

## Related Documentation

- [LLM Cache Guide](LLM_CACHE_GUIDE.md) - Caching strategies and configuration
- [LLM Latency Management](LLM_LATENCY_MANAGEMENT.md) - Latency optimization
- [LLM Output Validation Guide](LLM_OUTPUT_VALIDATION_GUIDE.md) - Schema validation
- [Prompt Engineering API](PROMPT_ENGINEERING_API.md) - Prompt customization
