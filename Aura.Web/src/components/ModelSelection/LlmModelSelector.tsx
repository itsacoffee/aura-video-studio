/**
 * Compact LLM Model Selector for feature pages
 * Allows users to select both a provider and model for AI operations
 */

import {
  makeStyles,
  tokens,
  Dropdown,
  Option,
  Text,
  Spinner,
  Tooltip,
  Button,
  shorthands,
  mergeClasses,
} from '@fluentui/react-components';
import { BrainCircuit20Regular, ArrowSync20Regular, Info20Regular } from '@fluentui/react-icons';
import React, { useEffect, useState, useCallback, useMemo } from 'react';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    ...shorthands.padding(tokens.spacingVerticalM, tokens.spacingHorizontalM),
    backgroundColor: tokens.colorNeutralBackground2,
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    ...shorthands.border('1px', 'solid', tokens.colorNeutralStroke2),
  },
  compactContainer: {
    ...shorthands.padding(tokens.spacingVerticalS, tokens.spacingHorizontalS),
    backgroundColor: 'transparent',
    ...shorthands.border('none'),
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  headerIcon: {
    color: tokens.colorBrandForeground1,
  },
  selectors: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
    alignItems: 'flex-end',
  },
  selectorField: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    minWidth: '180px',
    flex: '1',
    maxWidth: '280px',
  },
  fieldLabel: {
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightMedium,
    color: tokens.colorNeutralForeground2,
  },
  dropdown: {
    width: '100%',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
    alignItems: 'center',
  },
  modelInfo: {
    marginTop: tokens.spacingVerticalXS,
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  loadingRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    ...shorthands.padding(tokens.spacingVerticalS, '0'),
  },
  errorText: {
    color: tokens.colorPaletteRedForeground1,
    fontSize: tokens.fontSizeBase200,
  },
});

export interface LlmModelInfo {
  provider: string;
  modelId: string;
  maxTokens: number;
  contextWindow: number;
  aliases: string[];
  isDeprecated: boolean;
  deprecationDate?: string;
  replacementModel?: string;
}

export interface LlmSelection {
  provider: string;
  modelId: string;
}

export interface LlmModelSelectorProps {
  /** Currently selected provider and model */
  value?: LlmSelection;
  /** Callback when selection changes */
  onChange: (selection: LlmSelection) => void;
  /** Label for the selector section */
  label?: string;
  /** Show a more compact version */
  compact?: boolean;
  /** Disable the selector */
  disabled?: boolean;
  /** Show provider info tooltip */
  showProviderInfo?: boolean;
  /** Feature context for analytics (prefixed with _ to indicate optional usage) */
  _featureContext?: string;
  /** Additional CSS class */
  className?: string;
}

// Available LLM providers
const LLM_PROVIDERS = [
  { id: 'OpenAI', name: 'OpenAI (ChatGPT)', requiresApiKey: true },
  { id: 'Anthropic', name: 'Anthropic (Claude)', requiresApiKey: true },
  { id: 'Gemini', name: 'Google Gemini', requiresApiKey: true },
  { id: 'Ollama', name: 'Ollama (Local)', requiresApiKey: false },
  { id: 'Azure', name: 'Azure OpenAI', requiresApiKey: true },
];

export const LlmModelSelector: React.FC<LlmModelSelectorProps> = ({
  value,
  onChange,
  label = 'AI Model',
  compact = false,
  disabled = false,
  showProviderInfo = true,
  _featureContext,
  className,
}) => {
  // featureContext can be used for analytics/telemetry in the future
  void _featureContext;

  const styles = useStyles();
  const [availableModels, setAvailableModels] = useState<Record<string, LlmModelInfo[]>>({});
  const [isLoading, setIsLoading] = useState(false);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedProvider, setSelectedProvider] = useState<string>(value?.provider || '');
  const [selectedModel, setSelectedModel] = useState<string>(value?.modelId || '');

  // Fetch available models from the API
  const fetchModels = useCallback(async (provider?: string) => {
    setIsLoading(true);
    setError(null);

    try {
      const url = provider
        ? `/api/models/available?provider=${encodeURIComponent(provider)}`
        : '/api/models/available';

      const response = await fetch(url);
      if (!response.ok) {
        throw new Error(`Failed to load models: ${response.statusText}`);
      }

      const data = await response.json();
      setAvailableModels(data.providers || {});
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load models';
      setError(errorMessage);
      console.error('[LlmModelSelector] Error fetching models:', err);
    } finally {
      setIsLoading(false);
    }
  }, []);

  // Refresh models for a specific provider (force re-fetch from API)
  const refreshModels = useCallback(async () => {
    if (!selectedProvider) return;

    setIsRefreshing(true);
    setError(null);

    try {
      // First refresh the catalog from the provider
      await fetch('/api/models/llm/refresh', { method: 'POST' });

      // Then fetch the updated models
      await fetchModels(selectedProvider);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to refresh models';
      setError(errorMessage);
      console.error('[LlmModelSelector] Error refreshing models:', err);
    } finally {
      setIsRefreshing(false);
    }
  }, [selectedProvider, fetchModels]);

  // Initial load
  useEffect(() => {
    fetchModels();
  }, [fetchModels]);

  // Update local state when value prop changes
  useEffect(() => {
    if (value) {
      setSelectedProvider(value.provider);
      setSelectedModel(value.modelId);
    }
  }, [value]);

  // Get models for the selected provider
  const providerModels = useMemo(() => {
    if (!selectedProvider) return [];
    return availableModels[selectedProvider] || [];
  }, [availableModels, selectedProvider]);

  // Get info about the selected model
  const selectedModelInfo = useMemo(() => {
    return providerModels.find((m) => m.modelId === selectedModel);
  }, [providerModels, selectedModel]);

  // Handle provider change
  const handleProviderChange = useCallback(
    (providerId: string) => {
      setSelectedProvider(providerId);

      // Auto-select first model for the provider if available
      const models = availableModels[providerId] || [];
      const firstModel = models[0]?.modelId || '';
      setSelectedModel(firstModel);

      onChange({ provider: providerId, modelId: firstModel });
    },
    [availableModels, onChange]
  );

  // Handle model change
  const handleModelChange = useCallback(
    (modelId: string) => {
      setSelectedModel(modelId);
      onChange({ provider: selectedProvider, modelId });
    },
    [selectedProvider, onChange]
  );

  // Get provider description for tooltip
  const getProviderDescription = useCallback((providerId: string): string => {
    switch (providerId) {
      case 'OpenAI':
        return 'OpenAI provides GPT-4, GPT-4o, and other powerful models. Requires API key from platform.openai.com';
      case 'Anthropic':
        return 'Anthropic offers Claude models known for helpfulness and harmlessness. Requires API key from console.anthropic.com';
      case 'Gemini':
        return 'Google Gemini provides advanced multimodal AI capabilities. Requires API key from ai.google.dev';
      case 'Ollama':
        return 'Ollama runs open-source models locally on your machine. No API key required, but Ollama must be running.';
      case 'Azure':
        return 'Azure OpenAI provides enterprise-grade access to OpenAI models. Requires Azure subscription and deployment.';
      default:
        return 'Select an AI provider to use for this feature.';
    }
  }, []);

  const containerClass = mergeClasses(
    styles.container,
    compact && styles.compactContainer,
    className
  );

  if (isLoading && Object.keys(availableModels).length === 0) {
    return (
      <div className={containerClass}>
        <div className={styles.loadingRow}>
          <Spinner size="tiny" />
          <Text size={200}>Loading AI models...</Text>
        </div>
      </div>
    );
  }

  return (
    <div className={containerClass}>
      {!compact && (
        <div className={styles.header}>
          <BrainCircuit20Regular className={styles.headerIcon} />
          <Text weight="semibold">{label}</Text>
          {showProviderInfo && selectedProvider && (
            <Tooltip content={getProviderDescription(selectedProvider)} relationship="description">
              <Info20Regular style={{ cursor: 'help', color: tokens.colorNeutralForeground3 }} />
            </Tooltip>
          )}
        </div>
      )}

      <div className={styles.selectors}>
        <div className={styles.selectorField}>
          {!compact && <Text className={styles.fieldLabel}>Provider</Text>}
          <Dropdown
            className={styles.dropdown}
            placeholder="Select provider"
            value={selectedProvider}
            selectedOptions={selectedProvider ? [selectedProvider] : []}
            onOptionSelect={(_, data) => handleProviderChange(data.optionValue as string)}
            disabled={disabled || isLoading}
          >
            {LLM_PROVIDERS.map((provider) => (
              <Option key={provider.id} value={provider.id} text={provider.name}>
                {provider.name}
                {!provider.requiresApiKey && ' (Local)'}
              </Option>
            ))}
          </Dropdown>
        </div>

        <div className={styles.selectorField}>
          {!compact && <Text className={styles.fieldLabel}>Model</Text>}
          <Dropdown
            className={styles.dropdown}
            placeholder={selectedProvider ? 'Select model' : 'Select provider first'}
            value={selectedModel}
            selectedOptions={selectedModel ? [selectedModel] : []}
            onOptionSelect={(_, data) => handleModelChange(data.optionValue as string)}
            disabled={disabled || isLoading || !selectedProvider || providerModels.length === 0}
          >
            {providerModels.map((model) => (
              <Option
                key={model.modelId}
                value={model.modelId}
                text={model.modelId + (model.isDeprecated ? ' (Deprecated)' : '')}
              >
                {model.modelId}
                {model.isDeprecated && ' ⚠️'}
              </Option>
            ))}
          </Dropdown>
        </div>

        <div className={styles.actions}>
          <Tooltip content="Refresh available models from provider" relationship="label">
            <Button
              appearance="subtle"
              icon={isRefreshing ? <Spinner size="tiny" /> : <ArrowSync20Regular />}
              onClick={refreshModels}
              disabled={disabled || isRefreshing || !selectedProvider}
              size="small"
            />
          </Tooltip>
        </div>
      </div>

      {error && <Text className={styles.errorText}>{error}</Text>}

      {selectedModelInfo && !compact && (
        <Text className={styles.modelInfo}>
          Context: {selectedModelInfo.contextWindow.toLocaleString()} tokens
          {selectedModelInfo.maxTokens > 0 &&
            ` • Max output: ${selectedModelInfo.maxTokens.toLocaleString()} tokens`}
          {selectedModelInfo.isDeprecated && (
            <span style={{ color: tokens.colorPaletteYellowForeground1 }}>
              {' '}
              • Deprecated
              {selectedModelInfo.replacementModel && ` (use ${selectedModelInfo.replacementModel})`}
            </span>
          )}
        </Text>
      )}
    </div>
  );
};

export default LlmModelSelector;
