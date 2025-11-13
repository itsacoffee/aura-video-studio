/**
 * Error Toast Tests
 */

import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ReactElement } from 'react';
import { describe, it, expect, vi } from 'vitest';
import { ErrorToast, ErrorToastWithDetails } from '../ErrorToast';

const renderWithProvider = (component: ReactElement) => {
  return render(<FluentProvider theme={webLightTheme}>{component}</FluentProvider>);
};

describe('ErrorToast', () => {
  it('should render error toast with title and message', () => {
    renderWithProvider(<ErrorToast title="Test Error" message="Error message" severity="error" />);

    expect(screen.getByText('Test Error')).toBeInTheDocument();
    expect(screen.getByText('Error message')).toBeInTheDocument();
  });

  it('should display retry button when canRetry is true', () => {
    const onRetry = vi.fn();

    renderWithProvider(
      <ErrorToast
        title="Test Error"
        message="Error message"
        severity="error"
        canRetry={true}
        onRetry={onRetry}
      />
    );

    expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument();
  });

  it('should not display retry button when canRetry is false', () => {
    renderWithProvider(
      <ErrorToast title="Test Error" message="Error message" severity="error" canRetry={false} />
    );

    expect(screen.queryByRole('button', { name: /retry/i })).not.toBeInTheDocument();
  });

  it('should call onRetry when retry button is clicked', async () => {
    const user = userEvent.setup();
    const onRetry = vi.fn();

    renderWithProvider(
      <ErrorToast
        title="Test Error"
        message="Error message"
        severity="error"
        canRetry={true}
        onRetry={onRetry}
      />
    );

    const retryButton = screen.getByRole('button', { name: /retry/i });
    await user.click(retryButton);

    expect(onRetry).toHaveBeenCalledTimes(1);
  });

  it('should display correlation ID when provided', () => {
    renderWithProvider(
      <ErrorToast
        title="Test Error"
        message="Error message"
        severity="error"
        correlationId="test-123"
      />
    );

    expect(screen.getByText(/ID: test-123/i)).toBeInTheDocument();
  });

  it('should render custom actions', () => {
    const action1 = vi.fn();
    const action2 = vi.fn();

    renderWithProvider(
      <ErrorToast
        title="Test Error"
        message="Error message"
        severity="error"
        actions={[
          { label: 'Action 1', handler: action1 },
          { label: 'Action 2', handler: action2 },
        ]}
      />
    );

    expect(screen.getByRole('button', { name: 'Action 1' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Action 2' })).toBeInTheDocument();
  });

  it('should call action handlers when clicked', async () => {
    const user = userEvent.setup();
    const action1 = vi.fn();

    renderWithProvider(
      <ErrorToast
        title="Test Error"
        message="Error message"
        severity="error"
        actions={[{ label: 'Test Action', handler: action1 }]}
      />
    );

    const actionButton = screen.getByRole('button', { name: 'Test Action' });
    await user.click(actionButton);

    expect(action1).toHaveBeenCalledTimes(1);
  });

  it('should display dismiss button when onDismiss is provided', () => {
    const onDismiss = vi.fn();

    renderWithProvider(
      <ErrorToast
        title="Test Error"
        message="Error message"
        severity="error"
        onDismiss={onDismiss}
      />
    );

    const dismissButton = screen.getByRole('button', { name: '' });
    expect(dismissButton).toBeInTheDocument();
  });

  it('should call onDismiss when dismiss button is clicked', async () => {
    const user = userEvent.setup();
    const onDismiss = vi.fn();

    renderWithProvider(
      <ErrorToast
        title="Test Error"
        message="Error message"
        severity="error"
        onDismiss={onDismiss}
      />
    );

    const dismissButton = screen.getByRole('button', { name: '' });
    await user.click(dismissButton);

    expect(onDismiss).toHaveBeenCalledTimes(1);
  });
});

describe('ErrorToastWithDetails', () => {
  it('should add view details button when showDetailsLink is true', () => {
    const onShowDetails = vi.fn();

    renderWithProvider(
      <ErrorToastWithDetails
        title="Test Error"
        message="Error message"
        severity="error"
        showDetailsLink={true}
        onShowDetails={onShowDetails}
      />
    );

    expect(screen.getByRole('button', { name: /view details/i })).toBeInTheDocument();
  });

  it('should not add view details button when showDetailsLink is false', () => {
    renderWithProvider(
      <ErrorToastWithDetails
        title="Test Error"
        message="Error message"
        severity="error"
        showDetailsLink={false}
      />
    );

    expect(screen.queryByRole('button', { name: /view details/i })).not.toBeInTheDocument();
  });

  it('should call onShowDetails when details button is clicked', async () => {
    const user = userEvent.setup();
    const onShowDetails = vi.fn();

    renderWithProvider(
      <ErrorToastWithDetails
        title="Test Error"
        message="Error message"
        severity="error"
        showDetailsLink={true}
        onShowDetails={onShowDetails}
      />
    );

    const detailsButton = screen.getByRole('button', { name: /view details/i });
    await user.click(detailsButton);

    expect(onShowDetails).toHaveBeenCalledTimes(1);
  });
});
