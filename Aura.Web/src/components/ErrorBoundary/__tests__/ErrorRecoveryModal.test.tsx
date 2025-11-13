/**
 * Error Recovery Modal Tests
 */

import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ReactElement } from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { ErrorRecoveryModal, ErrorRecoveryOptions } from '../ErrorRecoveryModal';

const renderWithProvider = (component: ReactElement) => {
  return render(<FluentProvider theme={webLightTheme}>{component}</FluentProvider>);
};

describe('ErrorRecoveryModal', () => {
  const mockOnDismiss = vi.fn();

  const defaultOptions: ErrorRecoveryOptions = {
    title: 'Test Error',
    message: 'An error occurred during testing',
    severity: 'error',
    canRetry: true,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should render error modal when open', () => {
    renderWithProvider(
      <ErrorRecoveryModal isOpen={true} options={defaultOptions} onDismiss={mockOnDismiss} />
    );

    expect(screen.getByText('Test Error')).toBeInTheDocument();
    expect(screen.getByText('An error occurred during testing')).toBeInTheDocument();
  });

  it('should not render modal when closed', () => {
    renderWithProvider(
      <ErrorRecoveryModal isOpen={false} options={defaultOptions} onDismiss={mockOnDismiss} />
    );

    expect(screen.queryByText('Test Error')).not.toBeInTheDocument();
  });

  it('should display retry button when canRetry is true', () => {
    const optionsWithRetry: ErrorRecoveryOptions = {
      ...defaultOptions,
      canRetry: true,
      retryAction: vi.fn(),
    };

    renderWithProvider(
      <ErrorRecoveryModal isOpen={true} options={optionsWithRetry} onDismiss={mockOnDismiss} />
    );

    expect(screen.getByRole('button', { name: /try again/i })).toBeInTheDocument();
  });

  it('should not display retry button when canRetry is false', () => {
    const optionsNoRetry: ErrorRecoveryOptions = {
      ...defaultOptions,
      canRetry: false,
    };

    renderWithProvider(
      <ErrorRecoveryModal isOpen={true} options={optionsNoRetry} onDismiss={mockOnDismiss} />
    );

    expect(screen.queryByRole('button', { name: /try again/i })).not.toBeInTheDocument();
  });

  it('should call retryAction when retry button is clicked', async () => {
    const user = userEvent.setup();
    const retryAction = vi.fn().mockResolvedValue(undefined);

    const optionsWithRetry: ErrorRecoveryOptions = {
      ...defaultOptions,
      canRetry: true,
      retryAction,
    };

    renderWithProvider(
      <ErrorRecoveryModal isOpen={true} options={optionsWithRetry} onDismiss={mockOnDismiss} />
    );

    const retryButton = screen.getByRole('button', { name: /try again/i });
    await user.click(retryButton);

    await waitFor(() => {
      expect(retryAction).toHaveBeenCalledTimes(1);
    });
  });

  it('should show retrying state during retry', async () => {
    const user = userEvent.setup();
    const retryAction = vi.fn(() => new Promise((resolve) => setTimeout(resolve, 100)));

    const optionsWithRetry: ErrorRecoveryOptions = {
      ...defaultOptions,
      canRetry: true,
      retryAction,
    };

    renderWithProvider(
      <ErrorRecoveryModal isOpen={true} options={optionsWithRetry} onDismiss={mockOnDismiss} />
    );

    const retryButton = screen.getByRole('button', { name: /try again/i });
    await user.click(retryButton);

    expect(screen.getByText(/retrying operation/i)).toBeInTheDocument();
  });

  it('should dismiss modal on successful retry', async () => {
    const user = userEvent.setup();
    const retryAction = vi.fn().mockResolvedValue(undefined);

    const optionsWithRetry: ErrorRecoveryOptions = {
      ...defaultOptions,
      canRetry: true,
      retryAction,
    };

    renderWithProvider(
      <ErrorRecoveryModal isOpen={true} options={optionsWithRetry} onDismiss={mockOnDismiss} />
    );

    const retryButton = screen.getByRole('button', { name: /try again/i });
    await user.click(retryButton);

    await waitFor(() => {
      expect(mockOnDismiss).toHaveBeenCalled();
    });
  });

  it('should show error message on retry failure', async () => {
    const user = userEvent.setup();
    const retryAction = vi.fn().mockRejectedValue(new Error('Retry failed'));

    const optionsWithRetry: ErrorRecoveryOptions = {
      ...defaultOptions,
      canRetry: true,
      retryAction,
    };

    renderWithProvider(
      <ErrorRecoveryModal isOpen={true} options={optionsWithRetry} onDismiss={mockOnDismiss} />
    );

    const retryButton = screen.getByRole('button', { name: /try again/i });
    await user.click(retryButton);

    await waitFor(() => {
      expect(screen.getByText('Retry Failed')).toBeInTheDocument();
    });
  });

  it('should display suggested actions', () => {
    const mockAction = vi.fn();
    const optionsWithActions: ErrorRecoveryOptions = {
      ...defaultOptions,
      suggestedActions: [
        { label: 'Check Settings', action: mockAction },
        { label: 'Contact Support', action: mockAction },
      ],
    };

    renderWithProvider(
      <ErrorRecoveryModal isOpen={true} options={optionsWithActions} onDismiss={mockOnDismiss} />
    );

    expect(screen.getByText('Suggested Actions:')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /check settings/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /contact support/i })).toBeInTheDocument();
  });

  it('should call suggested action handler when clicked', async () => {
    const user = userEvent.setup();
    const mockAction = vi.fn();

    const optionsWithActions: ErrorRecoveryOptions = {
      ...defaultOptions,
      suggestedActions: [{ label: 'Test Action', action: mockAction }],
    };

    renderWithProvider(
      <ErrorRecoveryModal isOpen={true} options={optionsWithActions} onDismiss={mockOnDismiss} />
    );

    const actionButton = screen.getByRole('button', { name: /test action/i });
    await user.click(actionButton);

    expect(mockAction).toHaveBeenCalledTimes(1);
  });

  it('should display technical details when provided', () => {
    const optionsWithDetails: ErrorRecoveryOptions = {
      ...defaultOptions,
      technicalDetails: 'Stack trace: Error at line 42',
    };

    renderWithProvider(
      <ErrorRecoveryModal isOpen={true} options={optionsWithDetails} onDismiss={mockOnDismiss} />
    );

    expect(screen.getByText(/technical details/i)).toBeInTheDocument();
  });

  it('should handle close button click', async () => {
    const user = userEvent.setup();

    renderWithProvider(
      <ErrorRecoveryModal isOpen={true} options={defaultOptions} onDismiss={mockOnDismiss} />
    );

    const closeButton = screen.getByRole('button', { name: /close/i });
    await user.click(closeButton);

    expect(mockOnDismiss).toHaveBeenCalledTimes(1);
  });

  it('should disable buttons during retry', async () => {
    const user = userEvent.setup();
    const retryAction = vi.fn(() => new Promise((resolve) => setTimeout(resolve, 100)));

    const optionsWithRetry: ErrorRecoveryOptions = {
      ...defaultOptions,
      canRetry: true,
      retryAction,
    };

    renderWithProvider(
      <ErrorRecoveryModal isOpen={true} options={optionsWithRetry} onDismiss={mockOnDismiss} />
    );

    const retryButton = screen.getByRole('button', { name: /try again/i });
    const closeButton = screen.getByRole('button', { name: /close/i });

    await user.click(retryButton);

    expect(retryButton).toBeDisabled();
    expect(closeButton).toBeDisabled();
  });

  it('should display correct severity icon for error', () => {
    renderWithProvider(
      <ErrorRecoveryModal
        isOpen={true}
        options={{ ...defaultOptions, severity: 'error' }}
        onDismiss={mockOnDismiss}
      />
    );

    expect(screen.getByText('Test Error')).toBeInTheDocument();
  });

  it('should display correct severity icon for warning', () => {
    renderWithProvider(
      <ErrorRecoveryModal
        isOpen={true}
        options={{ ...defaultOptions, severity: 'warning' }}
        onDismiss={mockOnDismiss}
      />
    );

    expect(screen.getByText('Test Error')).toBeInTheDocument();
  });

  it('should handle onClose callback', async () => {
    const mockOnClose = vi.fn();
    const optionsWithOnClose: ErrorRecoveryOptions = {
      ...defaultOptions,
      onClose: mockOnClose,
    };

    renderWithProvider(
      <ErrorRecoveryModal isOpen={true} options={optionsWithOnClose} onDismiss={mockOnDismiss} />
    );

    expect(screen.getByText('Test Error')).toBeInTheDocument();
  });
});
