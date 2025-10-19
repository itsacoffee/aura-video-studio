import { create } from 'zustand';
import { apiUrl } from '../config/api';

export interface Job {
  id: string;
  stage: string;
  status: 'Queued' | 'Running' | 'Done' | 'Failed' | 'Skipped';
  percent: number;
  eta?: string;
  artifacts: JobArtifact[];
  logs: string[];
  startedAt: string;
  finishedAt?: string;
  correlationId?: string;
  errorMessage?: string;
  failureDetails?: JobFailure;
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
  polling: boolean;
  
  // Actions
  createJob: (brief: any, planSpec: any, voiceSpec: any, renderSpec: any) => Promise<string>;
  getJob: (jobId: string) => Promise<void>;
  getFailureDetails: (jobId: string) => Promise<JobFailure | null>;
  listJobs: () => Promise<void>;
  setActiveJob: (job: Job | null) => void;
  startPolling: (jobId: string) => void;
  stopPolling: () => void;
}

let pollInterval: ReturnType<typeof setInterval> | null = null;

export const useJobsStore = create<JobsState>((set, get) => ({
  activeJob: null,
  jobs: [],
  loading: false,
  polling: false,

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

      // Start polling for updates
      get().startPolling(jobId);

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
            failureDetails 
          } 
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

  startPolling: (jobId: string) => {
    if (pollInterval) {
      clearInterval(pollInterval);
    }

    set({ polling: true });

    pollInterval = setInterval(async () => {
      const state = get();
      await state.getJob(jobId);

      // Stop polling if job is done or failed
      if (state.activeJob && 
          (state.activeJob.status === 'Done' || state.activeJob.status === 'Failed')) {
        state.stopPolling();
      }
    }, 2000); // Poll every 2 seconds
  },

  stopPolling: () => {
    if (pollInterval) {
      clearInterval(pollInterval);
      pollInterval = null;
    }
    set({ polling: false });
  },
}));
