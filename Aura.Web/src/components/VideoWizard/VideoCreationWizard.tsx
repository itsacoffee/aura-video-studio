import {
  makeStyles,
  tokens,
  Title1,
  Text,
  Button,
  Card,
  Switch,
  Tooltip,
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogContent,
  DialogActions,
  Dropdown,
  Option,
  Badge,
} from '@fluentui/react-components';
import {
  ArrowLeft24Regular,
  ArrowRight24Regular,
  Save24Regular,
  Settings24Regular,
  DismissCircle24Regular,
  Lightbulb24Regular,
  DocumentMultiple24Regular,
  Clock24Regular,
  History24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect, useCallback, useRef, useMemo } from 'react';
import type { FC } from 'react';
import { useNavigate } from 'react-router-dom';
import { useWizardPersistence } from '../../hooks/useWizardPersistence';
import { listProviders } from '../../services/api/scriptApi';
import { WizardProgress } from '../WizardProgress';
import { AdvancedModePanel } from './AdvancedModePanel';
import { CelebrationEffect } from './CelebrationEffect';
import { CostEstimator } from './CostEstimator';
import { DraftManager } from './DraftManager';
import { BriefInput } from './steps/BriefInput';
import { FinalExport } from './steps/FinalExport';
import { PreviewGeneration } from './steps/PreviewGeneration';
import { ScriptReview } from './steps/ScriptReview';
import { StyleSelection } from './steps/StyleSelection';
import type {
  WizardData,
  StepValidation,
  VideoTemplate,
  WizardDraft,
  BriefData,
  StyleData,
  ScriptData,
  PreviewData,
  ExportData,
} from './types';
import { VideoTemplates } from './VideoTemplates';
import { WizardErrorBoundary } from './WizardErrorBoundary';

const useStyles = makeStyles({
  container: {
    maxWidth: '1280px',
    margin: '0 auto',
    padding: `${tokens.spacingVerticalXXL} ${tokens.spacingHorizontalXXL}`,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXL,
    minHeight: '100vh',
    animation: 'fadeIn 0.6s cubic-bezier(0.4, 0, 0.2, 1)',
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: tokens.spacingVerticalXL,
    paddingBottom: tokens.spacingVerticalL,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    animation: 'fadeInDown 0.5s cubic-bezier(0.4, 0, 0.2, 1)',
  },
  headerLeft: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    flex: 1,
  },
  headerRight: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
  },
  progressSection: {
    marginBottom: tokens.spacingVerticalXXL,
    animation: 'fadeIn 0.6s cubic-bezier(0.4, 0, 0.2, 1) 0.1s both',
  },
  contentCard: {
    padding: tokens.spacingVerticalXXXL,
    flex: 1,
    animation: 'fadeInUp 0.6s cubic-bezier(0.4, 0, 0.2, 1) 0.2s both',
    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    borderRadius: tokens.borderRadiusLarge,
    backgroundColor: tokens.colorNeutralBackground1,
    boxShadow: '0 2px 8px rgba(0, 0, 0, 0.04), 0 1px 3px rgba(0, 0, 0, 0.06)',
    border: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  navigationBar: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingTop: tokens.spacingVerticalXL,
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
    animation: 'fadeIn 0.6s cubic-bezier(0.4, 0, 0.2, 1) 0.3s both',
  },
  navigationButtons: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
  },
  advancedToggle: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalM}`,
    borderRadius: tokens.borderRadiusMedium,
    backgroundColor: tokens.colorNeutralBackground2,
    transition: 'all 0.2s cubic-bezier(0.4, 0, 0.2, 1)',
  },
  keyboardHint: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    letterSpacing: '0.01em',
  },
  timeEstimate: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    color: tokens.colorNeutralForeground2,
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightMedium,
  },
  autoSaveIndicator: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalM}`,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    transition: 'all 0.2s cubic-bezier(0.4, 0, 0.2, 1)',
  },
  resumeDialogContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  resumeInfo: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
  '@keyframes fadeIn': {
    '0%': { opacity: 0 },
    '100%': { opacity: 1 },
  },
  '@keyframes fadeInDown': {
    '0%': {
      opacity: 0,
      transform: 'translateY(-20px)',
    },
    '100%': {
      opacity: 1,
      transform: 'translateY(0)',
    },
  },
  '@keyframes fadeInUp': {
    '0%': {
      opacity: 0,
      transform: 'translateY(30px)',
    },
    '100%': {
      opacity: 1,
      transform: 'translateY(0)',
    },
  },
});

const STEP_LABELS = ['Brief', 'Style', 'Script', 'Preview', 'Export'];
const STEP_TIME_ESTIMATES = ['2 min', '3 min', '5 min', '3 min', '2 min'];
const STORAGE_KEY = 'aura-wizard-data';
const ADVANCED_MODE_KEY = 'aura-wizard-advanced-mode';
const AUTO_SAVE_INTERVAL = 30000;

export const VideoCreationWizard: FC = () => {
  const styles = useStyles();
  const navigate = useNavigate();
  const [currentStep, setCurrentStep] = useState(0);
  const [advancedMode, setAdvancedMode] = useState(() => {
    return localStorage.getItem(ADVANCED_MODE_KEY) === 'true';
  });
  const [showSaveDialog, setShowSaveDialog] = useState(false);
  const [showDraftManager, setShowDraftManager] = useState(false);
  const [showResumeDialog, setShowResumeDialog] = useState(false);
  const [lastSaved, setLastSaved] = useState<Date | null>(null);
  const autoSaveTimerRef = useRef<number | null>(null);
  const [showTemplates, setShowTemplates] = useState(false);
  const [showCelebration, setShowCelebration] = useState(false);
  const [selectedLlmProvider, setSelectedLlmProvider] = useState<string | undefined>(undefined);
  const [availableLlmProviders, setAvailableLlmProviders] = useState<
    Array<{ name: string; isAvailable: boolean; tier: string }>
  >([]);

  // Default wizard data - memoized to avoid recreating on each render
  const defaultWizardData = useMemo<WizardData>(
    () => ({
      brief: {
        topic: '',
        videoType: 'educational',
        targetAudience: '',
        keyMessage: '',
        duration: 60,
      },
      style: {
        // Use 'Null' as default voice provider - it's always available (generates silence)
        // The StyleSelection and PreviewGeneration components will auto-select a better provider if available
        voiceProvider: 'Null',
        voiceName: 'default',
        visualStyle: 'modern',
        musicGenre: 'ambient',
        musicEnabled: true,
        imageProvider: 'Placeholder',
      },
      script: {
        content: '',
        scenes: [],
        generatedAt: null,
      },
      preview: {
        thumbnails: [],
        audioSamples: [],
      },
      export: {
        quality: 'high',
        format: 'mp4',
        resolution: '1080p',
        includeCaptions: true,
      },
      advanced: {
        targetPlatform: 'youtube',
        customTransitions: false,
        llmParameters: {},
        ragConfiguration: {
          enabled: false,
          topK: 5,
          minimumScore: 0.6,
          maxContextTokens: 2000,
          includeCitations: true,
          tightenClaims: false,
        },
        customInstructions: '',
      },
    }),
    []
  );

  const [wizardData, setWizardData] = useState<WizardData>(() => {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved) {
      try {
        return JSON.parse(saved);
      } catch (error) {
        console.error('Failed to parse saved wizard data:', error);
      }
    }
    return defaultWizardData;
  });

  // Wizard persistence hook for state save/resume
  const {
    state: persistenceState,
    saveState: persistState,
    restoreSession,
    clearSession,
  } = useWizardPersistence({
    enableAutoSave: true,
    onSaveError: (error) => {
      console.error('[VideoCreationWizard] Auto-save failed:', error);
    },
  });

  // Check for resumable session on mount - capture values to avoid stale closures
  const hasResumableSession = persistenceState.hasResumableSession;
  const savedStep = persistenceState.savedStep;
  useEffect(() => {
    if (hasResumableSession && savedStep > 0) {
      // Only show resume dialog if there's meaningful progress
      setShowResumeDialog(true);
    }
  }, [hasResumableSession, savedStep]);

  // Handle resume session
  const handleResumeSession = useCallback(() => {
    const restored = restoreSession();
    if (restored) {
      setWizardData(restored.data);
      setCurrentStep(restored.step);
      console.info('[VideoCreationWizard] Session restored to step', restored.step);
    }
    setShowResumeDialog(false);
  }, [restoreSession]);

  // Handle start fresh (clear saved session)
  const handleStartFresh = useCallback(() => {
    clearSession();
    setWizardData(defaultWizardData);
    setCurrentStep(0);
    setShowResumeDialog(false);
  }, [clearSession, defaultWizardData]);

  const [stepValidation, setStepValidation] = useState<StepValidation[]>([
    { isValid: false, errors: [] },
    { isValid: false, errors: [] },
    { isValid: false, errors: [] },
    { isValid: false, errors: [] },
    { isValid: false, errors: [] },
  ]);

  // Save wizard data to localStorage and persistence hook whenever it changes
  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(wizardData));
    persistState(wizardData, currentStep);
    setLastSaved(new Date());
  }, [wizardData, currentStep, persistState]);

  // Auto-save every 30 seconds
  useEffect(() => {
    autoSaveTimerRef.current = window.setInterval(() => {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(wizardData));
      persistState(wizardData, currentStep);
      setLastSaved(new Date());
    }, AUTO_SAVE_INTERVAL);

    return () => {
      if (autoSaveTimerRef.current) {
        window.clearInterval(autoSaveTimerRef.current);
      }
    };
  }, [wizardData, currentStep, persistState]);

  // Save advanced mode preference
  useEffect(() => {
    localStorage.setItem(ADVANCED_MODE_KEY, String(advancedMode));
  }, [advancedMode]);

  // Keyboard navigation
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.ctrlKey && e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        if (currentStep < STEP_LABELS.length - 1 && stepValidation[currentStep].isValid) {
          setCurrentStep((prev) => prev + 1);
          window.scrollTo({ top: 0, behavior: 'smooth' });
        }
      } else if (e.ctrlKey && e.shiftKey && e.key === 'Enter') {
        e.preventDefault();
        if (currentStep > 0) {
          setCurrentStep((prev) => prev - 1);
          window.scrollTo({ top: 0, behavior: 'smooth' });
        }
      } else if (e.key === 'Escape') {
        e.preventDefault();
        setShowSaveDialog(true);
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [currentStep, stepValidation]);

  const updateWizardData = useCallback((updates: Partial<WizardData>) => {
    setWizardData((prev) => ({ ...prev, ...updates }));
  }, []);

  const updateStepValidation = useCallback((step: number, validation: StepValidation) => {
    setStepValidation((prev) => {
      const updated = [...prev];
      updated[step] = validation;
      return updated;
    });
  }, []);

  const handleNext = useCallback(() => {
    if (currentStep < STEP_LABELS.length - 1 && stepValidation[currentStep].isValid) {
      // Extra validation before navigating to Preview step (step 3)
      // Ensure script data with scenes exists
      if (currentStep === 2) {
        const hasScriptScenes = wizardData.script.scenes && wizardData.script.scenes.length > 0;
        if (!hasScriptScenes) {
          console.error(
            '[VideoCreationWizard] Cannot proceed to Preview: no script scenes available'
          );
          return;
        }
      }

      setCurrentStep((prev) => prev + 1);
      window.scrollTo({ top: 0, behavior: 'smooth' });

      // Show celebration when completing final step
      if (currentStep === STEP_LABELS.length - 2) {
        setShowCelebration(true);
      }
    }
  }, [currentStep, stepValidation, wizardData.script.scenes]);

  const handlePrevious = useCallback(() => {
    if (currentStep > 0) {
      setCurrentStep((prev) => prev - 1);
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }, [currentStep]);

  const handleStepClick = useCallback((step: number) => {
    setCurrentStep(step);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }, []);

  const handleSaveAndExit = useCallback(() => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(wizardData));
    navigate('/');
  }, [wizardData, navigate]);

  const handleClearAndExit = useCallback(() => {
    localStorage.removeItem(STORAGE_KEY);
    setShowSaveDialog(false);
    navigate('/');
  }, [navigate]);

  const handleSelectTemplate = useCallback((template: VideoTemplate) => {
    setWizardData((prev) => ({
      ...prev,
      ...template.defaultData,
      brief: {
        ...prev.brief,
        ...template.defaultData.brief,
      },
      style: {
        ...prev.style,
        ...template.defaultData.style,
      },
      advanced: {
        ...prev.advanced,
        ...template.defaultData.advanced,
      },
    }));
    setShowTemplates(false);
  }, []);

  const handleLoadDraft = useCallback((draft: WizardDraft) => {
    setWizardData(draft.data);
    setCurrentStep(draft.currentStep);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }, []);

  const getTimeSinceLastSave = useCallback(() => {
    if (!lastSaved) return '';
    const seconds = Math.floor((Date.now() - lastSaved.getTime()) / 1000);
    if (seconds < 60) return 'Just now';
    const minutes = Math.floor(seconds / 60);
    if (minutes === 1) return '1 minute ago';
    return `${minutes} minutes ago`;
  }, [lastSaved]);

  // Load available LLM providers on mount
  useEffect(() => {
    const loadProviders = async () => {
      try {
        const response = await listProviders();
        // Normalize provider names to handle "Ollama (model)" format
        const normalizeProviderName = (name: string) => {
          const parenIndex = name.indexOf('(');
          return parenIndex > 0 ? name.substring(0, parenIndex).trim() : name.trim();
        };

        const llmProviders = response.providers.filter((p) => {
          const normalized = normalizeProviderName(p.name);
          return (
            normalized === 'RuleBased' ||
            normalized === 'Ollama' ||
            normalized === 'OpenAI' ||
            normalized === 'Gemini' ||
            normalized === 'Anthropic'
          );
        });
        setAvailableLlmProviders(
          llmProviders.map((p) => ({
            name: p.name,
            isAvailable: p.isAvailable,
            tier: p.tier,
          }))
        );

        // Prefer Ollama if available (check normalized name), otherwise use first available
        const ollamaProvider = llmProviders.find((p) => {
          const normalized = normalizeProviderName(p.name);
          return p.isAvailable && normalized === 'Ollama';
        });
        if (ollamaProvider) {
          console.info('[VideoCreationWizard] Ollama is available, selecting it as default');
          setSelectedLlmProvider(ollamaProvider.name);
        } else {
          const firstAvailable = llmProviders.find((p) => p.isAvailable);
          if (firstAvailable) {
            console.info(
              '[VideoCreationWizard] Selecting first available provider:',
              firstAvailable.name
            );
            setSelectedLlmProvider(firstAvailable.name);
          }
        }
      } catch (error) {
        console.error('[VideoCreationWizard] Failed to load providers:', error);
      }
    };

    void loadProviders();
  }, []);

  const renderStepContent = () => {
    // Default data for graceful degradation when a step fails
    const getDefaultBrief = (): BriefData => ({
      topic: 'Video topic',
      videoType: 'educational',
      targetAudience: 'General audience',
      keyMessage: 'Key message',
      duration: 60,
    });

    const getDefaultStyle = (): StyleData => ({
      // Use 'Null' as default voice provider - it's always available (generates silence)
      voiceProvider: 'Null',
      voiceName: 'default',
      visualStyle: 'modern',
      musicGenre: 'ambient',
      musicEnabled: true,
      imageProvider: 'Placeholder',
    });

    const getDefaultScript = (): ScriptData => ({
      content: '',
      scenes: [],
      generatedAt: null,
    });

    const getDefaultPreview = (): PreviewData => ({
      thumbnails: [],
      audioSamples: [],
    });

    const getDefaultExport = (): ExportData => ({
      quality: 'high',
      format: 'mp4',
      resolution: '1080p',
      includeCaptions: true,
    });

    switch (currentStep) {
      case 0:
        return (
          <WizardErrorBoundary
            stepName="Brief Input"
            onError={(error, errorInfo) => {
              console.error('[VideoCreationWizard] Brief Input error:', {
                error: error.message,
                componentStack: errorInfo.componentStack,
              });
            }}
            onRetry={() => updateStepValidation(0, { isValid: false, errors: [] })}
            onSkipWithDefaults={() => {
              updateWizardData({ brief: getDefaultBrief() });
              updateStepValidation(0, { isValid: true, errors: [] });
            }}
            enableGracefulDegradation={true}
          >
            <BriefInput
              data={wizardData.brief}
              advancedMode={advancedMode}
              advancedData={wizardData.advanced}
              onChange={(brief) => updateWizardData({ brief })}
              onAdvancedChange={(advanced) => updateWizardData({ advanced })}
              onValidationChange={(validation) => updateStepValidation(0, validation)}
            />
          </WizardErrorBoundary>
        );
      case 1:
        return (
          <WizardErrorBoundary
            stepName="Style Selection"
            onError={(error, errorInfo) => {
              console.error('[VideoCreationWizard] Style Selection error:', {
                error: error.message,
                componentStack: errorInfo.componentStack,
              });
            }}
            onRetry={() => updateStepValidation(1, { isValid: false, errors: [] })}
            onSkipWithDefaults={() => {
              updateWizardData({ style: getDefaultStyle() });
              updateStepValidation(1, { isValid: true, errors: [] });
            }}
            enableGracefulDegradation={true}
          >
            <StyleSelection
              data={wizardData.style}
              briefData={wizardData.brief}
              advancedMode={advancedMode}
              onChange={(style) => updateWizardData({ style })}
              onValidationChange={(validation) => updateStepValidation(1, validation)}
            />
          </WizardErrorBoundary>
        );
      case 2:
        return (
          <WizardErrorBoundary
            stepName="Script Review"
            onError={(error, errorInfo) => {
              console.error('[VideoCreationWizard] Script Review error:', {
                error: error.message,
                componentStack: errorInfo.componentStack,
              });
            }}
            onRetry={() => updateStepValidation(2, { isValid: false, errors: [] })}
            onSkipWithDefaults={() => {
              updateWizardData({ script: getDefaultScript() });
              updateStepValidation(2, { isValid: false, errors: ['Script generation required'] });
            }}
            enableGracefulDegradation={false}
          >
            <ScriptReview
              data={wizardData.script}
              briefData={wizardData.brief}
              styleData={wizardData.style}
              advancedMode={advancedMode}
              selectedProvider={selectedLlmProvider}
              onProviderChange={setSelectedLlmProvider}
              advancedSettings={{
                llmParameters: wizardData.advanced.llmParameters,
                ragConfiguration: wizardData.advanced.ragConfiguration,
                customInstructions: wizardData.advanced.customInstructions,
                targetPlatform: wizardData.advanced.targetPlatform,
              }}
              onChange={(script) => updateWizardData({ script })}
              onValidationChange={(validation) => updateStepValidation(2, validation)}
            />
          </WizardErrorBoundary>
        );
      case 3:
        return (
          <WizardErrorBoundary
            stepName="Preview Generation"
            onError={(error, errorInfo) => {
              console.error('[VideoCreationWizard] Preview Generation error:', {
                error: error.message,
                componentStack: errorInfo.componentStack,
                hasScriptData: wizardData.script.scenes.length > 0,
              });
            }}
            onRetry={() =>
              updateStepValidation(3, { isValid: false, errors: ['Preview generation required'] })
            }
            onSkipWithDefaults={() => {
              updateWizardData({ preview: getDefaultPreview() });
              updateStepValidation(3, { isValid: true, errors: [] });
            }}
            enableGracefulDegradation={true}
          >
            <PreviewGeneration
              data={wizardData.preview}
              scriptData={wizardData.script}
              styleData={wizardData.style}
              advancedMode={advancedMode}
              onChange={(preview) => updateWizardData({ preview })}
              onValidationChange={(validation) => updateStepValidation(3, validation)}
            />
          </WizardErrorBoundary>
        );
      case 4:
        return (
          <WizardErrorBoundary
            stepName="Final Export"
            onError={(error, errorInfo) => {
              console.error('[VideoCreationWizard] Final Export error:', {
                error: error.message,
                componentStack: errorInfo.componentStack,
              });
            }}
            onRetry={() => updateStepValidation(4, { isValid: false, errors: [] })}
            onSkipWithDefaults={() => {
              updateWizardData({ export: getDefaultExport() });
              updateStepValidation(4, { isValid: true, errors: [] });
            }}
            enableGracefulDegradation={true}
          >
            <FinalExport
              data={wizardData.export}
              wizardData={wizardData}
              advancedMode={advancedMode}
              onChange={(exportData) => updateWizardData({ export: exportData })}
              onValidationChange={(validation) => updateStepValidation(4, validation)}
            />
          </WizardErrorBoundary>
        );
      default:
        return null;
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.headerLeft}>
          <Title1>Create Video</Title1>
          <Text className={styles.keyboardHint}>
            Use Tab to navigate, Ctrl+Enter to continue, Escape to save and exit
          </Text>
          {lastSaved && (
            <div className={styles.autoSaveIndicator}>
              <Clock24Regular style={{ fontSize: '14px', color: tokens.colorBrandForeground1 }} />
              <Text size={200}>Auto-saved {getTimeSinceLastSave()}</Text>
            </div>
          )}
        </div>
        <div className={styles.headerRight}>
          {/* LLM Provider Selector - Always visible */}
          {availableLlmProviders.length > 0 && (
            <Tooltip content="Select which LLM to use for script generation" relationship="label">
              <div
                style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}
              >
                <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                  LLM:
                </Text>
                <Dropdown
                  value={selectedLlmProvider || 'Auto'}
                  onOptionSelect={(_, data) => {
                    if (data.optionValue) {
                      setSelectedLlmProvider(data.optionValue);
                      console.info(
                        '[VideoCreationWizard] LLM provider changed to:',
                        data.optionValue
                      );
                    }
                  }}
                  style={{ minWidth: '150px' }}
                >
                  <Option value="Auto" text="Auto (Best Available)">
                    Auto (Best Available)
                  </Option>
                  {availableLlmProviders.map((provider) => (
                    <Option
                      key={provider.name}
                      value={provider.name}
                      disabled={!provider.isAvailable}
                    >
                      {provider.name}
                      {!provider.isAvailable ? ' (Unavailable)' : ''}
                    </Option>
                  ))}
                </Dropdown>
                {selectedLlmProvider &&
                  selectedLlmProvider !== 'Auto' &&
                  (() => {
                    // Normalize provider name for comparison (handle "Ollama (model)" format)
                    const normalizeProviderName = (name: string) => {
                      const parenIndex = name.indexOf('(');
                      return parenIndex > 0 ? name.substring(0, parenIndex).trim() : name.trim();
                    };
                    const selectedNormalized = normalizeProviderName(selectedLlmProvider);
                    const provider = availableLlmProviders.find(
                      (p) => normalizeProviderName(p.name) === selectedNormalized
                    );
                    const isAvailable = provider?.isAvailable ?? false;
                    return (
                      <Badge color={isAvailable ? 'success' : 'subtle'} size="small">
                        {isAvailable ? 'Active' : 'Unavailable'}
                      </Badge>
                    );
                  })()}
              </div>
            </Tooltip>
          )}
          <Tooltip content="Browse video templates" relationship="label">
            <Button
              appearance="secondary"
              icon={<Lightbulb24Regular />}
              onClick={() => setShowTemplates(true)}
            >
              Templates
            </Button>
          </Tooltip>
          <Tooltip content="Manage drafts" relationship="label">
            <Button
              appearance="secondary"
              icon={<DocumentMultiple24Regular />}
              onClick={() => setShowDraftManager(true)}
            >
              Drafts
            </Button>
          </Tooltip>
          <div className={styles.advancedToggle}>
            <Settings24Regular />
            <Switch
              checked={advancedMode}
              onChange={(_, data) => setAdvancedMode(data.checked)}
              label="Advanced Mode"
            />
          </div>
          <CostEstimator wizardData={wizardData} selectedLlmProvider={selectedLlmProvider} />
          <Tooltip content="Save progress and exit" relationship="label">
            <Button
              appearance="subtle"
              icon={<Save24Regular />}
              onClick={() => setShowSaveDialog(true)}
            >
              Save & Exit
            </Button>
          </Tooltip>
        </div>
      </div>

      {/* Advanced Mode Panel - appears when Advanced Mode is enabled */}
      {advancedMode && (
        <AdvancedModePanel
          selectedProvider={selectedLlmProvider}
          llmParameters={wizardData.advanced.llmParameters ?? {}}
          ragConfiguration={
            wizardData.advanced.ragConfiguration ?? {
              enabled: false,
              topK: 5,
              minimumScore: 0.6,
              maxContextTokens: 2000,
              includeCitations: true,
              tightenClaims: false,
            }
          }
          customInstructions={wizardData.advanced.customInstructions ?? ''}
          onLlmParametersChange={(params) =>
            updateWizardData({
              advanced: {
                ...wizardData.advanced,
                llmParameters: params,
              },
            })
          }
          onRagConfigurationChange={(config) =>
            updateWizardData({
              advanced: {
                ...wizardData.advanced,
                ragConfiguration: config,
              },
            })
          }
          onCustomInstructionsChange={(instructions) =>
            updateWizardData({
              advanced: {
                ...wizardData.advanced,
                customInstructions: instructions,
              },
            })
          }
        />
      )}

      <div className={styles.progressSection}>
        <WizardProgress
          currentStep={currentStep}
          totalSteps={STEP_LABELS.length}
          stepLabels={STEP_LABELS}
          onStepClick={handleStepClick}
          onSaveAndExit={() => setShowSaveDialog(true)}
        />
        <div
          className={styles.timeEstimate}
          style={{ marginTop: tokens.spacingVerticalM, textAlign: 'center' }}
        >
          <Text size={200}>Estimated time for this step: {STEP_TIME_ESTIMATES[currentStep]}</Text>
        </div>
      </div>

      <Card className={styles.contentCard}>{renderStepContent()}</Card>

      <div className={styles.navigationBar}>
        <div className={styles.navigationButtons}>
          {currentStep > 0 && (
            <Button appearance="secondary" icon={<ArrowLeft24Regular />} onClick={handlePrevious}>
              Previous
            </Button>
          )}
        </div>
        <div className={styles.navigationButtons}>
          {currentStep < STEP_LABELS.length - 1 && (
            <Tooltip
              content={
                !stepValidation[currentStep].isValid
                  ? `Please complete all required fields: ${stepValidation[currentStep].errors.join(', ')}`
                  : 'Continue to next step (Ctrl+Enter)'
              }
              relationship="label"
            >
              <Button
                appearance="primary"
                icon={<ArrowRight24Regular />}
                iconPosition="after"
                onClick={handleNext}
                disabled={!stepValidation[currentStep].isValid}
              >
                Next
              </Button>
            </Tooltip>
          )}
          {/* Final step (Export) has its own "Start Export" button in the FinalExport component */}
        </div>
      </div>

      <Dialog open={showSaveDialog} onOpenChange={(_, data) => setShowSaveDialog(data.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>Save Progress?</DialogTitle>
            <DialogContent>
              <Text>Do you want to save your progress? You can resume this wizard later.</Text>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setShowSaveDialog(false)}>
                Cancel
              </Button>
              <Button
                appearance="secondary"
                icon={<DismissCircle24Regular />}
                onClick={handleClearAndExit}
              >
                Discard Progress
              </Button>
              <Button appearance="primary" icon={<Save24Regular />} onClick={handleSaveAndExit}>
                Save & Exit
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>

      <Dialog open={showTemplates} onOpenChange={(_, data) => setShowTemplates(data.open)}>
        <DialogSurface style={{ maxWidth: '1200px', maxHeight: '90vh' }}>
          <DialogBody>
            <VideoTemplates
              onSelectTemplate={handleSelectTemplate}
              onClose={() => setShowTemplates(false)}
            />
          </DialogBody>
        </DialogSurface>
      </Dialog>

      <DraftManager
        open={showDraftManager}
        onClose={() => setShowDraftManager(false)}
        onLoadDraft={handleLoadDraft}
        currentData={wizardData}
        currentStep={currentStep}
      />

      <CelebrationEffect
        show={showCelebration}
        onComplete={() => setShowCelebration(false)}
        type="both"
        duration={3000}
      />

      {/* Resume Session Dialog */}
      <Dialog open={showResumeDialog} onOpenChange={(_, data) => setShowResumeDialog(data.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>Resume Previous Session?</DialogTitle>
            <DialogContent>
              <div className={styles.resumeDialogContent}>
                <Text>
                  You have an incomplete wizard session from a previous visit. Would you like to
                  continue where you left off?
                </Text>
                {persistenceState.savedAt && (
                  <div className={styles.resumeInfo}>
                    <History24Regular style={{ color: tokens.colorBrandForeground1 }} />
                    <div>
                      <Text weight="semibold" size={300}>
                        Step {persistenceState.savedStep + 1} of {STEP_LABELS.length}
                      </Text>
                      <Text
                        size={200}
                        style={{ display: 'block', color: tokens.colorNeutralForeground3 }}
                      >
                        Last saved: {persistenceState.savedAt.toLocaleString()}
                      </Text>
                    </div>
                  </div>
                )}
              </div>
            </DialogContent>
            <DialogActions>
              <Button
                appearance="secondary"
                icon={<DismissCircle24Regular />}
                onClick={handleStartFresh}
              >
                Start Fresh
              </Button>
              <Button
                appearance="primary"
                icon={<History24Regular />}
                onClick={handleResumeSession}
              >
                Resume Session
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  );
};
