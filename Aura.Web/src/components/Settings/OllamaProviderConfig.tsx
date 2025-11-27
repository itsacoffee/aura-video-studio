import {
  makeStyles,
  tokens,
  Text,
  Button,
  Field,
  Spinner,
  Dropdown,
  Option,
  Link,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
  Badge,
  Switch,
  SpinButton,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Filled,
  DismissCircle24Filled,
  ArrowClockwise24Regular,
  Warning24Regular,
  Desktop24Regular,
} from '@fluentui/react-icons';
import { useState, useCallback, useEffect } from 'react';
import type { FC } from 'react';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  statusRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalS,
  },
  statusBadge: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXXS,
  },
  modelsDropdown: {
    width: '100%',
  },
  buttonGroup: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
  },
  infoText: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  helpLink: {
    fontSize: tokens.fontSizeBase200,
  },
  messageBarContainer: {
    marginTop: tokens.spacingVerticalS,
  },
  modelInfo: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    marginTop: tokens.spacingVerticalXXS,
  },
  gpuSection: {
    marginTop: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
  gpuHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalS,
  },
  gpuSettings: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  gpuRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'space-between',
  },
  gpuStatus: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
    marginTop: tokens.spacingVerticalXS,
  },
});

interface OllamaProviderConfigProps {
  selectedModel?: string;
  onModelChange: (model: string) => void;
}

interface OllamaStatus {
  isAvailable: boolean;
  version?: string;
  modelsCount: number;
  message?: string;
}

interface OllamaModel {
  name: string;
  size: number;
  sizeFormatted: string;
  modified?: string;
  modifiedFormatted?: string;
}

interface GpuStatus {
  gpuEnabled: boolean;
  numGpu: number;
  numCtx: number;
  autoDetect: boolean;
  hasGpu: boolean;
  gpuName: string | null;
  vramMB: number;
  vramFormatted: string;
  recommendedNumGpu: number;
  recommendedNumCtx: number;
  detectionMethod: string;
}

export const OllamaProviderConfig: FC<OllamaProviderConfigProps> = ({
  selectedModel,
  onModelChange,
}) => {
  const styles = useStyles();
  const [status, setStatus] = useState<OllamaStatus | null>(null);
  const [models, setModels] = useState<OllamaModel[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // GPU state
  const [gpuStatus, setGpuStatus] = useState<GpuStatus | null>(null);
  const [gpuLoading, setGpuLoading] = useState(false);
  const [gpuEnabled, setGpuEnabled] = useState(true);
  const [numCtx, setNumCtx] = useState(4096);

  const checkStatus = useCallback(async () => {
    try {
      const response = await fetch('/api/providers/ollama/status');
      const data = await response.json();
      setStatus(data);
      return data;
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : String(err);
      setError(`Failed to check Ollama status: ${errorMessage}`);
      return null;
    }
  }, []);

  const fetchGpuStatus = useCallback(async () => {
    try {
      setGpuLoading(true);
      const response = await fetch('/api/ollama/gpu/status');
      const data = await response.json();
      setGpuStatus(data);
      setGpuEnabled(data.gpuEnabled);
      setNumCtx(data.numCtx);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : String(err);
      console.error('Failed to fetch GPU status:', errorMessage);
    } finally {
      setGpuLoading(false);
    }
  }, []);

  const handleAutoDetectGpu = useCallback(async () => {
    try {
      setGpuLoading(true);
      const response = await fetch('/api/ollama/gpu/auto-detect', { method: 'POST' });
      const data = await response.json();
      setGpuStatus(data);
      setGpuEnabled(data.gpuEnabled);
      setNumCtx(data.numCtx);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : String(err);
      setError(`Failed to auto-detect GPU: ${errorMessage}`);
    } finally {
      setGpuLoading(false);
    }
  }, []);

  const handleGpuEnabledChange = useCallback(
    async (enabled: boolean) => {
      setGpuEnabled(enabled);
      try {
        await fetch('/api/ollama/gpu/config', {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ gpuEnabled: enabled }),
        });
        await fetchGpuStatus();
      } catch (err: unknown) {
        const errorMessage = err instanceof Error ? err.message : String(err);
        setError(`Failed to update GPU setting: ${errorMessage}`);
      }
    },
    [fetchGpuStatus]
  );

  const handleNumCtxChange = useCallback(
    async (value: number | null) => {
      if (value === null) return;
      setNumCtx(value);
      try {
        await fetch('/api/ollama/gpu/config', {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ numCtx: value }),
        });
        await fetchGpuStatus();
      } catch (err: unknown) {
        const errorMessage = err instanceof Error ? err.message : String(err);
        setError(`Failed to update context window: ${errorMessage}`);
      }
    },
    [fetchGpuStatus]
  );

  const fetchModels = useCallback(async () => {
    try {
      const response = await fetch('/api/providers/ollama/models');
      const data = await response.json();

      if (data.success && data.models) {
        setModels(data.models);

        if (data.models.length > 0 && !selectedModel) {
          onModelChange(data.models[0].name);
        }

        setError(null);
      } else {
        setModels([]);
        if (!data.success) {
          setError(data.message || 'Failed to fetch models');
        }
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : String(err);
      setError(`Failed to fetch models: ${errorMessage}`);
      setModels([]);
    }
  }, [selectedModel, onModelChange]);

  const handleRefresh = useCallback(async () => {
    setLoading(true);
    setError(null);

    const statusData = await checkStatus();
    if (statusData?.isAvailable) {
      await fetchModels();
    } else {
      setModels([]);
    }

    setLoading(false);
  }, [checkStatus, fetchModels]);

  useEffect(() => {
    handleRefresh();
    fetchGpuStatus();
    // Run once on mount only
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const getStatusBadge = () => {
    if (loading) {
      return (
        <div className={styles.statusBadge}>
          <Spinner size="extra-tiny" />
          <Text size={200}>Checking...</Text>
        </div>
      );
    }

    if (!status) {
      return (
        <Badge appearance="filled" color="subtle">
          Unknown
        </Badge>
      );
    }

    if (status.isAvailable && status.modelsCount > 0) {
      return (
        <div className={styles.statusBadge}>
          <CheckmarkCircle24Filled style={{ color: tokens.colorPaletteGreenForeground1 }} />
          <Badge appearance="filled" color="success">
            Connected
          </Badge>
          <Text size={200} className={styles.infoText}>
            {status.modelsCount} {status.modelsCount === 1 ? 'model' : 'models'} available
          </Text>
        </div>
      );
    }

    if (status.isAvailable && status.modelsCount === 0) {
      return (
        <div className={styles.statusBadge}>
          <Warning24Regular style={{ color: tokens.colorPaletteYellowForeground1 }} />
          <Badge appearance="filled" color="warning">
            No Models
          </Badge>
        </div>
      );
    }

    return (
      <div className={styles.statusBadge}>
        <DismissCircle24Filled style={{ color: tokens.colorNeutralForeground3 }} />
        <Badge appearance="filled" color="subtle">
          Not Running
        </Badge>
      </div>
    );
  };

  return (
    <div className={styles.container}>
      <Field
        label="Ollama Service Status"
        hint={
          <span className={styles.helpLink}>
            Ollama runs locally without API keys.{' '}
            <Link href="https://ollama.com/" target="_blank" rel="noopener noreferrer">
              Learn more →
            </Link>
          </span>
        }
      >
        <div className={styles.statusRow}>
          {getStatusBadge()}
          <Button
            appearance="subtle"
            icon={<ArrowClockwise24Regular />}
            onClick={handleRefresh}
            disabled={loading}
            size="small"
          >
            Refresh
          </Button>
        </div>
      </Field>

      {error && (
        <MessageBar intent="error" className={styles.messageBarContainer}>
          <MessageBarBody>
            <MessageBarTitle>Error</MessageBarTitle>
            {error}
          </MessageBarBody>
        </MessageBar>
      )}

      {status && !status.isAvailable && (
        <MessageBar intent="info" className={styles.messageBarContainer}>
          <MessageBarBody>
            <MessageBarTitle>Ollama Not Running</MessageBarTitle>
            <Text size={200}>
              Install Ollama to use local AI models:{' '}
              <Link href="https://ollama.com/download" target="_blank" rel="noopener noreferrer">
                Download Ollama
              </Link>
            </Text>
            <br />
            <Text size={200} className={styles.infoText}>
              Linux/Mac: <code>curl -fsSL https://ollama.com/install.sh | sh</code>
            </Text>
          </MessageBarBody>
        </MessageBar>
      )}

      {status?.isAvailable && models.length === 0 && !loading && (
        <MessageBar intent="warning" className={styles.messageBarContainer}>
          <MessageBarBody>
            <MessageBarTitle>No Models Installed</MessageBarTitle>
            <Text size={200}>
              Pull a model to get started. For example: <code>ollama pull llama3.1</code>
            </Text>
            <br />
            <Text size={200} className={styles.infoText}>
              See available models at{' '}
              <Link href="https://ollama.com/library" target="_blank" rel="noopener noreferrer">
                ollama.com/library
              </Link>
            </Text>
          </MessageBarBody>
        </MessageBar>
      )}

      {models.length > 0 && (
        <Field label="Selected Model" hint="Choose which Ollama model to use for script generation">
          <Dropdown
            className={styles.modelsDropdown}
            placeholder="Select a model"
            value={selectedModel || ''}
            selectedOptions={selectedModel ? [selectedModel] : []}
            onOptionSelect={(_, data) => {
              if (data.optionValue) {
                onModelChange(data.optionValue);
              }
            }}
          >
            {models.map((model) => (
              <Option key={model.name} value={model.name} text={model.name}>
                <div>
                  <Text weight="semibold">{model.name}</Text>
                  <Text size={200} className={styles.modelInfo}>
                    {model.sizeFormatted}
                    {model.modifiedFormatted && ` • Modified: ${model.modifiedFormatted}`}
                  </Text>
                </div>
              </Option>
            ))}
          </Dropdown>
        </Field>
      )}

      {status?.version && (
        <Text size={200} className={styles.infoText}>
          Ollama version: {status.version}
        </Text>
      )}

      {/* GPU Configuration Section */}
      <div className={styles.gpuSection}>
        <div className={styles.gpuHeader}>
          <Desktop24Regular />
          <Text weight="semibold">GPU Acceleration</Text>
          {gpuLoading && <Spinner size="extra-tiny" />}
        </div>

        <div className={styles.gpuSettings}>
          <div className={styles.gpuRow}>
            <Text>Enable GPU</Text>
            <Switch
              checked={gpuEnabled}
              onChange={(_, data) => handleGpuEnabledChange(data.checked)}
              disabled={gpuLoading}
            />
          </div>

          {gpuStatus?.hasGpu && (
            <div className={styles.gpuStatus}>
              <Text size={200}>
                Detected: <strong>{gpuStatus.gpuName}</strong> ({gpuStatus.vramFormatted})
              </Text>
            </div>
          )}

          {!gpuStatus?.hasGpu && gpuStatus?.detectionMethod !== 'NotAvailable' && (
            <MessageBar intent="warning" className={styles.messageBarContainer}>
              <MessageBarBody>
                <MessageBarTitle>No GPU Detected</MessageBarTitle>
                <Text size={200}>
                  Ollama will use CPU mode. For faster inference, install NVIDIA drivers.
                </Text>
              </MessageBarBody>
            </MessageBar>
          )}

          {gpuEnabled && (
            <Field
              label="Context Window Size"
              hint="Higher values use more VRAM but allow longer context"
            >
              <SpinButton
                value={numCtx}
                onChange={(_, data) => handleNumCtxChange(data.value ?? null)}
                min={512}
                max={32768}
                step={512}
                disabled={gpuLoading}
              />
            </Field>
          )}

          <div className={styles.buttonGroup}>
            <Button
              appearance="subtle"
              icon={<ArrowClockwise24Regular />}
              onClick={handleAutoDetectGpu}
              disabled={gpuLoading}
              size="small"
            >
              Auto-detect GPU
            </Button>
          </div>

          {gpuStatus && (
            <Text size={200} className={styles.infoText}>
              {gpuEnabled
                ? `GPU layers: ${gpuStatus.numGpu === -1 ? 'All' : gpuStatus.numGpu} • Context: ${numCtx}`
                : 'GPU disabled (CPU mode)'}
            </Text>
          )}
        </div>
      </div>
    </div>
  );
};
