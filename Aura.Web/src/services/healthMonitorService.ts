/**
 * Health Monitoring Service
 * Detects performance degradation (memory leaks, slow rendering) and suggests corrective actions
 */

import { loggingService } from './loggingService';

export type HealthStatus = 'healthy' | 'warning' | 'critical';

export interface HealthMetrics {
  memoryUsage: number; // MB
  memoryLimit: number; // MB
  memoryPercentage: number; // 0-100
  fps: number;
  renderTime: number; // ms
  longTaskCount: number;
  status: HealthStatus;
}

export interface HealthWarning {
  type: 'memory' | 'performance' | 'long-task';
  severity: 'warning' | 'critical';
  message: string;
  suggestion: string;
  timestamp: string;
}

// Thresholds for health warnings
const MEMORY_WARNING_THRESHOLD = 75; // % of available memory
const MEMORY_CRITICAL_THRESHOLD = 90; // % of available memory
const FPS_WARNING_THRESHOLD = 30;
const FPS_CRITICAL_THRESHOLD = 15;
const RENDER_TIME_WARNING_THRESHOLD = 50; // ms
const RENDER_TIME_CRITICAL_THRESHOLD = 100; // ms
const LONG_TASK_THRESHOLD = 50; // ms
const MONITORING_INTERVAL = 10000; // 10 seconds

class HealthMonitorService {
  private intervalId: number | null = null;
  private isMonitoring = false;
  private metrics: HealthMetrics = {
    memoryUsage: 0,
    memoryLimit: 0,
    memoryPercentage: 0,
    fps: 60,
    renderTime: 0,
    longTaskCount: 0,
    status: 'healthy',
  };
  private warnings: HealthWarning[] = [];
  private listeners: Array<(warning: HealthWarning) => void> = [];
  private fpsHistory: number[] = [];
  private renderTimeHistory: number[] = [];
  private lastFrameTime = performance.now();
  private frameCount = 0;
  private longTaskObserver: PerformanceObserver | null = null;
  private longTaskCount = 0;

  /**
   * Start health monitoring
   */
  public start(): void {
    if (this.isMonitoring) {
      loggingService.warn('Health monitoring already running', 'healthMonitorService', 'start');
      return;
    }

    this.isMonitoring = true;

    // Start FPS monitoring
    this.startFpsMonitoring();

    // Start long task monitoring
    this.startLongTaskMonitoring();

    // Start periodic health checks
    this.intervalId = window.setInterval(() => {
      this.checkHealth();
    }, MONITORING_INTERVAL);

    loggingService.info('Health monitoring started', 'healthMonitorService', 'start');
  }

  /**
   * Stop health monitoring
   */
  public stop(): void {
    if (this.intervalId) {
      clearInterval(this.intervalId);
      this.intervalId = null;
    }

    if (this.longTaskObserver) {
      this.longTaskObserver.disconnect();
      this.longTaskObserver = null;
    }

    this.isMonitoring = false;
    loggingService.info('Health monitoring stopped', 'healthMonitorService', 'stop');
  }

  /**
   * Get current health metrics
   */
  public getMetrics(): HealthMetrics {
    return { ...this.metrics };
  }

  /**
   * Get recent warnings
   */
  public getWarnings(): HealthWarning[] {
    return [...this.warnings];
  }

  /**
   * Clear all warnings
   */
  public clearWarnings(): void {
    this.warnings = [];
  }

  /**
   * Add a listener for health warnings
   */
  public addWarningListener(listener: (warning: HealthWarning) => void): void {
    this.listeners.push(listener);
  }

  /**
   * Remove a warning listener
   */
  public removeWarningListener(listener: (warning: HealthWarning) => void): void {
    const index = this.listeners.indexOf(listener);
    if (index !== -1) {
      this.listeners.splice(index, 1);
    }
  }

  /**
   * Start FPS monitoring
   */
  private startFpsMonitoring(): void {
    const measureFps = () => {
      if (!this.isMonitoring) return;

      const now = performance.now();
      const delta = now - this.lastFrameTime;

      if (delta >= 1000) {
        const fps = Math.round((this.frameCount * 1000) / delta);
        this.fpsHistory.push(fps);

        // Keep only last 10 measurements
        if (this.fpsHistory.length > 10) {
          this.fpsHistory.shift();
        }

        // Calculate average FPS
        const avgFps = this.fpsHistory.reduce((a, b) => a + b, 0) / this.fpsHistory.length;
        this.metrics.fps = Math.round(avgFps);

        this.frameCount = 0;
        this.lastFrameTime = now;
      }

      this.frameCount++;
      requestAnimationFrame(measureFps);
    };

    requestAnimationFrame(measureFps);
  }

  /**
   * Start long task monitoring
   */
  private startLongTaskMonitoring(): void {
    try {
      // PerformanceObserver for long tasks (requires browser support)
      if ('PerformanceObserver' in window) {
        this.longTaskObserver = new PerformanceObserver((list) => {
          for (const entry of list.getEntries()) {
            if (entry.duration > LONG_TASK_THRESHOLD) {
              this.longTaskCount++;
            }
          }
          this.metrics.longTaskCount = this.longTaskCount;
        });

        try {
          this.longTaskObserver.observe({ entryTypes: ['longtask'] });
        } catch (e) {
          // longtask not supported in this browser
          loggingService.debug(
            'Long task monitoring not supported',
            'healthMonitorService',
            'startLongTaskMonitoring'
          );
        }
      }
    } catch (error) {
      loggingService.error(
        'Failed to start long task monitoring',
        error as Error,
        'healthMonitorService',
        'startLongTaskMonitoring'
      );
    }
  }

  /**
   * Check overall system health
   */
  private checkHealth(): void {
    // Check memory usage
    this.checkMemoryHealth();

    // Check performance
    this.checkPerformanceHealth();

    // Determine overall status
    this.updateHealthStatus();
  }

  /**
   * Check memory health
   */
  private checkMemoryHealth(): void {
    try {
      // Use Performance API memory info (Chrome only)
      if ('memory' in performance) {
        const memory = (
          performance as Performance & {
            memory: { usedJSHeapSize: number; jsHeapSizeLimit: number };
          }
        ).memory;
        const usedMB = memory.usedJSHeapSize / 1048576;
        const limitMB = memory.jsHeapSizeLimit / 1048576;
        const percentage = (usedMB / limitMB) * 100;

        this.metrics.memoryUsage = Math.round(usedMB);
        this.metrics.memoryLimit = Math.round(limitMB);
        this.metrics.memoryPercentage = Math.round(percentage);

        // Check thresholds
        if (percentage >= MEMORY_CRITICAL_THRESHOLD) {
          this.addWarning({
            type: 'memory',
            severity: 'critical',
            message: `Memory usage critical: ${Math.round(percentage)}%`,
            suggestion:
              'Consider closing unused clips, reducing preview quality, or reloading the page.',
            timestamp: new Date().toISOString(),
          });
        } else if (percentage >= MEMORY_WARNING_THRESHOLD) {
          this.addWarning({
            type: 'memory',
            severity: 'warning',
            message: `Memory usage high: ${Math.round(percentage)}%`,
            suggestion: 'Consider reducing the number of effects or clips in your timeline.',
            timestamp: new Date().toISOString(),
          });
        }
      }
    } catch (error) {
      loggingService.error(
        'Failed to check memory health',
        error as Error,
        'healthMonitorService',
        'checkMemoryHealth'
      );
    }
  }

  /**
   * Check performance health
   */
  private checkPerformanceHealth(): void {
    // Check FPS
    if (this.metrics.fps < FPS_CRITICAL_THRESHOLD) {
      this.addWarning({
        type: 'performance',
        severity: 'critical',
        message: `Performance critical: ${this.metrics.fps} FPS`,
        suggestion:
          'Reduce preview quality, close other applications, or disable real-time effects.',
        timestamp: new Date().toISOString(),
      });
    } else if (this.metrics.fps < FPS_WARNING_THRESHOLD) {
      this.addWarning({
        type: 'performance',
        severity: 'warning',
        message: `Performance degraded: ${this.metrics.fps} FPS`,
        suggestion: 'Consider reducing timeline complexity or preview quality.',
        timestamp: new Date().toISOString(),
      });
    }

    // Check long tasks
    if (this.longTaskCount > 10) {
      this.addWarning({
        type: 'long-task',
        severity: 'warning',
        message: `${this.longTaskCount} long-running tasks detected`,
        suggestion:
          'The application may be processing heavy operations. Performance may be degraded.',
        timestamp: new Date().toISOString(),
      });

      // Reset counter after warning
      this.longTaskCount = 0;
    }
  }

  /**
   * Update overall health status
   */
  private updateHealthStatus(): void {
    const recentWarnings = this.warnings.filter(
      (w) => Date.now() - new Date(w.timestamp).getTime() < 60000 // Last minute
    );

    const hasCritical = recentWarnings.some((w) => w.severity === 'critical');
    const hasWarning = recentWarnings.some((w) => w.severity === 'warning');

    if (hasCritical) {
      this.metrics.status = 'critical';
    } else if (hasWarning) {
      this.metrics.status = 'warning';
    } else {
      this.metrics.status = 'healthy';
    }
  }

  /**
   * Add a warning and notify listeners
   */
  private addWarning(warning: HealthWarning): void {
    // Avoid duplicate warnings within 30 seconds
    const isDuplicate = this.warnings.some(
      (w) =>
        w.type === warning.type &&
        w.severity === warning.severity &&
        Date.now() - new Date(w.timestamp).getTime() < 30000
    );

    if (isDuplicate) {
      return;
    }

    this.warnings.push(warning);

    // Keep only last 20 warnings
    if (this.warnings.length > 20) {
      this.warnings.shift();
    }

    // Log warning
    loggingService.warn(warning.message, 'healthMonitorService', 'warning', {
      type: warning.type,
      suggestion: warning.suggestion,
    });

    // Notify listeners
    this.listeners.forEach((listener) => {
      try {
        listener(warning);
      } catch (error) {
        loggingService.error(
          'Error in health warning listener',
          error as Error,
          'healthMonitorService',
          'addWarning'
        );
      }
    });
  }

  /**
   * Record render time for performance tracking
   */
  public recordRenderTime(timeMs: number): void {
    this.renderTimeHistory.push(timeMs);

    // Keep only last 10 measurements
    if (this.renderTimeHistory.length > 10) {
      this.renderTimeHistory.shift();
    }

    // Calculate average render time
    const avgRenderTime =
      this.renderTimeHistory.reduce((a, b) => a + b, 0) / this.renderTimeHistory.length;
    this.metrics.renderTime = Math.round(avgRenderTime);

    // Check render time thresholds
    if (avgRenderTime >= RENDER_TIME_CRITICAL_THRESHOLD) {
      this.addWarning({
        type: 'performance',
        severity: 'critical',
        message: `Slow rendering detected: ${Math.round(avgRenderTime)}ms`,
        suggestion: 'Reduce timeline complexity or disable real-time preview.',
        timestamp: new Date().toISOString(),
      });
    } else if (avgRenderTime >= RENDER_TIME_WARNING_THRESHOLD) {
      this.addWarning({
        type: 'performance',
        severity: 'warning',
        message: `Rendering slower than optimal: ${Math.round(avgRenderTime)}ms`,
        suggestion: 'Consider reducing the number of effects or preview quality.',
        timestamp: new Date().toISOString(),
      });
    }
  }
}

// Export singleton instance
export const healthMonitorService = new HealthMonitorService();
