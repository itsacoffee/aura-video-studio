import {
  makeStyles,
  tokens,
  Text,
  Button,
  ProgressBar,
  Badge,
  useToastController,
  useId,
  Toaster,
} from '@fluentui/react-components';
import { ChevronUp24Regular, ChevronDown24Regular } from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import { getVersion } from '../../services/api/versionApi';
import { useActivity, type Activity } from '../../state/activityContext';
import type { VersionInfo } from '../../types/api-v1';
import { ToastNotification } from '../Notifications/Toast';
import { ActivityDrawer } from '../StatusBar/ActivityDrawer';
import { ResourceMonitor } from '../StatusBar/ResourceMonitor';
import { BackendStatusIndicator } from '../StatusBar/BackendStatusIndicator';

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
    minHeight: '48px',
    maxHeight: '48px',
    transition: 'all 0.3s ease-in-out',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: '8px 20px',
    minHeight: '48px',
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
    gap: '12px',
  },
  compactProgress: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
    minWidth: '200px',
  },
  progressBar: {
    flex: 1,
    minWidth: '120px',
  },
  statusText: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  versionText: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground4,
    marginRight: '8px',
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

export function GlobalStatusFooter() {
  const styles = useStyles();
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [version, setVersion] = useState<VersionInfo | null>(null);
  const toasterId = useId('status-toaster');
  const { dispatchToast } = useToastController(toasterId);

  const {
    activities,
    activeActivities,
    queuedActivities,
    pausedActivities,
    completedActivities,
    failedActivities,
    recentHistory,
    batchOperations,
    updateActivity,
    pauseActivity,
    resumeActivity,
    removeActivity,
    setPriority,
    clearCompleted,
    clearHistory,
  } = useActivity();

  // Track previously completed/failed activities to show toasts
  const [previousActivityStates] = useState<Map<string, Activity>>(new Map());

  // Load version information on mount
  useEffect(() => {
    getVersion()
      .then((versionInfo) => setVersion(versionInfo))
      .catch((error) => {
        console.error('Failed to load version information:', error);
      });
  }, []);

  // Show toast notifications when activities complete or fail
  useEffect(() => {
    activities.forEach((activity) => {
      const previousState = previousActivityStates.get(activity.id);

      // Check if activity just completed
      if (
        activity.status === 'completed' &&
        previousState &&
        previousState.status !== 'completed'
      ) {
        const duration = formatDuration(activity.startTime, activity.endTime);
        dispatchToast(
          <ToastNotification
            type="success"
            title="Operation Completed"
            message={activity.title}
            duration={duration}
            onOpenFile={
              activity.artifactPath
                ? () => {
                    // In production, this would open the file location
                    // console.info('Open file:', activity.artifactPath);
                  }
                : undefined
            }
            showOpenButton={!!activity.artifactPath}
          />,
          { intent: 'success', timeout: 5000 }
        );
      }

      // Check if activity just failed
      if (activity.status === 'failed' && previousState && previousState.status !== 'failed') {
        dispatchToast(
          <ToastNotification
            type="error"
            title="Operation Failed"
            message={`${activity.title}: ${activity.error || 'Unknown error'}`}
          />,
          { intent: 'error', timeout: 10000 }
        );
      }

      // Update previous state
      previousActivityStates.set(activity.id, { ...activity });
    });
  }, [activities, dispatchToast, previousActivityStates]);

  // Don't render footer if there are no activities
  if (activities.length === 0 && recentHistory.length === 0) {
    return (
      <>
        <Toaster toasterId={toasterId} position="top-end" />
      </>
    );
  }

  // Get the primary active operation to show in collapsed state
  const primaryOperation = activeActivities[0] || pausedActivities[0] || queuedActivities[0];

  const activeCount = activeActivities.length + pausedActivities.length;
  const failedCount = failedActivities.length;

  const getSummaryText = () => {
    if (primaryOperation) {
      return primaryOperation.title;
    }
    if (activeCount > 0) {
      return `${activeCount} active operation${activeCount > 1 ? 's' : ''}`;
    }
    if (failedCount > 0) {
      return `${failedCount} failed operation${failedCount > 1 ? 's' : ''}`;
    }
    return 'All operations complete';
  };

  const handleRetryActivity = (activity: Activity) => {
    updateActivity(activity.id, {
      status: 'pending',
      progress: 0,
      error: undefined,
      endTime: undefined,
    });
  };

  return (
    <>
      <div className={styles.footer}>
        <div
          className={styles.header}
          onClick={() => setIsDrawerOpen(!isDrawerOpen)}
          onKeyDown={(e) => {
            if (e.key === 'Enter' || e.key === ' ') {
              e.preventDefault();
              setIsDrawerOpen(!isDrawerOpen);
            }
          }}
          role="button"
          tabIndex={0}
          aria-expanded={isDrawerOpen}
          aria-label="Toggle activity drawer"
        >
          <div className={styles.headerLeft}>
            {isDrawerOpen ? <ChevronDown24Regular /> : <ChevronUp24Regular />}

            {primaryOperation && primaryOperation.status === 'running' && (
              <div className={styles.compactProgress}>
                <ProgressBar
                  className={styles.progressBar}
                  value={primaryOperation.progress / 100}
                />
                <Text weight="semibold" size={200}>
                  {primaryOperation.progress}%
                </Text>
              </div>
            )}

            <Text className={styles.statusText}>{getSummaryText()}</Text>

            {primaryOperation?.details?.timeRemaining && (
              <Text className={styles.statusText}>
                ~
                {formatDuration(
                  new Date(),
                  new Date(Date.now() + primaryOperation.details.timeRemaining * 1000)
                )}{' '}
                remaining
              </Text>
            )}

            {activeCount > 0 && (
              <Badge appearance="filled" color="informative" size="small">
                {activeCount}
              </Badge>
            )}
            {failedCount > 0 && (
              <Badge appearance="filled" color="danger" size="small">
                {failedCount}
              </Badge>
            )}
          </div>

          <div className={styles.headerRight}>
            {version && (
              <Text
                className={styles.versionText}
                title={`Build: ${version.buildDate} | Runtime: ${version.runtimeVersion}`}
              >
                v{version.semanticVersion}
              </Text>
            )}
            <BackendStatusIndicator />
            <ResourceMonitor compact />
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
      </div>

      <ActivityDrawer
        isOpen={isDrawerOpen}
        onClose={() => setIsDrawerOpen(false)}
        activeActivities={activeActivities}
        queuedActivities={queuedActivities}
        pausedActivities={pausedActivities}
        completedActivities={completedActivities}
        recentHistory={recentHistory}
        batchOperations={batchOperations}
        onPause={pauseActivity}
        onResume={resumeActivity}
        onCancel={(id) => updateActivity(id, { status: 'cancelled' })}
        onRetry={handleRetryActivity}
        onPriorityChange={setPriority}
        onClearHistory={clearHistory}
        onRemoveFromHistory={removeActivity}
      />

      <Toaster toasterId={toasterId} position="top-end" />
    </>
  );
}
