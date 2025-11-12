import { makeStyles, tokens, Text } from '@fluentui/react-components';
import { GradientProgressBar } from './GradientProgressBar';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  title: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground1,
  },
  percentage: {
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightBold,
    color: tokens.colorBrandForeground1,
    fontFamily: 'monospace',
  },
  progressBarContainer: {
    width: '100%',
  },
  details: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  detailText: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  statusText: {
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground2,
  },
});

interface ProgressIndicatorProps {
  /**
   * Current progress percentage (0-100)
   */
  progress: number;
  /**
   * Title/label for the progress indicator
   */
  title?: string;
  /**
   * Current status message
   */
  status?: string;
  /**
   * Estimated time remaining in seconds
   */
  estimatedTimeRemaining?: number;
  /**
   * Whether to show the percentage value
   */
  showPercentage?: boolean;
  /**
   * Whether to show estimated time remaining
   */
  showTimeRemaining?: boolean;
  /**
   * ARIA label for accessibility
   */
  ariaLabel?: string;
}

/**
 * Progress indicator component for long-running operations
 * Shows progress percentage, status message, and estimated time remaining
 */
export function ProgressIndicator({
  progress,
  title = 'Processing',
  status,
  estimatedTimeRemaining,
  showPercentage = true,
  showTimeRemaining = true,
  ariaLabel,
}: ProgressIndicatorProps) {
  const styles = useStyles();

  const formatTime = (seconds: number): string => {
    if (seconds < 60) {
      return `${Math.round(seconds)}s`;
    }
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = Math.round(seconds % 60);
    return `${minutes}m ${remainingSeconds}s`;
  };

  const progressValue = Math.min(100, Math.max(0, progress));
  const effectiveAriaLabel = ariaLabel || `${title} - ${progressValue}% complete`;

  return (
    <div
      className={styles.container}
      role="status"
      aria-label={effectiveAriaLabel}
      aria-live="polite"
    >
      <div className={styles.header}>
        <Text className={styles.title}>{title}</Text>
        {showPercentage && <Text className={styles.percentage}>{progressValue}%</Text>}
      </div>

      <div className={styles.progressBarContainer}>
        <GradientProgressBar
          value={progressValue}
          max={100}
          thickness="large"
          aria-label={effectiveAriaLabel}
        />
      </div>

      {(status || (showTimeRemaining && estimatedTimeRemaining !== undefined)) && (
        <div className={styles.details}>
          {status && <Text className={styles.statusText}>{status}</Text>}
          {showTimeRemaining &&
            estimatedTimeRemaining !== undefined &&
            estimatedTimeRemaining > 0 && (
              <Text className={styles.detailText}>
                {formatTime(estimatedTimeRemaining)} remaining
              </Text>
            )}
        </div>
      )}
    </div>
  );
}
