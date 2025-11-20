import { describe, it, expect, beforeEach, vi } from 'vitest';
import {
  getLocalFirstRunStatus,
  setLocalFirstRunStatus,
  migrateLegacyFirstRunStatus,
  resetFirstRunStatus,
  hasCompletedFirstRun,
  clearFirstRunCache,
} from '../services/firstRunService';

// Mock localStorage
const localStorageMock = (() => {
  let store: Record<string, string> = {};
  return {
    getItem: (key: string) => store[key] || null,
    setItem: (key: string, value: string) => {
      store[key] = value;
    },
    removeItem: (key: string) => {
      delete store[key];
    },
    clear: () => {
      store = {};
    },
  };
})();

Object.defineProperty(window, 'localStorage', {
  value: localStorageMock,
});

// Mock fetch globally
global.fetch = vi.fn();

describe('firstRunService', () => {
  beforeEach(() => {
    // Clear localStorage before each test
    localStorage.clear();
    // Clear cache before each test
    clearFirstRunCache();
    vi.clearAllMocks();
  });

  describe('getLocalFirstRunStatus', () => {
    it('should return false when no keys are set', () => {
      expect(getLocalFirstRunStatus()).toBe(false);
    });

    it('should return true when hasCompletedFirstRun is true', () => {
      localStorage.setItem('hasCompletedFirstRun', 'true');
      expect(getLocalFirstRunStatus()).toBe(true);
    });

    it('should return true when legacy hasSeenOnboarding is true', () => {
      localStorage.setItem('hasSeenOnboarding', 'true');
      expect(getLocalFirstRunStatus()).toBe(true);
    });

    it('should prefer new key over legacy key', () => {
      localStorage.setItem('hasCompletedFirstRun', 'true');
      localStorage.setItem('hasSeenOnboarding', 'false');
      expect(getLocalFirstRunStatus()).toBe(true);
    });
  });

  describe('setLocalFirstRunStatus', () => {
    it('should set both new and legacy keys to true', () => {
      setLocalFirstRunStatus(true);
      expect(localStorage.getItem('hasCompletedFirstRun')).toBe('true');
      expect(localStorage.getItem('hasSeenOnboarding')).toBe('true');
    });

    it('should set both new and legacy keys to false', () => {
      setLocalFirstRunStatus(false);
      expect(localStorage.getItem('hasCompletedFirstRun')).toBe('false');
      expect(localStorage.getItem('hasSeenOnboarding')).toBe('false');
    });
  });

  describe('migrateLegacyFirstRunStatus', () => {
    it('should migrate legacy key to new key', () => {
      localStorage.setItem('hasSeenOnboarding', 'true');
      migrateLegacyFirstRunStatus();
      expect(localStorage.getItem('hasCompletedFirstRun')).toBe('true');
    });

    it('should not overwrite existing new key', () => {
      localStorage.setItem('hasCompletedFirstRun', 'true');
      localStorage.setItem('hasSeenOnboarding', 'false');
      migrateLegacyFirstRunStatus();
      expect(localStorage.getItem('hasCompletedFirstRun')).toBe('true');
    });

    it('should do nothing if no legacy key exists', () => {
      migrateLegacyFirstRunStatus();
      expect(localStorage.getItem('hasCompletedFirstRun')).toBeNull();
    });
  });

  describe('resetFirstRunStatus', () => {
    it('should clear both keys from localStorage', async () => {
      localStorage.setItem('hasCompletedFirstRun', 'true');
      localStorage.setItem('hasSeenOnboarding', 'true');

      // Mock backend call to succeed
      (global.fetch as unknown as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ success: true }),
      });

      await resetFirstRunStatus();

      expect(localStorage.getItem('hasCompletedFirstRun')).toBeNull();
      expect(localStorage.getItem('hasSeenOnboarding')).toBeNull();
    });

    it('should not throw if backend call fails', async () => {
      localStorage.setItem('hasCompletedFirstRun', 'true');

      // Mock backend call to fail
      (global.fetch as unknown as ReturnType<typeof vi.fn>).mockRejectedValueOnce(
        new Error('Network error')
      );

      await expect(resetFirstRunStatus()).resolves.not.toThrow();

      // Local storage should still be cleared
      expect(localStorage.getItem('hasCompletedFirstRun')).toBeNull();
    });
  });

  describe('hasCompletedFirstRun caching', () => {
    it('should cache the result for 5 seconds', async () => {
      // Mock backend to return false
      (global.fetch as unknown as ReturnType<typeof vi.fn>).mockResolvedValue({
        ok: true,
        json: async () => ({ completed: false }),
      });

      // First call should fetch from backend
      const result1 = await hasCompletedFirstRun();
      expect(result1).toBe(false);
      expect(global.fetch).toHaveBeenCalledTimes(1);

      // Second immediate call should use cache
      const result2 = await hasCompletedFirstRun();
      expect(result2).toBe(false);
      expect(global.fetch).toHaveBeenCalledTimes(1); // No additional calls

      // Third call should also use cache
      const result3 = await hasCompletedFirstRun();
      expect(result3).toBe(false);
      expect(global.fetch).toHaveBeenCalledTimes(1); // Still no additional calls
    });

    it('should return cached true value from localStorage without backend call', async () => {
      // Set localStorage to true
      localStorage.setItem('hasCompletedFirstRun', 'true');

      const result = await hasCompletedFirstRun();
      expect(result).toBe(true);
      // Should not make backend call if localStorage is true
      expect(global.fetch).not.toHaveBeenCalled();
    });

    it('should clear cache when clearFirstRunCache is called', async () => {
      // Mock backend to return false
      (global.fetch as unknown as ReturnType<typeof vi.fn>).mockResolvedValue({
        ok: true,
        json: async () => ({ completed: false }),
      });

      // First call
      await hasCompletedFirstRun();
      expect(global.fetch).toHaveBeenCalledTimes(1);

      // Clear cache
      clearFirstRunCache();

      // Second call should fetch again
      await hasCompletedFirstRun();
      expect(global.fetch).toHaveBeenCalledTimes(2);
    });

    it('should retry on backend failure with exponential backoff', async () => {
      // Mock backend to fail 2 times, then succeed
      (global.fetch as unknown as ReturnType<typeof vi.fn>)
        .mockRejectedValueOnce(new Error('Network error'))
        .mockRejectedValueOnce(new Error('Network error'))
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ completed: true }),
        });

      const result = await hasCompletedFirstRun();
      expect(result).toBe(true);
      expect(global.fetch).toHaveBeenCalledTimes(3); // Initial + 2 retries
    });

    it('should cache negative result on all retries failing', async () => {
      // Mock backend to always fail
      (global.fetch as unknown as ReturnType<typeof vi.fn>).mockRejectedValue(
        new Error('Network error')
      );

      const result = await hasCompletedFirstRun();
      expect(result).toBe(false);
      expect(global.fetch).toHaveBeenCalledTimes(3); // Initial + 2 retries

      // Next call should use cached false result
      const result2 = await hasCompletedFirstRun();
      expect(result2).toBe(false);
      // Should use cache, so no additional fetch calls
      expect(global.fetch).toHaveBeenCalledTimes(3); // Still 3 from first call
    }, 10000); // Increase timeout to 10 seconds to account for retry delays
  });
});
