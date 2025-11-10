/**
 * Enhanced logger with structured logging, trace context, and backend integration
 * Provides comprehensive logging capabilities for debugging and monitoring
 */

import { loggingService } from '../services/loggingService';

const isProduction = import.meta.env.PROD;

export enum LogLevel {
  DEBUG = 'DEBUG',
  INFO = 'INFO',
  WARN = 'WARN',
  ERROR = 'ERROR',
  PERFORMANCE = 'PERFORMANCE',
}

export interface TraceContext {
  traceId: string;
  spanId: string;
  parentSpanId?: string;
  operationName?: string;
}

export interface LogEntry {
  timestamp: string;
  level: LogLevel;
  message: string;
  component?: string;
  action?: string;
  context?: Record<string, unknown>;
  traceContext?: TraceContext;
  correlationId?: string;
  error?: {
    name: string;
    message: string;
    stack?: string;
  };
}

/**
 * Enhanced logger interface with production-aware logging and trace context
 */
class Logger {
  private traceContext: TraceContext | null = null;
  private logBuffer: LogEntry[] = [];
  private readonly MAX_BUFFER_SIZE = 100;
  private sendTimeout: number | null = null;

  /**
   * Set trace context for distributed tracing
   */
  setTraceContext(context: TraceContext): void {
    this.traceContext = context;
  }

  /**
   * Get current trace context
   */
  getTraceContext(): TraceContext | null {
    return this.traceContext;
  }

  /**
   * Create a child span for nested operations
   */
  createChildSpan(operationName: string): TraceContext {
    const parentSpan = this.traceContext;
    return {
      traceId: parentSpan?.traceId || this.generateId(),
      spanId: this.generateId(),
      parentSpanId: parentSpan?.spanId,
      operationName,
    };
  }

  /**
   * Debug-level logging (suppressed in production)
   */
  debug(
    message: string,
    component?: string,
    action?: string,
    context?: Record<string, unknown>
  ): void {
    if (!isProduction) {
      loggingService.debug(message, component, action, context);
      this.bufferLog(LogLevel.DEBUG, message, component, action, context);
    }
  }

  /**
   * Info-level logging (always shown)
   */
  info(
    message: string,
    component?: string,
    action?: string,
    context?: Record<string, unknown>
  ): void {
    loggingService.info(message, component, action, context);
    this.bufferLog(LogLevel.INFO, message, component, action, context);
  }

  /**
   * Warning-level logging (always shown)
   */
  warn(
    message: string,
    component?: string,
    action?: string,
    context?: Record<string, unknown>
  ): void {
    loggingService.warn(message, component, action, context);
    this.bufferLog(LogLevel.WARN, message, component, action, context);
  }

  /**
   * Error-level logging (always shown, sent to backend)
   */
  error(
    message: string,
    error?: Error,
    component?: string,
    action?: string,
    context?: Record<string, unknown>
  ): void {
    loggingService.error(message, error, component, action, context);
    this.bufferLog(LogLevel.ERROR, message, component, action, context, error);

    // Send critical errors to backend immediately
    if (error) {
      this.sendErrorToBackend(message, error, component, context);
    }
  }

  /**
   * Log performance metrics with structured data
   */
  performance(
    operation: string,
    duration: number,
    component?: string,
    context?: Record<string, unknown>
  ): void {
    loggingService.performance(operation, duration, component, context);
    
    const perfContext = {
      ...context,
      operation,
      duration,
      durationMs: duration,
      category: this.getPerformanceCategory(duration),
    };

    this.bufferLog(LogLevel.PERFORMANCE, `Performance: ${operation}`, component, 'performance', perfContext);

    // Warn on slow operations
    if (duration > 3000) {
      this.warn(`Slow operation detected: ${operation} took ${duration}ms`, component, 'performance', perfContext);
    }
  }

  /**
   * Time an async operation and log the result
   */
  async timeOperation<T>(
    operation: string,
    fn: () => Promise<T>,
    component?: string,
    context?: Record<string, unknown>
  ): Promise<T> {
    const start = performance.now();
    const childSpan = this.createChildSpan(operation);
    const previousContext = this.traceContext;
    
    try {
      this.setTraceContext(childSpan);
      const result = await fn();
      const duration = performance.now() - start;
      this.performance(operation, duration, component, { ...context, success: true });
      return result;
    } catch (error) {
      const duration = performance.now() - start;
      this.performance(operation, duration, component, { ...context, success: false });
      throw error;
    } finally {
      this.setTraceContext(previousContext);
    }
  }

  /**
   * Create a scoped logger for a specific component
   */
  forComponent(component: string) {
    return {
      debug: (message: string, action?: string, context?: Record<string, unknown>) =>
        this.debug(message, component, action, context),
      info: (message: string, action?: string, context?: Record<string, unknown>) =>
        this.info(message, component, action, context),
      warn: (message: string, action?: string, context?: Record<string, unknown>) =>
        this.warn(message, component, action, context),
      error: (message: string, error?: Error, action?: string, context?: Record<string, unknown>) =>
        this.error(message, error, component, action, context),
      performance: (operation: string, duration: number, context?: Record<string, unknown>) =>
        this.performance(operation, duration, component, context),
      timeOperation: <T,>(operation: string, fn: () => Promise<T>, context?: Record<string, unknown>) =>
        this.timeOperation(operation, fn, component, context),
    };
  }

  /**
   * Buffer log entry for batch sending
   */
  private bufferLog(
    level: LogLevel,
    message: string,
    component?: string,
    action?: string,
    context?: Record<string, unknown>,
    error?: Error
  ): void {
    const entry: LogEntry = {
      timestamp: new Date().toISOString(),
      level,
      message,
      component,
      action,
      context,
      traceContext: this.traceContext || undefined,
      correlationId: this.getCorrelationId() || undefined,
      error: error ? {
        name: error.name,
        message: error.message,
        stack: error.stack,
      } : undefined,
    };

    this.logBuffer.push(entry);

    // Trim buffer if too large
    if (this.logBuffer.length > this.MAX_BUFFER_SIZE) {
      this.logBuffer = this.logBuffer.slice(-this.MAX_BUFFER_SIZE);
    }

    // Schedule batch send
    this.scheduleBatchSend();
  }

  /**
   * Schedule batch send of logs to backend
   */
  private scheduleBatchSend(): void {
    if (this.sendTimeout) {
      return; // Already scheduled
    }

    this.sendTimeout = window.setTimeout(() => {
      this.sendLogsToBackend();
      this.sendTimeout = null;
    }, 5000); // Send every 5 seconds
  }

  /**
   * Send buffered logs to backend in batch
   */
  private async sendLogsToBackend(): Promise<void> {
    if (this.logBuffer.length === 0) return;

    const logsToSend = [...this.logBuffer];
    this.logBuffer = [];

    try {
      const correlationId = this.getCorrelationId();
      
      await fetch('/api/logs/batch', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(correlationId && { 'X-Correlation-ID': correlationId }),
          ...(this.traceContext && {
            'X-Trace-ID': this.traceContext.traceId,
            'X-Span-ID': this.traceContext.spanId,
          }),
        },
        body: JSON.stringify({
          logs: logsToSend,
          clientInfo: {
            userAgent: navigator.userAgent,
            url: window.location.href,
            viewport: {
              width: window.innerWidth,
              height: window.innerHeight,
            },
          },
        }),
      });
    } catch (error) {
      // Silently fail - don't want log sending to crash the app
      console.error('Failed to send logs to backend:', error);
    }
  }

  /**
   * Send error to backend for immediate processing
   */
  private async sendErrorToBackend(
    message: string,
    error: Error,
    component?: string,
    context?: Record<string, unknown>
  ): Promise<void> {
    try {
      const correlationId = this.getCorrelationId();

      await fetch('/api/logs/error', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(correlationId && { 'X-Correlation-ID': correlationId }),
          ...(this.traceContext && {
            'X-Trace-ID': this.traceContext.traceId,
            'X-Span-ID': this.traceContext.spanId,
          }),
        },
        body: JSON.stringify({
          timestamp: new Date().toISOString(),
          error: {
            name: error.name,
            message: error.message,
            stack: error.stack,
          },
          context: {
            component,
            userMessage: message,
            ...context,
          },
          traceContext: this.traceContext,
          userAgent: navigator.userAgent,
          url: window.location.href,
        }),
      });
    } catch (sendError) {
      // Silently fail - don't want error reporting to crash the app
      console.error('Failed to send error to backend:', sendError);
    }
  }

  /**
   * Get correlation ID from current context
   */
  private getCorrelationId(): string | null {
    return sessionStorage.getItem('lastCorrelationId');
  }

  /**
   * Set correlation ID for tracking across operations
   */
  setCorrelationId(correlationId: string): void {
    sessionStorage.setItem('lastCorrelationId', correlationId);
  }

  /**
   * Get performance category based on duration
   */
  private getPerformanceCategory(duration: number): string {
    if (duration < 100) return 'Fast';
    if (duration < 1000) return 'Normal';
    if (duration < 3000) return 'Slow';
    return 'VerySlow';
  }

  /**
   * Generate a unique ID for tracing
   */
  private generateId(): string {
    return Math.random().toString(36).substring(2, 15) +
           Math.random().toString(36).substring(2, 15);
  }

  /**
   * Get recent logs for debugging
   */
  getRecentLogs(): LogEntry[] {
    return [...this.logBuffer];
  }

  /**
   * Clear log buffer
   */
  clearBuffer(): void {
    this.logBuffer = [];
  }
}

// Export singleton logger instance
export const logger = new Logger();

// Export type for external use
export type { Logger };
