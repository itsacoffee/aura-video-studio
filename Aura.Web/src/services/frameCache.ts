/**
 * Intelligent Frame Cache Service
 *
 * Provides efficient frame caching with:
 * - Pre-loading frames ahead of playhead
 * - LRU cache for recently viewed frames
 * - Memory management to prevent browser crashes
 * - Performance optimization for smooth playback
 */

export interface CachedFrame {
  timestamp: number;
  imageData: ImageData;
  lastAccessed: number;
  size: number; // in bytes
}

export interface FrameCacheOptions {
  maxCacheSize?: number; // in MB
  preloadFrames?: number; // number of frames to preload ahead
  cacheRecentFrames?: number; // number of recently viewed frames to keep
  frameRate?: number;
}

export interface CacheStats {
  totalFrames: number;
  cacheSize: number; // in MB
  hitRate: number; // percentage
  missCount: number;
  hitCount: number;
  evictionCount: number;
}

/**
 * LRU Frame Cache for video playback optimization
 */
export class FrameCache {
  private cache: Map<number, CachedFrame> = new Map();
  private maxCacheSize: number; // in bytes
  private preloadFrameCount: number;
  private cacheRecentFrames: number;
  private frameRate: number;

  private currentCacheSize: number = 0; // in bytes
  private hitCount: number = 0;
  private missCount: number = 0;
  private evictionCount: number = 0;

  private videoElement: HTMLVideoElement | null = null;
  private canvasElement: HTMLCanvasElement | null = null;
  private preloadInterval: number | null = null;

  constructor(options: FrameCacheOptions = {}) {
    this.maxCacheSize = (options.maxCacheSize || 100) * 1024 * 1024; // Convert MB to bytes
    this.preloadFrameCount = options.preloadFrames || 30; // 1 second at 30fps
    this.cacheRecentFrames = options.cacheRecentFrames || 60; // 2 seconds at 30fps
    this.frameRate = options.frameRate || 30;
  }

  /**
   * Initialize cache with video element
   */
  initialize(videoElement: HTMLVideoElement, canvasElement?: HTMLCanvasElement): void {
    this.videoElement = videoElement;
    this.canvasElement = canvasElement || null;
  }

  /**
   * Get frame at specific timestamp
   */
  getFrame(timestamp: number): CachedFrame | null {
    // Round to frame boundary
    const frameTime = this.roundToFrameBoundary(timestamp);
    const cached = this.cache.get(frameTime);

    if (cached) {
      this.hitCount++;
      cached.lastAccessed = Date.now();
      return cached;
    }

    this.missCount++;
    return null;
  }

  /**
   * Cache a frame at specific timestamp
   */
  async cacheFrame(timestamp: number): Promise<CachedFrame | null> {
    if (!this.videoElement) return null;

    const frameTime = this.roundToFrameBoundary(timestamp);

    // Check if already cached
    const existing = this.cache.get(frameTime);
    if (existing) {
      existing.lastAccessed = Date.now();
      return existing;
    }

    // Capture frame
    const frameData = await this.captureFrame(frameTime);
    if (!frameData) return null;

    const frame: CachedFrame = {
      timestamp: frameTime,
      imageData: frameData,
      lastAccessed: Date.now(),
      size: frameData.width * frameData.height * 4, // RGBA
    };

    // Check if we need to evict frames
    while (this.currentCacheSize + frame.size > this.maxCacheSize && this.cache.size > 0) {
      this.evictLRU();
    }

    // Add to cache
    this.cache.set(frameTime, frame);
    this.currentCacheSize += frame.size;

    return frame;
  }

  /**
   * Capture frame from video at specific time
   */
  private async captureFrame(timestamp: number): Promise<ImageData | null> {
    if (!this.videoElement) return null;

    // Create temporary canvas if not provided
    const canvas = this.canvasElement || document.createElement('canvas');
    const ctx = canvas.getContext('2d');
    if (!ctx) return null;

    // Seek to timestamp
    const originalTime = this.videoElement.currentTime;
    this.videoElement.currentTime = timestamp;

    // Wait for seek to complete
    await new Promise<void>((resolve) => {
      const onSeeked = () => {
        this.videoElement?.removeEventListener('seeked', onSeeked);
        resolve();
      };
      this.videoElement?.addEventListener('seeked', onSeeked);

      // Timeout fallback
      setTimeout(resolve, 100);
    });

    // Ensure video has loaded
    if (!this.videoElement.videoWidth || !this.videoElement.videoHeight) {
      return null;
    }

    // Set canvas size
    canvas.width = this.videoElement.videoWidth;
    canvas.height = this.videoElement.videoHeight;

    // Draw frame
    ctx.drawImage(this.videoElement, 0, 0, canvas.width, canvas.height);

    // Get image data
    const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);

    // Restore original time if not using preload
    if (originalTime !== timestamp) {
      this.videoElement.currentTime = originalTime;
    }

    return imageData;
  }

  /**
   * Preload frames ahead of current position
   */
  async preloadFrames(currentTime: number): Promise<void> {
    if (!this.videoElement) return;

    const frameDuration = 1 / this.frameRate;
    const promises: Promise<CachedFrame | null>[] = [];

    for (let i = 0; i < this.preloadFrameCount; i++) {
      const time = currentTime + i * frameDuration;
      if (time <= this.videoElement.duration) {
        // Only preload if not already cached
        const frameTime = this.roundToFrameBoundary(time);
        if (!this.cache.has(frameTime)) {
          promises.push(this.cacheFrame(time));
        }
      }
    }

    // Cache frames in parallel (up to 5 at a time to avoid overwhelming)
    const batchSize = 5;
    for (let i = 0; i < promises.length; i += batchSize) {
      const batch = promises.slice(i, i + batchSize);
      await Promise.all(batch);
    }
  }

  /**
   * Start automatic preloading during playback
   */
  startPreloading(getCurrentTime: () => number, intervalMs: number = 500): void {
    this.stopPreloading();

    this.preloadInterval = window.setInterval(() => {
      const currentTime = getCurrentTime();
      this.preloadFrames(currentTime).catch((err) => {
        console.warn('Preload error:', err);
      });
    }, intervalMs);
  }

  /**
   * Stop automatic preloading
   */
  stopPreloading(): void {
    if (this.preloadInterval !== null) {
      clearInterval(this.preloadInterval);
      this.preloadInterval = null;
    }
  }

  /**
   * Round timestamp to nearest frame boundary
   */
  private roundToFrameBoundary(timestamp: number): number {
    const frameDuration = 1 / this.frameRate;
    return Math.round(timestamp / frameDuration) * frameDuration;
  }

  /**
   * Evict least recently used frame
   */
  private evictLRU(): void {
    let oldestTime = Date.now();
    let oldestKey: number | null = null;

    for (const [key, frame] of this.cache.entries()) {
      if (frame.lastAccessed < oldestTime) {
        oldestTime = frame.lastAccessed;
        oldestKey = key;
      }
    }

    if (oldestKey !== null) {
      const frame = this.cache.get(oldestKey);
      if (frame) {
        this.currentCacheSize -= frame.size;
        this.cache.delete(oldestKey);
        this.evictionCount++;
      }
    }
  }

  /**
   * Clear all cached frames
   */
  clear(): void {
    this.cache.clear();
    this.currentCacheSize = 0;
    this.hitCount = 0;
    this.missCount = 0;
    this.evictionCount = 0;
  }

  /**
   * Clear frames in a specific time range
   */
  clearRange(startTime: number, endTime: number): void {
    const keysToDelete: number[] = [];

    for (const [key] of this.cache.entries()) {
      if (key >= startTime && key <= endTime) {
        keysToDelete.push(key);
      }
    }

    for (const key of keysToDelete) {
      const frame = this.cache.get(key);
      if (frame) {
        this.currentCacheSize -= frame.size;
        this.cache.delete(key);
      }
    }
  }

  /**
   * Get cache statistics
   */
  getStats(): CacheStats {
    const totalAccesses = this.hitCount + this.missCount;
    const hitRate = totalAccesses > 0 ? (this.hitCount / totalAccesses) * 100 : 0;

    return {
      totalFrames: this.cache.size,
      cacheSize: this.currentCacheSize / (1024 * 1024), // Convert to MB
      hitRate,
      missCount: this.missCount,
      hitCount: this.hitCount,
      evictionCount: this.evictionCount,
    };
  }

  /**
   * Check if cache is near capacity
   */
  isNearCapacity(threshold: number = 0.9): boolean {
    return this.currentCacheSize / this.maxCacheSize >= threshold;
  }

  /**
   * Optimize cache by removing old frames
   */
  optimize(currentTime: number): void {
    const frameDuration = 1 / this.frameRate;
    const keepRangeStart = currentTime - this.cacheRecentFrames * frameDuration;
    const keepRangeEnd = currentTime + this.preloadFrameCount * frameDuration;

    const keysToDelete: number[] = [];

    for (const [key] of this.cache.entries()) {
      if (key < keepRangeStart || key > keepRangeEnd) {
        keysToDelete.push(key);
      }
    }

    for (const key of keysToDelete) {
      const frame = this.cache.get(key);
      if (frame) {
        this.currentCacheSize -= frame.size;
        this.cache.delete(key);
      }
    }
  }

  /**
   * Cleanup and destroy
   */
  destroy(): void {
    this.stopPreloading();
    this.clear();
    this.videoElement = null;
    this.canvasElement = null;
  }
}

/**
 * Singleton instance for global use
 */
export const frameCache = new FrameCache({
  maxCacheSize: 100, // 100 MB
  preloadFrames: 30,
  cacheRecentFrames: 60,
  frameRate: 30,
});
