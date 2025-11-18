/**
 * Video Generation Store
 * Zustand store for managing video generation state with persistence
 */

import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import type { VideoStatus } from '../services/api/videoApi';

export interface VideoGenerationJob {
  jobId: string;
  status: VideoStatus['status'];
  progress: number;
  stage: string;
  outputPath?: string;
  errorMessage?: string;
  createdAt: string;
  updatedAt: string;
  request: {
    topic: string;
    brief: Record<string, unknown>;
    planSpec: Record<string, unknown>;
  };
}

export interface VideoGenerationState {
  // Current active jobs
  activeJobs: Map<string, VideoGenerationJob>;

  // Job history
  jobHistory: VideoGenerationJob[];
  maxHistorySize: number;

  // UI state
  isGenerating: boolean;
  currentJobId: string | null;

  // Preferences
  autoSaveProjects: boolean;
  showProgressNotifications: boolean;

  // Actions
  startJob: (jobId: string, request: VideoGenerationJob['request']) => void;
  updateJobProgress: (jobId: string, progress: number, stage: string) => void;
  updateJobStatus: (jobId: string, status: VideoStatus) => void;
  completeJob: (jobId: string, outputPath: string) => void;
  failJob: (jobId: string, errorMessage: string) => void;
  cancelJob: (jobId: string) => void;
  clearJob: (jobId: string) => void;
  clearHistory: () => void;
  setCurrentJob: (jobId: string | null) => void;

  // Preferences
  setAutoSaveProjects: (enabled: boolean) => void;
  setShowProgressNotifications: (enabled: boolean) => void;
}

export const useVideoGenerationStore = create<VideoGenerationState>()(
  persist(
    (set, _get) => ({
      // Initial state
      activeJobs: new Map(),
      jobHistory: [],
      maxHistorySize: 50,
      isGenerating: false,
      currentJobId: null,
      autoSaveProjects: true,
      showProgressNotifications: true,

      // Start a new job
      startJob: (jobId, request) => {
        const newJob: VideoGenerationJob = {
          jobId,
          status: 'Queued',
          progress: 0,
          stage: 'Initializing',
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          request,
        };

        set((state) => {
          const newActiveJobs = new Map(state.activeJobs);
          newActiveJobs.set(jobId, newJob);

          return {
            activeJobs: newActiveJobs,
            isGenerating: true,
            currentJobId: jobId,
          };
        });
      },

      // Update job progress
      updateJobProgress: (jobId, progress, stage) => {
        set((state) => {
          const job = state.activeJobs.get(jobId);
          if (!job) return state;

          const updatedJob = {
            ...job,
            progress,
            stage,
            status: 'Running' as const,
            updatedAt: new Date().toISOString(),
          };

          const newActiveJobs = new Map(state.activeJobs);
          newActiveJobs.set(jobId, updatedJob);

          return { activeJobs: newActiveJobs };
        });
      },

      // Update job status
      updateJobStatus: (jobId, status) => {
        set((state) => {
          const job = state.activeJobs.get(jobId);
          if (!job) return state;

          const updatedJob = {
            ...job,
            status: status.status,
            progress: status.percent,
            stage: status.stage,
            outputPath: status.outputPath,
            errorMessage: status.errorMessage,
            updatedAt: new Date().toISOString(),
          };

          const newActiveJobs = new Map(state.activeJobs);
          newActiveJobs.set(jobId, updatedJob);

          return { activeJobs: newActiveJobs };
        });
      },

      // Complete a job successfully
      completeJob: (jobId, outputPath) => {
        set((state) => {
          const job = state.activeJobs.get(jobId);
          if (!job) return state;

          const completedJob = {
            ...job,
            status: 'Done' as const,
            progress: 100,
            outputPath,
            updatedAt: new Date().toISOString(),
          };

          // Move to history
          const newHistory = [completedJob, ...state.jobHistory];
          if (newHistory.length > state.maxHistorySize) {
            newHistory.pop();
          }

          const newActiveJobs = new Map(state.activeJobs);
          newActiveJobs.delete(jobId);

          return {
            activeJobs: newActiveJobs,
            jobHistory: newHistory,
            isGenerating: newActiveJobs.size > 0,
            currentJobId: state.currentJobId === jobId ? null : state.currentJobId,
          };
        });
      },

      // Fail a job
      failJob: (jobId, errorMessage) => {
        set((state) => {
          const job = state.activeJobs.get(jobId);
          if (!job) return state;

          const failedJob = {
            ...job,
            status: 'Failed' as const,
            errorMessage,
            updatedAt: new Date().toISOString(),
          };

          // Move to history
          const newHistory = [failedJob, ...state.jobHistory];
          if (newHistory.length > state.maxHistorySize) {
            newHistory.pop();
          }

          const newActiveJobs = new Map(state.activeJobs);
          newActiveJobs.delete(jobId);

          return {
            activeJobs: newActiveJobs,
            jobHistory: newHistory,
            isGenerating: newActiveJobs.size > 0,
            currentJobId: state.currentJobId === jobId ? null : state.currentJobId,
          };
        });
      },

      // Cancel a job
      cancelJob: (jobId) => {
        set((state) => {
          const job = state.activeJobs.get(jobId);
          if (!job) return state;

          const canceledJob = {
            ...job,
            status: 'Canceled' as const,
            updatedAt: new Date().toISOString(),
          };

          // Move to history
          const newHistory = [canceledJob, ...state.jobHistory];
          if (newHistory.length > state.maxHistorySize) {
            newHistory.pop();
          }

          const newActiveJobs = new Map(state.activeJobs);
          newActiveJobs.delete(jobId);

          return {
            activeJobs: newActiveJobs,
            jobHistory: newHistory,
            isGenerating: newActiveJobs.size > 0,
            currentJobId: state.currentJobId === jobId ? null : state.currentJobId,
          };
        });
      },

      // Clear a specific job from active jobs
      clearJob: (jobId) => {
        set((state) => {
          const newActiveJobs = new Map(state.activeJobs);
          newActiveJobs.delete(jobId);

          return {
            activeJobs: newActiveJobs,
            isGenerating: newActiveJobs.size > 0,
            currentJobId: state.currentJobId === jobId ? null : state.currentJobId,
          };
        });
      },

      // Clear job history
      clearHistory: () => {
        set({ jobHistory: [] });
      },

      // Set current active job
      setCurrentJob: (jobId) => {
        set({ currentJobId: jobId });
      },

      // Preferences
      setAutoSaveProjects: (enabled) => {
        set({ autoSaveProjects: enabled });
      },

      setShowProgressNotifications: (enabled) => {
        set({ showProgressNotifications: enabled });
      },
    }),
    {
      name: 'video-generation-store',
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({
        jobHistory: state.jobHistory,
        autoSaveProjects: state.autoSaveProjects,
        showProgressNotifications: state.showProgressNotifications,
        // Don't persist active jobs - they should be reloaded from server on app start
      }),
    }
  )
);
