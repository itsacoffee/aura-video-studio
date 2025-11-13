/**
 * Hybrid API Client
 *
 * Provides a unified API client that works in both web and Electron environments.
 * Uses transport abstraction to automatically select HTTP or IPC based on environment.
 */

import { env } from '../../config/env';
import { loggingService } from '../loggingService';
import {
  IApiTransport,
  TransportFactory,
  TransportRequestOptions,
  TransportResponse,
  SSESubscriptionOptions,
  UploadOptions,
  DownloadOptions,
} from './transport';

/**
 * API Client wrapper that uses transport abstraction
 */
export class ApiClient {
  private transport: IApiTransport;

  constructor(baseURL?: string) {
    const url = baseURL || env.apiBaseUrl;
    this.transport = TransportFactory.create(url);

    loggingService.info('API Client initialized', 'ApiClient', 'constructor', {
      transport: this.transport.getName(),
      environment: TransportFactory.getEnvironment(),
    });
  }

  /**
   * GET request
   */
  async get<T = unknown>(endpoint: string, options?: TransportRequestOptions): Promise<T> {
    const response = await this.transport.request<T>(endpoint, 'GET', undefined, options);
    return response.data;
  }

  /**
   * POST request
   */
  async post<T = unknown>(
    endpoint: string,
    data?: unknown,
    options?: TransportRequestOptions
  ): Promise<T> {
    const response = await this.transport.request<T>(endpoint, 'POST', data, options);
    return response.data;
  }

  /**
   * PUT request
   */
  async put<T = unknown>(
    endpoint: string,
    data?: unknown,
    options?: TransportRequestOptions
  ): Promise<T> {
    const response = await this.transport.request<T>(endpoint, 'PUT', data, options);
    return response.data;
  }

  /**
   * PATCH request
   */
  async patch<T = unknown>(
    endpoint: string,
    data?: unknown,
    options?: TransportRequestOptions
  ): Promise<T> {
    const response = await this.transport.request<T>(endpoint, 'PATCH', data, options);
    return response.data;
  }

  /**
   * DELETE request
   */
  async delete<T = unknown>(endpoint: string, options?: TransportRequestOptions): Promise<T> {
    const response = await this.transport.request<T>(endpoint, 'DELETE', undefined, options);
    return response.data;
  }

  /**
   * Subscribe to SSE events
   */
  subscribe(endpoint: string, options: SSESubscriptionOptions): () => void {
    return this.transport.subscribe(endpoint, options);
  }

  /**
   * Upload file with progress
   */
  async uploadFile<T = unknown>(
    endpoint: string,
    file: File,
    onProgress?: (progress: number) => void,
    options?: TransportRequestOptions
  ): Promise<T> {
    const uploadOptions: UploadOptions = {
      ...options,
      onProgress: onProgress ? (progress) => onProgress(progress.percentage) : undefined,
    };

    const response = await this.transport.upload<T>(endpoint, file, uploadOptions);
    return response.data;
  }

  /**
   * Download file with progress
   */
  async downloadFile(
    endpoint: string,
    filename: string,
    onProgress?: (progress: number) => void,
    options?: TransportRequestOptions
  ): Promise<void> {
    const downloadOptions: DownloadOptions = {
      ...options,
      filename,
      onProgress: onProgress ? (progress) => onProgress(progress.percentage) : undefined,
    };

    await this.transport.download(endpoint, downloadOptions);
  }

  /**
   * Get transport name for debugging
   */
  getTransportName(): string {
    return this.transport.getName();
  }

  /**
   * Check if running in Electron
   */
  isElectron(): boolean {
    return TransportFactory.isElectron();
  }

  /**
   * Get current environment
   */
  getEnvironment(): 'electron' | 'web' {
    return TransportFactory.getEnvironment();
  }
}

/**
 * Create singleton instance
 */
export const apiClient = new ApiClient();

/**
 * Export convenience functions that match the original API
 */
export async function get<T>(url: string, options?: TransportRequestOptions): Promise<T> {
  return apiClient.get<T>(url, options);
}

export async function post<T>(
  url: string,
  data?: unknown,
  options?: TransportRequestOptions
): Promise<T> {
  return apiClient.post<T>(url, data, options);
}

export async function put<T>(
  url: string,
  data?: unknown,
  options?: TransportRequestOptions
): Promise<T> {
  return apiClient.put<T>(url, data, options);
}

export async function patch<T>(
  url: string,
  data?: unknown,
  options?: TransportRequestOptions
): Promise<T> {
  return apiClient.patch<T>(url, data, options);
}

export async function del<T>(url: string, options?: TransportRequestOptions): Promise<T> {
  return apiClient.delete<T>(url, options);
}

export async function uploadFile<T>(
  url: string,
  file: File,
  onProgress?: (progress: number) => void,
  options?: TransportRequestOptions
): Promise<T> {
  return apiClient.uploadFile<T>(url, file, onProgress, options);
}

export async function downloadFile(
  url: string,
  filename: string,
  onProgress?: (progress: number) => void,
  options?: TransportRequestOptions
): Promise<void> {
  return apiClient.downloadFile(url, filename, onProgress, options);
}

/**
 * Export for use as default client
 */
export default apiClient;
