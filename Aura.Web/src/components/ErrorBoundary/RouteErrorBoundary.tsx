import { Component, ErrorInfo, ReactNode } from 'react';
import { loggingService } from '../../services/loggingService';
import { RouteErrorFallback } from './RouteErrorFallback';

interface Props {
  children: ReactNode;
  onRetry?: () => void | Promise<void>;
  routePath?: string;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
  isRetrying: boolean;
}

/**
 * Route-level error boundary that catches errors within a specific route
 * and provides retry functionality without taking down the entire app
 */
export class RouteErrorBoundary extends Component<Props, State> {
  public state: State = {
    hasError: false,
    error: null,
    errorInfo: null,
    isRetrying: false,
  };

  public static getDerivedStateFromError(error: Error): Partial<State> {
    return {
      hasError: true,
      error,
    };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    const routePath = this.props.routePath || window.location.hash || window.location.pathname;
    
    loggingService.error(
      `RouteErrorBoundary caught an error in route: ${routePath}`,
      error,
      'RouteErrorBoundary',
      'componentDidCatch',
      {
        componentStack: errorInfo.componentStack,
        routePath,
        hash: window.location.hash,
        pathname: window.location.pathname,
      }
    );

    this.setState({
      error,
      errorInfo,
    });
  }

  private handleRetry = async () => {
    loggingService.info(
      'User triggered retry from RouteErrorBoundary',
      'RouteErrorBoundary',
      'retry'
    );

    this.setState({ isRetrying: true });

    try {
      // If parent provides onRetry callback, execute it
      if (this.props.onRetry) {
        await this.props.onRetry();
      }

      // Reset error state after successful retry
      this.setState({
        hasError: false,
        error: null,
        errorInfo: null,
        isRetrying: false,
      });
    } catch (error) {
      // If retry fails, keep error state but stop loading
      loggingService.error(
        'Retry failed in RouteErrorBoundary',
        error instanceof Error ? error : new Error(String(error)),
        'RouteErrorBoundary',
        'handleRetry'
      );
      this.setState({ isRetrying: false });
    }
  };

  public render() {
    if (this.state.hasError && this.state.error) {
      return (
        <RouteErrorFallback
          error={this.state.error}
          errorInfo={this.state.errorInfo}
          onRetry={this.handleRetry}
          isRetrying={this.state.isRetrying}
        />
      );
    }

    return this.props.children;
  }
}
