import {
  Badge,
  Button,
  Card,
  CardHeader,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Input,
  Label,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
  Spinner,
  Text,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import {
  ArrowDownload24Regular,
  ArrowSync24Regular,
  Checkmark24Regular,
  ChevronDown24Regular,
  ChevronUp24Regular,
  Document24Regular,
  Folder24Regular,
  Link24Regular,
} from '@fluentui/react-icons';
import { useCallback, useEffect, useState } from 'react';
import { useNotifications } from '../Notifications/Toasts';
import { ManualInstallationModal } from './ManualInstallationModal';
import { apiUrl } from '@/config/api';
import { handleApiError, type UserFriendlyError } from '@/services/api/errorHandler';
import {
  ffmpegClient,
  type FFmpegDirectCheckResponse,
  type FFmpegStatusExtended,
} from '@/services/api/ffmpegClient';

const useStyles = makeStyles({
  card: {
    width: '100%',
    maxWidth: '900px',
    marginBottom: tokens.spacingVerticalM,
  },
  content: {
    padding: tokens.spacingVerticalM,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
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
    fontFamily: tokens.fontFamilyMonospace,
  },
  pathDisplay: {
    backgroundColor: tokens.colorNeutralBackground3,
    padding: tokens.spacingVerticalS,
    borderRadius: tokens.borderRadiusMedium,
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
    wordBreak: 'break-all',
  },
  errorList: {
    marginTop: tokens.spacingVerticalS,
    paddingLeft: tokens.spacingHorizontalL,
  },
  technicalDetails: {
    border: `1px solid ${tokens.colorNeutralStroke3}`,
    borderRadius: tokens.borderRadiusMedium,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
  },
  candidateHeader: {
    display: 'grid',
    gridTemplateColumns: '140px 1fr 120px',
    gap: tokens.spacingHorizontalM,
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: tokens.spacingVerticalXS,
  },
  candidateRow: {
    display: 'grid',
    gridTemplateColumns: '140px 1fr 120px',
    gap: tokens.spacingHorizontalM,
    alignItems: 'center',
    padding: `${tokens.spacingVerticalXS} 0`,
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  candidatePath: {
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
    wordBreak: 'break-all',
  },
});

export function FFmpegCard() {
  const styles = useStyles();
  const { showSuccessToast, showFailureToast } = useNotifications();

  const [status, setStatus] = useState<FFmpegStatusExtended | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isProcessing, setIsProcessing] = useState(false);
  const [actionError, setActionError] = useState<UserFriendlyError | null>(null);

  const [showAttachDialog, setShowAttachDialog] = useState(false);
  const [showManualModal, setShowManualModal] = useState(false);
  const [attachPath, setAttachPath] = useState('');

  const [technicalDetailsOpen, setTechnicalDetailsOpen] = useState(false);
  const [directCheck, setDirectCheck] = useState<FFmpegDirectCheckResponse | null>(null);
  const [directCheckError, setDirectCheckError] = useState<UserFriendlyError | null>(null);
  const [directCheckLoading, setDirectCheckLoading] = useState(false);

  const loadStatus = useCallback(async () => {
    setIsLoading(true);
    setActionError(null);
    try {
      const data = await ffmpegClient.getStatusExtended();
      setStatus(data);
    } catch (error) {
      const friendly = handleApiError(error);
      setActionError(friendly);
      setStatus(null);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const loadDirectCheck = useCallback(async () => {
    setDirectCheckLoading(true);
    setDirectCheckError(null);
    try {
      const response = await ffmpegClient.directCheck();
      setDirectCheck(response);
    } catch (error) {
      setDirectCheckError(handleApiError(error));
    } finally {
      setDirectCheckLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadStatus();
  }, [loadStatus]);

  const applyResponseError = (title: string, message: string, correlationId?: string, howToFix?: string[]) => {
    const friendly: UserFriendlyError = {
      title,
      message,
      correlationId,
      howToFix,
    };
    setActionError(friendly);
    showFailureToast({
      title: friendly.title,
      message: friendly.message,
    });
  };

  const handleInstall = async () => {
    setIsProcessing(true);
    setActionError(null);
    try {
      const response = await ffmpegClient.install({ version: 'latest' });
      if (response.success) {
        showSuccessToast({
          title: 'FFmpeg Installed',
          message: response.message,
        });
        await loadStatus();
      } else {
        applyResponseError(
          response.title ?? 'FFmpeg Installation Failed',
          response.detail ?? response.message,
          response.correlationId,
          response.howToFix
        );
      }
    } catch (error) {
      const friendly = handleApiError(error);
      setActionError(friendly);
      showFailureToast({ title: friendly.title, message: friendly.message });
    } finally {
      setIsProcessing(false);
    }
  };

  const handleRescan = async () => {
    setIsProcessing(true);
    setActionError(null);
    try {
      const response = await ffmpegClient.rescan();
      if (response.success) {
        showSuccessToast({
          title: 'FFmpeg Found',
          message: response.message,
        });
        await loadStatus();
      } else {
        applyResponseError('FFmpeg Not Found', response.message, response.correlationId);
      }
    } catch (error) {
      const friendly = handleApiError(error);
      setActionError(friendly);
      showFailureToast({ title: friendly.title, message: friendly.message });
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
    setActionError(null);

    try {
      const response = await ffmpegClient.useExisting({ path: attachPath.trim() });
      if (response.success) {
        setAttachPath('');
        showSuccessToast({
          title: 'FFmpeg Attached',
          message: response.message,
        });
        await loadStatus();
      } else {
        applyResponseError(
          response.title ?? 'Attach Failed',
          response.detail ?? response.message,
          response.correlationId,
          response.howToFix
        );
      }
    } catch (error) {
      const friendly = handleApiError(error);
      setActionError(friendly);
      showFailureToast({ title: friendly.title, message: friendly.message });
    } finally {
      setIsProcessing(false);
    }
  };

  const handleOpenFolder = async () => {
    if (!status?.path) {
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
        showSuccessToast({
          title: 'FFmpeg Location',
          message: `FFmpeg location: ${status.path}`,
        });
      }
    } catch {
      showSuccessToast({
        title: 'FFmpeg Location',
        message: `FFmpeg location: ${status.path}`,
      });
    }
  };

  const toggleTechnicalDetails = () => {
    const next = !technicalDetailsOpen;
    setTechnicalDetailsOpen(next);
    if (next && !directCheck && !directCheckLoading) {
      void loadDirectCheck();
    }
  };

  const getStatusBadge = () => {
    if (isLoading) {
      return null;
    }

    if (!status) {
      return <Badge appearance="outline">Unknown</Badge>;
    }

    if (status.installed && status.valid && status.version && status.versionMeetsRequirement) {
      return (
        <Badge appearance="filled" color="success">
          Installed
        </Badge>
      );
    }

    if (status.installed && status.valid && status.version && !status.versionMeetsRequirement) {
      return (
        <Badge appearance="filled" color="warning">
          Outdated
        </Badge>
      );
    }

    if (status.installed && !status.valid) {
      return (
        <Badge appearance="filled" color="danger">
          Invalid
        </Badge>
      );
    }

    return <Badge appearance="outline">Not Installed</Badge>;
  };

  const isReady =
    status?.installed && status?.valid && status?.version && status?.versionMeetsRequirement;

  const renderActionError = () => {
    if (!actionError) {
      return null;
    }

    return (
      <MessageBar intent="error">
        <MessageBarBody>
          <MessageBarTitle>{actionError.title}</MessageBarTitle>
          <Text>{actionError.message}</Text>
          {actionError.correlationId && (
            <Text className={styles.metadata} block>
              Correlation ID: {actionError.correlationId}
            </Text>
          )}
          {actionError.howToFix && actionError.howToFix.length > 0 && (
            <ul className={styles.errorList}>
              {actionError.howToFix.map((tip, index) => (
                <li key={index}>
                  <Text size={200}>{tip}</Text>
                </li>
              ))}
            </ul>
          )}
        </MessageBarBody>
      </MessageBar>
    );
  };

  const renderTechnicalDetails = () => {
    if (!technicalDetailsOpen) {
      return null;
    }

    if (directCheckLoading) {
      return <Spinner label="Collecting diagnostics..." />;
    }

    if (directCheckError) {
      return (
        <MessageBar intent="warning">
          <MessageBarBody>
            <MessageBarTitle>{directCheckError.title}</MessageBarTitle>
            <Text>{directCheckError.message}</Text>
          </MessageBarBody>
        </MessageBar>
      );
    }

    if (!directCheck) {
      return null;
    }

    return (
      <div className={styles.technicalDetails}>
        <Text weight="semibold" size={300} block>
          Candidate Diagnostics
        </Text>
        <Text size={200} className={styles.metadata} block>
          Active Source: {directCheck.overall.source ?? 'Not detected'}
        </Text>
        <div className={styles.candidateHeader}>
          <Text size={200}>Source</Text>
          <Text size={200}>Path</Text>
          <Text size={200}>Status</Text>
        </div>
        {directCheck.candidates.map((candidate) => (
          <div key={`${candidate.label}-${candidate.path ?? 'none'}`} className={styles.candidateRow}>
            <Text size={200}>{candidate.label}</Text>
            <Text size={200} className={styles.candidatePath}>
              {candidate.path ?? 'Not configured'}
            </Text>
            {candidate.valid ? (
              <Badge appearance="filled" color="success">
                Valid
              </Badge>
            ) : candidate.exists ? (
              <Badge appearance="filled" color="danger">
                {candidate.error ?? 'Invalid'}
              </Badge>
            ) : (
              <Badge appearance="outline">Missing</Badge>
            )}
          </div>
        ))}
      </div>
    );
  };

  return (
    <>
      <Card className={styles.card}>
        <CardHeader
          header={
            <div className={styles.row}>
              <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
                <Text weight="semibold" size={500}>
                  FFmpeg
                </Text>
                {getStatusBadge()}
              </div>
              <Button
                appearance="subtle"
                icon={technicalDetailsOpen ? <ChevronUp24Regular /> : <ChevronDown24Regular />}
                onClick={toggleTechnicalDetails}
              >
                {technicalDetailsOpen ? 'Hide Technical Details' : 'Show Technical Details'}
              </Button>
            </div>
          }
          description={
            <Text className={styles.metadata}>Required for video rendering and processing</Text>
          }
        />
        <div className={styles.content}>
          {renderActionError()}
          {status?.error && !actionError && (
            <MessageBar intent="warning">
              <MessageBarBody>{status.error}</MessageBarBody>
            </MessageBar>
          )}

          {isLoading ? (
            <Spinner label="Loading FFmpeg status..." size="small" />
          ) : status ? (
            <>
              {status.path && (
                <div>
                  <Text size={300} weight="semibold" block>
                    Path
                  </Text>
                  <div className={styles.pathDisplay}>{status.path}</div>
                  {status.version && (
                    <Text className={styles.metadata} block style={{ marginTop: tokens.spacingVerticalXXS }}>
                      Version: {status.version}{' '}
                      {status.versionMeetsRequirement ? (
                        <Badge appearance="outline" color="success">
                          ✓ {status.minimumVersion}+
                        </Badge>
                      ) : (
                        <Badge appearance="outline" color="warning">
                          Requires {status.minimumVersion}+
                        </Badge>
                      )}
                    </Text>
                  )}
                  {status.source && (
                    <Text className={styles.metadata} block style={{ marginTop: tokens.spacingVerticalXXS }}>
                      Source:{' '}
                      <Badge appearance="outline">
                        {status.source === 'Managed'
                          ? 'Managed Installation'
                          : status.source === 'PATH'
                            ? 'System PATH'
                            : status.source === 'Configured'
                              ? 'User Configured'
                              : status.source === 'Environment'
                                ? 'Bundled (App)'
                              : status.source}
                      </Badge>
                    </Text>
                  )}
                </div>
              )}

              <div className={styles.row}>
                <div className={styles.actions}>
                  {!isReady && (
                    <>
                      <Button
                        appearance="primary"
                        icon={<ArrowDownload24Regular />}
                        onClick={handleInstall}
                        disabled={isProcessing}
                      >
                        {isProcessing ? <Spinner size="tiny" /> : 'Install Managed FFmpeg'}
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
                    appearance={isReady ? 'secondary' : 'subtle'}
                    icon={<ArrowSync24Regular />}
                    onClick={handleRescan}
                    disabled={isProcessing}
                  >
                    {isProcessing ? <Spinner size="tiny" /> : 'Rescan'}
                  </Button>

                  {isReady && (
                    <>
                      <Button appearance="subtle" icon={<Folder24Regular />} onClick={handleOpenFolder}>
                        Open Folder
                      </Button>
                      <Button
                        appearance="subtle"
                        icon={<Checkmark24Regular />}
                        onClick={handleInstall}
                        disabled={isProcessing}
                      >
                        Repair
                      </Button>
                    </>
                  )}
                </div>
              </div>
            </>
          ) : (
            <MessageBar intent="warning">
              <MessageBarBody>Unable to load FFmpeg status</MessageBarBody>
            </MessageBar>
          )}

          {renderTechnicalDetails()}
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
                placeholder="C:\\ffmpeg\\bin\\ffmpeg.exe or C:\\ffmpeg"
                style={{ width: '100%' }}
              />
              <Text
                size={200}
                style={{ marginTop: tokens.spacingVerticalS, color: tokens.colorNeutralForeground3 }}
              >
                Provide the path to ffmpeg.exe (or ffmpeg binary on Linux/Mac), or the folder containing it.
              </Text>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setShowAttachDialog(false)}>
                Cancel
              </Button>
              <Button appearance="primary" onClick={handleAttach} disabled={isProcessing}>
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
