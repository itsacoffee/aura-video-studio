/**
 * Performance monitoring utilities for tracking memory and rendering performance
 */

export interface PerformanceMetrics {
  memoryUsage?: {
    usedJSHeapSize: number;
    totalJSHeapSize: number;
    jsHeapSizeLimit: number;
  };
  renderCount: number;
  lastRenderTime: number;
}

class PerformanceMonitor {
  private renderCount = 0;
  private lastRenderTime = 0;

  /**
   * Get current memory usage (only available in Chrome with --enable-precise-memory-info)
   */
  getMemoryUsage() {
    if ('memory' in performance) {
      const memory = (
        performance as Performance & {
          memory?: { usedJSHeapSize: number; totalJSHeapSize: number; jsHeapSizeLimit: number };
        }
      ).memory;
      if (memory) {
        return {
          usedJSHeapSize: memory.usedJSHeapSize,
          totalJSHeapSize: memory.totalJSHeapSize,
          jsHeapSizeLimit: memory.jsHeapSizeLimit,
        };
      }
    }
    return undefined;
  }

  /**
   * Track a render event
   */
  trackRender() {
    this.renderCount++;
    this.lastRenderTime = performance.now();
  }

  /**
   * Get current performance metrics
   */
  getMetrics(): PerformanceMetrics {
    return {
      memoryUsage: this.getMemoryUsage(),
      renderCount: this.renderCount,
      lastRenderTime: this.lastRenderTime,
    };
  }

  /**
   * Reset metrics
   */
  reset() {
    this.renderCount = 0;
    this.lastRenderTime = 0;
  }

  /**
   * Log performance metrics to console
   */
  logMetrics(label: string) {
    const metrics = this.getMetrics();
    // Performance logging is intentional
    // eslint-disable-next-line no-console
    console.log(`[Performance] ${label}:`, {
      renderCount: metrics.renderCount,
      lastRenderTime: `${metrics.lastRenderTime.toFixed(2)}ms`,
      memoryUsage: metrics.memoryUsage
        ? {
            used: `${(metrics.memoryUsage.usedJSHeapSize / 1024 / 1024).toFixed(2)} MB`,
            total: `${(metrics.memoryUsage.totalJSHeapSize / 1024 / 1024).toFixed(2)} MB`,
            limit: `${(metrics.memoryUsage.jsHeapSizeLimit / 1024 / 1024).toFixed(2)} MB`,
          }
        : 'Not available',
    });
  }

  /**
   * Check if memory usage is stable (not growing rapidly)
   */
  isMemoryStable(previousMetrics: PerformanceMetrics, threshold = 10): boolean {
    const current = this.getMemoryUsage();
    if (!current || !previousMetrics.memoryUsage) {
      return true; // Can't determine, assume stable
    }

    const previousUsed = previousMetrics.memoryUsage.usedJSHeapSize;
    const currentUsed = current.usedJSHeapSize;
    const growthMB = (currentUsed - previousUsed) / 1024 / 1024;

    return growthMB < threshold;
  }
}

export const performanceMonitor = new PerformanceMonitor();

/**
 * React hook to track component render count
 */
export function useRenderCount(componentName: string) {
  if (import.meta.env.DEV) {
    performanceMonitor.trackRender();
    // Development render tracking is intentional
    // eslint-disable-next-line no-console
    console.log(
      `[Render] ${componentName} rendered ${performanceMonitor.getMetrics().renderCount} times`
    );
  }
}
