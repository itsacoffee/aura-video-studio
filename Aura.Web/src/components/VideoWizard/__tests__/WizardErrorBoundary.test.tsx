import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { WizardErrorBoundary } from '../WizardErrorBoundary';

// Component that throws an error
const ThrowingComponent = ({ shouldThrow = true }: { shouldThrow?: boolean }) => {
  if (shouldThrow) {
    throw new Error('Test error message');
  }
  return <div>Component rendered successfully</div>;
};

// Good component that doesn't throw
const GoodComponent = () => {
  return <div>Good component content</div>;
};

describe('WizardErrorBoundary', () => {
  // Suppress console.error for error boundary tests
  const originalError = console.error;
  beforeEach(() => {
    console.error = vi.fn();
  });
  afterEach(() => {
    console.error = originalError;
  });

  it('renders children when no error occurs', () => {
    render(
      <WizardErrorBoundary stepName="Test Step">
        <GoodComponent />
      </WizardErrorBoundary>
    );

    expect(screen.getByText('Good component content')).toBeInTheDocument();
  });

  it('displays error UI when child throws', () => {
    render(
      <WizardErrorBoundary stepName="Test Step">
        <ThrowingComponent />
      </WizardErrorBoundary>
    );

    expect(screen.getByText(/Error in Test Step/i)).toBeInTheDocument();
    expect(screen.getByText(/Test error message/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Try Again/i })).toBeInTheDocument();
  });

  it('calls onError callback when error occurs', () => {
    const onError = vi.fn();

    render(
      <WizardErrorBoundary stepName="Test Step" onError={onError}>
        <ThrowingComponent />
      </WizardErrorBoundary>
    );

    expect(onError).toHaveBeenCalled();
    expect(onError.mock.calls[0][0]).toBeInstanceOf(Error);
    expect(onError.mock.calls[0][0].message).toBe('Test error message');
  });

  it('allows retry after error', async () => {
    const onRetry = vi.fn();
    let shouldThrow = true;

    const ConditionalThrow = () => {
      if (shouldThrow) {
        throw new Error('Test error');
      }
      return <div>Component recovered</div>;
    };

    render(
      <WizardErrorBoundary stepName="Test Step" onRetry={onRetry}>
        <ConditionalThrow />
      </WizardErrorBoundary>
    );

    // Should show error UI
    expect(screen.getByText(/Error in Test Step/i)).toBeInTheDocument();

    // Click retry
    shouldThrow = false;
    fireEvent.click(screen.getByRole('button', { name: /Try Again/i }));

    await waitFor(() => {
      expect(onRetry).toHaveBeenCalled();
    });
  });

  it('shows Skip with Defaults button when graceful degradation is enabled', () => {
    render(
      <WizardErrorBoundary
        stepName="Test Step"
        enableGracefulDegradation={true}
        onSkipWithDefaults={() => {}}
      >
        <ThrowingComponent />
      </WizardErrorBoundary>
    );

    expect(screen.getByRole('button', { name: /Skip with Defaults/i })).toBeInTheDocument();
  });

  it('does not show Skip with Defaults button when graceful degradation is disabled', () => {
    render(
      <WizardErrorBoundary stepName="Test Step" enableGracefulDegradation={false}>
        <ThrowingComponent />
      </WizardErrorBoundary>
    );

    expect(screen.queryByRole('button', { name: /Skip with Defaults/i })).not.toBeInTheDocument();
  });

  it('calls onSkipWithDefaults when Skip with Defaults is clicked', () => {
    const onSkipWithDefaults = vi.fn();

    render(
      <WizardErrorBoundary
        stepName="Test Step"
        enableGracefulDegradation={true}
        onSkipWithDefaults={onSkipWithDefaults}
      >
        <ThrowingComponent />
      </WizardErrorBoundary>
    );

    fireEvent.click(screen.getByRole('button', { name: /Skip with Defaults/i }));

    expect(onSkipWithDefaults).toHaveBeenCalled();
  });

  it('uses custom fallback when provided', () => {
    const customFallback = vi.fn().mockReturnValue(<div>Custom fallback content</div>);

    render(
      <WizardErrorBoundary stepName="Test Step" fallback={customFallback}>
        <ThrowingComponent />
      </WizardErrorBoundary>
    );

    expect(screen.getByText('Custom fallback content')).toBeInTheDocument();
    expect(customFallback).toHaveBeenCalled();
  });

  it('tracks retry count', () => {
    let throwCount = 0;
    const AlwaysThrows = () => {
      throwCount++;
      throw new Error(`Error #${throwCount}`);
    };

    render(
      <WizardErrorBoundary stepName="Test Step">
        <AlwaysThrows />
      </WizardErrorBoundary>
    );

    // First error
    expect(screen.getByText(/Error in Test Step/i)).toBeInTheDocument();

    // Click retry - component will throw again
    fireEvent.click(screen.getByRole('button', { name: /Try Again/i }));

    // Should show retry count in button
    expect(screen.getByText(/1\/3/i)).toBeInTheDocument();
  });

  it('shows warning banner after multiple retries', async () => {
    let throwCount = 0;
    const AlwaysThrows = () => {
      throwCount++;
      throw new Error(`Error #${throwCount}`);
    };

    render(
      <WizardErrorBoundary stepName="Test Step">
        <AlwaysThrows />
      </WizardErrorBoundary>
    );

    // Click retry 3 times
    for (let i = 0; i < 3; i++) {
      fireEvent.click(screen.getByRole('button', { name: /Try Again/i }));
    }

    await waitFor(() => {
      expect(screen.getByText(/Multiple retry attempts failed/i)).toBeInTheDocument();
    });
  });
});
