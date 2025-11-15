/**
 * Provider Ping API Client
 * Provides methods for testing provider connectivity with real network validation
 * Added as part of PR 336 improvements
 */

import apiClient, { type ExtendedAxiosRequestConfig } from './apiClient';

/**
 * Provider ping result with detailed network information
 */
export interface ProviderPingResult {
  attempted: boolean;
  success: boolean;
  errorCode: string | null;
  message: string | null;
  httpStatus: string | null;
  endpoint: string | null;
  responseTimeMs: number | null;
  correlationId: string | null;
}

/**
 * Response from ping-all endpoint
 */
export interface ProviderPingAllResponse {
  results: Record<string, ProviderPingResult>;
  timestamp: string;
  correlationId: string;
}

/**
 * Detailed provider validation status
 */
export interface ProviderValidationDetailedResponse {
  name: string;
  configured: boolean;
  reachable: boolean;
  errorCode: string | null;
  errorMessage: string | null;
  howToFix: string[] | null;
  lastValidated: string | null;
  category: string;
  tier: string;
  success: boolean;
  message: string;
  correlationId: string;
}

/**
 * API client for provider ping and connectivity testing
 * Uses real network I/O to validate provider availability
 */
export const providerPingClient = {
  /**
   * Ping a specific provider to test connectivity and API key validity
   * @param name - Provider name (e.g., "openai", "anthropic", "elevenlabs")
   * @returns Ping result with response time and detailed error information
   */
  async pingProvider(name: string): Promise<ProviderPingResult> {
    const config: ExtendedAxiosRequestConfig = {
      _skipCircuitBreaker: true,
    };
    const response = await apiClient.post<ProviderPingResult>(
      `/api/providers/${name}/ping`,
      {},
      config
    );
    return response.data;
  },

  /**
   * Ping all configured providers to test connectivity
   * @returns Map of provider names to ping results
   */
  async pingAllProviders(): Promise<ProviderPingAllResponse> {
    const config: ExtendedAxiosRequestConfig = {
      _skipCircuitBreaker: true,
    };
    const response = await apiClient.get<ProviderPingAllResponse>(
      '/api/providers/ping-all',
      config
    );
    return response.data;
  },

  /**
   * Validate a specific provider with detailed error information
   * Includes configuration check, reachability test, and diagnostic details
   * @param name - Provider name to validate
   * @returns Detailed validation result with how-to-fix suggestions
   */
  async validateProviderDetailed(name: string): Promise<ProviderValidationDetailedResponse> {
    const config: ExtendedAxiosRequestConfig = {
      _skipCircuitBreaker: true,
    };
    const response = await apiClient.post<ProviderValidationDetailedResponse>(
      `/api/providers/${name}/validate-detailed`,
      {},
      config
    );
    return response.data;
  },
};
