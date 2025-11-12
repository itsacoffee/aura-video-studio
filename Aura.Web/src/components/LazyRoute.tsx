import { Spinner } from '@fluentui/react-components';
import { ReactNode, Suspense } from 'react';
import { RouteWrapper } from './RouteWrapper';

interface LazyRouteProps {
  children: ReactNode;
  routePath: string;
  fallback?: ReactNode;
}

/**
 * Wrapper for lazy-loaded routes that adds Suspense and error boundary
 * Requirement 7: Each route has an error boundary that logs which route crashed
 */
export function LazyRoute({ children, routePath, fallback }: LazyRouteProps) {
  return (
    <RouteWrapper routePath={routePath}>
      <Suspense fallback={fallback || <Spinner label="Loading..." />}>{children}</Suspense>
    </RouteWrapper>
  );
}
