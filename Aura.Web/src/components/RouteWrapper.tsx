import { ReactNode } from 'react';
import { RouteErrorBoundary } from './ErrorBoundary/RouteErrorBoundary';

interface RouteWrapperProps {
  children: ReactNode;
  onRetry?: () => void | Promise<void>;
}

/**
 * Wrapper component that provides error boundary for individual routes
 */
export function RouteWrapper({ children, onRetry }: RouteWrapperProps) {
  return <RouteErrorBoundary onRetry={onRetry}>{children}</RouteErrorBoundary>;
}
