# Local Logging Setup Guide

This guide helps you configure and use the logging system in your local development environment.

## Prerequisites

- .NET 8.0 SDK
- Node.js 20+
- Code editor (VS Code, Visual Studio, or Rider)

## Backend Setup (Aura.Api)

### 1. Configuration

The logging system is already configured in `Program.cs`. By default, logs are written to:

- `logs/aura-api-YYYY-MM-DD.log` - All logs
- `logs/errors-YYYY-MM-DD.log` - Errors only
- `logs/warnings-YYYY-MM-DD.log` - Warnings and above
- `logs/performance-YYYY-MM-DD.log` - Performance metrics
- `logs/audit-YYYY-MM-DD.log` - Audit logs (90-day retention)
- Console - All logs (formatted)

### 2. Adjust Log Levels

Create or modify `appsettings.Development.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Information",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Information",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

### 3. Enabling Different Log Levels

#### Maximum Logging (Development)

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Verbose"
    }
  }
}
```

#### Production-like (Staging)

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

### 4. Custom Log Outputs

#### File-only Logging

```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/aura-dev-.log",
          "rollingInterval": "Day",
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

#### Structured JSON Logging

```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/aura-structured-.json",
          "rollingInterval": "Day",
          "formatter": "Serilog.Formatting.Json.JsonFormatter, Serilog"
        }
      }
    ]
  }
}
```

### 5. Using Logging in Code

#### Basic Logging

```csharp
public class MyService
{
    private readonly ILogger<MyService> _logger;

    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }

    public async Task DoSomethingAsync(string projectId)
    {
        _logger.LogDebug("Starting operation for {ProjectId}", projectId);
        
        try
        {
            _logger.LogInformation("Processing project {ProjectId}", projectId);
            await ProcessProject(projectId);
            _logger.LogInformation("Operation completed for {ProjectId}", projectId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operation failed for {ProjectId}", projectId);
            throw;
        }
    }
}
```

#### Performance Timing

```csharp
using Aura.Core.Logging;

public async Task ProcessVideoAsync(string projectId)
{
    using var timer = PerformanceTimer.Start(_logger, "VideoProcessing");
    timer.AddMetadata("ProjectId", projectId);
    
    try
    {
        timer.Checkpoint("ValidationStarted");
        await ValidateProject(projectId);
        
        timer.Checkpoint("GenerationStarted");
        await GenerateFrames(projectId);
        
        timer.Checkpoint("EncodingStarted");
        await EncodeVideo(projectId);
        
        timer.Stop(success: true);
    }
    catch (Exception ex)
    {
        timer.Stop(success: false, errorMessage: ex.Message);
        throw;
    }
}
```

#### Structured Logging

```csharp
using Aura.Core.Logging;

_logger.LogStructured(
    LogLevel.Information,
    "Video generation completed",
    new Dictionary<string, object>
    {
        ["ProjectId"] = projectId,
        ["Duration"] = duration,
        ["FrameCount"] = frameCount,
        ["Resolution"] = "1920x1080",
        ["Success"] = true
    });
```

## Frontend Setup (Aura.Web)

### 1. Using the Logger

```typescript
import { logger } from '../utils/logger';

// Component-scoped logging
const MyComponent = () => {
  const log = logger.forComponent('MyComponent');
  
  useEffect(() => {
    log.debug('Component mounted', 'mount');
    
    return () => {
      log.debug('Component unmounted', 'unmount');
    };
  }, []);
  
  const handleAction = async () => {
    log.info('Action started', 'handleAction');
    
    try {
      const result = await performAction();
      log.info('Action completed', 'handleAction', { result });
    } catch (error) {
      log.error('Action failed', error, 'handleAction');
    }
  };
  
  return <button onClick={handleAction}>Do Something</button>;
};
```

### 2. Performance Timing

```typescript
// Automatic timing
const result = await logger.timeOperation(
  'LoadDashboard',
  async () => {
    const data = await fetchDashboardData();
    return data;
  },
  'Dashboard',
  { userId: currentUser.id }
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
  throw error;
}
```

### 3. Error Boundaries

```typescript
import { ErrorBoundary } from '../components/ErrorBoundary';

// Wrap components
<ErrorBoundary componentName="Dashboard">
  <DashboardComponent />
</ErrorBoundary>
```

### 4. Trace Context

```typescript
import { logger } from '../utils/logger';

async function complexOperation() {
  // Create trace context
  const trace = logger.createChildSpan('ComplexOperation');
  logger.setTraceContext(trace);
  
  try {
    // All logs within this scope will include trace context
    logger.info('Starting complex operation', 'ComplexOps');
    
    await step1();
    await step2();
    await step3();
    
    logger.info('Complex operation completed', 'ComplexOps');
  } catch (error) {
    logger.error('Complex operation failed', error, 'ComplexOps');
    throw error;
  }
}
```

## Viewing Logs

### Console Output

When running locally, logs appear in your terminal:

```bash
# Start API
cd Aura.Api
dotnet run

# Console output:
[14:30:15 INF] [abc123] Request: GET /api/projects
[14:30:15 INF] [abc123] Processing request
[14:30:15 INF] [abc123] Response: GET /api/projects 200 in 45ms
```

### Log Files

```bash
# View latest logs
tail -f logs/aura-api-$(date +%Y-%m-%d).log

# View errors only
tail -f logs/errors-$(date +%Y-%m-%d).log

# Search logs
grep "ProjectId.*proj_123" logs/*.log

# View logs with context
grep -B 5 -A 5 "ERROR" logs/*.log
```

### Using Seq (Optional)

Seq is a powerful log viewer for development.

#### 1. Install Seq

```bash
# Using Docker
docker run --name seq -d --restart unless-stopped \
  -e ACCEPT_EULA=Y \
  -p 5341:80 \
  datalust/seq:latest
```

#### 2. Add Seq Sink

Add to `Aura.Core.csproj`:

```xml
<PackageReference Include="Serilog.Sinks.Seq" Version="7.0.0" />
```

Update `appsettings.Development.json`:

```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://localhost:5341"
        }
      }
    ]
  }
}
```

#### 3. View Logs in Seq

Navigate to `http://localhost:5341` in your browser.

### Log Analysis Tools

#### jq (for JSON logs)

```bash
# Pretty print JSON logs
cat logs/aura-structured-*.json | jq '.'

# Filter by level
cat logs/aura-structured-*.json | jq 'select(.Level == "Error")'

# Extract specific fields
cat logs/aura-structured-*.json | jq '{timestamp: .Timestamp, message: .Message, level: .Level}'
```

#### LogView (Log Viewer)

Install a log viewer tool:

```bash
# Using npm
npm install -g logview

# View logs
logview logs/aura-api-$(date +%Y-%m-%d).log
```

## Testing Logging

### Unit Tests

```csharp
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class MyServiceTests
{
    [Fact]
    public async Task Should_Log_Information_On_Success()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<MyService>>();
        var service = new MyService(mockLogger.Object);
        
        // Act
        await service.DoSomethingAsync("proj_123");
        
        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Operation completed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
    
    [Fact]
    public async Task Should_Log_Error_On_Failure()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<MyService>>();
        var service = new MyService(mockLogger.Object);
        
        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => 
            service.DoSomethingAsync("invalid"));
        
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
```

### Integration Tests

```csharp
public class LoggingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public LoggingIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task Should_Include_Correlation_ID_In_Response()
    {
        // Act
        var response = await _client.GetAsync("/api/projects");
        
        // Assert
        response.Headers.TryGetValues("X-Correlation-ID", out var values);
        Assert.NotNull(values);
        Assert.Single(values);
        Assert.NotEmpty(values.First());
    }
}
```

## Troubleshooting

### Logs Not Appearing

1. **Check log level**: Ensure `MinimumLevel` in `appsettings.json` is appropriate
2. **Check file permissions**: Ensure the logs directory is writable
3. **Check configuration**: Verify `appsettings.Development.json` is being loaded
4. **Check middleware order**: Ensure logging middleware is registered correctly

### Missing Correlation IDs

1. **Verify middleware registration** in `Program.cs`:
```csharp
app.UseCorrelationId(); // Should be early in pipeline
```

2. **Check middleware order**:
```csharp
app.UseCorrelationId();        // First
app.UseRequestLogging();       // After correlation ID
app.UsePerformanceTracking();  // After correlation ID
```

### Performance Impact

If logging is impacting performance:

1. **Adjust log levels**:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"  // Change from Debug
    }
  }
}
```

2. **Use async logging** (already configured):
```csharp
.WriteTo.Async(a => a.File("logs/aura-api-.log"))
```

3. **Enable log sampling**:
```csharp
.ConfigureLogSampling(sampleRate: 10)  // Keep 1 in 10 debug logs
```

### Large Log Files

If log files are growing too large:

1. **Enable file size limits** (already configured):
```csharp
.WriteTo.File(
    "logs/aura-api-.log",
    rollOnFileSizeLimit: true,
    fileSizeLimitBytes: 100_000_000)  // 100MB
```

2. **Adjust retention**:
```csharp
.WriteTo.File(
    "logs/aura-api-.log",
    retainedFileCountLimit: 7)  // Keep only 7 days
```

3. **Set up log rotation**:
```bash
# Add to cron (daily)
find /app/logs -name "*.log" -mtime +7 -exec gzip {} \;
find /app/logs -name "*.log.gz" -mtime +30 -delete
```

## Tips for Development

### 1. Use Debug Level Liberally

Debug logs are free in production (not evaluated), so use them generously during development:

```csharp
_logger.LogDebug("Variable value: {Value}", someValue);
_logger.LogDebug("Entering method {MethodName}", nameof(MyMethod));
```

### 2. Add Correlation IDs to Requests

Use the browser developer tools to see correlation IDs:

```javascript
// In Network tab, check Response Headers
X-Correlation-ID: abc123def456
X-Trace-ID: 7f8e9d0c1b2a
X-Span-ID: 3e2f1a0b9c8d
```

### 3. Use Log Scopes

Group related logs:

```csharp
using (logger.BeginScope("VideoGeneration:{ProjectId}", projectId))
{
    _logger.LogInformation("Starting generation");
    // All logs here will include the scope
    _logger.LogInformation("Generation completed");
}
```

### 4. Create Helper Scripts

```bash
#!/bin/bash
# watch-errors.sh
watch -n 1 'tail -20 logs/errors-$(date +%Y-%m-%d).log'
```

## Further Reading

- [Logging Best Practices](./LOGGING_BEST_PRACTICES.md)
- [Debugging with Traces](./DEBUGGING_WITH_TRACES.md)
- [Log Query Examples](./LOG_QUERY_EXAMPLES.md)
- [Serilog Documentation](https://serilog.net/)
