/**
 * Utility for handling API errors and extracting ProblemDetails from responses
 * This is a legacy compatibility layer. Use @/services/api/errorHandler instead.
 * @deprecated Use handleApiError from @/services/api/errorHandler for new code
 */

import { handleApiError, type UserFriendlyError } from '../services/api/errorHandler';
import { loggingService as logger } from '../services/loggingService';

export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  type?: string;
  correlationId?: string;
  errorCode?: string;
  [key: string]: unknown;
}

export interface ParsedApiError {
  title: string;
  message: string;
  errorDetails?: string;
  correlationId?: string;
  errorCode?: string;
  originalError: unknown;
}

/**
 * Map UserFriendlyError to ParsedApiError for backward compatibility
 */
function mapToLegacyParsedApiError(friendlyError: UserFriendlyError): ParsedApiError {
  return {
    title: friendlyError.title,
    message: friendlyError.message,
    errorDetails: friendlyError.technicalDetails,
    correlationId: friendlyError.correlationId,
    errorCode: friendlyError.errorCode,
    originalError: friendlyError,
  };
}

/**
 * Extract error code from type URI (e.g., "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E300" -> "E300")
 */
function extractErrorCodeFromType(type?: string): string | undefined {
  if (!type) return undefined;

  const match = type.match(/E\d{3,}/);
  return match ? match[0] : undefined;
}

/**
 * Parse an error response from the API and extract ProblemDetails
 * @deprecated Use handleApiError from @/services/api/errorHandler for new code
 */
export async function parseApiError(error: unknown): Promise<ParsedApiError> {
  // If it's a Response object (from fetch), we need to handle it specially
  if (error instanceof Response) {
    try {
      const contentType = error.headers.get('content-type');

      // Try to parse as JSON (ProblemDetails)
      if (
        contentType?.includes('application/json') ||
        contentType?.includes('application/problem+json')
      ) {
        const problemDetails: ProblemDetails = await error.json();

        return {
          title: problemDetails.title || `Error ${error.status}`,
          message: problemDetails.detail || error.statusText || 'An error occurred',
          errorDetails: problemDetails.detail,
          correlationId:
            problemDetails.correlationId || error.headers.get('X-Correlation-ID') || undefined,
          errorCode: problemDetails.errorCode || extractErrorCodeFromType(problemDetails.type),
          originalError: problemDetails,
        };
      }

      // Fallback for non-JSON responses
      const text = await error.text();
      return {
        title: `Error ${error.status}`,
        message: text || error.statusText || 'An error occurred',
        correlationId: error.headers.get('X-Correlation-ID') || undefined,
        originalError: { status: error.status, body: text },
      };
    } catch {
      // If parsing fails, return basic error info
      return {
        title: `Error ${error.status}`,
        message: error.statusText || 'An error occurred',
        correlationId: error.headers.get('X-Correlation-ID') || undefined,
        originalError: error,
      };
    }
  }

  // For non-Response errors, use the centralized handler
  const friendlyError = handleApiError(error);
  return mapToLegacyParsedApiError(friendlyError);
}

/**
 * Make an API call and handle errors consistently
 */
export async function fetchWithErrorHandling(
  url: string,
  options?: RequestInit
): Promise<Response> {
  const response = await fetch(url, options);

  if (!response.ok) {
    throw response;
  }

  return response;
}

/**
 * Open logs folder in file explorer (platform-aware)
 */
export function openLogsFolder(): void {
  // Call API endpoint to open logs folder
  fetch('/api/logs/open-folder', {
    method: 'POST',
  }).catch(async (error) => {
    logger.error(
      'Failed to open logs folder',
      error instanceof Error ? error : new Error(String(error)),
      'apiErrorHandler',
      'openLogsFolder'
    );
    // Fallback: try to navigate to logs page
    const { navigateToRoute } = await import('./navigation');
    navigateToRoute('/logs');
  });
}
