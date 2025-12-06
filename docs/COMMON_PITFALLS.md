# Common Pitfalls

This document lists common mistakes and anti-patterns to avoid when working on Aura Video Studio.

## Table of Contents

1. [React and Frontend](#react-and-frontend)
2. [Backend and C#](#backend-and-c)
3. [Video Processing](#video-processing)
4. [API Integration](#api-integration)

---

## React and Frontend

### ❌ Using `querySelector` with Fluent UI Class Names

**Problem**: Fluent UI's `makeStyles` generates hashed class names that change between builds.

```tsx
// ❌ WRONG - Will break between builds
const element = containerRef.current?.querySelector('[class*="rulerScrollable"]');
```

**Solution**: Use React refs instead.

```tsx
// ✅ CORRECT
const rulerScrollableRef = useRef<HTMLDivElement>(null);
// ... later
const element = rulerScrollableRef.current;
```

**Why**: Refs are stable references that don't depend on generated class names.

---

### ❌ Checking `isPlaying` Before Syncing Video Position

**Problem**: Video preview doesn't update when seeking while paused.

```tsx
// ❌ WRONG - Ignores seeks when paused
const handleTimeUpdate = () => {
  if (!playbackStore.isPlaying) return; // Bug here
  playbackStore.setCurrentTime(video.currentTime);
};
```

**Solution**: Always sync, regardless of play state.

```tsx
// ✅ CORRECT - Syncs on seek and play
const handleTimeUpdate = () => {
  playbackStore.setCurrentTime(video.currentTime);
};
```

**Why**: Seeking (dragging playhead) should update preview even when paused.

---

### ❌ Using Fixed Delays for SSE Connection

**Problem**: Artificial delays cause race conditions.

```tsx
// ❌ WRONG - Race condition
await new Promise(resolve => setTimeout(resolve, 2000)); // Wait for job registration
// Then connect to SSE
```

**Solution**: Connect immediately and handle missing jobs gracefully.

```tsx
// ✅ CORRECT - Connect immediately
const eventSource = new EventSource(sseUrl);
// SSE endpoint sends initial state even if job isn't running yet
```

**Why**: The 2-second delay doesn't guarantee job is registered, and may miss early events.

---

### ❌ Using `any` Type in Error Handling

**Problem**: TypeScript strict mode forbids `any` type.

```tsx
// ❌ WRONG - Will fail CI
try {
  await operation();
} catch (error: any) { // Forbidden
  console.error(error.message);
}
```

**Solution**: Use `unknown` with type guards.

```tsx
// ✅ CORRECT
try {
  await operation();
} catch (error: unknown) {
  const errorObj = error instanceof Error ? error : new Error(String(error));
  console.error(errorObj.message);
}
```

**Why**: TypeScript strict mode requires proper type narrowing for errors.

---

## Backend and C#

### ❌ Using Reflection to Access Private Fields

**Problem**: Reflection is fragile and breaks when implementation changes.

```csharp
// ❌ WRONG - Extremely fragile
var field = providerType.GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance);
var httpClient = (HttpClient?)field.GetValue(provider);
```

**Solution**: Use proper dependency injection with interfaces.

```csharp
// ✅ CORRECT
public interface IOllamaDirectClient
{
    Task<string> GenerateAsync(string model, string prompt, ...);
}

// Inject via constructor
private readonly IOllamaDirectClient _ollamaClient;
```

**Why**: Reflection breaks encapsulation, is untestable, and fails when wrapped in decorators.

---

### ❌ Setting Job Status to "completed" Without OutputPath

**Problem**: Frontend polls job status but has no file to download.

```csharp
// ❌ WRONG - Job appears complete but no output
await UpdateJobStatusAsync(jobId, "completed", 100); // Missing outputPath
```

**Solution**: Always set outputPath atomically with completed status.

```csharp
// ✅ CORRECT - Atomic update
await UpdateJobStatusAsync(jobId, "completed", 100, outputPath: "/path/to/video.mp4");
```

**Why**: Frontend waits for BOTH status="completed" AND a valid outputPath. Missing outputPath causes stuck jobs.

---

### ❌ Using `.Result` or `.Wait()` on Async Methods

**Problem**: Causes deadlocks in ASP.NET Core.

```csharp
// ❌ WRONG - Can deadlock
var result = ProcessAsync().Result;
var data = FetchDataAsync().Wait();
```

**Solution**: Use `async`/`await` all the way up.

```csharp
// ✅ CORRECT
var result = await ProcessAsync();
await FetchDataAsync();
```

**Why**: ASP.NET Core's synchronization context can deadlock when blocking async methods.

---

### ❌ Not Using `ConfigureAwait(false)` in Library Code

**Problem**: Unnecessary context switching in non-UI code.

```csharp
// ❌ SUBOPTIMAL - Captures context unnecessarily
var data = await FetchDataAsync();
```

**Solution**: Use `ConfigureAwait(false)` in library/service code.

```csharp
// ✅ CORRECT - Avoids context capture
var data = await FetchDataAsync().ConfigureAwait(false);
```

**Why**: Library code doesn't need to resume on original context. Controllers can omit this.

---

## Video Processing

### ❌ Assuming Video Completion Based Only on Progress Percentage

**Problem**: Jobs can reach 100% but still fail or not produce output.

```typescript
// ❌ WRONG - 100% doesn't guarantee success
if (jobData.percent >= 100) {
  return true; // Assume completed
}
```

**Solution**: Check BOTH status AND outputPath.

```typescript
// ✅ CORRECT - Verify completion
if (jobData.status === 'completed' && jobData.outputPath) {
  return true;
}
```

**Why**: A job can reach 100% progress but fail during finalization (e.g., file write error).

---

### ❌ Using Fixed Polling Intervals

**Problem**: Wastes network resources when job is idle.

```typescript
// ❌ WRONG - Always polls every 1 second
while (!completed) {
  await delay(1000);
  await checkStatus();
}
```

**Solution**: Use exponential backoff.

```typescript
// ✅ CORRECT - Backs off when idle
let pollDelay = 500;
while (!completed) {
  await delay(pollDelay);
  const status = await checkStatus();
  
  if (status.progress === lastProgress) {
    pollDelay = Math.min(pollDelay * 1.5, 5000); // Backoff
  } else {
    pollDelay = 500; // Reset when active
  }
}
```

**Why**: Exponential backoff reduces server load when jobs are slow or stuck.

---

## API Integration

### ❌ Not Handling SSE Connection Failures

**Problem**: Frontend gets stuck when SSE fails to connect.

```typescript
// ❌ WRONG - No fallback
const eventSource = new EventSource(sseUrl);
// What if this never connects?
```

**Solution**: Implement timeout and fallback to polling.

```typescript
// ✅ CORRECT - Fallback strategy
const eventSource = new EventSource(sseUrl);
const timeout = setTimeout(() => {
  if (!connectionEstablished) {
    eventSource.close();
    fallbackToPolling(); // Graceful degradation
  }
}, 30000);
```

**Why**: Network issues, CORS problems, or backend restarts can prevent SSE connection.

---

### ❌ Not Including Correlation IDs in Errors

**Problem**: Cannot trace requests across frontend and backend.

```typescript
// ❌ WRONG - Generic error
throw new Error('Request failed');
```

**Solution**: Include correlation ID from backend.

```typescript
// ✅ CORRECT - Traceable error
throw new Error(`Request failed (correlationId: ${errorData.correlationId})`);
```

**Why**: Correlation IDs allow matching frontend errors to backend logs.

---

### ❌ Swallowing Exceptions

**Problem**: Silent failures make debugging impossible.

```csharp
// ❌ WRONG - Exception disappears
try {
    await RiskyOperation();
} catch {
    // Do nothing - bug hides here
}
```

**Solution**: Log and rethrow or handle gracefully.

```csharp
// ✅ CORRECT - Log and rethrow
try {
    await RiskyOperation();
} catch (Exception ex) {
    _logger.LogError(ex, "Risky operation failed");
    throw; // Let caller handle
}
```

**Why**: Silent failures hide bugs and make production issues impossible to diagnose.

---

## Build and Deployment

### ❌ Committing with TODO/FIXME/HACK Comments

**Problem**: Zero-placeholder policy is enforced by pre-commit hooks and CI.

```csharp
// ❌ WRONG - Will be rejected by pre-commit hook
// TODO: Implement hardware acceleration
// FIXME: This breaks on large files
```

**Solution**: Finish implementation or create GitHub Issue.

```csharp
// ✅ CORRECT - Reference issue instead
// Currently using software encoding. Hardware acceleration available via
// separate provider configuration (see issue #123).
```

**Why**: All code must be production-ready when committed. Use GitHub Issues for future work.

---

### ❌ Not Running Pre-build Validation

**Problem**: CI failures that could have been caught locally.

```bash
# ❌ WRONG - Skip validation
npm run build
```

**Solution**: Run validation scripts before building.

```bash
# ✅ CORRECT - Validate first
npm run prebuild  # Runs validate-environment.js
npm run build
npm run postbuild # Runs verify-build.js
```

**Why**: Catches environment issues, placeholder violations, and build problems early.

---

## Quick Reference Checklist

Before submitting a PR, ensure:

- [ ] ✅ No `querySelector` with Fluent UI class names
- [ ] ✅ No reflection to access private fields
- [ ] ✅ No `any` types in error handling
- [ ] ✅ No `.Result` or `.Wait()` on async methods
- [ ] ✅ Job status transitions include outputPath when completed
- [ ] ✅ Video sync works when paused (not just playing)
- [ ] ✅ SSE connections have timeout and polling fallback
- [ ] ✅ No TODO/FIXME/HACK comments in code
- [ ] ✅ All exceptions are logged with correlation IDs
- [ ] ✅ Exponential backoff for polling operations

---

## Getting Help

If you encounter an issue not covered here:

1. Check `docs/ARCHITECTURE_DECISIONS.md` for design rationale
2. Search closed PRs for similar fixes
3. Ask in Discord or create a discussion on GitHub
4. Add new pitfalls to this document when discovered

---

## Change Log

- 2024-12-06: Initial version documenting common pitfalls
