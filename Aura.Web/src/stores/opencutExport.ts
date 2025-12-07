/**
 * OpenCut Export Store
 *
 * Manages export presets optimized for various platforms with one-click
 * export settings for resolution, codec, bitrate, and format.
 */

import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { createSafeJSONStorage } from './opencutPersist';
import type {
  ExportPreset as BaseExportPreset,
  ExportSettings as BaseExportSettings,
} from '../types/opencut';

/**
 * Extended export settings with optional audio sample rate.
 */
export interface ExportSettings extends BaseExportSettings {
  /** Audio sample rate in Hz */
  audioSampleRate?: number;
}

/**
 * Extended export preset with required platform.
 */
export interface ExportPreset extends Omit<BaseExportPreset, 'settings'> {
  /** Target platform/use case (required for store) */
  platform: string;
  /** Export settings */
  settings: ExportSettings;
}

/**
 * Built-in export presets for common platforms and use cases.
 */
export const BUILTIN_PRESETS: ExportPreset[] = [
  {
    id: 'youtube-4k',
    name: '4K Ultra HD',
    description: 'Best quality for video platforms, 4K resolution at 60fps',
    platform: 'Video Platform',
    settings: {
      format: 'mp4',
      videoCodec: 'h264',
      resolution: { width: 3840, height: 2160 },
      frameRate: 60,
      videoBitrate: 45000,
      audioCodec: 'aac',
      audioBitrate: 320,
      audioSampleRate: 48000,
      qualityPreset: 'ultra',
      useHardwareAcceleration: true,
      twoPass: true,
      includeAudio: true,
      burnCaptions: false,
    },
  },
  {
    id: 'youtube-1080p',
    name: '1080p Full HD',
    description: 'Standard HD quality for video platforms',
    platform: 'Video Platform',
    settings: {
      format: 'mp4',
      videoCodec: 'h264',
      resolution: { width: 1920, height: 1080 },
      frameRate: 30,
      videoBitrate: 12000,
      audioCodec: 'aac',
      audioBitrate: 256,
      audioSampleRate: 48000,
      qualityPreset: 'high',
      useHardwareAcceleration: true,
      twoPass: false,
      includeAudio: true,
      burnCaptions: false,
    },
  },
  {
    id: 'vertical-1080',
    name: 'Vertical HD',
    description: 'Vertical video for mobile platforms (9:16)',
    platform: 'Mobile Video',
    settings: {
      format: 'mp4',
      videoCodec: 'h264',
      resolution: { width: 1080, height: 1920 },
      frameRate: 30,
      videoBitrate: 8000,
      audioCodec: 'aac',
      audioBitrate: 192,
      audioSampleRate: 44100,
      qualityPreset: 'high',
      useHardwareAcceleration: true,
      twoPass: false,
      includeAudio: true,
      burnCaptions: false,
    },
  },
  {
    id: 'square-1080',
    name: 'Square HD',
    description: 'Square format for social media posts (1:1)',
    platform: 'Social Media',
    settings: {
      format: 'mp4',
      videoCodec: 'h264',
      resolution: { width: 1080, height: 1080 },
      frameRate: 30,
      videoBitrate: 6000,
      audioCodec: 'aac',
      audioBitrate: 192,
      audioSampleRate: 44100,
      qualityPreset: 'high',
      useHardwareAcceleration: true,
      twoPass: false,
      includeAudio: true,
      burnCaptions: false,
    },
  },
  {
    id: 'web-720p',
    name: '720p Web',
    description: 'Optimized for web streaming with smaller file size',
    platform: 'Web',
    settings: {
      format: 'mp4',
      videoCodec: 'h264',
      resolution: { width: 1280, height: 720 },
      frameRate: 30,
      videoBitrate: 5000,
      audioCodec: 'aac',
      audioBitrate: 128,
      audioSampleRate: 44100,
      qualityPreset: 'medium',
      useHardwareAcceleration: true,
      twoPass: false,
      includeAudio: true,
      burnCaptions: false,
    },
  },
  {
    id: 'gif-small',
    name: 'Animated GIF',
    description: 'Animated GIF for web sharing (no audio)',
    platform: 'Web',
    settings: {
      format: 'gif',
      // GIF uses palettegen/paletteuse filters, not traditional video codecs
      // h264 is a placeholder; FFmpeg handles GIF conversion differently
      videoCodec: 'h264',
      resolution: { width: 480, height: 480 },
      frameRate: 15,
      // GIF quality is controlled by palette generation, not bitrate
      videoBitrate: 0,
      // GIF format does not support audio
      audioCodec: 'aac',
      audioBitrate: 0,
      qualityPreset: 'medium',
      useHardwareAcceleration: false,
      twoPass: false,
      includeAudio: false,
      burnCaptions: false,
    },
  },
  {
    id: 'prores-master',
    name: 'ProRes Master',
    description: 'High-quality master copy for archiving and further editing',
    platform: 'Archive',
    settings: {
      format: 'mov',
      videoCodec: 'prores',
      resolution: { width: 1920, height: 1080 },
      frameRate: 30,
      // ProRes uses quality-based encoding; bitrate is determined by profile
      // Setting to 0 indicates quality-based rather than bitrate-based encoding
      videoBitrate: 0,
      // PCM audio is uncompressed; bitrate depends on sample rate/bit depth
      audioCodec: 'pcm',
      audioBitrate: 0,
      audioSampleRate: 48000,
      qualityPreset: 'ultra',
      useHardwareAcceleration: false,
      twoPass: false,
      includeAudio: true,
      burnCaptions: false,
    },
  },
];

/**
 * Export state interface.
 */
export interface ExportState {
  /** Built-in export presets */
  builtinPresets: ExportPreset[];
  /** User-created custom presets */
  customPresets: ExportPreset[];
  /** Currently selected preset ID */
  selectedPresetId: string | null;
  /** Current export settings (may differ from preset) */
  currentSettings: ExportSettings | null;
  /** Export progress (0-100) */
  exportProgress: number;
  /** Whether export is in progress */
  isExporting: boolean;
  /** Export error message */
  exportError: string | null;
}

/**
 * Export actions interface.
 */
export interface ExportActions {
  /** Select a preset by ID */
  selectPreset: (presetId: string) => void;
  /** Create a new custom preset */
  createCustomPreset: (name: string, platform: string, settings: ExportSettings) => string;
  /** Update an existing custom preset */
  updateCustomPreset: (presetId: string, updates: Partial<ExportPreset>) => void;
  /** Delete a custom preset */
  deleteCustomPreset: (presetId: string) => void;
  /** Set current export settings */
  setCurrentSettings: (settings: ExportSettings) => void;
  /** Update a single setting in current settings */
  updateCurrentSetting: <K extends keyof ExportSettings>(key: K, value: ExportSettings[K]) => void;
  /** Get a preset by ID */
  getPreset: (presetId: string) => ExportPreset | undefined;
  /** Get all presets for a specific platform */
  getPresetsByPlatform: (platform: string) => ExportPreset[];
  /** Get all unique platforms */
  getAllPlatforms: () => string[];
  /** Start export process */
  startExport: () => Promise<void>;
  /** Cancel export process */
  cancelExport: () => void;
  /** Estimate file size in MB */
  estimateFileSize: (durationSeconds?: number) => number;
  /** Reset export error */
  clearExportError: () => void;
}

export type OpenCutExportStore = ExportState & ExportActions;

function generateId(): string {
  return `custom-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

/**
 * OpenCut Export Store with persistence for custom presets.
 */
export const useExportStore = create<OpenCutExportStore>()(
  persist(
    (set, get) => ({
      builtinPresets: BUILTIN_PRESETS,
      customPresets: [],
      selectedPresetId: 'youtube-1080p',
      currentSettings: BUILTIN_PRESETS.find((p) => p.id === 'youtube-1080p')?.settings ?? null,
      exportProgress: 0,
      isExporting: false,
      exportError: null,

      selectPreset: (presetId) => {
        const preset = get().getPreset(presetId);
        if (preset) {
          set({
            selectedPresetId: presetId,
            currentSettings: { ...preset.settings },
          });
        }
      },

      createCustomPreset: (name, platform, settings) => {
        const id = generateId();
        const preset: ExportPreset = {
          id,
          name,
          description: `Custom preset for ${platform}`,
          platform,
          settings,
        };
        set((state) => ({
          customPresets: [...state.customPresets, preset],
        }));
        return id;
      },

      updateCustomPreset: (presetId, updates) => {
        set((state) => ({
          customPresets: state.customPresets.map((p) =>
            p.id === presetId ? { ...p, ...updates } : p
          ),
        }));
      },

      deleteCustomPreset: (presetId) => {
        set((state) => ({
          customPresets: state.customPresets.filter((p) => p.id !== presetId),
          selectedPresetId:
            state.selectedPresetId === presetId ? 'youtube-1080p' : state.selectedPresetId,
        }));
      },

      setCurrentSettings: (settings) => set({ currentSettings: settings }),

      updateCurrentSetting: (key, value) => {
        set((state) => ({
          currentSettings: state.currentSettings
            ? { ...state.currentSettings, [key]: value }
            : null,
        }));
      },

      getPreset: (presetId) => {
        const all = [...get().builtinPresets, ...get().customPresets];
        return all.find((p) => p.id === presetId);
      },

      getPresetsByPlatform: (platform) => {
        const all = [...get().builtinPresets, ...get().customPresets];
        return all.filter((p) => p.platform === platform);
      },

      getAllPlatforms: () => {
        const all = [...get().builtinPresets, ...get().customPresets];
        const platforms = new Set(all.map((p) => p.platform));
        return Array.from(platforms);
      },

      startExport: async () => {
        set({ isExporting: true, exportProgress: 0, exportError: null });
        // Export logic would be implemented with FFmpeg
        // This simulates the export process for UI demonstration
        try {
          for (let i = 0; i <= 100; i += 5) {
            await new Promise((r) => setTimeout(r, 100));
            if (!get().isExporting) {
              // Export was cancelled
              return;
            }
            set({ exportProgress: i });
          }
          set({ isExporting: false, exportProgress: 100 });
        } catch (error: unknown) {
          const errorMessage = error instanceof Error ? error.message : String(error);
          set({ isExporting: false, exportError: errorMessage });
        }
      },

      cancelExport: () => {
        set({ isExporting: false, exportProgress: 0 });
      },

      estimateFileSize: (durationSeconds = 60) => {
        const settings = get().currentSettings;
        if (!settings) return 0;
        // Calculate estimated file size in MB
        // Formula: (video_bitrate + audio_bitrate) * duration / 8192
        // where 8192 = 8 bits/byte * 1024 bytes/KB
        const videoBitrate = settings.videoBitrate || 0;
        const audioBitrate = settings.audioBitrate || 0;
        const totalBitrateKbps = videoBitrate + audioBitrate;
        // Convert kbps to MB: (kbps * seconds) / 8192
        return (totalBitrateKbps * durationSeconds) / 8192;
      },

      clearExportError: () => set({ exportError: null }),
    }),
    {
      name: 'opencut-export',
      storage: createSafeJSONStorage<OpenCutExportStore>('opencut-export'),
      partialize: (state) => ({
        customPresets: state.customPresets,
      }),
    }
  )
);
