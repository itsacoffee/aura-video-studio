/**
 * Global Error Boundary
 * Catches unhandled errors in the entire application
 */

import React, { Component, ReactNode } from 'react';
import { apiUrl } from '../../config/api';
import { loggingService } from '../../services/loggingService';
import { EnhancedErrorFallback } from './EnhancedErrorFallback';

interface Props {
  children: ReactNode;
  fallback?: (error: Error, errorInfo: React.ErrorInfo, reset: () => void) => ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: React.ErrorInfo | null;
  errorId: string;
}

export class GlobalErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = {
      hasError: false,
      error: null,
      errorInfo: null,
      errorId: '',
    };
  }

  static getDerivedStateFromError(error: Error): Partial<State> {
    // Use crypto.randomUUID for better uniqueness (or fallback to timestamp + random)
    const errorId =
      typeof crypto !== 'undefined' && crypto.randomUUID
        ? `ERR_${Date.now()}_${crypto.randomUUID().substring(0, 9)}`
        : `ERR_${Date.now()}_${Math.random().toString(36).substring(2, 11)}`;

    // Log to console for immediate debugging
    console.error(`[ErrorBoundary] ${errorId}:`, error);

    return {
      hasError: true,
      error,
      errorId,
    };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo): void {
    // Log to local logging service
    loggingService.error('Uncaught error in application', error, 'ErrorBoundary', 'global', {
      componentStack: errorInfo.componentStack,
      errorId: this.state.errorId,
    });

    // Log component stack for debugging
    console.error('[ErrorBoundary] Component Stack:', errorInfo.componentStack);

    // Send error to backend logging service
    this.logErrorToService(error, errorInfo);

    this.setState({
      errorInfo,
    });
  }

  /**
   * Send error details to backend logging service
   */
  private logErrorToService = async (error: Error, errorInfo: React.ErrorInfo): Promise<void> => {
    try {
      await fetch(apiUrl('/api/logs/error'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          timestamp: new Date().toISOString(),
          error: {
            name: error.name,
            message: error.message,
            stack: error.stack,
          },
          context: {
            errorId: this.state.errorId,
            componentStack: errorInfo.componentStack,
          },
          userAgent: navigator.userAgent,
          url: window.location.href,
        }),
      });
    } catch (logError) {
      // Don't let logging errors break the error boundary
      console.error('[ErrorBoundary] Failed to log error to service:', logError);
    }
  };

  handleReset = (): void => {
    this.setState({
      hasError: false,
      error: null,
      errorInfo: null,
      errorId: '',
    });
  };

  render(): ReactNode {
    const { hasError, error, errorInfo, errorId } = this.state;
    const { children, fallback } = this.props;

    if (hasError && error) {
      if (fallback && errorInfo) {
        return fallback(error, errorInfo, this.handleReset);
      }

      return (
        <EnhancedErrorFallback
          error={error}
          errorInfo={errorInfo ?? undefined}
          errorCode={errorId}
          reset={this.handleReset}
          showDetails={true}
        />
      );
    }

    return children;
  }
}
