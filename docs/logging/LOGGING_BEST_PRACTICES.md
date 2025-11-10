# Logging Best Practices Guide

## Overview

This guide provides best practices for using the structured logging and distributed tracing infrastructure in the Aura project.

## Table of Contents

- [Logging Levels](#logging-levels)
- [Structured Logging](#structured-logging)
- [Distributed Tracing](#distributed-tracing)
- [Performance Logging](#performance-logging)
- [Security Considerations](#security-considerations)
- [Frontend Logging](#frontend-logging)
- [Backend Logging](#backend-logging)

## Logging Levels

### When to Use Each Level

#### Debug
- Use for detailed diagnostic information
- **Suppressed in production**
- Examples:
  - Variable values during execution
  - Entry/exit of methods
  - Detailed state information

```csharp
logger.LogDebug("Processing item {ItemId} with value {Value}", itemId, value);
```

#### Information
- Use for general informational messages about application flow
- Examples:
  - Service started/stopped
  - Request received/completed
  - Background job executed

```csharp
logger.LogInformation("Video generation started for project {ProjectId}", projectId);
```

#### Warning
- Use for unexpected conditions that don't prevent operation
- Examples:
  - Slow performance detected
  - Deprecated API usage
  - Resource usage approaching limits

```csharp
logger.LogWarning("Request took {Duration}ms, exceeding threshold", duration);
```

#### Error
- Use for errors and exceptions that need investigation
- **Always include the exception object**
- Examples:
  - API call failures
  - Database errors
  - Validation failures

```csharp
logger.LogError(exception, "Failed to generate video for project {ProjectId}", projectId);
```

## Structured Logging

### Use Structured Properties

**✅ Good - Structured**
```csharp
logger.LogInformation(
    "User {UserId} created project {ProjectId} with duration {Duration}s",
    userId, projectId, duration);
```

**❌ Bad - String Concatenation**
```csharp
logger.LogInformation($"User {userId} created project {projectId} with duration {duration}s");
```

### Using LoggingExtensions

The `Aura.Core.Logging` namespace provides extension methods for common logging patterns:

#### Log with Context
```csharp
using Aura.Core.Logging;

logger.LogStructured(
    LogLevel.Information,
    "Operation completed: {OperationName}",
    new Dictionary<string, object>
    {
        ["OperationName"] = "VideoGeneration",
        ["ProjectId"] = projectId,
        ["Duration"] = duration,
        ["Success"] = true
    });
```

#### Performance Logging
```csharp
logger.LogPerformance(
    "Video Generation",
    duration,
    success: true,
    new Dictionary<string, object>
    {
        ["ProjectId"] = projectId,
        ["Resolution"] = "1920x1080",
        ["Frames"] = frameCount
    });
```

#### Audit Logging
```csharp
logger.LogAudit(
    action: "DeleteProject",
    userId: currentUser.Id,
    resourceId: projectId,
    new Dictionary<string, object>
    {
        ["ProjectName"] = projectName,
        ["Reason"] = "UserRequest"
    });
```

#### Security Logging
```csharp
logger.LogSecurity(
    eventType: "LoginAttempt",
    success: true,
    userId: userId,
    ipAddress: clientIp,
    details: "Two-factor authentication used");
```

## Distributed Tracing

### Using Trace Context

#### Backend (C#)

```csharp
using Aura.Core.Logging;

// Trace context is automatically propagated through middleware
// Access current trace context
var traceContext = TraceContext.Current;

// Create a child span for a sub-operation
using var childScope = TraceContext.Current?.CreateChildSpan("DatabaseQuery");
try
{
    // Operation code
    logger.LogInformation("Query executed successfully");
}
catch (Exception ex)
{
    logger.LogError(ex, "Query failed");
    throw;
}
```

#### Frontend (TypeScript)

```typescript
import { logger } from '../utils/logger';

// Create a trace context for an operation
const traceContext = logger.createChildSpan('LoadProjectData');
logger.setTraceContext(traceContext);

try {
  const data = await fetchProjectData(projectId);
  logger.info('Project data loaded', 'ProjectView', 'load', { projectId });
} catch (error) {
  logger.error('Failed to load project', error, 'ProjectView', 'load');
}
```

### Correlation IDs

Correlation IDs are automatically generated and propagated:
- Set in `CorrelationIdMiddleware` for each HTTP request
- Included in all log entries
- Returned in response headers as `X-Correlation-ID`
- Propagated to downstream services

#### Manual Correlation ID Usage

```csharp
using (logger.BeginCorrelatedScope(correlationId, "VideoProcessing"))
{
    // All logs within this scope will include the correlation ID
    logger.LogInformation("Processing video");
}
```

## Performance Logging

### Using PerformanceTimer

#### Backend

```csharp
using Aura.Core.Logging;

// Using disposable pattern
using (var timer = PerformanceTimer.Start(logger, "VideoEncoding"))
{
    timer.AddMetadata("Resolution", "1920x1080");
    timer.AddMetadata("Codec", "h264");
    
    // Checkpoint
    timer.Checkpoint("FramesProcessed");
    
    // Operation code
    await EncodeVideo(input, output);
    
    // Timer automatically logs on dispose
}

// Using extension method
var result = await logger.TimeOperationAsync(
    "DatabaseQuery",
    async () => await database.QueryAsync(sql),
    new Dictionary<string, object> { ["Query"] = "GetProjects" }
);
```

#### Frontend

```typescript
// Time an async operation
const result = await logger.timeOperation(
  'LoadDashboard',
  async () => {
    const data = await fetchDashboardData();
    return data;
  },
  'Dashboard',
  { userId }
);

// Manual timing
const start = performance.now();
try {
  await someOperation();
  const duration = performance.now() - start;
  logger.performance('SomeOperation', duration, 'MyComponent', { success: true });
} catch (error) {
  const duration = performance.now() - start;
  logger.performance('SomeOperation', duration, 'MyComponent', { success: false });
}
```

## Security Considerations

### Sensitive Data Filtering

The `SensitiveDataFilter` automatically redacts sensitive information:

**Automatically Filtered:**
- Email addresses (partially masked)
- Credit card numbers
- Social Security Numbers
- JWT tokens
- Properties containing: `password`, `secret`, `token`, `apikey`, `credential`

#### Manual Sanitization

```csharp
using Aura.Core.Logging;

// Sanitize a dictionary
var sanitized = LogSanitizer.SanitizeDictionary(userInput);
logger.LogInformation("User data: {@Data}", sanitized);

// Mask sensitive strings
var maskedApiKey = LogSanitizer.MaskString(apiKey, visibleChars: 4);
logger.LogInformation("API Key: {MaskedKey}", maskedApiKey);
```

#### TypeScript

```typescript
// Never log sensitive data directly
// ❌ Bad
logger.info('User logged in', 'Auth', 'login', { password: userPassword });

// ✅ Good
logger.info('User logged in', 'Auth', 'login', { userId: user.id });
```

## Frontend Logging

### Component-Scoped Logging

```typescript
import { logger } from '../utils/logger';

export const MyComponent = () => {
  // Create a scoped logger
  const componentLogger = logger.forComponent('MyComponent');
  
  useEffect(() => {
    componentLogger.debug('Component mounted', 'mount');
    
    return () => {
      componentLogger.debug('Component unmounted', 'unmount');
    };
  }, []);
  
  const handleAction = async () => {
    try {
      componentLogger.info('Action started', 'handleAction');
      await performAction();
      componentLogger.info('Action completed', 'handleAction');
    } catch (error) {
      componentLogger.error('Action failed', error, 'handleAction');
    }
  };
  
  return <button onClick={handleAction}>Perform Action</button>;
};
```

### Error Boundary Logging

```typescript
import { ErrorBoundary } from '../components/ErrorBoundary';

// Wrap components with error boundary
<ErrorBoundary componentName="Dashboard">
  <DashboardComponent />
</ErrorBoundary>
```

## Backend Logging

### Request Logging

Request logging is automatic via middleware. All requests are logged with:
- HTTP method and path
- Status code
- Duration
- Client IP
- User agent
- Correlation ID
- Trace ID and Span ID

### Operation Context

```csharp
using Aura.Core.Logging;

using (OperationContext.BeginScope("VideoGeneration", projectId))
{
    // All logs within this scope include the operation context
    logger.LogInformation("Starting video generation");
    
    // Nested operations automatically inherit context
    await GenerateFrames();
    await EncodeVideo();
    
    logger.LogInformation("Video generation completed");
}
```

## Log Sampling

For high-volume scenarios, use log sampling:

```csharp
// In Program.cs
Log.Logger = new LoggerConfiguration()
    .ConfigureStructuredLogging("Aura.Api")
    .ConfigureLogSampling(sampleRate: 10) // Keep 1 in 10 debug/verbose logs
    .CreateLogger();
```

## Common Anti-Patterns

### ❌ Don't

```csharp
// Don't use string concatenation
logger.LogInformation("User " + userId + " logged in");

// Don't log without context
logger.LogError("An error occurred");

// Don't log PII without sanitization
logger.LogInformation("User email: {Email}", userEmail);

// Don't ignore exceptions
catch (Exception ex)
{
    logger.LogError("Failed");
}
```

### ✅ Do

```csharp
// Use structured logging
logger.LogInformation("User {UserId} logged in", userId);

// Provide context
logger.LogError(ex, "Failed to save user data for {UserId}", userId);

// Sanitize or omit PII
logger.LogInformation("User registered: {UserId}", userId);

// Always log the exception
catch (Exception ex)
{
    logger.LogError(ex, "Failed to save user data for {UserId}", userId);
}
```

## Performance Tips

1. **Use log levels appropriately** - Debug logs are free in production (not evaluated)
2. **Avoid expensive operations in log statements** - Don't serialize large objects
3. **Use structured properties** - More efficient than string interpolation
4. **Batch frontend logs** - Logs are automatically batched and sent every 5 seconds
5. **Sample high-volume logs** - Use log sampling for debug/verbose logs

## Testing Considerations

### Unit Tests

```csharp
// Use ILogger<T> for easy mocking
public class VideoService
{
    private readonly ILogger<VideoService> _logger;
    
    public VideoService(ILogger<VideoService> logger)
    {
        _logger = logger;
    }
}

// In tests
var mockLogger = new Mock<ILogger<VideoService>>();
var service = new VideoService(mockLogger.Object);

// Verify logging
mockLogger.Verify(
    x => x.Log(
        LogLevel.Information,
        It.IsAny<EventId>(),
        It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("expected message")),
        null,
        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
    Times.Once);
```

## Further Reading

- [Debugging with Traces Guide](./DEBUGGING_WITH_TRACES.md)
- [Log Query Examples](./LOG_QUERY_EXAMPLES.md)
- [Local Logging Setup](./LOCAL_LOGGING_SETUP.md)
- [Serilog Documentation](https://serilog.net/)
