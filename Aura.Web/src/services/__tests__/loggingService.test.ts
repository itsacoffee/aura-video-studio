/**
 * Tests for the logging service
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { loggingService, createLogger } from '../loggingService';

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

describe('LoggingService', () => {
  beforeEach(() => {
    // Clear localStorage before each test
    localStorage.clear();
    // Clear logs
    loggingService.clearLogs();
    // Reset configuration to defaults
    loggingService.configure({
      enableConsole: false, // Disable console logging during tests
      enablePersistence: true,
      maxStoredLogs: 1000,
      minLogLevel: 'debug',
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('Basic Logging', () => {
    it('should log debug messages', () => {
      loggingService.debug('Test debug message', 'TestComponent', 'testAction', { key: 'value' });

      const logs = loggingService.getLogs();
      expect(logs).toHaveLength(1);
      expect(logs[0].level).toBe('debug');
      expect(logs[0].message).toBe('Test debug message');
      expect(logs[0].component).toBe('TestComponent');
      expect(logs[0].action).toBe('testAction');
      expect(logs[0].context).toEqual({ key: 'value' });
    });

    it('should log info messages', () => {
      loggingService.info('Test info message');

      const logs = loggingService.getLogs();
      expect(logs).toHaveLength(1);
      expect(logs[0].level).toBe('info');
      expect(logs[0].message).toBe('Test info message');
    });

    it('should log warning messages', () => {
      loggingService.warn('Test warning message', 'Component');

      const logs = loggingService.getLogs();
      expect(logs).toHaveLength(1);
      expect(logs[0].level).toBe('warn');
      expect(logs[0].message).toBe('Test warning message');
      expect(logs[0].component).toBe('Component');
    });

    it('should log error messages with error objects', () => {
      const error = new Error('Test error');
      loggingService.error('Error occurred', error, 'ErrorComponent');

      const logs = loggingService.getLogs();
      expect(logs).toHaveLength(1);
      expect(logs[0].level).toBe('error');
      expect(logs[0].message).toBe('Error occurred');
      expect(logs[0].error?.message).toBe('Test error');
      expect(logs[0].error?.name).toBe('Error');
    });

    it('should include timestamp in log entries', () => {
      loggingService.info('Test message');

      const logs = loggingService.getLogs();
      expect(logs[0].timestamp).toBeDefined();
      expect(new Date(logs[0].timestamp).getTime()).toBeGreaterThan(0);
    });
  });

  describe('Performance Logging', () => {
    it('should log performance metrics', () => {
      loggingService.performance('testOperation', 1500, 'PerformanceComponent', {
        operationId: '123',
      });

      const logs = loggingService.getLogs();
      expect(logs).toHaveLength(1);
      expect(logs[0].level).toBe('info');
      expect(logs[0].action).toBe('performance');
      expect(logs[0].performance?.operation).toBe('testOperation');
      expect(logs[0].performance?.duration).toBe(1500);
    });

    it('should measure async operation performance', async () => {
      const testFn = async () => {
        await new Promise((resolve) => setTimeout(resolve, 50));
        return 'result';
      };

      const result = await loggingService.measurePerformance('asyncOp', testFn, 'Component');

      expect(result).toBe('result');
      const logs = loggingService.getLogs();
      expect(logs).toHaveLength(1);
      expect(logs[0].performance?.operation).toBe('asyncOp');
      expect(logs[0].performance?.duration).toBeGreaterThan(0);
    });

    it('should measure sync operation performance', () => {
      const testFn = () => {
        let sum = 0;
        for (let i = 0; i < 100; i++) {
          sum += i;
        }
        return sum;
      };

      const result = loggingService.measurePerformanceSync('syncOp', testFn, 'Component');

      expect(result).toBe(4950);
      const logs = loggingService.getLogs();
      expect(logs).toHaveLength(1);
      expect(logs[0].performance?.operation).toBe('syncOp');
    });

    it('should log performance even when operation fails', async () => {
      const testFn = async () => {
        throw new Error('Operation failed');
      };

      await expect(
        loggingService.measurePerformance('failingOp', testFn, 'Component')
      ).rejects.toThrow('Operation failed');

      const logs = loggingService.getLogs();
      expect(logs).toHaveLength(1);
      expect(logs[0].context?.error).toBe(true);
    });
  });

  describe('Log Configuration', () => {
    it('should respect minimum log level', () => {
      loggingService.configure({ minLogLevel: 'warn' });

      loggingService.debug('Debug message');
      loggingService.info('Info message');
      loggingService.warn('Warning message');
      loggingService.error('Error message', new Error('test'));

      const logs = loggingService.getLogs();
      expect(logs).toHaveLength(2);
      expect(logs[0].level).toBe('warn');
      expect(logs[1].level).toBe('error');
    });

    it('should limit stored logs to maxStoredLogs', () => {
      loggingService.configure({ maxStoredLogs: 5 });

      for (let i = 0; i < 10; i++) {
        loggingService.info(`Message ${i}`);
      }

      const logs = loggingService.getLogs();
      expect(logs).toHaveLength(5);
      expect(logs[0].message).toBe('Message 5');
      expect(logs[4].message).toBe('Message 9');
    });

    it('should get current configuration', () => {
      const config = loggingService.getConfig();
      expect(config).toHaveProperty('enableConsole');
      expect(config).toHaveProperty('enablePersistence');
      expect(config).toHaveProperty('maxStoredLogs');
      expect(config).toHaveProperty('minLogLevel');
    });
  });

  describe('Log Filtering', () => {
    beforeEach(() => {
      loggingService.debug('Debug message', 'ComponentA');
      loggingService.info('Info message', 'ComponentB');
      loggingService.warn('Warning message', 'ComponentA');
      loggingService.error('Error message', new Error('test'), 'ComponentB');
    });

    it('should filter logs by level', () => {
      const debugLogs = loggingService.getLogsByLevel('debug');
      expect(debugLogs).toHaveLength(1);
      expect(debugLogs[0].level).toBe('debug');

      const errorLogs = loggingService.getLogsByLevel('error');
      expect(errorLogs).toHaveLength(1);
      expect(errorLogs[0].level).toBe('error');
    });

    it('should filter logs by component', () => {
      const componentALogs = loggingService.getLogsByComponent('ComponentA');
      expect(componentALogs).toHaveLength(2);
      expect(componentALogs.every((log) => log.component === 'ComponentA')).toBe(true);
    });

    it('should filter logs by time range', () => {
      const now = new Date();
      const oneMinuteAgo = new Date(now.getTime() - 60000);
      const oneMinuteFromNow = new Date(now.getTime() + 60000);

      const logs = loggingService.getLogsByTimeRange(oneMinuteAgo, oneMinuteFromNow);
      expect(logs.length).toBeGreaterThan(0);
    });
  });

  describe('Log Persistence', () => {
    it('should persist logs to localStorage when enabled', () => {
      loggingService.configure({ enablePersistence: true });
      loggingService.info('Persisted message');

      const stored = localStorage.getItem('app_logs');
      expect(stored).toBeDefined();

      const logs = JSON.parse(stored!);
      expect(logs).toHaveLength(1);
      expect(logs[0].message).toBe('Persisted message');
    });

    it('should not persist logs when disabled', () => {
      localStorage.clear();
      loggingService.configure({ enablePersistence: false });
      loggingService.info('Non-persisted message');

      const stored = localStorage.getItem('app_logs');
      expect(stored).toBeNull();
    });
  });

  describe('Log Export and Clear', () => {
    beforeEach(() => {
      loggingService.info('Message 1');
      loggingService.warn('Message 2');
      loggingService.error('Message 3', new Error('test'));
    });

    it('should export logs as JSON', () => {
      const exported = loggingService.exportLogs();
      const logs = JSON.parse(exported);

      expect(Array.isArray(logs)).toBe(true);
      expect(logs).toHaveLength(3);
      expect(logs[0].message).toBe('Message 1');
    });

    it('should clear all logs', () => {
      loggingService.clearLogs();

      const logs = loggingService.getLogs();
      expect(logs).toHaveLength(0);

      const stored = localStorage.getItem('app_logs');
      expect(stored).toBeNull();
    });
  });

  describe('Scoped Logger', () => {
    it('should create scoped logger for component', () => {
      const componentLogger = createLogger('TestComponent');

      componentLogger.info('Info from component');
      componentLogger.error('Error from component', new Error('test'));

      const logs = loggingService.getLogs();
      expect(logs).toHaveLength(2);
      expect(logs.every((log) => log.component === 'TestComponent')).toBe(true);
    });

    it('should use component logger method', () => {
      const logger = loggingService.createLogger('MyComponent');

      logger.debug('Debug message', 'action1');
      logger.info('Info message', 'action2', { key: 'value' });

      const logs = loggingService.getLogs();
      expect(logs).toHaveLength(2);
      expect(logs[0].component).toBe('MyComponent');
      expect(logs[0].action).toBe('action1');
      expect(logs[1].action).toBe('action2');
    });

    it('should measure performance with scoped logger', () => {
      const logger = loggingService.createLogger('PerfComponent');

      logger.performance('testOp', 100, { metadata: 'test' });

      const logs = loggingService.getLogs();
      expect(logs).toHaveLength(1);
      expect(logs[0].component).toBe('PerfComponent');
      expect(logs[0].performance?.duration).toBe(100);
    });
  });

  describe('Log Listeners', () => {
    it('should notify listeners of new log entries', () => {
      const listener = vi.fn();
      const unsubscribe = loggingService.subscribe(listener);

      loggingService.info('Test message');

      expect(listener).toHaveBeenCalledTimes(1);
      expect(listener).toHaveBeenCalledWith(
        expect.objectContaining({
          level: 'info',
          message: 'Test message',
        })
      );

      unsubscribe();
    });

    it('should unsubscribe listeners', () => {
      const listener = vi.fn();
      const unsubscribe = loggingService.subscribe(listener);

      unsubscribe();
      loggingService.info('Test message');

      expect(listener).not.toHaveBeenCalled();
    });

    it('should handle errors in listeners gracefully', () => {
      const faultyListener = () => {
        throw new Error('Listener error');
      };

      loggingService.subscribe(faultyListener);

      // Should not throw
      expect(() => loggingService.info('Test message')).not.toThrow();
    });
  });
});
