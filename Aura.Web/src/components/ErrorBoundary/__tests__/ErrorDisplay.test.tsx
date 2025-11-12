import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import {
  ErrorDisplay,
  createNetworkErrorDisplay,
  createAuthErrorDisplay,
  createValidationErrorDisplay,
} from '../ErrorDisplay';

describe('ErrorDisplay', () => {
  it('should render error title and message', () => {
    render(<ErrorDisplay title="Test Error" message="This is a test error message" type="error" />);

    expect(screen.getByText('Test Error')).toBeInTheDocument();
    expect(screen.getByText('This is a test error message')).toBeInTheDocument();
  });

  it('should render suggestions list when provided', () => {
    render(
      <ErrorDisplay
        title="Test Error"
        message="Error occurred"
        suggestions={['Try this', 'Or try that', 'Check this setting']}
      />
    );

    expect(screen.getByText('What you can do:')).toBeInTheDocument();
    expect(screen.getByText('Try this')).toBeInTheDocument();
    expect(screen.getByText('Or try that')).toBeInTheDocument();
    expect(screen.getByText('Check this setting')).toBeInTheDocument();
  });

  it('should show retry button when showRetry is true', () => {
    render(
      <ErrorDisplay
        title="Test Error"
        message="Error occurred"
        showRetry={true}
        onRetry={vi.fn()}
      />
    );

    expect(screen.getByRole('button', { name: /Try Again/i })).toBeInTheDocument();
  });

  it('should not show retry button when showRetry is false', () => {
    render(<ErrorDisplay title="Test Error" message="Error occurred" showRetry={false} />);

    expect(screen.queryByRole('button', { name: /Try Again/i })).not.toBeInTheDocument();
  });

  it('should call onRetry when retry button is clicked', async () => {
    const onRetry = vi.fn();
    const user = userEvent.setup();

    render(
      <ErrorDisplay
        title="Test Error"
        message="Error occurred"
        showRetry={true}
        onRetry={onRetry}
      />
    );

    const retryButton = screen.getByRole('button', { name: /Try Again/i });
    await user.click(retryButton);

    expect(onRetry).toHaveBeenCalledTimes(1);
  });

  it('should show dismiss button when onDismiss is provided', () => {
    render(<ErrorDisplay title="Test Error" message="Error occurred" onDismiss={vi.fn()} />);

    expect(screen.getByRole('button', { name: /Dismiss/i })).toBeInTheDocument();
  });

  it('should call onDismiss when dismiss button is clicked', async () => {
    const onDismiss = vi.fn();
    const user = userEvent.setup();

    render(<ErrorDisplay title="Test Error" message="Error occurred" onDismiss={onDismiss} />);

    const dismissButton = screen.getByRole('button', { name: /Dismiss/i });
    await user.click(dismissButton);

    expect(onDismiss).toHaveBeenCalledTimes(1);
  });

  it('should render custom retry and dismiss labels', () => {
    render(
      <ErrorDisplay
        title="Test Error"
        message="Error occurred"
        showRetry={true}
        onRetry={vi.fn()}
        onDismiss={vi.fn()}
        retryLabel="Retry Operation"
        dismissLabel="Close"
      />
    );

    expect(screen.getByRole('button', { name: 'Retry Operation' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Close' })).toBeInTheDocument();
  });

  it('should render warning type', () => {
    render(<ErrorDisplay title="Warning" message="This is a warning" type="warning" />);

    expect(screen.getByText('Warning')).toBeInTheDocument();
    expect(screen.getByText('This is a warning')).toBeInTheDocument();
  });

  it('should render info type', () => {
    render(<ErrorDisplay title="Information" message="This is information" type="info" />);

    expect(screen.getByText('Information')).toBeInTheDocument();
    expect(screen.getByText('This is information')).toBeInTheDocument();
  });
});

describe('Error Display Helpers', () => {
  it('createNetworkErrorDisplay should create network error props', () => {
    const onRetry = vi.fn();
    const props = createNetworkErrorDisplay(onRetry);

    expect(props.title).toBe('Network Connection Lost');
    expect(props.type).toBe('error');
    expect(props.showRetry).toBe(true);
    expect(props.onRetry).toBe(onRetry);
    expect(props.suggestions).toContain('Check your internet connection');
  });

  it('createAuthErrorDisplay should create auth error props', () => {
    const onRetry = vi.fn();
    const props = createAuthErrorDisplay(onRetry);

    expect(props.title).toBe('Authentication Failed');
    expect(props.type).toBe('error');
    expect(props.showRetry).toBe(true);
    expect(props.suggestions).toContain('Check your API keys in Settings');
  });

  it('createValidationErrorDisplay should create validation error props', () => {
    const errors = ['Field 1 is required', 'Field 2 is invalid'];
    const props = createValidationErrorDisplay(errors);

    expect(props.title).toBe('Validation Error');
    expect(props.type).toBe('warning');
    expect(props.showRetry).toBe(false);
    expect(props.suggestions).toEqual(errors);
  });
});
