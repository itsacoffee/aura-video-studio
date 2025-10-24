import { makeStyles, Spinner, Text, Button, tokens } from '@fluentui/react-components';
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
    backgroundColor: tokens.colorNeutralBackground2,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    minHeight: '60px',
    alignItems: 'center',
    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    boxShadow: '0 2px 4px rgba(0, 0, 0, 0.08)',
  },
  statusBarRunning: {
    backgroundColor: tokens.colorNeutralBackground3,
    borderBottom: `2px solid ${tokens.colorBrandBackground}`,
    animation: 'pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite',
  },
  statusBarCompleted: {
    backgroundColor: tokens.colorNeutralBackground3,
    borderBottom: `2px solid ${tokens.colorPaletteGreenBorder2}`,
  },
  statusBarFailed: {
    backgroundColor: tokens.colorNeutralBackground3,
    borderBottom: `2px solid ${tokens.colorPaletteRedForeground1}`,
  },
  leftSection: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
    flex: 1,
  },
  progressText: {
    minWidth: '50px',
    fontWeight: tokens.fontWeightSemibold,
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase300,
  },
  messageText: {
    flex: 1,
    fontWeight: tokens.fontWeightRegular,
  },
  progressBarContainer: {
    position: 'absolute',
    bottom: 0,
    left: 0,
    right: 0,
    height: '3px',
    backgroundColor: 'rgba(0, 0, 0, 0.1)',
  },
  progressBarFill: {
    height: '100%',
    background: `linear-gradient(90deg, ${tokens.colorBrandBackground}, ${tokens.colorPalettePurpleBackground2})`,
    transition: 'width 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    boxShadow: `0 0 8px ${tokens.colorBrandBackground}`,
  },
});

export interface JobStatusBarProps {
  jobId?: string;
  status: 'idle' | 'running' | 'completed' | 'failed';
  progress: number;
  message: string;
  onViewDetails?: () => void;
}

export function JobStatusBar({ status, progress, message, onViewDetails }: JobStatusBarProps) {
  const styles = useStyles();

  if (status === 'idle') {
    return null;
  }

  const getIcon = () => {
    switch (status) {
      case 'running':
        return <Spinner size="small" />;
      case 'completed':
        return <CheckmarkCircle24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />;
      case 'failed':
        return <ErrorCircle24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />;
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
    <div className={`${styles.statusBar} ${getBackgroundClass()}`} style={{ position: 'relative' }}>
      <div className={styles.leftSection}>
        {getIcon()}
        {status === 'running' && <Text className={styles.progressText}>{progress}%</Text>}
        <Text className={styles.messageText}>{message}</Text>
      </div>
      {onViewDetails && (
        <Button appearance="subtle" onClick={onViewDetails}>
          View Details
        </Button>
      )}
      {status === 'running' && (
        <div className={styles.progressBarContainer}>
          <div className={styles.progressBarFill} style={{ width: `${progress}%` }} />
        </div>
      )}
    </div>
  );
}
