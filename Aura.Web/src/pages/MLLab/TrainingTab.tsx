import {
  makeStyles,
  tokens,
  Text,
  Button,
  Card,
  Spinner,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
  Input,
  Label,
  ProgressBar,
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Select,
} from '@fluentui/react-components';
import {
  Play24Regular,
  ArrowReset24Regular,
  Dismiss24Regular,
  Checkmark24Regular,
  Warning24Regular,
} from '@fluentui/react-icons';
import { useState, useCallback, useEffect, type FC } from 'react';
import { useMLLabStore } from '../../state/mlLab';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    maxWidth: '1200px',
    margin: '0 auto',
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  sectionTitle: {
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightSemibold,
  },
  configGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
  progressCard: {
    padding: tokens.spacingVerticalXL,
  },
  progressHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  progressDetails: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  metricsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))',
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalM,
  },
  metricItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  metricLabel: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  metricValue: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
  },
  historyList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  historyCard: {
    padding: tokens.spacingVerticalM,
  },
  historyCardContent: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  statusBadge: {
    display: 'inline-flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalS}`,
    borderRadius: tokens.borderRadiusSmall,
    fontSize: tokens.fontSizeBase200,
  },
  completedBadge: {
    backgroundColor: tokens.colorPaletteGreenBackground2,
    color: tokens.colorPaletteGreenForeground2,
  },
  failedBadge: {
    backgroundColor: tokens.colorPaletteRedBackground2,
    color: tokens.colorPaletteRedForeground2,
  },
  cancelledBadge: {
    backgroundColor: tokens.colorNeutralBackground3,
    color: tokens.colorNeutralForeground3,
  },
  actionButtons: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
  },
});

export const TrainingTab: FC = () => {
  const styles = useStyles();
  const {
    activeTrainingJob,
    trainingHistory,
    isStartingTraining,
    trainingConfig,
    annotationStats,
    systemCapabilities,
    error,
    updateTrainingConfig,
    startTraining,
    cancelTraining,
    revertToDefaultModel,
    loadAnnotationStats,
  } = useMLLabStore();

  const [showRevertDialog, setShowRevertDialog] = useState(false);
  const [isReverting, setIsReverting] = useState(false);

  useEffect(() => {
    loadAnnotationStats();
  }, [loadAnnotationStats]);

  const handleStartTraining = useCallback(async () => {
    try {
      await startTraining();
    } catch (error) {
      console.error('Failed to start training:', error);
    }
  }, [startTraining]);

  const handleCancelTraining = useCallback(async () => {
    if (!activeTrainingJob) return;
    try {
      await cancelTraining(activeTrainingJob.jobId);
    } catch (error) {
      console.error('Failed to cancel training:', error);
    }
  }, [activeTrainingJob, cancelTraining]);

  const handleRevertToDefault = useCallback(async () => {
    setIsReverting(true);
    try {
      await revertToDefaultModel();
      setShowRevertDialog(false);
    } catch (error) {
      console.error('Failed to revert to default model:', error);
    } finally {
      setIsReverting(false);
    }
  }, [revertToDefaultModel]);

  const canStartTraining =
    !activeTrainingJob &&
    !isStartingTraining &&
    annotationStats &&
    annotationStats.totalAnnotations >= 10;

  const hasInsufficientData = annotationStats && annotationStats.totalAnnotations < 10;

  return (
    <div className={styles.container}>
      {error && (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      )}

      {/* System Status */}
      {systemCapabilities && !systemCapabilities.meetsMinimumRequirements && (
        <MessageBar intent="warning">
          <MessageBarBody>
            <MessageBarTitle>System Requirements Warning</MessageBarTitle>
            Your system does not meet the minimum requirements for training. Training may be very
            slow or fail.
          </MessageBarBody>
        </MessageBar>
      )}

      {/* Data Status */}
      {hasInsufficientData && (
        <MessageBar intent="warning">
          <MessageBarBody>
            <MessageBarTitle>Insufficient Training Data</MessageBarTitle>
            You need at least 10 annotated frames to start training. Currently have:{' '}
            {annotationStats?.totalAnnotations || 0}. Go to the Annotation tab to add more
            annotations.
          </MessageBarBody>
        </MessageBar>
      )}

      {/* Training Configuration */}
      <div className={styles.section}>
        <Text className={styles.sectionTitle}>1. Configure Training</Text>
        <div className={styles.configGrid}>
          <div>
            <Label htmlFor="model-name">Model Name (Optional)</Label>
            <Input
              id="model-name"
              placeholder="my-custom-model"
              value={trainingConfig.modelName || ''}
              onChange={(_, data) => updateTrainingConfig({ modelName: data.value || undefined })}
              disabled={!!activeTrainingJob}
            />
          </div>
          <div>
            <Label htmlFor="epochs-preset">Training Duration</Label>
            <Select
              id="epochs-preset"
              value={trainingConfig.epochsPreset || 'balanced'}
              onChange={(_, data) =>
                updateTrainingConfig({
                  epochsPreset: data.value as 'quick' | 'balanced' | 'thorough',
                })
              }
              disabled={!!activeTrainingJob}
            >
              <option value="quick">Quick (5-10 minutes)</option>
              <option value="balanced">Balanced (15-30 minutes)</option>
              <option value="thorough">Thorough (30-60 minutes)</option>
            </Select>
          </div>
        </div>

        <MessageBar intent="info">
          <MessageBarBody>
            Training will use all {annotationStats?.totalAnnotations || 0} annotations from the
            backend. Longer training durations may produce better models but take more time.
          </MessageBarBody>
        </MessageBar>
      </div>

      {/* Start Training */}
      <div className={styles.section}>
        <Text className={styles.sectionTitle}>2. Start Training</Text>
        <Button
          icon={<Play24Regular />}
          appearance="primary"
          size="large"
          onClick={handleStartTraining}
          disabled={!canStartTraining}
        >
          {isStartingTraining ? 'Starting Training...' : 'Start Training'}
        </Button>
        {!canStartTraining && !hasInsufficientData && (
          <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
            Training is already in progress or system is not ready
          </Text>
        )}
      </div>

      {/* Active Training Progress */}
      {activeTrainingJob && (
        <div className={styles.section}>
          <Text className={styles.sectionTitle}>Training Progress</Text>
          <Card className={styles.progressCard}>
            <div className={styles.progressHeader}>
              <div>
                <Text weight="semibold" size={500}>
                  Training in Progress
                </Text>
                <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                  Job ID: {activeTrainingJob.jobId}
                </Text>
              </div>
              <Button icon={<Dismiss24Regular />} onClick={handleCancelTraining}>
                Cancel
              </Button>
            </div>

            <div className={styles.progressDetails}>
              <ProgressBar
                value={activeTrainingJob.progress / 100}
                style={{ marginBottom: tokens.spacingVerticalS }}
              />
              <Text>
                Progress: {activeTrainingJob.progress.toFixed(1)}% - {activeTrainingJob.state}
              </Text>

              {activeTrainingJob.metrics && (
                <div className={styles.metricsGrid}>
                  <div className={styles.metricItem}>
                    <Text className={styles.metricLabel}>Loss</Text>
                    <Text className={styles.metricValue}>
                      {activeTrainingJob.metrics.loss.toFixed(4)}
                    </Text>
                  </div>
                  <div className={styles.metricItem}>
                    <Text className={styles.metricLabel}>Samples</Text>
                    <Text className={styles.metricValue}>{activeTrainingJob.metrics.samples}</Text>
                  </div>
                  <div className={styles.metricItem}>
                    <Text className={styles.metricLabel}>Duration</Text>
                    <Text className={styles.metricValue}>{activeTrainingJob.metrics.duration}</Text>
                  </div>
                </div>
              )}
            </div>
          </Card>
        </div>
      )}

      {/* Training History */}
      {trainingHistory.length > 0 && (
        <div className={styles.section}>
          <Text className={styles.sectionTitle}>Training History</Text>
          <div className={styles.historyList}>
            {trainingHistory.slice(0, 5).map((job) => (
              <Card key={job.jobId} className={styles.historyCard}>
                <div className={styles.historyCardContent}>
                  <div>
                    <Text weight="semibold">{job.jobId}</Text>
                    <Text size={200} style={{ display: 'block', marginTop: '4px' }}>
                      {new Date(job.createdAt).toLocaleString()}
                    </Text>
                  </div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                    {job.state === 'Completed' && (
                      <span className={`${styles.statusBadge} ${styles.completedBadge}`}>
                        <Checkmark24Regular />
                        Completed
                      </span>
                    )}
                    {job.state === 'Failed' && (
                      <span className={`${styles.statusBadge} ${styles.failedBadge}`}>
                        <Warning24Regular />
                        Failed
                      </span>
                    )}
                    {job.state === 'Cancelled' && (
                      <span className={`${styles.statusBadge} ${styles.cancelledBadge}`}>
                        <Dismiss24Regular />
                        Cancelled
                      </span>
                    )}
                  </div>
                </div>
                {job.error && (
                  <Text
                    size={200}
                    style={{ color: tokens.colorPaletteRedForeground1, marginTop: '8px' }}
                  >
                    Error: {job.error}
                  </Text>
                )}
                {job.modelPath && (
                  <Text
                    size={200}
                    style={{ color: tokens.colorNeutralForeground3, marginTop: '8px' }}
                  >
                    Model: {job.modelPath}
                  </Text>
                )}
              </Card>
            ))}
          </div>
        </div>
      )}

      {/* Model Management */}
      <div className={styles.section}>
        <Text className={styles.sectionTitle}>Model Management</Text>
        <div className={styles.actionButtons}>
          <Dialog
            open={showRevertDialog}
            onOpenChange={(_, data) => setShowRevertDialog(data.open)}
          >
            <DialogTrigger disableButtonEnhancement>
              <Button icon={<ArrowReset24Regular />}>Revert to Default Model</Button>
            </DialogTrigger>
            <DialogSurface>
              <DialogBody>
                <DialogTitle>Revert to Default Model?</DialogTitle>
                <DialogContent>
                  <Text>
                    This will replace your current custom model with the factory default model. Your
                    annotations will be preserved, but you&apos;ll need to retrain if you want to
                    use a custom model again.
                  </Text>
                </DialogContent>
                <DialogActions>
                  <DialogTrigger disableButtonEnhancement>
                    <Button appearance="secondary">Cancel</Button>
                  </DialogTrigger>
                  <Button
                    appearance="primary"
                    onClick={handleRevertToDefault}
                    disabled={isReverting}
                  >
                    {isReverting ? <Spinner size="tiny" /> : 'Revert to Default'}
                  </Button>
                </DialogActions>
              </DialogBody>
            </DialogSurface>
          </Dialog>
        </div>
        <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
          Use the default model if your custom model isn&apos;t performing well or if you want to
          start fresh.
        </Text>
      </div>
    </div>
  );
};
