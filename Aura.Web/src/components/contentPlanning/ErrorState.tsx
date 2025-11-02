import { Card, Text, Button, makeStyles, tokens } from '@fluentui/react-components';
import {
  ErrorCircleRegular,
  ArrowClockwiseRegular,
  PlugDisconnectedRegular,
  ShieldErrorRegular,
  ClockRegular,
} from '@fluentui/react-icons';
import React from 'react';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    padding: tokens.spacingVerticalXXL,
  },
  card: {
    maxWidth: '600px',
    width: '100%',
    padding: tokens.spacingVerticalXXL,
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    textAlign: 'center',
    gap: tokens.spacingVerticalL,
  },
  icon: {
    fontSize: '64px',
  },
  errorIcon: {
    color: tokens.colorPaletteRedForeground1,
  },
  warningIcon: {
    color: tokens.colorPaletteYellowForeground1,
  },
  title: {
    fontSize: tokens.fontSizeHero700,
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: tokens.spacingVerticalXS,
  },
  message: {
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground2,
    lineHeight: tokens.lineHeightBase300,
  },
  details: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    fontStyle: 'italic',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
  },
});

export type ErrorType = 'network' | 'auth' | 'rateLimit' | 'timeout' | 'server' | 'unknown';

interface ErrorStateProps {
  errorType?: ErrorType;
  message?: string;
  details?: string;
  onRetry?: () => void;
  retryDisabled?: boolean;
}

export const ErrorState: React.FC<ErrorStateProps> = ({
  errorType = 'unknown',
  message,
  details,
  onRetry,
  retryDisabled = false,
}) => {
  const styles = useStyles();

  const getErrorConfig = () => {
    switch (errorType) {
      case 'network':
        return {
          icon: <PlugDisconnectedRegular className={`${styles.icon} ${styles.errorIcon}`} />,
          title: 'Connection Failed',
          defaultMessage:
            'Unable to connect to the service. Please check your internet connection.',
        };
      case 'auth':
        return {
          icon: <ShieldErrorRegular className={`${styles.icon} ${styles.errorIcon}`} />,
          title: 'Authentication Failed',
          defaultMessage: 'Your API key is invalid or has expired. Please update it in Settings.',
        };
      case 'rateLimit':
        return {
          icon: <ClockRegular className={`${styles.icon} ${styles.warningIcon}`} />,
          title: 'Rate Limit Exceeded',
          defaultMessage: 'Too many requests. Please try again in a few minutes.',
        };
      case 'timeout':
        return {
          icon: <ClockRegular className={`${styles.icon} ${styles.warningIcon}`} />,
          title: 'Request Timeout',
          defaultMessage: 'The request took too long to complete. Please try again.',
        };
      case 'server':
        return {
          icon: <ErrorCircleRegular className={`${styles.icon} ${styles.errorIcon}`} />,
          title: 'Service Unavailable',
          defaultMessage: 'The service is temporarily unavailable. Please try again later.',
        };
      default:
        return {
          icon: <ErrorCircleRegular className={`${styles.icon} ${styles.errorIcon}`} />,
          title: 'Error Occurred',
          defaultMessage: 'An unexpected error occurred. Please try again.',
        };
    }
  };

  const config = getErrorConfig();

  return (
    <div className={styles.container}>
      <Card className={styles.card}>
        <div className={styles.content}>
          {config.icon}

          <div>
            <div className={styles.title}>{config.title}</div>
            <Text className={styles.message}>{message || config.defaultMessage}</Text>
            {details && <Text className={styles.details}>{details}</Text>}
          </div>

          {onRetry && (
            <div className={styles.actions}>
              <Button
                appearance="primary"
                icon={<ArrowClockwiseRegular />}
                onClick={onRetry}
                disabled={retryDisabled}
              >
                Try Again
              </Button>
            </div>
          )}
        </div>
      </Card>
    </div>
  );
};
