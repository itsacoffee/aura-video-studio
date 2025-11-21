import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import * as HealthCheckServiceModule from '../../services/HealthCheckService';
import { SetupWizard } from '../SetupWizard';

vi.mock('../../services/HealthCheckService', () => ({
  healthCheckService: {
    checkHealth: vi.fn(),
  },
}));

describe('SetupWizard', () => {
  const mockCheckHealth = vi.mocked(HealthCheckServiceModule.healthCheckService.checkHealth);

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should show loading state while checking backend health', () => {
    mockCheckHealth.mockImplementation(
      () => new Promise(() => {}) // Never resolves
    );

    render(<SetupWizard />);

    expect(screen.getByText(/Connecting to Aura backend/i)).toBeInTheDocument();
    expect(screen.getByText(/Attempt 0 of 10/i)).toBeInTheDocument();
  });

  it('should render children when backend is healthy', async () => {
    mockCheckHealth.mockResolvedValue({
      isHealthy: true,
      message: 'Backend is healthy',
      statusCode: 200,
      latencyMs: 50,
      timestamp: new Date(),
    });

    render(
      <SetupWizard>
        <div>Setup Content</div>
      </SetupWizard>
    );

    await waitFor(() => {
      expect(screen.getByText('Setup Content')).toBeInTheDocument();
    });
  });

  it('should call onBackendReady when backend becomes healthy', async () => {
    const onBackendReady = vi.fn();

    mockCheckHealth.mockResolvedValue({
      isHealthy: true,
      message: 'Backend is healthy',
      statusCode: 200,
      latencyMs: 50,
      timestamp: new Date(),
    });

    render(<SetupWizard onBackendReady={onBackendReady} />);

    await waitFor(() => {
      expect(onBackendReady).toHaveBeenCalledTimes(1);
    });
  });

  it('should show error message when backend is unhealthy', async () => {
    mockCheckHealth.mockResolvedValue({
      isHealthy: false,
      message: 'Connection refused',
      timestamp: new Date(),
    });

    render(<SetupWizard />);

    await waitFor(() => {
      expect(screen.getByText(/Backend Server Not Reachable/i)).toBeInTheDocument();
      expect(screen.getByText(/Connection refused/i)).toBeInTheDocument();
    });
  });

  it('should show troubleshooting steps when backend is unhealthy', async () => {
    mockCheckHealth.mockResolvedValue({
      isHealthy: false,
      message: 'Connection refused',
      timestamp: new Date(),
    });

    render(<SetupWizard />);

    await waitFor(() => {
      expect(screen.getByText(/Troubleshooting Steps:/i)).toBeInTheDocument();
      expect(screen.getByText(/Windows Firewall/i)).toBeInTheDocument();
      expect(screen.getByText(/port 5000/i)).toBeInTheDocument();
    });
  });

  it('should retry health check when retry button is clicked', async () => {
    const user = userEvent.setup();

    mockCheckHealth.mockResolvedValue({
      isHealthy: false,
      message: 'Connection refused',
      timestamp: new Date(),
    });

    render(<SetupWizard />);

    await waitFor(() => {
      expect(screen.getByText(/Retry Connection/i)).toBeInTheDocument();
    });

    const retryButton = screen.getByRole('button', { name: /Retry Connection \(Attempt 1\)/i });
    await user.click(retryButton);

    await waitFor(() => {
      expect(mockCheckHealth).toHaveBeenCalledTimes(2);
    });
  });

  it('should increment retry count on each retry', async () => {
    const user = userEvent.setup();

    mockCheckHealth.mockResolvedValue({
      isHealthy: false,
      message: 'Connection refused',
      timestamp: new Date(),
    });

    render(<SetupWizard />);

    await waitFor(() => {
      expect(
        screen.getByRole('button', { name: /Retry Connection \(Attempt 1\)/i })
      ).toBeInTheDocument();
    });

    const retryButton = screen.getByRole('button', { name: /Retry Connection \(Attempt 1\)/i });
    await user.click(retryButton);

    await waitFor(() => {
      expect(
        screen.getByRole('button', { name: /Retry Connection \(Attempt 2\)/i })
      ).toBeInTheDocument();
    });
  });

  it('should update progress during health check', async () => {
    let progressCallback: ((attempt: number, maxAttempts: number) => void) | undefined;

    mockCheckHealth.mockImplementation((onProgress) => {
      progressCallback = onProgress;
      return new Promise((resolve) => {
        setTimeout(() => {
          if (progressCallback) {
            progressCallback(1, 10);
            progressCallback(2, 10);
            progressCallback(3, 10);
          }
          resolve({
            isHealthy: true,
            message: 'Backend is healthy',
            statusCode: 200,
            timestamp: new Date(),
          });
        }, 100);
      });
    });

    render(<SetupWizard />);

    expect(screen.getByText(/Attempt 0 of 10/i)).toBeInTheDocument();

    await waitFor(
      () => {
        expect(screen.queryByText(/Connecting to Aura backend/i)).not.toBeInTheDocument();
      },
      { timeout: 500 }
    );
  });
});
