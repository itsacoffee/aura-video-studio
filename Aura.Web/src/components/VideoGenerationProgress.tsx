/**
 * VideoGenerationProgress Component
 *
 * Displays real-time progress for video generation using Server-Sent Events (SSE).
 * Shows progress bar, stage status, live console output, and provides cancellation and download actions.
 */

import {
  Badge,
  Button,
  Card,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  DialogTrigger,
  Divider,
  makeStyles,
  MessageBar,
  MessageBarActions,
  MessageBarBody,
  ProgressBar,
  Text,
  tokens,
  Tooltip,
} from '@fluentui/react-components';
import {
  Checkmark24Filled,
  Clock24Regular,
  Dismiss24Regular,
  DocumentArrowDown24Regular,
  Stop24Regular,
} from '@fluentui/react-icons';
import type { FC } from 'react';
import { useCallback, useEffect, useState, useMemo } from 'react';
import { apiUrl } from '@/config/api';
import { useSSEConnection } from '@/hooks/useSSEConnection';
import { loggingService } from '@/services/loggingService';
import { useProviderStatus } from '@/hooks/useProviderStatus';

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
  headerActions: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
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
    transform: 'scale(1.01)',
  },
  stageRowCompleted: {
    opacity: 0.75,
  },
  stageIcon: {
    width: '32px',
    height: '32px',
    borderRadius: '50%',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    flexShrink: 0,
    fontWeight: tokens.fontWeightSemibold,
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
  stageMeta: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
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
    flex: 1,
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
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalS,
  },
  logSection: {
    marginTop: tokens.spacingVerticalXL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    padding: tokens.spacingVerticalL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  logHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  logContainer: {
    maxHeight: '280px',
    overflowY: 'auto',
    borderRadius: tokens.borderRadiusSmall,
    backgroundColor: tokens.colorNeutralBackground1,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  logEntry: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalS,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    alignItems: 'baseline',
    fontFamily: "'Segoe UI', Consolas, 'Courier New', monospace",
    fontSize: tokens.fontSizeBase200,
  },
  logTimestamp: {
    color: tokens.colorNeutralForeground3,
    minWidth: '72px',
    fontSize: tokens.fontSizeBase200,
  },
  logMessage: {
    flex: 1,
    whiteSpace: 'pre-wrap',
    wordBreak: 'break-word',
  },
  warningList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
});

type StagePhase = 'plan' | 'tts' | 'visuals' | 'compose' | 'render';

interface StageDefinition {
  id: StagePhase;
  label: string;
  description: string;
  minProgress: number;
  maxProgress: number;
}

const STAGES: StageDefinition[] = [
  {
    id: 'plan',
    label: 'Planning & Script',
    description: 'Brief analysis, script drafting, and scene planning',
    minProgress: 0,
    maxProgress: 20,
  },
  {
    id: 'tts',
    label: 'Voiceover',
    description: 'Synthesizing narration with selected voice',
    minProgress: 20,
    maxProgress: 40,
  },
  {
    id: 'visuals',
    label: 'Visual Assets',
    description: 'Generating or selecting images and clips',
    minProgress: 40,
    maxProgress: 60,
  },
  {
    id: 'compose',
    label: 'Timeline Assembly',
    description: 'Aligning narration, visuals, and overlays',
    minProgress: 60,
    maxProgress: 85,
  },
  {
    id: 'render',
    label: 'Rendering & Export',
    description: 'FFmpeg rendering with hardware acceleration',
    minProgress: 85,
    maxProgress: 100,
  },
];

const PHASE_ORDER: StagePhase[] = STAGES.map((stage) => stage.id);

type LogSeverity = 'info' | 'warning' | 'error';

interface JobLogEventPayload {
  jobId: string;
  message: string;
  stage?: string;
  severity?: LogSeverity | string;
  timestamp?: string;
  correlationId?: string;
}

interface LogEntry extends JobLogEventPayload {
  id: string;
  severity: LogSeverity;
  timestamp: string;
}

interface ProgressEventPayload {
  jobId: string;
  stage: string;
  percent: number;
  etaSeconds?: number;
  message: string;
  warnings: string[];
  correlationId?: string;
  substageDetail?: string;
  currentItem?: number;
  totalItems?: number;
  timestamp?: string;
  phase?: string;
  elapsedSeconds?: number;
  estimatedRemainingSeconds?: number;
}

const normalizeSeverity = (severity?: string): LogSeverity => {
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

const mapStageToPhaseClient = (stage?: string): StagePhase => {
  if (!stage) {
    return 'plan';
  }

  const normalized = stage.toLowerCase();
  if (
    normalized.includes('script') ||
    normalized.includes('plan') ||
    normalized.includes('brief') ||
    normalized.includes('initial') ||
    normalized.includes('queue')
  ) {
    return 'plan';
  }
  if (normalized.includes('tts') || normalized.includes('audio') || normalized.includes('voice')) {
    return 'tts';
  }
  if (
    normalized.includes('visual') ||
    normalized.includes('image') ||
    normalized.includes('asset')
  ) {
    return 'visuals';
  }
  if (
    normalized.includes('composition') ||
    normalized.includes('compose') ||
    normalized.includes('timeline')
  ) {
    return 'compose';
  }
  return 'render';
};

const normalizePhase = (phase?: string | null): StagePhase => {
  if (!phase) {
    return 'plan';
  }
  switch (phase) {
    case 'processing':
      return 'plan';
    case 'complete':
      return 'render';
    case 'plan':
    case 'tts':
    case 'visuals':
    case 'compose':
    case 'render':
      return phase;
    default:
      return 'plan';
  }
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
  const [elapsedTime, setElapsedTime] = useState('');
  const [estimatedTimeRemaining, setEstimatedTimeRemaining] = useState('');
  const [isCompleted, setIsCompleted] = useState(false);
  const [isFailed, setIsFailed] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [videoResult, setVideoResult] = useState<{ videoUrl: string; videoPath: string } | null>(
    null
  );
  const [showCancelDialog, setShowCancelDialog] = useState(false);
  const [isCancelling, setIsCancelling] = useState(false);
  const [phaseState, setPhaseState] = useState<{ current: StagePhase; completed: StagePhase[] }>({
    current: 'plan',
    completed: [],
  });
  const [logEntries, setLogEntries] = useState<LogEntry[]>([]);
  const [warningMessages, setWarningMessages] = useState<string[]>([]);

  // Get provider status to show which providers are being used
  const { llmProviders, ttsProviders, imageProviders } = useProviderStatus(30000); // Poll every 30s during generation

  // Determine active provider based on current stage
  const activeProvider = useMemo(() => {
    const currentPhase = phaseState.current;
    
    // Script generation stage - show LLM provider
    if (currentPhase === 'plan' || currentPhase === 'script') {
      const availableLlm = llmProviders.find(p => p.available);
      if (availableLlm) {
        return {
          name: availableLlm.name,
          tier: availableLlm.tier,
          type: 'LLM' as const,
        };
      }
    }
    
    // TTS stage - show TTS provider
    if (currentPhase === 'tts' || currentPhase === 'audio') {
      const availableTts = ttsProviders.find(p => p.available);
      if (availableTts) {
        return {
          name: availableTts.name,
          tier: availableTts.tier,
          type: 'TTS' as const,
        };
      }
    }
    
    // Image generation stage - show image provider
    if (currentPhase === 'visuals' || currentPhase === 'images') {
      const availableImage = imageProviders.find(p => p.available);
      if (availableImage) {
        return {
          name: availableImage.name,
          tier: availableImage.tier,
          type: 'Image' as const,
        };
      }
    }
    
    return null;
  }, [phaseState.current, llmProviders, ttsProviders, imageProviders]);

  const resetState = useCallback(() => {
    setOverallProgress(0);
    setCurrentMessage('');
    setElapsedTime('');
    setEstimatedTimeRemaining('');
    setIsCompleted(false);
    setIsFailed(false);
    setErrorMessage('');
    setVideoResult(null);
    setPhaseState({ current: 'plan', completed: [] });
    setLogEntries([]);
    setWarningMessages([]);
    setIsCancelling(false);
    setShowCancelDialog(false);
  }, []);

  const appendLogEntry = useCallback((payload: JobLogEventPayload) => {
    if (!payload.message) {
      return;
    }

    setLogEntries((prev) => {
      const severity = normalizeSeverity(payload.severity);
      const timestamp = payload.timestamp ?? new Date().toISOString();
      const id = payload.correlationId
        ? `${timestamp}-${payload.correlationId}-${prev.length}`
        : `${timestamp}-${prev.length}`;

      const entry: LogEntry = {
        ...payload,
        id,
        severity,
        timestamp,
      };

      const next = [...prev, entry];
      if (next.length > 200) {
        return next.slice(next.length - 200);
      }
      return next;
    });
  }, []);

  const pushWarning = useCallback((message?: string) => {
    if (!message) {
      return;
    }

    setWarningMessages((prev) => {
      if (prev.includes(message)) {
        return prev;
      }
      const next = [message, ...prev];
      return next.slice(0, 4);
    });
  }, []);

  const updatePhaseState = useCallback((phase: StagePhase) => {
    setPhaseState((prev) => {
      const currentIndex = PHASE_ORDER.indexOf(phase);
      const completedSet = new Set(prev.completed);

      if (currentIndex > 0) {
        for (let i = 0; i < currentIndex; i += 1) {
          completedSet.add(PHASE_ORDER[i]);
        }
      }

      return {
        current: phase,
        completed: Array.from(completedSet),
      };
    });
  }, []);

  useEffect(() => {
    resetState();
  }, [jobId, resetState]);

  const { connect, disconnect, isConnected } = useSSEConnection({
    // eslint-disable-next-line sonarjs/cognitive-complexity
    onMessage: (message) => {
      loggingService.debug('SSE message received', 'VideoGenerationProgress', 'onMessage', {
        type: message.type,
      });

      switch (message.type) {
        case 'job-status': {
          const data = message.data as { status?: string; stage?: string; percent?: number };
          if (typeof data.percent === 'number') {
            setOverallProgress(data.percent);
          }
          if (data.stage) {
            updatePhaseState(mapStageToPhaseClient(data.stage));
          }
          break;
        }

        case 'step-progress': {
          const data = message.data as ProgressEventPayload;
          if (typeof data.percent === 'number') {
            setOverallProgress(data.percent);
          }

          const detailMessage = data.substageDetail
            ? `${data.stage}: ${data.substageDetail}`
            : data.message || data.stage || '';
          setCurrentMessage(detailMessage);

          if (typeof data.elapsedSeconds === 'number') {
            setElapsedTime(formatDurationFromSeconds(data.elapsedSeconds));
          }

          const etaSeconds =
            typeof data.estimatedRemainingSeconds === 'number'
              ? data.estimatedRemainingSeconds
              : typeof data.etaSeconds === 'number'
                ? data.etaSeconds
                : undefined;

          if (etaSeconds !== undefined) {
            setEstimatedTimeRemaining(formatDurationFromSeconds(etaSeconds));
          }

          const normalizedPhase = data.phase
            ? normalizePhase(data.phase)
            : data.stage
              ? mapStageToPhaseClient(data.stage)
              : 'plan';
          updatePhaseState(normalizedPhase);

          if (Array.isArray(data.warnings)) {
            data.warnings.forEach((warning) => pushWarning(warning));
          }
          break;
        }

        case 'step-status': {
          const data = message.data as { phase?: string; step?: string };
          if (data.phase) {
            updatePhaseState(normalizePhase(data.phase));
          } else if (data.step) {
            updatePhaseState(mapStageToPhaseClient(data.step));
          }
          break;
        }

        case 'job-log': {
          const data = message.data as JobLogEventPayload;
          appendLogEntry(data);
          if (normalizeSeverity(data.severity) === 'warning') {
            pushWarning(data.message);
          }
          break;
        }

        case 'warning': {
          const data = message.data as { message: string; step?: string };
          const logPayload: JobLogEventPayload = {
            jobId,
            message: data.message,
            severity: 'warning',
            stage: data.step,
            timestamp: new Date().toISOString(),
          };
          appendLogEntry(logPayload);
          pushWarning(data.message);
          break;
        }

        case 'job-completed': {
          const data = message.data as {
            output: { videoPath: string; subtitlePath?: string };
          };
          setIsCompleted(true);
          setOverallProgress(100);
          updatePhaseState('render');
          const result = {
            videoUrl: `/api/artifacts/${jobId}/video`,
            videoPath: data.output.videoPath,
          };
          setVideoResult(result);
          appendLogEntry({
            jobId,
            message: 'Job completed successfully.',
            severity: 'info',
            stage: 'complete',
            timestamp: new Date().toISOString(),
          });
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
          const data = message.data as { errorMessage?: string; logs?: string[]; stage?: string };
          setIsFailed(true);
          const failureMessage = data.errorMessage || 'Video generation failed';
          setErrorMessage(failureMessage);
          appendLogEntry({
            jobId,
            message: failureMessage,
            severity: 'error',
            stage: data.stage ?? 'pipeline',
            timestamp: new Date().toISOString(),
          });
          if (Array.isArray(data.logs)) {
            data.logs.forEach((log) =>
              appendLogEntry({
                jobId,
                message: log,
                severity: 'error',
                stage: data.stage ?? 'pipeline',
                timestamp: new Date().toISOString(),
              })
            );
          }
          loggingService.error(
            'Video generation failed',
            new Error(failureMessage),
            'VideoGenerationProgress',
            'job-failed'
          );
          if (onError) {
            onError(new Error(failureMessage));
          }
          disconnect();
          break;
        }

        case 'job-cancelled': {
          setIsCancelling(false);
          appendLogEntry({
            jobId,
            message: 'Job was cancelled.',
            severity: 'warning',
            stage: 'pipeline',
            timestamp: new Date().toISOString(),
          });
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

  const getStageStatus = (stage: StageDefinition): 'pending' | 'active' | 'completed' => {
    if (isCompleted) {
      return 'completed';
    }

    const stageIndex = PHASE_ORDER.indexOf(stage.id);
    const currentIndex = PHASE_ORDER.indexOf(phaseState.current);

    if (phaseState.completed.includes(stage.id) || stageIndex < currentIndex) {
      return 'completed';
    }

    if (stageIndex === currentIndex && !isFailed) {
      return 'active';
    }

    return 'pending';
  };

  const getStageProgress = (stage: StageDefinition): number => {
    if (overallProgress < stage.minProgress) return 0;
    if (overallProgress >= stage.maxProgress) return 100;
    const range = stage.maxProgress - stage.minProgress;
    const progress = overallProgress - stage.minProgress;
    return Math.round((progress / range) * 100);
  };

  const handleClearLogs = useCallback(() => {
    setLogEntries([]);
  }, []);

  const handleDismissWarning = useCallback((index: number) => {
    setWarningMessages((prev) => prev.filter((_, i) => i !== index));
  }, []);

  return (
    <Card className={styles.container}>
      <div className={styles.header}>
        <Text size={600} weight="semibold">
          Video Generation Progress
        </Text>
        <div className={styles.headerActions}>
          {isConnected && (
            <Text size={300} style={{ color: tokens.colorPaletteGreenForeground1 }}>
              ● Live
            </Text>
          )}
          <Tooltip content="Clear log entries" relationship="label">
            <Button size="small" appearance="subtle" onClick={handleClearLogs}>
              Clear Log
            </Button>
          </Tooltip>
        </div>
      </div>

      <div className={styles.mainProgress}>
        <div className={styles.progressHeader}>
          <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalXXS }}>
            <Text weight="semibold">{overallProgress}% Complete</Text>
            {activeProvider && (
              <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalXS }}>
                <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                  Using {activeProvider.type}:
                </Text>
                <Badge
                  appearance="filled"
                  color={
                    activeProvider.tier === 'paid'
                      ? 'brand'
                      : activeProvider.tier === 'local'
                        ? 'success'
                        : activeProvider.tier === 'free'
                          ? 'informative'
                          : 'subtle'
                  }
                >
                  {activeProvider.name}
                  {activeProvider.tier === 'paid'
                    ? ' (Paid)'
                    : activeProvider.tier === 'local'
                      ? ' (Local)'
                      : activeProvider.tier === 'free'
                        ? ' (Free)'
                        : ''}
                </Badge>
              </div>
            )}
          </div>
          {currentMessage && <Text size={300}>{currentMessage}</Text>}
        </div>
        <ProgressBar value={overallProgress / 100} thickness="large" />
      </div>

      {(elapsedTime || estimatedTimeRemaining) && (
        <div className={styles.statsRow}>
          {elapsedTime && (
            <div className={styles.statItem}>
              <Text size={200} weight="semibold">
                Elapsed Time
              </Text>
              <Text>{elapsedTime}</Text>
            </div>
          )}
          {estimatedTimeRemaining && (
            <div className={styles.statItem}>
              <Text size={200} weight="semibold">
                ETA
              </Text>
              <Text>{estimatedTimeRemaining}</Text>
            </div>
          )}
        </div>
      )}

      <div className={styles.stagesContainer}>
        {STAGES.map((stage) => {
          const status = getStageStatus(stage);
          const stageProgress = getStageProgress(stage);
          return (
            <div
              key={stage.id}
              className={`${styles.stageRow} ${
                status === 'active'
                  ? styles.stageRowActive
                  : status === 'completed'
                    ? styles.stageRowCompleted
                    : ''
              }`}
            >
              <div
                className={`${styles.stageIcon} ${
                  status === 'completed'
                    ? styles.stageIconCompleted
                    : status === 'active'
                      ? styles.stageIconActive
                      : styles.stageIconPending
                }`}
              >
                {status === 'completed' ? <Checkmark24Filled /> : <Clock24Regular />}
              </div>
              <div className={styles.stageContent}>
                <div className={styles.stageMeta}>
                  <Text weight="semibold">{stage.label}</Text>
                  <Badge appearance="ghost" color="informative">
                    {stage.id.toUpperCase()}
                  </Badge>
                </div>
                <Text size={200}>{stage.description}</Text>
                <ProgressBar
                  className={styles.stageProgress}
                  value={stageProgress / 100}
                  color={status === 'completed' ? 'success' : 'brand'}
                />
              </div>
            </div>
          );
        })}
      </div>

      {warningMessages.length > 0 && (
        <div className={styles.warningList}>
          {warningMessages.map((warning, index) => (
            <MessageBar key={`${warning}-${index}`} intent="warning">
              <MessageBarBody>{warning}</MessageBarBody>
              <MessageBarActions>
                <Button
                  appearance="transparent"
                  icon={<Dismiss24Regular />}
                  onClick={() => handleDismissWarning(index)}
                  aria-label="Dismiss warning"
                />
              </MessageBarActions>
            </MessageBar>
          ))}
        </div>
      )}

      {isFailed && (
        <MessageBar intent="error">
          <MessageBarBody>{errorMessage}</MessageBarBody>
        </MessageBar>
      )}

      <div className={styles.logSection}>
        <div className={styles.logHeader}>
          <Text weight="semibold">Live Console</Text>
          <Text size={200} color={tokens.colorNeutralForeground3}>
            Showing last {logEntries.length} events
          </Text>
        </div>
        <Divider />
        <div className={styles.logContainer}>
          {logEntries.length === 0 ? (
            <Text size={200} style={{ padding: tokens.spacingVerticalM }}>
              Waiting for pipeline events...
            </Text>
          ) : (
            logEntries.map((entry, idx) => (
              <div
                key={entry.id}
                className={styles.logEntry}
                style={idx === logEntries.length - 1 ? { borderBottom: 'none' } : undefined}
              >
                <Text className={styles.logTimestamp}>
                  {new Date(entry.timestamp).toLocaleTimeString()}
                </Text>
                <Badge
                  appearance="outline"
                  color={
                    entry.severity === 'error'
                      ? 'danger'
                      : entry.severity === 'warning'
                        ? 'warning'
                        : 'informative'
                  }
                >
                  {entry.severity.toUpperCase()}
                </Badge>
                <div className={styles.logMessage}>
                  <Text size={300}>{entry.message}</Text>
                  {entry.stage && (
                    <Text size={200} color={tokens.colorNeutralForeground3}>
                      Stage: {entry.stage}
                    </Text>
                  )}
                </div>
              </div>
            ))
          )}
        </div>
      </div>

      <div className={styles.controls}>
        {!isCompleted && !isFailed && (
          <Dialog
            open={showCancelDialog}
            onOpenChange={(_, data) => setShowCancelDialog(data.open)}
          >
            <DialogTrigger disableButtonEnhancement>
              <Button icon={<Stop24Regular />} appearance="secondary" disabled={isCancelling}>
                Cancel Generation
              </Button>
            </DialogTrigger>
            <DialogSurface>
              <DialogBody>
                <DialogTitle>Cancel video generation?</DialogTitle>
                <DialogContent>
                  This will stop the current generation process. You can start again later.
                </DialogContent>
                <DialogActions>
                  <Button appearance="secondary" onClick={() => setShowCancelDialog(false)}>
                    No, keep running
                  </Button>
                  <Button appearance="primary" onClick={handleCancel} disabled={isCancelling}>
                    Yes, cancel
                  </Button>
                </DialogActions>
              </DialogBody>
            </DialogSurface>
          </Dialog>
        )}

        {isCompleted && videoResult && (
          <Button
            icon={<DocumentArrowDown24Regular />}
            appearance="primary"
            as="a"
            href={videoResult.videoUrl}
            target="_blank"
            rel="noreferrer"
          >
            Download Video
          </Button>
        )}
      </div>
    </Card>
  );
};
