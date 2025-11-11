/**
 * Tests for Network Resilience Service
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import {
  networkResilienceService,
  type QueuedRequest,
  type NetworkResilienceConfig,
} from '../networkResilience';

describe('NetworkResilienceService', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
    networkResilienceService.clearQueue();
  });

  afterEach(() => {
    localStorage.clear();
  });

  describe('Configuration', () => {
    it('should configure resilience settings', () => {
      const config: Partial<NetworkResilienceConfig> = {
        enableOfflineQueue: true,
        maxQueueSize: 100,
        autoRetryOnReconnect: true,
      };

      networkResilienceService.configure(config);

      const requests = networkResilienceService.getQueuedRequests();
      expect(requests).toHaveLength(0);
    });
  });

  describe('Queue Management', () => {
    it('should queue a request', () => {
      const requestId = networkResilienceService.queueRequest(
        '/api/test',
        'POST',
        { data: 'test' },
        { priority: 'high' }
      );

      expect(requestId).toBeTruthy();

      const requests = networkResilienceService.getQueuedRequests();
      expect(requests).toHaveLength(1);
      expect(requests[0].url).toBe('/api/test');
      expect(requests[0].method).toBe('POST');
      expect(requests[0].priority).toBe('high');
    });

    it('should remove a request from queue', () => {
      const requestId = networkResilienceService.queueRequest('/api/test', 'POST');

      expect(networkResilienceService.getQueuedRequests()).toHaveLength(1);

      const removed = networkResilienceService.removeRequest(requestId);
      expect(removed).toBe(true);
      expect(networkResilienceService.getQueuedRequests()).toHaveLength(0);
    });

    it('should clear entire queue', () => {
      networkResilienceService.queueRequest('/api/test1', 'POST');
      networkResilienceService.queueRequest('/api/test2', 'GET');
      networkResilienceService.queueRequest('/api/test3', 'PUT');

      expect(networkResilienceService.getQueuedRequests()).toHaveLength(3);

      networkResilienceService.clearQueue();
      expect(networkResilienceService.getQueuedRequests()).toHaveLength(0);
    });

    it('should respect max queue size', () => {
      networkResilienceService.configure({ maxQueueSize: 2 });

      networkResilienceService.queueRequest('/api/test1', 'POST', undefined, { priority: 'high' });
      networkResilienceService.queueRequest('/api/test2', 'POST', undefined, {
        priority: 'normal',
      });
      networkResilienceService.queueRequest('/api/test3', 'POST', undefined, { priority: 'low' });

      const requests = networkResilienceService.getQueuedRequests();
      expect(requests).toHaveLength(2);

      // Should keep high and normal priority, remove low priority
      expect(requests.some((r) => r.url === '/api/test1')).toBe(true);
      expect(requests.some((r) => r.url === '/api/test2')).toBe(true);
      expect(requests.some((r) => r.url === '/api/test3')).toBe(false);
    });
  });

  describe('Priority Handling', () => {
    it('should queue requests with different priorities', () => {
      // Reset config to defaults to avoid state pollution from previous tests
      networkResilienceService.configure({ maxQueueSize: 50 });

      const id1 = networkResilienceService.queueRequest('/api/test1', 'POST', undefined, {
        priority: 'low',
      });
      const id2 = networkResilienceService.queueRequest('/api/test2', 'POST', undefined, {
        priority: 'high',
      });
      const id3 = networkResilienceService.queueRequest('/api/test3', 'POST', undefined, {
        priority: 'normal',
      });

      const requests = networkResilienceService.getQueuedRequests();
      expect(requests).toHaveLength(3);

      expect(requests.find((r) => r.id === id1)?.priority).toBe('low');
      expect(requests.find((r) => r.id === id2)?.priority).toBe('high');
      expect(requests.find((r) => r.id === id3)?.priority).toBe('normal');
    });
  });

  describe('Persistence', () => {
    it('should persist queue to localStorage', () => {
      networkResilienceService.configure({ queuePersistence: true });

      networkResilienceService.queueRequest('/api/test', 'POST', { data: 'test' });

      const stored = localStorage.getItem('aura_offline_request_queue');
      expect(stored).toBeTruthy();

      const parsed = JSON.parse(stored!) as QueuedRequest[];
      expect(parsed).toHaveLength(1);
      expect(parsed[0].url).toBe('/api/test');
    });

    it('should not persist queue when disabled', () => {
      networkResilienceService.configure({ queuePersistence: false });

      // Clear storage after disabling persistence
      localStorage.removeItem('aura_offline_request_queue');

      networkResilienceService.queueRequest('/api/test', 'POST');

      const stored = localStorage.getItem('aura_offline_request_queue');
      expect(stored).toBeFalsy();
    });
  });

  describe('Processing', () => {
    it('should process queued requests', async () => {
      const id1 = networkResilienceService.queueRequest('/api/test1', 'POST');
      const id2 = networkResilienceService.queueRequest('/api/test2', 'POST');

      const executedRequests: string[] = [];
      const executeRequest = vi.fn(async (request: QueuedRequest) => {
        executedRequests.push(request.id);
        return true;
      });

      await networkResilienceService.processQueue(executeRequest);

      expect(executeRequest).toHaveBeenCalledTimes(2);
      expect(executedRequests).toContain(id1);
      expect(executedRequests).toContain(id2);
      expect(networkResilienceService.getQueuedRequests()).toHaveLength(0);
    });

    it('should handle failed requests with retry', async () => {
      networkResilienceService.queueRequest('/api/test', 'POST', undefined, { maxRetries: 2 });

      let callCount = 0;
      const executeRequest = vi.fn(async () => {
        callCount++;
        return callCount > 1;
      });

      // First process - should fail and increment retry count
      await networkResilienceService.processQueue(executeRequest);
      expect(networkResilienceService.getQueuedRequests()).toHaveLength(1);
      expect(networkResilienceService.getQueuedRequests()[0].retryCount).toBe(1);

      // Second process - should succeed
      await networkResilienceService.processQueue(executeRequest);
      expect(networkResilienceService.getQueuedRequests()).toHaveLength(0);
    });

    it('should remove request after max retries', async () => {
      networkResilienceService.queueRequest('/api/test', 'POST', undefined, { maxRetries: 1 });

      const executeRequest = vi.fn(async () => false);

      await networkResilienceService.processQueue(executeRequest);

      expect(networkResilienceService.getQueuedRequests()).toHaveLength(0);
    });

    it('should process high priority requests first', async () => {
      networkResilienceService.queueRequest('/api/low', 'POST', undefined, { priority: 'low' });
      networkResilienceService.queueRequest('/api/high', 'POST', undefined, { priority: 'high' });
      networkResilienceService.queueRequest('/api/normal', 'POST', undefined, {
        priority: 'normal',
      });

      const executionOrder: string[] = [];
      const executeRequest = vi.fn(async (request: QueuedRequest) => {
        executionOrder.push(request.url);
        return true;
      });

      await networkResilienceService.processQueue(executeRequest);

      expect(executionOrder[0]).toBe('/api/high');
      expect(executionOrder[1]).toBe('/api/normal');
      expect(executionOrder[2]).toBe('/api/low');
    });
  });

  describe('Network Status', () => {
    it('should check if network is online', () => {
      const isOnline = networkResilienceService.isOnline();
      expect(typeof isOnline).toBe('boolean');
    });
  });

  describe('Error Handling', () => {
    it('should handle localStorage errors gracefully', () => {
      const setItemSpy = vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
        throw new Error('QuotaExceededError');
      });

      expect(() => {
        networkResilienceService.queueRequest('/api/test', 'POST');
      }).not.toThrow();

      setItemSpy.mockRestore();
    });

    it('should handle corrupted localStorage data', () => {
      localStorage.setItem('aura_offline_request_queue', 'invalid json');

      expect(() => {
        networkResilienceService.configure({ queuePersistence: true });
      }).not.toThrow();
    });

    it('should throw error when offline queue is disabled', () => {
      networkResilienceService.configure({ enableOfflineQueue: false });

      expect(() => {
        networkResilienceService.queueRequest('/api/test', 'POST');
      }).toThrow('Offline queue is disabled');
    });
  });
});
