/**
 * Global LLM Selector Component
 * Compact LLM model selector for the top bar that's available on every page
 *
 * Features:
 * - Fetches providers and models dynamically from API
 * - Properly syncs with persisted Zustand store state
 * - Validates persisted model is still available
 * - Shows loading states and error recovery options
 * - Handles all providers (OpenAI, Anthropic, Gemini, Azure, Ollama)
 * - Fetches Ollama models dynamically from local Ollama installation
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
  Badge,
  Text,
} from '@fluentui/react-components';
import {
  BrainCircuit20Regular,
  ArrowSync20Regular,
  ErrorCircle20Regular,
  Warning20Regular,
} from '@fluentui/react-icons';
import React, { useEffect, useState, useCallback, useMemo, useRef } from 'react';
import { useGlobalLlmStore } from '../../state/globalLlmStore';
import type { ModelValidationStatus } from '../../state/globalLlmStore';
import type { OllamaModel } from '../../types/api-v1';
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
  errorIndicator: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXXS,
    cursor: 'pointer',
  },
  modelBadge: {
    maxWidth: '150px',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  providerStatus: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
    marginLeft: tokens.spacingHorizontalXXS,
  },
});

interface ProviderInfo {
  id: string;
  name: string;
  requiresApiKey: boolean;
  modelCount?: number;
  isAvailable?: boolean;
}

// Providers that don't require an API key to function
const LOCAL_PROVIDERS = new Set(['Ollama', 'RuleBased']);

// Human-readable display names for providers
const PROVIDER_DISPLAY_NAMES: Record<string, string> = {
  RuleBased: 'Rule-Based (Offline)',
};

// Fallback model for RuleBased provider - always available offline
const RULE_BASED_FALLBACK_MODEL: LlmModelInfo = {
  modelId: 'rule-based-script-generator',
  provider: 'RuleBased',
  maxTokens: 4096,
  contextWindow: 4096,
  aliases: [],
  isDeprecated: false,
};

// Fallback models when API is unavailable or returns empty
const FALLBACK_MODELS: Record<string, LlmModelInfo[]> = {
  RuleBased: [RULE_BASED_FALLBACK_MODEL],
  Ollama: [], // Empty - user needs to configure and refresh
};

// Fallback provider info when API is unavailable
const FALLBACK_PROVIDER_INFO: ProviderInfo = {
  id: 'RuleBased',
  name: 'Rule-Based (Offline)',
  requiresApiKey: false,
  modelCount: 1,
  isAvailable: true,
};

// All known provider IDs
const ALL_PROVIDER_IDS = ['OpenAI', 'Anthropic', 'Gemini', 'Ollama', 'Azure', 'RuleBased'];

/** Get display name for a provider */
function getProviderDisplayName(providerId: string): string {
  return PROVIDER_DISPLAY_NAMES[providerId] || providerId;
}

/** Check if a provider requires an API key */
function providerRequiresApiKey(providerId: string): boolean {
  return !LOCAL_PROVIDERS.has(providerId);
}

// Default Ollama URL
const DEFAULT_OLLAMA_URL = 'http://127.0.0.1:11434';

export function GlobalLlmSelector() {
  const styles = useStyles();
  const { selection, setSelection, setModelValidation } = useGlobalLlmStore();
  const [availableModels, setAvailableModels] = useState<Record<string, LlmModelInfo[]>>({});
  const [providers, setProviders] = useState<ProviderInfo[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isInitialized, setIsInitialized] = useState(false);
  const initRef = useRef(false);

  // Ollama-specific state
  const [ollamaModels, setOllamaModels] = useState<OllamaModel[]>([]);
  const [isLoadingOllamaModels, setIsLoadingOllamaModels] = useState(false);
  const [savedOllamaModel, setSavedOllamaModel] = useState<string | null>(null);
  const [ollamaUrl, setOllamaUrl] = useState<string>(DEFAULT_OLLAMA_URL);
  const [ollamaError, setOllamaError] = useState<string | null>(null);

  const selectedProvider = selection?.provider || '';
  const selectedModel = selection?.modelId || '';

  // Fetch Ollama URL from provider paths
  const fetchOllamaUrl = useCallback(async () => {
    try {
      const response = await fetch('/api/providers/paths/load');
      if (response.ok) {
        const data = await response.json();
        const url = data.ollamaUrl || DEFAULT_OLLAMA_URL;
        setOllamaUrl(url);
        return url;
      }
    } catch (err) {
      console.warn('[GlobalLlmSelector] Failed to fetch Ollama URL, using default:', err);
    }
    return DEFAULT_OLLAMA_URL;
  }, []);

  // Fetch the global LLM selection from the backend
  // Backend selection takes priority over stale localStorage data on initialization
  const fetchGlobalLlmSelection = useCallback(async () => {
    try {
      const response = await fetch('/api/settings/llm/selection');
      if (response.ok) {
        const data = await response.json();
        if (data.success && data.provider) {
          // Always update store with backend selection - backend is source of truth
          // This ensures fresh builds reflect the actual configured model, not stale localStorage
          setSelection({
            provider: data.provider,
            modelId: data.modelId || '',
          });
          console.info(
            '[GlobalLlmSelector] Loaded global LLM selection from backend:',
            data.provider,
            data.modelId
          );
          return { provider: data.provider, modelId: data.modelId };
        }
      }
    } catch (err) {
      console.warn('[GlobalLlmSelector] Failed to fetch global LLM selection:', err);
    }
    return null;
  }, [setSelection]);

  // Fetch the currently saved Ollama model setting
  const fetchSavedOllamaModel = useCallback(async () => {
    try {
      const response = await fetch('/api/settings/ollama/model');
      if (response.ok) {
        const data = await response.json();
        if (data.success && data.model) {
          setSavedOllamaModel(data.model);
          return data.model;
        }
      }
    } catch (err) {
      console.warn('[GlobalLlmSelector] Failed to fetch saved Ollama model:', err);
    }
    return null;
  }, []);

  // Save Ollama model selection to backend
  const saveOllamaModel = useCallback(async (model: string) => {
    try {
      const response = await fetch('/api/settings/ollama/model', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ model }),
      });
      if (response.ok) {
        setSavedOllamaModel(model);
        console.info('[GlobalLlmSelector] Ollama model saved:', model);
      } else {
        console.warn('[GlobalLlmSelector] Failed to save Ollama model');
      }
    } catch (err) {
      console.error('[GlobalLlmSelector] Error saving Ollama model:', err);
    }
  }, []);

  // Fetch Ollama models from the local Ollama installation
  const fetchOllamaModels = useCallback(
    async (url?: string): Promise<{ success: boolean; models: OllamaModel[]; error?: string }> => {
      setIsLoadingOllamaModels(true);
      setOllamaError(null);
      try {
        const ollamaEndpointUrl = url || ollamaUrl;
        const response = await fetch(
          `/api/engines/ollama/models?url=${encodeURIComponent(ollamaEndpointUrl)}`
        );

        const data = await response.json();

        if (response.ok) {
          const models: OllamaModel[] = data.models || [];
          setOllamaModels(models);
          console.info('[GlobalLlmSelector] Fetched Ollama models:', models.length);

          if (models.length === 0) {
            const noModelsError =
              'No Ollama models installed. Run "ollama pull <model-name>" to download a model.';
            setOllamaError(noModelsError);
            return { success: false, models: [], error: noModelsError };
          }

          return { success: true, models };
        } else {
          // Parse error from response
          const errorMessage =
            data.error ||
            `Ollama not reachable (status ${response.status}). Ensure Ollama is running.`;
          console.warn('[GlobalLlmSelector] Failed to fetch Ollama models:', errorMessage);
          setOllamaModels([]);
          setOllamaError(errorMessage);
          return { success: false, models: [], error: errorMessage };
        }
      } catch (err) {
        const errorMessage = 'Cannot connect to Ollama. Ensure Ollama is running on your system.';
        console.warn('[GlobalLlmSelector] Error fetching Ollama models:', err);
        setOllamaModels([]);
        setOllamaError(errorMessage);
        return { success: false, models: [], error: errorMessage };
      } finally {
        setIsLoadingOllamaModels(false);
      }
    },
    [ollamaUrl]
  );

  // Fetch available models from the API
  const fetchModels = useCallback(async (showLoadingState = true) => {
    if (showLoadingState) {
      setIsLoading(true);
    }
    setError(null);

    try {
      const response = await fetch('/api/models/available');

      // Parse the response even if status is not OK - API now returns structured data
      const data = await response.json();

      if (!response.ok) {
        // Log the issue but try to use any data returned
        console.warn('[GlobalLlmSelector] API returned non-OK status:', response.status, data);
      }

      const providersData = data.providers || {};

      // If API returned empty providers but we got a response, the backend is up but has no configured models
      if (Object.keys(providersData).length === 0) {
        console.info('[GlobalLlmSelector] No providers from API, using fallback list');
        // Use fallback models for offline providers
        const fallbackModelsWithRuleBased = {
          ...FALLBACK_MODELS,
          RuleBased: [RULE_BASED_FALLBACK_MODEL], // Always include RuleBased
        };
        setAvailableModels(fallbackModelsWithRuleBased);

        // Build provider list from all known providers with fallback availability
        const fallbackProviderList: ProviderInfo[] = ALL_PROVIDER_IDS.map((providerId) => {
          const isLocalProvider = LOCAL_PROVIDERS.has(providerId);
          const fallbackModels = fallbackModelsWithRuleBased[providerId] || [];
          return {
            id: providerId,
            name: getProviderDisplayName(providerId),
            requiresApiKey: providerRequiresApiKey(providerId),
            modelCount: fallbackModels.length,
            // Local providers are always available, others need models
            isAvailable: isLocalProvider || fallbackModels.length > 0,
          };
        });
        setProviders(fallbackProviderList);
        return fallbackModelsWithRuleBased;
      }

      // Ensure RuleBased is always in the available models with fallback
      const modelsWithRuleBased = {
        ...providersData,
        RuleBased:
          providersData['RuleBased']?.length > 0
            ? providersData['RuleBased']
            : [RULE_BASED_FALLBACK_MODEL],
      };
      setAvailableModels(modelsWithRuleBased);

      // Build provider list dynamically from API response
      const providerList: ProviderInfo[] = Object.keys(modelsWithRuleBased).map((providerId) => {
        const models = modelsWithRuleBased[providerId] || [];
        const isLocalProvider = LOCAL_PROVIDERS.has(providerId);
        return {
          id: providerId,
          name: getProviderDisplayName(providerId),
          requiresApiKey: providerRequiresApiKey(providerId),
          modelCount: models.length,
          // Local providers are available even with 0 models from API (they work offline)
          isAvailable: isLocalProvider || models.length > 0,
        };
      });

      // Add common providers that might not have models yet
      for (const provider of ALL_PROVIDER_IDS) {
        if (!providerList.some((p) => p.id === provider)) {
          const isLocalProvider = LOCAL_PROVIDERS.has(provider);
          providerList.push({
            id: provider,
            name: getProviderDisplayName(provider),
            requiresApiKey: providerRequiresApiKey(provider),
            modelCount: 0,
            // Local providers are always available
            isAvailable: isLocalProvider,
          });
        }
      }

      // Sort providers: available first, then by local providers, then alphabetically
      providerList.sort((a, b) => {
        if (a.isAvailable && !b.isAvailable) return -1;
        if (!a.isAvailable && b.isAvailable) return 1;
        // Among unavailable, put local providers first
        if (!a.isAvailable && !b.isAvailable) {
          if (!a.requiresApiKey && b.requiresApiKey) return -1;
          if (a.requiresApiKey && !b.requiresApiKey) return 1;
        }
        return a.name.localeCompare(b.name);
      });

      setProviders(providerList);

      return modelsWithRuleBased;
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load models';
      setError(errorMessage);
      console.error('[GlobalLlmSelector] Error fetching models:', err);

      // Provide minimal fallback even on network error - RuleBased is always available
      const fallbackProviders: ProviderInfo[] = ALL_PROVIDER_IDS.map((providerId) => {
        const isLocalProvider = LOCAL_PROVIDERS.has(providerId);
        const fallbackModels = FALLBACK_MODELS[providerId] || [];
        return {
          id: providerId,
          name: getProviderDisplayName(providerId),
          requiresApiKey: providerRequiresApiKey(providerId),
          modelCount: fallbackModels.length,
          isAvailable: isLocalProvider || fallbackModels.length > 0,
        };
      });
      setProviders(fallbackProviders);
      setAvailableModels(FALLBACK_MODELS);

      return null;
    } finally {
      if (showLoadingState) {
        setIsLoading(false);
      }
    }
  }, []);

  // Validate and sync persisted selection with available models
  const validateAndSyncSelection = useCallback(
    async (modelsData: Record<string, LlmModelInfo[]>) => {
      if (!selection?.provider || !selection?.modelId) {
        // No selection to validate
        return;
      }

      const providerModels = modelsData[selection.provider] || [];

      // Check if the selected model is still available
      const modelExists = providerModels.some((m) => m.modelId === selection.modelId);

      if (!modelExists && providerModels.length > 0) {
        // Model no longer available, but provider has other models - select first available
        console.info(
          `[GlobalLlmSelector] Model "${selection.modelId}" no longer available for ${selection.provider}, selecting first available model`
        );
        setSelection({
          provider: selection.provider,
          modelId: providerModels[0].modelId,
        });
      } else if (!modelExists && providerModels.length === 0) {
        // Provider has no models available
        console.warn(
          `[GlobalLlmSelector] Provider "${selection.provider}" has no available models`
        );
        // Keep the selection but show a warning (don't clear it automatically)
      }
    },
    [selection, setSelection]
  );

  // Initial load and sync with persisted selection
  useEffect(() => {
    if (initRef.current) return;
    initRef.current = true;

    const initializeSelector = async () => {
      setIsLoading(true);
      try {
        // Fetch Ollama URL first
        const url = await fetchOllamaUrl();

        // Fetch global LLM selection from backend - backend is source of truth
        await fetchGlobalLlmSelection();

        // Fetch saved Ollama model setting
        await fetchSavedOllamaModel();

        // Fetch general models data
        const modelsData = await fetchModels(false);
        if (modelsData) {
          await validateAndSyncSelection(modelsData);
        }

        // If Ollama is the current provider, fetch Ollama models
        if (selection?.provider === 'Ollama') {
          await fetchOllamaModels(url);
        }
      } finally {
        setIsLoading(false);
        setIsInitialized(true);
      }
    };

    initializeSelector();
  }, [
    fetchModels,
    validateAndSyncSelection,
    fetchOllamaUrl,
    fetchGlobalLlmSelection,
    fetchSavedOllamaModel,
    fetchOllamaModels,
    selection?.provider,
  ]);

  // Fetch Ollama models when provider changes to Ollama
  useEffect(() => {
    if (selectedProvider === 'Ollama' && isInitialized) {
      fetchOllamaModels();
    }
  }, [selectedProvider, isInitialized, fetchOllamaModels]);

  // Helper function to get the default Ollama model
  const getDefaultOllamaModel = useCallback(
    (models: OllamaModel[]): string => {
      return savedOllamaModel || (models.length > 0 ? models[0].name : '');
    },
    [savedOllamaModel]
  );

  // Auto-select first Ollama model when models are loaded and no model is selected
  useEffect(() => {
    if (selectedProvider === 'Ollama' && ollamaModels.length > 0 && !selectedModel) {
      const firstModel = getDefaultOllamaModel(ollamaModels);
      if (firstModel) {
        setSelection({ provider: 'Ollama', modelId: firstModel });
        saveOllamaModel(firstModel);
        console.info('[GlobalLlmSelector] Auto-selected Ollama model:', firstModel);
      }
    }
  }, [
    selectedProvider,
    ollamaModels,
    selectedModel,
    getDefaultOllamaModel,
    setSelection,
    saveOllamaModel,
  ]);

  // Auto-select first model for API-based providers when provider is set but no model
  useEffect(() => {
    // Skip if it's Ollama (handled separately above), or if we already have a model
    if (selectedProvider === 'Ollama' || !selectedProvider || selectedModel) {
      return;
    }

    const models = availableModels[selectedProvider] || [];
    if (models.length > 0 && !selectedModel) {
      const firstModel = models[0].modelId;
      if (firstModel) {
        setSelection({ provider: selectedProvider, modelId: firstModel });
        console.info(
          '[GlobalLlmSelector] Auto-selected model for',
          selectedProvider,
          ':',
          firstModel
        );

        // Save to backend
        fetch('/api/settings/llm/selection', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ provider: selectedProvider, modelId: firstModel }),
        })
          .then((response) => {
            if (!response.ok) {
              console.warn(
                '[GlobalLlmSelector] Failed to save selection, status:',
                response.status
              );
            }
          })
          .catch((err) => console.warn('[GlobalLlmSelector] Failed to save selection:', err));
      }
    }
  }, [selectedProvider, selectedModel, availableModels, setSelection]);

  // Refresh models for a specific provider (force re-fetch from API)
  const refreshModels = useCallback(async () => {
    setIsRefreshing(true);
    setError(null);
    setOllamaError(null);

    try {
      // If Ollama is selected, refresh Ollama models specifically
      if (selectedProvider === 'Ollama') {
        const result = await fetchOllamaModels();
        if (!result.success && result.error) {
          setError(result.error);
        } else if (result.models.length > 0) {
          // Check if currently selected model exists in the refreshed list
          const modelExists = result.models.some((m) => m.name === selectedModel);
          if (!selectedModel || !modelExists) {
            // Auto-select first model if none selected or current model doesn't exist
            const firstModel = getDefaultOllamaModel(result.models);
            if (firstModel) {
              setSelection({ provider: 'Ollama', modelId: firstModel });
              saveOllamaModel(firstModel);
              console.info(
                '[GlobalLlmSelector] Auto-selected Ollama model after refresh:',
                firstModel
              );
            }
          }
        }
      } else {
        // First refresh the catalog from the provider
        await fetch('/api/models/llm/refresh', { method: 'POST' });

        // Then fetch the updated models
        const modelsData = await fetchModels(false);
        if (modelsData) {
          await validateAndSyncSelection(modelsData);
        }
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to refresh models';
      setError(errorMessage);
      console.error('[GlobalLlmSelector] Error refreshing models:', err);
    } finally {
      setIsRefreshing(false);
    }
  }, [
    fetchModels,
    validateAndSyncSelection,
    selectedProvider,
    fetchOllamaModels,
    getDefaultOllamaModel,
    selectedModel,
    setSelection,
    saveOllamaModel,
  ]);

  // Get models for the selected provider
  const providerModels = useMemo(() => {
    if (!selectedProvider) return [];

    // For Ollama, use the dynamically fetched Ollama models
    if (selectedProvider === 'Ollama') {
      // Convert Ollama models to LlmModelInfo format
      const ollamaLlmModels: LlmModelInfo[] = ollamaModels.map((m) => ({
        modelId: m.name,
        provider: 'Ollama',
        maxTokens: 4096,
        contextWindow: 4096,
        aliases: [],
        isDeprecated: false,
      }));

      // If we have Ollama models, return them
      if (ollamaLlmModels.length > 0) {
        return ollamaLlmModels;
      }

      // Fallback: if no models but we have a saved model, show it as an option
      if (savedOllamaModel) {
        return [
          {
            modelId: savedOllamaModel,
            provider: 'Ollama',
            maxTokens: 4096,
            contextWindow: 4096,
            aliases: [],
            isDeprecated: false,
          },
        ];
      }

      return [];
    }

    return availableModels[selectedProvider] || [];
  }, [availableModels, selectedProvider, ollamaModels, savedOllamaModel]);

  // Check if selected model exists in current provider's models
  const selectedModelExists = useMemo(() => {
    if (!selectedModel || !selectedProvider) return false;

    // For Ollama, handle async loading state
    if (selectedProvider === 'Ollama') {
      const existsInFetched = providerModels.some((m) => m.modelId === selectedModel);
      if (existsInFetched) return true;

      // Consider model as valid during loading or when we haven't fetched yet
      // This prevents the "Model not found" warning from showing prematurely
      // Cases where we should assume the model is valid:
      // 1. We're actively loading Ollama models
      // 2. We have no Ollama models yet and no error (initial state or hasn't fetched yet)
      // 3. The selected model matches the saved Ollama model (persisted preference)
      const isLoadingOrNotFetchedYet =
        isLoadingOllamaModels || (ollamaModels.length === 0 && !ollamaError);
      if (isLoadingOrNotFetchedYet || selectedModel === savedOllamaModel) {
        console.info(
          '[GlobalLlmSelector] Ollama model check: assuming valid during loading/initial state',
          { selectedModel, isLoadingOllamaModels, ollamaModelsCount: ollamaModels.length }
        );
        return true;
      }
    }

    return providerModels.some((m) => m.modelId === selectedModel);
  }, [
    selectedModel,
    selectedProvider,
    providerModels,
    savedOllamaModel,
    isLoadingOllamaModels,
    ollamaModels.length,
    ollamaError,
  ]);

  // Update model validation status in the global store
  // This allows other components (like BrainstormInput) to check if the model is valid
  useEffect(() => {
    // Skip validation updates during initial loading
    if (!isInitialized || (selectedProvider === 'Ollama' && isLoadingOllamaModels)) {
      return;
    }

    // Build validation status
    const validationStatus: ModelValidationStatus = {
      isValidated: true,
      isValid: selectedModelExists,
      lastValidatedAt: Date.now(),
      errorMessage: undefined,
    };

    // Add error message for Ollama when model is not found
    if (!selectedModelExists && selectedProvider === 'Ollama' && selectedModel) {
      validationStatus.errorMessage = `Model '${selectedModel}' is not installed in Ollama. Please run \`ollama pull ${selectedModel}\` to install it, or select a different model.`;
    } else if (!selectedModelExists && selectedModel) {
      validationStatus.errorMessage = `Model '${selectedModel}' is not available. Please select a different model.`;
    }

    setModelValidation(validationStatus);
  }, [
    selectedModelExists,
    selectedModel,
    selectedProvider,
    isInitialized,
    isLoadingOllamaModels,
    setModelValidation,
  ]);

  // Auto-select first available model when current model is not found (auto-recovery)
  useEffect(() => {
    // Skip during initial loading or if model is valid
    if (
      !isInitialized ||
      selectedModelExists ||
      isLoading ||
      isRefreshing ||
      (selectedProvider === 'Ollama' && isLoadingOllamaModels)
    ) {
      return;
    }

    // Skip if no provider or no model selected
    if (!selectedProvider || !selectedModel) {
      return;
    }

    // Check if we have alternative models to select
    const models = providerModels;
    if (models.length > 0) {
      const firstModel = models[0].modelId;
      console.info(
        `[GlobalLlmSelector] Auto-selecting first available model: ${firstModel} (was: ${selectedModel})`
      );
      setSelection({ provider: selectedProvider, modelId: firstModel });

      // For Ollama, also save the model selection to backend
      if (selectedProvider === 'Ollama') {
        saveOllamaModel(firstModel);
      }
    }
  }, [
    isInitialized,
    selectedModelExists,
    selectedModel,
    selectedProvider,
    providerModels,
    isLoading,
    isRefreshing,
    isLoadingOllamaModels,
    setSelection,
    saveOllamaModel,
  ]);

  // Handle provider change
  const handleProviderChange = useCallback(
    (providerId: string) => {
      // For Ollama, use the saved Ollama model or the first fetched model
      if (providerId === 'Ollama') {
        // Use saved model if available, otherwise first fetched model
        const defaultModel =
          savedOllamaModel || (ollamaModels.length > 0 ? ollamaModels[0].name : '');
        setSelection({ provider: providerId, modelId: defaultModel });
        return;
      }

      const models = availableModels[providerId] || [];
      const firstModel = models[0]?.modelId || '';
      setSelection({ provider: providerId, modelId: firstModel });
    },
    [availableModels, setSelection, savedOllamaModel, ollamaModels]
  );

  // Handle model change
  const handleModelChange = useCallback(
    (modelId: string) => {
      if (selectedProvider) {
        setSelection({ provider: selectedProvider, modelId });

        // For Ollama, also save the model selection to backend
        if (selectedProvider === 'Ollama') {
          saveOllamaModel(modelId);
        }
      }
    },
    [selectedProvider, setSelection, saveOllamaModel]
  );

  // Get display text for provider with model count
  const getProviderDisplayText = useCallback(
    (providerId: string): string => {
      const provider = providers.find((p) => p.id === providerId);
      return provider?.name || providerId;
    },
    [providers]
  );

  // Get display text for model (truncate if too long)
  const getModelDisplayText = useCallback((modelId: string): string => {
    if (!modelId) return 'Select model';
    if (modelId.length > 20) {
      return modelId.substring(0, 17) + '...';
    }
    return modelId;
  }, []);

  // Get Ollama model display text with size
  const getOllamaModelDisplayText = useCallback(
    (modelId: string): string => {
      const model = ollamaModels.find((m) => m.name === modelId);
      if (model) {
        return `${modelId} (${model.sizeGB.toFixed(2)} GB)`;
      }
      return modelId;
    },
    [ollamaModels]
  );

  // Get provider option text with status
  const getProviderOptionText = useCallback((provider: ProviderInfo): string => {
    let text = provider.name;
    if (!provider.requiresApiKey) {
      text += ' (Local)';
    }
    if (provider.modelCount !== undefined && provider.modelCount > 0) {
      text += ` [${provider.modelCount}]`;
    }
    return text;
  }, []);

  // Show loading state during initialization
  if (!isInitialized && isLoading) {
    return (
      <div className={styles.container}>
        <Tooltip content="Loading AI models..." relationship="label">
          <div className={styles.loadingSpinner}>
            <Spinner size="tiny" />
            <Text size={200}>Loading...</Text>
          </div>
        </Tooltip>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <Tooltip content="AI Model Selection" relationship="label">
        <BrainCircuit20Regular style={{ color: tokens.colorNeutralForeground3 }} />
      </Tooltip>

      <div className={styles.selectorGroup}>
        {/* Provider Dropdown */}
        <Dropdown
          className={styles.compactDropdown}
          placeholder="Provider"
          value={selectedProvider ? getProviderDisplayText(selectedProvider) : ''}
          selectedOptions={selectedProvider ? [selectedProvider] : []}
          onOptionSelect={(_, data) => {
            if (data.optionValue) {
              handleProviderChange(data.optionValue as string);
            }
          }}
          disabled={isLoading || isRefreshing}
        >
          {providers.map((provider) => (
            <Option
              key={provider.id}
              value={provider.id}
              text={provider.name}
              disabled={!provider.isAvailable && provider.modelCount === 0}
            >
              <span>{getProviderOptionText(provider)}</span>
              {!provider.isAvailable && provider.modelCount === 0 && (
                <span className={styles.providerStatus}> (unavailable)</span>
              )}
            </Option>
          ))}
        </Dropdown>

        {/* Model Dropdown */}
        <Dropdown
          className={styles.compactDropdown}
          placeholder={
            selectedProvider === 'Ollama' && isLoadingOllamaModels
              ? 'Loading models...'
              : selectedProvider
                ? 'Select model'
                : 'Select provider first'
          }
          value={selectedModel ? getModelDisplayText(selectedModel) : ''}
          selectedOptions={selectedModel && selectedModelExists ? [selectedModel] : []}
          onOptionSelect={(_, data) => {
            if (data.optionValue) {
              handleModelChange(data.optionValue as string);
            }
          }}
          disabled={
            isLoading ||
            isRefreshing ||
            !selectedProvider ||
            (selectedProvider === 'Ollama' ? isLoadingOllamaModels : providerModels.length === 0)
          }
        >
          {/* Loading state for Ollama */}
          {selectedProvider === 'Ollama' && isLoadingOllamaModels && (
            <Option key="loading" value="" disabled text="Loading...">
              Loading Ollama models...
            </Option>
          )}
          {/* No models message - show specific error for Ollama */}
          {providerModels.length === 0 &&
            selectedProvider &&
            !(selectedProvider === 'Ollama' && isLoadingOllamaModels) && (
              <Option key="no-models" value="" disabled text="No models available">
                {selectedProvider === 'Ollama'
                  ? ollamaError || 'No models found - click refresh'
                  : 'No models available'}
              </Option>
            )}
          {/* Model options - show size for Ollama models */}
          {providerModels.map((model) => {
            const displayText =
              selectedProvider === 'Ollama'
                ? getOllamaModelDisplayText(model.modelId)
                : model.modelId + (model.isDeprecated ? ' (Deprecated)' : '');

            const ollamaModel =
              selectedProvider === 'Ollama'
                ? ollamaModels.find((m) => m.name === model.modelId)
                : null;

            return (
              <Option key={model.modelId} value={model.modelId} text={displayText}>
                {ollamaModel ? (
                  <>
                    {model.modelId}{' '}
                    <span style={{ color: tokens.colorNeutralForeground3 }}>
                      ({ollamaModel.sizeGB.toFixed(2)} GB)
                    </span>
                  </>
                ) : (
                  <>
                    {model.modelId}
                    {model.isDeprecated && ' ⚠️'}
                  </>
                )}
              </Option>
            );
          })}
        </Dropdown>

        {/* Current model badge - shows selected model clearly */}
        {selectedModel && selectedModelExists && (
          <Tooltip content={`Current model: ${selectedModel}`} relationship="label">
            <Badge
              appearance="outline"
              color="informative"
              size="small"
              className={styles.modelBadge}
            >
              {getModelDisplayText(selectedModel)}
            </Badge>
          </Tooltip>
        )}

        {/* Warning badge if model doesn't exist - hide during Ollama loading */}
        {selectedModel &&
          !selectedModelExists &&
          selectedProvider &&
          isInitialized &&
          !(selectedProvider === 'Ollama' && isLoadingOllamaModels) && (
            <Tooltip
              content={
                selectedProvider === 'Ollama'
                  ? `Model "${selectedModel}" is not installed. Run "ollama pull ${selectedModel}" to install it, or click refresh.`
                  : `Model "${selectedModel}" not found. Click refresh to update available models.`
              }
              relationship="label"
            >
              <Badge appearance="filled" color="danger" size="medium" icon={<Warning20Regular />}>
                Model not found
              </Badge>
            </Tooltip>
          )}

        {/* Refresh button */}
        <Tooltip
          content={
            selectedProvider === 'Ollama'
              ? 'Refresh Ollama models from local installation'
              : 'Refresh available models from provider'
          }
          relationship="label"
        >
          <Button
            appearance="subtle"
            icon={
              isRefreshing || (selectedProvider === 'Ollama' && isLoadingOllamaModels) ? (
                <Spinner size="tiny" />
              ) : (
                <ArrowSync20Regular />
              )
            }
            onClick={refreshModels}
            disabled={
              isRefreshing || isLoading || (selectedProvider === 'Ollama' && isLoadingOllamaModels)
            }
            size="small"
            className={styles.refreshButton}
          />
        </Tooltip>
      </div>

      {/* Error indicator with retry - shows either general error or Ollama-specific error */}
      {(error || (selectedProvider === 'Ollama' && ollamaError)) && (
        <Tooltip content={`${error || ollamaError}. Click to retry.`} relationship="description">
          <div
            className={styles.errorIndicator}
            onClick={refreshModels}
            onKeyDown={(e) => {
              if (e.key === 'Enter' || e.key === ' ') {
                refreshModels();
              }
            }}
            role="button"
            tabIndex={0}
          >
            <ErrorCircle20Regular style={{ color: tokens.colorPaletteRedForeground1 }} />
          </div>
        </Tooltip>
      )}
    </div>
  );
}
