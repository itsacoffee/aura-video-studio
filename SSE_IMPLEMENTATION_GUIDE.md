# SSE Event Stream Implementation for Video Generation Progress

## Overview

This document describes the implementation of Server-Sent Events (SSE) for real-time video generation progress tracking in Aura Video Studio. The implementation provides live updates from the backend to the frontend without polling, resulting in immediate feedback and reduced server load.

## Architecture

### Backend (SSE Endpoint)

**Endpoint**: `GET /api/jobs/{jobId}/events`

**Location**: `Aura.Api/Controllers/JobsController.cs`

**Headers**:
```
Content-Type: text/event-stream
Cache-Control: no-cache
Connection: keep-alive
X-Accel-Buffering: no
```

**Event Types**:
1. `job-status` - Overall job status and progress percentage
2. `step-progress` - Detailed progress within current step
3. `step-status` - Step start/completion notifications
4. `job-completed` - Success with video download information
5. `job-failed` - Error details and failure information
6. `job-cancelled` - Cancellation confirmation
7. `warning` - Non-critical warnings
8. `error` - Critical errors

**Features**:
- Heartbeat/keepalive comments every 10 seconds
- Event ID for reconnection support
- Polling job status every 500ms for updates
- Automatic cleanup on job completion/failure/cancellation

### Frontend (SSE Client)

**SSE Client Classes**:
1. `SSEClient` - Legacy client with exponential backoff (in `sseClient.ts`)
2. `SseClient` - Modern event-driven client (in `sseClient.ts`)

**Hook**: `useSSEConnection` (in `hooks/useSSEConnection.ts`)

**Features**:
- Automatic reconnection with exponential backoff
- Last-Event-ID support for reconnection
- Multiple event type support
- Proper cleanup on unmount
- Connection state tracking

## Components

### 1. VideoGenerationProgress

**Location**: `Aura.Web/src/components/VideoGenerationProgress.tsx`

**Purpose**: Complete SSE-based progress component for video generation monitoring

**Features**:
- Real-time progress updates via SSE (no polling)
- Stage-based progress indicators (5 stages)
- Visual progress bars for overall and per-stage progress
- Elapsed time and estimated time remaining display
- Cancel functionality with confirmation dialog
- Success state with download button
- Error state with detailed error messages
- Live connection indicator

**Progress Stages**:
1. **Script Generation** (0-15%): Generate video script from brief
2. **Audio Synthesis** (15-35%): Convert script to speech
3. **Visual Generation** (35-65%): Create video scenes
4. **Timeline Composition** (65-85%): Assemble audio and visuals
5. **Video Rendering** (85-100%): Final video encoding

**Props**:
```typescript
interface VideoGenerationProgressProps {
  jobId: string;
  onComplete?: (result: { videoUrl: string; videoPath: string }) => void;
  onError?: (error: Error) => void;
  onCancel?: () => void;
}
```

**Usage**:
```tsx
import { VideoGenerationProgress } from '@/components/VideoGenerationProgress';

function MyComponent() {
  const [jobId, setJobId] = useState('');
  
  return (
    <VideoGenerationProgress
      jobId={jobId}
      onComplete={(result) => {
        console.log('Video ready:', result.videoUrl);
      }}
      onError={(error) => {
        console.error('Generation failed:', error.message);
      }}
      onCancel={() => {
        console.log('Generation cancelled');
      }}
    />
  );
}
```

### 2. JobProgressDrawer (Enhanced)

**Location**: `Aura.Web/src/components/JobProgressDrawer.tsx`

**Changes**: Converted from REST API polling to SSE-based updates

**Features**:
- Real-time updates via `useSSEConnection` hook
- Drawer-style UI for compact progress display
- Log viewer showing recent activity
- Cancel job functionality
- Elapsed time and ETA display

**Before** (Polling):
- Polled `/api/jobs/{jobId}/progress` every 1 second
- Separate request for logs
- Higher server load
- Potential delays in updates

**After** (SSE):
- Single SSE connection to `/api/jobs/{jobId}/events`
- Real-time event stream
- Minimal server load
- Instant updates

### 3. VideoGenerationProgressExample

**Location**: `Aura.Web/src/examples/VideoGenerationProgressExample.tsx`

**Purpose**: Example component demonstrating SSE functionality

**Features**:
- Job ID input for monitoring existing jobs
- Start/Reset controls
- Success and error message display
- Comprehensive documentation:
  - How-to instructions
  - SSE event descriptions
  - API call examples
  - Feature list
  - Code samples

## Event Flow

### 1. Job Creation
```typescript
// Frontend creates job
const response = await fetch('/api/jobs', {
  method: 'POST',
  body: JSON.stringify(jobRequest)
});

const { jobId } = await response.json();
```

### 2. SSE Connection
```typescript
// Frontend establishes SSE connection
const eventSource = new EventSource(`/api/jobs/${jobId}/events`);
```

### 3. Event Stream
```
Server → Client: event: job-status
                 data: {"status":"Running","stage":"Script","percent":5}
                 id: 1699200000000-1

Server → Client: event: step-progress
                 data: {"step":"Script","progressPct":10,"message":"Generating..."}
                 id: 1699200000000-2

Server → Client: : keepalive (heartbeat)

Server → Client: event: step-progress
                 data: {"step":"Script","progressPct":15,"message":"Complete"}
                 id: 1699200000000-3

Server → Client: event: job-completed
                 data: {"jobId":"abc123","output":{"videoPath":"/path/to/video.mp4"}}
                 id: 1699200000000-4
```

### 4. Component Updates
```typescript
// Frontend updates UI in real-time
useSSEConnection({
  onMessage: (message) => {
    switch (message.type) {
      case 'step-progress':
        setProgress(message.data.progressPct);
        break;
      case 'job-completed':
        setCompleted(true);
        break;
    }
  }
});
```

## Reconnection Support

### Last-Event-ID Header

When a client reconnects after a network interruption, it sends the last event ID it received:

```
GET /api/jobs/{jobId}/events
Last-Event-ID: 1699200000000-5
```

The server acknowledges the reconnection and resumes from the current state:
- Logs reconnection with event ID
- Sends current job status immediately
- Continues streaming new events

### Automatic Reconnection

The `useSSEConnection` hook provides automatic reconnection with exponential backoff:

```typescript
// Connection lost
→ Attempt 1 after 3 seconds
→ Attempt 2 after 6 seconds  
→ Attempt 3 after 12 seconds
→ Attempt 4 after 24 seconds
→ Attempt 5 after 48 seconds (max)
```

Default settings:
- Max reconnection attempts: 5
- Base delay: 3000ms
- Backoff multiplier: 2x

## Testing

### Manual Testing Checklist

1. **Start Generation**
   - [ ] Create video generation job via API
   - [ ] Copy job ID
   - [ ] Open VideoGenerationProgressExample
   - [ ] Enter job ID and click "Start Monitoring"

2. **Progress Updates**
   - [ ] Verify progress bar moves smoothly from 0-100%
   - [ ] Confirm stage labels update correctly
   - [ ] Check elapsed time updates every second
   - [ ] Verify estimated time remaining appears

3. **Completion**
   - [ ] Wait for job to complete
   - [ ] Verify success message appears
   - [ ] Check download button is displayed
   - [ ] Click download and verify video downloads

4. **Cancellation**
   - [ ] Start new job
   - [ ] Click "Cancel Generation" button
   - [ ] Confirm cancellation in dialog
   - [ ] Verify job cancels and SSE connection closes

5. **Error Handling**
   - [ ] Start job that will fail (e.g., invalid config)
   - [ ] Verify error message appears
   - [ ] Check error details are displayed

6. **Reconnection**
   - [ ] Start job
   - [ ] Disable network (simulate disconnection)
   - [ ] Re-enable network
   - [ ] Verify automatic reconnection
   - [ ] Check progress continues from last state

### API Testing with cURL

**Start Job**:
```bash
curl -X POST http://localhost:5005/api/jobs \
  -H "Content-Type: application/json" \
  -d '{
    "brief": {
      "topic": "Test Video",
      "audience": "Developers",
      "goal": "Test SSE",
      "tone": "Technical",
      "language": "en",
      "aspect": "Widescreen16x9"
    },
    "planSpec": {
      "targetDuration": "00:01:00",
      "pacing": "Conversational",
      "density": "Balanced",
      "style": "Modern"
    },
    "voiceSpec": {
      "voiceName": "David",
      "rate": 1.0,
      "pitch": 1.0,
      "pause": "Natural"
    },
    "renderSpec": {
      "res": "1080p",
      "container": "mp4",
      "videoBitrateK": 5000,
      "audioBitrateK": 192,
      "fps": 30,
      "codec": "h264",
      "qualityLevel": "High"
    }
  }'
```

**Monitor SSE Stream**:
```bash
curl -N http://localhost:5005/api/jobs/{jobId}/events
```

**Cancel Job**:
```bash
curl -X POST http://localhost:5005/api/jobs/{jobId}/cancel
```

## Troubleshooting

### SSE Connection Not Establishing

**Symptom**: Component shows "Connecting" but never connects

**Possible Causes**:
1. Backend not running on expected port
2. CORS configuration blocking SSE
3. Job ID invalid or not found

**Solutions**:
1. Verify backend is running: `curl http://localhost:5005/api/health`
2. Check browser console for CORS errors
3. Verify job ID exists: `curl http://localhost:5005/api/jobs/{jobId}`

### Progress Not Updating

**Symptom**: SSE connects but progress bar stays at 0%

**Possible Causes**:
1. Job is queued but not running
2. Backend not sending progress events
3. Frontend not parsing events correctly

**Solutions**:
1. Check job status: `curl http://localhost:5005/api/jobs/{jobId}`
2. Monitor SSE stream: `curl -N http://localhost:5005/api/jobs/{jobId}/events`
3. Check browser console for errors

### Reconnection Failing

**Symptom**: After network interruption, connection doesn't restore

**Possible Causes**:
1. Max reconnection attempts exceeded
2. Job completed during disconnection
3. Backend restarted (job lost)

**Solutions**:
1. Refresh page to reset reconnection counter
2. Check job status to see if it completed
3. Restart job if backend was restarted

## Performance Considerations

### Server Load

**Polling** (Before):
- 1 request/second per client
- 3600 requests/hour per client
- 86,400 requests/day per client

**SSE** (After):
- 1 connection per client (long-lived)
- Heartbeat every 10 seconds
- ~360 heartbeats/hour per client
- Significantly reduced load

### Network Efficiency

**Polling**:
- Full HTTP request/response overhead each time
- Repeated headers, handshake, parsing
- High latency (minimum 1 second delay)

**SSE**:
- Single connection establishment
- Minimal overhead per event
- Near-instant updates (< 100ms)

### Browser Compatibility

SSE is supported in all modern browsers:
- Chrome 6+
- Firefox 6+
- Safari 5+
- Edge 79+
- Opera 11+

**Not supported**: Internet Explorer (use polyfill if needed)

## Security Considerations

### Authentication

Current implementation uses correlation ID tracking. For production:

1. **Add authentication**:
   - Require API key or JWT token
   - Validate on SSE connection
   - Use secure cookies for browser clients

2. **Job ownership**:
   - Associate jobs with user accounts
   - Verify user can access job before streaming
   - Return 403 Forbidden for unauthorized access

3. **Rate limiting**:
   - Limit concurrent SSE connections per user
   - Prevent abuse of long-lived connections
   - Monitor connection duration

### Data Protection

- Never send sensitive data in SSE events
- Sanitize error messages (no stack traces)
- Use HTTPS in production
- Implement proper CORS policy

## Future Enhancements

### Planned Improvements

1. **Batched Updates**
   - Group multiple progress updates
   - Reduce event frequency for bandwidth savings
   - Configurable batch window (e.g., 100ms)

2. **Compression**
   - Gzip event data for large payloads
   - Reduce bandwidth usage
   - Especially beneficial for logs/errors

3. **Multi-job Monitoring**
   - Single SSE connection for multiple jobs
   - Multiplexed event stream
   - Reduced connection overhead

4. **Enhanced Reconnection**
   - Event replay from Last-Event-ID
   - Buffered events during disconnection
   - Seamless continuation on reconnect

5. **WebSocket Fallback**
   - Automatic fallback for older browsers
   - Bi-directional communication support
   - Enhanced real-time capabilities

## References

- [MDN: Server-Sent Events](https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events)
- [EventSource API](https://developer.mozilla.org/en-US/docs/Web/API/EventSource)
- [SSE Specification](https://html.spec.whatwg.org/multipage/server-sent-events.html)

## Support

For issues or questions:
1. Check browser console for errors
2. Review JobsController.cs logs
3. Test SSE endpoint with cURL
4. Open GitHub issue with details
