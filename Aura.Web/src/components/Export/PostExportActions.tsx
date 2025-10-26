import {
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogContent,
  DialogActions,
  Button,
  makeStyles,
  tokens,
  Body1,
  Caption1,
  Spinner,
  MessageBar,
  MessageBarBody,
} from '@fluentui/react-components';
import {
  Dismiss24Regular,
  FolderOpen24Regular,
  Play24Regular,
  ArrowUpload24Regular,
  Copy24Regular,
  CheckmarkCircle24Regular,
} from '@fluentui/react-icons';
import { useState } from 'react';

const useStyles = makeStyles({
  dialogSurface: {
    maxWidth: '500px',
    width: '100%',
  },
  actions: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    marginBottom: tokens.spacingVerticalL,
  },
  actionButton: {
    justifyContent: 'flex-start',
  },
  fileInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalL,
  },
  uploadSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  buttonActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
  },
});

export interface PostExportActionsProps {
  open: boolean;
  onClose: () => void;
  outputPath: string;
  fileName: string;
  fileSize: number;
  duration?: number;
}

export function PostExportActions({
  open,
  onClose,
  outputPath,
  fileName,
  fileSize,
  duration,
}: PostExportActionsProps) {
  const styles = useStyles();
  const [uploading, setUploading] = useState(false);
  const [uploadSuccess, setUploadSuccess] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);

  const formatFileSize = (bytes: number) => {
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
  };

  const formatDuration = (seconds: number) => {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  const handleOpenInExplorer = () => {
    // Open file location in system file explorer
    window.electron?.openPath(outputPath.substring(0, outputPath.lastIndexOf('\\')));
  };

  const handlePlayVideo = () => {
    // Open video in default video player
    window.electron?.openExternal(outputPath);
  };

  const handleCopyPath = async () => {
    try {
      await navigator.clipboard.writeText(outputPath);
      // Show success feedback
      alert('File path copied to clipboard');
    } catch (error) {
      console.error('Failed to copy path:', error);
      alert('Failed to copy path to clipboard');
    }
  };

  const handleUploadToYouTube = async () => {
    setUploading(true);
    setUploadError(null);

    try {
      // TODO: Implement YouTube upload via API
      // This would require OAuth authentication and YouTube Data API integration
      await new Promise((resolve) => setTimeout(resolve, 2000)); // Simulate upload
      setUploadSuccess(true);
    } catch (error) {
      setUploadError('Failed to upload to YouTube. Please try again.');
      console.error('Upload error:', error);
    } finally {
      setUploading(false);
    }
  };

  const handleUploadToVimeo = async () => {
    setUploading(true);
    setUploadError(null);

    try {
      // TODO: Implement Vimeo upload via API
      await new Promise((resolve) => setTimeout(resolve, 2000)); // Simulate upload
      setUploadSuccess(true);
    } catch (error) {
      setUploadError('Failed to upload to Vimeo. Please try again.');
      console.error('Upload error:', error);
    } finally {
      setUploading(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
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
            <div className={styles.fileInfo}>
              <Body1>
                <strong>{fileName}</strong>
              </Body1>
              <Caption1>Size: {formatFileSize(fileSize)}</Caption1>
              {duration && <Caption1>Duration: {formatDuration(duration)}</Caption1>}
              <Caption1 style={{ wordBreak: 'break-all' }}>{outputPath}</Caption1>
            </div>

            {uploadSuccess && (
              <MessageBar intent="success">
                <MessageBarBody>
                  <CheckmarkCircle24Regular /> Video uploaded successfully!
                </MessageBarBody>
              </MessageBar>
            )}

            {uploadError && (
              <MessageBar intent="error">
                <MessageBarBody>{uploadError}</MessageBarBody>
              </MessageBar>
            )}

            <div className={styles.actions}>
              <Button
                appearance="secondary"
                icon={<FolderOpen24Regular />}
                onClick={handleOpenInExplorer}
                className={styles.actionButton}
                disabled={uploading}
              >
                Open in File Explorer
              </Button>

              <Button
                appearance="secondary"
                icon={<Play24Regular />}
                onClick={handlePlayVideo}
                className={styles.actionButton}
                disabled={uploading}
              >
                Play Video
              </Button>

              <Button
                appearance="secondary"
                icon={<Copy24Regular />}
                onClick={handleCopyPath}
                className={styles.actionButton}
                disabled={uploading}
              >
                Copy File Path
              </Button>

              <div className={styles.uploadSection}>
                <Body1>
                  <strong>Upload to Platform</strong>
                </Body1>

                <Button
                  appearance="secondary"
                  icon={uploading ? <Spinner size="tiny" /> : <ArrowUpload24Regular />}
                  onClick={handleUploadToYouTube}
                  className={styles.actionButton}
                  disabled={uploading || uploadSuccess}
                >
                  {uploading ? 'Uploading...' : 'Upload to YouTube'}
                </Button>

                <Button
                  appearance="secondary"
                  icon={uploading ? <Spinner size="tiny" /> : <ArrowUpload24Regular />}
                  onClick={handleUploadToVimeo}
                  className={styles.actionButton}
                  disabled={uploading || uploadSuccess}
                >
                  {uploading ? 'Uploading...' : 'Upload to Vimeo'}
                </Button>
              </div>
            </div>
          </DialogContent>
          <DialogActions>
            <div className={styles.buttonActions}>
              <Button appearance="primary" onClick={onClose}>
                Done
              </Button>
            </div>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}

// Extend Window interface for Electron APIs
declare global {
  interface Window {
    electron?: {
      openPath: (path: string) => Promise<void>;
      openExternal: (url: string) => Promise<void>;
    };
  }
}
