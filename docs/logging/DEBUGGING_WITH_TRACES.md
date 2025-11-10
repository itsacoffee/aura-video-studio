# Debugging with Distributed Traces

## Overview

This guide demonstrates how to use distributed tracing to debug issues across the Aura frontend and backend.

## Understanding Distributed Tracing

### Key Concepts

- **Trace**: A complete journey of a request through the system
- **Span**: A single operation within a trace (e.g., HTTP request, database query)
- **Trace ID**: Unique identifier for an entire trace
- **Span ID**: Unique identifier for a specific span within a trace
- **Parent Span ID**: Links child spans to their parent
- **Correlation ID**: Simplified identifier for tracking requests

### Trace Propagation Flow

```
Frontend             Backend              Database
   |                    |                    |
   |---> Request ------>|                    |
   |    (TraceID: abc)  |                    |
   |                    |---> Query -------->|
   |                    |   (SpanID: def)    |
   |                    |<--- Result --------|
   |<--- Response ------|                    |
```

## Finding Related Logs

### By Correlation ID

Correlation IDs are the simplest way to track a request:

1. **Get Correlation ID from Response Headers**
```bash
curl -v https://api.example.com/api/projects
# Look for: X-Correlation-ID: a1b2c3d4e5f6
```

2. **Search Logs**
```bash
# Search all log files
grep "a1b2c3d4e5f6" logs/*.log

# Search with context
grep -C 10 "a1b2c3d4e5f6" logs/aura-api-*.log
```

3. **Filter in Log Viewer**
```
CorrelationId == "a1b2c3d4e5f6"
```

### By Trace ID

For distributed tracing across services:

1. **Extract Trace ID**
```bash
# From response headers
X-Trace-ID: 7f8e9d0c1b2a
```

2. **Search All Services**
```bash
# Search backend logs
grep "TraceId.*7f8e9d0c1b2a" logs/aura-api-*.log

# Search frontend logs (if available)
grep "traceId.*7f8e9d0c1b2a" logs/browser-*.log
```

3. **Visualize Trace Timeline**
```
TraceId == "7f8e9d0c1b2a"
| sort by Timestamp
| project Timestamp, SpanId, ParentSpanId, Message, Duration
```

## Common Debugging Scenarios

### Scenario 1: Slow API Request

**Problem**: API request taking too long

**Steps**:

1. **Identify the slow request from frontend logs**
```typescript
// Frontend automatically logs performance
SLOW REQUEST DETECTED: GET /api/projects took 6234ms
```

2. **Get correlation ID from browser network tab**
```
X-Correlation-ID: a1b2c3d4e5f6
```

3. **Search backend logs**
```bash
grep "a1b2c3d4e5f6" logs/aura-api-*.log | grep -E "(Duration|Performance)"
```

4. **Analyze performance breakdown**
```
[INFO] [a1b2c3d4e5f6] Request completed: GET /api/projects - 200 in 6234ms
[INFO] [a1b2c3d4e5f6] Performance: DatabaseQuery completed in 5800ms
[WARN] [a1b2c3d4e5f6] SLOW REQUEST DETECTED: DatabaseQuery took 5800ms
```

5. **Identify the bottleneck**
```csharp
// Add more granular timing
using (var timer = PerformanceTimer.Start(logger, "GetProjects"))
{
    timer.Checkpoint("BeforeDatabase");
    var projects = await db.Projects.ToListAsync();
    timer.Checkpoint("AfterDatabase");
    
    timer.Checkpoint("BeforeSerialization");
    var result = mapper.Map<List<ProjectDto>>(projects);
    timer.Checkpoint("AfterSerialization");
}
```

### Scenario 2: Intermittent Error

**Problem**: Error occurs occasionally, hard to reproduce

**Steps**:

1. **Find error in logs**
```bash
grep "ERROR" logs/errors-*.log | tail -20
```

2. **Extract correlation ID**
```
[ERROR] [xyz123] Failed to generate video for project proj_456
```

3. **Get full request context**
```bash
grep "xyz123" logs/aura-api-*.log
```

4. **Analyze the complete flow**
```
[INFO] [xyz123] Request: POST /api/render from 192.168.1.100
[INFO] [xyz123] Starting video generation for project proj_456
[INFO] [xyz123] Processing 120 frames
[WARN] [xyz123] Frame 87 processing slow: 2300ms
[ERROR] [xyz123] Failed to encode frame 87: Out of memory
[ERROR] [xyz123] Video generation failed
[ERROR] [xyz123] Response: POST /api/render 500 in 245000ms
```

5. **Identify pattern**
```bash
# Check if it's memory-related
grep "Out of memory" logs/*.log | wc -l

# Check if it's specific to large projects
grep "xyz123" logs/*.log | grep "frames"
```

### Scenario 3: Error Propagation Across Services

**Problem**: Frontend error caused by backend issue

**Steps**:

1. **Start from frontend error**
```typescript
[ERROR] [Frontend] Failed to load dashboard
TraceID: abc123, SpanID: def456
Error: Network request failed
```

2. **Find backend request with same trace ID**
```bash
grep "abc123" logs/aura-api-*.log
```

3. **Trace through backend operations**
```
[INFO] [abc123] [def456] Request: GET /api/dashboard
[INFO] [abc123] [ghi789] Child span: DatabaseQuery
[ERROR] [abc123] [ghi789] Database query timeout after 30s
[ERROR] [abc123] [def456] Request failed: DatabaseQueryTimeout
```

4. **Identify root cause**
```
Root cause: Database query timeout
Span hierarchy:
  - Frontend (def456) -> Backend Request (ghi789) -> Database Query
```

### Scenario 4: User-Reported Issue

**Problem**: User reports "Something went wrong"

**Steps**:

1. **Get information from user**
```
- Timestamp: 2024-01-15 14:30:00 UTC
- Action: Tried to export project
- Project ID: proj_789
- User ID: user_123
```

2. **Search logs by time window and user**
```bash
grep "user_123.*proj_789" logs/aura-api-2024-01-15.log | \
  grep -E "14:2[5-9]|14:3[0-5]"
```

3. **Find the correlation ID**
```
[INFO] [corr_abc] User user_123 initiated export for project proj_789
```

4. **Get complete trace**
```bash
grep "corr_abc" logs/aura-api-2024-01-15.log
```

5. **Analyze with context**
```
[INFO] [corr_abc] Export started
[INFO] [corr_abc] Validating project structure
[WARN] [corr_abc] Project contains invalid asset reference: asset_999
[ERROR] [corr_abc] Export validation failed: InvalidAssetReference
[ERROR] [corr_abc] Export failed for user user_123
```

## Using Trace Context Programmatically

### Creating Child Spans

#### Backend (C#)

```csharp
using Aura.Core.Logging;

public async Task<Video> GenerateVideo(string projectId)
{
    // Main operation
    logger.LogInformation("Starting video generation for {ProjectId}", projectId);
    
    // Create child span for sub-operation
    using var childScope = TraceContext.Current?.CreateChildSpan("ProcessFrames");
    try
    {
        var frames = await ProcessFrames(projectId);
        logger.LogInformation("Processed {FrameCount} frames", frames.Count);
        return frames;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Frame processing failed");
        throw;
    }
}
```

#### Frontend (TypeScript)

```typescript
async function loadProjectWithAssets(projectId: string) {
  const componentLogger = logger.forComponent('ProjectLoader');
  
  // Create child span for project load
  const projectSpan = logger.createChildSpan('LoadProject');
  logger.setTraceContext(projectSpan);
  
  try {
    const project = await fetchProject(projectId);
    componentLogger.info('Project loaded', 'load', { projectId });
    
    // Create child span for assets
    const assetsSpan = logger.createChildSpan('LoadAssets');
    logger.setTraceContext(assetsSpan);
    
    const assets = await fetchAssets(project.assetIds);
    componentLogger.info('Assets loaded', 'load', { count: assets.length });
    
    return { project, assets };
  } catch (error) {
    componentLogger.error('Failed to load project', error, 'load');
    throw error;
  }
}
```

## Log Aggregation and Search

### Using grep

```bash
# Find all errors for a specific project
grep "proj_123" logs/*.log | grep ERROR

# Find slow requests
grep "SLOW REQUEST" logs/aura-api-*.log

# Find all logs for a time range
grep "2024-01-15 14:[3-4][0-9]" logs/aura-api-*.log

# Find errors with context
grep -B 5 -A 5 "ERROR.*proj_123" logs/*.log

# Find trace hierarchy
grep "abc123" logs/*.log | sort
```

### Using Log Query Language

If using a log aggregation service (e.g., ELK, Splunk, Azure Log Analytics):

```sql
-- Find all spans in a trace
Logs
| where TraceId == "abc123"
| project Timestamp, Level, SpanId, ParentSpanId, Message, Duration
| order by Timestamp asc

-- Find slow operations
Logs
| where Level == "Performance" and DurationMs > 3000
| summarize count() by OperationName
| order by count_ desc

-- Find error patterns
Logs
| where Level == "ERROR"
| summarize count() by ExceptionType
| order by count_ desc

-- Trace visualization
Logs
| where CorrelationId == "xyz789"
| project Timestamp, Level, Message, Duration
| render timechart
```

## Best Practices for Debugging

### 1. Always Include Context

```csharp
// ❌ Hard to debug
logger.LogError("Operation failed");

// ✅ Easy to debug
logger.LogError(ex, 
    "Failed to generate video for project {ProjectId}, user {UserId}, resolution {Resolution}",
    projectId, userId, resolution);
```

### 2. Use Consistent Naming

```csharp
// ✅ Consistent operation names
logger.LogInformation("VideoGeneration started");
// ... operation ...
logger.LogInformation("VideoGeneration completed");

// Easy to grep
grep "VideoGeneration" logs/*.log
```

### 3. Log at State Transitions

```csharp
public async Task ProcessVideo(string projectId)
{
    logger.LogInformation("Processing started for {ProjectId}", projectId);
    
    try
    {
        logger.LogInformation("Validating project {ProjectId}", projectId);
        await ValidateProject(projectId);
        
        logger.LogInformation("Generating frames for {ProjectId}", projectId);
        await GenerateFrames(projectId);
        
        logger.LogInformation("Encoding video for {ProjectId}", projectId);
        await EncodeVideo(projectId);
        
        logger.LogInformation("Processing completed for {ProjectId}", projectId);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Processing failed for {ProjectId}", projectId);
        throw;
    }
}
```

### 4. Use Structured Properties

```csharp
// Makes it easy to query and filter
logger.LogInformation(
    "Video generated with {FrameCount} frames, {Duration}s, resolution {Resolution}",
    frameCount, duration, resolution);
```

## Troubleshooting Tips

### Can't Find Logs

1. **Check log retention period** (default: 30 days)
2. **Verify log file paths**
3. **Check log level configuration**
4. **Ensure middleware is registered**

### Missing Correlation IDs

1. **Verify CorrelationIdMiddleware is registered** in `Program.cs`
2. **Check middleware order** (should be early in pipeline)
3. **Ensure frontend is sending** `X-Correlation-ID` header

### Missing Trace Context

1. **Verify TraceContext is set** in middleware
2. **Check async context flow** (use `ConfigureAwait(false)` carefully)
3. **Ensure enrichers are registered** in Serilog configuration

## Further Reading

- [Logging Best Practices](./LOGGING_BEST_PRACTICES.md)
- [Log Query Examples](./LOG_QUERY_EXAMPLES.md)
- [W3C Trace Context Specification](https://www.w3.org/TR/trace-context/)
