/**
 * ExportProgress Component
 *
 * Displays export progress with a progress bar, percentage,
 * time remaining estimate, current stage indicator, and cancel button.
 */

import {
  makeStyles,
  tokens,
  Text,
  Button,
  ProgressBar,
  mergeClasses,
} from '@fluentui/react-components';
import { Dismiss24Regular, Checkmark24Regular } from '@fluentui/react-icons';
import { useMemo } from 'react';
import type { FC } from 'react';
import { useExportStore } from '../../../stores/opencutExport';

export interface ExportProgressProps {
  className?: string;
  onComplete?: () => void;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  title: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: 600,
  },
  progressSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  progressBar: {
    height: '8px',
  },
  progressInfo: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  percentage: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: 600,
    color: tokens.colorBrandForeground1,
  },
  stage: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  timeRemaining: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  actions: {
    display: 'flex',
    justifyContent: 'flex-end',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
  },
  completeContainer: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalL,
  },
  completeIcon: {
    width: '64px',
    height: '64px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: 'rgba(34, 197, 94, 0.2)',
    borderRadius: '50%',
    color: '#22C55E',
    fontSize: '32px',
  },
  completeText: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: 600,
    color: tokens.colorNeutralForeground1,
  },
  errorContainer: {
    padding: tokens.spacingVerticalM,
    backgroundColor: 'rgba(239, 68, 68, 0.1)',
    borderRadius: tokens.borderRadiusSmall,
    border: `1px solid rgba(239, 68, 68, 0.3)`,
  },
  errorText: {
    fontSize: tokens.fontSizeBase200,
    color: '#EF4444',
  },
});

const getExportStage = (progress: number): string => {
  if (progress < 10) return 'Preparing...';
  if (progress < 30) return 'Encoding video...';
  if (progress < 60) return 'Processing audio...';
  if (progress < 80) return 'Applying effects...';
  if (progress < 95) return 'Finalizing...';
  return 'Completing...';
};

const estimateTimeRemaining = (progress: number, _startTime?: number): string => {
  if (progress === 0 || progress >= 100) return '';
  // Simple estimation based on progress
  const estimatedTotalSeconds = 120; // Assume 2 minutes total for estimation
  const remainingSeconds = Math.round((estimatedTotalSeconds * (100 - progress)) / 100);
  if (remainingSeconds < 60) {
    return `${remainingSeconds}s remaining`;
  }
  const minutes = Math.floor(remainingSeconds / 60);
  const seconds = remainingSeconds % 60;
  return `${minutes}m ${seconds}s remaining`;
};

export const ExportProgress: FC<ExportProgressProps> = ({ className, onComplete }) => {
  const styles = useStyles();
  const { exportProgress, isExporting, exportError, cancelExport, clearExportError } =
    useExportStore();

  const stage = useMemo(() => getExportStage(exportProgress), [exportProgress]);
  const timeRemaining = useMemo(() => estimateTimeRemaining(exportProgress), [exportProgress]);

  const isComplete = exportProgress >= 100 && !isExporting;

  if (isComplete && !exportError) {
    return (
      <div className={mergeClasses(styles.container, styles.completeContainer, className)}>
        <div className={styles.completeIcon}>
          <Checkmark24Regular />
        </div>
        <Text className={styles.completeText}>Export Complete!</Text>
        <Button appearance="primary" onClick={onComplete}>
          Done
        </Button>
      </div>
    );
  }

  return (
    <div className={mergeClasses(styles.container, className)}>
      <div className={styles.header}>
        <Text className={styles.title}>{isExporting ? 'Exporting Video...' : 'Export'}</Text>
      </div>

      {exportError && (
        <div className={styles.errorContainer}>
          <Text className={styles.errorText}>Error: {exportError}</Text>
        </div>
      )}

      <div className={styles.progressSection}>
        <ProgressBar
          className={styles.progressBar}
          value={exportProgress / 100}
          max={1}
          shape="rounded"
        />
        <div className={styles.progressInfo}>
          <Text className={styles.percentage}>{Math.round(exportProgress)}%</Text>
          <Text className={styles.stage}>{stage}</Text>
        </div>
        {timeRemaining && <Text className={styles.timeRemaining}>{timeRemaining}</Text>}
      </div>

      <div className={styles.actions}>
        {exportError && (
          <Button appearance="secondary" onClick={clearExportError}>
            Dismiss Error
          </Button>
        )}
        {isExporting && (
          <Button appearance="secondary" icon={<Dismiss24Regular />} onClick={cancelExport}>
            Cancel
          </Button>
        )}
      </div>
    </div>
  );
};

export default ExportProgress;
