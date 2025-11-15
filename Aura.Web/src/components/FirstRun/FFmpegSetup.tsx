import {
  makeStyles,
  tokens,
  Card,
  Title3,
  Text,
  Button,
  Spinner,
  Badge,
  ProgressBar,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
  Input,
  Field,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Filled,
  Dismiss24Regular,
  Warning24Regular,
  ArrowDownload24Regular,
  Info24Regular,
  FolderOpen24Regular,
  ArrowClockwise24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect, useCallback } from 'react';
import type { FC } from 'react';
import { handleApiError } from '../../services/api/errorHandler';
import type { UserFriendlyError } from '../../services/api/errorHandler';
import { ffmpegClient, type FFmpegStatus } from '../../services/api/ffmpegClient';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  statusCard: {
    padding: tokens.spacingVerticalL,
  },
  statusHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalM,
  },
  statusDetails: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginLeft: '36px',
  },
  hardwareList: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
  },
  actionButtons: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalM,
  },
  infoBox: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'flex-start',
  },
  manualPathCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
  pathInputRow: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'flex-end',
    marginTop: tokens.spacingVerticalS,
  },
  recoveryOptions: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalM,
  },
});

interface FFmpegSetupProps {
  onStatusChange?: (installed: boolean, valid: boolean) => void;
  onAutoAdvance?: () => void;
}

export const FFmpegSetup: FC<FFmpegSetupProps> = ({ onStatusChange, onAutoAdvance }) => {
  const styles = useStyles();
  const [status, setStatus] = useState<FFmpegStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [installing, setInstalling] = useState(false);
  const [installProgress, setInstallProgress] = useState(0);
  const [error, setError] = useState<UserFriendlyError | null>(null);
  const [_retryCount, setRetryCount] = useState(0);
  const [showManualPath, setShowManualPath] = useState(false);
  const [manualPath, setManualPath] = useState('');
  const [validatingPath, setValidatingPath] = useState(false);

  const checkStatus = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);

      const statusData = await ffmpegClient.getStatus();
      setStatus(statusData);

      const isReady = statusData.installed && statusData.valid;
      onStatusChange?.(statusData.installed, statusData.valid);

      // Auto-advance if FFmpeg is ready
      if (isReady && onAutoAdvance) {
        console.info('[FFmpegSetup] FFmpeg is ready, auto-advancing...');
        onAutoAdvance();
      }
    } catch (err: unknown) {
      const friendlyError = handleApiError(err);
      setError(friendlyError);
      onStatusChange?.(false, false);
    } finally {
      setLoading(false);
    }
  }, [onStatusChange, onAutoAdvance]);

  useEffect(() => {
    checkStatus();
  }, [checkStatus]);

  const handleInstall = async () => {
    try {
      setInstalling(true);
      setInstallProgress(0);
      setError(null);

      // Simulate progress during installation
      const progressInterval = setInterval(() => {
        setInstallProgress((prev) => {
          if (prev >= 90) {
            clearInterval(progressInterval);
            return prev;
          }
          return prev + 10;
        });
      }, 2000);

      const result = await ffmpegClient.install();
      clearInterval(progressInterval);

      if (!result.success) {
        const errorMessage = result.message || 'Installation failed';
        throw new Error(errorMessage);
      }

      setInstallProgress(100);
      setRetryCount(0);

      // Verify installation after short delay
      setTimeout(async () => {
        await checkStatus();
        setInstalling(false);
        setInstallProgress(0);
      }, 1000);
    } catch (err: unknown) {
      setRetryCount((prev) => prev + 1);
      const friendlyError = handleApiError(err);
      setError(friendlyError);
      setInstalling(false);
      setInstallProgress(0);
    }
  };

  const handleRescan = async () => {
    try {
      setLoading(true);
      setError(null);

      const result = await ffmpegClient.rescan();

      if (result.success && result.installed && result.valid) {
        await checkStatus();
      } else {
        setError({
          title: 'FFmpeg Not Found',
          message: result.message || 'No valid FFmpeg installation found on this system.',
          howToFix: [
            'Install FFmpeg using the button above',
            'Or manually install FFmpeg and restart the wizard',
          ],
        });
      }
    } catch (err: unknown) {
      const friendlyError = handleApiError(err);
      setError(friendlyError);
    } finally {
      setLoading(false);
    }
  };

  const handleUseExisting = async () => {
    if (!manualPath.trim()) {
      setError({
        title: 'Invalid Path',
        message: 'Please enter a valid path to the FFmpeg executable.',
        howToFix: [],
      });
      return;
    }

    try {
      setValidatingPath(true);
      setError(null);

      const result = await ffmpegClient.useExisting({ path: manualPath.trim() });

      if (result.success && result.installed && result.valid) {
        await checkStatus();
        setShowManualPath(false);
        setManualPath('');
      } else {
        setError({
          title: 'Invalid FFmpeg',
          message:
            result.message || 'The specified path does not contain a valid FFmpeg installation.',
          howToFix: result.howToFix || [
            'Ensure the path points to the ffmpeg executable (ffmpeg.exe on Windows)',
            'Verify FFmpeg version is 4.0 or higher',
          ],
        });
      }
    } catch (err: unknown) {
      const friendlyError = handleApiError(err);
      setError(friendlyError);
    } finally {
      setValidatingPath(false);
    }
  };

  if (loading) {
    return (
      <div className={styles.container}>
        <Spinner label="Checking FFmpeg status..." />
      </div>
    );
  }

  // Show error if we have one and no status
  if (error && !status) {
    return (
      <div className={styles.container}>
        <Card className={styles.statusCard}>
          <MessageBar intent="error">
            <MessageBarBody>
              <MessageBarTitle>{error.title}</MessageBarTitle>
              <Text>{error.message}</Text>
              {error.correlationId && <Text size={200}>Correlation ID: {error.correlationId}</Text>}
              {error.howToFix && error.howToFix.length > 0 && (
                <div style={{ marginTop: tokens.spacingVerticalM }}>
                  <Text weight="semibold">How to fix:</Text>
                  <ul>
                    {error.howToFix.map((step, index) => (
                      <li key={index}>
                        <Text>{step}</Text>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
            </MessageBarBody>
          </MessageBar>

          <div className={styles.recoveryOptions}>
            <Text weight="semibold">Recovery Options:</Text>
            <div className={styles.actionButtons}>
              <Button appearance="primary" onClick={checkStatus} icon={<ArrowClockwise24Regular />}>
                Retry Check
              </Button>
              <Button appearance="secondary" onClick={handleRescan}>
                Rescan System
              </Button>
              <Button appearance="secondary" onClick={() => setShowManualPath(true)}>
                Use Existing Installation
              </Button>
            </div>
          </div>

          {showManualPath && (
            <Card className={styles.manualPathCard}>
              <Title3>Specify FFmpeg Path</Title3>
              <Text size={200}>
                Enter the full path to your FFmpeg executable (e.g., /usr/bin/ffmpeg or C:\Program
                Files\ffmpeg\bin\ffmpeg.exe)
              </Text>
              <div className={styles.pathInputRow}>
                <Field style={{ flex: 1 }}>
                  <Input
                    value={manualPath}
                    onChange={(e) => setManualPath(e.target.value)}
                    placeholder="Path to FFmpeg executable"
                  />
                </Field>
                <Button
                  icon={<FolderOpen24Regular />}
                  onClick={() => {
                    // Browse for file would go here in electron context
                    alert('File browser not available in web context');
                  }}
                >
                  Browse
                </Button>
              </div>
              <div className={styles.actionButtons}>
                <Button
                  appearance="primary"
                  onClick={handleUseExisting}
                  disabled={validatingPath || !manualPath.trim()}
                >
                  {validatingPath ? 'Validating...' : 'Validate and Use'}
                </Button>
                <Button
                  appearance="secondary"
                  onClick={() => {
                    setShowManualPath(false);
                    setManualPath('');
                  }}
                >
                  Cancel
                </Button>
              </div>
            </Card>
          )}
        </Card>
      </div>
    );
  }

  if (!status) {
    return (
      <div className={styles.container}>
        <Card className={styles.statusCard}>
          <div className={styles.statusHeader}>
            <Warning24Regular style={{ color: tokens.colorPaletteYellowForeground2 }} />
            <Title3>FFmpeg Status Unknown</Title3>
          </div>
          <Text>Unable to check FFmpeg status. Please try again.</Text>
          <div className={styles.actionButtons}>
            <Button appearance="primary" onClick={checkStatus}>
              Retry
            </Button>
          </div>
        </Card>
      </div>
    );
  }

  const hasHardwareAcceleration =
    status.hardwareAcceleration.nvencSupported ||
    status.hardwareAcceleration.amfSupported ||
    status.hardwareAcceleration.quickSyncSupported ||
    status.hardwareAcceleration.videoToolboxSupported;

  return (
    <div className={styles.container}>
      <Card className={styles.statusCard}>
        <div className={styles.statusHeader}>
          {status.installed && status.valid ? (
            <CheckmarkCircle24Filled style={{ color: tokens.colorPaletteGreenForeground1 }} />
          ) : (
            <Dismiss24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />
          )}
          <Title3>FFmpeg Status</Title3>
        </div>

        <div className={styles.statusDetails}>
          <Text>
            <strong>Installation:</strong>{' '}
            {status.installed && status.valid ? (
              <Badge appearance="filled" color="success">
                Installed
              </Badge>
            ) : (
              <Badge appearance="filled" color="danger">
                Not Installed
              </Badge>
            )}
          </Text>

          {status.version && (
            <Text>
              <strong>Version:</strong> {status.version}
              {status.versionMeetsRequirement ? (
                <Badge
                  appearance="outline"
                  color="success"
                  style={{ marginLeft: tokens.spacingHorizontalS }}
                >
                  ✓ {status.minimumVersion}+
                </Badge>
              ) : (
                <Badge
                  appearance="outline"
                  color="warning"
                  style={{ marginLeft: tokens.spacingHorizontalS }}
                >
                  Requires {status.minimumVersion}+
                </Badge>
              )}
            </Text>
          )}

          {status.path && (
            <Text>
              <strong>Location:</strong> {status.path}
            </Text>
          )}

          {status.source && (
            <Text>
              <strong>Source:</strong>{' '}
              <Badge appearance="outline">
                {status.source === 'Managed'
                  ? 'Managed Installation'
                  : status.source === 'PATH'
                    ? 'System PATH'
                    : status.source === 'Configured'
                      ? 'User Configured'
                      : status.source}
              </Badge>
            </Text>
          )}

          {status.error && (
            <Text style={{ color: tokens.colorPaletteRedForeground1 }}>
              <strong>Error:</strong> {status.errorMessage || status.error}
            </Text>
          )}

          {status.errorCode && (
            <Text size={200}>
              <strong>Error Code:</strong> {status.errorCode}
            </Text>
          )}

          {status.attemptedPaths && status.attemptedPaths.length > 0 && (
            <details style={{ marginTop: tokens.spacingVerticalS }}>
              <summary style={{ cursor: 'pointer', fontSize: '12px' }}>
                <Text size={200}>Show checked locations ({status.attemptedPaths.length})</Text>
              </summary>
              <ul style={{ fontSize: '11px', marginTop: tokens.spacingVerticalXS }}>
                {status.attemptedPaths.map((p, i) => (
                  <li key={i}>{p}</li>
                ))}
              </ul>
            </details>
          )}
        </div>

        {status.installed && status.valid && (
          <>
            <div className={styles.statusDetails} style={{ marginTop: tokens.spacingVerticalM }}>
              <Text weight="semibold">Hardware Acceleration:</Text>
              {hasHardwareAcceleration ? (
                <div className={styles.hardwareList}>
                  {status.hardwareAcceleration.nvencSupported && (
                    <Badge appearance="filled" color="brand">
                      NVIDIA NVENC
                    </Badge>
                  )}
                  {status.hardwareAcceleration.amfSupported && (
                    <Badge appearance="filled" color="danger">
                      AMD AMF
                    </Badge>
                  )}
                  {status.hardwareAcceleration.quickSyncSupported && (
                    <Badge appearance="filled" color="informative">
                      Intel QuickSync
                    </Badge>
                  )}
                  {status.hardwareAcceleration.videoToolboxSupported && (
                    <Badge appearance="filled" color="success">
                      VideoToolbox
                    </Badge>
                  )}
                </div>
              ) : (
                <Text size={200}>
                  <Badge appearance="outline">CPU Only (Slower)</Badge>
                  <span style={{ marginLeft: tokens.spacingHorizontalS }}>
                    Hardware acceleration not available
                  </span>
                </Text>
              )}
            </div>

            {hasHardwareAcceleration && (
              <div className={styles.infoBox} style={{ marginTop: tokens.spacingVerticalM }}>
                <Info24Regular />
                <Text size={200}>
                  Hardware acceleration detected! Video rendering will be 5-10x faster with GPU
                  encoding.
                </Text>
              </div>
            )}
          </>
        )}

        {!status.installed && (
          <div className={styles.actionButtons}>
            <Button
              appearance="primary"
              icon={<ArrowDownload24Regular />}
              onClick={handleInstall}
              disabled={installing}
            >
              {installing ? 'Installing...' : 'Install FFmpeg'}
            </Button>
            <Button appearance="secondary" onClick={checkStatus} disabled={installing}>
              Refresh Status
            </Button>
          </div>
        )}

        {installing && (
          <div style={{ marginTop: tokens.spacingVerticalM }}>
            <ProgressBar value={installProgress} max={100} />
            <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
              Downloading and installing FFmpeg...
            </Text>
          </div>
        )}
      </Card>

      {!status.installed && (
        <div className={styles.infoBox}>
          <Info24Regular />
          <div>
            <Text weight="semibold">About FFmpeg</Text>
            <Text size={200}>
              FFmpeg is required for video rendering and must be version 4.0 or higher. The
              installer will download and configure FFmpeg automatically for Windows users. For
              other platforms, please install FFmpeg manually and ensure it&apos;s available in your
              PATH.
            </Text>
          </div>
        </div>
      )}
    </div>
  );
};

export default FFmpegSetup;
