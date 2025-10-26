/**
 * Tests for Command History Service
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { CommandHistory, Command, BatchCommandImpl } from '../commandHistory';

// Mock command implementation for testing
class MockCommand implements Command {
  private executed = false;
  private timestamp: Date;

  constructor(
    public description: string,
    public executeAction: () => void = () => {},
    public undoAction: () => void = () => {}
  ) {
    this.timestamp = new Date();
  }

  execute(): void {
    this.executeAction();
    this.executed = true;
  }

  undo(): void {
    this.undoAction();
    this.executed = false;
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

describe('CommandHistory', () => {
  let commandHistory: CommandHistory;

  beforeEach(() => {
    commandHistory = new CommandHistory();
  });

  describe('Basic Operations', () => {
    it('should execute a command', () => {
      const executeFn = vi.fn();
      const command = new MockCommand('Test Command', executeFn);

      commandHistory.execute(command);

      expect(executeFn).toHaveBeenCalledTimes(1);
      expect(commandHistory.canUndo()).toBe(true);
      expect(commandHistory.canRedo()).toBe(false);
    });

    it('should undo a command', () => {
      const executeFn = vi.fn();
      const undoFn = vi.fn();
      const command = new MockCommand('Test Command', executeFn, undoFn);

      commandHistory.execute(command);
      const undoResult = commandHistory.undo();

      expect(undoResult).toBe(true);
      expect(undoFn).toHaveBeenCalledTimes(1);
      expect(commandHistory.canUndo()).toBe(false);
      expect(commandHistory.canRedo()).toBe(true);
    });

    it('should redo a command', () => {
      const executeFn = vi.fn();
      const command = new MockCommand('Test Command', executeFn);

      commandHistory.execute(command);
      commandHistory.undo();
      const redoResult = commandHistory.redo();

      expect(redoResult).toBe(true);
      expect(executeFn).toHaveBeenCalledTimes(2); // Once on execute, once on redo
      expect(commandHistory.canUndo()).toBe(true);
      expect(commandHistory.canRedo()).toBe(false);
    });

    it('should return false when trying to undo with empty stack', () => {
      const result = commandHistory.undo();
      expect(result).toBe(false);
    });

    it('should return false when trying to redo with empty stack', () => {
      const result = commandHistory.redo();
      expect(result).toBe(false);
    });
  });

  describe('Command Stack Management', () => {
    it('should clear redo stack when new command is executed', () => {
      const cmd1 = new MockCommand('Command 1');
      const cmd2 = new MockCommand('Command 2');
      const cmd3 = new MockCommand('Command 3');

      commandHistory.execute(cmd1);
      commandHistory.execute(cmd2);
      commandHistory.undo();

      expect(commandHistory.canRedo()).toBe(true);

      commandHistory.execute(cmd3);

      expect(commandHistory.canRedo()).toBe(false);
    });

    it('should maintain multiple undo/redo operations', () => {
      const cmd1 = new MockCommand('Command 1');
      const cmd2 = new MockCommand('Command 2');
      const cmd3 = new MockCommand('Command 3');

      commandHistory.execute(cmd1);
      commandHistory.execute(cmd2);
      commandHistory.execute(cmd3);

      expect(commandHistory.canUndo()).toBe(true);
      expect(commandHistory.getHistorySize()).toBe(3);

      commandHistory.undo();
      expect(commandHistory.getHistorySize()).toBe(2);

      commandHistory.undo();
      expect(commandHistory.getHistorySize()).toBe(1);

      commandHistory.redo();
      expect(commandHistory.getHistorySize()).toBe(2);
    });

    it('should enforce maximum history size', () => {
      const smallHistory = new CommandHistory(3);

      for (let i = 0; i < 5; i++) {
        smallHistory.execute(new MockCommand(`Command ${i}`));
      }

      expect(smallHistory.getHistorySize()).toBe(3);
      expect(smallHistory.getUndoDescription()).toBe('Command 4');
    });

    it('should update max history size and trim if needed', () => {
      for (let i = 0; i < 10; i++) {
        commandHistory.execute(new MockCommand(`Command ${i}`));
      }

      expect(commandHistory.getHistorySize()).toBe(10);

      commandHistory.setMaxHistorySize(5);

      expect(commandHistory.getHistorySize()).toBe(5);
      expect(commandHistory.getUndoDescription()).toBe('Command 9');
    });
  });

  describe('Command Descriptions', () => {
    it('should return correct undo description', () => {
      const command = new MockCommand('Add Clip');
      commandHistory.execute(command);

      expect(commandHistory.getUndoDescription()).toBe('Add Clip');
    });

    it('should return correct redo description', () => {
      const command = new MockCommand('Delete Clip');
      commandHistory.execute(command);
      commandHistory.undo();

      expect(commandHistory.getRedoDescription()).toBe('Delete Clip');
    });

    it('should return null when no undo available', () => {
      expect(commandHistory.getUndoDescription()).toBeNull();
    });

    it('should return null when no redo available', () => {
      expect(commandHistory.getRedoDescription()).toBeNull();
    });
  });

  describe('History Queries', () => {
    it('should return undo history in correct order', () => {
      const cmd1 = new MockCommand('Command 1');
      const cmd2 = new MockCommand('Command 2');
      const cmd3 = new MockCommand('Command 3');

      commandHistory.execute(cmd1);
      commandHistory.execute(cmd2);
      commandHistory.execute(cmd3);

      const history = commandHistory.getUndoHistory();

      expect(history).toHaveLength(3);
      expect(history[0].description).toBe('Command 3');
      expect(history[1].description).toBe('Command 2');
      expect(history[2].description).toBe('Command 1');
    });

    it('should include timestamps in history', () => {
      const command = new MockCommand('Test Command');
      commandHistory.execute(command);

      const history = commandHistory.getUndoHistory();

      expect(history[0].timestamp).toBeInstanceOf(Date);
    });
  });

  describe('Clear History', () => {
    it('should clear all undo and redo stacks', () => {
      commandHistory.execute(new MockCommand('Command 1'));
      commandHistory.execute(new MockCommand('Command 2'));
      commandHistory.undo();

      expect(commandHistory.canUndo()).toBe(true);
      expect(commandHistory.canRedo()).toBe(true);

      commandHistory.clear();

      expect(commandHistory.canUndo()).toBe(false);
      expect(commandHistory.canRedo()).toBe(false);
      expect(commandHistory.getHistorySize()).toBe(0);
    });
  });

  describe('Listeners', () => {
    it('should notify listeners on command execution', () => {
      const listener = vi.fn();
      commandHistory.subscribe(listener);

      // Should be called immediately on subscribe
      expect(listener).toHaveBeenCalledWith(false, false);

      commandHistory.execute(new MockCommand('Test'));

      expect(listener).toHaveBeenCalledWith(true, false);
    });

    it('should notify listeners on undo', () => {
      const listener = vi.fn();
      commandHistory.execute(new MockCommand('Test'));

      listener.mockClear();
      commandHistory.subscribe(listener);

      commandHistory.undo();

      expect(listener).toHaveBeenCalledWith(false, true);
    });

    it('should allow unsubscribing', () => {
      const listener = vi.fn();
      const unsubscribe = commandHistory.subscribe(listener);

      listener.mockClear();
      unsubscribe();

      commandHistory.execute(new MockCommand('Test'));

      expect(listener).not.toHaveBeenCalled();
    });

    it('should handle errors in listeners gracefully', () => {
      const faultyListener = () => {
        throw new Error('Listener error');
      };
      const goodListener = vi.fn();

      commandHistory.subscribe(faultyListener);
      commandHistory.subscribe(goodListener);

      // Should not throw
      expect(() => commandHistory.execute(new MockCommand('Test'))).not.toThrow();
      expect(goodListener).toHaveBeenCalled();
    });
  });

  describe('BatchCommand', () => {
    it('should execute all commands in batch', () => {
      const batch = new BatchCommandImpl('Batch Operation');
      const executeFn1 = vi.fn();
      const executeFn2 = vi.fn();
      const executeFn3 = vi.fn();

      batch.addCommand(new MockCommand('Command 1', executeFn1));
      batch.addCommand(new MockCommand('Command 2', executeFn2));
      batch.addCommand(new MockCommand('Command 3', executeFn3));

      commandHistory.execute(batch);

      expect(executeFn1).toHaveBeenCalled();
      expect(executeFn2).toHaveBeenCalled();
      expect(executeFn3).toHaveBeenCalled();
    });

    it('should undo all commands in reverse order', () => {
      const batch = new BatchCommandImpl('Batch Operation');
      const undoFn1 = vi.fn();
      const undoFn2 = vi.fn();
      const undoFn3 = vi.fn();

      batch.addCommand(new MockCommand('Command 1', () => {}, undoFn1));
      batch.addCommand(new MockCommand('Command 2', () => {}, undoFn2));
      batch.addCommand(new MockCommand('Command 3', () => {}, undoFn3));

      commandHistory.execute(batch);
      commandHistory.undo();

      expect(undoFn1).toHaveBeenCalled();
      expect(undoFn2).toHaveBeenCalled();
      expect(undoFn3).toHaveBeenCalled();

      // Verify reverse order
      const order: number[] = [];
      const trackingBatch = new BatchCommandImpl('Tracking Batch');
      trackingBatch.addCommand(
        new MockCommand(
          '1',
          () => {},
          () => order.push(1)
        )
      );
      trackingBatch.addCommand(
        new MockCommand(
          '2',
          () => {},
          () => order.push(2)
        )
      );
      trackingBatch.addCommand(
        new MockCommand(
          '3',
          () => {},
          () => order.push(3)
        )
      );

      commandHistory.execute(trackingBatch);
      commandHistory.undo();

      expect(order).toEqual([3, 2, 1]);
    });

    it('should use batch description', () => {
      const batch = new BatchCommandImpl('Multi-clip Delete');
      batch.addCommand(new MockCommand('Delete Clip 1'));
      batch.addCommand(new MockCommand('Delete Clip 2'));

      commandHistory.execute(batch);

      expect(commandHistory.getUndoDescription()).toBe('Multi-clip Delete');
    });

    it('should return all commands in batch', () => {
      const batch = new BatchCommandImpl('Batch');
      const cmd1 = new MockCommand('Command 1');
      const cmd2 = new MockCommand('Command 2');

      batch.addCommand(cmd1);
      batch.addCommand(cmd2);

      const commands = batch.getCommands();

      expect(commands).toHaveLength(2);
      expect(commands[0]).toBe(cmd1);
      expect(commands[1]).toBe(cmd2);
    });
  });
});
