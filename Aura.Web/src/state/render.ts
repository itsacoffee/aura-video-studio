import { create } from 'zustand';
import { apiUrl } from '../config/api';

export interface RenderSettings {
  resolution: {
    width: number;
    height: number;
  };
  fps: number;
  codec: 'H264' | 'HEVC' | 'AV1';
  container: 'mp4' | 'mkv' | 'mov';
  qualityLevel: number; // 0-100
  videoBitrateK: number;
  audioBitrateK: number;
  enableSceneCut: boolean;
}

export interface QueueItem {
  id: string;
  status: 'queued' | 'processing' | 'completed' | 'failed' | 'cancelled';
  progress: number;
  settings: RenderSettings;
  outputPath?: string;
  error?: string;
  createdAt: Date;
  startedAt?: Date;
  completedAt?: Date;
  estimatedTimeRemaining?: number; // seconds
  retryCount: number;
}

interface RenderState {
  // Current settings
  settings: RenderSettings;
  
  // Queue
  queue: QueueItem[];
  
  // Actions
  updateSettings: (settings: Partial<RenderSettings>) => void;
  setPreset: (preset: string) => void;
  addToQueue: (settings: RenderSettings) => string;
  removeFromQueue: (id: string) => void;
  updateQueueItem: (id: string, updates: Partial<QueueItem>) => void;
  processQueue: () => void;
}

// Preset configurations
const PRESETS: Record<string, Partial<RenderSettings>> = {
  'YouTube 1080p': {
    resolution: { width: 1920, height: 1080 },
    fps: 30,
    codec: 'H264',
    container: 'mp4',
    qualityLevel: 75,
    videoBitrateK: 12000,
    audioBitrateK: 256,
    enableSceneCut: true,
  },
  'YouTube Shorts': {
    resolution: { width: 1080, height: 1920 },
    fps: 30,
    codec: 'H264',
    container: 'mp4',
    qualityLevel: 75,
    videoBitrateK: 10000,
    audioBitrateK: 256,
    enableSceneCut: true,
  },
  'YouTube 4K': {
    resolution: { width: 3840, height: 2160 },
    fps: 30,
    codec: 'H264',
    container: 'mp4',
    qualityLevel: 75,
    videoBitrateK: 45000,
    audioBitrateK: 320,
    enableSceneCut: true,
  },
  'YouTube 1440p': {
    resolution: { width: 2560, height: 1440 },
    fps: 30,
    codec: 'H264',
    container: 'mp4',
    qualityLevel: 75,
    videoBitrateK: 24000,
    audioBitrateK: 256,
    enableSceneCut: true,
  },
  'YouTube 720p': {
    resolution: { width: 1280, height: 720 },
    fps: 30,
    codec: 'H264',
    container: 'mp4',
    qualityLevel: 75,
    videoBitrateK: 8000,
    audioBitrateK: 192,
    enableSceneCut: true,
  },
};

const DEFAULT_SETTINGS: RenderSettings = {
  resolution: { width: 1920, height: 1080 },
  fps: 30,
  codec: 'H264',
  container: 'mp4',
  qualityLevel: 75,
  videoBitrateK: 12000,
  audioBitrateK: 256,
  enableSceneCut: true,
};

export const useRenderStore = create<RenderState>((set, get) => ({
  settings: DEFAULT_SETTINGS,
  queue: [],

  updateSettings: (newSettings) => {
    set((state) => ({
      settings: { ...state.settings, ...newSettings },
    }));
  },

  setPreset: (preset) => {
    const presetSettings = PRESETS[preset];
    if (presetSettings) {
      set((state) => ({
        settings: { ...state.settings, ...presetSettings },
      }));
    }
  },

  addToQueue: (settings) => {
    const id = `render-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    const item: QueueItem = {
      id,
      status: 'queued',
      progress: 0,
      settings,
      createdAt: new Date(),
      retryCount: 0,
    };
    
    set((state) => ({
      queue: [...state.queue, item],
    }));
    
    // Start processing if not already processing
    setTimeout(() => get().processQueue(), 0);
    
    return id;
  },

  removeFromQueue: (id) => {
    set((state) => ({
      queue: state.queue.filter((item) => item.id !== id),
    }));
  },

  updateQueueItem: (id, updates) => {
    set((state) => ({
      queue: state.queue.map((item) =>
        item.id === id ? { ...item, ...updates } : item
      ),
    }));
  },

  processQueue: async () => {
    const { queue, updateQueueItem } = get();
    
    // Find next queued item (parallelism of 1)
    const processing = queue.filter((item) => item.status === 'processing');
    if (processing.length > 0) {
      return; // Already processing
    }
    
    const nextItem = queue.find((item) => item.status === 'queued');
    if (!nextItem) {
      return; // Nothing to process
    }
    
    // Start processing
    updateQueueItem(nextItem.id, {
      status: 'processing',
      startedAt: new Date(),
    });
    
    try {
      // Send render request to API
      const response = await fetch('/api/render', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          settings: nextItem.settings,
        }),
      });
      
      if (!response.ok) {
        throw new Error(`Render request failed: ${response.statusText}`);
      }
      
      const data = await response.json();
      const jobId = data.jobId;
      
      // Poll for progress
      const pollInterval = setInterval(async () => {
        try {
          const progressResponse = await fetch(`/api/render/${jobId}/progress`);
          if (!progressResponse.ok) {
            throw new Error('Failed to get progress');
          }
          
          const progressData = await progressResponse.json();
          
          updateQueueItem(nextItem.id, {
            progress: progressData.progress,
            estimatedTimeRemaining: progressData.estimatedTimeRemaining,
          });
          
          if (progressData.status === 'completed') {
            clearInterval(pollInterval);
            updateQueueItem(nextItem.id, {
              status: 'completed',
              progress: 100,
              outputPath: progressData.outputPath,
              completedAt: new Date(),
            });
            
            // Process next item
            setTimeout(() => get().processQueue(), 100);
          } else if (progressData.status === 'failed') {
            clearInterval(pollInterval);
            
            // Retry once
            if (nextItem.retryCount < 1) {
              updateQueueItem(nextItem.id, {
                status: 'queued',
                retryCount: nextItem.retryCount + 1,
              });
              setTimeout(() => get().processQueue(), 1000);
            } else {
              updateQueueItem(nextItem.id, {
                status: 'failed',
                error: progressData.error || 'Render failed',
                completedAt: new Date(),
              });
              
              // Process next item
              setTimeout(() => get().processQueue(), 100);
            }
          }
        } catch (error) {
          console.error('Error polling render progress:', error);
        }
      }, 2000); // Poll every 2 seconds
      
    } catch (error) {
      console.error('Error starting render:', error);
      
      // Retry once
      if (nextItem.retryCount < 1) {
        updateQueueItem(nextItem.id, {
          status: 'queued',
          retryCount: nextItem.retryCount + 1,
        });
        setTimeout(() => get().processQueue(), 1000);
      } else {
        updateQueueItem(nextItem.id, {
          status: 'failed',
          error: error instanceof Error ? error.message : 'Unknown error',
          completedAt: new Date(),
        });
        
        // Process next item
        setTimeout(() => get().processQueue(), 100);
      }
    }
  },
}));
