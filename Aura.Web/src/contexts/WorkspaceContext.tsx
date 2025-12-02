/**
 * Global workspace state context
 * Provides workspace state management throughout the application
 *
 * This context wraps the useWorkspaceState hook and makes workspace state
 * available to all components. It handles:
 * - Tracking current page/route
 * - Managing timeline viewport state
 * - Managing element selection
 * - Managing panel layouts
 * - Managing preview/player state
 * - Managing media library view state
 */

import React, { createContext, useContext, useEffect, useRef } from 'react';
import { useWorkspaceState, WORKSPACE_RESTORE_EVENT } from '../hooks/useWorkspaceState';
import { WorkspaceState } from '../types/project';

/**
 * Context value interface
 */
export interface WorkspaceContextValue {
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

  /** Restore workspace state from project */
  restoreWorkspaceState: (state: WorkspaceState) => void;

  /** Get current workspace state snapshot */
  getWorkspaceState: () => WorkspaceState;

  /** Whether workspace has unsaved changes */
  isDirty: boolean;
}

const WorkspaceContext = createContext<WorkspaceContextValue | null>(null);

/**
 * Props for WorkspaceProvider
 */
interface WorkspaceProviderProps {
  children: React.ReactNode;
  /** Optional debounce delay for auto-saving state changes (ms) */
  saveDebounce?: number;
}

/**
 * Provider component that makes workspace state available throughout the app
 *
 * @example
 * ```tsx
 * function App() {
 *   return (
 *     <WorkspaceProvider>
 *       <Router>
 *         <MainContent />
 *       </Router>
 *     </WorkspaceProvider>
 *   );
 * }
 * ```
 */
export function WorkspaceProvider({
  children,
  saveDebounce = 1000,
}: WorkspaceProviderProps): React.ReactElement {
  const workspace = useWorkspaceState({ saveDebounce });
  const restoreWorkspaceStateRef = useRef(workspace.restoreWorkspaceState);

  // Keep ref in sync
  useEffect(() => {
    restoreWorkspaceStateRef.current = workspace.restoreWorkspaceState;
  }, [workspace.restoreWorkspaceState]);

  // Listen for workspace restore events from ProjectContext
  useEffect(() => {
    const handleRestore = (event: Event) => {
      const customEvent = event as CustomEvent<WorkspaceState>;
      if (customEvent.detail) {
        restoreWorkspaceStateRef.current(customEvent.detail);
      }
    };

    window.addEventListener(WORKSPACE_RESTORE_EVENT, handleRestore);
    return () => {
      window.removeEventListener(WORKSPACE_RESTORE_EVENT, handleRestore);
    };
  }, []);

  const contextValue: WorkspaceContextValue = {
    workspaceState: workspace.workspaceState,
    setTimelineViewport: workspace.setTimelineViewport,
    setSelection: workspace.setSelection,
    setPanelState: workspace.setPanelState,
    setPreviewState: workspace.setPreviewState,
    setMediaLibraryState: workspace.setMediaLibraryState,
    restoreWorkspaceState: workspace.restoreWorkspaceState,
    getWorkspaceState: workspace.getWorkspaceState,
    isDirty: workspace.isDirty,
  };

  return <WorkspaceContext.Provider value={contextValue}>{children}</WorkspaceContext.Provider>;
}

/**
 * Hook to access workspace context
 *
 * @throws Error if used outside of WorkspaceProvider
 *
 * @example
 * ```tsx
 * function Timeline() {
 *   const { workspaceState, setTimelineViewport, setSelection } = useWorkspace();
 *
 *   const handleZoomChange = (newZoom: number) => {
 *     setTimelineViewport({ zoomLevel: newZoom });
 *   };
 *
 *   return <div>Zoom: {workspaceState.timeline.zoomLevel}</div>;
 * }
 * ```
 */
export function useWorkspace(): WorkspaceContextValue {
  const context = useContext(WorkspaceContext);
  if (!context) {
    throw new Error('useWorkspace must be used within a WorkspaceProvider');
  }
  return context;
}

/**
 * Optional hook that returns undefined instead of throwing if used outside provider
 * Useful for components that may be rendered outside the workspace context
 */
export function useWorkspaceOptional(): WorkspaceContextValue | undefined {
  return useContext(WorkspaceContext) ?? undefined;
}

/**
 * Register a handler for workspace restore events
 * Useful for components that need to respond to workspace restoration
 *
 * @param handler - Function to call when workspace is restored
 * @returns Cleanup function to unregister the handler
 */
export function registerWorkspaceRestoreHandler(
  handler: (state: WorkspaceState) => void
): () => void {
  const eventHandler = (event: Event) => {
    const customEvent = event as CustomEvent<WorkspaceState>;
    if (customEvent.detail) {
      handler(customEvent.detail);
    }
  };

  window.addEventListener(WORKSPACE_RESTORE_EVENT, eventHandler);
  return () => {
    window.removeEventListener(WORKSPACE_RESTORE_EVENT, eventHandler);
  };
}
