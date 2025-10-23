import React from 'react';
import {
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogContent,
  Button,
  makeStyles,
  tokens,
  ProgressBar,
  Text,
  Body1,
  Body1Strong,
  Caption1,
  Divider,
  Accordion,
  AccordionHeader,
  AccordionItem,
  AccordionPanel,
} from '@fluentui/react-components';
import {
  Dismiss24Regular,
  CheckmarkCircle24Regular,
  ErrorCircle24Regular,
  ArrowMinimize24Regular,
  FolderOpen24Regular,
  ArrowExport24Regular,
  ArrowRepeatAll24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  dialogSurface: {
    maxWidth: '600px',
    width: '100%',
  },
  progressSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalXL,
    alignItems: 'center',
  },
  progressCircle: {
    width: '120px',
    height: '120px',
    borderRadius: '50%',
    border: `8px solid ${tokens.colorNeutralStroke2}`,
    borderTopColor: tokens.colorBrandBackground,
    animation: 'spin 1s linear infinite',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  progressText: {
    fontSize: '32px',
    fontWeight: 'bold',
    color: tokens.colorNeutralForeground1,
  },
  stageText: {
    textAlign: 'center',
    color: tokens.colorNeutralForeground2,
  },
  timeInfo: {
    display: 'flex',
    justifyContent: 'space-between',
    width: '100%',
    gap: tokens.spacingHorizontalL,
  },
  timeItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    flex: 1,
  },
  sceneList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    width: '100%',
  },
  sceneItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  sceneIcon: {
    fontSize: '20px',
  },
  successSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalXL,
    alignItems: 'center',
  },
  successIcon: {
    fontSize: '64px',
    color: tokens.colorPaletteGreenForeground1,
  },
  errorSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalXL,
    alignItems: 'center',
  },
  errorIcon: {
    fontSize: '64px',
    color: tokens.colorPaletteRedForeground1,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
  },
  logViewer: {
    backgroundColor: tokens.colorNeutralBackground3,
    padding: tokens.spacingVerticalM,
    borderRadius: tokens.borderRadiusMedium,
    fontFamily: 'monospace',
    fontSize: '12px',
    maxHeight: '200px',
    overflowY: 'auto',
    whiteSpace: 'pre-wrap',
    wordBreak: 'break-all',
  },
});

export interface ExportProgressProps {
  open: boolean;
  status: 'rendering' | 'complete' | 'failed';
  progress: number;
  currentStage: string;
  currentScene?: number;
  totalScenes?: number;
  sceneProgress?: number;
  timeElapsed: number;
  timeRemaining: number;
  encodingSpeed?: number;
  realtimeMultiplier?: number;
  outputPath?: string;
  fileSize?: number;
  renderTime?: number;
  errorMessage?: string;
  ffmpegLog?: string;
  onClose: () => void;
  onMinimize?: () => void;
  onOpenFile?: () => void;
  onOpenLocation?: () => void;
  onRetry?: () => void;
  onExportAnother?: () => void;
}

export function ExportProgress({
  open,
  status,
  progress,
  currentStage,
  currentScene,
  totalScenes = 10,
  sceneProgress = 0,
  timeElapsed,
  timeRemaining,
  encodingSpeed = 0,
  realtimeMultiplier = 0,
  outputPath,
  fileSize,
  renderTime,
  errorMessage,
  ffmpegLog,
  onClose,
  onMinimize,
  onOpenFile,
  onOpenLocation,
  onRetry,
  onExportAnother,
}: ExportProgressProps) {
  const styles = useStyles();

  const formatTime = (seconds: number) => {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  const formatFileSize = (bytes: number) => {
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
  };

  const renderSceneProgress = () => {
    if (!currentScene || !totalScenes) return null;

    const scenes = Array.from({ length: totalScenes }, (_, i) => {
      const sceneNum = i + 1;
      let icon: React.ReactNode;
      let label: string;

      if (sceneNum < (currentScene || 0)) {
        icon = <CheckmarkCircle24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />;
        label = 'Complete';
      } else if (sceneNum === currentScene) {
        icon = (
          <div
            className={styles.progressCircle}
            style={{ width: '20px', height: '20px', border: '3px solid' }}
          />
        );
        label = `${sceneProgress}%`;
      } else {
        icon = (
          <div
            style={{
              width: '20px',
              height: '20px',
              borderRadius: '50%',
              backgroundColor: tokens.colorNeutralStroke2,
            }}
          />
        );
        label = 'Pending';
      }

      return (
        <div key={sceneNum} className={styles.sceneItem}>
          <div className={styles.sceneIcon}>{icon}</div>
          <Text>Scene {sceneNum}</Text>
          <Caption1 style={{ marginLeft: 'auto' }}>{label}</Caption1>
        </div>
      );
    });

    return scenes;
  };

  if (status === 'rendering') {
    return (
      <Dialog open={open} modalType="non-modal">
        <DialogSurface className={styles.dialogSurface}>
          <DialogBody>
            <DialogTitle
              action={
                <div style={{ display: 'flex', gap: tokens.spacingHorizontalS }}>
                  {onMinimize && (
                    <Button
                      appearance="subtle"
                      aria-label="minimize"
                      icon={<ArrowMinimize24Regular />}
                      onClick={onMinimize}
                    />
                  )}
                  <Button
                    appearance="subtle"
                    aria-label="cancel"
                    icon={<Dismiss24Regular />}
                    onClick={onClose}
                  />
                </div>
              }
            >
              Exporting Video
            </DialogTitle>
            <DialogContent>
              <div className={styles.progressSection}>
                <div className={styles.progressCircle}>
                  <div className={styles.progressText}>{progress}%</div>
                </div>

                <Body1Strong className={styles.stageText}>{currentStage}</Body1Strong>

                <ProgressBar value={progress} max={100} style={{ width: '100%' }} />

                <div className={styles.timeInfo}>
                  <div className={styles.timeItem}>
                    <Caption1>Time Elapsed</Caption1>
                    <Body1>{formatTime(timeElapsed)}</Body1>
                  </div>
                  <div className={styles.timeItem}>
                    <Caption1>Remaining</Caption1>
                    <Body1>{formatTime(timeRemaining)}</Body1>
                  </div>
                  {encodingSpeed > 0 && (
                    <div className={styles.timeItem}>
                      <Caption1>Speed</Caption1>
                      <Body1>{encodingSpeed.toFixed(1)} FPS</Body1>
                      {realtimeMultiplier > 0 && (
                        <Caption1>{realtimeMultiplier.toFixed(1)}x realtime</Caption1>
                      )}
                    </div>
                  )}
                </div>

                <Divider />

                {currentScene && totalScenes && (
                  <Accordion collapsible>
                    <AccordionItem value="scenes">
                      <AccordionHeader>
                        Scene Progress ({currentScene}/{totalScenes})
                      </AccordionHeader>
                      <AccordionPanel>
                        <div className={styles.sceneList}>{renderSceneProgress()}</div>
                      </AccordionPanel>
                    </AccordionItem>
                  </Accordion>
                )}

                <Button appearance="secondary" onClick={onClose}>
                  Cancel
                </Button>
              </div>
            </DialogContent>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    );
  }

  if (status === 'complete') {
    return (
      <Dialog open={open}>
        <DialogSurface className={styles.dialogSurface}>
          <DialogBody>
            <DialogTitle
              action={
                <Button
                  appearance="subtle"
                  aria-label="close"
                  icon={<Dismiss24Regular />}
                  onClick={onClose}
                />
              }
            >
              Export Complete
            </DialogTitle>
            <DialogContent>
              <div className={styles.successSection}>
                <CheckmarkCircle24Regular className={styles.successIcon} />
                <Body1Strong>Video exported successfully!</Body1Strong>

                <div
                  style={{
                    display: 'flex',
                    flexDirection: 'column',
                    gap: tokens.spacingVerticalS,
                    width: '100%',
                  }}
                >
                  {fileSize && <Caption1>File size: {formatFileSize(fileSize)}</Caption1>}
                  {renderTime && <Caption1>Render time: {formatTime(renderTime)}</Caption1>}
                  {outputPath && <Caption1>Output: {outputPath}</Caption1>}
                </div>

                <div className={styles.actions}>
                  {onOpenLocation && (
                    <Button
                      appearance="secondary"
                      icon={<FolderOpen24Regular />}
                      onClick={onOpenLocation}
                    >
                      Open Location
                    </Button>
                  )}
                  {onOpenFile && (
                    <Button
                      appearance="secondary"
                      icon={<ArrowExport24Regular />}
                      onClick={onOpenFile}
                    >
                      Open File
                    </Button>
                  )}
                  {onExportAnother && (
                    <Button appearance="primary" onClick={onExportAnother}>
                      Export Another
                    </Button>
                  )}
                  <Button appearance="secondary" onClick={onClose}>
                    Close
                  </Button>
                </div>
              </div>
            </DialogContent>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    );
  }

  if (status === 'failed') {
    return (
      <Dialog open={open}>
        <DialogSurface className={styles.dialogSurface}>
          <DialogBody>
            <DialogTitle
              action={
                <Button
                  appearance="subtle"
                  aria-label="close"
                  icon={<Dismiss24Regular />}
                  onClick={onClose}
                />
              }
            >
              Export Failed
            </DialogTitle>
            <DialogContent>
              <div className={styles.errorSection}>
                <ErrorCircle24Regular className={styles.errorIcon} />
                <Body1Strong>Export failed</Body1Strong>
                {errorMessage && <Text>{errorMessage}</Text>}

                {ffmpegLog && (
                  <Accordion collapsible style={{ width: '100%' }}>
                    <AccordionItem value="log">
                      <AccordionHeader>Show FFmpeg Log</AccordionHeader>
                      <AccordionPanel>
                        <div className={styles.logViewer}>{ffmpegLog}</div>
                      </AccordionPanel>
                    </AccordionItem>
                  </Accordion>
                )}

                <div className={styles.actions}>
                  {onRetry && (
                    <Button
                      appearance="primary"
                      icon={<ArrowRepeatAll24Regular />}
                      onClick={onRetry}
                    >
                      Retry
                    </Button>
                  )}
                  <Button appearance="secondary" onClick={onClose}>
                    Close
                  </Button>
                </div>
              </div>
            </DialogContent>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    );
  }

  return null;
}
