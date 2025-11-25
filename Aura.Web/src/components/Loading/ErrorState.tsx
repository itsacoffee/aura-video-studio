import { makeStyles, tokens, Card, Text, Button } from '@fluentui/react-components';
import { ErrorCircle24Regular } from '@fluentui/react-icons';

const useStyles = makeStyles({
  card: {
    padding: tokens.spacingVerticalXXL,
  },
  container: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
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
    maxWidth: '500px',
    whiteSpace: 'pre-line', // Support multiline messages
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalM,
  },
});

interface ErrorStateProps {
  /**
   * Error message to display
   */
  message: string;
  /**
   * Error title
   */
  title?: string;
  /**
   * Retry callback
   */
  onRetry?: () => void;
  /**
   * Additional action button
   */
  actionButton?: {
    label: string;
    onClick: () => void;
  };
  /**
   * Whether to wrap in a Card
   */
  withCard?: boolean;
  /**
   * ARIA label for accessibility
   */
  ariaLabel?: string;
}

/**
 * Error state component with retry functionality
 * Displays a clear error message with optional retry button
 */
export function ErrorState({
  message,
  title = 'Something went wrong',
  onRetry,
  actionButton,
  withCard = true,
  ariaLabel = 'Error state',
}: ErrorStateProps) {
  const styles = useStyles();

  const content = (
    <div className={styles.container} role="alert" aria-label={ariaLabel}>
      <ErrorCircle24Regular className={styles.icon} aria-hidden="true" />
      <div>
        <Text className={styles.title}>{title}</Text>
        <div className={styles.message}>
          {message.split('\n').map((line, index) => (
            <Text key={index} as="p" style={{ margin: index > 0 ? tokens.spacingVerticalXS : 0 }}>
              {line}
            </Text>
          ))}
        </div>
      </div>
      {(onRetry || actionButton) && (
        <div className={styles.actions}>
          {onRetry && (
            <Button appearance="primary" onClick={onRetry}>
              Try Again
            </Button>
          )}
          {actionButton && (
            <Button appearance="secondary" onClick={actionButton.onClick}>
              {actionButton.label}
            </Button>
          )}
        </div>
      )}
    </div>
  );

  if (withCard) {
    return <Card className={styles.card}>{content}</Card>;
  }

  return content;
}
