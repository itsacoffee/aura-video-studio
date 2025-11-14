# SSE Progress Monitoring and Cancellation Guide

This guide documents the real-time progress monitoring and job cancellation features in Aura Video Studio.

## Overview

The system provides real-time progress updates for video generation jobs using Server-Sent Events (SSE), with support for job cancellation at any point during execution.

## Architecture

### Backend Components

#### 1. SSE Endpoint
- **Endpoint**: `GET /api/jobs/{jobId}/events`
- **Location**: `Aura.Api/Controllers/JobsController.cs`
- **Features**:
  - Real-time progress streaming
  - Automatic reconnection support via Last-Event-ID
  - Keep-alive messages every 10 seconds
  - Event ID generation for reliable reconnection

#### 2. Progress Service
- **Location**: `Aura.Api/Services/ProgressService.cs`
- **Purpose**: Tracks and broadcasts progress updates
- **Features**:
  - In-memory cache for progress history
  - Subscriber pattern for broadcasting updates
  - Progress parsing with percentage extraction
  - Automatic cleanup

#### 3. Job Runner
- **Location**: `Aura.Core/Orchestrator/JobRunner.cs`
- **Features**:
  - CancellationTokenSource per job
  - Background job execution
  - Progress reporting via IProgress<T>
  - Cancellation support

#### 4. Cancellation Endpoint
- **Endpoint**: `POST /api/jobs/{jobId}/cancel`
- **Location**: `Aura.Api/Controllers/JobsController.cs`
- **Validation**:
  - Only cancels Running or Queued jobs
  - Returns appropriate error for terminal states

### Frontend Components

#### 1. SSE Client
- **Location**: `Aura.Web/src/services/api/sseClient.ts`
- **Classes**:
  - `SSEClient`: Legacy wrapper with automatic reconnection
  - `SseClient`: Modern event-based client
- **Features**:
  - Exponential backoff for reconnection
  - Connection state tracking
  - Timeout handling
  - Proper cleanup

#### 2. Jobs API Client
- **Location**: `Aura.Web/src/features/render/api/jobs.ts`
- **Functions**:
  - `subscribeToJobEvents()`: Subscribe to SSE events
  - `cancelJob()`: Cancel a running job
  - `retryJob()`: Retry a failed job

#### 3. useJobProgress Hook
- **Location**: `Aura.Web/src/hooks/useJobProgress.ts`
- **Purpose**: React hook for managing SSE lifecycle
- **Features**:
  - Automatic cleanup on unmount
  - Terminal event handling
  - Error recovery with retry

#### 4. RenderStatusDrawer Component
- **Location**: `Aura.Web/src/components/RenderStatus/RenderStatusDrawer.tsx`
- **Features**:
  - Real-time progress display
  - Cancel button (visible when job is Running)
  - Step-by-step progress visualization
  - Error display with remediation

## SSE Event Types

The system emits the following event types:

### 1. `job-status`
Sent when job transitions to a new status.

```json
{
  "status": "Running",
  "stage": "Script",
  "percent": 15,
  "correlationId": "abc123"
}
```

### 2. `step-status`
Sent when a new step starts.

```json
{
  "step": "Script",
  "status": "started",
  "phase": "plan",
  "correlationId": "abc123"
}
```

### 3. `step-progress`
Sent periodically during step execution (most frequent event).

```json
{
  "step": "Script",
  "phase": "plan",
  "progressPct": 25,
  "message": "Generating script...",
  "substageDetail": "Scene 3 of 5",
  "currentItem": 3,
  "totalItems": 5,
  "elapsedTime": "00:01:23",
  "estimatedTimeRemaining": "00:02:15",
  "correlationId": "abc123"
}
```

### 4. `job-completed`
Sent when job finishes successfully.

```json
{
  "status": "Succeeded",
  "jobId": "job-123",
  "artifacts": [
    {
      "name": "output.mp4",
      "path": "/artifacts/job-123/output.mp4",
      "type": "video/mp4",
      "sizeBytes": 12345678
    }
  ],
  "output": {
    "videoPath": "/artifacts/job-123/output.mp4",
    "subtitlePath": "/artifacts/job-123/subtitles.srt",
    "sizeBytes": 12345678
  },
  "correlationId": "abc123"
}
```

### 5. `job-failed`
Sent when job fails.

```json
{
  "status": "Failed",
  "jobId": "job-123",
  "stage": "Rendering",
  "errorMessage": "FFmpeg encoding failed",
  "errors": [
    {
      "code": "FFMPEG_ERROR",
      "message": "FFmpeg encoding failed",
      "remediation": "Check FFmpeg installation and system resources"
    }
  ],
  "logs": ["Last 10 log entries..."],
  "correlationId": "abc123"
}
```

### 6. `job-cancelled`
Sent when job is cancelled by user.

```json
{
  "status": "Cancelled",
  "jobId": "job-123",
  "stage": "Voice",
  "message": "Job was cancelled by user",
  "correlationId": "abc123"
}
```

### 7. `warning`
Sent for non-fatal warnings during execution.

```json
{
  "message": "API rate limit approaching",
  "step": "Script",
  "correlationId": "abc123"
}
```

### 8. `error`
Sent for connection or system errors.

```json
{
  "message": "Connection lost",
  "correlationId": "abc123"
}
```

## Job State Machine

Jobs follow a strict state machine:

```
Queued ──┬─> Running ──┬─> Succeeded (terminal)
         │             ├─> Failed (terminal)
         │             └─> Canceled (terminal)
         └─> Canceled (terminal)
```

**Valid Transitions**:
- `Queued → Running`: Job starts execution
- `Queued → Canceled`: Job cancelled before starting
- `Running → Succeeded`: Job completed successfully
- `Running → Failed`: Job encountered an error
- `Running → Canceled`: Job cancelled during execution

**Terminal States** (no further transitions):
- `Succeeded`
- `Failed`
- `Canceled`

## Progress Ranges

The progress percentage (0-100) maps to pipeline stages:

| Range | Stage | Description |
|-------|-------|-------------|
| 0-15% | Script | LLM script generation |
| 15-35% | TTS | Text-to-speech synthesis |
| 35-65% | Visuals | Image generation/selection |
| 65-85% | Composition | Timeline composition |
| 85-100% | Rendering | Final video rendering |

## Usage Examples

### Backend: Creating and Monitoring a Job

```csharp
// Create job with cancellation token
var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
var job = await jobRunner.CreateAndStartJobAsync(
    brief, planSpec, voiceSpec, renderSpec,
    correlationId: "req-123",
    isQuickDemo: false,
    ct: cts.Token
);

// Cancel job later
bool cancelled = jobRunner.CancelJob(job.Id);
```

### Frontend: Subscribing to Progress Updates

```typescript
import { subscribeToJobEvents, cancelJob } from '@/features/render/api/jobs';

// Subscribe to progress
const unsubscribe = subscribeToJobEvents(
  jobId,
  (event) => {
    switch (event.type) {
      case 'step-progress':
        console.log('Progress:', event.data.progressPct, '%');
        break;
      case 'job-completed':
        console.log('Job completed!', event.data.output);
        break;
      case 'job-failed':
        console.error('Job failed:', event.data.errorMessage);
        break;
    }
  },
  (error) => {
    console.error('Connection error:', error);
  }
);

// Cancel job
await cancelJob(jobId);

// Cleanup when done
unsubscribe();
```

### React Component with useJobProgress Hook

```tsx
import { useJobProgress } from '@/hooks/useJobProgress';
import { cancelJob } from '@/features/render/api/jobs';

function JobMonitor({ jobId }: { jobId: string }) {
  const [progress, setProgress] = useState(0);
  const [status, setStatus] = useState('Running');

  useJobProgress(jobId, (event) => {
    if (event.type === 'step-progress') {
      setProgress(event.data.progressPct);
    }
    if (event.type === 'job-status') {
      setStatus(event.data.status);
    }
  });

  const handleCancel = async () => {
    try {
      await cancelJob(jobId);
    } catch (error) {
      console.error('Cancel failed:', error);
    }
  };

  return (
    <div>
      <ProgressBar value={progress} />
      <p>Status: {status}</p>
      {status === 'Running' && (
        <button onClick={handleCancel}>Cancel</button>
      )}
    </div>
  );
}
```

## Reconnection Support

The SSE endpoint supports automatic reconnection using the Last-Event-ID mechanism:

1. **Client tracks event IDs**: Each event has a unique ID in format `{timestamp}-{counter}`
2. **Connection drops**: Client detects disconnection
3. **Reconnect with Last-Event-ID**: Client sends `Last-Event-ID` header or query parameter
4. **Server resumes**: Backend sends only events after the last received ID

Example reconnection:
```typescript
const lastEventId = '1699564800000-42';
const url = `/api/jobs/${jobId}/events?lastEventId=${lastEventId}`;
```

## Multiple Concurrent Jobs

The system supports monitoring multiple jobs simultaneously:

```typescript
const jobs = ['job-1', 'job-2', 'job-3'];

const unsubscribers = jobs.map(jobId =>
  subscribeToJobEvents(jobId, (event) => {
    console.log(`Job ${jobId}:`, event.type, event.data);
  })
);

// Cleanup all
unsubscribers.forEach(unsub => unsub());
```

## Testing

### Backend Tests

Run SSE resilience tests:
```bash
dotnet test --filter "FullyQualifiedName~SseResilienceTests"
```

Run progress and cancellation tests:
```bash
dotnet test --filter "FullyQualifiedName~SseProgressAndCancellationTests"
```

### Frontend Tests

The frontend includes comprehensive tests in:
- `Aura.Web/src/services/api/__tests__/videoApi.test.ts`
- `Aura.Web/src/services/api/__tests__/transport.test.ts`

## Troubleshooting

### SSE Connection Fails

**Symptoms**: Events not received, frequent reconnections

**Solutions**:
1. Check CORS headers in API configuration
2. Verify `X-Accel-Buffering: no` header is set
3. Check firewall/proxy settings
4. Verify API is running and accessible

### Progress Doesn't Update

**Symptoms**: Progress stuck at same percentage

**Solutions**:
1. Check job logs: `GET /api/jobs/{jobId}`
2. Verify job status is `Running`, not stuck in `Queued`
3. Check backend logs for errors
4. Restart API if progress service is unresponsive

### Cancellation Doesn't Work

**Symptoms**: Cancel button does nothing, job continues running

**Solutions**:
1. Verify job is in `Running` or `Queued` state
2. Check cancellation token is being passed to all operations
3. Verify no blocking operations that don't check cancellation
4. Check backend logs for cancellation errors

### Multiple Jobs Interfere

**Symptoms**: Progress from one job shows in another

**Solutions**:
1. Verify each SSE connection uses correct jobId
2. Check that cleanup functions are called on unmount
3. Ensure state is isolated per job (not shared)

## Performance Considerations

### Backend

- **Keep-alive interval**: 10 seconds (configurable in JobsController)
- **Poll interval**: 500ms for responsive updates
- **Connection timeout**: 5 minutes per connection
- **Memory**: Progress cached for 1 hour per job

### Frontend

- **Reconnection**: Exponential backoff starting at 1 second
- **Max retries**: 5 reconnection attempts
- **Timeout**: 5 minutes per connection
- **Cleanup**: Automatic on component unmount

## Security

### API Authorization

All endpoints require authentication (when auth is enabled):
- SSE endpoint checks auth token
- Cancel endpoint validates user owns the job
- Progress data scoped to authenticated user

### Rate Limiting

Recommended rate limits:
- SSE connections: 10 concurrent per user
- Cancel requests: 10 per minute per user
- Job creation: 5 per minute per user

## Future Enhancements

Potential improvements:
1. **WebSocket alternative**: For environments where SSE is problematic
2. **Push notifications**: For long-running jobs
3. **Progress persistence**: Resume monitoring after browser refresh
4. **Batch operations**: Cancel/monitor multiple jobs at once
5. **Admin dashboard**: Monitor all jobs system-wide

## Related Documentation

- API Reference
- [Job Orchestration](../JOB_ORCHESTRATION_IMPLEMENTATION_SUMMARY.md)
- [SSE Implementation Summary](../API_SSE_IMPLEMENTATION_SUMMARY.md)
- [Error Handling Guide](../ERROR_HANDLING_GUIDE.md)
