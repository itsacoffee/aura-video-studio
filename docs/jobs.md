# Jobs API Documentation

The Jobs API provides endpoints for creating, monitoring, and managing video generation jobs with real-time progress updates via Server-Sent Events (SSE).

## Endpoints

### POST /api/jobs

Creates a new video generation job.

**Request Body:**

```json
{
  "preset": "default" | "sample-hello-youtube",
  "inputs": {},
  "options": {
    "allowSkipUnavailable": true,
    "quality": "fast" | "balanced" | "high"
  }
}
```

Or legacy format:

```json
{
  "brief": {
    "topic": "Your video topic",
    "audience": "Target audience",
    "goal": "Video goal",
    "tone": "Tone of voice",
    "language": "en-US",
    "aspect": "Widescreen16x9"
  },
  "planSpec": {
    "targetDuration": "00:01:00",
    "pacing": "Conversational",
    "density": "Balanced",
    "style": "Modern"
  },
  "voiceSpec": {
    "voiceName": "en-US-JennyNeural",
    "rate": 1.0,
    "pitch": 1.0,
    "pause": 1.0
  },
  "renderSpec": {
    "res": "1080p",
    "container": "mp4",
    "videoBitrateK": 8000,
    "audioBitrateK": 192,
    "fps": 30,
    "codec": "H264",
    "qualityLevel": 75,
    "enableSceneCut": true
  }
}
```

**Response (202 Accepted):**

```json
{
  "jobId": "J-abc123...",
  "correlationId": "def456...",
  "status": "Queued",
  "stage": "Script"
}
```

**cURL Example:**

```bash
curl -X POST http://localhost:5005/api/jobs \
  -H "Content-Type: application/json" \
  -H "X-Correlation-ID: my-correlation-id" \
  -d '{
    "preset": "sample-hello-youtube",
    "options": {
      "allowSkipUnavailable": true,
      "quality": "balanced"
    }
  }'
```

---

### GET /api/jobs/{jobId}

Retrieves the current status and details of a job.

**Response:**

```json
{
  "jobId": "J-abc123...",
  "status": "Running",
  "createdUtc": "2025-10-18T20:50:00Z",
  "startedUtc": "2025-10-18T20:50:02Z",
  "endedUtc": null,
  "correlationId": "def456...",
  "steps": [
    {
      "name": "preflight",
      "status": "Succeeded",
      "progressPct": 100,
      "durationMs": 100,
      "errors": [],
      "startedAt": "2025-10-18T20:50:02Z",
      "completedAt": "2025-10-18T20:50:03Z"
    },
    {
      "name": "narration",
      "status": "Running",
      "progressPct": 42,
      "durationMs": 0,
      "errors": [],
      "startedAt": "2025-10-18T20:50:03Z"
    },
    {
      "name": "broll",
      "status": "Pending",
      "progressPct": 0,
      "durationMs": 0,
      "errors": []
    },
    {
      "name": "subtitles",
      "status": "Pending",
      "progressPct": 0,
      "durationMs": 0,
      "errors": []
    },
    {
      "name": "mux",
      "status": "Pending",
      "progressPct": 0,
      "durationMs": 0,
      "errors": []
    }
  ],
  "output": null,
  "warnings": [],
  "errors": []
}
```

**cURL Example:**

```bash
curl http://localhost:5005/api/jobs/J-abc123...
```

---

### GET /api/jobs/{jobId}/events

Streams real-time job progress updates using Server-Sent Events (SSE).

**Event Types:**

1. **step-progress** - Progress update for a step
   ```json
   {
     "step": "narration",
     "progressPct": 47,
     "correlationId": "def456..."
   }
   ```

2. **step-status** - Status change for a step
   ```json
   {
     "step": "mux",
     "status": "Succeeded",
     "correlationId": "def456..."
   }
   ```

3. **step-error** - Error encountered in a step
   ```json
   {
     "step": "narration",
     "code": "MissingApiKey:STABLE_KEY",
     "message": "Required API key 'STABLE_KEY' is not configured.",
     "remediation": "Add STABLE_KEY in Settings → Providers",
     "correlationId": "def456..."
   }
   ```

4. **job-status** - Overall job status change
   ```json
   {
     "status": "Running",
     "correlationId": "def456..."
   }
   ```

5. **job-completed** - Job completed successfully
   ```json
   {
     "status": "Succeeded",
     "output": {
       "videoPath": "/path/to/output.mp4",
       "sizeBytes": 12345678
     },
     "correlationId": "def456..."
   }
   ```

6. **job-failed** - Job failed
   ```json
   {
     "status": "Failed",
     "errors": [
       {
         "code": "FFmpegNotFound",
         "message": "FFmpeg executable not found.",
         "remediation": "Install FFmpeg from Settings → Dependencies"
       }
     ],
     "correlationId": "def456..."
   }
   ```

**cURL Example:**

```bash
curl -N http://localhost:5005/api/jobs/J-abc123.../events
```

**JavaScript Example:**

```javascript
const eventSource = new EventSource('/api/jobs/J-abc123.../events');

eventSource.addEventListener('step-progress', (e) => {
  const data = JSON.parse(e.data);
  console.log(`${data.step}: ${data.progressPct}%`);
});

eventSource.addEventListener('job-completed', (e) => {
  const data = JSON.parse(e.data);
  console.log('Job completed!', data.output);
  eventSource.close();
});

eventSource.addEventListener('job-failed', (e) => {
  const data = JSON.parse(e.data);
  console.error('Job failed:', data.errors);
  eventSource.close();
});
```

---

### POST /api/jobs/{jobId}/cancel

Cancels a running job and cleans up temporary files.

**Response (202 Accepted):**

```json
{
  "jobId": "J-abc123...",
  "message": "Job cancellation requested",
  "currentStatus": "Running",
  "correlationId": "def456..."
}
```

**cURL Example:**

```bash
curl -X POST http://localhost:5005/api/jobs/J-abc123.../cancel
```

---

### POST /api/jobs/{jobId}/retry

Retries a failed job (placeholder implementation).

**Query Parameters:**
- `strategy` (optional): Retry strategy (e.g., "manual")

**Response:**

```json
{
  "jobId": "J-abc123...",
  "currentStatus": "Failed",
  "currentStage": "Render",
  "strategy": "manual",
  "message": "Job retry not yet fully implemented. Please create a new job with adjusted settings.",
  "suggestedActions": [
    "Re-generate with different TTS provider if narration failed",
    "Use software encoder (x264) if hardware encoding failed",
    "Check FFmpeg installation if render failed",
    "Verify input files are valid if validation failed"
  ],
  "correlationId": "def456..."
}
```

**cURL Example:**

```bash
curl -X POST http://localhost:5005/api/jobs/J-abc123.../retry?strategy=manual
```

---

## Correlation IDs

All endpoints accept and return a correlation ID for request tracking:

- **Request Header:** `X-Correlation-ID: <your-id>`
- **Response Header:** `X-Correlation-ID: <generated-or-provided-id>`

If not provided, a correlation ID is automatically generated.

---

## Error Handling

All error responses follow the [RFC 7807 Problem Details](https://datatracker.ietf.org/doc/html/rfc7807) format:

```json
{
  "type": "https://docs.aura.studio/errors/E500",
  "title": "Error Creating Job",
  "status": 500,
  "detail": "Failed to create job: <error message>",
  "correlationId": "def456...",
  "traceId": "0HN4K2G3F4T5U"
}
```

---

## Rate Limiting

Currently, there are no rate limits on the Jobs API. This may change in future versions.

---

## Presets

### sample-hello-youtube

A guaranteed keyless sample that uses only local providers and bundled assets. Perfect for testing and verification on clean machines.

**Features:**
- Uses local TTS (no API keys required)
- Bundled CC0 video clips and music
- 20-30 second duration
- Works completely offline with only FFmpeg

**Example:**

```bash
curl -X POST http://localhost:5005/api/jobs \
  -H "Content-Type: application/json" \
  -d '{
    "preset": "sample-hello-youtube",
    "options": {
      "allowSkipUnavailable": true,
      "quality": "balanced"
    }
  }'
```
