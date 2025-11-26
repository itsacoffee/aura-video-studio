import type { BriefData, StyleData, ExportData, AdvancedData } from './types';

/**
 * Video preset definition for prosumer-friendly defaults.
 * Each preset is optimized for a specific use case.
 */
export interface VideoPreset {
  id: string;
  name: string;
  description: string;
  icon: string;
  duration: number; // seconds
  ttsProvider: 'Windows' | 'Piper' | 'ElevenLabs' | 'PlayHT';
  llmProvider: string;
  imageProvider: string;
  resolution: '720p' | '1080p' | '4k';
  aspectRatio: '16:9' | '9:16' | '1:1';
  visualStyle: 'modern' | 'minimal' | 'cinematic' | 'playful' | 'professional';
  musicGenre: 'ambient' | 'upbeat' | 'dramatic' | 'none';
  requiresApiKey: boolean;
  worksOffline: boolean;
  estimatedCost: number;
  targetPlatform: string;
}

/**
 * Predefined video presets matching the backend VideoPresets.cs definitions.
 */
export const VIDEO_PRESETS: VideoPreset[] = [
  {
    id: 'quick-demo',
    name: 'Quick Demo',
    description: '30-second preview with Windows TTS. Works with zero configuration.',
    icon: '⚡',
    duration: 30,
    ttsProvider: 'Windows',
    llmProvider: 'RuleBased',
    imageProvider: 'Placeholder',
    resolution: '720p',
    aspectRatio: '16:9',
    visualStyle: 'modern',
    musicGenre: 'none',
    requiresApiKey: false,
    worksOffline: true,
    estimatedCost: 0,
    targetPlatform: 'general',
  },
  {
    id: 'youtube-short',
    name: 'YouTube Short',
    description: '60-second vertical video optimized for YouTube Shorts.',
    icon: '📱',
    duration: 60,
    ttsProvider: 'Windows',
    llmProvider: 'RuleBased',
    imageProvider: 'Stock',
    resolution: '1080p',
    aspectRatio: '9:16',
    visualStyle: 'playful',
    musicGenre: 'upbeat',
    requiresApiKey: false,
    worksOffline: false,
    estimatedCost: 0,
    targetPlatform: 'youtube',
  },
  {
    id: 'tutorial',
    name: 'Tutorial',
    description: 'Educational content with clear visuals. Perfect for how-to videos.',
    icon: '📚',
    duration: 180,
    ttsProvider: 'Windows',
    llmProvider: 'RuleBased',
    imageProvider: 'Stock',
    resolution: '1080p',
    aspectRatio: '16:9',
    visualStyle: 'professional',
    musicGenre: 'ambient',
    requiresApiKey: false,
    worksOffline: false,
    estimatedCost: 0,
    targetPlatform: 'youtube',
  },
  {
    id: 'social-media',
    name: 'Social Media',
    description: 'Quick engaging content for Instagram, Twitter, and LinkedIn.',
    icon: '🎯',
    duration: 45,
    ttsProvider: 'Windows',
    llmProvider: 'RuleBased',
    imageProvider: 'Stock',
    resolution: '1080p',
    aspectRatio: '1:1',
    visualStyle: 'modern',
    musicGenre: 'upbeat',
    requiresApiKey: false,
    worksOffline: false,
    estimatedCost: 0,
    targetPlatform: 'social',
  },
];

/**
 * Applies a preset's settings to the wizard data objects.
 * Returns the updated brief, style, export, and advanced data.
 */
export function applyPresetToWizardData(preset: VideoPreset): {
  brief: Partial<BriefData>;
  style: Partial<StyleData>;
  export: Partial<ExportData>;
  advanced: Partial<AdvancedData>;
} {
  return {
    brief: {
      duration: preset.duration,
    },
    style: {
      voiceProvider: preset.ttsProvider,
      visualStyle: preset.visualStyle,
      musicGenre: preset.musicGenre,
      musicEnabled: preset.musicGenre !== 'none',
      imageProvider: preset.imageProvider,
      imageAspectRatio: preset.aspectRatio,
    },
    export: {
      resolution: preset.resolution,
      format: 'mp4',
      includeCaptions: true,
    },
    advanced: {
      targetPlatform: preset.targetPlatform as
        | 'youtube'
        | 'tiktok'
        | 'instagram'
        | 'twitter'
        | 'linkedin',
    },
  };
}
