/**
 * OpenAI API Key Validation Service
 *
 * Provides user-friendly validation of OpenAI API keys with detailed error messages
 */

import type { ValidateOpenAIKeyRequest, ProviderValidationResponse } from '../types/api-v1';
import apiClient from './api/apiClient';

export interface OpenAIValidationResult {
  isValid: boolean;
  status:
    | 'Valid'
    | 'Invalid'
    | 'RateLimited'
    | 'PermissionDenied'
    | 'ServiceIssue'
    | 'NetworkError'
    | 'Timeout'
    | 'Offline'
    | 'Pending';
  message: string;
  canSave: boolean;
  canContinue?: boolean;
  diagnosticInfo?: string;
  elapsedTimeMs?: number;
}

/**
 * Validate an OpenAI API key with the backend
 */
export async function validateOpenAIKey(
  apiKey: string,
  baseUrl?: string,
  organizationId?: string,
  projectId?: string
): Promise<OpenAIValidationResult> {
  try {
    const request: ValidateOpenAIKeyRequest = {
      apiKey,
      baseUrl,
      organizationId,
      projectId,
    };

    const response = await apiClient.post<ProviderValidationResponse>(
      '/api/providers/openai/validate',
      request
    );

    const result = response.data;

    return mapValidationResponse(result);
  } catch (error: unknown) {
    console.error('OpenAI validation error:', error);

    return {
      isValid: false,
      status: 'NetworkError',
      message: 'Network error while contacting server. Please check your connection.',
      canSave: false,
    };
  }
}

/**
 * Map backend validation response to user-friendly result
 */
function mapValidationResponse(response: ProviderValidationResponse): OpenAIValidationResult {
  const status = response.status;
  const diagnosticInfo = response.details?.diagnosticInfo;
  const elapsedTimeMs = response.details?.responseTimeMs ?? undefined;

  switch (status) {
    case 'Valid':
      return {
        isValid: true,
        status: 'Valid',
        message: response.message || 'API key is valid and verified with OpenAI.',
        canSave: true,
        canContinue: true,
        diagnosticInfo,
        elapsedTimeMs,
      };

    case 'Invalid':
      return {
        isValid: false,
        status: 'Invalid',
        message: response.message || 'Invalid API key. Please check the value and try again.',
        canSave: false,
        canContinue: false,
        diagnosticInfo,
        elapsedTimeMs,
      };

    case 'PermissionDenied':
      return {
        isValid: false,
        status: 'PermissionDenied',
        message:
          response.message || 'Access denied. Check organization/project permissions or billing.',
        canSave: false,
        canContinue: false,
        diagnosticInfo,
        elapsedTimeMs,
      };

    case 'RateLimited':
      return {
        isValid: true,
        status: 'RateLimited',
        message:
          response.message ||
          "Rate limited. Your key is valid, but you've hit a limit. You can continue and try again later.",
        canSave: true,
        canContinue: true,
        diagnosticInfo,
        elapsedTimeMs,
      };

    case 'ServiceIssue':
      return {
        isValid: false,
        status: 'ServiceIssue',
        message:
          response.message || 'OpenAI service issue. Your key may be valid; you can continue anyway.',
        canSave: true,
        canContinue: true,
        diagnosticInfo,
        elapsedTimeMs,
      };

    case 'NetworkError':
      return {
        isValid: false,
        status: 'NetworkError',
        message:
          response.message ||
          'Network error while contacting OpenAI. Please check your internet connection. You can continue anyway.',
        canSave: false,
        canContinue: true,
        diagnosticInfo,
        elapsedTimeMs,
      };

    case 'Timeout':
      return {
        isValid: false,
        status: 'Timeout',
        message: response.message || 'Request timed out. You can continue anyway, and the key will be validated on first use.',
        canSave: false,
        canContinue: true,
        diagnosticInfo,
        elapsedTimeMs,
      };

    case 'Offline':
      return {
        isValid: false,
        status: 'Offline',
        message: response.message || 'No internet connection detected. You can continue in offline mode.',
        canSave: true,
        canContinue: true,
        diagnosticInfo,
        elapsedTimeMs,
      };

    case 'Cancelled':
      return {
        isValid: false,
        status: 'NetworkError',
        message: response.message || 'Validation was cancelled.',
        canSave: false,
        canContinue: true,
        diagnosticInfo,
        elapsedTimeMs,
      };

    case 'Error':
      return {
        isValid: false,
        status: 'NetworkError',
        message: response.message || 'An error occurred during validation. You can continue anyway.',
        canSave: false,
        canContinue: true,
        diagnosticInfo,
        elapsedTimeMs,
      };

    default:
      return {
        isValid: false,
        status: 'NetworkError',
        message:
          response.message ||
          'Unexpected response from validation service. You can continue anyway.',
        canSave: false,
        canContinue: true,
        diagnosticInfo,
        elapsedTimeMs,
      };
  }
}

/**
 * Get user-friendly display text for validation status
 */
export function getStatusDisplayText(status: OpenAIValidationResult['status']): string {
  switch (status) {
    case 'Valid':
      return 'Validated ✓';
    case 'Invalid':
      return 'Invalid ✕';
    case 'RateLimited':
      return 'Rate Limited (can continue)';
    case 'PermissionDenied':
      return 'Permission Denied';
    case 'ServiceIssue':
      return 'Service Issue (can continue)';
    case 'NetworkError':
      return 'Network Error (can continue)';
    case 'Timeout':
      return 'Timeout (can continue)';
    case 'Offline':
      return 'Offline Mode (can continue)';
    case 'Pending':
      return 'Validating...';
    default:
      return 'Unknown Status';
  }
}

/**
 * Get color/appearance for validation status
 */
export function getStatusAppearance(
  status: OpenAIValidationResult['status']
): 'success' | 'danger' | 'warning' | 'subtle' {
  switch (status) {
    case 'Valid':
      return 'success';
    case 'Invalid':
    case 'PermissionDenied':
      return 'danger';
    case 'RateLimited':
    case 'ServiceIssue':
    case 'Offline':
      return 'warning';
    case 'NetworkError':
    case 'Timeout':
    case 'Pending':
      return 'subtle';
    default:
      return 'subtle';
  }
}

/**
 * Format elapsed time in seconds
 */
export function formatElapsedTime(ms?: number): string {
  if (!ms) return '';
  const seconds = (ms / 1000).toFixed(1);
  return `(${seconds}s)`;
}
