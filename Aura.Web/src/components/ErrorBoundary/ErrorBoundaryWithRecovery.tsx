import { makeStyles, tokens, Card, Button, Text } from '@fluentui/react-components';
import {
  ErrorCircle24Regular,
  ArrowClockwise24Regular,
  Home24Regular,
} from '@fluentui/react-icons';
import { Component, ReactNode, ErrorInfo } from 'react';
import { navigateToRoute } from '@/utils/navigation';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: '300px',
    padding: tokens.spacingVerticalXXL,
  },
  card: {
    maxWidth: '600px',
    padding: tokens.spacingVerticalXXL,
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalL,
    textAlign: 'center',
  },
  icon: {
    fontSize: '64px',
    color: tokens.colorPaletteRedForeground1,
  },
  title: {
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground1,
  },
  message: {
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground3,
  },
  errorDetails: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    fontSize: tokens.fontSizeBase200,
    fontFamily: 'monospace',
    color: tokens.colorNeutralForeground2,
    maxWidth: '100%',
    overflow: 'auto',
    textAlign: 'left',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalM,
  },
});

interface ErrorBoundaryWithRecoveryProps {
  children: ReactNode;
  fallback?: ReactNode;
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
  showErrorDetails?: boolean;
  resetKeys?: Array<unknown>;
}

interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
  errorCount: number;
}

/**
 * Enhanced error boundary with recovery options and better UX
 * Provides retry functionality and navigation to safe routes
 */
export class ErrorBoundaryWithRecovery extends Component<
  ErrorBoundaryWithRecoveryProps,
  ErrorBoundaryState
> {
  constructor(props: ErrorBoundaryWithRecoveryProps) {
    super(props);
    this.state = {
      hasError: false,
      error: null,
      errorInfo: null,
      errorCount: 0,
    };
  }

  static getDerivedStateFromError(error: Error): Partial<ErrorBoundaryState> {
    return {
      hasError: true,
      error,
    };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    this.setState((prevState) => ({
      errorInfo,
      errorCount: prevState.errorCount + 1,
    }));

    this.props.onError?.(error, errorInfo);

    console.error('Error Boundary caught an error:', error, errorInfo);
  }

  componentDidUpdate(
    prevProps: ErrorBoundaryWithRecoveryProps,
    _prevState: ErrorBoundaryState
  ): void {
    const { resetKeys } = this.props;

    if (resetKeys && prevProps.resetKeys) {
      const hasResetKeyChanged = resetKeys.some(
        (key, index) => key !== prevProps.resetKeys![index]
      );

      if (hasResetKeyChanged && this.state.hasError) {
        this.reset();
      }
    }
  }

  reset = (): void => {
    this.setState({
      hasError: false,
      error: null,
      errorInfo: null,
    });
  };

  handleRetry = (): void => {
    this.reset();
  };

  handleGoHome = (): void => {
    navigateToRoute('/');  };

  render(): ReactNode {
    const { hasError, error, errorInfo, errorCount } = this.state;
    const { children, fallback, showErrorDetails = false } = this.props;

    if (hasError) {
      if (fallback) {
        return fallback;
      }

      return (
        <ErrorFallbackContent
          error={error}
          errorInfo={errorInfo}
          errorCount={errorCount}
          showErrorDetails={showErrorDetails}
          onRetry={this.handleRetry}
          onGoHome={this.handleGoHome}
        />
      );
    }

    return children;
  }
}

interface ErrorFallbackContentProps {
  error: Error | null;
  errorInfo: ErrorInfo | null;
  errorCount: number;
  showErrorDetails: boolean;
  onRetry: () => void;
  onGoHome: () => void;
}

function ErrorFallbackContent({
  error,
  errorInfo,
  errorCount,
  showErrorDetails,
  onRetry,
  onGoHome,
}: ErrorFallbackContentProps) {
  const styles = useStyles();

  const errorMessage = error?.message || 'An unexpected error occurred';
  const shouldShowRetry = errorCount < 3;

  return (
    <div className={styles.container}>
      <Card className={styles.card}>
        <div className={styles.content}>
          <ErrorCircle24Regular className={styles.icon} aria-hidden="true" />

          <div>
            <Text className={styles.title}>Something went wrong</Text>
            <Text className={styles.message} as="p">
              {shouldShowRetry
                ? 'We encountered an error. You can try again or return to the home page.'
                : 'Multiple errors occurred. Please return to the home page and try again.'}
            </Text>
          </div>

          {showErrorDetails && error && (
            <details style={{ width: '100%', marginTop: tokens.spacingVerticalM }}>
              <summary style={{ cursor: 'pointer', marginBottom: tokens.spacingVerticalS }}>
                Error details
              </summary>
              <div className={styles.errorDetails}>
                <div>
                  <strong>Error:</strong> {errorMessage}
                </div>
                {errorInfo && (
                  <div style={{ marginTop: tokens.spacingVerticalS }}>
                    <strong>Stack:</strong>
                    <pre style={{ margin: 0, whiteSpace: 'pre-wrap' }}>
                      {errorInfo.componentStack}
                    </pre>
                  </div>
                )}
              </div>
            </details>
          )}

          <div className={styles.actions}>
            {shouldShowRetry && (
              <Button appearance="primary" icon={<ArrowClockwise24Regular />} onClick={onRetry}>
                Try Again
              </Button>
            )}
            <Button appearance="secondary" icon={<Home24Regular />} onClick={onGoHome}>
              Go to Home
            </Button>
          </div>

          {errorCount > 1 && (
            <Text size={200} style={{ color: tokens.colorPaletteRedForeground1 }}>
              This error has occurred {errorCount} times
            </Text>
          )}
        </div>
      </Card>
    </div>
  );
}
