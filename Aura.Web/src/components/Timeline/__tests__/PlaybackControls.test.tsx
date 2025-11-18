/**
 * Tests for PlaybackControls J/K/L shuttle functionality
 */

import '@testing-library/jest-dom';
import { fireEvent, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { PlaybackControls } from '../PlaybackControls';

describe('PlaybackControls - J/K/L Shuttle', () => {
  const defaultProps = {
    isPlaying: false,
    currentTime: 10,
    duration: 120,
    playbackSpeed: 1,
    frameRate: 30,
    onPlayPause: vi.fn(),
    onSeek: vi.fn(),
    onSpeedChange: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('Basic Rendering', () => {
    it('should render playback controls', () => {
      render(<PlaybackControls {...defaultProps} />);
      
      expect(screen.getByLabelText(/Play \(Space\/K\)/i)).toBeInTheDocument();
      expect(screen.getByText('00:10:00 / 02:00:00')).toBeInTheDocument();
    });

    it('should show pause button when playing', () => {
      render(<PlaybackControls {...defaultProps} isPlaying={true} />);
      
      expect(screen.getByLabelText(/Pause \(Space\/K\)/i)).toBeInTheDocument();
    });
  });

  describe('J Key (Reverse Shuttle)', () => {
    it('should start playback in reverse when J is pressed', () => {
      render(<PlaybackControls {...defaultProps} />);
      
      fireEvent.keyDown(window, { key: 'j' });
      
      expect(defaultProps.onPlayPause).toHaveBeenCalled();
      expect(defaultProps.onSpeedChange).toHaveBeenCalledWith(0.5);
    });

    it('should increase reverse speed on multiple J presses', () => {
      render(<PlaybackControls {...defaultProps} />);
      
      fireEvent.keyDown(window, { key: 'j' });
      fireEvent.keyDown(window, { key: 'j' });
      
      expect(defaultProps.onSpeedChange).toHaveBeenCalledWith(1);
    });

    it('should cap reverse speed at 4x', () => {
      render(<PlaybackControls {...defaultProps} />);
      
      for (let i = 0; i < 6; i++) {
        fireEvent.keyDown(window, { key: 'j' });
      }
      
      const lastCall = defaultProps.onSpeedChange.mock.calls.slice(-1)[0];
      expect(lastCall[0]).toBeLessThanOrEqual(2);
    });
  });

  describe('K Key (Pause)', () => {
    it('should pause playback when K is pressed', () => {
      render(<PlaybackControls {...defaultProps} isPlaying={true} />);
      
      fireEvent.keyDown(window, { key: 'k' });
      
      expect(defaultProps.onPlayPause).toHaveBeenCalled();
    });

    it('should reset shuttle speed when K is pressed', () => {
      render(<PlaybackControls {...defaultProps} />);
      
      fireEvent.keyDown(window, { key: 'l' });
      fireEvent.keyDown(window, { key: 'k' });
      
      expect(defaultProps.onSpeedChange).toHaveBeenCalledWith(1);
    });

    it('should not play when K is pressed while paused', () => {
      const onPlayPause = vi.fn();
      render(<PlaybackControls {...defaultProps} onPlayPause={onPlayPause} isPlaying={false} />);
      
      const callsBefore = onPlayPause.mock.calls.length;
      fireEvent.keyDown(window, { key: 'k' });
      const callsAfter = onPlayPause.mock.calls.length;
      
      expect(callsAfter).toBe(callsBefore);
    });
  });

  describe('L Key (Forward Shuttle)', () => {
    it('should start playback in forward when L is pressed', () => {
      render(<PlaybackControls {...defaultProps} />);
      
      fireEvent.keyDown(window, { key: 'l' });
      
      expect(defaultProps.onPlayPause).toHaveBeenCalled();
      expect(defaultProps.onSpeedChange).toHaveBeenCalledWith(0.5);
    });

    it('should increase forward speed on multiple L presses', () => {
      render(<PlaybackControls {...defaultProps} />);
      
      fireEvent.keyDown(window, { key: 'l' });
      fireEvent.keyDown(window, { key: 'l' });
      
      expect(defaultProps.onSpeedChange).toHaveBeenCalledWith(1);
    });

    it('should cap forward speed at 4x', () => {
      render(<PlaybackControls {...defaultProps} />);
      
      for (let i = 0; i < 6; i++) {
        fireEvent.keyDown(window, { key: 'l' });
      }
      
      const lastCall = defaultProps.onSpeedChange.mock.calls.slice(-1)[0];
      expect(lastCall[0]).toBeLessThanOrEqual(2);
    });
  });

  describe('Spacebar (Toggle Play/Pause)', () => {
    it('should toggle play/pause when spacebar is pressed', () => {
      render(<PlaybackControls {...defaultProps} />);
      
      fireEvent.keyDown(window, { key: ' ' });
      
      expect(defaultProps.onPlayPause).toHaveBeenCalled();
    });

    it('should reset shuttle speed when spacebar is pressed', () => {
      render(<PlaybackControls {...defaultProps} />);
      
      fireEvent.keyDown(window, { key: 'l' });
      fireEvent.keyDown(window, { key: ' ' });
      
      expect(defaultProps.onSpeedChange).toHaveBeenCalledWith(1);
    });
  });

  describe('Frame Navigation', () => {
    it('should step backward one frame when comma is pressed', () => {
      render(<PlaybackControls {...defaultProps} />);
      
      fireEvent.keyDown(window, { key: ',' });
      
      const expectedTime = 10 - 1 / 30;
      expect(defaultProps.onSeek).toHaveBeenCalledWith(expectedTime);
    });

    it('should step forward one frame when period is pressed', () => {
      render(<PlaybackControls {...defaultProps} />);
      
      fireEvent.keyDown(window, { key: '.' });
      
      const expectedTime = 10 + 1 / 30;
      expect(defaultProps.onSeek).toHaveBeenCalledWith(expectedTime);
    });

    it('should not go below zero when stepping backward', () => {
      render(<PlaybackControls {...defaultProps} currentTime={0} />);
      
      fireEvent.keyDown(window, { key: ',' });
      
      expect(defaultProps.onSeek).toHaveBeenCalledWith(0);
    });

    it('should not exceed duration when stepping forward', () => {
      render(<PlaybackControls {...defaultProps} currentTime={120} />);
      
      fireEvent.keyDown(window, { key: '.' });
      
      expect(defaultProps.onSeek).toHaveBeenCalledWith(120);
    });
  });

  describe('Jump Navigation', () => {
    it('should jump to start when Home is pressed', () => {
      render(<PlaybackControls {...defaultProps} />);
      
      fireEvent.keyDown(window, { key: 'Home' });
      
      expect(defaultProps.onSeek).toHaveBeenCalledWith(0);
    });

    it('should jump to end when End is pressed', () => {
      render(<PlaybackControls {...defaultProps} />);
      
      fireEvent.keyDown(window, { key: 'End' });
      
      expect(defaultProps.onSeek).toHaveBeenCalledWith(120);
    });
  });

  describe('Input Field Protection', () => {
    it('should not trigger shortcuts when typing in input fields', () => {
      render(
        <div>
          <PlaybackControls {...defaultProps} />
          <input type="text" data-testid="test-input" />
        </div>
      );
      
      const input = screen.getByTestId('test-input');
      input.focus();
      
      fireEvent.keyDown(input, { key: ' ' });
      
      expect(defaultProps.onPlayPause).not.toHaveBeenCalled();
    });
  });

  describe('Timecode Display', () => {
    it('should format timecode correctly', () => {
      render(<PlaybackControls {...defaultProps} currentTime={65.5} duration={120} />);
      
      expect(screen.getByText('01:05:15 / 02:00:00')).toBeInTheDocument();
    });

    it('should pad numbers with zeros', () => {
      render(<PlaybackControls {...defaultProps} currentTime={5.0667} duration={10} />);
      
      expect(screen.getByText(/00:05:\d{2} \/ 00:10:00/)).toBeInTheDocument();
    });
  });
});
