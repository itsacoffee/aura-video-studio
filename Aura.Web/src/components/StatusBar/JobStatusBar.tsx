import {
  makeStyles,
  Spinner,
  Text,
  Button,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Regular,
  ErrorCircle24Regular,
  Info24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  statusBar: {
    display: 'flex',
    justifyContent: 'space-between',
    padding: '12px 20px',
    backgroundColor: '#f5f5f5',
    borderBottom: '1px solid #e0e0e0',
    minHeight: '60px',
    alignItems: 'center',
  },
  statusBarRunning: {
    backgroundColor: '#e3f2fd',
  },
  statusBarCompleted: {
    backgroundColor: '#e8f5e9',
  },
  statusBarFailed: {
    backgroundColor: '#ffebee',
  },
  leftSection: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
  },
  progressText: {
    minWidth: '50px',
    fontWeight: '600',
  },
  messageText: {
    flex: 1,
  },
});

export interface JobStatusBarProps {
  jobId?: string;
  status: 'idle' | 'running' | 'completed' | 'failed';
  progress: number;
  message: string;
  onViewDetails?: () => void;
}

export function JobStatusBar({ 
  status, 
  progress, 
  message, 
  onViewDetails 
}: JobStatusBarProps) {
  const styles = useStyles();

  if (status === 'idle') {
    return null;
  }

  const getIcon = () => {
    switch (status) {
      case 'running':
        return <Spinner size="small" />;
      case 'completed':
        return <CheckmarkCircle24Regular style={{ color: '#2e7d32' }} />;
      case 'failed':
        return <ErrorCircle24Regular style={{ color: '#c62828' }} />;
      default:
        return <Info24Regular />;
    }
  };

  const getBackgroundClass = () => {
    switch (status) {
      case 'running':
        return styles.statusBarRunning;
      case 'completed':
        return styles.statusBarCompleted;
      case 'failed':
        return styles.statusBarFailed;
      default:
        return '';
    }
  };

  return (
    <div className={`${styles.statusBar} ${getBackgroundClass()}`}>
      <div className={styles.leftSection}>
        {getIcon()}
        {status === 'running' && (
          <Text className={styles.progressText}>{progress}%</Text>
        )}
        <Text className={styles.messageText}>{message}</Text>
      </div>
      {onViewDetails && (
        <Button appearance="subtle" onClick={onViewDetails}>
          View Details
        </Button>
      )}
    </div>
  );
}
