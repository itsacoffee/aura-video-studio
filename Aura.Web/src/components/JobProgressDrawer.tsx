import {
  Drawer,
  DrawerHeader,
  DrawerBody,
  Button,
  Text,
  ProgressBar,
  makeStyles,
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
} from '@fluentui/react-components';
import { Dismiss24Regular, Stop20Regular } from '@fluentui/react-icons';
import { useState, useEffect, useCallback } from 'react';
import { apiUrl } from '../config/api';
import { useSSEConnection } from '../hooks/useSSEConnection';
import { loggingService } from '../services/loggingService';

const useStyles = makeStyles({
  drawer: {
    width: '420px',
  },
  progressSection: {
    marginBottom: '20px',
  },
  progressText: {
    display: 'flex',
    justifyContent: 'space-between',
    marginTop: '8px',
    marginBottom: '4px',
  },
  stageText: {
    marginBottom: '8px',
    fontWeight: '600',
  },
  timeText: {
    display: 'flex',
    justifyContent: 'space-between',
    marginTop: '8px',
    fontSize: '12px',
    color: '#666',
  },
  logEntry: {
    padding: '8px',
    borderBottom: '1px solid #e0e0e0',
    fontSize: '12px',
    fontFamily: 'monospace',
    wordBreak: 'break-word',
  },
  logContainer: {
    maxHeight: '400px',
    overflowY: 'auto',
    marginTop: '8px',
  },
  actionButtons: {
    display: 'flex',
    gap: '8px',
    marginTop: '16px',
  },
});

export interface JobProgressDrawerProps {
  isOpen: boolean;
  onClose: () => void;
  jobId: string;
}

export function JobProgressDrawer({ isOpen, onClose, jobId }: JobProgressDrawerProps) {
  const styles = useStyles();
  const [progress, setProgress] = useState(0);
  const [status, setStatus] = useState('Running');
  const [stage, setStage] = useState('Initializing');
  const [logs, setLogs] = useState<string[]>([]);
  const [eta, setEta] = useState<string | null>(null);
  const [elapsed, setElapsed] = useState<string>('');
  const [isCancelling, setIsCancelling] = useState(false);
  const [showCancelDialog, setShowCancelDialog] = useState(false);

  // SSE connection for real-time updates
  const { connect, disconnect } = useSSEConnection({
    onMessage: (message) => {
      loggingService.debug('SSE message received', 'JobProgressDrawer', 'onMessage', {
        type: message.type,
      });

      switch (message.type) {
        case 'job-status': {
          const data = message.data as { status: string; stage: string; percent: number };
          setProgress(data.percent);
          setStatus(data.status.toLowerCase());
          setStage(data.stage);
          break;
        }

        case 'step-progress': {
          const data = message.data as {
            step: string;
            progressPct: number;
            message: string;
            elapsedTime?: string;
            estimatedTimeRemaining?: string;
          };
          setProgress(data.progressPct);
          setStage(data.step);
          if (data.message) {
            setLogs((prev) => [...prev.slice(-49), data.message]);
          }
          if (data.elapsedTime) {
            setElapsed(data.elapsedTime);
          }
          if (data.estimatedTimeRemaining) {
            setEta(data.estimatedTimeRemaining);
          }
          break;
        }

        case 'job-completed': {
          setStatus('completed');
          setProgress(100);
          disconnect();
          break;
        }

        case 'job-failed': {
          const data = message.data as { errorMessage?: string; logs?: string[] };
          setStatus('failed');
          if (data.errorMessage) {
            setLogs((prev) => [...prev, `ERROR: ${data.errorMessage}`]);
          }
          if (data.logs && Array.isArray(data.logs)) {
            setLogs((prev) => [...prev, ...data.logs]);
          }
          disconnect();
          break;
        }

        case 'job-cancelled': {
          setStatus('cancelled');
          disconnect();
          break;
        }

        case 'warning': {
          const data = message.data as { message: string };
          setLogs((prev) => [...prev.slice(-49), `WARNING: ${data.message}`]);
          break;
        }

        case 'error': {
          const data = message.data as { message: string };
          loggingService.error(
            'SSE error event',
            new Error(data.message),
            'JobProgressDrawer',
            'error'
          );
          break;
        }
      }
    },
    onError: (error) => {
      loggingService.error('SSE connection error', error, 'JobProgressDrawer', 'onError');
    },
  });

  useEffect(() => {
    if (!isOpen || !jobId) {
      return;
    }

    loggingService.info('Connecting to SSE for job progress', 'JobProgressDrawer', 'useEffect', {
      jobId,
    });
    connect(`/api/jobs/${jobId}/events`);

    return () => {
      disconnect();
    };
  }, [isOpen, jobId, connect, disconnect]);

  const handleCancelJob = useCallback(async () => {
    setIsCancelling(true);
    setShowCancelDialog(false);

    try {
      const response = await fetch(apiUrl(`/api/jobs/${jobId}/cancel`), {
        method: 'POST',
      });

      if (response.ok) {
        setStatus('cancelled');
        loggingService.info('Job cancelled successfully', 'JobProgressDrawer', 'handleCancelJob');
      } else {
        loggingService.error(
          'Failed to cancel job',
          new Error('API returned non-OK status'),
          'JobProgressDrawer',
          'handleCancelJob'
        );
      }
    } catch (error) {
      loggingService.error(
        'Error cancelling job',
        error instanceof Error ? error : new Error(String(error)),
        'JobProgressDrawer',
        'handleCancelJob'
      );
    } finally {
      setIsCancelling(false);
    }
  }, [jobId]);

  const isRunning = status === 'running';
  const canCancel = isRunning && !isCancelling;

  return (
    <Drawer open={isOpen} position="end" className={styles.drawer}>
      <DrawerHeader>
        <Text weight="semibold">Job Progress</Text>
        <Button appearance="subtle" icon={<Dismiss24Regular />} onClick={onClose} />
      </DrawerHeader>
      <DrawerBody>
        <div className={styles.progressSection}>
          <div className={styles.stageText}>
            <Text>Stage: {stage}</Text>
          </div>
          <ProgressBar value={progress / 100} />
          <div className={styles.progressText}>
            <Text>{progress}%</Text>
            <Text weight="semibold">{status}</Text>
          </div>
          <div className={styles.timeText}>
            <Text>Elapsed: {elapsed || '-'}</Text>
            {eta && <Text>ETA: {eta}</Text>}
          </div>
        </div>

        {canCancel && (
          <div className={styles.actionButtons}>
            <Dialog
              open={showCancelDialog}
              onOpenChange={(_, data) => setShowCancelDialog(data.open)}
            >
              <DialogTrigger disableButtonEnhancement>
                <Button appearance="outline" icon={<Stop20Regular />}>
                  Cancel Job
                </Button>
              </DialogTrigger>
              <DialogSurface>
                <DialogBody>
                  <DialogTitle>Cancel Job?</DialogTitle>
                  <DialogContent>
                    <Text>
                      Are you sure you want to cancel this job? This will stop the video generation
                      and clean up temporary files. This action cannot be undone.
                    </Text>
                  </DialogContent>
                  <DialogActions>
                    <DialogTrigger disableButtonEnhancement>
                      <Button appearance="secondary">No, keep running</Button>
                    </DialogTrigger>
                    <Button appearance="primary" onClick={handleCancelJob}>
                      Yes, cancel job
                    </Button>
                  </DialogActions>
                </DialogBody>
              </DialogSurface>
            </Dialog>
          </div>
        )}

        {isCancelling && <Text>Cancelling job...</Text>}

        <Text weight="semibold">Logs:</Text>
        <div className={styles.logContainer}>
          {logs.length === 0 ? (
            <Text>No logs available yet...</Text>
          ) : (
            logs.map((log, index) => (
              <div key={index} className={styles.logEntry}>
                {log}
              </div>
            ))
          )}
        </div>
      </DrawerBody>
    </Drawer>
  );
}
