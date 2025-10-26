/**
 * Export service for video export operations
 */

import { get, post } from './api/apiClient';

// Timeline-related types
export interface TimelineAsset {
  id: string;
  type: 'Image' | 'Video' | 'Audio';
  filePath: string;
  start: string; // TimeSpan
  duration: string; // TimeSpan
  position: {
    x: number;
    y: number;
    width: number;
    height: number;
  };
  zIndex?: number;
  opacity?: number;
  effects?: {
    brightness?: number;
    contrast?: number;
    saturation?: number;
    filter?: string;
  };
}

export interface TimelineScene {
  index: number;
  heading: string;
  script: string;
  start: string; // TimeSpan
  duration: string; // TimeSpan
  narrationAudioPath?: string;
  visualAssets?: TimelineAsset[];
  transitionType?: string;
  transitionDuration?: string; // TimeSpan
}

export interface EditableTimeline {
  scenes: TimelineScene[];
  backgroundMusicPath?: string;
  subtitles?: {
    enabled: boolean;
    filePath?: string;
    position?: string;
    fontSize?: number;
    fontColor?: string;
    backgroundColor?: string;
    backgroundOpacity?: number;
  };
}

export interface ExportPreset {
  name: string;
  description: string;
  platform: string;
  resolution: string;
  videoCodec: string;
  audioCodec: string;
  frameRate: number;
  videoBitrate: number;
  audioBitrate: number;
  aspectRatio: string;
  quality: string;
}

export interface ExportRequest {
  inputFile?: string;
  outputFile: string;
  presetName: string;
  startTime?: string; // TimeSpan format
  duration?: string; // TimeSpan format
  metadata?: Record<string, string>;
  timeline?: EditableTimeline;
}

export interface ExportJob {
  id: string;
  status: 'Queued' | 'Processing' | 'Completed' | 'Failed' | 'Cancelled';
  progress: number;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
  errorMessage?: string;
  outputFile?: string;
  estimatedTimeRemaining?: string;
}

export interface ExportHistoryItem {
  id: string;
  inputFile: string;
  outputFile: string;
  presetName: string;
  status: string;
  progress: number;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
  errorMessage?: string;
  fileSize?: number;
  durationSeconds?: number;
  platform?: string;
  resolution?: string;
  codec?: string;
}

export interface ExportResponse {
  jobId: string;
  message: string;
}

/**
 * Start a new export job
 */
export async function startExport(request: ExportRequest): Promise<ExportResponse> {
  return post<ExportResponse>('/api/export/start', request);
}

/**
 * Get the status of an export job
 */
export async function getExportStatus(jobId: string): Promise<ExportJob> {
  return get<ExportJob>(`/api/export/status/${jobId}`);
}

/**
 * Cancel an export job
 */
export async function cancelExport(jobId: string): Promise<{ message: string }> {
  return post<{ message: string }>(`/api/export/cancel/${jobId}`);
}

/**
 * Get all active export jobs
 */
export async function getActiveExports(): Promise<ExportJob[]> {
  return get<ExportJob[]>('/api/export/active');
}

/**
 * Get available export presets
 */
export async function getExportPresets(): Promise<ExportPreset[]> {
  return get<ExportPreset[]>('/api/export/presets');
}

/**
 * Get export history
 */
export async function getExportHistory(
  status?: string,
  limit?: number
): Promise<ExportHistoryItem[]> {
  const params = new URLSearchParams();
  if (status) params.append('status', status);
  if (limit) params.append('limit', limit.toString());
  
  const query = params.toString();
  return get<ExportHistoryItem[]>(`/api/export/history${query ? `?${query}` : ''}`);
}

/**
 * Poll export job status until completion or failure
 */
export async function pollExportStatus(
  jobId: string,
  onProgress: (job: ExportJob) => void,
  interval: number = 1000
): Promise<ExportJob> {
  return new Promise((resolve, reject) => {
    const poll = async () => {
      try {
        const job = await getExportStatus(jobId);
        onProgress(job);

        if (job.status === 'Completed') {
          resolve(job);
        } else if (job.status === 'Failed' || job.status === 'Cancelled') {
          reject(new Error(job.errorMessage || `Export ${job.status.toLowerCase()}`));
        } else {
          // Continue polling
          setTimeout(poll, interval);
        }
      } catch (error) {
        reject(error);
      }
    };

    poll();
  });
}
