/**
 * Protected Route Component
 * Wraps routes that require authentication
 */

import React, { useEffect } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { loggingService } from '../services/loggingService';

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredRole?: string;
  redirectTo?: string;
  fallback?: React.ReactNode;
}

export function ProtectedRoute({
  children,
  requiredRole,
  redirectTo = '/login',
  fallback,
}: ProtectedRouteProps) {
  const { isAuthenticated, user, isLoading, refreshUser } = useAuth();
  const location = useLocation();

  // Attempt to load user on mount if authenticated but no user data
  useEffect(() => {
    if (isAuthenticated && !user && !isLoading) {
      refreshUser().catch((error) => {
        loggingService.error(
          'Failed to load user in ProtectedRoute',
          error instanceof Error ? error : new Error(String(error)),
          'ProtectedRoute',
          'useEffect'
        );
      });
    }
  }, [isAuthenticated, user, isLoading, refreshUser]);

  // Show loading state
  if (isLoading) {
    if (fallback) {
      return <>{fallback}</>;
    }

    return (
      <div
        style={{
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          height: '100vh',
        }}
      >
        <div>Loading...</div>
      </div>
    );
  }

  // Redirect if not authenticated
  if (!isAuthenticated) {
    loggingService.warn('Unauthenticated access attempt', 'ProtectedRoute', 'render', {
      path: location.pathname,
    });

    return <Navigate to={redirectTo} state={{ from: location }} replace />;
  }

  // Check role if required
  if (requiredRole && user?.role !== requiredRole) {
    loggingService.warn('Unauthorized access attempt', 'ProtectedRoute', 'render', {
      path: location.pathname,
      requiredRole,
      userRole: user?.role,
    });

    return (
      <div
        style={{
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          height: '100vh',
          flexDirection: 'column',
          gap: '1rem',
        }}
      >
        <h1>Access Denied</h1>
        <p>You don't have permission to access this page.</p>
        <button
          onClick={() => window.history.back()}
          style={{
            padding: '0.5rem 1rem',
            backgroundColor: '#007bff',
            color: 'white',
            border: 'none',
            borderRadius: '4px',
            cursor: 'pointer',
          }}
        >
          Go Back
        </button>
      </div>
    );
  }

  return <>{children}</>;
}
