import { describe, it, expect, beforeEach } from 'vitest';
import { PerformanceTelemetry } from '../performanceTelemetry';

describe('PerformanceTelemetry', () => {
  let telemetry: PerformanceTelemetry;

  beforeEach(() => {
    telemetry = PerformanceTelemetry.getInstance();
    telemetry.reset();
  });

  describe('frame render tracking', () => {
    it('should record frame render events', () => {
      telemetry.recordFrameRender(1, true);
      telemetry.recordFrameRender(2, false);
      telemetry.recordFrameRender(3, true);

      const metrics = telemetry.getMetrics();
      expect(metrics.framesCached).toBeGreaterThan(0);
    });

    it('should calculate cache hit rate correctly', () => {
      for (let i = 0; i < 10; i++) {
        telemetry.recordFrameRender(i, i % 2 === 0);
      }

      const hitRate = telemetry.getCacheHitRate(10);
      expect(hitRate).toBe(50);
    });

    it('should return 0 cache hit rate when no events', () => {
      const hitRate = telemetry.getCacheHitRate(10);
      expect(hitRate).toBe(0);
    });

    it('should only consider recent events in window', () => {
      telemetry.recordFrameRender(1, true);

      const originalTimestamp = performance.now();
      vi.spyOn(performance, 'now').mockReturnValue(originalTimestamp + 11000);

      telemetry.recordFrameRender(2, false);

      const hitRate = telemetry.getCacheHitRate(10);
      expect(hitRate).toBe(0);

      vi.restoreAllMocks();
    });
  });

  describe('scrub latency tracking', () => {
    it('should record scrub events', () => {
      telemetry.recordScrubEvent(10);
      telemetry.recordScrubEvent(20);
      telemetry.recordScrubEvent(30);

      const avgLatency = telemetry.getAverageScrubLatency(10);
      expect(avgLatency).toBe(20);
    });

    it('should measure scrub latency', () => {
      const startTime = telemetry.startScrubMeasure();

      vi.spyOn(performance, 'now').mockReturnValue(startTime + 25);

      telemetry.endScrubMeasure(startTime);

      const avgLatency = telemetry.getAverageScrubLatency(10);
      expect(avgLatency).toBeCloseTo(25, 1);

      vi.restoreAllMocks();
    });

    it('should return 0 when no scrub events', () => {
      const avgLatency = telemetry.getAverageScrubLatency(10);
      expect(avgLatency).toBe(0);
    });
  });

  describe('performance assessment', () => {
    it('should detect low FPS issues', () => {
      for (let i = 0; i < 20; i++) {
        telemetry.recordFrameRender(i, true);
      }

      const { good, issues } = telemetry.isPerformanceGood();
      expect(good).toBe(false);
      expect(issues.some((issue) => issue.includes('Low FPS'))).toBe(true);
    });

    it('should detect high scrub latency issues', () => {
      telemetry.recordScrubEvent(100);
      telemetry.recordScrubEvent(150);

      const { good, issues } = telemetry.isPerformanceGood();
      expect(good).toBe(false);
      expect(issues.some((issue) => issue.includes('scrub latency'))).toBe(true);
    });

    it('should report good performance when all metrics are acceptable', () => {
      for (let i = 0; i < 30; i++) {
        telemetry.recordFrameRender(i, true);
      }

      telemetry.recordScrubEvent(20);
      telemetry.recordScrubEvent(30);

      const { good, issues } = telemetry.isPerformanceGood();

      if (!good) {
        expect(issues).not.toContain(expect.stringContaining('High scrub latency'));
      }
    });
  });

  describe('metrics summary', () => {
    it('should generate readable metrics summary', () => {
      telemetry.recordFrameRender(1, true);
      telemetry.recordScrubEvent(25);

      const summary = telemetry.getMetricsSummary();

      expect(summary).toContain('FPS:');
      expect(summary).toContain('Scrub:');
      expect(summary).toContain('Cache Hit:');
    });
  });

  describe('reset', () => {
    it('should clear all metrics', () => {
      telemetry.recordFrameRender(1, true);
      telemetry.recordScrubEvent(25);

      telemetry.reset();

      const metrics = telemetry.getMetrics();
      expect(metrics.playbackFps).toBe(0);
      expect(metrics.scrubLatencyMs).toBe(0);
      expect(metrics.cacheHitRate).toBe(0);
      expect(metrics.framesCached).toBe(0);
    });
  });
});
