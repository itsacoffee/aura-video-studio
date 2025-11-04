/**
 * Waveform Service
 *
 * Manages audio waveform generation and caching with priority-based loading
 */

import { apiUrl } from '../config/api';

export interface WaveformData {
  data: number[];
  sampleRate: number;
  duration: number;
}

export interface WaveformGenerateRequest {
  audioPath: string;
  targetSamples?: number;
  startTime?: number;
  endTime?: number;
}

export interface CachedWaveform {
  data: number[];
  timestamp: number;
  priority: number;
}

export class WaveformService {
  private static instance: WaveformService;
  private cache: Map<string, CachedWaveform> = new Map();
  private pendingRequests: Map<string, Promise<WaveformData>> = new Map();
  private maxCacheSize: number = 100;

  private constructor() {}

  public static getInstance(): WaveformService {
    if (!WaveformService.instance) {
      WaveformService.instance = new WaveformService();
    }
    return WaveformService.instance;
  }

  /**
   * Generate waveform data for audio file
   */
  public async generateWaveform(request: WaveformGenerateRequest): Promise<WaveformData> {
    const cacheKey = this.getCacheKey(request);

    const cached = this.cache.get(cacheKey);
    if (cached) {
      cached.timestamp = Date.now();
      return {
        data: cached.data,
        sampleRate: 44100,
        duration: 0,
      };
    }

    if (this.pendingRequests.has(cacheKey)) {
      return this.pendingRequests.get(cacheKey)!;
    }

    const promise = this.fetchWaveform(request);
    this.pendingRequests.set(cacheKey, promise);

    try {
      const result = await promise;
      this.cacheWaveform(cacheKey, result.data, 0);
      return result;
    } finally {
      this.pendingRequests.delete(cacheKey);
    }
  }

  /**
   * Generate waveform with priority (for visible ranges)
   */
  public async generateWaveformWithPriority(
    request: WaveformGenerateRequest,
    priority: number = 0
  ): Promise<WaveformData> {
    const cacheKey = this.getCacheKey(request);

    const cached = this.cache.get(cacheKey);
    if (cached) {
      cached.timestamp = Date.now();
      cached.priority = Math.max(cached.priority, priority);
      return {
        data: cached.data,
        sampleRate: 44100,
        duration: 0,
      };
    }

    const result = await this.generateWaveform(request);
    const cachedEntry = this.cache.get(cacheKey);
    if (cachedEntry) {
      cachedEntry.priority = priority;
    }
    return result;
  }

  /**
   * Fetch waveform image
   */
  public async generateWaveformImage(
    audioPath: string,
    width: number = 800,
    height: number = 100,
    trackType: string = 'narration'
  ): Promise<Blob> {
    const url = apiUrl(
      `/api/waveform/image?audioPath=${encodeURIComponent(audioPath)}&width=${width}&height=${height}&trackType=${trackType}`
    );

    const response = await fetch(url, { method: 'POST' });

    if (!response.ok) {
      throw new Error('Failed to generate waveform image');
    }

    return await response.blob();
  }

  /**
   * Clear waveform cache
   */
  public async clearCache(): Promise<void> {
    this.cache.clear();
    this.pendingRequests.clear();

    try {
      await fetch(apiUrl('/api/waveform/clear-cache'), { method: 'POST' });
    } catch (error) {
      console.error('Error clearing server cache:', error);
    }
  }

  /**
   * Get cache statistics
   */
  public getCacheStats(): {
    totalEntries: number;
    totalDataPoints: number;
    oldestTimestamp: number | null;
  } {
    let totalDataPoints = 0;
    let oldestTimestamp: number | null = null;

    for (const cached of this.cache.values()) {
      totalDataPoints += cached.data.length;
      if (oldestTimestamp === null || cached.timestamp < oldestTimestamp) {
        oldestTimestamp = cached.timestamp;
      }
    }

    return {
      totalEntries: this.cache.size,
      totalDataPoints,
      oldestTimestamp,
    };
  }

  private async fetchWaveform(request: WaveformGenerateRequest): Promise<WaveformData> {
    const response = await fetch(apiUrl('/api/waveform/generate'), {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        audioPath: request.audioPath,
        targetSamples: request.targetSamples || 1000,
        startTime: request.startTime || 0,
        endTime: request.endTime || 0,
      }),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ error: 'Failed to generate waveform' }));
      throw new Error(error.error || 'Failed to generate waveform');
    }

    return await response.json();
  }

  private getCacheKey(request: WaveformGenerateRequest): string {
    const samples = request.targetSamples || 1000;
    const start = request.startTime || 0;
    const end = request.endTime || 0;
    return `${request.audioPath}:${samples}:${start}:${end}`;
  }

  private cacheWaveform(key: string, data: number[], priority: number): void {
    while (this.cache.size >= this.maxCacheSize) {
      this.evictLRU();
    }

    this.cache.set(key, {
      data,
      timestamp: Date.now(),
      priority,
    });
  }

  private evictLRU(): void {
    let oldestKey: string | null = null;
    let oldestScore = Infinity;

    for (const [key, cached] of this.cache.entries()) {
      const age = Date.now() - cached.timestamp;
      const score = age - cached.priority * 10000;

      if (score < oldestScore) {
        oldestScore = score;
        oldestKey = key;
      }
    }

    if (oldestKey) {
      this.cache.delete(oldestKey);
    }
  }
}

export const waveformService = WaveformService.getInstance();
