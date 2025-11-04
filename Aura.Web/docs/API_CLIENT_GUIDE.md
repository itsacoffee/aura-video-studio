# Typed API Client & Data Layer Guide

This guide explains how to use the new typed API client, React Query data layer, and enhanced SSE hook.

## Overview

The new data layer provides:

- **Type-safe API calls** via TypedApiClient
- **Automatic caching & deduplication** via React Query
- **Retry logic** with exponential backoff
- **Circuit breaker** to prevent cascading failures
- **Enhanced SSE** with auto-reconnect
- **Correlation IDs** for request tracking

## Table of Contents

- [TypedApiClient](#typedapiclient)
- [React Query Integration](#react-query-integration)
- [SSE Hook](#sse-hook)
- [Query Keys](#query-keys)
- [Error Handling](#error-handling)
- [Migration Guide](#migration-guide)

## TypedApiClient

### Basic Usage

```typescript
import { typedApiClient } from '@/api/typedClient';

// GET request
const data = await typedApiClient.get<JobResponse>('/api/jobs/123');

// POST request
const newJob = await typedApiClient.post<JobResponse>('/api/jobs', {
  topic: 'AI Video Generation',
  duration: 60,
});

// PUT request
const updated = await typedApiClient.put<JobResponse>('/api/jobs/123', {
  status: 'completed',
});

// DELETE request
await typedApiClient.delete('/api/jobs/123');
```

### Error Handling

The client throws `ApiError` instances with detailed information:

```typescript
import { ApiError } from '@/api/typedClient';

try {
  const data = await typedApiClient.get('/api/jobs/123');
} catch (error) {
  if (error instanceof ApiError) {
    console.log('Status:', error.status);
    console.log('Message:', error.message);
    console.log('Code:', error.code);
    console.log('Correlation ID:', error.correlationId);
    console.log('Details:', error.details);
  }
}
```

### Features

- **Circuit Breaker**: Automatically opens after 5 failures, preventing cascading failures
- **Retry Logic**: Retries transient errors (5xx, timeouts) up to 3 times with exponential backoff
- **Correlation IDs**: Each request gets a unique UUID for tracking
- **No Retry on 4xx**: Client errors (400-499) are not retried

## React Query Integration

### Setup

The QueryClient is already configured in `App.tsx`. All queries benefit from:

- Automatic request deduplication
- Caching with stale-while-revalidate
- Background refetching
- Optimistic updates

### Using Queries

```typescript
import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryClient';
import { typedApiClient } from '@/api/typedClient';

function JobDetails({ jobId }: { jobId: string }) {
  const { data, error, isLoading, refetch } = useQuery({
    queryKey: queryKeys.jobs.detail(jobId),
    queryFn: async () => typedApiClient.get<JobResponse>(`/api/jobs/${jobId}`),
    staleTime: 10 * 1000, // Consider data fresh for 10 seconds
    refetchInterval: 5000, // Auto-refetch every 5 seconds
  });

  if (isLoading) return <Spinner />;
  if (error) return <ErrorMessage error={error} />;

  return <div>{data.status}</div>;
}
```

### Using Mutations

```typescript
import { useMutation } from '@tanstack/react-query';
import { invalidateQueries } from '@/api/queryClient';

function CreateJobButton() {
  const mutation = useMutation({
    mutationFn: async (request: CreateJobRequest) =>
      typedApiClient.post<JobResponse>('/api/jobs', request),
    onSuccess: () => {
      // Invalidate and refetch job list
      invalidateQueries.jobs();
    },
  });

  const handleCreate = () => {
    mutation.mutate({
      topic: 'New Video',
      duration: 60,
    });
  };

  return (
    <button onClick={handleCreate} disabled={mutation.isPending}>
      {mutation.isPending ? 'Creating...' : 'Create Job'}
    </button>
  );
}
```

### Request Deduplication

React Query automatically deduplicates requests with the same query key:

```typescript
// These three components will share a single API request
function ComponentA() {
  const { data } = useQuery({
    queryKey: queryKeys.jobs.detail('123'),
    queryFn: () => typedApiClient.get('/api/jobs/123'),
  });
}

function ComponentB() {
  const { data } = useQuery({
    queryKey: queryKeys.jobs.detail('123'),
    queryFn: () => typedApiClient.get('/api/jobs/123'),
  });
}

function ComponentC() {
  const { data } = useQuery({
    queryKey: queryKeys.jobs.detail('123'),
    queryFn: () => typedApiClient.get('/api/jobs/123'),
  });
}
```

## SSE Hook

### Basic Usage

```typescript
import { useSse, SseConnectionState } from '@/hooks/useSse';

function JobProgress({ jobId }: { jobId: string }) {
  const { state, lastEvent, events } = useSse<JobProgressEvent>({
    url: `/api/jobs/${jobId}/events`,
    onMessage: (event) => {
      console.log('Progress:', event.data.progress);
    },
  });

  if (state === SseConnectionState.CONNECTING) {
    return <div>Connecting...</div>;
  }

  if (state === SseConnectionState.ERROR) {
    return <div>Connection error</div>;
  }

  return (
    <div>
      <div>Status: {state}</div>
      <div>Progress: {lastEvent?.data.progress}%</div>
    </div>
  );
}
```

### With Zod Validation

```typescript
import { z } from 'zod';

const progressSchema = z.object({
  status: z.string(),
  progress: z.number().min(0).max(100),
  message: z.string(),
});

type ProgressEvent = z.infer<typeof progressSchema>;

function JobProgress({ jobId }: { jobId: string }) {
  const { lastEvent, error } = useSse<ProgressEvent>({
    url: `/api/jobs/${jobId}/events`,
    schema: progressSchema,
    onMessage: (event) => {
      // event.data is validated and typed
      console.log(event.data.progress);
    },
  });

  // Invalid events are automatically filtered out
}
```

### Event Types

Listen to specific event types:

```typescript
const { events } = useSse({
  url: '/api/jobs/123/events',
  eventTypes: ['progress', 'status', 'completed'],
  onMessage: (event) => {
    switch (event.type) {
      case 'progress':
        // Handle progress
        break;
      case 'status':
        // Handle status change
        break;
      case 'completed':
        // Handle completion
        break;
    }
  },
});
```

### Manual Control

```typescript
function ControlledSSE() {
  const { state, close, reconnect } = useSse({
    url: '/api/events',
    autoReconnect: true,
  });

  return (
    <>
      <div>State: {state}</div>
      <button onClick={close}>Disconnect</button>
      <button onClick={reconnect}>Reconnect</button>
    </>
  );
}
```

### Features

- **Auto-reconnect**: Exponential backoff (1s to 30s max)
- **Last-Event-ID**: Resumes streams after reconnection
- **Zod Validation**: Type-safe event parsing
- **Connection States**: CONNECTING, CONNECTED, DISCONNECTED, ERROR
- **Cleanup**: Automatically closes connection on unmount

## Query Keys

Use the centralized query key factory to ensure consistency:

```typescript
import { queryKeys } from '@/api/queryClient';

// Health checks
queryKeys.health.live(); // ['health', 'live']
queryKeys.health.ready(); // ['health', 'ready']

// Jobs
queryKeys.jobs.all; // ['jobs']
queryKeys.jobs.list({ status: 'running' }); // ['jobs', 'list', { status: 'running' }]
queryKeys.jobs.detail('123'); // ['jobs', 'detail', '123']

// Settings
queryKeys.settings.hardware(); // ['settings', 'hardware']
queryKeys.settings.providers(); // ['settings', 'providers']

// Engines
queryKeys.engines.list(); // ['engines', 'list']
queryKeys.engines.detail('stable-diffusion'); // ['engines', 'detail', 'stable-diffusion']
```

### Invalidating Queries

```typescript
import { invalidateQueries } from '@/api/queryClient';

// After creating a job
invalidateQueries.jobs();

// After updating settings
invalidateQueries.settings();

// After modifying engines
invalidateQueries.engines();
```

## Error Handling

### API Errors

All API errors are instances of `ApiError`:

```typescript
import { ApiError } from '@/api/typedClient';

try {
  await typedApiClient.post('/api/jobs', data);
} catch (error) {
  if (error instanceof ApiError) {
    // Known API error
    if (error.status === 400) {
      // Validation error
      showToast('Invalid input: ' + error.message);
    } else if (error.status === 500) {
      // Server error
      showToast('Server error. Please try again.');
    } else if (error.code === 'CIRCUIT_OPEN') {
      // Circuit breaker is open
      showToast('Service temporarily unavailable');
    }
  } else {
    // Unknown error
    showToast('An unexpected error occurred');
  }
}
```

### Query Errors

React Query provides error handling at the query level:

```typescript
const { data, error, isError } = useQuery({
  queryKey: ['jobs', '123'],
  queryFn: () => typedApiClient.get('/api/jobs/123'),
  retry: 3, // Override default retry count
  onError: (error) => {
    // Handle error at query level
    console.error('Query failed:', error);
  },
});

if (isError) {
  return <ErrorDisplay error={error} />;
}
```

## Migration Guide

### From Axios to TypedApiClient

**Before:**

```typescript
import axios from 'axios';

const response = await axios.get('/api/jobs/123');
const data = response.data;
```

**After:**

```typescript
import { typedApiClient } from '@/api/typedClient';

const data = await typedApiClient.get<JobResponse>('/api/jobs/123');
```

### From Fetch to React Query

**Before:**

```typescript
const [data, setData] = useState(null);
const [loading, setLoading] = useState(true);

useEffect(() => {
  fetch('/api/jobs/123')
    .then((res) => res.json())
    .then(setData)
    .finally(() => setLoading(false));
}, []);
```

**After:**

```typescript
import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryClient';

const { data, isLoading } = useQuery({
  queryKey: queryKeys.jobs.detail('123'),
  queryFn: () => typedApiClient.get<JobResponse>('/api/jobs/123'),
});
```

### From SseClient to useSse Hook

**Before:**

```typescript
const [eventSource, setEventSource] = useState(null);

useEffect(() => {
  const es = new EventSource('/api/jobs/123/events');
  es.onmessage = (e) => {
    const data = JSON.parse(e.data);
    // Handle data
  };
  setEventSource(es);

  return () => es.close();
}, []);
```

**After:**

```typescript
import { useSse } from '@/hooks/useSse';

const { lastEvent } = useSse<JobProgressEvent>({
  url: '/api/jobs/123/events',
  onMessage: (event) => {
    // Handle event.data (already parsed and validated)
  },
});
```

## Best Practices

1. **Use Query Keys Consistently**: Always use the `queryKeys` factory
2. **Invalidate After Mutations**: Call `invalidateQueries` after successful mutations
3. **Type Your Data**: Always provide type parameters to API calls
4. **Handle Errors Gracefully**: Show user-friendly messages, log details
5. **Use SSE for Real-Time**: Replace polling with SSE where possible
6. **Validate SSE Events**: Use Zod schemas for type-safe event handling
7. **Monitor Circuit Breaker**: Watch for `CIRCUIT_OPEN` errors
8. **Use Correlation IDs**: Include them in error reports for debugging

## Troubleshooting

### Circuit Breaker Opens

If the circuit breaker opens (error code `CIRCUIT_OPEN`):

1. Check if the API is healthy
2. Review recent error logs
3. Wait for the timeout (60 seconds by default)
4. The circuit will automatically test recovery

### SSE Disconnects

If SSE connections keep dropping:

1. Check network stability
2. Review server-side SSE implementation
3. Verify firewall/proxy settings
4. Check `maxReconnectAttempts` configuration

### Stale Data

If queries show stale data:

1. Adjust `staleTime` for the query
2. Use `refetchInterval` for automatic updates
3. Call `refetch()` manually when needed
4. Invalidate queries after mutations

## Additional Resources

- [React Query Documentation](https://tanstack.com/query/latest)
- [Server-Sent Events Specification](https://html.spec.whatwg.org/multipage/server-sent-events.html)
- [Zod Documentation](https://zod.dev/)
