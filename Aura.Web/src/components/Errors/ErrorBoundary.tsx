import React, { Component, ErrorInfo, ReactNode } from 'react';
import { ErrorDialog } from './ErrorDialog';
import type { ErrorInfo as ErrorDialogInfo } from './ErrorDialog';

interface Props {
  children: ReactNode;
  fallback?: (error: Error, errorInfo: ErrorInfo) => ReactNode;
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
  showDialog: boolean;
}

/**
 * Error boundary component that catches React errors and displays user-friendly error dialog
 */
export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = {
      hasError: false,
      error: null,
      errorInfo: null,
      showDialog: false,
    };
  }

  static getDerivedStateFromError(error: Error): Partial<State> {
    return {
      hasError: true,
      error,
      showDialog: true,
    };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    // Log error to console
    console.error('ErrorBoundary caught an error:', error, errorInfo);

    // Store error info
    this.setState({ errorInfo });

    // Call custom error handler if provided
    this.props.onError?.(error, errorInfo);

    // Report error to backend
    this.reportErrorToBackend(error, errorInfo);
  }

  private async reportErrorToBackend(error: Error, errorInfo: ErrorInfo) {
    try {
      await fetch('/api/error-report', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          timestamp: new Date().toISOString(),
          error: {
            name: error.name,
            message: error.message,
            stack: error.stack,
          },
          componentStack: errorInfo.componentStack,
          userAgent: navigator.userAgent,
          url: window.location.href,
          context: {
            userAgent: navigator.userAgent,
            platform: navigator.platform,
            language: navigator.language,
          },
        }),
      });
    } catch (reportError) {
      console.error('Failed to report error to backend:', reportError);
    }
  }

  private convertToErrorDialogInfo(): ErrorDialogInfo {
    const { error, errorInfo } = this.state;
    
    return {
      title: 'Application Error',
      message: error?.message || 'An unexpected error occurred',
      severity: 'error',
      errorCode: 'REACT_ERROR',
      correlationId: this.generateCorrelationId(),
      technicalDetails: error?.message,
      stackTrace: error?.stack,
      suggestedActions: [
        'Refresh the page to try again',
        'Clear your browser cache and reload',
        'If the problem persists, please contact support',
      ],
      documentationLinks: [
        {
          title: 'Troubleshooting Guide',
          url: 'https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/Troubleshooting.md',
          description: 'General troubleshooting tips and solutions',
        },
      ],
      canRetry: true,
    };
  }

  private generateCorrelationId(): string {
    return `react-${Date.now()}-${Math.random().toString(36).substring(7)}`;
  }

  private handleRetry = () => {
    // Reload the page to retry
    window.location.reload();
  };

  private handleCloseDialog = () => {
    this.setState({ showDialog: false });
  };

  private handleExportDiagnostics = async () => {
    const { error, errorInfo } = this.state;
    
    const diagnostics = {
      timestamp: new Date().toISOString(),
      error: {
        name: error?.name,
        message: error?.message,
        stack: error?.stack,
      },
      componentStack: errorInfo?.componentStack,
      userAgent: navigator.userAgent,
      url: window.location.href,
      localStorage: this.getLocalStorageSummary(),
      sessionStorage: this.getSessionStorageSummary(),
    };

    const blob = new Blob([JSON.stringify(diagnostics, null, 2)], {
      type: 'application/json',
    });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `error-diagnostics-${Date.now()}.json`;
    a.click();
    URL.revokeObjectURL(url);
  };

  private getLocalStorageSummary(): Record<string, any> {
    try {
      const summary: Record<string, any> = {};
      for (let i = 0; i < localStorage.length; i++) {
        const key = localStorage.key(i);
        if (key) {
          // Only include non-sensitive keys
          if (!key.includes('token') && !key.includes('password') && !key.includes('secret')) {
            summary[key] = localStorage.getItem(key);
          }
        }
      }
      return summary;
    } catch {
      return { error: 'Could not access localStorage' };
    }
  }

  private getSessionStorageSummary(): Record<string, any> {
    try {
      const summary: Record<string, any> = {};
      for (let i = 0; i < sessionStorage.length; i++) {
        const key = sessionStorage.key(i);
        if (key) {
          // Only include non-sensitive keys
          if (!key.includes('token') && !key.includes('password') && !key.includes('secret')) {
            summary[key] = sessionStorage.getItem(key);
          }
        }
      }
      return summary;
    } catch {
      return { error: 'Could not access sessionStorage' };
    }
  }

  render() {
    const { hasError, error, errorInfo, showDialog } = this.state;
    const { children, fallback } = this.props;

    if (hasError && error && errorInfo) {
      if (fallback) {
        return fallback(error, errorInfo);
      }

      return (
        <>
          {/* Show the dialog */}
          <ErrorDialog
            open={showDialog}
            onClose={this.handleCloseDialog}
            error={this.convertToErrorDialogInfo()}
            onRetry={this.handleRetry}
            onExportDiagnostics={this.handleExportDiagnostics}
          />
          
          {/* Optionally show a fallback UI */}
          <div
            style={{
              padding: '20px',
              textAlign: 'center',
              minHeight: '100vh',
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            <h1>Something went wrong</h1>
            <p>We're sorry, but something unexpected happened.</p>
            <button
              onClick={this.handleRetry}
              style={{
                marginTop: '20px',
                padding: '10px 20px',
                fontSize: '16px',
                cursor: 'pointer',
              }}
            >
              Reload Page
            </button>
          </div>
        </>
      );
    }

    return children;
  }
}
