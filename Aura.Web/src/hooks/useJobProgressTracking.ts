/**
 * useJobProgress Hook
 * Integrates SSE connection with ProgressStore for real-time job progress tracking
 * Implements resilience patterns: automatic reconnection, circuit breaker, exponential backoff
 */

import { useEffect, useCallback, useRef } from 'react';
import { useProgressStore, getReconnectDelay } from '../stores/progressStore';
import type { ProgressEventDto, HeartbeatEventDto } from '../types/api-v1';
import { useSSEConnection } from './useSSEConnection';

export interface UseJobProgressOptions {
  jobId: string;
  enabled?: boolean;
  onComplete?: () => void;
  onError?: (error: Error) => void;
}

export interface UseJobProgressResult {
  progress: ReturnType<typeof useProgressStore.getState>['jobProgress'] extends Map<string, infer T> ? T | undefined : never;
  isConnected: boolean;
  reconnectAttempts: number;
  circuitState: ReturnType<typeof useProgressStore.getState>['getCircuitState'];
  disconnect: () => void;
}

/**
 * Hook for tracking real-time job progress via SSE
 * Automatically handles connection, reconnection, and cleanup
 */
export function useJobProgress(options: UseJobProgressOptions): UseJobProgressResult {
  const { jobId, enabled = true, onComplete, onError } = options;
  
  const {
    updateProgress,
    setJobStatus,
    addWarning,
    clearProgress,
    registerConnection,
    updateConnectionStatus,
    incrementReconnectAttempt,
    resetConnection,
    recordSuccess,
    recordFailure,
    getCircuitState,
    getProgress,
    isConnected: isStoreConnected,
    shouldAttemptReconnect,
  } = useProgressStore();

  const progress = getProgress(jobId);
  const circuitState = getCircuitState();
  
  const reconnectAttemptsRef = useRef(0);
  const hasCompletedRef = useRef(false);

  // Handle SSE messages
  const handleMessage = useCallback((message: { type: string; data: unknown }) => {
    try {
      switch (message.type) {
        case 'step-progress': {
          const progressData = message.data as ProgressEventDto;
          updateProgress(jobId, progressData);
          recordSuccess();
          break;
        }

        case 'job-status': {
          const statusData = message.data as { status: string };
          const jobStatus = mapApiStatusToJobStatus(statusData.status);
          setJobStatus(jobId, jobStatus);
          recordSuccess();
          break;
        }

        case 'step-status': {
          const stepData = message.data as { step: string; status: string };
          recordSuccess();
          break;
        }

        case 'warning': {
          const warningData = message.data as { message: string };
          addWarning(jobId, warningData.message);
          break;
        }

        case 'job-completed': {
          setJobStatus(jobId, 'completed');
          hasCompletedRef.current = true;
          recordSuccess();
          if (onComplete) {
            onComplete();
          }
          break;
        }

        case 'job-failed': {
          const failData = message.data as { errorMessage?: string };
          setJobStatus(jobId, 'failed');
          hasCompletedRef.current = true;
          recordSuccess();
          if (onError && failData.errorMessage) {
            onError(new Error(failData.errorMessage));
          }
          break;
        }

        case 'job-cancelled': {
          setJobStatus(jobId, 'cancelled');
          hasCompletedRef.current = true;
          recordSuccess();
          break;
        }

        case 'heartbeat': {
          const heartbeat = message.data as HeartbeatEventDto;
          updateConnectionStatus(jobId, 'connected');
          recordSuccess();
          break;
        }

        case 'error': {
          const errorData = message.data as { message: string };
          recordFailure();
          if (onError) {
            onError(new Error(errorData.message));
          }
          break;
        }

        default:
          console.warn(`Unhandled SSE event type: ${message.type}`);
      }
    } catch (error) {
      const err = error instanceof Error ? error : new Error('Failed to process SSE message');
      console.error('Error processing SSE message:', err);
      recordFailure();
      if (onError) {
        onError(err);
      }
    }
  }, [jobId, updateProgress, setJobStatus, addWarning, recordSuccess, recordFailure, updateConnectionStatus, onComplete, onError]);

  // Handle connection open
  const handleOpen = useCallback(() => {
    updateConnectionStatus(jobId, 'connected');
    resetConnection(jobId);
    reconnectAttemptsRef.current = 0;
    recordSuccess();
  }, [jobId, updateConnectionStatus, resetConnection, recordSuccess]);

  // Handle connection error
  const handleError = useCallback((error: Error) => {
    console.error(`SSE connection error for job ${jobId}:`, error);
    updateConnectionStatus(jobId, 'error');
    recordFailure();
    
    if (onError) {
      onError(error);
    }
  }, [jobId, updateConnectionStatus, recordFailure, onError]);

  // Handle connection close
  const handleClose = useCallback(() => {
    updateConnectionStatus(jobId, 'disconnected');
  }, [jobId, updateConnectionStatus]);

  // Calculate reconnect delay with exponential backoff
  const reconnectDelay = getReconnectDelay(reconnectAttemptsRef.current);

  // Initialize SSE connection
  const { isConnected, reconnectAttempt, connect, disconnect } = useSSEConnection({
    onMessage: handleMessage,
    onError: handleError,
    onOpen: handleOpen,
    onClose: handleClose,
    reconnectDelay,
    maxReconnectAttempts: 5,
  });

  // Register connection in store
  useEffect(() => {
    if (enabled) {
      registerConnection(jobId, {
        jobId,
        connected: false,
        reconnectAttempts: 0,
        maxReconnectAttempts: 5,
        lastReconnectTime: null,
        connectionStatus: 'disconnected',
        lastHeartbeat: null,
      });
    }
  }, [jobId, enabled, registerConnection]);

  // Connect to SSE endpoint
  useEffect(() => {
    if (!enabled || hasCompletedRef.current) {
      return;
    }

    // Check circuit breaker state
    const currentCircuitState = getCircuitState();
    if (currentCircuitState === 'open') {
      console.warn(`Circuit breaker is open for job ${jobId}, not connecting`);
      return;
    }

    // Check if should attempt reconnection
    if (!shouldAttemptReconnect(jobId)) {
      console.warn(`Max reconnection attempts reached for job ${jobId}`);
      return;
    }

    const endpoint = `/api/jobs/${jobId}/events`;
    connect(endpoint);

    return () => {
      if (!hasCompletedRef.current) {
        disconnect();
      }
    };
  }, [jobId, enabled, connect, disconnect, getCircuitState, shouldAttemptReconnect]);

  // Track reconnect attempts in store
  useEffect(() => {
    if (reconnectAttempt > reconnectAttemptsRef.current) {
      reconnectAttemptsRef.current = reconnectAttempt;
      incrementReconnectAttempt(jobId);
    }
  }, [reconnectAttempt, jobId, incrementReconnectAttempt]);

  // Cleanup on unmount or job completion
  useEffect(() => {
    return () => {
      if (hasCompletedRef.current) {
        clearProgress(jobId);
      }
    };
  }, [jobId, clearProgress]);

  return {
    progress,
    isConnected: isStoreConnected(jobId),
    reconnectAttempts: reconnectAttemptsRef.current,
    circuitState,
    disconnect,
  };
}

/**
 * Map API job status to internal job status
 */
function mapApiStatusToJobStatus(apiStatus: string): 'queued' | 'running' | 'completed' | 'failed' | 'cancelled' {
  const normalized = apiStatus.toLowerCase();
  
  switch (normalized) {
    case 'queued':
      return 'queued';
    case 'running':
      return 'running';
    case 'done':
    case 'succeeded':
    case 'completed':
      return 'completed';
    case 'failed':
      return 'failed';
    case 'canceled':
    case 'cancelled':
      return 'cancelled';
    default:
      console.warn(`Unknown job status: ${apiStatus}, defaulting to 'running'`);
      return 'running';
  }
}
