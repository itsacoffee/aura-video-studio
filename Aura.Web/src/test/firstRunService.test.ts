import { describe, it, expect, beforeEach, vi } from 'vitest';
import {
  getLocalFirstRunStatus,
  setLocalFirstRunStatus,
  migrateLegacyFirstRunStatus,
  resetFirstRunStatus,
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
      (global.fetch as unknown as ReturnType<typeof vi.fn>).mockRejectedValueOnce(new Error('Network error'));

      await expect(resetFirstRunStatus()).resolves.not.toThrow();

      // Local storage should still be cleared
      expect(localStorage.getItem('hasCompletedFirstRun')).toBeNull();
    });
  });
});
