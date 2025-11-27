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

  const getStatusIcon = (status: string): string => {
    switch (status) {
      case 'running':
        return '▶️';
      case 'paused':
        return '⏸️';
      case 'completed':
        return '✅';
      case 'failed':
        return '❌';
      case 'canceled':
        return '🚫';
      default:
        return '⏳';
    }
  };

  return (
    <Card className={styles.jobCard} onContextMenu={(e) => onContextMenu(e, job)}>
      <CardHeader
        header={
          <div className={styles.jobHeader}>
            <Text className={styles.jobTitle}>{job.topic}</Text>
            <div className={styles.jobStatus}>
              <span>{getStatusIcon(job.status)}</span>
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
