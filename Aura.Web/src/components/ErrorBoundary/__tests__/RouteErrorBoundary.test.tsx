import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';
import { RouteErrorBoundary } from '../RouteErrorBoundary';

// Component that throws an error
function ErrorThrowingComponent() {
  throw new Error('Test error');
}

// Component that works
function WorkingComponent() {
  return <div>Working component</div>;
}

describe('RouteErrorBoundary', () => {
  it('should render children when there is no error', () => {
    render(
      <BrowserRouter>
        <RouteErrorBoundary>
          <WorkingComponent />
        </RouteErrorBoundary>
      </BrowserRouter>
    );

    expect(screen.getByText('Working component')).toBeInTheDocument();
  });

  it('should catch errors and display fallback UI', () => {
    // Suppress console.error for this test
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});

    render(
      <BrowserRouter>
        <RouteErrorBoundary>
          <ErrorThrowingComponent />
        </RouteErrorBoundary>
      </BrowserRouter>
    );

    expect(screen.getByText(/Oops! Something went wrong/i)).toBeInTheDocument();
    expect(screen.getByText(/An unexpected error occurred/i)).toBeInTheDocument();

    consoleError.mockRestore();
  });

  it('should show Try Again button', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});

    render(
      <BrowserRouter>
        <RouteErrorBoundary>
          <ErrorThrowingComponent />
        </RouteErrorBoundary>
      </BrowserRouter>
    );

    expect(screen.getByRole('button', { name: /Try Again/i })).toBeInTheDocument();

    consoleError.mockRestore();
  });

  it('should show Go to Home button', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});

    render(
      <BrowserRouter>
        <RouteErrorBoundary>
          <ErrorThrowingComponent />
        </RouteErrorBoundary>
      </BrowserRouter>
    );

    expect(screen.getByRole('button', { name: /Go to Home/i })).toBeInTheDocument();

    consoleError.mockRestore();
  });

  it('should call onRetry when Try Again is clicked', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    const onRetry = vi.fn(() => Promise.resolve());
    const user = userEvent.setup();

    render(
      <BrowserRouter>
        <RouteErrorBoundary onRetry={onRetry}>
          <ErrorThrowingComponent />
        </RouteErrorBoundary>
      </BrowserRouter>
    );

    const retryButton = screen.getByRole('button', { name: /Try Again/i });
    await user.click(retryButton);

    await waitFor(() => {
      expect(onRetry).toHaveBeenCalledTimes(1);
    });

    consoleError.mockRestore();
  });

  it('should display user-friendly network error message', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});

    function NetworkErrorComponent() {
      throw new Error('Network error: Failed to fetch');
    }

    render(
      <BrowserRouter>
        <RouteErrorBoundary>
          <NetworkErrorComponent />
        </RouteErrorBoundary>
      </BrowserRouter>
    );

    expect(screen.getByText(/Unable to connect to the server/i)).toBeInTheDocument();

    consoleError.mockRestore();
  });

  it('should display user-friendly 404 error message', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});

    function NotFoundErrorComponent() {
      throw new Error('404 Not Found');
    }

    render(
      <BrowserRouter>
        <RouteErrorBoundary>
          <NotFoundErrorComponent />
        </RouteErrorBoundary>
      </BrowserRouter>
    );

    expect(screen.getByText(/The requested resource was not found/i)).toBeInTheDocument();

    consoleError.mockRestore();
  });

  it('should display user-friendly timeout error message', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});

    function TimeoutErrorComponent() {
      throw new Error('Request timeout');
    }

    render(
      <BrowserRouter>
        <RouteErrorBoundary>
          <TimeoutErrorComponent />
        </RouteErrorBoundary>
      </BrowserRouter>
    );

    expect(screen.getByText(/The request took too long to complete/i)).toBeInTheDocument();

    consoleError.mockRestore();
  });
});
