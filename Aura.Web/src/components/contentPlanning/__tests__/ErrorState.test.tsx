import { render, screen, fireEvent } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { ErrorState } from '../ErrorState';

describe('ErrorState', () => {
  it('should render network error with default message', () => {
    render(<ErrorState errorType="network" />);

    expect(screen.getByText('Connection Failed')).toBeInTheDocument();
    expect(screen.getByText(/connect to the service/)).toBeInTheDocument();
  });

  it('should render auth error with default message', () => {
    render(<ErrorState errorType="auth" />);

    expect(screen.getByText('Authentication Failed')).toBeInTheDocument();
    expect(screen.getByText(/API key is invalid/)).toBeInTheDocument();
  });

  it('should render rate limit error', () => {
    render(<ErrorState errorType="rateLimit" />);

    expect(screen.getByText('Rate Limit Exceeded')).toBeInTheDocument();
    expect(screen.getByText(/Too many requests/)).toBeInTheDocument();
  });

  it('should render timeout error', () => {
    render(<ErrorState errorType="timeout" />);

    expect(screen.getByText('Request Timeout')).toBeInTheDocument();
    expect(screen.getByText(/took too long/)).toBeInTheDocument();
  });

  it('should render server error', () => {
    render(<ErrorState errorType="server" />);

    expect(screen.getByText('Service Unavailable')).toBeInTheDocument();
    expect(screen.getByText(/temporarily unavailable/)).toBeInTheDocument();
  });

  it('should render unknown error', () => {
    render(<ErrorState errorType="unknown" />);

    expect(screen.getByText('Error Occurred')).toBeInTheDocument();
    expect(screen.getByText(/unexpected error/)).toBeInTheDocument();
  });

  it('should render custom message when provided', () => {
    const customMessage = 'This is a custom error message';
    render(<ErrorState errorType="network" message={customMessage} />);

    expect(screen.getByText(customMessage)).toBeInTheDocument();
  });

  it('should render details when provided', () => {
    const details = 'Additional error details';
    render(<ErrorState errorType="network" details={details} />);

    expect(screen.getByText(details)).toBeInTheDocument();
  });

  it('should render retry button when onRetry provided', () => {
    const onRetry = vi.fn();
    render(<ErrorState errorType="network" onRetry={onRetry} />);

    const button = screen.getByRole('button', { name: /Try Again/i });
    expect(button).toBeInTheDocument();
  });

  it('should call onRetry when retry button clicked', () => {
    const onRetry = vi.fn();
    render(<ErrorState errorType="network" onRetry={onRetry} />);

    const button = screen.getByRole('button', { name: /Try Again/i });
    fireEvent.click(button);

    expect(onRetry).toHaveBeenCalledTimes(1);
  });

  it('should not render retry button when onRetry not provided', () => {
    render(<ErrorState errorType="network" />);

    const button = screen.queryByRole('button', { name: /Try Again/i });
    expect(button).not.toBeInTheDocument();
  });

  it('should disable retry button when retryDisabled is true', () => {
    const onRetry = vi.fn();
    render(<ErrorState errorType="network" onRetry={onRetry} retryDisabled={true} />);

    const button = screen.getByRole('button', { name: /Try Again/i });
    expect(button).toBeDisabled();
  });
});
