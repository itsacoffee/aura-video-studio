import { useEffect, useReducer } from 'react';
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
import { useNavigate } from 'react-router-dom';
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
import { InstallItemCard } from '../../components/Onboarding/InstallItemCard';
import { FileLocationsSummary } from '../../components/Onboarding/FileLocationsSummary';
import { WizardProgress } from '../../components/WizardProgress';
import { WelcomeStep } from './WelcomeStep';
import { ChooseTierStep } from './ChooseTierStep';
import { ApiKeySetupStep } from './ApiKeySetupStep';
import { CompletionStep } from './CompletionStep';
import { useEnginesStore } from '../../state/engines';

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
  const { attachEngine } = useEnginesStore();

  const totalSteps = 7;
  const stepLabels = ['Welcome', 'Choose Tier', 'API Keys', 'Hardware', 'Dependencies', 'Validation', 'Complete'];

  useEffect(() => {
    // Check if this is truly first run
    const hasSeenOnboarding = localStorage.getItem('hasSeenOnboarding');
    if (hasSeenOnboarding === 'true') {
      // User has already seen onboarding, redirect to home
      navigate('/');
      return;
    }

    // Check for saved progress
    const savedState = loadWizardStateFromStorage();
    if (savedState) {
      // Ask user if they want to resume
      const resume = window.confirm('You have incomplete setup. Would you like to resume where you left off?');
      if (resume && savedState) {
        dispatch({ type: 'LOAD_FROM_STORAGE', payload: savedState });
      } else {
        clearWizardStateFromStorage();
      }
    }
  }, [navigate]);

  // Save progress on state changes
  useEffect(() => {
    if (state.step > 0 && state.step < totalSteps - 1) {
      saveWizardStateToStorage(state);
    }
  }, [state, totalSteps]);

  // Check installation status when entering dependencies step (step 4)
  useEffect(() => {
    if (state.step === 4) {
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

    // Step 1: Choose Tier -> Step 2: API Keys (or skip to Step 3 if Free tier)
    if (state.step === 1) {
      if (!state.selectedTier) {
        alert('Please select a tier to continue');
        return;
      }
      
      // If Free tier, skip API keys step
      if (state.selectedTier === 'free') {
        dispatch({ type: 'SET_MODE', payload: 'free' });
        dispatch({ type: 'SET_STEP', payload: 3 }); // Skip to hardware detection
      } else {
        dispatch({ type: 'SET_MODE', payload: 'pro' });
        dispatch({ type: 'SET_STEP', payload: 2 }); // Go to API keys
      }
      return;
    }

    // Step 2: API Keys -> Step 3: Hardware
    if (state.step === 2) {
      dispatch({ type: 'SET_STEP', payload: 3 });
      return;
    }

    // Step 3: Hardware -> Step 4: Dependencies
    if (state.step === 3) {
      if (!state.hardware) {
        // Detect hardware before moving forward
        await detectHardwareThunk(dispatch);
      }
      dispatch({ type: 'SET_STEP', payload: 4 });
      return;
    }

    // Step 4: Dependencies -> Step 5: Validation
    if (state.step === 4) {
      // Install required items
      const requiredItems = state.installItems.filter((item) => item.required && !item.installed);
      for (const item of requiredItems) {
        await installItemThunk(item.id, dispatch);
      }
      dispatch({ type: 'SET_STEP', payload: 5 });
      return;
    }

    // Step 5: Validation -> Step 6: Complete
    if (state.step === 5) {
      // Run validation only if not already valid
      if (state.status === 'idle' || state.status === 'installed') {
        await runValidationThunk(state, dispatch);
        return; // Don't advance yet, wait for validation result
      } else if (state.status === 'valid' || state.status === 'ready') {
        // Already validated, move to completion
        dispatch({ type: 'SET_STEP', payload: 6 });
        return;
      } else if (state.status === 'invalid') {
        // Show fix actions, don't advance
        return;
      }
    }

    // Step 6: Completion - handled by completion step buttons
  };

  const handleBack = () => {
    if (state.step > 0) {
      // If going back from hardware (step 3) and we came from Free tier, go back to tier selection (step 1)
      if (state.step === 3 && state.selectedTier === 'free') {
        dispatch({ type: 'SET_STEP', payload: 1 });
      }
      // If going back from dependencies (step 4) and we're Pro tier, go to API keys (step 2)
      else if (state.step === 4 && state.selectedTier === 'pro') {
        dispatch({ type: 'SET_STEP', payload: 2 });
      }
      // Otherwise, go back one step
      else {
        dispatch({ type: 'SET_STEP', payload: state.step - 1 });
      }
      
      // Reset validation when going back from validation step
      if (state.step === 5) {
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

  const completeOnboarding = () => {
    clearWizardStateFromStorage();
    localStorage.setItem('hasSeenOnboarding', 'true');
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

  const handleSkipItem = (itemId: string) => {
    dispatch({ type: 'INSTALL_COMPLETE', payload: itemId });
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

  const handleSkipAllApiKeys = () => {
    if (window.confirm('Are you sure you want to skip API key setup? You can add them later in Settings.')) {
      dispatch({ type: 'SET_STEP', payload: 3 }); // Skip to hardware
    }
  };

  const handleSelectTier = (tier: 'free' | 'pro') => {
    dispatch({ type: 'SET_TIER', payload: tier });
  };

  const renderStep0 = () => <WelcomeStep />;

  const renderStep1 = () => (
    <ChooseTierStep
      selectedTier={state.selectedTier}
      onSelectTier={handleSelectTier}
    />
  );

  const renderStep2 = () => (
    <ApiKeySetupStep
      apiKeys={state.apiKeys}
      validationStatus={state.apiKeyValidationStatus}
      validationErrors={state.apiKeyErrors}
      onApiKeyChange={handleApiKeyChange}
      onValidateApiKey={handleValidateApiKey}
      onSkipAll={handleSkipAllApiKeys}
    />
  );

  const renderStep3 = () => (
    <>
      <Title2>Hardware Detection</Title2>

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
            {state.hardware.gpu && <Text>GPU: {state.hardware.gpu}</Text>}
            {state.hardware.vram && <Text>VRAM: {state.hardware.vram}GB</Text>}
            <Text style={{ marginTop: tokens.spacingVerticalM }}>
              <strong>Recommendation:</strong> {state.hardware.recommendation}
            </Text>
          </Card>

          {!state.hardware.canRunSD && state.mode === 'local' && (
            <Card>
              <Badge appearance="filled" color="warning">
                ⚠ Note
              </Badge>
              <Text style={{ marginTop: tokens.spacingVerticalS }}>
                Your system doesn&apos;t meet the requirements for local Stable Diffusion. We&apos;ll use
                Stock images as a fallback, or you can add cloud Pro providers later.
              </Text>
            </Card>
          )}
        </div>
      ) : (
        <Card>
          <Text>Click Next to detect your hardware...</Text>
        </Card>
      )}
    </>
  );

  const renderStep4 = () => (
    <>
      <Title2>Install Required Components</Title2>
      <Text>We&apos;ll help you install the necessary tools for your chosen mode.</Text>

      <div className={styles.installList}>
        {state.installItems.map((item) => (
          <InstallItemCard
            key={item.id}
            item={item}
            onInstall={() => installItemThunk(item.id, dispatch)}
            onAttachExisting={async (installPath, executablePath) => {
              await handleAttachExisting(item.id, installPath, executablePath);
            }}
            onSkip={!item.required ? () => handleSkipItem(item.id) : undefined}
          />
        ))}
      </div>

      <Card style={{ backgroundColor: tokens.colorNeutralBackground3 }}>
        <Text weight="semibold" style={{ marginBottom: tokens.spacingVerticalS }}>
          📌 Installation Options
        </Text>
        <Text style={{ marginBottom: tokens.spacingVerticalM }}>
          For each component, you have three options:
        </Text>
        <ul style={{ marginLeft: tokens.spacingHorizontalL, marginBottom: 0 }}>
          <li>
            <Text>
              <strong>Install:</strong> Automatically download and install to the default location
            </Text>
          </li>
          <li>
            <Text>
              <strong>Use Existing:</strong> If you already have it installed, point Aura to its location
            </Text>
          </li>
          <li>
            <Text>
              <strong>Skip:</strong> Skip optional components (you can install them later)
            </Text>
          </li>
        </ul>
      </Card>

      {state.errors.length > 0 && (
        <Card
          style={{
            backgroundColor: tokens.colorPaletteRedBackground1,
            padding: tokens.spacingVerticalM,
          }}
        >
          <Text
            weight="semibold"
            style={{
              color: tokens.colorPaletteRedForeground1,
              marginBottom: tokens.spacingVerticalS,
            }}
          >
            ⚠️ Installation Errors
          </Text>
          {state.errors.map((error, index) => (
            <Text
              key={index}
              size={200}
              style={{
                color: tokens.colorPaletteRedForeground1,
                display: 'block',
                marginTop: tokens.spacingVerticalXS,
              }}
            >
              • {error}
            </Text>
          ))}
        </Card>
      )}
    </>
  );

  const renderStep5 = () => (
    <>
      <Title2>Validation & Preflight Checks</Title2>

      {state.status === 'validating' ? (
        <Card>
          <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}>
            <Spinner size="small" />
            <Text>Running preflight checks...</Text>
          </div>
        </Card>
      ) : state.status === 'valid' || state.status === 'ready' ? (
        <>
          <Card>
            <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}>
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
      ) : state.status === 'invalid' && state.lastValidation ? (
        <>
          <Card className={styles.errorCard}>
            <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}>
              <Warning24Regular
                style={{ fontSize: '32px', color: tokens.colorPaletteRedForeground1 }}
              />
              <div>
                <Title2>Validation Failed</Title2>
                <Text>
                  Some providers are not available. Please fix the issues below or click Next to continue anyway.
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
                <div className={styles.fixActionsContainer}>
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

  const renderStep6 = () => {
    const validApiKeys = Object.entries(state.apiKeyValidationStatus)
      .filter(([_, status]) => status === 'valid')
      .map(([provider]) => provider);

    const installedComponents = state.installItems
      .filter((item) => item.installed)
      .map((item) => item.name);

    return (
      <CompletionStep
        summary={{
          tier: state.selectedTier || 'free',
          apiKeysConfigured: validApiKeys,
          hardwareDetected: !!state.hardware,
          componentsInstalled: installedComponents,
        }}
        onCreateFirstVideo={completeOnboarding}
        onExploreApp={() => {
          clearWizardStateFromStorage();
          localStorage.setItem('hasSeenOnboarding', 'true');
          navigate('/');
        }}
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
      default:
        return null;
    }
  };

  const buttonLabel = state.step === 5 ? (
    state.status === 'idle' || state.status === 'installed' ? 'Validate' : 
    state.status === 'invalid' ? 'Next Anyway' : 'Next'
  ) : 'Next';
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

      <div className={styles.content}>{renderStepContent()}</div>

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
