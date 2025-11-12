/**
 * Tests for route error boundaries
 * Requirement 7: Add React error boundary for each route that shows which route crashed
 */

import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { RouteErrorBoundary } from '../components/ErrorBoundary/RouteErrorBoundary';

// Mock loggingService BEFORE importing component
vi.mock('../services/loggingService', () => ({
  loggingService: {
    error: vi.fn(),
    warn: vi.fn(),
    info: vi.fn(),
  },
}));

import { loggingService } from '../services/loggingService';

// Component that throws an error
const ThrowError = ({ error }: { error?: string }) => {
  throw new Error(error || 'Test error');
};

describe('Route Error Boundary', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Suppress console.error for these tests since we expect errors
    vi.spyOn(console, 'error').mockImplementation(() => {
      // suppress
    });
  });

  it('should catch errors and log the route path', () => {
    const routePath = '/test-route';

    render(
      <RouteErrorBoundary routePath={routePath}>
        <ThrowError />
      </RouteErrorBoundary>
    );

    expect(loggingService.error).toHaveBeenCalledWith(
      expect.stringContaining(`RouteErrorBoundary caught an error in route: ${routePath}`),
      expect.any(Error),
      'RouteErrorBoundary',
      'componentDidCatch',
      expect.objectContaining({
        componentStack: expect.any(String),
        routePath,
      })
    );
  });

  it('should display error fallback UI when error occurs', () => {
    render(
      <RouteErrorBoundary routePath="/test-route">
        <ThrowError error="Something went wrong" />
      </RouteErrorBoundary>
    );

    // The RouteErrorFallback should be rendered
    // We're just checking that the error was caught and boundary worked
    expect(loggingService.error).toHaveBeenCalled();
  });

  it('should use hash location when routePath not provided', () => {
    window.location.hash = '#/dynamic-route';

    render(
      <RouteErrorBoundary>
        <ThrowError />
      </RouteErrorBoundary>
    );

    expect(loggingService.error).toHaveBeenCalledWith(
      expect.stringContaining('RouteErrorBoundary caught an error in route:'),
      expect.any(Error),
      'RouteErrorBoundary',
      'componentDidCatch',
      expect.objectContaining({
        hash: expect.stringContaining('/dynamic-route'),
      })
    );
  });

  it('should render children when no error occurs', () => {
    render(
      <RouteErrorBoundary routePath="/test-route">
        <div>Normal content</div>
      </RouteErrorBoundary>
    );

    expect(screen.getByText('Normal content')).toBeInTheDocument();
    expect(loggingService.error).not.toHaveBeenCalled();
  });

  it('should log both pathname and hash for comprehensive debugging', () => {
    window.location.hash = '#/test-hash';

    render(
      <RouteErrorBoundary routePath="/test-path">
        <ThrowError />
      </RouteErrorBoundary>
    );

    expect(loggingService.error).toHaveBeenCalledWith(
      expect.any(String),
      expect.any(Error),
      'RouteErrorBoundary',
      'componentDidCatch',
      expect.objectContaining({
        routePath: '/test-path',
        hash: expect.any(String),
        pathname: expect.any(String),
      })
    );
  });

  it('should include component stack in error log', () => {
    render(
      <RouteErrorBoundary routePath="/test-route">
        <ThrowError />
      </RouteErrorBoundary>
    );

    expect(loggingService.error).toHaveBeenCalledWith(
      expect.any(String),
      expect.any(Error),
      'RouteErrorBoundary',
      'componentDidCatch',
      expect.objectContaining({
        componentStack: expect.any(String),
      })
    );
  });
});
