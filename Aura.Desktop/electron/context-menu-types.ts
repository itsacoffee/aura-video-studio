/**
 * Context Menu Type Definitions
 *
 * This file defines TypeScript types for the context menu system used in Aura Video Studio.
 * These types are shared between the Electron main process and the React frontend.
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
 * Represents a single menu action item.
 */
export interface MenuAction {
  id: string;
  label: string;
  icon?: string;
  accelerator?: string;
  enabled?: boolean;
  visible?: boolean;
  checked?: boolean;
  type?: 'normal' | 'separator' | 'submenu' | 'checkbox' | 'radio';
  submenu?: MenuAction[];
  action?: string;
}

/**
 * Result of a context menu action.
 */
export interface ContextMenuResult {
  actionId: string;
  data: Record<string, unknown>;
}

/**
 * Response from showing a context menu.
 */
export interface ContextMenuShowResult {
  success: boolean;
  error?: string;
}
