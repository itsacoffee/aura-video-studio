/**
 * Tests for Timeout Configuration
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { timeoutConfig, DEFAULT_TIMEOUTS, getOperationTimeout } from '../timeouts';

describe('TimeoutConfiguration', () => {
  beforeEach(() => {
    localStorage.clear();
    timeoutConfig.resetToDefaults();
  });

  afterEach(() => {
    localStorage.clear();
  });

  describe('Default Timeouts', () => {
    it('should have sensible default timeout values', () => {
      expect(DEFAULT_TIMEOUTS.default).toBe(30000);
      expect(DEFAULT_TIMEOUTS.health).toBe(5000);
      expect(DEFAULT_TIMEOUTS.scriptGeneration).toBe(120000);
      expect(DEFAULT_TIMEOUTS.videoRendering).toBe(600000);
    });

    it('should return default timeout for operation', () => {
      expect(timeoutConfig.getTimeout('default')).toBe(30000);
      expect(timeoutConfig.getTimeout('health')).toBe(5000);
      expect(timeoutConfig.getTimeout('scriptGeneration')).toBe(120000);
    });
  });

  describe('Setting Timeouts', () => {
    it('should update timeout for specific operation', () => {
      timeoutConfig.setTimeout('default', 60000);
      expect(timeoutConfig.getTimeout('default')).toBe(60000);
    });

    it('should persist timeout changes to localStorage', () => {
      timeoutConfig.setTimeout('scriptGeneration', 180000);

      const stored = localStorage.getItem('aura_timeout_config');
      expect(stored).toBeTruthy();

      const parsed = JSON.parse(stored!);
      expect(parsed.scriptGeneration).toBe(180000);
    });

    it('should reject timeout less than 1 second', () => {
      expect(() => {
        timeoutConfig.setTimeout('default', 500);
      }).toThrow('Timeout must be at least 1000ms');
    });

    it('should reject timeout greater than 1 hour', () => {
      expect(() => {
        timeoutConfig.setTimeout('default', 3700000);
      }).toThrow('Timeout cannot exceed 3600000ms');
    });

    it('should update multiple timeouts at once', () => {
      timeoutConfig.setTimeouts({
        default: 45000,
        health: 10000,
        scriptGeneration: 150000,
      });

      expect(timeoutConfig.getTimeout('default')).toBe(45000);
      expect(timeoutConfig.getTimeout('health')).toBe(10000);
      expect(timeoutConfig.getTimeout('scriptGeneration')).toBe(150000);
    });
  });

  describe('Getting Configuration', () => {
    it('should return all timeout configurations', () => {
      const config = timeoutConfig.getConfig();

      expect(config.default).toBeDefined();
      expect(config.health).toBeDefined();
      expect(config.scriptGeneration).toBeDefined();
      expect(config.videoRendering).toBeDefined();
    });

    it('should return a copy of config (not reference)', () => {
      const config1 = timeoutConfig.getConfig();
      config1.default = 99999;

      const config2 = timeoutConfig.getConfig();
      expect(config2.default).not.toBe(99999);
    });
  });

  describe('Resetting Timeouts', () => {
    it('should reset all timeouts to defaults', () => {
      timeoutConfig.setTimeout('default', 60000);
      timeoutConfig.setTimeout('health', 15000);

      timeoutConfig.resetToDefaults();

      expect(timeoutConfig.getTimeout('default')).toBe(DEFAULT_TIMEOUTS.default);
      expect(timeoutConfig.getTimeout('health')).toBe(DEFAULT_TIMEOUTS.health);
    });

    it('should reset specific timeout to default', () => {
      timeoutConfig.setTimeout('scriptGeneration', 180000);

      timeoutConfig.resetTimeout('scriptGeneration');

      expect(timeoutConfig.getTimeout('scriptGeneration')).toBe(DEFAULT_TIMEOUTS.scriptGeneration);
    });

    it('should persist reset to localStorage', () => {
      timeoutConfig.setTimeout('default', 60000);
      timeoutConfig.resetToDefaults();

      const stored = localStorage.getItem('aura_timeout_config');
      expect(stored).toBeTruthy();

      const parsed = JSON.parse(stored!);
      expect(parsed.default).toBe(DEFAULT_TIMEOUTS.default);
    });
  });

  describe('Operation Timeout Helper', () => {
    it('should return correct timeout for operation shorthand', () => {
      expect(getOperationTimeout('default')).toBe(DEFAULT_TIMEOUTS.default);
      expect(getOperationTimeout('health')).toBe(DEFAULT_TIMEOUTS.health);
      expect(getOperationTimeout('script')).toBe(DEFAULT_TIMEOUTS.scriptGeneration);
      expect(getOperationTimeout('tts')).toBe(DEFAULT_TIMEOUTS.tts);
      expect(getOperationTimeout('image')).toBe(DEFAULT_TIMEOUTS.imageGeneration);
      expect(getOperationTimeout('video')).toBe(DEFAULT_TIMEOUTS.videoGeneration);
      expect(getOperationTimeout('render')).toBe(DEFAULT_TIMEOUTS.videoRendering);
      expect(getOperationTimeout('upload')).toBe(DEFAULT_TIMEOUTS.fileUpload);
      expect(getOperationTimeout('download')).toBe(DEFAULT_TIMEOUTS.fileDownload);
      expect(getOperationTimeout('quick')).toBe(DEFAULT_TIMEOUTS.quickOperations);
    });

    it('should return default timeout for unknown operation', () => {
      expect(getOperationTimeout('unknown' as never)).toBe(DEFAULT_TIMEOUTS.default);
    });
  });

  describe('Persistence', () => {
    it('should save and load configuration from localStorage', () => {
      timeoutConfig.setTimeout('default', 50000);
      timeoutConfig.setTimeout('health', 8000);

      const stored = localStorage.getItem('aura_timeout_config');
      expect(stored).toBeTruthy();

      const parsed = JSON.parse(stored!);
      expect(parsed.default).toBe(50000);
      expect(parsed.health).toBe(8000);
    });

    it('should handle corrupted localStorage data', () => {
      localStorage.setItem('aura_timeout_config', 'invalid json');

      const config = timeoutConfig.getConfig();
      expect(config.default).toBe(DEFAULT_TIMEOUTS.default);
    });

    it('should use defaults when localStorage is empty', () => {
      localStorage.clear();

      const config = timeoutConfig.getConfig();
      expect(config.default).toBe(DEFAULT_TIMEOUTS.default);
      expect(config.health).toBe(DEFAULT_TIMEOUTS.health);
    });
  });

  describe('Error Handling', () => {
    it('should handle localStorage quota errors when saving', () => {
      const setItemSpy = vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
        throw new Error('QuotaExceededError');
      });

      expect(() => {
        timeoutConfig.setTimeout('default', 45000);
      }).not.toThrow();

      setItemSpy.mockRestore();
    });

    it('should handle localStorage errors when loading', () => {
      const getItemSpy = vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
        throw new Error('Storage error');
      });

      const config = timeoutConfig.getConfig();
      expect(config).toBeDefined();

      getItemSpy.mockRestore();
    });
  });
});
