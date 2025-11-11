/**
 * Network Resilience Service
 * Handles offline request queueing, automatic retry, and network state management
 */

import { useAppStore } from '../stores/appStore';
import { loggingService } from './loggingService';

export interface QueuedRequest {
  id: string;
  url: string;
  method: string;
  data?: unknown;
  timestamp: number;
  retryCount: number;
  maxRetries: number;
  priority: 'high' | 'normal' | 'low';
}

export interface NetworkResilienceConfig {
  enableOfflineQueue: boolean;
  maxQueueSize: number;
  autoRetryOnReconnect: boolean;
  queuePersistence: boolean;
}

class NetworkResilienceService {
  private requestQueue: QueuedRequest[] = [];
  private readonly STORAGE_KEY = 'aura_offline_request_queue';
  private isProcessingQueue = false;

  private config: NetworkResilienceConfig = {
    enableOfflineQueue: true,
    maxQueueSize: 50,
    autoRetryOnReconnect: true,
    queuePersistence: true,
  };

  constructor() {
    this.loadQueueFromStorage();
    this.setupNetworkListeners();
  }

  /**
   * Configure the resilience service
   */
  public configure(config: Partial<NetworkResilienceConfig>): void {
    this.config = { ...this.config, ...config };
    loggingService.info('Network resilience configured', 'networkResilience', 'configure', {
      config: this.config,
    });
  }

  /**
   * Queue a request for later execution when network is restored
   */
  public queueRequest(
    url: string,
    method: string,
    data?: unknown,
    options: { priority?: 'high' | 'normal' | 'low'; maxRetries?: number } = {}
  ): string {
    if (!this.config.enableOfflineQueue) {
      throw new Error('Offline queue is disabled');
    }

    const request: QueuedRequest = {
      id: crypto.randomUUID(),
      url,
      method,
      data,
      timestamp: Date.now(),
      retryCount: 0,
      maxRetries: options.maxRetries ?? 3,
      priority: options.priority ?? 'normal',
    };

    // Check if we need to remove lowest priority item before adding new one
    if (this.requestQueue.length >= this.config.maxQueueSize) {
      this.removeLowestPriorityRequest(request);
    } else {
      this.requestQueue.push(request);
      this.saveQueueToStorage();
    }

    loggingService.info('Request queued for offline execution', 'networkResilience', 'queue', {
      id: request.id,
      url,
      method,
      queueSize: this.requestQueue.length,
    });

    return request.id;
  }

  /**
   * Remove a request from the queue
   */
  public removeRequest(id: string): boolean {
    const initialLength = this.requestQueue.length;
    this.requestQueue = this.requestQueue.filter((req) => req.id !== id);
    const removed = this.requestQueue.length < initialLength;

    if (removed) {
      this.saveQueueToStorage();
      loggingService.debug('Request removed from queue', 'networkResilience', 'remove', { id });
    }

    return removed;
  }

  /**
   * Get queued requests
   */
  public getQueuedRequests(): QueuedRequest[] {
    return [...this.requestQueue];
  }

  /**
   * Clear all queued requests
   */
  public clearQueue(): void {
    this.requestQueue = [];
    this.saveQueueToStorage();
    loggingService.info('Request queue cleared', 'networkResilience', 'clear');
  }

  /**
   * Process queued requests when network is restored
   */
  public async processQueue(
    executeRequest: (request: QueuedRequest) => Promise<boolean>
  ): Promise<void> {
    if (this.isProcessingQueue || this.requestQueue.length === 0) {
      return;
    }

    this.isProcessingQueue = true;
    loggingService.info(
      `Processing ${this.requestQueue.length} queued requests`,
      'networkResilience',
      'process'
    );

    // Sort by priority (high > normal > low) and timestamp (older first)
    const sortedQueue = this.sortQueueByPriority();

    for (const request of sortedQueue) {
      try {
        const success = await executeRequest(request);

        if (success) {
          this.removeRequest(request.id);
          loggingService.info(
            'Queued request executed successfully',
            'networkResilience',
            'execute',
            {
              id: request.id,
              url: request.url,
            }
          );
        } else {
          // Find the request in the queue and update it
          const queuedRequest = this.requestQueue.find((r) => r.id === request.id);
          if (queuedRequest) {
            queuedRequest.retryCount++;
            if (queuedRequest.retryCount >= queuedRequest.maxRetries) {
              this.removeRequest(queuedRequest.id);
              loggingService.warn(
                'Queued request exceeded max retries',
                'networkResilience',
                'retry',
                {
                  id: queuedRequest.id,
                  url: queuedRequest.url,
                  retryCount: queuedRequest.retryCount,
                }
              );
            } else {
              this.saveQueueToStorage();
            }
          }
        }
      } catch (error) {
        loggingService.error(
          'Error processing queued request',
          error as Error,
          'networkResilience',
          'error',
          { id: request.id, url: request.url }
        );
      }
    }

    this.isProcessingQueue = false;
  }

  /**
   * Check if network is currently online
   */
  public isOnline(): boolean {
    return useAppStore.getState().isOnline;
  }

  /**
   * Setup network event listeners
   */
  private setupNetworkListeners(): void {
    if (typeof window === 'undefined') return;

    window.addEventListener('online', () => {
      loggingService.info('Network connection restored', 'networkResilience', 'online');

      if (this.config.autoRetryOnReconnect && this.requestQueue.length > 0) {
        loggingService.info(
          `Auto-retrying ${this.requestQueue.length} queued requests`,
          'networkResilience',
          'autoRetry'
        );
      }
    });

    window.addEventListener('offline', () => {
      loggingService.warn('Network connection lost', 'networkResilience', 'offline');
    });
  }

  /**
   * Save queue to localStorage
   */
  private saveQueueToStorage(): void {
    if (!this.config.queuePersistence) return;

    try {
      localStorage.setItem(this.STORAGE_KEY, JSON.stringify(this.requestQueue));
    } catch (error) {
      loggingService.warn(
        'Failed to save request queue to storage',
        'networkResilience',
        'storage',
        { error: String(error) }
      );
    }
  }

  /**
   * Load queue from localStorage
   */
  private loadQueueFromStorage(): void {
    if (!this.config.queuePersistence) return;

    try {
      const stored = localStorage.getItem(this.STORAGE_KEY);
      if (stored) {
        this.requestQueue = JSON.parse(stored);
        loggingService.info(
          `Loaded ${this.requestQueue.length} requests from storage`,
          'networkResilience',
          'load'
        );
      }
    } catch (error) {
      loggingService.warn(
        'Failed to load request queue from storage',
        'networkResilience',
        'storage',
        { error: String(error) }
      );
    }
  }

  /**
   * Sort queue by priority and timestamp
   */
  private sortQueueByPriority(): QueuedRequest[] {
    const priorityOrder = { high: 0, normal: 1, low: 2 };

    return [...this.requestQueue].sort((a, b) => {
      const priorityDiff = priorityOrder[a.priority] - priorityOrder[b.priority];
      if (priorityDiff !== 0) return priorityDiff;
      return a.timestamp - b.timestamp;
    });
  }

  /**
   * Remove lowest priority request when queue is full
   * Only removes if the new request has higher priority than the lowest item
   */
  private removeLowestPriorityRequest(newRequest: QueuedRequest): void {
    const sorted = this.sortQueueByPriority();
    const lowestPriority = sorted[sorted.length - 1];

    if (!lowestPriority) {
      // Queue is somehow empty, just add the new request
      this.requestQueue.push(newRequest);
      this.saveQueueToStorage();
      return;
    }

    const priorityOrder = { high: 0, normal: 1, low: 2 };
    const newPriority = priorityOrder[newRequest.priority];
    const lowestPriorityValue = priorityOrder[lowestPriority.priority];

    // If new request has higher or equal priority, replace lowest
    // If same priority, newer timestamp wins (remove older one)
    if (
      newPriority < lowestPriorityValue ||
      (newPriority === lowestPriorityValue && newRequest.timestamp > lowestPriority.timestamp)
    ) {
      this.removeRequest(lowestPriority.id);
      this.requestQueue.push(newRequest);
      this.saveQueueToStorage();
      loggingService.warn(
        'Removed lowest priority request due to queue limit',
        'networkResilience',
        'limit',
        {
          id: lowestPriority.id,
          url: lowestPriority.url,
        }
      );
    } else {
      // New request has lower priority, don't add it
      loggingService.warn(
        'Rejected low priority request due to queue limit',
        'networkResilience',
        'limit',
        {
          id: newRequest.id,
          url: newRequest.url,
        }
      );
    }
  }
}

export const networkResilienceService = new NetworkResilienceService();
