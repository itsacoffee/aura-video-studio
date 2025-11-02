import {
  Button,
  Card,
  makeStyles,
  shorthands,
  Spinner,
  tokens,
  Text,
  Title3,
} from '@fluentui/react-components';
import { ErrorCircle24Regular, ArrowClockwise24Regular } from '@fluentui/react-icons';
import { ErrorInfo } from 'react';
import { useNavigate } from 'react-router-dom';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: '400px',
    ...shorthands.padding(tokens.spacingVerticalXXL),
  },
  card: {
    maxWidth: '600px',
    width: '100%',
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    ...shorthands.gap(tokens.spacingVerticalL),
    ...shorthands.padding(tokens.spacingVerticalXXL, tokens.spacingHorizontalXL),
    textAlign: 'center',
  },
  icon: {
    fontSize: '64px',
    color: tokens.colorPaletteRedForeground1,
  },
  message: {
    color: tokens.colorNeutralForeground2,
  },
  actions: {
    display: 'flex',
    ...shorthands.gap(tokens.spacingHorizontalM),
    marginTop: tokens.spacingVerticalM,
  },
  details: {
    marginTop: tokens.spacingVerticalL,
    ...shorthands.padding(tokens.spacingVerticalM),
    backgroundColor: tokens.colorNeutralBackground3,
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    textAlign: 'left',
    width: '100%',
    maxHeight: '200px',
    overflowY: 'auto',
  },
  errorText: {
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase200,
    whiteSpace: 'pre-wrap',
    wordBreak: 'break-word',
  },
});

interface RouteErrorFallbackProps {
  error: Error;
  errorInfo: ErrorInfo | null;
  onRetry: () => void | Promise<void>;
  isRetrying?: boolean;
}

export function RouteErrorFallback({
  error,
  errorInfo,
  onRetry,
  isRetrying = false,
}: RouteErrorFallbackProps) {
  const styles = useStyles();
  const navigate = useNavigate();

  const getUserFriendlyMessage = (error: Error): string => {
    const message = error.message.toLowerCase();

    if (message.includes('network') || message.includes('fetch')) {
      return 'Unable to connect to the server. Please check your internet connection and try again.';
    }

    if (message.includes('timeout')) {
      return 'The request took too long to complete. Please try again.';
    }

    if (message.includes('404')) {
      return 'The requested resource was not found.';
    }

    if (message.includes('500') || message.includes('internal server')) {
      return 'The server encountered an error. Please try again later.';
    }

    return 'An unexpected error occurred while loading this page.';
  };

  return (
    <div className={styles.container}>
      <Card className={styles.card}>
        <div className={styles.content}>
          <ErrorCircle24Regular className={styles.icon} />
          <Title3>Oops! Something went wrong</Title3>
          <Text className={styles.message}>{getUserFriendlyMessage(error)}</Text>

          <div className={styles.actions}>
            <Button
              appearance="primary"
              icon={isRetrying ? <Spinner size="tiny" /> : <ArrowClockwise24Regular />}
              onClick={onRetry}
              disabled={isRetrying}
            >
              {isRetrying ? 'Retrying...' : 'Try Again'}
            </Button>
            <Button appearance="secondary" onClick={() => navigate('/')}>
              Go to Home
            </Button>
          </div>

          {import.meta.env.DEV && (
            <details className={styles.details}>
              <summary>
                <Text weight="semibold">Error Details (Development Only)</Text>
              </summary>
              <div className={styles.errorText}>
                <Text block weight="semibold">
                  {error.name}: {error.message}
                </Text>
                {error.stack && (
                  <Text block size={200}>
                    {error.stack}
                  </Text>
                )}
                {errorInfo?.componentStack && (
                  <>
                    <Text block weight="semibold" style={{ marginTop: tokens.spacingVerticalM }}>
                      Component Stack:
                    </Text>
                    <Text block size={200}>
                      {errorInfo.componentStack}
                    </Text>
                  </>
                )}
              </div>
            </details>
          )}
        </div>
      </Card>
    </div>
  );
}
