import { describe, it, expect, beforeEach, vi } from 'vitest';
import { crashRecoveryService } from '../crashRecoveryService';

describe('CrashRecoveryService', () => {
  beforeEach(() => {
    // Clear storage before each test
    localStorage.clear();
    sessionStorage.clear();
    vi.clearAllMocks();
  });

  describe('initialize', () => {
    it('should detect clean startup when session was not active', () => {
      // Arrange - no session active
      sessionStorage.removeItem('aura_session_active');

      // Act
      const state = crashRecoveryService.initialize();

      // Assert
      expect(state.wasCleanShutdown).toBe(true);
      expect(state.consecutiveCrashes).toBe(0);
    });

    it('should detect crash when session was active', () => {
      // Arrange - simulate active session (would be set by previous session)
      sessionStorage.setItem('aura_session_active', 'true');

      // Act
      const state = crashRecoveryService.initialize();

      // Assert
      expect(state.wasCleanShutdown).toBe(false);
      expect(state.crashCount).toBeGreaterThan(0);
      expect(state.lastCrash).not.toBeNull();
    });

    it('should mark session as active after initialization', () => {
      // Act
      crashRecoveryService.initialize();

      // Assert
      expect(sessionStorage.getItem('aura_session_active')).toBe('true');
    });

    it('should increment consecutive crashes on repeated crashes', () => {
      // Arrange - simulate first crash
      sessionStorage.setItem('aura_session_active', 'true');
      const state1 = crashRecoveryService.initialize();

      // Simulate quick restart (crash within window)
      sessionStorage.setItem('aura_session_active', 'true');

      // Act - second crash
      const state2 = crashRecoveryService.initialize();

      // Assert
      expect(state2.consecutiveCrashes).toBeGreaterThan(state1.consecutiveCrashes);
    });
  });

  describe('markCleanShutdown', () => {
    it('should remove session active flag', () => {
      // Arrange
      crashRecoveryService.initialize();
      expect(sessionStorage.getItem('aura_session_active')).toBe('true');

      // Act
      crashRecoveryService.markCleanShutdown();

      // Assert
      expect(sessionStorage.getItem('aura_session_active')).toBeNull();
    });

    it('should update recovery state to clean shutdown', () => {
      // Arrange
      crashRecoveryService.initialize();

      // Act
      crashRecoveryService.markCleanShutdown();

      // Assert
      const state = crashRecoveryService.getRecoveryState();
      expect(state?.wasCleanShutdown).toBe(true);
    });
  });

  describe('shouldShowRecoveryScreen', () => {
    it('should return false for clean startups', () => {
      // Arrange
      sessionStorage.removeItem('aura_session_active');
      crashRecoveryService.initialize();

      // Act
      const shouldShow = crashRecoveryService.shouldShowRecoveryScreen();

      // Assert
      expect(shouldShow).toBe(false);
    });

    it('should return true after multiple consecutive crashes', () => {
      // Arrange - simulate multiple crashes
      for (let i = 0; i < 3; i++) {
        sessionStorage.setItem('aura_session_active', 'true');
        crashRecoveryService.initialize();
      }

      // Act
      const shouldShow = crashRecoveryService.shouldShowRecoveryScreen();

      // Assert
      expect(shouldShow).toBe(true);
    });
  });

  describe('resetCrashCounter', () => {
    it('should reset consecutive crash count', () => {
      // Arrange - simulate crashes
      sessionStorage.setItem('aura_session_active', 'true');
      crashRecoveryService.initialize();

      // Act
      crashRecoveryService.resetCrashCounter();

      // Assert
      const state = crashRecoveryService.getRecoveryState();
      expect(state?.consecutiveCrashes).toBe(0);
      expect(state?.wasCleanShutdown).toBe(true);
    });
  });

  describe('recordError', () => {
    it('should record error details in crash record', () => {
      // Arrange
      sessionStorage.setItem('aura_session_active', 'true');
      crashRecoveryService.initialize();
      const error = new Error('Test error');

      // Act
      crashRecoveryService.recordError(error);

      // Assert
      const state = crashRecoveryService.getRecoveryState();
      expect(state?.lastCrash?.error).toBe('Test error');
    });
  });

  describe('getRecoverySuggestions', () => {
    it('should return suggestions for multiple crashes', () => {
      // Arrange - simulate multiple crashes
      for (let i = 0; i < 3; i++) {
        sessionStorage.setItem('aura_session_active', 'true');
        crashRecoveryService.initialize();
      }

      // Act
      const suggestions = crashRecoveryService.getRecoverySuggestions();

      // Assert
      expect(suggestions.length).toBeGreaterThan(0);
      expect(suggestions.some((s) => s.includes('browser cache'))).toBe(true);
    });

    it('should return default suggestions for no crashes', () => {
      // Arrange
      crashRecoveryService.initialize();

      // Act
      const suggestions = crashRecoveryService.getRecoverySuggestions();

      // Assert
      expect(suggestions.length).toBeGreaterThan(0);
    });
  });

  describe('clearRecoveryData', () => {
    it('should clear all recovery data', () => {
      // Arrange
      sessionStorage.setItem('aura_session_active', 'true');
      crashRecoveryService.initialize();

      // Act
      crashRecoveryService.clearRecoveryData();

      // Assert
      expect(localStorage.getItem('aura_recovery_state')).toBeNull();
      expect(sessionStorage.getItem('aura_session_active')).toBeNull();
      expect(crashRecoveryService.getRecoveryState()).toBeNull();
    });
  });
});
