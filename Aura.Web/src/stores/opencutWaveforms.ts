/**
 * OpenCut Waveforms Store
 *
 * Manages waveform data for audio/video clips in the timeline.
 * Handles loading states, caching, and error handling.
 */

import { create } from 'zustand';
import { generateWaveformFromUrl, type WaveformPeaksData } from '../services/waveformService';

interface WaveformState {
  waveforms: Map<string, WaveformPeaksData>;
  loading: Set<string>;
  errors: Map<string, string>;
}

interface WaveformActions {
  loadWaveform: (mediaId: string, audioUrl: string, samples?: number) => Promise<void>;
  getWaveform: (mediaId: string) => WaveformPeaksData | undefined;
  isLoading: (mediaId: string) => boolean;
  getError: (mediaId: string) => string | undefined;
  clearWaveform: (mediaId: string) => void;
  clearAll: () => void;
}

export type OpenCutWaveformsStore = WaveformState & WaveformActions;

export const useWaveformStore = create<OpenCutWaveformsStore>((set, get) => ({
  waveforms: new Map(),
  loading: new Set(),
  errors: new Map(),

  loadWaveform: async (mediaId, audioUrl, samples = 200) => {
    // Don't reload if already loaded or loading
    if (get().waveforms.has(mediaId) || get().loading.has(mediaId)) return;

    // Mark as loading
    set((state) => {
      const newLoading = new Set(state.loading);
      newLoading.add(mediaId);
      const newErrors = new Map(state.errors);
      newErrors.delete(mediaId);
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
        newWaveforms.set(mediaId, waveformData);
        const newLoading = new Set(state.loading);
        newLoading.delete(mediaId);
        return {
          waveforms: newWaveforms,
          loading: newLoading,
        };
      });
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to load waveform';
      set((state) => {
        const newErrors = new Map(state.errors);
        newErrors.set(mediaId, errorMessage);
        const newLoading = new Set(state.loading);
        newLoading.delete(mediaId);
        return {
          errors: newErrors,
          loading: newLoading,
        };
      });
    }
  },

  getWaveform: (mediaId) => get().waveforms.get(mediaId),

  isLoading: (mediaId) => get().loading.has(mediaId),

  getError: (mediaId) => get().errors.get(mediaId),

  clearWaveform: (mediaId) => {
    set((state) => {
      const newWaveforms = new Map(state.waveforms);
      newWaveforms.delete(mediaId);
      const newLoading = new Set(state.loading);
      newLoading.delete(mediaId);
      const newErrors = new Map(state.errors);
      newErrors.delete(mediaId);
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
