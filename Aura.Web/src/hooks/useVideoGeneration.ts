/**
 * useVideoGeneration Hook
 * Handles complete video generation lifecycle with SSE updates
 */

import { useCallback, useEffect, useState } from 'react';
import {
  cancelVideoGeneration,
  generateVideo,
  type VideoGenerationRequest,
  type VideoStatus,
} from '../services/api/videoApi';
import type { ProgressEventDto } from '../types/api-v1';
import { useApiError } from './useApiError';
import { useSSEConnection } from './useSSEConnection';

export interface UseVideoGenerationOptions {
  onComplete?: (status: VideoStatus) => void;
  onError?: (error: Error) => void;
  onProgress?: (progress: number, message?: string) => void;
}

export interface UseVideoGenerationResult {
  isGenerating: boolean;
  progress: number;
  status: VideoStatus | null;
  error: Error | null;
  generate: (request: VideoGenerationRequest) => Promise<void>;
  cancel: () => Promise<void>;
  retry: () => Promise<void>;
  reset: () => void;
}

/**
 * Hook for managing video generation lifecycle
 */
export function useVideoGeneration(
  options: UseVideoGenerationOptions = {}
): UseVideoGenerationResult {
  const [isGenerating, setIsGenerating] = useState(false);
  const [progress, setProgress] = useState(0);
  const [status, setStatus] = useState<VideoStatus | null>(null);
  const [jobId, setJobId] = useState<string | null>(null);
  const [lastRequest, setLastRequest] = useState<VideoGenerationRequest | null>(null);

  const { error, setError, clearError } = useApiError();

  // SSE connection for real-time updates (isConnected is available but not used)
  const { connect, disconnect } = useSSEConnection({
    onMessage: (event) => {
      const { type, data } = event;

      switch (type) {
        case 'job-status': {
          const statusData = data as { status: string; stage: string; percent: number };
          setProgress(statusData.percent);
          if (options.onProgress) {
            options.onProgress(statusData.percent);
          }
          break;
        }

        case 'step-progress': {
          const progressData = data as ProgressEventDto & { progressPct?: number };
          const nextPercent =
            typeof progressData.percent === 'number'
              ? progressData.percent
              : typeof progressData.progressPct === 'number'
                ? progressData.progressPct
                : progress;
          setProgress(nextPercent);
          if (options.onProgress) {
            options.onProgress(
              nextPercent,
              progressData.message ?? progressData.substageDetail ?? undefined
            );
          }
          break;
        }

        case 'job-completed': {
          const completedData = data as VideoStatus;
          setStatus(completedData);
          setProgress(100);
          setIsGenerating(false);
          disconnect();
          if (options.onComplete) {
            options.onComplete(completedData);
          }
          break;
        }

        case 'job-failed': {
          const failedData = data as { errorMessage?: string };
          const error = new Error(failedData.errorMessage || 'Video generation failed');
          setError(error);
          setIsGenerating(false);
          disconnect();
          if (options.onError) {
            options.onError(error);
          }
          break;
        }

        case 'job-cancelled': {
          setIsGenerating(false);
          disconnect();
          break;
        }

        case 'error': {
          const errorData = data as { message: string };
          const error = new Error(errorData.message);
          setError(error);
          if (options.onError) {
            options.onError(error);
          }
          break;
        }
      }
    },
    onError: (error) => {
      setError(new Error(`Connection error: ${error.message}`));
      if (options.onError) {
        options.onError(error);
      }
    },
  });

  // Generate video
  const generate = useCallback(
    async (request: VideoGenerationRequest) => {
      try {
        clearError();
        setIsGenerating(true);
        setProgress(0);
        setStatus(null);
        setLastRequest(request);

        // Start video generation
        const response = await generateVideo(request);
        setJobId(response.jobId);

        // Connect to SSE for real-time updates
        connect(`/api/jobs/${response.jobId}/events`);
      } catch (err) {
        const error = err instanceof Error ? err : new Error('Failed to start video generation');
        setError(error);
        setIsGenerating(false);
        if (options.onError) {
          options.onError(error);
        }
      }
    },
    [clearError, connect, options, setError]
  );

  // Cancel video generation
  const cancel = useCallback(async () => {
    if (!jobId) return;

    try {
      await cancelVideoGeneration(jobId);
      disconnect();
      setIsGenerating(false);
    } catch (err) {
      const error = err instanceof Error ? err : new Error('Failed to cancel video generation');
      setError(error);
      if (options.onError) {
        options.onError(error);
      }
    }
  }, [jobId, disconnect, setError, options]);

  // Retry last request
  const retry = useCallback(async () => {
    if (!lastRequest) {
      throw new Error('No previous request to retry');
    }
    await generate(lastRequest);
  }, [lastRequest, generate]);

  // Reset state
  const reset = useCallback(() => {
    setIsGenerating(false);
    setProgress(0);
    setStatus(null);
    setJobId(null);
    clearError();
    disconnect();
  }, [clearError, disconnect]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      disconnect();
    };
  }, [disconnect]);

  return {
    isGenerating,
    progress,
    status,
    error,
    generate,
    cancel,
    retry,
    reset,
  };
}
