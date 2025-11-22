/**
 * Error Boundary Component
 * Catches React errors and displays fallback UI
 */

import React, { Component, ErrorInfo, ReactNode } from 'react';
import { loggingService } from '../services/loggingService';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
  errorId: string;
}

export class ErrorBoundary extends Component<Props, State> {
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
    return {
      hasError: true,
      error,
    };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    // Generate unique error ID for tracking
    const errorId = `ERR-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;

    // Log error to logging service
    loggingService.error(
      'React Error Boundary caught an error',
      error,
      'ErrorBoundary',
      'componentDidCatch',
      {
        componentStack: errorInfo.componentStack,
        errorId,
      }
    );

    // Update state with error info
    this.setState({
      errorInfo,
      errorId,
    });

    // Call custom error handler if provided
    this.props.onError?.(error, errorInfo);
  }

  handleReset = (): void => {
    this.setState({
      hasError: false,
      error: null,
      errorInfo: null,
      errorId: '',
    });
  };

  render(): ReactNode {
    if (this.state.hasError) {
      // Custom fallback UI if provided
      if (this.props.fallback) {
        return this.props.fallback;
      }

      // Ensure we ALWAYS render something visible
      return (
        <div
          style={{
            minHeight: '100vh',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            padding: '20px',
            backgroundColor: '#1e1e1e',
            color: '#ffffff',
          }}
        >
          <div
            style={{
              maxWidth: '600px',
              width: '100%',
              background: '#2d2d2d',
              border: '2px solid #ff4444',
              borderRadius: '8px',
              padding: '32px',
              boxShadow: '0 8px 32px rgba(0,0,0,0.4)',
            }}
          >
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '12px',
                marginBottom: '20px',
              }}
            >
              <span style={{ fontSize: '32px' }}>⚠️</span>
              <h1
                style={{
                  margin: 0,
                  fontSize: '24px',
                  fontWeight: 600,
                }}
              >
                Application Error
              </h1>
            </div>

            <p style={{ marginBottom: '16px', lineHeight: '1.6' }}>
              The application encountered an unexpected error during initialization.
            </p>

            <div
              style={{
                marginBottom: '16px',
                padding: '12px',
                backgroundColor: '#1a1a1a',
                borderRadius: '4px',
                fontSize: '13px',
                color: '#aaa',
              }}
            >
              <strong>Error ID:</strong> {this.state.errorId}
            </div>

            {this.state.error && (
              <details style={{ marginBottom: '20px' }}>
                <summary
                  style={{
                    cursor: 'pointer',
                    marginBottom: '8px',
                    fontSize: '14px',
                    color: '#888',
                  }}
                >
                  Technical Details
                </summary>
                <pre
                  style={{
                    padding: '12px',
                    backgroundColor: '#1a1a1a',
                    borderRadius: '4px',
                    fontSize: '12px',
                    overflow: 'auto',
                    maxHeight: '200px',
                    color: '#ff6b6b',
                  }}
                >
                  {this.state.error.message}
                  {'\n\n'}
                  {this.state.error.stack}
                </pre>
              </details>
            )}

            <div style={{ display: 'flex', gap: '12px', flexWrap: 'wrap' }}>
              <button
                style={{
                  padding: '10px 20px',
                  backgroundColor: '#0078d4',
                  color: 'white',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: 'pointer',
                  fontSize: '14px',
                  fontWeight: 600,
                }}
                onClick={() => window.location.reload()}
              >
                Reload Application
              </button>
              <button
                style={{
                  padding: '10px 20px',
                  backgroundColor: '#444',
                  color: 'white',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: 'pointer',
                  fontSize: '14px',
                }}
                onClick={this.handleReset}
              >
                Try to Recover
              </button>
            </div>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}

// Re-export CrashRecoveryScreen for backwards compatibility
export { CrashRecoveryScreen } from './ErrorBoundary/CrashRecoveryScreen';
