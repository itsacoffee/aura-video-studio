import { useEffect, useReducer } from 'react';
import {
  makeStyles,
  tokens,
  Title1,
  Title2,
  Title3,
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
  Play24Regular,
  Settings24Regular,
  VideoClip24Regular,
  Warning24Regular,
} from '@fluentui/react-icons';
import { useNavigate } from 'react-router-dom';
import {
  onboardingReducer,
  initialOnboardingState,
  runValidationThunk,
  detectHardwareThunk,
  installItemThunk,
  getButtonLabel,
  isButtonDisabled,
} from '../../state/onboarding';
import type { FixAction } from '../../state/providers';
import { InstallItemCard } from '../../components/Onboarding/InstallItemCard';
import { FileLocationsSummary } from '../../components/Onboarding/FileLocationsSummary';
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
  },
  footer: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: tokens.spacingVerticalXXL,
    paddingTop: tokens.spacingVerticalL,
    borderTop: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  steps: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    justifyContent: 'center',
    marginBottom: tokens.spacingVerticalL,
  },
  step: {
    width: '60px',
    height: '4px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: '2px',
  },
  stepActive: {
    backgroundColor: tokens.colorBrandBackground,
  },
  stepCompleted: {
    backgroundColor: tokens.colorPaletteGreenBackground2,
  },
  modeCard: {
    cursor: 'pointer',
    padding: tokens.spacingVerticalL,
    transition: 'all 0.2s',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
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
  successCard: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
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
  fixActionCard: {
    padding: tokens.spacingVerticalM,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
});

export function FirstRunWizard() {
  const styles = useStyles();
  const navigate = useNavigate();
  const [state, dispatch] = useReducer(onboardingReducer, initialOnboardingState);
  const { attachEngine } = useEnginesStore();

  const totalSteps = 4;

  useEffect(() => {
    // Check if this is truly first run
    const hasSeenOnboarding = localStorage.getItem('hasSeenOnboarding');
    if (hasSeenOnboarding === 'true') {
      // User has already seen onboarding, redirect to home
      navigate('/');
    }
  }, [navigate]);

  // Auto-advance to next step when validation succeeds
  useEffect(() => {
    if (state.status === 'valid' && state.step === 3) {
      // Validation passed on final step, mark as ready
      dispatch({ type: 'MARK_READY' });
    }
  }, [state.status, state.step]);

  const handleNext = async () => {
    if (state.step === 1 && !state.hardware) {
      // Detect hardware before moving to step 2
      await detectHardwareThunk(dispatch);
    }

    if (state.step === 2) {
      // Install required items
      const requiredItems = state.installItems.filter(item => item.required && !item.installed);
      for (const item of requiredItems) {
        await installItemThunk(item.id, dispatch);
      }
    }

    if (state.step === 3) {
      // Run validation only if not already valid
      if (state.status === 'idle' || state.status === 'installed') {
        await runValidationThunk(state, dispatch);
        return; // Don't advance yet, wait for validation result
      } else if (state.status === 'valid' || state.status === 'ready') {
        // Already validated, complete onboarding
        completeOnboarding();
        return;
      } else if (state.status === 'invalid') {
        // Show fix actions, don't advance
        return;
      }
    }

    if (state.step < totalSteps - 1) {
      dispatch({ type: 'SET_STEP', payload: state.step + 1 });
    }
  };

  const handleBack = () => {
    if (state.step > 0) {
      dispatch({ type: 'SET_STEP', payload: state.step - 1 });
      // Reset validation when going back
      if (state.step === 3) {
        dispatch({ type: 'RESET_VALIDATION' });
      }
    }
  };

  const handleSkip = () => {
    localStorage.setItem('hasSeenOnboarding', 'true');
    navigate('/');
  };

  const completeOnboarding = () => {
    localStorage.setItem('hasSeenOnboarding', 'true');
    navigate('/create');
  };

  const handleFixAction = (action: FixAction) => {
    switch (action.type) {
      case 'Install':
        // Navigate to downloads page with the item pre-selected
        navigate(`/downloads?item=${action.parameter}`);
        break;
      case 'Start':
        // Show instructions for starting the service
        alert(`To start ${action.parameter}, please follow these steps:\n\n${action.description}`);
        break;
      case 'OpenSettings':
        // Navigate to settings with the specific tab
        navigate(`/settings?tab=${action.parameter}`);
        break;
      case 'SwitchToFree':
        // Switch to free alternative
        dispatch({ type: 'RESET_VALIDATION' });
        alert(`Switched to ${action.parameter}. Click Validate again to check.`);
        break;
      case 'Help':
        // Open help URL
        if (action.parameter) {
          window.open(action.parameter, '_blank');
        }
        break;
    }
  };

  const handleAttachExisting = async (itemId: string, installPath: string, executablePath?: string) => {
    try {
      // Call attach engine API
      await attachEngine({
        engineId: itemId,
        installPath,
        executablePath,
      });
      
      // Mark item as installed in state
      dispatch({ type: 'INSTALL_COMPLETE', payload: itemId });
    } catch (error) {
      console.error(`Failed to attach ${itemId}:`, error);
      dispatch({ 
        type: 'INSTALL_FAILED', 
        payload: { 
          itemId, 
          error: error instanceof Error ? error.message : 'Failed to attach existing installation' 
        } 
      });
      throw error;
    }
  };

  const handleSkipItem = (itemId: string) => {
    // Mark item as skipped (treat as installed for progression)
    dispatch({ type: 'INSTALL_COMPLETE', payload: itemId });
  };

  const renderStep0 = () => (
    <>
      <Title2>Welcome to Aura Video Studio!</Title2>
      <Text>Let's get you set up in just a few steps. Choose your preferred mode:</Text>
      
      <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalM }}>
        <Card
          className={styles.modeCard}
          onClick={() => dispatch({ type: 'SET_MODE', payload: 'free' })}
          style={state.mode === 'free' ? { 
            borderColor: tokens.colorBrandBackground, 
            borderWidth: '2px',
            borderStyle: 'solid'
          } : {}}
        >
          <Title3>🆓 Free-Only Mode</Title3>
          <Text>
            Uses free, always-available providers:
            <ul>
              <li>Rule-based script generation</li>
              <li>Windows built-in text-to-speech</li>
              <li>Stock images from Pexels/Unsplash</li>
            </ul>
            Best for: Getting started quickly with zero setup
          </Text>
        </Card>

        <Card
          className={styles.modeCard}
          onClick={() => dispatch({ type: 'SET_MODE', payload: 'local' })}
          style={state.mode === 'local' ? { 
            borderColor: tokens.colorBrandBackground, 
            borderWidth: '2px',
            borderStyle: 'solid'
          } : {}}
        >
          <Title3>💻 Local Mode</Title3>
          <Text>
            Uses local AI engines for privacy and offline work:
            <ul>
              <li>Ollama for script generation</li>
              <li>Local Piper/Mimic3 TTS</li>
              <li>Stable Diffusion for visuals (requires NVIDIA GPU)</li>
            </ul>
            Best for: Privacy-conscious users with capable hardware
          </Text>
        </Card>

        <Card
          className={styles.modeCard}
          onClick={() => dispatch({ type: 'SET_MODE', payload: 'pro' })}
          style={state.mode === 'pro' ? { 
            borderColor: tokens.colorBrandBackground, 
            borderWidth: '2px',
            borderStyle: 'solid'
          } : {}}
        >
          <Title3>⭐ Pro Mode</Title3>
          <Text>
            Uses premium cloud APIs for best quality:
            <ul>
              <li>OpenAI GPT-4 for scripts</li>
              <li>ElevenLabs for voices</li>
              <li>Stability AI/Runway for visuals</li>
            </ul>
            Best for: Professional content creators (requires API keys)
          </Text>
        </Card>
      </div>
    </>
  );

  const renderStep1 = () => (
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
            <Title3>System Information</Title3>
            {state.hardware.gpu && <Text>GPU: {state.hardware.gpu}</Text>}
            {state.hardware.vram && <Text>VRAM: {state.hardware.vram}GB</Text>}
            <Text style={{ marginTop: tokens.spacingVerticalM }}>
              <strong>Recommendation:</strong> {state.hardware.recommendation}
            </Text>
          </Card>

          {!state.hardware.canRunSD && state.mode === 'local' && (
            <Card>
              <Badge appearance="filled" color="warning">⚠ Note</Badge>
              <Text style={{ marginTop: tokens.spacingVerticalS }}>
                Your system doesn't meet the requirements for local Stable Diffusion. 
                We'll use Stock images as a fallback, or you can add cloud Pro providers later.
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

  const renderStep2 = () => (
    <>
      <Title2>Install Required Components</Title2>
      <Text>We'll help you install the necessary tools for your chosen mode.</Text>

      <div className={styles.installList}>
        {state.installItems.map(item => (
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

      <Card>
        <Text>
          💡 Tip: You can always install additional engines later from the Downloads page.
        </Text>
      </Card>
    </>
  );

  const renderStep3 = () => (
    <>
      <Title2>Validation & Demo</Title2>
      
      {state.status === 'validating' ? (
        <Card>
          <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}>
            <Spinner size="small" />
            <Text>Running preflight checks...</Text>
          </div>
        </Card>
      ) : state.status === 'valid' || state.status === 'ready' ? (
        <>
          <div className={styles.successCard}>
            <Checkmark24Regular style={{ fontSize: '64px', color: tokens.colorPaletteGreenForeground1 }} />
            <Title1 style={{ marginTop: tokens.spacingVerticalL }}>All Set!</Title1>
            <Text style={{ marginTop: tokens.spacingVerticalM }}>
              Your system is ready to create amazing videos. Let's create your first project!
            </Text>
            <div style={{ display: 'flex', gap: tokens.spacingHorizontalM, justifyContent: 'center', marginTop: tokens.spacingVerticalXL }}>
              <Button
                appearance="primary"
                size="large"
                icon={<VideoClip24Regular />}
                onClick={completeOnboarding}
              >
                Create My First Video
              </Button>
              <Button
                appearance="secondary"
                size="large"
                icon={<Settings24Regular />}
                onClick={() => {
                  localStorage.setItem('hasSeenOnboarding', 'true');
                  navigate('/settings');
                }}
              >
                Go to Settings
              </Button>
            </div>
          </div>
          
          {/* Show file locations summary */}
          <FileLocationsSummary />
        </>
      ) : state.status === 'invalid' && state.lastValidation ? (
        <>
          <Card className={styles.errorCard}>
            <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}>
              <Warning24Regular style={{ fontSize: '32px', color: tokens.colorPaletteRedForeground1 }} />
              <div>
                <Title3>Validation Failed</Title3>
                <Text>Some providers are not available. Please fix the issues below to continue.</Text>
              </div>
            </div>
          </Card>

          {state.lastValidation.failedStages.map((stage, index) => (
            <Card key={index}>
              <Title3>{stage.stage} Stage</Title3>
              <Text><strong>Provider:</strong> {stage.provider}</Text>
              <Text><strong>Issue:</strong> {stage.message}</Text>
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
                      <li key={i}><Text size={200}>{suggestion}</Text></li>
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

          <Card>
            <Text>After fixing the issues, click Validate again to re-check your setup.</Text>
          </Card>
        </>
      ) : (
        <Card>
          <Text>Click Validate to check your setup and ensure all providers are working correctly.</Text>
        </Card>
      )}
    </>
  );

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
      default:
        return null;
    }
  };

  const isLastStep = state.step === totalSteps - 1;
  const buttonLabel = getButtonLabel(state.status, isLastStep);
  const buttonDisabled = isButtonDisabled(state.status, state.isDetectingHardware);

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title1>First-Run Setup</Title1>
        <div className={styles.steps}>
          {Array.from({ length: totalSteps }).map((_, i) => (
            <div
              key={i}
              className={`${styles.step} ${i === state.step ? styles.stepActive : ''} ${i < state.step ? styles.stepCompleted : ''}`}
            />
          ))}
        </div>
      </div>

      <div className={styles.content}>
        {renderStepContent()}
      </div>

      {state.status !== 'ready' && (
        <div className={styles.footer}>
          <Button
            appearance="subtle"
            onClick={handleSkip}
          >
            Skip Setup
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
                ) : isLastStep ? (
                  <Play24Regular />
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
