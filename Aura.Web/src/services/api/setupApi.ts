import apiClient, { type ExtendedAxiosRequestConfig } from './apiClient';

/**
 * API key configuration for saving with validation status
 */
export interface ApiKeyConfigDto {
  provider: string;
  key: string;
  isValidated?: boolean;
}

/**
 * Request to save API keys with optional validation bypass during setup
 */
export interface SaveSetupApiKeysRequest {
  apiKeys: ApiKeyConfigDto[];
  allowInvalid?: boolean;
  correlationId?: string;
}

/**
 * Response for setup API key save operation
 */
export interface SaveSetupApiKeysResponse {
  success: boolean;
  warnings?: string[] | null;
  errorMessage?: string | null;
  correlationId?: string | null;
}

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
 * Wizard progress save request
 */
export interface WizardProgressRequest {
  userId?: string;
  currentStep: number;
  state: Record<string, unknown>;
  correlationId?: string;
}

/**
 * Wizard status response
 */
export interface WizardStatusResponse {
  completed: boolean;
  currentStep: number;
  state: Record<string, unknown> | null;
  canResume: boolean;
  lastUpdated: string | null;
  completedAt?: string | null;
  version?: string | null;
}

/**
 * Wizard complete request
 */
export interface WizardCompleteRequest {
  userId?: string;
  finalStep: number;
  version?: string;
  selectedTier?: string;
  finalState?: Record<string, unknown>;
  correlationId?: string;
}

/**
 * Wizard reset request
 */
export interface WizardResetRequest {
  userId?: string;
  preserveData?: boolean;
  correlationId?: string;
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

  /**
   * Save wizard progress for resume capability
   */
  async saveWizardProgress(
    request: WizardProgressRequest
  ): Promise<{ success: boolean; message: string; correlationId?: string }> {
    const config: ExtendedAxiosRequestConfig = {
      _skipCircuitBreaker: true,
    };
    const response = await apiClient.post<{
      success: boolean;
      message: string;
      correlationId?: string;
    }>('/api/setup/wizard/save-progress', request, config);
    return response.data;
  },

  /**
   * Get wizard status and saved progress
   */
  async getWizardStatus(userId?: string): Promise<WizardStatusResponse> {
    const config: ExtendedAxiosRequestConfig = {
      _skipCircuitBreaker: true,
      params: userId ? { userId } : undefined,
    };
    const response = await apiClient.get<WizardStatusResponse>('/api/setup/wizard/status', config);
    return response.data;
  },

  /**
   * Mark wizard as complete
   */
  async completeWizard(
    request: WizardCompleteRequest
  ): Promise<{ success: boolean; message: string; correlationId?: string }> {
    const config: ExtendedAxiosRequestConfig = {
      _skipCircuitBreaker: true,
    };
    const response = await apiClient.post<{
      success: boolean;
      message: string;
      correlationId?: string;
    }>('/api/setup/wizard/complete', request, config);
    return response.data;
  },

  /**
   * Save API keys with optional validation bypass
   * Allows users to save invalid keys with explicit acknowledgment
   */
  async saveApiKeys(request: SaveSetupApiKeysRequest): Promise<SaveSetupApiKeysResponse> {
    const config: ExtendedAxiosRequestConfig = {
      _skipCircuitBreaker: true,
    };
    const response = await apiClient.post<SaveSetupApiKeysResponse>(
      '/api/setup/save-api-keys',
      request,
      config
    );
    return response.data;
  },

  /**
   * Reset wizard state (for testing or re-running)
   */
  async resetWizard(
    request: WizardResetRequest
  ): Promise<{ success: boolean; message: string; correlationId?: string }> {
    const config: ExtendedAxiosRequestConfig = {
      _skipCircuitBreaker: true,
    };
    const response = await apiClient.post<{
      success: boolean;
      message: string;
      correlationId?: string;
    }>('/api/setup/wizard/reset', request, config);
    return response.data;
  },

  /**
   * Ping backend to check if it's reachable
   * Returns ok: true if backend responds successfully, false otherwise
   */
  async pingBackend(): Promise<{ ok: boolean; details?: string }> {
    const config: ExtendedAxiosRequestConfig = { _skipCircuitBreaker: true };
    try {
      const response = await apiClient.get('/healthz/simple', config);
      return { ok: response.status === 200, details: response.statusText };
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error';
      return { ok: false, details: errorMessage };
    }
  },
};
