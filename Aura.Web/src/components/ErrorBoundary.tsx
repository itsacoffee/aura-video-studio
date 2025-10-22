import React, { Component, ErrorInfo, ReactNode } from 'react';
import { Button, Title1, Body1, makeStyles, tokens } from '@fluentui/react-components';
import { ErrorCircle24Regular, ArrowClockwise24Regular } from '@fluentui/react-icons';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
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

function ErrorFallback({ error, errorInfo, onReset }: { 
  error: Error; 
  errorInfo: ErrorInfo; 
  onReset: () => void;
}) {
  const styles = useStyles();
  const [showDetails, setShowDetails] = React.useState(false);

  return (
    <div className={styles.container}>
      <ErrorCircle24Regular className={styles.icon} />
      <Title1 className={styles.title}>Something went wrong</Title1>
      <Body1 className={styles.message}>
        We're sorry, but an unexpected error occurred. Please try refreshing the page.
      </Body1>
      <div className={styles.actions}>
        <Button
          appearance="primary"
          icon={<ArrowClockwise24Regular />}
          onClick={onReset}
        >
          Try Again
        </Button>
        <Button
          appearance="secondary"
          onClick={() => setShowDetails(!showDetails)}
        >
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
  };

  public static getDerivedStateFromError(error: Error): State {
    return {
      hasError: true,
      error,
      errorInfo: null,
    };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('ErrorBoundary caught an error:', error, errorInfo);
    
    // Log error to localStorage for debugging
    try {
      const errorLog = {
        timestamp: new Date().toISOString(),
        error: {
          message: error.message,
          stack: error.stack,
        },
        componentStack: errorInfo.componentStack,
      };
      
      const existingLogs = localStorage.getItem('error_logs');
      const logs = existingLogs ? JSON.parse(existingLogs) : [];
      logs.push(errorLog);
      
      // Keep only last 10 errors
      if (logs.length > 10) {
        logs.shift();
      }
      
      localStorage.setItem('error_logs', JSON.stringify(logs));
    } catch (e) {
      console.error('Failed to log error to localStorage:', e);
    }

    this.setState({
      error,
      errorInfo,
    });
  }

  private handleReset = () => {
    this.setState({
      hasError: false,
      error: null,
      errorInfo: null,
    });
  };

  public render() {
    if (this.state.hasError && this.state.error && this.state.errorInfo) {
      if (this.props.fallback) {
        return this.props.fallback;
      }
      
      return (
        <ErrorFallback
          error={this.state.error}
          errorInfo={this.state.errorInfo}
          onReset={this.handleReset}
        />
      );
    }

    return this.props.children;
  }
}
