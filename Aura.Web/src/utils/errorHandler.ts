/**
 * Centralized Error Handler
 * Provides consistent error handling and user-friendly messages
 */

import { loggingService } from '../services/loggingService';
import { useAppStore } from '../stores/appStore';

export interface ErrorConfig {
  title?: string;
  message?: string;
  showNotification?: boolean;
  logError?: boolean;
  context?: string;
  severity?: 'error' | 'warning' | 'info';
}

/**
 * Error type definitions
 */
export class AppError extends Error {
  constructor(
    message: string,
    public code?: string,
    public statusCode?: number,
    public context?: string
  ) {
    super(message);
    this.name = 'AppError';
  }
}

export class NetworkError extends AppError {
  constructor(message: string = 'Network connection lost') {
    super(message, 'NETWORK_ERROR', 0, 'network');
    this.name = 'NetworkError';
  }
}

export class AuthenticationError extends AppError {
  constructor(message: string = 'Authentication failed') {
    super(message, 'AUTH_ERROR', 401, 'authentication');
    this.name = 'AuthenticationError';
  }
}

export class ValidationError extends AppError {
  constructor(message: string, public fields?: Record<string, string>) {
    super(message, 'VALIDATION_ERROR', 400, 'validation');
    this.name = 'ValidationError';
  }
}

export class NotFoundError extends AppError {
  constructor(message: string = 'Resource not found') {
    super(message, 'NOT_FOUND', 404, 'notFound');
    this.name = 'NotFoundError';
  }
}

export class PermissionError extends AppError {
  constructor(message: string = 'Permission denied') {
    super(message, 'PERMISSION_ERROR', 403, 'permission');
    this.name = 'PermissionError';
  }
}

export class ServerError extends AppError {
  constructor(message: string = 'Server error occurred') {
    super(message, 'SERVER_ERROR', 500, 'server');
    this.name = 'ServerError';
  }
}

/**
 * Get user-friendly error message
 */
export function getUserFriendlyMessage(error: unknown): string {
  if (error instanceof AppError) {
    return error.message;
  }

  if (error instanceof Error) {
    // Check for common error patterns
    if (error.message.includes('network') || error.message.includes('fetch')) {
      return 'Network connection lost. Please check your internet connection.';
    }

    if (error.message.includes('timeout')) {
      return 'Request timed out. Please try again.';
    }

    if (error.message.includes('unauthorized') || error.message.includes('401')) {
      return 'Your session has expired. Please log in again.';
    }

    if (error.message.includes('forbidden') || error.message.includes('403')) {
      return 'You don\'t have permission to perform this action.';
    }

    if (error.message.includes('not found') || error.message.includes('404')) {
      return 'The requested resource was not found.';
    }

    return error.message;
  }

  return 'An unexpected error occurred. Please try again.';
}

/**
 * Get error severity
 */
export function getErrorSeverity(error: unknown): 'error' | 'warning' | 'info' {
  if (error instanceof NetworkError) {
    return 'warning';
  }

  if (error instanceof ValidationError) {
    return 'warning';
  }

  if (error instanceof NotFoundError) {
    return 'info';
  }

  return 'error';
}

/**
 * Handle error with centralized logic
 */
export function handleError(error: unknown, config: ErrorConfig = {}): void {
  const {
    title = 'Error',
    message,
    showNotification = true,
    logError = true,
    context = 'app',
    severity,
  } = config;

  // Log error if enabled
  if (logError) {
    const errorObj = error instanceof Error ? error : new Error(String(error));
    loggingService.error('Error handled by centralized handler', errorObj, 'errorHandler', context);
  }

  // Show notification if enabled
  if (showNotification) {
    const notificationMessage = message || getUserFriendlyMessage(error);
    const notificationSeverity = severity || getErrorSeverity(error);

    const { addNotification } = useAppStore.getState();
    addNotification({
      type: notificationSeverity,
      title,
      message: notificationMessage,
      duration: notificationSeverity === 'error' ? 5000 : 3000,
    });
  }
}

/**
 * Create error handler for specific context
 */
export function createErrorHandler(context: string) {
  return (error: unknown, config: Omit<ErrorConfig, 'context'> = {}) => {
    handleError(error, { ...config, context });
  };
}

/**
 * Wrap async function with error handling
 */
export function withErrorHandling<T extends (...args: unknown[]) => Promise<unknown>>(
  fn: T,
  config: ErrorConfig = {}
): T {
  return (async (...args: unknown[]) => {
    try {
      return await fn(...args);
    } catch (error) {
      handleError(error, config);
      throw error;
    }
  }) as T;
}

/**
 * Retry function with exponential backoff
 */
export async function retryWithBackoff<T>(
  fn: () => Promise<T>,
  maxRetries: number = 3,
  baseDelay: number = 1000
): Promise<T> {
  let lastError: Error | undefined;

  for (let attempt = 0; attempt < maxRetries; attempt++) {
    try {
      return await fn();
    } catch (error) {
      lastError = error instanceof Error ? error : new Error(String(error));

      // Don't retry on non-retryable errors
      if (error instanceof ValidationError || error instanceof PermissionError) {
        throw error;
      }

      if (attempt < maxRetries - 1) {
        const delay = baseDelay * Math.pow(2, attempt);
        loggingService.warn(
          `Retry attempt ${attempt + 1}/${maxRetries} after ${delay}ms`,
          'errorHandler',
          'retryWithBackoff'
        );
        await new Promise((resolve) => setTimeout(resolve, delay));
      }
    }
  }

  throw lastError;
}

/**
 * Convert HTTP status code to appropriate error
 */
export function createErrorFromStatus(
  status: number,
  message?: string
): AppError {
  switch (status) {
    case 400:
      return new ValidationError(message || 'Invalid request');
    case 401:
      return new AuthenticationError(message);
    case 403:
      return new PermissionError(message);
    case 404:
      return new NotFoundError(message);
    case 500:
    case 502:
    case 503:
      return new ServerError(message);
    default:
      return new AppError(message || 'An error occurred', undefined, status);
  }
}

/**
 * Check if error is retryable
 */
export function isRetryableError(error: unknown): boolean {
  if (error instanceof NetworkError || error instanceof ServerError) {
    return true;
  }

  if (error instanceof AppError) {
    return error.statusCode ? error.statusCode >= 500 : false;
  }

  return false;
}

/**
 * Format error for display
 */
export function formatError(error: unknown): {
  title: string;
  message: string;
  details?: string;
} {
  if (error instanceof AppError) {
    return {
      title: error.name,
      message: error.message,
      details: error.context,
    };
  }

  if (error instanceof Error) {
    return {
      title: 'Error',
      message: getUserFriendlyMessage(error),
      details: error.stack,
    };
  }

  return {
    title: 'Unknown Error',
    message: String(error),
  };
}
