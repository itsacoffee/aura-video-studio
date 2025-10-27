import {
  makeStyles,
  tokens,
  Text,
  Button,
  Tab,
  TabList,
  Divider,
} from '@fluentui/react-components';
import { Dismiss24Regular } from '@fluentui/react-icons';
import { useState } from 'react';
import { Activity } from '../../state/activityContext';
import { OperationHistory } from './OperationHistory';
import { OperationProgress } from './OperationProgress';
import { ResourceMonitor } from './ResourceMonitor';

const useStyles = makeStyles({
  drawer: {
    position: 'fixed',
    bottom: 0,
    left: 0,
    right: 0,
    backgroundColor: tokens.colorNeutralBackground2,
    borderTop: `1px solid ${tokens.colorNeutralStroke1}`,
    boxShadow: '0 -4px 12px rgba(0, 0, 0, 0.15)',
    zIndex: 999,
    maxHeight: '60vh',
    display: 'flex',
    flexDirection: 'column',
    transition: 'transform 0.3s ease-in-out',
  },
  drawerHidden: {
    transform: 'translateY(100%)',
  },
  drawerVisible: {
    transform: 'translateY(0)',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `${tokens.spacingVerticalM} ${tokens.spacingHorizontalL}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  title: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase400,
  },
  tabs: {
    padding: `0 ${tokens.spacingHorizontalL}`,
  },
  content: {
    flex: 1,
    overflowY: 'auto',
    padding: tokens.spacingHorizontalL,
  },
  operationsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  sectionTitle: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground2,
    paddingBottom: tokens.spacingVerticalXS,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
    color: tokens.colorNeutralForeground3,
  },
  batchSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  batchHeader: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  batchTitle: {
    fontWeight: tokens.fontWeightSemibold,
  },
  batchProgress: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
});

interface ActivityDrawerProps {
  isOpen: boolean;
  onClose: () => void;
  activeActivities: Activity[];
  queuedActivities: Activity[];
  pausedActivities: Activity[];
  completedActivities: Activity[];
  recentHistory: Activity[];
  batchOperations: Map<string, Activity[]>;
  onPause?: (id: string) => void;
  onResume?: (id: string) => void;
  onCancel?: (id: string) => void;
  onRetry?: (activity: Activity) => void;
  onPriorityChange?: (id: string, priority: number) => void;
  onClearHistory?: () => void;
  onRemoveFromHistory?: (id: string) => void;
}

export function ActivityDrawer({
  isOpen,
  onClose,
  activeActivities,
  queuedActivities,
  pausedActivities,
  completedActivities,
  recentHistory,
  batchOperations,
  onPause,
  onResume,
  onCancel,
  onRetry,
  onPriorityChange,
  onClearHistory,
  onRemoveFromHistory,
}: ActivityDrawerProps) {
  const styles = useStyles();
  const [selectedTab, setSelectedTab] = useState<'active' | 'history' | 'resources'>('active');

  // Group activities into batches
  const batchIds = Array.from(batchOperations.keys());
  const unbatchedActivities = [
    ...activeActivities,
    ...queuedActivities,
    ...pausedActivities,
  ].filter((a) => !a.batchId);

  const calculateBatchProgress = (activities: Activity[]): number => {
    if (activities.length === 0) return 0;
    const total = activities.reduce((sum, a) => sum + a.progress, 0);
    return Math.round(total / activities.length);
  };

  const getBatchStatus = (activities: Activity[]): string => {
    const running = activities.filter((a) => a.status === 'running').length;
    const completed = activities.filter((a) => a.status === 'completed').length;
    const total = activities.length;

    if (completed === total) return 'All completed';
    if (running > 0) return `${running} running`;
    return 'Queued';
  };

  return (
    <div className={`${styles.drawer} ${isOpen ? styles.drawerVisible : styles.drawerHidden}`}>
      <div className={styles.header}>
        <Text className={styles.title}>Activity Center</Text>
        <Button
          appearance="subtle"
          icon={<Dismiss24Regular />}
          onClick={onClose}
          aria-label="Close activity drawer"
        />
      </div>

      <div className={styles.tabs}>
        <TabList
          selectedValue={selectedTab}
          onTabSelect={(_, data) => setSelectedTab(data.value as 'active' | 'history' | 'resources')}
        >
          <Tab value="active">
            Active Operations (
            {activeActivities.length + queuedActivities.length + pausedActivities.length})
          </Tab>
          <Tab value="history">History ({recentHistory.length})</Tab>
          <Tab value="resources">Resources</Tab>
        </TabList>
      </div>

      <div className={styles.content}>
        {selectedTab === 'active' && (
          <div className={styles.operationsList}>
            {/* Batch Operations */}
            {batchIds.length > 0 && (
              <div className={styles.section}>
                <Text className={styles.sectionTitle}>Batch Operations</Text>
                {batchIds.map((batchId) => {
                  const batchActivities = batchOperations.get(batchId) || [];
                  const batchProgress = calculateBatchProgress(batchActivities);
                  const batchStatus = getBatchStatus(batchActivities);

                  return (
                    <div key={batchId} className={styles.batchSection}>
                      <div className={styles.batchHeader}>
                        <Text className={styles.batchTitle}>
                          Batch: {batchId} ({batchActivities.length} operations)
                        </Text>
                        <Text className={styles.batchProgress}>
                          {batchProgress}% - {batchStatus}
                        </Text>
                      </div>
                      <Divider />
                      {batchActivities.map((activity) => (
                        <OperationProgress
                          key={activity.id}
                          activity={activity}
                          onPause={onPause}
                          onResume={onResume}
                          onCancel={onCancel}
                          onRetry={() => onRetry?.(activity)}
                          onPriorityChange={onPriorityChange}
                          compact
                        />
                      ))}
                    </div>
                  );
                })}
              </div>
            )}

            {/* Active Operations */}
            {activeActivities.filter((a) => !a.batchId).length > 0 && (
              <div className={styles.section}>
                <Text className={styles.sectionTitle}>Active</Text>
                {activeActivities
                  .filter((a) => !a.batchId)
                  .map((activity) => (
                    <OperationProgress
                      key={activity.id}
                      activity={activity}
                      onPause={onPause}
                      onResume={onResume}
                      onCancel={onCancel}
                      onRetry={() => onRetry?.(activity)}
                      onPriorityChange={onPriorityChange}
                    />
                  ))}
              </div>
            )}

            {/* Paused Operations */}
            {pausedActivities.filter((a) => !a.batchId).length > 0 && (
              <div className={styles.section}>
                <Text className={styles.sectionTitle}>Paused</Text>
                {pausedActivities
                  .filter((a) => !a.batchId)
                  .map((activity) => (
                    <OperationProgress
                      key={activity.id}
                      activity={activity}
                      onPause={onPause}
                      onResume={onResume}
                      onCancel={onCancel}
                      onRetry={() => onRetry?.(activity)}
                      onPriorityChange={onPriorityChange}
                    />
                  ))}
              </div>
            )}

            {/* Queued Operations */}
            {queuedActivities.filter((a) => !a.batchId).length > 0 && (
              <div className={styles.section}>
                <Text className={styles.sectionTitle}>Queued</Text>
                {queuedActivities
                  .filter((a) => !a.batchId)
                  .map((activity) => (
                    <OperationProgress
                      key={activity.id}
                      activity={activity}
                      onPause={onPause}
                      onResume={onResume}
                      onCancel={onCancel}
                      onRetry={() => onRetry?.(activity)}
                      onPriorityChange={onPriorityChange}
                    />
                  ))}
              </div>
            )}

            {/* Recently Completed */}
            {completedActivities.length > 0 && (
              <div className={styles.section}>
                <Text className={styles.sectionTitle}>Recently Completed</Text>
                {completedActivities.slice(0, 5).map((activity) => (
                  <OperationProgress
                    key={activity.id}
                    activity={activity}
                    onPause={onPause}
                    onResume={onResume}
                    onCancel={onCancel}
                    onRetry={() => onRetry?.(activity)}
                    onPriorityChange={onPriorityChange}
                    compact
                  />
                ))}
              </div>
            )}

            {unbatchedActivities.length === 0 &&
              batchIds.length === 0 &&
              completedActivities.length === 0 && (
                <div className={styles.emptyState}>
                  <Text>No active operations</Text>
                </div>
              )}
          </div>
        )}

        {selectedTab === 'history' && (
          <OperationHistory
            history={recentHistory}
            onRetry={onRetry}
            onClearHistory={onClearHistory}
            onRemoveItem={onRemoveFromHistory}
          />
        )}

        {selectedTab === 'resources' && <ResourceMonitor />}
      </div>
    </div>
  );
}
