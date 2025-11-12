import { ReactNode } from 'react';
import { RouteErrorBoundary } from './ErrorBoundary/RouteErrorBoundary';

interface RouteWrapperProps {
  children: ReactNode;
  onRetry?: () => void | Promise<void>;
  routePath?: string;
}

/**
 * Wrapper component that provides error boundary for individual routes
 */
export function RouteWrapper({ children, onRetry, routePath }: RouteWrapperProps) {
  return (
    <RouteErrorBoundary onRetry={onRetry} routePath={routePath}>
      {children}
    </RouteErrorBoundary>
  );
}
