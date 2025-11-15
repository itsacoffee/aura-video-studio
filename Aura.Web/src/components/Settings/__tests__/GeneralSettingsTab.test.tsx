import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { describe, expect, vi, it, beforeEach } from 'vitest';
import { setupApi } from '../../../services/api/setupApi';
import type { GeneralSettings } from '../../../types/settings';
import { GeneralSettingsTab } from '../GeneralSettingsTab';

vi.mock('../../../services/api/setupApi', () => ({
  setupApi: {
    resetWizard: vi.fn(),
  },
}));

vi.mock('../../../services/firstRunService', () => ({
  resetFirstRunStatus: vi.fn(),
}));

const mockedResetWizard = vi.mocked(setupApi.resetWizard);

const defaultSettings: GeneralSettings = {
  theme: 'auto',
  startupBehavior: 'resume',
  checkUpdates: true,
  telemetry: false,
  language: 'en',
};

describe('GeneralSettingsTab - Reset Wizard', () => {
  const defaultProps = {
    settings: defaultSettings,
    onChange: vi.fn(),
    onSave: vi.fn(),
    hasChanges: false,
  };

  beforeEach(() => {
    mockedResetWizard.mockReset();
    vi.clearAllMocks();
  });

  it('renders reset wizard progress button', () => {
    render(
      <BrowserRouter>
        <GeneralSettingsTab {...defaultProps} />
      </BrowserRouter>
    );

    expect(screen.getByText('Reset Wizard Progress')).toBeInTheDocument();
  });

  it('shows confirmation dialog and calls API on confirmation', async () => {
    const confirmSpy = vi.spyOn(window, 'confirm');
    confirmSpy.mockReturnValue(true);

    mockedResetWizard.mockResolvedValue({
      success: true,
      message: 'Wizard reset successfully',
      correlationId: 'test-123',
    });

    const alertSpy = vi.spyOn(window, 'alert');
    alertSpy.mockImplementation(() => {});

    render(
      <BrowserRouter>
        <GeneralSettingsTab {...defaultProps} />
      </BrowserRouter>
    );

    const button = screen.getByText('Reset Wizard Progress');
    fireEvent.click(button);

    expect(confirmSpy).toHaveBeenCalledWith(
      expect.stringContaining('This will clear your saved wizard progress')
    );

    await waitFor(() => {
      expect(mockedResetWizard).toHaveBeenCalledWith({
        preserveData: true,
      });
    });

    await waitFor(() => {
      expect(alertSpy).toHaveBeenCalledWith('Wizard progress has been reset successfully.');
    });

    confirmSpy.mockRestore();
    alertSpy.mockRestore();
  });

  it('does not call API if user cancels confirmation', () => {
    const confirmSpy = vi.spyOn(window, 'confirm');
    confirmSpy.mockReturnValue(false);

    render(
      <BrowserRouter>
        <GeneralSettingsTab {...defaultProps} />
      </BrowserRouter>
    );

    const button = screen.getByText('Reset Wizard Progress');
    fireEvent.click(button);

    expect(confirmSpy).toHaveBeenCalled();
    expect(mockedResetWizard).not.toHaveBeenCalled();

    confirmSpy.mockRestore();
  });

  it('shows error message when API call fails', async () => {
    const confirmSpy = vi.spyOn(window, 'confirm');
    confirmSpy.mockReturnValue(true);

    mockedResetWizard.mockRejectedValue(new Error('Network error'));

    const alertSpy = vi.spyOn(window, 'alert');
    alertSpy.mockImplementation(() => {});

    render(
      <BrowserRouter>
        <GeneralSettingsTab {...defaultProps} />
      </BrowserRouter>
    );

    const button = screen.getByText('Reset Wizard Progress');
    fireEvent.click(button);

    await waitFor(() => {
      expect(alertSpy).toHaveBeenCalledWith(
        expect.stringContaining('Failed to reset wizard progress')
      );
    });

    confirmSpy.mockRestore();
    alertSpy.mockRestore();
  });

  it('disables button and shows loading state during reset', async () => {
    const confirmSpy = vi.spyOn(window, 'confirm');
    confirmSpy.mockReturnValue(true);

    let resolvePromise: (value: { success: boolean; message: string }) => void;
    const promise = new Promise<{ success: boolean; message: string }>((resolve) => {
      resolvePromise = resolve;
    });
    mockedResetWizard.mockReturnValue(promise);

    render(
      <BrowserRouter>
        <GeneralSettingsTab {...defaultProps} />
      </BrowserRouter>
    );

    const button = screen.getByText('Reset Wizard Progress');
    fireEvent.click(button);

    await waitFor(() => {
      expect(screen.getByText('Resetting...')).toBeInTheDocument();
      expect(screen.getByText('Resetting...')).toBeDisabled();
    });

    resolvePromise!({ success: true, message: 'Reset successful' });

    await waitFor(() => {
      expect(screen.getByText('Reset Wizard Progress')).toBeInTheDocument();
    });

    confirmSpy.mockRestore();
  });
});
