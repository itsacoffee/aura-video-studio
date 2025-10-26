/**
 * Loading Priority System
 *
 * Manages the loading order of components to ensure critical UI elements
 * load first while deferring non-critical components.
 */

import { createContext, useContext, useState, useEffect, ReactNode } from 'react';

export enum LoadingPriority {
  CRITICAL = 0, // Must load immediately (layout, navigation)
  HIGH = 1, // Important for initial view (preview, timeline)
  MEDIUM = 2, // Visible but can be deferred (effects panels)
  LOW = 3, // Below the fold or optional (advanced features)
  IDLE = 4, // Load when browser is idle (analytics, non-essential)
}

interface LoadingPriorityContextValue {
  currentPriority: LoadingPriority;
  shouldLoad: (priority: LoadingPriority) => boolean;
  registerComponent: (id: string, priority: LoadingPriority) => void;
  unregisterComponent: (id: string) => void;
}

const LoadingPriorityContext = createContext<LoadingPriorityContextValue | null>(null);

interface LoadingPriorityProviderProps {
  children: ReactNode;
  /**
   * Delay between loading different priority levels in milliseconds
   */
  priorityDelay?: number;
}

/**
 * Provider for the loading priority system
 */
export function LoadingPriorityProvider({
  children,
  priorityDelay = 100,
}: LoadingPriorityProviderProps) {
  const [currentPriority, setCurrentPriority] = useState<LoadingPriority>(LoadingPriority.CRITICAL);
  const [registeredComponents] = useState<Map<string, LoadingPriority>>(new Map());

  useEffect(() => {
    // Progressive loading: increase priority level over time
    const timers: ReturnType<typeof setTimeout>[] = [];

    // Load CRITICAL immediately (already set)

    // Load HIGH after delay
    timers.push(
      setTimeout(() => {
        setCurrentPriority(LoadingPriority.HIGH);
      }, priorityDelay)
    );

    // Load MEDIUM after more delay
    timers.push(
      setTimeout(() => {
        setCurrentPriority(LoadingPriority.MEDIUM);
      }, priorityDelay * 2)
    );

    // Load LOW after more delay
    timers.push(
      setTimeout(() => {
        setCurrentPriority(LoadingPriority.LOW);
      }, priorityDelay * 3)
    );

    // Load IDLE when browser is idle or after final delay
    if ('requestIdleCallback' in window) {
      const idleId = requestIdleCallback(() => {
        setCurrentPriority(LoadingPriority.IDLE);
      });
      return () => {
        timers.forEach(clearTimeout);
        cancelIdleCallback(idleId);
      };
    } else {
      timers.push(
        setTimeout(() => {
          setCurrentPriority(LoadingPriority.IDLE);
        }, priorityDelay * 4)
      );
    }

    return () => {
      timers.forEach(clearTimeout);
    };
  }, [priorityDelay]);

  const shouldLoad = (priority: LoadingPriority): boolean => {
    return priority <= currentPriority;
  };

  const registerComponent = (id: string, priority: LoadingPriority): void => {
    registeredComponents.set(id, priority);
  };

  const unregisterComponent = (id: string): void => {
    registeredComponents.delete(id);
  };

  const value: LoadingPriorityContextValue = {
    currentPriority,
    shouldLoad,
    registerComponent,
    unregisterComponent,
  };

  return (
    <LoadingPriorityContext.Provider value={value}>{children}</LoadingPriorityContext.Provider>
  );
}

/**
 * Hook to access loading priority context
 */
export function useLoadingPriority() {
  const context = useContext(LoadingPriorityContext);
  if (!context) {
    throw new Error('useLoadingPriority must be used within LoadingPriorityProvider');
  }
  return context;
}

interface PriorityLoadProps {
  priority: LoadingPriority;
  children: ReactNode;
  fallback?: ReactNode;
}

/**
 * Component that only renders its children when its priority level is reached
 */
export function PriorityLoad({ priority, children, fallback = null }: PriorityLoadProps) {
  const { shouldLoad } = useLoadingPriority();

  if (!shouldLoad(priority)) {
    return <>{fallback}</>;
  }

  return <>{children}</>;
}
