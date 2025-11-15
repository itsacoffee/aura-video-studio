/**
 * Centralized error handler for API errors with user-friendly messages
 */

import { AxiosError } from 'axios';
import { loggingService } from '../loggingService';

/**
 * Standard error response from backend
 */
export interface StandardErrorResponse {
  type: string;
  title: string;
  status: number;
  detail: string;
  correlationId: string;
  errorCode: string;
  howToFix?: string[];
  fieldErrors?: Record<string, string>;
  context?: Record<string, unknown>;
}

/**
 * User-friendly error information for display
 */
export interface UserFriendlyError {
  title: string;
  message: string;
  errorCode?: string;
  correlationId?: string;
  actions: ErrorAction[];
  technicalDetails?: string;
  learnMoreUrl?: string;
}

/**
 * Actionable steps the user can take
 */
export interface ErrorAction {
  label: string;
  description: string;
  link?: string;
  action?: () => void | Promise<void>;
}

/**
 * Error category for consistent grouping
 */
export enum ErrorCategory {
  Network = 'Network',
  Validation = 'Validation',
  Provider = 'Provider',
  Configuration = 'Configuration',
  Authentication = 'Authentication',
  Unknown = 'Unknown',
}

/**
 * Maps error codes to user-friendly messages and actions
 */
const ERROR_CODE_MAPPINGS: Record<string, UserFriendlyError> = {
  // Network Errors
  NET001_BackendUnreachable: {
    title: 'Backend Service Not Reachable',
    message: 'Cannot connect to the backend service. The API server is not responding.',
    errorCode: 'NET001_BackendUnreachable',
    actions: [
      {
        label: 'Check Backend Status',
        description: 'Verify the backend service is running',
      },
      {
        label: 'Check Firewall',
        description: 'Ensure no firewall is blocking the connection',
      },
      {
        label: 'Restart Application',
        description: 'Try restarting the entire application',
      },
    ],
    learnMoreUrl:
      'https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/network-errors.md#backend-unreachable',
  },

  NET002_DnsResolutionFailed: {
    title: 'DNS Resolution Failed',
    message: 'Cannot resolve the hostname. DNS lookup failed.',
    errorCode: 'NET002_DnsResolutionFailed',
    actions: [
      {
        label: 'Check Internet Connection',
        description: 'Verify you are connected to the internet',
      },
      {
        label: 'Try Alternative DNS',
        description: 'Use Google DNS (8.8.8.8) or Cloudflare DNS (1.1.1.1)',
      },
      {
        label: 'Flush DNS Cache',
        description: 'Clear your DNS cache and try again',
      },
    ],
    learnMoreUrl:
      'https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/network-errors.md#dns-resolution-failed',
  },

  NET003_TlsHandshakeFailed: {
    title: 'Secure Connection Failed',
    message: 'Failed to establish a secure connection. SSL/TLS error.',
    errorCode: 'NET003_TlsHandshakeFailed',
    actions: [
      {
        label: 'Update System Certificates',
        description: 'Update your operating system security certificates',
      },
      {
        label: 'Check System Time',
        description: 'Ensure your system date and time are correct',
      },
      {
        label: 'Contact IT',
        description: 'If on a corporate network, contact your IT department',
      },
    ],
    learnMoreUrl:
      'https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/network-errors.md#tls-handshake-failed',
  },

  NET004_NetworkTimeout: {
    title: 'Request Timed Out',
    message: 'The request took too long to complete and timed out.',
    errorCode: 'NET004_NetworkTimeout',
    actions: [
      {
        label: 'Check Connection Speed',
        description: 'Verify your internet connection is stable',
      },
      {
        label: 'Retry',
        description: 'Wait a moment and try again',
      },
      {
        label: 'Use Wired Connection',
        description: 'Try using a wired connection instead of WiFi',
      },
    ],
    learnMoreUrl:
      'https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/network-errors.md#network-timeout',
  },

  NET006_CorsMisconfigured: {
    title: 'CORS Configuration Error',
    message: 'Cross-origin request blocked. The origin is not allowed by CORS policy.',
    errorCode: 'NET006_CorsMisconfigured',
    actions: [
      {
        label: 'Check Configuration',
        description: 'Verify CORS settings in backend configuration',
      },
      {
        label: 'Restart Application',
        description: 'Restart both frontend and backend',
      },
    ],
    learnMoreUrl:
      'https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/network-errors.md#cors-misconfigured',
  },

  NET007_ProviderUnavailable: {
    title: 'Provider Service Unavailable',
    message: 'Cannot connect to the external provider service.',
    errorCode: 'NET007_ProviderUnavailable',
    actions: [
      {
        label: 'Check Provider Status',
        description: 'Visit the provider status page for outages',
      },
      {
        label: 'Verify API Key',
        description: 'Ensure your API key is valid and has not expired',
      },
      {
        label: 'Wait and Retry',
        description: 'The service may be temporarily down',
      },
    ],
    learnMoreUrl:
      'https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/provider-errors.md',
  },

  // Validation Errors
  VAL001_InvalidInput: {
    title: 'Invalid Input',
    message: 'The provided input is invalid. Please check your input and try again.',
    errorCode: 'VAL001_InvalidInput',
    actions: [
      {
        label: 'Review Input',
        description: 'Check all fields for errors',
      },
      {
        label: 'Check Requirements',
        description: 'Ensure all required fields are filled correctly',
      },
    ],
    learnMoreUrl:
      'https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/validation-errors.md#invalid-input',
  },

  VAL002_MissingRequiredField: {
    title: 'Missing Required Field',
    message: 'One or more required fields are missing.',
    errorCode: 'VAL002_MissingRequiredField',
    actions: [
      {
        label: 'Fill Required Fields',
        description: 'Complete all fields marked as required',
      },
    ],
    learnMoreUrl:
      'https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/validation-errors.md#missing-required-field',
  },

  // Authentication Errors
  AUTH001_ApiKeyMissing: {
    title: 'API Key Missing',
    message: 'A required API key has not been configured.',
    errorCode: 'AUTH001_ApiKeyMissing',
    actions: [
      {
        label: 'Configure API Key',
        description: 'Add your API key in Settings → Providers',
        link: '/settings/providers',
      },
    ],
    learnMoreUrl:
      'https://github.com/Coffee285/aura-video-studio/blob/main/docs/setup/api-keys.md',
  },

  AUTH002_ApiKeyInvalid: {
    title: 'Invalid API Key',
    message: 'The provided API key is invalid or has been revoked.',
    errorCode: 'AUTH002_ApiKeyInvalid',
    actions: [
      {
        label: 'Update API Key',
        description: 'Check and update your API key in Settings',
        link: '/settings/providers',
      },
      {
        label: 'Verify Key Format',
        description: 'Ensure the API key is in the correct format',
      },
    ],
    learnMoreUrl:
      'https://github.com/Coffee285/aura-video-studio/blob/main/docs/setup/api-keys.md',
  },

  AUTH006_RateLimitExceeded: {
    title: 'Rate Limit Exceeded',
    message: "You've exceeded the rate limit for this service.",
    errorCode: 'AUTH006_RateLimitExceeded',
    actions: [
      {
        label: 'Wait Before Retrying',
        description: 'Wait a few minutes before trying again',
      },
      {
        label: 'Upgrade Plan',
        description: 'Consider upgrading your service plan',
      },
    ],
    learnMoreUrl:
      'https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/provider-errors.md#rate-limits',
  },
};

/**
 * Get error category from error code
 */
function getErrorCategory(errorCode?: string): ErrorCategory {
  if (!errorCode) return ErrorCategory.Unknown;

  if (errorCode.startsWith('NET')) return ErrorCategory.Network;
  if (errorCode.startsWith('VAL')) return ErrorCategory.Validation;
  if (errorCode.startsWith('AUTH')) return ErrorCategory.Authentication;
  if (errorCode.startsWith('CFG')) return ErrorCategory.Configuration;
  if (errorCode.startsWith('E100') || errorCode.startsWith('E200') || errorCode.startsWith('E400'))
    return ErrorCategory.Provider;

  return ErrorCategory.Unknown;
}

/**
 * Parse and handle API errors with user-friendly messages
 */
export function handleApiError(error: unknown): UserFriendlyError {
  loggingService.debug('Handling API error', 'errorHandler', undefined, { error });

  // Check if it's an Axios error with response
  if (error && typeof error === 'object' && 'isAxiosError' in error) {
    const axiosError = error as AxiosError<StandardErrorResponse>;

    // Backend returned structured error response
    if (axiosError.response?.data) {
      const backendError = axiosError.response.data;

      // Use predefined mapping if available
      if (backendError.errorCode && ERROR_CODE_MAPPINGS[backendError.errorCode]) {
        const mapping = ERROR_CODE_MAPPINGS[backendError.errorCode];
        return {
          ...mapping,
          message: backendError.detail || mapping.message,
          correlationId: backendError.correlationId,
          technicalDetails: backendError.detail,
        };
      }

      // Use backend error response directly
      return {
        title: backendError.title || 'Error',
        message: backendError.detail || 'An error occurred',
        errorCode: backendError.errorCode,
        correlationId: backendError.correlationId,
        actions: backendError.howToFix
          ? backendError.howToFix.map((step, index) => ({
              label: `Step ${index + 1}`,
              description: step,
            }))
          : [
              {
                label: 'Try Again',
                description: 'Retry the operation',
              },
            ],
        technicalDetails: backendError.detail,
        learnMoreUrl: backendError.type,
      };
    }

    // Network error without response (backend unreachable)
    if (axiosError.code === 'ERR_NETWORK' || axiosError.code === 'ECONNREFUSED') {
      return {
        ...ERROR_CODE_MAPPINGS.NET001_BackendUnreachable,
        technicalDetails: axiosError.message,
      };
    }

    // Timeout error
    if (axiosError.code === 'ECONNABORTED' || axiosError.message.includes('timeout')) {
      return {
        ...ERROR_CODE_MAPPINGS.NET004_NetworkTimeout,
        technicalDetails: axiosError.message,
      };
    }

    // Generic network error
    return {
      title: 'Network Error',
      message: `Failed to connect to the server: ${axiosError.message}`,
      errorCode: 'NET_UNKNOWN',
      actions: [
        {
          label: 'Check Connection',
          description: 'Verify your internet connection',
        },
        {
          label: 'Retry',
          description: 'Try the operation again',
        },
      ],
      technicalDetails: axiosError.message,
      learnMoreUrl:
        'https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/network-errors.md',
    };
  }

  // Generic error fallback
  const errorMessage = error instanceof Error ? error.message : String(error);

  return {
    title: 'Unexpected Error',
    message: errorMessage || 'An unexpected error occurred',
    errorCode: 'ERR_UNKNOWN',
    actions: [
      {
        label: 'Try Again',
        description: 'Retry the operation',
      },
      {
        label: 'Check Logs',
        description: 'Review application logs for more details',
      },
    ],
    technicalDetails: errorMessage,
    learnMoreUrl:
      'https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/general-errors.md',
  };
}

/**
 * Parse API error for simple error message display
 */
export function parseApiError(error: unknown): { message: string; code?: string } {
  const friendlyError = handleApiError(error);
  return {
    message: friendlyError.message,
    code: friendlyError.errorCode,
  };
}

/**
 * Check if error is a network error
 */
export function isNetworkError(error: unknown): boolean {
  const friendlyError = handleApiError(error);
  return getErrorCategory(friendlyError.errorCode) === ErrorCategory.Network;
}

/**
 * Check if error is a validation error
 */
export function isValidationError(error: unknown): boolean {
  const friendlyError = handleApiError(error);
  return getErrorCategory(friendlyError.errorCode) === ErrorCategory.Validation;
}

/**
 * Check if error is an authentication error
 */
export function isAuthenticationError(error: unknown): boolean {
  const friendlyError = handleApiError(error);
  return getErrorCategory(friendlyError.errorCode) === ErrorCategory.Authentication;
}
