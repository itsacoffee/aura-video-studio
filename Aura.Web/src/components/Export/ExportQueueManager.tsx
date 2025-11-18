import {
  makeStyles,
  tokens,
  Body1,
  Caption1,
  ProgressBar,
  Button,
  Badge,
} from '@fluentui/react-components';
import {
  Dismiss24Regular,
  CheckmarkCircle24Regular,
  ErrorCircle24Regular,
  Clock24Regular,
  Play24Regular,
  Pause24Regular,
  ArrowRepeatAll24Regular,
} from '@fluentui/react-icons';
import { useJobQueue } from '../../hooks/useJobQueue';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    maxHeight: '600px',
    overflowY: 'auto',
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXL,
    gap: tokens.spacingVerticalM,
    color: tokens.colorNeutralForeground3,
  },
  jobItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  jobHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
  },
  jobInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    flex: 1,
  },
  jobActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
  },
  statusBadge: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  progressSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  progressInfo: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
});

// Map backend job status to UI status
const mapJobStatus = (
  status: string
): 'queued' | 'processing' | 'paused' | 'completed' | 'failed' => {
  switch (status) {
    case 'Pending':
      return 'queued';
    case 'Processing':
      return 'processing';
    case 'Completed':
      return 'completed';
    case 'Failed':
      return 'failed';
    case 'Cancelled':
      return 'failed';
    default:
      return 'queued';
  }
};

export function ExportQueueManager() {
  const styles = useStyles();

  // Use the job queue hook
  const { jobs: queueJobs, cancelJob } = useJobQueue();

  // Map backend jobs to export jobs format
  const jobs = queueJobs.map((job) => ({
    id: job.jobId,
    platform: job.isQuickDemo ? 'Quick Demo' : 'Export',
    status: mapJobStatus(job.status),
    progress: job.progress,
    fileName: job.outputPath ? job.outputPath.split('/').pop() || 'output.mp4' : 'processing...',
    createdAt: new Date(job.enqueuedAt),
    estimatedTimeRemaining: undefined, // Backend doesn't provide this yet
    encodingSpeed: undefined,
    error: job.errorMessage || undefined,
  }));

  const handleCancel = async (jobId: string) => {
    try {
      await cancelJob(jobId);
    } catch (error) {
      console.error('Failed to cancel job:', error);
    }
  };

  const handlePause = (_jobId: string) => {
    // TODO: Implement pause functionality in backend
  };

  const handleResume = (_jobId: string) => {
    // TODO: Implement resume functionality in backend
  };

  const handleRetry = (_jobId: string) => {
    // TODO: Implement retry functionality in backend
  };

  const getStatusIcon = (status: 'queued' | 'processing' | 'paused' | 'completed' | 'failed') => {
    switch (status) {
      case 'completed':
        return <CheckmarkCircle24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />;
      case 'failed':
        return <ErrorCircle24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />;
      case 'processing':
        return <Play24Regular style={{ color: tokens.colorBrandForeground1 }} />;
      case 'paused':
        return <Pause24Regular style={{ color: tokens.colorPaletteYellowForeground1 }} />;
      case 'queued':
        return <Clock24Regular style={{ color: tokens.colorNeutralForeground3 }} />;
    }
  };

  const getStatusColor = (status: 'queued' | 'processing' | 'paused' | 'completed' | 'failed') => {
    switch (status) {
      case 'completed':
        return 'success';
      case 'failed':
        return 'danger';
      case 'processing':
        return 'brand';
      case 'paused':
        return 'warning';
      case 'queued':
        return 'subtle';
      default:
        return 'subtle';
    }
  };

  if (jobs.length === 0) {
    return (
      <div className={styles.emptyState}>
        <Clock24Regular style={{ fontSize: '48px' }} />
        <Body1>No export jobs in queue</Body1>
        <Caption1>Export jobs will appear here when you add them to the queue</Caption1>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      {jobs.map((job) => (
        <div key={job.id} className={styles.jobItem}>
          <div className={styles.jobHeader}>
            <div className={styles.jobInfo}>
              <div className={styles.statusBadge}>
                {getStatusIcon(job.status)}
                <Body1>{job.platform}</Body1>
              </div>
              <Caption1>{job.fileName}</Caption1>
              <Badge size="small" appearance="tint" color={getStatusColor(job.status)}>
                {job.status.charAt(0).toUpperCase() + job.status.slice(1)}
              </Badge>
            </div>
            <div className={styles.jobActions}>
              {job.status === 'queued' && (
                <Button
                  appearance="subtle"
                  size="small"
                  icon={<Dismiss24Regular />}
                  onClick={() => handleCancel(job.id)}
                  aria-label="Cancel export"
                />
              )}
              {job.status === 'processing' && (
                <Button
                  appearance="subtle"
                  size="small"
                  icon={<Pause24Regular />}
                  onClick={() => handlePause(job.id)}
                  aria-label="Pause export"
                />
              )}
              {job.status === 'paused' && (
                <>
                  <Button
                    appearance="subtle"
                    size="small"
                    icon={<Play24Regular />}
                    onClick={() => handleResume(job.id)}
                    aria-label="Resume export"
                  />
                  <Button
                    appearance="subtle"
                    size="small"
                    icon={<Dismiss24Regular />}
                    onClick={() => handleCancel(job.id)}
                    aria-label="Cancel export"
                  />
                </>
              )}
              {job.status === 'failed' && (
                <Button
                  appearance="subtle"
                  size="small"
                  icon={<ArrowRepeatAll24Regular />}
                  onClick={() => handleRetry(job.id)}
                  aria-label="Retry export"
                />
              )}
            </div>
          </div>

          {job.status === 'processing' && (
            <div className={styles.progressSection}>
              <div className={styles.progressInfo}>
                <Caption1>{job.progress}%</Caption1>
                {job.estimatedTimeRemaining && (
                  <Caption1>~{job.estimatedTimeRemaining} remaining</Caption1>
                )}
              </div>
              <ProgressBar value={job.progress / 100} />
              {job.encodingSpeed && (
                <Caption1>{job.encodingSpeed.toFixed(1)} FPS encoding speed</Caption1>
              )}
            </div>
          )}

          {job.status === 'paused' && (
            <Caption1 style={{ color: tokens.colorPaletteYellowForeground1 }}>
              Export paused - Click resume to continue
            </Caption1>
          )}

          {job.status === 'failed' && job.error && (
            <Caption1 style={{ color: tokens.colorPaletteRedForeground1 }}>
              Error: {job.error}
            </Caption1>
          )}
        </div>
      ))}
    </div>
  );
}
