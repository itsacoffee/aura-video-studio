import type { ErrorInfo } from '../components/Errors/ErrorDialog';

/**
 * Client-side error handling service for managing errors and recovery
 */
class ErrorHandlingService {
  private errorQueue: QueuedError[] = [];
  private isProcessing = false;
  private readonly maxQueueSize = 100;

  /**
   * Handle an error with optional recovery attempts
   */
  async handleError(
    error: Error,
    context?: ErrorContext,
    options?: ErrorHandlingOptions
  ): Promise<ErrorHandlingResult> {
    const queuedError: QueuedError = {
      error,
      context: context || {},
      timestamp: new Date(),
      correlationId: this.generateCorrelationId(),
      attempts: 0,
    };

    // Add to queue
    this.errorQueue.push(queuedError);
    if (this.errorQueue.length > this.maxQueueSize) {
      this.errorQueue.shift(); // Remove oldest
    }

    // Log to console
    console.error('[ErrorHandling]', error, context);

    // Report to backend if enabled
    if (options?.reportToBackend !== false) {
      await this.reportToBackend(queuedError);
    }

    // Attempt recovery if specified
    if (options?.attemptRecovery && queuedError.attempts < (options.maxRetries || 3)) {
      return await this.attemptRecovery(queuedError, options);
    }

    return {
      success: false,
      error: queuedError,
      errorInfo: this.convertToErrorInfo(queuedError),
    };
  }

  /**
   * Attempt to recover from an error
   */
  private async attemptRecovery(
    queuedError: QueuedError,
    options: ErrorHandlingOptions
  ): Promise<ErrorHandlingResult> {
    queuedError.attempts++;

    try {
      if (options.recoveryStrategy) {
        await options.recoveryStrategy(queuedError.error);
        return {
          success: true,
          recovered: true,
          error: queuedError,
        };
      }

      // Default recovery strategies
      if (this.isNetworkError(queuedError.error)) {
        await this.delay(Math.pow(2, queuedError.attempts) * 1000);
        return {
          success: true,
          recovered: true,
          error: queuedError,
          message: 'Network error recovered after retry',
        };
      }

      return {
        success: false,
        error: queuedError,
        errorInfo: this.convertToErrorInfo(queuedError),
      };
    } catch (recoveryError) {
      console.error('[ErrorHandling] Recovery failed:', recoveryError);
      return {
        success: false,
        error: queuedError,
        errorInfo: this.convertToErrorInfo(queuedError),
      };
    }
  }

  /**
   * Convert error to user-friendly ErrorInfo
   */
  convertToErrorInfo(queuedError: QueuedError): ErrorInfo {
    const error = queuedError.error;
    const context = queuedError.context;

    // Determine error severity and category
    const severity = this.determineSeverity(error);
    const category = this.categorizeError(error);

    // Generate suggested actions
    const suggestedActions = this.generateSuggestedActions(error, category);

    // Generate troubleshooting steps
    const troubleshootingSteps = this.generateTroubleshootingSteps(error, category);

    // Generate documentation links
    const documentationLinks = this.generateDocumentationLinks(error, category);

    return {
      title: this.getErrorTitle(error, category),
      message: error.message || 'An unexpected error occurred',
      severity,
      errorCode: (error as any).errorCode || category,
      correlationId: queuedError.correlationId,
      suggestedActions,
      troubleshootingSteps,
      documentationLinks,
      technicalDetails: error.stack,
      stackTrace: error.stack,
      canRetry: this.canRetry(error),
      isTransient: this.isTransient(error),
      automatedRecovery: this.getAutomatedRecoveryOption(error),
    };
  }

  /**
   * Report error to backend
   */
  private async reportToBackend(queuedError: QueuedError): Promise<void> {
    try {
      await fetch('/api/error-report', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          timestamp: queuedError.timestamp.toISOString(),
          error: {
            name: queuedError.error.name,
            message: queuedError.error.message,
            stack: queuedError.error.stack,
          },
          context: queuedError.context,
          correlationId: queuedError.correlationId,
          userAgent: navigator.userAgent,
          url: window.location.href,
        }),
      });
    } catch (reportError) {
      console.error('[ErrorHandling] Failed to report error:', reportError);
    }
  }

  /**
   * Get recent errors from queue
   */
  getRecentErrors(count: number = 10): QueuedError[] {
    return this.errorQueue.slice(-count);
  }

  /**
   * Clear error queue
   */
  clearErrorQueue(): void {
    this.errorQueue = [];
  }

  /**
   * Export diagnostics
   */
  async exportDiagnostics(): Promise<void> {
    const diagnostics = {
      timestamp: new Date().toISOString(),
      errors: this.errorQueue.map((e) => ({
        timestamp: e.timestamp.toISOString(),
        error: {
          name: e.error.name,
          message: e.error.message,
          stack: e.error.stack,
        },
        context: e.context,
        correlationId: e.correlationId,
        attempts: e.attempts,
      })),
      system: {
        userAgent: navigator.userAgent,
        platform: navigator.platform,
        language: navigator.language,
        online: navigator.onLine,
        cookieEnabled: navigator.cookieEnabled,
      },
    };

    const blob = new Blob([JSON.stringify(diagnostics, null, 2)], {
      type: 'application/json',
    });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `diagnostics-${Date.now()}.json`;
    a.click();
    URL.revokeObjectURL(url);
  }

  private generateCorrelationId(): string {
    return `${Date.now()}-${Math.random().toString(36).substring(7)}`;
  }

  private determineSeverity(error: Error): 'error' | 'warning' | 'info' | 'critical' {
    if (error.name === 'NetworkError' || this.isNetworkError(error)) {
      return 'warning';
    }
    if (error.message.includes('fatal') || error.message.includes('critical')) {
      return 'critical';
    }
    return 'error';
  }

  private categorizeError(error: Error): string {
    if (this.isNetworkError(error)) return 'NETWORK_ERROR';
    if (error.name === 'ValidationError') return 'VALIDATION_ERROR';
    if (error.message.includes('provider') || error.message.includes('API')) return 'PROVIDER_ERROR';
    if (error.message.includes('render') || error.message.includes('FFmpeg')) return 'RENDER_ERROR';
    return 'UNKNOWN_ERROR';
  }

  private isNetworkError(error: Error): boolean {
    return (
      error.name === 'NetworkError' ||
      error.message.includes('network') ||
      error.message.includes('fetch') ||
      error.message.includes('timeout') ||
      (error as any).code === 'NETWORK_ERROR'
    );
  }

  private canRetry(error: Error): boolean {
    return this.isNetworkError(error) || this.isTransient(error);
  }

  private isTransient(error: Error): boolean {
    return (
      this.isNetworkError(error) ||
      error.message.includes('timeout') ||
      error.message.includes('temporary') ||
      (error as any).isTransient === true
    );
  }

  private getErrorTitle(error: Error, category: string): string {
    switch (category) {
      case 'NETWORK_ERROR':
        return 'Network Connection Error';
      case 'PROVIDER_ERROR':
        return 'Provider Error';
      case 'RENDER_ERROR':
        return 'Rendering Error';
      case 'VALIDATION_ERROR':
        return 'Validation Error';
      default:
        return 'Application Error';
    }
  }

  private generateSuggestedActions(error: Error, category: string): string[] {
    switch (category) {
      case 'NETWORK_ERROR':
        return [
          'Check your internet connection',
          'Verify firewall settings',
          'Try again in a few moments',
          'Contact support if the issue persists',
        ];
      case 'PROVIDER_ERROR':
        return [
          'Check provider configuration in Settings',
          'Verify API keys are valid',
          'Try a different provider',
          'Check provider service status',
        ];
      case 'RENDER_ERROR':
        return [
          'Check FFmpeg installation in Settings',
          'Verify input files are valid',
          'Try with different render settings',
          'Check system resources',
        ];
      default:
        return [
          'Try the operation again',
          'Refresh the page',
          'Check application logs',
          'Contact support if needed',
        ];
    }
  }

  private generateTroubleshootingSteps(error: Error, category: string) {
    switch (category) {
      case 'NETWORK_ERROR':
        return [
          {
            step: 1,
            title: 'Check Internet Connection',
            description: 'Verify that your device is connected to the internet',
            actions: ['Open a browser and navigate to a website', 'Check WiFi or ethernet connection'],
          },
          {
            step: 2,
            title: 'Check Firewall Settings',
            description: 'Ensure the application is not blocked by firewall',
            actions: ['Check Windows Firewall settings', 'Verify antivirus is not blocking'],
          },
        ];
      case 'PROVIDER_ERROR':
        return [
          {
            step: 1,
            title: 'Verify API Configuration',
            description: 'Check that provider API keys are correctly configured',
            actions: ['Open Settings → Providers', 'Verify API key is set', 'Test connection'],
          },
        ];
      default:
        return [];
    }
  }

  private generateDocumentationLinks(error: Error, category: string) {
    const links = [
      {
        title: 'Troubleshooting Guide',
        url: 'https://docs.aura.studio/troubleshooting',
        description: 'General troubleshooting tips and solutions',
      },
    ];

    if (category === 'NETWORK_ERROR') {
      links.push({
        title: 'Network Configuration',
        url: 'https://docs.aura.studio/network',
        description: 'How to configure network and proxy settings',
      });
    } else if (category === 'PROVIDER_ERROR') {
      links.push({
        title: 'Provider Configuration Guide',
        url: 'https://docs.aura.studio/providers',
        description: 'Learn how to configure and troubleshoot providers',
      });
    }

    return links;
  }

  private getAutomatedRecoveryOption(error: Error) {
    if (this.isNetworkError(error)) {
      return {
        name: 'RetryWithBackoff',
        description: 'Automatically retry with exponential backoff',
        estimatedTimeSeconds: 15,
      };
    }
    return undefined;
  }

  private delay(ms: number): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }
}

// Types
export interface QueuedError {
  error: Error;
  context: ErrorContext;
  timestamp: Date;
  correlationId: string;
  attempts: number;
}

export interface ErrorContext {
  [key: string]: any;
}

export interface ErrorHandlingOptions {
  reportToBackend?: boolean;
  attemptRecovery?: boolean;
  maxRetries?: number;
  recoveryStrategy?: (error: Error) => Promise<void>;
}

export interface ErrorHandlingResult {
  success: boolean;
  recovered?: boolean;
  error: QueuedError;
  errorInfo?: ErrorInfo;
  message?: string;
}

// Export singleton instance
export const errorHandlingService = new ErrorHandlingService();
