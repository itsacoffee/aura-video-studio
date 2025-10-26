/**
 * Comprehensive Logging Service
 * Provides structured logging with different log levels, persistence, and error reporting
 */

export type LogLevel = 'debug' | 'info' | 'warn' | 'error';

export interface LogEntry {
  timestamp: string;
  level: LogLevel;
  component?: string;
  action?: string;
  message: string;
  context?: Record<string, unknown>;
  error?: {
    message: string;
    stack?: string;
    name?: string;
  };
  performance?: {
    duration: number;
    operation: string;
  };
}

export interface LoggingConfig {
  enableConsole: boolean;
  enablePersistence: boolean;
  maxStoredLogs: number;
  minLogLevel: LogLevel;
}

const LOG_STORAGE_KEY = 'app_logs';
const CONFIG_STORAGE_KEY = 'logging_config';
const MAX_LOGS_DEFAULT = 1000;

// Log level hierarchy for filtering
const LOG_LEVELS: Record<LogLevel, number> = {
  debug: 0,
  info: 1,
  warn: 2,
  error: 3,
};

class LoggingService {
  private config: LoggingConfig;
  private logs: LogEntry[] = [];
  private listeners: Array<(entry: LogEntry) => void> = [];

  constructor() {
    // Load config from localStorage or use defaults
    const savedConfig = localStorage.getItem(CONFIG_STORAGE_KEY);
    this.config = savedConfig
      ? JSON.parse(savedConfig)
      : {
          enableConsole: true,
          enablePersistence: true,
          maxStoredLogs: MAX_LOGS_DEFAULT,
          minLogLevel: 'info',
        };

    // Load existing logs from localStorage
    this.loadLogs();
  }

  /**
   * Update logging configuration
   */
  public configure(config: Partial<LoggingConfig>): void {
    this.config = { ...this.config, ...config };
    localStorage.setItem(CONFIG_STORAGE_KEY, JSON.stringify(this.config));
  }

  /**
   * Get current configuration
   */
  public getConfig(): LoggingConfig {
    return { ...this.config };
  }

  /**
   * Log a debug message
   */
  public debug(
    message: string,
    component?: string,
    action?: string,
    context?: Record<string, unknown>
  ): void {
    this.log('debug', message, component, action, context);
  }

  /**
   * Log an info message
   */
  public info(
    message: string,
    component?: string,
    action?: string,
    context?: Record<string, unknown>
  ): void {
    this.log('info', message, component, action, context);
  }

  /**
   * Log a warning message
   */
  public warn(
    message: string,
    component?: string,
    action?: string,
    context?: Record<string, unknown>
  ): void {
    this.log('warn', message, component, action, context);
  }

  /**
   * Log an error message
   */
  public error(
    message: string,
    error?: Error,
    component?: string,
    action?: string,
    context?: Record<string, unknown>
  ): void {
    const errorInfo = error
      ? {
          message: error.message,
          stack: error.stack,
          name: error.name,
        }
      : undefined;

    this.log('error', message, component, action, context, errorInfo);
  }

  /**
   * Log performance metric
   */
  public performance(
    operation: string,
    duration: number,
    component?: string,
    context?: Record<string, unknown>
  ): void {
    const message = `Performance: ${operation} took ${duration}ms`;
    this.log('info', message, component, 'performance', context, undefined, {
      operation,
      duration,
    });
  }

  /**
   * Core logging method
   */
  private log(
    level: LogLevel,
    message: string,
    component?: string,
    action?: string,
    context?: Record<string, unknown>,
    error?: { message: string; stack?: string; name?: string },
    performance?: { operation: string; duration: number }
  ): void {
    // Check if log level meets minimum threshold
    if (LOG_LEVELS[level] < LOG_LEVELS[this.config.minLogLevel]) {
      return;
    }

    const entry: LogEntry = {
      timestamp: new Date().toISOString(),
      level,
      message,
      component,
      action,
      context,
      error,
      performance,
    };

    // Add to in-memory logs
    this.logs.push(entry);

    // Trim logs if exceeding max
    if (this.logs.length > this.config.maxStoredLogs) {
      this.logs = this.logs.slice(-this.config.maxStoredLogs);
    }

    // Persist to localStorage if enabled
    if (this.config.enablePersistence) {
      this.saveLogs();
    }

    // Log to console if enabled
    if (this.config.enableConsole) {
      this.logToConsole(entry);
    }

    // Notify listeners
    this.notifyListeners(entry);
  }

  /**
   * Log entry to console with appropriate method
   */
  private logToConsole(entry: LogEntry): void {
    const prefix = `[${entry.timestamp}]${entry.component ? ` [${entry.component}]` : ''}${entry.action ? ` [${entry.action}]` : ''}`;

    switch (entry.level) {
      case 'error':
        console.error(prefix, entry.message, entry.error || '', entry.context || {});
        break;
      case 'warn':
        console.warn(prefix, entry.message, entry.context || {});
        break;
      case 'info':
        console.info(prefix, entry.message, entry.context || {});
        break;
      case 'debug':
      default:
        console.log(prefix, entry.message, entry.context || {});
        break;
    }
  }

  /**
   * Save logs to localStorage
   */
  private saveLogs(): void {
    try {
      // Only save last N logs to avoid localStorage quota issues
      const logsToSave = this.logs.slice(-this.config.maxStoredLogs);
      localStorage.setItem(LOG_STORAGE_KEY, JSON.stringify(logsToSave));
    } catch (error) {
      console.error('Failed to save logs to localStorage:', error);
    }
  }

  /**
   * Load logs from localStorage
   */
  private loadLogs(): void {
    try {
      const savedLogs = localStorage.getItem(LOG_STORAGE_KEY);
      if (savedLogs) {
        this.logs = JSON.parse(savedLogs);
      }
    } catch (error) {
      console.error('Failed to load logs from localStorage:', error);
      this.logs = [];
    }
  }

  /**
   * Get all logs
   */
  public getLogs(): LogEntry[] {
    return [...this.logs];
  }

  /**
   * Get logs filtered by level
   */
  public getLogsByLevel(level: LogLevel): LogEntry[] {
    return this.logs.filter((log) => log.level === level);
  }

  /**
   * Get logs filtered by component
   */
  public getLogsByComponent(component: string): LogEntry[] {
    return this.logs.filter((log) => log.component === component);
  }

  /**
   * Get logs in a time range
   */
  public getLogsByTimeRange(start: Date, end: Date): LogEntry[] {
    return this.logs.filter((log) => {
      const logTime = new Date(log.timestamp);
      return logTime >= start && logTime <= end;
    });
  }

  /**
   * Clear all logs
   */
  public clearLogs(): void {
    this.logs = [];
    localStorage.removeItem(LOG_STORAGE_KEY);
  }

  /**
   * Export logs as JSON string
   */
  public exportLogs(): string {
    return JSON.stringify(this.logs, null, 2);
  }

  /**
   * Subscribe to log events
   */
  public subscribe(listener: (entry: LogEntry) => void): () => void {
    this.listeners.push(listener);
    return () => {
      this.listeners = this.listeners.filter((l) => l !== listener);
    };
  }

  /**
   * Notify all listeners of a new log entry
   */
  private notifyListeners(entry: LogEntry): void {
    this.listeners.forEach((listener) => {
      try {
        listener(entry);
      } catch (error) {
        console.error('Error in log listener:', error);
      }
    });
  }

  /**
   * Create a scoped logger for a specific component
   */
  public createLogger(component: string) {
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
   * Measure performance of an async operation
   */
  public async measurePerformance<T>(
    operation: string,
    fn: () => Promise<T>,
    component?: string,
    context?: Record<string, unknown>
  ): Promise<T> {
    const startTime = performance.now();
    try {
      const result = await fn();
      const duration = performance.now() - startTime;
      this.performance(operation, duration, component, context);
      return result;
    } catch (error) {
      const duration = performance.now() - startTime;
      this.performance(operation, duration, component, {
        ...context,
        error: true,
      });
      throw error;
    }
  }

  /**
   * Measure performance of a sync operation
   */
  public measurePerformanceSync<T>(
    operation: string,
    fn: () => T,
    component?: string,
    context?: Record<string, unknown>
  ): T {
    const startTime = performance.now();
    try {
      const result = fn();
      const duration = performance.now() - startTime;
      this.performance(operation, duration, component, context);
      return result;
    } catch (error) {
      const duration = performance.now() - startTime;
      this.performance(operation, duration, component, {
        ...context,
        error: true,
      });
      throw error;
    }
  }
}

// Export singleton instance
export const loggingService = new LoggingService();

// Export helper for creating component loggers
export const createLogger = (component: string) => loggingService.createLogger(component);
