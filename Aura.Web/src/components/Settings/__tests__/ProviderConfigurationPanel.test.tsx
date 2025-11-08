/**
 * Tests for Provider Configuration Panel
 */

import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import apiClient from '../../../services/api/apiClient';
import { ProviderConfigurationPanel } from '../ProviderConfigurationPanel';

vi.mock('../../../services/api/apiClient');

const mockProviders = {
  providers: [
    {
      name: 'OpenAI',
      type: 'LLM',
      enabled: true,
      priority: 1,
      apiKey: null,
      additionalSettings: {},
      costLimit: null,
      status: 'Not configured',
    },
    {
      name: 'ElevenLabs',
      type: 'TTS',
      enabled: false,
      priority: 2,
      apiKey: null,
      additionalSettings: {},
      costLimit: 100,
      status: 'Not configured',
    },
  ],
};

describe('ProviderConfigurationPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders provider configuration panel', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockProviders });

    render(<ProviderConfigurationPanel />);

    await waitFor(() => {
      expect(screen.getByText('Provider Configuration')).toBeInTheDocument();
    });

    expect(
      screen.getByText('Configure API keys, priorities, and cost limits for all providers')
    ).toBeInTheDocument();
  });

  it('loads providers on mount', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockProviders });

    render(<ProviderConfigurationPanel />);

    await waitFor(() => {
      expect(apiClient.get).toHaveBeenCalledWith('/api/providerconfiguration/providers');
    });

    await waitFor(() => {
      expect(screen.getByText('OpenAI')).toBeInTheDocument();
      expect(screen.getByText('ElevenLabs')).toBeInTheDocument();
    });
  });

  it('displays provider status badges', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockProviders });

    render(<ProviderConfigurationPanel />);

    await waitFor(() => {
      expect(screen.getByText('Enabled')).toBeInTheDocument();
      expect(screen.getByText('Disabled')).toBeInTheDocument();
    });
  });

  it('saves configuration when save button clicked', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockProviders });
    vi.mocked(apiClient.post).mockResolvedValue({ data: { success: true } });

    render(<ProviderConfigurationPanel />);

    await waitFor(() => {
      expect(screen.getByText('OpenAI')).toBeInTheDocument();
    });

    const saveButton = screen.getByRole('button', { name: /save configuration/i });
    fireEvent.click(saveButton);

    await waitFor(() => {
      expect(apiClient.post).toHaveBeenCalledWith('/api/providerconfiguration/providers', {
        providers: expect.arrayContaining([
          expect.objectContaining({ name: 'OpenAI' }),
          expect.objectContaining({ name: 'ElevenLabs' }),
        ]),
      });
    });
  });

  it('shows error message on load failure', async () => {
    vi.mocked(apiClient.get).mockRejectedValue(new Error('Network error'));

    render(<ProviderConfigurationPanel />);

    await waitFor(() => {
      expect(screen.getByText(/failed to load provider configuration/i)).toBeInTheDocument();
    });
  });

  it('displays priority controls for providers', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockProviders });

    render(<ProviderConfigurationPanel />);

    await waitFor(() => {
      expect(screen.getAllByRole('button', { name: /move up/i })).toHaveLength(2);
      expect(screen.getAllByRole('button', { name: /move down/i })).toHaveLength(2);
    });
  });
});
