import {
  Card,
  CardHeader,
  Button,
  Text,
  Badge,
  Spinner,
  makeStyles,
  tokens,
  MessageBar,
  MessageBarBody,
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  DialogActions,
  Input,
  Label,
} from '@fluentui/react-components';
import {
  ArrowDownload24Regular,
  Checkmark24Regular,
  ArrowSync24Regular,
  Folder24Regular,
  Link24Regular,
  Document24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import { apiUrl } from '../../config/api';
import { useNotifications } from '../Notifications/Toasts';
import { ManualInstallationModal } from './ManualInstallationModal';

const useStyles = makeStyles({
  card: {
    width: '100%',
    maxWidth: '800px',
    marginBottom: tokens.spacingVerticalM,
  },
  content: {
    padding: tokens.spacingVerticalM,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  row: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    flexWrap: 'wrap',
  },
  metadata: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
    fontFamily: 'monospace',
  },
  pathDisplay: {
    backgroundColor: tokens.colorNeutralBackground3,
    padding: tokens.spacingVerticalS,
    borderRadius: tokens.borderRadiusMedium,
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase200,
    wordBreak: 'break-all',
  },
});

interface FFmpegStatus {
  state: string;
  ffmpegPath?: string;
  version?: string;
  validationOutput?: string;
}

export function FFmpegCard() {
  const styles = useStyles();
  const { showSuccessToast, showFailureToast } = useNotifications();
  const [status, setStatus] = useState<FFmpegStatus | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isProcessing, setIsProcessing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showAttachDialog, setShowAttachDialog] = useState(false);
  const [showManualModal, setShowManualModal] = useState(false);
  const [attachPath, setAttachPath] = useState('');

  useEffect(() => {
    loadStatus();
  }, []);

  const loadStatus = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const response = await fetch(apiUrl('/api/downloads/ffmpeg/status'));
      if (response.ok) {
        const data = await response.json();
        setStatus(data);
      } else {
        throw new Error('Failed to fetch FFmpeg status');
      }
    } catch (err) {
      console.error('Failed to load FFmpeg status:', err);
      setError(err instanceof Error ? err.message : 'Failed to load status');
    } finally {
      setIsLoading(false);
    }
  };

  const handleInstall = async () => {
    setIsProcessing(true);
    setError(null);
    try {
      const response = await fetch(apiUrl('/api/downloads/ffmpeg/install'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ mode: 'managed' }),
      });

      if (response.ok) {
        await loadStatus();
        showSuccessToast({
          title: 'FFmpeg Installed',
          message: 'FFmpeg installed successfully!',
        });
      } else {
        const errorData = await response.json();
        throw new Error(errorData.error || 'Installation failed');
      }
    } catch (err) {
      console.error('FFmpeg installation failed:', err);
      setError(err instanceof Error ? err.message : 'Installation failed');
      showFailureToast({
        title: 'Installation Failed',
        message: `Installation failed: ${err instanceof Error ? err.message : 'Unknown error'}`,
      });
    } finally {
      setIsProcessing(false);
    }
  };

  const handleRescan = async () => {
    setIsProcessing(true);
    setError(null);
    try {
      const response = await fetch(apiUrl('/api/downloads/ffmpeg/rescan'), {
        method: 'POST',
      });

      if (response.ok) {
        const data = await response.json();
        if (data.found) {
          await loadStatus();
          showSuccessToast({
            title: 'FFmpeg Found',
            message: `FFmpeg found and registered!\nPath: ${data.ffmpegPath}\nVersion: ${data.versionString}`,
          });
        } else {
          showFailureToast({
            title: 'FFmpeg Not Found',
            message: `FFmpeg not found in standard locations.\n\nAttempted paths:\n${data.attemptedPaths?.join('\n')}\n\nUse "Attach Existing" to specify a custom path.`,
          });
        }
      } else {
        throw new Error('Rescan failed');
      }
    } catch (err) {
      console.error('FFmpeg rescan failed:', err);
      setError(err instanceof Error ? err.message : 'Rescan failed');
    } finally {
      setIsProcessing(false);
    }
  };

  const handleAttach = async () => {
    if (!attachPath.trim()) {
      showFailureToast({
        title: 'Path Required',
        message: 'Please enter a path',
      });
      return;
    }

    setIsProcessing(true);
    setShowAttachDialog(false);
    setError(null);

    try {
      const response = await fetch(apiUrl('/api/downloads/ffmpeg/attach'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ path: attachPath.trim() }),
      });

      if (response.ok) {
        const data = await response.json();
        await loadStatus();
        showSuccessToast({
          title: 'FFmpeg Attached',
          message: `FFmpeg attached successfully!\nPath: ${data.ffmpegPath}\nVersion: ${data.versionString}`,
        });
        setAttachPath('');
      } else {
        const errorData = await response.json();
        throw new Error(errorData.error || 'Failed to attach FFmpeg');
      }
    } catch (err) {
      console.error('FFmpeg attach failed:', err);
      setError(err instanceof Error ? err.message : 'Attach failed');
      showFailureToast({
        title: 'Attach Failed',
        message: `Failed to attach: ${err instanceof Error ? err.message : 'Unknown error'}`,
      });
    } finally {
      setIsProcessing(false);
    }
  };

  const handleOpenFolder = async () => {
    if (!status?.ffmpegPath) {
      showFailureToast({
        title: 'Path Not Available',
        message: 'FFmpeg path not available',
      });
      return;
    }

    try {
      const response = await fetch(apiUrl('/api/engines/open-folder'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ engineId: 'ffmpeg' }),
      });

      if (!response.ok) {
        // Fallback: just show the path
        showSuccessToast({
          title: 'FFmpeg Location',
          message: `FFmpeg location: ${status.ffmpegPath}`,
        });
      }
    } catch (err) {
      console.error('Failed to open folder:', err);
      showSuccessToast({
        title: 'FFmpeg Location',
        message: `FFmpeg location: ${status.ffmpegPath}`,
      });
    }
  };

  const getStatusBadge = () => {
    if (isLoading) return null;

    switch (status?.state) {
      case 'Installed':
      case 'ExternalAttached':
        return (
          <Badge appearance="filled" color="success">
            Installed
          </Badge>
        );
      case 'NotInstalled':
        return <Badge appearance="outline">Not Installed</Badge>;
      case 'PartiallyFailed':
        return (
          <Badge appearance="filled" color="warning">
            Needs Attention
          </Badge>
        );
      default:
        return <Badge appearance="outline">Unknown</Badge>;
    }
  };

  const isInstalled = status?.state === 'Installed' || status?.state === 'ExternalAttached';

  return (
    <>
      <Card className={styles.card}>
        <CardHeader
          header={
            <div className={styles.row}>
              <div
                style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}
              >
                <Text weight="semibold" size={500}>
                  FFmpeg
                </Text>
                {getStatusBadge()}
              </div>
            </div>
          }
          description={
            <div>
              <Text className={styles.metadata}>Required for video rendering and processing</Text>
            </div>
          }
        />
        <div className={styles.content}>
          {error && (
            <MessageBar intent="error">
              <MessageBarBody>{error}</MessageBarBody>
            </MessageBar>
          )}

          {isLoading ? (
            <Spinner label="Loading FFmpeg status..." size="small" />
          ) : (
            <>
              {isInstalled && status?.ffmpegPath && (
                <div>
                  <Text
                    size={300}
                    weight="semibold"
                    block
                    style={{ marginBottom: tokens.spacingVerticalXXS }}
                  >
                    Path:
                  </Text>
                  <div className={styles.pathDisplay}>{status.ffmpegPath}</div>
                  {status.version && (
                    <Text
                      className={styles.metadata}
                      block
                      style={{ marginTop: tokens.spacingVerticalXXS }}
                    >
                      Version: {status.version}
                    </Text>
                  )}
                </div>
              )}

              <div className={styles.row}>
                <div className={styles.actions}>
                  {!isInstalled && (
                    <>
                      <Button
                        appearance="primary"
                        icon={<ArrowDownload24Regular />}
                        onClick={handleInstall}
                        disabled={isProcessing}
                      >
                        {isProcessing ? <Spinner size="tiny" /> : 'Install'}
                      </Button>
                      <Button
                        appearance="secondary"
                        icon={<Link24Regular />}
                        onClick={() => setShowAttachDialog(true)}
                        disabled={isProcessing}
                      >
                        Attach Existing...
                      </Button>
                      <Button
                        appearance="secondary"
                        icon={<Document24Regular />}
                        onClick={() => setShowManualModal(true)}
                        disabled={isProcessing}
                      >
                        Manual Install Guide
                      </Button>
                    </>
                  )}

                  <Button
                    appearance={isInstalled ? 'secondary' : 'primary'}
                    icon={<ArrowSync24Regular />}
                    onClick={handleRescan}
                    disabled={isProcessing}
                    title="Rescan for FFmpeg in standard locations"
                  >
                    {isProcessing ? <Spinner size="tiny" /> : 'Rescan'}
                  </Button>

                  {isInstalled && (
                    <>
                      <Button
                        appearance="subtle"
                        icon={<Folder24Regular />}
                        onClick={handleOpenFolder}
                      >
                        Open Folder
                      </Button>
                      <Button
                        appearance="subtle"
                        icon={<Checkmark24Regular />}
                        onClick={handleInstall}
                        disabled={isProcessing}
                        title="Reinstall/repair FFmpeg"
                      >
                        Repair
                      </Button>
                    </>
                  )}
                </div>
              </div>
            </>
          )}
        </div>
      </Card>

      <Dialog open={showAttachDialog} onOpenChange={(_, data) => setShowAttachDialog(data.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>Attach Existing FFmpeg</DialogTitle>
            <DialogContent>
              <Label htmlFor="ffmpeg-path">FFmpeg Path (file or folder):</Label>
              <Input
                id="ffmpeg-path"
                value={attachPath}
                onChange={(_, data) => setAttachPath(data.value)}
                placeholder="C:\ffmpeg\bin\ffmpeg.exe or C:\ffmpeg"
                style={{ width: '100%' }}
              />
              <Text
                size={200}
                style={{
                  marginTop: tokens.spacingVerticalS,
                  color: tokens.colorNeutralForeground3,
                }}
              >
                Provide the path to ffmpeg.exe (or ffmpeg binary on Linux/Mac), or the folder
                containing it.
              </Text>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setShowAttachDialog(false)}>
                Cancel
              </Button>
              <Button appearance="primary" onClick={handleAttach}>
                Attach
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>

      <ManualInstallationModal
        open={showManualModal}
        onClose={() => setShowManualModal(false)}
        onVerify={handleRescan}
      />
    </>
  );
}
