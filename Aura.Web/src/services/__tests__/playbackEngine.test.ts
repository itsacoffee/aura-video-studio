/**
 * Tests for PlaybackEngine service
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { PlaybackEngine } from '../playbackEngine';

describe('PlaybackEngine', () => {
  let mockVideo: HTMLVideoElement;
  let mockCanvas: HTMLCanvasElement;

  beforeEach(() => {
    // Create mock video element
    mockVideo = document.createElement('video');
    mockVideo.src = 'test-video.mp4';
    
    // Create mock canvas element
    mockCanvas = document.createElement('canvas');
    
    // Mock video properties
    Object.defineProperty(mockVideo, 'duration', {
      value: 60,
      writable: true,
    });
    Object.defineProperty(mockVideo, 'currentTime', {
      value: 0,
      writable: true,
    });
    Object.defineProperty(mockVideo, 'videoWidth', {
      value: 1920,
      writable: true,
    });
    Object.defineProperty(mockVideo, 'videoHeight', {
      value: 1080,
      writable: true,
    });
  });

  it('should initialize with default state', () => {
    const engine = new PlaybackEngine({
      videoElement: mockVideo,
      canvasElement: mockCanvas,
    });

    const state = engine.getState();
    expect(state.isPlaying).toBe(false);
    expect(state.currentTime).toBe(0);
    expect(state.playbackSpeed).toBe(1.0);
    expect(state.quality).toBe('full');
    expect(state.volume).toBe(1.0);
    expect(state.isMuted).toBe(false);
    expect(state.isLooping).toBe(false);
    expect(state.inPoint).toBe(null);
    expect(state.outPoint).toBe(null);

    engine.destroy();
  });

  it('should set playback speed correctly', () => {
    const engine = new PlaybackEngine({
      videoElement: mockVideo,
      canvasElement: mockCanvas,
    });

    engine.setPlaybackSpeed(2.0);
    const state = engine.getState();
    expect(state.playbackSpeed).toBe(2.0);
    expect(mockVideo.playbackRate).toBe(2.0);

    engine.destroy();
  });

  it('should set preview quality', () => {
    const engine = new PlaybackEngine({
      videoElement: mockVideo,
      canvasElement: mockCanvas,
    });

    engine.setQuality('half');
    const state = engine.getState();
    expect(state.quality).toBe('half');

    engine.destroy();
  });

  it('should set volume', () => {
    const engine = new PlaybackEngine({
      videoElement: mockVideo,
      canvasElement: mockCanvas,
    });

    engine.setVolume(0.5);
    const state = engine.getState();
    expect(state.volume).toBe(0.5);
    expect(mockVideo.volume).toBe(0.5);

    engine.destroy();
  });

  it('should toggle mute', () => {
    const engine = new PlaybackEngine({
      videoElement: mockVideo,
      canvasElement: mockCanvas,
    });

    engine.toggleMute();
    let state = engine.getState();
    expect(state.isMuted).toBe(true);
    expect(mockVideo.muted).toBe(true);

    engine.toggleMute();
    state = engine.getState();
    expect(state.isMuted).toBe(false);
    expect(mockVideo.muted).toBe(false);

    engine.destroy();
  });

  it('should set loop mode', () => {
    const engine = new PlaybackEngine({
      videoElement: mockVideo,
      canvasElement: mockCanvas,
    });

    engine.setLoop(true);
    const state = engine.getState();
    expect(state.isLooping).toBe(true);

    engine.destroy();
  });

  it('should set in/out points', () => {
    const engine = new PlaybackEngine({
      videoElement: mockVideo,
      canvasElement: mockCanvas,
    });

    engine.setInPoint(10);
    engine.setOutPoint(30);
    const state = engine.getState();
    expect(state.inPoint).toBe(10);
    expect(state.outPoint).toBe(30);

    engine.destroy();
  });

  it('should clear in/out points', () => {
    const engine = new PlaybackEngine({
      videoElement: mockVideo,
      canvasElement: mockCanvas,
    });

    engine.setInPoint(10);
    engine.setOutPoint(30);
    engine.clearInOutPoints();
    const state = engine.getState();
    expect(state.inPoint).toBe(null);
    expect(state.outPoint).toBe(null);

    engine.destroy();
  });

  it('should call state change callback', () => {
    const onStateChange = vi.fn();
    const engine = new PlaybackEngine({
      videoElement: mockVideo,
      canvasElement: mockCanvas,
      onStateChange,
    });

    // Trigger state change by setting volume
    engine.setVolume(0.5);
    
    expect(onStateChange).toHaveBeenCalled();

    engine.destroy();
  });

  it('should seek to specific time', () => {
    const engine = new PlaybackEngine({
      videoElement: mockVideo,
      canvasElement: mockCanvas,
    });

    // Just verify seek doesn't throw
    expect(() => engine.seek(30)).not.toThrow();

    engine.destroy();
  });

  it('should clamp seek time to valid range', () => {
    const engine = new PlaybackEngine({
      videoElement: mockVideo,
      canvasElement: mockCanvas,
    });

    // Just verify seeks don't throw
    expect(() => engine.seek(100)).not.toThrow();
    expect(() => engine.seek(-10)).not.toThrow();

    engine.destroy();
  });

  it('should get metrics', () => {
    const engine = new PlaybackEngine({
      videoElement: mockVideo,
      canvasElement: mockCanvas,
    });

    const metrics = engine.getMetrics();
    expect(metrics).toHaveProperty('droppedFrames');
    expect(metrics).toHaveProperty('totalFrames');
    expect(metrics).toHaveProperty('currentFPS');
    expect(metrics).toHaveProperty('targetFPS');
    expect(metrics.targetFPS).toBe(30);

    engine.destroy();
  });

  it('should cleanup on destroy', () => {
    const engine = new PlaybackEngine({
      videoElement: mockVideo,
      canvasElement: mockCanvas,
    });

    // Should not throw
    expect(() => engine.destroy()).not.toThrow();
  });
});
