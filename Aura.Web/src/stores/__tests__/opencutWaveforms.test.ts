/**
 * OpenCut Waveforms Store Tests
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { useWaveformStore } from '../opencutWaveforms';

// Mock the waveformService module
vi.mock('../../services/waveformService', () => ({
  generateWaveformFromUrl: vi.fn(),
}));

import { generateWaveformFromUrl } from '../../services/waveformService';

const mockGenerateWaveform = vi.mocked(generateWaveformFromUrl);

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

  it('should start with empty state', () => {
    const state = useWaveformStore.getState();

    expect(state.waveforms.size).toBe(0);
    expect(state.loading.size).toBe(0);
    expect(state.errors.size).toBe(0);
  });

  it('should load waveform successfully', async () => {
    const mockWaveformData = {
      peaks: [0.5, 0.8, 0.3, 0.9],
      duration: 10,
      sampleRate: 44100,
      channels: 2,
    };

    mockGenerateWaveform.mockResolvedValueOnce(mockWaveformData);

    await useWaveformStore.getState().loadWaveform('media-1', 'http://example.com/audio.mp3', 100);

    const state = useWaveformStore.getState();
    expect(state.getWaveform('media-1', 100)).toEqual(mockWaveformData);
    expect(state.isLoading('media-1', 100)).toBe(false);
    expect(state.getError('media-1', 100)).toBeUndefined();
  });

  it('should handle loading error', async () => {
    mockGenerateWaveform.mockRejectedValueOnce(new Error('Failed to fetch audio'));

    await useWaveformStore.getState().loadWaveform('media-2', 'http://example.com/audio.mp3');

    const state = useWaveformStore.getState();
    expect(state.getWaveform('media-2')).toBeUndefined();
    expect(state.isLoading('media-2')).toBe(false);
    expect(state.getError('media-2')).toBe('Failed to fetch audio');
  });

  it('should not reload if already loaded', async () => {
    const mockWaveformData = {
      peaks: [0.5, 0.8],
      duration: 5,
      sampleRate: 44100,
      channels: 1,
    };

    mockGenerateWaveform.mockResolvedValueOnce(mockWaveformData);

    await useWaveformStore.getState().loadWaveform('media-3', 'http://example.com/audio.mp3');
    await useWaveformStore.getState().loadWaveform('media-3', 'http://example.com/audio.mp3');

    // Should only be called once
    expect(mockGenerateWaveform).toHaveBeenCalledTimes(1);
  });

  it('should load different waveforms for different samples values', async () => {
    const mockWaveformData100 = {
      peaks: [0.5, 0.8],
      duration: 5,
      sampleRate: 44100,
      channels: 1,
    };
    const mockWaveformData500 = {
      peaks: [0.3, 0.6, 0.9, 0.4, 0.7],
      duration: 5,
      sampleRate: 44100,
      channels: 1,
    };

    mockGenerateWaveform.mockResolvedValueOnce(mockWaveformData100);
    mockGenerateWaveform.mockResolvedValueOnce(mockWaveformData500);

    await useWaveformStore
      .getState()
      .loadWaveform('media-diff', 'http://example.com/audio.mp3', 100);
    await useWaveformStore
      .getState()
      .loadWaveform('media-diff', 'http://example.com/audio.mp3', 500);

    // Should be called twice for different samples values
    expect(mockGenerateWaveform).toHaveBeenCalledTimes(2);
    expect(useWaveformStore.getState().getWaveform('media-diff', 100)).toEqual(mockWaveformData100);
    expect(useWaveformStore.getState().getWaveform('media-diff', 500)).toEqual(mockWaveformData500);
  });

  it('should return undefined for non-existent waveform', () => {
    const result = useWaveformStore.getState().getWaveform('non-existent');
    expect(result).toBeUndefined();
  });

  it('should track loading state correctly', async () => {
    const mockWaveformData = {
      peaks: [0.5],
      duration: 1,
      sampleRate: 44100,
      channels: 1,
    };

    let resolvePromise: ((value: typeof mockWaveformData) => void) | undefined;
    const pendingPromise = new Promise<typeof mockWaveformData>((resolve) => {
      resolvePromise = resolve;
    });

    mockGenerateWaveform.mockReturnValueOnce(pendingPromise);

    // Start loading
    const loadPromise = useWaveformStore
      .getState()
      .loadWaveform('media-4', 'http://example.com/audio.mp3');

    // Check loading state
    expect(useWaveformStore.getState().isLoading('media-4')).toBe(true);

    // Resolve the promise
    resolvePromise?.(mockWaveformData);
    await loadPromise;

    // Check loading is complete
    expect(useWaveformStore.getState().isLoading('media-4')).toBe(false);
  });

  it('should clear specific waveform', async () => {
    const mockWaveformData = {
      peaks: [0.5],
      duration: 1,
      sampleRate: 44100,
      channels: 1,
    };

    mockGenerateWaveform.mockResolvedValueOnce(mockWaveformData);

    await useWaveformStore.getState().loadWaveform('media-5', 'http://example.com/audio.mp3');
    expect(useWaveformStore.getState().getWaveform('media-5')).toEqual(mockWaveformData);

    useWaveformStore.getState().clearWaveform('media-5');
    expect(useWaveformStore.getState().getWaveform('media-5')).toBeUndefined();
  });

  it('should clear all waveforms', async () => {
    const mockWaveformData = {
      peaks: [0.5],
      duration: 1,
      sampleRate: 44100,
      channels: 1,
    };

    mockGenerateWaveform.mockResolvedValue(mockWaveformData);

    await useWaveformStore.getState().loadWaveform('media-a', 'http://example.com/a.mp3');
    await useWaveformStore.getState().loadWaveform('media-b', 'http://example.com/b.mp3');

    expect(useWaveformStore.getState().waveforms.size).toBe(2);

    useWaveformStore.getState().clearAll();

    expect(useWaveformStore.getState().waveforms.size).toBe(0);
    expect(useWaveformStore.getState().loading.size).toBe(0);
    expect(useWaveformStore.getState().errors.size).toBe(0);
  });
});
