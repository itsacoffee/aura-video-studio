/**
 * Service for restoring complete session state when opening a project
 *
 * This service handles the restoration of the complete workspace state including:
 * - Navigation to the last active page
 * - Timeline zoom and scroll position
 * - Selected elements
 * - Panel configurations
 * - Playhead position
 * - Media library view state
 */

import { ProjectFile, WorkspaceState } from '../types/project';
import { loggingService } from './loggingService';
import { WORKSPACE_RESTORE_EVENT } from '../hooks/useWorkspaceState';

/**
 * Result of a session restoration attempt
 */
export interface SessionRestorationResult {
  /** Whether restoration was successful */
  success: boolean;
  /** Non-fatal issues encountered during restoration */
  warnings: string[];
  /** Fatal errors that prevented restoration */
  errors: string[];
}

/**
 * Custom events for specific restoration phases
 */
export const SESSION_EVENTS = {
  TIMELINE_RESTORE: 'timeline:restore',
  MEDIA_LIBRARY_RESTORE: 'mediaLibrary:restore',
  SELECTION_RESTORE: 'selection:restore',
  PLAYHEAD_RESTORE: 'playhead:restore',
} as const;

/**
 * Restore complete session state from a loaded project
 *
 * @param project - The loaded project file containing workspace state
 * @returns Result indicating success/failure and any issues
 */
export async function restoreSession(project: ProjectFile): Promise<SessionRestorationResult> {
  const warnings: string[] = [];
  const errors: string[] = [];

  loggingService.info('Starting session restoration', {
    projectName: project.metadata.name,
    hasWorkspace: !!project.workspace,
  });

  try {
    // 1. Restore workspace state (navigation, panels, etc.)
    if (project.workspace) {
      await restoreWorkspaceState(project.workspace);
    } else {
      warnings.push('No workspace state found, using defaults');
    }

    // 2. Restore timeline state
    await restoreTimelineState(project);

    // 3. Restore media library state
    await restoreMediaLibraryState(project);

    // 4. Restore selection state
    if (project.workspace?.selection) {
      await restoreSelectionState(project.workspace.selection);
    }

    // 5. Restore playhead position
    const playheadPosition =
      project.workspace?.preview?.playheadPosition ?? project.playerPosition ?? 0;
    await restorePlayheadPosition(playheadPosition);

    loggingService.info('Session restoration complete', {
      warnings: warnings.length,
      errors: errors.length,
    });

    return { success: true, warnings, errors };
  } catch (error: unknown) {
    const errorMessage = error instanceof Error ? error.message : 'Unknown error';
    errors.push(errorMessage);
    loggingService.error(
      'Session restoration failed',
      error instanceof Error ? error : new Error(errorMessage),
      'sessionRestoration'
    );
    return { success: false, warnings, errors };
  }
}

/**
 * Restore workspace state (triggers navigation and panel restoration)
 */
async function restoreWorkspaceState(state: WorkspaceState): Promise<void> {
  window.dispatchEvent(new CustomEvent(WORKSPACE_RESTORE_EVENT, { detail: state }));

  // Wait for navigation to complete
  await new Promise((resolve) => setTimeout(resolve, 100));
}

/**
 * Restore timeline state (tracks, clips, viewport)
 */
async function restoreTimelineState(project: ProjectFile): Promise<void> {
  window.dispatchEvent(
    new CustomEvent(SESSION_EVENTS.TIMELINE_RESTORE, {
      detail: {
        tracks: project.tracks,
        clips: project.clips,
        viewport: project.workspace?.timeline,
      },
    })
  );
}

/**
 * Restore media library state (items and view configuration)
 */
async function restoreMediaLibraryState(project: ProjectFile): Promise<void> {
  window.dispatchEvent(
    new CustomEvent(SESSION_EVENTS.MEDIA_LIBRARY_RESTORE, {
      detail: {
        items: project.mediaLibrary,
        state: project.workspace?.mediaLibrary,
      },
    })
  );
}

/**
 * Restore selection state (selected clips, tracks, media)
 */
async function restoreSelectionState(selection: WorkspaceState['selection']): Promise<void> {
  window.dispatchEvent(new CustomEvent(SESSION_EVENTS.SELECTION_RESTORE, { detail: selection }));
}

/**
 * Restore playhead position
 */
async function restorePlayheadPosition(position: number): Promise<void> {
  window.dispatchEvent(new CustomEvent(SESSION_EVENTS.PLAYHEAD_RESTORE, { detail: position }));
}

/**
 * Type guard to check if a project has workspace state
 */
export function hasWorkspaceState(
  project: ProjectFile
): project is ProjectFile & { workspace: WorkspaceState } {
  return project.workspace !== undefined;
}

/**
 * Validate workspace state structure
 * Returns true if the workspace state has all required fields
 */
export function isValidWorkspaceState(workspace: unknown): workspace is WorkspaceState {
  if (!workspace || typeof workspace !== 'object') {
    return false;
  }

  const ws = workspace as Record<string, unknown>;

  // Check required top-level properties
  if (typeof ws.activePage !== 'string') {
    return false;
  }

  // Check timeline
  if (!ws.timeline || typeof ws.timeline !== 'object') {
    return false;
  }
  const timeline = ws.timeline as Record<string, unknown>;
  if (typeof timeline.zoomLevel !== 'number') {
    return false;
  }
  if (
    !timeline.scrollPosition ||
    typeof (timeline.scrollPosition as Record<string, unknown>).x !== 'number'
  ) {
    return false;
  }

  // Check selection
  if (!ws.selection || typeof ws.selection !== 'object') {
    return false;
  }
  const selection = ws.selection as Record<string, unknown>;
  if (!Array.isArray(selection.clipIds) || !Array.isArray(selection.trackIds)) {
    return false;
  }

  // Check panels
  if (!ws.panels || typeof ws.panels !== 'object') {
    return false;
  }
  const panels = ws.panels as Record<string, unknown>;
  if (!Array.isArray(panels.openPanels) || !Array.isArray(panels.collapsedPanels)) {
    return false;
  }

  // Check preview
  if (!ws.preview || typeof ws.preview !== 'object') {
    return false;
  }
  const preview = ws.preview as Record<string, unknown>;
  if (typeof preview.playheadPosition !== 'number') {
    return false;
  }

  // Check mediaLibrary
  if (!ws.mediaLibrary || typeof ws.mediaLibrary !== 'object') {
    return false;
  }
  const mediaLib = ws.mediaLibrary as Record<string, unknown>;
  if (mediaLib.viewMode !== 'grid' && mediaLib.viewMode !== 'list') {
    return false;
  }

  return true;
}

/**
 * Register a handler for session restoration events
 *
 * @param eventType - The type of session event to listen for
 * @param handler - The handler function
 * @returns Cleanup function
 */
export function registerSessionEventHandler<T>(
  eventType: (typeof SESSION_EVENTS)[keyof typeof SESSION_EVENTS],
  handler: (data: T) => void
): () => void {
  const eventHandler = (event: Event) => {
    const customEvent = event as CustomEvent<T>;
    if (customEvent.detail !== undefined) {
      handler(customEvent.detail);
    }
  };

  window.addEventListener(eventType, eventHandler);
  return () => {
    window.removeEventListener(eventType, eventHandler);
  };
}
