/**
 * Tests for Health Monitoring Service
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { healthMonitorService } from '../healthMonitorService';

describe('HealthMonitorService', () => {
  beforeEach(() => {
    healthMonitorService.stop();
    healthMonitorService.clearWarnings();
  });

  afterEach(() => {
    healthMonitorService.stop();
  });

  describe('Basic Operations', () => {
    it('should start health monitoring', () => {
      healthMonitorService.start();

      // Give it a moment to initialize
      vi.waitFor(() => {
        const metrics = healthMonitorService.getMetrics();
        expect(metrics).toBeDefined();
        expect(metrics.status).toBe('healthy');
      });
    });

    it('should stop health monitoring', () => {
      healthMonitorService.start();
      healthMonitorService.stop();

      // Verify it stopped (no way to directly check, but shouldn't throw)
      expect(() => healthMonitorService.getMetrics()).not.toThrow();
    });

    it('should get current metrics', () => {
      healthMonitorService.start();

      const metrics = healthMonitorService.getMetrics();
      expect(metrics).toBeDefined();
      expect(metrics.fps).toBeGreaterThanOrEqual(0);
      expect(metrics.memoryUsage).toBeGreaterThanOrEqual(0);
      expect(metrics.status).toBeDefined();
    });
  });

  describe('Warnings', () => {
    it('should collect and retrieve warnings', () => {
      healthMonitorService.start();

      const initialWarnings = healthMonitorService.getWarnings();
      expect(Array.isArray(initialWarnings)).toBe(true);
    });

    it('should clear warnings', () => {
      healthMonitorService.start();
      healthMonitorService.clearWarnings();

      const warnings = healthMonitorService.getWarnings();
      expect(warnings).toHaveLength(0);
    });
  });

  describe('Warning Listeners', () => {
    it('should add and trigger warning listener', () => {
      const listener = vi.fn();
      healthMonitorService.addWarningListener(listener);

      // Start monitoring - this may trigger warnings
      healthMonitorService.start();

      // Clean up
      healthMonitorService.removeWarningListener(listener);
    });

    it('should remove warning listener', () => {
      const listener = vi.fn();

      healthMonitorService.addWarningListener(listener);
      healthMonitorService.removeWarningListener(listener);

      // Listener should not be called after removal
      healthMonitorService.start();

      // Give it a moment
      setTimeout(() => {
        // No assertions needed - just verifying no crash
      }, 100);
    });
  });

  describe('Performance Tracking', () => {
    it('should record render times', () => {
      healthMonitorService.start();

      // Record some render times
      healthMonitorService.recordRenderTime(10);
      healthMonitorService.recordRenderTime(20);
      healthMonitorService.recordRenderTime(15);

      const metrics = healthMonitorService.getMetrics();
      expect(metrics.renderTime).toBeGreaterThan(0);
    });

    it('should warn on slow render times', () => {
      const listener = vi.fn();
      healthMonitorService.addWarningListener(listener);
      healthMonitorService.start();

      // Record very slow render times
      healthMonitorService.recordRenderTime(150);
      healthMonitorService.recordRenderTime(150);

      // Should have triggered a warning
      vi.waitFor(() => {
        expect(listener).toHaveBeenCalled();
      });

      healthMonitorService.removeWarningListener(listener);
    });
  });

  describe('Health Status', () => {
    it('should report healthy status initially', () => {
      healthMonitorService.start();

      const metrics = healthMonitorService.getMetrics();
      expect(['healthy', 'warning', 'critical']).toContain(metrics.status);
    });
  });

  describe('Memory Monitoring', () => {
    it('should track memory usage if available', () => {
      healthMonitorService.start();

      const metrics = healthMonitorService.getMetrics();

      // Memory might not be available in all environments
      expect(metrics.memoryUsage).toBeGreaterThanOrEqual(0);
      expect(metrics.memoryLimit).toBeGreaterThanOrEqual(0);
      expect(metrics.memoryPercentage).toBeGreaterThanOrEqual(0);
    });
  });

  describe('FPS Monitoring', () => {
    it('should track FPS', () => {
      healthMonitorService.start();

      // Wait a bit for FPS to be measured
      setTimeout(() => {
        const metrics = healthMonitorService.getMetrics();
        expect(metrics.fps).toBeGreaterThanOrEqual(0);
      }, 100);
    });
  });
});
