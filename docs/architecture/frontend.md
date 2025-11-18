# Frontend Architecture

Comprehensive guide to the Aura Video Studio frontend architecture.

## Overview

The frontend is built with modern React and TypeScript, bundled with Vite, and runs inside Electron as the renderer process.

**Technology Stack:**
- React 18.2.0+ with TypeScript 5.3.3 (strict mode)
- Vite 6.4.1 for build tooling and HMR
- Fluent UI React Components 9.47.0 for UI
- Zustand 5.0.8 for state management
- React Router 6.21.0 for routing
- Axios 1.6.5 for HTTP client

## Project Structure

```
Aura.Web/
├── public/                 # Static assets
├── src/
│   ├── components/         # Reusable UI components
│   │   ├── common/        # Shared components (Button, Card, etc.)
│   │   ├── layout/        # Layout components (Header, Sidebar)
│   │   └── features/      # Feature-specific components
│   ├── pages/             # Route components (page-level)
│   ├── services/          # API clients and external services
│   │   └── api/           # API client and utilities
│   ├── stores/            # Zustand state stores
│   ├── hooks/             # Custom React hooks
│   ├── types/             # TypeScript type definitions
│   ├── utils/             # Utility functions
│   ├── styles/            # Global styles and themes
│   ├── App.tsx            # Root application component
│   └── main.tsx           # Application entry point
├── tests/                 # Test files
│   ├── unit/
│   ├── integration/
│   └── e2e/
├── package.json
├── tsconfig.json          # TypeScript configuration (strict mode)
├── vite.config.ts         # Vite configuration
└── playwright.config.ts   # E2E test configuration
```

## State Management (Zustand)

### Store Pattern

Each store focuses on a single domain with clear boundaries:

```typescript
// src/stores/jobStore.ts
import { create } from 'zustand';

interface Job {
  id: string;
  status: 'Queued' | 'Running' | 'Completed' | 'Failed';
  progress: number;
}

interface JobState {
  // State
  jobs: Job[];
  selectedJobId: string | null;
  isLoading: boolean;
  
  // Actions
  addJob: (job: Job) => void;
  updateJob: (id: string, updates: Partial<Job>) => void;
  removeJob: (id: string) => void;
  selectJob: (id: string | null) => void;
  loadJobs: () => Promise<void>;
}

export const useJobStore = create<JobState>((set, get) => ({
  // Initial state
  jobs: [],
  selectedJobId: null,
  isLoading: false,
  
  // Actions
  addJob: (job) => set((state) => ({
    jobs: [...state.jobs, job]
  })),
  
  updateJob: (id, updates) => set((state) => ({
    jobs: state.jobs.map(job => 
      job.id === id ? { ...job, ...updates } : job
    )
  })),
  
  removeJob: (id) => set((state) => ({
    jobs: state.jobs.filter(job => job.id !== id),
    selectedJobId: state.selectedJobId === id ? null : state.selectedJobId
  })),
  
  selectJob: (id) => set({ selectedJobId: id }),
  
  loadJobs: async () => {
    set({ isLoading: true });
    try {
      const jobs = await apiClient.get<Job[]>('/api/jobs');
      set({ jobs: jobs.data, isLoading: false });
    } catch (error) {
      console.error('Failed to load jobs:', error);
      set({ isLoading: false });
    }
  }
}));
```

### Using Stores in Components

```typescript
import { useJobStore } from '@/stores/jobStore';

function JobList() {
  const jobs = useJobStore(state => state.jobs);
  const isLoading = useJobStore(state => state.isLoading);
  const loadJobs = useJobStore(state => state.loadJobs);
  
  useEffect(() => {
    loadJobs();
  }, [loadJobs]);
  
  if (isLoading) return <Spinner />;
  
  return (
    <div>
      {jobs.map(job => (
        <JobCard key={job.id} job={job} />
      ))}
    </div>
  );
}
```

### Store Organization

**Existing Stores:**
- `onboarding.ts` - Wizard state and navigation
- `render.ts` - Video rendering state and progress
- `settings.ts` - Application settings
- `jobs.ts` - Job queue and status
- `timeline.ts` - Timeline editing state
- `engines.ts` - Engine configuration

## Routing (React Router 6)

### Route Configuration

```typescript
// src/App.tsx
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Layout />}>
          <Route index element={<HomePage />} />
          <Route path="create" element={<CreateWizard />} />
          <Route path="jobs" element={<JobsPage />} />
          <Route path="jobs/:id" element={<JobDetailPage />} />
          <Route path="timeline/:id" element={<TimelineEditor />} />
          <Route path="settings" element={<SettingsPage />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
```

### Protected Routes

```typescript
interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredConfig?: string[];
}

function ProtectedRoute({ children, requiredConfig }: ProtectedRouteProps) {
  const settings = useSettingsStore(state => state.settings);
  const navigate = useNavigate();
  
  useEffect(() => {
    if (requiredConfig && !checkConfig(settings, requiredConfig)) {
      navigate('/settings');
    }
  }, [settings, requiredConfig, navigate]);
  
  return <>{children}</>;
}

// Usage
<Route 
  path="create" 
  element={
    <ProtectedRoute requiredConfig={['llmProvider', 'ttsProvider']}>
      <CreateWizard />
    </ProtectedRoute>
  } 
/>
```

### Navigation

```typescript
import { useNavigate } from 'react-router-dom';

function MyComponent() {
  const navigate = useNavigate();
  
  const handleCreate = () => {
    navigate('/create');
  };
  
  const handleJobDetail = (jobId: string) => {
    navigate(`/jobs/${jobId}`);
  };
  
  return (
    <div>
      <Button onClick={handleCreate}>Create Video</Button>
    </div>
  );
}
```

## API Client Pattern

### Central API Client

```typescript
// src/services/api/apiClient.ts
import axios, { AxiosInstance, AxiosError } from 'axios';

const BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5005';

// Circuit breaker state
let failureCount = 0;
let circuitOpen = false;
let lastFailureTime = 0;

const FAILURE_THRESHOLD = 5;
const CIRCUIT_TIMEOUT = 30000; // 30 seconds

export const apiClient: AxiosInstance = axios.create({
  baseURL: BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json'
  }
});

// Request interceptor - add correlation ID
apiClient.interceptors.request.use((config) => {
  // Check circuit breaker
  if (circuitOpen) {
    const now = Date.now();
    if (now - lastFailureTime > CIRCUIT_TIMEOUT) {
      circuitOpen = false;
      failureCount = 0;
    } else {
      return Promise.reject(new Error('Circuit breaker is open'));
    }
  }
  
  // Add correlation ID
  config.headers['X-Correlation-ID'] = crypto.randomUUID();
  return config;
});

// Response interceptor - handle errors
apiClient.interceptors.response.use(
  (response) => {
    // Reset failure count on success
    failureCount = 0;
    return response;
  },
  (error: AxiosError) => {
    // Increment failure count
    failureCount++;
    lastFailureTime = Date.now();
    
    // Open circuit if threshold reached
    if (failureCount >= FAILURE_THRESHOLD) {
      circuitOpen = true;
    }
    
    return Promise.reject(error);
  }
);
```

### Error Handling

```typescript
// src/services/api/apiError.ts
import { AxiosError } from 'axios';

export interface ApiError {
  type: string;
  title: string;
  status: number;
  detail: string;
  correlationId?: string;
}

export function parseApiError(error: unknown): ApiError {
  if (axios.isAxiosError(error)) {
    const axiosError = error as AxiosError<ApiError>;
    
    if (axiosError.response?.data) {
      return axiosError.response.data;
    }
    
    return {
      type: 'NetworkError',
      title: 'Network Error',
      status: 0,
      detail: axiosError.message || 'An unknown network error occurred'
    };
  }
  
  const errorObj = error instanceof Error ? error : new Error(String(error));
  return {
    type: 'UnknownError',
    title: 'Unknown Error',
    status: 0,
    detail: errorObj.message
  };
}
```

### API Service Pattern

```typescript
// src/services/api/jobService.ts
import { apiClient } from './apiClient';
import { parseApiError } from './apiError';
import type { CreateJobRequest, JobResponse } from '@/types/api-v1';

export const jobService = {
  async create(request: CreateJobRequest): Promise<JobResponse> {
    try {
      const response = await apiClient.post<JobResponse>('/api/jobs', request);
      return response.data;
    } catch (error: unknown) {
      const apiError = parseApiError(error);
      console.error('Failed to create job:', apiError.detail);
      throw apiError;
    }
  },
  
  async get(id: string): Promise<JobResponse> {
    try {
      const response = await apiClient.get<JobResponse>(`/api/jobs/${id}`);
      return response.data;
    } catch (error: unknown) {
      const apiError = parseApiError(error);
      throw apiError;
    }
  },
  
  async list(page = 1, pageSize = 20): Promise<JobResponse[]> {
    try {
      const response = await apiClient.get<JobResponse[]>('/api/jobs', {
        params: { page, pageSize }
      });
      return response.data;
    } catch (error: unknown) {
      const apiError = parseApiError(error);
      throw apiError;
    }
  },
  
  async cancel(id: string): Promise<void> {
    try {
      await apiClient.delete(`/api/jobs/${id}`);
    } catch (error: unknown) {
      const apiError = parseApiError(error);
      throw apiError;
    }
  }
};
```

## Component Patterns

### Functional Components with TypeScript

```typescript
import React, { useState, useCallback, useMemo } from 'react';
import type { FC } from 'react';

interface JobCardProps {
  job: Job;
  onSelect?: (id: string) => void;
  onCancel?: (id: string) => void;
  disabled?: boolean;
}

const JobCard: FC<JobCardProps> = ({ 
  job, 
  onSelect, 
  onCancel, 
  disabled = false 
}) => {
  const [isHovered, setIsHovered] = useState(false);
  
  const handleSelect = useCallback(() => {
    onSelect?.(job.id);
  }, [job.id, onSelect]);
  
  const handleCancel = useCallback(() => {
    onCancel?.(job.id);
  }, [job.id, onCancel]);
  
  const statusColor = useMemo(() => {
    switch (job.status) {
      case 'Completed': return 'success';
      case 'Failed': return 'danger';
      case 'Running': return 'warning';
      default: return 'neutral';
    }
  }, [job.status]);
  
  return (
    <Card
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      onClick={handleSelect}
    >
      <CardHeader>
        <Text weight="semibold">{job.id}</Text>
        <Badge color={statusColor}>{job.status}</Badge>
      </CardHeader>
      <CardBody>
        <ProgressBar value={job.progress} />
      </CardBody>
      {isHovered && job.status === 'Running' && (
        <CardFooter>
          <Button 
            onClick={handleCancel} 
            disabled={disabled}
          >
            Cancel
          </Button>
        </CardFooter>
      )}
    </Card>
  );
};

export default JobCard;
```

### Custom Hooks

```typescript
// src/hooks/useJobProgress.ts
import { useState, useEffect } from 'react';

export function useJobProgress(jobId: string) {
  const [progress, setProgress] = useState(0);
  const [stage, setStage] = useState('');
  
  useEffect(() => {
    const eventSource = new EventSource(`/api/jobs/${jobId}/events`);
    
    eventSource.addEventListener('step-progress', (event) => {
      const data = JSON.parse(event.data);
      setProgress(data.progress);
      setStage(data.stage);
    });
    
    eventSource.addEventListener('job-completed', () => {
      setProgress(100);
      eventSource.close();
    });
    
    return () => {
      eventSource.close();
    };
  }, [jobId]);
  
  return { progress, stage };
}

// Usage
function JobProgressBar({ jobId }: { jobId: string }) {
  const { progress, stage } = useJobProgress(jobId);
  
  return (
    <div>
      <Text>{stage}</Text>
      <ProgressBar value={progress} />
    </div>
  );
}
```

## Performance Optimization

### Memoization

```typescript
// Expensive calculations
const sortedJobs = useMemo(() => {
  return jobs.sort((a, b) => 
    new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
  );
}, [jobs]);

// Callback stability
const handleJobUpdate = useCallback((id: string, updates: Partial<Job>) => {
  updateJob(id, updates);
}, [updateJob]);
```

### Component Memoization

```typescript
import React, { memo } from 'react';

const JobCard = memo<JobCardProps>(({ job, onSelect }) => {
  return (
    <Card>
      {/* Component content */}
    </Card>
  );
}, (prevProps, nextProps) => {
  // Custom comparison
  return prevProps.job.id === nextProps.job.id &&
         prevProps.job.status === nextProps.job.status;
});
```

### Virtualization

For long lists, use react-virtuoso or react-window:

```typescript
import { Virtuoso } from 'react-virtuoso';

function JobList({ jobs }: { jobs: Job[] }) {
  return (
    <Virtuoso
      style={{ height: '600px' }}
      data={jobs}
      itemContent={(index, job) => (
        <JobCard key={job.id} job={job} />
      )}
    />
  );
}
```

## TypeScript Best Practices

### Strict Mode

All code uses TypeScript strict mode:

```json
{
  "compilerOptions": {
    "strict": true,
    "noUncheckedIndexedAccess": true,
    "noImplicitAny": true
  }
}
```

### Type Safety

```typescript
// ✅ Good - explicit types
function processJob(job: Job): void {
  console.log(job.id);
}

// ❌ Bad - implicit any
function processJob(job) {  // Error in strict mode
  console.log(job.id);
}

// ✅ Good - error handling
try {
  await fetchData();
} catch (error: unknown) {
  const errorObj = error instanceof Error ? error : new Error(String(error));
  console.error(errorObj.message);
}

// ❌ Bad - any in catch
try {
  await fetchData();
} catch (error: any) {  // Forbidden
  console.error(error.message);
}
```

## Additional Resources

- [Testing Guide](../development/testing.md)
- [API Integration Guide](../development/FRONTEND_API_INTEGRATION_GUIDE.md)
- [TypeScript Guidelines](../TYPESCRIPT_GUIDELINES.md)
- [Performance Optimization](../development/PERFORMANCE_OPTIMIZATION_GUIDE.md)

---

Last updated: 2025-11-18
