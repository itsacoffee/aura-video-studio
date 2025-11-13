import apiClient, { resetCircuitBreaker } from './apiClient';

export interface FFmpegHardwareAcceleration {
  nvencSupported: boolean;
  amfSupported: boolean;
  quickSyncSupported: boolean;
  videoToolboxSupported: boolean;
  availableEncoders: string[];
}

export interface FFmpegStatus {
  installed: boolean;
  valid: boolean;
  version: string | null;
  path: string | null;
  source: string;
  error: string | null;
  versionMeetsRequirement: boolean;
  minimumVersion: string;
  hardwareAcceleration: FFmpegHardwareAcceleration;
  correlationId: string;
}

export interface FFmpegInstallRequest {
  version?: string;
}

export interface FFmpegInstallResponse {
  success: boolean;
  message: string;
  version?: string;
  path?: string;
  correlationId: string;
  howToFix?: string[];
}

export interface FFmpegRescanResponse {
  success: boolean;
  installed: boolean;
  version: string | null;
  path: string | null;
  source: string;
  valid: boolean;
  error: string | null;
  message: string;
  correlationId: string;
}

export interface UseExistingFFmpegRequest {
  path: string;
}

export interface UseExistingFFmpegResponse {
  success: boolean;
  message: string;
  installed: boolean;
  valid: boolean;
  path: string | null;
  version: string | null;
  source: string;
  correlationId: string;
  howToFix?: string[];
}

/**
 * API client for FFmpeg status and installation
 */
export const ffmpegClient = {
  /**
   * Get comprehensive FFmpeg status
   */
  async getStatus(): Promise<FFmpegStatus> {
    const response = await apiClient.get<FFmpegStatus>('/api/system/ffmpeg/status');

    // Reset circuit breaker on successful FFmpeg status check
    if (response.data.installed && response.data.valid) {
      resetCircuitBreaker();
    }

    return response.data;
  },

  /**
   * Install managed FFmpeg
   */
  async install(request?: FFmpegInstallRequest): Promise<FFmpegInstallResponse> {
    const response = await apiClient.post<FFmpegInstallResponse>('/api/ffmpeg/install', request);

    // Reset circuit breaker on successful installation
    if (response.data.success) {
      resetCircuitBreaker();
    }

    return response.data;
  },

  /**
   * Rescan system for FFmpeg installations
   */
  async rescan(): Promise<FFmpegRescanResponse> {
    const response = await apiClient.post<FFmpegRescanResponse>('/api/ffmpeg/rescan');

    // Reset circuit breaker on successful rescan that finds valid FFmpeg
    if (response.data.success && response.data.installed && response.data.valid) {
      resetCircuitBreaker();
    }

    return response.data;
  },

  /**
   * Validate and use an existing FFmpeg installation
   */
  async useExisting(request: UseExistingFFmpegRequest): Promise<UseExistingFFmpegResponse> {
    const response = await apiClient.post<UseExistingFFmpegResponse>(
      '/api/ffmpeg/use-existing',
      request
    );

    // Reset circuit breaker on successful validation
    if (response.data.success && response.data.installed && response.data.valid) {
      resetCircuitBreaker();
    }

    return response.data;
  },
};
