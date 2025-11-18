/**
 * Job Queue Store
 * Zustand store for managing background job queue state
 */

import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import type {
  JobQueueItem,
  QueueStatistics,
  QueueConfiguration,
} from '../services/jobQueueService';

export interface JobQueueState {
  // Jobs
  jobs: JobQueueItem[];
  activeJobIds: Set<string>;

  // Statistics
  statistics: QueueStatistics | null;

  // Configuration
  configuration: QueueConfiguration | null;

  // UI State
  isConnected: boolean;
  isLoadingJobs: boolean;
  error: string | null;

  // Filters
  statusFilter: string | null;

  // Actions
  setJobs: (jobs: JobQueueItem[]) => void;
  addJob: (job: JobQueueItem) => void;
  updateJob: (jobId: string, updates: Partial<JobQueueItem>) => void;
  removeJob: (jobId: string) => void;
  clearCompletedJobs: () => void;

  setStatistics: (stats: QueueStatistics) => void;
  setConfiguration: (config: QueueConfiguration) => void;

  setConnected: (connected: boolean) => void;
  setLoadingJobs: (loading: boolean) => void;
  setError: (error: string | null) => void;

  setStatusFilter: (status: string | null) => void;

  // Computed
  getJobById: (jobId: string) => JobQueueItem | undefined;
  getPendingJobs: () => JobQueueItem[];
  getProcessingJobs: () => JobQueueItem[];
  getCompletedJobs: () => JobQueueItem[];
  getFailedJobs: () => JobQueueItem[];
}

export const useJobQueueStore = create<JobQueueState>()(
  persist(
    (set, get) => ({
      // Initial state
      jobs: [],
      activeJobIds: new Set(),
      statistics: null,
      configuration: null,
      isConnected: false,
      isLoadingJobs: false,
      error: null,
      statusFilter: null,

      // Set all jobs
      setJobs: (jobs) => {
        set({
          jobs,
          activeJobIds: new Set(
            jobs
              .filter((j) => j.status === 'Processing' || j.status === 'Pending')
              .map((j) => j.jobId)
          ),
        });
      },

      // Add a new job
      addJob: (job) => {
        set((state) => {
          const existingJobIndex = state.jobs.findIndex((j) => j.jobId === job.jobId);

          if (existingJobIndex !== -1) {
            // Update existing job
            const newJobs = [...state.jobs];
            newJobs[existingJobIndex] = job;
            return { jobs: newJobs };
          } else {
            // Add new job at the beginning
            return { jobs: [job, ...state.jobs] };
          }
        });
      },

      // Update a job
      updateJob: (jobId, updates) => {
        set((state) => {
          const jobIndex = state.jobs.findIndex((j) => j.jobId === jobId);
          if (jobIndex === -1) return state;

          const updatedJob = { ...state.jobs[jobIndex], ...updates };
          const newJobs = [...state.jobs];
          newJobs[jobIndex] = updatedJob;

          // Update active job IDs
          const newActiveJobIds = new Set(state.activeJobIds);
          if (updatedJob.status === 'Processing' || updatedJob.status === 'Pending') {
            newActiveJobIds.add(jobId);
          } else {
            newActiveJobIds.delete(jobId);
          }

          return {
            jobs: newJobs,
            activeJobIds: newActiveJobIds,
          };
        });
      },

      // Remove a job
      removeJob: (jobId) => {
        set((state) => {
          const newActiveJobIds = new Set(state.activeJobIds);
          newActiveJobIds.delete(jobId);

          return {
            jobs: state.jobs.filter((j) => j.jobId !== jobId),
            activeJobIds: newActiveJobIds,
          };
        });
      },

      // Clear completed jobs
      clearCompletedJobs: () => {
        set((state) => ({
          jobs: state.jobs.filter((j) => j.status !== 'Completed'),
        }));
      },

      // Set statistics
      setStatistics: (statistics) => {
        set({ statistics });
      },

      // Set configuration
      setConfiguration: (configuration) => {
        set({ configuration });
      },

      // Set connection status
      setConnected: (isConnected) => {
        set({ isConnected });
      },

      // Set loading state
      setLoadingJobs: (isLoadingJobs) => {
        set({ isLoadingJobs });
      },

      // Set error
      setError: (error) => {
        set({ error });
      },

      // Set status filter
      setStatusFilter: (statusFilter) => {
        set({ statusFilter });
      },

      // Get job by ID
      getJobById: (jobId) => {
        return get().jobs.find((j) => j.jobId === jobId);
      },

      // Get pending jobs
      getPendingJobs: () => {
        return get().jobs.filter((j) => j.status === 'Pending');
      },

      // Get processing jobs
      getProcessingJobs: () => {
        return get().jobs.filter((j) => j.status === 'Processing');
      },

      // Get completed jobs
      getCompletedJobs: () => {
        return get().jobs.filter((j) => j.status === 'Completed');
      },

      // Get failed jobs
      getFailedJobs: () => {
        return get().jobs.filter((j) => j.status === 'Failed');
      },
    }),
    {
      name: 'job-queue-store',
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({
        // Only persist configuration preferences, not the actual jobs
        statusFilter: state.statusFilter,
        // Jobs will be reloaded from the server on app start
      }),
    }
  )
);
