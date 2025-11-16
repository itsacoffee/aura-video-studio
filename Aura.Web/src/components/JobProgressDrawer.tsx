import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  DialogTrigger,
  Drawer,
  DrawerBody,
  DrawerHeader,
  ProgressBar,
  Text,
  makeStyles,
} from '@fluentui/react-components';
import { Dismiss24Regular, Stop20Regular } from '@fluentui/react-icons';
import { useCallback, useEffect, useState } from 'react';
import { apiUrl } from '../config/api';
import { useSSEConnection } from '../hooks/useSSEConnection';
import { loggingService } from '../services/loggingService';
import type { ProgressEventDto } from '../types/api-v1';

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

type DrawerLogEntry = {
  id: string;
  message: string;
  severity: 'info' | 'warning' | 'error';
  timestamp: string;
  stage?: string;
};

type DrawerProgressEventPayload = ProgressEventDto & {
  step?: string;
  progressPct?: number;
};

interface DrawerLogPayload {
  jobId: string;
  message: string;
  stage?: string;
  severity?: string;
  timestamp?: string;
}

const normalizeSeverity = (severity?: string): DrawerLogEntry['severity'] => {
  if (!severity) {
    return 'info';
  }
  const normalized = severity.toLowerCase();
  if (normalized.includes('error')) {
    return 'error';
  }
  if (normalized.includes('warn')) {
    return 'warning';
  }
  return 'info';
};

const formatDurationFromSeconds = (seconds?: number | null): string => {
  if (seconds === undefined || seconds === null || Number.isNaN(seconds)) {
    return '';
  }

  if (seconds < 60) {
    return `${Math.max(0, Math.round(seconds))}s`;
  }

  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = Math.round(seconds % 60);

  if (minutes < 60) {
    return `${minutes}m ${remainingSeconds}s`;
  }

  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;
  return `${hours}h ${remainingMinutes}m`;
};

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
  const [logs, setLogs] = useState<DrawerLogEntry[]>([]);
  const [eta, setEta] = useState<string | null>(null);
  const [elapsed, setElapsed] = useState<string>('');
  const [isCancelling, setIsCancelling] = useState(false);
  const [showCancelDialog, setShowCancelDialog] = useState(false);

  const appendLogEntry = useCallback((payload: DrawerLogPayload) => {
    if (!payload.message) {
      return;
    }

    setLogs((prev) => {
      const timestamp = payload.timestamp ?? new Date().toISOString();
      const entry: DrawerLogEntry = {
        id: `${timestamp}-${prev.length}`,
        message: payload.message,
        severity: normalizeSeverity(payload.severity),
        timestamp,
        stage: payload.stage,
      };

      const next = [...prev, entry];
      if (next.length > 200) {
        return next.slice(next.length - 200);
      }
      return next;
    });
  }, []);

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
          const data = message.data as DrawerProgressEventPayload;
          const nextPercent =
            typeof data.percent === 'number'
              ? data.percent
              : typeof data.progressPct === 'number'
                ? data.progressPct
                : progress;
          setProgress(nextPercent);
          if (data.stage || data.step) {
            setStage(data.stage ?? data.step ?? stage);
          }
          if (typeof data.elapsedSeconds === 'number') {
            setElapsed(formatDurationFromSeconds(data.elapsedSeconds));
          }
          const etaSeconds =
            typeof data.estimatedRemainingSeconds === 'number'
              ? data.estimatedRemainingSeconds
              : typeof data.etaSeconds === 'number'
                ? data.etaSeconds
                : undefined;
          if (etaSeconds !== undefined) {
            setEta(formatDurationFromSeconds(etaSeconds));
          }
          break;
        }

        case 'job-completed': {
          setStatus('completed');
          setProgress(100);
          appendLogEntry({
            jobId,
            message: 'Job completed successfully.',
            severity: 'info',
          });
          disconnect();
          break;
        }

        case 'job-failed': {
          const data = message.data as { errorMessage?: string; logs?: string[]; stage?: string };
          setStatus('failed');
          appendLogEntry({
            jobId,
            message: data.errorMessage || 'Job failed',
            severity: 'error',
            stage: data.stage,
          });
          if (Array.isArray(data.logs)) {
            data.logs.forEach((log) =>
              appendLogEntry({
                jobId,
                message: log,
                severity: 'error',
                stage: data.stage,
              })
            );
          }
          disconnect();
          break;
        }

        case 'job-cancelled': {
          setStatus('cancelled');
          appendLogEntry({
            jobId,
            message: 'Job was cancelled.',
            severity: 'warning',
          });
          disconnect();
          break;
        }

        case 'job-log': {
          const data = message.data as DrawerLogPayload;
          appendLogEntry(data);
          break;
        }

        case 'warning': {
          const data = message.data as { message: string };
          appendLogEntry({
            jobId,
            message: data.message,
            severity: 'warning',
          });
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
            logs.map((log) => (
              <div key={log.id} className={styles.logEntry}>
                <Text as="span" weight="semibold">
                  [{new Date(log.timestamp).toLocaleTimeString()}]
                </Text>{' '}
                <Text
                  as="span"
                  weight="semibold"
                  style={{
                    color:
                      log.severity === 'error'
                        ? '#a4262c'
                        : log.severity === 'warning'
                          ? '#bc4b09'
                          : '#605e5c',
                  }}
                >
                  {log.severity.toUpperCase()}
                </Text>{' '}
                {log.stage && (
                  <Text as="span" style={{ color: '#605e5c' }}>
                    ({log.stage})
                  </Text>
                )}{' '}
                <Text as="span">{log.message}</Text>
              </div>
            ))
          )}
        </div>
      </DrawerBody>
    </Drawer>
  );
}
