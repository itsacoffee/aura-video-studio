/**
 * Electron Context Menu Type Definitions for Frontend
 *
 * This file provides TypeScript types for the context menu API exposed by the Electron preload script.
 * Import these types in React components that need to interact with context menus.
 */

/**
 * All supported context menu types in the application.
 */
export type ContextMenuType =
  | 'timeline-clip'
  | 'timeline-track'
  | 'timeline-empty'
  | 'media-asset'
  | 'ai-script'
  | 'job-queue'
  | 'preview-window'
  | 'ai-provider';

/**
 * Base context menu data structure.
 */
export interface ContextMenuData {
  type: ContextMenuType;
  position: { x: number; y: number };
  metadata: Record<string, unknown>;
}

/**
 * Data for timeline clip context menu.
 */
export interface TimelineClipMenuData {
  clipId: string;
  clipType: 'video' | 'audio' | 'image';
  startTime: number;
  duration: number;
  trackId: string;
  isLocked: boolean;
  hasAudio: boolean;
  hasClipboardData?: boolean;
}

/**
 * Data for timeline track context menu.
 */
export interface TimelineTrackMenuData {
  trackId: string;
  trackType: 'video' | 'audio' | 'overlay';
  isLocked: boolean;
  isMuted: boolean;
  isSolo: boolean;
  trackIndex: number;
  totalTracks: number;
}

/**
 * Data for empty timeline area context menu.
 */
export interface TimelineEmptyMenuData {
  timePosition: number;
  trackId?: string;
  hasClipboardData: boolean;
}

/**
 * Data for media asset context menu.
 */
export interface MediaAssetMenuData {
  assetId: string;
  assetType: 'video' | 'audio' | 'image';
  filePath: string;
  isFavorite: boolean;
  tags: string[];
}

/**
 * Data for AI script context menu.
 */
export interface AIScriptMenuData {
  sceneIndex: number;
  sceneText: string;
  jobId: string;
}

/**
 * Data for job queue context menu.
 */
export interface JobQueueMenuData {
  jobId: string;
  status: 'queued' | 'running' | 'paused' | 'completed' | 'failed' | 'canceled';
  outputPath?: string;
}

/**
 * Data for preview window context menu.
 */
export interface PreviewWindowMenuData {
  currentTime: number;
  duration: number;
  isPlaying: boolean;
  zoom: number;
}

/**
 * Data for AI provider context menu.
 */
export interface AIProviderMenuData {
  providerId: string;
  providerType: 'llm' | 'tts' | 'image';
  isDefault: boolean;
  hasFallback: boolean;
}

/**
 * Result of showing a context menu.
 */
export interface ContextMenuShowResult {
  success: boolean;
  error?: string;
}

/**
 * Callback data received from context menu actions.
 * Extends the original menu data with any additional action arguments.
 */
export interface ContextMenuActionData {
  actionArgs?: unknown[];
  [key: string]: unknown;
}

/**
 * Context Menu API exposed by the Electron preload script.
 */
export interface ContextMenuAPI {
  /**
   * Show a context menu of the specified type.
   * @param type - The type of context menu to show
   * @param data - Data specific to the menu type
   * @returns Promise resolving to the result of showing the menu
   */
  show: <T = unknown>(type: ContextMenuType, data: T) => Promise<ContextMenuShowResult>;

  /**
   * Register a listener for context menu actions.
   * @param type - The context menu type to listen for
   * @param actionType - The specific action type (e.g., 'onCut', 'onCopy')
   * @param callback - Function to call when the action is triggered
   * @returns Unsubscribe function to remove the listener
   */
  onAction: <T = ContextMenuActionData>(
    type: ContextMenuType,
    actionType: string,
    callback: (data: T) => void
  ) => () => void;

  /**
   * Reveal a file or folder in the OS file explorer.
   * @param filePath - Path to the file or folder
   */
  revealInOS: (filePath: string) => Promise<{ success: boolean; error?: string }>;

  /**
   * Open a file or path with the default system application.
   * @param filePath - Path to the file or folder
   */
  openPath: (filePath: string) => Promise<{ success: boolean; error?: string }>;
}
