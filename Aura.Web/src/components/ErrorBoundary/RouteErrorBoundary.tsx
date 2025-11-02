import { Component, ErrorInfo, ReactNode } from 'react';
import { loggingService } from '../../services/loggingService';
import { RouteErrorFallback } from './RouteErrorFallback';

interface Props {
  children: ReactNode;
  onRetry?: () => void;
  resetKeys?: unknown[];
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
}

/**
 * Route-level error boundary that catches errors in page components
 * and provides a retry mechanism that resets error state and re-triggers data loading
 */
export class RouteErrorBoundary extends Component<Props, State> {
  public state: State = {
    hasError: false,
    error: null,
    errorInfo: null,
  };

  public static getDerivedStateFromError(error: Error): Partial<State> {
    return {
      hasError: true,
      error,
    };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    loggingService.error(
      'RouteErrorBoundary caught an error',
      error,
      'RouteErrorBoundary',
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

  public componentDidUpdate(prevProps: Props) {
    // Reset error state if resetKeys change (allows parent to trigger retry)
    if (
      this.state.hasError &&
      this.props.resetKeys &&
      prevProps.resetKeys !== this.props.resetKeys
    ) {
      this.handleReset();
    }
  }

  private handleReset = () => {
    loggingService.info('User reset route error boundary', 'RouteErrorBoundary', 'reset');

    this.setState({
      hasError: false,
      error: null,
      errorInfo: null,
    });

    // Call parent's onRetry if provided
    if (this.props.onRetry) {
      this.props.onRetry();
    }
  };

  public render() {
    if (this.state.hasError && this.state.error) {
      return <RouteErrorFallback error={this.state.error} onRetry={this.handleReset} />;
    }

    return this.props.children;
  }
}
