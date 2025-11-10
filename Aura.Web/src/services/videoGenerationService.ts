/**
 * Video Generation Service
 * Provides end-to-end video generation with SSE progress tracking,
 * proper error handling, and automatic retry logic
 */

import { post, get, downloadFile, ExtendedAxiosRequestConfig } from './api/apiClient';
import { createSseClient, SseClient, SseConnectionState } from './api/sseClient';
import { loggingService } from './loggingService';

/**
 * Video generation request interface matching backend DTOs
 */
export interface VideoGenerationRequest {
  brief: string;
  voiceId?: string | null;
  style?: string | null;
  durationMinutes: number;
  options?: VideoGenerationOptions | null;
}

/**
 * Video generation options matching backend DTOs
 */
export interface VideoGenerationOptions {
  audience?: string | null;
  goal?: string | null;
  tone?: string | null;
  language?: string | null;
  aspect?: string | null;
  pacing?: string | null;
  density?: string | null;
  width?: number | null;
  height?: number | null;
  fps?: number | null;
  codec?: string | null;
  enableHardwareAcceleration?: boolean;
}

/**
 * Video generation response matching backend DTOs
 */
export interface VideoGenerationResponse {
  jobId: string;
  status: string;
  videoUrl: string | null;
  createdAt: string;
  correlationId: string;
}

/**
 * Video status matching backend DTOs
 */
export interface VideoStatus {
  jobId: string;
  status: string;
  progressPercentage: number;
  currentStage: string;
  createdAt: string;
  completedAt: string | null;
  videoUrl: string | null;
  errorMessage: string | null;
  processingSteps: string[];
  correlationId: string;
}

/**
 * Video metadata matching backend DTOs
 */
export interface VideoMetadata {
  jobId: string;
  outputPath: string;
  fileSizeBytes: number;
  createdAt: string;
  completedAt: string;
  duration: string;
  resolution: string;
  codec: string;
  fps: number;
  artifacts: ArtifactInfo[];
  correlationId: string;
}

/**
 * Artifact information
 */
export interface ArtifactInfo {
  name: string;
  path: string;
  type: string;
  sizeBytes: number;
}

/**
 * Progress update from SSE events
 */
export interface ProgressUpdate {
  percentage: number;
  stage: string;
  message: string;
  timestamp: string;
}

/**
 * SSE Event types from backend
 */
export type SseEventType = 
  | 'progress' 
  | 'stage-complete' 
  | 'done' 
  | 'error';

/**
 * SSE Event data structure
 */
export interface SseEventData {
  percentage?: number;
  stage?: string;
  nextStage?: string;
  message?: string;
  jobId?: string;
  videoUrl?: string;
  timestamp: string;
}

/**
 * Progress callback type
 */
export type ProgressCallback = (update: ProgressUpdate) => void;

/**
 * Error callback type
 */
export type ErrorCallback = (error: Error) => void;

/**
 * Connection status callback type
 */
export type ConnectionStatusCallback = (state: SseConnectionState) => void;

/**
 * Video Generation Service Class
 * Manages video generation requests and real-time progress tracking
 */
export class VideoGenerationService {
  private sseClient: SseClient | null = null;
  private currentJobId: string | null = null;
  private pollInterval: ReturnType<typeof setInterval> | null = null;

  /**
   * Start video generation
   * @param request Video generation request parameters
   * @returns Promise resolving to generation response
   */
  async generateVideo(request: VideoGenerationRequest): Promise<VideoGenerationResponse> {
    try {
      loggingService.info('Starting video generation', 'videoGenerationService', 'generateVideo', {
        brief: request.brief.substring(0, 50),
        duration: request.durationMinutes,
      });

      const response = await post<VideoGenerationResponse>(
        '/api/video/generate',
        request,
        {
          timeout: 60000, // 60 seconds timeout for initial request
          _skipDeduplication: false, // Allow deduplication to prevent duplicate submissions
        }
      );

      this.currentJobId = response.jobId;

      loggingService.info('Video generation started', 'videoGenerationService', 'generateVideo', {
        jobId: response.jobId,
        status: response.status,
        correlationId: response.correlationId,
      });

      return response;
    } catch (error) {
      loggingService.error(
        'Failed to start video generation',
        error instanceof Error ? error : new Error(String(error)),
        'videoGenerationService',
        'generateVideo'
      );
      throw error;
    }
  }

  /**
   * Get current status of video generation job
   * @param jobId Job identifier
   * @returns Promise resolving to video status
   */
  async getStatus(jobId: string): Promise<VideoStatus> {
    try {
      loggingService.debug('Fetching video status', 'videoGenerationService', 'getStatus', {
        jobId,
      });

      const response = await get<VideoStatus>(`/api/video/${jobId}/status`, {
        _skipRetry: false, // Allow retries for status checks
      });

      loggingService.debug('Video status fetched', 'videoGenerationService', 'getStatus', {
        jobId,
        status: response.status,
        progress: response.progressPercentage,
      });

      return response;
    } catch (error) {
      loggingService.error(
        'Failed to fetch video status',
        error instanceof Error ? error : new Error(String(error)),
        'videoGenerationService',
        'getStatus',
        { jobId }
      );
      throw error;
    }
  }

  /**
   * Stream progress updates via SSE with automatic reconnection
   * @param jobId Job identifier
   * @param onProgress Progress callback
   * @param onError Error callback
   * @param onConnectionStatusChange Connection status change callback
   * @returns Cleanup function to stop streaming
   */
  streamProgress(
    jobId: string,
    onProgress: ProgressCallback,
    onError?: ErrorCallback,
    onConnectionStatusChange?: ConnectionStatusCallback
  ): () => void {
    try {
      loggingService.info('Starting SSE progress stream', 'videoGenerationService', 'streamProgress', {
        jobId,
      });

      // Clean up existing SSE client if any
      this.stopStreaming();

      // Create new SSE client with reconnection support
      this.sseClient = createSseClient(jobId);
      this.currentJobId = jobId;

      // Subscribe to connection status changes
      if (onConnectionStatusChange) {
        this.sseClient.onStatusChange(onConnectionStatusChange);
      }

      // Handle progress events
      this.sseClient.on('progress', (event) => {
        try {
          const data = event.data as SseEventData;
          onProgress({
            percentage: data.percentage || 0,
            stage: data.stage || 'Processing',
            message: data.message || '',
            timestamp: data.timestamp,
          });
        } catch (err) {
          loggingService.error(
            'Error processing progress event',
            err instanceof Error ? err : new Error(String(err)),
            'videoGenerationService',
            'streamProgress'
          );
        }
      });

      // Handle stage completion
      this.sseClient.on('stage-complete', (event) => {
        try {
          const data = event.data as SseEventData;
          loggingService.debug('Stage completed', 'videoGenerationService', 'streamProgress', {
            stage: data.stage,
            nextStage: data.nextStage,
          });
        } catch (err) {
          loggingService.error(
            'Error processing stage-complete event',
            err instanceof Error ? err : new Error(String(err)),
            'videoGenerationService',
            'streamProgress'
          );
        }
      });

      // Handle completion
      this.sseClient.on('done', (event) => {
        try {
          const data = event.data as SseEventData;
          loggingService.info('Video generation completed', 'videoGenerationService', 'streamProgress', {
            jobId: data.jobId,
            videoUrl: data.videoUrl,
          });

          onProgress({
            percentage: 100,
            stage: 'Completed',
            message: 'Video generation complete!',
            timestamp: data.timestamp,
          });

          // Auto-cleanup on completion
          this.stopStreaming();
        } catch (err) {
          loggingService.error(
            'Error processing done event',
            err instanceof Error ? err : new Error(String(err)),
            'videoGenerationService',
            'streamProgress'
          );
        }
      });

      // Handle errors
      this.sseClient.on('error', (event) => {
        try {
          const data = event.data as SseEventData;
          const error = new Error(data.message || 'An error occurred during video generation');

          loggingService.error(
            'Video generation error',
            error,
            'videoGenerationService',
            'streamProgress',
            { jobId }
          );

          if (onError) {
            onError(error);
          }

          // Auto-cleanup on error
          this.stopStreaming();
        } catch (err) {
          loggingService.error(
            'Error processing error event',
            err instanceof Error ? err : new Error(String(err)),
            'videoGenerationService',
            'streamProgress'
          );
        }
      });

      // Connect to SSE endpoint
      this.sseClient.connect();

      // Return cleanup function
      return () => this.stopStreaming();
    } catch (error) {
      loggingService.error(
        'Failed to start SSE progress stream',
        error instanceof Error ? error : new Error(String(error)),
        'videoGenerationService',
        'streamProgress',
        { jobId }
      );

      if (onError) {
        onError(error instanceof Error ? error : new Error(String(error)));
      }

      // Return no-op cleanup function
      return () => {};
    }
  }

  /**
   * Stop SSE streaming and clean up resources
   */
  stopStreaming(): void {
    if (this.sseClient) {
      loggingService.debug('Stopping SSE stream', 'videoGenerationService', 'stopStreaming', {
        jobId: this.currentJobId,
      });
      this.sseClient.close();
      this.sseClient = null;
    }

    if (this.pollInterval) {
      clearInterval(this.pollInterval);
      this.pollInterval = null;
    }

    this.currentJobId = null;
  }

  /**
   * Poll for status updates (fallback when SSE is not available)
   * @param jobId Job identifier
   * @param onProgress Progress callback
   * @param onError Error callback
   * @param intervalMs Polling interval in milliseconds
   * @returns Cleanup function to stop polling
   */
  pollStatus(
    jobId: string,
    onProgress: ProgressCallback,
    onError?: ErrorCallback,
    intervalMs: number = 2000
  ): () => void {
    try {
      loggingService.info('Starting status polling', 'videoGenerationService', 'pollStatus', {
        jobId,
        intervalMs,
      });

      // Initial fetch
      this.getStatus(jobId)
        .then((status) => {
          onProgress({
            percentage: status.progressPercentage,
            stage: status.currentStage,
            message: `${status.currentStage} - ${status.progressPercentage}%`,
            timestamp: new Date().toISOString(),
          });
        })
        .catch((error) => {
          if (onError) {
            onError(error);
          }
        });

      // Set up polling
      this.pollInterval = setInterval(async () => {
        try {
          const status = await this.getStatus(jobId);

          onProgress({
            percentage: status.progressPercentage,
            stage: status.currentStage,
            message: `${status.currentStage} - ${status.progressPercentage}%`,
            timestamp: new Date().toISOString(),
          });

          // Stop polling if job is complete or failed
          if (status.status === 'completed' || status.status === 'failed') {
            this.stopPolling();

            if (status.status === 'failed' && onError) {
              onError(new Error(status.errorMessage || 'Video generation failed'));
            }
          }
        } catch (error) {
          loggingService.error(
            'Error polling status',
            error instanceof Error ? error : new Error(String(error)),
            'videoGenerationService',
            'pollStatus',
            { jobId }
          );

          if (onError) {
            onError(error instanceof Error ? error : new Error(String(error)));
          }
        }
      }, intervalMs);

      // Return cleanup function
      return () => this.stopPolling();
    } catch (error) {
      loggingService.error(
        'Failed to start status polling',
        error instanceof Error ? error : new Error(String(error)),
        'videoGenerationService',
        'pollStatus',
        { jobId }
      );

      if (onError) {
        onError(error instanceof Error ? error : new Error(String(error)));
      }

      // Return no-op cleanup function
      return () => {};
    }
  }

  /**
   * Stop status polling
   */
  private stopPolling(): void {
    if (this.pollInterval) {
      loggingService.debug('Stopping status polling', 'videoGenerationService', 'stopPolling', {
        jobId: this.currentJobId,
      });
      clearInterval(this.pollInterval);
      this.pollInterval = null;
    }
  }

  /**
   * Download generated video
   * @param jobId Job identifier
   * @param filename Optional filename for download
   * @param onProgress Optional progress callback
   */
  async downloadVideo(
    jobId: string,
    filename?: string,
    onProgress?: (progress: number) => void
  ): Promise<void> {
    try {
      loggingService.info('Downloading video', 'videoGenerationService', 'downloadVideo', {
        jobId,
        filename,
      });

      await downloadFile(
        `/api/video/${jobId}/download`,
        filename || `video-${jobId}.mp4`,
        onProgress
      );

      loggingService.info('Video downloaded successfully', 'videoGenerationService', 'downloadVideo', {
        jobId,
      });
    } catch (error) {
      loggingService.error(
        'Failed to download video',
        error instanceof Error ? error : new Error(String(error)),
        'videoGenerationService',
        'downloadVideo',
        { jobId }
      );
      throw error;
    }
  }

  /**
   * Get video metadata
   * @param jobId Job identifier
   * @returns Promise resolving to video metadata
   */
  async getMetadata(jobId: string): Promise<VideoMetadata> {
    try {
      loggingService.debug('Fetching video metadata', 'videoGenerationService', 'getMetadata', {
        jobId,
      });

      const response = await get<VideoMetadata>(`/api/video/${jobId}/metadata`);

      loggingService.debug('Video metadata fetched', 'videoGenerationService', 'getMetadata', {
        jobId,
        fileSize: response.fileSizeBytes,
        duration: response.duration,
      });

      return response;
    } catch (error) {
      loggingService.error(
        'Failed to fetch video metadata',
        error instanceof Error ? error : new Error(String(error)),
        'videoGenerationService',
        'getMetadata',
        { jobId }
      );
      throw error;
    }
  }

  /**
   * Cancel video generation job
   * @param jobId Job identifier
   */
  async cancelGeneration(jobId: string): Promise<void> {
    try {
      loggingService.info('Cancelling video generation', 'videoGenerationService', 'cancelGeneration', {
        jobId,
      });

      await post<{ message: string; jobId: string; correlationId: string }>(
        `/api/video/${jobId}/cancel`,
        undefined,
        {
          _skipRetry: true, // Don't retry cancellation requests
        }
      );

      // Stop streaming if this is the current job
      if (this.currentJobId === jobId) {
        this.stopStreaming();
      }

      loggingService.info('Video generation cancelled', 'videoGenerationService', 'cancelGeneration', {
        jobId,
      });
    } catch (error) {
      loggingService.error(
        'Failed to cancel video generation',
        error instanceof Error ? error : new Error(String(error)),
        'videoGenerationService',
        'cancelGeneration',
        { jobId }
      );
      throw error;
    }
  }

  /**
   * Get current job ID (if any active job)
   */
  getCurrentJobId(): string | null {
    return this.currentJobId;
  }

  /**
   * Check if SSE client is currently connected
   */
  isStreaming(): boolean {
    return this.sseClient !== null && this.sseClient.isConnected();
  }

  /**
   * Clean up all resources
   */
  cleanup(): void {
    this.stopStreaming();
    this.stopPolling();
    this.currentJobId = null;
  }
}

// Export singleton instance
export const videoGenerationService = new VideoGenerationService();

// Export class for testing and custom instances
export default VideoGenerationService;
