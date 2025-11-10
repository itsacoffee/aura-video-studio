/**
 * Video Generation API Service
 * Handles video generation, rendering, and job management
 */

import { get, post, put, del } from './apiClient';
import { loggingService } from '../loggingService';

export interface VideoGenerationRequest {
  script: string;
  title?: string;
  description?: string;
  settings?: VideoSettings;
  templateId?: string;
}

export interface VideoSettings {
  resolution?: '720p' | '1080p' | '4k';
  fps?: number;
  duration?: number;
  format?: 'mp4' | 'webm' | 'avi';
  quality?: 'low' | 'medium' | 'high' | 'ultra';
  audio?: AudioSettings;
  video?: {
    codec?: string;
    bitrate?: number;
  };
}

export interface AudioSettings {
  enabled?: boolean;
  voice?: string;
  speed?: number;
  pitch?: number;
  volume?: number;
}

export interface VideoJob {
  id: string;
  title: string;
  status: 'queued' | 'processing' | 'completed' | 'failed' | 'cancelled';
  progress: number;
  stage?: string;
  createdAt: string;
  updatedAt: string;
  completedAt?: string;
  outputUrl?: string;
  thumbnailUrl?: string;
  error?: string;
  metadata?: Record<string, unknown>;
}

export interface RenderRequest {
  projectId: string;
  settings?: VideoSettings;
  outputName?: string;
}

export interface ExportRequest {
  jobId: string;
  format?: string;
  quality?: string;
}

/**
 * Generate video from script
 */
export async function generateVideo(request: VideoGenerationRequest): Promise<VideoJob> {
  try {
    loggingService.info('Starting video generation', 'videoGenerationApi', 'generateVideo', {
      title: request.title,
    });
    
    const response = await post<VideoJob>('/api/video/generate', request, {
      timeout: 300000, // 5 minutes timeout for generation requests
    });
    
    loggingService.info('Video generation started', 'videoGenerationApi', 'generateVideo', {
      jobId: response.id,
    });
    
    return response;
  } catch (error) {
    loggingService.error(
      'Failed to start video generation',
      error instanceof Error ? error : new Error(String(error)),
      'videoGenerationApi',
      'generateVideo'
    );
    throw error;
  }
}

/**
 * Render project to video
 */
export async function renderProject(request: RenderRequest): Promise<VideoJob> {
  try {
    loggingService.info('Starting project render', 'videoGenerationApi', 'renderProject', {
      projectId: request.projectId,
    });
    
    const response = await post<VideoJob>('/api/video/render', request, {
      timeout: 300000, // 5 minutes timeout
    });
    
    loggingService.info('Project render started', 'videoGenerationApi', 'renderProject', {
      jobId: response.id,
    });
    
    return response;
  } catch (error) {
    loggingService.error(
      'Failed to start project render',
      error instanceof Error ? error : new Error(String(error)),
      'videoGenerationApi',
      'renderProject'
    );
    throw error;
  }
}

/**
 * Get job status
 */
export async function getJobStatus(jobId: string): Promise<VideoJob> {
  try {
    loggingService.debug('Fetching job status', 'videoGenerationApi', 'getJobStatus', { jobId });
    
    const response = await get<VideoJob>(`/api/jobs/${jobId}`);
    
    loggingService.debug('Job status fetched', 'videoGenerationApi', 'getJobStatus', {
      jobId,
      status: response.status,
      progress: response.progress,
    });
    
    return response;
  } catch (error) {
    loggingService.error(
      'Failed to fetch job status',
      error instanceof Error ? error : new Error(String(error)),
      'videoGenerationApi',
      'getJobStatus'
    );
    throw error;
  }
}

/**
 * Get all jobs
 */
export async function getJobs(
  filters?: {
    status?: string;
    limit?: number;
    offset?: number;
  }
): Promise<{
  jobs: VideoJob[];
  total: number;
}> {
  try {
    loggingService.debug('Fetching jobs', 'videoGenerationApi', 'getJobs');
    
    const params = new URLSearchParams();
    if (filters?.status) params.append('status', filters.status);
    if (filters?.limit) params.append('limit', filters.limit.toString());
    if (filters?.offset) params.append('offset', filters.offset.toString());
    
    const response = await get<{ jobs: VideoJob[]; total: number }>(
      `/api/jobs?${params.toString()}`
    );
    
    loggingService.debug('Jobs fetched', 'videoGenerationApi', 'getJobs', {
      count: response.jobs.length,
      total: response.total,
    });
    
    return response;
  } catch (error) {
    loggingService.error(
      'Failed to fetch jobs',
      error instanceof Error ? error : new Error(String(error)),
      'videoGenerationApi',
      'getJobs'
    );
    throw error;
  }
}

/**
 * Cancel job
 */
export async function cancelJob(jobId: string): Promise<void> {
  try {
    loggingService.info('Cancelling job', 'videoGenerationApi', 'cancelJob', { jobId });
    
    await post<void>(`/api/jobs/${jobId}/cancel`);
    
    loggingService.info('Job cancelled', 'videoGenerationApi', 'cancelJob', { jobId });
  } catch (error) {
    loggingService.error(
      'Failed to cancel job',
      error instanceof Error ? error : new Error(String(error)),
      'videoGenerationApi',
      'cancelJob'
    );
    throw error;
  }
}

/**
 * Retry failed job
 */
export async function retryJob(jobId: string): Promise<VideoJob> {
  try {
    loggingService.info('Retrying job', 'videoGenerationApi', 'retryJob', { jobId });
    
    const response = await post<VideoJob>(`/api/jobs/${jobId}/retry`);
    
    loggingService.info('Job retry started', 'videoGenerationApi', 'retryJob', {
      newJobId: response.id,
    });
    
    return response;
  } catch (error) {
    loggingService.error(
      'Failed to retry job',
      error instanceof Error ? error : new Error(String(error)),
      'videoGenerationApi',
      'retryJob'
    );
    throw error;
  }
}

/**
 * Delete job
 */
export async function deleteJob(jobId: string): Promise<void> {
  try {
    loggingService.info('Deleting job', 'videoGenerationApi', 'deleteJob', { jobId });
    
    await del<void>(`/api/jobs/${jobId}`);
    
    loggingService.info('Job deleted', 'videoGenerationApi', 'deleteJob', { jobId });
  } catch (error) {
    loggingService.error(
      'Failed to delete job',
      error instanceof Error ? error : new Error(String(error)),
      'videoGenerationApi',
      'deleteJob'
    );
    throw error;
  }
}

/**
 * Download video
 */
export async function downloadVideo(jobId: string, filename?: string): Promise<void> {
  try {
    loggingService.info('Downloading video', 'videoGenerationApi', 'downloadVideo', { jobId });
    
    const response = await get<Blob>(`/api/jobs/${jobId}/download`, {
      responseType: 'blob',
    });
    
    // Create download link
    const blob = new Blob([response]);
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename || `video-${jobId}.mp4`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
    
    loggingService.info('Video downloaded', 'videoGenerationApi', 'downloadVideo', { jobId });
  } catch (error) {
    loggingService.error(
      'Failed to download video',
      error instanceof Error ? error : new Error(String(error)),
      'videoGenerationApi',
      'downloadVideo'
    );
    throw error;
  }
}

/**
 * Export video in different format
 */
export async function exportVideo(request: ExportRequest): Promise<VideoJob> {
  try {
    loggingService.info('Exporting video', 'videoGenerationApi', 'exportVideo', {
      jobId: request.jobId,
      format: request.format,
    });
    
    const response = await post<VideoJob>('/api/video/export', request);
    
    loggingService.info('Video export started', 'videoGenerationApi', 'exportVideo', {
      exportJobId: response.id,
    });
    
    return response;
  } catch (error) {
    loggingService.error(
      'Failed to export video',
      error instanceof Error ? error : new Error(String(error)),
      'videoGenerationApi',
      'exportVideo'
    );
    throw error;
  }
}

/**
 * Get video thumbnail
 */
export async function getThumbnail(jobId: string): Promise<string> {
  try {
    loggingService.debug('Fetching video thumbnail', 'videoGenerationApi', 'getThumbnail', {
      jobId,
    });
    
    const response = await get<{ thumbnailUrl: string }>(`/api/jobs/${jobId}/thumbnail`);
    
    loggingService.debug('Thumbnail fetched', 'videoGenerationApi', 'getThumbnail', { jobId });
    
    return response.thumbnailUrl;
  } catch (error) {
    loggingService.error(
      'Failed to fetch thumbnail',
      error instanceof Error ? error : new Error(String(error)),
      'videoGenerationApi',
      'getThumbnail'
    );
    throw error;
  }
}

/**
 * Get video metadata
 */
export async function getVideoMetadata(
  jobId: string
): Promise<{
  duration: number;
  resolution: string;
  fps: number;
  codec: string;
  fileSize: number;
  bitrate: number;
}> {
  try {
    loggingService.debug('Fetching video metadata', 'videoGenerationApi', 'getVideoMetadata', {
      jobId,
    });
    
    const response = await get<{
      duration: number;
      resolution: string;
      fps: number;
      codec: string;
      fileSize: number;
      bitrate: number;
    }>(`/api/jobs/${jobId}/metadata`);
    
    loggingService.debug('Video metadata fetched', 'videoGenerationApi', 'getVideoMetadata', {
      jobId,
    });
    
    return response;
  } catch (error) {
    loggingService.error(
      'Failed to fetch video metadata',
      error instanceof Error ? error : new Error(String(error)),
      'videoGenerationApi',
      'getVideoMetadata'
    );
    throw error;
  }
}
