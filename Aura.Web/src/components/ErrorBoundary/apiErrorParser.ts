/**
 * API Error parsing utilities for ApiErrorDisplay component
 */

import type { ApiError } from './ApiErrorDisplay';
import { handleApiError, type UserFriendlyError } from '@/services/api/errorHandler';

/**
 * Map UserFriendlyError to ApiError for display
 */
function mapToApiError(friendlyError: UserFriendlyError): ApiError {
  return {
    errorCode: friendlyError.errorCode,
    message: friendlyError.message,
    technicalDetails: friendlyError.technicalDetails,
    suggestedActions: friendlyError.actions.map((a) => a.description),
    learnMoreUrl: friendlyError.learnMoreUrl,
    errorTitle: friendlyError.title,
    isTransient: friendlyError.actions.some((a) => a.label.toLowerCase().includes('retry')),
    correlationId: friendlyError.correlationId,
  };
}

/**
 * Parse API error from fetch response
 */
export async function parseApiError(response: Response): Promise<ApiError> {
  try {
    const data = await response.json();

    // If backend already sent error in our format, use it
    if (data.errorCode || data.title) {
      return {
        errorCode: data.errorCode || `HTTP_${response.status}`,
        message: data.message || data.detail || response.statusText || 'An error occurred',
        technicalDetails: data.technicalDetails,
        suggestedActions: data.suggestedActions || data.howToFix || [],
        learnMoreUrl: data.learnMoreUrl || data.type,
        errorTitle: data.errorTitle || data.title,
        isTransient: data.isTransient || false,
        correlationId: data.correlationId,
      };
    }

    // Otherwise, use centralized error handler
    const friendlyError = handleApiError(response);
    return mapToApiError(friendlyError);
  } catch {
    // If response is not JSON, use centralized error handler
    const friendlyError = handleApiError(response);
    return mapToApiError(friendlyError);
  }
}
