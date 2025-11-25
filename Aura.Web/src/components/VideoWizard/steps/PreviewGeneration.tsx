import {
  makeStyles,
  tokens,
  Title2,
  Title3,
  Text,
  Button,
  Card,
  Spinner,
  ProgressBar,
  Badge,
  Tooltip,
  Dropdown,
  Option,
  Field,
  Slider,
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogContent,
  DialogActions,
  Menu,
  MenuItem,
  MenuList,
  MenuPopover,
  MenuTrigger,
} from '@fluentui/react-components';
import {
  Play24Regular,
  ArrowClockwise24Regular,
  Warning24Regular,
  Image24Regular,
  Speaker224Regular,
  Settings24Regular,
  CheckmarkCircle24Regular,
  ErrorCircle24Regular,
  ArrowUpload24Regular,
  MoreHorizontal24Regular,
  ImageEdit24Regular,
  Search24Regular,
} from '@fluentui/react-icons';
import React, { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import type { FC } from 'react';
import type {
  PreviewData,
  ScriptData,
  StyleData,
  StepValidation,
  ScriptScene,
  PreviewThumbnail,
} from '../types';
import { getVisualsClient, VisualsClient } from '@/api/visualsClient';
import type { VisualProvider, BatchGenerateProgress } from '@/api/visualsClient';
import { ttsService, type TtsProvider, type TtsVoice } from '@/services/ttsService';
import apiClient from '@/services/api/apiClient';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXL,
  },
  header: {
    marginBottom: tokens.spacingVerticalL,
  },
  generationCard: {
    padding: tokens.spacingVerticalXXL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusLarge,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalXL,
    textAlign: 'center',
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    boxShadow: '0 2px 8px rgba(0, 0, 0, 0.04), 0 1px 3px rgba(0, 0, 0, 0.06)',
  },
  previewGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))',
    gap: tokens.spacingHorizontalXL,
    marginTop: tokens.spacingVerticalXL,
  },
  sceneCard: {
    padding: tokens.spacingVerticalL,
    cursor: 'pointer',
    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    borderRadius: tokens.borderRadiusLarge,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    backgroundColor: tokens.colorNeutralBackground1,
    ':hover': {
      transform: 'translateY(-4px)',
      boxShadow: '0 12px 32px rgba(0, 0, 0, 0.12), 0 4px 12px rgba(0, 0, 0, 0.08)',
      borderColor: tokens.colorBrandStroke1,
    },
    ':active': {
      transform: 'translateY(-2px)',
    },
  },
  scenePreview: {
    width: '100%',
    height: '180px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusLarge,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: tokens.spacingVerticalL,
    position: 'relative',
    overflow: 'hidden',
    boxShadow: 'inset 0 2px 8px rgba(0, 0, 0, 0.06)',
    border: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  sceneImage: {
    width: '100%',
    height: '100%',
    objectFit: 'cover',
  },
  sceneDetails: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  sceneActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalM,
    justifyContent: 'space-between',
  },
  progressSection: {
    width: '100%',
    maxWidth: '500px',
  },
  statsRow: {
    display: 'flex',
    justifyContent: 'space-around',
    padding: `${tokens.spacingVerticalXL} ${tokens.spacingHorizontalXXL}`,
    gap: tokens.spacingHorizontalXXL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusLarge,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    boxShadow: '0 1px 3px rgba(0, 0, 0, 0.05)',
  },
  statItem: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalS,
  },
  audioPreview: {
    width: '100%',
    marginTop: tokens.spacingVerticalM,
    height: '40px',
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusSmall,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  providerCard: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalL,
  },
  providerGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalM,
  },
  providerOption: {
    padding: tokens.spacingVerticalL,
    border: `2px solid ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusLarge,
    cursor: 'pointer',
    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    backgroundColor: tokens.colorNeutralBackground1,
    ':hover': {
      transform: 'translateY(-3px)',
      boxShadow: '0 8px 24px rgba(0, 0, 0, 0.12), 0 2px 8px rgba(0, 0, 0, 0.08)',
      borderColor: tokens.colorBrandStroke1,
    },
  },
  selectedProvider: {
    border: `2px solid ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorBrandBackground2,
    boxShadow: `0 0 0 3px ${tokens.colorBrandBackground2}40`,
  },
  settingsRow: {
    display: 'flex',
    gap: tokens.spacingHorizontalL,
    marginTop: tokens.spacingVerticalL,
    flexWrap: 'wrap',
  },
  settingItem: {
    flex: 1,
    minWidth: '200px',
  },
  imageOverlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: 'rgba(0, 0, 0, 0.75)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    opacity: 0,
    transition: 'opacity 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    backdropFilter: 'blur(4px)',
  },
  sceneCardHover: {
    ':hover .image-overlay': {
      opacity: 1,
    },
  },
  badgeGroup: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
    position: 'absolute',
    top: tokens.spacingVerticalS,
    left: tokens.spacingHorizontalS,
    flexWrap: 'wrap',
  },
  fullscreenDialog: {
    maxWidth: '90vw',
    maxHeight: '90vh',
  },
  fullscreenImage: {
    width: '100%',
    height: 'auto',
    maxHeight: '70vh',
    objectFit: 'contain',
  },
  imageDetails: {
    marginTop: tokens.spacingVerticalL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
});

interface PreviewGenerationProps {
  data: PreviewData;
  scriptData: ScriptData;
  styleData: StyleData;
  advancedMode: boolean;
  onChange: (data: PreviewData) => void;
  onValidationChange: (validation: StepValidation) => void;
}

type GenerationStatus = 'idle' | 'generating' | 'completed' | 'error';

export const PreviewGeneration: FC<PreviewGenerationProps> = ({
  data,
  scriptData,
  styleData,
  advancedMode,
  onChange,
  onValidationChange,
}) => {
  const styles = useStyles();

  // Memoize visualsClient to ensure consistent instance across renders
  const visualsClient = useMemo<VisualsClient>(() => getVisualsClient(), []);

  // State hooks - all declared unconditionally at top level
  const [status, setStatus] = useState<GenerationStatus>('idle');
  const [progress, setProgress] = useState(0);
  const [currentStage, setCurrentStage] = useState('');
  const [regeneratingScene, setRegeneratingScene] = useState<string | null>(null);
  const [providers, setProviders] = useState<VisualProvider[]>([]);
  const [selectedProvider, setSelectedProvider] = useState<string>('');
  const [imageStyle, setImageStyle] = useState<string>(styleData.imageStyle || 'photorealistic');
  const [imageQuality, setImageQuality] = useState<number>(styleData.imageQuality || 80);
  const [aspectRatio, setAspectRatio] = useState<string>(styleData.imageAspectRatio || '16:9');
  const [showProviderSettings, setShowProviderSettings] = useState(false);
  const [availableStyles, setAvailableStyles] = useState<string[]>([]);
  const [fullscreenImage, setFullscreenImage] = useState<{
    url: string;
    scene: ScriptScene;
    thumbnail: PreviewThumbnail | null;
  } | null>(null);
  const [providerLoadError, setProviderLoadError] = useState<string | null>(null);
  const [isLoadingProviders, setIsLoadingProviders] = useState(true);
  const [playingSceneId, setPlayingSceneId] = useState<string | null>(null);
  const [audioError, setAudioError] = useState<string | null>(null);
  const audioRef = useRef<HTMLAudioElement | null>(null);
  
  // TTS provider state
  const [ttsProviders, setTtsProviders] = useState<TtsProvider[]>([]);
  const [selectedTtsProvider, setSelectedTtsProvider] = useState<string>(styleData.voiceProvider || '');
  const [ttsVoices, setTtsVoices] = useState<TtsVoice[]>([]);
  const [selectedTtsVoice, setSelectedTtsVoice] = useState<string>(styleData.voiceName || '');
  const [isLoadingTtsProviders, setIsLoadingTtsProviders] = useState(true);
  const [isLoadingTtsVoices, setIsLoadingTtsVoices] = useState(false);
  const [ttsProviderStatus, setTtsProviderStatus] = useState<Record<string, { isAvailable: boolean; error?: string }>>({});

  // Use ref to track if providers have been selected to avoid stale closure issues
  const hasSelectedProviderRef = useRef(false);

  // Validate script data before proceeding
  const hasValidScriptData = useMemo(() => {
    return scriptData && Array.isArray(scriptData.scenes) && scriptData.scenes.length > 0;
  }, [scriptData]);

  const hasPreviewData = useMemo(() => {
    return data.thumbnails.length > 0 && data.audioSamples.length > 0;
  }, [data]);

  // Initialize status when component mounts with existing preview data
  useEffect(() => {
    if (hasPreviewData && status === 'idle') {
      setStatus('completed');
    }
  }, [hasPreviewData, status]);

  // Reset preview data if script scenes change significantly (e.g., script was regenerated)
  useEffect(() => {
    if (hasValidScriptData && data.thumbnails.length > 0) {
      const scriptSceneIds = new Set(scriptData.scenes.map((s) => s.id));
      const previewSceneIds = new Set(data.thumbnails.map((t) => t.sceneId));
      
      // Check if scenes have changed (different IDs or count mismatch)
      const scenesChanged = 
        scriptData.scenes.length !== data.thumbnails.length ||
        !scriptData.scenes.every((scene) => previewSceneIds.has(scene.id)) ||
        !data.thumbnails.every((thumb) => scriptSceneIds.has(thumb.sceneId));

      if (scenesChanged && status === 'completed') {
        // Script was regenerated, reset preview status
        setStatus('idle');
        onChange({
          thumbnails: [],
          audioSamples: [],
        });
      }
    }
  }, [scriptData.scenes, data.thumbnails, hasValidScriptData, status, onChange]);

  // Validation effect - always called
  useEffect(() => {
    if (!hasValidScriptData) {
      onValidationChange({
        isValid: false,
        errors: ['Script data is missing. Please go back and generate a script first.'],
      });
      return;
    }

    if (hasPreviewData) {
      onValidationChange({ isValid: true, errors: [] });
    } else {
      onValidationChange({ isValid: false, errors: ['Preview generation required'] });
    }
  }, [hasPreviewData, hasValidScriptData, onValidationChange]);

  // Load providers callback - no dependencies on selectedProvider to avoid stale closure
  const loadProviders = useCallback(async () => {
    setIsLoadingProviders(true);
    setProviderLoadError(null);

    try {
      const response = await visualsClient.getProviders();
      setProviders(response.providers);

      // Only set provider if one hasn't been selected yet
      if (!hasSelectedProviderRef.current) {
        // Prefer non-placeholder providers, but always fall back to Placeholder if needed
        const availableProvider = response.providers.find(
          (p) => p.isAvailable && p.name !== 'Placeholder'
        );
        const placeholderProvider = response.providers.find(
          (p) => p.name === 'Placeholder' && p.isAvailable
        );

        if (availableProvider) {
          setSelectedProvider(availableProvider.name);
          hasSelectedProviderRef.current = true;
        } else if (placeholderProvider) {
          // Placeholder is always available as guaranteed fallback
          setSelectedProvider('Placeholder');
          hasSelectedProviderRef.current = true;
          console.info('[PreviewGeneration] Using Placeholder provider as fallback');
        }
      }
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to load providers';
      console.error('Failed to load providers:', error);
      setProviderLoadError(errorMessage);
      
      // Even if loading fails, set Placeholder as fallback
      if (!hasSelectedProviderRef.current) {
        setSelectedProvider('Placeholder');
        hasSelectedProviderRef.current = true;
        setProviders([
          {
            name: 'Placeholder',
            isAvailable: true,
            requiresApiKey: false,
            capabilities: {
              providerName: 'Placeholder',
              supportsNegativePrompts: false,
              supportsBatchGeneration: true,
              supportsStylePresets: false,
              supportedAspectRatios: ['16:9', '9:16', '1:1', '4:3'],
              supportedStyles: ['solid'],
              maxWidth: 4096,
              maxHeight: 4096,
              isLocal: true,
              isFree: true,
              costPerImage: 0,
              tier: 'Free',
            },
          },
        ]);
      }
    } finally {
      setIsLoadingProviders(false);
    }
  }, [visualsClient]);

  // Load styles callback
  const loadStyles = useCallback(async () => {
    try {
      const response = await visualsClient.getStyles();
      setAvailableStyles(response.allStyles);
    } catch (error: unknown) {
      console.error('Failed to load styles:', error);
      setAvailableStyles(['photorealistic', 'artistic', 'cinematic', 'minimalist']);
    }
  }, [visualsClient]);

  // Load TTS providers
  const loadTtsProviders = useCallback(async () => {
    setIsLoadingTtsProviders(true);
    try {
      const providers = await ttsService.getAvailableProviders();
      setTtsProviders(providers);
      
      // Initialize selected provider if not set
      if (!selectedTtsProvider && providers.length > 0) {
        // Prefer Windows (free) or first available
        const windowsProvider = providers.find(p => p.name === 'Windows');
        const firstAvailable = providers[0];
        const initialProvider = windowsProvider || firstAvailable;
        if (initialProvider) {
          setSelectedTtsProvider(initialProvider.name);
        }
      }
      
      // Check status of all providers
      try {
        const statusResponse = await apiClient.get<{
          success: boolean;
          providers: Array<{ name: string; isAvailable: boolean; error?: string }>;
        }>('/api/tts/status');
        
        if (statusResponse.data.success) {
          const statusMap: Record<string, { isAvailable: boolean; error?: string }> = {};
          statusResponse.data.providers.forEach((p: { name: string; isAvailable: boolean; error?: string }) => {
            statusMap[p.name] = { isAvailable: p.isAvailable, error: p.error };
          });
          setTtsProviderStatus(statusMap);
        }
      } catch (statusError) {
        console.warn('Failed to check TTS provider status:', statusError);
      }
    } catch (error) {
      console.error('Failed to load TTS providers:', error);
    } finally {
      setIsLoadingTtsProviders(false);
    }
  }, [selectedTtsProvider]);

  // Load voices for selected TTS provider
  const loadTtsVoices = useCallback(async (provider: string) => {
    if (!provider) {
      setTtsVoices([]);
      setSelectedTtsVoice('');
      return;
    }
    
    setIsLoadingTtsVoices(true);
    try {
      const voices = await ttsService.getVoicesForProvider(provider);
      setTtsVoices(voices);
      
      // Set first voice if none selected or current voice not available
      if (voices.length > 0) {
        const currentVoiceExists = voices.some(v => v.name === selectedTtsVoice);
        if (!selectedTtsVoice || !currentVoiceExists) {
          setSelectedTtsVoice(voices[0].name);
        }
      } else {
        setSelectedTtsVoice('');
      }
    } catch (error) {
      console.error(`Failed to load voices for provider ${provider}:`, error);
      setTtsVoices([]);
      setSelectedTtsVoice('');
    } finally {
      setIsLoadingTtsVoices(false);
    }
  }, [selectedTtsVoice]);

  // Load voices when provider changes
  useEffect(() => {
    if (selectedTtsProvider) {
      void loadTtsVoices(selectedTtsProvider);
      // Clear audio errors when provider changes
      setAudioError(null);
    }
  }, [selectedTtsProvider, loadTtsVoices]);
  
  // Clear audio error when voice changes
  useEffect(() => {
    if (selectedTtsVoice) {
      setAudioError(null);
    }
  }, [selectedTtsVoice]);

  // Effect to load providers and styles on mount - properly includes dependencies
  useEffect(() => {
    void loadProviders();
    void loadStyles();
    void loadTtsProviders();
  }, [loadProviders, loadStyles, loadTtsProviders]);

  // Cleanup audio on unmount
  useEffect(() => {
    return () => {
      if (audioRef.current) {
        audioRef.current.pause();
        if (audioRef.current.src.startsWith('blob:')) {
          URL.revokeObjectURL(audioRef.current.src);
        }
        audioRef.current = null;
      }
    };
  }, []);

  // Update ref when selectedProvider changes
  useEffect(() => {
    if (selectedProvider) {
      hasSelectedProviderRef.current = true;
    }
  }, [selectedProvider]);

  const generatePreviews = useCallback(async () => {
    // Validate script data before generation
    if (!hasValidScriptData) {
      setStatus('error');
      setCurrentStage('No script data available. Please generate a script first.');
      return;
    }

    // Validate that a provider is selected, or use Placeholder as fallback
    const providerToUse = selectedProvider || 'Placeholder';
    if (!providerToUse) {
      setStatus('error');
      setCurrentStage('Please select an image provider before generating previews.');
      return;
    }

    setStatus('generating');
    setProgress(0);
    setCurrentStage('Initializing preview generation...');

    try {
      const prompts = scriptData.scenes.map((scene) => {
        const visualDesc = scene.visualDescription || scene.text.substring(0, 100);
        return `${styleData.visualStyle} style: ${visualDesc}`;
      });

      setCurrentStage('Generating images with AI...');

      const response = await visualsClient.batchGenerate(
        {
          prompts,
          style: imageStyle,
          aspectRatio,
          quality: imageQuality,
        },
        (batchProgress: BatchGenerateProgress) => {
          setProgress(batchProgress.progressPercentage * 0.8);
          setCurrentStage(
            `Generating image ${batchProgress.completedCount} of ${batchProgress.totalCount}...`
          );
        }
      );

      const thumbnails: PreviewThumbnail[] = response.images.map((img, index) => ({
        sceneId: scriptData.scenes[index].id,
        imageUrl: img.imagePath,
        caption:
          scriptData.scenes[index].visualDescription ||
          scriptData.scenes[index].text.substring(0, 50),
        provider: response.provider,
        generatedAt: img.generatedAt,
        quality: img.quality,
        clipScore: img.clipScore,
        isPlaceholder: false,
      }));

      for (let i = thumbnails.length; i < scriptData.scenes.length; i++) {
        const fallbackThumbnail: PreviewThumbnail = {
          sceneId: scriptData.scenes[i].id,
          imageUrl: `https://via.placeholder.com/400x300/6264a7/ffffff?text=Scene+${i + 1}`,
          caption: scriptData.scenes[i].text.substring(0, 50),
          isPlaceholder: true,
          failureReason: 'Generation failed - using placeholder',
        };
        thumbnails.push(fallbackThumbnail);
      }

      setCurrentStage('Generating audio previews...');
      setProgress(80);

      const audioSamples = [];
      for (let i = 0; i < scriptData.scenes.length; i++) {
        const scene = scriptData.scenes[i];
        await new Promise((resolve) => setTimeout(resolve, 300));

        audioSamples.push({
          sceneId: scene.id,
          audioUrl: `https://example.com/audio/${scene.id}.mp3`,
          duration: scene.duration,
          waveformData: Array.from({ length: 50 }, () => Math.random() * 100),
        });

        setProgress(80 + ((i + 1) / scriptData.scenes.length) * 20);
      }

      onChange({
        thumbnails,
        audioSamples,
        imageProvider: response.provider,
      });

      setProgress(100);
      setCurrentStage('Preview generation completed!');
      setStatus('completed');
    } catch (error: unknown) {
      console.error('Preview generation failed:', error);
      setStatus('error');
      setCurrentStage('Preview generation failed. Using placeholder images.');

      const fallbackThumbnails = scriptData.scenes.map((scene, index) => ({
        sceneId: scene.id,
        imageUrl: `https://via.placeholder.com/400x300/6264a7/ffffff?text=Scene+${index + 1}`,
        caption: scene.text.substring(0, 50),
        isPlaceholder: true,
        failureReason: error instanceof Error ? error.message : 'Unknown error',
      }));

      onChange({
        thumbnails: fallbackThumbnails,
        audioSamples: data.audioSamples,
      });

      onValidationChange({ isValid: false, errors: ['Preview generation failed'] });
    }
  }, [
    hasValidScriptData,
    selectedProvider,
    scriptData.scenes,
    styleData.visualStyle,
    imageStyle,
    aspectRatio,
    imageQuality,
    visualsClient,
    onChange,
    onValidationChange,
    data.audioSamples,
  ]);

  const regenerateScene = useCallback(
    async (sceneId: string) => {
      setRegeneratingScene(sceneId);

      try {
        const scene = scriptData.scenes.find((s) => s.id === sceneId);
        if (!scene) return;

        const visualDesc = scene.visualDescription || scene.text.substring(0, 100);
        const prompt = `${styleData.visualStyle} style: ${visualDesc}`;

        const response = await visualsClient.generateImage({
          prompt,
          style: imageStyle,
          aspectRatio,
          quality: imageQuality,
        });

        const updatedThumbnails = data.thumbnails.map((thumb) =>
          thumb.sceneId === sceneId
            ? {
                ...thumb,
                imageUrl: response.imagePath,
                provider: response.provider,
                generatedAt: response.generatedAt,
                isPlaceholder: false,
                failureReason: undefined,
              }
            : thumb
        );

        onChange({
          thumbnails: updatedThumbnails,
          audioSamples: data.audioSamples,
          imageProvider: response.provider,
        });
      } catch (error) {
        console.error('Scene regeneration failed:', error);

        const updatedThumbnails = data.thumbnails.map((thumb) =>
          thumb.sceneId === sceneId
            ? {
                ...thumb,
                failureReason: error instanceof Error ? error.message : 'Regeneration failed',
              }
            : thumb
        );

        onChange({
          thumbnails: updatedThumbnails,
          audioSamples: data.audioSamples,
        });
      } finally {
        setRegeneratingScene(null);
      }
    },
    [
      scriptData.scenes,
      styleData.visualStyle,
      imageStyle,
      aspectRatio,
      imageQuality,
      data,
      onChange,
      visualsClient,
    ]
  );

  const handleManualUpload = useCallback(
    (sceneId: string) => {
      const input = document.createElement('input');
      input.type = 'file';
      input.accept = 'image/*';
      input.onchange = (e) => {
        const file = (e.target as HTMLInputElement).files?.[0];
        if (file) {
          const reader = new FileReader();
          reader.onload = (event) => {
            const imageUrl = event.target?.result as string;
            const updatedThumbnails = data.thumbnails.map((thumb) =>
              thumb.sceneId === sceneId
                ? {
                    ...thumb,
                    imageUrl,
                    provider: 'Manual Upload',
                    isPlaceholder: false,
                  }
                : thumb
            );

            onChange({
              thumbnails: updatedThumbnails,
              audioSamples: data.audioSamples,
            });
          };
          reader.readAsDataURL(file);
        }
      };
      input.click();
    },
    [data, onChange]
  );

  const handleSearchFallback = useCallback(async (sceneId: string) => {
    console.info('Search fallback for scene:', sceneId);
  }, []);

  const playScenePreview = useCallback(
    async (sceneId: string) => {
      try {
        // Prevent multiple simultaneous plays
        if (playingSceneId === sceneId) {
          // If already playing this scene, stop it
          if (audioRef.current) {
            audioRef.current.pause();
            if (audioRef.current.src.startsWith('blob:')) {
              URL.revokeObjectURL(audioRef.current.src);
            }
            audioRef.current = null;
          }
          setPlayingSceneId(null);
          return;
        }

        // Stop any currently playing audio
        if (audioRef.current) {
          audioRef.current.pause();
          if (audioRef.current.src.startsWith('blob:')) {
            URL.revokeObjectURL(audioRef.current.src);
          }
          audioRef.current = null;
        }

        setPlayingSceneId(sceneId);
        setAudioError(null);

        // Find the scene
        const scene = scriptData.scenes.find((s) => s.id === sceneId);
        if (!scene) {
          throw new Error('Scene not found');
        }

        // Use the selected TTS provider, fallback to styleData if not set
        const apiProvider = selectedTtsProvider || styleData.voiceProvider;
        const voiceName = selectedTtsVoice || styleData.voiceName || 'default';
        
        if (!apiProvider) {
          throw new Error('No TTS provider selected. Please select a TTS provider in the settings.');
        }
        
        // Verify provider is available
        const providerStatus = ttsProviderStatus[apiProvider];
        if (providerStatus && !providerStatus.isAvailable) {
          throw new Error(
            `TTS provider "${apiProvider}" is not available. ${providerStatus.error || 'Please select a different provider.'}`
          );
        }

        // Generate audio preview and get the file directly
        const previewResponse = await ttsService.generatePreview({
          provider: apiProvider,
          voice: voiceName,
          sampleText: scene.text,
        });

        if (!previewResponse.success || !previewResponse.audioPath) {
          throw new Error('Failed to generate audio preview');
        }

        // Fetch the audio file - try to get it as a blob from the server
        let audioUrl: string;
        let _isBlobUrl = false;
        let _detectedMimeType = 'audio/wav';
        
        if (previewResponse.audioPath.startsWith('http://') || previewResponse.audioPath.startsWith('https://')) {
          // Already a URL
          audioUrl = previewResponse.audioPath;
        } else {
          // Server file path - fetch it as a blob using returnFile parameter
          try {
            const response = await apiClient.post(
              '/api/tts/preview?returnFile=true',
              {
                provider: apiProvider,
                voice: voiceName,
                sampleText: scene.text,
              },
              {
                responseType: 'blob',
                validateStatus: (status) => status < 500, // Don't throw on 4xx, we'll handle it
              }
            );
            
            // Check if response is actually an error (sometimes errors come as JSON in blob responses)
            if (response.status >= 400) {
              // Try to parse error message from blob
              try {
                // Clone the blob before reading it, so we don't consume it
                const blobCopy = response.data instanceof Blob 
                  ? response.data.slice() 
                  : new Blob([response.data]);
                const text = await blobCopy.text();
                const errorData = JSON.parse(text);
                throw new Error(errorData.error || errorData.message || `Server returned error: ${response.status}`);
              } catch (_parseError) {
                // If parsing fails, just report the status code
                throw new Error(`Failed to fetch audio file: Server returned ${response.status} ${response.statusText || ''}`);
              }
            }
            
            // Detect MIME type from response headers (axios uses lowercase header names)
            const contentType = response.headers['content-type'] || 
                                response.headers['Content-Type'] ||
                                (previewResponse.audioPath.endsWith('.mp3') ? 'audio/mpeg' :
                                 previewResponse.audioPath.endsWith('.opus') ? 'audio/opus' :
                                 previewResponse.audioPath.endsWith('.wav') ? 'audio/wav' :
                                 'audio/wav');
            
            _detectedMimeType = contentType;
            
            // Re-create blob from response data (axios blob response)
            // We need to recreate it because axios might return it as a different Blob type
            let blob: Blob;
            if (response.data instanceof Blob) {
              // If it's already a Blob, verify it's the right type or recreate with correct MIME
              if (response.data.type === contentType) {
                blob = response.data;
              } else {
                // Recreate with correct MIME type
                blob = new Blob([response.data], { type: contentType });
              }
            } else if (response.data instanceof ArrayBuffer) {
              blob = new Blob([response.data], { type: contentType });
            } else {
              // Fallback: assume it's binary data
              blob = new Blob([response.data], { type: contentType });
            }
            
            // Verify blob is not empty
            if (blob.size === 0) {
              throw new Error('Received empty audio file from server. The audio generation may have failed.');
            }
            
            audioUrl = URL.createObjectURL(blob);
            _isBlobUrl = true;
            
            console.info(`Audio fetched successfully. Type: ${contentType}, Size: ${blob.size} bytes`);
          } catch (fetchError: unknown) {
            const error = fetchError instanceof Error ? fetchError : new Error(String(fetchError));
            // Don't fallback silently - throw the error so user knows what went wrong
            console.error('Failed to fetch audio as blob:', error);
            throw new Error(`Failed to retrieve audio file: ${error.message}. Please try regenerating the preview.`);
          }
        }

        // Clean up any existing audio
        if (audioRef.current) {
          audioRef.current.pause();
          audioRef.current.src = '';
          audioRef.current.load(); // Reset the audio element
          if (audioRef.current.src.startsWith('blob:')) {
            URL.revokeObjectURL(audioRef.current.src);
          }
          audioRef.current = null;
        }

        // Create new audio element
        const audio = new Audio();
        audioRef.current = audio;

        // Set audio properties before loading
        audio.volume = 1.0; // Ensure volume is at maximum
        audio.preload = 'auto'; // Preload the audio
        audio.crossOrigin = 'anonymous'; // For CORS if needed

        // Handle audio events with proper error handling
        const handleEnded = () => {
          console.log('Audio playback ended');
          setPlayingSceneId(null);
          if (audio.src.startsWith('blob:')) {
            URL.revokeObjectURL(audio.src);
          }
          if (audioRef.current === audio) {
            audioRef.current = null;
          }
        };

        const handleError = (e: Event) => {
          const error = audio.error;
          let errorMessage = 'Failed to play audio preview';
          
          if (error) {
            switch (error.code) {
              case error.MEDIA_ERR_ABORTED:
                errorMessage = 'Audio playback was aborted';
                break;
              case error.MEDIA_ERR_NETWORK:
                errorMessage = 'Network error while loading audio';
                break;
              case error.MEDIA_ERR_DECODE:
                errorMessage = 'Audio file could not be decoded. The file may be corrupted or in an unsupported format.';
                break;
              case error.MEDIA_ERR_SRC_NOT_SUPPORTED:
                errorMessage = 'Audio format not supported by your browser';
                break;
              default:
                errorMessage = `Audio error: ${error.message || 'Unknown error'}`;
            }
            console.error('Audio playback error:', error, errorMessage);
          } else {
            console.error('Audio playback error event:', e);
          }
          
          setAudioError(errorMessage);
          setPlayingSceneId(null);
          if (audio.src.startsWith('blob:')) {
            URL.revokeObjectURL(audio.src);
          }
          if (audioRef.current === audio) {
            audioRef.current = null;
          }
        };

        const handleLoadedMetadata = () => {
          console.log('Audio metadata loaded. Duration:', audio.duration, 'seconds');
        };

        const handleCanPlay = () => {
          console.log('Audio can play. Ready state:', audio.readyState);
        };

        const handleCanPlayThrough = () => {
          console.log('Audio can play through. Ready state:', audio.readyState);
        };

        // Add all event listeners
        audio.addEventListener('ended', handleEnded, { once: true });
        audio.addEventListener('error', handleError, { once: true });
        audio.addEventListener('loadedmetadata', handleLoadedMetadata, { once: true });
        audio.addEventListener('canplay', handleCanPlay, { once: true });
        audio.addEventListener('canplaythrough', handleCanPlayThrough, { once: true });

        // Set the audio source and wait for it to load
        audio.src = audioUrl;
        
        // Wait for the audio to be ready before playing
        await new Promise<void>((resolve, reject) => {
          // Create a timeout to prevent infinite waiting
          const timeout = setTimeout(() => {
            cleanup();
            reject(new Error('Audio loading timeout after 30 seconds. The file may be too large or the network too slow.'));
          }, 30000); // 30 second timeout

          const cleanup = () => {
            clearTimeout(timeout);
            audio.removeEventListener('canplaythrough', onCanPlayThrough);
            audio.removeEventListener('canplay', onCanPlay);
            audio.removeEventListener('loadeddata', onLoadedData);
            audio.removeEventListener('error', onError);
          };

          const onCanPlayThrough = () => {
            cleanup();
            console.log('Audio can play through. Ready state:', audio.readyState);
            resolve();
          };

          const onCanPlay = () => {
            // If canplaythrough hasn't fired yet but canplay has, that's usually enough for short clips
            // But we'll still wait for canplaythrough for better reliability
            console.log('Audio can play. Ready state:', audio.readyState);
          };

          const onLoadedData = () => {
            console.log('Audio data loaded. Ready state:', audio.readyState);
          };

          const onError = (_e: Event) => {
            cleanup();
            const error = audio.error;
            let errorMsg = 'Audio failed to load';
            
            if (error) {
              switch (error.code) {
                case error.MEDIA_ERR_ABORTED:
                  errorMsg = 'Audio loading was aborted';
                  break;
                case error.MEDIA_ERR_NETWORK:
                  errorMsg = 'Network error while loading audio';
                  break;
                case error.MEDIA_ERR_DECODE:
                  errorMsg = 'Audio file could not be decoded';
                  break;
                case error.MEDIA_ERR_SRC_NOT_SUPPORTED:
                  errorMsg = 'Audio format not supported by your browser';
                  break;
              }
            }
            
            console.error('Audio loading error:', error, errorMsg);
            reject(new Error(errorMsg));
          };

          // Check if already loaded (shouldn't happen, but handle it)
          if (audio.readyState >= 4) { // HAVE_ENOUGH_DATA
            cleanup();
            console.log('Audio already loaded. Ready state:', audio.readyState);
            resolve();
            return;
          }

          // Listen for the events we need
          audio.addEventListener('canplaythrough', onCanPlayThrough, { once: true });
          audio.addEventListener('canplay', onCanPlay, { once: true });
          audio.addEventListener('loadeddata', onLoadedData, { once: true });
          audio.addEventListener('error', onError, { once: true });
          
          // Explicitly trigger loading
          audio.load();
          
          // For blob URLs, sometimes the events fire synchronously, so check again after a brief delay
          setTimeout(() => {
            if (audio.readyState >= 4 && timeout) {
              cleanup();
              console.log('Audio loaded synchronously. Ready state:', audio.readyState);
              resolve();
            }
          }, 100);
        });

        // Verify audio is valid before playing
        if (!audio.duration || audio.duration === 0) {
          throw new Error('Audio file appears to be empty or invalid');
        }

        if (audio.duration === Infinity || isNaN(audio.duration)) {
          // This might be a live stream or corrupted file
          console.warn('Audio duration is invalid:', audio.duration);
        }

        // Try to play the audio with user interaction context
        try {
          console.log(`Attempting to play audio. Duration: ${audio.duration}s, Volume: ${audio.volume}, Muted: ${audio.muted}`);
          const playPromise = audio.play();
          
          // Modern browsers return a promise
          if (playPromise !== undefined) {
            await playPromise;
            console.log('Audio playback started successfully');
          } else {
            // Fallback for older browsers
            console.log('Audio play() called (no promise returned)');
          }
        } catch (playError: unknown) {
          const error = playError instanceof Error ? playError : new Error(String(playError));
          console.error('Audio playback failed:', error);
          
          // Provide more helpful error messages
          if (error.name === 'NotAllowedError' || error.message.includes('play() request')) {
            throw new Error('Browser blocked autoplay. Please click the preview button again or interact with the page first.');
          } else if (error.name === 'NotSupportedError') {
            throw new Error('Audio format not supported. Please try a different TTS provider.');
          } else {
            throw new Error(`Failed to play audio: ${error.message}`);
          }
        }
      } catch (error) {
        console.error('Error playing scene preview:', error);
        let errorMessage = 'Failed to play audio preview';
        
        if (error instanceof Error) {
          errorMessage = error.message;
          
          // Provide more helpful error messages
          if (errorMessage.includes('not found') || errorMessage.includes('not available')) {
            errorMessage = `TTS provider is not available. Please select a different provider in the TTS settings.`;
          } else if (errorMessage.includes('No TTS provider')) {
            errorMessage = 'No TTS provider selected. Please select a TTS provider in the settings above.';
          } else if (errorMessage.includes('Failed to generate')) {
            errorMessage = `Failed to generate audio. ${errorMessage}`;
          } else if (!errorMessage.includes('Failed to play audio')) {
            // Only add prefix if not already a user-friendly message
            errorMessage = `Audio preview error: ${errorMessage}`;
          }
        }
        
        setAudioError(errorMessage);
        setPlayingSceneId(null);
        if (audioRef.current) {
          if (audioRef.current.src.startsWith('blob:')) {
            URL.revokeObjectURL(audioRef.current.src);
          }
          audioRef.current = null;
        }
      }
    },
    [scriptData.scenes, selectedTtsProvider, selectedTtsVoice, styleData.voiceProvider, styleData.voiceName, playingSceneId, ttsProviderStatus]
  );

  const renderProviderSettings = () => (
    <Card className={styles.providerCard}>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: tokens.spacingVerticalM,
        }}
      >
        <Title3>Image Generation Settings</Title3>
        <Button
          appearance="subtle"
          icon={<Settings24Regular />}
          onClick={() => setShowProviderSettings(!showProviderSettings)}
        >
          {showProviderSettings ? 'Hide' : 'Show'} Settings
        </Button>
      </div>

      {showProviderSettings && (
        <>
          <Text weight="semibold" style={{ marginBottom: tokens.spacingVerticalS }}>
            Select Image Provider
          </Text>
          <div className={styles.providerGrid}>
            {providers.map((provider) => (
              <div
                key={provider.name}
                role="button"
                tabIndex={provider.isAvailable ? 0 : -1}
                aria-disabled={!provider.isAvailable}
                aria-pressed={selectedProvider === provider.name}
                className={`${styles.providerOption} ${
                  selectedProvider === provider.name ? styles.selectedProvider : ''
                }`}
                onClick={() => provider.isAvailable && setSelectedProvider(provider.name)}
                onKeyDown={(e) => {
                  if ((e.key === 'Enter' || e.key === ' ') && provider.isAvailable) {
                    e.preventDefault();
                    setSelectedProvider(provider.name);
                  }
                }}
                style={{
                  opacity: provider.isAvailable ? 1 : 0.5,
                  cursor: provider.isAvailable ? 'pointer' : 'not-allowed',
                }}
              >
                <Text weight="semibold">{provider.name}</Text>
                <div
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: tokens.spacingHorizontalXS,
                    marginTop: tokens.spacingVerticalXS,
                  }}
                >
                  {provider.isAvailable ? (
                    <CheckmarkCircle24Regular
                      style={{ color: tokens.colorPaletteGreenForeground1, fontSize: '14px' }}
                    />
                  ) : (
                    <ErrorCircle24Regular
                      style={{ color: tokens.colorPaletteRedForeground1, fontSize: '14px' }}
                    />
                  )}
                  <Text size={200}>{provider.isAvailable ? 'Available' : 'Not Available'}</Text>
                </div>
                {provider.capabilities && (
                  <Text
                    size={200}
                    style={{
                      marginTop: tokens.spacingVerticalXS,
                      color: tokens.colorNeutralForeground3,
                    }}
                  >
                    {provider.capabilities.tier} •{' '}
                    {provider.capabilities.isFree
                      ? 'Free'
                      : `$${provider.capabilities.costPerImage}/image`}
                  </Text>
                )}
                {provider.name === 'Placeholder' && (
                  <Text
                    size={200}
                    style={{
                      marginTop: tokens.spacingVerticalXS,
                      color: tokens.colorNeutralForeground3,
                      fontStyle: 'italic',
                    }}
                  >
                    Generates solid color images - always available
                  </Text>
                )}
              </div>
            ))}
          </div>

          <div className={styles.settingsRow}>
            <Field label="Visual Style" className={styles.settingItem}>
              <Dropdown
                value={imageStyle}
                selectedOptions={[imageStyle]}
                onOptionSelect={(_, data) => setImageStyle(data.optionValue as string)}
              >
                {availableStyles.map((style) => (
                  <Option key={style} value={style}>
                    {style.charAt(0).toUpperCase() + style.slice(1)}
                  </Option>
                ))}
              </Dropdown>
            </Field>

            <Field label="Aspect Ratio" className={styles.settingItem}>
              <Dropdown
                value={aspectRatio}
                selectedOptions={[aspectRatio]}
                onOptionSelect={(_, data) => setAspectRatio(data.optionValue as string)}
              >
                <Option value="16:9">16:9 (Widescreen)</Option>
                <Option value="9:16">9:16 (Portrait)</Option>
                <Option value="1:1">1:1 (Square)</Option>
                <Option value="4:3">4:3 (Standard)</Option>
              </Dropdown>
            </Field>

            <Field label={`Quality: ${imageQuality}%`} className={styles.settingItem}>
              <Slider
                value={imageQuality}
                min={50}
                max={100}
                step={10}
                onChange={(_, data) => setImageQuality(data.value)}
              />
            </Field>
          </div>

          <div style={{ marginTop: tokens.spacingVerticalXL, paddingTop: tokens.spacingVerticalL, borderTop: `1px solid ${tokens.colorNeutralStroke2}` }}>
            <Title3 style={{ marginBottom: tokens.spacingVerticalM }}>Text-to-Speech Settings</Title3>
            <Text size={300} style={{ marginBottom: tokens.spacingVerticalM, color: tokens.colorNeutralForeground3 }}>
              Select a TTS provider and voice for audio previews
            </Text>
            
            <div className={styles.settingsRow}>
              <Field 
                label="TTS Provider" 
                className={styles.settingItem}
                validationState={selectedTtsProvider && ttsProviderStatus[selectedTtsProvider]?.isAvailable === false ? 'error' : undefined}
                validationMessage={selectedTtsProvider && ttsProviderStatus[selectedTtsProvider]?.isAvailable === false 
                  ? ttsProviderStatus[selectedTtsProvider].error || 'Provider is not available'
                  : undefined}
              >
                {isLoadingTtsProviders ? (
                  <Spinner size="small" />
                ) : (
                  <Dropdown
                    value={selectedTtsProvider}
                    selectedOptions={selectedTtsProvider ? [selectedTtsProvider] : []}
                    onOptionSelect={(_, data) => {
                      if (data.optionValue) {
                        setSelectedTtsProvider(data.optionValue as string);
                        setSelectedTtsVoice(''); // Reset voice when provider changes
                      }
                    }}
                    placeholder="Select TTS provider"
                  >
                    {ttsProviders.map((provider) => {
                      const status = ttsProviderStatus[provider.name];
                      const isAvailable = status?.isAvailable !== false;
                      return (
                        <Option 
                          key={provider.name} 
                          value={provider.name}
                          text={provider.name}
                          disabled={!isAvailable}
                        >
                          <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
                            {isAvailable ? (
                              <CheckmarkCircle24Regular style={{ fontSize: '14px', color: tokens.colorPaletteGreenForeground1 }} />
                            ) : (
                              <ErrorCircle24Regular style={{ fontSize: '14px', color: tokens.colorPaletteRedForeground1 }} />
                            )}
                            <span>{provider.name}</span>
                            <span style={{ marginLeft: 'auto', fontSize: '12px', color: tokens.colorNeutralForeground3 }}>
                              {provider.tier}
                            </span>
                          </div>
                        </Option>
                      );
                    })}
                  </Dropdown>
                )}
              </Field>

              <Field 
                label="Voice" 
                className={styles.settingItem}
                validationState={selectedTtsProvider && !selectedTtsVoice ? 'warning' : undefined}
                validationMessage={selectedTtsProvider && !selectedTtsVoice ? 'Please select a voice' : undefined}
              >
                {isLoadingTtsVoices ? (
                  <Spinner size="small" />
                ) : (
                  <Dropdown
                    value={selectedTtsVoice}
                    selectedOptions={selectedTtsVoice ? [selectedTtsVoice] : []}
                    onOptionSelect={(_, data) => {
                      if (data.optionValue) {
                        setSelectedTtsVoice(data.optionValue as string);
                      }
                    }}
                    placeholder={selectedTtsProvider ? "Select voice" : "Select provider first"}
                    disabled={!selectedTtsProvider || ttsVoices.length === 0}
                  >
                    {ttsVoices.map((voice) => (
                      <Option key={voice.name} value={voice.name} text={voice.name}>
                        {voice.name}
                        {voice.gender && ` (${voice.gender})`}
                        {voice.languageCode && ` - ${voice.languageCode}`}
                      </Option>
                    ))}
                  </Dropdown>
                )}
              </Field>
            </div>

            {selectedTtsProvider && ttsProviderStatus[selectedTtsProvider] && (
              <div style={{ 
                marginTop: tokens.spacingVerticalM, 
                padding: tokens.spacingVerticalM, 
                backgroundColor: ttsProviderStatus[selectedTtsProvider].isAvailable 
                  ? tokens.colorPaletteGreenBackground2 
                  : tokens.colorPaletteRedBackground2,
                borderRadius: tokens.borderRadiusMedium,
                display: 'flex',
                alignItems: 'center',
                gap: tokens.spacingHorizontalS
              }}>
                {ttsProviderStatus[selectedTtsProvider].isAvailable ? (
                  <>
                    <CheckmarkCircle24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />
                    <Text size={200} style={{ color: tokens.colorPaletteGreenForeground1 }}>
                      TTS provider "{selectedTtsProvider}" is available and ready
                    </Text>
                  </>
                ) : (
                  <>
                    <ErrorCircle24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />
                    <Text size={200} style={{ color: tokens.colorPaletteRedForeground1 }}>
                      TTS provider "{selectedTtsProvider}" is not available. {ttsProviderStatus[selectedTtsProvider].error || 'Please select a different provider.'}
                    </Text>
                  </>
                )}
              </div>
            )}
          </div>
        </>
      )}
    </Card>
  );

  const renderGenerationView = () => (
    <div className={styles.container}>
      {renderProviderSettings()}

      <div className={styles.generationCard}>
        <Title3>Generate Scene Previews</Title3>
        <Text>
          Create preview thumbnails and audio samples for each scene to review before final
          generation.
        </Text>

        <div className={styles.statsRow}>
          <div className={styles.statItem}>
            <Image24Regular style={{ fontSize: '32px', color: tokens.colorBrandForeground1 }} />
            <Text weight="semibold">{scriptData.scenes.length}</Text>
            <Text size={200}>Scenes</Text>
          </div>
          <div className={styles.statItem}>
            <Speaker224Regular style={{ fontSize: '32px', color: tokens.colorBrandForeground1 }} />
            <Text weight="semibold">{selectedTtsProvider || styleData.voiceProvider || 'Not selected'}</Text>
            <Text size={200}>TTS Provider</Text>
            {selectedTtsVoice && (
              <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                Voice: {selectedTtsVoice}
              </Text>
            )}
          </div>
          {selectedProvider && (
            <div className={styles.statItem}>
              <Image24Regular style={{ fontSize: '32px', color: tokens.colorBrandForeground1 }} />
              <Text weight="semibold">{selectedProvider}</Text>
              <Text size={200}>Image Provider</Text>
            </div>
          )}
        </div>

        <Button
          appearance="primary"
          size="large"
          onClick={generatePreviews}
          disabled={!selectedProvider && providers.length === 0}
        >
          Generate Previews
        </Button>

        {!selectedProvider && providers.length > 0 && (
          <Text size={200} style={{ color: tokens.colorPaletteRedForeground1 }}>
            Please select an image provider
          </Text>
        )}

        {selectedProvider === 'Placeholder' && (
          <Text size={200} style={{ color: tokens.colorNeutralForeground3, marginTop: tokens.spacingVerticalS }}>
            Using Placeholder provider - will generate solid color images for preview
          </Text>
        )}
      </div>
    </div>
  );

  const renderGeneratingView = () => (
    <div className={styles.generationCard}>
      <Spinner size="large" />
      <Title3>Generating Previews...</Title3>
      <Text>{currentStage}</Text>
      <div className={styles.progressSection}>
        <ProgressBar value={progress / 100} />
        <Text size={200} style={{ marginTop: tokens.spacingVerticalS }}>
          {Math.round(progress)}% complete
        </Text>
      </div>
    </div>
  );

  const renderCompletedView = () => (
    <div className={styles.container}>
      {renderProviderSettings()}

      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <Title3>Scene Previews</Title3>
          <Text>Review and regenerate individual scenes as needed</Text>
        </div>
        <Tooltip content="Regenerate all previews" relationship="label">
          <Button
            appearance="secondary"
            icon={<ArrowClockwise24Regular />}
            onClick={generatePreviews}
          >
            Regenerate All
          </Button>
        </Tooltip>
      </div>

      <div className={styles.previewGrid}>
        {scriptData.scenes.map((scene: ScriptScene, index: number) => {
          const thumbnail = data.thumbnails.find((t) => t.sceneId === scene.id);
          const audioSample = data.audioSamples.find((a) => a.sceneId === scene.id);
          const isRegenerating = regeneratingScene === scene.id;
          const isClickable = thumbnail && !isRegenerating;

          return (
            <Card key={scene.id} className={`${styles.sceneCard} ${styles.sceneCardHover}`}>
              <div
                role="button"
                tabIndex={isClickable ? 0 : -1}
                aria-label={`Scene ${index + 1} preview${isClickable ? ' - click to view fullscreen' : isRegenerating ? ' - regenerating' : ' - no preview available'}`}
                aria-disabled={!isClickable}
                className={styles.scenePreview}
                onClick={() =>
                  isClickable &&
                  setFullscreenImage({
                    url: thumbnail.imageUrl,
                    scene,
                    thumbnail,
                  })
                }
                onKeyDown={(e) => {
                  if ((e.key === 'Enter' || e.key === ' ') && isClickable) {
                    e.preventDefault();
                    setFullscreenImage({
                      url: thumbnail.imageUrl,
                      scene,
                      thumbnail,
                    });
                  }
                }}
                style={{ cursor: isClickable ? 'pointer' : 'default' }}
              >
                {isRegenerating ? (
                  <Spinner size="large" />
                ) : thumbnail ? (
                  <>
                    <img
                      src={thumbnail.imageUrl}
                      alt={thumbnail.caption}
                      className={styles.sceneImage}
                    />
                    <div
                      className={`image-overlay ${styles.imageOverlay}`}
                    >
                      <ImageEdit24Regular style={{ fontSize: '48px', color: 'white' }} />
                    </div>
                  </>
                ) : (
                  <Image24Regular style={{ fontSize: '48px' }} />
                )}
                <div className={styles.badgeGroup}>
                  <Badge appearance="filled">Scene {index + 1}</Badge>
                  {thumbnail?.isPlaceholder && (
                    <Badge appearance="filled" color="warning">
                      Placeholder
                    </Badge>
                  )}
                  {thumbnail?.provider && !thumbnail.isPlaceholder && (
                    <Badge appearance="filled" color="success">
                      {thumbnail.provider}
                    </Badge>
                  )}
                </div>
              </div>

              <div className={styles.sceneDetails}>
                <Text weight="semibold" size={300}>
                  {scene.text.substring(0, 60)}
                  {scene.text.length > 60 ? '...' : ''}
                </Text>
                <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                  Duration: {scene.duration}s
                </Text>

                {thumbnail?.quality && (
                  <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                    Quality: {thumbnail.quality}%
                  </Text>
                )}

                {thumbnail?.failureReason && (
                  <Text size={200} style={{ color: tokens.colorPaletteRedForeground1 }}>
                    {thumbnail.failureReason}
                  </Text>
                )}

                {audioSample && (() => {
                  const currentProvider = selectedTtsProvider || styleData.voiceProvider;
                  const providerStatus = currentProvider ? ttsProviderStatus[currentProvider] : null;
                  const isTtsAvailable = !currentProvider || (providerStatus?.isAvailable !== false);
                  
                  return (
                    <div className={styles.audioPreview}>
                      {isTtsAvailable ? (
                        <>
                          <CheckmarkCircle24Regular 
                            style={{ 
                              marginRight: tokens.spacingHorizontalS, 
                              color: tokens.colorPaletteGreenForeground1 
                            }} 
                          />
                          <Text size={200}>Audio ready</Text>
                          {currentProvider && (
                            <Text size={200} style={{ marginLeft: tokens.spacingHorizontalS, color: tokens.colorNeutralForeground3 }}>
                              ({currentProvider})
                            </Text>
                          )}
                        </>
                      ) : (
                        <>
                          <ErrorCircle24Regular 
                            style={{ 
                              marginRight: tokens.spacingHorizontalS, 
                              color: tokens.colorPaletteRedForeground1 
                            }} 
                          />
                          <Text size={200} style={{ color: tokens.colorPaletteRedForeground1 }}>
                            TTS not available
                          </Text>
                        </>
                      )}
                    </div>
                  );
                })()}

                <div className={styles.sceneActions}>
                  <Button
                    appearance="secondary"
                    icon={<Play24Regular />}
                    onClick={() => void playScenePreview(scene.id)}
                    disabled={
                      !thumbnail || 
                      !audioSample || 
                      playingSceneId === scene.id ||
                      !selectedTtsProvider ||
                      (ttsProviderStatus[selectedTtsProvider]?.isAvailable === false)
                    }
                  >
                    {playingSceneId === scene.id ? 'Playing...' : 'Preview'}
                  </Button>
                  {audioError && playingSceneId === scene.id && (
                    <Tooltip content={audioError} relationship="label">
                      <div style={{ 
                        display: 'flex', 
                        alignItems: 'center', 
                        gap: tokens.spacingHorizontalXS,
                        color: tokens.colorPaletteRedForeground1 
                      }}>
                        <ErrorCircle24Regular style={{ fontSize: '16px' }} />
                        <Text size={200} style={{ color: tokens.colorPaletteRedForeground1 }}>
                          {audioError.length > 50 ? `${audioError.substring(0, 50)}...` : audioError}
                        </Text>
                      </div>
                    </Tooltip>
                  )}

                  <Menu>
                    <MenuTrigger>
                      <Button
                        appearance="subtle"
                        icon={<MoreHorizontal24Regular />}
                        disabled={isRegenerating}
                      />
                    </MenuTrigger>
                    <MenuPopover>
                      <MenuList>
                        <MenuItem
                          icon={<ArrowClockwise24Regular />}
                          onClick={() => regenerateScene(scene.id)}
                        >
                          Regenerate
                        </MenuItem>
                        <MenuItem
                          icon={<ArrowUpload24Regular />}
                          onClick={() => handleManualUpload(scene.id)}
                        >
                          Upload Image
                        </MenuItem>
                        <MenuItem
                          icon={<Search24Regular />}
                          onClick={() => handleSearchFallback(scene.id)}
                        >
                          Search Stock Images
                        </MenuItem>
                      </MenuList>
                    </MenuPopover>
                  </Menu>
                </div>
              </div>
            </Card>
          );
        })}
      </div>

      {advancedMode && (
        <Card style={{ padding: tokens.spacingVerticalL, marginTop: tokens.spacingVerticalL }}>
          <Title3 style={{ marginBottom: tokens.spacingVerticalM }}>
            Advanced Preview Options
          </Title3>
          <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalM }}>
            <Text size={300}>
              Previews use lower quality settings for faster generation. Final video will use full
              quality settings.
            </Text>
            <Field label="Preview Quality" hint="Lower quality = faster preview generation">
              <Dropdown value="standard" disabled>
                <Option value="standard">Standard (Fast)</Option>
                <Option value="high">High (Slower)</Option>
              </Dropdown>
            </Field>
            <Field label="Preview Count" hint="Number of scenes to preview">
              <Dropdown value="all" disabled>
                <Option value="all">All Scenes</Option>
                <Option value="first3">First 3 Scenes</Option>
                <Option value="key">Key Scenes Only</Option>
              </Dropdown>
            </Field>
          </div>
        </Card>
      )}

      {fullscreenImage && (
        <Dialog
          open={!!fullscreenImage}
          onOpenChange={(_, data) => !data.open && setFullscreenImage(null)}
        >
          <DialogSurface className={styles.fullscreenDialog}>
            <DialogTitle>
              Scene {scriptData.scenes.findIndex((s) => s.id === fullscreenImage.scene.id) + 1}{' '}
              Preview
            </DialogTitle>
            <DialogBody>
              <DialogContent>
                <img
                  src={fullscreenImage.url}
                  alt="Fullscreen preview"
                  className={styles.fullscreenImage}
                />
                <div className={styles.imageDetails}>
                  <Text weight="semibold">Scene Text:</Text>
                  <Text>{fullscreenImage.scene.text}</Text>

                  {fullscreenImage.thumbnail.provider && (
                    <>
                      <Text weight="semibold" style={{ marginTop: tokens.spacingVerticalM }}>
                        Provider:
                      </Text>
                      <Text>{fullscreenImage.thumbnail.provider}</Text>
                    </>
                  )}

                  {fullscreenImage.thumbnail.quality && (
                    <>
                      <Text weight="semibold" style={{ marginTop: tokens.spacingVerticalM }}>
                        Quality Score:
                      </Text>
                      <Text>{fullscreenImage.thumbnail.quality}%</Text>
                    </>
                  )}

                  {fullscreenImage.scene.visualDescription && (
                    <>
                      <Text weight="semibold" style={{ marginTop: tokens.spacingVerticalM }}>
                        Visual Description:
                      </Text>
                      <Text>{fullscreenImage.scene.visualDescription}</Text>
                    </>
                  )}
                </div>
              </DialogContent>
              <DialogActions>
                <Button appearance="secondary" onClick={() => setFullscreenImage(null)}>
                  Close
                </Button>
                <Button
                  appearance="primary"
                  onClick={() => {
                    regenerateScene(fullscreenImage.scene.id);
                    setFullscreenImage(null);
                  }}
                >
                  Regenerate
                </Button>
              </DialogActions>
            </DialogBody>
          </DialogSurface>
        </Dialog>
      )}
    </div>
  );

  const renderErrorView = () => (
    <div className={styles.generationCard}>
      <Warning24Regular style={{ fontSize: '48px', color: tokens.colorPaletteRedForeground1 }} />
      <Title3>Preview Generation Failed</Title3>
      <Text>{currentStage}</Text>
      <Button appearance="primary" onClick={generatePreviews}>
        Try Again
      </Button>
    </div>
  );

  // Render missing script data error
  const renderMissingScriptDataError = () => (
    <div className={styles.generationCard}>
      <Warning24Regular style={{ fontSize: '48px', color: tokens.colorPaletteRedForeground1 }} />
      <Title3>Script Data Missing</Title3>
      <Text style={{ marginBottom: tokens.spacingVerticalM }}>
        No script data is available. Please go back to the Script Review step and generate a script
        first.
      </Text>
      <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
        The Preview Generation step requires completed script data with scenes to generate visual
        previews.
      </Text>
    </div>
  );

  // Render provider loading error with retry
  const renderProviderLoadError = () => (
    <div className={styles.generationCard}>
      <Warning24Regular style={{ fontSize: '48px', color: tokens.colorPaletteRedForeground1 }} />
      <Title3>Failed to Load Providers</Title3>
      <Text style={{ marginBottom: tokens.spacingVerticalM }}>
        {providerLoadError || 'An error occurred while loading image providers.'}
      </Text>
      <Button
        appearance="primary"
        icon={<ArrowClockwise24Regular />}
        onClick={() => void loadProviders()}
      >
        Retry
      </Button>
    </div>
  );

  // Main render - handle all states
  // First check for invalid script data
  if (!hasValidScriptData) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <Title2>Preview Generation</Title2>
          <Text>
            Generate preview thumbnails and audio samples to review your video before final
            rendering.
          </Text>
        </div>
        {renderMissingScriptDataError()}
      </div>
    );
  }

  // Check for provider loading error
  if (providerLoadError && !isLoadingProviders && providers.length === 0) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <Title2>Preview Generation</Title2>
          <Text>
            Generate preview thumbnails and audio samples to review your video before final
            rendering.
          </Text>
        </div>
        {renderProviderLoadError()}
      </div>
    );
  }

  // Show loading while providers are loading
  if (isLoadingProviders && providers.length === 0) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <Title2>Preview Generation</Title2>
          <Text>
            Generate preview thumbnails and audio samples to review your video before final
            rendering.
          </Text>
        </div>
        <div className={styles.generationCard}>
          <Spinner size="large" />
          <Title3>Loading Providers...</Title3>
          <Text>Please wait while we load available image generation providers.</Text>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title2>Preview Generation</Title2>
        <Text>
          Generate preview thumbnails and audio samples to review your video before final rendering.
        </Text>
      </div>

      {status === 'idle' && renderGenerationView()}
      {status === 'generating' && renderGeneratingView()}
      {status === 'completed' && renderCompletedView()}
      {status === 'error' && renderErrorView()}
    </div>
  );
};
