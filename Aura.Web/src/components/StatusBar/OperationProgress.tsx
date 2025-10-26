import { useState, useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Text,
  ProgressBar,
  Button,
  Tooltip,
  Badge,
} from '@fluentui/react-components';
import {
  Pause24Regular,
  Play24Regular,
  DismissCircle24Regular,
  ChevronDown24Regular,
  ChevronUp24Regular,
  ArrowClockwise24Regular,
  ArrowUp24Regular,
  ArrowDown24Regular,
  CheckmarkCircle24Regular,
  ErrorCircle24Regular,
  Clock24Regular,
  CloudArrowDown24Regular,
  CloudArrowUp24Regular,
  Lightbulb24Regular,
  Star24Regular,
} from '@fluentui/react-icons';
import { Activity, ActivityCategory, ActivityStatus } from '../../state/activityContext';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  containerRunning: {
    borderLeft: `4px solid ${tokens.colorBrandBackground}`,
  },
  containerCompleted: {
    borderLeft: `4px solid ${tokens.colorPaletteGreenBorder2}`,
  },
  containerFailed: {
    borderLeft: `4px solid ${tokens.colorPaletteRedBorder2}`,
  },
  containerPaused: {
    borderLeft: `4px solid ${tokens.colorPaletteYellowBorder2}`,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: tokens.spacingHorizontalS,
  },
  headerLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    flex: 1,
  },
  title: {
    fontWeight: tokens.fontWeightSemibold,
  },
  actions: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  progressSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  progressBar: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  progressDetails: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalM,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  detailsExpanded: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    paddingTop: tokens.spacingVerticalS,
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
    fontSize: tokens.fontSizeBase200,
  },
  detailRow: {
    display: 'flex',
    justifyContent: 'space-between',
    gap: tokens.spacingHorizontalM,
  },
  prioritySlider: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
});

interface OperationProgressProps {
  activity: Activity;
  onPause?: (id: string) => void;
  onResume?: (id: string) => void;
  onCancel?: (id: string) => void;
  onRetry?: (id: string) => void;
  onPriorityChange?: (id: string, priority: number) => void;
  compact?: boolean;
}

function formatTime(seconds: number): string {
  if (seconds < 60) {
    return `${Math.round(seconds)}s`;
  } else if (seconds < 3600) {
    const minutes = Math.floor(seconds / 60);
    const secs = Math.round(seconds % 60);
    return `${minutes}m ${secs}s`;
  } else {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    return `${hours}h ${minutes}m`;
  }
}

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  if (bytes < 1024 * 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  return `${(bytes / (1024 * 1024 * 1024)).toFixed(2)} GB`;
}

function getCategoryIcon(category?: ActivityCategory) {
  switch (category) {
    case 'import':
      return <CloudArrowDown24Regular />;
    case 'export':
      return <CloudArrowUp24Regular />;
    case 'analysis':
      return <Lightbulb24Regular />;
    case 'effects':
      return <Star24Regular />;
    default:
      return <Clock24Regular />;
  }
}

function getStatusIcon(status: ActivityStatus) {
  switch (status) {
    case 'completed':
      return <CheckmarkCircle24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />;
    case 'failed':
      return <ErrorCircle24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />;
    case 'paused':
      return <Pause24Regular style={{ color: tokens.colorPaletteYellowForeground1 }} />;
    default:
      return <Clock24Regular style={{ color: tokens.colorBrandForeground1 }} />;
  }
}

export function OperationProgress({
  activity,
  onPause,
  onResume,
  onCancel,
  onRetry,
  onPriorityChange,
  compact = false,
}: OperationProgressProps) {
  const styles = useStyles();
  const [showDetails, setShowDetails] = useState(false);
  const [timeElapsed, setTimeElapsed] = useState(0);

  // Update elapsed time
  useEffect(() => {
    if (activity.status !== 'running') {
      if (activity.endTime && activity.startTime) {
        setTimeElapsed((activity.endTime.getTime() - activity.startTime.getTime()) / 1000);
      }
      return;
    }

    const updateTime = () => {
      const elapsed = (new Date().getTime() - activity.startTime.getTime()) / 1000;
      setTimeElapsed(elapsed);
    };

    updateTime();
    const interval = setInterval(updateTime, 500);

    return () => clearInterval(interval);
  }, [activity.status, activity.startTime, activity.endTime]);

  const getContainerClass = () => {
    switch (activity.status) {
      case 'running':
        return styles.containerRunning;
      case 'completed':
        return styles.containerCompleted;
      case 'failed':
        return styles.containerFailed;
      case 'paused':
        return styles.containerPaused;
      default:
        return '';
    }
  };

  const estimatedTimeRemaining = activity.details?.timeRemaining || 
    (activity.progress > 0 && activity.progress < 100 
      ? (timeElapsed / activity.progress) * (100 - activity.progress)
      : undefined);

  if (compact) {
    return (
      <div className={`${styles.container} ${getContainerClass()}`}>
        <div className={styles.header}>
          <div className={styles.headerLeft}>
            {getStatusIcon(activity.status)}
            <Text className={styles.title}>{activity.title}</Text>
            {activity.status === 'running' && (
              <Badge appearance="filled" color="informative" size="small">
                {activity.progress}%
              </Badge>
            )}
          </div>
          <div className={styles.actions}>
            {activity.status === 'running' && activity.canPause && onPause && (
              <Tooltip content="Pause" relationship="label">
                <Button
                  appearance="subtle"
                  size="small"
                  icon={<Pause24Regular />}
                  onClick={() => onPause(activity.id)}
                />
              </Tooltip>
            )}
            {activity.status === 'paused' && onResume && (
              <Tooltip content="Resume" relationship="label">
                <Button
                  appearance="subtle"
                  size="small"
                  icon={<Play24Regular />}
                  onClick={() => onResume(activity.id)}
                />
              </Tooltip>
            )}
            {(activity.status === 'running' || activity.status === 'paused') && activity.canCancel && onCancel && (
              <Tooltip content="Cancel" relationship="label">
                <Button
                  appearance="subtle"
                  size="small"
                  icon={<DismissCircle24Regular />}
                  onClick={() => onCancel(activity.id)}
                />
              </Tooltip>
            )}
          </div>
        </div>
        {activity.status === 'running' && (
          <ProgressBar value={activity.progress / 100} />
        )}
      </div>
    );
  }

  return (
    <div className={`${styles.container} ${getContainerClass()}`}>
      <div className={styles.header}>
        <div className={styles.headerLeft}>
          {getCategoryIcon(activity.category)}
          {getStatusIcon(activity.status)}
          <Text className={styles.title}>{activity.title}</Text>
        </div>
        <div className={styles.actions}>
          {activity.status === 'running' && activity.canPause && onPause && (
            <Tooltip content="Pause" relationship="label">
              <Button
                appearance="subtle"
                size="small"
                icon={<Pause24Regular />}
                onClick={() => onPause(activity.id)}
              />
            </Tooltip>
          )}
          {activity.status === 'paused' && onResume && (
            <Tooltip content="Resume" relationship="label">
              <Button
                appearance="subtle"
                size="small"
                icon={<Play24Regular />}
                onClick={() => onResume(activity.id)}
              />
            </Tooltip>
          )}
          {(activity.status === 'running' || activity.status === 'paused') && activity.canCancel && onCancel && (
            <Tooltip content="Cancel" relationship="label">
              <Button
                appearance="subtle"
                size="small"
                icon={<DismissCircle24Regular />}
                onClick={() => onCancel(activity.id)}
              />
            </Tooltip>
          )}
          {activity.status === 'failed' && activity.canRetry && onRetry && (
            <Tooltip content="Retry" relationship="label">
              <Button
                appearance="subtle"
                size="small"
                icon={<ArrowClockwise24Regular />}
                onClick={() => onRetry(activity.id)}
              />
            </Tooltip>
          )}
          <Tooltip content={showDetails ? 'Hide Details' : 'View Details'} relationship="label">
            <Button
              appearance="subtle"
              size="small"
              icon={showDetails ? <ChevronUp24Regular /> : <ChevronDown24Regular />}
              onClick={() => setShowDetails(!showDetails)}
            />
          </Tooltip>
        </div>
      </div>

      {activity.message && (
        <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
          {activity.message}
        </Text>
      )}

      {(activity.status === 'running' || activity.status === 'paused') && (
        <div className={styles.progressSection}>
          <div className={styles.progressBar}>
            <ProgressBar value={activity.progress / 100} style={{ flex: 1 }} />
            <Text weight="semibold">{activity.progress}%</Text>
          </div>

          <div className={styles.progressDetails}>
            {activity.details?.currentItem && activity.details?.totalItems && (
              <Text>
                {activity.details.currentItem} / {activity.details.totalItems} items
              </Text>
            )}
            {activity.details?.speed && (
              <Text>
                {activity.details.speed.toFixed(1)} {activity.details.speedUnit || 'MB/s'}
              </Text>
            )}
            <Text>Elapsed: {formatTime(timeElapsed)}</Text>
            {estimatedTimeRemaining !== undefined && (
              <Text>Remaining: {formatTime(estimatedTimeRemaining)}</Text>
            )}
          </div>
        </div>
      )}

      {activity.error && (
        <div style={{ 
          padding: tokens.spacingVerticalS,
          backgroundColor: tokens.colorPaletteRedBackground1,
          borderRadius: tokens.borderRadiusSmall,
        }}>
          <Text size={200} style={{ color: tokens.colorPaletteRedForeground1 }}>
            {activity.error}
          </Text>
        </div>
      )}

      {showDetails && (
        <div className={styles.detailsExpanded}>
          <div className={styles.detailRow}>
            <Text>Started:</Text>
            <Text>{activity.startTime.toLocaleString()}</Text>
          </div>
          {activity.endTime && (
            <div className={styles.detailRow}>
              <Text>Ended:</Text>
              <Text>{activity.endTime.toLocaleString()}</Text>
            </div>
          )}
          <div className={styles.detailRow}>
            <Text>Duration:</Text>
            <Text>{formatTime(timeElapsed)}</Text>
          </div>
          {activity.details?.bytesProcessed && activity.details?.bytesTotal && (
            <div className={styles.detailRow}>
              <Text>Data:</Text>
              <Text>
                {formatBytes(activity.details.bytesProcessed)} / {formatBytes(activity.details.bytesTotal)}
              </Text>
            </div>
          )}
          {activity.status === 'pending' && onPriorityChange && (
            <div className={styles.prioritySlider}>
              <Text>Priority:</Text>
              <Button
                appearance="subtle"
                size="small"
                icon={<ArrowDown24Regular />}
                onClick={() => onPriorityChange(activity.id, (activity.priority || 5) - 1)}
                disabled={(activity.priority || 5) <= 1}
              />
              <Text weight="semibold">{activity.priority || 5}</Text>
              <Button
                appearance="subtle"
                size="small"
                icon={<ArrowUp24Regular />}
                onClick={() => onPriorityChange(activity.id, (activity.priority || 5) + 1)}
                disabled={(activity.priority || 5) >= 10}
              />
            </div>
          )}
        </div>
      )}
    </div>
  );
}
