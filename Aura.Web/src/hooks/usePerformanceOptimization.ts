/**
 * Custom hooks for performance optimization
 * Provides memoization, debouncing, and throttling utilities
 */

import React, { useCallback, useRef, useEffect, useMemo, useState } from 'react';

/**
 * Hook for debouncing a value - delays updating until after a wait period
 * Useful for search inputs and other user inputs that trigger expensive operations
 */
export function useDebounce<T>(value: T, delay: number): T {
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    return () => {
      clearTimeout(handler);
    };
  }, [value, delay]);

  return debouncedValue;
}

/**
 * Hook for debouncing a callback function
 * Useful for event handlers that should not fire too frequently
 */
export function useDebouncedCallback<T extends (...args: unknown[]) => void>(
  callback: T,
  delay: number
): T {
  const timeoutRef = useRef<ReturnType<typeof setTimeout>>();

  const debouncedCallback = useCallback(
    (...args: Parameters<T>) => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }

      timeoutRef.current = setTimeout(() => {
        callback(...args);
      }, delay);
    },
    [callback, delay]
  ) as T;

  useEffect(() => {
    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
    };
  }, []);

  return debouncedCallback;
}

/**
 * Hook for throttling a callback function
 * Ensures the callback is not called more than once per time period
 */
export function useThrottledCallback<T extends (...args: unknown[]) => void>(
  callback: T,
  delay: number
): T {
  const lastRunRef = useRef<number>(0);
  const timeoutRef = useRef<ReturnType<typeof setTimeout>>();

  const throttledCallback = useCallback(
    (...args: Parameters<T>) => {
      const now = Date.now();
      const timeSinceLastRun = now - lastRunRef.current;

      if (timeSinceLastRun >= delay) {
        callback(...args);
        lastRunRef.current = now;
      } else {
        if (timeoutRef.current) {
          clearTimeout(timeoutRef.current);
        }

        timeoutRef.current = setTimeout(() => {
          callback(...args);
          lastRunRef.current = Date.now();
        }, delay - timeSinceLastRun);
      }
    },
    [callback, delay]
  ) as T;

  useEffect(() => {
    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
    };
  }, []);

  return throttledCallback;
}

/**
 * Hook for tracking component mount state to prevent state updates on unmounted components
 */
export function useIsMounted(): () => boolean {
  const isMountedRef = useRef<boolean>(true);

  useEffect(() => {
    return () => {
      isMountedRef.current = false;
    };
  }, []);

  return useCallback(() => isMountedRef.current, []);
}

/**
 * Hook for lazy initialization of expensive values
 * Similar to useMemo but only runs once on mount
 */
export function useLazyInit<T>(initializer: () => T): T {
  const valueRef = useRef<T | null>(null);

  if (valueRef.current === null) {
    valueRef.current = initializer();
  }

  return valueRef.current;
}

/**
 * Hook for memoizing expensive computations with dependencies
 * Same as useMemo but with better performance tracking in dev mode
 */
export function useOptimizedMemo<T>(
  factory: () => T,
  deps: React.DependencyList,
  debugLabel?: string
): T {
  return useMemo(() => {
    if (import.meta.env.DEV && debugLabel) {
      const start = performance.now();
      const result = factory();
      const duration = performance.now() - start;

      if (duration > 16) {
        console.warn(
          `[Performance] Expensive memo computation in ${debugLabel}: ${duration.toFixed(2)}ms`
        );
      }

      return result;
    }

    return factory();
  }, deps);
}

/**
 * Hook for lazy loading images with intersection observer
 */
export function useLazyImage(src: string): {
  imageSrc: string | undefined;
  isLoaded: boolean;
  ref: React.RefObject<HTMLElement>;
} {
  const [imageSrc, setImageSrc] = useState<string>();
  const [isLoaded, setIsLoaded] = useState(false);
  const ref = useRef<HTMLElement>(null);

  useEffect(() => {
    if (!ref.current) return;

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            setImageSrc(src);
            observer.disconnect();
          }
        });
      },
      {
        rootMargin: '50px',
      }
    );

    observer.observe(ref.current);

    return () => {
      observer.disconnect();
    };
  }, [src]);

  useEffect(() => {
    if (!imageSrc) return;

    const img = new Image();
    img.onload = () => setIsLoaded(true);
    img.src = imageSrc;
  }, [imageSrc]);

  return { imageSrc, isLoaded, ref };
}

/**
 * Hook for viewport visibility detection
 * Useful for pausing animations or stopping heavy operations when component is not visible
 */
export function useIsInViewport(ref: React.RefObject<HTMLElement>): boolean {
  const [isInViewport, setIsInViewport] = useState(false);

  useEffect(() => {
    if (!ref.current) return;

    const observer = new IntersectionObserver(
      ([entry]) => {
        setIsInViewport(entry.isIntersecting);
      },
      {
        threshold: 0.1,
      }
    );

    observer.observe(ref.current);

    return () => {
      observer.disconnect();
    };
  }, [ref]);

  return isInViewport;
}
