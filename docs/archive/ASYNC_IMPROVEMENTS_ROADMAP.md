# Async Improvements Roadmap

This document identifies additional methods that could benefit from being made async to improve performance and prevent potential deadlocks.

## Analysis Date
2025-01-27

## Current Status

✅ **Completed**: All critical blocking async calls have been fixed
- Shutdown handlers use Task.Run with timeout
- Service constructors use fire-and-forget patterns
- Synchronous wrappers use Task.Run to avoid deadlocks

## Additional Methods That Could Be Made Async

### High Priority

#### 1. File I/O Operations

**Location**: Multiple services throughout `Aura.Core`

**Current Pattern**:
```csharp
var content = File.ReadAllText(path);
File.WriteAllText(path, content);
```

**Recommended Pattern**:
```csharp
var content = await File.ReadAllTextAsync(path).ConfigureAwait(false);
await File.WriteAllTextAsync(path, content).ConfigureAwait(false);
```

**Files to Review**:
- `Aura.Core/Services/Assets/AssetLibraryService.cs` - File operations in sync methods
- `Aura.Core/Configuration/KeyStore.cs` - File reads/writes
- `Aura.Core/Services/Storage/EnhancedLocalStorageService.cs` - File operations
- Any service that performs file I/O in synchronous methods

**Impact**: Medium - Improves responsiveness, especially with large files

#### 2. Database Operations

**Location**: `Aura.Core/Data/` and related services

**Current Pattern**:
```csharp
var result = _context.Projects.ToList();
```

**Recommended Pattern**:
```csharp
var result = await _context.Projects.ToListAsync().ConfigureAwait(false);
```

**Files to Review**:
- All repository classes that use Entity Framework
- Services that query the database synchronously

**Impact**: High - Database operations should always be async

#### 3. HTTP Client Calls

**Location**: Provider implementations in `Aura.Providers/`

**Current Pattern**:
```csharp
var response = _httpClient.GetStringAsync(url).Result;
```

**Recommended Pattern**:
```csharp
var response = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
```

**Files to Review**:
- All provider implementations
- Services that make HTTP calls

**Impact**: High - HTTP calls should always be async

### Medium Priority

#### 4. Configuration Loading

**Location**: `Aura.Core/Configuration/` services

**Current Pattern**:
```csharp
var config = LoadConfiguration().Result;
```

**Recommended Pattern**:
```csharp
var config = await LoadConfigurationAsync().ConfigureAwait(false);
```

**Impact**: Low-Medium - Configuration loading happens at startup, but async is still better

#### 5. Cache Operations

**Location**: Services with caching logic

**Current Pattern**:
```csharp
var cached = _cache.Get(key);
```

**Recommended Pattern**:
```csharp
var cached = await _cache.GetAsync(key).ConfigureAwait(false);
```

**Impact**: Low - Most cache operations are fast, but async is more consistent

### Low Priority (Nice to Have)

#### 6. Logging Operations

**Location**: High-volume logging scenarios

**Current Pattern**:
```csharp
_logger.LogInformation("Message");
```

**Note**: Most logging is already async-friendly, but consider async logging for high-volume scenarios.

**Impact**: Very Low - Logging is typically fast enough

## Implementation Guidelines

### When to Make Methods Async

✅ **Do make async**:
- File I/O operations
- Database operations
- HTTP/network calls
- Long-running computations (use Task.Run)
- Operations that wait on external resources

❌ **Don't make async**:
- Simple property getters/setters
- In-memory calculations
- Synchronous validation logic
- Operations that must complete synchronously (e.g., constructors)

### Migration Pattern

**Step 1**: Identify synchronous method
```csharp
public string LoadData(string path)
{
    return File.ReadAllText(path);
}
```

**Step 2**: Create async version
```csharp
public async Task<string> LoadDataAsync(string path, CancellationToken ct = default)
{
    return await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
}
```

**Step 3**: Update callers to use async version
```csharp
// Before
var data = service.LoadData(path);

// After
var data = await service.LoadDataAsync(path).ConfigureAwait(false);
```

**Step 4**: If synchronous version is still needed, use Task.Run wrapper
```csharp
public string LoadData(string path)
{
    return Task.Run(async () => await LoadDataAsync(path).ConfigureAwait(false))
        .GetAwaiter().GetResult();
}
```

## Testing Strategy

### 1. Unit Tests
- Test async methods with proper await
- Test cancellation token support
- Test timeout scenarios

### 2. Integration Tests
- Test async operations under load
- Test concurrent async operations
- Test deadlock scenarios

### 3. Performance Tests
- Compare sync vs async performance
- Measure throughput improvements
- Monitor resource usage

## Monitoring

Use `AsyncOperationMonitor` to track:
- Operation duration
- Stuck operations
- Timeout scenarios
- Performance metrics

## Priority Order

1. **Phase 1** (Immediate): Database operations, HTTP calls
2. **Phase 2** (Short-term): File I/O operations
3. **Phase 3** (Medium-term): Configuration loading, cache operations
4. **Phase 4** (Long-term): Remaining synchronous operations

## Success Metrics

- ✅ Zero blocking async calls (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`)
- ✅ All I/O operations are async
- ✅ All database operations are async
- ✅ All HTTP calls are async
- ✅ Improved application responsiveness
- ✅ Better resource utilization

## Notes

- Always use `ConfigureAwait(false)` in library code
- Always support `CancellationToken` in async methods
- Consider backward compatibility when migrating
- Update tests to use async patterns
- Document breaking changes

---

**Last Updated**: 2025-01-27
**Status**: Active - Ongoing improvements

