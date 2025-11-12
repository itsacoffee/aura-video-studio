/**
 * Tests for Custom Event Handlers Service
 *
 * REQUIREMENT 7: Each custom event handler must have implemented logic or throw NotImplementedError
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

// Mock loggingService before importing
vi.mock('../services/loggingService', () => ({
  loggingService: {
    info: vi.fn(),
    error: vi.fn(),
    warn: vi.fn(),
  },
}));

// Mock routeRegistry
vi.mock('../services/routeRegistry', () => ({
  warnIfNoHandler: vi.fn(),
}));

import {
  registerCustomEventHandlers,
  unregisterCustomEventHandlers,
  NotImplementedError,
} from '../services/customEventHandlers';
import { loggingService } from '../services/loggingService';

describe('Custom Event Handlers Service', () => {
  let consoleWarnSpy: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    vi.clearAllMocks();
    consoleWarnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
  });

  afterEach(() => {
    unregisterCustomEventHandlers();
    consoleWarnSpy.mockRestore();
  });

  describe('registerCustomEventHandlers', () => {
    it('should register all custom event handlers', () => {
      registerCustomEventHandlers();

      expect(loggingService.info).toHaveBeenCalledWith(
        expect.stringContaining('Registering custom event handlers')
      );
      expect(loggingService.info).toHaveBeenCalledWith(
        expect.stringContaining('registered successfully')
      );
    });

    it('should register app:saveProject handler', () => {
      registerCustomEventHandlers();

      const event = new CustomEvent('app:saveProject');
      expect(() => window.dispatchEvent(event)).not.toThrow();
    });

    it('should register app:saveProjectAs handler', () => {
      registerCustomEventHandlers();

      const event = new CustomEvent('app:saveProjectAs');
      expect(() => window.dispatchEvent(event)).not.toThrow();
    });

    it('should register app:showFind handler', () => {
      registerCustomEventHandlers();

      const event = new CustomEvent('app:showFind');
      expect(() => window.dispatchEvent(event)).not.toThrow();
    });

    it('should register app:clearCache handler', () => {
      registerCustomEventHandlers();

      const event = new CustomEvent('app:clearCache');
      expect(() => window.dispatchEvent(event)).not.toThrow();
    });

    it('should register app:showKeyboardShortcuts handler', () => {
      registerCustomEventHandlers();

      const event = new CustomEvent('app:showKeyboardShortcuts');
      expect(() => window.dispatchEvent(event)).not.toThrow();
    });

    it('should register app:checkForUpdates handler', () => {
      registerCustomEventHandlers();

      const event = new CustomEvent('app:checkForUpdates');
      expect(() => window.dispatchEvent(event)).not.toThrow();
    });
  });

  describe('NotImplementedError', () => {
    it('should be thrown for unimplemented features', () => {
      registerCustomEventHandlers();

      const event = new CustomEvent('app:saveProject');
      window.dispatchEvent(event);

      // Should log warning about not implemented
      expect(consoleWarnSpy).toHaveBeenCalledWith(expect.stringContaining('not yet implemented'));
    });

    it('should have correct error message format', () => {
      const error = new NotImplementedError('Test Feature');

      expect(error.message).toContain('Test Feature');
      expect(error.message).toContain('not yet implemented');
      expect(error.name).toBe('NotImplementedError');
    });

    it('should be instance of Error', () => {
      const error = new NotImplementedError('Test');

      expect(error).toBeInstanceOf(Error);
      expect(error).toBeInstanceOf(NotImplementedError);
    });
  });

  describe('Handler Error Handling', () => {
    it('should catch and log NotImplementedError for app:saveProject', () => {
      registerCustomEventHandlers();

      const event = new CustomEvent('app:saveProject');
      window.dispatchEvent(event);

      expect(loggingService.warn).toHaveBeenCalledWith(expect.stringContaining('Save Project'));
    });

    it('should catch and log NotImplementedError for app:clearCache', () => {
      registerCustomEventHandlers();

      const event = new CustomEvent('app:clearCache');
      window.dispatchEvent(event);

      expect(loggingService.warn).toHaveBeenCalledWith(expect.stringContaining('Clear Cache'));
    });

    it('should catch and log NotImplementedError for app:showKeyboardShortcuts', () => {
      registerCustomEventHandlers();

      const event = new CustomEvent('app:showKeyboardShortcuts');
      window.dispatchEvent(event);

      expect(loggingService.warn).toHaveBeenCalledWith(
        expect.stringContaining('Keyboard Shortcuts')
      );
    });
  });

  describe('unregisterCustomEventHandlers', () => {
    it('should unregister all handlers', () => {
      registerCustomEventHandlers();
      unregisterCustomEventHandlers();

      expect(loggingService.info).toHaveBeenCalledWith(expect.stringContaining('unregistered'));
    });

    it('should clean up handlers properly', () => {
      registerCustomEventHandlers();

      const event = new CustomEvent('app:saveProject');
      window.dispatchEvent(event);

      unregisterCustomEventHandlers();
      vi.clearAllMocks();

      window.dispatchEvent(event);

      // Should not trigger any new warnings after unregister
      expect(loggingService.warn).not.toHaveBeenCalled();
    });
  });

  describe('Event Logging', () => {
    it('should log info when events are triggered', () => {
      registerCustomEventHandlers();

      const event = new CustomEvent('app:saveProject');
      window.dispatchEvent(event);

      expect(loggingService.info).toHaveBeenCalledWith(
        expect.stringContaining('Save Project event triggered')
      );
    });

    it('should log different messages for different events', () => {
      registerCustomEventHandlers();

      window.dispatchEvent(new CustomEvent('app:saveProject'));
      window.dispatchEvent(new CustomEvent('app:clearCache'));

      expect(loggingService.info).toHaveBeenCalledWith(expect.stringContaining('Save Project'));
      expect(loggingService.info).toHaveBeenCalledWith(expect.stringContaining('Clear Cache'));
    });
  });

  describe('Integration with routeRegistry', () => {
    it('should call warnIfNoHandler for each event', async () => {
      const { warnIfNoHandler } = await import('../services/routeRegistry');

      registerCustomEventHandlers();

      window.dispatchEvent(new CustomEvent('app:saveProject'));

      expect(warnIfNoHandler).toHaveBeenCalledWith('app:saveProject');
    });
  });

  describe('All Required Handlers', () => {
    it('should handle all 6 custom events', () => {
      registerCustomEventHandlers();

      const events = [
        'app:saveProject',
        'app:saveProjectAs',
        'app:showFind',
        'app:clearCache',
        'app:showKeyboardShortcuts',
        'app:checkForUpdates',
      ];

      events.forEach((eventName) => {
        const event = new CustomEvent(eventName);
        expect(() => window.dispatchEvent(event)).not.toThrow();
      });

      // Each should have logged
      expect(loggingService.info.mock.calls.length).toBeGreaterThanOrEqual(events.length + 2);
    });
  });
});
