/**
 * Utility for handling API errors and extracting ProblemDetails from responses
 */

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
 * Parse an error response from the API and extract ProblemDetails
 */
export async function parseApiError(error: unknown): Promise<ParsedApiError> {
  // If it's a Response object (from fetch)
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
    } catch (parseError) {
      // If parsing fails, return basic error info
      return {
        title: `Error ${error.status}`,
        message: error.statusText || 'An error occurred',
        correlationId: error.headers.get('X-Correlation-ID') || undefined,
        originalError: error,
      };
    }
  }

  // If it's already a parsed ProblemDetails object
  if (error && typeof error === 'object') {
    const errorObj = error as Partial<ProblemDetails & { message?: string }>;
    if (errorObj.title || errorObj.detail || errorObj.status) {
      return {
        title: errorObj.title || 'Error',
        message: errorObj.detail || errorObj.message || 'An error occurred',
        errorDetails: errorObj.detail,
        correlationId: errorObj.correlationId,
        errorCode: errorObj.errorCode || extractErrorCodeFromType(errorObj.type),
        originalError: error,
      };
    }

    // Standard Error object
    if (errorObj.message) {
      return {
        title: 'Error',
        message: errorObj.message,
        originalError: error,
      };
    }
  }

  // Fallback for unknown error types
  return {
    title: 'Error',
    message: String(error) || 'An unknown error occurred',
    originalError: error,
  };
}

/**
 * Extract error code from type URI (e.g., "https://docs.aura.studio/errors/E300" -> "E300")
 */
function extractErrorCodeFromType(type?: string): string | undefined {
  if (!type) return undefined;

  const match = type.match(/E\d{3,}/);
  return match ? match[0] : undefined;
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
  }).catch((error) => {
    logger.error(
      'Failed to open logs folder',
      error instanceof Error ? error : new Error(String(error)),
      'apiErrorHandler',
      'openLogsFolder'
    );
    // Fallback: try to navigate to logs page
    window.location.href = '/logs';
  });
}
