import { useState } from 'react';
import {
  makeStyles,
  tokens,
  Text,
  Button,
  ProgressBar,
  Badge,
  Tooltip,
} from '@fluentui/react-components';
import {
  ChevronUp24Regular,
  ChevronDown24Regular,
  Dismiss24Regular,
  CheckmarkCircle24Regular,
  ErrorCircle24Regular,
  Clock24Regular,
  ArrowClockwise24Regular,
  DismissCircle24Regular,
} from '@fluentui/react-icons';
import { useActivity, type Activity } from '../../state/activityContext';

const useStyles = makeStyles({
  footer: {
    position: 'fixed',
    bottom: 0,
    left: 0,
    right: 0,
    backgroundColor: tokens.colorNeutralBackground2,
    borderTop: `1px solid ${tokens.colorNeutralStroke1}`,
    boxShadow: '0 -2px 8px rgba(0, 0, 0, 0.1)',
    zIndex: 1000,
    display: 'flex',
    flexDirection: 'column',
    maxHeight: '60vh',
    transition: 'all 0.3s ease-in-out',
  },
  collapsed: {
    maxHeight: '60px',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: '12px 20px',
    minHeight: '60px',
    cursor: 'pointer',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground3,
    },
  },
  headerLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
    flex: 1,
  },
  headerRight: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
  },
  activityList: {
    overflowY: 'auto',
    padding: '0 20px 20px 20px',
    display: 'flex',
    flexDirection: 'column',
    gap: '12px',
  },
  activityItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: '8px',
    padding: '12px',
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  activityItemRunning: {
    borderLeft: `4px solid ${tokens.colorBrandBackground}`,
  },
  activityItemCompleted: {
    borderLeft: `4px solid ${tokens.colorPaletteGreenBorder2}`,
  },
  activityItemFailed: {
    borderLeft: `4px solid ${tokens.colorPaletteRedBorder2}`,
  },
  activityHeader: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: '8px',
  },
  activityTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
    flex: 1,
  },
  activityActions: {
    display: 'flex',
    alignItems: 'center',
    gap: '4px',
  },
  activityMessage: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  activityProgress: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
  },
  activityTime: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    display: 'flex',
    alignItems: 'center',
    gap: '4px',
  },
  errorMessage: {
    padding: '8px',
    backgroundColor: tokens.colorPaletteRedBackground1,
    borderRadius: tokens.borderRadiusSmall,
    color: tokens.colorPaletteRedForeground1,
    fontSize: tokens.fontSizeBase200,
  },
  emptyState: {
    padding: '20px',
    textAlign: 'center',
    color: tokens.colorNeutralForeground3,
  },
  badge: {
    fontWeight: tokens.fontWeightSemibold,
  },
});

function formatDuration(startTime: Date, endTime?: Date): string {
  const end = endTime || new Date();
  const duration = Math.floor((end.getTime() - startTime.getTime()) / 1000);
  
  if (duration < 60) {
    return `${duration}s`;
  } else if (duration < 3600) {
    const minutes = Math.floor(duration / 60);
    const seconds = duration % 60;
    return `${minutes}m ${seconds}s`;
  } else {
    const hours = Math.floor(duration / 3600);
    const minutes = Math.floor((duration % 3600) / 60);
    return `${hours}h ${minutes}m`;
  }
}

function ActivityItemComponent({ activity }: { activity: Activity }) {
  const styles = useStyles();
  const { updateActivity, removeActivity } = useActivity();

  const getIcon = () => {
    switch (activity.status) {
      case 'completed':
        return <CheckmarkCircle24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />;
      case 'failed':
        return <ErrorCircle24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />;
      default:
        return <Clock24Regular style={{ color: tokens.colorBrandForeground1 }} />;
    }
  };

  const getItemClass = () => {
    switch (activity.status) {
      case 'running':
        return styles.activityItemRunning;
      case 'completed':
        return styles.activityItemCompleted;
      case 'failed':
        return styles.activityItemFailed;
      default:
        return '';
    }
  };

  const handleCancel = () => {
    updateActivity(activity.id, { status: 'cancelled' });
  };

  const handleRetry = () => {
    // Reset activity to pending state
    updateActivity(activity.id, {
      status: 'pending',
      progress: 0,
      error: undefined,
      endTime: undefined,
    });
  };

  const handleDismiss = () => {
    removeActivity(activity.id);
  };

  return (
    <div className={`${styles.activityItem} ${getItemClass()}`}>
      <div className={styles.activityHeader}>
        <div className={styles.activityTitle}>
          {getIcon()}
          <Text weight="semibold">{activity.title}</Text>
        </div>
        <div className={styles.activityActions}>
          {activity.status === 'running' && activity.canCancel && (
            <Tooltip content="Cancel" relationship="label">
              <Button
                appearance="subtle"
                size="small"
                icon={<DismissCircle24Regular />}
                onClick={handleCancel}
              />
            </Tooltip>
          )}
          {activity.status === 'failed' && activity.canRetry && (
            <Tooltip content="Retry" relationship="label">
              <Button
                appearance="subtle"
                size="small"
                icon={<ArrowClockwise24Regular />}
                onClick={handleRetry}
              />
            </Tooltip>
          )}
          {(activity.status === 'completed' || activity.status === 'failed') && (
            <Tooltip content="Dismiss" relationship="label">
              <Button
                appearance="subtle"
                size="small"
                icon={<Dismiss24Regular />}
                onClick={handleDismiss}
              />
            </Tooltip>
          )}
        </div>
      </div>

      {activity.message && (
        <Text className={styles.activityMessage}>{activity.message}</Text>
      )}

      {activity.status === 'running' && (
        <div className={styles.activityProgress}>
          <ProgressBar value={activity.progress / 100} />
          <Text size={200}>{activity.progress}%</Text>
        </div>
      )}

      {activity.error && (
        <div className={styles.errorMessage}>
          <Text size={200}>{activity.error}</Text>
        </div>
      )}

      <div className={styles.activityTime}>
        <Clock24Regular fontSize={14} />
        <Text size={200}>
          {formatDuration(activity.startTime, activity.endTime)}
          {activity.endTime && ` (completed)`}
        </Text>
      </div>
    </div>
  );
}

export function GlobalStatusFooter() {
  const styles = useStyles();
  const [isExpanded, setIsExpanded] = useState(true);
  const { 
    activities, 
    activeActivities, 
    failedActivities,
    clearCompleted,
  } = useActivity();

  // Don't render footer if there are no activities
  if (activities.length === 0) {
    return null;
  }

  const activeCount = activeActivities.length;
  const failedCount = failedActivities.length;

  const getSummaryText = () => {
    const parts = [];
    if (activeCount > 0) {
      parts.push(`${activeCount} active`);
    }
    if (failedCount > 0) {
      parts.push(`${failedCount} failed`);
    }
    if (parts.length === 0) {
      parts.push('All tasks complete');
    }
    return parts.join(', ');
  };

  return (
    <div className={`${styles.footer} ${!isExpanded ? styles.collapsed : ''}`}>
      <div className={styles.header} onClick={() => setIsExpanded(!isExpanded)}>
        <div className={styles.headerLeft}>
          {isExpanded ? <ChevronDown24Regular /> : <ChevronUp24Regular />}
          <Text weight="semibold">Activity Status</Text>
          <Text className={styles.badge}>{getSummaryText()}</Text>
          {activeCount > 0 && (
            <Badge appearance="filled" color="informative">
              {activeCount}
            </Badge>
          )}
          {failedCount > 0 && (
            <Badge appearance="filled" color="danger">
              {failedCount}
            </Badge>
          )}
        </div>
        <div className={styles.headerRight}>
          <Button
            appearance="subtle"
            size="small"
            onClick={(e) => {
              e.stopPropagation();
              clearCompleted();
            }}
          >
            Clear Completed
          </Button>
        </div>
      </div>

      {isExpanded && (
        <div className={styles.activityList}>
          {activities.length === 0 ? (
            <div className={styles.emptyState}>
              <Text>No activities to display</Text>
            </div>
          ) : (
            activities
              .slice()
              .reverse()
              .map(activity => (
                <ActivityItemComponent key={activity.id} activity={activity} />
              ))
          )}
        </div>
      )}
    </div>
  );
}
