/**
 * Ollama Setup Step Component
 * 
 * Guides users through setting up Ollama as a local LLM provider
 */

import {
  Card,
  makeStyles,
  tokens,
  Text,
  Title3,
  Button,
  Spinner,
  Link,
} from '@fluentui/react-components';
import {
  Checkmark24Regular,
  Warning24Regular,
  ArrowDownload24Regular,
  Play24Regular,
  Info24Regular,
} from '@fluentui/react-icons';
import { useEffect, useState } from 'react';
import type { OllamaSetupStatus, OllamaModelRecommendation } from '../../services/ollamaSetupService';
import {
  checkOllamaStatus,
  getInstallGuide,
  startOllama,
  getModelRecommendationsForSystem,
} from '../../services/ollamaSetupService';
import { useNotifications } from '../Notifications/Toasts';

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
  statusIcon: {
    width: '24px',
    height: '24px',
  },
  installGuide: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  stepsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    paddingLeft: tokens.spacingHorizontalXL,
  },
  modelsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  modelCard: {
    padding: tokens.spacingVerticalM,
  },
  modelHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalS,
  },
  modelInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalM,
  },
  infoCard: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorPaletteBlueBackground1,
    borderLeft: `4px solid ${tokens.colorPaletteBlueBorder1}`,
  },
});

export interface OllamaSetupStepProps {
  availableMemoryGB: number;
  availableDiskGB: number;
  onSetupComplete?: () => void;
}

export function OllamaSetupStep({ availableMemoryGB, availableDiskGB, onSetupComplete }: OllamaSetupStepProps) {
  const styles = useStyles();
  const { showSuccessToast, showFailureToast } = useNotifications();
  const [status, setStatus] = useState<OllamaSetupStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [starting, setStarting] = useState(false);
  const installGuide = getInstallGuide();

  useEffect(() => {
    checkStatus();
  }, []);

  const checkStatus = async () => {
    setLoading(true);
    try {
      const result = await checkOllamaStatus();
      setStatus(result);
      
      if (result.installed && result.running && result.modelsInstalled.length > 0) {
        onSetupComplete?.();
      }
    } catch (error) {
      console.error('Failed to check Ollama status:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleStartOllama = async () => {
    setStarting(true);
    try {
      const result = await startOllama();
      
      if (result.success) {
        showSuccessToast({
          title: 'Ollama Started',
          message: result.message,
        });
        await checkStatus();
      } else {
        showFailureToast({
          title: 'Failed to Start Ollama',
          message: result.message,
        });
      }
    } catch (error) {
      showFailureToast({
        title: 'Error Starting Ollama',
        message: error instanceof Error ? error.message : 'Unknown error',
      });
    } finally {
      setStarting(false);
    }
  };

  const handleOpenDownloadPage = () => {
    window.open(installGuide.downloadUrl, '_blank');
  };

  if (loading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', padding: tokens.spacingVerticalXXL }}>
        <Spinner size="large" label="Checking Ollama installation..." />
      </div>
    );
  }

  if (!status) {
    return (
      <Card className={styles.statusCard}>
        <Text>Failed to check Ollama status. Please try again.</Text>
        <Button appearance="secondary" onClick={checkStatus} style={{ marginTop: tokens.spacingVerticalM }}>
          Retry
        </Button>
      </Card>
    );
  }

  // Ollama is installed and running with models
  if (status.installed && status.running && status.modelsInstalled.length > 0) {
    return (
      <div className={styles.container}>
        <Card className={styles.statusCard}>
          <div className={styles.statusHeader}>
            <Checkmark24Regular className={styles.statusIcon} style={{ color: tokens.colorPaletteGreenForeground1 }} />
            <Title3>Ollama Ready!</Title3>
          </div>
          <Text>
            Ollama is installed, running, and has {status.modelsInstalled.length} model(s) installed.
          </Text>
          <div style={{ marginTop: tokens.spacingVerticalM }}>
            <Text weight="semibold">Installed Models:</Text>
            <ul style={{ marginTop: tokens.spacingVerticalS }}>
              {status.modelsInstalled.map((model) => (
                <li key={model}>
                  <Text>{model}</Text>
                </li>
              ))}
            </ul>
          </div>
        </Card>
      </div>
    );
  }

  // Ollama is installed but not running
  if (status.installed && !status.running) {
    return (
      <div className={styles.container}>
        <Card className={styles.statusCard}>
          <div className={styles.statusHeader}>
            <Warning24Regular className={styles.statusIcon} style={{ color: tokens.colorPaletteYellowForeground1 }} />
            <Title3>Ollama Installed but Not Running</Title3>
          </div>
          <Text>
            Ollama is installed at <code>{status.installationPath}</code> but is not currently running.
          </Text>
          <div className={styles.actions}>
            <Button 
              appearance="primary" 
              icon={<Play24Regular />}
              onClick={handleStartOllama}
              disabled={starting}
            >
              {starting ? 'Starting...' : 'Start Ollama'}
            </Button>
            <Button appearance="secondary" onClick={checkStatus}>
              Refresh Status
            </Button>
          </div>
        </Card>

        {status.modelsInstalled.length === 0 && (
          <Card className={styles.infoCard}>
            <div className={styles.statusHeader}>
              <Info24Regular className={styles.statusIcon} style={{ color: tokens.colorPaletteBlueForeground1 }} />
              <Title3>No Models Installed</Title3>
            </div>
            <Text>
              After starting Ollama, you'll need to download at least one model. Recommended models will be shown once Ollama is running.
            </Text>
          </Card>
        )}
      </div>
    );
  }

  // Ollama is not installed - show installation guide
  const recommendedModels = getModelRecommendationsForSystem(availableMemoryGB, availableDiskGB);

  return (
    <div className={styles.container}>
      <Card className={styles.statusCard}>
        <div className={styles.statusHeader}>
          <Info24Regular className={styles.statusIcon} style={{ color: tokens.colorPaletteBlueForeground1 }} />
          <Title3>Ollama Not Installed</Title3>
        </div>
        <Text>
          Ollama is a free, open-source tool that runs large language models locally on your computer. 
          It provides AI capabilities without requiring API keys or internet connectivity.
        </Text>
      </Card>

      <Card className={styles.statusCard}>
        <Title3 style={{ marginBottom: tokens.spacingVerticalM }}>Installation Guide for {installGuide.platform}</Title3>
        <div className={styles.installGuide}>
          <Text>
            <strong>Estimated time:</strong> {installGuide.estimatedTime}
          </Text>
          
          <Text weight="semibold" style={{ marginTop: tokens.spacingVerticalM }}>Steps:</Text>
          <ol className={styles.stepsList}>
            {installGuide.steps.map((step, index) => (
              <li key={index}>
                <Text>{step}</Text>
              </li>
            ))}
          </ol>

          <div className={styles.actions}>
            <Button 
              appearance="primary" 
              icon={<ArrowDownload24Regular />}
              onClick={handleOpenDownloadPage}
            >
              Download Ollama
            </Button>
            <Button appearance="secondary" onClick={checkStatus}>
              I've Installed Ollama
            </Button>
          </div>
        </div>
      </Card>

      {recommendedModels.length > 0 && (
        <Card className={styles.infoCard}>
          <Title3 style={{ marginBottom: tokens.spacingVerticalM }}>Recommended Models for Your System</Title3>
          <Text style={{ marginBottom: tokens.spacingVerticalM }}>
            Based on your available memory ({availableMemoryGB.toFixed(1)} GB) and disk space ({availableDiskGB.toFixed(1)} GB), 
            here are the models we recommend:
          </Text>
          <div className={styles.modelsList}>
            {recommendedModels.slice(0, 3).map((model) => (
              <Card key={model.name} className={styles.modelCard}>
                <div className={styles.modelHeader}>
                  <div className={styles.modelInfo}>
                    <Text weight="semibold">{model.displayName}</Text>
                    <Text size={200}>{model.size}</Text>
                  </div>
                  {model.recommended && (
                    <Text size={200} weight="semibold" style={{ color: tokens.colorPaletteGreenForeground1 }}>
                      Recommended
                    </Text>
                  )}
                </div>
                <Text size={200}>{model.description}</Text>
              </Card>
            ))}
          </div>
          <Text size={200} style={{ marginTop: tokens.spacingVerticalM }}>
            You can download models after installing Ollama using the command: <code>ollama pull &lt;model-name&gt;</code>
          </Text>
        </Card>
      )}

      <Card className={styles.infoCard}>
        <div className={styles.statusHeader}>
          <Info24Regular className={styles.statusIcon} style={{ color: tokens.colorPaletteBlueForeground1 }} />
          <Title3>Why Use Ollama?</Title3>
        </div>
        <ul>
          <li><Text>✅ <strong>Free and Open Source</strong> - No API keys or subscription costs</Text></li>
          <li><Text>✅ <strong>Privacy</strong> - All processing happens locally on your machine</Text></li>
          <li><Text>✅ <strong>No Internet Required</strong> - Works offline once models are downloaded</Text></li>
          <li><Text>✅ <strong>Multiple Models</strong> - Choose from dozens of open-source models</Text></li>
        </ul>
      </Card>
    </div>
  );
}
