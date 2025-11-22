/**
 * ErrorBoundary Tests
 * Tests for the improved ErrorBoundary fallback UI
 */

import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { ErrorBoundary } from '../ErrorBoundary';

// Mock loggingService
vi.mock('../../services/loggingService', () => ({
  loggingService: {
    error: vi.fn(),
  },
}));

// Component that throws an error
function ThrowError({ shouldThrow }: { shouldThrow: boolean }) {
  if (shouldThrow) {
    throw new Error('Test error message');
  }
  return <div>No error</div>;
}

describe('ErrorBoundary', () => {
  beforeEach(() => {
    // Mock console methods to avoid noise in test output
    vi.spyOn(console, 'error').mockImplementation(() => {});
    vi.spyOn(console, 'log').mockImplementation(() => {});
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders children when no error occurs', () => {
    render(
      <ErrorBoundary>
        <ThrowError shouldThrow={false} />
      </ErrorBoundary>
    );

    expect(screen.getByText('No error')).toBeInTheDocument();
  });

  it('displays visible error fallback UI when error is caught', () => {
    render(
      <ErrorBoundary>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    // Check for the new improved UI elements
    expect(screen.getByText('Application Error')).toBeInTheDocument();
    expect(
      screen.getByText('The application encountered an unexpected error during initialization.')
    ).toBeInTheDocument();
  });

  it('displays error ID in fallback UI', () => {
    const { container } = render(
      <ErrorBoundary>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    // Error ID label should be displayed
    expect(screen.getByText(/Error ID:/)).toBeInTheDocument();

    // The full error ID should follow the format ERR-<timestamp>-<random>
    const errorIdContainer = container.querySelector(
      '[style*="background-color: rgb(26, 26, 26)"]'
    );
    expect(errorIdContainer?.textContent).toMatch(/Error ID:\s*ERR-\d+-[a-z0-9]+/);
  });

  it('displays technical details in collapsible section', () => {
    render(
      <ErrorBoundary>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    // Technical details should be in a details element
    const detailsElement = screen.getByText('Technical Details');
    expect(detailsElement).toBeInTheDocument();

    // Error message should be displayed
    expect(screen.getByText(/Test error message/)).toBeInTheDocument();
  });

  it('has Reload Application button that triggers window reload', () => {
    const reloadSpy = vi.fn();
    Object.defineProperty(window, 'location', {
      writable: true,
      value: { reload: reloadSpy },
    });

    render(
      <ErrorBoundary>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    const reloadButton = screen.getByText('Reload Application');
    expect(reloadButton).toBeInTheDocument();

    fireEvent.click(reloadButton);
    expect(reloadSpy).toHaveBeenCalledTimes(1);
  });

  it('has Try to Recover button that attempts to reset error state', () => {
    let shouldThrow = true;

    const TestComponent = () => {
      if (shouldThrow) {
        throw new Error('Test error message');
      }
      return <div>Recovered successfully</div>;
    };

    const { rerender } = render(
      <ErrorBoundary>
        <TestComponent />
      </ErrorBoundary>
    );

    // Error should be displayed
    expect(screen.getByText('Application Error')).toBeInTheDocument();

    const recoverButton = screen.getByText('Try to Recover');
    expect(recoverButton).toBeInTheDocument();

    // Stop throwing and click recover
    shouldThrow = false;
    fireEvent.click(recoverButton);

    // Force a rerender after clicking recover
    rerender(
      <ErrorBoundary>
        <TestComponent />
      </ErrorBoundary>
    );

    // After recovery, should show normal content
    expect(screen.getByText('Recovered successfully')).toBeInTheDocument();
  });

  it('uses custom fallback when provided', () => {
    const customFallback = <div>Custom Error UI</div>;

    render(
      <ErrorBoundary fallback={customFallback}>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    expect(screen.getByText('Custom Error UI')).toBeInTheDocument();
    expect(screen.queryByText('Application Error')).not.toBeInTheDocument();
  });

  it('calls custom onError handler when provided', () => {
    const onErrorSpy = vi.fn();

    render(
      <ErrorBoundary onError={onErrorSpy}>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    expect(onErrorSpy).toHaveBeenCalledTimes(1);
    expect(onErrorSpy).toHaveBeenCalledWith(
      expect.any(Error),
      expect.objectContaining({
        componentStack: expect.any(String),
      })
    );
  });

  it('ensures fallback UI has full viewport height with dark theme', () => {
    const { container } = render(
      <ErrorBoundary>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    // Get the outer container div
    const outerDiv = container.firstChild as HTMLElement;

    // Check for dark background and full height
    expect(outerDiv.style.minHeight).toBe('100vh');
    expect(outerDiv.style.backgroundColor).toBe('rgb(30, 30, 30)'); // #1e1e1e
    expect(outerDiv.style.display).toBe('flex');
    expect(outerDiv.style.alignItems).toBe('center');
    expect(outerDiv.style.justifyContent).toBe('center');
  });

  it('generates unique error IDs for different errors', () => {
    const { container: container1, unmount } = render(
      <ErrorBoundary>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    const firstErrorIdContainer = container1.querySelector(
      '[style*="background-color: rgb(26, 26, 26)"]'
    );
    const firstErrorIdMatch = firstErrorIdContainer?.textContent?.match(/ERR-\d+-[a-z0-9]+/);
    const firstErrorId = firstErrorIdMatch ? firstErrorIdMatch[0] : '';

    unmount();

    // Wait a moment to ensure timestamp difference
    vi.useFakeTimers();
    vi.advanceTimersByTime(10);
    vi.useRealTimers();

    // Render again with a new error
    const { container: container2 } = render(
      <ErrorBoundary>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    const secondErrorIdContainer = container2.querySelector(
      '[style*="background-color: rgb(26, 26, 26)"]'
    );
    const secondErrorIdMatch = secondErrorIdContainer?.textContent?.match(/ERR-\d+-[a-z0-9]+/);
    const secondErrorId = secondErrorIdMatch ? secondErrorIdMatch[0] : '';

    // Error IDs should be different
    expect(firstErrorId).toBeTruthy();
    expect(secondErrorId).toBeTruthy();
    expect(firstErrorId).not.toBe(secondErrorId);
  });
});
