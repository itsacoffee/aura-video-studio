import {
  makeStyles,
  tokens,
  Text,
  Button,
  Badge,
  Tooltip,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Regular,
  ErrorCircle24Regular,
  DismissCircle24Regular,
  ArrowClockwise24Regular,
  MoreVertical24Regular,
  Open24Regular,
  Copy24Regular,
  Delete24Regular,
} from '@fluentui/react-icons';
import { useState } from 'react';
import { Activity } from '../../state/activityContext';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingBottom: tokens.spacingVerticalS,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  title: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase300,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
    color: tokens.colorNeutralForeground3,
  },
  historyList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    maxHeight: '400px',
    overflowY: 'auto',
  },
  historyItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusSmall,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  historyItemSuccess: {
    borderLeft: `3px solid ${tokens.colorPaletteGreenBorder2}`,
  },
  historyItemFailed: {
    borderLeft: `3px solid ${tokens.colorPaletteRedBorder2}`,
  },
  historyItemCancelled: {
    borderLeft: `3px solid ${tokens.colorNeutralStroke2}`,
  },
  icon: {
    flexShrink: 0,
  },
  content: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
    minWidth: 0,
  },
  itemTitle: {
    fontWeight: tokens.fontWeightSemibold,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  itemDetails: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  actions: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    flexShrink: 0,
  },
  filters: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalS,
  },
});

interface OperationHistoryProps {
  history: Activity[];
  onRetry?: (activity: Activity) => void;
  onViewDetails?: (activity: Activity) => void;
  onClearHistory?: () => void;
  onRemoveItem?: (id: string) => void;
}

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

export function OperationHistory({
  history,
  onRetry,
  onViewDetails,
  onClearHistory,
  onRemoveItem,
}: OperationHistoryProps) {
  const styles = useStyles();
  const [filter, setFilter] = useState<'all' | 'completed' | 'failed' | 'cancelled'>('all');

  const filteredHistory = history.filter((activity) => {
    if (filter === 'all') return true;
    return activity.status === filter;
  });

  const getIcon = (status: string) => {
    switch (status) {
      case 'completed':
        return (
          <CheckmarkCircle24Regular
            className={styles.icon}
            style={{ color: tokens.colorPaletteGreenForeground1 }}
          />
        );
      case 'failed':
        return (
          <ErrorCircle24Regular
            className={styles.icon}
            style={{ color: tokens.colorPaletteRedForeground1 }}
          />
        );
      case 'cancelled':
        return (
          <DismissCircle24Regular
            className={styles.icon}
            style={{ color: tokens.colorNeutralForeground3 }}
          />
        );
      default:
        return null;
    }
  };

  const getItemClass = (status: string) => {
    switch (status) {
      case 'completed':
        return styles.historyItemSuccess;
      case 'failed':
        return styles.historyItemFailed;
      case 'cancelled':
        return styles.historyItemCancelled;
      default:
        return '';
    }
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'completed':
        return (
          <Badge appearance="filled" color="success" size="small">
            Success
          </Badge>
        );
      case 'failed':
        return (
          <Badge appearance="filled" color="danger" size="small">
            Failed
          </Badge>
        );
      case 'cancelled':
        return (
          <Badge appearance="outline" size="small">
            Cancelled
          </Badge>
        );
      default:
        return null;
    }
  };

  const handleCopyDetails = (activity: Activity) => {
    const details = {
      id: activity.id,
      title: activity.title,
      status: activity.status,
      message: activity.message,
      startTime: activity.startTime.toISOString(),
      endTime: activity.endTime?.toISOString(),
      duration: formatDuration(activity.startTime, activity.endTime),
      error: activity.error,
      metadata: activity.metadata,
    };
    navigator.clipboard.writeText(JSON.stringify(details, null, 2));
  };

  const completedCount = history.filter((a) => a.status === 'completed').length;
  const failedCount = history.filter((a) => a.status === 'failed').length;
  const cancelledCount = history.filter((a) => a.status === 'cancelled').length;

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Text className={styles.title}>Operation History ({history.length}/50)</Text>
        {onClearHistory && history.length > 0 && (
          <Button appearance="subtle" size="small" onClick={onClearHistory}>
            Clear History
          </Button>
        )}
      </div>

      {history.length > 0 && (
        <div className={styles.filters}>
          <Button
            appearance={filter === 'all' ? 'primary' : 'subtle'}
            size="small"
            onClick={() => setFilter('all')}
          >
            All ({history.length})
          </Button>
          <Button
            appearance={filter === 'completed' ? 'primary' : 'subtle'}
            size="small"
            onClick={() => setFilter('completed')}
          >
            Completed ({completedCount})
          </Button>
          <Button
            appearance={filter === 'failed' ? 'primary' : 'subtle'}
            size="small"
            onClick={() => setFilter('failed')}
          >
            Failed ({failedCount})
          </Button>
          <Button
            appearance={filter === 'cancelled' ? 'primary' : 'subtle'}
            size="small"
            onClick={() => setFilter('cancelled')}
          >
            Cancelled ({cancelledCount})
          </Button>
        </div>
      )}

      {filteredHistory.length === 0 ? (
        <div className={styles.emptyState}>
          <Text>No operation history to display</Text>
        </div>
      ) : (
        <div className={styles.historyList}>
          {filteredHistory.map((activity) => (
            <div
              key={activity.id}
              className={`${styles.historyItem} ${getItemClass(activity.status)}`}
            >
              {getIcon(activity.status)}

              <div className={styles.content}>
                <Text className={styles.itemTitle}>{activity.title}</Text>
                <div className={styles.itemDetails}>
                  {getStatusBadge(activity.status)}
                  <Text>{activity.endTime?.toLocaleTimeString() || 'Unknown'}</Text>
                  <Text>Duration: {formatDuration(activity.startTime, activity.endTime)}</Text>
                </div>
              </div>

              <div className={styles.actions}>
                {activity.status === 'failed' && activity.canRetry && onRetry && (
                  <Tooltip content="Retry Operation" relationship="label">
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<ArrowClockwise24Regular />}
                      onClick={() => onRetry(activity)}
                    />
                  </Tooltip>
                )}
                {activity.status === 'completed' && activity.artifactPath && (
                  <Tooltip content="Open File" relationship="label">
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<Open24Regular />}
                      onClick={() => {
                        // In production, this would open the file
                        console.log('Open file:', activity.artifactPath);
                      }}
                    />
                  </Tooltip>
                )}
                <Menu>
                  <MenuTrigger disableButtonEnhancement>
                    <Button appearance="subtle" size="small" icon={<MoreVertical24Regular />} />
                  </MenuTrigger>
                  <MenuPopover>
                    <MenuList>
                      {onViewDetails && (
                        <MenuItem icon={<Open24Regular />} onClick={() => onViewDetails(activity)}>
                          View Details
                        </MenuItem>
                      )}
                      <MenuItem
                        icon={<Copy24Regular />}
                        onClick={() => handleCopyDetails(activity)}
                      >
                        Copy Details
                      </MenuItem>
                      {onRemoveItem && (
                        <MenuItem
                          icon={<Delete24Regular />}
                          onClick={() => onRemoveItem(activity.id)}
                        >
                          Remove
                        </MenuItem>
                      )}
                    </MenuList>
                  </MenuPopover>
                </Menu>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
