import apiClient, { resetCircuitBreaker, type ExtendedAxiosRequestConfig } from './apiClient';

export interface FFmpegHardwareAcceleration {
  nvencSupported: boolean;
  amfSupported: boolean;
  quickSyncSupported: boolean;
  videoToolboxSupported: boolean;
  availableEncoders: string[];
}

export type FFmpegMode = 'none' | 'system' | 'local' | 'custom';

export type FFmpegValidationResult =
  | 'ok'
  | 'not-found'
  | 'invalid-binary'
  | 'execution-error'
  | 'network-error'
  | 'unknown';

/**
 * FFmpeg status response from /api/ffmpeg/status endpoint
 * Enhanced with mode and validation tracking (PR 1)
 */
export interface FFmpegStatus {
  installed: boolean;
  valid: boolean;
  version: string | null;
  path: string | null;
  source: string;
  mode: FFmpegMode;
  error: string | null;
  lastValidatedAt: string | null;
  lastValidationResult: FFmpegValidationResult;
  correlationId: string;
}

/**
 * Extended FFmpeg status with hardware acceleration details
 * From /api/system/ffmpeg/status endpoint (legacy)
 */
export interface FFmpegStatusExtended extends FFmpegStatus {
  errorCode: string | null;
  errorMessage: string | null;
  attemptedPaths: string[];
  versionMeetsRequirement: boolean;
  minimumVersion: string;
  hardwareAcceleration: FFmpegHardwareAcceleration;
}

export interface FFmpegInstallRequest {
  version?: string;
}

/**
 * FFmpeg installation response with detailed error information
 * PR 336 improvements: includes errorCode, howToFix, and user-friendly messages
 */
export interface FFmpegInstallResponse {
  success: boolean;
  message: string;
  title?: string;
  detail?: string;
  version?: string;
  path?: string;
  installedAt?: string;
  mode?: FFmpegMode;
  errorCode?: string;
  howToFix?: string[];
  type?: string;
  correlationId: string;
}

/**
 * FFmpeg detection response from /api/ffmpeg/detect endpoint (PR 1)
 */
export interface FFmpegDetectResponse {
  success: boolean;
  installed: boolean;
  valid: boolean;
  version: string | null;
  path: string | null;
  source: string;
  mode: FFmpegMode;
  message: string;
  attemptedPaths?: string[];
  detail?: string;
  howToFix?: string[];
  correlationId: string;
}

export interface FFmpegRescanResponse {
  success: boolean;
  installed: boolean;
  version: string | null;
  path: string | null;
  source: string | null;
  valid: boolean;
  error: string | null;
  message: string;
  correlationId: string;
}

export interface UseExistingFFmpegRequest {
  path: string;
}

/**
 * Response from use-existing FFmpeg endpoint
 * PR 336 improvements: includes detailed error information and how-to-fix suggestions
 */
export interface UseExistingFFmpegResponse {
  success: boolean;
  message: string;
  installed: boolean;
  valid: boolean;
  path: string | null;
  version: string | null;
  source: string;
  mode?: FFmpegMode;
  title?: string;
  detail?: string;
  correlationId: string;
  howToFix?: string[];
}

export interface FFmpegDirectCheckCandidate {
  label: string;
  path: string | null;
  exists: boolean;
  executionAttempted: boolean;
  exitCode: number | null;
  timedOut: boolean;
  rawVersionOutput: string | null;
  versionParsed: string | null;
  valid: boolean;
  error: string | null;
}

export interface FFmpegDirectCheckResponse {
  candidates: FFmpegDirectCheckCandidate[];
  overall: {
    installed: boolean;
    valid: boolean;
    source: string | null;
    chosenPath: string | null;
    version: string | null;
  };
  correlationId: string;
}

/**
 * Request for setting custom FFmpeg path (PR 1)
 */
export interface SetPathRequest {
  path: string;
}

/**
 * Response from set-path endpoint (PR 1)
 */
export interface SetPathResponse {
  success: boolean;
  message: string;
  installed: boolean;
  valid: boolean;
  path: string | null;
  version: string | null;
  source: string;
  mode: FFmpegMode;
  title?: string;
  detail?: string;
  errorCode?: string;
  howToFix?: string[];
  attemptedPaths?: string[];
  correlationId: string;
}

/**
 * API client for FFmpeg status and installation
 * All FFmpeg API calls skip the circuit breaker to prevent false "service unavailable" errors during setup
 * PR 1: Enhanced with mode tracking, detection, and custom path configuration
 */
export const ffmpegClient = {
  /**
   * Get comprehensive FFmpeg status from /api/ffmpeg/status
   * PR 336: Simplified endpoint with essential installation information
   */
  async getStatus(): Promise<FFmpegStatus> {
    const config: ExtendedAxiosRequestConfig = {
      _skipCircuitBreaker: true,
    };
    const response = await apiClient.get<FFmpegStatus>('/api/ffmpeg/status', config);

    // Reset circuit breaker on successful FFmpeg status check
    if (response.data.installed && response.data.valid) {
      resetCircuitBreaker();
    }

    return response.data;
  },

  /**
   * Get extended FFmpeg status with hardware acceleration details
   * This uses the legacy /api/system/ffmpeg/status endpoint with full details
   */
  async getStatusExtended(): Promise<FFmpegStatusExtended> {
    const config: ExtendedAxiosRequestConfig = {
      _skipCircuitBreaker: true,
    };
    const response = await apiClient.get<FFmpegStatusExtended>(
      '/api/system/ffmpeg/status',
      config
    );

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
    const config: ExtendedAxiosRequestConfig = {
      _skipCircuitBreaker: true,
    };
    const response = await apiClient.post<FFmpegInstallResponse>(
      '/api/ffmpeg/install',
      request,
      config
    );

    // Reset circuit breaker on successful installation
    if (response.data.success) {
      resetCircuitBreaker();
    }

    return response.data;
  },

  /**
   * Force re-detection and validation of FFmpeg (PR 1)
   */
  async detect(): Promise<FFmpegDetectResponse> {
    const config: ExtendedAxiosRequestConfig = {
      _skipCircuitBreaker: true,
    };
    const response = await apiClient.post<FFmpegDetectResponse>(
      '/api/ffmpeg/detect',
      undefined,
      config
    );

    // Reset circuit breaker on successful detection
    if (response.data.success && response.data.installed && response.data.valid) {
      resetCircuitBreaker();
    }

    return response.data;
  },

  /**
   * Set and validate custom FFmpeg path (PR 1)
   */
  async setPath(request: SetPathRequest): Promise<SetPathResponse> {
    const config: ExtendedAxiosRequestConfig = {
      _skipCircuitBreaker: true,
    };
    const response = await apiClient.post<SetPathResponse>(
      '/api/ffmpeg/set-path',
      request,
      config
    );

    // Reset circuit breaker on successful validation
    if (response.data.success && response.data.installed && response.data.valid) {
      resetCircuitBreaker();
    }

    return response.data;
  },

  /**
   * Rescan system for FFmpeg installations
   */
  async rescan(): Promise<FFmpegRescanResponse> {
    const config: ExtendedAxiosRequestConfig = {
      _skipCircuitBreaker: true,
    };
    const response = await apiClient.post<FFmpegRescanResponse>(
      '/api/ffmpeg/rescan',
      undefined,
      config
    );

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
    const config: ExtendedAxiosRequestConfig = {
      _skipCircuitBreaker: true,
    };
    const response = await apiClient.post<UseExistingFFmpegResponse>(
      '/api/ffmpeg/use-existing',
      request,
      config
    );

    // Reset circuit breaker on successful validation
    if (response.data.success && response.data.installed && response.data.valid) {
      resetCircuitBreaker();
    }

    return response.data;
  },

  /**
   * Perform detailed diagnostics across all FFmpeg candidates
   */
  async directCheck(): Promise<FFmpegDirectCheckResponse> {
    const config: ExtendedAxiosRequestConfig = {
      _skipCircuitBreaker: true,
    };
    const response = await apiClient.get<FFmpegDirectCheckResponse>(
      '/api/debug/ffmpeg/direct-check',
      config
    );
    return response.data;
  },
};
