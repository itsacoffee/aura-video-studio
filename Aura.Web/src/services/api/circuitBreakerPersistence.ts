/**
 * Circuit Breaker Persistence - Stores circuit breaker state in localStorage
 * to maintain state across page reloads
 */

import { loggingService as logger } from '../loggingService';

/**
 * Circuit breaker state that can be persisted
 */
export interface CircuitBreakerState {
  state: 'CLOSED' | 'OPEN' | 'HALF_OPEN';
  failureCount: number;
  successCount: number;
  nextAttempt: number;
  timestamp: number;
}

/**
 * Record of all circuit breaker states by endpoint
 */
interface CircuitBreakerStateRecord {
  [endpoint: string]: CircuitBreakerState;
}

/**
 * Utility class for persisting circuit breaker state to localStorage
 */
export class PersistentCircuitBreaker {
  static readonly STORAGE_KEY = 'aura_circuit_breaker_state';
  private static readonly STALE_THRESHOLD_MS = 5 * 60 * 1000; // 5 minutes

  /**
   * Save circuit breaker state to localStorage
   */
  static saveState(endpoint: string, state: CircuitBreakerState): void {
    try {
      const allStates = this.loadAllStates();
      allStates[endpoint] = {
        ...state,
        timestamp: Date.now(),
      };
      localStorage.setItem(this.STORAGE_KEY, JSON.stringify(allStates));
    } catch (error) {
      // Handle localStorage quota errors or other issues silently
      logger.warn(
        'Failed to save circuit breaker state',
        'circuitBreakerPersistence',
        'saveState',
        { endpoint, error: String(error) }
      );
    }
  }

  /**
   * Load circuit breaker state from localStorage
   * Returns null if state doesn't exist or is stale
   */
  static loadState(endpoint: string): CircuitBreakerState | null {
    try {
      const allStates = this.loadAllStates();
      const state = allStates[endpoint];

      if (!state) {
        return null;
      }

      // Check if state is stale (older than 5 minutes)
      const now = Date.now();
      const age = now - state.timestamp;

      if (age > this.STALE_THRESHOLD_MS) {
        // State is stale, remove it
        this.clearState(endpoint);
        return null;
      }

      return state;
    } catch (error) {
      logger.warn(
        'Failed to load circuit breaker state',
        'circuitBreakerPersistence',
        'loadState',
        { endpoint, error: String(error) }
      );
      return null;
    }
  }

  /**
   * Clear circuit breaker state for a specific endpoint or all endpoints
   */
  static clearState(endpoint?: string): void {
    try {
      if (endpoint) {
        // Clear specific endpoint
        const allStates = this.loadAllStates();
        delete allStates[endpoint];
        localStorage.setItem(this.STORAGE_KEY, JSON.stringify(allStates));
      } else {
        // Clear all states
        localStorage.removeItem(this.STORAGE_KEY);
      }
    } catch (error) {
      logger.warn(
        'Failed to clear circuit breaker state',
        'circuitBreakerPersistence',
        'clearState',
        { endpoint, error: String(error) }
      );
    }
  }

  /**
   * Load all circuit breaker states from localStorage
   */
  private static loadAllStates(): CircuitBreakerStateRecord {
    try {
      const data = localStorage.getItem(this.STORAGE_KEY);
      if (!data) {
        return {};
      }
      return JSON.parse(data) as CircuitBreakerStateRecord;
    } catch (error) {
      logger.warn(
        'Failed to parse circuit breaker state',
        'circuitBreakerPersistence',
        'loadAllStates',
        { error: String(error) }
      );
      return {};
    }
  }
}
