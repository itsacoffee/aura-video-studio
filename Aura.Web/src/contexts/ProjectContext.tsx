/**
 * Project Context
 *
 * Provides global project state management across the application.
 * This context wraps the useProjectState hook functionality and makes it
 * available to the entire application, including non-React code via the
 * projectEventBus.
 */

import { useToastController, Toast, ToastTitle } from '@fluentui/react-components';
import { createContext, useContext, useEffect, useState, useCallback, useRef } from 'react';
import type { FC, ReactNode } from 'react';
import { ConfirmationDialog } from '../components/Dialogs/ConfirmationDialog';
import SaveProjectAsDialog from '../components/Dialogs/SaveProjectAsDialog';
import { WORKSPACE_STATE_CHANGED_EVENT, WORKSPACE_RESTORE_EVENT } from '../hooks/useWorkspaceState';
import { loggingService } from '../services/loggingService';
import {
  registerSaveProjectHandler,
  registerSaveProjectAsHandler,
  registerHasProjectHandler,
  registerShowToastHandler,
} from '../services/projectEventBus';
import {
  saveProject as saveProjectToBackend,
  saveToLocalStorage,
  loadFromLocalStorage,
  clearLocalStorage,
} from '../services/projectService';
import { ProjectFile, AutosaveStatus, createEmptyProject, WorkspaceState } from '../types/project';

/**
 * Project context state interface
 */
export interface ProjectContextState {
  // Current project state
  projectId: string | null;
  projectName: string | null;
  projectData: ProjectFile | null;
  isDirty: boolean;
  autosaveStatus: AutosaveStatus;
  lastSaved: Date | null;

  // Save As dialog state
  isSaveAsDialogOpen: boolean;
  openSaveAsDialog: () => void;
  closeSaveAsDialog: () => void;

  // Actions
  saveCurrentProject: (name?: string) => Promise<boolean>;
  saveProjectAs: (name: string) => Promise<string | null>;
  loadProject: (id: string) => Promise<void>;
  createNewProject: (name: string) => void;
  setProjectData: (data: ProjectFile) => void;
  markDirty: () => void;
  clearProject: () => void;

  // Workspace state integration
  /** Get current workspace state for saving */
  getWorkspaceState: () => WorkspaceState | undefined;
  /** Set workspace state after loading */
  setWorkspaceState: (state: WorkspaceState) => void;
}

const defaultContextState: ProjectContextState = {
  projectId: null,
  projectName: null,
  projectData: null,
  isDirty: false,
  autosaveStatus: 'idle',
  lastSaved: null,
  isSaveAsDialogOpen: false,
  openSaveAsDialog: () => {},
  closeSaveAsDialog: () => {},
  saveCurrentProject: async () => false,
  saveProjectAs: async () => null,
  loadProject: async () => {},
  createNewProject: () => {},
  setProjectData: () => {},
  markDirty: () => {},
  clearProject: () => {},
  getWorkspaceState: () => undefined,
  setWorkspaceState: () => {},
};

export const ProjectContext = createContext<ProjectContextState>(defaultContextState);

/**
 * Hook to access project context
 */
export function useProjectContext(): ProjectContextState {
  const context = useContext(ProjectContext);
  if (context === defaultContextState) {
    loggingService.warn(
      'useProjectContext called outside of ProjectProvider - using default state'
    );
  }
  return context;
}

/**
 * Props for ProjectProvider component
 */
interface ProjectProviderProps {
  children: ReactNode;
  autosaveInterval?: number; // milliseconds, default 120000 (2 minutes)
  enableAutosave?: boolean; // default true
}

/**
 * Provider component that wraps the application with project state
 */
export const ProjectProvider: FC<ProjectProviderProps> = ({
  children,
  autosaveInterval = 120000,
  enableAutosave = true,
}) => {
  const { dispatchToast } = useToastController('global');

  // Project state
  const [projectId, setProjectId] = useState<string | null>(null);
  const [projectName, setProjectName] = useState<string | null>(null);
  const [projectData, setProjectDataState] = useState<ProjectFile | null>(null);
  const [isDirty, setIsDirty] = useState(false);
  const [autosaveStatus, setAutosaveStatus] = useState<AutosaveStatus>('idle');
  const [lastSaved, setLastSaved] = useState<Date | null>(null);
  const [isSaveAsDialogOpen, setIsSaveAsDialogOpen] = useState(false);

  // Recovery dialog state
  const [isRecoveryDialogOpen, setIsRecoveryDialogOpen] = useState(false);
  const [pendingRecoveryData, setPendingRecoveryData] = useState<ProjectFile | null>(null);

  // Refs for autosave
  const autosaveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Save As dialog promise resolver
  const saveAsResolverRef = useRef<((result: string | null) => void) | null>(null);

  // Workspace state ref for integration with WorkspaceContext
  const currentWorkspaceRef = useRef<WorkspaceState | undefined>(undefined);

  /**
   * Helper function to show toast notifications
   */
  const showToast = useCallback(
    (message: string, intent: 'info' | 'warning' | 'success' | 'error') => {
      try {
        dispatchToast(
          <Toast>
            <ToastTitle>{message}</ToastTitle>
          </Toast>,
          { intent }
        );
      } catch (error) {
        console.warn('[ProjectContext] Toast dispatch failed:', error);
      }
    },
    [dispatchToast]
  );

  /**
   * Get current workspace state
   */
  const getWorkspaceState = useCallback((): WorkspaceState | undefined => {
    return currentWorkspaceRef.current;
  }, []);

  /**
   * Set workspace state (triggers restoration event)
   */
  const setWorkspaceState = useCallback((state: WorkspaceState) => {
    currentWorkspaceRef.current = state;
    window.dispatchEvent(
      new CustomEvent(WORKSPACE_RESTORE_EVENT, {
        detail: state,
      })
    );
  }, []);

  /**
   * Save the current project
   */
  const saveCurrentProject = useCallback(
    async (name?: string): Promise<boolean> => {
      if (!projectData) {
        loggingService.warn('No project data to save');
        return false;
      }

      const projectNameToUse = name || projectName || 'Untitled Project';
      setAutosaveStatus('saving');

      try {
        // Update project metadata and include workspace state
        const updatedProject: ProjectFile = {
          ...projectData,
          metadata: {
            ...projectData.metadata,
            name: projectNameToUse,
            lastModifiedAt: new Date().toISOString(),
          },
          // Include current workspace state for session restoration
          workspace: currentWorkspaceRef.current,
        };

        // Save to backend
        const response = await saveProjectToBackend(
          projectNameToUse,
          updatedProject,
          projectId || undefined
        );

        setProjectId(response.id);
        setProjectName(projectNameToUse);
        setProjectDataState(updatedProject);
        setIsDirty(false);
        setLastSaved(new Date());
        setAutosaveStatus('saved');

        // Also save to local storage for recovery
        saveToLocalStorage(updatedProject);

        loggingService.info('Project saved successfully', { projectId: response.id });

        // Reset status after 3 seconds
        setTimeout(() => {
          setAutosaveStatus('idle');
        }, 3000);

        return true;
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        loggingService.error('Failed to save project', new Error(errorMessage), 'ProjectContext');
        setAutosaveStatus('error');

        setTimeout(() => {
          setAutosaveStatus('idle');
        }, 5000);

        return false;
      }
    },
    [projectData, projectId, projectName]
  );

  /**
   * Open Save As dialog
   */
  const openSaveAsDialog = useCallback(() => {
    setIsSaveAsDialogOpen(true);
  }, []);

  /**
   * Close Save As dialog
   */
  const closeSaveAsDialog = useCallback(() => {
    setIsSaveAsDialogOpen(false);
    // Reject any pending promise
    if (saveAsResolverRef.current) {
      saveAsResolverRef.current(null);
      saveAsResolverRef.current = null;
    }
  }, []);

  /**
   * Save project with a new name (Save As)
   */
  const saveProjectAs = useCallback(
    async (name: string): Promise<string | null> => {
      if (!projectData && !name) {
        loggingService.warn('No project data to save as');
        return null;
      }

      const projectToSave = projectData || createEmptyProject(name);
      setAutosaveStatus('saving');

      try {
        // Update project metadata with new name - preserve original creation date for Save As
        const updatedProject: ProjectFile = {
          ...projectToSave,
          metadata: {
            ...projectToSave.metadata,
            name,
            lastModifiedAt: new Date().toISOString(),
            // Preserve original createdAt if exists, otherwise use current time for truly new projects
            createdAt: projectToSave.metadata.createdAt || new Date().toISOString(),
          },
          // Include current workspace state for session restoration
          workspace: currentWorkspaceRef.current,
        };

        // Save as new project (no ID means create new)
        const response = await saveProjectToBackend(name, updatedProject);

        setProjectId(response.id);
        setProjectName(name);
        setProjectDataState(updatedProject);
        setIsDirty(false);
        setLastSaved(new Date());
        setAutosaveStatus('saved');

        // Also save to local storage for recovery
        saveToLocalStorage(updatedProject);

        loggingService.info('Project saved as new', { projectId: response.id, name });

        // Reset status after 3 seconds
        setTimeout(() => {
          setAutosaveStatus('idle');
        }, 3000);

        return response.id;
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        loggingService.error(
          'Failed to save project as',
          new Error(errorMessage),
          'ProjectContext'
        );
        setAutosaveStatus('error');

        setTimeout(() => {
          setAutosaveStatus('idle');
        }, 5000);

        return null;
      }
    },
    [projectData]
  );

  /**
   * Load a project by ID
   */
  const loadProject = useCallback(async (id: string) => {
    try {
      const response = await fetch(`/api/project/${id}`);
      if (!response.ok) {
        throw new Error('Failed to load project');
      }

      const data = await response.json();
      const loadedProject = JSON.parse(data.projectData) as ProjectFile;

      setProjectId(data.id);
      setProjectName(loadedProject.metadata.name);
      setProjectDataState(loadedProject);
      setIsDirty(false);
      setLastSaved(new Date(data.lastModifiedAt));

      // Restore workspace state if available
      if (loadedProject.workspace) {
        currentWorkspaceRef.current = loadedProject.workspace;
        window.dispatchEvent(
          new CustomEvent(WORKSPACE_RESTORE_EVENT, {
            detail: loadedProject.workspace,
          })
        );
        loggingService.info('Project loaded with workspace state', { projectId: id });
      } else {
        loggingService.info('Project loaded (no workspace state)', { projectId: id });
      }
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : String(error);
      loggingService.error('Failed to load project', new Error(errorMessage), 'ProjectContext');
      throw error;
    }
  }, []);

  /**
   * Create a new empty project
   */
  const createNewProject = useCallback((name: string) => {
    const newProject = createEmptyProject(name);
    setProjectId(null);
    setProjectName(name);
    setProjectDataState(newProject);
    setIsDirty(false);
    setLastSaved(null);

    loggingService.info('New project created', { name });
  }, []);

  /**
   * Set project data directly
   */
  const setProjectData = useCallback((data: ProjectFile) => {
    setProjectDataState(data);
    setProjectName(data.metadata.name);
    setIsDirty(true);
  }, []);

  /**
   * Mark the project as dirty (has unsaved changes)
   */
  const markDirty = useCallback(() => {
    setIsDirty(true);
  }, []);

  /**
   * Clear the current project
   */
  const clearProject = useCallback(() => {
    setProjectId(null);
    setProjectName(null);
    setProjectDataState(null);
    setIsDirty(false);
    setLastSaved(null);
    clearLocalStorage();

    loggingService.info('Project cleared');
  }, []);

  // Register handlers with the event bus
  useEffect(() => {
    const cleanupSave = registerSaveProjectHandler(async () => {
      return await saveCurrentProject();
    });

    const cleanupSaveAs = registerSaveProjectAsHandler(async () => {
      return new Promise<string | null>((resolve) => {
        saveAsResolverRef.current = resolve;
        openSaveAsDialog();
      });
    });

    const cleanupHasProject = registerHasProjectHandler(() => {
      return projectData !== null || projectName !== null;
    });

    const cleanupToast = registerShowToastHandler(showToast);

    return () => {
      cleanupSave();
      cleanupSaveAs();
      cleanupHasProject();
      cleanupToast();
    };
  }, [saveCurrentProject, openSaveAsDialog, projectData, projectName, showToast]);

  // Listen for workspace state changes from WorkspaceContext
  useEffect(() => {
    const handleWorkspaceChange = (event: Event) => {
      const customEvent = event as CustomEvent<WorkspaceState>;
      if (customEvent.detail) {
        currentWorkspaceRef.current = customEvent.detail;
        setIsDirty(true);
      }
    };

    window.addEventListener(WORKSPACE_STATE_CHANGED_EVENT, handleWorkspaceChange);
    return () => {
      window.removeEventListener(WORKSPACE_STATE_CHANGED_EVENT, handleWorkspaceChange);
    };
  }, []);

  // Autosave functionality
  useEffect(() => {
    if (!enableAutosave || !isDirty || !projectName) {
      return;
    }

    // Clear existing timer
    if (autosaveTimerRef.current) {
      clearTimeout(autosaveTimerRef.current);
    }

    // Set new timer
    autosaveTimerRef.current = setTimeout(() => {
      if (isDirty && projectName) {
        saveCurrentProject(projectName).catch((error) => {
          loggingService.error(
            'Autosave failed',
            error instanceof Error ? error : new Error(String(error)),
            'ProjectContext'
          );
        });
      }
    }, autosaveInterval);

    return () => {
      if (autosaveTimerRef.current) {
        clearTimeout(autosaveTimerRef.current);
      }
    };
  }, [isDirty, projectName, autosaveInterval, enableAutosave, saveCurrentProject]);

  // Load autosave on mount if no project is loaded
  useEffect(() => {
    if (!projectId && !projectName) {
      const autosaved = loadFromLocalStorage();
      if (autosaved) {
        // Store the autosaved data and show recovery dialog
        setPendingRecoveryData(autosaved);
        setIsRecoveryDialogOpen(true);
      }
    }
    // Run only once on mount
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  /**
   * Handle recovery dialog confirm
   */
  const handleRecoveryConfirm = useCallback(() => {
    if (pendingRecoveryData) {
      setProjectDataState(pendingRecoveryData);
      setProjectName(pendingRecoveryData.metadata.name);
      setIsDirty(true);
      loggingService.info('Recovered autosaved project');
    }
    setPendingRecoveryData(null);
    setIsRecoveryDialogOpen(false);
  }, [pendingRecoveryData]);

  /**
   * Handle recovery dialog cancel
   */
  const handleRecoveryCancel = useCallback(() => {
    clearLocalStorage();
    setPendingRecoveryData(null);
    setIsRecoveryDialogOpen(false);
    loggingService.info('User declined autosave recovery, cleared localStorage');
  }, []);

  /**
   * Handle Save As dialog completion - called from the dialog
   */
  const handleSaveAsDialogComplete = useCallback(
    async (name: string) => {
      const result = await saveProjectAs(name);
      if (saveAsResolverRef.current) {
        saveAsResolverRef.current(result);
        saveAsResolverRef.current = null;
      }
      closeSaveAsDialog();
    },
    [saveProjectAs, closeSaveAsDialog]
  );

  const contextValue: ProjectContextState = {
    projectId,
    projectName,
    projectData,
    isDirty,
    autosaveStatus,
    lastSaved,
    isSaveAsDialogOpen,
    openSaveAsDialog,
    closeSaveAsDialog,
    saveCurrentProject,
    saveProjectAs,
    loadProject,
    createNewProject,
    setProjectData,
    markDirty,
    clearProject,
    getWorkspaceState,
    setWorkspaceState,
  };

  return (
    <ProjectContext.Provider value={contextValue}>
      {children}
      <SaveProjectAsDialog
        isOpen={isSaveAsDialogOpen}
        onClose={closeSaveAsDialog}
        onSave={handleSaveAsDialogComplete}
        currentName={projectName || undefined}
        currentDescription={projectData?.metadata?.description}
      />
      <ConfirmationDialog
        open={isRecoveryDialogOpen}
        onOpenChange={setIsRecoveryDialogOpen}
        title="Recover Autosaved Project"
        message={`An autosaved project "${pendingRecoveryData?.metadata?.name || 'Untitled'}" was found. Would you like to recover it?`}
        confirmLabel="Recover"
        cancelLabel="Discard"
        variant="info"
        onConfirm={handleRecoveryConfirm}
        onCancel={handleRecoveryCancel}
      />
    </ProjectContext.Provider>
  );
};

export default ProjectProvider;
