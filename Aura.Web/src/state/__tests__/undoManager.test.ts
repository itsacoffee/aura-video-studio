/**
 * Tests for the global undo manager
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { Command } from '../../services/commandHistory';
import { useUndoManager } from '../undoManager';

// Test command implementation
class TestCommand implements Command {
  private executed = false;
  private timestamp: Date;

  constructor(
    private description: string,
    private executeCallback?: () => void,
    private undoCallback?: () => void
  ) {
    this.timestamp = new Date();
  }

  execute(): void {
    this.executed = true;
    this.executeCallback?.();
  }

  undo(): void {
    this.executed = false;
    this.undoCallback?.();
  }

  getDescription(): string {
    return this.description;
  }

  getTimestamp(): Date {
    return this.timestamp;
  }

  isExecuted(): boolean {
    return this.executed;
  }
}

describe('UndoManager', () => {
  beforeEach(() => {
    // Clear the undo manager before each test
    useUndoManager.getState().clear();
  });

  describe('Basic Operations', () => {
    it('should start with no undo/redo available', () => {
      const state = useUndoManager.getState();
      expect(state.canUndo).toBe(false);
      expect(state.canRedo).toBe(false);
    });

    it('should execute a command and enable undo', () => {
      const { execute } = useUndoManager.getState();
      const command = new TestCommand('Test Action');

      execute(command);

      expect(command.isExecuted()).toBe(true);
      expect(useUndoManager.getState().canUndo).toBe(true);
      expect(useUndoManager.getState().canRedo).toBe(false);
    });

    it('should undo a command', () => {
      const { execute, undo } = useUndoManager.getState();
      const command = new TestCommand('Test Action');

      execute(command);
      expect(command.isExecuted()).toBe(true);

      undo();
      expect(command.isExecuted()).toBe(false);
      expect(useUndoManager.getState().canUndo).toBe(false);
      expect(useUndoManager.getState().canRedo).toBe(true);
    });

    it('should redo a command', () => {
      const { execute, undo, redo } = useUndoManager.getState();
      const command = new TestCommand('Test Action');

      execute(command);
      undo();
      expect(command.isExecuted()).toBe(false);

      redo();
      expect(command.isExecuted()).toBe(true);
      expect(useUndoManager.getState().canUndo).toBe(true);
      expect(useUndoManager.getState().canRedo).toBe(false);
    });

    it('should clear redo stack when new command is executed', () => {
      const { execute, undo } = useUndoManager.getState();
      const command1 = new TestCommand('Action 1');
      const command2 = new TestCommand('Action 2');

      execute(command1);
      undo();
      expect(useUndoManager.getState().canRedo).toBe(true);

      execute(command2);
      expect(useUndoManager.getState().canRedo).toBe(false);
    });
  });

  describe('Command Descriptions', () => {
    it('should return correct undo description', () => {
      const { execute, getUndoDescription } = useUndoManager.getState();
      const command = new TestCommand('Test Action');

      execute(command);
      expect(getUndoDescription()).toBe('Test Action');
    });

    it('should return correct redo description', () => {
      const { execute, undo, getRedoDescription } = useUndoManager.getState();
      const command = new TestCommand('Test Action');

      execute(command);
      undo();
      expect(getRedoDescription()).toBe('Test Action');
    });

    it('should return null when no undo available', () => {
      const { getUndoDescription } = useUndoManager.getState();
      expect(getUndoDescription()).toBe(null);
    });

    it('should return null when no redo available', () => {
      const { getRedoDescription } = useUndoManager.getState();
      expect(getRedoDescription()).toBe(null);
    });
  });

  describe('History', () => {
    it('should track command history', () => {
      const { execute, getHistory } = useUndoManager.getState();

      execute(new TestCommand('Action 1'));
      execute(new TestCommand('Action 2'));
      execute(new TestCommand('Action 3'));

      const history = getHistory();
      expect(history).toHaveLength(3);
      expect(history[0].description).toBe('Action 3'); // Most recent first
      expect(history[1].description).toBe('Action 2');
      expect(history[2].description).toBe('Action 1');
    });

    it('should clear all history', () => {
      const { execute, clear, getHistory } = useUndoManager.getState();

      execute(new TestCommand('Action 1'));
      execute(new TestCommand('Action 2'));

      clear();

      expect(getHistory()).toHaveLength(0);
      expect(useUndoManager.getState().canUndo).toBe(false);
      expect(useUndoManager.getState().canRedo).toBe(false);
    });
  });

  describe('History Panel', () => {
    it('should toggle history visibility', () => {
      const { toggleHistory, showHistory } = useUndoManager.getState();

      expect(showHistory).toBe(false);

      toggleHistory();
      expect(useUndoManager.getState().showHistory).toBe(true);

      toggleHistory();
      expect(useUndoManager.getState().showHistory).toBe(false);
    });

    it('should set history visibility', () => {
      const { setHistoryVisible } = useUndoManager.getState();

      setHistoryVisible(true);
      expect(useUndoManager.getState().showHistory).toBe(true);

      setHistoryVisible(false);
      expect(useUndoManager.getState().showHistory).toBe(false);
    });
  });

  describe('Multiple Commands', () => {
    it('should handle multiple undo operations', () => {
      const { execute, undo } = useUndoManager.getState();
      const commands = [
        new TestCommand('Action 1'),
        new TestCommand('Action 2'),
        new TestCommand('Action 3'),
      ];

      commands.forEach((cmd) => execute(cmd));
      expect(commands.every((cmd) => cmd.isExecuted())).toBe(true);

      undo();
      expect(commands[2].isExecuted()).toBe(false);
      expect(commands[1].isExecuted()).toBe(true);

      undo();
      expect(commands[1].isExecuted()).toBe(false);
      expect(commands[0].isExecuted()).toBe(true);

      undo();
      expect(commands[0].isExecuted()).toBe(false);
    });

    it('should handle multiple redo operations', () => {
      const { execute, undo, redo } = useUndoManager.getState();
      const commands = [new TestCommand('Action 1'), new TestCommand('Action 2')];

      commands.forEach((cmd) => execute(cmd));
      commands.forEach(() => undo());

      expect(commands.every((cmd) => !cmd.isExecuted())).toBe(true);

      redo();
      expect(commands[0].isExecuted()).toBe(true);

      redo();
      expect(commands[1].isExecuted()).toBe(true);
    });
  });

  describe('Command Callbacks', () => {
    it('should call execute callback', () => {
      let executed = false;
      const command = new TestCommand('Test', () => {
        executed = true;
      });

      useUndoManager.getState().execute(command);
      expect(executed).toBe(true);
    });

    it('should call undo callback', () => {
      let undone = false;
      const command = new TestCommand('Test', undefined, () => {
        undone = true;
      });

      const { execute, undo } = useUndoManager.getState();
      execute(command);
      undo();
      expect(undone).toBe(true);
    });
  });
});
