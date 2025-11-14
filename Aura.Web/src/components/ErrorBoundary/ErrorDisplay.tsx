/**
 * Generic Error Display Component
 * Provides consistent error display with recovery actions
 */

import {
  Button,
  Card,
  makeStyles,
  MessageBar,
  MessageBarBody,
  shorthands,
  Text,
  tokens,
} from '@fluentui/react-components';
import {
  ArrowClockwise24Regular,
  Dismiss24Regular,
  ErrorCircle24Regular,
  Warning24Regular,
  Info24Regular,
} from '@fluentui/react-icons';
import { ReactNode } from 'react';

const useStyles = makeStyles({
  container: {
    ...shorthands.padding(tokens.spacingVerticalL),
  },
  card: {
    maxWidth: '600px',
    ...shorthands.margin('0', 'auto'),
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalM),
    ...shorthands.padding(tokens.spacingVerticalL, tokens.spacingHorizontalL),
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    ...shorthands.gap(tokens.spacingHorizontalS),
  },
  icon: {
    fontSize: '32px',
  },
  errorIcon: {
    color: tokens.colorPaletteRedForeground1,
  },
  warningIcon: {
    color: tokens.colorPaletteYellowForeground1,
  },
  infoIcon: {
    color: tokens.colorPaletteBlueForeground2,
  },
  suggestionsList: {
    marginTop: tokens.spacingVerticalS,
    paddingLeft: tokens.spacingHorizontalL,
  },
  actions: {
    display: 'flex',
    ...shorthands.gap(tokens.spacingHorizontalS),
    flexWrap: 'wrap',
  },
});

export interface ErrorDisplayProps {
  title: string;
  message: string;
  type?: 'error' | 'warning' | 'info';
  suggestions?: string[];
  onRetry?: () => void | Promise<void>;
  onDismiss?: () => void;
  showRetry?: boolean;
  retryLabel?: string;
  dismissLabel?: string;
  children?: ReactNode;
}

export function ErrorDisplay({
  title,
  message,
  type = 'error',
  suggestions,
  onRetry,
  onDismiss,
  showRetry = false,
  retryLabel = 'Try Again',
  dismissLabel = 'Dismiss',
  children,
}: ErrorDisplayProps) {
  const styles = useStyles();

  const handleRetry = () => {
    if (onRetry) {
      const result = onRetry();
      if (result instanceof Promise) {
        result.catch((err) => {
          console.error('Retry failed:', err);
        });
      }
    }
  };

  const getIcon = () => {
    switch (type) {
      case 'error':
        return <ErrorCircle24Regular className={`${styles.icon} ${styles.errorIcon}`} />;
      case 'warning':
        return <Warning24Regular className={`${styles.icon} ${styles.warningIcon}`} />;
      case 'info':
        return <Info24Regular className={`${styles.icon} ${styles.infoIcon}`} />;
    }
  };

  const getIntent = () => {
    switch (type) {
      case 'error':
        return 'error' as const;
      case 'warning':
        return 'warning' as const;
      case 'info':
        return 'info' as const;
    }
  };

  return (
    <div className={styles.container}>
      <Card className={styles.card}>
        <div className={styles.content}>
          <div className={styles.header}>
            {getIcon()}
            <Text size={600} weight="semibold">
              {title}
            </Text>
          </div>

          <MessageBar intent={getIntent()}>
            <MessageBarBody>
              <Text>{message}</Text>
            </MessageBarBody>
          </MessageBar>

          {suggestions && suggestions.length > 0 && (
            <div>
              <Text weight="semibold" block>
                What you can do:
              </Text>
              <ul className={styles.suggestionsList}>
                {suggestions.map((suggestion, index) => (
                  <li key={index}>
                    <Text>{suggestion}</Text>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {children}

          {(showRetry || onDismiss) && (
            <div className={styles.actions}>
              {showRetry && onRetry && (
                <Button
                  appearance="primary"
                  icon={<ArrowClockwise24Regular />}
                  onClick={handleRetry}
                >
                  {retryLabel}
                </Button>
              )}
              {onDismiss && (
                <Button appearance="secondary" icon={<Dismiss24Regular />} onClick={onDismiss}>
                  {dismissLabel}
                </Button>
              )}
            </div>
          )}
        </div>
      </Card>
    </div>
  );
}

/**
 * Create error display props from different error types
 */
export function createNetworkErrorDisplay(onRetry?: () => void): ErrorDisplayProps {
  return {
    title: 'Network Connection Lost',
    message: 'Unable to connect to the server. Please check your internet connection.',
    type: 'error',
    suggestions: [
      'Check your internet connection',
      'Verify the server is running',
      'Try again in a few moments',
    ],
    onRetry,
    showRetry: true,
  };
}

export function createAuthErrorDisplay(onRetry?: () => void): ErrorDisplayProps {
  return {
    title: 'Authentication Failed',
    message: 'Your session has expired or your credentials are invalid.',
    type: 'error',
    suggestions: [
      'Check your API keys in Settings',
      'Verify your credentials are correct',
      'Try logging in again',
    ],
    onRetry,
    showRetry: true,
  };
}

export function createValidationErrorDisplay(errors: string[]): ErrorDisplayProps {
  return {
    title: 'Validation Error',
    message: 'Please correct the following errors before proceeding.',
    type: 'warning',
    suggestions: errors,
    showRetry: false,
  };
}

export function createGenericErrorDisplay(
  title: string,
  message: string,
  onRetry?: () => void
): ErrorDisplayProps {
  return {
    title,
    message,
    type: 'error',
    suggestions: ['Please try again', 'Contact support if the problem persists'],
    onRetry,
    showRetry: !!onRetry,
  };
}
