import { create } from 'zustand';
import { persist } from 'zustand/middleware';

export interface GlobalLlmSelection {
  provider: string;
  modelId: string;
}

/**
 * Model validation status for the currently selected model
 */
export interface ModelValidationStatus {
  /** Whether the model has been validated (checked against available models) */
  isValidated: boolean;
  /** Whether the model exists and is available for use */
  isValid: boolean;
  /** Error message if validation failed or model is not available */
  errorMessage?: string;
  /** Timestamp of last validation */
  lastValidatedAt?: number;
}

interface GlobalLlmStore {
  selection: GlobalLlmSelection | null;
  /** Validation status for the current model selection */
  modelValidation: ModelValidationStatus;
  setSelection: (selection: GlobalLlmSelection | null) => void;
  clearSelection: () => void;
  /** Update model validation status */
  setModelValidation: (status: ModelValidationStatus) => void;
  /** Reset model validation (e.g., when provider/model changes) */
  resetModelValidation: () => void;
}

// Local storage keys
const STORAGE_KEY = 'global-llm-selection';
const LEGACY_STORAGE_KEY = 'brainstorm-llm-selection';

/**
 * Migrate LLM selection from legacy localStorage key to new global key
 * This preserves user preferences when upgrading from the old per-page implementation
 *
 * @returns The migrated selection if found, null otherwise
 */
function migrateLegacyLlmSelection(): GlobalLlmSelection | null {
  if (typeof window === 'undefined') {
    return null;
  }

  try {
    // Check if new key already exists with valid data
    const existingData = localStorage.getItem(STORAGE_KEY);
    if (existingData) {
      try {
        const parsed = JSON.parse(existingData);
        // Zustand persist stores data as { state: { ... }, version: 0 }
        if (
          parsed?.state?.selection &&
          parsed.state.selection.provider &&
          parsed.state.selection.modelId
        ) {
          // New format already exists with valid selection, no migration needed
          return null;
        }
      } catch {
        // Invalid data in new key, proceed with migration check
      }
    }

    // Check for legacy key
    const legacyData = localStorage.getItem(LEGACY_STORAGE_KEY);
    if (!legacyData) {
      return null;
    }

    // Parse legacy data
    const legacySelection = JSON.parse(legacyData) as GlobalLlmSelection;

    // Validate migrated data structure
    if (
      legacySelection &&
      typeof legacySelection === 'object' &&
      typeof legacySelection.provider === 'string' &&
      typeof legacySelection.modelId === 'string' &&
      legacySelection.provider.length > 0 &&
      legacySelection.modelId.length > 0
    ) {
      console.info('[GlobalLlmStore] Migrating LLM selection from legacy key to global store', {
        provider: legacySelection.provider,
        modelId: legacySelection.modelId,
      });

      // Return the migrated selection - it will be saved by Zustand persist
      return legacySelection;
    }
  } catch (error) {
    console.warn('[GlobalLlmStore] Error during legacy LLM selection migration:', error);
  }

  return null;
}

/** Default model validation status (not yet validated) */
const DEFAULT_MODEL_VALIDATION: ModelValidationStatus = {
  isValidated: false,
  isValid: true, // Assume valid until proven otherwise to avoid false warnings
  errorMessage: undefined,
  lastValidatedAt: undefined,
};

export const useGlobalLlmStore = create<GlobalLlmStore>()(
  persist(
    (set) => ({
      selection: null,
      modelValidation: DEFAULT_MODEL_VALIDATION,
      setSelection: (selection) =>
        set({
          selection,
          // Reset validation when selection changes
          modelValidation: DEFAULT_MODEL_VALIDATION,
        }),
      clearSelection: () =>
        set({
          selection: null,
          modelValidation: DEFAULT_MODEL_VALIDATION,
        }),
      setModelValidation: (status) => set({ modelValidation: status }),
      resetModelValidation: () => set({ modelValidation: DEFAULT_MODEL_VALIDATION }),
    }),
    {
      name: STORAGE_KEY,
      // Only persist selection, not modelValidation (transient runtime state)
      partialize: (state) => ({ selection: state.selection }),
      // Run migration on rehydration
      onRehydrateStorage: () => {
        return (state, error) => {
          if (error) {
            console.error('[GlobalLlmStore] Error rehydrating store:', error);
            return;
          }

          // Always check for migration - even if state exists, it might be null/empty
          // This ensures we migrate if the new key exists but has no selection
          const migratedSelection = migrateLegacyLlmSelection();
          if (migratedSelection) {
            // Only migrate if current selection is null or empty
            // This prevents overwriting a valid selection with legacy data
            const currentSelection = state?.selection;
            if (!currentSelection || !currentSelection.provider || !currentSelection.modelId) {
              // Set the migrated selection
              useGlobalLlmStore.getState().setSelection(migratedSelection);

              // Clean up legacy key after successful migration
              try {
                localStorage.removeItem(LEGACY_STORAGE_KEY);
                console.info('[GlobalLlmStore] Legacy LLM selection key removed after migration');
              } catch (error) {
                console.warn('[GlobalLlmStore] Failed to remove legacy key:', error);
              }
            } else {
              // Current selection exists and is valid, but legacy key still exists
              // Clean up legacy key to prevent future migration attempts
              try {
                localStorage.removeItem(LEGACY_STORAGE_KEY);
                console.info(
                  '[GlobalLlmStore] Legacy LLM selection key removed (current selection is valid)'
                );
              } catch (error) {
                console.warn('[GlobalLlmStore] Failed to remove legacy key:', error);
              }
            }
          }
        };
      },
    }
  )
);
