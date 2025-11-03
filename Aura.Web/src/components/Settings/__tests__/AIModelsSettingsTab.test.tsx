/**
 * Tests for AIModelsSettingsTab component
 */

import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { render, screen } from '@testing-library/react';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { AIModelsSettingsTab } from '../AIModelsSettingsTab';

// Mock the ModelManager component
vi.mock('../../Engines/ModelManager', () => ({
  ModelManager: ({ engineName }: { engineName: string }) => (
    <div data-testid={`model-manager-${engineName.toLowerCase()}`}>
      ModelManager for {engineName}
    </div>
  ),
}));

const renderWithProvider = (component: React.ReactElement) => {
  return render(<FluentProvider theme={webLightTheme}>{component}</FluentProvider>);
};

describe('AIModelsSettingsTab', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should render the component with title and description', () => {
    renderWithProvider(<AIModelsSettingsTab />);

    expect(screen.getByText('AI Models Management')).toBeInTheDocument();
    expect(screen.getByText(/Manage AI models and voices for local engines/)).toBeInTheDocument();
  });

  it('should display info message about cloud providers', () => {
    renderWithProvider(<AIModelsSettingsTab />);

    expect(
      screen.getByText(/Models shown here are for locally-installed AI engines/)
    ).toBeInTheDocument();
    expect(
      screen.getByText(/Cloud-based providers.*do not require model downloads/i)
    ).toBeInTheDocument();
  });

  it('should display info box with model management features', () => {
    renderWithProvider(<AIModelsSettingsTab />);

    expect(screen.getByText('📦 About Model Management')).toBeInTheDocument();
    expect(screen.getByText(/View all installed models and their sizes/)).toBeInTheDocument();
    expect(screen.getByText(/Add external folders with existing models/)).toBeInTheDocument();
    expect(screen.getByText(/Verify model checksums for integrity/)).toBeInTheDocument();
    expect(screen.getByText(/Remove unused models to free up space/)).toBeInTheDocument();
  });

  it('should display all engine sections', () => {
    renderWithProvider(<AIModelsSettingsTab />);

    expect(screen.getByText('Ollama')).toBeInTheDocument();
    expect(screen.getByText('Piper')).toBeInTheDocument();
    expect(screen.getByText('Mimic3')).toBeInTheDocument();
    expect(screen.getByText('Stable Diffusion')).toBeInTheDocument();
    expect(screen.getByText('ComfyUI')).toBeInTheDocument();
  });

  it('should display engine descriptions', () => {
    renderWithProvider(<AIModelsSettingsTab />);

    expect(screen.getByText('Local LLM models for script generation')).toBeInTheDocument();
    expect(screen.getByText('Neural text-to-speech voices')).toBeInTheDocument();
    expect(screen.getByText('Offline text-to-speech voices')).toBeInTheDocument();
    expect(screen.getByText('AI image generation models')).toBeInTheDocument();
    expect(screen.getByText('Node-based AI image generation')).toBeInTheDocument();
  });

  it('should show Ollama section expanded by default', () => {
    renderWithProvider(<AIModelsSettingsTab />);

    // Ollama should be expanded, showing its ModelManager
    expect(screen.getByTestId('model-manager-ollama')).toBeInTheDocument();
  });

  it('should not show other engine sections by default', () => {
    renderWithProvider(<AIModelsSettingsTab />);

    // Other engines should not be expanded by default
    expect(screen.queryByTestId('model-manager-piper')).not.toBeInTheDocument();
    expect(screen.queryByTestId('model-manager-mimic3')).not.toBeInTheDocument();
    expect(screen.queryByTestId('model-manager-stable diffusion')).not.toBeInTheDocument();
    expect(screen.queryByTestId('model-manager-comfyui')).not.toBeInTheDocument();
  });
});
