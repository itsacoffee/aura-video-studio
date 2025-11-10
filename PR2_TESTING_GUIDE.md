# PR #2 Testing Guide: Provider Integration and Video Pipeline

## Quick Test Scenarios

### Scenario 1: Basic Video Generation (Happy Path)

#### Backend Test
```bash
# 1. Start the backend
cd Aura.Api
dotnet run

# 2. Test video generation endpoint
curl -X POST http://localhost:5000/api/video/generate \
  -H "Content-Type: application/json" \
  -d '{
    "brief": "A short video about AI",
    "durationMinutes": 0.5,
    "voiceId": "default",
    "style": "informative"
  }'

# Expected Response (202 Accepted):
{
  "jobId": "abc-123-def",
  "status": "pending",
  "videoUrl": null,
  "createdAt": "2025-11-10T...",
  "correlationId": "xyz-789"
}

# 3. Check job status
curl http://localhost:5000/api/video/{jobId}/status

# Expected Response:
{
  "jobId": "abc-123-def",
  "status": "processing",
  "progressPercentage": 45,
  "currentStage": "Audio",
  "createdAt": "...",
  "completedAt": null,
  "videoUrl": null,
  "errorMessage": null,
  "processingSteps": ["Initialized", "Script Generated", "Audio Synthesized"],
  "correlationId": "xyz-789"
}

# 4. Stream progress (SSE)
curl -N http://localhost:5000/api/video/{jobId}/stream

# Expected Events:
event: progress
data: {"percentage":15,"stage":"Script","message":"Processing: Script","timestamp":"..."}

event: stage-complete
data: {"stage":"Script","nextStage":"Audio","timestamp":"..."}

event: done
data: {"jobId":"abc-123-def","videoUrl":"/api/video/abc-123-def/download","timestamp":"..."}

# 5. Download video
curl http://localhost:5000/api/video/{jobId}/download --output video.mp4
```

#### Frontend Test (Browser Console)
```typescript
import { videoGenerationService } from './services/videoGenerationService';

// 1. Generate video
const response = await videoGenerationService.generateVideo({
  brief: "A short video about AI",
  durationMinutes: 0.5,
  voiceId: "default",
  style: "informative"
});

console.log('Job ID:', response.jobId);

// 2. Stream progress
const cleanup = videoGenerationService.streamProgress(
  response.jobId,
  (update) => {
    console.log(`Progress: ${update.percentage}% - ${update.stage}`);
  },
  (error) => {
    console.error('Error:', error.message);
  },
  (state) => {
    console.log('Connection status:', state.status);
  }
);

// 3. Check status
const status = await videoGenerationService.getStatus(response.jobId);
console.log('Current status:', status);

// 4. Download video (when complete)
await videoGenerationService.downloadVideo(response.jobId);

// 5. Cleanup
cleanup();
```

---

### Scenario 2: Error Handling

#### Test Invalid API Key
```bash
# Temporarily set invalid OpenAI key in appsettings.json
curl -X POST http://localhost:5000/api/video/generate \
  -H "Content-Type: application/json" \
  -d '{
    "brief": "Test video",
    "durationMinutes": 0.5
  }'

# Expected: Job created but will fail during script generation
# Check status after a few seconds:
curl http://localhost:5000/api/video/{jobId}/status

# Expected Response:
{
  "jobId": "...",
  "status": "failed",
  "progressPercentage": 15,
  "currentStage": "Script",
  "errorMessage": "OpenAI API key is invalid or has been revoked. Please check your API key in Settings → Providers → OpenAI",
  ...
}
```

#### Test Network Timeout (Ollama Not Running)
```bash
# Make sure Ollama is NOT running
# Configure to use Ollama in appsettings.json

curl -X POST http://localhost:5000/api/video/generate \
  -H "Content-Type: application/json" \
  -d '{
    "brief": "Test video",
    "durationMinutes": 0.5
  }'

# Expected: Retry attempts, then failure with message:
# "Cannot connect to Ollama at http://127.0.0.1:11434. Please ensure Ollama is running: 'ollama serve'"
```

---

### Scenario 3: SSE Reconnection

#### Frontend Test
```typescript
// 1. Start video generation
const response = await videoGenerationService.generateVideo({
  brief: "Long video test",
  durationMinutes: 2
});

// 2. Start SSE streaming
const cleanup = videoGenerationService.streamProgress(
  response.jobId,
  (update) => {
    console.log(`Progress: ${update.percentage}%`);
  },
  null,
  (state) => {
    console.log('Connection:', state.status, 'Attempt:', state.reconnectAttempt);
  }
);

// 3. Simulate network interruption
// - Close browser tab briefly and reopen
// - Or use browser dev tools to throttle network

// Expected behavior:
// Console output:
// Connection: connected Attempt: 0
// Progress: 15%
// Connection: reconnecting Attempt: 1
// (wait 1 second)
// Connection: connected Attempt: 0
// Progress: 30%
```

---

### Scenario 4: Job Cancellation

#### Backend Test
```bash
# 1. Start video generation
curl -X POST http://localhost:5000/api/video/generate \
  -H "Content-Type: application/json" \
  -d '{
    "brief": "Long video to cancel",
    "durationMinutes": 5
  }'

# Save jobId from response

# 2. Check status (should be running)
curl http://localhost:5000/api/video/{jobId}/status

# 3. Cancel job
curl -X POST http://localhost:5000/api/video/{jobId}/cancel

# Expected Response (200 OK):
{
  "message": "Job cancellation requested",
  "jobId": "...",
  "correlationId": "..."
}

# 4. Check status again
curl http://localhost:5000/api/video/{jobId}/status

# Expected Response:
{
  "jobId": "...",
  "status": "failed",  // or "cancelled" depending on timing
  ...
}
```

#### Frontend Test
```typescript
// 1. Start generation
const response = await videoGenerationService.generateVideo({
  brief: "Long video to cancel",
  durationMinutes: 5
});

// 2. Wait a bit, then cancel
await new Promise(resolve => setTimeout(resolve, 5000));
await videoGenerationService.cancelGeneration(response.jobId);

// 3. Check status
const status = await videoGenerationService.getStatus(response.jobId);
console.log('Status after cancel:', status.status);
// Expected: 'failed' or 'cancelled'
```

---

### Scenario 5: Polling Fallback

#### Frontend Test (When SSE Fails)
```typescript
// 1. Generate video
const response = await videoGenerationService.generateVideo({
  brief: "Test video",
  durationMinutes: 1
});

// 2. Use polling instead of SSE
const cleanup = videoGenerationService.pollStatus(
  response.jobId,
  (update) => {
    console.log(`Poll: ${update.percentage}% - ${update.stage}`);
  },
  (error) => {
    console.error('Polling error:', error);
  },
  2000  // Poll every 2 seconds
);

// Expected: Progress updates every 2 seconds until completion

// 3. Cleanup when done
cleanup();
```

---

## Integration Tests

### Test 1: Provider Fallback
```csharp
// Setup: Disable OpenAI (invalid key), enable Ollama with valid model

// Test: Generate video
// Expected: Should fallback to Ollama automatically
// Verify: Check logs for provider selection
```

### Test 2: Validation Errors
```bash
# Missing required fields
curl -X POST http://localhost:5000/api/video/generate \
  -H "Content-Type: application/json" \
  -d '{
    "durationMinutes": 0.5
  }'

# Expected: 400 Bad Request with validation errors
{
  "type": "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E400",
  "title": "Validation Failed",
  "status": 400,
  "detail": "Brief is required",
  "errors": {
    "Brief": ["The Brief field is required."]
  }
}
```

### Test 3: Rate Limiting
```typescript
// Generate 10 videos simultaneously
const promises = Array(10).fill(null).map(() =>
  videoGenerationService.generateVideo({
    brief: "Rate limit test",
    durationMinutes: 0.5
  })
);

// Some requests may be rate limited
const results = await Promise.allSettled(promises);

// Check which succeeded vs failed
results.forEach((result, i) => {
  if (result.status === 'fulfilled') {
    console.log(`Request ${i}: Success`);
  } else {
    console.log(`Request ${i}: Failed -`, result.reason.message);
  }
});
```

---

## Manual Verification Checklist

### Backend Health Checks
- [ ] API starts without errors
- [ ] Health endpoint returns Healthy
- [ ] FFmpeg is detected
- [ ] At least one LLM provider is available
- [ ] At least one TTS provider is available
- [ ] Database migrations applied
- [ ] Logs are being written

### API Endpoints
- [ ] POST /api/video/generate returns 202
- [ ] GET /api/video/{id}/status returns correct status
- [ ] GET /api/video/{id}/stream establishes SSE connection
- [ ] GET /api/video/{id}/download serves video file
- [ ] GET /api/video/{id}/metadata returns metadata
- [ ] POST /api/video/{id}/cancel cancels job

### Frontend Service
- [ ] TypeScript compiles without errors
- [ ] Service initializes without errors
- [ ] API calls use correct endpoints
- [ ] SSE client connects successfully
- [ ] Progress callbacks are invoked
- [ ] Error callbacks are invoked on failure
- [ ] Cleanup methods release resources
- [ ] Download triggers browser download

### Error Scenarios
- [ ] Invalid API key shows user-friendly message
- [ ] Network timeout retries automatically
- [ ] Provider unavailable shows helpful error
- [ ] Validation errors show field-specific messages
- [ ] SSE reconnection works after disconnect
- [ ] Cancellation stops job execution
- [ ] Resource cleanup occurs on failure

### Performance
- [ ] Video generation completes in reasonable time
- [ ] Progress updates arrive in real-time (< 1s latency)
- [ ] SSE heartbeat prevents timeout
- [ ] No memory leaks during long-running jobs
- [ ] Cleanup removes temporary files

---

## Debugging Tips

### Check Logs
```bash
# Backend logs
tail -f logs/aura-api-*.log

# Look for:
- [Information] Job created: {JobId}
- [Information] Script generated successfully
- [Information] Audio generated successfully
- [Information] Video rendered to: {Path}
- [Error] messages with correlation IDs
```

### Check Job Artifacts
```bash
# Artifacts are saved to configured output directory
ls -la /path/to/output/directory/

# Should contain:
- video-{jobId}.mp4
- narration-{jobId}.wav
- script-{jobId}.txt
```

### Check Browser Console
```javascript
// Enable verbose logging
localStorage.setItem('logLevel', 'debug');

// Check for:
- API requests with correlation IDs
- SSE connection status
- Progress updates
- Error messages
```

### Check Network Tab
- SSE connection should show "Pending" status (EventStream)
- API requests should have X-Correlation-ID headers
- Responses should be compressed (Content-Encoding: gzip/br)

---

## Common Issues & Solutions

### Issue: "Cannot connect to OpenAI API"
**Solution:** 
1. Check API key in appsettings.json
2. Verify internet connectivity
3. Check firewall settings
4. Verify OpenAI service status

### Issue: "Ollama not running"
**Solution:**
```bash
# Start Ollama
ollama serve

# Verify it's running
curl http://localhost:11434/api/version
```

### Issue: "SSE connection keeps disconnecting"
**Solution:**
1. Check reverse proxy settings (disable buffering)
2. Verify browser supports SSE
3. Check network stability
4. Fall back to polling

### Issue: "Video file not found"
**Solution:**
1. Check output directory permissions
2. Verify FFmpeg is installed
3. Check disk space
4. Review job error message

### Issue: "Progress stuck at X%"
**Solution:**
1. Check backend logs for errors
2. Verify providers are responding
3. Check for hung processes
4. Cancel and retry job

---

## Performance Benchmarks

### Expected Timings (on typical hardware):
- **Script Generation:** 5-15 seconds (OpenAI), 10-30 seconds (Ollama)
- **Audio Synthesis:** 5-10 seconds
- **Video Rendering:** 10-30 seconds
- **Total (30s video):** 20-60 seconds end-to-end

### Resource Usage:
- **Memory:** 500MB - 2GB (peak during rendering)
- **CPU:** 50-100% during rendering (FFmpeg)
- **Disk:** ~50-200MB per video (temporary files)
- **Network:** Minimal (unless using cloud providers)

---

## Success Criteria

✅ **All tests pass**
✅ **No unhandled exceptions in logs**
✅ **Generated videos are playable**
✅ **Progress reporting is accurate**
✅ **Errors show helpful messages**
✅ **Cancellation works reliably**
✅ **Resources are cleaned up**
✅ **SSE reconnection is seamless**

---

**Last Updated:** 2025-11-10  
**Tested By:** Implementation Team
