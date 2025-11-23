/**
 * Centralized error handler for API errors with user-friendly messages
 */

import { AxiosError } from 'axios';
import { apiUrl } from '../../config/api';
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
  howToFix?: string[];
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
    learnMoreUrl: 'https://github.com/Coffee285/aura-video-studio/blob/main/docs/setup/api-keys.md',
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
    learnMoreUrl: 'https://github.com/Coffee285/aura-video-studio/blob/main/docs/setup/api-keys.md',
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

  // FFmpeg Errors
  E302: {
    title: 'FFmpeg Not Found',
    message: 'FFmpeg is not installed or cannot be found on your system.',
    errorCode: 'E302',
    actions: [
      {
        label: 'Install FFmpeg',
        description: 'Click the Install FFmpeg button to download and install automatically',
      },
      {
        label: 'Add to PATH',
        description: 'If manually installed, add FFmpeg to your system PATH',
      },
    ],
    learnMoreUrl:
      'https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/ffmpeg-errors.md#e302',
  },

  E303: {
    title: 'Invalid FFmpeg Installation',
    message: 'FFmpeg was found but is invalid or corrupted.',
    errorCode: 'E303',
    actions: [
      {
        label: 'Reinstall FFmpeg',
        description: 'Remove the current installation and install again',
      },
      {
        label: 'Check Version',
        description: 'Ensure FFmpeg version is 4.0 or higher',
      },
    ],
    learnMoreUrl:
      'https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/ffmpeg-errors.md#e303',
  },

  E348: {
    title: 'FFmpeg Download Timeout',
    message: 'Download timed out. This may be due to slow network connection or large file size.',
    errorCode: 'E348',
    actions: [
      {
        label: 'Check Internet Speed',
        description: 'Verify your internet connection is stable',
      },
      {
        label: 'Retry Later',
        description: 'Try again when network conditions improve',
      },
      {
        label: 'Manual Install',
        description: 'Download FFmpeg manually from ffmpeg.org',
      },
    ],
    learnMoreUrl:
      'https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/ffmpeg-errors.md#e348',
  },

  E349: {
    title: 'FFmpeg Network Error',
    message: 'Network error occurred during FFmpeg download.',
    errorCode: 'E349',
    actions: [
      {
        label: 'Check Connection',
        description: 'Verify your internet connection',
      },
      {
        label: 'Check Firewall',
        description: 'Ensure firewall is not blocking downloads',
      },
      {
        label: 'Manual Install',
        description: 'Download FFmpeg manually and use "Use Existing FFmpeg"',
      },
    ],
    learnMoreUrl:
      'https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/ffmpeg-errors.md#e349',
  },

  E350: {
    title: 'FFmpeg File Corrupted',
    message: 'Downloaded file is corrupted or incomplete.',
    errorCode: 'E350',
    actions: [
      {
        label: 'Clear Cache',
        description: 'Clear browser cache and try again',
      },
      {
        label: 'Check Disk Space',
        description: 'Ensure sufficient disk space is available',
      },
      {
        label: 'Disable Antivirus',
        description: 'Temporarily disable antivirus during download',
      },
    ],
    learnMoreUrl:
      'https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/ffmpeg-errors.md#e350',
  },

  E351: {
    title: 'DNS Resolution Failed',
    message: 'Unable to resolve the FFmpeg download server hostname.',
    errorCode: 'E351',
    actions: [
      {
        label: 'Check DNS Settings',
        description: 'Try using Google DNS (8.8.8.8) or Cloudflare DNS (1.1.1.1)',
      },
      {
        label: 'Check Internet',
        description: 'Verify you are connected to the internet',
      },
    ],
    learnMoreUrl:
      'https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/ffmpeg-errors.md#e351',
  },

  E352: {
    title: 'Secure Connection Failed',
    message: 'Failed to establish a secure connection to the download server.',
    errorCode: 'E352',
    actions: [
      {
        label: 'Check System Time',
        description: 'Ensure your system date and time are correct',
      },
      {
        label: 'Update OS',
        description: 'Update your operating system security certificates',
      },
    ],
    learnMoreUrl:
      'https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/ffmpeg-errors.md#e352',
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
  if (errorCode.startsWith('E3')) return ErrorCategory.Configuration; // FFmpeg errors

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
        howToFix: backendError.howToFix,
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
 * Legacy ParsedApiError type for backward compatibility with contentPlanning components
 */
export interface ParsedApiError {
  type: 'network' | 'auth' | 'rateLimit' | 'timeout' | 'server' | 'unknown';
  message: string;
  details?: string;
  retryable: boolean;
}

/**
 * Legacy detailed error type for backward compatibility with wizard components
 */
export interface DetailedApiError {
  title: string;
  message: string;
  errorDetails?: string;
  correlationId?: string;
  errorCode?: string;
  originalError: unknown;
}

/**
 * Extract error code from type URI (e.g., "https://github.com/.../README.md#E300" -> "E300")
 */
function extractErrorCodeFromType(type?: string): string | undefined {
  if (!type) return undefined;

  const match = type.match(/E\d{3,}/);
  return match ? match[0] : undefined;
}

/**
 * Parse API error with full details for backward compatibility
 * Handles Response objects and other error types
 */
export async function parseApiError(error: unknown): Promise<DetailedApiError> {
  // If it's a Response object (from fetch), we need to handle it specially
  if (error instanceof Response) {
    try {
      const contentType = error.headers.get('content-type');

      // Try to parse as JSON (ProblemDetails)
      if (
        contentType?.includes('application/json') ||
        contentType?.includes('application/problem+json')
      ) {
        const problemDetails: StandardErrorResponse = await error.json();

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
 * Parse API error with detailed type information for backward compatibility
 * Used by content planning components
 */
export function parseApiErrorDetailed(error: unknown): ParsedApiError {
  const friendlyError = handleApiError(error);

  // Determine ErrorType from error code or title
  let type: ParsedApiError['type'] = 'unknown';

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

/**
 * Open logs folder in file explorer (platform-aware)
 */
export function openLogsFolder(): void {
  // Call API endpoint to open logs folder
  fetch(apiUrl('/api/logs/open-folder'), {
    method: 'POST',
  }).catch(async (error) => {
    loggingService.error(
      'Failed to open logs folder',
      error instanceof Error ? error : new Error(String(error)),
      'errorHandler',
      'openLogsFolder'
    );
    // Fallback: try to navigate to logs page
    const { navigateToRoute } = await import('../../utils/navigation');
    navigateToRoute('/logs');
  });
}
