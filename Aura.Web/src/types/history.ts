/**
 * Types for the undo/redo history system
 */

export interface HistoryEntry {
  /** Unique identifier for this history entry */
  id: string;

  /** Type of action performed */
  actionType: HistoryActionType;

  /** Human-readable description */
  description: string;

  /** Timestamp when action was performed */
  timestamp: string;

  /** Data needed to undo this action */
  undoData: unknown;

  /** Data needed to redo this action */
  redoData: unknown;

  /** Which feature/area this affects */
  scope: HistoryScope;

  /** Optional grouping for compound actions */
  groupId?: string;

  /** Whether this entry marks a save point */
  isSavePoint?: boolean;
}

export type HistoryActionType =
  // Clip operations
  | 'clip:add'
  | 'clip:delete'
  | 'clip:move'
  | 'clip:resize'
  | 'clip:split'
  | 'clip:duplicate'
  | 'clip:properties'

  // Track operations
  | 'track:add'
  | 'track:delete'
  | 'track:reorder'
  | 'track:rename'
  | 'track:visibility'
  | 'track:lock'

  // Effect operations
  | 'effect:add'
  | 'effect:remove'
  | 'effect:modify'
  | 'effect:reorder'

  // Media operations
  | 'media:import'
  | 'media:delete'
  | 'media:rename'

  // Wizard operations
  | 'wizard:brief'
  | 'wizard:plan'
  | 'wizard:voice'
  | 'wizard:render'

  // Project operations
  | 'project:settings'
  | 'project:metadata'

  // Selection operations
  | 'selection:change'

  // Compound operations
  | 'compound:start'
  | 'compound:end';

export type HistoryScope =
  | 'timeline'
  | 'media-library'
  | 'effects'
  | 'wizard'
  | 'project'
  | 'global';

export interface HistoryState {
  /** All history entries (past actions) */
  entries: HistoryEntry[];

  /** Current position in history (for undo/redo) */
  currentIndex: number;

  /** Maximum number of entries to keep */
  maxEntries: number;

  /** Entries that were undone (for redo) */
  redoStack: HistoryEntry[];

  /** Save points for reference */
  savePoints: string[];
}

export interface HistoryConfig {
  /** Maximum history entries to keep */
  maxEntries: number;

  /** Whether to persist history with project */
  persistWithProject: boolean;

  /** Actions to exclude from history */
  excludeActions: HistoryActionType[];

  /** Debounce time for rapid changes (ms) */
  debounceMs: number;
}

/**
 * Specific undo/redo data types for each action
 */
export interface ClipUndoData {
  clipId: string;
  trackId: string;
  previousState: {
    startTime: number;
    duration: number;
    trackId: string;
    properties?: Record<string, unknown>;
  };
}

export interface ClipRedoData {
  clipId: string;
  trackId: string;
  newState: {
    startTime: number;
    duration: number;
    trackId: string;
    properties?: Record<string, unknown>;
  };
}

export interface TrackUndoData {
  trackId: string;
  previousState: {
    index: number;
    name: string;
    type: 'video' | 'audio';
    visible: boolean;
    locked: boolean;
  };
}

export interface TrackRedoData {
  trackId: string;
  newState: {
    index: number;
    name: string;
    type: 'video' | 'audio';
    visible: boolean;
    locked: boolean;
  };
}

export interface EffectUndoData {
  effectId: string;
  clipId: string;
  previousState?: Record<string, unknown>;
}

export interface EffectRedoData {
  effectId: string;
  clipId: string;
  newState: Record<string, unknown>;
}

export interface MediaUndoData {
  mediaId: string;
  previousData?: Record<string, unknown>;
}

export interface MediaRedoData {
  mediaId: string;
  newData: Record<string, unknown>;
}
