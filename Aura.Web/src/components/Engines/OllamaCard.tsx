import {
  Card,
  CardHeader,
  CardPreview,
  Button,
  Text,
  Badge,
  Spinner,
  makeStyles,
  tokens,
  Link,
  Divider,
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  ProgressBar,
  Tooltip,
} from '@fluentui/react-components';
import {
  Checkmark24Regular,
  Warning24Regular,
  ArrowSync24Regular,
  Info24Regular,
  ArrowDownload24Regular,
  Play24Regular,
  Stop24Regular,
  Delete24Regular,
  Add24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect, useCallback } from 'react';
import { useOllamaDetection } from '../../hooks/useOllamaDetection';
import { ollamaClient, type RecommendedModel } from '../../services/api/ollamaClient';

const useStyles = makeStyles({
  card: {
    width: '100%',
    marginBottom: tokens.spacingVerticalM,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  title: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  content: {
    padding: tokens.spacingVerticalM,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'center',
  },
  helperText: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
    marginTop: tokens.spacingVerticalXS,
  },
  infoBox: {
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `3px solid ${tokens.colorBrandForeground1}`,
  },
  actionArea: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'center',
    marginTop: tokens.spacingVerticalS,
    flexWrap: 'wrap',
  },
  statusMessage: {
    padding: tokens.spacingVerticalS,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalS,
  },
  statusMessageSuccess: {
    backgroundColor: tokens.colorPaletteGreenBackground1,
  },
  statusMessageNeutral: {
    backgroundColor: tokens.colorNeutralBackground2,
  },
  successText: {
    color: tokens.colorPaletteGreenForeground1,
  },
  monospaceText: {
    fontFamily: 'monospace',
  },
  neutralText: {
    color: tokens.colorNeutralForeground3,
    marginBottom: tokens.spacingVerticalS,
  },
  modelsSection: {
    marginTop: tokens.spacingVerticalM,
  },
  modelList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    marginTop: tokens.spacingVerticalS,
  },
  modelItem: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalS}`,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  modelInfo: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  recommendedModelList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalS,
  },
  recommendedModelItem: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  recommendedModelInfo: {
    flex: 1,
  },
  progressContainer: {
    marginTop: tokens.spacingVerticalS,
  },
  warningText: {
    color: tokens.colorPaletteYellowForeground1,
  },
});

// Constants for installation progress simulation
const INSTALL_PROGRESS_INITIAL = 10;
const INSTALL_PROGRESS_INCREMENT = 10;
const INSTALL_PROGRESS_MAX_SIMULATED = 90;
const INSTALL_PROGRESS_INTERVAL_MS = 1000;
const INSTALL_PROGRESS_COMPLETE = 100;

// Delay before refreshing status after server start
const SERVER_STARTUP_DELAY_MS = 2000;

interface InstalledModel {
  name: string;
  size?: string;
  modifiedAt?: string;
}

/**
 * Enhanced Ollama card with installation and model management
 */
export function OllamaCard() {
  const styles = useStyles();
  const { isDetected, isChecking, detect } = useOllamaDetection(true);

  // State for installation and models
  const [isInstalled, setIsInstalled] = useState<boolean | null>(null);
  const [isInstalling, setIsInstalling] = useState(false);
  const [installProgress, setInstallProgress] = useState(0);
  const [isRunning, setIsRunning] = useState(false);
  const [isStarting, setIsStarting] = useState(false);
  const [isStopping, setIsStopping] = useState(false);
  const [installedModels, setInstalledModels] = useState<InstalledModel[]>([]);
  const [recommendedModels, setRecommendedModels] = useState<RecommendedModel[]>([]);
  const [isPullingModel, setIsPullingModel] = useState<string | null>(null);
  const [isDeletingModel, setIsDeletingModel] = useState<string | null>(null);
  const [showPullDialog, setShowPullDialog] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Fetch status on mount and when detection changes
  const refreshStatus = useCallback(async () => {
    try {
      const status = await ollamaClient.getStatus();
      setIsInstalled(status.installed ?? (status.running || false));
      setIsRunning(status.running || false);
      setError(null);

      // If running, fetch models
      if (status.running) {
        try {
          const modelsResponse = await ollamaClient.getModels();
          setInstalledModels(modelsResponse.models || []);
        } catch {
          // Models endpoint may fail if Ollama is not ready yet
          setInstalledModels([]);
        }
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : String(err);
      console.error('Failed to fetch Ollama status:', errorMessage);
      // If we can't reach the API, Ollama is likely not installed/running
      setIsInstalled(false);
      setIsRunning(false);
    }
  }, []);

  // Fetch recommended models
  const fetchRecommendedModels = useCallback(async () => {
    try {
      const response = await ollamaClient.getRecommendedModels();
      setRecommendedModels(response.models || []);
    } catch {
      // Use default recommended models if API fails
      setRecommendedModels([
        {
          name: 'llama3.2:3b',
          displayName: 'Llama 3.2 (3B)',
          description: 'Fast and efficient. Best for systems with limited resources.',
          size: '2.0 GB',
          sizeBytes: 2 * 1024 * 1024 * 1024,
          isRecommended: true,
        },
        {
          name: 'llama3.1:8b',
          displayName: 'Llama 3.1 (8B)',
          description: 'Balanced performance and quality. Recommended for most users.',
          size: '4.7 GB',
          sizeBytes: 4.7 * 1024 * 1024 * 1024,
          isRecommended: true,
        },
        {
          name: 'mistral:7b',
          displayName: 'Mistral (7B)',
          description: 'Excellent for creative writing and script generation.',
          size: '4.1 GB',
          sizeBytes: 4.1 * 1024 * 1024 * 1024,
          isRecommended: true,
        },
      ]);
    }
  }, []);

  useEffect(() => {
    refreshStatus();
    fetchRecommendedModels();
  }, [refreshStatus, fetchRecommendedModels]);

  // Refresh when detection state changes
  useEffect(() => {
    if (isDetected !== null) {
      refreshStatus();
    }
  }, [isDetected, refreshStatus]);

  // Handle Ollama installation
  const handleInstall = async () => {
    setIsInstalling(true);
    setInstallProgress(INSTALL_PROGRESS_INITIAL);
    setError(null);

    try {
      // Simulate progress while installing
      const progressInterval = setInterval(() => {
        setInstallProgress((prev) =>
          Math.min(prev + INSTALL_PROGRESS_INCREMENT, INSTALL_PROGRESS_MAX_SIMULATED)
        );
      }, INSTALL_PROGRESS_INTERVAL_MS);

      const result = await ollamaClient.install();

      clearInterval(progressInterval);
      setInstallProgress(INSTALL_PROGRESS_COMPLETE);

      if (result.success) {
        setIsInstalled(true);
        await refreshStatus();
      } else {
        setError(result.message || 'Installation failed');
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : String(err);
      setError(`Installation failed: ${errorMessage}`);
    } finally {
      setIsInstalling(false);
      setInstallProgress(0);
    }
  };

  // Handle server start
  const handleStart = async () => {
    setIsStarting(true);
    setError(null);

    try {
      const result = await ollamaClient.start();
      if (result.success) {
        setIsRunning(true);
        // Wait for server to be ready before refreshing status
        setTimeout(() => {
          refreshStatus();
          detect();
        }, SERVER_STARTUP_DELAY_MS);
      } else {
        setError(result.message || 'Failed to start Ollama');
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : String(err);
      setError(`Failed to start: ${errorMessage}`);
    } finally {
      setIsStarting(false);
    }
  };

  // Handle server stop
  const handleStop = async () => {
    setIsStopping(true);
    setError(null);

    try {
      const result = await ollamaClient.stop();
      if (result.success) {
        setIsRunning(false);
        setInstalledModels([]);
        detect();
      } else {
        setError(result.message || 'Failed to stop Ollama');
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : String(err);
      setError(`Failed to stop: ${errorMessage}`);
    } finally {
      setIsStopping(false);
    }
  };

  // Handle model pull
  const handlePullModel = async (modelName: string) => {
    setIsPullingModel(modelName);
    setError(null);
    setShowPullDialog(false);

    try {
      const result = await ollamaClient.pullModel(modelName);
      if (result.success) {
        await refreshStatus();
      } else {
        setError(result.message || `Failed to pull ${modelName}`);
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : String(err);
      setError(`Failed to pull model: ${errorMessage}`);
    } finally {
      setIsPullingModel(null);
    }
  };

  // Handle model delete
  const handleDeleteModel = async (modelName: string) => {
    setIsDeletingModel(modelName);
    setError(null);

    try {
      const result = await ollamaClient.deleteModel(modelName);
      if (result.success) {
        setInstalledModels((prev) => prev.filter((m) => m.name !== modelName));
      } else {
        setError(result.message || `Failed to delete ${modelName}`);
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : String(err);
      setError(`Failed to delete model: ${errorMessage}`);
    } finally {
      setIsDeletingModel(null);
    }
  };

  const getStatusBadge = () => {
    if (isChecking) {
      return (
        <Badge appearance="outline" icon={<Spinner size="tiny" />}>
          Checking...
        </Badge>
      );
    }

    if (isDetected === true || isRunning) {
      return (
        <Badge appearance="filled" color="success" icon={<Checkmark24Regular />}>
          Running
        </Badge>
      );
    }

    if (isInstalled) {
      return (
        <Badge appearance="tint" color="subtle" icon={<Warning24Regular />}>
          Installed (Not Running)
        </Badge>
      );
    }

    if (isDetected === false) {
      return (
        <Badge appearance="tint" color="subtle" icon={<Warning24Regular />}>
          Not Installed
        </Badge>
      );
    }

    return <Badge appearance="outline">Unknown</Badge>;
  };

  const getStatusIcon = () => {
    if (isChecking || isInstalling) {
      return <Spinner size="medium" />;
    }

    if (isDetected === true || isRunning) {
      return (
        <Checkmark24Regular
          style={{ fontSize: '32px', color: tokens.colorPaletteGreenForeground1 }}
        />
      );
    }

    return <Warning24Regular style={{ fontSize: '32px', color: tokens.colorNeutralForeground3 }} />;
  };

  const isModelInstalled = (modelName: string) => {
    return installedModels.some((m) => m.name === modelName || m.name.startsWith(modelName + ':'));
  };

  return (
    <Card className={styles.card}>
      <CardHeader
        header={
          <div className={styles.header}>
            <div className={styles.title}>
              {getStatusIcon()}
              <div>
                <Text weight="semibold" size={500}>
                  Ollama (Local AI)
                </Text>
                <Badge
                  appearance="tint"
                  color="informative"
                  style={{ marginLeft: tokens.spacingHorizontalS }}
                >
                  Optional
                </Badge>
              </div>
            </div>
            <div className={styles.actions}>{getStatusBadge()}</div>
          </div>
        }
        description={
          <Text className={styles.helperText}>
            Run AI models locally for script generation. Privacy-focused alternative to cloud APIs.
          </Text>
        }
      />
      <CardPreview className={styles.content}>
        {/* Error display */}
        {error && (
          <div
            className={styles.statusMessage}
            style={{ backgroundColor: tokens.colorPaletteRedBackground1 }}
          >
            <Text size={200} style={{ color: tokens.colorPaletteRedForeground1 }}>
              ⚠️ {error}
            </Text>
          </div>
        )}

        {/* Installation progress */}
        {isInstalling && (
          <div className={styles.progressContainer}>
            <Text size={200}>Installing Ollama...</Text>
            <ProgressBar value={installProgress / 100} />
          </div>
        )}

        {/* Not installed state */}
        {!isInstalled && !isInstalling && (
          <>
            <div className={styles.infoBox}>
              <div
                style={{
                  display: 'flex',
                  alignItems: 'flex-start',
                  gap: tokens.spacingHorizontalS,
                }}
              >
                <Info24Regular style={{ flexShrink: 0, marginTop: '2px' }} />
                <div>
                  <Text size={200}>
                    Ollama allows you to run AI models locally for script generation without sending
                    data to cloud services.
                  </Text>
                </div>
              </div>
            </div>

            <div className={styles.actionArea}>
              <Button
                appearance="primary"
                size="medium"
                icon={<ArrowDownload24Regular />}
                onClick={handleInstall}
                disabled={isInstalling}
              >
                Install Ollama
              </Button>
              <Button
                appearance="secondary"
                size="medium"
                icon={<ArrowSync24Regular />}
                onClick={() => {
                  detect();
                  refreshStatus();
                }}
                disabled={isChecking}
              >
                {isChecking ? 'Checking...' : 'Auto-Detect'}
              </Button>
            </div>

            <Text size={200} className={styles.neutralText}>
              Already have Ollama installed?{' '}
              <Link href="https://ollama.ai" target="_blank">
                Start it manually
              </Link>{' '}
              and click Auto-Detect.
            </Text>
          </>
        )}

        {/* Installed but not running state */}
        {isInstalled && !isRunning && !isInstalling && (
          <>
            <div className={`${styles.statusMessage} ${styles.statusMessageNeutral}`}>
              <Text size={200}>
                Ollama is installed but not running. Start the server to use local AI models.
              </Text>
            </div>

            <div className={styles.actionArea}>
              <Button
                appearance="primary"
                size="medium"
                icon={<Play24Regular />}
                onClick={handleStart}
                disabled={isStarting}
              >
                {isStarting ? 'Starting...' : 'Start Ollama'}
              </Button>
              <Button
                appearance="secondary"
                size="medium"
                icon={<ArrowSync24Regular />}
                onClick={() => {
                  detect();
                  refreshStatus();
                }}
                disabled={isChecking}
              >
                Refresh
              </Button>
            </div>
          </>
        )}

        {/* Running state */}
        {(isDetected === true || isRunning) && (
          <>
            <div className={`${styles.statusMessage} ${styles.statusMessageSuccess}`}>
              <Text size={200} className={styles.successText}>
                ✓ Ollama is running and available at{' '}
                <strong className={styles.monospaceText}>http://localhost:11434</strong>
              </Text>
            </div>

            <div className={styles.actionArea}>
              <Button
                appearance="secondary"
                size="medium"
                icon={<Stop24Regular />}
                onClick={handleStop}
                disabled={isStopping}
              >
                {isStopping ? 'Stopping...' : 'Stop Server'}
              </Button>
              <Button
                appearance="secondary"
                size="medium"
                icon={<ArrowSync24Regular />}
                onClick={() => {
                  detect();
                  refreshStatus();
                }}
                disabled={isChecking}
              >
                Refresh
              </Button>
            </div>

            <Divider style={{ marginTop: tokens.spacingVerticalM }} />

            {/* Models section */}
            <div className={styles.modelsSection}>
              <div
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                }}
              >
                <Text weight="semibold" size={400}>
                  Installed Models ({installedModels.length})
                </Text>
                <Dialog
                  open={showPullDialog}
                  onOpenChange={(_, data) => setShowPullDialog(data.open)}
                >
                  <DialogTrigger disableButtonEnhancement>
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<Add24Regular />}
                      disabled={!!isPullingModel}
                    >
                      Add Model
                    </Button>
                  </DialogTrigger>
                  <DialogSurface>
                    <DialogBody>
                      <DialogTitle>Pull a Model</DialogTitle>
                      <DialogContent>
                        <Text size={200} block style={{ marginBottom: tokens.spacingVerticalM }}>
                          Select a recommended model to download for script generation:
                        </Text>
                        <div className={styles.recommendedModelList}>
                          {recommendedModels.map((model) => (
                            <div key={model.name} className={styles.recommendedModelItem}>
                              <div className={styles.recommendedModelInfo}>
                                <Text weight="semibold">{model.displayName}</Text>
                                <Text size={200} block>
                                  {model.description}
                                </Text>
                                <Text size={100} style={{ color: tokens.colorNeutralForeground3 }}>
                                  Size: {model.size}
                                </Text>
                              </div>
                              {isModelInstalled(model.name) ? (
                                <Badge appearance="filled" color="success">
                                  Installed
                                </Badge>
                              ) : (
                                <Button
                                  appearance="primary"
                                  size="small"
                                  onClick={() => handlePullModel(model.name)}
                                  disabled={!!isPullingModel}
                                >
                                  {isPullingModel === model.name ? 'Pulling...' : 'Pull'}
                                </Button>
                              )}
                            </div>
                          ))}
                        </div>
                      </DialogContent>
                      <DialogActions>
                        <DialogTrigger disableButtonEnhancement>
                          <Button appearance="secondary">Close</Button>
                        </DialogTrigger>
                      </DialogActions>
                    </DialogBody>
                  </DialogSurface>
                </Dialog>
              </div>

              {installedModels.length === 0 ? (
                <div
                  className={styles.statusMessageNeutral}
                  style={{ marginTop: tokens.spacingVerticalS }}
                >
                  <Text size={200}>
                    No models installed. Click &quot;Add Model&quot; to download one.
                  </Text>
                </div>
              ) : (
                <div className={styles.modelList}>
                  {installedModels.map((model) => (
                    <div key={model.name} className={styles.modelItem}>
                      <div className={styles.modelInfo}>
                        <Text>{model.name}</Text>
                        {model.size && (
                          <Text size={100} style={{ color: tokens.colorNeutralForeground3 }}>
                            {model.size}
                          </Text>
                        )}
                      </div>
                      <Tooltip content="Delete model" relationship="label">
                        <Button
                          appearance="subtle"
                          size="small"
                          icon={<Delete24Regular />}
                          onClick={() => handleDeleteModel(model.name)}
                          disabled={isDeletingModel === model.name}
                        >
                          {isDeletingModel === model.name ? 'Deleting...' : ''}
                        </Button>
                      </Tooltip>
                    </div>
                  ))}
                </div>
              )}

              {/* Pulling model indicator */}
              {isPullingModel && (
                <div className={styles.progressContainer}>
                  <Text size={200}>
                    <Spinner size="tiny" style={{ marginRight: tokens.spacingHorizontalXS }} />
                    Pulling model: {isPullingModel}... This may take several minutes.
                  </Text>
                </div>
              )}
            </div>
          </>
        )}
      </CardPreview>
    </Card>
  );
}
