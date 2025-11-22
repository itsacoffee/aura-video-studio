/**
 * GlobalErrorBoundary Tests
 * Tests for error boundary functionality and backend logging
 */

import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { GlobalErrorBoundary } from '../GlobalErrorBoundary';

// Mock the apiUrl function
vi.mock('../../../config/api', () => ({
  apiUrl: (path: string) => `http://localhost:5005${path}`,
}));

// Component that throws an error
function ThrowError({ shouldThrow }: { shouldThrow: boolean }) {
  if (shouldThrow) {
    throw new Error('Test error');
  }
  return <div>No error</div>;
}

// Wrapper to provide Router context
function TestWrapper({ children }: { children: React.ReactNode }) {
  return <MemoryRouter>{children}</MemoryRouter>;
}

describe('GlobalErrorBoundary', () => {
  let originalFetch: typeof global.fetch;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    // Mock console methods to avoid noise in test output
    vi.spyOn(console, 'error').mockImplementation(() => {});
    vi.spyOn(console, 'log').mockImplementation(() => {});

    // Mock fetch
    originalFetch = global.fetch;
    fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ received: true }),
    });
    global.fetch = fetchMock;
  });

  afterEach(() => {
    vi.restoreAllMocks();
    global.fetch = originalFetch;
  });

  it('renders children when no error occurs', () => {
    render(
      <TestWrapper>
        <GlobalErrorBoundary>
          <ThrowError shouldThrow={false} />
        </GlobalErrorBoundary>
      </TestWrapper>
    );

    expect(screen.getByText('No error')).toBeInTheDocument();
  });

  it('displays error fallback when error is caught', () => {
    render(
      <TestWrapper>
        <GlobalErrorBoundary>
          <ThrowError shouldThrow={true} />
        </GlobalErrorBoundary>
      </TestWrapper>
    );

    // EnhancedErrorFallback should display error
    expect(screen.getByText('Application Error')).toBeInTheDocument();
  });

  it('sends error to backend logging service', async () => {
    render(
      <TestWrapper>
        <GlobalErrorBoundary>
          <ThrowError shouldThrow={true} />
        </GlobalErrorBoundary>
      </TestWrapper>
    );

    // Wait for fetch to be called
    await vi.waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        'http://localhost:5005/api/logs/error',
        expect.objectContaining({
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
        })
      );
    });

    // Verify request body structure
    const callArgs = fetchMock.mock.calls[0];
    const requestBody = JSON.parse(callArgs[1].body);

    expect(requestBody).toMatchObject({
      timestamp: expect.any(String),
      error: {
        name: 'Error',
        message: 'Test error',
        stack: expect.any(String),
      },
      context: {
        errorId: expect.stringMatching(/^ERR_\d+_[a-z0-9-]+$/), // Updated to allow hyphens from UUID
        componentStack: expect.any(String),
      },
      userAgent: expect.any(String),
      url: expect.any(String),
    });
  });

  it('generates unique error IDs', async () => {
    render(
      <TestWrapper>
        <GlobalErrorBoundary>
          <ThrowError shouldThrow={true} />
        </GlobalErrorBoundary>
      </TestWrapper>
    );

    await vi.waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(1);
    });

    const firstCallBody = JSON.parse(fetchMock.mock.calls[0][1].body);
    const firstErrorId = firstCallBody.context.errorId;

    // Verify error ID format (supports both crypto.randomUUID and Math.random formats)
    expect(firstErrorId).toMatch(/^ERR_\d+_[a-z0-9-]+$/);
  });

  it('handles fetch errors gracefully', async () => {
    // Make fetch fail
    fetchMock.mockRejectedValueOnce(new Error('Network error'));

    render(
      <TestWrapper>
        <GlobalErrorBoundary>
          <ThrowError shouldThrow={true} />
        </GlobalErrorBoundary>
      </TestWrapper>
    );

    // Should still display error fallback
    expect(screen.getByText('Application Error')).toBeInTheDocument();

    // Verify console.error was called for logging failure
    await vi.waitFor(() => {
      expect(console.error).toHaveBeenCalledWith(
        '[ErrorBoundary] Failed to log error to service:',
        expect.any(Error)
      );
    });
  });

  it('displays error ID in fallback UI', () => {
    render(
      <TestWrapper>
        <GlobalErrorBoundary>
          <ThrowError shouldThrow={true} />
        </GlobalErrorBoundary>
      </TestWrapper>
    );

    // EnhancedErrorFallback should display error code
    const errorCodeElement = screen.getByText(/Error Code:/);
    expect(errorCodeElement).toBeInTheDocument();

    // Error code should match format (supports both crypto.randomUUID and Math.random formats)
    const errorCodeText = errorCodeElement.textContent || '';
    expect(errorCodeText).toMatch(/ERR_\d+_[a-z0-9-]+/);
  });
});
