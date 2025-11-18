/**
 * Progress Store
 * Zustand store for managing real-time job progress with SSE subscription
 * Implements resilience patterns: reconnection, exponential backoff, circuit breaker
 */

import { create } from 'zustand';
import type { ProgressEventDto, ProviderCancellationStatusDto } from '../types/api-v1';

export interface ProgressState {
  // Progress tracking
  jobProgress: Map<string, JobProgressState>;

  // SSE connection state
  connections: Map<string, SSEConnectionInfo>;

  // Circuit breaker state
  circuitBreaker: CircuitBreakerState;

  // Actions
  updateProgress: (jobId: string, progress: ProgressEventDto) => void;
  setJobStatus: (jobId: string, status: JobStatus) => void;
  addWarning: (jobId: string, warning: string) => void;
  clearProgress: (jobId: string) => void;

  // SSE connection management
  registerConnection: (jobId: string, connectionInfo: SSEConnectionInfo) => void;
  updateConnectionStatus: (jobId: string, status: ConnectionStatus) => void;
  incrementReconnectAttempt: (jobId: string) => void;
  resetConnection: (jobId: string) => void;

  // Circuit breaker
  recordSuccess: () => void;
  recordFailure: () => void;
  getCircuitState: () => CircuitState;

  // Computed
  getProgress: (jobId: string) => JobProgressState | undefined;
  isConnected: (jobId: string) => boolean;
  shouldAttemptReconnect: (jobId: string) => boolean;
}

export interface JobProgressState {
  jobId: string;
  stage: string;
  percent: number;
  etaSeconds: number | null;
  elapsedSeconds: number | null;
  estimatedRemainingSeconds: number | null;
  message: string;
  warnings: string[];
  correlationId: string | null;
  substageDetail: string | null;
  currentItem: number | null;
  totalItems: number | null;
  phase: string | null;
  status: JobStatus;
  lastUpdated: Date;
  cancellationInfo?: CancellationInfo;
}

export interface CancellationInfo {
  requested: boolean;
  requestedAt: Date | null;
  providerStatuses: ProviderCancellationStatusDto[];
  warnings: string[];
}

export interface SSEConnectionInfo {
  jobId: string;
  connected: boolean;
  reconnectAttempts: number;
  maxReconnectAttempts: number;
  lastReconnectTime: Date | null;
  connectionStatus: ConnectionStatus;
  lastHeartbeat: Date | null;
}

export interface CircuitBreakerState {
  state: CircuitState;
  failureCount: number;
  successCount: number;
  lastFailureTime: Date | null;
  nextAttemptTime: Date | null;
}

export type JobStatus = 'queued' | 'running' | 'completed' | 'failed' | 'cancelled';
export type ConnectionStatus = 'connected' | 'connecting' | 'disconnected' | 'error';
export type CircuitState = 'closed' | 'open' | 'half-open';

const INITIAL_RECONNECT_DELAY = 3000; // 3 seconds
const CIRCUIT_BREAKER_THRESHOLD = 5;
const CIRCUIT_BREAKER_TIMEOUT = 60000; // 1 minute

export const useProgressStore = create<ProgressState>((set, get) => ({
  // Initial state
  jobProgress: new Map(),
  connections: new Map(),
  circuitBreaker: {
    state: 'closed',
    failureCount: 0,
    successCount: 0,
    lastFailureTime: null,
    nextAttemptTime: null,
  },

  // Update progress for a job
  updateProgress: (jobId, progress) => {
    set((state) => {
      const newProgress = new Map(state.jobProgress);
      const existing = newProgress.get(jobId);

      const percent =
        typeof (progress as { Percent?: number }).Percent === 'number'
          ? (progress as unknown as { Percent: number }).Percent
          : progress.percent;
      const etaSeconds =
        progress.estimatedRemainingSeconds ?? progress.etaSeconds ?? existing?.etaSeconds ?? null;

      newProgress.set(jobId, {
        jobId: progress.jobId ?? jobId,
        stage: progress.stage,
        percent,
        etaSeconds,
        elapsedSeconds: progress.elapsedSeconds ?? existing?.elapsedSeconds ?? null,
        estimatedRemainingSeconds:
          progress.estimatedRemainingSeconds ?? existing?.estimatedRemainingSeconds ?? etaSeconds,
        message: progress.message,
        warnings: progress.warnings?.length ? progress.warnings : existing?.warnings || [],
        correlationId: progress.correlationId ?? null,
        substageDetail: progress.substageDetail ?? null,
        currentItem: progress.currentItem ?? null,
        totalItems: progress.totalItems ?? null,
        phase: progress.phase ?? existing?.phase ?? null,
        status: existing?.status || 'running',
        lastUpdated: new Date(progress.timestamp || new Date()),
        cancellationInfo: existing?.cancellationInfo,
      });

      return { jobProgress: newProgress };
    });
  },

  // Set job status
  setJobStatus: (jobId, status) => {
    set((state) => {
      const newProgress = new Map(state.jobProgress);
      const existing = newProgress.get(jobId);

      if (existing) {
        newProgress.set(jobId, {
          ...existing,
          status,
          lastUpdated: new Date(),
        });
      }

      return { jobProgress: newProgress };
    });
  },

  // Add a warning to a job
  addWarning: (jobId, warning) => {
    set((state) => {
      const newProgress = new Map(state.jobProgress);
      const existing = newProgress.get(jobId);

      if (existing) {
        newProgress.set(jobId, {
          ...existing,
          warnings: [...existing.warnings, warning],
          lastUpdated: new Date(),
        });
      }

      return { jobProgress: newProgress };
    });
  },

  // Clear progress for a job
  clearProgress: (jobId) => {
    set((state) => {
      const newProgress = new Map(state.jobProgress);
      newProgress.delete(jobId);

      const newConnections = new Map(state.connections);
      newConnections.delete(jobId);

      return { jobProgress: newProgress, connections: newConnections };
    });
  },

  // Register SSE connection
  registerConnection: (jobId, connectionInfo) => {
    set((state) => {
      const newConnections = new Map(state.connections);
      newConnections.set(jobId, connectionInfo);
      return { connections: newConnections };
    });
  },

  // Update connection status
  updateConnectionStatus: (jobId, status) => {
    set((state) => {
      const newConnections = new Map(state.connections);
      const existing = newConnections.get(jobId);

      if (existing) {
        newConnections.set(jobId, {
          ...existing,
          connectionStatus: status,
          connected: status === 'connected',
          lastHeartbeat: status === 'connected' ? new Date() : existing.lastHeartbeat,
        });
      }

      return { connections: newConnections };
    });
  },

  // Increment reconnect attempt
  incrementReconnectAttempt: (jobId) => {
    set((state) => {
      const newConnections = new Map(state.connections);
      const existing = newConnections.get(jobId);

      if (existing) {
        newConnections.set(jobId, {
          ...existing,
          reconnectAttempts: existing.reconnectAttempts + 1,
          lastReconnectTime: new Date(),
        });
      }

      return { connections: newConnections };
    });
  },

  // Reset connection state
  resetConnection: (jobId) => {
    set((state) => {
      const newConnections = new Map(state.connections);
      const existing = newConnections.get(jobId);

      if (existing) {
        newConnections.set(jobId, {
          ...existing,
          reconnectAttempts: 0,
          lastReconnectTime: null,
          connectionStatus: 'disconnected',
          connected: false,
        });
      }

      return { connections: newConnections };
    });
  },

  // Record successful operation (circuit breaker)
  recordSuccess: () => {
    set((state) => ({
      circuitBreaker: {
        ...state.circuitBreaker,
        successCount: state.circuitBreaker.successCount + 1,
        failureCount: 0,
        state: 'closed',
        nextAttemptTime: null,
      },
    }));
  },

  // Record failed operation (circuit breaker)
  recordFailure: () => {
    set((state) => {
      const newFailureCount = state.circuitBreaker.failureCount + 1;
      const now = new Date();

      if (newFailureCount >= CIRCUIT_BREAKER_THRESHOLD) {
        return {
          circuitBreaker: {
            ...state.circuitBreaker,
            state: 'open',
            failureCount: newFailureCount,
            lastFailureTime: now,
            nextAttemptTime: new Date(now.getTime() + CIRCUIT_BREAKER_TIMEOUT),
          },
        };
      }

      return {
        circuitBreaker: {
          ...state.circuitBreaker,
          failureCount: newFailureCount,
          lastFailureTime: now,
        },
      };
    });
  },

  // Get circuit breaker state
  getCircuitState: () => {
    const { circuitBreaker } = get();

    if (circuitBreaker.state === 'open') {
      const now = new Date();
      if (circuitBreaker.nextAttemptTime && now >= circuitBreaker.nextAttemptTime) {
        set((state) => ({
          circuitBreaker: {
            ...state.circuitBreaker,
            state: 'half-open',
          },
        }));
        return 'half-open';
      }
    }

    return circuitBreaker.state;
  },

  // Get progress for a specific job
  getProgress: (jobId) => {
    return get().jobProgress.get(jobId);
  },

  // Check if connected to a job
  isConnected: (jobId) => {
    const connection = get().connections.get(jobId);
    return connection?.connected || false;
  },

  // Check if should attempt reconnection
  shouldAttemptReconnect: (jobId) => {
    const connection = get().connections.get(jobId);
    if (!connection) return true;

    const circuitState = get().getCircuitState();
    if (circuitState === 'open') return false;

    if (connection.reconnectAttempts >= connection.maxReconnectAttempts) {
      return false;
    }

    if (connection.lastReconnectTime) {
      const timeSinceLastAttempt = Date.now() - connection.lastReconnectTime.getTime();
      const backoffDelay = INITIAL_RECONNECT_DELAY * Math.pow(2, connection.reconnectAttempts);
      return timeSinceLastAttempt >= backoffDelay;
    }

    return true;
  },
}));

/**
 * Calculate exponential backoff delay for reconnection
 */
export function getReconnectDelay(attemptNumber: number): number {
  return Math.min(INITIAL_RECONNECT_DELAY * Math.pow(2, attemptNumber), 30000); // Max 30 seconds
}
