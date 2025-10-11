import { useState, useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Card,
  Text,
  Title2,
  Title3,
  Button,
  ProgressBar,
  Spinner,
} from '@fluentui/react-components';
import {
  Dismiss24Regular,
  Checkmark24Regular,
  ErrorCircle24Regular,
  ArrowRight24Regular,
} from '@fluentui/react-icons';
import { useJobsStore } from '../../state/jobs';

const useStyles = makeStyles({
  panel: {
    position: 'fixed',
    top: 0,
    right: 0,
    width: '500px',
    height: '100vh',
    backgroundColor: tokens.colorNeutralBackground1,
    borderLeft: `1px solid ${tokens.colorNeutralStroke1}`,
    boxShadow: tokens.shadow64,
    display: 'flex',
    flexDirection: 'column',
    zIndex: 1000,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: tokens.spacingVerticalL,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  content: {
    flex: 1,
    overflowY: 'auto',
    padding: tokens.spacingVerticalL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  step: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  stepHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  stepIcon: {
    width: '32px',
    height: '32px',
    borderRadius: '50%',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: tokens.colorNeutralBackground3,
  },
  stepIconActive: {
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundOnBrand,
  },
  stepIconDone: {
    backgroundColor: tokens.colorPaletteGreenBackground3,
    color: tokens.colorNeutralForegroundOnBrand,
  },
  stepIconFailed: {
    backgroundColor: tokens.colorPaletteRedBackground3,
    color: tokens.colorNeutralForegroundOnBrand,
  },
  logs: {
    backgroundColor: tokens.colorNeutralBackground3,
    padding: tokens.spacingVerticalM,
    borderRadius: tokens.borderRadiusMedium,
    fontFamily: 'monospace',
    fontSize: '12px',
    maxHeight: '200px',
    overflowY: 'auto',
    whiteSpace: 'pre-wrap',
  },
  actions: {
    padding: tokens.spacingVerticalL,
    borderTop: `1px solid ${tokens.colorNeutralStroke1}`,
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
  },
});

interface GenerationPanelProps {
  jobId: string;
  onClose: () => void;
}

const STAGES = ['Script', 'Voice', 'Visuals', 'Compose', 'Render', 'Complete'];

export function GenerationPanel({ jobId, onClose }: GenerationPanelProps) {
  const styles = useStyles();
  const { activeJob, getJob } = useJobsStore();
  const [showLogs, setShowLogs] = useState(false);

  useEffect(() => {
    getJob(jobId);
  }, [jobId, getJob]);

  if (!activeJob) {
    return (
      <div className={styles.panel}>
        <div className={styles.header}>
          <Title2>Loading...</Title2>
        </div>
        <div className={styles.content}>
          <Spinner label="Loading job..." />
        </div>
      </div>
    );
  }

  const currentStageIndex = STAGES.indexOf(activeJob.stage);

  return (
    <div className={styles.panel}>
      <div className={styles.header}>
        <Title2>Video Generation</Title2>
        <Button
          appearance="subtle"
          icon={<Dismiss24Regular />}
          onClick={onClose}
        />
      </div>

      <div className={styles.content}>
        {/* Progress Overview */}
        <Card>
          <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalM }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <Text weight="semibold">{activeJob.stage}</Text>
              <Text size={200}>{activeJob.percent}%</Text>
            </div>
            <ProgressBar value={activeJob.percent / 100} />
            {activeJob.status === 'Running' && (
              <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                {activeJob.eta ? `ETA: ${activeJob.eta}` : 'Processing...'}
              </Text>
            )}
            {activeJob.status === 'Failed' && (
              <Text size={200} style={{ color: tokens.colorPaletteRedForeground1 }}>
                {activeJob.errorMessage || 'Generation failed'}
              </Text>
            )}
          </div>
        </Card>

        {/* Stage Steps */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalL }}>
          {STAGES.map((stage, index) => {
            const isActive = index === currentStageIndex;
            const isDone = index < currentStageIndex || activeJob.status === 'Done';
            const isFailed = activeJob.status === 'Failed' && isActive;

            return (
              <div key={stage} className={styles.step}>
                <div className={styles.stepHeader}>
                  <div
                    className={`${styles.stepIcon} ${
                      isFailed
                        ? styles.stepIconFailed
                        : isDone
                        ? styles.stepIconDone
                        : isActive
                        ? styles.stepIconActive
                        : ''
                    }`}
                  >
                    {isFailed ? (
                      <ErrorCircle24Regular />
                    ) : isDone ? (
                      <Checkmark24Regular />
                    ) : (
                      <Text>{index + 1}</Text>
                    )}
                  </div>
                  <div>
                    <Title3>{stage}</Title3>
                    {isActive && activeJob.status === 'Running' && (
                      <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                        In progress...
                      </Text>
                    )}
                    {isDone && !isFailed && (
                      <Text size={200} style={{ color: tokens.colorPaletteGreenForeground1 }}>
                        Complete
                      </Text>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>

        {/* Logs */}
        {activeJob.logs.length > 0 && (
          <Card>
            <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalM }}>
              <Button
                appearance="transparent"
                onClick={() => setShowLogs(!showLogs)}
              >
                {showLogs ? 'Hide' : 'Show'} Logs ({activeJob.logs.length})
              </Button>
              {showLogs && (
                <div className={styles.logs}>
                  {activeJob.logs.slice(-20).join('\n')}
                </div>
              )}
            </div>
          </Card>
        )}

        {/* Artifacts */}
        {activeJob.artifacts.length > 0 && (
          <Card>
            <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalM }}>
              <Title3>Output Files</Title3>
              {activeJob.artifacts.map((artifact) => (
                <div
                  key={artifact.name}
                  style={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                  }}
                >
                  <div>
                    <Text weight="semibold">{artifact.name}</Text>
                    <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                      {(artifact.sizeBytes / 1024 / 1024).toFixed(2)} MB
                    </Text>
                  </div>
                  <Button
                    appearance="secondary"
                    onClick={() => {
                      // Open file location
                      window.open(`file:///${artifact.path.replace(/\\/g, '/')}`);
                    }}
                  >
                    Open
                  </Button>
                </div>
              ))}
            </div>
          </Card>
        )}
      </div>

      <div className={styles.actions}>
        {activeJob.status === 'Done' && (
          <Button
            appearance="primary"
            icon={<ArrowRight24Regular />}
            onClick={onClose}
          >
            Done
          </Button>
        )}
        {activeJob.status === 'Failed' && (
          <Button appearance="secondary" onClick={onClose}>
            Close
          </Button>
        )}
      </div>
    </div>
  );
}
