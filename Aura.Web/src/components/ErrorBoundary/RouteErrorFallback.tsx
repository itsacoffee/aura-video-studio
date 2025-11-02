import { Button, Card, Text, makeStyles, tokens } from '@fluentui/react-components';
import { ArrowClockwise24Regular, ErrorCircle24Regular } from '@fluentui/react-icons';
import { FC } from 'react';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: '400px',
    padding: tokens.spacingVerticalXXL,
  },
  card: {
    maxWidth: '600px',
    width: '100%',
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalXXL,
    textAlign: 'center',
  },
  icon: {
    fontSize: '64px',
    color: tokens.colorPaletteRedForeground1,
  },
  title: {
    fontSize: tokens.fontSizeHero800,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground1,
  },
  message: {
    color: tokens.colorNeutralForeground2,
    maxWidth: '500px',
  },
  errorDetails: {
    marginTop: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorPaletteRedForeground1,
    textAlign: 'left',
    maxWidth: '100%',
    overflow: 'auto',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
  },
});

interface RouteErrorFallbackProps {
  error: Error;
  onRetry: () => void;
}

export const RouteErrorFallback: FC<RouteErrorFallbackProps> = ({ error, onRetry }) => {
  const styles = useStyles();

  const getUserFriendlyMessage = (error: Error): string => {
    const message = error.message.toLowerCase();

    if (message.includes('network') || message.includes('fetch')) {
      return 'Unable to connect to the server. Please check your internet connection and try again.';
    }

    if (message.includes('timeout')) {
      return 'The request took too long to complete. Please try again.';
    }

    if (message.includes('unauthorized') || message.includes('403') || message.includes('401')) {
      return 'You do not have permission to access this resource.';
    }

    if (message.includes('not found') || message.includes('404')) {
      return 'The requested resource was not found.';
    }

    if (message.includes('server') || message.includes('500')) {
      return 'A server error occurred. Please try again later.';
    }

    return 'An unexpected error occurred while loading this page.';
  };

  const handleReload = () => {
    window.location.reload();
  };

  return (
    <div className={styles.container}>
      <Card className={styles.card}>
        <div className={styles.content}>
          <ErrorCircle24Regular className={styles.icon} />

          <div>
            <div className={styles.title}>Oops! Something went wrong</div>
            <Text className={styles.message}>{getUserFriendlyMessage(error)}</Text>
          </div>

          {import.meta.env.DEV && (
            <div className={styles.errorDetails}>
              <strong>Error Details (Development Only):</strong>
              <br />
              {error.name}: {error.message}
              {error.stack && (
                <>
                  <br />
                  <br />
                  {error.stack.split('\n').slice(0, 5).join('\n')}
                </>
              )}
            </div>
          )}

          <div className={styles.actions}>
            <Button appearance="primary" icon={<ArrowClockwise24Regular />} onClick={onRetry}>
              Try Again
            </Button>
            <Button appearance="secondary" onClick={handleReload}>
              Reload Page
            </Button>
          </div>
        </div>
      </Card>
    </div>
  );
};
