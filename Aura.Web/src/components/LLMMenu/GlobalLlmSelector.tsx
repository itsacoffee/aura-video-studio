/**
 * Global LLM Selector Component
 * Compact LLM model selector for the top bar that's available on every page
 */

import {
  Button,
  Dropdown,
  Option,
  makeStyles,
  tokens,
  Tooltip,
  Spinner,
  shorthands,
} from '@fluentui/react-components';
import { BrainCircuit20Regular, ArrowSync20Regular } from '@fluentui/react-icons';
import React, { useEffect, useState, useCallback, useMemo } from 'react';
import { useGlobalLlmStore } from '../../state/globalLlmStore';
import type { LlmModelInfo } from '../ModelSelection/LlmModelSelector';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  selectorGroup: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  dropdown: {
    minWidth: '140px',
    maxWidth: '200px',
  },
  compactDropdown: {
    minWidth: '120px',
    maxWidth: '160px',
  },
  refreshButton: {
    minWidth: 'auto',
    ...shorthands.padding(tokens.spacingHorizontalXS),
  },
  loadingSpinner: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
});

// Available LLM providers
const LLM_PROVIDERS = [
  { id: 'OpenAI', name: 'OpenAI', requiresApiKey: true },
  { id: 'Anthropic', name: 'Anthropic', requiresApiKey: true },
  { id: 'Gemini', name: 'Gemini', requiresApiKey: true },
  { id: 'Ollama', name: 'Ollama', requiresApiKey: false },
  { id: 'Azure', name: 'Azure', requiresApiKey: true },
];

export function GlobalLlmSelector() {
  const styles = useStyles();
  const { selection, setSelection } = useGlobalLlmStore();
  const [availableModels, setAvailableModels] = useState<Record<string, LlmModelInfo[]>>({});
  const [isLoading, setIsLoading] = useState(false);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const selectedProvider = selection?.provider || '';
  const selectedModel = selection?.modelId || '';

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
      console.error('[GlobalLlmSelector] Error fetching models:', err);
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
      console.error('[GlobalLlmSelector] Error refreshing models:', err);
    } finally {
      setIsRefreshing(false);
    }
  }, [selectedProvider, fetchModels]);

  // Initial load
  useEffect(() => {
    fetchModels();
  }, [fetchModels]);

  // Get models for the selected provider
  const providerModels = useMemo(() => {
    if (!selectedProvider) return [];
    return availableModels[selectedProvider] || [];
  }, [availableModels, selectedProvider]);

  // Handle provider change
  const handleProviderChange = useCallback(
    (providerId: string) => {
      // Auto-select first model for the provider if available
      const models = availableModels[providerId] || [];
      const firstModel = models[0]?.modelId || '';
      setSelection({ provider: providerId, modelId: firstModel });
    },
    [availableModels, setSelection]
  );

  // Handle model change
  const handleModelChange = useCallback(
    (modelId: string) => {
      if (selectedProvider) {
        setSelection({ provider: selectedProvider, modelId });
      }
    },
    [selectedProvider, setSelection]
  );

  // Get display text for provider
  const getProviderDisplayText = (providerId: string) => {
    const provider = LLM_PROVIDERS.find((p) => p.id === providerId);
    return provider?.name || providerId;
  };

  // Get display text for model (truncate if too long)
  const getModelDisplayText = (modelId: string) => {
    if (modelId.length > 20) {
      return modelId.substring(0, 17) + '...';
    }
    return modelId;
  };

  return (
    <div className={styles.container}>
      <Tooltip content="AI Model Selection" relationship="label">
        <BrainCircuit20Regular style={{ color: tokens.colorNeutralForeground3 }} />
      </Tooltip>
      <div className={styles.selectorGroup}>
        <Dropdown
          className={styles.compactDropdown}
          placeholder="Provider"
          value={selectedProvider ? getProviderDisplayText(selectedProvider) : 'Provider'}
          selectedOptions={selectedProvider ? [selectedProvider] : []}
          onOptionSelect={(_, data) => handleProviderChange(data.optionValue as string)}
          disabled={isLoading}
        >
          {LLM_PROVIDERS.map((provider) => (
            <Option key={provider.id} value={provider.id} text={provider.name}>
              {provider.name}
              {!provider.requiresApiKey && ' (Local)'}
            </Option>
          ))}
        </Dropdown>

        <Dropdown
          className={styles.compactDropdown}
          placeholder={selectedProvider ? 'Model' : 'Select provider'}
          value={selectedModel ? getModelDisplayText(selectedModel) : 'Model'}
          selectedOptions={selectedModel ? [selectedModel] : []}
          onOptionSelect={(_, data) => handleModelChange(data.optionValue as string)}
          disabled={isLoading || !selectedProvider || providerModels.length === 0}
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

        {selectedProvider && (
          <Tooltip content="Refresh available models from provider" relationship="label">
            <Button
              appearance="subtle"
              icon={isRefreshing ? <Spinner size="tiny" /> : <ArrowSync20Regular />}
              onClick={refreshModels}
              disabled={isRefreshing || !selectedProvider}
              size="small"
              className={styles.refreshButton}
            />
          </Tooltip>
        )}
      </div>
      {error && (
        <Tooltip content={error} relationship="description">
          <span style={{ color: tokens.colorPaletteRedForeground1, fontSize: '10px' }}>⚠</span>
        </Tooltip>
      )}
    </div>
  );
}
