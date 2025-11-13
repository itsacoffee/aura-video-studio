/**
 * Route Guard Component
 * Enforces route guards and handles guard failures
 */

import { Spinner } from '@fluentui/react-components';
import { useState, useEffect, type FC, type ReactNode, type ComponentType } from 'react';
import { Navigate } from 'react-router-dom';
import { loggingService } from '../services/loggingService';
import { navigationService } from '../services/navigationService';

interface RouteGuardProps {
  path: string;
  children: ReactNode;
  fallbackRoute?: string;
  showLoading?: boolean;
}

/**
 * RouteGuard Component
 * Checks route guards before rendering children
 */
export const RouteGuard: FC<RouteGuardProps> = ({
  path,
  children,
  fallbackRoute = '/',
  showLoading = true,
}) => {
  const [isChecking, setIsChecking] = useState(true);
  const [canAccess, setCanAccess] = useState(false);
  const [redirectPath, setRedirectPath] = useState<string | null>(null);

  useEffect(() => {
    let mounted = true;

    async function checkAccess() {
      try {
        const metadata = navigationService.getRouteMeta(path);

        if (!metadata || !metadata.guards || metadata.guards.length === 0) {
          if (mounted) {
            setCanAccess(true);
            setIsChecking(false);
          }
          return;
        }

        let allPassed = true;
        for (const guard of metadata.guards) {
          try {
            const result = await guard();
            if (!result) {
              allPassed = false;
              break;
            }
          } catch (error) {
            loggingService.error(
              'Guard execution failed',
              error as Error,
              'RouteGuard',
              'checkAccess',
              { path }
            );
            allPassed = false;
            break;
          }
        }

        if (mounted) {
          setCanAccess(allPassed);
          if (!allPassed) {
            loggingService.warn(`Access denied to ${path}`, 'RouteGuard', 'checkAccess');

            if (metadata.requiresFirstRun) {
              setRedirectPath('/setup');
            } else if (metadata.requiresFFmpeg) {
              setRedirectPath('/downloads');
            } else if (metadata.requiresSettings) {
              setRedirectPath('/settings');
            } else {
              setRedirectPath(fallbackRoute);
            }
          }
          setIsChecking(false);
        }
      } catch (error) {
        loggingService.error('Guard check failed', error as Error, 'RouteGuard', 'checkAccess', {
          path,
        });
        if (mounted) {
          setCanAccess(false);
          setRedirectPath(fallbackRoute);
          setIsChecking(false);
        }
      }
    }

    checkAccess();

    return () => {
      mounted = false;
    };
  }, [path, fallbackRoute]);

  if (isChecking) {
    return showLoading ? (
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          height: '100vh',
          flexDirection: 'column',
          gap: '16px',
        }}
      >
        <Spinner size="large" label="Checking prerequisites..." />
      </div>
    ) : null;
  }

  if (!canAccess && redirectPath) {
    return <Navigate to={redirectPath} replace />;
  }

  return <>{children}</>;
};

/**
 * Higher-order component to add guard protection to a route
 */
export function withRouteGuard<P extends object>(
  Component: ComponentType<P>,
  path: string,
  fallbackRoute?: string
): FC<P> {
  const GuardedComponent: FC<P> = (props) => {
    return (
      <RouteGuard path={path} fallbackRoute={fallbackRoute}>
        <Component {...props} />
      </RouteGuard>
    );
  };

  GuardedComponent.displayName = `withRouteGuard(${Component.displayName || Component.name || 'Component'})`;

  return GuardedComponent;
}
