/**
 * Global Error Boundary
 * Catches unhandled errors in the entire application
 */

import { Button } from '@fluentui/react-components';
import { Component, ReactNode } from 'react';
import { loggingService } from '../../services/loggingService';

interface Props {
  children: ReactNode;
  fallback?: (error: Error, errorInfo: React.ErrorInfo, reset: () => void) => ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: React.ErrorInfo | null;
}

/**
 * Global error boundary component
 */
export class GlobalErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = {
      hasError: false,
      error: null,
      errorInfo: null,
    };
  }

  static getDerivedStateFromError(error: Error): Partial<State> {
    return {
      hasError: true,
      error,
    };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo): void {
    // Log error to logging service
    loggingService.error('Uncaught error in application', error, 'ErrorBoundary', 'global', {
      componentStack: errorInfo.componentStack,
    });

    this.setState({
      errorInfo,
    });
  }

  handleReset = (): void => {
    this.setState({
      hasError: false,
      error: null,
      errorInfo: null,
    });
  };

  render(): ReactNode {
    const { hasError, error, errorInfo } = this.state;
    const { children, fallback } = this.props;

    if (hasError && error) {
      // Use custom fallback if provided
      if (fallback && errorInfo) {
        return fallback(error, errorInfo, this.handleReset);
      }

      // Default error UI
      return (
        <div
          style={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            minHeight: '100vh',
            padding: '2rem',
            textAlign: 'center',
          }}
        >
          <h1 style={{ fontSize: '2rem', marginBottom: '1rem', color: '#d13438' }}>
            Something went wrong
          </h1>
          <p style={{ marginBottom: '1rem', maxWidth: '600px' }}>
            An unexpected error occurred in the application. The error has been logged and
            we&apos;ll look into it.
          </p>
          <details style={{ marginBottom: '1rem', textAlign: 'left', maxWidth: '600px' }}>
            <summary style={{ cursor: 'pointer', marginBottom: '0.5rem' }}>
              Error Details (for developers)
            </summary>
            <pre
              style={{
                padding: '1rem',
                background: '#f5f5f5',
                borderRadius: '4px',
                overflow: 'auto',
                fontSize: '0.875rem',
              }}
            >
              {error.toString()}
              {errorInfo?.componentStack}
            </pre>
          </details>
          <div style={{ display: 'flex', gap: '1rem' }}>
            <Button appearance="primary" onClick={this.handleReset}>
              Try Again
            </Button>
            <Button appearance="secondary" onClick={() => window.location.reload()}>
              Reload Page
            </Button>
          </div>
        </div>
      );
    }

    return children;
  }
}
