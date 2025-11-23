import {
  Badge,
  Button,
  Card,
  makeStyles,
  ProgressBar,
  Spinner,
  Text,
  Title3,
  tokens,
} from '@fluentui/react-components';
import {
  ArrowClockwise24Regular,
  ArrowDownload24Regular,
  Checkmark24Regular,
  Warning24Regular,
} from '@fluentui/react-icons';
import { useCallback, useEffect, useRef, useState } from 'react';
import { resetCircuitBreaker } from '../../services/api/apiClient';
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

export interface TtsDependencyCardProps {
  provider: 'piper' | 'mimic3';
  onInstallComplete?: (status: { installed: boolean; path?: string; baseUrl?: string }) => void;
  onStatusChange?: (status: { installed: boolean; path?: string; baseUrl?: string } | null) => void;
  autoCheck?: boolean;
  autoExpandDetails?: boolean;
  refreshSignal?: number;
}

interface TtsStatus {
  installed: boolean;
  path?: string;
  baseUrl?: string;
  voiceModelPath?: string;
  executableExists?: boolean;
  voiceModelExists?: boolean;
  reachable?: boolean;
  error?: string | null;
}

export function TtsDependencyCard({
  provider,
  onInstallComplete,
  onStatusChange,
  autoCheck = true,
  autoExpandDetails = false,
  refreshSignal,
}: TtsDependencyCardProps) {
  const styles = useStyles();
  const { showSuccessToast, showFailureToast } = useNotifications();
  const [status, setStatus] = useState<TtsStatus | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isInstalling, setIsInstalling] = useState(false);
  const [installProgress, setInstallProgress] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [showDetails, setShowDetails] = useState(autoExpandDetails);
  const lastRefreshSignal = useRef<number | undefined>(refreshSignal);
  const initialCheckDoneRef = useRef(false);

  const providerName = provider === 'piper' ? 'Piper TTS' : 'Mimic3 TTS';
  const providerDescription =
    provider === 'piper'
      ? 'Fast, lightweight offline text-to-speech. High quality voices with low resource usage.'
      : 'Neural TTS server with natural-sounding voices. Runs via Docker or Python.';

  const checkStatus = useCallback(async (): Promise<TtsStatus | null> => {
    setIsLoading(true);
    setError(null);
    try {
      resetCircuitBreaker();
      console.info(`[TtsDependencyCard] Checking ${providerName} status`);

      const endpoint = provider === 'piper' ? '/api/setup/check-piper' : '/api/setup/check-mimic3';
      const response = await fetch(endpoint);

      if (!response.ok) {
        throw new Error(`Failed to check ${providerName} status: ${response.statusText}`);
      }

      const data = (await response.json()) as TtsStatus;
      setStatus(data);

      if (data.installed) {
        onInstallComplete?.(data);
      }

      onStatusChange?.(data);

      // Return the status data so callers can use it immediately without waiting for state update
      return data;
    } catch (err: unknown) {
      let errorMessage = `Failed to check ${providerName} status`;

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

        if (axiosError.code === 'ERR_NETWORK' || axiosError.code === 'ECONNREFUSED') {
          errorMessage =
            'Backend server is not running. To start the backend:\n\n' +
            '1. Open a terminal in the project root\n' +
            '2. Run: dotnet run --project Aura.Api\n' +
            '3. Wait for "Application started" message\n' +
            '4. Click "Check Again" or refresh this page';
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
      onStatusChange?.(null);
      return null;
    } finally {
      setIsLoading(false);
    }
  }, [provider, providerName, onInstallComplete, onStatusChange]);

  const handleRescan = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      resetCircuitBreaker();
      console.info(`[TtsDependencyCard] Rescanning for ${providerName}`);

      await checkStatus();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : `Failed to rescan ${providerName}`;
      setError(errorMessage);
      showFailureToast({
        title: 'Rescan Failed',
        message: errorMessage,
      });
    } finally {
      setIsLoading(false);
    }
  }, [providerName, checkStatus, showFailureToast]);

  useEffect(() => {
    if (autoCheck && !initialCheckDoneRef.current) {
      initialCheckDoneRef.current = true;
      void checkStatus();
    }
  }, [autoCheck, checkStatus]);

  useEffect(() => {
    if (autoExpandDetails && status && !status.installed) {
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
      resetCircuitBreaker();
      console.info(`[TtsDependencyCard] Starting ${providerName} installation`);

      const progressInterval = setInterval(() => {
        setInstallProgress((prev) => {
          if (prev >= 90) {
            return prev;
          }
          return prev + 10;
        });
      }, 500);

      const endpoint = provider === 'piper' ? '/api/setup/install-piper' : '/api/setup/install-mimic3';
      
      let response: Response;
      try {
        response = await fetch(endpoint, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
        });
      } catch (networkError) {
        clearInterval(progressInterval);
        const errorMsg = networkError instanceof Error ? networkError.message : 'Network error';
        console.error(`[TtsDependencyCard] Network error during ${providerName} installation:`, networkError);
        throw new Error(`Failed to connect to server: ${errorMsg}. Please ensure the backend is running.`);
      }

      clearInterval(progressInterval);

      // Try to parse response body first to get detailed error information
      let result: {
        success: boolean;
        message?: string;
        error?: string;
        path?: string;
        baseUrl?: string;
        voiceModelPath?: string;
        voiceModelDownloaded?: boolean;
        requiresDocker?: boolean;
        requiresManualInstall?: boolean;
        dockerUrl?: string;
        alternativeInstructions?: string;
        instructions?: string[];
      };

      try {
        const responseText = await response.text();
        if (!responseText) {
          throw new Error('Empty response from server');
        }
        result = JSON.parse(responseText);
      } catch (parseError) {
        console.error(`[TtsDependencyCard] Failed to parse response:`, parseError);
        if (!response.ok) {
          throw new Error(`Installation failed: ${response.status} ${response.statusText}`);
        }
        throw new Error('Invalid response from server');
      }

      // Check for HTTP errors
      if (!response.ok) {
        const errorMessage = result.error || result.message || `Installation failed: ${response.statusText}`;
        console.error(`[TtsDependencyCard] ${providerName} installation failed:`, errorMessage);
        throw new Error(errorMessage);
      }

      if (result.success) {
        showSuccessToast({
          title: `${providerName} Installed Successfully`,
          message: result.message || `${providerName} has been installed and is ready to use.`,
        });

        // Wait longer for the backend to finalize configuration and for services to be ready
        // Piper needs time for file system to flush, Mimic3 needs time for Docker container to be ready
        const waitTime = provider === 'mimic3' ? 4000 : 2500;
        await new Promise((resolve) => setTimeout(resolve, waitTime));

        // Retry status check with multiple attempts and increasing delays
        // Use the return value from checkStatus() instead of the state variable
        // because React state updates are asynchronous
        let statusChecked = false;
        let lastStatus: TtsStatus | null = null;

        for (let attempt = 0; attempt < 5; attempt++) {
          const currentStatus = await checkStatus();
          lastStatus = currentStatus;

          // Check if status is now installed using the returned value
          if (currentStatus?.installed) {
            statusChecked = true;
            console.info(`[TtsDependencyCard] ${providerName} status confirmed as installed on attempt ${attempt + 1}`);
            break;
          }

          if (attempt < 4) {
            // Wait progressively longer before retrying (exponential backoff)
            const retryDelay = 1000 * (attempt + 1);
            const errorMsg = currentStatus?.error || 'Not ready yet';
            console.info(`[TtsDependencyCard] ${providerName} not ready yet (${errorMsg}), retrying in ${retryDelay}ms (attempt ${attempt + 2}/5)`);
            await new Promise((resolve) => setTimeout(resolve, retryDelay));
          }
        }

        if (!statusChecked && lastStatus && !lastStatus.installed) {
          // Show a helpful message
          const errorMsg = lastStatus.error || 'Installation may still be in progress';
          console.warn(`[TtsDependencyCard] ${providerName} installation completed but status check didn't confirm after 5 attempts. Error: ${errorMsg}`);
          showFailureToast({
            title: `${providerName} Installation Pending`,
            message: `Installation completed, but ${providerName} is not ready yet. Please click 'Re-scan' in a few moments. ${errorMsg}`,
          });
        }
      } else {
        // Installation not fully automated - show instructions
        if (result.requiresDocker) {
          showFailureToast({
            title: 'Docker Required',
            message: result.message || 'Docker is required for Mimic3. Please install Docker Desktop first.',
          });
        } else if (result.requiresManualInstall) {
          showFailureToast({
            title: 'Manual Installation Required',
            message: result.message || 'Please install manually using the provided instructions.',
          });
        } else {
          throw new Error(result.message || 'Installation failed');
        }
      }
    } catch (err: unknown) {
      let errorTitle = 'Installation Failed';
      let errorMessage = 'Installation failed';

      console.error(`[TtsDependencyCard] ${providerName} installation error:`, err);

      if (err instanceof Error) {
        errorMessage = err.message;
        
        // Provide more helpful error messages for common issues
        if (errorMessage.includes('Network') || errorMessage.includes('Failed to connect')) {
          errorTitle = 'Connection Error';
          errorMessage = 'Unable to connect to the Aura backend. Please ensure the backend server is running and try again.';
        } else if (errorMessage.includes('timeout') || errorMessage.includes('Timeout')) {
          errorTitle = 'Timeout Error';
          errorMessage = 'The installation request timed out. This may be due to a slow internet connection. Please try again.';
        } else if (errorMessage.includes('Docker')) {
          errorTitle = 'Docker Required';
          errorMessage = 'Docker is required for Mimic3 TTS. Please install Docker Desktop first.';
        } else if (errorMessage.includes('manual') || errorMessage.includes('Manual')) {
          errorTitle = 'Manual Installation Required';
        }
      } else if (err && typeof err === 'object') {
        const errorObj = err as { message?: string; error?: string; code?: string };
        errorMessage = errorObj.message || errorObj.error || errorMessage;
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

    if (!status || !status.installed) {
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

    if (!status || !status.installed) {
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

  const isReady = Boolean(status?.installed);

  return (
    <Card className={styles.card}>
      <div className={styles.header}>
        <div className={styles.info}>
          {getStatusIcon()}
          <div className={styles.details}>
            <Title3 className={styles.name}>
              {providerName}
              <Badge
                appearance="tint"
                color="informative"
                style={{ marginLeft: tokens.spacingHorizontalS }}
              >
                Optional
              </Badge>
            </Title3>
            <Text className={styles.description} size={200}>
              {providerDescription}
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
                Install {providerName}
              </Button>
              <Button
                appearance="secondary"
                icon={<ArrowClockwise24Regular />}
                onClick={handleRescan}
                disabled={isInstalling || isLoading}
              >
                {isLoading ? 'Checking...' : 'Re-scan'}
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
              {isLoading ? 'Checking...' : 'Re-scan'}
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
              {provider === 'piper' && status.path && (
                <div className={styles.detailRow}>
                  <Text weight="semibold">Executable Path:</Text>
                  <Text size={200}>{status.path}</Text>
                </div>
              )}
              {provider === 'piper' && status.voiceModelPath && (
                <div className={styles.detailRow}>
                  <Text weight="semibold">Voice Model:</Text>
                  <Text size={200}>{status.voiceModelPath}</Text>
                </div>
              )}
              {provider === 'mimic3' && status.baseUrl && (
                <div className={styles.detailRow}>
                  <Text weight="semibold">Server URL:</Text>
                  <Text size={200}>{status.baseUrl}</Text>
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
                {providerName} is not installed or not ready. Install managed {providerName} to continue.
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

