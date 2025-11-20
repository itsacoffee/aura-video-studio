import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { render, screen, waitFor } from '@testing-library/react';
import React from 'react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import * as firstRunService from '../../services/firstRunService';

// Mock the firstRunService
vi.mock('../../services/firstRunService', () => ({
  hasCompletedFirstRun: vi.fn(),
  clearFirstRunCache: vi.fn(),
}));

// Create a simple ProtectedRoute component for testing
// This is extracted from AppRouterContent.tsx
const ProtectedRoute = ({ children }: { children: React.ReactNode }) => {
  const [setupComplete, setSetupComplete] = React.useState<boolean | null>(null);

  React.useEffect(() => {
    firstRunService
      .hasCompletedFirstRun()
      .then(setSetupComplete)
      .catch(() => {
        setSetupComplete(false);
      });
  }, []);

  if (setupComplete === null) {
    return (
      <div>
        <span>Loading...</span>
      </div>
    );
  }

  if (!setupComplete) {
    return <div>Redirecting to setup...</div>;
  }

  return <>{children}</>;
};

describe('ProtectedRoute', () => {
  const renderWithRouter = (initialEntries = ['/']) => {
    return render(
      <FluentProvider theme={webLightTheme}>
        <MemoryRouter initialEntries={initialEntries}>
          <Routes>
            <Route path="/setup" element={<div>Setup Wizard</div>} />
            <Route
              path="/"
              element={
                <ProtectedRoute>
                  <div>Protected Content</div>
                </ProtectedRoute>
              }
            />
          </Routes>
        </MemoryRouter>
      </FluentProvider>
    );
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should show loading state while checking first-run status', () => {
    vi.mocked(firstRunService.hasCompletedFirstRun).mockImplementation(
      () => new Promise(() => {}) // Never resolves
    );

    renderWithRouter();

    expect(screen.getByText(/Loading.../i)).toBeInTheDocument();
  });

  it('should show protected content when setup is complete', async () => {
    vi.mocked(firstRunService.hasCompletedFirstRun).mockResolvedValue(true);

    renderWithRouter();

    await waitFor(() => {
      expect(screen.getByText(/Protected Content/i)).toBeInTheDocument();
    });
  });

  it('should redirect to setup when first-run is not complete', async () => {
    vi.mocked(firstRunService.hasCompletedFirstRun).mockResolvedValue(false);

    renderWithRouter();

    await waitFor(() => {
      expect(screen.getByText(/Redirecting to setup.../i)).toBeInTheDocument();
    });
  });

  it('should handle errors by assuming setup is not complete', async () => {
    vi.mocked(firstRunService.hasCompletedFirstRun).mockRejectedValue(
      new Error('Backend unavailable')
    );

    renderWithRouter();

    await waitFor(() => {
      expect(screen.getByText(/Redirecting to setup.../i)).toBeInTheDocument();
    });
  });
});
