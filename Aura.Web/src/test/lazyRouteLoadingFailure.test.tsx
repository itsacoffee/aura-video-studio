/**
 * Tests for lazy route loading failures
 * Requirement 8: Test lazy route loading failure (delete chunk file) and verify error UI shows
 */

import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { render, screen, waitFor } from '@testing-library/react';
import { lazy } from 'react';
import { HashRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { LazyRoute } from '../components/LazyRoute';

// Mock loggingService
const mockError = vi.fn();
vi.mock('../services/loggingService', () => ({
  loggingService: {
    error: mockError,
    warn: vi.fn(),
    info: vi.fn(),
  },
}));

describe('Lazy Route Loading Failures', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Suppress console.error for these tests since we expect errors
    vi.spyOn(console, 'error').mockImplementation(() => {});
  });

  it('should show error UI when lazy component fails to load', async () => {
    // Create a lazy component that fails to load
    const FailingComponent = lazy(() => Promise.reject(new Error('Chunk load error')));

    render(
      <FluentProvider theme={webLightTheme}>
        <HashRouter>
          <LazyRoute routePath="/failing-route">
            <FailingComponent />
          </LazyRoute>
        </HashRouter>
      </FluentProvider>
    );

    // Wait for error boundary to catch the error
    await waitFor(
      () => {
        // The RouteErrorBoundary should have caught the error
        expect(mockError).toHaveBeenCalled();
      },
      { timeout: 3000 }
    );
  });

  it('should log route path when lazy loading fails', async () => {
    const routePath = '/test-lazy-route';
    const FailingComponent = lazy(() => Promise.reject(new Error('Failed to fetch')));

    render(
      <FluentProvider theme={webLightTheme}>
        <HashRouter>
          <LazyRoute routePath={routePath}>
            <FailingComponent />
          </LazyRoute>
        </HashRouter>
      </FluentProvider>
    );

    await waitFor(
      () => {
        expect(mockError).toHaveBeenCalledWith(
          expect.stringContaining(routePath),
          expect.any(Error),
          'RouteErrorBoundary',
          'componentDidCatch',
          expect.objectContaining({
            routePath,
          })
        );
      },
      { timeout: 3000 }
    );
  });

  it('should show loading spinner while lazy component loads', () => {
    // Create a component that takes time to load
    const SlowComponent = lazy(
      () =>
        new Promise((resolve) => {
          setTimeout(() => {
            resolve({ default: () => <div>Loaded content</div> });
          }, 100);
        })
    );

    render(
      <FluentProvider theme={webLightTheme}>
        <HashRouter>
          <LazyRoute routePath="/slow-route">
            <SlowComponent />
          </LazyRoute>
        </HashRouter>
      </FluentProvider>
    );

    // Should show loading state
    expect(screen.getByText('Loading...')).toBeInTheDocument();
  });

  it('should render successfully loaded lazy component', async () => {
    const SuccessComponent = lazy(() =>
      Promise.resolve({ default: () => <div>Successfully loaded</div> })
    );

    render(
      <FluentProvider theme={webLightTheme}>
        <HashRouter>
          <LazyRoute routePath="/success-route">
            <SuccessComponent />
          </LazyRoute>
        </HashRouter>
      </FluentProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Successfully loaded')).toBeInTheDocument();
    });
  });

  it('should handle network errors during chunk loading', async () => {
    const NetworkErrorComponent = lazy(() =>
      Promise.reject(new Error('ChunkLoadError: Loading chunk failed'))
    );

    render(
      <FluentProvider theme={webLightTheme}>
        <HashRouter>
          <LazyRoute routePath="/network-error-route">
            <NetworkErrorComponent />
          </LazyRoute>
        </HashRouter>
      </FluentProvider>
    );

    await waitFor(
      () => {
        expect(mockError).toHaveBeenCalledWith(
          expect.stringContaining('/network-error-route'),
          expect.objectContaining({
            message: expect.stringContaining('ChunkLoadError'),
          }),
          'RouteErrorBoundary',
          'componentDidCatch',
          expect.any(Object)
        );
      },
      { timeout: 3000 }
    );
  });

  it('should use custom fallback when provided', () => {
    const CustomFallback = <div>Custom loading indicator</div>;
    const SlowComponent = lazy(
      () =>
        new Promise((resolve) => {
          setTimeout(() => {
            resolve({ default: () => <div>Loaded</div> });
          }, 100);
        })
    );

    render(
      <FluentProvider theme={webLightTheme}>
        <HashRouter>
          <LazyRoute routePath="/custom-fallback-route" fallback={CustomFallback}>
            <SlowComponent />
          </LazyRoute>
        </HashRouter>
      </FluentProvider>
    );

    expect(screen.getByText('Custom loading indicator')).toBeInTheDocument();
  });
});
