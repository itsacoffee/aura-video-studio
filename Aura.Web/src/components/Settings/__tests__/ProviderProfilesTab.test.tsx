import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import * as providerProfilesApi from '../../../api/providerProfiles';
import type { ProviderProfileDto, ProfileRecommendationDto } from '../../../types/api-v1';
import { ProviderProfilesTab } from '../ProviderProfilesTab';

vi.mock('../../../api/providerProfiles');

const mockProfiles: ProviderProfileDto[] = [
  {
    id: 'free-only',
    name: 'Free-Only',
    description: 'Uses only free and offline providers',
    tier: 'FreeOnly',
    stages: { Script: 'Free', TTS: 'Windows', Visuals: 'Stock' },
    requiredApiKeys: [],
    usageNotes: 'Ideal for testing',
    lastValidatedAt: null,
  },
  {
    id: 'balanced-mix',
    name: 'Balanced Mix',
    description: 'Combines free and premium providers',
    tier: 'BalancedMix',
    stages: { Script: 'ProIfAvailable', TTS: 'Windows', Visuals: 'StockOrLocal' },
    requiredApiKeys: ['openai'],
    usageNotes: 'Best balance of quality and cost',
    lastValidatedAt: null,
  },
  {
    id: 'pro-max',
    name: 'Pro-Max',
    description: 'Premium providers for highest quality',
    tier: 'ProMax',
    stages: { Script: 'Pro', TTS: 'Pro', Visuals: 'Pro' },
    requiredApiKeys: ['openai', 'elevenlabs', 'stabilityai'],
    usageNotes: 'Maximum quality',
    lastValidatedAt: null,
  },
];

const mockRecommendation: ProfileRecommendationDto = {
  recommendedProfileId: 'free-only',
  recommendedProfileName: 'Free-Only',
  reason: 'No premium API keys configured',
  availableKeys: [],
  missingKeysForProMax: ['openai', 'elevenlabs', 'stabilityai'],
};

describe('ProviderProfilesTab', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(providerProfilesApi.getProfiles).mockResolvedValue(mockProfiles);
    vi.mocked(providerProfilesApi.getActiveProfile).mockResolvedValue(mockProfiles[0]);
    vi.mocked(providerProfilesApi.getRecommendedProfile).mockResolvedValue(mockRecommendation);
  });

  it('renders provider profiles', async () => {
    render(<ProviderProfilesTab />);

    await waitFor(() => {
      expect(screen.getByText('Free-Only')).toBeInTheDocument();
      expect(screen.getByText('Balanced Mix')).toBeInTheDocument();
      expect(screen.getByText('Pro-Max')).toBeInTheDocument();
    });
  });

  it('displays recommendation section', async () => {
    render(<ProviderProfilesTab />);

    await waitFor(() => {
      const elements = screen.getAllByText(/Free-Only/i);
      expect(elements.length).toBeGreaterThan(0);
    });
  });

  it('shows profile descriptions', async () => {
    render(<ProviderProfilesTab />);

    await waitFor(() => {
      expect(screen.getByText(/Uses only free and offline providers/)).toBeInTheDocument();
      expect(screen.getByText(/Combines free and premium providers/)).toBeInTheDocument();
      expect(screen.getByText(/Premium providers for highest quality/)).toBeInTheDocument();
    });
  });

  it('displays tier badges', async () => {
    render(<ProviderProfilesTab />);

    await waitFor(() => {
      expect(screen.getByText('Free')).toBeInTheDocument();
      expect(screen.getByText('Balanced')).toBeInTheDocument();
      expect(screen.getByText('Premium')).toBeInTheDocument();
    });
  });

  it('shows active profile badge', async () => {
    render(<ProviderProfilesTab />);

    await waitFor(() => {
      expect(screen.getByText('Active')).toBeInTheDocument();
    });
  });
});
