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
  console.info('[OpenAI Validation] Starting validation...');
  console.info('[OpenAI Validation] Key length:', apiKey?.length || 0);
  console.info('[OpenAI Validation] Base URL:', baseUrl || 'default');

  const startTime = performance.now();

  try {
    // Pre-check: Test network connectivity before validation
    console.info('[OpenAI Validation] Pre-check: Testing network connectivity...');
    const networkCheck = await testNetworkConnectivity();
    console.info('[OpenAI Validation] Network check result:', networkCheck);

    if (!networkCheck.success) {
      console.warn('[OpenAI Validation] Network connectivity issues detected:', networkCheck);
      // Continue anyway, backend will handle offline mode
    }

    const request: ValidateOpenAIKeyRequest = {
      apiKey,
      baseUrl,
      organizationId,
      projectId,
    };

    console.info('[OpenAI Validation] Sending validation request to backend...');

    const response = await apiClient.post<ProviderValidationResponse>(
      '/api/providers/openai/validate',
      request
    );

    const elapsed = performance.now() - startTime;
    console.info('[OpenAI Validation] Backend response received in', elapsed.toFixed(0), 'ms');
    console.info('[OpenAI Validation] Status:', response.data.status);
    console.info('[OpenAI Validation] Message:', response.data.message);
    console.info('[OpenAI Validation] Details:', response.data.details);

    const result = response.data;

    return mapValidationResponse(result);
  } catch (error: unknown) {
    const elapsed = performance.now() - startTime;
    console.error('[OpenAI Validation] Validation error after', elapsed.toFixed(0), 'ms:', error);

    // Enhanced error logging
    if (error instanceof Error) {
      console.error('[OpenAI Validation] Error name:', error.name);
      console.error('[OpenAI Validation] Error message:', error.message);
      console.error('[OpenAI Validation] Error stack:', error.stack);
    }

    // Check for specific error types
    const errorMessage = error instanceof Error ? error.message : String(error);
    console.error('[OpenAI Validation] Error details:', {
      errorType: error instanceof Error ? error.name : typeof error,
      errorMessage: errorMessage,
      elapsed: elapsed.toFixed(0) + 'ms',
    });

    return {
      isValid: false,
      status: 'NetworkError',
      message: 'Network error while contacting server. Please check your connection.',
      canSave: false,
      diagnosticInfo: `Client error: ${errorMessage} (after ${elapsed.toFixed(0)}ms)`,
      elapsedTimeMs: elapsed,
    };
  }
}

/**
 * Test network connectivity to backend
 */
async function testNetworkConnectivity(): Promise<{ success: boolean; error?: string }> {
  try {
    const response = await apiClient.get('/api/diagnostics/network-test', {
      timeout: 5000, // 5 second timeout for connectivity check
    });

    return {
      success: response.data.success === true,
      error: response.data.success ? undefined : 'Network test failed',
    };
  } catch (error: unknown) {
    console.warn('[OpenAI Validation] Network connectivity test failed:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : String(error),
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
          response.message ||
          'OpenAI service issue. Your key may be valid; you can continue anyway.',
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
        message:
          response.message ||
          'Request timed out. You can continue anyway, and the key will be validated on first use.',
        canSave: false,
        canContinue: true,
        diagnosticInfo,
        elapsedTimeMs,
      };

    case 'Offline':
      return {
        isValid: false,
        status: 'Offline',
        message:
          response.message || 'No internet connection detected. You can continue in offline mode.',
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
        message:
          response.message || 'An error occurred during validation. You can continue anyway.',
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
