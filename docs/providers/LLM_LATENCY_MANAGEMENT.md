# LLM Latency Management System

## Overview

The LLM Latency Management System provides comprehensive tracking, prediction, and user communication for potentially slow LLM API calls throughout the Aura Video Studio video generation pipeline. This system ensures graceful handling of timeouts, provides realistic time estimates, and maintains user confidence during long-running operations.

## Architecture

### Core Components

#### 1. LatencyManagementService
**Location**: `Aura.Core/Services/Performance/LatencyManagementService.cs`

Central service that tracks historical LLM operation performance and predicts future durations.

**Key Features**:
- Historical data tracking (maintains last 100 operations per provider/operation type)
- Intelligent time prediction based on token count and historical averages
- Confidence scoring based on data consistency and volume
- Timeout warning threshold detection
- Provider-specific performance summaries

**Usage**:
```csharp
var latencyService = serviceProvider.GetRequiredService<LatencyManagementService>();

// Record operation metrics
latencyService.RecordMetrics(new LatencyMetrics
{
    ProviderName = "OpenAI",
    OperationType = "ScriptGeneration",
    PromptTokenCount = 500,
    ResponseTimeMs = 15000,
    Success = true,
    RetryCount = 0
});

// Predict duration for next operation
var estimate = latencyService.PredictDuration("OpenAI", "ScriptGeneration", 500);
Console.WriteLine($"Estimated duration: {estimate.Description}");
Console.WriteLine($"Confidence: {estimate.Confidence:P0}");
```

#### 2. LlmOperationContext
**Location**: `Aura.Core/Services/Performance/LlmOperationContext.cs`

Wrapper for executing LLM operations with automatic latency tracking, timeout management, and progress reporting.

**Key Features**:
- Automatic timeout enforcement based on operation type
- Real-time progress updates every 5 seconds
- Warning notifications at 50% of timeout threshold
- Cancellation token propagation
- Automatic metrics recording

**Usage**:
```csharp
var context = serviceProvider.GetRequiredService<LlmOperationContext>();
var progress = new Progress<LlmOperationProgress>(p =>
{
    Console.WriteLine($"{p.Message} (elapsed: {p.ElapsedSeconds}s)");
    if (p.IsWarning)
    {
        Console.WriteLine("Warning: Operation taking longer than usual");
    }
});

var script = await context.ExecuteAsync(
    providerName: "OpenAI",
    operationType: "ScriptGeneration",
    operation: async ct => await llmProvider.DraftScriptAsync(brief, spec, ct),
    promptTokenCount: 500,
    progress: progress,
    cancellationToken: cancellationToken
);
```

#### 3. LatencyTelemetry
**Location**: `Aura.Core/Services/Performance/LatencyTelemetry.cs`

Structured logging for LLM operation performance and diagnostics.

**Key Features**:
- Operation success/failure logging
- Retry attempt tracking
- Timeout warning logging
- Time estimate logging
- JSON-formatted telemetry for analytics

**Log Examples**:
```
[INFO] LLM Operation: OpenAI ScriptGeneration completed in 15234ms (tokens: 500, retries: 0)
[WARN] LLM Operation: OpenAI ScriptGeneration is taking longer than usual (65s / 120s, 54%)
[INFO] LLM Operation: OpenAI ScriptGeneration retry 2/3 due to rate limit exceeded. Waiting 2000ms before retry
```

#### 4. Enhanced ProviderRetryWrapper
**Location**: `Aura.Core/Services/ProviderRetryWrapper.cs`

Enhanced retry logic with telemetry integration and user notifications.

**Enhancements**:
- Retry notification callbacks for UI updates
- Improved error classification (transient vs permanent)
- Telemetry logging for all retry attempts
- Provider name tracking for metrics

**Usage**:
```csharp
var retryWrapper = serviceProvider.GetRequiredService<ProviderRetryWrapper>();

var result = await retryWrapper.ExecuteWithRetryAsync(
    operation: async ct => await llmProvider.DraftScriptAsync(brief, spec, ct),
    operationName: "Script Generation",
    ct: cancellationToken,
    maxRetries: 3,
    onRetry: (operation, attempt, maxAttempts, reason, delayMs) =>
    {
        Console.WriteLine($"Retrying {operation} (attempt {attempt}/{maxAttempts}) due to {reason}");
    },
    providerName: "OpenAI"
);
```

## Configuration

### Timeout Policies
**Location**: `Aura.Api/appsettings.json` → `LlmTimeouts` section

```json
{
  "LlmTimeouts": {
    "ScriptGenerationTimeoutSeconds": 120,
    "ScriptRefinementTimeoutSeconds": 180,
    "VisualPromptTimeoutSeconds": 45,
    "NarrationOptimizationTimeoutSeconds": 30,
    "PacingAnalysisTimeoutSeconds": 60,
    "SceneImportanceTimeoutSeconds": 45,
    "ContentComplexityTimeoutSeconds": 45,
    "NarrativeArcTimeoutSeconds": 60,
    "WarningThresholdPercentage": 0.5
  }
}
```

**Timeout Guidelines**:
- **ScriptGeneration** (120s): Initial script draft generation
- **ScriptRefinement** (180s): Multi-pass refinement operations
- **VisualPrompt** (45s): Per-scene visual prompt generation
- **NarrationOptimization** (30s): Per-scene narration tweaks
- **PacingAnalysis** (60s): Full video pacing analysis
- **WarningThresholdPercentage** (0.5): Warn at 50% of timeout

### Dependency Injection Registration
**Location**: `Aura.Api/Program.cs`

```csharp
// Configure timeout policy from appsettings
builder.Services.Configure<LlmTimeoutPolicy>(
    builder.Configuration.GetSection("LlmTimeouts"));
builder.Services.AddSingleton(sp => 
    sp.GetRequiredService<IOptions<LlmTimeoutPolicy>>().Value);

// Register latency management services
builder.Services.AddSingleton<LatencyTelemetry>();
builder.Services.AddSingleton<LatencyManagementService>();
builder.Services.AddSingleton<LlmOperationContext>();
```

## API Integration

### Enhanced Job Progress Events

The `JobProgressEventArgs` model has been extended to support latency information:

```csharp
public class JobProgressEventArgs : EventArgs
{
    // Existing fields
    public string JobId { get; init; }
    public int Progress { get; init; }
    public JobStatus Status { get; init; }
    public string Stage { get; init; }
    public string Message { get; init; }
    public TimeSpan? Eta { get; init; }
    
    // New latency fields
    public bool IsWarning { get; init; }                    // True if timeout threshold exceeded
    public int? EstimatedDurationSeconds { get; init; }     // Predicted operation duration
    public int? ElapsedSeconds { get; init; }               // Current elapsed time
}
```

### Server-Sent Events (SSE)

Progress events with latency information are sent to clients via SSE:

```json
{
  "jobId": "abc123",
  "progress": 25,
  "status": "Running",
  "stage": "ScriptGeneration",
  "message": "Generating script... typically takes 15-30 seconds",
  "isWarning": false,
  "estimatedDurationSeconds": 20,
  "elapsedSeconds": 8
}
```

When operation exceeds warning threshold:

```json
{
  "jobId": "abc123",
  "progress": 25,
  "status": "Running",
  "stage": "ScriptGeneration",
  "message": "ScriptGeneration is taking longer than usual... (65s elapsed)",
  "isWarning": true,
  "estimatedDurationSeconds": 20,
  "elapsedSeconds": 65
}
```

## Time Estimate Accuracy

The system provides increasingly accurate estimates as historical data accumulates:

### Confidence Levels

- **< 10 data points**: Low confidence (0.3-0.6)
  - Description: "estimated X-Y seconds (limited historical data)"
  
- **10-50 data points**: Medium confidence (0.6-0.8)
  - Description: "usually takes X-Y seconds"
  
- **50+ data points**: High confidence (0.8-1.0)
  - Description: "typically takes X-Y seconds"

### Token Count Adjustment

Estimates automatically adjust based on prompt size:
- 500 token prompt → ~15 seconds
- 1000 token prompt → ~30 seconds (2x scaling)

### Provider Differences

The system tracks performance independently per provider:
- **OpenAI**: Fast (10-20 seconds for script generation)
- **Ollama** (local): Slower (30-60 seconds for script generation)
- **Gemini**: Medium (15-30 seconds for script generation)

## Retry Logic

### Transient Errors (Auto-Retry)

The system automatically retries these errors up to 3 times with exponential backoff:

- **429 Rate Limit**: "rate limit exceeded"
- **503 Service Unavailable**: "service temporarily unavailable"
- **502/504 Gateway Errors**: "gateway error"
- **Network Errors**: "network error"
- **Timeouts**: "request timeout"

### Retry Schedule

- **Attempt 1**: Immediate
- **Attempt 2**: 1-2 seconds delay
- **Attempt 3**: 2-4 seconds delay
- **Attempt 4**: 4-8 seconds delay (if maxRetries=4)

### Permanent Errors (No Retry)

- **401 Unauthorized**: Invalid API key
- **400 Bad Request**: Invalid request format
- **Other non-transient errors**

## Testing

### Unit Tests

**LatencyManagementServiceTests** (17 tests):
- Historical data tracking
- Time prediction accuracy
- Token count adjustment
- Timeout threshold detection
- Provider-specific tracking
- Performance summaries

**LlmOperationContextTests** (12 tests):
- Successful operation execution
- Timeout handling
- Cancellation propagation
- Progress reporting
- Warning notifications
- Metrics recording

### Running Tests

```bash
dotnet test --filter "FullyQualifiedName~LatencyManagementServiceTests"
dotnet test --filter "FullyQualifiedName~LlmOperationContextTests"
```

## Monitoring and Diagnostics

### Telemetry Logs

All LLM operations are logged with structured data:

```
[INFO] LLM Operation: OpenAI ScriptGeneration completed in 15234ms (tokens: 500, retries: 0)
[DEBUG] LLM Telemetry: {"Provider":"OpenAI","Operation":"ScriptGeneration","PromptTokens":500,"ResponseTimeMs":15234,"Success":true,"RetryCount":0,"Timestamp":"2025-10-31T13:54:19Z"}
```

### Performance Summaries

Query performance summaries for diagnostics:

```csharp
var summary = latencyService.GetPerformanceSummary("OpenAI", "ScriptGeneration");
Console.WriteLine($"Operations: {summary.DataPointCount}");
Console.WriteLine($"Average response time: {summary.AverageResponseTimeMs}ms");
Console.WriteLine($"Success rate: {summary.SuccessRate:P0}");
Console.WriteLine($"Average retries: {summary.AverageRetryCount:F2}");
```

## Best Practices

### 1. Always Use LlmOperationContext for LLM Calls

```csharp
// ✅ CORRECT
var script = await _llmOperationContext.ExecuteAsync(
    "OpenAI", "ScriptGeneration",
    ct => _llmProvider.DraftScriptAsync(brief, spec, ct),
    500, progress, ct
);

// ❌ WRONG
var script = await _llmProvider.DraftScriptAsync(brief, spec, ct);
```

### 2. Provide Accurate Token Counts

Estimate prompt tokens for better predictions:
```csharp
int promptTokens = EstimateTokenCount(brief, spec);
```

### 3. Report Progress to Users

Always provide progress callback for long operations:
```csharp
var progress = new Progress<LlmOperationProgress>(p =>
{
    UpdateUI(p.Message, p.IsWarning);
});
```

### 4. Handle Timeouts Gracefully

```csharp
try
{
    var result = await context.ExecuteAsync(...);
}
catch (TimeoutException ex)
{
    _logger.LogWarning("Operation timed out: {Message}", ex.Message);
    // Offer user option to retry with extended timeout
}
```

### 5. Configure Appropriate Timeouts

Adjust timeouts based on your typical usage:
- Development: Shorter timeouts for faster feedback
- Production: Longer timeouts to account for peak load

## Future Enhancements

### Planned Features

1. **Dynamic Timeout Extension**: Allow users to extend timeout mid-operation
2. **Persistent Historical Data**: Store metrics to database for cross-session learning
3. **Provider Performance Comparison**: Show relative performance of different providers
4. **Anomaly Detection**: Alert when operations are significantly slower than normal
5. **Cost Tracking**: Track API usage costs alongside latency
6. **UI Dashboard**: Visual display of LLM performance metrics

## Troubleshooting

### Issue: Operations Timing Out Frequently

**Solutions**:
1. Check provider API status
2. Increase timeout in `appsettings.json`
3. Review telemetry logs for error patterns
4. Consider switching to faster provider

### Issue: Inaccurate Time Estimates

**Cause**: Insufficient historical data

**Solutions**:
1. Wait for 10+ operations to build history
2. Ensure accurate token counts are provided
3. Check for outliers affecting averages

### Issue: No Progress Updates

**Cause**: Progress callback not provided

**Solution**: Always pass progress callback:
```csharp
var progress = new Progress<LlmOperationProgress>(p => /* handle */);
await context.ExecuteAsync(..., progress: progress, ...);
```

## Cost and Usage Tracking

The LLM Latency Management System integrates with the Cost and Usage Analytics system to track detailed token usage and costs for each operation.

### Token Accounting

Every LLM operation automatically records:
- Input tokens (prompt)
- Output tokens (completion)
- Model name used
- Provider name
- Response time (latency)
- Retry count
- Cache hit status
- Estimated cost

**Location**: `Aura.Core/Services/CostTracking/TokenTrackingService.cs`

**Integration**: The `UnifiedLlmOrchestrator` automatically records token metrics for every operation through the `RecordTokenMetrics` method.

**Token Metrics Structure**:
```csharp
public record TokenUsageMetrics
{
    public string ProviderName { get; init; }
    public string ModelName { get; init; }
    public string OperationType { get; init; }
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public long ResponseTimeMs { get; init; }
    public int RetryCount { get; init; }
    public bool CacheHit { get; init; }
    public decimal EstimatedCost { get; init; }
    public string? JobId { get; init; }
    public bool Success { get; init; }
}
```

### Cost Estimation

Costs are estimated based on current provider pricing tables maintained in `EnhancedCostTrackingService`:

- **OpenAI GPT-4**: $0.03 per 1K input tokens, $0.06 per 1K output tokens
- **Anthropic Claude 3**: $0.015 per 1K input tokens, $0.075 per 1K output tokens
- **Google Gemini**: $0.00025 per 1K tokens
- **Ollama**: Free (local)

Cost estimation happens automatically during each LLM operation and is recorded for budget tracking and optimization.

### Cache Cost Savings

Cache hits save significant costs by avoiding repeat API calls:
- Cache hit status is tracked for every operation
- Cost savings from cache are calculated and reported
- Run summaries include total cost saved by caching

**Example**: If a cached response would have cost $0.05, that $0.05 is added to the "cost saved by cache" total for the job.

### Real-Time Cost Accumulation

During video generation, costs accumulate in real-time:
- Each LLM operation adds to the running total
- Cost meter updates live in the UI
- Budget warnings trigger if thresholds are approached
- Hard limits can block operations if enabled

### Per-Run Cost Reports

After each video generation run, a comprehensive cost report is generated including:
- Total cost breakdown by stage (script generation, TTS, visuals, rendering)
- Total cost breakdown by provider
- Token usage statistics (total tokens, cache hit rate)
- Individual operation costs with timestamps
- Cost optimization suggestions

**Location**: `Aura.Core/Services/CostTracking/RunCostReportService.cs`

**API Endpoint**: `GET /api/cost-tracking/run-summary/{jobId}`

### Cost Optimization

The system analyzes usage patterns and provides optimization suggestions:

1. **Caching**: Enable LLM caching if cache hit rate is low
2. **Provider Selection**: Switch to lower-cost providers (e.g., Gemini instead of GPT-4)
3. **Prompt Optimization**: Reduce token usage through more efficient prompts
4. **Model Selection**: Use smaller models for simpler tasks
5. **Batching**: Combine operations to reduce overhead

**Example Suggestion**:
```
Category: Caching
Suggestion: Enable LLM caching to reduce costs. Current cache hit rate is low.
Estimated Savings: $3.50 (70% of current cost)
Quality Impact: No impact - identical results from cache
```

### Budget Controls

Budget thresholds can be configured at multiple levels:
- Overall monthly budget
- Per-provider budgets
- Per-project budgets
- Soft warnings (alert only)
- Hard limits (block operations)

When approaching budget limits:
- Warning at 75% of budget
- Alert at 90% of budget
- Block at 100% if hard limit enabled

### Monitoring and Alerts

Monitor cost trends through:
- Current period spending dashboard
- Historical cost reports
- Provider cost comparisons
- Cache effectiveness metrics
- Budget utilization percentages

**API Endpoint**: `GET /api/cost-tracking/current-period`

### Integration with Latency

Cost and latency are tracked together:
- Higher latency may indicate complex operations with higher token usage
- Retry attempts increase both latency and cost
- Failed operations still incur costs (tracked separately)

This integrated view helps optimize for both performance and cost efficiency.

## Contributing

When adding new LLM operations:

1. Add timeout configuration to `LlmTimeoutPolicy`
2. Add operation type constant
3. Update `GetTimeoutSeconds()` switch statement
4. Wrap operations with `LlmOperationContext`
5. Add tests for new operation type
6. Ensure token tracking is enabled (automatic in `UnifiedLlmOrchestrator`)

---

**Version**: 2.0.0  
**Last Updated**: November 5, 2025  
**Author**: Aura Development Team
