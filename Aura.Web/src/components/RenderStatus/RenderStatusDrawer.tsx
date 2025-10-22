import { useState, useEffect } from 'react';
import {
  Drawer,
  DrawerHeader,
  DrawerBody,
  makeStyles,
  tokens,
  Text,
  Badge,
  Button,
  ProgressBar,
  Spinner,
  Accordion,
  AccordionHeader,
  AccordionItem,
  AccordionPanel,
} from '@fluentui/react-components';
import {
  Dismiss24Regular,
  CheckmarkCircle24Filled,
  ErrorCircle24Filled,
  Warning24Filled,
  Folder24Regular,
  Settings24Regular,
  ArrowClockwise24Regular,
  Copy24Regular,
} from '@fluentui/react-icons';
import {
  JobResponse,
  JobStep,
  JobEvent,
  subscribeToJobEvents,
  getJob,
  cancelJob,
  retryJob,
} from '../../features/render/api/jobs';

const useStyles = makeStyles({
  drawer: {
    width: '420px',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingBottom: tokens.spacingVerticalM,
  },
  stepsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  stepItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    borderRadius: tokens.borderRadiusMedium,
    backgroundColor: tokens.colorNeutralBackground1,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  stepHeader: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  stepInfo: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  errorCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteRedBackground1,
    border: `1px solid ${tokens.colorPaletteRedBorder1}`,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalM,
  },
  errorActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalM,
    flexWrap: 'wrap',
  },
  successCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteGreenBackground1,
    border: `1px solid ${tokens.colorPaletteGreenBorder1}`,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalM,
  },
  successInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalS,
  },
  techDetails: {
    maxHeight: '200px',
    overflow: 'auto',
    backgroundColor: tokens.colorNeutralBackground3,
    padding: tokens.spacingVerticalS,
    borderRadius: tokens.borderRadiusSmall,
    fontFamily: 'monospace',
    fontSize: '12px',
    marginTop: tokens.spacingVerticalS,
  },
});

interface RenderStatusDrawerProps {
  jobId: string | null;
  isOpen: boolean;
  onClose: () => void;
}

export function RenderStatusDrawer({ jobId, isOpen, onClose }: RenderStatusDrawerProps) {
  const styles = useStyles();
  const [job, setJob] = useState<JobResponse | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!jobId || !isOpen) return;

    setLoading(true);

    // Load initial job state
    getJob(jobId)
      .then(setJob)
      .catch(console.error)
      .finally(() => setLoading(false));

    // Subscribe to events
    const unsubscribe = subscribeToJobEvents(
      jobId,
      (event: JobEvent) => {
        console.log('Job event:', event);

        // Update job state based on events
        setJob((prev) => {
          if (!prev) return prev;

          const updated = { ...prev };

          switch (event.type) {
            case 'job-status':
              updated.status = event.data.status;
              break;

            case 'step-status':
              updated.steps = updated.steps.map((step) =>
                step.name === event.data.step ? { ...step, status: event.data.status } : step
              );
              break;

            case 'step-progress':
              updated.steps = updated.steps.map((step) =>
                step.name === event.data.step
                  ? { ...step, progressPct: event.data.progressPct }
                  : step
              );
              break;

            case 'step-error':
              updated.steps = updated.steps.map((step) =>
                step.name === event.data.step
                  ? { ...step, errors: [...step.errors, event.data] }
                  : step
              );
              break;

            case 'job-completed':
              updated.status = 'Succeeded';
              updated.output = event.data.output;
              updated.endedUtc = new Date().toISOString();
              break;

            case 'job-failed':
              updated.status = 'Failed';
              updated.errors = event.data.errors || [];
              updated.endedUtc = new Date().toISOString();
              break;
          }

          return updated;
        });
      },
      (error) => {
        console.error('SSE error:', error);
      }
    );

    return () => {
      unsubscribe();
    };
  }, [jobId, isOpen]);

  const handleCancel = async () => {
    if (!jobId) return;
    try {
      await cancelJob(jobId);
      // Refresh job state
      const updated = await getJob(jobId);
      setJob(updated);
    } catch (error) {
      console.error('Failed to cancel job:', error);
    }
  };

  const handleRetry = async () => {
    if (!jobId) return;
    try {
      const response = await retryJob(jobId);
      onClose();
      // Retry creates a new job - navigate to it
      if (response?.jobId) {
        window.location.href = `/generate?jobId=${response.jobId}`;
      }
    } catch (error) {
      console.error('Failed to retry job:', error);
    }
  };

  const handleCopyTechDetails = (content: string) => {
    navigator.clipboard.writeText(content);
  };

  const handleOpenSettings = () => {
    // Deep-link to settings page
    window.location.href = '/settings?tab=providers';
  };

  const handleOpenSystemCheck = () => {
    // Navigate to health check page
    window.location.href = '/health';
  };

  const handleOpenFolder = () => {
    if (job?.output?.videoPath) {
      // Open folder in file explorer
      window.open(`file://${job.output.videoPath}`, '_blank');
    }
  };

  const renderStep = (step: JobStep) => {
    const statusIcon = () => {
      switch (step.status) {
        case 'Succeeded':
          return <CheckmarkCircle24Filled style={{ color: tokens.colorPaletteGreenForeground1 }} />;
        case 'Failed':
          return <ErrorCircle24Filled style={{ color: tokens.colorPaletteRedForeground1 }} />;
        case 'Running':
          return <Spinner size="tiny" />;
        case 'Skipped':
          return <Warning24Filled style={{ color: tokens.colorPaletteYellowForeground1 }} />;
        default:
          return null;
      }
    };

    const statusBadge = () => {
      const appearance = {
        Pending: 'outline',
        Running: 'tint',
        Succeeded: 'filled',
        Failed: 'filled',
        Skipped: 'outline',
        Canceled: 'outline',
      }[step.status] as 'outline' | 'tint' | 'filled';

      const color = {
        Pending: 'informative',
        Running: 'informative',
        Succeeded: 'success',
        Failed: 'danger',
        Skipped: 'warning',
        Canceled: 'subtle',
      }[step.status] as 'success' | 'danger' | 'warning' | 'informative' | 'subtle';

      return (
        <Badge appearance={appearance} color={color}>
          {step.status}
        </Badge>
      );
    };

    return (
      <div key={step.name} className={styles.stepItem}>
        <div className={styles.stepHeader}>
          <div className={styles.stepInfo}>
            {statusIcon()}
            <Text weight="semibold">{step.name}</Text>
          </div>
          {statusBadge()}
        </div>

        {step.status === 'Running' && step.progressPct > 0 && (
          <ProgressBar value={step.progressPct / 100} />
        )}

        {step.errors.length > 0 && (
          <div className={styles.errorCard}>
            {step.errors.map((error, idx) => (
              <div key={idx}>
                <Text weight="semibold">{error.message}</Text>
                <Text size={200} style={{ display: 'block', marginTop: tokens.spacingVerticalXS }}>
                  {error.remediation}
                </Text>

                <div className={styles.errorActions}>
                  {error.code.startsWith('MissingApiKey:') && (
                    <Button size="small" icon={<Settings24Regular />} onClick={handleOpenSettings}>
                      Open Settings
                    </Button>
                  )}
                  <Button size="small" icon={<Settings24Regular />} onClick={handleOpenSystemCheck}>
                    System Check
                  </Button>
                  <Button size="small" icon={<ArrowClockwise24Regular />} onClick={handleRetry}>
                    Retry
                  </Button>
                </div>

                <Accordion collapsible>
                  <AccordionItem value={`error-${idx}`}>
                    <AccordionHeader>Technical Details</AccordionHeader>
                    <AccordionPanel>
                      <div className={styles.techDetails}>
                        <pre>{JSON.stringify(error.details, null, 2)}</pre>
                      </div>
                      <Button
                        size="small"
                        icon={<Copy24Regular />}
                        onClick={() =>
                          handleCopyTechDetails(JSON.stringify(error.details, null, 2))
                        }
                        style={{ marginTop: tokens.spacingVerticalS }}
                      >
                        Copy
                      </Button>
                    </AccordionPanel>
                  </AccordionItem>
                </Accordion>
              </div>
            ))}
          </div>
        )}
      </div>
    );
  };

  return (
    <Drawer
      type="overlay"
      position="end"
      open={isOpen}
      onOpenChange={(_, { open }) => !open && onClose()}
      className={styles.drawer}
    >
      <DrawerHeader>
        <div className={styles.header}>
          <div>
            <Text size={500} weight="semibold">
              {job?.status === 'Succeeded' ? 'Render Complete' : 'Rendering...'}
            </Text>
            {job?.correlationId && (
              <Text size={200} style={{ display: 'block', color: tokens.colorNeutralForeground3 }}>
                Job {job.correlationId.substring(0, 8)}
              </Text>
            )}
          </div>
          <Button appearance="subtle" icon={<Dismiss24Regular />} onClick={onClose} />
        </div>
      </DrawerHeader>

      <DrawerBody>
        {loading && <Spinner label="Loading job status..." />}

        {job && (
          <div className={styles.stepsList}>
            {job.steps.map(renderStep)}

            {job.status === 'Succeeded' && job.output && (
              <div className={styles.successCard}>
                <div
                  style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}
                >
                  <CheckmarkCircle24Filled style={{ color: tokens.colorPaletteGreenForeground1 }} />
                  <Text weight="semibold">Video rendered successfully!</Text>
                </div>
                <div className={styles.successInfo}>
                  <Text size={200}>Size: {(job.output.sizeBytes / 1024 / 1024).toFixed(2)} MB</Text>
                  {job.startedUtc && job.endedUtc && (
                    <Text size={200}>
                      Duration:{' '}
                      {Math.round(
                        (new Date(job.endedUtc).getTime() - new Date(job.startedUtc).getTime()) /
                          1000
                      )}
                      s
                    </Text>
                  )}
                  <Button
                    appearance="primary"
                    icon={<Folder24Regular />}
                    onClick={handleOpenFolder}
                    style={{ marginTop: tokens.spacingVerticalS }}
                  >
                    Open Folder
                  </Button>
                </div>
              </div>
            )}

            {job.status === 'Failed' && job.errors.length > 0 && (
              <div className={styles.errorCard}>
                <div
                  style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}
                >
                  <ErrorCircle24Filled style={{ color: tokens.colorPaletteRedForeground1 }} />
                  <Text weight="semibold">Render failed</Text>
                </div>
                {job.errors.map((error, idx) => (
                  <div key={idx} style={{ marginTop: tokens.spacingVerticalS }}>
                    <Text>{error.message}</Text>
                    <Text
                      size={200}
                      style={{ display: 'block', marginTop: tokens.spacingVerticalXS }}
                    >
                      {error.remediation}
                    </Text>
                  </div>
                ))}
                <div className={styles.errorActions}>
                  <Button
                    appearance="primary"
                    icon={<ArrowClockwise24Regular />}
                    onClick={handleRetry}
                  >
                    Retry Job
                  </Button>
                </div>
              </div>
            )}

            {job.status === 'Running' && <Button onClick={handleCancel}>Cancel</Button>}
          </div>
        )}
      </DrawerBody>
    </Drawer>
  );
}
