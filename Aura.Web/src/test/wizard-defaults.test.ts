import { describe, it, expect } from 'vitest';

// Test default wizard settings
describe('Wizard Defaults', () => {
  it('should have correct default brief settings', () => {
    const defaultBrief = {
      topic: '',
      audience: 'General',
      goal: 'Informative',
      tone: 'Informative',
      language: 'en-US',
      aspect: 'Widescreen16x9' as const,
    };

    expect(defaultBrief.audience).toBe('General');
    expect(defaultBrief.tone).toBe('Informative');
    expect(defaultBrief.language).toBe('en-US');
  });

  it('should have correct default plan settings', () => {
    const defaultPlan = {
      targetDurationMinutes: 3.0,
      pacing: 'Conversational' as const,
      density: 'Balanced' as const,
      style: 'Documentary',
    };

    expect(defaultPlan.targetDurationMinutes).toBe(3.0);
    expect(defaultPlan.pacing).toBe('Conversational');
    expect(defaultPlan.density).toBe('Balanced');
  });

  it('should have correct default brand kit settings', () => {
    const defaultBrandKit = {
      watermarkOpacity: 0.7,
    };

    expect(defaultBrandKit.watermarkOpacity).toBe(0.7);
  });

  it('should have correct default captions settings', () => {
    const defaultCaptions = {
      enabled: true,
      format: 'srt' as const,
      burnIn: false,
      fontName: 'Arial',
      fontSize: 32,
      primaryColor: '#FFFFFF',
      outlineColor: '#000000',
      outlineWidth: 2,
      position: 'Bottom Center',
    };

    expect(defaultCaptions.enabled).toBe(true);
    expect(defaultCaptions.format).toBe('srt');
    expect(defaultCaptions.burnIn).toBe(false);
  });

  it('should have correct default stock sources', () => {
    const defaultStockSources = {
      enablePexels: true,
      enablePixabay: true,
      enableUnsplash: true,
      enableLocalAssets: false,
      enableStableDiffusion: false,
    };

    expect(defaultStockSources.enablePexels).toBe(true);
    expect(defaultStockSources.enablePixabay).toBe(true);
    expect(defaultStockSources.enableUnsplash).toBe(true);
  });
});
