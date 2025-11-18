/**
 * useApiError Hook
 * Standardized error handling for API requests using centralized error handler
 */

import { useState, useCallback } from 'react';
import { handleApiError, type UserFriendlyError } from '../services/api/errorHandler';

export interface ApiErrorInfo {
  message: string;
  statusCode?: number;
  errorCode?: string;
  correlationId?: string;
  retryable: boolean;
  userMessage: string;
  actions?: Array<{
    label: string;
    description: string;
    link?: string;
    action?: () => void | Promise<void>;
  }>;
}

/**
 * Map UserFriendlyError to ApiErrorInfo
 */
function mapToApiErrorInfo(friendlyError: UserFriendlyError): ApiErrorInfo {
  return {
    message: friendlyError.message,
    errorCode: friendlyError.errorCode,
    correlationId: friendlyError.correlationId,
    retryable: friendlyError.actions.some((a) => a.label.toLowerCase().includes('retry')),
    userMessage: friendlyError.message,
    actions: friendlyError.actions,
  };
}

export interface UseApiErrorResult {
  error: Error | null;
  errorInfo: ApiErrorInfo | null;
  setError: (error: unknown) => void;
  clearError: () => void;
  isRetryable: boolean;
}

/**
 * Hook for standardized API error handling
 */
export function useApiError(): UseApiErrorResult {
  const [error, setErrorState] = useState<Error | null>(null);
  const [errorInfo, setErrorInfo] = useState<ApiErrorInfo | null>(null);

  const setError = useCallback((error: unknown) => {
    const friendlyError = handleApiError(error);
    const info = mapToApiErrorInfo(friendlyError);
    setErrorInfo(info);
    setErrorState(error instanceof Error ? error : new Error(info.message));
  }, []);

  const clearError = useCallback(() => {
    setErrorState(null);
    setErrorInfo(null);
  }, []);

  return {
    error,
    errorInfo,
    setError,
    clearError,
    isRetryable: errorInfo?.retryable ?? false,
  };
}
