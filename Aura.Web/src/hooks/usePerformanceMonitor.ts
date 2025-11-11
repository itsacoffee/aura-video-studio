import { useEffect, useRef } from 'react';

/**
 * Development-only hook to track component re-renders and their causes
 * Helps identify unnecessary re-renders during performance optimization
 *
 * Usage:
 * ```tsx
 * function MyComponent({ prop1, prop2 }) {
 *   useWhyDidYouUpdate('MyComponent', { prop1, prop2 });
 *   // ... rest of component
 * }
 * ```
 */
export function useWhyDidYouUpdate(componentName: string, props: Record<string, unknown>): void {
  // Only run in development
  if (process.env.NODE_ENV === 'production') {
    return;
  }

  const previousProps = useRef<Record<string, unknown>>();

  useEffect(() => {
    if (previousProps.current) {
      const allKeys = Object.keys({ ...previousProps.current, ...props });
      const changedProps: Record<string, { from: unknown; to: unknown }> = {};

      allKeys.forEach((key) => {
        if (previousProps.current![key] !== props[key]) {
          changedProps[key] = {
            from: previousProps.current![key],
            to: props[key],
          };
        }
      });

      if (Object.keys(changedProps).length > 0) {
        console.log(`[WhyDidYouUpdate] ${componentName} re-rendered due to:`, changedProps);
      }
    }

    previousProps.current = props;
  });
}

/**
 * Hook to measure and log component render time
 * Useful for identifying performance bottlenecks
 *
 * Usage:
 * ```tsx
 * function MyComponent() {
 *   useRenderTime('MyComponent');
 *   // ... rest of component
 * }
 * ```
 */
export function useRenderTime(componentName: string, threshold = 16): void {
  // Only run in development
  if (process.env.NODE_ENV === 'production') {
    return;
  }

  const renderStartTime = useRef<number>();

  if (!renderStartTime.current) {
    renderStartTime.current = performance.now();
  }

  useEffect(() => {
    if (renderStartTime.current) {
      const renderTime = performance.now() - renderStartTime.current;

      if (renderTime > threshold) {
        console.warn(
          `[RenderTime] ${componentName} took ${renderTime.toFixed(2)}ms to render (threshold: ${threshold}ms)`
        );
      }

      renderStartTime.current = undefined;
    }
  });
}

/**
 * Hook to track mount count and detect unnecessary remounts
 *
 * Usage:
 * ```tsx
 * function MyComponent() {
 *   useMountEffect('MyComponent');
 *   // ... rest of component
 * }
 * ```
 */
export function useMountEffect(componentName: string): void {
  // Only run in development
  if (process.env.NODE_ENV === 'production') {
    return;
  }

  const mountCount = useRef(0);

  useEffect(() => {
    mountCount.current += 1;

    if (mountCount.current > 1) {
      console.warn(
        `[MountEffect] ${componentName} has been mounted ${mountCount.current} times. Check if unnecessary remounts are occurring.`
      );
    }

    return () => {
      console.log(`[MountEffect] ${componentName} unmounted (mount count: ${mountCount.current})`);
    };
  }, [componentName]);
}

/**
 * Utility to batch multiple state updates
 * React 18 automatically batches updates in event handlers,
 * but this can be useful for manual batching in async code
 */
export { unstable_batchedUpdates as batchUpdates } from 'react-dom';
