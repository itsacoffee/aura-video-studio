/**
 * History Service for managing undo/redo operations
 */

import type {
  HistoryEntry,
  HistoryState,
  HistoryConfig,
  HistoryActionType,
  HistoryScope,
} from '../types/history';
import { loggingService } from './loggingService';

const DEFAULT_CONFIG: HistoryConfig = {
  maxEntries: 100,
  persistWithProject: true,
  excludeActions: ['selection:change'],
  debounceMs: 300,
};

/**
 * Creates a new UUID for history entries
 */
function generateUUID(): string {
  if (typeof crypto !== 'undefined' && crypto.randomUUID) {
    return crypto.randomUUID();
  }
  // Fallback for environments without crypto.randomUUID
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === 'x' ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

class HistoryService {
  private state: HistoryState;
  private config: HistoryConfig;
  private debounceTimer: ReturnType<typeof setTimeout> | null = null;
  private pendingEntry: Partial<HistoryEntry> | null = null;
  private compoundGroupId: string | null = null;
  private listeners: Set<(state: HistoryState) => void> = new Set();
  private logger = loggingService.createLogger('HistoryService');

  constructor(config: Partial<HistoryConfig> = {}) {
    this.config = { ...DEFAULT_CONFIG, ...config };
    this.state = {
      entries: [],
      currentIndex: -1,
      maxEntries: this.config.maxEntries,
      redoStack: [],
      savePoints: [],
    };
  }

  /**
   * Initialize with existing history (from loaded project)
   */
  initialize(state?: Partial<HistoryState>): void {
    if (state) {
      this.state = {
        ...this.state,
        ...state,
        entries: state.entries || [],
        currentIndex: state.currentIndex ?? -1,
      };
    } else {
      this.clear();
    }
    this.notifyListeners();
  }

  /**
   * Record a new action in history
   */
  record(
    actionType: HistoryActionType,
    description: string,
    undoData: unknown,
    redoData: unknown,
    scope: HistoryScope = 'global'
  ): void {
    // Check if action should be excluded
    if (this.config.excludeActions.includes(actionType)) {
      return;
    }

    const entry: HistoryEntry = {
      id: generateUUID(),
      actionType,
      description,
      timestamp: new Date().toISOString(),
      undoData,
      redoData,
      scope,
      groupId: this.compoundGroupId || undefined,
    };

    // Handle debouncing for rapid changes
    if (this.shouldDebounce(actionType)) {
      this.pendingEntry = entry;
      this.scheduleDebouncedRecord();
      return;
    }

    this.addEntry(entry);
  }

  /**
   * Start a compound action (multiple operations as one undo step)
   */
  startCompound(description: string): string {
    const groupId = generateUUID();
    this.compoundGroupId = groupId;

    // Record start marker
    this.record('compound:start', description, { groupId }, { groupId }, 'global');

    return groupId;
  }

  /**
   * End a compound action
   */
  endCompound(): void {
    if (this.compoundGroupId) {
      this.record(
        'compound:end',
        'End compound action',
        { groupId: this.compoundGroupId },
        { groupId: this.compoundGroupId },
        'global'
      );
      this.compoundGroupId = null;
    }
  }

  /**
   * Undo the last action
   */
  undo(): HistoryEntry | null {
    if (!this.canUndo()) {
      this.logger.warn('Cannot undo: at beginning of history');
      return null;
    }

    // Handle compound actions
    const entries = this.getUndoEntries();

    for (const entry of entries) {
      this.executeUndo(entry);
    }

    // Move current index back
    this.state.currentIndex -= entries.length;

    // Add to redo stack
    this.state.redoStack.push(...entries.reverse());

    this.notifyListeners();

    return entries.length > 0 ? entries[entries.length - 1] : null;
  }

  /**
   * Redo the last undone action
   */
  redo(): HistoryEntry | null {
    if (!this.canRedo()) {
      this.logger.warn('Cannot redo: at end of history');
      return null;
    }

    const entry = this.state.redoStack.pop();
    if (!entry) {
      return null;
    }

    this.executeRedo(entry);

    // Move current index forward
    this.state.currentIndex += 1;

    this.notifyListeners();

    return entry;
  }

  /**
   * Check if undo is available
   */
  canUndo(): boolean {
    return this.state.currentIndex >= 0 && this.state.entries.length > 0;
  }

  /**
   * Check if redo is available
   */
  canRedo(): boolean {
    return this.state.redoStack.length > 0;
  }

  /**
   * Get description of the action that would be undone
   */
  getUndoDescription(): string | null {
    if (!this.canUndo()) {
      return null;
    }
    const entry = this.state.entries[this.state.currentIndex];
    return entry?.description || null;
  }

  /**
   * Get description of the action that would be redone
   */
  getRedoDescription(): string | null {
    if (!this.canRedo()) {
      return null;
    }
    const entry = this.state.redoStack[this.state.redoStack.length - 1];
    return entry?.description || null;
  }

  /**
   * Get all history entries
   */
  getHistory(): HistoryEntry[] {
    return [...this.state.entries].reverse();
  }

  /**
   * Get current state for persistence
   */
  getState(): HistoryState {
    return { ...this.state };
  }

  /**
   * Mark current position as a save point
   */
  markSavePoint(): void {
    if (this.state.currentIndex >= 0) {
      const entry = this.state.entries[this.state.currentIndex];
      if (entry) {
        entry.isSavePoint = true;
        this.state.savePoints.push(entry.id);
        this.notifyListeners();
      }
    }
  }

  /**
   * Clear all history
   */
  clear(): void {
    this.state = {
      entries: [],
      currentIndex: -1,
      maxEntries: this.config.maxEntries,
      redoStack: [],
      savePoints: [],
    };
    this.compoundGroupId = null;
    this.pendingEntry = null;
    if (this.debounceTimer) {
      clearTimeout(this.debounceTimer);
      this.debounceTimer = null;
    }
    this.notifyListeners();
  }

  /**
   * Subscribe to history state changes
   */
  subscribe(listener: (state: HistoryState) => void): () => void {
    this.listeners.add(listener);

    // Immediately notify the listener of current state
    try {
      listener(this.getState());
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      this.logger.error('Error in history listener:', errorObj);
    }

    return () => {
      this.listeners.delete(listener);
    };
  }

  /**
   * Get history size
   */
  getHistorySize(): number {
    return this.state.entries.length;
  }

  /**
   * Update configuration
   */
  setConfig(config: Partial<HistoryConfig>): void {
    this.config = { ...this.config, ...config };

    // Update max entries in state
    if (config.maxEntries !== undefined) {
      this.state.maxEntries = config.maxEntries;
      this.trimHistory();
    }

    this.notifyListeners();
  }

  /**
   * Get current configuration
   */
  getConfig(): HistoryConfig {
    return { ...this.config };
  }

  // Private methods

  private addEntry(entry: HistoryEntry): void {
    // Clear redo stack when new action is recorded
    this.state.redoStack = [];

    // Add entry
    this.state.entries.push(entry);
    this.state.currentIndex = this.state.entries.length - 1;

    // Trim if exceeding max
    this.trimHistory();

    this.logger.info('History entry added', 'record', {
      actionType: entry.actionType,
      description: entry.description,
      historySize: this.state.entries.length,
    });

    this.notifyListeners();
  }

  private trimHistory(): void {
    while (this.state.entries.length > this.state.maxEntries) {
      this.state.entries.shift();
      this.state.currentIndex = Math.max(-1, this.state.currentIndex - 1);
    }
  }

  private shouldDebounce(actionType: HistoryActionType): boolean {
    // Debounce certain rapid changes
    const debounceableActions: HistoryActionType[] = ['clip:resize', 'clip:move', 'effect:modify'];
    return debounceableActions.includes(actionType);
  }

  private scheduleDebouncedRecord(): void {
    if (this.debounceTimer) {
      clearTimeout(this.debounceTimer);
    }

    this.debounceTimer = setTimeout(() => {
      if (this.pendingEntry) {
        this.addEntry(this.pendingEntry as HistoryEntry);
        this.pendingEntry = null;
      }
      this.debounceTimer = null;
    }, this.config.debounceMs);
  }

  private getUndoEntries(): HistoryEntry[] {
    if (this.state.currentIndex < 0) {
      return [];
    }

    const entry = this.state.entries[this.state.currentIndex];
    if (!entry) {
      return [];
    }

    // If this is part of a compound action, get all entries in the group
    // Handle compound:end by moving through the group back to compound:start
    if (
      entry.groupId &&
      entry.actionType !== 'compound:start' &&
      entry.actionType !== 'compound:end'
    ) {
      const groupEntries: HistoryEntry[] = [];
      let index = this.state.currentIndex;

      while (index >= 0) {
        const currentEntry = this.state.entries[index];
        if (currentEntry.groupId === entry.groupId) {
          groupEntries.push(currentEntry);
          if (currentEntry.actionType === 'compound:start') {
            break;
          }
        }
        index--;
      }

      return groupEntries;
    }

    // If at compound:end, collect all entries back to compound:start
    if (entry.actionType === 'compound:end' && entry.groupId) {
      const groupEntries: HistoryEntry[] = [];
      let index = this.state.currentIndex;

      while (index >= 0) {
        const currentEntry = this.state.entries[index];
        if (currentEntry.groupId === entry.groupId) {
          groupEntries.push(currentEntry);
          if (currentEntry.actionType === 'compound:start') {
            break;
          }
        }
        index--;
      }

      return groupEntries;
    }

    return [entry];
  }

  private executeUndo(entry: HistoryEntry): void {
    this.logger.info('Executing undo', 'undo', {
      actionType: entry.actionType,
      description: entry.description,
    });
    // State changes are applied by consumers via subscription to history state.
    // Consumers access undoData from the entry and apply changes to their respective stores.
  }

  private executeRedo(entry: HistoryEntry): void {
    this.logger.info('Executing redo', 'redo', {
      actionType: entry.actionType,
      description: entry.description,
    });
    // State changes are applied by consumers via subscription to history state.
    // Consumers access redoData from the entry and apply changes to their respective stores.
  }

  private notifyListeners(): void {
    const state = this.getState();
    this.listeners.forEach((listener) => {
      try {
        listener(state);
      } catch (error: unknown) {
        const errorObj = error instanceof Error ? error : new Error(String(error));
        this.logger.error('Error in history listener:', errorObj);
      }
    });
  }
}

// Export singleton instance
export const historyService = new HistoryService();

// Export class for testing or custom instances
export { HistoryService };
