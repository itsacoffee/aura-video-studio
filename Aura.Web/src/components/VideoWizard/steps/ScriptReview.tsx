import {
  Badge,
  Button,
  Card,
  Checkbox,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Divider,
  Dropdown,
  Field,
  Input,
  Label,
  makeStyles,
  MessageBar,
  MessageBarBody,
  Option,
  Slider,
  Spinner,
  Text,
  Textarea,
  Title2,
  Title3,
  tokens,
  Tooltip,
} from '@fluentui/react-components';
import {
  ArrowClockwise24Regular,
  ArrowDownload24Regular,
  Clock24Regular,
  Delete24Regular,
  DocumentBulletList24Regular,
  DocumentText24Regular,
  History24Regular,
  Merge24Regular,
  Save24Regular,
  Sparkle24Regular,
  Speaker224Regular,
  SplitVertical24Regular,
  TextGrammarCheckmark24Regular,
} from '@fluentui/react-icons';
import type { FC } from 'react';
import React, { memo, useCallback, useEffect, useRef, useState } from 'react';
import {
  deleteScene,
  enhanceScript,
  exportScript,
  generateScript,
  getVersionHistory,
  listProviders,
  mergeScenes,
  regenerateAllScenes,
  regenerateScene,
  reorderScenes,
  revertToVersion,
  splitScene,
  updateScene,
  type GenerateScriptResponse,
  type ProviderInfoDto,
  type ScriptSceneDto,
  type ScriptVersionHistoryResponse,
} from '../../../services/api/scriptApi';
import { ttsService } from '../../../services/ttsService';
import { useNotifications } from '../../Notifications/Toasts';
import type { BriefData, ScriptData, ScriptScene, StepValidation, StyleData } from '../types';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  headerActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'center',
  },
  statsBar: {
    display: 'flex',
    gap: tokens.spacingHorizontalXL,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalL,
  },
  stat: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  statLabel: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  statValue: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground1,
  },
  scenesContainer: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  sceneCard: {
    padding: tokens.spacingVerticalL,
    position: 'relative',
  },
  sceneHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  sceneNumber: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  sceneActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  narrationField: {
    marginBottom: tokens.spacingVerticalM,
  },
  sceneMetadata: {
    display: 'flex',
    gap: tokens.spacingHorizontalL,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    marginTop: tokens.spacingVerticalS,
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXXL,
    gap: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    textAlign: 'center',
  },
  loadingContainer: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXXL,
    gap: tokens.spacingVerticalL,
  },
  providerSelect: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  audioRegenerateButton: {
    marginTop: tokens.spacingVerticalS,
  },
  messageBar: {
    marginTop: tokens.spacingVerticalS,
  },
  savingIndicator: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  enhancementPanel: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalL,
  },
  sliderGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  bulkActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalM,
  },
  versionList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    maxHeight: '400px',
    overflowY: 'auto',
  },
  versionItem: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: tokens.spacingVerticalS,
    borderRadius: tokens.borderRadiusSmall,
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground2Hover,
    },
  },
  sceneCardDragging: {
    opacity: 0.5,
    cursor: 'grabbing',
  },
  sceneCardDraggable: {
    cursor: 'grab',
    '&:active': {
      cursor: 'grabbing',
    },
  },
  sceneSelection: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginRight: tokens.spacingHorizontalS,
  },
  mergeActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusSmall,
  },
  splitDialog: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
});

interface ScriptReviewProps {
  data: ScriptData;
  briefData: BriefData;
  styleData: StyleData;
  advancedMode: boolean;
  selectedProvider?: string;
  onProviderChange?: (provider: string | undefined) => void;
  onChange: (data: ScriptData) => void;
  onValidationChange: (validation: StepValidation) => void;
}

const ScriptReviewComponent: FC<ScriptReviewProps> = ({
  data,
  briefData,
  styleData,
  advancedMode,
  selectedProvider: externalSelectedProvider,
  onProviderChange,
  onChange,
  onValidationChange,
}) => {
  const styles = useStyles();
  const { showSuccessToast, showFailureToast } = useNotifications();
  const [isGenerating, setIsGenerating] = useState(false);
  const [providers, setProviders] = useState<ProviderInfoDto[]>([]);
  const [internalSelectedProvider, setInternalSelectedProvider] = useState<string | undefined>();

  // Use external provider if provided, otherwise use internal state
  const selectedProvider = externalSelectedProvider !== undefined ? externalSelectedProvider : internalSelectedProvider;

  const setSelectedProvider = (provider: string | undefined) => {
    if (onProviderChange) {
      onProviderChange(provider);
    } else {
      setInternalSelectedProvider(provider);
    }
  };
  const [generatedScript, setGeneratedScript] = useState<GenerateScriptResponse | null>(null);
  const [editingScenes, setEditingScenes] = useState<Record<number, string>>({});
  const [regeneratingScenes, setRegeneratingScenes] = useState<Record<number, boolean>>({});
  const [regeneratingAudio, setRegeneratingAudio] = useState<Record<string, boolean>>({});
  const [audioMessages, setAudioMessages] = useState<
    Record<string, { type: 'success' | 'error'; message: string }>
  >({});
  const [savingScenes, setSavingScenes] = useState<Record<number, boolean>>({});
  const [showEnhancement, setShowEnhancement] = useState(false);
  const [toneAdjustment, setToneAdjustment] = useState(0);
  const [pacingAdjustment, setPacingAdjustment] = useState(0);
  const [isEnhancing, setIsEnhancing] = useState(false);
  const [isRegeneratingAll, setIsRegeneratingAll] = useState(false);
  const [showVersionHistory, setShowVersionHistory] = useState(false);
  const [versionHistory, setVersionHistory] = useState<ScriptVersionHistoryResponse | null>(null);
  const [isLoadingVersions, setIsLoadingVersions] = useState(false);
  const [selectedScenes, setSelectedScenes] = useState<Set<number>>(new Set());
  const [showSplitDialog, setShowSplitDialog] = useState(false);
  const [splitSceneNumber, setSplitSceneNumber] = useState<number | null>(null);
  const [splitPosition, setSplitPosition] = useState('');
  // Advanced LLM parameters
  const [llmTemperature, setLlmTemperature] = useState<number | undefined>(undefined);
  const [llmTopP, setLlmTopP] = useState<number | undefined>(undefined);
  const [llmTopK, setLlmTopK] = useState<number | undefined>(undefined);
  const [llmMaxTokens, setLlmMaxTokens] = useState<number | undefined>(undefined);
  const [llmFrequencyPenalty, setLlmFrequencyPenalty] = useState<number | undefined>(undefined);
  const [llmPresencePenalty, setLlmPresencePenalty] = useState<number | undefined>(undefined);
  // Model selection
  const [selectedModel, setSelectedModel] = useState<string | undefined>(undefined);
  const [isRefreshingModels, setIsRefreshingModels] = useState(false);

  // Helper function to determine provider parameter support
  const getProviderParameterSupport = (providerName: string | undefined) => {
    if (!providerName || providerName === 'Auto') {
      // Default to most permissive settings when Auto is selected
      return {
        supportsTemperature: true,
        supportsTopP: true,
        supportsTopK: true,
        supportsMaxTokens: true,
        supportsFrequencyPenalty: true,
        supportsPresencePenalty: true,
        maxTokensLimit: 4000,
        temperatureRange: { min: 0, max: 2 },
      };
    }

    const name = providerName.toLowerCase();

    // OpenAI and Azure OpenAI
    if (name.includes('openai') || name.includes('azure')) {
      return {
        supportsTemperature: true,
        supportsTopP: true,
        supportsTopK: false,
        supportsMaxTokens: true,
        supportsFrequencyPenalty: true,
        supportsPresencePenalty: true,
        maxTokensLimit: 4096, // GPT-4 context window
        temperatureRange: { min: 0, max: 2 },
      };
    }

    // Anthropic Claude
    if (name.includes('anthropic') || name.includes('claude')) {
      return {
        supportsTemperature: true,
        supportsTopP: true,
        supportsTopK: false,
        supportsMaxTokens: true,
        supportsFrequencyPenalty: false,
        supportsPresencePenalty: false,
        maxTokensLimit: 4096, // Claude context window
        temperatureRange: { min: 0, max: 1 },
      };
    }

    // Google Gemini
    if (name.includes('gemini') || name.includes('google')) {
      return {
        supportsTemperature: true,
        supportsTopP: true,
        supportsTopK: true,
        supportsMaxTokens: true,
        supportsFrequencyPenalty: false,
        supportsPresencePenalty: false,
        maxTokensLimit: 8192, // Gemini context window
        temperatureRange: { min: 0, max: 2 },
      };
    }

    // Ollama (exact match only)
    if (name === 'ollama') {
      return {
        supportsTemperature: true,
        supportsTopP: true,
        supportsTopK: true,
        supportsMaxTokens: true,
        supportsFrequencyPenalty: false,
        supportsPresencePenalty: false,
        maxTokensLimit: 2048, // Conservative limit for local models
        temperatureRange: { min: 0, max: 2 },
      };
    }

    // RuleBased (exact match only)
    if (name === 'rulebased' || name === 'rule-based') {
      return {
        supportsTemperature: false,
        supportsTopP: false,
        supportsTopK: false,
        supportsMaxTokens: false,
        supportsFrequencyPenalty: false,
        supportsPresencePenalty: false,
        maxTokensLimit: 0,
        temperatureRange: { min: 0, max: 0 },
      };
    }

    // Default: support all parameters
    return {
      supportsTemperature: true,
      supportsTopP: true,
      supportsTopK: true,
      supportsMaxTokens: true,
      supportsFrequencyPenalty: true,
      supportsPresencePenalty: true,
      maxTokensLimit: 4000,
      temperatureRange: { min: 0, max: 2 },
    };
  };

  const paramSupport = getProviderParameterSupport(selectedProvider);
  const [draggedSceneIndex, setDraggedSceneIndex] = useState<number | null>(null);
  const [isMerging, setIsMerging] = useState(false);
  const [isSplitting, setIsSplitting] = useState(false);
  const autoSaveTimeouts = useRef<Record<number, ReturnType<typeof setTimeout>>>({});

  const loadProviders = useCallback(async () => {
    try {
      const response = await listProviders();
      setProviders(response.providers);

      // Only auto-select if no external provider is set
      if (externalSelectedProvider === undefined) {
          // Filter to only LLM providers (same list as VideoCreationWizard)
          // Normalize provider names to handle "Ollama (model)" format
          const normalizeProviderName = (name: string) => {
            const parenIndex = name.indexOf('(');
            return parenIndex > 0 ? name.substring(0, parenIndex).trim() : name.trim();
          };

          const llmProviders = response.providers.filter((p) => {
            const normalized = normalizeProviderName(p.name);
            return normalized === 'RuleBased' ||
              normalized === 'Ollama' ||
              normalized === 'OpenAI' ||
              normalized === 'Gemini' ||
              normalized === 'Anthropic';
          });

          // Prefer Ollama if available (check normalized name), otherwise use first available provider
          const ollamaProvider = llmProviders.find((p) => {
            const normalized = normalizeProviderName(p.name);
            return p.isAvailable && normalized === 'Ollama';
          });
          if (ollamaProvider) {
            console.info('[ScriptReview] Ollama is available, selecting it as default');
            setSelectedProvider(ollamaProvider.name);
            // Set default model from provider
            if (ollamaProvider.defaultModel) {
              setSelectedModel(ollamaProvider.defaultModel);
            }
          } else {
            const availableProvider = llmProviders.find((p) => p.isAvailable);
            if (availableProvider) {
              console.info('[ScriptReview] Ollama not available, selecting first available provider:', availableProvider.name);
              setSelectedProvider(availableProvider.name);
              // Set default model from provider
              if (availableProvider.defaultModel) {
                setSelectedModel(availableProvider.defaultModel);
              }
            }
          }
      } else {
        console.info('[ScriptReview] Using external provider selection:', externalSelectedProvider);
        // Set model for external provider
        const provider = response.providers.find((p) => {
          const normalizeProviderName = (name: string) => {
            const parenIndex = name.indexOf('(');
            return parenIndex > 0 ? name.substring(0, parenIndex).trim() : name.trim();
          };
          return normalizeProviderName(p.name) === normalizeProviderName(externalSelectedProvider);
        });
        if (provider?.defaultModel) {
          setSelectedModel(provider.defaultModel);
        }
      }
    } catch (error) {
      console.error('Failed to load providers:', error);
    }
  }, [externalSelectedProvider, setSelectedProvider]);

  useEffect(() => {
    loadProviders();
  }, [loadProviders]);

  // Update selected model when provider changes
  useEffect(() => {
    if (selectedProvider && selectedProvider !== 'Auto') {
      const normalizeProviderName = (name: string) => {
        const parenIndex = name.indexOf('(');
        return parenIndex > 0 ? name.substring(0, parenIndex).trim() : name.trim();
      };
      const provider = providers.find((p) => {
        const normalized = normalizeProviderName(p.name);
        const selectedNormalized = normalizeProviderName(selectedProvider);
        return normalized === selectedNormalized;
      });
      if (provider) {
        // If model is not set or current model is not in available models, use default
        if (!selectedModel || (provider.availableModels.length > 0 && !provider.availableModels.includes(selectedModel))) {
          setSelectedModel(provider.defaultModel);
        }
      }
    }
  }, [selectedProvider, providers, selectedModel]);

  // Refresh Ollama models
  const handleRefreshOllamaModels = useCallback(async () => {
    const normalizeProviderName = (name: string) => {
      const parenIndex = name.indexOf('(');
      return parenIndex > 0 ? name.substring(0, parenIndex).trim() : name.trim();
    };
    const isOllama = selectedProvider && normalizeProviderName(selectedProvider) === 'Ollama';
    
    if (!isOllama) return;

    setIsRefreshingModels(true);
    try {
      // Reload providers to get fresh Ollama model list
      const response = await listProviders();
      setProviders(response.providers);
      
      // Update selected model if current one is no longer available
      const ollamaProvider = response.providers.find((p) => {
        const normalized = normalizeProviderName(p.name);
        return normalized === 'Ollama';
      });
      
      if (ollamaProvider) {
        if (!ollamaProvider.availableModels.includes(selectedModel || '')) {
          setSelectedModel(ollamaProvider.defaultModel);
        }
      }
      
      console.info('[ScriptReview] Ollama models refreshed');
    } catch (error) {
      console.error('Failed to refresh Ollama models:', error);
    } finally {
      setIsRefreshingModels(false);
    }
  }, [selectedProvider, selectedModel]);

  useEffect(() => {
    if (generatedScript && generatedScript.scenes.length > 0) {
      onValidationChange({ isValid: true, errors: [] });
    } else if (data && data.scenes.length > 0) {
      const hasEmptyScene = data.scenes.some((scene) => !scene.text || scene.text.trim() === '');
      if (hasEmptyScene) {
        onValidationChange({ isValid: false, errors: ['All scenes must have text'] });
      } else {
        onValidationChange({ isValid: true, errors: [] });
      }
    } else {
      onValidationChange({ isValid: false, errors: ['No script scenes available'] });
    }
  }, [generatedScript, data, onValidationChange]);

  const handleGenerateScript = async () => {
    if (!briefData.topic || briefData.topic.trim().length < 3) {
      showFailureToast({
        title: 'Invalid Topic',
        message: 'Please enter a valid topic (at least 3 characters) before generating a script.',
      });
      return;
    }

    setIsGenerating(true);
    try {
      // Normalize provider name to strip model info before sending to API
      const normalizeProviderName = (name: string | undefined): string | undefined => {
        if (!name || name === 'Auto') return undefined;
        const parenIndex = name.indexOf('(');
        return parenIndex > 0 ? name.substring(0, parenIndex).trim() : name.trim();
      };

      const currentProvider = providers.find((p) => {
        const normalized = normalizeProviderName(p.name);
        const selectedNormalized = normalizeProviderName(selectedProvider);
        return normalized === selectedNormalized;
      });

      // Only include modelOverride if:
      // 1. A model is selected
      // 2. The provider supports multiple models (has more than 1 available model)
      // 3. The selected model is different from the default
      const shouldIncludeModel = selectedModel && 
        currentProvider && 
        currentProvider.availableModels.length > 1 &&
        selectedModel !== currentProvider.defaultModel;

      const response = await generateScript({
        topic: briefData.topic,
        audience: briefData.targetAudience || 'General audience',
        goal: briefData.keyMessage || 'Create an engaging video',
        tone: styleData?.tone || 'Conversational',
        language: 'en',
        aspect: '16:9',
        targetDurationSeconds: briefData.duration || 60,
        pacing: 'Conversational',
        density: 'Balanced',
        style: styleData?.visualStyle || 'Modern',
        preferredProvider: normalizeProviderName(selectedProvider),
        ...(shouldIncludeModel && { modelOverride: selectedModel }),
        // Advanced LLM parameters (only include if explicitly set)
        ...(llmTemperature !== undefined && { temperature: llmTemperature }),
        ...(llmTopP !== undefined && { topP: llmTopP }),
        ...(llmTopK !== undefined && { topK: llmTopK }),
        ...(llmMaxTokens !== undefined && { maxTokens: llmMaxTokens }),
        ...(llmFrequencyPenalty !== undefined && { frequencyPenalty: llmFrequencyPenalty }),
        ...(llmPresencePenalty !== undefined && { presencePenalty: llmPresencePenalty }),
      });

      setGeneratedScript(response);

      const scriptScenes = response.scenes.map((scene) => ({
        id: `scene-${scene.number}`,
        text: scene.narration,
        duration: scene.durationSeconds,
        visualDescription: scene.visualPrompt,
        timestamp: response.scenes
          .slice(0, scene.number - 1)
          .reduce((sum, s) => sum + s.durationSeconds, 0),
      }));

      onChange({
        content: response.scenes.map((s) => s.narration).join('\n\n'),
        scenes: scriptScenes,
        generatedAt: new Date(),
      });

      showSuccessToast({
        title: 'Script Generated Successfully',
        message: `Generated ${response.scenes.length} scenes using ${response.metadata.providerName}. Total duration: ${Math.floor(response.totalDurationSeconds / 60)}:${String(response.totalDurationSeconds % 60).padStart(2, '0')}`,
      });
    } catch (error) {
      console.error('Script generation failed:', error);

      let errorTitle = 'Script Generation Failed';
      let errorMessage = 'Failed to generate script. Please check your provider configuration and try again.';

      // Try to extract detailed error information from the response
      if (error && typeof error === 'object' && 'response' in error) {
        const axiosError = error as { response?: { data?: { detail?: string; errors?: Record<string, string[]>; message?: string } } };
        const responseData = axiosError.response?.data;

        if (responseData) {
          // Check for validation errors
          if (responseData.errors && Object.keys(responseData.errors).length > 0) {
            errorTitle = 'Validation Error';
            const errorList = Object.entries(responseData.errors)
              .map(([field, messages]) => `${field}: ${Array.isArray(messages) ? messages.join(', ') : messages}`)
              .join('; ');
            errorMessage = `Request validation failed: ${errorList}`;
          } else if (responseData.detail) {
            errorMessage = responseData.detail;
          } else if (responseData.message) {
            errorMessage = responseData.message;
          }
        }
      } else if (error instanceof Error) {
        errorMessage = error.message;
      }

      showFailureToast({
        title: errorTitle,
        message: errorMessage,
      });
    } finally {
      setIsGenerating(false);
    }
  };

  const handleSceneEdit = useCallback(
    (sceneNumber: number, newNarration: string) => {
      setEditingScenes((prev) => ({
        ...prev,
        [sceneNumber]: newNarration,
      }));

      setSavingScenes((prev) => ({
        ...prev,
        [sceneNumber]: true,
      }));

      if (autoSaveTimeouts.current[sceneNumber]) {
        clearTimeout(autoSaveTimeouts.current[sceneNumber]);
      }

      autoSaveTimeouts.current[sceneNumber] = setTimeout(async () => {
        if (!generatedScript) return;

        try {
          await updateScene(generatedScript.scriptId, sceneNumber, {
            narration: newNarration,
          });

          const updatedScenes = generatedScript.scenes.map((scene) =>
            scene.number === sceneNumber ? { ...scene, narration: newNarration } : scene
          );

          setGeneratedScript({
            ...generatedScript,
            scenes: updatedScenes,
          });

          const scriptScenes = updatedScenes.map((scene) => ({
            id: `scene-${scene.number}`,
            text: scene.narration,
            duration: scene.durationSeconds,
            visualDescription: scene.visualPrompt,
            timestamp: updatedScenes
              .slice(0, scene.number - 1)
              .reduce((sum, s) => sum + s.durationSeconds, 0),
          }));

          onChange({
            content: updatedScenes.map((s) => s.narration).join('\n\n'),
            scenes: scriptScenes,
            generatedAt: new Date(),
          });

          setSavingScenes((prev) => ({
            ...prev,
            [sceneNumber]: false,
          }));
        } catch (error) {
          console.error('Failed to save scene:', error);
          setSavingScenes((prev) => ({
            ...prev,
            [sceneNumber]: false,
          }));
        }
      }, 2000);
    },
    [generatedScript, onChange]
  );

  const handleExportScript = async (format: 'text' | 'markdown') => {
    if (!generatedScript) return;

    try {
      const blob = await exportScript(generatedScript.scriptId, format);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${generatedScript.title.replace(/\s+/g, '_')}_${new Date().toISOString().slice(0, 10)}.${format === 'markdown' ? 'md' : 'txt'}`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (error) {
      console.error('Failed to export script:', error);
    }
  };

  const handleRegenerateScene = async (sceneNumber: number) => {
    if (!generatedScript) return;

    setRegeneratingScenes((prev) => ({ ...prev, [sceneNumber]: true }));

    try {
      const response = await regenerateScene(generatedScript.scriptId, sceneNumber);

      setGeneratedScript(response);

      const scriptScenes = response.scenes.map((scene) => ({
        id: `scene-${scene.number}`,
        text: scene.narration,
        duration: scene.durationSeconds,
        visualDescription: scene.visualPrompt,
        timestamp: response.scenes
          .slice(0, scene.number - 1)
          .reduce((sum, s) => sum + s.durationSeconds, 0),
      }));

      onChange({
        content: response.scenes.map((s) => s.narration).join('\n\n'),
        scenes: scriptScenes,
        generatedAt: new Date(),
      });
    } catch (error) {
      console.error('Failed to regenerate scene:', error);
    } finally {
      setRegeneratingScenes((prev) => ({ ...prev, [sceneNumber]: false }));
    }
  };

  const calculateWordCount = (scenes: ScriptSceneDto[]): number => {
    return scenes.reduce((total, scene) => {
      return total + scene.narration.split(/\s+/).filter((word) => word.length > 0).length;
    }, 0);
  };

  const handleRegenerateAudio = async (scene: ScriptScene, sceneIndex: number) => {
    setRegeneratingAudio((prev) => ({ ...prev, [scene.id]: true }));
    setAudioMessages((prev) => {
      const newMessages = { ...prev };
      delete newMessages[scene.id];
      return newMessages;
    });

    try {
      const response = await ttsService.regenerateAudio({
        sceneIndex,
        text: scene.text,
        startSeconds: scene.timestamp,
        durationSeconds: scene.duration,
        provider: styleData.voiceProvider,
        voiceName: styleData.voiceName,
      });

      if (response.success) {
        setAudioMessages((prev) => ({
          ...prev,
          [scene.id]: { type: 'success', message: 'Audio generated successfully' },
        }));
      }
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      setAudioMessages((prev) => ({
        ...prev,
        [scene.id]: { type: 'error', message: `Failed: ${errorObj.message}` },
      }));
    } finally {
      setRegeneratingAudio((prev) => ({ ...prev, [scene.id]: false }));
    }
  };

  const calculateReadingSpeed = (wordCount: number, durationSeconds: number): number => {
    if (durationSeconds === 0) return 0;
    return Math.round((wordCount / durationSeconds) * 60);
  };

  const handleEnhanceScript = async () => {
    if (!generatedScript) return;

    setIsEnhancing(true);
    try {
      const response = await enhanceScript(generatedScript.scriptId, {
        goal: 'Enhance script based on adjustments',
        toneAdjustment,
        pacingAdjustment,
      });

      setGeneratedScript(response);

      const scriptScenes = response.scenes.map((scene) => ({
        id: `scene-${scene.number}`,
        text: scene.narration,
        duration: scene.durationSeconds,
        visualDescription: scene.visualPrompt,
        timestamp: response.scenes
          .slice(0, scene.number - 1)
          .reduce((sum, s) => sum + s.durationSeconds, 0),
      }));

      onChange({
        content: response.scenes.map((s) => s.narration).join('\n\n'),
        scenes: scriptScenes,
        generatedAt: new Date(),
      });
    } catch (error) {
      console.error('Failed to enhance script:', error);
    } finally {
      setIsEnhancing(false);
    }
  };

  const handleRegenerateAll = async () => {
    if (!generatedScript) return;

    setIsRegeneratingAll(true);
    try {
      const response = await regenerateAllScenes(generatedScript.scriptId);

      setGeneratedScript(response);

      const scriptScenes = response.scenes.map((scene) => ({
        id: `scene-${scene.number}`,
        text: scene.narration,
        duration: scene.durationSeconds,
        visualDescription: scene.visualPrompt,
        timestamp: response.scenes
          .slice(0, scene.number - 1)
          .reduce((sum, s) => sum + s.durationSeconds, 0),
      }));

      onChange({
        content: response.scenes.map((s) => s.narration).join('\n\n'),
        scenes: scriptScenes,
        generatedAt: new Date(),
      });
    } catch (error) {
      console.error('Failed to regenerate all scenes:', error);
    } finally {
      setIsRegeneratingAll(false);
    }
  };

  const handleDeleteScene = async (sceneNumber: number) => {
    if (!generatedScript || generatedScript.scenes.length === 1) return;

    try {
      const response = await deleteScene(generatedScript.scriptId, sceneNumber);

      setGeneratedScript(response);

      const scriptScenes = response.scenes.map((scene) => ({
        id: `scene-${scene.number}`,
        text: scene.narration,
        duration: scene.durationSeconds,
        visualDescription: scene.visualPrompt,
        timestamp: response.scenes
          .slice(0, scene.number - 1)
          .reduce((sum, s) => sum + s.durationSeconds, 0),
      }));

      onChange({
        content: response.scenes.map((s) => s.narration).join('\n\n'),
        scenes: scriptScenes,
        generatedAt: new Date(),
      });
    } catch (error) {
      console.error('Failed to delete scene:', error);
    }
  };

  const loadVersionHistory = async () => {
    if (!generatedScript) return;

    setIsLoadingVersions(true);
    try {
      const history = await getVersionHistory(generatedScript.scriptId);
      setVersionHistory(history);
    } catch (error) {
      console.error('Failed to load version history:', error);
    } finally {
      setIsLoadingVersions(false);
    }
  };

  const handleRevertToVersion = async (versionId: string) => {
    if (!generatedScript) return;

    try {
      const response = await revertToVersion(generatedScript.scriptId, { versionId });

      setGeneratedScript(response);

      const scriptScenes = response.scenes.map((scene) => ({
        id: `scene-${scene.number}`,
        text: scene.narration,
        duration: scene.durationSeconds,
        visualDescription: scene.visualPrompt,
        timestamp: response.scenes
          .slice(0, scene.number - 1)
          .reduce((sum, s) => sum + s.durationSeconds, 0),
      }));

      onChange({
        content: response.scenes.map((s) => s.narration).join('\n\n'),
        scenes: scriptScenes,
        generatedAt: new Date(),
      });

      await loadVersionHistory();
    } catch (error) {
      console.error('Failed to revert to version:', error);
    }
  };

  const handleSceneSelection = (sceneNumber: number, checked: boolean) => {
    setSelectedScenes((prev) => {
      const newSet = new Set(prev);
      if (checked) {
        newSet.add(sceneNumber);
      } else {
        newSet.delete(sceneNumber);
      }
      return newSet;
    });
  };

  const handleMergeScenes = async () => {
    if (!generatedScript || selectedScenes.size < 2) return;

    setIsMerging(true);
    try {
      const sceneNumbers = Array.from(selectedScenes).sort((a, b) => a - b);
      const response = await mergeScenes(generatedScript.scriptId, {
        sceneNumbers,
        separator: ' ',
      });

      setGeneratedScript(response);
      setSelectedScenes(new Set());

      const scriptScenes = response.scenes.map((scene) => ({
        id: `scene-${scene.number}`,
        text: scene.narration,
        duration: scene.durationSeconds,
        visualDescription: scene.visualPrompt,
        timestamp: response.scenes
          .slice(0, scene.number - 1)
          .reduce((sum, s) => sum + s.durationSeconds, 0),
      }));

      onChange({
        content: response.scenes.map((s) => s.narration).join('\n\n'),
        scenes: scriptScenes,
        generatedAt: new Date(),
      });
    } catch (error) {
      console.error('Failed to merge scenes:', error);
    } finally {
      setIsMerging(false);
    }
  };

  const handleSplitScene = async () => {
    if (!generatedScript || splitSceneNumber === null || !splitPosition) return;

    const position = parseInt(splitPosition, 10);
    if (isNaN(position) || position <= 0) return;

    setIsSplitting(true);
    try {
      const response = await splitScene(generatedScript.scriptId, splitSceneNumber, {
        splitPosition: position,
      });

      setGeneratedScript(response);
      setShowSplitDialog(false);
      setSplitSceneNumber(null);
      setSplitPosition('');

      const scriptScenes = response.scenes.map((scene) => ({
        id: `scene-${scene.number}`,
        text: scene.narration,
        duration: scene.durationSeconds,
        visualDescription: scene.visualPrompt,
        timestamp: response.scenes
          .slice(0, scene.number - 1)
          .reduce((sum, s) => sum + s.durationSeconds, 0),
      }));

      onChange({
        content: response.scenes.map((s) => s.narration).join('\n\n'),
        scenes: scriptScenes,
        generatedAt: new Date(),
      });
    } catch (error) {
      console.error('Failed to split scene:', error);
    } finally {
      setIsSplitting(false);
    }
  };

  const handleDragStart = (index: number) => () => {
    setDraggedSceneIndex(index);
  };

  const handleDragOver = (index: number) => async (e: React.DragEvent) => {
    e.preventDefault();
    if (draggedSceneIndex !== null && draggedSceneIndex !== index && generatedScript) {
      const scenes = [...generatedScript.scenes];
      const draggedScene = scenes[draggedSceneIndex];
      scenes.splice(draggedSceneIndex, 1);
      scenes.splice(index, 0, draggedScene);

      const sceneOrder = scenes.map((scene) => scene.number);

      try {
        const response = await reorderScenes(generatedScript.scriptId, { sceneOrder });
        setGeneratedScript(response);
        setDraggedSceneIndex(index);

        const scriptScenes = response.scenes.map((scene) => ({
          id: `scene-${scene.number}`,
          text: scene.narration,
          duration: scene.durationSeconds,
          visualDescription: scene.visualPrompt,
          timestamp: response.scenes
            .slice(0, scene.number - 1)
            .reduce((sum, s) => sum + s.durationSeconds, 0),
        }));

        onChange({
          content: response.scenes.map((s) => s.narration).join('\n\n'),
          scenes: scriptScenes,
          generatedAt: new Date(),
        });
      } catch (error) {
        console.error('Failed to reorder scenes:', error);
      }
    }
  };

  const handleDragEnd = () => {
    setDraggedSceneIndex(null);
  };

  const isSceneDurationAppropriate = (scene: ScriptSceneDto): 'short' | 'good' | 'long' => {
    const wordCount = scene.narration.split(/\s+/).filter((word) => word.length > 0).length;
    const wpm = calculateReadingSpeed(wordCount, scene.durationSeconds);

    if (wpm < 120) return 'short';
    if (wpm > 180) return 'long';
    return 'good';
  };

  if (isGenerating) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <Title2>Script Review</Title2>
        </div>
        <div className={styles.loadingContainer}>
          <Spinner size="extra-large" />
          <Title3>Generating your script...</Title3>
          <Text>This may take a few moments. We&apos;re crafting the perfect narrative.</Text>
        </div>
      </div>
    );
  }

  if (!generatedScript && (!data || data.scenes.length === 0)) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <Title2>Script Review</Title2>
          <div className={styles.headerActions}>
            <Field label="LLM Provider" className={styles.providerSelect}>
              {providers.length > 0 ? (
                <Dropdown
                  value={selectedProvider || 'Auto'}
                  onOptionSelect={(_, data) => {
                    if (data.optionValue) {
                      setSelectedProvider(data.optionValue);
                    }
                  }}
                  className={styles.providerDropdown}
                  placeholder="Select provider..."
                >
                  <Option value="Auto" text="Auto (Best Available)">
                    Auto (Best Available)
                  </Option>
                  {providers.map((provider) => (
                    <Option
                      key={provider.name}
                      value={provider.name}
                      text={`${provider.name}${provider.isAvailable ? '' : ' (Unavailable)'}`}
                      disabled={!provider.isAvailable}
                    >
                      <div className={styles.providerInfo}>
                        <Text weight="semibold">
                          {provider.name}
                          {!provider.isAvailable && (
                            <Text size={200} style={{ color: tokens.colorPaletteRedForeground1 }}>
                              {' '}
                              (Unavailable)
                            </Text>
                          )}
                        </Text>
                        <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                          {provider.tier} • {provider.requiresInternet ? 'Cloud' : 'Local'}
                          {provider.estimatedCostPer1KTokens > 0 &&
                            ` • ~$${provider.estimatedCostPer1KTokens.toFixed(4)}/1K tokens`}
                        </Text>
                      </div>
                    </Option>
                  ))}
                </Dropdown>
              ) : (
                <Spinner size="tiny" />
              )}
            </Field>
            {/* Model Selection - Show if provider supports multiple models */}
            {selectedProvider && selectedProvider !== 'Auto' && (() => {
              const normalizeProviderName = (name: string) => {
                const parenIndex = name.indexOf('(');
                return parenIndex > 0 ? name.substring(0, parenIndex).trim() : name.trim();
              };
              const currentProvider = providers.find((p) => {
                const normalized = normalizeProviderName(p.name);
                const selectedNormalized = normalizeProviderName(selectedProvider);
                return normalized === selectedNormalized;
              });
              const isOllama = currentProvider && normalizeProviderName(currentProvider.name) === 'Ollama';
              const hasMultipleModels = currentProvider && currentProvider.availableModels.length > 1;

              if (!hasMultipleModels) {
                // Show current model even if only one available
                return currentProvider ? (
                  <Field label="Model">
                    <Text size={300} style={{ color: tokens.colorNeutralForeground2 }}>
                      {currentProvider.defaultModel}
                    </Text>
                  </Field>
                ) : null;
              }

              return (
                <Field label="Model">
                  <div style={{ display: 'flex', gap: tokens.spacingHorizontalXS, alignItems: 'center' }}>
                    <Dropdown
                      value={selectedModel || currentProvider?.defaultModel || ''}
                      onOptionSelect={(_, data) => {
                        if (data.optionValue) {
                          setSelectedModel(data.optionValue);
                        }
                      }}
                      style={{ minWidth: '180px' }}
                    >
                      {currentProvider?.availableModels.map((model) => (
                        <Option key={model} value={model} text={model}>
                          {model}
                          {model === currentProvider.defaultModel && (
                            <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                              {' '}(default)
                            </Text>
                          )}
                        </Option>
                      ))}
                    </Dropdown>
                    {isOllama && (
                      <Tooltip content="Refresh Ollama models list" relationship="label">
                        <Button
                          appearance="subtle"
                          icon={<ArrowClockwise24Regular />}
                          onClick={handleRefreshOllamaModels}
                          disabled={isRefreshingModels}
                          size="small"
                        >
                          {isRefreshingModels ? 'Refreshing...' : 'Refresh'}
                        </Button>
                      </Tooltip>
                    )}
                  </div>
                </Field>
              );
            })()}
            <Button
              appearance="primary"
              icon={<Sparkle24Regular />}
              onClick={handleGenerateScript}
              disabled={!briefData.topic || isGenerating}
            >
              {isGenerating ? 'Generating...' : 'Generate Script'}
            </Button>
          </div>
        </div>
        <div className={styles.emptyState}>
          <DocumentBulletList24Regular />
          <Title3>No script generated yet</Title3>
          <Text>
            Click &quot;Generate Script&quot; to create an AI-powered script based on your brief.
          </Text>
        </div>
      </div>
    );
  }

  if (!generatedScript && data && data.scenes.length > 0) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <Title2>Script Review</Title2>
          <Text>Review and edit the AI-generated script before proceeding.</Text>
        </div>

        <div className={styles.scenesContainer}>
          {data.scenes.map((scene, index) => (
            <Card key={scene.id} className={styles.sceneCard}>
              <div className={styles.sceneHeader}>
                <div className={styles.sceneNumber}>
                  <Badge appearance="filled" color="brand">
                    Scene {index + 1}
                  </Badge>
                </div>
                <div className={styles.sceneActions}>
                  <Tooltip content="Regenerate audio for this scene" relationship="label">
                    <Button
                      size="small"
                      icon={<Speaker224Regular />}
                      onClick={() => handleRegenerateAudio(scene, index)}
                      disabled={
                        regeneratingAudio[scene.id] || !scene.text || scene.text.trim() === ''
                      }
                    >
                      {regeneratingAudio[scene.id] ? 'Regenerating...' : 'Regenerate Audio'}
                    </Button>
                  </Tooltip>
                </div>
              </div>

              <Field className={styles.narrationField} label="Narration">
                <Textarea
                  value={scene.text}
                  onChange={(e) => {
                    const updatedScenes = [...data.scenes];
                    updatedScenes[index] = { ...scene, text: e.target.value };
                    onChange({
                      ...data,
                      scenes: updatedScenes,
                    });
                  }}
                  rows={4}
                  resize="vertical"
                />
              </Field>

              <div className={styles.sceneMetadata}>
                <div
                  style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalXS }}
                >
                  <Clock24Regular />
                  <Text>
                    {Math.floor(scene.timestamp / 60)}:
                    {String(Math.floor(scene.timestamp % 60)).padStart(2, '0')} -{' '}
                    {Math.floor((scene.timestamp + scene.duration) / 60)}:
                    {String(Math.floor((scene.timestamp + scene.duration) % 60)).padStart(2, '0')}
                  </Text>
                </div>
                <div
                  style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalXS }}
                >
                  <TextGrammarCheckmark24Regular />
                  <Text>
                    {scene.text.split(/\s+/).filter((word) => word.length > 0).length} words
                  </Text>
                </div>
              </div>

              {audioMessages[scene.id] && (
                <MessageBar
                  intent={audioMessages[scene.id].type === 'success' ? 'success' : 'error'}
                  className={styles.messageBar}
                >
                  <MessageBarBody>{audioMessages[scene.id].message}</MessageBarBody>
                </MessageBar>
              )}

              <Divider
                style={{
                  marginTop: tokens.spacingVerticalM,
                  marginBottom: tokens.spacingVerticalM,
                }}
              />

              <Field label="Visual Prompt">
                <Text size={200}>{scene.visualDescription}</Text>
              </Field>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  if (!generatedScript) {
    return null;
  }

  const wordCount = calculateWordCount(generatedScript.scenes);
  const wpm = calculateReadingSpeed(wordCount, generatedScript.totalDurationSeconds);

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title2>{generatedScript.title}</Title2>
        <div className={styles.headerActions}>
          <Tooltip content="Export as text file" relationship="label">
            <Button icon={<DocumentText24Regular />} onClick={() => handleExportScript('text')}>
              Export Text
            </Button>
          </Tooltip>
          <Tooltip content="Export as markdown file" relationship="label">
            <Button
              icon={<ArrowDownload24Regular />}
              onClick={() => handleExportScript('markdown')}
            >
              Export Markdown
            </Button>
          </Tooltip>
          <Tooltip content="Regenerate entire script" relationship="label">
            <Button icon={<ArrowClockwise24Regular />} onClick={handleGenerateScript}>
              Regenerate
            </Button>
          </Tooltip>
        </div>
      </div>

      <div className={styles.statsBar}>
        <div className={styles.stat}>
          <Text className={styles.statLabel}>Total Duration</Text>
          <Text className={styles.statValue}>
            {Math.floor(generatedScript.totalDurationSeconds / 60)}:
            {String(Math.floor(generatedScript.totalDurationSeconds % 60)).padStart(2, '0')}
          </Text>
        </div>
        <div className={styles.stat}>
          <Text className={styles.statLabel}>Word Count</Text>
          <Text className={styles.statValue}>{wordCount}</Text>
        </div>
        <div className={styles.stat}>
          <Text className={styles.statLabel}>Reading Speed</Text>
          <Text className={styles.statValue}>
            {wpm} WPM
            {wpm < 120 && ' (Slow)'}
            {wpm >= 120 && wpm <= 180 && ' (Good)'}
            {wpm > 180 && ' (Fast)'}
          </Text>
        </div>
        <div className={styles.stat}>
          <Text className={styles.statLabel}>Scenes</Text>
          <Text className={styles.statValue}>{generatedScript.scenes.length}</Text>
        </div>
        <div className={styles.stat}>
          <Text className={styles.statLabel}>Provider</Text>
          <Badge appearance="tint" color="brand">
            {generatedScript.metadata.providerName}
          </Badge>
        </div>
        {advancedMode && generatedScript.metadata && (
          <>
            <div className={styles.stat}>
              <Text className={styles.statLabel}>Model</Text>
              <Text className={styles.statValue}>{generatedScript.metadata.modelUsed || 'N/A'}</Text>
            </div>
            <div className={styles.stat}>
              <Text className={styles.statLabel}>Tokens</Text>
              <Text className={styles.statValue}>
                {generatedScript.metadata.tokensUsed?.toLocaleString() || 'N/A'}
              </Text>
            </div>
            {generatedScript.metadata.estimatedCost !== undefined && (
              <div className={styles.stat}>
                <Text className={styles.statLabel}>Cost</Text>
                <Text className={styles.statValue}>
                  ${generatedScript.metadata.estimatedCost.toFixed(4)}
                </Text>
              </div>
            )}
            {generatedScript.metadata.generationTimeSeconds !== undefined && (
              <div className={styles.stat}>
                <Text className={styles.statLabel}>Generation Time</Text>
                <Text className={styles.statValue}>
                  {generatedScript.metadata.generationTimeSeconds.toFixed(1)}s
                </Text>
              </div>
            )}
          </>
        )}
      </div>

      {/* Advanced Mode: LLM Parameters */}
      {advancedMode && (
        <Card style={{ padding: tokens.spacingVerticalL, marginBottom: tokens.spacingVerticalL }}>
          <Title3 style={{ marginBottom: tokens.spacingVerticalM }}>Advanced LLM Parameters</Title3>
          <Text size={300} style={{ marginBottom: tokens.spacingVerticalM, color: tokens.colorNeutralForeground3 }}>
            Fine-tune LLM generation parameters for more dynamic and customized results. These settings override default values.
            {selectedProvider && selectedProvider !== 'Auto' && (
              <Text size={200} style={{ display: 'block', marginTop: tokens.spacingVerticalXS, color: tokens.colorNeutralForeground2 }}>
                Provider: <Text weight="semibold">{selectedProvider}</Text> - Only supported parameters are shown below.
              </Text>
            )}
          </Text>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: tokens.spacingVerticalL }}>
            {paramSupport.supportsTemperature && (
              <div className={styles.sliderGroup}>
                <Label>
                  Temperature: {llmTemperature !== undefined ? llmTemperature.toFixed(2) : 'Auto'}
                </Label>
                <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                  Controls randomness ({paramSupport.temperatureRange.min} = deterministic, {paramSupport.temperatureRange.max} = very creative)
                </Text>
                <Slider
                  min={paramSupport.temperatureRange.min}
                  max={paramSupport.temperatureRange.max}
                  step={0.1}
                  value={llmTemperature ?? 0.7}
                  onChange={(_, data) => setLlmTemperature(data.value === 0.7 ? undefined : data.value)}
                  style={{
                    '--fui-slider-thumb-background': tokens.colorBrandForeground1,
                    '--fui-slider-rail-background': tokens.colorNeutralStroke1,
                    '--fui-slider-rail-background-hover': tokens.colorNeutralStroke2,
                  } as React.CSSProperties}
                />
                <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: tokens.spacingVerticalXS }}>
                  <Text size={200}>{paramSupport.temperatureRange.min}</Text>
                  <Text size={200}>{paramSupport.temperatureRange.max}</Text>
                </div>
              </div>
            )}

            {paramSupport.supportsTopP && (
              <div className={styles.sliderGroup}>
                <Label>
                  Top P: {llmTopP !== undefined ? llmTopP.toFixed(2) : 'Auto'}
                </Label>
                <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                  Nucleus sampling - controls diversity of tokens considered
                </Text>
                <Slider
                  min={0}
                  max={1}
                  step={0.05}
                  value={llmTopP ?? 0.9}
                  onChange={(_, data) => setLlmTopP(data.value === 0.9 ? undefined : data.value)}
                  style={{
                    '--fui-slider-thumb-background': tokens.colorBrandForeground1,
                    '--fui-slider-rail-background': tokens.colorNeutralStroke1,
                    '--fui-slider-rail-background-hover': tokens.colorNeutralStroke2,
                  } as React.CSSProperties}
                />
                <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: tokens.spacingVerticalXS }}>
                  <Text size={200}>0.0</Text>
                  <Text size={200}>1.0</Text>
                </div>
              </div>
            )}

            {paramSupport.supportsTopK && (
              <div className={styles.sliderGroup}>
                <Label>
                  Top K: {llmTopK !== undefined ? llmTopK : 'Auto'}
                </Label>
                <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                  Limits sampling to top K tokens (Gemini, Ollama only)
                </Text>
                <Slider
                  min={0}
                  max={100}
                  step={1}
                  value={llmTopK ?? 40}
                  onChange={(_, data) => setLlmTopK(data.value === 40 ? undefined : data.value)}
                  style={{
                    '--fui-slider-thumb-background': tokens.colorBrandForeground1,
                    '--fui-slider-rail-background': tokens.colorNeutralStroke1,
                    '--fui-slider-rail-background-hover': tokens.colorNeutralStroke2,
                  } as React.CSSProperties}
                />
                <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: tokens.spacingVerticalXS }}>
                  <Text size={200}>0</Text>
                  <Text size={200}>100</Text>
                </div>
              </div>
            )}

            {paramSupport.supportsMaxTokens && (
              <div className={styles.sliderGroup}>
                <Label>
                  Max Tokens: {llmMaxTokens !== undefined ? llmMaxTokens.toLocaleString() : 'Auto'}
                </Label>
                <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                  Maximum tokens to generate (limit: {paramSupport.maxTokensLimit.toLocaleString()})
                </Text>
                <Slider
                  min={100}
                  max={paramSupport.maxTokensLimit}
                  step={paramSupport.maxTokensLimit > 2000 ? 100 : 50}
                  value={llmMaxTokens ?? Math.min(2000, paramSupport.maxTokensLimit)}
                  onChange={(_, data) => {
                    const defaultValue = Math.min(2000, paramSupport.maxTokensLimit);
                    setLlmMaxTokens(data.value === defaultValue ? undefined : data.value);
                  }}
                  style={{
                    '--fui-slider-thumb-background': tokens.colorBrandForeground1,
                    '--fui-slider-rail-background': tokens.colorNeutralStroke1,
                    '--fui-slider-rail-background-hover': tokens.colorNeutralStroke2,
                  } as React.CSSProperties}
                />
                <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: tokens.spacingVerticalXS }}>
                  <Text size={200}>100</Text>
                  <Text size={200}>{paramSupport.maxTokensLimit.toLocaleString()}</Text>
                </div>
              </div>
            )}

            {paramSupport.supportsFrequencyPenalty && (
              <div className={styles.sliderGroup}>
                <Label>
                  Frequency Penalty: {llmFrequencyPenalty !== undefined ? llmFrequencyPenalty.toFixed(2) : 'Auto'}
                </Label>
                <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                  Reduces repetition (OpenAI/Azure only)
                </Text>
                <Slider
                  min={-2}
                  max={2}
                  step={0.1}
                  value={llmFrequencyPenalty ?? 0}
                  onChange={(_, data) => setLlmFrequencyPenalty(data.value === 0 ? undefined : data.value)}
                  style={{
                    '--fui-slider-thumb-background': tokens.colorBrandForeground1,
                    '--fui-slider-rail-background': tokens.colorNeutralStroke1,
                    '--fui-slider-rail-background-hover': tokens.colorNeutralStroke2,
                  } as React.CSSProperties}
                />
                <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: tokens.spacingVerticalXS }}>
                  <Text size={200}>-2.0</Text>
                  <Text size={200}>2.0</Text>
                </div>
              </div>
            )}

            {paramSupport.supportsPresencePenalty && (
              <div className={styles.sliderGroup}>
                <Label>
                  Presence Penalty: {llmPresencePenalty !== undefined ? llmPresencePenalty.toFixed(2) : 'Auto'}
                </Label>
                <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                  Encourages new topics (OpenAI/Azure only)
                </Text>
                <Slider
                  min={-2}
                  max={2}
                  step={0.1}
                  value={llmPresencePenalty ?? 0}
                  onChange={(_, data) => setLlmPresencePenalty(data.value === 0 ? undefined : data.value)}
                  style={{
                    '--fui-slider-thumb-background': tokens.colorBrandForeground1,
                    '--fui-slider-rail-background': tokens.colorNeutralStroke1,
                    '--fui-slider-rail-background-hover': tokens.colorNeutralStroke2,
                  } as React.CSSProperties}
                />
                <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: tokens.spacingVerticalXS }}>
                  <Text size={200}>-2.0</Text>
                  <Text size={200}>2.0</Text>
                </div>
              </div>
            )}

            {!paramSupport.supportsTemperature && !paramSupport.supportsTopP && !paramSupport.supportsTopK && (
              <div style={{ gridColumn: '1 / -1', padding: tokens.spacingVerticalM, backgroundColor: tokens.colorNeutralBackground2, borderRadius: tokens.borderRadiusMedium }}>
                <Text size={300} weight="semibold" style={{ color: tokens.colorNeutralForeground2 }}>
                  {selectedProvider === 'RuleBased' || selectedProvider?.toLowerCase().includes('rule')
                    ? 'Rule-based provider does not support LLM parameters. It uses template-based generation.'
                    : 'No advanced parameters available for this provider.'}
                </Text>
              </div>
            )}
          </div>
          {(paramSupport.supportsTemperature || paramSupport.supportsTopP || paramSupport.supportsTopK || paramSupport.supportsMaxTokens) && (
            <Button
              appearance="subtle"
              size="small"
              onClick={() => {
                setLlmTemperature(undefined);
                setLlmTopP(undefined);
                setLlmTopK(undefined);
                setLlmMaxTokens(undefined);
                setLlmFrequencyPenalty(undefined);
                setLlmPresencePenalty(undefined);
              }}
              style={{ marginTop: tokens.spacingVerticalM }}
            >
              Reset to Defaults
            </Button>
          )}
        </Card>
      )}

      {/* Bulk Actions Toolbar */}
      <div className={styles.bulkActions}>
        <Tooltip content="Regenerate all scenes in the script" relationship="label">
          <Button
            icon={<ArrowClockwise24Regular />}
            onClick={handleRegenerateAll}
            disabled={isRegeneratingAll}
          >
            {isRegeneratingAll ? 'Regenerating All...' : 'Regenerate All'}
          </Button>
        </Tooltip>
        <Tooltip content="Adjust tone and pacing" relationship="label">
          <Button
            appearance="subtle"
            icon={<Sparkle24Regular />}
            onClick={() => setShowEnhancement(!showEnhancement)}
          >
            Enhance Script
          </Button>
        </Tooltip>
        <Tooltip content="View version history" relationship="label">
          <Button
            appearance="subtle"
            icon={<History24Regular />}
            onClick={() => {
              setShowVersionHistory(true);
              void loadVersionHistory();
            }}
          >
            Version History
          </Button>
        </Tooltip>
        <Tooltip
          content={
            selectedScenes.size < 2
              ? 'Select at least 2 scenes to merge'
              : `Merge ${selectedScenes.size} selected scenes`
          }
          relationship="label"
        >
          <Button
            appearance="subtle"
            icon={<Merge24Regular />}
            onClick={handleMergeScenes}
            disabled={selectedScenes.size < 2 || isMerging}
          >
            {isMerging
              ? 'Merging...'
              : `Merge Scenes${selectedScenes.size > 0 ? ` (${selectedScenes.size})` : ''}`}
          </Button>
        </Tooltip>
      </div>

      {/* Merge Actions Helper */}
      {selectedScenes.size > 0 && (
        <div className={styles.mergeActions}>
          <Text size={200}>
            {selectedScenes.size} scene{selectedScenes.size > 1 ? 's' : ''} selected
          </Text>
          <Button size="small" appearance="subtle" onClick={() => setSelectedScenes(new Set())}>
            Clear Selection
          </Button>
        </div>
      )}

      {/* Enhancement Panel */}
      {showEnhancement && (
        <Card className={styles.enhancementPanel}>
          <Title3>Script Enhancement</Title3>
          <Text>Adjust tone and pacing to refine your script</Text>

          <div className={styles.sliderGroup}>
            <Label>Tone Adjustment</Label>
            <Text size={200}>
              {toneAdjustment < 0 && 'More Calm'}
              {toneAdjustment === 0 && 'Neutral'}
              {toneAdjustment > 0 && 'More Energetic'}
            </Text>
            <Slider
              min={-1}
              max={1}
              step={0.1}
              value={toneAdjustment}
              onChange={(_, data) => setToneAdjustment(data.value)}
            />
          </div>

          <div className={styles.sliderGroup}>
            <Label>Pacing Adjustment</Label>
            <Text size={200}>
              {pacingAdjustment < 0 && 'Slower'}
              {pacingAdjustment === 0 && 'Neutral'}
              {pacingAdjustment > 0 && 'Faster'}
            </Text>
            <Slider
              min={-1}
              max={1}
              step={0.1}
              value={pacingAdjustment}
              onChange={(_, data) => setPacingAdjustment(data.value)}
            />
          </div>

          <div style={{ display: 'flex', gap: tokens.spacingHorizontalM }}>
            <Button
              appearance="primary"
              onClick={handleEnhanceScript}
              disabled={isEnhancing || (toneAdjustment === 0 && pacingAdjustment === 0)}
            >
              {isEnhancing ? 'Applying...' : 'Apply Enhancement'}
            </Button>
            <Button
              appearance="subtle"
              onClick={() => {
                setToneAdjustment(0);
                setPacingAdjustment(0);
              }}
            >
              Reset
            </Button>
          </div>
        </Card>
      )}

      {/* Version History Dialog */}
      <Dialog
        open={showVersionHistory}
        onOpenChange={(_, data) => setShowVersionHistory(data.open)}
      >
        <DialogSurface>
          <DialogTitle>Version History</DialogTitle>
          <DialogBody>
            <DialogContent>
              {isLoadingVersions && (
                <div
                  style={{
                    display: 'flex',
                    justifyContent: 'center',
                    padding: tokens.spacingVerticalL,
                  }}
                >
                  <Spinner size="medium" />
                </div>
              )}
              {!isLoadingVersions && versionHistory && versionHistory.versions.length === 0 && (
                <Text>No version history available yet.</Text>
              )}
              {!isLoadingVersions && versionHistory && versionHistory.versions.length > 0 && (
                <div className={styles.versionList}>
                  {versionHistory.versions.map((version) => (
                    <div key={version.versionId} className={styles.versionItem}>
                      <div>
                        <Text weight="semibold">Version {version.versionNumber}</Text>
                        <Text
                          size={200}
                          style={{ display: 'block', color: tokens.colorNeutralForeground3 }}
                        >
                          {new Date(version.createdAt).toLocaleString()}
                        </Text>
                        {version.notes && (
                          <Text
                            size={200}
                            style={{ display: 'block', marginTop: tokens.spacingVerticalXXS }}
                          >
                            {version.notes}
                          </Text>
                        )}
                      </div>
                      <Button
                        size="small"
                        onClick={() => {
                          void handleRevertToVersion(version.versionId);
                          setShowVersionHistory(false);
                        }}
                      >
                        Revert
                      </Button>
                    </div>
                  ))}
                </div>
              )}
            </DialogContent>
          </DialogBody>
          <DialogActions>
            <Button appearance="secondary" onClick={() => setShowVersionHistory(false)}>
              Close
            </Button>
          </DialogActions>
        </DialogSurface>
      </Dialog>

      {/* Split Scene Dialog */}
      <Dialog open={showSplitDialog} onOpenChange={(_, data) => setShowSplitDialog(data.open)}>
        <DialogSurface>
          <DialogTitle>Split Scene {splitSceneNumber}</DialogTitle>
          <DialogBody>
            <DialogContent>
              <div className={styles.splitDialog}>
                <Text>
                  Enter the character position where you want to split the scene. The scene will be
                  divided into two parts at this position.
                </Text>
                {splitSceneNumber !== null && generatedScript && (
                  <div>
                    <Text
                      weight="semibold"
                      style={{ display: 'block', marginBottom: tokens.spacingVerticalXS }}
                    >
                      Scene Text (
                      {
                        generatedScript.scenes.find((s) => s.number === splitSceneNumber)?.narration
                          .length
                      }{' '}
                      characters):
                    </Text>
                    <Text
                      size={200}
                      style={{
                        display: 'block',
                        fontFamily: 'monospace',
                        padding: tokens.spacingVerticalS,
                        backgroundColor: tokens.colorNeutralBackground2,
                        borderRadius: tokens.borderRadiusSmall,
                      }}
                    >
                      {generatedScript.scenes.find((s) => s.number === splitSceneNumber)?.narration}
                    </Text>
                  </div>
                )}
                <Field label="Split Position (character index)">
                  <Input
                    type="number"
                    value={splitPosition}
                    onChange={(e) => setSplitPosition(e.target.value)}
                    placeholder="e.g., 50"
                  />
                </Field>
              </div>
            </DialogContent>
          </DialogBody>
          <DialogActions>
            <Button appearance="secondary" onClick={() => setShowSplitDialog(false)}>
              Cancel
            </Button>
            <Button
              appearance="primary"
              onClick={handleSplitScene}
              disabled={!splitPosition || isSplitting}
            >
              {isSplitting ? 'Splitting...' : 'Split Scene'}
            </Button>
          </DialogActions>
        </DialogSurface>
      </Dialog>

      <div className={styles.scenesContainer}>
        {generatedScript.scenes.map((scene, index) => {
          const durationStatus = isSceneDurationAppropriate(scene);
          const currentNarration = editingScenes[scene.number] ?? scene.narration;
          const isSelected = selectedScenes.has(scene.number);
          const isDragging = draggedSceneIndex === index;

          return (
            <Card
              key={scene.number}
              className={`${styles.sceneCard} ${isDragging ? styles.sceneCardDragging : ''} ${styles.sceneCardDraggable}`}
              draggable
              onDragStart={handleDragStart(index)}
              onDragOver={handleDragOver(index)}
              onDragEnd={handleDragEnd}
            >
              <div className={styles.sceneHeader}>
                <div className={styles.sceneNumber}>
                  <div className={styles.sceneSelection}>
                    <Checkbox
                      checked={isSelected}
                      onChange={(_, data) =>
                        handleSceneSelection(scene.number, data.checked === true)
                      }
                    />
                  </div>
                  <Badge appearance="filled" color="brand">
                    Scene {scene.number}
                  </Badge>
                  {durationStatus === 'short' && (
                    <Badge appearance="outline" color="warning">
                      Too Short
                    </Badge>
                  )}
                  {durationStatus === 'long' && (
                    <Badge appearance="outline" color="danger">
                      Too Long
                    </Badge>
                  )}
                  {savingScenes[scene.number] && (
                    <div className={styles.savingIndicator}>
                      <Spinner size="tiny" />
                      <Save24Regular />
                      <Text size={200}>Saving...</Text>
                    </div>
                  )}
                </div>
                <div className={styles.sceneActions}>
                  <Tooltip content="Regenerate this scene" relationship="label">
                    <Button
                      size="small"
                      icon={<ArrowClockwise24Regular />}
                      onClick={() => handleRegenerateScene(scene.number)}
                      disabled={regeneratingScenes[scene.number]}
                    >
                      {regeneratingScenes[scene.number] ? 'Regenerating...' : 'Regenerate'}
                    </Button>
                  </Tooltip>
                  <Tooltip content="Split this scene" relationship="label">
                    <Button
                      size="small"
                      appearance="subtle"
                      icon={<SplitVertical24Regular />}
                      onClick={() => {
                        setSplitSceneNumber(scene.number);
                        setShowSplitDialog(true);
                      }}
                    />
                  </Tooltip>
                  {generatedScript.scenes.length > 1 && (
                    <Tooltip content="Delete this scene" relationship="label">
                      <Button
                        size="small"
                        appearance="subtle"
                        icon={<Delete24Regular />}
                        onClick={() => void handleDeleteScene(scene.number)}
                      />
                    </Tooltip>
                  )}
                </div>
              </div>

              <Field className={styles.narrationField} label="Narration">
                <Textarea
                  value={currentNarration}
                  onChange={(e) => handleSceneEdit(scene.number, e.target.value)}
                  rows={4}
                  resize="vertical"
                />
              </Field>

              <div className={styles.sceneMetadata}>
                <div
                  style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalXS }}
                >
                  <Clock24Regular />
                  <Text>{scene.durationSeconds.toFixed(1)}s</Text>
                </div>
                <div
                  style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalXS }}
                >
                  <TextGrammarCheckmark24Regular />
                  <Text>
                    {scene.narration.split(/\s+/).filter((word) => word.length > 0).length} words
                  </Text>
                </div>
                <div
                  style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalXS }}
                >
                  <Text>Transition: {scene.transition}</Text>
                </div>
              </div>

              <Divider
                style={{
                  marginTop: tokens.spacingVerticalM,
                  marginBottom: tokens.spacingVerticalM,
                }}
              />

              <Field label="Visual Prompt">
                <Text size={200}>{scene.visualPrompt}</Text>
              </Field>
            </Card>
          );
        })}
      </div>
    </div>
  );
};

export const ScriptReview = memo(ScriptReviewComponent);
