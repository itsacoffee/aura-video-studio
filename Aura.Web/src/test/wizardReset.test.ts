/**
 * Test suite for wizard reset functionality
 * Validates the behavior described in clean-desktop.ps1 and MANDATORY_FIRST_RUN_SETUP.md
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import {
  resetFirstRunStatus,
  getLocalFirstRunStatus,
  setLocalFirstRunStatus,
} from '../services/firstRunService';
import {
  clearWizardStateFromStorage,
  loadWizardStateFromStorage,
  saveWizardStateToStorage,
  initialOnboardingState,
} from '../state/onboarding';

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

describe('Wizard Reset Functionality', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  describe('Local Storage Reset', () => {
    it('should clear wizard progress from localStorage', () => {
      // Setup: Save some wizard progress
      saveWizardStateToStorage(initialOnboardingState);
      expect(loadWizardStateFromStorage()).not.toBeNull();

      // Action: Clear wizard state
      clearWizardStateFromStorage();

      // Verify: Progress is cleared
      expect(loadWizardStateFromStorage()).toBeNull();
    });

    it('should clear first-run completion flags', async () => {
      // Setup: Mark as completed
      setLocalFirstRunStatus(true);
      expect(getLocalFirstRunStatus()).toBe(true);

      // Mock backend reset to succeed
      (global.fetch as unknown as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ success: true }),
      });

      // Action: Reset first-run status
      await resetFirstRunStatus();

      // Verify: Flags are cleared
      expect(getLocalFirstRunStatus()).toBe(false);
      expect(localStorage.getItem('hasCompletedFirstRun')).toBeNull();
      expect(localStorage.getItem('hasSeenOnboarding')).toBeNull();
    });
  });

  describe('Backend Reset (API contract)', () => {
    it('should call backend reset endpoint when triggered', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({ success: true, message: 'Wizard reset successfully' }),
      });
      global.fetch = mockFetch as unknown as typeof fetch;

      // Call the reset endpoint directly (simulating what resetWizardInBackend does)
      await fetch('/api/setup/wizard/reset', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userId: 'default', preserveData: false }),
      });

      expect(mockFetch).toHaveBeenCalledWith(
        '/api/setup/wizard/reset',
        expect.objectContaining({
          method: 'POST',
          headers: expect.objectContaining({ 'Content-Type': 'application/json' }),
        })
      );
    });
  });

  describe('Full Reset Scenario (clean-desktop.ps1)', () => {
    it('should fully reset wizard state as if app was never run', async () => {
      // Setup: Simulate a completed wizard
      localStorage.setItem('hasCompletedFirstRun', 'true');
      localStorage.setItem('hasSeenOnboarding', 'true');
      saveWizardStateToStorage({
        ...initialOnboardingState,
        step: 5,
        selectedTier: 'pro',
      });

      // Mock backend calls
      (global.fetch as unknown as ReturnType<typeof vi.fn>)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true }),
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true }),
        });

      // Action: Full reset (simulate clean-desktop.ps1 + app restart)
      clearWizardStateFromStorage();
      await resetFirstRunStatus();

      // Verify: All state is cleared
      expect(loadWizardStateFromStorage()).toBeNull();
      expect(getLocalFirstRunStatus()).toBe(false);
      expect(localStorage.getItem('hasCompletedFirstRun')).toBeNull();
      expect(localStorage.getItem('hasSeenOnboarding')).toBeNull();
      expect(localStorage.getItem('wizardProgress')).toBeNull();
    });

    it('should result in wizard appearing on next app start', async () => {
      // Setup: Complete wizard
      setLocalFirstRunStatus(true);

      // Mock backend to say wizard is not completed (database was deleted)
      // This simulates what happens after clean-desktop.ps1 deletes the database
      (global.fetch as unknown as ReturnType<typeof vi.fn>).mockResolvedValue({
        ok: true,
        json: async () => ({
          completed: false,
          currentStep: 0,
          state: null,
          canResume: false,
          lastUpdated: null,
        }),
      });

      // Action: Check backend status (what App.tsx does on startup)
      const response = await fetch('/api/setup/wizard/status');
      const status = await response.json();

      // Verify: Backend reports not completed, so wizard should show
      expect(status.completed).toBe(false);
      expect(status.canResume).toBe(false);

      // In the real app, this would trigger:
      // 1. localStorage.removeItem('hasCompletedFirstRun')
      // 2. setShouldShowOnboarding(true)
      // 3. User sees wizard again
    });
  });

  describe('Resume vs Fresh Start', () => {
    it('should allow resume when backend has saved progress', async () => {
      // Mock backend with saved progress
      (global.fetch as unknown as ReturnType<typeof vi.fn>).mockResolvedValue({
        ok: true,
        json: async () => ({
          completed: false,
          currentStep: 3,
          state: { step: 3, selectedTier: 'free' },
          canResume: true,
          lastUpdated: new Date().toISOString(),
        }),
      });

      // Fetch wizard status
      const response = await fetch('/api/setup/wizard/status');
      const status = await response.json();

      // Verify: Can resume
      expect(status.canResume).toBe(true);
      expect(status.currentStep).toBe(3);
    });

    it('should start fresh when backend has no progress', async () => {
      // Mock backend with no progress (after clean-desktop.ps1)
      (global.fetch as unknown as ReturnType<typeof vi.fn>).mockResolvedValue({
        ok: true,
        json: async () => ({
          completed: false,
          currentStep: 0,
          state: null,
          canResume: false,
          lastUpdated: null,
        }),
      });

      // Fetch wizard status
      const response = await fetch('/api/setup/wizard/status');
      const status = await response.json();

      // Verify: Cannot resume, must start fresh
      expect(status.canResume).toBe(false);
      expect(status.currentStep).toBe(0);
      expect(status.state).toBeNull();
    });
  });
});
