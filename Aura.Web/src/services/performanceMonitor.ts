/**
 * Performance Monitoring Service
 *
 * Provides performance tracking using React Profiler API, custom performance marks,
 * and metrics collection for component render times and bundle sizes.
 */

export interface PerformanceMetric {
  id: string;
  name: string;
  phase: 'mount' | 'update' | 'nested-update';
  actualDuration: number;
  baseDuration: number;
  startTime: number;
  commitTime: number;
  timestamp: number;
}

export interface RenderMetric {
  componentName: string;
  renderCount: number;
  totalDuration: number;
  averageDuration: number;
  maxDuration: number;
  minDuration: number;
  lastRenderTime: number;
}

export interface BundleMetric {
  name: string;
  size: number;
  gzipSize?: number;
  timestamp: number;
}

export interface PerformanceBudget {
  name: string;
  maxRenderTime: number; // in milliseconds
  maxBundleSize: number; // in bytes
  maxMemoryUsage?: number; // in MB
}

/**
 * Union type for all metric types
 */
export type AnyMetric = PerformanceMetric | RenderMetric | BundleMetric;

class PerformanceMonitor {
  private metrics: PerformanceMetric[] = [];
  private renderMetrics: Map<string, RenderMetric> = new Map();
  private bundleMetrics: BundleMetric[] = [];
  private budgets: PerformanceBudget[] = [];
  private isEnabled: boolean = true;
  private maxMetrics: number = 1000; // Keep last 1000 metrics
  private warningCallbacks: Array<(warning: string, metric: AnyMetric) => void> = [];

  constructor() {
    // Initialize with default budgets
    this.budgets = [
      { name: 'TimelinePanel', maxRenderTime: 16, maxBundleSize: 50000 }, // 16ms = 60fps
      { name: 'MediaLibraryPanel', maxRenderTime: 16, maxBundleSize: 50000 },
      { name: 'VideoPreviewPanel', maxRenderTime: 16, maxBundleSize: 50000 },
      { name: 'EffectsPanel', maxRenderTime: 33, maxBundleSize: 100000 }, // 33ms = 30fps acceptable
      { name: 'TotalBundle', maxRenderTime: 0, maxBundleSize: 500000 }, // 500KB total budget
    ];
  }

  /**
   * Enable or disable performance monitoring
   */
  setEnabled(enabled: boolean): void {
    this.isEnabled = enabled;
  }

  /**
   * Check if monitoring is enabled
   */
  isMonitoringEnabled(): boolean {
    return this.isEnabled;
  }

  /**
   * Profile callback for React Profiler API
   * Use this in <Profiler onRender={performanceMonitor.onRenderCallback} />
   */
  onRenderCallback = (
    id: string,
    phase: 'mount' | 'update' | 'nested-update',
    actualDuration: number,
    baseDuration: number,
    startTime: number,
    commitTime: number
  ): void => {
    if (!this.isEnabled) return;

    const metric: PerformanceMetric = {
      id,
      name: id,
      phase,
      actualDuration,
      baseDuration,
      startTime,
      commitTime,
      timestamp: Date.now(),
    };

    this.addMetric(metric);
    this.updateRenderMetrics(metric);
    this.checkBudgets(metric);
  };

  /**
   * Add a performance metric
   */
  private addMetric(metric: PerformanceMetric): void {
    this.metrics.push(metric);

    // Keep only the last maxMetrics entries
    if (this.metrics.length > this.maxMetrics) {
      this.metrics = this.metrics.slice(-this.maxMetrics);
    }
  }

  /**
   * Update aggregate render metrics for a component
   */
  private updateRenderMetrics(metric: PerformanceMetric): void {
    const existing = this.renderMetrics.get(metric.id);

    if (existing) {
      const newMetric: RenderMetric = {
        componentName: metric.id,
        renderCount: existing.renderCount + 1,
        totalDuration: existing.totalDuration + metric.actualDuration,
        averageDuration:
          (existing.totalDuration + metric.actualDuration) / (existing.renderCount + 1),
        maxDuration: Math.max(existing.maxDuration, metric.actualDuration),
        minDuration: Math.min(existing.minDuration, metric.actualDuration),
        lastRenderTime: metric.timestamp,
      };
      this.renderMetrics.set(metric.id, newMetric);
    } else {
      this.renderMetrics.set(metric.id, {
        componentName: metric.id,
        renderCount: 1,
        totalDuration: metric.actualDuration,
        averageDuration: metric.actualDuration,
        maxDuration: metric.actualDuration,
        minDuration: metric.actualDuration,
        lastRenderTime: metric.timestamp,
      });
    }
  }

  /**
   * Check if metric exceeds budget and trigger warnings
   */
  private checkBudgets(metric: PerformanceMetric): void {
    const budget = this.budgets.find((b) => b.name === metric.id);
    if (!budget) return;

    if (metric.actualDuration > budget.maxRenderTime) {
      const warning = `Performance Budget Exceeded: ${metric.id} took ${metric.actualDuration.toFixed(2)}ms (budget: ${budget.maxRenderTime}ms)`;
      console.warn(warning, metric);
      this.notifyWarning(warning, metric);
    }
  }

  /**
   * Mark the start of a custom performance measurement
   */
  mark(name: string): void {
    if (!this.isEnabled) return;
    performance.mark(name);
  }

  /**
   * Measure time between two marks
   */
  measure(name: string, startMark: string, endMark?: string): number | null {
    if (!this.isEnabled) return null;

    try {
      const measureName = `${name}-measure`;
      if (endMark) {
        performance.measure(measureName, startMark, endMark);
      } else {
        performance.measure(measureName, startMark);
      }

      const entries = performance.getEntriesByName(measureName);
      if (entries.length > 0) {
        const entry = entries[entries.length - 1];
        return entry.duration;
      }
    } catch (error) {
      console.error('Performance measure error:', error);
    }
    return null;
  }

  /**
   * Clear performance marks and measures
   */
  clearMarks(name?: string): void {
    if (name) {
      performance.clearMarks(name);
      performance.clearMeasures(name);
    } else {
      performance.clearMarks();
      performance.clearMeasures();
    }
  }

  /**
   * Add bundle size metric
   */
  addBundleMetric(metric: BundleMetric): void {
    this.bundleMetrics.push(metric);

    const budget = this.budgets.find((b) => b.name === metric.name || b.name === 'TotalBundle');
    if (budget && metric.size > budget.maxBundleSize) {
      const warning = `Bundle Size Budget Exceeded: ${metric.name} is ${(metric.size / 1024).toFixed(2)}KB (budget: ${(budget.maxBundleSize / 1024).toFixed(2)}KB)`;
      console.warn(warning, metric);
      this.notifyWarning(warning, metric);
    }
  }

  /**
   * Get all performance metrics
   */
  getMetrics(): PerformanceMetric[] {
    return [...this.metrics];
  }

  /**
   * Get render metrics for all components
   */
  getRenderMetrics(): RenderMetric[] {
    return Array.from(this.renderMetrics.values());
  }

  /**
   * Get render metrics for a specific component
   */
  getComponentMetrics(componentName: string): RenderMetric | undefined {
    return this.renderMetrics.get(componentName);
  }

  /**
   * Get bundle metrics
   */
  getBundleMetrics(): BundleMetric[] {
    return [...this.bundleMetrics];
  }

  /**
   * Get performance budgets
   */
  getBudgets(): PerformanceBudget[] {
    return [...this.budgets];
  }

  /**
   * Set custom performance budgets
   */
  setBudgets(budgets: PerformanceBudget[]): void {
    this.budgets = budgets;
  }

  /**
   * Add a single budget
   */
  addBudget(budget: PerformanceBudget): void {
    const existingIndex = this.budgets.findIndex((b) => b.name === budget.name);
    if (existingIndex >= 0) {
      this.budgets[existingIndex] = budget;
    } else {
      this.budgets.push(budget);
    }
  }

  /**
   * Get memory usage (if available)
   */
  getMemoryUsage(): {
    usedJSHeapSize?: number;
    totalJSHeapSize?: number;
    jsHeapSizeLimit?: number;
  } {
    interface PerformanceMemory {
      usedJSHeapSize: number;
      totalJSHeapSize: number;
      jsHeapSizeLimit: number;
    }

    if ('memory' in performance) {
      const memory = (performance as Performance & { memory: PerformanceMemory }).memory;
      return {
        usedJSHeapSize: memory.usedJSHeapSize,
        totalJSHeapSize: memory.totalJSHeapSize,
        jsHeapSizeLimit: memory.jsHeapSizeLimit,
      };
    }
    return {};
  }

  /**
   * Get FPS (frames per second) estimate based on recent render times
   */
  getFPS(): number {
    const recentMetrics = this.metrics.slice(-10);
    if (recentMetrics.length === 0) return 60; // Default

    const avgDuration =
      recentMetrics.reduce((sum, m) => sum + m.actualDuration, 0) / recentMetrics.length;
    return avgDuration > 0 ? Math.round(1000 / avgDuration) : 60;
  }

  /**
   * Register a callback for budget warnings
   */
  onWarning(callback: (warning: string, metric: AnyMetric) => void): () => void {
    this.warningCallbacks.push(callback);
    // Return unsubscribe function
    return () => {
      const index = this.warningCallbacks.indexOf(callback);
      if (index > -1) {
        this.warningCallbacks.splice(index, 1);
      }
    };
  }

  /**
   * Notify all warning callbacks
   */
  private notifyWarning(warning: string, metric: AnyMetric): void {
    this.warningCallbacks.forEach((callback) => {
      try {
        callback(warning, metric);
      } catch (error) {
        console.error('Error in warning callback:', error);
      }
    });
  }

  /**
   * Clear all metrics
   */
  clearMetrics(): void {
    this.metrics = [];
    this.renderMetrics.clear();
    this.bundleMetrics = [];
  }

  /**
   * Get performance summary
   */
  getSummary(): {
    totalComponents: number;
    totalRenders: number;
    averageRenderTime: number;
    slowestComponent: RenderMetric | null;
    fps: number;
    memoryUsage: { usedJSHeapSize?: number; totalJSHeapSize?: number; jsHeapSizeLimit?: number };
  } {
    const renderMetrics = this.getRenderMetrics();
    const totalRenders = renderMetrics.reduce((sum, m) => sum + m.renderCount, 0);
    const totalDuration = renderMetrics.reduce((sum, m) => sum + m.totalDuration, 0);
    const slowestComponent = renderMetrics.reduce<RenderMetric | null>((slowest, current) => {
      if (!slowest || current.maxDuration > slowest.maxDuration) {
        return current;
      }
      return slowest;
    }, null);

    return {
      totalComponents: renderMetrics.length,
      totalRenders,
      averageRenderTime: totalRenders > 0 ? totalDuration / totalRenders : 0,
      slowestComponent,
      fps: this.getFPS(),
      memoryUsage: this.getMemoryUsage(),
    };
  }

  /**
   * Export metrics to JSON
   */
  exportMetrics(): string {
    return JSON.stringify(
      {
        metrics: this.metrics,
        renderMetrics: Array.from(this.renderMetrics.entries()),
        bundleMetrics: this.bundleMetrics,
        budgets: this.budgets,
        summary: this.getSummary(),
        timestamp: Date.now(),
      },
      null,
      2
    );
  }
}

// Singleton instance
export const performanceMonitor = new PerformanceMonitor();

// Enable in development, can be toggled in production
if (import.meta.env.DEV) {
  performanceMonitor.setEnabled(true);
} else {
  // Can be enabled via localStorage for debugging in production
  performanceMonitor.setEnabled(localStorage.getItem('enablePerformanceMonitoring') === 'true');
}

// Expose to window for debugging
if (typeof window !== 'undefined') {
  (window as any).performanceMonitor = performanceMonitor;
}
