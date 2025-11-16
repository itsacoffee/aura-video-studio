/**
 * VideoGenerationProgress Component
 *
 * Displays real-time progress for video generation using Server-Sent Events (SSE).
 * Shows progress bar, current stage, time estimates, and provides cancellation functionality.
 */

import {
  makeStyles,
  tokens,
  Text,
  ProgressBar,
  Button,
  Card,
  MessageBar,
  MessageBarBody,
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
} from '@fluentui/react-components';
import {
  Checkmark24Filled,
  Clock24Regular,
  DocumentArrowDown24Regular,
  Stop24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect, useCallback } from 'react';
import type { FC } from 'react';
import { useSSEConnection } from '@/hooks/useSSEConnection';
import { loggingService } from '@/services/loggingService';
import { apiUrl } from '@/config/api';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalXL,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
    boxShadow: tokens.shadow8,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  stagesContainer: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  stageRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    transition: 'all 0.3s ease',
  },
  stageRowActive: {
    backgroundColor: tokens.colorBrandBackground2,
    transform: 'scale(1.02)',
  },
  stageRowCompleted: {
    opacity: 0.7,
  },
  stageIcon: {
    width: '32px',
    height: '32px',
    borderRadius: '50%',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    flexShrink: 0,
  },
  stageIconPending: {
    backgroundColor: tokens.colorNeutralBackground3,
    color: tokens.colorNeutralForeground3,
  },
  stageIconActive: {
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundOnBrand,
  },
  stageIconCompleted: {
    backgroundColor: tokens.colorPaletteGreenBackground2,
    color: tokens.colorPaletteGreenForeground1,
  },
  stageContent: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  stageProgress: {
    width: '100%',
  },
  statsRow: {
    display: 'flex',
    gap: tokens.spacingHorizontalXL,
    justifyContent: 'space-between',
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  statItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    alignItems: 'center',
  },
  controls: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
  },
  mainProgress: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  progressHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
});

interface Stage {
  id: string;
  label: string;
  description: string;
  minProgress: number;
  maxProgress: number;
}

const STAGES: Stage[] = [
  {
    id: 'script',
    label: 'Script Generation',
    description: 'Generating video script from brief',
    minProgress: 0,
    maxProgress: 15,
  },
  {
    id: 'audio',
    label: 'Audio Synthesis',
    description: 'Converting script to speech',
    minProgress: 15,
    maxProgress: 35,
  },
  {
    id: 'visuals',
    label: 'Visual Generation',
    description: 'Creating video scenes',
    minProgress: 35,
    maxProgress: 65,
  },
  {
    id: 'compositing',
    label: 'Timeline Composition',
    description: 'Assembling audio and visuals',
    minProgress: 65,
    maxProgress: 85,
  },
  {
    id: 'rendering',
    label: 'Video Rendering',
    description: 'Final video encoding',
    minProgress: 85,
    maxProgress: 100,
  },
];

export interface VideoGenerationProgressProps {
  jobId: string;
  onComplete?: (result: { videoUrl: string; videoPath: string }) => void;
  onError?: (error: Error) => void;
  onCancel?: () => void;
}

export const VideoGenerationProgress: FC<VideoGenerationProgressProps> = ({
  jobId,
  onComplete,
  onError,
  onCancel,
}) => {
  const styles = useStyles();
  const [overallProgress, setOverallProgress] = useState(0);
  const [currentMessage, setCurrentMessage] = useState('');
  const [elapsedTime, setElapsedTime] = useState<string>('');
  const [estimatedTimeRemaining, setEstimatedTimeRemaining] = useState<string>('');
  const [isCompleted, setIsCompleted] = useState(false);
  const [isFailed, setIsFailed] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [videoResult, setVideoResult] = useState<{ videoUrl: string; videoPath: string } | null>(
    null
  );
  const [showCancelDialog, setShowCancelDialog] = useState(false);
  const [isCancelling, setIsCancelling] = useState(false);

  // SSE connection for real-time updates
  const { connect, disconnect, isConnected } = useSSEConnection({
    onMessage: (message) => {
      loggingService.debug('SSE message received', 'VideoGenerationProgress', 'onMessage', {
        type: message.type,
      });

      switch (message.type) {
        case 'job-status': {
          const data = message.data as { status: string; stage: string; percent: number };
          setOverallProgress(data.percent);
          break;
        }

        case 'step-progress': {
          const data = message.data as {
            step: string;
            phase: string;
            progressPct: number;
            message: string;
            elapsedTime?: string;
            estimatedTimeRemaining?: string;
          };
          setOverallProgress(data.progressPct);
          setCurrentMessage(data.message);
          if (data.elapsedTime) {
            setElapsedTime(data.elapsedTime);
          }
          if (data.estimatedTimeRemaining) {
            setEstimatedTimeRemaining(data.estimatedTimeRemaining);
          }
          break;
        }

        case 'step-status': {
          const data = message.data as { step: string; status: string };
          loggingService.debug('Step status update', 'VideoGenerationProgress', 'step-status', {
            step: data.step,
            status: data.status,
          });
          break;
        }

        case 'job-completed': {
          const data = message.data as {
            output: { videoPath: string; subtitlePath?: string };
          };
          setIsCompleted(true);
          setOverallProgress(100);
          const result = {
            videoUrl: `/api/artifacts/${jobId}/video`,
            videoPath: data.output.videoPath,
          };
          setVideoResult(result);
          loggingService.info(
            'Video generation completed',
            'VideoGenerationProgress',
            'job-completed',
            { videoPath: data.output.videoPath }
          );
          if (onComplete) {
            onComplete(result);
          }
          disconnect();
          break;
        }

        case 'job-failed': {
          const data = message.data as { errorMessage?: string };
          setIsFailed(true);
          setErrorMessage(data.errorMessage || 'Video generation failed');
          loggingService.error(
            'Video generation failed',
            new Error(data.errorMessage || 'Unknown error'),
            'VideoGenerationProgress',
            'job-failed'
          );
          if (onError) {
            onError(new Error(data.errorMessage || 'Video generation failed'));
          }
          disconnect();
          break;
        }

        case 'job-cancelled': {
          setIsCancelling(false);
          loggingService.info(
            'Video generation cancelled',
            'VideoGenerationProgress',
            'job-cancelled'
          );
          if (onCancel) {
            onCancel();
          }
          disconnect();
          break;
        }

        case 'error': {
          const data = message.data as { message: string };
          loggingService.error(
            'SSE error event',
            new Error(data.message),
            'VideoGenerationProgress',
            'error'
          );
          break;
        }
      }
    },
    onError: (error) => {
      loggingService.error('SSE connection error', error, 'VideoGenerationProgress', 'onError');
      if (!isCompleted && !isFailed && onError) {
        onError(error);
      }
    },
  });

  useEffect(() => {
    if (jobId) {
      loggingService.info('Connecting to SSE', 'VideoGenerationProgress', 'useEffect', { jobId });
      connect(`/api/jobs/${jobId}/events`);
    }

    return () => {
      disconnect();
    };
  }, [jobId, connect, disconnect]);

  const handleCancel = useCallback(async () => {
    setShowCancelDialog(false);
    setIsCancelling(true);

    try {
      const response = await fetch(apiUrl(`/api/jobs/${jobId}/cancel`), {
        method: 'POST',
      });

      if (!response.ok) {
        throw new Error('Failed to cancel job');
      }

      loggingService.info('Job cancellation requested', 'VideoGenerationProgress', 'handleCancel');
    } catch (error) {
      loggingService.error(
        'Failed to cancel job',
        error instanceof Error ? error : new Error(String(error)),
        'VideoGenerationProgress',
        'handleCancel'
      );
      setIsCancelling(false);
    }
  }, [jobId]);

  const getStageStatus = (stage: Stage): 'pending' | 'active' | 'completed' => {
    if (overallProgress >= stage.maxProgress) return 'completed';
    if (overallProgress >= stage.minProgress && overallProgress < stage.maxProgress)
      return 'active';
    return 'pending';
  };

  const getStageProgress = (stage: Stage): number => {
    if (overallProgress < stage.minProgress) return 0;
    if (overallProgress >= stage.maxProgress) return 100;
    const range = stage.maxProgress - stage.minProgress;
    const progress = overallProgress - stage.minProgress;
    return Math.round((progress / range) * 100);
  };

  return (
    <Card className={styles.container}>
      <div className={styles.header}>
        <Text size={600} weight="semibold">
          Video Generation Progress
        </Text>
        {isConnected && (
          <Text size={300} style={{ color: tokens.colorPaletteGreenForeground1 }}>
            ● Live
          </Text>
        )}
      </div>

      <div className={styles.mainProgress}>
        <div className={styles.progressHeader}>
          <Text weight="semibold">{overallProgress}% Complete</Text>
          {currentMessage && <Text size={300}>{currentMessage}</Text>}
        </div>
        <ProgressBar value={overallProgress / 100} thickness="large" />
      </div>

      {(elapsedTime || estimatedTimeRemaining) && (
        <div className={styles.statsRow}>
          {elapsedTime && (
            <div className={styles.statItem}>
              <Clock24Regular />
              <Text size={300}>Elapsed</Text>
              <Text weight="semibold">{elapsedTime}</Text>
            </div>
          )}
          {estimatedTimeRemaining && (
            <div className={styles.statItem}>
              <Clock24Regular />
              <Text size={300}>Remaining</Text>
              <Text weight="semibold">{estimatedTimeRemaining}</Text>
            </div>
          )}
        </div>
      )}

      <div className={styles.stagesContainer}>
        {STAGES.map((stage) => {
          const status = getStageStatus(stage);
          const progress = getStageProgress(stage);

          return (
            <div
              key={stage.id}
              className={`${styles.stageRow} ${
                status === 'active' ? styles.stageRowActive : ''
              } ${status === 'completed' ? styles.stageRowCompleted : ''}`}
            >
              <div
                className={`${styles.stageIcon} ${
                  status === 'pending'
                    ? styles.stageIconPending
                    : status === 'active'
                      ? styles.stageIconActive
                      : styles.stageIconCompleted
                }`}
              >
                {status === 'completed' ? (
                  <Checkmark24Filled />
                ) : (
                  <Text weight="semibold">{stage.id.charAt(0).toUpperCase()}</Text>
                )}
              </div>
              <div className={styles.stageContent}>
                <Text weight="semibold">{stage.label}</Text>
                <Text size={300}>{stage.description}</Text>
                {status !== 'pending' && (
                  <ProgressBar value={progress / 100} className={styles.stageProgress} />
                )}
              </div>
              {status !== 'pending' && (
                <Text size={300} weight="semibold">
                  {progress}%
                </Text>
              )}
            </div>
          );
        })}
      </div>

      {isCompleted && videoResult && (
        <MessageBar intent="success">
          <MessageBarBody>
            <Text weight="semibold">Video generation completed successfully!</Text>
            <div className={styles.controls} style={{ marginTop: tokens.spacingVerticalM }}>
              <Button
                appearance="primary"
                icon={<DocumentArrowDown24Regular />}
                as="a"
                href={videoResult.videoUrl}
                download
              >
                Download Video
              </Button>
            </div>
          </MessageBarBody>
        </MessageBar>
      )}

      {isFailed && (
        <MessageBar intent="error">
          <MessageBarBody>
            <Text weight="semibold">Video generation failed</Text>
            <Text>{errorMessage}</Text>
          </MessageBarBody>
        </MessageBar>
      )}

      {!isCompleted && !isFailed && (
        <div className={styles.controls}>
          <Dialog
            open={showCancelDialog}
            onOpenChange={(_, data) => setShowCancelDialog(data.open)}
          >
            <DialogTrigger disableButtonEnhancement>
              <Button appearance="outline" icon={<Stop24Regular />} disabled={isCancelling}>
                {isCancelling ? 'Cancelling...' : 'Cancel Generation'}
              </Button>
            </DialogTrigger>
            <DialogSurface>
              <DialogBody>
                <DialogTitle>Cancel Video Generation?</DialogTitle>
                <DialogContent>
                  <Text>
                    Are you sure you want to cancel this video generation? This will stop the
                    process and clean up temporary files. This action cannot be undone.
                  </Text>
                </DialogContent>
                <DialogActions>
                  <DialogTrigger disableButtonEnhancement>
                    <Button appearance="secondary">Keep Running</Button>
                  </DialogTrigger>
                  <Button appearance="primary" onClick={handleCancel}>
                    Yes, Cancel
                  </Button>
                </DialogActions>
              </DialogBody>
            </DialogSurface>
          </Dialog>
        </div>
      )}
    </Card>
  );
};
