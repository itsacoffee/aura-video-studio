/**
 * Tests for Circuit Breaker Persistence
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { PersistentCircuitBreaker, CircuitBreakerState } from '../circuitBreakerPersistence';

describe('PersistentCircuitBreaker', () => {
  beforeEach(() => {
    // Clear localStorage before each test
    localStorage.clear();
    vi.clearAllMocks();
  });

  afterEach(() => {
    // Clean up after each test
    localStorage.clear();
  });

  describe('saveState', () => {
    it('should save circuit breaker state to localStorage', () => {
      const endpoint = '/api/test';
      const state: CircuitBreakerState = {
        state: 'OPEN',
        failureCount: 5,
        successCount: 0,
        nextAttempt: Date.now() + 60000,
        timestamp: Date.now(),
      };

      PersistentCircuitBreaker.saveState(endpoint, state);

      const saved = localStorage.getItem(PersistentCircuitBreaker.STORAGE_KEY);
      expect(saved).toBeTruthy();

      const parsed = JSON.parse(saved!);
      expect(parsed[endpoint]).toBeDefined();
      expect(parsed[endpoint].state).toBe('OPEN');
      expect(parsed[endpoint].failureCount).toBe(5);
    });

    it('should update existing state for endpoint', () => {
      const endpoint = '/api/test';
      const state1: CircuitBreakerState = {
        state: 'OPEN',
        failureCount: 5,
        successCount: 0,
        nextAttempt: Date.now() + 60000,
        timestamp: Date.now(),
      };

      PersistentCircuitBreaker.saveState(endpoint, state1);

      const state2: CircuitBreakerState = {
        state: 'HALF_OPEN',
        failureCount: 5,
        successCount: 1,
        nextAttempt: Date.now() + 30000,
        timestamp: Date.now(),
      };

      PersistentCircuitBreaker.saveState(endpoint, state2);

      const saved = localStorage.getItem(PersistentCircuitBreaker.STORAGE_KEY);
      const parsed = JSON.parse(saved!);
      expect(parsed[endpoint].state).toBe('HALF_OPEN');
      expect(parsed[endpoint].successCount).toBe(1);
    });

    it('should preserve states for different endpoints', () => {
      const endpoint1 = '/api/test1';
      const endpoint2 = '/api/test2';

      const state1: CircuitBreakerState = {
        state: 'OPEN',
        failureCount: 5,
        successCount: 0,
        nextAttempt: Date.now() + 60000,
        timestamp: Date.now(),
      };

      const state2: CircuitBreakerState = {
        state: 'CLOSED',
        failureCount: 0,
        successCount: 0,
        nextAttempt: Date.now(),
        timestamp: Date.now(),
      };

      PersistentCircuitBreaker.saveState(endpoint1, state1);
      PersistentCircuitBreaker.saveState(endpoint2, state2);

      const saved = localStorage.getItem(PersistentCircuitBreaker.STORAGE_KEY);
      const parsed = JSON.parse(saved!);

      expect(parsed[endpoint1].state).toBe('OPEN');
      expect(parsed[endpoint2].state).toBe('CLOSED');
    });

    it('should handle localStorage quota errors gracefully', () => {
      // Mock localStorage.setItem to throw an error
      const getItemSpy = vi.spyOn(Storage.prototype, 'getItem').mockReturnValue('{}');
      const setItemSpy = vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
        throw new Error('QuotaExceededError');
      });

      const endpoint = '/api/test';
      const state: CircuitBreakerState = {
        state: 'OPEN',
        failureCount: 5,
        successCount: 0,
        nextAttempt: Date.now() + 60000,
        timestamp: Date.now(),
      };

      // Should not throw - just handle the error gracefully
      expect(() => PersistentCircuitBreaker.saveState(endpoint, state)).not.toThrow();

      getItemSpy.mockRestore();
      setItemSpy.mockRestore();
    });
  });

  describe('loadState', () => {
    it('should load saved state from localStorage', () => {
      const endpoint = '/api/test';
      const state: CircuitBreakerState = {
        state: 'OPEN',
        failureCount: 5,
        successCount: 0,
        nextAttempt: Date.now() + 60000,
        timestamp: Date.now(),
      };

      PersistentCircuitBreaker.saveState(endpoint, state);

      const loaded = PersistentCircuitBreaker.loadState(endpoint);

      expect(loaded).toBeTruthy();
      expect(loaded?.state).toBe('OPEN');
      expect(loaded?.failureCount).toBe(5);
    });

    it('should return null for non-existent endpoint', () => {
      const loaded = PersistentCircuitBreaker.loadState('/api/nonexistent');
      expect(loaded).toBeNull();
    });

    it('should return null for stale state (older than 5 minutes)', () => {
      const endpoint = '/api/test';
      const staleTimestamp = Date.now() - 6 * 60 * 1000; // 6 minutes ago

      const state: CircuitBreakerState = {
        state: 'OPEN',
        failureCount: 5,
        successCount: 0,
        nextAttempt: Date.now() + 60000,
        timestamp: staleTimestamp,
      };

      // Manually save stale state
      const states = { [endpoint]: state };
      localStorage.setItem(PersistentCircuitBreaker.STORAGE_KEY, JSON.stringify(states));

      const loaded = PersistentCircuitBreaker.loadState(endpoint);
      expect(loaded).toBeNull();

      // Verify stale state was cleared
      const saved = localStorage.getItem(PersistentCircuitBreaker.STORAGE_KEY);
      const parsed = JSON.parse(saved!);
      expect(parsed[endpoint]).toBeUndefined();
    });

    it('should return valid state that is less than 5 minutes old', () => {
      const endpoint = '/api/test';
      const recentTimestamp = Date.now() - 2 * 60 * 1000; // 2 minutes ago

      const state: CircuitBreakerState = {
        state: 'OPEN',
        failureCount: 5,
        successCount: 0,
        nextAttempt: Date.now() + 60000,
        timestamp: recentTimestamp,
      };

      // Manually save recent state
      const states = { [endpoint]: state };
      localStorage.setItem(PersistentCircuitBreaker.STORAGE_KEY, JSON.stringify(states));

      const loaded = PersistentCircuitBreaker.loadState(endpoint);
      expect(loaded).toBeTruthy();
      expect(loaded?.state).toBe('OPEN');
    });

    it('should handle corrupted localStorage data gracefully', () => {
      const consoleSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});

      // Save invalid JSON
      localStorage.setItem(PersistentCircuitBreaker.STORAGE_KEY, 'invalid json');

      const loaded = PersistentCircuitBreaker.loadState('/api/test');
      expect(loaded).toBeNull();
      expect(consoleSpy).toHaveBeenCalled();

      consoleSpy.mockRestore();
    });
  });

  describe('clearState', () => {
    it('should clear state for specific endpoint', () => {
      const endpoint1 = '/api/test1';
      const endpoint2 = '/api/test2';

      const state: CircuitBreakerState = {
        state: 'OPEN',
        failureCount: 5,
        successCount: 0,
        nextAttempt: Date.now() + 60000,
        timestamp: Date.now(),
      };

      PersistentCircuitBreaker.saveState(endpoint1, state);
      PersistentCircuitBreaker.saveState(endpoint2, state);

      PersistentCircuitBreaker.clearState(endpoint1);

      const loaded1 = PersistentCircuitBreaker.loadState(endpoint1);
      const loaded2 = PersistentCircuitBreaker.loadState(endpoint2);

      expect(loaded1).toBeNull();
      expect(loaded2).toBeTruthy();
    });

    it('should clear all states when no endpoint provided', () => {
      const endpoint1 = '/api/test1';
      const endpoint2 = '/api/test2';

      const state: CircuitBreakerState = {
        state: 'OPEN',
        failureCount: 5,
        successCount: 0,
        nextAttempt: Date.now() + 60000,
        timestamp: Date.now(),
      };

      PersistentCircuitBreaker.saveState(endpoint1, state);
      PersistentCircuitBreaker.saveState(endpoint2, state);

      PersistentCircuitBreaker.clearState();

      const saved = localStorage.getItem(PersistentCircuitBreaker.STORAGE_KEY);
      expect(saved).toBeNull();
    });

    it('should handle errors when clearing state', () => {
      const removeItemSpy = vi.spyOn(Storage.prototype, 'removeItem').mockImplementation(() => {
        throw new Error('Storage error');
      });

      // Should not throw - just handle the error gracefully
      expect(() => PersistentCircuitBreaker.clearState()).not.toThrow();

      removeItemSpy.mockRestore();
    });
  });
});
