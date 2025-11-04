/**
 * Performance Telemetry Service
 *
 * Tracks playback FPS, scrub latency, and cache hit rates for preview performance
 */

export interface PerformanceMetrics {
  playbackFps: number;
  scrubLatencyMs: number;
  cacheHitRate: number;
  framesCached: number;
  cacheSizeBytes: number;
}

export interface ScrubEvent {
  timestamp: number;
  latencyMs: number;
}

export interface FrameRenderEvent {
  timestamp: number;
  frameNumber: number;
  fromCache: boolean;
}

export class PerformanceTelemetry {
  private static instance: PerformanceTelemetry;

  private frameRenderEvents: FrameRenderEvent[] = [];
  private scrubEvents: ScrubEvent[] = [];
  private lastFpsCalculation: number = 0;
  private currentFps: number = 0;
  private maxEventsToKeep: number = 300;

  private constructor() {}

  public static getInstance(): PerformanceTelemetry {
    if (!PerformanceTelemetry.instance) {
      PerformanceTelemetry.instance = new PerformanceTelemetry();
    }
    return PerformanceTelemetry.instance;
  }

  /**
   * Record a frame render event
   */
  public recordFrameRender(frameNumber: number, fromCache: boolean): void {
    const event: FrameRenderEvent = {
      timestamp: performance.now(),
      frameNumber,
      fromCache,
    };

    this.frameRenderEvents.push(event);

    while (this.frameRenderEvents.length > this.maxEventsToKeep) {
      this.frameRenderEvents.shift();
    }

    const now = performance.now();
    if (now - this.lastFpsCalculation > 1000) {
      this.calculateFps();
      this.lastFpsCalculation = now;
    }
  }

  /**
   * Record a scrub/seek event
   */
  public recordScrubEvent(latencyMs: number): void {
    const event: ScrubEvent = {
      timestamp: performance.now(),
      latencyMs,
    };

    this.scrubEvents.push(event);

    while (this.scrubEvents.length > this.maxEventsToKeep) {
      this.scrubEvents.shift();
    }
  }

  /**
   * Start measuring scrub latency
   */
  public startScrubMeasure(): number {
    return performance.now();
  }

  /**
   * End measuring scrub latency
   */
  public endScrubMeasure(startTime: number): void {
    const latency = performance.now() - startTime;
    this.recordScrubEvent(latency);
  }

  /**
   * Get current FPS
   */
  public getCurrentFps(): number {
    return this.currentFps;
  }

  /**
   * Get average scrub latency in the last N seconds
   */
  public getAverageScrubLatency(windowSeconds: number = 10): number {
    const now = performance.now();
    const windowStart = now - windowSeconds * 1000;

    const recentEvents = this.scrubEvents.filter((e) => e.timestamp >= windowStart);

    if (recentEvents.length === 0) {
      return 0;
    }

    const sum = recentEvents.reduce((acc, e) => acc + e.latencyMs, 0);
    return sum / recentEvents.length;
  }

  /**
   * Get cache hit rate in the last N seconds
   */
  public getCacheHitRate(windowSeconds: number = 10): number {
    const now = performance.now();
    const windowStart = now - windowSeconds * 1000;

    const recentEvents = this.frameRenderEvents.filter((e) => e.timestamp >= windowStart);

    if (recentEvents.length === 0) {
      return 0;
    }

    const cacheHits = recentEvents.filter((e) => e.fromCache).length;
    return (cacheHits / recentEvents.length) * 100;
  }

  /**
   * Get comprehensive performance metrics
   */
  public getMetrics(): PerformanceMetrics {
    return {
      playbackFps: this.currentFps,
      scrubLatencyMs: this.getAverageScrubLatency(10),
      cacheHitRate: this.getCacheHitRate(10),
      framesCached: this.frameRenderEvents.filter((e) => e.fromCache).length,
      cacheSizeBytes: 0,
    };
  }

  /**
   * Reset all metrics
   */
  public reset(): void {
    this.frameRenderEvents = [];
    this.scrubEvents = [];
    this.lastFpsCalculation = 0;
    this.currentFps = 0;
  }

  /**
   * Get metrics summary for display
   */
  public getMetricsSummary(): string {
    const metrics = this.getMetrics();
    return (
      `FPS: ${metrics.playbackFps.toFixed(1)} | ` +
      `Scrub: ${metrics.scrubLatencyMs.toFixed(0)}ms | ` +
      `Cache Hit: ${metrics.cacheHitRate.toFixed(0)}%`
    );
  }

  /**
   * Check if performance is acceptable
   */
  public isPerformanceGood(): {
    good: boolean;
    issues: string[];
  } {
    const metrics = this.getMetrics();
    const issues: string[] = [];

    if (metrics.playbackFps < 24) {
      issues.push(`Low FPS: ${metrics.playbackFps.toFixed(1)} (target: 24+)`);
    }

    if (metrics.scrubLatencyMs > 50) {
      issues.push(`High scrub latency: ${metrics.scrubLatencyMs.toFixed(0)}ms (target: <50ms)`);
    }

    if (metrics.cacheHitRate < 50 && this.frameRenderEvents.length > 30) {
      issues.push(`Low cache hit rate: ${metrics.cacheHitRate.toFixed(0)}% (target: 50%+)`);
    }

    return {
      good: issues.length === 0,
      issues,
    };
  }

  private calculateFps(): void {
    const now = performance.now();
    const oneSecondAgo = now - 1000;

    const recentFrames = this.frameRenderEvents.filter((e) => e.timestamp >= oneSecondAgo);

    this.currentFps = recentFrames.length;
  }

  /**
   * Log performance warnings if thresholds are exceeded
   */
  public logPerformanceWarnings(): void {
    const { good, issues } = this.isPerformanceGood();

    if (!good) {
      console.warn('Performance issues detected:', issues);
    }
  }
}

export const performanceTelemetry = PerformanceTelemetry.getInstance();
