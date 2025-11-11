# Network Resilience & Retry Logic Configuration Guide

## Overview

Aura Video Studio implements comprehensive network resilience at both frontend and backend layers to handle network failures, API timeouts, and service disruptions gracefully.

## Frontend Resilience

### 1. Network Resilience Service

**Location**: `Aura.Web/src/services/networkResilience.ts`

**Features**:
- Offline request queueing
- Priority-based request processing
- Automatic retry when network restores
- LocalStorage persistence across page reloads

**Configuration**:
```typescript
import { networkResilienceService } from '@/services/api/apiClient';

// Configure the service
networkResilienceService.configure({
  enableOfflineQueue: true,
  maxQueueSize: 50,
  autoRetryOnReconnect: true,
  queuePersistence: true
});
```

**Usage**:
```typescript
// Queue a request when offline
const requestId = networkResilienceService.queueRequest(
  '/api/jobs',
  'POST',
  { data: jobData },
  { priority: 'high', maxRetries: 3 }
);

// Process queue when online
await networkResilienceService.processQueue(async (request) => {
  try {
    await apiClient.post(request.url, request.data);
    return true; // Success
  } catch (error) {
    return false; // Retry
  }
});
```

### 2. Timeout Configuration

**Location**: `Aura.Web/src/config/timeouts.ts`

**Default Timeouts**:
```typescript
{
  default: 30000,        // 30 seconds
  health: 5000,          // 5 seconds
  auth: 10000,           // 10 seconds
  scriptGeneration: 120000,   // 2 minutes
  tts: 60000,            // 1 minute
  imageGeneration: 180000,    // 3 minutes
  videoGeneration: 300000,    // 5 minutes
  videoRendering: 600000,     // 10 minutes
  fileUpload: 120000,    // 2 minutes
  fileDownload: 180000,  // 3 minutes
  quickOperations: 5000  // 5 seconds
}
```

**Customization**:
```typescript
import { timeoutConfig, getOperationTimeout } from '@/config/timeouts';

// Update specific timeout
timeoutConfig.setTimeout('videoRendering', 900000); // 15 minutes

// Update multiple timeouts
timeoutConfig.setTimeouts({
  scriptGeneration: 180000,
  tts: 90000
});

// Get timeout for operation
const timeout = getOperationTimeout('render');

// Reset to defaults
timeoutConfig.resetToDefaults();
timeoutConfig.resetTimeout('videoRendering');
```

### 3. Circuit Breaker

**Location**: `Aura.Web/src/services/api/apiClient.ts`

**Configuration**:
```typescript
// Circuit breaker is automatic with these thresholds:
{
  failureThreshold: 5,      // Number of failures before opening
  successThreshold: 2,      // Successes needed to close from half-open
  timeout: 60000            // Wait time before retry (1 minute)
}
```

**Manual Control**:
```typescript
import { resetCircuitBreaker, getCircuitBreakerState } from '@/services/api/apiClient';

// Check state
const state = getCircuitBreakerState(); // 'CLOSED', 'OPEN', or 'HALF_OPEN'

// Manual reset (testing/recovery)
resetCircuitBreaker();
```

**State Persistence**: Circuit breaker state is persisted to localStorage and restored on page reload.

### 4. Exponential Backoff

**Automatic for all API calls** with the following configuration:
- Base delay: 1 second
- Max delay: 8 seconds
- Max retries: 3
- Applies to transient errors (5xx, network errors, timeouts)

**Custom retry configuration**:
```typescript
import { post } from '@/services/api/apiClient';

// Skip retry for specific request
await post('/api/jobs', data, {
  _skipRetry: true
});

// Custom timeout
await post('/api/jobs', data, {
  timeout: 60000 // 60 seconds
});
```

### 5. Offline Detection

**Location**: `Aura.Web/src/stores/appStore.ts`

**Automatic Features**:
- Monitors `navigator.onLine` status
- Shows notification when offline/online
- Updates global state for UI adaptation

**Usage in Components**:
```typescript
import { useAppStore } from '@/stores/appStore';

function MyComponent() {
  const isOnline = useAppStore((state) => state.isOnline);
  
  return (
    <div>
      {!isOnline && <OfflineWarning />}
      <button disabled={!isOnline}>Submit</button>
    </div>
  );
}
```

## Backend Resilience

### 1. Resilience Pipelines (Polly)

**Location**: `Aura.Core/Resilience/ResiliencePipelineFactory.cs`

**Features**:
- Exponential backoff retry
- Circuit breaker
- Timeout policies
- HTTP-specific handling

**HTTP Client Configuration**:
```csharp
// In Startup/Program.cs
services.AddResilientHttpClient("OpenAI", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com");
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
});

// Or typed client
services.AddResilientHttpClient<OpenAiLlmProvider>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(2);
});
```

### 2. Circuit Breaker Settings

**Location**: `appsettings.json` or `appsettings.resilience.json`

```json
{
  "CircuitBreaker": {
    "FailureThreshold": 5,
    "BreakDurationSeconds": 60,
    "RollingWindowMinutes": 5,
    "MinimumThroughput": 10
  }
}
```

**Per-Service Configuration**:
```csharp
var pipelineOptions = new ResiliencePipelineOptions
{
    Name = "OpenAI",
    EnableRetry = true,
    MaxRetryAttempts = 3,
    RetryDelay = TimeSpan.FromSeconds(1),
    EnableCircuitBreaker = true,
    CircuitBreakerFailureRatio = 0.5,
    CircuitBreakerBreakDuration = TimeSpan.FromMinutes(1),
    EnableTimeout = true,
    Timeout = TimeSpan.FromMinutes(2)
};

var pipeline = pipelineFactory.CreateCustomPipeline<HttpResponseMessage>(pipelineOptions);
```

### 3. Retry Strategy

**Default Configuration**:
- Max attempts: 3
- Base delay: 1 second
- Backoff type: Exponential
- Jitter: Enabled (randomizes delays)

**Transient Error Detection**:
- `HttpRequestException`
- `TimeoutException`
- HTTP 5xx status codes
- HTTP 408 (Request Timeout)
- HTTP 429 (Too Many Requests)

### 4. Provider-Specific Resilience

Many providers (LLM, TTS, Image) have built-in retry logic that complements the HTTP client resilience:

**OpenAiLlmProvider**:
```csharp
// Has configurable retries
public OpenAiLlmProvider(
    ILogger logger,
    HttpClient httpClient,
    string apiKey,
    string model = "gpt-4o-mini",
    int maxRetries = 2,  // Provider-level retries
    int timeoutSeconds = 120)
```

## Best Practices

### Frontend

1. **Use appropriate timeouts** for different operations
2. **Queue non-critical requests** when offline
3. **Provide feedback** to users about network status
4. **Implement retry UI** for failed operations
5. **Test offline scenarios** regularly

### Backend

1. **Use resilient HTTP clients** for all external API calls
2. **Configure appropriate timeouts** per service
3. **Monitor circuit breaker states** via health checks
4. **Log retry attempts** for debugging
5. **Implement fallback strategies** when all retries fail

## Monitoring

### Frontend Monitoring

Check browser DevTools Console for:
- `[networkResilience]` - Queue operations
- `[apiClient]` - Circuit breaker state changes
- `[apiClient]` - Retry attempts

### Backend Monitoring

Check application logs for:
- `Retry attempt {N}/{Max}` - Retry operations
- `Circuit breaker OPENED` - Service failures
- `Circuit breaker CLOSED` - Service recovery

## Troubleshooting

### Circuit Breaker Stuck Open

**Symptom**: All requests fail immediately with "Circuit breaker is open"

**Solution**:
```typescript
// Frontend
import { resetCircuitBreaker } from '@/services/api/apiClient';
resetCircuitBreaker();

// Backend - wait for timeout or restart service
```

### Requests Timing Out

**Check**:
1. Network connectivity
2. Timeout configuration for the operation
3. Backend service health
4. Provider API status

**Adjust timeouts if needed**:
```typescript
timeoutConfig.setTimeout('videoRendering', 1200000); // 20 minutes
```

### Queue Not Processing

**Check**:
1. Network status: `networkResilienceService.isOnline()`
2. Queue contents: `networkResilienceService.getQueuedRequests()`
3. Processing state: Check logs for `[networkResilience] [process]`

**Manual trigger**:
```typescript
await networkResilienceService.processQueue(executeRequest);
```

## Testing

### Frontend

```typescript
// Test offline handling
window.dispatchEvent(new Event('offline'));
// ... perform actions
window.dispatchEvent(new Event('online'));

// Test circuit breaker
for (let i = 0; i < 5; i++) {
  // Trigger failures to open circuit
}

// Test timeout configuration
timeoutConfig.setTimeout('quick', 1000);
```

### Backend

```csharp
// Mock transient failures
var mockHandler = new Mock<HttpMessageHandler>();
mockHandler
    .Protected()
    .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
    .ThrowsAsync(new HttpRequestException("Simulated failure"));

// Test with resilient client
var httpClient = new HttpClient(mockHandler.Object);
var provider = new OpenAiLlmProvider(logger, httpClient, apiKey);

// Should retry automatically
await provider.DraftScriptAsync(brief, spec, CancellationToken.None);
```

## Configuration Files

### Frontend

- `src/config/timeouts.ts` - Timeout configuration
- `src/services/networkResilience.ts` - Queue service
- `src/services/api/apiClient.ts` - Circuit breaker & retry
- `src/stores/appStore.ts` - Offline detection

### Backend

- `Aura.Core/Resilience/ResiliencePipelineFactory.cs` - Pipeline factory
- `Aura.Api/Startup/ResilienceServicesExtensions.cs` - DI setup
- `appsettings.json` - Circuit breaker settings
- Provider constructors - Provider-specific retries

## Future Enhancements

- Health dashboard showing circuit breaker states
- Configurable retry strategies per endpoint
- Request queue size metrics
- Automatic queue processing on network restore
- Provider fallback chains
- Rate limiting per provider
