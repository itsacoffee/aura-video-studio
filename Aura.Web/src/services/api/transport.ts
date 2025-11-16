/**
 * API Transport Abstraction Layer
 *
 * Provides a unified interface for HTTP (Axios) and IPC (Electron) communication.
 * Automatically detects the runtime environment and uses the appropriate transport.
 */

import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse, AxiosError } from 'axios';
import { loggingService } from '../loggingService';

/**
 * Transport request options
 */
export interface TransportRequestOptions {
  timeout?: number;
  signal?: AbortSignal;
  headers?: Record<string, string>;
  skipRetry?: boolean;
  skipCircuitBreaker?: boolean;
  skipDeduplication?: boolean;
}

/**
 * Transport response wrapper
 */
export interface TransportResponse<T = unknown> {
  data: T;
  status: number;
  statusText: string;
  headers: Record<string, string>;
}

/**
 * SSE subscription options
 */
export interface SSESubscriptionOptions {
  onMessage: (event: MessageEvent) => void;
  onError?: (error: Error) => void;
  onOpen?: () => void;
  onClose?: () => void;
  maxRetries?: number;
  retryDelay?: number;
  timeout?: number;
}

/**
 * Progress callback for uploads/downloads
 */
export interface ProgressCallback {
  (progress: { loaded: number; total: number; percentage: number }): void;
}

/**
 * Upload options
 */
export interface UploadOptions extends TransportRequestOptions {
  onProgress?: ProgressCallback;
}

/**
 * Download options
 */
export interface DownloadOptions extends TransportRequestOptions {
  onProgress?: ProgressCallback;
  filename?: string;
}

/**
 * API Transport Interface
 * Defines the contract for all transport implementations
 */
export interface IApiTransport {
  /**
   * Make a generic request
   */
  request<T = unknown>(
    endpoint: string,
    method: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE',
    data?: unknown,
    options?: TransportRequestOptions
  ): Promise<TransportResponse<T>>;

  /**
   * Subscribe to SSE events
   * Returns unsubscribe function
   */
  subscribe(endpoint: string, options: SSESubscriptionOptions): () => void;

  /**
   * Upload file with progress
   */
  upload<T = unknown>(
    endpoint: string,
    file: File,
    options?: UploadOptions
  ): Promise<TransportResponse<T>>;

  /**
   * Download file with progress
   */
  download(endpoint: string, options?: DownloadOptions): Promise<void>;

  /**
   * Check if transport is available
   */
  isAvailable(): boolean;

  /**
   * Get transport name for logging
   */
  getName(): string;
}

/**
 * HTTP Transport Implementation (for web and development)
 * Uses Axios for all HTTP communication
 */
export class HttpTransport implements IApiTransport {
  private axiosInstance: AxiosInstance;
  private baseURL: string;

  constructor(baseURL: string) {
    this.baseURL = baseURL;
    this.axiosInstance = axios.create({
      baseURL,
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    loggingService.info('HTTP Transport initialized', 'HttpTransport', 'constructor', {
      baseURL,
    });
  }

  getName(): string {
    return 'HTTP';
  }

  isAvailable(): boolean {
    return true;
  }

  async request<T = unknown>(
    endpoint: string,
    method: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE',
    data?: unknown,
    options?: TransportRequestOptions
  ): Promise<TransportResponse<T>> {
    try {
      const config: AxiosRequestConfig = {
        method,
        url: endpoint,
        data: method !== 'GET' ? data : undefined,
        params: method === 'GET' ? data : undefined,
        timeout: options?.timeout,
        signal: options?.signal,
        headers: options?.headers,
      };

      const response: AxiosResponse<T> = await this.axiosInstance.request(config);

      return {
        data: response.data,
        status: response.status,
        statusText: response.statusText,
        headers: response.headers as Record<string, string>,
      };
    } catch (error) {
      throw this.mapError(error as AxiosError);
    }
  }

  subscribe(endpoint: string, options: SSESubscriptionOptions): () => void {
    const url = `${this.baseURL}${endpoint}`;
    let eventSource: EventSource | null = null;
    let retryCount = 0;
    let reconnectTimer: number | null = null;
    const maxRetries = options.maxRetries ?? 5;
    const retryDelay = options.retryDelay ?? 1000;

    const connect = () => {
      try {
        eventSource = new EventSource(url);

        eventSource.onopen = () => {
          retryCount = 0;
          options.onOpen?.();
        };

        eventSource.onmessage = (event: MessageEvent) => {
          options.onMessage(event);
        };

        eventSource.onerror = () => {
          const error = new Error('SSE connection error');
          options.onError?.(error);

          if (retryCount < maxRetries && eventSource) {
            retryCount++;
            const delay = Math.min(retryDelay * Math.pow(2, retryCount - 1), 30000);

            loggingService.info('Scheduling SSE reconnect', 'HttpTransport', 'subscribe', {
              attempt: retryCount,
              delay,
            });

            reconnectTimer = window.setTimeout(() => {
              disconnect();
              connect();
            }, delay);
          } else {
            disconnect();
          }
        };
      } catch (error) {
        loggingService.error(
          'Failed to create EventSource',
          error instanceof Error ? error : new Error(String(error)),
          'HttpTransport',
          'subscribe'
        );
        options.onError?.(error instanceof Error ? error : new Error(String(error)));
      }
    };

    const disconnect = () => {
      if (reconnectTimer !== null) {
        window.clearTimeout(reconnectTimer);
        reconnectTimer = null;
      }

      if (eventSource) {
        eventSource.close();
        eventSource = null;
        options.onClose?.();
      }
    };

    connect();

    return disconnect;
  }

  async upload<T = unknown>(
    endpoint: string,
    file: File,
    options?: UploadOptions
  ): Promise<TransportResponse<T>> {
    const formData = new FormData();
    formData.append('file', file);

    try {
      const config: AxiosRequestConfig = {
        method: 'POST',
        url: endpoint,
        data: formData,
        timeout: options?.timeout,
        signal: options?.signal,
        headers: {
          ...options?.headers,
          'Content-Type': 'multipart/form-data',
        },
        onUploadProgress: (progressEvent) => {
          if (options?.onProgress && progressEvent.total) {
            const percentage = Math.round((progressEvent.loaded * 100) / progressEvent.total);
            options.onProgress({
              loaded: progressEvent.loaded,
              total: progressEvent.total,
              percentage,
            });
          }
        },
      };

      const response: AxiosResponse<T> = await this.axiosInstance.request(config);

      return {
        data: response.data,
        status: response.status,
        statusText: response.statusText,
        headers: response.headers as Record<string, string>,
      };
    } catch (error) {
      throw this.mapError(error as AxiosError);
    }
  }

  async download(endpoint: string, options?: DownloadOptions): Promise<void> {
    try {
      const config: AxiosRequestConfig = {
        method: 'GET',
        url: endpoint,
        responseType: 'blob',
        timeout: options?.timeout,
        signal: options?.signal,
        headers: options?.headers,
        onDownloadProgress: (progressEvent) => {
          if (options?.onProgress && progressEvent.total) {
            const percentage = Math.round((progressEvent.loaded * 100) / progressEvent.total);
            options.onProgress({
              loaded: progressEvent.loaded,
              total: progressEvent.total,
              percentage,
            });
          }
        },
      };

      const response = await this.axiosInstance.request(config);

      const blob = new Blob([response.data]);
      const downloadUrl = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = downloadUrl;
      link.download = options?.filename || 'download';
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(downloadUrl);
    } catch (error) {
      throw this.mapError(error as AxiosError);
    }
  }

  private mapError(error: AxiosError): Error {
    const mappedError = new Error(error.message);
    (mappedError as Error & { status?: number }).status = error.response?.status;
    (mappedError as Error & { statusText?: string }).statusText = error.response?.statusText;
    (mappedError as Error & { data?: unknown }).data = error.response?.data;
    return mappedError;
  }
}

/**
 * IPC Transport Implementation (for Electron)
 * Uses window.aura IPC bridge for communication
 */
export class IpcTransport implements IApiTransport {
  private aura: typeof window.aura;
  private baseURL: string;

  constructor() {
    if (!window.aura) {
      throw new Error('IPC Transport requires Electron environment');
    }

    this.aura = window.aura;
    this.baseURL = '';

    loggingService.info('IPC Transport initialized', 'IpcTransport', 'constructor');
  }

  getName(): string {
    return 'IPC';
  }

  isAvailable(): boolean {
    return !!window.aura;
  }

  async request<T = unknown>(
    endpoint: string,
    method: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE',
    data?: unknown,
    options?: TransportRequestOptions
  ): Promise<TransportResponse<T>> {
    try {
      const backendUrl =
        (await this.aura.backend?.getBaseUrl?.()) ??
        (await this.aura.backend?.getUrl?.());
      if (!backendUrl) {
        throw new Error('Backend URL is not available in Aura runtime');
      }
      const url = `${backendUrl}${endpoint}`;

      loggingService.debug(`IPC request: ${method} ${endpoint}`, 'IpcTransport', 'request', {
        method,
        endpoint,
      });

      const response = await fetch(url, {
        method,
        headers: {
          'Content-Type': 'application/json',
          ...options?.headers,
        },
        body: method !== 'GET' ? JSON.stringify(data) : undefined,
        signal: options?.signal,
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      const responseData = await response.json();

      return {
        data: responseData as T,
        status: response.status,
        statusText: response.statusText,
        headers: Object.fromEntries(response.headers.entries()),
      };
    } catch (error) {
      loggingService.error(
        'IPC request failed',
        error instanceof Error ? error : new Error(String(error)),
        'IpcTransport',
        'request'
      );
      throw error;
    }
  }

  subscribe(endpoint: string, options: SSESubscriptionOptions): () => void {
    let unsubscribe: (() => void) | null = null;
    let isActive = true;

    const setupSubscription = async () => {
      try {
        const backendUrl =
          (await this.aura.backend?.getBaseUrl?.()) ??
          (await this.aura.backend?.getUrl?.());
        if (!backendUrl) {
          throw new Error('Backend URL is not available in Aura runtime');
        }
        const url = `${backendUrl}${endpoint}`;

        const eventSource = new EventSource(url);

        eventSource.onopen = () => {
          if (isActive) {
            options.onOpen?.();
          }
        };

        eventSource.onmessage = (event: MessageEvent) => {
          if (isActive) {
            options.onMessage(event);
          }
        };

        eventSource.onerror = () => {
          if (isActive) {
            const error = new Error('IPC SSE connection error');
            options.onError?.(error);
            eventSource.close();
          }
        };

        unsubscribe = () => {
          isActive = false;
          eventSource.close();
          options.onClose?.();
        };
      } catch (error) {
        loggingService.error(
          'Failed to setup IPC SSE',
          error instanceof Error ? error : new Error(String(error)),
          'IpcTransport',
          'subscribe'
        );
        options.onError?.(error instanceof Error ? error : new Error(String(error)));
      }
    };

    setupSubscription();

    return () => {
      if (unsubscribe) {
        unsubscribe();
      }
    };
  }

  async upload<T = unknown>(
    endpoint: string,
    file: File,
    options?: UploadOptions
  ): Promise<TransportResponse<T>> {
    try {
      const backendUrl =
        (await this.aura.backend?.getBaseUrl?.()) ??
        (await this.aura.backend?.getUrl?.());
      if (!backendUrl) {
        throw new Error('Backend URL is not available in Aura runtime');
      }
      const url = `${backendUrl}${endpoint}`;

      const formData = new FormData();
      formData.append('file', file);

      const xhr = new XMLHttpRequest();

      return new Promise<TransportResponse<T>>((resolve, reject) => {
        xhr.upload.addEventListener('progress', (event) => {
          if (event.lengthComputable && options?.onProgress) {
            const percentage = Math.round((event.loaded * 100) / event.total);
            options.onProgress({
              loaded: event.loaded,
              total: event.total,
              percentage,
            });
          }
        });

        xhr.addEventListener('load', () => {
          if (xhr.status >= 200 && xhr.status < 300) {
            resolve({
              data: JSON.parse(xhr.responseText) as T,
              status: xhr.status,
              statusText: xhr.statusText,
              headers: {},
            });
          } else {
            reject(new Error(`Upload failed: ${xhr.status} ${xhr.statusText}`));
          }
        });

        xhr.addEventListener('error', () => {
          reject(new Error('Upload failed'));
        });

        xhr.open('POST', url);

        if (options?.headers) {
          Object.entries(options.headers).forEach(([key, value]) => {
            xhr.setRequestHeader(key, value);
          });
        }

        xhr.send(formData);
      });
    } catch (error) {
      loggingService.error(
        'IPC upload failed',
        error instanceof Error ? error : new Error(String(error)),
        'IpcTransport',
        'upload'
      );
      throw error;
    }
  }

  async download(endpoint: string, options?: DownloadOptions): Promise<void> {
    try {
      const backendUrl =
        (await this.aura.backend?.getBaseUrl?.()) ??
        (await this.aura.backend?.getUrl?.());
      if (!backendUrl) {
        throw new Error('Backend URL is not available in Aura runtime');
      }
      const url = `${backendUrl}${endpoint}`;

      const xhr = new XMLHttpRequest();

      return new Promise<void>((resolve, reject) => {
        xhr.addEventListener('progress', (event) => {
          if (event.lengthComputable && options?.onProgress) {
            const percentage = Math.round((event.loaded * 100) / event.total);
            options.onProgress({
              loaded: event.loaded,
              total: event.total,
              percentage,
            });
          }
        });

        xhr.addEventListener('load', () => {
          if (xhr.status >= 200 && xhr.status < 300) {
            const blob = new Blob([xhr.response]);
            const downloadUrl = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = downloadUrl;
            link.download = options?.filename || 'download';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            window.URL.revokeObjectURL(downloadUrl);
            resolve();
          } else {
            reject(new Error(`Download failed: ${xhr.status} ${xhr.statusText}`));
          }
        });

        xhr.addEventListener('error', () => {
          reject(new Error('Download failed'));
        });

        xhr.open('GET', url);
        xhr.responseType = 'blob';

        if (options?.headers) {
          Object.entries(options.headers).forEach(([key, value]) => {
            xhr.setRequestHeader(key, value);
          });
        }

        xhr.send();
      });
    } catch (error) {
      loggingService.error(
        'IPC download failed',
        error instanceof Error ? error : new Error(String(error)),
        'IpcTransport',
        'download'
      );
      throw error;
    }
  }
}

/**
 * Transport Factory
 * Automatically detects the environment and returns the appropriate transport
 */
export class TransportFactory {
  /**
   * Create transport instance based on environment
   */
  static create(baseURL: string): IApiTransport {
    if (this.isElectron()) {
      loggingService.info('Creating IPC transport', 'TransportFactory', 'create');
      return new IpcTransport();
    }

    loggingService.info('Creating HTTP transport', 'TransportFactory', 'create', { baseURL });
    return new HttpTransport(baseURL);
  }

  /**
   * Detect if running in Electron environment
   */
  static isElectron(): boolean {
    return typeof window !== 'undefined' && !!window.aura;
  }

  /**
   * Get current environment name
   */
  static getEnvironment(): 'electron' | 'web' {
    return this.isElectron() ? 'electron' : 'web';
  }
}

/**
 * Export transport instance for use throughout the app
 */
export function createTransport(baseURL: string): IApiTransport {
  return TransportFactory.create(baseURL);
}
