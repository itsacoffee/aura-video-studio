# Resource Management Guide

## Overview

This guide documents best practices for resource management in Aura Video Studio to prevent memory leaks, file handle exhaustion, and process accumulation.

## Critical Resource Types

### 1. HttpClient (Socket Exhaustion Prevention)

**Problem**: Creating new `HttpClient` instances per request leads to socket exhaustion.

**Solution**: Use shared `HttpClient` with proper lifetime management.

#### ✅ Correct Pattern (Dependency Injection)
```csharp
public class MyProvider
{
    private readonly HttpClient _httpClient;
    
    public MyProvider(HttpClient httpClient)
    {
        _httpClient = httpClient; // Injected, managed by DI container
    }
    
    public async Task<string> FetchDataAsync()
    {
        return await _httpClient.GetStringAsync("https://api.example.com/data");
    }
}
```

#### ✅ Correct Pattern (Test Classes)
```csharp
public class MyTests : IDisposable
{
    private readonly HttpClient _httpClient;
    
    public MyTests()
    {
        _httpClient = new HttpClient();
    }
    
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
    
    [Fact]
    public async Task Test()
    {
        // Use _httpClient for all tests
    }
}
```

#### ❌ Wrong Pattern
```csharp
// NEVER DO THIS
public async Task<string> FetchDataAsync()
{
    using var httpClient = new HttpClient(); // Creates new socket per call
    return await httpClient.GetStringAsync("url");
}
```

### 2. Process Management (FFmpeg, External Tools)

**Problem**: Processes not properly disposed can accumulate and exhaust system resources.

**Solution**: Use ProcessManager for tracking and automatic cleanup.

#### ✅ Correct Pattern
```csharp
public class FFmpegService : IFFmpegService
{
    private readonly IProcessManager _processManager;
    
    public async Task<FFmpegResult> ExecuteAsync(string arguments, CancellationToken ct)
    {
        Process? process = null;
        try
        {
            process = new Process { StartInfo = CreateStartInfo(arguments) };
            process.Start();
            
            // Register for tracking
            _processManager?.RegisterProcess(process.Id, "ffmpeg-job");
            
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
            
            return new FFmpegResult { Success = process.ExitCode == 0 };
        }
        catch (OperationCanceledException)
        {
            // Kill process on cancellation
            if (process != null && !process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
            throw;
        }
        finally
        {
            // Always dispose and unregister
            _processManager?.UnregisterProcess(process?.Id ?? 0);
            process?.Dispose();
        }
    }
}
```

### 3. File Handles (Streams, Files)

**Problem**: Unclosed file handles can exhaust available handles and lock files.

**Solution**: Always use `using` statements or proper try-finally disposal.

#### ✅ Correct Pattern
```csharp
// Preferred: using declaration
public async Task<string> ReadFileAsync(string path)
{
    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    using var reader = new StreamReader(stream);
    return await reader.ReadToEndAsync();
}

// Alternative: using block
public async Task WriteFileAsync(string path, string content)
{
    using (var stream = new FileStream(path, FileMode.Create))
    using (var writer = new StreamWriter(stream))
    {
        await writer.WriteAsync(content);
    }
}
```

#### ❌ Wrong Pattern
```csharp
// Missing using - stream not disposed if exception occurs
public string ReadFile(string path)
{
    var stream = new FileStream(path, FileMode.Open);
    var reader = new StreamReader(stream);
    return reader.ReadToEnd(); // Leaks if exception thrown
}
```

### 4. CancellationTokenSource

**Problem**: Not disposing CancellationTokenSource can leak resources.

**Solution**: Always dispose in finally block or use using statement.

#### ✅ Correct Pattern
```csharp
public async Task ExecuteWithTimeoutAsync(TimeSpan timeout)
{
    using var cts = new CancellationTokenSource(timeout);
    try
    {
        await DoWorkAsync(cts.Token);
    }
    catch (OperationCanceledException)
    {
        // Handle cancellation
    }
}

// For linked tokens stored in dictionary
private readonly Dictionary<string, CancellationTokenSource> _tokens = new();

public void StartJob(string jobId, CancellationToken ct)
{
    var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    _tokens[jobId] = cts;
}

public void CleanupJob(string jobId)
{
    if (_tokens.TryRemove(jobId, out var cts))
    {
        cts.Dispose(); // Always dispose
    }
}
```

## Memory Pressure Monitoring

### MemoryPressureMonitor Usage

The `MemoryPressureMonitor` tracks memory usage per job and triggers garbage collection when needed.

#### Integration with JobRunner

```csharp
public class JobRunner
{
    private readonly IMemoryPressureMonitor _memoryMonitor;
    
    private async Task ExecuteJobAsync(string jobId, CancellationToken ct)
    {
        try
        {
            // Start monitoring
            _memoryMonitor?.StartMonitoring(jobId);
            
            // Execute job with periodic memory checks
            await foreach (var progress in ProcessJobAsync(jobId, ct))
            {
                // Update peak memory tracking
                _memoryMonitor?.UpdatePeakMemory(jobId);
                
                // Force GC if memory pressure detected
                _memoryMonitor?.ForceCollectionIfNeeded();
            }
        }
        finally
        {
            // Stop monitoring and log statistics
            var stats = _memoryMonitor?.StopMonitoring(jobId);
            if (stats != null)
            {
                _logger.LogInformation(
                    "Job {JobId} memory: Start={Start}MB, Peak={Peak}MB, End={End}MB, Delta={Delta}MB",
                    jobId, stats.StartMemoryMb, stats.PeakMemoryMb, 
                    stats.EndMemoryMb, stats.MemoryDeltaMb);
            }
        }
    }
}
```

### Custom Memory Thresholds

```csharp
// Configure for low-memory environments
var monitor = new MemoryPressureMonitor(
    logger,
    memoryPressureThresholdMb: 1024, // 1GB threshold
    memoryPressureThresholdPercent: 0.80 // 80% of available
);
```

## Common Pitfalls

### 1. Async Disposal

```csharp
// ❌ Wrong - IDisposable.Dispose() is synchronous
public class MyService : IDisposable
{
    public void Dispose()
    {
        CleanupAsync().Wait(); // Can deadlock
    }
}

// ✅ Correct - Use IAsyncDisposable
public class MyService : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        await CleanupAsync();
    }
}
```

### 2. Finalizers with Managed Resources

```csharp
// ❌ Wrong - Don't use finalizer for managed resources
public class MyService : IDisposable
{
    private HttpClient _client;
    
    ~MyService() // Finalizer
    {
        _client?.Dispose(); // Unnecessary, managed by GC
    }
}

// ✅ Correct - Only for unmanaged resources
public class MyService : IDisposable
{
    private bool _disposed;
    
    public void Dispose()
    {
        if (_disposed) return;
        
        // Dispose managed resources
        _client?.Dispose();
        
        _disposed = true;
        GC.SuppressFinalize(this); // No finalizer needed
    }
}
```

### 3. Forgetting ConfigureAwait in Libraries

```csharp
// ❌ Wrong in library code
public async Task ProcessAsync()
{
    await _service.DoWorkAsync(); // Captures context unnecessarily
}

// ✅ Correct in library code
public async Task ProcessAsync()
{
    await _service.DoWorkAsync().ConfigureAwait(false);
}

// Note: In ASP.NET Core controllers, ConfigureAwait(false) is optional
```

## Testing Resource Management

### Unit Tests

```csharp
[Fact]
public async Task Service_Should_DisposeResources()
{
    // Arrange
    var service = new MyService();
    
    // Act
    await service.ProcessAsync();
    service.Dispose();
    
    // Assert - Verify resources cleaned up
    Assert.Equal(0, service.GetActiveConnectionCount());
}
```

### Stress Tests

```csharp
[Fact]
public async Task Service_UnderLoad_Should_NotLeakMemory()
{
    var initialMemory = GC.GetTotalMemory(forceFullCollection: true);
    
    // Execute many operations
    for (int i = 0; i < 1000; i++)
    {
        using var service = new MyService();
        await service.ProcessAsync();
    }
    
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    
    var finalMemory = GC.GetTotalMemory(forceFullCollection: false);
    var leakedMb = (finalMemory - initialMemory) / (1024.0 * 1024.0);
    
    Assert.True(leakedMb < 10, $"Leaked {leakedMb:F1}MB");
}
```

## Monitoring and Diagnostics

### Logging

Always log resource usage for long-running operations:

```csharp
_logger.LogInformation(
    "Job {JobId} completed: Duration={Duration}s, Memory={MemoryMb}MB, Processes={ProcessCount}",
    jobId, duration.TotalSeconds, memoryUsageMb, activeProcessCount);
```

### Health Checks

Implement health checks for resource monitoring:

```csharp
public class ResourceHealthCheck : IHealthCheck
{
    private readonly IProcessManager _processManager;
    private readonly IMemoryPressureMonitor _memoryMonitor;
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken ct = default)
    {
        var processCount = _processManager.GetProcessCount();
        var memoryPressure = _memoryMonitor.IsUnderMemoryPressure();
        
        if (processCount > 100 || memoryPressure)
        {
            return HealthCheckResult.Degraded(
                $"High resource usage: {processCount} processes, memory pressure: {memoryPressure}");
        }
        
        return HealthCheckResult.Healthy();
    }
}
```

## Checklist for Code Reviews

- [ ] All IDisposable objects have using statements or try-finally disposal
- [ ] No new HttpClient() in request handlers or loops
- [ ] Process.Dispose() called for all Process objects
- [ ] CancellationTokenSource.Dispose() in finally blocks
- [ ] FileStream uses FileShare.Read for read operations
- [ ] Large memory allocations cleared after use
- [ ] Long-running operations have memory monitoring
- [ ] Async methods use ConfigureAwait(false) in library code
- [ ] No finalizers unless managing unmanaged resources
- [ ] Tests verify resource cleanup

## References

- [ProcessManager](/home/runner/work/aura-video-studio/aura-video-studio/Aura.Core/Services/FFmpeg/ProcessManager.cs)
- [MemoryPressureMonitor](/home/runner/work/aura-video-studio/aura-video-studio/Aura.Core/Services/Memory/MemoryPressureMonitor.cs)
- [ResourceCleanupManager](/home/runner/work/aura-video-studio/aura-video-studio/Aura.Core/Services/ResourceCleanupManager.cs)
- [CleanupService](/home/runner/work/aura-video-studio/aura-video-studio/Aura.Core/Services/CleanupService.cs)
