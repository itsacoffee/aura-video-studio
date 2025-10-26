/**
 * Tests for AudioSyncService
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { AudioSyncService } from '../audioSyncService';

describe('AudioSyncService', () => {
  let mockVideo: HTMLVideoElement;
  let service: AudioSyncService;

  beforeEach(() => {
    // Create mock video element
    mockVideo = document.createElement('video');
    mockVideo.src = 'test-video.mp4';
    
    // Mock video properties
    Object.defineProperty(mockVideo, 'duration', {
      value: 60,
      writable: true,
    });
    Object.defineProperty(mockVideo, 'currentTime', {
      value: 0,
      writable: true,
    });
    Object.defineProperty(mockVideo, 'paused', {
      value: true,
      writable: true,
    });
    Object.defineProperty(mockVideo, 'buffered', {
      value: {
        length: 0,
        start: () => 0,
        end: () => 0,
      },
      writable: true,
    });

    service = new AudioSyncService({
      videoElement: mockVideo,
      targetFrameRate: 30,
    });
  });

  it('should initialize with default metrics', () => {
    const metrics = service.getMetrics();
    expect(metrics.currentOffset).toBe(0);
    expect(metrics.averageOffset).toBe(0);
    expect(metrics.maxOffset).toBe(0);
    expect(metrics.correctionCount).toBe(0);
    expect(metrics.inSync).toBe(true);
  });

  it('should start and stop monitoring', () => {
    expect(() => service.startMonitoring()).not.toThrow();
    expect(() => service.stopMonitoring()).not.toThrow();
  });

  it('should check if in sync', () => {
    const inSync = service.isInSync();
    expect(typeof inSync).toBe('boolean');
    expect(inSync).toBe(true);
  });

  it('should get offset history', () => {
    const history = service.getOffsetHistory();
    expect(Array.isArray(history)).toBe(true);
  });

  it('should reset metrics', () => {
    service.resetMetrics();
    const metrics = service.getMetrics();
    expect(metrics.currentOffset).toBe(0);
    expect(metrics.averageOffset).toBe(0);
    expect(metrics.correctionCount).toBe(0);
  });

  it('should cleanup on destroy', () => {
    expect(() => service.destroy()).not.toThrow();
  });
});
