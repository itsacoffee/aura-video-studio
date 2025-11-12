import { useCallback, useState } from 'react';
import type { ErrorInfo } from '../components/Errors/ErrorDialog';
import { errorHandlingService, type ErrorContext, type ErrorHandlingOptions } from '../services/errorHandlingService';

/**
 * Hook for handling errors with user feedback
 */
export function useErrorHandler() {
  const [currentError, setCurrentError] = useState<ErrorInfo | null>(null);
  const [showErrorDialog, setShowErrorDialog] = useState(false);

  const handleError = useCallback(
    async (
      error: Error,
      context?: ErrorContext,
      options?: ErrorHandlingOptions & { showDialog?: boolean }
    ) => {
      const result = await errorHandlingService.handleError(error, context, options);

      if (result.errorInfo && (options?.showDialog !== false)) {
        setCurrentError(result.errorInfo);
        setShowErrorDialog(true);
      }

      return result;
    },
    []
  );

  const clearError = useCallback(() => {
    setCurrentError(null);
    setShowErrorDialog(false);
  }, []);

  const retryOperation = useCallback(
    async (operation: () => Promise<void>) => {
      clearError();
      try {
        await operation();
      } catch (error) {
        await handleError(error as Error);
      }
    },
    [clearError, handleError]
  );

  return {
    currentError,
    showErrorDialog,
    handleError,
    clearError,
    retryOperation,
  };
}
