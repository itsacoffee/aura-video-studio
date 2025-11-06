# Job Queue & SSE Resilience Specification

## Overview

This document specifies the behavior and guarantees of the job queue and Server-Sent Events (SSE) system in Aura Video Studio.

## Job Queue Invariants

### State Machine

Jobs follow a strict state machine with the following states and transitions:

```
Queued → Running → Done/Succeeded
         ↓          ↓
         ↓       Failed
         ↓          ↓
       Canceled ←---┘
```

**Valid Transitions:**
- `Queued → Running` - Job starts execution
- `Queued → Canceled` - Job canceled before starting
- `Running → Done/Succeeded` - Job completed successfully
- `Running → Failed` - Job encountered an error
- `Running → Canceled` - Job canceled during execution

**Terminal States:**
- `Done`/`Succeeded` - Successfully completed
- `Failed` - Encountered an error
- `Canceled` - User or system canceled

Terminal states cannot transition to any other state.

### Timestamp Invariants

Jobs maintain the following timestamps with strict ordering:

```
CreatedUtc ≤ QueuedUtc ≤ StartedUtc ≤ EndedUtc
                                      ↑
                          CompletedUtc | CanceledUtc | FailedAt
```

**Timestamp Definitions:**

| Timestamp | Set When | Required |
|-----------|----------|----------|
| `CreatedUtc` | Job created | Always |
| `QueuedUtc` | Job enters queue (same as CreatedUtc) | Always |
| `StartedUtc` | Job transitions to Running | When started |
| `CompletedUtc` | Job transitions to Done/Succeeded | When completed |
| `CanceledUtc` | Job transitions to Canceled | When canceled |
| `EndedUtc` | Job enters terminal state | Terminal states only |

**Ordering Rules:**
1. `CreatedUtc` is immutable and set on creation
2. `QueuedUtc` equals `CreatedUtc` for newly created jobs
3. `StartedUtc` must be ≥ `QueuedUtc`
4. `CompletedUtc` must be ≥ `StartedUtc`
5. `CanceledUtc` must be ≥ `StartedUtc` (if job was running)
6. `EndedUtc` is set to `CompletedUtc`, `CanceledUtc`, or failure timestamp

### Monotonic Progress

Progress values must be **monotonically increasing**:

- Progress ranges from 0 to 100
- Once set, progress can only increase or stay the same
- Progress never decreases
- Attempting to set a lower progress value keeps the current value

**Implementation:**
```csharp
public Job WithMonotonicProgress(int newPercent)
{
    var safePercent = Math.Max(this.Percent, Math.Clamp(newPercent, 0, 100));
    return this with { Percent = safePercent };
}
```

### Resumability Rules

Jobs may support resumption depending on the stage:

**Resumable Stages:**
- `Script` - Can resume after script generation
- `Voice` - Can resume after TTS synthesis
- `Visuals` - Can resume after visual generation

**Non-Resumable Stages:**
- `Rendering` - Atomic operation, cannot resume mid-render
- `Initialization` - Too early to have meaningful checkpoints

**Resume Support:**
- `CanResume` flag indicates if job supports resumption
- `LastCompletedStep` tracks the last successful checkpoint
- Resume starts from `LastCompletedStep`, skipping completed work

## Server-Sent Events (SSE) Specification

### Event Types

The SSE endpoint emits the following event types:

| Event Type | Purpose | Data Structure |
|------------|---------|----------------|
| `job-status` | Overall job status changes | `{ status, stage, percent, correlationId }` |
| `step-status` | Stage/phase transitions | `{ step, status, phase, correlationId }` |
| `step-progress` | Progress within a step | `{ step, phase, progressPct, message, correlationId }` |
| `warning` | Non-fatal warnings | `{ message, step, correlationId }` |
| `error` | Error occurred | `{ message, correlationId }` |
| `job-completed` | Successful completion | `{ status, jobId, artifacts, output, correlationId }` |
| `job-failed` | Job failure | `{ status, jobId, stage, errors, errorMessage, logs, correlationId }` |
| `job-cancelled` | Job cancellation | `{ status, jobId, stage, message, correlationId }` |

### Event IDs

All SSE events include a unique event ID for reconnection support:

**Format:** `{timestamp}-{counter}`

**Example:** `1704067200000-5`

**Properties:**
- Timestamp: Unix milliseconds (UTC)
- Counter: Incremental within the connection
- Event IDs are monotonically increasing
- Used for Last-Event-ID reconnection

### Stage to Phase Mapping

Stages are mapped to high-level phases for consistent UI display:

| Stage | Phase | Progress Range |
|-------|-------|----------------|
| Initialization, Queued, Script, Planning, Brief | `plan` | 0-15% |
| TTS, Audio, Voice | `tts` | 15-35% |
| Visuals, Images, Assets | `visuals` | 35-65% |
| Composition, Timeline, Compose | `compose` | 65-85% |
| Rendering, Render, Encode | `render` | 85-100% |
| Complete, Done | `complete` | 100% |
| Unknown | `processing` | N/A |

### Heartbeat/Keepalive

SSE connections send keepalive comments to prevent timeout:

**Interval:** 10 seconds

**Format:** `: keepalive: 2024-01-01T12:00:00.000Z\n\n`

**Purpose:**
- Detect broken connections
- Prevent proxy/load balancer timeouts
- Trigger reconnection if no heartbeat received

### Reconnection Protocol

SSE clients can recover from transient disconnections using Last-Event-ID:

**Initial Connection:**
```
GET /api/jobs/{jobId}/events
```

**Reconnection (after disconnect):**
```
GET /api/jobs/{jobId}/events?lastEventId={lastEventId}
```

**Server Behavior:**
1. Parse `lastEventId` from query parameter or `Last-Event-ID` header
2. Send only events that occurred after `lastEventId`
3. Include event IDs in all subsequent events

**Client Behavior:**
1. Track `lastEventId` from each received event
2. On disconnect, attempt reconnection with exponential backoff
3. Include `lastEventId` in reconnection request
4. Merge new events with existing state
5. Enforce monotonic progress on client side

**Backoff Strategy:**
```
Attempt 1: 1 second
Attempt 2: 2 seconds
Attempt 3: 4 seconds
Attempt 4: 8 seconds
Attempt 5: 16 seconds
Max delay: 30 seconds
Max attempts: 5
```

### Progress Monotonicity in SSE

SSE events may arrive out of order due to network conditions. Clients must enforce monotonic progress:

**Client-Side Enforcement:**
```typescript
let currentProgress = 0;

sseClient.on('step-progress', (event) => {
  const newProgress = event.data.progressPct;
  
  // Only accept if progress increases
  if (newProgress >= currentProgress) {
    currentProgress = newProgress;
    updateUI(currentProgress);
  } else {
    // Reject out-of-order event
    console.debug(`Rejected progress ${newProgress}%, current is ${currentProgress}%`);
  }
});
```

## Cancellation and Cleanup

### Cancellation Flow

1. User clicks cancel button
2. Frontend calls `POST /api/jobs/{jobId}/cancel`
3. Backend triggers cancellation token
4. Orchestrator receives cancellation and stops processing
5. Cleanup service removes temporary files and proxies
6. Job status updated to `Canceled` with `CanceledUtc` timestamp
7. SSE emits `job-cancelled` event

### Cleanup Operations

**Temporary Files:**
- Location: `{LocalAppData}/Aura/temp/{jobId}/`
- Cleaned on cancellation
- Cleaned on completion
- Orphaned files cleaned after 24 hours

**Proxy Media:**
- Location: `{LocalAppData}/Aura/proxy/{jobId}/`
- Cleaned on cancellation
- Cleaned on completion
- Orphaned files cleaned after 48 hours

**Cleanup Guarantees:**
1. Cleanup always attempted on cancellation
2. Cleanup failures logged as warnings (non-fatal)
3. Orphan sweeper runs periodically for missed cleanups
4. Cancel endpoint returns `cleanupScheduled: true`

### Preventing Downstream Work

Cancellation prevents downstream work via:

1. **CancellationToken**: Passed to all async operations
2. **Status Checks**: Orchestrator checks job status before each stage
3. **Token Propagation**: Cancellation token linked to all child operations
4. **Cleanup First**: Temporary artifacts cleaned before final status update

## Error Handling

### SSE Connection Errors

**Transient Errors** (retry with backoff):
- Network timeout
- Connection reset
- Server temporarily unavailable

**Permanent Errors** (stop reconnecting):
- Job not found (404)
- Authorization failed (401/403)
- Max reconnection attempts exceeded

**Error Response:**
```json
{
  "event": "error",
  "data": {
    "message": "Connection error details",
    "correlationId": "..."
  }
}
```

### Job Execution Errors

**Recoverable Errors:**
- Provider API rate limits (retry with backoff)
- Temporary resource unavailability
- Network timeouts

**Non-Recoverable Errors:**
- Invalid input data
- Missing dependencies
- Insufficient system resources
- Provider authentication failures

**Error Reporting:**
1. Error logged to job logs
2. `JobStepError` objects added to `job.Errors`
3. `job.FailureDetails` populated with diagnostic info
4. SSE emits `job-failed` event with error details
5. Job transitions to `Failed` status

## Testing

### Unit Tests

**Job Model Tests:**
- State transition validation
- Timestamp ordering
- Monotonic progress enforcement
- Terminal state identification
- Resumability flags

**SSE Client Tests:**
- Event ID generation
- Reconnection logic
- Backoff calculation
- Status tracking

### E2E Tests

**JobQueueStabilityTests (14 tests):**
- Monotonic progress through full lifecycle
- State transition invariants
- Timestamp ordering for successful jobs
- Timestamp ordering for canceled jobs
- Cleanup service integration
- Resumability tracking

**SseResilienceTests (26 tests):**
- Event ID monotonicity
- Progress monotonicity with out-of-order events
- Reconnection with Last-Event-ID
- Heartbeat mechanism
- Stage-to-phase mapping (21 combinations)
- Event type consistency
- Error recovery with exponential backoff

### Manual Testing Scenarios

**SSE Disconnection:**
1. Start a long-running job
2. Disconnect network
3. Reconnect network
4. Verify: SSE resumes with Last-Event-ID
5. Verify: Progress continues from last known state
6. Verify: No duplicate events processed

**Job Cancellation:**
1. Start a job
2. Cancel at 40% progress
3. Verify: Status changes to Canceled
4. Verify: Temporary files cleaned up
5. Verify: Proxy files cleaned up
6. Verify: CanceledUtc timestamp set
7. Verify: EndedUtc equals CanceledUtc

**Monotonic Progress:**
1. Start a job
2. Induce out-of-order SSE events (network delay)
3. Verify: Progress never decreases in UI
4. Verify: Out-of-order events logged and rejected
5. Verify: Final progress reaches 100%

## Performance Considerations

### SSE Connection Management

- Max 1 SSE connection per job
- Connections closed when job reaches terminal state
- Heartbeat interval optimized for connection liveness vs. bandwidth
- Event batching not implemented (real-time updates prioritized)

### Cleanup Performance

- Cleanup is asynchronous and non-blocking
- Failed cleanup logged but doesn't fail job
- Orphan sweeper runs daily, not on demand
- Large file cleanup may take seconds

### Progress Update Frequency

- Progress updated on meaningful milestones (not every 1%)
- SSE events sent on progress change (debounced to 500ms)
- Backend polls job state every 500ms
- UI throttles progress bar updates to 60 FPS

## Security Considerations

### SSE Authentication

- SSE connections inherit HTTP session authentication
- No additional auth required per event
- Connection URL includes job ID for authorization

### Correlation IDs

- All events include correlation ID for request tracing
- Correlation IDs logged for debugging
- Never include sensitive data in correlation IDs

### Cleanup Security

- Cleanup service validates job ownership
- Directory traversal prevention (no relative paths)
- Cleanup limited to known directories
- No arbitrary file deletion

## Version History

- **v1.0** (2024-01) - Initial specification
  - State machine with monotonic progress
  - SSE with Last-Event-ID support
  - Cancellation with cleanup
  - Resumability support
