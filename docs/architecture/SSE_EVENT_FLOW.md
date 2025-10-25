# SSE Event Flow Diagram

## Architecture Overview

```
┌─────────────────┐
│   Frontend      │
│   (React)       │
└────────┬────────┘
         │ EventSource
         │ GET /api/jobs/{jobId}/events
         ▼
┌─────────────────────────────────────────────────┐
│          JobsController.GetJobEvents()          │
│  - Sets SSE headers (text/event-stream)        │
│  - Polls job state every 1 second              │
│  - Sends SSE events on changes                 │
└────────┬────────────────────────────────────────┘
         │ Polls
         ▼
┌─────────────────────────────────────────────────┐
│            JobRunner.GetJob()                   │
│  - Returns current job state                    │
│  - Job includes: Status, Stage, Percent, etc.  │
└────────┬────────────────────────────────────────┘
         │
         │ Job State
         │
┌────────▼────────────────────────────────────────┐
│         JobRunner (Background Task)             │
│  - Executes job through stages                  │
│  - Publishes JobProgress events                 │
│  - Updates job state in ArtifactManager         │
└────────┬────────────────────────────────────────┘
         │
         │ Publishes JobProgressEventArgs
         │
┌────────▼────────────────────────────────────────┐
│     JobProgress Event Subscribers               │
│  - Event contains:                              │
│    * JobId, Progress (0-100)                    │
│    * Status, Stage, Message                     │
│    * Timestamp, CorrelationId                   │
└─────────────────────────────────────────────────┘
```

## Event Flow Sequence

### 1. Job Creation
```
Client → POST /api/jobs
       ↓
   JobRunner.CreateAndStartJobAsync()
       ↓
   Task.Run(ExecuteJobAsync)
       ↓
   Returns Job { Id, Status: Queued, ... }
```

### 2. SSE Subscription
```
Client → GET /api/jobs/{jobId}/events
       ↓
   JobsController.GetJobEvents()
       ↓
   Response.Headers["Content-Type"] = "text/event-stream"
       ↓
   Start polling loop (every 1 second)
```

### 3. Job Execution & Progress Updates
```
ExecuteJobAsync()
  ├─ UpdateJob(status: Running, stage: "Initialization")
  │   └─ Raises JobProgress event
  │       └─ JobProgressEventArgs published
  │
  ├─ VideoOrchestrator.GenerateVideoAsync()
  │   ├─ Reports: "Generating script"
  │   │   └─ ParseProgressMessage() → stage: "Script"
  │   │       └─ UpdateJob() → JobProgress event
  │   │
  │   ├─ Reports: "Generating narration"
  │   │   └─ ParseProgressMessage() → stage: "Voice"
  │   │       └─ UpdateJob() → JobProgress event
  │   │
  │   ├─ Reports: "Generating visuals"
  │   │   └─ ParseProgressMessage() → stage: "Visuals"
  │   │       └─ UpdateJob() → JobProgress event
  │   │
  │   └─ Reports: "Rendering video"
  │       └─ ParseProgressMessage() → stage: "Rendering"
  │           ├─ UpdateJob() → JobProgress event
  │           └─ FfmpegVideoComposer.RenderAsync()
  │               └─ Writes logs to Logs/ffmpeg/{jobId}.log
  │
  └─ UpdateJob(status: Done, percent: 100)
      └─ Raises final JobProgress event
```

### 4. SSE Polling & Event Emission
```
JobsController.GetJobEvents() - Polling Loop
  │
  ├─ Every 1 second:
  │   ├─ job = JobRunner.GetJob(jobId)
  │   ├─ Compare with previous state
  │   │
  │   └─ If changed:
  │       ├─ Status changed?
  │       │   └─ SendSseEvent("job-status", { status, correlationId })
  │       │
  │       ├─ Stage changed?
  │       │   └─ SendSseEvent("step-status", { step, status, correlationId })
  │       │
  │       └─ Percent changed?
  │           └─ SendSseEvent("step-progress", { step, progressPct, correlationId })
  │
  └─ Job complete/failed:
      ├─ Status: Done
      │   └─ SendSseEvent("job-completed", { status, output, correlationId })
      │
      └─ Status: Failed
          └─ SendSseEvent("job-failed", { status, errors, correlationId })
```

### 5. Frontend Event Handling
```
subscribeToJobEvents(jobId, (event) => {
  switch (event.type) {
    case 'job-status':
      // Update overall job status badge
      break;
      
    case 'step-status':
      // Update step status in progress list
      break;
      
    case 'step-progress':
      // Update progress bar for current step
      break;
      
    case 'job-completed':
      // Show success state with download button
      break;
      
    case 'job-failed':
      // Show error modal with remediation
      break;
  }
})
```

## SSE Message Format

### Event: job-status
```
event: job-status
data: {"status":"Running","correlationId":"abc123"}

```

### Event: step-status
```
event: step-status
data: {"step":"Rendering","status":"Running","correlationId":"abc123"}

```

### Event: step-progress
```
event: step-progress
data: {"step":"Rendering","progressPct":50,"correlationId":"abc123"}

```

### Event: job-completed
```
event: job-completed
data: {"status":"Succeeded","output":{"videoPath":"/path/to/video.mp4","sizeBytes":12345678},"correlationId":"abc123"}

```

### Event: job-failed
```
event: job-failed
data: {"status":"Failed","errors":[{"code":"E304-FFMPEG_RUNTIME","message":"FFmpeg render failed","remediation":"Check FFmpeg installation"}],"correlationId":"abc123"}

```

## Stage Transitions

The `ParseProgressMessage()` method in JobRunner detects stage transitions from orchestrator messages:

| Message Contains | Stage | Progress % |
|------------------|-------|------------|
| "Generating script" | Script | 10 |
| "Generating narration" or "voice" | Voice | 30 |
| "visual" or "image" | Visuals | 50 |
| "render" or "composing video" | Rendering | 70 |
| "postprocess" | Postprocessing | 90 |

## FFmpeg Log Files

Location: `~/.local/share/Aura/Logs/ffmpeg/{jobId}.log`

Example log content:
```
FFmpeg Render Log - Job ID: abc123def456
Correlation ID: xyz789
Started: 2025-10-20 12:34:56 UTC
Resolution: 1920x1080
FFmpeg Path: /usr/bin/ffmpeg
Command: -i input.mp4 -c:v libx264 -preset medium output.mp4
--------------------------------------------------------------------------------
[stderr] ffmpeg version 4.4.2
[stderr] Input #0, mp4, from 'input.mp4':
[stderr]   Duration: 00:00:30.00, start: 0.000000, bitrate: 5000 kb/s
[stderr] Stream #0:0: Video: h264, yuv420p, 1920x1080, 25 fps
[stderr] frame=  100 fps= 25 q=28.0 size=    1024kB time=00:00:04.00 bitrate=2048.0kbits/s
[stderr] frame=  200 fps= 25 q=28.0 size=    2048kB time=00:00:08.00 bitrate=2048.0kbits/s
[stderr] video:10240kB audio:1536kB subtitle:0kB other streams:0kB global headers:0kB muxing overhead: 0.5%
--------------------------------------------------------------------------------
Completed: 2025-10-20 12:35:30 UTC
Exit Code: 0
```

## Error Handling Flow

```
Exception in ExecuteJobAsync()
  │
  ├─ Catch OperationCanceledException
  │   └─ UpdateJob(status: Failed, errorMessage: "Job was cancelled")
  │       └─ SSE sends job-failed event
  │
  └─ Catch Exception
      ├─ CreateFailureDetails(job, ex)
      │   ├─ Check if FFmpeg error
      │   │   ├─ Yes: Read TryReadFfmpegLog(jobId)
      │   │   │     └─ JobFailure with StderrSnippet, LogPath
      │   │   └─ No: Generic JobFailure with SuggestedActions
      │   │
      │   └─ Return JobFailure object
      │
      └─ UpdateJob(status: Failed, failureDetails: details)
          └─ SSE sends job-failed event with errors array
```

## Correlation ID Flow

Correlation IDs flow through the entire system for traceability:

```
HTTP Request
  └─ HttpContext.TraceIdentifier
      └─ JobRunner.CreateAndStartJobAsync(correlationId)
          └─ Job.CorrelationId
              ├─ JobProgressEventArgs.CorrelationId
              │   └─ SSE event data.correlationId
              │
              ├─ FFmpeg log: "Correlation ID: xyz"
              │
              └─ ErrorModel.CorrelationId
                  └─ Error response JSON
```

## Key Implementation Details

### JobRunner Event Publishing
- Events published on every `UpdateJob()` call
- Events contain full job state snapshot
- Correlation ID always included
- Timestamp automatically added

### SSE Polling Strategy
- Poll every 1 second (configurable)
- Only send events on state changes
- Connection kept alive with periodic checks
- Automatic cleanup on job completion

### Frontend Reconnection (Recommended)
```typescript
let retryCount = 0;
const maxRetries = 5;
const baseDelay = 1000;

function connect() {
  const unsubscribe = subscribeToJobEvents(
    jobId,
    handleEvent,
    (error) => {
      if (retryCount < maxRetries) {
        const delay = baseDelay * Math.pow(2, retryCount);
        setTimeout(() => {
          retryCount++;
          connect();
        }, delay);
      }
    }
  );
}
```

## Performance Considerations

1. **Polling Interval**: 1 second balances responsiveness vs. load
2. **Event Filtering**: Only send events when state actually changes
3. **Connection Limits**: Consider max concurrent SSE connections
4. **Log File Size**: FFmpeg logs can grow large for long renders
5. **Memory**: Job state kept in memory during execution

## Security Considerations

1. **Authentication**: SSE endpoint should require authentication
2. **Authorization**: Verify user owns the job before streaming events
3. **Rate Limiting**: Limit number of SSE connections per user
4. **Correlation IDs**: Don't expose sensitive information in IDs
5. **Error Messages**: Sanitize error messages before sending to client
