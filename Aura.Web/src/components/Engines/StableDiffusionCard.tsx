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
  ProgressBar,
  Accordion,
  AccordionItem,
  AccordionHeader,
  AccordionPanel,
  Divider,
} from '@fluentui/react-components';
import {
  Play24Regular,
  Stop24Regular,
  ArrowDownload24Regular,
  Checkmark24Regular,
  Warning24Regular,
  Info24Regular,
  Folder24Regular,
  Globe24Regular,
  ArrowSync24Regular,
  Image24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect, useCallback } from 'react';
import { apiUrl } from '../../config/api';
import { useNotifications } from '../Notifications/Toasts';

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
    flexWrap: 'wrap',
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
  warningBox: {
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorPaletteYellowBackground1,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `3px solid ${tokens.colorPaletteYellowForeground1}`,
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalS,
  },
  successBox: {
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorPaletteGreenBackground1,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `3px solid ${tokens.colorPaletteGreenForeground1}`,
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalS,
  },
  errorBox: {
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorPaletteRedBackground1,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `3px solid ${tokens.colorPaletteRedForeground1}`,
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalS,
  },
  progressSection: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalS,
  },
  modelsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalS,
  },
  modelItem: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
  },
  monospace: {
    fontFamily: 'monospace',
  },
  row: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: tokens.spacingVerticalS,
  },
});

interface GpuCheckResult {
  meetsRequirements: boolean;
  isNvidiaGpu: boolean;
  vramGB: number;
  gpuModel?: string;
  gpuVendor?: string;
  message: string;
  recommendation?: string;
}

interface SDStatus {
  isInstalled: boolean;
  isRunning: boolean;
  isHealthy: boolean;
  port: number;
  processId?: number;
  installPath?: string;
  models?: string[];
  lastError?: string;
}

interface SDModel {
  id: string;
  name: string;
  description: string;
  sizeBytes: number;
  minVramGB: number;
  isDefault: boolean;
}

/**
 * Stable Diffusion card with GPU check, installation, and server controls
 */
export function StableDiffusionCard() {
  const styles = useStyles();
  const { showSuccessToast, showFailureToast } = useNotifications();

  const [gpuCheck, setGpuCheck] = useState<GpuCheckResult | null>(null);
  const [isCheckingGpu, setIsCheckingGpu] = useState(false);
  const [status, setStatus] = useState<SDStatus | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);
  const [isInstalling, setIsInstalling] = useState(false);
  const [installProgress, setInstallProgress] = useState(0);
  const [installMessage, setInstallMessage] = useState('');
  const [availableModels, setAvailableModels] = useState<SDModel[]>([]);

  // Check GPU requirements
  const checkGpu = useCallback(async () => {
    setIsCheckingGpu(true);
    try {
      const response = await fetch(apiUrl('/api/engines/stable-diffusion/gpu-check'));
      if (response.ok) {
        const data = await response.json();
        setGpuCheck(data);
      }
    } catch (error) {
      console.error('Failed to check GPU:', error);
      setGpuCheck({
        meetsRequirements: false,
        isNvidiaGpu: false,
        vramGB: 0,
        message: 'Could not detect GPU. Please ensure your GPU drivers are installed.',
        recommendation: 'Try updating your GPU drivers and restarting.',
      });
    } finally {
      setIsCheckingGpu(false);
    }
  }, []);

  // Fetch SD status
  const fetchStatus = useCallback(async () => {
    try {
      const response = await fetch(apiUrl('/api/engines/stable-diffusion/status'));
      if (response.ok) {
        const data = await response.json();
        setStatus(data);
      }
    } catch (error) {
      console.error('Failed to fetch SD status:', error);
    }
  }, []);

  // Fetch available models
  const fetchModels = useCallback(async () => {
    try {
      const response = await fetch(apiUrl('/api/engines/stable-diffusion/models'));
      if (response.ok) {
        const data = await response.json();
        setAvailableModels(data.models || []);
      }
    } catch (error) {
      console.error('Failed to fetch models:', error);
    }
  }, []);

  // Initial load
  useEffect(() => {
    checkGpu();
    fetchStatus();
    fetchModels();

    // Poll status every 5 seconds
    const interval = setInterval(fetchStatus, 5000);
    return () => clearInterval(interval);
  }, [checkGpu, fetchStatus, fetchModels]);

  // Handle installation via engines API
  const handleInstall = async () => {
    setIsInstalling(true);
    setInstallProgress(0);
    setInstallMessage('Starting installation...');

    try {
      const engineId = 'stable-diffusion-webui';

      // First, trigger the installation via the engines API
      const response = await fetch(apiUrl('/api/engines/install'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ engineId }),
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.error || 'Installation failed');
      }

      // Poll the status to track installation progress
      let attempts = 0;
      const maxAttempts = 60; // 5 minutes

      while (attempts < maxAttempts) {
        await new Promise((resolve) => setTimeout(resolve, 5000));
        const statusResponse = await fetch(apiUrl('/api/engines/stable-diffusion/status'));
        if (statusResponse.ok) {
          const statusData = await statusResponse.json();
          if (statusData.isInstalled) {
            setInstallProgress(100);
            setInstallMessage('Installation complete!');
            setIsInstalling(false);
            await fetchStatus();
            showSuccessToast({
              title: 'Installation Complete',
              message: 'Stable Diffusion WebUI installed successfully!',
            });
            return;
          }
        }
        setInstallProgress(Math.min(90, (attempts / maxAttempts) * 90));
        setInstallMessage(`Installing Stable Diffusion WebUI... (${attempts * 5}s)`);
        attempts++;
      }

      throw new Error('Installation timed out');
    } catch (error) {
      console.error('Installation failed:', error);
      showFailureToast({
        title: 'Installation Failed',
        message: error instanceof Error ? error.message : 'Unknown error',
      });
    } finally {
      setIsInstalling(false);
    }
  };

  // Handle server start
  const handleStart = async () => {
    setIsProcessing(true);
    try {
      const response = await fetch(apiUrl('/api/engines/stable-diffusion/start'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ port: 7860 }),
      });

      if (response.ok) {
        showSuccessToast({
          title: 'Server Starting',
          message:
            'Stable Diffusion WebUI is starting. It may take a few minutes to become fully operational.',
        });
        await fetchStatus();
      } else {
        const error = await response.json();
        throw new Error(error.error || 'Failed to start server');
      }
    } catch (error) {
      console.error('Failed to start SD:', error);
      showFailureToast({
        title: 'Start Failed',
        message: error instanceof Error ? error.message : 'Unknown error',
      });
    } finally {
      setIsProcessing(false);
    }
  };

  // Handle server stop
  const handleStop = async () => {
    setIsProcessing(true);
    try {
      const response = await fetch(apiUrl('/api/engines/stable-diffusion/stop'), {
        method: 'POST',
      });

      if (response.ok) {
        showSuccessToast({
          title: 'Server Stopped',
          message: 'Stable Diffusion WebUI stopped successfully.',
        });
        await fetchStatus();
      } else {
        const error = await response.json();
        throw new Error(error.error || 'Failed to stop server');
      }
    } catch (error) {
      console.error('Failed to stop SD:', error);
      showFailureToast({
        title: 'Stop Failed',
        message: error instanceof Error ? error.message : 'Unknown error',
      });
    } finally {
      setIsProcessing(false);
    }
  };

  const formatBytes = (bytes: number) => {
    const gb = bytes / (1024 * 1024 * 1024);
    return gb >= 1 ? `${gb.toFixed(1)} GB` : `${(bytes / (1024 * 1024)).toFixed(0)} MB`;
  };

  const getStatusBadge = () => {
    if (isCheckingGpu) {
      return (
        <Badge appearance="outline" icon={<Spinner size="tiny" />}>
          Checking...
        </Badge>
      );
    }

    if (!status?.isInstalled) {
      return <Badge appearance="outline">Not Installed</Badge>;
    }

    if (status?.isRunning && status?.isHealthy) {
      return (
        <Badge appearance="filled" color="success" icon={<Checkmark24Regular />}>
          Running
        </Badge>
      );
    }

    if (status?.isRunning) {
      return (
        <Badge appearance="filled" color="warning" icon={<Spinner size="tiny" />}>
          Starting...
        </Badge>
      );
    }

    return (
      <Badge appearance="filled" color="informative">
        Installed
      </Badge>
    );
  };

  return (
    <Card className={styles.card}>
      <CardHeader
        header={
          <div className={styles.header}>
            <div className={styles.title}>
              <Image24Regular style={{ fontSize: '24px', color: tokens.colorBrandForeground1 }} />
              <div>
                <Text weight="semibold" size={500}>
                  Stable Diffusion WebUI
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
            Generate custom images locally with AI. Requires NVIDIA GPU with 6GB+ VRAM.
          </Text>
        }
      />
      <CardPreview className={styles.content}>
        {/* GPU Check Section */}
        {isCheckingGpu ? (
          <div className={styles.infoBox}>
            <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
              <Spinner size="small" />
              <Text>Checking GPU requirements...</Text>
            </div>
          </div>
        ) : gpuCheck && !gpuCheck.meetsRequirements ? (
          <div className={styles.warningBox}>
            <Warning24Regular
              style={{ flexShrink: 0, color: tokens.colorPaletteYellowForeground1 }}
            />
            <div>
              <Text weight="semibold" block>
                {gpuCheck.message}
              </Text>
              {gpuCheck.recommendation && (
                <Text size={200} style={{ marginTop: tokens.spacingVerticalXXS }}>
                  {gpuCheck.recommendation}
                </Text>
              )}
            </div>
          </div>
        ) : (
          gpuCheck && (
            <div className={styles.successBox}>
              <Checkmark24Regular
                style={{ flexShrink: 0, color: tokens.colorPaletteGreenForeground1 }}
              />
              <div>
                <Text weight="semibold" block>
                  {gpuCheck.message}
                </Text>
                {gpuCheck.recommendation && (
                  <Text size={200} style={{ marginTop: tokens.spacingVerticalXXS }}>
                    {gpuCheck.recommendation}
                  </Text>
                )}
              </div>
            </div>
          )
        )}

        {/* GPU Details */}
        {gpuCheck && (
          <div className={styles.infoBox}>
            <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
              <Info24Regular style={{ flexShrink: 0 }} />
              <div>
                <Text size={200}>
                  Detected GPU: <strong>{gpuCheck.gpuModel || 'Unknown'}</strong> (
                  {gpuCheck.gpuVendor || 'Unknown'})
                </Text>
                <Text size={200} block>
                  VRAM: <strong>{gpuCheck.vramGB} GB</strong>
                  {gpuCheck.vramGB >= 12 && ' (SDXL capable)'}
                  {gpuCheck.vramGB >= 6 && gpuCheck.vramGB < 12 && ' (SD 1.5 recommended)'}
                </Text>
              </div>
            </div>
          </div>
        )}

        {/* Installation Progress */}
        {isInstalling && (
          <div className={styles.progressSection}>
            <Text weight="semibold" block style={{ marginBottom: tokens.spacingVerticalS }}>
              Installing Stable Diffusion WebUI
            </Text>
            <ProgressBar value={installProgress / 100} />
            <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
              {installMessage}
            </Text>
          </div>
        )}

        {/* Actions */}
        <div className={styles.row}>
          <div className={styles.actions}>
            {!status?.isInstalled ? (
              <Button
                appearance="primary"
                icon={<ArrowDownload24Regular />}
                onClick={handleInstall}
                disabled={isProcessing || isInstalling || (gpuCheck && !gpuCheck.meetsRequirements)}
                title={
                  gpuCheck && !gpuCheck.meetsRequirements
                    ? 'GPU requirements not met'
                    : 'Install Stable Diffusion WebUI'
                }
              >
                Install
              </Button>
            ) : status?.isRunning ? (
              <Button
                appearance="secondary"
                icon={<Stop24Regular />}
                onClick={handleStop}
                disabled={isProcessing}
              >
                Stop
              </Button>
            ) : (
              <Button
                appearance="primary"
                icon={<Play24Regular />}
                onClick={handleStart}
                disabled={isProcessing || (gpuCheck && !gpuCheck.meetsRequirements)}
              >
                Start
              </Button>
            )}

            <Button
              appearance="subtle"
              icon={<ArrowSync24Regular />}
              onClick={() => {
                checkGpu();
                fetchStatus();
              }}
              disabled={isCheckingGpu}
            >
              Refresh
            </Button>

            {status?.isRunning && status?.port && (
              <Button
                appearance="subtle"
                icon={<Globe24Regular />}
                onClick={() => window.open(`http://localhost:${status.port}`, '_blank')}
              >
                Open Web UI
              </Button>
            )}
          </div>

          {status?.installPath && (
            <Button
              appearance="subtle"
              icon={<Folder24Regular />}
              onClick={() => {
                // Trigger open folder via API
                fetch(apiUrl('/api/engines/open-folder'), {
                  method: 'POST',
                  headers: { 'Content-Type': 'application/json' },
                  body: JSON.stringify({ engineId: 'stable-diffusion-webui' }),
                });
              }}
            >
              Open Folder
            </Button>
          )}
        </div>

        {/* Server Status Details */}
        {status?.isInstalled && (
          <>
            <Divider style={{ marginTop: tokens.spacingVerticalS }} />
            <div>
              <Text size={200}>
                Install Path: <span className={styles.monospace}>{status.installPath}</span>
              </Text>
              {status.isRunning && (
                <Text size={200} block>
                  Port: <span className={styles.monospace}>{status.port}</span>
                  {status.processId && ` • PID: ${status.processId}`}
                </Text>
              )}
            </div>
          </>
        )}

        {/* Error Display */}
        {status?.lastError && (
          <div className={styles.errorBox}>
            <Warning24Regular style={{ flexShrink: 0, color: tokens.colorPaletteRedForeground1 }} />
            <Text size={200}>{status.lastError}</Text>
          </div>
        )}

        {/* Available Models Section */}
        {status?.isInstalled && availableModels.length > 0 && (
          <Accordion collapsible style={{ marginTop: tokens.spacingVerticalS }}>
            <AccordionItem value="models">
              <AccordionHeader>
                <Text weight="semibold">Available Models ({availableModels.length})</Text>
              </AccordionHeader>
              <AccordionPanel>
                <div className={styles.modelsList}>
                  {availableModels.map((model) => (
                    <div key={model.id} className={styles.modelItem}>
                      <div>
                        <Text weight="semibold">{model.name}</Text>
                        {model.isDefault && (
                          <Badge
                            appearance="outline"
                            color="brand"
                            style={{ marginLeft: tokens.spacingHorizontalS }}
                          >
                            Recommended
                          </Badge>
                        )}
                        <Text size={200} block>
                          {model.description}
                        </Text>
                        <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                          Size: {formatBytes(model.sizeBytes)} • Min VRAM: {model.minVramGB}GB
                        </Text>
                      </div>
                    </div>
                  ))}
                </div>
              </AccordionPanel>
            </AccordionItem>
          </Accordion>
        )}

        {/* Loaded Models (from running server) */}
        {status?.isRunning && status?.models && status.models.length > 0 && (
          <Accordion collapsible style={{ marginTop: tokens.spacingVerticalS }}>
            <AccordionItem value="loaded-models">
              <AccordionHeader>
                <Text weight="semibold">Loaded Models ({status.models.length})</Text>
              </AccordionHeader>
              <AccordionPanel>
                <div className={styles.modelsList}>
                  {status.models.map((model, idx) => (
                    <div key={idx} className={styles.modelItem}>
                      <Text className={styles.monospace}>{model}</Text>
                    </div>
                  ))}
                </div>
              </AccordionPanel>
            </AccordionItem>
          </Accordion>
        )}

        {/* Help Link */}
        <div style={{ marginTop: tokens.spacingVerticalS }}>
          <Link href="https://github.com/AUTOMATIC1111/stable-diffusion-webui" target="_blank">
            Learn More About Stable Diffusion WebUI
          </Link>
        </div>
      </CardPreview>
    </Card>
  );
}
