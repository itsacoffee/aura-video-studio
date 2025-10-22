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
}

export const useJobState = create<JobState>((set) => ({
  currentJobId: null,
  status: 'idle',
  progress: 0,
  message: '',

  setJob: (jobId: string) => {
    set({
      currentJobId: jobId,
      status: 'running',
      progress: 0,
      message: 'Starting job...',
    });
  },

  updateProgress: (progress: number, message: string) => {
    set({ progress, message });
  },

  setStatus: (status: JobStatus) => {
    set({ status });
  },

  clearJob: () => {
    set({
      currentJobId: null,
      status: 'idle',
      progress: 0,
      message: '',
    });
  },
}));
