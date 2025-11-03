/**
 * Type definitions for the global undo/redo system
 */

import { Command } from '../services/commandHistory';

/**
 * Extended metadata for undoable commands
 */
export interface CommandMetadata {
  id: string;
  userId?: string;
  timestamp: Date;
  label: string;
  affectedResourceIds?: string[];
  isPersistent: boolean;
  canBatch: boolean;
}

/**
 * Enhanced undoable command interface with metadata
 */
export interface UndoableCommand extends Command {
  metadata: CommandMetadata;
}

/**
 * Server-side action for persistent operations
 */
export interface ServerAction {
  actionId: string;
  actionType: string;
  resourceIds: string[];
  payload: unknown;
  inverseActionType?: string;
  status: 'applied' | 'undone' | 'failed';
}

/**
 * Configuration for undo manager
 */
export interface UndoManagerConfig {
  maxHistorySize: number;
  enableServerActions: boolean;
  enableNotifications: boolean;
}
