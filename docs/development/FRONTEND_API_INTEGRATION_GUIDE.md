# Frontend API Integration Guide

## Quick Start

This guide explains how to use the newly integrated frontend API client to communicate with the Aura backend.

---

## Table of Contents

1. [Authentication](#authentication)
2. [API Services](#api-services)
3. [React Query Hooks](#react-query-hooks)
4. [Error Handling](#error-handling)
5. [Real-time Updates](#real-time-updates)
6. [Best Practices](#best-practices)

---

## Authentication

### Using the Auth Hook

```typescript
import { useAuth } from '@/hooks/useAuth';

function MyComponent() {
  const { isAuthenticated, user, login, logout, isLoading, error } = useAuth();

  const handleLogin = async () => {
    try {
      await login({
        email: 'user@example.com',
        password: 'password123',
        rememberMe: true,
      });
      // User is now logged in
    } catch (error) {
      // Error is automatically displayed as notification
      console.error('Login failed:', error);
    }
  };

  return (
    <div>
      {isAuthenticated ? (
        <>
          <p>Welcome, {user?.name}!</p>
          <button onClick={logout}>Logout</button>
        </>
      ) : (
        <button onClick={handleLogin} disabled={isLoading}>
          Login
        </button>
      )}
    </div>
  );
}
```

### Protected Routes

```typescript
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { Route, Routes } from 'react-router-dom';

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      
      {/* Protected route - requires authentication */}
      <Route
        path="/dashboard"
        element={
          <ProtectedRoute>
            <Dashboard />
          </ProtectedRoute>
        }
      />
      
      {/* Admin-only route */}
      <Route
        path="/admin"
        element={
          <ProtectedRoute requiredRole="admin">
            <AdminPanel />
          </ProtectedRoute>
        }
      />
    </Routes>
  );
}
```

### Manual Token Management

```typescript
import { useAuthStore } from '@/stores/authStore';

// Get auth state
const token = useAuthStore((state) => state.token);
const user = useAuthStore((state) => state.user);

// Manually set token (e.g., from external auth)
useAuthStore.getState().setToken('your-token-here', 'refresh-token', 3600);

// Check token expiry
const isExpired = useAuthStore.getState().checkTokenExpiry();

// Manually refresh token
await useAuthStore.getState().refreshAuthToken();
```

---

## API Services

### Video Generation

```typescript
import * as videoApi from '@/services/api/videoGenerationApi';

// Generate video
const job = await videoApi.generateVideo({
  script: 'My video script',
  title: 'My Video',
  settings: {
    resolution: '1080p',
    fps: 30,
    quality: 'high',
  },
});

// Get job status
const status = await videoApi.getJobStatus(job.id);

// List all jobs
const { jobs, total } = await videoApi.getJobs({
  status: 'completed',
  limit: 10,
  offset: 0,
});

// Cancel job
await videoApi.cancelJob(job.id);

// Download video
await videoApi.downloadVideo(job.id, 'my-video.mp4');
```

### User Management

```typescript
import * as userApi from '@/services/api/userApi';

// Get user preferences
const preferences = await userApi.getUserPreferences();

// Update preferences
await userApi.updateUserPreferences({
  theme: 'dark',
  language: 'en-US',
  notifications: {
    email: true,
    push: false,
  },
});

// Upload avatar
const file = new File([blob], 'avatar.jpg', { type: 'image/jpeg' });
const { avatarUrl } = await userApi.uploadAvatar(file);

// Get activity log
const { activities, total } = await userApi.getUserActivity(50, 0);
```

### Admin Operations

```typescript
import * as adminApi from '@/services/api/adminApi';

// Get system stats
const stats = await adminApi.getSystemStats();

// List users
const { users, total } = await adminApi.getUsers(1, 50, {
  status: 'active',
  role: 'user',
});

// Update user
await adminApi.updateUser(userId, {
  name: 'New Name',
  role: 'admin',
});

// Suspend user
await adminApi.suspendUser(userId, 'Violation of terms');

// Get audit logs
const { logs } = await adminApi.getAuditLogs(1, 100, {
  action: 'login',
  startDate: '2024-01-01',
  endDate: '2024-12-31',
});
```

### Projects

```typescript
import * as projectService from '@/services/projectService';

// Get all projects
const projects = await projectService.getProjects();

// Get specific project
const project = await projectService.getProject(projectId);

// Save project
const saved = await projectService.saveProject(
  'My Project',
  projectFile,
  projectId,
  'Project description',
  thumbnailUrl
);

// Delete project
await projectService.deleteProject(projectId);

// Duplicate project
const duplicate = await projectService.duplicateProject(projectId);
```

---

## React Query Hooks

### Video Generation Hook

```typescript
import { useVideoGeneration, useJobStatus, useJobs } from '@/hooks/useApiClient';

function VideoGenerator() {
  const { generateVideo } = useVideoGeneration();
  const [jobId, setJobId] = useState<string>();
  
  // Query job status with auto-polling
  const { data: job, isLoading } = useJobStatus(jobId);
  
  // List all jobs
  const { data: jobsData } = useJobs({ status: 'processing' });

  const handleGenerate = async () => {
    const result = await generateVideo.mutateAsync({
      script: 'My script',
      settings: { resolution: '1080p' },
    });
    setJobId(result.id);
  };

  return (
    <div>
      <button onClick={handleGenerate} disabled={generateVideo.isPending}>
        Generate Video
      </button>
      
      {job && (
        <div>
          <p>Status: {job.status}</p>
          <p>Progress: {job.progress}%</p>
        </div>
      )}
    </div>
  );
}
```

### User Preferences Hook

```typescript
import { useUserPreferences } from '@/hooks/useApiClient';

function SettingsPage() {
  const { data: preferences, update, isLoading } = useUserPreferences();

  const handleSave = async (updates: Partial<UserPreferences>) => {
    await update.mutateAsync(updates);
  };

  if (isLoading) return <div>Loading...</div>;

  return (
    <div>
      <p>Theme: {preferences?.theme}</p>
      <button onClick={() => handleSave({ theme: 'dark' })}>
        Switch to Dark Mode
      </button>
    </div>
  );
}
```

### Admin Hooks

```typescript
import { useAdminStats, useAdminUsers } from '@/hooks/useApiClient';

function AdminDashboard() {
  const { data: stats } = useAdminStats();
  const { data: usersData, updateUser, suspendUser } = useAdminUsers(1, 50);

  const handleUpdateUser = async (userId: string, updates: any) => {
    await updateUser.mutateAsync({ userId, updates });
  };

  return (
    <div>
      <h1>System Stats</h1>
      <p>Total Users: {stats?.users.total}</p>
      <p>Active Users: {stats?.users.active}</p>
      
      <h2>Users</h2>
      {usersData?.users.map(user => (
        <div key={user.id}>
          <p>{user.name} ({user.email})</p>
          <button onClick={() => handleUpdateUser(user.id, { role: 'admin' })}>
            Make Admin
          </button>
        </div>
      ))}
    </div>
  );
}
```

---

## Error Handling

### Using Error Boundary

```typescript
import { ErrorBoundary } from '@/components/ErrorBoundary';

function App() {
  return (
    <ErrorBoundary
      fallback={<div>Something went wrong. Please refresh the page.</div>}
      onError={(error, errorInfo) => {
        console.error('Error caught by boundary:', error, errorInfo);
      }}
    >
      <YourApp />
    </ErrorBoundary>
  );
}
```

### Centralized Error Handling

```typescript
import {
  handleError,
  withErrorHandling,
  NetworkError,
  ValidationError,
} from '@/utils/errorHandler';

// Manual error handling
try {
  await apiCall();
} catch (error) {
  handleError(error, {
    title: 'Operation Failed',
    message: 'Custom error message',
    showNotification: true,
    context: 'myComponent',
  });
}

// Wrap function with error handling
const fetchData = withErrorHandling(
  async () => {
    const data = await apiCall();
    return data;
  },
  {
    title: 'Failed to fetch data',
    context: 'dataFetch',
  }
);

// Throw custom errors
if (!isValid) {
  throw new ValidationError('Invalid input', {
    email: 'Email is required',
    password: 'Password must be at least 8 characters',
  });
}
```

### Error Hook

```typescript
import { useErrorHandler } from '@/components/ErrorBoundary';

function MyComponent() {
  const throwError = useErrorHandler();

  const handleAction = async () => {
    try {
      await riskyOperation();
    } catch (error) {
      // Throw to nearest error boundary
      throwError(error as Error);
    }
  };

  return <button onClick={handleAction}>Do Something</button>;
}
```

---

## Real-time Updates

### Server-Sent Events (SSE)

```typescript
import { createSseClient } from '@/services/api/sseClient';

function JobMonitor({ jobId }: { jobId: string }) {
  const [progress, setProgress] = useState(0);
  const sseClientRef = useRef<any>(null);

  useEffect(() => {
    // Create SSE client
    const client = createSseClient(jobId);

    // Subscribe to events
    client.on('job-status', (event) => {
      console.log('Job status:', event.data);
    });

    client.on('step-progress', (event) => {
      setProgress(event.data.progressPct);
    });

    client.on('job-completed', (event) => {
      console.log('Job completed!', event.data);
    });

    // Monitor connection state
    client.onStatusChange((state) => {
      console.log('Connection state:', state.status);
    });

    // Connect
    client.connect();
    sseClientRef.current = client;

    // Cleanup
    return () => {
      client.close();
    };
  }, [jobId]);

  return <div>Progress: {progress}%</div>;
}
```

### SignalR (when backend hubs are implemented)

```typescript
import { getProgressHubClient } from '@/services/api/signalRClient';

function RealTimeUpdates() {
  const hubClient = useRef(getProgressHubClient());

  useEffect(() => {
    const client = hubClient.current;

    // Connect to hub
    client.connect().catch(console.error);

    // Subscribe to events
    client.on('ProgressUpdate', (data) => {
      console.log('Progress:', data);
    });

    // Invoke hub method
    const sendMessage = async () => {
      await client.invoke('SendMessage', 'Hello from client!');
    };

    // Cleanup
    return () => {
      client.disconnect();
    };
  }, []);

  return <div>Real-time updates enabled</div>;
}
```

---

## Best Practices

### 1. Use React Query Hooks

**✅ DO:**
```typescript
import { useJobs } from '@/hooks/useApiClient';

function JobsList() {
  const { data, isLoading, error } = useJobs();
  // Automatic caching, refetching, and state management
}
```

**❌ DON'T:**
```typescript
function JobsList() {
  const [jobs, setJobs] = useState([]);
  
  useEffect(() => {
    videoApi.getJobs().then(setJobs);
  }, []);
  // Manual state management, no caching
}
```

### 2. Handle Loading and Error States

**✅ DO:**
```typescript
function MyComponent() {
  const { data, isLoading, error } = useQuery(...);

  if (isLoading) return <LoadingSpinner />;
  if (error) return <ErrorMessage error={error} />;

  return <DataDisplay data={data} />;
}
```

### 3. Use Error Boundaries

**✅ DO:**
```typescript
<ErrorBoundary>
  <CriticalComponent />
</ErrorBoundary>
```

### 4. Leverage Automatic Features

- **Request Deduplication:** Automatic for POST/PUT/PATCH
- **Retry Logic:** Automatic with exponential backoff
- **Circuit Breaker:** Automatic protection against cascading failures
- **Token Refresh:** Automatic when token is about to expire

### 5. Type Safety

**✅ DO:**
```typescript
import type { VideoGenerationRequest, VideoJob } from '@/services/api/videoGenerationApi';

const request: VideoGenerationRequest = {
  script: 'My script',
  settings: { resolution: '1080p' },
};
```

### 6. Logging

All API services automatically log operations. Access logs via:
```typescript
import { loggingService } from '@/services/loggingService';

loggingService.info('Custom log message', 'myComponent', 'myFunction', { data });
```

---

## Configuration

### Environment Variables

Create `.env.development`:
```env
VITE_API_BASE_URL=http://127.0.0.1:5005
VITE_APP_VERSION=1.0.0
VITE_ENV=development
VITE_ENABLE_DEBUG=true
```

Create `.env.production`:
```env
VITE_API_BASE_URL=https://api.yourdomain.com
VITE_APP_VERSION=1.0.0
VITE_ENV=production
VITE_ENABLE_DEBUG=false
```

### Backend CORS (appsettings.json)

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://yourdomain.com",
      "https://app.yourdomain.com"
    ]
  }
}
```

---

## Troubleshooting

### Issue: CORS errors

**Solution:** Check that:
1. Backend CORS is configured in `Program.cs`
2. Frontend URL is in allowed origins
3. Browser isn't blocking requests

### Issue: 401 Unauthorized

**Solution:**
1. Check if user is logged in: `useAuth().isAuthenticated`
2. Verify token hasn't expired
3. Try manual login again

### Issue: Network errors

**Solution:**
1. Check if backend is running
2. Verify API base URL in `.env`
3. Check browser console for details

### Issue: SSE not connecting

**Solution:**
1. Check job ID is valid
2. Backend SSE endpoint is working
3. Check browser network tab for SSE connection

---

## Additional Resources

- [API Reference](./api/index.md)
- [Backend Integration Summary](./FRONTEND_BACKEND_INTEGRATION_COMPLETE.md)
- [React Query Documentation](https://tanstack.com/query/latest)
- [Zustand Documentation](https://zustand-demo.pmnd.rs/)

---

## Support

For issues or questions:
1. Check this guide
2. Review implementation summary: `FRONTEND_BACKEND_INTEGRATION_COMPLETE.md`
3. Check API service files for detailed JSDoc comments
4. Review error logs in browser console
