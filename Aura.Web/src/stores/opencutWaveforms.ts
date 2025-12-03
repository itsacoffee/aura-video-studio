/**
 * OpenCut Waveforms Store
 *
 * Manages waveform data for audio/video clips in the timeline.
 * Handles loading states, caching, and error handling.
 */

import { create } from 'zustand';
import { generateWaveformFromUrl, type WaveformPeaksData } from '../services/waveformService';

/**
 * Generate a cache key that includes both mediaId and samples
 */
function getCacheKey(mediaId: string, samples: number): string {
  return `${mediaId}:${samples}`;
}

interface WaveformState {
  waveforms: Map<string, WaveformPeaksData>;
  loading: Set<string>;
  errors: Map<string, string>;
}

interface WaveformActions {
  loadWaveform: (mediaId: string, audioUrl: string, samples?: number) => Promise<void>;
  getWaveform: (mediaId: string, samples?: number) => WaveformPeaksData | undefined;
  isLoading: (mediaId: string, samples?: number) => boolean;
  getError: (mediaId: string, samples?: number) => string | undefined;
  clearWaveform: (mediaId: string) => void;
  clearAll: () => void;
}

export type OpenCutWaveformsStore = WaveformState & WaveformActions;

export const useWaveformStore = create<OpenCutWaveformsStore>((set, get) => ({
  waveforms: new Map(),
  loading: new Set(),
  errors: new Map(),

  loadWaveform: async (mediaId, audioUrl, samples = 200) => {
    const cacheKey = getCacheKey(mediaId, samples);

    // Don't reload if already loaded or loading
    if (get().waveforms.has(cacheKey) || get().loading.has(cacheKey)) return;

    // Mark as loading
    set((state) => {
      const newLoading = new Set(state.loading);
      newLoading.add(cacheKey);
      const newErrors = new Map(state.errors);
      newErrors.delete(cacheKey);
      return {
        loading: newLoading,
        errors: newErrors,
      };
    });

    try {
      const waveformData = await generateWaveformFromUrl(audioUrl, {
        samples,
        normalize: true,
      });

      set((state) => {
        const newWaveforms = new Map(state.waveforms);
        newWaveforms.set(cacheKey, waveformData);
        const newLoading = new Set(state.loading);
        newLoading.delete(cacheKey);
        return {
          waveforms: newWaveforms,
          loading: newLoading,
        };
      });
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to load waveform';
      set((state) => {
        const newErrors = new Map(state.errors);
        newErrors.set(cacheKey, errorMessage);
        const newLoading = new Set(state.loading);
        newLoading.delete(cacheKey);
        return {
          errors: newErrors,
          loading: newLoading,
        };
      });
    }
  },

  getWaveform: (mediaId, samples = 200) => {
    const cacheKey = getCacheKey(mediaId, samples);
    return get().waveforms.get(cacheKey);
  },

  isLoading: (mediaId, samples = 200) => {
    const cacheKey = getCacheKey(mediaId, samples);
    return get().loading.has(cacheKey);
  },

  getError: (mediaId, samples = 200) => {
    const cacheKey = getCacheKey(mediaId, samples);
    return get().errors.get(cacheKey);
  },

  clearWaveform: (mediaId) => {
    set((state) => {
      const newWaveforms = new Map(state.waveforms);
      const newLoading = new Set(state.loading);
      const newErrors = new Map(state.errors);

      // Clear all entries that match this mediaId (any samples value)
      for (const key of newWaveforms.keys()) {
        if (key.startsWith(`${mediaId}:`)) {
          newWaveforms.delete(key);
        }
      }
      for (const key of newLoading) {
        if (key.startsWith(`${mediaId}:`)) {
          newLoading.delete(key);
        }
      }
      for (const key of newErrors.keys()) {
        if (key.startsWith(`${mediaId}:`)) {
          newErrors.delete(key);
        }
      }

      return {
        waveforms: newWaveforms,
        loading: newLoading,
        errors: newErrors,
      };
    });
  },

  clearAll: () =>
    set({
      waveforms: new Map(),
      loading: new Set(),
      errors: new Map(),
    }),
}));
