import { useState, useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Title3,
  Text,
  Dropdown,
  Option,
  Field,
  Tooltip,
  Card,
  Button,
  Spinner,
  Badge,
} from '@fluentui/react-components';
import { Info24Regular, CheckmarkCircle20Filled, Warning20Filled, ArrowDownload20Regular } from '@fluentui/react-icons';
import type { PerStageProviderSelection } from '../../state/providers';
import {
  ScriptProviders,
  TtsProviders,
  VisualsProviders,
  UploadProviders,
} from '../../state/providers';
import { useEnginesStore } from '../../state/engines';

const useStyles = makeStyles({
  section: {
    padding: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalM,
  },
  fieldGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  infoIcon: {
    marginLeft: tokens.spacingHorizontalXS,
    color: tokens.colorBrandForeground1,
    cursor: 'help',
  },
  engineStatus: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    marginTop: tokens.spacingVerticalXS,
    padding: tokens.spacingVerticalXS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusSmall,
  },
  installButton: {
    marginTop: tokens.spacingVerticalXS,
  },
});

interface ProviderSelectionProps {
  selection: PerStageProviderSelection;
  onSelectionChange: (selection: PerStageProviderSelection) => void;
}

export function ProviderSelection({ selection, onSelectionChange }: ProviderSelectionProps) {
  const styles = useStyles();
  const { engineStatuses, fetchEngines, fetchEngineStatus, installEngine, isLoading } = useEnginesStore();
  const [installing, setInstalling] = useState<string | null>(null);

  // Fetch engines on mount
  useEffect(() => {
    fetchEngines();
  }, [fetchEngines]);

  // Map provider values to engine IDs
  const providerToEngineMap: Record<string, string> = {
    'LocalSD': 'stable-diffusion',
    'ComfyUI': 'comfyui',
    'Piper': 'piper',
    'Mimic3': 'mimic3',
  };

  const updateSelection = (stage: keyof PerStageProviderSelection, value: string) => {
    onSelectionChange({
      ...selection,
      [stage]: value,
    });
  };

  const getEngineStatus = (providerValue: string) => {
    const engineId = providerToEngineMap[providerValue];
    if (!engineId) return null;
    
    return engineStatuses.get(engineId);
  };

  const isEngineInstalled = (providerValue: string): boolean => {
    const engineId = providerToEngineMap[providerValue];
    if (!engineId) return true; // Non-engine providers are always "available"
    
    const status = engineStatuses.get(engineId);
    return status?.isInstalled ?? false;
  };

  const handleInstallEngine = async (providerValue: string) => {
    const engineId = providerToEngineMap[providerValue];
    if (!engineId) return;

    setInstalling(engineId);
    try {
      await installEngine(engineId);
      await fetchEngineStatus(engineId);
      // Optionally trigger preflight revalidation here
    } catch (error) {
      console.error(`Failed to install ${engineId}:`, error);
      alert(`Failed to install ${providerValue}. Check console for details.`);
    } finally {
      setInstalling(null);
    }
  };

  const renderEngineStatus = (providerValue: string, label: string) => {
    const engineId = providerToEngineMap[providerValue];
    if (!engineId) return null;

    const status = getEngineStatus(providerValue);
    const installed = isEngineInstalled(providerValue);
    const isInstalling = installing === engineId;

    if (isInstalling) {
      return (
        <div className={styles.engineStatus}>
          <Spinner size="tiny" />
          <Text size={200}>Installing {label}...</Text>
        </div>
      );
    }

    if (!installed) {
      return (
        <>
          <div className={styles.engineStatus}>
            <Warning20Filled color={tokens.colorPaletteYellowForeground1} />
            <Text size={200}>Not installed</Text>
          </div>
          <Button
            className={styles.installButton}
            size="small"
            appearance="primary"
            icon={<ArrowDownload20Regular />}
            onClick={() => handleInstallEngine(providerValue)}
            disabled={isLoading}
          >
            Install & Validate
          </Button>
        </>
      );
    }

    if (status?.isRunning) {
      return (
        <div className={styles.engineStatus}>
          <CheckmarkCircle20Filled color={tokens.colorPaletteGreenForeground1} />
          <Text size={200}>Running {status.isHealthy ? '(Healthy)' : '(Starting...)'}</Text>
        </div>
      );
    }

    return (
      <div className={styles.engineStatus}>
        <Badge appearance="tint" color="success">Installed</Badge>
        <Text size={200}>Ready to use</Text>
      </div>
    );
  };

  return (
    <Card className={styles.section}>
      <Title3>
        Provider Selection (Per-Stage)
        <Tooltip content="Choose which provider to use for each stage of video generation" relationship="label">
          <Info24Regular className={styles.infoIcon} />
        </Tooltip>
      </Title3>
      <Text size={200} style={{ marginBottom: tokens.spacingVerticalM }}>
        Override the profile defaults by selecting specific providers for each stage
      </Text>
      
      <div className={styles.fieldGroup}>
        <Field
          label={
            <div style={{ display: 'flex', alignItems: 'center' }}>
              Script LLM Provider
              <Tooltip content="Which AI model to use for script generation" relationship="label">
                <Info24Regular className={styles.infoIcon} />
              </Tooltip>
            </div>
          }
        >
          <Dropdown
            value={selection.script || 'Auto'}
            onOptionSelect={(_, data) => updateSelection('script', data.optionValue as string)}
          >
            <Option value="Auto">Auto (Use Profile Default)</Option>
            {ScriptProviders.map((provider) => (
              <Option key={provider.value} value={provider.value}>
                {provider.label}
              </Option>
            ))}
          </Dropdown>
        </Field>

        <Field
          label={
            <div style={{ display: 'flex', alignItems: 'center' }}>
              TTS Provider
              <Tooltip content="Which text-to-speech engine to use for voice narration" relationship="label">
                <Info24Regular className={styles.infoIcon} />
              </Tooltip>
            </div>
          }
        >
          <Dropdown
            value={selection.tts || 'Auto'}
            onOptionSelect={(_, data) => updateSelection('tts', data.optionValue as string)}
          >
            <Option value="Auto">Auto (Use Profile Default)</Option>
            {TtsProviders.map((provider) => (
              <Option key={provider.value} value={provider.value}>
                {provider.label}
              </Option>
            ))}
          </Dropdown>
          {(selection.tts === 'Piper' || selection.tts === 'Mimic3') && 
            renderEngineStatus(selection.tts, selection.tts)}
        </Field>

        <Field
          label={
            <div style={{ display: 'flex', alignItems: 'center' }}>
              Visuals Provider
              <Tooltip content="Which image provider to use for generating/sourcing visuals" relationship="label">
                <Info24Regular className={styles.infoIcon} />
              </Tooltip>
            </div>
          }
        >
          <Dropdown
            value={selection.visuals || 'Auto'}
            onOptionSelect={(_, data) => updateSelection('visuals', data.optionValue as string)}
          >
            <Option value="Auto">Auto (Use Profile Default)</Option>
            {VisualsProviders.map((provider) => (
              <Option key={provider.value} value={provider.value}>
                {provider.label}
              </Option>
            ))}
          </Dropdown>
          {(selection.visuals === 'LocalSD' || selection.visuals === 'ComfyUI') && 
            renderEngineStatus(selection.visuals, selection.visuals)}
        </Field>

        <Field
          label={
            <div style={{ display: 'flex', alignItems: 'center' }}>
              Upload Provider
              <Tooltip content="Whether to automatically upload the finished video" relationship="label">
                <Info24Regular className={styles.infoIcon} />
              </Tooltip>
            </div>
          }
        >
          <Dropdown
            value={selection.upload || 'Auto'}
            onOptionSelect={(_, data) => updateSelection('upload', data.optionValue as string)}
          >
            <Option value="Auto">Auto (Use Profile Default)</Option>
            {UploadProviders.map((provider) => (
              <Option key={provider.value} value={provider.value}>
                {provider.label}
              </Option>
            ))}
          </Dropdown>
        </Field>
      </div>
    </Card>
  );
}
