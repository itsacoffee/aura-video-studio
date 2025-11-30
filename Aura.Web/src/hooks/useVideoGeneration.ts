/**
 * useVideoGeneration Hook
 * Handles complete video generation lifecycle with SSE updates
 */

import { useCallback, useEffect, useRef, useState } from 'react';
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
  /** Callback when progress appears stalled (no change for extended period) */
  onStall?: (stallInfo: { progress: number; stage: string; durationSeconds: number }) => void;
  /** Timeout in milliseconds for entire generation (default: 10 minutes) */
  timeoutMs?: number;
}

export interface UseVideoGenerationResult {
  isGenerating: boolean;
  progress: number;
  status: VideoStatus | null;
  error: Error | null;
  /** Current stage of generation */
  currentStage: string;
  /** Whether the generation appears stalled */
  isStalled: boolean;
  generate: (request: VideoGenerationRequest) => Promise<void>;
  cancel: () => Promise<void>;
  retry: () => Promise<void>;
  reset: () => void;
}

/**
 * Hook for managing video generation lifecycle with stall detection
 */
export function useVideoGeneration(
  options: UseVideoGenerationOptions = {}
): UseVideoGenerationResult {
  const { timeoutMs = 10 * 60 * 1000 } = options; // 10 minutes default

  const [isGenerating, setIsGenerating] = useState(false);
  const [progress, setProgress] = useState(0);
  const [status, setStatus] = useState<VideoStatus | null>(null);
  const [jobId, setJobId] = useState<string | null>(null);
  const [lastRequest, setLastRequest] = useState<VideoGenerationRequest | null>(null);
  const [currentStage, setCurrentStage] = useState<string>('');
  const [isStalled, setIsStalled] = useState(false);

  const { error, setError, clearError } = useApiError();

  // Refs for timeout tracking
  const timeoutTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Cleanup timeout utility
  const cleanupTimeout = useCallback(() => {
    if (timeoutTimerRef.current) {
      clearTimeout(timeoutTimerRef.current);
      timeoutTimerRef.current = null;
    }
  }, []);

  // Handle progress updates (job-status and step-progress events)
  const handleProgressUpdate = useCallback(
    (percent: number, stage?: string, message?: string) => {
      setProgress(percent);
      if (stage) setCurrentStage(stage);
      setIsStalled(false);
      options.onProgress?.(percent, message);
    },
    [options]
  );

  // Handle terminal events (cleanup connection and timeout)
  const handleTerminalEvent = useCallback(
    (disconnectFn: () => void) => {
      setIsGenerating(false);
      cleanupTimeout();
      disconnectFn();
    },
    [cleanupTimeout]
  );

  // SSE connection for real-time updates
  const { connect, disconnect } = useSSEConnection({
    onMessage: (event) => {
      const { type, data } = event;

      switch (type) {
        case 'job-status': {
          const statusData = data as { status: string; stage: string; percent: number };
          handleProgressUpdate(statusData.percent, statusData.stage);
          break;
        }

        case 'step-progress': {
          const progressData = data as ProgressEventDto & { progressPct?: number; stage?: string };
          const nextPercent =
            typeof progressData.percent === 'number'
              ? progressData.percent
              : typeof progressData.progressPct === 'number'
                ? progressData.progressPct
                : progress;
          handleProgressUpdate(
            nextPercent,
            progressData.stage,
            progressData.message ?? progressData.substageDetail
          );
          break;
        }

        case 'warning': {
          const warningData = data as {
            step?: string;
            percent?: number;
            stallDurationSeconds?: number;
          };
          if (warningData.stallDurationSeconds && warningData.stallDurationSeconds > 0) {
            setIsStalled(true);
            options.onStall?.({
              progress: warningData.percent ?? progress,
              stage: warningData.step ?? currentStage,
              durationSeconds: warningData.stallDurationSeconds,
            });
          }
          break;
        }

        case 'heartbeat':
          break;

        case 'job-completed': {
          const completedData = data as VideoStatus;
          setStatus(completedData);
          setProgress(100);
          handleTerminalEvent(disconnect);
          options.onComplete?.(completedData);
          break;
        }

        case 'job-failed': {
          const failedData = data as { errorMessage?: string };
          const failedError = new Error(failedData.errorMessage || 'Video generation failed');
          setError(failedError);
          handleTerminalEvent(disconnect);
          options.onError?.(failedError);
          break;
        }

        case 'job-cancelled':
          handleTerminalEvent(disconnect);
          break;

        case 'error': {
          const errorData = data as { message: string };
          const sseError = new Error(errorData.message);
          setError(sseError);
          options.onError?.(sseError);
          break;
        }
      }
    },
    onError: (err) => {
      setError(new Error(`Connection error: ${err.message}`));
      options.onError?.(err);
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
        setCurrentStage('');
        setIsStalled(false);
        cleanupTimeout();

        // Start video generation
        const response = await generateVideo(request);
        setJobId(response.jobId);

        // Set up timeout timer
        if (timeoutMs > 0) {
          timeoutTimerRef.current = setTimeout(() => {
            const timeoutError = new Error(
              `Video generation timed out after ${Math.round(timeoutMs / 60000)} minutes. Consider cancelling and retrying.`
            );
            setError(timeoutError);
            if (options.onError) {
              options.onError(timeoutError);
            }
          }, timeoutMs);
        }

        // Connect to SSE for real-time updates
        connect(`/api/jobs/${response.jobId}/events`);
      } catch (err) {
        const genError = err instanceof Error ? err : new Error('Failed to start video generation');
        setError(genError);
        setIsGenerating(false);
        cleanupTimeout();
        if (options.onError) {
          options.onError(genError);
        }
      }
    },
    [clearError, connect, options, setError, timeoutMs, cleanupTimeout]
  );

  // Cancel video generation
  const cancel = useCallback(async () => {
    if (!jobId) return;

    try {
      await cancelVideoGeneration(jobId);
      cleanupTimeout();
      disconnect();
      setIsGenerating(false);
    } catch (err) {
      const cancelError =
        err instanceof Error ? err : new Error('Failed to cancel video generation');
      setError(cancelError);
      if (options.onError) {
        options.onError(cancelError);
      }
    }
  }, [jobId, disconnect, setError, options, cleanupTimeout]);

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
    setCurrentStage('');
    setIsStalled(false);
    cleanupTimeout();
    clearError();
    disconnect();
  }, [clearError, disconnect, cleanupTimeout]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      cleanupTimeout();
      disconnect();
    };
  }, [disconnect, cleanupTimeout]);

  return {
    isGenerating,
    progress,
    status,
    error,
    currentStage,
    isStalled,
    generate,
    cancel,
    retry,
    reset,
  };
}
