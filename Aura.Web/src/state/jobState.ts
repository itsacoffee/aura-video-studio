import { create } from 'zustand';

export type JobStatus = 'idle' | 'running' | 'completed' | 'failed';

export interface JobState {
  currentJobId: string | null;
  status: JobStatus;
  progress: number;
  message: string;
  setJob: (jobId: string) => void;
  updateProgress: (progress: number, message: string) => void;
  setStatus: (status: JobStatus) => void;
  clearJob: () => void;
  canStartNewJob: () => boolean;
}

// Valid state transitions to prevent invalid operations
const VALID_TRANSITIONS: Record<JobStatus, JobStatus[]> = {
  idle: ['running'],
  running: ['completed', 'failed'],
  completed: ['idle'],
  failed: ['idle'],
};

export const useJobState = create<JobState>((set, get) => ({
  currentJobId: null,
  status: 'idle',
  progress: 0,
  message: '',

  setJob: (jobId: string) => {
    const currentStatus = get().status;

    // Prevent starting a new job if one is already running
    if (currentStatus === 'running') {
      console.warn('Cannot start new job: a job is already running');
      return;
    }

    set({
      currentJobId: jobId,
      status: 'running',
      progress: 0,
      message: 'Starting job...',
    });
  },

  updateProgress: (progress: number, message: string) => {
    const currentStatus = get().status;

    // Only update progress if job is running
    if (currentStatus !== 'running') {
      console.warn(`Cannot update progress: job is ${currentStatus}`);
      return;
    }

    set({ progress, message });
  },

  setStatus: (status: JobStatus) => {
    const currentStatus = get().status;

    // Validate state transition
    if (!VALID_TRANSITIONS[currentStatus]?.includes(status)) {
      console.warn(`Invalid state transition from ${currentStatus} to ${status}`);
      return;
    }

    set({ status });
  },

  clearJob: () => {
    const currentStatus = get().status;

    // Only allow clearing if job is completed, failed, or idle
    if (currentStatus === 'running') {
      console.warn('Cannot clear job while it is still running');
      return;
    }

    set({
      currentJobId: null,
      status: 'idle',
      progress: 0,
      message: '',
    });
  },

  canStartNewJob: () => {
    const currentStatus = get().status;
    return currentStatus === 'idle' || currentStatus === 'completed' || currentStatus === 'failed';
  },
}));
