/**
 * Custom Event Handlers Service
 * Implements or stubs handlers for custom menu events
 *
 * REQUIREMENT 7: Each custom event handler must have implemented logic or throw NotImplementedError
 */

import { loggingService } from './loggingService';
import { warnIfNoHandler } from './routeRegistry';

/**
 * Custom error thrown when a feature is not yet implemented
 */
export class NotImplementedError extends Error {
  constructor(featureName: string) {
    super(`Feature not yet implemented: ${featureName}`);
    this.name = 'NotImplementedError';
  }
}

/**
 * Handler for app:saveProject event
 * Currently not fully implemented - throws NotImplementedError
 */
function handleSaveProject(): void {
  loggingService.info('Save Project event triggered');
  warnIfNoHandler('app:saveProject');

  throw new NotImplementedError('Save Project');
}

/**
 * Handler for app:saveProjectAs event
 * Currently not fully implemented - throws NotImplementedError
 */
function handleSaveProjectAs(): void {
  loggingService.info('Save Project As event triggered');
  warnIfNoHandler('app:saveProjectAs');

  throw new NotImplementedError('Save Project As');
}

/**
 * Handler for app:showFind event
 * Currently not fully implemented - throws NotImplementedError
 */
function handleShowFind(): void {
  loggingService.info('Show Find event triggered');
  warnIfNoHandler('app:showFind');

  throw new NotImplementedError('Show Find Dialog');
}

/**
 * Handler for app:clearCache event
 * Currently not fully implemented - throws NotImplementedError
 */
function handleClearCache(): void {
  loggingService.info('Clear Cache event triggered');
  warnIfNoHandler('app:clearCache');

  throw new NotImplementedError('Clear Cache');
}

/**
 * Handler for app:showKeyboardShortcuts event
 * Currently not fully implemented - throws NotImplementedError
 */
function handleShowKeyboardShortcuts(): void {
  loggingService.info('Show Keyboard Shortcuts event triggered');
  warnIfNoHandler('app:showKeyboardShortcuts');

  throw new NotImplementedError('Show Keyboard Shortcuts');
}

/**
 * Handler for app:checkForUpdates event
 * Currently not fully implemented - throws NotImplementedError
 */
function handleCheckForUpdates(): void {
  loggingService.info('Check for Updates event triggered');
  warnIfNoHandler('app:checkForUpdates');

  throw new NotImplementedError('Check for Updates');
}

/**
 * Registers all custom event handlers on the window object
 * Should be called once at application startup
 */
export function registerCustomEventHandlers(): void {
  loggingService.info('Registering custom event handlers...');

  window.addEventListener('app:saveProject', () => {
    try {
      handleSaveProject();
    } catch (err) {
      const error = err instanceof Error ? err : new Error(String(err));
      if (error instanceof NotImplementedError) {
        loggingService.warn(error.message);
        console.warn(`[Custom Events] ${error.message}`);
      } else {
        loggingService.error(
          'Error in app:saveProject handler',
          error,
          'customEventHandlers',
          'saveProject'
        );
        throw error;
      }
    }
  });

  window.addEventListener('app:saveProjectAs', () => {
    try {
      handleSaveProjectAs();
    } catch (err) {
      const error = err instanceof Error ? err : new Error(String(err));
      if (error instanceof NotImplementedError) {
        loggingService.warn(error.message);
        console.warn(`[Custom Events] ${error.message}`);
      } else {
        loggingService.error(
          'Error in app:saveProjectAs handler',
          error,
          'customEventHandlers',
          'saveProjectAs'
        );
        throw error;
      }
    }
  });

  window.addEventListener('app:showFind', () => {
    try {
      handleShowFind();
    } catch (err) {
      const error = err instanceof Error ? err : new Error(String(err));
      if (error instanceof NotImplementedError) {
        loggingService.warn(error.message);
        console.warn(`[Custom Events] ${error.message}`);
      } else {
        loggingService.error(
          'Error in app:showFind handler',
          error,
          'customEventHandlers',
          'showFind'
        );
        throw error;
      }
    }
  });

  window.addEventListener('app:clearCache', () => {
    try {
      handleClearCache();
    } catch (err) {
      const error = err instanceof Error ? err : new Error(String(err));
      if (error instanceof NotImplementedError) {
        loggingService.warn(error.message);
        console.warn(`[Custom Events] ${error.message}`);
      } else {
        loggingService.error(
          'Error in app:clearCache handler',
          error,
          'customEventHandlers',
          'clearCache'
        );
        throw error;
      }
    }
  });

  window.addEventListener('app:showKeyboardShortcuts', () => {
    try {
      handleShowKeyboardShortcuts();
    } catch (err) {
      const error = err instanceof Error ? err : new Error(String(err));
      if (error instanceof NotImplementedError) {
        loggingService.warn(error.message);
        console.warn(`[Custom Events] ${error.message}`);
      } else {
        loggingService.error(
          'Error in app:showKeyboardShortcuts handler',
          error,
          'customEventHandlers',
          'showKeyboardShortcuts'
        );
        throw error;
      }
    }
  });

  window.addEventListener('app:checkForUpdates', () => {
    try {
      handleCheckForUpdates();
    } catch (err) {
      const error = err instanceof Error ? err : new Error(String(err));
      if (error instanceof NotImplementedError) {
        loggingService.warn(error.message);
        console.warn(`[Custom Events] ${error.message}`);
      } else {
        loggingService.error(
          'Error in app:checkForUpdates handler',
          error,
          'customEventHandlers',
          'checkForUpdates'
        );
        throw error;
      }
    }
  });

  loggingService.info('Custom event handlers registered successfully');
}

/**
 * Unregisters all custom event handlers
 * Should be called when cleaning up (e.g., in tests)
 */
export function unregisterCustomEventHandlers(): void {
  window.removeEventListener('app:saveProject', handleSaveProject);
  window.removeEventListener('app:saveProjectAs', handleSaveProjectAs);
  window.removeEventListener('app:showFind', handleShowFind);
  window.removeEventListener('app:clearCache', handleClearCache);
  window.removeEventListener('app:showKeyboardShortcuts', handleShowKeyboardShortcuts);
  window.removeEventListener('app:checkForUpdates', handleCheckForUpdates);

  loggingService.info('Custom event handlers unregistered');
}
