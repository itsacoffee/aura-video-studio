/**
 * Crash Recovery Service
 * Detects crashes and provides recovery mechanisms
 */

import { loggingService } from './loggingService';

interface CrashRecord {
  timestamp: number;
  url: string;
  error?: string;
  stackTrace?: string;
}

interface RecoveryState {
  wasCleanShutdown: boolean;
  lastCrash: CrashRecord | null;
  crashCount: number;
  consecutiveCrashes: number;
}

const STORAGE_KEY = 'aura_recovery_state';
const SESSION_KEY = 'aura_session_active';
const CRASH_THRESHOLD = 3; // Max consecutive crashes before showing recovery screen
const CRASH_WINDOW_MS = 60000; // 1 minute window for crash detection

class CrashRecoveryService {
  private recoveryState: RecoveryState | null = null;

  /**
   * Initialize crash recovery on application startup
   */
  initialize(): RecoveryState {
    try {
      // Check if session was active when page closed
      const sessionActive = sessionStorage.getItem(SESSION_KEY) === 'true';

      // Load previous recovery state
      const savedState = localStorage.getItem(STORAGE_KEY);
      const previousState: RecoveryState = savedState
        ? JSON.parse(savedState)
        : {
            wasCleanShutdown: true,
            lastCrash: null,
            crashCount: 0,
            consecutiveCrashes: 0,
          };

      // Detect crash (session was active but browser/tab closed unexpectedly)
      const didCrash = sessionActive;

      if (didCrash) {
        loggingService.warn(
          'Detected unclean shutdown - possible crash',
          'CrashRecoveryService',
          'initialize'
        );

        // Check if crash was recent (within crash window)
        const now = Date.now();
        const lastCrashTime = previousState.lastCrash?.timestamp || 0;
        const isConsecutiveCrash = now - lastCrashTime < CRASH_WINDOW_MS;

        const crashRecord: CrashRecord = {
          timestamp: now,
          url: window.location.href,
        };

        this.recoveryState = {
          wasCleanShutdown: false,
          lastCrash: crashRecord,
          crashCount: previousState.crashCount + 1,
          consecutiveCrashes: isConsecutiveCrash ? previousState.consecutiveCrashes + 1 : 1,
        };

        // Log crash
        loggingService.error(
          `Application crash detected (consecutive: ${this.recoveryState.consecutiveCrashes})`,
          new Error('Crash detected'),
          'CrashRecoveryService',
          'initialize',
          {
            crashCount: this.recoveryState.crashCount,
            consecutiveCrashes: this.recoveryState.consecutiveCrashes,
            lastUrl: crashRecord.url,
          }
        );
      } else {
        // Clean startup
        this.recoveryState = {
          wasCleanShutdown: true,
          lastCrash: previousState.lastCrash,
          crashCount: previousState.crashCount,
          consecutiveCrashes: 0, // Reset consecutive counter on clean startup
        };
      }

      // Mark session as active
      sessionStorage.setItem(SESSION_KEY, 'true');

      // Save recovery state
      this.saveRecoveryState();

      return this.recoveryState;
    } catch (error) {
      loggingService.error(
        'Failed to initialize crash recovery',
        error as Error,
        'CrashRecoveryService',
        'initialize'
      );

      // Return safe default state
      return {
        wasCleanShutdown: true,
        lastCrash: null,
        crashCount: 0,
        consecutiveCrashes: 0,
      };
    }
  }

  /**
   * Mark shutdown as clean (call this on beforeunload)
   */
  markCleanShutdown(): void {
    try {
      sessionStorage.removeItem(SESSION_KEY);

      if (this.recoveryState) {
        this.recoveryState.wasCleanShutdown = true;
        this.saveRecoveryState();
      }
    } catch (error) {
      loggingService.error(
        'Failed to mark clean shutdown',
        error as Error,
        'CrashRecoveryService',
        'markCleanShutdown'
      );
    }
  }

  /**
   * Record an error that might lead to a crash
   */
  recordError(error: Error, context?: Record<string, unknown>): void {
    try {
      loggingService.error(
        'Potential crash error recorded',
        error,
        'CrashRecoveryService',
        'recordError',
        context
      );

      if (this.recoveryState && this.recoveryState.lastCrash) {
        this.recoveryState.lastCrash.error = error.message;
        this.recoveryState.lastCrash.stackTrace = error.stack;
        this.saveRecoveryState();
      }
    } catch (err) {
      console.error('Failed to record error:', err);
    }
  }

  /**
   * Get current recovery state
   */
  getRecoveryState(): RecoveryState | null {
    return this.recoveryState;
  }

  /**
   * Check if application should show recovery screen
   */
  shouldShowRecoveryScreen(): boolean {
    if (!this.recoveryState) {
      return false;
    }

    return (
      !this.recoveryState.wasCleanShutdown &&
      this.recoveryState.consecutiveCrashes >= CRASH_THRESHOLD
    );
  }

  /**
   * Reset crash counter (after successful recovery)
   */
  resetCrashCounter(): void {
    if (this.recoveryState) {
      this.recoveryState.consecutiveCrashes = 0;
      this.recoveryState.wasCleanShutdown = true;
      this.saveRecoveryState();

      loggingService.info(
        'Crash counter reset - successful recovery',
        'CrashRecoveryService',
        'resetCrashCounter'
      );
    }
  }

  /**
   * Clear all recovery data (for troubleshooting)
   */
  clearRecoveryData(): void {
    try {
      localStorage.removeItem(STORAGE_KEY);
      sessionStorage.removeItem(SESSION_KEY);
      this.recoveryState = null;

      loggingService.info('Recovery data cleared', 'CrashRecoveryService', 'clearRecoveryData');
    } catch (error) {
      loggingService.error(
        'Failed to clear recovery data',
        error as Error,
        'CrashRecoveryService',
        'clearRecoveryData'
      );
    }
  }

  /**
   * Get recovery suggestions based on crash history
   */
  getRecoverySuggestions(): string[] {
    if (!this.recoveryState) {
      return [];
    }

    const suggestions: string[] = [];

    if (this.recoveryState.consecutiveCrashes >= CRASH_THRESHOLD) {
      suggestions.push('Try clearing your browser cache and reloading');
      suggestions.push('Disable browser extensions that might interfere');
      suggestions.push('Check if your browser is up to date');
    }

    if (this.recoveryState.crashCount > 5) {
      suggestions.push('Your browser storage might be full - try clearing some data');
      suggestions.push('Check browser console for error messages');
    }

    if (this.recoveryState.lastCrash?.url) {
      suggestions.push(`Avoid navigating to: ${this.recoveryState.lastCrash.url}`);
    }

    if (suggestions.length === 0) {
      suggestions.push('Try reloading the page');
      suggestions.push('Clear browser cache and cookies');
      suggestions.push('Use a different browser');
    }

    return suggestions;
  }

  private saveRecoveryState(): void {
    try {
      if (this.recoveryState) {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(this.recoveryState));
      }
    } catch (error) {
      console.error('Failed to save recovery state:', error);
    }
  }
}

export const crashRecoveryService = new CrashRecoveryService();
