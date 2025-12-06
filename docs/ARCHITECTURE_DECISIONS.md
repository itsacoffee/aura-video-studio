# Architecture Decisions

This document captures key architectural decisions made in the Aura Video Studio project to prevent anti-patterns and ensure maintainability.

## Table of Contents

1. [Ollama Integration Pattern](#ollama-integration-pattern)
2. [Video Export Job State Management](#video-export-job-state-management)
3. [React Component Refs vs Query Selectors](#react-component-refs-vs-query-selectors)
4. [Video Preview Synchronization](#video-preview-synchronization)

---

## Ollama Integration Pattern

### Decision

**Use `IOllamaDirectClient` with proper dependency injection instead of reflection to access Ollama provider internals.**

### Context

Previous implementation in `IdeationService.cs` used reflection to access private fields of `OllamaLlmProvider`:

```csharp
// ❌ OLD APPROACH (DO NOT USE)
var httpClientField = providerType.GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance);
var httpClient = (HttpClient?)httpClientField.GetValue(_llmProvider);
```

This approach had several critical problems:

1. **Fragile**: Breaks when field names change
2. **Untestable**: Cannot mock private field access
3. **Violates encapsulation**: Bypasses intended provider interface
4. **Fails with decorators**: Doesn't work when provider is wrapped

### Solution

Created a clean interface and implementation:

```csharp
// ✅ NEW APPROACH (USE THIS)
public interface IOllamaDirectClient
{
    Task<string> GenerateAsync(string model, string prompt, ...);
    Task<bool> IsAvailableAsync(CancellationToken ct);
    Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken ct);
}

public class OllamaDirectClient : IOllamaDirectClient
{
    // Injected via IHttpClientFactory with proper lifetime management
    private readonly HttpClient _httpClient;
    private readonly OllamaSettings _settings;
    
    // Implementation with retry logic and timeout handling
}
```

Registered in DI container:

```csharp
// Program.cs
builder.Services.AddHttpClient<IOllamaDirectClient, OllamaDirectClient>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
});
```

### Benefits

- **Testable**: Easy to mock `IOllamaDirectClient` in tests
- **Maintainable**: Changes to Ollama provider don't break ideation service
- **Proper lifetime management**: HttpClient managed by `IHttpClientFactory`
- **Retry logic**: Built-in exponential backoff for transient failures
- **Heartbeat logging**: Tracks long-running requests to detect hangs

### Files Affected

- `Aura.Core/Providers/IOllamaDirectClient.cs` - Interface definition
- `Aura.Core/Providers/OllamaDirectClient.cs` - Implementation
- `Aura.Core/Services/Ideation/IdeationService.cs` - Refactored to use interface
- `Aura.Api/Program.cs` - DI registration

---

## Video Export Job State Management

### Decision

**Require `outputPath` to be set atomically with "completed" status, and reject completion without it.**

### Context

Jobs were completing (status="completed", progress=100%) but `outputPath` was null, causing frontend polling to fail at 72%. This was a race condition where:

1. Job runner set status to "completed" 
2. But didn't set `outputPath` in the same update
3. Frontend saw "completed" status and stopped polling
4. But had no file to download

### Solution

#### Backend (`ExportJobService.cs`)

```csharp
public Task UpdateJobStatusAsync(string jobId, string status, int percent, string? outputPath = null, ...)
{
    // CRITICAL: Require outputPath when status is "completed"
    if (status == "completed" && string.IsNullOrWhiteSpace(outputPath))
    {
        _logger.LogError(
            "CRITICAL: Job {JobId} attempted to transition to 'completed' without outputPath. " +
            "This will cause frontend polling to fail. Rejecting status update.", 
            jobId);
        return Task.CompletedTask; // Don't update - force caller to provide outputPath
    }
    
    // Atomic update of both status and outputPath
    var updatedJob = job with
    {
        Status = status,
        OutputPath = outputPath ?? job.OutputPath, // Preserve existing if not provided
        ...
    };
}
```

#### Frontend (`FinalExport.tsx`)

```typescript
// ARCHITECTURAL FIX: Only consider completed if BOTH conditions met
function checkJobCompletion(jobData: JobStatusData): boolean {
  if (status === 'completed') {
    const hasOutput = jobData.outputPath || (jobData.artifacts?.length > 0);
    
    if (!hasOutput) {
      // Job says completed but no output - backend bug, keep polling
      console.error('[FinalExport] Job completed but outputPath missing - backend bug');
      return false; // NOT truly completed
    }
    
    return true; // Truly completed with output
  }
  return false;
}
```

Removed the 2-second `JOB_REGISTRATION_DELAY_MS` that was causing race conditions. SSE now connects immediately.

### Benefits

- **No more "72% stuck" bug**: Jobs cannot complete without an output file
- **Atomic updates**: Status and outputPath change together
- **Better error detection**: Backend logs when update is rejected
- **No race conditions**: Removed artificial delay before SSE connection

### Files Affected

- `Aura.Core/Services/Export/ExportJobService.cs` - Validation and atomic updates
- `Aura.Web/src/components/VideoWizard/steps/FinalExport.tsx` - Fixed completion check
- `Aura.Core/Services/Export/JobStateManager.cs` - State machine (created but not yet integrated)

---

## React Component Refs vs Query Selectors

### Decision

**Always use refs to access DOM elements within React components. Never use `querySelector` with Fluent UI class names.**

### Context

Previous implementation in `Timeline.tsx` used `querySelector` with Fluent UI class names:

```tsx
// ❌ OLD APPROACH (DO NOT USE)
const rulerScrollable = containerRef.current?.querySelector('[class*="rulerScrollable"]');
```

**Problem**: Fluent UI's `makeStyles` generates hashed class names (e.g., `rulerScrollable-abc123`) that change between builds. This breaks the selector.

### Solution

Use React refs instead:

```tsx
// ✅ NEW APPROACH (USE THIS)
// 1. Create ref at component level
const rulerScrollableRef = useRef<HTMLDivElement>(null);

// 2. Use ref in event handlers
const handleMouseMove = (e: globalThis.MouseEvent) => {
  const rulerScrollable = rulerScrollableRef.current;
  if (!rulerScrollable) return;
  // ... use rulerScrollable
};

// 3. Attach ref to element
<div className={styles.rulerScrollable} ref={rulerScrollableRef}>
  ...
</div>
```

### Benefits

- **Stable references**: Refs don't change between builds
- **Type-safe**: TypeScript knows the element type
- **Performance**: Direct reference, no DOM traversal
- **Maintainable**: Survives CSS framework changes

### Files Affected

- `Aura.Web/src/components/OpenCut/Timeline.tsx` - Fixed playhead drag

---

## Video Preview Synchronization

### Decision

**Always sync video element time from playback store, not just when playing.**

### Context

Previous implementation only updated video position during playback:

```tsx
// ❌ OLD APPROACH (DO NOT USE)
const handleTimeUpdate = () => {
  if (!playbackStore.isPlaying) return; // BUG: Ignores seeks when paused
  playbackStore.setCurrentTime(video.currentTime);
};
```

**Problem**: Dragging the playhead while paused didn't update the video preview because `isPlaying` was false.

### Solution

Two-way synchronization:

```tsx
// ✅ NEW APPROACH (USE THIS)

// 1. Video → Store: Always sync, even when paused
const handleTimeUpdate = () => {
  playbackStore.setCurrentTime(video.currentTime); // Removed isPlaying check
};

// 2. Store → Video: Sync when playhead is dragged
useEffect(() => {
  const video = videoRef.current;
  if (!video || !videoSrc) return;
  
  // Only sync if significant difference (avoid feedback loop)
  if (Math.abs(video.currentTime - playbackStore.currentTime) > 0.1) {
    video.currentTime = playbackStore.currentTime;
  }
}, [playbackStore.currentTime, videoSrc]);
```

### Benefits

- **Seeking works when paused**: Dragging playhead updates preview
- **Smooth synchronization**: 0.1s threshold prevents jitter
- **No feedback loops**: Threshold prevents infinite updates

### Files Affected

- `Aura.Web/src/components/OpenCut/PreviewPanel.tsx` - Fixed video sync

---

## Future Considerations

### Performance Monitoring

Consider adding performance tracking for:
- Video render pipeline stages
- Ollama API call durations
- Job state transition timing

### State Machine Integration

The `JobStateManager` class was created but not yet fully integrated. Future work should:
- Replace string-based status checks with enum-based state machine
- Add state transition validation
- Emit events for SSE subscribers

### Error Recovery

Implement circuit breaker pattern for:
- Ollama API calls (prevent cascading failures)
- Video rendering pipeline (auto-retry with backoff)
- SSE connections (graceful degradation to polling)

---

## References

- PR #XXX: Comprehensive architectural fixes
- Issue #XXX: Video export fails at 72%
- Issue #XXX: Ideation timeout errors
- Issue #XXX: OpenCut playhead not draggable

## Change Log

- 2024-12-06: Initial version documenting architectural fixes
