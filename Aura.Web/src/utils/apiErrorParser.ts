/**
 * Utility for parsing API errors and determining error types
 */

import type { AxiosError } from 'axios';
import type { ErrorType } from '../components/contentPlanning/ErrorState';

export interface ParsedApiError {
  type: ErrorType;
  message: string;
  details?: string;
  retryable: boolean;
}

/**
 * Parse axios error and determine error type
 */
export function parseApiError(error: unknown): ParsedApiError {
  if (!error) {
    return {
      type: 'unknown',
      message: 'An unexpected error occurred',
      retryable: false,
    };
  }

  const axiosError = error as AxiosError;

  // Timeout errors (check this first before generic network error)
  if (axiosError.code === 'ECONNABORTED' || axiosError.message?.includes('timeout')) {
    return {
      type: 'timeout',
      message: 'The request took too long to complete. Please try again.',
      retryable: true,
    };
  }

  // Network errors (no response received)
  if (axiosError.code === 'ERR_NETWORK') {
    return {
      type: 'network',
      message: 'Unable to connect to the service. Please check your internet connection.',
      retryable: true,
    };
  }

  // Response errors
  if (axiosError.response) {
    const status = axiosError.response.status;
    const responseData = axiosError.response.data as Record<string, unknown> | undefined;

    switch (status) {
      case 401:
        return {
          type: 'auth',
          message: 'Your API key is invalid or has expired. Please update it in Settings.',
          details: 'Unauthorized access',
          retryable: false,
        };

      case 403:
        return {
          type: 'auth',
          message: 'Access denied. Please check your API key permissions.',
          details: 'Forbidden',
          retryable: false,
        };

      case 429: {
        const retryAfter = axiosError.response.headers?.['retry-after'];
        const retryMessage = retryAfter
          ? `Please try again in ${retryAfter} seconds.`
          : 'Please try again in a few minutes.';
        return {
          type: 'rateLimit',
          message: `Rate limit exceeded. ${retryMessage}`,
          details: responseData?.message as string | undefined,
          retryable: true,
        };
      }

      case 500:
      case 502:
      case 503:
      case 504:
        return {
          type: 'server',
          message: 'The service is temporarily unavailable. Please try again later.',
          details: `Server error (${status})`,
          retryable: true,
        };

      case 400:
        return {
          type: 'unknown',
          message: (responseData?.message as string) || 'Invalid request. Please check your input.',
          details: 'Bad request',
          retryable: false,
        };

      case 404:
        return {
          type: 'unknown',
          message: 'The requested resource was not found.',
          details: 'Not found',
          retryable: false,
        };

      default:
        return {
          type: 'unknown',
          message:
            (responseData?.message as string) || 'An unexpected error occurred. Please try again.',
          details: `HTTP ${status}`,
          retryable: status >= 500,
        };
    }
  }

  // Generic errors
  if (error instanceof Error) {
    return {
      type: 'unknown',
      message: error.message || 'An unexpected error occurred',
      retryable: false,
    };
  }

  return {
    type: 'unknown',
    message: 'An unexpected error occurred',
    retryable: false,
  };
}
