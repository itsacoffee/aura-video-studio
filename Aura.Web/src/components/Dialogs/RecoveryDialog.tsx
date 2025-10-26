/**
 * Recovery Dialog Component
 * Provides recovery flows for common failures
 */

import {
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Button,
  Text,
  Caption1,
  makeStyles,
  tokens,
  Spinner,
} from '@fluentui/react-components';
import {
  Warning24Regular,
  ArrowClockwise24Regular,
  Folder24Regular,
  DocumentSave24Regular,
} from '@fluentui/react-icons';
import { useState } from 'react';
import { autoSaveService } from '../../services/autoSaveService';
import { loggingService } from '../../services/loggingService';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  warningSection: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteYellowBackground2,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `4px solid ${tokens.colorPaletteYellowBorder2}`,
  },
  warningIcon: {
    color: tokens.colorPaletteYellowForeground1,
    flexShrink: 0,
  },
  warningText: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  versionList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    maxHeight: '200px',
    overflowY: 'auto',
  },
  versionItem: {
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    cursor: 'pointer',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground3Hover,
    },
  },
  versionItemSelected: {
    backgroundColor: tokens.colorBrandBackground2,
    ':hover': {
      backgroundColor: tokens.colorBrandBackground2Hover,
    },
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
});

export type RecoveryType =
  | 'missing-media'
  | 'corrupted-project'
  | 'out-of-memory'
  | 'auto-save-recovery';

export interface RecoveryDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  type: RecoveryType;
  onRecover?: (recoveryData?: unknown) => void;
}

export function RecoveryDialog({ open, onOpenChange, type, onRecover }: RecoveryDialogProps) {
  const styles = useStyles();
  const [isRecovering, setIsRecovering] = useState(false);
  const [selectedVersion, setSelectedVersion] = useState<number | null>(null);
  const versions = autoSaveService.getVersions();

  const getTitle = () => {
    switch (type) {
      case 'missing-media':
        return 'Missing Media Files';
      case 'corrupted-project':
        return 'Corrupted Project';
      case 'out-of-memory':
        return 'Low Memory Warning';
      case 'auto-save-recovery':
        return 'Recover Unsaved Changes';
      default:
        return 'Recovery Options';
    }
  };

  const getMessage = () => {
    switch (type) {
      case 'missing-media':
        return 'Some media files in your project could not be found. You can locate the missing files or continue without them.';
      case 'corrupted-project':
        return 'The project file appears to be corrupted. You can try to recover from a previous version.';
      case 'out-of-memory':
        return 'The application is running low on memory. This may cause performance issues or crashes.';
      case 'auto-save-recovery':
        return 'We found auto-saved versions of your project. Would you like to recover your work?';
      default:
        return 'An issue was detected. Please choose a recovery option.';
    }
  };

  const getSuggestion = () => {
    switch (type) {
      case 'missing-media':
        return 'Locate the missing files in their original location, or remove them from the project.';
      case 'corrupted-project':
        return 'Select a backup version below to restore your project.';
      case 'out-of-memory':
        return 'Reduce preview quality, close unused clips, or reload the application.';
      case 'auto-save-recovery':
        return 'Select a version below to restore your work.';
      default:
        return 'Choose an option below to continue.';
    }
  };

  const handleLocateFiles = () => {
    loggingService.info('User attempting to locate missing files', 'RecoveryDialog', 'locateFiles');
    // Trigger file picker or navigation to locate files
    if (onRecover) {
      onRecover({ action: 'locate' });
    }
    onOpenChange(false);
  };

  const handleReduceQuality = async () => {
    setIsRecovering(true);
    loggingService.info('User reducing preview quality', 'RecoveryDialog', 'reduceQuality');

    // Simulate quality reduction
    await new Promise((resolve) => setTimeout(resolve, 1000));

    if (onRecover) {
      onRecover({ action: 'reduce-quality' });
    }
    setIsRecovering(false);
    onOpenChange(false);
  };

  const handleRestoreVersion = async () => {
    if (selectedVersion === null) return;

    setIsRecovering(true);
    loggingService.info(
      `User restoring version ${selectedVersion}`,
      'RecoveryDialog',
      'restoreVersion'
    );

    const version = autoSaveService.getVersion(selectedVersion);
    if (version && onRecover) {
      await new Promise((resolve) => setTimeout(resolve, 500));
      onRecover({ action: 'restore', version: version.projectState });
    }

    setIsRecovering(false);
    onOpenChange(false);
  };

  const handleContinueWithout = () => {
    loggingService.info('User continuing without recovery', 'RecoveryDialog', 'continueWithout');
    if (onRecover) {
      onRecover({ action: 'skip' });
    }
    onOpenChange(false);
  };

  const renderActions = () => {
    switch (type) {
      case 'missing-media':
        return (
          <>
            <Button
              appearance="primary"
              icon={<Folder24Regular />}
              onClick={handleLocateFiles}
              disabled={isRecovering}
            >
              Locate Files
            </Button>
            <Button appearance="secondary" onClick={handleContinueWithout} disabled={isRecovering}>
              Continue Without Files
            </Button>
            <Button
              appearance="secondary"
              onClick={() => onOpenChange(false)}
              disabled={isRecovering}
            >
              Cancel
            </Button>
          </>
        );
      case 'corrupted-project':
      case 'auto-save-recovery':
        return (
          <>
            <Button
              appearance="primary"
              icon={<DocumentSave24Regular />}
              onClick={handleRestoreVersion}
              disabled={isRecovering || selectedVersion === null}
            >
              {isRecovering ? <Spinner size="tiny" /> : null}
              Restore Selected
            </Button>
            <Button appearance="secondary" onClick={handleContinueWithout} disabled={isRecovering}>
              Discard Changes
            </Button>
            <Button
              appearance="secondary"
              onClick={() => onOpenChange(false)}
              disabled={isRecovering}
            >
              Cancel
            </Button>
          </>
        );
      case 'out-of-memory':
        return (
          <>
            <Button
              appearance="primary"
              icon={<ArrowClockwise24Regular />}
              onClick={handleReduceQuality}
              disabled={isRecovering}
            >
              {isRecovering ? <Spinner size="tiny" /> : null}
              Reduce Quality
            </Button>
            <Button appearance="secondary" onClick={() => window.location.reload()}>
              Reload Page
            </Button>
            <Button
              appearance="secondary"
              onClick={() => onOpenChange(false)}
              disabled={isRecovering}
            >
              Cancel
            </Button>
          </>
        );
      default:
        return (
          <Button appearance="primary" onClick={() => onOpenChange(false)}>
            Close
          </Button>
        );
    }
  };

  return (
    <Dialog open={open} onOpenChange={(_, data) => onOpenChange(data.open)}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>{getTitle()}</DialogTitle>
          <DialogContent className={styles.content}>
            <div className={styles.warningSection}>
              <Warning24Regular className={styles.warningIcon} />
              <div className={styles.warningText}>
                <Text weight="semibold">{getMessage()}</Text>
                <Caption1>{getSuggestion()}</Caption1>
              </div>
            </div>

            {(type === 'corrupted-project' || type === 'auto-save-recovery') &&
              versions.length > 0 && (
                <div>
                  <Text weight="semibold" style={{ marginBottom: '0.5rem', display: 'block' }}>
                    Available Versions:
                  </Text>
                  <div className={styles.versionList}>
                    {versions.map((version) => (
                      <div
                        key={version.version}
                        className={`${styles.versionItem} ${
                          selectedVersion === version.version ? styles.versionItemSelected : ''
                        }`}
                        role="button"
                        tabIndex={0}
                        onClick={() => setSelectedVersion(version.version)}
                        onKeyDown={(e) => {
                          if (e.key === 'Enter' || e.key === ' ') {
                            e.preventDefault();
                            setSelectedVersion(version.version);
                          }
                        }}
                      >
                        <Text weight="semibold">Version {version.version}</Text>
                        <Caption1>Saved: {new Date(version.timestamp).toLocaleString()}</Caption1>
                        <Caption1>
                          Clips: {version.projectState.clips.length} | Tracks:{' '}
                          {version.projectState.tracks.length}
                        </Caption1>
                      </div>
                    ))}
                  </div>
                </div>
              )}
          </DialogContent>
          <DialogActions className={styles.actions}>{renderActions()}</DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
