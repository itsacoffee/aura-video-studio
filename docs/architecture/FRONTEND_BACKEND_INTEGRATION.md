# Frontend-Backend Integration Guide

## Overview

This document describes the complete frontend-backend integration implementation for PR #6. The integration connects React components with ASP.NET Core APIs, providing a full video generation and project management workflow.

## Architecture

### API Layer (`Aura.Web/src/services/api/`)

#### 1. Video Generation API (`videoApi.ts`)
- **Purpose**: Handles video generation requests and status polling
- **Key Functions**:
  - `generateVideo()` - Start video generation job
  - `getVideoStatus()` - Poll job status
  - `cancelVideoGeneration()` - Cancel running job
  - `streamProgress()` - SSE connection for real-time updates
  - `listJobs()` - Get all jobs

#### 2. Projects API (`projectsApi.ts`)
- **Purpose**: Project CRUD operations
- **Key Functions**:
  - `listProjects()` - Get projects with filters/pagination
  - `getProject()` - Get single project
  - `createProject()` - Create new project
  - `updateProject()` - Update existing project
  - `deleteProject()` - Delete project
  - `duplicateProject()` - Duplicate project
  - `getProjectStatistics()` - Get project stats

#### 3. Base API Client (`apiClient.ts`)
- **Features**:
  - Circuit breaker pattern for fault tolerance
  - Automatic retry with exponential backoff
  - Request deduplication
  - Correlation ID tracking
  - Comprehensive error handling

### Hooks Layer (`Aura.Web/src/hooks/`)

#### 1. useVideoGeneration Hook
```typescript
const {
  isGenerating,
  progress,
  status,
  error,
  generate,
  cancel,
  retry,
  reset
} = useVideoGeneration({
  onComplete: (status) => { /* ... */ },
  onError: (error) => { /* ... */ },
  onProgress: (progress, message) => { /* ... */ }
});
```

**Features**:
- Manages video generation lifecycle
- SSE connection for real-time progress
- Automatic error handling
- Retry capability

#### 2. useProjects Hook
```typescript
const {
  projects,
  total,
  isLoading,
  error,
  createProject,
  updateProject,
  deleteProject,
  duplicateProject,
  refetch,
  setFilters,
  getProjectById
} = useProjects({
  filters: { status: 'draft' },
  autoRefetch: true,
  refetchInterval: 30000
});
```

**Features**:
- React Query integration for caching
- Optimistic updates
- Automatic pagination
- Filter and sort support

#### 3. useSSE / useSSEConnection Hooks
- **Purpose**: Server-Sent Events (SSE) connection management
- **Features**:
  - Automatic reconnection with exponential backoff
  - Last-Event-ID support for resuming streams
  - Event validation with Zod schemas
  - Connection state tracking

### State Management (`Aura.Web/src/stores/`)

#### 1. Video Generation Store (`videoGenerationStore.ts`)
```typescript
const {
  activeJobs,
  jobHistory,
  isGenerating,
  startJob,
  updateJobProgress,
  completeJob,
  failJob,
  cancelJob
} = useVideoGenerationStore();
```

**Persistence**:
- Job history
- User preferences (auto-save, notifications)

#### 2. Projects Store (`projectsStore.ts`)
```typescript
const {
  projects,
  selectedProjectId,
  filters,
  sortBy,
  recentProjects,
  draftProject,
  addProject,
  updateProject,
  selectProject,
  setFilters
} = useProjectsStore();
```

**Persistence**:
- Recent projects list
- Filter preferences
- Draft auto-save
- Sort preferences

#### 3. App Store (`appStore.ts`)
```typescript
const {
  isSidebarOpen,
  notifications,
  settings,
  isOnline,
  addNotification,
  updateSettings
} = useAppStore();
```

**Persistence**:
- UI state (sidebar, theme)
- User settings
- Notifications temporarily stored

## Integration Patterns

### 1. Video Generation Flow

```typescript
// In component
import { useVideoGeneration } from '@/hooks/useVideoGeneration';
import { useVideoGenerationStore } from '@/stores/videoGenerationStore';

function VideoGenerationComponent() {
  const videoGen = useVideoGeneration({
    onComplete: (status) => {
      // Handle completion
      saveToProject(status);
    },
    onError: (error) => {
      // Show error notification
      showError(error.message);
    },
    onProgress: (progress, message) => {
      // Update UI
      updateProgressBar(progress);
    }
  });

  const handleGenerate = async () => {
    await videoGen.generate({
      brief: { /* ... */ },
      planSpec: { /* ... */ },
      voiceSpec: { /* ... */ },
      renderSpec: { /* ... */ }
    });
  };

  return (
    <div>
      <Button onClick={handleGenerate} disabled={videoGen.isGenerating}>
        Generate Video
      </Button>
      {videoGen.isGenerating && (
        <ProgressBar value={videoGen.progress} />
      )}
    </div>
  );
}
```

### 2. Project Management Flow

```typescript
import { useProjects } from '@/hooks/useProjects';
import { useProjectsStore } from '@/stores/projectsStore';

function ProjectsComponent() {
  const {
    projects,
    isLoading,
    createProject,
    updateProject,
    deleteProject
  } = useProjects({
    filters: { status: 'draft' }
  });

  const handleCreate = async () => {
    const project = await createProject({
      name: 'My Project',
      brief: { /* ... */ },
      planSpec: { /* ... */ }
    });
    
    // Optimistic update happens automatically
  };

  return (
    <div>
      {projects.map(project => (
        <ProjectCard key={project.id} project={project} />
      ))}
    </div>
  );
}
```

### 3. Error Handling

All API calls automatically handle errors through:

1. **Circuit Breaker**: Prevents cascading failures
2. **Retry Logic**: Automatic retry with exponential backoff
3. **Error Normalization**: Consistent error format
4. **User-Friendly Messages**: HTTP status code to message mapping

```typescript
// Errors are automatically caught and formatted
try {
  await generateVideo(request);
} catch (error) {
  // error.userMessage contains user-friendly message
  // error.errorCode contains app-specific error code
  // error.correlationId for debugging
}
```

## SSE Integration

### Real-Time Progress Updates

```typescript
// Hook handles SSE connection automatically
const videoGen = useVideoGeneration({
  onProgress: (progress, message) => {
    console.log(`${progress}%: ${message}`);
  }
});

// SSE events received:
// - job-status: Overall job status
// - step-progress: Individual step progress
// - job-completed: Generation complete
// - job-failed: Generation failed
// - job-cancelled: Job was cancelled
// - error: Connection or processing error
```

## Testing

### Unit Tests
- `stores/__tests__/`: Store logic tests
- `hooks/__tests__/`: Hook behavior tests
- `services/api/__tests__/`: API client tests

### Integration Tests
- `examples/IntegrationDemo.tsx`: Full integration example
- E2E tests in `Aura.E2E/`: End-to-end workflow tests

### Running Tests

```bash
# Unit tests
npm run test

# Integration tests
npm run test:integration

# E2E tests
npm run test:e2e
```

## Performance Optimizations

1. **React Query Caching**: 30s stale time for projects
2. **Request Deduplication**: Prevents duplicate API calls
3. **Optimistic Updates**: Immediate UI feedback
4. **Lazy Loading**: Code splitting for routes
5. **SSE Connection Pooling**: Reuse connections

## Security

1. **CSRF Protection**: Tokens in all POST/PUT/DELETE requests
2. **XSS Prevention**: Content sanitization
3. **Secure Cookies**: HttpOnly, Secure, SameSite flags
4. **Content Security Policy**: Strict CSP headers
5. **Correlation IDs**: Request tracking for audit

## Monitoring

### Frontend Metrics
- API call success/failure rates
- SSE connection stability
- Component render performance
- User interaction metrics

### Error Tracking
- Automatic error logging with context
- Correlation ID for request tracking
- Circuit breaker state monitoring

## Common Issues and Solutions

### Issue: SSE Connection Drops
**Solution**: Automatic reconnection with exponential backoff implemented in `useSSEConnection`

### Issue: Stale Data
**Solution**: React Query automatic refetching, configurable intervals

### Issue: Race Conditions
**Solution**: Request deduplication, optimistic updates with rollback

### Issue: Circuit Breaker Triggered
**Solution**: Manual reset via `resetCircuitBreaker()`, or wait for timeout

## API Endpoints

### Video Generation
- `POST /api/jobs` - Create video generation job
- `GET /api/jobs/{id}` - Get job status
- `GET /api/jobs/{id}/events` - SSE stream for progress
- `POST /api/jobs/{id}/cancel` - Cancel job
- `GET /api/jobs` - List all jobs

### Projects
- `GET /api/projects` - List projects (with filters)
- `GET /api/projects/{id}` - Get project
- `POST /api/projects` - Create project
- `PUT /api/projects/{id}` - Update project
- `DELETE /api/projects/{id}` - Delete project
- `POST /api/projects/{id}/duplicate` - Duplicate project
- `GET /api/projects/statistics` - Get statistics

## Example Integration

See `Aura.Web/src/examples/IntegrationDemo.tsx` for a complete working example demonstrating:
- Video generation with real-time progress
- Project creation and management
- Store integration
- Error handling
- Notification system

## Next Steps

1. Add authentication middleware
2. Implement WebSocket fallback for SSE
3. Add request batching for bulk operations
4. Implement offline mode with queue
5. Add analytics tracking
