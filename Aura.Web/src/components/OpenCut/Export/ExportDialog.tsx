/**
 * ExportDialog Component
 *
 * Main export configuration dialog with preset selector,
 * settings panel, preview of output settings, file size estimate,
 * and export button with progress.
 */

import {
  makeStyles,
  tokens,
  Text,
  Button,
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogContent,
  DialogActions,
  DialogTrigger,
  Divider,
  mergeClasses,
} from '@fluentui/react-components';
import { ArrowExportLtr24Regular, Dismiss24Regular } from '@fluentui/react-icons';
import { useState, useCallback, useMemo } from 'react';
import type { FC } from 'react';
import { useExportStore } from '../../../stores/opencutExport';
import { useOpenCutTimelineStore } from '../../../stores/opencutTimeline';
import { ExportProgress } from './ExportProgress';
import { ExportSettingsPanel } from './ExportSettings';
import { PresetSelector } from './PresetSelector';

export interface ExportDialogProps {
  className?: string;
  trigger?: React.ReactElement;
}

const useStyles = makeStyles({
  dialogSurface: {
    maxWidth: '900px',
    width: '90vw',
    maxHeight: '85vh',
  },
  dialogContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    overflow: 'auto',
  },
  twoColumnLayout: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingHorizontalL,
  },
  column: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  sectionTitle: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: 600,
    marginBottom: tokens.spacingVerticalXS,
  },
  summarySection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
  summaryRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  summaryLabel: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  summaryValue: {
    fontSize: tokens.fontSizeBase200,
    fontWeight: 600,
    color: tokens.colorNeutralForeground1,
  },
  fileSizeEstimate: {
    marginTop: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: tokens.borderRadiusSmall,
    textAlign: 'center',
  },
  fileSizeLabel: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  fileSizeValue: {
    fontSize: tokens.fontSizeBase500,
    fontWeight: 600,
    color: tokens.colorBrandForeground1,
  },
  progressContainer: {
    marginTop: tokens.spacingVerticalM,
  },
  closeButton: {
    position: 'absolute',
    top: tokens.spacingVerticalS,
    right: tokens.spacingHorizontalS,
  },
});

export const ExportDialog: FC<ExportDialogProps> = ({ className, trigger }) => {
  const styles = useStyles();
  const [isOpen, setIsOpen] = useState(false);
  const [showSavePresetDialog, setShowSavePresetDialog] = useState(false);
  const [newPresetName, setNewPresetName] = useState('');

  const {
    currentSettings,
    selectedPresetId,
    getPreset,
    isExporting,
    exportProgress,
    startExport,
    estimateFileSize,
    createCustomPreset,
  } = useExportStore();

  const { getTotalDuration } = useOpenCutTimelineStore();

  const selectedPreset = useMemo(() => {
    return selectedPresetId ? getPreset(selectedPresetId) : undefined;
  }, [selectedPresetId, getPreset]);

  const duration = useMemo(() => getTotalDuration(), [getTotalDuration]);

  const estimatedSize = useMemo(() => {
    return estimateFileSize(duration);
  }, [estimateFileSize, duration]);

  const handleExport = useCallback(async () => {
    await startExport();
  }, [startExport]);

  const _handleClose = useCallback(() => {
    if (!isExporting) {
      setIsOpen(false);
    }
  }, [isExporting]);

  const handleSaveAsPreset = useCallback(() => {
    setShowSavePresetDialog(true);
  }, []);

  const handleConfirmSavePreset = useCallback(() => {
    if (newPresetName.trim() && currentSettings) {
      createCustomPreset(newPresetName.trim(), 'Custom', currentSettings);
      setShowSavePresetDialog(false);
      setNewPresetName('');
    }
  }, [newPresetName, currentSettings, createCustomPreset]);

  const handleExportComplete = useCallback(() => {
    setIsOpen(false);
  }, []);

  const formatResolution = () => {
    if (!currentSettings) return '-';
    const { width, height } = currentSettings.resolution;
    return `${width} × ${height}`;
  };

  const formatBitrate = () => {
    if (!currentSettings) return '-';
    return `${(currentSettings.videoBitrate / 1000).toFixed(1)} Mbps`;
  };

  const formatFileSize = (sizeMB: number): string => {
    if (sizeMB < 1) {
      return `${Math.round(sizeMB * 1024)} KB`;
    }
    if (sizeMB >= 1024) {
      return `${(sizeMB / 1024).toFixed(2)} GB`;
    }
    return `${sizeMB.toFixed(1)} MB`;
  };

  const isComplete = exportProgress >= 100 && !isExporting;

  return (
    <>
      <Dialog open={isOpen} onOpenChange={(_, data) => setIsOpen(data.open)}>
        <DialogTrigger disableButtonEnhancement>
          {trigger ?? (
            <Button appearance="primary" icon={<ArrowExportLtr24Regular />}>
              Export
            </Button>
          )}
        </DialogTrigger>
        <DialogSurface className={mergeClasses(styles.dialogSurface, className)}>
          <DialogTitle>
            Export Video
            <DialogTrigger action="close" disableButtonEnhancement>
              <Button
                className={styles.closeButton}
                appearance="subtle"
                icon={<Dismiss24Regular />}
                aria-label="Close"
                disabled={isExporting}
              />
            </DialogTrigger>
          </DialogTitle>
          <DialogBody>
            <DialogContent className={styles.dialogContent}>
              {isExporting || isComplete ? (
                <ExportProgress
                  className={styles.progressContainer}
                  onComplete={handleExportComplete}
                />
              ) : (
                <>
                  {/* Preset Selection */}
                  <div>
                    <Text className={styles.sectionTitle}>Choose a Preset</Text>
                    <PresetSelector />
                  </div>

                  <Divider />

                  {/* Two-column layout for settings and summary */}
                  <div className={styles.twoColumnLayout}>
                    {/* Settings Column */}
                    <div className={styles.column}>
                      <Text className={styles.sectionTitle}>Export Settings</Text>
                      <ExportSettingsPanel onSaveAsPreset={handleSaveAsPreset} />
                    </div>

                    {/* Summary Column */}
                    <div className={styles.column}>
                      <Text className={styles.sectionTitle}>Output Summary</Text>
                      <div className={styles.summarySection}>
                        <div className={styles.summaryRow}>
                          <Text className={styles.summaryLabel}>Preset</Text>
                          <Text className={styles.summaryValue}>
                            {selectedPreset?.name ?? 'Custom'}
                          </Text>
                        </div>
                        <div className={styles.summaryRow}>
                          <Text className={styles.summaryLabel}>Format</Text>
                          <Text className={styles.summaryValue}>
                            {currentSettings?.format.toUpperCase() ?? '-'}
                          </Text>
                        </div>
                        <div className={styles.summaryRow}>
                          <Text className={styles.summaryLabel}>Resolution</Text>
                          <Text className={styles.summaryValue}>{formatResolution()}</Text>
                        </div>
                        <div className={styles.summaryRow}>
                          <Text className={styles.summaryLabel}>Frame Rate</Text>
                          <Text className={styles.summaryValue}>
                            {currentSettings?.frameRate ?? '-'} fps
                          </Text>
                        </div>
                        <div className={styles.summaryRow}>
                          <Text className={styles.summaryLabel}>Video Codec</Text>
                          <Text className={styles.summaryValue}>
                            {currentSettings?.videoCodec.toUpperCase() ?? '-'}
                          </Text>
                        </div>
                        <div className={styles.summaryRow}>
                          <Text className={styles.summaryLabel}>Bitrate</Text>
                          <Text className={styles.summaryValue}>{formatBitrate()}</Text>
                        </div>
                        <div className={styles.summaryRow}>
                          <Text className={styles.summaryLabel}>Audio</Text>
                          <Text className={styles.summaryValue}>
                            {currentSettings?.includeAudio
                              ? `${currentSettings.audioCodec.toUpperCase()} ${currentSettings.audioBitrate}kbps`
                              : 'None'}
                          </Text>
                        </div>
                        <div className={styles.summaryRow}>
                          <Text className={styles.summaryLabel}>Duration</Text>
                          <Text className={styles.summaryValue}>{Math.round(duration)}s</Text>
                        </div>
                      </div>

                      <div className={styles.fileSizeEstimate}>
                        <Text className={styles.fileSizeLabel}>Estimated File Size</Text>
                        <Text className={styles.fileSizeValue} block>
                          {formatFileSize(estimatedSize)}
                        </Text>
                      </div>
                    </div>
                  </div>
                </>
              )}
            </DialogContent>
            {!isExporting && !isComplete && (
              <DialogActions>
                <DialogTrigger disableButtonEnhancement>
                  <Button appearance="secondary">Cancel</Button>
                </DialogTrigger>
                <Button
                  appearance="primary"
                  icon={<ArrowExportLtr24Regular />}
                  onClick={handleExport}
                  disabled={!currentSettings}
                >
                  Export Video
                </Button>
              </DialogActions>
            )}
          </DialogBody>
        </DialogSurface>
      </Dialog>

      {/* Save Preset Dialog */}
      <Dialog
        open={showSavePresetDialog}
        onOpenChange={(_, data) => setShowSavePresetDialog(data.open)}
      >
        <DialogSurface>
          <DialogTitle>Save Custom Preset</DialogTitle>
          <DialogBody>
            <DialogContent>
              <Text>Enter a name for your custom preset:</Text>
              <input
                type="text"
                value={newPresetName}
                onChange={(e) => setNewPresetName(e.target.value)}
                placeholder="My Custom Preset"
                style={{
                  width: '100%',
                  padding: '8px',
                  marginTop: '8px',
                  borderRadius: '4px',
                  border: `1px solid ${tokens.colorNeutralStroke1}`,
                  backgroundColor: tokens.colorNeutralBackground1,
                  color: tokens.colorNeutralForeground1,
                }}
              />
            </DialogContent>
            <DialogActions>
              <DialogTrigger disableButtonEnhancement>
                <Button appearance="secondary">Cancel</Button>
              </DialogTrigger>
              <Button
                appearance="primary"
                onClick={handleConfirmSavePreset}
                disabled={!newPresetName.trim()}
              >
                Save Preset
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </>
  );
};

export default ExportDialog;
