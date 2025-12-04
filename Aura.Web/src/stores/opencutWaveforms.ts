/**
 * OpenCut Waveforms Store
 *
 * Manages waveform data for audio and video clips in the OpenCut timeline.
 * Provides caching, loading state, and error handling for waveform generation.
 */

import { create } from 'zustand';

/**
 * Waveform data containing peaks for visualization
 */
export interface WaveformData {
  peaks: number[];
  duration: number;
  sampleRate: number;
  channels: number;
}

/**
 * Options for waveform generation
 */
export interface WaveformOptions {
  samples: number;
  channel?: number;
  normalize?: boolean;
}

/** Internal cache for waveform data */
const waveformCache = new Map<string, WaveformData>();

/**
 * Generate waveform data from an audio URL using the Web Audio API
 */
export async function generateWaveform(
  audioUrl: string,
  options: WaveformOptions = { samples: 200 }
): Promise<WaveformData> {
  const cacheKey = `${audioUrl}-${options.samples}`;
  const cached = waveformCache.get(cacheKey);
  if (cached) {
    return cached;
  }

  const audioContext = new AudioContext();

  try {
    const response = await fetch(audioUrl);
    const arrayBuffer = await response.arrayBuffer();
    const audioBuffer = await audioContext.decodeAudioData(arrayBuffer);

    const channelData = audioBuffer.getChannelData(options.channel ?? 0);
    const samplesPerPeak = Math.floor(channelData.length / options.samples);
    const peaks: number[] = [];

    for (let i = 0; i < options.samples; i++) {
      const start = i * samplesPerPeak;
      const end = start + samplesPerPeak;
      let max = 0;

      for (let j = start; j < end && j < channelData.length; j++) {
        const abs = Math.abs(channelData[j]);
        if (abs > max) max = abs;
      }

      peaks.push(max);
    }

    // Normalize if requested
    if (options.normalize) {
      const maxPeak = Math.max(...peaks);
      if (maxPeak > 0) {
        for (let i = 0; i < peaks.length; i++) {
          peaks[i] = peaks[i] / maxPeak;
        }
      }
    }

    const waveformData: WaveformData = {
      peaks,
      duration: audioBuffer.duration,
      sampleRate: audioBuffer.sampleRate,
      channels: audioBuffer.numberOfChannels,
    };

    waveformCache.set(cacheKey, waveformData);
    return waveformData;
  } finally {
    await audioContext.close();
  }
}

/**
 * Clear waveform cache for a specific URL or all URLs
 */
export function clearWaveformCache(audioUrl?: string): void {
  if (audioUrl) {
    for (const key of waveformCache.keys()) {
      if (key.startsWith(audioUrl)) {
        waveformCache.delete(key);
      }
    }
  } else {
    waveformCache.clear();
  }
}

/**
 * Get waveform data from cache without generating
 */
export function getWaveformFromCache(audioUrl: string, samples: number): WaveformData | undefined {
  return waveformCache.get(`${audioUrl}-${samples}`);
}

/**
 * Waveform store state
 */
interface WaveformState {
  waveforms: Map<string, WaveformData>;
  loading: Set<string>;
  errors: Map<string, string>;
}

/**
 * Waveform store actions
 */
interface WaveformActions {
  loadWaveform: (mediaId: string, audioUrl: string, samples?: number) => Promise<void>;
  getWaveform: (mediaId: string) => WaveformData | undefined;
  isLoading: (mediaId: string) => boolean;
  getError: (mediaId: string) => string | undefined;
  clearWaveform: (mediaId: string) => void;
  clearAll: () => void;
}

export type WaveformStore = WaveformState & WaveformActions;

/**
 * OpenCut waveform store for managing waveform state
 */
export const useWaveformStore = create<WaveformStore>((set, get) => ({
  waveforms: new Map(),
  loading: new Set(),
  errors: new Map(),

  loadWaveform: async (mediaId, audioUrl, samples = 200) => {
    // Skip if already loaded or loading
    if (get().waveforms.has(mediaId) || get().loading.has(mediaId)) {
      return;
    }

    // Mark as loading
    set((state) => {
      const newLoading = new Set(state.loading);
      newLoading.add(mediaId);
      const newErrors = new Map(state.errors);
      newErrors.delete(mediaId);
      return { loading: newLoading, errors: newErrors };
    });

    try {
      const waveformData = await generateWaveform(audioUrl, { samples, normalize: true });

      set((state) => {
        const newWaveforms = new Map(state.waveforms);
        newWaveforms.set(mediaId, waveformData);
        const newLoading = new Set(state.loading);
        newLoading.delete(mediaId);
        return { waveforms: newWaveforms, loading: newLoading };
      });
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to load waveform';

      set((state) => {
        const newErrors = new Map(state.errors);
        newErrors.set(mediaId, errorMessage);
        const newLoading = new Set(state.loading);
        newLoading.delete(mediaId);
        return { errors: newErrors, loading: newLoading };
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
      return { waveforms: newWaveforms };
    });
  },

  clearAll: () => {
    clearWaveformCache();
    set({ waveforms: new Map(), loading: new Set(), errors: new Map() });
  },
}));
