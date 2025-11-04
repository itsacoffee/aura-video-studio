import { create } from 'zustand';
import type {
  AnnotationItemDto,
  AnnotationStatsDto,
  TrainingJobStatusDto,
  StartTrainingResponse,
} from '../types/api-v1';

/**
 * Frame with annotation state
 */
export interface AnnotatedFrame {
  id: string;
  framePath: string;
  videoPath: string;
  thumbnailUrl?: string;
  rating?: number;
  timestamp: number;
}

/**
 * Video with annotation progress
 */
export interface AnnotatedVideo {
  path: string;
  name: string;
  framesExtracted: number;
  framesAnnotated: number;
  frames: AnnotatedFrame[];
}

/**
 * Training configuration
 */
export interface TrainingConfig {
  modelName?: string;
  datasetSizeCap?: number;
  epochsPreset?: 'quick' | 'balanced' | 'thorough';
}

/**
 * System capabilities for preflight check
 */
export interface SystemCapabilities {
  hasGPU: boolean;
  gpuName?: string;
  totalRAM: number;
  availableRAM: number;
  availableDiskSpace: number;
  meetsMinimumRequirements: boolean;
  warnings: string[];
}

interface MLLabState {
  // Annotation state
  videos: AnnotatedVideo[];
  selectedVideoPath?: string;
  currentFrameIndex: number;
  annotationStats?: AnnotationStatsDto;
  isLoadingStats: boolean;
  isSavingAnnotations: boolean;

  // Training state
  activeTrainingJob?: TrainingJobStatusDto;
  trainingHistory: TrainingJobStatusDto[];
  isStartingTraining: boolean;
  trainingConfig: TrainingConfig;

  // System state
  systemCapabilities?: SystemCapabilities;
  isCheckingSystem: boolean;

  // UI state
  currentTab: 'annotation' | 'training';
  error?: string;

  // Annotation actions
  setCurrentTab: (tab: 'annotation' | 'training') => void;
  addVideo: (video: AnnotatedVideo) => void;
  removeVideo: (videoPath: string) => void;
  selectVideo: (videoPath: string) => void;
  setFrames: (videoPath: string, frames: AnnotatedFrame[]) => void;
  rateFrame: (videoPath: string, frameId: string, rating: number) => void;
  setCurrentFrameIndex: (index: number) => void;
  loadAnnotationStats: () => Promise<void>;
  saveAnnotations: () => Promise<void>;

  // Training actions
  updateTrainingConfig: (config: Partial<TrainingConfig>) => void;
  startTraining: () => Promise<StartTrainingResponse>;
  updateTrainingJobStatus: (status: TrainingJobStatusDto) => void;
  cancelTraining: (jobId: string) => Promise<void>;
  revertToDefaultModel: () => Promise<void>;

  // System actions
  checkSystemCapabilities: () => Promise<void>;

  // Reset
  reset: () => void;
}

const initialState = {
  videos: [],
  currentFrameIndex: 0,
  isLoadingStats: false,
  isSavingAnnotations: false,
  trainingHistory: [],
  isStartingTraining: false,
  trainingConfig: {},
  isCheckingSystem: false,
  currentTab: 'annotation' as const,
};

export const useMLLabStore = create<MLLabState>((set, get) => ({
  ...initialState,

  // Annotation actions
  setCurrentTab: (tab) => set({ currentTab: tab }),

  addVideo: (video) =>
    set((state) => ({
      videos: [...state.videos, video],
      selectedVideoPath: state.selectedVideoPath || video.path,
    })),

  removeVideo: (videoPath) =>
    set((state) => ({
      videos: state.videos.filter((v) => v.path !== videoPath),
      selectedVideoPath:
        state.selectedVideoPath === videoPath ? state.videos[0]?.path : state.selectedVideoPath,
    })),

  selectVideo: (videoPath) => set({ selectedVideoPath: videoPath, currentFrameIndex: 0 }),

  setFrames: (videoPath, frames) =>
    set((state) => ({
      videos: state.videos.map((v) => (v.path === videoPath ? { ...v, frames } : v)),
    })),

  rateFrame: (videoPath, frameId, rating) =>
    set((state) => ({
      videos: state.videos.map((v) =>
        v.path === videoPath
          ? {
              ...v,
              frames: v.frames.map((f) => (f.id === frameId ? { ...f, rating } : f)),
              framesAnnotated: v.frames.filter((f) => f.id === frameId || f.rating !== undefined)
                .length,
            }
          : v
      ),
    })),

  setCurrentFrameIndex: (index) => set({ currentFrameIndex: index }),

  loadAnnotationStats: async () => {
    set({ isLoadingStats: true, error: undefined });
    try {
      const response = await fetch('/api/ml/annotations/stats');
      if (!response.ok) {
        throw new Error('Failed to load annotation stats');
      }
      const stats = await response.json();
      set({ annotationStats: stats, isLoadingStats: false });
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isLoadingStats: false });
    }
  },

  saveAnnotations: async () => {
    set({ isSavingAnnotations: true, error: undefined });
    try {
      const state = get();
      const allAnnotations: AnnotationItemDto[] = [];

      // Collect all rated frames from all videos
      state.videos.forEach((video) => {
        video.frames.forEach((frame) => {
          if (frame.rating !== undefined) {
            allAnnotations.push({
              framePath: frame.framePath,
              rating: frame.rating,
              metadata: {
                videoPath: video.path,
                timestamp: frame.timestamp.toString(),
              },
            });
          }
        });
      });

      if (allAnnotations.length === 0) {
        throw new Error('No annotations to save');
      }

      const response = await fetch('/api/ml/annotations/upload', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ annotations: allAnnotations }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.detail || 'Failed to save annotations');
      }

      // Reload stats after successful save
      await get().loadAnnotationStats();

      set({ isSavingAnnotations: false });
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isSavingAnnotations: false });
      throw errorObj;
    }
  },

  // Training actions
  updateTrainingConfig: (config) =>
    set((state) => ({
      trainingConfig: { ...state.trainingConfig, ...config },
    })),

  startTraining: async () => {
    set({ isStartingTraining: true, error: undefined });
    try {
      const state = get();
      const response = await fetch('/api/ml/train/frame-importance', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          modelName: state.trainingConfig.modelName,
          pipelineConfig: state.trainingConfig.epochsPreset
            ? { epochsPreset: state.trainingConfig.epochsPreset }
            : undefined,
        }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.detail || 'Failed to start training');
      }

      const result: StartTrainingResponse = await response.json();

      // Start polling for job status
      const pollStatus = async () => {
        try {
          const statusResponse = await fetch(`/api/ml/train/${result.jobId}/status`);
          if (statusResponse.ok) {
            const status: TrainingJobStatusDto = await statusResponse.json();
            get().updateTrainingJobStatus(status);

            // Continue polling if job is still running
            if (status.state === 'Running' || status.state === 'Queued') {
              setTimeout(pollStatus, 2000);
            }
          }
        } catch (error) {
          console.error('Failed to poll training status:', error);
        }
      };

      // Start polling
      setTimeout(pollStatus, 1000);

      set({ isStartingTraining: false });
      return result;
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isStartingTraining: false });
      throw errorObj;
    }
  },

  updateTrainingJobStatus: (status) =>
    set((state) => {
      const isNewJob = !state.activeTrainingJob || state.activeTrainingJob.jobId !== status.jobId;

      // If job is completed/failed/cancelled, move to history
      if (['Completed', 'Failed', 'Cancelled'].includes(status.state)) {
        const updatedHistory = isNewJob
          ? [...state.trainingHistory, status]
          : state.trainingHistory.map((job) => (job.jobId === status.jobId ? status : job));

        return {
          activeTrainingJob: undefined,
          trainingHistory: updatedHistory,
        };
      }

      // Otherwise, update active job
      return { activeTrainingJob: status };
    }),

  cancelTraining: async (jobId) => {
    set({ error: undefined });
    try {
      const response = await fetch(`/api/ml/train/${jobId}/cancel`, {
        method: 'POST',
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.detail || 'Failed to cancel training');
      }
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message });
      throw errorObj;
    }
  },

  revertToDefaultModel: async () => {
    set({ error: undefined });
    try {
      const response = await fetch('/api/ml/model/revert', {
        method: 'POST',
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.detail || 'Failed to revert to default model');
      }
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message });
      throw errorObj;
    }
  },

  // System actions
  checkSystemCapabilities: async () => {
    set({ isCheckingSystem: true, error: undefined });
    try {
      const response = await fetch('/api/health/system');
      if (!response.ok) {
        throw new Error('Failed to check system capabilities');
      }
      const systemInfo = await response.json();

      // Parse system info and determine capabilities
      const capabilities: SystemCapabilities = {
        hasGPU: systemInfo.gpuInfo?.isAvailable || false,
        gpuName: systemInfo.gpuInfo?.name,
        totalRAM: systemInfo.memory?.totalGB || 0,
        availableRAM: systemInfo.memory?.availableGB || 0,
        availableDiskSpace: systemInfo.disk?.availableGB || 0,
        meetsMinimumRequirements: true,
        warnings: [],
      };

      // Check minimum requirements
      if (capabilities.totalRAM < 8) {
        capabilities.meetsMinimumRequirements = false;
        capabilities.warnings.push('Less than 8GB RAM - training may be slow or fail');
      }

      if (capabilities.availableDiskSpace < 5) {
        capabilities.warnings.push('Less than 5GB disk space available - may not be sufficient');
      }

      if (!capabilities.hasGPU) {
        capabilities.warnings.push('No GPU detected - training will use CPU (slower)');
      }

      set({ systemCapabilities: capabilities, isCheckingSystem: false });
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      set({ error: errorObj.message, isCheckingSystem: false });
    }
  },

  reset: () => set(initialState),
}));
