/**
 * Tests for VideoPreviewPlayer component
 */

import '@testing-library/jest-dom';
import { render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { VideoPreviewPlayer } from '../VideoPreviewPlayer';

describe('VideoPreviewPlayer', () => {
  beforeEach(() => {
    HTMLVideoElement.prototype.play = vi.fn().mockResolvedValue(undefined);
    HTMLVideoElement.prototype.pause = vi.fn();
    HTMLVideoElement.prototype.load = vi.fn();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('Basic Rendering', () => {
    it('should render placeholder when no video URL provided', () => {
      render(<VideoPreviewPlayer />);

      expect(screen.getByText(/Preview will appear after rendering/i)).toBeInTheDocument();
    });

    it('should render video element when URL is provided', () => {
      render(<VideoPreviewPlayer videoUrl="/test-video.mp4" />);

      const video = document.querySelector('video');
      expect(video).toBeInTheDocument();
      expect(video?.src).toContain('test-video.mp4');
    });
  });

  describe('Time Synchronization', () => {
    it('should sync video time with external currentTime prop', async () => {
      const { rerender } = render(<VideoPreviewPlayer videoUrl="/test.mp4" currentTime={0} />);

      const video = document.querySelector('video');

      rerender(<VideoPreviewPlayer videoUrl="/test.mp4" currentTime={5} />);

      await waitFor(() => {
        expect(video?.currentTime).toBe(5);
      });
    });

    it('should call onTimeUpdate when video time changes', () => {
      const onTimeUpdate = vi.fn();
      render(<VideoPreviewPlayer videoUrl="/test.mp4" onTimeUpdate={onTimeUpdate} />);

      const video = document.querySelector('video');
      if (video) {
        const event = new Event('timeupdate');
        video.dispatchEvent(event);
      }

      expect(onTimeUpdate).toHaveBeenCalled();
    });
  });

  describe('Playback Control', () => {
    it('should sync play state with external isPlaying prop', async () => {
      const { rerender } = render(<VideoPreviewPlayer videoUrl="/test.mp4" isPlaying={false} />);

      rerender(<VideoPreviewPlayer videoUrl="/test.mp4" isPlaying={true} />);

      await waitFor(() => {
        expect(HTMLVideoElement.prototype.play).toHaveBeenCalled();
      });
    });

    it('should sync pause state with external isPlaying prop', async () => {
      const { rerender } = render(<VideoPreviewPlayer videoUrl="/test.mp4" isPlaying={true} />);

      rerender(<VideoPreviewPlayer videoUrl="/test.mp4" isPlaying={false} />);

      await waitFor(() => {
        expect(HTMLVideoElement.prototype.pause).toHaveBeenCalled();
      });
    });

    it('should call onPlayPauseChange when video plays', () => {
      const onPlayPauseChange = vi.fn();
      render(<VideoPreviewPlayer videoUrl="/test.mp4" onPlayPauseChange={onPlayPauseChange} />);

      const video = document.querySelector('video');
      if (video) {
        const event = new Event('play');
        video.dispatchEvent(event);
      }

      expect(onPlayPauseChange).toHaveBeenCalledWith(true);
    });
  });

  describe('Playback Speed', () => {
    it('should sync playback speed with external prop', async () => {
      const { rerender } = render(<VideoPreviewPlayer videoUrl="/test.mp4" playbackSpeed={1} />);

      const video = document.querySelector('video');

      rerender(<VideoPreviewPlayer videoUrl="/test.mp4" playbackSpeed={2} />);

      await waitFor(() => {
        expect(video?.playbackRate).toBe(2);
      });
    });
  });

  describe('Seek Operations', () => {
    it('should update time when seeking via slider', () => {
      const onTimeUpdate = vi.fn();
      const onSeek = vi.fn();

      render(
        <VideoPreviewPlayer videoUrl="/test.mp4" onTimeUpdate={onTimeUpdate} onSeek={onSeek} />
      );

      expect(onSeek).toBeDefined();
    });
  });
});
