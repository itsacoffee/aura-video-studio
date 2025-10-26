// Jobs API client with SSE support for real-time progress updates

export interface JobStep {
  name: string;
  status: 'Pending' | 'Running' | 'Succeeded' | 'Failed' | 'Skipped' | 'Canceled';
  progressPct: number;
  durationMs: number;
  errors: JobStepError[];
  startedAt?: string;
  completedAt?: string;
}

export interface JobStepError {
  code: string;
  message: string;
  remediation: string;
  details?: Record<string, unknown>;
}

export interface JobOutput {
  videoPath: string;
  sizeBytes: number;
}

export interface JobResponse {
  jobId: string;
  status: 'Queued' | 'Running' | 'Succeeded' | 'Failed' | 'Canceled';
  createdUtc: string;
  startedUtc?: string;
  endedUtc?: string;
  steps: JobStep[];
  output?: JobOutput;
  warnings: string[];
  errors: JobStepError[];
  correlationId: string;
}

export interface CreateJobRequest {
  preset?: string;
  inputs?: Record<string, unknown>;
  options?: {
    allowSkipUnavailable?: boolean;
    quality?: 'fast' | 'balanced' | 'high';
  };
  // Legacy format support
  brief?: Record<string, unknown>;
  planSpec?: Record<string, unknown>;
  voiceSpec?: Record<string, unknown>;
  renderSpec?: Record<string, unknown>;
}

export interface CreateJobResponse {
  jobId: string;
  correlationId: string;
  status?: string;
  stage?: string;
}

export type JobEventType =
  | 'step-progress'
  | 'step-status'
  | 'step-error'
  | 'job-status'
  | 'job-completed'
  | 'job-failed'
  | 'error';

export interface JobEvent {
  type: JobEventType;
  data: unknown;
}

/**
 * Create a new video generation job
 */
export async function createJob(request: CreateJobRequest): Promise<CreateJobResponse> {
  const response = await fetch('/api/jobs', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(`Failed to create job: ${response.statusText}`);
  }

  return response.json();
}

/**
 * Get job status and details
 */
export async function getJob(jobId: string): Promise<JobResponse> {
  const response = await fetch(`/api/jobs/${jobId}`);

  if (!response.ok) {
    throw new Error(`Failed to get job: ${response.statusText}`);
  }

  return response.json();
}

/**
 * Subscribe to Server-Sent Events for job progress
 */
export function subscribeToJobEvents(
  jobId: string,
  onEvent: (event: JobEvent) => void,
  onError?: (error: Error) => void
): () => void {
  const eventSource = new EventSource(`/api/jobs/${jobId}/events`);

  // Handle all event types
  const eventTypes: JobEventType[] = [
    'step-progress',
    'step-status',
    'step-error',
    'job-status',
    'job-completed',
    'job-failed',
    'error',
  ];

  eventTypes.forEach((eventType) => {
    eventSource.addEventListener(eventType, (e: MessageEvent) => {
      try {
        const data = JSON.parse(e.data);
        onEvent({ type: eventType, data });
      } catch (err) {
        console.error(`Failed to parse ${eventType} event:`, err);
      }
    });
  });

  eventSource.onerror = (err) => {
    console.error('SSE connection error:', err);
    if (onError) {
      onError(new Error('SSE connection failed'));
    }
    eventSource.close();
  };

  // Return cleanup function
  return () => {
    eventSource.close();
  };
}

/**
 * Cancel a running job
 */
export async function cancelJob(jobId: string): Promise<void> {
  const response = await fetch(`/api/jobs/${jobId}/cancel`, {
    method: 'POST',
  });

  if (!response.ok) {
    throw new Error(`Failed to cancel job: ${response.statusText}`);
  }
}

/**
 * Retry a failed job
 */
export async function retryJob(jobId: string, strategy?: string): Promise<CreateJobResponse> {
  const url = strategy
    ? `/api/jobs/${jobId}/retry?strategy=${encodeURIComponent(strategy)}`
    : `/api/jobs/${jobId}/retry`;

  const response = await fetch(url, {
    method: 'POST',
  });

  if (!response.ok) {
    throw new Error(`Failed to retry job: ${response.statusText}`);
  }

  return response.json();
}
