/**
 * Enhanced SSE (Server-Sent Events) Client
 *
 * Provides robust SSE connection management with:
 * - Automatic reconnection with exponential backoff
 * - Connection state tracking
 * - Proper cleanup and cancellation
 * - Progress event parsing
 * - Error recovery
 */

import { loggingService } from '@/services/loggingService';
import { toError } from '@/utils/errorUtils';

export interface SSEConnectionOptions {
  url: string;
  onMessage: (event: MessageEvent) => void;
  onError?: (error: Error) => void;
  onOpen?: () => void;
  onClose?: () => void;
  maxRetries?: number;
  retryDelay?: number;
  timeout?: number;
}

export enum SSEConnectionState {
  CONNECTING = 'CONNECTING',
  CONNECTED = 'CONNECTED',
  DISCONNECTED = 'DISCONNECTED',
  ERROR = 'ERROR',
  CLOSED = 'CLOSED',
}

/**
 * SSE Event Types for Job Progress
 */
export interface JobStatusEvent {
  jobId: string;
  status: string;
  stage: string;
  percent: number;
  message?: string;
}

export interface StepProgressEvent {
  jobId: string;
  step: string;
  phase: string;
  progressPct: number;
  message: string;
  substageDetail?: string;
  currentItem?: number;
  totalItems?: number;
  elapsedTime?: string;
  estimatedTimeRemaining?: string;
}

export interface JobCompletedEvent {
  jobId: string;
  artifacts: Array<{
    name: string;
    path: string;
    type: string;
    sizeBytes: number;
  }>;
  output: {
    videoPath: string;
    subtitlePath?: string;
  };
}

export interface JobFailedEvent {
  jobId: string;
  errorMessage: string;
  errors: Array<{ message: string }>;
  logs: string[];
}

export interface JobCancelledEvent {
  jobId: string;
  message: string;
}

/**
 * SSE Connection State Interface
 */
export interface SseConnectionState {
  status: 'connecting' | 'connected' | 'disconnected' | 'error';
  reconnectAttempt: number;
  lastEventId: string | null;
}

/**
 * SSE Event with data
 */
export interface SseEvent<T = unknown> {
  type: string;
  data: T;
  id?: string;
}

export class SSEClient {
  private eventSource: EventSource | null = null;
  private options: Required<SSEConnectionOptions>;
  private state: SSEConnectionState = SSEConnectionState.DISCONNECTED;
  private retryCount = 0;
  private reconnectTimer: number | null = null;
  private timeoutTimer: number | null = null;
  private isCancelled = false;

  constructor(options: SSEConnectionOptions) {
    this.options = {
      onError: () => {},
      onOpen: () => {},
      onClose: () => {},
      maxRetries: 5,
      retryDelay: 1000,
      timeout: 300000, // 5 minutes default timeout
      ...options,
    };
  }

  /**
   * Connect to SSE endpoint
   */
  public connect(): void {
    if (this.isCancelled) {
      loggingService.debug('SSE connection cancelled', 'sseClient', 'connect');
      return;
    }

    if (this.state === SSEConnectionState.CONNECTED) {
      loggingService.warn('SSE already connected', 'sseClient', 'connect');
      return;
    }

    this.setState(SSEConnectionState.CONNECTING);

    try {
      loggingService.info('Connecting to SSE', 'sseClient', 'connect', {
        url: this.options.url,
        retry: this.retryCount,
      });

      this.eventSource = new EventSource(this.options.url);

      this.eventSource.onopen = () => {
        loggingService.info('SSE connection opened', 'sseClient', 'open');
        this.setState(SSEConnectionState.CONNECTED);
        this.retryCount = 0;
        this.clearReconnectTimer();
        this.startTimeoutTimer();
        this.options.onOpen();
      };

      this.eventSource.onmessage = (event: MessageEvent) => {
        this.resetTimeoutTimer();

        loggingService.debug('SSE message received', 'sseClient', 'message', {
          data: event.data?.substring(0, 100),
        });

        try {
          this.options.onMessage(event);
        } catch (error) {
          loggingService.error(
            'Error handling SSE message',
            toError(error),
            'sseClient',
            'message'
          );
        }
      };

      this.eventSource.onerror = () => {
        loggingService.error(
          'SSE connection error',
          new Error('EventSource error'),
          'sseClient',
          'error',
          { readyState: this.eventSource?.readyState }
        );

        this.setState(SSEConnectionState.ERROR);
        this.clearTimeoutTimer();

        const error = new Error(
          `SSE connection error (readyState: ${this.eventSource?.readyState})`
        );
        this.options.onError(error);

        // Attempt reconnection if not cancelled and within retry limit
        if (!this.isCancelled && this.retryCount < this.options.maxRetries) {
          this.scheduleReconnect();
        } else {
          this.close();
        }
      };
    } catch (error) {
      loggingService.error('Failed to create EventSource', toError(error), 'sseClient', 'connect');
      this.setState(SSEConnectionState.ERROR);
      this.options.onError(toError(error));

      if (!this.isCancelled && this.retryCount < this.options.maxRetries) {
        this.scheduleReconnect();
      }
    }
  }

  /**
   * Schedule automatic reconnection
   */
  private scheduleReconnect(): void {
    this.retryCount++;
    const delay = Math.min(this.options.retryDelay * Math.pow(2, this.retryCount - 1), 30000);

    loggingService.info('Scheduling SSE reconnect', 'sseClient', 'reconnect', {
      attempt: this.retryCount,
      maxRetries: this.options.maxRetries,
      delay,
    });

    this.clearReconnectTimer();
    this.reconnectTimer = window.setTimeout(() => {
      this.close();
      this.connect();
    }, delay);
  }

  /**
   * Start timeout timer
   */
  private startTimeoutTimer(): void {
    this.clearTimeoutTimer();

    if (this.options.timeout > 0) {
      this.timeoutTimer = window.setTimeout(() => {
        loggingService.warn('SSE connection timeout', 'sseClient', 'timeout');
        this.close();
        this.options.onError(new Error('SSE connection timeout'));
      }, this.options.timeout);
    }
  }

  /**
   * Reset timeout timer (called on each message received)
   */
  private resetTimeoutTimer(): void {
    this.clearTimeoutTimer();
    this.startTimeoutTimer();
  }

  /**
   * Clear timeout timer
   */
  private clearTimeoutTimer(): void {
    if (this.timeoutTimer !== null) {
      window.clearTimeout(this.timeoutTimer);
      this.timeoutTimer = null;
    }
  }

  /**
   * Clear reconnect timer
   */
  private clearReconnectTimer(): void {
    if (this.reconnectTimer !== null) {
      window.clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }
  }

  /**
   * Update connection state
   */
  private setState(state: SSEConnectionState): void {
    const previousState = this.state;
    this.state = state;

    if (previousState !== state) {
      loggingService.debug('SSE state changed', 'sseClient', 'state', {
        from: previousState,
        to: state,
      });
    }
  }

  /**
   * Get current connection state
   */
  public getState(): SSEConnectionState {
    return this.state;
  }

  /**
   * Check if connected
   */
  public isConnected(): boolean {
    return this.state === SSEConnectionState.CONNECTED;
  }

  /**
   * Close SSE connection
   */
  public close(): void {
    loggingService.info('Closing SSE connection', 'sseClient', 'close');

    this.clearReconnectTimer();
    this.clearTimeoutTimer();

    if (this.eventSource) {
      this.eventSource.close();
      this.eventSource = null;
    }

    this.setState(SSEConnectionState.CLOSED);
    this.options.onClose();
  }

  /**
   * Cancel connection and prevent reconnection
   */
  public cancel(): void {
    loggingService.info('Cancelling SSE connection', 'sseClient', 'cancel');
    this.isCancelled = true;
    this.close();
  }
}

/**
 * Parse SSE event data as JSON
 */
export function parseSSEJson<T = unknown>(event: MessageEvent): T | null {
  try {
    return JSON.parse(event.data);
  } catch (error) {
    loggingService.error('Failed to parse SSE JSON', toError(error), 'sseClient', 'parse');
    return null;
  }
}

/**
 * Create an SSE client for generation progress
 */
export function createGenerationProgressSSE(
  jobId: string,
  onProgress: (progress: GenerationProgressEvent) => void,
  onError?: (error: Error) => void,
  onComplete?: () => void
): SSEClient {
  const baseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5005';
  const url = `${baseUrl}/api/generation/progress/${jobId}`;

  return new SSEClient({
    url,
    onMessage: (event) => {
      const progress = parseSSEJson<GenerationProgressEvent>(event);
      if (progress) {
        onProgress(progress);

        // Auto-close on completion
        if (progress.stage === 'Completed' || progress.stage === 'Failed') {
          onComplete?.();
        }
      }
    },
    onError: (error) => {
      loggingService.error('Generation progress SSE error', error, 'sseClient', 'generation');
      onError?.(error);
    },
    maxRetries: 3,
    retryDelay: 2000,
    timeout: 600000, // 10 minutes for long-running generations
  });
}

/**
 * Generation progress event from SSE
 */
export interface GenerationProgressEvent {
  jobId: string;
  stage: string;
  overallPercent: number;
  stagePercent: number;
  message?: string;
  currentItem?: number;
  totalItems?: number;
  elapsedTime?: string;
  estimatedTimeRemaining?: string;
  error?: string;
}

/**
 * Event-based SSE Client for Job Progress
 * Supports multiple event types and provides a cleaner API for job tracking
 */
export class SseClient {
  private eventSource: EventSource | null = null;
  private eventHandlers: Map<string, Array<(event: SseEvent) => void>> = new Map();
  private statusChangeHandlers: Array<(state: SseConnectionState) => void> = [];
  private connectionState: SseConnectionState = {
    status: 'disconnected',
    reconnectAttempt: 0,
    lastEventId: null,
  };
  private jobId: string;
  private reconnectTimer: number | null = null;
  private maxReconnectAttempts = 5;
  private reconnectDelay = 3000;

  constructor(jobId: string) {
    this.jobId = jobId;
  }

  /**
   * Register event handler
   */
  on(eventType: string, handler: (event: SseEvent) => void): void {
    if (!this.eventHandlers.has(eventType)) {
      this.eventHandlers.set(eventType, []);
    }
    this.eventHandlers.get(eventType)!.push(handler);
  }

  /**
   * Register status change handler
   */
  onStatusChange(handler: (state: SseConnectionState) => void): void {
    this.statusChangeHandlers.push(handler);
  }

  /**
   * Update connection state and notify handlers
   */
  private updateConnectionState(updates: Partial<SseConnectionState>): void {
    this.connectionState = {
      ...this.connectionState,
      ...updates,
    };

    this.statusChangeHandlers.forEach((handler) => {
      try {
        handler(this.connectionState);
      } catch (error) {
        loggingService.error(
          'Error in status change handler',
          toError(error),
          'SseClient',
          'updateConnectionState'
        );
      }
    });
  }

  /**
   * Connect to SSE endpoint
   */
  connect(): void {
    if (this.eventSource) {
      loggingService.warn('SSE already connected', 'SseClient', 'connect');
      return;
    }

    const baseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5005';
    const url = `${baseUrl}/api/jobs/${this.jobId}/events`;

    loggingService.info('Connecting to SSE', 'SseClient', 'connect', { url, jobId: this.jobId });

    this.updateConnectionState({ status: 'connecting' });

    try {
      this.eventSource = new EventSource(url);

      this.eventSource.onopen = () => {
        loggingService.info('SSE connection opened', 'SseClient', 'onopen', { jobId: this.jobId });
        this.updateConnectionState({
          status: 'connected',
          reconnectAttempt: 0,
        });
      };

      this.eventSource.onerror = () => {
        loggingService.error(
          'SSE connection error',
          new Error('EventSource error'),
          'SseClient',
          'onerror',
          { jobId: this.jobId }
        );

        this.updateConnectionState({ status: 'error' });

        if (this.connectionState.reconnectAttempt < this.maxReconnectAttempts && this.eventSource) {
          const attempt = this.connectionState.reconnectAttempt + 1;
          const delay = this.reconnectDelay * Math.pow(2, attempt - 1);

          loggingService.info('Scheduling SSE reconnect', 'SseClient', 'onerror', {
            attempt,
            delay,
            jobId: this.jobId,
          });

          this.updateConnectionState({ reconnectAttempt: attempt });

          this.reconnectTimer = window.setTimeout(() => {
            this.close();
            this.connect();
          }, delay);
        } else {
          this.close();
        }
      };

      const eventTypes = [
        'job-status',
        'step-progress',
        'step-status',
        'job-completed',
        'job-failed',
        'job-cancelled',
        'warning',
        'error',
      ];

      eventTypes.forEach((eventType) => {
        this.eventSource!.addEventListener(eventType, (event: Event) => {
          const messageEvent = event as MessageEvent;

          loggingService.debug('SSE event received', 'SseClient', eventType, {
            jobId: this.jobId,
          });

          try {
            const data = JSON.parse(messageEvent.data);

            if (messageEvent.lastEventId) {
              this.updateConnectionState({ lastEventId: messageEvent.lastEventId });
            }

            const sseEvent: SseEvent = {
              type: eventType,
              data,
              id: messageEvent.lastEventId,
            };

            const handlers = this.eventHandlers.get(eventType) || [];
            handlers.forEach((handler) => {
              try {
                handler(sseEvent);
              } catch (error) {
                loggingService.error(
                  `Error in ${eventType} handler`,
                  toError(error),
                  'SseClient',
                  'eventHandler'
                );
              }
            });
          } catch (error) {
            loggingService.error(
              `Failed to parse ${eventType} event`,
              toError(error),
              'SseClient',
              'eventHandler'
            );
          }
        });
      });
    } catch (error) {
      loggingService.error('Failed to create EventSource', toError(error), 'SseClient', 'connect');
      this.updateConnectionState({ status: 'error' });
    }
  }

  /**
   * Close SSE connection
   */
  close(): void {
    if (this.reconnectTimer !== null) {
      window.clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }

    if (this.eventSource) {
      loggingService.info('Closing SSE connection', 'SseClient', 'close', { jobId: this.jobId });
      this.eventSource.close();
      this.eventSource = null;
    }

    this.updateConnectionState({ status: 'disconnected' });
  }

  /**
   * Check if connected
   */
  isConnected(): boolean {
    return this.connectionState.status === 'connected';
  }

  /**
   * Get current connection state
   */
  getConnectionState(): SseConnectionState {
    return this.connectionState;
  }
}

/**
 * Factory function to create SSE client for job progress
 */
export function createSseClient(jobId: string): SseClient {
  return new SseClient(jobId);
}
