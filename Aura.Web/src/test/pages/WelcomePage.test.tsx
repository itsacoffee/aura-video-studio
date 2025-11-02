import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { WelcomePage } from '../../pages/WelcomePage';
import * as firstRunService from '../../services/firstRunService';

// Mock the firstRunService
vi.mock('../../services/firstRunService', () => ({
  hasCompletedFirstRun: vi.fn(),
  migrateLegacyFirstRunStatus: vi.fn(),
}));

// Mock the API URL
vi.mock('../../config/api', () => ({
  apiUrl: (path: string) => path,
}));

// Mock the child components that have complex dependencies
vi.mock('../../components/FirstRunDiagnostics', () => ({
  FirstRunDiagnostics: () => null,
}));

vi.mock('../../components/SystemCheckCard', () => ({
  SystemCheckCard: () => null,
}));

vi.mock('../../components/Tooltips', () => ({
  TooltipContent: {},
  TooltipWithLink: () => null,
}));

describe('WelcomePage - First-Run Callout', () => {
  const renderWithProviders = (component: React.ReactElement) => {
    return render(
      <FluentProvider theme={webLightTheme}>
        <BrowserRouter>{component}</BrowserRouter>
      </FluentProvider>
    );
  };

  beforeEach(() => {
    vi.clearAllMocks();
    // Mock fetch to prevent actual API calls and return proper data structure
    global.fetch = vi.fn((url) => {
      if (typeof url === 'string' && url.includes('/api/capabilities')) {
        return Promise.resolve({
          ok: true,
          json: () =>
            Promise.resolve({
              tier: 'B',
              cpu: { threads: 8 },
              ram: { gb: 16 },
              gpu: { model: 'NVIDIA RTX 3060', vramGB: 12 },
              enableNVENC: true,
              enableSD: true,
              offlineOnly: false,
            }),
        } as Response);
      }
      return Promise.resolve({
        ok: true,
        json: () => Promise.resolve({ status: 'healthy' }),
      } as Response);
    });
  });

  it('should show first-time setup callout when user has not completed first run', async () => {
    vi.mocked(firstRunService.hasCompletedFirstRun).mockResolvedValue(false);

    renderWithProviders(<WelcomePage />);

    await waitFor(() => {
      expect(screen.getByText(/Start Here: First-Time Setup/i)).toBeInTheDocument();
    });

    expect(screen.getByText(/Begin Setup Wizard/i)).toBeInTheDocument();
    expect(screen.getByText(/3-5 minute wizard/i)).toBeInTheDocument();
  });

  it('should hide first-time setup callout when user has completed first run', async () => {
    vi.mocked(firstRunService.hasCompletedFirstRun).mockResolvedValue(true);

    renderWithProviders(<WelcomePage />);

    await waitFor(() => {
      expect(screen.queryByText(/Start Here: First-Time Setup/i)).not.toBeInTheDocument();
    });

    expect(screen.queryByText(/Begin Setup Wizard/i)).not.toBeInTheDocument();
  });

  it('should show reconfigure callout with "Configure Setup" button when user has completed first run', async () => {
    vi.mocked(firstRunService.hasCompletedFirstRun).mockResolvedValue(true);

    renderWithProviders(<WelcomePage />);

    await waitFor(() => {
      expect(screen.getByText(/Need to Reconfigure Your Setup?/i)).toBeInTheDocument();
    });

    expect(screen.getByRole('button', { name: /Configure Setup/i })).toBeInTheDocument();
    expect(screen.getByText(/Update API keys, configure dependencies/i)).toBeInTheDocument();
  });

  it('should hide reconfigure callout when showing first-time callout', async () => {
    vi.mocked(firstRunService.hasCompletedFirstRun).mockResolvedValue(false);

    renderWithProviders(<WelcomePage />);

    await waitFor(() => {
      expect(screen.getByText(/Begin Setup Wizard/i)).toBeInTheDocument();
    });

    // The reconfigure callout should not be present
    expect(screen.queryByText(/Need to Reconfigure Your Setup?/i)).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Configure Setup/i })).not.toBeInTheDocument();
  });

  it('should always show Create Video and Settings buttons', async () => {
    vi.mocked(firstRunService.hasCompletedFirstRun).mockResolvedValue(false);

    renderWithProviders(<WelcomePage />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Create Video/i })).toBeInTheDocument();
    });

    expect(screen.getByRole('button', { name: /Settings/i })).toBeInTheDocument();
  });

  it('should handle error gracefully when checking first-run status fails', async () => {
    vi.mocked(firstRunService.hasCompletedFirstRun).mockRejectedValue(
      new Error('Failed to check status')
    );

    renderWithProviders(<WelcomePage />);

    await waitFor(() => {
      // Should not crash and should show the page
      expect(screen.getByText(/Welcome to Aura Video Studio/i)).toBeInTheDocument();
    });

    // Should not show the first-time callout on error (defaults to false)
    expect(screen.queryByText(/Start Here: First-Time Setup/i)).not.toBeInTheDocument();
  });
});
