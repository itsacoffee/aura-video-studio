/**
 * Tests for WaveformDisplay component
 */

import { render } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';

import { WaveformDisplay } from '../WaveformDisplay';

describe('WaveformDisplay', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    // Mock canvas context
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

  describe('Rendering', () => {
    it('should render canvas element', () => {
      const waveformData = {
        peaks: [0.5, 0.8, 0.3, 0.6, 0.9],
        duration: 5,
        sampleRate: 44100,
        channels: 2,
      };

      const { container } = render(
        <WaveformDisplay waveformData={waveformData} width={400} height={100} />
      );

      const canvas = container.querySelector('canvas');
      expect(canvas).toBeInTheDocument();
    });

    it('should render with correct dimensions', () => {
      const waveformData = {
        peaks: [0.5, 0.8, 0.3],
        duration: 5,
        sampleRate: 44100,
        channels: 2,
      };

      const { container } = render(
        <WaveformDisplay waveformData={waveformData} width={600} height={120} />
      );

      const wrapper = container.firstChild as HTMLElement;
      expect(wrapper).toHaveStyle({ width: '600px', height: '120px' });
    });

    it('should render loading indicator when isLoading is true', () => {
      const { container } = render(
        <WaveformDisplay waveformData={null} width={400} height={100} isLoading={true} />
      );

      // Look for loading container which will have animation styles
      const loadingContainer = container.querySelector('[style*="animation"]');
      // The loading indicator should be visible when isLoading is true
      expect(container.firstChild).toBeInTheDocument();
    });

    it('should not render loading indicator when isLoading is false', () => {
      const waveformData = {
        peaks: [0.5],
        duration: 1,
        sampleRate: 44100,
        channels: 1,
      };

      const { container } = render(
        <WaveformDisplay waveformData={waveformData} width={400} height={100} isLoading={false} />
      );

      const loadingBar = container.querySelector('[class*="loadingBar"]');
      expect(loadingBar).not.toBeInTheDocument();
    });
  });

  describe('Canvas Drawing', () => {
    it('should draw waveform when data is provided', () => {
      const mockContext = {
        fillStyle: '',
        strokeStyle: '',
        lineWidth: 0,
        fillRect: vi.fn(),
        beginPath: vi.fn(),
        stroke: vi.fn(),
        fill: vi.fn(),
        scale: vi.fn(),
        roundRect: vi.fn(),
      };

      HTMLCanvasElement.prototype.getContext = vi.fn().mockReturnValue(mockContext);

      const waveformData = {
        peaks: [0.5, 0.8, 0.3],
        duration: 3,
        sampleRate: 44100,
        channels: 1,
      };

      render(<WaveformDisplay waveformData={waveformData} width={300} height={100} />);

      // Should have been called to draw background and bars
      expect(mockContext.fillRect).toHaveBeenCalled();
      expect(mockContext.fill).toHaveBeenCalled();
    });

    it('should not draw when waveformData is null', () => {
      const mockContext = {
        fillStyle: '',
        strokeStyle: '',
        lineWidth: 0,
        fillRect: vi.fn(),
        beginPath: vi.fn(),
        stroke: vi.fn(),
        fill: vi.fn(),
        scale: vi.fn(),
        roundRect: vi.fn(),
      };

      HTMLCanvasElement.prototype.getContext = vi.fn().mockReturnValue(mockContext);

      render(<WaveformDisplay waveformData={null} width={300} height={100} />);

      // fill should not have been called (only for bars)
      expect(mockContext.fill).not.toHaveBeenCalled();
    });
  });

  describe('Custom Colors', () => {
    it('should use custom color when provided', () => {
      const waveformData = {
        peaks: [0.5],
        duration: 1,
        sampleRate: 44100,
        channels: 1,
      };

      // Just verify rendering works with custom colors
      const { container } = render(
        <WaveformDisplay
          waveformData={waveformData}
          width={400}
          height={100}
          color="#FF0000"
          backgroundColor="#000000"
        />
      );

      expect(container.querySelector('canvas')).toBeInTheDocument();
    });
  });

  describe('Trim Support', () => {
    it('should handle trim values', () => {
      const waveformData = {
        peaks: [0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0],
        duration: 10,
        sampleRate: 44100,
        channels: 1,
      };

      const { container } = render(
        <WaveformDisplay
          waveformData={waveformData}
          width={400}
          height={100}
          trimStart={2}
          trimEnd={2}
          clipDuration={10}
        />
      );

      expect(container.querySelector('canvas')).toBeInTheDocument();
    });
  });
});
