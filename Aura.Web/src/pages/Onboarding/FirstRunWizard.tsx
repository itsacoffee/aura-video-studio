import {
  makeStyles,
  tokens,
  Title2,
  Text,
  Button,
  Card,
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
import { FFmpegDependencyCard } from '../../components/Onboarding/FFmpegDependencyCard';
import { WelcomeScreen } from '../../components/Onboarding/WelcomeScreen';
import type { WorkspacePreferences } from '../../components/Onboarding/WorkspaceSetup';
import { WorkspaceSetup } from '../../components/Onboarding/WorkspaceSetup';
import { WizardProgress } from '../../components/WizardProgress';
import { wizardAnalytics } from '../../services/analytics';
import { setupApi } from '../../services/api/setupApi';
import { markFirstRunCompleted } from '../../services/firstRunService';
import {
  onboardingReducer,
  initialOnboardingState,
  validateApiKeyThunk,
  saveWizardStateToStorage,
  loadWizardStateFromStorage,
  clearWizardStateFromStorage,
} from '../../state/onboarding';
import { ApiKeySetupStep } from './ApiKeySetupStep';

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

export interface FirstRunWizardProps {
  onComplete?: () => void | Promise<void>;
}

export function FirstRunWizard({ onComplete }: FirstRunWizardProps = {}) {
  const styles = useStyles();
  const navigate = useNavigate();
  const [state, dispatch] = useReducer(onboardingReducer, initialOnboardingState);
  const [stepStartTime, setStepStartTime] = useState<number>(Date.now());
  const [wizardStartTime] = useState<number>(Date.now());

  // FFmpeg status state
  const [ffmpegReady, setFfmpegReady] = useState(false);
  const [ffmpegPath, setFfmpegPath] = useState<string | null>(null);

  // Provider validation state
  const [hasAtLeastOneProvider, setHasAtLeastOneProvider] = useState(false);

  // Notifications hook
  const { showSuccessToast, showFailureToast } = useNotifications();

  // Simplified mandatory setup flow - 5 core steps
  const totalSteps = 5;
  const stepLabels = [
    'Welcome',
    'FFmpeg Installation',
    'Provider Configuration',
    'Workspace Setup',
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

  // Check if at least one provider is configured
  useEffect(() => {
    const validProviders = Object.entries(state.apiKeyValidationStatus).filter(
      ([_, status]) => status === 'valid'
    );
    const hasOfflineMode = state.selectedTier === 'free' && state.mode === 'free';
    setHasAtLeastOneProvider(validProviders.length > 0 || hasOfflineMode);
  }, [state.apiKeyValidationStatus, state.selectedTier, state.mode]);

  // Navigation protection: Prevent leaving page during setup
  useEffect(() => {
    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      // Only show warning if setup is not complete (not on final step)
      if (state.step < totalSteps - 1) {
        e.preventDefault();
        e.returnValue = '';
        return '';
      }
    };

    window.addEventListener('beforeunload', handleBeforeUnload);

    return () => {
      window.removeEventListener('beforeunload', handleBeforeUnload);
    };
  }, [state.step, totalSteps]);

  // Prevent browser back button during setup
  useEffect(() => {
    const preventBackNavigation = () => {
      window.history.pushState(null, '', window.location.href);
    };

    // Push initial state
    window.history.pushState(null, '', window.location.href);

    // Listen for popstate (back/forward button)
    window.addEventListener('popstate', preventBackNavigation);

    return () => {
      window.removeEventListener('popstate', preventBackNavigation);
    };
  }, []);

  const handleNext = async () => {
    // Step 0: Welcome -> Step 1: FFmpeg Installation
    if (state.step === 0) {
      dispatch({ type: 'SET_STEP', payload: 1 });
      return;
    }

    // Step 1: FFmpeg Installation -> Step 2: Provider Configuration
    if (state.step === 1) {
      // Allow proceeding even without FFmpeg (user can skip)
      // The warning is already shown in the UI
      dispatch({ type: 'SET_STEP', payload: 2 });
      return;
    }

    // Step 2: Provider Configuration -> Step 3: Workspace Setup
    if (state.step === 2) {
      // Must have at least one provider configured
      if (!hasAtLeastOneProvider) {
        showFailureToast({
          title: 'Provider Required',
          message: 'Please configure at least one LLM provider or choose offline mode to continue.',
        });
        return;
      }
      dispatch({ type: 'SET_STEP', payload: 3 });
      return;
    }

    // Step 3: Workspace Setup -> Step 4: Complete
    if (state.step === 3) {
      // Validate workspace is configured
      if (!state.workspacePreferences?.defaultSaveLocation) {
        showFailureToast({
          title: 'Workspace Required',
          message: 'Please configure your workspace location to continue.',
        });
        return;
      }
      dispatch({ type: 'SET_STEP', payload: 4 });
      return;
    }

    // Step 4: Completion - handled by completion step buttons
  };

  const handleBack = () => {
    if (state.step > 0) {
      dispatch({ type: 'SET_STEP', payload: state.step - 1 });
    }
  };

  const handleStepClick = (step: number) => {
    // Allow clicking on completed steps to go back, but not forward
    if (step < state.step) {
      dispatch({ type: 'SET_STEP', payload: step });
    }
  };

  const completeOnboarding = async () => {
    try {
      // Call backend API to complete setup and persist to database
      const setupResult = await setupApi.completeSetup({
        ffmpegPath: ffmpegPath,
        outputDirectory: state.workspacePreferences?.defaultSaveLocation,
      });

      if (!setupResult.success) {
        showFailureToast({
          title: 'Setup Validation Failed',
          message: setupResult.errors?.join(', ') || 'Please ensure all requirements are met.',
        });
        return;
      }

      // Clear wizard state and mark local completion
      clearWizardStateFromStorage();
      await markFirstRunCompleted();

      // Track completion
      const totalTime = (Date.now() - wizardStartTime) / 1000;
      const validApiKeys = Object.entries(state.apiKeyValidationStatus)
        .filter(([_, status]) => status === 'valid')
        .map(([provider]) => provider);

      wizardAnalytics.completed(totalTime, {
        tier: state.selectedTier || 'free',
        api_keys_count: validApiKeys.length,
        components_installed: ffmpegReady ? 1 : 0,
        template_selected: false,
        tutorial_completed: false,
      });

      showSuccessToast({
        title: 'Setup Complete',
        message: "Welcome to Aura Video Studio! Let's create your first video.",
      });

      // Call the onComplete callback if provided
      if (onComplete) {
        await onComplete();
      } else {
        // Fallback to navigation
        navigate('/');
      }
    } catch (error) {
      console.error('Error completing setup:', error);
      showFailureToast({
        title: 'Setup Error',
        message: 'Failed to complete setup. Please try again.',
      });
    }
  };

  // Handler functions for simplified setup
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

  const handleWorkspacePreferencesChange = async (preferences: WorkspacePreferences) => {
    dispatch({ type: 'SET_WORKSPACE_PREFERENCES', payload: preferences });

    // Validate directory with backend
    if (preferences.defaultSaveLocation) {
      try {
        const dirCheck = await setupApi.checkDirectory({ path: preferences.defaultSaveLocation });
        if (!dirCheck.isValid) {
          showFailureToast({
            title: 'Invalid Directory',
            message: dirCheck.error || 'The selected directory is not writable.',
          });
        }
      } catch (error) {
        console.warn('Could not validate directory with backend:', error);
      }
    }
  };

  const handleBrowseFolder = async (): Promise<string | null> => {
    // Workspace setup component handles folder picking with real implementation
    return null;
  };

  const renderStep0 = () => (
    <WelcomeScreen
      onGetStarted={handleNext}
      onImportProject={() => {
        // Future: implement project import
        alert('Project import coming soon!');
      }}
    />
  );

  // New simplified step renderers for mandatory setup

  const renderStep1FFmpeg = () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalL }}>
      <div style={{ textAlign: 'center', marginBottom: tokens.spacingVerticalL }}>
        <Title2>FFmpeg Installation</Title2>
        <Text style={{ display: 'block', marginTop: tokens.spacingVerticalM }}>
          FFmpeg is required for video generation. We&apos;ll help you install it automatically.
        </Text>
        <Card
          style={{
            marginTop: tokens.spacingVerticalL,
            padding: tokens.spacingVerticalM,
            backgroundColor: tokens.colorNeutralBackground3,
          }}
        >
          <Text size={300}>
            <strong>Why is this required?</strong>
          </Text>
          <Text style={{ display: 'block', marginTop: tokens.spacingVerticalS }}>
            FFmpeg is the industry-standard tool for video processing. Aura uses it to render your
            videos, add transitions, apply effects, and export in various formats. Without FFmpeg,
            video generation cannot proceed.
          </Text>
        </Card>
      </div>

      <FFmpegDependencyCard
        autoCheck={true}
        autoExpandDetails={true}
        onInstallComplete={async () => {
          setFfmpegReady(true);
          dispatch({ type: 'INSTALL_COMPLETE', payload: 'ffmpeg' });

          // Also check with new setup API to get path
          try {
            const ffmpegCheck = await setupApi.checkFFmpeg();
            if (ffmpegCheck.isInstalled && ffmpegCheck.path) {
              setFfmpegPath(ffmpegCheck.path);
            }
          } catch (error) {
            console.warn('Could not get FFmpeg path from setup API:', error);
          }
        }}
      />

      {!ffmpegReady && (
        <Card
          style={{
            padding: tokens.spacingVerticalL,
            backgroundColor: tokens.colorPaletteYellowBackground1,
            borderLeft: `4px solid ${tokens.colorPaletteYellowBorder1}`,
          }}
        >
          <Text
            weight="semibold"
            style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}
          >
            <Warning24Regular /> Want to install FFmpeg manually?
          </Text>
          <Text style={{ display: 'block', marginTop: tokens.spacingVerticalS }}>
            If you prefer to install FFmpeg yourself or already have it installed, you can skip this
            step. However, video generation will not work until FFmpeg is properly installed.
          </Text>
          <div
            style={{
              marginTop: tokens.spacingVerticalM,
              display: 'flex',
              gap: tokens.spacingHorizontalS,
            }}
          >
            <Button
              appearance="secondary"
              onClick={() => {
                if (
                  window.confirm(
                    'Are you sure you want to skip FFmpeg installation? Video generation will not work without FFmpeg. You can install it later from Settings.'
                  )
                ) {
                  setFfmpegReady(true);
                  showSuccessToast({
                    title: 'FFmpeg Skipped',
                    message:
                      'Remember to install FFmpeg before creating videos. You can do this from Settings.',
                  });
                }
              }}
            >
              Skip for Now
            </Button>
          </div>
        </Card>
      )}
    </div>
  );

  // Step 2: Provider Configuration (At least one required)
  const renderStep2Providers = () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalL }}>
      <div style={{ textAlign: 'center', marginBottom: tokens.spacingVerticalL }}>
        <Title2>Provider Configuration</Title2>
        <Text style={{ display: 'block', marginTop: tokens.spacingVerticalM }}>
          Configure at least ONE LLM provider to generate video scripts, or use offline mode.
        </Text>
        <Card
          style={{
            marginTop: tokens.spacingVerticalL,
            padding: tokens.spacingVerticalM,
            backgroundColor: tokens.colorNeutralBackground3,
          }}
        >
          <Text size={300}>
            <strong>Why is this required?</strong>
          </Text>
          <Text style={{ display: 'block', marginTop: tokens.spacingVerticalS }}>
            LLM providers power the AI script generation. You need at least one configured to create
            video scripts automatically. Premium providers (OpenAI, Anthropic) offer higher quality,
            while offline mode provides basic functionality without API keys.
          </Text>
        </Card>
      </div>

      <ApiKeySetupStep
        apiKeys={state.apiKeys}
        validationStatus={state.apiKeyValidationStatus}
        validationErrors={state.apiKeyErrors}
        onApiKeyChange={handleApiKeyChange}
        onValidateApiKey={handleValidateApiKey}
        onSkipValidation={handleSkipValidation}
        onSkipAll={() => {
          dispatch({ type: 'SET_MODE', payload: 'free' });
          dispatch({ type: 'SET_TIER', payload: 'free' });
          setHasAtLeastOneProvider(true);
          showSuccessToast({
            title: 'Offline Mode Enabled',
            message: 'Using rule-based script generation. You can add API keys later in Settings.',
          });
        }}
      />

      {!hasAtLeastOneProvider && (
        <Card
          style={{
            padding: tokens.spacingVerticalM,
            backgroundColor: tokens.colorPaletteRedBackground1,
          }}
        >
          <Text
            weight="semibold"
            style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}
          >
            <Warning24Regular /> At least one provider is required
          </Text>
          <Text style={{ display: 'block', marginTop: tokens.spacingVerticalS }}>
            Configure at least one API key and validate it, or click &quot;Skip All&quot; to use
            offline mode.
          </Text>
        </Card>
      )}
    </div>
  );

  // Step 3: Workspace Setup (Required)
  const renderStep3Workspace = () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalL }}>
      <div style={{ textAlign: 'center', marginBottom: tokens.spacingVerticalL }}>
        <Title2>Workspace Setup</Title2>
        <Text style={{ display: 'block', marginTop: tokens.spacingVerticalM }}>
          Configure where Aura will save your videos and cache files.
        </Text>
        <Card
          style={{
            marginTop: tokens.spacingVerticalL,
            padding: tokens.spacingVerticalM,
            backgroundColor: tokens.colorNeutralBackground3,
          }}
        >
          <Text size={300}>
            <strong>Why is this required?</strong>
          </Text>
          <Text style={{ display: 'block', marginTop: tokens.spacingVerticalS }}>
            Aura needs to know where to save your generated videos and temporary files. We&apos;ve
            pre-filled sensible defaults for your operating system, but you can customize these
            locations.
          </Text>
        </Card>
      </div>

      <WorkspaceSetup
        preferences={state.workspacePreferences}
        onPreferencesChange={handleWorkspacePreferencesChange}
        onBrowseFolder={handleBrowseFolder}
      />
    </div>
  );

  // Step 4: Setup Complete
  const renderStep4Complete = () => {
    const validApiKeys = Object.entries(state.apiKeyValidationStatus)
      .filter(([_, status]) => status === 'valid')
      .map(([provider]) => provider);

    return (
      <div
        style={{
          display: 'flex',
          flexDirection: 'column',
          gap: tokens.spacingVerticalL,
          textAlign: 'center',
        }}
      >
        <div style={{ padding: tokens.spacingVerticalXXL }}>
          <div style={{ fontSize: '64px', marginBottom: tokens.spacingVerticalL }}>
            <Checkmark24Regular
              style={{ width: '64px', height: '64px', color: tokens.colorPaletteGreenForeground1 }}
            />
          </div>
          <Title2>Setup Complete! Let&apos;s create your first video</Title2>
          <Text
            style={{
              display: 'block',
              marginTop: tokens.spacingVerticalM,
              marginBottom: tokens.spacingVerticalXL,
            }}
          >
            You&apos;re all set! Here&apos;s what we configured:
          </Text>

          <Card
            style={{
              padding: tokens.spacingVerticalL,
              textAlign: 'left',
              maxWidth: '600px',
              margin: '0 auto',
            }}
          >
            <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalM }}>
              <div
                style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}
              >
                <Checkmark24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />
                <Text weight="semibold">FFmpeg installed and ready</Text>
              </div>

              {validApiKeys.length > 0 ? (
                <div
                  style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}
                >
                  <Checkmark24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />
                  <Text weight="semibold">
                    {validApiKeys.length} LLM provider(s) configured: {validApiKeys.join(', ')}
                  </Text>
                </div>
              ) : (
                <div
                  style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}
                >
                  <Checkmark24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />
                  <Text weight="semibold">Offline mode enabled (rule-based generation)</Text>
                </div>
              )}

              <div
                style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}
              >
                <Checkmark24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />
                <Text weight="semibold">Workspace configured</Text>
              </div>
            </div>
          </Card>

          <div
            style={{
              marginTop: tokens.spacingVerticalXXL,
              display: 'flex',
              gap: tokens.spacingHorizontalL,
              justifyContent: 'center',
            }}
          >
            <Button appearance="primary" size="large" onClick={completeOnboarding}>
              Start Creating Videos
            </Button>
          </div>
        </div>
      </div>
    );
  };

  const renderStepContent = () => {
    switch (state.step) {
      case 0:
        return renderStep0(); // Welcome
      case 1:
        return renderStep1FFmpeg(); // FFmpeg Installation
      case 2:
        return renderStep2Providers(); // Provider Configuration
      case 3:
        return renderStep3Workspace(); // Workspace Setup
      case 4:
        return renderStep4Complete(); // Complete
      default:
        return null;
    }
  };

  const buttonLabel = 'Next';
  const buttonDisabled =
    (state.step === 2 && !hasAtLeastOneProvider) ||
    state.status === 'validating' ||
    state.status === 'installing';

  return (
    <div
      className={styles.container}
      style={{
        position: 'fixed',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        zIndex: 10000,
        backgroundColor: tokens.colorNeutralBackground1,
        overflow: 'auto',
      }}
    >
      <div
        className={styles.header}
        style={{ textAlign: 'center', paddingTop: tokens.spacingVerticalXXL }}
      >
        <Title2>Welcome to Aura Video Studio - Let&apos;s get you set up!</Title2>
        <Text
          style={{
            display: 'block',
            marginTop: tokens.spacingVerticalS,
            marginBottom: tokens.spacingVerticalL,
          }}
        >
          Step {state.step + 1} of {totalSteps} - Required Setup
        </Text>
        <WizardProgress
          currentStep={state.step}
          totalSteps={totalSteps}
          stepLabels={stepLabels}
          onStepClick={handleStepClick}
          onSaveAndExit={undefined} // No exit during mandatory setup
        />
      </div>

      <div className={styles.content}>
        <div className={styles.stepContent} key={state.step}>
          {renderStepContent()}
        </div>
      </div>

      {state.step < totalSteps - 1 && (
        <div className={styles.footer}>
          <div style={{ flex: 1 }} />

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
