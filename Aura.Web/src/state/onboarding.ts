// Onboarding state management with state machine for First-Run Wizard

import { apiUrl } from '../config/api';
import { resetCircuitBreaker } from '../services/api/apiClient';
import type { FieldValidationError } from '../services/api/providersApi';
import { validateProviderEnhanced } from '../services/api/providersApi';
import { getDefaultCacheLocation, getDefaultSaveLocation } from '../utils/pathUtils';
import type { PreflightReport, StageCheck } from './providers';

// Hardware configuration constants
const MIN_VRAM_FOR_STABLE_DIFFUSION = 6; // GB - Minimum VRAM for SD 1.5

/**
 * Wizard validation status - deterministic state machine
 * Enhanced with explicit error states and recovery options
 *
 * State transitions:
 * NotStarted → CheckingEnvironment → FFmpegCheck → FFmpegInstallInProgress →
 * FFmpegInstalled → ProviderConfig → ValidationInProgress → Completed
 *
 * Error states can be entered from any state and include recovery metadata
 */
export type WizardStatus =
  | 'idle' // Initial state, waiting for user to click Validate
  | 'validating' // Running preflight checks
  | 'valid' // Validation passed, ready to advance
  | 'invalid' // Validation failed, showing fix actions
  | 'installing' // Installing dependencies/engines
  | 'installed' // Installation complete
  | 'ready'; // All set, wizard complete

/**
 * Enhanced wizard step states for explicit flow control
 */
export type WizardStepState =
  | 'NotStarted'
  | 'CheckingEnvironment'
  | 'FFmpegCheck'
  | 'FFmpegInstallInProgress'
  | 'FFmpegInstalled'
  | 'ProviderConfig'
  | 'ValidationInProgress'
  | 'Completed'
  | 'Error';

/**
 * Error category for targeted recovery actions
 */
export type ErrorCategory =
  | 'Network'
  | 'Validation'
  | 'Permission'
  | 'DiskSpace'
  | 'Internal'
  | 'Configuration';

/**
 * Enhanced error metadata for recovery
 */
export interface WizardError {
  code: string;
  message: string;
  category: ErrorCategory;
  canRetry: boolean;
  recoveryActions: string[];
  correlationId: string;
  timestamp: Date;
  affectedComponent?: string;
}

export type WizardMode = 'free' | 'local' | 'pro';
export type TierSelection = 'free' | 'pro' | null;

export interface WizardValidation {
  correlationId: string;
  timestamp: Date;
  report: PreflightReport;
  failedStages: StageCheck[];
}

export interface OnboardingState {
  step: number;
  mode: WizardMode;
  selectedTier: TierSelection;
  status: WizardStatus;
  stepState: WizardStepState;
  lastValidation: WizardValidation | null;
  errors: string[];
  currentError: WizardError | null;
  isDetectingHardware: boolean;
  isScanningDependencies: boolean;
  hardware: {
    gpu?: string;
    vram?: number;
    canRunSD: boolean;
    recommendation: string;
  } | null;
  installItems: Array<{
    id: string;
    name: string;
    description?: string;
    defaultPath?: string;
    required: boolean;
    installed: boolean;
    installing: boolean;
    skipped: boolean;
    error?: string;
    lastAttemptTimestamp?: number;
    retryCount?: number;
  }>;
  apiKeys: Record<string, string>;
  apiKeyValidationStatus: Record<string, 'idle' | 'validating' | 'valid' | 'invalid'>;
  apiKeyErrors: Record<string, string>;
  apiKeyFieldErrors: Record<string, FieldValidationError[]>;
  apiKeyAccountInfo: Record<string, string>;
  workspacePreferences: {
    defaultSaveLocation: string;
    cacheLocation: string;
    autosaveInterval: number;
    theme: 'light' | 'dark' | 'auto';
  };
  selectedTemplate: string | null;
  showTutorial: boolean;
  tutorialCompleted: boolean;
  stateTransitionLog: Array<{
    from: string;
    to: string;
    timestamp: Date;
    correlationId: string;
    action: string;
  }>;
}

export const initialOnboardingState: OnboardingState = {
  step: 0,
  mode: 'free',
  selectedTier: null,
  status: 'idle',
  stepState: 'NotStarted',
  lastValidation: null,
  errors: [],
  currentError: null,
  isDetectingHardware: false,
  isScanningDependencies: false,
  hardware: null,
  installItems: [
    {
      id: 'ffmpeg',
      name: 'FFmpeg (Video encoding)',
      description:
        'Essential video and audio processing toolkit. Required for all video generation.',
      defaultPath: '%LOCALAPPDATA%\\Aura\\Tools\\ffmpeg',
      required: true,
      installed: false,
      installing: false,
      skipped: false,
    },
    {
      id: 'ollama',
      name: 'Ollama (Local AI)',
      description:
        'Run AI models locally for script generation. Privacy-focused alternative to cloud APIs.',
      defaultPath: '%LOCALAPPDATA%\\Aura\\Tools\\ollama',
      required: false,
      installed: false,
      installing: false,
      skipped: false,
    },
    {
      id: 'stable-diffusion',
      name: 'Stable Diffusion WebUI',
      description: 'Generate custom images locally. Requires NVIDIA GPU with 6GB+ VRAM.',
      defaultPath: '%LOCALAPPDATA%\\Aura\\Tools\\stable-diffusion-webui',
      required: false,
      installed: false,
      installing: false,
      skipped: false,
    },
  ],
  apiKeys: {},
  apiKeyValidationStatus: {},
  apiKeyErrors: {},
  apiKeyFieldErrors: {},
  apiKeyAccountInfo: {},
  workspacePreferences: {
    defaultSaveLocation: getDefaultSaveLocation(),
    cacheLocation: getDefaultCacheLocation(),
    autosaveInterval: 3,
    theme: 'auto',
  },
  selectedTemplate: null,
  showTutorial: false,
  tutorialCompleted: false,
  stateTransitionLog: [],
};

// Action types
export type OnboardingAction =
  | { type: 'SET_STEP'; payload: number }
  | { type: 'SET_MODE'; payload: WizardMode }
  | { type: 'SET_TIER'; payload: TierSelection }
  | { type: 'SET_STATUS'; payload: WizardStatus }
  | { type: 'SET_STEP_STATE'; payload: WizardStepState }
  | { type: 'SET_ERROR'; payload: WizardError }
  | { type: 'CLEAR_ERROR' }
  | { type: 'START_VALIDATION' }
  | { type: 'VALIDATION_SUCCESS'; payload: { report: PreflightReport; correlationId: string } }
  | { type: 'VALIDATION_FAILED'; payload: { report: PreflightReport; correlationId: string } }
  | { type: 'START_HARDWARE_DETECTION' }
  | { type: 'HARDWARE_DETECTED'; payload: OnboardingState['hardware'] }
  | { type: 'HARDWARE_DETECTION_FAILED'; payload: string }
  | { type: 'START_DEPENDENCY_SCAN' }
  | { type: 'DEPENDENCY_SCAN_COMPLETE' }
  | { type: 'START_INSTALL'; payload: string }
  | { type: 'INSTALL_COMPLETE'; payload: string }
  | { type: 'INSTALL_FAILED'; payload: { itemId: string; error: string; canRetry?: boolean } }
  | { type: 'SKIP_INSTALL'; payload: string }
  | { type: 'MARK_READY' }
  | { type: 'RESET_VALIDATION' }
  | { type: 'SET_API_KEY'; payload: { provider: string; key: string } }
  | { type: 'START_API_KEY_VALIDATION'; payload: string }
  | {
      type: 'API_KEY_VALID';
      payload: { provider: string; accountInfo?: string; fieldErrors?: FieldValidationError[] };
    }
  | {
      type: 'API_KEY_INVALID';
      payload: { provider: string; error: string; fieldErrors?: FieldValidationError[] };
    }
  | { type: 'SKIP_API_KEY_VALIDATION'; payload: string }
  | { type: 'SET_WORKSPACE_PREFERENCES'; payload: OnboardingState['workspacePreferences'] }
  | { type: 'SET_TEMPLATE'; payload: string | null }
  | { type: 'TOGGLE_TUTORIAL' }
  | { type: 'COMPLETE_TUTORIAL' }
  | { type: 'SET_MANUAL_HARDWARE'; payload: { vram?: number; hasGpu: boolean } }
  | { type: 'SKIP_HARDWARE_DETECTION' }
  | { type: 'LOAD_FROM_STORAGE'; payload: Partial<OnboardingState> }
  | { type: 'SAVE_TO_BACKEND_TRIGGERED' }
  | {
      type: 'LOG_STATE_TRANSITION';
      payload: { from: string; to: string; action: string; correlationId: string };
    };

/**
 * Helper function to log state transitions with correlation ID
 */
function logStateTransition(
  state: OnboardingState,
  action: string,
  newState: Partial<OnboardingState>,
  correlationId?: string
): OnboardingState {
  const transitionEntry = {
    from: `Step${state.step}-${state.stepState}`,
    to: `Step${newState.step ?? state.step}-${newState.stepState ?? state.stepState}`,
    timestamp: new Date(),
    correlationId: correlationId || `transition-${Date.now()}`,
    action,
  };

  console.info('[Wizard State Transition]', transitionEntry);

  return {
    ...state,
    ...newState,
    stateTransitionLog: [...state.stateTransitionLog, transitionEntry],
  };
}

// Reducer
export function onboardingReducer(
  state: OnboardingState,
  action: OnboardingAction
): OnboardingState {
  switch (action.type) {
    case 'SET_STEP':
      return logStateTransition(state, 'SET_STEP', { step: action.payload });

    case 'SET_MODE':
      return { ...state, mode: action.payload };

    case 'SET_TIER':
      return { ...state, selectedTier: action.payload };

    case 'SET_STATUS':
      return logStateTransition(state, 'SET_STATUS', { status: action.payload });

    case 'SET_STEP_STATE':
      return logStateTransition(state, 'SET_STEP_STATE', { stepState: action.payload });

    case 'SAVE_TO_BACKEND_TRIGGERED':
      return state;

    case 'SET_ERROR':
      console.error('[Wizard Error]', {
        code: action.payload.code,
        message: action.payload.message,
        category: action.payload.category,
        correlationId: action.payload.correlationId,
      });
      return logStateTransition(
        state,
        'SET_ERROR',
        {
          currentError: action.payload,
          stepState: 'Error',
          errors: [...state.errors, action.payload.message],
        },
        action.payload.correlationId
      );

    case 'CLEAR_ERROR':
      return logStateTransition(state, 'CLEAR_ERROR', {
        currentError: null,
      });

    case 'START_VALIDATION':
      return {
        ...state,
        status: 'validating',
        errors: [],
        lastValidation: null,
      };

    case 'VALIDATION_SUCCESS':
      return {
        ...state,
        status: 'valid',
        lastValidation: {
          correlationId: action.payload.correlationId,
          timestamp: new Date(),
          report: action.payload.report,
          failedStages: [],
        },
        errors: [],
      };

    case 'VALIDATION_FAILED':
      return {
        ...state,
        status: 'invalid',
        lastValidation: {
          correlationId: action.payload.correlationId,
          timestamp: new Date(),
          report: action.payload.report,
          failedStages: action.payload.report.stages.filter((s) => s.status === 'fail'),
        },
        errors: action.payload.report.stages
          .filter((s) => s.status === 'fail')
          .map((s) => s.message),
      };

    case 'START_HARDWARE_DETECTION':
      return {
        ...state,
        isDetectingHardware: true,
        hardware: null,
      };

    case 'HARDWARE_DETECTED':
      return {
        ...state,
        isDetectingHardware: false,
        hardware: action.payload,
      };

    case 'HARDWARE_DETECTION_FAILED':
      return {
        ...state,
        isDetectingHardware: false,
        hardware: {
          canRunSD: false,
          recommendation:
            action.payload ||
            'Could not detect hardware. We recommend starting with Free-only mode using Stock images.',
        },
      };

    case 'START_DEPENDENCY_SCAN':
      return {
        ...state,
        isScanningDependencies: true,
      };

    case 'DEPENDENCY_SCAN_COMPLETE':
      return {
        ...state,
        isScanningDependencies: false,
      };

    case 'START_INSTALL':
      return {
        ...state,
        status: 'installing',
        installItems: state.installItems.map((item) =>
          item.id === action.payload ? { ...item, installing: true } : item
        ),
      };

    case 'INSTALL_COMPLETE':
      return {
        ...state,
        status: 'installed',
        installItems: state.installItems.map((item) =>
          item.id === action.payload
            ? { ...item, installing: false, installed: true, skipped: false }
            : item
        ),
      };

    case 'INSTALL_FAILED': {
      const targetItem = state.installItems.find((item) => item.id === action.payload.itemId);
      const retryCount = (targetItem?.retryCount || 0) + 1;

      return logStateTransition(
        state,
        'INSTALL_FAILED',
        {
          status: 'idle',
          installItems: state.installItems.map((item) =>
            item.id === action.payload.itemId
              ? {
                  ...item,
                  installing: false,
                  error: action.payload.error,
                  lastAttemptTimestamp: Date.now(),
                  retryCount,
                }
              : item
          ),
          errors: [
            ...state.errors,
            `Failed to install ${action.payload.itemId}: ${action.payload.error}`,
          ],
          currentError: {
            code: `INSTALL_FAILED_${action.payload.itemId.toUpperCase()}`,
            message: action.payload.error,
            category: action.payload.error.toLowerCase().includes('network')
              ? 'Network'
              : action.payload.error.toLowerCase().includes('permission')
                ? 'Permission'
                : action.payload.error.toLowerCase().includes('disk') ||
                    action.payload.error.toLowerCase().includes('space')
                  ? 'DiskSpace'
                  : 'Internal',
            canRetry: action.payload.canRetry ?? true,
            recoveryActions: [
              'Retry installation',
              'Use existing installation',
              'Skip for now and configure later',
            ],
            correlationId: `install-fail-${Date.now()}`,
            timestamp: new Date(),
            affectedComponent: action.payload.itemId,
          },
        },
        `install-fail-${Date.now()}`
      );
    }

    case 'SKIP_INSTALL':
      return {
        ...state,
        installItems: state.installItems.map((item) =>
          item.id === action.payload ? { ...item, skipped: true, installed: false } : item
        ),
      };

    case 'MARK_READY':
      return {
        ...state,
        status: 'ready',
      };

    case 'RESET_VALIDATION':
      return {
        ...state,
        status: 'idle',
        lastValidation: null,
        errors: [],
      };

    case 'SET_API_KEY':
      return {
        ...state,
        apiKeys: {
          ...state.apiKeys,
          [action.payload.provider]: action.payload.key,
        },
      };

    case 'START_API_KEY_VALIDATION':
      return {
        ...state,
        apiKeyValidationStatus: {
          ...state.apiKeyValidationStatus,
          [action.payload]: 'validating',
        },
        apiKeyErrors: {
          ...state.apiKeyErrors,
          [action.payload]: '',
        },
      };

    case 'API_KEY_VALID':
      return {
        ...state,
        apiKeyValidationStatus: {
          ...state.apiKeyValidationStatus,
          [action.payload.provider]: 'valid',
        },
        apiKeyErrors: {
          ...state.apiKeyErrors,
          [action.payload.provider]: '',
        },
        apiKeyFieldErrors: {
          ...state.apiKeyFieldErrors,
          [action.payload.provider]: action.payload.fieldErrors || [],
        },
        apiKeyAccountInfo: {
          ...state.apiKeyAccountInfo,
          [action.payload.provider]: action.payload.accountInfo || '',
        },
      };

    case 'API_KEY_INVALID':
      return {
        ...state,
        apiKeyValidationStatus: {
          ...state.apiKeyValidationStatus,
          [action.payload.provider]: 'invalid',
        },
        apiKeyErrors: {
          ...state.apiKeyErrors,
          [action.payload.provider]: action.payload.error,
        },
        apiKeyFieldErrors: {
          ...state.apiKeyFieldErrors,
          [action.payload.provider]: action.payload.fieldErrors || [],
        },
      };

    case 'SKIP_API_KEY_VALIDATION':
      return {
        ...state,
        apiKeyValidationStatus: {
          ...state.apiKeyValidationStatus,
          [action.payload]: 'idle',
        },
        apiKeyErrors: {
          ...state.apiKeyErrors,
          [action.payload]: '',
        },
        apiKeyFieldErrors: {
          ...state.apiKeyFieldErrors,
          [action.payload]: [],
        },
      };

    case 'SET_WORKSPACE_PREFERENCES':
      return {
        ...state,
        workspacePreferences: action.payload,
      };

    case 'SET_TEMPLATE':
      return {
        ...state,
        selectedTemplate: action.payload,
      };

    case 'TOGGLE_TUTORIAL':
      return {
        ...state,
        showTutorial: !state.showTutorial,
      };

    case 'COMPLETE_TUTORIAL':
      return {
        ...state,
        tutorialCompleted: true,
        showTutorial: false,
      };

    case 'SET_MANUAL_HARDWARE': {
      const vramAmount = action.payload.vram || 0;
      const canRunSD = action.payload.hasGpu && vramAmount >= MIN_VRAM_FOR_STABLE_DIFFUSION;
      const gpuDescription = action.payload.hasGpu
        ? `GPU with ${vramAmount}GB VRAM (manually configured)`
        : 'No dedicated GPU (integrated graphics)';
      const recommendation = action.payload.hasGpu
        ? `Manually configured GPU with ${vramAmount}GB VRAM. ${canRunSD ? 'Should be sufficient for local Stable Diffusion.' : 'We recommend using Stock images or Pro cloud providers.'}`
        : 'No dedicated GPU detected. We recommend using Stock images or Pro cloud providers.';

      return {
        ...state,
        hardware: {
          gpu: gpuDescription,
          vram: action.payload.vram,
          canRunSD,
          recommendation,
        },
      };
    }

    case 'SKIP_HARDWARE_DETECTION':
      return {
        ...state,
        isDetectingHardware: false,
        hardware: {
          canRunSD: false,
          recommendation:
            'Hardware detection skipped. You can configure hardware settings later in Settings.',
        },
      };

    case 'LOAD_FROM_STORAGE':
      return {
        ...state,
        ...action.payload,
      };

    case 'LOG_STATE_TRANSITION':
      return {
        ...state,
        stateTransitionLog: [
          ...state.stateTransitionLog,
          {
            ...action.payload,
            timestamp: new Date(),
          },
        ],
      };

    default:
      return state;
  }
}

// Thunks / async actions
export async function runValidationThunk(
  state: OnboardingState,
  dispatch: (action: OnboardingAction) => void
): Promise<void> {
  dispatch({ type: 'START_VALIDATION' });

  try {
    const profileMap: Record<WizardMode, string> = {
      free: 'Free-Only',
      local: 'Balanced Mix',
      pro: 'Pro-Max',
    };

    const correlationId = `validation-${Date.now()}-${Math.random().toString(36).substring(7)}`;
    const response = await fetch(
      `/api/preflight?profile=${profileMap[state.mode]}&correlationId=${correlationId}`
    );

    if (!response.ok) {
      throw new Error(`Preflight check failed: ${response.statusText}`);
    }

    const report: PreflightReport = await response.json();

    if (report.ok) {
      dispatch({ type: 'VALIDATION_SUCCESS', payload: { report, correlationId } });
    } else {
      dispatch({ type: 'VALIDATION_FAILED', payload: { report, correlationId } });
    }
  } catch (error) {
    console.error('Validation failed:', error);
    // Create a synthetic failed report
    const syntheticReport: PreflightReport = {
      ok: false,
      stages: [
        {
          stage: 'System',
          status: 'fail',
          provider: 'Network',
          message: error instanceof Error ? error.message : 'Unknown error',
          hint: 'Check your network connection and try again',
        },
      ],
    };
    dispatch({
      type: 'VALIDATION_FAILED',
      payload: {
        report: syntheticReport,
        correlationId: `error-${Date.now()}`,
      },
    });
  }
}

export async function detectHardwareThunk(
  dispatch: (action: OnboardingAction) => void
): Promise<void> {
  dispatch({ type: 'START_HARDWARE_DETECTION' });

  try {
    const response = await fetch('/api/probes/run', {
      method: 'POST',
    });
    if (response.ok) {
      const data = await response.json();

      const gpuInfo = data.gpu || 'Unknown GPU';
      const vramGB = data.vramGB || 0;
      const canRunSD = data.enableLocalDiffusion || false;

      let recommendation = '';
      if (canRunSD) {
        recommendation = `Your ${gpuInfo} with ${vramGB}GB VRAM can run Stable Diffusion locally!`;
      } else {
        recommendation = `Your system doesn't meet requirements for local Stable Diffusion. We recommend using Stock images or Pro cloud providers.`;
      }

      dispatch({
        type: 'HARDWARE_DETECTED',
        payload: {
          gpu: gpuInfo,
          vram: vramGB,
          canRunSD,
          recommendation,
        },
      });
    } else {
      dispatch({
        type: 'HARDWARE_DETECTION_FAILED',
        payload:
          'Could not detect hardware. We recommend starting with Free-only mode using Stock images.',
      });
    }
  } catch (error) {
    console.error('Hardware detection failed:', error);
    dispatch({
      type: 'HARDWARE_DETECTION_FAILED',
      payload:
        'Could not detect hardware. We recommend starting with Free-only mode using Stock images.',
    });
  }
}

/**
 * Helper to get install configuration for an item
 */
function getInstallConfig(itemId: string): {
  apiEndpoint: string;
  statusEndpoint: string;
  requestBody?: { mode: string };
} | null {
  switch (itemId) {
    case 'ffmpeg':
      return {
        apiEndpoint: apiUrl('/api/downloads/ffmpeg/install'),
        statusEndpoint: apiUrl('/api/system/ffmpeg/status'),
        requestBody: { mode: 'managed' },
      };
    case 'ollama':
    case 'stable-diffusion':
      // Installation not yet implemented via API - use Download Center
      return null;
    default:
      throw new Error(`Unknown item: ${itemId}`);
  }
}

/**
 * Helper to categorize errors and determine if retry is possible
 */
function categorizeInstallError(
  error: unknown,
  retryAttempt: number,
  maxRetries: number
): {
  message: string;
  isNetworkError: boolean;
  canRetry: boolean;
} {
  const errorMessage = error instanceof Error ? error.message : 'Unknown error';
  const isNetworkError =
    errorMessage.toLowerCase().includes('network') ||
    errorMessage.toLowerCase().includes('fetch') ||
    errorMessage.toLowerCase().includes('timeout') ||
    errorMessage.toLowerCase().includes('connection');

  return {
    message: errorMessage,
    isNetworkError,
    canRetry: isNetworkError && retryAttempt < maxRetries,
  };
}

/**
 * Helper to verify installation status
 */
async function verifyInstallation(itemId: string, statusEndpoint: string): Promise<void> {
  const statusResponse = await fetch(statusEndpoint);
  if (!statusResponse.ok) {
    return; // Don't fail on status check failure
  }

  const statusData = await statusResponse.json();

  // For FFmpeg, check if it's actually installed and valid
  if (itemId === 'ffmpeg' && (!statusData.installed || !statusData.valid)) {
    throw new Error(
      'FFmpeg installation completed but validation failed. The installation may be incomplete.'
    );
  }
}

/**
 * Helper to execute installation with timeout
 */
async function executeInstallation(
  apiEndpoint: string,
  requestBody: { mode: string } | undefined
): Promise<{ success: boolean; error?: string }> {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), 120000); // 2 minute timeout

  try {
    const response = await fetch(apiEndpoint, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(requestBody),
      signal: controller.signal,
    });

    clearTimeout(timeoutId);

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({ error: response.statusText }));
      const errorMessage = errorData.error || `Installation failed with status ${response.status}`;
      return { success: false, error: errorMessage };
    }

    return await response.json();
  } catch (fetchError) {
    clearTimeout(timeoutId);

    if (fetchError instanceof Error && fetchError.name === 'AbortError') {
      throw new Error(
        'Installation timed out. Please check your internet connection and try again.'
      );
    }

    throw fetchError;
  }
}

export async function installItemThunk(
  itemId: string,
  dispatch: (action: OnboardingAction) => void,
  retryAttempt = 0
): Promise<void> {
  const maxRetries = 3;
  const retryDelay = Math.min(1000 * Math.pow(2, retryAttempt), 10000);

  dispatch({ type: 'START_INSTALL', payload: itemId });

  try {
    const config = getInstallConfig(itemId);

    // Handle items that don't support installation yet
    if (!config) {
      dispatch({ type: 'INSTALL_COMPLETE', payload: itemId });
      return;
    }

    // Execute installation
    const result = await executeInstallation(config.apiEndpoint, config.requestBody);

    // Handle installation failure with retry
    if (!result.success) {
      const canRetry = retryAttempt < maxRetries;
      if (canRetry) {
        console.warn(
          `Installation attempt ${retryAttempt + 1} failed, retrying in ${retryDelay}ms...`
        );
        await new Promise((resolve) => setTimeout(resolve, retryDelay));
        return installItemThunk(itemId, dispatch, retryAttempt + 1);
      }
      throw new Error(result.error || 'Installation failed');
    }

    // Verify installation
    try {
      await verifyInstallation(itemId, config.statusEndpoint);
    } catch (statusError) {
      console.warn(`Failed to verify ${itemId} status after installation:`, statusError);
    }

    dispatch({ type: 'INSTALL_COMPLETE', payload: itemId });
  } catch (error) {
    console.error(`Installation of ${itemId} failed:`, error);

    const { message, canRetry } = categorizeInstallError(error, retryAttempt, maxRetries);

    dispatch({
      type: 'INSTALL_FAILED',
      payload: {
        itemId,
        error: message,
        canRetry,
      },
    });
  }
}

// Button label helper
export function getButtonLabel(status: WizardStatus, isLastStep: boolean): string {
  switch (status) {
    case 'idle':
      return isLastStep ? 'Validate' : 'Next';
    case 'validating':
      return 'Validating…';
    case 'valid':
      return 'Next';
    case 'invalid':
      return 'Fix Issues';
    case 'installing':
      return 'Installing…';
    case 'installed':
      return 'Validate';
    case 'ready':
      return 'Continue';
    default:
      return 'Next';
  }
}

// Button disabled helper
export function isButtonDisabled(status: WizardStatus, isDetectingHardware: boolean): boolean {
  return status === 'validating' || status === 'installing' || isDetectingHardware;
}

// Check if can advance to next step
export function canAdvanceStep(status: WizardStatus): boolean {
  return status === 'valid' || status === 'ready';
}

// Check installation status for an item using the dependency rescan API
export async function checkInstallationStatusThunk(
  itemId: string,
  dispatch: (action: OnboardingAction) => void
): Promise<void> {
  try {
    // For now, we'll use the old approach for FFmpeg and skip for others
    // The full rescan will handle all dependencies
    if (itemId === 'ffmpeg') {
      const statusEndpoint = apiUrl('/api/downloads/ffmpeg/status');
      const response = await fetch(statusEndpoint);
      if (response.ok) {
        const data = await response.json();
        if (data.state === 'Installed' || data.state === 'ExternalAttached') {
          dispatch({ type: 'INSTALL_COMPLETE', payload: itemId });
        }
      }
    }
  } catch (error) {
    console.warn(`Failed to check installation status for ${itemId}:`, error);
  }
}

// Check all installation statuses using the dependency rescan API
export async function checkAllInstallationStatusesThunk(
  dispatch: (action: OnboardingAction) => void
): Promise<void> {
  dispatch({ type: 'START_DEPENDENCY_SCAN' });

  try {
    // Call the dependency rescan API to check all dependencies
    const response = await fetch(apiUrl('/api/dependencies/rescan'), {
      method: 'POST',
    });

    if (response.ok) {
      const data = await response.json();

      if (data.success && data.dependencies) {
        // Map dependency status to installation state
        for (const dep of data.dependencies) {
          const itemId = dep.id;

          // Map status to our install state
          if (dep.status === 'Installed') {
            dispatch({ type: 'INSTALL_COMPLETE', payload: itemId });
          }
        }
      }
    } else {
      console.warn('Dependency rescan API failed:', response.statusText);
      // Fallback to individual checks for FFmpeg
      await checkInstallationStatusThunk('ffmpeg', dispatch);
    }
  } catch (error) {
    console.warn('Failed to check all installation statuses:', error);
    // Fallback to individual checks for FFmpeg
    await checkInstallationStatusThunk('ffmpeg', dispatch);
  } finally {
    dispatch({ type: 'DEPENDENCY_SCAN_COMPLETE' });
  }
}

// Validate API key
export async function validateApiKeyThunk(
  provider: string,
  apiKey: string,
  dispatch: (action: OnboardingAction) => void
): Promise<void> {
  dispatch({ type: 'START_API_KEY_VALIDATION', payload: provider });

  // CRITICAL FIX: Reset circuit breaker before API key validation
  // This prevents false "service unavailable" errors from persisted circuit breaker state
  resetCircuitBreaker();
  console.info(`[API Key Validation] Circuit breaker reset for ${provider} validation`);

  try {
    // Map frontend provider IDs to backend provider names and API key fields
    const providerNameMap: Record<string, { validatorName: string; keyField: string }> = {
      openai: { validatorName: 'OpenAI', keyField: 'openai' },
      anthropic: { validatorName: 'Anthropic', keyField: 'anthropic' },
      gemini: { validatorName: 'Gemini', keyField: 'gemini' },
      elevenlabs: { validatorName: 'ElevenLabs', keyField: 'elevenlabs' },
      playht: { validatorName: 'PlayHT', keyField: 'playht' },
      replicate: { validatorName: 'Replicate', keyField: 'replicate' },
      pexels: { validatorName: 'Pexels', keyField: 'pexels' },
      ollama: { validatorName: 'Ollama', keyField: 'ollama' },
    };

    const providerInfo = providerNameMap[provider.toLowerCase()] || {
      validatorName: provider,
      keyField: provider.toLowerCase(),
    };

    // Ollama doesn't require an API key - skip format validation and key saving
    const isOllama = provider.toLowerCase() === 'ollama';
    
    if (!isOllama) {
      // Client-side format validation for providers that require API keys
      const formatValidation = validateApiKeyFormat(provider, apiKey);
      if (!formatValidation.valid) {
        dispatch({
          type: 'API_KEY_INVALID',
          payload: { provider, error: formatValidation.error || 'Invalid API key format' },
        });
        return;
      }

      // Save the API key using secure KeyVault API (encrypted storage)
      const saveResponse = await fetch(apiUrl('/api/keys/set'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          provider: providerInfo.keyField,
          apiKey: apiKey.trim(),
        }),
      });

      if (!saveResponse.ok) {
        throw new Error(`Failed to save API key: ${saveResponse.statusText}`);
      }
    }

    // Use enhanced validation endpoint for providers that support it
    // For Ollama, use the legacy validation endpoint since it doesn't require an API key
    if (isOllama) {
      // Ollama validation - use legacy endpoint that checks service availability
      console.info('[Ollama Validation] Using legacy validation endpoint');
      const response = await fetch(apiUrl('/api/providers/validate'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          providers: [providerInfo.validatorName],
        }),
      });

      if (!response.ok) {
        throw new Error(`Validation request failed: ${response.statusText}`);
      }

      const result: {
        results: Array<{ name: string; ok: boolean; details: string }>;
        ok: boolean;
      } = await response.json();

      const providerResult = result.results.find(
        (r) => r.name.toLowerCase() === providerInfo.validatorName.toLowerCase()
      );

      if (!providerResult) {
        throw new Error('Ollama validation result not found in response');
      }

      if (providerResult.ok) {
        dispatch({
          type: 'API_KEY_VALID',
          payload: {
            provider,
            accountInfo: providerResult.details || 'Ollama is running and validated',
          },
        });
      } else {
        dispatch({
          type: 'API_KEY_INVALID',
          payload: {
            provider,
            error: providerResult.details || 'Ollama validation failed. Ensure Ollama is running.',
          },
        });
      }
      return;
    }

    // Use enhanced validation endpoint for providers that require API keys
    try {
      const validationResponse = await validateProviderEnhanced({
        provider: providerInfo.validatorName,
        configuration: {
          ApiKey: apiKey.trim(),
        },
      });

      console.info('[Enhanced Validation] Response:', {
        provider: providerInfo.validatorName,
        isValid: validationResponse.isValid,
        status: validationResponse.status,
        fieldErrors: validationResponse.fieldErrors,
      });

      if (validationResponse.isValid) {
        dispatch({
          type: 'API_KEY_VALID',
          payload: {
            provider,
            accountInfo: validationResponse.overallMessage || 'API key validated successfully',
            fieldErrors: (validationResponse.fieldErrors as FieldValidationError[]) || [],
          },
        });
      } else {
        const errorMessage = validationResponse.overallMessage || 'API key validation failed';
        dispatch({
          type: 'API_KEY_INVALID',
          payload: {
            provider,
            error: errorMessage,
            fieldErrors: (validationResponse.fieldErrors as FieldValidationError[]) || [],
          },
        });
      }
    } catch (validationError) {
      console.error('[Enhanced Validation] Error:', validationError);

      // Check if this is a network error before falling back
      const { isNetworkError } = await import('../services/api/errorHandler');
      if (isNetworkError(validationError)) {
        console.warn('[Enhanced Validation] Network error detected, not marking key as invalid');
        dispatch({
          type: 'API_KEY_INVALID',
          payload: {
            provider,
            error:
              'Unable to reach the backend server to validate your API key. Please ensure the backend service is running and try again.',
          },
        });
        return;
      }

      // Fallback to legacy validation if enhanced validation fails
      console.info('[Enhanced Validation] Falling back to legacy validation');
      const response = await fetch(apiUrl('/api/providers/validate'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          providers: [providerInfo.validatorName],
        }),
      });

      if (!response.ok) {
        throw new Error(`Validation request failed: ${response.statusText}`);
      }

      const result: {
        results: Array<{ name: string; ok: boolean; details: string }>;
        ok: boolean;
      } = await response.json();

      const providerResult = result.results.find(
        (r) => r.name.toLowerCase() === providerInfo.validatorName.toLowerCase()
      );

      if (!providerResult) {
        throw new Error('Provider validation result not found in response');
      }

      if (providerResult.ok) {
        dispatch({
          type: 'API_KEY_VALID',
          payload: {
            provider,
            accountInfo: providerResult.details || 'API key validated successfully',
          },
        });
      } else {
        dispatch({
          type: 'API_KEY_INVALID',
          payload: {
            provider,
            error: providerResult.details || 'API key validation failed',
          },
        });
      }
    }
  } catch (error: unknown) {
    console.error('API key validation error:', error);

    // Import error handler to properly categorize errors
    const { isNetworkError, parseApiError } = await import('../services/api/errorHandler');

    // Check if this is a network error
    if (isNetworkError(error)) {
      const { message } = await parseApiError(error);
      dispatch({
        type: 'API_KEY_INVALID',
        payload: {
          provider,
          error:
            message ||
            'Network error: Unable to connect to the backend server. Please ensure the backend is running and your network connection is stable.',
        },
      });
      return;
    }

    // For other errors, provide a clear message
    const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
    dispatch({
      type: 'API_KEY_INVALID',
      payload: {
        provider,
        error: `Validation failed: ${errorMessage}. Please try again or check your network connection.`,
      },
    });
  }
}

// Validate API key format
function validateApiKeyFormat(
  provider: string,
  apiKey: string
): { valid: boolean; error?: string } {
  const trimmedKey = apiKey.trim();

  if (!trimmedKey) {
    return { valid: false, error: 'Please enter your API key' };
  }

  switch (provider) {
    case 'openai':
      if (!trimmedKey.startsWith('sk-')) {
        return { valid: false, error: 'OpenAI API keys must start with "sk-"' };
      }
      if (trimmedKey.length < 20) {
        return { valid: false, error: 'OpenAI API keys must be at least 20 characters' };
      }
      break;
    case 'anthropic':
      if (!trimmedKey.startsWith('sk-ant-')) {
        return { valid: false, error: 'Anthropic API keys start with "sk-ant-"' };
      }
      break;
    case 'gemini':
      if (trimmedKey.length !== 39) {
        return { valid: false, error: 'Google Gemini API keys are 39 characters long' };
      }
      break;
    case 'replicate':
      if (!trimmedKey.startsWith('r8_')) {
        return { valid: false, error: 'Replicate API keys start with "r8_"' };
      }
      break;
    case 'elevenlabs':
      if (trimmedKey.length !== 32) {
        return { valid: false, error: 'ElevenLabs API keys are 32 characters long' };
      }
      break;
    case 'playht':
      if (!trimmedKey.includes(':')) {
        return {
          valid: false,
          error: 'PlayHT requires both User ID and Secret Key (format: userId:secretKey)',
        };
      }
      break;
    case 'pexels':
      if (trimmedKey.length < 20) {
        return { valid: false, error: 'Pexels API keys should be at least 20 characters' };
      }
      break;
  }

  return { valid: true };
}

// Save wizard state to localStorage and backend
export function saveWizardStateToStorage(state: OnboardingState): void {
  try {
    const stateToSave = {
      step: state.step,
      selectedTier: state.selectedTier,
      mode: state.mode,
      apiKeys: state.apiKeys,
      apiKeyValidationStatus: state.apiKeyValidationStatus,
      hardware: state.hardware,
      installItems: state.installItems,
    };
    localStorage.setItem('wizardProgress', JSON.stringify(stateToSave));

    // Also save to backend (fire and forget)
    import('../services/firstRunService').then(({ saveWizardProgressToBackend }) => {
      saveWizardProgressToBackend(
        state.step,
        state.selectedTier,
        JSON.stringify(stateToSave)
      ).catch((error) => {
        console.warn('Failed to save wizard progress to backend:', error);
      });
    });
  } catch (error) {
    console.error('Failed to save wizard state:', error);
  }
}

// Load wizard state from localStorage
export function loadWizardStateFromStorage(): Partial<OnboardingState> | null {
  try {
    const saved = localStorage.getItem('wizardProgress');
    if (saved) {
      return JSON.parse(saved);
    }
  } catch (error) {
    console.error('Failed to load wizard state:', error);
  }
  return null;
}

// Clear wizard state from localStorage
export function clearWizardStateFromStorage(): void {
  try {
    localStorage.removeItem('wizardProgress');
  } catch (error) {
    console.error('Failed to clear wizard state:', error);
  }
}

/**
 * Save wizard progress to backend (with automatic retry)
 */
export async function saveWizardProgressToBackend(
  state: OnboardingState,
  correlationId?: string
): Promise<boolean> {
  try {
    const { setupApi } = await import('../services/api/setupApi');

    // Extract serializable state (exclude non-serializable fields)
    const serializableState = {
      step: state.step,
      mode: state.mode,
      selectedTier: state.selectedTier,
      status: state.status,
      stepState: state.stepState,
      lastStep: state.step,
      apiKeys: state.apiKeys,
      apiKeyValidationStatus: state.apiKeyValidationStatus,
      installItems: state.installItems.map((item) => ({
        id: item.id,
        installed: item.installed,
        skipped: item.skipped,
      })),
    };

    const result = await setupApi.saveWizardProgress({
      currentStep: state.step,
      state: serializableState,
      correlationId: correlationId || `wizard-save-${Date.now()}`,
    });

    console.info('[Wizard Persistence] Progress saved to backend:', result);
    return result.success;
  } catch (error) {
    console.error('[Wizard Persistence] Failed to save progress to backend:', error);
    return false;
  }
}

/**
 * Load wizard progress from backend
 */
export async function loadWizardProgressFromBackend(
  userId?: string
): Promise<Partial<OnboardingState> | null> {
  try {
    const { setupApi } = await import('../services/api/setupApi');

    const status = await setupApi.getWizardStatus(userId);

    if (!status.canResume || !status.state) {
      return null;
    }

    console.info('[Wizard Persistence] Loaded progress from backend:', status);

    return status.state as Partial<OnboardingState>;
  } catch (error) {
    console.error('[Wizard Persistence] Failed to load progress from backend:', error);
    return null;
  }
}

/**
 * Mark wizard as complete in backend with retry logic
 * CRITICAL: This function must succeed for the wizard to exit properly
 */
export async function completeWizardInBackend(
  state: OnboardingState,
  correlationId?: string
): Promise<boolean> {
  const maxRetries = 3;
  const retryDelay = 2000; // 2 seconds between retries

  for (let attempt = 1; attempt <= maxRetries; attempt++) {
    try {
      console.info(`[Wizard Persistence] Completing wizard in backend (attempt ${attempt}/${maxRetries})`);

      const { setupApi } = await import('../services/api/setupApi');

      const result = await setupApi.completeWizard({
        finalStep: state.step,
        version: '1.0.0',
        selectedTier: state.selectedTier || undefined,
        finalState: {
          mode: state.mode,
          apiKeys: state.apiKeys,
          workspacePreferences: state.workspacePreferences,
        },
        correlationId: correlationId || `wizard-complete-${Date.now()}`,
      });

      if (result.success) {
        console.info('[Wizard Persistence] ✅ Wizard completed successfully in backend');
        return true;
      } else {
        console.error('[Wizard Persistence] ❌ Backend returned success=false:', result);
        // If backend explicitly says failure, don't retry
        return false;
      }
    } catch (error) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      console.error(`[Wizard Persistence] ❌ Attempt ${attempt}/${maxRetries} failed:`, errorObj.message);

      // If this is the last attempt, return false
      if (attempt === maxRetries) {
        console.error('[Wizard Persistence] ❌ All retry attempts exhausted');
        return false;
      }

      // Wait before retrying
      console.info(`[Wizard Persistence] Retrying in ${retryDelay}ms...`);
      await new Promise(resolve => setTimeout(resolve, retryDelay));
    }
  }

  return false;
}

/**
 * Reset wizard state in backend
 */
export async function resetWizardInBackend(
  preserveData: boolean = false,
  correlationId?: string
): Promise<boolean> {
  try {
    const { setupApi } = await import('../services/api/setupApi');

    const result = await setupApi.resetWizard({
      preserveData,
      correlationId: correlationId || `wizard-reset-${Date.now()}`,
    });

    console.info('[Wizard Persistence] Wizard reset in backend:', result);
    return result.success;
  } catch (error) {
    console.error('[Wizard Persistence] Failed to reset wizard in backend:', error);
    return false;
  }
}

/**
 * Middleware function to trigger auto-save after significant state changes
 * Returns the action types that should trigger an auto-save
 */
export function shouldTriggerAutoSave(actionType: OnboardingAction['type']): boolean {
  const autoSaveTriggers: OnboardingAction['type'][] = [
    'SET_STEP',
    'SET_TIER',
    'INSTALL_COMPLETE',
    'SKIP_INSTALL',
    'API_KEY_VALID',
    'SET_WORKSPACE_PREFERENCES',
  ];

  return autoSaveTriggers.includes(actionType);
}

/**
 * Enhanced reducer wrapper that triggers auto-save for significant changes
 */
export function onboardingReducerWithAutoSave(
  state: OnboardingState,
  action: OnboardingAction,
  enableAutoSave: boolean = true
): OnboardingState {
  const newState = onboardingReducer(state, action);

  if (enableAutoSave && shouldTriggerAutoSave(action.type)) {
    void saveWizardProgressToBackend(newState).catch((err) => {
      console.error('[Auto-save] Failed to save progress:', err);
    });
  }

  return newState;
}
