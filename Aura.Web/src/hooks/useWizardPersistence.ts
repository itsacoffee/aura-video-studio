/**
 * useWizardPersistence - Hook for persisting and resuming wizard state
 * Enables users to resume interrupted wizard sessions
 */

import { useCallback, useEffect, useRef, useState } from 'react';
import type { WizardData } from '../components/VideoWizard/types';

const STORAGE_KEY = 'aura-wizard-data';
const STEP_KEY = 'aura-wizard-step';
const TIMESTAMP_KEY = 'aura-wizard-timestamp';
const SESSION_ID_KEY = 'aura-wizard-session-id';

/** Maximum age of saved state before it's considered stale (24 hours) */
const MAX_STATE_AGE_MS = 24 * 60 * 60 * 1000;

/** Auto-save debounce interval in milliseconds */
const AUTO_SAVE_DEBOUNCE_MS = 2000;

interface PersistenceState {
  /** Whether there is a saved session that can be resumed */
  hasResumableSession: boolean;
  /** The saved wizard data if available */
  savedData: WizardData | null;
  /** The saved step number */
  savedStep: number;
  /** Timestamp of when the session was saved */
  savedAt: Date | null;
  /** Unique session ID for correlation */
  sessionId: string | null;
  /** Whether auto-save is currently in progress */
  isSaving: boolean;
  /** Error message if save failed */
  saveError: string | null;
  /** Last successful save timestamp */
  lastSaveTime: Date | null;
}

interface UseWizardPersistenceOptions {
  /** Enable auto-save on data changes */
  enableAutoSave?: boolean;
  /** Auto-save interval in milliseconds */
  autoSaveInterval?: number;
  /** Callback when session is restored */
  onSessionRestored?: (data: WizardData, step: number) => void;
  /** Callback when save fails */
  onSaveError?: (error: Error) => void;
}

interface UseWizardPersistenceReturn {
  /** Current persistence state */
  state: PersistenceState;
  /** Save the current wizard state */
  saveState: (data: WizardData, step: number) => void;
  /** Restore a saved session */
  restoreSession: () => { data: WizardData; step: number } | null;
  /** Clear the saved session */
  clearSession: () => void;
  /** Check if a session can be resumed */
  checkResumable: () => boolean;
  /** Manually trigger a save */
  triggerSave: () => void;
}

/**
 * Generate a unique session ID using crypto API for better security
 */
function generateSessionId(): string {
  // Use crypto.randomUUID if available (modern browsers), otherwise fallback
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return `wizard-${Date.now()}-${crypto.randomUUID().substring(0, 8)}`;
  }
  // Fallback for older environments
  return `wizard-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

/**
 * Safely parse JSON with error handling
 */
function safeJsonParse<T>(json: string | null): T | null {
  if (!json) return null;
  try {
    return JSON.parse(json) as T;
  } catch (error) {
    console.error('[useWizardPersistence] Failed to parse saved data:', error);
    return null;
  }
}

/**
 * Check if saved state is still valid (not expired)
 */
function isStateValid(timestamp: string | null): boolean {
  if (!timestamp) return false;
  const savedTime = new Date(timestamp).getTime();
  const now = Date.now();
  return now - savedTime < MAX_STATE_AGE_MS;
}

/**
 * Hook for persisting and resuming wizard state
 */
export function useWizardPersistence(
  options: UseWizardPersistenceOptions = {}
): UseWizardPersistenceReturn {
  const {
    enableAutoSave = true,
    autoSaveInterval = AUTO_SAVE_DEBOUNCE_MS,
    onSessionRestored,
    onSaveError,
  } = options;

  const [state, setState] = useState<PersistenceState>(() => {
    // Check for existing session on mount
    const savedData = safeJsonParse<WizardData>(localStorage.getItem(STORAGE_KEY));
    const savedStep = parseInt(localStorage.getItem(STEP_KEY) || '0', 10);
    const timestamp = localStorage.getItem(TIMESTAMP_KEY);
    const sessionId = localStorage.getItem(SESSION_ID_KEY);
    const isValid = isStateValid(timestamp);

    return {
      hasResumableSession: isValid && savedData !== null,
      savedData: isValid ? savedData : null,
      savedStep: isValid ? savedStep : 0,
      savedAt: timestamp && isValid ? new Date(timestamp) : null,
      sessionId: isValid ? sessionId : null,
      isSaving: false,
      saveError: null,
      lastSaveTime: null,
    };
  });

  // Ref for debouncing auto-save
  const autoSaveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const pendingDataRef = useRef<{ data: WizardData; step: number } | null>(null);

  /**
   * Save wizard state to localStorage
   */
  const saveState = useCallback(
    (data: WizardData, step: number) => {
      setState((prev) => ({ ...prev, isSaving: true, saveError: null }));

      try {
        const sessionId = localStorage.getItem(SESSION_ID_KEY) || generateSessionId();
        const timestamp = new Date().toISOString();

        localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
        localStorage.setItem(STEP_KEY, step.toString());
        localStorage.setItem(TIMESTAMP_KEY, timestamp);
        localStorage.setItem(SESSION_ID_KEY, sessionId);

        setState((prev) => ({
          ...prev,
          hasResumableSession: true,
          savedData: data,
          savedStep: step,
          savedAt: new Date(timestamp),
          sessionId,
          isSaving: false,
          lastSaveTime: new Date(),
        }));

        console.info('[useWizardPersistence] State saved successfully', { step, sessionId });
      } catch (error) {
        const err = error instanceof Error ? error : new Error(String(error));
        console.error('[useWizardPersistence] Failed to save state:', err);

        setState((prev) => ({
          ...prev,
          isSaving: false,
          saveError: err.message,
        }));

        if (onSaveError) {
          onSaveError(err);
        }
      }
    },
    [onSaveError]
  );

  /**
   * Debounced auto-save function
   */
  const debouncedSave = useCallback(
    (data: WizardData, step: number) => {
      pendingDataRef.current = { data, step };

      if (autoSaveTimerRef.current) {
        clearTimeout(autoSaveTimerRef.current);
      }

      autoSaveTimerRef.current = setTimeout(() => {
        if (pendingDataRef.current) {
          saveState(pendingDataRef.current.data, pendingDataRef.current.step);
          pendingDataRef.current = null;
        }
      }, autoSaveInterval);
    },
    [saveState, autoSaveInterval]
  );

  /**
   * Restore a saved session
   */
  const restoreSession = useCallback((): { data: WizardData; step: number } | null => {
    if (!state.hasResumableSession || !state.savedData) {
      return null;
    }

    console.info('[useWizardPersistence] Restoring session', {
      step: state.savedStep,
      sessionId: state.sessionId,
    });

    if (onSessionRestored) {
      onSessionRestored(state.savedData, state.savedStep);
    }

    return {
      data: state.savedData,
      step: state.savedStep,
    };
  }, [state, onSessionRestored]);

  /**
   * Clear the saved session
   */
  const clearSession = useCallback(() => {
    try {
      localStorage.removeItem(STORAGE_KEY);
      localStorage.removeItem(STEP_KEY);
      localStorage.removeItem(TIMESTAMP_KEY);
      localStorage.removeItem(SESSION_ID_KEY);

      setState({
        hasResumableSession: false,
        savedData: null,
        savedStep: 0,
        savedAt: null,
        sessionId: null,
        isSaving: false,
        saveError: null,
        lastSaveTime: null,
      });

      console.info('[useWizardPersistence] Session cleared');
    } catch (error) {
      console.error('[useWizardPersistence] Failed to clear session:', error);
    }
  }, []);

  /**
   * Check if a session can be resumed
   */
  const checkResumable = useCallback((): boolean => {
    const timestamp = localStorage.getItem(TIMESTAMP_KEY);
    const savedData = localStorage.getItem(STORAGE_KEY);
    return isStateValid(timestamp) && savedData !== null;
  }, []);

  /**
   * Trigger an immediate save (bypasses debounce)
   */
  const triggerSave = useCallback(() => {
    if (pendingDataRef.current) {
      if (autoSaveTimerRef.current) {
        clearTimeout(autoSaveTimerRef.current);
        autoSaveTimerRef.current = null;
      }
      saveState(pendingDataRef.current.data, pendingDataRef.current.step);
      pendingDataRef.current = null;
    }
  }, [saveState]);

  // Cleanup timer on unmount
  useEffect(() => {
    return () => {
      if (autoSaveTimerRef.current) {
        clearTimeout(autoSaveTimerRef.current);
      }
    };
  }, []);

  // Create the save function to expose based on auto-save setting
  const exposedSave = enableAutoSave ? debouncedSave : saveState;

  return {
    state,
    saveState: exposedSave,
    restoreSession,
    clearSession,
    checkResumable,
    triggerSave,
  };
}

export default useWizardPersistence;
