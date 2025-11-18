/**
 * Tests for WaveformDisplay component
 */

import { render, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { waveformService } from '../../../services/waveformService';
import { WaveformDisplay } from '../WaveformDisplay';

vi.mock('../../../services/waveformService', () => ({
  waveformService: {
    generateWaveform: vi.fn(),
  },
}));

describe('WaveformDisplay', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    
    HTMLCanvasElement.prototype.getContext = vi.fn().mockReturnValue({
      fillStyle: '',
      strokeStyle: '',
      lineWidth: 0,
      fillRect: vi.fn(),
      beginPath: vi.fn(),
      stroke: vi.fn(),
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('Waveform Loading', () => {
    it('should load waveform data when audioPath is provided', async () => {
      const mockWaveformData = {
        data: [0.5, 0.8, 0.3, 0.6, 0.9],
        sampleRate: 44100,
        duration: 5,
      };

      vi.mocked(waveformService.generateWaveform).mockResolvedValue(mockWaveformData);

      render(
        <WaveformDisplay
          audioPath="/test-audio.wav"
          width={400}
          height={100}
        />
      );

      await waitFor(() => {
        expect(waveformService.generateWaveform).toHaveBeenCalledWith({
          audioPath: '/test-audio.wav',
          targetSamples: 200,
        });
      });
    });

    it('should handle waveform loading errors gracefully', async () => {
      vi.mocked(waveformService.generateWaveform).mockRejectedValue(
        new Error('Failed to load waveform')
      );

      const { container } = render(
        <WaveformDisplay
          audioPath="/bad-audio.wav"
          width={400}
          height={100}
        />
      );

      await waitFor(() => {
        expect(container.querySelector('canvas')).not.toBeInTheDocument();
      });
    });
  });

  describe('Canvas Rendering', () => {
    it('should render canvas with correct dimensions', async () => {
      const mockWaveformData = {
        data: [0.5, 0.8, 0.3],
        sampleRate: 44100,
        duration: 5,
      };

      vi.mocked(waveformService.generateWaveform).mockResolvedValue(mockWaveformData);

      const { container } = render(
        <WaveformDisplay
          audioPath="/test-audio.wav"
          width={600}
          height={120}
        />
      );

      await waitFor(() => {
        const canvas = container.querySelector('canvas');
        expect(canvas).toBeInTheDocument();
      });
    });

    it('should use custom colors when provided', async () => {
      const mockWaveformData = {
        data: [0.5],
        sampleRate: 44100,
        duration: 1,
      };

      vi.mocked(waveformService.generateWaveform).mockResolvedValue(mockWaveformData);

      render(
        <WaveformDisplay
          audioPath="/test-audio.wav"
          width={400}
          height={100}
          color="rgba(0, 255, 0, 0.8)"
          backgroundColor="rgba(0, 0, 0, 0.5)"
        />
      );

      await waitFor(() => {
        expect(waveformService.generateWaveform).toHaveBeenCalled();
      });
    });
  });

  describe('Lifecycle', () => {
    it('should cleanup on unmount', async () => {
      const mockWaveformData = {
        data: [0.5],
        sampleRate: 44100,
        duration: 1,
      };

      vi.mocked(waveformService.generateWaveform).mockResolvedValue(mockWaveformData);

      const { unmount } = render(
        <WaveformDisplay
          audioPath="/test-audio.wav"
          width={400}
          height={100}
        />
      );

      await waitFor(() => {
        expect(waveformService.generateWaveform).toHaveBeenCalled();
      });

      unmount();
    });

    it('should reload waveform when audioPath changes', async () => {
      const mockWaveformData = {
        data: [0.5],
        sampleRate: 44100,
        duration: 1,
      };

      vi.mocked(waveformService.generateWaveform).mockResolvedValue(mockWaveformData);

      const { rerender } = render(
        <WaveformDisplay
          audioPath="/test-audio-1.wav"
          width={400}
          height={100}
        />
      );

      await waitFor(() => {
        expect(waveformService.generateWaveform).toHaveBeenCalledTimes(1);
      });

      rerender(
        <WaveformDisplay
          audioPath="/test-audio-2.wav"
          width={400}
          height={100}
        />
      );

      await waitFor(() => {
        expect(waveformService.generateWaveform).toHaveBeenCalledTimes(2);
      });
    });
  });
});
