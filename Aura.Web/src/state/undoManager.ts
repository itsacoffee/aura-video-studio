/**
 * Global Undo Manager Store
 * Manages application-wide undo/redo state using Zustand
 */

import { create } from 'zustand';
import { Command, CommandHistory } from '../services/commandHistory';

/**
 * Action history entry for display in UI
 */
export interface ActionHistoryEntry {
  id: string;
  description: string;
  timestamp: Date;
  canUndo: boolean;
}

/**
 * Undo manager state and actions
 */
interface UndoManagerState {
  // Command history instance
  commandHistory: CommandHistory;

  // State flags
  canUndo: boolean;
  canRedo: boolean;

  // UI state
  showHistory: boolean;

  // Actions
  execute: (command: Command) => void;
  undo: () => void;
  redo: () => void;
  clear: () => void;
  getUndoDescription: () => string | null;
  getRedoDescription: () => string | null;
  getHistory: () => ActionHistoryEntry[];
  toggleHistory: () => void;
  setHistoryVisible: (visible: boolean) => void;
}

/**
 * Global undo manager store
 */
export const useUndoManager = create<UndoManagerState>((set, get) => {
  const commandHistory = new CommandHistory(100); // Max 100 actions

  // Subscribe to command history changes to update state
  commandHistory.subscribe((canUndo, canRedo) => {
    set({ canUndo, canRedo });
  });

  return {
    commandHistory,
    canUndo: false,
    canRedo: false,
    showHistory: false,

    execute: (command: Command) => {
      const { commandHistory } = get();
      commandHistory.execute(command);
    },

    undo: () => {
      const { commandHistory } = get();
      const success = commandHistory.undo();
      if (!success) {
        console.warn('Nothing to undo');
      }
    },

    redo: () => {
      const { commandHistory } = get();
      const success = commandHistory.redo();
      if (!success) {
        console.warn('Nothing to redo');
      }
    },

    clear: () => {
      const { commandHistory } = get();
      commandHistory.clear();
    },

    getUndoDescription: () => {
      const { commandHistory } = get();
      return commandHistory.getUndoDescription();
    },

    getRedoDescription: () => {
      const { commandHistory } = get();
      return commandHistory.getRedoDescription();
    },

    getHistory: () => {
      const { commandHistory } = get();
      const history = commandHistory.getUndoHistory();
      return history.map((entry, index) => ({
        id: `action-${index}`,
        description: entry.description,
        timestamp: entry.timestamp,
        canUndo: true,
      }));
    },

    toggleHistory: () => {
      set((state) => ({ showHistory: !state.showHistory }));
    },

    setHistoryVisible: (visible: boolean) => {
      set({ showHistory: visible });
    },
  };
});
