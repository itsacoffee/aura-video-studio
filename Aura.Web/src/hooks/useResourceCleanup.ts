import { useEffect, useRef, useCallback } from 'react';

/**
 * Registry for managing cleanup of various resources
 */
interface ResourceCleanupRegistry {
  registerTimeout: (id: number) => void;
  registerInterval: (id: number) => void;
  registerBlobUrl: (url: string) => void;
  registerEventListener: (
    element: EventTarget,
    event: string,
    handler: EventListener,
    options?: AddEventListenerOptions | boolean
  ) => void;
  registerCleanup: (cleanup: () => void) => void;
  cleanup: () => void;
}

/**
 * Hook for managing resource cleanup to prevent memory leaks
 *
 * Automatically cleans up all registered resources on component unmount:
 * - Timeouts (clearTimeout)
 * - Intervals (clearInterval)
 * - Blob URLs (URL.revokeObjectURL)
 * - Event listeners (removeEventListener)
 * - Custom cleanup functions
 *
 * @example
 * ```tsx
 * const { registerTimeout, registerBlobUrl, registerInterval } = useResourceCleanup();
 *
 * // Register timeout
 * const timeoutId = setTimeout(() => {}, 1000);
 * registerTimeout(timeoutId);
 *
 * // Register blob URL
 * const blobUrl = URL.createObjectURL(blob);
 * registerBlobUrl(blobUrl);
 *
 * // Register interval
 * const intervalId = setInterval(() => {}, 1000);
 * registerInterval(intervalId);
 * ```
 */
export function useResourceCleanup(): ResourceCleanupRegistry {
  const timeoutsRef = useRef<Set<number>>(new Set());
  const intervalsRef = useRef<Set<number>>(new Set());
  const blobUrlsRef = useRef<Set<string>>(new Set());
  const eventListenersRef = useRef<
    Array<{
      element: EventTarget;
      event: string;
      handler: EventListener;
      options?: AddEventListenerOptions | boolean;
    }>
  >([]);
  const customCleanupRef = useRef<Array<() => void>>([]);

  const registerTimeout = useCallback((id: number) => {
    timeoutsRef.current.add(id);
  }, []);

  const registerInterval = useCallback((id: number) => {
    intervalsRef.current.add(id);
  }, []);

  const registerBlobUrl = useCallback((url: string) => {
    blobUrlsRef.current.add(url);

    // Track in dev mode for memory profiling
    if (import.meta.env.DEV && typeof window !== 'undefined') {
      const auraMemory = (window as typeof window & { __AURA_BLOB_COUNT__?: number })
        .__AURA_BLOB_COUNT__;
      if (typeof auraMemory === 'number') {
        (window as typeof window & { __AURA_BLOB_COUNT__?: number }).__AURA_BLOB_COUNT__ =
          auraMemory + 1;
      } else {
        (window as typeof window & { __AURA_BLOB_COUNT__?: number }).__AURA_BLOB_COUNT__ = 1;
      }
    }
  }, []);

  const registerEventListener = useCallback(
    (
      element: EventTarget,
      event: string,
      handler: EventListener,
      options?: AddEventListenerOptions | boolean
    ) => {
      element.addEventListener(event, handler, options);
      eventListenersRef.current.push({ element, event, handler, options });
    },
    []
  );

  const registerCleanup = useCallback((cleanup: () => void) => {
    customCleanupRef.current.push(cleanup);
  }, []);

  const cleanup = useCallback(() => {
    // Clear timeouts
    timeoutsRef.current.forEach((id) => {
      clearTimeout(id);
    });
    timeoutsRef.current.clear();

    // Clear intervals
    intervalsRef.current.forEach((id) => {
      clearInterval(id);
    });
    intervalsRef.current.clear();

    // Revoke blob URLs
    blobUrlsRef.current.forEach((url) => {
      try {
        URL.revokeObjectURL(url);

        // Track in dev mode for memory profiling
        if (import.meta.env.DEV && typeof window !== 'undefined') {
          const auraMemory = (window as typeof window & { __AURA_BLOB_COUNT__?: number })
            .__AURA_BLOB_COUNT__;
          if (typeof auraMemory === 'number' && auraMemory > 0) {
            (window as typeof window & { __AURA_BLOB_COUNT__?: number }).__AURA_BLOB_COUNT__ =
              auraMemory - 1;
          }
        }
      } catch (error) {
        // Blob URL might already be revoked, ignore error
        if (import.meta.env.DEV) {
          // eslint-disable-next-line no-console
          console.debug('Failed to revoke blob URL:', url, error);
        }
      }
    });
    blobUrlsRef.current.clear();

    // Remove event listeners
    eventListenersRef.current.forEach(({ element, event, handler, options }) => {
      try {
        element.removeEventListener(event, handler, options);
      } catch (error) {
        // Element might be removed from DOM, ignore error
        if (import.meta.env.DEV) {
          // eslint-disable-next-line no-console
          console.debug('Failed to remove event listener:', event, error);
        }
      }
    });
    eventListenersRef.current = [];

    // Run custom cleanup functions
    customCleanupRef.current.forEach((fn) => {
      try {
        fn();
      } catch (error) {
        console.error('Error in custom cleanup function:', error);
      }
    });
    customCleanupRef.current = [];
  }, []);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      cleanup();
    };
  }, [cleanup]);

  return {
    registerTimeout,
    registerInterval,
    registerBlobUrl,
    registerEventListener,
    registerCleanup,
    cleanup,
  };
}
