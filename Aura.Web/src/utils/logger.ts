/**
 * Enhanced logger that wraps loggingService with production-aware logging levels
 * and correlation ID tracking
 */

import { loggingService } from '../services/loggingService';

const isProduction = import.meta.env.PROD;

/**
 * Enhanced logger interface with production-aware logging
 */
class Logger {
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
  }

  /**
   * Error-level logging (always shown, sent to backend in production)
   */
  error(
    message: string,
    error?: Error,
    component?: string,
    action?: string,
    context?: Record<string, unknown>
  ): void {
    loggingService.error(message, error, component, action, context);

    // In production, also send critical errors to backend
    if (isProduction && error) {
      this.sendErrorToBackend(message, error, component, context);
    }
  }

  /**
   * Log performance metrics
   */
  performance(
    operation: string,
    duration: number,
    component?: string,
    context?: Record<string, unknown>
  ): void {
    loggingService.performance(operation, duration, component, context);
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
    };
  }

  /**
   * Send error to backend for aggregation
   */
  private async sendErrorToBackend(
    message: string,
    error: Error,
    component?: string,
    context?: Record<string, unknown>
  ): Promise<void> {
    try {
      const correlationId = this.getCorrelationId();

      await fetch('/api/errors/report', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(correlationId && { 'X-Correlation-ID': correlationId }),
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
   * Can be enhanced to extract from ongoing API calls
   */
  private getCorrelationId(): string | null {
    // Check if there's a recent correlation ID in sessionStorage
    return sessionStorage.getItem('lastCorrelationId');
  }

  /**
   * Set correlation ID for tracking across operations
   */
  setCorrelationId(correlationId: string): void {
    sessionStorage.setItem('lastCorrelationId', correlationId);
  }
}

// Export singleton logger instance
export const logger = new Logger();

// Export type for external use
export type { Logger };
