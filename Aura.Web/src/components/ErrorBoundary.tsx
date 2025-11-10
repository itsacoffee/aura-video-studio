import { Component, ErrorInfo, ReactNode } from 'react';
import { loggingService } from '../services/loggingService';
import { logger } from '../utils/logger';
import { ErrorFallback } from './ErrorBoundary/ErrorFallback';
import { ErrorReportDialog } from './ErrorReportDialog';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
  /**
   * Component name for logging context
   */
  componentName?: string;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
  showReportDialog: boolean;
  errorCount: number;
}

/**
 * Enhanced Error Boundary with comprehensive logging and tracing
 * Captures React errors, logs them with context, and sends critical errors to backend
 */
export class ErrorBoundary extends Component<Props, State> {
  private errorTimestamps: number[] = [];
  private readonly ERROR_THRESHOLD = 5; // Max errors in time window
  private readonly TIME_WINDOW = 10000; // 10 seconds

  public state: State = {
    hasError: false,
    error: null,
    errorInfo: null,
    showReportDialog: false,
    errorCount: 0,
  };

  public static getDerivedStateFromError(error: Error): Partial<State> {
    return {
      hasError: true,
      error,
    };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    const { componentName } = this.props;
    const now = Date.now();
    
    // Track error frequency to detect error loops
    this.errorTimestamps.push(now);
    this.errorTimestamps = this.errorTimestamps.filter(
      (timestamp) => now - timestamp < this.TIME_WINDOW
    );

    const errorCount = this.errorTimestamps.length;
    const isErrorLoop = errorCount >= this.ERROR_THRESHOLD;

    // Enhanced logging with structured context
    const errorContext = {
      componentStack: errorInfo.componentStack,
      componentName: componentName || 'Unknown',
      errorCount,
      isErrorLoop,
      traceContext: logger.getTraceContext(),
      url: window.location.href,
      timestamp: new Date().toISOString(),
    };

    // Log to console service
    loggingService.error(
      'ErrorBoundary caught an error',
      error,
      componentName || 'ErrorBoundary',
      'componentDidCatch',
      errorContext
    );

    // Log with enhanced logger (includes backend reporting)
    logger.error(
      isErrorLoop 
        ? `Error loop detected in ${componentName || 'component'} (${errorCount} errors in ${TIME_WINDOW}ms)`
        : `React error in ${componentName || 'component'}`,
      error,
      componentName || 'ErrorBoundary',
      'componentDidCatch',
      errorContext
    );

    // If error loop is detected, log critical warning
    if (isErrorLoop) {
      logger.error(
        'CRITICAL: Error loop detected - possible infinite re-render or recursive error',
        new Error('Error Loop Detected'),
        'ErrorBoundary',
        'errorLoop',
        {
          ...errorContext,
          recentErrors: this.errorTimestamps.map(ts => new Date(ts).toISOString()),
        }
      );
    }

    this.setState({
      error,
      errorInfo,
      errorCount,
    });
  }

  private handleReset = () => {
    const { componentName } = this.props;
    
    loggingService.info('User reset error boundary', componentName || 'ErrorBoundary', 'reset');
    logger.info(
      'Error boundary reset by user',
      componentName || 'ErrorBoundary',
      'reset',
      {
        previousErrorCount: this.state.errorCount,
        hadErrorLoop: this.errorTimestamps.length >= this.ERROR_THRESHOLD,
      }
    );

    // Clear error history on reset
    this.errorTimestamps = [];

    this.setState({
      hasError: false,
      error: null,
      errorInfo: null,
      showReportDialog: false,
      errorCount: 0,
    });
  };

  private handleReport = () => {
    const { componentName } = this.props;
    
    loggingService.info('User opened error report dialog', componentName || 'ErrorBoundary', 'report');
    logger.info(
      'User initiated error report',
      componentName || 'ErrorBoundary',
      'report',
      {
        errorName: this.state.error?.name,
        errorMessage: this.state.error?.message,
      }
    );

    this.setState({
      showReportDialog: true,
    });
  };

  public render() {
    if (this.state.hasError && this.state.error && this.state.errorInfo) {
      if (this.props.fallback) {
        return this.props.fallback;
      }

      // If we're in an error loop, show a more severe error message
      const isErrorLoop = this.errorTimestamps.length >= this.ERROR_THRESHOLD;

      return (
        <>
          <ErrorFallback
            error={this.state.error}
            errorInfo={this.state.errorInfo}
            onReset={this.handleReset}
            onReport={this.handleReport}
            isErrorLoop={isErrorLoop}
          />
          <ErrorReportDialog
            open={this.state.showReportDialog}
            onOpenChange={(open) => this.setState({ showReportDialog: open })}
            error={this.state.error}
            errorInfo={{
              componentStack: this.state.errorInfo.componentStack || undefined,
            }}
          />
        </>
      );
    }

    return this.props.children;
  }
}
