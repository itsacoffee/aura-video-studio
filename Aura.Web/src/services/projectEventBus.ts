/**
 * Project Event Bus Service
 *
 * Provides a bridge between non-React code (like customEventHandlers.ts) and
 * the React project state. This allows menu commands and other event sources
 * to trigger project operations without direct access to React hooks.
 */

import { loggingService } from './loggingService';

/**
 * Handler function type for project save operations
 * Returns true if the save was successful, false otherwise
 */
export type ProjectSaveHandler = () => Promise<boolean>;

/**
 * Handler function type for save-as operations that may need user input
 * Returns the new project ID if successful, null otherwise
 */
export type ProjectSaveAsHandler = () => Promise<string | null>;

/**
 * Handler function type for checking if there's a project to save
 */
export type HasProjectHandler = () => boolean;

/**
 * Handler function type for showing "coming soon" or informational toasts
 */
export type ShowToastHandler = (
  message: string,
  intent: 'info' | 'warning' | 'success' | 'error'
) => void;

interface ProjectEventBusState {
  saveProjectHandler: ProjectSaveHandler | null;
  saveProjectAsHandler: ProjectSaveAsHandler | null;
  hasProjectHandler: HasProjectHandler | null;
  showToastHandler: ShowToastHandler | null;
}

const state: ProjectEventBusState = {
  saveProjectHandler: null,
  saveProjectAsHandler: null,
  hasProjectHandler: null,
  showToastHandler: null,
};

/**
 * Register a handler for saving the current project
 * @param handler - Function that saves the current project
 * @returns Cleanup function to unregister the handler
 */
export function registerSaveProjectHandler(handler: ProjectSaveHandler): () => void {
  state.saveProjectHandler = handler;
  loggingService.info('Save project handler registered');
  return () => {
    if (state.saveProjectHandler === handler) {
      state.saveProjectHandler = null;
      loggingService.info('Save project handler unregistered');
    }
  };
}

/**
 * Register a handler for saving the project with a new name
 * @param handler - Function that handles save-as operation
 * @returns Cleanup function to unregister the handler
 */
export function registerSaveProjectAsHandler(handler: ProjectSaveAsHandler): () => void {
  state.saveProjectAsHandler = handler;
  loggingService.info('Save project as handler registered');
  return () => {
    if (state.saveProjectAsHandler === handler) {
      state.saveProjectAsHandler = null;
      loggingService.info('Save project as handler unregistered');
    }
  };
}

/**
 * Register a handler to check if there's a project currently loaded
 * @param handler - Function that returns true if a project is loaded
 * @returns Cleanup function to unregister the handler
 */
export function registerHasProjectHandler(handler: HasProjectHandler): () => void {
  state.hasProjectHandler = handler;
  loggingService.info('Has project handler registered');
  return () => {
    if (state.hasProjectHandler === handler) {
      state.hasProjectHandler = null;
      loggingService.info('Has project handler unregistered');
    }
  };
}

/**
 * Register a handler for showing toast notifications
 * @param handler - Function that shows a toast notification
 * @returns Cleanup function to unregister the handler
 */
export function registerShowToastHandler(handler: ShowToastHandler): () => void {
  state.showToastHandler = handler;
  loggingService.info('Show toast handler registered');
  return () => {
    if (state.showToastHandler === handler) {
      state.showToastHandler = null;
      loggingService.info('Show toast handler unregistered');
    }
  };
}

/**
 * Execute the save project operation
 * @throws Error if no handler is registered
 */
export async function executeSaveProject(): Promise<boolean> {
  if (!state.saveProjectHandler) {
    loggingService.warn('No save project handler registered');
    showToast('No project is currently open', 'warning');
    return false;
  }

  // Check if there's actually a project to save
  if (state.hasProjectHandler && !state.hasProjectHandler()) {
    loggingService.info('Save project called but no project is loaded');
    showToast('No project to save - create or open a project first', 'info');
    return false;
  }

  try {
    const result = await state.saveProjectHandler();
    if (result) {
      showToast('Project saved successfully', 'success');
    }
    return result;
  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : String(error);
    loggingService.error('Failed to save project', new Error(errorMessage), 'projectEventBus');
    showToast(`Failed to save project: ${errorMessage}`, 'error');
    return false;
  }
}

/**
 * Execute the save project as operation
 * @throws Error if no handler is registered
 */
export async function executeSaveProjectAs(): Promise<string | null> {
  if (!state.saveProjectAsHandler) {
    loggingService.warn('No save project as handler registered');
    showToast('Save As functionality is not available', 'warning');
    return null;
  }

  try {
    const result = await state.saveProjectAsHandler();
    if (result) {
      showToast('Project saved with new name', 'success');
    }
    return result;
  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : String(error);
    loggingService.error('Failed to save project as', new Error(errorMessage), 'projectEventBus');
    showToast(`Failed to save project: ${errorMessage}`, 'error');
    return null;
  }
}

/**
 * Show a toast notification if a handler is registered
 */
export function showToast(
  message: string,
  intent: 'info' | 'warning' | 'success' | 'error' = 'info'
): void {
  if (state.showToastHandler) {
    state.showToastHandler(message, intent);
  } else {
    // Fallback to console if no toast handler is registered
    const logLevel = intent === 'error' ? 'error' : intent === 'warning' ? 'warn' : 'info';
    console[logLevel](`[Project Event Bus] ${message}`);
  }
}

/**
 * Check if any handlers are registered
 */
export function hasHandlers(): boolean {
  return (
    state.saveProjectHandler !== null ||
    state.saveProjectAsHandler !== null ||
    state.hasProjectHandler !== null
  );
}

/**
 * Get current handler registration status (for debugging)
 */
export function getHandlerStatus(): {
  saveProject: boolean;
  saveProjectAs: boolean;
  hasProject: boolean;
  showToast: boolean;
} {
  return {
    saveProject: state.saveProjectHandler !== null,
    saveProjectAs: state.saveProjectAsHandler !== null,
    hasProject: state.hasProjectHandler !== null,
    showToast: state.showToastHandler !== null,
  };
}
