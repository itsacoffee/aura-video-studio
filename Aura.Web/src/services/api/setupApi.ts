import apiClient, { type ExtendedAxiosRequestConfig } from './apiClient';

/**
 * System setup status response
 */
export interface SystemSetupStatus {
  isComplete: boolean;
  ffmpegPath?: string | null;
  outputDirectory?: string;
}

/**
 * Setup completion request
 */
export interface SetupCompleteRequest {
  ffmpegPath?: string | null;
  outputDirectory?: string;
}

/**
 * FFmpeg check response
 */
export interface FFmpegCheckResponse {
  isInstalled: boolean;
  path?: string | null;
  version?: string | null;
  error?: string | null;
}

/**
 * Directory check request
 */
export interface DirectoryCheckRequest {
  path: string;
}

/**
 * Directory check response
 */
export interface DirectoryCheckResponse {
  isValid: boolean;
  error?: string | null;
}

/**
 * API client for system setup operations
 * All setup API calls skip the circuit breaker to prevent false "service unavailable" errors
 */
export const setupApi = {
  /**
   * Get current system setup status
   */
  async getSystemStatus(): Promise<SystemSetupStatus> {
    const config: ExtendedAxiosRequestConfig = {
      _skipCircuitBreaker: true,
    };
    const response = await apiClient.get<SystemSetupStatus>('/api/setup/system-status', config);
    return response.data;
  },

  /**
   * Complete the setup process
   */
  async completeSetup(
    request: SetupCompleteRequest
  ): Promise<{ success: boolean; errors?: string[] }> {
    const config: ExtendedAxiosRequestConfig = {
      _skipCircuitBreaker: true,
    };
    const response = await apiClient.post<{ success: boolean; errors?: string[] }>(
      '/api/setup/complete',
      request,
      config
    );
    return response.data;
  },

  /**
   * Check FFmpeg installation status
   */
  async checkFFmpeg(): Promise<FFmpegCheckResponse> {
    const config: ExtendedAxiosRequestConfig = {
      _skipCircuitBreaker: true,
    };
    const response = await apiClient.get<FFmpegCheckResponse>('/api/setup/check-ffmpeg', config);
    return response.data;
  },

  /**
   * Check if directory is valid and writable
   */
  async checkDirectory(request: DirectoryCheckRequest): Promise<DirectoryCheckResponse> {
    const config: ExtendedAxiosRequestConfig = {
      _skipCircuitBreaker: true,
    };
    const response = await apiClient.post<DirectoryCheckResponse>(
      '/api/setup/check-directory',
      request,
      config
    );
    return response.data;
  },
};
