/**
 * Tests for FrameCache service
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { FrameCache } from '../frameCache';

describe('FrameCache', () => {
  let cache: FrameCache;

  beforeEach(() => {
    cache = new FrameCache({
      maxCacheSize: 10, // 10 MB
      preloadFrames: 10,
      cacheRecentFrames: 20,
      frameRate: 30,
    });
  });

  it('should initialize with empty cache', () => {
    const stats = cache.getStats();
    expect(stats.totalFrames).toBe(0);
    expect(stats.cacheSize).toBe(0);
    expect(stats.hitCount).toBe(0);
    expect(stats.missCount).toBe(0);
  });

  it('should track cache misses', () => {
    const frame = cache.getFrame(1.0);
    expect(frame).toBe(null);

    const stats = cache.getStats();
    expect(stats.missCount).toBe(1);
    expect(stats.hitCount).toBe(0);
  });

  it('should clear cache', () => {
    cache.clear();
    const stats = cache.getStats();
    expect(stats.totalFrames).toBe(0);
    expect(stats.cacheSize).toBe(0);
    expect(stats.hitCount).toBe(0);
    expect(stats.missCount).toBe(0);
  });

  it('should check if near capacity', () => {
    const isNear = cache.isNearCapacity(0.9);
    expect(typeof isNear).toBe('boolean');
  });

  it('should cleanup on destroy', () => {
    expect(() => cache.destroy()).not.toThrow();
  });
});
