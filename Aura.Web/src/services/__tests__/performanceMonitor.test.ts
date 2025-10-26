/**
 * Tests for Performance Monitor Service
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { performanceMonitor } from '../performanceMonitor';

describe('PerformanceMonitor', () => {
  beforeEach(() => {
    performanceMonitor.clearMetrics();
    performanceMonitor.setEnabled(true);
  });

  describe('Basic Functionality', () => {
    it('should enable and disable monitoring', () => {
      performanceMonitor.setEnabled(false);
      expect(performanceMonitor.isMonitoringEnabled()).toBe(false);

      performanceMonitor.setEnabled(true);
      expect(performanceMonitor.isMonitoringEnabled()).toBe(true);
    });

    it('should track render metrics', () => {
      performanceMonitor.onRenderCallback('TestComponent', 'mount', 10, 8, 100, 110);

      const metrics = performanceMonitor.getMetrics();
      expect(metrics.length).toBe(1);
      expect(metrics[0].id).toBe('TestComponent');
      expect(metrics[0].actualDuration).toBe(10);
    });

    it('should update render metrics on multiple renders', () => {
      performanceMonitor.onRenderCallback('TestComponent', 'mount', 10, 8, 100, 110);
      performanceMonitor.onRenderCallback('TestComponent', 'update', 5, 8, 200, 205);

      const renderMetrics = performanceMonitor.getRenderMetrics();
      const testMetric = renderMetrics.find((m) => m.componentName === 'TestComponent');

      expect(testMetric).toBeDefined();
      expect(testMetric?.renderCount).toBe(2);
      expect(testMetric?.averageDuration).toBe(7.5); // (10 + 5) / 2
      expect(testMetric?.maxDuration).toBe(10);
      expect(testMetric?.minDuration).toBe(5);
    });

    it('should not track metrics when disabled', () => {
      performanceMonitor.setEnabled(false);
      performanceMonitor.onRenderCallback('TestComponent', 'mount', 10, 8, 100, 110);

      const metrics = performanceMonitor.getMetrics();
      expect(metrics.length).toBe(0);
    });
  });

  describe('Performance Budgets', () => {
    it('should have default budgets', () => {
      const budgets = performanceMonitor.getBudgets();
      expect(budgets.length).toBeGreaterThan(0);
      expect(budgets.find((b) => b.name === 'TimelinePanel')).toBeDefined();
    });

    it('should add custom budgets', () => {
      performanceMonitor.addBudget({
        name: 'CustomComponent',
        maxRenderTime: 20,
        maxBundleSize: 100000,
      });

      const budgets = performanceMonitor.getBudgets();
      const customBudget = budgets.find((b) => b.name === 'CustomComponent');
      expect(customBudget).toBeDefined();
      expect(customBudget?.maxRenderTime).toBe(20);
    });

    it('should warn when budget is exceeded', () => {
      const warnings: any[] = [];
      const unsubscribe = performanceMonitor.onWarning((warning, metric) => {
        warnings.push({ warning, metric });
      });

      // TimelinePanel budget is 16ms
      performanceMonitor.onRenderCallback('TimelinePanel', 'mount', 20, 8, 100, 120);

      expect(warnings.length).toBe(1);
      expect(warnings[0].warning).toContain('Performance Budget Exceeded');
      expect(warnings[0].warning).toContain('TimelinePanel');

      unsubscribe();
    });
  });

  describe('Custom Marks and Measures', () => {
    it('should create performance marks', () => {
      performanceMonitor.mark('test-start');
      const marks = performance.getEntriesByName('test-start');
      expect(marks.length).toBeGreaterThan(0);

      performanceMonitor.clearMarks('test-start');
    });

    it('should measure time between marks', () => {
      performanceMonitor.mark('test-start');
      performanceMonitor.mark('test-end');

      const duration = performanceMonitor.measure('test-operation', 'test-start', 'test-end');
      expect(duration).toBeGreaterThanOrEqual(0);

      performanceMonitor.clearMarks();
    });
  });

  describe('Bundle Metrics', () => {
    it('should track bundle metrics', () => {
      performanceMonitor.addBundleMetric({
        name: 'main-bundle',
        size: 50000,
        timestamp: Date.now(),
      });

      const bundleMetrics = performanceMonitor.getBundleMetrics();
      expect(bundleMetrics.length).toBe(1);
      expect(bundleMetrics[0].name).toBe('main-bundle');
      expect(bundleMetrics[0].size).toBe(50000);
    });

    it('should warn when bundle size exceeds budget', () => {
      const warnings: any[] = [];
      const unsubscribe = performanceMonitor.onWarning((warning, metric) => {
        warnings.push({ warning, metric });
      });

      // Add bundle that exceeds TimelinePanel budget (50KB)
      performanceMonitor.addBundleMetric({
        name: 'TimelinePanel',
        size: 60000,
        timestamp: Date.now(),
      });

      expect(warnings.length).toBe(1);
      expect(warnings[0].warning).toContain('Bundle Size Budget Exceeded');

      unsubscribe();
    });
  });

  describe('Summary and Export', () => {
    it('should provide performance summary', () => {
      performanceMonitor.onRenderCallback('Component1', 'mount', 10, 8, 100, 110);
      performanceMonitor.onRenderCallback('Component2', 'mount', 5, 4, 200, 205);

      const summary = performanceMonitor.getSummary();

      expect(summary.totalComponents).toBe(2);
      expect(summary.totalRenders).toBe(2);
      expect(summary.averageRenderTime).toBe(7.5);
      expect(summary.slowestComponent).toBeDefined();
      expect(summary.slowestComponent?.componentName).toBe('Component1');
    });

    it('should export metrics as JSON', () => {
      performanceMonitor.onRenderCallback('TestComponent', 'mount', 10, 8, 100, 110);

      const json = performanceMonitor.exportMetrics();
      const data = JSON.parse(json);

      expect(data.metrics).toBeDefined();
      expect(data.renderMetrics).toBeDefined();
      expect(data.budgets).toBeDefined();
      expect(data.summary).toBeDefined();
      expect(data.timestamp).toBeDefined();
    });
  });

  describe('FPS Calculation', () => {
    it('should calculate FPS based on render times', () => {
      // Simulate consistent 16ms renders (60fps)
      for (let i = 0; i < 10; i++) {
        performanceMonitor.onRenderCallback('TestComponent', 'update', 16, 8, i * 16, (i + 1) * 16);
      }

      const fps = performanceMonitor.getFPS();
      expect(fps).toBeGreaterThan(50); // Should be close to 60
      expect(fps).toBeLessThan(70);
    });
  });

  describe('Memory Usage', () => {
    it('should get memory usage if available', () => {
      const memory = performanceMonitor.getMemoryUsage();

      // Memory API might not be available in all environments
      if ('memory' in performance) {
        expect(memory.usedJSHeapSize).toBeDefined();
        expect(memory.totalJSHeapSize).toBeDefined();
        expect(memory.jsHeapSizeLimit).toBeDefined();
      } else {
        expect(Object.keys(memory).length).toBe(0);
      }
    });
  });

  describe('Metric Limits', () => {
    it('should limit stored metrics to prevent memory leaks', () => {
      // Add more than the max (1000) metrics
      for (let i = 0; i < 1500; i++) {
        performanceMonitor.onRenderCallback('TestComponent', 'update', 5, 4, i * 10, (i + 1) * 10);
      }

      const metrics = performanceMonitor.getMetrics();
      expect(metrics.length).toBeLessThanOrEqual(1000);
    });
  });
});
