# Hybrid API Communication Layer

## Overview

The hybrid API communication layer provides a unified interface for making API calls in both web browsers (HTTP) and Electron environments (IPC). It automatically detects the runtime environment and uses the appropriate transport method.

## Architecture

```
┌─────────────────────────────────────────────┐
│           Application Layer                  │
│  (Components, Services, State Management)   │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│            API Client                        │
│    (Unified interface for all requests)     │
└──────────────────┬──────────────────────────┘
                   │
         ┌─────────┴─────────┐
         │                   │
┌────────▼────────┐  ┌──────▼──────────┐
│  HTTP Transport │  │  IPC Transport  │
│   (Web/Dev)     │  │   (Electron)    │
└────────┬────────┘  └──────┬──────────┘
         │                   │
┌────────▼────────┐  ┌──────▼──────────┐
│  Axios Client   │  │  window.aura│
│  (REST API)     │  │  (IPC Bridge)   │
└─────────────────┘  └─────────────────┘
```

## Key Components

### 1. Transport Interface (`IApiTransport`)

Defines the contract for all transport implementations:

- `request()` - Make HTTP requests (GET, POST, PUT, PATCH, DELETE)
- `subscribe()` - Subscribe to SSE (Server-Sent Events)
- `upload()` - Upload files with progress tracking
- `download()` - Download files with progress tracking
- `isAvailable()` - Check if transport is available
- `getName()` - Get transport name for logging

### 2. HTTP Transport

Uses Axios for traditional HTTP communication:

- Used in web browsers and development mode
- Supports all standard HTTP methods
- Native EventSource for SSE
- XHR for file uploads/downloads with progress
- Circuit breaker and retry logic
- Correlation IDs for request tracking

### 3. IPC Transport

Uses Electron's IPC bridge for secure communication:

- Used in Electron desktop application
- Communicates via `window.aura` API
- Backend URL managed by Electron
- Same interface as HTTP transport
- Automatic fallback to HTTP for SSE

### 4. API Client

Provides a convenient wrapper around the transport layer:

- Auto-detects environment (web vs Electron)
- Creates appropriate transport instance
- Maintains backward compatibility
- Provides convenience methods: `get()`, `post()`, `put()`, `patch()`, `delete()`
- Supports SSE subscriptions
- Supports file uploads/downloads

### 5. Transport Factory

Automatically selects the correct transport:

```typescript
if (window.aura exists) {
  return new IpcTransport();
} else {
  return new HttpTransport(baseURL);
}
```

## Usage Examples

### Basic Requests

```typescript
import { apiClient } from '@/services/api/client';

// GET request
const health = await apiClient.get<HealthResponse>('/health');

// POST request with data
const job = await apiClient.post<JobResponse>('/api/jobs', {
  title: 'My Job',
  type: 'video',
});

// PUT request
const updated = await apiClient.put<JobResponse>(`/api/jobs/${jobId}`, updateData);

// DELETE request
await apiClient.delete(`/api/jobs/${jobId}`);
```

### SSE Subscriptions

```typescript
// Subscribe to server-sent events
const unsubscribe = apiClient.subscribe(`/api/jobs/${jobId}/events`, {
  onMessage: (event) => {
    const data = JSON.parse(event.data);
    console.log('Progress:', data.percentage);
  },
  onError: (error) => {
    console.error('SSE error:', error);
  },
  onOpen: () => {
    console.log('SSE connection opened');
  },
  onClose: () => {
    console.log('SSE connection closed');
  },
  maxRetries: 5,
  retryDelay: 2000,
});

// Later, unsubscribe
unsubscribe();
```

### File Uploads

```typescript
// Upload file with progress
const result = await apiClient.uploadFile<UploadResponse>(
  '/api/upload/video',
  videoFile,
  (progress) => {
    console.log(`Upload progress: ${progress}%`);
  }
);
```

### File Downloads

```typescript
// Download file with progress
await apiClient.downloadFile(`/api/download/video/${videoId}`, 'my-video.mp4', (progress) => {
  console.log(`Download progress: ${progress}%`);
});
```

### Environment Detection

```typescript
// Check if running in Electron
if (apiClient.isElectron()) {
  console.log('Running in Electron');
} else {
  console.log('Running in browser');
}

// Get transport name (for debugging)
console.log('Transport:', apiClient.getTransportName()); // "HTTP" or "IPC"

// Get environment
const env = apiClient.getEnvironment(); // "web" or "electron"
```

### Custom Options

```typescript
import type { TransportRequestOptions } from '@/services/api/transport';

// Request with custom timeout
const options: TransportRequestOptions = {
  timeout: 10000, // 10 seconds
  skipRetry: true,
  skipCircuitBreaker: true,
  headers: {
    'X-Custom-Header': 'value',
  },
};

const response = await apiClient.get('/api/endpoint', options);
```

## Migration Guide

### From Old API Client

**Old code:**

```typescript
import { get, post, uploadFile } from './api/apiClient';

const health = await get<HealthResponse>('/health');
const job = await post<JobResponse>('/api/jobs', data);
const upload = await uploadFile('/api/upload', file, onProgress);
```

**New code:**

```typescript
import { apiClient } from './api/client';

const health = await apiClient.get<HealthResponse>('/health');
const job = await apiClient.post<JobResponse>('/api/jobs', data);
const upload = await apiClient.uploadFile('/api/upload', file, onProgress);
```

### Key Changes

1. **Import statement:**
   - Old: `import { get, post, ... } from './api/apiClient'`
   - New: `import { apiClient } from './api/client'`

2. **Method calls:**
   - Old: `get(url, options)`
   - New: `apiClient.get(url, options)`

3. **File operations:**
   - Old: `uploadFile(url, file, onProgress)`
   - New: `apiClient.uploadFile(url, file, onProgress)`

4. **SSE subscriptions:**
   - Old: Create separate SSE client
   - New: Use `apiClient.subscribe()`

## Benefits

### 1. Unified Interface

- Same code works in both web and Electron
- No environment-specific conditionals
- Consistent error handling

### 2. Automatic Environment Detection

- Detects Electron vs web at runtime
- Uses optimal transport for each environment
- No manual configuration needed

### 3. Type Safety

- Full TypeScript support
- Generic type parameters for responses
- Compile-time error checking

### 4. Backward Compatibility

- Maintains existing API surface
- Minimal changes to existing code
- Progressive migration path

### 5. Security

- IPC communication in Electron (more secure than HTTP)
- No exposed backend URL in Electron
- Sandboxed renderer process

### 6. Performance

- IPC is faster than HTTP for local communication
- No network overhead in Electron
- Efficient binary data transfer

## Testing

### Unit Tests

```typescript
import { describe, it, expect, vi } from 'vitest';
import { TransportFactory } from './transport';

describe('TransportFactory', () => {
  it('should detect web environment', () => {
    window.aura = undefined;
    expect(TransportFactory.isElectron()).toBe(false);
  });

  it('should detect Electron environment', () => {
    window.aura = {
      /* mock */
    };
    expect(TransportFactory.isElectron()).toBe(true);
  });

  it('should create HTTP transport in web', () => {
    window.aura = undefined;
    const transport = TransportFactory.create('http://localhost:5005');
    expect(transport.getName()).toBe('HTTP');
  });

  it('should create IPC transport in Electron', () => {
    window.aura = { backend: { getBaseUrl: vi.fn() } };
    const transport = TransportFactory.create('http://localhost:5005');
    expect(transport.getName()).toBe('IPC');
  });
});
```

### Integration Tests

Test your services in both environments:

1. Run tests in browser (HTTP transport)
2. Run tests in Electron (IPC transport)
3. Verify same behavior in both

## Troubleshooting

### Issue: "IPC Transport requires Electron environment"

**Cause:** Trying to create IPC transport outside Electron

**Solution:** Use `TransportFactory.create()` instead of directly instantiating `IpcTransport`

### Issue: SSE not working in Electron

**Cause:** EventSource not properly initialized

**Solution:** Ensure backend URL is correctly retrieved via `window.aura.backend.getBaseUrl()`

### Issue: File uploads failing

**Cause:** Incorrect FormData handling

**Solution:** Both transports handle FormData automatically, just pass the File object

### Issue: CORS errors in web

**Cause:** Backend not configured for CORS

**Solution:** Configure backend CORS settings for development URL

## Best Practices

1. **Always use `apiClient` instance:**

   ```typescript
   import { apiClient } from '@/services/api/client';
   ```

2. **Don't instantiate transports directly:**

   ```typescript
   // ❌ Don't do this
   const transport = new HttpTransport();

   // ✅ Do this instead
   const client = new ApiClient();
   ```

3. **Use type parameters for responses:**

   ```typescript
   const response = await apiClient.get<MyResponseType>('/api/endpoint');
   ```

4. **Handle errors properly:**

   ```typescript
   try {
     const result = await apiClient.post('/api/endpoint', data);
   } catch (error) {
     console.error('API error:', error);
   }
   ```

5. **Clean up SSE subscriptions:**

   ```typescript
   const unsubscribe = apiClient.subscribe(url, options);

   // Later...
   useEffect(() => {
     return () => unsubscribe(); // Cleanup on unmount
   }, []);
   ```

## Future Enhancements

- [ ] Request queuing and deduplication
- [ ] Offline request caching
- [ ] Request retry with exponential backoff
- [ ] Circuit breaker pattern
- [ ] Request/response interceptors
- [ ] Mock transport for testing
- [ ] WebSocket support
- [ ] GraphQL support
