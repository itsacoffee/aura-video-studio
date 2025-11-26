/**
 * HTTP Interceptor for global fetch requests
 * Handles 401 Unauthorized (redirect to login) and 500 Server Error (show toast)
 *
 * This module provides utilities to enhance the existing fetch interceptor
 */

import { useConnectionStore } from '../stores/connectionStore';

/**
 * Global handlers for toast and navigation (set from React context)
 */
let globalToastHandler: ((message: string, type?: 'error' | 'warning' | 'info') => void) | null =
  null;
let globalNavigationHandler: ((path: string) => void) | null = null;

/**
 * Set global toast handler (called from App.tsx)
 */
export function setHttpInterceptorToastHandler(
  handler: (message: string, type?: 'error' | 'warning' | 'info') => void
) {
  globalToastHandler = handler;
}

/**
 * Set global navigation handler (called from App.tsx)
 */
export function setHttpInterceptorNavigationHandler(handler: (path: string) => void) {
  globalNavigationHandler = handler;
}

/**
 * Wrap fetch response to handle 401 and 500 errors
 * Call this after getting a response from fetch
 */
export async function handleHttpErrorResponse(response: Response, url: string): Promise<Response> {
  // Handle 401 Unauthorized - redirect to login
  if (response.status === 401 && url.includes('/api/')) {
    // Clear any auth tokens
    localStorage.removeItem('auth_token');

    // Only redirect if not already on login page
    if (!window.location.pathname.includes('/login')) {
      // Try to use navigation service if available, otherwise use window.location
      if (globalNavigationHandler) {
        globalNavigationHandler('/login');
      } else {
        window.location.href = '/login';
      }
    }

    // Update connection store
    useConnectionStore.getState().setStatus('offline');
    useConnectionStore.getState().setError('Authentication required');
  }

  // Handle 500 Server Error - show user-friendly toast
  if (response.status === 500 && url.includes('/api/')) {
    const errorMessage =
      'The server encountered an error. Please try again later or contact support if the problem persists.';

    if (globalToastHandler) {
      globalToastHandler(errorMessage, 'error');
    } else {
      // Fallback to console if toast not available yet
      console.error('[HTTP Interceptor] Server Error:', errorMessage);
    }

    // Update connection store
    useConnectionStore.getState().setError('Server error occurred');
  }

  // Update connection store for successful requests
  if (response.ok && url.includes('/api/')) {
    useConnectionStore.getState().setStatus('online');
    useConnectionStore.getState().setError(null);
  }

  return response;
}

/**
 * Handle fetch errors (network failures, etc.)
 * @param error - The error that occurred
 * @param _url - The URL that was being fetched (for logging/debugging)
 */
export function handleHttpError(error: unknown, _url: string): void {
  // Handle network errors
  if (error instanceof TypeError && error.message.includes('fetch')) {
    useConnectionStore.getState().setStatus('offline');
    useConnectionStore.getState().setError('Network connection failed');

    if (globalToastHandler) {
      globalToastHandler('Unable to connect to the server. Please check your connection.', 'error');
    }
  }
}
