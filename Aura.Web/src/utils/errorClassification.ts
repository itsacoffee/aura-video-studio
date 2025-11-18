/**
 * Comprehensive error classification and user-friendly message generation
 * PR 1: Fixes generic "Network Error" issues by providing specific error types and messages
 */

export enum ErrorCategory {
  /** Backend is unreachable (connection refused, DNS failure, etc.) */
  BACKEND_UNREACHABLE = 'BACKEND_UNREACHABLE',
  /** Request timed out */
  TIMEOUT = 'TIMEOUT',
  /** Circuit breaker is open (too many failures) */
  CIRCUIT_BREAKER_OPEN = 'CIRCUIT_BREAKER_OPEN',
  /** CORS policy blocked the request */
  CORS_ERROR = 'CORS_ERROR',
  /** Backend returned 400-499 client error */
  CLIENT_ERROR = 'CLIENT_ERROR',
  /** Backend returned 500-599 server error */
  SERVER_ERROR = 'SERVER_ERROR',
  /** Request was aborted/cancelled */
  ABORTED = 'ABORTED',
  /** Unknown error */
  UNKNOWN = 'UNKNOWN',
}

export interface ClassifiedError {
  category: ErrorCategory;
  title: string;
  message: string;
  technicalDetails: string;
  isRetryable: boolean;
  suggestedActions: string[];
  originalError: unknown;
}

/**
 * Classify an error from an HTTP request
 */
export function classifyError(error: unknown): ClassifiedError {
  // Handle axios errors
  if (error && typeof error === 'object' && 'isAxiosError' in error && error.isAxiosError) {
    const axiosError = error as {
      code?: string;
      message?: string;
      response?: {
        status?: number;
        data?: {
          title?: string;
          detail?: string;
          message?: string;
        };
      };
      request?: unknown;
    };

    // Timeout errors
    if (
      axiosError.code === 'ECONNABORTED' ||
      axiosError.message?.includes('timeout') ||
      axiosError.message?.includes('Timeout')
    ) {
      return {
        category: ErrorCategory.TIMEOUT,
        title: 'Request Timeout',
        message:
          'The server took too long to respond. This may be due to slow network or the service being overloaded.',
        technicalDetails: axiosError.message || 'Request timeout',
        isRetryable: true,
        suggestedActions: [
          'Wait a moment and try again',
          'Check your internet connection speed',
          'Try again during off-peak hours if the service is busy',
        ],
        originalError: error,
      };
    }

    // Backend responded with error
    if (axiosError.response) {
      const status = axiosError.response.status || 0;
      const responseData = axiosError.response.data;

      // Client errors (400-499)
      if (status >= 400 && status < 500) {
        const detail =
          responseData?.detail ||
          responseData?.message ||
          responseData?.title ||
          'The request was invalid';

        return {
          category: ErrorCategory.CLIENT_ERROR,
          title: responseData?.title || `Error ${status}`,
          message: detail,
          technicalDetails: `HTTP ${status}: ${detail}`,
          isRetryable: false,
          suggestedActions: getClientErrorActions(status, detail),
          originalError: error,
        };
      }

      // Server errors (500-599)
      if (status >= 500) {
        return {
          category: ErrorCategory.SERVER_ERROR,
          title: 'Server Error',
          message:
            'The server encountered an error processing your request. This is not your fault.',
          technicalDetails: `HTTP ${status}: ${axiosError.message}`,
          isRetryable: true,
          suggestedActions: [
            'Wait a moment and try again',
            'The issue may resolve itself shortly',
            'Contact support if the problem persists',
          ],
          originalError: error,
        };
      }
    }

    // Request was made but no response (network error)
    if (axiosError.request && !axiosError.response) {
      // Check for specific network error codes
      if (axiosError.code === 'ENOTFOUND' || axiosError.message?.includes('ENOTFOUND')) {
        return {
          category: ErrorCategory.BACKEND_UNREACHABLE,
          title: 'Cannot Reach Server',
          message:
            'Unable to connect to the server. The service may be offline or there may be a DNS issue.',
          technicalDetails: 'DNS lookup failed (ENOTFOUND)',
          isRetryable: true,
          suggestedActions: [
            'Check if the backend service is running',
            'Verify your network connection',
            'Check if you can access other websites',
            'Try again in a few moments',
          ],
          originalError: error,
        };
      }

      if (
        axiosError.code === 'ECONNREFUSED' ||
        axiosError.message?.includes('ECONNREFUSED') ||
        axiosError.message?.includes('ERR_CONNECTION_REFUSED')
      ) {
        return {
          category: ErrorCategory.BACKEND_UNREACHABLE,
          title: 'Connection Refused',
          message:
            'The backend server is not accepting connections. The service may not be running.',
          technicalDetails: 'Connection refused (ECONNREFUSED)',
          isRetryable: true,
          suggestedActions: [
            'Ensure the backend service is running (check Services or Task Manager)',
            'Verify the service is listening on the correct port',
            'Check firewall settings',
            'Restart the application',
          ],
          originalError: error,
        };
      }

      // Generic network error
      return {
        category: ErrorCategory.BACKEND_UNREACHABLE,
        title: 'Network Error',
        message:
          'Could not connect to the backend service. Check if the service is running and your network connection is active.',
        technicalDetails: axiosError.message || 'Network request failed',
        isRetryable: true,
        suggestedActions: [
          'Check if the backend service is running',
          'Verify your network connection',
          'Check firewall and antivirus settings',
          'Try restarting the application',
        ],
        originalError: error,
      };
    }
  }

  // Circuit breaker errors (specific marker)
  if (error instanceof Error && error.message?.includes('Circuit breaker is OPEN')) {
    return {
      category: ErrorCategory.CIRCUIT_BREAKER_OPEN,
      title: 'Service Temporarily Unavailable',
      message:
        'Too many recent failures. The service is temporarily blocked to prevent overload. Please wait a moment.',
      technicalDetails: error.message,
      isRetryable: true,
      suggestedActions: [
        'Wait 30-60 seconds before trying again',
        'Check if the backend service is running properly',
        'Look at the application logs for underlying issues',
      ],
      originalError: error,
    };
  }

  // AbortError (user cancelled or timeout)
  if (error instanceof Error && error.name === 'AbortError') {
    return {
      category: ErrorCategory.ABORTED,
      title: 'Request Cancelled',
      message: 'The request was cancelled or timed out.',
      technicalDetails: error.message,
      isRetryable: true,
      suggestedActions: ['Try the operation again', 'Ensure stable network connection'],
      originalError: error,
    };
  }

  // TypeError: Failed to fetch (CORS or network)
  if (error instanceof TypeError && error.message?.includes('Failed to fetch')) {
    return {
      category: ErrorCategory.BACKEND_UNREACHABLE,
      title: 'Cannot Connect to Service',
      message:
        'Unable to reach the backend service. This could be a network issue, CORS configuration problem, or the service may not be running.',
      technicalDetails: `TypeError: ${error.message}`,
      isRetryable: true,
      suggestedActions: [
        'Ensure the backend service is running',
        'Check your network connection',
        'Verify CORS is configured correctly (if running in development mode)',
        'Check browser console for detailed CORS errors',
        'Try restarting the backend service',
      ],
      originalError: error,
    };
  }

  // Generic Error object
  if (error instanceof Error) {
    return {
      category: ErrorCategory.UNKNOWN,
      title: 'Unexpected Error',
      message: error.message || 'An unexpected error occurred',
      technicalDetails: error.message,
      isRetryable: false,
      suggestedActions: ['Try the operation again', 'Contact support if the issue persists'],
      originalError: error,
    };
  }

  // Unknown error type
  return {
    category: ErrorCategory.UNKNOWN,
    title: 'Unknown Error',
    message: 'An unexpected error occurred. Please try again.',
    technicalDetails: String(error),
    isRetryable: false,
    suggestedActions: ['Try the operation again', 'Contact support if the issue persists'],
    originalError: error,
  };
}

/**
 * Get suggested actions for client errors based on status code
 */
function getClientErrorActions(status: number, _detail: string): string[] {
  switch (status) {
    case 400:
      return [
        'Check that all required fields are filled out correctly',
        'Verify the format of your input',
        'Review the error message for specific guidance',
      ];
    case 401:
      return [
        'Sign in again',
        'Check that your credentials are correct',
        'Your session may have expired',
      ];
    case 403:
      return [
        'You do not have permission for this operation',
        'Contact an administrator for access',
        'Ensure you are signed in with the correct account',
      ];
    case 404:
      return [
        'The requested resource was not found',
        'Check that the ID or path is correct',
        'The resource may have been deleted',
      ];
    case 409:
      return [
        'A conflict occurred (resource may already exist)',
        'Refresh and try again',
        'Check for duplicate entries',
      ];
    case 428:
      return [
        'Setup is required before using this feature',
        'Complete the initial setup wizard',
        'Configure required settings',
      ];
    case 429:
      return [
        'Too many requests - please slow down',
        'Wait a minute and try again',
        'Rate limit will reset shortly',
      ];
    default:
      return [
        'Check the error message for details',
        'Verify your input is correct',
        'Contact support if the issue persists',
      ];
  }
}

/**
 * Format a classified error for display
 */
export function formatErrorMessage(classified: ClassifiedError): string {
  return `${classified.title}: ${classified.message}`;
}

/**
 * Check if an error is retryable
 */
export function isRetryable(error: unknown): boolean {
  const classified = classifyError(error);
  return classified.isRetryable;
}

/**
 * Get suggested user actions for an error
 */
export function getSuggestedActions(error: unknown): string[] {
  const classified = classifyError(error);
  return classified.suggestedActions;
}
