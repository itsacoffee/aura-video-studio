import { makeStyles, tokens, Spinner } from '@fluentui/react-components';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: '200px',
    gap: tokens.spacingVerticalL,
  },
  spinner: {
    width: '48px',
    height: '48px',
  },
  message: {
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground3,
  },
});

interface SuspenseFallbackProps {
  message?: string;
  minHeight?: string;
}

/**
 * Standardized fallback component for React Suspense boundaries
 * Shows a centered spinner with optional loading message
 */
export function SuspenseFallback({
  message = 'Loading...',
  minHeight = '200px',
}: SuspenseFallbackProps) {
  const styles = useStyles();

  return (
    <div
      className={styles.container}
      style={{ minHeight }}
      role="status"
      aria-live="polite"
      aria-label={message}
    >
      <Spinner
        className={styles.spinner}
        size="extra-large"
        label={message}
        labelPosition="below"
      />
    </div>
  );
}

/**
 * Minimal fallback for smaller UI sections
 */
export function SuspenseFallbackMinimal() {
  return (
    <div
      style={{
        display: 'flex',
        justifyContent: 'center',
        padding: '1rem',
      }}
      role="status"
      aria-live="polite"
      aria-label="Loading"
    >
      <Spinner size="small" />
    </div>
  );
}

/**
 * Full page fallback for route transitions
 */
export function SuspenseFallbackFullPage({ message = 'Loading page...' }: SuspenseFallbackProps) {
  const styles = useStyles();

  return (
    <div
      className={styles.container}
      style={{ minHeight: '100vh' }}
      role="status"
      aria-live="polite"
      aria-label={message}
    >
      <Spinner
        className={styles.spinner}
        size="extra-large"
        label={message}
        labelPosition="below"
      />
    </div>
  );
}
