# API Reference Overview

Aura Video Studio exposes a RESTful API for video generation and management, with Server-Sent Events (SSE) for real-time progress updates.

## Base URL

- **Development**: `http://localhost:5005`
- **Production (Electron)**: Embedded backend on dynamic port

## API Version

Current API version: **v1**

All endpoints are prefixed with `/api/`

## Authentication

The API runs on localhost loopback by default and does not require authentication for local development. In production Electron builds, the API is embedded and accessed only by the frontend.

## Content Type

- **Request**: `application/json`
- **Response**: `application/json`
- **SSE**: `text/event-stream`

## Error Handling

Errors follow RFC 7807 ProblemDetails format:

```json
{
  "type": "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E400",
  "title": "Validation Error",
  "status": 400,
  "detail": "Brief topic is required",
  "correlationId": "abc-123-def"
}
```

## Core Endpoints

See individual API documentation files for detailed endpoint specifications:

- [Jobs API](jobs-api.md) - Video generation job management
- [Settings API](settings-api.md) - Application settings
- [Providers API](providers-api.md) - Provider configuration and validation
- [FFmpeg API](ffmpeg-api.md) - FFmpeg status and capabilities
- [Health Checks](health-api.md) - Liveness and readiness probes
- [SSE Events](sse-events.md) - Server-Sent Events for real-time updates

## Data Transfer Objects (DTOs)

All DTOs are defined in `Aura.Api/Models/ApiModels.V1/`:

- **Dtos.cs** - All request/response DTOs
- **Enums.cs** - All enums (JobStatus, AspectRatio, etc.)
- **ProviderSelection.cs** - Provider selection models

Frontend TypeScript types are maintained in `Aura.Web/src/types/api-v1.ts` and should match backend DTOs.

## Quick Example

```typescript
// Create a video generation job
const response = await fetch('http://localhost:5005/api/jobs', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    brief: {
      topic: 'AI Technology',
      audience: 'Students',
      goal: 'Educate',
      tone: 'Friendly'
    }
  })
});

const job = await response.json();

// Subscribe to progress updates
const eventSource = new EventSource(`http://localhost:5005/api/jobs/${job.id}/events`);
eventSource.addEventListener('step-progress', (event) => {
  console.log('Progress:', JSON.parse(event.data));
});
```

## Additional Documentation

- [SSE Implementation Guide](../development/SSE_IMPLEMENTATION_GUIDE.md)
- [Frontend API Integration](../development/FRONTEND_API_INTEGRATION_GUIDE.md)
- [Error Handling Guide](../development/ERROR_HANDLING_GUIDE.md)

---

Last updated: 2025-11-18
