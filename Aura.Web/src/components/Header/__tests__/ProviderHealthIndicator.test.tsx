/**
 * Tests for ProviderHealthIndicator component
 */

import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { ProviderHealthIndicator } from '../ProviderHealthIndicator';

// Mock the useProviderStatus hook
const mockHealthSummary = {
  level: 'healthy' as const,
  availableLlm: 2,
  totalLlm: 2,
  availableTts: 3,
  totalTts: 3,
  availableImages: 1,
  totalImages: 1,
  message: 'All providers operational',
};

const mockUseProviderStatus = vi.fn();

vi.mock('../../../hooks/useProviderStatus', () => ({
  useProviderStatus: () => mockUseProviderStatus(),
}));

const renderWithProvider = (component: React.ReactElement) => {
  return render(
    <MemoryRouter>
      <FluentProvider theme={webLightTheme}>{component}</FluentProvider>
    </MemoryRouter>
  );
};

describe('ProviderHealthIndicator', () => {
  beforeEach(() => {
    mockUseProviderStatus.mockReturnValue({
      healthSummary: mockHealthSummary,
      isLoading: false,
      lastUpdated: new Date(),
      llmProviders: [],
      ttsProviders: [],
      imageProviders: [],
      error: null,
      refresh: vi.fn(),
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('renders with healthy status indicator', async () => {
    renderWithProvider(<ProviderHealthIndicator />);

    await waitFor(() => {
      const button = screen.getByRole('button');
      expect(button).toBeInTheDocument();
      expect(button).toHaveAttribute(
        'aria-label',
        expect.stringContaining('All providers healthy')
      );
    });
  });

  it('shows loading spinner when initially loading', async () => {
    mockUseProviderStatus.mockReturnValue({
      healthSummary: mockHealthSummary,
      isLoading: true,
      lastUpdated: null,
      llmProviders: [],
      ttsProviders: [],
      imageProviders: [],
      error: null,
      refresh: vi.fn(),
    });

    renderWithProvider(<ProviderHealthIndicator />);

    await waitFor(() => {
      const button = screen.getByRole('button');
      expect(button).toHaveAttribute('aria-label', 'Loading provider status...');
    });
  });

  it('shows degraded status when some providers unavailable', async () => {
    mockUseProviderStatus.mockReturnValue({
      healthSummary: {
        ...mockHealthSummary,
        level: 'degraded',
        availableLlm: 1,
        message: 'Some LLM providers unavailable',
      },
      isLoading: false,
      lastUpdated: new Date(),
      llmProviders: [],
      ttsProviders: [],
      imageProviders: [],
      error: null,
      refresh: vi.fn(),
    });

    renderWithProvider(<ProviderHealthIndicator />);

    await waitFor(() => {
      const button = screen.getByRole('button');
      expect(button).toHaveAttribute(
        'aria-label',
        expect.stringContaining('Some providers unavailable')
      );
    });
  });

  it('shows critical status when critical providers unavailable', async () => {
    mockUseProviderStatus.mockReturnValue({
      healthSummary: {
        ...mockHealthSummary,
        level: 'critical',
        availableLlm: 0,
        message: 'No script generation providers available',
      },
      isLoading: false,
      lastUpdated: new Date(),
      llmProviders: [],
      ttsProviders: [],
      imageProviders: [],
      error: null,
      refresh: vi.fn(),
    });

    renderWithProvider(<ProviderHealthIndicator />);

    await waitFor(() => {
      const button = screen.getByRole('button');
      expect(button).toHaveAttribute(
        'aria-label',
        expect.stringContaining('Critical providers unavailable')
      );
    });
  });

  it('opens drawer when clicked', async () => {
    renderWithProvider(<ProviderHealthIndicator />);

    // Find and click the button
    const button = await screen.findByRole('button');
    fireEvent.click(button);

    // Drawer opens with a title - check for "Provider Status" text
    await waitFor(() => {
      expect(screen.getByText('Provider Status')).toBeInTheDocument();
    });
  });

  it('uses custom poll interval', async () => {
    const customInterval = 60000;

    renderWithProvider(<ProviderHealthIndicator pollInterval={customInterval} />);

    await waitFor(() => {
      const button = screen.getByRole('button');
      expect(button).toBeInTheDocument();
    });
  });
});
