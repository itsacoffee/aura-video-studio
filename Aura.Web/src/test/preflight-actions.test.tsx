import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { render, screen, fireEvent } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';
import { PreflightPanel } from '../components/PreflightPanel';
import type { PreflightReport } from '../state/providers';

// Helper to wrap component with required providers
const renderWithProviders = (component: React.ReactElement) => {
  return render(
    <BrowserRouter>
      <FluentProvider theme={webLightTheme}>{component}</FluentProvider>
    </BrowserRouter>
  );
};

describe('PreflightPanel - Actionable Fixes', () => {
  it('should render without report', () => {
    const mockRunPreflight = vi.fn();

    renderWithProviders(
      <PreflightPanel
        profile="Free-Only"
        report={null}
        isRunning={false}
        onRunPreflight={mockRunPreflight}
      />
    );

    expect(screen.getByText(/Run a preflight check/i)).toBeDefined();
  });

  it('should show fix actions for failed checks', () => {
    const mockRunPreflight = vi.fn();
    const report: PreflightReport = {
      ok: false,
      stages: [
        {
          stage: 'Script',
          status: 'fail',
          provider: 'OpenAI',
          message: 'API key not configured',
          hint: 'Configure your OpenAI API key in Settings',
          fixActions: [
            {
              type: 'OpenSettings',
              label: 'Add API Key',
              parameter: 'api-keys',
              description: 'Open Settings to configure OpenAI API key',
            },
          ],
        },
      ],
    };

    renderWithProviders(
      <PreflightPanel
        profile="Pro-Max"
        report={report}
        isRunning={false}
        onRunPreflight={mockRunPreflight}
      />
    );

    // Check that fix action button is rendered
    expect(screen.getByText('Add API Key')).toBeDefined();
  });

  it('should show safe defaults button when preflight fails', () => {
    const mockRunPreflight = vi.fn();
    const mockApplySafeDefaults = vi.fn();
    const report: PreflightReport = {
      ok: false,
      stages: [
        {
          stage: 'TTS',
          status: 'warn',
          provider: 'ElevenLabs',
          message: 'Not available',
          hint: 'API key missing',
          fixActions: [
            {
              type: 'SwitchToFree',
              label: 'Use Windows TTS',
              parameter: 'Windows',
              description: 'Switch to built-in Windows TTS instead',
            },
          ],
        },
      ],
    };

    renderWithProviders(
      <PreflightPanel
        profile="Pro-Max"
        report={report}
        isRunning={false}
        onRunPreflight={mockRunPreflight}
        onApplySafeDefaults={mockApplySafeDefaults}
      />
    );

    // Check that safe defaults button is rendered
    const safeDefaultsButton = screen.getByText(/Use Safe Defaults/i);
    expect(safeDefaultsButton).toBeDefined();

    // Click the button
    fireEvent.click(safeDefaultsButton);
    expect(mockApplySafeDefaults).toHaveBeenCalled();
  });

  it('should show success badge when all checks pass', () => {
    const mockRunPreflight = vi.fn();
    const report: PreflightReport = {
      ok: true,
      stages: [
        {
          stage: 'Script',
          status: 'pass',
          provider: 'RuleBased',
          message: 'Available',
        },
        {
          stage: 'TTS',
          status: 'pass',
          provider: 'Windows',
          message: 'Available',
        },
        {
          stage: 'Visuals',
          status: 'pass',
          provider: 'Stock',
          message: 'Available',
        },
      ],
    };

    renderWithProviders(
      <PreflightPanel
        profile="Free-Only"
        report={report}
        isRunning={false}
        onRunPreflight={mockRunPreflight}
      />
    );

    expect(screen.getByText(/All systems ready/i)).toBeDefined();
  });

  it('should handle multiple fix actions for a single check', () => {
    const mockRunPreflight = vi.fn();
    const report: PreflightReport = {
      ok: false,
      stages: [
        {
          stage: 'Visuals',
          status: 'fail',
          provider: 'StableDiffusion',
          message: 'Not running',
          hint: 'Start SD WebUI with --api flag',
          fixActions: [
            {
              type: 'Install',
              label: 'Download SD WebUI',
              parameter: 'stable-diffusion',
              description: 'Download Stable Diffusion WebUI from Downloads page',
            },
            {
              type: 'SwitchToFree',
              label: 'Use Stock Images',
              parameter: 'Stock',
              description: 'Switch to free stock images instead',
            },
          ],
        },
      ],
    };

    renderWithProviders(
      <PreflightPanel
        profile="Balanced Mix"
        report={report}
        isRunning={false}
        onRunPreflight={mockRunPreflight}
      />
    );

    // Both fix actions should be present
    expect(screen.getByText('Download SD WebUI')).toBeDefined();
    expect(screen.getByText('Use Stock Images')).toBeDefined();
  });
});
