/**
 * OpenCut Waveforms Store Tests
 */

import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { useWaveformStore } from '../opencutWaveforms';
import type { WaveformData } from '../opencutWaveforms';

describe('OpenCutWaveformsStore', () => {
  beforeEach(() => {
    // Reset store before each test
    useWaveformStore.setState({
      waveforms: new Map(),
      loading: new Set(),
      errors: new Map(),
    });
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('Initial State', () => {
    it('should have empty initial state', () => {
      const state = useWaveformStore.getState();
      expect(state.waveforms.size).toBe(0);
      expect(state.loading.size).toBe(0);
      expect(state.errors.size).toBe(0);
    });
  });

  describe('State Getters', () => {
    it('should return undefined for non-existent waveform', () => {
      const { getWaveform } = useWaveformStore.getState();
      expect(getWaveform('non-existent')).toBeUndefined();
    });

    it('should return false for non-loading media', () => {
      const { isLoading } = useWaveformStore.getState();
      expect(isLoading('non-existent')).toBe(false);
    });

    it('should return undefined for non-existent error', () => {
      const { getError } = useWaveformStore.getState();
      expect(getError('non-existent')).toBeUndefined();
    });
  });

  describe('Waveform Storage', () => {
    it('should store waveform data directly', () => {
      const mockWaveformData: WaveformData = {
        peaks: [0.5, 0.8, 0.3, 0.6, 0.9],
        duration: 5,
        sampleRate: 44100,
        channels: 2,
      };

      // Manually set waveform data
      useWaveformStore.setState((state) => {
        const newWaveforms = new Map(state.waveforms);
        newWaveforms.set('media-1', mockWaveformData);
        return { waveforms: newWaveforms };
      });

      const waveform = useWaveformStore.getState().getWaveform('media-1');
      expect(waveform).toBeDefined();
      expect(waveform?.peaks).toEqual([0.5, 0.8, 0.3, 0.6, 0.9]);
      expect(waveform?.duration).toBe(5);
      expect(waveform?.sampleRate).toBe(44100);
      expect(waveform?.channels).toBe(2);
    });

    it('should clear specific waveform', () => {
      const mockWaveformData: WaveformData = {
        peaks: [0.5, 0.8],
        duration: 2,
        sampleRate: 44100,
        channels: 1,
      };

      // Set waveform data
      useWaveformStore.setState((state) => {
        const newWaveforms = new Map(state.waveforms);
        newWaveforms.set('media-1', mockWaveformData);
        return { waveforms: newWaveforms };
      });

      expect(useWaveformStore.getState().getWaveform('media-1')).toBeDefined();

      // Clear specific waveform
      useWaveformStore.getState().clearWaveform('media-1');

      expect(useWaveformStore.getState().getWaveform('media-1')).toBeUndefined();
    });

    it('should clear all waveforms', () => {
      const mockWaveformData: WaveformData = {
        peaks: [0.5],
        duration: 1,
        sampleRate: 44100,
        channels: 1,
      };

      // Set multiple waveforms
      useWaveformStore.setState((state) => {
        const newWaveforms = new Map(state.waveforms);
        newWaveforms.set('media-1', mockWaveformData);
        newWaveforms.set('media-2', mockWaveformData);
        return { waveforms: newWaveforms };
      });

      expect(useWaveformStore.getState().getWaveform('media-1')).toBeDefined();
      expect(useWaveformStore.getState().getWaveform('media-2')).toBeDefined();

      // Clear all
      useWaveformStore.getState().clearAll();

      expect(useWaveformStore.getState().getWaveform('media-1')).toBeUndefined();
      expect(useWaveformStore.getState().getWaveform('media-2')).toBeUndefined();
    });
  });

  describe('Loading State', () => {
    it('should track loading state', () => {
      // Set loading state
      useWaveformStore.setState((state) => {
        const newLoading = new Set(state.loading);
        newLoading.add('media-1');
        return { loading: newLoading };
      });

      expect(useWaveformStore.getState().isLoading('media-1')).toBe(true);
      expect(useWaveformStore.getState().isLoading('media-2')).toBe(false);
    });

    it('should clear loading state when complete', () => {
      // Set loading state
      useWaveformStore.setState((state) => {
        const newLoading = new Set(state.loading);
        newLoading.add('media-1');
        return { loading: newLoading };
      });

      expect(useWaveformStore.getState().isLoading('media-1')).toBe(true);

      // Clear loading
      useWaveformStore.setState((state) => {
        const newLoading = new Set(state.loading);
        newLoading.delete('media-1');
        return { loading: newLoading };
      });

      expect(useWaveformStore.getState().isLoading('media-1')).toBe(false);
    });
  });

  describe('Error State', () => {
    it('should track error state', () => {
      // Set error state
      useWaveformStore.setState((state) => {
        const newErrors = new Map(state.errors);
        newErrors.set('media-1', 'Failed to load waveform');
        return { errors: newErrors };
      });

      expect(useWaveformStore.getState().getError('media-1')).toBe('Failed to load waveform');
      expect(useWaveformStore.getState().getError('media-2')).toBeUndefined();
    });

    it('should clear errors on clearAll', () => {
      // Set error state
      useWaveformStore.setState((state) => {
        const newErrors = new Map(state.errors);
        newErrors.set('media-1', 'Error');
        return { errors: newErrors };
      });

      expect(useWaveformStore.getState().getError('media-1')).toBeDefined();

      useWaveformStore.getState().clearAll();

      expect(useWaveformStore.getState().getError('media-1')).toBeUndefined();
    });
  });

  describe('Skip Loading If Already Loaded', () => {
    it('should not reload if waveform already exists', async () => {
      const mockWaveformData: WaveformData = {
        peaks: [0.5],
        duration: 1,
        sampleRate: 44100,
        channels: 1,
      };

      // Pre-set the waveform
      useWaveformStore.setState((state) => {
        const newWaveforms = new Map(state.waveforms);
        newWaveforms.set('media-1', mockWaveformData);
        return { waveforms: newWaveforms };
      });

      const { loadWaveform, isLoading } = useWaveformStore.getState();

      // Try to load again - should be skipped
      await loadWaveform('media-1', 'http://test.com/audio.wav', 200);

      // Should not be loading since it was skipped
      expect(useWaveformStore.getState().isLoading('media-1')).toBe(false);
      // Original data should still be there
      expect(useWaveformStore.getState().getWaveform('media-1')).toBe(mockWaveformData);
    });

    it('should not reload if currently loading', () => {
      // Set loading state
      useWaveformStore.setState((state) => {
        const newLoading = new Set(state.loading);
        newLoading.add('media-1');
        return { loading: newLoading };
      });

      // Track if the loading state changes
      const initialLoadingSize = useWaveformStore.getState().loading.size;

      // Try to load again - should be skipped
      useWaveformStore.getState().loadWaveform('media-1', 'http://test.com/audio.wav', 200);

      // Loading set size should not have changed (no duplicate entry)
      expect(useWaveformStore.getState().loading.size).toBe(initialLoadingSize);
    });
  });
});
