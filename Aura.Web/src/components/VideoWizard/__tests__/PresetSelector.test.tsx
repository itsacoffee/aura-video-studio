import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { VIDEO_PRESETS, applyPresetToWizardData } from '../presetData';
import PresetSelector from '../PresetSelector';

describe('PresetSelector', () => {
  const mockOnSelectPreset = vi.fn();
  const mockOnSkip = vi.fn();

  beforeEach(() => {
    mockOnSelectPreset.mockClear();
    mockOnSkip.mockClear();
  });

  it('renders all 4 preset profiles', () => {
    render(<PresetSelector selectedPresetId={null} onSelectPreset={mockOnSelectPreset} />);

    expect(screen.getByText('Quick Demo')).toBeInTheDocument();
    expect(screen.getByText('YouTube Short')).toBeInTheDocument();
    expect(screen.getByText('Tutorial')).toBeInTheDocument();
    expect(screen.getByText('Social Media')).toBeInTheDocument();
  });

  it('displays preset descriptions', () => {
    render(<PresetSelector selectedPresetId={null} onSelectPreset={mockOnSelectPreset} />);

    expect(screen.getByText(/30-second preview with Windows TTS/)).toBeInTheDocument();
    expect(screen.getByText(/60-second vertical video/)).toBeInTheDocument();
    expect(screen.getByText(/Educational content/)).toBeInTheDocument();
    expect(screen.getByText(/Quick engaging content/)).toBeInTheDocument();
  });

  it('calls onSelectPreset when a preset is clicked', () => {
    render(<PresetSelector selectedPresetId={null} onSelectPreset={mockOnSelectPreset} />);

    // Find the Quick Demo text and click its parent card
    const quickDemoText = screen.getByText('Quick Demo');
    // The Card component is an ancestor, find the clickable area
    const card =
      quickDemoText.closest('[role="group"]') ||
      quickDemoText.parentElement?.parentElement?.parentElement;
    expect(card).not.toBeNull();
    if (card) {
      fireEvent.click(card);
    }

    expect(mockOnSelectPreset).toHaveBeenCalledWith(
      expect.objectContaining({
        id: 'quick-demo',
        name: 'Quick Demo',
      })
    );
  });

  it('shows Selected badge for selected preset', () => {
    render(<PresetSelector selectedPresetId="quick-demo" onSelectPreset={mockOnSelectPreset} />);

    expect(screen.getByText('Selected')).toBeInTheDocument();
  });

  it('shows skip button when showSkipButton is true', () => {
    render(
      <PresetSelector
        selectedPresetId={null}
        onSelectPreset={mockOnSelectPreset}
        onSkip={mockOnSkip}
        showSkipButton={true}
      />
    );

    const skipButton = screen.getByRole('button', { name: /Skip and customize manually/i });
    expect(skipButton).toBeInTheDocument();

    fireEvent.click(skipButton);
    expect(mockOnSkip).toHaveBeenCalled();
  });

  it('does not show skip button when showSkipButton is false', () => {
    render(
      <PresetSelector
        selectedPresetId={null}
        onSelectPreset={mockOnSelectPreset}
        showSkipButton={false}
      />
    );

    expect(
      screen.queryByRole('button', { name: /Skip and customize manually/i })
    ).not.toBeInTheDocument();
  });

  it('displays Free and Offline badges for Quick Demo preset', () => {
    render(<PresetSelector selectedPresetId={null} onSelectPreset={mockOnSelectPreset} />);

    // Quick Demo should have Offline badge since worksOffline is true
    const offlineBadges = screen.getAllByText('Offline');
    expect(offlineBadges.length).toBeGreaterThan(0);

    // All presets with estimatedCost === 0 should have $0 badge
    const costBadges = screen.getAllByText('$0');
    expect(costBadges.length).toBe(4);
  });
});

describe('VIDEO_PRESETS', () => {
  it('contains exactly 4 presets', () => {
    expect(VIDEO_PRESETS).toHaveLength(4);
  });

  it('Quick Demo preset has correct defaults', () => {
    const quickDemo = VIDEO_PRESETS.find((p) => p.id === 'quick-demo');
    expect(quickDemo).toBeDefined();
    expect(quickDemo?.ttsProvider).toBe('Windows');
    expect(quickDemo?.llmProvider).toBe('RuleBased');
    expect(quickDemo?.imageProvider).toBe('Placeholder');
    expect(quickDemo?.requiresApiKey).toBe(false);
    expect(quickDemo?.worksOffline).toBe(true);
    expect(quickDemo?.duration).toBe(30);
  });

  it('YouTube Short preset has vertical aspect ratio', () => {
    const youtubeShort = VIDEO_PRESETS.find((p) => p.id === 'youtube-short');
    expect(youtubeShort).toBeDefined();
    expect(youtubeShort?.aspectRatio).toBe('9:16');
    expect(youtubeShort?.resolution).toBe('1080p');
  });

  it('Tutorial preset has longer duration', () => {
    const tutorial = VIDEO_PRESETS.find((p) => p.id === 'tutorial');
    expect(tutorial).toBeDefined();
    expect(tutorial?.duration).toBe(180); // 3 minutes
    expect(tutorial?.visualStyle).toBe('professional');
  });

  it('Social Media preset has square aspect ratio', () => {
    const socialMedia = VIDEO_PRESETS.find((p) => p.id === 'social-media');
    expect(socialMedia).toBeDefined();
    expect(socialMedia?.aspectRatio).toBe('1:1');
  });

  it('all presets favor free/local providers', () => {
    for (const preset of VIDEO_PRESETS) {
      expect(preset.requiresApiKey).toBe(false);
      expect(preset.estimatedCost).toBe(0);
      expect(['Windows', 'Piper']).toContain(preset.ttsProvider);
      expect(['RuleBased', 'Ollama']).toContain(preset.llmProvider);
    }
  });
});

describe('applyPresetToWizardData', () => {
  it('applies Quick Demo preset to wizard data', () => {
    const quickDemo = VIDEO_PRESETS.find((p) => p.id === 'quick-demo')!;
    const result = applyPresetToWizardData(quickDemo);

    expect(result.brief.duration).toBe(30);
    expect(result.style.voiceProvider).toBe('Windows');
    expect(result.style.visualStyle).toBe('modern');
    expect(result.style.imageProvider).toBe('Placeholder');
    expect(result.export.resolution).toBe('720p');
  });

  it('applies YouTube Short preset with vertical settings', () => {
    const youtubeShort = VIDEO_PRESETS.find((p) => p.id === 'youtube-short')!;
    const result = applyPresetToWizardData(youtubeShort);

    expect(result.style.imageAspectRatio).toBe('9:16');
    expect(result.export.resolution).toBe('1080p');
    expect(result.advanced.targetPlatform).toBe('youtube');
  });

  it('enables music for presets with music genre', () => {
    const socialMedia = VIDEO_PRESETS.find((p) => p.id === 'social-media')!;
    const result = applyPresetToWizardData(socialMedia);

    expect(result.style.musicEnabled).toBe(true);
    expect(result.style.musicGenre).toBe('upbeat');
  });

  it('disables music for presets with none genre', () => {
    const quickDemo = VIDEO_PRESETS.find((p) => p.id === 'quick-demo')!;
    const result = applyPresetToWizardData(quickDemo);

    expect(result.style.musicEnabled).toBe(false);
    expect(result.style.musicGenre).toBe('none');
  });
});
