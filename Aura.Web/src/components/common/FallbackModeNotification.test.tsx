/**
 * Tests for FallbackModeNotification component
 */

import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { FallbackModeNotification } from './FallbackModeNotification';

// Mock useNavigate
const mockNavigate = vi.fn();
vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router-dom')>();
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

describe('FallbackModeNotification', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    sessionStorage.clear();
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  const renderComponent = (props = {}) => {
    const defaultProps = {
      isVisible: true,
      providerUsed: 'RuleBased',
      fallbackReason: 'Ollama not available',
    };
    return render(
      <MemoryRouter>
        <FallbackModeNotification {...defaultProps} {...props} />
      </MemoryRouter>
    );
  };

  it('should render when isVisible is true', () => {
    renderComponent();
    expect(screen.getByRole('alert')).toBeInTheDocument();
    expect(screen.getByText(/Running in Offline Mode/i)).toBeInTheDocument();
  });

  it('should not render when isVisible is false', () => {
    renderComponent({ isVisible: false });
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('should display provider name', () => {
    renderComponent({ providerUsed: 'RuleBased' });
    expect(screen.getByText(/RuleBased/)).toBeInTheDocument();
  });

  it('should display fallback reason', () => {
    const fallbackReason = 'Primary AI provider not available';
    renderComponent({ fallbackReason });
    expect(screen.getByText(new RegExp(fallbackReason))).toBeInTheDocument();
  });

  it('should navigate to settings when Configure Ollama button is clicked', () => {
    renderComponent();
    const configureButton = screen.getByRole('button', { name: /Configure Ollama/i });
    fireEvent.click(configureButton);
    expect(mockNavigate).toHaveBeenCalledWith('/settings?tab=ai-providers');
  });

  it('should dismiss notification when dismiss button is clicked', async () => {
    const onDismiss = vi.fn();
    renderComponent({ onDismiss });

    const dismissButton = screen.getByRole('button', { name: /Dismiss notification/i });
    fireEvent.click(dismissButton);

    await waitFor(() => {
      expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    });
    expect(onDismiss).toHaveBeenCalled();
  });

  it('should persist dismissal in session storage when persistDismissal is true', () => {
    const storageKey = 'test-fallback-dismissed';
    renderComponent({ persistDismissal: true, storageKey });

    const dismissButton = screen.getByRole('button', { name: /Dismiss notification/i });
    fireEvent.click(dismissButton);

    expect(sessionStorage.getItem(storageKey)).toBe('true');
  });

  it('should not show notification if previously dismissed in session', () => {
    const storageKey = 'test-fallback-dismissed';
    sessionStorage.setItem(storageKey, 'true');

    renderComponent({ persistDismissal: true, storageKey });
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('should contain link to download Ollama', () => {
    renderComponent();
    const link = screen.getByRole('link', { name: /Download Ollama/i });
    expect(link).toHaveAttribute('href', 'https://ollama.com');
    expect(link).toHaveAttribute('target', '_blank');
  });
});
