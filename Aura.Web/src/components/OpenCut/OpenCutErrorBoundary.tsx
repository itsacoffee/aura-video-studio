import React, { Component, ErrorInfo, ReactNode } from 'react';
import {
  makeStyles,
  tokens,
  Title2,
  Body1,
  Button,
  Card,
} from '@fluentui/react-components';
import { ErrorCircle24Filled, ArrowClockwise24Regular } from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: '400px',
    padding: tokens.spacingVerticalXXXL,
  },
  errorCard: {
    maxWidth: '600px',
    padding: tokens.spacingVerticalXXL,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalL,
    textAlign: 'center',
  },
  icon: {
    color: tokens.colorPaletteRedForeground1,
    fontSize: '48px',
  },
  title: {
    color: tokens.colorNeutralForeground1,
  },
  message: {
    color: tokens.colorNeutralForeground2,
  },
  details: {
    marginTop: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    textAlign: 'left',
    width: '100%',
    maxHeight: '200px',
    overflowY: 'auto',
    wordBreak: 'break-word',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalL,
  },
});

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
}

class OpenCutErrorBoundaryClass extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = {
      hasError: false,
      error: null,
      errorInfo: null,
    };
  }

  static getDerivedStateFromError(error: Error): Partial<State> {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('OpenCut Error Boundary caught error:', error, errorInfo);
    this.setState({
      error,
      errorInfo,
    });
  }

  handleReset = () => {
    this.setState({
      hasError: false,
      error: null,
      errorInfo: null,
    });
  };

  handleReload = () => {
    window.location.reload();
  };

  render() {
    if (this.state.hasError) {
      return <ErrorDisplay error={this.state.error} onReset={this.handleReset} onReload={this.handleReload} />;
    }

    return this.props.children;
  }
}

interface ErrorDisplayProps {
  error: Error | null;
  onReset: () => void;
  onReload: () => void;
}

function ErrorDisplay({ error, onReset, onReload }: ErrorDisplayProps) {
  const styles = useStyles();

  return (
    <div className={styles.container}>
      <Card className={styles.errorCard}>
        <ErrorCircle24Filled className={styles.icon} />
        <Title2 className={styles.title}>OpenCut Editor Error</Title2>
        <Body1 className={styles.message}>
          The video editor encountered an unexpected error and could not load.
          {error && (
            <>
              <br />
              <br />
              This may be due to:
              <ul style={{ textAlign: 'left', margin: '1rem 0' }}>
                <li>Missing or misconfigured AI providers</li>
                <li>Browser compatibility issues</li>
                <li>Corrupted editor state</li>
              </ul>
            </>
          )}
        </Body1>
        {error && (
          <div className={styles.details}>
            <strong>Error Details:</strong>
            <br />
            {error.message}
            {error.stack && (
              <>
                <br />
                <br />
                <strong>Stack Trace:</strong>
                <br />
                {error.stack.split('\n').slice(0, 5).join('\n')}
              </>
            )}
          </div>
        )}
        <div className={styles.actions}>
          <Button appearance="primary" icon={<ArrowClockwise24Regular />} onClick={onReset}>
            Try Again
          </Button>
          <Button onClick={onReload}>Reload Page</Button>
        </div>
      </Card>
    </div>
  );
}

export const OpenCutErrorBoundary = OpenCutErrorBoundaryClass;
