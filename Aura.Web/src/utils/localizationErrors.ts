/**
 * Localization error handling utilities
 * Provides typed error parsing and user-friendly messages for localization operations
 */

/**
 * Error codes returned by the localization API
 */
export const LocalizationErrorCode = {
  INVALID_LANGUAGE: 'INVALID_LANGUAGE',
  INVALID_LANGUAGE_EMPTY: 'INVALID_LANGUAGE_EMPTY',
  INVALID_LANGUAGE_FORMAT: 'INVALID_LANGUAGE_FORMAT',
  LANGUAGE_NOT_IN_STANDARD_LIST: 'LANGUAGE_NOT_IN_STANDARD_LIST',
  EMPTY_CONTENT: 'EMPTY_CONTENT',
  TEXT_TOO_LONG: 'TEXT_TOO_LONG',
  TIMEOUT: 'TIMEOUT',
  CIRCUIT_BREAKER_OPEN: 'CIRCUIT_BREAKER_OPEN',
  PROVIDER_ERROR: 'PROVIDER_ERROR',
  INTERNAL_ERROR: 'INTERNAL_ERROR',
  INVALID_REQUEST: 'INVALID_REQUEST',
} as const;

export type LocalizationErrorCodeType =
  (typeof LocalizationErrorCode)[keyof typeof LocalizationErrorCode];

/**
 * Structure of error response from the localization API
 */
export interface LocalizationErrorResponse {
  type?: string;
  title: string;
  status: number;
  detail: string;
  correlationId?: string;
  errorCode?: LocalizationErrorCodeType;
  languageCode?: string;
  textLength?: number;
  maxLength?: number;
  timeoutSeconds?: number;
  retryAfterSeconds?: number;
  providerName?: string;
  isRetryable?: boolean;
  suggestedActions?: string[];
}

/**
 * Parsed error with user-friendly information
 */
export interface ParsedLocalizationError {
  title: string;
  message: string;
  errorCode: string;
  isRetryable: boolean;
  retryAfterSeconds?: number;
  suggestedActions: string[];
  correlationId?: string;
}

/**
 * Parse API error response into a structured format
 */
export function parseLocalizationError(error: unknown): ParsedLocalizationError {
  // Handle fetch/network errors
  if (error instanceof Error) {
    if (error.name === 'AbortError') {
      return {
        title: 'Request Cancelled',
        message: 'The operation was cancelled or timed out.',
        errorCode: 'TIMEOUT',
        isRetryable: true,
        suggestedActions: [
          'Check your internet connection',
          'Try again with shorter text',
          'Ensure the AI provider is running',
        ],
      };
    }

    // Network error
    if (error.message.includes('network') || error.message.includes('fetch')) {
      return {
        title: 'Connection Error',
        message: 'Unable to connect to the server. Please check your connection.',
        errorCode: 'NETWORK_ERROR',
        isRetryable: true,
        suggestedActions: [
          'Check your internet connection',
          'Verify the backend server is running',
          'Try again in a few moments',
        ],
      };
    }
  }

  // Handle API error responses (from response.json())
  if (typeof error === 'object' && error !== null) {
    const apiError = error as LocalizationErrorResponse;

    return {
      title: apiError.title || 'Localization Error',
      message: apiError.detail || 'An unexpected error occurred',
      errorCode: apiError.errorCode || 'UNKNOWN',
      isRetryable: apiError.isRetryable ?? isRetryableError(apiError.status),
      retryAfterSeconds: apiError.retryAfterSeconds,
      suggestedActions: apiError.suggestedActions || getDefaultActions(apiError.errorCode),
      correlationId: apiError.correlationId,
    };
  }

  // Fallback for unknown error types
  return {
    title: 'Unexpected Error',
    message: String(error),
    errorCode: 'UNKNOWN',
    isRetryable: true,
    suggestedActions: ['Try the operation again', 'Contact support if the problem persists'],
  };
}

/**
 * Determine if an error is retryable based on status code
 */
function isRetryableError(status?: number): boolean {
  if (!status) return true;
  // 408 (Timeout), 429 (Rate Limit), 500+ (Server errors) are retryable
  return status === 408 || status === 429 || status >= 500;
}

/**
 * Get default suggested actions for error codes
 */
function getDefaultActions(errorCode?: string): string[] {
  switch (errorCode) {
    case LocalizationErrorCode.INVALID_LANGUAGE:
    case LocalizationErrorCode.INVALID_LANGUAGE_EMPTY:
    case LocalizationErrorCode.INVALID_LANGUAGE_FORMAT:
      return [
        'Select a valid language from the dropdown',
        'Use ISO 639-1 language codes (e.g., "en", "es", "fr")',
      ];

    case LocalizationErrorCode.EMPTY_CONTENT:
      return ['Enter some text to translate', 'Provide content for cultural analysis'];

    case LocalizationErrorCode.TEXT_TOO_LONG:
      return [
        'Split your text into smaller sections',
        'Translate in multiple batches',
        'Remove unnecessary content before translating',
      ];

    case LocalizationErrorCode.TIMEOUT:
      return [
        'Try again with shorter text',
        'Check your internet connection',
        'Verify the AI provider is responding',
      ];

    case LocalizationErrorCode.CIRCUIT_BREAKER_OPEN:
      return [
        'Wait a few minutes before retrying',
        'Check if the AI provider is running',
        'Try using a different provider in settings',
      ];

    case LocalizationErrorCode.PROVIDER_ERROR:
      return [
        'Check your API key configuration',
        'Verify the AI provider is running',
        'Try using a different provider',
      ];

    default:
      return ['Try the operation again', 'Contact support if the problem persists'];
  }
}

/**
 * Get a user-friendly error message for display
 */
export function getUserFriendlyMessage(error: ParsedLocalizationError): string {
  switch (error.errorCode) {
    case LocalizationErrorCode.INVALID_LANGUAGE:
    case LocalizationErrorCode.INVALID_LANGUAGE_FORMAT:
      return 'The selected language is not valid. Please choose a supported language from the list.';

    case LocalizationErrorCode.EMPTY_CONTENT:
      return 'Please enter some text before requesting translation or analysis.';

    case LocalizationErrorCode.TEXT_TOO_LONG:
      return 'The text is too long. Please split it into smaller sections and translate each separately.';

    case LocalizationErrorCode.TIMEOUT:
      return 'The request took too long. This usually happens with large texts or when the AI service is busy.';

    case LocalizationErrorCode.CIRCUIT_BREAKER_OPEN:
      return 'The translation service is temporarily unavailable due to repeated errors. Please wait a moment and try again.';

    case LocalizationErrorCode.PROVIDER_ERROR:
      return 'There was a problem with the AI translation service. Check that your AI provider is configured and running.';

    default:
      return error.message;
  }
}

/**
 * Get specific guidance based on error code
 */
export function getErrorGuidance(errorCode: string): string {
  switch (errorCode) {
    case LocalizationErrorCode.INVALID_LANGUAGE:
    case LocalizationErrorCode.INVALID_LANGUAGE_FORMAT:
      return 'Language codes should be in ISO 639-1 format like "en" for English, "es" for Spanish, or "fr" for French. Regional variants like "es-MX" are also supported.';

    case LocalizationErrorCode.TEXT_TOO_LONG:
      return 'Large documents should be translated in sections. Consider breaking your content into paragraphs or scenes and translating each separately.';

    case LocalizationErrorCode.TIMEOUT:
      return 'Long texts take more time to process. If you frequently encounter timeouts, try translating smaller chunks or check if your AI provider needs more resources.';

    case LocalizationErrorCode.CIRCUIT_BREAKER_OPEN:
      return 'This protection activates when the translation service fails repeatedly. It prevents overloading the system and allows recovery time.';

    case LocalizationErrorCode.PROVIDER_ERROR:
      return 'The AI provider (like Ollama or OpenAI) encountered an error. Make sure Ollama is running if using local models, or check your API key if using cloud services.';

    default:
      return '';
  }
}

/**
 * Determine the severity level for UI styling
 */
export function getErrorSeverity(errorCode: string): 'info' | 'warning' | 'error' | 'critical' {
  switch (errorCode) {
    case LocalizationErrorCode.LANGUAGE_NOT_IN_STANDARD_LIST:
      return 'warning';

    case LocalizationErrorCode.EMPTY_CONTENT:
    case LocalizationErrorCode.INVALID_LANGUAGE:
    case LocalizationErrorCode.INVALID_LANGUAGE_EMPTY:
    case LocalizationErrorCode.INVALID_LANGUAGE_FORMAT:
    case LocalizationErrorCode.TEXT_TOO_LONG:
      return 'warning';

    case LocalizationErrorCode.TIMEOUT:
      return 'error';

    case LocalizationErrorCode.CIRCUIT_BREAKER_OPEN:
    case LocalizationErrorCode.PROVIDER_ERROR:
    case LocalizationErrorCode.INTERNAL_ERROR:
      return 'critical';

    default:
      return 'error';
  }
}
