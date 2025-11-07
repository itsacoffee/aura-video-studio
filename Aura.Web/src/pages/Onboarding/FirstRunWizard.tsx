import {
  makeStyles,
  tokens,
  Title2,
  Text,
  Button,
  Card,
  Badge,
  Spinner,
} from '@fluentui/react-components';
import {
  Checkmark24Regular,
  ChevronRight24Regular,
  ChevronLeft24Regular,
  Warning24Regular,
} from '@fluentui/react-icons';
import { useEffect, useReducer, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useNotifications } from '../../components/Notifications/Toasts';
import { CompletionScreen } from '../../components/Onboarding/CompletionScreen';
import type { Dependency } from '../../components/Onboarding/DependencyCheck';
import { DependencyCheck } from '../../components/Onboarding/DependencyCheck';
import { FFmpegDependencyCard } from '../../components/Onboarding/FFmpegDependencyCard';
import { FileLocationsSummary } from '../../components/Onboarding/FileLocationsSummary';
import { OllamaDependencyCard } from '../../components/Onboarding/OllamaDependencyCard';
import { TemplateSelection, defaultTemplates } from '../../components/Onboarding/TemplateSelection';
import { WelcomeScreen } from '../../components/Onboarding/WelcomeScreen';
import type { WorkspacePreferences } from '../../components/Onboarding/WorkspaceSetup';
import { WorkspaceSetup } from '../../components/Onboarding/WorkspaceSetup';
import { WizardProgress } from '../../components/WizardProgress';
import { wizardAnalytics } from '../../services/analytics';
import { markFirstRunCompleted, markWizardNeverShowAgain } from '../../services/firstRunService';
import {
  onboardingReducer,
  initialOnboardingState,
  runValidationThunk,
  detectHardwareThunk,
  installItemThunk,
  checkAllInstallationStatusesThunk,
  validateApiKeyThunk,
  saveWizardStateToStorage,
  loadWizardStateFromStorage,
  clearWizardStateFromStorage,
} from '../../state/onboarding';
import type { FixAction } from '../../state/providers';
import { ApiKeySetupStep } from './ApiKeySetupStep';
import { ChooseTierStep } from './ChooseTierStep';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    padding: tokens.spacingVerticalXXL,
    maxWidth: '900px',
    margin: '0 auto',
  },
  header: {
    textAlign: 'center',
    marginBottom: tokens.spacingVerticalXXL,
  },
  content: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    overflow: 'auto',
    paddingBottom: tokens.spacingVerticalL,
    position: 'relative',
  },
  stepContent: {
    animation: 'slideIn 0.4s ease-out',
  },
  '@keyframes slideIn': {
    from: {
      opacity: 0,
      transform: 'translateX(20px)',
    },
    to: {
      opacity: 1,
      transform: 'translateX(0)',
    },
  },
  footer: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: tokens.spacingVerticalXXL,
    paddingTop: tokens.spacingVerticalL,
    borderTop: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  hardwareInfo: {
    display: 'flex',
    flexDirection: 'column' as const,
    gap: tokens.spacingVerticalM,
  },
  installList: {
    display: 'flex',
    flexDirection: 'column' as const,
    gap: tokens.spacingVerticalS,
  },
  validationItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
  },
  errorCard: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorPaletteRedBackground1,
  },
  fixActionsContainer: {
    display: 'flex',
    flexDirection: 'column' as const,
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalL,
  },
});

export function FirstRunWizard() {
  const styles = useStyles();
  const navigate = useNavigate();
  const [state, dispatch] = useReducer(onboardingReducer, initialOnboardingState);
  const [stepStartTime, setStepStartTime] = useState<number>(Date.now());
  const [wizardStartTime] = useState<number>(Date.now());

  // Hardware detection manual input state
  const [showManualInput, setShowManualInput] = useState(false);
  const [manualVram, setManualVram] = useState<string>('');
  const [hasGpu, setHasGpu] = useState<boolean>(true);

  // Sample generation state
  const [isGeneratingSample, setIsGeneratingSample] = useState(false);
  const [sampleGenerationError, setSampleGenerationError] = useState<string | null>(null);

  // FFmpeg status state
  const [ffmpegReady, setFfmpegReady] = useState(false);

  // Notifications hook
  const { showSuccessToast, showFailureToast } = useNotifications();

  // Enhanced step labels for the new wizard flow
  const totalSteps = 9;
  const stepLabels = [
    'Welcome',
    'Configure Providers',
    'API Keys',
    'Dependencies',
    'Workspace',
    'Templates',
    'Hardware',
    'Validation',
    'Complete',
  ];

  useEffect(() => {
    // Track wizard start
    wizardAnalytics.started();

    // Check for saved progress
    const savedState = loadWizardStateFromStorage();
    if (savedState) {
      // Ask user if they want to resume
      const resume = window.confirm(
        'You have incomplete setup. Would you like to resume where you left off?'
      );
      if (resume && savedState) {
        dispatch({ type: 'LOAD_FROM_STORAGE', payload: savedState });
      } else {
        clearWizardStateFromStorage();
      }
    }
  }, []);

  // Track step changes
  useEffect(() => {
    const currentTime = Date.now();
    const timeSpent = (currentTime - stepStartTime) / 1000; // Convert to seconds

    if (state.step > 0 && timeSpent > 1) {
      // Track previous step completion
      wizardAnalytics.stepCompleted(
        state.step - 1,
        stepLabels[state.step - 1] || 'Unknown',
        timeSpent
      );
    }

    // Track new step view
    wizardAnalytics.stepViewed(state.step, stepLabels[state.step] || 'Unknown');
    setStepStartTime(currentTime);
  }, [state.step]); // eslint-disable-line react-hooks/exhaustive-deps

  // Save progress on state changes
  useEffect(() => {
    if (state.step > 0 && state.step < totalSteps - 1) {
      saveWizardStateToStorage(state);
    }
  }, [state, totalSteps]);

  // Check installation status when entering dependencies step (step 3)
  useEffect(() => {
    if (state.step === 3) {
      checkAllInstallationStatusesThunk(dispatch);
    }
  }, [state.step]);

  // Auto-advance to next step when validation succeeds
  useEffect(() => {
    if (state.status === 'valid' && state.step === 5) {
      // Validation passed on final validation step, mark as ready
      dispatch({ type: 'MARK_READY' });
    }
  }, [state.status, state.step]);

  const handleNext = async () => {
    // Step 0: Welcome -> Step 1: Choose Tier
    if (state.step === 0) {
      dispatch({ type: 'SET_STEP', payload: 1 });
      return;
    }

    // Step 1: Choose Tier -> Step 2: API Keys (or skip to Step 3: Dependencies if Free tier)
    if (state.step === 1) {
      if (!state.selectedTier) {
        alert('Please select a tier to continue');
        return;
      }

      // If Free tier, skip API keys step
      if (state.selectedTier === 'free') {
        dispatch({ type: 'SET_MODE', payload: 'free' });
        dispatch({ type: 'SET_STEP', payload: 3 }); // Skip to dependencies
      } else {
        dispatch({ type: 'SET_MODE', payload: 'pro' });
        dispatch({ type: 'SET_STEP', payload: 2 }); // Go to API keys
      }
      return;
    }

    // Step 2: API Keys -> Step 3: Dependencies
    if (state.step === 2) {
      dispatch({ type: 'SET_STEP', payload: 3 });
      return;
    }

    // Step 3: Dependencies -> Step 4: Workspace
    if (state.step === 3) {
      dispatch({ type: 'SET_STEP', payload: 4 });
      return;
    }

    // Step 4: Workspace -> Step 5: Templates
    if (state.step === 4) {
      dispatch({ type: 'SET_STEP', payload: 5 });
      return;
    }

    // Step 5: Templates -> Step 6: Hardware
    if (state.step === 5) {
      dispatch({ type: 'SET_STEP', payload: 6 });
      return;
    }

    // Step 6: Hardware -> Step 7: Validation
    // Hardware detection is optional - always allow proceeding
    if (state.step === 6) {
      // Trigger detection if not done yet, but don't wait for it
      if (!state.hardware && !state.isDetectingHardware) {
        detectHardwareThunk(dispatch); // Fire and forget
      }
      dispatch({ type: 'SET_STEP', payload: 7 });
      return;
    }

    // Step 7: Validation -> Step 8: Completion
    if (state.step === 7) {
      // Run validation only if not already valid
      if (state.status === 'idle' || state.status === 'installed') {
        await runValidationThunk(state, dispatch);
        return; // Don't advance yet, wait for validation result
      } else if (state.status === 'valid' || state.status === 'ready') {
        // Already validated, move to completion
        dispatch({ type: 'SET_STEP', payload: 8 });
        return;
      } else if (state.status === 'invalid') {
        // Allow proceeding anyway - validation failures shouldn't block
        dispatch({ type: 'SET_STEP', payload: 8 });
        return;
      }
    }

    // Step 8: Completion - handled by completion step buttons
  };

  const handleBack = () => {
    if (state.step > 0) {
      // If going back from dependencies (step 3) and we came from Free tier, go back to tier selection (step 1)
      if (state.step === 3 && state.selectedTier === 'free') {
        dispatch({ type: 'SET_STEP', payload: 1 });
      }
      // If going back from workspace (step 4) and we're Pro tier, go to API keys (step 2)
      else if (state.step === 4 && state.selectedTier === 'pro') {
        dispatch({ type: 'SET_STEP', payload: 2 });
      }
      // Otherwise, go back one step
      else {
        dispatch({ type: 'SET_STEP', payload: state.step - 1 });
      }

      // Reset validation when going back from validation step
      if (state.step === 7) {
        dispatch({ type: 'RESET_VALIDATION' });
      }
    }
  };

  const handleSaveAndExit = () => {
    saveWizardStateToStorage(state);
    navigate('/');
  };

  const handleStepClick = (step: number) => {
    // Allow clicking on completed steps to go back
    if (step < state.step) {
      dispatch({ type: 'SET_STEP', payload: step });
    }
  };

  const completeOnboarding = async () => {
    clearWizardStateFromStorage();
    await markFirstRunCompleted();
    navigate('/create');
  };

  const handleFixAction = (action: FixAction) => {
    switch (action.type) {
      case 'Install':
        navigate(`/downloads?item=${action.parameter}`);
        break;
      case 'Start':
        alert(`To start ${action.parameter}, please follow these steps:\n\n${action.description}`);
        break;
      case 'OpenSettings':
        navigate(`/settings?tab=${action.parameter}`);
        break;
      case 'SwitchToFree':
        dispatch({ type: 'RESET_VALIDATION' });
        alert(`Switched to ${action.parameter}. Click Validate again to check.`);
        break;
      case 'Help':
        if (action.parameter) {
          window.open(action.parameter, '_blank');
        }
        break;
    }
  };

  /* Kept for potential future use with manual dependency installation
  const handleAttachExisting = async (
    itemId: string,
    installPath: string,
    executablePath?: string
  ) => {
    try {
      await attachEngine({
        engineId: itemId,
        installPath,
        executablePath,
      });
      dispatch({ type: 'INSTALL_COMPLETE', payload: itemId });
    } catch (error) {
      console.error(`Failed to attach ${itemId}:`, error);
      dispatch({
        type: 'INSTALL_FAILED',
        payload: {
          itemId,
          error: error instanceof Error ? error.message : 'Failed to attach existing installation',
        },
      });
      throw error;
    }
  };
  */

  const handleSkipItem = (itemId: string) => {
    dispatch({ type: 'SKIP_INSTALL', payload: itemId });
  };

  const handleApiKeyChange = (provider: string, key: string) => {
    dispatch({ type: 'SET_API_KEY', payload: { provider, key } });
  };

  const handleValidateApiKey = async (provider: string) => {
    const apiKey = state.apiKeys[provider];
    if (apiKey) {
      await validateApiKeyThunk(provider, apiKey, dispatch);
    }
  };

  const handleSkipValidation = (provider: string) => {
    dispatch({ type: 'SKIP_API_KEY_VALIDATION', payload: provider });
  };

  const handleSkipAllApiKeys = () => {
    if (
      window.confirm(
        'Are you sure you want to skip API key setup? You can add them later in Settings.'
      )
    ) {
      dispatch({ type: 'SET_STEP', payload: 3 }); // Skip to hardware
    }
  };

  const handleSelectTier = (tier: 'free' | 'pro') => {
    dispatch({ type: 'SET_TIER', payload: tier });
    wizardAnalytics.tierSelected(tier);
  };

  // New handlers for enhanced wizard components
  const handleWorkspacePreferencesChange = (preferences: WorkspacePreferences) => {
    dispatch({ type: 'SET_WORKSPACE_PREFERENCES', payload: preferences });
  };

  const handleBrowseFolder = async (): Promise<string | null> => {
    // Workspace setup component now handles folder picking with real implementation
    return null;
  };

  const handleTemplateSelect = (templateId: string) => {
    dispatch({ type: 'SET_TEMPLATE', payload: templateId });
    wizardAnalytics.templateSelected(templateId);
  };

  const handleUseTemplate = (templateId: string) => {
    dispatch({ type: 'SET_TEMPLATE', payload: templateId });
    wizardAnalytics.templateSelected(templateId);
    handleNext(); // Auto-advance after template selection
  };

  const handleSkipTemplate = () => {
    dispatch({ type: 'SET_TEMPLATE', payload: null });
    handleNext();
  };

  const handleDependencyAutoInstall = async (dependencyId: string): Promise<void> => {
    wizardAnalytics.dependencyInstalled(dependencyId, 'auto');
    await installItemThunk(dependencyId, dispatch);
  };

  const handleDependencyManualInstall = (dependencyId: string) => {
    // Navigate to download page or show instructions
    navigate(`/downloads?item=${dependencyId}`);
  };

  const handleDependencySkip = (dependencyId: string) => {
    handleSkipItem(dependencyId);
  };

  const handleDependencyAssignPath = async (dependencyId: string, path: string): Promise<void> => {
    try {
      // Call the attach API endpoint with URL-encoded component ID
      const encodedDependencyId = encodeURIComponent(dependencyId);
      const response = await fetch(`/api/dependencies/${encodedDependencyId}/attach`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          path,
          attachInPlace: false,
        }),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({
          error: `HTTP ${response.status}: ${response.statusText}`,
        }));
        throw new Error(errorData.error || 'Failed to attach dependency');
      }

      const result = await response.json();

      if (result.success) {
        // Mark as installed
        dispatch({ type: 'INSTALL_COMPLETE', payload: dependencyId });
        // Rescan to verify
        await checkAllInstallationStatusesThunk(dispatch);
      } else {
        throw new Error(result.error || 'Failed to attach dependency');
      }
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error assigning path';
      console.error(`Failed to assign path for ${dependencyId}:`, errorMessage);
      throw error;
    }
  };

  const handleNeverShowAgain = (checked: boolean) => {
    if (checked) {
      markWizardNeverShowAgain();
    }
  };

  const handleGenerateSample = async () => {
    setIsGeneratingSample(true);
    setSampleGenerationError(null);

    try {
      const response = await fetch('/api/quick/demo', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ topic: 'Welcome to Aura Video Studio' }),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({
          detail: `HTTP ${response.status}: ${response.statusText}`,
        }));
        throw new Error(errorData.detail || 'Failed to generate sample video');
      }

      const result = await response.json();

      if (result.jobId) {
        // Show success toast
        showSuccessToast({
          title: 'Sample Video Started',
          message: 'Your sample video is being generated.',
          onViewResults: () => {
            navigate(`/jobs/${result.jobId}`);
          },
        });
      } else {
        setSampleGenerationError('Sample generation started but no job ID was returned');
      }
    } catch (error: unknown) {
      const errorMessage =
        error instanceof Error ? error.message : 'Unknown error generating sample video';
      console.error('Sample generation failed:', errorMessage);
      setSampleGenerationError(errorMessage);

      showFailureToast({
        title: 'Sample Video Failed',
        message: errorMessage,
        onOpenLogs: () => {
          navigate('/logs');
        },
        timeout: 5000,
      });
    } finally {
      setIsGeneratingSample(false);
    }
  };

  // Render step 0: Enhanced Welcome Screen
  const renderStep0 = () => (
    <WelcomeScreen
      onGetStarted={handleNext}
      onImportProject={() => {
        // Future: implement project import
        alert('Project import coming soon!');
      }}
    />
  );

  // Render step 1: Tier Selection with hardware-based recommendation
  const renderStep1 = () => {
    const hardwareProfile = state.hardware
      ? {
          vram: state.hardware.vram || 0,
          hasGpu: (state.hardware.vram || 0) > 0,
          gpuVendor: state.hardware.gpu || undefined,
        }
      : null;

    return (
      <ChooseTierStep
        selectedTier={state.selectedTier}
        onSelectTier={handleSelectTier}
        hardware={hardwareProfile}
      />
    );
  };

  // Render step 2: API Keys
  const renderStep2 = () => (
    <ApiKeySetupStep
      apiKeys={state.apiKeys}
      validationStatus={state.apiKeyValidationStatus}
      validationErrors={state.apiKeyErrors}
      onApiKeyChange={handleApiKeyChange}
      onValidateApiKey={handleValidateApiKey}
      onSkipValidation={handleSkipValidation}
      onSkipAll={handleSkipAllApiKeys}
    />
  );

  // Render step 3: Dependencies with FFmpeg and Ollama cards
  const renderStep3 = () => {
    const otherDependencies: Dependency[] = state.installItems
      .filter((item) => item.id !== 'ffmpeg' && item.id !== 'ollama')
      .map((item) => ({
        id: item.id,
        name: item.name,
        description: item.description || '',
        required: item.required,
        status: state.isScanningDependencies
          ? 'checking'
          : item.installing
            ? 'checking'
            : item.installed
              ? 'installed'
              : item.skipped
                ? 'skipped'
                : 'missing',
        canAutoInstall: true,
        installing: item.installing,
        installProgress: item.installing ? 50 : undefined,
      }));

    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalL }}>
        <div style={{ textAlign: 'center', marginBottom: tokens.spacingVerticalL }}>
          <Title2>Dependency Validation</Title2>
          <Text>
            Checking for required components to ensure the best experience. We&apos;ll help you
            install anything that&apos;s missing.
          </Text>
        </div>

        {/* FFmpeg - Required */}
        <FFmpegDependencyCard
          autoCheck={true}
          onInstallComplete={() => {
            setFfmpegReady(true);
            dispatch({ type: 'INSTALL_COMPLETE', payload: 'ffmpeg' });
          }}
        />

        {/* Ollama - Optional with auto-detect */}
        <OllamaDependencyCard autoDetect={true} />

        {/* Other dependencies */}
        {otherDependencies.length > 0 && (
          <DependencyCheck
            dependencies={otherDependencies}
            onAutoInstall={handleDependencyAutoInstall}
            onManualInstall={handleDependencyManualInstall}
            onSkip={handleDependencySkip}
            onAssignPath={handleDependencyAssignPath}
            onRescan={async () => {
              await checkAllInstallationStatusesThunk(dispatch);
            }}
            isScanning={state.isScanningDependencies}
          />
        )}
      </div>
    );
  };

  // Render step 4: Workspace Setup
  const renderStep4 = () => (
    <WorkspaceSetup
      preferences={state.workspacePreferences}
      onPreferencesChange={handleWorkspacePreferencesChange}
      onBrowseFolder={handleBrowseFolder}
    />
  );

  // Render step 5: Template Selection
  const renderStep5 = () => (
    <TemplateSelection
      templates={defaultTemplates}
      selectedTemplateId={state.selectedTemplate}
      onSelectTemplate={handleTemplateSelect}
      onSkip={handleSkipTemplate}
      onUseTemplate={handleUseTemplate}
    />
  );

  // Render step 6: Hardware Detection
  const renderStep6 = () => {
    const handleDetectAgain = async () => {
      await detectHardwareThunk(dispatch);
    };

    const handleManualSubmit = () => {
      const DEFAULT_VRAM_FALLBACK = 4; // GB - Default fallback for manual GPU configuration
      const vramValue = parseInt(manualVram, 10);
      dispatch({
        type: 'SET_MANUAL_HARDWARE',
        payload: {
          vram: hasGpu ? (isNaN(vramValue) ? DEFAULT_VRAM_FALLBACK : vramValue) : 0,
          hasGpu,
        },
      });
      setShowManualInput(false);
    };

    const handleSkip = () => {
      dispatch({ type: 'SKIP_HARDWARE_DETECTION' });
    };

    return (
      <>
        <Title2>Hardware Detection (Optional)</Title2>
        <Text style={{ marginBottom: tokens.spacingVerticalL }}>
          We&apos;ll detect your GPU to optimize video generation settings. This is optional - you
          can skip and configure later.
        </Text>

        {state.isDetectingHardware ? (
          <Card>
            <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}>
              <Spinner size="small" />
              <Text>Detecting your hardware capabilities...</Text>
            </div>
          </Card>
        ) : state.hardware ? (
          <div className={styles.hardwareInfo}>
            <Card>
              <Title2>System Information</Title2>
              {state.hardware.gpu && (
                <Text>
                  <strong>GPU:</strong> {state.hardware.gpu}
                </Text>
              )}
              {state.hardware.vram !== undefined && state.hardware.vram > 0 && (
                <Text>
                  <strong>VRAM:</strong> {state.hardware.vram}GB
                </Text>
              )}
              <Text style={{ marginTop: tokens.spacingVerticalM }}>
                <strong>Based on your hardware, we suggest:</strong> {state.hardware.recommendation}
              </Text>

              <div
                style={{
                  marginTop: tokens.spacingVerticalL,
                  display: 'flex',
                  gap: tokens.spacingHorizontalM,
                }}
              >
                <Button appearance="secondary" onClick={handleDetectAgain}>
                  Detect Again
                </Button>
                <Button appearance="secondary" onClick={() => setShowManualInput(true)}>
                  Manual Input
                </Button>
              </div>
            </Card>

            {!state.hardware.canRunSD && (
              <Card>
                <Badge appearance="filled" color="informative">
                  ℹ️ Info
                </Badge>
                <Text style={{ marginTop: tokens.spacingVerticalS }}>
                  Your system may not support local Stable Diffusion image generation. No problem!
                  You can use Stock images (free) or connect cloud providers later for AI image
                  generation.
                </Text>
              </Card>
            )}
          </div>
        ) : showManualInput ? (
          <Card>
            <Title2>Manual GPU Configuration</Title2>
            <Text style={{ marginBottom: tokens.spacingVerticalM }}>
              If automatic detection didn&apos;t work, you can manually specify your GPU details:
            </Text>

            <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalM }}>
              <div>
                <label>
                  <input
                    type="radio"
                    checked={hasGpu}
                    onChange={() => setHasGpu(true)}
                    style={{ marginRight: tokens.spacingHorizontalS }}
                  />
                  I have a dedicated GPU
                </label>
              </div>

              {hasGpu && (
                <div>
                  <label htmlFor="vram-input" style={{ display: 'block', marginBottom: '4px' }}>
                    VRAM (GB):
                  </label>
                  <select
                    id="vram-input"
                    value={manualVram}
                    onChange={(e) => setManualVram(e.target.value)}
                    style={{
                      padding: '8px',
                      borderRadius: '4px',
                      border: `1px solid ${tokens.colorNeutralStroke1}`,
                      width: '200px',
                    }}
                  >
                    <option value="">Select VRAM...</option>
                    <option value="4">4 GB</option>
                    <option value="6">6 GB</option>
                    <option value="8">8 GB</option>
                    <option value="10">10 GB</option>
                    <option value="12">12 GB</option>
                    <option value="16">16 GB</option>
                    <option value="24">24 GB</option>
                  </select>
                </div>
              )}

              <div>
                <label>
                  <input
                    type="radio"
                    checked={!hasGpu}
                    onChange={() => setHasGpu(false)}
                    style={{ marginRight: tokens.spacingHorizontalS }}
                  />
                  I don&apos;t have a dedicated GPU (integrated graphics only)
                </label>
              </div>

              <div
                style={{
                  display: 'flex',
                  gap: tokens.spacingHorizontalM,
                  marginTop: tokens.spacingVerticalM,
                }}
              >
                <Button appearance="primary" onClick={handleManualSubmit}>
                  Save Configuration
                </Button>
                <Button appearance="secondary" onClick={() => setShowManualInput(false)}>
                  Cancel
                </Button>
              </div>
            </div>
          </Card>
        ) : (
          <Card>
            <Text style={{ marginBottom: tokens.spacingVerticalL }}>
              We haven&apos;t detected your hardware yet. You can:
            </Text>
            <div style={{ display: 'flex', gap: tokens.spacingHorizontalM, flexWrap: 'wrap' }}>
              <Button appearance="primary" onClick={handleDetectAgain}>
                Detect Hardware
              </Button>
              <Button appearance="secondary" onClick={() => setShowManualInput(true)}>
                Manual Input
              </Button>
              <Button appearance="secondary" onClick={handleSkip}>
                Skip for Now
              </Button>
            </div>
          </Card>
        )}

        {/* Always show prominent skip button */}
        <Card style={{ marginTop: tokens.spacingVerticalL, textAlign: 'center' }}>
          <Text style={{ marginBottom: tokens.spacingVerticalM }}>
            Don&apos;t want to configure hardware now?
          </Text>
          <Button appearance="secondary" size="large" onClick={handleSkip}>
            Continue Without Hardware Detection
          </Button>
        </Card>
      </>
    );
  };

  // Render step 7: Validation & Preflight Checks with real readiness
  const renderStep7 = () => {
    // Check real readiness:
    // 1. FFmpeg installed, valid, and has version
    // 2. At least one TTS provider OR demo TTS skip
    // 3. At least one image provider OR stock/fallback visuals enabled
    const hasTtsProvider = state.installItems.some(
      (item) => item.id.includes('tts') && item.installed
    );
    const hasImageProvider = state.installItems.some(
      (item) => item.id.includes('stable-diffusion') && item.installed
    );

    // For now, we'll allow proceeding if FFmpeg is ready
    // Full TTS/Image validation will be done via the backend preflight API
    const isReady = ffmpegReady;

    return (
      <>
        <Title2>Validation & Preflight Checks</Title2>

        {state.status === 'validating' ? (
          <Card>
            <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}>
              <Spinner size="small" />
              <Text>Running preflight checks...</Text>
            </div>
          </Card>
        ) : isReady && (state.status === 'valid' || state.status === 'ready') ? (
          <>
            <Card>
              <div
                style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}
              >
                <Checkmark24Regular
                  style={{ fontSize: '32px', color: tokens.colorPaletteGreenForeground1 }}
                />
                <div>
                  <Title2>All Checks Passed!</Title2>
                  <Text>Your system is ready to create videos.</Text>
                </div>
              </div>
            </Card>

            <FileLocationsSummary />
          </>
        ) : !isReady ? (
          <>
            <Card
              style={{
                padding: tokens.spacingVerticalL,
                backgroundColor: tokens.colorPaletteYellowBackground1,
              }}
            >
              <div
                style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}
              >
                <Warning24Regular
                  style={{ fontSize: '32px', color: tokens.colorPaletteYellowForeground1 }}
                />
                <div>
                  <Title2>Not Ready</Title2>
                  <Text>Some components need attention before you can generate videos.</Text>
                </div>
              </div>
            </Card>

            {/* Show specific CTAs */}
            <Card style={{ padding: tokens.spacingVerticalL }}>
              <Title2 style={{ marginBottom: tokens.spacingVerticalM }}>Required Actions:</Title2>
              <div
                style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalM }}
              >
                {!ffmpegReady && (
                  <div>
                    <Text
                      weight="semibold"
                      style={{ display: 'block', marginBottom: tokens.spacingVerticalS }}
                    >
                      ⚠ FFmpeg Not Ready
                    </Text>
                    <Text
                      size={200}
                      style={{ display: 'block', marginBottom: tokens.spacingVerticalS }}
                    >
                      FFmpeg is required for video rendering. Install it from the Dependencies step.
                    </Text>
                    <Button
                      appearance="primary"
                      onClick={() => dispatch({ type: 'SET_STEP', payload: 3 })}
                    >
                      Go to Dependencies
                    </Button>
                  </div>
                )}
                {!hasTtsProvider && (
                  <div>
                    <Text
                      weight="semibold"
                      style={{ display: 'block', marginBottom: tokens.spacingVerticalS }}
                    >
                      ℹ No TTS Provider
                    </Text>
                    <Text
                      size={200}
                      style={{ display: 'block', marginBottom: tokens.spacingVerticalS }}
                    >
                      You can use Windows TTS (built-in) or configure other providers in Settings.
                    </Text>
                    <Button
                      appearance="secondary"
                      onClick={() => window.open('/settings?tab=providers', '_blank')}
                    >
                      Configure TTS
                    </Button>
                  </div>
                )}
                {!hasImageProvider && (
                  <div>
                    <Text
                      weight="semibold"
                      style={{ display: 'block', marginBottom: tokens.spacingVerticalS }}
                    >
                      ℹ No Image Provider
                    </Text>
                    <Text
                      size={200}
                      style={{ display: 'block', marginBottom: tokens.spacingVerticalS }}
                    >
                      You can use stock visuals (built-in) or configure image providers in Settings.
                    </Text>
                    <Button
                      appearance="secondary"
                      onClick={() => window.open('/settings?tab=providers', '_blank')}
                    >
                      Configure Images
                    </Button>
                  </div>
                )}
              </div>
            </Card>
          </>
        ) : state.status === 'invalid' && state.lastValidation ? (
          <>
            <Card
              style={{
                padding: tokens.spacingVerticalL,
                backgroundColor: tokens.colorPaletteRedBackground1,
              }}
            >
              <div
                style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}
              >
                <Warning24Regular
                  style={{ fontSize: '32px', color: tokens.colorPaletteRedForeground1 }}
                />
                <div>
                  <Title2>Validation Failed</Title2>
                  <Text>
                    Some providers are not available. Please fix the issues below or click Next to
                    continue anyway.
                  </Text>
                </div>
              </div>
            </Card>

            {state.lastValidation.failedStages.map((stage, index) => (
              <Card key={index}>
                <Title2>{stage.stage} Stage</Title2>
                <Text>
                  <strong>Provider:</strong> {stage.provider}
                </Text>
                <Text>
                  <strong>Issue:</strong> {stage.message}
                </Text>
                {stage.hint && (
                  <Text style={{ marginTop: tokens.spacingVerticalS, fontStyle: 'italic' }}>
                    💡 {stage.hint}
                  </Text>
                )}

                {stage.suggestions && stage.suggestions.length > 0 && (
                  <div style={{ marginTop: tokens.spacingVerticalM }}>
                    <Text weight="semibold">Suggestions:</Text>
                    <ul style={{ marginTop: tokens.spacingVerticalXS }}>
                      {stage.suggestions.map((suggestion, i) => (
                        <li key={i}>
                          <Text size={200}>{suggestion}</Text>
                        </li>
                      ))}
                    </ul>
                  </div>
                )}

                {stage.fixActions && stage.fixActions.length > 0 && (
                  <div
                    style={{
                      display: 'flex',
                      flexDirection: 'column',
                      gap: tokens.spacingVerticalM,
                      marginTop: tokens.spacingVerticalL,
                    }}
                  >
                    <Text weight="semibold">Quick Fixes:</Text>
                    {stage.fixActions.map((action, i) => (
                      <Button
                        key={i}
                        appearance="secondary"
                        onClick={() => handleFixAction(action)}
                        style={{ justifyContent: 'flex-start' }}
                      >
                        {action.label}
                      </Button>
                    ))}
                  </div>
                )}
              </Card>
            ))}
          </>
        ) : (
          <Card>
            <Text>
              Click Validate to check your setup and ensure all providers are working correctly.
            </Text>
          </Card>
        )}
      </>
    );
  };

  // Render step 8: Enhanced Completion with Quick Demo
  const renderStep8 = () => {
    const validApiKeys = Object.entries(state.apiKeyValidationStatus)
      .filter(([_, status]) => status === 'valid')
      .map(([provider]) => provider);

    const installedComponents = state.installItems
      .filter((item) => item.installed)
      .map((item) => item.name);

    const templateName = state.selectedTemplate
      ? defaultTemplates.find((t) => t.id === state.selectedTemplate)?.name
      : undefined;

    return (
      <CompletionScreen
        summary={{
          tier: state.selectedTier || 'free',
          apiKeysConfigured: validApiKeys,
          hardwareDetected: !!state.hardware,
          componentsInstalled: installedComponents,
          workspaceConfigured: true,
          tutorialCompleted: false,
          templateSelected: templateName,
        }}
        onCreateFirstVideo={completeOnboarding}
        onGenerateSample={handleGenerateSample}
        isGeneratingSample={isGeneratingSample}
        sampleGenerationError={sampleGenerationError}
        onExploreApp={async () => {
          clearWizardStateFromStorage();
          await markFirstRunCompleted();

          // Track completion
          const totalTime = (Date.now() - wizardStartTime) / 1000;
          wizardAnalytics.completed(totalTime, {
            tier: state.selectedTier || 'free',
            api_keys_count: validApiKeys.length,
            components_installed: installedComponents.length,
            template_selected: !!state.selectedTemplate,
            tutorial_completed: false,
          });

          navigate('/');
        }}
        onNeverShowAgain={handleNeverShowAgain}
        showNeverShowAgain={true}
      />
    );
  };

  const renderStepContent = () => {
    switch (state.step) {
      case 0:
        return renderStep0();
      case 1:
        return renderStep1();
      case 2:
        return renderStep2();
      case 3:
        return renderStep3();
      case 4:
        return renderStep4();
      case 5:
        return renderStep5();
      case 6:
        return renderStep6();
      case 7:
        return renderStep7();
      case 8:
        return renderStep8();
      default:
        return null;
    }
  };

  const buttonLabel =
    state.step === 7
      ? state.status === 'idle' || state.status === 'installed'
        ? 'Validate'
        : state.status === 'invalid'
          ? 'Next Anyway'
          : 'Next'
      : 'Next';
  const buttonDisabled =
    (state.step === 1 && !state.selectedTier) ||
    state.isDetectingHardware ||
    state.status === 'validating' ||
    state.status === 'installing';

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <WizardProgress
          currentStep={state.step}
          totalSteps={totalSteps}
          stepLabels={stepLabels}
          onStepClick={handleStepClick}
          onSaveAndExit={state.step < totalSteps - 1 ? handleSaveAndExit : undefined}
        />
      </div>

      <div className={styles.content}>
        <div className={styles.stepContent} key={state.step}>
          {renderStepContent()}
        </div>
      </div>

      {state.step < totalSteps - 1 && (
        <div className={styles.footer}>
          <Button appearance="subtle" onClick={handleSaveAndExit}>
            Save and Exit
          </Button>

          <div style={{ display: 'flex', gap: tokens.spacingHorizontalM }}>
            {state.step > 0 && (
              <Button
                appearance="secondary"
                icon={<ChevronLeft24Regular />}
                onClick={handleBack}
                disabled={buttonDisabled}
              >
                Back
              </Button>
            )}
            <Button
              appearance="primary"
              icon={
                state.status === 'validating' || state.status === 'installing' ? (
                  <Spinner size="tiny" />
                ) : (
                  <ChevronRight24Regular />
                )
              }
              iconPosition="after"
              onClick={handleNext}
              disabled={buttonDisabled}
            >
              {buttonLabel}
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
