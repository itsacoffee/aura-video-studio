/**
 * Global Undo Manager Store
 * Manages application-wide undo/redo state using Zustand
 * Supports both client-side and server-side persistent undo operations
 */

import { create } from 'zustand';
import { recordAction, undoAction } from '../services/api/actionsApi';
import { Command, CommandHistory } from '../services/commandHistory';
import type { RecordActionRequest } from '../types/api-v1';

/**
 * Action history entry for display in UI
 */
export interface ActionHistoryEntry {
  id: string;
  description: string;
  timestamp: Date;
  canUndo: boolean;
  serverActionId?: string;
}

/**
 * Extended command interface with server persistence support
 */
export interface PersistableCommand extends Command {
  /**
   * Indicates if this command should be persisted to server
   */
  isPersistent?: boolean;

  /**
   * Action type for server-side action log
   */
  getActionType?: () => string;

  /**
   * Get payload for server persistence
   */
  getPayload?: () => string;

  /**
   * Get inverse payload for undo
   */
  getInversePayload?: () => string;

  /**
   * Affected resource IDs
   */
  getAffectedResourceIds?: () => string;

  /**
   * Store server action ID after recording
   */
  serverActionId?: string;
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

  // Server persistence enabled flag
  serverPersistenceEnabled: boolean;

  // Actions
  execute: (command: PersistableCommand) => Promise<void>;
  undo: () => Promise<void>;
  redo: () => void;
  clear: () => void;
  getUndoDescription: () => string | null;
  getRedoDescription: () => string | null;
  getHistory: () => ActionHistoryEntry[];
  toggleHistory: () => void;
  setHistoryVisible: (visible: boolean) => void;
  setServerPersistenceEnabled: (enabled: boolean) => void;
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
    serverPersistenceEnabled: true,

    execute: async (command: PersistableCommand) => {
      const { commandHistory, serverPersistenceEnabled } = get();

      commandHistory.execute(command);

      if (serverPersistenceEnabled && command.isPersistent && command.getActionType) {
        try {
          const request: RecordActionRequest = {
            userId: 'anonymous',
            actionType: command.getActionType(),
            description: command.getDescription(),
            affectedResourceIds: command.getAffectedResourceIds?.(),
            payloadJson: command.getPayload?.(),
            inversePayloadJson: command.getInversePayload?.(),
            isPersistent: true,
            canBatch: false,
          };

          const response = await recordAction(request);
          command.serverActionId = response.actionId;

          console.log(
            `Action ${command.getDescription()} recorded to server with ID ${response.actionId}`
          );
        } catch (error: unknown) {
          const errorObj = error instanceof Error ? error : new Error(String(error));
          console.error('Failed to record action to server:', errorObj.message);
        }
      }
    },

    undo: async () => {
      const { commandHistory, serverPersistenceEnabled } = get();

      const lastCommand = commandHistory['undoStack'][commandHistory['undoStack'].length - 1] as
        | PersistableCommand
        | undefined;

      const success = commandHistory.undo();
      if (!success) {
        console.warn('Nothing to undo');
        return;
      }

      if (serverPersistenceEnabled && lastCommand?.isPersistent && lastCommand?.serverActionId) {
        try {
          const response = await undoAction(lastCommand.serverActionId);
          if (response.success) {
            console.log(`Server action ${lastCommand.serverActionId} undone successfully`);
          } else {
            console.error(`Failed to undo server action: ${response.errorMessage}`);
          }
        } catch (error: unknown) {
          const errorObj = error instanceof Error ? error : new Error(String(error));
          console.error('Failed to undo server action:', errorObj.message);
        }
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

    setServerPersistenceEnabled: (enabled: boolean) => {
      set({ serverPersistenceEnabled: enabled });
      console.log(`Server persistence ${enabled ? 'enabled' : 'disabled'}`);
    },
  };
});
