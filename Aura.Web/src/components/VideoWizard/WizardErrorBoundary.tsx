/**
 * WizardErrorBoundary - Error boundary specifically for VideoWizard steps
 * Provides user-friendly error recovery with retry and graceful degradation
 */

import { Button, Card, Text, Title3, tokens, makeStyles } from '@fluentui/react-components';
import {
  ErrorCircle24Regular,
  ArrowClockwise24Regular,
  ArrowRight24Regular,
  Warning24Regular,
} from '@fluentui/react-icons';
import { Component, ReactNode } from 'react';
import type { ErrorInfo } from 'react';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXL,
    textAlign: 'center',
    minHeight: '200px',
  },
  card: {
    maxWidth: '500px',
    padding: tokens.spacingVerticalXXL,
  },
  icon: {
    fontSize: '48px',
    marginBottom: tokens.spacingVerticalL,
  },
  title: {
    marginBottom: tokens.spacingVerticalM,
  },
  message: {
    marginBottom: tokens.spacingVerticalL,
    color: tokens.colorNeutralForeground3,
  },
  errorDetails: {
    marginTop: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    fontSize: tokens.fontSizeBase200,
    fontFamily: 'monospace',
    maxHeight: '100px',
    overflow: 'auto',
    textAlign: 'left',
  },
  buttonGroup: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalL,
    flexWrap: 'wrap',
    justifyContent: 'center',
  },
  warningBanner: {
    backgroundColor: tokens.colorPaletteYellowBackground1,
    padding: tokens.spacingVerticalM,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalL,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
});

interface WizardErrorBoundaryProps {
  children: ReactNode;
  /** Name of the wizard step for context in error messages */
  stepName: string;
  /** Callback when error occurs */
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
  /** Callback when user clicks retry */
  onRetry?: () => void;
  /** Callback when user chooses to skip with defaults */
  onSkipWithDefaults?: () => void;
  /** Custom fallback render function */
  fallback?: (props: {
    error: Error;
    reset: () => void;
    skipWithDefaults?: () => void;
  }) => ReactNode;
  /** Enable graceful degradation (continue with defaults on failure) */
  enableGracefulDegradation?: boolean;
}

interface WizardErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
  retryCount: number;
}

// Maximum retries before suggesting skip
const MAX_RETRIES = 3;

/**
 * Error boundary for VideoWizard steps
 * Features:
 * - User-friendly error messages
 * - Retry functionality with count tracking
 * - Graceful degradation option (skip with defaults)
 * - Error logging and reporting
 */
export class WizardErrorBoundary extends Component<
  WizardErrorBoundaryProps,
  WizardErrorBoundaryState
> {
  constructor(props: WizardErrorBoundaryProps) {
    super(props);
    this.state = {
      hasError: false,
      error: null,
      errorInfo: null,
      retryCount: 0,
    };
  }

  static getDerivedStateFromError(error: Error): Partial<WizardErrorBoundaryState> {
    return {
      hasError: true,
      error,
    };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    const { stepName, onError } = this.props;

    // Log error with context
    console.error(`[WizardErrorBoundary] Error in step "${stepName}":`, {
      error: error.message,
      stack: error.stack,
      componentStack: errorInfo.componentStack,
      retryCount: this.state.retryCount,
    });

    this.setState({ errorInfo });

    // Call custom error handler if provided
    if (onError) {
      onError(error, errorInfo);
    }
  }

  handleReset = (): void => {
    const { onRetry } = this.props;

    this.setState((prevState) => ({
      hasError: false,
      error: null,
      errorInfo: null,
      retryCount: prevState.retryCount + 1,
    }));

    if (onRetry) {
      onRetry();
    }
  };

  handleSkipWithDefaults = (): void => {
    const { onSkipWithDefaults, stepName } = this.props;

    console.info(`[WizardErrorBoundary] Skipping step "${stepName}" with defaults`);

    this.setState({
      hasError: false,
      error: null,
      errorInfo: null,
    });

    if (onSkipWithDefaults) {
      onSkipWithDefaults();
    }
  };

  render(): ReactNode {
    const { hasError, error, retryCount } = this.state;
    const { children, stepName, fallback, enableGracefulDegradation = true } = this.props;

    if (hasError && error) {
      // Use custom fallback if provided
      if (fallback) {
        return fallback({
          error,
          reset: this.handleReset,
          skipWithDefaults: enableGracefulDegradation ? this.handleSkipWithDefaults : undefined,
        });
      }

      // Default error UI
      return (
        <WizardErrorFallback
          error={error}
          stepName={stepName}
          retryCount={retryCount}
          maxRetries={MAX_RETRIES}
          onRetry={this.handleReset}
          onSkipWithDefaults={enableGracefulDegradation ? this.handleSkipWithDefaults : undefined}
        />
      );
    }

    return children;
  }
}

// Functional component for the error fallback UI (to use hooks for styles)
interface WizardErrorFallbackProps {
  error: Error;
  stepName: string;
  retryCount: number;
  maxRetries: number;
  onRetry: () => void;
  onSkipWithDefaults?: () => void;
}

function WizardErrorFallback({
  error,
  stepName,
  retryCount,
  maxRetries,
  onRetry,
  onSkipWithDefaults,
}: WizardErrorFallbackProps): JSX.Element {
  const styles = useStyles();
  const hasExceededRetries = retryCount >= maxRetries;

  return (
    <div className={styles.container}>
      <Card className={styles.card}>
        <ErrorCircle24Regular
          className={styles.icon}
          style={{ color: tokens.colorPaletteRedForeground1 }}
        />
        <Title3 className={styles.title}>Error in {stepName}</Title3>
        <Text className={styles.message}>
          Something went wrong while loading this step.
          {hasExceededRetries
            ? ' You can skip this step and continue with default settings.'
            : ' Please try again or skip with defaults.'}
        </Text>

        {hasExceededRetries && (
          <div className={styles.warningBanner}>
            <Warning24Regular style={{ color: tokens.colorPaletteDarkOrangeForeground1 }} />
            <Text size={200}>Multiple retry attempts failed. Consider skipping this step.</Text>
          </div>
        )}

        <div className={styles.buttonGroup}>
          <Button appearance="primary" icon={<ArrowClockwise24Regular />} onClick={onRetry}>
            Try Again {retryCount > 0 && `(${retryCount}/${maxRetries})`}
          </Button>
          {onSkipWithDefaults && (
            <Button
              appearance="secondary"
              icon={<ArrowRight24Regular />}
              onClick={onSkipWithDefaults}
            >
              Skip with Defaults
            </Button>
          )}
        </div>

        {/* Show error details for debugging (collapsible in future) */}
        <details>
          <summary style={{ cursor: 'pointer', marginTop: tokens.spacingVerticalM }}>
            <Text size={200}>Show error details</Text>
          </summary>
          <div className={styles.errorDetails}>{error.message}</div>
        </details>
      </Card>
    </div>
  );
}

export default WizardErrorBoundary;
