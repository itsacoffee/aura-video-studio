/**
 * JobQueue Component
 * Displays and manages the job queue with context menu support
 */

import { Text, Button, makeStyles, tokens } from '@fluentui/react-components';
import { useCallback, useState, useMemo } from 'react';
import { env } from '../../config/env';
import { useJobQueue } from '../../hooks/useJobQueue';
import { useJobQueueContextMenu } from '../../hooks/useJobQueueContextMenu';
import { JobCard, type Job } from './JobCard';
import { JobLogsModal } from './JobLogsModal';

const useStyles = makeStyles({
  jobQueue: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    padding: tokens.spacingVerticalM,
  },
  queueHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalL,
  },
  jobsList: {
    flex: 1,
    overflowY: 'auto',
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXL,
    color: tokens.colorNeutralForeground3,
    textAlign: 'center',
  },
});

export function JobQueue() {
  const styles = useStyles();
  const [selectedJobLogs, setSelectedJobLogs] = useState<string | null>(null);
  const [showLogsModal, setShowLogsModal] = useState(false);

  const { jobs: queueJobs, cancelJob, loadJobs, clearCompletedJobs } = useJobQueue();

  // Convert queue jobs to the Job format expected by JobCard
  const jobs: Job[] = useMemo(() => {
    return queueJobs.map((qj) => ({
      id: qj.jobId,
      topic: qj.correlationId || 'Video Job',
      status: mapQueueStatus(qj.status),
      progress: qj.progress,
      stage: qj.currentStage || 'Processing',
      createdAt: qj.enqueuedAt,
      outputPath: qj.outputPath || undefined,
    }));
  }, [queueJobs]);

  const handlePauseJob = useCallback(
    async (jobId: string) => {
      try {
        const response = await fetch(`${env.apiBaseUrl}/api/jobs/${jobId}/pause`, {
          method: 'POST',
        });
        if (response.ok) {
          await loadJobs();
        } else {
          console.error('Failed to pause job:', await response.text());
        }
      } catch (error: unknown) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        console.error('Failed to pause job:', errorMessage);
      }
    },
    [loadJobs]
  );

  const handleResumeJob = useCallback(
    async (jobId: string) => {
      try {
        const response = await fetch(`${env.apiBaseUrl}/api/jobs/${jobId}/resume`, {
          method: 'POST',
        });
        if (response.ok) {
          await loadJobs();
        } else {
          console.error('Failed to resume job:', await response.text());
        }
      } catch (error: unknown) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        console.error('Failed to resume job:', errorMessage);
      }
    },
    [loadJobs]
  );

  const handleCancelJob = useCallback(
    async (jobId: string) => {
      try {
        await cancelJob(jobId);
      } catch (error: unknown) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        console.error('Failed to cancel job:', errorMessage);
      }
    },
    [cancelJob]
  );

  const handleViewLogs = useCallback(async (jobId: string) => {
    try {
      const response = await fetch(`${env.apiBaseUrl}/api/jobs/${jobId}/logs`);
      if (response.ok) {
        const logs = await response.text();
        setSelectedJobLogs(logs);
        setShowLogsModal(true);
      } else {
        console.error('Failed to fetch job logs:', await response.text());
      }
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : String(error);
      console.error('Failed to fetch job logs:', errorMessage);
    }
  }, []);

  const handleRetryJob = useCallback(
    async (jobId: string) => {
      try {
        const response = await fetch(`${env.apiBaseUrl}/api/jobs/${jobId}/retry`, {
          method: 'POST',
        });
        if (response.ok) {
          await loadJobs();
        } else {
          console.error('Failed to retry job:', await response.text());
        }
      } catch (error: unknown) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        console.error('Failed to retry job:', errorMessage);
      }
    },
    [loadJobs]
  );

  const handleClearCompleted = useCallback(() => {
    clearCompletedJobs();
  }, [clearCompletedJobs]);

  const handleCloseLogsModal = useCallback(() => {
    setShowLogsModal(false);
    setSelectedJobLogs(null);
  }, []);

  const handleJobContextMenu = useJobQueueContextMenu({
    onPause: handlePauseJob,
    onResume: handleResumeJob,
    onCancel: handleCancelJob,
    onViewLogs: handleViewLogs,
    onRetry: handleRetryJob,
  });

  return (
    <div className={styles.jobQueue}>
      <div className={styles.queueHeader}>
        <Text size={600}>Job Queue</Text>
        <Button onClick={handleClearCompleted}>Clear Completed</Button>
      </div>
      <div className={styles.jobsList}>
        {jobs.length === 0 ? (
          <div className={styles.emptyState}>
            <Text size={400}>No jobs in queue</Text>
            <Text>Start a video generation to see jobs here</Text>
          </div>
        ) : (
          jobs.map((job) => <JobCard key={job.id} job={job} onContextMenu={handleJobContextMenu} />)
        )}
      </div>
      {showLogsModal && <JobLogsModal logs={selectedJobLogs} onClose={handleCloseLogsModal} />}
    </div>
  );
}

/**
 * Maps queue job status to the status format expected by JobCard
 */
function mapQueueStatus(status: string): Job['status'] {
  switch (status.toLowerCase()) {
    case 'pending':
      return 'queued';
    case 'processing':
      return 'running';
    case 'completed':
      return 'completed';
    case 'failed':
      return 'failed';
    case 'cancelled':
    case 'canceled':
      return 'canceled';
    case 'paused':
      return 'paused';
    default:
      return 'queued';
  }
}
