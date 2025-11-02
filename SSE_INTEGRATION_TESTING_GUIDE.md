# SSE Integration Testing Guide

## Overview

This guide provides instructions for testing the Server-Sent Events (SSE) integration for real-time job progress updates in Aura Video Studio.

## What Was Changed

### Backend (Aura.Api)
1. **Enhanced SSE Endpoint** (`JobsController.GetJobEvents`)
   - Added keep-alive pings every 10 seconds
   - Improved event structure with phase information
   - Added progress message support
   - Better error and warning event handling
   - Helper methods for phase mapping

### Frontend (Aura.Web)
1. **New SSE Client** (`src/services/api/sseClient.ts`)
   - Auto-reconnect with exponential backoff
   - Resilient to network interruptions
   - Proper event handling for all job states

2. **Updated Jobs Store** (`src/state/jobs.ts`)
   - Replaced polling with SSE streaming
   - Real-time progress updates
   - Proper state management for job lifecycle

3. **Enhanced GenerationPanel** (`src/components/Generation/GenerationPanel.tsx`)
   - Shows current phase (plan, tts, visuals, compose, render)
   - Displays progress messages
   - Auto-starts SSE streaming on mount

## Manual Testing Checklist

### Prerequisites
- Backend API running on port 5005
- Frontend dev server running on port 5173
- FFmpeg installed and accessible
- Browser developer tools open (Network tab)

### Test 1: Quick Demo End-to-End

**Steps:**
1. Navigate to http://localhost:5173
2. Click "Quick Demo" button on Welcome page
3. Observe GenerationPanel opens on the right side

**Expected Results:**
- ✅ API POST to `/api/quick/demo` succeeds
- ✅ Response contains `jobId`
- ✅ SSE connection established to `/api/jobs/{jobId}/events`
- ✅ Network tab shows EventSource connection type "text/event-stream"
- ✅ Progress updates appear in real-time:
  - Script phase (0-15%)
  - TTS phase (15-35%)
  - Visuals phase (35-65%)
  - Compose phase (65-85%)
  - Render phase (85-100%)
- ✅ Phase name displayed in GenerationPanel
- ✅ Progress message updates visible
- ✅ Keep-alive comments received every 10 seconds (check Network tab)
- ✅ On completion:
  - Status changes to "Done"
  - Artifacts section appears
  - Video file shown with size
  - "Open folder" button works
  - Success toast notification appears

**Console Verification:**
```
[SseClient] Connecting to http://localhost:5005/api/jobs/{jobId}/events
[SseClient] Connected successfully
[SseClient] Received job-status: {...}
[SseClient] Received step-progress: {...}
[JobsStore] Job status update
[JobsStore] Step progress
[JobsStore] Job completed
```

### Test 2: Full Generate Video Flow

**Steps:**
1. Click "Create" in sidebar
2. Fill in wizard:
   - **Step 1 - Brief**: 
     - Topic: "Test video"
     - Audience: "General"
     - Goal: "Demonstrate"
     - Tone: "Informative"
   - **Step 2 - Plan**: Accept defaults
   - **Step 3 - Voice**: Accept defaults
3. Click "Generate Video"

**Expected Results:**
- ✅ Preflight check passes
- ✅ API POST to `/api/jobs` succeeds
- ✅ SSE connection established
- ✅ GenerationPanel shows progress
- ✅ All phases complete successfully
- ✅ Video file generated
- ✅ Artifacts downloadable

### Test 3: SSE Auto-Reconnect

**Steps:**
1. Start Quick Demo
2. During execution, simulate network interruption:
   - Open Network tab in DevTools
   - Set throttling to "Offline" for 5 seconds
   - Set back to "No throttling"

**Expected Results:**
- ✅ SSE connection drops
- ✅ Auto-reconnect attempts logged in console
- ✅ Connection re-establishes automatically
- ✅ Progress updates resume
- ✅ Job completes successfully
- ✅ Max 5 reconnect attempts before giving up

**Console Verification:**
```
[SseClient] Connection error
[SseClient] Reconnecting in 1000ms (attempt 1/5)
[SseClient] Attempting reconnect...
[SseClient] Connected successfully
```

### Test 4: Job Cancellation

**Steps:**
1. Start Quick Demo or Full Generate
2. During execution (around 20-50%), click Cancel button

**Expected Results:**
- ✅ API POST to `/api/jobs/{jobId}/cancel` succeeds
- ✅ SSE receives `job-cancelled` event
- ✅ Progress stops updating
- ✅ Status changes to "Canceled"
- ✅ Panel shows cancellation message
- ✅ SSE connection closes

### Test 5: Error Handling

**Steps:**
1. Stop FFmpeg or make it unavailable
2. Try to generate video

**Expected Results:**
- ✅ Job starts but fails during render phase
- ✅ SSE receives `job-failed` event
- ✅ Error message displayed in panel
- ✅ "View logs" button available
- ✅ Failure details accessible
- ✅ Suggested actions provided

### Test 6: Keep-Alive Verification

**Steps:**
1. Start a long-running job (if possible, or use a debug breakpoint)
2. Monitor Network tab for SSE connection

**Expected Results:**
- ✅ Keep-alive comments (`:keepalive:`) sent every 10 seconds
- ✅ Connection stays open throughout job execution
- ✅ No connection timeout
- ✅ Browser doesn't close idle connection

**Network Tab:**
```
event: job-status
data: {"status":"Running",...}

: keepalive: 2024-01-01T12:00:00Z

event: step-progress
data: {"step":"TTS",...}

: keepalive: 2024-01-01T12:00:10Z
```

### Test 7: Multiple Jobs

**Steps:**
1. Start Quick Demo job
2. Before it completes, navigate away
3. Start another job
4. Navigate back to first job

**Expected Results:**
- ✅ First SSE connection closes when navigating away
- ✅ Second SSE connection establishes for new job
- ✅ No memory leaks from dangling connections
- ✅ Each job tracked independently

## Debugging Tips

### SSE Not Connecting
1. Check browser console for errors
2. Verify API is running: `curl http://localhost:5005/api/healthz`
3. Check CORS headers in Network tab
4. Verify job was created: `curl http://localhost:5005/api/jobs/{jobId}`

### Progress Not Updating
1. Check SSE connection in Network tab (should show "text/event-stream")
2. Look for event messages in Network preview
3. Verify `startStreaming` is called (check console logs)
4. Check if JobRunner is actually executing the job

### Keep-Alive Not Working
1. Verify 10-second interval in backend code
2. Check Network tab for comment lines starting with `:`
3. Ensure `FlushAsync` is called after sending ping

### Build Issues
```bash
# Backend
cd Aura.Api
dotnet build

# Frontend
cd Aura.Web
npm run typecheck
npm run lint
npm run build
```

## Performance Verification

### Expected Metrics
- **SSE Connection**: <100ms to establish
- **Event Latency**: <50ms from backend emit to frontend receive
- **Memory Usage**: Stable during long jobs (no leaks)
- **Reconnect Time**: <2 seconds for first attempt
- **Keep-Alive Interval**: Exactly 10 seconds ±100ms

### Monitoring
```javascript
// Browser console
// Check memory usage
console.memory

// Monitor SSE events
performance.getEntriesByType('resource').filter(r => r.name.includes('/events'))
```

## Success Criteria

All tests must pass for the feature to be considered complete:
- ✅ Quick Demo produces playable MP4 in Demo Mode
- ✅ Full Generate Video runs end-to-end with visible progress
- ✅ SSE connection resilient to network interruptions
- ✅ Keep-alive pings prevent connection timeout
- ✅ Error messages surface in UI with helpful guidance
- ✅ Download links work for artifacts
- ✅ Job cancellation works cleanly
- ✅ No console errors during normal operation
- ✅ No memory leaks with multiple jobs

## Known Limitations

1. **Browser Support**: SSE requires modern browser (EventSource API)
2. **Concurrent Jobs**: Frontend tracks one active job at a time
3. **Reconnect Limit**: Max 5 attempts before giving up
4. **Large Jobs**: Very long jobs (>1 hour) may need connection refresh

## Next Steps

After manual testing passes:
1. Add automated E2E tests using Playwright
2. Add unit tests for SSE client
3. Add integration tests for JobRunner with progress events
4. Performance testing under load
5. Add monitoring/telemetry for production
