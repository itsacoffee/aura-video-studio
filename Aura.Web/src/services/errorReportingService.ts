/**
 * Error Reporting Service
 * Manages error severity levels, user-friendly notifications, and error reporting
 */

import { loggingService, LogEntry } from './loggingService';

export type ErrorSeverity = 'info' | 'warning' | 'error' | 'critical';

export interface ErrorReport {
  id: string;
  severity: ErrorSeverity;
  title: string;
  message: string;
  technicalDetails?: string;
  timestamp: string;
  userAction?: string;
  browserInfo: {
    userAgent: string;
    platform: string;
    language: string;
  };
  appState?: Record<string, unknown>;
  logs?: LogEntry[];
  stackTrace?: string;
}

export interface ErrorNotification {
  id: string;
  severity: ErrorSeverity;
  title: string;
  message: string;
  actions?: ErrorAction[];
  dismissible: boolean;
  autoHide: boolean;
  duration?: number; // ms
}

export interface ErrorAction {
  label: string;
  handler: () => void;
  primary?: boolean;
}

class ErrorReportingService {
  private errorQueue: ErrorReport[] = [];
  private notificationListeners: Array<(notification: ErrorNotification) => void> = [];
  private maxQueueSize = 50;

  /**
   * Report an error with specific severity
   */
  public reportError(
    severity: ErrorSeverity,
    title: string,
    message: string,
    error?: Error,
    context?: {
      userAction?: string;
      appState?: Record<string, unknown>;
    }
  ): string {
    const errorId = this.generateErrorId();

    const report: ErrorReport = {
      id: errorId,
      severity,
      title,
      message,
      technicalDetails: error?.message,
      timestamp: new Date().toISOString(),
      userAction: context?.userAction,
      browserInfo: {
        userAgent: navigator.userAgent,
        platform: navigator.platform,
        language: navigator.language,
      },
      appState: context?.appState,
      logs: loggingService.getLogs().slice(-20), // Last 20 logs
      stackTrace: error?.stack,
    };

    // Add to queue
    this.errorQueue.push(report);
    if (this.errorQueue.length > this.maxQueueSize) {
      this.errorQueue.shift();
    }

    // Log the error
    loggingService.error(
      `[${severity.toUpperCase()}] ${title}: ${message}`,
      error,
      'errorReportingService',
      'reportError',
      {
        errorId,
        severity,
        userAction: context?.userAction,
      }
    );

    return errorId;
  }

  /**
   * Show a user-friendly error notification
   */
  public showNotification(
    severity: ErrorSeverity,
    title: string,
    message: string,
    options?: {
      actions?: ErrorAction[];
      dismissible?: boolean;
      autoHide?: boolean;
      duration?: number;
    }
  ): string {
    const notificationId = this.generateErrorId();

    const notification: ErrorNotification = {
      id: notificationId,
      severity,
      title,
      message,
      actions: options?.actions,
      dismissible: options?.dismissible ?? true,
      autoHide: options?.autoHide ?? (severity !== 'error' && severity !== 'critical'),
      duration:
        options?.duration ??
        (severity === 'info' ? 5000 : severity === 'warning' ? 8000 : undefined),
    };

    // Notify all listeners
    this.notificationListeners.forEach((listener) => {
      try {
        listener(notification);
      } catch (error) {
        loggingService.error(
          'Error in notification listener',
          error as Error,
          'errorReportingService',
          'showNotification'
        );
      }
    });

    loggingService.info(
      `Notification shown: ${title}`,
      'errorReportingService',
      'showNotification',
      { severity, notificationId }
    );

    return notificationId;
  }

  /**
   * Report and show notification for an info message
   */
  public info(
    title: string,
    message: string,
    options?: {
      actions?: ErrorAction[];
      duration?: number;
    }
  ): void {
    this.showNotification('info', title, message, {
      actions: options?.actions,
      autoHide: true,
      dismissible: true,
      duration: options?.duration ?? 5000,
    });
  }

  /**
   * Report and show notification for a warning
   */
  public warning(
    title: string,
    message: string,
    options?: {
      actions?: ErrorAction[];
      duration?: number;
    }
  ): void {
    this.showNotification('warning', title, message, {
      actions: options?.actions,
      autoHide: true,
      dismissible: true,
      duration: options?.duration ?? 8000,
    });
  }

  /**
   * Report and show notification for an error
   */
  public error(
    title: string,
    message: string,
    error?: Error,
    options?: {
      actions?: ErrorAction[];
      userAction?: string;
      appState?: Record<string, unknown>;
    }
  ): void {
    this.reportError('error', title, message, error, {
      userAction: options?.userAction,
      appState: options?.appState,
    });

    this.showNotification('error', title, message, {
      actions: options?.actions,
      autoHide: false,
      dismissible: true,
    });
  }

  /**
   * Report and show notification for a critical error
   */
  public critical(
    title: string,
    message: string,
    error?: Error,
    options?: {
      actions?: ErrorAction[];
      userAction?: string;
      appState?: Record<string, unknown>;
    }
  ): void {
    this.reportError('critical', title, message, error, {
      userAction: options?.userAction,
      appState: options?.appState,
    });

    this.showNotification('critical', title, message, {
      actions: options?.actions,
      autoHide: false,
      dismissible: false,
    });
  }

  /**
   * Add a listener for error notifications
   */
  public addNotificationListener(listener: (notification: ErrorNotification) => void): void {
    this.notificationListeners.push(listener);
  }

  /**
   * Remove a notification listener
   */
  public removeNotificationListener(listener: (notification: ErrorNotification) => void): void {
    const index = this.notificationListeners.indexOf(listener);
    if (index !== -1) {
      this.notificationListeners.splice(index, 1);
    }
  }

  /**
   * Get all error reports
   */
  public getErrorReports(): ErrorReport[] {
    return [...this.errorQueue];
  }

  /**
   * Get a specific error report by ID
   */
  public getErrorReport(errorId: string): ErrorReport | null {
    return this.errorQueue.find((r) => r.id === errorId) || null;
  }

  /**
   * Clear all error reports
   */
  public clearErrorReports(): void {
    this.errorQueue = [];
  }

  /**
   * Submit an error report to the server
   */
  public async submitErrorReport(errorId: string, userDescription?: string): Promise<boolean> {
    const report = this.getErrorReport(errorId);
    if (!report) {
      loggingService.warn(
        `Error report ${errorId} not found`,
        'errorReportingService',
        'submitErrorReport'
      );
      return false;
    }

    try {
      const response = await fetch('/api/error-report', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          ...report,
          userDescription,
        }),
      });

      if (!response.ok) {
        throw new Error(`Failed to submit error report: ${response.statusText}`);
      }

      loggingService.info(
        `Error report ${errorId} submitted successfully`,
        'errorReportingService',
        'submitErrorReport'
      );

      return true;
    } catch (error) {
      loggingService.error(
        'Failed to submit error report',
        error as Error,
        'errorReportingService',
        'submitErrorReport',
        { errorId }
      );
      return false;
    }
  }

  /**
   * Generate a unique error ID
   */
  private generateErrorId(): string {
    return `err-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
  }

  /**
   * Get error color based on severity
   */
  public getSeverityColor(severity: ErrorSeverity): string {
    switch (severity) {
      case 'info':
        return 'blue';
      case 'warning':
        return 'yellow';
      case 'error':
        return 'red';
      case 'critical':
        return 'darkred';
      default:
        return 'gray';
    }
  }

  /**
   * Get error icon based on severity
   */
  public getSeverityIcon(severity: ErrorSeverity): string {
    switch (severity) {
      case 'info':
        return 'Info';
      case 'warning':
        return 'Warning';
      case 'error':
        return 'ErrorCircle';
      case 'critical':
        return 'StatusErrorFull';
      default:
        return 'Info';
    }
  }
}

// Export singleton instance
export const errorReportingService = new ErrorReportingService();
