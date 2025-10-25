import React, { Component, ErrorInfo, ReactNode } from 'react';
import { Button, Title1, Body1, makeStyles, tokens } from '@fluentui/react-components';
import { ErrorCircle24Regular, ArrowClockwise24Regular, Send24Regular } from '@fluentui/react-icons';
import { loggingService } from '../services/loggingService';
import { ErrorReportDialog } from './ErrorReportDialog';

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

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: '400px',
    padding: '2rem',
    textAlign: 'center',
  },
  icon: {
    color: tokens.colorPaletteRedBorder1,
    marginBottom: '1rem',
  },
  title: {
    marginBottom: '0.5rem',
  },
  message: {
    marginBottom: '1.5rem',
    color: tokens.colorNeutralForeground2,
  },
  actions: {
    display: 'flex',
    gap: '1rem',
  },
  details: {
    marginTop: '2rem',
    padding: '1rem',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    textAlign: 'left',
    maxWidth: '600px',
    maxHeight: '200px',
    overflowY: 'auto',
    fontFamily: 'monospace',
    fontSize: '0.875rem',
  },
});

// eslint-disable-next-line react-refresh/only-export-components
function ErrorFallback({
  error,
  errorInfo,
  onReset,
  onReport,
}: {
  error: Error;
  errorInfo: ErrorInfo;
  onReset: () => void;
  onReport: () => void;
}) {
  const styles = useStyles();
  const [showDetails, setShowDetails] = React.useState(false);

  return (
    <div className={styles.container}>
      <ErrorCircle24Regular className={styles.icon} />
      <Title1 className={styles.title}>Something went wrong</Title1>
      <Body1 className={styles.message}>
        We&apos;re sorry, but an unexpected error occurred. Please try refreshing the page.
      </Body1>
      <div className={styles.actions}>
        <Button appearance="primary" icon={<ArrowClockwise24Regular />} onClick={onReset}>
          Try Again
        </Button>
        <Button appearance="secondary" icon={<Send24Regular />} onClick={onReport}>
          Report Error
        </Button>
        <Button appearance="secondary" onClick={() => setShowDetails(!showDetails)}>
          {showDetails ? 'Hide Details' : 'Show Details'}
        </Button>
      </div>
      {showDetails && (
        <div className={styles.details}>
          <strong>Error:</strong> {error.message}
          <br />
          <br />
          <strong>Stack:</strong>
          <pre>{errorInfo.componentStack}</pre>
        </div>
      )}
    </div>
  );
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
