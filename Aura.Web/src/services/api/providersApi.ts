/**
 * Providers API Service
 * Provides typed methods for provider configuration, validation, and status operations
 */

import { get, post, put } from './apiClient';
import type { ExtendedAxiosRequestConfig } from './apiClient';

/**
 * Provider configuration interface
 */
export interface ProviderConfig {
  providerId: string;
  enabled: boolean;
  apiKey?: string;
  endpoint?: string;
  modelName?: string;
  settings?: Record<string, unknown>;
}

/**
 * Provider status response
 */
export interface ProviderStatus {
  providerId: string;
  name: string;
  available: boolean;
  configured: boolean;
  healthy: boolean;
  message?: string;
  lastChecked?: string;
}

/**
 * Provider test connection response
 */
export interface TestConnectionResponse {
  success: boolean;
  message: string;
  details?: Record<string, unknown>;
}

/**
 * Provider models response
 */
export interface ProviderModelsResponse {
  models: Array<{
    id: string;
    name: string;
    description?: string;
    capabilities?: string[];
    contextLength?: number;
  }>;
}

/**
 * API key validation response
 */
export interface ApiKeyValidationResponse {
  isValid: boolean;
  message: string;
  status?: string;
  errorType?: string;
  networkCheckPassed?: boolean;
  details?: Record<string, unknown>;
}

/**
 * Get all provider statuses
 */
export async function getProviderStatuses(
  config?: ExtendedAxiosRequestConfig
): Promise<ProviderStatus[]> {
  return get<ProviderStatus[]>('/api/providers/status', config);
}

/**
 * Get provider configuration
 */
export async function getProviderConfig(
  providerId: string,
  config?: ExtendedAxiosRequestConfig
): Promise<ProviderConfig> {
  return get<ProviderConfig>(`/api/providers/${providerId}/config`, config);
}

/**
 * Update provider configuration
 */
export async function updateProviderConfig(
  providerId: string,
  providerConfig: Partial<ProviderConfig>,
  config?: ExtendedAxiosRequestConfig
): Promise<ProviderConfig> {
  return put<ProviderConfig>(`/api/providers/${providerId}/config`, providerConfig, config);
}

/**
 * Test provider connection
 */
export async function testProviderConnection(
  providerId: string,
  testConfig?: { apiKey?: string; endpoint?: string },
  config?: ExtendedAxiosRequestConfig
): Promise<TestConnectionResponse> {
  return post<TestConnectionResponse>(
    '/api/providers/test-connection',
    {
      providerId,
      ...testConfig,
    },
    config
  );
}

/**
 * Validate OpenAI API key with detailed error handling
 */
export async function validateOpenAIKey(
  apiKey: string,
  config?: ExtendedAxiosRequestConfig
): Promise<ApiKeyValidationResponse> {
  const response = await post<{
    isValid: boolean;
    status: string;
    message?: string;
    correlationId?: string;
    details?: {
      provider: string;
      keyFormat: string;
      formatValid: boolean;
      networkCheckPassed?: boolean;
      httpStatusCode?: number;
      errorType?: string;
      responseTimeMs?: number;
      diagnosticInfo?: string;
    };
  }>('/api/providers/openai/validate', { apiKey }, config);

  return {
    isValid: response.isValid,
    message: response.message || 'Validation completed',
    status: response.status,
    errorType: response.details?.errorType,
    networkCheckPassed: response.details?.networkCheckPassed,
    details: response.details,
  };
}

/**
 * Validate ElevenLabs API key
 */
export async function validateElevenLabsKey(
  apiKey: string,
  config?: ExtendedAxiosRequestConfig
): Promise<ApiKeyValidationResponse> {
  return post<ApiKeyValidationResponse>('/api/providers/elevenlabs/validate', { apiKey }, config);
}

/**
 * Validate PlayHT API key
 */
export async function validatePlayHTKey(
  apiKey: string,
  userId: string,
  config?: ExtendedAxiosRequestConfig
): Promise<ApiKeyValidationResponse> {
  return post<ApiKeyValidationResponse>(
    '/api/providers/playht/validate',
    { apiKey, userId },
    config
  );
}

/**
 * Get available models for a provider
 */
export async function getProviderModels(
  providerId: string,
  config?: ExtendedAxiosRequestConfig
): Promise<ProviderModelsResponse> {
  return get<ProviderModelsResponse>(`/api/providers/${providerId}/models`, config);
}

/**
 * Get provider preferences (profile selection)
 */
export async function getProviderPreferences(
  config?: ExtendedAxiosRequestConfig
): Promise<{ selectedProfile: string; customSelections?: Record<string, string> }> {
  return get<{ selectedProfile: string; customSelections?: Record<string, string> }>(
    '/api/providers/preferences',
    config
  );
}

/**
 * Update provider preferences
 */
export async function updateProviderPreferences(
  preferences: { selectedProfile: string; customSelections?: Record<string, string> },
  config?: ExtendedAxiosRequestConfig
): Promise<void> {
  return post<void>('/api/providers/preferences', preferences, config);
}

/**
 * Enhanced provider validation request
 */
export interface EnhancedProviderValidationRequest {
  provider: string;
  configuration: Record<string, string | undefined | null>;
  partialValidation?: boolean;
  correlationId?: string;
}

/**
 * Field-level validation error
 */
export interface FieldValidationError {
  fieldName: string;
  errorCode: string;
  errorMessage: string;
  suggestedFix?: string | null;
}

/**
 * Enhanced provider validation response
 */
export interface EnhancedProviderValidationResponse {
  isValid: boolean;
  status: string;
  provider: string;
  fieldErrors?: FieldValidationError[] | null;
  fieldValidationStatus?: Record<string, boolean> | null;
  overallMessage?: string | null;
  correlationId?: string | null;
  details?: unknown;
}

/**
 * Save partial configuration request
 */
export interface SavePartialConfigurationRequest {
  provider: string;
  partialConfiguration: Record<string, string | undefined | null>;
  correlationId?: string;
}

/**
 * Save partial configuration response
 */
export interface SavePartialConfigurationResponse {
  success: boolean;
  message: string;
  savedFields?: string[];
  correlationId?: string;
}

/**
 * Validate provider configuration with field-level validation
 */
export async function validateProviderEnhanced(
  request: EnhancedProviderValidationRequest,
  config?: ExtendedAxiosRequestConfig
): Promise<EnhancedProviderValidationResponse> {
  return post<EnhancedProviderValidationResponse>(
    '/api/providers/validate-enhanced',
    request,
    config
  );
}

/**
 * Save partial provider configuration
 */
export async function savePartialConfiguration(
  request: SavePartialConfigurationRequest,
  config?: ExtendedAxiosRequestConfig
): Promise<SavePartialConfigurationResponse> {
  return post<SavePartialConfigurationResponse>(
    '/api/providers/save-partial-config',
    request,
    config
  );
}
