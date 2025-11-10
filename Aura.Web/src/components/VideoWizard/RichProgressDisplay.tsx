import {
  makeStyles,
  tokens,
  Text,
  ProgressBar,
  Button,
  Card,
  Spinner,
} from '@fluentui/react-components';
import {
  Checkmark24Filled,
  Clock24Regular,
  Dismiss24Regular,
  Pause24Regular,
  Play24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import type { FC } from 'react';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalXL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
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
    backgroundColor: tokens.colorNeutralBackground1,
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
    backgroundColor: tokens.colorNeutralBackground1,
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
    justifyContent: 'center',
  },
  previewSection: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalM,
  },
  previewGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(120px, 1fr))',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalM,
  },
  previewItem: {
    aspectRatio: '16/9',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusSmall,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    position: 'relative',
    overflow: 'hidden',
  },
  previewImage: {
    width: '100%',
    height: '100%',
    objectFit: 'cover',
  },
  completionBadge: {
    position: 'absolute',
    top: '4px',
    right: '4px',
    backgroundColor: tokens.colorPaletteGreenBackground2,
    color: tokens.colorPaletteGreenForeground1,
    borderRadius: '50%',
    width: '20px',
    height: '20px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
});

export interface ProgressStage {
  id: string;
  name: string;
  description: string;
  status: 'pending' | 'active' | 'completed' | 'error';
  progress?: number;
  estimatedTime?: number;
}

export interface RichProgressDisplayProps {
  stages: ProgressStage[];
  currentStage: string;
  overallProgress: number;
  timeElapsed?: number;
  timeRemaining?: number;
  onPause?: () => void;
  onResume?: () => void;
  onCancel?: () => void;
  isPaused?: boolean;
  canPause?: boolean;
  canCancel?: boolean;
  preview?: {
    items: Array<{
      id: string;
      thumbnail?: string;
      status: 'pending' | 'completed';
    }>;
  };
}

export const RichProgressDisplay: FC<RichProgressDisplayProps> = ({
  stages,
  currentStage,
  overallProgress,
  timeElapsed = 0,
  timeRemaining = 0,
  onPause,
  onResume,
  onCancel,
  isPaused = false,
  canPause = true,
  canCancel = true,
  preview,
}) => {
  const styles = useStyles();
  const [displayedProgress, setDisplayedProgress] = useState(0);

  // Smooth progress animation
  useEffect(() => {
    const interval = setInterval(() => {
      setDisplayedProgress((prev) => {
        if (prev < overallProgress) {
          return Math.min(prev + 1, overallProgress);
        }
        return prev;
      });
    }, 20);

    return () => clearInterval(interval);
  }, [overallProgress]);

  const formatTime = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  const getStageIcon = (stage: ProgressStage) => {
    if (stage.status === 'completed') {
      return <Checkmark24Filled />;
    }
    if (stage.status === 'active') {
      return <Spinner size="tiny" />;
    }
    return <Text weight="semibold">{stages.indexOf(stage) + 1}</Text>;
  };

  const getStageIconClass = (stage: ProgressStage) => {
    if (stage.status === 'completed') return styles.stageIconCompleted;
    if (stage.status === 'active') return styles.stageIconActive;
    return styles.stageIconPending;
  };

  const getStageRowClass = (stage: ProgressStage) => {
    let className = styles.stageRow;
    if (stage.status === 'active') className += ` ${styles.stageRowActive}`;
    if (stage.status === 'completed') className += ` ${styles.stageRowCompleted}`;
    return className;
  };

  const completedStages = stages.filter((s) => s.status === 'completed').length;

  return (
    <Card className={styles.container}>
      {/* Overall Progress */}
      <div>
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: tokens.spacingVerticalS }}>
          <Text weight="semibold" size={400}>
            Generating Your Video
          </Text>
          <Text weight="semibold" size={400}>
            {Math.round(displayedProgress)}%
          </Text>
        </div>
        <ProgressBar value={displayedProgress / 100} thickness="large" />
      </div>

      {/* Stats Row */}
      <div className={styles.statsRow}>
        <div className={styles.statItem}>
          <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
            Time Elapsed
          </Text>
          <Text weight="semibold" size={400}>
            <Clock24Regular style={{ marginRight: tokens.spacingHorizontalXS }} />
            {formatTime(timeElapsed)}
          </Text>
        </div>
        <div className={styles.statItem}>
          <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
            Time Remaining
          </Text>
          <Text weight="semibold" size={400}>
            <Clock24Regular style={{ marginRight: tokens.spacingHorizontalXS }} />
            {formatTime(timeRemaining)}
          </Text>
        </div>
        <div className={styles.statItem}>
          <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
            Stages Complete
          </Text>
          <Text weight="semibold" size={400}>
            {completedStages} / {stages.length}
          </Text>
        </div>
      </div>

      {/* Stages */}
      <div className={styles.stagesContainer}>
        {stages.map((stage) => (
          <div key={stage.id} className={getStageRowClass(stage)}>
            <div className={`${styles.stageIcon} ${getStageIconClass(stage)}`}>
              {getStageIcon(stage)}
            </div>
            <div className={styles.stageContent}>
              <Text weight="semibold">{stage.name}</Text>
              <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                {stage.description}
              </Text>
              {stage.status === 'active' && stage.progress !== undefined && (
                <div className={styles.stageProgress}>
                  <ProgressBar value={stage.progress / 100} />
                </div>
              )}
            </div>
            {stage.estimatedTime && stage.status === 'pending' && (
              <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                ~{stage.estimatedTime}s
              </Text>
            )}
          </div>
        ))}
      </div>

      {/* Preview Section */}
      {preview && preview.items.length > 0 && (
        <div className={styles.previewSection}>
          <Text weight="semibold" size={300}>
            Preview Progress
          </Text>
          <div className={styles.previewGrid}>
            {preview.items.map((item) => (
              <div key={item.id} className={styles.previewItem}>
                {item.thumbnail ? (
                  <img src={item.thumbnail} alt="" className={styles.previewImage} />
                ) : (
                  <Spinner size="tiny" />
                )}
                {item.status === 'completed' && (
                  <div className={styles.completionBadge}>
                    <Checkmark24Filled style={{ fontSize: '12px' }} />
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Controls */}
      <div className={styles.controls}>
        {canPause && (
          <>
            {isPaused ? (
              <Button
                appearance="secondary"
                icon={<Play24Regular />}
                onClick={onResume}
                disabled={!onResume}
              >
                Resume
              </Button>
            ) : (
              <Button
                appearance="secondary"
                icon={<Pause24Regular />}
                onClick={onPause}
                disabled={!onPause}
              >
                Pause
              </Button>
            )}
          </>
        )}
        {canCancel && (
          <Button
            appearance="secondary"
            icon={<Dismiss24Regular />}
            onClick={onCancel}
            disabled={!onCancel}
          >
            Cancel
          </Button>
        )}
      </div>
    </Card>
  );
};
