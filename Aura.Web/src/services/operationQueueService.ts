/**
 * Operation Queue Service
 *
 * Manages queuing, prioritization, and execution of operations.
 * Provides logging and tracking for all operations.
 */

import { loggingService } from './loggingService';

export interface QueuedOperation {
  id: string;
  name: string;
  priority: number; // 1-10, higher is more important
  parameters: Record<string, unknown>;
  timestamp: Date;
  status: 'queued' | 'running' | 'completed' | 'failed' | 'cancelled';
  startTime?: Date;
  endTime?: Date;
  result?: unknown;
  error?: string;
  metadata?: Record<string, unknown>;
}

export interface OperationLogEntry {
  id: string;
  operationId: string;
  timestamp: Date;
  name: string;
  parameters: Record<string, unknown>;
  status: 'queued' | 'running' | 'completed' | 'failed' | 'cancelled';
  duration?: number; // milliseconds
  result?: unknown;
  error?: string;
  metadata?: Record<string, unknown>;
}

class OperationQueueService {
  private operations: Map<string, QueuedOperation> = new Map();
  private operationLog: OperationLogEntry[] = [];
  private maxLogSize = 1000; // Keep last 1000 operations in memory
  private logStorageKey = 'aura-operation-log';

  constructor() {
    this.loadLogFromStorage();
  }

  /**
   * Add an operation to the queue
   */
  addOperation(
    name: string,
    parameters: Record<string, unknown>,
    priority: number = 5,
    metadata?: Record<string, unknown>
  ): string {
    const id = `op-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    const operation: QueuedOperation = {
      id,
      name,
      priority: Math.max(1, Math.min(10, priority)),
      parameters,
      timestamp: new Date(),
      status: 'queued',
      metadata,
    };

    this.operations.set(id, operation);
    this.logOperation(operation);

    loggingService.info(`Operation queued: ${name}`, 'operationQueueService', 'addOperation', {
      id,
      priority,
      parameters,
    });

    return id;
  }

  /**
   * Update operation status
   */
  updateOperation(id: string, updates: Partial<QueuedOperation>): void {
    const operation = this.operations.get(id);
    if (!operation) {
      loggingService.warn(`Operation not found: ${id}`, 'operationQueueService', 'updateOperation');
      return;
    }

    const updated = { ...operation, ...updates };
    this.operations.set(id, updated);
    this.logOperation(updated);
  }

  /**
   * Start an operation
   */
  startOperation(id: string): void {
    const operation = this.operations.get(id);
    if (!operation) {
      return;
    }

    this.updateOperation(id, {
      status: 'running',
      startTime: new Date(),
    });

    loggingService.info(
      `Operation started: ${operation.name}`,
      'operationQueueService',
      'startOperation',
      { id }
    );
  }

  /**
   * Complete an operation
   */
  completeOperation(id: string, result?: unknown): void {
    const operation = this.operations.get(id);
    if (!operation) {
      return;
    }

    const endTime = new Date();
    this.updateOperation(id, {
      status: 'completed',
      endTime,
      result,
    });

    const duration = operation.startTime
      ? endTime.getTime() - operation.startTime.getTime()
      : undefined;

    loggingService.info(
      `Operation completed: ${operation.name}`,
      'operationQueueService',
      'completeOperation',
      { id, duration }
    );
  }

  /**
   * Fail an operation
   */
  failOperation(id: string, error: string): void {
    const operation = this.operations.get(id);
    if (!operation) {
      return;
    }

    const endTime = new Date();
    this.updateOperation(id, {
      status: 'failed',
      endTime,
      error,
    });

    const duration = operation.startTime
      ? endTime.getTime() - operation.startTime.getTime()
      : undefined;

    loggingService.error(
      `Operation failed: ${operation.name}`,
      new Error(error),
      'operationQueueService',
      'failOperation',
      { id, duration }
    );
  }

  /**
   * Cancel an operation
   */
  cancelOperation(id: string): void {
    const operation = this.operations.get(id);
    if (!operation) {
      return;
    }

    this.updateOperation(id, {
      status: 'cancelled',
      endTime: new Date(),
    });

    loggingService.info(
      `Operation cancelled: ${operation.name}`,
      'operationQueueService',
      'cancelOperation',
      { id }
    );
  }

  /**
   * Get operation by ID
   */
  getOperation(id: string): QueuedOperation | undefined {
    return this.operations.get(id);
  }

  /**
   * Get all operations
   */
  getAllOperations(): QueuedOperation[] {
    return Array.from(this.operations.values()).sort((a, b) => b.priority - a.priority);
  }

  /**
   * Get queued operations sorted by priority
   */
  getQueuedOperations(): QueuedOperation[] {
    return Array.from(this.operations.values())
      .filter((op) => op.status === 'queued')
      .sort((a, b) => b.priority - a.priority);
  }

  /**
   * Get running operations
   */
  getRunningOperations(): QueuedOperation[] {
    return Array.from(this.operations.values()).filter((op) => op.status === 'running');
  }

  /**
   * Get operation log
   */
  getOperationLog(limit?: number): OperationLogEntry[] {
    const log = [...this.operationLog].reverse();
    return limit ? log.slice(0, limit) : log;
  }

  /**
   * Clear completed operations from queue
   */
  clearCompleted(): void {
    const toRemove: string[] = [];
    this.operations.forEach((op, id) => {
      if (op.status === 'completed') {
        toRemove.push(id);
      }
    });
    toRemove.forEach((id) => this.operations.delete(id));
  }

  /**
   * Clear all operations
   */
  clearAll(): void {
    this.operations.clear();
  }

  /**
   * Clear operation log
   */
  clearLog(): void {
    this.operationLog = [];
    this.saveLogToStorage();
    loggingService.info('Operation log cleared', 'operationQueueService', 'clearLog');
  }

  /**
   * Export operation log as JSON
   */
  exportLog(): string {
    return JSON.stringify(this.operationLog, null, 2);
  }

  /**
   * Log an operation to persistent log
   */
  private logOperation(operation: QueuedOperation): void {
    const logEntry: OperationLogEntry = {
      id: `log-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
      operationId: operation.id,
      timestamp: new Date(),
      name: operation.name,
      parameters: operation.parameters,
      status: operation.status,
      duration:
        operation.startTime && operation.endTime
          ? operation.endTime.getTime() - operation.startTime.getTime()
          : undefined,
      result: operation.result,
      error: operation.error,
      metadata: operation.metadata,
    };

    this.operationLog.push(logEntry);

    // Trim log if it exceeds max size
    if (this.operationLog.length > this.maxLogSize) {
      this.operationLog = this.operationLog.slice(-this.maxLogSize);
    }

    // Save to localStorage periodically
    if (this.operationLog.length % 10 === 0) {
      this.saveLogToStorage();
    }
  }

  /**
   * Load operation log from localStorage
   */
  private loadLogFromStorage(): void {
    try {
      const stored = localStorage.getItem(this.logStorageKey);
      if (stored) {
        const parsed = JSON.parse(stored);
        this.operationLog = parsed.map((entry: { timestamp: string }) => ({
          ...entry,
          timestamp: new Date(entry.timestamp),
        }));
        loggingService.info(
          `Loaded ${this.operationLog.length} operation log entries`,
          'operationQueueService',
          'loadLogFromStorage'
        );
      }
    } catch (error) {
      loggingService.error(
        'Failed to load operation log from storage',
        error instanceof Error ? error : new Error(String(error)),
        'operationQueueService',
        'loadLogFromStorage'
      );
    }
  }

  /**
   * Save operation log to localStorage
   */
  private saveLogToStorage(): void {
    try {
      localStorage.setItem(this.logStorageKey, JSON.stringify(this.operationLog));
    } catch (error) {
      loggingService.error(
        'Failed to save operation log to storage',
        error instanceof Error ? error : new Error(String(error)),
        'operationQueueService',
        'saveLogToStorage'
      );
    }
  }
}

export const operationQueueService = new OperationQueueService();
