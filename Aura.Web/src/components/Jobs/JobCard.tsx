/**
 * JobCard Component
 * Displays a single job in the job queue with progress and status information
 */

import {
  Card,
  CardHeader,
  Text,
  ProgressBar,
  Badge,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import {
  PlayCircle24Regular,
  PauseCircle24Regular,
  CheckmarkCircle24Regular,
  ErrorCircle24Regular,
  DismissCircle24Regular,
  Clock24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  jobCard: {
    marginBottom: '12px',
    cursor: 'context-menu',
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  jobHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  jobTitle: {
    fontWeight: 600,
  },
  jobStatus: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
  },
  progressSection: {
    marginTop: '8px',
  },
  jobMeta: {
    marginTop: '8px',
    fontSize: '12px',
    color: tokens.colorNeutralForeground3,
  },
  statusIcon: {
    display: 'flex',
    alignItems: 'center',
  },
});

export interface Job {
  id: string;
  topic: string;
  status: 'queued' | 'running' | 'paused' | 'completed' | 'failed' | 'canceled';
  progress: number;
  stage: string;
  createdAt: string;
  outputPath?: string;
}

interface JobCardProps {
  job: Job;
  onContextMenu: (e: React.MouseEvent, job: Job) => void;
}

export function JobCard({ job, onContextMenu }: JobCardProps) {
  const styles = useStyles();

  const getStatusColor = (
    status: string
  ): 'success' | 'warning' | 'danger' | 'subtle' | 'informative' => {
    switch (status) {
      case 'running':
        return 'success';
      case 'paused':
        return 'warning';
      case 'completed':
        return 'success';
      case 'failed':
        return 'danger';
      case 'canceled':
        return 'subtle';
      default:
        return 'informative';
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'running':
        return (
          <PlayCircle24Regular
            aria-label="Running"
            style={{ color: tokens.colorPaletteGreenForeground1 }}
          />
        );
      case 'paused':
        return (
          <PauseCircle24Regular
            aria-label="Paused"
            style={{ color: tokens.colorPaletteYellowForeground1 }}
          />
        );
      case 'completed':
        return (
          <CheckmarkCircle24Regular
            aria-label="Completed"
            style={{ color: tokens.colorPaletteGreenForeground1 }}
          />
        );
      case 'failed':
        return (
          <ErrorCircle24Regular
            aria-label="Failed"
            style={{ color: tokens.colorPaletteRedForeground1 }}
          />
        );
      case 'canceled':
        return (
          <DismissCircle24Regular
            aria-label="Canceled"
            style={{ color: tokens.colorNeutralForeground3 }}
          />
        );
      default:
        return (
          <Clock24Regular aria-label="Queued" style={{ color: tokens.colorNeutralForeground3 }} />
        );
    }
  };

  return (
    <Card className={styles.jobCard} onContextMenu={(e) => onContextMenu(e, job)}>
      <CardHeader
        header={
          <div className={styles.jobHeader}>
            <Text className={styles.jobTitle}>{job.topic}</Text>
            <div className={styles.jobStatus}>
              <span className={styles.statusIcon}>{getStatusIcon(job.status)}</span>
              <Badge color={getStatusColor(job.status)}>{job.status.toUpperCase()}</Badge>
            </div>
          </div>
        }
      />
      {job.status === 'running' && (
        <div className={styles.progressSection}>
          <Text size={200}>
            {job.stage} - {job.progress}%
          </Text>
          <ProgressBar value={job.progress / 100} />
        </div>
      )}
      <div className={styles.jobMeta}>
        <Text>Started: {new Date(job.createdAt).toLocaleString()}</Text>
        {job.outputPath && <Text> • Output: {job.outputPath}</Text>}
      </div>
    </Card>
  );
}
