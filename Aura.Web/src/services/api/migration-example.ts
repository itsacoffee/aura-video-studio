/**
 * Example: How to migrate services to use the hybrid API client
 *
 * This file demonstrates the migration pattern for existing services.
 */

import { apiClient } from './client';
import type { TransportRequestOptions } from './transport';

/**
 * Example: Health API Service
 * Shows how to migrate from direct Axios usage to hybrid client
 */
export class HealthApiService {
  /**
   * Check API health
   *
   * Before migration:
   * ```typescript
   * import { get } from './api/apiClient';
   * const response = await get<HealthResponse>('/health');
   * ```
   *
   * After migration:
   * ```typescript
   * import { apiClient } from './api/client';
   * const response = await apiClient.get<HealthResponse>('/health');
   * ```
   */
  async checkHealth(): Promise<{ status: string; timestamp: string }> {
    return apiClient.get('/health');
  }

  /**
   * Check if API is ready
   * With custom timeout
   */
  async checkReady(timeoutMs = 5000): Promise<{ ready: boolean }> {
    const options: TransportRequestOptions = {
      timeout: timeoutMs,
    };
    return apiClient.get('/health/ready', options);
  }
}

/**
 * Example: Jobs API Service
 * Shows how to handle POST requests with data
 */
export class JobsApiService {
  /**
   * Create a new job
   *
   * Before:
   * ```typescript
   * import { post } from './api/apiClient';
   * const response = await post<JobResponse>('/api/jobs', jobData);
   * ```
   *
   * After:
   * ```typescript
   * const response = await apiClient.post<JobResponse>('/api/jobs', jobData);
   * ```
   */
  async createJob(data: { title: string; type: string }): Promise<{ jobId: string }> {
    return apiClient.post('/api/jobs', data);
  }

  /**
   * Get job status with SSE
   *
   * Before:
   * ```typescript
   * import { createSseClient } from './api/sseClient';
   * const client = createSseClient(jobId);
   * ```
   *
   * After:
   * ```typescript
   * const unsubscribe = apiClient.subscribe(`/api/jobs/${jobId}/events`, {
   *   onMessage: (event) => {
   *     const data = JSON.parse(event.data);
   *     console.log('Progress:', data);
   *   }
   * });
   * ```
   */
  subscribeToJobProgress(
    jobId: string,
    onProgress: (data: unknown) => void,
    onError?: (error: Error) => void
  ): () => void {
    return apiClient.subscribe(`/api/jobs/${jobId}/events`, {
      onMessage: (event) => {
        try {
          const data = JSON.parse(event.data);
          onProgress(data);
        } catch (error) {
          onError?.(error instanceof Error ? error : new Error(String(error)));
        }
      },
      onError,
    });
  }
}

/**
 * Example: File Upload Service
 * Shows how to handle file uploads with progress
 */
export class FileUploadService {
  /**
   * Upload a file with progress tracking
   *
   * Before:
   * ```typescript
   * import { uploadFile } from './api/apiClient';
   * const response = await uploadFile<UploadResponse>('/api/upload', file, (progress) => {
   *   console.log(`Upload progress: ${progress}%`);
   * });
   * ```
   *
   * After:
   * ```typescript
   * const response = await apiClient.uploadFile<UploadResponse>('/api/upload', file, (progress) => {
   *   console.log(`Upload progress: ${progress}%`);
   * });
   * ```
   */
  async uploadVideo(
    file: File,
    onProgress?: (progress: number) => void
  ): Promise<{ fileId: string; url: string }> {
    return apiClient.uploadFile('/api/upload/video', file, onProgress);
  }

  /**
   * Download a file with progress tracking
   */
  async downloadVideo(
    videoId: string,
    filename: string,
    onProgress?: (progress: number) => void
  ): Promise<void> {
    await apiClient.downloadFile(`/api/download/video/${videoId}`, filename, onProgress);
  }
}

/**
 * Example: Environment Detection
 * Shows how to detect the current environment and adjust behavior
 */
export class EnvironmentService {
  /**
   * Check if running in Electron
   */
  isElectron(): boolean {
    return apiClient.isElectron();
  }

  /**
   * Get current transport name (for debugging)
   */
  getTransport(): string {
    return apiClient.getTransportName();
  }

  /**
   * Get environment name
   */
  getEnvironment(): 'electron' | 'web' {
    return apiClient.getEnvironment();
  }

  /**
   * Example: Conditional behavior based on environment
   */
  getBackendUrl(): string {
    if (apiClient.isElectron()) {
      return 'Backend URL is managed by Electron IPC';
    }
    return import.meta.env.VITE_API_BASE_URL || 'http://localhost:5005';
  }
}

/**
 * Migration Checklist:
 *
 * 1. Replace imports:
 *    - Old: `import { get, post, ... } from './api/apiClient'`
 *    - New: `import { apiClient } from './api/client'`
 *
 * 2. Update method calls:
 *    - Old: `get<T>(url, options)`
 *    - New: `apiClient.get<T>(url, options)`
 *
 * 3. Update SSE subscriptions:
 *    - Old: `createSseClient(jobId)` with event handlers
 *    - New: `apiClient.subscribe(url, { onMessage, onError })`
 *
 * 4. Update file operations:
 *    - Old: `uploadFile(url, file, onProgress)`
 *    - New: `apiClient.uploadFile(url, file, onProgress)`
 *
 * 5. Test in both environments:
 *    - Web browser (HTTP transport)
 *    - Electron app (IPC transport)
 *
 * 6. No changes needed for:
 *    - Request options
 *    - Error handling
 *    - Type definitions
 *    - Response parsing
 */
