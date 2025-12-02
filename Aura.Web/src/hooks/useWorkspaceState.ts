/**
 * Hook for managing workspace state with automatic persistence
 * Provides workspace state management for session restoration when opening projects
 */

import { useState, useEffect, useCallback, useRef } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { WorkspaceState, DEFAULT_WORKSPACE_STATE } from '../types/project';

/**
 * Options for the useWorkspaceState hook
 */
export interface UseWorkspaceStateOptions {
  /** Debounce delay for saving state changes (ms) */
  saveDebounce?: number;
}

/**
 * Return type for the useWorkspaceState hook
 */
export interface UseWorkspaceStateReturn {
  /** Current workspace state */
  workspaceState: WorkspaceState;

  /** Update timeline viewport */
  setTimelineViewport: (viewport: Partial<WorkspaceState['timeline']>) => void;

  /** Update selection */
  setSelection: (selection: Partial<WorkspaceState['selection']>) => void;

  /** Update panel state */
  setPanelState: (panels: Partial<WorkspaceState['panels']>) => void;

  /** Update preview state */
  setPreviewState: (preview: Partial<WorkspaceState['preview']>) => void;

  /** Update media library state */
  setMediaLibraryState: (state: Partial<WorkspaceState['mediaLibrary']>) => void;

  /** Save current workspace state to project */
  saveWorkspaceState: () => void;

  /** Restore workspace state from project */
  restoreWorkspaceState: (state: WorkspaceState) => void;

  /** Mark workspace as dirty */
  markDirty: () => void;

  /** Whether workspace has unsaved changes */
  isDirty: boolean;

  /** Get current workspace state snapshot */
  getWorkspaceState: () => WorkspaceState;
}

/**
 * Custom event for workspace state changes
 */
export const WORKSPACE_STATE_CHANGED_EVENT = 'workspace:stateChanged';

/**
 * Custom event for workspace save request
 */
export const WORKSPACE_SAVE_EVENT = 'workspace:save';

/**
 * Custom event for workspace restoration
 */
export const WORKSPACE_RESTORE_EVENT = 'workspace:restore';

/**
 * Hook for managing workspace state with automatic persistence
 *
 * @param options - Configuration options for the hook
 * @returns Workspace state and methods to update it
 */
export function useWorkspaceState(options: UseWorkspaceStateOptions = {}): UseWorkspaceStateReturn {
  const { saveDebounce = 1000 } = options;

  const location = useLocation();
  const navigate = useNavigate();

  const [workspaceState, setWorkspaceState] = useState<WorkspaceState>(DEFAULT_WORKSPACE_STATE);
  const [isDirty, setIsDirty] = useState(false);

  const saveTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const workspaceStateRef = useRef<WorkspaceState>(workspaceState);

  // Keep ref in sync with state
  useEffect(() => {
    workspaceStateRef.current = workspaceState;
  }, [workspaceState]);

  // Track current page automatically
  useEffect(() => {
    setWorkspaceState((prev) => ({
      ...prev,
      activePage: location.pathname,
    }));
    setIsDirty(true);
  }, [location.pathname]);

  // Debounced save
  const scheduleSave = useCallback(() => {
    if (saveTimeoutRef.current) {
      clearTimeout(saveTimeoutRef.current);
    }
    saveTimeoutRef.current = setTimeout(() => {
      // Trigger save through custom event for ProjectContext to handle
      window.dispatchEvent(
        new CustomEvent(WORKSPACE_STATE_CHANGED_EVENT, {
          detail: workspaceStateRef.current,
        })
      );
    }, saveDebounce);
  }, [saveDebounce]);

  /**
   * Generic state update function
   */
  const updateState = useCallback(
    <K extends keyof WorkspaceState>(key: K, value: Partial<WorkspaceState[K]>) => {
      setWorkspaceState((prev) => ({
        ...prev,
        [key]: { ...(prev[key] as object), ...value },
      }));
      setIsDirty(true);
      scheduleSave();
    },
    [scheduleSave]
  );

  /**
   * Update timeline viewport state
   */
  const setTimelineViewport = useCallback(
    (viewport: Partial<WorkspaceState['timeline']>) => {
      updateState('timeline', viewport);
    },
    [updateState]
  );

  /**
   * Update selection state
   */
  const setSelection = useCallback(
    (selection: Partial<WorkspaceState['selection']>) => {
      updateState('selection', selection);
    },
    [updateState]
  );

  /**
   * Update panel state
   */
  const setPanelState = useCallback(
    (panels: Partial<WorkspaceState['panels']>) => {
      updateState('panels', panels);
    },
    [updateState]
  );

  /**
   * Update preview state
   */
  const setPreviewState = useCallback(
    (preview: Partial<WorkspaceState['preview']>) => {
      updateState('preview', preview);
    },
    [updateState]
  );

  /**
   * Update media library state
   */
  const setMediaLibraryState = useCallback(
    (state: Partial<WorkspaceState['mediaLibrary']>) => {
      updateState('mediaLibrary', state);
    },
    [updateState]
  );

  /**
   * Immediately save workspace state
   */
  const saveWorkspaceState = useCallback(() => {
    window.dispatchEvent(
      new CustomEvent(WORKSPACE_SAVE_EVENT, {
        detail: workspaceStateRef.current,
      })
    );
    setIsDirty(false);
  }, []);

  /**
   * Restore workspace state from a saved state object
   */
  const restoreWorkspaceState = useCallback(
    (state: WorkspaceState) => {
      setWorkspaceState(state);

      // Navigate to saved page if different from current
      if (state.activePage && state.activePage !== location.pathname) {
        navigate(state.activePage);
      }

      setIsDirty(false);
    },
    [navigate, location.pathname]
  );

  /**
   * Mark workspace as having unsaved changes
   */
  const markDirty = useCallback(() => {
    setIsDirty(true);
  }, []);

  /**
   * Get current workspace state snapshot
   */
  const getWorkspaceState = useCallback((): WorkspaceState => {
    return workspaceStateRef.current;
  }, []);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (saveTimeoutRef.current) {
        clearTimeout(saveTimeoutRef.current);
      }
    };
  }, []);

  return {
    workspaceState,
    setTimelineViewport,
    setSelection,
    setPanelState,
    setPreviewState,
    setMediaLibraryState,
    saveWorkspaceState,
    restoreWorkspaceState,
    markDirty,
    isDirty,
    getWorkspaceState,
  };
}
