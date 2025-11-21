/**
 * Backend Health Service
 *
 * Provides robust health checking and status monitoring for the backend service.
 * The backend process is managed by Electron (Aura.Desktop/electron/backend-service.js).
 * This service handles frontend-side health checking with retry logic and exponential backoff.
 *
 * Note: This is for health CHECKING only. Process management (spawn/kill) is handled
 * by Electron's BackendService in Aura.Desktop, not in browser-based code.
 */

import axios, { type AxiosError } from 'axios';
import { loggingService } from './loggingService';

export interface BackendHealthStatus {
  reachable: boolean;
  healthy: boolean;
  lastChecked: Date;
  error: string | null;
  responseTime: number | null;
}

export interface BackendHealthCheckOptions {
  timeout?: number;
  maxRetries?: number;
  retryDelay?: number;
  exponentialBackoff?: boolean;
}

export class BackendHealthService {
  private baseUrl: string;
  private healthEndpoint: string;
  private status: BackendHealthStatus = {
    reachable: false,
    healthy: false,
    lastChecked: new Date(),
    error: 'Not checked yet',
    responseTime: null,
  };

  constructor(baseUrl?: string) {
    this.baseUrl = baseUrl || import.meta.env.VITE_API_BASE_URL || 'http://localhost:5005';
    this.healthEndpoint = '/healthz/simple';
  }

  /**
   * Check backend health with retry logic
   * @param options Health check options
   * @returns Health status
   */
  async checkHealth(options: BackendHealthCheckOptions = {}): Promise<BackendHealthStatus> {
    const {
      timeout = 5000,
      maxRetries = 10,
      retryDelay = 1000,
      exponentialBackoff = true,
    } = options;

    let lastError: Error | null = null;
    let attempt = 0;

    while (attempt < maxRetries) {
      attempt++;

      try {
        const startTime = Date.now();
        const response = await axios.get(`${this.baseUrl}${this.healthEndpoint}`, {
          timeout,
          validateStatus: (status) => status < 500,
        });

        const responseTime = Date.now() - startTime;

        if (response.status === 200 && response.data?.status === 'ok') {
          this.status = {
            reachable: true,
            healthy: true,
            lastChecked: new Date(),
            error: null,
            responseTime,
          };

          loggingService.info('BackendHealthService', 'Backend is healthy', undefined, {
            attempt,
            responseTime,
            url: `${this.baseUrl}${this.healthEndpoint}`,
          });

          return this.status;
        } else {
          throw new Error(`Backend returned status ${response.status}`);
        }
      } catch (error: unknown) {
        const errorObj = error instanceof Error ? error : new Error(String(error));
        lastError = errorObj;

        const axiosError = error as AxiosError;
        const isNetworkError =
          axiosError.code === 'ECONNREFUSED' ||
          axiosError.code === 'ETIMEDOUT' ||
          axiosError.message?.toLowerCase().includes('network') ||
          axiosError.message?.toLowerCase().includes('timeout');

        loggingService.warn(
          'BackendHealthService',
          `Health check attempt ${attempt}/${maxRetries} failed`,
          undefined,
          {
            error: errorObj.message,
            code: axiosError.code,
            isNetworkError,
            willRetry: attempt < maxRetries,
          }
        );

        if (attempt < maxRetries) {
          const delay = exponentialBackoff ? retryDelay * Math.pow(2, attempt - 1) : retryDelay;

          await new Promise((resolve) => setTimeout(resolve, delay));
        }
      }
    }

    this.status = {
      reachable: false,
      healthy: false,
      lastChecked: new Date(),
      error: lastError?.message || 'Backend not reachable',
      responseTime: null,
    };

    loggingService.error(
      'BackendHealthService',
      lastError || new Error('Backend not reachable'),
      undefined,
      'Health check failed after all retries',
      {
        maxRetries,
        url: `${this.baseUrl}${this.healthEndpoint}`,
      }
    );

    return this.status;
  }

  /**
   * Quick health check without retries
   * @param timeout Request timeout in milliseconds
   * @returns Health status
   */
  async quickCheck(timeout = 2000): Promise<BackendHealthStatus> {
    return this.checkHealth({
      timeout,
      maxRetries: 1,
      retryDelay: 0,
      exponentialBackoff: false,
    });
  }

  /**
   * Wait for backend to become healthy
   * @param timeout Maximum wait time in milliseconds
   * @param checkInterval Interval between checks in milliseconds
   * @returns True if backend became healthy, false if timeout
   */
  async waitForHealthy(timeout = 30000, checkInterval = 1000): Promise<boolean> {
    const startTime = Date.now();
    let attempt = 0;

    loggingService.info(
      'BackendHealthService',
      'Waiting for backend to become healthy',
      undefined,
      {
        timeout,
        checkInterval,
        url: `${this.baseUrl}${this.healthEndpoint}`,
      }
    );

    while (Date.now() - startTime < timeout) {
      attempt++;

      const status = await this.quickCheck(2000);

      if (status.healthy) {
        loggingService.info('BackendHealthService', 'Backend is now healthy', undefined, {
          attempt,
          elapsedTime: Date.now() - startTime,
        });
        return true;
      }

      if (Date.now() - startTime + checkInterval < timeout) {
        await new Promise((resolve) => setTimeout(resolve, checkInterval));
      }
    }

    loggingService.warn(
      'BackendHealthService',
      'Backend did not become healthy within timeout',
      undefined,
      {
        timeout,
        attempts: attempt,
      }
    );

    return false;
  }

  /**
   * Get current cached status (no network request)
   * @returns Current health status
   */
  getStatus(): BackendHealthStatus {
    return { ...this.status };
  }

  /**
   * Get backend base URL
   * @returns Base URL
   */
  getBaseUrl(): string {
    return this.baseUrl;
  }

  /**
   * Update backend base URL
   * @param url New base URL
   */
  setBaseUrl(url: string): void {
    this.baseUrl = url;
    loggingService.info('BackendHealthService', 'Backend URL updated', undefined, { url });
  }
}

/**
 * Singleton instance for global use
 */
export const backendHealthService = new BackendHealthService();
