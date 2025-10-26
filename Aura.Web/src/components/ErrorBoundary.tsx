import { Component, ErrorInfo, ReactNode } from 'react';
import { loggingService } from '../services/loggingService';
import { ErrorReportDialog } from './ErrorReportDialog';
import { ErrorFallback } from './ErrorBoundary/ErrorFallback';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
  showReportDialog: boolean;
}

export class ErrorBoundary extends Component<Props, State> {
  public state: State = {
    hasError: false,
    error: null,
    errorInfo: null,
    showReportDialog: false,
  };

  public static getDerivedStateFromError(error: Error): Partial<State> {
    return {
      hasError: true,
      error,
    };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    // Log error using the logging service
    loggingService.error(
      'ErrorBoundary caught an error',
      error,
      'ErrorBoundary',
      'componentDidCatch',
      {
        componentStack: errorInfo.componentStack,
      }
    );

    this.setState({
      error,
      errorInfo,
    });
  }

  private handleReset = () => {
    loggingService.info('User reset error boundary', 'ErrorBoundary', 'reset');
    this.setState({
      hasError: false,
      error: null,
      errorInfo: null,
      showReportDialog: false,
    });
  };

  private handleReport = () => {
    loggingService.info('User opened error report dialog', 'ErrorBoundary', 'report');
    this.setState({
      showReportDialog: true,
    });
  };

  public render() {
    if (this.state.hasError && this.state.error && this.state.errorInfo) {
      if (this.props.fallback) {
        return this.props.fallback;
      }

      return (
        <>
          <ErrorFallback
            error={this.state.error}
            errorInfo={this.state.errorInfo}
            onReset={this.handleReset}
            onReport={this.handleReport}
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
