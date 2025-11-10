/**
 * API Error Display Component
 * Displays errors from API responses with error codes and "Learn More" links
 */

import {
  Button,
  Card,
  Link,
  makeStyles,
  MessageBar,
  MessageBarBody,
  shorthands,
  Text,
  tokens,
} from '@fluentui/react-components';
import {
  ArrowClockwise24Regular,
  ErrorCircle24Regular,
  Info24Regular,
} from '@fluentui/react-icons';

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
    color: tokens.colorPaletteRedForeground1,
  },
  errorCode: {
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
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
  learnMore: {
    display: 'flex',
    alignItems: 'center',
    ...shorthands.gap(tokens.spacingHorizontalXS),
  },
});

export interface ApiError {
  errorCode: string;
  message: string;
  technicalDetails?: string;
  suggestedActions?: string[];
  learnMoreUrl?: string;
  errorTitle?: string;
  isTransient?: boolean;
  correlationId?: string;
}

interface ApiErrorDisplayProps {
  error: ApiError;
  onRetry?: () => void | Promise<void>;
  onDismiss?: () => void;
  showTechnicalDetails?: boolean;
}

export function ApiErrorDisplay({
  error,
  onRetry,
  onDismiss,
  showTechnicalDetails = false,
}: ApiErrorDisplayProps) {
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

  return (
    <div className={styles.container}>
      <Card className={styles.card}>
        <div className={styles.content}>
          <div className={styles.header}>
            <ErrorCircle24Regular className={styles.icon} />
            <div>
              <Text size={600} weight="semibold" block>
                {error.errorTitle || 'Error Occurred'}
              </Text>
              <Text className={styles.errorCode}>Error Code: {error.errorCode}</Text>
            </div>
          </div>

          <MessageBar intent="error">
            <MessageBarBody>
              <Text>{error.message}</Text>
            </MessageBarBody>
          </MessageBar>

          {error.suggestedActions && error.suggestedActions.length > 0 && (
            <div>
              <Text weight="semibold" block>
                What you can do:
              </Text>
              <ul className={styles.suggestionsList}>
                {error.suggestedActions.map((action, index) => (
                  <li key={index}>
                    <Text>{action}</Text>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {showTechnicalDetails && error.technicalDetails && (
            <details>
              <summary style={{ cursor: 'pointer', marginBottom: tokens.spacingVerticalS }}>
                <Text weight="semibold">Technical Details</Text>
              </summary>
              <Card
                style={{
                  padding: tokens.spacingVerticalS,
                  backgroundColor: tokens.colorNeutralBackground3,
                }}
              >
                <Text
                  size={300}
                  style={{ fontFamily: 'monospace', whiteSpace: 'pre-wrap', display: 'block' }}
                >
                  {error.technicalDetails}
                </Text>
                {error.correlationId && (
                  <Text size={200} style={{ marginTop: tokens.spacingVerticalXS, display: 'block' }}>
                    Correlation ID: {error.correlationId}
                  </Text>
                )}
              </Card>
            </details>
          )}

          <div className={styles.actions}>
            {onRetry && error.isTransient && (
              <Button
                appearance="primary"
                icon={<ArrowClockwise24Regular />}
                onClick={handleRetry}
              >
                Try Again
              </Button>
            )}
            {onDismiss && (
              <Button appearance="secondary" onClick={onDismiss}>
                Dismiss
              </Button>
            )}
          </div>

          {error.learnMoreUrl && (
            <div className={styles.learnMore}>
              <Info24Regular />
              <Link href={error.learnMoreUrl} target="_blank" rel="noopener noreferrer">
                Learn more about this error
              </Link>
            </div>
          )}
        </div>
      </Card>
    </div>
  );
}

/**
 * Parse API error from fetch response
 */
export async function parseApiError(response: Response): Promise<ApiError> {
  try {
    const data = await response.json();
    return {
      errorCode: data.errorCode || `HTTP_${response.status}`,
      message: data.message || response.statusText || 'An error occurred',
      technicalDetails: data.technicalDetails,
      suggestedActions: data.suggestedActions || [],
      learnMoreUrl: data.learnMoreUrl,
      errorTitle: data.errorTitle,
      isTransient: data.isTransient || false,
      correlationId: data.correlationId,
    };
  } catch {
    // If response is not JSON, create a generic error
    return {
      errorCode: `HTTP_${response.status}`,
      message: response.statusText || 'An error occurred',
      suggestedActions: ['Check your internet connection', 'Try again later'],
      isTransient: response.status >= 500,
    };
  }
}
