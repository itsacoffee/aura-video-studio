import { useState, useCallback } from 'react';

export interface LoadingState {
  isLoading: boolean;
  error: string | null;
  progress?: number;
  estimatedTimeRemaining?: number;
  status?: string;
}

export interface LoadingActions {
  startLoading: (status?: string) => void;
  stopLoading: () => void;
  setError: (error: string | null) => void;
  updateProgress: (progress: number, estimatedTimeRemaining?: number, status?: string) => void;
  reset: () => void;
}

/**
 * Custom hook for managing loading states with progress tracking
 * @param initialLoading - Initial loading state (default: false)
 * @returns Tuple of [LoadingState, LoadingActions]
 */
export function useLoadingState(
  initialLoading = false
): [LoadingState, LoadingActions] {
  const [state, setState] = useState<LoadingState>({
    isLoading: initialLoading,
    error: null,
    progress: undefined,
    estimatedTimeRemaining: undefined,
    status: undefined,
  });

  const startLoading = useCallback((status?: string) => {
    setState({
      isLoading: true,
      error: null,
      progress: undefined,
      estimatedTimeRemaining: undefined,
      status,
    });
  }, []);

  const stopLoading = useCallback(() => {
    setState((prev) => ({
      ...prev,
      isLoading: false,
      progress: undefined,
      estimatedTimeRemaining: undefined,
      status: undefined,
    }));
  }, []);

  const setError = useCallback((error: string | null) => {
    setState((prev) => ({
      ...prev,
      error,
      isLoading: false,
    }));
  }, []);

  const updateProgress = useCallback(
    (progress: number, estimatedTimeRemaining?: number, status?: string) => {
      setState((prev) => ({
        ...prev,
        progress,
        estimatedTimeRemaining,
        status: status || prev.status,
      }));
    },
    []
  );

  const reset = useCallback(() => {
    setState({
      isLoading: false,
      error: null,
      progress: undefined,
      estimatedTimeRemaining: undefined,
      status: undefined,
    });
  }, []);

  return [
    state,
    {
      startLoading,
      stopLoading,
      setError,
      updateProgress,
      reset,
    },
  ];
}

/**
 * Wrapper for async operations with automatic loading state management
 * @param loadingActions - Loading actions from useLoadingState
 * @param operation - Async operation to execute
 * @param errorMessage - Optional custom error message
 * @returns Result of the operation or undefined on error
 */
export async function withLoadingState<T>(
  loadingActions: LoadingActions,
  operation: () => Promise<T>,
  errorMessage?: string
): Promise<T | undefined> {
  loadingActions.startLoading();
  try {
    const result = await operation();
    loadingActions.stopLoading();
    return result;
  } catch (error) {
    const message =
      errorMessage || (error instanceof Error ? error.message : 'An error occurred');
    loadingActions.setError(message);
    return undefined;
  }
}
