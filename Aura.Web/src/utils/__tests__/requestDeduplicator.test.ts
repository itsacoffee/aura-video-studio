import { describe, it, expect, beforeEach } from 'vitest';
import { RequestDeduplicator } from '../requestDeduplicator';

// Test timing constants
const TEST_DELAY_MS = 50;
const SHORT_DELAY_MS = 10;

describe('RequestDeduplicator', () => {
  let deduplicator: RequestDeduplicator;

  beforeEach(() => {
    deduplicator = new RequestDeduplicator();
  });

  describe('deduplicate', () => {
    it('should execute request and return result', async () => {
      const result = await deduplicator.deduplicate('test-key', async () => {
        return 'test-result';
      });

      expect(result).toBe('test-result');
    });

    it('should reuse in-flight promise for same key', async () => {
      let callCount = 0;
      const request = async () => {
        callCount++;
        await new Promise((resolve) => setTimeout(resolve, TEST_DELAY_MS));
        return `result-${callCount}`;
      };

      // Start two requests with the same key
      const promise1 = deduplicator.deduplicate('same-key', request);
      const promise2 = deduplicator.deduplicate('same-key', request);

      const [result1, result2] = await Promise.all([promise1, promise2]);

      // Should only execute once
      expect(callCount).toBe(1);
      expect(result1).toBe('result-1');
      expect(result2).toBe('result-1');
    });

    it('should execute separate requests for different keys', async () => {
      const request1 = async () => {
        await new Promise((resolve) => setTimeout(resolve, SHORT_DELAY_MS));
        return 'result-1';
      };

      const request2 = async () => {
        await new Promise((resolve) => setTimeout(resolve, SHORT_DELAY_MS));
        return 'result-2';
      };

      const promise1 = deduplicator.deduplicate('key-1', request1);
      const promise2 = deduplicator.deduplicate('key-2', request2);

      const [result1, result2] = await Promise.all([promise1, promise2]);

      // Should execute both requests separately
      expect(result1).toBe('result-1');
      expect(result2).toBe('result-2');
    });

    it('should allow new request after previous completes', async () => {
      let callCount = 0;
      const request = async () => {
        callCount++;
        return `result-${callCount}`;
      };

      const result1 = await deduplicator.deduplicate('key', request);
      const result2 = await deduplicator.deduplicate('key', request);

      // Should execute twice since they're sequential
      expect(callCount).toBe(2);
      expect(result1).toBe('result-1');
      expect(result2).toBe('result-2');
    });

    it('should handle promise rejection', async () => {
      const error = new Error('Test error');
      const request = async () => {
        throw error;
      };

      await expect(deduplicator.deduplicate('key', request)).rejects.toThrow('Test error');

      // Should allow retry after failure
      const successRequest = async () => 'success';
      const result = await deduplicator.deduplicate('key', successRequest);
      expect(result).toBe('success');
    });
  });

  describe('isPending', () => {
    it('should return false for non-existent key', () => {
      expect(deduplicator.isPending('non-existent')).toBe(false);
    });

    it('should return true for in-flight request', async () => {
      const promise = deduplicator.deduplicate('key', async () => {
        await new Promise((resolve) => setTimeout(resolve, TEST_DELAY_MS));
        return 'result';
      });

      expect(deduplicator.isPending('key')).toBe(true);

      await promise;

      expect(deduplicator.isPending('key')).toBe(false);
    });
  });

  describe('clear', () => {
    it('should clear specific key', async () => {
      const promise = deduplicator.deduplicate('key', async () => {
        await new Promise((resolve) => setTimeout(resolve, TEST_DELAY_MS));
        return 'result';
      });

      expect(deduplicator.isPending('key')).toBe(true);

      deduplicator.clear('key');

      expect(deduplicator.isPending('key')).toBe(false);

      // Original promise should still resolve
      await expect(promise).resolves.toBe('result');
    });

    it('should clear all keys', async () => {
      deduplicator.deduplicate('key1', async () => {
        await new Promise((resolve) => setTimeout(resolve, TEST_DELAY_MS));
        return 'result1';
      });

      deduplicator.deduplicate('key2', async () => {
        await new Promise((resolve) => setTimeout(resolve, TEST_DELAY_MS));
        return 'result2';
      });

      expect(deduplicator.pendingCount).toBe(2);

      deduplicator.clear();

      expect(deduplicator.pendingCount).toBe(0);
    });
  });

  describe('pendingCount', () => {
    it('should track pending request count', async () => {
      expect(deduplicator.pendingCount).toBe(0);

      const promise1 = deduplicator.deduplicate('key1', async () => {
        await new Promise((resolve) => setTimeout(resolve, TEST_DELAY_MS));
        return 'result1';
      });

      expect(deduplicator.pendingCount).toBe(1);

      const promise2 = deduplicator.deduplicate('key2', async () => {
        await new Promise((resolve) => setTimeout(resolve, TEST_DELAY_MS));
        return 'result2';
      });

      expect(deduplicator.pendingCount).toBe(2);

      await Promise.all([promise1, promise2]);

      expect(deduplicator.pendingCount).toBe(0);
    });
  });
});
