/**
 * Command Pattern Infrastructure for Undo/Redo System
 * Provides a comprehensive command history manager with undo/redo capabilities
 */

/**
 * Base interface for all commands
 */
export interface Command {
  /**
   * Execute the command
   */
  execute(): void;

  /**
   * Undo the command
   */
  undo(): void;

  /**
   * Get a human-readable description of the command
   */
  getDescription(): string;

  /**
   * Get timestamp when command was executed
   */
  getTimestamp(): Date;
}

/**
 * Interface for batch commands (multiple operations treated as one)
 */
export interface BatchCommand extends Command {
  /**
   * Add a command to the batch
   */
  addCommand(command: Command): void;

  /**
   * Get all commands in the batch
   */
  getCommands(): Command[];
}

/**
 * Listener for command history changes
 */
export type CommandHistoryListener = (canUndo: boolean, canRedo: boolean) => void;

/**
 * Command History Manager
 * Manages undo/redo stacks with configurable history size
 */
export class CommandHistory {
  private undoStack: Command[] = [];
  private redoStack: Command[] = [];
  private maxHistorySize: number;
  private listeners: Set<CommandHistoryListener> = new Set();

  /**
   * Create a new command history manager
   * @param maxHistorySize Maximum number of commands to keep in history (default: 50)
   */
  constructor(maxHistorySize: number = 50) {
    this.maxHistorySize = maxHistorySize;
  }

  /**
   * Execute a command and add it to the history
   * @param command Command to execute
   */
  execute(command: Command): void {
    command.execute();
    this.undoStack.push(command);

    // Clear redo stack when a new command is executed
    this.redoStack = [];

    // Enforce maximum history size
    if (this.undoStack.length > this.maxHistorySize) {
      this.undoStack.shift();
    }

    this.notifyListeners();
  }

  /**
   * Undo the last command
   * @returns true if undo was successful, false if nothing to undo
   */
  undo(): boolean {
    const command = this.undoStack.pop();
    if (!command) {
      return false;
    }

    command.undo();
    this.redoStack.push(command);

    this.notifyListeners();
    return true;
  }

  /**
   * Redo the last undone command
   * @returns true if redo was successful, false if nothing to redo
   */
  redo(): boolean {
    const command = this.redoStack.pop();
    if (!command) {
      return false;
    }

    command.execute();
    this.undoStack.push(command);

    this.notifyListeners();
    return true;
  }

  /**
   * Check if undo is available
   */
  canUndo(): boolean {
    return this.undoStack.length > 0;
  }

  /**
   * Check if redo is available
   */
  canRedo(): boolean {
    return this.redoStack.length > 0;
  }

  /**
   * Get the description of the command that would be undone
   */
  getUndoDescription(): string | null {
    const command = this.undoStack[this.undoStack.length - 1];
    return command ? command.getDescription() : null;
  }

  /**
   * Get the description of the command that would be redone
   */
  getRedoDescription(): string | null {
    const command = this.redoStack[this.redoStack.length - 1];
    return command ? command.getDescription() : null;
  }

  /**
   * Get the full undo history (most recent first)
   */
  getUndoHistory(): Array<{ description: string; timestamp: Date }> {
    return [...this.undoStack].reverse().map((cmd) => ({
      description: cmd.getDescription(),
      timestamp: cmd.getTimestamp(),
    }));
  }

  /**
   * Clear all history
   */
  clear(): void {
    this.undoStack = [];
    this.redoStack = [];
    this.notifyListeners();
  }

  /**
   * Subscribe to history changes
   * @param listener Function to call when history changes
   * @returns Unsubscribe function
   */
  subscribe(listener: CommandHistoryListener): () => void {
    this.listeners.add(listener);
    // Immediately notify the listener of current state
    try {
      listener(this.canUndo(), this.canRedo());
    } catch (error) {
      console.error('Error in command history listener:', error);
    }

    return () => {
      this.listeners.delete(listener);
    };
  }

  /**
   * Notify all listeners of history changes
   */
  private notifyListeners(): void {
    const canUndo = this.canUndo();
    const canRedo = this.canRedo();
    this.listeners.forEach((listener) => {
      try {
        listener(canUndo, canRedo);
      } catch (error) {
        console.error('Error in command history listener:', error);
      }
    });
  }

  /**
   * Get the current history size
   */
  getHistorySize(): number {
    return this.undoStack.length;
  }

  /**
   * Set the maximum history size
   * @param size New maximum size
   */
  setMaxHistorySize(size: number): void {
    this.maxHistorySize = size;
    // Trim history if needed
    while (this.undoStack.length > this.maxHistorySize) {
      this.undoStack.shift();
    }
    this.notifyListeners();
  }
}

/**
 * Implementation of batch command for grouping multiple operations
 */
export class BatchCommandImpl implements BatchCommand {
  private commands: Command[] = [];
  private description: string;
  private timestamp: Date;

  constructor(description: string) {
    this.description = description;
    this.timestamp = new Date();
  }

  addCommand(command: Command): void {
    this.commands.push(command);
  }

  getCommands(): Command[] {
    return [...this.commands];
  }

  execute(): void {
    this.commands.forEach((cmd) => cmd.execute());
  }

  undo(): void {
    // Undo in reverse order
    [...this.commands].reverse().forEach((cmd) => cmd.undo());
  }

  getDescription(): string {
    return this.description;
  }

  getTimestamp(): Date {
    return this.timestamp;
  }
}
