/**
 * Provider configuration state management
 */

import { create } from 'zustand';
import type {
  ProviderConfigDto,
  QualityConfigDto,
  ConfigurationProfileDto,
  ConfigValidationResultDto,
} from '../types/api-v1';

interface ProviderConfigState {
  providers: ProviderConfigDto[];
  qualityConfig: QualityConfigDto | null;
  profiles: ConfigurationProfileDto[];
  validationResult: ConfigValidationResultDto | null;
  isLoading: boolean;
  isSaving: boolean;
  error: string | null;

  // Actions
  setProviders: (providers: ProviderConfigDto[]) => void;
  setQualityConfig: (config: QualityConfigDto) => void;
  setProfiles: (profiles: ConfigurationProfileDto[]) => void;
  setValidationResult: (result: ConfigValidationResultDto | null) => void;
  setIsLoading: (loading: boolean) => void;
  setIsSaving: (saving: boolean) => void;
  setError: (error: string | null) => void;
  updateProvider: (name: string, updates: Partial<ProviderConfigDto>) => void;
  reorderProviders: (startIndex: number, endIndex: number) => void;
  resetToDefaults: () => void;
}

const createDefaultQualityConfig = (): QualityConfigDto => ({
  video: {
    resolution: '1080p',
    width: 1920,
    height: 1080,
    framerate: 30,
    bitratePreset: 'High',
    bitrateKbps: 5000,
    codec: 'h264',
    container: 'mp4',
  },
  audio: {
    bitrate: 192,
    sampleRate: 48000,
    channels: 2,
    codec: 'aac',
  },
  subtitles: {
    fontFamily: 'Arial',
    fontSize: 24,
    fontColor: '#FFFFFF',
    backgroundColor: '#000000',
    backgroundOpacity: 0.7,
    position: 'Bottom',
    outlineWidth: 2,
    outlineColor: '#000000',
  },
});

export const useProviderConfigStore = create<ProviderConfigState>((set) => ({
  providers: [],
  qualityConfig: null,
  profiles: [],
  validationResult: null,
  isLoading: false,
  isSaving: false,
  error: null,

  setProviders: (providers) => set({ providers }),

  setQualityConfig: (config) => set({ qualityConfig: config }),

  setProfiles: (profiles) => set({ profiles }),

  setValidationResult: (result) => set({ validationResult: result }),

  setIsLoading: (loading) => set({ isLoading: loading }),

  setIsSaving: (saving) => set({ isSaving: saving }),

  setError: (error) => set({ error }),

  updateProvider: (name, updates) =>
    set((state) => ({
      providers: state.providers.map((p) => (p.name === name ? { ...p, ...updates } : p)),
    })),

  reorderProviders: (startIndex, endIndex) =>
    set((state) => {
      const result = Array.from(state.providers);
      const [removed] = result.splice(startIndex, 1);
      result.splice(endIndex, 0, removed);

      return {
        providers: result.map((p, index) => ({
          ...p,
          priority: index + 1,
        })),
      };
    }),

  resetToDefaults: () =>
    set({
      providers: [],
      qualityConfig: createDefaultQualityConfig(),
      validationResult: null,
      error: null,
    }),
}));
