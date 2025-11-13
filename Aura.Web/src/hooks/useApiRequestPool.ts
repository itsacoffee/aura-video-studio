import axios, { AxiosInstance, AxiosRequestConfig } from 'axios';
import { useCallback, useRef } from 'react';

/**
 * API Request Pool Manager
 * Limits concurrent API requests to prevent overwhelming the backend
 * and reduces memory usage from too many simultaneous connections
 */
class ApiRequestPool {
  private queue: Array<{
    config: AxiosRequestConfig;
    resolve: (value: unknown) => void;
    reject: (error: unknown) => void;
  }> = [];
  private activeRequests = 0;
  private maxConcurrent: number;
  private axiosInstance: AxiosInstance;

  constructor(maxConcurrent = 6, axiosInstance?: AxiosInstance) {
    this.maxConcurrent = maxConcurrent;
    this.axiosInstance = axiosInstance || axios;
  }

  async request<T>(config: AxiosRequestConfig): Promise<T> {
    if (this.activeRequests < this.maxConcurrent) {
      return this.executeRequest<T>(config);
    }

    return new Promise<T>((resolve, reject) => {
      this.queue.push({ config, resolve: resolve as (value: unknown) => void, reject });
    });
  }

  private async executeRequest<T>(config: AxiosRequestConfig): Promise<T> {
    this.activeRequests++;

    try {
      const response = await this.axiosInstance.request<T>(config);
      return response.data;
    } finally {
      this.activeRequests--;
      this.processQueue();
    }
  }

  private processQueue(): void {
    if (this.queue.length === 0 || this.activeRequests >= this.maxConcurrent) {
      return;
    }

    const next = this.queue.shift();
    if (next) {
      this.executeRequest(next.config).then(next.resolve).catch(next.reject);
    }
  }

  getStats() {
    return {
      activeRequests: this.activeRequests,
      queuedRequests: this.queue.length,
      maxConcurrent: this.maxConcurrent,
    };
  }

  setMaxConcurrent(max: number) {
    this.maxConcurrent = max;
    this.processQueue();
  }
}

const defaultPool = new ApiRequestPool();

export function useApiRequestPool(maxConcurrent = 6) {
  const poolRef = useRef<ApiRequestPool>(new ApiRequestPool(maxConcurrent));

  const request = useCallback(async <T>(config: AxiosRequestConfig): Promise<T> => {
    return poolRef.current.request<T>(config);
  }, []);

  const getStats = useCallback(() => {
    return poolRef.current.getStats();
  }, []);

  const setMaxConcurrent = useCallback((max: number) => {
    poolRef.current.setMaxConcurrent(max);
  }, []);

  return {
    request,
    getStats,
    setMaxConcurrent,
  };
}

export { ApiRequestPool, defaultPool };
