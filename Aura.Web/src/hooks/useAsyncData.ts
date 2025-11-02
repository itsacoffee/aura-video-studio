import { useEffect, useCallback, useRef, useState } from 'react';
import { loggingService } from '../services/loggingService';

export interface AsyncDataState<T> {
  data: T | null;
  loading: boolean;
  error: Error | null;
}

export interface UseAsyncDataOptions<T> {
  /**
   * Initial data value
   */
  initialData?: T;
  /**
   * Whether to fetch immediately on mount
   */
  immediate?: boolean;
  /**
   * Callback when data is successfully loaded
   */
  onSuccess?: (data: T) => void;
  /**
   * Callback when an error occurs
   */
  onError?: (error: Error) => void;
  /**
   * Default value to use if the API returns empty/null
   */
  fallbackData?: T;
}

/**
 * Hook for loading async data with proper error handling and loading states
 * Handles empty responses gracefully and provides retry functionality
 */
export function useAsyncData<T>(
  fetchFn: () => Promise<T>,
  deps: unknown[] = [],
  options: UseAsyncDataOptions<T> = {}
): AsyncDataState<T> & { refetch: () => Promise<void>; reset: () => void } {
  const { initialData = null, immediate = true, onSuccess, onError, fallbackData } = options;

  const [data, setData] = useState<T | null>(initialData);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);
  const isMountedRef = useRef(true);
  const abortControllerRef = useRef<AbortController | null>(null);

  const fetchData = useCallback(async () => {
    // Cancel any pending request
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }

    // Create new abort controller
    abortControllerRef.current = new AbortController();

    setLoading(true);
    setError(null);

    try {
      const result = await fetchFn();

      // Only update state if component is still mounted
      if (isMountedRef.current) {
        // Handle empty/null responses
        const finalData = result ?? fallbackData ?? null;
        setData(finalData as T | null);
        setError(null);

        if (onSuccess && finalData !== null) {
          onSuccess(finalData as T);
        }
      }
    } catch (err: unknown) {
      if (isMountedRef.current) {
        const error = err instanceof Error ? err : new Error(String(err));

        // Don't treat aborted requests as errors
        if (error.name !== 'AbortError') {
          setError(error);
          setData(fallbackData ?? null);

          loggingService.error('Error fetching async data', error, 'useAsyncData', 'fetchData');

          if (onError) {
            onError(error);
          }
        }
      }
    } finally {
      if (isMountedRef.current) {
        setLoading(false);
      }
    }
    // Dependencies include fetchFn and options that affect the fetch behavior
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [fetchFn, fallbackData, onSuccess, onError, ...deps]);

  const reset = useCallback(() => {
    setData(initialData);
    setError(null);
    setLoading(false);
  }, [initialData]);

  useEffect(() => {
    isMountedRef.current = true;

    if (immediate) {
      fetchData();
    }

    return () => {
      isMountedRef.current = false;
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
    };
    // Dependencies include fetchData which contains all the fetch logic
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [immediate, fetchData, ...deps]);

  return {
    data,
    loading,
    error,
    refetch: fetchData,
    reset,
  };
}
