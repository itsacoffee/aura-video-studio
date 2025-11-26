/**
 * Enhanced Error Boundary for Electron with Backend Restart Support
 * Detects backend connection failures and provides Electron-specific recovery
 */

import React, { Component, ErrorInfo, ReactNode } from 'react';

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
  isBackendError: boolean;
  isElectron: boolean;
}

export class ElectronErrorBoundary extends Component<Props, State> {
  public state: State = {
    hasError: false,
    error: null,
    errorInfo: null,
    isBackendError: false,
    isElectron: false,
  };

  public static getDerivedStateFromError(error: Error): Partial<State> {
    // Check if error is backend-related
    const isBackendError =
      error.message.includes('Network Error') ||
      error.message.includes('timeout') ||
      error.message.includes('Backend') ||
      error.message.includes('ECONNREFUSED');

    // Check if running in Electron
    const isElectron = !!(
      typeof window !== 'undefined' &&
      (window.aura || window.desktopBridge || window.electron)
    );

    return {
      hasError: true,
      error,
      isBackendError,
      isElectron,
    };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('ElectronErrorBoundary caught an error:', error, errorInfo);
    this.setState({
      error,
      errorInfo,
    });
  }

  private handleRetry = async () => {
    const { isElectron, isBackendError } = this.state;

    // If running in Electron and it's a backend error, try to restart backend
    if (isElectron && isBackendError) {
      try {
        // Try multiple Electron API paths for backend restart
        if (window.aura?.backend?.restart) {
          const result = await window.aura.backend.restart();
          if (result) {
            this.setState({ hasError: false, error: null, errorInfo: null });
            window.location.reload();
            return;
          }
        } else if (window.electron?.backend?.restart) {
          const result = (await window.electron.backend.restart()) as { success?: boolean; error?: string } | undefined;
          if (result && result.success) {
            this.setState({ hasError: false, error: null, errorInfo: null });
            window.location.reload();
            return;
          } else if (result) {
            alert(`Failed to restart backend: ${result.error}`);
          }
        } else if (window.electronAPI?.restartBackend) {
          const result = (await window.electronAPI.restartBackend()) as { success?: boolean; error?: string } | undefined;
          if (result && result.success) {
            this.setState({ hasError: false, error: null, errorInfo: null });
            window.location.reload();
            return;
          } else if (result) {
            alert(`Failed to restart backend: ${result.error}`);
          }
        }
      } catch (error) {
        console.error('Failed to restart backend:', error);
        alert(
          `Backend restart failed: ${error instanceof Error ? error.message : String(error)}`
        );
      }
    } else {
      // In browser, just reload
      window.location.reload();
    }
  };

  public render() {
    if (this.state.hasError) {
      const { isElectron, isBackendError, error, errorInfo } = this.state;

      return (
        <div style={{ padding: '2rem', maxWidth: '800px', margin: '0 auto' }}>
          <h1>Backend Server Not Reachable</h1>
          <p>The Aura backend server could not be reached after multiple attempts.</p>

          {isElectron ? (
            <div>
              <h2>Running in Desktop App Mode</h2>
              <p>The backend should auto-start automatically. If you see this error:</p>
              <ul>
                <li>Check the application logs for errors</li>
                <li>Try clicking "Restart Backend" below</li>
                <li>If the problem persists, try restarting the application</li>
              </ul>
              <button
                onClick={this.handleRetry}
                style={{
                  padding: '0.5rem 1rem',
                  marginTop: '1rem',
                  backgroundColor: '#0078d4',
                  color: 'white',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: 'pointer',
                  fontSize: '14px',
                }}
              >
                Restart Backend
              </button>
            </div>
          ) : (
            <div>
              <h2>Running in Browser Mode (Development)</h2>
              <p>Please ensure the backend is running:</p>
              <ol>
                <li>Navigate to the project root directory in your terminal</li>
                <li>
                  Run: <code>dotnet run --project Aura.Api</code>
                </li>
                <li>Wait for the message "Application started. Press Ctrl+C to shut down."</li>
                <li>Then click "Retry" below</li>
              </ol>
              <button
                onClick={this.handleRetry}
                style={{
                  padding: '0.5rem 1rem',
                  marginTop: '1rem',
                  backgroundColor: '#0078d4',
                  color: 'white',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: 'pointer',
                  fontSize: '14px',
                }}
              >
                Retry Connection
              </button>
            </div>
          )}

          <details style={{ marginTop: '2rem' }}>
            <summary style={{ cursor: 'pointer', fontWeight: 'bold' }}>
              Technical Details
            </summary>
            <pre
              style={{
                background: '#f5f5f5',
                padding: '1rem',
                overflow: 'auto',
                borderRadius: '4px',
                marginTop: '1rem',
              }}
            >
              {error?.toString()}
              {'\n\n'}
              {errorInfo?.componentStack}
            </pre>
          </details>
        </div>
      );
    }

    return this.props.children;
  }
}
