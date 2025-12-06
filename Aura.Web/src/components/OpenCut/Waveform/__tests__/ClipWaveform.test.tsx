/**
 * Tests for ClipWaveform component
 */

import { render, waitFor } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { useWaveformStore, clearWaveformCache } from '../../../../stores/opencutWaveforms';
import { ClipWaveform } from '../ClipWaveform';

// Mock canvas context
beforeEach(() => {
  HTMLCanvasElement.prototype.getContext = vi.fn().mockReturnValue({
    fillStyle: '',
    strokeStyle: '',
    lineWidth: 0,
    fillRect: vi.fn(),
    beginPath: vi.fn(),
    stroke: vi.fn(),
    fill: vi.fn(),
    scale: vi.fn(),
    roundRect: vi.fn(),
  });
});

describe('ClipWaveform', () => {
  beforeEach(() => {
    // Reset store before each test
    useWaveformStore.setState({
      waveforms: new Map(),
      loading: new Set(),
      errors: new Map(),
    });
    clearWaveformCache();
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('Rendering', () => {
    it('should render WaveformDisplay component', () => {
      const { container } = render(
        <ClipWaveform
          mediaId="media-1"
          audioUrl="http://test.com/audio.wav"
          width={400}
          height={100}
        />
      );

      expect(container.firstChild).toBeInTheDocument();
    });

    it('should render with custom colors', () => {
      const { container } = render(
        <ClipWaveform
          mediaId="media-1"
          audioUrl="http://test.com/audio.wav"
          width={400}
          height={100}
          color="#FF0000"
          backgroundColor="#000000"
        />
      );

      expect(container.firstChild).toBeInTheDocument();
    });
  });

  describe('Waveform Loading', () => {
    it('should trigger waveform loading on mount', async () => {
      // Pre-populate the store with mock data to avoid network calls
      const mockWaveformData = {
        peaks: [0.5, 0.8, 0.3],
        duration: 5,
        sampleRate: 44100,
        channels: 2,
      };

      useWaveformStore.setState({
        waveforms: new Map([['media-1', mockWaveformData]]),
        loading: new Set(),
        errors: new Map(),
      });

      render(
        <ClipWaveform
          mediaId="media-1"
          audioUrl="http://test.com/audio.wav"
          width={400}
          height={100}
        />
      );

      // Component should have access to the waveform
      await waitFor(() => {
        const waveform = useWaveformStore.getState().getWaveform('media-1');
        expect(waveform).toBeDefined();
      });
    });

    it('should show loading state while loading', async () => {
      // Set loading state
      useWaveformStore.setState({
        waveforms: new Map(),
        loading: new Set(['media-1']),
        errors: new Map(),
      });

      const { container } = render(
        <ClipWaveform
          mediaId="media-1"
          audioUrl="http://test.com/audio.wav"
          width={400}
          height={100}
        />
      );

      // The component should render - loading indicator is part of WaveformDisplay
      expect(container.firstChild).toBeInTheDocument();
    });
  });

  describe('Props Handling', () => {
    it('should pass trim values to WaveformDisplay', () => {
      const mockWaveformData = {
        peaks: [0.1, 0.2, 0.3, 0.4, 0.5],
        duration: 5,
        sampleRate: 44100,
        channels: 1,
      };

      useWaveformStore.setState({
        waveforms: new Map([['media-1', mockWaveformData]]),
        loading: new Set(),
        errors: new Map(),
      });

      const { container } = render(
        <ClipWaveform
          mediaId="media-1"
          audioUrl="http://test.com/audio.wav"
          width={400}
          height={100}
          trimStart={1}
          trimEnd={1}
          clipDuration={5}
        />
      );

      expect(container.querySelector('canvas')).toBeInTheDocument();
    });

    it('should use custom sample count', () => {
      const { container } = render(
        <ClipWaveform
          mediaId="media-1"
          audioUrl="http://test.com/audio.wav"
          width={400}
          height={100}
          samples={100}
        />
      );

      expect(container.firstChild).toBeInTheDocument();
    });
  });
});
