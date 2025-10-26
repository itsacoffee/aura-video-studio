import { describe, it, expect, beforeEach, vi } from 'vitest';
import { operationQueueService } from '../operationQueueService';

describe('OperationQueueService', () => {
  beforeEach(() => {
    // Clear operations before each test
    operationQueueService.clearAll();
    operationQueueService.clearLog();
  });

  it('should add an operation to the queue', () => {
    const id = operationQueueService.addOperation(
      'Test Operation',
      { param1: 'value1' },
      5
    );

    expect(id).toBeDefined();
    expect(id).toMatch(/^op-/);

    const operation = operationQueueService.getOperation(id);
    expect(operation).toBeDefined();
    expect(operation?.name).toBe('Test Operation');
    expect(operation?.status).toBe('queued');
    expect(operation?.priority).toBe(5);
  });

  it('should start an operation', () => {
    const id = operationQueueService.addOperation('Test Operation', {});
    operationQueueService.startOperation(id);

    const operation = operationQueueService.getOperation(id);
    expect(operation?.status).toBe('running');
    expect(operation?.startTime).toBeDefined();
  });

  it('should complete an operation', () => {
    const id = operationQueueService.addOperation('Test Operation', {});
    operationQueueService.startOperation(id);
    operationQueueService.completeOperation(id, { result: 'success' });

    const operation = operationQueueService.getOperation(id);
    expect(operation?.status).toBe('completed');
    expect(operation?.endTime).toBeDefined();
    expect(operation?.result).toEqual({ result: 'success' });
  });

  it('should fail an operation', () => {
    const id = operationQueueService.addOperation('Test Operation', {});
    operationQueueService.startOperation(id);
    operationQueueService.failOperation(id, 'Test error');

    const operation = operationQueueService.getOperation(id);
    expect(operation?.status).toBe('failed');
    expect(operation?.endTime).toBeDefined();
    expect(operation?.error).toBe('Test error');
  });

  it('should cancel an operation', () => {
    const id = operationQueueService.addOperation('Test Operation', {});
    operationQueueService.cancelOperation(id);

    const operation = operationQueueService.getOperation(id);
    expect(operation?.status).toBe('cancelled');
    expect(operation?.endTime).toBeDefined();
  });

  it('should get queued operations sorted by priority', () => {
    const id1 = operationQueueService.addOperation('Low Priority', {}, 2);
    const id2 = operationQueueService.addOperation('High Priority', {}, 9);
    const id3 = operationQueueService.addOperation('Medium Priority', {}, 5);

    const queued = operationQueueService.getQueuedOperations();
    expect(queued).toHaveLength(3);
    expect(queued[0].id).toBe(id2); // High priority first
    expect(queued[1].id).toBe(id3);
    expect(queued[2].id).toBe(id1);
  });

  it('should get running operations', () => {
    const id1 = operationQueueService.addOperation('Op 1', {});
    const id2 = operationQueueService.addOperation('Op 2', {});
    const id3 = operationQueueService.addOperation('Op 3', {});

    operationQueueService.startOperation(id1);
    operationQueueService.startOperation(id2);

    const running = operationQueueService.getRunningOperations();
    expect(running).toHaveLength(2);
    expect(running.find(op => op.id === id1)).toBeDefined();
    expect(running.find(op => op.id === id2)).toBeDefined();
  });

  it('should clear completed operations', () => {
    const id1 = operationQueueService.addOperation('Op 1', {});
    const id2 = operationQueueService.addOperation('Op 2', {});

    operationQueueService.startOperation(id1);
    operationQueueService.completeOperation(id1);
    operationQueueService.startOperation(id2);

    const allOps = operationQueueService.getAllOperations();
    expect(allOps).toHaveLength(2);

    operationQueueService.clearCompleted();

    const remainingOps = operationQueueService.getAllOperations();
    expect(remainingOps).toHaveLength(1);
    expect(remainingOps[0].status).toBe('running');
  });

  it('should maintain operation log', () => {
    const id = operationQueueService.addOperation('Test Operation', { param: 'value' });
    operationQueueService.startOperation(id);
    operationQueueService.completeOperation(id, { result: 'success' });

    const log = operationQueueService.getOperationLog();
    expect(log.length).toBeGreaterThan(0);
    
    // Should have log entries for: queued, running, completed
    const opLogs = log.filter(entry => entry.operationId === id);
    expect(opLogs.length).toBeGreaterThan(0);
  });

  it('should export operation log as JSON', () => {
    operationQueueService.addOperation('Op 1', {});
    operationQueueService.addOperation('Op 2', {});

    const exported = operationQueueService.exportLog();
    expect(exported).toBeDefined();
    
    const parsed = JSON.parse(exported);
    expect(Array.isArray(parsed)).toBe(true);
  });

  it('should clamp priority between 1 and 10', () => {
    const id1 = operationQueueService.addOperation('Op 1', {}, 15);
    const id2 = operationQueueService.addOperation('Op 2', {}, -5);
    const id3 = operationQueueService.addOperation('Op 3', {}, 0);

    const op1 = operationQueueService.getOperation(id1);
    const op2 = operationQueueService.getOperation(id2);
    const op3 = operationQueueService.getOperation(id3);

    expect(op1?.priority).toBe(10);
    expect(op2?.priority).toBe(1);
    expect(op3?.priority).toBe(1);
  });

  it('should handle updating non-existent operation gracefully', () => {
    // Should not throw error
    expect(() => {
      operationQueueService.updateOperation('non-existent-id', { status: 'completed' });
    }).not.toThrow();
  });

  it('should include metadata in operations', () => {
    const metadata = { userId: '123', source: 'test' };
    const id = operationQueueService.addOperation('Test', {}, 5, metadata);

    const operation = operationQueueService.getOperation(id);
    expect(operation?.metadata).toEqual(metadata);
  });
});
