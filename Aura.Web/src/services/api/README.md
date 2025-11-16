# API Client Documentation

## Overview

The enhanced API client provides comprehensive error handling, automatic retry logic, circuit breaker pattern, and request queueing for all API interactions in Aura Video Studio.

## Features

### 🔄 Automatic Retry with Exponential Backoff

- Automatically retries transient errors (5xx, network errors, timeouts)
- Uses exponential backoff: 1s, 2s, 4s delays
- Up to 3 retry attempts (4 total attempts including initial request)
- Does NOT retry client errors (4xx)

### 🛡️ Circuit Breaker Pattern

- Prevents cascading failures when backend is consistently failing
- Opens after 5 consecutive failures
- Auto-recovery attempt after 60 seconds (half-open state)
- Closes after 2 successful requests in half-open state

### 📊 Request Queueing

- Prevents rate limit errors (429 Too Many Requests)
- Queues requests with 1-second intervals
- Per-endpoint queue keys

### ⏱️ Timeout Configuration

- Default timeout: 30 seconds
- Configurable per-request
- Longer timeouts for video generation (up to 120s)

### 🚫 Request Cancellation

- AbortController support
- Prevents memory leaks on navigation
- Properly cancels pending requests

### 💬 User-Friendly Error Messages

- Maps HTTP status codes to helpful messages
- Application-specific error codes (E300-E332)
- Actionable guidance for users
- Full technical details logged

### 📝 Comprehensive Logging

- All requests/responses logged
- Performance metrics for slow requests (>1s)
- Error context for debugging
- Integration with logging service

---

## Environment Detection and Configuration

### API Base URL Resolution

The API client automatically detects the appropriate backend URL using a priority-based fallback chain:

1. **Electron Desktop App**: `window.aura.backend.getBaseUrl()` or `window.AURA_BACKEND_URL`
2. **Environment Variable**: `VITE_API_BASE_URL` from `.env` files
3. **Current Origin**: `window.location.origin` (when UI served from backend)
4. **Development Fallback**: `http://127.0.0.1:5005`

```typescript
import { resolveApiBaseUrl, isElectronEnvironment } from '@/config/apiBaseUrl';

// Synchronous (uses cached value)
const config = resolveApiBaseUrl();
console.log(config.value); // "http://localhost:5005"
console.log(config.source); // "electron" | "env" | "origin" | "fallback"
console.log(config.isElectron); // true/false

// Asynchronous (for initial Electron setup)
const configAsync = await resolveApiBaseUrlAsync();
```

### Environment Variables

Configure via `.env` files:

```env
# .env.development
VITE_API_BASE_URL=http://localhost:5005

# .env.production
VITE_API_BASE_URL=https://api.your-domain.com
```

### Electron Integration

The system detects Electron and uses the backend URL provided by the Electron process:

```typescript
// Electron preload script sets these globals
window.AURA_BACKEND_URL = 'http://localhost:5005';
window.AURA_IS_ELECTRON = true;

// Or via async API
window.aura = {
  backend: {
    getUrl: async () => 'http://localhost:5005',
  },
};
```

---

## Usage Examples

### Basic GET Request

```typescript
import { get } from './services/api/apiClient';

// Simple GET request with automatic retry and error handling
const projects = await get<Project[]>('/api/projects');
```

### POST Request with Data

```typescript
import { post } from './services/api/apiClient';

const newProject = await post<Project>('/api/projects', {
  name: 'My Video Project',
  description: 'A test project',
});
```

### Request with Custom Timeout

```typescript
import { postWithTimeout } from './services/api/apiClient';

// Video generation may take longer
const video = await postWithTimeout<VideoResult>(
  '/api/video/generate',
  { projectId: '123' },
  120000 // 2 minutes
);
```

### Cancellable Request

```typescript
import { createAbortController, getCancellable } from './services/api/apiClient';

const abortController = createAbortController();

// Start request
const requestPromise = getCancellable<Data>('/api/data', abortController);

// Cancel on navigation
useEffect(() => {
  return () => abortController.abort();
}, []);
```

### Queued Request (Rate Limited Endpoint)

```typescript
import { getQueued } from './services/api/apiClient';

// Queue requests to prevent rate limiting
const data = await getQueued<Data>('/api/ai/generate', 'ai-generation-queue');
```

### File Upload with Progress

```typescript
import { uploadFile } from './services/api/apiClient';

const asset = await uploadFile<Asset>('/api/assets/upload', file, (progress) => {
  console.log(`Upload progress: ${progress}%`);
});
```

### Skip Retry for Specific Request

```typescript
import { get } from './services/api/apiClient';

// Don't retry this request
const data = await get<Data>('/api/data', {
  _skipRetry: true,
});
```

### Skip Circuit Breaker

```typescript
import { get } from './services/api/apiClient';

// Bypass circuit breaker for health checks
const health = await get<Health>('/api/health', {
  _skipCircuitBreaker: true,
});
```

## Error Handling

### Error Codes

The API client maps error codes to user-friendly messages:

| Code | Description              | Actions                                        |
| ---- | ------------------------ | ---------------------------------------------- |
| E300 | Script Generation Failed | Try different prompt, check AI provider config |
| E304 | Invalid Content Plan     | Check duration range, verify required fields   |
| E306 | Authentication Failed    | Check API key, verify credentials              |
| E310 | Video Generation Failed  | Check input assets, try again                  |
| E320 | Asset Upload Failed      | Check file size/format, verify connection      |
| E330 | Provider Not Configured  | Configure provider in settings                 |
| E332 | Rate Limit Exceeded      | Wait before retrying, upgrade plan             |

### Handling Errors in Components

```typescript
try {
  const data = await get<Data>('/api/data');
  // Handle success
} catch (error: any) {
  // User-friendly message for display
  const message = error.userMessage || 'An error occurred';

  // Error code for specific handling
  const code = error.errorCode;

  // Full technical details in logs
  console.error('API Error:', {
    message: error.message,
    status: error.response?.status,
    data: error.response?.data,
  });

  // Show toast/notification
  showToast(message, 'error');
}
```

## Circuit Breaker Management

```typescript
import { getCircuitBreakerState, resetCircuitBreaker } from './services/api/apiClient';

// Check circuit breaker state
const state = getCircuitBreakerState(); // 'CLOSED' | 'OPEN' | 'HALF_OPEN'

// Manually reset circuit breaker (e.g., after manual intervention)
resetCircuitBreaker();
```

## HTTP Methods

| Method | Function                       | Use Case         |
| ------ | ------------------------------ | ---------------- |
| GET    | `get<T>(url, config?)`         | Fetch data       |
| POST   | `post<T>(url, data, config?)`  | Create resources |
| PUT    | `put<T>(url, data, config?)`   | Update resources |
| PATCH  | `patch<T>(url, data, config?)` | Partial updates  |
| DELETE | `del<T>(url, config?)`         | Delete resources |

## Advanced Configuration

### Extended Request Config

```typescript
interface ExtendedAxiosRequestConfig {
  _retry?: number; // Current retry count (internal)
  _skipRetry?: boolean; // Skip automatic retry
  _skipCircuitBreaker?: boolean; // Bypass circuit breaker
  _timeout?: number; // Custom timeout in ms
  _queueKey?: string; // Queue key for rate limiting
  // ... standard AxiosRequestConfig options
}
```

## Migration Guide

### From Raw Fetch

**Before:**

```typescript
const response = await fetch('/api/projects');
if (!response.ok) {
  throw new Error('Failed to fetch projects');
}
const projects = await response.json();
```

**After:**

```typescript
const projects = await get<Project[]>('/api/projects');
```

### From Direct Axios

**Before:**

```typescript
const response = await axios.post('/api/projects', data);
return response.data;
```

**After:**

```typescript
return post<Project>('/api/projects', data);
```

## Best Practices

1. **Always specify type parameters** for type safety:

   ```typescript
   const data = await get<MyType>('/api/data');
   ```

2. **Handle errors gracefully** with user-friendly messages:

   ```typescript
   catch (error: any) {
     showToast(error.userMessage, 'error');
   }
   ```

3. **Use appropriate timeouts** for long-running operations:

   ```typescript
   await postWithTimeout('/api/video/generate', data, 120000);
   ```

4. **Cancel requests on unmount** to prevent memory leaks:

   ```typescript
   useEffect(() => {
     const controller = createAbortController();
     getCancellable('/api/data', controller);
     return () => controller.abort();
   }, []);
   ```

5. **Queue rate-limited endpoints** to prevent 429 errors:
   ```typescript
   await getQueued('/api/ai/generate', 'ai-queue');
   ```

## Performance Considerations

- Requests >1s are logged as performance issues
- Circuit breaker prevents wasted attempts to failing services
- Request queueing reduces concurrent load
- Retry delays use exponential backoff to avoid overwhelming server

## Logging

All API operations are logged with:

- Request method and URL
- Request/response data
- Performance metrics
- Error details with context
- Circuit breaker state changes

Access logs through the logging service or browser console.

## Testing

The API client includes comprehensive test coverage:

- Unit tests: `apiClient.test.ts`
- Integration tests: `apiClient-integration.test.ts`

Run tests:

```bash
npm test -- src/test/apiClient
```
