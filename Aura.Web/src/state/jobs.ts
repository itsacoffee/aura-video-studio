import { create } from 'zustand';
import { apiUrl } from '../config/api';
import {
  SseClient,
  createSseClient,
  type JobStatusEvent,
  type StepProgressEvent,
  type JobCompletedEvent,
  type JobFailedEvent,
  type JobCancelledEvent,
} from '../services/api/sseClient';
import { loggingService as logger } from '../services/loggingService';

export interface Job {
  id: string;
  stage: string;
  status: 'Queued' | 'Running' | 'Done' | 'Failed' | 'Skipped' | 'Canceled';
  percent: number;
  eta?: string;
  artifacts: JobArtifact[];
  logs: string[];
  startedAt: string;
  finishedAt?: string;
  correlationId?: string;
  errorMessage?: string;
  failureDetails?: JobFailure;
  phase?: string; // Current phase: plan, tts, visuals, compose, render
  progressMessage?: string; // Latest progress message
}

export interface JobFailure {
  stage: string;
  message: string;
  correlationId: string;
  stderrSnippet?: string;
  installLogSnippet?: string;
  logPath?: string;
  suggestedActions: string[];
  errorCode?: string;
  failedAt: string;
}

export interface JobArtifact {
  name: string;
  path: string;
  type: string;
  sizeBytes: number;
  createdAt: string;
}

interface JobsState {
  // Active job being displayed
  activeJob: Job | null;

  // All jobs list
  jobs: Job[];

  // Loading states
  loading: boolean;
  streaming: boolean; // Tracks if SSE is active

  // Actions
  createJob: (
    brief: Record<string, unknown>,
    planSpec: Record<string, unknown>,
    voiceSpec: Record<string, unknown>,
    renderSpec: Record<string, unknown>
  ) => Promise<string>;
  getJob: (jobId: string) => Promise<void>;
  getFailureDetails: (jobId: string) => Promise<JobFailure | null>;
  listJobs: () => Promise<void>;
  setActiveJob: (job: Job | null) => void;
  startStreaming: (jobId: string) => void;
  stopStreaming: () => void;
  updateJobFromSse: (updates: Partial<Job>) => void;
}

let sseClient: SseClient | null = null;

export const useJobsStore = create<JobsState>((set, get) => ({
  activeJob: null,
  jobs: [],
  loading: false,
  streaming: false,

  createJob: async (brief, planSpec, voiceSpec, renderSpec) => {
    set({ loading: true });
    try {
      const response = await fetch('/api/jobs', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          brief,
          planSpec,
          voiceSpec,
          renderSpec,
        }),
      });

      if (!response.ok) {
        throw new Error('Failed to create job');
      }

      const data = await response.json();
      const jobId = data.jobId;

      // Start SSE streaming for updates
      get().startStreaming(jobId);

      return jobId;
    } catch (error) {
      console.error('Error creating job:', error);
      throw error;
    } finally {
      set({ loading: false });
    }
  },

  getJob: async (jobId: string) => {
    try {
      const response = await fetch(`/api/jobs/${jobId}`);
      if (!response.ok) {
        throw new Error('Failed to get job');
      }

      const job = await response.json();
      set({ activeJob: job });
    } catch (error) {
      console.error('Error getting job:', error);
    }
  },

  getFailureDetails: async (jobId: string) => {
    try {
      const response = await fetch(`/api/jobs/${jobId}/failure-details`);
      if (!response.ok) {
        if (response.status === 404 || response.status === 400) {
          return null;
        }
        throw new Error('Failed to get failure details');
      }

      const failureDetails: JobFailure = await response.json();

      // Update active job with failure details if it matches
      const state = get();
      if (state.activeJob && state.activeJob.id === jobId) {
        set({
          activeJob: {
            ...state.activeJob,
            failureDetails,
          },
        });
      }

      return failureDetails;
    } catch (error) {
      console.error('Error getting failure details:', error);
      return null;
    }
  },

  listJobs: async () => {
    set({ loading: true });
    try {
      const response = await fetch(apiUrl('/api/jobs'));
      if (!response.ok) {
        throw new Error('Failed to list jobs');
      }

      const data = await response.json();
      set({ jobs: data.jobs || [] });
    } catch (error) {
      console.error('Error listing jobs:', error);
    } finally {
      set({ loading: false });
    }
  },

  setActiveJob: (job: Job | null) => {
    set({ activeJob: job });
  },

  updateJobFromSse: (updates: Partial<Job>) => {
    const state = get();
    if (state.activeJob) {
      set({
        activeJob: {
          ...state.activeJob,
          ...updates,
        },
      });
    }
  },

  startStreaming: (jobId: string) => {
    // Stop any existing SSE connection
    get().stopStreaming();

    logger.debug(`Starting SSE streaming for job: ${jobId}`, 'JobsStore', 'startStreaming');
    set({ streaming: true });

    // Create new SSE client
    sseClient = createSseClient(jobId);

    // Handle job status updates
    sseClient.on('job-status', (event) => {
      const data = event.data as JobStatusEvent;
      logger.debug('Job status update', 'JobsStore', 'jobStatus', { data });
      get().updateJobFromSse({
        status: data.status as Job['status'],
        stage: data.stage,
        percent: data.percent,
      });
    });

    // Handle step progress updates
    sseClient.on('step-progress', (event) => {
      const data = event.data as StepProgressEvent;
      logger.debug('Step progress', 'JobsStore', 'stepProgress', { data });
      get().updateJobFromSse({
        stage: data.step,
        phase: data.phase,
        percent: data.progressPct,
        progressMessage: data.message,
      });
    });

    // Handle step status changes (phase transitions)
    sseClient.on('step-status', (event) => {
      const data = event.data as { step: string; status: string; phase: string };
      logger.debug('Step status', 'JobsStore', 'stepStatus', { data });
      get().updateJobFromSse({
        stage: data.step,
        phase: data.phase,
      });
    });

    // Handle job completion
    sseClient.on('job-completed', (event) => {
      const data = event.data as JobCompletedEvent;
      logger.debug('Job completed', 'JobsStore', 'jobCompleted', { data });
      get().updateJobFromSse({
        status: 'Done',
        percent: 100,
        artifacts: data.artifacts.map((a) => ({
          name: a.name,
          path: a.path,
          type: a.type,
          sizeBytes: a.sizeBytes,
          createdAt: new Date().toISOString(),
        })),
        finishedAt: new Date().toISOString(),
      });
      get().stopStreaming();
    });

    // Handle job failure
    sseClient.on('job-failed', (event) => {
      const data = event.data as JobFailedEvent;
      logger.error(
        'Job failed',
        new Error(data.errorMessage || 'Job failed'),
        'JobsStore',
        'jobFailed',
        { data }
      );
      get().updateJobFromSse({
        status: 'Failed',
        errorMessage: data.errorMessage || data.errors[0]?.message || 'Job failed',
        logs: data.logs || [],
        finishedAt: new Date().toISOString(),
      });
      get().stopStreaming();
    });

    // Handle job cancellation
    sseClient.on('job-cancelled', (event) => {
      const data = event.data as JobCancelledEvent;
      logger.debug('Job cancelled', 'JobsStore', 'jobCancelled', { data });
      get().updateJobFromSse({
        status: 'Canceled',
        errorMessage: data.message,
        finishedAt: new Date().toISOString(),
      });
      get().stopStreaming();
    });

    // Handle warnings
    sseClient.on('warning', (event) => {
      const data = event.data as { message: string };
      logger.warn(data.message, 'JobsStore', 'warning');
      // Could update logs here if needed
    });

    // Handle errors
    sseClient.on('error', (event) => {
      const data = event.data as { message: string };
      logger.error('SSE error', new Error(data.message), 'JobsStore', 'sseError');
      get().updateJobFromSse({
        status: 'Failed',
        errorMessage: data.message,
      });
      get().stopStreaming();
    });

    // Connect to SSE endpoint
    sseClient.connect();
  },

  stopStreaming: () => {
    if (sseClient) {
      logger.debug('Stopping SSE streaming', 'JobsStore', 'stopStreaming');
      sseClient.close();
      sseClient = null;
    }
    set({ streaming: false });
  },
}));
