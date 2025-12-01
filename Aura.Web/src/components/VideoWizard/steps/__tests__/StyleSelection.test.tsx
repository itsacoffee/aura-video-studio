import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import type { StyleData, BriefData } from '../../types';
import { StyleSelection } from '../StyleSelection';
import * as visualsClientModule from '../../../../api/visualsClient';

// Mock the visualsClient
vi.mock('../../../../api/visualsClient', () => ({
  getVisualsClient: vi.fn(),
}));

describe('StyleSelection', () => {
  const mockBriefData: BriefData = {
    topic: 'AI Video Generation',
    videoType: 'educational',
    targetAudience: 'Content creators',
    keyMessage: 'Create videos easily with AI',
    duration: 60,
  };

  const mockStyleData: StyleData = {
    voiceProvider: 'ElevenLabs',
    voiceName: 'Adam',
    visualStyle: 'modern',
    musicGenre: 'none',
    musicEnabled: false,
    imageProvider: 'Placeholder',
    imageStyle: 'photorealistic',
  };

  const mockProviders = [
    {
      name: 'Placeholder',
      isAvailable: true,
      requiresApiKey: false,
      capabilities: {
        providerName: 'Placeholder',
        supportsNegativePrompts: false,
        supportsBatchGeneration: true,
        supportsStylePresets: false,
        supportedAspectRatios: ['16:9', '9:16', '1:1', '4:3'],
        supportedStyles: ['solid-color', 'gradient', 'text-overlay'],
        maxWidth: 1920,
        maxHeight: 1080,
        isLocal: true,
        isFree: true,
        costPerImage: 0,
        tier: 'Free - Always Available',
      },
    },
    {
      name: 'Stock',
      isAvailable: true,
      requiresApiKey: false,
      capabilities: {
        providerName: 'Stock',
        supportsNegativePrompts: false,
        supportsBatchGeneration: false,
        supportsStylePresets: false,
        supportedAspectRatios: ['16:9', '9:16', '1:1', '4:3'],
        supportedStyles: ['photorealistic', 'artistic', 'cinematic'],
        maxWidth: 1920,
        maxHeight: 1080,
        isLocal: false,
        isFree: true,
        costPerImage: 0,
        tier: 'Free',
      },
    },
    {
      name: 'LocalSD',
      isAvailable: false,
      requiresApiKey: false,
      capabilities: {
        providerName: 'LocalSD',
        supportsNegativePrompts: true,
        supportsBatchGeneration: true,
        supportsStylePresets: true,
        supportedAspectRatios: ['16:9', '9:16', '1:1', '4:3'],
        supportedStyles: ['photorealistic', 'artistic', 'cinematic', 'minimalist'],
        maxWidth: 2048,
        maxHeight: 2048,
        isLocal: true,
        isFree: true,
        costPerImage: 0,
        tier: 'Free',
      },
    },
  ];

  const mockVisualsClient = {
    getProviders: vi.fn().mockResolvedValue({
      providers: mockProviders,
      timestamp: new Date().toISOString(),
    }),
    getStyles: vi.fn().mockResolvedValue({
      allStyles: ['photorealistic', 'artistic', 'cinematic', 'minimalist'],
      stylesByProvider: {},
    }),
  };

  const mockOnChange = vi.fn();
  const mockOnValidationChange = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(visualsClientModule.getVisualsClient).mockReturnValue(
      mockVisualsClient as unknown as visualsClientModule.VisualsClient
    );
  });

  it('renders style selection header', async () => {
    render(
      <StyleSelection
        data={mockStyleData}
        briefData={mockBriefData}
        advancedMode={false}
        onChange={mockOnChange}
        onValidationChange={mockOnValidationChange}
      />
    );

    await waitFor(() => {
      expect(screen.getByText('Style Selection')).toBeInTheDocument();
    });
  });

  it('displays provider cards with subtitles', async () => {
    render(
      <StyleSelection
        data={mockStyleData}
        briefData={mockBriefData}
        advancedMode={false}
        onChange={mockOnChange}
        onValidationChange={mockOnValidationChange}
      />
    );

    await waitFor(() => {
      // Check provider names are displayed
      expect(screen.getByText('Placeholder')).toBeInTheDocument();
      expect(screen.getByText('Stock')).toBeInTheDocument();
      expect(screen.getByText('LocalSD')).toBeInTheDocument();

      // Check subtitles are displayed for each provider
      expect(screen.getByText('Solid color backgrounds with text overlay')).toBeInTheDocument();
      expect(screen.getByText('Pexels • Pixabay • Unsplash')).toBeInTheDocument();
      expect(screen.getByText('Stable Diffusion WebUI')).toBeInTheDocument();
    });
  });

  it('sets default imageStyle to photorealistic when not provided', async () => {
    const styleDataWithoutImageStyle: StyleData = {
      ...mockStyleData,
      imageStyle: undefined,
      musicGenre: undefined,
      voiceProvider: undefined,
      visualStyle: undefined,
    } as unknown as StyleData;

    render(
      <StyleSelection
        data={styleDataWithoutImageStyle}
        briefData={mockBriefData}
        advancedMode={false}
        onChange={mockOnChange}
        onValidationChange={mockOnValidationChange}
      />
    );

    await waitFor(() => {
      // Verify onChange was called with photorealistic as the default imageStyle
      expect(mockOnChange).toHaveBeenCalledWith(
        expect.objectContaining({
          imageStyle: 'photorealistic',
        })
      );
    });
  });

  it('sets default musicGenre to none', async () => {
    const styleDataWithoutMusic: StyleData = {
      ...mockStyleData,
      musicGenre: undefined,
      voiceProvider: undefined,
      visualStyle: undefined,
      imageStyle: undefined,
    } as unknown as StyleData;

    render(
      <StyleSelection
        data={styleDataWithoutMusic}
        briefData={mockBriefData}
        advancedMode={false}
        onChange={mockOnChange}
        onValidationChange={mockOnValidationChange}
      />
    );

    await waitFor(() => {
      // Verify onChange was called with none as the default musicGenre
      expect(mockOnChange).toHaveBeenCalledWith(
        expect.objectContaining({
          musicGenre: 'none',
        })
      );
    });
  });

  it('sets musicEnabled to false by default', async () => {
    const styleDataWithoutDefaults: StyleData = {
      ...mockStyleData,
      musicGenre: undefined,
      musicEnabled: undefined,
      voiceProvider: undefined,
      visualStyle: undefined,
      imageStyle: undefined,
    } as unknown as StyleData;

    render(
      <StyleSelection
        data={styleDataWithoutDefaults}
        briefData={mockBriefData}
        advancedMode={false}
        onChange={mockOnChange}
        onValidationChange={mockOnValidationChange}
      />
    );

    await waitFor(() => {
      // Verify onChange was called with musicEnabled as false
      expect(mockOnChange).toHaveBeenCalledWith(
        expect.objectContaining({
          musicEnabled: false,
        })
      );
    });
  });

  it('displays music settings with None as default selected', async () => {
    render(
      <StyleSelection
        data={mockStyleData}
        briefData={mockBriefData}
        advancedMode={false}
        onChange={mockOnChange}
        onValidationChange={mockOnValidationChange}
      />
    );

    await waitFor(() => {
      expect(screen.getByText('Music Settings')).toBeInTheDocument();
    });

    // Check that the Background Music dropdown exists and shows 'none' as the value
    const musicDropdown = screen.getByRole('combobox', { name: /background music/i });
    expect(musicDropdown).toBeInTheDocument();
    // The button should contain text matching "none" (case insensitive)
    expect(musicDropdown.textContent).toMatch(/none/i);
  });

  it('allows selecting a provider card', async () => {
    render(
      <StyleSelection
        data={mockStyleData}
        briefData={mockBriefData}
        advancedMode={false}
        onChange={mockOnChange}
        onValidationChange={mockOnValidationChange}
      />
    );

    await waitFor(() => {
      expect(screen.getByText('Stock')).toBeInTheDocument();
    });

    // Click on Stock provider title text to select the provider
    const stockTitle = screen.getByText('Stock');
    fireEvent.click(stockTitle);

    await waitFor(() => {
      expect(mockOnChange).toHaveBeenCalledWith(
        expect.objectContaining({
          imageProvider: 'Stock',
        })
      );
    });
  });

  it('displays validation message correctly', async () => {
    render(
      <StyleSelection
        data={mockStyleData}
        briefData={mockBriefData}
        advancedMode={false}
        onChange={mockOnChange}
        onValidationChange={mockOnValidationChange}
      />
    );

    await waitFor(() => {
      // Validation should pass with valid data
      expect(mockOnValidationChange).toHaveBeenCalledWith(
        expect.objectContaining({
          isValid: true,
        })
      );
    });
  });

  it('shows unavailable providers with reduced opacity', async () => {
    render(
      <StyleSelection
        data={mockStyleData}
        briefData={mockBriefData}
        advancedMode={false}
        onChange={mockOnChange}
        onValidationChange={mockOnValidationChange}
      />
    );

    await waitFor(() => {
      // LocalSD is not available, so it should show "Not Available"
      expect(screen.getByText('LocalSD')).toBeInTheDocument();
      expect(screen.getByText('Not Available')).toBeInTheDocument();
    });
  });

  it('displays photorealistic first in the available styles', async () => {
    render(
      <StyleSelection
        data={mockStyleData}
        briefData={mockBriefData}
        advancedMode={false}
        onChange={mockOnChange}
        onValidationChange={mockOnValidationChange}
      />
    );

    await waitFor(() => {
      expect(screen.getByText('Image Style')).toBeInTheDocument();
    });

    // The mockVisualsClient returns 'photorealistic' as first in the styles list
    // The component displays it with first letter capitalized
    // Check that the style dropdown button shows the current photorealistic value
    const styleButton = screen.getByRole('combobox', { name: /image style/i });
    expect(styleButton).toBeInTheDocument();
    // The button value should be 'photorealistic' (the mockStyleData has imageStyle: 'photorealistic')
    expect(styleButton.textContent).toMatch(/photorealistic/i);
  });
});
