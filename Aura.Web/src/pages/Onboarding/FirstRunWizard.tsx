import {
  makeStyles,
  tokens,
  Title2,
  Title3,
  Text,
  Button,
  Card,
  Spinner,
  Field,
  Input,
} from '@fluentui/react-components';
import {
  Checkmark24Regular,
  ChevronRight24Regular,
  ChevronLeft24Regular,
  Warning24Regular,
  ArrowClockwise24Regular,
  FolderOpen24Regular,
} from '@fluentui/react-icons';
import { useCallback, useEffect, useMemo, useReducer, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useNotifications } from '../../components/Notifications/Toasts';
import { AutoSaveIndicator } from '../../components/Onboarding/AutoSaveIndicator';
import type { AutoSaveStatus } from '../../components/Onboarding/AutoSaveIndicator';
import { BackendStatusBanner } from '../../components/Onboarding/BackendStatusBanner';
import { FFmpegDependencyCard } from '../../components/Onboarding/FFmpegDependencyCard';
import { ResumeWizardDialog } from '../../components/Onboarding/ResumeWizardDialog';
import { WelcomeScreen } from '../../components/Onboarding/WelcomeScreen';
import type { WorkspacePreferences } from '../../components/Onboarding/WorkspaceSetup';
import { WorkspaceSetup } from '../../components/Onboarding/WorkspaceSetup';
import { WizardProgress } from '../../components/WizardProgress';
import { wizardAnalytics } from '../../services/analytics';
import { resetCircuitBreaker } from '../../services/api/apiClient';
import { PersistentCircuitBreaker } from '../../services/api/circuitBreakerPersistence';
import type { FFmpegStatus } from '../../services/api/ffmpegClient';
import { ffmpegClient } from '../../services/api/ffmpegClient';
import { setupApi } from '../../services/api/setupApi';
import type { WizardStatusResponse } from '../../services/api/setupApi';
import { markFirstRunCompleted } from '../../services/firstRunService';
import {
  onboardingReducer,
  initialOnboardingState,
  validateApiKeyThunk,
  saveWizardStateToStorage,
  loadWizardStateFromStorage,
  clearWizardStateFromStorage,
  loadWizardProgressFromBackend,
  saveWizardProgressToBackend,
  completeWizardInBackend,
} from '../../state/onboarding';
import { pickFolder } from '../../utils/pathUtils';
import { ApiKeySetupStep } from './ApiKeySetupStep';

/**
 * FirstRunWizard - The primary onboarding wizard for new users
 *
 * This is the ONLY setup wizard that should be used. It provides a streamlined
 * 6-step mandatory setup process:
 *
 * Step 0: Welcome - Introduction to Aura Video Studio
 * Step 1: FFmpeg Check - Quick detection of existing FFmpeg installation
 * Step 2: FFmpeg Install - Guided installation or manual configuration
 * Step 3: Provider Configuration - Set up at least one LLM provider (or use offline mode)
 * Step 4: Workspace Setup - Configure default save locations
 * Step 5: Complete - Summary and transition to main app
 *
 * Key Features:
 * - Circuit breaker state is cleared on mount to prevent false "backend not running" errors
 * - Auto-save progress to backend and localStorage for resume capability
 * - Resume dialog shows if user has incomplete setup from previous session
 * - Backend status banner shows only when backend is actually unreachable
 * - Graceful shutdown with proper FFmpeg process cleanup
 *
 * NOTE: SetupWizard.tsx has been removed as it was an old/unused implementation
 * that caused confusion. FirstRunWizard is the canonical implementation.
 */

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    padding: `${tokens.spacingVerticalL} ${tokens.spacingHorizontalXL}`,
    maxWidth: '900px',
    margin: '0 auto',
  },
  header: {
    textAlign: 'center',
    marginBottom: tokens.spacingVerticalL,
  },
  content: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    overflow: 'auto',
    paddingBottom: tokens.spacingVerticalM,
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
    marginTop: tokens.spacingVerticalL,
    paddingTop: tokens.spacingVerticalM,
    borderTop: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  hardwareInfo: {
    display: 'flex',
    flexDirection: 'column' as const,
    gap: tokens.spacingVerticalS,
  },
  installList: {
    display: 'flex',
    flexDirection: 'column' as const,
    gap: tokens.spacingVerticalXS,
  },
  validationItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalS,
  },
  errorCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteRedBackground1,
  },
  fixActionsContainer: {
    display: 'flex',
    flexDirection: 'column' as const,
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalM,
  },
  manualAttachCard: {
    display: 'flex',
    flexDirection: 'column' as const,
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
  },
  manualHeader: {
    display: 'flex',
    flexDirection: 'column' as const,
    gap: tokens.spacingVerticalXXS,
  },
  pathInputRow: {
    display: 'flex',
    flexDirection: 'row' as const,
    gap: tokens.spacingHorizontalS,
    alignItems: 'center',
  },
  manualActions: {
    display: 'flex',
    flexDirection: 'row' as const,
    gap: tokens.spacingHorizontalS,
    flexWrap: 'wrap' as const,
  },
  statusSummary: {
    display: 'flex',
    flexDirection: 'column' as const,
    gap: tokens.spacingVerticalXXS,
  },
});

export interface FirstRunWizardProps {
  onComplete?: () => void | Promise<void>;
}

export function FirstRunWizard({ onComplete }: FirstRunWizardProps = {}) {
  const styles = useStyles();
  const navigate = useNavigate();
  const [state, dispatch] = useReducer(onboardingReducer, initialOnboardingState);
  const [stepStartTime, setStepStartTime] = useState<number>(0);

  // Auto-save state
  const [autoSaveStatus, setAutoSaveStatus] = useState<AutoSaveStatus>('idle');
  const [lastSaved, setLastSaved] = useState<Date | null>(null);
  const [autoSaveError, setAutoSaveError] = useState<string | null>(null);

  // Resume dialog state
  const [showResumeDialog, setShowResumeDialog] = useState(false);
  const [wizardStatus, setWizardStatus] = useState<WizardStatusResponse | null>(null);

  const wizardStartTimeRef = useRef<number>(0);

  // FFmpeg status state
  const [ffmpegReady, setFfmpegReady] = useState(false);
  const [ffmpegPath, setFfmpegPath] = useState<string | null>(null);
  const [ffmpegPathInput, setFfmpegPathInput] = useState('');
  const [isBrowsingForFfmpeg, setIsBrowsingForFfmpeg] = useState(false);
  const [isValidatingFfmpegPath, setIsValidatingFfmpegPath] = useState(false);
  const [isRescanningFfmpeg, setIsRescanningFfmpeg] = useState(false);
  const [ffmpegRefreshSignal, setFfmpegRefreshSignal] = useState(0);
  const pendingRescanRef = useRef(false);
  const [ffmpegManualOverride, setFfmpegManualOverride] = useState(false);
  const defaultFfmpegPlaceholder = useMemo(() => {
    if (typeof navigator === 'undefined' || !navigator.platform) {
      return '/usr/bin/ffmpeg';
    }

    const platform = navigator.platform.toLowerCase();
    if (platform.includes('win')) {
      return 'C:\\Program Files\\ffmpeg\\bin\\ffmpeg.exe';
    }
    if (platform.includes('mac')) {
      return '/opt/homebrew/bin/ffmpeg';
    }
    return '/usr/bin/ffmpeg';
  }, []);

  // Provider validation state
  const [hasAtLeastOneProvider, setHasAtLeastOneProvider] = useState(false);
  const [allowInvalidKeys, setAllowInvalidKeys] = useState(false);

  // Completion state
  const [isCompletingSetup, setIsCompletingSetup] = useState(false);
  const [completionErrors, setCompletionErrors] = useState<string[]>([]);

  // Notifications hook
  const { showSuccessToast, showFailureToast } = useNotifications();

  // Simplified mandatory setup flow - 6 core steps
  const totalSteps = 6;
  const stepLabels = [
    'Welcome',
    'FFmpeg Check',
    'FFmpeg Install',
    'Provider Configuration',
    'Workspace Setup',
    'Complete',
  ];

  useEffect(() => {
    // Initialize timestamps client-side only (avoid hydration mismatches)
    const now = Date.now();
    setStepStartTime(now);
    wizardStartTimeRef.current = now;

    // Track wizard start
    wizardAnalytics.started();

    // CRITICAL FIX: Clear all circuit breaker state on wizard mount
    // This prevents false "service unavailable" errors from persisted circuit breaker state
    PersistentCircuitBreaker.clearState();
    resetCircuitBreaker();
    console.info('[FirstRunWizard] Circuit breaker state cleared on mount');

    // Check for saved progress from backend
    const checkSavedProgress = async () => {
      try {
        const status = await setupApi.getWizardStatus();
        if (status.canResume) {
          setWizardStatus(status);
          setShowResumeDialog(true);
        }
      } catch (error) {
        console.warn('[FirstRunWizard] Failed to check saved wizard status:', error);

        // Fallback to localStorage
        const savedState = loadWizardStateFromStorage();
        if (savedState) {
          const resume = window.confirm(
            'You have incomplete setup. Would you like to resume where you left off?'
          );
          if (resume && savedState) {
            dispatch({ type: 'LOAD_FROM_STORAGE', payload: savedState });
          } else {
            clearWizardStateFromStorage();
          }
        }
      }
    };

    void checkSavedProgress();
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

    // CRITICAL FIX: Ping backend with retry before FFmpeg check in Step 2
    // This ensures backend is reachable before attempting FFmpeg detection
    if (state.step === 2) {
      console.info('[FirstRunWizard] Entering Step 2, pinging backend with retry');
      const checkBackendAndFFmpeg = async () => {
        for (let i = 0; i < 3; i++) {
          const ping = await setupApi.pingBackend();
          if (ping.ok) {
            console.info('[FirstRunWizard] Backend is reachable, triggering FFmpeg check');
            setFfmpegRefreshSignal((prev) => prev + 1);
            return;
          }
          console.warn(`[FirstRunWizard] Backend ping attempt ${i + 1}/3 failed: ${ping.details}`);
          await new Promise((resolve) => setTimeout(resolve, 1000 * (i + 1)));
        }
        console.error('[FirstRunWizard] Backend not reachable after 3 attempts');
      };
      void checkBackendAndFFmpeg();
    }
  }, [state.step]); // eslint-disable-line react-hooks/exhaustive-deps

  // Save progress on state changes
  useEffect(() => {
    if (state.step > 0 && state.step < totalSteps - 1) {
      saveWizardStateToStorage(state);

      // Auto-save to backend
      const autoSave = async () => {
        setAutoSaveStatus('saving');
        setAutoSaveError(null);

        try {
          const success = await saveWizardProgressToBackend(state);
          if (success) {
            setAutoSaveStatus('saved');
            setLastSaved(new Date());
            setTimeout(() => setAutoSaveStatus('idle'), 3000);
          } else {
            setAutoSaveStatus('error');
            setAutoSaveError('Failed to save progress');
          }
        } catch (error) {
          console.error('[Auto-save] Failed:', error);
          setAutoSaveStatus('error');
          setAutoSaveError(error instanceof Error ? error.message : 'Unknown error');
        }
      };

      void autoSave();
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

  // Resume wizard handlers
  const handleResumeWizard = useCallback(async () => {
    try {
      const savedState = await loadWizardProgressFromBackend();
      if (savedState) {
        dispatch({ type: 'LOAD_FROM_STORAGE', payload: savedState });
        setShowResumeDialog(false);
        showSuccessToast({
          title: 'Setup Resumed',
          message: 'Your previous setup progress has been restored.',
        });
      }
    } catch (error) {
      console.error('[Resume] Failed to load saved state:', error);
      showFailureToast({
        title: 'Resume Failed',
        message: 'Failed to resume saved progress. Starting fresh.',
      });
      setShowResumeDialog(false);
    }
  }, [showSuccessToast, showFailureToast]);

  const handleStartFresh = useCallback(async () => {
    // Clear localStorage
    clearWizardStateFromStorage();

    // Also reset backend wizard state
    try {
      const { resetWizardInBackend } = await import('../../state/onboarding');
      await resetWizardInBackend(false); // Don't preserve data
      console.info('[FirstRunWizard] Wizard state reset in backend and localStorage');
    } catch (error) {
      console.warn('[FirstRunWizard] Failed to reset wizard state in backend:', error);
      // Continue anyway - localStorage is cleared which is most important
    }

    setShowResumeDialog(false);
    showSuccessToast({
      title: 'Starting Fresh',
      message: 'Previous setup progress cleared. Starting wizard from the beginning.',
    });
  }, [showSuccessToast]);

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

    // Step 1: FFmpeg Check -> Step 2: FFmpeg Install
    if (state.step === 1) {
      dispatch({ type: 'SET_STEP', payload: 2 });
      return;
    }

    // Step 2: FFmpeg Install -> Step 3: Provider Configuration
    if (state.step === 2) {
      // Allow proceeding even without FFmpeg (user can skip)
      // The warning is already shown in the UI
      dispatch({ type: 'SET_STEP', payload: 3 });
      return;
    }

    // Step 3: Provider Configuration -> Step 4: Workspace Setup
    if (state.step === 3) {
      // Check if there are any configured (non-empty) API keys
      const configuredKeys = Object.entries(state.apiKeys).filter(
        ([_, key]) => key && key.trim().length > 0
      );
      const hasInvalidKeys = configuredKeys.some(
        ([provider, _]) => state.apiKeyValidationStatus[provider] === 'invalid'
      );

      // Must have at least one provider configured OR allow invalid keys checkbox must be checked
      const canProceed =
        hasAtLeastOneProvider || (configuredKeys.length > 0 && allowInvalidKeys && hasInvalidKeys);

      if (!canProceed && !hasAtLeastOneProvider) {
        showFailureToast({
          title: 'Provider Required',
          message: 'Please configure at least one LLM provider or choose offline mode to continue.',
        });
        return;
      }

      if (!canProceed && hasInvalidKeys && !allowInvalidKeys) {
        showFailureToast({
          title: 'Invalid API Keys',
          message:
            'Some API keys are invalid. Please validate them or check "Allow me to continue with invalid API keys" to proceed.',
        });
        return;
      }

      dispatch({ type: 'SET_STEP', payload: 4 });
      return;
    }

    // Step 4: Workspace Setup -> Step 5: Complete
    if (state.step === 4) {
      // Validate workspace is configured
      if (!state.workspacePreferences?.defaultSaveLocation) {
        showFailureToast({
          title: 'Workspace Required',
          message: 'Please configure your workspace location to continue.',
        });
        return;
      }
      dispatch({ type: 'SET_STEP', payload: 5 });
      return;
    }

    // Step 5: Completion - handled by completion step buttons
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
    if (isCompletingSetup) {
      return; // Prevent double-clicks
    }

    // Clear any previous errors
    setCompletionErrors([]);

    // Validate setup and show warnings if needed
    const warnings: string[] = [];

    if (!ffmpegReady) {
      warnings.push('FFmpeg not detected - video rendering will not work until you install it');
    }

    const validApiKeys = Object.entries(state.apiKeyValidationStatus)
      .filter(([_, status]) => status === 'valid')
      .map(([provider]) => provider);

    if (validApiKeys.length === 0 && !state.apiKeyValidationStatus['ollama']) {
      warnings.push(
        'No LLM provider configured - script generation will use basic rule-based fallback'
      );
    }

    if (!state.workspacePreferences?.defaultSaveLocation) {
      warnings.push('Workspace location not configured - videos will be saved to default location');
    }

    // Show warnings dialog if there are any
    if (warnings.length > 0) {
      const proceed = window.confirm(
        'Setup has some warnings:\n\n' +
          warnings.map((w, i) => `${i + 1}. ${w}`).join('\n') +
          '\n\nDo you want to complete setup anyway?'
      );

      if (!proceed) {
        return;
      }
    }

    setIsCompletingSetup(true);
    try {
      console.info('[FirstRunWizard] Starting onboarding completion', {
        ffmpegPath,
        workspaceLocation: state.workspacePreferences?.defaultSaveLocation,
      });

      // Call backend API to complete setup and persist to database
      const setupResult = await setupApi.completeSetup({
        ffmpegPath: ffmpegPath,
        outputDirectory: state.workspacePreferences?.defaultSaveLocation,
      });

      console.info('[FirstRunWizard] Setup API response:', setupResult);

      if (!setupResult.success) {
        // Show errors inline on the page
        const errors = setupResult.errors || [
          'Setup validation failed. Please check your configuration.',
        ];
        setCompletionErrors(errors);

        console.error('[FirstRunWizard] Setup validation failed:', {
          errors,
          correlationId: setupResult.correlationId,
        });

        showFailureToast({
          title: 'Setup Validation Failed',
          message: errors.join('; '),
        });

        // Don't proceed with completion
        return;
      }

      // Mark wizard as complete in backend
      await completeWizardInBackend(state);

      // Clear wizard state and mark local completion
      clearWizardStateFromStorage();
      await markFirstRunCompleted();

      console.info('[FirstRunWizard] First run marked as completed');

      // Track completion
      const totalTime = (Date.now() - wizardStartTimeRef.current) / 1000;
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

      console.info('[FirstRunWizard] Navigating to completion destination');

      // Call the onComplete callback if provided
      if (onComplete) {
        await onComplete();
      } else {
        // Fallback to navigation
        navigate('/');
      }
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      console.error('[FirstRunWizard] Error completing setup:', errorObj);

      setCompletionErrors([`Failed to complete setup: ${errorObj.message}`]);

      showFailureToast({
        title: 'Setup Error',
        message: `Failed to complete setup: ${errorObj.message}. Please try again or exit to the main app.`,
      });
    } finally {
      setIsCompletingSetup(false);
    }
  };

  const handleExitWizard = async () => {
    const confirmed = window.confirm(
      'Are you sure you want to exit the setup wizard?\n\n' +
        'You can complete setup later from the Settings page.'
    );

    if (confirmed) {
      console.info('[FirstRunWizard] User confirmed exit from wizard');
      // Save current progress
      try {
        await saveWizardProgressToBackend(state);
        localStorage.setItem('aura-setup-aborted', 'true');
        localStorage.setItem('aura-setup-aborted-step', state.step.toString());
      } catch (error: unknown) {
        const errorObj = error instanceof Error ? error : new Error(String(error));
        console.warn('[FirstRunWizard] Failed to save progress on exit:', errorObj);
      }

      // Navigate to main app
      if (onComplete) {
        await onComplete();
      } else {
        navigate('/');
      }
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

  const handleLocalProviderReady = useCallback(
    (provider: string) => {
      dispatch({
        type: 'API_KEY_VALID',
        payload: {
          provider,
          accountInfo:
            provider === 'ollama'
              ? 'Ollama marked as ready. Ensure the Ollama service is running before generating scripts.'
              : 'Local provider marked as ready.',
          fieldErrors: [],
        },
      });

      showSuccessToast({
        title: 'Provider Ready',
        message:
          provider === 'ollama'
            ? 'Ollama will be used whenever it is running locally.'
            : 'Local provider is now marked as ready.',
      });
    },
    [dispatch, showSuccessToast]
  );

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

  const handleFfmpegStatusUpdate = useCallback(
    (status: FFmpegStatus | null) => {
      const isReady = Boolean(status?.installed && status?.valid);
      if (isReady) {
        setFfmpegManualOverride(false);
        setFfmpegReady(true);
        dispatch({ type: 'INSTALL_COMPLETE', payload: 'ffmpeg' });
      } else if (!ffmpegManualOverride) {
        setFfmpegReady(false);
      }

      if (isReady && status?.path) {
        setFfmpegPath((previousPath) => {
          if (status.path && previousPath !== status.path) {
            setFfmpegPathInput((currentInput) =>
              currentInput.trim().length === 0 || currentInput === previousPath
                ? status.path!
                : currentInput
            );
          }
          return status.path;
        });
      }

      if (pendingRescanRef.current) {
        pendingRescanRef.current = false;
        setIsRescanningFfmpeg(false);

        if (isReady) {
          showSuccessToast({
            title: 'FFmpeg Detected',
            message: `FFmpeg is ready${status?.path ? ` at ${status.path}` : ''}.`,
          });
        } else {
          showFailureToast({
            title: 'FFmpeg Not Found',
            message:
              status?.error ||
              'Aura could not detect FFmpeg. Provide the executable path below or install the managed version.',
          });
        }
      }
    },
    [dispatch, ffmpegManualOverride, showFailureToast, showSuccessToast]
  );

  const handleRescanFfmpeg = () => {
    if (isRescanningFfmpeg) {
      return;
    }
    // Reset circuit breaker before rescanning
    resetCircuitBreaker();
    console.info('[Rescan FFmpeg] Circuit breaker reset, initiating rescan');
    pendingRescanRef.current = true;
    setIsRescanningFfmpeg(true);
    setFfmpegRefreshSignal((prev) => prev + 1);
  };

  const openExternalLink = useCallback((url: string) => {
    if (typeof window === 'undefined') {
      return;
    }
    window.open(url, '_blank', 'noopener,noreferrer');
  }, []);

  /**
   * Normalize a path to point to FFmpeg executable
   * Converts folder paths to executable paths based on platform conventions
   */
  const normalizeFfmpegPath = (path: string): string => {
    let normalized = path.trim().replace(/[\\/]+$/, '');
    const usesWindowsSeparators = normalized.includes('\\') && !normalized.includes('/');
    const separator = usesWindowsSeparators ? '\\' : '/';
    const executableName = usesWindowsSeparators ? 'ffmpeg.exe' : 'ffmpeg';
    const lower = normalized.toLowerCase();

    // Path already points to executable
    if (lower.endsWith(executableName)) {
      return normalized;
    }

    // Path points to bin folder
    if (lower.endsWith(`${separator}bin`.toLowerCase())) {
      return `${normalized}${separator}${executableName}`;
    }

    // Path points to ffmpeg folder
    if (lower.endsWith(`${separator}ffmpeg`)) {
      return `${normalized}${separator}bin${separator}${executableName}`;
    }

    // Assume path is a folder, append executable name
    return `${normalized}${separator}${executableName}`;
  };

  /**
   * Attempt to select FFmpeg path using available pickers
   */
  const selectFfmpegPath = async (): Promise<string | null> => {
    const isWindows = navigator.userAgent.toLowerCase().includes('windows');
    const ffmpegFilters = isWindows
      ? [{ name: 'FFmpeg executable', extensions: ['exe'] }]
      : [{ name: 'FFmpeg executable', extensions: ['*'] }];

    // Try Electron file picker first
    if (window.aura?.dialogs?.openFile) {
      try {
        const filePath = await window.aura.dialogs.openFile({
          title: 'Select FFmpeg Executable',
          filters: ffmpegFilters,
        });
        if (filePath) return filePath;
      } catch (error: unknown) {
        console.warn('Electron file picker failed:', error);
      }
    }

    // Fallback to folder picker
    try {
      return await pickFolder();
    } catch (error: unknown) {
      console.warn('Folder picker failed:', error);
      return null;
    }
  };

  const handleBrowseForFfmpeg = useCallback(async () => {
    setIsBrowsingForFfmpeg(true);
    try {
      const selectedPath = await selectFfmpegPath();

      if (!selectedPath) {
        return;
      }

      // Validate path is not empty after trimming
      const trimmedPath = selectedPath.trim();
      if (trimmedPath.length === 0) {
        showFailureToast({
          title: 'Invalid Path',
          message: 'The selected path is empty. Please select a valid FFmpeg executable or folder.',
        });
        return;
      }

      // Normalize path to point to executable
      const normalizedPath = normalizeFfmpegPath(trimmedPath);

      // Reset state and populate input for validation
      setFfmpegReady(false);
      setFfmpegManualOverride(false);
      setFfmpegPathInput(normalizedPath);

      console.info('[Browse FFmpeg] Selected path:', normalizedPath);
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error';
      console.error('Failed to browse for FFmpeg:', error);
      showFailureToast({
        title: 'Browse Failed',
        message: `Unable to open the system file picker: ${errorMessage}. Enter the FFmpeg path manually instead.`,
      });
    } finally {
      setIsBrowsingForFfmpeg(false);
    }
  }, [showFailureToast]);

  /**
   * Parse FFmpeg validation error to extract user-friendly messages
   */
  const parseFFmpegValidationError = useCallback(
    (error: unknown): { title: string; message: string } => {
      let errorTitle = 'Validation Error';
      let errorMessage = 'Unexpected error validating FFmpeg path.';

      if (error && typeof error === 'object') {
        const axiosError = error as {
          code?: string;
          response?: {
            data?: {
              message?: string;
              detail?: string;
              error?: string;
              howToFix?: string[];
              title?: string;
            };
            status?: number;
          };
          message?: string;
          request?: unknown;
        };

        // Network-level errors (no response received)
        if (axiosError.code === 'ERR_NETWORK' || axiosError.code === 'ECONNREFUSED') {
          errorTitle = 'Backend Not Running';
          errorMessage =
            'Cannot connect to the Aura backend server. To start the backend:\n\n' +
            '1. Open a terminal in the project root\n' +
            '2. Run: dotnet run --project Aura.Api\n' +
            '3. Wait for "Application started" message\n' +
            '4. Try validating the path again';
        } else if (axiosError.code === 'ECONNABORTED' || axiosError.code === 'ETIMEDOUT') {
          errorTitle = 'Connection Timeout';
          errorMessage =
            'The validation request timed out. The backend may be starting up or overloaded.\n\n' +
            'Wait a moment and try again. If the problem persists, restart the backend.';
        } else if (axiosError.request && !axiosError.response) {
          errorTitle = 'Network Error';
          errorMessage =
            'No response from the backend server. To start the backend:\n\n' +
            '1. Open a terminal in the project root\n' +
            '2. Run: dotnet run --project Aura.Api\n' +
            '3. Wait for "Application started" message\n' +
            '4. Try validating the path again';
        } else if (axiosError.response?.data) {
          const data = axiosError.response.data;
          errorTitle = data.title || 'Validation Failed';
          errorMessage = data.message || data.detail || data.error || errorMessage;

          if (data.howToFix && data.howToFix.length > 0) {
            errorMessage +=
              '\n\nSuggestions:\n' + data.howToFix.map((tip) => `• ${tip}`).join('\n');
          }
        } else if (axiosError.message) {
          errorMessage = axiosError.message;
        }
      } else if (error instanceof Error) {
        errorMessage = error.message;
      }

      return { title: errorTitle, message: errorMessage };
    },
    []
  );

  const handleValidateFfmpegPath = useCallback(async () => {
    const trimmedPath = ffmpegPathInput.trim();
    if (trimmedPath.length === 0) {
      showFailureToast({
        title: 'Path Required',
        message: 'Enter the full path to your FFmpeg executable before validating.',
      });
      return;
    }

    setIsValidatingFfmpegPath(true);
    try {
      resetCircuitBreaker();
      console.info('[Validate FFmpeg] Circuit breaker reset, attempting validation');

      const result = await ffmpegClient.useExisting({ path: trimmedPath });

      if (result.success && result.installed && result.valid) {
        const resolvedPath = result.path ?? trimmedPath;
        setFfmpegReady(true);
        setFfmpegPath(resolvedPath);
        setFfmpegPathInput(resolvedPath);
        setFfmpegManualOverride(true);
        dispatch({ type: 'INSTALL_COMPLETE', payload: 'ffmpeg' });
        showSuccessToast({
          title: 'FFmpeg Attached',
          message: result.message || `Aura will use FFmpeg at ${resolvedPath}.`,
        });
        setFfmpegRefreshSignal((prev) => prev + 1);
      } else {
        setFfmpegReady(false);
        showFailureToast({
          title: 'Invalid FFmpeg Path',
          message:
            result.message ||
            'The selected location does not contain a valid FFmpeg executable. Select the binary and try again.',
        });
      }
    } catch (error: unknown) {
      console.error('Failed to validate FFmpeg path:', error);
      const { title, message } = parseFFmpegValidationError(error);
      setFfmpegReady(false);
      showFailureToast({ title, message });
    } finally {
      setIsValidatingFfmpegPath(false);
    }
  }, [dispatch, ffmpegPathInput, parseFFmpegValidationError, showFailureToast, showSuccessToast]);

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

  // Step 1: FFmpeg Check - Quick status check only, no installation options
  const renderStep1FFmpeg = () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalM }}>
      <div style={{ textAlign: 'center', marginBottom: tokens.spacingVerticalM }}>
        <Title2>Check for Existing FFmpeg</Title2>
        <Text style={{ display: 'block', marginTop: tokens.spacingVerticalS }}>
          Let&apos;s check if FFmpeg is already installed on your system.
        </Text>
        <Card
          style={{
            marginTop: tokens.spacingVerticalM,
            padding: tokens.spacingVerticalS,
            backgroundColor: tokens.colorNeutralBackground3,
          }}
        >
          <Text size={300}>
            <strong>What is FFmpeg?</strong>
          </Text>
          <Text style={{ display: 'block', marginTop: tokens.spacingVerticalXS }}>
            FFmpeg is the industry-standard tool for video processing. Aura uses it to render your
            videos, add transitions, apply effects, and export in various formats. Without FFmpeg,
            video generation cannot proceed.
          </Text>
        </Card>
      </div>

      <Card
        style={{
          padding: tokens.spacingVerticalL,
        }}
      >
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: tokens.spacingHorizontalM,
            marginBottom: tokens.spacingVerticalM,
          }}
        >
          {isRescanningFfmpeg ? (
            <Spinner size="medium" />
          ) : ffmpegReady && ffmpegPath ? (
            <Checkmark24Regular
              style={{
                fontSize: '32px',
                color: tokens.colorPaletteGreenForeground1,
              }}
            />
          ) : (
            <Warning24Regular
              style={{
                fontSize: '32px',
                color: tokens.colorPaletteYellowForeground1,
              }}
            />
          )}
          <div style={{ flex: 1 }}>
            <Title3>FFmpeg Status</Title3>
            {ffmpegReady && ffmpegPath ? (
              <Text style={{ display: 'block', marginTop: tokens.spacingVerticalXS }}>
                ✓ FFmpeg is installed and ready at {ffmpegPath}
              </Text>
            ) : (
              <Text style={{ display: 'block', marginTop: tokens.spacingVerticalXS }}>
                Checking for FFmpeg installation...
              </Text>
            )}
          </div>
          <Button
            appearance="secondary"
            icon={<ArrowClockwise24Regular />}
            onClick={handleRescanFfmpeg}
            disabled={isRescanningFfmpeg}
          >
            {isRescanningFfmpeg ? 'Checking...' : 'Check Again'}
          </Button>
        </div>

        {ffmpegReady && ffmpegPath && (
          <div
            style={{
              padding: tokens.spacingVerticalM,
              backgroundColor: tokens.colorPaletteGreenBackground1,
              borderRadius: tokens.borderRadiusMedium,
              borderLeft: `4px solid ${tokens.colorPaletteGreenBorder1}`,
            }}
          >
            <Text weight="semibold">Great! FFmpeg is ready to use.</Text>
            <Text style={{ display: 'block', marginTop: tokens.spacingVerticalXS }}>
              You can proceed to the next step.
            </Text>
          </div>
        )}

        {!ffmpegReady && !isRescanningFfmpeg && (
          <div
            style={{
              padding: tokens.spacingVerticalM,
              backgroundColor: tokens.colorNeutralBackground3,
              borderRadius: tokens.borderRadiusMedium,
            }}
          >
            <Text weight="semibold">
              <Warning24Regular style={{ marginRight: tokens.spacingHorizontalXS }} />
              FFmpeg Not Detected
            </Text>
            <Text style={{ display: 'block', marginTop: tokens.spacingVerticalXS }}>
              Don&apos;t worry! The next step will guide you through installing FFmpeg or
              configuring an existing installation.
            </Text>
          </div>
        )}
      </Card>
    </div>
  );

  // Step 2: FFmpeg Install - Installation and manual configuration
  const renderStep2FFmpeg = () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalM }}>
      <div style={{ textAlign: 'center', marginBottom: tokens.spacingVerticalM }}>
        <Title2>Install or Configure FFmpeg</Title2>
        <Text style={{ display: 'block', marginTop: tokens.spacingVerticalS }}>
          Choose how you want to set up FFmpeg: managed installation or manual configuration.
        </Text>
      </div>

      {/* Managed Installation Option */}
      <FFmpegDependencyCard
        autoCheck={true}
        autoExpandDetails={true}
        refreshSignal={ffmpegRefreshSignal}
        onInstallComplete={handleFfmpegStatusUpdate}
        onStatusChange={handleFfmpegStatusUpdate}
      />

      {/* Manual Configuration Option - Consolidated without duplicate Re-scan */}
      <Card className={styles.manualAttachCard}>
        <div className={styles.manualHeader}>
          <Title3>Or Use an Existing FFmpeg Installation</Title3>
          <Text size={200}>
            Already have FFmpeg installed? Provide the path to your FFmpeg executable below.
          </Text>
        </div>

        <Field label="FFmpeg executable path">
          <div className={styles.pathInputRow}>
            <Input
              value={ffmpegPathInput}
              onChange={(event) => {
                const value = event.target.value;
                setFfmpegPathInput(value);
                if (ffmpegReady && value.trim() !== (ffmpegPath ?? '')) {
                  setFfmpegReady(false);
                }
                if (ffmpegManualOverride && value.trim() !== (ffmpegPath ?? '')) {
                  setFfmpegManualOverride(false);
                }
              }}
              placeholder={defaultFfmpegPlaceholder}
            />
            <Button
              appearance="secondary"
              icon={<FolderOpen24Regular />}
              onClick={handleBrowseForFfmpeg}
              disabled={isBrowsingForFfmpeg}
            >
              {isBrowsingForFfmpeg ? 'Browsing...' : 'Browse'}
            </Button>
          </div>
        </Field>

        <div className={styles.manualActions}>
          <Button
            appearance="primary"
            onClick={handleValidateFfmpegPath}
            disabled={isValidatingFfmpegPath || ffmpegPathInput.trim().length === 0}
          >
            {isValidatingFfmpegPath ? 'Validating...' : 'Validate Path'}
          </Button>
        </div>

        <div className={styles.statusSummary}>
          {ffmpegReady && ffmpegPath ? (
            <Text
              size={200}
              weight="semibold"
              style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}
            >
              <Checkmark24Regular /> Using FFmpeg at {ffmpegPath}
            </Text>
          ) : (
            <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
              Enter the path to your FFmpeg executable and click Validate Path
            </Text>
          )}
        </div>
      </Card>

      {/* Manual Installation Resources */}
      <Card
        style={{
          padding: tokens.spacingVerticalM,
          display: 'flex',
          flexDirection: 'column',
          gap: tokens.spacingVerticalS,
        }}
      >
        <Title3>Manual Installation Resources</Title3>
        <Text size={200}>
          Need to download FFmpeg? Get it from the official sources below. After installing, return
          here and use the Browse button above to locate your FFmpeg executable.
        </Text>
        <div
          style={{
            display: 'flex',
            flexWrap: 'wrap',
            gap: tokens.spacingHorizontalS,
          }}
        >
          <Button
            appearance="secondary"
            onClick={() => openExternalLink('https://www.gyan.dev/ffmpeg/builds/')}
          >
            Download Windows Build
          </Button>
          <Button
            appearance="secondary"
            onClick={() => openExternalLink('https://ffmpeg.org/download.html')}
          >
            Official FFmpeg Instructions
          </Button>
        </div>
      </Card>

      {/* Warning about proceeding without FFmpeg */}
      {!ffmpegReady && (
        <Card
          style={{
            padding: tokens.spacingVerticalM,
            backgroundColor: tokens.colorPaletteYellowBackground1,
            borderLeft: `4px solid ${tokens.colorPaletteYellowBorder1}`,
          }}
        >
          <Text
            weight="semibold"
            style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}
          >
            <Warning24Regular /> FFmpeg Required for Video Rendering
          </Text>
          <Text style={{ display: 'block', marginTop: tokens.spacingVerticalXS }}>
            You can proceed to the next step without FFmpeg, but video generation will not work
            until it&apos;s properly installed. You can configure it later from Settings if needed.
          </Text>
        </Card>
      )}
    </div>
  );

  // Step 3: Provider Configuration (At least one required)
  const renderStep3Providers = () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalM }}>
      <div style={{ textAlign: 'center', marginBottom: tokens.spacingVerticalM }}>
        <Title2>Provider Configuration</Title2>
        <Text style={{ display: 'block', marginTop: tokens.spacingVerticalS }}>
          Configure at least ONE LLM provider to generate video scripts, or use offline mode.
        </Text>
        <Card
          style={{
            marginTop: tokens.spacingVerticalM,
            padding: tokens.spacingVerticalS,
            backgroundColor: tokens.colorNeutralBackground3,
          }}
        >
          <Text size={300}>
            <strong>Why is this required?</strong>
          </Text>
          <Text style={{ display: 'block', marginTop: tokens.spacingVerticalXS }}>
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
        fieldErrors={state.apiKeyFieldErrors}
        accountInfo={state.apiKeyAccountInfo}
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
        onLocalProviderReady={handleLocalProviderReady}
        allowInvalidKeys={allowInvalidKeys}
        onAllowInvalidKeysChange={setAllowInvalidKeys}
      />

      {!hasAtLeastOneProvider && (
        <Card
          style={{
            padding: tokens.spacingVerticalS,
            backgroundColor: tokens.colorPaletteRedBackground1,
          }}
        >
          <Text
            weight="semibold"
            style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}
          >
            <Warning24Regular /> At least one provider is required
          </Text>
          <Text style={{ display: 'block', marginTop: tokens.spacingVerticalXS }}>
            Configure at least one API key and validate it, or click &quot;Skip All&quot; to use
            offline mode.
          </Text>
        </Card>
      )}
    </div>
  );

  // Step 3: Workspace Setup (Required)
  const renderStep3Workspace = () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalM }}>
      <div style={{ textAlign: 'center', marginBottom: tokens.spacingVerticalM }}>
        <Title2>Workspace Setup</Title2>
        <Text style={{ display: 'block', marginTop: tokens.spacingVerticalS }}>
          Configure where Aura will save your videos and cache files.
        </Text>
        <Card
          style={{
            marginTop: tokens.spacingVerticalM,
            padding: tokens.spacingVerticalS,
            backgroundColor: tokens.colorNeutralBackground3,
          }}
        >
          <Text size={300}>
            <strong>Why is this required?</strong>
          </Text>
          <Text style={{ display: 'block', marginTop: tokens.spacingVerticalXS }}>
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

  // Step 5: Setup Complete (Step 6/6)
  const renderStep4Complete = () => {
    const validApiKeys = Object.entries(state.apiKeyValidationStatus)
      .filter(([_, status]) => status === 'valid')
      .map(([provider]) => provider);

    return (
      <div
        style={{
          display: 'flex',
          flexDirection: 'column',
          gap: tokens.spacingVerticalM,
          textAlign: 'center',
        }}
      >
        <div style={{ padding: tokens.spacingVerticalL }}>
          <div style={{ fontSize: '64px', marginBottom: tokens.spacingVerticalM }}>
            <Checkmark24Regular
              style={{ width: '64px', height: '64px', color: tokens.colorPaletteGreenForeground1 }}
            />
          </div>
          <Title2>Setup Summary - Ready to Save</Title2>
          <Text
            style={{
              display: 'block',
              marginTop: tokens.spacingVerticalS,
              marginBottom: tokens.spacingVerticalL,
            }}
          >
            Review your configuration and save to complete setup:
          </Text>

          {/* Show validation errors if any */}
          {completionErrors.length > 0 && (
            <Card
              style={{
                padding: tokens.spacingVerticalM,
                backgroundColor: tokens.colorPaletteRedBackground1,
                border: `1px solid ${tokens.colorPaletteRedBorder1}`,
                maxWidth: '600px',
                margin: '0 auto 1rem auto',
              }}
            >
              <div
                style={{
                  display: 'flex',
                  flexDirection: 'column',
                  gap: tokens.spacingVerticalXS,
                  textAlign: 'left',
                }}
              >
                <div
                  style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}
                >
                  <Warning24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />
                  <Title3 style={{ color: tokens.colorPaletteRedForeground1 }}>
                    Validation Failed
                  </Title3>
                </div>
                <ul style={{ margin: 0, paddingLeft: '1.5rem' }}>
                  {completionErrors.map((error, index) => (
                    <li key={index}>
                      <Text style={{ color: tokens.colorPaletteRedForeground1 }}>{error}</Text>
                    </li>
                  ))}
                </ul>
                <Text
                  size={200}
                  style={{
                    color: tokens.colorPaletteRedForeground1,
                    marginTop: tokens.spacingVerticalXS,
                  }}
                >
                  Please go back and fix these issues, or exit to complete setup later.
                </Text>
              </div>
            </Card>
          )}

          <Card
            style={{
              padding: tokens.spacingVerticalM,
              textAlign: 'left',
              maxWidth: '600px',
              margin: '0 auto',
            }}
          >
            <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalS }}>
              <div
                style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}
              >
                {ffmpegReady ? (
                  <>
                    <Checkmark24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />
                    <Text weight="semibold">FFmpeg installed and ready</Text>
                  </>
                ) : (
                  <>
                    <Warning24Regular style={{ color: tokens.colorPaletteYellowForeground1 }} />
                    <Text weight="semibold">
                      FFmpeg can be installed later (remember to add it before creating videos)
                    </Text>
                  </>
                )}
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
              marginTop: tokens.spacingVerticalL,
              display: 'flex',
              flexDirection: 'row',
              gap: tokens.spacingHorizontalM,
              justifyContent: 'center',
              alignItems: 'center',
            }}
          >
            <Button
              appearance="secondary"
              size="large"
              onClick={handleExitWizard}
              disabled={isCompletingSetup}
            >
              Exit Wizard
            </Button>
            <Button
              appearance="primary"
              size="large"
              onClick={completeOnboarding}
              disabled={isCompletingSetup}
              icon={isCompletingSetup ? <Spinner size="tiny" /> : undefined}
            >
              {isCompletingSetup ? 'Saving...' : 'Save'}
            </Button>
          </div>
          <Text
            size={200}
            style={{ marginTop: tokens.spacingVerticalS, color: tokens.colorNeutralForeground3 }}
          >
            Save will complete setup and take you to the main app
          </Text>
        </div>
      </div>
    );
  };

  const renderStepContent = () => {
    switch (state.step) {
      case 0:
        return renderStep0(); // Welcome
      case 1:
        return renderStep1FFmpeg(); // FFmpeg Check
      case 2:
        return renderStep2FFmpeg(); // FFmpeg Install
      case 3:
        return renderStep3Providers(); // Provider Configuration
      case 4:
        return renderStep3Workspace(); // Workspace Setup
      case 5:
        return renderStep4Complete(); // Complete
      default:
        return null;
    }
  };

  const buttonLabel = 'Next';
  const buttonDisabled =
    (state.step === 3 && !hasAtLeastOneProvider) ||
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
        style={{ textAlign: 'center', paddingTop: tokens.spacingVerticalL }}
      >
        <Title2>Welcome to Aura Video Studio - Let&apos;s get you set up!</Title2>
        <Text
          style={{
            display: 'block',
            marginTop: tokens.spacingVerticalXS,
            marginBottom: tokens.spacingVerticalM,
          }}
        >
          Step {state.step + 1} of {totalSteps} - Required Setup
        </Text>
        <WizardProgress
          currentStep={state.step}
          totalSteps={totalSteps}
          stepLabels={stepLabels}
          onStepClick={handleStepClick}
          onSaveAndExit={handleExitWizard}
        />
      </div>

      <ResumeWizardDialog
        open={showResumeDialog}
        wizardStatus={wizardStatus}
        onResume={handleResumeWizard}
        onStartFresh={handleStartFresh}
      />

      <div className={styles.content}>
        {state.step > 0 && state.step < totalSteps - 1 && <BackendStatusBanner />}
        <div className={styles.stepContent} key={state.step}>
          {renderStepContent()}
        </div>
      </div>

      {state.step < totalSteps - 1 && (
        <div className={styles.footer}>
          <div
            style={{
              flex: 1,
              display: 'flex',
              alignItems: 'center',
              gap: tokens.spacingHorizontalM,
            }}
          >
            <AutoSaveIndicator
              status={autoSaveStatus}
              lastSaved={lastSaved}
              error={autoSaveError}
            />
          </div>

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
