/**
 * Utility for parsing API errors and determining error types
 * This is a legacy compatibility layer. Use @/services/api/errorHandler instead.
 * @deprecated Use handleApiError from @/services/api/errorHandler for new code
 */

import type { ErrorType } from '../components/contentPlanning/ErrorState';
import { handleApiError, type UserFriendlyError } from '../services/api/errorHandler';

export interface ParsedApiError {
  type: ErrorType;
  message: string;
  details?: string;
  retryable: boolean;
}

/**
 * Map UserFriendlyError to ParsedApiError for backward compatibility
 */
function mapToLegacyFormat(friendlyError: UserFriendlyError): ParsedApiError {
  // Determine ErrorType from error code or title
  let type: ErrorType = 'unknown';

  if (friendlyError.errorCode?.startsWith('NET') || friendlyError.title.includes('Network')) {
    type = 'network';
  } else if (
    friendlyError.errorCode?.startsWith('AUTH') ||
    friendlyError.title.includes('API Key') ||
    friendlyError.title.includes('Authentication')
  ) {
    type = 'auth';
  } else if (
    friendlyError.errorCode === 'AUTH006_RateLimitExceeded' ||
    friendlyError.title.includes('Rate Limit')
  ) {
    type = 'rateLimit';
  } else if (
    friendlyError.errorCode === 'NET004_NetworkTimeout' ||
    friendlyError.title.includes('Timeout')
  ) {
    type = 'timeout';
  } else if (friendlyError.title.includes('Server') || friendlyError.title.includes('Service')) {
    type = 'server';
  }

  // Determine if retryable based on error code or actions
  const retryable =
    friendlyError.errorCode?.startsWith('NET') ||
    friendlyError.errorCode?.startsWith('E3') || // FFmpeg errors
    friendlyError.actions.some((a) => a.label.toLowerCase().includes('retry')) ||
    false;

  return {
    type,
    message: friendlyError.message,
    details: friendlyError.technicalDetails,
    retryable,
  };
}

/**
 * Parse axios error and determine error type
 * @deprecated Use handleApiError from @/services/api/errorHandler for new code
 */
export function parseApiError(error: unknown): ParsedApiError {
  const friendlyError = handleApiError(error);
  return mapToLegacyFormat(friendlyError);
}
