import {
  makeStyles,
  tokens,
  Title3,
  Text,
  Card,
  Button,
  Spinner,
  Badge,
  ProgressBar,
} from '@fluentui/react-components';
import {
  Checkmark24Regular,
  Warning24Regular,
  ArrowDownload24Regular,
  ArrowClockwise24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect, useRef, useCallback } from 'react';
import { resetCircuitBreaker } from '../../services/api/apiClient';
import { ffmpegClient, type FFmpegStatusExtended } from '../../services/api/ffmpegClient';
import { useNotifications } from '../Notifications/Toasts';

const useStyles = makeStyles({
  card: {
    padding: tokens.spacingVerticalL,
    transition: 'all 0.2s ease-in-out',
    ':hover': {
      boxShadow: tokens.shadow8,
    },
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalM,
  },
  info: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    flex: 1,
  },
  statusIcon: {
    fontSize: '32px',
  },
  details: {
    flex: 1,
  },
  name: {
    marginBottom: tokens.spacingVerticalXS,
  },
  description: {
    color: tokens.colorNeutralForeground3,
    display: 'block',
  },
  actionsContainer: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'center',
    flexWrap: 'wrap',
  },
  detailsSection: {
    marginTop: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  detailRow: {
    display: 'flex',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalXS,
  },
  progressSection: {
    marginTop: tokens.spacingVerticalM,
  },
});

export interface FFmpegDependencyCardProps {
  onInstallComplete?: (status: FFmpegStatusExtended) => void;
  onStatusChange?: (status: FFmpegStatusExtended | null) => void;
  autoCheck?: boolean;
  autoExpandDetails?: boolean;
  refreshSignal?: number;
}

export function FFmpegDependencyCard({
  onInstallComplete,
  onStatusChange,
  autoCheck = true,
  autoExpandDetails = false,
  refreshSignal,
}: FFmpegDependencyCardProps) {
  const styles = useStyles();
  const { showSuccessToast, showFailureToast } = useNotifications();
  const [status, setStatus] = useState<FFmpegStatusExtended | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isInstalling, setIsInstalling] = useState(false);
  const [installProgress, setInstallProgress] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [showDetails, setShowDetails] = useState(autoExpandDetails);
  const lastRefreshSignal = useRef<number | undefined>(refreshSignal);

  const checkStatus = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      // Reset circuit breaker before checking status
      resetCircuitBreaker();
      console.info('[FFmpegDependencyCard] Circuit breaker reset, checking FFmpeg status');

      const ffmpegStatus = await ffmpegClient.getStatusExtended();
      setStatus(ffmpegStatus);

      if (ffmpegStatus.installed && ffmpegStatus.valid) {
        onInstallComplete?.(ffmpegStatus);
      }

      onStatusChange?.(ffmpegStatus);
    } catch (err: unknown) {
      let errorMessage = 'Failed to check FFmpeg status';

      if (err && typeof err === 'object') {
        const axiosError = err as {
          code?: string;
          response?: {
            data?: {
              message?: string;
              detail?: string;
              error?: string;
            };
            status?: number;
          };
          message?: string;
          request?: unknown;
        };

        // Network-level errors (no response received) - IMPROVED GUIDANCE
        if (axiosError.code === 'ERR_NETWORK' || axiosError.code === 'ECONNREFUSED') {
          errorMessage = 
            'Backend server is not running. To start the backend:\n\n' +
            '1. Open a terminal in the project root\n' +
            '2. Run: dotnet run --project Aura.Api\n' +
            '3. Wait for "Application started" message\n' +
            '4. Click "Check Again" or refresh this page';
        } else if (axiosError.code === 'ECONNABORTED' || axiosError.code === 'ETIMEDOUT') {
          errorMessage = 
            'Connection timeout. The backend may be starting up or overloaded.\n\n' +
            'Wait a moment and try again. If the problem persists, restart the backend.';
        } else if (axiosError.request && !axiosError.response) {
          errorMessage = 
            'No response from backend server. To start the backend:\n\n' +
            '1. Open a terminal in the project root\n' +
            '2. Run: dotnet run --project Aura.Api\n' +
            '3. Wait for "Application started" message\n' +
            '4. Click "Check Again" or refresh this page';
        } else if (axiosError.response?.data) {
          const data = axiosError.response.data;
          errorMessage = data.message || data.detail || data.error || errorMessage;

          if (axiosError.response.status === 428) {
            errorMessage = `Setup required: ${errorMessage}. Please complete the setup wizard first.`;
          }
        } else if (axiosError.message) {
          errorMessage = axiosError.message;
        }
      } else if (err instanceof Error) {
        errorMessage = err.message;
      }

      setError(errorMessage);
      onStatusChange?.(null);
    } finally {
      setIsLoading(false);
    }
  }, [onInstallComplete, onStatusChange]);

  const handleRescan = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      // Reset circuit breaker before rescanning
      resetCircuitBreaker();
      console.info('[FFmpegDependencyCard] Circuit breaker reset, rescanning for FFmpeg');

      const rescanResult = await ffmpegClient.rescan();

      if (rescanResult.success) {
        showSuccessToast({
          title: 'Rescan Complete',
          message: rescanResult.message,
        });

        // After rescan, fetch full extended status
        const updatedStatus = await ffmpegClient.getStatusExtended();
        setStatus(updatedStatus);

        if (updatedStatus.installed && updatedStatus.valid) {
          onInstallComplete?.(updatedStatus);
        }

        onStatusChange?.(updatedStatus);

        // Get full status with hardware acceleration
        await checkStatus();
      } else {
        throw new Error(rescanResult.message || 'Rescan failed');
      }
    } catch (err: unknown) {
      let errorMessage = 'Failed to rescan for FFmpeg';

      if (err && typeof err === 'object') {
        const axiosError = err as {
          code?: string;
          response?: {
            data?: {
              message?: string;
              detail?: string;
              error?: string;
            };
          };
          message?: string;
          request?: unknown;
        };

        // Network-level errors - IMPROVED GUIDANCE
        if (axiosError.code === 'ERR_NETWORK' || axiosError.code === 'ECONNREFUSED') {
          errorMessage = 
            'Backend server is not running. To start the backend:\n\n' +
            '1. Open a terminal in the project root\n' +
            '2. Run: dotnet run --project Aura.Api\n' +
            '3. Wait for "Application started" message\n' +
            '4. Try rescanning again';
        } else if (axiosError.code === 'ECONNABORTED' || axiosError.code === 'ETIMEDOUT') {
          errorMessage = 
            'Connection timeout. The backend may be starting up or overloaded.\n\n' +
            'Wait a moment and try again. If the problem persists, restart the backend.';
        } else if (axiosError.request && !axiosError.response) {
          errorMessage = 
            'No response from backend server. To start the backend:\n\n' +
            '1. Open a terminal in the project root\n' +
            '2. Run: dotnet run --project Aura.Api\n' +
            '3. Wait for "Application started" message\n' +
            '4. Try rescanning again';
        } else if (axiosError.response?.data) {
          const data = axiosError.response.data;
          errorMessage = data.message || data.detail || data.error || errorMessage;
        } else if (axiosError.message) {
          errorMessage = axiosError.message;
        }
      } else if (err instanceof Error) {
        errorMessage = err.message;
      }

      setError(errorMessage);
      showFailureToast({
        title: 'Rescan Failed',
        message: errorMessage,
      });
    } finally {
      setIsLoading(false);
    }
  }, [checkStatus, onInstallComplete, onStatusChange, showSuccessToast, showFailureToast]);

  useEffect(() => {
    if (autoCheck) {
      void checkStatus();
    }
  }, [autoCheck, checkStatus]);

  // Auto-expand details if FFmpeg is not ready and autoExpandDetails is true
  useEffect(() => {
    if (autoExpandDetails && status && (!status.installed || !status.valid)) {
      setShowDetails(true);
    }
  }, [status, autoExpandDetails]);

  useEffect(() => {
    if (refreshSignal === undefined) {
      return;
    }

    if (lastRefreshSignal.current === refreshSignal) {
      return;
    }

    lastRefreshSignal.current = refreshSignal;
    void checkStatus();
  }, [refreshSignal, checkStatus]);

  const handleInstall = async () => {
    setIsInstalling(true);
    setInstallProgress(0);
    setError(null);

    try {
      // Reset circuit breaker before installation
      resetCircuitBreaker();
      console.info('[FFmpegDependencyCard] Circuit breaker reset, starting FFmpeg installation');

      const progressInterval = setInterval(() => {
        setInstallProgress((prev) => {
          if (prev >= 90) {
            return prev;
          }
          return prev + 10;
        });
      }, 500);

      const result = await ffmpegClient.install();

      clearInterval(progressInterval);
      setInstallProgress(100);

      if (result.success) {
        showSuccessToast({
          title: 'FFmpeg Installed Successfully',
          message: result.message || 'FFmpeg has been installed and is ready to use.',
        });

        // Wait a moment for the backend to finalize, then check status
        await new Promise((resolve) => setTimeout(resolve, 1000));
        await checkStatus();
      } else {
        throw new Error(result.message || 'Installation failed');
      }
    } catch (err: unknown) {
      let errorTitle = 'Installation Failed';
      let errorMessage = 'Installation failed';

      if (err && typeof err === 'object') {
        const axiosError = err as {
          code?: string;
          response?: {
            data?: {
              message?: string;
              detail?: string;
              error?: string;
              title?: string;
            };
            status?: number;
          };
          message?: string;
          request?: unknown;
        };

        // Network-level errors
        if (axiosError.code === 'ERR_NETWORK' || axiosError.code === 'ECONNREFUSED') {
          errorTitle = 'Backend Unreachable';
          errorMessage = 'Unable to connect to the Aura backend. Please ensure it is running.';
        } else if (axiosError.code === 'ECONNABORTED' || axiosError.code === 'ETIMEDOUT') {
          errorTitle = 'Connection Timeout';
          errorMessage = 'The installation request timed out. This may be due to a slow download.';
        } else if (axiosError.request && !axiosError.response) {
          errorTitle = 'Network Error';
          errorMessage = 'No response from backend. Please check that the Aura backend is running.';
        } else if (axiosError.response?.data) {
          const data = axiosError.response.data;
          errorTitle = data.title || errorTitle;
          errorMessage = data.message || data.detail || data.error || errorMessage;
        } else if (axiosError.message) {
          errorMessage = axiosError.message;
        }
      } else if (err instanceof Error) {
        errorMessage = err.message;
      }

      setError(errorMessage);
      showFailureToast({
        title: errorTitle,
        message: errorMessage,
      });
    } finally {
      setIsInstalling(false);
      setInstallProgress(0);
    }
  };

  const getStatusIcon = () => {
    if (isLoading) {
      return <Spinner size="medium" className={styles.statusIcon} />;
    }

    if (!status || !status.installed || !status.valid) {
      return (
        <Warning24Regular
          className={styles.statusIcon}
          style={{ color: tokens.colorPaletteYellowForeground1 }}
        />
      );
    }

    return (
      <Checkmark24Regular
        className={styles.statusIcon}
        style={{ color: tokens.colorPaletteGreenForeground1 }}
      />
    );
  };

  const getStatusBadge = () => {
    if (isInstalling) {
      return (
        <Badge appearance="filled" color="informative">
          Installing...
        </Badge>
      );
    }

    if (isLoading) {
      return <Badge appearance="outline">Checking...</Badge>;
    }

    if (!status || !status.installed || !status.valid) {
      return (
        <Badge appearance="filled" color="warning">
          Not Ready
        </Badge>
      );
    }

    return (
      <Badge appearance="filled" color="success">
        Ready
      </Badge>
    );
  };

  const isReady = Boolean(status?.installed && status?.valid);

  const formatSource = (source?: string | null) => {
    if (!source) {
      return 'Unknown';
    }

    switch (source) {
      case 'Managed':
        return 'Managed Installation';
      case 'Configured':
        return 'User Configured';
      case 'PATH':
        return 'System PATH';
      case 'Environment':
        return 'Bundled (App)';
      default:
        return source;
    }
  };

  return (
    <Card className={styles.card}>
      <div className={styles.header}>
        <div className={styles.info}>
          {getStatusIcon()}
          <div className={styles.details}>
            <Title3 className={styles.name}>
              FFmpeg (Video Encoding)
              <Badge
                appearance="tint"
                color="danger"
                style={{ marginLeft: tokens.spacingHorizontalS }}
              >
                Required
              </Badge>
            </Title3>
            <Text className={styles.description} size={200}>
              Essential video and audio processing toolkit. Required for all video generation.
            </Text>
          </div>
        </div>
        <div className={styles.actionsContainer}>
          {getStatusBadge()}
          {!isReady && (
            <>
              <Button
                appearance="primary"
                icon={<ArrowDownload24Regular />}
                onClick={handleInstall}
                disabled={isInstalling || isLoading}
              >
                Install Managed FFmpeg
              </Button>
              <Button
                appearance="secondary"
                icon={<ArrowClockwise24Regular />}
                onClick={handleRescan}
                disabled={isInstalling || isLoading}
              >
                {isLoading ? 'Scanning...' : 'Re-scan'}
              </Button>
            </>
          )}
          {isReady && (
            <Button
              appearance="secondary"
              icon={<ArrowClockwise24Regular />}
              onClick={handleRescan}
              disabled={isLoading}
            >
              {isLoading ? 'Scanning...' : 'Re-scan'}
            </Button>
          )}
        </div>
      </div>

      {isInstalling && installProgress !== undefined && (
        <div className={styles.progressSection}>
          <ProgressBar value={installProgress} max={100} />
          <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
            Installing... {installProgress}%
          </Text>
        </div>
      )}

      {error && (
        <div
          style={{
            marginTop: tokens.spacingVerticalM,
            padding: tokens.spacingVerticalS,
            backgroundColor: tokens.colorPaletteRedBackground1,
            borderRadius: tokens.borderRadiusSmall,
          }}
        >
          <Text
            size={200}
            style={{
              color: tokens.colorPaletteRedForeground1,
              whiteSpace: 'pre-wrap',
              display: 'block',
            }}
          >
            ⚠ {error}
          </Text>
        </div>
      )}

      {showDetails && status && (
        <div className={styles.detailsSection}>
          {isReady ? (
            <>
              <div className={styles.detailRow}>
                <Text weight="semibold">Version:</Text>
                <Text>{status.version || 'Unknown'}</Text>
              </div>
              <div className={styles.detailRow}>
                <Text weight="semibold">Path:</Text>
                <Text size={200}>{status.path || 'Unknown'}</Text>
              </div>
              <div className={styles.detailRow}>
                <Text weight="semibold">Source:</Text>
                <Text>{formatSource(status.source)}</Text>
              </div>
              {status.hardwareAcceleration.availableEncoders.length > 0 && (
                <div className={styles.detailRow}>
                  <Text weight="semibold">Hardware Acceleration:</Text>
                  <Text size={200}>
                    {status.hardwareAcceleration.nvencSupported && '✓ NVENC '}
                    {status.hardwareAcceleration.amfSupported && '✓ AMF '}
                    {status.hardwareAcceleration.quickSyncSupported && '✓ QuickSync '}
                    {!status.hardwareAcceleration.nvencSupported &&
                      !status.hardwareAcceleration.amfSupported &&
                      !status.hardwareAcceleration.quickSyncSupported &&
                      'CPU Only'}
                  </Text>
                </div>
              )}
            </>
          ) : (
            <>
              <Text
                style={{
                  color: tokens.colorNeutralForeground3,
                  marginBottom: tokens.spacingVerticalM,
                }}
              >
                FFmpeg is not installed or not ready. Install managed FFmpeg to continue.
              </Text>
              {status.error && (
                <Text
                  style={{
                    color: tokens.colorPaletteRedForeground1,
                    marginBottom: tokens.spacingVerticalM,
                    fontWeight: 'semibold',
                  }}
                >
                  Error: {status.error}
                </Text>
              )}
              <div style={{ display: 'flex', gap: tokens.spacingHorizontalS, flexWrap: 'wrap' }}>
                <Button
                  appearance="secondary"
                  onClick={() => {
                    window.open('/downloads', '_blank');
                  }}
                  disabled={isInstalling}
                >
                  Attach Existing...
                </Button>
              </div>
            </>
          )}
        </div>
      )}

      <div
        style={{
          display: 'flex',
          gap: tokens.spacingHorizontalS,
          marginTop: tokens.spacingVerticalM,
        }}
      >
        <Button
          appearance="subtle"
          size="small"
          onClick={() => setShowDetails(!showDetails)}
          disabled={isInstalling || isLoading}
        >
          {showDetails ? 'Hide Details' : 'Show Details'}
        </Button>
      </div>
    </Card>
  );
}
